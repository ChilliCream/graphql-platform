import type { CSSProperties } from "react";

interface FrenchPressProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * French press brewer, drawn as monoline art so it inherits `currentColor` and
 * scales crisply. Decorative by default.
 */
export function FrenchPress({ className, style }: FrenchPressProps) {
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
      {/* liquid */}
      <path
        d="M31 72 L65 72 L65 86 Q65 91 60 91 L36 91 Q31 91 31 86 Z"
        fill="currentColor"
        fillOpacity={0.22}
        stroke="none"
      />
      {/* knob */}
      <rect x="42" y="6" width="12" height="6" rx="2" />
      {/* plunger bar */}
      <rect x="26" y="15" width="44" height="6" rx="3" />
      {/* rod */}
      <path d="M48 21 L48 30" />
      {/* lid (overhangs the carafe) */}
      <rect x="22" y="30" width="52" height="9" rx="4" />
      {/* carafe body */}
      <rect x="29" y="40" width="38" height="53" rx="8" />
      {/* handle */}
      <path d="M67 58 C79 60 79 78 67 80" />
      {/* plunger mesh level */}
      <path d="M34 53 L62 53" strokeDasharray="3 4.5" />
    </svg>
  );
}
