import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { FiMoon, FiSun } from 'react-icons/fi';
import { useAuth } from '../auth/AuthContext';
import { canAccess, homePath } from '../auth/access';
import { useTheme } from '../auth/ThemeContext';
import Logo from '../components/Logo';
import { apiErrorMessage } from '../components/Shared';

export default function Login() {
  const { login, isAuthenticated, session } = useAuth();
  const { isDark, toggleTheme } = useTheme();
  const [form, setForm] = useState({ username: '', password: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const fallback = homePath(session?.role);

  if (isAuthenticated) return <Navigate to={fallback} replace />;

  const submit = async event => {
    event.preventDefault();
    if (!form.username.trim() || !form.password) {
      setError('Username and password are required.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      const next = await login(form.username, form.password);
      const requested = location.state?.from;
      navigate(requested && canAccess(next.role, requested) ? requested : homePath(next.role), { replace: true });
    } catch (err) {
      setError(apiErrorMessage(err, 'Sign in failed. Try again.'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="login-page">
      <section className="login-shell">
        <div className="login-form-pane">
          <header className="login-brand-row">
            <div className="login-brand">
              <Logo />
              <strong>ExpenseGuard</strong>
            </div>
            <button className="btn btn-ghost btn-sm" type="button" onClick={toggleTheme}
              aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}>
              {isDark ? <FiSun /> : <FiMoon />}
            </button>
          </header>
          <div className="login-form-body">
            <h1>Sign in</h1>
            <p>Review receipts, OCR, and claim approvals in one workspace.</p>
            <form className="login-form" onSubmit={submit} noValidate>
              {error && <div className="alert alert-danger" role="alert">{error}</div>}
              <label className="form-group" htmlFor="username">
                <span className="form-label">Username</span>
                <input id="username" className="form-control" autoComplete="username" value={form.username}
                  placeholder="nimal.perera"
                  onChange={e => setForm({ ...form, username: e.target.value })} />
              </label>
              <label className="form-group" htmlFor="password">
                <span className="form-label">Password</span>
                <input id="password" className="form-control" type="password" autoComplete="current-password"
                  value={form.password} placeholder="••••••••"
                  onChange={e => setForm({ ...form, password: e.target.value })} />
              </label>
              <button className="btn btn-primary login-submit" type="submit" disabled={busy}>
                {busy ? 'Signing in…' : 'Sign in'}
              </button>
            </form>
          </div>
          <p className="login-footnote">Authorized employees only.</p>
        </div>
        <aside className="login-hero">
          <img src="/login-hero.jpg" alt="Team reviewing expense receipts at a meeting table" />
          <div className="login-hero-card login-hero-card-top">
            <span>Receipt review</span>
            <small>OCR matched · ready for approval</small>
          </div>
          <div className="login-hero-card login-hero-card-mid">
            <span>Manager queue</span>
            <small>Policy, fraud, and budget checked</small>
          </div>
          <div className="login-hero-card login-hero-card-bottom">
            <span>Finance payout</span>
            <small>After the approval chain</small>
          </div>
        </aside>
      </section>
    </main>
  );
}
