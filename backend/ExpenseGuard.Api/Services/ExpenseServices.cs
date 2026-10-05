using ExpenseGuard.Api.Contracts;
using ExpenseGuard.Api.Data;
using ExpenseGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseGuard.Api.Services;

public interface ICurrentEmployee
{
    int EmployeeId { get; }
}

public sealed class HeaderCurrentEmployee(IHttpContextAccessor accessor) : ICurrentEmployee
{
    public int EmployeeId => int.TryParse(accessor.HttpContext?.Request.Headers["X-Employee-Id"], out var id) && id > 0
        ? id : throw new UnauthorizedAccessException("A valid X-Employee-Id header is required.");
}

public interface IEmployeeService
{
    Task<EmployeeProfileDto> GetProfileAsync(int actorId, CancellationToken ct);
    Task<IReadOnlyList<EmployeeProfileDto>> ListAsync(int actorId, CancellationToken ct);
}

public sealed class EmployeeService(AppDbContext db) : IEmployeeService
{
    private static EmployeeProfileDto Map(Employee e) => new(
        e.EmployeeId, e.FullName, e.Email, e.Username, e.IsActive, e.IsLocked, e.RoleId,
        e.DepartmentId, e.ManagerId, e.DesignationId, e.Designation == null ? null : e.Designation.Name);

    public async Task<EmployeeProfileDto> GetProfileAsync(int actorId, CancellationToken ct)
        => Map(await db.Employees.AsNoTracking().Include(e => e.Designation)
            .SingleOrDefaultAsync(e => e.EmployeeId == actorId, ct)
            ?? throw new KeyNotFoundException("Employee not found."));

    public async Task<IReadOnlyList<EmployeeProfileDto>> ListAsync(int actorId, CancellationToken ct)
    {
        var isAdmin = await db.Employees.AsNoTracking().AnyAsync(
            e => e.EmployeeId == actorId && e.IsActive && !e.IsLocked && e.Role.RoleName.ToLower() == "admin", ct);
        if (!isAdmin) throw new UnauthorizedAccessException("Administrator role is required.");
        return await db.Employees.AsNoTracking().Include(e => e.Designation).OrderBy(e => e.FullName)
            .Select(e => new EmployeeProfileDto(e.EmployeeId, e.FullName, e.Email, e.Username,
                e.IsActive, e.IsLocked, e.RoleId, e.DepartmentId, e.ManagerId, e.DesignationId,
                e.Designation == null ? null : e.Designation.Name)).ToListAsync(ct);
    }
}

public interface IPurchaseRequestService
{
    Task<IReadOnlyList<PurchaseRequestDto>> ListAsync(int actorId, CancellationToken ct);
    Task<PurchaseRequestDto> GetAsync(int id, int actorId, CancellationToken ct);
    Task<PurchaseRequestDto> CreateAsync(PurchaseRequestWriteDto input, int actorId, CancellationToken ct);
    Task<PurchaseRequestDto> UpdateAsync(int id, PurchaseRequestWriteDto input, int actorId, CancellationToken ct);
    Task SubmitAsync(int id, int actorId, CancellationToken ct);
    Task DeleteAsync(int id, int actorId, CancellationToken ct);
}

public sealed class PurchaseRequestService(AppDbContext db) : IPurchaseRequestService
{
    private static PurchaseRequestDto Map(PurchaseRequest p) => new(p.PurchaseRequestId, p.EmployeeId,
        p.Description, p.EstimatedAmount, p.Currency, p.Vendor, p.Status, p.SubmittedAt, p.Version);

    private async Task<PurchaseRequest> Owned(int id, int actorId, CancellationToken ct)
    {
        var item = await db.PurchaseRequests.SingleOrDefaultAsync(p => p.PurchaseRequestId == id, ct)
            ?? throw new KeyNotFoundException("Purchase request not found.");
        if (item.EmployeeId != actorId) throw new UnauthorizedAccessException("Purchase request is owned by another employee.");
        return item;
    }

    public async Task<IReadOnlyList<PurchaseRequestDto>> ListAsync(int actorId, CancellationToken ct)
        => (await db.PurchaseRequests.AsNoTracking().Where(p => p.EmployeeId == actorId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync(ct)).Select(Map).ToList();

    public async Task<PurchaseRequestDto> GetAsync(int id, int actorId, CancellationToken ct) => Map(await Owned(id, actorId, ct));

    public async Task<PurchaseRequestDto> CreateAsync(PurchaseRequestWriteDto input, int actorId, CancellationToken ct)
    {
        var item = new PurchaseRequest { EmployeeId = actorId };
        Apply(item, input);
        db.Add(item);
        await db.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<PurchaseRequestDto> UpdateAsync(int id, PurchaseRequestWriteDto input, int actorId, CancellationToken ct)
    {
        var item = await Owned(id, actorId, ct);
        if (item.Status != PurchaseRequestStatus.Draft) throw new InvalidOperationException("Only draft purchase requests can be edited.");
        if (item.Version != input.Version) throw new DbUpdateConcurrencyException("Purchase request has changed.");
        Apply(item, input);
        item.Version++;
        await db.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task SubmitAsync(int id, int actorId, CancellationToken ct)
    {
        var item = await Owned(id, actorId, ct);
        if (item.Status != PurchaseRequestStatus.Draft) throw new InvalidOperationException("Only draft purchase requests can be submitted.");
        item.Status = PurchaseRequestStatus.Submitted;
        item.SubmittedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.Version++;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int actorId, CancellationToken ct)
    {
        var item = await Owned(id, actorId, ct);
        if (item.Status != PurchaseRequestStatus.Draft) throw new InvalidOperationException("Only draft purchase requests can be deleted.");
        db.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    private static void Apply(PurchaseRequest item, PurchaseRequestWriteDto input)
    {
        item.Description = input.Description.Trim();
        item.EstimatedAmount = input.EstimatedAmount;
        item.Currency = input.Currency.ToUpperInvariant();
        item.Vendor = input.Vendor?.Trim();
        item.UpdatedAt = DateTime.UtcNow;
    }
}

public interface IClaimService
{
    Task<IReadOnlyList<ClaimDto>> SearchAsync(ClaimSearchQuery query, int actorId, CancellationToken ct);
    Task<ClaimDto> GetAsync(int id, int actorId, CancellationToken ct);
    Task<ClaimDto> CreateAsync(ClaimWriteDto input, int actorId, CancellationToken ct);
    Task<ClaimDto> UpdateAsync(int id, ClaimWriteDto input, int actorId, CancellationToken ct);
    Task TransitionAsync(int id, int actorId, ClaimStatus target, string? reason, CancellationToken ct);
    Task DeleteAsync(int id, int actorId, CancellationToken ct);
    Task<IReadOnlyList<ClaimHistoryDto>> HistoryAsync(int id, int actorId, CancellationToken ct);
}

public sealed class ClaimService(AppDbContext db) : IClaimService
{
    private static ClaimDto Map(ExpenseClaim c) => new(c.ExpenseClaimId, c.EmployeeId, c.PurchaseRequestId,
        c.Amount, c.Category, c.Description, c.Currency, c.Vendor, c.PurchaseDate, c.Flow, c.Status, c.Version);

    private async Task<ExpenseClaim> Owned(int id, int actorId, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.SingleOrDefaultAsync(c => c.ExpenseClaimId == id && c.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("Claim not found.");
        if (claim.EmployeeId != actorId) throw new UnauthorizedAccessException("Claim is owned by another employee.");
        return claim;
    }

    public async Task<IReadOnlyList<ClaimDto>> SearchAsync(ClaimSearchQuery query, int actorId, CancellationToken ct)
    {
        var items = db.ExpenseClaims.AsNoTracking().Where(c => c.EmployeeId == actorId && c.DeletedAt == null);
        if (query.Status is not null) items = items.Where(c => c.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Category)) items = items.Where(c => c.Category == query.Category);
        if (query.From is not null) items = items.Where(c => c.CreatedAt >= query.From);
        if (query.To is not null) items = items.Where(c => c.CreatedAt <= query.To);
        return (await items.OrderByDescending(c => c.CreatedAt).Take(query.Limit).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<ClaimDto> GetAsync(int id, int actorId, CancellationToken ct) => Map(await Owned(id, actorId, ct));

    public async Task<ClaimDto> CreateAsync(ClaimWriteDto input, int actorId, CancellationToken ct)
    {
        await ValidateLink(input, actorId, ct);
        var claim = new ExpenseClaim { EmployeeId = actorId };
        Apply(claim, input);
        db.Add(claim);
        await db.SaveChangesAsync(ct);
        return Map(claim);
    }

    public async Task<ClaimDto> UpdateAsync(int id, ClaimWriteDto input, int actorId, CancellationToken ct)
    {
        var claim = await Owned(id, actorId, ct);
        if (claim.Status is not (ClaimStatus.Draft or ClaimStatus.NeedsCorrection))
            throw new InvalidOperationException("Only draft or correction claims can be edited.");
        if (claim.Version != input.Version) throw new DbUpdateConcurrencyException("Claim has changed.");
        await ValidateLink(input, actorId, ct);
        Apply(claim, input);
        claim.Version++;
        await db.SaveChangesAsync(ct);
        return Map(claim);
    }

    public async Task TransitionAsync(int id, int actorId, ClaimStatus target, string? reason, CancellationToken ct)
    {
        var claim = await Owned(id, actorId, ct);
        var allowed = target == ClaimStatus.Submitted &&
            claim.Status is ClaimStatus.Draft or ClaimStatus.NeedsCorrection;
        if (!allowed) throw new InvalidOperationException($"Cannot transition claim from {claim.Status} to {target}.");
        if (!await db.Receipts.AnyAsync(r => r.ExpenseClaimId == id, ct))
            throw new InvalidOperationException("At least one receipt is required before submission.");
        var from = claim.Status;
        claim.Status = target;
        claim.SubmittedAt = DateTime.UtcNow;
        claim.UpdatedAt = DateTime.UtcNow;
        claim.Version++;
        db.ClaimStatusHistories.Add(new ClaimStatusHistory
        {
            ExpenseClaimId = id, FromStatus = from, ToStatus = target,
            ChangedByEmployeeId = actorId, Reason = reason
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int actorId, CancellationToken ct)
    {
        var claim = await Owned(id, actorId, ct);
        if (claim.Status != ClaimStatus.Draft) throw new InvalidOperationException("Only draft claims can be deleted.");
        claim.DeletedAt = DateTime.UtcNow;
        claim.Version++;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ClaimHistoryDto>> HistoryAsync(int id, int actorId, CancellationToken ct)
    {
        _ = await Owned(id, actorId, ct);
        return await db.ClaimStatusHistories.AsNoTracking().Where(h => h.ExpenseClaimId == id)
            .OrderBy(h => h.ChangedAt).Select(h => new ClaimHistoryDto(h.ClaimStatusHistoryId,
                h.FromStatus, h.ToStatus, h.ChangedByEmployeeId, h.Reason, h.ChangedAt)).ToListAsync(ct);
    }

    private async Task ValidateLink(ClaimWriteDto input, int actorId, CancellationToken ct)
    {
        if (input.PurchaseRequestId is null) return;
        var request = await db.PurchaseRequests.AsNoTracking()
            .SingleOrDefaultAsync(p => p.PurchaseRequestId == input.PurchaseRequestId, ct)
            ?? throw new InvalidOperationException("Purchase request does not exist.");
        if (request.EmployeeId != actorId) throw new UnauthorizedAccessException("Purchase request is owned by another employee.");
        if (request.Status != PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("Only approved purchase requests can be linked.");
    }

    private static void Apply(ExpenseClaim claim, ClaimWriteDto input)
    {
        claim.Amount = input.Amount;
        claim.Category = input.Category.Trim();
        claim.Description = input.Description.Trim();
        claim.Currency = input.Currency.ToUpperInvariant();
        claim.Vendor = input.Vendor?.Trim();
        claim.PurchaseDate = input.PurchaseDate;
        claim.PurchaseRequestId = input.PurchaseRequestId;
        claim.Flow = input.Flow;
        claim.UpdatedAt = DateTime.UtcNow;
    }
}
