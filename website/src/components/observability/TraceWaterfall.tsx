"use client";

import React, { FC } from "react";

// Trace waterfall mock. Configurable spans + colors. The brief says reuse this
// in the hero, Section 02, and Section 08; the only thing that varies is the
// span list, the highlighted index, and an optional annotation rendered to
// the right of one bar (never inside it). Colors come from
// DESKTOP_SERVICES via the --cc-col-* CSS tokens so the waterfall visually
// rhymes with Act3.

export interface TraceSpan {
  readonly key: string;
  readonly name: string;
  readonly hops?: number;
  readonly p95: string;
  readonly start: number; // 0..1
  readonly end: number; // 0..1
  readonly color: string; // CSS color (token reference)
  readonly variant?: "ok" | "error" | "highlight";
  readonly annotation?: string;
}

interface TraceWaterfallProps {
  readonly spans: readonly TraceSpan[];
  readonly height?: number;
  readonly totalLabel?: string;
  readonly axisMs?: readonly number[];
  readonly compact?: boolean;
}

export const TraceWaterfall: FC<TraceWaterfallProps> = ({
  spans,
  height,
  totalLabel = "0ms · 600ms",
  axisMs = [0, 150, 300, 450, 600],
  compact = false,
}) => {
  // Layout: [name col][bar col][meta col]. The bar col is the only one that
  // scales with span.start..span.end; the others are fixed.
  const PAD_X = 16;
  const NAME_W = 168;
  const META_W = 72;
  const ROW_H = compact ? 30 : 38;
  const BAR_H = compact ? 14 : 18;
  const HEADER_H = 26;
  const FOOTER_H = 26;
  const W = 720;
  const H = height ?? HEADER_H + spans.length * ROW_H + FOOTER_H;
  const BAR_X = PAD_X + NAME_W;
  const BAR_W = W - BAR_X - META_W - PAD_X;

  return (
    <svg
      className="cc-trace-waterfall"
      viewBox={`0 0 ${W} ${H}`}
      preserveAspectRatio="xMidYMid meet"
      role="img"
      aria-label="Trace waterfall: gateway and four service spans"
    >
      {/* axis ticks */}
      <g
        stroke="var(--cc-ink-faint)"
        strokeWidth="1"
        strokeDasharray="2 4"
        opacity="0.5"
      >
        {axisMs.map((_, i) => {
          const x = BAR_X + (BAR_W * i) / (axisMs.length - 1);
          return (
            <line
              key={i}
              x1={x}
              y1={HEADER_H - 4}
              x2={x}
              y2={H - FOOTER_H + 4}
            />
          );
        })}
      </g>

      {/* axis labels (top) */}
      <g
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.08em"
      >
        {axisMs.map((ms, i) => {
          const x = BAR_X + (BAR_W * i) / (axisMs.length - 1);
          const anchor =
            i === 0 ? "start" : i === axisMs.length - 1 ? "end" : "middle";
          return (
            <text key={i} x={x} y={14} textAnchor={anchor}>
              {ms}ms
            </text>
          );
        })}
      </g>

      {/* spans */}
      <g>
        {spans.map((s, i) => {
          const y = HEADER_H + i * ROW_H;
          const barX = BAR_X + BAR_W * s.start;
          const barW = Math.max(2, BAR_W * (s.end - s.start));
          const isError = s.variant === "error";
          const isHighlight = s.variant === "highlight";
          const fillOpacity = isError ? 0.85 : isHighlight ? 0.95 : 0.7;
          const strokeColor = isError ? "var(--cc-col-cat)" : s.color;
          return (
            <g key={s.key}>
              {/* row guide */}
              <line
                x1={BAR_X}
                y1={y + ROW_H - 6}
                x2={BAR_X + BAR_W}
                y2={y + ROW_H - 6}
                stroke="var(--cc-ink-faint)"
                strokeWidth="1"
                opacity="0.25"
              />
              {/* name */}
              <text
                x={PAD_X}
                y={y + ROW_H / 2 + 4}
                fontFamily="var(--cc-font-mono), monospace"
                fontSize="12"
                fill={isError ? "var(--cc-col-cat)" : "var(--cc-ink)"}
              >
                {s.name}
              </text>
              {s.hops !== undefined && (
                <text
                  x={PAD_X}
                  y={y + ROW_H / 2 + 17}
                  fontFamily="var(--cc-font-mono), monospace"
                  fontSize="9"
                  letterSpacing="0.1em"
                  fill="var(--cc-ink-dim)"
                >
                  {s.hops} HOPS
                </text>
              )}
              {/* bar */}
              <rect
                x={barX}
                y={y + (ROW_H - BAR_H) / 2}
                width={barW}
                height={BAR_H}
                rx={3}
                fill={s.color}
                fillOpacity={fillOpacity}
                stroke={strokeColor}
                strokeWidth={isHighlight ? 1.6 : 1}
                strokeOpacity={isHighlight ? 1 : 0.5}
              />
              {isHighlight && (
                <rect
                  x={barX - 3}
                  y={y + (ROW_H - BAR_H) / 2 - 3}
                  width={barW + 6}
                  height={BAR_H + 6}
                  rx={5}
                  fill="none"
                  stroke="var(--cc-ink)"
                  strokeWidth="1"
                  strokeDasharray="3 3"
                  opacity="0.6"
                />
              )}
              {/* p95 */}
              <text
                x={W - PAD_X}
                y={y + ROW_H / 2 + 4}
                textAnchor="end"
                fontFamily="var(--cc-font-mono), monospace"
                fontSize="11"
                letterSpacing="0.06em"
                fill={isError ? "var(--cc-col-cat)" : "var(--cc-ink)"}
              >
                {s.p95}
              </text>
              {/* annotation (right of bar, never inside) */}
              {s.annotation && (
                <g>
                  <line
                    x1={barX + barW + 4}
                    y1={y + ROW_H / 2}
                    x2={barX + barW + 16}
                    y2={y + ROW_H / 2}
                    stroke="var(--cc-col-cat)"
                    strokeWidth="1.2"
                  />
                  <text
                    x={barX + barW + 20}
                    y={y + ROW_H / 2 + 4}
                    fontFamily="var(--cc-font-mono), monospace"
                    fontSize="10"
                    letterSpacing="0.04em"
                    fill="var(--cc-col-cat)"
                  >
                    {s.annotation}
                  </text>
                </g>
              )}
            </g>
          );
        })}
      </g>

      {/* footer label */}
      <text
        x={PAD_X}
        y={H - 8}
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.14em"
        fill="var(--cc-ink-dim)"
      >
        {totalLabel}
      </text>
    </svg>
  );
};

// Default span set used by the hero collage and Section 02. One gateway span
// on top, four owning service spans cascading below, Billing annotated as
// the slow leg. Colors mirror DESKTOP_SERVICES exactly.
export const DEFAULT_TRACE: readonly TraceSpan[] = [
  {
    key: "gateway",
    name: "gateway · cart-checkout",
    hops: 4,
    p95: "412ms",
    start: 0.0,
    end: 0.92,
    color: "var(--cc-ink)",
  },
  {
    key: "catalog",
    name: "Catalog.products",
    p95: "38ms",
    start: 0.04,
    end: 0.14,
    color: "var(--cc-col-cat)",
  },
  {
    key: "billing",
    name: "Billing.charge",
    p95: "302ms",
    start: 0.16,
    end: 0.85,
    color: "var(--cc-col-bil)",
    variant: "error",
    annotation: "↑ 412ms · timeout",
  },
  {
    key: "ordering",
    name: "Ordering.create",
    p95: "47ms",
    start: 0.2,
    end: 0.32,
    color: "var(--cc-col-ord)",
  },
  {
    key: "shipping",
    name: "Shipping.quote",
    p95: "52ms",
    start: 0.34,
    end: 0.48,
    color: "var(--cc-col-shi)",
  },
];

// Span set used by the agent panel: same shape, but Billing.charge is the
// highlighted span the agent surfaced.
export const AGENT_TRACE: readonly TraceSpan[] = [
  {
    key: "gateway",
    name: "gateway · cart-checkout",
    hops: 4,
    p95: "412ms",
    start: 0.0,
    end: 0.92,
    color: "var(--cc-ink)",
  },
  {
    key: "catalog",
    name: "Catalog.products",
    p95: "38ms",
    start: 0.04,
    end: 0.14,
    color: "var(--cc-col-cat)",
  },
  {
    key: "billing",
    name: "Billing.charge",
    p95: "302ms",
    start: 0.16,
    end: 0.85,
    color: "var(--cc-col-bil)",
    variant: "highlight",
    annotation: "← slowest in window",
  },
  {
    key: "ordering",
    name: "Ordering.create",
    p95: "47ms",
    start: 0.2,
    end: 0.32,
    color: "var(--cc-col-ord)",
  },
  {
    key: "shipping",
    name: "Shipping.quote",
    p95: "52ms",
    start: 0.34,
    end: 0.48,
    color: "var(--cc-col-shi)",
  },
];

// Replay panel uses a slimmer waterfall (no hops, compact rows).
export const REPLAY_PROD_TRACE: readonly TraceSpan[] = [
  {
    key: "gateway",
    name: "gateway",
    p95: "412ms",
    start: 0.0,
    end: 0.92,
    color: "var(--cc-ink)",
  },
  {
    key: "catalog",
    name: "Catalog.products",
    p95: "38ms",
    start: 0.04,
    end: 0.14,
    color: "var(--cc-col-cat)",
  },
  {
    key: "billing",
    name: "Billing.charge",
    p95: "FAILED",
    start: 0.16,
    end: 0.9,
    color: "var(--cc-col-bil)",
    variant: "error",
    annotation: "timeout",
  },
];

export const REPLAY_STAGING_TRACE: readonly TraceSpan[] = [
  {
    key: "gateway",
    name: "gateway",
    p95: "87ms",
    start: 0.0,
    end: 0.18,
    color: "var(--cc-ink)",
  },
  {
    key: "catalog",
    name: "Catalog.products",
    p95: "31ms",
    start: 0.02,
    end: 0.08,
    color: "var(--cc-col-cat)",
  },
  {
    key: "billing",
    name: "Billing.charge",
    p95: "44ms",
    start: 0.08,
    end: 0.16,
    color: "var(--cc-col-bil)",
  },
];
