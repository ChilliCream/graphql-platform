import Link from "next/link";
import { DrinkIcon } from "@/src/components/DrinkIcon";
import type { ProductKey } from "@/src/data/learn/facets";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";
import { PRODUCT_ART } from "./productArt";

interface LearnListRowProps {
  readonly href: string;
  readonly title: string;
  /** Kicker text (post category, falling back to the primary topic label). */
  readonly kicker: string;
  readonly featuredImage: string | null;
  /** Thumbnail fallback icon when there is no `featuredImage`; `null` renders a plain square. */
  readonly product: ProductKey | null;
  readonly author: string | null;
  readonly date: string;
}

/**
 * Compact article list row (learn-editorial.md section 14.1): the whole row
 * is one link, no card surface. Rows carry their own bottom-border divider
 * (rather than a `divide-y` container) so the same row renders correctly
 * whether its list is one column or the two-column ramp the editorial band
 * uses (learn-editorial.md section 15.1); the established precedent for this
 * is `LearnExplainerList`'s per-row `border-b`.
 */
export function LearnListRow({ href, title, kicker, featuredImage, product, author, date }: LearnListRowProps) {
  const art = product ? PRODUCT_ART[product] : null;
  const dateLabel = formatDate(date, { month: "short", day: "numeric", year: "numeric" });

  return (
    <Link
      href={href}
      className="group/row border-cc-ink-faint grid grid-cols-[auto_1fr] items-start gap-4 border-b py-4 no-underline first:pt-0"
    >
      {featuredImage ? (
        <Picture
          src={featuredImage}
          alt=""
          sizes="80px"
          className="border-cc-ink-faint size-20 shrink-0 rounded-lg border object-cover"
        />
      ) : (
        <span className="bg-cc-white/4 border-cc-ink-faint flex size-20 shrink-0 items-center justify-center rounded-lg border">
          {art ? <DrinkIcon Icon={art.Drink} name={art.drinkName} base={40} /> : null}
        </span>
      )}
      <span className="flex flex-col gap-1.5">
        <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">{kicker}</span>
        <span className="text-cc-heading group-hover/row:text-cc-accent line-clamp-2 leading-snug font-medium transition-colors">
          {title}
        </span>
        <span className="text-cc-ink-dim text-sm">
          {author ? (
            <>
              {author} <span aria-hidden="true">·</span> {dateLabel}
            </>
          ) : (
            dateLabel
          )}
        </span>
      </span>
    </Link>
  );
}
