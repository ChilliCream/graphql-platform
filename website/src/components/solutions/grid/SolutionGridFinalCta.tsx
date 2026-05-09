"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridButton,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type { SolutionFinalCta as SolutionFinalCtaData } from "@/data/solutions/types";

interface SolutionGridFinalCtaProps {
  readonly cta: SolutionFinalCtaData;
}

// Archetype L (final CTA). Per Grid spec anti-pattern: one primary
// button only, no third CTA. The secondary destination is reachable via a
// quiet inline text link below the primary so the CTA reads as a single
// call to action, not a tied vote. Tertiary is dropped here intentionally.
export const SolutionGridFinalCta: FC<SolutionGridFinalCtaProps> = ({
  cta,
}) => (
  <GridSection hairlineBottom>
    <Inner>
      <span className="cc-grid-eyebrow">Pick your way in</span>
      <h2 className="cc-grid-h2">{cta.headline}</h2>
      <p className="cc-grid-lede">{cta.sub}</p>
      <Buttons>
        <GridButton variant="primary" href={cta.primary.href}>
          {cta.primary.label}
        </GridButton>
        <SecondaryLink href={cta.secondary.href}>
          {cta.secondary.label} →
        </SecondaryLink>
      </Buttons>
    </Inner>
  </GridSection>
);

const Inner = styled.div`
  max-width: 720px;
  margin: 0 auto;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;

  .cc-grid-h2 {
    margin: 8px auto 6px;
    max-width: 24ch;
  }

  .cc-grid-lede {
    margin: 0 auto 28px;
    max-width: 56ch;
  }
`;

const Buttons = styled.div`
  display: inline-flex;
  align-items: center;
  gap: 24px;
  flex-wrap: wrap;
  justify-content: center;
`;

const SecondaryLink = styled.a`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  font-weight: 500;
  color: ${GRID_TOKENS.inkPrimary};
  text-decoration: none;
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  padding-bottom: 2px;
  transition: border-color 0.12s ease;

  &:hover,
  &:focus-visible {
    border-color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  }
`;
