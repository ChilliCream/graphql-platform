"use client";

import { usePathname, useSearchParams } from "next/navigation";
import { NAV_LINKS, LearnSubnavLinkList } from "@/src/components/learn/LearnSubnavLinkList";

/**
 * Client-only piece of `LearnSubnav`: reads the pathname and `product` query
 * param to find which single link is active, then renders the shared link
 * list with that link highlighted.
 */
export function LearnSubnavLinks() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const activeProduct = searchParams.get("product");
  const productIsPromoted = NAV_LINKS.some((link) => link.product === activeProduct);

  const isActive = (link: (typeof NAV_LINKS)[number]): boolean =>
    link.product
      ? pathname === "/learn/browse" && activeProduct === link.product
      : link.href === "/learn/browse"
        ? pathname === "/learn/browse" && !productIsPromoted
        : pathname === link.href || pathname.startsWith(`${link.href}/`);

  const active = NAV_LINKS.find(isActive);

  return <LearnSubnavLinkList activeHref={active?.href} />;
}
