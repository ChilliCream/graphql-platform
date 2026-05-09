"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";

import {
  nativeIntegrations,
  recentlyAdded,
} from "@/data/integrations/integrations";

import { IntegrationCard } from "./IntegrationCard";

// Section 06: full grid of every native integration. "Native" means
// owned and tested by ChilliCream, the first-party set. Cards render in the
// richer (non-dense) variant: 1px solid border, larger logo presence, full
// type badge in the footer. The grid hides itself when the active Type pill
// excludes Native, so the hero filter feels like a real toggle.
export const NativeIntegrationsGrid: FC = () => {
  const searchParams = useSearchParams();
  const type = searchParams?.get("type");
  const q = (searchParams?.get("q") ?? "").trim().toLowerCase();

  const visible = useMemo(() => {
    if (type === "community") {
      return [];
    }
    const recent = new Set(recentlyAdded(6).map((i) => i.slug));
    return nativeIntegrations().filter((i) => {
      if (type === "recent" && !recent.has(i.slug)) {
        return false;
      }
      if (q.length > 0) {
        const hay = (i.name + " " + i.tagline).toLowerCase();
        if (!hay.includes(q)) {
          return false;
        }
      }
      return true;
    });
  }, [type, q]);

  if (visible.length === 0) {
    return null;
  }

  return (
    <section className="cc-in-section cc-in-typesection">
      <div className="cc-section-label">
        <span className="num">06</span> Native
      </div>
      <div className="cc-in-typesection-inner">
        <div className="cc-in-typesection-head">
          <div>
            <span className="eyebrow">First-party</span>
            <h2 className="display">Native integrations.</h2>
            <p>
              Maintained by ChilliCream. Tested against every release, shipped
              as part of the platform.
            </p>
          </div>
          <span className="count-pill">
            {visible.length} {visible.length === 1 ? "package" : "packages"}
          </span>
        </div>
        <div className="cc-in-grid">
          {visible.map((integration) => (
            <IntegrationCard key={integration.slug} integration={integration} />
          ))}
        </div>
      </div>
    </section>
  );
};
