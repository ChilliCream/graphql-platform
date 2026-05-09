"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";
import type {
  HeroMotif as HeroMotifKind,
  SolutionHero as SolutionHeroData,
} from "@/data/solutions/types";

import { HeroMotif } from "../HeroMotif";

interface SolutionHeroCinematicProps {
  readonly hero: SolutionHeroData;
  readonly motif?: HeroMotifKind;
  readonly slug: string;
  readonly stepNumber: string;
  readonly chapterName: string;
}

// Cinematic variant of section 01 (Hero). Identical layout to the default
// SolutionHero, with the in-section `cc-section-label` replaced by an
// `<ActLabel>` mounted at the band gutter. The chapter name is the slug
// expressed as a homepage-style act marker (e.g. "01 POLYGLOT FEDERATION").
export const SolutionHeroCinematic: FC<SolutionHeroCinematicProps> = ({
  hero,
  motif,
  slug,
  stepNumber,
  chapterName,
}) => {
  const { eyebrow, headline, headlineAccent, sub, primaryCta, secondaryCta } =
    hero;

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
    <Band
      variant="glow"
      glowFrom="top-right"
      ariaLabel="Solution hero"
      className="cc-band"
    >
      <ActLabel n={stepNumber} name={chapterName} />
      <div className="cc-sl-section cc-sl-hero">
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
