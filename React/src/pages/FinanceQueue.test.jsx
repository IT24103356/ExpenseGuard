import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import FinanceQueue from './FinanceQueue';
import * as api from '../services/api';

vi.mock('../services/api', () => ({
  getFinanceQueue: vi.fn(), processReimbursement: vi.fn(), submitPayment: vi.fn(),
}));

describe('FinanceQueue', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders numeric claim ids without crashing', async () => {
    api.getFinanceQueue.mockResolvedValue({
      data: [{
        id: 3, expenseClaimId: 3, employeeId: 6, employeeName: 'Nimal Perera',
        departmentId: 2, departmentName: 'Engineering & IT', amount: 15000,
        currency: 'USD', status: 'APPROVED', paymentReference: null,
      }],
    });
    render(
      <MemoryRouter>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <FinanceQueue />
        </QueryClientProvider>
      </MemoryRouter>,
    );
    expect(await screen.findByText('#3')).toBeInTheDocument();
    expect(screen.getByText('Nimal Perera')).toBeInTheDocument();
  });
});
