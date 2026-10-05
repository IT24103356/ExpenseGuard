import { render, screen } from '@testing-library/react';
import { AuthProvider } from '../auth/AuthContext';
import { AccessGate, QueryState } from './RequestState';

describe('role-aware request states', () => {
  it('shows authentication guidance without a session', () => {
    render(<AuthProvider value={{ isAuthenticated: false, roles: [], hasAnyRole: () => false }}><AccessGate roles={['Admin']}><div>secret</div></AccessGate></AuthProvider>);
    expect(screen.getByText('Sign in required')).toBeInTheDocument();
    expect(screen.queryByText('secret')).not.toBeInTheDocument();
  });

  it('shows forbidden guidance for the wrong role', () => {
    render(<AuthProvider value={{ isAuthenticated: true, roles: ['Employee'], hasAnyRole: (...roles) => roles.includes('Employee') }}><AccessGate roles={['Admin']}><div>secret</div></AccessGate></AuthProvider>);
    expect(screen.getByText('Access denied')).toBeInTheDocument();
  });

  it('uses a safe message for service failures', () => {
    render(<QueryState query={{ isLoading: false, isError: true, error: {}, refetch: vi.fn() }}>content</QueryState>);
    expect(screen.getByText(/No changes were assumed/)).toBeInTheDocument();
  });
});

