import type { ComponentPropsWithoutRef } from "react";

/** Arc spinner for a check that is still running. Callers supply the size,
 * color, and the rotation utility. */
export function SpinnerGlyph(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox="0 0 16 16" fill="none" aria-hidden="true" {...props}>
      <circle cx="8" cy="8" r="6" stroke="currentColor" strokeOpacity={0.25} strokeWidth={2} />
      <path d="M8 2 a6 6 0 0 1 6 6" stroke="currentColor" strokeWidth={2} strokeLinecap="round" />
    </svg>
  );
}
