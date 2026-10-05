import axios from 'axios';

const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({ baseURL: BASE_URL });

let authResolver = () => ({
  token: localStorage.getItem('jwt_token'),
  employeeId: localStorage.getItem('employee_id'),
});

export const setAuthResolver = (resolver) => {
  authResolver = resolver;
};

// Transitional X-Employee-Id support is isolated here so the shared auth
// branch can remove it once the API resolves employee identity from JWT claims.
api.interceptors.request.use(config => {
  const { token, employeeId } = authResolver() || {};
  if (token) config.headers.Authorization = `Bearer ${token}`;
  if (employeeId) config.headers['X-Employee-Id'] = employeeId;
  return config;
});

const data = (request) => request.then(response => response.data);

// ─── Employee expense claims ────────────────────────────────────────────────
export const getMyProfile = () => data(api.get('/employees/me'));
export const getEmployees = () => data(api.get('/employees'));

export const getPurchaseRequests = () => data(api.get('/purchase-requests'));
export const getPurchaseRequest = (id) => data(api.get(`/purchase-requests/${id}`));
export const createPurchaseRequest = (body) => data(api.post('/purchase-requests', body));
export const updatePurchaseRequest = (id, body) => data(api.put(`/purchase-requests/${id}`, body));
export const submitPurchaseRequest = (id) => data(api.post(`/purchase-requests/${id}/submit`));
export const deletePurchaseRequest = (id) => data(api.delete(`/purchase-requests/${id}`));

export const searchClaims = (params) => data(api.get('/claims', { params }));
export const getClaim = (id) => data(api.get(`/claims/${id}`));
export const createClaim = (body) => data(api.post('/claims', body));
export const updateClaim = (id, body) => data(api.put(`/claims/${id}`, body));
export const submitClaim = (id) => data(api.post(`/claims/${id}/submit`));
export const resubmitClaim = (id, reason) => data(api.post(`/claims/${id}/resubmit`, reason, {
  headers: { 'Content-Type': 'application/json' },
}));
export const deleteClaim = (id) => data(api.delete(`/claims/${id}`));
export const getClaimHistory = (id) => data(api.get(`/claims/${id}/history`));
export const uploadReceipt = (id, file) => {
  const body = new FormData();
  body.append('file', file);
  return data(api.post(`/claims/${id}/receipts`, body));
};
export const correctReceipt = (claimId, receiptId, body) =>
  data(api.patch(`/claims/${claimId}/receipts/${receiptId}`, body));

// ─── Reimbursements ──────────────────────────────────────────────────────────
export const getFinanceQueue = (params) => api.get('/reimbursements/finance-queue', { params });
export const getReimbursement = (id) => api.get(`/reimbursements/${id}`);
export const getReimbursementByClaim = (claimId) => api.get(`/reimbursements/by-claim/${claimId}`);
export const getEmployeeReimbursements = (employeeId) => api.get(`/reimbursements/employee/${employeeId}`);
export const processReimbursement = (id) => api.post(`/reimbursements/${id}/process`);
export const submitPayment = (id) => api.post(`/reimbursements/${id}/payment`);

// ─── Budgets ─────────────────────────────────────────────────────────────────
export const getAllBudgets = (fiscalYear) => api.get('/budgets', { params: { fiscalYear } });
export const getBudget = (id) => api.get(`/budgets/${id}`);
export const createBudget = (data) => api.post('/budgets', data);
export const updateBudget = (id, data) => api.put(`/budgets/${id}`, data);
export const getBudgetTransactions = (id) => api.get(`/budgets/${id}/transactions`);
export const getBudgetSummary = (id) => api.get(`/budgets/${id}/summary`);
export const getDepartmentBudgets = (deptId) => api.get(`/budgets/department/${deptId}`);

// ─── Reports ─────────────────────────────────────────────────────────────────
export const getSpendVsBudget = (params) => api.get('/reports/spend-vs-budget', { params });
export const getMonthlySpending = (params) => api.get('/reports/monthly-spending', { params });
export const getCategorySpending = (params) => api.get('/reports/category-spending', { params });
export const getReimbursementSummary = (params) => api.get('/reports/reimbursement-summary', { params });
export const getPaymentSummary = (params) => api.get('/reports/payment-summary', { params });
export const getFinanceDashboard = (fiscalYear) => api.get('/reports/finance-dashboard', { params: { fiscalYear } });

// ─── Workflows ───────────────────────────────────────────────────────────────
export const startWorkflow = (data) => api.post('/workflows/start', data);
export const getWorkflow = (id) => api.get(`/workflows/${id}`);
export const getWorkflowByClaim = (claimId) => api.get(`/workflows/by-claim/${claimId}`);
export const submitApproval = (id, data) => api.post(`/workflows/${id}/approval`, data);

export default api;
