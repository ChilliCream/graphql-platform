import type { CSSProperties } from "react";

interface CloudGlyphProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/** Cloud, for the BYOC unlock. */
export function CloudGlyph({ className, style }: CloudGlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <path d="M7 18h10a4 4 0 0 0 .5-7.97A5.5 5.5 0 0 0 6.5 9 4 4 0 0 0 7 18z" />
    </svg>
  );
}
