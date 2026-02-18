import React from "react";

import { getPaginatedPosts } from "@/lib/blog";
import { BlogListPage } from "@/lib/blog-list-page";
import { createMetadata } from "@/lib/metadata";

export const metadata = createMetadata({ title: "Blog" });

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
