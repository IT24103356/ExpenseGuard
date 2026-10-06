import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ReimbursementDetails from './ReimbursementDetails';
import * as api from '../services/api';

vi.mock('../services/api', () => ({
  decideReimbursement: vi.fn(), getReimbursement: vi.fn(),
  processReimbursement: vi.fn(), startApproval: vi.fn(), submitPayment: vi.fn(),
}));

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ hasRole: (...roles) => roles.includes('Manager') }),
}));

describe('ReimbursementDetails', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows the receipt, OCR, and AI summary for approval', async () => {
    api.getReimbursement.mockResolvedValue({
      data: {
        id: 7, expenseClaimId: 3, employeeId: 6, employeeName: 'Nimal Perera',
        departmentName: 'Engineering & IT', category: 'advertising', description: 'social media',
        amount: 15000, currency: 'USD', status: 'PENDING_APPROVAL', currentRequiredRole: 'Manager',
        approvalSteps: [{ sequence: 1, requiredRole: 'Manager', status: 'PENDING' }],
        hasFlags: true,
        review: {
          hasFlags: true,
          summary: 'Receipt OCR matched BrightReach Media. Department budget could not reserve the claim.',
          policy: { outcome: 'not_applicable', summary: 'No applicable policy was found.', flags: [] },
          fraud: { outcome: 'low', summary: 'No fraud flags from the receipt and claim checks.', flags: [] },
          budget: { outcome: 'exceeded', summary: 'No active budget could reserve the claim amount.', flags: [{ code: 'BUDGET', severity: 'high', message: 'No active budget could reserve the claim amount.' }] },
        },
        receipts: [{
          receiptId: 3, fileName: 'Image.png', contentType: 'image/png',
          storageUrl: 'https://files.example/ads.png', extractedVendor: 'BrightReach Media',
          extractedAmount: 15000, extractedCurrency: 'USD', extractedText: 'RECEIPT\nTotal 15000',
        }],
      },
    });
    render(
      <MemoryRouter initialEntries={['/approvals/7']}>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <Routes><Route path="/approvals/:id" element={<ReimbursementDetails />} /></Routes>
        </QueryClientProvider>
      </MemoryRouter>,
    );
    expect(await screen.findByRole('img', { name: 'Image.png' })).toBeInTheDocument();
    expect(screen.getAllByText(/BrightReach Media/).length).toBeGreaterThan(0);
    expect(screen.getByText(/Department budget could not reserve/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Approve' })).toBeInTheDocument();
  });
});
