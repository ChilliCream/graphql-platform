"use client";

/**
 * The hero scene: a three-column circuit read left to right and back.
 * Documents never fly; pulses fly, documents dock and hold. The client's
 * formatted GraphQL request docks on the left and stays readable. The
 * gateway is a machine panel (not the composition glow): its body builds
 * the query plan row by row, and as it does, the request's field lines
 * colorize to the owning service's color, showing ownership rather than
 * telling it. Sub-queries dock at each subgraph as real formatted GraphQL,
 * crossfade into the JSON fragments they return, and the loop closes with
 * a formatted JSON response document assembling under the request. The
 * phase strip at the bottom narrates. One loop is 22 seconds: every
 * document holds long enough to actually read.
 */

import { MONO_FONT } from "../palette";
import { easeInOutCubic, measure, PulseGlyph, ramp, useVisual } from "./anim";
import { CANON, INK_DIM, sampleCubic } from "./stage";

const T = 22000;

const CODE = "#c9d4e8";
const INK = "#e8eef8";
const DIM = "rgba(201,212,232,0.45)";
const TEAL = "#5eead4";
const GREEN = "#8fd6a0";
const CARD_FILL = "rgba(12,19,34,0.6)";
const CARD_STROKE = "rgba(245,241,234,0.14)";
const GW_FILL = "#101a2e";
const GW_STROKE = "rgba(245,241,234,0.22)";
const HAIRLINE = "rgba(245,241,234,0.1)";

/* The three services on stage: Catalog, Billing, Shipping. */
const SUBS = [
  { s: 0, field: "name", json: '"name": "Aero Mug"', port: ":4001" },
  { s: 1, field: "price", json: '"price": "24.90 EUR"', port: ":4002" },
  {
    s: 3,
    field: "delivery",
    json: '"delivery": "2d"',
    port: ":4003",
  },
] as const;

/* ── Layout ──────────────────────────────────────────────────────────── */

const REQ = { x: 100, y: 130, w: 310, h: 178 } as const;
const RESP = { x: 100, y: 372, w: 310, h: 202 } as const;
const GW = { x: 590, y: 230, w: 240, h: 180 } as const;
const SUB_X = 1060;
const SUB_W = 270;
const SUB_H = 126;
const SUB_Y = [100, 272, 444] as const;

const GW_ROW_Y = [324, 346, 368] as const;
const LANE_OUT_Y = [268, 320, 372] as const;
const SUB_DOCK_Y = SUB_Y.map((y) => y + 18);

interface DocLine {
  readonly t: string;
  readonly ind: number;
  readonly c?: string;
  /** Initial opacity for the reduced-motion static frame. */
  readonly o?: number;
}

/* The client request: real, formatted GraphQL. Lines 2..4 are the field
   lines that colorize to their owner's color during PLAN; statically they
   are hidden so only the colored overlays show. */
const REQ_LINES: readonly DocLine[] = [
  { t: "query GetProduct {", ind: 0 },
  { t: 'product(id: "P-401") {', ind: 1 },
  { t: "name", ind: 2, o: 0 },
  { t: "price", ind: 2, o: 0 },
  { t: "delivery", ind: 2, o: 0 },
  { t: "}", ind: 1, c: DIM },
  { t: "}", ind: 0, c: DIM },
];
const REQ_FIELD_LINES = [2, 3, 4] as const;

/* The one response: real, formatted JSON, values in owner colors. */
const RESP_LINES: readonly DocLine[] = [
  { t: "{", ind: 0, c: DIM },
  { t: '"data": {', ind: 1 },
  { t: '"product": {', ind: 2 },
  { t: '"name": "Aero Mug",', ind: 3, c: CANON[0].color },
  { t: '"price": "24.90 EUR",', ind: 3, c: CANON[1].color },
  { t: '"delivery": "2d"', ind: 3, c: CANON[3].color },
  { t: "}", ind: 2, c: DIM },
  { t: "}", ind: 1, c: DIM },
  { t: "}", ind: 0, c: DIM },
];

/* Each subgraph gets its own formatted sub-query, then answers with a
   formatted JSON fragment in the same card body. */
function subQueryLines(field: string, color: string): readonly DocLine[] {
  return [
    { t: "{", ind: 0, c: DIM },
    { t: 'product(id: "P-401") {', ind: 1 },
    { t: field, ind: 2, c: color },
    { t: "}", ind: 1, c: DIM },
    { t: "}", ind: 0, c: DIM },
  ];
}
function subJsonLines(json: string, color: string): readonly DocLine[] {
  return [
    { t: "{", ind: 0, c: DIM },
    { t: '"product": {', ind: 1 },
    { t: json, ind: 2, c: color },
    { t: "}", ind: 1, c: DIM },
    { t: "}", ind: 0, c: DIM },
  ];
}

/* ── Lanes ───────────────────────────────────────────────────────────── */

function lane(
  p0: readonly [number, number],
  c1: readonly [number, number],
  c2: readonly [number, number],
  p1: readonly [number, number],
) {
  const { pts, d } = sampleCubic(p0, c1, c2, p1, 32);
  return { poly: measure(pts), d };
}

const LANE_IN = lane([412, 219], [500, 219], [512, 300], [588, 300]);
const LANE_RESP = lane([588, 348], [512, 348], [500, 458], [412, 458]);
const LANES = SUBS.map((_, i) =>
  lane(
    [832, LANE_OUT_Y[i]],
    [920, LANE_OUT_Y[i]],
    [952, SUB_DOCK_Y[i]],
    [1058, SUB_DOCK_Y[i]],
  ),
);
const BACKS = LANES.map((l) => measure([...l.poly.pts].reverse()));

/* ── Schedule ────────────────────────────────────────────────────────── */

const REQ_IN = 600;
const REQ_PULSE = [3100, 3750] as const;
const PLAN_ROW = [4500, 4850, 5200] as const;
const EXEC_DEPART = [8200, 8460, 8720] as const;
const FLIGHT = 720;
const FLIP = [11700, 12600, 13500] as const;
const RET_DEPART = FLIP.map((f) => f + 1300);
const RET_ARRIVE = RET_DEPART.map((d) => d + FLIGHT);
const MERGE_AT = 15900;
const RESP_PULSE = [17350, 18000] as const;
const RESP_IN = 18000;
const FADE = [21100, 21800] as const;

const PHASES = [
  { label: "request", from: REQ_IN, to: 4200 },
  { label: "plan", from: 4200, to: 8200 },
  { label: "execute", from: 8200, to: 15800 },
  { label: "merge", from: 15800, to: RESP_PULSE[0] },
  { label: "respond", from: RESP_PULSE[0], to: FADE[0] },
] as const;

/* ── Small render helpers ────────────────────────────────────────────── */

interface CodeLinesProps {
  /** Register per-line refs; omit when the lines are driven as a group. */
  readonly set?: (k: string) => (node: SVGElement | null) => void;
  readonly prefix?: string;
  readonly lines: readonly DocLine[];
  readonly x: number;
  readonly y0: number;
  readonly lh: number;
  readonly size: number;
  readonly indent: number;
}

function CodeLines({
  set,
  prefix,
  lines,
  x,
  y0,
  lh,
  size,
  indent,
}: CodeLinesProps) {
  return (
    <g>
      {lines.map((l, i) => (
        <text
          key={i}
          ref={set && prefix !== undefined ? set(`${prefix}${i}`) : undefined}
          x={x + l.ind * indent}
          y={y0 + i * lh}
          fontFamily={MONO_FONT}
          fontSize={size}
          fill={l.c ?? CODE}
          opacity={l.o ?? 1}
        >
          {l.t}
        </text>
      ))}
    </g>
  );
}

interface DocHeaderProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly label: string;
  readonly labelColor?: string;
  readonly right: string;
  readonly rightColor?: string;
}

function DocHeader({
  x,
  y,
  w,
  label,
  labelColor = INK_DIM,
  right,
  rightColor = INK_DIM,
}: DocHeaderProps) {
  return (
    <g>
      <text
        x={x + 16}
        y={y + 20}
        fontFamily={MONO_FONT}
        fontSize={9.5}
        letterSpacing="0.2em"
        fill={labelColor}
      >
        {label}
      </text>
      <text
        x={x + w - 14}
        y={y + 20}
        textAnchor="end"
        fontFamily={MONO_FONT}
        fontSize={9}
        fill={rightColor}
        opacity={0.75}
      >
        {right}
      </text>
      <line x1={x} x2={x + w} y1={y + 30} y2={y + 30} stroke={HAIRLINE} />
    </g>
  );
}

/* ── Scene ───────────────────────────────────────────────────────────── */

export function GatewayScene() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const fade = 1 - ramp(t, FADE[0], FADE[1]);

    // ── REQUEST: the query document docks on the left and stays. ────
    const cardIn = easeInOutCubic(ramp(t, REQ_IN, REQ_IN + 350));
    h.setPop("reqCard", cardIn * fade, cardIn);
    REQ_LINES.forEach((_, i) => {
      const lineIn = ramp(t, 750 + i * 90, 1010 + i * 90);
      const isField = REQ_FIELD_LINES.indexOf(i as 2 | 3 | 4);
      if (isField >= 0) {
        const cf = ramp(t, PLAN_ROW[isField], PLAN_ROW[isField] + 350);
        h.setO(`reqL${i}`, lineIn * (1 - cf));
        h.setO(`reqOv${isField}`, lineIn * cf);
      } else {
        h.setO(`reqL${i}`, lineIn);
      }
    });
    if (t >= REQ_PULSE[0] && t < REQ_PULSE[1]) {
      const u = easeInOutCubic(ramp(t, REQ_PULSE[0], REQ_PULSE[1]));
      const op =
        Math.min(ramp(t, REQ_PULSE[0], REQ_PULSE[0] + 120), 1) *
        (1 - ramp(t, REQ_PULSE[1] - 100, REQ_PULSE[1]));
      h.placePulse("pIn", LANE_IN.poly, u, op, 3);
    } else {
      h.hidePulse("pIn");
    }
    h.setRing("ringGw", (t - REQ_PULSE[1]) / 500, 6, 18);

    // The gateway wakes when the request arrives.
    const lit =
      0.18 * ramp(t, REQ_IN, REQ_IN + 350) +
      0.32 * ramp(t, REQ_PULSE[1], REQ_PULSE[1] + 300);
    const flash =
      0.5 *
      ramp(t, MERGE_AT, MERGE_AT + 250) *
      (1 - ramp(t, MERGE_AT + 250, MERGE_AT + 1100));
    h.setO("gwBorder", (lit + flash) * fade);

    // ── PLAN: the plan builds inside the gateway, row by row. ────────
    h.setO("planLbl", ramp(t, 4300, 4550) * fade);
    SUBS.forEach((_, i) => {
      const rowIn = easeInOutCubic(ramp(t, PLAN_ROW[i], PLAN_ROW[i] + 350));
      h.setPop(`planRow${i}`, rowIn * fade, rowIn);
      // The parent reqCard group already applies the end fade.
      h.setO(`reqTick${i}`, ramp(t, PLAN_ROW[i], PLAN_ROW[i] + 350));
    });

    // ── EXECUTE: pulses out, sub-queries dock, work, answers return. ─
    SUBS.forEach((sub, i) => {
      const arrive = EXEC_DEPART[i] + FLIGHT;
      if (t >= EXEC_DEPART[i] && t < arrive) {
        const u = easeInOutCubic(ramp(t, EXEC_DEPART[i], arrive));
        const op =
          Math.min(ramp(t, EXEC_DEPART[i], EXEC_DEPART[i] + 120), 1) *
          (1 - ramp(t, arrive - 100, arrive));
        h.placePulse(`pOut${i}`, LANES[i].poly, u, op, 2.6);
      } else {
        h.hidePulse(`pOut${i}`);
      }
      h.setRing(`ringS${i}`, (t - arrive) / 450, 6, 14);

      // The sub-query docks and holds, then flips to the JSON answer.
      const qIn = easeInOutCubic(ramp(t, arrive, arrive + 300));
      const qOut = 1 - ramp(t, FLIP[i], FLIP[i] + 260);
      h.setPop(`subQ${i}`, qIn * qOut * fade, qIn);
      const jIn = ramp(t, FLIP[i] + 120, FLIP[i] + 380);
      const jDim = 1 - 0.6 * ramp(t, RET_DEPART[i] + 200, RET_DEPART[i] + 700);
      h.setO(`subJ${i}`, jIn * jDim * fade);

      // Working shimmer while the service resolves.
      const busy = t >= arrive + 200 && t < FLIP[i] ? 1 : 0;
      h.setO(`busy${i}`, busy * (0.4 + 0.3 * Math.sin(t / 160)) * fade);

      // The answer travels home.
      if (t >= RET_DEPART[i] && t < RET_ARRIVE[i]) {
        const u = easeInOutCubic(ramp(t, RET_DEPART[i], RET_ARRIVE[i]));
        const op =
          Math.min(ramp(t, RET_DEPART[i], RET_DEPART[i] + 120), 1) *
          (1 - ramp(t, RET_ARRIVE[i] - 100, RET_ARRIVE[i]));
        h.placePulse(`pBack${i}`, BACKS[i], u, op, 2.6);
      } else {
        h.hidePulse(`pBack${i}`);
      }
      h.setRing(`ringB${i}`, (t - RET_ARRIVE[i]) / 450, 5, 13);

      // Each homecoming checks off its plan row and lights its segment.
      h.setO(`chk${i}`, ramp(t, RET_ARRIVE[i], RET_ARRIVE[i] + 250) * fade);
      h.setO(
        `seg${i}`,
        0.9 * ramp(t, RET_ARRIVE[i], RET_ARRIVE[i] + 250) * fade,
      );
    });

    // ── MERGE: the merge bar unifies, briefly. ─────────────────────
    const sweep =
      ramp(t, MERGE_AT, MERGE_AT + 300) *
      (1 - ramp(t, MERGE_AT + 500, MERGE_AT + 1000));
    h.setO("sweep", sweep * 0.85);

    // ── RESPOND: one JSON document assembles under the request. ──────
    if (t >= RESP_PULSE[0] && t < RESP_PULSE[1]) {
      const u = easeInOutCubic(ramp(t, RESP_PULSE[0], RESP_PULSE[1]));
      const op =
        Math.min(ramp(t, RESP_PULSE[0], RESP_PULSE[0] + 120), 1) *
        (1 - ramp(t, RESP_PULSE[1] - 100, RESP_PULSE[1]));
      h.placePulse("pResp", LANE_RESP.poly, u, op, 3);
    } else {
      h.hidePulse("pResp");
    }
    h.setRing("ringResp", (t - RESP_PULSE[1]) / 500, 6, 16);
    const respIn = easeInOutCubic(ramp(t, RESP_IN, RESP_IN + 350));
    // Eases in with the request card so the loop wrap has no hard pop.
    h.setO("respGhost", 0.5 * (1 - respIn) * ramp(t, REQ_IN, REQ_IN + 350));
    h.setPop("respCard", respIn * fade, respIn);
    RESP_LINES.forEach((_, i) => {
      h.setO(
        `respL${i}`,
        ramp(t, RESP_IN + 150 + i * 80, RESP_IN + 410 + i * 80),
      );
    });

    // ── The phase strip narrates. ────────────────────────────────────
    PHASES.forEach((p, i) => {
      const active = t >= p.from && t < p.to ? 1 : 0;
      h.setO(`ph${i}`, 0.35 + 0.6 * active);
      h.setO(`phDot${i}`, active);
    });
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 1400 630" width="100%" className="block">
        <defs>
          <filter id="gw-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
          <radialGradient id="gw-stage" cx="50%" cy="50%" r="50%">
            <stop offset="0" stopColor="#f5f0ea" stopOpacity="0.07" />
            <stop offset="0.6" stopColor="#f5f0ea" stopOpacity="0.025" />
            <stop offset="1" stopColor="#f5f0ea" stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* Lanes. */}
        <path
          d={LANE_IN.d}
          fill="none"
          stroke="rgba(245,241,234,0.18)"
          strokeWidth={1.5}
        />
        <path
          d={LANE_RESP.d}
          fill="none"
          stroke="rgba(245,241,234,0.18)"
          strokeWidth={1.5}
        />
        {LANES.map((l, i) => (
          <path
            key={i}
            d={l.d}
            fill="none"
            stroke={CANON[SUBS[i].s].color}
            strokeOpacity={0.26}
            strokeWidth={1.5}
          />
        ))}
        {/* Quiet continuations keep wide screens alive. */}
        {SUBS.map((sub, i) => (
          <path
            key={i}
            d={`M${SUB_X + SUB_W + 2} ${SUB_DOCK_Y[i]} C ${SUB_X + SUB_W + 80} ${SUB_DOCK_Y[i]}, ${SUB_X + SUB_W + 110} ${SUB_DOCK_Y[i] + (i - 1) * 36}, 1450 ${SUB_DOCK_Y[i] + (i - 1) * 52}`}
            fill="none"
            stroke={CANON[sub.s].color}
            strokeOpacity={0.09}
            strokeWidth={1.5}
          />
        ))}

        {/* Column eyebrows. */}
        <text
          x={REQ.x}
          y={112}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.22em"
          fill={INK_DIM}
          opacity={0.7}
        >
          CLIENT
        </text>
        <text
          x={SUB_X}
          y={86}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.22em"
          fill={INK_DIM}
          opacity={0.7}
        >
          SUBGRAPHS
        </text>

        {/* ── The client request document. ── */}
        <g ref={set("reqCard")} opacity={1}>
          <rect
            x={REQ.x}
            y={REQ.y}
            width={REQ.w}
            height={REQ.h}
            rx={12}
            fill={CARD_FILL}
            stroke={CARD_STROKE}
          />
          <DocHeader
            x={REQ.x}
            y={REQ.y}
            w={REQ.w}
            label="REQUEST"
            right="POST /graphql"
          />
          <CodeLines
            set={set}
            prefix="reqL"
            lines={REQ_LINES}
            x={REQ.x + 18}
            y0={REQ.y + 52}
            lh={18.5}
            size={12.5}
            indent={15}
          />
          {/* Ownership overlays: the field lines, in their owner's color. */}
          {REQ_FIELD_LINES.map((li, i) => (
            <text
              key={i}
              ref={set(`reqOv${i}`)}
              x={REQ.x + 18 + REQ_LINES[li].ind * 15}
              y={REQ.y + 52 + li * 18.5}
              fontFamily={MONO_FONT}
              fontSize={12.5}
              fill={CANON[SUBS[i].s].color}
              opacity={1}
            >
              {REQ_LINES[li].t}
            </text>
          ))}
          {REQ_FIELD_LINES.map((li, i) => (
            <rect
              key={i}
              ref={set(`reqTick${i}`)}
              x={REQ.x + 8}
              y={REQ.y + 52 + li * 18.5 - 9}
              width={3}
              height={11}
              rx={1.5}
              fill={CANON[SUBS[i].s].color}
              opacity={1}
            />
          ))}
        </g>

        {/* ── The gateway: a machine, not a sun. ── */}
        <g>
          {/* Faint stage light so the machine holds the center. */}
          <circle
            cx={GW.x + GW.w / 2}
            cy={GW.y + GW.h / 2}
            r={210}
            fill="url(#gw-stage)"
          />
          <rect
            x={GW.x}
            y={GW.y}
            width={GW.w}
            height={GW.h}
            rx={14}
            fill={GW_FILL}
            stroke={GW_STROKE}
          />
          <rect
            ref={set("gwBorder")}
            x={GW.x}
            y={GW.y}
            width={GW.w}
            height={GW.h}
            rx={14}
            fill="none"
            stroke="#f5f0ea"
            strokeWidth={1.25}
            opacity={0.5}
          />
          {/* Merge glyph: three in, one out. */}
          <g
            stroke={INK_DIM}
            strokeWidth={1.3}
            strokeLinecap="round"
            fill="none"
          >
            <path d="M606 246 h7 M606 252 h7 M606 258 h7" />
            <path d="M613 246 L620 252 L613 258 M613 252 h7" />
            <path d="M620 252 h8" />
          </g>
          <text
            x={636}
            y={256}
            fontFamily={MONO_FONT}
            fontSize={11}
            letterSpacing="0.22em"
            fill={INK}
          >
            GATEWAY
          </text>
          <text
            x={710}
            y={274}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0.8}
          >
            plans · executes · merges
          </text>
          <line
            x1={GW.x}
            x2={GW.x + GW.w}
            y1={284}
            y2={284}
            stroke={HAIRLINE}
          />
          <text
            ref={set("planLbl")}
            x={608}
            y={303}
            fontFamily={MONO_FONT}
            fontSize={8.5}
            letterSpacing="0.2em"
            fill={INK_DIM}
            opacity={1}
          >
            QUERY PLAN
          </text>
          {SUBS.map((sub, i) => (
            <g key={i} ref={set(`planRow${i}`)} opacity={1}>
              <rect
                x={608}
                y={GW_ROW_Y[i] - 8}
                width={8}
                height={8}
                rx={2.5}
                fill={CANON[sub.s].color}
              />
              <text
                x={624}
                y={GW_ROW_Y[i]}
                fontFamily={MONO_FONT}
                fontSize={11}
                fill={CODE}
              >
                {CANON[sub.s].name}
              </text>
              <text
                x={786}
                y={GW_ROW_Y[i]}
                textAnchor="end"
                fontFamily={MONO_FONT}
                fontSize={10.5}
                fill={INK_DIM}
              >
                {sub.field}
              </text>
            </g>
          ))}
          {SUBS.map((sub, i) => (
            <text
              key={i}
              ref={set(`chk${i}`)}
              x={800}
              y={GW_ROW_Y[i]}
              fontFamily={MONO_FONT}
              fontSize={10}
              fill={CANON[sub.s].color}
              opacity={1}
            >
              ✓
            </text>
          ))}
          {/* The merge bar: three results become one. */}
          {SUBS.map((sub, i) => (
            <rect
              key={i}
              ref={set(`seg${i}`)}
              x={608 + i * 68}
              y={384}
              width={62}
              height={5}
              rx={2.5}
              fill={CANON[sub.s].color}
              opacity={0.9}
            />
          ))}
          <rect
            ref={set("sweep")}
            x={608}
            y={384}
            width={198}
            height={5}
            rx={2.5}
            fill="#f5f0ea"
            opacity={0}
          />
        </g>
        <circle
          ref={set("ringGw")}
          cx={588}
          cy={300}
          r={6}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />
        {SUBS.map((sub, i) => (
          <circle
            key={i}
            ref={set(`ringB${i}`)}
            cx={832}
            cy={LANE_OUT_Y[i]}
            r={5}
            fill="none"
            stroke={CANON[sub.s].color}
            strokeWidth={1.5}
            opacity={0}
          />
        ))}

        {/* ── The subgraphs. ── */}
        {SUBS.map((sub, i) => (
          <g key={i}>
            <rect
              x={SUB_X}
              y={SUB_Y[i]}
              width={SUB_W}
              height={SUB_H}
              rx={12}
              fill={CARD_FILL}
              stroke={CARD_STROKE}
            />
            <rect
              x={SUB_X + 14}
              y={SUB_Y[i] + 11}
              width={10}
              height={10}
              rx={3}
              fill={CANON[sub.s].color}
            />
            <text
              x={SUB_X + 32}
              y={SUB_Y[i] + 20}
              fontFamily={MONO_FONT}
              fontSize={11}
              fill={CODE}
            >
              {CANON[sub.s].name}
            </text>
            <text
              x={SUB_X + SUB_W - 14}
              y={SUB_Y[i] + 20}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8.5}
              fill={INK_DIM}
              opacity={0.6}
            >
              {sub.port}
            </text>
            <line
              x1={SUB_X}
              x2={SUB_X + SUB_W}
              y1={SUB_Y[i] + 30}
              y2={SUB_Y[i] + 30}
              stroke={HAIRLINE}
            />
            <rect
              ref={set(`busy${i}`)}
              x={SUB_X}
              y={SUB_Y[i]}
              width={SUB_W}
              height={SUB_H}
              rx={12}
              fill="none"
              stroke={CANON[sub.s].color}
              strokeOpacity={0.8}
              strokeWidth={1.25}
              opacity={0}
            />
            <circle
              ref={set(`ringS${i}`)}
              cx={SUB_X - 2}
              cy={SUB_DOCK_Y[i]}
              r={6}
              fill="none"
              stroke={CANON[sub.s].color}
              strokeWidth={1.5}
              opacity={0}
            />
            {/* Body: the sub-query docks here, then the JSON answer. The
                lines are driven as a group, so they register no refs. */}
            <g ref={set(`subQ${i}`)} opacity={0}>
              <CodeLines
                lines={subQueryLines(sub.field, CANON[sub.s].color)}
                x={SUB_X + 16}
                y0={SUB_Y[i] + 48}
                lh={15.5}
                size={11}
                indent={13}
              />
            </g>
            <g ref={set(`subJ${i}`)} opacity={1}>
              <CodeLines
                lines={subJsonLines(sub.json, CANON[sub.s].color)}
                x={SUB_X + 16}
                y0={SUB_Y[i] + 48}
                lh={15.5}
                size={11}
                indent={13}
              />
            </g>
          </g>
        ))}

        {/* ── The response document. A dashed ghost holds its place until
            the merged answer lands there. ── */}
        <g ref={set("respGhost")} opacity={0}>
          <rect
            x={RESP.x}
            y={RESP.y}
            width={RESP.w}
            height={RESP.h}
            rx={12}
            fill="none"
            stroke="rgba(245,241,234,0.16)"
            strokeDasharray="5 7"
          />
          <text
            x={RESP.x + 16}
            y={RESP.y + 20}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.2em"
            fill={INK_DIM}
            opacity={0.55}
          >
            RESPONSE
          </text>
        </g>
        <g ref={set("respCard")} opacity={1}>
          <rect
            x={RESP.x}
            y={RESP.y}
            width={RESP.w}
            height={RESP.h}
            rx={12}
            fill={CARD_FILL}
            stroke="rgba(102,190,119,0.4)"
          />
          <DocHeader
            x={RESP.x}
            y={RESP.y}
            w={RESP.w}
            label="RESPONSE"
            labelColor={GREEN}
            right="200 OK"
            rightColor={GREEN}
          />
          <CodeLines
            set={set}
            prefix="respL"
            lines={RESP_LINES}
            x={RESP.x + 18}
            y0={RESP.y + 48}
            lh={17}
            size={12}
            indent={14}
          />
        </g>
        <circle
          ref={set("ringResp")}
          cx={412}
          cy={458}
          r={6}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />

        {/* Traveling pulses: the only things that move. */}
        <PulseGlyph
          set={set}
          id="pIn"
          main={TEAL}
          soft="#d5fbf2"
          filter="gw-soft"
        />
        <PulseGlyph
          set={set}
          id="pResp"
          main={TEAL}
          soft="#d5fbf2"
          filter="gw-soft"
        />
        {SUBS.map((sub, i) => (
          <g key={i}>
            <PulseGlyph
              set={set}
              id={`pOut${i}`}
              main={CANON[sub.s].color}
              soft={CANON[sub.s].soft}
              filter="gw-soft"
            />
            <PulseGlyph
              set={set}
              id={`pBack${i}`}
              main={CANON[sub.s].color}
              soft={CANON[sub.s].soft}
              filter="gw-soft"
            />
          </g>
        ))}

        {/* The phase strip: the caption is part of the scene. The static
            frame shows the completed story, so RESPOND starts active. */}
        {PHASES.map((p, i) => {
          const x = 700 + (i - 2) * 140;
          const last = i === PHASES.length - 1;
          return (
            <g key={p.label}>
              <circle
                ref={set(`phDot${i}`)}
                cx={x}
                cy={594}
                r={2.5}
                fill="#f5f0ea"
                opacity={last ? 1 : 0}
              />
              <text
                ref={set(`ph${i}`)}
                x={x}
                y={614}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={11.5}
                letterSpacing="0.26em"
                fill="#f5f0ea"
                opacity={last ? 0.95 : 0.35}
              >
                {p.label.toUpperCase()}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}
