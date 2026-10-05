import { Link, useLocation } from 'react-router-dom';
import { FiGrid, FiList, FiDollarSign, FiBarChart2, FiActivity, FiSettings, FiShield, FiAlertTriangle } from 'react-icons/fi';

const navItems = [
  { section: 'Finance', items: [
    { to: '/', icon: <FiGrid />, label: 'Finance Dashboard', id: 'nav-dashboard' },
    { to: '/finance-queue', icon: <FiList />, label: 'Processing Queue', id: 'nav-queue' },
  ]},
  { section: 'Budget', items: [
    { to: '/budgets', icon: <FiDollarSign />, label: 'Budget Overview', id: 'nav-budgets' },
    { to: '/reports', icon: <FiBarChart2 />, label: 'Spend Analytics', id: 'nav-reports' },
  ]},
  { section: 'Agentic AI', items: [
    { to: '/workflows', icon: <FiActivity />, label: 'Workflow Monitor', id: 'nav-workflows' },
  ]},
  { section: 'Risk & Compliance', items: [
    { to: '/policies', icon: <FiShield />, label: 'Policies', id: 'nav-policies' },
    { to: '/fraud', icon: <FiAlertTriangle />, label: 'Fraud Review', id: 'nav-fraud' },
  ]},
  { section: 'System', items: [
    { to: '/admin/budgets', icon: <FiSettings />, label: 'Admin: Budgets', id: 'nav-admin' },
  ]},
];

export default function Sidebar() {
  const location = useLocation();
  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <div className="logo-icon">💰</div>
        <div>
          <h2>ReimburseAI</h2>
          <span>Finance Module</span>
        </div>
      </div>
      <nav className="sidebar-nav">
        {navItems.map(section => (
          <div key={section.section}>
            <div className="nav-section-title">{section.section}</div>
            {section.items.map(item => (
              <Link
                key={item.to}
                to={item.to}
                id={item.id}
                className={`nav-item ${location.pathname === item.to ? 'active' : ''}`}
              >
                {item.icon}
                <span>{item.label}</span>
              </Link>
            ))}
          </div>
        ))}
      </nav>
    </aside>
  );
}
