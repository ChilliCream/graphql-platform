import React from "react";

import { getAllTags, getPostsByTag } from "@/lib/blog";
import { BlogListPage } from "@/lib/blog-list-page";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";

interface PageProps {
  params: Promise<{ tag: string }>;
}

export async function generateStaticParams() {
  return getAllTags().map((tag) => ({ tag }));
}

export async function generateMetadata({ params }: PageProps) {
  const { tag } = await params;
  const tagUrl = `${siteMetadata.siteUrl}/blog/tags/${tag}/`;
  return createMetadata({
    title: `Blog - ${tag}`,
    pageUrl: tagUrl,
    canonicalUrl: tagUrl,
  });
}

export default async function BlogTagPage({ params }: PageProps) {
  const { tag } = await params;
  const { posts, totalPages } = getPostsByTag(tag, 1);

  return (
    <BlogListPage
      posts={posts}
      currentPage={1}
      totalPages={totalPages}
      linkPrefix={`/blog/tags/${tag}`}
      tag={tag}
    />
  );
}
