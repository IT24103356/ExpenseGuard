class ExpenseClaimModel {
  final String id;
  final String claimNumber;
  final String employeeName;
  final String departmentName;
  final DateTime claimDate;
  final double totalAmount;
  final String currency;
  final String merchantName;
  final String category;
  final String status;
  final String policyStatus;
  final String riskStatus;
  final int? riskScore;
  final int violationCount;
  final int duplicateCount;
  final String? latestRevisionComment;
  final String? latestRejectionReason;

  ExpenseClaimModel({
    required this.id,
    required this.claimNumber,
    required this.employeeName,
    required this.departmentName,
    required this.claimDate,
    required this.totalAmount,
    required this.currency,
    required this.merchantName,
    required this.category,
    required this.status,
    required this.policyStatus,
    required this.riskStatus,
    this.riskScore,
    required this.violationCount,
    required this.duplicateCount,
    this.latestRevisionComment,
    this.latestRejectionReason,
  });

  factory ExpenseClaimModel.fromJson(Map<String, dynamic> json) {
    return ExpenseClaimModel(
      id: json['id'] ?? '',
      claimNumber: json['claimNumber'] ?? '',
      employeeName: json['employeeName'] ?? '',
      departmentName: json['departmentName'] ?? '',
      claimDate: DateTime.tryParse(json['claimDate'] ?? '') ?? DateTime.now(),
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      currency: json['currency'] ?? 'LKR',
      merchantName: json['merchantName'] ?? '',
      category: json['category'] ?? '',
      status: json['status'] ?? 'SUBMITTED',
      policyStatus: json['policyStatus'] ?? 'PENDING',
      riskStatus: json['riskStatus'] ?? 'NOT_ASSESSED',
      riskScore: json['riskScore'] as int?,
      violationCount: (json['violationCount'] as num?)?.toInt() ?? 0,
      duplicateCount: (json['duplicateCount'] as num?)?.toInt() ?? 0,
      latestRevisionComment: json['latestRevisionComment'],
      latestRejectionReason: json['latestRejectionReason'],
    );
  }
}

class PolicyViolationModel {
  final String ruleCode;
  final String severity;
  final String message;
  final String actualValue;
  final String allowedValue;

  PolicyViolationModel({
    required this.ruleCode,
    required this.severity,
    required this.message,
    required this.actualValue,
    required this.allowedValue,
  });

  factory PolicyViolationModel.fromJson(Map<String, dynamic> json) {
    return PolicyViolationModel(
      ruleCode: json['ruleCode'] ?? '',
      severity: json['severity'] ?? '',
      message: json['message'] ?? '',
      actualValue: json['actualValue'] ?? '',
      allowedValue: json['allowedValue'] ?? '',
    );
  }
}
