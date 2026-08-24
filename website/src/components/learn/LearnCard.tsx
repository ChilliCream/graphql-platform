import Link from "next/link";
import type { ComponentPropsWithoutRef } from "react";
import { DrinkIcon } from "@/src/components/DrinkIcon";
import { youTubePosterFallback } from "@/src/components/youTubePosterUrl";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { Tag } from "@/src/design-system/Tag";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { ContentTypeBadge } from "./ContentTypeBadge";
import { topicLabelForProduct } from "./editorial";
import { learnItemHref } from "./learnItemHref";
import { PRODUCT_ART } from "./productArt";
import { STACK_ICONS } from "./stackIcons";

interface LearnCardProps {
  readonly item: LearnItemSummary;
}

/** Diagonal arrow-out-of-box glyph for items that open in a new tab. */
function ExternalArrowIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...props}
    >
      <path d="M7 17 17 7" />
      <path d="M9 7h8v8" />
    </svg>
  );
}

function HeaderMeta({ item }: LearnCardProps) {
  if (item.type === "template") {
    if (!item.agentReady) {
      return null;
    }
    // Outline Tag is the canonical Agent-ready skin (learn-harmonization.md
    // D10); the solid warning pill this replaced was the only solid-warning
    // surface in the system and read as a different flag than the one on
    // TemplateDetail.
    return <Tag className="border-cc-warning/40 text-cc-warning">Agent-ready</Tag>;
  }
  // A video with a youtubeId already states its duration as an overlay on
  // its thumbnail (VideoThumb); the header meta slot is only the duration's
  // second appearance for the two legacy, thumbnail-less entries.
  const meta = item.type === "video" ? (item.youtubeId ? undefined : item.duration) : item.level;
  if (!meta) {
    return null;
  }
  return <span className="text-cc-ink-dim font-mono text-[0.6875rem] tracking-wider uppercase">{meta}</span>;
}

/**
 * Poster thumbnail for a video item that has a native `youtubeId`
 * (learn-harmonization.md section 2.5 item 5, ticket website-8s5.4). Renders
 * a plain `<img>` rather than the `YouTubePoster` component itself, since
 * `LearnCard` is also imported by `LearnCatalog`, a client component:
 * `YouTubePoster` pulls in the `node:fs`-based optimized-image manifest,
 * which cannot enter a browser bundle. `poster` is the self-hosted optimized
 * src resolved once in `content.ts` (server-only) and carried on the item;
 * without one (development, or the manifest has no entry) this falls back to
 * the external `hqdefault` thumbnail built from the same `youTubePosterFallback`
 * key `YouTubePoster` uses. Overlays the duration only when the video has one
 * (7 of the 9 seeded videos don't, so most cards render no chip at all).
 */
function VideoThumb({
  videoId,
  duration,
  poster,
}: {
  readonly videoId: string;
  readonly duration?: string;
  readonly poster?: string;
}) {
  return (
    <div className="bg-cc-white/4 relative mb-4 aspect-video overflow-hidden rounded-lg">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src={poster ?? youTubePosterFallback(videoId)}
        alt=""
        loading="lazy"
        decoding="async"
        className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
      />
      {duration && (
        <span className="bg-cc-surface/90 text-cc-ink absolute right-2 bottom-2 rounded-full px-2 py-0.5 font-mono text-[0.6875rem] tracking-wider">
          {duration}
        </span>
      )}
    </div>
  );
}

/**
 * Unified card for every /learn content type: template, video, tutorial,
 * example, and workshop. Per learn-harmonization.md section 2.3/D4, color no
 * longer varies by type: the badge, hover border, and footer icons are
 * neutral at rest, and only the accent CTA and the icons' hover state carry
 * color.
 */
export function LearnCard({ item }: LearnCardProps) {
  const href = learnItemHref(item);
  const external = !href.startsWith("/");
  const hasThumbnail = item.type === "video" && Boolean(item.youtubeId);

  const inner = (
    <>
      {hasThumbnail && item.type === "video" && item.youtubeId && (
        <VideoThumb videoId={item.youtubeId} duration={item.duration} poster={item.poster} />
      )}
      <div className="flex items-start justify-between gap-3">
        <ContentTypeBadge type={item.type} />
        <HeaderMeta item={item} />
      </div>
      <h3 className="font-heading text-cc-heading text-h6 mt-3 font-semibold">{item.title}</h3>
      {hasThumbnail && item.type === "video" ? (
        <span className="text-cc-ink-dim mt-1 block font-mono text-xs tracking-wider uppercase">
          {topicLabelForProduct(item.products)}
        </span>
      ) : null}
      <p className="text-cc-ink-dim mt-2 line-clamp-3 text-sm leading-relaxed">{item.tagline}</p>
      <div className="mt-auto flex items-center justify-between gap-3 pt-4">
        <span
          className="flex items-end gap-2 grayscale transition-[filter] duration-200 group-hover:grayscale-0"
          aria-hidden="true"
        >
          <span className="flex items-end gap-1.5">
            {item.products.map((product) => {
              const art = PRODUCT_ART[product];
              return <DrinkIcon key={product} Icon={art.Drink} name={art.drinkName} base={28} />;
            })}
          </span>
          {item.type === "template" && item.stack.length > 0 && (
            <span className="flex items-center gap-1.5">
              {item.stack.map((key) => {
                const { Icon, label } = STACK_ICONS[key];
                return (
                  <span
                    key={key}
                    title={label}
                    className="bg-cc-white/8 flex size-7 items-center justify-center rounded-lg"
                  >
                    <Icon className="size-4" />
                  </span>
                );
              })}
            </span>
          )}
        </span>
        <span className="text-cc-accent inline-flex shrink-0 items-center gap-2 text-sm font-medium">
          Open
          {external ? (
            <ExternalArrowIcon className="size-3.5 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
          ) : (
            <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-1" />
          )}
        </span>
      </div>
    </>
  );

  const className =
    "border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover group flex h-full flex-col rounded-2xl border p-6 no-underline backdrop-blur-sm transition-[border-color,transform] duration-200 hover:-translate-y-1";
  const ariaLabel = `Open: ${item.title}`;

  if (external) {
    return (
      <a href={href} target="_blank" rel="noopener noreferrer" aria-label={ariaLabel} className={className}>
        {inner}
      </a>
    );
  }
  return (
    <Link href={href} prefetch={false} aria-label={ariaLabel} className={className}>
      {inner}
    </Link>
  );
}
