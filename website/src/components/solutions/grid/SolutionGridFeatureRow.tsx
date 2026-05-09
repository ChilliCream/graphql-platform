"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import { FEATURE_CARDS } from "@/data/solutions/shared";
import type { FeatureCardId } from "@/data/solutions/types";

import { PillarIcon } from "../PillarIcon";

interface SolutionGridFeatureRowProps {
  readonly cards: readonly FeatureCardId[];
}

// Shared 6-feature row. Per Grid spec: a `<GridRow cols={6}>` of dense
// feature tiles, each cell rendered as a noPadding GridCard holding a
// stroke icon and a single-line label, no body. Demoted-foundations row,
// kept tight so it reads as platform reassurance, not the main act.
export const SolutionGridFeatureRow: FC<SolutionGridFeatureRowProps> = ({
  cards,
}) => {
  const resolved = cards
    .map((id) => FEATURE_CARDS[id])
    .filter((c): c is NonNullable<typeof c> => c !== undefined);

  if (resolved.length === 0) {
    return null;
  }

  return (
    <GridSection hairlineBottom>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">Foundations</span>
        <h2 className="cc-grid-h2">
          Every Fusion deployment ships with these foundations.
        </h2>
      </div>
      <GridRow cols={6}>
        {resolved.map((c) => (
          <GridCard key={c.id}>
            <Tile>
              <IconSlot aria-hidden>
                <PillarIcon kind={c.icon} size={20} />
              </IconSlot>
              <Label>{c.title}</Label>
            </Tile>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};

const Tile = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
  align-items: flex-start;
  min-height: 132px;
`;

const IconSlot = styled.div`
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
`;

const Label = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 13.5px;
  font-weight: 500;
  line-height: 1.35;
  color: ${GRID_TOKENS.inkPrimary};
  letter-spacing: -0.005em;
  text-wrap: balance;
`;
