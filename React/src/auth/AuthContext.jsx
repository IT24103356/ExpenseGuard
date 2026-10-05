import { createContext, useMemo } from 'react';

const AuthContext = createContext({ token: null, role: null });

export function AuthProvider({ children, value }) {
  const auth = useMemo(() => value ?? {
    token: localStorage.getItem('jwt_token'),
    role: localStorage.getItem('user_role'),
  }, [value]);

  return <AuthContext.Provider value={auth}>{children}</AuthContext.Provider>;
}

