import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  static const String baseUrl = 'http://localhost:5000/api';
  static String? _token;

  static void setToken(String token) => _token = token;

  static Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    if (_token != null) 'Authorization': 'Bearer $_token',
  };

  // ── Reimbursements ──────────────────────────────────────────────────────
  static Future<List<dynamic>> getEmployeeReimbursements(String employeeId) async {
    final resp = await http.get(
      Uri.parse('$baseUrl/reimbursements/employee/$employeeId'),
      headers: _headers,
    );
    if (resp.statusCode == 200) return jsonDecode(resp.body);
    throw Exception('Failed to load reimbursements: ${resp.statusCode}');
  }

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
  static Future<List<dynamic>> getReimbursementHistory(String employeeId) async {
    return getEmployeeReimbursements(employeeId);
  }
}
