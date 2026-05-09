"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";

import { Band } from "@/components/redesign-system/Band";
import { DottedGridBg } from "@/components/redesign-system/cinematic";
import {
  nativeIntegrations,
  recentlyAdded,
} from "@/data/integrations/integrations";

import { IntegrationCard } from "../IntegrationCard";

// Cinematic native grid: same content as NativeIntegrationsGrid, with a faint
// `<DottedGridBg density="sm" fade="both" />` painted under the tile wall so
// the directory reads as a topology surface rather than a flat box of cards.
// The fade is `both` so the dots dissolve into the band on top and bottom,
// matching the way `.cc-in-typesection` is sandwiched between the by-category
// stack and the community grid.
//
// The in-section `.cc-section-label` is hidden by IntegrationsCinematicRoot;
// the band itself is unlabelled in cinematic mode (the chapter sequence ends
// at the last category block, the Native/Community/Starters/DualCta tail
// reads as the conversion outro).
export const IntegrationsCinematicNativeGrid: FC = () => {
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
    <Band
      variant="default"
      className="cc-band cc-band-native"
      ariaLabel="Native integrations"
    >
      <DottedGridBg density="sm" fade="both" />
      <div className="cc-in-typesection-inner">
        <div className="cc-in-typesection-head">
          <div>
            <span className="eyebrow">First-party</span>
            <h2 className="display">Native integrations.</h2>
            <p>
              Built and supported by ChilliCream. Tested against every release,
              shipped as part of the platform, real partner color on every tile.
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
    </Band>
  );
};
