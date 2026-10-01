using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReimbursementBudget.API.Data.Entities
{
    public class SpendSummary
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string DepartmentId { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Period { get; set; } = string.Empty; // e.g. "2026-01", "2026-Q1", "2026"

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpend { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BudgetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Variance { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal UtilizationPercentage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
