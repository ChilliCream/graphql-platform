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
      className={`bg-cc-ink-faint text-cc-prose ring-cc-card-border rounded px-1.5 py-0.5 font-mono text-[0.875em] ring-1 ${className}`.trim()}
      {...props}
    />
  );
}
