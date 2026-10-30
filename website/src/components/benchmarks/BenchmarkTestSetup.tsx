import { BenchmarkCpuMap } from "@/src/components/benchmarks/BenchmarkCpuMap";
import {
  SECTION_HEADING,
  SUBSECTION_HEADING,
} from "@/src/components/benchmarks/benchmarkHeadings";
import {
  benchmarkFacts,
  formatDate,
} from "@/src/components/benchmarks/benchmarkView";
import { Link } from "@/src/design-system/Link";
import type {
  BenchmarkHistory,
  BenchmarkReport,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkTestSetupProps {
  readonly report: BenchmarkReport;
  readonly history: BenchmarkHistory;
}

const NUMBER_WORDS = [
  "zero",
  "one",
  "two",
  "three",
  "four",
  "five",
  "six",
  "seven",
  "eight",
  "nine",
];

/** Spells out numbers below ten, as in running text. */
const countText = (count: number) => NUMBER_WORDS[count] ?? String(count);

interface ProcedureStep {
  readonly title: string;
  readonly text: string;
}

type SpecRow = readonly [label: string, value: string];

/** The "Test conditions" section: the procedure and the test machine in one panel, with the publisher note below. */
export function BenchmarkTestSetup({
  report,
  history,
}: BenchmarkTestSetupProps) {
  const facts = benchmarkFacts(report);
  const firstRun = history.scenarios
    .flatMap((scenario) => scenario.variants)
    .flatMap((variant) => variant.results)
    .map((result) => result.run.completedAt ?? result.run.startedAt)
    .filter((value): value is string => typeof value === "string")
    .toSorted((a, b) => Date.parse(a) - Date.parse(b))[0];
  const warmups = facts.warmupRuns ?? 1;
  const runs = facts.measuredRuns;

  const steps: readonly ProcedureStep[] = [
    {
      title: "Start",
      text: "The router runs on 2 dedicated physical cores (4 hardware threads). CPU boost is disabled to reduce clock-speed variation.",
    },
    {
      title: "Warm up",
      text: `${warmups === 1 ? "One run" : `${warmups} runs`} of the full scenario length ${warmups === 1 ? "primes" : "prime"} the router before anything is measured. ${warmups === 1 ? "Its results are" : "Their results are"} discarded.`,
    },
    {
      title: "Measure",
      text: `${runs === undefined ? "Several" : runs} more runs of the same length follow. Each one sends the same GraphQL operation with the load profile of the scenario.`,
    },
    {
      title: "Check responses",
      text: "A request only counts as a success when it returns HTTP 200 without GraphQL errors.",
    },
    {
      title: "Report the median",
      text: `Results use the median requests per second of the ${runs === undefined ? "measured" : runs} runs, and run variance shows how much the runs differed.`,
    },
  ];

  const hardware: readonly SpecRow[] = [
    ["Processor", facts.cpu],
    ["CPU topology", facts.cpuTopology],
    ["Memory", `${facts.memoryGb} GB`],
  ];
  const software: readonly SpecRow[] = [
    ["Operating system", facts.os],
    ["Subgraphs", facts.subgraphs],
  ];

  return (
    <section
      aria-labelledby={`benchmark-setup-heading`}
      className="mt-24 sm:mt-28"
    >
      <h2 id={`benchmark-setup-heading`} className={SECTION_HEADING}>
        Test conditions
      </h2>
      <div className="border-cc-card-border mt-6 rounded-lg border">
        <div className="divide-cc-card-border grid divide-y lg:grid-cols-2 lg:divide-x lg:divide-y-0">
          <div className="flex flex-col px-5 py-6 lg:pr-8">
            <h3 className={SUBSECTION_HEADING}>Procedure</h3>
            <p className="text-caption text-cc-prose mt-1">
              Every scenario runs these steps in order, one router at a time.
            </p>
            <ol className="mt-5 flex flex-1 flex-col gap-4 lg:justify-between">
              {steps.map((step, index) => (
                <li
                  key={step.title}
                  className="grid grid-cols-[1.5rem_minmax(0,1fr)] gap-x-2"
                >
                  <span
                    aria-hidden="true"
                    className="text-cc-ink-dim text-sm tabular-nums"
                  >
                    {index + 1}
                  </span>
                  <div>
                    <p className="text-cc-heading text-sm font-semibold">
                      {step.title}
                    </p>
                    <p className="text-caption text-cc-prose mt-0.5">
                      {step.text}
                    </p>
                  </div>
                </li>
              ))}
            </ol>
          </div>
          <div className="px-5 py-6 lg:pl-8">
            <SpecGroup
              id={`benchmark-hardware`}
              title="Hardware"
              rows={hardware}
            />
            <SpecGroup
              id={`benchmark-software`}
              title="Software"
              rows={software}
              className="mt-8"
            />
            <BenchmarkCpuMap
              id={`benchmark-cpus`}
              report={report}
              className="mt-8"
            />
          </div>
        </div>
      </div>
      <p className="text-caption text-cc-ink-dim mt-4">
        Published by ChilliCream, the team behind{" "}
        <Link href={facts.links.fusion}>Fusion</Link>. Built on{" "}
        <Link href={facts.links.guild}>
          The Guild&apos;s gateway benchmarks
        </Link>{" "}
        and <Link href={facts.links.repository}>run daily</Link> on{" "}
        {countText(report.gateways.length)} routers
        {firstRun ? ` since ${formatDate(firstRun)}` : ""}.
      </p>
    </section>
  );
}

interface SpecGroupProps {
  readonly id: string;
  readonly title: string;
  readonly rows: readonly SpecRow[];
  readonly className?: string;
}

function SpecGroup({ id, title, rows, className = "" }: SpecGroupProps) {
  return (
    <div className={className}>
      <h3 id={id} className={SUBSECTION_HEADING}>
        {title}
      </h3>
      <dl
        aria-labelledby={id}
        className="divide-cc-card-border mt-2 divide-y text-sm"
      >
        {rows.map(([label, value]) => (
          <div
            key={label}
            className="grid gap-1 py-3 sm:grid-cols-[minmax(0,2fr)_minmax(0,3fr)] sm:gap-4"
          >
            <dt className="text-cc-ink-dim">{label}</dt>
            <dd className="text-cc-heading wrap-break-word">{value}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}
