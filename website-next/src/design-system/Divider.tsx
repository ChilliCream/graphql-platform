import type { ComponentPropsWithoutRef } from "react";

export function Divider({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"hr">) {
  return (
    <hr
      className={`my-8 h-1 border-0 bg-gradient-to-r from-fuchsia-500 via-amber-400 to-emerald-500 ${className}`.trim()}
      {...props}
    />
  );
}
