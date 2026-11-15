import { useId } from "react";
import type { CSSProperties } from "react";

interface SchemaCompositionIconProps {
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

/** Three source schemas joining into one composed schema. */
export function SchemaCompositionIcon({
  className,
  style,
}: SchemaCompositionIconProps) {
  const uid = useId();
  const tile = `schema-composition-tile-${uid}`;
  const border = `schema-composition-border-${uid}`;
  const glow = `schema-composition-glow-${uid}`;
  const inner = `schema-composition-inner-${uid}`;
  const stroke = `schema-composition-stroke-${uid}`;

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
        <path d="M28 22h6c8 0 4 18 12 18h4M28 40h22M28 58h6c8 0 4-18 12-18" />
        <rect x="14" y="14" width="14" height="16" rx="3" fill={TILE_FROM} />
        <rect x="14" y="32" width="14" height="16" rx="3" fill={TILE_FROM} />
        <rect x="14" y="50" width="14" height="16" rx="3" fill={TILE_FROM} />
        <rect x="50" y="26" width="17" height="28" rx="4" fill={TILE_FROM} />
        <path
          d="M19 20h4m-4 4h4m-4 14h4m-4 4h4m-4 14h4m-4 4h4m32-26h7m-7 6h7m-7 6h7"
          stroke={STROKE_BRIGHT}
        />
      </g>
    </svg>
  );
}
