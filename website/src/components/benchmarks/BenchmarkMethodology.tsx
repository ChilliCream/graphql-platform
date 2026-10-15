import Link from "next/link";
import {
  BENCHMARK_REPOSITORY,
  type BenchmarkReport,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkMethodologyProps {
  readonly report: BenchmarkReport;
}

const TESTS = [
  {
    number: "01",
    title: "Real-world: downstream latency",
    id: "constant-latency",
    description:
      "Every subgraph request adds a simulated 4 ms I/O delay. Fifty concurrent virtual users repeatedly execute the nested query for 120 seconds. This measures end-to-end throughput when downstream calls take time, including the effect of each router's query plan.",
  },
  {
    number: "02",
    title: "Burst: increasing concurrency",
    id: "burst",
    description:
      "The load holds at 50 virtual users for 10 seconds, ramps to 500 over 40 seconds, then returns to 50 over 10 seconds. With no added subgraph I/O delay, this tests the router under rising concurrency. Its RPS is the run average, not the peak throughput at 500 users.",
  },
  {
    number: "03",
    title: "Synthetic: gateway overhead",
    id: "constant",
    description:
      "Fifty concurrent virtual users execute the same query for 120 seconds with no simulated subgraph delay. The subgraphs perform no database or external I/O work. This emphasizes router execution overhead; subgraph processing and local networking still contribute to the result.",
  },
] as const;

export function BenchmarkMethodology({ report }: BenchmarkMethodologyProps) {
  const source = `${BENCHMARK_REPOSITORY}/blob/${report.run.sourceCommit}`;

  return (
    <>
      <section
        id="methodology"
        className="border-cc-card-border scroll-mt-28 border-t py-14"
        aria-labelledby="methodology-heading"
      >
        <div className="grid gap-10 lg:grid-cols-[1fr_2fr]">
          <div>
            <div className="text-cc-accent mb-3 font-mono text-xs tracking-widest uppercase">
              The methodology
            </div>
            <h2
              id="methodology-heading"
              className="font-heading text-h4 text-cc-heading font-semibold"
            >
              One workload.
              <br />
              Three perspectives.
            </h2>
            <p className="text-cc-prose mt-5">
              All scenarios use the same nested GraphQL operation across
              Accounts, Inventory, Products, and Reviews.
            </p>
            <p className="mt-4">
              The query follows users to reviews, products, other reviews, and
              their authors. A second root field fetches top products with
              reviews and author details. It exercises entity resolution,
              dependent subgraph fetches, and response assembly.
            </p>
          </div>
          <div className="divide-cc-card-border divide-y">
            {TESTS.map((test) => (
              <article
                key={test.id}
                className="flex gap-5 py-6 first:pt-0 last:pb-0"
              >
                <span
                  className="text-cc-accent pt-1 font-mono text-xs"
                  aria-hidden="true"
                >
                  {test.number}
                </span>
                <div>
                  <h3 className="text-cc-heading text-lg font-semibold">
                    {test.title}
                  </h3>
                  <p className="text-cc-prose mt-3">{test.description}</p>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>
      <section
        id="hardware"
        className="border-cc-card-border scroll-mt-28 border-t py-14"
        aria-labelledby="hardware-heading"
      >
        <div className="grid gap-8 lg:grid-cols-2">
          <div className="border-cc-card-border bg-cc-surface/60 rounded-2xl border p-6 sm:p-8">
            <div className="text-cc-accent mb-3 font-mono text-xs tracking-widest uppercase">
              The environment
            </div>
            <h2
              id="hardware-heading"
              className="font-heading text-h5 text-cc-heading font-semibold"
            >
              Hardware &amp; software
            </h2>
            <dl className="divide-cc-card-border mt-6 divide-y text-sm">
              {[
                ["Processor", report.hardware.cpu],
                ["Logical CPUs reported", String(report.hardware.logicalCpus)],
                ["Memory reported", `${report.hardware.memoryGb} GB`],
                ["Operating system", report.hardware.os],
                ["Benchmark host", report.hardware.host],
                [
                  "Subgraphs",
                  `${report.subgraphs.implementation} ${report.subgraphs.version} (.NET)`,
                ],
              ].map(([label, value]) => (
                <div
                  key={label}
                  className="grid grid-cols-[8rem_1fr] gap-4 py-3"
                >
                  <dt className="text-cc-ink-dim">{label}</dt>
                  <dd className="text-cc-heading min-w-0 wrap-break-word">
                    {value}
                  </dd>
                </div>
              ))}
            </dl>
            <p className="text-caption text-cc-ink-dim mt-4">
              Hardware values are recorded by the benchmark host. The harness
              assigns CPU affinity to the load generator, router, and subgraphs
              using its{" "}
              <a
                href={`${source}/profiles.json`}
                className="text-cc-accent underline-offset-4 hover:underline"
              >
                CPU profiles
              </a>
              .
            </p>
          </div>
          <div className="py-2 lg:p-6">
            <h2 className="font-heading text-h5 text-cc-heading font-semibold">
              How to read the comparison
            </h2>
            <div className="text-cc-prose mt-5 space-y-5">
              <p>
                RPS means HTTP requests per second reported by k6. The table
                uses the median across measured runs. Best and worst show the
                range; CV is the coefficient of variation across those runs. A
                full-duration warmup run is discarded before measurements begin.
              </p>
              <p>
                Peak memory is the highest sampled gateway process RSS in the
                measured run selected for median RPS, including its monitored
                child processes. Values are converted from KiB to MiB and do not
                include the load generator or subgraph processes. Missing
                measurements are shown as not reported.
              </p>
              <p>
                k6 checks HTTP status, GraphQL errors, and the expected response
                structure. Invalid responses and failed runs receive a status
                instead of a throughput ranking. These checks cover this
                operation, not general GraphQL conformance.
              </p>
              <p>
                Fusion and feddi use GraphQL Federation; the other
                configurations use Apollo Federation. The separately labeled
                Fusion Apollo Federation preview tests that protocol. The same
                subgraph implementation and logical workload are used across
                both families.
              </p>
              <p>
                These results describe this workload, concurrency,
                configuration, and hardware. They do not measure production
                database behavior or predict throughput for a different graph.
                Throughput alone does not show tail latency or recovery after a
                burst.
              </p>
            </div>
            <div className="mt-6 flex flex-wrap gap-x-6 gap-y-3 text-sm">
              <a
                href={`${source}/k6/k6.js`}
                className="text-cc-accent underline-offset-4 hover:underline"
              >
                Read the query &amp; checks ↗
              </a>
              <a
                href={`${source}/README.md`}
                className="text-cc-accent underline-offset-4 hover:underline"
              >
                Reproduce the benchmark ↗
              </a>
            </div>
          </div>
        </div>
      </section>
      <section className="border-cc-card-border border-t py-12">
        <p className="text-caption text-cc-ink-dim max-w-4xl">
          Published by ChilliCream, the team behind Fusion. The benchmark suite
          builds on{" "}
          <a
            className="text-cc-accent underline-offset-4 hover:underline"
            href="https://github.com/graphql-hive/graphql-gateways-benchmark"
          >
            The Guild&apos;s GraphQL gateway benchmarks
          </a>
          . Measurements, configurations, and source reports are available for
          inspection.
        </p>
        <div className="mt-5 flex flex-wrap gap-x-6 gap-y-3 text-sm">
          <Link
            href="/docs/fusion"
            className="text-cc-accent underline-offset-4 hover:underline"
          >
            Explore Fusion documentation →
          </Link>
          <a
            href={`${BENCHMARK_REPOSITORY}/issues`}
            className="text-cc-accent underline-offset-4 hover:underline"
          >
            Discuss the benchmark ↗
          </a>
        </div>
      </section>
    </>
  );
}
