import type { Metadata } from "next";
import { BlogPostCard } from "@/src/components/BlogPostCard";
import { loadBlogPosts } from "@/src/helpers/blogCards";

export const metadata: Metadata = { title: "Blog" };

export default function BlogListPage() {
  const posts = loadBlogPosts();

  return (
    <div className="cc-content-dark min-h-screen px-5 py-12 sm:py-16">
      <header className="mx-auto mb-12 max-w-3xl text-center">
        <h1 className="text-4xl font-bold tracking-tight text-slate-100 sm:text-5xl">
          Blog
        </h1>
        <p className="mt-4 text-lg text-slate-400">
          The latest news about ChilliCream and our products
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
