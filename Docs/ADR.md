# ADR-001: Coordinator/Planner Agent Orchestration Pattern

**Status:** Accepted  
**Date:** 2026-10-01  
**Component:** Reimbursement & Budget Tracking — Agentic AI Subsystem

---

## Context

The Expense Reimbursement workflow requires coordinating multiple specialized agents, enforcing business rules, managing state, and ensuring that critical boundaries (payment authorization) are never crossed without human approval. A flat or event-driven approach does not provide sufficient visibility, auditability, or control.

## Decision

We adopt a **Coordinator/Planner Agent** architecture in which:

1. **One Coordinator** orchestrates the entire workflow by generating a deterministic 9-step plan
2. **Specialized tools** (via `AgentToolbox`) are the only way agents interact with the backend
3. **Human approval** is a hard pause — no downstream step (budget deduction, payment) executes until the Coordinator receives an explicit `APPROVED` signal
4. All tool calls are **allow-listed** and inputs validated before execution
5. Workflow state is persisted in PostgreSQL (`WorkflowExecution`, `WorkflowStep`) to survive restarts and enable auditability

## Consequences

✅ Complete audit trail for every step and tool call  
✅ Idempotent workflow starts (duplicate claim IDs return existing workflow)  
✅ Human approval is the only gateway to financial operations  
✅ Retry logic (Polly) handles transient failures without coordinator awareness  
❌ Coordinator is a synchronous orchestrator — long-running workflows may need background job support in production  

---

# ADR-002: Deterministic Server-Side Financial Calculations

**Status:** Accepted  
**Date:** 2026-10-01

## Context

Financial calculations (budget utilization, remaining budget, utilization %) must be consistent, auditable, and immune to floating-point issues in client applications.

## Decision

All financial calculations are performed **exclusively on the backend** using C# `decimal` arithmetic. Clients receive pre-computed values. The React dashboard and Flutter app are pure display consumers.

## Consequences

✅ Single source of truth for all financial figures  
✅ No risk of client-side rounding discrepancies  
✅ All analytics queries computed in PostgreSQL via EF Core LINQ  

---

# ADR-003: Idempotency Strategy

**Status:** Accepted  
**Date:** 2026-10-01

## Context

Payment failures and retries can cause duplicate payments and duplicate workflow starts.

## Decision

- `Reimbursement.IdempotencyKey` is a unique string per claim processed
- `WorkflowExecution` is keyed by `ExpenseClaimId` — calling `StartWorkflow` twice for the same claim returns the existing workflow
- `PaymentTransaction` is checked for existing `COMPLETED` status before calling the payment provider
- `SandboxPaymentProvider` uses unique transaction IDs from the caller

## Consequences

✅ Zero duplicate payments  
✅ Zero duplicate workflow executions  
✅ Safe to retry on network failure  
