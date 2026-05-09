"use client";

import React from "react";
import styled, { css } from "styled-components";

import { GRID_TOKENS } from "./tokens";

// N-up row of cards that share their adjacent borders. The signature
// Vercel "no double-border" effect is achieved by giving every cell a
// `border-right` and `border-bottom`, then framing the row container with
// a `border-top` and `border-left`. The container's `overflow: hidden`
// clips the trailing borders so the row reads as a single grid frame.
//
// Mobile (<720px) collapses to a single column with horizontal hairlines
// between rows.

export type GridRowCols = 2 | 3 | 4 | 6;

export interface GridRowProps {
  cols: GridRowCols;
  className?: string;
  children: React.ReactNode;
}

interface OuterProps {
  $cols: GridRowCols;
}

const Outer = styled.div<OuterProps>`
  display: grid;
  grid-template-columns: ${({ $cols }) => `repeat(${$cols}, 1fr)`};
  gap: 0;
  width: 100%;
  border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-left: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  overflow: hidden;

  > * {
    border: 0;
    border-right: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
    border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
    border-radius: 0;
  }

  @media (max-width: 720px) {
    grid-template-columns: 1fr;
    border-left: 0;

    > * {
      border-right: 0;
    }
  }

  ${({ $cols }) =>
    $cols >= 4
      ? css`
          @media (max-width: 1024px) and (min-width: 721px) {
            grid-template-columns: repeat(2, 1fr);
          }
        `
      : ""}
`;

/**
 * N-up row of cards sharing 1px hairline borders. Each child should be a
 * `GridCard` (or any element). Adjacent cards collapse their borders so the
 * row reads as a single grid frame. Collapses to a single column on mobile.
 */
export const GridRow: React.FC<GridRowProps> = ({
  cols,
  className,
  children,
}) => {
  return (
    <Outer $cols={cols} className={className}>
      {children}
    </Outer>
  );
};
