import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import EmployeeClaimDetail from './EmployeeClaimDetail';
import * as api from '../services/api';

vi.mock('../services/api', () => ({
  correctReceipt: vi.fn(), getClaim: vi.fn(), getClaimHistory: vi.fn(),
  getClaimReceipts: vi.fn(), resubmitClaim: vi.fn(), updateClaim: vi.fn(), uploadReceipt: vi.fn(),
}));

function renderDetail() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <MemoryRouter initialEntries={['/employee/claims/2']}>
      <QueryClientProvider client={client}>
        <Routes>
          <Route path="/employee/claims/:id" element={<EmployeeClaimDetail />} />
        </Routes>
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('EmployeeClaimDetail', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getClaimHistory.mockResolvedValue([]);
    api.getClaimReceipts.mockResolvedValue([]);
  });

  it('shows the saved receipt photo and OCR without asking for another upload', async () => {
    api.getClaim.mockResolvedValue({
      expenseClaimId: 2, category: 'advertising', description: 'social media',
      amount: 15000, currency: 'USD', status: 'UnderReview', version: 2,
      receipts: [{
        receiptId: 8, fileName: 'ads.jpg', contentType: 'image/jpeg',
        storageUrl: 'https://files.example/ads.jpg', processingStatus: 'Processed',
        extractedVendor: 'Meta', extractedAmount: 15000, extractedCurrency: 'USD',
        extractedText: 'Meta Ads\nTotal 15000', requiresManualReview: false, confidence: 0.91,
      }],
    });
    renderDetail();
    expect(await screen.findByRole('img', { name: 'ads.jpg' })).toHaveAttribute('src', 'https://files.example/ads.jpg');
    expect(screen.getByLabelText(/ocr text/i)).toHaveValue('Meta Ads\nTotal 15000');
    expect(screen.getByDisplayValue('Meta')).toBeInTheDocument();
    expect(screen.queryByLabelText(/receipt file/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /replace receipt/i })).not.toBeInTheDocument();
  });
});
