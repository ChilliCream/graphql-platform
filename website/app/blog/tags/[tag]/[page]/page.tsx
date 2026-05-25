import React from "react";

import { getAllTags, getPostsByTag, getPostsPerPage } from "@/lib/blog";
import { BlogListPage } from "@/lib/blog-list-page";
import { createMetadata } from "@/lib/metadata";

interface PageProps {
  params: Promise<{ tag: string; page: string }>;
}

export async function generateStaticParams() {
  const tags = getAllTags();
  const postsPerPage = getPostsPerPage();
  const params: { tag: string; page: string }[] = [];

  for (const tag of tags) {
    const { totalPages } = getPostsByTag(tag, 1);
    for (let i = 2; i <= totalPages; i++) {
      params.push({ tag, page: String(i) });
    }
  }

  return params;
}

export async function generateMetadata({ params }: PageProps) {
  const { tag, page } = await params;
  return createMetadata({ title: `Blog - ${tag} - Page ${page}` });
}

export default async function BlogTagPaginatedPage({ params }: PageProps) {
  const { tag, page } = await params;
  const pageNum = parseInt(page, 10);
  const { posts, totalPages } = getPostsByTag(tag, pageNum);

  return (
    <BlogListPage
      posts={posts}
      currentPage={pageNum}
      totalPages={totalPages}
      linkPrefix={`/blog/tags/${tag}`}
      tag={tag}
    />
  );
}
