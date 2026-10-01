using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReimbursementBudget.API.Data.Entities
{
    public class DepartmentBudget
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string DepartmentId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        public int FiscalYear { get; set; }

        [MaxLength(20)]
        public string BudgetPeriod { get; set; } = "ANNUAL"; // ANNUAL, Q1, Q2, Q3, Q4, MONTHLY

        [Column(TypeName = "decimal(18,2)")]
        public decimal AllocatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ApprovedSpend { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidSpend { get; set; } = 0;

        [MaxLength(10)]
        public string Currency { get; set; } = "LKR";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed (not stored)
        [NotMapped]
        public decimal RemainingBudget => AllocatedAmount - ApprovedSpend;

        [NotMapped]
        public decimal UtilizationPercentage =>
            AllocatedAmount > 0 ? Math.Round((ApprovedSpend / AllocatedAmount) * 100, 2) : 0;

        // Navigation
        public ICollection<BudgetTransaction> BudgetTransactions { get; set; } = new List<BudgetTransaction>();
        public ICollection<BudgetAlert> BudgetAlerts { get; set; } = new List<BudgetAlert>();
    }
}
