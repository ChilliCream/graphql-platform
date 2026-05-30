import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { DocPageMeta } from "@/src/design-system/DocPageMeta";
import { EditOnGitHub } from "@/src/design-system/EditOnGitHub";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { compileDoc } from "@/src/helpers/compileDoc";
import {
  CONTENT_ROOT,
  encodeDocId,
  listDocSlugs,
  resolveFile,
} from "@/src/helpers/docsParams";
import { getGitMetadata } from "@/src/helpers/gitMetadata";
import { githubEditUrl } from "@/src/helpers/githubEditUrl";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";

type Params = {
  slug: string[];
};

type PageProps = {
  params: Promise<Params>;
};

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  return listDocSlugs().map((slug) => ({ slug }));
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

  const id = encodeDocId(slug);
  const ogImage = {
    url: toAbsoluteUrl(`/docs-og/${id}/opengraph-image`),
    width: 1200,
    height: 630,
    type: "image/png",
    alt: title ? `${title} documentation` : "ChilliCream documentation",
  };

  return {
    title,
    description,
    openGraph: {
      type: "article",
      title,
      description,
      images: [ogImage],
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images: [ogImage],
    },
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
