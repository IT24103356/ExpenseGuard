import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:reimbursement_budget/features/compliance/compliance_providers.dart';
import 'package:reimbursement_budget/features/compliance/policy_guidance_screen.dart';

class FakeComplianceRepository implements ComplianceRepository {
  @override
  Future<ComplianceFeedback> evaluate(ComplianceInput input) async => const ComplianceFeedback(
    outcome: 'non_compliant',
    violations: [PolicyViolation(ruleCode: 'RECEIPT_REQUIRED', message: 'Attach a receipt.', severity: 'high')],
  );
}

void main() {
  Widget subject() => ProviderScope(
    overrides: [complianceRepositoryProvider.overrideWithValue(FakeComplianceRepository())],
    child: const MaterialApp(home: PolicyGuidanceScreen()),
  );

  testWidgets('validates claim input before evaluating', (tester) async {
    await tester.pumpWidget(subject());
    await tester.tap(find.text('Check compliance'));
    await tester.pump();
    expect(find.text('Enter a valid numeric claim ID.'), findsOneWidget);
  });

  testWidgets('renders authoritative revision feedback', (tester) async {
    await tester.pumpWidget(subject());
    await tester.enterText(find.byKey(const Key('claimId')), '42');
    await tester.tap(find.text('Check compliance'));
    await tester.pumpAndSettle();
    expect(find.text('Attach a receipt.'), findsOneWidget);
    expect(find.textContaining('RECEIPT_REQUIRED'), findsOneWidget);
  });
}

