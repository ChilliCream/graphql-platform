import type { ComponentPropsWithoutRef } from "react";

/** Small git-branch glyph used in pull-request figures. */
export function BranchGlyph(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox="0 0 16 16" fill="none" aria-hidden="true" {...props}>
      <circle
        cx="4.5"
        cy="3.6"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <circle
        cx="4.5"
        cy="12.4"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <circle
        cx="11.5"
        cy="3.6"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <path d="M4.5 5.1v5.8" stroke="currentColor" strokeWidth={1.1} />
      <path
        d="M11.5 5.1v1.3a3 3 0 0 1-3 3H6"
        stroke="currentColor"
        strokeWidth={1.1}
      />
    </svg>
  );
}
