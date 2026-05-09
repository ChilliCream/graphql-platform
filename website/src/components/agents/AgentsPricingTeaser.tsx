"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

// Section 09: pricing teaser. Narrow accent band (amber wash) with a single
// CTA. Lean on purpose so the final CTA below carries all the weight at the
// bottom of the page. The CTA picks up the accent on hover — amber as
// system signal: "the agent is paid here."

export const AgentsPricingTeaser: FC = () => {
  return (
    <Band variant="accent" ariaLabel="Pricing">
      <div className="cc-ag-pricing-inner">
        <div>
          <div className="cc-section-label cc-ag-pricing-label">
            <span className="num">09</span> Billing
          </div>
          <div className="eyebrow">Billing</div>
          <h2 className="display">Nitro is the paid surface.</h2>
          <p>
            Hot Chocolate, Mocha, Strawberry Shake, and Fusion are open source
            and feed it. The agent loop runs on the same plan you already pay
            for observability and federation.
          </p>
        </div>
        <a href="/pricing" className="cc-btn cc-btn-ghost cc-ag-pricing-cta">
          See pricing →
        </a>
      </div>
    </Band>
  );
};
