namespace ExpenseGuard.Api.Models;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal ApprovalLimit { get; set; }
    public bool CanApprove { get; set; }

    public ICollection<Employee> Employees { get; set; } = [];
}