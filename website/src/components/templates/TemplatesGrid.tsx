"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";

import { Band } from "@/components/redesign-system/Band";
import {
  type ClientKey,
  type LanguageKey,
  type ProductKey,
  type TopologyKey,
  type UseCaseKey,
} from "@/data/templates/filters";
import { TEMPLATES, type Template } from "@/data/templates/templates";

import { FilterBar } from "./FilterBar";
import { FilterRail, parseFilters } from "./FilterRail";
import { TemplateCard } from "./TemplateCard";

// Threshold below which the filter rail collapses to a horizontal chip bar.
// 6-axis sticky chrome on a tiny corpus reads as configurator overkill, so
// we only restore the rail once the catalog has enough rows to justify the
// vertical real estate.
const RAIL_THRESHOLD = 12;

const matchesFilters = (
  template: Template,
  filters: ReturnType<typeof parseFilters>
): boolean => {
  const topology = filters.topology;
  if (
    topology &&
    topology.size > 0 &&
    !topology.has(template.topology as TopologyKey)
  ) {
    return false;
  }
  const useCase = filters.use;
  if (
    useCase &&
    useCase.size > 0 &&
    !template.useCases.some((u) => useCase.has(u as UseCaseKey))
  ) {
    return false;
  }
  const language = filters.language;
  if (
    language &&
    language.size > 0 &&
    !language.has(template.language as LanguageKey)
  ) {
    return false;
  }
  const client = filters.client;
  if (
    client &&
    client.size > 0 &&
    !template.clients.some((c) => client.has(c as ClientKey))
  ) {
    return false;
  }
  const product = filters.product;
  if (
    product &&
    product.size > 0 &&
    !template.products.some((p) => product.has(p as ProductKey))
  ) {
    return false;
  }
  const agent = filters.agent;
  if (agent && agent.size > 0 && agent.has("yes") && !template.agentReady) {
    return false;
  }
  return true;
};

// Section 02: filter chrome + grid. Filter state lives in the URL via
// useSearchParams; we read it once per render and pass it as a prop to the
// chrome and the grid. The grid filters TEMPLATES client-side because there
// are <50 templates total and a search index is overkill.
//
// Chrome shape depends on catalog size: <=12 templates collapses to a single
// horizontal FilterBar above the grid (no rail container chrome), >12
// restores the 6-axis sticky FilterRail. Both round-trip to the same URL
// state so deep links survive the layout switch.
export const TemplatesGrid: FC = () => {
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

  const clearAll = useCallback((): void => {
    router.replace(pathname, { scroll: false });
  }, [pathname, router]);

  const useRail = TEMPLATES.length > RAIL_THRESHOLD;

  return (
    <Band variant="default" ariaLabel="Templates gallery">
      <div className="cc-section-label">
        <span className="num">02</span> Gallery
      </div>
      {useRail ? (
        <div className="cc-tp-gallery-inner">
          <FilterRail active={filters} totalCount={TEMPLATES.length} />
          <GridArea
            visible={visible}
            totalActive={totalActive}
            clearAll={clearAll}
          />
        </div>
      ) : (
        <div className="cc-tp-gallery-stack">
          <FilterBar active={filters} />
          <GridArea
            visible={visible}
            totalActive={totalActive}
            clearAll={clearAll}
          />
        </div>
      )}
    </Band>
  );
};

interface GridAreaProps {
  readonly visible: readonly Template[];
  readonly totalActive: number;
  readonly clearAll: () => void;
}

const GridArea: FC<GridAreaProps> = ({ visible, totalActive, clearAll }) => {
  return (
    <div className="cc-tp-grid-wrap">
      <div className="cc-tp-grid-bar">
        <span className="cc-tp-grid-count">
          Showing <strong>{visible.length}</strong> of {TEMPLATES.length}
        </span>
        {totalActive > 0 && (
          <button
            type="button"
            className="cc-tp-rail-clearall"
            onClick={clearAll}
          >
            Clear all filters
          </button>
        )}
      </div>
      {visible.length === 0 ? (
        <EmptyState onClear={clearAll} />
      ) : (
        <div className="cc-tp-grid">
          {visible.map((t) => (
            <TemplateCard key={t.slug} template={t} />
          ))}
        </div>
      )}
    </div>
  );
};

interface EmptyStateProps {
  readonly onClear: () => void;
}

// Empty-filter state. The icon is a stroke-rendered "no results" mark
// (intersecting circle with a slash) that picks up the page accent on the
// stroke, mirroring the brewer-icon vocabulary used elsewhere in the
// gallery.
const EmptyState: FC<EmptyStateProps> = ({ onClear }) => {
  return (
    <div className="cc-tp-empty" role="status" aria-live="polite">
      <svg
        className="cc-tp-empty-icon"
        viewBox="0 0 48 48"
        aria-hidden
        focusable="false"
      >
        <circle
          cx={24}
          cy={24}
          r={18}
          fill="none"
          stroke="var(--cc-accent, currentColor)"
          strokeWidth={1.6}
        />
        <line
          x1={11}
          y1={37}
          x2={37}
          y2={11}
          stroke="var(--cc-accent, currentColor)"
          strokeWidth={1.6}
          strokeLinecap="round"
        />
      </svg>
      <h3>No templates match.</h3>
      <p>Loosen a chip or two and the gallery comes back.</p>
      <button type="button" onClick={onClear}>
        Clear all →
      </button>
    </div>
  );
};
