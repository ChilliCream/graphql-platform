import Link from "next/link";
import { HUBS, hubHref } from "@/src/data/learn/hubs";

export interface SubnavLink {
  readonly label: string;
  readonly href: string;
}

/**
 * The four topic hubs (website-kbx.3, per the 2026-08-23 user ruling)
 * replace the old Hot Chocolate / Fusion / Nitro product links as the
 * promoted subnav set; products remain a `/learn/browse` facet, reachable
 * through Browse and each row's topic kicker, not a subnav link.
 */
export const NAV_LINKS: readonly SubnavLink[] = [
  ...HUBS.map((hub) => ({ label: hub.label, href: hubHref(hub.key) })),
  { label: "Articles", href: "/learn/articles" },
  { label: "Browse", href: "/learn/browse" },
];

interface LearnSubnavLinkListProps {
  readonly activeHref?: string;
}

/** Renders the `NAV_LINKS` as tab-style links. `activeHref` names the single link (by `href`) that carries the active state; omit it to render all links idle. */
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
