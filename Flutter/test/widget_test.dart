import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:reimbursement_budget/main.dart';

void main() {
  testWidgets('App renders without crashing', (WidgetTester tester) async {
    await tester.pumpWidget(const ReimbursementApp());
    expect(find.byType(MaterialApp), findsOneWidget);
  });
}
