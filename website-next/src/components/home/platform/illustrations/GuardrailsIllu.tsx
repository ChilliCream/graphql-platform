"use client";

import { motion } from "motion/react";

/**
 * Section-grade "Release safety" illustration: the schema evolution timeline.
 *
 * A horizontal registry rail carries published schema versions as solid teal
 * nodes (v12, v13), each tagged and marked published. The newest revision, v14,
 * is a dashed, dimmed ghost node held to the right of a thin review gate, marked
 * by a single amber pending dot that slowly pulses while it waits. A compact diff
 * panel shows why it is held: one additive line classified SAFE (green) and one
 * removal classified BREAKING (coral). Nothing risky promotes itself.
 *
 * The only looping accent is the amber pulse ring on the held v14 node; every
 * label, tag, numeral, and chip is static so the resting and first frame read
 * fully. cc-* dark palette only, thin 1px strokes, generous negative space.
 * Every SVG id is prefixed `illu-guardrails-`.
 */

interface GuardrailsIlluProps {
  readonly className?: string;
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';
const HEADING = '"Josefin Sans", Futura, sans-serif';

/** Locked cc-* palette: dark surfaces, neutral ink, status hues only for status. */
const C = {
  surface: "#0c1322",
  cardBg: "rgba(12, 19, 34, 0.55)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  dim: "rgba(245, 241, 234, 0.62)",
  eyebrow: "#62748e",
  border: "rgba(245, 241, 234, 0.12)",
  accent: "#5eead4",
  healthy: "#34d399",
  amber: "#fbbf24",
  coral: "#f0786a",
} as const;

const RAIL_Y = 96;
const GATE_X = 262;

interface VersionNode {
  readonly v: string;
  readonly tag: string;
  readonly cx: number;
}

// Ascending schema versions landing on the rail. v12 and v13 are published; v14
// is the newest revision, held to the right of the review gate pending approval.
const PUBLISHED: readonly VersionNode[] = [
  { v: "v12", tag: "eshops@2274", cx: 96 },
  { v: "v13", tag: "eshops@2288", cx: 192 },
];

const PENDING: VersionNode = { v: "v14", tag: "eshops@2291", cx: 352 };

export function GuardrailsIllu({ className }: GuardrailsIlluProps) {
  return (
    <div className={["mx-auto w-full", className ?? ""].join(" ")}>
      <svg
        viewBox="0 0 440 260"
        aria-hidden="true"
        style={{ width: "100%", height: "auto", display: "block" }}
      >
        {/* eyebrows: what this is, and the system that holds it. */}
        <text
          x={22}
          y={16}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.16em"
          fill={C.eyebrow}
        >
          SCHEMA EVOLUTION
        </text>
        <text
          x={418}
          y={16}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.16em"
          fill={C.eyebrow}
          textAnchor="end"
        >
          SCHEMA REGISTRY
        </text>

        {/* the rail: a faint stub of earlier history, solid through the published
            span, dashed past the gate where v14 waits. */}
        <line
          x1={22}
          y1={RAIL_Y}
          x2={48}
          y2={RAIL_Y}
          stroke={C.eyebrow}
          strokeWidth={1.2}
          strokeDasharray="2 3"
          opacity={0.4}
        />
        <line
          x1={48}
          y1={RAIL_Y}
          x2={GATE_X}
          y2={RAIL_Y}
          stroke={C.border}
          strokeWidth={1.4}
        />
        <line
          x1={GATE_X}
          y1={RAIL_Y}
          x2={406}
          y2={RAIL_Y}
          stroke={C.eyebrow}
          strokeWidth={1.4}
          strokeDasharray="3 3"
          opacity={0.65}
        />

        {/* the review gate: a thin line the newest version must clear, with a
            checkpoint marker on the rail and a count of what it holds back. */}
        <line
          x1={GATE_X}
          y1={56}
          x2={GATE_X}
          y2={138}
          stroke={C.border}
          strokeWidth={1}
        />
        <rect
          x={GATE_X - 4.5}
          y={RAIL_Y - 4.5}
          width={9}
          height={9}
          rx={1.4}
          fill={C.surface}
          stroke={C.eyebrow}
          strokeWidth={1.1}
          transform={`rotate(45 ${GATE_X} ${RAIL_Y})`}
        />
        <text
          x={GATE_X}
          y={48}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.16em"
          fill={C.eyebrow}
          textAnchor="middle"
        >
          REVIEW GATE
        </text>
        <text
          x={GATE_X}
          y={154}
          fontFamily={MONO}
          fontSize={8}
          letterSpacing="0.08em"
          fill={C.dim}
          textAnchor="middle"
        >
          1 held
        </text>

        {/* published versions: solid teal node on the rail, numeral above, tag and
            a published chip below. */}
        {PUBLISHED.map((n) => (
          <g key={n.v}>
            <text
              x={n.cx}
              y={RAIL_Y - 26}
              fontFamily={HEADING}
              fontSize={20}
              fontWeight={600}
              fill={C.heading}
              textAnchor="middle"
            >
              {n.v}
            </text>
            <circle
              cx={n.cx}
              cy={RAIL_Y}
              r={7}
              fill={C.surface}
              stroke={C.accent}
              strokeWidth={1.4}
            />
            <circle cx={n.cx} cy={RAIL_Y} r={3} fill={C.accent} />
            <text
              x={n.cx}
              y={RAIL_Y + 23}
              fontFamily={MONO}
              fontSize={9}
              fill={C.dim}
              textAnchor="middle"
            >
              {n.tag}
            </text>
            <rect
              x={n.cx - 31}
              y={RAIL_Y + 30}
              width={62}
              height={16}
              rx={8}
              fill="rgba(94, 234, 212, 0.08)"
              stroke="rgba(94, 234, 212, 0.32)"
              strokeWidth={1}
            />
            <text
              x={n.cx}
              y={RAIL_Y + 41}
              fontFamily={MONO}
              fontSize={8}
              letterSpacing="0.08em"
              fill={C.accent}
              textAnchor="middle"
            >
              published
            </text>
          </g>
        ))}

        {/* held version: a dashed, dimmed ghost node past the gate, marked by the
            single amber pending dot with a slow pulse while it waits. */}
        <text
          x={PENDING.cx}
          y={RAIL_Y - 26}
          fontFamily={HEADING}
          fontSize={20}
          fontWeight={600}
          fill={C.dim}
          textAnchor="middle"
        >
          {PENDING.v}
        </text>
        <motion.circle
          cx={PENDING.cx}
          cy={RAIL_Y}
          fill="none"
          stroke={C.amber}
          strokeWidth={1}
          vectorEffect="non-scaling-stroke"
          initial={{ r: 7, opacity: 0.5 }}
          animate={{ r: [7, 12, 7], opacity: [0.5, 0, 0.5] }}
          transition={{ duration: 2.6, repeat: Infinity, ease: "easeInOut" }}
        />
        <circle
          cx={PENDING.cx}
          cy={RAIL_Y}
          r={7}
          fill={C.surface}
          stroke={C.amber}
          strokeWidth={1.4}
          strokeDasharray="2.4 2.2"
        />
        <circle cx={PENDING.cx} cy={RAIL_Y} r={3} fill={C.amber} />
        <text
          x={PENDING.cx}
          y={RAIL_Y + 23}
          fontFamily={MONO}
          fontSize={9}
          fill={C.eyebrow}
          textAnchor="middle"
        >
          {PENDING.tag}
        </text>
        <rect
          x={PENDING.cx - 31}
          y={RAIL_Y + 30}
          width={62}
          height={16}
          rx={8}
          fill="rgba(251, 191, 36, 0.10)"
          stroke="rgba(251, 191, 36, 0.40)"
          strokeWidth={1}
        />
        <text
          x={PENDING.cx}
          y={RAIL_Y + 41}
          fontFamily={MONO}
          fontSize={8}
          letterSpacing="0.08em"
          fill={C.amber}
          textAnchor="middle"
        >
          pending
        </text>

        {/* connector tying the held node to the changes that hold it. */}
        <line
          x1={PENDING.cx}
          y1={RAIL_Y + 46}
          x2={PENDING.cx}
          y2={168}
          stroke={C.border}
          strokeWidth={1}
          strokeDasharray="2 2.4"
        />

        {/* diff panel: the two changes in v14, each classified by the registry. */}
        <rect
          x={22}
          y={168}
          width={396}
          height={74}
          rx={12}
          fill={C.cardBg}
          stroke={C.border}
          strokeWidth={1}
        />
        <text
          x={38}
          y={186}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.16em"
          fill={C.eyebrow}
        >
          v14 PROPOSED CHANGES
        </text>
        <text
          x={402}
          y={186}
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.08em"
          fill={C.dim}
          textAnchor="end"
        >
          1 safe / 1 breaking
        </text>

        <text
          x={38}
          y={210}
          fontFamily={MONO}
          fontSize={10.5}
          xmlSpace="preserve"
        >
          <tspan fill={C.healthy}>+ </tspan>
          <tspan fill={C.accent}>Product.tags</tspan>
          <tspan fill={C.dim}>: [String!]</tspan>
        </text>
        <rect
          x={356}
          y={200}
          width={46}
          height={16}
          rx={8}
          fill="rgba(52, 211, 153, 0.08)"
          stroke="rgba(52, 211, 153, 0.34)"
          strokeWidth={1}
        />
        <text
          x={379}
          y={211}
          fontFamily={MONO}
          fontSize={8}
          letterSpacing="0.08em"
          fill={C.healthy}
          textAnchor="middle"
        >
          SAFE
        </text>

        <text
          x={38}
          y={232}
          fontFamily={MONO}
          fontSize={10.5}
          xmlSpace="preserve"
        >
          <tspan fill={C.coral}>- </tspan>
          <tspan fill={C.accent}>Product.rating</tspan>
          <tspan fill={C.dim}>: Int</tspan>
        </text>
        <rect
          x={330}
          y={222}
          width={72}
          height={16}
          rx={8}
          fill="rgba(240, 120, 106, 0.08)"
          stroke="rgba(240, 120, 106, 0.38)"
          strokeWidth={1}
        />
        <text
          x={366}
          y={233}
          fontFamily={MONO}
          fontSize={8}
          letterSpacing="0.08em"
          fill={C.coral}
          textAnchor="middle"
        >
          BREAKING
        </text>
      </svg>
    </div>
  );
}
