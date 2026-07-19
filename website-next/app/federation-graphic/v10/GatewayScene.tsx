"use client";

/**
 * The v10 hero scene: the canonical federation silhouette, animated.
 *
 * The client sits on top. Below it, the gateway; below that, the three
 * subgraphs in a row. The request document docks on the gateway's left,
 * the response on its right, so the whole exchange reads as one circuit:
 * down the left lane, through the plan, out to the services, back up the
 * right lane. There is exactly one response document: it assembles field
 * by field beside the gateway as results return, seals to 200 OK, and an
 * envelope delivers it up to the client.
 *
 * Inside the gateway the query plan is a real plan: step 1 runs Catalog
 * and Billing in parallel, step 2 runs Shipping directly beneath Catalog,
 * joined by a straight dependency edge that carries the weight value when
 * Catalog returns. Billing deliberately finishes last so the loop proves
 * Shipping waits only on Catalog, not on all of step 1. Plan chips show
 * live state: pending, executing (pulsing), done (check). Sub-queries
 * stay docked at the services for the whole loop; the returned fragment
 * appends beneath them. One loop is ~26 seconds.
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

const NODE = { x: 636, y: 40, w: 128, h: 44 } as const;
const OK_CHIP = { x: 772, y: 52, w: 62, h: 20 } as const;
const REQ = { x: 180, y: 150, w: 310, h: 170 } as const;
const RESP = { x: 910, y: 150, w: 310, h: 190 } as const;
const GW = { x: 550, y: 130, w: 300, h: 290 } as const;
const SUB_W = 280;
const SUB_H = 150;
const SUB_Y = 486;
const SUB_X = [220, 560, 900] as const;
const SUB_CX = SUB_X.map((x) => x + SUB_W / 2);

/* Plan chips: step 1 side by side, step 2 directly beneath Catalog so the
   dependency edge is a straight vertical drop. */
const CHIP = [
  { x: 592, y: 254, w: 120, h: 44 },
  { x: 722, y: 254, w: 116, h: 44 },
  { x: 592, y: 342, w: 120, h: 44 },
] as const;
const GW_OUT_X = [620, 700, 780] as const;

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

/* The one response: skeleton braces, then one field per returned result. */
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

/* The circuit: request down the left, response up the right. */
const LANE_REQ = lane([668, 86], [600, 140], [560, 190], [548, 236]);
const LANE_RESP = lane([852, 236], [840, 190], [800, 140], [732, 86]);

const LANES = [
  lane([GW_OUT_X[0], 420], [620, 452], [SUB_CX[0], 452], [SUB_CX[0], 484]),
  lane([GW_OUT_X[1], 420], [700, 444], [SUB_CX[1], 456], [SUB_CX[1], 484]),
  lane([GW_OUT_X[2], 420], [780, 452], [SUB_CX[2], 452], [SUB_CX[2], 484]),
] as const;
const BACKS = LANES.map((l) => measure([...l.poly.pts].reverse()));

/* The dependency edge: a straight drop from Catalog to Shipping. */
const DEP = lane([652, 298], [652, 312], [652, 328], [652, 342]);

/* ── Schedule ────────────────────────────────────────────────────────── */

const ENV_OUT = [900, 1650] as const;
const REQ_CARD = 1700;
const TAB_IN = 4000;
const PLAN_CHIP = [4400, 4650, 4900] as const;
const DEP_IN = 5200;
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
const OK_IN = 19500;
const FADE = [24800, 25600] as const;

/* Chip state windows: [pop, executing-from, done-at]. */
const CHIP_STATE = [
  [PLAN_CHIP[0], EXEC1[0], BACK[0][1]],
  [PLAN_CHIP[1], EXEC1[1], BACK[1][1]],
  [PLAN_CHIP[2], EXEC2, BACK[2][1]],
] as const;

const PHASES = [
  { label: "request", from: 600, to: 4200 },
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
      <circle r={13} fill={stroke} opacity={0.14} filter="url(#gw10-soft)" />
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

interface GhostProps {
  readonly set: (k: string) => (node: SVGElement | null) => void;
  readonly id: string;
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly label: string;
}

/** A dashed placeholder that holds a document's place until it exists. */
function Ghost({ set, id, x, y, w, h, label }: GhostProps) {
  return (
    <g ref={set(id)} opacity={0}>
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        rx={12}
        fill="none"
        stroke="rgba(245,241,234,0.16)"
        strokeDasharray="5 7"
      />
      <text
        x={x + 16}
        y={y + 20}
        fontFamily={MONO_FONT}
        fontSize={9.5}
        letterSpacing="0.2em"
        fill={INK_DIM}
        opacity={0.55}
      >
        {label}
      </text>
    </g>
  );
}

/* ── Scene ───────────────────────────────────────────────────────────── */

export function GatewayScene() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const fade = 1 - ramp(t, FADE[0], FADE[1]);
    const intro = ramp(t, 300, 800);

    // ── REQUEST: the envelope rides down; the document docks. ────────
    if (t >= ENV_OUT[0] && t < ENV_OUT[1]) {
      const u = easeInOutCubic(ramp(t, ENV_OUT[0], ENV_OUT[1]));
      const [x, y] = pointAt(LANE_REQ.poly, u);
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
    const cardIn = easeInOutCubic(ramp(t, REQ_CARD, REQ_CARD + 350));
    h.setO("reqGhost", 0.5 * (1 - cardIn) * intro);
    h.setPop("reqCard", cardIn * fade, cardIn);
    REQ_LINES.forEach((_, i) => {
      const lineIn = ramp(t, REQ_CARD + 100 + i * 90, REQ_CARD + 360 + i * 90);
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

    // The gateway wakes, docks the request as a tab, and plans.
    const lit = 0.18 * intro + 0.32 * ramp(t, ENV_OUT[1], ENV_OUT[1] + 300);
    const flash =
      0.5 *
      ramp(t, DONE_AT, DONE_AT + 250) *
      (1 - ramp(t, DONE_AT + 250, DONE_AT + 1100));
    h.setO("gwBorder", (lit + flash) * fade);
    const tabIn = ramp(t, TAB_IN, TAB_IN + 300);
    h.setPop("tab", tabIn * fade, 1);
    h.setO("gwIdle", 0.35 * (1 - tabIn) * intro);

    // ── PLAN: two steps, one dependency. ─────────────────────────────
    h.setO("planLbl", ramp(t, 4300, 4550) * fade);
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
    h.setO("planMeta", 0.6 * ramp(t, PLAN_CHIP[2], PLAN_CHIP[2] + 350) * fade);
    h.setO(
      "needsWeight",
      0.65 * ramp(t, PLAN_CHIP[2], PLAN_CHIP[2] + 350) * fade,
    );
    const depBoost = t >= WSLIDE[0] && t < WSLIDE[1] + 200 ? 0.4 : 0;
    h.setO("depEdge", (0.5 + depBoost) * ramp(t, DEP_IN, DEP_IN + 400) * fade);
    h.setO("depLbl", (0.65 + depBoost) * ramp(t, DEP_IN, DEP_IN + 400) * fade);

    // The weight value drops along the dependency edge.
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
      h.setO(`idle${i}`, 0.3 * (1 - qIn) * intro);

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

    // ── The one response, assembled field by field on the right. ─────
    const bufIn = easeInOutCubic(ramp(t, BUF_IN, BUF_IN + 350));
    h.setO("respGhost", 0.5 * (1 - bufIn) * intro);
    h.setPop("resp", bufIn * fade, bufIn);
    OUT_FIELD_LINES.forEach((li, i) => {
      h.setO(`respL${li}`, ramp(t, BACK[i][1], BACK[i][1] + 300));
      h.setO(`respTick${i}`, ramp(t, BACK[i][1], BACK[i][1] + 300));
    });
    h.setO("respBuilding", (1 - ramp(t, DONE_AT, DONE_AT + 300)) * 0.75);
    h.setO("respOk", ramp(t, DONE_AT, DONE_AT + 300));
    h.setO(
      "respFlash",
      0.7 *
        ramp(t, DONE_AT, DONE_AT + 250) *
        (1 - ramp(t, DONE_AT + 400, DONE_AT + 1100)),
    );

    // ── RESPOND: the sealed response rides up to the client. ─────────
    if (t >= ENV_BACK[0] && t < ENV_BACK[1]) {
      const u = easeInOutCubic(ramp(t, ENV_BACK[0], ENV_BACK[1]));
      const [x, y] = pointAt(LANE_RESP.poly, u);
      h.setX("envResp", x, y);
      h.setO(
        "envResp",
        Math.min(ramp(t, ENV_BACK[0], ENV_BACK[0] + 150), 1) *
          (1 - ramp(t, ENV_BACK[1] - 120, ENV_BACK[1])),
      );
    } else {
      h.setO("envResp", 0);
    }
    h.setRing("ringClient", (t - ENV_BACK[1]) / 500, 6, 16);
    h.setPop("okChip", ramp(t, OK_IN, OK_IN + 300) * fade, 1);

    // ── The phase strip narrates. ────────────────────────────────────
    PHASES.forEach((p, i) => {
      const active = t >= p.from && t < p.to ? 1 : 0;
      h.setO(`ph${i}`, 0.35 + 0.6 * active);
      h.setO(`phDot${i}`, active);
    });
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 1400 740" width="100%" className="block">
        <defs>
          <filter id="gw10-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
          <radialGradient id="gw10-stage" cx="50%" cy="50%" r="50%">
            <stop offset="0" stopColor="#f5f0ea" stopOpacity="0.07" />
            <stop offset="0.6" stopColor="#f5f0ea" stopOpacity="0.025" />
            <stop offset="1" stopColor="#f5f0ea" stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* Lanes: the circuit, and the fan to the services. */}
        <path
          d={LANE_REQ.d}
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
            stroke={SUBS[i].svc.color}
            strokeOpacity={0.26}
            strokeWidth={1.5}
          />
        ))}
        {/* Quiet continuations keep wide screens alive. */}
        <path
          d="M0 235 C 60 235, 120 235, 178 235"
          fill="none"
          stroke="#f5f0ea"
          strokeOpacity={0.07}
          strokeWidth={1.5}
        />
        <path
          d="M1222 235 C 1280 235, 1340 235, 1400 235"
          fill="none"
          stroke="#f5f0ea"
          strokeOpacity={0.07}
          strokeWidth={1.5}
        />
        {/* Docking stubs: the documents plug into the gateway's sides. */}
        <line
          x1={REQ.x + REQ.w}
          x2={GW.x}
          y1={235}
          y2={235}
          stroke={HAIRLINE}
          strokeDasharray="3 5"
        />
        <line
          x1={GW.x + GW.w}
          x2={RESP.x}
          y1={235}
          y2={235}
          stroke={HAIRLINE}
          strokeDasharray="3 5"
        />

        {/* ── The client, on top. ── */}
        <text
          x={700}
          y={30}
          textAnchor="middle"
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
        <g stroke={INK_DIM} strokeWidth={1.2} fill="none">
          <rect x={652} y={56} width={14} height={11} rx={2} />
          <line x1={652} x2={666} y1={59.5} y2={59.5} />
        </g>
        <text x={674} y={66} fontFamily={MONO_FONT} fontSize={10} fill={INK}>
          web-app
        </text>
        <circle
          ref={set("ringClient")}
          cx={732}
          cy={86}
          r={6}
          fill="none"
          stroke={GREEN}
          strokeWidth={1.5}
          opacity={0}
        />
        {/* Delivery receipt. */}
        <g ref={set("okChip")} opacity={1}>
          <rect
            x={OK_CHIP.x}
            y={OK_CHIP.y}
            width={OK_CHIP.w}
            height={OK_CHIP.h}
            rx={6}
            fill="rgba(102,190,119,0.08)"
            stroke="rgba(102,190,119,0.45)"
          />
          <text
            x={OK_CHIP.x + OK_CHIP.w / 2}
            y={OK_CHIP.y + 14}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={GREEN}
          >
            200 OK
          </text>
        </g>

        {/* ── The request document, docked left of the gateway. ── */}
        <Ghost
          set={set}
          id="reqGhost"
          x={REQ.x}
          y={REQ.y}
          w={REQ.w}
          h={REQ.h}
          label="REQUEST"
        />
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

        {/* ── The gateway, center stage. ── */}
        <g>
          <circle cx={700} cy={275} r={230} fill="url(#gw10-stage)" />
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
            <path d="M566 148 h7 M566 154 h7 M566 160 h7" />
            <path d="M573 148 L580 154 L573 160 M573 154 h7" />
            <path d="M580 154 h8" />
          </g>
          <text
            x={596}
            y={159}
            fontFamily={MONO_FONT}
            fontSize={11}
            letterSpacing="0.22em"
            fill={INK}
          >
            GATEWAY
          </text>
          <text
            x={GW.x + GW.w - 16}
            y={159}
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
            y1={176}
            y2={176}
            stroke={HAIRLINE}
          />
          <text
            ref={set("gwIdle")}
            x={700}
            y={300}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.14em"
            fill={INK_DIM}
            opacity={0}
          >
            waiting for request…
          </text>

          {/* The received request, docked as a tab. */}
          <g ref={set("tab")} opacity={1}>
            <rect
              x={566}
              y={190}
              width={158}
              height={22}
              rx={6}
              fill="rgba(94,234,212,0.08)"
              stroke="rgba(94,234,212,0.4)"
            />
            <rect x={566} y={190} width={3} height={22} rx={1.5} fill={TEAL} />
            <text
              x={578}
              y={205}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill={CODE}
            >
              query GetProduct
            </text>
          </g>

          <text
            ref={set("planLbl")}
            x={566}
            y={242}
            fontFamily={MONO_FONT}
            fontSize={8.5}
            letterSpacing="0.2em"
            fill={INK_DIM}
            opacity={1}
          >
            QUERY PLAN
          </text>
          <text
            ref={set("planMeta")}
            x={GW.x + GW.w - 16}
            y={242}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={8}
            fill={INK_DIM}
            opacity={0.6}
          >
            2 steps · 1 dependency
          </text>

          {/* Step numerals on a left rail. */}
          <text
            ref={set("num1")}
            x={570}
            y={282}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0.55}
          >
            1
          </text>
          <text
            ref={set("num2")}
            x={570}
            y={370}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0.55}
          >
            2
          </text>

          {/* Dependency edge: Catalog's weight drops straight to Shipping. */}
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
            x={662}
            y={324}
            fontFamily={MONO_FONT}
            fontSize={8}
            fill={CAT.color}
            opacity={0.65}
          >
            weight
          </text>
          <text
            ref={set("needsWeight")}
            x={722}
            y={368}
            fontFamily={MONO_FONT}
            fontSize={8}
            fill={INK_DIM}
            opacity={0.65}
          >
            needs weight from Catalog
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
                y={c.y + 10}
                width={8}
                height={8}
                rx={2.5}
                fill={SUBS[i].svc.color}
              />
              <text
                x={c.x + 24}
                y={c.y + 18}
                fontFamily={MONO_FONT}
                fontSize={10}
                fill={CODE}
              >
                {SUBS[i].svc.name}
              </text>
              <text
                x={c.x + 10}
                y={c.y + 34}
                fontFamily={MONO_FONT}
                fontSize={8.5}
                fill={INK_DIM}
                opacity={0.85}
              >
                {SUBS[i].fields}
              </text>
              <circle
                ref={set(`dot${i}`)}
                cx={c.x + c.w - 13}
                cy={c.y + 15}
                r={3}
                fill={SUBS[i].svc.color}
                opacity={0}
              />
              <text
                ref={set(`chk${i}`)}
                x={c.x + c.w - 18}
                y={c.y + 19}
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
          cx={548}
          cy={236}
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

        {/* ── The one response, assembled right of the gateway. ── */}
        <Ghost
          set={set}
          id="respGhost"
          x={RESP.x}
          y={RESP.y}
          w={RESP.w}
          h={RESP.h}
          label="RESPONSE"
        />
        <g ref={set("resp")} opacity={1}>
          <rect
            x={RESP.x}
            y={RESP.y}
            width={RESP.w}
            height={RESP.h}
            rx={12}
            fill={CARD_FILL}
            stroke="rgba(102,190,119,0.4)"
          />
          <rect
            ref={set("respFlash")}
            x={RESP.x}
            y={RESP.y}
            width={RESP.w}
            height={RESP.h}
            rx={12}
            fill="none"
            stroke="rgba(102,190,119,0.8)"
            strokeWidth={1.25}
            opacity={0}
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
            ref={set("respBuilding")}
            x={RESP.x + RESP.w - 14}
            y={RESP.y + 20}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0}
          >
            building…
          </text>
          <text
            ref={set("respOk")}
            x={RESP.x + RESP.w - 14}
            y={RESP.y + 20}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={GREEN}
            opacity={1}
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
          {OUT_FIELD_LINES.map((li, i) => (
            <rect
              key={i}
              ref={set(`respTick${i}`)}
              x={RESP.x + 8}
              y={RESP.y + 46 + li * 16 - 8}
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
            cx={GW_OUT_X[i]}
            cy={422}
            r={5}
            fill="none"
            stroke={SUBS[i].svc.color}
            strokeWidth={1.5}
            opacity={0}
          />
        ))}

        {/* ── The subgraphs, in a row below. ── */}
        <text
          x={SUB_X[0]}
          y={470}
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
              x={SUB_X[i]}
              y={SUB_Y}
              width={SUB_W}
              height={SUB_H}
              rx={12}
              fill={CARD_FILL}
              stroke={CARD_STROKE}
            />
            <rect
              x={SUB_X[i] + 14}
              y={SUB_Y + 11}
              width={10}
              height={10}
              rx={3}
              fill={sub.svc.color}
            />
            <text
              x={SUB_X[i] + 32}
              y={SUB_Y + 20}
              fontFamily={MONO_FONT}
              fontSize={11}
              fill={CODE}
            >
              {sub.svc.name}
            </text>
            <text
              x={SUB_X[i] + SUB_W - 14}
              y={SUB_Y + 20}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8.5}
              fill={INK_DIM}
              opacity={0.6}
            >
              {sub.port}
            </text>
            <line
              x1={SUB_X[i]}
              x2={SUB_X[i] + SUB_W}
              y1={SUB_Y + 30}
              y2={SUB_Y + 30}
              stroke={HAIRLINE}
            />
            <rect
              ref={set(`busy${i}`)}
              x={SUB_X[i]}
              y={SUB_Y}
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
              cx={SUB_CX[i]}
              cy={SUB_Y - 2}
              r={6}
              fill="none"
              stroke={sub.svc.color}
              strokeWidth={1.5}
              opacity={0}
            />
            <text
              ref={set(`idle${i}`)}
              x={SUB_CX[i]}
              y={SUB_Y + 95}
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
                x1={SUB_X[i]}
                x2={SUB_X[i] + SUB_W}
                y1={SUB_Y + 116}
                y2={SUB_Y + 116}
                stroke={HAIRLINE}
              />
              <text
                x={SUB_X[i] + 16}
                y={SUB_Y + 136}
                fontFamily={MONO_FONT}
                fontSize={10}
                fill={sub.svc.color}
              >
                {sub.result}
              </text>
            </g>
          </g>
        ))}

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
              filter="gw10-soft"
            />
            <PulseGlyph
              set={set}
              id={`pBack${i}`}
              main={sub.svc.color}
              soft={sub.svc.soft}
              filter="gw10-soft"
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
                cy={694}
                r={2.5}
                fill="#f5f0ea"
                opacity={last ? 1 : 0}
              />
              <text
                ref={set(`ph${i}`)}
                x={x}
                y={714}
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
  const x = SUB_X[i] + 16;
  const lh = 15;
  const y0 = SUB_Y + 48;
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
