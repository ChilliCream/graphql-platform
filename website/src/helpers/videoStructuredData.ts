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
  "Hands-on ChilliCream TV episodes on Hot Chocolate, DataLoader, and building GraphQL APIs in .NET, each with its example source code.";

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
    uploadDate: video.date,
    embedUrl: `https://www.youtube.com/embed/${video.youtubeId}`,
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
        uploadDate: video.date,
      },
    })),
  };
}
