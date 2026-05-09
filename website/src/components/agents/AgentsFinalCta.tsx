"use client";

import React, { FC } from "react";

// Section 10: final CTA. Three buttons in one row. Centered, gradient-accent
// headline so it lands hard at the bottom of the page (mirrors the other
// /products/nitro/observability and /enterprise final CTAs).

export const AgentsFinalCta: FC = () => {
  return (
    <section className="cc-ag-section cc-ag-final">
      <div className="cc-section-label">
        <span className="num">10</span> Ready?
      </div>
      <div className="cc-ag-final-inner">
        <div className="eyebrow">Ready?</div>
        <h2 className="display">
          Stop being the <span className="accent">human glue.</span>
        </h2>
        <div className="cc-cta-row">
          <a href="/docs/nitro" className="cc-btn cc-btn-primary">
            Install Nitro CLI
          </a>
          <a
            href="/products/nitro/agents/cursor"
            className="cc-btn cc-btn-ghost"
          >
            Add MCP to Cursor
          </a>
          <a
            href="mailto:contact@chillicream.com?subject=Nitro%20agents%20demo"
            className="cc-btn cc-btn-ghost"
          >
            Book a demo
          </a>
        </div>
      </div>
    </section>
  );
};
