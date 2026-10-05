import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/expense_models.dart';
import '../repositories/expense_repository.dart';

const apiBaseUrl = String.fromEnvironment(
  'EXPENSE_GUARD_API_URL',
  defaultValue: 'http://10.0.2.2:5000/api',
);

final authSessionProvider = Provider<AuthSession>((ref) => const EnvironmentAuthSession());
final expenseRepositoryProvider = Provider<ExpenseRepository>((ref) => HttpExpenseRepository(
  baseUrl: apiBaseUrl,
  auth: ref.watch(authSessionProvider),
));

final profileProvider = FutureProvider<EmployeeProfile>((ref) =>
  ref.watch(expenseRepositoryProvider).getProfile());
final purchaseRequestsProvider = FutureProvider<List<PurchaseRequest>>((ref) =>
  ref.watch(expenseRepositoryProvider).getPurchaseRequests());

class ClaimFilter {
  const ClaimFilter({this.status, this.category});
  final String? status;
  final String? category;
}

final claimFilterProvider = StateProvider<ClaimFilter>((ref) => const ClaimFilter());
final claimsProvider = FutureProvider<List<ExpenseClaim>>((ref) {
  final filter = ref.watch(claimFilterProvider);
  return ref.watch(expenseRepositoryProvider).getClaims(status: filter.status, category: filter.category);
});
final claimProvider = FutureProvider.family<ExpenseClaim, int>((ref, id) =>
  ref.watch(expenseRepositoryProvider).getClaim(id));
final claimHistoryProvider = FutureProvider.family<List<ClaimHistory>, int>((ref, id) =>
  ref.watch(expenseRepositoryProvider).getHistory(id));
