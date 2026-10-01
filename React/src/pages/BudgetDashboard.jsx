import { useState, useEffect } from 'react';
import { getAllBudgets, getBudgetSummary } from '../services/api';
import { LoadingSpinner, AmountDisplay, ProgressBar } from '../components/Shared';
import { useNavigate } from 'react-router-dom';
import { FiPlusCircle } from 'react-icons/fi';

export default function BudgetDashboard() {
  const navigate = useNavigate();
  const [budgets, setBudgets] = useState([]);
  const [loading, setLoading] = useState(true);
  const [year, setYear] = useState(new Date().getFullYear());

  useEffect(() => {
    setLoading(true);
    getAllBudgets(year)
      .then(r => setBudgets(r.data?.length ? r.data : getMockBudgets()))
      .catch(() => setBudgets(getMockBudgets()))
      .finally(() => setLoading(false));
  }, [year]);

  if (loading) return <div className="page-content"><LoadingSpinner /></div>;

  const totalAllocated = budgets.reduce((s, b) => s + b.allocatedAmount, 0);
  const totalSpent = budgets.reduce((s, b) => s + b.approvedSpend, 0);
  const totalRemaining = budgets.reduce((s, b) => s + b.remainingBudget, 0);

  return (
    <div className="page-content">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <h2 style={{ fontSize: 22, fontWeight: 800 }}>Budget Overview</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: 14 }}>Department allocations and utilization</p>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <select id="budget-year-select" className="form-control" style={{ width: 120 }}
            value={year} onChange={e => setYear(Number(e.target.value))}>
            {[2024, 2025, 2026, 2027].map(y => <option key={y} value={y}>{y}</option>)}
          </select>
          <button id="btn-create-budget" className="btn btn-primary" onClick={() => navigate('/admin/budgets')}>
            <FiPlusCircle /> Manage
          </button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="stats-grid" style={{ marginBottom: 24 }}>
        <div className="stat-card primary">
          <div className="stat-label">Total Allocated</div>
          <div className="stat-value"><AmountDisplay amount={totalAllocated} large /></div>
          <div className="stat-sub">All departments FY{year}</div>
        </div>
        <div className="stat-card warning">
          <div className="stat-label">Total Spent</div>
          <div className="stat-value"><AmountDisplay amount={totalSpent} large /></div>
          <div className="stat-sub">{((totalSpent/totalAllocated)*100).toFixed(1)}% of total budget</div>
        </div>
        <div className="stat-card success">
          <div className="stat-label">Remaining</div>
          <div className="stat-value"><AmountDisplay amount={totalRemaining} large /></div>
          <div className="stat-sub">Available to allocate</div>
        </div>
      </div>

      {/* Department Budget Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))', gap: 16 }}>
        {budgets.map(budget => (
          <div key={budget.id} className="card" id={`budget-card-${budget.departmentId}`}
            style={{ cursor: 'pointer' }}
            onClick={() => navigate(`/budgets/${budget.id}`)}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 16 }}>
              <div>
                <h3 style={{ fontSize: 16, fontWeight: 700 }}>{budget.departmentName}</h3>
                <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{budget.departmentId} • FY{budget.fiscalYear}</div>
              </div>
              <span style={{
                fontSize: 11, padding: '2px 8px', borderRadius: 12,
                background: budget.isActive ? 'rgba(16,185,129,0.15)' : 'rgba(100,116,139,0.15)',
                color: budget.isActive ? 'var(--accent-success)' : 'var(--text-muted)'
              }}>
                {budget.isActive ? 'ACTIVE' : 'INACTIVE'}
              </span>
            </div>

            <ProgressBar value={budget.approvedSpend} max={budget.allocatedAmount} />

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 8, marginTop: 16 }}>
              <MiniStat label="Allocated" value={<AmountDisplay amount={budget.allocatedAmount} />} />
              <MiniStat label="Spent" value={<AmountDisplay amount={budget.approvedSpend} />} />
              <MiniStat label="Remaining" value={<AmountDisplay amount={budget.remainingBudget} />}
                color={budget.remainingBudget < 0 ? 'var(--accent-danger)' : 'var(--accent-success)'} />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function MiniStat({ label, value, color }) {
  return (
    <div style={{ textAlign: 'center' }}>
      <div style={{ fontSize: 10, color: 'var(--text-muted)', marginBottom: 4, textTransform: 'uppercase' }}>{label}</div>
      <div style={{ fontSize: 13, fontWeight: 700, color: color || 'var(--text-primary)' }}>{value}</div>
    </div>
  );
}

function getMockBudgets() {
  return [
    { id: '1', departmentId: 'DEPT-ENG', departmentName: 'Engineering', fiscalYear: 2026, allocatedAmount: 10000000, approvedSpend: 4200000, paidSpend: 4200000, remainingBudget: 5800000, utilizationPercentage: 42, currency: 'LKR', isActive: true },
    { id: '2', departmentId: 'DEPT-MKT', departmentName: 'Marketing', fiscalYear: 2026, allocatedAmount: 7500000, approvedSpend: 5600000, paidSpend: 5600000, remainingBudget: 1900000, utilizationPercentage: 74.7, currency: 'LKR', isActive: true },
    { id: '3', departmentId: 'DEPT-HR', departmentName: 'HR', fiscalYear: 2026, allocatedAmount: 5000000, approvedSpend: 1200000, paidSpend: 1200000, remainingBudget: 3800000, utilizationPercentage: 24, currency: 'LKR', isActive: true },
    { id: '4', departmentId: 'DEPT-FIN', departmentName: 'Finance', fiscalYear: 2026, allocatedAmount: 3000000, approvedSpend: 350000, paidSpend: 350000, remainingBudget: 2650000, utilizationPercentage: 11.7, currency: 'LKR', isActive: true },
    { id: '5', departmentId: 'DEPT-OPS', departmentName: 'Operations', fiscalYear: 2026, allocatedAmount: 6000000, approvedSpend: 5600000, paidSpend: 5600000, remainingBudget: 400000, utilizationPercentage: 93.3, currency: 'LKR', isActive: true },
  ];
}
