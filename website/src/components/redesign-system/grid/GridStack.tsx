"use client";

import React from "react";
import styled from "styled-components";

import { GRID_TOKENS } from "./tokens";

// Vertical column of cards that share top-to-bottom borders. The mirror of
// `GridRow` for vertical stacks, used inside split layouts where one column
// holds multiple stacked cards aligned to a single shared frame.

export interface GridStackProps {
  className?: string;
  children: React.ReactNode;
}

const Outer = styled.div`
  display: grid;
  grid-template-columns: 1fr;
  gap: 0;
  width: 100%;
  border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-left: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-right: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  overflow: hidden;

  > * {
    border: 0;
    border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
    border-radius: 0;
  }
`;

/**
 * Vertical column of cards sharing 1px hairline borders top-to-bottom.
 * Use inside split layouts to align stacked cards to a single shared frame.
 */
export const GridStack: React.FC<GridStackProps> = ({
  className,
  children,
}) => {
  return <Outer className={className}>{children}</Outer>;
};
