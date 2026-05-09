"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import { LOGOS } from "@/data/solutions/shared";

interface SolutionGridLogoWallProps {
  readonly logos: readonly string[];
  readonly caption: string;
}

// Archetype H (logo wall). Per Grid spec: `<GridRow cols={4}>` of
// typographic descriptor lockups (no monograms). Named brands render as
// wordmark; anonymous tier-coded customers render their structured
// industry/scale/region descriptor. Caption sits centered above the grid.
const ANONYMOUS_DESCRIPTORS: Record<string, string> = {
  euTier1Bank: "TIER-1 BANK · DACH · 18M ACCOUNTS",
  top3EuInsurer: "TOP-3 INSURER · EU · 11 MARKETS",
  naHealthNetwork: "HEALTH NETWORK · NA · 47 SYSTEMS",
  logisticsPaaS: "LOGISTICS PAAS · GLOBAL · 12 LANGUAGES",
  fsiGroup: "FSI GROUP · NA · 9 WK ROLLOUT",
  iberianRetailBank: "RETAIL BANK · IBERIA · 100% IN-VPC",
  dachReinsurer: "REINSURER · DACH · GDPR-FIRST",
  nordicTelco: "TELCO · NORDIC · 1.4M SUBSCRIBERS",
  ukChallengerBank: "CHALLENGER BANK · UK · MOBILE-FIRST",
  globalCardNetwork: "CARD NETWORK · GLOBAL · 120K REQ/S",
};

export const SolutionGridLogoWall: FC<SolutionGridLogoWallProps> = ({
  logos,
  caption,
}) => {
  const resolved = logos
    .map((id) => LOGOS[id])
    .filter((l): l is NonNullable<typeof l> => l !== undefined);

  if (resolved.length === 0) {
    return null;
  }

  return (
    <GridSection hairlineBottom>
      <Caption>{caption}</Caption>
      <GridRow cols={4}>
        {resolved.map((l) => (
          <GridCard key={l.id}>
            <Cell>
              {l.named ? (
                <Wordmark>{l.label}</Wordmark>
              ) : (
                <Descriptor>
                  {ANONYMOUS_DESCRIPTORS[l.id] ?? l.label.toUpperCase()}
                </Descriptor>
              )}
            </Cell>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};

const Caption = styled.p`
  text-align: center;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
  margin: 0 0 40px;
`;

const Cell = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  min-height: 96px;
  width: 100%;
`;

const Wordmark = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 18px;
  font-weight: 600;
  letter-spacing: -0.02em;
  color: ${GRID_TOKENS.inkPrimary};
`;

const Descriptor = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10.5px;
  letter-spacing: 0.14em;
  color: ${GRID_TOKENS.inkBody};
  line-height: 1.45;
  text-wrap: balance;
`;
