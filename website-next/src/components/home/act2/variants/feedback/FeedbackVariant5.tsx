/**
 * Agentic coding, variant 5: the MCP mini-hub.
 *
 * A tiny hub-and-spoke diagram, re-authored as an authentic cropped slice of the
 * Nitro / Banana Cake Pop topology canvas (GitHub-dark dot grid). Four small
 * source chips (schema / published ops / clients / skills) sit on the left and
 * right and converge inward along curved edges into a single `/graphql/mcp`
 * core node at the center. That core is what a coding agent connects to: the
 * server projects schema, published operations, registered clients, and checked
 * in skills onto one MCP surface.
 *
 * Deliberately minimal: no tab bar, no toolbar, no surrounding app chrome. Just
 * the dot-grid canvas, four chips, the converging edges, and the core node, so
 * it reads as one focused widget beside a paragraph.
 *
 * Fully static: the settled final frame only, no animation, no motion package,
 * no hooks. Colors come from the shared Nitro palette via inline `style` (the
 * graph / topology tokens). All SVG ids are prefixed "feedback-v5-".
 */
import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface FeedbackVariant5Props {
  readonly className?: string;
}

/** A source chip feeding the hub: its label, accent, and anchor on the canvas. */
interface Source {
  readonly id: string;
  readonly label: string;
  readonly color: string;
  /** chip center, in the 320x200 canvas viewBox. */
  readonly x: number;
  readonly y: number;
  /** the side of the chip that the edge leaves from. */
  readonly side: "left" | "right";
}

const W = 320;
const H = 200;
const CX = W / 2;
const CY = H / 2;

/** The four feeds that the server projects onto the single MCP surface. */
const SOURCES: readonly Source[] = [
  {
    id: "schema",
    label: "schema",
    color: nitro.icGraphql,
    x: 52,
    y: 40,
    side: "left",
  },
  {
    id: "ops",
    label: "published ops",
    color: nitro.synField,
    x: 52,
    y: 160,
    side: "left",
  },
  {
    id: "clients",
    label: "clients",
    color: nitro.blue,
    x: 268,
    y: 40,
    side: "right",
  },
  {
    id: "skills",
    label: "skills",
    color: nitro.accentHover,
    x: 268,
    y: 160,
    side: "right",
  },
];

const CHIP_W = 96;
const CHIP_H = 30;
const CORE_R = 38;

/** Bezier from a source chip's inner edge to the rim of the core node. */
function edgePath(s: Source): string {
  const startX = s.side === "left" ? s.x + CHIP_W / 2 : s.x - CHIP_W / 2;
  const startY = s.y;
  const dx = CX - startX;
  const dy = CY - startY;
  const dist = Math.hypot(dx, dy);
  // stop short of the core so the edge meets the rim, not the center.
  const endX = CX - (dx / dist) * (CORE_R + 2);
  const endY = CY - (dy / dist) * (CORE_R + 2);
  const c1x = startX + dx * 0.45;
  const c2x = endX - dx * 0.35;
  return `M${startX} ${startY} C${c1x} ${startY} ${c2x} ${endY} ${endX} ${endY}`;
}

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

export function FeedbackVariant5({ className }: FeedbackVariant5Props) {
  return (
    <div
      className={className}
      style={{
        width: "100%",
        maxWidth: 340,
        margin: "0 auto",
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: nitro.radius,
        fontFamily: nitro.font,
        color: nitro.text,
        overflow: "hidden",
        userSelect: "none",
        boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
      }}
    >
      <svg
        viewBox={`0 0 ${W} ${H}`}
        width="100%"
        role="img"
        aria-label="Schema, published operations, clients, and skills converging into a single /graphql/mcp surface"
        style={{ display: "block", background: nitro.graphCanvas }}
      >
        <defs>
          <pattern
            id="feedback-v5-dots"
            width="18"
            height="18"
            patternUnits="userSpaceOnUse"
          >
            <circle cx="1" cy="1" r="1" fill={nitro.graphDots} opacity="0.5" />
          </pattern>
          <radialGradient id="feedback-v5-core-glow" cx="50%" cy="50%" r="50%">
            <stop
              offset="0%"
              stopColor={nitro.accentHover}
              stopOpacity="0.22"
            />
            <stop offset="100%" stopColor={nitro.accentHover} stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* dot-grid canvas */}
        <rect x="0" y="0" width={W} height={H} fill="url(#feedback-v5-dots)" />

        {/* converging edges: chip rim -> core rim, each tinted by its source */}
        {SOURCES.map((s) => {
          const d = edgePath(s);
          const inX = s.side === "left" ? s.x + CHIP_W / 2 : s.x - CHIP_W / 2;
          return (
            <g key={`edge-${s.id}`}>
              <path
                d={d}
                fill="none"
                stroke={nitro.graphEdge}
                strokeWidth={1.5}
              />
              {/* a short colored stub at the source end keys the edge to its chip */}
              <circle cx={inX} cy={s.y} r="2.4" fill={s.color} />
            </g>
          );
        })}

        {/* core glow + node */}
        <circle
          cx={CX}
          cy={CY}
          r={CORE_R + 16}
          fill="url(#feedback-v5-core-glow)"
        />
        <circle
          cx={CX}
          cy={CY}
          r={CORE_R}
          fill={nitro.graphNodeSuccess}
          stroke={nitro.accentHover}
          strokeWidth={1.5}
        />
        <text
          x={CX}
          y={CY - 5}
          textAnchor="middle"
          style={{
            ...MONO,
            fontSize: 11,
            fontWeight: 700,
            fill: nitro.textStrong,
          }}
        >
          /graphql
        </text>
        <text
          x={CX}
          y={CY + 9}
          textAnchor="middle"
          style={{
            ...MONO,
            fontSize: 11,
            fontWeight: 700,
            fill: nitro.accentHover,
          }}
        >
          /mcp
        </text>
        <text
          x={CX}
          y={CY + 24}
          textAnchor="middle"
          style={{
            ...MONO,
            fontSize: 7,
            letterSpacing: "0.14em",
            fill: nitro.textDim,
          }}
        >
          MCP SURFACE
        </text>

        {/* source chips */}
        {SOURCES.map((s) => (
          <g key={`chip-${s.id}`}>
            <rect
              x={s.x - CHIP_W / 2}
              y={s.y - CHIP_H / 2}
              width={CHIP_W}
              height={CHIP_H}
              rx={6}
              fill={nitro.graphNode}
              stroke={nitro.graphEdge}
              strokeWidth={1}
            />
            <circle
              cx={s.x - CHIP_W / 2 + 13}
              cy={s.y}
              r="3.5"
              fill={s.color}
            />
            <text
              x={s.x - CHIP_W / 2 + 23}
              y={s.y + 3.5}
              style={{
                fontFamily: nitro.font,
                fontSize: 10,
                fontWeight: 500,
                fill: nitro.text,
              }}
            >
              {s.label}
            </text>
          </g>
        ))}
      </svg>

      {/* one thin caption row, no app chrome */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "8px 12px",
          background: nitro.surface,
          borderTop: `1px solid ${nitro.border}`,
        }}
      >
        <span style={{ fontSize: 11, color: nitro.textSecondary }}>
          One surface for coding agents
        </span>
        <span
          style={{
            ...MONO,
            fontSize: 9,
            letterSpacing: "0.1em",
            textTransform: "uppercase",
            color: nitro.textDim,
            whiteSpace: "nowrap",
          }}
        >
          {SOURCES.length} feeds
        </span>
      </div>
    </div>
  );
}
