"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type { SolutionRecord } from "@/data/solutions/types";

interface SolutionGridRelatedProps {
  readonly solutions: readonly SolutionRecord[];
}

// `<GridRow cols={3}>` of cross-link cards. Each card is an anchor wrapping
// the GridCard so the whole tile is the hit target; on hover the GridCard's
// own background lift kicks in.
export const SolutionGridRelated: FC<SolutionGridRelatedProps> = ({
  solutions,
}) => {
  if (solutions.length === 0) {
    return null;
  }

  return (
    <GridSection hairlineBottom>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">More solutions</span>
        <h2 className="cc-grid-h2">Where this leads next.</h2>
      </div>
      <GridRow cols={3}>
        {solutions.map((s) => (
          <GridCard
            key={s.slug}
            as="a"
            {...({
              href: `/solutions/${s.slug}/?v=grid`,
            } as React.AnchorHTMLAttributes<HTMLAnchorElement>)}
          >
            <Inner>
              <Eyebrow>
                {s.category === "industry" ? "Industry" : "Use case"}
              </Eyebrow>
              <h3 className="cc-grid-h3">{s.title}</h3>
              <Body>{s.hero.sub}</Body>
              <Arrow aria-hidden>Read more →</Arrow>
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
  gap: 10px;
  min-height: 220px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const Body = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0 0 16px;
  flex: 1;
  text-wrap: pretty;
`;

const Arrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  margin-top: auto;
`;
