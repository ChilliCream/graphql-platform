"use client";

import React from "react";
import { MDXRemoteSerializeResult } from "next-mdx-remote";

import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { BlogArticle } from "@/components/articles/blog-article";
import { MdxContent } from "./mdx-content";
import type { BlogPost } from "./blog";

interface LatestPost {
  fields: { slug: string };
  frontmatter: { title: string };
}

interface BlogPostPageProps {
  post: BlogPost;
  mdxSource: MDXRemoteSerializeResult;
  headings: Array<{ depth: number; value: string }>;
  latestPosts: LatestPost[];
}

export function BlogPostPage({
  post,
  mdxSource,
  headings,
  latestPosts,
}: BlogPostPageProps) {
  const formattedDate = new Date(post.date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "2-digit",
  });

  const data = {
    mdx: {
      fields: {
        slug: post.slug,
        readingTime: { text: post.readingTime },
      },
      frontmatter: {
        featuredImage: post.featuredImage,
        featuredVideoId: post.featuredVideoId,
        path: post.path,
        title: post.title,
        description: post.description,
        tags: post.tags,
        author: post.author,
        authorImageUrl: post.authorImageUrl,
        authorUrl: post.authorUrl,
        date: formattedDate,
      },
      headings,
    },
    latestPosts: {
      posts: latestPosts,
    },
  };

  return (
    <SiteLayout>
      <SEO
        title={post.title}
        description={post.description}
        isArticle
        imageUrl={post.featuredImage}
      />
      <BlogArticle data={data} content={<MdxContent source={mdxSource} />} />
    </SiteLayout>
  );
}
