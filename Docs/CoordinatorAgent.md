# CoordinatorPlannerAgent — Architecture & Design Guide

## Purpose

The `CoordinatorPlannerAgent` is the central orchestrator of the expense reimbursement workflow. It:

1. Creates a **deterministic 9-step plan** for each new reimbursement
2. Executes each step by calling validated tools via `AgentToolbox`
3. **Pauses** at the Human Approval step — payment is NEVER processed without explicit manager approval
4. **Resumes** after approval is submitted via the `POST /workflows/{id}/approval` endpoint
5. Persists all state in PostgreSQL for full auditability

---

## Workflow State Machine

```
STARTED → RUNNING → WAITING_FOR_APPROVAL → RUNNING → COMPLETED
                                         → REJECTED
                                         → REVISION_REQUIRED
          RUNNING → FAILED
```

---

## 9-Step Plan

| # | Step Name | Agent | Type | Blocking |
|---|-----------|-------|------|----------|
| 1 | INTAKE | ExpenseExtractionAgent | AGENT | Yes |
| 2 | POLICY_CHECK | PolicyComplianceAgent | AGENT | Yes |
| 3 | RISK_CHECK | FraudAnomalyRiskAgent | AGENT | Yes |
| 4 | **HUMAN_APPROVAL** | **Human Manager** | **HUMAN_APPROVAL** | **HARD STOP** |
| 5 | BUDGET_CHECK | BudgetAgent | TOOL | Yes |
| 6 | FINANCE_PROCESSING | FinanceAgent | AGENT | Yes |
| 7 | PAYMENT | PaymentSandbox | TOOL | Yes |
| 8 | BUDGET_UPDATE | BudgetAgent | TOOL | Yes |
| 9 | FINAL_RESULT | FinanceAgent | AGENT | Yes |

---

## Key Rules (Golden Cases)

### GC-1: Normal Approval + Successful Payment
- All checks pass → Awaits human approval → Approved → Budget sufficient → Payment succeeds → `PAID`

### GC-2: High-Risk Claim
- `FraudAnomalyRiskAgent` flags the claim → Step fails → Workflow `FAILED`

### GC-3: Rejected by Manager
- Human step → Decision: `REJECTED` → Workflow `REJECTED`, no payment attempted

### GC-4: Revision Required
- Human step → Decision: `REVISION_REQUIRED` → Workflow `REVISION_REQUIRED`, no payment

### GC-5: Insufficient Budget
- `BudgetAgent` returns insufficient → Reimbursement set to `BUDGET_REVIEW_REQUIRED` → Workflow `FAILED`

### GC-6: Payment Failure
- Sandbox returns failed → `PaymentTransaction` recorded as `Failed` → Reimbursement `PAYMENT_FAILED`

---

## Human Approval Boundary

**This is the most critical safety boundary in the system.**

```
Step 3 completes → Coordinator calls PauseForHumanApprovalAsync()
                 → WorkflowExecution.Status = WaitingForApproval
                 → CoordinatorPlannerAgent.ExecuteAsync() returns (DOES NOT PROCEED)

POST /workflows/{id}/approval is called by Manager
→ ResumeAfterApprovalAsync() is called
→ If APPROVED: steps 5-9 execute
→ If REJECTED/REVISION_REQUIRED: workflow terminates, NO financial operations
```

---

## AgentToolbox — Allow-Listed Tools

| Tool | Validates | Side Effect |
|------|-----------|-------------|
| `CreateReimbursementAsync` | Amount > 0, valid IDs | Creates `Reimbursement` record |
| `CheckBudgetAvailabilityAsync` | DeptId, Year | Read-only |
| `ProcessReimbursementAsync` | Status = Approved | Sets status to Processing |
| `SubmitPaymentAsync` | No existing COMPLETED tx | Calls `IPaymentProvider` |
| `DeductBudgetAsync` | Budget exists | Updates `ApprovedSpend` |
| `RecordFinalResultAsync` | — | Updates workflow outcome |

---

## Retry Strategy

Each tool call uses Polly with:
- Max 3 retries
- Exponential backoff: 1s, 2s, 4s
- Only retried on transient exceptions (network timeouts, DB deadlocks)
- Human approval steps: **never retried**
