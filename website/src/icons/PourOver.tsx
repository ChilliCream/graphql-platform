import type { CSSProperties } from "react";

interface PourOverProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Pour-over brewer, drawn as monoline art so it inherits `currentColor` and
 * scales crisply. Decorative by default.
 */
export function PourOver({ className, style }: PourOverProps) {
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
      <path d="M 56 28 L 144 28 L 116 100 L 84 100 Z" />
      <line x1="76" y1="108" x2="124" y2="108" />
      <line x1="76" y1="118" x2="124" y2="118" />
      <path d="M 84 118 L 60 196 Q 60 204 70 204 L 130 204 Q 140 204 140 196 L 116 118" />
      <line x1="66" y1="178" x2="134" y2="178" opacity={0.4} />
      <path d="M 56 28 Q 64 22 72 28" />
    </svg>
  );
}
