"use client";

/**
 * Mocha messaging page, v13. One circuit board spans the whole page, revealed
 * only where light falls. Service nodes light as you scroll to them; the lanes
 * between them stay faintly visible and carry coral messages. Each concept
 * section's diagram (mediator, pub/sub, outbox, saga, trace, transports) is
 * printed onto that same board and stays dark until the section scrolls in,
 * when its light pool illuminates the structure and a coral message runs its
 * real lanes. Hovering a service node emits a burst. Muted premium palette.
 */

import Link from "next/link";
import { useEffect, useRef, useState } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const GRID = 28;
const MONO_FONT = "ui-monospace, SFMono-Regular, Menlo, monospace";

const SUBSTRATE = "#0a0e18";
const PAD_COLOR = "rgba(150, 166, 194, 0.14)";
// Muted-slate ground-plane hatch (v10 uses cyan; kept on-palette here).
const HATCH_COLOR = "rgba(150, 166, 194, 0.08)";
const HATCH_EDGE = "rgba(150, 166, 194, 0.13)";
// Scattered background IC footprints: dark packages that populate the board.
const FOOT_BODY = "#0b1220";
const FOOT_EDGE = "rgba(158, 176, 204, 0.44)";
const FOOT_PIN = "rgba(158, 176, 204, 0.34)";
const PASSIVE_PAD = "rgba(168, 184, 210, 0.5)";
const PASSIVE_BODY = "rgba(120, 136, 164, 0.32)";
const SILK_SCATTER = "rgba(150, 168, 198, 0.42)";
const TRACE_COLOR = "rgba(139, 160, 188, 0.42)";
const TRACE_ALT_COLOR = "rgba(139, 160, 188, 0.26)";
const LANE_COLOR = "rgba(174, 190, 216, 0.5)";
const LANE_ALWAYS = "rgba(162, 182, 210, 0.19)";
const VIA_COLOR = "rgba(164, 180, 208, 0.55)";
const LABEL_COLOR = "rgba(176, 192, 216, 0.85)";
const LABEL_DIM = "rgba(150, 166, 194, 0.55)";
// IC-chip footprint tones: a package body that sits above the substrate, a
// crisp silkscreen edge, metallic lead pads, a pin-1 mark and silkscreen text.
const CHIP_BODY = "#1c2740";
const CHIP_EDGE = "rgba(206, 220, 244, 0.9)";
const CHIP_PIN = "rgba(190, 206, 232, 0.66)";
const CHIP_PIN1 = "rgba(228, 238, 252, 0.92)";
const CHIP_LABEL = "rgba(220, 231, 248, 0.95)";
const SILK_DIM = "rgba(154, 172, 200, 0.7)";
const NODE_HALO = "150, 176, 208";
const MSG = "224, 140, 122";
const MSG_SOFT = "246, 202, 190";

const NODE_LIGHT_RADIUS = 150;
const MOTIF_LIGHT_RADIUS = 190;
const PULSE_LIGHT_RADIUS = 98;
const PULSE_TRAIL = 128;
const BIN_H = 320;

interface Point {
  readonly x: number;
  readonly y: number;
}

interface Trace {
  readonly pts: Point[];
  readonly cum: number[];
  readonly len: number;
  readonly to: number;
  readonly connector: boolean;
  // Copper layer: 0 = top (bright), 1 = inner (dim, via-terminated). Random
  // filler copper is demoted to layer 1 where it would cross top-layer copper,
  // so every visible crossing reads as a deliberate two-layer board.
  layer: number;
}

interface MotifEndpoint {
  x: number;
  y: number;
  flash: number;
}

/** A lead pad on a package edge, with its outward normal and whether copper
 * actually lands on it (a lane endpoint) or it escapes/stays NC. */
interface Pad {
  readonly x: number;
  readonly y: number;
  readonly nx: number;
  readonly ny: number;
  readonly connected: boolean;
}

interface MotifBox {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly label: string;
  pads?: Pad[];
  escapes?: Point[][];
}

interface MotifLabel {
  readonly x: number;
  readonly y: number;
  readonly text: string;
  readonly anchor: CanvasTextAlign;
  readonly dim?: boolean;
}

/** A point where a pulse spawns a child pulse; childTaps cascade a grandchild. */
interface MotifTap {
  at: number;
  trace: Trace;
  ep: MotifEndpoint;
  childTaps?: MotifTap[];
  fired?: boolean;
}

type MotifKind =
  | "mediator"
  | "pubsub"
  | "outbox"
  | "saga"
  | "trace"
  | "transports";

interface Motif {
  readonly kind: MotifKind;
  readonly x: number;
  readonly y: number;
  readonly boxes: MotifBox[];
  readonly labels: MotifLabel[];
  readonly endpoints: MotifEndpoint[];
  readonly rings: Point[];
  readonly inLanes: Trace[];
  readonly outLanes: Trace[];
  readonly segs: Trace[];
  readonly branches: Trace[];
  mainLane?: Trace;
  readonly taps: MotifTap[];
  readonly lamp: { x: number; y: number; r: number };
  readonly top: number;
  readonly bottom: number;
  level: number;
  clock: number;
  beat: number;
  // Set in generateBoard: the block's scroll-order number, the pad where the
  // spine docks into this circuit, and the copper from the spine to that pad.
  block?: number;
  inPad?: MotifEndpoint;
  inStub?: Trace;
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
  to: number;
  dim?: boolean;
  ep?: MotifEndpoint;
  replyLane?: Trace;
  replyEp?: MotifEndpoint;
  fanout?: { trace: Trace; ep: MotifEndpoint }[];
  remaining?: { trace: Trace; ep: MotifEndpoint }[];
  taps?: MotifTap[];
  retry?: boolean;
  retried?: boolean;
  originEp?: MotifEndpoint;
  // The message arriving from the spine: on completion, fire the block it docks.
  arriveMotif?: Motif;
}

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

interface Footprint {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  pads?: Pad[];
  escapes?: Point[][];
}

/** A via/pad plus the unit direction of the trace arriving at it (for teardrops). */
interface Via {
  readonly x: number;
  readonly y: number;
  readonly dx: number;
  readonly dy: number;
}

/** A small two-pad passive (resistor/cap) footprint. */
interface Passive {
  readonly x: number;
  readonly y: number;
  readonly horiz: boolean;
}

interface Silk {
  readonly x: number;
  readonly y: number;
  readonly text: string;
}

interface Board {
  readonly traces: Trace[];
  readonly connectors: Trace[];
  readonly outgoing: Trace[][];
  readonly nodes: Point[];
  readonly vias: Via[];
  readonly motifs: Motif[];
  readonly hatches: Rect[];
  readonly footprints: Footprint[];
  readonly passives: Passive[];
  readonly silks: Silk[];
  readonly bins: number[][];
}

interface MotifAnchorPoint {
  readonly kind: MotifKind;
  readonly x: number;
  readonly y: number;
}

const DIR_VECS: readonly Point[] = [
  { x: 1, y: 0 },
  { x: 1, y: 1 },
  { x: 0, y: 1 },
  { x: -1, y: 1 },
  { x: -1, y: 0 },
  { x: -1, y: -1 },
  { x: 0, y: -1 },
  { x: 1, y: -1 },
];

function mulberry32(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a += 0x6d2b79f5;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * A private PRNG seeded by a part's position, so per-pin decisions are
 * deterministic and stable across sub-pixel re-measures without touching the
 * board's main PRNG stream.
 */
function posRand(x: number, y: number): () => number {
  return mulberry32(
    (Math.imul(Math.round(x), 73856093) ^
      Math.imul(Math.round(y), 19349663)) >>>
      0,
  );
}

/**
 * Lead pads for an IC package on its two long sides. Rows are uniform, except
 * where a lane endpoint lands: that pad snaps to the lane's y (moving the pad
 * to the copper, never the copper to the pad) and is marked connected.
 */
function chipPads(box: MotifBox, leftYs: number[], rightYs: number[]): Pad[] {
  const pads: Pad[] = [];
  const pins = Math.max(3, Math.round(box.h / 11));
  const pitch = box.h / (pins + 1);
  const sides: [number, number, number[]][] = [
    [box.x, -1, leftYs],
    [box.x + box.w, 1, rightYs],
  ];
  for (const [sx, nx, ys] of sides) {
    const rows: { y: number; connected: boolean }[] = [];
    for (let p = 1; p <= pins; p++) {
      rows.push({ y: box.y + p * pitch, connected: false });
    }
    for (const cy of ys) {
      let best = -1;
      let bd = Infinity;
      for (let r = 0; r < rows.length; r++) {
        const d = Math.abs(rows[r].y - cy);
        if (d < bd) {
          bd = d;
          best = r;
        }
      }
      if (best >= 0 && bd < pitch * 0.6) {
        rows[best] = { y: cy, connected: true };
      } else {
        rows.push({ y: cy, connected: true });
      }
    }
    for (const r of rows) {
      pads.push({ x: sx, y: r.y, nx, ny: 0, connected: r.connected });
    }
  }
  return pads;
}

/** Lead pads on the top and bottom edges of a scattered footprint. */
function footPads(foot: Footprint): Pad[] {
  const pads: Pad[] = [];
  for (let px = foot.x + 7; px <= foot.x + foot.w - 9; px += 10) {
    pads.push({ x: px + 1.5, y: foot.y, nx: 0, ny: -1, connected: false });
    pads.push({
      x: px + 1.5,
      y: foot.y + foot.h,
      nx: 0,
      ny: 1,
      connected: false,
    });
  }
  return pads;
}

/**
 * Short escape traces off the unconnected pads: most pins escape perpendicular
 * to a via, some stay NC, so the package reads as soldered into the board.
 */
function padEscapes(
  pads: Pad[],
  rand: () => number,
  keep?: (x: number, y: number) => boolean,
): Point[][] {
  const escapes: Point[][] = [];
  for (const pad of pads) {
    if (pad.connected || rand() > 0.72) {
      continue;
    }
    const tipX = pad.x + pad.nx * 5;
    const tipY = pad.y + pad.ny * 5;
    const len = 9 + rand() * 9;
    const ex = tipX + pad.nx * len;
    const ey = tipY + pad.ny * len;
    if (keep && !keep(ex, ey)) {
      continue;
    }
    escapes.push([
      { x: tipX, y: tipY },
      { x: ex, y: ey },
    ]);
  }
  return escapes;
}

function traceFrom(pts: Point[], to: number, connector: boolean): Trace {
  const cum = [0];
  let len = 0;
  for (let i = 1; i < pts.length; i++) {
    len += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    cum.push(len);
  }
  return { pts, cum, len, to, connector, layer: 0 };
}

function walk(
  rand: () => number,
  start: Point,
  dir: number,
  w: number,
  h: number,
): Trace | null {
  const pts: Point[] = [start];
  let { x, y } = start;
  let d = dir;
  const segs = 2 + Math.floor(rand() * 3);
  for (let s = 0; s < segs; s++) {
    const diag = d % 2 === 1;
    const cells = diag
      ? 1 + Math.floor(rand() * 2)
      : 2 + Math.floor(rand() * 4);
    const nx = x + DIR_VECS[d].x * cells * GRID;
    const ny = y + DIR_VECS[d].y * cells * GRID;
    if (nx < GRID || nx > w - GRID || ny < GRID || ny > h - GRID) {
      break;
    }
    x = nx;
    y = ny;
    pts.push({ x, y });
    if (diag) {
      d = (d + (rand() < 0.5 ? 1 : 7)) % 8;
    } else if (rand() > 0.5) {
      d = (d + (rand() < 0.5 ? 1 : 7)) % 8;
    }
  }
  return pts.length >= 2 ? traceFrom(pts, -1, false) : null;
}

function connector(a: Point, b: Point, to: number): Trace {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const mid =
    Math.abs(dx) >= Math.abs(dy)
      ? { x: b.x - Math.sign(dx) * Math.abs(dy), y: a.y }
      : { x: a.x, y: b.y - Math.sign(dy) * Math.abs(dx) };
  return traceFrom([a, mid, b], to, true);
}

function pointAt(trace: Trace, dist: number): Point {
  const d = Math.max(0, Math.min(dist, trace.len));
  let i = 1;
  while (i < trace.cum.length - 1 && trace.cum[i] < d) {
    i++;
  }
  const segLen = trace.cum[i] - trace.cum[i - 1];
  const t = segLen > 0 ? (d - trace.cum[i - 1]) / segLen : 0;
  const a = trace.pts[i - 1];
  const b = trace.pts[i];
  return { x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t };
}

function envelope(p: Pulse): number {
  return (
    Math.min(1, p.dist / 70) *
    Math.min(1, Math.max(0, (p.trace.len - p.dist) / 120))
  );
}

/**
 * Build one concept motif around (cx, cy). Coordinates follow a 400x240 local
 * frame centered on the anchor. Structural lanes are pushed to `sink` so they
 * are drawn and revealed like the rest of the board; the returned Motif keeps
 * typed refs for the message drive.
 */
function buildMotif(
  kind: MotifKind,
  cx: number,
  cy: number,
  sink: Trace[],
): Motif {
  const qx = (x: number) => cx - 200 + x;
  const qy = (y: number) => cy - 120 + y;
  const lane = (pts: Point[]) => {
    const t = traceFrom(pts, -1, true);
    sink.push(t);
    return t;
  };
  const ep = (x: number, y: number): MotifEndpoint => ({ x, y, flash: 0 });

  const m: Motif = {
    kind,
    x: cx,
    y: cy,
    boxes: [],
    labels: [],
    endpoints: [],
    rings: [],
    inLanes: [],
    outLanes: [],
    segs: [],
    branches: [],
    taps: [],
    lamp: { x: cx, y: cy, r: MOTIF_LIGHT_RADIUS },
    top: cy - 145,
    bottom: cy + 145,
    level: 0,
    clock: 0,
    beat: 0,
  };

  if (kind === "mediator") {
    // Matched REQ/REP pair between two ICs, with a JP1 0-ohm strap in the
    // request net (the build-time, zero-reflection direct dispatch).
    m.boxes.push({ x: qx(26), y: qy(88), w: 58, h: 52, label: "SEND" });
    m.boxes.push({ x: qx(316), y: qy(88), w: 58, h: 52, label: "HANDLER" });
    // REQ net: SEND TX -> (through JP1) -> HANDLER RX.
    m.outLanes.push(
      lane([
        { x: qx(84), y: qy(108) },
        { x: qx(316), y: qy(108) },
      ]),
    );
    // REP net: HANDLER TX-reply -> SEND RX, parallel below.
    m.inLanes.push(
      lane([
        { x: qx(316), y: qy(124) },
        { x: qx(84), y: qy(124) },
      ]),
    );
    // JP1 strap: two lands bridged, sitting on the request net.
    const strap = lane([
      { x: qx(184), y: qy(108) },
      { x: qx(212), y: qy(108) },
    ]);
    m.rings.push({ x: qx(184), y: qy(108) }, { x: qx(212), y: qy(108) });
    const strapEp = ep(qx(212), qy(108));
    m.endpoints.push(ep(qx(84), qy(114)), ep(qx(316), qy(114)), strapEp);
    m.taps.push({ at: 100, trace: strap, ep: strapEp });
    m.labels.push(
      {
        x: qx(96),
        y: qy(78),
        text: "ISender.Send(CreateReview)",
        anchor: "start",
      },
      { x: qx(96), y: qy(140), text: "reply", anchor: "start", dim: true },
      {
        x: qx(198),
        y: qy(96),
        text: "resolved at build",
        anchor: "center",
        dim: true,
      },
    );
  } else if (kind === "pubsub") {
    // 1:3 fan-out buffer: one publish cloned into three equal-length copies
    // so every subscriber receives on the same tick (broadcast, not serial).
    m.boxes.push({ x: qx(58), y: qy(92), w: 86, h: 52, label: "1:3 BUF" });
    m.boxes.push({ x: qx(300), y: qy(42), w: 62, h: 36, label: "SUB A" });
    m.boxes.push({ x: qx(300), y: qy(100), w: 62, h: 36, label: "SUB B" });
    m.boxes.push({ x: qx(300), y: qy(160), w: 62, h: 36, label: "SUB C" });
    // J1 publish header into the buffer.
    m.rings.push({ x: qx(14), y: qy(110) }, { x: qx(14), y: qy(126) });
    m.inLanes.push(
      lane([
        { x: qx(22), y: qy(118) },
        { x: qx(58), y: qy(118) },
      ]),
    );
    // Three equal-length output nets; the middle carries a trombone to match.
    m.outLanes.push(
      lane([
        { x: qx(144), y: qy(104) },
        { x: qx(196), y: qy(104) },
        { x: qx(240), y: qy(60) },
        { x: qx(300), y: qy(60) },
      ]),
      lane([
        { x: qx(144), y: qy(118) },
        { x: qx(200), y: qy(118) },
        { x: qx(200), y: qy(140) },
        { x: qx(244), y: qy(140) },
        { x: qx(244), y: qy(118) },
        { x: qx(300), y: qy(118) },
      ]),
      lane([
        { x: qx(144), y: qy(132) },
        { x: qx(196), y: qy(132) },
        { x: qx(240), y: qy(178) },
        { x: qx(300), y: qy(178) },
      ]),
    );
    m.endpoints.push(
      ep(qx(101), qy(118)),
      ep(qx(300), qy(60)),
      ep(qx(300), qy(118)),
      ep(qx(300), qy(178)),
    );
    m.rings.push(
      { x: qx(300), y: qy(60) },
      { x: qx(300), y: qy(118) },
      { x: qx(300), y: qy(178) },
    );
    m.labels.push(
      { x: qx(20), y: qy(102), text: "PUBLISH", anchor: "start", dim: true },
      {
        x: qx(101),
        y: qy(84),
        text: "TOPIC ReviewCreated",
        anchor: "center",
        dim: true,
      },
    );
  } else if (kind === "outbox") {
    m.boxes.push({ x: qx(30), y: qy(88), w: 70, h: 56, label: "OUTBOX" });
    m.boxes.push({ x: qx(300), y: qy(88), w: 70, h: 56, label: "INBOX" });
    m.segs.push(
      lane([
        { x: qx(100), y: qy(116) },
        { x: qx(300), y: qy(116) },
      ]),
    );
    m.endpoints.push(ep(qx(100), qy(116)), ep(qx(300), qy(116)));
    m.labels.push({
      x: qx(200),
      y: qy(80),
      text: "COMMIT TOGETHER",
      anchor: "center",
    });
    m.labels.push({
      x: qx(200),
      y: qy(162),
      text: "DEDUPED BY ID",
      anchor: "center",
      dim: true,
    });
  } else if (kind === "saga") {
    m.boxes.push({ x: qx(22), y: qy(86), w: 74, h: 40, label: "DRAFT" });
    m.boxes.push({ x: qx(163), y: qy(86), w: 74, h: 40, label: "CHECKED" });
    m.boxes.push({ x: qx(304), y: qy(86), w: 74, h: 40, label: "PUBLISHED" });
    m.boxes.push({ x: qx(165), y: qy(168), w: 70, h: 34, label: "COMP." });
    m.segs.push(
      lane([
        { x: qx(96), y: qy(106) },
        { x: qx(163), y: qy(106) },
      ]),
      lane([
        { x: qx(237), y: qy(106) },
        { x: qx(304), y: qy(106) },
      ]),
    );
    m.branches.push(
      lane([
        { x: qx(200), y: qy(126) },
        { x: qx(200), y: qy(168) },
      ]),
      lane([
        { x: qx(165), y: qy(185) },
        { x: qx(120), y: qy(185) },
        { x: qx(120), y: qy(126) },
      ]),
    );
    m.endpoints.push(
      ep(qx(59), qy(106)),
      ep(qx(200), qy(106)),
      ep(qx(341), qy(106)),
      ep(qx(200), qy(185)),
    );
  } else if (kind === "trace") {
    // Delay-line span waterfall: each span is a copper row whose LENGTH is its
    // duration; the request forks top-to-bottom into nested spans exported at
    // an OTLP header. Longer copper visibly takes the pulse longer to fill.
    m.boxes.push({ x: qx(14), y: qy(46), w: 56, h: 44, label: "REQ" });
    const root = lane([
      { x: qx(72), y: qy(68) },
      { x: qx(374), y: qy(68) },
    ]);
    m.mainLane = root;
    const auth = lane([
      { x: qx(110), y: qy(100) },
      { x: qx(206), y: qy(100) },
    ]);
    const db = lane([
      { x: qx(134), y: qy(130) },
      { x: qx(356), y: qy(130) },
    ]);
    const cache = lane([
      { x: qx(166), y: qy(160) },
      { x: qx(250), y: qy(160) },
    ]);
    const publish = lane([
      { x: qx(190), y: qy(190) },
      { x: qx(330), y: qy(190) },
    ]);
    m.segs.push(auth, db, cache, publish);
    // Nesting stitches from each parent row down to its child's start pad.
    m.branches.push(
      lane([
        { x: qx(110), y: qy(68) },
        { x: qx(110), y: qy(100) },
      ]),
      lane([
        { x: qx(134), y: qy(68) },
        { x: qx(134), y: qy(130) },
      ]),
      lane([
        { x: qx(166), y: qy(130) },
        { x: qx(166), y: qy(160) },
      ]),
      lane([
        { x: qx(190), y: qy(68) },
        { x: qx(190), y: qy(190) },
      ]),
    );
    const j1 = lane([
      { x: qx(374), y: qy(68) },
      { x: qx(388), y: qy(56) },
    ]);
    m.rings.push(
      { x: qx(110), y: qy(100) },
      { x: qx(134), y: qy(130) },
      { x: qx(166), y: qy(160) },
      { x: qx(190), y: qy(190) },
      { x: qx(374), y: qy(56) },
      { x: qx(374), y: qy(68) },
      { x: qx(374), y: qy(80) },
    );
    const inEp = ep(qx(16), qy(68));
    const authEp = ep(qx(206), qy(100));
    const dbEp = ep(qx(356), qy(130));
    const cacheEp = ep(qx(250), qy(160));
    const pubEp = ep(qx(330), qy(190));
    const rootEp = ep(qx(374), qy(68));
    const otlpEp = ep(qx(388), qy(56));
    m.endpoints.push(inEp, authEp, dbEp, cacheEp, pubEp, rootEp, otlpEp);
    // Taps fire down the staircase in fork-x order; DB carries a grandchild.
    m.taps.push(
      { at: 38, trace: auth, ep: authEp },
      {
        at: 62,
        trace: db,
        ep: dbEp,
        childTaps: [{ at: 32, trace: cache, ep: cacheEp }],
      },
      { at: 118, trace: publish, ep: pubEp },
      { at: 300, trace: j1, ep: otlpEp },
    );
    const spanLabels: [number, number, string][] = [
      [80, 64, "HANDLER"],
      [110, 96, "AUTH"],
      [134, 126, "DB.QUERY"],
      [166, 156, "CACHE.GET"],
      [190, 186, "PUBLISH"],
    ];
    for (const [lx, ly, text] of spanLabels) {
      m.labels.push({ x: qx(lx), y: qy(ly), text, anchor: "start", dim: true });
    }
    m.labels.push(
      { x: qx(380), y: qy(48), text: "OTLP", anchor: "start", dim: true },
      {
        x: qx(16),
        y: qy(212),
        text: "TRACE · SPAN WATERFALL",
        anchor: "start",
        dim: true,
      },
    );
  } else {
    // Shunt-strap selector JP1: three interchangeable transport modules on
    // identical header rows; whichever is strapped bridges onto the SAME
    // common rail and the SAME handler contract. Swap the strap, not the code.
    const rows = [
      { y: 86, label: "RMQ" },
      { y: 120, label: "PG" },
      { y: 154, label: "INP" },
    ];
    for (const r of rows) {
      m.boxes.push({
        x: qx(22),
        y: qy(r.y - 11),
        w: 46,
        h: 22,
        label: r.label,
      });
      // Path: transport -> source pin -> (shunt bridge) -> common rail ->
      // down/up to the contract row -> handler. One lane per row.
      m.segs.push(
        lane([
          { x: qx(68), y: qy(r.y) },
          { x: qx(186), y: qy(r.y) },
          { x: qx(214), y: qy(r.y) },
          { x: qx(214), y: qy(120) },
          { x: qx(318), y: qy(120) },
        ]),
      );
      m.rings.push({ x: qx(186), y: qy(r.y) }, { x: qx(214), y: qy(r.y) });
      m.labels.push({
        x: qx(20),
        y: qy(r.y + 3),
        text: r.label,
        anchor: "end",
        dim: true,
      });
    }
    // Common rail (tied bus) and the handler it always drives.
    m.branches.push(
      lane([
        { x: qx(214), y: qy(86) },
        { x: qx(214), y: qy(154) },
      ]),
    );
    m.boxes.push({ x: qx(318), y: qy(109), w: 30, h: 22, label: "U9" });
    m.endpoints.push(ep(qx(318), qy(120)));
    m.labels.push(
      {
        x: qx(200),
        y: qy(70),
        text: "JP1 SELECT TRANSPORT",
        anchor: "center",
        dim: true,
      },
      { x: qx(270), y: qy(112), text: "CONTRACT", anchor: "center", dim: true },
      {
        x: qx(22),
        y: qy(182),
        text: "SWAP THE STRAP, NOT THE HANDLER",
        anchor: "start",
        dim: true,
      },
    );
  }

  // Scale the whole footprint up around its centre so the components read at
  // a glance instead of as a tiny cluster. Trace lengths, tap offsets and box
  // sizes all scale together so the animation timing stays consistent.
  const S = 1.15;
  const scPt = (p: Point): Point => ({
    x: cx + (p.x - cx) * S,
    y: cy + (p.y - cy) * S,
  });
  const scaled = new Set<Trace>();
  const scaleTrace = (t: Trace) => {
    if (scaled.has(t)) {
      return;
    }
    scaled.add(t);
    for (let i = 0; i < t.pts.length; i++) {
      t.pts[i] = scPt(t.pts[i]);
    }
    for (let i = 0; i < t.cum.length; i++) {
      t.cum[i] *= S;
    }
    (t as { len: number }).len *= S;
  };
  for (const t of m.inLanes) {
    scaleTrace(t);
  }
  for (const t of m.outLanes) {
    scaleTrace(t);
  }
  for (const t of m.segs) {
    scaleTrace(t);
  }
  for (const t of m.branches) {
    scaleTrace(t);
  }
  if (m.mainLane) {
    scaleTrace(m.mainLane);
  }
  for (const tp of m.taps) {
    scaleTrace(tp.trace);
    tp.at *= S;
  }
  for (let i = 0; i < m.boxes.length; i++) {
    const b = m.boxes[i];
    const p = scPt({ x: b.x, y: b.y });
    m.boxes[i] = { x: p.x, y: p.y, w: b.w * S, h: b.h * S, label: b.label };
  }
  for (let i = 0; i < m.rings.length; i++) {
    m.rings[i] = scPt(m.rings[i]);
  }
  for (const e of m.endpoints) {
    const p = scPt({ x: e.x, y: e.y });
    e.x = p.x;
    e.y = p.y;
  }
  for (let i = 0; i < m.labels.length; i++) {
    const lb = m.labels[i];
    const p = scPt({ x: lb.x, y: lb.y });
    m.labels[i] = {
      x: p.x,
      y: p.y,
      text: lb.text,
      anchor: lb.anchor,
      dim: lb.dim,
    };
  }

  // Solder the chips into their nets: give each package pads snapped to the y
  // where a lane actually lands, and short escapes off the pins that do not,
  // so the ICs read as routed rather than pasted on. Uses a position-seeded
  // PRNG so it never disturbs the board's main stream.
  const laneEnds: Point[] = [];
  const collectEnds = (t?: Trace) => {
    if (t) {
      laneEnds.push(t.pts[0], t.pts[t.pts.length - 1]);
    }
  };
  m.inLanes.forEach(collectEnds);
  m.outLanes.forEach(collectEnds);
  m.segs.forEach(collectEnds);
  m.branches.forEach(collectEnds);
  collectEnds(m.mainLane);
  for (const box of m.boxes) {
    const inSpan = (p: Point) => p.y >= box.y - 6 && p.y <= box.y + box.h + 6;
    const leftYs = laneEnds
      .filter((p) => Math.abs(p.x - box.x) < 8 && inSpan(p))
      .map((p) => p.y);
    const rightYs = laneEnds
      .filter((p) => Math.abs(p.x - (box.x + box.w)) < 8 && inSpan(p))
      .map((p) => p.y);
    box.pads = chipPads(box, leftYs, rightYs);
    box.escapes = padEscapes(box.pads, posRand(box.x, box.y));
  }

  return m;
}

function generateBoard(
  nodes: Point[],
  motifAnchors: MotifAnchorPoint[],
  w: number,
  h: number,
): Board {
  const rand = mulberry32(29 + Math.floor(w) * 7 + Math.floor(h));
  const traces: Trace[] = [];
  const connectors: Trace[] = [];
  const vias: Via[] = [];
  const outgoing: Trace[][] = nodes.map(() => []);
  const pushVia = (t: Trace) => {
    const pts = t.pts;
    const end = pts[pts.length - 1];
    const prev = pts[pts.length - 2] ?? end;
    const len = Math.hypot(end.x - prev.x, end.y - prev.y) || 1;
    vias.push({
      x: end.x,
      y: end.y,
      dx: (end.x - prev.x) / len,
      dy: (end.y - prev.y) / len,
    });
  };

  const motifs = motifAnchors.map((a) => buildMotif(a.kind, a.x, a.y, traces));

  // The board is modelled around the motifs: no foreign copper crosses a motif
  // block, and a deliberate spine down the centre gutter feeds each one.
  const gutterX = w / 2;
  const ordered = [...motifs].sort((a, b) => a.y - b.y);
  const motifRects: Rect[] = motifAnchors.map((a) => ({
    x: a.x - 236,
    y: a.y - 150,
    w: 472,
    h: 300,
  }));
  const inMotif = (p: Point) =>
    motifRects.some(
      (r) => p.x >= r.x && p.x <= r.x + r.w && p.y >= r.y && p.y <= r.y + r.h,
    );
  const crossesMotif = (pts: Point[]) => {
    for (let i = 1; i < pts.length; i++) {
      const a = pts[i - 1];
      const b = pts[i];
      const steps = Math.max(
        1,
        Math.ceil(Math.hypot(b.x - a.x, b.y - a.y) / 10),
      );
      for (let s = 0; s <= steps; s++) {
        const t = s / steps;
        if (inMotif({ x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t })) {
          return true;
        }
      }
    }
    return false;
  };

  // Single-layer routing discipline: a build-time occupancy grid so no two
  // top-layer traces cross. Deliberate copper (motif lanes, spine, stubs,
  // feeder, backbone) is stamped first and never rejected; random filler that
  // would cross it is demoted to a real inner layer (via-terminated, drawn
  // dim) or dropped. Discs around every node and the spine top are exempt so
  // the radial fans and the node feeds do not reject one another.
  const CELL = GRID / 2;
  const gcols = Math.max(1, Math.ceil(w / CELL));
  const grows = Math.max(1, Math.ceil(h / CELL));
  const occTop = new Uint8Array(gcols * grows);
  const occInner = new Uint8Array(gcols * grows);
  const exemptPts: Point[] = [...nodes];
  const EXEMPT_R2 = (GRID * 1.5) ** 2;
  const exemptAt = (x: number, y: number) => {
    for (const e of exemptPts) {
      if ((x - e.x) ** 2 + (y - e.y) ** 2 < EXEMPT_R2) {
        return true;
      }
    }
    return false;
  };
  const STEP = CELL / 3;
  const eachSample = (pts: Point[], fn: (x: number, y: number) => void) => {
    for (let i = 1; i < pts.length; i++) {
      const a = pts[i - 1];
      const b = pts[i];
      const steps = Math.max(
        1,
        Math.ceil(Math.hypot(b.x - a.x, b.y - a.y) / STEP),
      );
      for (let s = 0; s <= steps; s++) {
        const t = s / steps;
        fn(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
      }
    }
  };
  const stamp = (pts: Point[], grid: Uint8Array) => {
    eachSample(pts, (x, y) => {
      if (exemptAt(x, y)) {
        return;
      }
      const cx = Math.floor(x / CELL);
      const cy = Math.floor(y / CELL);
      for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
          const gx = cx + dx;
          const gy = cy + dy;
          if (gx >= 0 && gy >= 0 && gx < gcols && gy < grows) {
            grid[gy * gcols + gx] = 1;
          }
        }
      }
    });
  };
  const blocked = (pts: Point[], grid: Uint8Array) => {
    let hit = false;
    eachSample(pts, (x, y) => {
      if (hit || exemptAt(x, y)) {
        return;
      }
      const gx = Math.min(gcols - 1, Math.max(0, Math.floor(x / CELL)));
      const gy = Math.min(grows - 1, Math.max(0, Math.floor(y / CELL)));
      if (grid[gy * gcols + gx]) {
        hit = true;
      }
    });
    return hit;
  };
  const pushViaStart = (t: Trace) => {
    const s = t.pts[0];
    const nx = t.pts[1] ?? s;
    const len = Math.hypot(nx.x - s.x, nx.y - s.y) || 1;
    vias.push({
      x: s.x,
      y: s.y,
      dx: (s.x - nx.x) / len,
      dy: (s.y - nx.y) / len,
    });
  };
  // Motif internal lanes are already fully shielded by crossesMotif (walks
  // never enter a motif rect), so they need no stamping.

  // The message spine: one signal net down the centre gutter, fed by the
  // nearest service node and stubbed into every motif block in scroll order.
  // Laid before the random copper and stamped, so nothing crosses the gutter.
  if (ordered.length) {
    const first = ordered[0];
    const last = ordered[ordered.length - 1];
    const spineTop = { x: gutterX, y: first.y - 120 };
    const spineBot = { x: gutterX, y: last.y + 120 };
    exemptPts.push(spineTop);
    const spine = traceFrom([spineTop, spineBot], -1, true);
    traces.push(spine);
    connectors.push(spine);
    pushVia(spine);
    stamp(spine.pts, occTop);
    if (nodes.length) {
      let ni = 0;
      let best = Infinity;
      for (let i = 0; i < nodes.length; i++) {
        const d = Math.hypot(nodes[i].x - gutterX, nodes[i].y - spineTop.y);
        if (d < best) {
          best = d;
          ni = i;
        }
      }
      const feeder = connector(nodes[ni], spineTop, -1);
      traces.push(feeder);
      connectors.push(feeder);
      outgoing[ni].push(feeder);
      stamp(feeder.pts, occTop);
    }
    ordered.forEach((mo, idx) => {
      mo.block = idx + 1;
      const edgeX = mo.x < gutterX ? mo.x + 234 : mo.x - 234;
      const stubEnd = { x: edgeX, y: mo.y };
      // Dock the spine onto the nearest real pad or lane end of the circuit,
      // so the message lands on copper the block actually uses, not a dead via.
      const candidates: Point[] = [];
      for (const b of mo.boxes) {
        for (const pad of b.pads ?? []) {
          candidates.push({ x: pad.x + pad.nx * 5, y: pad.y });
        }
      }
      const laneRefs = [
        ...mo.inLanes,
        ...mo.outLanes,
        ...mo.segs,
        ...mo.branches,
        ...(mo.mainLane ? [mo.mainLane] : []),
      ];
      for (const t of laneRefs) {
        candidates.push(t.pts[0], t.pts[t.pts.length - 1]);
      }
      let dock = stubEnd;
      let bd = Infinity;
      for (const c of candidates) {
        const d = Math.hypot(c.x - stubEnd.x, c.y - stubEnd.y);
        if (d < bd) {
          bd = d;
          dock = c;
        }
      }
      const pts =
        bd < 150
          ? [{ x: gutterX, y: mo.y }, ...connector(stubEnd, dock, -1).pts]
          : [{ x: gutterX, y: mo.y }, stubEnd];
      const inStub = traceFrom(pts, -1, true);
      traces.push(inStub);
      connectors.push(inStub);
      pushVia(inStub);
      stamp(inStub.pts, occTop);
      mo.inStub = inStub;
      mo.inPad = { x: dock.x, y: dock.y, flash: 0 };
      mo.rings.push({ x: dock.x, y: dock.y });
    });
  }

  // Node backbone, confined to the hero band and never crossing a motif.
  // Deliberate copper: laid and stamped, never rejected against the grid.
  const heroBandY = ordered.length ? ordered[0].y - 150 : h;
  for (let i = 0; i < nodes.length; i++) {
    if (nodes[i].y > heroBandY) {
      continue;
    }
    const others = nodes
      .map((n, j) => ({ j, d: Math.hypot(n.x - nodes[i].x, n.y - nodes[i].y) }))
      .filter((o) => o.j > i && o.d < 620 && nodes[o.j].y <= heroBandY)
      .sort((a, b) => a.d - b.d)
      .slice(0, 2);
    for (const o of others) {
      const laneT = connector(nodes[i], nodes[o.j], o.j);
      if (crossesMotif(laneT.pts)) {
        continue;
      }
      const back = traceFrom([...laneT.pts].reverse(), i, true);
      connectors.push(laneT, back);
      outgoing[i].push(laneT);
      outgoing[o.j].push(back);
      traces.push(laneT);
      stamp(laneT.pts, occTop);
    }
  }

  // Radial node traces: checked against the grid, demoted or dropped on a hit.
  for (const node of nodes) {
    const count = 4 + Math.floor(rand() * 2);
    // Fan the radial traces out in distinct directions so they emanate
    // cleanly from the node instead of overlapping into a tangle.
    const base = Math.floor(rand() * 8);
    const used = new Set<number>();
    for (let i = 0; i < count; i++) {
      let dir = (base + i * 2 + (rand() < 0.5 ? 0 : 1)) % 8;
      while (used.has(dir)) {
        dir = (dir + 1) % 8;
      }
      used.add(dir);
      const start = {
        x: node.x + DIR_VECS[dir].x * GRID * 0.36,
        y: node.y + DIR_VECS[dir].y * GRID * 0.36,
      };
      const t = walk(rand, start, dir, w, h);
      if (!t || crossesMotif(t.pts)) {
        continue;
      }
      if (!blocked(t.pts, occTop)) {
        traces.push(t);
        pushVia(t);
        stamp(t.pts, occTop);
      } else if (!blocked(t.pts, occInner)) {
        t.layer = 1;
        traces.push(t);
        pushVia(t);
        stamp(t.pts, occInner);
      }
    }
  }

  // Ambient filler: same discipline. Demoted walks (inner layer) get a via at
  // both ends so a dim trace reads as entering and leaving through plated holes.
  const ambient = Math.min(900, Math.floor((w * h) / 9000));
  for (let i = 0; i < ambient; i++) {
    const start = {
      x: GRID * (1 + Math.floor(rand() * (w / GRID - 2))),
      y: GRID * (1 + Math.floor(rand() * (h / GRID - 2))),
    };
    const t = walk(rand, start, Math.floor(rand() * 8), w, h);
    if (!t || crossesMotif(t.pts)) {
      continue;
    }
    if (!blocked(t.pts, occTop)) {
      traces.push(t);
      pushVia(t);
      stamp(t.pts, occTop);
    } else if (!blocked(t.pts, occInner)) {
      t.layer = 1;
      traces.push(t);
      pushVia(t);
      pushViaStart(t);
      stamp(t.pts, occInner);
    }
  }

  // Keep-outs so board furniture never lands on a service node or a motif.
  const keepOuts: Rect[] = [];
  for (const n of nodes) {
    keepOuts.push({ x: n.x - 52, y: n.y - 44, w: 104, h: 96 });
  }
  // Furniture keep-out is the circuit courtyard (its parts, lanes and labels),
  // not the whole block, so passives, footprints and silks fill the frame
  // corners and each motif reads as a populated region, not a cleared island.
  for (const mo of motifs) {
    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;
    const add = (x: number, y: number) => {
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    };
    for (const b of mo.boxes) {
      add(b.x, b.y);
      add(b.x + b.w, b.y + b.h);
    }
    const courtLanes = [
      ...mo.inLanes,
      ...mo.outLanes,
      ...mo.segs,
      ...mo.branches,
      ...(mo.mainLane ? [mo.mainLane] : []),
    ];
    for (const t of courtLanes) {
      for (const p of t.pts) {
        add(p.x, p.y);
      }
    }
    for (const r of mo.rings) {
      add(r.x, r.y);
    }
    for (const lb of mo.labels) {
      const halfW = lb.text.length * 2.4;
      const lcx =
        lb.anchor === "start"
          ? lb.x + halfW
          : lb.anchor === "end"
            ? lb.x - halfW
            : lb.x;
      add(lcx - halfW, lb.y - 6);
      add(lcx + halfW, lb.y + 2);
    }
    const cm = 18;
    keepOuts.push({
      x: minX - cm,
      y: minY - cm,
      w: maxX - minX + 2 * cm,
      h: maxY - minY + 2 * cm,
    });
  }
  if (ordered.length) {
    const first = ordered[0];
    const last = ordered[ordered.length - 1];
    keepOuts.push({
      x: gutterX - 26,
      y: first.y - 132,
      w: 52,
      h: last.y - first.y + 264,
    });
  }
  const clashes = (r: Rect) =>
    keepOuts.some(
      (k) =>
        r.x < k.x + k.w &&
        r.x + r.w > k.x &&
        r.y < k.y + k.h &&
        r.y + r.h > k.y,
    );

  // Hatched ground-plane patches: the copper pour a real board is mostly made of.
  const hatches: Rect[] = [];
  const hatchCount = Math.min(26, Math.floor((w * h) / 150000));
  for (let i = 0; i < hatchCount; i++) {
    const patch: Rect = {
      x: GRID * Math.floor(rand() * (w / GRID - 8)),
      y: GRID * Math.floor(rand() * (h / GRID - 8)),
      w: GRID * (3 + Math.floor(rand() * 5)),
      h: GRID * (2 + Math.floor(rand() * 4)),
    };
    if (!clashes(patch)) {
      hatches.push(patch);
    }
  }

  // IC footprints scattered across the board, so it reads as populated.
  const footprints: Footprint[] = [];
  const footCount = Math.min(42, Math.floor((w * h) / 90000));
  for (let i = 0; i < footCount; i++) {
    const foot: Footprint = {
      x: GRID * Math.floor(rand() * (w / GRID - 5)),
      y: GRID * Math.floor(rand() * (h / GRID - 4)),
      w: GRID * (2 + Math.floor(rand() * 3)),
      h: GRID * (1 + Math.floor(rand() * 2)),
    };
    if (!clashes(foot)) {
      footprints.push(foot);
    }
  }
  for (const foot of footprints) {
    foot.pads = footPads(foot);
    foot.escapes = padEscapes(
      foot.pads,
      posRand(foot.x, foot.y),
      (x, y) => !inMotif({ x, y }),
    );
  }

  // Passive two-pad parts (0402/0805): smaller and more numerous than the ICs,
  // they fill the board believably.
  const passives: Passive[] = [];
  const passiveCount = Math.min(90, Math.floor((w * h) / 42000));
  for (let i = 0; i < passiveCount; i++) {
    const p: Passive = {
      x: GRID * (1 + Math.floor(rand() * (w / GRID - 2))),
      y: GRID * (1 + Math.floor(rand() * (h / GRID - 2))),
      horiz: rand() < 0.5,
    };
    if (!clashes({ x: p.x - 8, y: p.y - 8, w: 16, h: 16 })) {
      passives.push(p);
    }
  }

  // Silkscreen reference designators near the parts, so it reads as a real board.
  const silks: Silk[] = [];
  for (let i = 0; i < footprints.length; i++) {
    silks.push({
      x: footprints[i].x + 1,
      y: footprints[i].y - 4,
      text: `U${1 + Math.floor(rand() * 79)}`,
    });
  }
  const passivePrefix = ["R", "C", "L", "D"];
  for (let i = 0; i < passives.length; i += 2) {
    silks.push({
      x: passives[i].x - 7,
      y: passives[i].y - 6,
      text: `${passivePrefix[Math.floor(rand() * passivePrefix.length)]}${1 + Math.floor(rand() * 89)}`,
    });
  }
  const loosePrefix = ["TP", "J", "R", "C"];
  const looseCount = Math.min(30, Math.floor((w * h) / 120000));
  for (let i = 0; i < looseCount; i++) {
    const s: Silk = {
      x: GRID * (1 + Math.floor(rand() * (w / GRID - 3))),
      y: GRID * (1 + Math.floor(rand() * (h / GRID - 3))),
      text: `${loosePrefix[Math.floor(rand() * loosePrefix.length)]}${1 + Math.floor(rand() * 89)}`,
    };
    if (!clashes({ x: s.x - 4, y: s.y - 8, w: 30, h: 12 })) {
      silks.push(s);
    }
  }

  const bins: number[][] = [];
  for (let i = 0; i < traces.length; i++) {
    let minY = Infinity;
    let maxY = -Infinity;
    for (const p of traces[i].pts) {
      minY = Math.min(minY, p.y);
      maxY = Math.max(maxY, p.y);
    }
    const from = Math.max(0, Math.floor(minY / BIN_H));
    const to = Math.floor(maxY / BIN_H);
    for (let b = from; b <= to; b++) {
      (bins[b] ??= []).push(i);
    }
  }

  return {
    traces,
    connectors,
    outgoing,
    nodes,
    vias,
    motifs,
    hatches,
    footprints,
    passives,
    silks,
    bins,
  };
}

interface BoardParams {
  opacity: number;
  ambient: number;
  lightGain: number;
}

const DEFAULT_PARAMS: BoardParams = { opacity: 1, ambient: 0, lightGain: 1 };

interface BoardBackgroundProps {
  readonly paramsRef: React.RefObject<BoardParams>;
}

function BoardBackground({ paramsRef }: BoardBackgroundProps) {
  const rootRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    const canvas = canvasRef.current;
    if (!root || !canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    const lit = document.createElement("canvas");
    const mask = document.createElement("canvas");
    const litCtx = lit.getContext("2d");
    const maskCtx = mask.getContext("2d");
    if (!ctx || !litCtx || !maskCtx) {
      return;
    }
    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;

    let board: Board | null = null;
    let pulses: Pulse[] = [];
    let power: number[] = [];
    let flash: number[] = [];
    let hover: number[] = [];
    let hoveredIndex = -1;
    const cursor = { x: -9999, y: -9999, active: false };
    let cleanups: Array<() => void> = [];
    let dpr = 1;
    let viewW = 0;
    let viewH = 0;
    let raf = 0;
    let last = 0;
    let spawnClock = 0;
    let disposed = false;

    function sizeCanvases() {
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      viewW = window.innerWidth;
      viewH = window.innerHeight;
      for (const c of [canvas!, lit, mask]) {
        c.width = Math.round(viewW * dpr);
        c.height = Math.round(viewH * dpr);
      }
    }

    function emitNode(nodeIndex: number) {
      if (!board || pulses.length >= 16) {
        return;
      }
      const lanes = (board.outgoing[nodeIndex] ?? []).filter((t) => t.len > 60);
      if (lanes.length === 0) {
        return;
      }
      const t = lanes[Math.floor(Math.random() * lanes.length)];
      pulses.push({
        trace: t,
        dist: 0,
        speed: 160 + Math.random() * 90,
        to: t.to,
      });
    }

    function spawnAmbient() {
      if (!board || pulses.length >= 10) {
        return;
      }
      const lanes = board.connectors.filter((t) => t.len > 60);
      if (lanes.length === 0) {
        return;
      }
      const t = lanes[Math.floor(Math.random() * lanes.length)];
      pulses.push({
        trace: t,
        dist: 0,
        speed: 160 + Math.random() * 90,
        to: t.to,
      });
    }

    function attach(chips: HTMLElement[]) {
      for (const c of cleanups) {
        c();
      }
      cleanups = chips.map((chip, i) => {
        const enter = () => {
          hoveredIndex = i;
          if (!reduced) {
            emitNode(i);
            emitNode(i);
          }
        };
        const leave = () => {
          if (hoveredIndex === i) {
            hoveredIndex = -1;
          }
        };
        chip.addEventListener("pointerenter", enter);
        chip.addEventListener("pointerleave", leave);
        return () => {
          chip.removeEventListener("pointerenter", enter);
          chip.removeEventListener("pointerleave", leave);
        };
      });
    }

    function measureAndBuild() {
      sizeCanvases();
      const container = root!.parentElement ?? root!;
      const scrollY = window.scrollY;
      const rootRect = root!.getBoundingClientRect();
      const originY = rootRect.top + scrollY;
      const chips = Array.from(
        container.querySelectorAll<HTMLElement>("[data-v13-node]"),
      );
      const nodes: Point[] = chips.map((chip) => {
        const r = chip.getBoundingClientRect();
        return {
          x: r.left + r.width / 2,
          y: r.top + r.height / 2 + scrollY - originY,
        };
      });
      const anchors: MotifAnchorPoint[] = Array.from(
        container.querySelectorAll<HTMLElement>("[data-v13-motif]"),
      )
        .filter((el) => el.offsetParent !== null)
        .map((el) => {
          const r = el.getBoundingClientRect();
          return {
            kind: el.dataset.v13Motif as MotifKind,
            x: r.left + r.width / 2,
            y: r.top + r.height / 2 + scrollY - originY,
          };
        });
      board = generateBoard(
        nodes,
        anchors,
        container.clientWidth,
        container.scrollHeight,
      );
      pulses = [];
      power = nodes.map((_, i) => (reduced ? 1 : (power[i] ?? 0)));
      flash = nodes.map(() => 0);
      hover = nodes.map(() => 0);
      attach(chips);
    }

    function boardTop() {
      return -root!.getBoundingClientRect().top;
    }

    function drawBoardSlice(top: number) {
      if (!board) {
        return;
      }
      litCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      litCtx!.globalCompositeOperation = "source-over";
      litCtx!.clearRect(0, 0, viewW, viewH);
      litCtx!.translate(0, -top);
      const bottom = top + viewH;

      litCtx!.fillStyle = SUBSTRATE;
      litCtx!.fillRect(0, top, viewW, viewH);
      litCtx!.fillStyle = PAD_COLOR;
      const pitch = GRID * 2;
      const firstRow = Math.floor(top / pitch) * pitch;
      for (let y = firstRow; y <= bottom; y += pitch) {
        for (let x = 0; x <= viewW; x += pitch) {
          litCtx!.fillRect(x - 0.8, y - 0.8, 1.6, 1.6);
        }
      }

      // Hatched ground-plane patches, under the traces.
      litCtx!.lineWidth = 1;
      for (const patch of board.hatches) {
        if (patch.y + patch.h < top || patch.y > bottom) {
          continue;
        }
        litCtx!.save();
        litCtx!.beginPath();
        litCtx!.rect(patch.x, patch.y, patch.w, patch.h);
        litCtx!.clip();
        litCtx!.strokeStyle = HATCH_COLOR;
        litCtx!.beginPath();
        for (let x = patch.x - patch.h; x <= patch.x + patch.w; x += 9) {
          litCtx!.moveTo(x, patch.y + patch.h);
          litCtx!.lineTo(x + patch.h, patch.y);
        }
        litCtx!.stroke();
        litCtx!.restore();
        litCtx!.strokeStyle = HATCH_EDGE;
        litCtx!.strokeRect(patch.x, patch.y, patch.w, patch.h);
      }

      litCtx!.lineCap = "round";
      litCtx!.lineJoin = "round";
      const binFrom = Math.max(0, Math.floor(top / BIN_H));
      const binTo = Math.floor(bottom / BIN_H);
      const drawn = new Set<number>();
      const visible: number[] = [];
      for (let b = binFrom; b <= binTo; b++) {
        for (const i of board.bins[b] ?? []) {
          if (drawn.has(i)) {
            continue;
          }
          drawn.add(i);
          visible.push(i);
        }
      }
      const strokeTrace = (t: Trace) => {
        litCtx!.beginPath();
        litCtx!.moveTo(t.pts[0].x, t.pts[0].y);
        for (let p = 1; p < t.pts.length; p++) {
          litCtx!.lineTo(t.pts[p].x, t.pts[p].y);
        }
        litCtx!.stroke();
      };
      // Inner-layer copper first, dim and thin, so top-layer copper paints over
      // it and each crossing reads as a genuine two-layer board.
      litCtx!.lineWidth = 1.1;
      litCtx!.strokeStyle = TRACE_ALT_COLOR;
      for (const i of visible) {
        const t = board.traces[i];
        if (t.connector || t.layer !== 1) {
          continue;
        }
        strokeTrace(t);
      }
      // Top-layer copper: signal lanes fatter than ambient filler.
      for (const i of visible) {
        const t = board.traces[i];
        if (!t.connector && t.layer === 1) {
          continue;
        }
        if (t.connector) {
          litCtx!.lineWidth = 2.2;
          litCtx!.strokeStyle = LANE_COLOR;
        } else {
          litCtx!.lineWidth = 1.4;
          litCtx!.strokeStyle = TRACE_COLOR;
        }
        strokeTrace(t);
      }

      // Scattered IC footprints populate the board between the concept motifs.
      for (const foot of board.footprints) {
        if (foot.y + foot.h < top - 36 || foot.y > bottom + 36) {
          continue;
        }
        // Pin escapes + vias, drawn first so the package body tucks over them.
        litCtx!.strokeStyle = TRACE_COLOR;
        litCtx!.lineWidth = 1;
        for (const esc of foot.escapes ?? []) {
          litCtx!.beginPath();
          litCtx!.moveTo(esc[0].x, esc[0].y);
          for (let p = 1; p < esc.length; p++) {
            litCtx!.lineTo(esc[p].x, esc[p].y);
          }
          litCtx!.stroke();
          const e = esc[esc.length - 1];
          litCtx!.fillStyle = VIA_COLOR;
          litCtx!.beginPath();
          litCtx!.arc(e.x, e.y, 1.5, 0, Math.PI * 2);
          litCtx!.fill();
          litCtx!.fillStyle = SUBSTRATE;
          litCtx!.beginPath();
          litCtx!.arc(e.x, e.y, 0.7, 0, Math.PI * 2);
          litCtx!.fill();
        }
        litCtx!.fillStyle = FOOT_BODY;
        litCtx!.strokeStyle = FOOT_EDGE;
        litCtx!.lineWidth = 1.1;
        litCtx!.beginPath();
        litCtx!.roundRect(foot.x, foot.y, foot.w, foot.h, 3);
        litCtx!.fill();
        litCtx!.stroke();
        litCtx!.fillStyle = FOOT_PIN;
        for (let px = foot.x + 7; px <= foot.x + foot.w - 9; px += 10) {
          litCtx!.fillRect(px, foot.y - 4, 3, 4);
          litCtx!.fillRect(px, foot.y + foot.h, 3, 4);
        }
      }

      // Passive two-pad parts (resistors/caps): two copper lands and a body.
      for (const p of board.passives) {
        if (p.y < top - 8 || p.y > bottom + 8) {
          continue;
        }
        if (p.horiz) {
          litCtx!.fillStyle = PASSIVE_BODY;
          litCtx!.fillRect(p.x - 3, p.y - 2.4, 6, 4.8);
          litCtx!.fillStyle = PASSIVE_PAD;
          litCtx!.fillRect(p.x - 6, p.y - 2.8, 3, 5.6);
          litCtx!.fillRect(p.x + 3, p.y - 2.8, 3, 5.6);
        } else {
          litCtx!.fillStyle = PASSIVE_BODY;
          litCtx!.fillRect(p.x - 2.4, p.y - 3, 4.8, 6);
          litCtx!.fillStyle = PASSIVE_PAD;
          litCtx!.fillRect(p.x - 2.8, p.y - 6, 5.6, 3);
          litCtx!.fillRect(p.x - 2.8, p.y + 3, 5.6, 3);
        }
      }

      // Motif furniture (boxes, endpoint rings, labels) is part of the board.
      for (const mo of board.motifs) {
        if (mo.bottom < top - 20 || mo.top > bottom + 20) {
          continue;
        }
        // Silkscreen block boundary + designator, so the motif reads as a
        // labelled functional zone of the board, not a cleared rectangle.
        const bx = mo.x - 226;
        const by = mo.y - 138;
        litCtx!.save();
        litCtx!.strokeStyle = "rgba(150, 168, 198, 0.2)";
        litCtx!.lineWidth = 1;
        litCtx!.setLineDash([5, 5]);
        litCtx!.beginPath();
        litCtx!.roundRect(bx, by, 452, 276, 10);
        litCtx!.stroke();
        litCtx!.restore();
        litCtx!.fillStyle = "rgba(150, 168, 198, 0.5)";
        litCtx!.textAlign = "left";
        litCtx!.font = `600 8px ${MONO_FONT}`;
        litCtx!.fillText(
          `BLK${mo.block ?? 0} · ${mo.kind.toUpperCase()}`,
          bx + 10,
          by + 14,
        );
        let chipIdx = 0;
        for (const box of mo.boxes) {
          chipIdx++;
          const bcx = box.x + box.w / 2;
          const bcy = box.y + box.h / 2;
          // Pin escapes off the pads that carry no lane, drawn first so the
          // package body tucks over them like real solder.
          litCtx!.strokeStyle = TRACE_COLOR;
          litCtx!.lineWidth = 1;
          for (const esc of box.escapes ?? []) {
            litCtx!.beginPath();
            litCtx!.moveTo(esc[0].x, esc[0].y);
            for (let p = 1; p < esc.length; p++) {
              litCtx!.lineTo(esc[p].x, esc[p].y);
            }
            litCtx!.stroke();
            const e = esc[esc.length - 1];
            litCtx!.fillStyle = VIA_COLOR;
            litCtx!.beginPath();
            litCtx!.arc(e.x, e.y, 1.5, 0, Math.PI * 2);
            litCtx!.fill();
            litCtx!.fillStyle = SUBSTRATE;
            litCtx!.beginPath();
            litCtx!.arc(e.x, e.y, 0.7, 0, Math.PI * 2);
            litCtx!.fill();
          }
          // Lead pads down both long sides, snapped to where lanes land.
          litCtx!.fillStyle = CHIP_PIN;
          for (const pad of box.pads ?? []) {
            const px = pad.nx < 0 ? box.x - 5 : box.x + box.w;
            litCtx!.fillRect(px, pad.y - 1.5, 5, 3);
          }
          // Package body.
          litCtx!.fillStyle = CHIP_BODY;
          litCtx!.strokeStyle = CHIP_EDGE;
          litCtx!.lineWidth = 1.1;
          litCtx!.beginPath();
          litCtx!.roundRect(box.x, box.y, box.w, box.h, 2.5);
          litCtx!.fill();
          litCtx!.stroke();
          // Pin-1 dot.
          litCtx!.fillStyle = CHIP_PIN1;
          litCtx!.beginPath();
          litCtx!.arc(box.x + 5.5, box.y + 5.5, 1.4, 0, Math.PI * 2);
          litCtx!.fill();
          // Function name on the package.
          litCtx!.fillStyle = CHIP_LABEL;
          litCtx!.textAlign = "center";
          litCtx!.font = `600 9.5px ${MONO_FONT}`;
          litCtx!.fillText(box.label, bcx, bcy + 3.5);
          // Silkscreen reference designator above the package.
          litCtx!.fillStyle = SILK_DIM;
          litCtx!.font = `8px ${MONO_FONT}`;
          litCtx!.fillText(`U${chipIdx}`, bcx, box.y - 5);
        }
        for (const r of mo.rings) {
          // Copper annular pad with a plated hole and a bright centre land.
          litCtx!.fillStyle = "rgba(176, 192, 218, 0.72)";
          litCtx!.beginPath();
          litCtx!.arc(r.x, r.y, 3.4, 0, Math.PI * 2);
          litCtx!.fill();
          litCtx!.fillStyle = SUBSTRATE;
          litCtx!.beginPath();
          litCtx!.arc(r.x, r.y, 1.9, 0, Math.PI * 2);
          litCtx!.fill();
          litCtx!.fillStyle = "rgba(236, 242, 252, 0.9)";
          litCtx!.beginPath();
          litCtx!.arc(r.x, r.y, 1, 0, Math.PI * 2);
          litCtx!.fill();
        }
        for (const lb of mo.labels) {
          litCtx!.textAlign = lb.anchor;
          litCtx!.fillStyle = lb.dim ? LABEL_DIM : LABEL_COLOR;
          litCtx!.font = `600 8px ${MONO_FONT}`;
          litCtx!.fillText(lb.text, lb.x, lb.y);
        }
        litCtx!.textAlign = "start";
      }

      // Vias: a teardrop fillet where the trace meets the pad (so it reads as
      // routed, not drawn), then a crisp copper ring with a plated hole.
      const padR = 2.3;
      for (const via of board.vias) {
        if (via.y < top - 8 || via.y > bottom + 8) {
          continue;
        }
        litCtx!.fillStyle = VIA_COLOR;
        // Teardrop: a wedge from the incoming trace out to the pad edge.
        const ax = via.x - via.dx * padR * 2.7;
        const ay = via.y - via.dy * padR * 2.7;
        const px = -via.dy * padR;
        const py = via.dx * padR;
        litCtx!.beginPath();
        litCtx!.moveTo(ax, ay);
        litCtx!.lineTo(via.x + px, via.y + py);
        litCtx!.lineTo(via.x - px, via.y - py);
        litCtx!.closePath();
        litCtx!.fill();
        // Annular ring.
        litCtx!.beginPath();
        litCtx!.arc(via.x, via.y, padR, 0, Math.PI * 2);
        litCtx!.fill();
        litCtx!.fillStyle = SUBSTRATE;
        litCtx!.beginPath();
        litCtx!.arc(via.x, via.y, 1.05, 0, Math.PI * 2);
        litCtx!.fill();
      }

      // Silkscreen reference designators, printed on top like a real board.
      litCtx!.fillStyle = SILK_SCATTER;
      litCtx!.textAlign = "left";
      litCtx!.font = `7px ${MONO_FONT}`;
      for (const silk of board.silks) {
        if (silk.y < top - 10 || silk.y > bottom + 10) {
          continue;
        }
        litCtx!.fillText(silk.text, silk.x, silk.y);
      }
    }

    function nodeLevel(i: number, time: number): number {
      const flicker = 0.9 + 0.06 * Math.sin(time / 720 + i * 1.7);
      return flicker * power[i] * (1 + 0.4 * hover[i] + 0.5 * flash[i]);
    }

    function drawLightMask(top: number, time: number) {
      if (!board) {
        return;
      }
      maskCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      maskCtx!.clearRect(0, 0, viewW, viewH);
      maskCtx!.translate(0, -top);
      const bottom = top + viewH;
      const margin = NODE_LIGHT_RADIUS * 1.3;
      const params = paramsRef.current ?? DEFAULT_PARAMS;
      const gain = params.lightGain;
      // Ambient floor: reveal the whole board faintly, independent of the light
      // pools, so the background can be made as present as the reader wants.
      if (params.ambient > 0) {
        maskCtx!.fillStyle = `rgba(255,255,255,${params.ambient})`;
        maskCtx!.fillRect(0, top, viewW, viewH);
      }

      for (let i = 0; i < board.nodes.length; i++) {
        const n = board.nodes[i];
        if (n.y < top - margin || n.y > bottom + margin) {
          continue;
        }
        const level = nodeLevel(i, time) * gain;
        if (level <= 0.01) {
          continue;
        }
        // Three slowly drifting light pools instead of one hard disc, so the
        // reveal reads as an organic glow rather than a circular spot.
        const radius = NODE_LIGHT_RADIUS * (1 + 0.12 * flash[i]);
        const p1 = time * 0.00041 + i * 2.4;
        const p2 = time * 0.00031 + i * 1.3;
        const blobs = [
          { dx: 0, dy: 0, r: radius, a: 0.82 },
          {
            dx: Math.cos(p1) * 26,
            dy: Math.sin(p2) * 22,
            r: radius * 0.64,
            a: 0.5,
          },
          {
            dx: -Math.sin(p2) * 30,
            dy: -Math.cos(p1) * 20,
            r: radius * 0.5,
            a: 0.42,
          },
        ];
        for (const b of blobs) {
          const bx = n.x + b.dx;
          const by = n.y + b.dy;
          const a = b.a * level;
          const g = maskCtx!.createRadialGradient(bx, by, 0, bx, by, b.r);
          g.addColorStop(0, `rgba(255,255,255,${a})`);
          g.addColorStop(0.55, `rgba(255,255,255,${a * 0.42})`);
          g.addColorStop(1, "rgba(255,255,255,0)");
          maskCtx!.fillStyle = g;
          maskCtx!.beginPath();
          maskCtx!.arc(bx, by, b.r, 0, Math.PI * 2);
          maskCtx!.fill();
        }
      }

      // Motif light: a bright spread of overlapping pools across the whole
      // footprint (not one disc) so every component reads, with soft organic
      // edges and a gentle breathe.
      for (const mo of board.motifs) {
        if (mo.bottom < top - 40 || mo.top > bottom + 40 || mo.level <= 0.01) {
          continue;
        }
        const lp = mo.lamp;
        const breathe = 0.95 + 0.05 * Math.sin(time / 900 + lp.x * 0.01);
        const level = mo.level * breathe * gain;
        const drift = Math.sin(time / 1300 + lp.y * 0.01) * 12;
        const spread = lp.r * 0.92;
        const blobs = [
          { dx: 0, dy: 0, r: lp.r * 1.08, a: 0.95 },
          { dx: -spread, dy: drift, r: lp.r * 0.86, a: 0.9 },
          { dx: spread, dy: -drift, r: lp.r * 0.86, a: 0.9 },
        ];
        for (const b of blobs) {
          const bx = lp.x + b.dx;
          const by = lp.y + b.dy;
          const a = b.a * level;
          const g = maskCtx!.createRadialGradient(bx, by, 0, bx, by, b.r);
          g.addColorStop(0, `rgba(255,255,255,${a})`);
          g.addColorStop(0.5, `rgba(255,255,255,${a * 0.66})`);
          g.addColorStop(1, "rgba(255,255,255,0)");
          maskCtx!.fillStyle = g;
          maskCtx!.beginPath();
          maskCtx!.ellipse(bx, by, b.r, b.r * 0.82, 0, 0, Math.PI * 2);
          maskCtx!.fill();
        }
      }

      // The pointer's own light pool.
      if (cursor.active && cursor.y > top - 170 && cursor.y < bottom + 170) {
        const cr = 165;
        const g = maskCtx!.createRadialGradient(
          cursor.x,
          cursor.y,
          0,
          cursor.x,
          cursor.y,
          cr,
        );
        g.addColorStop(0, `rgba(255,255,255,${0.5 * gain})`);
        g.addColorStop(0.5, `rgba(255,255,255,${0.17 * gain})`);
        g.addColorStop(1, "rgba(255,255,255,0)");
        maskCtx!.fillStyle = g;
        maskCtx!.beginPath();
        maskCtx!.arc(cursor.x, cursor.y, cr, 0, Math.PI * 2);
        maskCtx!.fill();
      }

      for (const pulse of pulses) {
        const alpha = envelope(pulse) * (pulse.dim ? 0.6 : 1) * gain;
        if (alpha <= 0) {
          continue;
        }
        for (let k = 0; k < 3; k++) {
          const d = pulse.dist - (k * PULSE_TRAIL) / 2.5;
          if (d < 0) {
            break;
          }
          const p = pointAt(pulse.trace, d);
          if (
            p.y < top - PULSE_LIGHT_RADIUS ||
            p.y > bottom + PULSE_LIGHT_RADIUS
          ) {
            continue;
          }
          const r = PULSE_LIGHT_RADIUS * (1 - k * 0.22);
          const a = alpha * (1 - k * 0.3) * 0.85;
          const g = maskCtx!.createRadialGradient(p.x, p.y, 0, p.x, p.y, r);
          g.addColorStop(0, `rgba(255,255,255,${a})`);
          g.addColorStop(1, "rgba(255,255,255,0)");
          maskCtx!.fillStyle = g;
          maskCtx!.beginPath();
          maskCtx!.arc(p.x, p.y, r, 0, Math.PI * 2);
          maskCtx!.fill();
        }
      }
    }

    function drawGlows(top: number, time: number) {
      if (!board) {
        return;
      }
      ctx!.save();
      ctx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx!.translate(0, -top);
      const bottom = top + viewH;

      // Always-on faint lanes (nodes + motifs) so connections read.
      ctx!.globalCompositeOperation = "source-over";
      ctx!.lineCap = "round";
      ctx!.lineJoin = "round";
      ctx!.lineWidth = 1;
      ctx!.strokeStyle = LANE_ALWAYS;
      const binFrom = Math.max(0, Math.floor(top / BIN_H));
      const binTo = Math.floor(bottom / BIN_H);
      const drawn = new Set<number>();
      for (let b = binFrom; b <= binTo; b++) {
        for (const i of board.bins[b] ?? []) {
          if (drawn.has(i) || !board.traces[i].connector) {
            continue;
          }
          drawn.add(i);
          const t = board.traces[i];
          ctx!.beginPath();
          ctx!.moveTo(t.pts[0].x, t.pts[0].y);
          for (let p = 1; p < t.pts.length; p++) {
            ctx!.lineTo(t.pts[p].x, t.pts[p].y);
          }
          ctx!.stroke();
        }
      }

      ctx!.globalCompositeOperation = "lighter";

      // A soft light bloom sits over each motif as its section comes into
      // focus, lifting the muted board structure printed there into view
      // without drawing a brighter copy on top of it.
      for (const mo of board.motifs) {
        if (mo.bottom < top - 40 || mo.top > bottom + 40 || mo.level <= 0.02) {
          continue;
        }
        const lp = mo.lamp;
        const bloom = ctx!.createRadialGradient(
          lp.x,
          lp.y,
          0,
          lp.x,
          lp.y,
          lp.r * 1.05,
        );
        bloom.addColorStop(0, `rgba(${NODE_HALO},${0.1 * mo.level})`);
        bloom.addColorStop(0.5, `rgba(${NODE_HALO},${0.04 * mo.level})`);
        bloom.addColorStop(1, `rgba(${NODE_HALO},0)`);
        ctx!.fillStyle = bloom;
        ctx!.beginPath();
        ctx!.ellipse(lp.x, lp.y, lp.r * 1.6, lp.r * 0.86, 0, 0, Math.PI * 2);
        ctx!.fill();
      }

      for (let i = 0; i < board.nodes.length; i++) {
        const n = board.nodes[i];
        if (n.y < top - NODE_LIGHT_RADIUS || n.y > bottom + NODE_LIGHT_RADIUS) {
          continue;
        }
        const level = nodeLevel(i, time);
        if (level <= 0.01) {
          continue;
        }
        // The node shines: a soft halo, a brighter core, and a hot centre,
        // all breathing so it reads as a live light source, not a flat dot.
        const pulse = 0.72 + 0.28 * Math.sin(time / 640 + i * 1.9);
        const glow = level * pulse;
        const haloR = NODE_LIGHT_RADIUS * 0.8;
        const halo = ctx!.createRadialGradient(n.x, n.y, 0, n.x, n.y, haloR);
        halo.addColorStop(0, `rgba(${NODE_HALO},${0.14 * glow})`);
        halo.addColorStop(1, `rgba(${NODE_HALO},0)`);
        ctx!.fillStyle = halo;
        ctx!.beginPath();
        ctx!.arc(n.x, n.y, haloR, 0, Math.PI * 2);
        ctx!.fill();
        const core = ctx!.createRadialGradient(n.x, n.y, 0, n.x, n.y, 30);
        core.addColorStop(0, `rgba(224, 234, 250, ${0.42 * glow})`);
        core.addColorStop(0.5, `rgba(${NODE_HALO},${0.18 * glow})`);
        core.addColorStop(1, `rgba(${NODE_HALO},0)`);
        ctx!.fillStyle = core;
        ctx!.beginPath();
        ctx!.arc(n.x, n.y, 30, 0, Math.PI * 2);
        ctx!.fill();
        if (flash[i] > 0.02) {
          ctx!.strokeStyle = `rgba(${MSG},${0.4 * flash[i]})`;
          ctx!.lineWidth = 1.4;
          ctx!.beginPath();
          ctx!.arc(n.x, n.y, 8 + (1 - flash[i]) * 20, 0, Math.PI * 2);
          ctx!.stroke();
        }
      }

      // A soft bloom under the pointer so its light reads as light.
      if (cursor.active && cursor.y > top - 170 && cursor.y < bottom + 170) {
        const cg = ctx!.createRadialGradient(
          cursor.x,
          cursor.y,
          0,
          cursor.x,
          cursor.y,
          150,
        );
        cg.addColorStop(0, `rgba(${NODE_HALO},0.1)`);
        cg.addColorStop(1, `rgba(${NODE_HALO},0)`);
        ctx!.fillStyle = cg;
        ctx!.beginPath();
        ctx!.arc(cursor.x, cursor.y, 150, 0, Math.PI * 2);
        ctx!.fill();
      }

      // Motif endpoint arrival flashes (coral).
      for (const mo of board.motifs) {
        if (mo.bottom < top - 40 || mo.top > bottom + 40) {
          continue;
        }
        for (const e of mo.endpoints) {
          if (e.flash <= 0.02) {
            continue;
          }
          ctx!.strokeStyle = `rgba(${MSG},${0.6 * e.flash})`;
          ctx!.lineWidth = 1.5;
          ctx!.beginPath();
          ctx!.arc(e.x, e.y, 6 + (1 - e.flash) * 18, 0, Math.PI * 2);
          ctx!.stroke();
        }
        if (mo.inPad && mo.inPad.flash > 0.02) {
          ctx!.strokeStyle = `rgba(${MSG},${0.6 * mo.inPad.flash})`;
          ctx!.lineWidth = 1.5;
          ctx!.beginPath();
          ctx!.arc(
            mo.inPad.x,
            mo.inPad.y,
            6 + (1 - mo.inPad.flash) * 18,
            0,
            Math.PI * 2,
          );
          ctx!.stroke();
        }
      }

      // Coral messages.
      ctx!.lineCap = "round";
      for (const pulse of pulses) {
        const alpha = envelope(pulse) * (pulse.dim ? 0.6 : 1);
        if (alpha <= 0) {
          continue;
        }
        const head = pointAt(pulse.trace, pulse.dist);
        if (head.y < top - PULSE_TRAIL || head.y > bottom + PULSE_TRAIL) {
          continue;
        }
        const chunks = 7;
        ctx!.lineWidth = 1.7;
        let prev = pointAt(pulse.trace, Math.max(0, pulse.dist - PULSE_TRAIL));
        for (let k = 1; k <= chunks; k++) {
          const d = Math.max(
            0,
            pulse.dist - PULSE_TRAIL + (k * PULSE_TRAIL) / chunks,
          );
          const p = pointAt(pulse.trace, d);
          ctx!.strokeStyle = `rgba(${MSG},${alpha * Math.pow(k / chunks, 2) * 0.7})`;
          ctx!.beginPath();
          ctx!.moveTo(prev.x, prev.y);
          ctx!.lineTo(p.x, p.y);
          ctx!.stroke();
          prev = p;
        }
        const glowR = pulse.dim ? 5 : 7;
        const g = ctx!.createRadialGradient(
          head.x,
          head.y,
          0,
          head.x,
          head.y,
          glowR,
        );
        g.addColorStop(0, `rgba(${MSG_SOFT},${alpha * 0.34})`);
        g.addColorStop(1, `rgba(${MSG_SOFT},0)`);
        ctx!.fillStyle = g;
        ctx!.beginPath();
        ctx!.arc(head.x, head.y, glowR, 0, Math.PI * 2);
        ctx!.fill();
        ctx!.fillStyle = `rgba(${MSG_SOFT},${alpha})`;
        ctx!.beginPath();
        ctx!.arc(head.x, head.y, pulse.dim ? 1.3 : 1.7, 0, Math.PI * 2);
        ctx!.fill();
      }
      ctx!.restore();
    }

    function render(time: number) {
      const params = paramsRef.current ?? DEFAULT_PARAMS;
      canvas!.style.opacity = String(params.opacity);
      const top = boardTop();
      drawBoardSlice(top);
      drawLightMask(top, time);
      litCtx!.setTransform(1, 0, 0, 1, 0, 0);
      litCtx!.globalCompositeOperation = "destination-in";
      litCtx!.drawImage(mask, 0, 0);
      litCtx!.globalCompositeOperation = "source-over";
      ctx!.setTransform(1, 0, 0, 1, 0, 0);
      ctx!.globalCompositeOperation = "source-over";
      ctx!.clearRect(0, 0, canvas!.width, canvas!.height);
      ctx!.drawImage(lit, 0, 0);
      drawGlows(top, time);
    }

    function emit(p: Pulse) {
      if (pulses.length < 24) {
        pulses.push(p);
      }
    }

    const BEAT: Record<MotifKind, number> = {
      mediator: 2600,
      pubsub: 2200,
      outbox: 2600,
      saga: 3600,
      trace: 3200,
      transports: 2400,
    };

    // The block's internal choreography, run when the message reaches its dock.
    function fireMotif(mo: Motif) {
      if (mo.kind === "mediator") {
        emit({
          trace: mo.outLanes[0],
          dist: 0,
          speed: 190,
          to: -1,
          ep: mo.endpoints[1],
          replyLane: mo.inLanes[0],
          replyEp: mo.endpoints[0],
          taps: mo.taps.map((t) => ({
            at: t.at,
            trace: t.trace,
            ep: t.ep,
            fired: false,
          })),
        });
      } else if (mo.kind === "pubsub") {
        emit({
          trace: mo.inLanes[0],
          dist: 0,
          speed: 210,
          to: -1,
          ep: mo.endpoints[0],
          fanout: mo.outLanes.map((t, i) => ({
            trace: t,
            ep: mo.endpoints[1 + i],
          })),
        });
      } else if (mo.kind === "outbox") {
        mo.beat++;
        emit({
          trace: mo.segs[0],
          dist: 0,
          speed: 180,
          to: -1,
          ep: mo.endpoints[1],
          retry: mo.beat % 4 === 0,
          originEp: mo.endpoints[0],
        });
      } else if (mo.kind === "saga") {
        mo.beat++;
        if (mo.beat % 3 === 0 && mo.branches.length >= 2) {
          emit({
            trace: mo.segs[0],
            dist: 0,
            speed: 170,
            to: -1,
            ep: mo.endpoints[1],
            remaining: [
              { trace: mo.branches[0], ep: mo.endpoints[3] },
              { trace: mo.branches[1], ep: mo.endpoints[0] },
            ],
          });
        } else {
          emit({
            trace: mo.segs[0],
            dist: 0,
            speed: 170,
            to: -1,
            ep: mo.endpoints[1],
            remaining: [{ trace: mo.segs[1], ep: mo.endpoints[2] }],
          });
        }
      } else if (mo.kind === "trace" && mo.mainLane) {
        emit({
          trace: mo.mainLane,
          dist: 0,
          speed: 150,
          to: -1,
          ep: mo.endpoints[5],
          taps: mo.taps.map((t) => ({
            at: t.at,
            trace: t.trace,
            ep: t.ep,
            childTaps: t.childTaps,
            fired: false,
          })),
        });
      } else if (mo.kind === "transports") {
        // Only the strapped transport conducts; it always drives the shared
        // handler over the same common rail and contract.
        mo.beat++;
        const idx = mo.beat % mo.segs.length;
        emit({
          trace: mo.segs[idx],
          dist: 0,
          speed: 210,
          to: -1,
          ep: mo.endpoints[0],
        });
      }
    }

    function driveMotif(mo: Motif, dt: number) {
      if (mo.level < 0.3) {
        return;
      }
      mo.clock += dt * 1000;
      if (mo.clock < BEAT[mo.kind]) {
        return;
      }
      mo.clock = 0;
      // The message arrives from the spine and lands on the dock pad; its
      // completion fires the block, so one net conducts the whole way in.
      if (mo.inStub && mo.inPad) {
        emit({
          trace: mo.inStub,
          dist: 0,
          speed: 240,
          to: -1,
          ep: mo.inPad,
          arriveMotif: mo,
        });
      } else {
        fireMotif(mo);
      }
    }

    function step(dt: number) {
      if (!board) {
        return;
      }
      const top = boardTop();
      const bottom = top + viewH;
      const center = top + viewH * 0.5;
      for (let i = 0; i < board.nodes.length; i++) {
        const y = board.nodes[i].y;
        if (y > top - 40 && y < bottom + 40) {
          power[i] = Math.min(1, power[i] + dt / 0.8);
        }
        flash[i] = Math.max(0, flash[i] - dt / 0.7);
        const target = i === hoveredIndex ? 1 : 0;
        hover[i] += (target - hover[i]) * Math.min(1, dt / 0.15);
      }

      for (const mo of board.motifs) {
        const onScreen =
          mo.bottom > top + viewH * 0.05 && mo.top < bottom - viewH * 0.05;
        mo.level += ((onScreen ? 1 : 0) - mo.level) * Math.min(1, dt / 0.5);
        for (const e of mo.endpoints) {
          e.flash = Math.max(0, e.flash - dt / 0.7);
        }
        if (mo.inPad) {
          mo.inPad.flash = Math.max(0, mo.inPad.flash - dt / 0.7);
        }
        if (!reduced && Math.abs(mo.y - center) < viewH * 0.6) {
          driveMotif(mo, dt);
        }
      }

      const finished: Pulse[] = [];
      for (const p of pulses) {
        p.dist += p.speed * dt;
        if (p.retry && !p.retried && p.dist >= p.trace.len * 0.5) {
          p.dist = 0;
          p.retried = true;
          if (p.originEp) {
            p.originEp.flash = 1;
          }
        }
        if (p.taps) {
          for (const t of p.taps) {
            if (!t.fired && p.dist >= t.at) {
              t.fired = true;
              emit({
                trace: t.trace,
                dist: 0,
                speed: 220,
                to: -1,
                dim: true,
                ep: t.ep,
                taps: t.childTaps?.map((c) => ({
                  at: c.at,
                  trace: c.trace,
                  ep: c.ep,
                  childTaps: c.childTaps,
                  fired: false,
                })),
              });
            }
          }
        }
        if (p.dist >= p.trace.len) {
          finished.push(p);
        }
      }
      pulses = pulses.filter((p) => p.dist < p.trace.len);
      for (const f of finished) {
        if (f.to >= 0) {
          flash[f.to] = 1;
        }
        if (f.ep) {
          f.ep.flash = 1;
        }
        if (f.arriveMotif) {
          fireMotif(f.arriveMotif);
        }
        if (f.replyLane) {
          emit({
            trace: f.replyLane,
            dist: 0,
            speed: f.speed * 0.85,
            to: -1,
            dim: true,
            ep: f.replyEp,
          });
        }
        if (f.fanout) {
          for (const fo of f.fanout) {
            emit({
              trace: fo.trace,
              dist: 0,
              speed: f.speed,
              to: -1,
              ep: fo.ep,
            });
          }
        }
        if (f.remaining && f.remaining.length > 0) {
          const [h, ...rest] = f.remaining;
          emit({
            trace: h.trace,
            dist: 0,
            speed: f.speed,
            to: -1,
            ep: h.ep,
            remaining: rest,
          });
        }
      }

      spawnClock += dt * 1000;
      if (spawnClock >= 1100) {
        spawnClock = 0;
        if (Math.random() < 0.8) {
          spawnAmbient();
        }
      }
    }

    function loop(time: number) {
      if (disposed) {
        return;
      }
      const dt = last > 0 ? Math.min((time - last) / 1000, 0.05) : 0;
      last = time;
      step(dt);
      render(time);
      raf = requestAnimationFrame(loop);
    }

    function start() {
      if (!reduced && raf === 0) {
        last = 0;
        raf = requestAnimationFrame(loop);
      }
    }
    function stop() {
      if (raf !== 0) {
        cancelAnimationFrame(raf);
        raf = 0;
      }
    }

    measureAndBuild();
    if (reduced) {
      for (const mo of board!.motifs) {
        mo.level = 1;
      }
      render(0);
    } else {
      start();
    }

    // The node markers sit in flex columns whose width depends on their label,
    // so a late web-font load shifts them. Re-measure once fonts settle (and
    // shortly after) so the board aligns to their final positions.
    if (document.fonts?.ready) {
      document.fonts.ready.then(() => {
        if (!disposed) {
          measureAndBuild();
          if (reduced) {
            render(0);
          }
        }
      });
    }
    const settleTimer = window.setTimeout(() => {
      if (!disposed) {
        measureAndBuild();
        if (reduced) {
          render(0);
        }
      }
    }, 600);
    cleanups.push(() => window.clearTimeout(settleTimer));

    const onScroll = () => {
      if (reduced) {
        render(0);
      }
    };
    const onVisibility = () => {
      if (document.hidden) {
        stop();
      } else {
        start();
      }
    };
    let resizeRaf = 0;
    const ro = new ResizeObserver(() => {
      cancelAnimationFrame(resizeRaf);
      resizeRaf = requestAnimationFrame(() => {
        measureAndBuild();
        if (reduced) {
          render(0);
        }
      });
    });
    ro.observe(root);
    window.addEventListener("scroll", onScroll, { passive: true });
    document.addEventListener("visibilitychange", onVisibility);

    // The pointer carries its own light, revealing the board it moves over.
    // Tracked on the window so it works everywhere, including over the copy
    // that sits above the board layer.
    const onPointerMove = (e: PointerEvent) => {
      const rect = root.getBoundingClientRect();
      cursor.x = e.clientX - rect.left;
      cursor.y = e.clientY - rect.top;
      cursor.active = true;
      if (reduced) {
        render(0);
      }
    };
    const onPointerLeave = () => {
      cursor.active = false;
    };
    window.addEventListener("pointermove", onPointerMove, { passive: true });
    document.documentElement.addEventListener("mouseleave", onPointerLeave);

    return () => {
      disposed = true;
      stop();
      cancelAnimationFrame(resizeRaf);
      ro.disconnect();
      window.removeEventListener("scroll", onScroll);
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("pointermove", onPointerMove);
      document.documentElement.removeEventListener(
        "mouseleave",
        onPointerLeave,
      );
      for (const c of cleanups) {
        c();
      }
    };
  }, [paramsRef]);

  return (
    <div ref={rootRef} className="absolute inset-0" aria-hidden="true">
      <canvas
        ref={canvasRef}
        className="pointer-events-none fixed inset-0 h-full w-full"
      />
    </div>
  );
}

interface NodeChipProps {
  readonly label: string;
  readonly className?: string;
}

/** Refined ring-and-dot service marker (a light source on the board). */
function NodeChip({ label, className }: NodeChipProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-1.5 ${className ?? ""}`}
    >
      <span
        data-v13-node
        className="pointer-events-auto relative block h-2.5 w-2.5 rounded-full border border-[rgba(205,216,232,0.55)] transition-colors duration-300 hover:border-[rgba(246,202,190,0.9)]"
      >
        <span className="absolute inset-[2.5px] rounded-full bg-[rgba(232,238,248,0.9)]" />
      </span>
      <span
        className="font-mono text-[0.55rem] tracking-[0.2em] uppercase"
        style={{ color: "rgba(150,166,194,0.6)", fontFamily: MONO_FONT }}
      >
        {label}
      </span>
    </div>
  );
}

/** Invisible anchor: the board prints and illuminates the named motif here. */
function MotifStage({ kind }: { readonly kind: MotifKind }) {
  return (
    <div
      data-v13-motif={kind}
      aria-hidden="true"
      className="pointer-events-none h-64 w-full max-lg:h-52"
    />
  );
}

function Eyebrow({ children }: { readonly children: React.ReactNode }) {
  return (
    <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
      {children}
    </p>
  );
}

const RANGES = [
  {
    scope: "In process",
    name: "Mediator",
    copy: "A command maps to one typed handler. Validation, caching, and telemetry sit around it as middleware.",
  },
  {
    scope: "Across services",
    name: "Bus",
    copy: "Publish an event once. Every service that subscribes reacts on its own schedule, on any transport.",
  },
  {
    scope: "Over time",
    name: "Sagas",
    copy: "Long-running work keeps its state, advances as events arrive, and compensates when a step fails.",
  },
] as const;

const PATTERNS = [
  { name: "Event", question: "Who needs to know?", method: "PublishAsync" },
  { name: "Send", question: "Who should act?", method: "SendAsync" },
  {
    name: "Request-Reply",
    question: "What is the result?",
    method: "RequestAsync",
  },
] as const;

const TRANSPORTS = ["RabbitMQ", "Postgres", "in-process"] as const;

function Card({ children }: { readonly children: React.ReactNode }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-[#0b0f1a]/60 p-6 backdrop-blur-sm">
      {children}
    </div>
  );
}

/** Live tuning panel for the board background. Values persist to localStorage
 * so a chosen look becomes the default on the next visit. */
function BackgroundControls({ paramsRef }: BoardBackgroundProps) {
  const [open, setOpen] = useState(false);
  const [vals, setVals] = useState<BoardParams>(() => {
    if (typeof window === "undefined") {
      return { ...DEFAULT_PARAMS };
    }
    try {
      const raw = window.localStorage.getItem("v13-bg-params");
      if (raw) {
        return { ...DEFAULT_PARAMS, ...JSON.parse(raw) } as BoardParams;
      }
    } catch {
      // ignore malformed persisted state
    }
    return { ...DEFAULT_PARAMS };
  });

  // Push the current values into the live render params (on mount, so persisted
  // values take effect, and on every change).
  useEffect(() => {
    if (paramsRef.current) {
      Object.assign(paramsRef.current, vals);
    }
  }, [paramsRef, vals]);

  const update = (key: keyof BoardParams, value: number) => {
    setVals((prev) => {
      const next = { ...prev, [key]: value };
      try {
        window.localStorage.setItem("v13-bg-params", JSON.stringify(next));
      } catch {
        // ignore storage failures
      }
      return next;
    });
  };

  const reset = () => {
    setVals({ ...DEFAULT_PARAMS });
    try {
      window.localStorage.removeItem("v13-bg-params");
    } catch {
      // ignore storage failures
    }
  };

  const rows: [keyof BoardParams, string, number, number, number][] = [
    ["opacity", "Opacity", 0.15, 1, 0.01],
    ["ambient", "Ambient reveal", 0, 0.6, 0.01],
    ["lightGain", "Light intensity", 0.4, 2.4, 0.05],
  ];

  return (
    <div className="text-cc-ink fixed bottom-5 left-20 z-50 font-mono">
      {open ? (
        <div className="border-cc-card-border bg-cc-card-bg/95 w-64 rounded-xl border p-3 backdrop-blur">
          <div className="flex items-center justify-between">
            <span className="text-cc-ink-dim text-[10px] tracking-wider uppercase">
              Background
            </span>
            <button
              type="button"
              onClick={() => setOpen(false)}
              className="text-cc-ink-dim hover:text-cc-ink text-xs"
              aria-label="Close background controls"
            >
              ✕
            </button>
          </div>
          <div className="mt-3 flex flex-col gap-3">
            {rows.map(([key, label, min, max, step]) => (
              <label key={key} className="flex flex-col gap-1">
                <span className="flex justify-between text-[11px]">
                  <span>{label}</span>
                  <span className="text-cc-ink-dim tabular-nums">
                    {vals[key].toFixed(2)}
                  </span>
                </span>
                <input
                  type="range"
                  min={min}
                  max={max}
                  step={step}
                  value={vals[key]}
                  onChange={(e) => update(key, Number(e.target.value))}
                  className="accent-cc-accent"
                />
              </label>
            ))}
          </div>
          <button
            type="button"
            onClick={reset}
            className="border-cc-card-border hover:border-cc-card-border-hover mt-3 w-full rounded-lg border py-1 text-[11px]"
          >
            Reset
          </button>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setOpen(true)}
          className="border-cc-card-border bg-cc-card-bg/95 hover:border-cc-card-border-hover rounded-full border px-3 py-1.5 text-[10px] tracking-wider uppercase backdrop-blur"
        >
          Background
        </button>
      )}
    </div>
  );
}

export function ClientPage() {
  const paramsRef = useRef<BoardParams>({ ...DEFAULT_PARAMS });
  return (
    <div className="relative bg-[#0a0e18]">
      <BoardBackground paramsRef={paramsRef} />
      <BackgroundControls paramsRef={paramsRef} />

      {/* Hero / why */}
      <section className="relative flex min-h-svh items-center overflow-hidden">
        <NodeChip label="Ordering" className="top-[26%] left-[58%]" />
        <NodeChip label="Billing" className="top-[20%] right-[10%]" />
        <NodeChip
          label="Payments"
          className="top-[62%] left-[62%] max-md:hidden"
        />
        <NodeChip label="Shipping" className="right-[6%] bottom-[24%]" />
        <div className="relative z-10 mx-auto grid w-full max-w-6xl items-center gap-10 px-5 py-24 sm:px-12 lg:grid-cols-2">
          <div>
            <Eyebrow>Why Mocha</Eyebrow>
            <h1 className="font-heading text-h3 sm:text-h2 mt-5 leading-[1.15] font-semibold text-balance">
              <span className="text-cc-ink-dim block">
                A request returns in milliseconds.
              </span>
              <span className="text-cc-heading block">
                The work it sets off runs for days.
              </span>
            </h1>
            <p className="text-cc-ink mt-6 max-w-md text-base text-pretty sm:text-lg">
              Without a messaging layer, that work stops the moment the request
              returns. Events scatter across services, and no one can follow
              where they went. Mocha carries them: commands, events, handlers,
              and sagas, with every hop traced in Nitro.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
            </div>
          </div>
          <div aria-hidden="true" />
        </div>
      </section>

      {/* One model, three ranges */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <NodeChip
          label="Reviews"
          className="top-[6%] right-[4%] max-md:hidden"
        />
        <NodeChip
          label="Inventory"
          className="bottom-[8%] left-[3%] max-md:hidden"
        />
        <div className="relative z-10 max-w-2xl">
          <Eyebrow>One model</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
            The same message model, three ranges.
          </h2>
          <p className="text-cc-ink mt-4 text-base sm:text-lg">
            A method call, a hop between services, and a process that runs for
            days. One handler-first model spans all three.
          </p>
        </div>
        <div className="relative z-10 mt-12 grid gap-5 md:grid-cols-3">
          {RANGES.map((r) => (
            <Card key={r.name}>
              <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.2em] uppercase">
                {r.scope}
              </p>
              <p className="text-cc-heading font-heading text-h5 mt-3 font-semibold">
                {r.name}
              </p>
              <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
                {r.copy}
              </p>
            </Card>
          ))}
        </div>
      </section>

      {/* Mediator */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <NodeChip
          label="Catalog"
          className="top-[12%] right-[6%] max-md:hidden"
        />
        <div className="relative z-10 grid items-center gap-10 lg:grid-cols-2">
          <div className="order-2 lg:order-1">
            <MotifStage kind="mediator" />
          </div>
          <div className="order-1 lg:order-2">
            <Eyebrow>Mediator</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
              A command dispatches through the in-process mediator.
            </h2>
            <p className="text-cc-ink mt-4 max-w-md text-base sm:text-lg">
              ISender.Send(CreateReview) lands on a [Handler] method. A source
              generator discovers it at compile time and emits typed
              registration plus a pre-compiled pipeline. Dispatch is a direct
              call, zero-reflection and AOT-friendly, not a reflective lookup on
              the hot path.
            </p>
          </div>
        </div>
      </section>

      {/* Bus */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <NodeChip
          label="Notifications"
          className="right-[5%] bottom-[10%] max-md:hidden"
        />
        <div className="relative z-10 grid items-center gap-10 lg:grid-cols-2">
          <div>
            <Eyebrow>Bus</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
              The same model crosses service boundaries.
            </h2>
            <p className="text-cc-ink mt-4 max-w-md text-base sm:text-lg">
              The handler publishes ReviewCreated. The shape that powers an
              in-process notification powers a cross-service consumer without
              changes. PublishAsync and SendAsync read the same whether the
              message stays in-process or rides a transport to another service.
            </p>
          </div>
          <MotifStage kind="pubsub" />
        </div>
      </section>

      {/* Patterns */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <NodeChip
          label="Identity"
          className="top-[8%] left-[4%] max-md:hidden"
        />
        <div className="relative z-10 max-w-2xl">
          <Eyebrow>Patterns</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
            Three patterns, one handler-first model.
          </h2>
          <p className="text-cc-ink mt-4 text-base sm:text-lg">
            The same API covers pub/sub, fire-and-forget, and request/reply. The
            catalog is the Enterprise Integration Patterns, not a bespoke
            vocabulary.
          </p>
        </div>
        <div className="relative z-10 mt-12 grid gap-5 md:grid-cols-3">
          {PATTERNS.map((p) => (
            <Card key={p.name}>
              <p className="text-cc-heading font-heading text-[1rem] font-semibold">
                {p.name}
              </p>
              <p className="text-cc-ink-dim mt-1 text-sm">{p.question}</p>
              <span className="border-cc-accent/60 text-cc-accent bg-cc-surface mt-4 inline-block rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem]">
                {p.method}
              </span>
            </Card>
          ))}
        </div>
      </section>

      {/* Reliability */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <NodeChip
          label="Search"
          className="top-[10%] right-[5%] max-md:hidden"
        />
        <div className="relative z-10 grid items-center gap-10 lg:grid-cols-2">
          <div>
            <Eyebrow>Reliability</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
              At-least-once delivery, exactly-once processing.
            </h2>
            <p className="text-cc-ink mt-4 max-w-md text-base sm:text-lg">
              Brokers redeliver; pretending otherwise is the bug. Mocha makes
              the failure modes explicit and handles them for you.
            </p>
          </div>
          <MotifStage kind="outbox" />
        </div>
        <div className="relative z-10 mt-6 grid gap-5 md:grid-cols-2">
          <Card>
            <p className="text-cc-heading font-heading text-h5 font-semibold">
              Transactional outbox
            </p>
            <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
              The Postgres domain write and the ReviewCreated message commit
              together, so a crash never loses a message.
            </p>
          </Card>
          <Card>
            <p className="text-cc-heading font-heading text-h5 font-semibold">
              Idempotent inbox
            </p>
            <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
              Deduped by message id, so the handler runs once even when the
              broker hands you the same message twice.
            </p>
          </Card>
        </div>
        <p className="text-cc-ink relative z-10 mt-6 max-w-2xl text-[0.95rem] leading-relaxed">
          That is exactly-once{" "}
          <span className="text-cc-accent font-medium">processing</span>, not
          exactly-once delivery, with retry, dead-letter routing, and circuit
          breaker as pipeline middleware.
        </p>
      </section>

      {/* Sagas */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <NodeChip
          label="Warehouse"
          className="bottom-[8%] left-[4%] max-md:hidden"
        />
        <div className="relative z-10 grid items-center gap-10 lg:grid-cols-2">
          <div className="order-2 lg:order-1">
            <MotifStage kind="saga" />
          </div>
          <div className="order-1 lg:order-2">
            <Eyebrow>Saga</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
              A saga carries the workflow across services.
            </h2>
            <p className="text-cc-ink mt-4 max-w-md text-base sm:text-lg">
              The review saga is a C# state machine: Draft to Checked to
              Published. At startup Mocha validates the shape, that every state
              is reachable and every path reaches a final state, so a workflow
              cannot silently get stuck on the way to production.
            </p>
          </div>
        </div>
      </section>

      {/* Observability */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <NodeChip
          label="Audit"
          className="top-[10%] right-[5%] max-md:hidden"
        />
        <div className="relative z-10 grid items-center gap-10 lg:grid-cols-2">
          <div className="order-2 lg:order-1">
            <MotifStage kind="trace" />
          </div>
          <div className="order-1 lg:order-2">
            <Eyebrow>Observability</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
              Every hop is a span in Nitro.
            </h2>
            <p className="text-cc-ink mt-4 max-w-md text-base sm:text-lg">
              Decoupled work does not have to be a black box. Mocha is
              OpenTelemetry-native: every dispatch, transport hop, and handler
              execution emits spans, and correlation propagates across service
              boundaries. Follow a message from publish to consume as real
              spans.
            </p>
          </div>
        </div>
      </section>

      {/* Transports */}
      <section className="relative mx-auto max-w-6xl px-5 py-28 sm:px-12">
        <div className="relative z-10 grid items-center gap-10 lg:grid-cols-2">
          <div>
            <Eyebrow>Transports</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
              Swap transports without touching handlers.
            </h2>
            <p className="text-cc-ink mt-4 max-w-md text-base sm:text-lg">
              Lead with RabbitMQ, Postgres, and in-process, and swap them
              without changing a handler. Kafka, Azure Service Bus, and Event
              Hub also exist in source.
            </p>
            <div className="mt-6 flex flex-wrap items-center gap-2">
              {TRANSPORTS.map((t) => (
                <span
                  key={t}
                  className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem]"
                >
                  {t}
                </span>
              ))}
              <Link
                href="/docs/mocha"
                className="text-cc-accent hover:text-cc-accent-hover ml-1 text-sm font-medium"
              >
                Learn more about Mocha →
              </Link>
            </div>
          </div>
          <MotifStage kind="transports" />
        </div>
      </section>

      {/* CTA */}
      <section className="relative mx-auto flex max-w-6xl flex-col items-center px-5 py-32 pb-44 text-center sm:px-12">
        <NodeChip label="" className="bottom-[26%] left-1/2 -translate-x-1/2" />
        <div className="relative z-10 mx-auto max-w-2xl">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold text-balance">
            Keep the workflow moving without losing the thread.
          </h2>
          <p className="text-cc-ink mt-5 text-base sm:text-lg">
            Write a handler, attribute it, dispatch it. The bus, the outbox, the
            inbox, the sagas, and the traces are part of the framework, not
            packages you wire yourself.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/platform">See the platform</OutlineButton>
          </div>
        </div>
      </section>
    </div>
  );
}
