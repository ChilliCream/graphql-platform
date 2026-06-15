import Link from "next/link";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";

/**
 * Left rail for blog posts mirroring the docs sidebar, listing the most recent
 * posts (newest first) so readers can jump between articles. The currently open
 * post is excluded by the caller, which also caps the list length.
 */
export function BlogSidebar({
  posts,
  currentHref,
}: {
  posts: BlogPostSummary[];
  currentHref: string;
}) {
  return (
    <div className="flex min-h-0 flex-1 flex-col gap-2 px-5 py-6 text-sm">
      <p className="px-3 text-xs font-semibold uppercase tracking-widest text-cc-ink-dim">
        Latest posts
      </p>
      <nav className="min-h-0 flex-1 overflow-y-auto overflow-x-hidden">
        <ul className="flex flex-col gap-1">
          {posts.map((post) => {
            const isActive = post.href === currentHref;
            return (
              <li key={post.stem}>
                <Link
                  href={post.href}
                  prefetch={false}
                  aria-current={isActive ? "page" : undefined}
                  className={`block rounded px-3 py-1.5 transition-colors ${
                    isActive
                      ? "bg-cc-white/10 font-medium text-cc-white"
                      : "text-cc-nav-text hover:bg-cc-white/5 hover:text-cc-white"
                  }`}
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
