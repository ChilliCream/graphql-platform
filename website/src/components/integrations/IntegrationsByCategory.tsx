"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";

import {
  CATEGORIES,
  categoryLabel,
  type CategoryKey,
} from "@/data/integrations/categories";
import {
  type Integration,
  INTEGRATIONS,
  recentlyAdded,
} from "@/data/integrations/integrations";

import { CategoryRail } from "./CategoryRail";
import { IntegrationCard } from "./IntegrationCard";

const PER_CATEGORY_CAP = 6;

// Apply the URL-driven type/search/category filters to the full catalogue.
// Same shape as the templates matchesFilters() helper: we read the URL once,
// fold it over the catalogue once, render once. Catalogue is <30 entries so
// this is well below any "we need a real index" threshold.
const applyFilters = (
  integrations: readonly Integration[],
  type: string | null,
  q: string,
  category: string | null
): readonly Integration[] => {
  const recent = new Set(recentlyAdded(6).map((i) => i.slug));
  const needle = q.trim().toLowerCase();

  return integrations.filter((i) => {
    if (type === "native" && i.type !== "native") {
      return false;
    }
    if (type === "community" && i.type !== "community") {
      return false;
    }
    if (type === "recent" && !recent.has(i.slug)) {
      return false;
    }
    if (category && i.category !== category) {
      return false;
    }
    if (needle.length > 0) {
      const hay =
        i.name.toLowerCase() +
        " " +
        i.tagline.toLowerCase() +
        " " +
        categoryLabel(i.category).toLowerCase();
      if (!hay.includes(needle)) {
        return false;
      }
    }
    return true;
  });
};

// Section 05: the catalogue body. Sticky left rail + a stack of category
// blocks on the right. Each block renders up to PER_CATEGORY_CAP cards plus
// a "Browse all in <Category> →" link when the category overflows. The whole
// stack reacts to the hero's Type pill and search box through the URL.
export const IntegrationsByCategory: FC = () => {
  const searchParams = useSearchParams();
  const type = searchParams?.get("type") ?? null;
  const q = searchParams?.get("q") ?? "";
  const category = searchParams?.get("category") ?? null;

  const filtered = useMemo(
    () => applyFilters(INTEGRATIONS, type, q, category),
    [type, q, category]
  );

  const byCategory = useMemo(() => {
    const out = new Map<CategoryKey, Integration[]>();
    for (const cat of CATEGORIES) {
      out.set(cat.key, []);
    }
    for (const integration of filtered) {
      out.get(integration.category)?.push(integration);
    }
    return out;
  }, [filtered]);

  const visibleCategories = CATEGORIES.filter(
    (cat) => (byCategory.get(cat.key)?.length ?? 0) > 0
  );

  return (
    <section className="cc-in-section cc-in-catalogue">
      <div className="cc-section-label">
        <span className="num">05</span> Browse
      </div>
      <div className="cc-in-catalogue-inner">
        <CategoryRail />
        <div className="cc-in-cat-stack">
          {visibleCategories.length === 0 ? (
            <div className="cc-in-empty">
              <h3>No integrations match.</h3>
              <p>Try a different filter or clear your search.</p>
            </div>
          ) : (
            visibleCategories.map((cat) => {
              const items = byCategory.get(cat.key) ?? [];
              const visible = items.slice(0, PER_CATEGORY_CAP);
              return (
                <div
                  key={cat.key}
                  id={`cat-${cat.key}`}
                  className="cc-in-cat-block"
                >
                  <div className="cc-in-cat-head">
                    <div>
                      <h2 className="display">{cat.label}</h2>
                      <p>{cat.tagline}</p>
                    </div>
                    {items.length > PER_CATEGORY_CAP && (
                      <a
                        href={`/integrations?category=${cat.key}`}
                        className="browse"
                      >
                        Browse all in {cat.label} →
                      </a>
                    )}
                  </div>
                  <div className="cc-in-grid">
                    {visible.map((integration) => (
                      <IntegrationCard
                        key={integration.slug}
                        integration={integration}
                      />
                    ))}
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>
    </section>
  );
};
