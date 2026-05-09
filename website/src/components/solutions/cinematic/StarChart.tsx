"use client";

import React, { FC, useMemo } from "react";

// StarChart paints an astronomical chart background behind the cinematic
// solution pages. The signature of this approach is per-slug differentiation:
// each solution slug lights up a recognisable constellation centered in the
// canvas, while a shared field of background stars surrounds it.
//
// Visual vocabulary:
//   * background star field    small filled circles, magnitudes 1.5-3.5px,
//                              cream ink at 0.55-0.95 opacity
//   * constellation stars      brighter cream points at the lit positions
//   * constellation lines      dashed cream hairlines connecting the lit
//                              stars so the shape reads as a figure
//   * Greek-letter labels      monospace alpha/beta/gamma/delta/epsilon at
//                              the lower-right of the brightest stars
//   * ecliptic / RA hairlines  very faint diagonal/curved hint of celestial
//                              coordinates so the field reads as a chart
//
// The component renders absolute inset:0 / z-index:0 / pointer-events:none
// behind the page content. The base canvas is 1440x2000; if the page is
// taller, the chart does not repeat — the constellation stays anchored at
// roughly 50% width / 40% height of the canvas as a single focal point.

export interface StarChartProps {
  readonly slug: string;
  readonly className?: string;
}

// ============================================================
// Constellation maps. Each constellation is a list of stars (in viewBox
// coords on the 1440x2000 canvas, centered around x=720 / y=800) and a
// list of edges connecting star indices. Greek labels mark the brightest
// stars in classical bayer order.
// ============================================================

interface ConstellationStar {
  readonly x: number;
  readonly y: number;
  /** Magnitude radius in px. 3.0+ reads as bright, 2.0-2.8 as medium. */
  readonly mag: number;
  /** Optional Greek letter label (alpha, beta, gamma, delta, epsilon). */
  readonly label?: string;
}

interface Constellation {
  readonly name: string;
  readonly stars: readonly ConstellationStar[];
  /** Edges as [fromIndex, toIndex] pairs. */
  readonly edges: readonly (readonly [number, number])[];
}

// Lyra — small lyre/harp shape, ~6 stars. Vega (alpha) at the top.
const LYRA: Constellation = {
  name: "lyra",
  stars: [
    { x: 720, y: 690, mag: 3.4, label: "α" }, // Vega
    { x: 660, y: 800, mag: 2.6, label: "β" },
    { x: 770, y: 800, mag: 2.6, label: "γ" },
    { x: 640, y: 910, mag: 2.4, label: "δ" },
    { x: 790, y: 910, mag: 2.4, label: "ε" },
    { x: 720, y: 970, mag: 2.2 },
  ],
  edges: [
    [0, 1],
    [0, 2],
    [1, 3],
    [2, 4],
    [3, 5],
    [4, 5],
    [1, 2],
  ],
};

// Single bright star at center with a ring of 5 around it (Vega-in-isolation
// motif for the single-graph slug).
const SINGLE_STAR: Constellation = {
  name: "single-star",
  stars: [
    { x: 720, y: 800, mag: 3.6, label: "α" },
    { x: 720, y: 670, mag: 2.2, label: "β" },
    { x: 845, y: 750, mag: 2.2, label: "γ" },
    { x: 800, y: 900, mag: 2.2, label: "δ" },
    { x: 640, y: 900, mag: 2.2, label: "ε" },
    { x: 595, y: 750, mag: 2.2 },
  ],
  edges: [
    [0, 1],
    [0, 2],
    [0, 3],
    [0, 4],
    [0, 5],
  ],
};

// Hydra — long curving line of stars, ~9 stars. Sweeps from upper-left to
// lower-right with a gentle S curve.
const HYDRA: Constellation = {
  name: "hydra",
  stars: [
    { x: 470, y: 690, mag: 2.6, label: "α" }, // head
    { x: 540, y: 720, mag: 2.4, label: "β" },
    { x: 605, y: 760, mag: 2.4 },
    { x: 670, y: 800, mag: 2.8, label: "γ" }, // bright middle
    { x: 740, y: 830, mag: 2.4 },
    { x: 805, y: 855, mag: 2.4, label: "δ" },
    { x: 875, y: 870, mag: 2.6 },
    { x: 945, y: 895, mag: 2.4, label: "ε" },
    { x: 1000, y: 935, mag: 2.6 },
  ],
  edges: [
    [0, 1],
    [1, 2],
    [2, 3],
    [3, 4],
    [4, 5],
    [5, 6],
    [6, 7],
    [7, 8],
  ],
};

// Aquila — eagle, ~7 stars. Altair (alpha) is the bright center; wings
// extend to the sides with a tail behind.
const AQUILA: Constellation = {
  name: "aquila",
  stars: [
    { x: 720, y: 800, mag: 3.4, label: "α" }, // Altair
    { x: 720, y: 730, mag: 2.4, label: "β" },
    { x: 720, y: 870, mag: 2.4, label: "γ" },
    { x: 600, y: 770, mag: 2.6, label: "δ" }, // left wing
    { x: 510, y: 745, mag: 2.4 },
    { x: 840, y: 770, mag: 2.6, label: "ε" }, // right wing
    { x: 930, y: 745, mag: 2.4 },
  ],
  edges: [
    [1, 0],
    [0, 2],
    [3, 0],
    [4, 3],
    [0, 5],
    [5, 6],
  ],
};

// Cygnus — northern cross/swan, 5 stars in a cross pattern. Deneb at the
// tail (top), beta at the head (bottom), gamma at the cross, the wings
// stretching left and right.
const CYGNUS: Constellation = {
  name: "cygnus",
  stars: [
    { x: 720, y: 670, mag: 3.2, label: "α" }, // Deneb (tail)
    { x: 720, y: 800, mag: 2.6, label: "γ" }, // cross center
    { x: 720, y: 930, mag: 2.6, label: "β" }, // head
    { x: 590, y: 800, mag: 2.6, label: "δ" }, // left wing
    { x: 850, y: 800, mag: 2.6, label: "ε" }, // right wing
  ],
  edges: [
    [0, 1],
    [1, 2],
    [3, 1],
    [1, 4],
  ],
};

// Orion's Belt — three stars in a tight diagonal line, plus two flanking
// stars (Betelgeuse / Rigel) for shoulder-and-foot framing. ~5 stars total.
const ORION_BELT: Constellation = {
  name: "orion-belt",
  stars: [
    { x: 645, y: 805, mag: 2.8, label: "α" }, // belt left
    { x: 720, y: 800, mag: 2.8, label: "β" }, // belt center
    { x: 795, y: 795, mag: 2.8, label: "γ" }, // belt right
    { x: 590, y: 690, mag: 3.4, label: "δ" }, // Betelgeuse (upper-left)
    { x: 850, y: 920, mag: 3.4, label: "ε" }, // Rigel (lower-right)
  ],
  edges: [
    [0, 1],
    [1, 2],
    [3, 0],
    [2, 4],
  ],
};

// Bootes — kite shape, ~7 stars. Arcturus (alpha) at the bottom; the kite
// widens upward then narrows to a point.
const BOOTES: Constellation = {
  name: "bootes",
  stars: [
    { x: 720, y: 950, mag: 3.4, label: "α" }, // Arcturus
    { x: 660, y: 850, mag: 2.4, label: "β" },
    { x: 780, y: 850, mag: 2.4, label: "γ" },
    { x: 620, y: 720, mag: 2.6, label: "δ" }, // upper-left
    { x: 820, y: 720, mag: 2.6, label: "ε" }, // upper-right
    { x: 720, y: 660, mag: 2.4 }, // top point
    { x: 720, y: 850, mag: 2.2 }, // kite cross
  ],
  edges: [
    [0, 1],
    [0, 2],
    [1, 3],
    [2, 4],
    [3, 5],
    [4, 5],
    [1, 6],
    [6, 2],
  ],
};

const CONSTELLATIONS: Record<string, Constellation> = {
  "polyglot-federation": LYRA,
  "single-graph": SINGLE_STAR,
  federation: HYDRA,
  agents: AQUILA,
  "event-driven": CYGNUS,
  banking: ORION_BELT,
  regulated: BOOTES,
};

// ============================================================
// Background star field. Deterministic LCG so the same field renders on
// every paint: identical fields per page reload, no React hydration drift.
// 80 stars at small magnitudes scattered across the canvas, with the
// center region thinned out so the lit constellation stays legible.
// ============================================================

interface BackgroundStar {
  readonly x: number;
  readonly y: number;
  readonly r: number;
  readonly opacity: number;
}

const buildBackgroundField = (): readonly BackgroundStar[] => {
  // Linear congruential generator seeded for a stable star field.
  let seed = 0x9e3779b1;
  const rand = (): number => {
    seed = (seed * 1664525 + 1013904223) >>> 0;
    return seed / 0x100000000;
  };

  const stars: BackgroundStar[] = [];
  const target = 88;
  // Avoid placing background stars too close to the constellation focus
  // (centered near 720,800 with ~180px radius) so the lit shape reads.
  const focusX = 720;
  const focusY = 800;
  const focusR = 200;

  while (stars.length < target) {
    const x = rand() * 1440;
    const y = rand() * 2000;
    const dx = x - focusX;
    const dy = y - focusY;
    const dist = Math.sqrt(dx * dx + dy * dy);
    if (dist < focusR) {
      // Skip but allow a few faint stars in the focus area.
      if (rand() > 0.18) {
        continue;
      }
    }
    // Magnitude distribution: lots of tiny stars, a few medium, rare bright.
    const m = rand();
    let r: number;
    let opacity: number;
    if (m < 0.72) {
      r = 1.5 + rand() * 0.6;
      opacity = 0.55 + rand() * 0.18;
    } else if (m < 0.94) {
      r = 2.1 + rand() * 0.6;
      opacity = 0.7 + rand() * 0.15;
    } else {
      r = 2.7 + rand() * 0.8;
      opacity = 0.85 + rand() * 0.1;
    }
    stars.push({ x, y, r, opacity });
  }
  return stars;
};

// ============================================================
// Main component
// ============================================================

const VIEW_W = 1440;
const VIEW_H = 2000;
const CONSTELLATION_INK = "rgba(245, 241, 234, 0.92)";
const CONSTELLATION_LINE = "rgba(245, 241, 234, 0.16)";
const LABEL_INK = "rgba(245, 241, 234, 0.4)";
const GRID_INK = "rgba(245, 241, 234, 0.04)";

export const StarChart: FC<StarChartProps> = ({ slug, className }) => {
  const constellation = CONSTELLATIONS[slug] ?? LYRA;
  const backgroundStars = useMemo(buildBackgroundField, []);

  return (
    <svg
      className={className}
      viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
      preserveAspectRatio="xMidYMin slice"
      aria-hidden
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        width: "100%",
        // Fixed pixel height matching the viewBox so the constellation
        // anchors near the top of the page (around the hero / outcomes
        // bands) regardless of total page height. Bands further down read
        // against the SolutionsRoot navy gradient alone.
        height: `${VIEW_H}px`,
        zIndex: 0,
        pointerEvents: "none",
      }}
    >
      {/* Ecliptic / RA grid hint: two faint diagonal hairlines and a curved
          arc through the chart center. Suggestive of celestial coordinates
          rather than a complete grid. */}
      <g stroke={GRID_INK} strokeWidth={1} fill="none">
        <line x1={-100} y1={520} x2={1540} y2={1080} />
        <line x1={-100} y1={1100} x2={1540} y2={500} />
        <path d="M -40 950 Q 720 720 1480 950" />
        <path d="M -40 1320 Q 720 1100 1480 1320" />
      </g>

      {/* Background star field. Uniform cream points, small magnitudes. */}
      <g fill="rgb(245, 241, 234)">
        {backgroundStars.map((s, i) => (
          <circle
            key={`bg-${i}`}
            cx={s.x}
            cy={s.y}
            r={s.r}
            fillOpacity={s.opacity}
          />
        ))}
      </g>

      {/* Constellation lines: dashed hairlines connecting the lit stars. */}
      <g
        stroke={CONSTELLATION_LINE}
        strokeWidth={1}
        strokeDasharray="2 4"
        fill="none"
      >
        {constellation.edges.map(([a, b], i) => {
          const from = constellation.stars[a];
          const to = constellation.stars[b];
          return (
            <line
              key={`edge-${i}`}
              x1={from.x}
              y1={from.y}
              x2={to.x}
              y2={to.y}
            />
          );
        })}
      </g>

      {/* Constellation stars: cream points sized by magnitude. */}
      <g fill={CONSTELLATION_INK}>
        {constellation.stars.map((s, i) => (
          <circle key={`con-${i}`} cx={s.x} cy={s.y} r={s.mag} />
        ))}
      </g>

      {/* Greek labels on the brightest stars. */}
      <g
        fill={LABEL_INK}
        fontFamily="JetBrains Mono, ui-monospace, SFMono-Regular, monospace"
        fontSize={9}
      >
        {constellation.stars.map((s, i) =>
          s.label ? (
            <text key={`lbl-${i}`} x={s.x + s.mag + 5} y={s.y + s.mag + 9}>
              {s.label}
            </text>
          ) : null
        )}
      </g>
    </svg>
  );
};
