"use client";

import {
  useEffect,
  useRef,
  useState,
  type ComponentPropsWithoutRef,
} from "react";
import { BrokenMedia } from "./BrokenMedia";

export function Image({
  alt,
  className = "",
  onError,
  ...props
}: ComponentPropsWithoutRef<"img">) {
  const [broken, setBroken] = useState(false);
  const ref = useRef<HTMLImageElement>(null);

  useEffect(() => {
    // On a statically exported page the <img> can fire its error event before
    // React hydrates and attaches onError, so the event is missed. Detect an
    // image that already finished loading unsuccessfully on mount.
    const img = ref.current;
    if (img && img.complete && img.naturalWidth === 0) {
      setBroken(true);
    }
  }, []);

  if (broken) {
    return <BrokenMedia message="This image couldn't be loaded." />;
  }

  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      ref={ref}
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
