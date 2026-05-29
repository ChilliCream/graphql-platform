import path from "node:path";
import type { Metadata } from "next";
import Image from "next/image";
import { notFound } from "next/navigation";
import { BlogMetadata } from "@/src/design-system/BlogMetadata";
import { BlogTags } from "@/src/design-system/BlogTags";
import { LatestBlogPosts } from "@/src/design-system/LatestBlogPosts";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { loadBlogPosts } from "@/src/helpers/blogCards";
import {
  BLOG_ROOT,
  blogUrlFromBlogRelPath,
  listBlogPosts,
  resolveBlogFile,
} from "@/src/helpers/blogPaths";
import { compileDoc } from "@/src/helpers/compileDoc";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";

// How many recent posts to surface in the left "Latest Blog Posts" rail.
const LATEST_COUNT = 8;

type BlogFrontmatter = {
  title?: string;
  description?: string;
  author?: string;
  authorUrl?: string;
  authorImageUrl?: string;
  date?: string;
  tags?: string[];
};

export const dynamicParams = false;

export function generateStaticParams(): { slug: string[] }[] {
  return listBlogPosts().map(({ parsed }) => ({
    slug: [parsed.year, parsed.month, parsed.day, parsed.slug],
  }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const rel = resolveBlogFile(slug);
  if (rel === null) {
    return {};
  }
  const { title, description } = readFrontmatter(path.join(BLOG_ROOT, rel));
  return {
    title,
    description,
  };
}

export default async function BlogPage({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}) {
  const { slug } = await params;
  const rel = resolveBlogFile(slug);

  if (rel === null) {
    notFound();
  }

  const { content, frontmatter, toc } = await compileDoc<BlogFrontmatter>(
    path.join(BLOG_ROOT, rel)
  );

  // loadBlogPosts() already resolves featured-image URLs and sorts newest
  // first, so we reuse it for both the header hero and the left rail rather
  // than re-deriving the image path here.
  const currentHref = blogUrlFromBlogRelPath(rel);
  const allPosts = loadBlogPosts();
  const featuredImage = allPosts.find(
    (p) => p.card.href === currentHref
  )?.card.featuredImage;
  const latestPosts = allPosts.slice(0, LATEST_COUNT).map((p) => ({
    href: p.card.href,
    title: p.card.title,
  }));

  return (
    <div className="cc-content-dark cc-prose-invert grid min-h-screen grid-cols-1 xl:grid-cols-[16rem_minmax(0,1fr)_20rem]">
      <aside className="hidden xl:block sticky top-[72px] self-start h-[calc(100vh-72px)] overflow-y-auto">
        <LatestBlogPosts posts={latestPosts} currentHref={currentHref ?? undefined} />
      </aside>
      <main className="min-w-0 px-5 py-8 sm:px-12">
        <article className="mx-auto max-w-5xl">
          {featuredImage ? (
            <div className="not-prose mb-8 aspect-[16/9] w-full overflow-hidden rounded-xl bg-[#0c1322]">
              <Image
                src={featuredImage}
                alt={frontmatter.title ?? ""}
                width={1280}
                height={720}
                priority
                className="h-full w-full object-cover"
              />
            </div>
          ) : null}
          {frontmatter.title ? (
            <Typography variant="h1">{frontmatter.title}</Typography>
          ) : null}
          <BlogMetadata
            author={frontmatter.author}
            authorUrl={frontmatter.authorUrl}
            authorImageUrl={frontmatter.authorImageUrl}
            date={frontmatter.date}
          />
          <BlogTags tags={frontmatter.tags} />
          {content}
        </article>
      </main>
      <aside className="hidden xl:block sticky top-[72px] self-start h-[calc(100vh-72px)] overflow-y-auto">
        <TableOfContents items={toc} label="In this Article" />
      </aside>
    </div>
  );
}
