using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReimbursementBudget.API.Data.Entities
{
    public class PaymentTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReimbursementId { get; set; }

        [MaxLength(200)]
        public string? ExternalTransactionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "LKR";

        [MaxLength(50)]
        public string Status { get; set; } = PaymentStatus.Pending;

        public string? RequestPayloadSummary { get; set; }
        public string? ResponseSummary { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Reimbursement Reimbursement { get; set; } = null!;
    }

    public static class PaymentStatus
    {
        public const string Pending = "PENDING";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
        public const string Timeout = "TIMEOUT";
        public const string Cancelled = "CANCELLED";
    }
}
