import type { CSSProperties } from "react";

interface ProductionPulseIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-cta)";

/** A pulse line over a bar chart, for "See what happens in production". */
export function ProductionPulseIcon({
  className,
  style,
}: ProductionPulseIconProps) {
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
      <rect
        x="18"
        y="46"
        width="10"
        height="16"
        rx="2.5"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.5"
      />
      <rect
        x="35"
        y="34"
        width="10"
        height="28"
        rx="2.5"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.5"
      />
      <rect
        x="52"
        y="41"
        width="10"
        height="21"
        rx="2.5"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.5"
      />
      <path
        d="M14 40h9l6-14 7 26 6-16h11"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
