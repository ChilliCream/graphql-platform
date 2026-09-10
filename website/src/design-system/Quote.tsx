import type { ComponentPropsWithoutRef } from "react";

export function Quote({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"blockquote">) {
  return (
    <blockquote
      className={`border-cc-card-border text-cc-prose my-6 border-l-4 px-4 py-1 italic ${className}`.trim()}
      {...props}
    />
  );
}
