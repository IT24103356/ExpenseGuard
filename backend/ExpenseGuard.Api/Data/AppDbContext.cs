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
    public DbSet<PolicyDesignation> PolicyDesignations => Set<PolicyDesignation>();
    public DbSet<PolicyEvaluation> PolicyEvaluations => Set<PolicyEvaluation>();
    public DbSet<PolicyViolation> PolicyViolations => Set<PolicyViolation>();
    public DbSet<FraudFlag> FraudFlags => Set<FraudFlag>();
    public DbSet<FraudEvaluation> FraudEvaluations => Set<FraudEvaluation>();
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

        modelBuilder.Entity<Policy>()
            .HasIndex(p => new { p.PolicyCode, p.Version })
            .IsUnique();
        modelBuilder.Entity<Policy>()
            .HasIndex(p => new { p.IsActive, p.Category, p.Currency, p.DepartmentId, p.EffectiveFrom, p.EffectiveTo });
        modelBuilder.Entity<PolicyDesignation>()
            .HasIndex(x => new { x.PolicyId, x.Designation })
            .IsUnique();
        modelBuilder.Entity<PolicyEvaluation>()
            .HasIndex(x => new { x.ExpenseClaimId, x.InputFingerprint })
            .IsUnique();
        modelBuilder.Entity<PolicyEvaluation>()
            .HasOne(x => x.ExpenseClaim).WithMany(x => x.PolicyEvaluations)
            .HasForeignKey(x => x.ExpenseClaimId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PolicyEvaluation>()
            .HasOne(x => x.Policy).WithMany(x => x.Evaluations)
            .HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<PolicyViolation>()
            .HasOne(x => x.PolicyEvaluation).WithMany(x => x.Violations)
            .HasForeignKey(x => x.PolicyEvaluationId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Department)
            .WithMany(d => d.Budgets)
            .HasForeignKey(b => b.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FraudFlag>()
            .HasOne(f => f.ExpenseClaim)
            .WithMany(c => c.FraudFlags)
            .HasForeignKey(f => f.ExpenseClaimId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FraudFlag>()
            .HasOne(x => x.FraudEvaluation).WithMany(x => x.Flags)
            .HasForeignKey(x => x.FraudEvaluationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FraudEvaluation>()
            .HasOne(x => x.ExpenseClaim).WithMany(x => x.FraudEvaluations)
            .HasForeignKey(x => x.ExpenseClaimId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FraudEvaluation>()
            .HasIndex(x => new { x.ExpenseClaimId, x.InputFingerprint }).IsUnique();
        modelBuilder.Entity<FraudEvaluation>()
            .HasIndex(x => x.NormalizedInvoiceNumber);
        modelBuilder.Entity<FraudFlag>()
            .HasIndex(x => new { x.Status, x.Severity, x.CreatedAt });

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
        modelBuilder.Entity<Policy>().Property(p => p.PolicyCode).HasMaxLength(50);
        modelBuilder.Entity<Policy>().Property(p => p.Currency).HasMaxLength(3);
        modelBuilder.Entity<PolicyDesignation>().Property(p => p.Designation).HasMaxLength(100);
        modelBuilder.Entity<PolicyEvaluation>().Property(p => p.InputFingerprint).HasMaxLength(64);
        modelBuilder.Entity<PolicyEvaluation>().Property(p => p.Outcome).HasMaxLength(30);
        modelBuilder.Entity<PolicyViolation>().Property(p => p.RuleCode).HasMaxLength(60);
        modelBuilder.Entity<PolicyViolation>().Property(p => p.Severity).HasMaxLength(20);
        modelBuilder.Entity<FraudEvaluation>().Property(p => p.InputFingerprint).HasMaxLength(64);
        modelBuilder.Entity<FraudEvaluation>().Property(p => p.NormalizedInvoiceNumber).HasMaxLength(100);
        modelBuilder.Entity<FraudEvaluation>().Property(p => p.RiskLevel).HasMaxLength(20);
        modelBuilder.Entity<FraudFlag>().Property(p => p.RuleCode).HasMaxLength(60);
        modelBuilder.Entity<FraudFlag>().Property(p => p.Severity).HasMaxLength(20);
        modelBuilder.Entity<FraudFlag>().Property(p => p.Source).HasMaxLength(30);
        modelBuilder.Entity<FraudFlag>().Property(p => p.Status).HasMaxLength(30);
        modelBuilder.Entity<FraudFlag>().Property(p => p.EvidenceJson).HasColumnType("jsonb");
        modelBuilder.Entity<Reimbursement>().Property(r => r.Status).HasMaxLength(30);
        modelBuilder.Entity<Budget>().Property(b => b.Period).HasMaxLength(20);

        modelBuilder.Entity<Role>().Property(r => r.ApprovalLimit).HasPrecision(18, 2);
        modelBuilder.Entity<ExpenseClaim>().Property(c => c.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Policy>().Property(p => p.MinAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Policy>().Property(p => p.MaxAmount).HasPrecision(18, 2);
        modelBuilder.Entity<PolicyViolation>().Property(p => p.ExpectedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<PolicyViolation>().Property(p => p.ActualAmount).HasPrecision(18, 2);
        modelBuilder.Entity<FraudFlag>().Property(f => f.RiskScore).HasPrecision(5, 2);
        modelBuilder.Entity<FraudEvaluation>().Property(f => f.RiskScore).HasPrecision(5, 2);
        modelBuilder.Entity<FraudEvaluation>().Property(f => f.ClaimAmount).HasPrecision(18, 2);
        modelBuilder.Entity<FraudEvaluation>().Property(f => f.ReceiptAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Reimbursement>().Property(r => r.Total).HasPrecision(18, 2);
        modelBuilder.Entity<Budget>().Property(b => b.AllocatedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Budget>().Property(b => b.SpentAmount).HasPrecision(18, 2);
    }
}