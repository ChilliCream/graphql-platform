"use client";

import { useState, type CSSProperties, type ReactNode } from "react";
import { lightDark } from "@/src/components/lightDark";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";

const FIELD_STYLE = {
  "--vc-field": lightDark(
    "var(--color-cc-control-bg)",
    "rgba(255, 255, 255, 0.05)",
  ),
  "--vc-field-ring": lightDark(
    "rgba(7, 94, 117, 0.25)",
    "rgba(255, 255, 255, 0.1)",
  ),
} as CSSProperties;

const CHECK_STYLE = {
  "--vc-check": lightDark(
    "var(--color-cc-control-border)",
    "rgba(255, 255, 255, 0.3)",
  ),
  "--vc-check-ring": lightDark(
    "var(--color-cc-accent)",
    "rgba(255, 255, 255, 0.4)",
  ),
} as CSSProperties;
import { SearchIcon } from "@/src/icons/Search";

export interface VideoCatalogItem {
  readonly key: string;
  readonly topics: readonly string[];
  readonly year: string;
  readonly presenter: string | null;
  readonly hasExample: boolean;
  /** Lowercase text the search box matches against. */
  readonly searchText: string;
  /** The rendered card for this video. */
  readonly card: ReactNode;
}

interface VideoCatalogProps {
  /** Every video, newest first. */
  readonly items: readonly VideoCatalogItem[];
  /** Display names of the topic tags. */
  readonly topicLabels: Readonly<Record<string, string>>;
}

interface Filters {
  readonly query: string;
  readonly topics: ReadonlySet<string>;
  readonly years: ReadonlySet<string>;
  readonly presenters: ReadonlySet<string>;
  readonly exampleOnly: boolean;
}

type FacetKey = "topics" | "years" | "presenters" | "example";

const NO_FILTERS: Filters = {
  query: "",
  topics: new Set(),
  years: new Set(),
  presenters: new Set(),
  exampleOnly: false,
};

/**
 * The video catalog: a filter panel with search and facets on the left, the
 * matching video cards on the right. Facet counts reflect the other active
 * filters.
 */
export function VideoCatalog({ items, topicLabels }: VideoCatalogProps) {
  const [filters, setFilters] = useState<Filters>(NO_FILTERS);
  const [filtersOpen, setFiltersOpen] = useState(false);

  const visible = items.filter((item) => matches(item, filters));

  const topics = facetValues(items.flatMap((item) => item.topics));
  const years = facetValues(items.map((item) => item.year)).sort((a, b) =>
    b.localeCompare(a),
  );
  const presenters = facetValues(
    items.flatMap((item) => (item.presenter ? [item.presenter] : [])),
  );
  const exampleCount = items.filter((item) => item.hasExample).length;

  const countFor = (
    facet: FacetKey,
    test: (item: VideoCatalogItem) => boolean,
  ) =>
    items.filter((item) => matches(item, filters, facet) && test(item)).length;

  const toggle = (facet: "topics" | "years" | "presenters", value: string) =>
    setFilters((current) => {
      const next = new Set(current[facet]);
      if (next.has(value)) {
        next.delete(value);
      } else {
        next.add(value);
      }
      return { ...current, [facet]: next };
    });

  const clear = (facet: "topics" | "years" | "presenters") =>
    setFilters((current) => ({ ...current, [facet]: new Set<string>() }));

  const activeCount =
    filters.topics.size +
    filters.years.size +
    filters.presenters.size +
    (filters.exampleOnly ? 1 : 0) +
    (filters.query.trim() === "" ? 0 : 1);

  return (
    <div className="grid gap-8 lg:grid-cols-[15rem_minmax(0,1fr)] lg:gap-12">
      <div className="lg:sticky lg:top-24 lg:self-start">
        <button
          type="button"
          aria-expanded={filtersOpen}
          aria-controls="video-filters"
          onClick={() => setFiltersOpen((open) => !open)}
          className="border-cc-card-border text-cc-heading hover:border-cc-card-border-hover flex w-full cursor-pointer items-center justify-between rounded-full border px-4 py-2 text-sm transition-colors lg:hidden"
        >
          <span>
            Filter episodes
            {activeCount > 0 ? (
              <span className="text-cc-amber"> ({activeCount})</span>
            ) : null}
          </span>
          <ChevronDownIcon
            aria-hidden="true"
            className={`size-3 fill-current transition-transform ${filtersOpen ? "rotate-180" : ""}`}
          />
        </button>

        <aside
          id="video-filters"
          aria-label="Filter videos"
          className={`${filtersOpen ? "mt-5 block" : "hidden"} lg:mt-0 lg:block`}
        >
          <label className="relative block">
            <span className="sr-only">Search videos</span>
            <SearchIcon
              aria-hidden="true"
              className="text-cc-ink-dim pointer-events-none absolute top-1/2 left-3.5 size-3.5 -translate-y-1/2 fill-current"
            />
            <input
              type="search"
              value={filters.query}
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  query: event.target.value,
                }))
              }
              placeholder="Search videos"
              style={FIELD_STYLE}
              className="border-cc-card-border text-cc-ink placeholder:text-cc-ink-dim hover:border-cc-card-border-hover focus:border-cc-card-border-hover w-full rounded-full border bg-(--vc-field) py-2 pr-4 pl-10 text-sm transition-colors focus:ring-2 focus:ring-(--vc-field-ring) focus:outline-hidden"
            />
          </label>

          <FacetGroup
            title="Topic"
            onClear={filters.topics.size > 0 ? () => clear("topics") : null}
          >
            {topics.map((topic) => (
              <FacetOption
                key={topic}
                label={topicLabels[topic] ?? topic}
                count={countFor("topics", (item) =>
                  item.topics.includes(topic),
                )}
                checked={filters.topics.has(topic)}
                onChange={() => toggle("topics", topic)}
              />
            ))}
          </FacetGroup>

          {years.length > 1 ? (
            <FacetGroup
              title="Year"
              onClear={filters.years.size > 0 ? () => clear("years") : null}
            >
              {years.map((year) => (
                <FacetOption
                  key={year}
                  label={year}
                  count={countFor("years", (item) => item.year === year)}
                  checked={filters.years.has(year)}
                  onChange={() => toggle("years", year)}
                />
              ))}
            </FacetGroup>
          ) : null}

          {presenters.length > 1 ? (
            <FacetGroup
              title="Presenter"
              onClear={
                filters.presenters.size > 0 ? () => clear("presenters") : null
              }
            >
              {presenters.map((presenter) => (
                <FacetOption
                  key={presenter}
                  label={presenter}
                  count={countFor(
                    "presenters",
                    (item) => item.presenter === presenter,
                  )}
                  checked={filters.presenters.has(presenter)}
                  onChange={() => toggle("presenters", presenter)}
                />
              ))}
            </FacetGroup>
          ) : null}

          {exampleCount > 0 && exampleCount < items.length ? (
            <FacetGroup title="Source code" onClear={null}>
              <FacetOption
                label="Has example source"
                count={countFor("example", (item) => item.hasExample)}
                checked={filters.exampleOnly}
                onChange={() =>
                  setFilters((current) => ({
                    ...current,
                    exampleOnly: !current.exampleOnly,
                  }))
                }
              />
            </FacetGroup>
          ) : null}
        </aside>
      </div>

      <div className="min-w-0">
        <p className="sr-only" aria-live="polite" data-llms-ignore>
          {visible.length === items.length
            ? `Showing all ${items.length} episodes`
            : `Showing ${visible.length} of ${items.length} episodes`}
        </p>
        {visible.length > 0 ? (
          <ul className="m-0 grid list-none grid-cols-1 gap-5 p-0 sm:grid-cols-2 xl:grid-cols-3">
            {visible.map((item) => (
              <li key={item.key} className="m-0 flex p-0">
                {item.card}
              </li>
            ))}
          </ul>
        ) : (
          <div className="border-cc-card-border flex flex-col items-center gap-4 rounded-2xl border border-dashed px-6 py-16 text-center">
            <p className="text-cc-ink m-0">No videos match your filters.</p>
            <button
              type="button"
              onClick={() => setFilters(NO_FILTERS)}
              className="text-cc-heading decoration-cc-ink-faint hover:decoration-cc-heading cursor-pointer text-sm font-medium underline underline-offset-4 transition-colors"
            >
              Clear filters
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

interface FacetGroupProps {
  readonly title: string;
  /** Clears the group's selection; null when nothing in the group is selected. */
  readonly onClear: (() => void) | null;
  readonly children: ReactNode;
}

function FacetGroup({ title, onClear, children }: FacetGroupProps) {
  return (
    <fieldset className="border-cc-card-border m-0 mt-6 min-w-0 border-0 border-t p-0 pt-5">
      <legend className="sr-only">{title}</legend>
      <div className="mb-2 flex items-baseline justify-between gap-3">
        <Eyebrow size="2xs" color="ink-dim" aria-hidden="true">
          {title}
        </Eyebrow>
        {onClear ? (
          <button
            type="button"
            onClick={onClear}
            aria-label={`Clear ${title.toLowerCase()} filter`}
            className="text-cc-ink-dim hover:text-cc-heading cursor-pointer text-xs transition-colors"
          >
            Clear
          </button>
        ) : null}
      </div>
      <div className="flex flex-col">{children}</div>
    </fieldset>
  );
}

interface FacetOptionProps {
  readonly label: string;
  readonly count: number;
  readonly checked: boolean;
  readonly onChange: () => void;
}

function FacetOption({ label, count, checked, onChange }: FacetOptionProps) {
  const disabled = count === 0 && !checked;
  return (
    <label
      className={`flex items-center gap-3 rounded-md py-1.5 text-sm transition-colors ${
        disabled
          ? "text-cc-ink-dim/50 cursor-default"
          : "text-cc-ink hover:text-cc-heading cursor-pointer"
      }`}
    >
      <span className="relative flex size-4 flex-none">
        <input
          type="checkbox"
          checked={checked}
          disabled={disabled}
          onChange={onChange}
          style={CHECK_STYLE}
          className="peer checked:border-cc-amber checked:bg-cc-amber size-4 cursor-pointer appearance-none rounded-[4px] border border-(--vc-check) bg-transparent transition-colors focus-visible:ring-2 focus-visible:ring-(--vc-check-ring) focus-visible:outline-none disabled:cursor-default disabled:opacity-40"
        />
        <CheckGlyph className="text-cc-surface pointer-events-none absolute inset-0 hidden size-4 peer-checked:block" />
      </span>
      <span className="min-w-0 flex-1">{label}</span>
      <span className="text-cc-ink-dim font-mono text-xs tabular-nums">
        {count}
      </span>
    </label>
  );
}

function matches(item: VideoCatalogItem, filters: Filters, except?: FacetKey) {
  const term = filters.query.trim().toLowerCase();
  return (
    (term === "" || item.searchText.includes(term)) &&
    (except === "topics" ||
      filters.topics.size === 0 ||
      item.topics.some((topic) => filters.topics.has(topic))) &&
    (except === "years" ||
      filters.years.size === 0 ||
      filters.years.has(item.year)) &&
    (except === "presenters" ||
      filters.presenters.size === 0 ||
      (item.presenter !== null && filters.presenters.has(item.presenter))) &&
    (except === "example" || !filters.exampleOnly || item.hasExample)
  );
}

/** Distinct values, most frequent first. */
function facetValues(values: readonly string[]): string[] {
  const counts = new Map<string, number>();
  for (const value of values) {
    counts.set(value, (counts.get(value) ?? 0) + 1);
  }
  return [...counts.entries()]
    .sort(([a, countA], [b, countB]) => countB - countA || a.localeCompare(b))
    .map(([value]) => value);
}
