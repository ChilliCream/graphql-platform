"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { SolutionFinalCta as SolutionFinalCtaData } from "@/data/solutions/types";

interface SolutionFinalCtaProps {
  readonly cta: SolutionFinalCtaData;
  readonly stepNumber: string;
}

// Section 10: the close. One primary button (visually dominant, full
// cream), one ghost-link secondary, and the tertiary demoted to a small
// inline text link below the buttons. Three equally-weighted buttons reads
// as a tie at the conversion moment, which is the worst outcome.
export const SolutionFinalCta: FC<SolutionFinalCtaProps> = ({
  cta,
  stepNumber,
}) => (
  <Band variant="glow" glowFrom="bottom-right" ariaLabel="Get started">
    <div className="cc-sl-section cc-sl-final">
      <div className="cc-section-label">
        <span className="num">{stepNumber}</span> Get started
      </div>
      <div className="cc-sl-final-inner">
        <div className="eyebrow">Pick your way in</div>
        <h2 className="display">{cta.headline}</h2>
        <p>{cta.sub}</p>
        <div className="cc-sl-final-buttons">
          <a href={cta.primary.href} className="cc-btn cc-btn-primary">
            {cta.primary.label} →
          </a>
          <a href={cta.secondary.href} className="cc-sl-final-link">
            {cta.secondary.label} →
          </a>
        </div>
        {cta.tertiary && (
          <a href={cta.tertiary.href} className="cc-sl-final-tertiary">
            {cta.tertiary.label} →
          </a>
        )}
      </div>
    </div>
  </Band>
);
