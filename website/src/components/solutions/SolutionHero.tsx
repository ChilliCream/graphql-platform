"use client";

import React, { FC } from "react";

import type { SolutionHero as SolutionHeroData } from "@/data/solutions/types";

interface SolutionHeroProps {
  readonly hero: SolutionHeroData;
}

// Section 01: text-only hero. Headline is the emotional compression, sub
// is the explainer. The optional accent fragment is rendered with the same
// drink-themed gradient (cool blue, violet, warm amber) used on every
// other dark page (customers, enterprise, pricing).
export const SolutionHero: FC<SolutionHeroProps> = ({ hero }) => {
  const { eyebrow, headline, headlineAccent, sub, primaryCta, secondaryCta } =
    hero;

  // Split the headline around the accent fragment so we can apply the
  // gradient class to just that span. If there is no accent or the accent
  // is not present in the headline, render the whole headline plain.
  let before = headline;
  let accent: string | null = null;
  let after = "";
  if (headlineAccent && headline.includes(headlineAccent)) {
    const idx = headline.indexOf(headlineAccent);
    before = headline.slice(0, idx);
    accent = headlineAccent;
    after = headline.slice(idx + headlineAccent.length);
  }

  return (
    <section className="cc-sl-section cc-sl-hero">
      <div className="cc-section-label">
        <span className="num">01</span> {eyebrow.split("/")[0].trim()}
      </div>
      <div className="cc-sl-hero-inner">
        <div className="eyebrow">{eyebrow}</div>
        <h1 className="display">
          {before}
          {accent && <span className="accent">{accent}</span>}
          {after}
        </h1>
        <p>{sub}</p>
        <div className="cc-cta-row">
          <a href={primaryCta.href} className="cc-btn cc-btn-primary">
            {primaryCta.label} →
          </a>
          <a href={secondaryCta.href} className="cc-btn cc-btn-ghost">
            {secondaryCta.label}
          </a>
        </div>
      </div>
    </section>
  );
};
