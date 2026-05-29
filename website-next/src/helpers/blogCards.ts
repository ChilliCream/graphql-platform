import fs from "node:fs";
import path from "node:path";
import matter from "gray-matter";
import { blogUrlForStem, BLOG_ROOT, listBlogPosts } from "./blogPaths";

export type BlogCard = {
  href: string;
  title: string;
  formattedDate?: string;
  author?: string;
  authorImageUrl?: string;
  featuredImage?: string;
  readTime: number;
};

// A loaded post: the card data plus the fields the listing pages need to
// filter/sort (sort key from the file stem, authored tags).
export type BlogPost = {
  sortKey: string;
  tags: string[];
  card: BlogCard;
};

function formatLongDate(dateStr?: string): string | undefined {
  if (!dateStr) {
    return undefined;
  }
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) {
    return dateStr;
  }
  // Long form like "October 07, 2024".
  return d.toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "2-digit",
  });
}

// Estimate reading time from the markdown body (frontmatter stripped):
// count whitespace-delimited words and assume ~200 words per minute.
function readTimeMinutes(body: string): number {
  const words = body.trim().split(/\s+/).filter(Boolean).length;
  return Math.max(1, Math.round(words / 200));
}

// Featured images are not carried in the /website-next public tree. The
// frontmatter stores a bare filename; resolve it against chillicream.com
// using the post's full stem (YYYY-MM-DD-slug) as the folder — the same
// approach the Header "Latest Blog Post" panel uses. next/image renders
// external URLs because the static export sets images.unoptimized.
function resolveFeaturedImage(
  stem: string,
  featuredImage: unknown
): string | undefined {
  if (typeof featuredImage !== "string" || featuredImage.length === 0) {
    return undefined;
  }
  return `https://chillicream.com/images/blog/${stem}/${featuredImage}`;
}

function toTags(value: unknown): string[] {
  return Array.isArray(value)
    ? value.filter(
        (tag): tag is string => typeof tag === "string" && tag.length > 0
      )
    : [];
}

// Read every post once at build time (static export: no client fetching),
// parsing frontmatter and body in a single pass. Newest first by the date
// encoded in the file stem.
export function loadBlogPosts(): BlogPost[] {
  return listBlogPosts()
    .map(({ stem, parsed, rel }) => {
      const raw = fs.readFileSync(path.join(BLOG_ROOT, rel), "utf-8");
      const { data: fm, content } = matter(raw);
      return {
        sortKey: stem,
        tags: toTags(fm.tags),
        card: {
          href: blogUrlForStem(parsed),
          title: typeof fm.title === "string" ? fm.title : parsed.slug,
          formattedDate: formatLongDate(
            typeof fm.date === "string" ? fm.date : undefined
          ),
          author: typeof fm.author === "string" ? fm.author : undefined,
          authorImageUrl:
            typeof fm.authorImageUrl === "string"
              ? fm.authorImageUrl
              : undefined,
          featuredImage: resolveFeaturedImage(stem, fm.featuredImage),
          readTime: readTimeMinutes(content),
        },
      };
    })
    .sort((a, b) => b.sortKey.localeCompare(a.sortKey));
}
