import { useId } from "react";
import type { CSSProperties } from "react";

interface StackedLayersIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-info)";
const TILE_FROM =
  "color-mix(in srgb, var(--color-cc-info) 28%, var(--color-cc-surface))";
const TILE_TO =
  "color-mix(in srgb, var(--color-cc-info) 10%, var(--color-cc-surface))";
const BORDER_BRIGHT =
  "color-mix(in srgb, var(--color-cc-info) 80%, var(--color-cc-white))";
const BORDER_DIM = "color-mix(in srgb, var(--color-cc-info) 20%, transparent)";
const STROKE_BRIGHT =
  "color-mix(in srgb, var(--color-cc-info) 55%, var(--color-cc-white))";
const FILL_SOFT = "color-mix(in srgb, var(--color-cc-info) 22%, transparent)";

/** Three stacked slabs tied by a side connector, for "Keep your existing stack". */
export function StackedLayersIcon({
  className,
  style,
}: StackedLayersIconProps) {
  const uid = useId();
  const tile = `stacked-layers-tile-${uid}`;
  const border = `stacked-layers-border-${uid}`;
  const glow = `stacked-layers-glow-${uid}`;
  const inner = `stacked-layers-inner-${uid}`;
  const stroke = `stacked-layers-stroke-${uid}`;

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
          x1="18"
          y1="18"
          x2="62"
          y2="62"
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

      <rect
        x="18"
        y="18"
        width="44"
        height="13"
        rx="4"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinejoin="round"
      />
      <rect
        x="18"
        y="33.5"
        width="44"
        height="13"
        rx="4"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinejoin="round"
      />
      <rect
        x="18"
        y="49"
        width="44"
        height="13"
        rx="4"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinejoin="round"
      />
      <path
        d="M23 24.5v31"
        stroke={STROKE_BRIGHT}
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeOpacity="0.55"
      />
      <circle cx="23" cy="24.5" r="1.8" fill={STROKE_BRIGHT} />
      <circle cx="23" cy="55.5" r="1.8" fill={STROKE_BRIGHT} />
    </svg>
  );
}
