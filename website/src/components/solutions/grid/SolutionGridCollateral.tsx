"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridButton,
  GridCard,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type { Collateral } from "@/data/solutions/types";

interface SolutionGridCollateralProps {
  readonly collateral: Collateral;
}

const KIND_EYEBROWS: Record<Collateral["kind"], string> = {
  playbook: "Playbook · Free download",
  starter: "Starter · Clone & deploy",
  workshop: "Workshop · 90-minute session",
};

// Single GridCard for the playbook offer. Title left, primary download
// button right; collapses to a stacked layout on mobile. Closes with the
// section's bottom hairline.
export const SolutionGridCollateral: FC<SolutionGridCollateralProps> = ({
  collateral,
}) => (
  <GridSection hairlineBottom>
    <Wrap>
      <GridCard>
        <Inner>
          <Copy>
            <Eyebrow>{KIND_EYEBROWS[collateral.kind]}</Eyebrow>
            <Title>{collateral.title}</Title>
          </Copy>
          <ButtonSlot>
            <GridButton variant="primary" href={collateral.href}>
              Download
            </GridButton>
          </ButtonSlot>
        </Inner>
      </GridCard>
    </Wrap>
  </GridSection>
);

const Wrap = styled.div`
  max-width: 1080px;
  margin: 0 auto;
`;

const Inner = styled.div`
  display: grid;
  grid-template-columns: 1fr auto;
  align-items: center;
  gap: 32px;

  @media (max-width: 720px) {
    grid-template-columns: 1fr;
    text-align: left;
  }
`;

const Copy = styled.div`
  display: flex;
  flex-direction: column;
  gap: 10px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const Title = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(22px, 2.4vw, 28px);
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.2;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  text-wrap: pretty;
`;

const ButtonSlot = styled.div`
  display: flex;
  align-items: center;
`;
