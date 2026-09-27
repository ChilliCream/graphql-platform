import type { CSSProperties } from "react";

interface SecurityShieldIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-success)";

/** A checked shield, for "Centralize API security". */
export function SecurityShieldIcon({
  className,
  style,
}: SecurityShieldIconProps) {
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
        d="M40 15l19 7v18c0 15-9 24-19 27-10-3-19-12-19-27V22z"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinejoin="round"
      />
      <path
        d="M30 40l7 7 14-15"
        stroke={COLOR}
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
