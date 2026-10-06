# Fraud / Anomaly-Risk Agent Specification & Design

## 1. Primary Responsibility
The **Fraud/Anomaly-Risk Agent** (`FraudAnomalyRiskAgent`) is an agentic AI subsystem in the Corporate Spend Management platform. Its core responsibility is:

> *"Analyze an expense claim for duplicate claims, unusual amounts, and suspicious spending patterns, then return a structured risk assessment that can trigger a mandatory human approval pause."*

### Key Governance Principles:
- **No Direct Financial Approval or Rejection:** The agent never directly approves or rejects a financial reimbursement. Final authority remains exclusively with authorized human managers.
- **Recommendations Only:** The agent outputs recommendations: `LOW_RISK`, `MEDIUM_RISK`, `HIGH_RISK`, or `REVIEW_REQUIRED`.
- **Hybrid Architecture:** Deterministic business logic performs strict validation first (limits, receipts, currencies, allowed categories); the agent then analyzes higher-order anomalies, velocity clustering, threshold evasions, and statistical outliers.

---

## 2. Controlled Allow-Listed Tools
The agent does **not** have uncontrolled database access. It interacts strictly through allow-listed, permission-scoped tools with typed input/output contracts and error handling:

| Tool Name | Scope & Purpose | Input | Output |
| :--- | :--- | :--- | :--- |
| `get_claim_details` | Loads validated claim metadata, items, and department context | `claimId: Guid` | `ExpenseClaim` (projection) |
| `get_employee_expense_history` | Retrieves employee's historical category spending and status history | `employeeId: Guid` | `List<AgentClaimHistoryItem>` |
| `get_department_spending_statistics` | Retrieves baseline spending statistics (mean, median, std-dev, p90) | `departmentId: Guid`, `category: string` | `AgentDepartmentCategoryStats` |
| `find_possible_duplicate_claims` | Runs deterministic matching across historical non-rejected claims | `claimId: Guid` | `List<AgentDuplicateCandidate>` |
| `create_risk_assessment` | Persists structured risk level, score (0-100), and detected signals | `claimId: Guid`, `AgentRiskAssessmentOutput` | `RiskAssessment` entity |
| `create_risk_log` | Records individual anomaly signals into the compliance audit log | `claimId: Guid`, `reason: string`, `severity: string` | `AuditLog` entity |

---

## 3. Agent Input Contract
The agent operates on structured data representing the expense claim and localized spending context:

```json
{
  "claimId": "c3333333-3333-3333-3333-333333333333",
  "employeeId": "66666666-6666-6666-6666-666666666666",
  "departmentId": "11111111-1111-1111-1111-111111111111",
  "amount": 25000.00,
  "currency": "LKR",
  "category": "Hotel",
  "merchant": "ABC Hotel",
  "expenseDate": "2026-09-20T10:00:00Z",
  "description": "Sales summit overnight lodging",
  "historicalClaims": [ ... ],
  "departmentStatistics": [ ... ],
  "possibleDuplicates": [ ... ]
}
```

---

## 4. Anomaly & Fraud Detection Algorithms

### 4.1. Deterministic Duplicate Matching
Claims are compared against historical non-rejected records using multi-field matching:
- **Exact Match:** Same Employee + Same Merchant + Same Date + Same Amount $\rightarrow$ `DuplicateMatchType.EXACT` (Similarity 1.00).
- **Document Match:** Identical receipt file or invoice URL attached across claims $\rightarrow$ `DuplicateMatchType.EXACT` (Similarity 0.98).
- **Partial Match:** Composite score based on employee match (+0.20), identical amount (+0.35), same merchant (+0.25), date within 48 hours (+0.15), and description word overlap/Jaccard similarity $\ge 0.70$ (+0.15).

### 4.2. Statistical Amount Outlier Detection
Compares current spending with historical category distribution:
- **Z-Score Formula:**
  $$Z = \frac{X - \mu}{\sigma}$$
  Where $X$ is claim amount, $\mu$ is employee's historical category average, and $\sigma$ is standard deviation.
  - If $Z \ge 2.5\sigma$: Flagged as `UNUSUAL_AMOUNT` with `HIGH` severity.
  - If $Z \ge 1.8\sigma$: Flagged as `UNUSUAL_AMOUNT` with `MEDIUM` severity.
  - If $X > 2.5 \times \mu$ (when sample size is small): Flagged as significant outlier.
- **Department 90th Percentile Check:**
  If claim exceeds the department 90th percentile threshold ($P_{90}$) for that category, an elevated risk flag is generated.

### 4.3. Suspicious Pattern Detection
- **Threshold Proximity Evasion:** Detects claims clustered between 1% and 5% immediately below managerial escalation limits (e.g., claiming 49,500 LKR when 50,000 LKR triggers senior director approval).
- **Merchant Velocity Clustering:** Detects $\ge 3$ claims to the same merchant within a short temporal window.

---

## 5. Risk Scoring Rubric & Levels

| Composite Score | Risk Level | Workflow Consequence |
| :---: | :---: | :--- |
| **0 – 34** | `LOW_RISK` | Compliant claim; proceeds to standard manager sign-off or auto-approval. |
| **35 – 59** | `MEDIUM_RISK` | Flagged for advisory manager attention during queue review. |
| **60 – 79** | `HIGH_RISK` | Workflow halts; managerial sign-off mandated with highlighted signals. |
| **80 – 100** | `REVIEW_REQUIRED` | Exact duplicate, extreme outlier, or agent safe-fallback; pauses workflow. |

---

## 6. Safe-Failure Guarantee (Fail-Closed)
If an unexpected exception occurs (e.g. database timeout, missing historical records, tool failure, or unparseable output):
- The agent **never** fails open (it **never** auto-approves).
- It generates a `SAFE_FALLBACK` result:
  - `RiskScore`: 85
  - `RiskLevel`: `REVIEW_REQUIRED`
  - `Signals`: `[ { "type": "SAFE_FALLBACK", "severity": "HIGH", "evidence": "Safe-failure engaged due to tool exception." } ]`
- The workflow state machine transitions to `WAITING_FOR_MANAGER_APPROVAL`, ensuring human oversight.
