import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  BENCHMARK_REPOSITORY,
  benchmarkRunId,
  type BenchmarkReport,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkHeroProps {
  readonly report: BenchmarkReport;
}

export function BenchmarkHero({ report }: BenchmarkHeroProps) {
  const runId = benchmarkRunId(report);
  const { completedAt, startedAt } = report.run;
  const timestamp = completedAt ?? startedAt;
  const date =
    timestamp === null
      ? undefined
      : new Intl.DateTimeFormat("en-GB", {
          day: "numeric",
          month: "long",
          year: "numeric",
          timeZone: "UTC",
        }).format(new Date(timestamp));

  return (
    <section className="pt-14 pb-12 sm:pt-20 sm:pb-16">
      <div className="text-cc-accent mb-5 font-mono text-xs tracking-widest uppercase">
        Open benchmarks / GraphQL federation
      </div>
      <div className="grid items-end gap-10 lg:grid-cols-[1fr_19rem]">
        <div>
          <h1 className="font-heading text-h4 text-cc-heading sm:text-h2 max-w-4xl font-semibold tracking-tight">
            GraphQL router &amp; gateway{" "}
            <span className="text-cc-accent">performance benchmarks.</span>
          </h1>
          <p className="text-cc-prose mt-6 max-w-3xl text-lg">
            Compare Fusion, Apollo, Hive, Cosmo, Grafbase, Caliban, and feddi on
            the same federated workload. Explore throughput, latency, resource
            usage, test conditions, and the source behind every result.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="#results">Explore the results ↓</SolidButton>
            <OutlineButton href={BENCHMARK_REPOSITORY}>
              View benchmark source ↗
            </OutlineButton>
          </div>
        </div>
        <aside
          className="border-cc-card-border bg-cc-surface/70 rounded-2xl border p-6"
          aria-label="Report provenance"
        >
          <div className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
            Benchmark report
          </div>
          <p className="font-heading text-h5 text-cc-heading mt-3 font-semibold">
            Run #{report.run.number}
          </p>
          {timestamp !== null && (
            <div className="border-cc-card-border mt-5 border-t pt-4 text-sm">
              <span
                className={`mr-2 inline-block size-2 rounded-full ${completedAt ? "bg-cc-success" : "bg-cc-warning"}`}
                aria-hidden="true"
              />
              {completedAt ? "Completed" : "Started"}{" "}
              <time dateTime={timestamp}>{date}</time>
            </div>
          )}
          {runId && (
            <a
              className="text-cc-accent mt-3 inline-block text-sm underline-offset-4 hover:underline"
              href={`${BENCHMARK_REPOSITORY}/actions/runs/${runId}`}
            >
              Inspect this run ↗
            </a>
          )}
        </aside>
      </div>
    </section>
  );
}
