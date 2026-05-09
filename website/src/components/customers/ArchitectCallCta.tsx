"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

// Section 06: architect-call CTA on a single accent-glow band. Dual CTA
// — top-of-funnel ghost link to the technical white paper and a primary
// "book a reference call" link that hands the lead off to sales with the
// right tag in the URL. Drops the previous bordered halo card; the
// page accent does the elevation.
export const ArchitectCallCta: FC = () => {
  return (
    <Band variant="glow" glowFrom="top-right" ariaLabel="Reference call">
      <div className="cc-section-label">
        <span className="num">06</span> Reference call
      </div>
      <div className="cc-cu-architect-inner">
        <div className="eyebrow">For platform architects</div>
        <h2 className="display">Want this in your stack?</h2>
        <p>
          We can broker a private reference call with a customer in your sector
          — or hand you the technical white paper if you'd rather read first.
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
    </Band>
  );
};
