"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";

import {
  type ClientKey,
  type LanguageKey,
  type ProductKey,
  type TopologyKey,
  type UseCaseKey,
} from "@/data/templates/filters";
import { TEMPLATES, type Template } from "@/data/templates/templates";

import { FilterRail, parseFilters } from "./FilterRail";
import { TemplateCard } from "./TemplateCard";

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

// Section 02: filter rail + grid. Filter state lives in the URL via
// useSearchParams; we read it once per render and pass it as a prop to the
// rail and the grid. The grid filters TEMPLATES client-side because there
// are <50 templates total and a search index is overkill.
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

  return (
    <section className="cc-tp-section cc-tp-gallery">
      <div className="cc-section-label">
        <span className="num">02</span> Gallery
      </div>
      <div className="cc-tp-gallery-inner">
        <FilterRail active={filters} totalCount={TEMPLATES.length} />
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
            <div className="cc-tp-empty">
              <h3>No templates match your filters.</h3>
              <p>Loosen a chip or two and the gallery comes back.</p>
              <button type="button" onClick={clearAll}>
                Clear all →
              </button>
            </div>
          ) : (
            <div className="cc-tp-grid">
              {visible.map((t) => (
                <TemplateCard key={t.slug} template={t} />
              ))}
            </div>
          )}
        </div>
      </div>
    </section>
  );
};
