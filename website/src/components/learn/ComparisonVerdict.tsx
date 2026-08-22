import { CardGrid } from "@/src/components/CardGrid";
import { CheckList } from "@/src/components/CheckList";
import { HighlightCard } from "@/src/components/HighlightCard";

interface ComparisonOption {
  readonly name: string;
  /** Short "Choose {name} when" facts, rendered as a check list. */
  readonly reasons: readonly string[];
  /** Marks the ChilliCream option: renders the rainbow-border highlight treatment with an "Our take" badge (learn-editorial.md section 6.2). */
  readonly ours?: boolean;
}

interface ComparisonVerdictProps {
  /** One card per compared option, 2 or 3 entries. */
  readonly options: readonly ComparisonOption[];
}

/**
 * MDX-usable verdict cards for comparison articles: one card per compared
 * option with a "Choose {X} when" checklist, so the vendor position is
 * disclosed visually instead of smuggled into prose.
 */
export function ComparisonVerdict({ options }: ComparisonVerdictProps) {
  const cols = options.length >= 3 ? 3 : 2;
  return (
    <CardGrid cols={cols} itemsStretch>
      {options.map((option) => (
        <HighlightCard key={option.name} highlight={option.ours} badgeLabel="Our take">
          <h3 className="font-heading text-cc-heading text-lg font-semibold">Choose {option.name} when</h3>
          <CheckList items={option.reasons} />
        </HighlightCard>
      ))}
    </CardGrid>
  );
}
