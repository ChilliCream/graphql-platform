import Link from "next/link";
import type { ProductKey } from "@/src/data/learn/facets";

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

interface LearnSubnavLinkListProps {
  readonly activeHref?: string;
}

/** Renders the five `NAV_LINKS` as tab-style links. `activeHref` names the single link (by `href`) that carries the active state; omit it to render all links idle. */
export function LearnSubnavLinkList({ activeHref }: LearnSubnavLinkListProps) {
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
