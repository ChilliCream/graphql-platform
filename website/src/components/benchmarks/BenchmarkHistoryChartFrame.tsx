"use client";

import { createContext, memo, useContext, type CSSProperties } from "react";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";
import { SUBSECTION_HEADING } from "@/src/components/benchmarks/benchmarkHeadings";
import {
  COMPOSITION_GROUPS,
  formatHistoryDate,
  formatNumber,
  historyDateIntervals,
  historyScale,
  historyTimeScale,
} from "@/src/components/benchmarks/benchmarkView";
import { BenchmarkHistoryLegend } from "@/src/components/benchmarks/BenchmarkHistoryLegend";
import { useSeriesHighlight } from "@/src/components/benchmarks/useSeriesHighlight";
import { WarningIcon } from "@/src/icons/Warning";
import type { Composition } from "@/src/helpers/benchmarkReport";

/** One run of a history chart series. */
export interface HistoryChartPoint {
  /** The index of the run date in the chart's dates. */
  readonly date: number;
  readonly rps: number;
  readonly version: string;
  /** The runtime of the run, absent when it is the runtime of the series. */
  readonly runtime?: string;
  /** Why the run failed, absent when it succeeded. */
  readonly failure?: string;
}

/** One series of a history chart. */
export interface HistoryChartSeries {
  readonly id: string;
  readonly name: string;
  /** The runtime of the newest version. */
  readonly runtime: string;
  readonly composition: Composition;
  readonly technology: string;
  readonly preview: boolean;
  readonly color: string;
  /** Whether the line is dashed and the points hollow. */
  readonly dashed: boolean;
  /** The requests per second of the newest run that did not fail, or of the newest run when every run failed. */
  readonly newestRps: number;
  /** The relative throughput change between the versions of the oldest and the newest run that did not fail, absent for fewer than two such runs. */
  readonly change?: {
    readonly value: number;
    readonly from: string;
    readonly to: string;
  };
  /** Whether the newest run failed, absent when it did not. */
  readonly latestFailed?: true;
  /** The runs of the series, oldest first. */
  readonly points: readonly HistoryChartPoint[];
}

interface HistoryChartPin {
  readonly pinned: string | undefined;
  readonly toggle: (seriesId: string) => void;
}

const Pin = createContext<HistoryChartPin | null>(null);

/** The pinned series of the enclosing history chart. */
export function useHistoryChartPin() {
  const pin = useContext(Pin);
  if (!pin) {
    throw new Error("History chart pins require a BenchmarkHistoryChartFrame.");
  }
  return pin;
}

interface BenchmarkHistoryChartFrameProps {
  readonly id: string;
  readonly series: readonly HistoryChartSeries[];
  /** The requests per second at the bottom and the top of the plot before it is fitted to the shown series. */
  readonly domain: readonly [low: number, high: number];
  /** Every charted run date, oldest first. */
  readonly dates: readonly number[];
}

const compact = new Intl.NumberFormat("en-US", {
  notation: "compact",
  maximumFractionDigits: 1,
});

const quote = (value: string) => JSON.stringify(value);

const round = (position: number) => Math.round(position * 100) / 100;

// The chart sets `--highlight` and `--highlight-<index>` for its highlighted series, which sets `--active` on that series and `--dim` on the others.
// `--center` is the position of a point within its button, which spans the plot height in a pinned series.
const CHART_STYLES = `
[data-history-chart] svg[data-series]{z-index:var(--active,0);opacity:calc(1 - var(--dim,0)*.85);transition:opacity .1s}
[data-history-chart] svg[data-series] path{stroke-width:calc(1.5 + var(--active,0)*.5)}
[data-history-chart] [data-point]{z-index:calc(var(--active,0)*2)}
[data-history-chart] [data-point]:is(:hover,:focus-visible){z-index:3}
[data-history-chart] [data-point]::before{content:"";position:absolute;top:var(--center,50%);left:50%;translate:-50% -50%;width:var(--point-size);height:var(--point-size);border:1px solid var(--point-color);border-radius:9999px;background:var(--point-fill);box-shadow:0 0 0 calc(2px - var(--dim,0)*2px) var(--color-cc-bg);opacity:calc(1 - var(--dim,0)*.8);transition:opacity .1s}
[data-history-chart] [data-point][data-failed]::before{display:none}
[data-history-chart] [data-point]>svg{position:absolute;top:var(--center,50%);left:50%;translate:-50% -50%;opacity:calc(1 - var(--dim,0)*.8)}
[data-history-chart] [data-point]:is(:hover,:focus-visible)::after{content:"";position:absolute;z-index:-1;left:50%;top:calc(var(--center,50%) - (var(--fit-top) + var(--y)*var(--fit-height)/100)*1cqh);height:100cqh;border-left:1px dashed var(--color-cc-ink-dim);pointer-events:none}
[data-history-chart] [data-point]>span{display:none}
[data-history-chart] [data-point]:is(:hover,:focus-visible)>span{--below:clamp(0,(25 - var(--fit-top) - var(--y)*var(--fit-height)/100)*1000,1);--start:clamp(0,(15 - var(--x))*1000,1);--end:clamp(0,(var(--x) - 85)*1000,1);position:absolute;top:var(--center,50%);left:50%;z-index:10;display:flex;flex-direction:column;width:max-content;translate:calc(-50% + var(--start)*50% - var(--end)*50%) calc(var(--below)*16px + (1 - var(--below))*(-100% - 16px));padding:.5rem .75rem;border:1px solid var(--color-cc-card-border);border-radius:.25rem;background:var(--color-cc-bg);box-shadow:0 10px 15px -3px rgb(0 0 0/.1),0 4px 6px -4px rgb(0 0 0/.1);color:var(--color-cc-heading);font-size:.75rem;line-height:1rem;text-align:left;white-space:nowrap;pointer-events:none}
[data-history-chart] [data-point]>span::before{content:var(--title);font-weight:600}
[data-history-chart] [data-point]>span>:first-child::after{content:" requests/s"}
[data-history-chart] [data-point]>span>:nth-child(2){margin-top:.25rem;color:var(--color-cc-ink-dim)}
[data-history-chart] [data-point]>span>:nth-child(2)::before{content:"Version "}
[data-history-chart] [data-point]>span::after{content:var(--composition);order:1;color:var(--color-cc-ink-dim)}
[data-history-chart] [data-point]>span>:nth-child(3){order:2;display:flex;align-items:center;gap:.25rem;margin-top:.5rem;padding-top:.5rem;border-top:1px solid var(--color-cc-card-border);color:var(--color-cc-danger);font-size:11px}
[data-history-chart] li[data-series]>button{opacity:calc(1 - var(--dim,0)*.4)}
[data-history-chart] li[data-series]>button:not([aria-pressed=true]){background-color:color-mix(in oklab,var(--color-cc-hover) calc(var(--active,0)*100%),transparent)}
[data-history-chart] li[data-series]>button:not([aria-pressed=true]):hover{background-color:var(--color-cc-hover)}
`;

/** A history chart that shows the series that match the filters on a fitted scale. Hovering or focusing a point or legend entry highlights its series and shows the point's tooltip. Clicking a point pins its series, and clicking a legend entry toggles the pin. While a series is pinned, hovering anywhere above or below one of its points shows that point. */
export function BenchmarkHistoryChartFrame({
  id,
  series,
  domain,
  dates,
}: BenchmarkHistoryChartFrameProps) {
  const { filters } = useBenchmarkPreferences();
  const visible = series.filter(
    (item) =>
      (filters.showPreviews || !item.preview) &&
      (filters.technologies.length === 0 ||
        filters.technologies.includes(item.technology)),
  );
  const { pinned, pin, togglePinned } = useSeriesHighlight(
    visible.map((item) => item.id),
  );
  if (visible.length === 0) {
    return null;
  }
  const { x } = historyTimeScale(dates);
  const { ticks, y } = historyScale(
    visible.flatMap((item) => item.points.map((point) => point.rps)),
  );
  const top = round(y(domain[1]));
  const height = round(y(domain[0]) - top);
  const { dates: dateTicks, intervals } = historyDateIntervals(dates);
  // The labels of the dates whose index is a multiple of each interval, so container queries can show the interval that fits.
  const dateRuns = (index: number) =>
    index === 0
      ? undefined
      : intervals
          .filter((interval) => index % interval.runs === 0)
          .map((interval) => interval.runs)
          .join(" ");
  const includeYear =
    new Date(dates[0]).getUTCFullYear() !==
    new Date(dates.at(-1)!).getUTCFullYear();
  const scope = `#${id}-chart`;
  const styles = [
    ...series.flatMap((item, index) => {
      const seriesId = quote(item.id);
      return [
        `${scope} [data-series=${seriesId}]{--point-color:${item.color};--point-fill:${item.dashed ? "var(--color-cc-bg)" : item.color};--title:${quote(`${item.name} (${seriesRuntimes(item)[0]})`)};--composition:${quote(compositionName(item))};--active:var(--highlight-${index},0);--dim:calc(var(--highlight,0) - var(--highlight-${index},0))}`,
        ...seriesRuntimes(item)
          .slice(1)
          .map(
            (runtime, runtimeIndex) =>
              `${scope} [data-series=${seriesId}][data-runtime="${runtimeIndex + 1}"]{--title:${quote(`${item.name} (${runtime})`)}}`,
          ),
        `${scope}:not([data-pinned]):has([data-series=${seriesId}]:is(:hover,:focus-visible),[data-series=${seriesId}]>:focus-visible){--highlight:1;--highlight-${index}:1}`,
      ];
    }),
    ...intervals.map(({ runs, minWidth, maxWidth }) => {
      const query = [
        ...(minWidth > 0 ? [`(min-width:${minWidth}px)`] : []),
        ...(maxWidth < Infinity ? [`(not (min-width:${maxWidth}px))`] : []),
      ].join(" and ");
      return `@container ${query}{${scope} [data-date-runs]:not([data-date-runs~="${runs}"]){display:none}}`;
    }),
    ...series
      .filter((item) => !visible.includes(item))
      .map((item) => `${scope} [data-series=${quote(item.id)}]{display:none}`),
    ...COMPOSITION_GROUPS.filter(
      (group) => !visible.some((item) => item.composition === group.id),
    ).map(
      (group) =>
        `${scope} [data-legend-group=${quote(group.id)}]{display:none}`,
    ),
    ...(pinned
      ? [
          `${scope}{--highlight:1;--highlight-${series.findIndex((item) => item.id === pinned)}:1}`,
          `${scope} [data-point]:not([data-series=${quote(pinned)}]){pointer-events:none}`,
          `${scope} [data-point][data-series=${quote(pinned)}]{top:calc(var(--fit-top)*-1cqh);width:var(--column-width);height:100cqh;translate:-50% 0;--center:calc((var(--fit-top) + var(--y)*var(--fit-height)/100)*1cqh)}`,
          `${scope} [data-point][data-series=${quote(pinned)}]:focus-visible{outline:none}`,
          `${scope} [data-point][data-series=${quote(pinned)}]:focus-visible::before{outline:2px solid var(--color-cc-accent);outline-offset:2px}`,
          `${scope} [data-point]:not([data-series=${quote(pinned)}])>span{display:none}`,
        ]
      : []),
  ].join("");
  return (
    <Pin.Provider value={{ pinned, toggle: togglePinned }}>
      <figure
        id={`${id}-chart`}
        data-history-chart
        data-pinned={pinned}
        data-benchmark-scroll-anchor={`chart-${id}`}
        className="border-cc-card-border relative rounded-lg border"
        aria-labelledby={`${id}-history`}
      >
        <style href="benchmark-history-chart" precedence="default">
          {CHART_STYLES}
        </style>
        <style>{styles}</style>
        <figcaption className="border-cc-card-border border-b px-5 py-4">
          <h3 id={`${id}-history`} className={SUBSECTION_HEADING}>
            Throughput over time
          </h3>
          <p className="text-caption text-cc-ink-dim mt-1">
            Median requests per second by date.
          </p>
        </figcaption>
        <div className="px-5 pt-10 pb-5">
          <div className="text-cc-ink-dim mb-4 text-xs">requests/s</div>
          <div className="grid grid-cols-[2.5rem_1fr]">
            <div
              className="relative h-60 text-[11px] tabular-nums sm:h-72"
              aria-hidden="true"
            >
              {ticks.map((tick) => (
                <span
                  key={tick}
                  className="text-cc-ink-dim absolute right-2 -translate-y-1/2"
                  style={{ top: `${y(tick)}%` }}
                >
                  {compact.format(tick)}
                </span>
              ))}
            </div>
            <div
              className="[container-type:size] relative h-60 sm:h-72"
              onClick={(event) => {
                const button =
                  event.target instanceof Element
                    ? event.target.closest<HTMLElement>("button[data-point]")
                    : null;
                if (button) {
                  pin(button.dataset.series!);
                }
              }}
            >
              {ticks.map((tick) => (
                <div
                  key={tick}
                  className="border-cc-card-border absolute inset-x-0 border-t"
                  style={{ top: `${y(tick)}%` }}
                />
              ))}
              {dateTicks.map((date, index) => (
                <div
                  key={date}
                  data-date-runs={dateRuns(index)}
                  className="border-cc-card-border/50 absolute inset-y-0 border-l"
                  style={{ left: `${x(date)}%` }}
                />
              ))}
              <div
                className="pointer-events-none absolute inset-x-0"
                style={
                  {
                    top: `${top}%`,
                    height: `${height}%`,
                    "--fit-top": top,
                    "--fit-height": height,
                  } as CSSProperties
                }
              >
                <HistoryChartPlot
                  series={series}
                  dates={dates}
                  domain={domain}
                />
              </div>
            </div>
            <div className="@container relative col-start-2 mt-3 h-9 text-[10px] sm:text-[11px]">
              {dateTicks.map((date, index) => (
                <span
                  key={date}
                  data-date-runs={dateRuns(index)}
                  className="text-cc-ink-dim absolute whitespace-nowrap"
                  style={{
                    left: `${x(date)}%`,
                    transform: `translateX(${x(date) === 0 ? "0" : x(date) === 100 ? "-100%" : "-50%"})`,
                  }}
                >
                  {formatHistoryDate(date, includeYear)}
                </span>
              ))}
            </div>
          </div>
        </div>
        <div className="border-cc-card-border space-y-4 border-t px-5 py-4">
          <BenchmarkHistoryLegend id={id} series={series} />
        </div>
        <div className="sr-only">
          <HistoryChartTable series={series} dates={dates} />
        </div>
      </figure>
    </Pin.Provider>
  );
}

interface HistoryChartPlotProps {
  readonly series: readonly HistoryChartSeries[];
  readonly dates: readonly number[];
  readonly domain: readonly [low: number, high: number];
}

/** The lines and point buttons of every series, positioned on the scale of `domain`. */
const HistoryChartPlot = memo(function HistoryChartPlot({
  series,
  dates,
  domain,
}: HistoryChartPlotProps) {
  const { x } = historyTimeScale(dates);
  const pointX = (point: HistoryChartPoint) => round(x(dates[point.date]));
  const pointY = (point: HistoryChartPoint) =>
    round((1 - (point.rps - domain[0]) / (domain[1] - domain[0] || 1)) * 100);
  const maxPoints = Math.max(...series.map((item) => item.points.length));
  const includeYear =
    new Date(dates[0]).getUTCFullYear() !==
    new Date(dates.at(-1)!).getUTCFullYear();

  return (
    <>
      {series.map((item) => (
        <svg
          key={item.id}
          data-series={item.id}
          className="pointer-events-none absolute inset-0 h-full w-full overflow-visible [&_path]:[vector-effect:non-scaling-stroke]"
          viewBox="0 0 100 100"
          preserveAspectRatio="none"
          aria-hidden="true"
          fill="none"
          stroke={item.color}
          strokeWidth={1.5}
          strokeOpacity={0.8}
          strokeLinecap="round"
        >
          {seriesPaths(item, pointX, pointY).map(
            (path) =>
              path.d && (
                <path
                  key={String(path.dashed)}
                  d={path.d}
                  strokeDasharray={path.dashed ? "4 4" : undefined}
                />
              ),
          )}
        </svg>
      ))}
      <div
        className="focus-visible:*:outline-cc-accent contents *:pointer-events-auto *:absolute *:top-[calc(var(--y)*1%)] *:left-[calc(var(--x)*1%)] *:flex *:size-5 *:w-(--point-width) *:-translate-1/2 *:cursor-pointer *:items-center *:justify-center *:rounded-full focus-visible:*:outline-2"
        style={
          {
            "--point-width": `clamp(3px, ${round(100 / maxPoints)}%, 20px)`,
            "--point-size": maxPoints > 60 ? "0.1875rem" : "0.3125rem",
            "--column-width": `${round(100 / Math.max(dates.length - 1, 1))}%`,
          } as CSSProperties
        }
      >
        {series.flatMap((item) => {
          const runtimes = seriesRuntimes(item);
          return item.points.map((point) => {
            const runtime = point.runtime ?? item.runtime;
            const runtimeIndex = runtimes.indexOf(runtime);
            return (
              <button
                key={`${item.id}-${point.date}`}
                type="button"
                data-series={item.id}
                data-point=""
                data-runtime={runtimeIndex > 0 ? runtimeIndex : undefined}
                data-failed={point.failure ? "" : undefined}
                aria-label={`${item.name}, ${formatHistoryDate(dates[point.date], includeYear)}, ${runtime}, ${compositionName(item)}, version ${point.version}, ${formatNumber(point.rps)} requests/s${point.failure ? ", failed" : ""}`}
                style={
                  {
                    "--x": pointX(point),
                    "--y": pointY(point),
                  } as CSSProperties
                }
              >
                {point.failure && (
                  <WarningIcon
                    className={`text-cc-danger bg-cc-bg ring-cc-bg block shrink-0 rounded-full ring-2 ${maxPoints > 60 ? "size-2.5" : "size-3"}`}
                    strokeWidth={2.5}
                  />
                )}
                <span>
                  <span>{formatNumber(point.rps)}</span>
                  <span>{point.version}</span>
                  {point.failure && (
                    <span>
                      <WarningIcon className="size-3 shrink-0" />
                      {point.failure}
                    </span>
                  )}
                </span>
              </button>
            );
          });
        })}
      </div>
    </>
  );
});

const compositionName = (series: HistoryChartSeries) =>
  COMPOSITION_GROUPS.find((group) => group.id === series.composition)?.name ??
  "";

/** Returns the runtimes of a series, the runtime of the series first. */
const seriesRuntimes = (series: HistoryChartSeries) => [
  ...new Set([
    series.runtime,
    ...series.points.flatMap((point) => point.runtime ?? []),
  ]),
];

interface HistoryChartTableProps {
  readonly series: readonly HistoryChartSeries[];
  readonly dates: readonly number[];
}

/** The runs of every series as a table for assistive technology. */
const HistoryChartTable = memo(function HistoryChartTable({
  series,
  dates,
}: HistoryChartTableProps) {
  return (
    <table>
      <caption>Median requests per second by run date, oldest first.</caption>
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
                    `${new Date(dates[point.date]).toISOString().slice(0, 10)}: ${point.version}, ${formatNumber(point.rps)} requests/s${point.failure ? ", failed" : ""}`,
                )
                .join(", ")}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
});

/** Returns the solid and the dashed segments of a series line as SVG path data, empty when the line has no such segments. A segment is dashed when the series is dashed or either of its runs failed. */
function seriesPaths(
  series: HistoryChartSeries,
  x: (point: HistoryChartPoint) => number,
  y: (point: HistoryChartPoint) => number,
) {
  const paths = [
    { dashed: false, d: "", end: -1 },
    { dashed: true, d: "", end: -1 },
  ];
  series.points.slice(1).forEach((point, index) => {
    const previous = series.points[index];
    const path =
      paths[
        series.dashed ||
        previous.failure !== undefined ||
        point.failure !== undefined
          ? 1
          : 0
      ];
    if (path.end !== index) {
      path.d += `M${x(previous)} ${y(previous)}`;
    }
    path.d += `L${x(point)} ${y(point)}`;
    path.end = index + 1;
  });
  return paths;
}
