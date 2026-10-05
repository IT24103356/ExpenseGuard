using Microsoft.EntityFrameworkCore;
using ReimbursementBudget.API.Data.Entities;

namespace ReimbursementBudget.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Reimbursement> Reimbursements => Set<Reimbursement>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<DepartmentBudget> DepartmentBudgets => Set<DepartmentBudget>();
        public DbSet<BudgetTransaction> BudgetTransactions => Set<BudgetTransaction>();
        public DbSet<BudgetAlert> BudgetAlerts => Set<BudgetAlert>();
        public DbSet<SpendSummary> SpendSummaries => Set<SpendSummary>();
        public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();
        public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Reimbursement
            modelBuilder.Entity<Reimbursement>(e =>
            {
                e.HasIndex(r => r.ExpenseClaimId).IsUnique();
                e.HasIndex(r => r.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
                e.HasIndex(r => r.Status);
                e.HasIndex(r => r.DepartmentId);
                e.Property(r => r.Amount).HasPrecision(18, 2);
            });

            // PaymentTransaction
            modelBuilder.Entity<PaymentTransaction>(e =>
            {
                e.HasOne(pt => pt.Reimbursement)
                 .WithMany(r => r.PaymentTransactions)
                 .HasForeignKey(pt => pt.ReimbursementId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(pt => pt.ExternalTransactionId);
            });

            // DepartmentBudget
            modelBuilder.Entity<DepartmentBudget>(e =>
            {
                e.HasIndex(b => new { b.DepartmentId, b.FiscalYear, b.BudgetPeriod }).IsUnique();
            });

            // BudgetTransaction
            modelBuilder.Entity<BudgetTransaction>(e =>
            {
                e.HasOne(bt => bt.DepartmentBudget)
                 .WithMany(b => b.BudgetTransactions)
                 .HasForeignKey(bt => bt.DepartmentBudgetId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(bt => bt.Reimbursement)
                 .WithMany(r => r.BudgetTransactions)
                 .HasForeignKey(bt => bt.ReimbursementId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(bt => bt.DepartmentBudgetId);
                e.HasIndex(bt => bt.TransactionDate);
            });

            // BudgetAlert
            modelBuilder.Entity<BudgetAlert>(e =>
            {
                e.HasOne(ba => ba.DepartmentBudget)
                 .WithMany(b => b.BudgetAlerts)
                 .HasForeignKey(ba => ba.DepartmentBudgetId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // SpendSummary
            modelBuilder.Entity<SpendSummary>(e =>
            {
                e.HasIndex(ss => new { ss.DepartmentId, ss.Period, ss.Category }).IsUnique();
            });

            // WorkflowExecution
            modelBuilder.Entity<WorkflowExecution>(e =>
            {
                e.HasIndex(w => w.WorkflowId).IsUnique();
                e.HasIndex(w => w.ExpenseClaimId).IsUnique();
                e.HasIndex(w => w.CorrelationId);
                e.HasIndex(w => w.Status);
            });

            // WorkflowStep
            modelBuilder.Entity<WorkflowStep>(e =>
            {
                e.HasOne(ws => ws.WorkflowExecution)
                 .WithMany(we => we.Steps)
                 .HasForeignKey(ws => ws.WorkflowExecutionId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(ws => new { ws.WorkflowExecutionId, ws.StepNumber }).IsUnique();
            });
        }
    }
}
