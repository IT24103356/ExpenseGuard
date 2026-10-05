import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'screens/employee_home_screen.dart';
import 'screens/expense_claim_screen.dart';
import 'screens/purchase_request_screen.dart';

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
    initialLocation: '/',
    routes: [
      GoRoute(path: '/', builder: (ctx, state) => const EmployeeHomeScreen()),
      GoRoute(path: '/claims/new', builder: (ctx, state) => const ExpenseClaimScreen()),
      GoRoute(path: '/claims/:id', builder: (ctx, state) => ExpenseClaimScreen(claimId: int.parse(state.pathParameters['id']!))),
      GoRoute(path: '/purchase-requests/new', builder: (ctx, state) => const PurchaseRequestScreen()),
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
