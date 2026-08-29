import Link from "next/link";
import { DrinkIcon } from "@/src/components/DrinkIcon";
import { Picture } from "@/src/design-system/Picture";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { formatDate } from "@/src/helpers/formatDate";
import { PRODUCT_ART } from "./productArt";

interface LearnArticleCardProps {
  readonly post: BlogPostSummary;
  /**
   * `"card"` (default) stacks a 16:9 image above the text for one grid
   * track; `"split"` places the image beside the text from `lg` up and
   * stacks below it, for a section's full-width lead post.
   */
  readonly layout?: "card" | "split";
  /**
   * Kicker text (post category, falling back to a non-date label such as
   * "Article"). The card's meta line already carries `author · date`, so
   * the kicker must never fall back to the date itself, which would render
   * it twice (website-vlm).
   */
  readonly kicker: string;
  /** Link target for the kicker (its hub page, `src/data/learn/hubs.ts`); omit to render the kicker as plain text. */
  readonly kickerHref?: string;
  /** Viewport-to-image-width hint for the call site's column. */
  readonly sizes: string;
}

/**
 * Article card for a blog post (learn-harmonization.md section 2.5.2): 16:9
 * artwork, kicker, title, and `author · date`. Like `LearnListRow`, the
 * title is a stretched link covering the card, so clicking anywhere except
 * the kicker opens the post, and the kicker is its own link to its hub when
 * the caller supplies one (D19). A post without a `featuredImage` falls back
 * to its primary product's art on a tinted 16:9 tile, keeping the grid row's
 * geometry; a post with neither renders headline-first.
 *
 * This is the editorial counterpart to `LearnCard`, which takes a catalog
 * `LearnItemSummary` (template, video, tutorial, example, workshop): that
 * card carries a content-type badge, level or duration meta, and a product
 * and stack icon footer, has no article artwork or `author · date` line, and
 * wraps its whole body in one link, which cannot hold an independently
 * clickable kicker.
 */
export function LearnArticleCard({ post, layout = "card", kicker, kickerHref, sizes }: LearnArticleCardProps) {
  const split = layout === "split";
  const art = post.products[0] ? PRODUCT_ART[post.products[0]] : null;
  const dateLabel = formatDate(post.date, { month: "short", day: "numeric", year: "numeric" });

  // A post with neither artwork nor a product renders headline-first rather
  // than reserving an empty 16:9 tile.
  const showImage = post.featuredImage !== null || art !== null;

  return (
    <div className={`group/card relative flex h-full flex-col ${split ? "lg:flex-row lg:items-center lg:gap-10" : ""}`}>
      {showImage ? (
        <div
          className={`bg-cc-white/4 aspect-video overflow-hidden rounded-2xl ${
            // The split image keeps its authored 16:9 crop, so its height grows
            // with its width; it takes a smaller share of the row from `xl` up
            // to stay in scale with the cards below it.
            split ? "lg:w-[45%] lg:shrink-0 xl:w-[40%]" : ""
          }`}
        >
          {post.featuredImage ? (
            <Picture
              src={post.featuredImage}
              alt=""
              sizes={sizes}
              className="h-full w-full object-cover transition-transform duration-300 group-hover/card:scale-[1.02]"
            />
          ) : (
            <span className="flex h-full w-full items-center justify-center">
              {art ? <DrinkIcon Icon={art.Drink} name={art.drinkName} base={split ? 96 : 56} /> : null}
            </span>
          )}
        </div>
      ) : null}
      <div
        className={`flex flex-1 flex-col ${split ? "lg:min-w-0" : ""} ${showImage ? (split ? "mt-5 lg:mt-0" : "mt-4") : ""}`}
      >
        {kickerHref ? (
          <Link
            href={kickerHref}
            className="text-cc-ink-dim hover:text-cc-accent relative z-10 w-fit font-mono text-xs tracking-wider uppercase no-underline transition-colors"
          >
            {kicker}
          </Link>
        ) : (
          <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">{kicker}</span>
        )}
        <h3 className={`font-heading text-cc-heading mt-2 font-semibold text-balance ${split ? "text-h5" : "text-h6"}`}>
          <Link
            href={post.href}
            className="group-hover/card:text-cc-accent static line-clamp-3 no-underline transition-colors"
          >
            {post.title}
            <span className="absolute inset-0" aria-hidden="true" />
          </Link>
        </h3>
        {split && post.description ? (
          <p className="text-cc-ink-dim mt-4 line-clamp-3 max-w-[68ch] text-lg">{post.description}</p>
        ) : null}
        <span className={`text-cc-ink-dim text-sm ${split ? "mt-6" : "mt-auto pt-3"}`}>
          {post.author ? (
            <>
              {post.author} <span aria-hidden="true">·</span> {dateLabel}
            </>
          ) : (
            dateLabel
          )}
        </span>
      </div>
    </div>
  );
}
