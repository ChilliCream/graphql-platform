"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";
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

import { CategoryRail } from "../CategoryRail";
import { IntegrationCard } from "../IntegrationCard";

const PER_CATEGORY_CAP = 6;

// Cinematic per-block chapter numbering: hero is `01`, spotlight is `02`,
// featured is `03`, so the first category block carries `04`. The label is
// rendered inside the block (band-relative), not in the band gutter, because
// many categories share one band.
const FIRST_CATEGORY_INDEX = 4;

const CATEGORY_LABEL_OVERRIDES: Partial<Record<CategoryKey, string>> = {
  auth: "Auth",
  "ai-agents": "AI & Agents",
  "ci-cd": "CI / CD",
  data: "Data",
  cloud: "Cloud",
  observability: "Observability",
  messaging: "Messaging",
  frontend: "Frontend",
};

const padNumber = (n: number): string => n.toString().padStart(2, "0");

// Apply the URL-driven type/search/category filters to the full catalogue.
// 1:1 with IntegrationsByCategory.applyFilters; duplicating the helper keeps
// the cinematic variant self-contained without exporting internal helpers.
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

// Cinematic by-category: same sticky-rail + category-stack layout as the
// default variant, but each visible category block carries its own
// `<ActLabel>` chapter marker (04 AI & AGENTS, 05 AUTH, 06 OBSERVABILITY,
// ...). The chapter numbering follows the page-level numbering: hero is 01,
// spotlight is 02, featured is 03, and each category block continues from 04.
//
// Only the visible categories are numbered (a category with zero matching
// integrations is skipped entirely, same as the default variant), so a tight
// filter doesn't leave gaps in the chapter sequence.
export const IntegrationsCinematicByCategory: FC = () => {
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
    <Band
      variant="default"
      className="cc-band cc-band-category"
      ariaLabel="Browse integrations by category"
    >
      <div className="cc-in-catalogue-inner">
        <CategoryRail />
        <div className="cc-in-cat-stack">
          {visibleCategories.length === 0 ? (
            <div className="cc-in-empty">
              <h3>No integrations match.</h3>
              <p>Try a different filter.</p>
            </div>
          ) : (
            visibleCategories.map((cat, index) => {
              const items = byCategory.get(cat.key) ?? [];
              const visible = items.slice(0, PER_CATEGORY_CAP);
              const isMarquee = index === 0;
              const blockClass = isMarquee
                ? "cc-in-cat-block is-marquee"
                : "cc-in-cat-block";
              const chapterN = padNumber(FIRST_CATEGORY_INDEX + index);
              const chapterName =
                CATEGORY_LABEL_OVERRIDES[cat.key] ?? cat.label;
              return (
                <div key={cat.key} id={`cat-${cat.key}`} className={blockClass}>
                  <ActLabel
                    n={chapterN}
                    name={chapterName}
                    className="cc-cinematic-cat-label"
                  />
                  <div className="cc-in-cat-head">
                    <div>
                      {isMarquee && (
                        <span className="eyebrow">Marquee category</span>
                      )}
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
    </Band>
  );
};
