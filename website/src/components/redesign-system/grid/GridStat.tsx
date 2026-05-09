"use client";

import React from "react";
import styled from "styled-components";

import { GRID_TOKENS } from "./tokens";

// Single stat tile for use inside a `GridRow` (typically the 4-stat strip
// archetype). Renders an optional monospace caption above a large display
// number, with a small label beneath. Sits inside a `GridCard` cell so the
// padding/border come from the card.

export interface GridStatProps {
  /** The display number, e.g. "37%", "12M", "100+". */
  value: string;
  /** Single-line description sitting below the value. */
  label: string;
  /** Optional uppercase monospace eyebrow above the number. */
  caption?: string;
  /** Optional accent color override for the number. */
  accent?: string;
  className?: string;
}

const Outer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  width: 100%;
`;

const Caption = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: ${GRID_TOKENS.eyebrowSize};
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-grid-ink-faint, ${GRID_TOKENS.inkFaint});
`;

interface ValueProps {
  $accent?: string;
}

const Value = styled.span<ValueProps>`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(40px, 5vw, 72px);
  font-weight: 600;
  letter-spacing: -0.04em;
  line-height: 1;
  color: ${({ $accent }) => $accent ?? GRID_TOKENS.inkPrimary};
  font-feature-settings: "tnum" 1;
`;

const Label = styled.span`
  font-size: 14px;
  line-height: 1.4;
  color: var(--cc-grid-ink-body, ${GRID_TOKENS.inkBody});
`;

/**
 * Single stat tile: optional monospace caption above a large number, small
 * label beneath. Place inside a `GridCard` cell within a `GridRow`.
 */
export const GridStat: React.FC<GridStatProps> = ({
  value,
  label,
  caption,
  accent,
  className,
}) => {
  return (
    <Outer className={className}>
      {caption ? <Caption>{caption}</Caption> : null}
      <Value $accent={accent}>{value}</Value>
      <Label>{label}</Label>
    </Outer>
  );
};
