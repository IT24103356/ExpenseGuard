using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;
using ReimbursementBudget.API.DTOs;

namespace ReimbursementBudget.API.Services
{
    public interface IReportingService
    {
        Task<SpendVsBudgetReport> GetSpendVsBudgetReportAsync(int? fiscalYear, string? departmentId);
        Task<List<MonthlySpendDto>> GetMonthlySpendingAsync(int? fiscalYear, string? departmentId);
        Task<List<CategorySpendDto>> GetCategorySpendingAsync(int? fiscalYear, string? departmentId);
        Task<ReimbursementSummaryReport> GetReimbursementSummaryAsync(DateTime? from, DateTime? to);
        Task<PaymentSummaryReport> GetPaymentSummaryAsync(DateTime? from, DateTime? to);
        Task<FinanceDashboardDto> GetFinanceDashboardAsync(int? fiscalYear);
        Task<List<DepartmentSpendDto>> GetDepartmentSpendingAsync(string departmentId, int? fiscalYear);
    }

    public class ReportingService : IReportingService
    {
        private readonly AppDbContext _db;

        public ReportingService(AppDbContext db) => _db = db;

        public async Task<SpendVsBudgetReport> GetSpendVsBudgetReportAsync(int? fiscalYear, string? departmentId)
        {
            var year = fiscalYear ?? DateTime.UtcNow.Year;
            var budgetQuery = _db.DepartmentBudgets.Where(b => b.FiscalYear == year && b.IsActive);
            if (!string.IsNullOrEmpty(departmentId))
                budgetQuery = budgetQuery.Where(b => b.DepartmentId == departmentId);

            var budgets = await budgetQuery.ToListAsync();

            var reimbQuery = _db.Reimbursements.AsQueryable();
            if (!string.IsNullOrEmpty(departmentId))
                reimbQuery = reimbQuery.Where(r => r.DepartmentId == departmentId);

            var reimbursements = await reimbQuery.ToListAsync();

            var deptSummaries = budgets.Select(b =>
            {
                var deptReimbs = reimbursements.Where(r => r.DepartmentId == b.DepartmentId).ToList();
                return new DepartmentSpendDto
                {
                    DepartmentId = b.DepartmentId,
                    DepartmentName = b.DepartmentName,
                    AllocatedBudget = b.AllocatedAmount,
                    TotalSpent = b.ApprovedSpend,
                    RemainingBudget = b.RemainingBudget,
                    UtilizationPercentage = b.UtilizationPercentage,
                    ReimbursementCount = deptReimbs.Count
                };
            }).ToList();

            var totalAllocated = budgets.Sum(b => b.AllocatedAmount);
            var totalSpent = budgets.Sum(b => b.ApprovedSpend);
            var pending = reimbursements.Where(r =>
                r.Status == ReimbursementStatus.Approved ||
                r.Status == ReimbursementStatus.ReadyForFinance ||
                r.Status == ReimbursementStatus.Processing).Sum(r => r.Amount);

            return new SpendVsBudgetReport
            {
                DepartmentSummaries = deptSummaries,
                TotalAllocated = totalAllocated,
                TotalSpent = totalSpent,
                TotalPending = pending,
                TotalRemaining = totalAllocated - totalSpent,
                OverallUtilization = totalAllocated > 0 ? Math.Round((totalSpent / totalAllocated) * 100, 2) : 0,
                PendingReimbursements = reimbursements.Count(r => r.Status == ReimbursementStatus.Approved),
                ProcessingCount = reimbursements.Count(r => r.Status == ReimbursementStatus.Processing),
                PaidCount = reimbursements.Count(r => r.Status == ReimbursementStatus.Paid),
                FailedPayments = reimbursements.Count(r => r.Status == ReimbursementStatus.PaymentFailed),
                Period = $"FY{year}"
            };
        }

        public async Task<List<MonthlySpendDto>> GetMonthlySpendingAsync(int? fiscalYear, string? departmentId)
        {
            var year = fiscalYear ?? DateTime.UtcNow.Year;
            var query = _db.Reimbursements
                .Where(r => r.CompletedAt.HasValue &&
                            r.CompletedAt.Value.Year == year &&
                            r.Status == ReimbursementStatus.Paid);

            if (!string.IsNullOrEmpty(departmentId))
                query = query.Where(r => r.DepartmentId == departmentId);

            var paid = await query.ToListAsync();

            return paid
                .GroupBy(r => r.CompletedAt!.Value.Month)
                .Select(g =>
                {
                    var monthName = new DateTime(year, g.Key, 1).ToString("MMM yyyy");
                    var total = g.Sum(r => r.Amount);
                    return new MonthlySpendDto
                    {
                        Month = monthName,
                        TotalSpend = total,
                        ReimbursementCount = g.Count(),
                        AverageReimbursement = g.Any() ? Math.Round(total / g.Count(), 2) : 0
                    };
                })
                .OrderBy(m => DateTime.ParseExact(m.Month, "MMM yyyy", null))
                .ToList();
        }

        public async Task<List<CategorySpendDto>> GetCategorySpendingAsync(int? fiscalYear, string? departmentId)
        {
            // Category data would normally come from the ExpenseClaim entity in MEM1.
            // We proxy by department as a grouping fallback.
            var year = fiscalYear ?? DateTime.UtcNow.Year;
            var query = _db.Reimbursements
                .Where(r => r.Status == ReimbursementStatus.Paid &&
                            r.CompletedAt.HasValue &&
                            r.CompletedAt.Value.Year == year);

            if (!string.IsNullOrEmpty(departmentId))
                query = query.Where(r => r.DepartmentId == departmentId);

            var data = await query.ToListAsync();
            var total = data.Sum(r => r.Amount);

            return data
                .GroupBy(r => r.DepartmentId)
                .Select(g => new CategorySpendDto
                {
                    Category = g.Key,
                    TotalSpend = g.Sum(r => r.Amount),
                    Count = g.Count(),
                    Percentage = total > 0 ? Math.Round((g.Sum(r => r.Amount) / total) * 100, 2) : 0
                })
                .OrderByDescending(c => c.TotalSpend)
                .ToList();
        }

        public async Task<ReimbursementSummaryReport> GetReimbursementSummaryAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Reimbursements.AsQueryable();
            if (from.HasValue) query = query.Where(r => r.RequestedAt >= from.Value);
            if (to.HasValue) query = query.Where(r => r.RequestedAt <= to.Value);

            var all = await query.ToListAsync();
            var total = all.Sum(r => r.Amount);
            var count = all.Count;

            return new ReimbursementSummaryReport
            {
                TotalClaims = count,
                TotalAmount = total,
                AverageAmount = count > 0 ? Math.Round(total / count, 2) : 0,
                PaidCount = all.Count(r => r.Status == ReimbursementStatus.Paid),
                PendingCount = all.Count(r =>
                    r.Status == ReimbursementStatus.Approved ||
                    r.Status == ReimbursementStatus.Processing),
                FailedCount = all.Count(r => r.Status == ReimbursementStatus.PaymentFailed),
                SuccessRate = count > 0
                    ? Math.Round((decimal)all.Count(r => r.Status == ReimbursementStatus.Paid) / count * 100, 2)
                    : 0
            };
        }

        public async Task<PaymentSummaryReport> GetPaymentSummaryAsync(DateTime? from, DateTime? to)
        {
            var query = _db.PaymentTransactions.AsQueryable();
            if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);

            var all = await query.ToListAsync();
            var total = all.Count;

            return new PaymentSummaryReport
            {
                TotalPayments = total,
                SuccessfulPayments = all.Count(p => p.Status == PaymentStatus.Completed),
                FailedPayments = all.Count(p => p.Status == PaymentStatus.Failed),
                TimeoutPayments = all.Count(p => p.Status == PaymentStatus.Timeout),
                TotalAmountPaid = all.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
                SuccessRate = total > 0
                    ? Math.Round((decimal)all.Count(p => p.Status == PaymentStatus.Completed) / total * 100, 2)
                    : 0
            };
        }

        public async Task<FinanceDashboardDto> GetFinanceDashboardAsync(int? fiscalYear)
        {
            var report = await GetSpendVsBudgetReportAsync(fiscalYear, null);
            var monthly = await GetMonthlySpendingAsync(fiscalYear, null);

            var alerts = await _db.BudgetAlerts
                .Where(a => a.Status == BudgetAlertStatus.Active)
                .OrderByDescending(a => a.ThresholdPercentage)
                .Take(10)
                .ToListAsync();

            return new FinanceDashboardDto
            {
                TotalBudget = report.TotalAllocated,
                TotalSpend = report.TotalSpent,
                Remaining = report.TotalRemaining,
                PendingReimbursements = report.PendingReimbursements,
                ProcessingCount = report.ProcessingCount,
                PaidCount = report.PaidCount,
                FailedPayments = report.FailedPayments,
                OverallUtilization = report.OverallUtilization,
                DepartmentBreakdown = report.DepartmentSummaries,
                MonthlyTrend = monthly,
                ActiveAlerts = alerts.Select(a => new BudgetAlertDto
                {
                    Id = a.Id,
                    DepartmentId = a.DepartmentId,
                    ThresholdPercentage = a.ThresholdPercentage,
                    CurrentSpend = a.CurrentSpend,
                    BudgetAmount = a.BudgetAmount,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }

        public async Task<List<DepartmentSpendDto>> GetDepartmentSpendingAsync(string departmentId, int? fiscalYear)
        {
            var report = await GetSpendVsBudgetReportAsync(fiscalYear, departmentId);
            return report.DepartmentSummaries;
        }
    }
}
