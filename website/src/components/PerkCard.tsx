import type { CSSProperties, ReactElement } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { PopularBadge } from "@/src/components/PopularBadge";
import { popularBorderStyle } from "@/src/components/popularRing";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

type PerkAccent = "accent" | "violet" | "coral";

const ACCENT_TEXT: Record<PerkAccent, string> = {
  accent: "text-cc-accent",
  violet: "text-[#7c92c6]",
  coral: "text-[#f0786a]",
};

interface PerkCardProps {
  readonly title: string;
  readonly items: readonly string[];
  /** Mono kicker in the header (e.g. "Level 1"), tinted by `accent`. */
  readonly tag?: string;
  /** Short line under the title. */
  readonly subtitle?: string;
  /** Lead paragraph above the perk list. */
  readonly intro?: string;
  /** Mono label above the perk list (e.g. "What we cover"). */
  readonly listLabel?: string;
  /** Optional illustration in the top-right of the card. */
  readonly Icon?: () => ReactElement;
  /** Tints the tag, icon, and perk checks. Defaults to the brand accent. */
  readonly accent?: PerkAccent;
  readonly cta?: {
    readonly label: string;
    readonly href: string;
    readonly solid?: boolean;
  };
  /** Rainbow gradient border plus a "popular" badge. */
  readonly highlight?: boolean;
  readonly highlightLabel?: string;
}

/**
 * A perk/offer card: an optional tagged + iconed header, a lead paragraph, a
 * labelled checklist that fills the card, and an optional call to action. Used
 * for level cards and corporate-offer cards; `highlight` marks the popular one.
 *
 * Each section is a subgrid row, so when several cards share a grid row their
 * tags, titles, and lead paragraphs line up and the perk lists start at the same
 * height regardless of how much each paragraph wraps.
 */
export function PerkCard({
  title,
  items,
  tag,
  subtitle,
  intro,
  listLabel,
  Icon,
  accent = "accent",
  cta,
  highlight = false,
  highlightLabel = "Most popular",
}: PerkCardProps) {
  const accentText = ACCENT_TEXT[accent];
  const CtaButton = cta?.solid ? SolidButton : OutlineButton;

  // One subgrid row per section cell below; the count must match the number of
  // in-flow children (the absolutely-positioned badge does not take a row).
  const rowCount = 2 + (tag ? 1 : 0) + (intro ? 1 : 0);
  const cardStyle: CSSProperties = highlight
    ? { gridRow: `span ${rowCount}`, ...popularBorderStyle }
    : { gridRow: `span ${rowCount}` };

  return (
    <article
      className={`relative grid h-full grid-rows-subgrid gap-5 rounded-3xl p-6 sm:p-7 ${
        highlight ? "" : "border-cc-card-border bg-cc-card-bg/60 border"
      }`}
      style={cardStyle}
    >
      {highlight && <PopularBadge label={highlightLabel} />}

      {tag && (
        <span
          className={`font-mono text-xs font-semibold tracking-[0.2em] ${accentText}`}
        >
          {tag}
        </span>
      )}

      <div className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-2">
          <h3 className="font-heading text-cc-heading text-h4 font-semibold">
            {title}
          </h3>
          {subtitle && (
            <p className="text-cc-ink-dim text-sm leading-relaxed">
              {subtitle}
            </p>
          )}
        </div>
        {Icon && (
          <span className={`flex-none ${accentText}`}>
            <Icon />
          </span>
        )}
      </div>

      {intro && <p className="text-cc-ink text-sm leading-relaxed">{intro}</p>}

      <div className="flex h-full flex-col gap-5">
        {listLabel && (
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            {listLabel}
          </p>
        )}
        <ul className="flex flex-1 flex-col gap-2">
          {items.map((item) => (
            <li key={item} className="flex items-start gap-3">
              <span className={`mt-1 flex-none ${accentText}`}>
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">{item}</span>
            </li>
          ))}
        </ul>

        {cta && (
          <CtaButton href={cta.href} className="w-full">
            {cta.label}
          </CtaButton>
        )}
      </div>
    </article>
  );
}
