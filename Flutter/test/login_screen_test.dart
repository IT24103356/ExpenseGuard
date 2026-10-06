import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:reimbursement_budget/auth/auth_provider.dart';
import 'package:reimbursement_budget/screens/login_screen.dart';
import 'package:reimbursement_budget/services/api_service.dart';

void main() {
  testWidgets('login matches the web sign-in copy and has no social register', (tester) async {
    await tester.pumpWidget(ProviderScope(
      overrides: [
        sessionStorageProvider.overrideWithValue(MemoryStorage()),
        unauthenticatedApiProvider.overrideWithValue(FakeApi()),
      ],
      child: const MaterialApp(home: LoginScreen()),
    ));
    await tester.pumpAndSettle();

    expect(find.text('ExpenseGuard'), findsOneWidget);
    expect(find.text('Sign in'), findsWidgets);
    expect(find.widgetWithText(TextFormField, 'Username'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Password'), findsOneWidget);
    expect(find.text('Receipt review'), findsOneWidget);
    expect(find.text('Google'), findsNothing);
    expect(find.text('Register'), findsNothing);
    expect(find.text('Create account'), findsNothing);
  });
}

class MemoryStorage implements SessionStorage {
  String? value;
  @override
  Future<void> clear() async => value = null;
  @override
  Future<String?> read() async => value;
  @override
  Future<void> write(String newValue) async => value = newValue;
}

class FakeApi extends ApiService {
  @override
  Future<Map<String, dynamic>> login(String username, String password) async => {
    'token': 'test',
    'expiresAt': '2099-01-01T00:00:00.000Z',
    'employeeId': 7,
    'username': username,
    'role': 'Employee',
  };
}
