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

import { CYAN, MONO_FONT, NAVY } from "./palette";

const GRID = 28;

const PAD_COLOR = "rgba(150, 166, 194, 0.14)";
const HATCH_COLOR = "rgba(150, 166, 194, 0.08)";
const HATCH_EDGE = "rgba(150, 166, 194, 0.13)";
const FOOT_BODY = "#0b1220";
const FOOT_EDGE = "rgba(158, 176, 204, 0.44)";
const FOOT_PIN = "rgba(158, 176, 204, 0.34)";
const PASSIVE_PAD = "rgba(168, 184, 210, 0.5)";
const PASSIVE_BODY = "rgba(120, 136, 164, 0.32)";
const SILK_SCATTER = "rgba(150, 168, 198, 0.42)";
const TRACE_COLOR = "rgba(139, 160, 188, 0.42)";
const TRACE_ALT_COLOR = "rgba(139, 160, 188, 0.26)";
const LANE_COLOR = "rgba(174, 190, 216, 0.5)";
const VIA_COLOR = "rgba(164, 180, 208, 0.55)";
const MSG = "224, 140, 122";
const MSG_SOFT = "246, 202, 190";

const CYAN_RGB = `${parseInt(CYAN.slice(1, 3), 16)}, ${parseInt(CYAN.slice(3, 5), 16)}, ${parseInt(CYAN.slice(5, 7), 16)}`;

const BOARD_ALPHA = 0.6;
const PULSE_TRAIL = 128;

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
  readonly connected: boolean;
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
  readonly hatches: Rect[];
  readonly footprints: Footprint[];
  readonly passives: Passive[];
  readonly silks: Silk[];
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
function padEscapes(pads: Pad[], rand: () => number): Point[][] {
  const escapes: Point[][] = [];
  for (const pad of pads) {
    if (pad.connected || rand() > 0.72) {
      continue;
    }
    const tipX = pad.x + pad.nx * 5;
    const tipY = pad.y + pad.ny * 5;
    const len = 9 + rand() * 9;
    escapes.push([
      { x: tipX, y: tipY },
      { x: tipX + pad.nx * len, y: tipY + pad.ny * len },
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
  // top-layer traces cross. Deliberate copper (the node backbone) is stamped
  // first and never rejected; random filler that would cross it is demoted to
  // a real inner layer (via-terminated, drawn dim) or dropped. Discs around
  // every node are exempt so the radial fans do not reject one another.
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

  // Node backbone: each node connects to its nearest neighbours. Deliberate
  // copper: laid and stamped, never rejected against the grid.
  for (let i = 0; i < nodes.length; i++) {
    const others = nodes
      .map((n, j) => ({ j, d: Math.hypot(n.x - nodes[i].x, n.y - nodes[i].y) }))
      .filter((o) => o.j > i && o.d < 620)
      .sort((a, b) => a.d - b.d)
      .slice(0, 2);
    for (const o of others) {
      const laneT = connector(nodes[i], nodes[o.j], o.j);
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
      if (!t) {
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
    if (!t) {
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

  // Keep-outs so board furniture never lands on a service node.
  const keepOuts: Rect[] = [];
  for (const n of nodes) {
    keepOuts.push({ x: n.x - 52, y: n.y - 44, w: 104, h: 96 });
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
    foot.escapes = padEscapes(foot.pads, posRand(foot.x, foot.y));
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

  return {
    traces,
    connectors,
    outgoing,
    nodes,
    vias,
    hatches,
    footprints,
    passives,
    silks,
  };
}

interface NodeChipProps {
  readonly label: string;
  readonly className?: string;
}

/** Refined ring-and-dot service marker (a hover target on the board). */
function NodeChip({ label, className }: NodeChipProps) {
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
      <span
        className="font-mono text-[0.6rem] tracking-[0.28em] uppercase"
        style={{ color: "rgba(176,192,216,0.85)", fontFamily: MONO_FONT }}
      >
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
        for (let px = foot.x + 7; px <= foot.x + foot.w - 9; px += 10) {
          g.fillRect(px, foot.y - 4, 3, 4);
          g.fillRect(px, foot.y + foot.h, 3, 4);
        }
      }

      // Passive two-pad parts (resistors/caps): two copper lands and a body.
      for (const p of board.passives) {
        if (p.horiz) {
          g.fillStyle = PASSIVE_BODY;
          g.fillRect(p.x - 3, p.y - 2.4, 6, 4.8);
          g.fillStyle = PASSIVE_PAD;
          g.fillRect(p.x - 6, p.y - 2.8, 3, 5.6);
          g.fillRect(p.x + 3, p.y - 2.8, 3, 5.6);
        } else {
          g.fillStyle = PASSIVE_BODY;
          g.fillRect(p.x - 2.4, p.y - 3, 4.8, 6);
          g.fillStyle = PASSIVE_PAD;
          g.fillRect(p.x - 2.8, p.y - 6, 5.6, 3);
          g.fillRect(p.x - 2.8, p.y + 3, 5.6, 3);
        }
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

      // Silkscreen reference designators, printed on top like a real board.
      g.fillStyle = SILK_SCATTER;
      g.textAlign = "left";
      g.font = `7px ${MONO_FONT}`;
      for (const silk of board.silks) {
        g.fillText(silk.text, silk.x, silk.y);
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
        halo.addColorStop(0, `rgba(${CYAN_RGB},${0.13 * level})`);
        halo.addColorStop(1, `rgba(${CYAN_RGB},0)`);
        c.fillStyle = halo;
        c.beginPath();
        c.arc(n.x, n.y, 85, 0, Math.PI * 2);
        c.fill();
        const core = c.createRadialGradient(n.x, n.y, 0, n.x, n.y, 16);
        core.addColorStop(0, `rgba(${CYAN_RGB},${0.3 * level})`);
        core.addColorStop(1, `rgba(${CYAN_RGB},0)`);
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
      <NodeChip label="Ordering" className="top-[22%] left-[54%]" />
      <NodeChip label="Billing" className="top-[14%] right-[10%]" />
      <NodeChip
        label="Catalog"
        className="top-[46%] left-[74%] max-md:hidden"
      />
      <NodeChip
        label="Payments"
        className="bottom-[30%] left-[57%] max-md:hidden"
      />
      <NodeChip
        label="Shipping"
        className="right-[7%] bottom-[18%] max-md:hidden"
      />
      <NodeChip
        label="Inventory"
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
