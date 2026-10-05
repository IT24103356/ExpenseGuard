import axios from 'axios';

const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({ baseURL: BASE_URL });

// Attach JWT from localStorage on every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('jwt_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

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
