"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  CornerCross,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";

// 01 Hero (archetype A): text-only band on the page surface. Per the spec,
// the hero band closes with a 1px hairline foot, the eyebrow takes the
// per-page accent thread, and the headline carries no gradient flourish in
// the Grid variant. Corner-cross ornaments at all four corners reinforce
// the drafting-paper feel.
export const CustomersGridHero: FC = () => {
  return (
    <GridSection hairlineTop hairlineBottom>
      <Frame>
        <CornerCross position="top-left" />
        <CornerCross position="top-right" />
        <CornerCross position="bottom-left" />
        <CornerCross position="bottom-right" />
        <Inner>
          <Eyebrow>Customers</Eyebrow>
          <Headline>
            Built by enterprises that can&apos;t afford to break.
          </Headline>
          <Sub>
            27 banks, 14 insurers, 6 of the top 20 European retailers, and 3
            national rail operators run their public-facing graphs on this
            stack.
          </Sub>
          <Buttons>
            <PrimaryBtn href="/contact/sales">Talk to our team</PrimaryBtn>
            <SecondaryBtn href="#stories">Read the stories</SecondaryBtn>
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
  max-width: 880px;
  margin: 0 auto;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 22px;
`;

const Eyebrow = styled.div`
  font-family: var(--cc-font-mono), monospace;
  font-size: ${GRID_TOKENS.eyebrowSize};
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const Headline = styled.h1`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(40px, 6vw, 72px);
  font-weight: 600;
  letter-spacing: -0.025em;
  line-height: 1.04;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  max-width: 22ch;
  text-wrap: balance;
`;

const Sub = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(15px, 1.15vw, 18px);
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  max-width: 60ch;
  margin: 0;
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
