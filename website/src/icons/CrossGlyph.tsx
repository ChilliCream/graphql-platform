import type { ComponentPropsWithoutRef } from "react";

/** Cross mark used in failed-status badges and check lists. */
export function CrossGlyph(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox="0 0 16 16" fill="none" aria-hidden="true" {...props}>
      <path d="M4 4 L12 12 M12 4 L4 12" stroke="currentColor" strokeWidth={2} strokeLinecap="round" />
    </svg>
  );
}
