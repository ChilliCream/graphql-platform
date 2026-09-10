import type { ComponentPropsWithoutRef } from "react";

/** Ruled document mark used for lint and schema check output. */
export function LintMark(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg
      viewBox="0 0 16 16"
      width="13"
      height="13"
      aria-hidden="true"
      {...props}
    >
      <rect
        x="2.5"
        y="2"
        width="11"
        height="12"
        rx="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.1"
      />
      <path
        d="M5 6h6M5 8.5h6M5 11h3.5"
        stroke="currentColor"
        strokeWidth="1.1"
        strokeLinecap="round"
      />
    </svg>
  );
}
