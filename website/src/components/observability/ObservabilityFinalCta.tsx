"use client";

import React, { FC } from "react";

// Section 09: terminal CTA. Lives inside a glow Band on the redesigned page,
// so this component carries only its own typography and the button row, the
// surface chrome comes from the band. Two buttons + one quiet text link.

export const ObservabilityFinalCta: FC = () => {
  return (
    <div className="cc-obs-final-inner">
      <div className="cc-section-label cc-obs-final-label">
        <span className="num">09</span> Get started
      </div>
      <div className="eyebrow">Ready when you are</div>
      <h2 className="display">
        See the federation. <span className="accent">Quietly.</span>
      </h2>
      <div className="cc-cta-row">
        <a href="/pricing" className="cc-btn cc-btn-primary">
          Start free →
        </a>
        <a
          href="mailto:contact@chillicream.com?subject=Nitro%20observability"
          className="cc-btn cc-btn-ghost"
        >
          Talk to an engineer
        </a>
      </div>
      <a href="/enterprise" className="cc-obs-final-link">
        Nitro for enterprise →
      </a>
    </div>
  );
};
