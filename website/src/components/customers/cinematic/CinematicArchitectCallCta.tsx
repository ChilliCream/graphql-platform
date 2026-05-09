"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";

// Section 05 (cinematic): same architect-call CTA as the default variant on
// the accent-glow band, with the gutter eyebrow upgraded to the shared
// `<ActLabel>`. Closes the cinematic chapter run.
export const CinematicArchitectCallCta: FC = () => {
  return (
    <Band variant="glow" glowFrom="top-right" ariaLabel="Reference call">
      <ActLabel n="05" name="Research call" />
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
