const API_BASE_URL = 'http://localhost:5104/api';

class ApiService {
  getToken() {
    return localStorage.getItem('compliance_auth_token');
  }

  setToken(token) {
    localStorage.setItem('compliance_auth_token', token);
  }

  getCurrentUser() {
    const raw = localStorage.getItem('compliance_user');
    return raw ? JSON.parse(raw) : null;
  }

  setCurrentUser(user) {
    localStorage.setItem('compliance_user', JSON.stringify(user));
  }

  clearAuth() {
    localStorage.removeItem('compliance_auth_token');
    localStorage.removeItem('compliance_user');
  }

  async request(endpoint, options = {}) {
    const token = this.getToken();
    const headers = {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    };

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `Request failed with status ${response.status}`);
    }

    return response.json();
  }

  // Auth
  async login(username, password) {
    const data = await this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    this.setToken(data.token);
    this.setCurrentUser({
      id: data.userId,
      username: data.username,
      fullName: data.fullName,
      role: data.role,
      departmentId: data.departmentId,
      departmentName: data.departmentName,
    });
    return data;
  }

  // Dashboard Stats
  async getDashboardStats() {
    return this.request('/policy-compliance/stats/compliance-dashboard');
  }

  // Manager Review Queue
  async getReviewQueue(params = {}) {
    const query = new URLSearchParams();
    if (params.riskLevel) query.append('riskLevel', params.riskLevel);
    if (params.policyStatus) query.append('policyStatus', params.policyStatus);
    if (params.status) query.append('status', params.status);
    if (params.departmentId) query.append('departmentId', params.departmentId);
    if (params.category) query.append('category', params.category);
    if (params.searchTerm) query.append('searchTerm', params.searchTerm);
    if (params.page) query.append('page', params.page);
    if (params.pageSize) query.append('pageSize', params.pageSize);

    return this.request(`/policy-compliance/review-queue?${query.toString()}`);
  }

  // Claim Details
  async getClaimDetails(claimId) {
    return this.request(`/policy-compliance/claims/${claimId}`);
  }

  // Claim Risk
  async getClaimRisk(claimId) {
    return this.request(`/policy-compliance/claims/${claimId}/risk`);
  }

  // Claim Policy Checks
  async getClaimPolicyChecks(claimId) {
    return this.request(`/policy-compliance/claims/${claimId}/policy-checks`);
  }

  // Claim Audit Trail
  async getClaimAudit(claimId) {
    return this.request(`/policy-compliance/claims/${claimId}/audit`);
  }

  // Manager Actions
  async approveClaim(claimId, comment = '') {
    return this.request(`/policy-compliance/claims/${claimId}/approve`, {
      method: 'POST',
      body: JSON.stringify({ comment }),
    });
  }

  async rejectClaim(claimId, reason) {
    return this.request(`/policy-compliance/claims/${claimId}/reject`, {
      method: 'POST',
      body: JSON.stringify({ comment: reason }),
    });
  }

  async requestRevision(claimId, comment) {
    return this.request(`/policy-compliance/claims/${claimId}/request-revision`, {
      method: 'POST',
      body: JSON.stringify({ comment }),
    });
  }

  // Employee Operations
  async submitClaim(claimData) {
    return this.request('/policy-compliance/claims', {
      method: 'POST',
      body: JSON.stringify(claimData),
    });
  }

  async resubmitClaim(claimId, resubmitData) {
    return this.request(`/policy-compliance/claims/${claimId}/resubmit`, {
      method: 'PUT',
      body: JSON.stringify(resubmitData),
    });
  }

  async getMyClaims() {
    return this.request('/policy-compliance/my-claims');
  }

  // Policies
  async getPolicies() {
    return this.request('/policy-compliance/policies');
  }

  // Finance
  async reimburseClaim(claimId, notes = '') {
    return this.request(`/policy-compliance/claims/${claimId}/reimburse`, {
      method: 'POST',
      body: JSON.stringify({ comment: notes }),
    });
  }
}

export const api = new ApiService();
