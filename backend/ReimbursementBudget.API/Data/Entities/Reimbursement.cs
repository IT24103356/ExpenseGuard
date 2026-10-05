using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReimbursementBudget.API.Data.Entities
{
    public class Reimbursement
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ExpenseClaimId { get; set; }

        [Required]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        public string DepartmentId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "LKR";

        [MaxLength(50)]
        public string Status { get; set; } = ReimbursementStatus.Approved;

        [MaxLength(100)]
        public string? PaymentReference { get; set; }

        [MaxLength(100)]
        public string? PaymentProvider { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string? FailureReason { get; set; }

        [MaxLength(500)]
        public string? IdempotencyKey { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
        public ICollection<BudgetTransaction> BudgetTransactions { get; set; } = new List<BudgetTransaction>();
    }

    public static class ReimbursementStatus
    {
        public const string Approved = "APPROVED";
        public const string ReadyForFinance = "READY_FOR_FINANCE";
        public const string Processing = "PROCESSING";
        public const string PaymentPending = "PAYMENT_PENDING";
        public const string Paid = "PAID";
        public const string PaymentFailed = "PAYMENT_FAILED";
        public const string OnHold = "ON_HOLD";
        public const string BudgetReviewRequired = "BUDGET_REVIEW_REQUIRED";
        public const string Rejected = "REJECTED";
    }
}
