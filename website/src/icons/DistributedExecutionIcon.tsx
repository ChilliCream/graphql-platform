import { useId } from "react";
import type { CSSProperties } from "react";

interface DistributedExecutionIconProps {
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

/** One request branching across services and merging into one response. */
export function DistributedExecutionIcon({
  className,
  style,
}: DistributedExecutionIconProps) {
  const uid = useId();
  const tile = `distributed-execution-tile-${uid}`;
  const border = `distributed-execution-border-${uid}`;
  const glow = `distributed-execution-glow-${uid}`;
  const inner = `distributed-execution-inner-${uid}`;
  const stroke = `distributed-execution-stroke-${uid}`;

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
          x1="14"
          y1="26"
          x2="66"
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

      <g
        stroke={`url(#${stroke})`}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M40 21v8m0-3-3-3m3 3 3-3M40 29c0 8-18 5-18 13m18-13v13m0-13c0 8 18 5 18 13M22 50c0 9 18 5 18 12m0-12v12m18-12c0 9-18 5-18 12m0 0-3-3m3 3 3-3" />
        <rect x="31" y="12" width="18" height="9" rx="3" fill={TILE_FROM} />
        <rect x="31" y="62" width="18" height="9" rx="3" fill={TILE_FROM} />
        <circle cx="22" cy="46" r="4" fill={TILE_FROM} />
        <circle cx="40" cy="46" r="4" fill={TILE_FROM} />
        <circle cx="58" cy="46" r="4" fill={TILE_FROM} />
      </g>
    </svg>
  );
}
