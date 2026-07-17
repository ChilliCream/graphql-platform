"use client";

/**
 * Mocha messaging page, v14 hero board. The v13 full-page circuit board,
 * scoped to the hero section and rendered permanently at a faded strength
 * instead of being revealed by a light mask. Six service nodes sit right of
 * the copy, breathe with a cyan halo, and exchange coral message pulses over
 * the copper backbone. Purely decorative; the static board is pre-rendered
 * once to an offscreen canvas and composited each frame.
 */

import { useEffect, useRef } from "react";

import { MONO_FONT, NAVY, TEAL } from "./palette";

const GRID = 28;

const PAD_COLOR = "rgba(150, 166, 194, 0.14)";
const HATCH_COLOR = "rgba(150, 166, 194, 0.13)";
const HATCH_EDGE = "rgba(150, 166, 194, 0.15)";
const FOOT_BODY = "#0b1220";
const FOOT_EDGE = "rgba(158, 176, 204, 0.44)";
const FOOT_PIN = "rgba(158, 176, 204, 0.34)";
const PASSIVE_PAD = "rgba(168, 184, 210, 0.5)";
const PASSIVE_BODY = "rgba(120, 136, 164, 0.32)";
const RES_BODY = "rgba(150, 166, 194, 0.38)";
const RES_LAND = "rgba(196, 210, 232, 0.62)";
const CAN_BODY = "rgba(120, 136, 164, 0.3)";
const CAN_STRIPE = "rgba(196, 210, 232, 0.5)";
const COURT_COLOR = "rgba(150, 168, 198, 0.28)";
const SILK_SCATTER = "rgba(150, 168, 198, 0.42)";
const TRACE_COLOR = "rgba(139, 160, 188, 0.42)";
const TRACE_ALT_COLOR = "rgba(139, 160, 188, 0.26)";
const LANE_COLOR = "rgba(174, 190, 216, 0.5)";
const VIA_COLOR = "rgba(164, 180, 208, 0.55)";
const MSG = "224, 140, 122";
const MSG_SOFT = "246, 202, 190";

const GLOW_RGB = `${parseInt(TEAL.slice(1, 3), 16)}, ${parseInt(TEAL.slice(3, 5), 16)}, ${parseInt(TEAL.slice(5, 7), 16)}`;

const BOARD_ALPHA = 0.6;
const PULSE_TRAIL = 128;

// ---- Board generator tuning ----
// Component placement.
const MAJOR_CELL = 210; // coarse placement grid pitch for major ICs
const MAJOR_PROB = 0.55; // chance a coarse cell hosts a major IC
const PART_MARGIN = 10; // solder-mask margin between placed parts
const PAD_LEN = 3.5; // drawn IC pad protrusion outside the package body
const COURT_GAP = 3; // silkscreen courtyard offset outside a part body
const CAN_R = 4.5; // electrolytic capacitor can radius
const DECAP_MIN = 2; // decoupling passives per major IC (min..max)
const DECAP_MAX = 4;
// Netlist and bus routing.
const NET_NODE_IC_MAX = 520; // a service node taps ICs within this radius
const NET_IC_MIN = 150; // IC-to-IC bus distance band
const NET_IC_MAX = 450;
const NET_EDGE_MAX = 260; // rim ICs closer than this may leave the board
// Pin pitch equals lane pitch, so the order-preserving fanout at a pad row
// degenerates to dead-straight entries: zero transverse delta, no splay.
const BUS_LANE_PITCH = 5; // spacing between parallel bus lanes AND IC pad pitch
const BUS_CLEAR = 3; // extra clearance either side of a bus
const BUS_STUB_MIN = 10; // perpendicular exit stub length range
const BUS_STUB_MAX = 20;
const BUS_END_SKIP = 22; // px at bus ends exempt from clearance (fanout zone)
// Fills.
const STITCH_PITCH = 12; // via stitching row pitch
const AMBIENT_DIV = 60000; // ambient walk count = area / this (~15% of v14)
const POUR_MIN = 3; // ground pour region count bounds
const POUR_MAX = 6;
const POUR_MAX_SPAN = 9; // pour max side length in GRID cells

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

/** A lead pad on a package edge, with its outward normal and whether copper
 * actually lands on it (a lane endpoint) or it escapes/stays NC. */
interface Pad {
  readonly x: number;
  readonly y: number;
  readonly nx: number;
  readonly ny: number;
  connected: boolean;
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

type PassiveKind = "res" | "cap";

/** A small two-pad passive footprint: a chip resistor (bright end lands on a
 * lighter body) or a ceramic capacitor (squarer body between two lands). */
interface Passive {
  readonly x: number;
  readonly y: number;
  readonly horiz: boolean;
  readonly kind: PassiveKind;
  /** Part of a passive bank sharing one designator. */
  readonly bank?: boolean;
}

/** An electrolytic capacitor can: round body, silk ring, polarity stripe. */
interface Can {
  readonly x: number;
  readonly y: number;
  /** Angle the polarity crescent faces. */
  readonly ang: number;
}

/** A thin silkscreen body outline printed around a part; round for cans. */
interface Courtyard {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly round?: boolean;
}

/** One end of a routed bus: a component pad row, or a bare point with an
 * explicit exit direction (a service node or a board-edge exit). */
interface BusEnd {
  readonly foot?: Footprint;
  readonly pt: Point;
  readonly dir?: Point;
}

interface Silk {
  readonly x: number;
  readonly y: number;
  readonly text: string;
  /** Rotated 90 degrees, printed along a vertical board edge. */
  readonly vert?: boolean;
}

interface Board {
  readonly traces: Trace[];
  readonly connectors: Trace[];
  readonly outgoing: Trace[][];
  readonly nodes: Point[];
  readonly vias: Via[];
  readonly hatches: Rect[];
  readonly footprints: Footprint[];
  readonly passives: Passive[];
  readonly cans: Can[];
  readonly testpoints: Point[];
  readonly courtyards: Courtyard[];
  readonly silks: Silk[];
  readonly fiducials: Point[];
  readonly holes: Point[];
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
  to: number;
}

/** An arrival flash ring at a trace endpoint that is not a service node. */
interface RingFlash {
  x: number;
  y: number;
  life: number;
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

/** Lead pads on the two LONG edges of a package (top/bottom for a landscape
 * body, left/right for a portrait one), at exactly BUS_LANE_PITCH so a bus
 * meets a pad row dead straight with zero transverse fanout. */
function footPads(foot: Footprint): Pad[] {
  const pads: Pad[] = [];
  if (foot.w >= foot.h) {
    for (let px = foot.x + 4; px <= foot.x + foot.w - 4; px += BUS_LANE_PITCH) {
      pads.push({ x: px, y: foot.y, nx: 0, ny: -1, connected: false });
      pads.push({ x: px, y: foot.y + foot.h, nx: 0, ny: 1, connected: false });
    }
  } else {
    for (let py = foot.y + 4; py <= foot.y + foot.h - 4; py += BUS_LANE_PITCH) {
      pads.push({ x: foot.x, y: py, nx: -1, ny: 0, connected: false });
      pads.push({ x: foot.x + foot.w, y: py, nx: 1, ny: 0, connected: false });
    }
  }
  return pads;
}

/**
 * Short escape traces off the unconnected pads: most pins escape perpendicular
 * to a via, some stay NC, so the package reads as soldered into the board.
 * `keep` vets the escape tip (where the via lands); a tip that would sit on
 * copper or inside other board furniture drops that escape. The PRNG is
 * consumed identically either way, so decisions stay deterministic.
 */
function padEscapes(
  pads: Pad[],
  rand: () => number,
  keep: (x: number, y: number) => boolean,
): Point[][] {
  const escapes: Point[][] = [];
  for (const pad of pads) {
    if (pad.connected || rand() > 0.72) {
      continue;
    }
    const tipX = pad.x + pad.nx * 5;
    const tipY = pad.y + pad.ny * 5;
    const len = 9 + rand() * 9;
    const endX = tipX + pad.nx * len;
    const endY = tipY + pad.ny * len;
    if (!keep(endX, endY)) {
      continue;
    }
    escapes.push([
      { x: tipX, y: tipY },
      { x: endX, y: endY },
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

/** Drop coincident points and merge collinear runs so offsetting stays exact. */
function dedupPath(pts: Point[]): Point[] {
  const out: Point[] = [];
  for (const p of pts) {
    const last = out[out.length - 1];
    if (
      last &&
      Math.abs(last.x - p.x) < 0.26 &&
      Math.abs(last.y - p.y) < 0.26
    ) {
      continue;
    }
    out.push(p);
  }
  for (let i = out.length - 2; i >= 1; i--) {
    const a = out[i - 1];
    const b = out[i];
    const c = out[i + 1];
    const cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
    const dot = (b.x - a.x) * (c.x - b.x) + (b.y - a.y) * (c.y - b.y);
    if (Math.abs(cross) < 0.01 && dot > 0) {
      out.splice(i, 1);
    }
  }
  return out;
}

/** One segment of a polyline, pre-shifted along its left normal. */
interface OffsetSeg {
  readonly ax: number;
  readonly ay: number;
  readonly bx: number;
  readonly by: number;
  readonly dx: number;
  readonly dy: number;
}

/**
 * Exact parallel offset of an open polyline: EVERY segment is shifted along
 * its left normal by `o`, then consecutive offset segments are rejoined at
 * the intersection of their carrier lines (a true miter), so the offset
 * curve keeps a constant perpendicular distance through every bend. All
 * bends here are 45 or 90 degrees, so the intersections are
 * well-conditioned; collinear neighbours skip the join and keep the shared
 * offset endpoint.
 */
function offsetPolyline(pts: Point[], o: number): Point[] {
  if (pts.length < 2) {
    return pts.map((p) => ({ x: p.x, y: p.y }));
  }
  const segs: OffsetSeg[] = [];
  for (let i = 0; i < pts.length - 1; i++) {
    const dx = pts[i + 1].x - pts[i].x;
    const dy = pts[i + 1].y - pts[i].y;
    const l = Math.hypot(dx, dy) || 1;
    const ux = dx / l;
    const uy = dy / l;
    segs.push({
      ax: pts[i].x - uy * o,
      ay: pts[i].y + ux * o,
      bx: pts[i + 1].x - uy * o,
      by: pts[i + 1].y + ux * o,
      dx: ux,
      dy: uy,
    });
  }
  const out: Point[] = [{ x: segs[0].ax, y: segs[0].ay }];
  for (let i = 1; i < segs.length; i++) {
    const p = segs[i - 1];
    const q = segs[i];
    const cross = p.dx * q.dy - p.dy * q.dx;
    if (Math.abs(cross) < 1e-6) {
      out.push({ x: p.bx, y: p.by });
      continue;
    }
    const t = ((q.ax - p.ax) * q.dy - (q.ay - p.ay) * q.dx) / cross;
    out.push({ x: p.ax + p.dx * t, y: p.ay + p.dy * t });
  }
  out.push({ x: segs[segs.length - 1].bx, y: segs[segs.length - 1].by });
  return out;
}

/**
 * Bus centerline: perpendicular stubs off both pad rows, then a straight /
 * one 45-degree diagonal / straight run between the stub ends. `midT` slides
 * the diagonal along the run so parallel buses do not echo one another.
 */
function busPath(
  a: Point,
  adir: Point,
  aStub: number,
  b: Point,
  bdir: Point,
  bStub: number,
  midT: number,
): Point[] {
  const a2 = { x: a.x + adir.x * aStub, y: a.y + adir.y * aStub };
  const b2 = { x: b.x + bdir.x * bStub, y: b.y + bdir.y * bStub };
  const dx = b2.x - a2.x;
  const dy = b2.y - a2.y;
  const adx = Math.abs(dx);
  const ady = Math.abs(dy);
  const d = Math.min(adx, ady);
  const pts: Point[] = [a, a2];
  if (adx >= ady) {
    const before = (adx - d) * midT;
    const p1 = { x: a2.x + Math.sign(dx) * before, y: a2.y };
    pts.push(p1, { x: p1.x + Math.sign(dx) * d, y: b2.y });
  } else {
    const before = (ady - d) * midT;
    const p1 = { x: a2.x, y: a2.y + Math.sign(dy) * before };
    pts.push(p1, { x: b2.x, y: p1.y + Math.sign(dy) * d });
  }
  pts.push(b2, b);
  return dedupPath(pts);
}

/** A run of `n` consecutive unconnected pads on one package edge, chosen
 * nearest the target's projection along that edge (the fanout window). */
function padWindow(
  foot: Footprint,
  nx: number,
  ny: number,
  n: number,
  toward: number,
): Pad[] | null {
  const horiz = ny !== 0;
  const row = (foot.pads ?? [])
    .filter((p) => p.nx === nx && p.ny === ny)
    .sort((p, q) => (horiz ? p.x - q.x : p.y - q.y));
  let best: Pad[] | null = null;
  let bestScore = Infinity;
  for (let i = 0; i + n <= row.length; i++) {
    const win = row.slice(i, i + n);
    if (win.some((p) => p.connected)) {
      continue;
    }
    const c0 = horiz ? win[0].x : win[0].y;
    const c1 = horiz ? win[n - 1].x : win[n - 1].y;
    const score = Math.abs((c0 + c1) / 2 - toward);
    if (score < bestScore) {
      bestScore = score;
      best = win;
    }
  }
  return best;
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

function generateBoard(nodes: Point[], w: number, h: number): Board {
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

  // Single-layer routing discipline: a build-time occupancy grid so no two
  // top-layer traces cross. Backbone lanes are laid first, in order, each one
  // vetted against copper already placed and dropped when its run is
  // occupied. Random filler that would cross top copper is demoted to a real
  // inner layer (via-terminated, drawn dim) or dropped. Discs around every
  // node are exempt so the radial fans do not reject one another.
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
  // Furniture occupancy: package bodies, banks, fiducials and mounting holes
  // are stamped into their own grid (single cell, no inflation) so bus routing
  // can refuse to run under parts without a part rejecting its own fanout.
  const occFurn = new Uint8Array(gcols * grows);
  const stampRect = (r: Rect, grid: Uint8Array) => {
    const x0 = Math.max(0, Math.floor(r.x / CELL));
    const y0 = Math.max(0, Math.floor(r.y / CELL));
    const x1 = Math.min(gcols - 1, Math.floor((r.x + r.w) / CELL));
    const y1 = Math.min(grows - 1, Math.floor((r.y + r.h) / CELL));
    for (let gy = y0; gy <= y1; gy++) {
      for (let gx = x0; gx <= x1; gx++) {
        grid[gy * gcols + gx] = 1;
      }
    }
  };
  // Bus clearance test: the centerline and its two extreme lane offsets are
  // sampled against top copper and furniture, skipping BUS_END_SKIP px at
  // both ends where the fanout legitimately hugs its own package.
  const busBlocked = (pts: Point[], half: number): boolean => {
    for (const o of [0, -half, half]) {
      const line = o === 0 ? pts : offsetPolyline(pts, o);
      let total = 0;
      for (let i = 1; i < line.length; i++) {
        total += Math.hypot(
          line[i].x - line[i - 1].x,
          line[i].y - line[i - 1].y,
        );
      }
      let acc = 0;
      for (let i = 1; i < line.length; i++) {
        const a = line[i - 1];
        const b = line[i];
        const segLen = Math.hypot(b.x - a.x, b.y - a.y);
        const steps = Math.max(1, Math.ceil(segLen / STEP));
        for (let s = 0; s <= steps; s++) {
          const t = s / steps;
          const dist = acc + segLen * t;
          if (dist < BUS_END_SKIP || dist > total - BUS_END_SKIP) {
            continue;
          }
          const x = a.x + (b.x - a.x) * t;
          const y = a.y + (b.y - a.y) * t;
          if (x < 2 || y < 2 || x > w - 2 || y > h - 2) {
            return true;
          }
          if (exemptAt(x, y)) {
            continue;
          }
          const gx = Math.min(gcols - 1, Math.max(0, Math.floor(x / CELL)));
          const gy = Math.min(grows - 1, Math.max(0, Math.floor(y / CELL)));
          if (occTop[gy * gcols + gx] || occFurn[gy * gcols + gx]) {
            return true;
          }
        }
        acc += segLen;
      }
    }
    return false;
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

  // Node backbone: each node connects to its nearest neighbours. Lanes are
  // laid in order and each candidate is checked against copper already on the
  // top layer, so no two backbone lanes cross; a blocked link is skipped (a
  // node keeping fewer links is realistic).
  for (let i = 0; i < nodes.length; i++) {
    const others = nodes
      .map((n, j) => ({ j, d: Math.hypot(n.x - nodes[i].x, n.y - nodes[i].y) }))
      .filter((o) => o.j > i && o.d < 620)
      .sort((a, b) => a.d - b.d)
      .slice(0, 2);
    for (const o of others) {
      const laneT = connector(nodes[i], nodes[o.j], o.j);
      if (blocked(laneT.pts, occTop)) {
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

  // Keep-outs so board furniture never lands on a service node.
  const keepOuts: Rect[] = [];
  for (const n of nodes) {
    keepOuts.push({ x: n.x - 52, y: n.y - 44, w: 104, h: 96 });
  }

  // Furniture placement discipline: every placed rect (part body, silk text
  // extent, pour, fiducial, hole) is recorded here. A candidate must stay on
  // the board, clear the node keep-outs, clear all earlier furniture by a
  // solder-mask margin, and sample copper-free on both layer grids.
  const furniture: Rect[] = [];
  const overlaps = (a: Rect, b: Rect, m: number) =>
    a.x < b.x + b.w + m &&
    a.x + a.w + m > b.x &&
    a.y < b.y + b.h + m &&
    a.y + a.h + m > b.y;
  const copperAt = (x: number, y: number) => {
    const gx = Math.min(gcols - 1, Math.max(0, Math.floor(x / CELL)));
    const gy = Math.min(grows - 1, Math.max(0, Math.floor(y / CELL)));
    return occTop[gy * gcols + gx] === 1 || occInner[gy * gcols + gx] === 1;
  };
  const rectOnCopper = (r: Rect) => {
    const xs = Math.max(1, Math.ceil(r.w / (CELL / 2)));
    const ys = Math.max(1, Math.ceil(r.h / (CELL / 2)));
    for (let iy = 0; iy <= ys; iy++) {
      for (let ix = 0; ix <= xs; ix++) {
        if (copperAt(r.x + (r.w * ix) / xs, r.y + (r.h * iy) / ys)) {
          return true;
        }
      }
    }
    return false;
  };
  const rejected = (r: Rect) =>
    r.x < 2 ||
    r.y < 2 ||
    r.x + r.w > w - 2 ||
    r.y + r.h > h - 2 ||
    keepOuts.some((k) => overlaps(r, k, 0)) ||
    furniture.some((f) => overlaps(r, f, PART_MARGIN)) ||
    rectOnCopper(r);

  // Courtyard geometry: every part's furniture rect IS its silkscreen
  // courtyard (COURT_GAP outside the body, widened to clear the pin rows on
  // pad edges), so copper clearance and part-vs-part collision tests all
  // respect the courtyard and nothing may cross it.
  const icCourt = (r: Rect): Rect =>
    r.w >= r.h
      ? {
          x: r.x - COURT_GAP,
          y: r.y - PAD_LEN - 2,
          w: r.w + COURT_GAP * 2,
          h: r.h + (PAD_LEN + 2) * 2,
        }
      : {
          x: r.x - PAD_LEN - 2,
          y: r.y - COURT_GAP,
          w: r.w + (PAD_LEN + 2) * 2,
          h: r.h + COURT_GAP * 2,
        };
  // Passive half-extents: along the part axis (body plus end lands), across.
  const passiveHalf = (kind: PassiveKind) =>
    kind === "res" ? { l: 5.5, c: 2 } : { l: 5.6, c: 2.8 };
  const passiveCourt = (p: Passive): Rect => {
    const { l, c } = passiveHalf(p.kind);
    const hw = (p.horiz ? l : c) + COURT_GAP;
    const hh = (p.horiz ? c : l) + COURT_GAP;
    return { x: p.x - hw, y: p.y - hh, w: hw * 2, h: hh * 2 };
  };
  const canCourt = (x: number, y: number): Rect => ({
    x: x - CAN_R - COURT_GAP,
    y: y - CAN_R - COURT_GAP,
    w: (CAN_R + COURT_GAP) * 2,
    h: (CAN_R + COURT_GAP) * 2,
  });

  // ---- Stage 1: component placement on a jittered coarse grid. ----
  // Each coarse cell hosts one major IC with probability MAJOR_PROB, jittered
  // inside the cell and snapped to GRID; grid sampling makes overlap nearly
  // impossible but every body is still vetted against the placed-rect list.
  const majors: Footprint[] = [];
  const footprints: Footprint[] = [];
  const footCourts: Rect[] = [];
  const passives: Passive[] = [];
  const cans: Can[] = [];
  const testpoints: Point[] = [];
  const courtyards: Courtyard[] = [];
  const silks: Silk[] = [];
  const leftovers: Rect[] = [];
  const mcols = Math.max(1, Math.round(w / MAJOR_CELL));
  const mrows = Math.max(1, Math.round(h / MAJOR_CELL));
  const cw = w / mcols;
  const ch = h / mrows;
  for (let cy = 0; cy < mrows; cy++) {
    for (let cx = 0; cx < mcols; cx++) {
      const cell: Rect = { x: cx * cw, y: cy * ch, w: cw, h: ch };
      if (rand() >= MAJOR_PROB) {
        leftovers.push(cell);
        continue;
      }
      let bw = GRID * (2 + Math.floor(rand() * 3));
      let bh = GRID * (1.5 + 0.5 * Math.floor(rand() * 4));
      if (rand() < 0.4) {
        const t = bw;
        bw = bh;
        bh = t;
      }
      let done = false;
      for (let attempt = 0; attempt < 6 && !done; attempt++) {
        const fx =
          GRID *
          Math.round((cell.x + rand() * Math.max(1, cell.w - bw)) / GRID);
        const fy =
          GRID *
          Math.round((cell.y + rand() * Math.max(1, cell.h - bh)) / GRID);
        const r: Rect = { x: fx, y: fy, w: bw, h: bh };
        const court = icCourt(r);
        if (rejected(court)) {
          continue;
        }
        const foot: Footprint = { x: fx, y: fy, w: bw, h: bh };
        foot.pads = footPads(foot);
        majors.push(foot);
        footprints.push(foot);
        footCourts.push(court);
        courtyards.push(court);
        furniture.push(court);
        stampRect(court, occFurn);
        done = true;
      }
      if (!done) {
        leftovers.push(cell);
      }
    }
  }

  // A few medium ICs in leftover cells.
  const mediumCount = Math.min(leftovers.length, 2 + Math.floor(rand() * 3));
  for (let i = 0; i < mediumCount; i++) {
    const cell = leftovers[Math.floor(rand() * leftovers.length)];
    for (let attempt = 0; attempt < 6; attempt++) {
      const bw = GRID * (1 + Math.floor(rand() * 2));
      const bh = GRID;
      const fx =
        GRID * Math.round((cell.x + rand() * Math.max(1, cell.w - bw)) / GRID);
      const fy =
        GRID * Math.round((cell.y + rand() * Math.max(1, cell.h - bh)) / GRID);
      const r: Rect = { x: fx, y: fy, w: bw, h: bh };
      const court = icCourt(r);
      if (rejected(court)) {
        continue;
      }
      const foot: Footprint = { x: fx, y: fy, w: bw, h: bh };
      foot.pads = footPads(foot);
      footprints.push(foot);
      footCourts.push(court);
      courtyards.push(court);
      furniture.push(court);
      stampRect(court, occFurn);
      break;
    }
  }

  // Passive banks: short rows of identical resistors or capacitors sharing
  // one designator and one courtyard around the whole row.
  const banks: { court: Rect; kind: PassiveKind }[] = [];
  const bankCount = 3 + Math.floor(rand() * 3);
  for (let i = 0; i < bankCount && leftovers.length > 0; i++) {
    const cell = leftovers[Math.floor(rand() * leftovers.length)];
    for (let attempt = 0; attempt < 8; attempt++) {
      const n = 2 + Math.floor(rand() * 3);
      const horiz = rand() < 0.5;
      const kind: PassiveKind = rand() < 0.5 ? "res" : "cap";
      const bx = GRID * Math.round((cell.x + rand() * cell.w) / GRID);
      const by = GRID * Math.round((cell.y + rand() * cell.h) / GRID);
      const { l, c } = passiveHalf(kind);
      const court: Rect = horiz
        ? {
            x: bx - c - COURT_GAP,
            y: by - l - COURT_GAP,
            w: (n - 1) * 10 + (c + COURT_GAP) * 2,
            h: (l + COURT_GAP) * 2,
          }
        : {
            x: bx - l - COURT_GAP,
            y: by - c - COURT_GAP,
            w: (l + COURT_GAP) * 2,
            h: (n - 1) * 10 + (c + COURT_GAP) * 2,
          };
      if (rejected(court)) {
        continue;
      }
      for (let k = 0; k < n; k++) {
        passives.push({
          x: bx + (horiz ? k * 10 : 0),
          y: by + (horiz ? 0 : k * 10),
          horiz: !horiz,
          kind,
          bank: true,
        });
      }
      courtyards.push(court);
      furniture.push(court);
      stampRect(court, occFurn);
      banks.push({ court, kind });
      break;
    }
  }

  // Board furniture: fiducials in three or four corners, a few mounting
  // holes along the rim, and an edge legend.
  const fiducials: Point[] = [];
  const corners: Point[] = [
    { x: 24 + rand() * 20, y: 24 + rand() * 20 },
    { x: w - 24 - rand() * 20, y: 24 + rand() * 20 },
    { x: 24 + rand() * 20, y: h - 24 - rand() * 20 },
    { x: w - 24 - rand() * 20, y: h - 24 - rand() * 20 },
  ];
  const skipCorner = rand() < 0.5 ? Math.floor(rand() * 4) : -1;
  for (let i = 0; i < corners.length; i++) {
    if (i === skipCorner) {
      continue;
    }
    const c = corners[i];
    const r: Rect = { x: c.x - 6, y: c.y - 6, w: 12, h: 12 };
    if (!rejected(r)) {
      fiducials.push(c);
      furniture.push(r);
      stampRect(r, occFurn);
    }
  }
  const holes: Point[] = [];
  const holeCount = 2 + Math.floor(rand() * 2);
  for (let i = 0; i < holeCount; i++) {
    for (let attempt = 0; attempt < 8; attempt++) {
      const onVert = rand() < 0.5;
      const c = onVert
        ? { x: rand() < 0.5 ? 18 : w - 18, y: 40 + rand() * (h - 80) }
        : { x: 40 + rand() * (w - 80), y: rand() < 0.5 ? 18 : h - 18 };
      const r: Rect = { x: c.x - 10, y: c.y - 10, w: 20, h: 20 };
      if (!rejected(r)) {
        holes.push(c);
        furniture.push(r);
        stampRect(r, occFurn);
        break;
      }
    }
  }
  const edgeTexts = ["MOCHA · REV 2.1", "HC-MSG-11 · 6L"];
  const edgeCount = 1 + Math.floor(rand() * 2);
  for (let i = 0; i < edgeCount; i++) {
    const text = edgeTexts[i % edgeTexts.length];
    const tw = text.length * 4.5;
    for (let attempt = 0; attempt < 6; attempt++) {
      if (i === 0) {
        const x = 40 + rand() * Math.max(1, w - tw - 80);
        const r: Rect = { x, y: h - 14, w: tw, h: 10 };
        if (!rejected(r)) {
          silks.push({ x, y: h - 6, text });
          furniture.push(r);
          break;
        }
      } else {
        const y = 40 + rand() * Math.max(1, h - tw - 80);
        const r: Rect = { x: w - 13, y, w: 11, h: tw };
        if (!rejected(r)) {
          silks.push({ x: w - 11, y, text, vert: true });
          furniture.push(r);
          break;
        }
      }
    }
  }

  // ---- Stage 2 + 3: netlist-driven bus routing. ----
  // Every routed bus goes somewhere deliberate: a netlist edge is realized as
  // 2-6 parallel lanes fanning off consecutive pads, with a retry ladder of
  // alternate diagonal positions, then fewer lanes, then a skip.
  const footCenter = (f: Footprint): Point => ({
    x: f.x + f.w / 2,
    y: f.y + f.h / 2,
  });
  const sideToward = (foot: Footprint, target: Point) => {
    const c = footCenter(foot);
    return foot.w >= foot.h
      ? { nx: 0, ny: target.y < c.y ? -1 : 1 }
      : { nx: target.x < c.x ? -1 : 1, ny: 0 };
  };
  const routeBus = (
    src: BusEnd,
    dst: BusEnd,
    want: number,
    endVias: boolean,
  ): boolean => {
    const aRef = src.foot ? footCenter(src.foot) : src.pt;
    const bRef = dst.foot ? footCenter(dst.foot) : dst.pt;
    for (let n = want; n >= 2; n--) {
      // Preferred exit faces first, then the opposite long edge as fallback,
      // so one blocked face does not kill the whole connection.
      for (const combo of [0, 1, 2, 3]) {
        const sFlip = combo & 1 ? -1 : 1;
        const dFlip = combo & 2 ? -1 : 1;
        if ((sFlip < 0 && !src.foot) || (dFlip < 0 && !dst.foot)) {
          continue;
        }
        let a = src.pt;
        let adir = src.dir ?? { x: 1, y: 0 };
        let srcPads: Pad[] | null = null;
        if (src.foot) {
          const s0 = sideToward(src.foot, bRef);
          const s = { nx: s0.nx * sFlip, ny: s0.ny * sFlip };
          srcPads = padWindow(
            src.foot,
            s.nx,
            s.ny,
            n,
            s.ny !== 0 ? bRef.x : bRef.y,
          );
          if (!srcPads) {
            continue;
          }
          a = {
            x: (srcPads[0].x + srcPads[n - 1].x) / 2,
            y: (srcPads[0].y + srcPads[n - 1].y) / 2,
          };
          adir = { x: s.nx, y: s.ny };
        }
        let b = dst.pt;
        let bdir = dst.dir ?? { x: 1, y: 0 };
        let dstPads: Pad[] | null = null;
        if (dst.foot) {
          const s0 = sideToward(dst.foot, aRef);
          const s = { nx: s0.nx * dFlip, ny: s0.ny * dFlip };
          dstPads = padWindow(
            dst.foot,
            s.nx,
            s.ny,
            n,
            s.ny !== 0 ? aRef.x : aRef.y,
          );
          if (!dstPads) {
            continue;
          }
          b = {
            x: (dstPads[0].x + dstPads[n - 1].x) / 2,
            y: (dstPads[0].y + dstPads[n - 1].y) / 2,
          };
          bdir = { x: s.nx, y: s.ny };
        }
        const halfLane = ((n - 1) / 2) * BUS_LANE_PITCH;
        const half = halfLane + BUS_CLEAR;
        // A pad-row end keeps its first bend clear of the fanout zone: the
        // shared parallel run starts BUS_END_SKIP out from the row, and an
        // inner lane's miter can pull a corner back toward the row by up to
        // halfLane, so the perpendicular stub covers both.
        const aStub = src.foot
          ? BUS_END_SKIP + halfLane + 2 + rand() * 6
          : BUS_STUB_MIN + rand() * (BUS_STUB_MAX - BUS_STUB_MIN);
        const bStub = dst.foot
          ? BUS_END_SKIP + halfLane + 2 + rand() * 6
          : BUS_STUB_MIN + rand() * (BUS_STUB_MAX - BUS_STUB_MIN);
        const midTs = [0.32 + rand() * 0.36, 0.15, 0.85, 0.55];
        for (const midT of midTs) {
          const center = busPath(a, adir, aStub, b, bdir, bStub, midT);
          if (center.length < 2 || busBlocked(center, half)) {
            continue;
          }
          const laneLines: Point[][] = [];
          for (let k = 0; k < n; k++) {
            laneLines.push(
              offsetPolyline(center, (k - (n - 1) / 2) * BUS_LANE_PITCH),
            );
          }
          // Order-preserving fanout at each pad row: the shared parallel run
          // stops at a boundary BUS_END_SKIP out from the row. Lanes (by
          // their transverse coordinate at that boundary) and pads (by their
          // coordinate along the row) are both sorted on the same world axis
          // and paired in order, which makes crossings geometrically
          // impossible; each pairing is realized as a straight perpendicular
          // run off the pad plus one 45-degree jog onto the lane.
          const fanout = (pads: Pad[] | null, dir: Point, atStart: boolean) => {
            if (!pads) {
              return;
            }
            const alongX = dir.y !== 0;
            const order = laneLines
              .map((line, k) => {
                const p = atStart ? line[0] : line[line.length - 1];
                return { k, c: alongX ? p.x : p.y };
              })
              .sort((p, q) => p.c - q.c);
            const sorted = [...pads].sort((p, q) =>
              alongX ? p.x - q.x : p.y - q.y,
            );
            for (let r = 0; r < order.length; r++) {
              const line = laneLines[order[r].k];
              const pad = sorted[r];
              pad.connected = true;
              // The lane's straight stub crosses the row at its first/last
              // point; the fanout boundary sits BUS_END_SKIP along the stub.
              const p0 = atStart ? line[0] : line[line.length - 1];
              const bx = p0.x + dir.x * BUS_END_SKIP;
              const by = p0.y + dir.y * BUS_END_SKIP;
              const jog = Math.abs(alongX ? pad.x - bx : pad.y - by);
              const run = BUS_END_SKIP - jog;
              const fan: Point[] = [{ x: pad.x, y: pad.y }];
              if (jog > 0.01 && run > 0.01) {
                fan.push({ x: pad.x + dir.x * run, y: pad.y + dir.y * run });
              }
              fan.push({ x: bx, y: by });
              if (atStart) {
                line.splice(0, 1, ...fan);
              } else {
                fan.reverse();
                line.splice(line.length - 1, 1, ...fan);
              }
            }
          };
          fanout(srcPads, adir, true);
          fanout(dstPads, bdir, false);
          for (const line of laneLines) {
            const t = traceFrom(dedupPath(line), -1, false);
            traces.push(t);
            stamp(t.pts, occTop);
            if (endVias) {
              pushVia(t);
            }
          }
          return true;
        }
      }
    }
    return false;
  };

  // Service nodes tap into their one or two nearest major ICs.
  for (const node of nodes) {
    const near = majors
      .map((f) => {
        const c = footCenter(f);
        return { f, d: Math.hypot(c.x - node.x, c.y - node.y) };
      })
      .filter((o) => o.d > 70 && o.d < NET_NODE_IC_MAX)
      .sort((p, q) => p.d - q.d)
      .slice(0, 1 + Math.floor(rand() * 2));
    for (const o of near) {
      const c = footCenter(o.f);
      const dx = c.x - node.x;
      const dy = c.y - node.y;
      const dir =
        Math.abs(dx) >= Math.abs(dy)
          ? { x: Math.sign(dx) || 1, y: 0 }
          : { x: 0, y: Math.sign(dy) || 1 };
      routeBus(
        { pt: node, dir },
        { foot: o.f, pt: c },
        2 + Math.floor(rand() * 2),
        false,
      );
    }
  }

  // Each major IC talks to one or two neighbours at bus-friendly distance.
  for (let i = 0; i < majors.length; i++) {
    const c1 = footCenter(majors[i]);
    const near = majors
      .map((f, j) => {
        const c = footCenter(f);
        return { f, j, d: Math.hypot(c.x - c1.x, c.y - c1.y) };
      })
      .filter((o) => o.j > i && o.d >= NET_IC_MIN && o.d <= NET_IC_MAX)
      .sort((p, q) => p.d - q.d)
      .slice(0, 1 + Math.floor(rand() * 2));
    for (const o of near) {
      routeBus(
        { foot: majors[i], pt: c1 },
        { foot: o.f, pt: footCenter(o.f) },
        2 + Math.floor(rand() * 5),
        false,
      );
    }
  }

  // Rim ICs: a few buses leave the board through the nearest edge, ending in
  // a neat via row as if a connector or another board continues them.
  const rims = majors
    .map((f) => {
      const c = footCenter(f);
      const sides: { d: number; pt: Point; dir: Point }[] = [
        { d: c.x, pt: { x: 9, y: c.y }, dir: { x: 1, y: 0 } },
        { d: w - c.x, pt: { x: w - 9, y: c.y }, dir: { x: -1, y: 0 } },
        { d: c.y, pt: { x: c.x, y: 9 }, dir: { x: 0, y: 1 } },
        { d: h - c.y, pt: { x: c.x, y: h - 9 }, dir: { x: 0, y: -1 } },
      ];
      sides.sort((p, q) => p.d - q.d);
      return { f, ...sides[0] };
    })
    .filter((o) => o.d < NET_EDGE_MAX)
    .sort((p, q) => p.d - q.d);
  const wantRuns = 2 + Math.floor(rand() * 2);
  let edgeRuns = 0;
  for (const o of rims) {
    if (edgeRuns >= wantRuns) {
      break;
    }
    if (
      routeBus(
        { foot: o.f, pt: footCenter(o.f) },
        { pt: o.pt, dir: o.dir },
        3 + Math.floor(rand() * 4),
        true,
      )
    ) {
      edgeRuns++;
    }
  }

  // Decoupling ceramic caps: 2-4 hugging one long edge of each major IC,
  // axis-parallel to the edge and aligned to pad columns (which now sit at
  // the 5px lane pitch). The center offset clears the IC courtyard (5.5),
  // the solder-mask margin (10) and the cap's own courtyard (5.8).
  for (const foot of majors) {
    const horiz = foot.w >= foot.h;
    const side = rand() < 0.5 ? -1 : 1;
    const off = 22 + rand() * 4;
    const want = DECAP_MIN + Math.floor(rand() * (DECAP_MAX - DECAP_MIN + 1));
    const row = (foot.pads ?? []).filter((p) =>
      horiz ? p.ny === side : p.nx === side,
    );
    let placed = 0;
    for (let attempt = 0; attempt < row.length && placed < want; attempt++) {
      const pad = row[Math.floor(rand() * row.length)];
      const p: Passive = horiz
        ? {
            x: pad.x,
            y: side < 0 ? foot.y - off : foot.y + foot.h + off,
            horiz: true,
            kind: "cap",
          }
        : {
            x: side < 0 ? foot.x - off : foot.x + foot.w + off,
            y: pad.y,
            horiz: false,
            kind: "cap",
          };
      const court = passiveCourt(p);
      if (!rejected(court)) {
        passives.push(p);
        courtyards.push(court);
        furniture.push(court);
        stampRect(court, occFurn);
        placed++;
      }
    }
  }

  // Bulk electrolytic cans near some major ICs, plus a few scattered.
  const tryCan = (x: number, y: number): boolean => {
    const court = canCourt(x, y);
    if (rejected(court)) {
      return false;
    }
    cans.push({ x, y, ang: rand() * Math.PI * 2 });
    courtyards.push({ ...court, round: true });
    furniture.push(court);
    stampRect(court, occFurn);
    return true;
  };
  for (const foot of majors) {
    if (rand() > 0.35) {
      continue;
    }
    const horiz = foot.w >= foot.h;
    const want = 1 + Math.floor(rand() * 2);
    let placed = 0;
    for (let attempt = 0; attempt < 6 && placed < want; attempt++) {
      // Off a short (padless) edge: courtyard 3 + margin 10 + can court 7.5.
      const side = rand() < 0.5 ? -1 : 1;
      const off = 21 + rand() * 8;
      const x = horiz
        ? side < 0
          ? foot.x - off
          : foot.x + foot.w + off
        : foot.x + 4 + rand() * Math.max(1, foot.w - 8);
      const y = horiz
        ? foot.y + 4 + rand() * Math.max(1, foot.h - 8)
        : side < 0
          ? foot.y - off
          : foot.y + foot.h + off;
      if (tryCan(x, y)) {
        placed++;
      }
    }
  }
  const looseCans = 2 + Math.floor(rand() * 2);
  for (let i = 0; i < looseCans; i++) {
    for (let attempt = 0; attempt < 8; attempt++) {
      if (tryCan(20 + rand() * (w - 40), 20 + rand() * (h - 40))) {
        break;
      }
    }
  }

  // Scattered discrete resistors and ceramic caps in open space.
  const looseParts = Math.min(10, Math.floor((w * h) / 150000));
  for (let i = 0; i < looseParts; i++) {
    for (let attempt = 0; attempt < 6; attempt++) {
      const p: Passive = {
        x: GRID * (1 + Math.floor(rand() * (w / GRID - 2))),
        y: GRID * (1 + Math.floor(rand() * (h / GRID - 2))),
        horiz: rand() < 0.5,
        kind: rand() < 0.55 ? "res" : "cap",
      };
      const court = passiveCourt(p);
      if (rejected(court)) {
        continue;
      }
      passives.push(p);
      courtyards.push(court);
      furniture.push(court);
      stampRect(court, occFurn);
      break;
    }
  }

  // Test points: a bare annular pad with an adjacent TP label, no courtyard.
  const tpCount = 3 + Math.floor(rand() * 3);
  for (let i = 0; i < tpCount; i++) {
    for (let attempt = 0; attempt < 8; attempt++) {
      const x = 20 + rand() * (w - 40);
      const y = 20 + rand() * (h - 40);
      const r: Rect = { x: x - 4.5, y: y - 4.5, w: 9, h: 9 };
      if (rejected(r)) {
        continue;
      }
      testpoints.push({ x, y });
      furniture.push(r);
      stampRect(r, occFurn);
      break;
    }
  }

  // ---- Stage 4: via stitching rows in free space. ----
  const stitchDirs: Point[] = [
    { x: 1, y: 0 },
    { x: 0, y: 1 },
    { x: 0.7071, y: 0.7071 },
    { x: 0.7071, y: -0.7071 },
  ];
  const freePoint = (x: number, y: number) =>
    x > 10 &&
    y > 10 &&
    x < w - 10 &&
    y < h - 10 &&
    !copperAt(x, y) &&
    !furniture.some(
      (f) =>
        x > f.x - 4 && x < f.x + f.w + 4 && y > f.y - 4 && y < f.y + f.h + 4,
    ) &&
    !keepOuts.some((k) => x > k.x && x < k.x + k.w && y > k.y && y < k.y + k.h);
  const stitchRows = 2 + Math.floor(rand() * 2);
  for (let i = 0; i < stitchRows; i++) {
    for (let attempt = 0; attempt < 12; attempt++) {
      const n = 6 + Math.floor(rand() * 5);
      const dir = stitchDirs[Math.floor(rand() * stitchDirs.length)];
      const sx = 20 + rand() * (w - 40);
      const sy = 20 + rand() * (h - 40);
      let ok = true;
      for (let k = 0; k < n && ok; k++) {
        ok = freePoint(
          sx + dir.x * STITCH_PITCH * k,
          sy + dir.y * STITCH_PITCH * k,
        );
      }
      if (!ok) {
        continue;
      }
      for (let k = 0; k < n; k++) {
        const px = sx + dir.x * STITCH_PITCH * k;
        const py = sy + dir.y * STITCH_PITCH * k;
        vias.push({ x: px, y: py, dx: 0, dy: 0 });
        stamp(
          [
            { x: px, y: py },
            { x: px, y: py },
          ],
          occTop,
        );
      }
      break;
    }
  }

  // Ambient filler: same discipline. Demoted walks (inner layer) get a via at
  // both ends so a dim trace reads as entering and leaving through plated holes.
  const ambient = Math.min(140, Math.floor((w * h) / AMBIENT_DIV));
  for (let i = 0; i < ambient; i++) {
    const start = {
      x: GRID * (1 + Math.floor(rand() * (w / GRID - 2))),
      y: GRID * (1 + Math.floor(rand() * (h / GRID - 2))),
    };
    const t = walk(rand, start, Math.floor(rand() * 8), w, h);
    if (!t || blocked(t.pts, occFurn)) {
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

  // Silkscreen reference designators, each anchored to its part's courtyard:
  // preferred just above the top-left corner, nudged below / left / right
  // when blocked, dropped when no side fits (the courtyard always prints).
  const silkRect = (x: number, y: number, text: string): Rect => ({
    x,
    y: y - 8,
    w: text.length * 4.5,
    h: 8,
  });
  const placeSilkNear = (text: string, court: Rect): boolean => {
    const tw = text.length * 4.5;
    const spots: Point[] = [
      { x: court.x, y: court.y - 3 },
      { x: court.x, y: court.y + court.h + 11 },
      { x: court.x - tw - 3, y: court.y + 7 },
      { x: court.x + court.w + 3, y: court.y + 7 },
    ];
    for (const s of spots) {
      const r = silkRect(s.x, s.y, text);
      // Adjacency needs a tighter margin than parts use: 2px keeps the label
      // clear of every courtyard (its own sits 3px away) without orphaning
      // it PART_MARGIN away from the outline it names.
      if (
        r.x < 2 ||
        r.y < 2 ||
        r.x + r.w > w - 2 ||
        r.y + r.h > h - 2 ||
        keepOuts.some((k) => overlaps(r, k, 0)) ||
        furniture.some((f) => overlaps(r, f, 2)) ||
        rectOnCopper(r)
      ) {
        continue;
      }
      silks.push({ x: s.x, y: s.y, text });
      furniture.push(r);
      return true;
    }
    return false;
  };
  let rIdx = 1;
  let cIdx = 1;
  let rnIdx = 1;
  let tpIdx = 1;
  for (let i = 0; i < footprints.length; i++) {
    placeSilkNear(`U${1 + Math.floor(rand() * 79)}`, footCourts[i]);
  }
  for (const bk of banks) {
    placeSilkNear(bk.kind === "res" ? `RN${rnIdx++}` : `C${cIdx++}`, bk.court);
  }
  for (const p of passives) {
    if (p.bank) {
      continue;
    }
    placeSilkNear(
      p.kind === "res" ? `R${rIdx++}` : `C${cIdx++}`,
      passiveCourt(p),
    );
  }
  for (const can of cans) {
    placeSilkNear(`C${cIdx++}`, canCourt(can.x, can.y));
  }
  for (const tp of testpoints) {
    placeSilkNear(`TP${tpIdx++}`, { x: tp.x - 4.5, y: tp.y - 4.5, w: 9, h: 9 });
  }

  // ---- Stage 6: ground pour. ----
  // A few large hatched regions grown greedily into free space by GRID steps,
  // so the copper fill reads as filling the gaps between routing rather than
  // as random stamps.
  const hatches: Rect[] = [];
  const pourCount = Math.min(
    POUR_MAX,
    Math.max(POUR_MIN, Math.floor((w * h) / 400000)),
  );
  for (let i = 0; i < pourCount; i++) {
    for (let attempt = 0; attempt < 14; attempt++) {
      let r: Rect = {
        x: GRID * Math.floor(rand() * Math.max(1, w / GRID - 3)),
        y: GRID * Math.floor(rand() * Math.max(1, h / GRID - 3)),
        w: GRID * 2,
        h: GRID * 2,
      };
      if (rejected(r)) {
        continue;
      }
      let step = Math.floor(rand() * 4);
      let grew = true;
      while (grew) {
        grew = false;
        const cands: Rect[] = [
          { x: r.x, y: r.y, w: r.w + GRID, h: r.h },
          { x: r.x - GRID, y: r.y, w: r.w + GRID, h: r.h },
          { x: r.x, y: r.y, w: r.w, h: r.h + GRID },
          { x: r.x, y: r.y - GRID, w: r.w, h: r.h + GRID },
        ];
        for (let j = 0; j < cands.length; j++) {
          const c = cands[(step + j) % cands.length];
          if (
            c.w <= GRID * POUR_MAX_SPAN &&
            c.h <= GRID * POUR_MAX_SPAN &&
            !rejected(c)
          ) {
            r = c;
            grew = true;
            step++;
            break;
          }
        }
      }
      hatches.push(r);
      furniture.push(r);
      break;
    }
  }

  // Pin escapes resolve last, once all copper and furniture is known, so no
  // escape tip (where its via lands) sits on a trace or inside another part.
  const escapeKeep = (x: number, y: number) =>
    !copperAt(x, y) &&
    !furniture.some(
      (f) =>
        x > f.x - 2 && x < f.x + f.w + 2 && y > f.y - 2 && y < f.y + f.h + 2,
    );
  for (const foot of footprints) {
    foot.pads = footPads(foot);
    foot.escapes = padEscapes(foot.pads, posRand(foot.x, foot.y), escapeKeep);
  }

  return {
    traces,
    connectors,
    outgoing,
    nodes,
    vias,
    hatches,
    footprints,
    passives,
    cans,
    testpoints,
    courtyards,
    silks,
    fiducials,
    holes,
  };
}

interface NodeChipProps {
  readonly label: string;
  readonly designator: string;
  readonly className?: string;
}

/** Refined ring-and-dot service marker (a hover target on the board). */
function NodeChip({ label, designator, className }: NodeChipProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-1.5 ${className ?? ""}`}
    >
      <span
        data-v14-node
        className="pointer-events-auto relative block h-2.5 w-2.5 rounded-full border border-[rgba(205,216,232,0.55)] transition-colors duration-300 hover:border-[rgba(246,202,190,0.9)]"
      >
        <span className="absolute inset-[2.5px] rounded-full bg-[rgba(232,238,248,0.9)]" />
      </span>
      {/* Printed like the board's silkscreen: silk paint tone, a reference
          designator, no UI glow. */}
      <span
        className="font-mono text-[0.68rem] font-semibold tracking-[0.26em] whitespace-nowrap uppercase"
        style={{ color: "rgba(170,188,214,0.8)", fontFamily: MONO_FONT }}
      >
        <span
          className="font-normal"
          style={{ color: "rgba(154,172,200,0.45)" }}
        >
          {designator}·
        </span>
        {label}
      </span>
    </div>
  );
}

export function HeroBoard() {
  const rootRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    const canvas = canvasRef.current;
    if (!root || !canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    const off = document.createElement("canvas");
    const offCtx = off.getContext("2d");
    if (!ctx || !offCtx) {
      return;
    }
    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    // Deterministic runtime stream for spawn timing and lane choice.
    const runtime = mulberry32(0x51ed270b);

    let board: Board | null = null;
    let pulses: Pulse[] = [];
    let flash: number[] = [];
    let hover: number[] = [];
    let rings: RingFlash[] = [];
    let hoveredIndex = -1;
    let hoverCleanups: Array<() => void> = [];
    let dpr = 1;
    let w = 0;
    let h = 0;
    let raf = 0;
    let last = 0;
    let spawnClock = 0;
    let inView = false;
    let disposed = false;

    function emitNode(nodeIndex: number) {
      if (!board || pulses.length >= 12) {
        return;
      }
      const lanes = (board.outgoing[nodeIndex] ?? []).filter((t) => t.len > 60);
      if (lanes.length === 0) {
        return;
      }
      const t = lanes[Math.floor(runtime() * lanes.length)];
      pulses.push({ trace: t, dist: 0, speed: 150 + runtime() * 90, to: t.to });
    }

    function spawnAmbient() {
      if (!board || pulses.length >= 12) {
        return;
      }
      const lanes = board.connectors.filter((t) => t.len > 60);
      if (lanes.length === 0) {
        return;
      }
      const t = lanes[Math.floor(runtime() * lanes.length)];
      pulses.push({ trace: t, dist: 0, speed: 150 + runtime() * 90, to: t.to });
    }

    function attach(chips: HTMLElement[]) {
      for (const c of hoverCleanups) {
        c();
      }
      hoverCleanups = chips.map((chip, i) => {
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

    // Pre-render the full static board once, at device resolution, with a
    // transparent background so the page shows through. The rAF loop then only
    // composites this bitmap and draws the live layers on top.
    function prerender() {
      if (!board) {
        return;
      }
      off.width = Math.round(w * dpr);
      off.height = Math.round(h * dpr);
      const g = offCtx!;
      g.setTransform(dpr, 0, 0, dpr, 0, 0);
      g.clearRect(0, 0, w, h);

      // Pad-dot grid.
      g.fillStyle = PAD_COLOR;
      const pitch = GRID * 2;
      for (let y = 0; y <= h; y += pitch) {
        for (let x = 0; x <= w; x += pitch) {
          g.fillRect(x - 0.8, y - 0.8, 1.6, 1.6);
        }
      }

      // Hatched ground-plane patches, under the traces.
      g.lineWidth = 1;
      for (const patch of board.hatches) {
        g.save();
        g.beginPath();
        g.rect(patch.x, patch.y, patch.w, patch.h);
        g.clip();
        g.strokeStyle = HATCH_COLOR;
        g.beginPath();
        for (let x = patch.x - patch.h; x <= patch.x + patch.w; x += 9) {
          g.moveTo(x, patch.y + patch.h);
          g.lineTo(x + patch.h, patch.y);
        }
        g.stroke();
        g.restore();
        g.strokeStyle = HATCH_EDGE;
        g.strokeRect(patch.x, patch.y, patch.w, patch.h);
      }

      g.lineCap = "round";
      g.lineJoin = "round";
      const strokeTrace = (t: Trace) => {
        g.beginPath();
        g.moveTo(t.pts[0].x, t.pts[0].y);
        for (let p = 1; p < t.pts.length; p++) {
          g.lineTo(t.pts[p].x, t.pts[p].y);
        }
        g.stroke();
      };
      // Inner-layer copper first, dim and thin, so top-layer copper paints over
      // it and each crossing reads as a genuine two-layer board.
      g.lineWidth = 1.1;
      g.strokeStyle = TRACE_ALT_COLOR;
      for (const t of board.traces) {
        if (t.connector || t.layer !== 1) {
          continue;
        }
        strokeTrace(t);
      }
      // Top-layer copper: signal lanes fatter than ambient filler.
      for (const t of board.traces) {
        if (!t.connector && t.layer === 1) {
          continue;
        }
        if (t.connector) {
          g.lineWidth = 2.2;
          g.strokeStyle = LANE_COLOR;
        } else {
          g.lineWidth = 1.4;
          g.strokeStyle = TRACE_COLOR;
        }
        strokeTrace(t);
      }

      // Scattered IC footprints populate the board.
      for (const foot of board.footprints) {
        // Pin escapes + vias, drawn first so the package body tucks over them.
        g.strokeStyle = TRACE_COLOR;
        g.lineWidth = 1;
        for (const esc of foot.escapes ?? []) {
          g.beginPath();
          g.moveTo(esc[0].x, esc[0].y);
          for (let p = 1; p < esc.length; p++) {
            g.lineTo(esc[p].x, esc[p].y);
          }
          g.stroke();
          const e = esc[esc.length - 1];
          g.fillStyle = VIA_COLOR;
          g.beginPath();
          g.arc(e.x, e.y, 1.5, 0, Math.PI * 2);
          g.fill();
          g.fillStyle = NAVY;
          g.beginPath();
          g.arc(e.x, e.y, 0.7, 0, Math.PI * 2);
          g.fill();
        }
        g.fillStyle = FOOT_BODY;
        g.strokeStyle = FOOT_EDGE;
        g.lineWidth = 1.1;
        g.beginPath();
        g.roundRect(foot.x, foot.y, foot.w, foot.h, 3);
        g.fill();
        g.stroke();
        g.fillStyle = FOOT_PIN;
        for (const pad of foot.pads ?? []) {
          if (pad.ny !== 0) {
            g.fillRect(
              pad.x - 1,
              pad.ny < 0 ? foot.y - PAD_LEN : foot.y + foot.h,
              2,
              PAD_LEN,
            );
          } else {
            g.fillRect(
              pad.nx < 0 ? foot.x - PAD_LEN : foot.x + foot.w,
              pad.y - 1,
              PAD_LEN,
              2,
            );
          }
        }
      }

      // Two-pad passives. Chip resistors: a 7x3.5 lighter body with two
      // brighter end lands. Ceramic caps: a squarer body between two lands.
      for (const p of board.passives) {
        const res = p.kind === "res";
        const l = res ? 5.5 : 5.6; // half-length along the part axis
        const landW = res ? 2 : 3;
        const landH = res ? 4 : 5.6;
        const bodyL = res ? 7 : 5.2;
        const bodyC = res ? 3.5 : 5.2;
        g.fillStyle = res ? RES_BODY : PASSIVE_BODY;
        if (p.horiz) {
          g.fillRect(p.x - bodyL / 2, p.y - bodyC / 2, bodyL, bodyC);
        } else {
          g.fillRect(p.x - bodyC / 2, p.y - bodyL / 2, bodyC, bodyL);
        }
        g.fillStyle = res ? RES_LAND : PASSIVE_PAD;
        if (p.horiz) {
          g.fillRect(p.x - l, p.y - landH / 2, landW, landH);
          g.fillRect(p.x + l - landW, p.y - landH / 2, landW, landH);
        } else {
          g.fillRect(p.x - landH / 2, p.y - l, landH, landW);
          g.fillRect(p.x - landH / 2, p.y + l - landW, landH, landW);
        }
      }

      // Electrolytic cans: filled round body, a crisp silk ring, and a
      // polarity stripe (a chord crescent on one side).
      for (const can of board.cans) {
        g.fillStyle = CAN_BODY;
        g.beginPath();
        g.arc(can.x, can.y, CAN_R, 0, Math.PI * 2);
        g.fill();
        g.fillStyle = CAN_STRIPE;
        g.beginPath();
        g.arc(can.x, can.y, CAN_R, can.ang - 0.6, can.ang + 0.6);
        g.closePath();
        g.fill();
        g.strokeStyle = SILK_SCATTER;
        g.lineWidth = 1;
        g.beginPath();
        g.arc(can.x, can.y, CAN_R, 0, Math.PI * 2);
        g.stroke();
      }

      // Test points: a single bare annular pad.
      for (const tp of board.testpoints) {
        g.fillStyle = VIA_COLOR;
        g.beginPath();
        g.arc(tp.x, tp.y, 3, 0, Math.PI * 2);
        g.fill();
        g.fillStyle = NAVY;
        g.beginPath();
        g.arc(tp.x, tp.y, 1.6, 0, Math.PI * 2);
        g.fill();
      }

      // Fiducials: a filled dot inside an open ring.
      for (const f of board.fiducials) {
        g.fillStyle = VIA_COLOR;
        g.beginPath();
        g.arc(f.x, f.y, 1.7, 0, Math.PI * 2);
        g.fill();
        g.strokeStyle = FOOT_EDGE;
        g.lineWidth = 1;
        g.beginPath();
        g.arc(f.x, f.y, 4.5, 0, Math.PI * 2);
        g.stroke();
      }

      // Mounting holes: a wide annular ring around a bare hole.
      for (const m of board.holes) {
        g.fillStyle = VIA_COLOR;
        g.beginPath();
        g.arc(m.x, m.y, 7, 0, Math.PI * 2);
        g.fill();
        g.fillStyle = NAVY;
        g.beginPath();
        g.arc(m.x, m.y, 4.2, 0, Math.PI * 2);
        g.fill();
      }

      // Vias: a teardrop fillet where the trace meets the pad (so it reads as
      // routed, not drawn), then a crisp copper ring with a plated hole.
      const padR = 2.3;
      for (const via of board.vias) {
        g.fillStyle = VIA_COLOR;
        // Teardrop: a wedge from the incoming trace out to the pad edge.
        const ax = via.x - via.dx * padR * 2.7;
        const ay = via.y - via.dy * padR * 2.7;
        const px = -via.dy * padR;
        const py = via.dx * padR;
        g.beginPath();
        g.moveTo(ax, ay);
        g.lineTo(via.x + px, via.y + py);
        g.lineTo(via.x - px, via.y - py);
        g.closePath();
        g.fill();
        // Annular ring.
        g.beginPath();
        g.arc(via.x, via.y, padR, 0, Math.PI * 2);
        g.fill();
        g.fillStyle = NAVY;
        g.beginPath();
        g.arc(via.x, via.y, 1.05, 0, Math.PI * 2);
        g.fill();
      }

      // Silkscreen courtyards: thin square-cornered body outlines printed
      // around every part (circles around electrolytic cans).
      g.strokeStyle = COURT_COLOR;
      g.lineWidth = 1;
      for (const ct of board.courtyards) {
        if (ct.round) {
          g.beginPath();
          g.arc(ct.x + ct.w / 2, ct.y + ct.h / 2, ct.w / 2, 0, Math.PI * 2);
          g.stroke();
        } else {
          g.strokeRect(ct.x, ct.y, ct.w, ct.h);
        }
      }

      // Silkscreen reference designators, printed on top like a real board.
      g.fillStyle = SILK_SCATTER;
      g.textAlign = "left";
      g.font = `7px ${MONO_FONT}`;
      for (const silk of board.silks) {
        if (silk.vert) {
          g.save();
          g.translate(silk.x, silk.y);
          g.rotate(Math.PI / 2);
          g.fillText(silk.text, 0, 0);
          g.restore();
        } else {
          g.fillText(silk.text, silk.x, silk.y);
        }
      }
    }

    function render(time: number) {
      const c = ctx!;
      c.setTransform(1, 0, 0, 1, 0, 0);
      c.globalCompositeOperation = "source-over";
      c.clearRect(0, 0, canvas!.width, canvas!.height);
      c.globalAlpha = BOARD_ALPHA;
      c.drawImage(off, 0, 0);
      c.globalAlpha = 1;
      if (!board) {
        return;
      }
      c.setTransform(dpr, 0, 0, dpr, 0, 0);
      c.globalCompositeOperation = "lighter";

      // Breathing cyan halos at each service node; hover lifts them.
      for (let i = 0; i < board.nodes.length; i++) {
        const n = board.nodes[i];
        const breathe = 1 + 0.08 * Math.sin(time / 177 + i * 2.1);
        const level = breathe * (1 + 0.5 * hover[i]);
        const halo = c.createRadialGradient(n.x, n.y, 0, n.x, n.y, 85);
        halo.addColorStop(0, `rgba(${GLOW_RGB},${0.13 * level})`);
        halo.addColorStop(1, `rgba(${GLOW_RGB},0)`);
        c.fillStyle = halo;
        c.beginPath();
        c.arc(n.x, n.y, 85, 0, Math.PI * 2);
        c.fill();
        const core = c.createRadialGradient(n.x, n.y, 0, n.x, n.y, 16);
        core.addColorStop(0, `rgba(${GLOW_RGB},${0.3 * level})`);
        core.addColorStop(1, `rgba(${GLOW_RGB},0)`);
        c.fillStyle = core;
        c.beginPath();
        c.arc(n.x, n.y, 16, 0, Math.PI * 2);
        c.fill();
      }

      // Coral messages: a fading trail and a soft glowing head.
      c.lineCap = "round";
      for (const pulse of pulses) {
        const alpha = envelope(pulse);
        if (alpha <= 0) {
          continue;
        }
        const head = pointAt(pulse.trace, pulse.dist);
        const chunks = 7;
        c.lineWidth = 1.7;
        let prev = pointAt(pulse.trace, Math.max(0, pulse.dist - PULSE_TRAIL));
        for (let k = 1; k <= chunks; k++) {
          const d = Math.max(
            0,
            pulse.dist - PULSE_TRAIL + (k * PULSE_TRAIL) / chunks,
          );
          const p = pointAt(pulse.trace, d);
          c.strokeStyle = `rgba(${MSG},${alpha * Math.pow(k / chunks, 2) * 0.95})`;
          c.beginPath();
          c.moveTo(prev.x, prev.y);
          c.lineTo(p.x, p.y);
          c.stroke();
          prev = p;
        }
        const g = c.createRadialGradient(head.x, head.y, 0, head.x, head.y, 9);
        g.addColorStop(0, `rgba(${MSG_SOFT},${alpha * 0.44})`);
        g.addColorStop(1, `rgba(${MSG_SOFT},0)`);
        c.fillStyle = g;
        c.beginPath();
        c.arc(head.x, head.y, 9, 0, Math.PI * 2);
        c.fill();
        c.fillStyle = `rgba(${MSG_SOFT},${alpha})`;
        c.beginPath();
        c.arc(head.x, head.y, 2, 0, Math.PI * 2);
        c.fill();
      }

      // Arrival flash rings at nodes and at loose trace endpoints.
      c.lineWidth = 1.4;
      for (let i = 0; i < board.nodes.length; i++) {
        if (flash[i] <= 0.02) {
          continue;
        }
        const n = board.nodes[i];
        c.strokeStyle = `rgba(${MSG},${0.4 * flash[i]})`;
        c.beginPath();
        c.arc(n.x, n.y, 8 + (1 - flash[i]) * 20, 0, Math.PI * 2);
        c.stroke();
      }
      for (const rf of rings) {
        if (rf.life <= 0.02) {
          continue;
        }
        c.strokeStyle = `rgba(${MSG},${0.4 * rf.life})`;
        c.beginPath();
        c.arc(rf.x, rf.y, 8 + (1 - rf.life) * 20, 0, Math.PI * 2);
        c.stroke();
      }

      c.globalCompositeOperation = "source-over";
    }

    function step(dt: number) {
      if (!board) {
        return;
      }
      for (let i = 0; i < board.nodes.length; i++) {
        flash[i] = Math.max(0, flash[i] - dt / 0.7);
        const target = i === hoveredIndex ? 1 : 0;
        hover[i] += (target - hover[i]) * Math.min(1, dt / 0.15);
      }
      for (const rf of rings) {
        rf.life = Math.max(0, rf.life - dt / 0.7);
      }
      rings = rings.filter((rf) => rf.life > 0);

      const finished: Pulse[] = [];
      for (const p of pulses) {
        p.dist += p.speed * dt;
        if (p.dist >= p.trace.len) {
          finished.push(p);
        }
      }
      pulses = pulses.filter((p) => p.dist < p.trace.len);
      for (const f of finished) {
        if (f.to >= 0) {
          flash[f.to] = 1;
        } else {
          const end = f.trace.pts[f.trace.pts.length - 1];
          rings.push({ x: end.x, y: end.y, life: 1 });
        }
      }

      spawnClock += dt * 1000;
      if (spawnClock >= 900) {
        spawnClock = 0;
        if (runtime() < 0.85) {
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
      if (raf === 0) {
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
    function sync() {
      if (!reduced && inView && !document.hidden) {
        start();
      } else {
        stop();
      }
    }

    function measureAndBuild() {
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = root!.clientWidth;
      h = root!.clientHeight;
      canvas!.width = Math.round(w * dpr);
      canvas!.height = Math.round(h * dpr);
      const rect = root!.getBoundingClientRect();
      const chips = Array.from(
        root!.querySelectorAll<HTMLElement>("[data-v14-node]"),
      ).filter((el) => el.offsetParent !== null);
      const nodes: Point[] = chips.map((chip) => {
        const r = chip.getBoundingClientRect();
        return {
          x: r.left + r.width / 2 - rect.left,
          y: r.top + r.height / 2 - rect.top,
        };
      });
      board = generateBoard(nodes, w, h);
      pulses = [];
      rings = [];
      flash = nodes.map(() => 0);
      hover = nodes.map(() => 0);
      hoveredIndex = -1;
      attach(chips);
      prerender();
    }

    measureAndBuild();
    render(0);

    let io: IntersectionObserver | null = null;
    const onVisibility = () => {
      sync();
    };
    if (!reduced) {
      io = new IntersectionObserver(
        (entries) => {
          inView = entries[entries.length - 1]?.isIntersecting ?? false;
          sync();
        },
        { rootMargin: "60px" },
      );
      io.observe(root);
      document.addEventListener("visibilitychange", onVisibility);
    }

    // The node markers sit in flex columns whose width depends on their label,
    // so a late web-font load shifts them. Re-measure once fonts settle.
    if (document.fonts?.ready) {
      document.fonts.ready.then(() => {
        if (!disposed) {
          measureAndBuild();
          render(0);
        }
      });
    }

    let resizeRaf = 0;
    const ro = new ResizeObserver(() => {
      cancelAnimationFrame(resizeRaf);
      resizeRaf = requestAnimationFrame(() => {
        if (
          disposed ||
          (root.clientWidth === w && root.clientHeight === h && board)
        ) {
          return;
        }
        measureAndBuild();
        render(0);
      });
    });
    ro.observe(root);

    return () => {
      disposed = true;
      stop();
      cancelAnimationFrame(resizeRaf);
      ro.disconnect();
      io?.disconnect();
      document.removeEventListener("visibilitychange", onVisibility);
      for (const c of hoverCleanups) {
        c();
      }
    };
  }, []);

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 overflow-hidden"
    >
      <canvas ref={canvasRef} className="absolute inset-0 h-full w-full" />
      <NodeChip
        label="Ordering"
        designator="U7"
        className="top-[22%] left-[54%]"
      />
      <NodeChip
        label="Billing"
        designator="U12"
        className="top-[14%] right-[10%]"
      />
      <NodeChip
        label="Catalog"
        designator="U31"
        className="top-[46%] left-[74%] max-md:hidden"
      />
      <NodeChip
        label="Payments"
        designator="U18"
        className="bottom-[30%] left-[57%] max-md:hidden"
      />
      <NodeChip
        label="Shipping"
        designator="U44"
        className="right-[7%] bottom-[18%] max-md:hidden"
      />
      <NodeChip
        label="Inventory"
        designator="U26"
        className="bottom-[10%] left-[70%] max-lg:hidden"
      />
      <div
        className="absolute inset-0"
        style={{
          background:
            "linear-gradient(90deg, rgba(11,15,26,0.92) 0%, rgba(11,15,26,0.5) 34%, rgba(11,15,26,0) 60%)",
        }}
      />
      <div
        className="absolute inset-0"
        style={{
          background:
            "linear-gradient(180deg, rgba(11,15,26,0) 70%, rgba(11,15,26,0.94) 98%)",
        }}
      />
      <div
        className="absolute inset-0"
        style={{
          background:
            "linear-gradient(180deg, rgba(11,15,26,0.6) 0%, rgba(11,15,26,0) 14%)",
        }}
      />
      <div className="absolute inset-0 bg-[#0b0f1a]/45 md:hidden" />
    </div>
  );
}
