"use client";

import React, { FC } from "react";
import styled from "styled-components";

// SolutionThemes paints a per-slug ambient background behind each cinematic
// /solutions/[slug] page. The seven themes share a vocabulary (dark navy
// surface inherited from SolutionsRoot, hairline strokes around 1-1.5px,
// monochromatic accent per slug at very low opacity) but each is its own
// concept so that the seven solution pages read as siblings, not duplicates.
//
//   1. polyglot-federation   typography mosaic of language snippets
//   2. single-graph          one large planet with orbiting dots
//   3. federation            hexagonal honeycomb mesh
//   4. agents                triangular activation lattice
//   5. event-driven          horizontal pulse-stream message bus
//   6. banking               guilloche engraving rosette
//   7. regulated             heraldic seal watermark
//
// Each sub-theme is a functional component returning a single inline SVG.
// All render absolute inset:0 / z-index:0 / pointer-events:none so the
// SolutionsRoot section content paints on top untouched. aria-hidden so the
// background is invisible to assistive tech.

export type SolutionSlug =
  | "polyglot-federation"
  | "single-graph"
  | "federation"
  | "agents"
  | "event-driven"
  | "banking"
  | "regulated";

export interface SolutionThemesProps {
  readonly slug: SolutionSlug;
  readonly className?: string;
}

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
`;

const Svg = styled.svg`
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  display: block;
`;

const BASE_W = 1440;
const BASE_H = 3200;

// ============================================================
// 1. polyglot-federation: typography mosaic of language snippets
// ============================================================

interface Snippet {
  readonly x: number;
  readonly y: number;
  readonly rotation: number;
  readonly lines: readonly string[];
}

const POLYGLOT_SNIPPETS: readonly Snippet[] = [
  {
    x: 120,
    y: 220,
    rotation: -2.4,
    lines: [
      "public class OrderService {",
      "  public Order place(Cart c) {",
      "    return repo.save(...);",
      "  }",
    ],
  },
  {
    x: 940,
    y: 360,
    rotation: 1.8,
    lines: [
      "@app.route('/orders')",
      "def list_orders():",
      "    return jsonify(query())",
    ],
  },
  {
    x: 480,
    y: 640,
    rotation: -0.7,
    lines: [
      "func main() {",
      '    http.HandleFunc("/", h)',
      "    http.ListenAndServe(...)",
      "}",
    ],
  },
  {
    x: 1080,
    y: 820,
    rotation: 2.6,
    lines: [
      "fn handle<T: Send>(",
      "    req: Request<T>,",
      ") -> Result<Response> {",
    ],
  },
  {
    x: 200,
    y: 1080,
    rotation: -1.2,
    lines: ["interface Order {", "  id: string;", "  total: number;", "}"],
  },
  {
    x: 820,
    y: 1280,
    rotation: 2.1,
    lines: ["type Resolver = {", "  Query: { orders: [Order] }", "};"],
  },
  {
    x: 360,
    y: 1560,
    rotation: -2.8,
    lines: ["defmodule Orders do", "  def list, do: Repo.all()", "end"],
  },
  {
    x: 1100,
    y: 1780,
    rotation: 0.9,
    lines: [
      "module Orders",
      "  def self.all",
      "    Order.where(open: true)",
      "  end",
      "end",
    ],
  },
  {
    x: 140,
    y: 2040,
    rotation: 1.6,
    lines: ["SELECT id, total", "FROM orders", "WHERE status = 'open';"],
  },
  {
    x: 720,
    y: 2280,
    rotation: -2.0,
    lines: [
      "(defn list-orders []",
      "  (->> (q {:status :open})",
      "       (map ->dto)))",
    ],
  },
  {
    x: 980,
    y: 2580,
    rotation: 2.4,
    lines: ["extension Order {", "  func place() throws -> Self", "}"],
  },
  {
    x: 280,
    y: 2820,
    rotation: -1.4,
    lines: ["type Query {", "  orders(status: Status): [Order!]!", "}"],
  },
];

const PolyglotMosaic: FC = () => (
  <Svg
    xmlns="http://www.w3.org/2000/svg"
    viewBox={`0 0 ${BASE_W} ${BASE_H}`}
    preserveAspectRatio="xMidYMin slice"
    aria-hidden="true"
  >
    <g
      fill="rgba(245, 241, 234, 0.06)"
      fontFamily="JetBrains Mono, ui-monospace, SFMono-Regular, monospace"
      fontSize={9.5}
    >
      {POLYGLOT_SNIPPETS.map((s, i) => (
        <g
          key={`pg-${i}`}
          transform={`translate(${s.x} ${s.y}) rotate(${s.rotation})`}
        >
          {s.lines.map((line, li) => (
            <text key={`pg-${i}-${li}`} x={0} y={li * 14}>
              {line}
            </text>
          ))}
        </g>
      ))}
    </g>
  </Svg>
);

// ============================================================
// 2. single-graph: one big planet with a few orbiting dots
// ============================================================

const SinglePlanet: FC = () => {
  const cx = 720;
  const cy = 960;
  const r = 360;
  const accent = "rgba(96, 200, 220, 0.06)";
  const accentBright = "rgba(96, 200, 220, 0.22)";

  return (
    <Svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox={`0 0 ${BASE_W} ${BASE_H}`}
      preserveAspectRatio="xMidYMin slice"
      aria-hidden="true"
    >
      {/* Main planet outline */}
      <circle
        cx={cx}
        cy={cy}
        r={r}
        fill="none"
        stroke={accent}
        strokeWidth={1.5}
      />
      {/* Two faint ecliptic hairlines crossing the planet, hinting at axes */}
      <g stroke="rgba(96, 200, 220, 0.04)" strokeWidth={1} fill="none">
        <ellipse cx={cx} cy={cy} rx={r} ry={r * 0.32} />
        <ellipse
          cx={cx}
          cy={cy}
          rx={r * 0.32}
          ry={r}
          transform={`rotate(8 ${cx} ${cy})`}
        />
      </g>
      {/* Orbiting dots at varying angles, all on a slightly larger orbit */}
      <g fill={accentBright}>
        <circle
          cx={cx + Math.cos(-0.6) * (r + 80)}
          cy={cy + Math.sin(-0.6) * (r + 80)}
          r={3}
        />
        <circle
          cx={cx + Math.cos(0.9) * (r + 60)}
          cy={cy + Math.sin(0.9) * (r + 60)}
          r={2.2}
        />
        <circle
          cx={cx + Math.cos(2.6) * (r + 110)}
          cy={cy + Math.sin(2.6) * (r + 110)}
          r={2.6}
        />
        <circle
          cx={cx + Math.cos(4.1) * (r + 70)}
          cy={cy + Math.sin(4.1) * (r + 70)}
          r={2}
        />
      </g>
      {/* Single satellite with a curved arc indicating motion */}
      <g
        stroke="rgba(96, 200, 220, 0.18)"
        strokeWidth={1}
        fill="none"
        strokeLinecap="round"
      >
        <path
          d={`M ${cx + r + 140} ${cy - 40} A ${r + 140} ${r + 140} 0 0 1 ${
            cx + r + 110
          } ${cy + 60}`}
        />
      </g>
      <circle cx={cx + r + 140} cy={cy - 40} r={3.2} fill={accentBright} />
    </Svg>
  );
};

// ============================================================
// 3. federation: hexagonal honeycomb mesh
// ============================================================

const HEX_SIZE = 42; // distance from center to vertex
const HEX_W = HEX_SIZE * Math.sqrt(3); // ~72.7px
const HEX_H = HEX_SIZE * 1.5; // 63px (vertical step)

const hexPoints = (cx: number, cy: number): string => {
  // Pointy-top hexagon: vertices at 30, 90, 150, 210, 270, 330 degrees.
  const pts: string[] = [];
  for (let i = 0; i < 6; i++) {
    const angle = (Math.PI / 180) * (60 * i - 30);
    const x = cx + HEX_SIZE * Math.cos(angle);
    const y = cy + HEX_SIZE * Math.sin(angle);
    pts.push(`${x.toFixed(2)},${y.toFixed(2)}`);
  }
  return pts.join(" ");
};

// Cells whose stroke should read as a brighter "active subgraph".
// Picked spatially so they scatter through the page bands.
const FEDERATION_ACTIVE_POSITIONS: readonly { col: number; row: number }[] = [
  { col: 4, row: 6 },
  { col: 12, row: 11 },
  { col: 7, row: 22 },
  { col: 16, row: 30 },
  { col: 3, row: 38 },
];

const FederationHoneycomb: FC = () => {
  const activeKeys = new Set(
    FEDERATION_ACTIVE_POSITIONS.map((p) => `${p.col}:${p.row}`)
  );
  const cells: { cx: number; cy: number; key: string }[] = [];
  const cols = Math.ceil(BASE_W / HEX_W) + 2;
  const rows = Math.ceil(BASE_H / HEX_H) + 2;
  for (let row = -1; row < rows; row++) {
    for (let col = -1; col < cols; col++) {
      const cx = col * HEX_W + (row % 2 === 0 ? 0 : HEX_W / 2);
      const cy = row * HEX_H;
      cells.push({ cx, cy, key: `${col}:${row}` });
    }
  }

  return (
    <Svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox={`0 0 ${BASE_W} ${BASE_H}`}
      preserveAspectRatio="xMidYMin slice"
      aria-hidden="true"
    >
      <g fill="none" strokeWidth={1}>
        {cells.map(({ cx, cy, key }) => (
          <polygon
            key={key}
            points={hexPoints(cx, cy)}
            stroke={
              activeKeys.has(key)
                ? "rgba(140, 100, 220, 0.16)"
                : "rgba(140, 100, 220, 0.06)"
            }
          />
        ))}
      </g>
    </Svg>
  );
};

// ============================================================
// 4. agents: triangular activation lattice
// ============================================================

interface LatticeDot {
  readonly cx: number;
  readonly cy: number;
}

const TRI_SPACING = 60;
const TRI_ROW_H = TRI_SPACING * (Math.sqrt(3) / 2); // ~52px

const buildTriangularLattice = (): readonly LatticeDot[] => {
  const dots: LatticeDot[] = [];
  const cols = Math.ceil(BASE_W / TRI_SPACING) + 2;
  const rows = Math.ceil(BASE_H / TRI_ROW_H) + 2;
  for (let row = -1; row < rows; row++) {
    for (let col = -1; col < cols; col++) {
      const cx = col * TRI_SPACING + (row % 2 === 0 ? 0 : TRI_SPACING / 2);
      const cy = row * TRI_ROW_H;
      dots.push({ cx, cy });
    }
  }
  return dots;
};

// Lit positions on the triangular lattice, picked to feel deliberate
// (one per major page band).
const AGENTS_LIT_NODES: readonly LatticeDot[] = [
  { cx: 360, cy: 480 },
  { cx: 1140, cy: 1080 },
  { cx: 600, cy: 1860 },
  { cx: 1020, cy: 2640 },
];

const AgentsTriangleLattice: FC = () => {
  const dots = buildTriangularLattice();

  return (
    <Svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox={`0 0 ${BASE_W} ${BASE_H}`}
      preserveAspectRatio="xMidYMin slice"
      aria-hidden="true"
    >
      <defs>
        <radialGradient id="cc-st-agents-halo">
          <stop offset="0%" stopColor="rgba(247, 186, 100, 0.20)" />
          <stop offset="100%" stopColor="rgba(247, 186, 100, 0)" />
        </radialGradient>
      </defs>
      <g fill="rgba(245, 241, 234, 0.06)">
        {dots.map((d, i) => (
          <circle key={`tri-${i}`} cx={d.cx} cy={d.cy} r={1} />
        ))}
      </g>
      {AGENTS_LIT_NODES.map(({ cx, cy }, i) => (
        <g key={`lit-${i}`}>
          <circle cx={cx} cy={cy} r={16} fill="url(#cc-st-agents-halo)" />
          <circle cx={cx} cy={cy} r={4.5} fill="rgba(247, 186, 100, 0.45)" />
        </g>
      ))}
    </Svg>
  );
};

// ============================================================
// 5. event-driven: horizontal pulse-stream message bus
// ============================================================

interface BusRow {
  readonly y: number;
  readonly markers: readonly { x: number; w: number; label?: string }[];
}

const EVENT_ROWS: readonly BusRow[] = [
  {
    y: 360,
    markers: [
      { x: 120, w: 6, label: "t=0.4s" },
      { x: 220, w: 4 },
      { x: 280, w: 4 },
      { x: 340, w: 6 },
      { x: 460, w: 4, label: "t=1.1s" },
      { x: 540, w: 4 },
      { x: 720, w: 4 },
      { x: 820, w: 6 },
      { x: 940, w: 4 },
      { x: 1100, w: 4, label: "+0.3s" },
      { x: 1200, w: 4 },
    ],
  },
  {
    y: 720,
    markers: [
      { x: 180, w: 4 },
      { x: 360, w: 4 },
      { x: 600, w: 6, label: "t=12.4s" },
      { x: 900, w: 4 },
      { x: 1180, w: 4 },
    ],
  },
  {
    y: 1140,
    markers: [
      { x: 80, w: 4 },
      { x: 140, w: 6 },
      { x: 220, w: 4 },
      { x: 290, w: 4 },
      { x: 360, w: 6, label: "t=12.7s" },
      { x: 440, w: 4 },
      { x: 520, w: 4 },
      { x: 600, w: 4 },
      { x: 700, w: 6 },
      { x: 780, w: 4 },
      { x: 860, w: 4 },
      { x: 980, w: 6 },
      { x: 1080, w: 4 },
      { x: 1180, w: 4 },
      { x: 1260, w: 4 },
    ],
  },
  {
    y: 1560,
    markers: [
      { x: 240, w: 4 },
      { x: 480, w: 4, label: "+0.3s" },
      { x: 760, w: 6 },
      { x: 1040, w: 4 },
    ],
  },
  {
    y: 1980,
    markers: [
      { x: 80, w: 4 },
      { x: 200, w: 6, label: "t=18.2s" },
      { x: 320, w: 4 },
      { x: 420, w: 4 },
      { x: 540, w: 4 },
      { x: 640, w: 6 },
      { x: 760, w: 4 },
      { x: 880, w: 4 },
      { x: 1000, w: 6 },
      { x: 1120, w: 4 },
      { x: 1240, w: 4 },
    ],
  },
  {
    y: 2400,
    markers: [
      { x: 160, w: 4 },
      { x: 480, w: 6 },
      { x: 820, w: 4, label: "t=24.0s" },
      { x: 1140, w: 4 },
    ],
  },
  {
    y: 2820,
    markers: [
      { x: 100, w: 6 },
      { x: 220, w: 4 },
      { x: 320, w: 4 },
      { x: 440, w: 6 },
      { x: 540, w: 4 },
      { x: 660, w: 4 },
      { x: 780, w: 6, label: "+0.7s" },
      { x: 900, w: 4 },
      { x: 1040, w: 4 },
      { x: 1160, w: 6 },
      { x: 1280, w: 4 },
    ],
  },
];

const EventStream: FC = () => (
  <Svg
    xmlns="http://www.w3.org/2000/svg"
    viewBox={`0 0 ${BASE_W} ${BASE_H}`}
    preserveAspectRatio="xMidYMin slice"
    aria-hidden="true"
  >
    {/* Bus rails: faint horizontal hairlines under each row */}
    <g stroke="rgba(180, 220, 100, 0.06)" strokeWidth={1} fill="none">
      {EVENT_ROWS.map((row, i) => (
        <line key={`rail-${i}`} x1={0} y1={row.y} x2={BASE_W} y2={row.y} />
      ))}
    </g>
    {/* Message markers as small filled vertical ticks */}
    <g fill="rgba(180, 220, 100, 0.20)">
      {EVENT_ROWS.flatMap((row, ri) =>
        row.markers.map((m, mi) => (
          <rect
            key={`m-${ri}-${mi}`}
            x={m.x}
            y={row.y - 8}
            width={m.w === 6 ? 2.5 : 1.5}
            height={16}
          />
        ))
      )}
    </g>
    {/* Tiny timestamp labels next to selected markers */}
    <g
      fill="rgba(180, 220, 100, 0.32)"
      fontFamily="JetBrains Mono, ui-monospace, SFMono-Regular, monospace"
      fontSize={9}
    >
      {EVENT_ROWS.flatMap((row, ri) =>
        row.markers
          .filter((m) => m.label)
          .map((m, mi) => (
            <text key={`lbl-${ri}-${mi}`} x={m.x + 6} y={row.y - 12}>
              {m.label}
            </text>
          ))
      )}
    </g>
  </Svg>
);

// ============================================================
// 6. banking: guilloche engraving rosette
// ============================================================

const BankingGuilloche: FC = () => {
  const cx = 720;
  const cy = 720;
  const baseR = 320;
  const stroke = "rgba(70, 100, 180, 0.06)";
  const strokeBright = "rgba(70, 100, 180, 0.10)";

  // Build a guilloche path: a rose curve where the radius modulates with
  // a sinusoid as the angle sweeps. We layer multiple curves at different
  // phases / petal counts to get the interweaving paper-currency feel.
  const buildRosette = (
    petals: number,
    amplitude: number,
    phase: number
  ): string => {
    const steps = 720;
    const pts: string[] = [];
    for (let i = 0; i <= steps; i++) {
      const t = (i / steps) * Math.PI * 2;
      const r = baseR + amplitude * Math.sin(petals * t + phase);
      const x = cx + r * Math.cos(t);
      const y = cy + r * Math.sin(t);
      pts.push(`${i === 0 ? "M" : "L"} ${x.toFixed(2)} ${y.toFixed(2)}`);
    }
    return pts.join(" ") + " Z";
  };

  return (
    <Svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox={`0 0 ${BASE_W} ${BASE_H}`}
      preserveAspectRatio="xMidYMin slice"
      aria-hidden="true"
    >
      <g fill="none" stroke={stroke} strokeWidth={1}>
        {/* Outer ring frames */}
        <circle cx={cx} cy={cy} r={baseR + 56} />
        <circle cx={cx} cy={cy} r={baseR + 50} />
        {/* Layered rosettes with rotating phase + varying petal count */}
        <path d={buildRosette(8, 36, 0)} />
        <path d={buildRosette(8, 36, Math.PI / 8)} />
        <path d={buildRosette(12, 24, Math.PI / 12)} />
        <path d={buildRosette(12, 24, 0)} />
        <path d={buildRosette(16, 18, Math.PI / 16)} />
        <path d={buildRosette(20, 14, 0)} />
        {/* Inner ring frames */}
        <circle cx={cx} cy={cy} r={baseR - 110} />
        <circle cx={cx} cy={cy} r={baseR - 116} />
      </g>
      {/* A single brighter inner star to ground the rosette */}
      <g fill="none" stroke={strokeBright} strokeWidth={1}>
        <path d={buildRosette(6, 60, 0)} />
      </g>
      {/* Center monogram tick: a tiny filled disc */}
      <circle cx={cx} cy={cy} r={2.4} fill={strokeBright} />
    </Svg>
  );
};

// ============================================================
// 7. regulated: heraldic seal watermark
// ============================================================

const RegulatedSeal: FC = () => {
  // Shield anchored upper-right. Hand-laid path so the silhouette reads
  // as a classical heraldic shield (rounded shoulders, peaked base).
  const sx = 1180; // top-left x of shield bounding box
  const sy = 200; // top y
  const sw = 200;
  const sh = 280;
  const stroke = "rgba(120, 180, 160, 0.10)";
  const strokeBright = "rgba(120, 180, 160, 0.18)";

  // Shield outline: rounded top, peaked bottom point.
  const shieldPath = `
    M ${sx} ${sy + 14}
    Q ${sx} ${sy} ${sx + 14} ${sy}
    L ${sx + sw - 14} ${sy}
    Q ${sx + sw} ${sy} ${sx + sw} ${sy + 14}
    L ${sx + sw} ${sy + sh * 0.55}
    Q ${sx + sw} ${sy + sh * 0.85} ${sx + sw / 2} ${sy + sh}
    Q ${sx} ${sy + sh * 0.85} ${sx} ${sy + sh * 0.55}
    Z
  `;

  // Inner hairline frame: shield offset by 8px.
  const innerSx = sx + 8;
  const innerSy = sy + 8;
  const innerSw = sw - 16;
  const innerSh = sh - 12;
  const innerShieldPath = `
    M ${innerSx} ${innerSy + 12}
    Q ${innerSx} ${innerSy} ${innerSx + 12} ${innerSy}
    L ${innerSx + innerSw - 12} ${innerSy}
    Q ${innerSx + innerSw} ${innerSy} ${innerSx + innerSw} ${innerSy + 12}
    L ${innerSx + innerSw} ${innerSy + innerSh * 0.55}
    Q ${innerSx + innerSw} ${innerSy + innerSh * 0.85} ${
    innerSx + innerSw / 2
  } ${innerSy + innerSh}
    Q ${innerSx} ${innerSy + innerSh * 0.85} ${innerSx} ${
    innerSy + innerSh * 0.55
  }
    Z
  `;

  // Quartering: a vertical pale and a horizontal fess dividing the shield.
  const midX = sx + sw / 2;
  const midY = sy + sh * 0.42;

  return (
    <Svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox={`0 0 ${BASE_W} ${BASE_H}`}
      preserveAspectRatio="xMidYMin slice"
      aria-hidden="true"
    >
      <g fill="none" stroke={stroke} strokeWidth={1.2}>
        <path d={shieldPath} />
        <path d={innerShieldPath} />
      </g>
      {/* Internal divisions (cross + fess) */}
      <g fill="none" stroke={stroke} strokeWidth={1}>
        <line x1={midX} y1={sy + 16} x2={midX} y2={sy + sh - 24} />
        <line x1={sx + 14} y1={midY} x2={sx + sw - 14} y2={midY} />
      </g>
      {/* Small heraldic ornament in the upper-left quarter: a tiny cross */}
      <g fill="none" stroke={strokeBright} strokeWidth={1}>
        <line
          x1={sx + sw * 0.25 - 8}
          y1={sy + sh * 0.22}
          x2={sx + sw * 0.25 + 8}
          y2={sy + sh * 0.22}
        />
        <line
          x1={sx + sw * 0.25}
          y1={sy + sh * 0.22 - 8}
          x2={sx + sw * 0.25}
          y2={sy + sh * 0.22 + 8}
        />
      </g>
      {/* Compliance label below the shield */}
      <g
        fill="rgba(120, 180, 160, 0.22)"
        fontFamily="JetBrains Mono, ui-monospace, SFMono-Regular, monospace"
        fontSize={10}
        letterSpacing="0.18em"
      >
        <text x={sx + sw / 2} y={sy + sh + 26} textAnchor="middle">
          REGULATORY SOC 2 ISO 27001
        </text>
      </g>
    </Svg>
  );
};

// ============================================================
// SolutionThemes: dispatcher
// ============================================================

/**
 * Per-slug ambient background for the cinematic /solutions/[slug] pages.
 * Renders one of seven distinct themes based on `slug`, each evoking the
 * conceptual signature of that solution while sharing the cinematic
 * vocabulary (dark navy ground, hairline strokes, low-opacity accent ink).
 * Decorative only and hidden from assistive tech.
 */
export const SolutionThemes: FC<SolutionThemesProps> = ({
  slug,
  className,
}) => {
  const theme = renderThemeFor(slug);
  return (
    <Outer className={className} aria-hidden="true">
      {theme}
    </Outer>
  );
};

const renderThemeFor = (slug: SolutionSlug): React.ReactElement => {
  switch (slug) {
    case "polyglot-federation":
      return <PolyglotMosaic />;
    case "single-graph":
      return <SinglePlanet />;
    case "federation":
      return <FederationHoneycomb />;
    case "agents":
      return <AgentsTriangleLattice />;
    case "event-driven":
      return <EventStream />;
    case "banking":
      return <BankingGuilloche />;
    case "regulated":
      return <RegulatedSeal />;
  }
};
