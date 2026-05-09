"use client";

import React from "react";
import styled from "styled-components";

// PriceTiers renders a minimal, sophisticated background for the cinematic
// /pricing variant. Two layers, both very low opacity, both pointer-events:
// none:
//
//   1. a sparse "price ladder" of 5-6 horizontal hairlines spaced unevenly
//      down the page, each tagged with a small monospace price label at the
//      right gutter ($0, $499, $1.5k, $5k, $10k, Custom);
//   2. one oversized outlined `$` glyph anchored to the right edge of the
//      viewport, vertically centered around the upper third of the page so
//      it reads on first paint. Stroke-only, accent-tinted, bleeds off the
//      right edge so only the left half is fully visible.

export interface PriceTiersProps {
  className?: string;
}

// Page is sized to comfortably cover the cinematic /pricing tree. The $ is
// positioned within the first 900px of vertical space so it shows above the
// fold on the first viewport.
const VIEW_WIDTH = 1440;
const VIEW_HEIGHT = 3000;

// Uneven spacing reads as a "price tier ladder" rather than a uniform grid.
// Each entry is a fraction of VIEW_HEIGHT.
const TIERS: { y: number; label: string }[] = [
  { y: 0.06, label: "$0" },
  { y: 0.18, label: "$499" },
  { y: 0.34, label: "$1.5k" },
  { y: 0.52, label: "$5k" },
  { y: 0.72, label: "$10k" },
  { y: 0.92, label: "Custom" },
];

const LABEL_RIGHT_PAD = 32;
const DOLLAR_FONT_SIZE = 820;
const DOLLAR_X = VIEW_WIDTH + 80; // bleeds off the right edge
const DOLLAR_Y = VIEW_HEIGHT * 0.32; // sits in the upper portion

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
`;

const FullSvg = styled.svg`
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  display: block;
`;

/**
 * Sparse price-ladder + ghost dollar background for the cinematic /pricing
 * variant. Decorative only and hidden from assistive tech.
 */
export const PriceTiers: React.FC<PriceTiersProps> = ({ className }) => {
  return (
    <Outer className={className} aria-hidden="true">
      <FullSvg
        xmlns="http://www.w3.org/2000/svg"
        viewBox={`0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`}
        preserveAspectRatio="xMidYMin slice"
      >
        {/* Ghost dollar sign, anchored to the right edge of the viewport. */}
        <text
          x={DOLLAR_X}
          y={DOLLAR_Y}
          textAnchor="end"
          dominantBaseline="middle"
          fontFamily='Georgia, "Times New Roman", serif'
          fontWeight={700}
          fontSize={DOLLAR_FONT_SIZE}
          fill="none"
          stroke="rgba(140, 160, 240, 0.06)"
          strokeWidth="1.5"
        >
          $
        </text>

        {/* Horizontal price-tier rules. */}
        {TIERS.map(({ y, label }) => {
          const yPx = y * VIEW_HEIGHT;
          return (
            <g key={label}>
              <line
                x1={0}
                x2={VIEW_WIDTH}
                y1={yPx}
                y2={yPx}
                stroke="rgba(245, 241, 234, 0.07)"
                strokeWidth="1"
              />
              <text
                x={VIEW_WIDTH - LABEL_RIGHT_PAD}
                y={yPx - 6}
                fontSize="10"
                fill="rgba(245, 241, 234, 0.10)"
                textAnchor="end"
                dominantBaseline="alphabetic"
                fontFamily="var(--cc-font-mono, ui-monospace, monospace)"
              >
                {label}
              </text>
            </g>
          );
        })}
      </FullSvg>
    </Outer>
  );
};
