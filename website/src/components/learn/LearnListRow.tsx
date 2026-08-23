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
  /**
   * `compact` drops the thumbnail to 48px (or omits it when there is no
   * `featuredImage`) for narrow columns, e.g. the editorial band's rail and a
   * topic rail's secondary items (learn-harmonization.md section 2.5.1/2.5.2).
   */
  readonly density?: "default" | "compact";
}

/**
 * Compact article list row (learn-editorial.md section 14.1, amended by
 * learn-harmonization.md D3/D7): the whole row is one link, no card surface.
 * Rows carry their own bottom-border divider (rather than a `divide-y`
 * container) so the same row renders correctly whether its list is one
 * column or the two-column ramp the editorial band uses (learn-editorial.md
 * section 15.1); the established precedent for this is `LearnExplainerList`'s
 * per-row `border-b`.
 */
export function LearnListRow({
  href,
  title,
  kicker,
  featuredImage,
  product,
  author,
  date,
  density = "default",
}: LearnListRowProps) {
  const art = product ? PRODUCT_ART[product] : null;
  const dateLabel = formatDate(date, { month: "short", day: "numeric", year: "numeric" });
  const compact = density === "compact";
  const thumbSize = compact ? "size-12" : "size-20";
  const showThumb = !compact || featuredImage !== null;

  return (
    <Link
      href={href}
      className={`group/row border-cc-card-border grid items-start gap-4 border-b py-5 no-underline ${
        showThumb ? "grid-cols-[auto_1fr]" : "grid-cols-1"
      }`}
    >
      {showThumb ? (
        featuredImage ? (
          <Picture
            src={featuredImage}
            alt=""
            sizes={compact ? "48px" : "80px"}
            className={`${thumbSize} shrink-0 rounded-lg object-cover`}
          />
        ) : (
          <span className={`bg-cc-white/4 flex ${thumbSize} shrink-0 items-center justify-center rounded-lg`}>
            {art ? <DrinkIcon Icon={art.Drink} name={art.drinkName} base={compact ? 24 : 40} /> : null}
          </span>
        )
      ) : null}
      <span className="flex flex-col gap-1.5">
        <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">{kicker}</span>
        <span className="font-heading text-h6 text-cc-heading group-hover/row:text-cc-accent line-clamp-2 font-semibold transition-colors">
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
