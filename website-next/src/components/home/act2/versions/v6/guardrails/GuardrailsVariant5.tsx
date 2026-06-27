"use client";

import { motion } from "motion/react";

/**
 * v6 "Release safety" hook illustration (variant 5): the release timeline gate.
 *
 * Bespoke, one-off design (no shared v6 theme). A vertical version timeline:
 * each published schema version lands as a solid dot on a rail (v12, v13), green
 * and accounted for. Below a thin review gate sits one DASHED, semi-transparent
 * ghost node (v14), held pending review and marked by a single amber dot that
 * gently pulses while it waits. A footer names the flag that caught it. Nothing
 * risky promotes itself.
 *
 * The one looping accent is the amber pulse ring on the held v14 node; every
 * label, tag, and numeral is static so the resting and first frame read fully.
 * cc-* dark palette only, thin 1px strokes, generous negative space. Every SVG
 * id is prefixed `v6-guardrails-5-`.
 */

interface GuardrailsVariant5Props {
  readonly className?: string;
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';
const HEADING = '"Josefin Sans", Futura, sans-serif';

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, status hues. */
const C = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  dim: "rgba(245, 241, 234, 0.62)",
  eyebrow: "#62748e",
  border: "rgba(245, 241, 234, 0.12)",
  accent: "#5eead4",
  healthy: "#34d399",
  amber: "#fbbf24",
} as const;

const RAIL_X = 22;
const CARD_X = 40;
const CARD_W = 266;

interface PublishedNode {
  readonly v: string;
  readonly tag: string;
  readonly cy: number;
}

// Ascending schema versions landing on the rail. Tags reuse the registry sample
// from the sibling cell; v14 carries the dev build held before it reaches prod.
const PUBLISHED: readonly PublishedNode[] = [
  { v: "v12", tag: "eshops@2274", cy: 43 },
  { v: "v13", tag: "eshops@2288", cy: 87 },
];

const PENDING = { v: "v14", tag: "eshops@2291", cy: 150 } as const;

const GATE_Y = 118;

export function GuardrailsVariant5({ className }: GuardrailsVariant5Props) {
  return (
    <div
      className={[
        "mx-auto flex w-full max-w-[320px] justify-center select-none",
        className ?? "",
      ].join(" ")}
    >
      <svg
        viewBox="0 0 320 200"
        role="img"
        aria-label="Release timeline. Published schema versions v12 and v13 land as solid green dots on a rail. Below a review gate, version v14 (eshops@2291) is held as a dashed amber pending node because Product.rating was removed, a breaking change."
        style={{ width: "100%", height: "auto", display: "block" }}
      >
        {/* eyebrows: what this is, and the system that holds it. */}
        <text
          x={14}
          y={14}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.16em"
          fill={C.eyebrow}
        >
          RELEASE TIMELINE
        </text>
        <text
          x={306}
          y={14}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.16em"
          fill={C.eyebrow}
          textAnchor="end"
        >
          SCHEMA REGISTRY
        </text>

        {/* the rail: a faint stub of earlier history, solid through the
            published span, dashed below the gate where v14 waits. */}
        <line
          x1={RAIL_X}
          y1={22}
          x2={RAIL_X}
          y2={36}
          stroke={C.eyebrow}
          strokeWidth={1.2}
          strokeDasharray="2 3"
          opacity={0.4}
        />
        <line
          x1={RAIL_X}
          y1={36}
          x2={RAIL_X}
          y2={GATE_Y}
          stroke={C.border}
          strokeWidth={1.4}
        />
        <line
          x1={RAIL_X}
          y1={GATE_Y}
          x2={RAIL_X}
          y2={PENDING.cy}
          stroke={C.eyebrow}
          strokeWidth={1.4}
          strokeDasharray="3 3"
          opacity={0.7}
        />

        {/* published versions: solid dot on the rail, a connector, and a card
            carrying the version numeral, its tag, and a published chip. */}
        {PUBLISHED.map((n) => (
          <g key={n.v}>
            <line
              x1={28}
              y1={n.cy}
              x2={CARD_X}
              y2={n.cy}
              stroke={C.border}
              strokeWidth={1}
            />
            <circle
              cx={RAIL_X}
              cy={n.cy}
              r={5.5}
              fill={C.surface}
              stroke={C.healthy}
              strokeWidth={1.4}
            />
            <circle cx={RAIL_X} cy={n.cy} r={2.4} fill={C.healthy} />

            <rect
              x={CARD_X}
              y={n.cy - 17}
              width={CARD_W}
              height={34}
              rx={8}
              fill={C.surface}
              stroke={C.border}
              strokeWidth={1}
            />
            <text
              x={54}
              y={n.cy + 5}
              fontFamily={HEADING}
              fontSize={16}
              fontWeight={600}
              fill={C.heading}
            >
              {n.v}
            </text>
            <text
              x={89}
              y={n.cy + 3.5}
              fontFamily={MONO}
              fontSize={9.5}
              fill={C.dim}
            >
              {n.tag}
            </text>

            <rect
              x={224}
              y={n.cy - 9}
              width={70}
              height={18}
              rx={9}
              fill="rgba(52, 211, 153, 0.08)"
              stroke="rgba(52, 211, 153, 0.32)"
              strokeWidth={1}
            />
            <text
              x={259}
              y={n.cy + 3}
              fontFamily={MONO}
              fontSize={8.5}
              letterSpacing="0.04em"
              fill={C.healthy}
              textAnchor="middle"
            >
              published
            </text>
          </g>
        ))}

        {/* the review gate: a thin line the timeline must clear, labelled, with
            a count of what it is currently holding back. */}
        <line
          x1={6}
          y1={GATE_Y}
          x2={314}
          y2={GATE_Y}
          stroke={C.border}
          strokeWidth={1}
        />
        <text
          x={14}
          y={GATE_Y - 6}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.16em"
          fill={C.eyebrow}
        >
          REVIEW GATE
        </text>
        <text
          x={306}
          y={GATE_Y - 6}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.12em"
          fill={C.dim}
          textAnchor="end"
        >
          1 release held
        </text>

        {/* held release: a dashed, dimmed ghost node behind the gate, marked by
            the single amber pending dot with a slow pulse while it waits. */}
        <line
          x1={28}
          y1={PENDING.cy}
          x2={CARD_X}
          y2={PENDING.cy}
          stroke={C.border}
          strokeWidth={1}
          strokeDasharray="2 2"
        />
        <motion.circle
          cx={RAIL_X}
          cy={PENDING.cy}
          fill="none"
          stroke={C.amber}
          strokeWidth={1}
          vectorEffect="non-scaling-stroke"
          initial={{ r: 5.5, opacity: 0.5 }}
          animate={{ r: [5.5, 10, 5.5], opacity: [0.5, 0, 0.5] }}
          transition={{ duration: 2.6, repeat: Infinity, ease: "easeInOut" }}
        />
        <circle
          cx={RAIL_X}
          cy={PENDING.cy}
          r={5.5}
          fill={C.surface}
          stroke={C.amber}
          strokeWidth={1.4}
          strokeDasharray="2.2 2.2"
        />
        <circle cx={RAIL_X} cy={PENDING.cy} r={2.4} fill={C.amber} />

        <rect
          x={CARD_X}
          y={PENDING.cy - 18}
          width={CARD_W}
          height={36}
          rx={8}
          fill="none"
          stroke={C.border}
          strokeWidth={1}
          strokeDasharray="4 3"
        />
        <text
          x={54}
          y={PENDING.cy + 5}
          fontFamily={HEADING}
          fontSize={16}
          fontWeight={600}
          fill={C.dim}
        >
          {PENDING.v}
        </text>
        <text
          x={89}
          y={PENDING.cy + 3.5}
          fontFamily={MONO}
          fontSize={9.5}
          fill={C.eyebrow}
        >
          {PENDING.tag}
        </text>
        <rect
          x={222}
          y={PENDING.cy - 9}
          width={72}
          height={18}
          rx={9}
          fill="rgba(251, 191, 36, 0.10)"
          stroke="rgba(251, 191, 36, 0.40)"
          strokeWidth={1}
        />
        <text
          x={258}
          y={PENDING.cy + 3}
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.04em"
          fill={C.amber}
          textAnchor="middle"
        >
          pending
        </text>

        {/* footer: the flag the registry caught, the reason v14 is held. */}
        <circle cx={14} cy={184} r={2.4} fill={C.amber} />
        <text x={24} y={187} fontFamily={MONO} fontSize={9.5}>
          <tspan fill={C.accent}>Product.rating</tspan>
          <tspan fill={C.dim}> removed - breaking change</tspan>
        </text>
      </svg>
    </div>
  );
}
