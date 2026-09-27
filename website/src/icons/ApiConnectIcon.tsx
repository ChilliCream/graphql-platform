import { useId } from "react";
import type { CSSProperties } from "react";

interface ApiConnectIconProps {
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
const FILL_SOFT = "color-mix(in srgb, var(--color-cc-accent) 22%, transparent)";

/** Three services converging into one connector, for "Connect every API". */
export function ApiConnectIcon({ className, style }: ApiConnectIconProps) {
  const uid = useId();
  const tile = `api-connect-tile-${uid}`;
  const border = `api-connect-border-${uid}`;
  const glow = `api-connect-glow-${uid}`;
  const inner = `api-connect-inner-${uid}`;
  const stroke = `api-connect-stroke-${uid}`;

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

      <path
        d="M24 30q6 10 14 18M40 22v26M56 30q-6 10-14 18"
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle
        cx="24"
        cy="26"
        r="6"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
      />
      <circle
        cx="40"
        cy="20"
        r="6"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
      />
      <circle
        cx="56"
        cy="26"
        r="6"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
      />
      <rect
        x="30"
        y="48"
        width="20"
        height="14"
        rx="4"
        fill={FILL_SOFT}
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
      />
      <path
        d="M35 62v6M45 62v6"
        stroke={`url(#${stroke})`}
        strokeWidth="2.25"
        strokeLinecap="round"
      />
      <circle cx="40" cy="55" r="1.6" fill={STROKE_BRIGHT} />
    </svg>
  );
}
