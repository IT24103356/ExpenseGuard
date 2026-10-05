using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReimbursementBudget.API.Data.Entities
{
    public class BudgetAlert
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string DepartmentId { get; set; } = string.Empty;

        public Guid DepartmentBudgetId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ThresholdPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentSpend { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BudgetAmount { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = BudgetAlertStatus.Active; // ACTIVE, ACKNOWLEDGED, RESOLVED

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public DepartmentBudget DepartmentBudget { get; set; } = null!;
    }

    public static class BudgetAlertStatus
    {
        public const string Active = "ACTIVE";
        public const string Acknowledged = "ACKNOWLEDGED";
        public const string Resolved = "RESOLVED";
    }
}
