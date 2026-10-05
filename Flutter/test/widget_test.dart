import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:reimbursement_budget/main.dart';
import 'package:reimbursement_budget/models/expense_models.dart';
import 'package:reimbursement_budget/providers/expense_providers.dart';
import 'package:reimbursement_budget/repositories/expense_repository.dart';
import 'dart:typed_data';

void main() {
  testWidgets('employee app renders claim empty state', (WidgetTester tester) async {
    await tester.pumpWidget(ProviderScope(
      overrides: [expenseRepositoryProvider.overrideWithValue(FakeExpenseRepository())],
      child: const ReimbursementApp(),
    ));
    await tester.pumpAndSettle();
    expect(find.byType(MaterialApp), findsOneWidget);
    expect(find.text('No claims match your filters.'), findsOneWidget);
  });
}

class FakeExpenseRepository implements ExpenseRepository {
  @override
  Future<EmployeeProfile> getProfile() async => const EmployeeProfile(id: 1, fullName: 'Test Employee', email: 'test@example.com');
  @override
  Future<List<ExpenseClaim>> getClaims({String? status, String? category}) async => [];
  @override
  Future<List<PurchaseRequest>> getPurchaseRequests() async => [];
  @override
  Future<ExpenseClaim> getClaim(int id) => throw UnimplementedError();
  @override
  Future<List<ClaimHistory>> getHistory(int claimId) async => [];
  @override
  Future<PurchaseRequest> createPurchaseRequest(Map<String, dynamic> draft) => throw UnimplementedError();
  @override
  Future<void> submitPurchaseRequest(int id) async {}
  @override
  Future<ExpenseClaim> saveClaim(ClaimDraft draft, {int? id}) => throw UnimplementedError();
  @override
  Future<void> submitClaim(int id) async {}
  @override
  Future<void> resubmitClaim(int id, String? reason) async {}
  @override
  Future<ReceiptResult> uploadReceipt(int claimId, Uint8List bytes, String fileName) => throw UnimplementedError();
  @override
  Future<ReceiptResult> correctReceipt(int claimId, int receiptId, Map<String, dynamic> correction) => throw UnimplementedError();
}
