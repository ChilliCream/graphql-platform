"use client";

import React from "react";

// Final ask. Distinct from the Enterprise band above: this one points the
// self-serve reader at the docs and the install line, not at sales. One
// primary action, one quiet text link. Lives on a glow Band so the page
// ends on a punctuation mark rather than another bordered card.
export const PricingFooterCta: React.FC = () => {
  return (
    <div className="cc-footer-cta">
      <div className="cc-footer-cta-inner">
        <div className="eyebrow">Ready when you are</div>
        <h2 className="display">Start free. Scale when you need to.</h2>
        <p>
          Every Nitro tier ships with hard limits, budget alerts, and the same
          OSS engine underneath. No lock-in, no surprise invoices.
        </p>
        <div
          className="cc-footer-install"
          aria-label="Install Hot Chocolate from NuGet"
        >
          <span className="prompt">$</span>
          <span className="cmd">dotnet add package </span>
          <span className="pkg">HotChocolate</span>
        </div>
        <div className="cc-cta-row">
          <a
            href="https://nitro.chillicream.com"
            className="cc-btn cc-btn-primary"
          >
            Start free →
          </a>
          <a
            href="https://chillicream.com/docs"
            className="cc-footer-text-link"
          >
            Read the docs →
          </a>
        </div>
      </div>
    </div>
  );
};
