import type { ComponentPropsWithoutRef } from "react";

export function Quote({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"blockquote">) {
  return (
    <blockquote
      className={`my-6 border-l-4 border-cyan-500 bg-cyan-50 px-4 py-2 italic text-cyan-900 ${className}`.trim()}
      {...props}
    />
  );
}
