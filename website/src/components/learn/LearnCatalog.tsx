"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { CardGrid } from "@/src/components/CardGrid";
import type { FilterAxisDef, LearnContentType, ProductKey } from "@/src/data/learn/facets";
import {
  clientLabel,
  CONTENT_TYPE_OPTIONS,
  contentTypeLabel,
  languageLabel,
  PRODUCT_OPTIONS,
  productLabel,
  TEMPLATE_FILTER_AXES,
  topologyLabel,
  useCaseLabel,
} from "@/src/data/learn/facets";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { LearnCard } from "./LearnCard";
import { LearnEmptyState } from "./LearnEmptyState";
import { LearnFacetBar, type ContentTypeSelection } from "./LearnFacetBar";

// Faceted /learn catalog: content type is the primary (single-select) facet,
// product mix is always-visible, and the template-only axes (topology, use
// case, language, client, agent-ready) apply only while Templates is
// selected. All state is URL-synced so a filtered view is shareable, typing
// debounced 250ms like the old TemplateCatalog.

// Template-only axes, excluding product mix: product has its own
// always-visible row (section 3.2 of the design spec), even though it lives
// inside TEMPLATE_FILTER_AXES in the data model.
const TEMPLATE_AXES: readonly FilterAxisDef[] = TEMPLATE_FILTER_AXES.filter((axis) => axis.key !== "product");

const VALID_CONTENT_TYPES = new Set<string>(CONTENT_TYPE_OPTIONS.map((option) => option.key));
const VALID_PRODUCT_KEYS = new Set<string>(PRODUCT_OPTIONS.map((option) => option.key));

type AxisSelection = Readonly<Record<string, readonly string[]>>;

const emptyAxisSelection = (): AxisSelection =>
  Object.fromEntries(TEMPLATE_AXES.map((axis) => [axis.key, [] as readonly string[]]));

const axisValues = (item: LearnItemSummary, axisKey: string): readonly string[] => {
  if (item.type !== "template") {
    return [];
  }
  switch (axisKey) {
    case "topology":
      return [item.topology];
    case "use":
      return item.useCases;
    case "language":
      return [item.language];
    case "client":
      return item.clients;
    case "agent":
      return item.agentReady ? ["yes"] : [];
    default:
      return [];
  }
};

const matchesAxes = (item: LearnItemSummary, selection: AxisSelection): boolean =>
  TEMPLATE_AXES.every((axis) => {
    const selected = selection[axis.key] ?? [];
    if (selected.length === 0) {
      return true;
    }
    return selected.some((key) => axisValues(item, axis.key).includes(key));
  });

const matchesProducts = (item: LearnItemSummary, selected: readonly ProductKey[]): boolean =>
  selected.length === 0 || selected.every((key) => item.products.includes(key));

const searchHaystack = (item: LearnItemSummary): string => {
  const parts = [item.title, item.tagline, contentTypeLabel(item.type), ...item.products.map(productLabel)];
  if (item.type === "template") {
    parts.push(
      topologyLabel(item.topology),
      languageLabel(item.language),
      ...item.useCases.map(useCaseLabel),
      ...item.clients.map(clientLabel),
    );
  }
  return parts.join(" ").toLowerCase();
};

const matchesQuery = (item: LearnItemSummary, query: string): boolean => {
  const tokens = query.toLowerCase().split(/\s+/).filter(Boolean);
  if (tokens.length === 0) {
    return true;
  }
  const haystack = searchHaystack(item);
  return tokens.every((token) => haystack.includes(token));
};

interface LearnCatalogProps {
  readonly items: readonly LearnItemSummary[];
}

export function LearnCatalog({ items }: LearnCatalogProps) {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const rawType = searchParams.get("type");
  const contentType: ContentTypeSelection = VALID_CONTENT_TYPES.has(rawType ?? "")
    ? (rawType as LearnContentType)
    : "all";

  const productSelection: readonly ProductKey[] = useMemo(
    () =>
      (searchParams.get("product")?.split(",").filter(Boolean) ?? []).filter((key): key is ProductKey =>
        VALID_PRODUCT_KEYS.has(key),
      ),
    [searchParams],
  );

  const axisSelection: AxisSelection = useMemo(() => {
    const fromParam = (axisKey: string) => searchParams.get(axisKey)?.split(",").filter(Boolean) ?? [];
    return Object.fromEntries(TEMPLATE_AXES.map((axis) => [axis.key, fromParam(axis.key)]));
  }, [searchParams]);

  const queryParam = searchParams.get("q") ?? "";
  const [query, setQuery] = useState(queryParam);

  const applyToUrl = (
    nextType: ContentTypeSelection,
    nextProducts: readonly ProductKey[],
    nextAxes: AxisSelection,
    nextQuery: string,
  ) => {
    const params = new URLSearchParams(searchParams.toString());
    if (nextType === "all") {
      params.delete("type");
    } else {
      params.set("type", nextType);
    }
    if (nextProducts.length === 0) {
      params.delete("product");
    } else {
      params.set("product", nextProducts.join(","));
    }
    for (const axis of TEMPLATE_AXES) {
      const selected = nextAxes[axis.key] ?? [];
      if (selected.length === 0) {
        params.delete(axis.key);
      } else {
        params.set(axis.key, selected.join(","));
      }
    }
    if (nextQuery.trim() === "") {
      params.delete("q");
    } else {
      params.set("q", nextQuery.trim());
    }
    const encoded = params.toString();
    router.replace(encoded ? `${pathname}?${encoded}` : pathname, { scroll: false });
  };

  // Debounce typing into the URL; selection changes apply immediately.
  useEffect(() => {
    if (query === queryParam) {
      return;
    }
    const handle = setTimeout(() => applyToUrl(contentType, productSelection, axisSelection, query), 250);
    return () => clearTimeout(handle);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- applyToUrl/selection are derived from the same params snapshot
  }, [query, queryParam]);

  const setContentType = (next: ContentTypeSelection) => applyToUrl(next, productSelection, axisSelection, query);

  const toggleProduct = (key: ProductKey) => {
    const next = productSelection.includes(key)
      ? productSelection.filter((value) => value !== key)
      : [...productSelection, key];
    applyToUrl(contentType, next, axisSelection, query);
  };

  const toggleAxis = (axis: FilterAxisDef, key: string) => {
    const current = axisSelection[axis.key] ?? [];
    const next =
      axis.kind === "multi"
        ? current.includes(key)
          ? current.filter((value) => value !== key)
          : [...current, key]
        : current.includes(key)
          ? []
          : [key];
    applyToUrl(contentType, productSelection, { ...axisSelection, [axis.key]: next }, query);
  };

  const clearAll = () => {
    setQuery("");
    applyToUrl("all", [], emptyAxisSelection(), "");
  };

  const matchesFilters = (item: LearnItemSummary) => {
    if (contentType !== "all" && item.type !== contentType) {
      return false;
    }
    if (!matchesProducts(item, productSelection)) {
      return false;
    }
    if (contentType === "template" && !matchesAxes(item, axisSelection)) {
      return false;
    }
    return matchesQuery(item, query);
  };

  const visibleItems = items.filter(matchesFilters);

  const typeCount = (type: ContentTypeSelection) =>
    items.filter((item) => (type === "all" || item.type === type) && matchesQuery(item, query)).length;

  const productCount = (key: ProductKey) =>
    items.filter(
      (item) =>
        (contentType === "all" || item.type === contentType) &&
        matchesProducts(item, [...productSelection.filter((selected) => selected !== key), key]) &&
        (contentType !== "template" || matchesAxes(item, axisSelection)) &&
        matchesQuery(item, query),
    ).length;

  const axisOptionCount = (axis: FilterAxisDef, key: string) =>
    items.filter(
      (item) =>
        item.type === "template" &&
        matchesProducts(item, productSelection) &&
        matchesAxes(item, { ...axisSelection, [axis.key]: [key] }) &&
        matchesQuery(item, query),
    ).length;

  const activeFilterCount =
    (contentType !== "all" ? 1 : 0) +
    productSelection.length +
    TEMPLATE_AXES.reduce((sum, axis) => sum + (axisSelection[axis.key]?.length ?? 0), 0) +
    (query.trim() ? 1 : 0);

  const typeHasAnyItems = contentType === "all" || items.some((item) => item.type === contentType);

  return (
    <section className="py-14 sm:py-20">
      <LearnFacetBar
        contentType={contentType}
        onContentTypeChange={setContentType}
        typeCount={typeCount}
        productSelection={productSelection}
        onToggleProduct={toggleProduct}
        productCount={productCount}
        axes={TEMPLATE_AXES}
        axisSelection={axisSelection}
        onToggleAxis={toggleAxis}
        axisOptionCount={axisOptionCount}
        query={query}
        onQueryChange={setQuery}
        activeFilterCount={activeFilterCount}
        onClearAll={clearAll}
      />

      <div className="mt-8 min-h-[24rem]">
        {!typeHasAnyItems ? (
          <LearnEmptyState
            heading={`No ${contentTypeLabel(contentType as LearnContentType).toLowerCase()} yet`}
            description="New content lands here as it ships. Browse templates in the meantime."
            actionLabel="Show everything"
            onAction={clearAll}
          />
        ) : visibleItems.length > 0 ? (
          <>
            <p className="text-cc-ink-dim text-caption mb-4">
              {visibleItems.length} {visibleItems.length === 1 ? "result" : "results"}
            </p>
            <CardGrid cols={3} step="progressive" itemsStretch>
              {visibleItems.map((item) => (
                <LearnCard key={`${item.type}-${item.slug}`} item={item} />
              ))}
            </CardGrid>
          </>
        ) : (
          <LearnEmptyState
            heading="Nothing matches"
            description="Loosen a filter or clear the search."
            actionLabel="Clear search & filters"
            onAction={clearAll}
          />
        )}
      </div>
    </section>
  );
}
