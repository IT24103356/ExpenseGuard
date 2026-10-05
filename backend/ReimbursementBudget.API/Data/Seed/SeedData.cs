using System;
using System.Linq;
using System.Threading.Tasks;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;

namespace ReimbursementBudget.API.Data.Seed
{
    public static class SeedData
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (db.DepartmentBudgets.Any()) return; // Already seeded

            var budgets = new[]
            {
                new DepartmentBudget { DepartmentId = "DEPT-ENG",  DepartmentName = "Engineering",    FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 10_000_000, Currency = "LKR" },
                new DepartmentBudget { DepartmentId = "DEPT-MKT",  DepartmentName = "Marketing",      FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 7_500_000,  Currency = "LKR" },
                new DepartmentBudget { DepartmentId = "DEPT-HR",   DepartmentName = "HR",             FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 5_000_000,  Currency = "LKR" },
                new DepartmentBudget { DepartmentId = "DEPT-FIN",  DepartmentName = "Finance",        FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 3_000_000,  Currency = "LKR" },
                new DepartmentBudget { DepartmentId = "DEPT-OPS",  DepartmentName = "Operations",     FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 6_000_000,  Currency = "LKR" },
            };

            db.DepartmentBudgets.AddRange(budgets);
            await db.SaveChangesAsync();
        }
    }
}
