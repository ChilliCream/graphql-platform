"use client";

import React from "react";
import styled from "styled-components";

import {
  GridButton,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";

// Archetype L. Centered final ask. Two buttons, no card chrome: the section
// itself closes the page with a 1px top hairline shared with the FAQ above.
export const PricingGridFooterCta: React.FC = () => {
  return (
    <GridSection variant="default">
      <Wrap>
        <Eyebrow>Ready when you are</Eyebrow>
        <Headline>Start free. Scale when you need to.</Headline>
        <Lede>
          Every Nitro tier ships with hard limits, budget alerts, and the same
          OSS engine underneath. No lock-in, no surprise invoices.
        </Lede>
        <Actions>
          <GridButton variant="primary" href="https://nitro.chillicream.com">
            Start free
          </GridButton>
          <GridButton variant="secondary" href="https://chillicream.com/docs">
            Read the docs
          </GridButton>
        </Actions>
      </Wrap>
    </GridSection>
  );
};

const Wrap = styled.div`
  text-align: center;
  max-width: 720px;
  margin: 0 auto;
`;

const Eyebrow = styled.div`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
  margin-bottom: 18px;
`;

const Headline = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${GRID_TOKENS.h2Size};
  font-weight: 600;
  line-height: 1.05;
  letter-spacing: -0.03em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0 0 18px;
  text-wrap: balance;
`;

const Lede = styled.p`
  font-size: 16px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  max-width: 56ch;
  margin: 0 auto 32px;
  text-wrap: pretty;
`;

const Actions = styled.div`
  display: flex;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
`;
