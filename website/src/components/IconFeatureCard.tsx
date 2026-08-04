import type { ReactElement } from "react";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";

interface IconFeatureCardProps {
  readonly icon: ReactElement;
  readonly title: string;
  readonly copy: string;
  /** Mono kicker shown above the icon (stacked layout). */
  readonly eyebrow?: string;
  /** Mono kicker shown under the title (inline layout). */
  readonly subtitle?: string;
  /** Italic note pinned to the bottom of the card. */
  readonly footnote?: string;
  /** Icon stacked above the title, or inline beside it. */
  readonly layout?: "stacked" | "inline";
  /** Card prominence: `md` (default), or `lg` for hero-level cards. */
  readonly size?: "md" | "lg";
  /** Content alignment for the stacked layout: `start` (default) or `center`. */
  readonly align?: "start" | "center";
}

const STACKED_ICON_SIZE: Record<"md" | "lg", string> = {
  md: "[&>svg]:h-8 [&>svg]:w-8",
  lg: "[&>svg]:h-11 [&>svg]:w-11",
};

/**
 * A feature card with a bare accent icon, a title, and a body line. Stacks the
 * icon above the title by default (with an optional eyebrow above it), or sets
 * it inline beside a title + mono subtitle. Used for hero scenario boxes,
 * outcome grids, and delivery-format grids.
 */
export function IconFeatureCard({
  icon,
  title,
  copy,
  eyebrow,
  subtitle,
  footnote,
  layout = "stacked",
  size = "md",
  align = "start",
}: IconFeatureCardProps) {
  if (layout === "inline") {
    return (
      <Card as="article" variant="panel" className="flex h-full flex-col gap-4">
        <div className="flex items-center gap-3">
          <span className="text-cc-accent flex-none [&>svg]:h-7 [&>svg]:w-7">
            {icon}
          </span>
          <div>
            <h3 className="font-heading text-cc-heading text-lg font-semibold">
              {title}
            </h3>
            {subtitle && (
              <Eyebrow as="span" size="2xs">
                {subtitle}
              </Eyebrow>
            )}
          </div>
        </div>
        <p className="text-cc-ink text-sm leading-relaxed">{copy}</p>
        {footnote && (
          <p className="text-cc-ink-dim mt-auto text-xs leading-relaxed italic">
            {footnote}
          </p>
        )}
      </Card>
    );
  }

  const Heading = size === "lg" ? "h2" : "h3";

  return (
    <Card
      as="article"
      variant="panel"
      className={`flex h-full flex-col gap-4 ${
        align === "center" ? "items-center text-center" : ""
      }`}
    >
      {eyebrow && (
        <Eyebrow as="div" size="2xs" color="ink-dim">
          {eyebrow}
        </Eyebrow>
      )}
      <span className={`text-cc-accent ${STACKED_ICON_SIZE[size]}`}>
        {icon}
      </span>
      <Heading
        className={`font-heading text-cc-heading font-semibold ${
          size === "lg" ? "text-xl" : "text-base"
        }`}
      >
        {title}
      </Heading>
      <p className="text-cc-ink text-sm leading-relaxed">{copy}</p>
      {footnote && (
        <p className="text-cc-ink-dim mt-auto text-xs leading-relaxed italic">
          {footnote}
        </p>
      )}
    </Card>
  );
}
