"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";

// Cinematic Section 09: pricing teaser. Mirrors `AgentsPricingTeaser`
// 1:1 in content and layout; the only chrome difference is the
// `<ActLabel>` chapter marker that sits in the band gutter at top:36px
// instead of the inline `.cc-section-label` (which is hidden by
// `AgentsCinematicRoot`).

export const AgentsPricingTeaserCinematic: FC = () => {
  return (
    <Band variant="accent" ariaLabel="Pricing">
      <ActLabel n="09" name="Billing" />
      <div className="cc-ag-pricing-inner">
        <div>
          <div className="eyebrow">Billing</div>
          <h2 className="display">Nitro is the paid surface.</h2>
          <p>
            Hot Chocolate, Mocha, Strawberry Shake, and Fusion are open source
            and feed it. The agent loop runs on the same plan you already pay
            for observability and federation.
          </p>
        </div>
        <a href="/pricing" className="cc-btn cc-btn-ghost cc-ag-pricing-cta">
          See pricing &rarr;
        </a>
      </div>
    </Band>
  );
};
