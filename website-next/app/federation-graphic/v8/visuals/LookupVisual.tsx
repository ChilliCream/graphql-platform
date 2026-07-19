"use client";

/**
 * The @lookup field in the page's stream language: Billing's column drops
 * straight into the runtime gateway chip, and the schema card that carries
 * the @lookup field hangs on that line like a transit station. On the spine,
 * the gateway sends one real batched operation (left card) and receives one
 * response per id (right card). Beads ride the wires: the call travels up the
 * column into Billing, the answers come back down and land in the response
 * card. Every card sits on a wire, every document is a real multi-line
 * block, and a frozen frame shows the completed exchange.
 */

import type { ReactNode } from "react";

import { MONO_FONT } from "../palette";
import { PulseGlyph, easeInOutCubic, measure, ramp, useVisual } from "./anim";
import { CANON, GatewayChip, INK_DIM, StreamMarker } from "./stage";

const T = 8000;

const CHIP: readonly [number, number] = [450, 250];

/* Billing's column and the lookup-field card hanging on it. */
const COL_X = 450;
const DOOR = { x: 295, y: 72, w: 310, h: 95 } as const;

/* The batched call (left) and the per-id response (right), on the spine. */
const REQ = { x: 40, y: 164, w: 270, h: 172 } as const;
const RESP = { x: 620, y: 180, w: 260, h: 140 } as const;

const QUERY_ROWS = [
  "query ($id: ID!) {",
  "  productById(id: $id) {",
  "    price",
  "  }",
  "}",
] as const;

const RESP_ROWS = [
  "[",
  '  { "price": "24.90 EUR" },',
  '  { "price": "12.50 EUR" }',
  "]",
] as const;

/* Wires the beads ride. */
const LANE_IN = measure([
  [REQ.x + REQ.w, CHIP[1]],
  [CHIP[0] - 44, CHIP[1]],
]);
const COL_UP = measure([
  [COL_X, CHIP[1] - 13],
  [COL_X, DOOR.y + DOOR.h],
]);
const COL_DOWN = measure([
  [COL_X, DOOR.y + DOOR.h],
  [COL_X, CHIP[1] - 13],
]);
const LANE_OUT = measure([
  [CHIP[0] + 44, CHIP[1]],
  [RESP.x, CHIP[1]],
]);

interface MobileLine {
  readonly text: ReactNode;
  readonly dot?: string;
}

interface MobileCardProps {
  readonly label: string;
  readonly color?: string;
  readonly accent?: boolean;
  readonly lines: readonly MobileLine[];
  readonly footer?: ReactNode;
}

function MobileCard({ label, color, accent, lines, footer }: MobileCardProps) {
  return (
    <div
      className={`rounded-xl border bg-[#0d1424] p-4 ${
        accent ? "border-[rgba(94,234,212,0.35)]" : "border-cc-card-border"
      }`}
    >
      <div className="flex items-center gap-2">
        {color && (
          <span
            className="inline-block h-2.5 w-2.5 rounded-[3px]"
            style={{ background: color }}
          />
        )}
        <span
          className={`font-mono text-[10px] tracking-[0.2em] uppercase ${
            accent ? "text-[#5eead4]" : "text-cc-nav-label"
          }`}
        >
          {label}
        </span>
      </div>
      <div className="border-cc-card-border mt-2 border-t pt-2 font-mono text-[12px] leading-6 text-[#c9d4e8]">
        {lines.map((l, i) => (
          <div key={i} className="flex items-center gap-2">
            <span className="whitespace-pre">{l.text}</span>
            {l.dot && (
              <span
                className="ml-auto inline-block h-2 w-2 shrink-0 rounded-full"
                style={{ background: l.dot }}
              />
            )}
          </div>
        ))}
      </div>
      {footer && (
        <div className="border-cc-card-border text-cc-ink-dim mt-2 border-t pt-2 font-mono text-[10.5px]">
          {footer}
        </div>
      )}
    </div>
  );
}

export function LookupVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // The response rows dim early, then land again with the amber bead.
    const dim = ramp(t, 250, 550);
    const rPop = easeInOutCubic(ramp(t, 5200, 5600));
    const rv = 1 - 0.55 * dim * (1 - rPop);
    h.setPop("respRows", rv, rv);
    const nPop = easeInOutCubic(ramp(t, 5700, 6100));
    const nv = 1 - 0.6 * dim * (1 - nPop);
    h.setPop("respNote", nv, nv);

    // The call: card -> chip, then up Billing's column to the lookup field.
    if (t >= 700 && t < 1400) {
      h.placePulse(
        "q1",
        LANE_IN,
        easeInOutCubic(ramp(t, 700, 1400)),
        Math.min((t - 700) / 130, 1),
        2.5,
      );
    } else {
      h.hidePulse("q1");
    }
    h.setRing("ringChip", (t - 1400) / 450, 18, 32);
    if (t >= 1500 && t < 2300) {
      h.placePulse("q2", COL_UP, easeInOutCubic(ramp(t, 1500, 2300)), 1, 2.3);
    } else {
      h.hidePulse("q2");
    }

    // The @lookup field answers with a quiet glow.
    const glow = ramp(t, 2300, 2600) * (1 - ramp(t, 3400, 3900));
    h.setO("lkGlow", glow * 0.14);

    // The answers: back down the column, then out to the response card.
    if (t >= 3600 && t < 4400) {
      h.placePulse("a1", COL_DOWN, easeInOutCubic(ramp(t, 3600, 4400)), 1, 2.3);
    } else {
      h.hidePulse("a1");
    }
    if (t >= 4500 && t < 5200) {
      h.placePulse("a2", LANE_OUT, easeInOutCubic(ramp(t, 4500, 5200)), 1, 2.4);
    } else {
      h.hidePulse("a2");
    }
    h.setRing("ringResp", (t - 5200) / 450, 6, 14);
  });

  return (
    <>
      {/* Phones: the same story as a compact stacked column. */}
      <div aria-hidden="true" className="space-y-4 sm:hidden">
        <MobileCard
          label="Billing · schema.graphql"
          color={CANON[1].color}
          lines={[
            { text: "type Query {" },
            {
              text: (
                <>
                  {"  productById(id: ID!): Product "}
                  <span className="text-[#5eead4]">@lookup</span>
                </>
              ),
            },
            { text: "}" },
          ]}
        />
        <MobileCard
          label="The lookup call"
          lines={[
            { text: "query ($id: ID!) {" },
            { text: "  productById(id: $id) {" },
            { text: "    price" },
            { text: "  }" },
            { text: "}" },
          ]}
          footer={
            <>
              <div>variables · one call carries every id</div>
              <div className="mt-1 whitespace-pre text-[#c9d4e8]">
                {'[{ "id": "P-401" }, { "id": "P-882" }]'}
              </div>
            </>
          }
        />
        <MobileCard
          accent
          label="One response per id"
          lines={[
            { text: "[" },
            { text: '  { "price": "24.90 EUR" },', dot: CANON[1].color },
            { text: '  { "price": "12.50 EUR" }', dot: CANON[1].color },
            { text: "]" },
          ]}
          footer="→ fills the plan's missing price"
        />
        <p className="text-cc-ink-dim text-center font-mono text-[10px] tracking-[0.18em] uppercase">
          Trades an id for the entity
        </p>
      </div>

      {/* Larger screens: the wired stream canvas. */}
      <div
        ref={rootRef}
        aria-hidden="true"
        className="hidden w-full overflow-x-auto sm:block"
      >
        <svg
          viewBox="0 0 900 352"
          width="100%"
          className="block min-w-[640px] sm:min-w-0"
        >
          <defs>
            <filter id="lk-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          {/* Billing's column, bold like the transit streams. */}
          <path
            d={`M${COL_X} 48 V${CHIP[1] - 13}`}
            fill="none"
            stroke={CANON[1].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />

          {/* The spine lanes. */}
          <path
            d={`M${REQ.x + REQ.w} ${CHIP[1]} H${CHIP[0] - 44}`}
            fill="none"
            stroke="rgba(139,160,188,0.4)"
            strokeWidth={1.5}
          />
          <path
            d={`M${CHIP[0] + 44} ${CHIP[1]} H${RESP.x}`}
            fill="none"
            stroke="rgba(139,160,188,0.4)"
            strokeWidth={1.5}
          />

          {/* Beads travel under the cards. */}
          <PulseGlyph
            set={set}
            id="q1"
            main="#ffffff"
            soft="#ffffff"
            filter="lk-soft"
          />
          <PulseGlyph
            set={set}
            id="q2"
            main="#ffffff"
            soft="#ffffff"
            filter="lk-soft"
          />
          <PulseGlyph
            set={set}
            id="a1"
            main={CANON[1].color}
            soft={CANON[1].soft}
            filter="lk-soft"
          />
          <PulseGlyph
            set={set}
            id="a2"
            main={CANON[1].color}
            soft={CANON[1].soft}
            filter="lk-soft"
          />

          {/* The lookup field: Billing's ordinary query field, on Billing's line. */}
          <rect
            x={DOOR.x}
            y={DOOR.y}
            width={DOOR.w}
            height={DOOR.h}
            rx={12}
            fill="#0d1424"
            stroke="rgba(245,241,234,0.13)"
          />
          <rect
            x={DOOR.x + 14}
            y={DOOR.y + 11}
            width={10}
            height={10}
            rx={3}
            fill={CANON[1].color}
          />
          <text
            x={DOOR.x + 32}
            y={DOOR.y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            THE LOOKUP FIELD
          </text>
          <text
            x={DOOR.x + DOOR.w - 14}
            y={DOOR.y + 20}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={INK_DIM}
            opacity={0.6}
          >
            billing · schema.graphql
          </text>
          <line
            x1={DOOR.x}
            x2={DOOR.x + DOOR.w}
            y1={DOOR.y + 30}
            y2={DOOR.y + 30}
            stroke="rgba(245,241,234,0.1)"
          />
          <rect
            ref={set("lkGlow")}
            x={DOOR.x + 8}
            y={DOOR.y + 53}
            width={DOOR.w - 16}
            height={17}
            rx={5}
            fill="#5eead4"
            opacity={0}
          />
          <text
            x={DOOR.x + 16}
            y={DOOR.y + 48}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#c9d4e8"
          >
            {"type Query {"}
          </text>
          <text
            x={DOOR.x + 16}
            y={DOOR.y + 65}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#c9d4e8"
          >
            {"  productById(id: ID!): Product "}
            <tspan fill="#5eead4">@lookup</tspan>
          </text>
          <text
            x={DOOR.x + 16}
            y={DOOR.y + 82}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#c9d4e8"
          >
            {"}"}
          </text>

          {/* The batched call, wired into the chip. */}
          <rect
            x={REQ.x}
            y={REQ.y}
            width={REQ.w}
            height={REQ.h}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <text
            x={REQ.x + 14}
            y={REQ.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            THE LOOKUP CALL
          </text>
          <line
            x1={REQ.x}
            x2={REQ.x + REQ.w}
            y1={REQ.y + 32}
            y2={REQ.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {QUERY_ROWS.map((code, i) => (
            <text
              key={i}
              x={REQ.x + 16}
              y={REQ.y + 50 + i * 17}
              xmlSpace="preserve"
              fontFamily={MONO_FONT}
              fontSize={11.5}
              fill="#c9d4e8"
            >
              {code}
            </text>
          ))}
          <line
            x1={REQ.x}
            x2={REQ.x + REQ.w}
            y1={REQ.y + 130}
            y2={REQ.y + 130}
            stroke="rgba(245,241,234,0.1)"
          />
          <text
            x={REQ.x + 16}
            y={REQ.y + 146}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={INK_DIM}
            opacity={0.85}
          >
            variables · one call carries every id
          </text>
          <text
            x={REQ.x + 16}
            y={REQ.y + 161}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={INK_DIM}
          >
            {'[{ "id": "P-401" }, { "id": "P-882" }]'}
          </text>

          {/* One response per id, wired off the chip. */}
          <rect
            x={RESP.x}
            y={RESP.y}
            width={RESP.w}
            height={RESP.h}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(94,234,212,0.35)"
          />
          <text
            x={RESP.x + 14}
            y={RESP.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill="#5eead4"
          >
            ONE RESPONSE PER ID
          </text>
          <line
            x1={RESP.x}
            x2={RESP.x + RESP.w}
            y1={RESP.y + 32}
            y2={RESP.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          <g ref={set("respRows")} opacity={1}>
            {RESP_ROWS.map((code, i) => (
              <g key={i}>
                <text
                  x={RESP.x + 16}
                  y={RESP.y + 50 + i * 17}
                  xmlSpace="preserve"
                  fontFamily={MONO_FONT}
                  fontSize={11.5}
                  fill="#c9d4e8"
                >
                  {code}
                </text>
                {(i === 1 || i === 2) && (
                  <circle
                    cx={RESP.x + RESP.w - 22}
                    cy={RESP.y + 46 + i * 17}
                    r={3}
                    fill={CANON[1].color}
                  />
                )}
              </g>
            ))}
          </g>
          <line
            x1={RESP.x}
            x2={RESP.x + RESP.w}
            y1={RESP.y + 113}
            y2={RESP.y + 113}
            stroke="rgba(245,241,234,0.1)"
          />
          <g ref={set("respNote")} opacity={1}>
            <text
              x={RESP.x + 16}
              y={RESP.y + 129}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill={INK_DIM}
            >
              → fills the plan&apos;s missing price
            </text>
          </g>

          {/* Billing's marker and the runtime gateway. */}
          <StreamMarker x={450} y={36} color={CANON[1].color} label="Billing" />
          <GatewayChip x={CHIP[0]} y={CHIP[1]} />
          <circle
            ref={set("ringChip")}
            cx={CHIP[0]}
            cy={CHIP[1]}
            r={18}
            fill="none"
            stroke="#fff"
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ringResp")}
            cx={RESP.x}
            cy={CHIP[1]}
            r={6}
            fill="none"
            stroke={CANON[1].color}
            strokeWidth={1.5}
            opacity={0}
          />
          <text
            x={CHIP[0]}
            y={292}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.18em"
            fill={INK_DIM}
            opacity={0.75}
          >
            TRADES AN ID FOR THE ENTITY
          </text>
        </svg>
      </div>
    </>
  );
}
