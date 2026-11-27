"use client";

import {
  createContext,
  memo,
  useContext,
  useMemo,
  type CSSProperties,
  type MouseEvent,
} from "react";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";
import { SUBSECTION_HEADING } from "@/src/components/benchmarks/benchmarkHeadings";
import {
  COMPOSITION_GROUPS,
  HISTORY_METRICS,
  formatHistoryDate,
  historyDateIntervals,
  historyScale,
  historyTimeScale,
  type HistoryMetric,
  type HistoryValues,
} from "@/src/components/benchmarks/benchmarkView";
import { BenchmarkHistoryLegend } from "@/src/components/benchmarks/BenchmarkHistoryLegend";
import { useSeriesHighlight } from "@/src/components/benchmarks/useSeriesHighlight";
import { Dropdown, DropdownItem } from "@/src/design-system/Dropdown";
import { WarningIcon } from "@/src/icons/Warning";
import type { Composition } from "@/src/helpers/benchmarkReport";

/** One run of a history chart series. */
export interface HistoryChartPoint extends HistoryValues {
  /** The index of the run date in the chart's dates. */
  readonly date: number;
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
  /** The metric values of the newest run that did not fail, or of the newest run when every run failed. */
  readonly newest: HistoryValues;
  /** The relative change of each metric between the versions of the oldest and the newest run that did not fail and recorded it, absent for a metric with fewer than two such runs. */
  readonly changes: Readonly<
    Partial<
      Record<
        HistoryMetric,
        {
          readonly value: number;
          readonly from: string;
          readonly to: string;
        }
      >
    >
  >;
  /** Whether the newest run failed, absent when it did not. */
  readonly latestFailed?: true;
  /** The runs of the series, oldest first. */
  readonly points: readonly HistoryChartPoint[];
}

interface HistoryChartPin {
  readonly pinned: readonly string[];
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
  /** The values at the bottom and the top of the plot for each metric before it is fitted to the shown series, absent for a metric that no run recorded. */
  readonly domains: Readonly<
    Partial<Record<HistoryMetric, readonly [low: number, high: number]>>
  >;
  /** Every charted run date, oldest first. */
  readonly dates: readonly number[];
}

const METRICS = Object.keys(HISTORY_METRICS) as HistoryMetric[];

const quote = (value: string) => JSON.stringify(value);

const round = (position: number) => Math.round(position * 100) / 100;

const sentence = (text: string) => text.charAt(0).toUpperCase() + text.slice(1);

/** Returns whether a pointer event happened within the border box of `element`, as opposed to one of its pseudo-elements or children outside it. */
const isOnElement = (event: MouseEvent, element: Element) => {
  const bounds = element.getBoundingClientRect();
  return (
    event.clientX >= bounds.left &&
    event.clientX <= bounds.right &&
    event.clientY >= bounds.top &&
    event.clientY <= bounds.bottom
  );
};

// The chart sets `--highlight` and `--highlight-<index>` for its highlighted series, which sets `--active` on those series and `--dim` on the others.
const CHART_STYLES = `
[data-history-chart] svg[data-series]{z-index:var(--active,0);opacity:calc(1 - var(--dim,0)*.85);transition:opacity .1s}
[data-history-chart] svg[data-series] path{stroke-width:calc(1.5 + var(--active,0)*.5)}
[data-history-chart] [data-point]{z-index:calc(var(--active,0)*2)}
[data-history-chart] [data-point]:is(:hover,:focus-visible){z-index:3}
[data-history-chart] [data-point]::before{content:"";position:absolute;top:50%;left:50%;translate:-50% -50%;width:var(--point-size);height:var(--point-size);border:1px solid var(--point-color);border-radius:9999px;background:var(--point-fill);box-shadow:0 0 0 calc(2px - var(--dim,0)*2px) var(--color-cc-bg);opacity:calc(1 - var(--dim,0)*.8);transition:opacity .1s}
[data-history-chart] [data-point][data-failed]::before{display:none}
[data-history-chart] [data-point]>svg{position:absolute;top:50%;left:50%;translate:-50% -50%;opacity:calc(1 - var(--dim,0)*.8)}
[data-history-chart] [data-point]:is(:hover,:focus-visible)::after{content:"";position:absolute;z-index:-1;top:calc(50% - (var(--fit-top) + var(--y)*var(--fit-height)/100)*1cqh);left:50%;width:1px;height:100cqh;translate:-50% 0;background:repeating-linear-gradient(var(--color-cc-ink-dim) 0 3px,transparent 0 6px) 50% 0/1px 100% no-repeat;pointer-events:none}
[data-history-chart] [data-point]>i{position:absolute;z-index:-1;top:calc(50% - (var(--fit-top) + var(--y)*var(--fit-height)/100 - var(--band-top,0))*1cqh);left:50%;width:var(--column-width);height:calc((var(--band-bottom,100) - var(--band-top,0))*1cqh);translate:-50% 0;cursor:default}
[data-history-chart] [data-point]>span{display:none}
[data-history-chart] [data-point]:is(:hover,:focus-visible)>span{--below:clamp(0,(25 - var(--fit-top) - var(--y)*var(--fit-height)/100)*1000,1);--start:clamp(0,(15 - var(--x))*1000,1);--end:clamp(0,(var(--x) - 85)*1000,1);position:absolute;top:50%;left:50%;z-index:10;display:flex;flex-direction:column;width:max-content;translate:calc(-50% + var(--start)*50% - var(--end)*50%) calc(var(--below)*16px + (1 - var(--below))*(-100% - 16px));padding:.5rem .75rem;border:1px solid var(--color-cc-card-border);border-radius:.25rem;background:var(--color-cc-bg);box-shadow:0 10px 15px -3px rgb(0 0 0/.1),0 4px 6px -4px rgb(0 0 0/.1);color:var(--color-cc-heading);font-size:.75rem;line-height:1rem;text-align:left;white-space:nowrap;pointer-events:none}
[data-history-chart] [data-point]>span::before{content:var(--title);font-weight:600}
[data-history-chart] [data-point]>span>:first-child::after{content:var(--unit)}
[data-history-chart] [data-point]>span>:nth-child(2){margin-top:.25rem;color:var(--color-cc-ink-dim)}
[data-history-chart] [data-point]>span>:nth-child(2)::before{content:"Version "}
[data-history-chart] [data-point]>span::after{content:var(--composition);order:1;color:var(--color-cc-ink-dim)}
[data-history-chart] [data-point]>span>:nth-child(3){order:2;display:flex;align-items:center;gap:.25rem;margin-top:.5rem;padding-top:.5rem;border-top:1px solid var(--color-cc-card-border);color:var(--color-cc-danger);font-size:11px}
[data-history-chart] li[data-series]>button{opacity:calc(1 - var(--dim,0)*.4)}
[data-history-chart] li[data-series]>button:not([aria-pressed=true]){background-color:color-mix(in oklab,var(--color-cc-hover) calc(var(--active,0)*100%),transparent)}
[data-history-chart] li[data-series]>button:not([aria-pressed=true]):hover{background-color:var(--color-cc-hover)}
`;

/** A history chart that shows the series that match the filters, plotting the metric chosen in its menu on a scale fitted to the pinned series, or to every shown series when none is pinned. Series outside the fitted scale are cut off at the edges of the plot. Hovering or focusing a point or legend entry highlights its series and shows the point's tooltip. Clicking a point pins its series, clicking a legend entry toggles the pin of its series, and clicking anywhere else in the chart outside the menu removes every pin. While series are pinned, hovering anywhere in a date's column shows the nearest point of a pinned series. */
export function BenchmarkHistoryChartFrame({
  id,
  series,
  domains,
  dates,
}: BenchmarkHistoryChartFrameProps) {
  const { filters, historyMetric, setHistoryMetric } =
    useBenchmarkPreferences();
  const visible = useMemo(
    () =>
      series.filter(
        (item) =>
          (filters.showPreviews || !item.preview) &&
          (filters.technologies.length === 0 ||
            filters.technologies.includes(item.technology)),
      ),
    [series, filters],
  );
  const visibleIds = useMemo(() => visible.map((item) => item.id), [visible]);
  const { pinned, pin, unpin, togglePinned } = useSeriesHighlight(visibleIds);
  if (visible.length === 0) {
    return null;
  }
  const metrics = METRICS.filter((metric) => domains[metric]);
  const metric = domains[historyMetric] ? historyMetric : "rps";
  const { title, unit, lowerIsBetter, formatTick } = HISTORY_METRICS[metric];
  const domain = domains[metric]!;
  const { x } = historyTimeScale(dates);
  const values = (items: readonly HistoryChartSeries[]) =>
    items.flatMap((item) =>
      item.points.flatMap((point) => point[metric] ?? []),
    );
  const pinnedValues = values(
    visible.filter((item) => pinned.includes(item.id)),
  );
  const fitted = pinnedValues.length > 0 ? pinnedValues : values(visible);
  const { ticks, y } = historyScale(fitted.length > 0 ? fitted : domain);
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
  const pinnedSeries = pinned
    .map((seriesId) => `[data-series=${quote(seriesId)}]`)
    .join(",");
  const styles = [
    `${scope}{--unit:${quote(` ${unit}`)}}`,
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
    ...(pinned.length > 0
      ? [
          `${scope}{--highlight:1;${pinned
            .map(
              (seriesId) =>
                `--highlight-${series.findIndex((item) => item.id === seriesId)}:1`,
            )
            .join(";")}}`,
          `${scope} svg[data-series]:not(${pinnedSeries}){clip-path:inset(calc(var(--fit-top)*-1cqh) -10px calc((var(--fit-top) + var(--fit-height) - 100)*1cqh) -10px)}`,
          `${scope} [data-point]:not(${pinnedSeries}){pointer-events:none;clip-path:inset(calc(50% - (var(--fit-top) + var(--y)*var(--fit-height)/100)*1cqh) -10px calc(50% - (100 - var(--fit-top) - var(--y)*var(--fit-height)/100)*1cqh) -10px)}`,
          `${scope} [data-point]:not(${pinnedSeries})>span{display:none}`,
        ]
      : []),
  ].join("");
  return (
    <Pin.Provider value={{ pinned, toggle: togglePinned }}>
      <figure
        id={`${id}-chart`}
        data-history-chart
        data-pinned={pinned.length > 0 ? "" : undefined}
        data-benchmark-scroll-anchor={`chart-${id}`}
        className="border-cc-card-border relative rounded-lg border"
        aria-labelledby={`${id}-history`}
        onClick={(event) => {
          const target = event.target instanceof Element ? event.target : null;
          const button = target?.closest<HTMLElement>("button[data-point]");
          if (button && (event.detail === 0 || isOnElement(event, button))) {
            pin(button.dataset.series!);
          } else if (
            pinned.length > 0 &&
            !target?.closest("li[data-series] > button, [data-history-metric]")
          ) {
            unpin();
          }
        }}
      >
        <style href="benchmark-history-chart" precedence="default">
          {CHART_STYLES}
        </style>
        <style>{styles}</style>
        <figcaption className="border-cc-card-border flex flex-wrap items-start justify-between gap-x-6 gap-y-3 border-b px-5 py-4">
          <div>
            <h3 id={`${id}-history`} className={SUBSECTION_HEADING}>
              {title}
            </h3>
            <p className="text-caption text-cc-ink-dim mt-1">
              {lowerIsBetter ? "Lower" : "Higher"} is better.
            </p>
          </div>
          {metrics.length > 1 && (
            <div data-history-metric>
              <Dropdown
                className="w-40"
                panelClassName="p-1"
                trigger={
                  <>
                    <span className="sr-only">Metric: </span>
                    {HISTORY_METRICS[metric].label}
                  </>
                }
              >
                <ul className="m-0 flex list-none flex-col p-0">
                  {metrics.map((item) => (
                    <DropdownItem
                      key={item}
                      active={item === metric}
                      onClick={() => setHistoryMetric(item)}
                    >
                      {HISTORY_METRICS[item].label}
                    </DropdownItem>
                  ))}
                </ul>
              </Dropdown>
            </div>
          )}
        </figcaption>
        <div className="px-5 pt-10 pb-5">
          <div className="text-cc-ink-dim mb-4 text-xs">{unit}</div>
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
                  {formatTick(tick)}
                </span>
              ))}
            </div>
            <div className="[container-type:size] relative h-60 sm:h-72">
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
                  metric={metric}
                  pinned={pinned}
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
          <BenchmarkHistoryLegend id={id} series={series} metric={metric} />
        </div>
        <div className="sr-only">
          <HistoryChartTable series={series} dates={dates} metric={metric} />
        </div>
      </figure>
    </Pin.Provider>
  );
}

interface HistoryChartPlotProps {
  readonly series: readonly HistoryChartSeries[];
  readonly dates: readonly number[];
  readonly domain: readonly [low: number, high: number];
  readonly metric: HistoryMetric;
  readonly pinned: readonly string[];
}

/** The lines and point buttons of every series for `metric`, positioned on the scale of `domain`. Each point of a pinned series answers to hovering the part of its date's column that is nearer to it than to the other pinned points. */
const HistoryChartPlot = memo(function HistoryChartPlot({
  series,
  dates,
  domain,
  metric,
  pinned,
}: HistoryChartPlotProps) {
  const { x } = historyTimeScale(dates);
  const { format, unit } = HISTORY_METRICS[metric];
  const plotted = series.map((item) => ({
    ...item,
    points: item.points.filter((point) => point[metric] !== undefined),
  }));
  const pointX = (point: HistoryChartPoint) => round(x(dates[point.date]));
  const pointY = (point: HistoryChartPoint) =>
    round(
      (1 - (point[metric]! - domain[0]) / (domain[1] - domain[0] || 1)) * 100,
    );
  const maxPoints = Math.max(...plotted.map((item) => item.points.length));
  const bands = columnBands(
    plotted.filter((item) => pinned.includes(item.id)),
    pointY,
  );
  const includeYear =
    new Date(dates[0]).getUTCFullYear() !==
    new Date(dates.at(-1)!).getUTCFullYear();

  return (
    <>
      {plotted.map((item) => (
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
            "--column-width": `${round(100 / Math.max(dates.length - 1, 1))}cqw`,
          } as CSSProperties
        }
      >
        {plotted.flatMap((item) => {
          const runtimes = seriesRuntimes(item);
          return item.points.map((point) => {
            const key = `${item.id}-${point.date}`;
            const runtime = point.runtime ?? item.runtime;
            const runtimeIndex = runtimes.indexOf(runtime);
            const band = bands.get(key);
            return (
              <button
                key={key}
                type="button"
                data-series={item.id}
                data-point=""
                data-runtime={runtimeIndex > 0 ? runtimeIndex : undefined}
                data-failed={point.failure ? "" : undefined}
                aria-label={`${item.name}, ${formatHistoryDate(dates[point.date], includeYear)}, ${runtime}, ${compositionName(item)}, version ${point.version}, ${format(point[metric]!)} ${unit}${point.failure ? ", failed" : ""}`}
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
                  <span>{format(point[metric]!)}</span>
                  <span>{point.version}</span>
                  {point.failure && (
                    <span>
                      <WarningIcon className="size-3 shrink-0" />
                      {point.failure}
                    </span>
                  )}
                </span>
                {band && (
                  <i
                    style={
                      {
                        "--band-top": bandEdge(band.top),
                        "--band-bottom": bandEdge(band.bottom),
                      } as CSSProperties
                    }
                  />
                )}
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

/** Returns the edges of the column part that each point of `series` answers to, keyed by series id and date index. An edge lies halfway to the nearest point above or below in the same column, and is absent at the top or the bottom of the column. Positions are plot percentages. */
function columnBands(
  series: readonly HistoryChartSeries[],
  y: (point: HistoryChartPoint) => number,
) {
  const columns = new Map<number, { key: string; y: number }[]>();
  for (const item of series) {
    for (const point of item.points) {
      const column = columns.get(point.date) ?? [];
      column.push({ key: `${item.id}-${point.date}`, y: y(point) });
      columns.set(point.date, column);
    }
  }
  const bands = new Map<string, { top?: number; bottom?: number }>();
  for (const column of columns.values()) {
    column.sort((a, b) => a.y - b.y);
    column.forEach((entry, index) => {
      const above = column[index - 1];
      const below = column[index + 1];
      bands.set(entry.key, {
        ...(above ? { top: round((above.y + entry.y) / 2) } : {}),
        ...(below ? { bottom: round((entry.y + below.y) / 2) } : {}),
      });
    });
  }
  return bands;
}

/** Returns a plot percentage as a percentage of the plot area that the plot is fitted into, or undefined for an absent edge. */
const bandEdge = (position: number | undefined) =>
  position === undefined
    ? undefined
    : `calc(var(--fit-top) + ${position}*var(--fit-height)/100)`;

interface HistoryChartTableProps {
  readonly series: readonly HistoryChartSeries[];
  readonly dates: readonly number[];
  readonly metric: HistoryMetric;
}

/** The runs of every series as a table for assistive technology. */
const HistoryChartTable = memo(function HistoryChartTable({
  series,
  dates,
  metric,
}: HistoryChartTableProps) {
  const { quantity, unit, format } = HISTORY_METRICS[metric];
  return (
    <table>
      <caption>{sentence(quantity)} by run date, oldest first.</caption>
      <thead>
        <tr>
          <th scope="col">Router</th>
          <th scope="col">Run dates, versions and {quantity}</th>
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
                .flatMap((point) => {
                  const value = point[metric];
                  return value === undefined
                    ? []
                    : `${new Date(dates[point.date]).toISOString().slice(0, 10)}: ${point.version}, ${format(value)} ${unit}${point.failure ? ", failed" : ""}`;
                })
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
