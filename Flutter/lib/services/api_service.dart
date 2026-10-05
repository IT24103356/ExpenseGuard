import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  static const String baseUrl = String.fromEnvironment(
    'EXPENSEGUARD_API_URL',
    defaultValue: 'http://localhost:5000/api',
  );
  static String? _token = const String.fromEnvironment(
    'EXPENSEGUARD_AUTH_TOKEN',
  );

  static void setToken(String token) => _token = token;

  static Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    if (_token != null && _token!.isNotEmpty) 'Authorization': 'Bearer $_token',
  };

  static String get _currentEmployeeId {
    if (_token == null || _token!.isEmpty) {
      throw StateError('Sign in before loading employee reimbursements.');
    }
    final segments = _token!.split('.');
    if (segments.length != 3) throw StateError('Invalid authentication token.');
    final payload = jsonDecode(
      utf8.decode(base64Url.decode(base64Url.normalize(segments[1]))),
    ) as Map<String, dynamic>;
    final value = payload['employee_id'] ?? payload['employeeId'] ?? payload['sub'];
    if (value == null || value.toString().isEmpty) {
      throw StateError('The token does not identify an employee.');
    }
    return value.toString();
  }

  // ── Reimbursements ──────────────────────────────────────────────────────
  static Future<List<dynamic>> getEmployeeReimbursements(String employeeId) async {
    final resp = await http.get(
      Uri.parse('$baseUrl/reimbursements/employee/$employeeId'),
      headers: _headers,
    );
    if (resp.statusCode == 200) return jsonDecode(resp.body);
    throw Exception('Failed to load reimbursements: ${resp.statusCode}');
  }

  static Future<List<dynamic>> getCurrentEmployeeReimbursements() =>
      getEmployeeReimbursements(_currentEmployeeId);

  static Future<Map<String, dynamic>> getReimbursement(String id) async {
    final resp = await http.get(
      Uri.parse('$baseUrl/reimbursements/$id'),
      headers: _headers,
    );
    if (resp.statusCode == 200) return jsonDecode(resp.body);
    throw Exception('Reimbursement not found');
  }

  static Future<Map<String, dynamic>> getReimbursementByClaim(String claimId) async {
    final resp = await http.get(
      Uri.parse('$baseUrl/reimbursements/by-claim/$claimId'),
      headers: _headers,
    );
    if (resp.statusCode == 200) return jsonDecode(resp.body);
    throw Exception('Not found');
  }

  // ── Workflows ────────────────────────────────────────────────────────────
  static Future<Map<String, dynamic>> getWorkflowByClaim(String claimId) async {
    final resp = await http.get(
      Uri.parse('$baseUrl/workflows/by-claim/$claimId'),
      headers: _headers,
    );
    if (resp.statusCode == 200) return jsonDecode(resp.body);
    throw Exception('Workflow not found');
  }

  // ── History ──────────────────────────────────────────────────────────────
  static Future<List<dynamic>> getReimbursementHistory() =>
      getCurrentEmployeeReimbursements();
}
