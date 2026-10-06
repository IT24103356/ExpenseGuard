# Testing Guide

## Overview

The test suite covers all 6 **Agent Golden Cases** plus additional edge cases:
- 8 `ReimbursementServiceTests` — Finance queue, payment, idempotency
- 6 `BudgetServiceTests` — Budget CRUD, deduction, alerts, utilization
- 5 `CoordinatorPlannerAgentTests` — Workflow orchestration, approval decisions

**Total: 20 tests, 100% pass rate**

---

## Prerequisites

```powershell
# .NET 8 SDK
dotnet --version  # >= 8.0

# No database required — uses EF Core InMemory provider
```

---

## Running Tests

```powershell
cd Backend/ReimbursementBudget.Tests
dotnet test
```

Expected output:
```
Passed! - Failed: 0, Passed: 20, Skipped: 0, Total: 20, Duration: ~514ms
```

---

## Golden Case Test Mapping

| Golden Case | Test Method | What It Verifies |
|-------------|-------------|-----------------|
| GC-1: Normal approval + payment | `SufficientBudget_AllowsProcessing` + `SuccessfulPayment_UpdatesStatus_ToPaid` | Budget check passes, payment succeeds → PAID |
| GC-2: High-risk (rejected at risk check) | `RejectedClaim_CannotBeProcessed` | Non-approved claim blocked from processing |
| GC-3: Manager rejection | `Approval_Rejected_TerminatesWorkflow_WithoutPayment` | Workflow REJECTED, final outcome set |
| GC-4: Revision required | `Approval_RevisionRequired_PausesWorkflow` | Workflow status = RevisionRequired |
| GC-5: Insufficient budget | `InsufficientBudget_SetsStatus_BudgetReviewRequired` | Status = BudgetReviewRequired, payment NOT called |
| GC-6: Payment failure | `FailedPayment_UpdatesStatus_ToPaymentFailed` | Payment sandbox returns failure → PaymentFailed |

---

## Additional Tests

| Test | What It Verifies |
|------|-----------------|
| `ApprovedClaim_CreatesReimbursement_AndStatusIsApproved` | New reimbursement created with APPROVED status |
| `DuplicateClaimId_ReturnsExisting_NotDuplicate` | Idempotency: same claim ID returns existing |
| `DuplicatePayment_IsBlocked` | Existing COMPLETED payment tx prevents re-payment |
| `CheckBudget_SufficientFunds_ReturnsTrue` | Budget availability check — sufficient |
| `CheckBudget_InsufficientFunds_ReturnsFalse` | Budget availability check — insufficient |
| `CreateBudget_StoresBudget_Successfully` | Budget creation with correct utilization |
| `DeductBudget_CorrectlyUpdatesTotals` | Spend totals after deduction |
| `Budget_Alert_CreatedAt80Percent` | Alert generated when utilization crosses 80% |
| `UtilizationPercentage_CalculatedCorrectly` | Deterministic server-side calculation |
| `StartWorkflow_CreatesExecution_WithCorrectSteps` | 9-step plan created |
| `StartWorkflow_Idempotent_NoDuplicates` | Duplicate workflow not created |
| `Approval_NotWaiting_ThrowsException` | Guard: can't resume non-waiting workflow |

---

## Test Architecture

All tests use:
- **xUnit** as the test framework
- **Moq** for mocking `IPaymentProvider`, `IBudgetService`, `IReimbursementService`
- **FluentAssertions** for readable assertions
- **EF Core InMemory** for database (transaction warnings suppressed via `ConfigureWarnings`)
- **NullLogger** (no logging overhead in tests)

---

## Running the React Frontend

```powershell
cd React
npm run dev
# Opens at http://localhost:5173
```

Works with mock data if the backend is not connected.

---

## Running the Backend API

```powershell
# Set JWT key environment variable
$env:Jwt__Key = "your-32-char-minimum-secret-key!!"

cd Backend/ReimbursementBudget.API
dotnet run
# Swagger available at http://localhost:5000/swagger
```

> **Note:** PostgreSQL must be running and connection string set in `appsettings.json` or via `ConnectionStrings__DefaultConnection` env var.

---

## Running the Flutter App

```powershell
cd Flutter
flutter pub get
flutter run  # Requires connected Android/iOS device or emulator
```
