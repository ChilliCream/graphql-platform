import fs from "node:fs";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { DocPageMeta } from "@/src/design-system/DocPageMeta";
import { EditOnGitHub } from "@/src/design-system/EditOnGitHub";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { compileDoc } from "@/src/helpers/compileDoc";
import { getGitMetadata } from "@/src/helpers/gitMetadata";
import { githubEditUrl } from "@/src/helpers/githubEditUrl";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";

const CONTENT_ROOT = path.join(process.cwd(), "content/docs");

type Params = {
  slug: string[];
};

type PageProps = {
  params: Promise<Params>;
};

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const params = walk(CONTENT_ROOT)
    .filter((f) => /\.mdx?$/.test(f))
    .map((f) => path.relative(CONTENT_ROOT, f).replace(/\.mdx?$/, ""))
    .map((rel) => rel.split(path.sep))
    .map((parts) =>
      parts[parts.length - 1] === "index" ? parts.slice(0, -1) : parts,
    )
    .filter((slug) => slug.length > 0)
    .map((slug) => ({ slug }));

  // output: export requires at least one prerendered path; placeholder
  // renders 404 via notFound() when no content is present.
  return params.length > 0 ? params : [{ slug: ["__empty__"] }];
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
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

export default async function DocPage({ params }: PageProps) {
  const { slug } = await params;
  const rel = resolveFile(slug);

  if (rel === null) {
    notFound();
  }

  const absolutePath = path.join(CONTENT_ROOT, rel);
  const { content, frontmatter, toc } = await compileDoc(absolutePath);
  const gitMeta = await getGitMetadata(absolutePath);

  return (
    <div className="grid min-h-[calc(100vh-72px)] grid-cols-1 2xl:grid-cols-[1fr_20rem]">
      <main className="min-w-0 px-5 py-8 sm:px-12">
        <article className="mx-auto max-w-5xl">
          {frontmatter.title ? (
            <Typography variant="h1">{frontmatter.title}</Typography>
          ) : null}

          {content}

          <EditOnGitHub href={githubEditUrl(`content/docs/${rel}`)} />

          <DocPageMeta
            isoDate={gitMeta.isoDate}
            displayDate={gitMeta.displayDate}
            author={gitMeta.author}
          />
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
