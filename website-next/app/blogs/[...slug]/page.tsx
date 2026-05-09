import fs from "node:fs";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import type { HeadingItem } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";

const CONTENT_ROOT = path.join(process.cwd(), "blogs");

export const dynamicParams = false;

export function generateStaticParams(): { slug: string[] }[] {
  if (!fs.existsSync(CONTENT_ROOT)) {
    return [];
  }

  return walk(CONTENT_ROOT)
    .filter((f) => /\.mdx?$/.test(f))
    .map((f) => path.relative(CONTENT_ROOT, f).replace(/\.mdx?$/, ""))
    .map((rel) => rel.split(path.sep))
    .map((parts) =>
      parts[parts.length - 1] === "index" ? parts.slice(0, -1) : parts
    )
    .filter((slug) => slug.length > 0)
    .map((slug) => ({ slug }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const rel = resolveFile(slug);
  if (rel === null) {
    return {};
  }
  const { title, description } = readFrontmatter(path.join(CONTENT_ROOT, rel));
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
  const rel = resolveFile(slug);

  if (rel === null) {
    notFound();
  }

  const { title } = readFrontmatter(path.join(CONTENT_ROOT, rel));
  const mod = await import(`@/blogs/${rel}`);
  const Post = mod.default;
  const toc: HeadingItem[] = Array.isArray(mod.toc) ? mod.toc : [];

  return (
    <div className="mx-auto max-w-7xl px-6 py-8 grid gap-10 lg:grid-cols-[1fr_15rem]">
      <article className="min-w-0">
        {title ? <Typography variant="h1">{title}</Typography> : null}
        <Post />
      </article>
      <TableOfContents items={toc} />
    </div>
  );
}

function resolveFile(slug: string[]): string | null {
  const joined = slug.join("/");
  const candidates = [
    `${joined}.md`,
    `${joined}.mdx`,
    `${joined}/index.md`,
    `${joined}/index.mdx`,
  ];

  for (const c of candidates) {
    if (fs.existsSync(path.join(CONTENT_ROOT, c))) {
      return c;
    }
  }
  return null;
}

function walk(dir: string): string[] {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  return entries.flatMap((e) => {
    const full = path.join(dir, e.name);
    return e.isDirectory() ? walk(full) : [full];
  });
}
