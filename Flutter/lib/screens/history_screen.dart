import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../services/api_service.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({super.key});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  List<dynamic> _history = [];
  bool _loading = true;
  final String _employeeId = 'EMP-001';

  @override
  void initState() { super.initState(); _load(); }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final data = await ApiService.getReimbursementHistory(_employeeId);
      setState(() { _history = data.where((d) => d['status'] == 'PAID').toList(); _loading = false; });
    } catch (_) {
      setState(() { _history = _mockHistory(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final totalPaid = _history.fold<double>(0, (sum, h) => sum + ((h['amount'] as num?)?.toDouble() ?? 0));
    return Scaffold(
      appBar: AppBar(
        title: const Text('Reimbursement History', style: TextStyle(fontWeight: FontWeight.bold)),
        leading: IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.go('/claims')),
      ),
      body: Column(children: [
        // Summary Banner
        Container(
          width: double.infinity, padding: const EdgeInsets.all(20),
          decoration: const BoxDecoration(gradient: LinearGradient(colors: [Color(0xFF6366F1), Color(0xFF8B5CF6)])),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('Total Reimbursed', style: TextStyle(color: Colors.white70, fontSize: 13)),
            const SizedBox(height: 4),
            Text('LKR ${totalPaid.toStringAsFixed(2)}', style: const TextStyle(color: Colors.white, fontSize: 26, fontWeight: FontWeight.w800)),
            Text('${_history.length} paid reimbursements', style: const TextStyle(color: Colors.white70, fontSize: 12)),
          ]),
        ),
        // History List
        Expanded(
          child: _loading
              ? const Center(child: CircularProgressIndicator())
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _history.length,
                  itemBuilder: (ctx, i) {
                    final h = _history[i];
                    return Card(
                      margin: const EdgeInsets.only(bottom: 10),
                      child: ListTile(
                        leading: const CircleAvatar(
                          backgroundColor: Color(0xFF10B981),
                          child: Icon(Icons.check, color: Colors.white, size: 18),
                        ),
                        title: Text('LKR ${(h['amount'] as num?)?.toStringAsFixed(2) ?? '0.00'}',
                            style: const TextStyle(fontWeight: FontWeight.bold)),
                        subtitle: Text('${h['paymentReference'] ?? '—'} • ${h['departmentId'] ?? ''}'),
                        trailing: Text(h['completedAt']?.substring(0, 10) ?? '', style: TextStyle(fontSize: 11, color: Colors.grey.shade500)),
                        onTap: () => context.go('/reimbursement/${h['id']}'),
                      ),
                    );
                  },
                ),
        ),
      ]),
    );
  }

  List<dynamic> _mockHistory() => [
    { 'id': '1', 'amount': 25000.0, 'departmentId': 'DEPT-ENG', 'status': 'PAID', 'paymentReference': 'PAY-10001', 'completedAt': '2026-09-16T10:30:00Z' },
    { 'id': '2', 'amount': 12500.0, 'departmentId': 'DEPT-ENG', 'status': 'PAID', 'paymentReference': 'PAY-10003', 'completedAt': '2026-08-20T14:00:00Z' },
    { 'id': '3', 'amount': 5000.0, 'departmentId': 'DEPT-ENG', 'status': 'PAID', 'paymentReference': 'PAY-10004', 'completedAt': '2026-07-10T09:00:00Z' },
  ];
}
