import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../services/api_service.dart';

class MyClaimsScreen extends StatefulWidget {
  const MyClaimsScreen({super.key});
  @override
  State<MyClaimsScreen> createState() => _MyClaimsScreenState();
}

class _MyClaimsScreenState extends State<MyClaimsScreen> {
  List<dynamic> _claims = [];
  bool _loading = true;
  static const String _employeeId = String.fromEnvironment('EMPLOYEE_ID');

  @override
  void initState() {
    super.initState();
    _loadClaims();
  }

  Future<void> _loadClaims() async {
    setState(() => _loading = true);
    if (_employeeId.isEmpty) {
      setState(() { _claims = []; _loading = false; });
      return;
    }
    try {
      final data = await ApiService.getEmployeeReimbursements(_employeeId);
      setState(() { _claims = data; _loading = false; });
    } catch (_) {
      setState(() { _claims = _mockClaims(_employeeId); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Claims', style: TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          IconButton(icon: const Icon(Icons.policy_outlined), tooltip: 'Policy guidance', onPressed: () => context.go('/policy-guidance')),
          IconButton(icon: const Icon(Icons.history), onPressed: () => context.go('/history')),
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadClaims),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _claims.isEmpty
              ? _buildEmpty()
              : RefreshIndicator(
                  onRefresh: _loadClaims,
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: _claims.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 12),
                    itemBuilder: (ctx, i) => _ClaimCard(claim: _claims[i]),
                  ),
                ),
    );
  }

  Widget _buildEmpty() => Center(
    child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
      Icon(_employeeId.isEmpty ? Icons.lock_outline : Icons.receipt_long_outlined, size: 64, color: Colors.grey.shade600),
      const SizedBox(height: 16),
      Text(_employeeId.isEmpty ? 'Sign in to view your claims' : 'No claims found', style: TextStyle(color: Colors.grey.shade500, fontSize: 16)),
    ]),
  );
}

class _ClaimCard extends StatelessWidget {
  final dynamic claim;
  const _ClaimCard({required this.claim});

  @override
  Widget build(BuildContext context) {
    final status = claim['status'] as String? ?? 'UNKNOWN';
    final color = _statusColor(status);
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.go('/reimbursement/${claim['id']}'),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
              Text('Claim #${(claim['expenseClaimId'] as String? ?? '').substring(0, 8)}…',
                  style: const TextStyle(fontFamily: 'monospace', fontSize: 12, color: Colors.grey)),
              _StatusChip(status: status, color: color),
            ]),
            const SizedBox(height: 10),
            Text(
              _formatAmount(claim['amount'], claim['currency']),
              style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800, letterSpacing: -0.5),
            ),
            const SizedBox(height: 4),
            Row(children: [
              Icon(Icons.business_outlined, size: 14, color: Colors.grey.shade500),
              const SizedBox(width: 4),
              Text(claim['departmentId'] ?? '', style: TextStyle(fontSize: 12, color: Colors.grey.shade500)),
              const Spacer(),
              Text(_formatDate(claim['requestedAt']), style: TextStyle(fontSize: 11, color: Colors.grey.shade600)),
            ]),
            if (claim['paymentReference'] != null) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: Colors.green.shade900.withOpacity(0.3),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text('Ref: ${claim['paymentReference']}',
                    style: TextStyle(fontFamily: 'monospace', fontSize: 11, color: Colors.green.shade300)),
              ),
            ],
          ]),
        ),
      ),
    );
  }

  Color _statusColor(String s) => switch (s) {
    'PAID' => Colors.green,
    'PROCESSING' || 'PAYMENT_PENDING' => Colors.indigo,
    'PAYMENT_FAILED' || 'REJECTED' => Colors.red,
    'BUDGET_REVIEW_REQUIRED' => Colors.blue,
    _ => Colors.amber,
  };

  String _formatAmount(dynamic amount, dynamic currency) =>
      '${currency ?? 'LKR'} ${_formatNum(amount)}';

  String _formatNum(dynamic n) {
    if (n == null) return '0.00';
    final num val = n is num ? n : num.tryParse(n.toString()) ?? 0;
    return val.toStringAsFixed(2).replaceAllMapped(
        RegExp(r'(\d)(?=(\d{3})+\.)'), (m) => '${m[1]},');
  }

  String _formatDate(dynamic d) {
    if (d == null) return '';
    try {
      return DateTime.parse(d).toLocal().toString().substring(0, 10);
    } catch (_) { return d.toString(); }
  }
}

class _StatusChip extends StatelessWidget {
  final String status;
  final Color color;
  const _StatusChip({required this.status, required this.color});

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
    decoration: BoxDecoration(
      color: color.withOpacity(0.15),
      borderRadius: BorderRadius.circular(20),
      border: Border.all(color: color.withOpacity(0.4)),
    ),
    child: Text(status.replaceAll('_', ' '),
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: color, letterSpacing: 0.5)),
  );
}

List<dynamic> _mockClaims(String employeeId) => [
  { 'id': '11111111-1111-1111-1111-111111111111', 'expenseClaimId': 'a1b2c3d4-1234-5678-abcd-ef1234567890', 'employeeId': employeeId, 'departmentId': 'DEPT-ENG', 'amount': 25000.0, 'currency': 'LKR', 'status': 'PAID', 'requestedAt': '2026-09-15T10:00:00Z', 'paymentReference': 'PAY-10001' },
  { 'id': '22222222-2222-2222-2222-222222222222', 'expenseClaimId': 'b2c3d4e5-2345-6789-bcde-f12345678901', 'employeeId': employeeId, 'departmentId': 'DEPT-ENG', 'amount': 75000.0, 'currency': 'LKR', 'status': 'WAITING_FOR_APPROVAL', 'requestedAt': '2026-09-20T08:00:00Z', 'paymentReference': null },
  { 'id': '33333333-3333-3333-3333-333333333333', 'expenseClaimId': 'c3d4e5f6-3456-789a-cdef-123456789012', 'employeeId': employeeId, 'departmentId': 'DEPT-ENG', 'amount': 12500.0, 'currency': 'LKR', 'status': 'PAYMENT_FAILED', 'requestedAt': '2026-09-10T14:00:00Z', 'paymentReference': 'PAY-10002' },
];
