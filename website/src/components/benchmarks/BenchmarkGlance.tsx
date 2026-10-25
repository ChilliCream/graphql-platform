import {
  BenchmarkVerdictSummary,
  fastestSentence,
} from "@/src/components/benchmarks/BenchmarkVerdict";
import {
  SECTION_HEADING,
  SUBSECTION_HEADING,
} from "@/src/components/benchmarks/benchmarkHeadings";
import {
  COMPOSITION_GROUPS,
  compositionReport,
} from "@/src/components/benchmarks/benchmarkView";
import type {
  BenchmarkCatalogEntry,
  BenchmarkReport,
  ScenarioId,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkGlanceProps {
  readonly report: BenchmarkReport;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
  /** The variants shown in the tables of each scenario. */
  readonly visibleRows: ReadonlyMap<ScenarioId, ReadonlySet<string>>;
}

/** The results summary across every scenario, one card per composition. */
export function BenchmarkGlance({
  report,
  variants,
  visibleRows,
}: BenchmarkGlanceProps) {
  const answer = fastestSentence(report);
  return (
    <section aria-labelledby="benchmark-results-heading">
      <h2 id="benchmark-results-heading" className={SECTION_HEADING}>
        Results at a glance
      </h2>
      {answer && (
        <p className="text-body text-cc-prose mt-3 max-w-4xl">{answer}</p>
      )}
      <div className="mt-6 grid gap-10 xl:grid-cols-2 xl:gap-12">
        {COMPOSITION_GROUPS.map((group) => (
          <section
            key={group.id}
            aria-labelledby={`benchmark-results-${group.id}-heading`}
          >
            <h3
              id={`benchmark-results-${group.id}-heading`}
              className={SUBSECTION_HEADING}
            >
              {group.name}
            </h3>
            <BenchmarkVerdictSummary
              id={`benchmark-results-${group.id}`}
              report={compositionReport(report, variants, group.id)}
              visibleRows={visibleRows}
            />
          </section>
        ))}
      </div>
    </section>
  );
}
