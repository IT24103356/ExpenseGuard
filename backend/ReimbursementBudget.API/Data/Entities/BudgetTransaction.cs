using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReimbursementBudget.API.Data.Entities
{
    public class BudgetTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DepartmentBudgetId { get; set; }

        public Guid? ExpenseClaimId { get; set; }

        public Guid? ReimbursementId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string TransactionType { get; set; } = BudgetTransactionType.Debit; // DEBIT, CREDIT, HOLD, RELEASE

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public DepartmentBudget DepartmentBudget { get; set; } = null!;
        public Reimbursement? Reimbursement { get; set; }
    }

    public static class BudgetTransactionType
    {
        public const string Debit = "DEBIT";       // spend committed
        public const string Credit = "CREDIT";      // budget added or reversed
        public const string Hold = "HOLD";          // pending approval
        public const string Release = "RELEASE";    // hold released
    }
}
