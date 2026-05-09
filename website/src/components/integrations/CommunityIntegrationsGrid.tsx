"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";

import {
  communityIntegrations,
  recentlyAdded,
} from "@/data/integrations/integrations";

import { IntegrationCard } from "./IntegrationCard";

// Section 07: full grid of community integrations. Denser layout (4 columns
// at desktop instead of 3) with smaller cards, mirroring Vercel's "External
// Integrations" rail at the bottom of /marketplace. The visual hierarchy
// communicates the same thing the Type pill says: Native is heavier, denser,
// more present; Community is lighter, more utilitarian.
export const CommunityIntegrationsGrid: FC = () => {
  const searchParams = useSearchParams();
  const type = searchParams?.get("type");
  const q = (searchParams?.get("q") ?? "").trim().toLowerCase();

  const visible = useMemo(() => {
    if (type === "native") {
      return [];
    }
    const recent = new Set(recentlyAdded(6).map((i) => i.slug));
    return communityIntegrations().filter((i) => {
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
        <span className="num">07</span> Community
      </div>
      <div className="cc-in-typesection-inner">
        <div className="cc-in-typesection-head">
          <div>
            <span className="eyebrow">Built on the platform</span>
            <h2 className="display">Community integrations.</h2>
            <p>
              Open-source packages from the ecosystem. Maintained by the
              community, listed here once they ship a working release.
            </p>
          </div>
          <span className="count-pill">
            {visible.length} {visible.length === 1 ? "package" : "packages"}
          </span>
        </div>
        <div className="cc-in-grid is-dense">
          {visible.map((integration) => (
            <IntegrationCard
              key={integration.slug}
              integration={integration}
              dense
            />
          ))}
        </div>
      </div>
    </section>
  );
};
