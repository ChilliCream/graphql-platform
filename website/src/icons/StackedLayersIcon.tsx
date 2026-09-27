import type { CSSProperties } from "react";

interface StackedLayersIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-info)";

/** Three stacked slabs, for "Keep your existing stack". */
export function StackedLayersIcon({
  className,
  style,
}: StackedLayersIconProps) {
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
        y="18"
        width="44"
        height="13"
        rx="4"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinejoin="round"
      />
      <rect
        x="18"
        y="33.5"
        width="44"
        height="13"
        rx="4"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinejoin="round"
      />
      <rect
        x="18"
        y="49"
        width="44"
        height="13"
        rx="4"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinejoin="round"
      />
    </svg>
  );
}
