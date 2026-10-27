import { useId } from "react";
import type { CSSProperties } from "react";

interface MergingPathsIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-warning)";
const TILE_FROM =
  "color-mix(in srgb, var(--color-cc-warning) 28%, var(--color-cc-surface))";
const TILE_TO =
  "color-mix(in srgb, var(--color-cc-warning) 10%, var(--color-cc-surface))";
const BORDER_BRIGHT =
  "color-mix(in srgb, var(--color-cc-warning) 80%, var(--color-cc-white))";
const BORDER_DIM =
  "color-mix(in srgb, var(--color-cc-warning) 20%, transparent)";
const STROKE_BRIGHT =
  "color-mix(in srgb, var(--color-cc-warning) 55%, var(--color-cc-white))";

/** Two paths running side by side that merge into one, for "Adopt without disruption". */
export function MergingPathsIcon({ className, style }: MergingPathsIconProps) {
  const uid = useId();
  const tile = `merging-paths-tile-${uid}`;
  const border = `merging-paths-border-${uid}`;
  const glow = `merging-paths-glow-${uid}`;
  const inner = `merging-paths-inner-${uid}`;
  const stroke = `merging-paths-stroke-${uid}`;

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

      <path
        d="M16 28h10c8 0 8 12 16 12M16 52h10c8 0 8-12 16-12"
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M42 40h22"
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
      />
      <circle cx="16" cy="28" r="3.25" fill={STROKE_BRIGHT} />
      <circle cx="16" cy="52" r="3.25" fill={STROKE_BRIGHT} />
      <circle
        cx="64"
        cy="40"
        r="4"
        fill={COLOR}
        stroke={STROKE_BRIGHT}
        strokeWidth="1.5"
      />
    </svg>
  );
}
