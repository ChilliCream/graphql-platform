import Link from "next/link";
import { ArrowLink } from "@/src/components/ArrowLink";
import { CardGrid } from "@/src/components/CardGrid";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { LearnCard } from "./LearnCard";

interface CollectionSubLink {
  readonly label: string;
  readonly href: string;
}

interface LearnCollectionSectionProps {
  readonly items: readonly LearnItemSummary[];
  readonly subLinks: readonly CollectionSubLink[];
}

/**
 * The curated "use this" band (learn-editorial.md section 3.5): a preview of
 * `/learn/browse` built from the catalog's own card and grid, plus
 * type-scoped sub-links into the browse surface.
 */
export function LearnCollectionSection({ items, subLinks }: LearnCollectionSectionProps) {
  if (items.length === 0) {
    return null;
  }
  return (
    <section className="border-cc-card-border border-t py-14 sm:py-20">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Start building</h2>
        <ArrowLink href="/learn/browse">Browse the catalog</ArrowLink>
      </div>
      <CardGrid cols={3} step="progressive" itemsStretch>
        {items.map((item) => (
          <LearnCard key={`${item.type}-${item.slug}`} item={item} />
        ))}
      </CardGrid>
      {subLinks.length > 0 ? (
        <div className="mt-6 flex flex-wrap gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
          {subLinks.map((link) => (
            <Link key={link.href} href={link.href} className="text-cc-ink-dim hover:text-cc-heading transition-colors">
              {link.label}
            </Link>
          ))}
        </div>
      ) : null}
    </section>
  );
}
