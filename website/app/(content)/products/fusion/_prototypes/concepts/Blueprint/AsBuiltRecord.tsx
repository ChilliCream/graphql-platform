"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BP } from "./palette";
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

const ROW_Y = 82;
const ROW_PITCH = 40;

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
74%{opacity:1;transform:translateY(150px)}
76%,100%{opacity:0;transform:translateY(150px)}}
${OPERATIONS.map((_, i) => fade(`bp-a-verdict${i}`, 62 + i * 3.6)).join("\n")}
${fade("bp-a-summary", 82)}
${fade("bp-a-note", 88)}
`;

export function AsBuiltRecord() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();

  return (
    <Sheet
      title="Schema governance · as-built record"
      no="DWG-104"
      rev="E"
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 480 360"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        <text className="bp-a-head bp-t-dim" x={14} y={16} fontSize={6.5}>
          REVISION E COMPARED WITH THE AS-BUILT RECORD
        </text>

        {/* The revision: a field taken off the drawing */}
        <path
          className="bp-a-rev"
          d="M14 26h196v168h-196Z"
          pathLength={1}
          fill={BP.plate}
          stroke={BP.ink}
          strokeWidth={1.3}
        />
        <g className="bp-a-rev-label">
          <text x={26} y={46} fontSize={8}>
            REV E · CATALOG
          </text>
          <text className="bp-t-dim" x={26} y={72} fontSize={7}>
            type Product {"{"}
          </text>
          <text className="bp-t-dim" x={26} y={88} fontSize={7}>
            {"  id: ID!"}
          </text>
          <text className="bp-t-dim" x={26} y={104} fontSize={7}>
            {"  name: String!"}
          </text>
        </g>
        <g className="bp-a-removed">
          <text className="bp-t-red" x={26} y={126} fontSize={7}>
            {"  price: Float!"}
          </text>
          <path
            d="M26 123h96"
            fill="none"
            stroke={BP.redline}
            strokeWidth={1}
          />
          <text className="bp-t-red" x={132} y={126} fontSize={6}>
            REMOVED
          </text>
          <text className="bp-t-dim" x={26} y={146} fontSize={7}>
            {"}"}
          </text>
        </g>
        <g className="bp-a-pass" style={{ transformOrigin: "112px 172px" }}>
          <path
            d="M26 158h172v28H26Z"
            fill="none"
            stroke={BP.ok}
            strokeWidth={1.2}
          />
          <text
            className="bp-t-ok"
            x={112}
            y={176}
            textAnchor="middle"
            fontSize={7.5}
          >
            COMPOSITION: PASS
          </text>
        </g>
        <text className="bp-a-note bp-t-dim" x={14} y={212} fontSize={6.2}>
          THE SOURCE SCHEMAS STILL COMPOSE
        </text>
        <text className="bp-a-note bp-t-dim" x={14} y={224} fontSize={6.2}>
          THE BUILD STAYS GREEN
        </text>

        {/* The comparison */}
        <path
          className="bp-a-compare"
          d="M210 110h30"
          pathLength={1}
          fill="none"
          stroke={BP.dim}
          strokeWidth={1}
        />
        <path
          className="bp-a-compare-head"
          d="M240 110l-7 -2.6v5.2Z"
          fill={BP.dim}
        />

        {/* The as-built record */}
        <path
          className="bp-a-record"
          d="M240 26h226v268H240Z"
          pathLength={1}
          fill={BP.plate}
          stroke={BP.ink}
          strokeWidth={1.3}
        />
        <g className="bp-a-record-label">
          <text x={254} y={46} fontSize={8}>
            AS-BUILT RECORD
          </text>
          <text
            className="bp-t-dim"
            x={452}
            y={46}
            textAnchor="end"
            fontSize={6}
          >
            NITRO
          </text>
          <text className="bp-t-dim" x={254} y={62} fontSize={6}>
            PUBLISHED OPERATIONS · REGISTERED CLIENTS
          </text>
        </g>

        {OPERATIONS.map((operation, i) => {
          const y = ROW_Y + i * ROW_PITCH;

          return (
            <g key={operation.name}>
              <g className={`bp-a-op${i}`}>
                <path
                  d={`M254 ${y}h198`}
                  fill="none"
                  stroke={BP.inkFaint}
                  strokeWidth={0.8}
                />
                <text x={254} y={y + 16} fontSize={7.5}>
                  {operation.name}
                </text>
                <text className="bp-t-dim" x={254} y={y + 28} fontSize={6}>
                  {`${operation.client} · ${operation.note}`}
                </text>
              </g>
              <text
                className={`bp-a-verdict${i} ${VERDICT_CLASS[operation.verdict]}`}
                x={452}
                y={y + 20}
                textAnchor="end"
                fontSize={7.5}
              >
                {operation.verdict}
              </text>
            </g>
          );
        })}

        {/* The sweep across the record */}
        <g className="bp-a-scan">
          <path d="M246 76h214" fill="none" stroke={BP.dim} strokeWidth={1.2} />
          <text className="bp-t-cyan" x={246} y={70} fontSize={6}>
            CHECKING
          </text>
        </g>

        <text className="bp-a-summary bp-t-dim" x={254} y={282} fontSize={6.2}>
          1 BREAKING · 1 RISKY · 2 SAFE · BEFORE THE MERGE
        </text>

        <text className="bp-a-note bp-t-dim" x={240} y={318} fontSize={6.2}>
          THE OPERATION THE MOBILE APP RUNS IS FLAGGED BEFORE IT SHIPS
        </text>
      </svg>
    </Sheet>
  );
}
