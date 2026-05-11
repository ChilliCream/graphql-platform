import type { ComponentPropsWithoutRef } from "react";

export function Quote({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"blockquote">) {
  return (
    <blockquote
      className={`my-6 border-l-4 border-slate-300 px-4 py-1 italic text-slate-700 ${className}`.trim()}
      {...props}
    />
  );
}
