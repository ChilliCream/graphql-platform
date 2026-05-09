"use client";

import React, { FC } from "react";

// Section 07: architect-call CTA band. Dual CTA — top-of-funnel ghost link
// to the technical white paper and a primary "book a reference call" link
// that hands the lead off to sales with the right tag in the URL.
export const ArchitectCallCta: FC = () => {
  return (
    <section className="cc-cu-section cc-cu-architect">
      <div className="cc-section-label">
        <span className="num">07</span> Reference call
      </div>
      <div className="cc-cu-architect-inner">
        <div className="cc-cu-architect-card">
          <div className="eyebrow">For platform architects</div>
          <h2 className="display">Want this in your stack?</h2>
          <p>
            We can broker a private reference call with a customer in your
            sector — or hand you the technical white paper if you'd rather read
            first.
          </p>
          <div className="cc-cta-row">
            <a
              href="/whitepapers/fusion-architecture"
              className="cc-btn cc-btn-ghost"
            >
              Read the technical white paper →
            </a>
            <a
              href="/contact/sales?interest=reference"
              className="cc-btn cc-btn-primary"
            >
              Book a reference call →
            </a>
          </div>
        </div>
      </div>
    </section>
  );
};
