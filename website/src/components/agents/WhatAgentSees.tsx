"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { AgentSeesKind, AGENT_SEES_TILES } from "@/data/agents/agent-sees";

// Section 04: six tiles, each carrying a mini-visual that gestures at the
// kind of signal Nitro exposes through MCP. Borderless on a default band
// (no card chrome) so the grid reads as a "what's queryable" inventory
// rather than yet another rack of dark cards. Card chrome is reserved for
// guardrails (where the edge IS the constraint signal).

const STROKE = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.4,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

// Mini trace waterfall: 4 stacked rows, named lanes + colored bars. Smaller
// than TraceWaterfall, optimized to read at tile size.
const MiniTrace: FC = () => {
  const rows = [
    { name: "gateway", color: "var(--cc-ink)", start: 0, end: 0.92 },
    { name: "Catalog", color: "var(--cc-col-cat)", start: 0.04, end: 0.16 },
    { name: "Billing", color: "var(--cc-col-bil)", start: 0.18, end: 0.84 },
    { name: "Shipping", color: "var(--cc-col-shi)", start: 0.36, end: 0.5 },
  ];
  return (
    <div className="cc-ag-sees-mini-trace">
      {rows.map((r) => (
        <div key={r.name} className="cc-ag-sees-mini-trace-row">
          <span className="name">{r.name}</span>
          <span className="bar-track">
            <span
              className="bar"
              style={{
                left: `${r.start * 100}%`,
                width: `${(r.end - r.start) * 100}%`,
                background: r.color,
                opacity: 0.85,
              }}
            />
          </span>
          <span className="ms">{Math.round((r.end - r.start) * 600)}ms</span>
        </div>
      ))}
    </div>
  );
};

// Sparkline + tick marks. 60 schematic points, last one accented with a dot.
const Sparkline: FC = () => {
  const W = 260;
  const H = 88;
  const points = [
    24, 28, 26, 32, 30, 36, 33, 40, 38, 44, 42, 48, 46, 52, 50, 58, 64, 60, 72,
    68, 76, 80, 84, 78, 70, 62, 54, 50, 56, 62, 68, 74, 80, 86, 82, 90,
  ];
  const max = Math.max(...points);
  const min = Math.min(...points);
  const norm = (v: number) => H - 14 - ((v - min) / (max - min)) * (H - 28);
  const path = points
    .map(
      (p, i) =>
        `${i === 0 ? "M" : "L"} ${(W * i) / (points.length - 1)} ${norm(p)}`
    )
    .join(" ");
  const lastX = W;
  const lastY = norm(points[points.length - 1]);
  return (
    <svg viewBox={`0 0 ${W} ${H}`} width="100%" height={H} aria-hidden>
      <line
        x1="0"
        y1={H - 6}
        x2={W}
        y2={H - 6}
        stroke="var(--cc-ink-faint)"
        strokeDasharray="2 4"
      />
      <path
        d={`${path} L ${W} ${H - 6} L 0 ${H - 6} Z`}
        fill="var(--cc-amber)"
        fillOpacity="0.08"
      />
      <path
        d={path}
        fill="none"
        stroke="var(--cc-amber)"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx={lastX} cy={lastY} r="4" fill="var(--cc-amber)" />
      <text
        x={W - 8}
        y="14"
        textAnchor="end"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.1em"
        fill="var(--cc-ink-dim)"
      >
        p95 412ms
      </text>
      <text
        x="0"
        y="14"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.1em"
        fill="var(--cc-ink-dim)"
      >
        Billing.charge
      </text>
    </svg>
  );
};

// Logs preview — three lines, colored field-path token + level + message.
const LogsPreview: FC = () => (
  <div className="cc-ag-sees-logs">
    <div>
      <span className="lvl">12:04</span> <span className="err">err</span>{" "}
      <span className="field">@Billing.charge</span> timeout · 412ms
    </div>
    <div>
      <span className="lvl">12:03</span> <span className="lvl">info</span>{" "}
      <span className="field">@Catalog.products</span> ok · 38ms
    </div>
    <div>
      <span className="lvl">12:02</span> <span className="lvl">warn</span>{" "}
      <span className="field">@Shipping.quote</span> retry · 84ms
    </div>
    <div>
      <span className="lvl">12:01</span> <span className="lvl">info</span>{" "}
      <span className="field">@Ordering.create</span> ok · 47ms
    </div>
  </div>
);

// Tiny pub/sub topology — 3 publishers feeding 2 topics feeding 4 subs.
const MessagingTopology: FC = () => (
  <svg viewBox="0 0 260 110" width="100%" aria-hidden>
    <g {...STROKE} stroke="var(--cc-amber)" opacity="0.7">
      <line x1="20" y1="20" x2="100" y2="40" />
      <line x1="20" y1="55" x2="100" y2="55" />
      <line x1="20" y1="90" x2="100" y2="70" />
      <line x1="160" y1="40" x2="240" y2="20" />
      <line x1="160" y1="40" x2="240" y2="55" />
      <line x1="160" y1="70" x2="240" y2="55" />
      <line x1="160" y1="70" x2="240" y2="90" />
    </g>
    {/* publishers (left) */}
    {[20, 55, 90].map((y, i) => (
      <g key={`p-${i}`}>
        <circle
          cx="20"
          cy={y}
          r="5"
          fill="var(--cc-amber)"
          fillOpacity="0.18"
          stroke="var(--cc-amber)"
          strokeWidth="1.2"
        />
      </g>
    ))}
    {/* topics (mid) */}
    {[
      { y: 40, label: "orders" },
      { y: 70, label: "billing" },
    ].map((t) => (
      <g key={t.label}>
        <rect
          x="105"
          y={t.y - 8}
          width="50"
          height="16"
          rx="3"
          fill="rgba(247,186,100,0.1)"
          stroke="var(--cc-amber)"
          strokeWidth="1.2"
          strokeOpacity="0.6"
        />
        <text
          x="130"
          y={t.y + 4}
          textAnchor="middle"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="9"
          letterSpacing="0.08em"
          fill="var(--cc-amber)"
        >
          {t.label}
        </text>
      </g>
    ))}
    {/* subscribers (right) */}
    {[20, 55, 55, 90].map((y, i) => (
      <circle
        key={`s-${i}`}
        cx="240"
        cy={y}
        r="5"
        fill="var(--cc-ink)"
        fillOpacity="0.1"
        stroke="var(--cc-ink)"
        strokeOpacity="0.5"
        strokeWidth="1.2"
      />
    ))}
  </svg>
);

// API graph: type → field → resolver. A small horizontal flow.
const GraphPreview: FC = () => (
  <svg viewBox="0 0 260 110" width="100%" aria-hidden>
    <g {...STROKE} stroke="var(--cc-ink-faint)" opacity="0.7">
      <line x1="56" y1="55" x2="118" y2="55" />
      <line x1="174" y1="55" x2="232" y2="55" />
    </g>
    {/* type pill */}
    <rect
      x="6"
      y="40"
      width="50"
      height="30"
      rx="6"
      fill="rgba(120,140,220,0.08)"
      stroke="var(--cc-col-shi)"
      strokeOpacity="0.5"
      strokeWidth="1.2"
    />
    <text
      x="31"
      y="59"
      textAnchor="middle"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      fill="var(--cc-col-shi)"
    >
      Cart
    </text>
    {/* field pill */}
    <rect
      x="118"
      y="40"
      width="56"
      height="30"
      rx="6"
      fill="rgba(247,186,100,0.1)"
      stroke="var(--cc-amber)"
      strokeOpacity="0.6"
      strokeWidth="1.2"
    />
    <text
      x="146"
      y="59"
      textAnchor="middle"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      fill="var(--cc-amber)"
    >
      .total
    </text>
    {/* resolver pill */}
    <rect
      x="174"
      y="40"
      width="80"
      height="30"
      rx="6"
      fill="rgba(200,160,230,0.08)"
      stroke="var(--cc-col-usr)"
      strokeOpacity="0.5"
      strokeWidth="1.2"
    />
    <text
      x="214"
      y="59"
      textAnchor="middle"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      fill="var(--cc-col-usr)"
    >
      Billing
    </text>
    <text
      x="6"
      y="22"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="9"
      letterSpacing="0.16em"
      fill="var(--cc-ink-dim)"
      style={{ textTransform: "uppercase" }}
    >
      type
    </text>
    <text
      x="118"
      y="22"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="9"
      letterSpacing="0.16em"
      fill="var(--cc-ink-dim)"
      style={{ textTransform: "uppercase" }}
    >
      field
    </text>
    <text
      x="174"
      y="22"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="9"
      letterSpacing="0.16em"
      fill="var(--cc-ink-dim)"
      style={{ textTransform: "uppercase" }}
    >
      owner
    </text>
  </svg>
);

// Code references — tiny C# CQRS command stub.
const CodePreview: FC = () => (
  <div className="cc-ag-sees-code">
    <div>
      <span className="gutter">12</span>
      <span className="kw">public sealed record</span>{" "}
      <span className="ty">CancelOrder</span>(
    </div>
    <div>
      <span className="gutter">13</span>
      {"  "}
      <span className="ty">OrderId</span> id, <span className="ty">Reason</span>{" "}
      reason)
    </div>
    <div>
      <span className="gutter">14</span>
      {"  "}: <span className="ty">ICommand</span>;
    </div>
    <div>
      <span className="gutter">15</span>
      <span className="com">// emits OrderCancelled</span>
    </div>
  </div>
);

const RENDERERS: Record<AgentSeesKind, () => React.ReactElement> = {
  traces: () => <MiniTrace />,
  metrics: () => <Sparkline />,
  logs: () => <LogsPreview />,
  messaging: () => <MessagingTopology />,
  graph: () => <GraphPreview />,
  code: () => <CodePreview />,
};

export const WhatAgentSees: FC = () => {
  return (
    <Band variant="default" ariaLabel="Six surfaces, one MCP endpoint">
      <div className="cc-ag-band-inner">
        <div className="cc-section-label">
          <span className="num">04</span> What the agent sees
        </div>
        <div className="cc-ag-feature-header">
          <div className="eyebrow">One schema-typed surface</div>
          <h2 className="display">Six surfaces. One MCP endpoint.</h2>
          <p>
            Every signal a senior engineer would chase — distributed traces,
            metrics, logs, messaging topology, the API graph, and the source
            code itself — queryable from one schema-typed place.
          </p>
        </div>

        <div className="cc-ag-sees-grid">
          {AGENT_SEES_TILES.map((tile) => {
            const render = RENDERERS[tile.key];
            return (
              <div key={tile.key} className="cc-ag-sees-tile">
                <div className="eyebrow">{tile.eyebrow}</div>
                <h3>{tile.title}</h3>
                <p>{tile.body}</p>
                <div className="cc-ag-sees-viz">{render()}</div>
              </div>
            );
          })}
        </div>
      </div>
    </Band>
  );
};
