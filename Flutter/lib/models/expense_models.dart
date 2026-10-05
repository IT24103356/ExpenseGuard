String enumName(dynamic value, List<String> names) {
  if (value is int && value >= 0 && value < names.length) return names[value];
  return value?.toString() ?? '';
}

class EmployeeProfile {
  const EmployeeProfile({required this.id, required this.fullName, required this.email, this.designation});
  final int id;
  final String fullName;
  final String email;
  final String? designation;
  factory EmployeeProfile.fromJson(Map<String, dynamic> json) => EmployeeProfile(
    id: json['employeeId'] as int, fullName: json['fullName'] as String,
    email: json['email'] as String, designation: json['designation'] as String?,
  );
}

class PurchaseRequest {
  const PurchaseRequest({required this.id, required this.description, required this.amount, required this.currency, required this.status, required this.version, this.vendor});
  final int id;
  final String description;
  final double amount;
  final String currency;
  final String status;
  final int version;
  final String? vendor;
  factory PurchaseRequest.fromJson(Map<String, dynamic> json) => PurchaseRequest(
    id: json['purchaseRequestId'] as int, description: json['description'] as String,
    amount: (json['estimatedAmount'] as num).toDouble(), currency: json['currency'] as String,
    status: enumName(json['status'], const ['Draft', 'Submitted', 'Approved', 'Rejected', 'Cancelled']),
    version: json['version'] as int, vendor: json['vendor'] as String?,
  );
}

class ExpenseClaim {
  const ExpenseClaim({
    required this.id, required this.amount, required this.category, required this.description,
    required this.currency, required this.status, required this.flow, required this.version,
    this.vendor, this.purchaseDate, this.purchaseRequestId,
  });
  final int id;
  final double amount;
  final String category;
  final String description;
  final String currency;
  final String status;
  final String flow;
  final int version;
  final String? vendor;
  final DateTime? purchaseDate;
  final int? purchaseRequestId;
  factory ExpenseClaim.fromJson(Map<String, dynamic> json) => ExpenseClaim(
    id: json['expenseClaimId'] as int, amount: (json['amount'] as num).toDouble(),
    category: json['category'] as String, description: json['description'] as String,
    currency: json['currency'] as String,
    status: enumName(json['status'], const ['Draft', 'Submitted', 'UnderReview', 'Approved', 'Rejected', 'NeedsCorrection', 'Cancelled']),
    flow: enumName(json['flow'], const ['OutOfPocket', 'PrePurchase']),
    version: json['version'] as int, vendor: json['vendor'] as String?,
    purchaseDate: json['purchaseDate'] == null ? null : DateTime.parse(json['purchaseDate'] as String),
    purchaseRequestId: json['purchaseRequestId'] as int?,
  );
}

class ClaimHistory {
  const ClaimHistory({required this.id, required this.from, required this.to, required this.changedAt, this.reason});
  final int id;
  final String from;
  final String to;
  final DateTime changedAt;
  final String? reason;
  factory ClaimHistory.fromJson(Map<String, dynamic> json) => ClaimHistory(
    id: json['claimStatusHistoryId'] as int,
    from: enumName(json['fromStatus'], const ['Draft', 'Submitted', 'UnderReview', 'Approved', 'Rejected', 'NeedsCorrection', 'Cancelled']),
    to: enumName(json['toStatus'], const ['Draft', 'Submitted', 'UnderReview', 'Approved', 'Rejected', 'NeedsCorrection', 'Cancelled']),
    changedAt: DateTime.parse(json['changedAt'] as String), reason: json['reason'] as String?,
  );
}

class ReceiptResult {
  const ReceiptResult({
    required this.id, required this.processingStatus, required this.requiresManualReview,
    this.vendor, this.amount, this.purchaseDate, this.currency, this.confidence,
  });
  final int id;
  final String processingStatus;
  final bool requiresManualReview;
  final String? vendor;
  final double? amount;
  final DateTime? purchaseDate;
  final String? currency;
  final double? confidence;
  factory ReceiptResult.fromJson(Map<String, dynamic> json) => ReceiptResult(
    id: json['receiptId'] as int,
    processingStatus: enumName(json['processingStatus'], const ['Pending', 'Processed', 'NeedsReview', 'Failed']),
    requiresManualReview: json['requiresManualReview'] as bool? ?? false,
    vendor: json['extractedVendor'] as String?, amount: (json['extractedAmount'] as num?)?.toDouble(),
    purchaseDate: json['extractedDate'] == null ? null : DateTime.parse(json['extractedDate'] as String),
    currency: json['extractedCurrency'] as String?, confidence: (json['confidence'] as num?)?.toDouble(),
  );
}

class ClaimDraft {
  const ClaimDraft({
    required this.amount, required this.category, required this.description, required this.currency,
    required this.flow, this.vendor, this.purchaseDate, this.purchaseRequestId, this.version = 0,
  });
  final double amount;
  final String category;
  final String description;
  final String currency;
  final String flow;
  final String? vendor;
  final DateTime? purchaseDate;
  final int? purchaseRequestId;
  final int version;
  Map<String, dynamic> toJson() => {
    'amount': amount, 'category': category, 'description': description, 'currency': currency,
    'vendor': vendor, 'purchaseDate': purchaseDate?.toIso8601String(),
    'purchaseRequestId': purchaseRequestId, 'flow': flow == 'PrePurchase' ? 1 : 0, 'version': version,
  };
}
