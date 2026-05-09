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
      className={`rounded bg-rose-50 px-1.5 py-0.5 font-mono text-sm text-rose-700 ring-1 ring-rose-200 ${className}`.trim()}
      {...props}
    />
  );
}
