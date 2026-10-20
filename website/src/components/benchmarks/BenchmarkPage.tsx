import { BenchmarkExplorer } from "@/src/components/benchmarks/BenchmarkExplorer";
import { BenchmarkHeader } from "@/src/components/benchmarks/BenchmarkHeader";
import { BenchmarkTestSetup } from "@/src/components/benchmarks/BenchmarkTestSetup";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import {
  federationBenchmarkHistory as history,
  federationBenchmarks as report,
} from "@/src/data/federationBenchmarks";
import { BENCHMARK_PAGE as PAGE } from "@/src/components/benchmarks/benchmarkRoutes";
import type { ScenarioId } from "@/src/helpers/benchmarkReport";

interface BenchmarkPageProps {
  readonly initialScenario: ScenarioId;
}

export function BenchmarkPage({ initialScenario }: BenchmarkPageProps) {
  return (
    <>
      <PageStructuredData
        {...PAGE}
        dateModified={
          report.run.completedAt ?? report.run.startedAt ?? undefined
        }
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "GraphQL Federation Benchmarks" },
        ]}
      />
      <article>
        <BenchmarkHeader report={report} />
        <div className="mt-10">
          <BenchmarkExplorer
            report={report}
            history={history}
            initialScenario={initialScenario}
          />
        </div>
        <BenchmarkTestSetup report={report} />
      </article>
    </>
  );
}
