import type { CSSProperties } from "react";

interface ChemexProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Chemex carafe with its collar, drawn as monoline art so it inherits
 * `currentColor` and scales crisply. Decorative by default.
 */
export function Chemex({ className, style }: ChemexProps) {
  return (
    <svg
      viewBox="0 0 96 104"
      fill="none"
      stroke="currentColor"
      strokeWidth={3}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
      style={style}
    >
      {/* flask liquid */}
      <path
        d="M25 84 Q48 92 71 84 C70 97 60 100 48 100 C36 100 26 97 25 84 Z"
        fill="currentColor"
        fillOpacity={0.22}
        stroke="none"
      />
      {/* pour spout + flared rim */}
      <path d="M29 17 Q33 12 38 18" />
      <path d="M29 17 Q48 25 67 19" />
      {/* neck cone */}
      <path d="M29 17 L41 55" />
      <path d="M67 19 L55 55" />
      {/* collar with bow detail */}
      <rect x="37" y="54" width="22" height="12" rx="2" />
      <path d="M44 57 L48 60 L44 63" />
      <path d="M52 57 L48 60 L52 63" />
      {/* flask body */}
      <path d="M41 66 C16 76 18 100 48 100 C78 100 80 76 55 66" />
    </svg>
  );
}
