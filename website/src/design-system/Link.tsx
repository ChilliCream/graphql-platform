import type { ComponentPropsWithoutRef } from "react";
import NextLink from "next/link";

export function Link({
  href = "",
  children,
  className = "",
  ...props
}: ComponentPropsWithoutRef<"a">) {
  const styles =
    "text-cc-accent underline decoration-cc-accent/30 underline-offset-2 hover:decoration-cc-accent";
  const merged = `${styles} ${className}`.trim();

  if (href.startsWith("/")) {
    return (
      <NextLink href={href} className={merged} {...props}>
        {children}
      </NextLink>
    );
  }

  if (
    href.startsWith("#") ||
    href.startsWith("mailto:") ||
    href.startsWith("tel:")
  ) {
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
