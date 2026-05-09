"use client";

import React from "react";
import styled from "styled-components";

// ActivationLattice renders a quiet "sparse activation lattice" as the
// full-page background for the cinematic /products/nitro/agents variant.
// The metaphor: graph paper anchors at every grid intersection, with a
// handful of amber-lit nodes scattered across the page indicating where
// the agent is currently active. Two layers, both pointer-events: none:
//
//   1. an 80px dot grid covering the page, painted via a CSS
//      radial-gradient tile so the dots stay at true 80px spacing
//      regardless of viewport size. Each dot is a 2px circle in a
//      barely-visible parchment tone, like the anchor marks on graph
//      paper;
//   2. 3 to 5 amber-lit nodes positioned by absolute coordinates on a
//      1440x3000 base canvas. Each lit node is a 4-5px amber dot with a
//      soft glow halo (16px radial-gradient circle behind it). The
//      positions are deliberate: one near the hero, one near the
//      terminal mock, one near the Loop diagram, one near the demos
//      band, one near the final CTA.
//
// No connecting lines, no labels, no big patterns. The page should feel
// like a quiet board with a few lit cells.

export interface ActivationLatticeProps {
  className?: string;
}

interface LitNode {
  readonly cx: number;
  readonly cy: number;
}

// Lit node positions on a 1440x3000 base canvas. Chosen to feel
// deliberate: one per major section, never two in the same band.
const LIT_NODES: readonly LitNode[] = [
  { cx: 240, cy: 280 }, // hero left
  { cx: 1180, cy: 600 }, // upper right, near terminal
  { cx: 480, cy: 1200 }, // near Loop
  { cx: 960, cy: 1900 }, // near demos
  { cx: 300, cy: 2700 }, // near final CTA
];

const BASE_W = 1440;
const BASE_H = 3000;

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
  background-image: radial-gradient(
    circle at 1px 1px,
    rgba(245, 241, 234, 0.06) 1px,
    transparent 1.5px
  );
  background-size: 80px 80px;
  background-position: 0 0;
`;

const NodesSvg = styled.svg`
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: ${BASE_H}px;
  display: block;
`;

/**
 * Quiet sparse activation lattice background for the cinematic
 * /products/nitro/agents variant. Decorative only and hidden from
 * assistive tech.
 */
export const ActivationLattice: React.FC<ActivationLatticeProps> = ({
  className,
}) => {
  return (
    <Outer className={className} aria-hidden="true">
      <NodesSvg
        xmlns="http://www.w3.org/2000/svg"
        viewBox={`0 0 ${BASE_W} ${BASE_H}`}
        preserveAspectRatio="xMidYMin meet"
      >
        <defs>
          <radialGradient id="cc-activation-lattice-halo">
            <stop offset="0%" stopColor="rgba(247, 186, 100, 0.20)" />
            <stop offset="100%" stopColor="rgba(247, 186, 100, 0)" />
          </radialGradient>
        </defs>
        {LIT_NODES.map(({ cx, cy }, i) => (
          <g key={i}>
            <circle
              cx={cx}
              cy={cy}
              r={16}
              fill="url(#cc-activation-lattice-halo)"
            />
            <circle cx={cx} cy={cy} r={4.5} fill="rgba(247, 186, 100, 0.45)" />
          </g>
        ))}
      </NodesSvg>
    </Outer>
  );
};
