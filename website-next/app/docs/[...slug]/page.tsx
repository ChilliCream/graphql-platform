import fs from "node:fs";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Sidebar } from "@/src/design-system/Sidebar";
import { SidebarDrawer } from "@/src/design-system/SidebarDrawer";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import type { HeadingItem } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { buildContentTree } from "@/src/helpers/buildContentTree";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";

const CONTENT_ROOT = path.join(process.cwd(), "docs");

export const dynamicParams = false;

export function generateStaticParams(): { slug: string[] }[] {
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

export default async function DocPage({
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
  const mod = await import(`@/docs/${rel.slice(0, -3)}.md`);
  const Doc = mod.default;
  const toc: HeadingItem[] = Array.isArray(mod.toc) ? mod.toc : [];
  const tree = buildContentTree(CONTENT_ROOT, "/docs");

  return (
    <div className="grid grid-cols-1 lg:grid-cols-[20rem_1fr] 2xl:grid-cols-[20rem_1fr_20rem]">
      <SidebarDrawer>
        <Sidebar tree={tree} />
      </SidebarDrawer>
      <main className="min-w-0 px-5 py-8 sm:px-12">
        <article className="mx-auto max-w-5xl">
          {title ? <Typography variant="h1">{title}</Typography> : null}
          <Doc />
        </article>
      </main>
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
