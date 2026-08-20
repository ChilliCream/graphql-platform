"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import type { TemplateSummary } from "@/src/data/templates/templates";
import {
  FILTER_AXES,
  clientLabel,
  languageLabel,
  productLabel,
  topologyLabel,
  useCaseLabel,
} from "@/src/data/templates/filters";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { SearchIcon } from "@/src/icons/Search";
import { TemplateCard } from "./TemplateCard";

// Faceted template catalog: text search plus the six filter axes from the
// data model, all URL-synced so any filtered view is shareable. Within an
// axis options combine with OR, except Product mix which matches all
// selected products (finding library combinations is the point).

type AxisKey = (typeof FILTER_AXES)[number]["key"];

type Selection = Readonly<Record<AxisKey, readonly string[]>>;

const EMPTY_SELECTION: Selection = {
  topology: [],
  use: [],
  language: [],
  client: [],
  product: [],
  agent: [],
};

const axisValues = (template: TemplateSummary, axis: AxisKey): readonly string[] => {
  switch (axis) {
    case "topology":
      return [template.topology];
    case "use":
      return template.useCases;
    case "language":
      return [template.language];
    case "client":
      return template.clients;
    case "product":
      return template.products;
    case "agent":
      return template.agentReady ? ["yes"] : [];
  }
};

const matchesSelection = (template: TemplateSummary, selection: Selection): boolean =>
  FILTER_AXES.every((axis) => {
    const selected = selection[axis.key];
    if (selected.length === 0) {
      return true;
    }
    const values = axisValues(template, axis.key);
    return axis.key === "product"
      ? selected.every((key) => values.includes(key))
      : selected.some((key) => values.includes(key));
  });

const searchHaystack = (template: TemplateSummary): string =>
  [
    template.title,
    template.tagline,
    topologyLabel(template.topology),
    languageLabel(template.language),
    ...template.useCases.map(useCaseLabel),
    ...template.clients.map(clientLabel),
    ...template.products.map(productLabel),
  ]
    .join(" ")
    .toLowerCase();

const matchesQuery = (template: TemplateSummary, query: string): boolean => {
  const tokens = query.toLowerCase().split(/\s+/).filter(Boolean);
  if (tokens.length === 0) {
    return true;
  }
  const haystack = searchHaystack(template);
  return tokens.every((token) => haystack.includes(token));
};

interface TemplateCatalogProps {
  readonly templates: readonly TemplateSummary[];
}

export function TemplateCatalog({ templates }: TemplateCatalogProps) {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const selection: Selection = useMemo(() => {
    const fromParam = (axis: AxisKey) => searchParams.get(axis)?.split(",").filter(Boolean) ?? [];
    return {
      topology: fromParam("topology"),
      use: fromParam("use"),
      language: fromParam("language"),
      client: fromParam("client"),
      product: fromParam("product"),
      agent: fromParam("agent"),
    };
  }, [searchParams]);

  const queryParam = searchParams.get("q") ?? "";
  const [query, setQuery] = useState(queryParam);

  const applyToUrl = (next: Selection, nextQuery: string) => {
    const params = new URLSearchParams(searchParams.toString());
    for (const axis of FILTER_AXES) {
      if (next[axis.key].length === 0) {
        params.delete(axis.key);
      } else {
        params.set(axis.key, next[axis.key].join(","));
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
    const handle = setTimeout(() => applyToUrl(selection, query), 250);
    return () => clearTimeout(handle);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- applyToUrl/selection are derived from the same params snapshot
  }, [query, queryParam]);

  const toggleOption = (axis: (typeof FILTER_AXES)[number], key: string) => {
    const current = selection[axis.key];
    const next =
      axis.kind === "multi"
        ? current.includes(key)
          ? current.filter((value) => value !== key)
          : [...current, key]
        : current.includes(key)
          ? []
          : [key];
    applyToUrl({ ...selection, [axis.key]: next }, query);
  };

  const clearAll = () => {
    setQuery("");
    applyToUrl(EMPTY_SELECTION, "");
  };

  const activeCount = FILTER_AXES.reduce((sum, axis) => sum + selection[axis.key].length, 0);
  const visibleTemplates = templates.filter(
    (template) => matchesSelection(template, selection) && matchesQuery(template, query),
  );

  // Facet counts: how many templates an option yields given the current
  // search and the other axes' selections.
  const optionCount = (axis: (typeof FILTER_AXES)[number], key: string) =>
    templates.filter(
      (template) => matchesSelection(template, { ...selection, [axis.key]: [key] }) && matchesQuery(template, query),
    ).length;

  return (
    <section id="catalog" className="scroll-mt-24 py-14 sm:py-20">
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mb-8 font-semibold text-balance">
        Pick your starting point
      </h2>

      <div className="gap-10 lg:grid lg:grid-cols-[15rem_minmax(0,1fr)]">
        <aside>
          <div className="lg:sticky lg:top-28">
            <div className="relative mb-6">
              <SearchIcon className="text-cc-ink-dim pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 fill-current" />
              <input
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search templates…"
                aria-label="Search templates"
                className="border-cc-card-border bg-cc-surface/60 text-cc-heading placeholder:text-cc-ink-dim focus:border-cc-accent w-full rounded-lg border py-2.5 pr-3 pl-10 text-sm transition-colors outline-none"
              />
            </div>
            <details className="border-cc-card-border group rounded-lg border lg:hidden [&_summary]:cursor-pointer">
              <summary className="text-cc-heading flex items-center justify-between px-4 py-3 text-sm font-medium select-none">
                Filters
                {activeCount > 0 && (
                  <span className="bg-cc-accent/15 text-cc-accent rounded-full px-2.5 py-0.5 font-mono text-xs">
                    {activeCount}
                  </span>
                )}
              </summary>
              <div className="border-cc-card-border border-t px-4 pt-2 pb-4">
                <FacetGroups
                  selection={selection}
                  onToggle={toggleOption}
                  optionCount={optionCount}
                  activeCount={activeCount}
                  onClearAll={clearAll}
                />
              </div>
            </details>
            <div className="max-lg:hidden">
              <FacetGroups
                selection={selection}
                onToggle={toggleOption}
                optionCount={optionCount}
                activeCount={activeCount}
                onClearAll={clearAll}
              />
            </div>
          </div>
        </aside>

        <div className="max-lg:mt-10">
          {visibleTemplates.length > 0 ? (
            <div className="grid gap-6 md:grid-cols-2 2xl:grid-cols-3">
              {visibleTemplates.map((template) => (
                <TemplateCard key={template.slug} template={template} />
              ))}
            </div>
          ) : (
            <div className="border-cc-card-border rounded-2xl border border-dashed px-8 py-20 text-center">
              <p className="text-cc-heading font-heading text-lg font-semibold">No templates match</p>
              <p className="text-cc-ink-dim mx-auto mt-2 max-w-md text-sm leading-relaxed">
                No template covers that combination yet. Loosen a filter, or start from the closest starter and check
                the docs for the missing piece.
              </p>
              <button
                type="button"
                onClick={clearAll}
                className="border-cc-card-border text-cc-heading hover:border-cc-accent hover:text-cc-accent mt-6 cursor-pointer rounded-full border px-5 py-2 text-sm font-medium transition-colors"
              >
                Clear search & filters
              </button>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function FacetGroups({
  selection,
  onToggle,
  optionCount,
  activeCount,
  onClearAll,
}: {
  readonly selection: Selection;
  readonly onToggle: (axis: (typeof FILTER_AXES)[number], key: string) => void;
  readonly optionCount: (axis: (typeof FILTER_AXES)[number], key: string) => number;
  readonly activeCount: number;
  readonly onClearAll: () => void;
}) {
  return (
    <div className="space-y-7">
      {activeCount > 0 && (
        <button
          type="button"
          onClick={onClearAll}
          className="text-cc-accent hover:text-cc-accent-hover cursor-pointer font-mono text-[0.65rem] tracking-[0.15em] uppercase"
        >
          ✕ Clear all filters ({activeCount})
        </button>
      )}
      {FILTER_AXES.map((axis) => (
        <fieldset key={axis.key}>
          <legend className="text-cc-ink-dim font-mono text-[0.65rem] font-semibold tracking-[0.18em] uppercase">
            {axis.label}
            {axis.key === "product" && <span className="ml-2 normal-case opacity-60">matches all</span>}
          </legend>
          <div className="mt-3 space-y-0.5">
            {axis.options.map((option) => {
              const active = selection[axis.key].includes(option.key);
              const count = optionCount(axis, option.key);
              return (
                <button
                  key={option.key}
                  type="button"
                  onClick={() => onToggle(axis, option.key)}
                  aria-pressed={active}
                  disabled={!active && count === 0}
                  className="group flex w-full cursor-pointer items-center gap-2.5 rounded-md px-2 py-1.5 text-left text-sm transition-colors disabled:cursor-default disabled:opacity-40"
                >
                  <span
                    aria-hidden="true"
                    className={`flex size-4 shrink-0 items-center justify-center border transition-colors ${
                      axis.kind === "single" || axis.kind === "toggle" ? "rounded-full" : "rounded-[4px]"
                    } ${
                      active
                        ? "bg-cc-accent border-cc-accent"
                        : "border-cc-card-border-hover group-hover:border-cc-accent/70"
                    }`}
                  >
                    {active && <CheckGlyph className="text-cc-surface size-3" />}
                  </span>
                  <span
                    className={`flex-1 truncate transition-colors ${
                      active ? "text-cc-heading" : "text-cc-ink-dim group-hover:text-cc-heading"
                    }`}
                  >
                    {option.label}
                  </span>
                  <span className="text-cc-ink-dim font-mono text-[0.65rem]">{count}</span>
                </button>
              );
            })}
          </div>
        </fieldset>
      ))}
    </div>
  );
}
