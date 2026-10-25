import type { CSSProperties } from "react";

interface FrenchPressProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * French press brewer, drawn as monoline art so it inherits `currentColor` and
 * scales crisply. Decorative by default.
 */
export function FrenchPress({ className, style }: FrenchPressProps) {
  return (
    <svg
      viewBox="0 0 200 220"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <line x1="100" y1="20" x2="100" y2="34" />
      <circle cx="100" cy="20" r="4" />
      <rect x="50" y="34" width="100" height="14" rx="3" />
      <path d="M 56 48 L 56 192 L 144 192 L 144 48" />
      <path d="M 144 80 Q 168 80 168 110 Q 168 140 144 140" />
      <line x1="100" y1="48" x2="100" y2="120" />
      <line x1="68" y1="120" x2="132" y2="120" />
      <line x1="60" y1="160" x2="140" y2="160" opacity={0.4} />
    </svg>
  );
}
