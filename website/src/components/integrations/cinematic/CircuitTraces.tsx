"use client";

import React from "react";
import styled from "styled-components";

// Ambient PCB-trace background for the cinematic /integrations variant.
// Renders a hand-placed scatter of right-angle and 45-mitered copper traces,
// circular vias at junctions and endpoints, small SMD component pads with
// monospace reference designators, and a few faint silkscreen outlines. The
// composition reads as an etched circuit board: every integration is a
// soldered connection at the silicon level.
//
// The SVG sits at `position: absolute; inset: 0; z-index: 0;` behind the
// page content. It is purely decorative and aria-hidden.

export interface CircuitTracesProps {
  className?: string;
}

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;

  & > svg {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

// Trace color palette. The integrations accent is a green oklch around
// (80, 200, 140); the alpha tiers below match the spec.
const TRACE = "rgba(80, 200, 140, 0.16)";
const TRACE_HOT = "rgba(80, 200, 140, 0.24)";
const VIA = "rgba(80, 200, 140, 0.30)";
const PAD_FILL = "rgba(80, 200, 140, 0.10)";
const PAD_STROKE = "rgba(80, 200, 140, 0.22)";
const REFDES = "rgba(80, 200, 140, 0.20)";
const SILK = "rgba(245, 241, 234, 0.07)";

// Trace definitions. Each trace is a list of (x,y) waypoints connected by
// 45-mitered or right-angle segments. The renderer walks consecutive
// waypoints and emits horizontal / vertical / 45-degree diagonals so the
// path stays orthogonal in the PCB sense (no bezier curves). Coordinates
// are in the 1440x3000 viewBox.
interface TraceDef {
  pts: [number, number][];
  width: number;
  hot?: boolean;
}

// Endpoint via marker. Hot endpoints get a brighter ring.
interface ViaDef {
  x: number;
  y: number;
  hot?: boolean;
}

// Surface-mount pad with optional reference designator.
interface PadDef {
  x: number;
  y: number;
  w: number;
  h: number;
  refdes?: string;
  refdesPos?: "above" | "below" | "right";
}

// Silkscreen outline (dashed white rectangle around a component group).
interface SilkDef {
  x: number;
  y: number;
  w: number;
  h: number;
}

// Composition: 16 hand-placed traces snaking across the canvas. Some run
// horizontally, some bend through 45-degree corners, and one (T6/T7 share
// the (700, 740) via) demonstrates a branching split. Vias are placed at
// every endpoint and at branch points.
const TRACES: TraceDef[] = [
  // T1: top-left horizontal run with a single 45-mitre dip
  {
    pts: [
      [40, 180],
      [380, 180],
      [460, 260],
      [720, 260],
    ],
    width: 1.5,
    hot: true,
  },
  // T2: top-right S-curve down to mid
  {
    pts: [
      [1400, 120],
      [1100, 120],
      [1020, 200],
      [1020, 380],
      [880, 520],
    ],
    width: 1,
  },
  // T3: long horizontal trunk near top
  {
    pts: [
      [40, 420],
      [240, 420],
      [320, 500],
      [620, 500],
      [700, 420],
      [1180, 420],
    ],
    width: 2,
    hot: true,
  },
  // T4: mid-left vertical drop with 45-mitre kick
  {
    pts: [
      [160, 580],
      [160, 800],
      [240, 880],
      [240, 1080],
    ],
    width: 1,
  },
  // T5: mid-right diagonal staircase
  {
    pts: [
      [1380, 640],
      [1140, 640],
      [1060, 720],
      [1060, 880],
      [980, 960],
    ],
    width: 1.5,
  },
  // T6: branching trunk (shares junction at 700,740 with T7)
  {
    pts: [
      [40, 740],
      [620, 740],
      [700, 740],
      [820, 740],
      [900, 820],
      [1240, 820],
    ],
    width: 1.5,
    hot: true,
  },
  // T7: branch off T6's junction, drops down
  {
    pts: [
      [700, 740],
      [700, 980],
      [780, 1060],
      [780, 1240],
    ],
    width: 1,
  },
  // T8: lower-left snake
  {
    pts: [
      [40, 1180],
      [320, 1180],
      [400, 1100],
      [560, 1100],
      [640, 1180],
      [880, 1180],
    ],
    width: 1,
  },
  // T9: long horizontal mid-low trunk
  {
    pts: [
      [120, 1380],
      [400, 1380],
      [480, 1460],
      [880, 1460],
      [960, 1380],
      [1340, 1380],
    ],
    width: 2,
    hot: true,
  },
  // T10: vertical riser on right
  {
    pts: [
      [1280, 1540],
      [1280, 1740],
      [1200, 1820],
      [1200, 2020],
    ],
    width: 1.5,
  },
  // T11: lower-mid horizontal
  {
    pts: [
      [200, 1660],
      [520, 1660],
      [600, 1580],
      [820, 1580],
    ],
    width: 1,
  },
  // T12: deep low S
  {
    pts: [
      [40, 1900],
      [340, 1900],
      [420, 1980],
      [700, 1980],
      [780, 1900],
      [1080, 1900],
    ],
    width: 1.5,
    hot: true,
  },
  // T13: bottom-left trace
  {
    pts: [
      [140, 2200],
      [140, 2380],
      [220, 2460],
      [560, 2460],
    ],
    width: 1,
  },
  // T14: bottom-mid diagonal staircase
  {
    pts: [
      [440, 2200],
      [620, 2200],
      [700, 2280],
      [700, 2480],
      [800, 2580],
      [1060, 2580],
    ],
    width: 1,
  },
  // T15: bottom-right vertical
  {
    pts: [
      [1320, 2200],
      [1320, 2400],
      [1240, 2480],
      [1240, 2700],
    ],
    width: 1.5,
    hot: true,
  },
  // T16: bottom horizontal trunk
  {
    pts: [
      [80, 2780],
      [380, 2780],
      [460, 2860],
      [880, 2860],
      [960, 2780],
      [1380, 2780],
    ],
    width: 1.5,
  },
];

// Vias: junctions and endpoints. Hand-derived from the trace endpoints
// and branch points above.
const VIAS: ViaDef[] = [
  // T1
  { x: 40, y: 180 },
  { x: 720, y: 260, hot: true },
  // T2
  { x: 1400, y: 120 },
  { x: 880, y: 520 },
  // T3
  { x: 40, y: 420 },
  { x: 1180, y: 420, hot: true },
  // T4
  { x: 160, y: 580 },
  { x: 240, y: 1080 },
  // T5
  { x: 1380, y: 640 },
  { x: 980, y: 960 },
  // T6 + T7 branch junction
  { x: 40, y: 740 },
  { x: 700, y: 740, hot: true },
  { x: 1240, y: 820, hot: true },
  // T7
  { x: 780, y: 1240 },
  // T8
  { x: 40, y: 1180 },
  { x: 880, y: 1180 },
  // T9
  { x: 120, y: 1380 },
  { x: 1340, y: 1380, hot: true },
  // T10
  { x: 1280, y: 1540 },
  { x: 1200, y: 2020 },
  // T11
  { x: 200, y: 1660 },
  { x: 820, y: 1580 },
  // T12
  { x: 40, y: 1900 },
  { x: 1080, y: 1900, hot: true },
  // T13
  { x: 140, y: 2200 },
  { x: 560, y: 2460 },
  // T14
  { x: 440, y: 2200 },
  { x: 1060, y: 2580 },
  // T15
  { x: 1320, y: 2200 },
  { x: 1240, y: 2700, hot: true },
  // T16
  { x: 80, y: 2780 },
  { x: 1380, y: 2780 },
];

// SMD pads scattered along the trace network. The reference designators
// follow real PCB conventions: U=IC, R=resistor, C=capacitor, J=connector.
const PADS: PadDef[] = [
  { x: 380, y: 174, w: 12, h: 8, refdes: "U1", refdesPos: "above" },
  { x: 460, y: 254, w: 8, h: 12, refdes: "R3", refdesPos: "right" },
  { x: 1100, y: 114, w: 12, h: 8, refdes: "C7", refdesPos: "below" },
  { x: 320, y: 414, w: 8, h: 12, refdes: "R12", refdesPos: "right" },
  { x: 620, y: 494, w: 12, h: 8, refdes: "U4", refdesPos: "above" },
  { x: 900, y: 814, w: 12, h: 8, refdes: "U2", refdesPos: "above" },
  { x: 1140, y: 634, w: 8, h: 12, refdes: "C3" },
  { x: 700, y: 974, w: 12, h: 8, refdes: "R7", refdesPos: "below" },
  { x: 480, y: 1374, w: 8, h: 12, refdes: "C12", refdesPos: "right" },
  { x: 960, y: 1374, w: 12, h: 8, refdes: "U6", refdesPos: "above" },
  { x: 600, y: 1574, w: 12, h: 8, refdes: "R18", refdesPos: "above" },
  { x: 420, y: 1974, w: 8, h: 12, refdes: "C9" },
  { x: 780, y: 1974, w: 12, h: 8, refdes: "U8", refdesPos: "below" },
  { x: 220, y: 2454, w: 12, h: 8, refdes: "J2", refdesPos: "above" },
  { x: 800, y: 2574, w: 8, h: 12, refdes: "R22" },
  { x: 460, y: 2854, w: 12, h: 8, refdes: "U5", refdesPos: "above" },
  { x: 960, y: 2774, w: 8, h: 12, refdes: "C5", refdesPos: "right" },
];

// Silkscreen outlines: faint dashed rectangles around component clusters,
// like a real PCB's silkscreen layer.
const SILKS: SilkDef[] = [
  { x: 360, y: 150, w: 130, h: 60 },
  { x: 1080, y: 90, w: 60, h: 60 },
  { x: 600, y: 470, w: 130, h: 60 },
  { x: 870, y: 790, w: 60, h: 60 },
  { x: 940, y: 1350, w: 60, h: 60 },
  { x: 760, y: 1950, w: 60, h: 60 },
  { x: 200, y: 2430, w: 60, h: 60 },
  { x: 440, y: 2830, w: 130, h: 60 },
];

// Builds an SVG `d` attribute from a list of waypoints, mitring each
// corner with a 45-degree segment (8px) so the trace bends like real
// copper, not at sharp 90-degree angles. Adjacent waypoints must share
// either x or y for the right-angle assumption to hold.
function tracePath(pts: [number, number][]): string {
  if (pts.length === 0) {
    return "";
  }
  const parts: string[] = [];
  parts.push(`M ${pts[0][0]} ${pts[0][1]}`);

  const MITRE = 8;

  for (let i = 1; i < pts.length; i++) {
    const [px, py] = pts[i - 1];
    const [cx, cy] = pts[i];

    if (i < pts.length - 1) {
      const [nx, ny] = pts[i + 1];
      // Decide if we need to mitre at (cx, cy) by checking if the
      // incoming and outgoing axes differ.
      const inHoriz = py === cy;
      const outHoriz = cy === ny;
      if (inHoriz !== outHoriz) {
        // Mitre corner. Pull back from (cx,cy) by MITRE along the
        // incoming direction, then cut a 45-degree segment toward the
        // outgoing direction's start.
        const inDx = Math.sign(cx - px);
        const inDy = Math.sign(cy - py);
        const outDx = Math.sign(nx - cx);
        const outDy = Math.sign(ny - cy);
        const ax = cx - inDx * MITRE;
        const ay = cy - inDy * MITRE;
        const bx = cx + outDx * MITRE;
        const by = cy + outDy * MITRE;
        parts.push(`L ${ax} ${ay}`);
        parts.push(`L ${bx} ${by}`);
        continue;
      }
    }
    parts.push(`L ${cx} ${cy}`);
  }
  return parts.join(" ");
}

// Renders a single via: a small filled outer disc with a hollow center,
// echoing a real PCB plated through-hole.
const Via: React.FC<{ x: number; y: number; hot?: boolean }> = ({
  x,
  y,
  hot,
}) => {
  const rOuter = hot ? 3.2 : 2.6;
  const rInner = hot ? 1.4 : 1.1;
  return (
    <g>
      <circle
        cx={x}
        cy={y}
        r={rOuter}
        fill="none"
        stroke={VIA}
        strokeWidth={1.2}
      />
      <circle cx={x} cy={y} r={rInner} fill={VIA} />
    </g>
  );
};

// Renders an SMD pad: a filled rectangle with a thin border. The reference
// designator (U1, R12, ...) sits above, below, or to the right of the pad.
const Pad: React.FC<PadDef> = ({ x, y, w, h, refdes, refdesPos }) => {
  let tx = x + w / 2;
  let ty = y - 3;
  let textAnchor: "start" | "middle" = "middle";
  if (refdesPos === "below") {
    ty = y + h + 9;
  } else if (refdesPos === "right") {
    tx = x + w + 4;
    ty = y + h / 2 + 3;
    textAnchor = "start";
  }
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        rx={1}
        fill={PAD_FILL}
        stroke={PAD_STROKE}
        strokeWidth={0.75}
      />
      {refdes ? (
        <text
          x={tx}
          y={ty}
          fill={REFDES}
          fontFamily="ui-monospace, SFMono-Regular, monospace"
          fontSize={8}
          letterSpacing="0.5"
          textAnchor={textAnchor}
        >
          {refdes}
        </text>
      ) : null}
    </g>
  );
};

/**
 * Decorative PCB-trace background for the cinematic /integrations variant.
 * Renders a scatter of right-angle copper traces with vias at junctions,
 * a few SMD pads with reference designators, and faint silkscreen outlines.
 * Hidden from assistive tech and never receives pointer events.
 */
export const CircuitTraces: React.FC<CircuitTracesProps> = ({ className }) => {
  return (
    <Outer className={className} aria-hidden="true">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 1440 3000"
        preserveAspectRatio="xMidYMid slice"
      >
        {/* Silkscreen layer (component group outlines) */}
        {SILKS.map((s, i) => (
          <rect
            key={`silk-${i}`}
            x={s.x}
            y={s.y}
            width={s.w}
            height={s.h}
            fill="none"
            stroke={SILK}
            strokeWidth={0.6}
            strokeDasharray="3 3"
            rx={2}
          />
        ))}

        {/* Copper traces */}
        {TRACES.map((t, i) => (
          <path
            key={`trace-${i}`}
            d={tracePath(t.pts)}
            fill="none"
            stroke={t.hot ? TRACE_HOT : TRACE}
            strokeWidth={t.width}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        ))}

        {/* SMD pads with reference designators */}
        {PADS.map((p, i) => (
          <Pad key={`pad-${i}`} {...p} />
        ))}

        {/* Vias */}
        {VIAS.map((v, i) => (
          <Via key={`via-${i}`} x={v.x} y={v.y} hot={v.hot} />
        ))}
      </svg>
    </Outer>
  );
};
