"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  CornerCross,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";

// 06 Architect-call CTA (archetype L): centered headline, sub, and a
// strict 2-button row. No card chrome; the section's hairline foot
// closes off the band before the related-links footer. Corner crosses
// at all four section corners reinforce the "drafting paper" feel one
// last time on the page.
export const CustomersGridArchitectCta: FC = () => {
  return (
    <GridSection hairlineBottom>
      <Frame>
        <CornerCross position="top-left" />
        <CornerCross position="top-right" />
        <CornerCross position="bottom-left" />
        <CornerCross position="bottom-right" />
        <Inner>
          <Eyebrow>For platform architects</Eyebrow>
          <H2>Want this in your stack?</H2>
          <Sub>
            We can broker a private reference call with a customer in your
            sector, or hand you the technical white paper if you&apos;d rather
            read first.
          </Sub>
          <Buttons>
            <PrimaryBtn href="/contact/sales?interest=reference">
              Book a reference call
            </PrimaryBtn>
            <SecondaryBtn href="/whitepapers/fusion-architecture">
              Read the white paper
            </SecondaryBtn>
          </Buttons>
        </Inner>
      </Frame>
    </GridSection>
  );
};

const Frame = styled.div`
  position: relative;
  padding: clamp(40px, 6vw, 80px) 0;
`;

const Inner = styled.div`
  max-width: 760px;
  margin: 0 auto;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 18px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: ${GRID_TOKENS.eyebrowSize};
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const H2 = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(30px, 3.8vw, 50px);
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.05;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  max-width: 22ch;
  text-wrap: balance;
`;

const Sub = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(15px, 1.1vw, 17px);
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  max-width: 56ch;
  text-wrap: pretty;
`;

const Buttons = styled.div`
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  justify-content: center;
  margin-top: 8px;

  @media (max-width: 560px) {
    flex-direction: column;
    width: 100%;
    align-items: stretch;

    > a {
      width: 100%;
    }
  }
`;

const BaseBtn = styled.a`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 12px 22px;
  border-radius: 0;
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  font-weight: 500;
  line-height: 1;
  text-decoration: none;
  transition: background 0.12s ease, color 0.12s ease, border-color 0.12s ease;
`;

const PrimaryBtn = styled(BaseBtn)`
  background: #ffffff;
  color: ${GRID_TOKENS.bgBase};
  border: 1px solid #ffffff;

  &:hover,
  &:focus-visible {
    background: #e5e5e5;
    border-color: #e5e5e5;
  }
`;

const SecondaryBtn = styled(BaseBtn)`
  background: transparent;
  color: ${GRID_TOKENS.inkPrimary};
  border: 1px solid ${GRID_TOKENS.hairlineStrong};

  &:hover,
  &:focus-visible {
    background: rgba(245, 241, 234, 0.04);
  }
`;
