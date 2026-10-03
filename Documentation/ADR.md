# Architectural Decision Records (ADRs) — Policy & Compliance Module

## ADR-001: Separation of Deterministic Policy Rules vs. Agentic Anomaly Analysis

### Status
Accepted

### Context
Corporate spend compliance requires enforceability of non-negotiable legal and financial policies (e.g., maximum expense caps, mandatory receipts, authorized currencies, and budget ceilings). Relying solely on Large Language Models or agentic reasoning for business rule enforcement poses risks of hallucination, non-deterministic decisions, and regulatory non-compliance.

### Decision
We partition the verification pipeline into two distinct layers:
1. **Deterministic Policy Validation Engine (`PolicyValidationEngine`):** Executes hardcoded mathematical and logical checks against active company policies stored in PostgreSQL. If a limit is exceeded or a receipt is missing, it produces an immutable `PolicyViolation` record without agent interpretation.
2. **Fraud/Anomaly-Risk Agent Subsystem (`FraudAnomalyRiskAgent`):** Analyzes multidimensional risk signals (cross-claim duplicates, $Z$-score statistical distribution outliers, merchant velocity clusters, threshold proximity evasions). The agent synthesizes findings into a structured score and signals.

### Consequences
- **Positive:** Absolute regulatory reliability and repeatability for hard rules; sophisticated pattern recognition for fraud without exposing business logic to hallucination.
- **Negative:** Requires maintaining two distinct validation layers.

---

## ADR-002: Human Approval Pause Pattern (Workflow State Machine)

### Status
Accepted

### Context
Autonomous AI systems must not possess unilateral authority to disburse or cancel company funds. A mechanism is required to halt automation and require authorized human review whenever elevated risk or policy exceptions occur.

### Decision
We implement a state machine pause pattern. If any of the following conditions occur:
- `PolicyStatus == VIOLATIONS_FOUND`
- `RiskLevel == HIGH_RISK` or `RiskLevel == REVIEW_REQUIRED`
- Policy mandates manager approval
- Claim total exceeds standard threshold

The workflow halts immediately with status `WAITING_FOR_MANAGER_APPROVAL`.
The claim is placed into the **Manager Review Queue**. Only an authenticated user with role `Manager` or `Admin` can advance the state via explicit `/approve`, `/reject`, or `/request-revision` API commands.

### Consequences
- **Positive:** Complete prevention of unauthorized disbursements; visible and explainable pause points for viva demonstration.
- **Negative:** Requires manager action before reimbursement can proceed.

---

## ADR-003: Allow-Listed Controlled Agent Tools with Projections

### Status
Accepted

### Context
Granting the agent unrestricted database queries or direct write access to sensitive financial tables introduces security vulnerabilities and unpredictable state changes.

### Decision
The agent interacts exclusively with six allow-listed tools exposed via the `IComplianceAgentTools` interface:
- `get_claim_details`
- `get_employee_expense_history`
- `get_department_spending_statistics`
- `find_possible_duplicate_claims`
- `create_risk_assessment`
- `create_risk_log`

Every tool performs input validation, operates with restricted read-only projections, and handles exceptions safely.

### Consequences
- **Positive:** Tight least-privilege security model; clear auditability of all agent tool invocations.
- **Negative:** Adding new agent analytical capabilities requires creating explicit new tool contracts.

---

## ADR-004: Unified ASP.NET Core Web API for Web & Mobile Clients

### Status
Accepted

### Context
Both a React web application (Manager Compliance Portal) and a Flutter mobile app (Employee Portal) are required. Bypassing the central backend or creating separate Python/microservice backends violates enterprise architecture guidelines.

### Decision
All web and mobile clients communicate exclusively through the single **ASP.NET Core Web API** running on port 5104. The API handles JWT authentication, enforces role-based access control (`Manager`, `Employee`, `Finance`), and coordinates the agentic subsystem internally.

### Consequences
- **Positive:** Single source of truth for business rules, authorization, and data integrity.
- **Negative:** Requires CORS and flexible endpoint DTOs accommodating both web and mobile client needs.

---

## ADR-005: Fail-Closed Safe-Failure Protocol

### Status
Accepted

### Context
If an agent crashes, experiences network latency, or encounters unexpected schema errors, an unhandled failure could either lock the claim in limbo or inadvertently auto-approve it.

### Decision
We implement a **Fail-Closed Safe-Failure Protocol**. If any tool throws an exception or the agent analysis times out:
1. The error is logged into `AgentExecution` with status `SAFE_FALLBACK`.
2. The agent returns a synthesized `REVIEW_REQUIRED` assessment with risk score 85 and reason `SAFE-FAILURE PAUSE`.
3. The workflow pauses at `WAITING_FOR_MANAGER_APPROVAL`.

### Consequences
- **Positive:** Guaranteed zero risk of accidental auto-approvals during technical failures.
- **Negative:** Minor tool glitches trigger manual review by managers.
