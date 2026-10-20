import { Card } from "@/src/design-system/Card";
import type { ComparisonSummary } from "@/src/helpers/comparisons";

interface ComparisonHighlightsProps {
  readonly highlights: ComparisonSummary["highlights"];
  readonly className?: string;
}

/** The key differences of a comparison as a grid of short cards. */
export function ComparisonHighlights({
  highlights,
  className,
}: ComparisonHighlightsProps) {
  return (
    <section aria-labelledby="highlights-heading" className={className}>
      <h2 id="highlights-heading" className="sr-only">
        Key differences
      </h2>
      <ul className="grid gap-4 sm:grid-cols-2">
        {highlights.map((highlight) => (
          <Card as="li" key={highlight.title} variant="tile">
            <h3 className="font-heading text-cc-heading text-lg font-semibold">
              {highlight.title}
            </h3>
            <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
              {highlight.text}
            </p>
          </Card>
        ))}
      </ul>
    </section>
  );
}
