"use client";

import React from "react";

import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AllBlogPosts } from "@/components/widgets";
import type { BlogPost } from "./blog";

interface BlogListPageProps {
  posts: BlogPost[];
  currentPage: number;
  totalPages: number;
  linkPrefix: string;
  tag?: string;
}

export function BlogListPage({
  posts,
  currentPage,
  totalPages,
  linkPrefix,
  tag,
}: BlogListPageProps) {
  const title = tag ? `Blog - ${tag}` : "Blog";
  const description = tag
    ? `Posts tagged with "${tag}"`
    : "The latest news about ChilliCream and our products";

  const data = {
    edges: posts.map((post) => ({
      node: {
        id: post.slug,
        frontmatter: {
          featuredImage: post.featuredImage || undefined,
          path: post.slug,
          title: post.title,
          author: post.author || undefined,
          authorImageUrl: undefined,
          date: post.date,
        },
        fields: {
          readingTime: {
            text: post.readingTime,
          },
        },
      },
    })),
  };

  return (
    <SiteLayout>
      <SEO title={title} />
      <AllBlogPosts
        data={data}
        description={description}
        currentPage={currentPage}
        totalPages={totalPages}
        basePath={linkPrefix}
      />
    </SiteLayout>
  );
}
