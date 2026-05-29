import Image from "next/image";
import Link from "next/link";
import type { BlogCard } from "@/src/helpers/blogCards";

export function BlogPostCard({ post }: { post: BlogCard }) {
  return (
    <Link
      href={post.href}
      className="group flex h-full flex-col overflow-hidden rounded-lg border border-white/10 bg-[#0c1322] no-underline transition-colors hover:border-[var(--cc-accent)]"
    >
      {post.featuredImage ? (
        <div className="relative aspect-[16/9] w-full overflow-hidden bg-[#0c1322]">
          <Image
            src={post.featuredImage}
            alt={post.title}
            width={640}
            height={360}
            className="h-full w-full object-cover"
          />
        </div>
      ) : (
        <div className="aspect-[16/9] w-full bg-white/[0.02]" />
      )}

      <div className="flex flex-1 flex-col p-5">
        {post.author ? (
          <div className="flex items-center gap-2">
            {post.authorImageUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={post.authorImageUrl}
                alt={`${post.author}'s avatar`}
                width={24}
                height={24}
                loading="lazy"
                decoding="async"
                className="h-6 w-6 rounded-full object-cover"
              />
            ) : null}
            <span className="text-sm text-slate-300">{post.author}</span>
          </div>
        ) : null}

        <h2 className="mt-3 text-lg font-semibold leading-snug text-slate-100 group-hover:text-[var(--cc-accent)]">
          {post.title}
        </h2>

        <p className="mt-auto pt-4 text-sm text-slate-400">
          {post.formattedDate ? <span>{post.formattedDate}</span> : null}
          {post.formattedDate ? <span> · </span> : null}
          <span>{post.readTime} min read</span>
        </p>
      </div>
    </Link>
  );
}
