"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";

import { Band } from "@/components/redesign-system/Band";
import {
  communityIntegrations,
  recentlyAdded,
} from "@/data/integrations/integrations";

import { IntegrationCard } from "../IntegrationCard";

// Cinematic community grid: same content as CommunityIntegrationsGrid; the
// band carries the cinematic `cc-band` class so IntegrationsCinematicRoot can
// apply the extended top gutter. The in-section `.cc-section-label` is hidden
// by the cinematic root; the band is unlabelled in cinematic mode (the
// chapter sequence ends at the last category block).
export const IntegrationsCinematicCommunityGrid: FC = () => {
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
    <Band
      variant="tinted"
      className="cc-band cc-band-community cc-in-tinted-band"
      ariaLabel="Community integrations"
    >
      <div className="cc-in-typesection-inner">
        <div className="cc-in-typesection-head">
          <div>
            <span className="eyebrow">Built on the platform</span>
            <h2 className="display">Community integrations.</h2>
            <p>
              Open-source packages from the ecosystem. Maintained by the
              community and reviewed by us, listed once they ship a working
              release.
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
    </Band>
  );
};
