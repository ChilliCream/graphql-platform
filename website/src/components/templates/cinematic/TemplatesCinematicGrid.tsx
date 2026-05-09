"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic/ActLabel";
import { TEMPLATES, type Template } from "@/data/templates/templates";

import { FilterRail, parseFilters } from "../FilterRail";
import { TemplateCard } from "../TemplateCard";
import { matchesFilters } from "../TemplatesGrid";

import { CinematicFilterBar } from "./CinematicFilterBar";

// Threshold below which the cinematic gallery uses the prism chip row
// instead of the 6-axis sticky rail. Mirrors TemplatesGrid's RAIL_THRESHOLD
// so the variants stay symmetric.
const RAIL_THRESHOLD = 12;

// Cinematic gallery: same URL-driven filter behavior as the default grid;
// the prism `CinematicFilterBar` replaces the flat `FilterBar` chrome and
// `<ActLabel n="02" name="GALLERY" />` chapters the section. The 6-axis rail
// stays unchanged for >12 templates, since the rail is already its own
// strong piece of chrome and re-skinning it would duplicate FilterRail
// without an obvious payoff.
export const TemplatesCinematicGrid: FC = () => {
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
    <Band
      variant="default"
      ariaLabel="Templates gallery"
      className="cc-tp-cinematic-band"
    >
      <ActLabel n="02" name="Gallery" />
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
          <CinematicFilterBar active={filters} />
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

// Empty state mirrors TemplatesGrid's EmptyState verbatim so the cinematic
// variant doesn't regress the no-results affordance.
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
