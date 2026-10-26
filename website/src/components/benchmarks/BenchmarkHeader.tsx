import { benchmarkFacts } from "@/src/components/benchmarks/benchmarkView";
import { Link } from "@/src/design-system/Link";
import type { BenchmarkReport } from "@/src/helpers/benchmarkReport";

interface BenchmarkHeaderProps {
  readonly report: BenchmarkReport;
}

/** The page title and description with the number and date of the latest run. */
export function BenchmarkHeader({ report }: BenchmarkHeaderProps) {
  return (
    <header className="grid gap-8 pt-10 sm:pt-16 lg:grid-cols-[minmax(0,1fr)_18rem] lg:items-end lg:gap-16">
      <div>
        <h1 className="font-heading text-h4 sm:text-h3 lg:text-h2 text-cc-heading font-semibold">
          GraphQL Federation Benchmark
        </h1>
        <p className="text-h6 text-cc-prose mt-5 max-w-3xl font-normal">
          An open benchmark of GraphQL federation routers/gateways. Each router
          is evaluated under the same conditions, on the same machine with the
          same subgraphs and the same GraphQL operation, so you can compare
          throughput, latency, memory and CPU on equal terms.
          <br />
          New results are published every day from a public workflow that anyone
          can inspect or rerun on their own hardware.
        </p>
      </div>
      <RunPanel report={report} />
    </header>
  );
}

interface RunPanelProps {
  readonly report: BenchmarkReport;
}

function RunPanel({ report }: RunPanelProps) {
  const facts = benchmarkFacts(report);
  const run = `#${facts.runNumber}`;

  return (
    <div className="border-cc-card-border bg-cc-surface/80 w-full max-w-sm rounded-lg border lg:max-w-none">
      <p className="border-cc-card-border text-cc-heading border-b px-5 py-3 text-sm font-semibold">
        Latest run
      </p>
      <dl className="divide-cc-card-border divide-y px-5 text-sm">
        <div className="flex items-baseline justify-between gap-4 py-3">
          <dt className="text-cc-ink-dim">Run</dt>
          <dd className="tabular-nums">
            {facts.runUrl ? (
              <Link href={facts.runUrl}>{run}</Link>
            ) : (
              <span className="text-cc-heading">{run}</span>
            )}
          </dd>
        </div>
        {facts.date && (
          <div className="flex items-baseline justify-between gap-4 py-3">
            <dt className="text-cc-ink-dim">
              {facts.completed ? "Completed" : "Started"}
            </dt>
            <dd className="text-cc-heading tabular-nums">
              <time dateTime={facts.timestamp ?? undefined}>{facts.date}</time>
            </dd>
          </div>
        )}
      </dl>
    </div>
  );
}
