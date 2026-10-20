import type { ComponentPropsWithoutRef } from "react";

/** Two side-by-side panels, for comparison pages. */
export function CompareIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 512 512"
      aria-hidden="true"
      {...props}
    >
      <path d="M96 64C60.7 64 32 92.7 32 128l0 256c0 35.3 28.7 64 64 64l112 0c17.7 0 32-14.3 32-32l0-320c0-17.7-14.3-32-32-32L96 64zM304 64c-17.7 0-32 14.3-32 32l0 320c0 17.7 14.3 32 32 32l112 0c35.3 0 64-28.7 64-64l0-256c0-35.3-28.7-64-64-64L304 64zm16 48l96 0c8.8 0 16 7.2 16 16l0 256c0 8.8-7.2 16-16 16l-96 0 0-288z" />
    </svg>
  );
}
