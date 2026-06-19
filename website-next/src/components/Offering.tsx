import type { ComponentType, CSSProperties } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { CheckIcon } from "./CheckIcon";

interface OfferingProps {
  readonly title: string;
  /** Short, mono-cased caption shown under the title (pricing style). */
  readonly tagline?: string;
  /** Longer paragraph shown under the title (services style). */
  readonly description?: string;
  /** Optional illustration centered at the top of the card. */
  readonly Icon?: ComponentType<{ readonly className?: string }>;
  /** Headline price, e.g. "$400", "Free", "Custom". */
  readonly price?: string;
  /** Note shown next to the price, e.g. "per month". */
  readonly priceNote?: string;
  readonly perks: readonly string[];
  readonly callToAction?: { readonly title: string; readonly link: string };
  /** Highlights the card with the accent ring and "Most Popular" badge. */
  readonly popular?: boolean;
  readonly headingLevel?: "h2" | "h3";
}

export function Offering({
  title,
  tagline,
  description,
  Icon,
  price,
  priceNote,
  perks,
  callToAction,
  popular = false,
  headingLevel: Heading = "h3",
}: OfferingProps) {
  const CallToActionButton = popular ? SolidButton : OutlineButton;

  return (
    <div
      className={`relative flex h-full flex-col rounded-3xl border p-6 sm:p-7 ${
        popular
          ? "bg-cc-card-bg border-[#06668c]"
          : "border-cc-card-border bg-cc-card-bg/60"
      }`}
    >
      {popular && <PopularBadge />}

      {Icon && (
        <>
          <div className="flex flex-col items-center text-center">
            <Icon className="text-cc-ink h-28 w-auto" />
          </div>
          <Dots />
        </>
      )}

      <Heading className="font-heading text-cc-heading text-xl font-semibold">
        {title}
      </Heading>
      {tagline && (
        <p className="text-cc-nav-label mt-1 font-mono text-xs">{tagline}</p>
      )}
      {description && (
        <p className="text-cc-ink-dim mt-3 text-sm">{description}</p>
      )}

      {price && (
        <div className="mt-5 flex items-baseline gap-2">
          <span className="font-heading text-cc-heading text-h3 font-semibold">
            {price}
          </span>
          {priceNote && (
            <span className="text-cc-nav-label font-mono text-xs">
              {priceNote}
            </span>
          )}
        </div>
      )}

      <Dots />

      <ul className="flex flex-1 flex-col gap-3">
        {perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{perk}</span>
          </li>
        ))}
      </ul>

      {callToAction && (
        <CallToActionButton href={callToAction.link} className="mt-7 w-full">
          {callToAction.title}
        </CallToActionButton>
      )}
    </div>
  );
}

function Dots() {
  return (
    <div
      aria-hidden="true"
      className="my-5 border-t border-dashed border-[rgba(245,241,234,0.16)]"
    />
  );
}

// "Most Popular" badge: a hexagon (flat top/bottom, triangular ends) drawn
// without SVG so it scales with its text. `--t` is the tip width. The outline is
// six gradient "lines" layered on one element, so every edge shares a single
// coordinate space and the joints meet exactly. A separate clip-path layer fills
// the interior so it masks the card border passing behind the badge. The three
// layers carry explicit z-index because clip-path forms a stacking context that
// would otherwise paint the fill on top of the outline and text.
const RING_COLOR = "#06668c";

const HEX_CLIP =
  "polygon(0 50%, var(--t) 0, calc(100% - var(--t)) 0, 100% 50%, calc(100% - var(--t)) 100%, var(--t) 100%)";

const hairline = (dir: string) =>
  `linear-gradient(to ${dir}, transparent calc(50% - 0.6px), ${RING_COLOR} calc(50% - 0.6px), ${RING_COLOR} calc(50% + 0.6px), transparent calc(50% + 0.6px))`;

const BADGE_OUTLINE = [
  `linear-gradient(${RING_COLOR}, ${RING_COLOR}) center top / calc(100% - var(--t) * 2) 1px no-repeat`,
  `linear-gradient(${RING_COLOR}, ${RING_COLOR}) center bottom / calc(100% - var(--t) * 2) 1px no-repeat`,
  `${hairline("top left")} left top / var(--t) 50% no-repeat`,
  `${hairline("bottom left")} left bottom / var(--t) 50% no-repeat`,
  `${hairline("top right")} right top / var(--t) 50% no-repeat`,
  `${hairline("bottom right")} right bottom / var(--t) 50% no-repeat`,
].join(", ");

function PopularBadge() {
  return (
    <span
      className="absolute top-0 left-1/2 inline-grid -translate-x-1/2 -translate-y-1/2"
      style={{ "--t": "13px" } as CSSProperties}
    >
      <span
        aria-hidden="true"
        className="bg-cc-surface relative z-0 [grid-area:1/1]"
        style={{ clipPath: HEX_CLIP }}
      />
      <span
        aria-hidden="true"
        className="relative z-10 [grid-area:1/1]"
        style={{ background: BADGE_OUTLINE }}
      />
      <span className="text-cc-nav-label relative z-20 px-7 py-2 font-mono text-[0.65rem] tracking-[0.15em] whitespace-nowrap uppercase [grid-area:1/1]">
        Most Popular
      </span>
    </span>
  );
}
