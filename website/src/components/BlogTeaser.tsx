import Link from "next/link";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";

export type BlogTeaserData = {
  href: string;
  title: string;
  date?: string;
  featuredImage: string | null;
  category?: string | null;
  description?: string | null;
  author?: string | null;
  authorImageUrl?: string | null;
};

type BlogTeaserProps = {
  post: BlogTeaserData;
  /** Renders the image band above the title. Defaults to shown. */
  showMedia?: boolean;
  /** Clamps the description to 3 lines. Defaults to clamped. */
  clampDescription?: boolean;
};

export function BlogTeaser({
  post,
  showMedia = true,
  clampDescription = true,
}: BlogTeaserProps) {
  return (
    <article className="group/teaser h-full w-full">
      <Link
        href={post.href}
        className="border-cc-ink-faint bg-cc-white/2.5 hover:border-cc-card-border-hover hover:bg-cc-white/5 flex h-full flex-col overflow-hidden rounded-2xl border no-underline transition-[background-color,border-color,transform] duration-150 hover:-translate-y-0.5"
      >
        {showMedia ? (
          <div className="border-cc-ink-faint bg-cc-white/4 aspect-video w-full overflow-hidden border-b">
            {post.featuredImage ? (
              <Picture
                src={post.featuredImage}
                alt=""
                sizes="(max-width: 768px) 100vw, 400px"
                className="h-full w-full object-cover"
              />
            ) : null}
          </div>
        ) : null}
        <div className="flex flex-1 flex-col px-7 pt-6 pb-6">
          <div className="text-cc-ink-dim flex items-center gap-3 text-xs tracking-[0.16em] uppercase">
            {post.category ? (
              <span className="border-cc-ink-faint text-cc-ink rounded-md border py-1.5 pr-[calc(0.5rem-0.16em)] pl-2 leading-none">
                {post.category}
              </span>
            ) : null}
            {post.date ? (
              <time dateTime={post.date}>
                {formatDate(post.date, { month: "short", year: "numeric" })}
              </time>
            ) : null}
          </div>
          <h3 className="text-cc-heading m-0 mt-5 mb-3 text-xl leading-tight font-medium tracking-[-0.015em]">
            {post.title}
          </h3>
          {post.description ? (
            <p
              className={
                clampDescription
                  ? "text-cc-ink-dim m-0 mb-6 line-clamp-3 text-sm leading-[1.55]"
                  : "text-cc-ink-dim m-0 mb-6 text-sm leading-[1.55]"
              }
            >
              {post.description}
            </p>
          ) : null}
          <span className="text-cc-ink group-hover/teaser:text-cc-accent mt-auto inline-flex items-center gap-1.5 text-xs tracking-[0.18em] uppercase transition-colors">
            Read
            <ArrowRightIcon className="size-3.5" />
          </span>
        </div>
      </Link>
    </article>
  );
}
