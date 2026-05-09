"use client";

import React from "react";
import styled from "styled-components";

// LedgerPaper renders a faint accountant's-ledger pattern as a full-page
// background for the cinematic /pricing variant. Three layers, all very low
// opacity, all pointer-events: none:
//
//   1. an inline SVG `<pattern>` of horizontal hairline rules every 30px,
//      so the eye reads "page after page of ruled accounting paper" as it
//      scrolls;
//   2. a single 1px vertical red margin rule pinned to the left gutter,
//      lifted from the spine of a real ledger book;
//   3. a column of mono line-numbers (010, 020, 030, ...) running down
//      just inside the margin rule, legible only to readers who actively
//      look for them.
//
// A faint warm-cream gradient overlay nudges the dark navy page bg toward
// "old paper" without losing the dark theme.

export interface LedgerPaperProps {
  className?: string;
}

const RULE_SPACING = 30;
const NUMBER_INTERVAL = 10; // every 10 rules => 010, 020, 030, ...
const TOTAL_NUMBERED_LINES = 240; // covers ~7200px of vertical scroll
const MARGIN_RULE_X = 64; // matches default --cc-pad-x lower bound
const NUMBER_X = 24; // sits in the gutter, inside the margin rule

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
`;

const RulesSvg = styled.svg`
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  display: block;
`;

const MarginRule = styled.div`
  position: absolute;
  top: 0;
  bottom: 0;
  left: ${MARGIN_RULE_X}px;
  width: 1px;
  background: rgba(220, 80, 80, 0.16);
`;

const NumbersSvg = styled.svg`
  position: absolute;
  top: 0;
  left: 0;
  width: ${MARGIN_RULE_X}px;
  height: 100%;
  display: block;
  overflow: visible;
  font-family: var(--cc-font-mono, ui-monospace, "SF Mono", Menlo, monospace);
`;

const WarmOverlay = styled.div`
  position: absolute;
  inset: 0;
  background: linear-gradient(
    180deg,
    rgba(245, 235, 210, 0.04) 0%,
    rgba(245, 235, 210, 0.02) 50%,
    rgba(245, 235, 210, 0.035) 100%
  );
  mix-blend-mode: overlay;
`;

/**
 * Faint ledger-paper background pattern for the cinematic /pricing variant.
 * Decorative only and hidden from assistive tech.
 */
export const LedgerPaper: React.FC<LedgerPaperProps> = ({ className }) => {
  const patternId = React.useId();

  // Pre-compute the line numbers so the JSX stays flat and readable.
  const numbers: { y: number; label: string }[] = [];
  for (let i = 1; i <= TOTAL_NUMBERED_LINES / NUMBER_INTERVAL; i++) {
    const lineIndex = i * NUMBER_INTERVAL;
    const y = lineIndex * RULE_SPACING;
    const label = String(lineIndex).padStart(3, "0");
    numbers.push({ y, label });
  }

  return (
    <Outer className={className} aria-hidden="true">
      <WarmOverlay />
      <RulesSvg xmlns="http://www.w3.org/2000/svg">
        <defs>
          <pattern
            id={patternId}
            width="100%"
            height={RULE_SPACING}
            patternUnits="userSpaceOnUse"
          >
            <line
              x1="0"
              y1={RULE_SPACING - 0.5}
              x2="100%"
              y2={RULE_SPACING - 0.5}
              stroke="rgba(245, 241, 234, 0.07)"
              strokeWidth="1"
            />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill={`url(#${patternId})`} />
      </RulesSvg>
      <MarginRule />
      <NumbersSvg xmlns="http://www.w3.org/2000/svg">
        {numbers.map(({ y, label }) => (
          <text
            key={label}
            x={NUMBER_X}
            y={y + 3}
            fontSize="9"
            fill="rgba(245, 241, 234, 0.10)"
            textAnchor="start"
            dominantBaseline="middle"
            fontFamily="var(--cc-font-mono, ui-monospace, monospace)"
          >
            {label}
          </text>
        ))}
      </NumbersSvg>
    </Outer>
  );
};
