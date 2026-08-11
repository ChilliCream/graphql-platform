import type { ComponentPropsWithoutRef } from "react";

interface ConnectorArrowGlyphProps extends ComponentPropsWithoutRef<"svg"> {
  readonly dashed?: boolean;
}

/** Horizontal arrow linking two nodes in a schematic. */
export function ConnectorArrowGlyph({
  dashed = false,
  ...props
}: ConnectorArrowGlyphProps) {
  return (
    <svg viewBox="0 0 40 24" fill="none" aria-hidden="true" {...props}>
      <path
        d="M2 12 H32"
        stroke="currentColor"
        strokeWidth={1.5}
        strokeDasharray={dashed ? "3 3" : undefined}
      />
      <path d="M30 7 L38 12 L30 17" stroke="currentColor" strokeWidth={1.5} />
    </svg>
  );
}
