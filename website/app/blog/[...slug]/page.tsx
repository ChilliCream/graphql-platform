import React from "react";

import {
  getAllBlogPosts,
  getBlogPostBySlug,
  getLatestPostsForNav,
  getPaginatedPosts,
  getPostsPerPage,
} from "@/lib/blog";
import { compileMdxContent, extractHeadings } from "@/lib/mdx";
import { createMetadata } from "@/lib/metadata";
import { BlogPostPage } from "@/lib/blog-post-page";
import { BlogListPage } from "@/lib/blog-list-page";
import { notFound } from "next/navigation";

interface PageProps {
  params: Promise<{ slug: string[] }>;
}

export async function generateStaticParams() {
  const posts = getAllBlogPosts();
  const postsPerPage = getPostsPerPage();
  const totalPages = Math.ceil(posts.length / postsPerPage);

  const params: { slug: string[] }[] = [];

  // Pagination pages (page 2+)
  for (let i = 2; i <= totalPages; i++) {
    params.push({ slug: [String(i)] });
  }

  // Blog post pages (year/month/day/slug)
  for (const post of posts) {
    const parts = post.slug.replace(/^\/blog\//, "").split("/");
    params.push({ slug: parts });
  }

  return params;
}

export async function generateMetadata({ params }: PageProps) {
  const { slug } = await params;

  // Pagination page
  if (slug.length === 1 && /^\d+$/.test(slug[0])) {
    return createMetadata({ title: `Blog - Page ${slug[0]}` });
  }

  // Blog post (year/month/day/slug)
  if (slug.length === 4) {
    const postSlug = `/blog/${slug.join("/")}`;
    const post = getBlogPostBySlug(postSlug);

    return createMetadata({
      title: post?.title || "Blog Post",
      description: post?.description,
      isArticle: true,
      imageUrl: post?.featuredImage,
    });
  }

  return createMetadata({ title: "Blog" });
}

export default async function BlogCatchAllPage({ params }: PageProps) {
  const { slug } = await params;

  // Pagination page (e.g., /blog/2, /blog/3)
  if (slug.length === 1 && /^\d+$/.test(slug[0])) {
    const pageNum = parseInt(slug[0], 10);
    const { posts, totalPages } = getPaginatedPosts(pageNum);

    return (
      <BlogListPage
        posts={posts}
        currentPage={pageNum}
        totalPages={totalPages}
        linkPrefix="/blog"
      />
    );
  }

  // Blog post page (e.g., /blog/2024/01/15/my-post)
  if (slug.length === 4) {
    const postSlug = `/blog/${slug.join("/")}`;
    const post = getBlogPostBySlug(postSlug);

    if (!post) {
      return notFound();
    }

    const { mdxSource } = await compileMdxContent(post.content);
    const headings = extractHeadings(post.content);
    const latestPosts = getLatestPostsForNav();

    return (
      <BlogPostPage
        post={post}
        mdxSource={mdxSource}
        headings={headings}
        latestPosts={latestPosts}
      />
    );
  }

  return notFound();
}
