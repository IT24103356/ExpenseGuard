using System.Text.Json;
using ExpenseGuard.Api.Contracts;
using ExpenseGuard.Api.Models;

namespace ExpenseGuard.Api.Services;

public static class ClaimReviewComposer
{
    public static PurchaseRequestReviewDto Compose(
        ExpenseClaim claim,
        PolicyEvaluation? policy,
        FraudEvaluation? fraud,
        bool reserved,
        string? failureReason,
        IReadOnlyDictionary<string, string>? ai = null)
    {
        var policyFlags = (policy?.Violations ?? [])
            .Select(v => new ReviewFlagDto(v.RuleCode, v.Severity, v.Message)).ToList();
        var policyOutcome = policy?.Outcome ?? "not_applicable";
        var policySection = new ReviewSectionDto(policyOutcome,
            Lookup(ai, "policy") ?? DefaultPolicy(policyOutcome, policyFlags), policyFlags);

        var fraudFlags = ((fraud?.Flags.Count > 0 ? fraud.Flags : claim.FraudFlags) ?? [])
            .Select(f => new ReviewFlagDto(f.RuleCode, f.Severity, f.FlagReason)).ToList();
        var fraudOutcome = fraud?.RiskLevel ?? (fraudFlags.Count == 0 ? "low" : "medium");
        var fraudSection = new ReviewSectionDto(fraudOutcome,
            Lookup(ai, "fraud") ?? DefaultFraud(fraudOutcome, fraudFlags), fraudFlags);

        var budgetFlags = reserved
            ? new List<ReviewFlagDto>()
            : [new ReviewFlagDto("BUDGET_RESERVE_FAILED", "high",
                failureReason ?? "No active budget could reserve the claim amount.")];
        var budgetSection = new ReviewSectionDto(reserved ? "ok" : "exceeded",
            Lookup(ai, "budget") ?? (reserved
                ? "Department budget can cover this claim."
                : budgetFlags[0].Message),
            budgetFlags, claim.Amount);

        var receipt = claim.Receipts?.OrderBy(r => r.ReceiptId).LastOrDefault();
        var receiptNote = string.Join(" ", new[] { Lookup(ai, "receipt"), DefaultReceipt(receipt, claim) }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        var hasFlags = policyFlags.Count > 0 || fraudFlags.Count > 0 || budgetFlags.Count > 0;
        var findings = hasFlags
            ? string.Join(" ", new[] { policySection.Summary, fraudSection.Summary, budgetSection.Summary }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
            : "Receipt OCR and policy, fraud, and budget checks are clear.";
        var summary = string.Join(" ", new[]
        {
            receiptNote,
            findings,
            "Human approval is still required."
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new PurchaseRequestReviewDto(
            claim.Category, policy?.Policy?.PolicyCode, claim.Employee.DepartmentId,
            claim.Employee.Department?.DepartmentName, claim.Employee.FullName,
            claim.Employee.Designation?.Name, hasFlags, summary,
            policySection, fraudSection, budgetSection);
    }

    public static Dictionary<string, string> AiSummaries(IEnumerable<WorkflowStep> steps)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.OutputSnapshotJson)) continue;
            try
            {
                using var json = JsonDocument.Parse(step.OutputSnapshotJson);
                if (!json.RootElement.TryGetProperty("Ai", out var ai)
                    && !json.RootElement.TryGetProperty("ai", out ai))
                    continue;
                if (ai.ValueKind != JsonValueKind.Object) continue;
                var agent = ai.TryGetProperty("agent", out var name) ? name.GetString() : step.Name;
                var status = ai.TryGetProperty("status", out var st) ? st.GetString() : null;
                var summary = ai.TryGetProperty("summary", out var text) ? text.GetString() : null;
                if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(summary)) continue;
                if (status is "failed" or "not_implemented") continue;
                result[agent] = summary;
            }
            catch (JsonException)
            {
                // Ignore malformed workflow snapshots and keep deterministic findings.
            }
        }
        return result;
    }

    private static string? Lookup(IReadOnlyDictionary<string, string>? ai, string agent)
        => ai is not null && ai.TryGetValue(agent, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string DefaultPolicy(string outcome, IReadOnlyList<ReviewFlagDto> flags)
        => flags.Count == 0
            ? outcome == "compliant" ? "Claim matches the applicable policy." : "No applicable policy was found."
            : string.Join(" ", flags.Select(f => f.Message));

    private static string DefaultFraud(string outcome, IReadOnlyList<ReviewFlagDto> flags)
        => flags.Count == 0
            ? "No fraud flags from the receipt and claim checks."
            : $"{outcome}: {string.Join(" ", flags.Select(f => f.Message))}";

    private static string DefaultReceipt(Receipt? receipt, ExpenseClaim claim)
    {
        if (receipt is null) return "No receipt image was available for AI review.";
        var parts = new List<string> { "AI reviewed the receipt image and OCR text." };
        if (!string.IsNullOrWhiteSpace(receipt.ExtractedVendor))
            parts.Add($"OCR vendor is {receipt.ExtractedVendor}.");
        if (receipt.ExtractedAmount is not null)
            parts.Add($"OCR amount is {receipt.ExtractedCurrency ?? claim.Currency} {receipt.ExtractedAmount}.");
        if (receipt.ExtractedAmount is not null && receipt.ExtractedAmount != claim.Amount)
            parts.Add("The OCR amount does not match the claimed amount.");
        return string.Join(" ", parts);
    }
}
