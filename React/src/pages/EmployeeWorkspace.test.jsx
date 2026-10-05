import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import EmployeeWorkspace from './EmployeeWorkspace';
import * as api from '../services/api';

vi.mock('../services/api', () => ({
  createClaim: vi.fn(), createPurchaseRequest: vi.fn(), deleteClaim: vi.fn(),
  deletePurchaseRequest: vi.fn(), getEmployees: vi.fn(), getMyProfile: vi.fn(),
  getPurchaseRequests: vi.fn(), searchClaims: vi.fn(), submitClaim: vi.fn(),
  submitPurchaseRequest: vi.fn(),
}));

function renderWorkspace() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<MemoryRouter><QueryClientProvider client={client}><EmployeeWorkspace /></QueryClientProvider></MemoryRouter>);
}

describe('EmployeeWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.searchClaims.mockResolvedValue([]);
    api.getPurchaseRequests.mockResolvedValue([]);
  });

  it('shows a useful empty claim state', async () => {
    renderWorkspace();
    expect(await screen.findByText('No claims match these filters.')).toBeInTheDocument();
  });

  it('loads the current employee profile without an employee selector', async () => {
    api.getMyProfile.mockResolvedValue({
      employeeId: 7, fullName: 'Asha Perera', email: 'asha@example.com',
      username: 'asha', designation: 'Engineer', isActive: true, isLocked: false,
    });
    renderWorkspace();
    fireEvent.click(screen.getByRole('button', { name: 'Profile' }));
    expect(await screen.findByText('Asha Perera')).toBeInTheDocument();
    expect(screen.queryByLabelText(/employee id/i)).not.toBeInTheDocument();
  });
});
