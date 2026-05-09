"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

// Section 01: tagline hero on a default band. The accent on "break."
// picks up the per-page slate-warm gradient. The supporting paragraph
// has been replaced by a single attributable trust line per the
// validating-reader brief — the hero stops sounding like product
// marketing and earns the close immediately.
export const CustomersHero: FC = () => {
  return (
    <Band variant="default">
      <div className="cc-cu-hero">
        <div className="cc-section-label">
          <span className="num">01</span> Customers
        </div>
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
