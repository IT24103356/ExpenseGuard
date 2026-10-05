import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'features/budget/department_budget_screen.dart';
import 'screens/my_claims_screen.dart';
import 'screens/claim_detail_screen.dart';
import 'screens/reimbursement_status_screen.dart';
import 'screens/payment_status_screen.dart';
import 'screens/history_screen.dart';

void main() {
  runApp(const ProviderScope(child: ReimbursementApp()));
}

class ReimbursementApp extends StatelessWidget {
  const ReimbursementApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'ReimbursementBudget',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
      routerConfig: _router,
    );
  }

  static final _router = GoRouter(
    initialLocation: '/claims',
    routes: [
      GoRoute(path: '/claims', builder: (ctx, state) => const MyClaimsScreen()),
      GoRoute(path: '/claims/:id', builder: (ctx, state) => ClaimDetailScreen(claimId: state.pathParameters['id']!)),
      GoRoute(path: '/reimbursement/:id', builder: (ctx, state) => ReimbursementStatusScreen(reimbursementId: state.pathParameters['id']!)),
      GoRoute(path: '/payment/:id', builder: (ctx, state) => PaymentStatusScreen(reimbursementId: state.pathParameters['id']!)),
      GoRoute(path: '/history', builder: (ctx, state) => const HistoryScreen()),
      GoRoute(path: '/budget', builder: (ctx, state) => const DepartmentBudgetScreen()),
    ],
  );

  ThemeData _buildTheme() {
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: const Color(0xFF6366F1),
        brightness: Brightness.dark,
        primary: const Color(0xFF6366F1),
        secondary: const Color(0xFF8B5CF6),
        surface: const Color(0xFF1A2236),
        onSurface: const Color(0xFFF1F5F9),
      ),
      scaffoldBackgroundColor: const Color(0xFF0A0E1A),
      cardTheme: CardThemeData(
        color: const Color(0xFF1A2236),
        elevation: 0,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: Color(0xFF111827),
        foregroundColor: Color(0xFFF1F5F9),
        elevation: 0,
      ),
    );
  }
}
