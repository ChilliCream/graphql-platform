import type { ComponentPropsWithoutRef } from "react";

/** Circled cross marking a blocked or rejected item. */
export function BlockMark(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg
      viewBox="0 0 12 12"
      width="13"
      height="13"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...props}
    >
      <circle cx="6" cy="6" r="4.7" />
      <path d="M4.4 4.4 7.6 7.6M7.6 4.4 4.4 7.6" />
    </svg>
  );
}
