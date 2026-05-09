"use client";

import React, { FC } from "react";

// Section 09: three-CTA finale. Self-serve / engineer / enterprise. Centered,
// with a one-line eyebrow above and a gradient-accented headline so it lands
// hard at the bottom of the page.

export const ObservabilityFinalCta: FC = () => {
  return (
    <section className="cc-obs-section cc-obs-final">
      <div className="cc-section-label">
        <span className="num">09</span> Get started
      </div>
      <div className="cc-obs-final-inner">
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
          <a href="/enterprise" className="cc-btn cc-btn-ghost">
            Nitro for enterprise
          </a>
        </div>
      </div>
    </section>
  );
};
