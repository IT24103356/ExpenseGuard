using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;
using ReimbursementBudget.API.DTOs;

namespace ReimbursementBudget.API.Services
{
    public interface IReimbursementService
    {
        Task<ServiceResult<ReimbursementDto>> CreateReimbursementAsync(CreateReimbursementRequest request);
        Task<ServiceResult<ReimbursementDto>> GetReimbursementAsync(Guid id);
        Task<ServiceResult<PagedResult<ReimbursementDto>>> GetFinanceQueueAsync(FinanceQueueFilter filter);
        Task<ServiceResult<ReimbursementDto>> ProcessReimbursementAsync(Guid id, string financeUserId);
        Task<ServiceResult<ReimbursementDto>> SubmitPaymentAsync(Guid id, string financeUserId);
        Task<ServiceResult<ReimbursementDto>> GetByClaimIdAsync(Guid claimId);
        Task<ServiceResult<List<ReimbursementDto>>> GetEmployeeReimbursementsAsync(string employeeId);
    }

    public class ReimbursementService : IReimbursementService
    {
        private readonly AppDbContext _db;
        private readonly IBudgetService _budgetService;
        private readonly Infrastructure.Payment.IPaymentProvider _paymentProvider;
        private readonly ILogger<ReimbursementService> _logger;

        public ReimbursementService(
            AppDbContext db,
            IBudgetService budgetService,
            Infrastructure.Payment.IPaymentProvider paymentProvider,
            ILogger<ReimbursementService> logger)
        {
            _db = db;
            _budgetService = budgetService;
            _paymentProvider = paymentProvider;
            _logger = logger;
        }

        public async Task<ServiceResult<ReimbursementDto>> CreateReimbursementAsync(CreateReimbursementRequest request)
        {
            // Idempotency: one reimbursement per claim
            var existing = await _db.Reimbursements
                .FirstOrDefaultAsync(r => r.ExpenseClaimId == request.ExpenseClaimId);

            if (existing != null)
                return ServiceResult<ReimbursementDto>.Ok(MapToDto(existing));

            var reimbursement = new Reimbursement
            {
                ExpenseClaimId = request.ExpenseClaimId,
                EmployeeId = request.EmployeeId,
                DepartmentId = request.DepartmentId,
                Amount = request.Amount,
                Currency = request.Currency ?? "LKR",
                Status = ReimbursementStatus.Approved,
                IdempotencyKey = $"REIMB-{request.ExpenseClaimId}",
                RequestedAt = DateTime.UtcNow
            };

            _db.Reimbursements.Add(reimbursement);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created Reimbursement {Id} for Claim {ClaimId}", reimbursement.Id, request.ExpenseClaimId);
            return ServiceResult<ReimbursementDto>.Ok(MapToDto(reimbursement));
        }

        public async Task<ServiceResult<ReimbursementDto>> GetReimbursementAsync(Guid id)
        {
            var r = await _db.Reimbursements
                .Include(x => x.PaymentTransactions)
                .Include(x => x.BudgetTransactions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null) return ServiceResult<ReimbursementDto>.Fail("Reimbursement not found");
            return ServiceResult<ReimbursementDto>.Ok(MapToDto(r));
        }

        public async Task<ServiceResult<PagedResult<ReimbursementDto>>> GetFinanceQueueAsync(FinanceQueueFilter filter)
        {
            var query = _db.Reimbursements.AsQueryable();

            // Filtering
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(r => r.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.DepartmentId))
                query = query.Where(r => r.DepartmentId == filter.DepartmentId);
            if (filter.FromDate.HasValue)
                query = query.Where(r => r.RequestedAt >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(r => r.RequestedAt <= filter.ToDate.Value);
            if (filter.MinAmount.HasValue)
                query = query.Where(r => r.Amount >= filter.MinAmount.Value);
            if (filter.MaxAmount.HasValue)
                query = query.Where(r => r.Amount <= filter.MaxAmount.Value);
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(r => r.EmployeeId.Contains(filter.SearchTerm) ||
                                         r.DepartmentId.Contains(filter.SearchTerm));

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "amount" => filter.SortDesc ? query.OrderByDescending(r => r.Amount) : query.OrderBy(r => r.Amount),
                "status" => filter.SortDesc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                _ => filter.SortDesc ? query.OrderByDescending(r => r.RequestedAt) : query.OrderBy(r => r.RequestedAt)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ServiceResult<PagedResult<ReimbursementDto>>.Ok(new PagedResult<ReimbursementDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ServiceResult<ReimbursementDto>> ProcessReimbursementAsync(Guid id, string financeUserId)
        {
            var r = await _db.Reimbursements.FindAsync(id);
            if (r == null) return ServiceResult<ReimbursementDto>.Fail("Not found");
            if (r.Status != ReimbursementStatus.ReadyForFinance && r.Status != ReimbursementStatus.Approved)
                return ServiceResult<ReimbursementDto>.Fail($"Cannot process reimbursement in status {r.Status}");

            // Budget check
            var budgetCheck = await _budgetService.CheckBudgetAvailabilityAsync(r.DepartmentId, r.Amount, DateTime.UtcNow.Year);
            if (!budgetCheck.IsSuccess || !budgetCheck.Data)
            {
                r.Status = ReimbursementStatus.BudgetReviewRequired;
                r.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return ServiceResult<ReimbursementDto>.Fail("Insufficient budget. Status set to BUDGET_REVIEW_REQUIRED.");
            }

            r.Status = ReimbursementStatus.Processing;
            r.ProcessedAt = DateTime.UtcNow;
            r.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ServiceResult<ReimbursementDto>.Ok(MapToDto(r));
        }

        public async Task<ServiceResult<ReimbursementDto>> SubmitPaymentAsync(Guid id, string financeUserId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var r = await _db.Reimbursements.FindAsync(id);
                if (r == null) return ServiceResult<ReimbursementDto>.Fail("Not found");
                if (r.Status != ReimbursementStatus.Processing)
                    return ServiceResult<ReimbursementDto>.Fail($"Cannot submit payment in status {r.Status}");

                // Idempotency guard: check if a payment already exists
                var existingPayment = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.ReimbursementId == r.Id &&
                                               pt.Status == PaymentStatus.Completed);
                if (existingPayment != null)
                {
                    _logger.LogWarning("Duplicate payment attempt blocked for Reimbursement {Id}", id);
                    return ServiceResult<ReimbursementDto>.Fail("Payment already completed. Duplicate prevented.");
                }

                r.Status = ReimbursementStatus.PaymentPending;
                r.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var paymentReq = new Infrastructure.Payment.PaymentRequest(
                    IdempotencyKey: r.IdempotencyKey ?? $"PAY-{r.Id}",
                    ReimbursementId: r.Id.ToString(),
                    EmployeeId: r.EmployeeId,
                    DepartmentId: r.DepartmentId,
                    Amount: r.Amount,
                    Currency: r.Currency,
                    Description: $"Reimbursement for expense claim {r.ExpenseClaimId}"
                );

                var paymentResp = await _paymentProvider.CreatePaymentAsync(paymentReq);

                var paymentTxn = new PaymentTransaction
                {
                    ReimbursementId = r.Id,
                    ExternalTransactionId = paymentResp.TransactionId,
                    Amount = r.Amount,
                    Currency = r.Currency,
                    Status = paymentResp.Status,
                    RequestPayloadSummary = JsonSerializer.Serialize(paymentReq),
                    ResponseSummary = paymentResp.RawResponse,
                    FailureReason = paymentResp.FailureReason
                };
                _db.PaymentTransactions.Add(paymentTxn);

                if (paymentResp.Success && paymentResp.Status == "COMPLETED")
                {
                    r.Status = ReimbursementStatus.Paid;
                    r.PaymentReference = paymentResp.TransactionId;
                    r.PaymentProvider = "SandboxProvider";
                    r.CompletedAt = DateTime.UtcNow;

                    // Update budget
                    await _budgetService.DeductBudgetAsync(r.DepartmentId, r.Amount, r.Id, r.ExpenseClaimId, DateTime.UtcNow.Year);
                }
                else
                {
                    r.Status = ReimbursementStatus.PaymentFailed;
                    r.FailureReason = paymentResp.FailureReason ?? paymentResp.Status;
                    r.RetryCount += 1;
                }

                r.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Payment {Status} for Reimbursement {Id} TxnId={TxnId}",
                    r.Status, id, paymentResp.TransactionId);

                return ServiceResult<ReimbursementDto>.Ok(MapToDto(r));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Payment submission failed for Reimbursement {Id}", id);
                return ServiceResult<ReimbursementDto>.Fail($"Payment submission error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ReimbursementDto>> GetByClaimIdAsync(Guid claimId)
        {
            var r = await _db.Reimbursements
                .Include(x => x.PaymentTransactions)
                .FirstOrDefaultAsync(x => x.ExpenseClaimId == claimId);
            if (r == null) return ServiceResult<ReimbursementDto>.Fail("Not found");
            return ServiceResult<ReimbursementDto>.Ok(MapToDto(r));
        }

        public async Task<ServiceResult<List<ReimbursementDto>>> GetEmployeeReimbursementsAsync(string employeeId)
        {
            var results = await _db.Reimbursements
                .Include(x => x.PaymentTransactions)
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
            return ServiceResult<List<ReimbursementDto>>.Ok(results.Select(MapToDto).ToList());
        }

        private static ReimbursementDto MapToDto(Reimbursement r) => new()
        {
            Id = r.Id,
            ExpenseClaimId = r.ExpenseClaimId,
            EmployeeId = r.EmployeeId,
            DepartmentId = r.DepartmentId,
            Amount = r.Amount,
            Currency = r.Currency,
            Status = r.Status,
            PaymentReference = r.PaymentReference,
            PaymentProvider = r.PaymentProvider,
            RequestedAt = r.RequestedAt,
            ProcessedAt = r.ProcessedAt,
            CompletedAt = r.CompletedAt,
            FailureReason = r.FailureReason,
            RetryCount = r.RetryCount,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
