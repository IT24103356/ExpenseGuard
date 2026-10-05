import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getApprovalQueue } from '../services/api';
import { AmountDisplay, EmptyState, ErrorState, LoadingSpinner, StatusBadge } from '../components/Shared';

export default function ApprovalQueue() {
  const queue = useQuery({
    queryKey: ['approval-queue'],
    queryFn: async () => (await getApprovalQueue()).data,
  });
  return <div className="page-content">
    <div className="page-heading"><h2>My approval queue</h2>
      <button className="btn btn-ghost btn-sm" onClick={() => queue.refetch()} disabled={queue.isFetching}>Refresh</button></div>
    {queue.isPending && <LoadingSpinner />}
    {queue.isError && <ErrorState error={queue.error} onRetry={queue.refetch} />}
    {queue.data?.length === 0 && <EmptyState message="No reimbursements await your role." />}
    {queue.data?.length > 0 && <div className="table-wrapper"><table>
      <thead><tr><th>Claim</th><th>Employee</th><th>Amount</th><th>Status</th><th>Action</th></tr></thead>
      <tbody>{queue.data.map(item => <tr key={item.id}>
        <td>{item.expenseClaimId}</td><td>{item.employeeId}</td>
        <td><AmountDisplay amount={item.amount} currency={item.currency} /></td>
        <td><StatusBadge status={item.status} /></td>
        <td><Link className="btn btn-primary btn-sm" to={`/approvals/${item.id}`}>Review</Link></td>
      </tr>)}</tbody>
    </table></div>}
  </div>;
}
