"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { HeroMotif as HeroMotifKind } from "@/data/solutions/types";
import type { SolutionHero as SolutionHeroData } from "@/data/solutions/types";

import { HeroMotif } from "./HeroMotif";

interface SolutionHeroProps {
  readonly hero: SolutionHeroData;
  readonly motif?: HeroMotifKind;
  readonly slug: string;
}

// Section 01: full-bleed glow band, copy left (60%), motif right (40%). The
// motif is decorative and tints with the per-slug accent thread; copy is
// the load-bearing element. The headline accent span keeps its gradient so
// the page accent threads through hero, motif, proof, diagram, CTA.
export const SolutionHero: FC<SolutionHeroProps> = ({ hero, motif, slug }) => {
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
    <Band variant="glow" glowFrom="top-right" ariaLabel="Solution hero">
      <div className="cc-sl-section cc-sl-hero">
        <div className="cc-section-label">
          <span className="num">01</span> {eyebrow.split("/")[0].trim()}
        </div>
        <div className="cc-sl-hero-grid">
          <div className="cc-sl-hero-copy">
            <div className="eyebrow">{eyebrow}</div>
            <h1 className="display">
              {before}
              {accent && <span className="accent">{accent}</span>}
              {after}
            </h1>
            <p>{sub}</p>
            <div className="cc-cta-row cc-cta-row--left">
              <a href={primaryCta.href} className="cc-btn cc-btn-primary">
                {primaryCta.label} →
              </a>
              <a href={secondaryCta.href} className="cc-btn cc-btn-ghost">
                {secondaryCta.label}
              </a>
            </div>
          </div>
          {motif ? (
            <div className="cc-sl-hero-motif" aria-hidden>
              <HeroMotif kind={motif} slug={slug} />
            </div>
          ) : null}
        </div>
      </div>
    </Band>
  );
};
