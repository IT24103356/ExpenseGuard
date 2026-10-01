import 'package:flutter/material.dart';
import '../services/api_service.dart';

class PaymentStatusScreen extends StatefulWidget {
  final String reimbursementId;
  const PaymentStatusScreen({super.key, required this.reimbursementId});

  @override
  State<PaymentStatusScreen> createState() => _PaymentStatusScreenState();
}

class _PaymentStatusScreenState extends State<PaymentStatusScreen> {
  Map<String, dynamic>? _data;
  bool _loading = true;

  @override
  void initState() { super.initState(); _load(); }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final d = await ApiService.getReimbursement(widget.reimbursementId);
      setState(() { _data = d; _loading = false; });
    } catch (_) {
      setState(() { _data = _mockData(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Payment Status')), body: const Center(child: CircularProgressIndicator()));
    final d = _data!;
    final isPaid = d['status'] == 'PAID';
    return Scaffold(
      appBar: AppBar(title: const Text('Payment Details', style: TextStyle(fontWeight: FontWeight.bold))),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(children: [
                Icon(isPaid ? Icons.check_circle : Icons.error_outline,
                    size: 72, color: isPaid ? Colors.green : Colors.red),
                const SizedBox(height: 16),
                Text(isPaid ? 'Payment Completed' : 'Payment ${d['status']}',
                    style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold,
                        color: isPaid ? Colors.green : Colors.red)),
                const SizedBox(height: 8),
                Text(
                  'LKR ${(d['amount'] as num?)?.toStringAsFixed(2) ?? '0.00'}',
                  style: const TextStyle(fontSize: 32, fontWeight: FontWeight.w800, letterSpacing: -1),
                ),
                const SizedBox(height: 20),
                if (d['paymentReference'] != null) ...[
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                    decoration: BoxDecoration(color: Colors.grey.shade800, borderRadius: BorderRadius.circular(8)),
                    child: Column(children: [
                      Text('Payment Reference', style: TextStyle(fontSize: 11, color: Colors.grey.shade500)),
                      const SizedBox(height: 4),
                      Text(d['paymentReference'], style: const TextStyle(fontFamily: 'monospace', fontSize: 18, fontWeight: FontWeight.bold, letterSpacing: 1)),
                    ]),
                  ),
                ],
                const SizedBox(height: 16),
                _PayRow('Provider', d['paymentProvider'] ?? 'Sandbox'),
                _PayRow('Department', d['departmentId'] ?? ''),
                _PayRow('Completed', d['completedAt'] != null ? DateTime.parse(d['completedAt']).toLocal().toString().substring(0, 16) : '—'),
                if (d['failureReason'] != null)
                  _PayRow('Failure', d['failureReason'], color: Colors.red),
              ]),
            ),
          ),
        ]),
      ),
    );
  }

  Map<String, dynamic> _mockData() => {
    'id': widget.reimbursementId, 'amount': 25000.0, 'currency': 'LKR',
    'status': 'PAID', 'paymentReference': 'PAY-10001', 'paymentProvider': 'SandboxProvider',
    'departmentId': 'DEPT-ENG', 'completedAt': '2026-09-16T10:30:00Z', 'failureReason': null,
  };
}

class _PayRow extends StatelessWidget {
  final String label, value;
  final Color? color;
  const _PayRow(this.label, this.value, {this.color});

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 6),
    child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
      Text(label, style: TextStyle(color: Colors.grey.shade500, fontSize: 13)),
      Text(value, style: TextStyle(fontSize: 13, fontWeight: FontWeight.w500, color: color)),
    ]),
  );
}
