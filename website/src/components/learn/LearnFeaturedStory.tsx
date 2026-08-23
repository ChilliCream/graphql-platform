import Link from "next/link";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";

interface LearnFeaturedStoryProps {
  readonly post: BlogPostSummary;
  /** Marks the story's image as the page's LCP candidate. */
  readonly priority?: boolean;
}

/**
 * The page's single pinned story (learn-editorial.md section 14.2), retiring
 * `LearnFeatureHero`: an open editorial composition, not a boxed panel, so it
 * reads bigger than the surrounding content rather than "same card, larger".
 * The whole composition is one link; there is no per-item CTA label (section
 * 14.5). Falls back to a headline-first layout when the post has no
 * `featuredImage`.
 */
export function LearnFeaturedStory({ post, priority = false }: LearnFeaturedStoryProps) {
  const { featuredImage } = post;

  return (
    <Link href={post.href} className="group/featured flex flex-col no-underline">
      {featuredImage ? (
        <div className="border-cc-ink-faint aspect-video overflow-hidden rounded-2xl border">
          <Picture
            src={featuredImage}
            alt=""
            sizes="(max-width: 1279px) 100vw, 38vw"
            priority={priority}
            className="h-full w-full object-cover"
          />
        </div>
      ) : null}
      <div className={`flex flex-wrap items-center gap-3 ${featuredImage ? "mt-6" : ""}`}>
        <Eyebrow as="span" color="accent">
          Featured
        </Eyebrow>
        {post.category ? (
          <span className="border-cc-ink-faint text-cc-ink rounded-md border py-1.5 pr-[calc(0.5rem-0.16em)] pl-2 font-mono text-xs leading-none tracking-[0.16em] uppercase">
            {post.category}
          </span>
        ) : null}
      </div>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 xl:text-h2 group-hover/featured:text-cc-accent mt-5 font-semibold text-balance transition-colors">
        {post.title}
      </h2>
      {post.description ? <p className="text-cc-ink-dim mt-4 line-clamp-3 text-lg">{post.description}</p> : null}
      <div className="text-cc-ink-dim mt-6 flex items-center gap-2 text-sm">
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
    </Link>
  );
}
