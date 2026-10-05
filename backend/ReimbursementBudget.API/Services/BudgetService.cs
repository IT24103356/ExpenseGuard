using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;
using ReimbursementBudget.API.DTOs;

namespace ReimbursementBudget.API.Services
{
    public interface IBudgetService
    {
        Task<ServiceResult<bool>> CheckBudgetAvailabilityAsync(string departmentId, decimal amount, int fiscalYear);
        Task<ServiceResult<BudgetDto>> GetBudgetAsync(Guid id);
        Task<ServiceResult<List<BudgetDto>>> GetAllBudgetsAsync(int? fiscalYear);
        Task<ServiceResult<BudgetDto>> CreateBudgetAsync(CreateBudgetRequest request);
        Task<ServiceResult<BudgetDto>> UpdateBudgetAsync(Guid id, UpdateBudgetRequest request);
        Task<ServiceResult<List<BudgetTransactionDto>>> GetBudgetTransactionsAsync(Guid budgetId);
        Task<ServiceResult<BudgetSummaryDto>> GetBudgetSummaryAsync(Guid budgetId);
        Task<ServiceResult<bool>> DeductBudgetAsync(string departmentId, decimal amount, Guid reimbursementId, Guid claimId, int fiscalYear);
        Task<ServiceResult<List<BudgetDto>>> GetDepartmentBudgetsAsync(string departmentId);
    }

    public class BudgetService : IBudgetService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BudgetService> _logger;

        public BudgetService(AppDbContext db, ILogger<BudgetService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> CheckBudgetAvailabilityAsync(string departmentId, decimal amount, int fiscalYear)
        {
            var budget = await _db.DepartmentBudgets
                .FirstOrDefaultAsync(b => b.DepartmentId == departmentId &&
                                          b.FiscalYear == fiscalYear &&
                                          b.IsActive);

            if (budget == null)
            {
                _logger.LogWarning("No budget found for Department {Dept} FY{Year}", departmentId, fiscalYear);
                return ServiceResult<bool>.Fail($"No active budget found for department {departmentId} in fiscal year {fiscalYear}");
            }

            var remaining = budget.AllocatedAmount - budget.ApprovedSpend;
            var available = remaining >= amount;

            _logger.LogInformation("Budget check: Dept={Dept} Allocated={Alloc} Spent={Spent} Remaining={Remaining} Requested={Amount} Available={Available}",
                departmentId, budget.AllocatedAmount, budget.ApprovedSpend, remaining, amount, available);

            return ServiceResult<bool>.Ok(available);
        }

        public async Task<ServiceResult<bool>> DeductBudgetAsync(
            string departmentId, decimal amount, Guid reimbursementId, Guid claimId, int fiscalYear)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var budget = await _db.DepartmentBudgets
                    .FirstOrDefaultAsync(b => b.DepartmentId == departmentId &&
                                              b.FiscalYear == fiscalYear &&
                                              b.IsActive);

                if (budget == null)
                    return ServiceResult<bool>.Fail("Budget not found");

                budget.ApprovedSpend += amount;
                budget.PaidSpend += amount;
                budget.UpdatedAt = DateTime.UtcNow;

                var txn = new BudgetTransaction
                {
                    DepartmentBudgetId = budget.Id,
                    ReimbursementId = reimbursementId,
                    ExpenseClaimId = claimId,
                    Amount = amount,
                    TransactionType = BudgetTransactionType.Debit,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Payment for reimbursement {reimbursementId}"
                };
                _db.BudgetTransactions.Add(txn);

                // Check budget alerts (80% and 90% thresholds)
                await CheckAndCreateAlertsAsync(budget);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Budget deducted: Dept={Dept} Amount={Amount} NewSpend={Spend}",
                    departmentId, amount, budget.ApprovedSpend);

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Budget deduction failed for Dept {Dept}", departmentId);
                return ServiceResult<bool>.Fail($"Budget deduction failed: {ex.Message}");
            }
        }

        private async Task CheckAndCreateAlertsAsync(DepartmentBudget budget)
        {
            var utilization = budget.AllocatedAmount > 0
                ? (budget.ApprovedSpend / budget.AllocatedAmount) * 100
                : 0;

            decimal[] thresholds = { 80m, 90m, 100m };
            foreach (var threshold in thresholds)
            {
                if (utilization >= threshold)
                {
                    var exists = await _db.BudgetAlerts.AnyAsync(a =>
                        a.DepartmentBudgetId == budget.Id &&
                        a.ThresholdPercentage == threshold &&
                        a.Status == BudgetAlertStatus.Active);

                    if (!exists)
                    {
                        _db.BudgetAlerts.Add(new BudgetAlert
                        {
                            DepartmentId = budget.DepartmentId,
                            DepartmentBudgetId = budget.Id,
                            ThresholdPercentage = threshold,
                            CurrentSpend = budget.ApprovedSpend,
                            BudgetAmount = budget.AllocatedAmount,
                            Status = BudgetAlertStatus.Active
                        });
                    }
                }
            }
        }

        public async Task<ServiceResult<BudgetDto>> GetBudgetAsync(Guid id)
        {
            var b = await _db.DepartmentBudgets.FindAsync(id);
            if (b == null) return ServiceResult<BudgetDto>.Fail("Budget not found");
            return ServiceResult<BudgetDto>.Ok(MapToDto(b));
        }

        public async Task<ServiceResult<List<BudgetDto>>> GetAllBudgetsAsync(int? fiscalYear)
        {
            var query = _db.DepartmentBudgets.AsQueryable();
            if (fiscalYear.HasValue) query = query.Where(b => b.FiscalYear == fiscalYear.Value);
            var results = await query.OrderBy(b => b.DepartmentName).ToListAsync();
            return ServiceResult<List<BudgetDto>>.Ok(results.Select(MapToDto).ToList());
        }

        public async Task<ServiceResult<BudgetDto>> CreateBudgetAsync(CreateBudgetRequest request)
        {
            var existing = await _db.DepartmentBudgets.AnyAsync(b =>
                b.DepartmentId == request.DepartmentId &&
                b.FiscalYear == request.FiscalYear &&
                b.BudgetPeriod == request.BudgetPeriod);

            if (existing)
                return ServiceResult<BudgetDto>.Fail("Budget already exists for this department/period");

            var budget = new DepartmentBudget
            {
                DepartmentId = request.DepartmentId,
                DepartmentName = request.DepartmentName,
                FiscalYear = request.FiscalYear,
                BudgetPeriod = request.BudgetPeriod ?? "ANNUAL",
                AllocatedAmount = request.AllocatedAmount,
                Currency = request.Currency ?? "LKR"
            };

            _db.DepartmentBudgets.Add(budget);

            // Create initial credit transaction
            _db.BudgetTransactions.Add(new BudgetTransaction
            {
                DepartmentBudgetId = budget.Id,
                Amount = request.AllocatedAmount,
                TransactionType = BudgetTransactionType.Credit,
                Description = $"Initial budget allocation for {request.DepartmentName} FY{request.FiscalYear}"
            });

            await _db.SaveChangesAsync();
            return ServiceResult<BudgetDto>.Ok(MapToDto(budget));
        }

        public async Task<ServiceResult<BudgetDto>> UpdateBudgetAsync(Guid id, UpdateBudgetRequest request)
        {
            var budget = await _db.DepartmentBudgets.FindAsync(id);
            if (budget == null) return ServiceResult<BudgetDto>.Fail("Budget not found");

            var oldAmount = budget.AllocatedAmount;
            budget.AllocatedAmount = request.AllocatedAmount;
            budget.UpdatedAt = DateTime.UtcNow;

            var diff = request.AllocatedAmount - oldAmount;
            _db.BudgetTransactions.Add(new BudgetTransaction
            {
                DepartmentBudgetId = budget.Id,
                Amount = Math.Abs(diff),
                TransactionType = diff >= 0 ? BudgetTransactionType.Credit : BudgetTransactionType.Debit,
                Description = $"Budget reallocation: {oldAmount} → {request.AllocatedAmount}"
            });

            await _db.SaveChangesAsync();
            return ServiceResult<BudgetDto>.Ok(MapToDto(budget));
        }

        public async Task<ServiceResult<List<BudgetTransactionDto>>> GetBudgetTransactionsAsync(Guid budgetId)
        {
            var txns = await _db.BudgetTransactions
                .Where(bt => bt.DepartmentBudgetId == budgetId)
                .OrderByDescending(bt => bt.TransactionDate)
                .ToListAsync();

            var dtos = txns.Select(bt => new BudgetTransactionDto
            {
                Id = bt.Id,
                DepartmentBudgetId = bt.DepartmentBudgetId,
                ReimbursementId = bt.ReimbursementId,
                Amount = bt.Amount,
                TransactionType = bt.TransactionType,
                TransactionDate = bt.TransactionDate,
                Description = bt.Description
            }).ToList();

            return ServiceResult<List<BudgetTransactionDto>>.Ok(dtos);
        }

        public async Task<ServiceResult<BudgetSummaryDto>> GetBudgetSummaryAsync(Guid budgetId)
        {
            var budget = await _db.DepartmentBudgets.FindAsync(budgetId);
            if (budget == null) return ServiceResult<BudgetSummaryDto>.Fail("Budget not found");

            var alerts = await _db.BudgetAlerts
                .Where(a => a.DepartmentBudgetId == budgetId && a.Status == BudgetAlertStatus.Active)
                .ToListAsync();

            return ServiceResult<BudgetSummaryDto>.Ok(new BudgetSummaryDto
            {
                BudgetId = budget.Id,
                DepartmentId = budget.DepartmentId,
                DepartmentName = budget.DepartmentName,
                FiscalYear = budget.FiscalYear,
                AllocatedAmount = budget.AllocatedAmount,
                ApprovedSpend = budget.ApprovedSpend,
                PaidSpend = budget.PaidSpend,
                RemainingBudget = budget.RemainingBudget,
                UtilizationPercentage = budget.UtilizationPercentage,
                Currency = budget.Currency,
                ActiveAlerts = alerts.Count
            });
        }

        public async Task<ServiceResult<List<BudgetDto>>> GetDepartmentBudgetsAsync(string departmentId)
        {
            var results = await _db.DepartmentBudgets
                .Where(b => b.DepartmentId == departmentId)
                .OrderByDescending(b => b.FiscalYear)
                .ToListAsync();
            return ServiceResult<List<BudgetDto>>.Ok(results.Select(MapToDto).ToList());
        }

        private static BudgetDto MapToDto(DepartmentBudget b) => new()
        {
            Id = b.Id,
            DepartmentId = b.DepartmentId,
            DepartmentName = b.DepartmentName,
            FiscalYear = b.FiscalYear,
            BudgetPeriod = b.BudgetPeriod,
            AllocatedAmount = b.AllocatedAmount,
            ApprovedSpend = b.ApprovedSpend,
            PaidSpend = b.PaidSpend,
            RemainingBudget = b.RemainingBudget,
            UtilizationPercentage = b.UtilizationPercentage,
            Currency = b.Currency,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
