"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo, useState } from "react";

import { FILTER_AXES } from "@/data/templates/filters";

import { FilterAxis } from "./FilterAxis";

// Per-axis active-set readers. Filters are URL-driven so any combination is
// shareable: /templates?topology=federation&product=fusion,nitro&agent=yes
//
// Encoding: comma-separated values per axis. Empty axis means "no filter".
// Single-select axes (topology) accept only the first value. Toggle axes
// (agent-ready) treat the presence of "yes" as on, anything else as off.
export type FilterState = Readonly<Record<string, ReadonlySet<string>>>;

export const parseFilters = (
  searchParams: URLSearchParams | ReadonlyURLSearchParamsLike
): FilterState => {
  const out: Record<string, Set<string>> = {};
  for (const axis of FILTER_AXES) {
    const raw = searchParams.get(axis.key);
    if (!raw) {
      out[axis.key] = new Set();
      continue;
    }
    const parts = raw
      .split(",")
      .map((p) => p.trim())
      .filter((p) => p.length > 0);
    if (axis.kind === "single") {
      out[axis.key] = new Set(parts.slice(0, 1));
    } else {
      // Validate: keep only known option keys.
      const valid = new Set(axis.options.map((o) => o.key));
      out[axis.key] = new Set(parts.filter((p) => valid.has(p as never)));
    }
  }
  return out as FilterState;
};

interface ReadonlyURLSearchParamsLike {
  get(name: string): string | null;
  toString(): string;
}

interface FilterRailProps {
  readonly active: FilterState;
  readonly totalCount: number;
}

// Rail with all 6 axes, sticky on desktop, collapses to a bottom-sheet
// button on mobile. State lives in the URL — we read via useSearchParams
// and write via router.replace so back/forward navigation is filter-aware.
export const FilterRail: FC<FilterRailProps> = ({ active }) => {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [open, setOpen] = useState(false);

  const totalActive = useMemo(() => {
    let n = 0;
    for (const axis of FILTER_AXES) {
      n += active[axis.key]?.size ?? 0;
    }
    return n;
  }, [active]);

  const writeFilters = useCallback(
    (next: FilterState): void => {
      const params = new URLSearchParams(searchParams?.toString() ?? "");
      for (const axis of FILTER_AXES) {
        const set = next[axis.key];
        if (!set || set.size === 0) {
          params.delete(axis.key);
        } else {
          params.set(axis.key, [...set].join(","));
        }
      }
      const qs = params.toString();
      router.replace(qs ? `${pathname}?${qs}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams]
  );

  const toggle = useCallback(
    (axisKey: string, optionKey: string): void => {
      const axis = FILTER_AXES.find((a) => a.key === axisKey);
      if (!axis) {
        return;
      }
      const current = active[axisKey] ?? new Set<string>();
      const next = new Set(current);
      if (axis.kind === "single") {
        // Single-select: clicking the active key clears it, otherwise replace.
        if (next.has(optionKey)) {
          next.delete(optionKey);
        } else {
          next.clear();
          next.add(optionKey);
        }
      } else if (axis.kind === "toggle") {
        if (next.has(optionKey)) {
          next.delete(optionKey);
        } else {
          next.add(optionKey);
        }
      } else {
        if (next.has(optionKey)) {
          next.delete(optionKey);
        } else {
          next.add(optionKey);
        }
      }
      const out = { ...active, [axisKey]: next } as Record<string, Set<string>>;
      writeFilters(out as unknown as FilterState);
    },
    [active, writeFilters]
  );

  const clearAxis = useCallback(
    (axisKey: string): void => {
      const out = { ...active, [axisKey]: new Set<string>() } as Record<
        string,
        Set<string>
      >;
      writeFilters(out as unknown as FilterState);
    },
    [active, writeFilters]
  );

  const clearAll = useCallback((): void => {
    const out: Record<string, Set<string>> = {};
    for (const axis of FILTER_AXES) {
      out[axis.key] = new Set();
    }
    writeFilters(out as unknown as FilterState);
  }, [writeFilters]);

  return (
    <>
      <button
        type="button"
        className="cc-tp-rail-toggle"
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
        aria-controls="cc-tp-rail-panel"
      >
        <span>Filters</span>
        <span className="badge" aria-label={`${totalActive} active`}>
          {totalActive}
        </span>
      </button>
      <aside
        id="cc-tp-rail-panel"
        className={`cc-tp-rail${open ? "" : " is-collapsed"}`}
        aria-label="Filter templates"
      >
        <div className="cc-tp-rail-head">
          <span className="cc-tp-rail-title">Filters</span>
          {totalActive > 0 && (
            <button
              type="button"
              className="cc-tp-rail-clearall"
              onClick={clearAll}
            >
              Clear all
            </button>
          )}
        </div>
        {FILTER_AXES.map((axis) => (
          <FilterAxis
            key={axis.key}
            title={axis.label}
            kind={axis.kind}
            options={axis.options}
            active={active[axis.key] ?? new Set<string>()}
            onToggle={(k) => toggle(axis.key, k)}
            onClear={() => clearAxis(axis.key)}
          />
        ))}
      </aside>
    </>
  );
};
