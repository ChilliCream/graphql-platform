import React from "react";

import {
  getAllDocPages,
  getDocPageBySlug,
  getDocsConfig,
  getProductInfo,
} from "@/lib/docs";
import { compileMdxContent, extractHeadings } from "@/lib/mdx";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";
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

  const product = page?.product;
  const version = page?.version;
  const productInfo = product ? getProductInfo(product) : undefined;
  const latestVersion = productInfo?.latestStableVersion;
  const isLatest = !version || !latestVersion || version === latestVersion;

  const pageUrl = `${siteMetadata.siteUrl}${fullSlug}/`;
  let canonicalUrl: string;

  if (isLatest) {
    canonicalUrl = pageUrl;
  } else {
    const restOfPath = slug.slice(2).join("/");
    canonicalUrl = restOfPath
      ? `${siteMetadata.siteUrl}/docs/${product}/${latestVersion}/${restOfPath}/`
      : `${siteMetadata.siteUrl}/docs/${product}/${latestVersion}/`;
  }

  return createMetadata({
    title,
    description: page?.frontmatter?.description,
    canonicalUrl,
    pageUrl,
    noIndex: !isLatest,
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
