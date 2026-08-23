import type { FilterAxisDef, LearnContentType, ProductKey } from "@/src/data/learn/facets";
import { CONTENT_TYPE_OPTIONS, PRODUCT_OPTIONS } from "@/src/data/learn/facets";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { SearchIcon } from "@/src/icons/Search";

export type ContentTypeSelection = LearnContentType | "all";

// One active-pill recipe for every content-type facet (learn-harmonization.md
// section 2.3/D4): color no longer varies by type, matching the product-mix
// pills' existing accent treatment below.
const ACTIVE_TYPE_PILL_CLASSES = "border-cc-accent/40 bg-cc-accent/15 text-cc-accent";

interface LearnFacetBarProps {
  readonly contentType: ContentTypeSelection;
  readonly onContentTypeChange: (type: ContentTypeSelection) => void;
  readonly typeCount: (type: ContentTypeSelection) => number;
  readonly productSelection: readonly ProductKey[];
  readonly onToggleProduct: (key: ProductKey) => void;
  readonly productCount: (key: ProductKey) => number;
  /** Template-only axes (topology, use case, language, client, agent-ready); product mix has its own always-visible row. */
  readonly axes: readonly FilterAxisDef[];
  readonly axisSelection: Readonly<Record<string, readonly string[]>>;
  readonly onToggleAxis: (axis: FilterAxisDef, key: string) => void;
  readonly axisOptionCount: (axis: FilterAxisDef, key: string) => number;
  readonly query: string;
  readonly onQueryChange: (value: string) => void;
  readonly activeFilterCount: number;
  readonly onClearAll: () => void;
  /** Visible result count, rendered inline at the right end of the type-pill row (learn-harmonization.md D22). */
  readonly resultCount: number;
}

/**
 * The /learn filter bar: content-type pills and search on row 1, product mix
 * and (when Templates is active) a "More filters" disclosure with the
 * template-only axes on row 2.
 */
export function LearnFacetBar({
  contentType,
  onContentTypeChange,
  typeCount,
  productSelection,
  onToggleProduct,
  productCount,
  axes,
  axisSelection,
  onToggleAxis,
  axisOptionCount,
  query,
  onQueryChange,
  activeFilterCount,
  onClearAll,
  resultCount,
}: LearnFacetBarProps) {
  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap items-center gap-2.5">
          <TypePill
            active={contentType === "all"}
            label={`All (${typeCount("all")})`}
            onClick={() => onContentTypeChange("all")}
            activeClassName="border-cc-card-border-hover bg-cc-hover text-cc-heading"
          />
          {CONTENT_TYPE_OPTIONS.map((option) => (
            <TypePill
              key={option.key}
              active={contentType === option.key}
              label={`${option.label} (${typeCount(option.key)})`}
              onClick={() => onContentTypeChange(option.key)}
              activeClassName={ACTIVE_TYPE_PILL_CLASSES}
            />
          ))}
          <span className="text-cc-ink-dim ml-1 font-mono text-[0.6875rem] tracking-wider uppercase">
            {resultCount} {resultCount === 1 ? "result" : "results"}
          </span>
        </div>
        <div className="relative lg:w-72 lg:shrink-0">
          <SearchIcon className="text-cc-ink-dim pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 fill-current" />
          <input
            type="search"
            value={query}
            onChange={(event) => onQueryChange(event.target.value)}
            placeholder="Search the catalog…"
            aria-label="Search the learning catalog"
            className="border-cc-card-border bg-cc-surface/60 text-cc-heading placeholder:text-cc-ink-dim focus:border-cc-accent w-full rounded-lg border py-2.5 pr-3 pl-10 text-sm transition-colors outline-none"
          />
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2.5">
        {PRODUCT_OPTIONS.map((option) => {
          const active = productSelection.includes(option.key);
          const count = productCount(option.key);
          return (
            <button
              key={option.key}
              type="button"
              onClick={() => onToggleProduct(option.key)}
              aria-pressed={active}
              disabled={!active && count === 0}
              className={`group inline-flex cursor-pointer items-center gap-2 rounded-full border px-3 py-1.5 text-sm transition-colors disabled:cursor-default disabled:opacity-40 ${
                active
                  ? "border-cc-accent/40 bg-cc-accent/15 text-cc-accent"
                  : "border-cc-card-border text-cc-ink-dim hover:border-cc-accent"
              }`}
            >
              <span
                aria-hidden="true"
                className={`flex size-4 shrink-0 items-center justify-center rounded-[4px] border transition-colors ${
                  active
                    ? "bg-cc-accent border-cc-accent"
                    : "border-cc-card-border-hover group-hover:border-cc-accent/70"
                }`}
              >
                {active && <CheckGlyph className="text-cc-surface size-3" />}
              </span>
              {option.label}
              <span className="font-mono text-[0.6875rem] opacity-70">{count}</span>
            </button>
          );
        })}

        {contentType === "template" && (
          <details className="border-cc-card-border group rounded-lg border [&_summary]:cursor-pointer">
            <summary className="text-cc-heading flex items-center gap-2 px-3 py-1.5 text-sm font-medium select-none">
              More filters
            </summary>
            <div className="border-cc-card-border grid gap-6 border-t p-4 sm:grid-cols-2 lg:grid-cols-4">
              {axes.map((axis) => (
                <fieldset key={axis.key}>
                  <legend className="text-cc-ink-dim font-mono text-[0.6875rem] font-semibold tracking-[0.18em] uppercase">
                    {axis.label}
                  </legend>
                  <div className="mt-3 space-y-0.5">
                    {axis.options.map((option) => {
                      const active = axisSelection[axis.key]?.includes(option.key) ?? false;
                      const count = axisOptionCount(axis, option.key);
                      return (
                        <button
                          key={option.key}
                          type="button"
                          onClick={() => onToggleAxis(axis, option.key)}
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
                          <span className="text-cc-ink-dim font-mono text-[0.6875rem]">{count}</span>
                        </button>
                      );
                    })}
                  </div>
                </fieldset>
              ))}
            </div>
          </details>
        )}

        {activeFilterCount > 0 && (
          <button
            type="button"
            onClick={onClearAll}
            className="text-cc-accent hover:text-cc-accent-hover ml-auto cursor-pointer font-mono text-[0.6875rem] tracking-[0.15em] uppercase"
          >
            ✕ Clear all
          </button>
        )}
      </div>
    </div>
  );
}

function TypePill({
  active,
  label,
  onClick,
  activeClassName,
}: {
  readonly active: boolean;
  readonly label: string;
  readonly onClick: () => void;
  readonly activeClassName: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={`cursor-pointer rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors ${
        active ? activeClassName : "border-cc-card-border text-cc-ink-dim hover:border-cc-accent"
      }`}
    >
      {label}
    </button>
  );
}
