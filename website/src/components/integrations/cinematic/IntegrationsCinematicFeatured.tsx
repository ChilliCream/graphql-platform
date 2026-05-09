"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";
import { featuredIntegrations } from "@/data/integrations/integrations";

import { IntegrationCard } from "../IntegrationCard";

// Cinematic featured: same content as FeaturedIntegrations; the band carries
// the cinematic `cc-band` class so IntegrationsCinematicRoot can apply the
// extended top gutter, and the in-section `.cc-section-label` is hidden
// (the `<ActLabel>` is mounted at the band level by IntegrationsCinematic).
export const IntegrationsCinematicFeatured: FC = () => {
  const featured = featuredIntegrations();
  if (featured.length === 0) {
    return null;
  }
  return (
    <Band
      variant="tinted"
      className="cc-band cc-band-featured cc-in-tinted-band"
      ariaLabel="Featured integrations"
    >
      <ActLabel n="03" name="Featured" />
      <div className="cc-in-featured-inner">
        <div className="cc-in-featured-head">
          <div>
            <span className="eyebrow">Featured</span>
            <h2 className="display">Curated by the marketplace team.</h2>
          </div>
        </div>
        <div className="cc-in-grid">
          {featured.map((integration) => (
            <IntegrationCard key={integration.slug} integration={integration} />
          ))}
        </div>
      </div>
    </Band>
  );
};
