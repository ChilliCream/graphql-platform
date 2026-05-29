import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogPostCard } from "@/src/components/BlogPostCard";
import { loadBlogPosts } from "@/src/helpers/blogCards";

export const dynamicParams = false;

// Collect the set of unique tag strings exactly as authored across all posts.
// Next encodes the route param, so we return the raw (unencoded) values.
export function generateStaticParams(): { tag: string }[] {
  const tags = new Set<string>();
  for (const post of loadBlogPosts()) {
    for (const tag of post.tags) {
      tags.add(tag);
    }
  }
  return [...tags].map((tag) => ({ tag }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ tag: string }>;
}): Promise<Metadata> {
  const { tag } = await params;
  const decoded = decodeURIComponent(tag);
  return { title: `Posts tagged "${decoded}"` };
}

export default async function BlogTagPage({
  params,
}: {
  params: Promise<{ tag: string }>;
}) {
  const { tag } = await params;
  const decoded = decodeURIComponent(tag);
  const needle = decoded.toLowerCase();

  const posts = loadBlogPosts().filter((post) =>
    post.tags.some((t) => t.toLowerCase() === needle)
  );

  if (posts.length === 0) {
    notFound();
  }

  return (
    <div className="cc-content-dark min-h-screen px-5 py-12 sm:py-16">
      <header className="mx-auto mb-12 max-w-3xl text-center">
        <h1 className="text-4xl font-bold tracking-tight text-slate-100 sm:text-5xl">
          Posts tagged &ldquo;{decoded}&rdquo;
        </h1>
        <p className="mt-4 text-lg text-slate-400">
          {posts.length} {posts.length === 1 ? "post" : "posts"}
        </p>
      </header>

      <div className="mx-auto max-w-6xl">
        <ul className="grid list-none grid-cols-1 gap-8 p-0 sm:grid-cols-2 lg:grid-cols-3">
          {posts.map(({ card }) => (
            <li key={card.href}>
              <BlogPostCard post={card} />
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
