"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type { SolutionPillars } from "@/data/solutions/types";

import { PillarIcon } from "../PillarIcon";

interface SolutionGridPillarsProps {
  readonly pillars: SolutionPillars;
}

// Archetype D (3-up benefit row). Each pillar is a square-cornered
// GridCard with stroke icon, h3 title, and body. Adjacent cards share a
// single 1px hairline. Centered intro heading sits above the row.
export const SolutionGridPillars: FC<SolutionGridPillarsProps> = ({
  pillars,
}) => {
  const { headline, sub, items } = pillars;
  const cols = items.length === 4 ? 4 : 3;

  return (
    <GridSection hairlineBottom>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">What you get</span>
        <h2 className="cc-grid-h2">{headline}</h2>
        {sub && <p>{sub}</p>}
      </div>
      <GridRow cols={cols === 4 ? 4 : 3}>
        {items.map((p) => (
          <GridCard key={p.title} as="article">
            <Inner>
              <IconSlot aria-hidden>
                <PillarIcon kind={p.icon} size={24} />
              </IconSlot>
              <h3 className="cc-grid-h3">{p.title}</h3>
              <p className="cc-grid-body">{p.body}</p>
            </Inner>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};

const Inner = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 240px;
`;

const IconSlot = styled.div`
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  margin-bottom: 4px;
`;
