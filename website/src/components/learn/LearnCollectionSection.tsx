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
  /** Target for the "Browse the catalog" link; defaults to the unfiltered catalog. A topic hub page passes its own pre-filtered `/learn/browse` href. */
  readonly browseHref?: string;
}

/**
 * The curated "use this" band (learn-editorial.md section 3.5): a preview of
 * `/learn/browse` built from the catalog's own card and grid, plus
 * type-scoped sub-links into the browse surface. Tinted per
 * learn-harmonization.md section 2.5.3: full-bleed card background, no
 * `border-t` seam, and a uniform `LearnCard` grid (website-kbx.21: the
 * former full-width lead card made the section too heavy).
 */
export function LearnCollectionSection({ items, subLinks, browseHref = "/learn/browse" }: LearnCollectionSectionProps) {
  if (items.length === 0) {
    return null;
  }

  return (
    // Breaks out of the `(learn)` layout's `max-w-8xl` gutter so the tint
    // reaches the viewport edge (learn-harmonization.md section 2.5.3). The
    // horizontal padding is applied here, on the full-viewport section,
    // before the `max-w-8xl` centering below, mirroring the layout's own
    // `px-5 py-8 sm:px-12` -> `max-w-8xl mx-auto` order (website-kbx.5): that
    // is what keeps this section's content edge lined up with every other
    // section's, at every viewport width the `max-w-8xl` cap can bind at.
    <section className="bg-cc-card-bg relative left-1/2 w-screen -translate-x-1/2 px-5 py-10 sm:px-12 sm:py-12">
      <div className="max-w-8xl mx-auto">
        <div className="mb-8 flex items-center justify-between gap-4">
          <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Start building</h2>
          <ArrowLink href={browseHref}>Browse the catalog</ArrowLink>
        </div>
        <CardGrid cols={3} step="progressive" itemsStretch>
          {items.map((item) => (
            <LearnCard key={`${item.type}-${item.slug}`} item={item} />
          ))}
        </CardGrid>
        {subLinks.length > 0 ? (
          <div className="mt-6 flex flex-wrap gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
            {subLinks.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className="text-cc-ink-dim hover:text-cc-heading transition-colors"
              >
                {link.label}
              </Link>
            ))}
          </div>
        ) : null}
      </div>
    </section>
  );
}
