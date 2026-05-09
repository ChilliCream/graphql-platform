"use client";

import React from "react";
import styled from "styled-components";

// Topographic contour map background for the cinematic enterprise variant.
// Renders nested closed-curve isolines, like elevation contours surveyed
// across the federation territory. Pure inline SVG, positioned absolute
// inset 0 with low opacity so the page content reads on top.
//
// Composition: a handful of "elevation regions" scattered across a 1600x2400
// viewBox, each region a stack of concentric organic loops drawn as smoothly
// closed cubic-bezier paths. Inner loops are slightly brighter (higher
// elevation), every fifth loop is dashed to evoke the index lines on a real
// topographic map, and a few elevation markers (".450.", ".600.", ".750.")
// are placed sparingly on outer contours.

export interface TopographicContoursProps {
  className?: string;
}

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;

  & > svg {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

const STROKE_OUTER = "rgba(108, 156, 220, 0.10)";
const STROKE_INNER = "rgba(108, 156, 220, 0.18)";
const LABEL_FILL = "rgba(108, 156, 220, 0.32)";

interface RegionSpec {
  /** Region center x in viewBox units. */
  readonly cx: number;
  /** Region center y in viewBox units. */
  readonly cy: number;
  /** Number of nested contours from outermost to innermost. */
  readonly rings: number;
  /** Outer ring radius in viewBox units. */
  readonly outerRadius: number;
  /** Step between rings in viewBox units. */
  readonly step: number;
  /** Aspect-ratio scaling for rx vs ry, 1 is round, >1 stretches horizontally. */
  readonly aspect: number;
  /** Rotation in degrees applied to the whole region. */
  readonly rotation: number;
  /** Per-ring random irregularity seed, deterministic across renders. */
  readonly seed: number;
  /** Optional elevation marker labels keyed by ring index. */
  readonly labels?: Readonly<Record<number, string>>;
}

// Four regions cover the viewBox: a major massif top-left, a smaller ridge
// upper-right, a low plateau mid-left, and a deep basin bottom-right. Counts
// chosen so the page totals roughly 36 contours, comfortably inside the
// 8-12 per region window with 3-4 regions called for.
const REGIONS: readonly RegionSpec[] = [
  {
    cx: 360,
    cy: 420,
    rings: 11,
    outerRadius: 320,
    step: 26,
    aspect: 1.18,
    rotation: -12,
    seed: 17,
    labels: { 0: "·450·", 4: "·600·", 9: "·825·" },
  },
  {
    cx: 1240,
    cy: 320,
    rings: 9,
    outerRadius: 240,
    step: 24,
    aspect: 0.86,
    rotation: 22,
    seed: 41,
    labels: { 1: "·525·", 6: "·750·" },
  },
  {
    cx: 480,
    cy: 1480,
    rings: 8,
    outerRadius: 280,
    step: 30,
    aspect: 1.32,
    rotation: 8,
    seed: 73,
    labels: { 2: "·600·", 7: "·900·" },
  },
  {
    cx: 1280,
    cy: 1820,
    rings: 10,
    outerRadius: 300,
    step: 26,
    aspect: 0.92,
    rotation: -28,
    seed: 109,
    labels: { 0: "·480·", 5: "·675·", 8: "·810·" },
  },
];

/**
 * Deterministic LCG-based pseudo-random in [-1, 1]. Pure function of seed and
 * step, so the SVG renders identically on server and client.
 */
const jitter = (seed: number, step: number): number => {
  const x = Math.sin(seed * 9301 + step * 49297) * 233280;
  return (x - Math.floor(x) - 0.5) * 2;
};

/**
 * Build a closed organic loop around (cx, cy) using N points sampled around
 * an ellipse with per-point radial jitter. Points are joined with smooth
 * cubic beziers (Catmull-Rom-to-bezier) and the path is closed.
 */
const buildLoop = (
  cx: number,
  cy: number,
  rx: number,
  ry: number,
  rotationDeg: number,
  irregularity: number,
  seed: number
): string => {
  const POINTS = 18;
  const cosR = Math.cos((rotationDeg * Math.PI) / 180);
  const sinR = Math.sin((rotationDeg * Math.PI) / 180);

  const pts: { x: number; y: number }[] = [];
  for (let i = 0; i < POINTS; i++) {
    const t = (i / POINTS) * Math.PI * 2;
    const j = jitter(seed, i) * irregularity;
    const lrx = rx * (1 + j);
    const lry = ry * (1 + jitter(seed + 1, i) * irregularity);
    const lx = Math.cos(t) * lrx;
    const ly = Math.sin(t) * lry;
    pts.push({
      x: cx + lx * cosR - ly * sinR,
      y: cy + lx * sinR + ly * cosR,
    });
  }

  // Catmull-Rom to cubic bezier, closed.
  const TENSION = 0.5;
  const segs: string[] = [];
  for (let i = 0; i < POINTS; i++) {
    const p0 = pts[(i - 1 + POINTS) % POINTS];
    const p1 = pts[i];
    const p2 = pts[(i + 1) % POINTS];
    const p3 = pts[(i + 2) % POINTS];
    const c1x = p1.x + ((p2.x - p0.x) / 6) * TENSION * 2;
    const c1y = p1.y + ((p2.y - p0.y) / 6) * TENSION * 2;
    const c2x = p2.x - ((p3.x - p1.x) / 6) * TENSION * 2;
    const c2y = p2.y - ((p3.y - p1.y) / 6) * TENSION * 2;
    if (i === 0) {
      segs.push(`M ${p1.x.toFixed(1)} ${p1.y.toFixed(1)}`);
    }
    segs.push(
      `C ${c1x.toFixed(1)} ${c1y.toFixed(1)}, ${c2x.toFixed(1)} ${c2y.toFixed(
        1
      )}, ${p2.x.toFixed(1)} ${p2.y.toFixed(1)}`
    );
  }
  segs.push("Z");
  return segs.join(" ");
};

interface RegionContoursProps {
  spec: RegionSpec;
}

const RegionContours: React.FC<RegionContoursProps> = ({ spec }) => {
  const { cx, cy, rings, outerRadius, step, aspect, rotation, seed, labels } =
    spec;
  const out: React.ReactNode[] = [];

  for (let i = 0; i < rings; i++) {
    const r = outerRadius - i * step;
    const rx = r * aspect;
    const ry = r;
    // Inner rings get slightly less jitter so the "summit" reads as smoother.
    const irregularity = 0.14 - (i / rings) * 0.08;
    const d = buildLoop(cx, cy, rx, ry, rotation, irregularity, seed + i);
    // Inner half of the stack is the brighter "high elevation" band.
    const stroke = i >= rings / 2 ? STROKE_INNER : STROKE_OUTER;
    // Every fifth ring picks up a dashed stroke (classic topo index line).
    const dashed = i > 0 && i % 5 === 0;

    out.push(
      <path
        key={`r${i}`}
        d={d}
        fill="none"
        stroke={stroke}
        strokeWidth={1}
        strokeDasharray={dashed ? "4 6" : undefined}
        vectorEffect="non-scaling-stroke"
      />
    );

    const label = labels?.[i];
    if (label) {
      // Anchor labels at the right-most point of the loop (rotated frame).
      const cosR = Math.cos((rotation * Math.PI) / 180);
      const sinR = Math.sin((rotation * Math.PI) / 180);
      const lx = cx + rx * cosR;
      const ly = cy + rx * sinR;
      out.push(
        <text
          key={`l${i}`}
          x={lx.toFixed(1)}
          y={ly.toFixed(1)}
          fontFamily="var(--cc-font-mono), monospace"
          fontSize={9}
          letterSpacing="0.08em"
          fill={LABEL_FILL}
          textAnchor="middle"
          dominantBaseline="middle"
        >
          {label}
        </text>
      );
    }
  }

  return <>{out}</>;
};

/**
 * Renders nested topographic contour lines as a non-interactive background
 * for the cinematic enterprise variant. Sits behind page content at z-index
 * 0 and is hidden from assistive tech.
 */
export const TopographicContours: React.FC<TopographicContoursProps> = ({
  className,
}) => {
  return (
    <Outer className={className} aria-hidden="true">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 1600 2400"
        preserveAspectRatio="xMidYMid slice"
      >
        {REGIONS.map((spec, i) => (
          <RegionContours key={i} spec={spec} />
        ))}
      </svg>
    </Outer>
  );
};
