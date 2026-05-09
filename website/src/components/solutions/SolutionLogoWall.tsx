"use client";

import React, { FC } from "react";

import { LOGOS } from "@/data/solutions/shared";

interface SolutionLogoWallProps {
  readonly logos: readonly string[];
  readonly caption: string;
  readonly stepNumber: string;
}

// Section 09: the customer logo wall. Mixed grid, named brands sit next
// to anonymous tier-coded monograms. Same typographic-tile treatment as
// the customers page trust wall: the honesty of the mix is the point.
export const SolutionLogoWall: FC<SolutionLogoWallProps> = ({
  logos,
  caption,
  stepNumber,
}) => {
  const resolved = logos
    .map((id) => LOGOS[id])
    .filter((l): l is NonNullable<typeof l> => l !== undefined);

  return (
    <section className="cc-sl-section cc-sl-logos">
      <div className="cc-section-label">
        <span className="num">{stepNumber}</span> Adopters
      </div>
      <div className="cc-sl-logos-inner">
        <p className="cc-sl-logos-caption">{caption}</p>
        <div className="cc-sl-logos-grid">
          {resolved.map((l) => (
            <div
              key={l.id}
              className={`cc-sl-logo-tile${l.named ? " is-named" : ""}`}
            >
              <div className="cc-sl-logo-mono">{l.monogram}</div>
              <div className="cc-sl-logo-caption">{l.label}</div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
