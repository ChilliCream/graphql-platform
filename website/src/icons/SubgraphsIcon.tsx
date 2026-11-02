import { useId } from "react";
import type { CSSProperties } from "react";

interface SubgraphsIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-accent)";
const TILE_FROM =
  "color-mix(in srgb, var(--color-cc-accent) 28%, var(--color-cc-surface))";
const TILE_TO =
  "color-mix(in srgb, var(--color-cc-accent) 10%, var(--color-cc-surface))";
const BORDER_BRIGHT =
  "color-mix(in srgb, var(--color-cc-accent) 80%, var(--color-cc-white))";
const BORDER_DIM =
  "color-mix(in srgb, var(--color-cc-accent) 20%, transparent)";
const STROKE_BRIGHT =
  "color-mix(in srgb, var(--color-cc-accent) 55%, var(--color-cc-white))";

/** Three separate clusters of connected service nodes. */
export function SubgraphsIcon({ className, style }: SubgraphsIconProps) {
  const uid = useId();
  const tile = `subgraphs-tile-${uid}`;
  const border = `subgraphs-border-${uid}`;
  const glow = `subgraphs-glow-${uid}`;
  const inner = `subgraphs-inner-${uid}`;
  const stroke = `subgraphs-stroke-${uid}`;

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

      <g stroke={`url(#${stroke})`} strokeWidth="2" strokeLinejoin="round">
        <path d="m18 22 14 3-8 13Zm30 0 14 3-8 13ZM30 50l16 1-7 13Z" />
      </g>
      <g fill={STROKE_BRIGHT}>
        <circle cx="18" cy="22" r="3" />
        <circle cx="32" cy="25" r="3" />
        <circle cx="24" cy="38" r="3" />
        <circle cx="48" cy="22" r="3" />
        <circle cx="62" cy="25" r="3" />
        <circle cx="54" cy="38" r="3" />
        <circle cx="30" cy="50" r="3" />
        <circle cx="46" cy="51" r="3" />
        <circle cx="39" cy="64" r="3" />
      </g>
    </svg>
  );
}
