"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { SolutionPillars } from "@/data/solutions/types";

import { PillarIcon } from "./PillarIcon";

interface PillarsSectionProps {
  readonly pillars: SolutionPillars;
}

// Section 03: pillars are content-on-band, not cards. Each pillar is an
// icon-headline-body lockup separated by whitespace and a hairline rule on
// the left. The grid switches between 3 and 4 columns based on item count.
export const PillarsSection: FC<PillarsSectionProps> = ({ pillars }) => {
  const { headline, sub, items } = pillars;
  const gridClass =
    items.length <= 3
      ? "cc-sl-pillars-grid cc-sl-pillars-3"
      : "cc-sl-pillars-grid";

  return (
    <Band variant="default" ariaLabel="Pillars">
      <div className="cc-sl-section cc-sl-pillars">
        <div className="cc-section-label">
          <span className="num">03</span> Pillars
        </div>
        <div className="cc-sl-heading">
          <div className="eyebrow">What you get</div>
          <h2 className="display">{headline}</h2>
          {sub && <p>{sub}</p>}
        </div>
        <div className={gridClass}>
          {items.map((p) => (
            <div key={p.title} className="cc-sl-pillar">
              <div className="cc-sl-pillar-icon">
                <PillarIcon kind={p.icon} size={24} />
              </div>
              <h3 className="cc-sl-pillar-title display">{p.title}</h3>
              <p className="cc-sl-pillar-body">{p.body}</p>
            </div>
          ))}
        </div>
      </div>
    </Band>
  );
};
