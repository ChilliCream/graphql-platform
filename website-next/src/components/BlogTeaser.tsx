import Link from "next/link";

export type BlogTeaserData = {
  href: string;
  title: string;
  date: string;
  featuredImage: string | null;
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
        className="flex h-full flex-col overflow-hidden rounded-lg border border-slate-200 bg-white text-slate-900 no-underline transition-shadow hover:shadow-md"
      >
        <div className="relative aspect-[16/9] w-full overflow-hidden bg-slate-100">
          {post.featuredImage ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={post.featuredImage}
              alt=""
              loading="lazy"
              decoding="async"
              className="h-full w-full object-cover transition-transform duration-300 group-hover/teaser:scale-[1.02]"
            />
          ) : null}
        </div>
        <div className="flex flex-1 flex-col gap-3 p-5">
          <h3 className="m-0 line-clamp-3 text-lg font-semibold leading-snug text-slate-900 group-hover/teaser:text-primary-700">
            {post.title}
          </h3>
          <div className="mt-auto flex items-center gap-2 text-xs text-slate-500">
            {post.author ? (
              <span className="flex items-center gap-1.5">
                {post.authorImageUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={post.authorImageUrl}
                    alt=""
                    width={20}
                    height={20}
                    loading="lazy"
                    decoding="async"
                    className="h-5 w-5 rounded-full object-cover"
                  />
                ) : null}
                <span>{post.author}</span>
              </span>
            ) : null}
            {post.author ? <span aria-hidden="true">·</span> : null}
            <time dateTime={post.date}>{formatDate(post.date)}</time>
          </div>
        </div>
      </Link>
    </article>
  );
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return iso;
  }
  return d.toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
