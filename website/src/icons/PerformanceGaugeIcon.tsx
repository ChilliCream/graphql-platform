import { useId } from "react";
import type { CSSProperties } from "react";

interface PerformanceGaugeIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/** Orange, mixed from the warning and danger tokens so it reads distinct from both. */
const COLOR =
  "color-mix(in srgb, var(--color-cc-warning) 50%, var(--color-cc-danger))";
const TILE_FROM = `color-mix(in srgb, ${COLOR} 28%, var(--color-cc-surface))`;
const TILE_TO = `color-mix(in srgb, ${COLOR} 10%, var(--color-cc-surface))`;
const BORDER_BRIGHT = `color-mix(in srgb, ${COLOR} 80%, var(--color-cc-white))`;
const BORDER_DIM = `color-mix(in srgb, ${COLOR} 20%, transparent)`;
const STROKE_BRIGHT = `color-mix(in srgb, ${COLOR} 55%, var(--color-cc-white))`;
const FILL_SOFT = `color-mix(in srgb, ${COLOR} 22%, transparent)`;

/** A gauge with a needle pinned high, for "Performance by design". */
export function PerformanceGaugeIcon({
  className,
  style,
}: PerformanceGaugeIconProps) {
  const uid = useId();
  const tile = `performance-gauge-tile-${uid}`;
  const border = `performance-gauge-border-${uid}`;
  const glow = `performance-gauge-glow-${uid}`;
  const inner = `performance-gauge-inner-${uid}`;
  const stroke = `performance-gauge-stroke-${uid}`;

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
          x1="16"
          y1="18"
          x2="58"
          y2="54"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={STROKE_BRIGHT} />
          <stop offset="1" stopColor={COLOR} />
        </linearGradient>
        <radialGradient id={inner} cx="30%" cy="24%" r="60%">
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
        d="M16 54a24 24 0 0 1 48 0"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
      />
      <path
        d="M22 32l4 4M58 32l-4 4M40 18v6"
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
      />
      <path
        d="M40 54l12-20"
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
      />
      <circle
        cx="40"
        cy="54"
        r="4.5"
        fill={COLOR}
        stroke={STROKE_BRIGHT}
        strokeWidth="1.5"
      />
    </svg>
  );
}
