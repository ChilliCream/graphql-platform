"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";

import { TerminalChipRow } from "@/components/redesign-system/cinematic/TerminalChipRow";
import { FILTER_AXES } from "@/data/templates/filters";

import type { FilterState } from "../FilterRail";

interface CinematicFilterBarProps {
  readonly active: FilterState;
}

// Cinematic filter row: each tab is rendered as a prism-bordered terminal
// chip from the redesign-system primitive, while staying functional. The
// underlying URL-driven toggle behavior is identical to FilterBar; only the
// chrome is replaced.
//
// The prism chip is presentational (renders a styled <span>); to keep chips
// clickable we wrap each one in an unstyled button that calls the existing
// toggle handler and forwards aria-pressed / is-active state via a wrapping
// class that TemplatesCinematicRoot styles.
//
// Tab definitions mirror FilterBar verbatim so deep links (`?topology=
// federation`, `?use=cqrs`, etc.) continue to round-trip across variants.
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

// Shared by "All" and the per-tab chips: a single prism chip rendered inside
// a button so click handling stays on the wrapper. We render each chip
// individually (not via TerminalChipRow's own row) so we can attach a
// per-chip handler and is-active state.
const PrismChipButton: FC<{
  readonly label: string;
  readonly isActive: boolean;
  readonly onClick: () => void;
}> = ({ label, isActive, onClick }) => {
  return (
    <button
      type="button"
      className={`cc-tp-cinematic-filterbar-btn${
        isActive ? " is-active" : " is-inactive"
      }`}
      onClick={onClick}
      aria-pressed={isActive}
    >
      <TerminalChipRow chips={[label]} accent="prism" />
    </button>
  );
};

export const CinematicFilterBar: FC<CinematicFilterBarProps> = ({ active }) => {
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
    <div
      className="cc-tp-cinematic-filterbar"
      role="group"
      aria-label="Filter templates"
    >
      <PrismChipButton
        label="All"
        isActive={totalActive === 0}
        onClick={clearAll}
      />
      {TAB_DEFS.map((tab) => {
        const isActive = active[tab.axis]?.has(tab.key) ?? false;
        return (
          <PrismChipButton
            key={`${tab.axis}:${tab.key}`}
            label={tab.label}
            isActive={isActive}
            onClick={() => toggleTab(tab.axis, tab.key)}
          />
        );
      })}
    </div>
  );
};
