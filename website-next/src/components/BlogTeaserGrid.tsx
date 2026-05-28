import { BlogTeaser, type BlogTeaserData } from "./BlogTeaser";

type BlogTeaserGridProps = {
  posts: BlogTeaserData[];
};

export function BlogTeaserGrid({ posts }: BlogTeaserGridProps) {
  if (posts.length === 0) {
    return (
      <p className="text-stone-600">
        No posts yet. Check back soon.
      </p>
    );
  }

  return (
    <ul className="m-0 grid list-none grid-cols-1 gap-6 p-0 sm:grid-cols-2 lg:grid-cols-3">
      {posts.map((post) => (
        <li key={post.href} className="m-0 flex p-0">
          <BlogTeaser post={post} />
        </li>
      ))}
    </ul>
  );
}
