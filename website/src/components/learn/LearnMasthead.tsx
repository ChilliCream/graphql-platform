import { Eyebrow } from "@/src/design-system/Eyebrow";

interface LearnMastheadProps {
  readonly title: string;
  readonly teaser: string;
}

/**
 * Compact landing header (learn-editorial.md section 3.1). Deliberately not
 * the full-height `PageHero`: an editorial front page leads with content,
 * not a display title, so this carries roughly half `PageHero`'s vertical
 * weight and stays left-aligned.
 */
export function LearnMasthead({ title, teaser }: LearnMastheadProps) {
  return (
    <header className="py-10 sm:py-14">
      <Eyebrow color="ink-dim">Learn</Eyebrow>
      <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-3 font-semibold text-balance">{title}</h1>
      <p className="text-cc-ink-dim mt-4 max-w-2xl">{teaser}</p>
    </header>
  );
}
