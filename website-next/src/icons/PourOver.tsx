import type { CSSProperties } from "react";

interface PourOverProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Pour-over dripper and mug, drawn as monoline art so it inherits
 * `currentColor` and scales crisply. Decorative by default.
 */
export function PourOver({ className, style }: PourOverProps) {
  return (
    <svg
      viewBox="0 0 96 104"
      fill="none"
      stroke="currentColor"
      strokeWidth={3}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
      style={style}
    >
      {/* mug liquid */}
      <path
        d="M32 80 L64 80 L64 86 Q64 92 58 92 L38 92 Q32 92 32 86 Z"
        fill="currentColor"
        fillOpacity={0.22}
        stroke="none"
      />
      {/* funnel rim */}
      <ellipse cx="48" cy="17" rx="27" ry="5" />
      {/* funnel cone */}
      <path d="M21 18 L46 52 L50 52 L75 18" />
      {/* funnel ridges */}
      <path d="M29 27 L67 27" />
      <path d="M33 35 L63 35" />
      <path d="M37 43 L59 43" />
      {/* drip */}
      <path d="M48 52 L48 60" />
      {/* mug body */}
      <path d="M31 60 L65 60 L65 86 Q65 94 57 94 L39 94 Q31 94 31 86 Z" />
      {/* mug handle */}
      <path d="M65 68 C77 70 77 86 65 88" />
    </svg>
  );
}
