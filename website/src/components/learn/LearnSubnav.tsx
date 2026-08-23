import { Suspense } from "react";
import Link from "next/link";
import type { ProductKey } from "@/src/data/learn/facets";
import { LearnSubnavLinks } from "@/src/components/learn/LearnSubnavLinks";

export interface SubnavLink {
  readonly label: string;
  readonly href: string;
  /** Set on the three promoted product links; their active state matches the `product` query param on `/learn/browse` instead of a plain route prefix. */
  readonly product?: ProductKey;
}

export const NAV_LINKS: readonly SubnavLink[] = [
  { label: "Hot Chocolate", href: "/learn/browse?product=hot-chocolate", product: "hot-chocolate" },
  { label: "Fusion", href: "/learn/browse?product=fusion", product: "fusion" },
  { label: "Nitro", href: "/learn/browse?product=nitro", product: "nitro" },
  { label: "Articles", href: "/learn/articles" },
  { label: "Browse", href: "/learn/browse" },
];

/** Renders the five `NAV_LINKS` as tab-style links. `activeHref` names the single link (by `href`) that carries the active state; omit it to render all links idle. */
export function LearnSubnavLinkList({ activeHref }: { readonly activeHref?: string }) {
  return (
    <>
      {NAV_LINKS.map((link) => {
        const active = link.href === activeHref;
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
    </>
  );
}

/**
 * Persistent second navigation bar for every /learn route (learn-editorial.md
 * section 13): the Learn wordmark, promoted topic links, Articles, Browse,
 * and a right-aligned Subscribe link. Sticky under the global header;
 * rendered once by the learn segment layout.
 *
 * Server-rendered shell: only the active-link computation (`LearnSubnavLinks`)
 * needs `useSearchParams`, so it is the sole client boundary. Its Suspense
 * fallback renders the same five links with no active state, so the exported
 * static HTML always contains the wordmark, all links, and Subscribe.
 */
export function LearnSubnav() {
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
          <Suspense fallback={<LearnSubnavLinkList />}>
            <LearnSubnavLinks />
          </Suspense>
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
