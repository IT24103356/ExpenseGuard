import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/claim_models.dart';

class MobileApiService {
  static const String baseUrl = 'http://127.0.0.1:5104/api';
  String? _token;
  String? currentRole;
  String? currentFullName;

  bool get isAuthenticated => _token != null;

  Future<bool> login(String username, String password) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'username': username, 'password': password}),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        _token = data['token'];
        currentRole = data['role'];
        currentFullName = data['fullName'];
        return true;
      }
      return false;
    } catch (e) {
      return false;
    }
  }

  Future<List<ExpenseClaimModel>> getMyClaims() async {
    final response = await http.get(
      Uri.parse('$baseUrl/policy-compliance/my-claims'),
      headers: {
        'Content-Type': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      },
    );

    if (response.statusCode == 200) {
      final List list = jsonDecode(response.body);
      return list.map((item) => ExpenseClaimModel.fromJson(item)).toList();
    }
    throw Exception('Failed to load my claims: ${response.statusCode}');
  }

  Future<Map<String, dynamic>> getClaimDetails(String claimId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/policy-compliance/claims/$claimId'),
      headers: {
        'Content-Type': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      },
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }
    throw Exception('Failed to load claim: ${response.statusCode}');
  }

  Future<bool> submitClaim({
    required String category,
    required String merchantName,
    required double amount,
    required String description,
    String? receiptUrl,
  }) async {
    final payload = {
      'claimDate': DateTime.now().toIso8601String(),
      'merchantName': merchantName,
      'category': category,
      'totalAmount': amount,
      'currency': 'LKR',
      'description': description,
      'items': [
        {
          'expenseDate': DateTime.now().toIso8601String(),
          'category': category,
          'merchant': merchantName,
          'amount': amount,
          'currency': 'LKR',
          'description': description,
          'receiptUrl': receiptUrl,
        }
      ]
    };

    final response = await http.post(
      Uri.parse('$baseUrl/policy-compliance/claims'),
      headers: {
        'Content-Type': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      },
      body: jsonEncode(payload),
    );

    return response.statusCode == 201 || response.statusCode == 200;
  }
}

final mobileApi = MobileApiService();
