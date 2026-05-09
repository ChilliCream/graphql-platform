"use client";

import React from "react";
import styled from "styled-components";

import { GRID_TOKENS } from "./tokens";

// Asymmetric two-column split with a single 1px hairline running down the
// middle. Used for hero text + visual pairs and big asymmetric feature
// cards. Children must be exactly two; the first occupies the left column,
// the second the right.

export type GridSplitRatio = "50-50" | "66-33" | "33-66" | "60-40";

export interface GridSplitProps {
  ratio?: GridSplitRatio;
  className?: string;
  /** Exactly two children. */
  children: React.ReactNode;
}

interface OuterProps {
  $ratio: GridSplitRatio;
}

const ratioColumns = (ratio: GridSplitRatio): string => {
  switch (ratio) {
    case "66-33":
      return "2fr 1fr";
    case "33-66":
      return "1fr 2fr";
    case "60-40":
      return "3fr 2fr";
    case "50-50":
    default:
      return "1fr 1fr";
  }
};

const Outer = styled.div<OuterProps>`
  display: grid;
  grid-template-columns: ${({ $ratio }) => ratioColumns($ratio)};
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
`;

/**
 * Asymmetric two-column split sharing a single 1px center hairline. Use for
 * hero text + visual pairs and big asymmetric feature cards. Children must
 * be exactly two. Collapses to a single stacked column on mobile.
 */
export const GridSplit: React.FC<GridSplitProps> = ({
  ratio = "50-50",
  className,
  children,
}) => {
  return (
    <Outer $ratio={ratio} className={className}>
      {children}
    </Outer>
  );
};
