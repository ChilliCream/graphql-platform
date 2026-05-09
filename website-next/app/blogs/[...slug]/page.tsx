import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogMetadata } from "@/src/design-system/BlogMetadata";
import { BlogTags } from "@/src/design-system/BlogTags";
import { Sidebar } from "@/src/design-system/Sidebar";
import { SidebarDrawer } from "@/src/design-system/SidebarDrawer";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import type { HeadingItem } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { buildBlogTree } from "@/src/helpers/buildBlogTree";
import {
  BLOG_ROOT,
  listBlogPosts,
  resolveBlogFile,
} from "@/src/helpers/blogPaths";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";

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

  const frontmatter = readFrontmatter(path.join(BLOG_ROOT, rel));
  const title =
    typeof frontmatter.title === "string" ? frontmatter.title : undefined;
  const author =
    typeof frontmatter.author === "string" ? frontmatter.author : undefined;
  const authorUrl =
    typeof frontmatter.authorUrl === "string"
      ? frontmatter.authorUrl
      : undefined;
  const authorImageUrl =
    typeof frontmatter.authorImageUrl === "string"
      ? frontmatter.authorImageUrl
      : undefined;
  const date =
    typeof frontmatter.date === "string" ? frontmatter.date : undefined;
  const tags = Array.isArray(frontmatter.tags)
    ? (frontmatter.tags as unknown[]).filter(
        (t): t is string => typeof t === "string"
      )
    : undefined;

  const mod = await import(`@/blogs/${rel.slice(0, -3)}.md`);
  const Post = mod.default;
  const toc: HeadingItem[] = Array.isArray(mod.toc) ? mod.toc : [];
  const tree = buildBlogTree();

  return (
    <div className="grid grid-cols-1 lg:grid-cols-[20rem_1fr] 2xl:grid-cols-[20rem_1fr_20rem]">
      <SidebarDrawer>
        <Sidebar tree={tree} />
      </SidebarDrawer>
      <main className="min-w-0 px-5 py-8 sm:px-12">
        <article className="mx-auto max-w-5xl">
          {title ? <Typography variant="h1">{title}</Typography> : null}
          <BlogMetadata
            author={author}
            authorUrl={authorUrl}
            authorImageUrl={authorImageUrl}
            date={date}
          />
          <BlogTags tags={tags} />
          <Post />
        </article>
      </main>
      <TableOfContents items={toc} />
    </div>
  );
}
