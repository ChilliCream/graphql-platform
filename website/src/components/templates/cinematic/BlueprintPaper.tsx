"use client";

import React from "react";
import styled from "styled-components";

// Blueprint-paper background for the /templates cinematic variant. Evokes
// architect's drafting paper to underline the framing "templates are
// engineering blueprints to copy". Pure inline SVG, low opacity, ambient.
//
// Composition (back-to-front):
//   1. A faint cyan-blue grid: a major grid every 40px (1px) layered over a
//      minor grid every 8px (0.5px). Tiled via a single `<pattern>` so it
//      scales with the page.
//   2. A double-line border frame at a 16px margin (1px outer, 0.5px inner,
//      4px gap), the standard blueprint sheet border.
//   3. Diagonal reference ticks in the top-left, two short 45 degree dashed
//      lines suggesting an orientation reference.
//   4. Dimensioning marks scattered across the canvas, monospace 9px labels
//      with arrow-tick lines pointing between gridlines, like leftover marks
//      from a drafting session.
//   5. A multi-row title block in the bottom-right corner, the signature
//      blueprint move (PROJECT, SCALE, DATE, REV, DRAWN BY).
//
// Positioned absolute inset 0 z-index 0 pointer-events none aria-hidden, so
// the parent root's normal flow content paints on top untouched.

export interface BlueprintPaperProps {
  className?: string;
}

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;

  & > svg.cc-bp-grid {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

// The title block sits at the bottom-right of the surface, anchored by the
// outer wrapper so it scrolls with the content rather than staying glued to
// the viewport. Positioned absolute on top of the tiled grid.
const TitleBlock = styled.div`
  position: absolute;
  bottom: 32px;
  right: 32px;
  width: 280px;
  height: 140px;
  pointer-events: none;

  & > svg {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

// Diagonal orientation reference, top-left corner, fixed size so it reads as
// a tick mark and not a graphic.
const OrientationRef = styled.div`
  position: absolute;
  top: 32px;
  left: 32px;
  width: 64px;
  height: 64px;
  pointer-events: none;

  & > svg {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

// Dimensioning marks rendered as a horizontal strip near the top-right and a
// vertical strip near the bottom-left. Each strip is a small fixed-size SVG
// so the dimensions read at a consistent pixel size regardless of viewport.
const DimensionStrip = styled.div<{
  $top?: string;
  $left?: string;
  $right?: string;
  $bottom?: string;
  $w: number;
  $h: number;
}>`
  position: absolute;
  ${({ $top }) => ($top ? `top: ${$top};` : "")}
  ${({ $left }) => ($left ? `left: ${$left};` : "")}
  ${({ $right }) => ($right ? `right: ${$right};` : "")}
  ${({ $bottom }) => ($bottom ? `bottom: ${$bottom};` : "")}
  width: ${({ $w }) => $w}px;
  height: ${({ $h }) => $h}px;
  pointer-events: none;

  & > svg {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

const GRID_LINE_MAJOR = "rgba(108, 156, 220, 0.08)";
const GRID_LINE_MINOR = "rgba(108, 156, 220, 0.04)";
const FRAME_LINE = "rgba(108, 156, 220, 0.18)";
const TITLE_INK = "rgba(108, 156, 220, 0.18)";
const DIM_INK = "rgba(108, 156, 220, 0.14)";

/**
 * Decorative blueprint-paper backdrop. A tiled cyan-blue grid layered with a
 * sheet border, dimension marks, an orientation reference, and a corner
 * title block. Hidden from assistive tech.
 */
export const BlueprintPaper: React.FC<BlueprintPaperProps> = ({
  className,
}) => {
  const minorId = React.useId();
  const majorId = React.useId();

  return (
    <Outer className={className} aria-hidden="true">
      <svg
        className="cc-bp-grid"
        xmlns="http://www.w3.org/2000/svg"
        preserveAspectRatio="none"
      >
        <defs>
          {/* Minor grid: 8px cells, 0.5px hairlines. */}
          <pattern
            id={minorId}
            width={8}
            height={8}
            patternUnits="userSpaceOnUse"
          >
            <path
              d="M 8 0 L 0 0 0 8"
              fill="none"
              stroke={GRID_LINE_MINOR}
              strokeWidth={0.5}
            />
          </pattern>
          {/* Major grid: 40px cells, 1px lines. Five minor cells per major. */}
          <pattern
            id={majorId}
            width={40}
            height={40}
            patternUnits="userSpaceOnUse"
          >
            <rect width={40} height={40} fill={`url(#${minorId})`} />
            <path
              d="M 40 0 L 0 0 0 40"
              fill="none"
              stroke={GRID_LINE_MAJOR}
              strokeWidth={1}
            />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill={`url(#${majorId})`} />
        {/* Sheet border: outer 1px, inner 0.5px, 4px gap. */}
        <rect
          x={16}
          y={16}
          width="calc(100% - 32px)"
          height="calc(100% - 32px)"
          fill="none"
          stroke={FRAME_LINE}
          strokeWidth={1}
        />
        <rect
          x={20}
          y={20}
          width="calc(100% - 40px)"
          height="calc(100% - 40px)"
          fill="none"
          stroke={FRAME_LINE}
          strokeWidth={0.5}
        />
      </svg>

      {/* Top-left diagonal orientation reference. */}
      <OrientationRef>
        <svg viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg">
          <line
            x1={4}
            y1={32}
            x2={28}
            y2={8}
            stroke={DIM_INK}
            strokeWidth={1}
            strokeDasharray="3 3"
          />
          <line
            x1={32}
            y1={60}
            x2={56}
            y2={36}
            stroke={DIM_INK}
            strokeWidth={1}
            strokeDasharray="3 3"
          />
          <text
            x={36}
            y={20}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={DIM_INK}
            letterSpacing={0.5}
          >
            N 45 E
          </text>
        </svg>
      </OrientationRef>

      {/* Top-right horizontal dimension: "32mm" between two ticks. */}
      <DimensionStrip $top="64px" $right="64px" $w={140} $h={20}>
        <svg viewBox="0 0 140 20" xmlns="http://www.w3.org/2000/svg">
          <line x1={4} y1={4} x2={4} y2={16} stroke={DIM_INK} strokeWidth={1} />
          <line
            x1={136}
            y1={4}
            x2={136}
            y2={16}
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <line
            x1={4}
            y1={10}
            x2={62}
            y2={10}
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <line
            x1={86}
            y1={10}
            x2={136}
            y2={10}
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <text
            x={70}
            y={13}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={DIM_INK}
            letterSpacing={0.4}
          >
            32mm
          </text>
        </svg>
      </DimensionStrip>

      {/* Mid-left vertical dimension: "Ø12" with arrow ticks. */}
      <DimensionStrip $top="40%" $left="48px" $w={48} $h={120}>
        <svg viewBox="0 0 48 120" xmlns="http://www.w3.org/2000/svg">
          <line x1={4} y1={4} x2={16} y2={4} stroke={DIM_INK} strokeWidth={1} />
          <line
            x1={4}
            y1={116}
            x2={16}
            y2={116}
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <line
            x1={10}
            y1={4}
            x2={10}
            y2={48}
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <line
            x1={10}
            y1={72}
            x2={10}
            y2={116}
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <text
            x={24}
            y={63}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={DIM_INK}
            letterSpacing={0.4}
          >
            Ø12
          </text>
        </svg>
      </DimensionStrip>

      {/* Lower-right radius callout: "R8" with leader line. */}
      <DimensionStrip $bottom="200px" $right="200px" $w={88} $h={48}>
        <svg viewBox="0 0 88 48" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M 4 44 Q 4 4 84 4"
            fill="none"
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <line
            x1={44}
            y1={24}
            x2={68}
            y2={36}
            stroke={DIM_INK}
            strokeWidth={1}
          />
          <text
            x={70}
            y={42}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={DIM_INK}
            letterSpacing={0.4}
          >
            R8
          </text>
        </svg>
      </DimensionStrip>

      {/* Bottom-right corner title block. The signature blueprint move. */}
      <TitleBlock>
        <svg viewBox="0 0 280 140" xmlns="http://www.w3.org/2000/svg">
          {/* Outer frame. */}
          <rect
            x={0.5}
            y={0.5}
            width={279}
            height={139}
            fill="none"
            stroke={TITLE_INK}
            strokeWidth={1}
          />
          {/* Row dividers: 4 rows of ~28px. */}
          <line
            x1={0}
            y1={35}
            x2={280}
            y2={35}
            stroke={TITLE_INK}
            strokeWidth={0.5}
          />
          <line
            x1={0}
            y1={70}
            x2={280}
            y2={70}
            stroke={TITLE_INK}
            strokeWidth={0.5}
          />
          <line
            x1={0}
            y1={105}
            x2={280}
            y2={105}
            stroke={TITLE_INK}
            strokeWidth={0.5}
          />
          {/* Label/value column split at 100px. */}
          <line
            x1={100}
            y1={0}
            x2={100}
            y2={140}
            stroke={TITLE_INK}
            strokeWidth={0.5}
          />
          {/* Row 1 */}
          <text
            x={10}
            y={22}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            PROJECT
          </text>
          <text
            x={110}
            y={22}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            TEMPLATES
          </text>
          {/* Row 2 */}
          <text
            x={10}
            y={57}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            SCALE
          </text>
          <text
            x={110}
            y={57}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            1:1
          </text>
          {/* Row 3 */}
          <text
            x={10}
            y={92}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            DATE
          </text>
          <text
            x={110}
            y={92}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            2026.05
          </text>
          {/* Row 4: split into REV and DRAWN BY. */}
          <line
            x1={160}
            y1={105}
            x2={160}
            y2={140}
            stroke={TITLE_INK}
            strokeWidth={0.5}
          />
          <text
            x={10}
            y={127}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            REV
          </text>
          <text
            x={110}
            y={127}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            A
          </text>
          <text
            x={170}
            y={127}
            fontFamily="ui-monospace, monospace"
            fontSize={9}
            fill={TITLE_INK}
            letterSpacing={0.6}
          >
            CHILLICREAM
          </text>
        </svg>
      </TitleBlock>
    </Outer>
  );
};
