"use client";

import React, { FC } from "react";

// Section 09: pricing teaser. One line of copy + a single CTA. We keep this
// section deliberately lean so the final CTA below has all the visual
// weight at the bottom of the page.

export const AgentsPricingTeaser: FC = () => {
  return (
    <section className="cc-ag-section cc-ag-feature">
      <div className="cc-section-label">
        <span className="num">09</span> Billing
      </div>
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
        <a href="/pricing" className="cc-btn cc-btn-ghost">
          See pricing →
        </a>
      </div>
    </section>
  );
};
