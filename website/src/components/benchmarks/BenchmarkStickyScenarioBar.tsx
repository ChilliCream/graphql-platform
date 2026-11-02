"use client";

import Link from "next/link";
import { useEffect, useRef } from "react";
import { BenchmarkFilterButton } from "@/src/components/benchmarks/BenchmarkFilterButton";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";
import { BENCHMARK_SCENARIO_PATHS } from "@/src/components/benchmarks/benchmarkRoutes";
import { SCENARIO_INFO } from "@/src/components/benchmarks/benchmarkView";
import type { BenchmarkControls } from "@/src/components/benchmarks/useBenchmarkControls";

/** The height of the sticky site header in pixels. */
const HEADER_HEIGHT = 72;

interface BenchmarkStickyScenarioBarProps {
  readonly controls: BenchmarkControls;
}

/** A one-line copy of the scenario tiles, fixed below the site header between scrolling past the tiles and scrolling past the last results panel. Its filter button appears once the bar covers the inline filter button. */
export function BenchmarkStickyScenarioBar({
  controls,
}: BenchmarkStickyScenarioBarProps) {
  const { scenarioBar, setScenarioBar } = useBenchmarkPreferences();
  const { visible, filter } = scenarioBar;
  const bar = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const root = bar.current?.closest("[data-benchmark-explorer]") ?? document;
    let frame = 0;
    const update = () => {
      frame = 0;
      const rects = (selector: string) =>
        [...root.querySelectorAll(selector)]
          .map((element) => element.getBoundingClientRect())
          .filter((rect) => rect.height > 0);
      const tiles = rects("[data-benchmark-scenario-tiles]")[0];
      const panel = rects("[data-benchmark-panel]").at(-1);
      const inlineFilter = rects("[data-benchmark-inline-filter]")[0];
      const barBottom = HEADER_HEIGHT + (bar.current?.offsetHeight ?? 0);
      const next = {
        visible:
          tiles !== undefined &&
          tiles.bottom <= HEADER_HEIGHT &&
          (panel === undefined || panel.bottom > barBottom),
        filter: inlineFilter === undefined || inlineFilter.top < barBottom,
      };
      setScenarioBar((current) =>
        current.visible === next.visible && current.filter === next.filter
          ? current
          : next,
      );
    };
    const schedule = () => {
      if (!frame) {
        frame = requestAnimationFrame(update);
      }
    };
    schedule();
    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("resize", schedule);
    return () => {
      cancelAnimationFrame(frame);
      window.removeEventListener("scroll", schedule);
      window.removeEventListener("resize", schedule);
    };
  }, [setScenarioBar]);

  return (
    <div
      inert={!visible}
      data-benchmark-controls
      className={`pointer-events-none fixed inset-x-0 top-18 z-30 transition-[translate,opacity] duration-200 md:px-12 ${visible ? "translate-y-0 opacity-100" : "-translate-y-full opacity-0"}`}
    >
      <div
        ref={bar}
        className={`bg-cc-surface border-cc-white/20 mx-auto grid max-w-7xl border-b bg-linear-to-b from-white/7 to-white/3 shadow-[0_14px_32px_-10px_rgb(0_0_0/0.85)] transition-[grid-template-columns] duration-200 md:border-x ${filter ? "grid-cols-[repeat(3,minmax(0,1fr))_3rem] sm:grid-cols-[repeat(3,minmax(0,1fr))_3.5rem]" : "grid-cols-[repeat(3,minmax(0,1fr))_0rem]"} ${visible ? "pointer-events-auto" : ""}`}
      >
        {controls.scenarios.map((scenario, index) => {
          const current = controls.selected === scenario.id;
          return (
            <Link
              key={scenario.id}
              href={BENCHMARK_SCENARIO_PATHS[scenario.id]}
              scroll={false}
              aria-current={current ? "page" : undefined}
              className={`border-cc-white/10 font-heading focus-visible:outline-cc-accent flex min-h-12 min-w-0 items-center px-3 text-sm font-semibold transition-colors first:pl-5 focus-visible:outline-2 focus-visible:-outline-offset-2 sm:min-h-14 sm:px-5 sm:text-lg ${index < controls.scenarios.length - 1 ? "border-r" : ""} ${current ? "bg-cc-white/8 text-cc-heading shadow-[inset_0_-4px_0_0_var(--color-cc-heading)]" : "text-cc-heading/75 hover:bg-cc-white/4 hover:text-cc-heading"}`}
            >
              <span className="truncate">
                {SCENARIO_INFO[scenario.id].name}
              </span>
            </Link>
          );
        })}
        <div inert={!filter} className="min-w-0">
          <div
            className={`border-cc-white/10 h-full w-12 border-l transition-opacity duration-200 sm:w-14 ${filter ? "opacity-100" : "invisible opacity-0"}`}
          >
            <BenchmarkFilterButton
              controls={controls}
              className="hover:bg-cc-white/4 h-full w-full"
            />
          </div>
        </div>
      </div>
    </div>
  );
}
