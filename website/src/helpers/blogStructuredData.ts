import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { authorPersonId } from "@/src/data/authors";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";
import type { JsonLdNode } from "@/src/helpers/structuredData";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import { getShareImageSrc } from "@/src/image-optimization/manifest";

export const BLOG_ID = schemaId("/blog", "blog");
export const BLOG_DESCRIPTION =
  "The ChilliCream blog: announcements, deep dives, and how-tos.";

export function createBlogNode(
  description: string = BLOG_DESCRIPTION,
): JsonLdNode {
  return {
    "@type": "Blog",
    "@id": BLOG_ID,
    url: toAbsoluteUrl("/blog"),
    name: "ChilliCream Blog",
    description,
    publisher: schemaRef(ORGANIZATION_ID),
    inLanguage: "en",
  };
}

export function createBlogItemListNode(
  path: string,
  name: string,
  posts: readonly BlogPostSummary[],
  startPosition = 1,
): JsonLdNode {
  return {
    "@type": "ItemList",
    "@id": schemaId(path, "posts"),
    name,
    numberOfItems: posts.length,
    itemListOrder: "https://schema.org/ItemListOrderDescending",
    itemListElement: posts.map((post, index) => ({
      "@type": "ListItem",
      position: startPosition + index,
      url: toAbsoluteUrl(post.href),
      item: {
        "@type": "BlogPosting",
        "@id": schemaId(post.href, "article"),
        url: toAbsoluteUrl(post.href),
        headline: post.title,
        ...(post.description ? { description: post.description } : {}),
        datePublished: post.date,
        ...(post.category ? { articleSection: post.category } : {}),
        ...(post.featuredImage
          ? {
              image: toAbsoluteUrl(getShareImageSrc(post.featuredImage)),
            }
          : {}),
        publisher: schemaRef(ORGANIZATION_ID),
        ...(post.authorProfile
          ? { author: schemaRef(authorPersonId(post.authorProfile)) }
          : {}),
        isPartOf: schemaRef(BLOG_ID),
        inLanguage: "en",
        isAccessibleForFree: true,
      },
    })),
  };
}
