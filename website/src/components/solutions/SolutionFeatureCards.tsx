"use client";

import React, { FC } from "react";

import { FEATURE_CARDS } from "@/data/solutions/shared";
import type { FeatureCardId } from "@/data/solutions/types";

import { PillarIcon } from "./PillarIcon";

interface SolutionFeatureCardsProps {
  readonly cards: readonly FeatureCardId[];
  readonly stepNumber: string;
}

// Section 07: the shared 6-card grid. Same vocabulary across every solution
// page (performance, security, observability, dx, scale, openness). The
// pages reference the cards by id so a copy edit in shared.ts propagates
// across all seven pages.
export const SolutionFeatureCards: FC<SolutionFeatureCardsProps> = ({
  cards,
  stepNumber,
}) => {
  const resolved = cards
    .map((id) => FEATURE_CARDS[id])
    .filter((c): c is NonNullable<typeof c> => c !== undefined);

  return (
    <section className="cc-sl-section cc-sl-features">
      <div className="cc-section-label">
        <span className="num">{stepNumber}</span> Capabilities
      </div>
      <div className="cc-sl-features-inner">
        <div className="cc-sl-heading">
          <div className="eyebrow">Capabilities</div>
          <h2 className="display">
            Every page on the platform, the same six promises.
          </h2>
          <p>
            The capabilities below are guaranteed across every solution
            ChilliCream ships. Pick a use case, get the same operational posture
            underneath.
          </p>
        </div>
        <div className="cc-sl-features-grid">
          {resolved.map((c) => (
            <div key={c.id} className="cc-sl-feature-card">
              <div className="cc-sl-feature-icon">
                <PillarIcon kind={c.icon} size={20} />
              </div>
              <h3 className="cc-sl-feature-title display">{c.title}</h3>
              <p className="cc-sl-feature-body">{c.body}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
