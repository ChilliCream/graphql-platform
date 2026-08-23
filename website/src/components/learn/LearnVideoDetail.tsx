import type { ReactNode } from "react";
import { CardGrid } from "@/src/components/CardGrid";
import { ArrowLink } from "@/src/components/ArrowLink";
import { productLabel } from "@/src/data/learn/facets";
import type { LearnItemSummary, VideoItem } from "@/src/data/learn/types";
import { SolidButton } from "@/src/design-system/Button";
import { Link } from "@/src/design-system/Link";
import { Tag } from "@/src/design-system/Tag";
import { formatDate } from "@/src/helpers/formatDate";
import { ArticleBreadcrumb } from "./ArticleLayout";
import { ContentTypeBadge } from "./ContentTypeBadge";
import { topicLabelForProduct } from "./editorial";
import { LearnCard } from "./LearnCard";
import { LearnVideoPlayer } from "./LearnVideoPlayer";

interface LearnVideoDetailProps {
  readonly video: VideoItem;
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

function ExampleCard({ exampleUrl }: { readonly exampleUrl: string }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm">
      <p className="text-cc-heading font-heading text-lg font-semibold">Example code</p>
      <p className="text-cc-prose mt-2 text-sm leading-relaxed">The complete project built in this video.</p>
      <SolidButton href={exampleUrl} className="mt-4 w-full">
        Download example
      </SolidButton>
      <p className="text-cc-ink-dim mt-3 text-sm">Free download, no signup</p>
    </div>
  );
}

function Facts({ video }: { readonly video: VideoItem }) {
  return (
    <div>
      <dl className="space-y-4 text-sm">
        <Detail label="Products" value={video.products.map(productLabel).join(", ")} />
        {video.duration && <Detail label="Duration" value={video.duration} />}
        {video.level && <Detail label="Level" value={video.level} className="capitalize" />}
        {video.publishedAt && <Detail label="Published" value={formatDate(video.publishedAt)} />}
      </dl>
      <div className="mt-5">
        <ArrowLink href={video.url} target="_blank" rel="noopener noreferrer">
          Watch on YouTube
        </ArrowLink>
      </div>
    </div>
  );
}

function Detail({
  label,
  value,
  className = "",
}: {
  readonly label: string;
  readonly value: string;
  readonly className?: string;
}) {
  return (
    <div>
      <dt className="text-cc-ink-dim font-mono text-[0.65rem] tracking-wider uppercase">{label}</dt>
      <dd className={`text-cc-heading mt-1 ${className}`.trim()}>{value}</dd>
    </div>
  );
}

/**
 * Video detail page composition (learn-editorial.md section 20): header,
 * click-to-load embed, description, example-code download, facts list, and
 * a related-videos rail. The page loads data and picks related items; this
 * component only renders the props it's given.
 */
export function LearnVideoDetail({ video, related }: LearnVideoDetailProps) {
  const paragraphs = video.description ? paragraphsOf(video.description) : [];
  const topic = topicLabelForProduct(video.products);
  const metaLine = video.publishedAt
    ? [formatDate(video.publishedAt), video.duration].filter(Boolean).join(" · ")
    : video.duration;

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
      <header className="py-10 sm:py-16">
        <div className="mb-8">
          <ArticleBreadcrumb
            items={[
              { label: "Learn", href: "/learn" },
              { label: "Videos", href: "/learn/browse?type=video" },
              { label: video.title },
            ]}
          />
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <ContentTypeBadge type="video" />
          <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">{topic}</span>
          {video.level && <Tag className="capitalize">{video.level}</Tag>}
        </div>
        <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-6 font-semibold tracking-[-0.02em] text-balance">
          {video.title}
        </h1>
        {metaLine && <p className="text-cc-ink-dim mt-3 text-sm">{metaLine}</p>}
        <p className="text-cc-prose mt-5 max-w-2xl text-lg leading-relaxed">{video.tagline}</p>
      </header>

      <div className="border-cc-card-border grid gap-12 border-t py-12 lg:grid-cols-[minmax(0,1fr)_19rem] lg:gap-16">
        {/* Mobile/tablet (below lg): player, example card, description, facts. */}
        <div className="lg:hidden">
          <LearnVideoPlayer videoId={video.youtubeId ?? ""} title={video.title} />
          {video.exampleUrl && (
            <div className="mt-10">
              <ExampleCard exampleUrl={video.exampleUrl} />
            </div>
          )}
          {description && <div className="mt-10">{description}</div>}
          <div className="mt-10">
            <Facts video={video} />
          </div>
        </div>

        {/* Desktop (lg+): fluid left column (player, description), sticky 19rem aside (example card, facts). */}
        <div className="hidden min-w-0 lg:block">
          <LearnVideoPlayer videoId={video.youtubeId ?? ""} title={video.title} />
          {description && <div className="mt-10">{description}</div>}
        </div>
        <aside className="hidden lg:sticky lg:top-28 lg:block">
          {video.exampleUrl && (
            <div className="mb-6">
              <ExampleCard exampleUrl={video.exampleUrl} />
            </div>
          )}
          <Facts video={video} />
        </aside>
      </div>

      {related.length > 0 && (
        <section className="border-cc-card-border border-t py-16 sm:py-24">
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold">More to watch</h2>
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
