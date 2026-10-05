import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getReimbursement, getWorkflowByClaim, submitPayment, processReimbursement } from '../services/api';
import { StatusBadge, LoadingSpinner, AmountDisplay } from '../components/Shared';
import WorkflowMonitor from './WorkflowMonitor';
import { FiArrowLeft, FiCreditCard, FiPlay } from 'react-icons/fi';

export default function ReimbursementDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [reimb, setReimb] = useState(null);
  const [workflow, setWorkflow] = useState(null);
  const [loading, setLoading] = useState(true);
  const [actionMsg, setActionMsg] = useState(null);

  useEffect(() => {
    setLoading(true);
    getReimbursement(id)
      .then(r => {
        setReimb(r.data);
        return getWorkflowByClaim(r.data.expenseClaimId);
      })
      .then(r => setWorkflow(r.data))
      .catch(() => setReimb(getMockDetail(id)))
      .finally(() => setLoading(false));
  }, [id]);

  const handleProcess = async () => {
    try {
      const r = await processReimbursement(id);
      setReimb(r.data);
      setActionMsg({ type: 'success', text: 'Moved to Processing' });
    } catch (e) {
      setActionMsg({ type: 'danger', text: e.response?.data?.error || 'Error processing' });
    }
  };

  const handlePay = async () => {
    try {
      const r = await submitPayment(id);
      setReimb(r.data);
      setActionMsg({ type: 'success', text: `Payment ${r.data.status === 'PAID' ? 'COMPLETED' : 'submitted'}` });
    } catch (e) {
      setActionMsg({ type: 'danger', text: e.response?.data?.error || 'Payment failed' });
    }
  };

  if (loading) return <div className="page-content"><LoadingSpinner /></div>;
  const r = reimb || getMockDetail(id);

  return (
    <div className="page-content">
      <button id="btn-back" className="btn btn-ghost btn-sm" onClick={() => navigate(-1)} style={{ marginBottom: 20 }}>
        <FiArrowLeft /> Back
      </button>

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 24 }}>
        <div>
          <h2 style={{ fontSize: 22, fontWeight: 800 }}>Reimbursement Details</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: 13, fontFamily: 'monospace' }}>{r.id}</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          {(r.status === 'APPROVED' || r.status === 'READY_FOR_FINANCE') && (
            <button id="btn-detail-process" className="btn btn-primary" onClick={handleProcess}>
              <FiPlay /> Process
            </button>
          )}
          {r.status === 'PROCESSING' && (
            <button id="btn-detail-pay" className="btn btn-success" onClick={handlePay}>
              <FiCreditCard /> Submit Payment
            </button>
          )}
        </div>
      </div>

      {actionMsg && (
        <div className={`alert alert-${actionMsg.type}`} style={{ marginBottom: 16 }}>
          {actionMsg.text}
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 20 }}>
        {/* Reimbursement Info */}
        <div className="card">
          <h3 style={{ marginBottom: 16, fontSize: 15 }}>Reimbursement Information</h3>
          <DetailRow label="Employee ID" value={r.employeeId} />
          <DetailRow label="Department" value={r.departmentId} />
          <DetailRow label="Claim ID" value={<span style={{ fontFamily: 'monospace', fontSize: 12 }}>{r.expenseClaimId}</span>} />
          <DetailRow label="Amount" value={<AmountDisplay amount={r.amount} />} />
          <DetailRow label="Currency" value={r.currency} />
          <DetailRow label="Status" value={<StatusBadge status={r.status} />} />
          <DetailRow label="Requested" value={new Date(r.requestedAt).toLocaleString()} />
          {r.processedAt && <DetailRow label="Processed" value={new Date(r.processedAt).toLocaleString()} />}
          {r.completedAt && <DetailRow label="Completed" value={new Date(r.completedAt).toLocaleString()} />}
        </div>

        {/* Payment Info */}
        <div className="card">
          <h3 style={{ marginBottom: 16, fontSize: 15 }}>Payment Information</h3>
          <DetailRow label="Payment Reference" value={r.paymentReference || '—'} />
          <DetailRow label="Payment Provider" value={r.paymentProvider || '—'} />
          <DetailRow label="Retry Count" value={r.retryCount || 0} />
          {r.failureReason && (
            <div className="alert alert-danger" style={{ marginTop: 12 }}>
              ❌ {r.failureReason}
            </div>
          )}
          {r.status === 'PAID' && (
            <div className="alert alert-success" style={{ marginTop: 12 }}>
              ✅ Payment completed successfully
            </div>
          )}
          {r.status === 'BUDGET_REVIEW_REQUIRED' && (
            <div className="alert alert-warning" style={{ marginTop: 12 }}>
              ⚠️ Insufficient budget — requires Finance review
            </div>
          )}
        </div>
      </div>

      {/* Workflow Monitor embedded */}
      {workflow && (
        <div style={{ marginTop: 4 }}>
          <h3 style={{ marginBottom: 16, fontSize: 16 }}>Workflow Execution</h3>
          <WorkflowMonitor embedded workflow={workflow} />
        </div>
      )}
    </div>
  );
}

function DetailRow({ label, value }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)', alignItems: 'center' }}>
      <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>{label}</span>
      <span style={{ fontSize: 14 }}>{value}</span>
    </div>
  );
}

function getMockDetail(id) {
  return {
    id, employeeId: 'EMP-001', departmentId: 'DEPT-ENG',
    expenseClaimId: 'a1b2c3d4-1234-5678-abcd-ef1234567890',
    amount: 25000, currency: 'LKR', status: 'PROCESSING',
    paymentReference: null, paymentProvider: null, retryCount: 0,
    requestedAt: '2026-09-15T10:00:00Z', failureReason: null
  };
}
