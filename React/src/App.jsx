import { BrowserRouter, Routes, Route } from 'react-router-dom';
import './index.css';
import Sidebar from './components/Sidebar';
import FinanceDashboard from './pages/FinanceDashboard';
import FinanceQueue from './pages/FinanceQueue';
import ReimbursementDetails from './pages/ReimbursementDetails';
import BudgetDashboard from './pages/BudgetDashboard';
import SpendReports from './pages/SpendReports';
import WorkflowMonitor from './pages/WorkflowMonitor';
import AdminBudgets from './pages/AdminBudgets';
import PolicyManagement from './pages/PolicyManagement';
import { FraudDetail, FraudQueue } from './pages/FraudReview';

function App() {
  return (
    <BrowserRouter>
      <div className="app-layout">
        <Sidebar />
        <main className="main-content">
          <div className="topbar">
            <h1>ReimbursementBudget — Finance Module</h1>
            <div className="topbar-actions">
              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>Coordinator/Planner Agent Active</span>
              <span style={{
                width: 8, height: 8, borderRadius: '50%', background: 'var(--accent-success)',
                boxShadow: '0 0 8px var(--accent-success)', display: 'inline-block'
              }} />
            </div>
          </div>
          <Routes>
            <Route path="/" element={<FinanceDashboard />} />
            <Route path="/finance-queue" element={<FinanceQueue />} />
            <Route path="/reimbursements/:id" element={<ReimbursementDetails />} />
            <Route path="/budgets" element={<BudgetDashboard />} />
            <Route path="/reports" element={<SpendReports />} />
            <Route path="/workflows" element={<WorkflowMonitor />} />
            <Route path="/workflows/:id" element={<WorkflowMonitor />} />
            <Route path="/admin/budgets" element={<AdminBudgets />} />
            <Route path="/policies" element={<PolicyManagement />} />
            <Route path="/fraud" element={<FraudQueue />} />
            <Route path="/fraud/:id" element={<FraudDetail />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
