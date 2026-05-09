"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";

import { INTEGRATIONS } from "@/data/integrations/integrations";

// Type-pill values mirror the default IntegrationsHero: "all" is the no-filter
// default; "recent" is the pseudo-type that surfaces the latest 6 additions;
// "native" and "community" map to integration.type. URL key is ?type=.
const TYPE_PILLS = [
  { key: "all", label: "All" },
  { key: "native", label: "Native" },
  { key: "community", label: "Community" },
  { key: "recent", label: "Recently Added" },
] as const;

type IntegrationTypePill = (typeof TYPE_PILLS)[number]["key"];

// Cinematic hero: same content and filter behaviour as the default
// IntegrationsHero, with the in-section `.cc-section-label` removed (the
// `<ActLabel>` is mounted at the band level by IntegrationsCinematic). The
// `?v=cinematic` flag is preserved when the type pill writes to the URL so
// filter clicks don't kick the reader back to the default variant.
export const IntegrationsCinematicHero: FC = () => {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const activeType = (searchParams?.get("type") ??
    "all") as IntegrationTypePill;

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

  return (
    <section className="cc-in-section cc-in-hero">
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
        </div>
      </div>
    </section>
  );
};
