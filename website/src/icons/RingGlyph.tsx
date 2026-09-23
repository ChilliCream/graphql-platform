import type { ComponentPropsWithoutRef } from "react";

/** Static ring for a row waiting its turn; unlike SpinnerGlyph it never rotates. */
export function RingGlyph(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox="0 0 16 16" fill="none" aria-hidden="true" {...props}>
      <circle cx="8" cy="8" r="6" stroke="currentColor" strokeWidth={2} />
    </svg>
  );
}
