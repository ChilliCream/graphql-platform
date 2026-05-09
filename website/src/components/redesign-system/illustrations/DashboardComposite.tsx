"use client";

import React from "react";

// Synthetic-dashboard register: 2-3 layered mock UI panels with real-looking
// charts, designed to be placed at the upper-right of a hero and bleed off
// the page edge. Goal: looks like a real product wall, not a single static
// image.
//
// Each panel is its own tiny inline-SVG mock. The composite layers them:
// panel 0 in front, panels 1-2 offset back-right (or back-left) with subtle
// dimming. Decorative, `aria-hidden`.

const ACCENT_FALLBACK = "var(--cc-accent, currentColor)";

export type DashboardPanelKind =
  | "trace"
  | "chart"
  | "log-stream"
  | "kpi-tile"
  | "schema-diff";

export interface DashboardCompositeProps {
  /** Up to 3 panels, layered (z-stacked) with offsets. Order: front to back. */
  panels: DashboardPanelKind[];
  accent?: string;
  bleedDirection?: "right" | "left";
  className?: string;
}

// ---------- panel primitives ----------

interface PanelProps {
  accent: string;
}

const PANEL_W = 360;
const PANEL_H = 240;

const PanelFrame: React.FC<{
  children: React.ReactNode;
  title: string;
  accent: string;
}> = ({ children, title, accent }) => {
  return (
    <g>
      <rect
        x={0}
        y={0}
        width={PANEL_W}
        height={PANEL_H}
        rx={10}
        ry={10}
        fill="rgba(10, 13, 24, 0.92)"
        stroke="currentColor"
        strokeOpacity={0.18}
        strokeWidth={1}
      />
      <line
        x1={0}
        y1={32}
        x2={PANEL_W}
        y2={32}
        stroke="currentColor"
        strokeOpacity={0.12}
        strokeWidth={1}
      />
      <circle cx={14} cy={16} r={3} fill="currentColor" opacity={0.32} />
      <circle cx={26} cy={16} r={3} fill="currentColor" opacity={0.32} />
      <circle cx={38} cy={16} r={3} fill={accent} opacity={0.7} />
      <text
        x={56}
        y={20}
        fontFamily="var(--cc-font-mono), monospace"
        fontSize={10}
        letterSpacing="0.12em"
        fill="currentColor"
        opacity={0.62}
      >
        {title.toUpperCase()}
      </text>
      {children}
    </g>
  );
};

const TracePanel: React.FC<PanelProps> = ({ accent }) => {
  const rows = [
    { x: 60, w: 240, color: "currentColor", op: 0.42 },
    { x: 80, w: 200, color: "currentColor", op: 0.28 },
    { x: 100, w: 168, color: accent, op: 0.85 },
    { x: 120, w: 80, color: "currentColor", op: 0.28 },
    { x: 140, w: 56, color: "currentColor", op: 0.22 },
    { x: 100, w: 64, color: "currentColor", op: 0.22 },
  ];
  return (
    <PanelFrame title="trace · /orders" accent={accent}>
      {rows.map((r, i) => {
        const y = 56 + i * 26;
        return (
          <g key={i}>
            <text
              x={16}
              y={y + 9}
              fontFamily="var(--cc-font-mono), monospace"
              fontSize={9}
              fill="currentColor"
              opacity={0.5}
            >
              {["gateway", "auth", "orders", "db.query", "cache", "audit"][i]}
            </text>
            <rect
              x={r.x}
              y={y}
              width={r.w}
              height={12}
              rx={2}
              ry={2}
              fill={r.color}
              opacity={r.op}
            />
          </g>
        );
      })}
    </PanelFrame>
  );
};

const ChartPanel: React.FC<PanelProps> = ({ accent }) => {
  const points = [
    [16, 180],
    [44, 168],
    [72, 172],
    [100, 150],
    [128, 156],
    [156, 132],
    [184, 138],
    [212, 110],
    [240, 96],
    [268, 102],
    [296, 78],
    [324, 86],
    [344, 64],
  ] as const;
  const polyline = points.map((p) => p.join(",")).join(" ");
  const area = `M ${points[0][0]},${PANEL_H - 20} L ${polyline} L ${
    points[points.length - 1][0]
  },${PANEL_H - 20} Z`;
  return (
    <PanelFrame title="p99 latency" accent={accent}>
      <g stroke="currentColor" strokeOpacity={0.1} strokeWidth={1}>
        {[60, 100, 140, 180].map((y) => (
          <line key={y} x1={0} y1={y} x2={PANEL_W} y2={y} />
        ))}
      </g>
      <path d={area} fill={accent} opacity={0.18} />
      <polyline
        points={polyline}
        fill="none"
        stroke={accent}
        strokeWidth={1.8}
      />
      {points.map((p, i) => (
        <circle key={i} cx={p[0]} cy={p[1]} r={1.8} fill={accent} />
      ))}
    </PanelFrame>
  );
};

const LogStreamPanel: React.FC<PanelProps> = ({ accent }) => {
  const lines = [
    { ts: "12:04:11", lvl: "INFO", msg: "GET /orders 200 · 86ms", k: "info" },
    {
      ts: "12:04:11",
      lvl: "INFO",
      msg: "subgraph.users.lookup · 12ms",
      k: "info",
    },
    {
      ts: "12:04:12",
      lvl: "WARN",
      msg: "cache miss · key=user:4c2a",
      k: "warn",
    },
    {
      ts: "12:04:12",
      lvl: "INFO",
      msg: "subgraph.billing.plan · 18ms",
      k: "info",
    },
    {
      ts: "12:04:13",
      lvl: "ERROR",
      msg: "downstream timeout · /payments",
      k: "err",
    },
    { ts: "12:04:13", lvl: "INFO", msg: "GET /orders 200 · 91ms", k: "info" },
    { ts: "12:04:14", lvl: "INFO", msg: "GET /orders 200 · 84ms", k: "info" },
  ];
  return (
    <PanelFrame title="log stream" accent={accent}>
      {lines.map((l, i) => {
        const y = 50 + i * 24;
        const lvlColor =
          l.k === "err"
            ? "rgba(255, 130, 130, 0.92)"
            : l.k === "warn"
            ? accent
            : "currentColor";
        const lvlOp = l.k === "info" ? 0.6 : 1;
        return (
          <g
            key={i}
            fontFamily="var(--cc-font-mono), monospace"
            fontSize={9}
            fill="currentColor"
          >
            <text x={16} y={y} opacity={0.42}>
              {l.ts}
            </text>
            <text x={70} y={y} fill={lvlColor} opacity={lvlOp}>
              {l.lvl}
            </text>
            <text x={108} y={y} opacity={0.74}>
              {l.msg}
            </text>
          </g>
        );
      })}
    </PanelFrame>
  );
};

const KpiTilePanel: React.FC<PanelProps> = ({ accent }) => {
  const tiles = [
    { label: "p99", value: "112ms", trend: "-18%" },
    { label: "errors", value: "0.04%", trend: "-62%" },
    { label: "rps", value: "8.2k", trend: "+12%" },
    { label: "saturation", value: "41%", trend: "stable" },
  ];
  return (
    <PanelFrame title="kpis · last 24h" accent={accent}>
      {tiles.map((t, i) => {
        const col = i % 2;
        const row = Math.floor(i / 2);
        const x = 16 + col * 168;
        const y = 50 + row * 88;
        return (
          <g key={i}>
            <rect
              x={x}
              y={y}
              width={160}
              height={76}
              rx={6}
              ry={6}
              fill="currentColor"
              opacity={0.04}
              stroke="currentColor"
              strokeOpacity={0.12}
            />
            <text
              x={x + 12}
              y={y + 18}
              fontFamily="var(--cc-font-mono), monospace"
              fontSize={9}
              letterSpacing="0.14em"
              fill="currentColor"
              opacity={0.5}
            >
              {t.label.toUpperCase()}
            </text>
            <text
              x={x + 12}
              y={y + 46}
              fontFamily="var(--cc-font-sans), sans-serif"
              fontSize={20}
              fontWeight={500}
              fill="currentColor"
            >
              {t.value}
            </text>
            <text
              x={x + 12}
              y={y + 64}
              fontFamily="var(--cc-font-mono), monospace"
              fontSize={9}
              fill={accent}
            >
              {t.trend}
            </text>
          </g>
        );
      })}
    </PanelFrame>
  );
};

const SchemaDiffPanel: React.FC<PanelProps> = ({ accent }) => {
  const lines = [
    { kind: "ctx", text: "type BillingAddress {" },
    { kind: "ctx", text: "  street: String!" },
    { kind: "ctx", text: "  city: String!" },
    { kind: "del", text: "  zip: String!" },
    { kind: "add", text: "  postalCode: String!" },
    { kind: "ctx", text: "  country: String!" },
    { kind: "ctx", text: "}" },
  ] as const;
  return (
    <PanelFrame title="schema diff" accent={accent}>
      {lines.map((l, i) => {
        const y = 50 + i * 22;
        const fill =
          l.kind === "add"
            ? "rgba(120, 220, 160, 0.16)"
            : l.kind === "del"
            ? "rgba(255, 140, 140, 0.16)"
            : "transparent";
        const sigil = l.kind === "add" ? "+" : l.kind === "del" ? "-" : " ";
        const sigilColor =
          l.kind === "add"
            ? "rgba(120, 220, 160, 0.95)"
            : l.kind === "del"
            ? "rgba(255, 140, 140, 0.95)"
            : "currentColor";
        return (
          <g key={i}>
            <rect
              x={8}
              y={y - 12}
              width={PANEL_W - 16}
              height={20}
              rx={3}
              ry={3}
              fill={fill}
            />
            <text
              x={20}
              y={y}
              fontFamily="var(--cc-font-mono), monospace"
              fontSize={10}
              fill={sigilColor}
              opacity={l.kind === "ctx" ? 0.4 : 1}
            >
              {sigil}
            </text>
            <text
              x={36}
              y={y}
              fontFamily="var(--cc-font-mono), monospace"
              fontSize={10}
              fill="currentColor"
              opacity={l.kind === "ctx" ? 0.7 : 0.92}
            >
              {l.text}
            </text>
          </g>
        );
      })}
      <circle cx={PANEL_W - 24} cy={20} r={4} fill={accent} />
    </PanelFrame>
  );
};

const PANEL_RENDERERS: Record<DashboardPanelKind, React.FC<PanelProps>> = {
  trace: TracePanel,
  chart: ChartPanel,
  "log-stream": LogStreamPanel,
  "kpi-tile": KpiTilePanel,
  "schema-diff": SchemaDiffPanel,
};

// ---------- composite ----------

const COMPOSITE_W = 640;
const COMPOSITE_H = 440;

/**
 * Layered mock UI panels that read as a product wall. Place at the upper
 * edge of a hero and let it bleed off the page in `bleedDirection`.
 */
export const DashboardComposite: React.FC<DashboardCompositeProps> = ({
  panels,
  accent,
  bleedDirection = "right",
  className,
}) => {
  const accentColor = accent ?? ACCENT_FALLBACK;
  const limited = panels.slice(0, 3);
  const sign = bleedDirection === "right" ? 1 : -1;

  // Layer offsets: panel 0 is in front, panel 1 sits behind and offset back,
  // panel 2 sits even further back. The translate values place the front
  // panel near the centre and offset the rear panels toward the bleed edge.
  const layers = limited.map((kind, i) => {
    const reverseIndex = limited.length - 1 - i;
    const dx = 80 + reverseIndex * 110 * sign;
    const dy = 30 + reverseIndex * 36;
    const opacity =
      i === limited.length - 1 ? 1 : 0.6 - (limited.length - 2 - i) * 0.18;
    return { kind, dx, dy, opacity, key: `${kind}-${i}` };
  });

  return (
    <svg
      className={className}
      viewBox={`0 0 ${COMPOSITE_W} ${COMPOSITE_H}`}
      preserveAspectRatio="xMidYMid meet"
      aria-hidden="true"
    >
      {layers.map((layer) => {
        const Renderer = PANEL_RENDERERS[layer.kind];
        return (
          <g
            key={layer.key}
            transform={`translate(${layer.dx} ${layer.dy})`}
            opacity={layer.opacity}
          >
            <Renderer accent={accentColor} />
          </g>
        );
      })}
    </svg>
  );
};
