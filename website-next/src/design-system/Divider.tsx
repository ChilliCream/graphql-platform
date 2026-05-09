import type { ComponentPropsWithoutRef } from "react";

export function Divider({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"hr">) {
  return (
    <hr
      className={`my-10 border-0 border-t border-slate-200 ${className}`.trim()}
      {...props}
    />
  );
}
