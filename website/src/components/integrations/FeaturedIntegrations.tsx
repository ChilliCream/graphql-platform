"use client";

import React, { FC } from "react";

import { featuredIntegrations } from "@/data/integrations/integrations";

import { IntegrationCard } from "./IntegrationCard";

// Section 04: hand-curated featured row. 3-4 cards drive the message of the
// quarter; flip the `featured` flag on an entry in integrations.ts to swap
// the lineup. Order matches the order in INTEGRATIONS, so editing the seed
// array is enough to reorder, no re-sort here.
export const FeaturedIntegrations: FC = () => {
  const featured = featuredIntegrations();
  if (featured.length === 0) {
    return null;
  }
  return (
    <section className="cc-in-section cc-in-featured">
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
    </section>
  );
};
