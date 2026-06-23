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
        className="border-cc-ink-faint bg-cc-white/2.5 hover:border-cc-card-border-hover hover:bg-cc-white/5 flex h-full flex-col overflow-hidden rounded-2xl border no-underline transition-[background-color,border-color,transform] duration-150 hover:-translate-y-0.5"
      >
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
        <div className="flex flex-1 flex-col px-7 pt-6 pb-6">
          <div className="text-cc-ink-dim flex items-center gap-3 text-xs tracking-[0.16em] uppercase">
            {post.category ? (
              <span className="border-cc-ink-faint text-cc-ink rounded-md border py-1.5 pr-[calc(0.5rem-0.16em)] pl-2 leading-none">
                {post.category}
              </span>
            ) : null}
            <time dateTime={post.date}>
              {formatDate(post.date, { month: "short", year: "numeric" })}
            </time>
          </div>
          <h3 className="text-cc-ink m-0 mt-5 mb-3 text-xl leading-tight font-medium tracking-[-0.015em]">
            {post.title}
          </h3>
          {post.description ? (
            <p className="text-cc-ink-dim m-0 mb-6 line-clamp-3 text-sm leading-[1.55]">
              {post.description}
            </p>
          ) : null}
          <span className="text-cc-ink group-hover/teaser:text-cc-accent mt-auto text-xs tracking-[0.18em] uppercase transition-colors">
            Read →
          </span>
        </div>
      </Link>
    </article>
  );
}
