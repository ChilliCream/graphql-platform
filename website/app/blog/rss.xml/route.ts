import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { SITE_URL } from "@/src/helpers/siteUrl";
import { getShareImageSrc } from "@/src/image-optimization/manifest";

export const dynamic = "force-static";

const FEED_TITLE = "ChilliCream Blog";
const FEED_DESCRIPTION =
  "Announcements, deep dives, and how-tos from the ChilliCream GraphQL Platform team.";

function escape(text: string): string {
  return text
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&apos;");
}

function imageMimeType(src: string): string {
  if (/\.jpe?g$/i.test(src)) {
    return "image/jpeg";
  }
  if (/\.webp$/i.test(src)) {
    return "image/webp";
  }
  return "image/png";
}

function asRfc822(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return new Date().toUTCString();
  }
  return d.toUTCString();
}

export function GET() {
  const posts = listBlogPostSummaries();
  const buildDate = posts[0]?.date ?? new Date().toISOString();

  const items = posts
    .map((post) => {
      const url = `${SITE_URL}${post.href}`;
      const description = post.description ?? "";
      const shareImage = post.featuredImage
        ? getShareImageSrc(post.featuredImage)
        : null;
      const enclosure = shareImage
        ? `<enclosure url="${SITE_URL}${escape(shareImage)}" type="${imageMimeType(shareImage)}" />`
        : "";
      const categories = post.tags
        .map((tag) => `<category>${escape(tag)}</category>`)
        .join("");
      return `    <item>
      <title>${escape(post.title)}</title>
      <link>${escape(url)}</link>
      <guid isPermaLink="true">${escape(url)}</guid>
      <pubDate>${asRfc822(post.date)}</pubDate>
      ${post.author ? `<author>${escape(post.author)}</author>` : ""}
      ${categories}
      ${enclosure}
      <description>${escape(description)}</description>
    </item>`;
    })
    .join("\n");

  const body = `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
  <channel>
    <title>${escape(FEED_TITLE)}</title>
    <link>${SITE_URL}/blog</link>
    <atom:link href="${SITE_URL}/blog/rss.xml" rel="self" type="application/rss+xml" />
    <description>${escape(FEED_DESCRIPTION)}</description>
    <language>en-us</language>
    <lastBuildDate>${asRfc822(buildDate)}</lastBuildDate>
${items}
  </channel>
</rss>
`;

  return new Response(body, {
    headers: {
      "Content-Type": "application/rss+xml; charset=utf-8",
    },
  });
}
