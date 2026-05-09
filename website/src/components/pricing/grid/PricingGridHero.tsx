"use client";

import React from "react";
import styled from "styled-components";

import {
  GridButton,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";

// Archetype A. Centered text-only hero. Eyebrow takes the per-page accent
// color via the `--cc-accent` cascade variable; everything else stays in
// monochrome ink. Two buttons sit below the sub-headline.
export const PricingGridHero: React.FC = () => {
  return (
    <GridSection variant="default" hairlineTop hairlineBottom>
      <Wrap>
        <Eyebrow>Pricing</Eyebrow>
        <Headline>Pricing that scales with your team.</Headline>
        <SubHeadline>
          Open source, all the way up. Pay only for the parts you don&apos;t
          want to run.
        </SubHeadline>
        <Actions>
          <GridButton variant="primary" href="https://nitro.chillicream.com">
            Start free
          </GridButton>
          <GridButton
            variant="secondary"
            href="mailto:contact@chillicream.com?subject=Pricing"
          >
            Talk to sales
          </GridButton>
        </Actions>
      </Wrap>
    </GridSection>
  );
};

const Wrap = styled.div`
  text-align: center;
  max-width: 880px;
  margin: 0 auto;
  padding: clamp(48px, 8vw, 96px) 0;
`;

const Eyebrow = styled.div`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
  margin-bottom: 28px;
`;

const Headline = styled.h1`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${GRID_TOKENS.heroSize};
  font-weight: 600;
  line-height: 1.02;
  letter-spacing: -0.04em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0 0 24px;
  text-wrap: balance;
`;

const SubHeadline = styled.p`
  font-size: clamp(16px, 1.2vw, 19px);
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  max-width: 60ch;
  margin: 0 auto 40px;
  text-wrap: pretty;
`;

const Actions = styled.div`
  display: flex;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
`;
