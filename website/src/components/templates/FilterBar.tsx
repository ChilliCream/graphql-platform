"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";

import { FILTER_AXES } from "@/data/templates/filters";

import type { FilterState } from "./FilterRail";

interface FilterBarProps {
  readonly active: FilterState;
}

// Compact horizontal version of the filter rail, used at the top of the
// gallery instead of the sticky 6-axis sidebar when the catalog is small
// (TEMPLATES.length <= 12). The 6 axes still apply, but they collapse into
// one wrapping row of category-tabs ("All / Topology / Patterns / Stacks /
// Agent-Ready") that round-trips to the same URL state as FilterRail. Once
// the catalog grows past the threshold, TemplatesGrid restores the rail.
//
// Tab definitions are derived from the existing FILTER_AXES, so adding a
// chip in one axis automatically appears in the bar's "All chips" overflow.
//
// "All" clears every axis. Each tab corresponds to a single chip on a
// single axis, the four most useful entry points across the 6 axes:
//   - Topology: federation
//   - Use case: cqrs, realtime, observability, multi-tenant, llm-mcp
//   - Agent-ready toggle
const TAB_DEFS: readonly {
  readonly label: string;
  readonly axis: string;
  readonly key: string;
}[] = [
  { label: "Topology", axis: "topology", key: "federation" },
  { label: "Polyglot", axis: "topology", key: "polyglot" },
  { label: "CQRS", axis: "use", key: "cqrs" },
  { label: "Realtime", axis: "use", key: "realtime" },
  { label: "Observability", axis: "use", key: "observability" },
  { label: "Multi-tenant", axis: "use", key: "multi-tenant" },
  { label: "Agent-ready", axis: "agent", key: "yes" },
];

export const FilterBar: FC<FilterBarProps> = ({ active }) => {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

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

  const toggleTab = useCallback(
    (axisKey: string, optionKey: string): void => {
      const axis = FILTER_AXES.find((a) => a.key === axisKey);
      if (!axis) {
        return;
      }
      const current = active[axisKey] ?? new Set<string>();
      const next = new Set(current);
      if (axis.kind === "single") {
        if (next.has(optionKey)) {
          next.delete(optionKey);
        } else {
          next.clear();
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

  const clearAll = useCallback((): void => {
    const out: Record<string, Set<string>> = {};
    for (const axis of FILTER_AXES) {
      out[axis.key] = new Set();
    }
    writeFilters(out as unknown as FilterState);
  }, [writeFilters]);

  return (
    <div className="cc-tp-filterbar" role="group" aria-label="Filter templates">
      <button
        type="button"
        className={`cc-tp-filterbar-tab${
          totalActive === 0 ? " is-active" : ""
        }`}
        onClick={clearAll}
        aria-pressed={totalActive === 0}
      >
        All
      </button>
      {TAB_DEFS.map((tab) => {
        const isActive = active[tab.axis]?.has(tab.key) ?? false;
        return (
          <button
            key={`${tab.axis}:${tab.key}`}
            type="button"
            className={`cc-tp-filterbar-tab${isActive ? " is-active" : ""}`}
            onClick={() => toggleTab(tab.axis, tab.key)}
            aria-pressed={isActive}
          >
            {tab.label}
          </button>
        );
      })}
    </div>
  );
};
