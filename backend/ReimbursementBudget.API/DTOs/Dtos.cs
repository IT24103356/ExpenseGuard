using System;
using System.Collections.Generic;

namespace ReimbursementBudget.API.DTOs
{
    // ─── Shared ─────────────────────────────────────────────────────────────────
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; } = default!;
        public string? ErrorMessage { get; private set; }

        public static ServiceResult<T> Ok(T data) =>
            new() { IsSuccess = true, Data = data };

        public static ServiceResult<T> Fail(string error) =>
            new() { IsSuccess = false, ErrorMessage = error };
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    // ─── Reimbursement DTOs ──────────────────────────────────────────────────────
    public class ReimbursementDto
    {
        public Guid Id { get; set; }
        public Guid ExpenseClaimId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "LKR";
        public string Status { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string? PaymentProvider { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? FailureReason { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateReimbursementRequest
    {
        public Guid ExpenseClaimId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
    }

    public class FinanceQueueFilter
    {
        public string? Status { get; set; }
        public string? DepartmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ─── Budget DTOs ─────────────────────────────────────────────────────────────
    public class BudgetDto
    {
        public Guid Id { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public string BudgetPeriod { get; set; } = "ANNUAL";
        public decimal AllocatedAmount { get; set; }
        public decimal ApprovedSpend { get; set; }
        public decimal PaidSpend { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string Currency { get; set; } = "LKR";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class BudgetSummaryDto
    {
        public Guid BudgetId { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal ApprovedSpend { get; set; }
        public decimal PaidSpend { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string Currency { get; set; } = "LKR";
        public int ActiveAlerts { get; set; }
    }

    public class BudgetTransactionDto
    {
        public Guid Id { get; set; }
        public Guid DepartmentBudgetId { get; set; }
        public Guid? ReimbursementId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class CreateBudgetRequest
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public string? BudgetPeriod { get; set; }
        public decimal AllocatedAmount { get; set; }
        public string? Currency { get; set; }
    }

    public class UpdateBudgetRequest
    {
        public decimal AllocatedAmount { get; set; }
    }

    // ─── Reporting DTOs ──────────────────────────────────────────────────────────
    public class SpendVsBudgetReport
    {
        public List<DepartmentSpendDto> DepartmentSummaries { get; set; } = new();
        public decimal TotalAllocated { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal OverallUtilization { get; set; }
        public int PendingReimbursements { get; set; }
        public int ProcessingCount { get; set; }
        public int PaidCount { get; set; }
        public int FailedPayments { get; set; }
        public string Period { get; set; } = string.Empty;
    }

    public class DepartmentSpendDto
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public decimal AllocatedBudget { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public int ReimbursementCount { get; set; }
    }

    public class MonthlySpendDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal TotalSpend { get; set; }
        public int ReimbursementCount { get; set; }
        public decimal AverageReimbursement { get; set; }
    }

    public class CategorySpendDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalSpend { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class ReimbursementSummaryReport
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class PaymentSummaryReport
    {
        public int TotalPayments { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int TimeoutPayments { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal SuccessRate { get; set; }
    }

    // ─── Workflow DTOs ───────────────────────────────────────────────────────────
    public class WorkflowExecutionDto
    {
        public Guid Id { get; set; }
        public string WorkflowId { get; set; } = string.Empty;
        public Guid ExpenseClaimId { get; set; }
        public string Objective { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CurrentStep { get; set; } = string.Empty;
        public string? ApprovalStatus { get; set; }
        public string? FinalOutcome { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<WorkflowStepDto> Steps { get; set; } = new();
    }

    public class WorkflowStepDto
    {
        public Guid Id { get; set; }
        public int StepNumber { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? StepType { get; set; }
        public string? ValidationResult { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class StartWorkflowRequest
    {
        public Guid ExpenseClaimId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Category { get; set; }
    }

    public class ApprovalDecisionRequest
    {
        public string Decision { get; set; } = string.Empty; // APPROVED | REJECTED | REVISION_REQUIRED
        public string? Comment { get; set; }
        public string ApproverId { get; set; } = string.Empty;
    }

    public class FinanceDashboardDto
    {
        public decimal TotalBudget { get; set; }
        public decimal TotalSpend { get; set; }
        public decimal Remaining { get; set; }
        public int PendingReimbursements { get; set; }
        public int ProcessingCount { get; set; }
        public int PaidCount { get; set; }
        public int FailedPayments { get; set; }
        public decimal OverallUtilization { get; set; }
        public List<DepartmentSpendDto> DepartmentBreakdown { get; set; } = new();
        public List<MonthlySpendDto> MonthlyTrend { get; set; } = new();
        public List<BudgetAlertDto> ActiveAlerts { get; set; } = new();
    }

    public class BudgetAlertDto
    {
        public Guid Id { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
        public decimal ThresholdPercentage { get; set; }
        public decimal CurrentSpend { get; set; }
        public decimal BudgetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
