import Link from "next/link";
import type { ArticleSummary } from "@/src/helpers/articles";
import { ContentTypeBadge } from "./ContentTypeBadge";

interface LearnExplainerListProps {
  readonly articles: readonly ArticleSummary[];
}

/** Below this count, the section folds into an adjacent band instead of rendering on its own (learn-harmonization.md section 2.5.4, D1). */
export const EXPLAINER_LIST_MIN_ITEMS = 3;

/**
 * Explainers and comparisons section (learn-editorial.md section 3.6):
 * reference-list rows rather than cards, since these have no imagery. Not
 * cards: kind chip, title, one-line clamped description, row separated by a
 * bottom border. Renders only once there are at least {@link MIN_ITEMS}
 * articles; below that the caller folds the lone item into an adjacent band.
 *
 * The header's "All explainers" arrow link targets `/learn/browse?type=explainer`
 * once the catalog knows the type; comparison/explainer stay off the
 * `/learn/browse` facet bar (see the browse route's decision comment), so
 * this section omits that link rather than pointing at a facet that does
 * not exist.
 */
export function LearnExplainerList({ articles }: LearnExplainerListProps) {
  if (articles.length < EXPLAINER_LIST_MIN_ITEMS) {
    return null;
  }
  return (
    <section className="py-10 sm:py-12">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Explainers</h2>
      </div>
      <ul className="m-0 grid list-none grid-cols-1 gap-x-10 p-0 lg:grid-cols-2">
        {articles.map((article) => (
          <li key={article.slug} className="border-cc-card-border border-b py-5 first:pt-0">
            <Link href={article.href} className="group/row flex flex-col gap-2 no-underline">
              <ContentTypeBadge type={article.kind} />
              <span className="font-heading text-h6 text-cc-heading group-hover/row:text-cc-accent font-semibold transition-colors">
                {article.title}
              </span>
              {article.description ? (
                <span className="text-cc-ink-dim line-clamp-1 text-sm">{article.description}</span>
              ) : null}
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
