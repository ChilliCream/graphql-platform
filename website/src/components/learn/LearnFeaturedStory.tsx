import Link from "next/link";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Picture } from "@/src/design-system/Picture";
import { Tag } from "@/src/design-system/Tag";
import { formatDate } from "@/src/helpers/formatDate";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";

interface LearnFeaturedStoryProps {
  readonly post: BlogPostSummary;
  /** Marks the story's image as the page's LCP candidate. */
  readonly priority?: boolean;
  /** Viewport-to-image-width hint for the call site's column; defaults to the landing band's center column. */
  readonly sizes?: string;
  /** "stacked" (default) renders the image above the text at all widths; "split" places the image beside the title/dek/meta from `lg` up and stacks below `lg`. */
  readonly layout?: "stacked" | "split";
}

/**
 * The page's single pinned story (learn-editorial.md section 14.2, amended by
 * learn-harmonization.md D3/D6), retiring `LearnFeatureHero`: an open
 * editorial composition, not a boxed panel, so it reads bigger than the
 * surrounding content rather than "same card, larger". The whole composition
 * is one link; there is no per-item CTA label (section 14.5). Falls back to a
 * headline-first layout when the post has no `featuredImage`.
 */
export function LearnFeaturedStory({
  post,
  priority = false,
  sizes = "(max-width: 1279px) 100vw, 38vw",
  layout = "stacked",
}: LearnFeaturedStoryProps) {
  const { featuredImage } = post;
  const isSplit = layout === "split";

  return (
    <Link
      href={post.href}
      className={`group/featured flex flex-col no-underline ${isSplit ? "lg:flex-row lg:items-center lg:gap-10" : ""}`}
    >
      {featuredImage ? (
        <div
          className={`aspect-video overflow-hidden rounded-2xl ${
            isSplit ? "lg:aspect-auto lg:h-[22rem] lg:w-[45%] lg:shrink-0" : ""
          }`}
        >
          <Picture
            src={featuredImage}
            alt=""
            sizes={sizes}
            priority={priority}
            className="h-full w-full object-cover"
          />
        </div>
      ) : null}
      <div className={isSplit ? "lg:min-w-0 lg:flex-1" : undefined}>
        <div className={`flex flex-wrap items-center gap-3 ${featuredImage ? "mt-6" : ""} ${isSplit ? "lg:mt-0" : ""}`}>
          <Eyebrow as="span" color="accent">
            Featured
          </Eyebrow>
          {post.category ? <Tag>{post.category}</Tag> : null}
        </div>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 group-hover/featured:text-cc-accent mt-5 font-semibold text-balance transition-colors">
          {post.title}
        </h2>
        {post.description ? <p className="text-cc-ink-dim mt-4 line-clamp-3 text-lg">{post.description}</p> : null}
        <div className="text-cc-ink-dim mt-8 flex items-center gap-3 text-sm">
          {post.author ? (
            <>
              {post.authorImageUrl ? (
                <Picture
                  src={post.authorImageUrl}
                  alt=""
                  width={30}
                  height={30}
                  sizes="30px"
                  className="h-[30px] w-[30px] rounded-full object-cover"
                />
              ) : null}
              <span>{post.author}</span>
              <span aria-hidden="true">·</span>
            </>
          ) : null}
          <span>{formatDate(post.date, { month: "short", day: "numeric", year: "numeric" })}</span>
        </div>
      </div>
    </Link>
  );
}
