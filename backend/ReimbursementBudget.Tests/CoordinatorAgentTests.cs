using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReimbursementBudget.API.Agents;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;
using ReimbursementBudget.API.DTOs;
using ReimbursementBudget.API.Infrastructure.Payment;
using ReimbursementBudget.API.Services;
using ReimbursementBudget.API.Tools;
using Xunit;

namespace ReimbursementBudget.Tests
{
    public class BudgetServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly BudgetService _service;

        public BudgetServiceTests()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _db = new AppDbContext(opts);
            _service = new BudgetService(_db, NullLogger<BudgetService>.Instance);
        }

        [Fact]
        public async Task CheckBudget_SufficientFunds_ReturnsTrue()
        {
            _db.DepartmentBudgets.Add(new DepartmentBudget
            {
                DepartmentId = "DEPT-ENG", DepartmentName = "Engineering",
                FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 100_000,
                ApprovedSpend = 90_000
            });
            await _db.SaveChangesAsync();

            // 100000 - 90000 = 10000 remaining, request 5000 → sufficient
            var result = await _service.CheckBudgetAvailabilityAsync("DEPT-ENG", 5_000, DateTime.UtcNow.Year);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task CheckBudget_InsufficientFunds_ReturnsFalse()
        {
            _db.DepartmentBudgets.Add(new DepartmentBudget
            {
                DepartmentId = "DEPT-MKT", DepartmentName = "Marketing",
                FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 100_000,
                ApprovedSpend = 90_000
            });
            await _db.SaveChangesAsync();

            // Remaining = 10000, request 25000 → insufficient
            var result = await _service.CheckBudgetAvailabilityAsync("DEPT-MKT", 25_000, DateTime.UtcNow.Year);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task CreateBudget_StoresBudget_Successfully()
        {
            var req = new CreateBudgetRequest
            {
                DepartmentId = "DEPT-HR", DepartmentName = "HR",
                FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 5_000_000, Currency = "LKR"
            };

            var result = await _service.CreateBudgetAsync(req);

            result.IsSuccess.Should().BeTrue();
            result.Data.AllocatedAmount.Should().Be(5_000_000);
            result.Data.RemainingBudget.Should().Be(5_000_000);
            result.Data.UtilizationPercentage.Should().Be(0);
        }

        [Fact]
        public async Task DeductBudget_CorrectlyUpdatesTotals()
        {
            var budget = new DepartmentBudget
            {
                DepartmentId = "DEPT-OPS", DepartmentName = "Operations",
                FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 500_000,
                ApprovedSpend = 100_000
            };
            _db.DepartmentBudgets.Add(budget);
            await _db.SaveChangesAsync();

            await _service.DeductBudgetAsync("DEPT-OPS", 50_000, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.Year);

            var updated = await _db.DepartmentBudgets.FindAsync(budget.Id);
            updated!.ApprovedSpend.Should().Be(150_000);
            updated.RemainingBudget.Should().Be(350_000);
        }

        [Fact]
        public async Task Budget_Alert_CreatedAt80Percent()
        {
            var budget = new DepartmentBudget
            {
                DepartmentId = "DEPT-FIN", DepartmentName = "Finance",
                FiscalYear = DateTime.UtcNow.Year, AllocatedAmount = 100_000,
                ApprovedSpend = 79_000 // just below 80%
            };
            _db.DepartmentBudgets.Add(budget);
            await _db.SaveChangesAsync();

            // Deduct 2000 to push to 81%
            await _service.DeductBudgetAsync("DEPT-FIN", 2_000, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.Year);

            var alerts = await _db.BudgetAlerts
                .Where(a => a.DepartmentId == "DEPT-FIN")
                .ToListAsync();

            alerts.Should().Contain(a => a.ThresholdPercentage == 80);
        }

        [Fact]
        public async Task UtilizationPercentage_CalculatedCorrectly()
        {
            // Deterministic: (50000/200000)*100 = 25%
            var budget = new DepartmentBudget
            {
                DepartmentId = "DEPT-TEST", DepartmentName = "Test",
                FiscalYear = DateTime.UtcNow.Year,
                AllocatedAmount = 200_000, ApprovedSpend = 50_000
            };
            _db.DepartmentBudgets.Add(budget);
            await _db.SaveChangesAsync();

            var result = await _service.GetBudgetSummaryAsync(budget.Id);
            result.Data.UtilizationPercentage.Should().Be(25);
        }

        public void Dispose() => _db.Dispose();
    }

    public class CoordinatorPlannerAgentTests : IDisposable
    {
        private readonly AppDbContext _db;

        public CoordinatorPlannerAgentTests()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _db = new AppDbContext(opts);
        }

        [Fact]
        public async Task StartWorkflow_CreatesExecution_WithCorrectSteps()
        {
            var coordinator = BuildCoordinator();
            var request = new StartWorkflowRequest
            {
                ExpenseClaimId = Guid.NewGuid(),
                EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG",
                Amount = 25_000,
                Currency = "LKR"
            };

            var result = await coordinator.StartWorkflowAsync(request);

            result.Should().NotBeNull();
            result.TotalSteps.Should().Be(9);
            result.Steps.Should().HaveCount(9);
        }

        [Fact]
        public async Task StartWorkflow_Idempotent_NoDuplicates()
        {
            var coordinator = BuildCoordinator();
            var claimId = Guid.NewGuid();
            var request = new StartWorkflowRequest
            {
                ExpenseClaimId = claimId, EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 10_000
            };

            await coordinator.StartWorkflowAsync(request);
            await Task.Delay(100); // let first workflow start
            await coordinator.StartWorkflowAsync(request); // duplicate

            var count = await _db.WorkflowExecutions
                .CountAsync(w => w.ExpenseClaimId == claimId);
            count.Should().Be(1);
        }

        [Fact]
        public async Task Approval_Rejected_TerminatesWorkflow_WithoutPayment()
        {
            var coordinator = BuildCoordinator();
            var claimId = Guid.NewGuid();
            var wf = new WorkflowExecution
            {
                WorkflowId = "WF-TEST-REJECT",
                ExpenseClaimId = claimId,
                Objective = "Test",
                Status = WorkflowStatus.WaitingForApproval,
                CurrentStep = "HUMAN_APPROVAL"
            };
            var approvalStep = new WorkflowStep
            {
                StepNumber = 4,
                StepName = "HUMAN_APPROVAL",
                StepType = "HUMAN_APPROVAL",
                Status = WorkflowStepStatus.WaitingForHuman,
                AgentName = "HUMAN_APPROVAL"
            };
            wf.Steps.Add(approvalStep);
            _db.WorkflowExecutions.Add(wf);
            await _db.SaveChangesAsync();

            var decision = new ApprovalDecisionRequest
            {
                Decision = "REJECTED",
                Comment = "Non-compliant claim",
                ApproverId = "MGR-001"
            };

            var result = await coordinator.ResumeAfterApprovalAsync(wf.Id, decision);

            result.Status.Should().Be(WorkflowStatus.Rejected);
            result.FinalOutcome.Should().Contain("REJECTED");
        }

        [Fact]
        public async Task Approval_RevisionRequired_PausesWorkflow()
        {
            var coordinator = BuildCoordinator();
            var wf = new WorkflowExecution
            {
                WorkflowId = "WF-TEST-REVISION",
                ExpenseClaimId = Guid.NewGuid(),
                Objective = "Test",
                Status = WorkflowStatus.WaitingForApproval,
                CurrentStep = "HUMAN_APPROVAL"
            };
            wf.Steps.Add(new WorkflowStep
            {
                StepNumber = 4, StepName = "HUMAN_APPROVAL",
                StepType = "HUMAN_APPROVAL",
                Status = WorkflowStepStatus.WaitingForHuman,
                AgentName = "HUMAN_APPROVAL"
            });
            _db.WorkflowExecutions.Add(wf);
            await _db.SaveChangesAsync();

            var result = await coordinator.ResumeAfterApprovalAsync(wf.Id, new ApprovalDecisionRequest
            {
                Decision = "REVISION_REQUIRED",
                Comment = "Please add receipts",
                ApproverId = "MGR-001"
            });

            result.Status.Should().Be(WorkflowStatus.RevisionRequired);
        }

        [Fact]
        public async Task Approval_NotWaiting_ThrowsException()
        {
            var coordinator = BuildCoordinator();
            var wf = new WorkflowExecution
            {
                WorkflowId = "WF-TEST-INVALID",
                ExpenseClaimId = Guid.NewGuid(),
                Objective = "Test",
                Status = WorkflowStatus.Completed // not waiting
            };
            _db.WorkflowExecutions.Add(wf);
            await _db.SaveChangesAsync();

            var act = async () => await coordinator.ResumeAfterApprovalAsync(wf.Id, new ApprovalDecisionRequest
            {
                Decision = "APPROVED", ApproverId = "MGR-001"
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        private CoordinatorPlannerAgent BuildCoordinator()
        {
            // Mock all toolbox dependencies
            var paymentMock = new Mock<IPaymentProvider>();
            var budgetMock = new Mock<IBudgetService>();
            var reimbMock = new Mock<IReimbursementService>();

            budgetMock.Setup(b => b.CheckBudgetAvailabilityAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));
            reimbMock.Setup(r => r.CreateReimbursementAsync(It.IsAny<CreateReimbursementRequest>()))
                .ReturnsAsync(ServiceResult<ReimbursementDto>.Ok(new ReimbursementDto
                {
                    Id = Guid.NewGuid(), Status = "PROCESSING", Amount = 25000, Currency = "LKR"
                }));
            reimbMock.Setup(r => r.ProcessReimbursementAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<ReimbursementDto>.Ok(new ReimbursementDto
                {
                    Id = Guid.NewGuid(), Status = "PROCESSING"
                }));
            reimbMock.Setup(r => r.SubmitPaymentAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<ReimbursementDto>.Ok(new ReimbursementDto
                {
                    Id = Guid.NewGuid(), Status = ReimbursementStatus.Paid, PaymentReference = "PAY-SIM"
                }));
            budgetMock.Setup(b => b.DeductBudgetAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var toolbox = new AgentToolbox(
                _db, paymentMock.Object, budgetMock.Object, reimbMock.Object,
                NullLogger<AgentToolbox>.Instance);

            return new CoordinatorPlannerAgent(_db, toolbox, NullLogger<CoordinatorPlannerAgent>.Instance);
        }

        public void Dispose() => _db.Dispose();
    }
}
