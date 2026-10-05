import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../models/claim_models.dart';

class ClaimsListScreen extends StatefulWidget {
  const ClaimsListScreen({super.key});

  @override
  State<ClaimsListScreen> createState() => _ClaimsListScreenState();
}

class _ClaimsListScreenState extends State<ClaimsListScreen> {
  List<ExpenseClaimModel> _claims = [];
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchClaims();
  }

  Future<void> _fetchClaims() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final claims = await mobileApi.getMyClaims();
      setState(() {
        _claims = claims;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
      });
    } finally {
      setState(() {
        _loading = false;
      });
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'APPROVED':
        return Colors.green;
      case 'REJECTED':
        return Colors.red;
      case 'REVISION_REQUIRED':
        return Colors.purple;
      case 'WAITING_FOR_MANAGER_APPROVAL':
        return Colors.amber.shade800;
      default:
        return Colors.blue;
    }
  }

  void _showSubmitDialog() {
    final catController = TextEditingController(text: 'Meals');
    final merchantController = TextEditingController();
    final amountController = TextEditingController();
    final descController = TextEditingController();
    final receiptController = TextEditingController(text: 'https://storage.enterprise.internal/receipts/mobile.pdf');

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Submit Expense Claim'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<String>(
                initialValue: 'Meals',
                decoration: const InputDecoration(labelText: 'Category'),
                items: const [
                  DropdownMenuItem(value: 'Meals', child: Text('Meals (Limit: 15,000)')),
                  DropdownMenuItem(value: 'Hotel', child: Text('Hotel (Limit: 50,000)')),
                  DropdownMenuItem(value: 'Travel', child: Text('Travel (Limit: 20,000)')),
                  DropdownMenuItem(value: 'Software', child: Text('Software (Limit: 35,000)')),
                ],
                onChanged: (val) {
                  if (val != null) catController.text = val;
                },
              ),
              TextField(
                controller: merchantController,
                decoration: const InputDecoration(labelText: 'Merchant Name'),
              ),
              TextField(
                controller: amountController,
                decoration: const InputDecoration(labelText: 'Amount (LKR)'),
                keyboardType: TextInputType.number,
              ),
              TextField(
                controller: receiptController,
                decoration: const InputDecoration(labelText: 'Receipt URL'),
              ),
              TextField(
                controller: descController,
                decoration: const InputDecoration(labelText: 'Description / Purpose'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () async {
              if (merchantController.text.isEmpty || amountController.text.isEmpty) {
                return;
              }
              final amount = double.tryParse(amountController.text) ?? 0.0;
              final nav = Navigator.of(context);
              final messenger = ScaffoldMessenger.of(context);
              final success = await mobileApi.submitClaim(
                category: catController.text,
                merchantName: merchantController.text,
                amount: amount,
                description: descController.text,
                receiptUrl: receiptController.text,
              );
              nav.pop();
              if (success) {
                messenger.showSnackBar(
                  const SnackBar(content: Text('Claim submitted to ASP.NET Core API!')),
                );
                _fetchClaims();
              }
            },
            child: const Text('Submit'),
          ),
        ],
      ),
    );
  }

  void _showClaimDetails(ExpenseClaimModel claim) async {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => FutureBuilder<Map<String, dynamic>>(
        future: mobileApi.getClaimDetails(claim.id),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Padding(
              padding: EdgeInsets.all(32.0),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          if (snapshot.hasError || !snapshot.hasData) {
            return const Padding(
              padding: EdgeInsets.all(24.0),
              child: Text('Failed to load details'),
            );
          }

          final data = snapshot.data!;
          final violations = (data['violations'] as List? ?? []);
          final risk = data['riskAssessment'] as Map<String, dynamic>?;

          return Padding(
            padding: const EdgeInsets.all(20.0),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      claim.claimNumber,
                      style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      '${claim.currency} ${claim.totalAmount.toStringAsFixed(2)}',
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Text('${claim.category} • ${claim.merchantName}'),
                const Divider(height: 24),
                Text(
                  'Workflow Status: ${claim.status}',
                  style: TextStyle(
                    color: _getStatusColor(claim.status),
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                if (claim.latestRevisionComment != null) ...[
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: Colors.purple.shade50,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: Colors.purple.shade200),
                    ),
                    child: Text(
                      'Manager Feedback: ${claim.latestRevisionComment}',
                      style: const TextStyle(color: Colors.purple, fontSize: 13),
                    ),
                  ),
                  const SizedBox(height: 8),
                ],
                Text('Policy Status: ${claim.policyStatus} (${violations.length} violations)'),
                if (violations.isNotEmpty)
                  ...violations.map((v) => Padding(
                        padding: const EdgeInsets.only(top: 4.0),
                        child: Text('• ${v['ruleCode']}: ${v['message']}', style: const TextStyle(color: Colors.red, fontSize: 12)),
                      )),
                const SizedBox(height: 12),
                Text('Fraud/Risk Level: ${risk?['riskLevel'] ?? claim.riskStatus} (Score: ${risk?['riskScore'] ?? claim.riskScore ?? 0})'),
                if (risk?['reasonSummary'] != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 4.0),
                    child: Text('${risk!['reasonSummary']}', style: const TextStyle(fontSize: 12, color: Colors.grey)),
                  ),
                const SizedBox(height: 20),
                Center(
                  child: ElevatedButton(
                    onPressed: () => Navigator.pop(ctx),
                    child: const Text('Close'),
                  ),
                )
              ],
            ),
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('SpendGuard Compliance Mobile'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _fetchClaims,
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text('Error: $_error'),
                      const SizedBox(height: 10),
                      ElevatedButton(onPressed: _fetchClaims, child: const Text('Retry')),
                    ],
                  ),
                )
              : _claims.isEmpty
                  ? const Center(child: Text('No expense claims found.'))
                  : RefreshIndicator(
                      onRefresh: _fetchClaims,
                      child: ListView.builder(
                        itemCount: _claims.length,
                        itemBuilder: (ctx, i) {
                          final c = _claims[i];
                          return Card(
                            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                            child: ListTile(
                              title: Text(
                                c.claimNumber,
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('${c.category} • ${c.merchantName}'),
                                  Text(
                                    c.status,
                                    style: TextStyle(
                                      color: _getStatusColor(c.status),
                                      fontWeight: FontWeight.w600,
                                      fontSize: 12,
                                    ),
                                  ),
                                  if (c.violationCount > 0)
                                    Text(
                                      '${c.violationCount} policy violation(s)',
                                      style: const TextStyle(color: Colors.red, fontSize: 11),
                                    ),
                                ],
                              ),
                              trailing: Text(
                                '${c.currency} ${c.totalAmount.toStringAsFixed(0)}',
                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                              ),
                              onTap: () => _showClaimDetails(c),
                            ),
                          );
                        },
                      ),
                    ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _showSubmitDialog,
        icon: const Icon(Icons.add),
        label: const Text('New Claim'),
      ),
    );
  }
}
