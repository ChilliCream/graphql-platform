"use client";

import React from "react";
import styled, { css } from "styled-components";

// Dotted-grid background for "directory" surfaces (logo walls, integration
// tile grids, federation diagrams). Renders an SVG `<pattern>` of dots at
// 16/24/32px spacing with an optional mask-image fade so the grid dissolves
// into the surrounding band. Position it as the first child of a `<Band>`
// or a wrapper that sits at `position: absolute; inset: 0; z-index: 0;`.

export type DottedGridDensity = "sm" | "md" | "lg";
export type DottedGridFade = "none" | "top" | "bottom" | "both";

export interface DottedGridBgProps {
  /** Grid spacing: `sm`=16px, `md`=24px, `lg`=32px. Defaults to `md`. */
  density?: DottedGridDensity;
  /** Dot color. Defaults to `var(--cc-ink-faint)`. */
  tone?: string;
  /** Mask gradient that fades the grid into the surrounding band. */
  fade?: DottedGridFade;
  className?: string;
}

const SPACING: Record<DottedGridDensity, number> = {
  sm: 16,
  md: 24,
  lg: 32,
};

const FADE_MASKS: Record<DottedGridFade, string> = {
  none: "none",
  top: "linear-gradient(to bottom, transparent 0%, #000 32%)",
  bottom: "linear-gradient(to top, transparent 0%, #000 32%)",
  both: "linear-gradient(to bottom, transparent 0%, #000 22%, #000 78%, transparent 100%)",
};

interface OuterProps {
  $fade: DottedGridFade;
}

const Outer = styled.div<OuterProps>`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;

  ${({ $fade }) =>
    $fade === "none"
      ? ""
      : css`
          -webkit-mask-image: ${FADE_MASKS[$fade]};
          mask-image: ${FADE_MASKS[$fade]};
        `}

  & > svg {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

/**
 * SVG pattern of dots used as a non-interactive background for directory
 * surfaces. The grid is purely decorative and is hidden from assistive tech.
 */
export const DottedGridBg: React.FC<DottedGridBgProps> = ({
  density = "md",
  tone = "var(--cc-ink-faint)",
  fade = "none",
  className,
}) => {
  const spacing = SPACING[density];
  const patternId = React.useId();

  return (
    <Outer $fade={fade} className={className} aria-hidden="true">
      <svg xmlns="http://www.w3.org/2000/svg">
        <defs>
          <pattern
            id={patternId}
            width={spacing}
            height={spacing}
            patternUnits="userSpaceOnUse"
          >
            <circle cx={1} cy={1} r={1} fill={tone} />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill={`url(#${patternId})`} />
      </svg>
    </Outer>
  );
};
