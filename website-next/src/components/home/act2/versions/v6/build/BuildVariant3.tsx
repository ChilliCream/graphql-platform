"use client";

import { motion } from "motion/react";

/**
 * v6 "Build" hook, variant 3: a filling build-status bar that resolves to a check.
 *
 * Bespoke, one-off illustration (no shared v6 theme): a short horizontal track of
 * compile-stage ticks whose teal progress bar fills left to right across the four
 * stages the build reconciles (schema, batching, client, contract) and resolves
 * into a single solid teal check disc. Beneath it sits one mono terminal line,
 * `build succeeded`, carrying the real whole-build elapsed total (1.8s). The lone
 * looping accent is a slow halo pulse around the disc; the disc, bar, and label
 * are static, so the resting and first frame are fully legible.
 *
 * cc-* dark palette only, thin 1px strokes, generous negative space. Every svg id
 * is prefixed "v6-build-3-".
 */

interface BuildVariant3Props {
  readonly className?: string;
}

const C = {
  page: "#0b0f1a",
  surface: "#0c1322",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  inkFaint: "rgba(245,241,234,0.16)",
  border: "rgba(245,241,234,0.12)",
  accent: "#5eead4",
  accentHover: "#99f6e4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v6-build-3-";

// The four compile stages the build reconciles, left to right. Each is one filled
// segment of the progress bar; the gaps between them read as the stage ticks.
const STAGES: readonly {
  readonly label: string;
  readonly x: number;
  readonly w: number;
  readonly cx: number;
}[] = [
  { label: "schema", x: 18, w: 43, cx: 39.5 },
  { label: "batching", x: 65, w: 43, cx: 86.5 },
  { label: "client", x: 112, w: 43, cx: 133.5 },
  { label: "contract", x: 159, w: 43, cx: 180.5 },
];

// Stage tick marks sit just above each gap between filled segments.
const TICKS: readonly number[] = [63, 110, 157];

export function BuildVariant3({ className }: BuildVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build status
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 280 84"
            width="100%"
            aria-hidden="true"
            style={{ display: "block", overflow: "visible" }}
          >
            <defs>
              {/* Left-to-right brightening: the fill resolving toward the check. */}
              <linearGradient
                id={`${ID}fill`}
                x1="18"
                y1="0"
                x2="202"
                y2="0"
                gradientUnits="userSpaceOnUse"
              >
                <stop offset="0" stopColor={C.accent} stopOpacity="0.78" />
                <stop offset="1" stopColor={C.accentHover} stopOpacity="1" />
              </linearGradient>

              {/* Teal chevron that lands the bar into the check disc. */}
              <marker
                id={`${ID}tip`}
                markerWidth="6"
                markerHeight="6"
                refX="4.4"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0 0.6 L5 3 L0 5.4"
                  fill="none"
                  stroke={C.accent}
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>
            </defs>

            {/* Stage ticks above the track. */}
            {TICKS.map((tx) => (
              <line
                key={`${ID}tick-${tx}`}
                x1={tx}
                y1={32}
                x2={tx}
                y2={37}
                stroke={C.inkFaint}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
                strokeLinecap="round"
              />
            ))}

            {/* Track pill. */}
            <rect
              x={14}
              y={38}
              width={194}
              height={16}
              rx={8}
              fill={C.surface}
              stroke={C.border}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />

            {/* Filled stage segments (progress complete) + their labels. */}
            {STAGES.map((stage) => (
              <g key={`${ID}stage-${stage.label}`}>
                <rect
                  x={stage.x}
                  y={42}
                  width={stage.w}
                  height={8}
                  rx={3}
                  fill={`url(#${ID}fill)`}
                />
                <text
                  x={stage.cx}
                  y={68}
                  textAnchor="middle"
                  fontFamily={C.mono}
                  fontSize="7.5"
                  letterSpacing="0.04em"
                  fill={C.navLabel}
                >
                  {stage.label}
                </text>
              </g>
            ))}

            {/* Bar resolves into the disc. */}
            <line
              x1={209}
              y1={46}
              x2={222}
              y2={46}
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              markerEnd={`url(#${ID}tip)`}
            />

            {/* Single looping accent: a slow halo pulse around the resolved disc. */}
            <motion.circle
              cx={240}
              cy={46}
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              initial={{ r: 16, opacity: 0.4 }}
              animate={{ r: [16, 23, 16], opacity: [0.4, 0, 0.4] }}
              transition={{ duration: 2.8, repeat: Infinity, ease: "easeInOut" }}
            />

            {/* The check disc. */}
            <circle cx={240} cy={46} r={15} fill={C.accent} />
            <path
              d="M233 46.5 L238 51 L247 41.5"
              fill="none"
              stroke={C.page}
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            />
          </svg>
        </div>

        {/* The settled terminal line, carrying the whole-build elapsed total. */}
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-4">
          <span className="font-mono text-xs">
            <span className="text-cc-nav-label">$ </span>
            <span className="text-cc-accent">build succeeded</span>
          </span>
          <span className="text-cc-ink-dim font-mono text-xs">1.8s</span>
        </div>
      </div>
    </div>
  );
}
