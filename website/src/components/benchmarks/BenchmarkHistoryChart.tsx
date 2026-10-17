"use client";

import { useState } from "react";
import {
  COMPOSITION_GROUPS,
  formatChange,
  formatNumber,
  historyTimeScale,
  formatHistoryDate,
  type HistoryPoint,
  historyScale,
  historySeries,
  seriesChange,
  type HistorySeries,
} from "@/src/components/benchmarks/benchmarkView";
import { useSeriesHighlight } from "@/src/components/benchmarks/useSeriesHighlight";
import type {
  BenchmarkCatalogEntry,
  BenchmarkHistoryScenario,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkHistoryChartProps {
  readonly id: string;
  readonly scenario: BenchmarkHistoryScenario;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
  readonly stableOnly: boolean;
}

const compact = new Intl.NumberFormat("en-US", {
  notation: "compact",
  maximumFractionDigits: 1,
});

export function BenchmarkHistoryChart({
  id,
  scenario,
  variants,
  stableOnly,
}: BenchmarkHistoryChartProps) {
  const { active, pinned, setHovered, setFocused, togglePinned } =
    useSeriesHighlight();
  const [selected, setSelected] = useState<{
    readonly seriesId: string;
    readonly point: HistoryPoint;
  }>();
  const series = historySeries(scenario, variants, stableOnly);
  if (series.length === 0) {
    return null;
  }
  const { ticks, y } = historyScale(
    series.flatMap((item) => item.points.map((point) => point.rps)),
  );
  const { x } = historyTimeScale(
    series.flatMap((item) => item.points.map((point) => point.timestamp)),
  );
  const dates = series
    .reduce((longest, item) =>
      item.points.length > longest.points.length ? item : longest,
    )
    .points.map((point) => point.timestamp);
  const timestamps = series.flatMap((item) =>
    item.points.map((point) => point.timestamp),
  );
  const includeYear =
    new Date(Math.min(...timestamps)).getUTCFullYear() !==
    new Date(Math.max(...timestamps)).getUTCFullYear();
  const dateLabel = (timestamp: number) =>
    formatHistoryDate(timestamp, includeYear);
  const maxPoints = Math.max(...series.map((item) => item.points.length));
  const selectedSeries = series.find((item) => item.id === selected?.seriesId);
  const point = selectedSeries?.points.find(
    (point) =>
      point === selected?.point ||
      point.runNumber === selected?.point.runNumber,
  );
  const align = (position: number) =>
    position > 85 ? "-100%" : position < 15 ? "0" : "-50%";
  const ordered = series.toSorted(
    (a, b) => (a.id === active ? 1 : 0) - (b.id === active ? 1 : 0),
  );
  return (
    <figure
      className="border-cc-card-border relative rounded-lg border"
      aria-labelledby={`${id}-history`}
      onMouseLeave={() => {
        setHovered(undefined);
        setSelected(undefined);
      }}
    >
      <figcaption className="border-cc-card-border border-b px-5 py-4">
        <h3
          id={`${id}-history`}
          className="font-heading text-h6 text-cc-heading font-semibold"
        >
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
            className="relative h-60 sm:h-72"
            onMouseLeave={() => {
              setHovered(undefined);
              setSelected(undefined);
            }}
          >
            {ticks.map((tick) => (
              <div
                key={tick}
                className="border-cc-card-border absolute inset-x-0 border-t"
                style={{ top: `${y(tick)}%` }}
              />
            ))}
            {dates.map((date) => (
              <div
                key={date}
                className="border-cc-card-border/50 absolute inset-y-0 border-l"
                style={{ left: `${x(date)}%` }}
              />
            ))}
            {point && (
              <div
                className="border-cc-ink-dim pointer-events-none absolute inset-y-0 z-[1] border-l border-dashed"
                style={{ left: `${x(point.timestamp)}%` }}
              />
            )}
            <svg
              className="absolute inset-0 h-full w-full overflow-visible"
              viewBox="0 0 100 100"
              preserveAspectRatio="none"
              aria-hidden="true"
            >
              {ordered.map((item) => (
                <g key={item.id}>
                  <polyline
                    points={item.points
                      .map((point) => `${x(point.timestamp)},${y(point.rps)}`)
                      .join(" ")}
                    fill="none"
                    stroke={item.color}
                    strokeWidth={item.id === active ? 2 : 1.5}
                    strokeOpacity={active && item.id !== active ? 0.12 : 0.8}
                    strokeDasharray={item.dashed ? "4 4" : undefined}
                    vectorEffect="non-scaling-stroke"
                  />
                </g>
              ))}
            </svg>
            {ordered.flatMap((item) =>
              item.points.map((point) => (
                <button
                  key={`${item.id}-${point.runNumber}`}
                  type="button"
                  aria-label={`${item.name}, ${dateLabel(point.timestamp)}, version ${point.version}, ${formatNumber(point.rps)} requests/s`}
                  className={`focus-visible:outline-cc-accent absolute flex size-5 -translate-1/2 cursor-pointer items-center justify-center rounded-full focus-visible:outline-2 ${item.id === active ? "z-[2]" : ""}`}
                  style={{
                    width: `clamp(3px, ${100 / maxPoints}%, 20px)`,
                    left: `${x(point.timestamp)}%`,
                    top: `${y(point.rps)}%`,
                  }}
                  onMouseEnter={() => {
                    setHovered(item.id);
                    setSelected({ seriesId: item.id, point });
                  }}
                  onMouseLeave={() => setSelected(undefined)}
                  onFocus={(event) => {
                    if (event.currentTarget.matches(":focus-visible")) {
                      setFocused(item.id);
                      setSelected({ seriesId: item.id, point });
                    }
                  }}
                  onBlur={() => {
                    setFocused(undefined);
                    setSelected(undefined);
                  }}
                  onClick={() => {
                    togglePinned(item.id);
                    setSelected({ seriesId: item.id, point });
                  }}
                >
                  <span className="bg-cc-bg ring-cc-bg flex shrink-0 rounded-full ring-2">
                    <span
                      className={`block ${maxPoints > 60 ? "size-0.75" : "size-1.25"} rounded-full border ${active && item.id !== active ? "opacity-20" : ""}`}
                      style={{
                        borderColor: item.color,
                        backgroundColor: item.dashed
                          ? "var(--color-cc-bg)"
                          : item.color,
                      }}
                    />
                  </span>
                </button>
              )),
            )}
            {point && (
              <>
                <div
                  role="tooltip"
                  className="bg-cc-bg text-cc-heading border-cc-card-border pointer-events-none absolute z-10 rounded border px-3 py-2 text-xs whitespace-nowrap shadow-lg"
                  style={{
                    left: `${x(point.timestamp)}%`,
                    top: `${y(point.rps)}%`,
                    transform: `translate(${align(x(point.timestamp))}, ${y(point.rps) < 25 ? "16px" : "calc(-100% - 16px)"})`,
                  }}
                >
                  <div className="font-semibold">{selectedSeries?.name}</div>
                  <div>Version {point.version}</div>
                  <div>{formatNumber(point.rps)} requests/s</div>
                </div>
              </>
            )}
          </div>
          <div className="relative col-start-2 mt-3 h-9 text-[10px] sm:text-[11px]">
            {dates.map((date, index) => (
              <span
                key={date}
                className={`text-cc-ink-dim absolute whitespace-nowrap ${index % Math.ceil(dates.length / (includeYear ? 3 : 5)) !== 0 ? "hidden lg:block" : ""}`}
                style={{
                  left: `${x(date)}%`,
                  transform: `translateX(${x(date) === 0 ? "0" : x(date) === 100 ? "-100%" : "-50%"})`,
                }}
              >
                {dateLabel(date)}
              </span>
            ))}
          </div>
        </div>
      </div>
      <div className="border-cc-card-border space-y-4 border-t px-5 py-4">
        {COMPOSITION_GROUPS.map((group) => {
          const members = series
            .filter((item) => item.composition === group.id)
            .toSorted((a, b) => b.points.at(-1)!.rps - a.points.at(-1)!.rps);
          if (members.length === 0) {
            return null;
          }
          return (
            <div key={group.id}>
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
                  <LegendButton
                    key={item.id}
                    series={item}
                    active={active}
                    pinned={pinned === item.id}
                    onHighlight={setHovered}
                    onFocusChange={setFocused}
                    onToggle={togglePinned}
                  />
                ))}
              </ul>
            </div>
          );
        })}
      </div>
      <table className="sr-only">
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
            <tr key={item.id}>
              <th scope="row">
                {item.name} ({item.runtime})
              </th>
              <td>
                {item.points
                  .map(
                    (point) =>
                      `${new Date(point.timestamp).toISOString()}: ${point.version}, ${formatNumber(point.rps)} requests/s`,
                  )
                  .join(", ")}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </figure>
  );
}

interface LegendButtonProps {
  readonly series: HistorySeries;
  readonly active: string | undefined;
  readonly pinned: boolean;
  readonly onHighlight: (id: string | undefined) => void;
  readonly onFocusChange: (id: string | undefined) => void;
  readonly onToggle: (id: string) => void;
}

function LegendButton({
  series,
  active,
  pinned,
  onHighlight,
  onFocusChange,
  onToggle,
}: LegendButtonProps) {
  const isActive = series.id === active;
  const newest = series.points.at(-1)!;
  const change = seriesChange(series);

  return (
    <li>
      <button
        type="button"
        aria-pressed={pinned}
        onMouseEnter={() => onHighlight(series.id)}
        onMouseLeave={() => onHighlight(undefined)}
        onFocus={(event) => {
          if (event.currentTarget.matches(":focus-visible")) {
            onFocusChange(series.id);
          }
        }}
        onBlur={() => onFocusChange(undefined)}
        onClick={() => onToggle(series.id)}
        className={`focus-visible:outline-cc-accent flex cursor-pointer items-center gap-2.5 rounded-md px-2 py-1.5 text-left text-sm transition-colors focus-visible:outline-2 ${
          isActive ? "bg-cc-hover" : "hover:bg-cc-hover"
        } ${active && !isActive ? "opacity-60" : ""}`}
      >
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
        <span className="min-w-0">
          <span className="block truncate">
            <span className="text-cc-heading">{series.name}</span>
            <span className="border-cc-card-border text-cc-ink-dim ml-1 rounded border px-1.5 align-[1px] font-mono text-[11px] leading-5 whitespace-nowrap">
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
                title={`Change from ${series.points[0].version} to ${newest.version}`}
              >
                <span aria-hidden="true">{change >= 0 ? "▲" : "▼"}</span>{" "}
                <span className="sr-only">{change >= 0 ? "up" : "down"}</span>
                {formatChange(change)}
              </span>
            )}
          </span>
        </span>
      </button>
    </li>
  );
}
