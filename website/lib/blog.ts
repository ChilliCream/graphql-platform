import path from "path";
import readingTime from "reading-time";

import { getContentDir, getFilesRecursively, readMarkdownFile } from "./content";

export interface BlogPost {
  slug: string;
  title: string;
  description?: string;
  author: string;
  authorUrl?: string;
  authorImageUrl?: string;
  date: string;
  tags: string[];
  featuredImage?: string;
  featuredVideoId?: string;
  content: string;
  readingTime: string;
  path: string;
}

const BLOG_DIR = getContentDir("blog");
const POSTS_PER_PAGE = 21;

let _cachedPosts: BlogPost[] | null = null;

export function getAllBlogPosts(): BlogPost[] {
  if (_cachedPosts) return _cachedPosts;

  const files = getFilesRecursively(BLOG_DIR, ".md");
  const posts: BlogPost[] = [];

  for (const file of files) {
    const { frontmatter, content } = readMarkdownFile(file);

    if (!frontmatter.path || !frontmatter.path.startsWith("/blog/")) continue;

    // Resolve featured image path relative to blog post
    let featuredImage: string | undefined;
    if (frontmatter.featuredImage) {
      const imgPath = frontmatter.featuredImage;
      if (typeof imgPath === "string") {
        const dir = path.dirname(file);
        const absImgPath = path.resolve(dir, imgPath);
        const relToSrc = path.relative(getContentDir(), absImgPath);
        featuredImage = `/images/${relToSrc}`;
      }
    }

    posts.push({
      slug: frontmatter.path,
      title: frontmatter.title || "",
      description: frontmatter.description || "",
      author: frontmatter.author || "Unknown",
      authorUrl: frontmatter.authorUrl || "",
      authorImageUrl: frontmatter.authorImageUrl || "",
      date: frontmatter.date
        ? new Date(frontmatter.date).toISOString()
        : "",
      tags: frontmatter.tags || [],
      featuredImage,
      featuredVideoId: frontmatter.featuredVideoId || undefined,
      content,
      readingTime: readingTime(content).text,
      path: frontmatter.path,
    });
  }

  // Sort by date descending
  posts.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

  _cachedPosts = posts;
  return posts;
}

export function getBlogPostBySlug(slug: string): BlogPost | undefined {
  return getAllBlogPosts().find((p) => p.slug === slug);
}

export function getPaginatedPosts(page: number) {
  const posts = getAllBlogPosts();
  const totalPages = Math.ceil(posts.length / POSTS_PER_PAGE);
  const start = (page - 1) * POSTS_PER_PAGE;
  const paginatedPosts = posts.slice(start, start + POSTS_PER_PAGE);

  return {
    posts: paginatedPosts,
    currentPage: page,
    totalPages,
    totalPosts: posts.length,
  };
}

export function getAllTags(): string[] {
  const posts = getAllBlogPosts();
  const tagSet = new Set<string>();
  for (const post of posts) {
    for (const tag of post.tags) {
      tagSet.add(tag);
    }
  }
  return Array.from(tagSet).sort();
}

export function getPostsByTag(tag: string, page: number) {
  const allPosts = getAllBlogPosts().filter((p) => p.tags.includes(tag));
  const totalPages = Math.ceil(allPosts.length / POSTS_PER_PAGE);
  const start = (page - 1) * POSTS_PER_PAGE;
  const paginatedPosts = allPosts.slice(start, start + POSTS_PER_PAGE);

  return {
    posts: paginatedPosts,
    currentPage: page,
    totalPages,
    totalPosts: allPosts.length,
    tag,
  };
}

export function getPostsPerPage() {
  return POSTS_PER_PAGE;
}

export function getLatestPostsForNav(count = 10) {
  return getAllBlogPosts()
    .slice(0, count)
    .map((post) => ({
      fields: { slug: post.slug },
      frontmatter: { title: post.title },
    }));
}

export function getRecentNitroBlogPostTeasers(count = 3) {
  const posts = getAllBlogPosts()
    .filter((p) => p.tags.some((t) => t.toLowerCase() === "nitro"))
    .slice(0, count);

  return posts.map((post) => ({
    id: post.slug,
    frontmatter: {
      featuredImage: post.featuredImage,
      path: post.path,
      title: post.title,
      author: post.author,
      authorImageUrl: post.authorImageUrl,
      date: post.date
        ? new Date(post.date).toLocaleDateString("en-US", {
            year: "numeric",
            month: "long",
            day: "2-digit",
          })
        : "",
    },
    fields: {
      readingTime: { text: post.readingTime },
    },
  }));
}

export function getLatestBlogPostForHeader() {
  const posts = getAllBlogPosts();
  if (posts.length === 0) return null;

  const post = posts[0];
  return {
    title: post.title,
    path: post.path,
    date: post.date
      ? new Date(post.date).toLocaleDateString("en-US", {
          year: "numeric",
          month: "long",
          day: "2-digit",
        })
      : "",
    readingTime: post.readingTime,
    featuredImage: post.featuredImage,
  };
}

export function getRecentBlogPostTeasers(count = 3) {
  const posts = getAllBlogPosts().slice(0, count);

  return posts.map((post) => ({
    id: post.slug,
    frontmatter: {
      featuredImage: post.featuredImage,
      path: post.path,
      title: post.title,
      author: post.author,
      authorImageUrl: post.authorImageUrl,
      date: post.date
        ? new Date(post.date).toLocaleDateString("en-US", {
            year: "numeric",
            month: "long",
            day: "2-digit",
          })
        : "",
    },
    fields: {
      readingTime: { text: post.readingTime },
    },
  }));
}
