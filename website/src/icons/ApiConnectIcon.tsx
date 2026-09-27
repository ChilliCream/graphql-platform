import type { CSSProperties } from "react";

interface ApiConnectIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-accent)";

/** Three services converging into one connector, for "Connect every API". */
export function ApiConnectIcon({ className, style }: ApiConnectIconProps) {
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
        d="M24 30q6 10 14 18M40 22v26M56 30q-6 10-14 18"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle
        cx="24"
        cy="26"
        r="6"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
      />
      <circle
        cx="40"
        cy="20"
        r="6"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
      />
      <circle
        cx="56"
        cy="26"
        r="6"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
      />
      <rect
        x="30"
        y="48"
        width="20"
        height="14"
        rx="4"
        fill={COLOR}
        fillOpacity="0.2"
        stroke={COLOR}
        strokeWidth="2.75"
      />
      <path
        d="M35 62v6M45 62v6"
        stroke={COLOR}
        strokeWidth="2.75"
        strokeLinecap="round"
      />
    </svg>
  );
}
