"use client";

import React from "react";
import styled from "styled-components";

// Ambient distributed-trace topology background for the cinematic
// /products/nitro/observability page. Renders a sparse field of small "span"
// dots scattered across the canvas, a few faint hairline edges connecting
// pairs or trios of them, and loose service-cluster outlines around groups
// of related spans. A handful of dots carry tiny mono timestamp labels so
// the field reads as a real trace topology faded into the background.
//
// All positions are hand-placed for compositional balance. The component is
// purely decorative and is hidden from assistive tech.
//
// The SVG fills its container with `position: absolute; inset: 0;` and is
// pointer-events: none so it never intercepts clicks. Wrap it in a
// `position: relative` ancestor (the cinematic root does this) so it spans
// the full page height.

export interface TraceTopologyProps {
  className?: string;
}

// SVG viewBox dimensions. The viewBox is fixed so all positions below are
// deterministic; the SVG scales via `preserveAspectRatio="xMidYMid slice"`
// so the layout keeps its aspect ratio while filling the container.
const VB_W = 1440;
const VB_H = 3000;

// Color tokens. Kept inline so the values are obvious at a glance and the
// component has zero external dependencies beyond styled-components.
const DOT_BASE = "rgba(245, 241, 234, 0.10)";
const DOT_HIGHLIGHT = "rgba(96, 200, 220, 0.30)";
const LABEL_COLOR = "rgba(96, 200, 220, 0.20)";
const EDGE_COLOR = "rgba(96, 200, 220, 0.10)";
const CLUSTER_COLOR = "rgba(245, 241, 234, 0.04)";

interface Dot {
  /** x position in viewBox units. */
  x: number;
  /** y position in viewBox units. */
  y: number;
  /** Radius in viewBox units. 2-3 px reads as a "span" without asserting. */
  r: number;
  /** When true, paint with the cyan highlight; otherwise the bone base. */
  highlight?: boolean;
  /** Optional tiny mono label rendered alongside the dot. */
  label?: {
    text: string;
    /** Anchor side relative to the dot. */
    side: "left" | "right";
    /** Vertical offset from the dot's center. */
    dy?: number;
  };
}

// Hand-placed span dots. Roughly five loose clusters plus a few isolated
// strays so the eye reads service groupings without any obvious grid.
const DOTS: Dot[] = [
  // Cluster A — upper-left "gateway" service. Tight knot of spans, one
  // highlighted as the traced root, one timestamp callout.
  {
    x: 180,
    y: 240,
    r: 2.5,
    highlight: true,
    label: { text: "t=0.00s", side: "right", dy: -6 },
  },
  { x: 230, y: 290, r: 2 },
  { x: 156, y: 318, r: 2 },
  { x: 210, y: 360, r: 2.5, label: { text: "12ms", side: "right", dy: 4 } },
  { x: 264, y: 332, r: 2 },

  // Stray spans linking gateway -> auth area.
  { x: 360, y: 280, r: 2 },
  {
    x: 440,
    y: 250,
    r: 2.5,
    highlight: true,
    label: { text: "87ms", side: "right", dy: -6 },
  },

  // Cluster B — upper-right "auth" service.
  { x: 1080, y: 220, r: 2 },
  { x: 1140, y: 260, r: 2.5, highlight: true },
  { x: 1196, y: 232, r: 2, label: { text: "p99", side: "right", dy: 4 } },
  { x: 1124, y: 320, r: 2 },
  { x: 1060, y: 296, r: 2 },

  // Mid-page stray spans drifting between services.
  { x: 540, y: 480, r: 2 },
  { x: 620, y: 540, r: 2.5, label: { text: "+12ms", side: "right", dy: -6 } },
  { x: 760, y: 520, r: 2, highlight: true },
  { x: 880, y: 580, r: 2 },
  { x: 980, y: 540, r: 2 },

  // Cluster C — left-of-center "products" service near vertical mid.
  { x: 220, y: 980, r: 2 },
  {
    x: 290,
    y: 1020,
    r: 2.5,
    highlight: true,
    label: { text: "t=0.34s", side: "right", dy: -6 },
  },
  { x: 340, y: 1064, r: 2 },
  { x: 268, y: 1098, r: 2 },
  { x: 200, y: 1056, r: 2, label: { text: "412ms", side: "left", dy: 4 } },
  { x: 360, y: 1004, r: 2 },

  // Cluster D — right side "checkout" service.
  { x: 1080, y: 1180, r: 2 },
  {
    x: 1140,
    y: 1228,
    r: 2.5,
    highlight: true,
    label: { text: "228ms", side: "right", dy: -6 },
  },
  { x: 1196, y: 1180, r: 2 },
  { x: 1184, y: 1264, r: 2 },
  { x: 1100, y: 1296, r: 2, label: { text: "p95", side: "right", dy: 4 } },

  // Mid-low strays.
  { x: 480, y: 1380, r: 2 },
  { x: 600, y: 1420, r: 2.5, label: { text: "63ms", side: "right", dy: -6 } },
  { x: 720, y: 1480, r: 2 },
  { x: 840, y: 1440, r: 2, highlight: true },

  // Cluster E — left "inventory" service, lower third.
  { x: 200, y: 1820, r: 2 },
  {
    x: 260,
    y: 1864,
    r: 2.5,
    highlight: true,
    label: { text: "+41ms", side: "right", dy: -6 },
  },
  { x: 320, y: 1900, r: 2 },
  { x: 248, y: 1932, r: 2 },
  { x: 180, y: 1888, r: 2 },

  // Mid lower-third strays.
  { x: 540, y: 2020, r: 2 },
  {
    x: 660,
    y: 2080,
    r: 2.5,
    label: { text: "t=0.91s", side: "right", dy: -6 },
  },
  { x: 780, y: 2040, r: 2 },
  { x: 900, y: 2100, r: 2, highlight: true },

  // Cluster F — right "payments" service near bottom.
  { x: 1100, y: 2180, r: 2 },
  {
    x: 1160,
    y: 2230,
    r: 2.5,
    highlight: true,
    label: { text: "p99", side: "right", dy: 4 },
  },
  { x: 1212, y: 2200, r: 2 },
  { x: 1140, y: 2284, r: 2, label: { text: "184ms", side: "left", dy: 4 } },
  { x: 1080, y: 2256, r: 2 },

  // Bottom strays + a final highlighted endpoint span so the eye trails to
  // the bottom of the page.
  { x: 360, y: 2540, r: 2 },
  { x: 480, y: 2600, r: 2.5, label: { text: "39ms", side: "right", dy: -6 } },
  { x: 620, y: 2580, r: 2 },
  { x: 760, y: 2640, r: 2, highlight: true },
  { x: 900, y: 2600, r: 2 },
  {
    x: 1040,
    y: 2660,
    r: 2.5,
    label: { text: "t=1.20s", side: "right", dy: 4 },
  },
];

interface Cluster {
  /** Cluster outline center x. */
  cx: number;
  /** Cluster outline center y. */
  cy: number;
  /** Cluster outline radius. */
  r: number;
}

// Loose outlines around each service cluster. Sized to enclose the dots in
// that cluster with comfortable padding so the outline reads as ambient.
const CLUSTERS: Cluster[] = [
  { cx: 210, cy: 310, r: 96 }, // A gateway
  { cx: 1130, cy: 270, r: 100 }, // B auth
  { cx: 280, cy: 1040, r: 110 }, // C products
  { cx: 1140, cy: 1230, r: 104 }, // D checkout
  { cx: 245, cy: 1880, r: 98 }, // E inventory
  { cx: 1140, cy: 2230, r: 102 }, // F payments
];

// Hand-placed trace edges. Each edge is a quadratic curve from a -> b with
// a single control point; the eye picks out 2-3 paths threading through
// the field without forming an obvious figure.
interface Edge {
  /** Start dot index. */
  a: number;
  /** End dot index. */
  b: number;
  /** Control point x in viewBox units. */
  cx: number;
  /** Control point y in viewBox units. */
  cy: number;
}

// Index legend (matches the DOTS array above):
//   0..4   Cluster A (gateway)
//   5..6   gateway -> auth strays
//   7..11  Cluster B (auth)
//   12..16 mid-page strays
//   17..22 Cluster C (products)
//   23..27 Cluster D (checkout)
//   28..31 mid-low strays
//   32..36 Cluster E (inventory)
//   37..40 mid lower-third strays
//   41..45 Cluster F (payments)
//   46..51 bottom strays
const EDGES: Edge[] = [
  // Trace 1: gateway root -> stray -> auth highlight (top arc).
  { a: 0, b: 5, cx: 280, cy: 200 },
  { a: 5, b: 6, cx: 410, cy: 220 },
  { a: 6, b: 8, cx: 780, cy: 200 },

  // Trace 2: gateway -> mid strays -> products highlight.
  { a: 3, b: 12, cx: 380, cy: 540 },
  { a: 12, b: 14, cx: 660, cy: 460 },
  { a: 14, b: 18, cx: 540, cy: 820 },

  // Trace 3: products -> mid-low strays -> checkout highlight.
  { a: 20, b: 28, cx: 420, cy: 1280 },
  { a: 28, b: 30, cx: 660, cy: 1340 },
  { a: 30, b: 24, cx: 980, cy: 1340 },

  // Trace 4: checkout -> inventory diagonal.
  { a: 25, b: 33, cx: 700, cy: 1700 },

  // Trace 5: inventory -> mid lower strays -> payments highlight.
  { a: 33, b: 37, cx: 420, cy: 1980 },
  { a: 37, b: 40, cx: 720, cy: 2120 },
  { a: 40, b: 42, cx: 1020, cy: 2180 },

  // Trace 6: payments -> bottom stray -> bottom highlight -> trail end.
  { a: 42, b: 47, cx: 880, cy: 2480 },
  { a: 47, b: 49, cx: 620, cy: 2660 },
  { a: 49, b: 51, cx: 900, cy: 2700 },
];

/**
 * Distributed-trace topology ambient background. Sparse field of span dots
 * with faint hairline edges and loose service-cluster outlines, evoking a
 * real trace diagram laid flat as background texture. Decorative only.
 */
export const TraceTopology: React.FC<TraceTopologyProps> = ({ className }) => {
  return (
    <Root aria-hidden="true" className={className}>
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox={`0 0 ${VB_W} ${VB_H}`}
        preserveAspectRatio="xMidYMid slice"
        style={{
          position: "absolute",
          inset: 0,
          width: "100%",
          height: "100%",
          display: "block",
        }}
      >
        {/* Service cluster outlines. Painted first so dots and edges sit
            on top of them and the outline reads as a containing ring. */}
        <g>
          {CLUSTERS.map((c, i) => (
            <circle
              key={`cluster-${i}`}
              cx={c.cx}
              cy={c.cy}
              r={c.r}
              fill="none"
              stroke={CLUSTER_COLOR}
              strokeWidth={0.5}
            />
          ))}
        </g>

        {/* Trace edges. Dashed quadratic curves so the eye reads them as
            connections without competing with the dots themselves. */}
        <g>
          {EDGES.map((e, i) => {
            const a = DOTS[e.a];
            const b = DOTS[e.b];
            if (!a || !b) {
              return null;
            }
            const d = `M${a.x} ${a.y} Q${e.cx} ${e.cy} ${b.x} ${b.y}`;
            return (
              <path
                key={`edge-${i}`}
                d={d}
                fill="none"
                stroke={EDGE_COLOR}
                strokeWidth={0.75}
                strokeDasharray="4 6"
                strokeLinecap="round"
              />
            );
          })}
        </g>

        {/* Span dots and their optional timestamp labels. */}
        <g>
          {DOTS.map((dot, i) => {
            const fill = dot.highlight ? DOT_HIGHLIGHT : DOT_BASE;
            const labelX =
              dot.label?.side === "left"
                ? dot.x - dot.r - 6
                : dot.x + dot.r + 6;
            const labelY = dot.y + (dot.label?.dy ?? 0);
            const textAnchor = dot.label?.side === "left" ? "end" : "start";
            return (
              <g key={`dot-${i}`}>
                <circle cx={dot.x} cy={dot.y} r={dot.r} fill={fill} />
                {dot.label ? (
                  <text
                    x={labelX}
                    y={labelY}
                    fill={LABEL_COLOR}
                    fontFamily="var(--cc-font-mono), ui-monospace, monospace"
                    fontSize={9}
                    letterSpacing={0.6}
                    textAnchor={textAnchor}
                    dominantBaseline="middle"
                  >
                    {dot.label.text}
                  </text>
                ) : null}
              </g>
            );
          })}
        </g>
      </svg>
    </Root>
  );
};

const Root = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
`;
