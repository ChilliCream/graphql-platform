import React from "react";

import { getPaginatedPosts } from "@/lib/blog";
import { BlogListPage } from "@/lib/blog-list-page";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";

export const metadata = createMetadata({
  title: "Blog",
  pageUrl: `${siteMetadata.siteUrl}/blog/`,
  canonicalUrl: `${siteMetadata.siteUrl}/blog/`,
});

export default function BlogPage() {
  const { posts, totalPages } = getPaginatedPosts(1);

  return (
    <BlogListPage
      posts={posts}
      currentPage={1}
      totalPages={totalPages}
      linkPrefix="/blog"
    />
  );
}
