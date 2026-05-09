"use client";

import React, { FC } from "react";

import type { SolutionFinalCta as SolutionFinalCtaData } from "@/data/solutions/types";

interface SolutionFinalCtaProps {
  readonly cta: SolutionFinalCtaData;
  readonly stepNumber: string;
}

// Section 10: the three-button CTA stack. PLG self-serve, sales-led, then
// the commercial product. ChilliCream sells into all three motions; the
// stack lets the visitor self-segment. Industry pages still ship three
// buttons but the primary points at sales, not docs.
export const SolutionFinalCta: FC<SolutionFinalCtaProps> = ({
  cta,
  stepNumber,
}) => (
  <section className="cc-sl-section cc-sl-final">
    <div className="cc-section-label">
      <span className="num">{stepNumber}</span> Get started
    </div>
    <div className="cc-sl-final-inner">
      <div className="eyebrow">Three ways in</div>
      <h2 className="display">{cta.headline}</h2>
      <p>{cta.sub}</p>
      <div className="cc-sl-final-buttons">
        <a href={cta.primary.href} className="cc-btn cc-btn-primary">
          {cta.primary.label} →
        </a>
        <a href={cta.secondary.href} className="cc-btn cc-btn-ghost">
          {cta.secondary.label}
        </a>
        {cta.tertiary && (
          <a href={cta.tertiary.href} className="cc-btn cc-btn-ghost">
            {cta.tertiary.label}
          </a>
        )}
      </div>
    </div>
  </section>
);
