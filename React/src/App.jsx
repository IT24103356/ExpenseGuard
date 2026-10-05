import { BrowserRouter, Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import './index.css';
import { useAuth } from './auth/AuthContext';
import Sidebar from './components/Sidebar';
import { LoadingSpinner } from './components/Shared';
import FinanceDashboard from './pages/FinanceDashboard';
import FinanceQueue from './pages/FinanceQueue';
import ReimbursementDetails from './pages/ReimbursementDetails';
import BudgetDashboard from './pages/BudgetDashboard';
import SpendReports from './pages/SpendReports';
import WorkflowAudit from './pages/WorkflowAudit';
import AdminBudgets from './pages/AdminBudgets';
import Login from './pages/Login';
import Roles from './pages/Roles';
import ApprovalQueue from './pages/ApprovalQueue';
import EmployeeWorkspace from './pages/EmployeeWorkspace';
import EmployeeClaimDetail from './pages/EmployeeClaimDetail';
import PolicyManagement from './pages/PolicyManagement';
import { FraudDetail, FraudQueue } from './pages/FraudReview';

function ProtectedRoute({ roles }) {
  const auth = useAuth();
  const location = useLocation();
  if (auth.isBootstrapping) return <LoadingSpinner />;
  if (!auth.isAuthenticated) return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  if (roles && !auth.hasRole(...roles)) return <Navigate to="/forbidden" replace />;
  return <Outlet />;
}

function Shell() {
  const { session, logout } = useAuth();
  return (
    <div className="app-layout">
      <Sidebar />
      <main className="main-content">
        <div className="topbar">
          <h1>ExpenseGuard</h1>
          <div className="topbar-actions">
            <span>{session.username} · {session.role}</span>
            <button className="btn btn-ghost btn-sm" onClick={logout}>Sign out</button>
          </div>
        </div>
        <Outlet />
      </main>
    </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<Shell />}>
            <Route index element={<FinanceDashboard />} />
            <Route path="reimbursements/:id" element={<ReimbursementDetails />} />
            <Route path="employee" element={<EmployeeWorkspace />} />
            <Route path="employee/claims/:id" element={<EmployeeClaimDetail />} />
            <Route path="forbidden" element={<div className="page-content"><div className="alert alert-danger" role="alert">Access denied.</div></div>} />
            <Route element={<ProtectedRoute roles={['Finance', 'Admin']} />}>
              <Route path="finance-queue" element={<FinanceQueue />} />
              <Route path="budgets" element={<BudgetDashboard />} />
              <Route path="reports" element={<SpendReports />} />
              <Route path="workflows" element={<WorkflowAudit />} />
              <Route path="workflows/:id" element={<WorkflowAudit />} />
              <Route path="policies" element={<PolicyManagement />} />
              <Route path="fraud" element={<FraudQueue />} />
              <Route path="fraud/:id" element={<FraudDetail />} />
            </Route>
            <Route element={<ProtectedRoute roles={['Manager', 'DepartmentHead', 'Admin']} />}>
              <Route path="approvals" element={<ApprovalQueue />} />
              <Route path="approvals/:id" element={<ReimbursementDetails />} />
            </Route>
            <Route element={<ProtectedRoute roles={['Admin']} />}>
              <Route path="admin/budgets" element={<AdminBudgets />} />
              <Route path="admin/roles" element={<Roles />} />
            </Route>
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
