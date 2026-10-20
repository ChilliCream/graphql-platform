import { PageStructuredData } from "@/src/components/PageStructuredData";
import { SectionHeading } from "@/src/components/SectionHeading";
import { VideoCard } from "@/src/components/videos/VideoCard";
import { VideoCatalog } from "@/src/components/videos/VideoCatalog";
import { VideosHero } from "@/src/components/videos/VideosHero";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaRef } from "@/src/helpers/structuredData";
import {
  createVideoItemListNode,
  VIDEOS_DESCRIPTION,
} from "@/src/helpers/videoStructuredData";
import {
  listVideos,
  listVideoTags,
  topicLabel,
  VIDEOS_PATH,
} from "@/src/helpers/videos";

const PAGE = {
  title: "Videos",
  description: VIDEOS_DESCRIPTION,
  path: VIDEOS_PATH,
} as const;

export const metadata = pageMetadata(PAGE);

export default function VideosIndex() {
  const videos = listVideos();
  const topics = listVideoTags(videos);
  const videoList = createVideoItemListNode(
    PAGE.path,
    "ChilliCream TV episodes",
    videos,
  );

  return (
    <div className="px-5 py-8 sm:px-12">
      <PageStructuredData
        {...PAGE}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Videos" }]}
        mainEntity={schemaRef(videoList["@id"]!)}
        additionalNodes={[videoList]}
      />
      <div className="mx-auto max-w-7xl">
        <VideosHero videos={videos} browseHref="#episodes" />

        <section
          id="episodes"
          aria-labelledby="episodes-title"
          className="scroll-mt-8 pt-16 pb-8"
        >
          <div className="mb-10">
            <SectionHeading titleId="episodes-title" title="All episodes" />
          </div>
          {videos.length > 0 ? (
            <VideoCatalog
              topicLabels={Object.fromEntries(
                topics.map((topic) => [topic, topicLabel(topic)]),
              )}
              items={videos.map((video) => ({
                key: video.stem,
                topics: video.tags,
                year: video.date.slice(0, 4),
                presenter: video.author?.name ?? null,
                hasExample: video.example !== null,
                searchText: [
                  video.title,
                  video.description,
                  ...video.tags.map(topicLabel),
                ]
                  .join(" ")
                  .toLowerCase(),
                card: <VideoCard video={video} />,
              }))}
            />
          ) : (
            <p className="text-cc-ink-dim">No videos yet. Check back soon.</p>
          )}
        </section>
      </div>
    </div>
  );
}
