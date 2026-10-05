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
    public DbSet<BudgetTransaction> BudgetTransactions => Set<BudgetTransaction>();
    public DbSet<BudgetAlert> BudgetAlerts => Set<BudgetAlert>();

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

        modelBuilder.Entity<BudgetTransaction>()
            .HasOne(t => t.Budget)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BudgetAlert>()
            .HasOne(a => a.Budget)
            .WithMany(b => b.Alerts)
            .HasForeignKey(a => a.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<Department>().HasIndex(d => d.Code).IsUnique();
        modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.DepartmentId, b.PeriodStart, b.PeriodEnd, b.Currency })
            .IsUnique();
        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(t => new { t.BudgetId, t.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        modelBuilder.Entity<BudgetTransaction>().HasIndex(t => new { t.BudgetId, t.CreatedAt });
        modelBuilder.Entity<BudgetAlert>()
            .HasIndex(a => new { a.BudgetId, a.ThresholdPercent, a.Status });

        modelBuilder.Entity<ExpenseClaim>()
            .HasIndex(c => c.Status);

        modelBuilder.Entity<Employee>().Property(e => e.Username).HasMaxLength(100);
        modelBuilder.Entity<Role>().Property(r => r.RoleName).HasMaxLength(50);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.Category).HasMaxLength(100);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.Status).HasMaxLength(30);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.PurchaseNo).HasMaxLength(100);
        modelBuilder.Entity<Policy>().Property(p => p.Category).HasMaxLength(100);
        modelBuilder.Entity<Reimbursement>().Property(r => r.Status).HasMaxLength(30);
        modelBuilder.Entity<Department>().Property(d => d.Code).HasMaxLength(20);
        modelBuilder.Entity<Department>().Property(d => d.DepartmentName).HasMaxLength(120);
        modelBuilder.Entity<Department>().Property(d => d.Version).IsConcurrencyToken();
        modelBuilder.Entity<Budget>().Property(b => b.Name).HasMaxLength(120);
        modelBuilder.Entity<Budget>().Property(b => b.Currency).HasMaxLength(3).IsFixedLength();
        modelBuilder.Entity<Budget>().Property(b => b.Version).IsConcurrencyToken();
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.Reference).HasMaxLength(100);
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.Description).HasMaxLength(500);
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.IdempotencyKey).HasMaxLength(100);
        modelBuilder.Entity<BudgetAlert>().Property(a => a.Message).HasMaxLength(500);
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<BudgetAlert>().Property(a => a.Severity).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<BudgetAlert>().Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Role>().Property(r => r.ApprovalLimit).HasPrecision(18, 2);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Policy>().Property(p => p.MaxAmount).HasPrecision(18, 2);
        modelBuilder.Entity<FraudFlag>().Property(f => f.RiskScore).HasPrecision(5, 2);
        modelBuilder.Entity<Reimbursement>().Property(r => r.Total).HasPrecision(18, 2);
        modelBuilder.Entity<Budget>().Property(b => b.AllocatedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Budget>().Property(b => b.ReservedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Budget>().Property(b => b.SpentAmount).HasPrecision(18, 2);
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.AllocatedBalance).HasPrecision(18, 2);
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.ReservedBalance).HasPrecision(18, 2);
        modelBuilder.Entity<BudgetTransaction>().Property(t => t.SpentBalance).HasPrecision(18, 2);
        modelBuilder.Entity<BudgetAlert>().Property(a => a.ThresholdPercent).HasPrecision(5, 2);
        modelBuilder.Entity<BudgetAlert>().Property(a => a.UtilizationPercent).HasPrecision(7, 2);

        modelBuilder.Entity<Budget>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Budgets_Dates", "\"PeriodEnd\" >= \"PeriodStart\"");
            t.HasCheckConstraint("CK_Budgets_Balances", "\"AllocatedAmount\" >= 0 AND \"ReservedAmount\" >= 0 AND \"SpentAmount\" >= 0 AND \"ReservedAmount\" + \"SpentAmount\" <= \"AllocatedAmount\"");
        });
        modelBuilder.Entity<BudgetTransaction>().ToTable(t =>
            t.HasCheckConstraint("CK_BudgetTransactions_Amount", "\"Amount\" > 0"));
    }
}