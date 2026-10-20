import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { MoreVideos } from "@/src/components/videos/MoreVideos";
import { VideoByline } from "@/src/components/videos/VideoByline";
import { YouTubeVideo } from "@/src/components/YouTubeVideo";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { TagList } from "@/src/design-system/TagList";
import { createAuthorPersonNode } from "@/src/data/authors";
import { compileDoc } from "@/src/helpers/compileDoc";
import { getLastModifiedFromGit } from "@/src/helpers/gitMetadata";
import { SITE_NAME, TWITTER_HANDLE } from "@/src/helpers/site";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import { schemaRef } from "@/src/helpers/structuredData";
import {
  createVideoObjectNode,
  videoObjectId,
} from "@/src/helpers/videoStructuredData";
import {
  findRelatedVideos,
  formatEpisode,
  listVideos,
  topicLabel,
  VIDEOS_PATH,
  videoPosterUrl,
  youtubeWatchUrl,
} from "@/src/helpers/videos";
import { ArrowLeftIcon } from "@/src/icons/ArrowLeft";
import { BranchIcon } from "@/src/icons/BranchIcon";
import { YouTubeIcon } from "@/src/icons/YouTube";

type Params = { slug: string };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const params = listVideos().map((video) => ({ slug: video.stem }));
  // output: export requires at least one prerendered path; the placeholder
  // renders a 404 via notFound() when there are no videos.
  return params.length > 0 ? params : [{ slug: "__empty__" }];
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const video = listVideos().find((candidate) => candidate.stem === slug);
  if (!video) {
    return {};
  }

  const images = [toAbsoluteUrl(videoPosterUrl(video.youtubeId))];
  return {
    title: video.title,
    description: video.description,
    alternates: { canonical: video.href },
    openGraph: {
      type: "video.other",
      siteName: SITE_NAME,
      title: video.title,
      description: video.description,
      url: video.href,
      images,
      videos: [{ url: `https://www.youtube.com/embed/${video.youtubeId}` }],
    },
    twitter: {
      card: "summary_large_image",
      site: TWITTER_HANDLE,
      title: video.title,
      description: video.description,
      images,
    },
  };
}

export default async function VideoPage({ params }: PageProps) {
  const { slug } = await params;
  const videos = listVideos();
  const video = videos.find((candidate) => candidate.stem === slug);
  if (!video) {
    notFound();
  }

  const [{ content }, lastModified] = await Promise.all([
    compileDoc(video.file),
    getLastModifiedFromGit(video.file),
  ]);
  const related = findRelatedVideos(video, videos);

  const additionalNodes = [createVideoObjectNode(video)];
  if (video.author) {
    additionalNodes.push(createAuthorPersonNode(video.author));
  }

  return (
    <div className="px-5 py-8 sm:px-12">
      <PageStructuredData
        title={video.title}
        description={video.description}
        dateModified={lastModified?.toISOString()}
        path={video.href}
        pageType="ItemPage"
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Videos", path: VIDEOS_PATH },
          { name: video.title },
        ]}
        mainEntity={schemaRef(videoObjectId(video))}
        additionalNodes={additionalNodes}
      />
      <div className="mx-auto grid max-w-7xl grid-cols-1 gap-12 lg:grid-cols-[minmax(0,1fr)_20rem] xl:gap-16">
        <article className="min-w-0">
          <Link
            href={VIDEOS_PATH}
            prefetch={false}
            data-llms-ignore
            className="text-cc-ink-dim hover:text-cc-heading inline-flex items-center gap-1.5 text-sm no-underline transition-colors print:hidden"
          >
            <ArrowLeftIcon className="size-3.5" />
            All videos
          </Link>
          <YouTubeVideo
            videoId={video.youtubeId}
            playlabel={`Play ${video.title}`}
            sizes="(max-width: 1023px) calc(100vw - 2.5rem), 880px"
            className="mt-5 rounded-2xl"
          />
          <Eyebrow size="2xs" color="amber" className="mt-8">
            Episode {formatEpisode(video.episode)}
          </Eyebrow>
          <h1 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-2 tracking-tight">
            {video.title}
          </h1>
          <div className="mt-4 flex flex-wrap items-center justify-between gap-4">
            <VideoByline video={video} />
            <div className="grid w-full gap-3 sm:inline-grid sm:w-auto sm:auto-cols-fr sm:grid-flow-col print:hidden">
              {video.example ? (
                <SolidButton
                  href={video.example}
                  className="gap-2 border border-transparent"
                >
                  <BranchIcon />
                  Example source
                </SolidButton>
              ) : null}
              <OutlineButton
                href={youtubeWatchUrl(video.youtubeId)}
                className="gap-2"
              >
                <YouTubeIcon className="text-cc-youtube h-4 w-auto fill-current" />
                Watch on YouTube
              </OutlineButton>
            </div>
          </div>
          <Card className="mt-8 px-6 py-2 sm:px-8">
            {content}
            <TagList tags={video.tags.map(topicLabel)} className="mt-2 mb-6" />
          </Card>
        </article>
        <MoreVideos videos={related} className="lg:pt-10" />
      </div>
    </div>
  );
}
