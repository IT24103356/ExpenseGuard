using System.ComponentModel.DataAnnotations;
using ExpenseGuard.Api.Models;

namespace ExpenseGuard.Api.Contracts;

public sealed record EmployeeProfileDto(
    int EmployeeId, string FullName, string Email, string Username, bool IsActive, bool IsLocked,
    int RoleId, int DepartmentId, int? ManagerId, int? DesignationId, string? Designation);

public sealed class PurchaseRequestWriteDto
{
    [Required, StringLength(2000)] public string Description { get; init; } = string.Empty;
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal EstimatedAmount { get; init; }
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Currency { get; init; } = "USD";
    [StringLength(200)] public string? Vendor { get; init; }
    [Range(0, long.MaxValue)] public long Version { get; init; }
}

public sealed record PurchaseRequestDto(
    int PurchaseRequestId, int EmployeeId, string Description, decimal EstimatedAmount, string Currency,
    string? Vendor, PurchaseRequestStatus Status, DateTime? SubmittedAt, long Version);

public sealed class ClaimWriteDto : IValidatableObject
{
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Amount { get; init; }
    [Required, StringLength(100)] public string Category { get; init; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; init; } = string.Empty;
    [Required, RegularExpression("^[A-Za-z]{3}$")] public string Currency { get; init; } = "USD";
    [StringLength(200)] public string? Vendor { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public int? PurchaseRequestId { get; init; }
    public ClaimFlow Flow { get; init; } = ClaimFlow.OutOfPocket;
    [Range(0, long.MaxValue)] public long Version { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Flow == ClaimFlow.PrePurchase && PurchaseRequestId is null)
            yield return new ValidationResult("Pre-purchase claims require a purchase request.", [nameof(PurchaseRequestId)]);
        if (Flow == ClaimFlow.OutOfPocket && PurchaseRequestId is not null)
            yield return new ValidationResult("Out-of-pocket claims cannot link a purchase request.", [nameof(PurchaseRequestId)]);
    }
}

public sealed record ClaimDto(
    int ExpenseClaimId, int EmployeeId, int? PurchaseRequestId, decimal Amount, string Category,
    string Description, string Currency, string? Vendor, DateTime? PurchaseDate, ClaimFlow Flow,
    ClaimStatus Status, long Version);

public sealed class ReceiptCorrectionDto
{
    [StringLength(200)] public string? Vendor { get; init; }
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal? Amount { get; init; }
    public DateTime? PurchaseDate { get; init; }
    [RegularExpression("^[A-Za-z]{3}$")] public string? Currency { get; init; }
}

public sealed record ReceiptDto(
    int ReceiptId, int ExpenseClaimId, string StorageUrl, string FileName, string ContentType,
    long SizeBytes, string Sha256, ReceiptProcessingStatus ProcessingStatus, string? ExtractedVendor,
    decimal? ExtractedAmount, DateTime? ExtractedDate, string? ExtractedCurrency, decimal? Confidence,
    bool RequiresManualReview, DateTime? CorrectedAt);

public sealed record ClaimHistoryDto(
    int ClaimStatusHistoryId, ClaimStatus FromStatus, ClaimStatus ToStatus,
    int ChangedByEmployeeId, string? Reason, DateTime ChangedAt);

public sealed class ClaimSearchQuery
{
    public ClaimStatus? Status { get; init; }
    [StringLength(100)] public string? Category { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    [Range(1, 100)] public int Limit { get; init; } = 50;
}
