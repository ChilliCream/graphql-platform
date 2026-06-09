import Link from "next/link";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";

export type BlogTeaserData = {
  href: string;
  title: string;
  date: string;
  featuredImage: string | null;
  category?: string | null;
  description?: string | null;
  author?: string | null;
  authorImageUrl?: string | null;
};

type BlogTeaserProps = {
  post: BlogTeaserData;
};

export function BlogTeaser({ post }: BlogTeaserProps) {
  return (
    <article className="group/teaser h-full">
      <Link
        href={post.href}
        className="flex h-full flex-col overflow-hidden rounded-2xl border border-cc-ink-faint bg-cc-white/2.5 no-underline transition-[background-color,border-color,transform] duration-150 hover:-translate-y-0.5 hover:border-cc-card-border-hover hover:bg-cc-white/5"
      >
        <div className="aspect-video w-full overflow-hidden border-b border-cc-ink-faint bg-cc-white/4">
          {post.featuredImage ? (
            <Picture
              src={post.featuredImage}
              alt=""
              sizes="(max-width: 768px) 100vw, 400px"
              className="h-full w-full object-cover"
            />
          ) : null}
        </div>
        <div className="flex flex-1 flex-col px-6.5 pt-5.5 pb-6">
          <div className="flex items-center gap-3 text-[11px] uppercase tracking-[0.16em] text-cc-ink-dim">
            {post.category ? (
              <span className="rounded-md border border-cc-ink-faint px-2 py-1 text-cc-ink">
                {post.category}
              </span>
            ) : null}
            <time dateTime={post.date}>
              {formatDate(post.date, { month: "short", year: "numeric" })}
            </time>
          </div>
          <h3 className="m-0 mt-4.5 mb-3 text-[22px] font-medium leading-tight tracking-[-0.015em] text-cc-ink">
            {post.title}
          </h3>
          {post.description ? (
            <p className="m-0 mb-5.5 line-clamp-3 text-[15px] leading-[1.55] text-cc-ink-dim">
              {post.description}
            </p>
          ) : null}
          <span className="mt-auto text-[11px] uppercase tracking-[0.18em] text-cc-ink transition-colors group-hover/teaser:text-cc-accent">
            Read →
          </span>
        </div>
      </Link>
    </article>
  );
}
