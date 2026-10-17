import type { ComponentPropsWithoutRef } from "react";

export function QuestionCircleIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 16 16"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...props}
    >
      <circle cx="8" cy="8" r="6.25" />
      <path d="M6.4 6.2a1.7 1.7 0 0 1 3.3.5c0 1.1-1.7 1.5-1.7 2.5" />
      <path d="M8 11.4h.01" />
    </svg>
  );
}
