# Policy & Compliance Module — Web API Specification

## Overview
The **Policy & Compliance API** is built on **ASP.NET Core Web API** and connects to a normalized **PostgreSQL** database via **Entity Framework Core**. It serves both the React Web Application (Manager Compliance Portal) and the Flutter Mobile Application (Employee Portal).

- **Base URL:** `http://localhost:5104/api`
- **Swagger Documentation:** `http://localhost:5104/swagger`
- **Authentication:** JWT Bearer tokens in `Authorization: Bearer <token>`
- **Authorization:** Role-Based Access Control (`Manager`, `Employee`, `Finance`, `Admin`)

---

## 1. Authentication Endpoints

### 1.1. Login
- **Endpoint:** `POST /api/auth/login`
- **Access:** Public
- **Request Body:**
```json
{
  "username": "sarah.chen",
  "password": "Password123!"
}
```
- **Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "44444444-4444-4444-4444-444444444444",
  "username": "sarah.chen",
  "fullName": "Sarah Chen",
  "role": "Manager",
  "departmentId": "11111111-1111-1111-1111-111111111111",
  "departmentName": "Sales & Marketing"
}
```

---

## 2. Manager Review Queue & Compliance Endpoints

### 2.1. Compliance Dashboard Overview Stats
- **Endpoint:** `GET /api/policy-compliance/stats/compliance-dashboard`
- **Access:** `[Authorize(Roles = "Manager,Finance,Admin")]`
- **Response (200 OK):**
```json
{
  "totalPendingReviews": 4,
  "highRiskClaims": 2,
  "policyViolationsCount": 2,
  "awaitingRevisionCount": 1,
  "totalApproved": 2,
  "totalRejected": 0,
  "totalPendingAmount": 161500.00,
  "recentHighRiskClaims": [ ... ],
  "recentActivities": [ ... ]
}
```

### 2.2. Manager Review Queue
- **Endpoint:** `GET /api/policy-compliance/review-queue`
- **Access:** `[Authorize(Roles = "Manager,Admin")]`
- **Query Parameters:** `riskLevel`, `policyStatus`, `status`, `departmentId`, `category`, `minAmount`, `maxAmount`, `searchTerm`, `page`, `pageSize`
- **Response (200 OK):**
```json
[
  {
    "id": "c3333333-3333-3333-3333-333333333333",
    "claimNumber": "CLM-2026-1003",
    "employeeId": "66666666-6666-6666-6666-666666666666",
    "employeeName": "John Doe",
    "departmentId": "11111111-1111-1111-1111-111111111111",
    "departmentName": "Sales & Marketing",
    "claimDate": "2026-09-20T10:00:00Z",
    "totalAmount": 25000.00,
    "currency": "LKR",
    "merchantName": "ABC Hotel",
    "category": "Hotel",
    "status": "WAITING_FOR_MANAGER_APPROVAL",
    "policyStatus": "COMPLIANT",
    "riskStatus": "REVIEW_REQUIRED",
    "riskScore": 92,
    "violationCount": 0,
    "duplicateCount": 1
  }
]
```

### 2.3. Claim Details
- **Endpoint:** `GET /api/policy-compliance/claims/{id}`
- **Access:** `[Authorize]`
- **Response (200 OK):** Returns complete claim aggregate including line items, deterministic violations, risk assessment, duplicate candidates, review decisions, and audit history.

### 2.4. Claim Risk Details
- **Endpoint:** `GET /api/policy-compliance/claims/{id}/risk`
- **Access:** `[Authorize(Roles = "Manager,Finance,Admin")]`
- **Response (200 OK):**
```json
{
  "claimId": "c3333333-3333-3333-3333-333333333333",
  "claimNumber": "CLM-2026-1003",
  "riskScore": 92,
  "riskStatus": "REVIEW_REQUIRED",
  "riskAssessment": {
    "id": "...",
    "riskScore": 92,
    "riskLevel": "REVIEW_REQUIRED",
    "duplicateDetected": true,
    "unusualAmountDetected": false,
    "suspiciousPatternDetected": false,
    "reasonSummary": "CRITICAL: Exact duplicate claim identified matching approved Claim #CLM-2026-1002.",
    "signals": [
      {
        "type": "DUPLICATE",
        "severity": "CRITICAL",
        "evidence": "Exact duplicate detected against Claim #CLM-2026-1002. Matching parameters: Employee, Merchant (ABC Hotel), Date (2026-09-20), and Amount (25,000.00)."
      }
    ],
    "agentVersion": "FraudAnomalyRiskAgent-v1.2",
    "createdAt": "..."
  },
  "duplicateMatches": [
    {
      "matchingClaimId": "b2222222-2222-2222-2222-222222222222",
      "matchingClaimNumber": "CLM-2026-1002",
      "similarityScore": 1.00,
      "matchType": "EXACT",
      "evidence": "Exact duplicate detected: Same Employee (John Doe), Merchant (ABC Hotel), Date (2026-09-20), and Amount (25,000.00 LKR)."
    }
  ]
}
```

### 2.5. Claim Policy Checks
- **Endpoint:** `GET /api/policy-compliance/claims/{id}/policy-checks`
- **Access:** `[Authorize]`
- **Response (200 OK):**
```json
{
  "claimId": "d4444444-4444-4444-4444-444444444444",
  "claimNumber": "CLM-2026-1004",
  "policyStatus": "VIOLATIONS_FOUND",
  "violations": [
    {
      "id": "...",
      "ruleCode": "RULE_MAX_AMOUNT_EXCEEDED",
      "severity": "HIGH",
      "message": "Claim amount (65,000.00 LKR) exceeds the policy maximum limit of 15,000.00 LKR for 'Meals'.",
      "actualValue": "65,000.00 LKR",
      "allowedValue": "15,000.00 LKR"
    }
  ]
}
```

### 2.6. Claim Audit Trail
- **Endpoint:** `GET /api/policy-compliance/claims/{id}/audit`
- **Access:** `[Authorize]`
- **Response (200 OK):** Chronological array of audit logs.

---

## 3. Manager Approval Actions

### 3.1. Approve Claim
- **Endpoint:** `POST /api/policy-compliance/claims/{id}/approve`
- **Access:** `[Authorize(Roles = "Manager,Admin")]`
- **Request Body:**
```json
{
  "comment": "Verified client banquet attendance. Approved under manager discretion."
}
```
- **Response (200 OK):** Updated `ExpenseClaimDetailDto` with status `APPROVED`.

### 3.2. Reject Claim
- **Endpoint:** `POST /api/policy-compliance/claims/{id}/reject`
- **Access:** `[Authorize(Roles = "Manager,Admin")]`
- **Request Body (Reason is mandatory):**
```json
{
  "comment": "Rejected due to duplicate claim matching previously reimbursed invoice."
}
```
- **Response (200 OK):** Updated `ExpenseClaimDetailDto` with status `REJECTED`.

### 3.3. Request Revision
- **Endpoint:** `POST /api/policy-compliance/claims/{id}/request-revision`
- **Access:** `[Authorize(Roles = "Manager,Admin")]`
- **Request Body (Instructions are mandatory):**
```json
{
  "comment": "Please attach your itemized boarding pass and travel approval email."
}
```
- **Response (200 OK):** Updated `ExpenseClaimDetailDto` with status `REVISION_REQUIRED`.

---

## 4. Employee & Finance Endpoints

### 4.1. Submit Claim
- **Endpoint:** `POST /api/policy-compliance/claims`
- **Access:** `[Authorize(Roles = "Employee,Manager,Admin")]`
- **Behavior:** Automatically executes deterministic policy validation engine and the Fraud/Anomaly-Risk Agent in an orchestrated pipeline. Workflow pauses at `WAITING_FOR_MANAGER_APPROVAL` if violations or high risk are found.

### 4.2. Resubmit Claim
- **Endpoint:** `PUT /api/policy-compliance/claims/{id}/resubmit`
- **Access:** `[Authorize(Roles = "Employee,Manager,Admin")]`
- **Behavior:** Accepts revised line items, receipt documents, and employee revision notes. Resets status to `SUBMITTED` and re-runs the policy validation and agent evaluation pipeline.

### 4.3. Reimburse Claim (Finance)
- **Endpoint:** `POST /api/policy-compliance/claims/{id}/reimburse`
- **Access:** `[Authorize(Roles = "Finance,Admin")]`
- **Behavior:** Marks an `APPROVED` claim as `REIMBURSED` with finance wire reference.
