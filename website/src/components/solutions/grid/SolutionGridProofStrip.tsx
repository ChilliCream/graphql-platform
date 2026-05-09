"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GridStat,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type { ProofMetric } from "@/data/solutions/types";

interface SolutionGridProofStripProps {
  readonly metrics: readonly ProofMetric[];
}

// Archetype C (4-stat strip). Four flush stat cells share a 1px hairline
// frame; each cell is a GridStat (oversized number + outcome label) with
// the customer attribution as a small monospace caption above.
export const SolutionGridProofStrip: FC<SolutionGridProofStripProps> = ({
  metrics,
}) => {
  if (metrics.length === 0) {
    return null;
  }

  return (
    <GridSection hairlineBottom>
      <GridRow cols={4}>
        {metrics.map((m, i) => (
          <GridCard key={`${m.value}-${i}`}>
            <Cell>
              <GridStat
                value={m.value}
                label={m.outcome}
                caption={m.customer.toUpperCase()}
              />
            </Cell>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};

const Cell = styled.div`
  min-height: 200px;
  display: flex;
  align-items: flex-start;
  width: 100%;
  color: ${GRID_TOKENS.inkPrimary};
`;
