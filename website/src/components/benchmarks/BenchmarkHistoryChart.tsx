"use client";

import { useState } from "react";
import {
  formatRuntimeTag,
  isPreview,
  variantName,
  type BenchmarkCatalogEntry,
  type BenchmarkHistoryScenario,
  type Composition,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkHistoryChartProps {
  readonly id: string;
  readonly scenario: BenchmarkHistoryScenario;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
  readonly stableOnly: boolean;
}

interface Point {
  readonly slot: number;
  readonly version: string;
  readonly rps: number;
}

interface Series {
  readonly id: string;
  readonly name: string;
  readonly runtime: string;
  readonly composition: Composition;
  readonly color: string;
  readonly dashed: boolean;
  readonly points: readonly Point[];
}

const SERIES_COLORS = [
  "#3987e5",
  "#d95926",
  "#199e70",
  "#c98500",
  "#d55181",
  "#008300",
  "#9085e9",
  "#e66767",
];
const DASH = "5 4";
const SLOTS = 3;
const SLOT_X = [10, 50, 90];
const LABEL_CLEARANCE = 25;

const GROUPS = [
  { id: "composite-schema", name: "Composite Schema" },
  { id: "apollo-federation", name: "Apollo Federation" },
] as const satisfies readonly { id: Composition; name: string }[];

const formatRps = (value: number) =>
  new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(value);

const formatChange = (value: number) => `${Math.abs(value * 100).toFixed(1)}%`;

const niceStep = (range: number) => {
  const magnitude = 10 ** Math.floor(Math.log10(range));
  const fraction = range / magnitude;
  const nice = [1, 2, 2.5, 5, 10].find((step) => fraction <= step) ?? 10;
  return nice * magnitude;
};

const scale = (values: readonly number[]) => {
  const low = Math.min(...values);
  const high = Math.max(...values);
  const step = niceStep(Math.max(high - low, high * 0.1, 1) / 4);
  const min = Math.max(0, Math.floor(low / step) * step);
  const max = Math.ceil(high / step) * step;
  const ticks = Array.from(
    { length: Math.round((max - min) / step) + 1 },
    (_, index) => min + index * step,
  );
  return {
    ticks,
    y: (value: number) => (1 - (value - min) / (max - min || 1)) * 100,
  };
};

const changeOf = (series: Series) => {
  const first = series.points[0];
  const last = series.points.at(-1)!;
  return series.points.length > 1 && first.rps > 0
    ? (last.rps - first.rps) / first.rps
    : undefined;
};

export function BenchmarkHistoryChart({
  id,
  scenario,
  variants,
  stableOnly,
}: BenchmarkHistoryChartProps) {
  const [hovered, setHovered] = useState<string>();
  const [pinned, setPinned] = useState<string>();
  const active = hovered ?? pinned;

  const catalogOrder = [...variants.keys()];
  const series: Series[] = scenario.variants.flatMap((history) => {
    const entry = variants.get(history.variant)!;
    const versions = history.versions
      .filter(
        (version) =>
          !stableOnly || !isPreview(version.version, version.platform),
      )
      .slice(-SLOTS);
    if (versions.length === 0) {
      return [];
    }
    const colorIndex = catalogOrder.indexOf(history.variant);
    return {
      id: history.variant,
      name: variantName(entry),
      runtime: formatRuntimeTag(versions.at(-1)!.platform),
      composition: entry.variant.composition,
      color: SERIES_COLORS[colorIndex % SERIES_COLORS.length],
      dashed: colorIndex >= SERIES_COLORS.length,
      points: versions.map((version, index) => ({
        slot: SLOTS - versions.length + index,
        version: version.version,
        rps: version.throughput.medianRps,
      })),
    };
  });
  if (series.length === 0) {
    return null;
  }

  const { ticks, y } = scale(
    series.flatMap((item) => item.points.map((point) => point.rps)),
  );
  const ordered = series.toSorted(
    (a, b) => (a.id === active ? 1 : 0) - (b.id === active ? 1 : 0),
  );
  const activeSeries = series.find((item) => item.id === active);
  const highlight = (seriesId: string | undefined) => () =>
    setHovered(seriesId);

  return (
    <figure className="mt-10" aria-labelledby={`history-${id}`}>
      <figcaption>
        <h4 id={`history-${id}`} className="text-cc-heading font-semibold">
          Throughput across recent versions
        </h4>
        <p className="text-caption text-cc-ink-dim mt-1 max-w-3xl">
          Median requests/s of up to the three newest versions of each gateway,
          oldest on the left. Older versions come from earlier runs with the
          same scenario configuration, CPU, and subgraphs. Hover or select a
          gateway to highlight it.
        </p>
      </figcaption>
      <div
        className="mt-9 grid h-64 grid-cols-[3.5rem_1fr] sm:h-72"
        aria-hidden="true"
      >
        <div className="relative">
          <span className="text-cc-ink-dim absolute -top-6 left-0 text-[11px]">
            req/s
          </span>
          {ticks.map((tick) => (
            <span
              key={tick}
              className="text-cc-ink-dim absolute right-3 -translate-y-1/2 font-mono text-[11px] tabular-nums"
              style={{ top: `${y(tick)}%` }}
            >
              {formatRps(tick)}
            </span>
          ))}
        </div>
        <div className="relative" onMouseLeave={highlight(undefined)}>
          {ticks.map((tick) => (
            <div
              key={tick}
              className="border-cc-card-border absolute inset-x-0 border-t"
              style={{ top: `${y(tick)}%` }}
            />
          ))}
          <svg
            className="absolute inset-0 h-full w-full overflow-visible"
            viewBox="0 0 100 100"
            preserveAspectRatio="none"
          >
            {ordered.map((item) => {
              const points = item.points
                .map((point) => `${SLOT_X[point.slot]},${y(point.rps)}`)
                .join(" ");
              const isActive = item.id === active;
              return (
                <g key={item.id}>
                  <polyline
                    points={points}
                    fill="none"
                    stroke={item.color}
                    strokeWidth={isActive ? 3 : 2}
                    strokeDasharray={item.dashed ? DASH : undefined}
                    strokeLinejoin="round"
                    strokeLinecap="round"
                    vectorEffect="non-scaling-stroke"
                    className={`motion-safe:transition-opacity ${active && !isActive ? "opacity-15" : ""}`}
                  />
                  <polyline
                    points={points}
                    fill="none"
                    stroke="transparent"
                    strokeWidth={16}
                    vectorEffect="non-scaling-stroke"
                    pointerEvents="stroke"
                    onMouseEnter={highlight(item.id)}
                  />
                </g>
              );
            })}
          </svg>
          {ordered.flatMap((item) =>
            item.points.map((point) => {
              const isActive = item.id === active;
              return (
                <span
                  key={`${item.id}-${point.slot}`}
                  className={`ring-cc-bg absolute -translate-1/2 rounded-full border-2 ring-2 motion-safe:transition-opacity ${
                    isActive ? "size-3" : "size-2.5"
                  } ${active && !isActive ? "opacity-20" : ""}`}
                  style={{
                    left: `${SLOT_X[point.slot]}%`,
                    top: `${y(point.rps)}%`,
                    borderColor: item.color,
                    backgroundColor: item.dashed
                      ? "var(--color-cc-bg)"
                      : item.color,
                  }}
                  onMouseEnter={highlight(item.id)}
                />
              );
            }),
          )}
          {activeSeries?.points.map((point) => {
            const top = y(point.rps);
            const below = activeSeries.points.some(
              (item) => y(item.rps) < LABEL_CLEARANCE,
            );
            return (
              <span
                key={point.slot}
                className={`bg-cc-bg/90 border-cc-card-border pointer-events-none absolute flex flex-col rounded border px-1.5 py-0.5 font-mono text-[11px] leading-4 whitespace-nowrap ${
                  point.slot === 0
                    ? "-translate-x-2"
                    : point.slot === SLOTS - 1
                      ? "-translate-x-[calc(100%-0.5rem)]"
                      : "-translate-x-1/2"
                } ${below ? "translate-y-3" : "-translate-y-[calc(100%+0.75rem)]"} ${point.slot === SLOTS - 1 ? "text-right" : ""}`}
                style={{ left: `${SLOT_X[point.slot]}%`, top: `${top}%` }}
              >
                <span className="text-cc-heading">{point.version}</span>
                <span className="text-cc-ink-dim">
                  {formatRps(point.rps)}
                  <span className="hidden sm:inline"> req/s</span>
                </span>
              </span>
            );
          })}
        </div>
      </div>
      <div className="mt-6 space-y-4">
        {GROUPS.map((group) => {
          const members = series
            .filter((item) => item.composition === group.id)
            .toSorted((a, b) => b.points.at(-1)!.rps - a.points.at(-1)!.rps);
          if (members.length === 0) {
            return null;
          }
          return (
            <div key={group.id}>
              <h5 className="text-cc-ink-dim mb-2 text-xs font-medium">
                {group.name}
              </h5>
              <ul className="grid gap-1.5 sm:grid-cols-2 lg:grid-cols-3">
                {members.map((item) => {
                  const change = changeOf(item);
                  const newest = item.points.at(-1)!;
                  const isActive = item.id === active;
                  return (
                    <li key={item.id}>
                      <button
                        type="button"
                        aria-pressed={pinned === item.id}
                        onMouseEnter={highlight(item.id)}
                        onMouseLeave={highlight(undefined)}
                        onFocus={highlight(item.id)}
                        onBlur={highlight(undefined)}
                        onClick={() =>
                          setPinned((current) =>
                            current === item.id ? undefined : item.id,
                          )
                        }
                        className={`flex w-full items-center gap-3 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
                          isActive
                            ? "bg-cc-white/5"
                            : "border-cc-card-border hover:bg-cc-white/2"
                        } ${active && !isActive ? "opacity-50" : ""}`}
                        style={
                          isActive ? { borderColor: item.color } : undefined
                        }
                      >
                        <svg
                          className="h-2.5 w-6 shrink-0 overflow-visible"
                          viewBox="0 0 24 10"
                          aria-hidden="true"
                        >
                          <line
                            x1="1"
                            y1="5"
                            x2="23"
                            y2="5"
                            stroke={item.color}
                            strokeWidth="2"
                            strokeLinecap="round"
                            strokeDasharray={item.dashed ? "4 3" : undefined}
                          />
                          <circle
                            cx="12"
                            cy="5"
                            r="3"
                            stroke={item.color}
                            strokeWidth="2"
                            fill={
                              item.dashed ? "var(--color-cc-bg)" : item.color
                            }
                          />
                        </svg>
                        <span className="min-w-0 flex-1">
                          <span className="text-cc-heading block truncate font-semibold">
                            {item.name}{" "}
                            <span className="text-cc-ink-dim font-mono text-[11px] font-normal">
                              {item.runtime}
                            </span>
                          </span>
                          <span className="text-cc-ink-dim block truncate font-mono text-[11px]">
                            {item.points
                              .map((point) => point.version)
                              .join(" → ")}
                          </span>
                        </span>
                        <span className="shrink-0 text-right font-mono text-xs tabular-nums">
                          <span className="text-cc-heading block">
                            {formatRps(newest.rps)}
                          </span>
                          {change === undefined ? (
                            <span className="text-cc-ink-dim block font-sans">
                              1 version
                            </span>
                          ) : (
                            <span
                              className={`block ${change >= 0 ? "text-cc-success" : "text-cc-danger"}`}
                              title={`Change from ${item.points[0].version} to ${newest.version}`}
                            >
                              {change >= 0 ? "▲" : "▼"} {formatChange(change)}
                            </span>
                          )}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            </div>
          );
        })}
      </div>
      <table className="sr-only">
        <caption>
          Median requests per second of the newest versions of each gateway,
          oldest first.
        </caption>
        <thead>
          <tr>
            <th scope="col">Gateway</th>
            <th scope="col">Versions and median requests per second</th>
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
                  .map((point) => `${point.version}: ${formatRps(point.rps)}`)
                  .join(", ")}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </figure>
  );
}
