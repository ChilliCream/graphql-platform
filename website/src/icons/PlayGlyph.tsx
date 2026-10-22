import type { ComponentPropsWithoutRef } from "react";

/** Solid play triangle used on video posters. */
export function PlayGlyph(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" {...props}>
      <path d="M8 5v14l11-7z" />
    </svg>
  );
}
