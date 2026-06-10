export function validateEmail(v: string): true | string {
  if (!v.trim()) return 'Email required';
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim()) || 'Invalid email';
}
