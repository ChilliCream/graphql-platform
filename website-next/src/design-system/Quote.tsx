import type { ComponentPropsWithoutRef } from "react";

export function Quote({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"blockquote">) {
  return (
    <blockquote
      className={`my-6 border-l-4 border-cc-card-border px-4 py-1 italic text-cc-ink-dim ${className}`.trim()}
      {...props}
    />
  );
}
