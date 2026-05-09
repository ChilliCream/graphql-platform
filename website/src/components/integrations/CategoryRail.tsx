"use client";

import React, { FC, useCallback } from "react";

import {
  CATEGORIES,
  type Category,
  type CategoryKey,
} from "@/data/integrations/categories";
import {
  INTEGRATIONS,
  integrationsByCategory,
} from "@/data/integrations/integrations";

interface CategoryRailProps {
  // Click handler scrolls the page to the category section. We intentionally
  // do NOT push the active category to the URL: it would conflict with the
  // ?type filter and force a router replace on every scroll.
  readonly onJump?: (category: CategoryKey) => void;
}

// Section 03: sticky left rail (desktop) or top chip strip (mobile) listing
// the eight canonical categories with per-category counts. Clicking a row
// scrolls to the matching category block. Same vocabulary as the templates
// FilterRail but simpler: one axis, no multi-select.
export const CategoryRail: FC<CategoryRailProps> = ({ onJump }) => {
  const handleClick = useCallback(
    (key: CategoryKey) =>
      (e: React.MouseEvent): void => {
        // Allow normal anchor behaviour as a no-JS fallback. When a click
        // handler is supplied, intercept and let it scroll smoothly.
        if (onJump) {
          e.preventDefault();
          onJump(key);
        }
      },
    [onJump]
  );

  const counts = INTEGRATIONS.reduce<Record<CategoryKey, number>>(
    (acc, integration) => {
      acc[integration.category] = (acc[integration.category] ?? 0) + 1;
      return acc;
    },
    {} as Record<CategoryKey, number>
  );

  return (
    <aside className="cc-in-rail" aria-label="Integration categories">
      <span className="cc-in-rail-title">Categories</span>
      {CATEGORIES.map((cat: Category) => {
        const count = counts[cat.key] ?? integrationsByCategory(cat.key).length;
        return (
          <a
            key={cat.key}
            href={`#cat-${cat.key}`}
            className="cc-in-rail-link"
            onClick={handleClick(cat.key)}
          >
            <span>{cat.label}</span>
            <span className="rail-count">{count}</span>
          </a>
        );
      })}
    </aside>
  );
};
