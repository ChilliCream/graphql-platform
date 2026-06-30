import type { ReactElement } from "react";

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
}: IconFeatureCardProps) {
  if (layout === "inline") {
    return (
      <article className="border-cc-card-border bg-cc-card-bg/60 flex h-full flex-col gap-4 rounded-3xl border p-6">
        <div className="flex items-center gap-3">
          <span className="text-cc-accent flex-none [&>svg]:h-7 [&>svg]:w-7">
            {icon}
          </span>
          <div>
            <h3 className="font-heading text-cc-heading text-lg font-semibold">
              {title}
            </h3>
            {subtitle && (
              <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                {subtitle}
              </span>
            )}
          </div>
        </div>
        <p className="text-cc-ink text-sm leading-relaxed">{copy}</p>
        {footnote && (
          <p className="text-cc-ink-dim mt-auto text-xs leading-relaxed italic">
            {footnote}
          </p>
        )}
      </article>
    );
  }

  const Heading = size === "lg" ? "h2" : "h3";

  return (
    <article className="border-cc-card-border bg-cc-card-bg/60 flex h-full flex-col gap-4 rounded-3xl border p-6 sm:p-7">
      {eyebrow && (
        <div className="text-cc-ink-dim font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {eyebrow}
        </div>
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
    </article>
  );
}
