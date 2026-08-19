namespace ExpenseGuard.Api.Models;

public class Budget
{
    public int BudgetId { get; set; }
    public int DepartmentId { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }

    public Department Department { get; set; } = null!;
}