"use client";

import {
  createContext,
  useContext,
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";
import { SUBSECTION_HEADING } from "@/src/components/benchmarks/benchmarkHeadings";
import {
  COMPOSITION_GROUPS,
  formatHistoryDate,
  historyDateTicks,
  historyScale,
  historyTimeScale,
} from "@/src/components/benchmarks/benchmarkView";
import { useSeriesHighlight } from "@/src/components/benchmarks/useSeriesHighlight";
import { WarningIcon } from "@/src/icons/Warning";
import type { Composition } from "@/src/helpers/benchmarkReport";

/** What the frame knows about one server-rendered series of the chart. */
export interface HistoryChartSeries {
  readonly id: string;
  readonly composition: Composition;
  readonly technology: string;
  readonly preview: boolean;
  /** The lowest charted requests per second. */
  readonly low: number;
  /** The highest charted requests per second. */
  readonly high: number;
}

interface HistoryChartHighlight {
  readonly active: string | undefined;
  readonly pinned: string | undefined;
  readonly highlight: (seriesId: string | undefined) => void;
  readonly endHover: () => void;
  readonly focus: (seriesId: string | undefined) => void;
  readonly toggle: (seriesId: string) => void;
}

const Highlight = createContext<HistoryChartHighlight | null>(null);

/** The highlighted and pinned series of the enclosing history chart. */
export function useHistoryChartHighlight() {
  const highlight = useContext(Highlight);
  if (!highlight) {
    throw new Error(
      "History chart highlights require a BenchmarkHistoryChartFrame.",
    );
  }
  return highlight;
}

interface SelectedPoint {
  readonly seriesId: string;
  readonly x: number;
  /** The position on the scale of the server-rendered plot, 0 at the top and 100 at the bottom. */
  readonly y: number;
  /** The lines of the point's server-rendered tooltip. */
  readonly title: string;
  readonly throughput: string;
  readonly version: string;
  readonly composition: string;
  readonly note: string | undefined;
  /** Whether the pointer selected the point, as opposed to keyboard focus. */
  readonly pointer: boolean;
}

interface BenchmarkHistoryChartFrameProps {
  readonly id: string;
  readonly series: readonly HistoryChartSeries[];
  /** The requests per second at the bottom and the top of the server-rendered plot. */
  readonly domain: readonly [low: number, high: number];
  /** Every charted run date, oldest first. */
  readonly dates: readonly number[];
  /** The series lines and point buttons, positioned on the scale of `domain`. */
  readonly plot: ReactNode;
  readonly legend: ReactNode;
  /** The data of every series for assistive technology, which the chart hides visually. */
  readonly table: ReactNode;
}

const compact = new Intl.NumberFormat("en-US", {
  notation: "compact",
  maximumFractionDigits: 1,
});

const quote = (value: string) => JSON.stringify(value);

const pointButton = (target: EventTarget | null) =>
  target instanceof Element
    ? target.closest<HTMLElement>("button[data-point]")
    : null;

/** The interactive shell of a history chart: it shows the series that match the filters on a fitted scale, and owns the axes, highlights and tooltip. */
export function BenchmarkHistoryChartFrame({
  id,
  series,
  domain,
  dates,
  plot,
  legend,
  table,
}: BenchmarkHistoryChartFrameProps) {
  const { filters } = useBenchmarkPreferences();
  const figureRef = useRef<HTMLElement>(null);
  const plotRef = useRef<HTMLDivElement>(null);
  const [plotWidth, setPlotWidth] = useState(0);
  useEffect(() => {
    const element = plotRef.current;
    if (!element) {
      return;
    }
    const observer = new ResizeObserver(([entry]) => {
      setPlotWidth(entry.contentRect.width);
    });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);
  const visible = series.filter(
    (item) =>
      (filters.showPreviews || !item.preview) &&
      (filters.technologies.length === 0 ||
        filters.technologies.includes(item.technology)),
  );
  const {
    active,
    pinned,
    setHovered,
    setFocused,
    togglePinned,
    clearPointerHighlight,
    clearHoverSoon,
  } = useSeriesHighlight(visible.map((item) => item.id));
  const [selected, setSelected] = useState<SelectedPoint>();
  const clearPointerSelection = useEffectEvent((target: Node | null) => {
    if (!target || !plotRef.current?.contains(target)) {
      setSelected(undefined);
    }
    if (!target || !figureRef.current?.contains(target)) {
      clearPointerHighlight();
    }
  });
  const pointerSelection = selected?.pointer === true;
  // Hides a pointer tooltip whose leave event was missed, so it never outlives the pointer.
  useEffect(() => {
    if (!pointerSelection) {
      return;
    }
    const onMove = (event: PointerEvent) =>
      clearPointerSelection(event.target as Node);
    const onOut = (event: PointerEvent) => {
      if (!event.relatedTarget) {
        clearPointerSelection(null);
      }
    };
    const onBlur = () => clearPointerSelection(null);
    document.addEventListener("pointermove", onMove, { passive: true });
    document.addEventListener("pointerout", onOut);
    window.addEventListener("blur", onBlur);
    return () => {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerout", onOut);
      window.removeEventListener("blur", onBlur);
    };
  }, [pointerSelection]);
  if (visible.length === 0) {
    return null;
  }
  const select = (button: HTMLElement, pointer: boolean) => {
    const [title, throughput, version, composition, note] = [
      ...button.querySelector("[data-tooltip]")!.children,
    ].map((line) => line.textContent ?? "");
    setSelected({
      seriesId: button.dataset.series!,
      x: Number(button.style.getPropertyValue("--x")),
      y: Number(button.style.getPropertyValue("--y")),
      title,
      throughput,
      version,
      composition,
      note,
      pointer,
    });
  };
  const { ticks, y } = historyScale(
    visible.flatMap((item) => [item.low, item.high]),
  );
  const top = y(domain[1]);
  const height = y(domain[0]) - top;
  const fitted = (position: number) => top + (position / 100) * height;
  const { x } = historyTimeScale(dates);
  const dateTicks = historyDateTicks(dates, plotWidth);
  const includeYear =
    new Date(dates[0]).getUTCFullYear() !==
    new Date(dates.at(-1)!).getUTCFullYear();
  const align = (position: number) =>
    position > 85 ? "-100%" : position < 15 ? "0" : "-50%";
  const pointY = selected && fitted(selected.y);
  const scope = `#${id}-chart`;
  const styles = [
    ...series
      .filter((item) => !visible.includes(item))
      .map((item) => `${scope} [data-series=${quote(item.id)}]{display:none}`),
    ...COMPOSITION_GROUPS.filter(
      (group) => !visible.some((item) => item.composition === group.id),
    ).map(
      (group) =>
        `${scope} [data-legend-group=${quote(group.id)}]{display:none}`,
    ),
    ...(active
      ? [
          `${scope} svg[data-series]:not([data-series=${quote(active)}]){opacity:.15}`,
          `${scope} svg[data-series=${quote(active)}]{z-index:1}`,
          `${scope} svg[data-series=${quote(active)}] line{stroke-width:2}`,
          `${scope} button[data-point]:not([data-series=${quote(active)}])>span{opacity:.2;background:none;box-shadow:none}`,
          `${scope} button[data-point][data-series=${quote(active)}]{z-index:2}`,
        ]
      : []),
  ].join("");
  const onMouseLeave = () => {
    clearPointerHighlight();
    setSelected(undefined);
  };
  return (
    <Highlight.Provider
      value={{
        active,
        pinned,
        highlight: setHovered,
        endHover: clearHoverSoon,
        focus: setFocused,
        toggle: (seriesId) => {
          setSelected(undefined);
          togglePinned(seriesId);
        },
      }}
    >
      <figure
        ref={figureRef}
        id={`${id}-chart`}
        data-benchmark-scroll-anchor={`chart-${id}`}
        className="border-cc-card-border relative rounded-lg border"
        aria-labelledby={`${id}-history`}
        onMouseLeave={onMouseLeave}
      >
        {styles && <style>{styles}</style>}
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
              ref={plotRef}
              className="relative h-60 sm:h-72"
              onMouseLeave={onMouseLeave}
              onMouseOver={(event) => {
                const button = pointButton(event.target);
                if (
                  !button ||
                  button.contains(event.relatedTarget as Node | null)
                ) {
                  return;
                }
                const seriesId = button.dataset.series!;
                if (pinned && pinned !== seriesId) {
                  return;
                }
                setHovered(seriesId);
                select(button, true);
              }}
              onMouseOut={(event) => {
                const button = pointButton(event.target);
                if (
                  !button ||
                  button.contains(event.relatedTarget as Node | null)
                ) {
                  return;
                }
                setHovered(undefined);
                setSelected(undefined);
              }}
              onFocus={(event) => {
                const button = pointButton(event.target);
                const seriesId = button?.dataset.series;
                if (
                  button &&
                  (!pinned || pinned === seriesId) &&
                  button.matches(":focus-visible")
                ) {
                  setFocused(seriesId);
                  select(button, false);
                }
              }}
              onBlur={(event) => {
                if (pointButton(event.target)) {
                  setFocused(undefined);
                  setSelected(undefined);
                }
              }}
              onClick={(event) => {
                const button = pointButton(event.target);
                if (button) {
                  togglePinned(button.dataset.series!);
                  select(button, true);
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
              {dateTicks.map((date) => (
                <div
                  key={date}
                  className="border-cc-card-border/50 absolute inset-y-0 border-l"
                  style={{ left: `${x(date)}%` }}
                />
              ))}
              {selected && (
                <div
                  className="border-cc-ink-dim pointer-events-none absolute inset-y-0 z-[1] border-l border-dashed"
                  style={{ left: `${selected.x}%` }}
                />
              )}
              <div
                className="pointer-events-none absolute inset-x-0"
                style={{ top: `${top}%`, height: `${height}%` }}
              >
                {plot}
              </div>
              {selected && pointY !== undefined && (
                <div
                  role="tooltip"
                  className="bg-cc-bg text-cc-heading border-cc-card-border pointer-events-none absolute z-10 rounded border px-3 py-2 text-xs whitespace-nowrap shadow-lg"
                  style={{
                    left: `${selected.x}%`,
                    top: `${pointY}%`,
                    transform: `translate(${align(selected.x)}, ${pointY < 25 ? "16px" : "calc(-100% - 16px)"})`,
                  }}
                >
                  <div className="font-semibold">{selected.title}</div>
                  <div>{selected.throughput}</div>
                  <div className="text-cc-ink-dim mt-1">{selected.version}</div>
                  <div className="text-cc-ink-dim">{selected.composition}</div>
                  {selected.note && (
                    <div className="text-cc-danger border-cc-card-border mt-2 flex items-center gap-1 border-t pt-2 text-[11px]">
                      <WarningIcon className="size-3 shrink-0" />
                      {selected.note}
                    </div>
                  )}
                </div>
              )}
            </div>
            <div className="relative col-start-2 mt-3 h-9 text-[10px] sm:text-[11px]">
              {dateTicks.map((date) => (
                <span
                  key={date}
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
        <div
          className="border-cc-card-border space-y-4 border-t px-5 py-4"
          onMouseLeave={() => setHovered(undefined)}
        >
          {legend}
        </div>
        <div className="sr-only">{table}</div>
      </figure>
    </Highlight.Provider>
  );
}
