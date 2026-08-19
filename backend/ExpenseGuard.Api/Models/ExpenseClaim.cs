namespace ExpenseGuard.Api.Models;

public class ExpenseClaim
{
    public int ExpenseClaimId { get; set; }
    public int EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string PurchaseNo { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public string? ReceiptImg { get; set; }
    public string? ReceiptDoc { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public ICollection<FraudFlag> FraudFlags { get; set; } = [];
    public Reimbursement? Reimbursement { get; set; }
}