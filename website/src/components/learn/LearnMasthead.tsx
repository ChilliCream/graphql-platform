import { Eyebrow } from "@/src/design-system/Eyebrow";

interface LearnMastheadProps {
  readonly title: string;
  readonly teaser: string;
  /** Renders the "Learn" eyebrow above the title. Defaults to `true`. */
  readonly showEyebrow?: boolean;
}

/**
 * Compact landing header (learn-editorial.md section 3.1, amended by
 * learn-harmonization.md D2/D6/D8/D23). Deliberately not the full-height
 * `PageHero`: an editorial front page leads with content, not a display
 * title, so this carries roughly half `PageHero`'s vertical weight and stays
 * left-aligned.
 */
export function LearnMasthead({ title, teaser, showEyebrow = true }: LearnMastheadProps) {
  return (
    <header className="py-6 sm:py-8">
      {showEyebrow ? <Eyebrow color="ink-dim">Learn</Eyebrow> : null}
      <h1 className={`font-heading text-cc-heading text-h3 font-semibold text-balance ${showEyebrow ? "mt-3" : ""}`}>
        {title}
      </h1>
      <p className="text-cc-ink-dim mt-4 max-w-2xl text-lg">{teaser}</p>
    </header>
  );
}
