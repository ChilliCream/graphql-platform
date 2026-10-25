import type { ComponentPropsWithoutRef } from "react";

export function Divider({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"hr">) {
  return (
    <hr
      className={`border-cc-card-border my-10 border-0 border-t ${className}`.trim()}
      {...props}
    />
  );
}
