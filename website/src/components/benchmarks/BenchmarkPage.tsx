import { BenchmarkExplorer } from "@/src/components/benchmarks/BenchmarkExplorer";
import { benchmarkTechnology } from "@/src/components/benchmarks/benchmarkFilters";
import { BenchmarkHeader } from "@/src/components/benchmarks/BenchmarkHeader";
import { BenchmarkHistoryChart } from "@/src/components/benchmarks/BenchmarkHistoryChart";
import { BenchmarkTestSetup } from "@/src/components/benchmarks/BenchmarkTestSetup";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import {
  federationBenchmarkHistory as history,
  federationBenchmarks as report,
} from "@/src/data/federationBenchmarks";
import { BENCHMARK_PAGE as PAGE } from "@/src/components/benchmarks/benchmarkRoutes";
import {
  benchmarkPageDescription,
  isShownVariant,
} from "@/src/components/benchmarks/benchmarkView";
import {
  benchmarkVariants,
  type ScenarioId,
} from "@/src/helpers/benchmarkReport";
import {
  BENCHMARK_DATASET_ID,
  createBenchmarkDatasetNode,
} from "@/src/helpers/benchmarkStructuredData";
import { schemaRef } from "@/src/helpers/structuredData";

interface BenchmarkPageProps {
  readonly initialScenario: ScenarioId;
}

export function BenchmarkPage({ initialScenario }: BenchmarkPageProps) {
  const historyVariants = benchmarkVariants(history);
  const charts = Object.fromEntries(
    history.scenarios.flatMap((scenario) => {
      const shown = scenario.variants.filter((item) => {
        const entry = historyVariants.get(item.variant);
        return entry && isShownVariant(entry) && item.results.length > 0;
      });
      return shown.length > 0
        ? [
            [
              scenario.id,
              <BenchmarkHistoryChart
                key={scenario.id}
                id={`benchmark-${scenario.id}`}
                scenario={{ ...scenario, variants: shown }}
                variants={historyVariants}
              />,
            ],
          ]
        : [];
    }),
  );

  return (
    <>
      <PageStructuredData
        {...PAGE}
        description={benchmarkPageDescription(report)}
        dateModified={
          report.run.completedAt ?? report.run.startedAt ?? undefined
        }
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "GraphQL Federation Benchmarks" },
        ]}
        mainEntity={schemaRef(BENCHMARK_DATASET_ID)}
        additionalNodes={[createBenchmarkDatasetNode(report, history)]}
      />
      <article>
        <BenchmarkHeader report={report} />
        <div className="mt-10">
          <BenchmarkExplorer
            report={report}
            historyTechnologies={[
              ...new Set(
                [...historyVariants.values()].map(benchmarkTechnology),
              ),
            ]}
            charts={charts}
            initialScenario={initialScenario}
          />
        </div>
        <BenchmarkTestSetup report={report} history={history} />
      </article>
    </>
  );
}
