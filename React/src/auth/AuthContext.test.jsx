import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider, useAuth } from './AuthContext';

vi.mock('../services/api', () => ({
  loginRequest: vi.fn(),
  setUnauthorizedHandler: vi.fn(),
}));

function SessionProbe() {
  const auth = useAuth();
  return <span>{auth.isBootstrapping ? 'loading' : auth.session?.username ?? 'anonymous'}</span>;
}

describe('AuthProvider bootstrap', () => {
  afterEach(() => localStorage.clear());

  it('restores a non-expired session from storage', async () => {
    localStorage.setItem('expenseguard.session', JSON.stringify({
      token: 'token', username: 'alice', role: 'Employee', employeeId: 7,
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    }));
    render(<AuthProvider><SessionProbe /></AuthProvider>);
    await waitFor(() => expect(screen.getByText('alice')).toBeInTheDocument());
  });

  it('removes an expired session', async () => {
    localStorage.setItem('expenseguard.session', JSON.stringify({
      token: 'token', username: 'alice', expiresAt: '2020-01-01T00:00:00Z',
    }));
    render(<AuthProvider><SessionProbe /></AuthProvider>);
    await waitFor(() => expect(screen.getByText('anonymous')).toBeInTheDocument());
    expect(localStorage.getItem('expenseguard.session')).toBeNull();
  });
});
