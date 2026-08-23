import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { LearnVideoDetail } from "@/src/components/learn/LearnVideoDetail";
import { getOptimizedImage } from "@/src/image-optimization/manifest";
import { productLabel } from "@/src/data/learn/facets";
import { LEARN_SUMMARIES, VIDEO_ITEMS } from "@/src/data/learn/content";
import type { LearnItemSummary, VideoItem } from "@/src/data/learn/types";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL, toAbsoluteUrl } from "@/src/helpers/siteUrl";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

/** Related items shown below a video, capped per learn-editorial.md section 20.5. */
const MAX_RELATED = 3;

export const dynamicParams = false;

/**
 * Only videos with a `youtubeId` get a native detail page; the two legacy
 * entries seeded before the TV migration carry no id (or long description)
 * and keep linking straight to YouTube via `learnItemHref`.
 */
function findVideo(slug: string): (VideoItem & { readonly youtubeId: string }) | undefined {
  return VIDEO_ITEMS.find((video) => video.slug === slug && video.youtubeId) as
    | (VideoItem & { readonly youtubeId: string })
    | undefined;
}

export function generateStaticParams(): { slug: string }[] {
  return VIDEO_ITEMS.filter((video) => video.youtubeId).map((video) => ({ slug: video.slug }));
}

/** Self-hosted optimized poster when built, else the external `hqdefault` thumbnail (matches `YouTubePoster`'s resolution). */
function posterUrl(youtubeId: string): string {
  const remote = `https://i.ytimg.com/vi/${youtubeId}/maxresdefault.jpg`;
  const opt = getOptimizedImage(remote);
  return toAbsoluteUrl(opt?.fallbackSrc ?? `https://i.ytimg.com/vi/${youtubeId}/hqdefault.jpg`);
}

/** `"51:49"` / `"1:02:15"` mm:ss or h:mm:ss duration to ISO 8601 (`PT51M49S`). */
function toIsoDuration(duration: string): string {
  const parts = duration.split(":").map(Number);
  const [hours, minutes, seconds] = parts.length === 3 ? parts : parts.length === 2 ? [0, ...parts] : [0, 0, ...parts];
  const body = `${hours ? `${hours}H` : ""}${minutes ? `${minutes}M` : ""}${seconds ? `${seconds}S` : ""}`;
  return `PT${body || "0S"}`;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const video = findVideo(slug);
  if (!video) {
    return {};
  }
  const base = pageMetadata({
    title: video.title,
    description: video.tagline,
    path: `/learn/videos/${video.slug}`,
    keywords: ["GraphQL video", ...video.products.map(productLabel)],
  });
  const image = posterUrl(video.youtubeId);
  return {
    ...base,
    openGraph: { ...base.openGraph, images: [image] },
    twitter: { ...base.twitter, images: [image] },
  };
}

function structuredData(video: VideoItem & { readonly youtubeId: string }) {
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "BreadcrumbList",
        itemListElement: [
          { "@type": "ListItem", position: 1, name: "Learn", item: `${SITE_URL}/learn` },
          { "@type": "ListItem", position: 2, name: "Videos", item: `${SITE_URL}/learn/browse?type=video` },
          { "@type": "ListItem", position: 3, name: video.title },
        ],
      },
      {
        "@type": "VideoObject",
        name: video.title,
        description: video.tagline,
        thumbnailUrl: posterUrl(video.youtubeId),
        ...(video.publishedAt ? { uploadDate: video.publishedAt } : {}),
        ...(video.duration ? { duration: toIsoDuration(video.duration) } : {}),
        embedUrl: `https://www.youtube-nocookie.com/embed/${video.youtubeId}`,
        url: `${SITE_URL}/learn/videos/${video.slug}`,
      },
    ],
  };
}

/**
 * Related videos for a video (learn-editorial.md section 20.5): other videos
 * sharing a product, newest first; padded to {@link MAX_RELATED} with the
 * newest remaining videos, then with non-video items sharing a product
 * (templates first). The current video is always excluded.
 */
function findRelated(video: VideoItem): readonly LearnItemSummary[] {
  const byNewest = (a: VideoItem, b: VideoItem) => (b.publishedAt ?? "").localeCompare(a.publishedAt ?? "");
  const otherVideos = VIDEO_ITEMS.filter((v) => v.slug !== video.slug);
  const usedSlugs = new Set([video.slug]);

  const sameProduct = otherVideos.filter((v) => v.products.some((p) => video.products.includes(p))).sort(byNewest);
  const primary = sameProduct.slice(0, MAX_RELATED);
  primary.forEach((v) => usedSlugs.add(v.slug));

  let result: LearnItemSummary[] = [...primary];
  if (result.length < MAX_RELATED) {
    const remainingVideos = otherVideos.filter((v) => !usedSlugs.has(v.slug)).sort(byNewest);
    const pad = remainingVideos.slice(0, MAX_RELATED - result.length);
    pad.forEach((v) => usedSlugs.add(v.slug));
    result = [...result, ...pad];
  }
  if (result.length < MAX_RELATED) {
    const nonVideo = LEARN_SUMMARIES.filter(
      (item) =>
        item.type !== "video" && !usedSlugs.has(item.slug) && item.products.some((p) => video.products.includes(p)),
    );
    const templatesFirst = [
      ...nonVideo.filter((i) => i.type === "template"),
      ...nonVideo.filter((i) => i.type !== "template"),
    ];
    result = [...result, ...templatesFirst].slice(0, MAX_RELATED);
  }
  return result;
}

export default async function VideoPage({ params }: PageProps) {
  const { slug } = await params;
  const video = findVideo(slug);
  if (!video) {
    notFound();
  }
  const related = findRelated(video);
  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData(video)) }} />
      <LearnVideoDetail video={video} related={related} />
    </>
  );
}
