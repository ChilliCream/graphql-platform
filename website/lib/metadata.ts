import type { Metadata } from "next";

import { siteMetadata } from "./site-config";

interface GenerateMetadataOptions {
  title: string;
  description?: string;
  imageUrl?: string;
  isArticle?: boolean;
}

export function createMetadata({
  title,
  description,
  imageUrl,
  isArticle,
}: GenerateMetadataOptions): Metadata {
  const metaDescription = description || siteMetadata.description;
  const metaAuthor = `@${siteMetadata.author}`;
  const metaImageUrl = imageUrl
    ? `${siteMetadata.siteUrl}${imageUrl}`
    : `${siteMetadata.siteUrl}/favicon-32x32.png`;
  const metaType = isArticle ? "article" : "website";

  return {
    title,
    description: metaDescription,
    openGraph: {
      url: siteMetadata.siteUrl,
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
