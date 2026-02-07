import React from "react";

import { getAllDocPages, getDocPageBySlug, getDocsConfig } from "@/lib/docs";
import { compileMdxContent, extractHeadings } from "@/lib/mdx";
import { createMetadata } from "@/lib/metadata";
import { DocPageView } from "@/lib/doc-page-view";
import { notFound } from "next/navigation";

interface PageProps {
  params: Promise<{ slug: string[] }>;
}

export async function generateStaticParams() {
  const pages = getAllDocPages();

  return pages.map((page) => ({
    slug: page.slug.replace(/^\/docs\//, "").split("/"),
  }));
}

export async function generateMetadata({ params }: PageProps) {
  const { slug } = await params;
  const fullSlug = "/docs/" + slug.join("/");
  const page = getDocPageBySlug(fullSlug);

  const title =
    page?.frontmatter?.title || slug[slug.length - 1] || "Documentation";

  return createMetadata({
    title,
    description: page?.frontmatter?.description,
  });
}

export default async function DocPage({ params }: PageProps) {
  const { slug } = await params;
  const fullSlug = "/docs/" + slug.join("/");
  const page = getDocPageBySlug(fullSlug);

  if (!page) {
    return notFound();
  }

  const { mdxSource } = await compileMdxContent(page.content, page.originPath);
  const docsConfig = getDocsConfig();
  const headings = extractHeadings(page.content);

  return (
    <DocPageView
      page={page}
      mdxSource={mdxSource}
      docsConfig={docsConfig}
      slug={slug}
      headings={headings}
    />
  );
}
