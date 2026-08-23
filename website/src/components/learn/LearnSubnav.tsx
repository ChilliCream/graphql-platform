"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import type { ProductKey } from "@/src/data/learn/facets";

interface SubnavLink {
  readonly label: string;
  readonly href: string;
  /** Set on the three promoted product links; their active state matches the `product` query param on `/learn/browse` instead of a plain route prefix. */
  readonly product?: ProductKey;
}

const NAV_LINKS: readonly SubnavLink[] = [
  { label: "Hot Chocolate", href: "/learn/browse?product=hot-chocolate", product: "hot-chocolate" },
  { label: "Fusion", href: "/learn/browse?product=fusion", product: "fusion" },
  { label: "Nitro", href: "/learn/browse?product=nitro", product: "nitro" },
  { label: "Articles", href: "/learn/articles" },
  { label: "Browse", href: "/learn/browse" },
];

/**
 * Persistent second navigation bar for every /learn route (learn-editorial.md
 * section 13): the Learn wordmark, promoted topic links, Articles, Browse,
 * and a right-aligned Subscribe link. Sticky under the global header;
 * rendered once by the learn segment layout.
 */
export function LearnSubnav() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const activeProduct = searchParams.get("product");

  const isActive = (link: SubnavLink): boolean =>
    link.product
      ? pathname === "/learn/browse" && activeProduct === link.product
      : pathname === link.href || pathname.startsWith(`${link.href}/`);

  return (
    <nav
      aria-label="Learn"
      className="border-cc-card-border bg-cc-card-bg sticky top-18 z-30 border-b backdrop-blur-[18px] backdrop-saturate-150"
    >
      <div className="max-w-8xl mx-auto grid h-12 grid-cols-[auto_1fr_auto] items-stretch gap-6 px-5 sm:px-12">
        <Link
          href="/learn"
          className="font-heading text-cc-heading flex shrink-0 items-center font-semibold no-underline"
        >
          Learn
        </Link>
        <div className="flex [scrollbar-width:none]! items-stretch gap-6 overflow-x-auto [&::-webkit-scrollbar]:hidden!">
          {NAV_LINKS.map((link) => {
            const active = isActive(link);
            return (
              <Link
                key={link.label}
                href={link.href}
                aria-current={active ? "page" : undefined}
                className={`relative flex shrink-0 items-center text-sm whitespace-nowrap no-underline transition-colors ${
                  active ? "text-cc-heading" : "text-cc-ink-dim hover:text-cc-heading"
                }`}
              >
                {link.label}
                {active && <span aria-hidden="true" className="bg-cc-accent absolute inset-x-0 bottom-0 h-0.5" />}
              </Link>
            );
          })}
        </div>
        <Link
          href="/learn#subscribe"
          className="text-cc-accent hover:text-cc-accent-hover flex shrink-0 items-center text-sm font-medium no-underline"
        >
          Subscribe
        </Link>
      </div>
    </nav>
  );
}
