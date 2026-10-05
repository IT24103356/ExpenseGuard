using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using ExpenseGuard.Api.Contracts;
using ExpenseGuard.Api.Data;
using ExpenseGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseGuard.Api.Services;

public sealed record StoredObject(string Url, string PublicId);
public sealed record ReceiptExtraction(string? Vendor, decimal? Amount, DateTime? Date, string? Currency, decimal Confidence, bool RequiresManualReview);

public interface IReceiptStorage
{
    Task<StoredObject> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct);
}

public interface IReceiptOcr
{
    Task<ReceiptExtraction> ExtractAsync(Stream content, string fileName, string contentType, CancellationToken ct);
}

public sealed class CloudinaryReceiptStorage(HttpClient http) : IReceiptStorage
{
    public async Task<StoredObject> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        var cloud = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        var preset = Environment.GetEnvironmentVariable("CLOUDINARY_UPLOAD_PRESET");
        if (string.IsNullOrWhiteSpace(cloud) || string.IsNullOrWhiteSpace(preset))
            throw new InvalidOperationException("Cloudinary environment configuration is missing.");
        using var form = new MultipartFormDataContent();
        using var file = new StreamContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(preset), "upload_preset");
        using var response = await http.PostAsync($"https://api.cloudinary.com/v1_1/{cloud}/auto/upload", form, ct);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return new(json.RootElement.GetProperty("secure_url").GetString()!, json.RootElement.GetProperty("public_id").GetString()!);
    }
}

public sealed class OcrSpaceReceiptOcr(HttpClient http) : IReceiptOcr
{
    public async Task<ReceiptExtraction> ExtractAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        var key = Environment.GetEnvironmentVariable("OCR_SPACE_API_KEY");
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("OCR.Space environment configuration is missing.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ocr.space/parse/image");
        request.Headers.Add("apikey", key);
        using var form = new MultipartFormDataContent();
        using var file = new StreamContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent("true"), "isTable");
        request.Content = form;
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var text = json.RootElement.GetProperty("ParsedResults")[0].GetProperty("ParsedText").GetString();
        return new(text?.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim(),
            null, null, null, 0.50m, true);
    }
}

public sealed class FakeReceiptStorage : IReceiptStorage
{
    public Task<StoredObject> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
        => Task.FromResult(new StoredObject($"https://local.invalid/receipts/{Uri.EscapeDataString(fileName)}", $"fake-{fileName}"));
}

public sealed class FakeReceiptOcr : IReceiptOcr
{
    public Task<ReceiptExtraction> ExtractAsync(Stream content, string fileName, string contentType, CancellationToken ct)
        => Task.FromResult(new ReceiptExtraction("LOCAL TEST VENDOR", 12.34m, new DateTime(2026, 1, 1), "USD", 0.99m, false));
}

public interface IReceiptService
{
    Task<ReceiptDto> UploadAsync(int claimId, Stream content, string fileName, string contentType, long length, int actorId, CancellationToken ct);
    Task<ReceiptDto> CorrectAsync(int claimId, int receiptId, ReceiptCorrectionDto input, int actorId, CancellationToken ct);
}

public sealed class ReceiptService(AppDbContext db, IReceiptStorage storage, IReceiptOcr ocr) : IReceiptService
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "application/pdf"];

    public async Task<ReceiptDto> UploadAsync(int claimId, Stream content, string fileName, string contentType, long length, int actorId, CancellationToken ct)
    {
        var claim = await OwnedClaim(claimId, actorId, ct);
        if (claim.Status is not (ClaimStatus.Draft or ClaimStatus.NeedsCorrection))
            throw new InvalidOperationException("Receipts can only be added to draft or correction claims.");
        if (length <= 0 || length > MaxBytes) throw new ArgumentException("Receipt must be between 1 byte and 10 MB.");
        if (!AllowedTypes.Contains(contentType.ToLowerInvariant())) throw new ArgumentException("Only JPEG, PNG and PDF receipts are supported.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length != length || buffer.Length > MaxBytes) throw new ArgumentException("Receipt length is invalid.");
        var hash = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
        if (await db.Receipts.AnyAsync(r => r.ExpenseClaimId == claimId && r.Sha256 == hash, ct))
            throw new InvalidOperationException("This receipt has already been uploaded to the claim.");

        buffer.Position = 0;
        var stored = await storage.UploadAsync(buffer, Path.GetFileName(fileName), contentType, ct);
        buffer.Position = 0;
        var extraction = await ocr.ExtractAsync(buffer, Path.GetFileName(fileName), contentType, ct);
        var receipt = new Receipt
        {
            ExpenseClaimId = claimId, StorageUrl = stored.Url, PublicId = stored.PublicId,
            FileName = Path.GetFileName(fileName), ContentType = contentType.ToLowerInvariant(),
            SizeBytes = length, Sha256 = hash, ProcessingStatus = extraction.RequiresManualReview
                ? ReceiptProcessingStatus.NeedsReview : ReceiptProcessingStatus.Processed,
            ExtractedVendor = extraction.Vendor, ExtractedAmount = extraction.Amount,
            ExtractedDate = extraction.Date, ExtractedCurrency = extraction.Currency?.ToUpperInvariant(),
            Confidence = extraction.Confidence, RequiresManualReview = extraction.RequiresManualReview
        };
        db.Add(receipt);
        await db.SaveChangesAsync(ct);
        return Map(receipt);
    }

    public async Task<ReceiptDto> CorrectAsync(int claimId, int receiptId, ReceiptCorrectionDto input, int actorId, CancellationToken ct)
    {
        _ = await OwnedClaim(claimId, actorId, ct);
        var receipt = await db.Receipts.SingleOrDefaultAsync(r => r.ReceiptId == receiptId && r.ExpenseClaimId == claimId, ct)
            ?? throw new KeyNotFoundException("Receipt not found.");
        receipt.ExtractedVendor = input.Vendor?.Trim();
        receipt.ExtractedAmount = input.Amount;
        receipt.ExtractedDate = input.PurchaseDate;
        receipt.ExtractedCurrency = input.Currency?.ToUpperInvariant();
        receipt.RequiresManualReview = false;
        receipt.ProcessingStatus = ReceiptProcessingStatus.Processed;
        receipt.CorrectedAt = DateTime.UtcNow;
        receipt.CorrectedByEmployeeId = actorId;
        await db.SaveChangesAsync(ct);
        return Map(receipt);
    }

    private async Task<ExpenseClaim> OwnedClaim(int id, int actorId, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.SingleOrDefaultAsync(c => c.ExpenseClaimId == id && c.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("Claim not found.");
        if (claim.EmployeeId != actorId) throw new UnauthorizedAccessException("Claim is owned by another employee.");
        return claim;
    }

    private static ReceiptDto Map(Receipt r) => new(r.ReceiptId, r.ExpenseClaimId, r.StorageUrl, r.FileName,
        r.ContentType, r.SizeBytes, r.Sha256, r.ProcessingStatus, r.ExtractedVendor, r.ExtractedAmount,
        r.ExtractedDate, r.ExtractedCurrency, r.Confidence, r.RequiresManualReview, r.CorrectedAt);
}
