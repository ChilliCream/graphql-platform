import type { ReactNode } from "react";
import { CardGrid } from "@/src/components/CardGrid";
import { ArrowLink } from "@/src/components/ArrowLink";
import { productLabel } from "@/src/data/learn/facets";
import { findHub, hubHref, hubsForLearnItem } from "@/src/data/learn/hubs";
import type { LearnItemSummary, VideoItem } from "@/src/data/learn/types";
import { Link } from "@/src/design-system/Link";
import { formatDate } from "@/src/helpers/formatDate";
import { Detail } from "./Detail";
import { LearnCard } from "./LearnCard";
import { LearnVideoPlayer } from "./LearnVideoPlayer";

interface LearnVideoDetailProps {
  readonly video: VideoItem & { readonly youtubeId: string };
  readonly related: readonly LearnItemSummary[];
}

const URL_RE = /(https?:\/\/[^\s]+)/g;

/** Splits a bare-URL plaintext description into `\n\n`-separated paragraphs. */
function paragraphsOf(description: string): readonly string[] {
  return description
    .split(/\n{2,}/)
    .map((paragraph) => paragraph.trim())
    .filter(Boolean);
}

/** Renders a paragraph's bare URLs as links; `URL_RE`'s capture group puts matches at odd indices. */
function linkify(text: string): ReactNode {
  return text.split(URL_RE).map((part, index) =>
    index % 2 === 1 ? (
      <Link key={index} href={part}>
        {part}
      </Link>
    ) : (
      part
    ),
  );
}

/** Topic-hub links (Detail's dt styling, linked dd values) for the video's hubs, per the canonical taxonomy in `src/data/learn/hubs.ts`. */
function TopicsDetail({ video }: { readonly video: VideoItem }) {
  const hubs = hubsForLearnItem(video)
    .map((key) => findHub(key))
    .filter((hub) => hub !== undefined);
  if (hubs.length === 0) {
    return null;
  }
  return (
    <div>
      <dt className="text-cc-ink-dim font-mono text-[0.6875rem] tracking-wider uppercase">Topics</dt>
      <dd className="text-cc-heading mt-1 flex flex-wrap gap-x-3 gap-y-1">
        {hubs.map((hub) => (
          <Link key={hub.key} href={hubHref(hub.key)}>
            {hub.label}
          </Link>
        ))}
      </dd>
    </div>
  );
}

/**
 * The metadata rail to the right of the player (learn-editorial.md section
 * 20, amended per website-kbx.19, website-kbx.23, and website-yw2.1, which
 * dropped the example-repository link: every video's exampleRepoUrl pointed
 * at github.com/ChilliCream/examples, a repo that doesn't exist). Follows
 * `TemplateDetail`'s sticky sidebar-card treatment (website-8s5.3) so the two
 * detail types feel consistent.
 */
function VideoRail({ video }: { readonly video: VideoItem }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm">
      <div className="p-5">
        <dl className="space-y-4 text-sm">
          {video.publishedAt && <Detail label="Published" value={formatDate(video.publishedAt)} />}
          {video.duration && <Detail label="Duration" value={video.duration} />}
          {video.level && <Detail label="Level" value={video.level} className="capitalize" />}
          <TopicsDetail video={video} />
          <Detail label="Products" value={video.products.map(productLabel).join(", ")} />
        </dl>
        <div className="mt-5">
          <ArrowLink href={video.url} target="_blank" rel="noopener noreferrer">
            Watch on YouTube
          </ArrowLink>
        </div>
      </div>
    </div>
  );
}

/**
 * Video detail page composition (learn-editorial.md section 20, amended by
 * learn-harmonization.md D2/D9, website-kbx.19, website-kbx.23, and
 * website-kbx.24): title-only header, click-to-load embed, description,
 * and a metadata rail (example repository link, publish date, duration,
 * level, topics, products) to the right of the player. The page loads data
 * and picks related items; this component only renders the props it's given.
 *
 * Each piece (player, rail, description) renders once; explicit grid
 * placement (not a duplicated `lg:hidden` / `hidden lg:block` pair) puts them
 * in reading order on small screens (player, rail, description) and into a
 * player+description column beside a sticky rail column at `lg`
 * (website-kbx.19, following hnm.1's website-8s5.3 comment 2).
 */
export function LearnVideoDetail({ video, related }: LearnVideoDetailProps) {
  const paragraphs = video.description ? paragraphsOf(video.description) : [];

  const description = paragraphs.length > 0 && (
    <div className="max-w-3xl space-y-4">
      {paragraphs.map((paragraph, index) => (
        <p key={index} className="text-cc-prose leading-7">
          {linkify(paragraph)}
        </p>
      ))}
    </div>
  );

  return (
    <>
      <header className="pb-8 sm:pb-10">
        <h1 className="font-heading text-cc-heading text-h3 font-semibold tracking-[-0.02em] text-balance">
          {video.title}
        </h1>
      </header>

      <div className="grid gap-10 pb-12 lg:grid-cols-[minmax(0,1fr)_19rem] lg:items-start lg:gap-x-16 lg:gap-y-10">
        <div className="order-1 min-w-0 lg:order-none lg:col-start-1 lg:row-start-1">
          <LearnVideoPlayer videoId={video.youtubeId} title={video.title} />
        </div>
        <div className="order-2 lg:sticky lg:top-28 lg:order-none lg:col-start-2 lg:row-span-2 lg:row-start-1">
          <VideoRail video={video} />
        </div>
        {description && (
          <div className="order-3 min-w-0 lg:order-none lg:col-start-1 lg:row-start-2">{description}</div>
        )}
      </div>

      {related.length > 0 && (
        <section className="border-cc-card-border border-t py-8 sm:py-10">
          <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">More to watch</h2>
          <div className="mt-8">
            <CardGrid cols={3} step="progressive" itemsStretch>
              {related.map((item) => (
                <LearnCard key={item.slug} item={item} />
              ))}
            </CardGrid>
          </div>
        </section>
      )}
    </>
  );
}
