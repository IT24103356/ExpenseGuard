namespace ExpenseGuard.Api.Models;

public class Policy
{
    public int PolicyId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
    public bool ReceiptRequired { get; set; }
    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }
}
