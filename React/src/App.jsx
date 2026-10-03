import React, { useState, useEffect } from 'react';
import Navbar from './components/Navbar';
import ComplianceDashboard from './components/ComplianceDashboard';
import ManagerReviewQueue from './components/ManagerReviewQueue';
import ClaimReviewModal from './components/ClaimReviewModal';
import EmployeePortal from './components/EmployeePortal';
import PoliciesView from './components/PoliciesView';
import { api } from './services/api';

export default function App() {
  const [currentUser, setCurrentUser] = useState(null);
  const [activeTab, setActiveTab] = useState('dashboard');
  const [dashboardStats, setDashboardStats] = useState(null);
  const [queueClaims, setQueueClaims] = useState([]);
  const [filters, setFilters] = useState({});
  const [selectedClaim, setSelectedClaim] = useState(null);
  const [loading, setLoading] = useState(true);

  // Initialize and login with default Manager (Sarah Chen)
  useEffect(() => {
    async function initAuth() {
      try {
        const loggedIn = await api.login('sarah.chen', 'Password123!');
        setCurrentUser(loggedIn);
        await loadAllData({});
      } catch (err) {
        console.error('Initialization error:', err);
      } finally {
        setLoading(false);
      }
    }
    initAuth();
  }, []);

  const loadAllData = async (currentFilters = filters) => {
    try {
      const [statsData, queueData] = await Promise.all([
        api.getDashboardStats().catch(() => null),
        api.getReviewQueue(currentFilters).catch(() => []),
      ]);
      if (statsData) setDashboardStats(statsData);
      if (queueData) setQueueClaims(queueData);
    } catch (err) {
      console.error('Error fetching data:', err);
    }
  };

  const handleSwitchUser = async (username) => {
    try {
      setLoading(true);
      const loggedIn = await api.login(username, 'Password123!');
      setCurrentUser(loggedIn);
      await loadAllData(filters);
    } catch (err) {
      alert(`Login failed for ${username}: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectClaim = async (claimId) => {
    try {
      const claimDetails = await api.getClaimDetails(claimId);
      setSelectedClaim(claimDetails);
    } catch (err) {
      alert(`Could not load claim details: ${err.message}`);
    }
  };

  const handleFilterChange = async (newFilters) => {
    setFilters(newFilters);
    const data = await api.getReviewQueue(newFilters);
    setQueueClaims(data);
  };

  const handleApprove = async (claimId, comment) => {
    await api.approveClaim(claimId, comment);
    await loadAllData(filters);
  };

  const handleReject = async (claimId, reason) => {
    await api.rejectClaim(claimId, reason);
    await loadAllData(filters);
  };

  const handleRequestRevision = async (claimId, comment) => {
    await api.requestRevision(claimId, comment);
    await loadAllData(filters);
  };

  if (loading && !currentUser) {
    return (
      <div style={{ display: 'flex', height: '100vh', alignItems: 'center', justifyContent: 'center', flexDirection: 'column', gap: '1rem' }}>
        <div style={{ fontSize: '1.5rem', fontWeight: 800 }}>SpendGuard Policy & Compliance</div>
        <div style={{ color: 'var(--text-muted)' }}>Connecting to ASP.NET Core API on port 5104...</div>
      </div>
    );
  }

  return (
    <div>
      <Navbar
        activeTab={activeTab}
        setActiveTab={setActiveTab}
        currentUser={currentUser}
        onSwitchUser={handleSwitchUser}
        onRefresh={() => loadAllData(filters)}
      />

      <main className="main-content">
        {activeTab === 'dashboard' && (
          <ComplianceDashboard
            stats={dashboardStats}
            onSelectClaim={handleSelectClaim}
            onNavigateQueue={() => setActiveTab('review-queue')}
          />
        )}

        {activeTab === 'review-queue' && (
          <ManagerReviewQueue
            claims={queueClaims}
            onSelectClaim={handleSelectClaim}
            onFilterChange={handleFilterChange}
            filters={filters}
          />
        )}

        {activeTab === 'employee' && (
          <EmployeePortal
            currentUser={currentUser}
            onSelectClaim={handleSelectClaim}
          />
        )}

        {activeTab === 'policies' && <PoliciesView />}
      </main>

      {/* Detail & Action Review Modal */}
      {selectedClaim && (
        <ClaimReviewModal
          claim={selectedClaim}
          currentUser={currentUser}
          onClose={() => setSelectedClaim(null)}
          onApprove={handleApprove}
          onReject={handleReject}
          onRequestRevision={handleRequestRevision}
        />
      )}
    </div>
  );
}
