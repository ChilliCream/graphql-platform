"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";
import styled from "styled-components";

import {
  GRID_TOKENS,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
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

import { IntegrationGridCard } from "./IntegrationGridCard";

// Each category renders as its own GridSection (hairlineTop) with a 1-line
// band above a 4-up GridRow. The full set of categories collapses by URL
// filter the same way as the Default variant: ?type=, ?category=, ?q= drive
// which categories surface and which integrations they hold.
//
// Cap per category mirrors the Default 6-card cap so the page stays a
// browse-by-category index, not an infinite scroll. When a category overflows
// the cap, a "Browse all in <Category> ->" affordance routes to the same URL
// the Default page uses.
const PER_CATEGORY_CAP = 8;

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

export const IntegrationsGridByCategory: FC = () => {
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

  if (visibleCategories.length === 0) {
    return (
      <GridSection variant="default" hairlineTop hairlineBottom>
        <Empty>
          <h3>No integrations match.</h3>
          <p>Try a different filter.</p>
        </Empty>
      </GridSection>
    );
  }

  return (
    <>
      {visibleCategories.map((cat) => {
        const items = byCategory.get(cat.key) ?? [];
        const visible = items.slice(0, PER_CATEGORY_CAP);
        return (
          <GridSection
            key={cat.key}
            id={`cat-${cat.key}`}
            variant="default"
            hairlineTop
          >
            <Head>
              <HeadCopy>
                <CategoryEyebrow>{cat.key.replace("-", " / ")}</CategoryEyebrow>
                <CategoryTitle>{cat.label}</CategoryTitle>
                <CategoryTagline>{cat.tagline}</CategoryTagline>
              </HeadCopy>
              {items.length > PER_CATEGORY_CAP && (
                <BrowseAll href={`/integrations?category=${cat.key}`}>
                  Browse all <span aria-hidden>&rarr;</span>
                </BrowseAll>
              )}
            </Head>
            <GridRow cols={4}>
              {visible.map((integration) => (
                <IntegrationGridCard
                  key={integration.slug}
                  integration={integration}
                />
              ))}
            </GridRow>
          </GridSection>
        );
      })}
    </>
  );
};

const Head = styled.div`
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
  padding: 0 0 28px;
`;

const HeadCopy = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
`;

const CategoryEyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const CategoryTitle = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(24px, 2.6vw, 36px);
  font-weight: 600;
  letter-spacing: -0.025em;
  line-height: 1.1;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
`;

const CategoryTagline = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  max-width: 60ch;
`;

const BrowseAll = styled.a`
  flex: 0 0 auto;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkPrimary};
  text-decoration: none;
  white-space: nowrap;
  display: inline-flex;
  align-items: center;
  gap: 6px;

  &:hover {
    color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  }
`;

const Empty = styled.div`
  text-align: center;
  padding: clamp(64px, 8vw, 120px) 0;
  color: ${GRID_TOKENS.inkBody};

  h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 24px;
    font-weight: 600;
    letter-spacing: -0.02em;
    color: ${GRID_TOKENS.inkPrimary};
    margin: 0 0 8px;
  }

  p {
    font-size: 14px;
    margin: 0;
  }
`;
