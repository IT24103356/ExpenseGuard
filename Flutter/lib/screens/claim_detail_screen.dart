import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../services/api_service.dart';

class ClaimDetailScreen extends StatefulWidget {
  final String claimId;
  const ClaimDetailScreen({super.key, required this.claimId});

  @override
  State<ClaimDetailScreen> createState() => _ClaimDetailScreenState();
}

class _ClaimDetailScreenState extends State<ClaimDetailScreen> {
  Map<String, dynamic>? _reimb;
  Map<String, dynamic>? _workflow;
  bool _loading = true;

  @override
  void initState() { super.initState(); _load(); }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final reimb = await ApiService.getReimbursementByClaim(widget.claimId);
      setState(() => _reimb = reimb);
      final wf = await ApiService.getWorkflowByClaim(widget.claimId);
      setState(() { _workflow = wf; _loading = false; });
    } catch (_) {
      setState(() { _reimb = _mockReimb(); _workflow = _mockWorkflow(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Claim Details')), body: const Center(child: CircularProgressIndicator()));
    final d = _reimb!;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Claim Details', style: TextStyle(fontWeight: FontWeight.bold)),
        leading: IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.go('/claims')),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('Claim Information', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
            const SizedBox(height: 12),
            _Row('Claim ID', widget.claimId.substring(0, 16) + '…'),
            _Row('Amount', 'LKR ${(d['amount'] as num?)?.toStringAsFixed(2)}'),
            _Row('Status', d['status']),
            _Row('Department', d['departmentId']),
            _Row('Requested', d['requestedAt']?.substring(0, 10) ?? ''),
          ]))),
          const SizedBox(height: 12),
          if (_workflow != null) Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('Workflow Status', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
            const SizedBox(height: 4),
            Text('${_workflow!['workflowId']}', style: TextStyle(fontSize: 12, color: Colors.grey.shade500, fontFamily: 'monospace')),
            const SizedBox(height: 12),
            ...((_workflow!['steps'] as List?) ?? []).map((s) => _StepRow(step: s)),
          ]))),
          const SizedBox(height: 16),
          OutlinedButton.icon(
            onPressed: () => context.go(int.tryParse(widget.claimId) == null
                ? '/policy-guidance'
                : '/policy-guidance?claimId=${Uri.encodeQueryComponent(widget.claimId)}'),
            icon: const Icon(Icons.policy_outlined),
            label: const Text('Review compliance and revise'),
          ),
          const SizedBox(height: 8),
          if (d['status'] == 'PAID')
            ElevatedButton.icon(
              onPressed: () => context.go('/payment/${d['id']}'),
              icon: const Icon(Icons.receipt_long),
              label: const Text('View Payment'),
              style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF6366F1), foregroundColor: Colors.white, minimumSize: const Size(double.infinity, 48), shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12))),
            ),
        ],
      ),
    );
  }

  Map<String, dynamic> _mockReimb() => { 'id': 'mock-id', 'amount': 25000.0, 'currency': 'LKR', 'status': 'PAID', 'departmentId': 'DEPT-ENG', 'requestedAt': '2026-09-15T10:00:00Z' };
  Map<String, dynamic> _mockWorkflow() => {
    'workflowId': 'WF-10001', 'status': 'WAITING_FOR_APPROVAL',
    'steps': [
      { 'stepName': 'INTAKE', 'status': 'COMPLETED', 'agentName': 'ExpenseExtractionAgent' },
      { 'stepName': 'POLICY_CHECK', 'status': 'COMPLETED', 'agentName': 'PolicyComplianceAgent' },
      { 'stepName': 'RISK_CHECK', 'status': 'COMPLETED', 'agentName': 'FraudAnomalyRiskAgent' },
      { 'stepName': 'HUMAN_APPROVAL', 'status': 'WAITING_FOR_HUMAN', 'agentName': 'Human Manager' },
      { 'stepName': 'BUDGET_CHECK', 'status': 'PENDING', 'agentName': 'BudgetAgent' },
      { 'stepName': 'PAYMENT', 'status': 'PENDING', 'agentName': 'PaymentSandbox' },
    ]
  };
}

class _Row extends StatelessWidget {
  final String label, value;
  const _Row(this.label, this.value);

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 5),
    child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
      Text(label, style: TextStyle(color: Colors.grey.shade500, fontSize: 13)),
      Text(value, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w500)),
    ]),
  );
}

class _StepRow extends StatelessWidget {
  final dynamic step;
  const _StepRow({required this.step});

  @override
  Widget build(BuildContext context) {
    final status = step['status'] as String? ?? 'PENDING';
    final color = switch (status) {
      'COMPLETED' => Colors.green, 'FAILED' => Colors.red,
      'WAITING_FOR_HUMAN' => Colors.orange, 'IN_PROGRESS' => Colors.indigo, _ => Colors.grey,
    };
    final icon = switch (status) {
      'COMPLETED' => Icons.check_circle,
      'FAILED' => Icons.cancel,
      'WAITING_FOR_HUMAN' => Icons.pause_circle,
      _ => Icons.radio_button_unchecked,
    };
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(children: [
        Icon(icon, size: 16, color: color),
        const SizedBox(width: 8),
        Text((step['stepName'] as String? ?? '').replaceAll('_', ' '), style: const TextStyle(fontSize: 13)),
        const Spacer(),
        Text(step['agentName'] ?? '', style: TextStyle(fontSize: 10, color: Colors.grey.shade600)),
      ]),
    );
  }
}
