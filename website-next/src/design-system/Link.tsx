import type { ComponentPropsWithoutRef } from "react";
import NextLink from "next/link";

export function Link({
  href = "",
  children,
  className = "",
  ...props
}: ComponentPropsWithoutRef<"a">) {
  const styles =
    "text-fuchsia-700 underline decoration-fuchsia-300 decoration-2 underline-offset-2 hover:decoration-fuchsia-700";
  const merged = `${styles} ${className}`.trim();

  if (href.startsWith("/")) {
    return (
      <NextLink href={href} className={merged} {...props}>
        {children}
      </NextLink>
    );
  }

  if (href.startsWith("#")) {
    return (
      <a href={href} className={merged} {...props}>
        {children}
      </a>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={merged}
      {...props}
    >
      {children}
    </a>
  );
}
