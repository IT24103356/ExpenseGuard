import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { setAuthResolver } from '../services/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => ({
    token: localStorage.getItem('jwt_token'),
    employeeId: localStorage.getItem('employee_id'),
  }));

  useEffect(() => {
    setAuthResolver(() => session);
  }, [session]);

  const value = useMemo(() => ({
    ...session,
    setSession(next) {
      if (next.token) localStorage.setItem('jwt_token', next.token);
      else localStorage.removeItem('jwt_token');
      if (next.employeeId) localStorage.setItem('employee_id', String(next.employeeId));
      else localStorage.removeItem('employee_id');
      setSession({ token: next.token || null, employeeId: next.employeeId || null });
    },
    clearSession() {
      localStorage.removeItem('jwt_token');
      localStorage.removeItem('employee_id');
      setSession({ token: null, employeeId: null });
    },
  }), [session]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider');
  return context;
}
