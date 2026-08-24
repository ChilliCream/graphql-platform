import type { ComponentPropsWithoutRef } from "react";

/** Hexagonal node mark representing a schema registry entry. */
export function RegistryNodeIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox="0 0 16 16" width="13" height="13" aria-hidden="true" {...props}>
      <path
        fill="currentColor"
        d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
      />
      <circle cx="8" cy="8" r="1.7" fill="currentColor" />
    </svg>
  );
}
