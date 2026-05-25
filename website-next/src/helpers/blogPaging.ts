import type { BlogPostSummary } from "./blogPosts";

export const POSTS_PER_PAGE = 9;

export type PageSlice = {
  posts: BlogPostSummary[];
  currentPage: number;
  totalPages: number;
};

export function paginate(
  posts: BlogPostSummary[],
  page: number,
): PageSlice | null {
  const totalPages = Math.max(1, Math.ceil(posts.length / POSTS_PER_PAGE));
  if (page < 1 || page > totalPages) {
    return null;
  }
  const start = (page - 1) * POSTS_PER_PAGE;
  return {
    posts: posts.slice(start, start + POSTS_PER_PAGE),
    currentPage: page,
    totalPages,
  };
}

export function listTags(posts: BlogPostSummary[]): string[] {
  const set = new Set<string>();
  for (const post of posts) {
    for (const tag of post.tags) {
      set.add(tag);
    }
  }
  return [...set].sort();
}

export function postsForTag(
  posts: BlogPostSummary[],
  tag: string,
): BlogPostSummary[] {
  return posts.filter((p) => p.tags.includes(tag));
}
