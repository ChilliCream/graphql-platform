import type { CSSProperties } from "react";

interface MokaPotProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Moka pot brewer, drawn as monoline art so it inherits `currentColor` and
 * scales crisply. Decorative by default.
 */
export function MokaPot({ className, style }: MokaPotProps) {
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
      <circle cx="100" cy="26" r="4" />
      <line x1="100" y1="30" x2="100" y2="36" />
      <path d="M 76 44 Q 100 28 124 44" />
      <path d="M 76 44 L 86 108 L 114 108 L 124 44" />
      <path d="M 76 46 L 62 58 L 80 68" />
      <path d="M 122 58 Q 146 62 143 84 Q 140 102 115 100" />
      <line x1="80" y1="112" x2="120" y2="112" />
      <path d="M 84 116 L 64 194 Q 64 202 74 202 L 126 202 Q 136 202 136 194 L 116 116 Z" />
      <line x1="92" y1="116" x2="82" y2="198" opacity={0.4} />
      <line x1="108" y1="116" x2="118" y2="198" opacity={0.4} />
    </svg>
  );
}
