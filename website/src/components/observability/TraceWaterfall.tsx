"use client";

import React, { FC } from "react";

// Trace waterfall mock. Configurable spans + colors. The brief says reuse this
// in the hero, Section 02, and Section 08; the only thing that varies is the
// span list, the highlighted index, and an optional annotation rendered to
// the right of one bar (never inside it).
//
// Two visual modes share one component:
//   - The schematic mode (DEFAULT_TRACE / AGENT_TRACE / REPLAY_*) keeps the
//     per-service color tokens that Act3 / agents-page consumers depend on.
//   - The dense mode (DENSE_TRACE) renders 12-15 spans with up to 2 levels of
//     indentation, quartile gridlines, and a single accent gradient for the
//     non-outlier rows. Slow / error / cache spans break out of the gradient.
//
// `monoLane` enables the dense visual treatment without altering the existing
// API: span items can carry an optional `depth` (0|1|2) for indentation and an
// optional `chip` ("error" | "cache" | "retry") that renders inside the bar.

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
  readonly depth?: 0 | 1 | 2;
  readonly chip?: "error" | "cache" | "retry";
}

interface TraceWaterfallProps {
  readonly spans: readonly TraceSpan[];
  readonly height?: number;
  readonly totalLabel?: string;
  readonly axisMs?: readonly number[];
  readonly compact?: boolean;
  /**
   * Dense single-hue lane: gridlines at quartile points, indentation for nested
   * spans, smaller monospace eyebrow per span. Used by Section 02 and the hero
   * composite. Defaults to false so existing call sites are unchanged.
   */
  readonly monoLane?: boolean;
}

export const TraceWaterfall: FC<TraceWaterfallProps> = ({
  spans,
  height,
  totalLabel = "0ms · 600ms",
  axisMs = [0, 150, 300, 450, 600],
  compact = false,
  monoLane = false,
}) => {
  // Layout: [name col][bar col][meta col]. The bar col is the only one that
  // scales with span.start..span.end; the others are fixed.
  const PAD_X = 16;
  const NAME_W = monoLane ? 188 : 168;
  const META_W = 72;
  const ROW_H = monoLane ? 22 : compact ? 30 : 38;
  const BAR_H = monoLane ? 10 : compact ? 14 : 18;
  const HEADER_H = 26;
  const FOOTER_H = 26;
  const W = 720;
  const H = height ?? HEADER_H + spans.length * ROW_H + FOOTER_H;
  const BAR_X = PAD_X + NAME_W;
  const BAR_W = W - BAR_X - META_W - PAD_X;

  const baseFill = monoLane ? "var(--cc-accent, var(--cc-ink))" : null;
  const dimFill = monoLane ? "var(--cc-ink)" : null;

  return (
    <svg
      className="cc-trace-waterfall"
      viewBox={`0 0 ${W} ${H}`}
      preserveAspectRatio="xMidYMid meet"
      role="img"
      aria-label="Trace waterfall"
    >
      {/* axis ticks */}
      <g
        stroke="var(--cc-ink-faint)"
        strokeWidth="1"
        strokeDasharray={monoLane ? undefined : "2 4"}
        opacity={monoLane ? 0.35 : 0.5}
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
          const isError = s.variant === "error" || s.chip === "error";
          const isHighlight = s.variant === "highlight";
          const isCache = s.chip === "cache";
          const depthIndent = monoLane && s.depth ? s.depth * 14 : 0;

          // Color resolution: in monoLane mode, default to a single accent
          // family and only break out for outliers. In schematic mode, keep
          // the per-service color the consumer passed in.
          let fillColor = s.color;
          let strokeColor = s.color;
          let fillOpacity = isError ? 0.85 : isHighlight ? 0.95 : 0.7;
          if (monoLane) {
            if (isError) {
              fillColor = "var(--cc-col-cat)";
              strokeColor = "var(--cc-col-cat)";
              fillOpacity = 0.78;
            } else if (isCache) {
              fillColor = "var(--cc-col-shi)";
              strokeColor = "var(--cc-col-shi)";
              fillOpacity = 0.32;
            } else if (isHighlight) {
              fillColor = baseFill ?? s.color;
              strokeColor = baseFill ?? s.color;
              fillOpacity = 0.92;
            } else {
              // Single-hue gradient body, opacity varies subtly with depth so
              // nested spans recede.
              fillColor = dimFill ?? s.color;
              strokeColor = dimFill ?? s.color;
              fillOpacity = 0.36 - (s.depth ?? 0) * 0.04;
            }
          }

          const labelColor = isError
            ? "var(--cc-col-cat)"
            : monoLane
            ? "var(--cc-ink)"
            : "var(--cc-ink)";
          const meta = isError ? "var(--cc-col-cat)" : "var(--cc-ink)";

          return (
            <g key={s.key}>
              {/* row guide (only in schematic mode; gridlines do this in mono) */}
              {!monoLane && (
                <line
                  x1={BAR_X}
                  y1={y + ROW_H - 6}
                  x2={BAR_X + BAR_W}
                  y2={y + ROW_H - 6}
                  stroke="var(--cc-ink-faint)"
                  strokeWidth="1"
                  opacity="0.25"
                />
              )}
              {/* name */}
              <text
                x={PAD_X + depthIndent}
                y={y + ROW_H / 2 + (monoLane ? 3 : 4)}
                fontFamily="var(--cc-font-mono), monospace"
                fontSize={monoLane ? 10 : 12}
                fill={labelColor}
                opacity={monoLane && (s.depth ?? 0) > 0 ? 0.78 : 1}
              >
                {s.name}
              </text>
              {s.hops !== undefined && !monoLane && (
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
                rx={monoLane ? 2 : 3}
                fill={fillColor}
                fillOpacity={fillOpacity}
                stroke={strokeColor}
                strokeWidth={isHighlight ? 1.6 : monoLane ? 0 : 1}
                strokeOpacity={isHighlight ? 1 : 0.5}
              />
              {/* in-bar chip for cache hit / retry, monoLane only */}
              {monoLane && s.chip && s.chip !== "error" && (
                <g>
                  <rect
                    x={barX + 2}
                    y={y + (ROW_H - BAR_H) / 2 - 1}
                    width={s.chip === "cache" ? 26 : 24}
                    height={BAR_H + 2}
                    rx={2}
                    fill="var(--cc-col-shi)"
                    fillOpacity={0.18}
                    stroke="var(--cc-col-shi)"
                    strokeOpacity={0.55}
                    strokeWidth={0.8}
                  />
                  <text
                    x={barX + (s.chip === "cache" ? 15 : 14)}
                    y={y + ROW_H / 2 + 3}
                    textAnchor="middle"
                    fontFamily="var(--cc-font-mono), monospace"
                    fontSize="8"
                    letterSpacing="0.14em"
                    fill="var(--cc-col-shi)"
                  >
                    {s.chip === "cache" ? "HIT" : "RTRY"}
                  </text>
                </g>
              )}
              {isHighlight && !monoLane && (
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
                y={y + ROW_H / 2 + (monoLane ? 3 : 4)}
                textAnchor="end"
                fontFamily="var(--cc-font-mono), monospace"
                fontSize={monoLane ? 10 : 11}
                letterSpacing="0.06em"
                fill={meta}
                opacity={monoLane && !isError && !isHighlight ? 0.78 : 1}
              >
                {s.p95}
              </text>
              {/* annotation (right of bar, never inside) */}
              {s.annotation && !monoLane && (
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

// Default span set used by the hero collage (legacy) and any consumer that
// wants the original 5-row schematic view. Kept verbatim so the agents page
// and other consumers continue to render unchanged.
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

// Dense span set for the redesigned Section 02 + hero composite. 14 rows with
// gateway > service > resolver indentation, gridline-aware spacing, single hue
// for non-outliers. Billing.charge is the slow span, billing.db.payments is
// the cache hit, payments.gateway.charge is the in-error external call.
export const DENSE_TRACE: readonly TraceSpan[] = [
  {
    key: "gateway",
    name: "gateway · cart-checkout",
    p95: "412ms",
    start: 0.0,
    end: 0.99,
    color: "var(--cc-ink)",
    depth: 0,
  },
  {
    key: "auth",
    name: "auth.session.verify",
    p95: "8ms",
    start: 0.01,
    end: 0.03,
    color: "var(--cc-ink)",
    depth: 1,
  },
  {
    key: "catalog",
    name: "Catalog.products",
    p95: "38ms",
    start: 0.04,
    end: 0.14,
    color: "var(--cc-ink)",
    depth: 1,
  },
  {
    key: "catalog-db",
    name: "catalog.db.products",
    p95: "12ms",
    start: 0.05,
    end: 0.09,
    color: "var(--cc-ink)",
    depth: 2,
  },
  {
    key: "catalog-cache",
    name: "catalog.cache.lookup",
    p95: "1ms",
    start: 0.09,
    end: 0.11,
    color: "var(--cc-ink)",
    depth: 2,
    chip: "cache",
  },
  {
    key: "ordering",
    name: "Ordering.create",
    p95: "47ms",
    start: 0.16,
    end: 0.28,
    color: "var(--cc-ink)",
    depth: 1,
  },
  {
    key: "ordering-db",
    name: "ordering.db.insert",
    p95: "22ms",
    start: 0.18,
    end: 0.24,
    color: "var(--cc-ink)",
    depth: 2,
  },
  {
    key: "shipping",
    name: "Shipping.quote",
    p95: "52ms",
    start: 0.28,
    end: 0.42,
    color: "var(--cc-ink)",
    depth: 1,
  },
  {
    key: "shipping-cache",
    name: "shipping.cache.rates",
    p95: "1ms",
    start: 0.29,
    end: 0.31,
    color: "var(--cc-ink)",
    depth: 2,
    chip: "cache",
  },
  {
    key: "users",
    name: "Users.preferences",
    p95: "14ms",
    start: 0.34,
    end: 0.4,
    color: "var(--cc-ink)",
    depth: 1,
  },
  {
    key: "billing",
    name: "Billing.charge",
    p95: "302ms",
    start: 0.42,
    end: 0.95,
    color: "var(--cc-ink)",
    depth: 1,
    variant: "highlight",
  },
  {
    key: "billing-db",
    name: "billing.db.customer",
    p95: "9ms",
    start: 0.43,
    end: 0.46,
    color: "var(--cc-ink)",
    depth: 2,
  },
  {
    key: "payments",
    name: "payments.gateway.charge",
    p95: "FAILED",
    start: 0.46,
    end: 0.94,
    color: "var(--cc-ink)",
    depth: 2,
    variant: "error",
    chip: "error",
  },
  {
    key: "audit",
    name: "audit.log.write",
    p95: "4ms",
    start: 0.95,
    end: 0.98,
    color: "var(--cc-ink)",
    depth: 1,
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
