using System.Text;
using ExpenseGuard.Api.Contracts;
using ExpenseGuard.Api.Data;
using ExpenseGuard.Api.Models;
using ExpenseGuard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseGuard.Api.Tests;

public sealed class ExpenseWorkflowTests
{
    private static AppDbContext Db()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static ClaimWriteDto ClaimInput(int? requestId = null, ClaimFlow flow = ClaimFlow.OutOfPocket)
        => new() { Amount = 25, Category = "Meals", Description = "Team lunch", Currency = "usd",
            PurchaseRequestId = requestId, Flow = flow };

    [Fact]
    public async Task Claim_access_enforces_ownership()
    {
        await using var db = Db();
        db.ExpenseClaims.Add(new ExpenseClaim { ExpenseClaimId = 10, EmployeeId = 1 });
        await db.SaveChangesAsync();
        var service = new ClaimService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync(10, 2, default));
    }

    [Fact]
    public async Task Submission_requires_receipt_and_records_transition()
    {
        await using var db = Db();
        db.ExpenseClaims.Add(new ExpenseClaim { ExpenseClaimId = 10, EmployeeId = 1 });
        await db.SaveChangesAsync();
        var service = new ClaimService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransitionAsync(10, 1, ClaimStatus.Submitted, null, default));

        db.Receipts.Add(new Receipt { ExpenseClaimId = 10, StorageUrl = "x", PublicId = "x",
            FileName = "x.pdf", ContentType = "application/pdf", Sha256 = "abc" });
        await db.SaveChangesAsync();
        await service.TransitionAsync(10, 1, ClaimStatus.Submitted, "ready", default);

        Assert.Equal(ClaimStatus.Submitted, db.ExpenseClaims.Single().Status);
        Assert.Equal(ClaimStatus.Submitted, db.ClaimStatusHistories.Single().ToStatus);
    }

    [Fact]
    public async Task Prepurchase_claim_requires_owned_approved_request()
    {
        await using var db = Db();
        db.PurchaseRequests.Add(new PurchaseRequest
        {
            PurchaseRequestId = 7, EmployeeId = 1, Description = "Laptop",
            EstimatedAmount = 100, Status = PurchaseRequestStatus.Approved
        });
        await db.SaveChangesAsync();
        var service = new ClaimService(db);

        var created = await service.CreateAsync(ClaimInput(7, ClaimFlow.PrePurchase), 1, default);
        Assert.Equal(7, created.PurchaseRequestId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(ClaimInput(7, ClaimFlow.PrePurchase), 2, default));
    }

    [Fact]
    public async Task Receipt_upload_validates_type_and_deduplicates_hash()
    {
        await using var db = Db();
        db.ExpenseClaims.Add(new ExpenseClaim { ExpenseClaimId = 10, EmployeeId = 1 });
        await db.SaveChangesAsync();
        var service = new ReceiptService(db, new FakeReceiptStorage(), new FakeReceiptOcr());
        var bytes = Encoding.UTF8.GetBytes("receipt");

        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAsync(
            10, new MemoryStream(bytes), "receipt.txt", "text/plain", bytes.Length, 1, default));
        var receipt = await service.UploadAsync(
            10, new MemoryStream(bytes), "receipt.pdf", "application/pdf", bytes.Length, 1, default);
        Assert.Equal(64, receipt.Sha256.Length);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(
            10, new MemoryStream(bytes), "copy.pdf", "application/pdf", bytes.Length, 1, default));
    }

    [Fact]
    public async Task Ocr_correction_clears_manual_review()
    {
        await using var db = Db();
        db.ExpenseClaims.Add(new ExpenseClaim { ExpenseClaimId = 10, EmployeeId = 1 });
        db.Receipts.Add(new Receipt { ReceiptId = 4, ExpenseClaimId = 10, StorageUrl = "x", PublicId = "x",
            FileName = "x.pdf", ContentType = "application/pdf", Sha256 = "abc",
            RequiresManualReview = true, ProcessingStatus = ReceiptProcessingStatus.NeedsReview });
        await db.SaveChangesAsync();
        var service = new ReceiptService(db, new FakeReceiptStorage(), new FakeReceiptOcr());

        var result = await service.CorrectAsync(10, 4,
            new ReceiptCorrectionDto { Vendor = "Correct Vendor", Amount = 42, Currency = "eur" }, 1, default);

        Assert.False(result.RequiresManualReview);
        Assert.Equal("EUR", result.ExtractedCurrency);
        Assert.NotNull(result.CorrectedAt);
    }
}
