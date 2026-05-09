"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";
import styled from "styled-components";

import { GridRow, GridSection } from "@/components/redesign-system/grid";
import { FILTER_AXES } from "@/data/templates/filters";
import { TEMPLATES, type Template } from "@/data/templates/templates";

import { type FilterState, parseFilters } from "../FilterRail";
import { matchesFilters } from "../TemplatesGrid";

import { TemplatesGridCard } from "./TemplatesGridCard";

// Section 02: filter strip + 3-column gallery for the Grid variant.
//
// Filter logic re-uses the URL-driven `parseFilters` + `matchesFilters` from
// the Default variant so deep links round-trip across variants. The chip row
// renders the same TAB_DEFS as `FilterBar` but as square GridButton-style
// chips: ghost when inactive, secondary when active.
//
// The gallery is a 3-column `<GridRow cols={3}>` of square cards. Each card
// holds the existing thumbnail SVG full-bleed at the top, then the title,
// 1-line tagline, product chips, and a "View →" CTA at the bottom.

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

export const TemplatesGridGallery: FC = () => {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const filters = useMemo(() => {
    return parseFilters(searchParams ?? new URLSearchParams());
  }, [searchParams]);

  const visible = useMemo<readonly Template[]>(() => {
    return TEMPLATES.filter((t) => matchesFilters(t, filters));
  }, [filters]);

  const totalActive = useMemo(() => {
    let n = 0;
    for (const set of Object.values(filters)) {
      n += set.size;
    }
    return n;
  }, [filters]);

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
      const current = filters[axisKey] ?? new Set<string>();
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
      const out = { ...filters, [axisKey]: next } as Record<
        string,
        Set<string>
      >;
      writeFilters(out as unknown as FilterState);
    },
    [filters, writeFilters]
  );

  const clearAll = useCallback((): void => {
    const out: Record<string, Set<string>> = {};
    for (const axis of FILTER_AXES) {
      out[axis.key] = new Set();
    }
    writeFilters(out as unknown as FilterState);
  }, [writeFilters]);

  return (
    <GridSection hairlineBottom id="templates-gallery">
      <FilterStrip>
        <FilterRow role="group" aria-label="Filter templates">
          <Chip
            type="button"
            data-active={totalActive === 0}
            onClick={clearAll}
            aria-pressed={totalActive === 0}
          >
            All
          </Chip>
          {TAB_DEFS.map((tab) => {
            const isActive = filters[tab.axis]?.has(tab.key) ?? false;
            return (
              <Chip
                key={`${tab.axis}:${tab.key}`}
                type="button"
                data-active={isActive}
                onClick={() => toggle(tab.axis, tab.key)}
                aria-pressed={isActive}
              >
                {tab.label}
              </Chip>
            );
          })}
        </FilterRow>
        <CountLine>
          Showing <strong>{visible.length}</strong> of {TEMPLATES.length}
        </CountLine>
      </FilterStrip>
      {visible.length === 0 ? (
        <EmptyState onClear={clearAll} />
      ) : (
        <GridRow cols={3}>
          {visible.map((t) => (
            <TemplatesGridCard key={t.slug} template={t} />
          ))}
        </GridRow>
      )}
    </GridSection>
  );
};

interface EmptyStateProps {
  readonly onClear: () => void;
}

const EmptyState: FC<EmptyStateProps> = ({ onClear }) => {
  return (
    <Empty role="status" aria-live="polite">
      <h3>No templates match.</h3>
      <p>Loosen a chip or two and the gallery comes back.</p>
      <button type="button" onClick={onClear}>
        Clear all →
      </button>
    </Empty>
  );
};

const FilterStrip = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
  padding-bottom: 20px;
  margin-bottom: 0;
`;

const FilterRow = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
`;

// Square ghost chip (inactive) flips to secondary (active) with a hairline
// border. Mirrors GridButton's variant="ghost" / variant="secondary"
// rhythm without nesting an actual button-as-button (these emit
// onClick + aria-pressed, not navigation).
const Chip = styled.button`
  display: inline-flex;
  align-items: center;
  padding: 8px 14px;
  border-radius: 0;
  background: transparent;
  color: rgba(245, 241, 234, 0.62);
  border: 1px solid transparent;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  cursor: pointer;
  transition: color 0.15s ease, background 0.15s ease, border-color 0.15s ease;

  &:hover,
  &:focus-visible {
    color: var(--cc-ink, #f5f1ea);
  }

  &[data-active="true"] {
    color: var(--cc-ink, #f5f1ea);
    border-color: var(--cc-grid-hairline-strong, rgba(245, 241, 234, 0.32));
    background: rgba(245, 241, 234, 0.04);
  }

  &:focus-visible {
    outline: 2px solid var(--cc-accent, currentColor);
    outline-offset: 2px;
  }
`;

const CountLine = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: rgba(245, 241, 234, 0.62);

  strong {
    color: var(--cc-ink, #f5f1ea);
    font-weight: 500;
  }
`;

const Empty = styled.div`
  border: 1px solid var(--cc-grid-hairline, rgba(245, 241, 234, 0.16));
  padding: 56px 28px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;

  h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 22px;
    font-weight: 600;
    letter-spacing: -0.02em;
    color: var(--cc-ink, #f5f1ea);
    margin: 0;
  }

  p {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 14px;
    color: rgba(245, 241, 234, 0.62);
    margin: 0;
    line-height: 1.55;
  }

  button {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink, #f5f1ea);
    padding: 10px 18px;
    border: 1px solid var(--cc-grid-hairline-strong, rgba(245, 241, 234, 0.32));
    border-radius: 0;
    background: transparent;
    cursor: pointer;
    transition: background 0.15s ease, color 0.15s ease;

    &:hover,
    &:focus-visible {
      background: rgba(245, 241, 234, 0.04);
    }
  }
`;
