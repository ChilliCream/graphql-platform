"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";

// Cinematic Section 10: final CTA. Mirrors `AgentsFinalCta` 1:1 in
// content and layout; the only chrome difference is the `<ActLabel>`
// chapter marker that sits in the band gutter at top:36px instead of
// the inline `.cc-section-label` (which is hidden by
// `AgentsCinematicRoot`).

export const AgentsFinalCtaCinematic: FC = () => {
  return (
    <Band variant="glow" glowFrom="bottom-right" ariaLabel="Ready to ship">
      <ActLabel n="10" name="Ready?" />
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
