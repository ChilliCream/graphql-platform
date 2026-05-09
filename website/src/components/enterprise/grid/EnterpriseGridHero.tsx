"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridButton, GridSection } from "@/components/redesign-system/grid";

// Text-only hero (archetype A). Eyebrow + h1 + lede + 2 buttons, centered,
// closed by a 1px hairline at the bottom that doubles as the top border of
// the ROI strip below.

const TRUST_SEGMENTS: readonly string[] = [
  "EU retail bank",
  "Logistics PaaS",
  "FSI group",
  "Public-sector cloud",
  "Global insurer",
];

interface EnterpriseGridHeroProps {
  readonly onPrimaryClick: () => void;
}

const Inner = styled.div`
  max-width: 960px;
  margin: 0 auto;
  text-align: center;
`;

const CtaRow = styled.div`
  display: flex;
  gap: 12px;
  justify-content: center;
  flex-wrap: wrap;
  margin-top: 32px;
`;

const TrustLine = styled.p`
  margin: 48px auto 0;
  max-width: 720px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--cc-ink-dim);
  line-height: 1.7;
  text-wrap: pretty;

  .seg {
    color: var(--cc-ink);
  }
  .sep {
    color: var(--cc-ink-faint);
  }
`;

export const EnterpriseGridHero: FC<EnterpriseGridHeroProps> = ({
  onPrimaryClick,
}) => {
  return (
    <GridSection hairlineBottom>
      <Inner>
        <span className="cc-grid-eyebrow">For platform teams</span>
        <h1 className="cc-grid-h1">
          The GraphQL platform for enterprise platform teams.
        </h1>
        <p className="cc-grid-lede">
          Hot Chocolate, Fusion, and Nitro give your platform team one stack to
          compose every backend you have, in any language, on infrastructure you
          control. Self-hosted, air-gapped, agent-ready, and supported by the
          engineers who built it.
        </p>
        <CtaRow>
          <GridButton variant="primary" onClick={onPrimaryClick}>
            Get a Nitro demo
          </GridButton>
          <GridButton variant="secondary" href="/">
            Explore the platform
          </GridButton>
        </CtaRow>

        <TrustLine aria-label="Customer segments">
          Trusted by{" "}
          {TRUST_SEGMENTS.map((segment, i) => (
            <React.Fragment key={segment}>
              {i > 0 && <span className="sep"> · </span>}
              <span className="seg">{segment}</span>
            </React.Fragment>
          ))}
        </TrustLine>
      </Inner>
    </GridSection>
  );
};
