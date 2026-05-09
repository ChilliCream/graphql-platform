"use client";

import React, { FC } from "react";

// Bottom-of-detail-page CTA. Same dual-CTA pattern as the index's
// architect-call band, but at story-page intensity (card, not gradient
// frame) so it doesn't compete with the editorial body above.
export const ReferenceCallCta: FC = () => {
  return (
    <section className="cc-csd-section cc-csd-cta">
      <div className="cc-csd-cta-inner">
        <div className="eyebrow">Want this in your stack?</div>
        <h2 className="display">Talk to a federation architect.</h2>
        <p>
          We can broker a private reference call with a customer in your sector,
          or hand you the docs if you'd rather start there.
        </p>
        <div className="cc-cta-row">
          <a
            href="https://chillicream.com/docs"
            className="cc-btn cc-btn-ghost"
          >
            Read the docs →
          </a>
          <a
            href="/contact/sales?interest=reference"
            className="cc-btn cc-btn-primary"
          >
            Talk to sales →
          </a>
        </div>
      </div>
    </section>
  );
};
