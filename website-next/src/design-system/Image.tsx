import type { ComponentPropsWithoutRef } from "react";

export function Image({
  alt,
  className = "",
  ...props
}: ComponentPropsWithoutRef<"img">) {
  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      alt={alt ?? ""}
      className={`my-6 max-w-full rounded-lg ring-2 ring-amber-300 ${className}`.trim()}
      {...props}
    />
  );
}
