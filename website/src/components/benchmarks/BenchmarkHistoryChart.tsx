"use client";

import {
  COMPOSITION_GROUPS,
  formatChange,
  formatNumber,
  HISTORY_SLOTS,
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

const SLOT_X = Array.from(
  { length: HISTORY_SLOTS },
  (_, index) => ((index + 0.5) / HISTORY_SLOTS) * 100,
);
const SLOT_NAMES = [
  { long: "Two releases ago", short: "Two back" },
  { long: "Previous release", short: "Previous" },
  { long: "Latest release", short: "Latest" },
];
const DASH = "4 4";
/** Labels move below their points when the active series comes this close to the top, in percent. */
const LABEL_CLEARANCE = 20;

const compact = new Intl.NumberFormat("en-US", {
  notation: "compact",
  maximumFractionDigits: 1,
});

/** A dot and connector chart of each router's recent releases, framed as a panel with the legend in its footer. */
export function BenchmarkHistoryChart({
  id,
  scenario,
  variants,
  stableOnly,
}: BenchmarkHistoryChartProps) {
  const { active, pinned, setHovered, togglePinned } = useSeriesHighlight();

  const series = historySeries(scenario, variants, stableOnly);
  if (series.length === 0) {
    return null;
  }

  const { ticks, y } = historyScale(
    series.flatMap((item) => item.points.map((point) => point.rps)),
  );
  const widestTick = ticks
    .map((tick) => compact.format(tick))
    .reduce(
      (widest, label) => (label.length > widest.length ? label : widest),
      "",
    );
  const ordered = series.toSorted(
    (a, b) => (a.id === active ? 1 : 0) - (b.id === active ? 1 : 0),
  );
  const activeSeries = series.find((item) => item.id === active);
  const labelsBelow =
    activeSeries?.points.some((point) => y(point.rps) < LABEL_CLEARANCE) ??
    false;
  const highlight = (seriesId: string | undefined) => () =>
    setHovered(seriesId);
  const titleId = `${id}-history`;

  return (
    <figure
      className="border-cc-card-border relative rounded-lg border"
      aria-labelledby={titleId}
    >
      <figcaption className="border-cc-card-border flex flex-col gap-1 border-b px-5 py-4 sm:flex-row sm:items-baseline sm:justify-between sm:gap-6">
        <div>
          <h3
            id={titleId}
            className="font-heading text-h6 text-cc-heading font-semibold"
          >
            Throughput across recent releases
          </h3>
          <p className="text-caption text-cc-ink-dim mt-1">
            Median requests per second of each router&apos;s three newest
            releases in this scenario. Select a router to see its versions.
          </p>
        </div>
      </figcaption>

      <div className="px-3 pt-10 pb-5 sm:px-5" aria-hidden="true">
        <div className="grid grid-cols-[2.25rem_1fr]">
          <div className="relative h-60 sm:h-72">
            <div className="text-cc-ink-dim absolute inset-y-0 right-2 text-[11px] tabular-nums">
              <span className="invisible block h-0 overflow-hidden">
                {widestTick}
              </span>
              <abbr
                title="Requests per second"
                className="absolute bottom-full left-1/2 mb-3 -translate-x-1/2 no-underline"
              >
                RPS
              </abbr>
              {ticks.map((tick) => (
                <span
                  key={tick}
                  className="absolute right-0 -translate-y-1/2"
                  style={{ top: `${y(tick)}%` }}
                >
                  {compact.format(tick)}
                </span>
              ))}
            </div>
          </div>
          <div
            className="relative h-60 sm:h-72"
            onMouseLeave={highlight(undefined)}
          >
            {ticks.map((tick) => (
              <div
                key={tick}
                className="border-cc-card-border absolute inset-x-0 border-t"
                style={{ top: `${y(tick)}%` }}
              />
            ))}
            {SLOT_X.map((x) => (
              <div
                key={x}
                className="border-cc-card-border/50 absolute inset-y-0 border-l"
                style={{ left: `${x}%` }}
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
                      strokeWidth={isActive ? 2 : 1.5}
                      strokeOpacity={isActive ? 1 : active ? 0.12 : 0.6}
                      strokeDasharray={item.dashed ? DASH : undefined}
                      strokeLinejoin="round"
                      strokeLinecap="round"
                      vectorEffect="non-scaling-stroke"
                      className="motion-safe:transition-[stroke-opacity]"
                    />
                    <polyline
                      points={points}
                      fill="none"
                      stroke="transparent"
                      strokeWidth={24}
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
                    className="absolute flex size-7 -translate-1/2 items-center justify-center"
                    style={{
                      left: `${SLOT_X[point.slot]}%`,
                      top: `${y(point.rps)}%`,
                    }}
                    onMouseEnter={highlight(item.id)}
                  >
                    <span
                      className={`ring-cc-bg block rounded-full border-2 ring-2 motion-safe:transition-[opacity,width,height] ${
                        isActive ? "size-3.5" : "size-3"
                      } ${active && !isActive ? "opacity-20" : ""}`}
                      style={{
                        borderColor: item.color,
                        backgroundColor: item.dashed
                          ? "var(--color-cc-bg)"
                          : item.color,
                      }}
                    />
                  </span>
                );
              }),
            )}

            {activeSeries?.points.map((point) => (
              <span
                key={point.slot}
                className={`bg-cc-bg/90 pointer-events-none absolute flex -translate-x-1/2 flex-col items-center rounded px-1.5 py-0.5 text-[11px] leading-4 whitespace-nowrap tabular-nums ${
                  labelsBelow
                    ? "translate-y-3"
                    : "-translate-y-[calc(100%+0.75rem)]"
                }`}
                style={{
                  left: `${SLOT_X[point.slot]}%`,
                  top: `${y(point.rps)}%`,
                }}
              >
                <span className="text-cc-heading font-medium">
                  {point.version}
                </span>
                <span className="text-cc-ink-dim">
                  {formatNumber(point.rps)}
                </span>
              </span>
            ))}
          </div>

          <div className="col-start-2 grid grid-cols-3 pt-3">
            {SLOT_NAMES.map((name) => (
              <span
                key={name.long}
                className="text-cc-ink-dim text-center text-[11px] leading-4 sm:text-xs"
              >
                <span className="sm:hidden">{name.short}</span>
                <span className="hidden sm:inline">{name.long}</span>
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
          const groupId = `${id}-legend-${group.id}`;
          return (
            <div key={group.id}>
              <p
                id={groupId}
                className="text-cc-ink-dim mb-1.5 text-xs font-medium"
              >
                {group.name}
              </p>
              <ul
                aria-labelledby={groupId}
                className="-mx-2 flex flex-wrap gap-x-1 gap-y-0.5"
              >
                {members.map((item) => (
                  <LegendButton
                    key={item.id}
                    series={item}
                    active={active}
                    pinned={pinned === item.id}
                    onHighlight={setHovered}
                    onToggle={togglePinned}
                  />
                ))}
              </ul>
            </div>
          );
        })}
      </div>

      <table className="sr-only">
        <caption>
          Median requests per second of the newest releases of each router,
          oldest first.
        </caption>
        <thead>
          <tr>
            <th scope="col">Router</th>
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
                  .map(
                    (point) => `${point.version}: ${formatNumber(point.rps)}`,
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
  readonly onToggle: (id: string) => void;
}

function LegendButton({
  series,
  active,
  pinned,
  onHighlight,
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
        onFocus={() => onHighlight(series.id)}
        onBlur={() => onHighlight(undefined)}
        onClick={() => onToggle(series.id)}
        className={`focus-visible:outline-cc-accent flex items-center gap-2.5 rounded-md px-2 py-1.5 text-left text-sm transition-colors focus-visible:outline-2 ${
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
            <span className="text-cc-ink-dim">req/s</span>
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
