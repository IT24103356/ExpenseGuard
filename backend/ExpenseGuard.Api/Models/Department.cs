namespace ExpenseGuard.Api.Models;

public class Department
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<Policy> Policies { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
}