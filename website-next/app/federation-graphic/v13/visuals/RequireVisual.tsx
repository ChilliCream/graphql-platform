"use client";

/**
 * @require as a miniature of the transit fan: Catalog's coral column and
 * Shipping's cyan column fall straight from their markers, carry their cards
 * as stations (the schema that owns weight and the fetch document on the
 * coral line; the schema that declares @require and the injected call on the
 * cyan line), then both bend through one shared band into the runtime
 * gateway chip. Beads relay strictly in step order: up the coral line to
 * fetch the weight, back down, then up the cyan line with the value
 * injected, and delivery returns. The services never talk to each other.
 * Every document is a real multi-line block and the static frame is the
 * completed relay.
 */

import { MONO_FONT } from "../palette";
import type { Pt } from "./anim";
import { PulseGlyph, easeInOutCubic, measure, ramp, useVisual } from "./anim";
import {
  CANON,
  GatewayChip,
  INK_DIM,
  StreamMarker,
  sampleCubic,
} from "./stage";

const T = 9000;

const CHIP: readonly [number, number] = [455, 546];
const BEND_START = 482;

/** A column: straight drop, then the shared bend band into the chip. */
function column(x: number): {
  readonly d: string;
  readonly down: ReturnType<typeof measure>;
  readonly up: ReturnType<typeof measure>;
} {
  const top: Pt = [x, 48];
  const bend: Pt = [x, BEND_START];
  const c1: Pt = [x, BEND_START + (CHIP[1] - BEND_START) * 0.5];
  const c2: Pt = [CHIP[0], CHIP[1] - (CHIP[1] - BEND_START) * 0.35];
  const end: Pt = [CHIP[0], CHIP[1] - 13];
  const { pts } = sampleCubic(bend, c1, c2, end);
  const full: Pt[] = [top, ...pts];
  const down = measure(full);
  const up = measure([...full].reverse());
  const d = `M${top[0]} ${top[1]} L${bend[0]} ${bend[1]} C ${c1[0]} ${c1[1]}, ${c2[0]} ${c2[1]}, ${end[0]} ${end[1]}`;
  return { d, down, up };
}

const CAT_X = 220;
const SHIP_X = 690;
const CAT_COL = column(CAT_X);
const SHIP_COL = column(SHIP_X);

/* Stations on the coral line. */
const CAT_SDL = { x: 70, y: 62, w: 300, h: 200 } as const;
const STEP1 = { x: 70, y: 302, w: 300, h: 162 } as const;

/* Stations on the cyan line. */
const SHIP_SDL = { x: 530, y: 62, w: 320, h: 218 } as const;
const STEP2 = { x: 540, y: 302, w: 300, h: 162 } as const;

const CAT_SDL_ROWS = [
  "type Query {",
  "  productById(id: ID!): Product @lookup",
  "}",
  "",
  "type Product {",
  "  id: ID!",
  "  name: String!",
  "  weight: Float!",
  "}",
] as const;

const SHIP_SDL_ROWS = [
  "type Query {",
  "  productById(id: ID!): Product @lookup",
  "}",
  "",
  "type Product {",
  "  id: ID!",
  "  delivery(",
  '    weight: Float! @require(field: "weight")',
  "  ): String!",
  "}",
] as const;

const STEP1_ROWS = [
  "query ($id: ID!) {",
  "  productById(id: $id) {",
  "    weight",
  "  }",
  "}",
] as const;

const STEP2_ROWS = [
  "query ($id: ID!) {",
  "  productById(id: $id) {",
  "    delivery(weight: 12.4)",
  "  }",
  "}",
] as const;

/* Directive tokens tinted teal inside the SDL cards. */
const SDL_MARKS = ['@require(field: "weight")', "@lookup"] as const;

export function RequireVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // The response rows dim early, then pop when their bead lands.
    const dim = ramp(t, 250, 550);
    const r1Pop = easeInOutCubic(ramp(t, 3000, 3400));
    const r1 = 1 - 0.55 * dim * (1 - r1Pop);
    h.setPop("resp1", r1, r1);
    const r2Pop = easeInOutCubic(ramp(t, 6300, 6700));
    const r2 = 1 - 0.55 * dim * (1 - r2Pop);
    h.setPop("resp2", r2, r2);

    // Step 1: up the coral line to fetch, the weight comes back down.
    if (t >= 600 && t < 1600) {
      h.placePulse(
        "u1",
        CAT_COL.up,
        easeInOutCubic(ramp(t, 600, 1600)),
        Math.min((t - 600) / 150, 1),
        2.3,
      );
    } else {
      h.hidePulse("u1");
    }
    h.setO("glow1", (ramp(t, 600, 900) - ramp(t, 1700, 2200)) * 0.1);
    if (t >= 2000 && t < 3000) {
      h.placePulse(
        "d1",
        CAT_COL.down,
        easeInOutCubic(ramp(t, 2000, 3000)),
        1,
        2.3,
      );
    } else {
      h.hidePulse("d1");
    }
    h.setRing("ring1", (t - 3000) / 500, 18, 32);

    // Step 2: up the cyan line with the weight injected; @require answers.
    if (t >= 3600 && t < 4600) {
      h.placePulse(
        "u2",
        SHIP_COL.up,
        easeInOutCubic(ramp(t, 3600, 4600)),
        1,
        2.5,
      );
    } else {
      h.hidePulse("u2");
    }
    h.setO("glow2", (ramp(t, 3600, 3900) - ramp(t, 4700, 5200)) * 0.1);
    const rq = ramp(t, 4600, 4900) * (1 - ramp(t, 5600, 6100));
    h.setO("rqGlow", rq * 0.14);
    if (t >= 5200 && t < 6200) {
      h.placePulse(
        "d2",
        SHIP_COL.down,
        easeInOutCubic(ramp(t, 5200, 6200)),
        1,
        2.3,
      );
    } else {
      h.hidePulse("d2");
    }
    h.setRing("ring2", (t - 6200) / 500, 18, 32);
  });

  return (
    <>
      {/* Phones: the same relay as a compact stacked column. */}
      <div className="sm:hidden">
        <MobileRelay />
      </div>

      {/* Larger screens: the animated relay canvas. */}
      <div
        ref={rootRef}
        aria-hidden="true"
        className="hidden w-full overflow-x-auto sm:block"
      >
        <svg
          viewBox="0 0 900 596"
          width="100%"
          className="block min-w-[640px] sm:min-w-0"
        >
          <defs>
            <filter id="rq-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          {/* The two columns, bending together into the gateway. */}
          <path
            d={CAT_COL.d}
            fill="none"
            stroke={CANON[0].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />
          <path
            d={SHIP_COL.d}
            fill="none"
            stroke={CANON[3].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />

          {/* Beads travel under the cards. */}
          <PulseGlyph
            set={set}
            id="u1"
            main="#ffffff"
            soft="#ffffff"
            filter="rq-soft"
          />
          <PulseGlyph
            set={set}
            id="d1"
            main={CANON[0].color}
            soft={CANON[0].soft}
            filter="rq-soft"
          />
          <PulseGlyph
            set={set}
            id="u2"
            main="#ffffff"
            soft="#ffffff"
            filter="rq-soft"
          />
          <PulseGlyph
            set={set}
            id="d2"
            main={CANON[3].color}
            soft={CANON[3].soft}
            filter="rq-soft"
          />

          {/* Catalog: the schema that owns weight. */}
          <rect
            x={CAT_SDL.x}
            y={CAT_SDL.y}
            width={CAT_SDL.w}
            height={CAT_SDL.h}
            rx={12}
            fill="#0d1424"
            stroke="rgba(245,241,234,0.13)"
          />
          <rect
            x={CAT_SDL.x + 14}
            y={CAT_SDL.y + 11}
            width={10}
            height={10}
            rx={3}
            fill={CANON[0].color}
          />
          <text
            x={CAT_SDL.x + 32}
            y={CAT_SDL.y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            CATALOG · OWNS WEIGHT
          </text>
          <line
            x1={CAT_SDL.x}
            x2={CAT_SDL.x + CAT_SDL.w}
            y1={CAT_SDL.y + 30}
            y2={CAT_SDL.y + 30}
            stroke="rgba(245,241,234,0.1)"
          />
          {CAT_SDL_ROWS.map((code, i) => {
            const mark = SDL_MARKS.find((m) => code.includes(m));
            const [before, after] = mark ? code.split(mark) : [code];
            return (
              <text
                key={i}
                x={CAT_SDL.x + 16}
                y={CAT_SDL.y + 48 + i * 17}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={10}
                fill={code.includes("weight:") ? "#e8eef8" : "#c9d4e8"}
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

          {/* Step 1: the fetch the gateway runs first, on the same line. */}
          <rect
            x={STEP1.x}
            y={STEP1.y}
            width={STEP1.w}
            height={STEP1.h}
            rx={12}
            fill="#0d1424"
            stroke="rgba(245,241,234,0.13)"
          />
          <rect
            ref={set("glow1")}
            x={STEP1.x}
            y={STEP1.y}
            width={STEP1.w}
            height={STEP1.h}
            rx={12}
            fill={CANON[0].color}
            opacity={0}
          />
          <text
            x={STEP1.x + 14}
            y={STEP1.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            STEP 1 · FETCH THE WEIGHT
          </text>
          <line
            x1={STEP1.x}
            x2={STEP1.x + STEP1.w}
            y1={STEP1.y + 32}
            y2={STEP1.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {STEP1_ROWS.map((code, i) => (
            <text
              key={i}
              x={STEP1.x + 16}
              y={STEP1.y + 50 + i * 17}
              xmlSpace="preserve"
              fontFamily={MONO_FONT}
              fontSize={11.5}
              fill="#c9d4e8"
            >
              {code}
            </text>
          ))}
          <line
            x1={STEP1.x}
            x2={STEP1.x + STEP1.w}
            y1={STEP1.y + 130}
            y2={STEP1.y + 130}
            stroke="rgba(245,241,234,0.1)"
          />
          <g ref={set("resp1")} opacity={1}>
            <path
              d="M0 4 h8 M5.5 1.5 L8 4 L5.5 6.5"
              transform={`translate(${STEP1.x + 16}, ${STEP1.y + 140.5})`}
              fill="none"
              stroke={INK_DIM}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <text
              x={STEP1.x + 30}
              y={STEP1.y + 148}
              xmlSpace="preserve"
              fontFamily={MONO_FONT}
              fontSize={10}
              fill={INK_DIM}
            >
              {'{ "weight": '}
              <tspan fill={CANON[0].soft}>12.4</tspan>
              {" }"}
            </text>
          </g>

          {/* Shipping: the schema that declares what it needs. */}
          <rect
            x={SHIP_SDL.x}
            y={SHIP_SDL.y}
            width={SHIP_SDL.w}
            height={SHIP_SDL.h}
            rx={12}
            fill="#0d1424"
            stroke="rgba(245,241,234,0.13)"
          />
          <rect
            x={SHIP_SDL.x + 14}
            y={SHIP_SDL.y + 11}
            width={10}
            height={10}
            rx={3}
            fill={CANON[3].color}
          />
          <text
            x={SHIP_SDL.x + 32}
            y={SHIP_SDL.y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            SHIPPING
          </text>
          <text
            x={SHIP_SDL.x + SHIP_SDL.w - 14}
            y={SHIP_SDL.y + 20}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={INK_DIM}
            opacity={0.6}
          >
            schema.graphql
          </text>
          <line
            x1={SHIP_SDL.x}
            x2={SHIP_SDL.x + SHIP_SDL.w}
            y1={SHIP_SDL.y + 30}
            y2={SHIP_SDL.y + 30}
            stroke="rgba(245,241,234,0.1)"
          />
          <rect
            ref={set("rqGlow")}
            x={SHIP_SDL.x + 8}
            y={SHIP_SDL.y + 154}
            width={SHIP_SDL.w - 16}
            height={17}
            rx={5}
            fill="#5eead4"
            opacity={0}
          />
          {SHIP_SDL_ROWS.map((code, i) => {
            const mark = SDL_MARKS.find((m) => code.includes(m));
            const [before, after] = mark ? code.split(mark) : [code];
            return (
              <text
                key={i}
                x={SHIP_SDL.x + 16}
                y={SHIP_SDL.y + 48 + i * 17}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={10}
                fill={code.includes("weight:") ? "#e8eef8" : "#c9d4e8"}
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

          {/* Step 2: the call the gateway makes, weight injected, same line. */}
          <rect
            x={STEP2.x}
            y={STEP2.y}
            width={STEP2.w}
            height={STEP2.h}
            rx={12}
            fill="#0d1424"
            stroke="rgba(245,241,234,0.13)"
          />
          <rect
            ref={set("glow2")}
            x={STEP2.x}
            y={STEP2.y}
            width={STEP2.w}
            height={STEP2.h}
            rx={12}
            fill={CANON[3].color}
            opacity={0}
          />
          <text
            x={STEP2.x + 14}
            y={STEP2.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            STEP 2 · WEIGHT INJECTED
          </text>
          <line
            x1={STEP2.x}
            x2={STEP2.x + STEP2.w}
            y1={STEP2.y + 32}
            y2={STEP2.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {STEP2_ROWS.map((code, i) => {
            const [before, after] = code.split("12.4");
            return (
              <text
                key={i}
                x={STEP2.x + 16}
                y={STEP2.y + 50 + i * 17}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={11.5}
                fill="#c9d4e8"
              >
                {after === undefined ? (
                  code
                ) : (
                  <>
                    {before}
                    <tspan fill={CANON[0].soft}>12.4</tspan>
                    {after}
                  </>
                )}
              </text>
            );
          })}
          <line
            x1={STEP2.x}
            x2={STEP2.x + STEP2.w}
            y1={STEP2.y + 130}
            y2={STEP2.y + 130}
            stroke="rgba(245,241,234,0.1)"
          />
          <g ref={set("resp2")} opacity={1}>
            <path
              d="M0 4 h8 M5.5 1.5 L8 4 L5.5 6.5"
              transform={`translate(${STEP2.x + 16}, ${STEP2.y + 140.5})`}
              fill="none"
              stroke={INK_DIM}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <text
              x={STEP2.x + 30}
              y={STEP2.y + 148}
              xmlSpace="preserve"
              fontFamily={MONO_FONT}
              fontSize={10}
              fill={INK_DIM}
            >
              {'{ "delivery": '}
              <tspan fill={CANON[3].soft}>{'"2d"'}</tspan>
              {" }"}
            </text>
          </g>

          {/* Markers and the runtime gateway at the hub. */}
          <StreamMarker
            x={CAT_X}
            y={34}
            color={CANON[0].color}
            label="Catalog"
          />
          <StreamMarker
            x={SHIP_X}
            y={34}
            color={CANON[3].color}
            label="Shipping"
          />
          <GatewayChip x={CHIP[0]} y={CHIP[1]} />
          <circle
            ref={set("ring1")}
            cx={CHIP[0]}
            cy={CHIP[1]}
            r={18}
            fill="none"
            stroke={CANON[0].color}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ring2")}
            cx={CHIP[0]}
            cy={CHIP[1]}
            r={18}
            fill="none"
            stroke={CANON[3].color}
            strokeWidth={1.5}
            opacity={0}
          />
          <text
            x={CHIP[0]}
            y={578}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.16em"
            fill={INK_DIM}
            opacity={0.8}
          >
            SHIPPING NEVER CALLS CATALOG · THE GATEWAY DOES THE FETCHING
          </text>
        </svg>
      </div>
    </>
  );
}

/* Tokens tinted when they appear in a mobile code row. */
const MOBILE_TINTS: readonly {
  readonly token: string;
  readonly fill: string;
}[] = [
  { token: '@require(field: "weight")', fill: "#5eead4" },
  { token: "@lookup", fill: "#5eead4" },
  { token: "12.4", fill: CANON[0].soft },
  { token: '"2d"', fill: CANON[3].soft },
];

interface MobileRowProps {
  readonly code: string;
  readonly dim?: boolean;
  readonly arrow?: boolean;
}

/* Tiny response arrow, drawn instead of a text glyph. */
function ArrowGlyph() {
  return (
    <svg
      viewBox="0 0 9 8"
      aria-hidden="true"
      className="mr-1.5 inline-block h-2 w-[9px]"
    >
      <path
        d="M0 4 h8 M5.5 1.5 L8 4 L5.5 6.5"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function MobileRow({ code, dim, arrow }: MobileRowProps) {
  const tint = MOBILE_TINTS.find((m) => code.includes(m.token));
  const cls = "whitespace-pre " + (dim ? "text-cc-ink-dim" : "text-[#c9d4e8]");
  if (!tint) {
    return (
      <div className={cls}>
        {arrow && <ArrowGlyph />}
        {code}
      </div>
    );
  }
  const [before, after] = code.split(tint.token);
  return (
    <div className={cls}>
      {arrow && <ArrowGlyph />}
      {before}
      <span style={{ color: tint.fill }}>{tint.token}</span>
      {after}
    </div>
  );
}

interface MobileCardProps {
  readonly label: string;
  readonly color: string;
  readonly file?: string;
  readonly lines: readonly string[];
  readonly resp?: readonly MobileRowProps[];
}

function MobileCard({ label, color, file, lines, resp }: MobileCardProps) {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <div className="flex items-center gap-2">
        <span
          className="inline-block h-2.5 w-2.5 rounded-[3px]"
          style={{ background: color }}
        />
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
          {label}
        </span>
        {file && (
          <span className="text-cc-ink-dim ml-auto font-mono text-[9px]">
            {file}
          </span>
        )}
      </div>
      <div className="border-cc-card-border mt-2 overflow-x-auto border-t pt-2 font-mono text-[12px] leading-6">
        {lines.map((code, i) => (
          <MobileRow key={i} code={code} />
        ))}
      </div>
      {resp && (
        <div className="border-cc-card-border mt-2 overflow-x-auto border-t pt-2 font-mono text-[11px] leading-5">
          {resp.map((r, i) => (
            <MobileRow key={i} {...r} />
          ))}
        </div>
      )}
    </div>
  );
}

function MobileRelay() {
  return (
    <div className="space-y-4">
      <MobileCard
        label="Catalog · owns weight"
        color={CANON[0].color}
        lines={CAT_SDL_ROWS}
      />
      <MobileCard
        label="Step 1 · fetch the weight"
        color={CANON[0].color}
        lines={STEP1_ROWS}
        resp={[{ code: '{ "weight": 12.4 }', dim: true, arrow: true }]}
      />
      <MobileCard
        label="Shipping"
        color={CANON[3].color}
        file="schema.graphql"
        lines={SHIP_SDL_ROWS}
      />
      <MobileCard
        label="Step 2 · weight injected"
        color={CANON[3].color}
        lines={STEP2_ROWS}
        resp={[{ code: '{ "delivery": "2d" }', dim: true, arrow: true }]}
      />
      <p className="text-cc-ink-dim text-center font-mono text-[10px] tracking-[0.16em] uppercase">
        Shipping never calls Catalog · the gateway does the fetching
      </p>
    </div>
  );
}
