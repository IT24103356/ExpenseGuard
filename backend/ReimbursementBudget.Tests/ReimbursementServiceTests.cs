using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;
using ReimbursementBudget.API.DTOs;
using ReimbursementBudget.API.Infrastructure.Payment;
using ReimbursementBudget.API.Services;
using Xunit;

namespace ReimbursementBudget.Tests
{
    public class ReimbursementServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly Mock<IPaymentProvider> _paymentMock;
        private readonly Mock<IBudgetService> _budgetMock;
        private readonly ReimbursementService _service;

        public ReimbursementServiceTests()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _db = new AppDbContext(opts);
            _paymentMock = new Mock<IPaymentProvider>();
            _budgetMock = new Mock<IBudgetService>();

            _service = new ReimbursementService(
                _db, _budgetMock.Object, _paymentMock.Object,
                NullLogger<ReimbursementService>.Instance);
        }

        // ── Test 1: Approved claim enters finance queue ───────────────────────
        [Fact]
        public async Task ApprovedClaim_CreatesReimbursement_AndStatusIsApproved()
        {
            var req = new CreateReimbursementRequest
            {
                ExpenseClaimId = Guid.NewGuid(),
                EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG",
                Amount = 25_000,
                Currency = "LKR"
            };

            var result = await _service.CreateReimbursementAsync(req);

            result.IsSuccess.Should().BeTrue();
            result.Data.Status.Should().Be(ReimbursementStatus.Approved);
            result.Data.Amount.Should().Be(25_000);
        }

        // ── Test 2: Duplicate claim does not create second reimbursement ──────
        [Fact]
        public async Task DuplicateClaimId_ReturnsExisting_NotDuplicate()
        {
            var claimId = Guid.NewGuid();
            var req = new CreateReimbursementRequest
            {
                ExpenseClaimId = claimId, EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 10_000
            };

            await _service.CreateReimbursementAsync(req);
            var second = await _service.CreateReimbursementAsync(req);

            second.IsSuccess.Should().BeTrue();
            _db.Reimbursements.CountAsync().Result.Should().Be(1);
        }

        // ── Test 3: Insufficient budget blocks processing ─────────────────────
        [Fact]
        public async Task InsufficientBudget_SetsStatus_BudgetReviewRequired()
        {
            var claimId = Guid.NewGuid();
            var r = new Reimbursement
            {
                ExpenseClaimId = claimId, EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 25_000,
                Status = ReimbursementStatus.Approved
            };
            _db.Reimbursements.Add(r);
            await _db.SaveChangesAsync();

            _budgetMock
                .Setup(b => b.CheckBudgetAvailabilityAsync("DEPT-ENG", 25_000, It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(false)); // insufficient

            var result = await _service.ProcessReimbursementAsync(r.Id, "FINANCE-001");

            result.IsSuccess.Should().BeFalse();
            var updated = await _db.Reimbursements.FindAsync(r.Id);
            updated!.Status.Should().Be(ReimbursementStatus.BudgetReviewRequired);
        }

        // ── Test 4: Sufficient budget allows processing ───────────────────────
        [Fact]
        public async Task SufficientBudget_AllowsProcessing()
        {
            var claimId = Guid.NewGuid();
            var r = new Reimbursement
            {
                ExpenseClaimId = claimId, EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 25_000,
                Status = ReimbursementStatus.Approved
            };
            _db.Reimbursements.Add(r);
            await _db.SaveChangesAsync();

            _budgetMock
                .Setup(b => b.CheckBudgetAvailabilityAsync("DEPT-ENG", 25_000, It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true)); // sufficient

            var result = await _service.ProcessReimbursementAsync(r.Id, "FINANCE-001");

            result.IsSuccess.Should().BeTrue();
            result.Data.Status.Should().Be(ReimbursementStatus.Processing);
        }

        // ── Test 5: Successful payment updates status to PAID ─────────────────
        [Fact]
        public async Task SuccessfulPayment_UpdatesStatus_ToPaid()
        {
            var r = new Reimbursement
            {
                ExpenseClaimId = Guid.NewGuid(), EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 25_000,
                Status = ReimbursementStatus.Processing,
                IdempotencyKey = "key-001"
            };
            _db.Reimbursements.Add(r);
            await _db.SaveChangesAsync();

            _paymentMock
                .Setup(p => p.CreatePaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(new PaymentResponse(true, "PAY-99999", "COMPLETED", 25_000, "LKR", null, "{}"));
            _budgetMock
                .Setup(b => b.DeductBudgetAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var result = await _service.SubmitPaymentAsync(r.Id, "FINANCE-001");

            result.IsSuccess.Should().BeTrue();
            result.Data.Status.Should().Be(ReimbursementStatus.Paid);
            result.Data.PaymentReference.Should().Be("PAY-99999");
        }

        // ── Test 6: Failed payment updates failure status ─────────────────────
        [Fact]
        public async Task FailedPayment_UpdatesStatus_ToPaymentFailed()
        {
            var r = new Reimbursement
            {
                ExpenseClaimId = Guid.NewGuid(), EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 25_000,
                Status = ReimbursementStatus.Processing,
                IdempotencyKey = "key-002"
            };
            _db.Reimbursements.Add(r);
            await _db.SaveChangesAsync();

            _paymentMock
                .Setup(p => p.CreatePaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(new PaymentResponse(false, "PAY-ERR", "FAILED", 25_000, "LKR", "Sandbox failure", "{}"));

            var result = await _service.SubmitPaymentAsync(r.Id, "FINANCE-001");

            result.IsSuccess.Should().BeTrue();
            result.Data.Status.Should().Be(ReimbursementStatus.PaymentFailed);
        }

        // ── Test 7: Duplicate payment is prevented ────────────────────────────
        [Fact]
        public async Task DuplicatePayment_IsBlocked()
        {
            var r = new Reimbursement
            {
                ExpenseClaimId = Guid.NewGuid(), EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 10_000,
                Status = ReimbursementStatus.Processing
            };
            _db.Reimbursements.Add(r);
            _db.PaymentTransactions.Add(new PaymentTransaction
            {
                ReimbursementId = r.Id,
                Status = PaymentStatus.Completed,
                Amount = 10_000,
                Currency = "LKR"
            });
            await _db.SaveChangesAsync();

            var result = await _service.SubmitPaymentAsync(r.Id, "FINANCE-001");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Duplicate");
        }

        // ── Test 8: Rejected claim cannot enter finance queue ─────────────────
        [Fact]
        public async Task RejectedClaim_CannotBeProcessed()
        {
            var r = new Reimbursement
            {
                ExpenseClaimId = Guid.NewGuid(), EmployeeId = "EMP-001",
                DepartmentId = "DEPT-ENG", Amount = 10_000,
                Status = ReimbursementStatus.Rejected
            };
            _db.Reimbursements.Add(r);
            await _db.SaveChangesAsync();

            _budgetMock
                .Setup(b => b.CheckBudgetAvailabilityAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var result = await _service.ProcessReimbursementAsync(r.Id, "FINANCE-001");

            result.IsSuccess.Should().BeFalse();
        }

        public void Dispose() => _db.Dispose();
    }
}
