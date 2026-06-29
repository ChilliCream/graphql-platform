import type { ComponentType, CSSProperties } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { CheckIcon } from "./CheckIcon";

interface OfferingProps {
  readonly title: string;
  /** Short, mono-cased caption shown under the title. */
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

  // Each section below is one subgrid row. When rendered inside an `OfferingGrid`
  // the card inherits the grid's row tracks (`grid-template-rows: subgrid`), so a
  // section (notably the variable-height description) lines up across every card
  // in the same row, keeping the price, divider, and perks aligned. The row count
  // must match the number of section cells rendered.
  const rowCount = 2 + (Icon ? 1 : 0) + (description ? 1 : 0) + (price ? 1 : 0);

  // The popular card gets a rainbow gradient border (the gradient paints the
  // border-box layer, the opaque surface fill paints the padding-box layer on
  // top), so the rainbow shows only on the edge and the interior stays solid.
  const cardStyle: CSSProperties = popular
    ? {
        gridRow: `span ${rowCount}`,
        border: "1.5px solid transparent",
        background:
          "linear-gradient(var(--color-cc-surface), var(--color-cc-surface)) padding-box, linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%) border-box",
      }
    : { gridRow: `span ${rowCount}` };

  return (
    <div
      className={`relative grid h-full grid-rows-subgrid gap-0 rounded-3xl p-6 sm:p-7 ${
        popular ? "" : "border-cc-card-border bg-cc-card-bg/60 border"
      }`}
      style={cardStyle}
    >
      {popular && <PopularBadge />}

      {Icon && (
        <div className="flex flex-col items-center text-center">
          <Icon className="text-cc-ink h-28 w-auto" />
          <Dots className="w-full" />
        </div>
      )}

      <Heading className="font-heading text-cc-heading text-xl font-semibold">
        {title}
      </Heading>
      {description && (
        <p className="text-cc-nav-label mt-1 font-mono text-xs">
          {description}
        </p>
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

      <div className="flex h-full flex-col">
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
    </div>
  );
}

function Dots({ className = "" }: { readonly className?: string }) {
  return (
    <div
      aria-hidden="true"
      className={`border-cc-ink-faint my-5 border-t border-dashed ${className}`}
    />
  );
}

const RING_COLOR = "var(--color-cc-accent)";

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
      <span className="text-cc-heading relative z-20 px-7 py-2 font-mono text-[0.65rem] tracking-[0.15em] whitespace-nowrap uppercase [grid-area:1/1]">
        Most Popular
      </span>
    </span>
  );
}
