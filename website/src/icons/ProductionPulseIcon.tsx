import { useId } from "react";
import type { CSSProperties } from "react";

interface ProductionPulseIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-cta)";
const TILE_FROM =
  "color-mix(in srgb, var(--color-cc-cta) 28%, var(--color-cc-surface))";
const TILE_TO =
  "color-mix(in srgb, var(--color-cc-cta) 10%, var(--color-cc-surface))";
const BORDER_BRIGHT =
  "color-mix(in srgb, var(--color-cc-cta) 80%, var(--color-cc-white))";
const BORDER_DIM = "color-mix(in srgb, var(--color-cc-cta) 20%, transparent)";
const STROKE_BRIGHT =
  "color-mix(in srgb, var(--color-cc-cta) 55%, var(--color-cc-white))";
const FILL_SOFT = "color-mix(in srgb, var(--color-cc-cta) 22%, transparent)";

/** A pulse line clear of the bars, for "See what happens in production". */
export function ProductionPulseIcon({
  className,
  style,
}: ProductionPulseIconProps) {
  const uid = useId();
  const tile = `production-pulse-tile-${uid}`;
  const border = `production-pulse-border-${uid}`;
  const glow = `production-pulse-glow-${uid}`;
  const inner = `production-pulse-inner-${uid}`;
  const barStroke = `production-pulse-bar-stroke-${uid}`;
  const lineStroke = `production-pulse-line-stroke-${uid}`;

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
          id={barStroke}
          x1="18"
          y1="34"
          x2="62"
          y2="62"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={STROKE_BRIGHT} />
          <stop offset="1" stopColor={COLOR} />
        </linearGradient>
        <linearGradient
          id={lineStroke}
          x1="14"
          y1="16"
          x2="66"
          y2="30"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={STROKE_BRIGHT} />
          <stop offset="1" stopColor={COLOR} />
        </linearGradient>
        <radialGradient id={inner} cx="30%" cy="20%" r="60%">
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

      <rect
        x="18"
        y="46"
        width="10"
        height="16"
        rx="2.5"
        fill={FILL_SOFT}
        stroke={`url(#${barStroke})`}
        strokeWidth="2"
      />
      <rect
        x="35"
        y="34"
        width="10"
        height="28"
        rx="2.5"
        fill={FILL_SOFT}
        stroke={`url(#${barStroke})`}
        strokeWidth="2"
      />
      <rect
        x="52"
        y="41"
        width="10"
        height="21"
        rx="2.5"
        fill={FILL_SOFT}
        stroke={`url(#${barStroke})`}
        strokeWidth="2"
      />
      <path
        d="M14 26L20 26L24 18L28 30L32 20L37 26L42 26L46 22L50 28L54 24L66 24"
        stroke={`url(#${lineStroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="24" cy="18" r="2" fill={STROKE_BRIGHT} />
    </svg>
  );
}
