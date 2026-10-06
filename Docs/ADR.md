# Architecture Decision Records

## ADR 1 — One public API, one PostgreSQL database

ExpenseGuard.Api is the only public ASP.NET host. React and Flutter call it only. PostgreSQL holds domain state. The Python LangGraph service is internal and may change domain data only through authenticated ASP.NET endpoints.

## ADR 2 — Shared identity and four owned verticals

JWT roles are Employee, Manager, DepartmentHead, Finance, and Admin. Intake, policy/fraud, budget, and reimbursement remain owned slices behind that shared identity. High-value advertising claims require Manager, then Department Head, then Finance.

## ADR 3 — Deployed topology

Local and container development use:

- PostgreSQL 16
- ExpenseGuard.Api on port 5000
- expenseguard-agents on port 8088
- React on 5173, Flutter against the same API

Development startup applies EF migrations and demo seed data (four role accounts, Advertising policy, Marketing budget). Secrets stay in environment variables or user secrets, never in git.
