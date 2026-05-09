"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

// The hero anchors the page accent: H1 with a single accent gradient span,
// plus a typographic trust line directly under the buttons. The previous
// 5-tile monogram rack was visually heavy for an anonymised customer signal;
// a single line of attributed segments reads as proof, not decoration.

const TRUST_SEGMENTS: readonly string[] = [
  "EU retail bank",
  "Logistics PaaS",
  "FSI group",
  "Public-sector cloud",
  "Global insurer",
];

interface EnterpriseHeroProps {
  readonly onPrimaryClick: () => void;
}

export const EnterpriseHero: FC<EnterpriseHeroProps> = ({ onPrimaryClick }) => {
  return (
    <Band variant="default">
      <div className="cc-ent-hero-inner">
        <div className="cc-section-label">
          <span className="num">02</span> Enterprise
        </div>
        <div className="eyebrow">For platform teams</div>
        <h1 className="display">
          The GraphQL platform for{" "}
          <span className="accent">enterprise platform teams.</span>
        </h1>
        <p>
          Hot Chocolate, Fusion, and Nitro give your platform team one stack to
          compose every backend you have, in any language, on infrastructure you
          control. Self-hosted, air-gapped, agent-ready, and supported by the
          engineers who built it.
        </p>
        <div className="cc-cta-row">
          <button
            type="button"
            onClick={onPrimaryClick}
            className="cc-btn cc-btn-primary"
          >
            Get a Nitro demo →
          </button>
          <a href="/" className="cc-btn cc-btn-ghost">
            Explore the platform
          </a>
        </div>

        <p className="cc-ent-hero-trustline" aria-label="Customer segments">
          Trusted by{" "}
          {TRUST_SEGMENTS.map((segment, i) => (
            <React.Fragment key={segment}>
              {i > 0 && <span className="sep"> · </span>}
              <span className="seg">{segment}</span>
            </React.Fragment>
          ))}
        </p>
      </div>
    </Band>
  );
};
