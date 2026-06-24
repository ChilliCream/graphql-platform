import type { CSSProperties, ReactNode } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface BuildVariant4Props {
  readonly className?: string;
}

/**
 * Build-loop scene, variant 4 (source -> generated lineage).
 *
 * A small, focused widget that tells the Hot Chocolate source-generation story:
 * one implementation-first C# source line declaring a `[QueryType] ProductApi`
 * class (GitHub-dark syntax tokens, a single thin code title row), feeding a
 * tiny lineage diagram where the source pill branches into the three artifacts
 * the generator emits at build time: the GraphQL schema, the resolver pipeline,
 * and the strongly-typed client.
 *
 * One node in, three chips out: it reads as a single idea, not an app screen.
 * Rendered as the settled FINAL frame, static, no animation. Self-contained;
 * Nitro palette only via inline styles; every SVG / element id is prefixed
 * "build-v4-".
 */
export function BuildVariant4({ className }: BuildVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      style={{ fontFamily: nitro.font, color: nitro.text }}
    >
      <div
        style={{
          background: nitro.bg,
          border: `1px solid ${nitro.border}`,
          borderRadius: nitro.radius,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
        }}
      >
        {/* Single thin code title row: filename + language label. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "8px",
            height: "30px",
            padding: "0 12px",
            background: nitro.surface,
            borderBottom: `1px solid ${nitro.border}`,
          }}
        >
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: "11px",
              color: nitro.textSecondary,
            }}
          >
            ProductApi.cs
          </span>
          <span
            style={{
              marginLeft: "auto",
              fontFamily: nitro.mono,
              fontSize: "10px",
              letterSpacing: "0.08em",
              color: nitro.textDim,
            }}
          >
            C#
          </span>
        </div>

        {/* The implementation-first source line: this declaration is the source. */}
        <div
          style={{
            padding: "12px 14px 10px",
            fontFamily: nitro.mono,
            fontSize: "12px",
            lineHeight: "1.6",
            whiteSpace: "pre",
          }}
        >
          <div>
            <Attr>[QueryType]</Attr>
          </div>
          <div>
            <Kw>public partial class </Kw>
            <Type>ProductApi</Type>
          </div>
        </div>

        {/* Lineage diagram: source pill branches into three emitted artifacts. */}
        <div style={{ padding: "0 14px 16px" }}>
          <Lineage />
        </div>
      </div>
    </div>
  );
}

/** The four-node source -> generated lineage drawn as a small inline SVG. */
function Lineage() {
  const w = 280;
  const h = 150;

  // Source pill (top), three artifact chips fanned out along the bottom.
  const srcCx = w / 2;
  const srcCy = 26;
  const srcW = 132;
  const chipY = 112;
  const chips = [
    { cx: 46, label: "schema", color: nitro.icGraphql },
    { cx: 140, label: "resolvers", color: nitro.synField },
    { cx: 234, label: "client", color: nitro.blue },
  ] as const;

  return (
    <svg
      role="img"
      aria-label="ProductApi source generates a schema, resolver pipeline, and client"
      viewBox={`0 0 ${w} ${h}`}
      width="100%"
      style={{ display: "block", overflow: "visible" }}
    >
      <defs>
        <marker
          id="build-v4-arrow"
          viewBox="0 0 8 8"
          refX="6"
          refY="4"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M1 1L6 4L1 7" fill="none" stroke={nitro.graphEdge} />
        </marker>
      </defs>

      {/* edges: source -> each chip, with arrowheads into the chips */}
      {chips.map((chip) => (
        <path
          key={`build-v4-edge-${chip.label}`}
          d={`M ${srcCx} ${srcCy + 16} C ${srcCx} ${srcCy + 56}, ${chip.cx} ${chipY - 48}, ${chip.cx} ${chipY - 16}`}
          fill="none"
          stroke={nitro.graphEdge}
          strokeWidth="1.4"
          markerEnd="url(#build-v4-arrow)"
        />
      ))}

      {/* source node: the [QueryType] ProductApi pill */}
      <g>
        <rect
          x={srcCx - srcW / 2}
          y={srcCy - 16}
          width={srcW}
          height={32}
          rx={16}
          fill={nitro.card}
          stroke={nitro.borderStrong}
          strokeWidth="1"
        />
        <circle cx={srcCx - srcW / 2 + 18} cy={srcCy} r={4.5} fill={nitro.icObject} />
        <text
          x={srcCx + 6}
          y={srcCy}
          textAnchor="middle"
          dominantBaseline="central"
          fontFamily={nitro.mono}
          fontSize="11.5"
          fill={nitro.textStrong}
        >
          ProductApi
        </text>
      </g>

      {/* artifact chips */}
      {chips.map((chip) => (
        <Chip
          key={`build-v4-chip-${chip.label}`}
          cx={chip.cx}
          cy={chipY}
          label={chip.label}
          color={chip.color}
        />
      ))}
    </svg>
  );
}

/** One emitted-artifact chip: a small rounded tag with a tinted dot. */
function Chip({
  cx,
  cy,
  label,
  color,
}: {
  readonly cx: number;
  readonly cy: number;
  readonly label: string;
  readonly color: string;
}) {
  const cw = 80;
  const ch = 28;
  return (
    <g>
      <rect
        x={cx - cw / 2}
        y={cy - ch / 2}
        width={cw}
        height={ch}
        rx={6}
        fill={nitro.surface}
        stroke={nitro.border}
        strokeWidth="1"
      />
      <circle cx={cx - cw / 2 + 14} cy={cy} r={3.5} fill={color} />
      <text
        x={cx + 7}
        y={cy}
        textAnchor="middle"
        dominantBaseline="central"
        fontFamily={nitro.mono}
        fontSize="11"
        fill={nitro.text}
      >
        {label}
      </text>
    </g>
  );
}

/* GitHub-dark C# syntax spans for the single source declaration line. */
const span = (
  color: string,
): ((p: { readonly children: ReactNode }) => ReactNode) =>
  function Span({ children }) {
    const style: CSSProperties = { color };
    return <span style={style}>{children}</span>;
  };

const Attr = span(nitro.synField);
const Kw = span(nitro.synKeyword);
const Type = span(nitro.synType);
