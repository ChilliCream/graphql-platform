import type { ComponentPropsWithoutRef } from "react";

export function CodeBlock({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"pre">) {
  return (
    <pre
      className={`my-6 overflow-x-auto rounded-lg bg-slate-900 p-4 font-mono text-sm leading-6 text-slate-100 shadow-md ${className}`.trim()}
      {...props}
    />
  );
}
