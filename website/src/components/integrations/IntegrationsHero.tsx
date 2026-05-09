"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { ChangeEvent, FC, useCallback, useMemo } from "react";

import { INTEGRATIONS } from "@/data/integrations/integrations";

// Type-pill values. "all" is the no-filter default; "recent" is the
// pseudo-type that surfaces the latest 6 additions. Native and Community map
// to integration.type. URL key is ?type=, search is ?q=.
const TYPE_PILLS = [
  { key: "all", label: "All" },
  { key: "native", label: "Native" },
  { key: "community", label: "Community" },
  { key: "recent", label: "Recently Added" },
] as const;

export type IntegrationTypePill = (typeof TYPE_PILLS)[number]["key"];

const SearchIcon: FC = () => (
  <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden>
    <circle cx="7" cy="7" r="5" stroke="currentColor" strokeWidth="1.4" />
    <line
      x1="11"
      y1="11"
      x2="14"
      y2="14"
      stroke="currentColor"
      strokeWidth="1.4"
      strokeLinecap="round"
    />
  </svg>
);

// Section 01: hero. H1 with a gradient on the noun "stack.", subhead, then
// the Type pill row + client-side search box. The pill row writes ?type= to
// the URL; the search writes ?q=. The grids elsewhere on the page read those
// values out of useSearchParams, so back/forward navigation is filter-aware.
export const IntegrationsHero: FC = () => {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const activeType = (searchParams?.get("type") ??
    "all") as IntegrationTypePill;
  const query = searchParams?.get("q") ?? "";

  const counts = useMemo(() => {
    const total = INTEGRATIONS.length;
    let native = 0;
    let community = 0;
    for (const i of INTEGRATIONS) {
      if (i.type === "native") {
        native++;
      } else {
        community++;
      }
    }
    return { all: total, native, community, recent: 6 };
  }, []);

  const writeParam = useCallback(
    (key: string, value: string | null): void => {
      const params = new URLSearchParams(searchParams?.toString() ?? "");
      if (value && value.length > 0) {
        params.set(key, value);
      } else {
        params.delete(key);
      }
      const qs = params.toString();
      router.replace(qs ? `${pathname}?${qs}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams]
  );

  const onPickType = useCallback(
    (key: IntegrationTypePill): void => {
      writeParam("type", key === "all" ? null : key);
    },
    [writeParam]
  );

  const onSearchChange = useCallback(
    (e: ChangeEvent<HTMLInputElement>): void => {
      writeParam("q", e.target.value);
    },
    [writeParam]
  );

  const onSearchClear = useCallback((): void => {
    writeParam("q", null);
  }, [writeParam]);

  return (
    <section className="cc-in-section cc-in-hero">
      <div className="cc-section-label">
        <span className="num">01</span> Integrations
      </div>
      <div className="cc-in-hero-inner">
        <div className="kicker">
          <span className="eyebrow">Compatibility, in one place</span>
        </div>
        <h1 className="display">
          Plug ChilliCream into the rest of your{" "}
          <span className="accent">stack.</span>
        </h1>
        <p>
          The API platform for humans and agents works with the auth,
          observability, messaging, data, and frontend tools you already run.
        </p>
        <div className="cc-in-hero-controls">
          <div
            className="cc-in-typepills"
            role="tablist"
            aria-label="Filter by type"
          >
            {TYPE_PILLS.map((pill) => {
              const isActive = activeType === pill.key;
              const count = counts[pill.key];
              return (
                <button
                  key={pill.key}
                  type="button"
                  role="tab"
                  aria-selected={isActive}
                  className={`cc-in-typepill${isActive ? " is-active" : ""}`}
                  onClick={() => onPickType(pill.key)}
                >
                  {pill.label}
                  <span className="count">{count}</span>
                </button>
              );
            })}
          </div>
          <label className="cc-in-search">
            <SearchIcon />
            <input
              type="search"
              value={query}
              onChange={onSearchChange}
              placeholder="Search integrations..."
              aria-label="Search integrations"
            />
            {query.length > 0 && (
              <button
                type="button"
                className="cc-in-search-clear"
                onClick={onSearchClear}
                aria-label="Clear search"
              >
                Clear
              </button>
            )}
          </label>
        </div>
      </div>
    </section>
  );
};
