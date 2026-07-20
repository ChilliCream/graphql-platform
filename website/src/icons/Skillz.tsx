import type { CSSProperties } from "react";

interface SkillzProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Skillz product logo, inlined as SVG so it ships in the HTML and can be sized
 * and positioned with CSS. Decorative by default. Gradient ids are prefixed so
 * several inlined icons on a page don't collide.
 */
export function Skillz({ className, style }: SkillzProps) {
  return (
    <svg
      viewBox="0 0 64 64"
      fill="none"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <defs>
        <linearGradient
          id="skillz-brand"
          x1="12"
          y1="0"
          x2="12"
          y2="24"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#F61D6E" />
          <stop offset="0.5" stopColor="#FB522E" />
          <stop offset="1" stopColor="#FFA52B" />
        </linearGradient>
      </defs>
      <g transform="translate(5.12 5.12) scale(2.24)">
        <path
          d="M12 1.74 L21.72 7.14 L21.72 16.86 L12 22.26 L2.28 16.86 L2.28 7.14 Z"
          fill="#0c1322"
          stroke="url(#skillz-brand)"
          strokeWidth="1.3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M2.28 7.14 L12 12 L21.72 7.14 M12 12 L12 22.26"
          stroke="url(#skillz-brand)"
          strokeWidth="1.3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M12 4.18 L17.35 6.86 L6.65 7.15 L12 9.83"
          stroke="url(#skillz-brand)"
          strokeWidth="1.3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </g>
    </svg>
  );
}
