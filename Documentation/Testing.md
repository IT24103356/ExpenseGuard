# Testing & Evaluation Report — Policy & Compliance Module

## 1. Test Strategy Overview
The Policy & Compliance module utilizes automated unit, integration, and agentic evaluation testing implemented in **xUnit**, **Moq**, and **FluentAssertions**.

All tests execute against the core engine and evaluate:
1. **Deterministic Business Rules:** Complete isolation from stochastic LLM behavior.
2. **Deterministic Duplicate Matching:** Multi-parameter candidate scoring and exact matching.
3. **Statistical Anomaly Algorithms:** Mean, standard deviation ($Z$-scores), percentiles, and threshold proximity evasion.
4. **Agentic Subsystem & Safe-Failure:** Structured schema adherence and safe fail-closed behavior on tool failure.
5. **Approval Workflow Pre-conditions:** Role verification, pre-condition checks, and audit history preservation.

---

## 2. Test Execution Summary

```
Total Test Cases: 18
Passed: 18
Failed: 0
Skipped: 0
Duration: 68 ms
Framework: .NET 10.0 (xUnit)
```

---

## 3. Unit Test Breakdown

### 3.1. Policy Validation Engine (`PolicyValidationTests`)
- `ValidateClaim_WhenWithinLimitsWithReceipt_ReturnsCompliant`: Verifies compliant claims produce no violations.
- `ValidateClaim_WhenMealExceedsConfiguredMaximum_ReturnsPolicyViolation`: Verifies `RULE_MAX_AMOUNT_EXCEEDED` is triggered when spend exceeds policy cap.
- `ValidateClaim_WhenReceiptIsMissing_ReturnsReceiptViolation`: Verifies `RULE_MISSING_RECEIPT` is generated when receipt document is absent.
- `ValidateClaim_WhenCategoryIsNotAllowed_ReturnsCategoryDisallowedViolation`: Verifies `RULE_CATEGORY_DISALLOWED` is flagged for unapproved categories.
- `ValidateClaim_WhenCurrencyIsDisallowed_ReturnsCurrencyViolation`: Verifies `RULE_CURRENCY_DISALLOWED` enforces corporate currency restrictions.
- `ValidateClaim_WhenExceedsDepartmentMonthlyBudget_ReturnsBudgetExceededViolation`: Verifies cumulative monthly department limit checks (`RULE_DEPARTMENT_BUDGET_EXCEEDED`).

### 3.2. Duplicate Detection Engine (`DuplicateDetectionTests`)
- `DetectDuplicates_WhenExactSameEmployeeMerchantDateAmount_ReturnsExactMatch`: Verifies candidate matching produces `EXACT` match and similarity score `1.00`.
- `DetectDuplicates_WhenIdenticalReceiptAttachedToDifferentClaim_ReturnsMatch`: Verifies identical receipts across claims are detected as critical duplicates.
- `DetectDuplicates_WhenCompletelyUnrelatedClaim_ReturnsNoMatches`: Verifies zero false-positive matches on distinct claims.

### 3.3. Approval Business Rules & Security (`ApprovalBusinessRulesTests`)
- `ApproveClaim_WhenUserIsNotManager_ThrowsUnauthorizedAccessException`: Proves employees cannot approve claims or bypass manager authorization.
- `RejectClaim_WhenNoReasonProvided_ThrowsArgumentException`: Proves manager must provide an explanatory rejection reason.
- `RequestRevision_WhenCommentProvided_TransitionsToRevisionRequired`: Verifies claim transitions to `REVISION_REQUIRED` and preserves manager feedback.

---

## 4. Agent Evaluation — Golden Test Cases Matrix (Section 20)

| Golden Case | Input Scenario | Expected Risk Level | Expected Signals | Workflow Outcome | Test Status |
| :--- | :--- | :---: | :--- | :--- | :---: |
| **Case 1: Normal Claim** | Amount: 5,000 LKR, Meals, within historical average (4,500 LKR). | `LOW_RISK` | No anomalies detected. Risk score < 35. | Eligible for auto-clearance or standard sign-off. | **PASSED** |
| **Case 2: Exact Duplicate** | Amount: 25,000 LKR, ABC Hotel, 2026-09-20, identical to approved Claim #1002. | `REVIEW_REQUIRED` | `DUPLICATE` (Severity: CRITICAL). Matching claim identified. | Workflow PAUSES at `WAITING_FOR_MANAGER_APPROVAL`. | **PASSED** |
| **Case 3: Extreme Amount** | Amount: 85,000 LKR, Meals. Baseline is 15,000 LKR ($Z > +3.1\sigma$). | `HIGH_RISK` | `UNUSUAL_AMOUNT` (Severity: HIGH). Statistical amount outlier. | Workflow PAUSES at `WAITING_FOR_MANAGER_APPROVAL`. | **PASSED** |
| **Case 4: Duplicate + Extreme Amount** | Amount: 75,000 LKR, matching prior claim AND extreme amount outlier. | `REVIEW_REQUIRED` | `DUPLICATE` + `UNUSUAL_AMOUNT`. Risk score $\ge 80$. | Workflow PAUSES at `WAITING_FOR_MANAGER_APPROVAL`. | **PASSED** |
| **Case 5: Agent Tool Failure** | Tool throws `TimeoutException` during database execution. | `REVIEW_REQUIRED` | `SAFE_FALLBACK` (Severity: HIGH). Reason: Safe-failure engaged. | Safe-fallback engaged: Claim is **NOT** auto-approved. Pauses for human review. | **PASSED** |

---

## 5. Verification Command
To reproduce and execute the entire test suite locally:
```powershell
dotnet test Backend/tests/PolicyCompliance.Tests/PolicyCompliance.Tests.csproj
```
