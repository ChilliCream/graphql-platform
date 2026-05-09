"use client";

import React, { FC } from "react";

import type { Integration } from "@/data/integrations/integrations";

import { IntegrationCard } from "./IntegrationCard";

interface RelatedIntegrationsProps {
  readonly integrations: readonly Integration[];
}

// Bottom-of-detail-page rail. Same vocabulary as the index card so the page
// closes the loop back into the catalogue. Order is computed by
// findRelatedIntegrations() in the data module: same category first, then
// product overlap, then anything.
export const RelatedIntegrations: FC<RelatedIntegrationsProps> = ({
  integrations,
}) => {
  if (integrations.length === 0) {
    return null;
  }
  return (
    <section className="cc-ind-section cc-ind-related">
      <div className="cc-ind-related-inner">
        <div className="cc-ind-related-heading">
          <div className="eyebrow">More integrations</div>
          <h2 className="display">Pairs well with these.</h2>
        </div>
        <div className="cc-in-grid">
          {integrations.map((integration) => (
            <IntegrationCard key={integration.slug} integration={integration} />
          ))}
        </div>
      </div>
    </section>
  );
};
