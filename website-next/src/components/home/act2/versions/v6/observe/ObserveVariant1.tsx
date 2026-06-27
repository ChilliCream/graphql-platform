"use client";

import { motion } from "motion/react";

interface ObserveVariant1Props {
  readonly className?: string;
}

/**
 * v6 "Production view" hook, variant 1: watch p99 cross the SLO line.
 *
 * Bespoke, one-off illustration (no shared v6 theme): a single operation tile for
 * the `checkout` query. A calm teal p99 sparkline holds a flat baseline, then
 * bends sharply up and breaches a dashed SLO threshold line. The instant it
 * crosses, the tail turns coral, a small coral ring marks the crossing point, the
 * `318 ms` value reads coral, and an amber `Investigating` pill flags the op top
 * right, so the thing that is hurting is already surfaced, not buried in a chart.
 *
 * Sole looping accent: a soft coral halo pulses around the breach point at the end
 * of the tail. Every value, the SLO line, and the curve are fully legible at rest,
 * and there is no layout shift.
 *
 * cc-* dark palette only; status colors encode real status (amber = the op is
 * under investigation, coral = the p99 in breach). One amber pill, one coral
 * curve, the rest teal and grey. Inline SVG id prefix "v6-observe-1-".
 */
const ID = "v6-observe-1-";

const C = {
  page: "#0b0f1a",
  accent: "#5eead4",
  coral: "#f0786a",
  slo: "rgba(245,241,234,0.30)",
  navLabel: "#62748e",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

// p99 latency sample (ms): a flat baseline near 155 ms that bends sharply up and
// breaches the 250 ms SLO, peaking at 318 ms (the value on the tile).
const SERIES: readonly number[] = [
  152, 158, 150, 161, 155, 163, 157, 168, 162, 178, 205, 248, 292, 318,
];

// Fixed value domain so the SLO line sits at a stable height regardless of data.
const DOMAIN_MIN = 120;
const DOMAIN_MAX = 340;
const SLO_VALUE = 250;

// Plot rectangle inside the 280 x 84 viewBox.
const PLOT = { left: 10, right: 270, top: 8, bottom: 74 } as const;

type Point = readonly [number, number];

function buildSloSparkline() {
  const n = SERIES.length;
  const round = (v: number) => Math.round(v * 10) / 10;
  const xOf = (i: number) =>
    PLOT.left + (i / (n - 1)) * (PLOT.right - PLOT.left);
  const yOf = (v: number) =>
    PLOT.bottom -
    ((v - DOMAIN_MIN) / (DOMAIN_MAX - DOMAIN_MIN)) * (PLOT.bottom - PLOT.top);

  const pts: Point[] = SERIES.map((v, i) => [xOf(i), yOf(v)]);
  const sloY = yOf(SLO_VALUE);

  // Single upward crossing: split the curve where it passes the SLO value.
  const crossIndex = SERIES.findIndex((v) => v > SLO_VALUE);
  const prev = crossIndex - 1;
  const t = (SLO_VALUE - SERIES[prev]) / (SERIES[crossIndex] - SERIES[prev]);
  const crossX = xOf(prev) + t * (xOf(crossIndex) - xOf(prev));
  const cross: Point = [crossX, sloY];

  const below: Point[] = [...pts.slice(0, crossIndex), cross];
  const above: Point[] = [cross, ...pts.slice(crossIndex)];

  const toLine = (p: readonly Point[]) =>
    p
      .map(([x, y], i) => `${i === 0 ? "M" : "L"}${round(x)} ${round(y)}`)
      .join(" ");
  const toArea = (p: readonly Point[]) =>
    `${toLine(p)} L${round(p[p.length - 1][0])} ${PLOT.bottom} L${round(p[0][0])} ${PLOT.bottom} Z`;

  return {
    belowLine: toLine(below),
    aboveLine: toLine(above),
    belowArea: toArea(below),
    aboveArea: toArea(above),
    sloY: round(sloY),
    cross: [round(cross[0]), round(cross[1])] as const,
    last: [round(pts[n - 1][0]), round(pts[n - 1][1])] as const,
  };
}

const SPARK = buildSloSparkline();

export function ObserveVariant1({ className }: ObserveVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Header: operation name + span-kind tag, amber Investigating pill. */}
        <div className="flex items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
            <span className="text-cc-heading font-mono text-sm font-semibold">
              checkout
            </span>
            <span className="border-cc-card-border text-cc-nav-label rounded border px-1.5 py-0.5 font-mono text-[0.6rem] tracking-[0.04em]">
              query
            </span>
          </div>
          <span className="border-cc-status-investigating/40 text-cc-status-investigating bg-cc-status-investigating/10 inline-flex shrink-0 items-center rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] font-medium whitespace-nowrap">
            Investigating
          </span>
        </div>

        {/* p99 label + the breaching value. */}
        <div className="mt-4 flex items-baseline justify-between">
          <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.12em] uppercase">
            p99 latency
          </span>
          <span
            className="font-mono text-sm font-semibold tabular-nums"
            style={{ color: C.coral }}
          >
            318 ms
          </span>
        </div>

        {/* The sparkline crossing the dashed SLO line. */}
        <div className="mt-2.5">
          <svg
            viewBox="0 0 280 84"
            width="100%"
            style={{ display: "block", overflow: "visible" }}
            role="img"
            aria-label="p99 latency holding near 155 ms, then climbing sharply across the 250 ms SLO line to 318 ms in breach"
          >
            <defs>
              <linearGradient
                id={`${ID}calm-fill`}
                x1="0"
                y1={PLOT.top}
                x2="0"
                y2={PLOT.bottom}
                gradientUnits="userSpaceOnUse"
              >
                <stop offset="0" stopColor={C.accent} stopOpacity="0.14" />
                <stop offset="1" stopColor={C.accent} stopOpacity="0" />
              </linearGradient>
              <linearGradient
                id={`${ID}breach-fill`}
                x1="0"
                y1={PLOT.top}
                x2="0"
                y2={PLOT.bottom}
                gradientUnits="userSpaceOnUse"
              >
                <stop offset="0" stopColor={C.coral} stopOpacity="0.26" />
                <stop offset="1" stopColor={C.coral} stopOpacity="0" />
              </linearGradient>
            </defs>

            {/* Area washes under the calm and breaching parts of the curve. */}
            <path d={SPARK.belowArea} fill={`url(#${ID}calm-fill)`} />
            <path d={SPARK.aboveArea} fill={`url(#${ID}breach-fill)`} />

            {/* Dashed SLO threshold line + its label. */}
            <line
              x1={PLOT.left - 2}
              y1={SPARK.sloY}
              x2={PLOT.right + 2}
              y2={SPARK.sloY}
              stroke={C.slo}
              strokeWidth="1"
              strokeDasharray="3 3"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={PLOT.left}
              y={SPARK.sloY - 5}
              fontFamily={C.mono}
              fontSize="8"
              letterSpacing="0.06em"
              fill={C.navLabel}
            >
              SLO 250 ms
            </text>

            {/* Calm baseline (teal) then the breaching tail (coral). */}
            <path
              d={SPARK.belowLine}
              fill="none"
              stroke={C.accent}
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            />
            <path
              d={SPARK.aboveLine}
              fill="none"
              stroke={C.coral}
              strokeWidth="1.7"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            />

            {/* Coral ring marking the exact crossing point on the SLO line. */}
            <circle
              cx={SPARK.cross[0]}
              cy={SPARK.cross[1]}
              r={2.4}
              fill={C.page}
              stroke={C.coral}
              strokeWidth="1.4"
              vectorEffect="non-scaling-stroke"
            />

            {/* Sole looping accent: a soft halo pulsing at the breach point. */}
            <motion.circle
              cx={SPARK.last[0]}
              cy={SPARK.last[1]}
              fill="none"
              stroke={C.coral}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              initial={{ r: 4, opacity: 0.5 }}
              animate={{ r: [4, 11, 4], opacity: [0.5, 0, 0.5] }}
              transition={{
                duration: 2.6,
                repeat: Infinity,
                ease: "easeInOut",
              }}
            />
            <circle
              cx={SPARK.last[0]}
              cy={SPARK.last[1]}
              r={2.8}
              fill={C.coral}
            />
          </svg>
        </div>

        {/* Footer: quantified breach + how fast it surfaced. */}
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
          <span className="text-cc-ink-dim font-mono text-[0.62rem]">
            +68 ms over SLO
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.62rem]">
            flagged 14s ago
          </span>
        </div>
      </div>
    </div>
  );
}
