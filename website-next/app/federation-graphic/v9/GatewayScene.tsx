"use client";

/**
 * The v9 hero scene: the query plan is a real plan.
 *
 * One client node on the left holds one lane to the gateway; the request
 * and response are cargo on that lane (traveling envelopes), not endpoints.
 * The gateway panel builds a two-step plan: step 1 runs Catalog and Billing
 * in parallel, step 2 runs Shipping, which needs the product's weight from
 * Catalog first; the dependency is a drawn edge in the plan, and the weight
 * value visibly slides along it when Catalog returns. Every plan node shows
 * its live state: pending, executing (pulsing), done (check). Below the
 * gateway, the response JSON assembles field by field as results arrive.
 * Subgraph cards keep their incoming query on screen the whole time and
 * append the returned fragment beneath it. The loop ends with the sealed
 * response traveling back to the client. One loop is ~26 seconds.
 */

import { MONO_FONT } from "./palette";
import {
  easeInOutCubic,
  measure,
  pointAt,
  PulseGlyph,
  ramp,
  useVisual,
} from "./anim";
import { CANON, INK_DIM, sampleCubic } from "./stage";

const T = 26200;

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

const CAT = CANON[0];
const BIL = CANON[1];
const SHP = CANON[3];

/* ── Layout ──────────────────────────────────────────────────────────── */

const NODE = { x: 100, y: 170, w: 120, h: 44 } as const;
const REQ = { x: 90, y: 250, w: 310, h: 170 } as const;
const RESP = { x: 90, y: 446, w: 310, h: 190 } as const;
const GW = { x: 560, y: 150, w: 300, h: 300 } as const;
const BUF = { x: 560, y: 472, w: 300, h: 178 } as const;
const SUB_X = 1050;
const SUB_W = 280;
const SUB_H = 150;
const SUB_Y = [90, 268, 446] as const;

/* Plan chips inside the gateway: step 1 in parallel, step 2 below. */
const CHIP = [
  { x: 596, y: 272, w: 126, h: 38 },
  { x: 734, y: 272, w: 108, h: 38 },
  { x: 622, y: 354, w: 126, h: 38 },
] as const;

const LANE_OUT_Y = [250, 300, 350] as const;
const SUB_DOCK_Y = SUB_Y.map((y) => y + 18);

interface DocLine {
  readonly t: string;
  readonly ind: number;
  readonly c?: string;
  /** Initial opacity for the reduced-motion static frame. */
  readonly o?: number;
}

/* The client request. Lines 2..4 colorize to their owner during PLAN. */
const REQ_LINES: readonly DocLine[] = [
  { t: "query GetProduct {", ind: 0 },
  { t: 'product(id: "42") {', ind: 1 },
  { t: "name", ind: 2, o: 0 },
  { t: "price", ind: 2, o: 0 },
  { t: "deliveryEta", ind: 2, o: 0 },
  { t: "}", ind: 1, c: DIM },
  { t: "}", ind: 0, c: DIM },
];
const REQ_FIELD_LINES = [2, 3, 4] as const;
const REQ_FIELD_COLOR = [CAT.color, BIL.color, SHP.color] as const;

/* The response JSON: skeleton braces, then one field per returned result. */
const OUT_LINES: readonly DocLine[] = [
  { t: "{", ind: 0, c: DIM },
  { t: '"data": {', ind: 1 },
  { t: '"product": {', ind: 2 },
  { t: '"name": "Aero Mug",', ind: 3, c: CAT.color },
  { t: '"price": 24.90,', ind: 3, c: BIL.color },
  { t: '"deliveryEta": "2 days"', ind: 3, c: SHP.color },
  { t: "}", ind: 2, c: DIM },
  { t: "}", ind: 1, c: DIM },
  { t: "}", ind: 0, c: DIM },
];
const OUT_FIELD_LINES = [3, 4, 5] as const;

interface SubDef {
  readonly svc: (typeof CANON)[number];
  readonly port: string;
  readonly result: string;
  /** Plan chip caption: the fields this step resolves. */
  readonly fields: string;
}

const SUBS: readonly SubDef[] = [
  {
    svc: CAT,
    port: ":4001",
    result: '{ "name": "Aero Mug", "weight": 1.2 }',
    fields: "name · weight",
  },
  { svc: BIL, port: ":4002", result: '{ "price": 24.90 }', fields: "price" },
  {
    svc: SHP,
    port: ":4003",
    result: '{ "deliveryEta": "2 days" }',
    fields: "deliveryEta",
  },
];

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

/* One lane between client and gateway; the documents ride it both ways. */
const LANE_MAIN = lane([220, 192], [340, 192], [430, 280], [558, 280]);
const LANE_BACK = { poly: measure([...LANE_MAIN.poly.pts].reverse()) };

const LANES = SUBS.map((_, i) =>
  lane(
    [862, LANE_OUT_Y[i]],
    [950, LANE_OUT_Y[i]],
    [968, SUB_DOCK_Y[i]],
    [1048, SUB_DOCK_Y[i]],
  ),
);
const BACKS = LANES.map((l) => measure([...l.poly.pts].reverse()));

/* The dependency edge in the plan: Catalog's weight feeds Shipping. */
const DEP = lane([659, 310], [659, 334], [685, 332], [685, 354]);

/* ── Schedule ────────────────────────────────────────────────────────── */

const REQ_IN = 600;
const ENV_OUT = [3200, 3950] as const;
const TAB_IN = 3950;
const PLAN_CHIP = [4300, 4550, 4800] as const;
const DEP_IN = 5100;
const BUF_IN = 7100;
const EXEC1 = [7400, 7600] as const;
const FLIGHT = 700;
const SUB_ARRIVE = [EXEC1[0] + FLIGHT, EXEC1[1] + FLIGHT, 13400] as const;
const RES_AT = [10800, 13800, 16200] as const;
const BACK = [
  [11000, 11700],
  [14000, 14700],
  [16400, 17100],
] as const;
const WSLIDE = [11800, 12600] as const;
const EXEC2 = 12700;
const DONE_AT = 17400;
const DISPATCH = 18600;
const ENV_BACK = [18600, 19500] as const;
const RESP_IN = 19500;
const FADE = [24800, 25600] as const;

/* Chip state windows: [pop, executing-from, done-at]. */
const CHIP_STATE = [
  [PLAN_CHIP[0], EXEC1[0], BACK[0][1]],
  [PLAN_CHIP[1], EXEC1[1], BACK[1][1]],
  [PLAN_CHIP[2], EXEC2, BACK[2][1]],
] as const;

const PHASES = [
  { label: "request", from: REQ_IN, to: 4200 },
  { label: "plan", from: 4200, to: EXEC1[0] },
  { label: "execute", from: EXEC1[0], to: DONE_AT },
  { label: "merge", from: DONE_AT, to: DISPATCH },
  { label: "respond", from: DISPATCH, to: FADE[0] },
] as const;

/* ── Render helpers ──────────────────────────────────────────────────── */

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

interface EnvelopeProps {
  readonly set: (k: string) => (node: SVGElement | null) => void;
  readonly id: string;
  readonly stroke: string;
}

/** A document in transport: a small envelope riding a lane. */
function Envelope({ set, id, stroke }: EnvelopeProps) {
  return (
    <g ref={set(id)} opacity={0}>
      <circle r={13} fill={stroke} opacity={0.14} filter="url(#gw9-soft)" />
      <rect
        x={-12}
        y={-8.5}
        width={24}
        height={17}
        rx={3}
        fill="#0d1424"
        stroke={stroke}
        strokeWidth={1.25}
      />
      <line x1={-6} x2={6} y1={-2.5} y2={-2.5} stroke={DIM} strokeWidth={1} />
      <line x1={-6} x2={6} y1={2.5} y2={2.5} stroke={DIM} strokeWidth={1} />
    </g>
  );
}

/* ── Scene ───────────────────────────────────────────────────────────── */

export function GatewayScene() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const fade = 1 - ramp(t, FADE[0], FADE[1]);

    // ── REQUEST: the query docks at the client, then rides the lane. ─
    const cardIn = easeInOutCubic(ramp(t, REQ_IN, REQ_IN + 350));
    h.setPop("reqCard", cardIn * fade, cardIn);
    REQ_LINES.forEach((_, i) => {
      const lineIn = ramp(t, 750 + i * 90, 1010 + i * 90);
      const isField = REQ_FIELD_LINES.indexOf(i as 2 | 3 | 4);
      if (isField >= 0) {
        const cf = ramp(t, PLAN_CHIP[isField], PLAN_CHIP[isField] + 350);
        h.setO(`reqL${i}`, lineIn * (1 - cf));
        h.setO(`reqOv${isField}`, lineIn * cf);
      } else {
        h.setO(`reqL${i}`, lineIn);
      }
    });
    REQ_FIELD_LINES.forEach((_, i) => {
      h.setO(`reqTick${i}`, ramp(t, PLAN_CHIP[i], PLAN_CHIP[i] + 350));
    });
    if (t >= ENV_OUT[0] && t < ENV_OUT[1]) {
      const u = easeInOutCubic(ramp(t, ENV_OUT[0], ENV_OUT[1]));
      const [x, y] = pointAt(LANE_MAIN.poly, u);
      h.setX("envReq", x, y);
      h.setO(
        "envReq",
        Math.min(ramp(t, ENV_OUT[0], ENV_OUT[0] + 150), 1) *
          (1 - ramp(t, ENV_OUT[1] - 120, ENV_OUT[1])),
      );
    } else {
      h.setO("envReq", 0);
    }
    h.setRing("ringGw", (t - ENV_OUT[1]) / 500, 6, 18);

    // The gateway wakes and docks the request as a tab.
    const lit =
      0.18 * ramp(t, REQ_IN, REQ_IN + 350) +
      0.32 * ramp(t, ENV_OUT[1], ENV_OUT[1] + 300);
    const flash =
      0.5 *
      ramp(t, DONE_AT, DONE_AT + 250) *
      (1 - ramp(t, DONE_AT + 250, DONE_AT + 1100));
    h.setO("gwBorder", (lit + flash) * fade);
    const tabIn = ramp(t, TAB_IN, TAB_IN + 300);
    h.setPop("tab", tabIn * fade, 1);
    h.setO("gwIdle", 0.35 * (1 - tabIn) * ramp(t, 300, 800));

    // ── PLAN: two steps, one dependency. ─────────────────────────────
    h.setO("planLbl", ramp(t, 4200, 4450) * fade);
    CHIP.forEach((_, i) => {
      const pop = easeInOutCubic(
        ramp(t, CHIP_STATE[i][0], CHIP_STATE[i][0] + 350),
      );
      h.setPop(`chip${i}`, pop * fade, pop);
      const executing = t >= CHIP_STATE[i][1] && t < CHIP_STATE[i][2] ? 1 : 0;
      const done = ramp(t, CHIP_STATE[i][2], CHIP_STATE[i][2] + 250);
      h.setO(`chipRun${i}`, executing * (0.5 + 0.3 * Math.sin(t / 150)) * fade);
      h.setO(`chipDone${i}`, done * 0.5 * fade);
      h.setO(
        `dot${i}`,
        (executing ? 0.55 + 0.35 * Math.sin(t / 130) : 0.25 * pop) *
          (1 - done) *
          fade,
      );
      h.setO(`chk${i}`, done * fade);
    });
    h.setO(
      "num1",
      (0.55 + 0.35 * (t >= EXEC1[0] && t < BACK[1][1] ? 1 : 0)) *
        ramp(t, PLAN_CHIP[0], PLAN_CHIP[0] + 350) *
        fade,
    );
    h.setO(
      "num2",
      (0.55 + 0.35 * (t >= EXEC2 && t < BACK[2][1] ? 1 : 0)) *
        ramp(t, PLAN_CHIP[2], PLAN_CHIP[2] + 350) *
        fade,
    );
    const depBoost = t >= WSLIDE[0] && t < WSLIDE[1] + 200 ? 0.4 : 0;
    h.setO("depEdge", (0.5 + depBoost) * ramp(t, DEP_IN, DEP_IN + 400) * fade);
    h.setO("depLbl", (0.65 + depBoost) * ramp(t, DEP_IN, DEP_IN + 400) * fade);

    // The weight value slides along the dependency edge.
    if (t >= WSLIDE[0] && t < WSLIDE[1]) {
      const u = easeInOutCubic(ramp(t, WSLIDE[0], WSLIDE[1]));
      const [x, y] = pointAt(DEP.poly, u);
      h.setX("wchip", x, y);
      h.setO(
        "wchip",
        Math.min(ramp(t, WSLIDE[0], WSLIDE[0] + 150), 1) *
          (1 - ramp(t, WSLIDE[1] - 120, WSLIDE[1])),
      );
    } else {
      h.setO("wchip", 0);
    }

    // ── EXECUTE: pulses out, queries dock and stay, results return. ──
    SUBS.forEach((_, i) => {
      const depart = i === 2 ? EXEC2 : EXEC1[i];
      const arrive = SUB_ARRIVE[i];
      if (t >= depart && t < arrive) {
        const u = easeInOutCubic(ramp(t, depart, arrive));
        const op =
          Math.min(ramp(t, depart, depart + 120), 1) *
          (1 - ramp(t, arrive - 100, arrive));
        h.placePulse(`pOut${i}`, LANES[i].poly, u, op, 2.6);
      } else {
        h.hidePulse(`pOut${i}`);
      }
      h.setRing(`ringS${i}`, (t - arrive) / 450, 6, 14);

      // The query docks and holds for the rest of the loop.
      const qIn = easeInOutCubic(ramp(t, arrive, arrive + 300));
      h.setPop(`subQ${i}`, qIn * fade, qIn);
      h.setO(`idle${i}`, 0.3 * (1 - qIn) * ramp(t, 300, 800));

      // Working shimmer until the result is ready.
      const busy = t >= arrive + 200 && t < RES_AT[i] ? 1 : 0;
      h.setO(`busy${i}`, busy * (0.4 + 0.3 * Math.sin(t / 160)) * fade);

      // The result appends below the query and stays.
      h.setO(`subR${i}`, ramp(t, RES_AT[i], RES_AT[i] + 300) * fade);

      if (t >= BACK[i][0] && t < BACK[i][1]) {
        const u = easeInOutCubic(ramp(t, BACK[i][0], BACK[i][1]));
        const op =
          Math.min(ramp(t, BACK[i][0], BACK[i][0] + 120), 1) *
          (1 - ramp(t, BACK[i][1] - 100, BACK[i][1]));
        h.placePulse(`pBack${i}`, BACKS[i], u, op, 2.6);
      } else {
        h.hidePulse(`pBack${i}`);
      }
      h.setRing(`ringF${i}`, (t - BACK[i][1]) / 450, 5, 13);
    });

    // ── The response assembles under the gateway, field by field. ────
    const bufIn = easeInOutCubic(ramp(t, BUF_IN, BUF_IN + 350));
    const dimmed = 1 - 0.65 * ramp(t, DISPATCH, DISPATCH + 400);
    h.setPop("buf", bufIn * dimmed * fade, bufIn);
    OUT_FIELD_LINES.forEach((li, i) => {
      h.setO(`bufL${li}`, ramp(t, BACK[i][1], BACK[i][1] + 300));
      h.setO(`bufTick${i}`, ramp(t, BACK[i][1], BACK[i][1] + 300));
    });
    h.setO("bufBuilding", (1 - ramp(t, DONE_AT, DONE_AT + 300)) * 0.75);
    h.setO("bufOk", ramp(t, DONE_AT, DONE_AT + 300));
    h.setO(
      "bufFlash",
      0.7 *
        ramp(t, DONE_AT, DONE_AT + 250) *
        (1 - ramp(t, DONE_AT + 400, DONE_AT + 1100)),
    );

    // ── RESPOND: the sealed response rides home. ─────────────────────
    if (t >= ENV_BACK[0] && t < ENV_BACK[1]) {
      const u = easeInOutCubic(ramp(t, ENV_BACK[0], ENV_BACK[1]));
      const [x, y] = pointAt(LANE_BACK.poly, u);
      h.setX("envResp", x, y);
      h.setO(
        "envResp",
        Math.min(ramp(t, ENV_BACK[0], ENV_BACK[0] + 150), 1) *
          (1 - ramp(t, ENV_BACK[1] - 120, ENV_BACK[1])),
      );
    } else {
      h.setO("envResp", 0);
    }
    h.setRing("ringNode", (t - ENV_BACK[1]) / 500, 6, 16);
    const respIn = easeInOutCubic(ramp(t, RESP_IN, RESP_IN + 350));
    h.setO("respGhost", 0.5 * (1 - respIn) * ramp(t, REQ_IN, REQ_IN + 350));
    h.setPop("respCard", respIn * fade, respIn);
    OUT_LINES.forEach((_, i) => {
      h.setO(
        `respL${i}`,
        ramp(t, RESP_IN + 100 + i * 70, RESP_IN + 330 + i * 70),
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
      <svg viewBox="0 0 1400 700" width="100%" className="block">
        <defs>
          <filter id="gw9-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
          <radialGradient id="gw9-stage" cx="50%" cy="50%" r="50%">
            <stop offset="0" stopColor="#f5f0ea" stopOpacity="0.07" />
            <stop offset="0.6" stopColor="#f5f0ea" stopOpacity="0.025" />
            <stop offset="1" stopColor="#f5f0ea" stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* Lanes. */}
        <path
          d={LANE_MAIN.d}
          fill="none"
          stroke="rgba(245,241,234,0.18)"
          strokeWidth={1.5}
        />
        {LANES.map((l, i) => (
          <path
            key={i}
            d={l.d}
            fill="none"
            stroke={SUBS[i].svc.color}
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
            stroke={sub.svc.color}
            strokeOpacity={0.09}
            strokeWidth={1.5}
          />
        ))}

        {/* ── The client: one node, its documents stacked beneath. ── */}
        <text
          x={NODE.x}
          y={156}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.22em"
          fill={INK_DIM}
          opacity={0.7}
        >
          CLIENT
        </text>
        <rect
          x={NODE.x}
          y={NODE.y}
          width={NODE.w}
          height={NODE.h}
          rx={10}
          fill={GW_FILL}
          stroke={GW_STROKE}
        />
        {/* Browser glyph. */}
        <g stroke={INK_DIM} strokeWidth={1.2} fill="none">
          <rect x={114} y={186} width={14} height={11} rx={2} />
          <line x1={114} x2={128} y1={189.5} y2={189.5} />
        </g>
        <text x={136} y={196} fontFamily={MONO_FONT} fontSize={10} fill={INK}>
          web-app
        </text>
        <line
          x1={160}
          x2={160}
          y1={NODE.y + NODE.h}
          y2={REQ.y}
          stroke={HAIRLINE}
          strokeDasharray="3 5"
        />
        <circle
          ref={set("ringNode")}
          cx={222}
          cy={192}
          r={6}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />

        {/* The request document, held by the client. */}
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
          <text
            x={REQ.x + 16}
            y={REQ.y + 20}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.2em"
            fill={INK_DIM}
          >
            REQUEST
          </text>
          <text
            x={REQ.x + REQ.w - 14}
            y={REQ.y + 20}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0.75}
          >
            POST /graphql
          </text>
          <line
            x1={REQ.x}
            x2={REQ.x + REQ.w}
            y1={REQ.y + 30}
            y2={REQ.y + 30}
            stroke={HAIRLINE}
          />
          <CodeLines
            set={set}
            prefix="reqL"
            lines={REQ_LINES}
            x={REQ.x + 18}
            y0={REQ.y + 48}
            lh={17.5}
            size={12}
            indent={15}
          />
          {REQ_FIELD_LINES.map((li, i) => (
            <text
              key={i}
              ref={set(`reqOv${i}`)}
              x={REQ.x + 18 + REQ_LINES[li].ind * 15}
              y={REQ.y + 48 + li * 17.5}
              fontFamily={MONO_FONT}
              fontSize={12}
              fill={REQ_FIELD_COLOR[i]}
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
              y={REQ.y + 48 + li * 17.5 - 9}
              width={3}
              height={11}
              rx={1.5}
              fill={REQ_FIELD_COLOR[i]}
              opacity={1}
            />
          ))}
        </g>

        {/* ── The gateway: plan with steps, states, and a dependency. ── */}
        <g>
          <circle
            cx={GW.x + GW.w / 2}
            cy={GW.y + GW.h / 2}
            r={230}
            fill="url(#gw9-stage)"
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
            <path d="M578 170 h7 M578 176 h7 M578 182 h7" />
            <path d="M585 170 L592 176 L585 182 M585 176 h7" />
            <path d="M592 176 h8" />
          </g>
          <text
            x={608}
            y={181}
            fontFamily={MONO_FONT}
            fontSize={11}
            letterSpacing="0.22em"
            fill={INK}
          >
            GATEWAY
          </text>
          <text
            x={GW.x + GW.w - 16}
            y={181}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={INK_DIM}
            opacity={0.8}
          >
            plans · executes · merges
          </text>
          <line
            x1={GW.x}
            x2={GW.x + GW.w}
            y1={196}
            y2={196}
            stroke={HAIRLINE}
          />

          {/* The received request, docked as a tab. */}
          <g ref={set("tab")} opacity={1}>
            <rect
              x={578}
              y={210}
              width={158}
              height={22}
              rx={6}
              fill="rgba(94,234,212,0.08)"
              stroke="rgba(94,234,212,0.4)"
            />
            <rect x={578} y={210} width={3} height={22} rx={1.5} fill={TEAL} />
            <text
              x={590}
              y={225}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill={CODE}
            >
              query GetProduct
            </text>
          </g>

          <text
            ref={set("gwIdle")}
            x={710}
            y={330}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.14em"
            fill={INK_DIM}
            opacity={0}
          >
            waiting for request…
          </text>
          <text
            ref={set("planLbl")}
            x={578}
            y={258}
            fontFamily={MONO_FONT}
            fontSize={8.5}
            letterSpacing="0.2em"
            fill={INK_DIM}
            opacity={1}
          >
            QUERY PLAN
          </text>

          {/* Step numerals. */}
          <text
            ref={set("num1")}
            x={578}
            y={296}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0.55}
          >
            1
          </text>
          <text
            ref={set("num2")}
            x={578}
            y={378}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0.55}
          >
            2
          </text>

          {/* Dependency edge: Catalog's weight feeds Shipping. */}
          <path
            ref={set("depEdge")}
            d={DEP.d}
            fill="none"
            stroke={CAT.color}
            strokeWidth={1.25}
            strokeDasharray="3 4"
            opacity={0.5}
          />
          <text
            ref={set("depLbl")}
            x={714}
            y={336}
            fontFamily={MONO_FONT}
            fontSize={8}
            fill={CAT.color}
            opacity={0.65}
          >
            weight
          </text>

          {/* Plan chips with live state. */}
          {CHIP.map((c, i) => (
            <g key={i} ref={set(`chip${i}`)} opacity={1}>
              <rect
                x={c.x}
                y={c.y}
                width={c.w}
                height={c.h}
                rx={9}
                fill="#0d1424"
                stroke={CARD_STROKE}
              />
              <rect
                ref={set(`chipRun${i}`)}
                x={c.x}
                y={c.y}
                width={c.w}
                height={c.h}
                rx={9}
                fill="none"
                stroke={SUBS[i].svc.color}
                strokeWidth={1.25}
                opacity={0}
              />
              <rect
                ref={set(`chipDone${i}`)}
                x={c.x}
                y={c.y}
                width={c.w}
                height={c.h}
                rx={9}
                fill="none"
                stroke={SUBS[i].svc.color}
                strokeWidth={1}
                opacity={0.5}
              />
              <rect
                x={c.x + 10}
                y={c.y + 9}
                width={7}
                height={7}
                rx={2}
                fill={SUBS[i].svc.color}
              />
              <text
                x={c.x + 22}
                y={c.y + 16}
                fontFamily={MONO_FONT}
                fontSize={9}
                fill={CODE}
              >
                {SUBS[i].svc.name}
              </text>
              <text
                x={c.x + 10}
                y={c.y + 30}
                fontFamily={MONO_FONT}
                fontSize={8.5}
                fill={INK_DIM}
                opacity={0.85}
              >
                {SUBS[i].fields}
              </text>
              <circle
                ref={set(`dot${i}`)}
                cx={c.x + c.w - 12}
                cy={c.y + 13}
                r={2.8}
                fill={SUBS[i].svc.color}
                opacity={0}
              />
              <text
                ref={set(`chk${i}`)}
                x={c.x + c.w - 16}
                y={c.y + 17}
                fontFamily={MONO_FONT}
                fontSize={9.5}
                fill={SUBS[i].svc.color}
                opacity={1}
              >
                ✓
              </text>
            </g>
          ))}
        </g>
        <circle
          ref={set("ringGw")}
          cx={558}
          cy={280}
          r={6}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />
        {/* The weight value in transit along the dependency edge. */}
        <g ref={set("wchip")} opacity={0}>
          <rect
            x={-34}
            y={-9}
            width={68}
            height={18}
            rx={6}
            fill="#0d1424"
            stroke={CAT.color}
            strokeOpacity={0.9}
          />
          <text
            x={0}
            y={3.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={CAT.color}
          >
            weight: 1.2
          </text>
        </g>

        {/* Connector: the gateway writes the response below. */}
        <line
          x1={710}
          x2={710}
          y1={GW.y + GW.h}
          y2={BUF.y}
          stroke={HAIRLINE}
          strokeDasharray="3 5"
        />

        {/* ── The response, assembled field by field. ── */}
        <g ref={set("buf")} opacity={0.35}>
          <rect
            x={BUF.x}
            y={BUF.y}
            width={BUF.w}
            height={BUF.h}
            rx={12}
            fill={CARD_FILL}
            stroke={CARD_STROKE}
          />
          <rect
            ref={set("bufFlash")}
            x={BUF.x}
            y={BUF.y}
            width={BUF.w}
            height={BUF.h}
            rx={12}
            fill="none"
            stroke="rgba(102,190,119,0.8)"
            strokeWidth={1.25}
            opacity={0}
          />
          <text
            x={BUF.x + 16}
            y={BUF.y + 19}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.2em"
            fill={INK_DIM}
          >
            RESPONSE
          </text>
          <text
            ref={set("bufBuilding")}
            x={BUF.x + BUF.w - 14}
            y={BUF.y + 19}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0}
          >
            building…
          </text>
          <text
            ref={set("bufOk")}
            x={BUF.x + BUF.w - 14}
            y={BUF.y + 19}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={GREEN}
            opacity={1}
          >
            200 OK
          </text>
          <line
            x1={BUF.x}
            x2={BUF.x + BUF.w}
            y1={BUF.y + 28}
            y2={BUF.y + 28}
            stroke={HAIRLINE}
          />
          <CodeLines
            set={set}
            prefix="bufL"
            lines={OUT_LINES}
            x={BUF.x + 18}
            y0={BUF.y + 44}
            lh={15}
            size={10.5}
            indent={12}
          />
          {OUT_FIELD_LINES.map((li, i) => (
            <rect
              key={i}
              ref={set(`bufTick${i}`)}
              x={BUF.x + 8}
              y={BUF.y + 44 + li * 15 - 8}
              width={3}
              height={10}
              rx={1.5}
              fill={REQ_FIELD_COLOR[i]}
              opacity={1}
            />
          ))}
        </g>
        {SUBS.map((_, i) => (
          <circle
            key={i}
            ref={set(`ringF${i}`)}
            cx={862}
            cy={LANE_OUT_Y[i]}
            r={5}
            fill="none"
            stroke={SUBS[i].svc.color}
            strokeWidth={1.5}
            opacity={0}
          />
        ))}

        {/* ── The subgraphs: query stays, result appends. ── */}
        <text
          x={SUB_X}
          y={76}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.22em"
          fill={INK_DIM}
          opacity={0.7}
        >
          SUBGRAPHS
        </text>
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
              fill={sub.svc.color}
            />
            <text
              x={SUB_X + 32}
              y={SUB_Y[i] + 20}
              fontFamily={MONO_FONT}
              fontSize={11}
              fill={CODE}
            >
              {sub.svc.name}
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
              stroke={sub.svc.color}
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
              stroke={sub.svc.color}
              strokeWidth={1.5}
              opacity={0}
            />
            <text
              ref={set(`idle${i}`)}
              x={SUB_X + SUB_W / 2}
              y={SUB_Y[i] + 95}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing="0.24em"
              fill={INK_DIM}
              opacity={0}
            >
              IDLE
            </text>
            {/* The incoming query: docked, and it stays. */}
            <g ref={set(`subQ${i}`)} opacity={1}>
              <SubQuery i={i} />
            </g>
            {/* The returned fragment, appended below the query. */}
            <g ref={set(`subR${i}`)} opacity={1}>
              <line
                x1={SUB_X}
                x2={SUB_X + SUB_W}
                y1={SUB_Y[i] + 116}
                y2={SUB_Y[i] + 116}
                stroke={HAIRLINE}
              />
              <text
                x={SUB_X + 16}
                y={SUB_Y[i] + 136}
                fontFamily={MONO_FONT}
                fontSize={10}
                fill={sub.svc.color}
              >
                {sub.result}
              </text>
            </g>
          </g>
        ))}

        {/* ── The response, delivered. A ghost holds its place. ── */}
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
          <text
            x={RESP.x + 16}
            y={RESP.y + 20}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.2em"
            fill={GREEN}
          >
            RESPONSE
          </text>
          <text
            x={RESP.x + RESP.w - 14}
            y={RESP.y + 20}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={GREEN}
            opacity={0.75}
          >
            200 OK
          </text>
          <line
            x1={RESP.x}
            x2={RESP.x + RESP.w}
            y1={RESP.y + 30}
            y2={RESP.y + 30}
            stroke={HAIRLINE}
          />
          <CodeLines
            set={set}
            prefix="respL"
            lines={OUT_LINES}
            x={RESP.x + 18}
            y0={RESP.y + 46}
            lh={16}
            size={11.5}
            indent={13}
          />
        </g>

        {/* Documents in transport. */}
        <Envelope set={set} id="envReq" stroke={TEAL} />
        <Envelope set={set} id="envResp" stroke={GREEN} />

        {/* Data pulses. */}
        {SUBS.map((sub, i) => (
          <g key={i}>
            <PulseGlyph
              set={set}
              id={`pOut${i}`}
              main={sub.svc.color}
              soft={sub.svc.soft}
              filter="gw9-soft"
            />
            <PulseGlyph
              set={set}
              id={`pBack${i}`}
              main={sub.svc.color}
              soft={sub.svc.soft}
              filter="gw9-soft"
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
                cy={668}
                r={2.5}
                fill="#f5f0ea"
                opacity={last ? 1 : 0}
              />
              <text
                ref={set(`ph${i}`)}
                x={x}
                y={688}
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

/* ── Sub-query documents (with the dependency made visible) ──────────── */

interface SubQueryProps {
  readonly i: number;
}

/**
 * Catalog's query fetches name plus weight (underlined: fetched for the
 * plan, not the client). Shipping's query carries the weight value from
 * Catalog, rendered in Catalog's color to show its provenance.
 */
function SubQuery({ i }: SubQueryProps) {
  const y = SUB_Y[i];
  const x = SUB_X + 16;
  const lh = 15;
  const y0 = y + 48;
  const sub = SUBS[i];
  const common = { fontFamily: MONO_FONT, fontSize: 10.5 };
  const charW = 6.3;
  return (
    <g>
      <text x={x} y={y0} {...common} fill={DIM}>
        {"{"}
      </text>
      <text x={x + 13} y={y0 + lh} {...common} fill={CODE}>
        {'product(id: "42") {'}
      </text>
      {i === 0 && (
        <g>
          <text x={x + 26} y={y0 + 2 * lh} {...common} fill={sub.svc.color}>
            name
          </text>
          <text
            x={x + 26 + 5 * charW}
            y={y0 + 2 * lh}
            {...common}
            fill={sub.svc.soft}
          >
            weight
          </text>
          <line
            x1={x + 26 + 5 * charW}
            x2={x + 26 + 11 * charW}
            y1={y0 + 2 * lh + 3}
            y2={y0 + 2 * lh + 3}
            stroke={sub.svc.soft}
            strokeOpacity={0.7}
            strokeDasharray="2 3"
          />
        </g>
      )}
      {i === 1 && (
        <text x={x + 26} y={y0 + 2 * lh} {...common} fill={sub.svc.color}>
          price
        </text>
      )}
      {i === 2 && (
        <g>
          <text x={x + 26} y={y0 + 2 * lh} {...common} fill={sub.svc.color}>
            deliveryEta(
          </text>
          <text
            x={x + 26 + 12 * charW}
            y={y0 + 2 * lh}
            {...common}
            fill={CAT.color}
          >
            weight: 1.2
          </text>
          <text
            x={x + 26 + 23 * charW}
            y={y0 + 2 * lh}
            {...common}
            fill={sub.svc.color}
          >
            {")"}
          </text>
        </g>
      )}
      <text x={x + 13} y={y0 + 3 * lh} {...common} fill={DIM}>
        {"}"}
      </text>
      <text x={x} y={y0 + 4 * lh} {...common} fill={DIM}>
        {"}"}
      </text>
    </g>
  );
}
