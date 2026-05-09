"use client";

import React from "react";
import styled, { css } from "styled-components";

import { GRID_TOKENS } from "./tokens";

// Full-bleed section band for the Grid variant. Pages stack `GridSection`
// containers vertically; adjacent bands share their hairline border, which
// is what produces the Vercel-style continuous grid frame.
//
// Variants:
//   default  - page-base background, primary ink color
//   inverted - near-black surface, lifted text color (use 1-2x per page)

export type GridSectionVariant = "default" | "inverted";

export interface GridSectionProps {
  variant?: GridSectionVariant;
  /** Render a 1px top hairline. */
  hairlineTop?: boolean;
  /** Render a 1px bottom hairline. */
  hairlineBottom?: boolean;
  className?: string;
  children: React.ReactNode;
  as?: keyof JSX.IntrinsicElements;
  id?: string;
}

interface OuterProps {
  $variant: GridSectionVariant;
  $hairlineTop: boolean;
  $hairlineBottom: boolean;
}

const variantStyles = (variant: GridSectionVariant) => {
  switch (variant) {
    case "inverted":
      return css`
        background: ${GRID_TOKENS.bgInverted};
        color: #ffffff;

        h1,
        h2,
        h3,
        h4,
        h5,
        h6 {
          color: #ffffff;
        }
      `;
    case "default":
    default:
      return css`
        background: ${GRID_TOKENS.bgBase};
        color: ${GRID_TOKENS.inkPrimary};
      `;
  }
};

const Outer = styled.section<OuterProps>`
  position: relative;
  width: 100%;
  padding-top: clamp(96px, 14vw, 160px);
  padding-bottom: clamp(96px, 14vw, 160px);

  ${({ $variant }) => variantStyles($variant)}

  ${({ $hairlineTop }) =>
    $hairlineTop
      ? css`
          border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
        `
      : ""}

  ${({ $hairlineBottom }) =>
    $hairlineBottom
      ? css`
          border-bottom: 1px solid
            var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
        `
      : ""}
`;

const Inner = styled.div`
  max-width: ${GRID_TOKENS.pageMaxWidth};
  margin: 0 auto;
  padding: 0 var(--cc-pad-x, ${GRID_TOKENS.pageGutter});
`;

/**
 * Full-bleed band container for Grid variant pages. Sets the section
 * background, optional 1px hairline borders top and bottom, and constrains
 * inner content to the 1280px page frame.
 */
export const GridSection: React.FC<GridSectionProps> = ({
  variant = "default",
  hairlineTop = false,
  hairlineBottom = false,
  className,
  children,
  as,
  id,
}) => {
  return (
    <Outer
      as={as as React.ElementType | undefined}
      $variant={variant}
      $hairlineTop={hairlineTop}
      $hairlineBottom={hairlineBottom}
      className={className}
      id={id}
    >
      <Inner>{children}</Inner>
    </Outer>
  );
};
