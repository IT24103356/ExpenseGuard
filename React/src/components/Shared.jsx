export function StatusBadge({ status }) {
  const cls = status?.toLowerCase().replace(/_/g, '_') || 'pending';
  return <span className={`badge ${cls}`}>{status?.replace(/_/g, ' ')}</span>;
}

export function LoadingSpinner() {
  return <div className="loading-container"><div className="loading-spinner"/></div>;
}

export function AmountDisplay({ amount, currency = 'LKR', large }) {
  const formatted = new Intl.NumberFormat('en-LK', {
    style: 'currency', currency, maximumFractionDigits: 2
  }).format(amount || 0);
  return <span className={`amount ${large ? 'large' : ''}`}>{formatted}</span>;
}

export function ProgressBar({ value, max }) {
  const pct = max > 0 ? Math.min((value / max) * 100, 100) : 0;
  const colorClass = pct >= 90 ? 'progress-high' : pct >= 70 ? 'progress-medium' : 'progress-low';
  return (
    <div>
      <div className="budget-meter-labels">
        <span>{pct.toFixed(1)}% utilised</span>
        <span>{pct < 100 ? `${(100 - pct).toFixed(1)}% remaining` : 'Over budget!'}</span>
      </div>
      <div className="progress-bar-track">
        <div className={`progress-bar-fill ${colorClass}`} style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}

export function Pagination({ page, totalPages, onPageChange }) {
  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);
  return (
    <div className="pagination">
      <button className="page-btn" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>‹</button>
      {pages.slice(Math.max(0, page - 3), Math.min(totalPages, page + 2)).map(p => (
        <button
          key={p}
          id={`page-btn-${p}`}
          className={`page-btn ${p === page ? 'active' : ''}`}
          onClick={() => onPageChange(p)}
        >{p}</button>
      ))}
      <button className="page-btn" onClick={() => onPageChange(page + 1)} disabled={page >= totalPages}>›</button>
    </div>
  );
}
