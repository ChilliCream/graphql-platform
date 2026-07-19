"use client";

/**
 * The v12 hero scene: the v11 flow, slowed down and narrated.
 *
 * A hovering guide pointer glides between points of interest, each beat
 * pairing it with a short note and leader line; the notes are the hero's
 * only copy, so the page needs no subtitle. Step 1 dispatches both
 * services together, but the guide explains them one at a time, each
 * beat held long enough to read. The story plays once (~40 seconds),
 * holds the finished scene, and can be replayed by dragging the
 * timeline at the bottom.
 *
 * The request document sits on the left, the gateway in the middle, the
 * single response document on the right; both card frames are always
 * visible, and quiet lanes continue off both screen edges toward the
 * unseen client. Inside the gateway the query plan also runs left to
 * right: step 1 is a column of two chips queried in parallel (Billing on
 * top, whose price needs no requirement and answers first; Catalog
 * below), step 2 is Shipping to their right, joined by a horizontal
 * dependency edge that carries the weight value when Catalog returns.
 * The three subgraphs sit in a row underneath in the same order; their
 * queries dock and stay, results append below. The response assembles
 * field by field (pending fields hold dim dashed placeholder rows),
 * seals to 200 OK, and an envelope carries it off the right edge.
 */

import { useState } from "react";

import { MONO_FONT } from "./palette";
import {
  clamp01,
  easeInOutCubic,
  easeOutCubic,
  Envelope,
  measure,
  pointAt,
  PulseGlyph,
  ramp,
  useVisual,
} from "./anim";
import { CANON, INK_DIM, sampleCubic } from "./stage";
import { driveFlanks, FlankLayer, FlankSwitcher } from "./flanks";
import type { FlankVariant } from "./flanks";

/* One-shot: the story plays once, the clock stops here, and the finished
   scene holds. The timeline scrubs it back. */
const T = 39600;

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

const REQ = { x: 120, y: 170, w: 310, h: 170 } as const;
const RESP = { x: 960, y: 160, w: 310, h: 190 } as const;
const GW = { x: 540, y: 150, w: 340, h: 192 } as const;
const MID_Y = 246;
const SUB_W = 280;
const SUB_H = 150;
const SUB_Y = 450;
const SUB_X = [220, 560, 900] as const;
const SUB_CX = SUB_X.map((x) => x + SUB_W / 2);

/* Plan chips: step 1 is the left column (parallel), step 2 the right.
   Billing sits alone on the top row (no requirement); Catalog and
   Shipping share the bottom row so the dependency edge is horizontal. */
const CHIP = [
  { x: 556, y: 224, w: 116, h: 44 },
  { x: 556, y: 282, w: 116, h: 44 },
  { x: 748, y: 282, w: 116, h: 44 },
] as const;
const GW_OUT_X = [630, 710, 790] as const;

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
  { t: "price", ind: 2, o: 0 },
  { t: "name", ind: 2, o: 0 },
  { t: "deliveryEta", ind: 2, o: 0 },
  { t: "}", ind: 1, c: DIM },
  { t: "}", ind: 0, c: DIM },
];
const REQ_FIELD_LINES = [2, 3, 4] as const;
const REQ_FIELD_COLOR = [BIL.color, CAT.color, SHP.color] as const;

/* The one response: skeleton braces, then one field per returned result. */
const OUT_LINES: readonly DocLine[] = [
  { t: "{", ind: 0, c: DIM },
  { t: '"data": {', ind: 1 },
  { t: '"product": {', ind: 2 },
  { t: '"price": 24.90,', ind: 3, c: BIL.color },
  { t: '"name": "Aero Mug",', ind: 3, c: CAT.color },
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

/* Story order: Billing first (price resolves with no requirement), then
   Catalog (whose weight feeds Shipping), then Shipping. */
const SUBS: readonly SubDef[] = [
  { svc: BIL, port: ":4002", result: '{ "price": 24.90 }', fields: "price" },
  {
    svc: CAT,
    port: ":4001",
    result: '{ "name": "Aero Mug", "weight": 1.2 }',
    fields: "name · weight",
  },
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

/* The row: world in, request to gateway, gateway to response, world out. */
const LANE_SEND = lane(
  [REQ.x + REQ.w + 4, MID_Y],
  [480, MID_Y],
  [510, MID_Y],
  [GW.x - 2, MID_Y],
);
const LANE_OUT = lane(
  [RESP.x + RESP.w + 4, MID_Y],
  [1320, MID_Y],
  [1380, MID_Y],
  [1450, MID_Y],
);

const LANES = [
  lane([GW_OUT_X[0], 342], [630, 396], [SUB_CX[0], 396], [SUB_CX[0], 448]),
  lane([GW_OUT_X[1], 342], [710, 380], [SUB_CX[1], 400], [SUB_CX[1], 448]),
  lane([GW_OUT_X[2], 342], [790, 396], [SUB_CX[2], 396], [SUB_CX[2], 448]),
] as const;
const BACKS = LANES.map((l) => measure([...l.poly.pts].reverse()));

/* The dependency edge: horizontal along the bottom chip row, Catalog to
   Shipping. */
const DEP_Y = 304;
const DEP = lane([672, DEP_Y], [696, DEP_Y], [724, DEP_Y], [748, DEP_Y]);

/* The query's arrival from the unseen client: one continuous journey
   from the far LEFT screen edge (the flank leg, 1000 flank units) into
   the request card. Both legs share one clock and easing, so the
   envelope crosses the seam between the flank SVG and this one without
   a velocity jump. */
const ENTRY_VIS = measure([
  [0, MID_Y],
  [REQ.x - 2, MID_Y],
]);
const ENTRY_FLANK_LEN = 1000;
const ENTRY_TOTAL = ENTRY_FLANK_LEN + ENTRY_VIS.total;

/* ── Schedule ────────────────────────────────────────────────────────── */

const REQ_CARD = 800;
const ENV_SEND = [4300, 5000] as const;
const PLAN_CHIP = [5500, 5850, 6200] as const;
const DEP_IN = 6600;
const BUF_IN = 9000;
const EXEC1 = [9400, 9650] as const;
const FLIGHT = 800;
const EXEC2 = 28400;
const SUB_ARRIVE = [
  EXEC1[0] + FLIGHT,
  EXEC1[1] + FLIGHT,
  EXEC2 + FLIGHT,
] as const;
/* Both step-1 requests are sent together, but the guide explains one
   service at a time, so each result arrives inside its own beat:
   Billing first (nothing to wait for), Catalog while its beat runs.
   Every event lands ~1s into its beat, after the note is readable. */
const RES_AT = [13800, 18000, 31800] as const;
const BACK = [
  [14000, 14800],
  [18200, 19000],
  [32000, 32800],
] as const;
const WSLIDE = [25400, 26400] as const;
const DONE_AT = 33600;
const ENV_OUT = [37400, 38300] as const;

/* Chip state windows: [pop, executing-from, done-at]. */
const CHIP_STATE = [
  [PLAN_CHIP[0], EXEC1[0], BACK[0][1]],
  [PLAN_CHIP[1], EXEC1[1], BACK[1][1]],
  [PLAN_CHIP[2], EXEC2, BACK[2][1]],
] as const;

interface Beat {
  readonly from: number;
  readonly to: number;
  /** Short label for this beat's dot on the timeline. */
  readonly tag: string;
  /** Where the guide pointer hovers. */
  readonly target: readonly [number, number];
  /** Center of the note's first text line. */
  readonly note: readonly [number, number];
  /** Leader from the note toward the target, as polyline points. */
  readonly lead: string;
  readonly lines: readonly [string, string];
}

/* The guide: a hovering pointer that glides between points of interest,
   with a short two-line note per beat. One service is explained at a
   time even though step 1 dispatches in parallel. */
const BEATS: readonly Beat[] = [
  {
    from: 900,
    to: 5200,
    tag: "query",
    target: [340, 166],
    note: [340, 108],
    lead: "340,133 340,156",
    lines: [
      "One query goes to one endpoint.",
      "The client doesn't see the services.",
    ],
  },
  {
    from: 5200,
    to: EXEC1[0],
    tag: "plan",
    target: [710, 148],
    note: [710, 108],
    lead: "710,133 710,140",
    lines: [
      "The gateway writes a query plan:",
      "every field belongs to one subgraph.",
    ],
  },
  {
    from: EXEC1[0],
    to: 13000,
    tag: "step 1",
    target: [548, 275],
    note: [470, 108],
    lead: "470,133 541,266",
    lines: [
      "Step 1: Billing and Catalog are",
      "queried at once. Shipping must wait.",
    ],
  },
  {
    from: 13000,
    to: 17200,
    tag: "billing",
    // The note sits in the empty pocket left of the Billing card,
    // level with the result row it points at.
    target: [228, 588],
    note: [118, 566],
    lead: "196,576 216,584",
    lines: ["Billing resolves price.", "It only needs the product id."],
  },
  {
    from: 17200,
    to: 21400,
    tag: "catalog",
    target: [568, 588],
    note: [700, 622],
    lead: "656,610 578,596",
    lines: ["Catalog returns the name, and the", "weight the plan asked for."],
  },
  {
    from: 21400,
    to: 24600,
    tag: "weight",
    target: [872, 304],
    note: [920, 108],
    lead: "920,133 920,290 882,302",
    lines: ["Shipping needs the product's weight", "to price the delivery."],
  },
  {
    from: 24600,
    to: EXEC2,
    tag: "handoff",
    target: [872, 304],
    note: [920, 108],
    lead: "920,133 920,290 882,302",
    lines: [
      "Services never talk to each other:",
      "the gateway carries the weight over.",
    ],
  },
  {
    from: EXEC2,
    to: DONE_AT,
    tag: "step 2",
    // The note sits in the empty pocket right of the Shipping card,
    // pointing at the docked query row (the one carrying the weight)
    // from the right, clear of the card's text.
    target: [1108, 524],
    note: [1292, 516],
    lead: "1188,525 1122,524",
    lines: [
      "Step 2: Shipping runs with the",
      "weight and returns the estimate.",
    ],
  },
  {
    from: DONE_AT,
    to: 36600,
    tag: "merge",
    // Right of the ONE RESPONSE label so the leader crosses no text.
    target: [1150, 157],
    note: [1150, 108],
    lead: "1150,133 1150,148",
    lines: ["Every field is in. The gateway", "merges one response: 200 OK."],
  },
  {
    from: 36600,
    to: T,
    tag: "respond",
    // The pointer sits under the exit lane so the departing envelope
    // never flies through it; the note answers the page's headline.
    target: [1300, 280],
    note: [1254, 395],
    lead: "1254,385 1294,290",
    lines: [
      "One response goes back.",
      "Many services, one graph: that's federation.",
    ],
  },
];

/** Length of a leader polyline, for the draw-in animation. */
function leadLength(points: string): number {
  const pts = points.split(" ").map((p) => p.split(",").map(Number));
  let len = 0;
  for (let i = 0; i < pts.length - 1; i++) {
    len += Math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1]);
  }
  return len;
}
const LEAD_LEN = BEATS.map((b) => leadLength(b.lead));

/* The timeline: a labeled dot per caption, evenly spaced so the labels
   read as a legend, plus a draggable playhead. The first and last dots
   are the line's start and end points. The playhead moves
   piecewise-linearly between dots, crossing each interior dot exactly
   when that beat begins; the final segment sweeps the merge and respond
   beats together so every moment of the tail stays scrubbable and the
   playhead reaches the endpoint exactly when the story completes. */
const TL_X0 = 360;
const TL_X1 = 1040;
const TL_W = TL_X1 - TL_X0;
const TL_Y = 664;
const TL_DOT_X = BEATS.map((_, i) => TL_X0 + (i * TL_W) / (BEATS.length - 1));
/* The first segment sweeps the pre-story arrival together with the
   query beat, and the last segment sweeps merge+respond, so both the
   entry and exit envelope transits stay scrubbable. A zero-width edge
   segment would park the playhead and make those moments unreachable. */
const TL_TIMES = [0, ...BEATS.slice(1, -1).map((c) => c.from), T];
const TL_XS = [...TL_DOT_X];

/** Loop time to playhead x. */
function tlX(t: number): number {
  for (let i = 0; i < TL_TIMES.length - 1; i++) {
    if (t < TL_TIMES[i + 1]) {
      const u = (t - TL_TIMES[i]) / (TL_TIMES[i + 1] - TL_TIMES[i]);
      return TL_XS[i] + u * (TL_XS[i + 1] - TL_XS[i]);
    }
  }
  return TL_X1;
}

/** Timeline x back to loop time, for scrubbing. */
function tlT(x: number): number {
  const v = Math.min(Math.max(x, TL_X0), TL_X1);
  for (let i = 0; i < TL_XS.length - 1; i++) {
    if (v < TL_XS[i + 1]) {
      const u = (v - TL_XS[i]) / (TL_XS[i + 1] - TL_XS[i]);
      return TL_TIMES[i] + u * (TL_TIMES[i + 1] - TL_TIMES[i]);
    }
  }
  return T - 1;
}

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

/* ── Scene ───────────────────────────────────────────────────────────── */

export function GatewayScene() {
  const [flank, setFlank] = useState<FlankVariant>("journey");
  const { rootRef, set, ctl } = useVisual(T, (t, h, scrubbing, wall) => {
    const intro = ramp(t, 300, 800);

    // ── The query arrives from the unseen client off-screen left. ────
    // The flank leg (see flanks.tsx) covers dist 0..1000 on the same
    // clock; this leg takes over once the envelope crosses the seam.
    const inDist = easeInOutCubic(ramp(t, 300, REQ_CARD)) * ENTRY_TOTAL;
    if (inDist > ENTRY_FLANK_LEN && t < REQ_CARD + 60) {
      const [ex, ey] = pointAt(
        ENTRY_VIS,
        (inDist - ENTRY_FLANK_LEN) / ENTRY_VIS.total,
      );
      h.setX("envIn", ex, ey);
      h.setO("envIn", 1 - ramp(t, REQ_CARD - 40, REQ_CARD + 40));
    } else {
      h.setO("envIn", 0);
    }
    h.setRing("ringReq", (t - REQ_CARD) / 500, 5, 14);

    // ── REQUEST: the document types in, then rides to the gateway. ───
    // The card frame is always present; the query types into it.
    const cardIn = easeInOutCubic(ramp(t, REQ_CARD, REQ_CARD + 350));
    h.setO("reqCard", Math.max(0.9 * intro, cardIn));
    REQ_LINES.forEach((_, i) => {
      const lineIn = ramp(
        t,
        REQ_CARD + 100 + i * 100,
        REQ_CARD + 400 + i * 100,
      );
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
    if (t >= ENV_SEND[0] && t < ENV_SEND[1]) {
      const u = easeInOutCubic(ramp(t, ENV_SEND[0], ENV_SEND[1]));
      const [x, y] = pointAt(LANE_SEND.poly, u);
      h.setX("envSend", x, y);
      h.setO(
        "envSend",
        Math.min(ramp(t, ENV_SEND[0], ENV_SEND[0] + 150), 1) *
          (1 - ramp(t, ENV_SEND[1] - 120, ENV_SEND[1])),
      );
    } else {
      h.setO("envSend", 0);
    }
    h.setRing("ringGw", (t - ENV_SEND[1]) / 500, 6, 18);

    // The gateway wakes when the request arrives.
    const lit = 0.18 * intro + 0.32 * ramp(t, ENV_SEND[1], ENV_SEND[1] + 300);
    const flash =
      0.5 *
      ramp(t, DONE_AT, DONE_AT + 250) *
      (1 - ramp(t, DONE_AT + 250, DONE_AT + 1100));
    h.setO("gwBorder", lit + flash);
    h.setO(
      "gwIdle",
      0.35 * (1 - ramp(t, ENV_SEND[1], ENV_SEND[1] + 300)) * intro,
    );

    // ── PLAN: left to right, one dependency. ─────────────────────────
    h.setO("planLbl", ramp(t, ENV_SEND[1] + 200, ENV_SEND[1] + 450));
    h.setO("planMeta", 0.6 * ramp(t, PLAN_CHIP[2], PLAN_CHIP[2] + 350));
    CHIP.forEach((_, i) => {
      const pop = easeInOutCubic(
        ramp(t, CHIP_STATE[i][0], CHIP_STATE[i][0] + 350),
      );
      h.setPop(`chip${i}`, pop, pop);
      const executing = t >= CHIP_STATE[i][1] && t < CHIP_STATE[i][2] ? 1 : 0;
      const done = ramp(t, CHIP_STATE[i][2], CHIP_STATE[i][2] + 250);
      // One breathing phase per service (chip border, dot, and busy
      // shimmer in step), offset between services, on the wall clock so
      // it never freezes while the story clock is held.
      const breath = Math.sin(wall / 150 + i);
      h.setO(`chipRun${i}`, executing * (0.5 + 0.3 * breath));
      h.setO(`chipDone${i}`, done * 0.5);
      h.setO(
        `dot${i}`,
        (executing ? 0.55 + 0.35 * breath : 0.25 * pop) * (1 - done),
      );
      h.setO(`chk${i}`, done);
    });
    const inTransit = t >= WSLIDE[0] && t < WSLIDE[1] + 200 ? 1 : 0;
    h.setO("depEdge", (0.5 + 0.4 * inTransit) * ramp(t, DEP_IN, DEP_IN + 400));
    h.setO("depLbl", (0.65 + 0.35 * inTransit) * ramp(t, DEP_IN, DEP_IN + 400));

    // The weight value crosses to Shipping.
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
    // Follow-through: the Shipping chip acknowledges the delivery.
    h.setO(
      "wGlint",
      ramp(t, WSLIDE[1], WSLIDE[1] + 150) *
        (1 - ramp(t, WSLIDE[1] + 250, WSLIDE[1] + 700)),
    );
    h.setRing("wRing", (t - WSLIDE[1]) / 450, 5, 10);

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
      h.setPop(`subQ${i}`, qIn, qIn);
      h.setO(
        `idle${i}`,
        0.3 * (1 - ramp(t, arrive - 500, arrive - 100)) * intro,
      );

      // Working shimmer until the result is ready.
      const busy = t >= arrive + 200 && t < RES_AT[i] ? 1 : 0;
      h.setO(`busy${i}`, busy * (0.4 + 0.3 * Math.sin(wall / 150 + i)));

      // The result appends below the query and stays.
      h.setO(`subR${i}`, ramp(t, RES_AT[i], RES_AT[i] + 300));

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
    // The card frame is always present; "building…" marks when the
    // gateway starts filling it.
    const bufIn = easeInOutCubic(ramp(t, BUF_IN, BUF_IN + 350));
    h.setO("resp", Math.max(0.9 * intro, bufIn));
    OUT_FIELD_LINES.forEach((li, i) => {
      const fieldIn = ramp(t, BACK[i][1], BACK[i][1] + 300);
      h.setO(`respL${li}`, fieldIn);
      h.setO(`respTick${i}`, fieldIn);
      // Pending fields hold a dim dashed slot until their value lands.
      h.setO(`respGh${i}`, 0.35 * (1 - fieldIn));
    });
    h.setO(
      "respBuilding",
      ramp(t, BUF_IN, BUF_IN + 300) *
        (1 - ramp(t, DONE_AT, DONE_AT + 300)) *
        0.75,
    );
    h.setO("respOk", ramp(t, DONE_AT, DONE_AT + 300));
    h.setO(
      "respFlash",
      0.7 *
        ramp(t, DONE_AT, DONE_AT + 250) *
        (1 - ramp(t, DONE_AT + 400, DONE_AT + 1100)),
    );

    // ── RESPOND: the sealed response leaves toward the caller. ───────
    if (t >= ENV_OUT[0] && t < ENV_OUT[1]) {
      const u = easeInOutCubic(ramp(t, ENV_OUT[0], ENV_OUT[1]));
      const [x, y] = pointAt(LANE_OUT.poly, u);
      h.setX("envOut", x, y);
      h.setO(
        "envOut",
        Math.min(ramp(t, ENV_OUT[0], ENV_OUT[0] + 150), 1) *
          (1 - ramp(t, ENV_OUT[1] - 120, ENV_OUT[1])),
      );
    } else {
      h.setO("envOut", 0);
    }

    // ── The guide and the timeline. ──────────────────────────────────
    // Notes swap per beat; the pointer glides between targets. While
    // scrubbing, the active note shows at full opacity so it is
    // readable wherever the playhead is dropped. The final note never
    // fades: the finished scene holds.
    let bi = 0;
    BEATS.forEach((b, i) => {
      if (t >= b.from) {
        bi = i;
      }
      const last = i === BEATS.length - 1;
      const active = t >= b.from && (last || t < b.to) ? 1 : 0;
      const noteIn = ramp(t, b.from + 350, b.from + 650);
      const o = scrubbing
        ? active
        : noteIn * (last ? 1 : 1 - ramp(t, b.to - 200, b.to));
      h.setPop(`note${i}`, o, scrubbing ? 1 : easeOutCubic(noteIn));
      h.setO(`mnote${i}`, o);
      // The leader draws itself from the note toward the target.
      h.setDash(
        `lead${i}`,
        LEAD_LEN[i] *
          (1 - (scrubbing ? 1 : ramp(t, b.from + 300, b.from + 600))),
      );
      h.setO(`tlDot${i}`, 0.3 + 0.7 * active);
      h.setO(`tlLbl${i}`, 0.35 + 0.6 * active);
    });
    // The glide scales with distance and flies a shallow arc so the
    // pointer lifts over card content instead of cutting through it.
    const cur = BEATS[bi];
    const prv = BEATS[Math.max(bi - 1, 0)];
    const dist = Math.hypot(
      cur.target[0] - prv.target[0],
      cur.target[1] - prv.target[1],
    );
    const gDur = Math.min(750, Math.max(400, dist * 1.3));
    const gu = easeInOutCubic(ramp(t, cur.from, cur.from + gDur));
    const lift = Math.sin(Math.PI * gu) * Math.min(40, dist * 0.12);
    const gx = prv.target[0] + (cur.target[0] - prv.target[0]) * gu;
    const gy =
      prv.target[1] +
      (cur.target[1] - prv.target[1]) * gu -
      lift +
      1.6 * Math.sin(wall / 430);
    h.setDot("guideGlow", gx, gy);
    h.setDot("guideRing", gx, gy, 6.5 + 0.8 * Math.sin(wall / 520));
    h.setDot("guideCore", gx, gy);
    h.setO("guide", ramp(t, 900, 1300));
    const headX = tlX(t);
    h.setDot("tlHead", headX, TL_Y);
    h.setDot("tlHeadGlow", headX, TL_Y);
    // Once the story has settled, invite a replay.
    h.setO(
      "tlHint",
      ramp(t, T - 900, T - 200) * (0.45 + 0.15 * Math.sin(wall / 400)),
    );
    driveFlanks(flank, t, wall, h);
  });

  const seekFromPointer = (e: React.PointerEvent<SVGRectElement>) => {
    const r = e.currentTarget.getBoundingClientRect();
    const svgX =
      TL_X0 - 16 + clamp01((e.clientX - r.left) / r.width) * (TL_W + 32);
    ctl.seek(Math.min(tlT(svgX), T - 1));
  };

  return (
    <>
      <div
        ref={rootRef}
        aria-hidden="true"
        className="relative w-full select-none"
      >
        <FlankLayer side="left" variant={flank} set={set} />
        <FlankLayer side="right" variant={flank} set={set} />
        <div className="relative mx-auto w-full max-w-[1480px]">
          <svg viewBox="0 76 1400 624" width="100%" className="block">
            <defs>
              <filter
                id="gw11-soft"
                x="-60%"
                y="-60%"
                width="220%"
                height="220%"
              >
                <feGaussianBlur stdDeviation="2.4" />
              </filter>
              <radialGradient id="gw11-stage" cx="50%" cy="50%" r="50%">
                <stop offset="0" stopColor="#f5f0ea" stopOpacity="0.07" />
                <stop offset="0.6" stopColor="#f5f0ea" stopOpacity="0.025" />
                <stop offset="1" stopColor="#f5f0ea" stopOpacity="0" />
              </radialGradient>
            </defs>

            {/* Lanes: the row, and the fan to the services. */}
            <path
              d={`M-40 ${MID_Y} C 20 ${MID_Y}, 60 ${MID_Y}, ${REQ.x - 2} ${MID_Y}`}
              fill="none"
              stroke="#f5f0ea"
              strokeOpacity={0.09}
              strokeWidth={1.5}
            />
            <path
              d={LANE_SEND.d}
              fill="none"
              stroke="rgba(245,241,234,0.18)"
              strokeWidth={1.5}
            />
            <path
              d={LANE_OUT.d}
              fill="none"
              stroke="rgba(245,241,234,0.13)"
              strokeWidth={1.5}
            />
            <line
              x1={GW.x + GW.w}
              x2={RESP.x - 2}
              y1={MID_Y}
              y2={MID_Y}
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

            {/* ── The request document. ── */}
            <text
              x={REQ.x}
              y={152}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.22em"
              fill={INK_DIM}
              opacity={0.7}
            >
              CLIENT REQUEST
            </text>
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

            {/* ── The gateway. ── */}
            <g>
              <circle
                cx={GW.x + GW.w / 2}
                cy={MID_Y}
                r={220}
                fill="url(#gw11-stage)"
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
                <path d="M556 168 h7 M556 174 h7 M556 180 h7" />
                <path d="M563 168 L570 174 L563 180 M563 174 h7" />
                <path d="M570 174 h8" />
              </g>
              <text
                x={586}
                y={179}
                fontFamily={MONO_FONT}
                fontSize={11}
                letterSpacing="0.22em"
                fill={INK}
              >
                GATEWAY
              </text>
              <line
                x1={GW.x}
                x2={GW.x + GW.w}
                y1={192}
                y2={192}
                stroke={HAIRLINE}
              />
              <text
                ref={set("gwIdle")}
                x={GW.x + GW.w / 2}
                y={262}
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
                x={556}
                y={212}
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
                y={212}
                textAnchor="end"
                fontFamily={MONO_FONT}
                fontSize={8}
                fill={INK_DIM}
                opacity={0.6}
              >
                2 steps · 1 dependency
              </text>

              {/* Dependency edge: Catalog's weight crosses to Shipping. */}
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
                x={710}
                y={290}
                textAnchor="middle"
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
              cx={GW.x - 2}
              cy={MID_Y}
              r={6}
              fill="none"
              stroke={TEAL}
              strokeWidth={1.5}
              opacity={0}
            />
            {/* The Shipping chip acknowledges the weight's delivery. */}
            <rect
              ref={set("wGlint")}
              x={CHIP[2].x}
              y={CHIP[2].y}
              width={CHIP[2].w}
              height={CHIP[2].h}
              rx={9}
              fill="none"
              stroke={CAT.color}
              strokeWidth={1.25}
              opacity={0}
            />
            <circle
              ref={set("wRing")}
              cx={748}
              cy={DEP_Y}
              r={5}
              fill="none"
              stroke={CAT.color}
              strokeWidth={1.25}
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

            {/* ── The one response, assembled on the right. ── */}
            <text
              x={RESP.x}
              y={142}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.22em"
              fill={INK_DIM}
              opacity={0.7}
            >
              ONE RESPONSE
            </text>
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
              {/* Pending fields hold dim dashed slots until their value lands. */}
              {OUT_FIELD_LINES.map((li, i) => (
                <line
                  key={i}
                  ref={set(`respGh${i}`)}
                  x1={RESP.x + 18 + OUT_LINES[li].ind * 13}
                  x2={RESP.x + 18 + OUT_LINES[li].ind * 13 + 96}
                  y1={RESP.y + 46 + li * 16 - 4}
                  y2={RESP.y + 46 + li * 16 - 4}
                  stroke={REQ_FIELD_COLOR[i]}
                  strokeOpacity={0.8}
                  strokeDasharray="3 5"
                  opacity={0}
                />
              ))}
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
                cy={344}
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
              y={434}
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
            <Envelope set={set} id="envIn" stroke={TEAL} />
            <circle
              ref={set("ringReq")}
              cx={REQ.x}
              cy={MID_Y}
              r={5}
              fill="none"
              stroke={TEAL}
              strokeWidth={1.5}
              opacity={0}
            />
            <Envelope set={set} id="envSend" stroke={TEAL} />
            <Envelope set={set} id="envOut" stroke={GREEN} />

            {/* Data pulses. */}
            {SUBS.map((sub, i) => (
              <g key={i}>
                <PulseGlyph
                  set={set}
                  id={`pOut${i}`}
                  main={sub.svc.color}
                  soft={sub.svc.soft}
                  filter="gw11-soft"
                />
                <PulseGlyph
                  set={set}
                  id={`pBack${i}`}
                  main={sub.svc.color}
                  soft={sub.svc.soft}
                  filter="gw11-soft"
                />
              </g>
            ))}

            {/* The guide: per-beat notes with leader lines, plus a hovering
            pointer that glides between points of interest. The static
            frame shows the completed story, so the RESPOND note and the
            pointer's final position start visible. */}
            {BEATS.map((b, i) => (
              <g
                key={i}
                ref={set(`note${i}`)}
                opacity={i === BEATS.length - 1 ? 1 : 0}
              >
                <polyline
                  ref={set(`lead${i}`)}
                  points={b.lead}
                  fill="none"
                  stroke="rgba(94,234,212,0.45)"
                  strokeWidth={1}
                  strokeDasharray={LEAD_LEN[i]}
                  strokeDashoffset={0}
                />
                {b.lines.map((ln, li) => (
                  <text
                    key={li}
                    x={b.note[0]}
                    y={b.note[1] + li * 18}
                    textAnchor="middle"
                    fontSize={13}
                    fill="rgba(222,230,244,0.92)"
                  >
                    {ln}
                  </text>
                ))}
              </g>
            ))}
            <g ref={set("guide")} opacity={1}>
              <circle
                ref={set("guideGlow")}
                cx={BEATS[BEATS.length - 1].target[0]}
                cy={BEATS[BEATS.length - 1].target[1]}
                r={11}
                fill={TEAL}
                opacity={0.18}
                filter="url(#gw11-soft)"
              />
              <circle
                ref={set("guideRing")}
                cx={BEATS[BEATS.length - 1].target[0]}
                cy={BEATS[BEATS.length - 1].target[1]}
                r={6.5}
                fill="none"
                stroke={TEAL}
                strokeWidth={1.25}
                opacity={0.9}
              />
              <circle
                ref={set("guideCore")}
                cx={BEATS[BEATS.length - 1].target[0]}
                cy={BEATS[BEATS.length - 1].target[1]}
                r={2.2}
                fill={TEAL}
              />
            </g>

            {/* The timeline: one dot per caption change; drag anywhere on it
            to scrub the loop. The static frame shows the completed story,
            so the playhead rests on the final beat's dot. */}
            <line
              x1={TL_X0}
              x2={TL_X1}
              y1={TL_Y}
              y2={TL_Y}
              stroke="rgba(245,241,234,0.18)"
              strokeWidth={1.5}
              strokeLinecap="round"
            />
            {BEATS.map((c, i) => (
              <g key={i}>
                <circle
                  ref={set(`tlDot${i}`)}
                  cx={TL_DOT_X[i]}
                  cy={TL_Y}
                  r={4}
                  fill="#f5f0ea"
                  opacity={i === BEATS.length - 1 ? 1 : 0.3}
                />
                <text
                  ref={set(`tlLbl${i}`)}
                  x={TL_DOT_X[i]}
                  y={TL_Y + 24}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={9.5}
                  letterSpacing="0.18em"
                  fill="#f5f0ea"
                  opacity={i === BEATS.length - 1 ? 0.95 : 0.35}
                >
                  {c.tag.toUpperCase()}
                </text>
              </g>
            ))}
            <circle
              ref={set("tlHeadGlow")}
              cx={TL_DOT_X[BEATS.length - 1]}
              cy={TL_Y}
              r={10}
              fill={TEAL}
              opacity={0.16}
              filter="url(#gw11-soft)"
            />
            <circle
              ref={set("tlHead")}
              cx={TL_DOT_X[BEATS.length - 1]}
              cy={TL_Y}
              r={5.5}
              fill={TEAL}
            />
            <rect
              x={TL_X0 - 16}
              y={TL_Y - 20}
              width={TL_W + 32}
              height={52}
              fill="transparent"
              style={{ cursor: "grab", touchAction: "none" }}
              onPointerDown={(e) => {
                e.currentTarget.setPointerCapture(e.pointerId);
                ctl.hold(true);
                seekFromPointer(e);
              }}
              onPointerMove={(e) => {
                if (e.currentTarget.hasPointerCapture(e.pointerId)) {
                  seekFromPointer(e);
                }
              }}
              onPointerUp={(e) => {
                e.currentTarget.releasePointerCapture(e.pointerId);
                ctl.hold(false);
              }}
              onPointerCancel={() => ctl.hold(false)}
            />

            {/* Once the story settles, invite a replay. */}
            <text
              ref={set("tlHint")}
              x={TL_X1 + 22}
              y={TL_Y + 4}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.12em"
              fill={INK_DIM}
              opacity={0.55}
            >
              DRAG TO REPLAY
            </text>
          </svg>
        </div>

        {/* On small screens the SVG notes render too small to read, so
            the active note is mirrored as body-size text under the
            graphic. */}
        <div className="relative mx-auto h-16 w-full max-w-sm px-4 text-center sm:hidden">
          {BEATS.map((b, i) => (
            <p
              key={i}
              ref={set(`mnote${i}`)}
              className="absolute inset-x-0 top-0 text-sm text-[#dee6f4]/90"
              style={{ opacity: i === BEATS.length - 1 ? 1 : 0 }}
            >
              {b.lines[0]} {b.lines[1]}
            </p>
          ))}
        </div>
      </div>
      <FlankSwitcher value={flank} onChange={setFlank} />
    </>
  );
}

/* ── Sub-query documents (with the dependency made visible) ──────────── */

interface SubQueryProps {
  readonly i: number;
}

/**
 * Billing's query needs nothing extra. Catalog's query fetches name plus
 * weight (underlined: fetched for the plan, not the client). Shipping's
 * query carries the weight value from Catalog, rendered in Catalog's
 * color to show its provenance.
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
        <text x={x + 26} y={y0 + 2 * lh} {...common} fill={sub.svc.color}>
          price
        </text>
      )}
      {i === 1 && (
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
