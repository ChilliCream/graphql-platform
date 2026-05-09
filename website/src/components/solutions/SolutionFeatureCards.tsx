"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { FEATURE_CARDS } from "@/data/solutions/shared";
import type { FeatureCardId } from "@/data/solutions/types";

import { PillarIcon } from "./PillarIcon";

interface SolutionFeatureCardsProps {
  readonly cards: readonly FeatureCardId[];
  readonly stepNumber: string;
}

// Section 07: the shared 6-feature foundation strip. Demoted from a
// 6-card grid to a dense icon-and-label row so this band reads as platform
// foundation reassurance, not as the main act. Each tile is just a small
// icon + 1-line label, monochrome, no card chrome. Lives on a tinted band
// so it sits visually below the solution-specific content above it.
export const SolutionFeatureCards: FC<SolutionFeatureCardsProps> = ({
  cards,
  stepNumber,
}) => {
  const resolved = cards
    .map((id) => FEATURE_CARDS[id])
    .filter((c): c is NonNullable<typeof c> => c !== undefined);

  return (
    <Band variant="tinted" ariaLabel="Foundations">
      <div className="cc-sl-tint-scope">
        <div className="cc-sl-section cc-sl-features">
          <div className="cc-section-label">
            <span className="num">{stepNumber}</span> Foundations
          </div>
          <div className="cc-sl-features-inner">
            <div className="cc-sl-features-head">
              <div className="eyebrow">Foundations</div>
              <h2 className="cc-sl-features-headline">
                Every Fusion deployment ships with these foundations.
              </h2>
            </div>
            <div className="cc-sl-features-row">
              {resolved.map((c) => (
                <div key={c.id} className="cc-sl-feature-chip">
                  <div className="cc-sl-feature-chip-icon">
                    <PillarIcon kind={c.icon} size={20} />
                  </div>
                  <span className="cc-sl-feature-chip-label">{c.title}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </Band>
  );
};
