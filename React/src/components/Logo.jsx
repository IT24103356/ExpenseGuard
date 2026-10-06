import { useId } from 'react';

export default function Logo({ className = 'logo-icon', title = 'ExpenseGuard' }) {
  const gradientId = `eg-logo-${useId().replace(/:/g, '')}`;
  return (
    <svg className={className} viewBox="0 0 40 40" role="img" aria-label={title}>
      <defs>
        <linearGradient id={gradientId} x1="6" y1="4" x2="34" y2="36" gradientUnits="userSpaceOnUse">
          <stop stopColor="#6366f1" />
          <stop offset="1" stopColor="#8b5cf6" />
        </linearGradient>
      </defs>
      <rect width="40" height="40" rx="10" fill={`url(#${gradientId})`} />
      <path fill="#fff" d="M20 7.6c.5 0 5.2 1 8.6 1.6.6.1 1 .6 1 1.2v9.6c0 6.6-4.5 11.2-9.1 13.4-.3.1-.7.1-1 0-4.6-2.2-9.1-6.8-9.1-13.4V10.4c0-.6.4-1.1 1-1.2 3.4-.6 8.1-1.6 8.6-1.6z" />
      <path fill="none" stroke="#6366f1" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" d="M15.2 19.7 18.6 23l6.4-6.8" />
    </svg>
  );
}
