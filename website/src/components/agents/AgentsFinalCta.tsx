"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

// Section 10: final CTA on a glow band. Two buttons (down from three) so
// the close has a clear primary + secondary, not a tie. The middle "Add MCP
// to Cursor" button has been demoted to the IDE chip strip in Section 08.

export const AgentsFinalCta: FC = () => {
  return (
    <Band variant="glow" glowFrom="bottom-right" ariaLabel="Ready to ship">
      <div className="cc-ag-final-inner">
        <div className="cc-section-label cc-ag-final-label">
          <span className="num">10</span> Ready?
        </div>
        <div className="eyebrow">Ready?</div>
        <h2 className="display">
          Stop being the <span className="accent">human glue.</span>
        </h2>
        <div className="cc-cta-row">
          <a href="/docs/nitro" className="cc-btn cc-btn-primary">
            Install Nitro CLI
          </a>
          <a
            href="mailto:contact@chillicream.com?subject=Nitro%20agents%20demo"
            className="cc-btn cc-btn-ghost"
          >
            Book a demo
          </a>
        </div>
      </div>
    </Band>
  );
};
