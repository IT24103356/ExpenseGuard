/* oxlint-disable react/only-export-components -- local auth adapter intentionally co-locates its hook */
import { createContext, useContext, useMemo } from 'react';

const AuthContext = createContext(null);

function parseJwt(token) {
  try {
    return JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
  } catch {
    return {};
  }
}

export function AuthProvider({ children, value }) {
  const auth = useMemo(() => {
    if (value) return value;
    const token = localStorage.getItem('jwt_token');
    const claims = token ? parseJwt(token) : {};
    const rawRoles = claims.role
      ?? claims.roles
      ?? claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      ?? [];
    const roles = Array.isArray(rawRoles) ? rawRoles : [rawRoles].filter(Boolean);
    return {
      token,
      isAuthenticated: Boolean(token),
      roles,
      userId: claims.sub ?? claims.nameid ?? null,
      hasAnyRole: (...allowed) => roles.some(role => allowed.includes(role)),
    };
  }, [value]);

  return <AuthContext.Provider value={auth}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const auth = useContext(AuthContext);
  if (!auth) throw new Error('useAuth must be used inside AuthProvider');
  return auth;
}

