import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../auth/auth_provider.dart';
import '../widgets/expense_guard_logo.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});
  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _username = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _username.dispose();
    _password.dispose();
    super.dispose();
  }

  void _submit() {
    if (_formKey.currentState!.validate()) {
      ref.read(authProvider.notifier).login(_username.text, _password.text);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authProvider);
    final keyboardOpen = MediaQuery.viewInsetsOf(context).bottom > 0;
    return Scaffold(
      backgroundColor: const Color(0xFF0A0E1A),
      body: SafeArea(
        child: LayoutBuilder(builder: (context, constraints) {
          final wide = constraints.maxWidth >= 800;
          final form = _LoginForm(
            formKey: _formKey,
            username: _username,
            password: _password,
            error: auth.hasError ? auth.error.toString() : null,
            busy: auth.isLoading,
            onSubmit: _submit,
          );
          final hero = const _LoginHero();
          if (wide) {
            return Padding(
              padding: const EdgeInsets.all(16),
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: const Color(0xFF111827),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: const Color(0xFF1E293B)),
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(20),
                  child: Row(children: [
                    Expanded(flex: 4, child: form),
                    Expanded(flex: 5, child: hero),
                  ]),
                ),
              ),
            );
          }
          return Column(children: [
            Expanded(child: form),
            if (!keyboardOpen) const SizedBox(height: 168, child: _LoginHero()),
          ]);
        }),
      ),
    );
  }
}

class _LoginForm extends StatelessWidget {
  const _LoginForm({
    required this.formKey,
    required this.username,
    required this.password,
    required this.error,
    required this.busy,
    required this.onSubmit,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController username;
  final TextEditingController password;
  final String? error;
  final bool busy;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(24, 20, 24, 16),
      child: Form(
        key: formKey,
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          const Row(children: [
            ExpenseGuardLogo(size: 40),
            SizedBox(width: 10),
            Text('ExpenseGuard', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
          ]),
          const SizedBox(height: 36),
          const Text('Sign in', style: TextStyle(fontSize: 32, fontWeight: FontWeight.w800, letterSpacing: -0.8)),
          const SizedBox(height: 8),
          Text(
            'Review receipts, OCR, and claim approvals in one workspace.',
            style: TextStyle(color: Colors.grey.shade400, height: 1.4),
          ),
          const SizedBox(height: 24),
          if (error != null)
            Semantics(
              liveRegion: true,
              child: Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Text(error!, style: const TextStyle(color: Color(0xFFEF4444))),
              ),
            ),
          TextFormField(
            controller: username,
            autofillHints: const [AutofillHints.username],
            textInputAction: TextInputAction.next,
            decoration: _field('Username', 'nimal.perera'),
            validator: (value) => value == null || value.trim().isEmpty ? 'Username is required' : null,
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: password,
            obscureText: true,
            autofillHints: const [AutofillHints.password],
            onFieldSubmitted: (_) => onSubmit(),
            decoration: _field('Password', '••••••••'),
            validator: (value) => value == null || value.isEmpty ? 'Password is required' : null,
          ),
          const SizedBox(height: 20),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: const LinearGradient(colors: [Color(0xFF6366F1), Color(0xFF8B5CF6)]),
              borderRadius: BorderRadius.circular(12),
            ),
            child: FilledButton(
              onPressed: busy ? null : onSubmit,
              style: FilledButton.styleFrom(
                backgroundColor: Colors.transparent,
                shadowColor: Colors.transparent,
                disabledBackgroundColor: Colors.transparent,
                minimumSize: const Size.fromHeight(48),
              ),
              child: Text(busy ? 'Signing in…' : 'Sign in'),
            ),
          ),
          const SizedBox(height: 18),
          Text('Authorized employees only.', textAlign: TextAlign.center, style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
        ]),
      ),
    );
  }

  InputDecoration _field(String label, String hint) => InputDecoration(
    labelText: label,
    hintText: hint,
    filled: true,
    fillColor: const Color(0xFF0A0E1A),
    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: const BorderSide(color: Color(0xFF1E293B)),
    ),
  );
}

class _LoginHero extends StatelessWidget {
  const _LoginHero();

  @override
  Widget build(BuildContext context) {
    return Stack(fit: StackFit.expand, children: [
      Image.asset(
        'assets/images/login-hero.jpg',
        fit: BoxFit.cover,
        errorBuilder: (_, __, ___) => const ColoredBox(color: Color(0xFF1A2236)),
      ),
      const DecoratedBox(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [Color(0x1A0A0E1A), Color(0x660A0E1A)],
          ),
        ),
      ),
      const Padding(
        padding: EdgeInsets.all(16),
        child: Column(children: [
          _HeroCard(title: 'Receipt review', subtitle: 'OCR matched · ready for approval'),
          Spacer(),
          Align(
            alignment: Alignment.centerRight,
            child: _HeroCard(title: 'Manager queue', subtitle: 'Policy, fraud, and budget checked'),
          ),
          Spacer(),
          Align(
            alignment: Alignment.centerLeft,
            child: _HeroCard(title: 'Finance payout', subtitle: 'After the approval chain'),
          ),
        ]),
      ),
    ]);
  }
}

class _HeroCard extends StatelessWidget {
  const _HeroCard({required this.title, required this.subtitle});
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: const Color(0xEB1A2236),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF1E293B)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: [
          Text(title, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13)),
          Text(subtitle, style: TextStyle(fontSize: 11, color: Colors.grey.shade400)),
        ]),
      ),
    );
  }
}
