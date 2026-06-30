import type { ReactNode } from "react";

type BandSkin = "accent" | "card" | "spectrum" | "bare" | "warm";
type BandLayout = "split" | "centered";

interface BandProps {
  readonly skin?: BandSkin;
  readonly layout?: BandLayout;
  /** Outer section spacing utilities, e.g. "py-16 sm:py-20". */
  readonly className?: string;
  /** Sets `aria-labelledby` on the section (point it at a SectionHeading titleId). */
  readonly labelledBy?: string;
  /** Split layout: the wide primary cell. */
  readonly main?: ReactNode;
  /** Split layout: the narrow secondary cell. */
  readonly aside?: ReactNode;
  /** Centered layout: the column content. */
  readonly children?: ReactNode;
}

// Decorative glows for the skinned panels.
const ACCENT_GLOW =
  "radial-gradient(60% 80% at 100% 0%, rgba(22,185,228,0.12), transparent 60%), radial-gradient(50% 70% at 0% 100%, rgba(124,146,198,0.10), transparent 60%)";
const WARM_GLOW =
  "radial-gradient(60% 80% at 100% 0%, rgba(22,185,228,0.12), transparent 60%), radial-gradient(50% 70% at 0% 100%, rgba(240,120,106,0.10), transparent 60%)";
const SPECTRUM_HAIRLINE =
  "linear-gradient(90deg, transparent, #16b9e4 30%, #7c92c6 50%, #f0786a 70%, transparent)";
const SPECTRUM_GLOW =
  "radial-gradient(50% 50% at 50% 50%, rgba(94,234,212,0.12), transparent 70%)";

const PANEL_SKIN: Record<Exclude<BandSkin, "bare">, string> = {
  accent: "border-cc-accent/40 bg-cc-card-bg/70 p-8 sm:p-12",
  warm: "border-cc-accent/40 bg-cc-card-bg/70 p-8 sm:p-12",
  card: "border-cc-card-border bg-cc-card-bg/60 p-8 sm:p-12",
  spectrum: "border-cc-card-border bg-cc-card-bg p-10 sm:p-16",
};

/**
 * A page band: an optional skinned panel (border, plus decorative glows for the
 * `accent` and `spectrum` skins) wrapping either a two-cell `split` layout
 * (`main` + `aside`) or a `centered` column (`children`). The `bare` skin drops
 * the panel and renders the content directly in the section.
 */
export function Band({
  skin = "card",
  layout = "split",
  className,
  labelledBy,
  main,
  aside,
  children,
}: BandProps) {
  const centered = layout === "centered";
  const content =
    layout === "split" ? (
      <div className="grid items-center gap-8 lg:grid-cols-[1.4fr_1fr]">
        {main}
        {aside}
      </div>
    ) : (
      children
    );

  if (skin === "bare") {
    return (
      <section aria-labelledby={labelledBy} className={className}>
        <div className={centered ? "text-center" : undefined}>{content}</div>
      </section>
    );
  }

  return (
    <section aria-labelledby={labelledBy} className={className}>
      <div
        className={`relative overflow-hidden rounded-3xl border ${PANEL_SKIN[skin]}`}
      >
        {(skin === "accent" || skin === "warm") && (
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0"
            style={{ background: skin === "warm" ? WARM_GLOW : ACCENT_GLOW }}
          />
        )}
        {skin === "spectrum" && (
          <>
            <div
              aria-hidden="true"
              className="pointer-events-none absolute inset-x-0 top-0 h-px"
              style={{ background: SPECTRUM_HAIRLINE }}
            />
            <div
              aria-hidden="true"
              className="pointer-events-none absolute -top-32 left-1/2 h-64 w-[40rem] max-w-full -translate-x-1/2 opacity-50 blur-3xl"
              style={{ background: SPECTRUM_GLOW }}
            />
          </>
        )}
        <div className={`relative ${centered ? "text-center" : ""}`.trim()}>
          {content}
        </div>
      </div>
    </section>
  );
}
