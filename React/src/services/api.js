import axios from 'axios';

const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({ baseURL: BASE_URL });
let onUnauthorized = null;

api.interceptors.request.use(config => {
  let session = null;
  try { session = JSON.parse(localStorage.getItem('expenseguard.session')); } catch { /* invalid storage is handled by auth bootstrap */ }
  if (session?.token) config.headers.Authorization = `Bearer ${session.token}`;
  if (session?.employeeId) config.headers['X-Employee-Id'] = session.employeeId;
  return config;
});

api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) onUnauthorized?.();
    return Promise.reject(error);
  },
);

export const setUnauthorizedHandler = handler => { onUnauthorized = handler; };
export const loginRequest = credentials => api.post('/auth/login', credentials);

const data = request => request.then(response => response.data);

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

export const getFinanceQueue = (params) => api.get('/reimbursements/finance-queue', { params });
export const getApprovalQueue = () => api.get('/reimbursements/approval-queue');
export const getReimbursement = (id) => api.get(`/reimbursements/${id}`);
export const getEmployeeReimbursements = (employeeId) => api.get(`/reimbursements/employee/${employeeId}`);
export const processReimbursement = (id) => api.post(`/reimbursements/${id}/process`);
export const submitPayment = (id) => api.post(`/reimbursements/${id}/payment`);
export const startApproval = (id, templateId) => api.post(`/reimbursements/${id}/approval-process`, null, { params: { templateId } });
export const decideReimbursement = (id, decision, comment) =>
  api.post(`/reimbursements/${id}/${decision}`, comment || null, { headers: { 'Content-Type': 'application/json' } });

export const getRoles = () => api.get('/roles');
export const assignRole = (employeeId, roleId) => api.put(`/roles/employees/${employeeId}`, { roleId });

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

// Canonical workflow/audit read endpoints.
export const getWorkflows = () => api.get('/workflows');
export const getWorkflow = (id) => api.get(`/workflows/${id}`);
export const getWorkflowAudit = (id) => api.get(`/workflows/${id}/audit`);

export default api;
