import type { CSSProperties } from "react";

interface MergingPathsIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-warning)";

/** Two paths running side by side that merge into one, for "Adopt without disruption". */
export function MergingPathsIcon({ className, style }: MergingPathsIconProps) {
  return (
    <svg
      viewBox="0 0 80 80"
      fill="none"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <rect
        x="4"
        y="4"
        width="72"
        height="72"
        rx="18"
        fill={COLOR}
        fillOpacity="0.12"
      />
      <path
        d="M16 28h10c8 0 8 12 16 12M16 52h10c8 0 8-12 16-12"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M42 40h22"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
      />
      <circle cx="16" cy="28" r="3.5" fill={COLOR} />
      <circle cx="16" cy="52" r="3.5" fill={COLOR} />
      <circle cx="64" cy="40" r="3.5" fill={COLOR} />
    </svg>
  );
}
