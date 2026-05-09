"use client";

import React from "react";
import styled from "styled-components";

// Single-instance "echo" illustration scattered into a band's negative space.
// Strict usage rules:
//   * MAX 1 per band.
//   * NEVER on comparison-table or dataviz bands (the iconography competes
//     with the tabular surface).
//   * NEVER in a band gutter that already contains an `<ActLabel>`.
// The host band MUST set `position: relative` so the absolute placement
// resolves against the band's own box, not an ancestor.

export type ScatterVariant = "brewer-mini" | "orbit-mini" | "cup-tilt";

export interface ScatterIllustrationProps {
  /** Which icon to render. */
  variant: ScatterVariant;
  /** Position within the host band as `[x%, y%]` (0-100). */
  position: [number, number];
  /** Multiplier applied to the native ~120px size. Defaults to `0.5`. */
  scale?: number;
  /** Final opacity. Defaults to `0.6`. */
  opacity?: number;
  className?: string;
}

interface OuterProps {
  $x: number;
  $y: number;
  $scale: number;
  $opacity: number;
}

const Outer = styled.div<OuterProps>`
  position: absolute;
  left: ${({ $x }) => $x}%;
  top: ${({ $y }) => $y}%;
  transform: translate(-50%, -50%) scale(${({ $scale }) => $scale});
  transform-origin: center;
  opacity: ${({ $opacity }) => $opacity};
  pointer-events: none;
  width: 120px;
  height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;

  & > svg {
    width: 100%;
    height: 100%;
    display: block;
  }
`;

const STROKE = "var(--cc-ink-faint, rgba(245, 241, 234, 0.16))";

const BrewerMini: React.FC = () => (
  <svg viewBox="0 0 120 120" fill="none" aria-hidden="true">
    {/* Coffee dripper at top */}
    <path
      d="M 36 20 L 84 20 L 74 50 L 46 50 Z"
      stroke={STROKE}
      strokeWidth="1.5"
      strokeLinejoin="round"
    />
    <line
      x1="40"
      y1="28"
      x2="80"
      y2="28"
      stroke={STROKE}
      strokeWidth="1.5"
      strokeLinecap="round"
    />
    {/* Drip line */}
    <line
      x1="60"
      y1="50"
      x2="60"
      y2="74"
      stroke={STROKE}
      strokeWidth="1.5"
      strokeLinecap="round"
    />
    {/* Cup at bottom */}
    <path
      d="M 40 78 L 80 78 L 76 100 L 44 100 Z"
      stroke={STROKE}
      strokeWidth="1.5"
      strokeLinejoin="round"
    />
    <path
      d="M 80 84 Q 92 86 90 96 Q 88 102 80 100"
      stroke={STROKE}
      strokeWidth="1.5"
      strokeLinecap="round"
      fill="none"
    />
  </svg>
);

const OrbitMini: React.FC = () => (
  <svg viewBox="0 0 120 120" fill="none" aria-hidden="true">
    <ellipse
      cx="60"
      cy="60"
      rx="48"
      ry="18"
      stroke={STROKE}
      strokeWidth="1.5"
    />
    <ellipse
      cx="60"
      cy="60"
      rx="36"
      ry="14"
      stroke={STROKE}
      strokeWidth="1.5"
    />
    <ellipse cx="60" cy="60" rx="22" ry="9" stroke={STROKE} strokeWidth="1.5" />
    <circle cx="60" cy="60" r="3" fill={STROKE} />
  </svg>
);

const CupTilt: React.FC = () => (
  <svg viewBox="0 0 120 120" fill="none" aria-hidden="true">
    <g transform="rotate(-12 60 60)">
      <path
        d="M 36 40 L 84 40 L 78 92 L 42 92 Z"
        stroke={STROKE}
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M 84 50 Q 100 54 98 72 Q 96 84 82 84"
        stroke={STROKE}
        strokeWidth="1.5"
        strokeLinecap="round"
        fill="none"
      />
      <ellipse
        cx="60"
        cy="40"
        rx="24"
        ry="6"
        stroke={STROKE}
        strokeWidth="1.5"
        fill="none"
      />
    </g>
  </svg>
);

const VARIANT_GLYPHS: Record<ScatterVariant, React.FC> = {
  "brewer-mini": BrewerMini,
  "orbit-mini": OrbitMini,
  "cup-tilt": CupTilt,
};

/**
 * Decorative micro-illustration anchored at a percentage position inside its
 * host band. Purely ornamental and hidden from assistive tech.
 */
export const ScatterIllustration: React.FC<ScatterIllustrationProps> = ({
  variant,
  position,
  scale = 0.5,
  opacity = 0.6,
  className,
}) => {
  const Glyph = VARIANT_GLYPHS[variant];
  const [x, y] = position;
  return (
    <Outer
      $x={x}
      $y={y}
      $scale={scale}
      $opacity={opacity}
      className={className}
      aria-hidden="true"
    >
      <Glyph />
    </Outer>
  );
};
