import type { CSSProperties } from "react";

interface PerformanceGaugeIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-danger)";

/** A gauge with a needle pinned high, for "Performance by design". */
export function PerformanceGaugeIcon({
  className,
  style,
}: PerformanceGaugeIconProps) {
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
        d="M16 54a24 24 0 0 1 48 0"
        fill={COLOR}
        fillOpacity="0.15"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
      />
      <path
        d="M22 32l4 4M58 32l-4 4M40 18v6"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
      />
      <path
        d="M40 54l12-20"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
      />
      <circle cx="40" cy="54" r="4.5" fill={COLOR} />
    </svg>
  );
}
