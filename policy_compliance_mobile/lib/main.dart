import 'package:flutter/material.dart';
import 'services/api_service.dart';
import 'screens/claims_list_screen.dart';

void main() {
  runApp(const PolicyComplianceMobileApp());
}

class PolicyComplianceMobileApp extends StatelessWidget {
  const PolicyComplianceMobileApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SpendGuard Mobile',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF6366F1),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
      ),
      home: const MobileAuthScreen(),
    );
  }
}

class MobileAuthScreen extends StatefulWidget {
  const MobileAuthScreen({super.key});

  @override
  State<MobileAuthScreen> createState() => _MobileAuthScreenState();
}

class _MobileAuthScreenState extends State<MobileAuthScreen> {
  String _selectedUser = 'john.doe';
  bool _loading = false;
  String? _error;

  final Map<String, String> _demoUsers = {
    'john.doe': 'John Doe (Sales Employee)',
    'jane.smith': 'Jane Smith (Engineering Employee)',
    'sarah.chen': 'Sarah Chen (Manager)',
  };

  Future<void> _handleLogin() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    final success = await mobileApi.login(_selectedUser, 'Password123!');
    setState(() {
      _loading = false;
    });

    if (success && mounted) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => const ClaimsListScreen()),
      );
    } else {
      setState(() {
        _error = 'Failed to connect to ASP.NET Core API at ${MobileApiService.baseUrl}';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Icon(Icons.shield, size: 64, color: Color(0xFF6366F1)),
              const SizedBox(height: 16),
              const Text(
                'SpendGuard Mobile',
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const Text(
                'Employee Spend & Compliance Subsystem',
                textAlign: TextAlign.center,
                style: TextStyle(color: Colors.grey),
              ),
              const SizedBox(height: 32),
              const Text('Select Employee Persona:'),
              const SizedBox(height: 8),
              DropdownButtonFormField<String>(
                initialValue: _selectedUser,
                decoration: const InputDecoration(border: OutlineInputBorder()),
                items: _demoUsers.entries
                    .map((e) => DropdownMenuItem(value: e.key, child: Text(e.value)))
                    .toList(),
                onChanged: (val) {
                  if (val != null) setState(() => _selectedUser = val);
                },
              ),
              const SizedBox(height: 20),
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12.0),
                  child: Text(
                    _error!,
                    style: const TextStyle(color: Colors.red, fontSize: 13),
                    textAlign: TextAlign.center,
                  ),
                ),
              ElevatedButton.icon(
                onPressed: _loading ? null : _handleLogin,
                icon: const Icon(Icons.login),
                label: Text(_loading ? 'Connecting...' : 'Sign In as Employee'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF6366F1),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
