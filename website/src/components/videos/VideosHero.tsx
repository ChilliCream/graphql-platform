import { ButtonRow } from "@/src/components/ButtonRow";
import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import {
  CHILLICREAM_TV_CHANNEL_URL,
  type VideoSummary,
} from "@/src/helpers/videos";
import { YouTubeIcon } from "@/src/icons/YouTube";
import { VideoStage } from "./VideoStage";

interface VideosHeroProps {
  /** All videos, newest first. */
  readonly videos: readonly VideoSummary[];
  /** Target of the browse call to action, e.g. the episode list anchor. */
  readonly browseHref: string;
}

const UP_NEXT_COUNT = 5;

/**
 * Header of the videos index: the section intro next to a player window with
 * the latest episode and the episodes that follow it.
 */
export function VideosHero({ videos, browseHref }: VideosHeroProps) {
  return (
    <div className="border-cc-card-border relative left-1/2 -mt-8 w-screen -translate-x-1/2 overflow-hidden border-b">
      <PageSection className="grid items-center gap-12 py-16 sm:pt-20 sm:pb-24 lg:grid-cols-[minmax(0,0.72fr)_minmax(0,1.28fr)] lg:gap-14">
        <div>
          <Eyebrow color="ink-dim">ChilliCream TV</Eyebrow>
          <h1 className="font-heading text-h3 text-cc-heading sm:text-h2 lg:text-h3 mt-4 tracking-tight text-balance">
            Learn from the people{" "}
            <span className="text-cc-amber">who build it.</span>
          </h1>
          <p className="text-cc-prose mt-6 max-w-lg text-lg leading-relaxed">
            Hands-on episodes from the Hot Chocolate team on DataLoader,
            pagination, projections, and everything else that goes into a fast
            GraphQL API in .NET.
          </p>
          <ButtonRow align="start" className="mt-9">
            <SolidButton href={browseHref}>Browse episodes</SolidButton>
            <OutlineButton href={CHILLICREAM_TV_CHANNEL_URL} className="gap-2">
              <YouTubeIcon className="text-cc-youtube h-4 w-auto fill-current" />
              Subscribe on YouTube
            </OutlineButton>
          </ButtonRow>
        </div>

        {videos.length > 0 ? (
          <div data-llms-ignore>
            <VideoStage
              videos={videos}
              upNextCount={UP_NEXT_COUNT}
              channelUrl={CHILLICREAM_TV_CHANNEL_URL}
            />
          </div>
        ) : null}
      </PageSection>
    </div>
  );
}
