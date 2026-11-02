import { BlogTeaser, type BlogTeaserData } from "./BlogTeaser";

type BlogTeaserGridProps = {
  posts: readonly BlogTeaserData[];
  /** Renders each card's image band. Defaults to shown. */
  showMedia?: boolean;
  /** Clamps each description to 3 lines. Defaults to clamped. */
  clampDescription?: boolean;
};

export function BlogTeaserGrid({
  posts,
  showMedia = true,
  clampDescription = true,
}: BlogTeaserGridProps) {
  if (posts.length === 0) {
    return <p className="text-cc-ink-dim">No posts yet. Check back soon.</p>;
  }

  return (
    <ul className="m-0 grid list-none grid-cols-1 gap-5 p-0 sm:grid-cols-2 lg:grid-cols-3">
      {posts.map((post) => (
        <li key={post.href} className="m-0 flex p-0">
          <BlogTeaser
            post={post}
            showMedia={showMedia}
            clampDescription={clampDescription}
          />
        </li>
      ))}
    </ul>
  );
}
