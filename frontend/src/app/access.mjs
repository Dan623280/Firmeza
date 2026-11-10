export const roles = ['Administrador', 'Cliente'];
export function validSession(auth, now = Date.now()) {
 return !!auth && typeof auth.token === 'string' && auth.token.length > 0 && roles.includes(auth.role) && new Date(auth.expiresAt).getTime() > now;
}
export function homeFor(role) { return role === 'Administrador' ? '/admin' : '/'; }
export function roleDestination(auth, requiredRole, now = Date.now()) {
 if (!validSession(auth, now)) return '/ingresar';
 return auth.role === requiredRole ? null : homeFor(auth.role);
}
