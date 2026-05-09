import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogMetadata } from "@/src/design-system/BlogMetadata";
import { BlogTags } from "@/src/design-system/BlogTags";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import {
  BLOG_ROOT,
  listBlogPosts,
  resolveBlogFile,
} from "@/src/helpers/blogPaths";
import { compileDoc } from "@/src/helpers/compileDoc";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";

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

  return (
    <div className="grid grid-cols-1 2xl:grid-cols-[1fr_20rem]">
      <main className="min-w-0 px-5 py-8 sm:px-12">
        <article className="mx-auto max-w-5xl">
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
      <TableOfContents items={toc} />
    </div>
  );
}
