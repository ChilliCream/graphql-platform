import { authorPersonId } from "@/src/data/authors";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
  type JsonLdNode,
} from "@/src/helpers/structuredData";
import { videoPosterUrl, type VideoSummary } from "@/src/helpers/videos";

export const VIDEOS_DESCRIPTION =
  "Hands-on ChilliCream TV episodes on GraphQL and .NET with Hot Chocolate, Fusion, Nitro, Mocha, and Strawberry Shake, from the team that builds them.";

/** The video's running time as an ISO 8601 duration, e.g. `PT0H12M18S`. */
function isoDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return `PT${hours}H${minutes}M${seconds % 60}S`;
}

export function videoObjectId(video: VideoSummary): string {
  return schemaId(video.href, "video");
}

/** The schema.org `VideoObject` describing one video page. */
export function createVideoObjectNode(video: VideoSummary): JsonLdNode {
  return {
    "@type": "VideoObject",
    "@id": videoObjectId(video),
    url: toAbsoluteUrl(video.href),
    name: video.title,
    description: video.description,
    thumbnailUrl: [toAbsoluteUrl(videoPosterUrl(video.youtubeId))],
    uploadDate: video.uploadDate,
    duration: isoDuration(video.duration),
    embedUrl: `https://www.youtube-nocookie.com/embed/${video.youtubeId}`,
    publisher: schemaRef(ORGANIZATION_ID),
    ...(video.author
      ? { author: schemaRef(authorPersonId(video.author)) }
      : {}),
    ...(video.tags.length > 0 ? { keywords: video.tags } : {}),
    inLanguage: "en",
    isAccessibleForFree: true,
  };
}

export function createVideoItemListNode(
  path: string,
  name: string,
  videos: readonly VideoSummary[],
): JsonLdNode {
  return {
    "@type": "ItemList",
    "@id": schemaId(path, "videos"),
    name,
    numberOfItems: videos.length,
    itemListOrder: "https://schema.org/ItemListOrderDescending",
    itemListElement: videos.map((video, index) => ({
      "@type": "ListItem",
      position: index + 1,
      url: toAbsoluteUrl(video.href),
      item: {
        "@type": "VideoObject",
        "@id": videoObjectId(video),
        url: toAbsoluteUrl(video.href),
        name: video.title,
        description: video.description,
        thumbnailUrl: [toAbsoluteUrl(videoPosterUrl(video.youtubeId))],
        uploadDate: video.uploadDate,
        duration: isoDuration(video.duration),
      },
    })),
  };
}
