import Link from "next/link";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";

interface LearnFeatureHeroProps {
  readonly post: BlogPostSummary;
}

/**
 * The landing's single editorially pinned story (learn-editorial.md section
 * 3.2): the `BlogTeaser` recipe scaled up to a two-column panel, image-first
 * on mobile. Falls back to a single column when the post has no
 * `featuredImage`; the hero never renders empty.
 */
export function LearnFeatureHero({ post }: LearnFeatureHeroProps) {
  const hasImage = Boolean(post.featuredImage);

  return (
    <Link
      href={post.href}
      className={`group/hero border-cc-ink-faint bg-cc-white/2.5 hover:border-cc-card-border-hover hover:bg-cc-white/5 grid overflow-hidden rounded-2xl border no-underline transition-[background-color,border-color,transform] duration-150 hover:-translate-y-0.5 ${
        hasImage ? "lg:grid-cols-[1.1fr_1fr]" : ""
      }`}
    >
      {hasImage ? (
        <div className="border-cc-ink-faint bg-cc-white/4 aspect-video w-full overflow-hidden border-b lg:order-2 lg:aspect-auto lg:border-b-0 lg:border-l">
          <Picture
            src={post.featuredImage as string}
            alt=""
            sizes="(max-width: 1023px) 100vw, 45vw"
            className="h-full w-full object-cover"
          />
        </div>
      ) : null}
      <div className="flex flex-col justify-center px-7 py-8 sm:px-10 sm:py-12 lg:order-1">
        <div className="flex flex-wrap items-center gap-3">
          <Eyebrow as="span" color="accent">
            Featured
          </Eyebrow>
          {post.category ? (
            <span className="border-cc-ink-faint text-cc-ink rounded-md border py-1.5 pr-[calc(0.5rem-0.16em)] pl-2 font-mono text-xs leading-none tracking-[0.16em] uppercase">
              {post.category}
            </span>
          ) : null}
          <time dateTime={post.date} className="text-cc-ink-dim font-mono text-xs tracking-[0.16em] uppercase">
            {formatDate(post.date, { month: "short", year: "numeric" })}
          </time>
        </div>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 lg:text-h2 mt-5 font-semibold text-balance">
          {post.title}
        </h2>
        {post.description ? (
          <p className="text-cc-ink-dim mt-4 line-clamp-3 text-base sm:text-lg">{post.description}</p>
        ) : null}
        {/*
          Not `BlogMetadata`: this whole panel is already one `<a>` (the
          hero recipe, section 3.2), and `BlogMetadata` wraps its author in
          an anchor whenever one is set, which nested inside this Link would
          produce invalid <a> inside <a> markup and a hydration mismatch.
          This renders the same byline information without that nested link.
        */}
        {post.author ? (
          <div className="text-cc-ink-dim mt-6 flex items-center gap-2 text-sm">
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
            <span>{formatDate(post.date, { month: "short", day: "numeric", year: "numeric" })}</span>
          </div>
        ) : null}
        <span className="text-cc-accent group-hover/hero:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors">
          Read story
          <ArrowRightIcon className="size-3.5" />
        </span>
      </div>
    </Link>
  );
}
