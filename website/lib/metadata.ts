import type { Metadata } from "next";

import { siteMetadata } from "./site-config";

interface GenerateMetadataOptions {
  title: string;
  description?: string;
  imageUrl?: string;
  isArticle?: boolean;
  canonicalUrl?: string;
  pageUrl?: string;
  noIndex?: boolean;
}

export function createMetadata({
  title,
  description,
  imageUrl,
  isArticle,
  canonicalUrl,
  pageUrl,
  noIndex,
}: GenerateMetadataOptions): Metadata {
  const metaDescription = description || siteMetadata.description;
  const metaAuthor = `@${siteMetadata.author}`;
  const metaImageUrl = imageUrl
    ? `${siteMetadata.siteUrl}${imageUrl}`
    : `${siteMetadata.siteUrl}/favicon-32x32.png`;
  const metaType = isArticle ? "article" : "website";
  const resolvedUrl = pageUrl || siteMetadata.siteUrl;

  return {
    title,
    description: metaDescription,
    ...(canonicalUrl && {
      alternates: { canonical: canonicalUrl },
    }),
    ...(noIndex && {
      robots: { index: false, follow: true },
    }),
    openGraph: {
      url: resolvedUrl,
      title,
      description: metaDescription,
      type: metaType as "website" | "article",
      images: [{ url: metaImageUrl }],
    },
    twitter: {
      card: "summary_large_image",
      title,
      site: metaAuthor,
      creator: metaAuthor,
      images: [metaImageUrl],
      description: description || undefined,
    },
  };
}
