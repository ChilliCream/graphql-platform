"use client";

import { useState, type ComponentPropsWithoutRef } from "react";
import { BrokenMedia } from "./BrokenMedia";

export function Image({
  alt,
  className = "",
  onError,
  ...props
}: ComponentPropsWithoutRef<"img">) {
  const [broken, setBroken] = useState(false);

  if (broken) {
    return <BrokenMedia message="This image couldn't be loaded." />;
  }

  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      alt={alt ?? ""}
      className={`my-6 max-w-full rounded-md ring-1 ring-cc-card-border ${className}`.trim()}
      {...props}
      onError={(event) => {
        onError?.(event);
        setBroken(true);
      }}
    />
  );
}
