namespace ExpenseGuard.Api.Models;

public class Reimbursement
{
    public int ReimbursementId { get; set; }
    public int ExpenseClaimId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ExpenseClaim ExpenseClaim { get; set; } = null!;
}