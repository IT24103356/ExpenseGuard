import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { getFinanceQueue, processReimbursement, submitPayment } from '../services/api';
import { StatusBadge, LoadingSpinner, AmountDisplay, Pagination } from '../components/Shared';
import { FiRefreshCw, FiSearch, FiEye, FiPlay, FiCreditCard } from 'react-icons/fi';

const STATUS_OPTIONS = ['', 'APPROVED', 'READY_FOR_FINANCE', 'PROCESSING', 'PAYMENT_PENDING', 'PAID', 'PAYMENT_FAILED', 'ON_HOLD', 'BUDGET_REVIEW_REQUIRED'];

export default function FinanceQueue() {
  const navigate = useNavigate();
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState({ status: '', departmentId: '', searchTerm: '', sortBy: 'requestedAt', sortDesc: true });
  const [actionLoading, setActionLoading] = useState(null);
  const [toast, setToast] = useState(null);

  const fetchQueue = useCallback(() => {
    setLoading(true);
    getFinanceQueue({ ...filters, page, pageSize: 15 })
      .then(r => {
        setItems(r.data.items || getMockQueue().items);
        setTotal(r.data.totalCount || getMockQueue().totalCount);
        setTotalPages(r.data.totalPages || 1);
      })
      .catch(() => {
        const mock = getMockQueue();
        setItems(mock.items);
        setTotal(mock.totalCount);
        setTotalPages(1);
      })
      .finally(() => setLoading(false));
  }, [filters, page]);

  useEffect(() => { fetchQueue(); }, [fetchQueue]);

  const showToast = (msg, type = 'success') => {
    setToast({ msg, type });
    setTimeout(() => setToast(null), 3500);
  };

  const handleProcess = async (id) => {
    setActionLoading(id + '-process');
    try {
      await processReimbursement(id);
      showToast('Reimbursement moved to processing');
      fetchQueue();
    } catch (e) {
      showToast(e.response?.data?.error || 'Failed to process', 'danger');
    } finally { setActionLoading(null); }
  };

  const handlePay = async (id) => {
    setActionLoading(id + '-pay');
    try {
      await submitPayment(id);
      showToast('Payment submitted to sandbox');
      fetchQueue();
    } catch (e) {
      showToast(e.response?.data?.error || 'Payment failed', 'danger');
    } finally { setActionLoading(null); }
  };

  return (
    <div className="page-content">
      {toast && (
        <div className={`alert alert-${toast.type}`} style={{ position: 'fixed', top: 80, right: 24, zIndex: 1000, minWidth: 300 }}>
          {toast.msg}
        </div>
      )}

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ fontSize: 22, fontWeight: 800 }}>Finance Processing Queue</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: 14 }}>{total} total reimbursements</p>
        </div>
        <button id="btn-refresh-queue" className="btn btn-ghost btn-sm" onClick={fetchQueue}>
          <FiRefreshCw /> Refresh
        </button>
      </div>

      <div className="table-wrapper">
        {/* Filter Bar */}
        <div className="filter-bar">
          <div style={{ position: 'relative' }}>
            <FiSearch style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
            <input
              id="search-input"
              style={{ paddingLeft: 30 }}
              placeholder="Search employee / dept..."
              value={filters.searchTerm}
              onChange={e => setFilters(f => ({ ...f, searchTerm: e.target.value }))}
            />
          </div>
          <select
            id="status-filter"
            value={filters.status}
            onChange={e => setFilters(f => ({ ...f, status: e.target.value }))}
          >
            {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s || 'All Statuses'}</option>)}
          </select>
          <input
            id="dept-filter"
            placeholder="Department ID"
            value={filters.departmentId}
            onChange={e => setFilters(f => ({ ...f, departmentId: e.target.value }))}
          />
          <select
            id="sort-filter"
            value={filters.sortBy}
            onChange={e => setFilters(f => ({ ...f, sortBy: e.target.value }))}
          >
            <option value="requestedAt">Date</option>
            <option value="amount">Amount</option>
            <option value="status">Status</option>
          </select>
        </div>

        {/* Table */}
        {loading ? <LoadingSpinner /> : (
          <table>
            <thead>
              <tr>
                <th>Employee</th>
                <th>Department</th>
                <th>Claim ID</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Requested</th>
                <th>Payment Ref</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id} id={`queue-row-${item.id}`}>
                  <td>{item.employeeId}</td>
                  <td>{item.departmentId}</td>
                  <td style={{ fontFamily: 'monospace', fontSize: 12, color: 'var(--text-muted)' }}>
                    {item.expenseClaimId?.substring(0, 8)}…
                  </td>
                  <td><AmountDisplay amount={item.amount} /></td>
                  <td><StatusBadge status={item.status} /></td>
                  <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                    {new Date(item.requestedAt).toLocaleDateString()}
                  </td>
                  <td style={{ fontFamily: 'monospace', fontSize: 12 }}>
                    {item.paymentReference || '—'}
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button
                        id={`btn-view-${item.id}`}
                        className="btn btn-ghost btn-sm"
                        onClick={() => navigate(`/reimbursements/${item.id}`)}
                      ><FiEye /></button>
                      {(item.status === 'APPROVED' || item.status === 'READY_FOR_FINANCE') && (
                        <button
                          id={`btn-process-${item.id}`}
                          className="btn btn-primary btn-sm"
                          disabled={actionLoading === item.id + '-process'}
                          onClick={() => handleProcess(item.id)}
                        ><FiPlay /> Process</button>
                      )}
                      {item.status === 'PROCESSING' && (
                        <button
                          id={`btn-pay-${item.id}`}
                          className="btn btn-success btn-sm"
                          disabled={actionLoading === item.id + '-pay'}
                          onClick={() => handlePay(item.id)}
                        ><FiCreditCard /> Pay</button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
      </div>
    </div>
  );
}

function getMockQueue() {
  return {
    items: [
      { id: '1', employeeId: 'EMP-001', departmentId: 'DEPT-ENG', expenseClaimId: 'a1b2c3d4-1234-5678-abcd-ef1234567890', amount: 25000, status: 'APPROVED', requestedAt: '2026-09-15T10:00:00Z', paymentReference: null },
      { id: '2', employeeId: 'EMP-002', departmentId: 'DEPT-MKT', expenseClaimId: 'b2c3d4e5-2345-6789-bcde-f12345678901', amount: 75000, status: 'PROCESSING', requestedAt: '2026-09-14T14:30:00Z', paymentReference: null },
      { id: '3', employeeId: 'EMP-003', departmentId: 'DEPT-HR', expenseClaimId: 'c3d4e5f6-3456-789a-cdef-123456789012', amount: 12500, status: 'PAID', requestedAt: '2026-09-10T09:00:00Z', paymentReference: 'PAY-10001' },
      { id: '4', employeeId: 'EMP-004', departmentId: 'DEPT-ENG', expenseClaimId: 'd4e5f6a7-4567-89ab-def0-234567890123', amount: 180000, status: 'BUDGET_REVIEW_REQUIRED', requestedAt: '2026-09-12T11:00:00Z', paymentReference: null },
      { id: '5', employeeId: 'EMP-005', departmentId: 'DEPT-FIN', expenseClaimId: 'e5f6a7b8-5678-9abc-ef01-345678901234', amount: 5000, status: 'PAYMENT_FAILED', requestedAt: '2026-09-11T16:00:00Z', paymentReference: 'PAY-10002' },
    ],
    totalCount: 5
  };
}
