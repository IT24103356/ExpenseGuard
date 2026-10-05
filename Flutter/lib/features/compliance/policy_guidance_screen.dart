import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'compliance_providers.dart';

class PolicyGuidanceScreen extends ConsumerStatefulWidget {
  final String? initialClaimId;
  const PolicyGuidanceScreen({super.key, this.initialClaimId});

  @override
  ConsumerState<PolicyGuidanceScreen> createState() => _PolicyGuidanceScreenState();
}

class _PolicyGuidanceScreenState extends ConsumerState<PolicyGuidanceScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _claim;
  final _currency = TextEditingController(text: 'USD');
  final _designation = TextEditingController();
  final _receiptAmount = TextEditingController();

  @override
  void initState() {
    super.initState();
    _claim = TextEditingController(text: widget.initialClaimId ?? '');
  }

  @override
  void dispose() {
    _claim.dispose();
    _currency.dispose();
    _designation.dispose();
    _receiptAmount.dispose();
    super.dispose();
  }

  void _evaluate() {
    if (!_formKey.currentState!.validate()) return;
    ref.read(complianceControllerProvider.notifier).evaluate(ComplianceInput(
      claimId: int.parse(_claim.text),
      currency: _currency.text.toUpperCase(),
      designation: _designation.text,
      receiptAmount: _receiptAmount.text.isEmpty ? null : double.parse(_receiptAmount.text),
    ));
  }

  @override
  Widget build(BuildContext context) {
    final result = ref.watch(complianceControllerProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Policy guidance')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const Text('Check before you revise', style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
          const SizedBox(height: 6),
          Text('Use the claim details from your authenticated account. No employee identity is stored in this form.', style: TextStyle(color: Colors.grey.shade400)),
          const SizedBox(height: 16),
          Card(child: Padding(
            padding: const EdgeInsets.all(16),
            child: Form(
              key: _formKey,
              child: Column(children: [
                TextFormField(
                  key: const Key('claimId'),
                  controller: _claim,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Claim ID'),
                  validator: (value) => (int.tryParse(value ?? '') ?? 0) < 1 ? 'Enter a valid numeric claim ID.' : null,
                ),
                TextFormField(
                  key: const Key('currency'),
                  controller: _currency,
                  textCapitalization: TextCapitalization.characters,
                  maxLength: 3,
                  decoration: const InputDecoration(labelText: 'Currency'),
                  validator: (value) => !RegExp(r'^[A-Za-z]{3}$').hasMatch(value ?? '') ? 'Use a three-letter currency.' : null,
                ),
                TextFormField(controller: _designation, decoration: const InputDecoration(labelText: 'Designation (optional)')),
                TextFormField(
                  controller: _receiptAmount,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(labelText: 'Receipt amount (optional)'),
                  validator: (value) {
                    if (value == null || value.isEmpty) return null;
                    final amount = double.tryParse(value);
                    return amount == null || amount < 0 ? 'Enter a non-negative amount.' : null;
                  },
                ),
                const SizedBox(height: 18),
                FilledButton.icon(
                  onPressed: result.isLoading ? null : _evaluate,
                  icon: const Icon(Icons.policy_outlined),
                  label: Text(result.isLoading ? 'Checking…' : 'Check compliance'),
                ),
              ]),
            ),
          )),
          const SizedBox(height: 16),
          result.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => _FailureCard(error: error, onRetry: _evaluate),
            data: (feedback) => feedback == null ? const _GuidanceEmpty() : _FeedbackCard(feedback: feedback),
          ),
        ],
      ),
    );
  }
}

class _GuidanceEmpty extends StatelessWidget {
  const _GuidanceEmpty();
  @override
  Widget build(BuildContext context) => const Card(child: Padding(
    padding: EdgeInsets.all(16),
    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text('What you will see', style: TextStyle(fontWeight: FontWeight.bold)),
      SizedBox(height: 8),
      Text('Policy violations and revision guidance are returned by ExpenseGuard.Api. Fraud evidence stays restricted to authorized reviewers; this screen never guesses or exposes confidential flags.'),
    ]),
  ));
}

class _FeedbackCard extends StatelessWidget {
  final ComplianceFeedback feedback;
  const _FeedbackCard({required this.feedback});

  @override
  Widget build(BuildContext context) => Card(child: Padding(
    padding: const EdgeInsets.all(16),
    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Row(children: [
        Icon(feedback.violations.isEmpty ? Icons.check_circle : Icons.edit_note, color: feedback.violations.isEmpty ? Colors.green : Colors.orange),
        const SizedBox(width: 8),
        Text('Outcome: ${feedback.outcome}', style: const TextStyle(fontWeight: FontWeight.bold)),
      ]),
      const SizedBox(height: 12),
      if (feedback.violations.isEmpty)
        const Text('No policy violations were returned. Fraud screening details are visible only to authorized reviewers.')
      else ...[
        const Text('Revise the claim using this feedback, then check again:'),
        const SizedBox(height: 8),
        ...feedback.violations.map((item) => ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const Icon(Icons.warning_amber),
          title: Text(item.message),
          subtitle: Text('${item.ruleCode} · ${item.severity}'),
        )),
      ],
    ]),
  ));
}

class _FailureCard extends StatelessWidget {
  final Object error;
  final VoidCallback onRetry;
  const _FailureCard({required this.error, required this.onRetry});
  @override
  Widget build(BuildContext context) {
    final message = error is ApiFailure ? (error as ApiFailure).message : 'The check failed safely. No compliance result was assumed.';
    return Card(child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(children: [
        const Icon(Icons.error_outline, color: Colors.red),
        const SizedBox(height: 8),
        Text(message, textAlign: TextAlign.center),
        const SizedBox(height: 8),
        OutlinedButton(onPressed: onRetry, child: const Text('Retry')),
      ]),
    ));
  }
}

