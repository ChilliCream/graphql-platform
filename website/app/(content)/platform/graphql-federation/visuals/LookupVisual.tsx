"use client";

import type { ReactNode } from "react";

import { MONO_FONT } from "../palette";
import { PulseGlyph, easeInOutCubic, measure, ramp, useVisual } from "./anim";
import { CANON, GatewayChip, INK_DIM, StreamMarker } from "./stage";

const T = 8000;

const CHIP: readonly [number, number] = [450, 330];

const COL_X = 450;
const DOOR = { x: 320, y: 60, w: 310, h: 178 } as const;

const REQ = { x: 40, y: 264, w: 270, h: 132 } as const;
const RESP = { x: 620, y: 262, w: 260, h: 136 } as const;

const KEY = '@key(fields: "id")';
const LOOKUP = "@lookup";

const SCHEMA_ROWS = [
  "type Query {",
  `  productById(id: ID!): Product ${LOOKUP}`,
  "}",
  "",
  `type Product ${KEY} {`,
  "  id: ID!",
  "  price: Money!",
  "}",
] as const;

const SCHEMA_MARKS = [KEY, LOOKUP] as const;

const QUERY_ROWS = [
  "query ($id: ID!) {",
  "  productById(id: $id) {",
  "    price",
  "  }",
  "}",
] as const;

const RESP_ROWS = [
  "{",
  '  "productById": {',
  '    "price": "24.90 EUR"',
  "  }",
  "}",
] as const;
const RESP_DOT_ROW = 2;

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
}

function MobileCard({ label, color, accent, lines }: MobileCardProps) {
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
    </div>
  );
}

export function LookupVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const dim = ramp(t, 250, 550);
    const rPop = easeInOutCubic(ramp(t, 5200, 5600));
    const rv = 1 - 0.55 * dim * (1 - rPop);
    h.setPop("respRows", rv, rv);

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

    const glow = ramp(t, 2300, 2600) * (1 - ramp(t, 3400, 3900));
    h.setO("lkGlow", glow * 0.14);

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
      <div aria-hidden="true" className="space-y-4 sm:hidden">
        <MobileCard
          label="Billing · schema.graphql"
          color={CANON[1].color}
          lines={SCHEMA_ROWS.map((code) => {
            const mark = SCHEMA_MARKS.find((m) => code.includes(m));
            if (!mark) {
              return { text: code };
            }
            const [before, after] = code.split(mark);
            return {
              text: (
                <>
                  {before}
                  <span className="text-[#5eead4]">{mark}</span>
                  {after}
                </>
              ),
            };
          })}
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
        />
        <MobileCard
          accent
          label="The lookup response"
          lines={RESP_ROWS.map((code, i) => ({
            text: code,
            dot: i === RESP_DOT_ROW ? CANON[1].color : undefined,
          }))}
        />
      </div>

      <div
        ref={rootRef}
        aria-hidden="true"
        className="hidden w-full overflow-x-auto sm:block"
      >
        <svg
          viewBox="0 0 900 420"
          width="100%"
          className="block min-w-[640px] sm:min-w-0"
        >
          <defs>
            <filter id="lk-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          <path
            d={`M${COL_X} 48 V${CHIP[1] - 13}`}
            fill="none"
            stroke={CANON[1].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />

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
            KEY AND LOOKUP
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
          {SCHEMA_ROWS.map((code, i) => {
            const mark = SCHEMA_MARKS.find((m) => code.includes(m));
            const [before, after] = mark ? code.split(mark) : [code];
            return (
              <text
                key={i}
                x={DOOR.x + 16}
                y={DOOR.y + 48 + i * 17}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={11}
                fill="#c9d4e8"
              >
                {mark ? (
                  <>
                    {before}
                    <tspan fill="#5eead4">{mark}</tspan>
                    {after}
                  </>
                ) : (
                  code
                )}
              </text>
            );
          })}

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
            THE LOOKUP RESPONSE
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
                {i === RESP_DOT_ROW && (
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
        </svg>
      </div>
    </>
  );
}
