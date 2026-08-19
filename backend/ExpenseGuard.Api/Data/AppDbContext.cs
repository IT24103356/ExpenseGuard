using Microsoft.EntityFrameworkCore;
using ExpenseGuard.Api.Models;

namespace ExpenseGuard.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<FraudFlag> FraudFlags => Set<FraudFlag>();
    public DbSet<Reimbursement> Reimbursements => Set<Reimbursement>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Username)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Role)
            .WithMany(r => r.Employees)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExpenseClaim>()
            .HasOne(c => c.Employee)
            .WithMany(e => e.ExpenseClaims)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Policy>()
            .HasOne(p => p.Department)
            .WithMany(d => d.Policies)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Department)
            .WithMany(d => d.Budgets)
            .HasForeignKey(b => b.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FraudFlag>()
            .HasOne(f => f.ExpenseClaim)
            .WithMany(c => c.FraudFlags)
            .HasForeignKey(f => f.ExpenseClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Reimbursement>()
            .HasOne(r => r.ExpenseClaim)
            .WithOne(c => c.Reimbursement)
            .HasForeignKey<Reimbursement>(r => r.ExpenseClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.DepartmentId, b.Period })
            .IsUnique();

        modelBuilder.Entity<ExpenseClaim>()
            .HasIndex(c => c.Status);

        modelBuilder.Entity<Employee>().Property(e => e.Username).HasMaxLength(100);
        modelBuilder.Entity<Role>().Property(r => r.RoleName).HasMaxLength(50);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.Category).HasMaxLength(100);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.Status).HasMaxLength(30);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.PurchaseNo).HasMaxLength(100);
        modelBuilder.Entity<Policy>().Property(p => p.Category).HasMaxLength(100);
        modelBuilder.Entity<Reimbursement>().Property(r => r.Status).HasMaxLength(30);
        modelBuilder.Entity<Budget>().Property(b => b.Period).HasMaxLength(20);

        modelBuilder.Entity<Role>().Property(r => r.ApprovalLimit).HasPrecision(18, 2);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Policy>().Property(p => p.MaxAmount).HasPrecision(18, 2);
        modelBuilder.Entity<FraudFlag>().Property(f => f.RiskScore).HasPrecision(5, 2);
        modelBuilder.Entity<Reimbursement>().Property(r => r.Total).HasPrecision(18, 2);
        modelBuilder.Entity<Budget>().Property(b => b.AllocatedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Budget>().Property(b => b.SpentAmount).HasPrecision(18, 2);
    }
}