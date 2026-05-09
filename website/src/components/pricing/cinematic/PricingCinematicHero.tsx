"use client";

import React from "react";

import { PricingCalculator } from "../PricingCalculator";

// Cinematic hero: replaces the default variant's in-section `cc-section-label`
// with the proper `<ActLabel>` (mounted at the band level by PricingCinematic),
// so this component only renders the centered hero copy + calculator.
export const PricingCinematicHero: React.FC = () => {
  return (
    <div className="cc-pricing-hero">
      <div className="cc-pricing-hero-inner">
        <div className="eyebrow">Pricing</div>
        <h1 className="display">
          Pricing for <span className="accent">humans and agents.</span>
        </h1>
        <p>
          Open source, all the way up. Pay only for the parts you don't want to
          run.
        </p>

        <PricingCalculator />
      </div>
    </div>
  );
};
