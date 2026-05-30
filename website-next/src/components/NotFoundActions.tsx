"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

/**
 * 404 call-to-action buttons. The secondary link adapts to where the reader
 * hit the dead end: docs pages offer the docs index, blog pages the blog.
 */
export function NotFoundActions() {
  const pathname = usePathname() ?? "";

  let secondary: { href: string; label: string } | null = null;
  if (pathname.startsWith("/docs")) {
    secondary = { href: "/docs", label: "Browse the docs" };
  } else if (pathname.startsWith("/blog")) {
    secondary = { href: "/blog", label: "Browse the blog" };
  }

  return (
    <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
      <Link
        href="/"
        className="inline-flex h-10.5 items-center rounded-md border-2 border-cc-cta bg-cc-cta px-7 text-sm font-medium text-cc-ink no-underline transition-colors hover:bg-cc-cta-hover"
      >
        Take me home
      </Link>
      {secondary ? (
        <Link
          href={secondary.href}
          className="inline-flex h-10.5 items-center rounded-md border border-cc-card-border px-7 text-sm font-medium text-cc-ink-dim no-underline transition-colors hover:border-cc-card-border-hover hover:text-cc-ink"
        >
          {secondary.label}
        </Link>
      ) : null}
    </div>
  );
}
