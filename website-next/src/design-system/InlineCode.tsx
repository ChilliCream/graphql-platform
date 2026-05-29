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
      className={`rounded bg-cc-ink-faint px-1.5 py-0.5 font-mono text-[0.875em] text-cc-ink ring-1 ring-cc-card-border ${className}`.trim()}
      {...props}
    />
  );
}
