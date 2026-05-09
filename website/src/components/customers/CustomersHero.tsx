"use client";

import React, { FC } from "react";

// Section 01: tagline hero. Restrained typography only. The accent on
// "break." picks up the same drink-themed gradient (cool blue → violet →
// warm amber) used by the pricing and enterprise heroes — the noun gets
// the colour, the verb stays cream.
export const CustomersHero: FC = () => {
  return (
    <section className="cc-cu-section cc-cu-hero">
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
          Federations that ship. Agents that connect. Humans that sleep. Real
          customer stories from the platform teams running Hot Chocolate,
          Fusion, and Nitro in production.
        </p>
        <div className="cc-cta-row">
          <a href="/contact/sales" className="cc-btn cc-btn-primary">
            Talk to our team →
          </a>
        </div>
      </div>
    </section>
  );
};
