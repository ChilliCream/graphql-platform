"use client";

import React from "react";

export const PricingHero: React.FC = () => {
  return (
    <section className="cc-pricing-section cc-pricing-hero">
      <div className="cc-section-label">
        <span className="num">01</span> Pricing
      </div>
      <div className="cc-pricing-hero-inner">
        <div className="eyebrow">Pricing</div>
        <h1 className="display">
          Pricing for <span className="accent">humans and agents.</span>
        </h1>
        <p>
          Open source, all the way up. Pay only for the parts you don't want to
          run.
        </p>
      </div>
    </section>
  );
};
