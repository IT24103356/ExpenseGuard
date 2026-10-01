# ExpenseGuard

A full-stack corporate expense management platform with a multi-agent AI system that automates policy compliance and fraud checks, routing high-risk claims for human approval before reimbursement.

## Architecture Overview

ExpenseGuard uses a modern, distributed architecture:
- **Backend**: ASP.NET Core Web API (.NET 8), Entity Framework Core, PostgreSQL
- **Web App**: React with Vite, React Router, Recharts, Axios
- **Mobile App**: Flutter / Dart
- **AI Agentic System**: Multi-Agent orchestration including Coordinator/Planner Agent, Policy Compliance Agent, and Fraud Detection Agent
- **Auth**: JWT-based authentication with Role-Based Access Control (`FinanceAdmin`, `Manager`, `Employee`)
- **CI/CD**: GitHub Actions workflow for continuous build and test verification

---

## Repository Structure

```
ExpenseGuard/
├── .github/
│   └── workflows/
│       └── backend-ci.yml           # CI workflow for build and test validation
├── backend/
│   ├── ExpenseGuard.sln             # Unified solution (.NET 8)
│   ├── ExpenseGuard.slnx            # Solution definition
│   ├── ExpenseGuard.Api/            # Core shared API models & context
│   │   ├── Models/                  # Shared domain entities
│   │   └── Data/AppDbContext.cs     # Shared database context
│   ├── ReimbursementBudget.API/     # Reimbursement & Budget Tracking Service
│   │   ├── Agents/                  # Coordinator/Planner Agent
│   │   ├── Controllers/             # RESTful API controllers
│   │   ├── Data/                    # DbContext, Configurations, Migrations, Seed
│   │   ├── DTOs/                    # Request/Response data transfer objects
│   │   ├── Infrastructure/Payment/  # Mock payment gateway sandbox
│   │   ├── Services/                # Business logic services
│   │   └── Tools/                   # Agent tool execution toolbox
│   └── ReimbursementBudget.Tests/   # Unit & Integration Tests (20 tests)
├── Docs/
│   ├── ADR.md                       # Architecture Decision Records
│   ├── CoordinatorAgent.md          # Coordinator/Planner Agent specification
│   └── Testing.md                   # Comprehensive testing guide & test matrix
├── Flutter/                         # Flutter Mobile App for Employees
│   ├── lib/
│   │   ├── screens/                 # Mobile claim submission & tracking screens
│   │   └── services/                # Backend API integration service
│   └── pubspec.yaml
└── React/                           # React Web Dashboard for Finance Admins
    ├── src/
    │   ├── components/              # UI widgets and layouts
    │   ├── pages/                   # Finance & Budget dashboards, reports, workflow monitor
    │   └── services/api.js          # API service client with JWT interceptor
    └── package.json
```

---

## Getting Started

### 1. Backend (.NET 8 Web API)

**Prerequisites:** .NET 8 SDK, PostgreSQL (or InMemory for testing)

```bash
cd backend
dotnet restore ExpenseGuard.sln
dotnet build ExpenseGuard.sln
dotnet test ExpenseGuard.sln
```

To run the Reimbursement & Budget Tracking API:
```bash
cd backend/ReimbursementBudget.API
dotnet run
```
Swagger UI available at: `http://localhost:5000/swagger`

### 2. React Web App

**Prerequisites:** Node.js 18+

```bash
cd React
npm install
npm run dev
```
Dashboard available at: `http://localhost:5173`

### 3. Flutter Mobile App

**Prerequisites:** Flutter SDK 3.x

```bash
cd Flutter
flutter pub get
flutter run
```

---

## Business Component: Reimbursement & Budget Tracking

**Owner / Student ID:** IT24101739  
**Agentic AI Responsibility:** Coordinator/Planner Agent

- **Workflow Coordination:** Coordinates the end-to-end expense lifecycle from policy validation and fraud detection delegation to human approval routing, finance batch processing, and payment execution.
- **Budget Tracking:** Enforces department budget caps, reservation mechanisms, utilization alerts, and fiscal period resets.
- **Payment Sandbox:** Simulates asynchronous payment disbursement with realistic latency, idempotency keys, and transaction references.
