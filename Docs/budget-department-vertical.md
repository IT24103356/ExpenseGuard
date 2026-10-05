# Budget and Department vertical

The canonical runtime is `backend/ExpenseGuard.Api`; it owns the PostgreSQL
tables and all authoritative decimal balance changes. The monitoring package
under `ai/expenseguard-agents` is read-only and returns structured observations.

## API surface

- `GET|POST /api/departments`, `GET|PUT|DELETE /api/departments/{id}`
- `POST /api/budgets`, `GET /api/budgets/{id}`
- `GET /api/budgets/{id}/availability?amount=...`
- `POST /api/budgets/{id}/reserve|release|spend`
- `GET /api/budgets/{id}/transactions|alerts`
- `GET /api/budgets/reports/utilization`

Mutation requests require an idempotency key and current version. A replay with
the same budget/key returns the current budget without applying the amount
again. A stale version returns HTTP 409. Insufficient funds return HTTP 409;
invalid balance transitions return HTTP 422.

Roles are `Admin`, `Finance`, and `Manager`. Authentication must issue a role
claim understood by ASP.NET Core.

## Environment

- `ConnectionStrings__Default`: PostgreSQL connection string.
- `Jwt__Key`: JWT signing key (required, inject through a secret store).
- `Jwt__Issuer`: token issuer; defaults to `ExpenseGuard`.
- `Jwt__Audience`: token audience; defaults to `ExpenseGuard`.
- `EXPENSEGUARD_GEMINI_API_KEY`: optional monitoring-agent enrichment key.
- `EXPENSEGUARD_GEMINI_MODEL`: reserved model name for future enrichment.

No Gemini key is required for local deterministic monitoring or tests. The
agent does not currently send data to Gemini.

## Local verification

```powershell
dotnet test backend/ExpenseGuard.Api.Tests/ExpenseGuard.Api.Tests.csproj
python -m pip install -e "ai/expenseguard-agents[test]"
python -m pytest ai/expenseguard-agents/tests
```

Create/apply migrations only against a developer-owned database. Never point
`dotnet ef database update` at shared environments.

## Clients

The React finance client uses TanStack Query for server state. Budget allocation,
utilization reports, transaction history, alert review, and department
administration are available at `/budgets` and `/admin/departments`. Set
`VITE_API_URL` to the API root (for example `http://localhost:5000/api`).
Authentication remains owned by the shared shell; its JWT is read from
`jwt_token`.

The Flutter employee view is available at `/budget` and uses Riverpod providers
that the owning authentication/profile layer can override:

- `apiBaseUrlProvider`
- `authTokenProvider`
- `departmentBudgetIdProvider`

For local builds these can be seeded with `--dart-define=EXPENSEGUARD_API_URL=...`,
`EXPENSEGUARD_AUTH_TOKEN`, and `EXPENSEGUARD_DEPARTMENT_BUDGET_ID`. Employee IDs
are derived from authenticated JWT claims for the existing claims screens and
are not hardcoded.

The current API authorizes only `Admin`, `Finance`, and `Manager` on
`/api/budgets`. An employee JWT therefore receives a handled HTTP 403 until the
backend exposes a least-privilege employee budget-context/availability endpoint
or extends authorization with department scoping.

```powershell
cd React
npm install
npm run lint
npm test
npm run build

cd ..\Flutter
flutter pub get
flutter analyze
flutter test
```
