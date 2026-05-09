"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GridStat,
} from "@/components/redesign-system/grid";

// 4-stat strip (archetype C). Translates the existing PlatformTeamRoi data
// into a row of GridStat cards sharing a 1px hairline frame. Mirrors the
// `vercel-sol-marketing` y≈540 pattern: big number top-left, small label
// underneath, mono attribution caption above the number.

interface RoiItem {
  readonly value: string;
  readonly label: string;
  readonly attribution: string;
}

const ROI_ITEMS: readonly RoiItem[] = [
  {
    value: "47 → 1",
    label:
      "Hand-rolled BFFs consolidated into one Fusion mesh on a single rotation.",
    attribution: "TOP-5 EU RETAIL BANK",
  },
  {
    value: "9 wks",
    label:
      "End-to-end federation rollout for an 18-service FSI group, audit signed-off.",
    attribution: "NORTH AMERICAN FSI GROUP",
  },
  {
    value: "480 → 90",
    label:
      "P99 ms after replacing a stack of hand-rolled BFFs with one Fusion gateway.",
    attribution: "PLATFORM TEAM, RETAIL BANKING",
  },
  {
    value: "12",
    label:
      "Languages composed in one polyglot mesh, fully air-gapped on Nitro Self-Hosted.",
    attribution: "REGULATED LOGISTICS PLATFORM",
  },
];

const Note = styled.p`
  text-align: center;
  margin: 32px auto 0;
  max-width: 60ch;
  font-size: 13px;
  color: var(--cc-ink-dim);
  line-height: 1.6;
`;

export const EnterpriseGridRoi: FC = () => {
  return (
    <GridSection>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">Customer outcomes</span>
        <h2 className="cc-grid-h2">
          Real numbers from production platform teams.
        </h2>
        <p>
          We don't quote headline ROI percentages until we have a third-party
          study to back them up. Here's what we can publish now: the
          consolidation, the rollout time, and the latency move from teams
          running Fusion in production.
        </p>
      </div>
      <GridRow cols={4}>
        {ROI_ITEMS.map((item) => (
          <GridCard key={item.attribution}>
            <GridStat
              caption={item.attribution}
              value={item.value}
              label={item.label}
            />
          </GridCard>
        ))}
      </GridRow>
      <Note>
        Each metric is approved for publication by the customer. Names and
        industry segments are anonymised; the numbers are not.
      </Note>
    </GridSection>
  );
};
