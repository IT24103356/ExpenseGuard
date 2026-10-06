import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { decideReimbursement, getReimbursement, processReimbursement, startApproval, submitPayment } from '../services/api';
import { AmountDisplay, ApprovalProgress, ErrorState, LoadingSpinner, StatusBadge, apiErrorMessage, stageLabel } from '../components/Shared';
import { useAuth } from '../auth/AuthContext';

const canPreview = receipt => {
  const url = receipt?.storageUrl || '';
  return /^https?:\/\//i.test(url) && !url.includes('local.invalid');
};
const latestReceipt = receipts => receipts?.length ? receipts[receipts.length - 1] : null;

export default function ReimbursementDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const auth = useAuth();
  const queryClient = useQueryClient();
  const [actionMsg, setActionMsg] = useState(null);
  const [comment, setComment] = useState('');
  const [templateId, setTemplateId] = useState('');
  const reimbursement = useQuery({ queryKey: ['reimbursement', id], queryFn: async () => (await getReimbursement(id)).data });
  const action = useMutation({
    mutationFn: async ({ type }) => {
      if (type === 'process') return processReimbursement(id);
      if (type === 'pay') return submitPayment(id);
      if (type === 'start') return startApproval(id, Number(templateId));
      return decideReimbursement(id, type, comment);
    },
    onSuccess: (_, variables) => {
      setActionMsg({ type: 'success', text: `${variables.type.replaceAll('-', ' ')} completed.` });
      setComment('');
      queryClient.invalidateQueries({ queryKey: ['reimbursement', id] });
      queryClient.invalidateQueries({ queryKey: ['approval-queue'] });
    },
    onError: error => setActionMsg({ type: 'danger', text: apiErrorMessage(error) }),
  });

  if (reimbursement.isPending && !reimbursement.data) return <div className="page-content"><LoadingSpinner /></div>;
  if (reimbursement.isError && !reimbursement.data) return <div className="page-content"><ErrorState error={reimbursement.error} onRetry={reimbursement.refetch} /></div>;
  const r = reimbursement.data;
  const review = r.review;
  const receipt = latestReceipt(r.receipts);
  const canApprove = auth.hasRole('Manager', 'DepartmentHead', 'Finance', 'Admin');
  const canFinance = auth.hasRole('Finance', 'Admin');
  const pending = r.status === 'PENDING_APPROVAL';

  return (
    <div className="page-content">
      <button className="btn btn-ghost btn-sm" type="button" onClick={() => navigate(-1)}>← Back</button>
      <div className="employee-heading">
        <div>
          <h2>Claim #{r.expenseClaimId}</h2>
          <p>{r.employeeName || `Employee #${r.employeeId}`} · {r.departmentName || `Department #${r.departmentId}`}</p>
          <ApprovalProgress steps={r.approvalSteps} currentRole={r.currentRequiredRole} />
        </div>
        <StatusBadge status={r.status} />
      </div>
      {actionMsg && <div className={`alert alert-${actionMsg.type}`}>{actionMsg.text}</div>}
      <div className="review-grid">
        <article className="card">
          <h3>Claim</h3>
          <Detail label="Category" value={r.category || review?.category || '—'} />
          <Detail label="Vendor" value={r.vendor || receipt?.extractedVendor || 'Not provided'} />
          <Detail label="Amount" value={<AmountDisplay amount={r.amount} currency={r.currency} />} />
          <p className="employee-secondary">{r.description || 'No description'}</p>
        </article>
        <article className={`card review-summary ${review?.hasFlags || r.hasFlags ? 'flagged' : ''}`}>
          <h3>AI review summary</h3>
          <p className="employee-secondary">Based on the receipt image and OCR text, then policy, fraud, and budget checks.</p>
          <p>{review?.summary || 'Review findings are not available yet.'}</p>
        </article>
      </div>
      <div className="review-grid">
        <article className="card">
          <h3>Receipt and OCR</h3>
          {receipt ? <>
            {canPreview(receipt) && receipt.contentType?.startsWith('image/') && (
              <img className="receipt-preview" src={receipt.storageUrl} alt={receipt.fileName || 'Receipt'} />
            )}
            {canPreview(receipt) && receipt.contentType === 'application/pdf' && (
              <a className="btn btn-ghost btn-sm" href={receipt.storageUrl} target="_blank" rel="noreferrer">Open {receipt.fileName || 'PDF'}</a>
            )}
            <Detail label="File" value={receipt.fileName || 'Receipt'} />
            <Detail label="OCR vendor" value={receipt.extractedVendor || '—'} />
            <Detail label="OCR amount" value={receipt.extractedAmount != null ? `${receipt.extractedCurrency || r.currency} ${receipt.extractedAmount}` : '—'} />
            {receipt.extractedText && <label className="form-group"><span className="form-label">OCR text</span>
              <textarea className="form-control" readOnly rows={6} value={receipt.extractedText} /></label>}
          </> : <p className="employee-secondary">No receipt is attached.</p>}
        </article>
        <Section title="Policy" section={review?.policy} />
        <Section title="Fraud risk" section={review?.fraud} />
        <Section title="Budget" section={review?.budget} currency={r.currency} />
      </div>
      {canFinance && r.status === 'APPROVED' && (
        <button className="btn btn-primary" disabled={action.isPending} onClick={() => action.mutate({ type: 'process' })}>Process payment</button>
      )}
      {canFinance && r.status === 'PROCESSING' && (
        <button className="btn btn-success" disabled={action.isPending} onClick={() => action.mutate({ type: 'pay' })}>Submit payment</button>
      )}
      {canApprove && (
        <div className="card">
          <h3>Decision</h3>
          <p className="employee-secondary">Current stage: {stageLabel(r.currentRequiredRole)}</p>
          {!pending && (
            <div className="form-group">
              <label className="form-label" htmlFor="template-id">Workflow template ID</label>
              <input id="template-id" className="form-control" inputMode="numeric" value={templateId} onChange={e => setTemplateId(e.target.value)} />
              <button className="btn btn-primary" disabled={action.isPending || !/^[1-9]\d*$/.test(templateId)}
                onClick={() => action.mutate({ type: 'start' })}>Start approval</button>
            </div>
          )}
          {pending && <>
            <label className="form-label" htmlFor="decision-comment">Comment</label>
            <textarea id="decision-comment" className="form-control" value={comment} onChange={e => setComment(e.target.value)} rows={3} />
            <div className="decision-actions">
              <button className="btn btn-success" disabled={action.isPending} onClick={() => action.mutate({ type: 'approve' })}>Approve</button>
              <button className="btn btn-danger" disabled={action.isPending || !comment.trim()} onClick={() => action.mutate({ type: 'reject' })}>Reject</button>
              <button className="btn btn-ghost" disabled={action.isPending || !comment.trim()} onClick={() => action.mutate({ type: 'revise' })}>Request revision</button>
            </div>
          </>}
        </div>
      )}
      <p><Link to="/approvals">Return to approval queue</Link></p>
    </div>
  );
}

function Section({ title, section, currency }) {
  if (!section) return <article className="card"><h3>{title}</h3><p className="employee-secondary">No finding.</p></article>;
  return (
    <article className={`card review-section ${section.outcome}`}>
      <h3>{title}</h3>
      <StatusBadge status={section.outcome} />
      <p>{section.summary}</p>
      {section.requested != null && (
        <p className="employee-secondary">Requested {section.requested.toLocaleString()} {currency} · remaining {section.available?.toLocaleString() ?? '—'}</p>
      )}
      {section.flags?.length > 0 && (
        <ul className="review-flags">{section.flags.map(flag => (
          <li key={flag.code}><strong>{flag.severity}</strong> {flag.message}</li>
        ))}</ul>
      )}
    </article>
  );
}

function Detail({ label, value }) {
  return <div className="review-detail"><span>{label}</span><strong>{value}</strong></div>;
}
