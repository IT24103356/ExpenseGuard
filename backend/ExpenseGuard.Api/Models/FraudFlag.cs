namespace ExpenseGuard.Api.Models;

public class FraudFlag
{
    public int FraudFlagId { get; set; }
    public int ExpenseClaimId { get; set; }
    public string FlagReason { get; set; } = string.Empty;
    public decimal RiskScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ExpenseClaim ExpenseClaim { get; set; } = null!;
}
