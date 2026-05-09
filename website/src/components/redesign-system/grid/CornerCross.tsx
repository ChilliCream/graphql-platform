"use client";

import React from "react";
import styled, { css } from "styled-components";

import { GRID_TOKENS } from "./tokens";

// Small "+" cross-mark ornament sitting at a card or section corner. Used to
// reinforce the drafting-paper feel of the Grid variant. Wrap a relatively
// positioned container with four `CornerCross` elements (one per corner) so
// that adjacent crosses align across shared borders.

export type CornerCrossPosition =
  | "top-left"
  | "top-right"
  | "bottom-left"
  | "bottom-right";

export interface CornerCrossProps {
  position: CornerCrossPosition;
  /** Outer dimension of the SVG box, in px. Defaults to 12. */
  size?: number;
  /** Stroke color. Defaults to the strong hairline token. */
  color?: string;
  /** Distance from the corner in px. Negative values overhang the edge. */
  inset?: number;
  className?: string;
}

interface WrapperProps {
  $position: CornerCrossPosition;
  $size: number;
  $inset: number;
}

const positionStyles = (position: CornerCrossPosition, inset: number) => {
  switch (position) {
    case "top-left":
      return css`
        top: ${inset}px;
        left: ${inset}px;
      `;
    case "top-right":
      return css`
        top: ${inset}px;
        right: ${inset}px;
      `;
    case "bottom-left":
      return css`
        bottom: ${inset}px;
        left: ${inset}px;
      `;
    case "bottom-right":
      return css`
        bottom: ${inset}px;
        right: ${inset}px;
      `;
  }
};

const Wrapper = styled.span<WrapperProps>`
  position: absolute;
  display: inline-block;
  width: ${({ $size }) => `${$size}px`};
  height: ${({ $size }) => `${$size}px`};
  pointer-events: none;
  z-index: 1;

  ${({ $position, $inset }) => positionStyles($position, $inset)}
`;

/**
 * Absolute-positioned "+" corner ornament. Place inside a relatively
 * positioned container (typically a `GridCard` or `GridSection`); use four
 * instances to mark all four corners. Crosses overhang the corner by `inset`
 * pixels so adjacent crosses align across shared borders.
 */
export const CornerCross: React.FC<CornerCrossProps> = ({
  position,
  size = 12,
  color,
  inset = -6,
  className,
}) => {
  const stroke =
    color ?? `var(--cc-grid-hairline-strong, ${GRID_TOKENS.hairlineStrong})`;
  const half = size / 2;
  return (
    <Wrapper
      $position={position}
      $size={size}
      $inset={inset}
      className={className}
      aria-hidden="true"
    >
      <svg
        width={size}
        height={size}
        viewBox={`0 0 ${size} ${size}`}
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <line
          x1={half}
          y1={0}
          x2={half}
          y2={size}
          stroke={stroke}
          strokeWidth={1}
        />
        <line
          x1={0}
          y1={half}
          x2={size}
          y2={half}
          stroke={stroke}
          strokeWidth={1}
        />
      </svg>
    </Wrapper>
  );
};
