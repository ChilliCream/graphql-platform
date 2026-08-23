import Link from "next/link";
import { ArrowLink } from "@/src/components/ArrowLink";
import { CardGrid } from "@/src/components/CardGrid";
import type { ArticleSummary } from "@/src/helpers/articles";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { LearnCard } from "./LearnCard";
import { LearnFeatureCard } from "./LearnFeatureCard";
import { LearnListRow } from "./LearnListRow";

interface CollectionSubLink {
  readonly label: string;
  readonly href: string;
}

interface LearnCollectionSectionProps {
  readonly items: readonly LearnItemSummary[];
  readonly subLinks: readonly CollectionSubLink[];
  /**
   * A lone explainer/comparison article, folded in here as a `LearnListRow`
   * when there are fewer than 3 (learn-harmonization.md section 2.5.4, D1):
   * `LearnExplainerList` does not render its own section below that count.
   */
  readonly foldedExplainer?: ArticleSummary | null;
}

/**
 * The curated "use this" band (learn-editorial.md section 3.5): a preview of
 * `/learn/browse` built from the catalog's own card and grid, plus
 * type-scoped sub-links into the browse surface. Tinted per
 * learn-harmonization.md section 2.5.3: full-bleed card background, no
 * `border-t` seam, and a horizontal `LearnFeatureCard` leading the grid.
 */
export function LearnCollectionSection({ items, subLinks, foldedExplainer }: LearnCollectionSectionProps) {
  if (items.length === 0) {
    return null;
  }
  const [lead, ...rest] = items;

  return (
    <section className="bg-cc-card-bg py-10 sm:py-12">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Start building</h2>
        <ArrowLink href="/learn/browse">Browse the catalog</ArrowLink>
      </div>
      {lead ? (
        <div className="mb-6">
          <LearnFeatureCard item={lead} />
        </div>
      ) : null}
      <CardGrid cols={3} step="progressive" itemsStretch>
        {rest.map((item) => (
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
      {foldedExplainer ? (
        <div className="border-cc-card-border mt-8 border-t pt-6">
          <LearnListRow
            href={foldedExplainer.href}
            title={foldedExplainer.title}
            kicker="Explainer"
            featuredImage={foldedExplainer.featuredImage}
            product={foldedExplainer.products[0] ?? null}
            author={foldedExplainer.author}
            date={foldedExplainer.date}
          />
        </div>
      ) : null}
    </section>
  );
}
