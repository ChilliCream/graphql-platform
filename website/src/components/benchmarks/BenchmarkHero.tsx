import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  BENCHMARK_REPOSITORY,
  type BenchmarkReport,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkHeroProps {
  readonly report: BenchmarkReport;
}

export function BenchmarkHero({ report }: BenchmarkHeroProps) {
  const date = new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(report.run.startedAt));

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
            the same federated workload. Explore throughput, test conditions,
            and the source behind every result.
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
            {report.example ? "Example report" : "Benchmark report"}
          </div>
          <p className="font-heading text-h5 text-cc-heading mt-3 font-semibold">
            Run #{report.run.number}
          </p>
          <time dateTime={report.run.startedAt} className="text-caption">
            {date}
          </time>
          <div className="border-cc-card-border mt-5 border-t pt-4 text-sm">
            <span
              className={`mr-2 inline-block size-2 rounded-full ${report.run.status === "complete" ? "bg-cc-success" : "bg-cc-warning"}`}
              aria-hidden="true"
            />
            {report.run.status === "complete"
              ? "Completed run"
              : "Snapshot of a run in progress"}
          </div>
          <a
            className="text-cc-accent mt-3 inline-block text-sm underline-offset-4 hover:underline"
            href={`${BENCHMARK_REPOSITORY}/actions/runs/${report.run.id}`}
          >
            Inspect this run ↗
          </a>
        </aside>
      </div>
      <div className="border-cc-card-border mt-12 grid grid-cols-2 gap-6 border-y py-6 md:grid-cols-4">
        {[
          ["04", "Connected subgraphs"],
          ["03", "Traffic scenarios"],
          ["k6", "Load generator"],
          ["RPS", "Requests per second"],
        ].map(([value, label]) => (
          <div key={label}>
            <div className="font-heading text-h5 text-cc-heading font-semibold tabular-nums">
              {value}
            </div>
            <div className="text-caption text-cc-ink-dim mt-1">{label}</div>
          </div>
        ))}
      </div>
      {report.example && (
        <p className="text-caption text-cc-ink-dim mt-5 max-w-4xl">
          This example JSON report was transcribed from published run #
          {report.run.number}. It is a fixed snapshot, not a live feed. Pending
          scenarios have no results in this snapshot.
        </p>
      )}
    </section>
  );
}
