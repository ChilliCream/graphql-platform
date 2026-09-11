"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BP, FONT } from "./palette";
import { DUR, draw, fade, Sheet, stamp } from "./Sheet";

/**
 * "Composition protects the graph, Nitro protects your clients": a revision
 * checked against the as-built record. The revision on the left removes a
 * field and still passes the tolerance check, because every source schema
 * still composes. On the right, Nitro's as-built record - the operations real
 * clients actually published - is swept against that same revision, and the
 * operation the mobile app runs comes back marked breaking.
 *
 * Rest state: the revision stamped as composing, the record swept and every
 * published operation marked safe, risky or breaking.
 */

interface Operation {
  readonly client: string;
  readonly name: string;
  readonly verdict: "SAFE" | "RISKY" | "BREAKING";
  readonly note: string;
}

/** The as-built record: what registered clients actually run today. */
const OPERATIONS: readonly Operation[] = [
  {
    client: "WEB",
    name: "ProductList",
    verdict: "SAFE",
    note: "id · name",
  },
  {
    client: "MOBILE",
    name: "CheckoutQuery",
    verdict: "BREAKING",
    note: "id · price",
  },
  {
    client: "PARTNER API",
    name: "OrderFeed",
    verdict: "RISKY",
    note: "order · eta",
  },
  {
    client: "AGENT",
    name: "StockLookup",
    verdict: "SAFE",
    note: "id · stock",
  },
];

const VERDICT_CLASS: Record<Operation["verdict"], string> = {
  SAFE: "bp-t-ok",
  RISKY: "bp-t-query",
  BREAKING: "bp-t-red",
};

const ROW_Y = 96;
const ROW_PITCH = 46;
/** The as-built record panel: its left edge and the x its fields end at. */
const REC_X = 200;
const REC_RIGHT = 460;

const CSS = `
${fade("bp-a-head", 1)}
${draw("bp-a-rev", 3, 6)}
${fade("bp-a-rev-label", 9)}
${fade("bp-a-removed", 16)}
${stamp("bp-a-pass", 22)}
${draw("bp-a-record", 30, 6)}
${fade("bp-a-record-label", 36)}
${OPERATIONS.map((_, i) => fade(`bp-a-op${i}`, 40 + i * 3)).join("\n")}
${draw("bp-a-compare", 54, 4)}
${fade("bp-a-compare-head", 58)}
.bp-a-scan{opacity:0;transform:none;animation:bp-a-scan ${DUR}s linear infinite}
@keyframes bp-a-scan{
0%,58%{opacity:0;transform:none}
60%{opacity:1;transform:none}
74%{opacity:1;transform:translateY(176px)}
76%,100%{opacity:0;transform:translateY(176px)}}
${OPERATIONS.map((_, i) => fade(`bp-a-verdict${i}`, 62 + i * 3.6)).join("\n")}
${fade("bp-a-summary", 82)}
${fade("bp-a-note", 88)}
`;

export function AsBuiltRecord() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();

  return (
    <Sheet
      title="As-built record"
      no="DWG-104"
      rev="E"
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 480 318"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        <text
          className="bp-a-head bp-t-dim"
          x={12}
          y={22}
          fontSize={FONT.label}
        >
          REVISION E · AS-BUILT RECORD
        </text>

        {/* The revision: a field taken off the drawing */}
        <path
          className="bp-a-rev"
          d="M12 32h174v180H12Z"
          pathLength={1}
          fill={BP.plate}
          stroke={BP.ink}
          strokeWidth={1.3}
        />
        <g className="bp-a-rev-label">
          <text x={18} y={56} fontSize={FONT.label}>
            CATALOG
          </text>
          <text
            className="bp-t-dim"
            x={180}
            y={56}
            textAnchor="end"
            fontSize={FONT.label}
          >
            REV E
          </text>
          <text className="bp-t-dim" x={18} y={84} fontSize={FONT.label}>
            type Product {"{"}
          </text>
          <text className="bp-t-dim" x={30} y={106} fontSize={FONT.label}>
            id: ID!
          </text>
          <text className="bp-t-dim" x={30} y={128} fontSize={FONT.label}>
            name: String!
          </text>
        </g>
        <g className="bp-a-removed">
          <text className="bp-t-red" x={30} y={150} fontSize={FONT.label}>
            price: Float!
          </text>
          <path
            d="M30 144h150"
            fill="none"
            stroke={BP.redline}
            strokeWidth={1}
          />
          <text className="bp-t-red" x={30} y={172} fontSize={FONT.label}>
            REMOVED
          </text>
          <text className="bp-t-dim" x={18} y={198} fontSize={FONT.label}>
            {"}"}
          </text>
        </g>
        <g className="bp-a-pass" style={{ transformOrigin: "99px 240px" }}>
          <path
            d="M12 224h174v32H12Z"
            fill="none"
            stroke={BP.ok}
            strokeWidth={1.2}
          />
          <text
            className="bp-t-ok"
            x={99}
            y={245}
            textAnchor="middle"
            fontSize={FONT.label}
          >
            COMPOSITION PASS
          </text>
        </g>
        <text
          className="bp-a-note bp-t-dim"
          x={12}
          y={278}
          fontSize={FONT.label}
        >
          STILL COMPOSES
        </text>
        <text
          className="bp-a-note bp-t-dim"
          x={12}
          y={300}
          fontSize={FONT.label}
        >
          BUILD IS GREEN
        </text>

        {/* The comparison */}
        <path
          className="bp-a-compare"
          d="M186 120h8"
          pathLength={1}
          fill="none"
          stroke={BP.dim}
          strokeWidth={1}
        />
        <path
          className="bp-a-compare-head"
          d="M200 120l-9-3.4v6.8Z"
          fill={BP.dim}
        />

        {/* The as-built record */}
        <path
          className="bp-a-record"
          d={`M${REC_X} 32h270v258H${REC_X}Z`}
          pathLength={1}
          fill={BP.plate}
          stroke={BP.ink}
          strokeWidth={1.3}
        />
        <g className="bp-a-record-label">
          <text x={REC_X + 14} y={56} fontSize={FONT.label}>
            AS-BUILT RECORD
          </text>
          <text
            className="bp-t-dim"
            x={REC_RIGHT}
            y={56}
            textAnchor="end"
            fontSize={FONT.label}
          >
            NITRO
          </text>
          <text
            className="bp-t-dim"
            x={REC_X + 14}
            y={78}
            fontSize={FONT.label}
          >
            PUBLISHED OPERATIONS
          </text>
        </g>

        {OPERATIONS.map((operation, i) => {
          const y = ROW_Y + i * ROW_PITCH;

          return (
            <g key={operation.name}>
              <g className={`bp-a-op${i}`}>
                <path
                  d={`M${REC_X + 14} ${y}h246`}
                  fill="none"
                  stroke={BP.inkFaint}
                  strokeWidth={0.8}
                />
                <text x={REC_X + 14} y={y + 20} fontSize={FONT.label}>
                  {operation.name}
                </text>
                <text
                  className="bp-t-dim"
                  x={REC_X + 14}
                  y={y + 40}
                  fontSize={FONT.label}
                >
                  {operation.note}
                </text>
                <text
                  className="bp-t-dim"
                  x={REC_RIGHT}
                  y={y + 40}
                  textAnchor="end"
                  fontSize={FONT.label}
                >
                  {operation.client}
                </text>
              </g>
              <text
                className={`bp-a-verdict${i} ${VERDICT_CLASS[operation.verdict]}`}
                x={REC_RIGHT}
                y={y + 20}
                textAnchor="end"
                fontSize={FONT.label}
              >
                {operation.verdict}
              </text>
            </g>
          );
        })}

        {/* The sweep across the record */}
        <g className="bp-a-scan">
          <path
            d={`M${REC_X + 6} 88h258`}
            fill="none"
            stroke={BP.dim}
            strokeWidth={1.2}
          />
          <text
            className="bp-t-cyan"
            x={REC_X + 6}
            y={80}
            fontSize={FONT.label}
          >
            CHECKING
          </text>
        </g>

        <text
          className="bp-a-summary bp-t-dim"
          x={12}
          y={314}
          fontSize={FONT.label}
        >
          1 BREAKING · 1 RISKY · 2 SAFE
        </text>
      </svg>
    </Sheet>
  );
}
