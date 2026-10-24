import Link from "next/link";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import { WarningIcon } from "@/src/icons/Warning";
import { BenchmarkColumnHint } from "@/src/components/benchmarks/BenchmarkColumnHint";
import { BenchmarkPreviewBadge } from "@/src/components/benchmarks/BenchmarkPreviewBadge";
import {
  formatLatency,
  formatMemory,
  formatNumber,
  formatReliability,
  isMeasured,
  isReliable,
  isWinner,
  type ResultsGroup,
} from "@/src/components/benchmarks/benchmarkView";
import {
  formatRuntimeTag,
  gatewayLink,
  isPreview,
  variantName,
  type BenchmarkCatalogEntry,
} from "@/src/helpers/benchmarkReport";

const theme = {
  heading: "font-heading text-h6 text-cc-heading pb-3 font-semibold",
  scroller: "overflow-x-auto",
  table: "w-full min-w-256 table-fixed text-left text-sm",
  caption: "sr-only",
  head: "text-cc-ink-dim border-cc-heading/60 border-b text-xs xl:whitespace-nowrap",
  headCell: "py-2.5 font-medium",
  cell: "py-2.5",
  startPad: "pl-4",
  endPad: "pr-4 sm:pr-5",
  row: "border-cc-card-border border-b",
  winnerRow:
    "border-cc-accent/40 bg-cc-accent/10 border-b [&>th]:shadow-[inset_3px_0_0_0_var(--color-cc-accent)]",
  failedRow:
    "border-cc-card-border bg-cc-danger/5 border-b [&>th]:shadow-[inset_3px_0_0_0_var(--color-cc-danger)]",
  name: "text-cc-heading font-semibold",
  nameLink:
    "text-cc-heading hover:text-cc-accent font-semibold underline-offset-4 hover:underline",
  tag: "border-cc-card-border text-cc-ink-dim rounded border px-1.5 font-mono text-[11px] leading-5 whitespace-nowrap",
  rps: "text-cc-heading font-mono font-semibold",
  winnerRps: "text-cc-accent font-mono font-semibold",
  failedRps: "text-cc-danger font-mono font-semibold",
  barTrack: "bg-cc-white/5 h-1 rounded-full",
  bar: "bg-cc-ink-dim/40 rounded-full",
  winnerBar: "bg-cc-accent rounded-full",
  failedBar: "bg-cc-danger/60 rounded-full",
  value: "font-mono text-xs",
  unit: "text-cc-ink-dim",
  danger: "text-cc-danger",
} as const;

interface BenchmarkResultsTableProps {
  readonly id: string;
  readonly title: string;
  readonly caption: string;
  readonly group: ResultsGroup;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
  readonly measuredRuns: number | undefined;
  readonly gatewayThreads: number | undefined;
  /** A sentence appended to the requests per second explanation, or undefined for none. */
  readonly throughputNote?: string;
}

const COLUMNS = [
  { label: "Router/gateway", width: "w-[25%]" },
  { label: "Median requests/s", width: "w-[15%]" },
  { label: "p95 latency", width: "w-[9%]" },
  { label: "p99.9 latency", width: "w-[10%]" },
  { label: "Peak memory", width: "w-[10%]" },
  { label: "Average CPU", width: "w-[11%]" },
  { label: "Reliability", width: "w-[9%]" },
  { label: "Run variance", width: "w-[11%]" },
] as const;

const columnHints = (
  gatewayThreads: number | undefined,
  throughputNote: string | undefined,
) => [
  undefined,
  `HTTP requests per second, as the median across the measured runs.${throughputNote ? ` ${throughputNote}` : ""}`,
  "95% of requests completed within this time.",
  "99.9% of requests completed within this time.",
  "The highest sampled resident memory (RSS) of the router process and its child processes.",
  `Average utilization of the router’s ${gatewayThreads ?? "allocated"} hardware threads. 100% means both of its cores are fully busy on both threads.`,
  "The share of requests that passed the checks for HTTP status, GraphQL errors and response structure.",
  "How much throughput varied across the measured runs, as the coefficient of variation.",
];

/** One composition table of a scenario: every row, its throughput bar, latency, resources, reliability and variance. */
export function BenchmarkResultsTable({
  id,
  title,
  caption,
  group,
  variants,
  measuredRuns,
  gatewayThreads,
  throughputNote,
}: BenchmarkResultsTableProps) {
  const hints = columnHints(gatewayThreads, throughputNote);
  const headingId = `group-${id}`;
  const notReported = (
    <span className={`${theme.unit} font-sans`} title="Not reported">
      —
    </span>
  );
  const pad = (index: number) =>
    index === 0
      ? `${theme.startPad} pr-3`
      : index === COLUMNS.length - 1
        ? `${theme.endPad} pl-3`
        : "px-3";
  const unit = (text: string) => (
    <>
      {" "}
      <span className={theme.unit}>{text}</span>
    </>
  );

  return (
    <div data-benchmark-scroll-anchor={`table-${id}`}>
      <h3 id={headingId} className={theme.heading}>
        {group.id === "composite-schema" ? (
          <Link
            href="https://graphql.github.io/graphql-federation-spec/"
            className="hover:text-cc-accent underline-offset-4 hover:underline"
          >
            {title}
          </Link>
        ) : (
          title
        )}
      </h3>
      <BenchmarkMobileResults
        group={group}
        variants={variants}
        measuredRuns={measuredRuns}
        hints={hints}
        headingId={headingId}
      />
      <div
        className={`hidden md:block ${theme.scroller}`}
        role="region"
        aria-labelledby={headingId}
        tabIndex={0}
      >
        <table className={theme.table}>
          <caption className={theme.caption}>{caption}</caption>
          <colgroup>
            {COLUMNS.map((column) => (
              <col key={column.label} className={column.width} />
            ))}
          </colgroup>
          <thead className={theme.head}>
            <tr>
              {COLUMNS.map((column, index) => (
                <th
                  key={column.label}
                  scope="col"
                  className={`${theme.headCell} ${pad(index)} ${index > 1 ? "text-right" : ""}`}
                >
                  <span className="whitespace-nowrap">
                    {column.label}
                    {hints[index] !== undefined && (
                      <BenchmarkColumnHint label={column.label}>
                        {hints[index]}
                      </BenchmarkColumnHint>
                    )}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {group.rows.map((row) => {
              const entry = variants.get(row.variant)!;
              const runs = row.throughput?.runsRps?.length ?? measuredRuns;
              const winner = isWinner(row, group);
              const failed = row.status === "failed";
              const value = `${theme.cell} ${theme.value} text-right whitespace-nowrap tabular-nums`;
              return (
                <tr
                  key={row.variant}
                  data-benchmark-scroll-anchor={`row-${id}-${row.variant}`}
                  className={
                    winner
                      ? theme.winnerRow
                      : failed
                        ? theme.failedRow
                        : theme.row
                  }
                >
                  <th
                    scope="row"
                    className={`${theme.cell} ${pad(0)} font-normal`}
                  >
                    <RouterIdentity entry={entry} />
                    {failed && isMeasured(row) && (
                      <BenchmarkFailureBadge
                        completedRuns={row.throughput.runsRps?.length}
                        measuredRuns={measuredRuns}
                      />
                    )}
                  </th>
                  {!isMeasured(row) ? (
                    <td
                      colSpan={COLUMNS.length - 1}
                      className="px-4 py-4 text-center text-sm"
                    >
                      <BenchmarkFailure />
                    </td>
                  ) : (
                    <>
                      <td className={`${theme.cell} ${pad(1)}`}>
                        <div className="flex items-center gap-3">
                          <span
                            className={`w-12 shrink-0 text-right tabular-nums ${winner ? theme.winnerRps : failed ? theme.failedRps : theme.rps}`}
                          >
                            {formatNumber(row.throughput.medianRps)}
                            {winner && (
                              <span className="sr-only">
                                , highest reported
                              </span>
                            )}
                          </span>
                          <div
                            className={`flex-1 overflow-hidden ${theme.barTrack}`}
                            aria-hidden="true"
                          >
                            <div
                              className={`h-full ${winner ? theme.winnerBar : failed ? theme.failedBar : theme.bar}`}
                              style={{
                                width: `${group.maxRps === 0 ? 0 : (row.throughput.medianRps / group.maxRps) * 100}%`,
                              }}
                            />
                          </div>
                        </div>
                      </td>
                      <td className={`${value} ${pad(2)}`}>
                        {row.latencyMs ? (
                          <>
                            {formatLatency(row.latencyMs.p95)}
                            {unit("ms")}
                          </>
                        ) : (
                          notReported
                        )}
                      </td>
                      <td className={`${value} ${pad(3)}`}>
                        {row.latencyMs ? (
                          <>
                            {formatLatency(row.latencyMs.p99_9)}
                            {unit("ms")}
                          </>
                        ) : (
                          notReported
                        )}
                      </td>
                      <td className={`${value} ${pad(4)}`}>
                        {row.memory ? (
                          <>
                            {formatMemory(row.memory.peakRssKiB)}
                            {unit("MiB")}
                          </>
                        ) : (
                          notReported
                        )}
                      </td>
                      <td className={`${value} ${pad(5)}`}>
                        {row.cpu ? (
                          <>
                            {row.cpu.avgUtilizationPercent.toFixed(2)}
                            {unit("%")}
                          </>
                        ) : (
                          notReported
                        )}
                      </td>
                      <td
                        className={`${value} ${pad(6)} ${isReliable(row) ? "" : theme.danger}`}
                      >
                        {formatReliability(
                          row.requests.successful,
                          row.requests.total,
                        )}
                      </td>
                      <td className={`${value} ${pad(7)}`}>
                        {runs !== 1 ? (
                          `±${row.throughput.cvPercent.toFixed(1)}%`
                        ) : (
                          <span title="Variation cannot be estimated from one measured run">
                            —
                          </span>
                        )}
                      </td>
                    </>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

interface RouterIdentityProps {
  readonly entry: BenchmarkCatalogEntry;
}

function RouterIdentity({ entry }: RouterIdentityProps) {
  const name = variantName(entry);
  const link = gatewayLink(entry.gateway);
  return (
    <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
      <span className="inline-flex items-center gap-1.5">
        {isPreview(
          entry.variant.version,
          entry.variant.platform,
          entry.variant.preview,
        ) && <BenchmarkPreviewBadge />}
        {link ? (
          <Link href={link} className={theme.nameLink}>
            {name}
          </Link>
        ) : (
          <span className={theme.name}>{name}</span>
        )}
      </span>
      {entry.variant.version !== null && (
        <span className={theme.tag}>{entry.variant.version}</span>
      )}
      <span className={theme.tag}>
        {formatRuntimeTag(entry.variant.platform)}
      </span>
    </div>
  );
}

function BenchmarkFailure() {
  return (
    <span className="text-cc-danger inline-flex items-center gap-1 leading-5 [&_button]:ml-0 [&_button]:translate-y-0">
      <WarningIcon className="relative top-px size-4 shrink-0" />
      <span>The benchmark failed to run for this router</span>
      <BenchmarkColumnHint label="benchmark failure">
        This could be due to benchmark misconfiguration and does not mean that
        the router is faulty.
      </BenchmarkColumnHint>
    </span>
  );
}

interface BenchmarkFailureBadgeProps {
  readonly completedRuns: number | undefined;
  readonly measuredRuns: number | undefined;
}

function BenchmarkFailureBadge({
  completedRuns,
  measuredRuns,
}: BenchmarkFailureBadgeProps) {
  return (
    <div className="text-cc-danger mt-1 flex items-center gap-1 text-xs leading-5 [&_button]:ml-0 [&_button]:translate-y-0">
      <WarningIcon className="size-3.5 shrink-0" />
      <span>
        {completedRuns !== undefined &&
        measuredRuns !== undefined &&
        completedRuns < measuredRuns
          ? `Failed: ${completedRuns} of ${measuredRuns} runs completed`
          : "Failed"}
      </span>
      <BenchmarkColumnHint label="benchmark failure">
        The benchmark failed for this router, so its results are partial and not
        ranked. This could be due to benchmark misconfiguration and does not
        mean that the router is faulty.
      </BenchmarkColumnHint>
    </div>
  );
}

interface BenchmarkMobileResultsProps {
  readonly group: ResultsGroup;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
  readonly measuredRuns: number | undefined;
  readonly hints: readonly (string | undefined)[];
  readonly headingId: string;
}

function BenchmarkMobileResults({
  group,
  variants,
  measuredRuns,
  hints,
  headingId,
}: BenchmarkMobileResultsProps) {
  return (
    <div className="md:hidden">
      <div className="border-cc-heading/60 text-cc-ink-dim flex items-center justify-between gap-3 border-b px-3 py-2.5 text-xs">
        <span>Router</span>
        <span className="whitespace-nowrap">
          Median requests/s
          <BenchmarkColumnHint label="Median requests/s">
            {hints[1]}
          </BenchmarkColumnHint>
        </span>
      </div>
      <ul aria-labelledby={headingId}>
        {group.rows.map((row) => {
          const entry = variants.get(row.variant)!;
          const winner = isWinner(row, group);
          const failed = row.status === "failed";
          const runs = row.throughput?.runsRps?.length ?? measuredRuns;
          const metrics = isMeasured(row)
            ? [
                row.latencyMs ? `${formatLatency(row.latencyMs.p95)} ms` : "—",
                row.latencyMs
                  ? `${formatLatency(row.latencyMs.p99_9)} ms`
                  : "—",
                row.memory ? `${formatMemory(row.memory.peakRssKiB)} MiB` : "—",
                row.cpu ? `${row.cpu.avgUtilizationPercent.toFixed(2)}%` : "—",
                formatReliability(row.requests.successful, row.requests.total),
                runs === 1 ? "—" : `±${row.throughput.cvPercent.toFixed(1)}%`,
              ]
            : [];
          return (
            <li
              key={row.variant}
              data-benchmark-scroll-anchor={`mobile-row-${headingId}-${row.variant}`}
              className={`border-cc-card-border border-b px-3 py-4 ${winner ? "bg-cc-accent/10 shadow-[inset_3px_0_0_0_var(--color-cc-accent)]" : failed ? "bg-cc-danger/5 shadow-[inset_3px_0_0_0_var(--color-cc-danger)]" : ""}`}
            >
              <div className="grid grid-cols-[minmax(0,1fr)_auto] items-start gap-3 text-sm">
                <div>
                  <RouterIdentity entry={entry} />
                  {failed && isMeasured(row) && (
                    <BenchmarkFailureBadge
                      completedRuns={row.throughput.runsRps?.length}
                      measuredRuns={measuredRuns}
                    />
                  )}
                </div>
                {isMeasured(row) && (
                  <div className="min-w-20 text-right">
                    <p
                      className={
                        winner
                          ? theme.winnerRps
                          : failed
                            ? theme.failedRps
                            : theme.rps
                      }
                    >
                      {formatNumber(row.throughput.medianRps)}
                      {winner && (
                        <span className="sr-only">, highest reported</span>
                      )}
                    </p>
                    <div
                      className={`${theme.barTrack} mt-2`}
                      aria-hidden="true"
                    >
                      <div
                        className={`h-full ${winner ? theme.winnerBar : failed ? theme.failedBar : theme.bar}`}
                        style={{
                          width: `${group.maxRps === 0 ? 0 : (row.throughput.medianRps / group.maxRps) * 100}%`,
                        }}
                      />
                    </div>
                  </div>
                )}
              </div>
              {!isMeasured(row) ? (
                <div className="mt-3 text-left text-sm">
                  <BenchmarkFailure />
                </div>
              ) : (
                <details className="group mt-3">
                  <summary className="text-cc-ink-dim hover:text-cc-prose focus-visible:outline-cc-accent flex w-fit cursor-pointer list-none items-center gap-1.5 rounded text-xs focus-visible:outline-2 [&::-webkit-details-marker]:hidden">
                    <ChevronDownIcon
                      aria-hidden="true"
                      className="size-2.5 fill-current group-open:rotate-180"
                    />
                    All metrics
                    <span className="sr-only"> for {variantName(entry)}</span>
                  </summary>
                  <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-3 text-xs">
                    {metrics.map((metric, index) => (
                      <div key={COLUMNS[index + 2].label}>
                        <dt className="text-cc-ink-dim">
                          {COLUMNS[index + 2].label}
                          <BenchmarkColumnHint label={COLUMNS[index + 2].label}>
                            {hints[index + 2]}
                          </BenchmarkColumnHint>
                        </dt>
                        <dd
                          className={`mt-1 font-mono ${index === 4 && !isReliable(row) ? theme.danger : "text-cc-heading"}`}
                          title={
                            index === 5 && runs === 1
                              ? "Variation cannot be estimated from one measured run"
                              : undefined
                          }
                        >
                          {metric}
                        </dd>
                      </div>
                    ))}
                  </dl>
                </details>
              )}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
