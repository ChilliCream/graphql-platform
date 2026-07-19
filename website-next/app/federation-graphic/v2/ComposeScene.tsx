/**
 * Federation hero scene, v2 — no metaphor. The actual domain, staged: three
 * team-owned schema panels each declare their slice of Product; field
 * connectors flow into one composed Product card (per-field owner chips, id as
 * the visible identity join), which feeds the graph below. Connectors draw in
 * once on load; everything is static under prefers-reduced-motion. Decorative
 * for screen readers — the copy beside it carries the message.
 */

import { CYAN, GREEN, MONO_FONT, SLATE, TEAL, VIOLET } from "./palette";

const INK = "#e8eef8";
const CODE = "#c9d4e8";
const CARD_BG = "rgba(12, 19, 34, 0.72)";
const CARD_BORDER = "rgba(245, 241, 234, 0.14)";

const PANEL_X = 30;
const PANEL_W = 310;
const CARD_X = 580;
const CARD_W = 310;
const CARD_Y = 210;

interface SourcePanel {
  readonly team: string;
  readonly color: string;
  readonly y: number;
  /** SDL lines inside the panel body, after `type Product {`. */
  readonly fields: readonly string[];
}

const SOURCES: readonly SourcePanel[] = [
  {
    team: "catalog",
    color: CYAN,
    y: 30,
    fields: ["id: ID!", "name: String!", "price: Money!"],
  },
  {
    team: "inventory",
    color: GREEN,
    y: 240,
    fields: ["id: ID!", "stock: Int!"],
  },
  {
    team: "reviews",
    color: VIOLET,
    y: 430,
    fields: ["id: ID!", "rating: Float!"],
  },
];

interface ComposedRow {
  readonly code: string;
  readonly color?: string;
  readonly isKey?: boolean;
}

const COMPOSED: readonly ComposedRow[] = [
  { code: "id: ID!", isKey: true },
  { code: "name: String!", color: CYAN },
  { code: "price: Money!", color: CYAN },
  { code: "stock: Int!", color: GREEN },
  { code: "rating: Float!", color: VIOLET },
];

function panelHeight(p: SourcePanel): number {
  // header 36 + top pad 18 + lines (type/fields/brace) + bottom pad 12
  return 36 + 18 + (p.fields.length + 2) * 22 + 12;
}

/** Y of a field's text baseline inside its panel (0-based, after `type` line). */
function fieldY(p: SourcePanel, i: number): number {
  return p.y + 36 + 18 + (i + 1) * 22;
}

function rowY(j: number): number {
  return CARD_Y + 48 + j * 24;
}

function connector(y1: number, y2: number): string {
  const x1 = PANEL_X + PANEL_W;
  const x2 = CARD_X;
  return `M ${x1} ${y1 - 4} C ${x1 + 95} ${y1 - 4}, ${x2 - 95} ${y2 - 4}, ${x2} ${y2 - 4}`;
}

export function ComposeScene() {
  const flows: { d: string; color: string; delay: number }[] = [];
  const idJoins: { d: string; color: string }[] = [];
  let flowIndex = 0;
  for (const src of SOURCES) {
    src.fields.forEach((f, i) => {
      const target = COMPOSED.findIndex((r) => r.code === f);
      if (target < 0) {
        return;
      }
      const d = connector(fieldY(src, i), rowY(target));
      if (COMPOSED[target].isKey) {
        idJoins.push({ d, color: src.color });
      } else {
        flows.push({ d, color: src.color, delay: 0.5 + flowIndex * 0.22 });
        flowIndex += 1;
      }
    });
  }

  return (
    <svg
      id="fcs-svg"
      aria-hidden="true"
      className="h-auto w-full"
      viewBox="0 0 920 640"
    >
      <style>{`
        .fcs-flow {
          stroke-dasharray: 1;
          stroke-dashoffset: 1;
          animation: fcs-draw 0.7s cubic-bezier(0.6, 0, 0.2, 1) forwards;
        }
        .fcs-join {
          opacity: 0;
          animation: fcs-fade 0.6s ease-out 1.6s forwards;
        }
        .fcs-halo {
          opacity: 0;
          animation: fcs-fade 0.8s ease-out 1.9s forwards;
        }
        @keyframes fcs-draw {
          to { stroke-dashoffset: 0; }
        }
        @keyframes fcs-fade {
          to { opacity: 1; }
        }
        @media (prefers-reduced-motion: reduce) {
          .fcs-flow { animation: none; stroke-dashoffset: 0; }
          .fcs-join, .fcs-halo { animation: none; opacity: 1; }
        }
      `}</style>

      {/* Source panels: one schema per team. */}
      {SOURCES.map((p) => {
        const h = panelHeight(p);
        return (
          <g key={p.team}>
            <rect
              x={PANEL_X}
              y={p.y}
              width={PANEL_W}
              height={h}
              rx={14}
              fill={CARD_BG}
              stroke={CARD_BORDER}
            />
            <circle cx={PANEL_X + 22} cy={p.y + 19} r={4} fill={p.color} />
            <text
              x={PANEL_X + 36}
              y={p.y + 23}
              fill={SLATE}
              fontFamily={MONO_FONT}
              fontSize="11"
              fontWeight="600"
              letterSpacing="0.22em"
            >
              {p.team.toUpperCase()}
            </text>
            <text
              x={PANEL_X + PANEL_W - 20}
              y={p.y + 23}
              textAnchor="end"
              fill={SLATE}
              fontFamily={MONO_FONT}
              fontSize="10"
              opacity="0.7"
            >
              schema.graphql
            </text>
            <line
              x1={PANEL_X}
              x2={PANEL_X + PANEL_W}
              y1={p.y + 36}
              y2={p.y + 36}
              stroke={CARD_BORDER}
            />
            <text
              x={PANEL_X + 20}
              y={p.y + 36 + 18}
              fill={CODE}
              fontFamily={MONO_FONT}
              fontSize="13.5"
            >
              {"type "}
              <tspan fill={p.color}>Product</tspan>
              {" {"}
            </text>
            {p.fields.map((f, i) => (
              <text
                key={f}
                x={PANEL_X + 40}
                y={fieldY(p, i)}
                fill={f.startsWith("id:") ? SLATE : CODE}
                fontFamily={MONO_FONT}
                fontSize="13.5"
              >
                {f}
              </text>
            ))}
            <text
              x={PANEL_X + 20}
              y={fieldY(p, p.fields.length)}
              fill={CODE}
              fontFamily={MONO_FONT}
              fontSize="13.5"
            >
              {"}"}
            </text>
          </g>
        );
      })}

      {/* Identity joins: every team's id line, dashed into the key row. */}
      {idJoins.map((j, i) => (
        <path
          key={i}
          className="fcs-join"
          d={j.d}
          fill="none"
          stroke={j.color}
          strokeWidth="1.5"
          strokeDasharray="3 5"
          strokeOpacity="0.55"
        />
      ))}

      {/* Field flows into the composed type. */}
      {flows.map((f, i) => (
        <path
          key={i}
          className="fcs-flow"
          d={f.d}
          pathLength={1}
          fill="none"
          stroke={f.color}
          strokeWidth="2.5"
          strokeOpacity="0.9"
          style={{ animationDelay: `${f.delay}s` }}
        />
      ))}

      {/* The composed entity. */}
      <g>
        <rect
          className="fcs-halo"
          x={CARD_X - 7}
          y={CARD_Y - 7}
          width={CARD_W + 14}
          height={214}
          rx={20}
          fill="none"
          stroke={TEAL}
          strokeOpacity="0.4"
          strokeWidth="2"
        />
        <rect
          x={CARD_X}
          y={CARD_Y}
          width={CARD_W}
          height={200}
          rx={14}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <text
          x={CARD_X + 20}
          y={CARD_Y + 24}
          fill={INK}
          fontFamily={MONO_FONT}
          fontSize="14"
          fontWeight="600"
        >
          Product
        </text>
        <text
          x={CARD_X + CARD_W - 20}
          y={CARD_Y + 24}
          textAnchor="end"
          fill={TEAL}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.22em"
        >
          COMPOSED
        </text>
        <line
          x1={CARD_X}
          x2={CARD_X + CARD_W}
          y1={CARD_Y + 36}
          y2={CARD_Y + 36}
          stroke={CARD_BORDER}
        />
        {COMPOSED.map((r, j) => (
          <g key={r.code}>
            <text
              x={CARD_X + 20}
              y={rowY(j)}
              fill={r.isKey ? INK : CODE}
              fontFamily={MONO_FONT}
              fontSize="13.5"
            >
              {r.code}
            </text>
            {r.isKey ? (
              <g>
                {/* All three teams share the identity. */}
                <circle
                  cx={CARD_X + CARD_W - 56}
                  cy={rowY(0) - 4}
                  r={3.5}
                  fill={CYAN}
                />
                <circle
                  cx={CARD_X + CARD_W - 42}
                  cy={rowY(0) - 4}
                  r={3.5}
                  fill={GREEN}
                />
                <circle
                  cx={CARD_X + CARD_W - 28}
                  cy={rowY(0) - 4}
                  r={3.5}
                  fill={VIOLET}
                />
              </g>
            ) : (
              <circle
                cx={CARD_X + CARD_W - 28}
                cy={rowY(j) - 4}
                r={3.5}
                fill={r.color}
              />
            )}
          </g>
        ))}
      </g>

      {/* The composed type takes its place in the graph. */}
      <g className="fcs-halo">
        <line
          x1={CARD_X + CARD_W / 2}
          y1={CARD_Y + 200}
          x2={CARD_X + CARD_W / 2}
          y2={492}
          stroke={TEAL}
          strokeOpacity="0.5"
          strokeWidth="1.5"
        />
        <line
          x1={623}
          y1={520}
          x2={703}
          y2={520}
          stroke={CARD_BORDER}
          strokeWidth="1.5"
        />
        <line
          x1={767}
          y1={520}
          x2={847}
          y2={520}
          stroke={CARD_BORDER}
          strokeWidth="1.5"
        />
        <circle
          cx={607}
          cy={520}
          r={16}
          fill="none"
          stroke={SLATE}
          strokeWidth="1.5"
        />
        <circle
          cx={735}
          cy={520}
          r={28}
          fill="none"
          stroke={TEAL}
          strokeWidth="2"
        />
        <circle
          cx={863}
          cy={520}
          r={16}
          fill="none"
          stroke={SLATE}
          strokeWidth="1.5"
        />
        <text
          x={607}
          y={556}
          textAnchor="middle"
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10.5"
        >
          Order
        </text>
        <text
          x={735}
          y={524}
          textAnchor="middle"
          fill={TEAL}
          fontFamily={MONO_FONT}
          fontSize="11"
          fontWeight="600"
        >
          Product
        </text>
        <text
          x={863}
          y={556}
          textAnchor="middle"
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10.5"
        >
          Customer
        </text>
      </g>

      {/* Caption. */}
      <text
        x={300}
        y={600}
        fill={SLATE}
        fontFamily={MONO_FONT}
        fontSize="11"
        letterSpacing="0.24em"
      >
        THREE SCHEMAS · ONE TYPE · ONE GRAPH
      </text>
    </svg>
  );
}
