# ExpenseGuard employee app

The department budget screen at `/budget` uses Riverpod and accepts
authentication/profile values through provider overrides. Local values can be
supplied with:

```text
--dart-define=EXPENSEGUARD_API_URL=http://localhost:5000/api
--dart-define=EXPENSEGUARD_AUTH_TOKEN=<jwt>
--dart-define=EXPENSEGUARD_DEPARTMENT_BUDGET_ID=<budget-id>
```

The employee ID for existing claim requests is read from the authenticated
JWT (`employee_id`, `employeeId`, or `sub`), never from a hardcoded value.

Run `flutter pub get`, `flutter analyze`, and `flutter test`.

Note: the current backend budget controller excludes the Employee role, so the
screen intentionally presents a forbidden state until a department-scoped,
employee-readable endpoint is available.

## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Learn Flutter](https://docs.flutter.dev/get-started/learn-flutter)
- [Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Flutter learning resources](https://docs.flutter.dev/reference/learning-resources)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.
