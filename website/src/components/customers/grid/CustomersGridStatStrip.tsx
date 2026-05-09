"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import { AGGREGATE_STATS, TRUST_AGGREGATE } from "@/data/customers/aggregates";

// 02 By the numbers (archetype C): 4-up stat strip. Each cell is a
// `GridCard` holding a `GridStat`-shaped tile with the attribution rendered
// underneath as the source of evidence. The strip is bordered top and
// bottom by the section's hairlines; cells share their interior borders
// through `GridRow`. No vibrant tiles: each cell is mono-on-card.
export const CustomersGridStatStrip: FC = () => {
  return (
    <GridSection hairlineBottom>
      <Heading>
        <Eyebrow>By the numbers</Eyebrow>
        <Trustline>{TRUST_AGGREGATE}</Trustline>
      </Heading>
      <GridRow cols={4}>
        {AGGREGATE_STATS.map((stat) => (
          <GridCard key={stat.key}>
            <Stat>
              <Caption>{stat.attribution}</Caption>
              <Value>{stat.value}</Value>
              <Label>{stat.label}</Label>
            </Stat>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};

const Heading = styled.div`
  margin: 0 0 clamp(36px, 4vw, 56px);
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-width: 760px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: ${GRID_TOKENS.eyebrowSize};
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const Trustline = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(20px, 2.2vw, 28px);
  font-weight: 500;
  letter-spacing: -0.015em;
  line-height: 1.3;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  text-wrap: pretty;
`;

const Stat = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  align-items: flex-start;
  min-height: 160px;
`;

const Caption = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const Value = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(32px, 3.6vw, 48px);
  font-weight: 600;
  letter-spacing: -0.03em;
  line-height: 1;
  color: ${GRID_TOKENS.inkPrimary};
  font-feature-settings: "tnum" 1;
`;

const Label = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  line-height: 1.45;
  color: ${GRID_TOKENS.inkBody};
  text-wrap: pretty;
`;
