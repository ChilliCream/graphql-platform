import type { ComponentPropsWithoutRef } from "react";

export function InlineCode({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"code">) {
  const isBlock = className.startsWith("language-");
  if (isBlock) {
    return <code className={className} {...props} />;
  }
  return (
    <code
      className={`rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[0.875em] text-slate-800 ring-1 ring-slate-200 ${className}`.trim()}
      {...props}
    />
  );
}
