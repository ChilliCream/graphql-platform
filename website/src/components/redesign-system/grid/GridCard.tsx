"use client";

import React from "react";
import styled, { css } from "styled-components";

import { GRID_TOKENS } from "./tokens";

// Square-cornered, hairline-bordered card. The atomic surface unit of the
// Grid variant. Cards never have rounded corners, drop shadows, or chrome
// gradients. Adjacency between cards is handled by `GridRow` / `GridStack`,
// which collapse adjacent borders to a shared 1px line.

export type GridCardVariant = "default" | "inverted";

export interface GridCardProps {
  variant?: GridCardVariant;
  /** Suppress the default internal padding. */
  noPadding?: boolean;
  className?: string;
  children: React.ReactNode;
  as?: keyof JSX.IntrinsicElements;
}

interface OuterProps {
  $variant: GridCardVariant;
  $noPadding: boolean;
}

const variantStyles = (variant: GridCardVariant) => {
  switch (variant) {
    case "inverted":
      return css`
        background: var(--cc-grid-card-bg-inverted, ${GRID_TOKENS.bgInverted});
        color: #ffffff;
      `;
    case "default":
    default:
      return css`
        background: var(--cc-grid-card-bg, ${GRID_TOKENS.bgCard});
        color: ${GRID_TOKENS.inkPrimary};
      `;
  }
};

const Outer = styled.div<OuterProps>`
  position: relative;
  display: block;
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-radius: 0;
  padding: ${({ $noPadding }) =>
    $noPadding
      ? "0"
      : `var(--cc-grid-card-padding, ${GRID_TOKENS.cardPadding})`};

  ${({ $variant }) => variantStyles($variant)}

  a&:hover,
  a&:focus-visible {
    background: var(--cc-grid-card-hover, ${GRID_TOKENS.bgHover});
    cursor: pointer;
  }
`;

/**
 * Hairline-bordered card with strict square corners. Use as the atomic
 * surface unit inside Grid variant pages. Compose multiples through
 * `GridRow`, `GridStack`, or `GridSplit` to share borders.
 */
export const GridCard: React.FC<GridCardProps> = ({
  variant = "default",
  noPadding = false,
  className,
  children,
  as,
}) => {
  return (
    <Outer
      as={as as React.ElementType | undefined}
      $variant={variant}
      $noPadding={noPadding}
      className={className}
    >
      {children}
    </Outer>
  );
};
