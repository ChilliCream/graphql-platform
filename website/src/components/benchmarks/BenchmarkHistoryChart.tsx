import type { CSSProperties } from "react";
import { benchmarkTechnology } from "@/src/components/benchmarks/benchmarkFilters";
import { BenchmarkHistoryChartFrame } from "@/src/components/benchmarks/BenchmarkHistoryChartFrame";
import { BenchmarkHistoryLegendButton } from "@/src/components/benchmarks/BenchmarkHistoryLegendButton";
import {
  COMPOSITION_GROUPS,
  formatChange,
  formatNumber,
  historyTimeScale,
  formatHistoryDate,
  type HistoryPoint,
  historyScale,
  historySeries,
  latestCompletedPoint,
  seriesChange,
  type HistorySeries,
} from "@/src/components/benchmarks/benchmarkView";
import { BenchmarkPreviewBadge } from "@/src/components/benchmarks/BenchmarkPreviewBadge";
import { WarningIcon } from "@/src/icons/Warning";
import type {
  BenchmarkCatalogEntry,
  BenchmarkHistoryScenario,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkHistoryChartProps {
  readonly id: string;
  readonly scenario: BenchmarkHistoryScenario;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
}

/** The throughput history of a scenario with every variant of `scenario`, scaled to the released series. The frame hides the series that the filters exclude. */
export function BenchmarkHistoryChart({
  id,
  scenario,
  variants,
}: BenchmarkHistoryChartProps) {
  const series = historySeries(scenario, variants);
  if (series.length === 0) {
    return null;
  }
  const rps = (items: readonly HistorySeries[]) =>
    items.flatMap((item) => item.points.map((point) => point.rps));
  const released = series.filter((item) => !item.preview);
  const scale = historyScale(rps(released.length > 0 ? released : series));
  const time = historyTimeScale(
    series.flatMap((item) => item.points.map((point) => point.timestamp)),
  );
  const dates = time.ticks;
  const round = (position: number) => Math.round(position * 100) / 100;
  const x = (timestamp: number) => round(time.x(timestamp));
  const y = (value: number) => round(scale.y(value));
  const includeYear =
    new Date(dates[0]).getUTCFullYear() !==
    new Date(dates.at(-1)!).getUTCFullYear();
  const dateLabel = (timestamp: number) =>
    formatHistoryDate(timestamp, includeYear);
  const maxPoints = Math.max(...series.map((item) => item.points.length));
  const { measuredRuns } = scenario.config;
  const failureNote = (point: HistoryPoint) =>
    !point.failed
      ? undefined
      : measuredRuns !== undefined && point.completedRuns < measuredRuns
        ? `Only ${point.completedRuns} of ${measuredRuns} runs completed`
        : "Failed";
  const compositionName = (item: HistorySeries) =>
    COMPOSITION_GROUPS.find((group) => group.id === item.composition)?.name;

  return (
    <BenchmarkHistoryChartFrame
      id={id}
      series={series.map((item) => ({
        id: item.id,
        composition: item.composition,
        technology: benchmarkTechnology(variants.get(item.id)!),
        preview: item.preview,
        low: Math.min(...item.points.map((point) => point.rps)),
        high: Math.max(...item.points.map((point) => point.rps)),
      }))}
      domain={[scale.ticks[0], scale.ticks.at(-1)!]}
      dates={dates}
      plot={
        <>
          {series.map((item) => (
            <svg
              key={item.id}
              data-series={item.id}
              className="pointer-events-none absolute inset-0 h-full w-full overflow-visible [&_line]:[vector-effect:non-scaling-stroke]"
              viewBox="0 0 100 100"
              preserveAspectRatio="none"
              aria-hidden="true"
              stroke={item.color}
              strokeWidth={1.5}
              strokeOpacity={0.8}
              strokeLinecap="round"
            >
              {item.points.slice(1).map((point, index) => {
                const previous = item.points[index];
                return (
                  <line
                    key={point.runNumber}
                    x1={x(previous.timestamp)}
                    y1={y(previous.rps)}
                    x2={x(point.timestamp)}
                    y2={y(point.rps)}
                    strokeDasharray={
                      item.dashed || previous.failed || point.failed
                        ? "4 4"
                        : undefined
                    }
                  />
                );
              })}
            </svg>
          ))}
          <div
            className="focus-visible:*:outline-cc-accent contents *:pointer-events-auto *:absolute *:top-[calc(var(--y)*1%)] *:left-[calc(var(--x)*1%)] *:flex *:size-5 *:w-(--point-width) *:-translate-1/2 *:cursor-pointer *:items-center *:justify-center *:rounded-full focus-visible:*:outline-2"
            style={
              {
                "--point-width": `clamp(3px, ${round(100 / maxPoints)}%, 20px)`,
              } as CSSProperties
            }
          >
            {series.flatMap((item) =>
              item.points.map((point) => {
                const note = failureNote(point);
                return (
                  <button
                    key={`${item.id}-${point.runNumber}`}
                    type="button"
                    data-series={item.id}
                    data-point={point.runNumber}
                    aria-label={`${item.name}, ${dateLabel(point.timestamp)}, ${point.runtime}, ${compositionName(item)}, version ${point.version}, ${formatNumber(point.rps)} requests/s${point.failed ? ", failed" : ""}`}
                    style={
                      {
                        "--x": x(point.timestamp),
                        "--y": y(point.rps),
                      } as CSSProperties
                    }
                  >
                    <span className="bg-cc-bg ring-cc-bg flex shrink-0 rounded-full ring-2">
                      {point.failed ? (
                        <WarningIcon
                          className={`text-cc-danger block ${maxPoints > 60 ? "size-2.5" : "size-3"}`}
                          strokeWidth={2.5}
                        />
                      ) : (
                        <span
                          className={`block ${maxPoints > 60 ? "size-0.75" : "size-1.25"} rounded-full border`}
                          style={{
                            borderColor: item.color,
                            backgroundColor: item.dashed
                              ? "var(--color-cc-bg)"
                              : item.color,
                          }}
                        />
                      )}
                    </span>
                    <span data-tooltip className="hidden">
                      <span>
                        {item.name} ({point.runtime})
                      </span>
                      <span>{formatNumber(point.rps)} requests/s</span>
                      <span>Version {point.version}</span>
                      <span>{compositionName(item)}</span>
                      {note && <span>{note}</span>}
                    </span>
                  </button>
                );
              }),
            )}
          </div>
        </>
      }
      legend={COMPOSITION_GROUPS.map((group) => {
        const members = series
          .filter((item) => item.composition === group.id)
          .toSorted((a, b) => b.points.at(-1)!.rps - a.points.at(-1)!.rps);
        if (members.length === 0) {
          return null;
        }
        return (
          <div key={group.id} data-legend-group={group.id}>
            <p
              id={`${id}-legend-${group.id}`}
              className="text-cc-ink-dim mb-1.5 text-xs font-medium"
            >
              {group.name}
            </p>
            <ul
              aria-labelledby={`${id}-legend-${group.id}`}
              className="-mx-2 flex flex-wrap gap-x-1 gap-y-0.5"
            >
              {members.map((item) => (
                <BenchmarkHistoryLegendButton key={item.id} seriesId={item.id}>
                  <LegendEntry series={item} />
                </BenchmarkHistoryLegendButton>
              ))}
            </ul>
          </div>
        );
      })}
      table={
        <table>
          <caption>
            Median requests per second by run date, oldest first.
          </caption>
          <thead>
            <tr>
              <th scope="col">Router</th>
              <th scope="col">
                Run dates, versions and median requests per second
              </th>
            </tr>
          </thead>
          <tbody>
            {series.map((item) => (
              <tr
                key={item.id}
                data-series={item.id}
                data-llms-ignore={item.preview ? "" : undefined}
              >
                <th scope="row">
                  {item.name} ({item.runtime})
                </th>
                <td>
                  {item.points
                    .map(
                      (point) =>
                        `${new Date(point.timestamp).toISOString()}: ${point.version}, ${formatNumber(point.rps)} requests/s${point.failed ? ", failed" : ""}`,
                    )
                    .join(", ")}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
    />
  );
}

interface LegendEntryProps {
  readonly series: HistorySeries;
}

function LegendEntry({ series }: LegendEntryProps) {
  const latest = series.points.at(-1)!;
  const newest = latestCompletedPoint(series) ?? latest;
  const change = seriesChange(series);

  return (
    <>
      <svg
        className="h-3 w-6 shrink-0 overflow-visible"
        viewBox="0 0 24 12"
        aria-hidden="true"
      >
        <line
          x1="1"
          y1="6"
          x2="23"
          y2="6"
          stroke={series.color}
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeDasharray={series.dashed ? "3 3" : undefined}
        />
        <circle
          cx="12"
          cy="6"
          r="4"
          stroke={series.color}
          strokeWidth="2"
          fill={series.dashed ? "var(--color-cc-bg)" : series.color}
        />
      </svg>
      <span
        className="min-w-0 flex-1"
        data-nosnippet={series.preview ? "" : undefined}
      >
        <span className="flex min-w-0 items-center gap-1">
          {series.preview && <BenchmarkPreviewBadge />}
          <span
            className="text-cc-heading min-w-0 truncate"
            title={series.name}
          >
            {series.name}
          </span>
          <span
            className="border-cc-card-border text-cc-ink-dim max-w-1/2 min-w-0 shrink-0 truncate rounded border px-1.5 font-mono text-[11px] leading-5"
            title={series.runtime}
          >
            {series.runtime}
          </span>
        </span>
        <span className="mt-0.5 flex items-baseline gap-2 text-xs tabular-nums">
          <span className="text-cc-heading font-semibold">
            {formatNumber(newest.rps)}
          </span>
          <span className="text-cc-ink-dim">requests/s</span>
          {change !== undefined && (
            <span
              className={change >= 0 ? "text-cc-success" : "text-cc-danger"}
              title={`Change from ${series.points.find((point) => !point.failed)!.version} to ${newest.version}`}
            >
              <span aria-hidden="true">{change >= 0 ? "▲" : "▼"}</span>{" "}
              <span className="sr-only">{change >= 0 ? "up" : "down"}</span>
              {formatChange(change)}
            </span>
          )}
          {latest.failed && (
            <span
              className="text-cc-danger self-center"
              title="Latest run failed"
            >
              <WarningIcon className="size-3.5" />
              <span className="sr-only">Latest run failed</span>
            </span>
          )}
        </span>
      </span>
    </>
  );
}
