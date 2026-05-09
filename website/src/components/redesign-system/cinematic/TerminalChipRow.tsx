"use client";

import React from "react";
import styled, { css } from "styled-components";

// Row of terminal-tab pills lifted from the homepage's `.cc-adapter-pill-d`
// (see `landing/desktop/DesktopLandingRoot.tsx` lines 476-501). The `prism`
// accent reproduces the conic-gradient border using the
// `background-image` + `background-clip: border-box` technique with a
// transparent padding-box so the gradient only paints the border ring.
// `ink` is the flat hairline used elsewhere on dark surfaces. `single`
// paints the border in one color, used for category-tinted chip rows.

export type TerminalChipAccent = "prism" | "ink" | "single";
export type TerminalChipAlign = "left" | "center";

export interface TerminalChipRowProps {
  /** The chip labels. Rendered uppercase by the component. */
  chips: string[];
  /** Border treatment. Defaults to `ink`. */
  accent?: TerminalChipAccent;
  /** Single-color border value. Required when `accent="single"`. */
  singleColor?: string;
  /** Horizontal alignment of the row. Defaults to `left`. */
  align?: TerminalChipAlign;
  className?: string;
}

interface OuterProps {
  $align: TerminalChipAlign;
}

const Outer = styled.div<OuterProps>`
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  ${({ $align }) =>
    $align === "center"
      ? css`
          justify-content: center;
        `
      : css`
          justify-content: flex-start;
        `}
`;

const baseChip = css`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 8px 14px;
  border-radius: 12px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-ink);
  font-weight: 600;
  white-space: nowrap;
`;

const prismChip = css`
  ${baseChip}
  border: 1.5px solid transparent;
  background-color: transparent;
  background-image: linear-gradient(#0c1322, #0c1322),
    linear-gradient(
      90deg,
      var(--cc-col-ord, oklch(0.76 0.16 150)),
      var(--cc-col-shi, oklch(0.74 0.14 220)),
      var(--cc-col-usr, oklch(0.72 0.18 310)),
      var(--cc-col-cat, oklch(0.74 0.18 30)),
      var(--cc-col-bil, oklch(0.82 0.16 90))
    );
  background-origin: padding-box, border-box;
  background-clip: padding-box, border-box;
  box-shadow: 0 12px 30px -16px rgba(0, 0, 0, 0.6);
`;

const inkChip = css`
  ${baseChip}
  border: 1px solid var(--cc-ink-faint);
  background: rgba(255, 255, 255, 0.02);
`;

interface ChipProps {
  $accent: TerminalChipAccent;
  $singleColor?: string;
}

const Chip = styled.span<ChipProps>`
  ${({ $accent, $singleColor }) => {
    if ($accent === "prism") {
      return prismChip;
    }
    if ($accent === "single") {
      return css`
        ${baseChip}
        border: 1px solid ${$singleColor ?? "var(--cc-ink-faint)"};
        background: rgba(255, 255, 255, 0.02);
      `;
    }
    return inkChip;
  }}
`;

/**
 * Horizontal row of terminal-tab chips with three border treatments. The
 * row is announced as a presentation group; chip text is the only content.
 */
export const TerminalChipRow: React.FC<TerminalChipRowProps> = ({
  chips,
  accent = "ink",
  singleColor,
  align = "left",
  className,
}) => {
  return (
    <Outer $align={align} className={className} role="presentation">
      {chips.map((chip, i) => (
        <Chip key={i} $accent={accent} $singleColor={singleColor}>
          {chip}
        </Chip>
      ))}
    </Outer>
  );
};
