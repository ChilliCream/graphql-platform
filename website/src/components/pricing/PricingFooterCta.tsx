"use client";

import React from "react";

export const PricingFooterCta: React.FC = () => {
  return (
    <section className="cc-pricing-section cc-footer-cta">
      <div className="cc-footer-cta-inner">
        <div className="eyebrow">Ready when you are</div>
        <h2 className="display">Start free. Scale when you need to.</h2>
        <p>
          Every Nitro tier ships with hard limits, budget alerts, and the same
          OSS engine underneath. No lock-in, no surprise invoices.
        </p>
        <div className="cc-cta-row">
          <a
            href="https://chillicream.com/docs"
            className="cc-btn cc-btn-ghost"
          >
            Read the docs →
          </a>
          <a
            href="mailto:contact@chillicream.com?subject=Sales"
            className="cc-btn cc-btn-primary"
          >
            Talk to sales →
          </a>
        </div>
      </div>
    </section>
  );
};
