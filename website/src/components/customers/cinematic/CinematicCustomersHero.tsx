"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";

// Section 01 (cinematic): same tagline hero as the default variant, but the
// gutter eyebrow is rendered through the shared `<ActLabel>` primitive so the
// cinematic variant chapters consistently with the homepage's `.cc-act-label`
// chrome (hairline numeral box + uppercase monospace name).
export const CinematicCustomersHero: FC = () => {
  return (
    <Band variant="default">
      <ActLabel n="01" name="Customers" />
      <div className="cc-cu-hero">
        <div className="cc-cu-hero-inner">
          <div className="eyebrow">Customers</div>
          <h1 className="display">
            Built by enterprises that{" "}
            <span className="accent">can't afford to break.</span>
          </h1>
          <p>
            27 banks, 14 insurers, 6 of the top 20 European retailers, and 3
            national rail operators run their public-facing graphs on this
            stack.
          </p>
          <div className="cc-cta-row">
            <a href="/contact/sales" className="cc-btn cc-btn-primary">
              Talk to our team →
            </a>
          </div>
        </div>
      </div>
    </Band>
  );
};
