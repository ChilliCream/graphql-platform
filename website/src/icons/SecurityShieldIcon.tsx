import { useId } from "react";
import type { CSSProperties } from "react";

interface SecurityShieldIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-success)";
const TILE_FROM =
  "color-mix(in srgb, var(--color-cc-success) 28%, var(--color-cc-surface))";
const TILE_TO =
  "color-mix(in srgb, var(--color-cc-success) 10%, var(--color-cc-surface))";
const BORDER_BRIGHT =
  "color-mix(in srgb, var(--color-cc-success) 80%, var(--color-cc-white))";
const BORDER_DIM =
  "color-mix(in srgb, var(--color-cc-success) 20%, transparent)";
const STROKE_BRIGHT =
  "color-mix(in srgb, var(--color-cc-success) 55%, var(--color-cc-white))";
const FILL_SOFT =
  "color-mix(in srgb, var(--color-cc-success) 22%, transparent)";

/** A checked shield, for "Centralize API security". */
export function SecurityShieldIcon({
  className,
  style,
}: SecurityShieldIconProps) {
  const uid = useId();
  const tile = `security-shield-tile-${uid}`;
  const border = `security-shield-border-${uid}`;
  const glow = `security-shield-glow-${uid}`;
  const inner = `security-shield-inner-${uid}`;
  const stroke = `security-shield-stroke-${uid}`;

  return (
    <svg
      viewBox="0 0 80 80"
      fill="none"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <defs>
        <linearGradient
          id={tile}
          x1="8"
          y1="8"
          x2="72"
          y2="72"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={TILE_FROM} />
          <stop offset="1" stopColor={TILE_TO} />
        </linearGradient>
        <linearGradient
          id={border}
          x1="8"
          y1="8"
          x2="72"
          y2="72"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={BORDER_BRIGHT} />
          <stop offset="1" stopColor={BORDER_DIM} />
        </linearGradient>
        <linearGradient
          id={stroke}
          x1="21"
          y1="15"
          x2="59"
          y2="67"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={STROKE_BRIGHT} />
          <stop offset="1" stopColor={COLOR} />
        </linearGradient>
        <radialGradient id={inner} cx="30%" cy="22%" r="60%">
          <stop offset="0" stopColor={STROKE_BRIGHT} stopOpacity="0.35" />
          <stop offset="1" stopColor={STROKE_BRIGHT} stopOpacity="0" />
        </radialGradient>
        <filter id={glow} x="-60%" y="-60%" width="220%" height="220%">
          <feGaussianBlur stdDeviation="5" />
        </filter>
      </defs>

      <rect
        x="8"
        y="8"
        width="64"
        height="64"
        rx="16"
        fill={COLOR}
        opacity="0.3"
        filter={`url(#${glow})`}
      />
      <rect
        x="4"
        y="4"
        width="72"
        height="72"
        rx="18"
        fill={`url(#${tile})`}
        stroke={`url(#${border})`}
        strokeWidth="1.5"
      />
      <rect
        x="4"
        y="4"
        width="72"
        height="72"
        rx="18"
        fill={`url(#${inner})`}
      />

      <path
        d="M40 15l19 7v18c0 15-9 24-19 27-10-3-19-12-19-27V22z"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinejoin="round"
      />
      <circle cx="40" cy="15" r="1.8" fill={STROKE_BRIGHT} />
      <path
        d="M30 40l7 7 14-15"
        stroke={`url(#${stroke})`}
        strokeWidth="2.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
