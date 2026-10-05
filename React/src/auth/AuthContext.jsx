import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { loginRequest, setUnauthorizedHandler } from '../services/api';

const STORAGE_KEY = 'expenseguard.session';
const AuthContext = createContext(null);

function readSession() {
  try {
    const session = JSON.parse(localStorage.getItem(STORAGE_KEY));
    if (!session?.token || !session?.expiresAt || Date.parse(session.expiresAt) <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return session;
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(undefined);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setSession(null);
  }, []);

  useEffect(() => {
    setSession(readSession());
    setUnauthorizedHandler(logout);
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  const login = useCallback(async (username, password) => {
    const { data } = await loginRequest({ username: username.trim(), password });
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    setSession(data);
    return data;
  }, []);

  const value = useMemo(() => ({
    session,
    isBootstrapping: session === undefined,
    isAuthenticated: Boolean(session),
    login,
    logout,
    hasRole: (...roles) => Boolean(session && roles.includes(session.role)),
    hasAnyRole: (...roles) => Boolean(session && roles.includes(session.role)),
  }), [session, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth must be used inside AuthProvider');
  return value;
}

export function getStoredToken() {
  return readSession()?.token ?? null;
}
