import Link from "next/link";

export type LatestBlogItem = {
  href: string;
  title: string;
};

type LatestBlogPostsProps = {
  posts: LatestBlogItem[];
  currentHref?: string;
};

export function LatestBlogPosts({ posts, currentHref }: LatestBlogPostsProps) {
  if (posts.length === 0) {
    return null;
  }

  return (
    <div className="px-5 py-8 max-w-[21rem]">
      <p className="text-xs font-semibold uppercase tracking-widest text-slate-400 mb-3">
        Latest Blog Posts
      </p>
      <nav>
        <ul className="space-y-1 border-l border-white/10 text-sm list-none m-0 p-0">
          {posts.map((post) => {
            const isActive = post.href === currentHref;
            return (
              <li key={post.href}>
                <Link
                  href={post.href}
                  data-latest-link
                  data-active={isActive ? "true" : undefined}
                  aria-current={isActive ? "page" : undefined}
                  className="block border-l-2 border-transparent py-1 pl-3 leading-snug no-underline transition-colors"
                >
                  {post.title}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>
    </div>
  );
}
