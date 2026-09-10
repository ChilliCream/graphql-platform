import type { CSSProperties } from "react";

interface DripBrewerProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Drip coffee brewer, drawn as monoline art so it inherits `currentColor` and
 * scales crisply. Decorative by default.
 */
export function DripBrewer({ className, style }: DripBrewerProps) {
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
      <path d="M 36 28 L 164 28 L 164 70 L 132 70 L 132 90 L 68 90 L 68 70 L 36 70 Z" />
      <path d="M 76 90 L 124 90 L 110 132 L 90 132 Z" />
      <path d="M 70 138 L 130 138 L 138 198 L 62 198 Z" />
      <path d="M 130 150 Q 154 150 154 174 Q 154 196 138 196" />
      <line x1="68" y1="172" x2="132" y2="172" opacity={0.4} />
      <line x1="40" y1="200" x2="160" y2="200" />
    </svg>
  );
}
