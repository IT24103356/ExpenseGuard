import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import Login from './Login';

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ login: vi.fn(), isAuthenticated: false, session: null }),
}));

vi.mock('../auth/ThemeContext', () => ({
  useTheme: () => ({ isDark: false, toggleTheme: vi.fn() }),
}));

describe('Login', () => {
  it('shows a sign-in form with the expense hero and no social or register options', () => {
    render(<MemoryRouter><Login /></MemoryRouter>);
    expect(screen.getByRole('img', { name: 'ExpenseGuard' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.getByLabelText('Username')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: /expense receipts/i })).toBeInTheDocument();
    expect(screen.queryByText(/google/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /create account|register|sign up/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /create|register|sign up/i })).not.toBeInTheDocument();
  });
});
