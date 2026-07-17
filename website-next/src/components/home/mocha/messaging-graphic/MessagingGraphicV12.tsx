"use client";

/**
 * Mocha messaging, version 12: "Board, on the record".
 *
 * The board-and-traffic system from v10/v11, restyled to the site palette
 * (the landing hero gradient: blue #16b9e4 for the service lights, coral
 * #f0786a for the messages) and reworked so the messages actually travel
 * THROUGH each section's concept motif, which becomes the focal point of that
 * section while it is in view.
 *
 * Each content section owns one motif, centered in a stage band as the
 * section's visual. A shared in-view controller ramps that motif's lamp to
 * full when the section is on screen, dims distant service nodes, and runs the
 * motif's scripted demo so a real pulse (or fan-out, reply, retry, rollback,
 * span-tap) crosses the printed structure:
 *
 *   mediator     reqreply   request out, reply back between two ICs
 *   bus          pubsub     one publish fans out to three subscribers
 *   patterns     triptych   a spotlight steps SEND / PUBLISH / REQ-REPLY
 *   reliability  outbox     outbox -> inbox, with an occasional retry stall
 *   sagas        saga       a pulse advances Draft -> Checked -> Published,
 *                           occasionally diverting down a compensation rail
 *   observability tracetap  a message sheds a span at each probe into a rail
 *   transports   bus        three transport lanes, the live one rotating
 *
 * Hero and CTA have no motif: the hero fans a pulse outward across the
 * services, the CTA pulls pulses inward to converge behind the button.
 *
 * Rendering pipeline unchanged: an offscreen "lit" canvas gets the visible
 * board slice, a light mask keeps it only where light falls, and glows and
 * trails draw additively over the page on a fixed viewport canvas translated
 * by scrollY.
 */

import Link from "next/link";
import { useEffect, useRef } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Board geometry. Traces walk on this grid with 45 degree bends, PCB style.
const GRID = 24;
const SUBSTRATE = "#0c1322";
const TRACE_COLOR = "rgba(148, 163, 184, 0.42)";
const TRACE_ALT_COLOR = "rgba(124, 146, 198, 0.34)";
const LANE_COLOR = "rgba(150, 175, 210, 0.7)";
const VIA_COLOR = "rgba(148, 163, 184, 0.55)";
const PAD_COLOR = "rgba(148, 163, 184, 0.12)";
const SILK_COLOR = "rgba(148, 163, 184, 0.45)";
const SILK_EM_COLOR = "rgba(124, 146, 198, 0.85)";
const HATCH_COLOR = "rgba(124, 146, 198, 0.08)";
const MONO_FONT = "ui-monospace, SFMono-Regular, Menlo, monospace";

// The two site-palette anchors (landing hero gradient).
const BLUE = "22, 185, 228";
const CORAL = "240, 120, 106";
const CORAL_SOFT = "253, 178, 168";

// Light sizes in CSS pixels.
const NODE_LIGHT_RADIUS = 168;
const PULSE_LIGHT_RADIUS = 108;
const PULSE_TRAIL = 150;

const MAX_NODE_PULSES = 10;
const NODE_SPAWN_MS = 520;

interface Point {
  readonly x: number;
  readonly y: number;
}

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

type TraceKind = "radial" | "connector" | "ambient";

interface Trace {
  readonly pts: Point[];
  /** Cumulative length at each point, cum[0] = 0. */
  readonly cum: number[];
  readonly len: number;
  /** Node index the trace leaves from, or -1. */
  readonly from: number;
  /** Node index a connector arrives at, or -1. */
  readonly to: number;
  readonly kind: TraceKind;
  rev?: Trace;
}

interface Silkscreen {
  x: number;
  y: number;
  text: string;
  em?: boolean;
}

interface Footprint {
  x: number;
  y: number;
  w: number;
  h: number;
}

interface HatchPatch {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** A point on a motif that flashes when a pulse arrives at it. */
interface MotifEndpoint {
  x: number;
  y: number;
  flash: number;
}

/** A lamp keeping a motif readable; brightens with the motif's focal level. */
interface MotifLight {
  x: number;
  y: number;
  r: number;
  /** Beat this lamp belongs to; the active beat's lamp is brightest. -1 = always. */
  cell: number;
}

type MotifKind =
  | "reqreply"
  | "pubsub"
  | "outbox"
  | "bus"
  | "patterns"
  | "saga"
  | "tracetap";

interface PatternCell {
  readonly kind: "send" | "pubsub" | "reqreply";
  readonly inLane: Trace;
  readonly outLanes: Trace[];
  readonly replyLane?: Trace;
  readonly endpoints: MotifEndpoint[];
}

interface Motif {
  readonly kind: MotifKind;
  readonly x: number;
  readonly y: number;
  readonly lamps: MotifLight[];
  readonly endpoints: MotifEndpoint[];
  readonly inLanes: Trace[];
  readonly outLanes: Trace[];
  readonly segments: Trace[];
  readonly branches: Trace[];
  readonly cells: PatternCell[];
  mainLane?: Trace;
  readonly taps: { at: number; trace: Trace; endpoint: MotifEndpoint }[];
  /** Vertical extent used for the in-view test. */
  readonly top: number;
  readonly bottom: number;
  level: number;
  clock: number;
  beat: number;
  activeIndex: number;
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
  dim?: boolean;
  flashEndpoint?: MotifEndpoint;
  replyLane?: Trace;
  replyEndpoint?: MotifEndpoint;
  fanout?: { trace: Trace; endpoint: MotifEndpoint }[];
  remaining?: { trace: Trace; endpoint: MotifEndpoint }[];
  taps?: {
    at: number;
    trace: Trace;
    endpoint: MotifEndpoint;
    fired: boolean;
  }[];
  retry?: boolean;
  retried?: boolean;
  originEndpoint?: MotifEndpoint;
}

interface Board {
  readonly traces: Trace[];
  readonly outgoing: Trace[][];
  readonly vias: Point[];
  readonly nodes: Point[];
  readonly ctaIndex: number;
  readonly silks: Silkscreen[];
  readonly footprints: Footprint[];
  readonly hatches: HatchPatch[];
  readonly motifs: Motif[];
  readonly bins: number[][];
  readonly width: number;
  readonly height: number;
}

const BIN_H = 300;

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

function pointInRect(x: number, y: number, r: Rect): boolean {
  return x >= r.x && x <= r.x + r.w && y >= r.y && y <= r.y + r.h;
}

function segmentHitsRect(a: Point, b: Point, r: Rect): boolean {
  const len = Math.hypot(b.x - a.x, b.y - a.y);
  const steps = Math.max(1, Math.ceil(len / 8));
  for (let i = 0; i <= steps; i++) {
    const t = i / steps;
    if (pointInRect(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, r)) {
      return true;
    }
  }
  return false;
}

function segmentHitsAny(a: Point, b: Point, keepOuts: Rect[]): boolean {
  for (const r of keepOuts) {
    if (segmentHitsRect(a, b, r)) {
      return true;
    }
  }
  return false;
}

function traceFromPoints(
  pts: Point[],
  from: number,
  to: number,
  kind: TraceKind,
): Trace {
  const cum: number[] = [0];
  let len = 0;
  for (let i = 1; i < pts.length; i++) {
    len += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    cum.push(len);
  }
  return { pts, cum, len, from, to, kind };
}

function walkTrace(
  rand: () => number,
  start: Point,
  dir: number,
  width: number,
  height: number,
  from: number,
  keepOuts: Rect[],
): Trace | null {
  const pts: Point[] = [start];
  let { x, y } = start;
  let d = dir;
  const segments = 3 + Math.floor(rand() * 5);
  for (let s = 0; s < segments; s++) {
    const diagonal = d % 2 === 1;
    const cells = diagonal
      ? 1 + Math.floor(rand() * 3)
      : 2 + Math.floor(rand() * 7);
    const nx = x + DIR_VECS[d].x * cells * GRID;
    const ny = y + DIR_VECS[d].y * cells * GRID;
    if (nx < GRID || nx > width - GRID || ny < GRID || ny > height - GRID) {
      break;
    }
    if (segmentHitsAny({ x, y }, { x: nx, y: ny }, keepOuts)) {
      break;
    }
    x = nx;
    y = ny;
    pts.push({ x, y });
    if (diagonal) {
      d = (d + (rand() < 0.5 ? 1 : 7)) % 8;
    } else if (rand() > 0.45) {
      d = (d + (rand() < 0.5 ? 1 : 7)) % 8;
    }
  }
  return pts.length >= 2
    ? traceFromPoints(pts, from, -1, from >= 0 ? "radial" : "ambient")
    : null;
}

function connectorTrace(
  a: Point,
  b: Point,
  from: number,
  to: number,
  keepOuts: Rect[],
): Trace {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  let mid1: Point;
  let mid2: Point;
  if (Math.abs(dx) >= Math.abs(dy)) {
    mid1 = { x: b.x - Math.sign(dx) * Math.abs(dy), y: a.y };
    mid2 = { x: a.x + Math.sign(dx) * Math.abs(dy), y: b.y };
  } else {
    mid1 = { x: a.x, y: b.y - Math.sign(dy) * Math.abs(dx) };
    mid2 = { x: b.x, y: a.y + Math.sign(dy) * Math.abs(dx) };
  }
  for (const mid of [mid1, mid2]) {
    if (
      !segmentHitsAny(a, mid, keepOuts) &&
      !segmentHitsAny(mid, b, keepOuts)
    ) {
      return traceFromPoints([a, mid, b], from, to, "connector");
    }
  }
  return traceFromPoints([a, mid1, b], from, to, "connector");
}

/** Collector the motif builders append their drawable pieces into. */
interface BoardBag {
  traces: Trace[];
  vias: Point[];
  silks: Silkscreen[];
  footprints: Footprint[];
  keepOuts: Rect[];
}

function endpoint(x: number, y: number): MotifEndpoint {
  return { x, y, flash: 0 };
}

function lamp(x: number, y: number, r: number, cell = -1): MotifLight {
  return { x, y, r, cell };
}

function line(pts: Point[], kind: TraceKind): Trace {
  return traceFromPoints(pts, -1, -1, kind);
}

function blankMotif(kind: MotifKind, x: number, y: number): Motif {
  return {
    kind,
    x,
    y,
    lamps: [],
    endpoints: [],
    inLanes: [],
    outLanes: [],
    segments: [],
    branches: [],
    cells: [],
    taps: [],
    top: y - 120,
    bottom: y + 120,
    level: 0,
    clock: 0,
    beat: 0,
    activeIndex: -1,
  };
}

/**
 * Build one concept motif: its drawable structure goes into the bag, and the
 * returned Motif keeps typed references to the lanes and endpoints the drive
 * routine animates.
 */
function buildMotif(
  kind: MotifKind,
  x: number,
  y: number,
  bag: BoardBag,
): Motif {
  const m = blankMotif(kind, x, y);

  if (kind === "reqreply") {
    bag.footprints.push({ x: x - 118, y: y - 11, w: 24, h: 22 });
    bag.footprints.push({ x: x + 94, y: y - 11, w: 24, h: 22 });
    const sender = endpoint(x - 106, y);
    const handler = endpoint(x + 106, y);
    const out = line(
      [
        { x: x - 92, y: y - 4 },
        { x: x + 92, y: y - 4 },
      ],
      "connector",
    );
    const back = line(
      [
        { x: x + 92, y: y + 4 },
        { x: x - 92, y: y + 4 },
      ],
      "connector",
    );
    bag.traces.push(out, back);
    m.inLanes.push(back);
    m.outLanes.push(out);
    m.endpoints.push(sender, handler);
    bag.silks.push({ x: x - 40, y: y - 22, text: "REQUEST / REPLY", em: true });
    bag.keepOuts.push({ x: x - 48, y: y - 34, w: 128, h: 20 });
    m.lamps.push(lamp(x, y, 168));
    m.top = y - 60;
    m.bottom = y + 60;
  } else if (kind === "pubsub") {
    const hub = endpoint(x, y);
    const input = line(
      [
        { x: x - 150, y },
        { x: x - 4, y },
      ],
      "connector",
    );
    const out0 = line(
      [
        { x, y },
        { x: x + 40, y },
        { x: x + 108, y: y - 60 },
      ],
      "connector",
    );
    const out1 = line(
      [
        { x, y },
        { x: x + 130, y },
      ],
      "connector",
    );
    const out2 = line(
      [
        { x, y },
        { x: x + 40, y },
        { x: x + 108, y: y + 60 },
      ],
      "connector",
    );
    bag.traces.push(input, out0, out1, out2);
    const subs = [
      endpoint(x + 108, y - 60),
      endpoint(x + 130, y),
      endpoint(x + 108, y + 60),
    ];
    m.inLanes.push(input);
    m.outLanes.push(out0, out1, out2);
    m.endpoints.push(hub, subs[0], subs[1], subs[2]);
    bag.vias.push({ x: x - 150, y }, ...subs);
    bag.silks.push({ x: x - 150, y: y - 12, text: "PUBLISH", em: true });
    bag.silks.push({ x: x + 112, y: y - 62, text: "SUB", em: true });
    bag.silks.push({ x: x + 134, y: y + 2, text: "SUB", em: true });
    bag.silks.push({ x: x + 112, y: y + 74, text: "SUB", em: true });
    bag.keepOuts.push({ x: x - 150, y: y - 24, w: 66, h: 18 });
    m.lamps.push(lamp(x + 6, y, 176));
    m.top = y - 80;
    m.bottom = y + 80;
  } else if (kind === "outbox") {
    bag.footprints.push({ x: x - 168, y: y - 22, w: 60, h: 44 });
    bag.footprints.push({ x: x + 108, y: y - 22, w: 60, h: 44 });
    const outbox = endpoint(x - 108, y);
    const inbox = endpoint(x + 108, y);
    const lane = line(
      [
        { x: x - 108, y },
        { x: x + 108, y },
      ],
      "connector",
    );
    bag.traces.push(lane);
    m.segments.push(lane);
    m.endpoints.push(outbox, inbox);
    bag.silks.push({ x: x - 166, y: y + 38, text: "OUTBOX", em: true });
    bag.silks.push({ x: x + 112, y: y + 38, text: "INBOX", em: true });
    bag.keepOuts.push({ x: x - 168, y: y - 24, w: 60, h: 68 });
    bag.keepOuts.push({ x: x + 108, y: y - 24, w: 60, h: 68 });
    m.lamps.push(lamp(x, y, 184));
    m.top = y - 60;
    m.bottom = y + 60;
  } else if (kind === "bus") {
    const names = ["RABBITMQ", "POSTGRES", "IN-PROCESS"];
    names.forEach((name, i) => {
      const ly = y + (i - 1) * 34;
      const lane = line(
        [
          { x: x - 220, y: ly },
          { x: x + 220, y: ly },
        ],
        "connector",
      );
      bag.traces.push(lane);
      m.segments.push(lane);
      m.endpoints.push(endpoint(x + 220, ly));
      bag.vias.push({ x: x - 220, y: ly }, { x: x + 220, y: ly });
      bag.silks.push({ x: x - 214, y: ly - 8, text: name, em: true });
      bag.keepOuts.push({ x: x - 220, y: ly - 20, w: 112, h: 14 });
      m.lamps.push(lamp(x, ly, 150, i));
    });
    m.top = y - 60;
    m.bottom = y + 60;
  } else if (kind === "patterns") {
    const cols = [x - 210, x, x + 210];
    const caps = ["SEND", "PUBLISH", "REQUEST / REPLY"];
    for (let c = 0; c < 3; c++) {
      const cx = cols[c];
      if (c === 0) {
        const out = line(
          [
            { x: cx - 70, y },
            { x: cx + 40, y },
            { x: cx + 70, y: y + 30 },
          ],
          "connector",
        );
        bag.traces.push(out);
        const end = endpoint(cx + 70, y + 30);
        bag.vias.push({ x: cx - 70, y }, end);
        m.cells.push({
          kind: "send",
          inLane: out,
          outLanes: [out],
          endpoints: [end],
        });
      } else if (c === 1) {
        const input = line(
          [
            { x: cx - 70, y },
            { x: cx - 6, y },
          ],
          "connector",
        );
        const b0 = line(
          [
            { x: cx, y },
            { x: cx + 24, y },
            { x: cx + 66, y: y - 34 },
          ],
          "connector",
        );
        const b1 = line(
          [
            { x: cx, y },
            { x: cx + 72, y },
          ],
          "connector",
        );
        const b2 = line(
          [
            { x: cx, y },
            { x: cx + 24, y },
            { x: cx + 66, y: y + 34 },
          ],
          "connector",
        );
        bag.traces.push(input, b0, b1, b2);
        const hub = endpoint(cx, y);
        const ends = [
          endpoint(cx + 66, y - 34),
          endpoint(cx + 72, y),
          endpoint(cx + 66, y + 34),
        ];
        bag.vias.push(...ends);
        m.cells.push({
          kind: "pubsub",
          inLane: input,
          outLanes: [b0, b1, b2],
          endpoints: [hub, ...ends],
        });
      } else {
        bag.footprints.push({ x: cx - 84, y: y - 9, w: 20, h: 18 });
        bag.footprints.push({ x: cx + 64, y: y - 9, w: 20, h: 18 });
        const out = line(
          [
            { x: cx - 62, y: y - 4 },
            { x: cx + 62, y: y - 4 },
          ],
          "connector",
        );
        const back = line(
          [
            { x: cx + 62, y: y + 4 },
            { x: cx - 62, y: y + 4 },
          ],
          "connector",
        );
        bag.traces.push(out, back);
        const a = endpoint(cx - 74, y);
        const b = endpoint(cx + 74, y);
        m.cells.push({
          kind: "reqreply",
          inLane: out,
          outLanes: [out],
          replyLane: back,
          endpoints: [a, b],
        });
      }
      bag.silks.push({ x: cx - 34, y: y + 62, text: caps[c], em: true });
      bag.keepOuts.push({ x: cx - 40, y: y + 50, w: 92, h: 18 });
      m.lamps.push(lamp(cx, y, 132, c));
    }
    m.top = y - 70;
    m.bottom = y + 80;
  } else if (kind === "saga") {
    const states = ["DRAFT", "CHECKED", "PUBLISHED"];
    const sx = [x - 170, x, x + 170];
    for (let i = 0; i < 3; i++) {
      bag.footprints.push({ x: sx[i] - 22, y: y - 15, w: 44, h: 30 });
      m.endpoints.push(endpoint(sx[i], y));
      bag.silks.push({ x: sx[i] - 20, y: y - 22, text: states[i], em: true });
      bag.keepOuts.push({ x: sx[i] - 26, y: y - 34, w: 56, h: 16 });
    }
    const s0 = line(
      [
        { x: sx[0] + 22, y },
        { x: sx[1] - 22, y },
      ],
      "connector",
    );
    const s1 = line(
      [
        { x: sx[1] + 22, y },
        { x: sx[2] - 22, y },
      ],
      "connector",
    );
    bag.traces.push(s0, s1);
    m.segments.push(s0, s1);
    // Compensation: Checked drops to a Compensated pad, then rolls back to Draft.
    const comp = endpoint(x, y + 78);
    m.endpoints.push(comp);
    bag.footprints.push({ x: x - 24, y: y + 64, w: 48, h: 28 });
    const down = line(
      [
        { x: sx[1], y: y + 15 },
        { x: sx[1], y: y + 64 },
      ],
      "connector",
    );
    const rail = line(
      [
        { x: sx[1] - 24, y: y + 78 },
        { x: sx[0], y: y + 78 },
        { x: sx[0], y: y + 15 },
      ],
      "connector",
    );
    bag.traces.push(down, rail);
    m.branches.push(down, rail);
    bag.silks.push({ x: x - 40, y: y + 104, text: "COMPENSATED", em: true });
    bag.keepOuts.push({ x: x - 44, y: y + 94, w: 96, h: 16 });
    m.lamps.push(lamp(x, y + 20, 210));
    m.top = y - 40;
    m.bottom = y + 110;
  } else {
    // tracetap: main message lane with three probes tapping into a rail.
    const railY = y - 70;
    const main = line(
      [
        { x: x - 220, y },
        { x: x + 220, y },
      ],
      "connector",
    );
    bag.traces.push(main);
    m.mainLane = main;
    bag.silks.push({ x: x - 220, y: y + 16, text: "TRACE", em: true });
    const rail = line(
      [
        { x: x - 220, y: railY },
        { x: x + 220, y: railY },
      ],
      "ambient",
    );
    bag.traces.push(rail);
    const probeX = [x - 120, x, x + 120];
    const labels = ["SPANS", "METRICS", "LOGS"];
    for (let i = 0; i < 3; i++) {
      const px = probeX[i];
      const probe = line(
        [
          { x: px, y },
          { x: px, y: railY },
        ],
        "ambient",
      );
      bag.traces.push(probe);
      const collector = endpoint(px, railY);
      m.endpoints.push(collector);
      bag.vias.push({ x: px, y }, { x: px, y: railY });
      bag.silks.push({ x: px - 6, y: railY - 8, text: labels[i], em: true });
      bag.keepOuts.push({ x: px - 8, y: railY - 20, w: 52, h: 14 });
      m.taps.push({ at: px - (x - 220), trace: probe, endpoint: collector });
    }
    m.lamps.push(lamp(x, (y + railY) / 2, 200));
    m.top = railY - 40;
    m.bottom = y + 40;
  }

  return m;
}

interface MotifAnchorPoint {
  readonly kind: MotifKind;
  readonly x: number;
  readonly y: number;
}

function generateBoard(
  nodes: Point[],
  ctaIndex: number,
  labelKeepOuts: Rect[],
  motifAnchors: MotifAnchorPoint[],
  width: number,
  height: number,
): Board {
  const rand = mulberry32(1337 + width * 31 + Math.floor(height));
  const bag: BoardBag = {
    traces: [],
    vias: [],
    silks: [],
    footprints: [],
    keepOuts: [...labelKeepOuts],
  };
  const outgoing: Trace[][] = nodes.map(() => []);

  const motifs = motifAnchors.map((a) => buildMotif(a.kind, a.x, a.y, bag));
  const keepOuts = bag.keepOuts;

  // Traces radiating out of every service node.
  for (let n = 0; n < nodes.length; n++) {
    const node = nodes[n];
    const count = 6 + Math.floor(rand() * 3);
    for (let i = 0; i < count; i++) {
      const dir = Math.floor(rand() * 8);
      const start = {
        x: node.x + DIR_VECS[dir].x * GRID * 0.5,
        y: node.y + DIR_VECS[dir].y * GRID * 0.5,
      };
      const trace = walkTrace(rand, start, dir, width, height, n, keepOuts);
      if (trace) {
        bag.traces.push(trace);
        outgoing[n].push(trace);
        bag.vias.push(trace.pts[trace.pts.length - 1]);
      }
    }
  }

  // A lane between each node and its two nearest neighbors, with a reversed
  // twin so replies and inbound convergence have a path.
  for (let i = 0; i < nodes.length; i++) {
    const others = nodes
      .map((n, j) => ({
        n,
        j,
        d: Math.hypot(n.x - nodes[i].x, n.y - nodes[i].y),
      }))
      .filter((o) => o.j > i)
      .sort((a, b) => a.d - b.d)
      .slice(0, 2);
    for (const o of others) {
      const laneTrace = connectorTrace(nodes[i], o.n, i, o.j, keepOuts);
      const back = traceFromPoints(
        [...laneTrace.pts].reverse(),
        o.j,
        i,
        "connector",
      );
      laneTrace.rev = back;
      back.rev = laneTrace;
      bag.traces.push(laneTrace);
      outgoing[i].push(laneTrace);
      outgoing[o.j].push(back);
    }
  }

  const ambientCount = Math.min(900, Math.floor((width * height) / 9500));
  for (let i = 0; i < ambientCount; i++) {
    const start = {
      x: GRID * (1 + Math.floor(rand() * (width / GRID - 2))),
      y: GRID * (1 + Math.floor(rand() * (height / GRID - 2))),
    };
    if (keepOuts.some((r) => pointInRect(start.x, start.y, r))) {
      continue;
    }
    const trace = walkTrace(
      rand,
      start,
      Math.floor(rand() * 8),
      width,
      height,
      -1,
      keepOuts,
    );
    if (trace) {
      bag.traces.push(trace);
      bag.vias.push(trace.pts[0], trace.pts[trace.pts.length - 1]);
    }
  }

  const hatches: HatchPatch[] = [];
  const hatchCount = Math.min(24, Math.floor((width * height) / 150000));
  for (let i = 0; i < hatchCount; i++) {
    const patch = {
      x: GRID * Math.floor(rand() * (width / GRID - 8)),
      y: GRID * Math.floor(rand() * (height / GRID - 8)),
      w: GRID * (3 + Math.floor(rand() * 5)),
      h: GRID * (2 + Math.floor(rand() * 4)),
    };
    if (!keepOuts.some((r) => rectsOverlap(patch, r))) {
      hatches.push(patch);
    }
  }

  const footCount = Math.min(34, Math.floor((width * height) / 100000));
  for (let i = 0; i < footCount; i++) {
    const foot = {
      x: GRID * Math.floor(rand() * (width / GRID - 5)),
      y: GRID * Math.floor(rand() * (height / GRID - 4)),
      w: GRID * (2 + Math.floor(rand() * 3)),
      h: GRID * (1 + Math.floor(rand() * 2)),
    };
    if (!keepOuts.some((r) => rectsOverlap(foot, r))) {
      bag.footprints.push(foot);
    }
  }

  const silkPool = ["U", "R", "C", "MSG-", "NET-", "TP"];
  const silkCount = Math.min(54, Math.floor((width * height) / 64000));
  for (let i = 0; i < silkCount; i++) {
    const prefix = silkPool[Math.floor(rand() * silkPool.length)];
    const silk = {
      x: GRID * (1 + Math.floor(rand() * (width / GRID - 3))),
      y: GRID * (1 + Math.floor(rand() * (height / GRID - 3))),
      text: `${prefix}${1 + Math.floor(rand() * 89)}`,
    };
    if (!keepOuts.some((r) => pointInRect(silk.x, silk.y, r))) {
      bag.silks.push(silk);
    }
  }

  const bins: number[][] = [];
  for (let i = 0; i < bag.traces.length; i++) {
    let minY = Infinity;
    let maxY = -Infinity;
    for (const p of bag.traces[i].pts) {
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
    traces: bag.traces,
    outgoing,
    vias: bag.vias,
    nodes,
    ctaIndex,
    silks: bag.silks,
    footprints: bag.footprints,
    hatches,
    motifs,
    bins,
    width,
    height,
  };
}

function rectsOverlap(a: Rect, b: Rect): boolean {
  return (
    a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y
  );
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

function pulseEnvelope(pulse: Pulse): number {
  return (
    Math.min(1, pulse.dist / 90) *
    Math.min(1, Math.max(0, (pulse.trace.len - pulse.dist) / 140))
  );
}

/** Every drivable lane of a motif plus its horizontal extent, for emphasis. */
function motifGeometry(m: Motif): {
  lanes: Trace[];
  minX: number;
  maxX: number;
} {
  const lanes: Trace[] = [
    ...m.inLanes,
    ...m.outLanes,
    ...m.segments,
    ...m.branches,
  ];
  if (m.mainLane) {
    lanes.push(m.mainLane);
  }
  for (const c of m.cells) {
    lanes.push(c.inLane, ...c.outLanes);
    if (c.replyLane) {
      lanes.push(c.replyLane);
    }
  }
  let minX = Infinity;
  let maxX = -Infinity;
  for (const l of lanes) {
    for (const p of l.pts) {
      minX = Math.min(minX, p.x);
      maxX = Math.max(maxX, p.x);
    }
  }
  for (const ep of m.endpoints) {
    minX = Math.min(minX, ep.x);
    maxX = Math.max(maxX, ep.x);
  }
  return { lanes, minX, maxX };
}

function CircuitCanvas() {
  const rootRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    const canvas = canvasRef.current;
    if (!root || !canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    if (!ctx) {
      return;
    }
    const litCanvas = document.createElement("canvas");
    const maskCanvas = document.createElement("canvas");
    const litCtx = litCanvas.getContext("2d");
    const maskCtx = maskCanvas.getContext("2d");
    if (!litCtx || !maskCtx) {
      return;
    }

    const reducedMotion = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;

    let board: Board | null = null;
    let pulses: Pulse[] = [];
    let power: number[] = [];
    let flash: number[] = [];
    let hover: number[] = [];
    let hoveredIndex = -1;
    let chipCleanups: Array<() => void> = [];
    let dpr = 1;
    let viewW = 0;
    let viewH = 0;
    let rafId = 0;
    let lastTime = 0;
    let elapsed = 0;
    let nodeSpawnClock = 0;
    let heroClock = 0;
    let ctaClock = 0;
    let openingDone = false;
    let disposed = false;

    function sizeCanvases() {
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      viewW = window.innerWidth;
      viewH = window.innerHeight;
      for (const c of [canvas!, litCanvas, maskCanvas]) {
        c.width = Math.round(viewW * dpr);
        c.height = Math.round(viewH * dpr);
      }
    }

    function emit(pulse: Pulse) {
      if (pulses.length < 40) {
        pulses.push(pulse);
      }
    }

    function spawnFromNode(nodeIndex: number, preferConnector = true) {
      if (!board) {
        return;
      }
      const lanes = board.outgoing[nodeIndex] ?? [];
      const connectors = lanes.filter(
        (t) => t.kind === "connector" && t.len > 120,
      );
      const pool =
        preferConnector && connectors.length > 0 && Math.random() < 0.7
          ? connectors
          : lanes.filter((t) => t.len > 110);
      if (pool.length === 0) {
        return;
      }
      const trace = pool[Math.floor(Math.random() * pool.length)];
      emit({ trace, dist: 0, speed: 250 + Math.random() * 170 });
    }

    function heroFanout() {
      if (!board) {
        return;
      }
      const lanes = (board.outgoing[0] ?? []).filter((t) => t.len > 120);
      for (const trace of lanes.slice(0, 4)) {
        emit({ trace, dist: 0, speed: 300 + Math.random() * 120 });
      }
    }

    function attachChipListeners(chips: HTMLElement[]) {
      for (const cleanup of chipCleanups) {
        cleanup();
      }
      chipCleanups = chips.map((chip, i) => {
        const enter = () => {
          hoveredIndex = i;
          if (!reducedMotion) {
            spawnFromNode(i);
            spawnFromNode(i);
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
      const scope = root!.parentElement ?? root!;
      const chips = Array.from(
        scope.querySelectorAll<HTMLElement>("[data-mg12-node]"),
      );
      const scrollY = window.scrollY;
      const nodes: Point[] = [];
      const labelKeepOuts: Rect[] = [];
      let ctaIndex = -1;
      chips.forEach((chip, i) => {
        const r = chip.getBoundingClientRect();
        nodes.push({
          x: r.left + r.width / 2,
          y: r.top + r.height / 2 + scrollY,
        });
        if (chip.dataset.mg12Cta !== undefined) {
          ctaIndex = i;
        }
        const label = chip.nextElementSibling;
        if (label && label.textContent) {
          const lr = label.getBoundingClientRect();
          labelKeepOuts.push({
            x: lr.left - 8,
            y: lr.top + scrollY - 6,
            w: lr.width + 16,
            h: lr.height + 12,
          });
        }
      });
      const motifAnchors: MotifAnchorPoint[] = Array.from(
        scope.querySelectorAll<HTMLElement>("[data-mg12-motif]"),
      )
        .filter((el) => el.offsetParent !== null)
        .map((el) => {
          const r = el.getBoundingClientRect();
          return {
            kind: el.dataset.mg12Motif as MotifKind,
            x: r.left + r.width / 2,
            y: r.top + r.height / 2 + scrollY,
          };
        });
      board = generateBoard(
        nodes,
        ctaIndex,
        labelKeepOuts,
        motifAnchors,
        root!.clientWidth,
        root!.scrollHeight,
      );
      pulses = [];
      power = nodes.map((_, i) => (reducedMotion ? 1 : (power[i] ?? 0)));
      flash = nodes.map(() => 0);
      hover = nodes.map(() => 0);
      openingDone = false;
      attachChipListeners(chips);
    }

    function drawBoardSlice(scrollY: number) {
      if (!board) {
        return;
      }
      litCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      litCtx!.globalCompositeOperation = "source-over";
      litCtx!.clearRect(0, 0, viewW, viewH);
      litCtx!.translate(0, -scrollY);
      const top = scrollY;
      const bottom = scrollY + viewH;

      litCtx!.fillStyle = SUBSTRATE;
      litCtx!.fillRect(0, top, viewW, viewH);
      litCtx!.fillStyle = PAD_COLOR;
      const pitch = GRID * 2;
      const firstRow = Math.floor(top / pitch) * pitch;
      for (let yy = firstRow; yy <= bottom; yy += pitch) {
        for (let xx = 0; xx <= viewW; xx += pitch) {
          litCtx!.fillRect(xx - 0.75, yy - 0.75, 1.5, 1.5);
        }
      }

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
        for (let xx = patch.x - patch.h; xx <= patch.x + patch.w; xx += 8) {
          litCtx!.moveTo(xx, patch.y + patch.h);
          litCtx!.lineTo(xx + patch.h, patch.y);
        }
        litCtx!.stroke();
        litCtx!.restore();
        litCtx!.strokeStyle = "rgba(148, 163, 184, 0.12)";
        litCtx!.strokeRect(patch.x, patch.y, patch.w, patch.h);
      }

      litCtx!.lineCap = "round";
      litCtx!.lineJoin = "round";
      const binFrom = Math.max(0, Math.floor(top / BIN_H));
      const binTo = Math.floor(bottom / BIN_H);
      const drawn = new Set<number>();
      for (let b = binFrom; b <= binTo; b++) {
        for (const i of board.bins[b] ?? []) {
          if (drawn.has(i)) {
            continue;
          }
          drawn.add(i);
          const trace = board.traces[i];
          if (trace.kind === "connector") {
            litCtx!.lineWidth = 2.4;
            litCtx!.strokeStyle = LANE_COLOR;
          } else {
            litCtx!.lineWidth = 1.4;
            litCtx!.strokeStyle = i % 6 === 0 ? TRACE_ALT_COLOR : TRACE_COLOR;
          }
          litCtx!.beginPath();
          litCtx!.moveTo(trace.pts[0].x, trace.pts[0].y);
          for (let p = 1; p < trace.pts.length; p++) {
            litCtx!.lineTo(trace.pts[p].x, trace.pts[p].y);
          }
          litCtx!.stroke();
        }
      }

      for (const foot of board.footprints) {
        if (foot.y + foot.h < top - 8 || foot.y > bottom + 8) {
          continue;
        }
        litCtx!.fillStyle = "#0a1120";
        litCtx!.strokeStyle = "rgba(148, 163, 184, 0.45)";
        litCtx!.lineWidth = 1.2;
        litCtx!.beginPath();
        litCtx!.roundRect(foot.x, foot.y, foot.w, foot.h, 3);
        litCtx!.fill();
        litCtx!.stroke();
        litCtx!.fillStyle = "rgba(148, 163, 184, 0.35)";
        for (let px = foot.x + 6; px <= foot.x + foot.w - 8; px += 9) {
          litCtx!.fillRect(px, foot.y - 4, 3, 4);
          litCtx!.fillRect(px, foot.y + foot.h, 3, 4);
        }
      }

      litCtx!.strokeStyle = VIA_COLOR;
      litCtx!.lineWidth = 1.2;
      litCtx!.fillStyle = SUBSTRATE;
      for (const via of board.vias) {
        if (via.y < top - 8 || via.y > bottom + 8) {
          continue;
        }
        litCtx!.beginPath();
        litCtx!.arc(via.x, via.y, 2.6, 0, Math.PI * 2);
        litCtx!.fill();
        litCtx!.stroke();
      }

      for (const silk of board.silks) {
        if (silk.y < top - 14 || silk.y > bottom + 14) {
          continue;
        }
        litCtx!.font = silk.em ? `10px ${MONO_FONT}` : `9px ${MONO_FONT}`;
        litCtx!.fillStyle = silk.em ? SILK_EM_COLOR : SILK_COLOR;
        litCtx!.fillText(silk.text, silk.x, silk.y);
      }
    }

    function nodeFocusDim(node: Point): number {
      // Dim service nodes that are far from whatever motif is in focus, so
      // the active motif is the brightest thing in its section.
      if (!board) {
        return 1;
      }
      let focus = 0;
      for (const m of board.motifs) {
        if (m.level > 0.2) {
          const near = Math.abs(m.y - node.y) < 260 ? 0 : m.level;
          focus = Math.max(focus, near);
        }
      }
      return 1 - 0.5 * focus;
    }

    function nodeLevel(i: number, time: number): number {
      if (!board) {
        return 0;
      }
      const flickerBase =
        0.86 +
        0.08 * Math.sin(time / 690 + i * 1.7) +
        0.06 * Math.sin(time / 251 + i * 3.1);
      return (
        flickerBase *
        power[i] *
        (1 + 0.35 * hover[i] + 0.8 * flash[i]) *
        nodeFocusDim(board.nodes[i])
      );
    }

    function drawLightMask(scrollY: number, time: number) {
      if (!board) {
        return;
      }
      maskCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      maskCtx!.clearRect(0, 0, viewW, viewH);
      maskCtx!.translate(0, -scrollY);
      const top = scrollY;
      const bottom = scrollY + viewH;
      const margin = NODE_LIGHT_RADIUS * 1.4;

      for (let i = 0; i < board.nodes.length; i++) {
        const node = board.nodes[i];
        if (node.y < top - margin || node.y > bottom + margin) {
          continue;
        }
        const level = nodeLevel(i, time);
        if (level <= 0.01) {
          continue;
        }
        const radius = NODE_LIGHT_RADIUS * (1 + 0.18 * flash[i]);
        const p1 = time * 0.00037 + i * 2.4;
        const p2 = time * 0.00029 + i * 1.3;
        const blobs = [
          { dx: 0, dy: 0, r: radius, a: 0.85 },
          {
            dx: Math.cos(p1) * 30,
            dy: Math.sin(p2) * 26,
            r: radius * 0.62,
            a: 0.5,
          },
          {
            dx: -Math.sin(p2) * 34,
            dy: -Math.cos(p1) * 24,
            r: radius * 0.48,
            a: 0.42,
          },
        ];
        for (const blob of blobs) {
          const bx = node.x + blob.dx;
          const by = node.y + blob.dy;
          const alpha = Math.min(1, blob.a * level);
          const g = maskCtx!.createRadialGradient(bx, by, 0, bx, by, blob.r);
          g.addColorStop(0, `rgba(255,255,255,${alpha})`);
          g.addColorStop(0.55, `rgba(255,255,255,${alpha * 0.45})`);
          g.addColorStop(1, "rgba(255,255,255,0)");
          maskCtx!.fillStyle = g;
          maskCtx!.beginPath();
          maskCtx!.arc(bx, by, blob.r, 0, Math.PI * 2);
          maskCtx!.fill();
        }
      }

      for (const m of board.motifs) {
        if (m.bottom < top - 40 || m.top > bottom + 40) {
          continue;
        }
        for (const lp of m.lamps) {
          const cellFactor =
            lp.cell < 0 ? 1 : m.activeIndex === lp.cell ? 1 : 0.4;
          const breathe = 0.92 + 0.08 * Math.sin(time / 840 + lp.x * 0.01);
          const alpha = (0.24 + 0.76 * m.level) * cellFactor * breathe;
          const g = maskCtx!.createRadialGradient(
            lp.x,
            lp.y,
            0,
            lp.x,
            lp.y,
            lp.r,
          );
          g.addColorStop(0, `rgba(255,255,255,${alpha})`);
          g.addColorStop(0.6, `rgba(255,255,255,${alpha * 0.4})`);
          g.addColorStop(1, "rgba(255,255,255,0)");
          maskCtx!.fillStyle = g;
          maskCtx!.beginPath();
          maskCtx!.arc(lp.x, lp.y, lp.r, 0, Math.PI * 2);
          maskCtx!.fill();
        }
      }

      for (const pulse of pulses) {
        const alpha = pulseEnvelope(pulse) * (pulse.dim ? 0.55 : 1);
        if (alpha <= 0) {
          continue;
        }
        for (let k = 0; k < 4; k++) {
          const d = pulse.dist - (k * PULSE_TRAIL) / 3;
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
          const r = PULSE_LIGHT_RADIUS * (1 - k * 0.18);
          const a = alpha * (1 - k * 0.24) * 0.9;
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

    function drawGlows(scrollY: number, time: number) {
      if (!board) {
        return;
      }
      ctx!.save();
      ctx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx!.translate(0, -scrollY);
      ctx!.globalCompositeOperation = "lighter";
      const top = scrollY;
      const bottom = scrollY + viewH;

      // Blue halo around each service node.
      for (let i = 0; i < board.nodes.length; i++) {
        const node = board.nodes[i];
        if (
          node.y < top - NODE_LIGHT_RADIUS ||
          node.y > bottom + NODE_LIGHT_RADIUS
        ) {
          continue;
        }
        const level = nodeLevel(i, time);
        if (level <= 0.01) {
          continue;
        }
        const haloR = NODE_LIGHT_RADIUS * 0.9;
        const halo = ctx!.createRadialGradient(
          node.x,
          node.y,
          0,
          node.x,
          node.y,
          haloR,
        );
        halo.addColorStop(0, `rgba(${BLUE},${0.14 * level})`);
        halo.addColorStop(0.45, `rgba(${BLUE},${0.05 * level})`);
        halo.addColorStop(1, `rgba(${BLUE},0)`);
        ctx!.fillStyle = halo;
        ctx!.beginPath();
        ctx!.arc(node.x, node.y, haloR, 0, Math.PI * 2);
        ctx!.fill();
        // Coral arrival ring: a message just landed here.
        if (flash[i] > 0.02) {
          const ringR = 14 + (1 - flash[i]) * 46;
          ctx!.strokeStyle = `rgba(${CORAL},${0.55 * flash[i]})`;
          ctx!.lineWidth = 1.6;
          ctx!.beginPath();
          ctx!.arc(node.x, node.y, ringR, 0, Math.PI * 2);
          ctx!.stroke();
        }
      }

      // Motif emphasis: the explanatory board (pub/sub, outbox, saga, ...) is
      // lifted and re-stroked over the reveal so its concept stays legible
      // while its section is in view, independent of where the light falls.
      for (const m of board.motifs) {
        if (m.bottom < top - 60 || m.top > bottom + 60 || m.level <= 0.03) {
          continue;
        }
        const geo = motifGeometry(m);
        const cx = (geo.minX + geo.maxX) / 2;
        const cy = (m.top + m.bottom) / 2;
        const rx = (geo.maxX - geo.minX) / 2 + 80;
        const ry = (m.bottom - m.top) / 2 + 56;

        // Soft panel lift so the dark board furniture under the motif reads.
        const panel = ctx!.createRadialGradient(
          cx,
          cy,
          0,
          cx,
          cy,
          Math.max(rx, ry),
        );
        panel.addColorStop(0, `rgba(${BLUE},${0.07 * m.level})`);
        panel.addColorStop(1, `rgba(${BLUE},0)`);
        ctx!.fillStyle = panel;
        ctx!.beginPath();
        ctx!.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
        ctx!.fill();

        // Bright structural lanes.
        ctx!.lineCap = "round";
        ctx!.lineJoin = "round";
        ctx!.lineWidth = 2.2;
        ctx!.strokeStyle = `rgba(150, 205, 240, ${0.34 + 0.5 * m.level})`;
        for (const l of geo.lanes) {
          ctx!.beginPath();
          ctx!.moveTo(l.pts[0].x, l.pts[0].y);
          for (let p = 1; p < l.pts.length; p++) {
            ctx!.lineTo(l.pts[p].x, l.pts[p].y);
          }
          ctx!.stroke();
        }

        // Endpoint dots (always) plus the coral arrival flash.
        for (const ep of m.endpoints) {
          ctx!.fillStyle = `rgba(150, 205, 240, ${0.55 * m.level})`;
          ctx!.beginPath();
          ctx!.arc(ep.x, ep.y, 2.8, 0, Math.PI * 2);
          ctx!.fill();
          if (ep.flash > 0.02) {
            const ringR = 8 + (1 - ep.flash) * 30;
            ctx!.strokeStyle = `rgba(${CORAL},${0.65 * ep.flash})`;
            ctx!.lineWidth = 1.8;
            ctx!.beginPath();
            ctx!.arc(ep.x, ep.y, ringR, 0, Math.PI * 2);
            ctx!.stroke();
            const g = ctx!.createRadialGradient(ep.x, ep.y, 0, ep.x, ep.y, 24);
            g.addColorStop(0, `rgba(${CORAL_SOFT},${0.55 * ep.flash})`);
            g.addColorStop(1, `rgba(${CORAL_SOFT},0)`);
            ctx!.fillStyle = g;
            ctx!.beginPath();
            ctx!.arc(ep.x, ep.y, 24, 0, Math.PI * 2);
            ctx!.fill();
          }
        }

        // Brighter concept captions (the silkscreen labels on this motif).
        ctx!.font = `600 11px ${MONO_FONT}`;
        const labelAlpha = 0.5 + 0.5 * m.level;
        for (const s of board.silks) {
          if (!s.em || s.y < m.top - 12 || s.y > m.bottom + 24) {
            continue;
          }
          if (Math.abs(s.x - cx) > rx + 60) {
            continue;
          }
          ctx!.fillStyle = `rgba(178, 210, 242, ${labelAlpha})`;
          ctx!.fillText(s.text, s.x, s.y);
        }
      }

      // Coral pulse trails and heads.
      ctx!.lineCap = "round";
      for (const pulse of pulses) {
        const dimF = pulse.dim ? 0.5 : 1;
        const alpha = pulseEnvelope(pulse) * dimF;
        if (alpha <= 0) {
          continue;
        }
        const head = pointAt(pulse.trace, pulse.dist);
        if (
          head.y > top - PULSE_TRAIL - PULSE_LIGHT_RADIUS &&
          head.y < bottom + PULSE_TRAIL + PULSE_LIGHT_RADIUS
        ) {
          const chunks = 8;
          ctx!.lineWidth = 2.4;
          let prev = pointAt(
            pulse.trace,
            Math.max(0, pulse.dist - PULSE_TRAIL),
          );
          for (let k = 1; k <= chunks; k++) {
            const d = Math.max(
              0,
              pulse.dist - PULSE_TRAIL + (k * PULSE_TRAIL) / chunks,
            );
            const p = pointAt(pulse.trace, d);
            ctx!.strokeStyle = `rgba(${CORAL},${alpha * Math.pow(k / chunks, 2) * 0.85})`;
            ctx!.beginPath();
            ctx!.moveTo(prev.x, prev.y);
            ctx!.lineTo(p.x, p.y);
            ctx!.stroke();
            prev = p;
          }
          const headR = pulse.dim ? 18 : 27;
          const g = ctx!.createRadialGradient(
            head.x,
            head.y,
            0,
            head.x,
            head.y,
            headR,
          );
          g.addColorStop(0, `rgba(${CORAL_SOFT},${alpha * 0.6})`);
          g.addColorStop(1, `rgba(${CORAL_SOFT},0)`);
          ctx!.fillStyle = g;
          ctx!.beginPath();
          ctx!.arc(head.x, head.y, headR, 0, Math.PI * 2);
          ctx!.fill();
          ctx!.fillStyle = `rgba(255,240,235,${alpha})`;
          ctx!.beginPath();
          ctx!.arc(head.x, head.y, 2.5, 0, Math.PI * 2);
          ctx!.fill();
        }
      }
      ctx!.restore();
    }

    function renderFrame(time: number) {
      const scrollY = window.scrollY;
      drawBoardSlice(scrollY);
      drawLightMask(scrollY, time);
      litCtx!.setTransform(1, 0, 0, 1, 0, 0);
      litCtx!.globalCompositeOperation = "destination-in";
      litCtx!.drawImage(maskCanvas, 0, 0);
      litCtx!.globalCompositeOperation = "source-over";
      ctx!.setTransform(1, 0, 0, 1, 0, 0);
      ctx!.globalCompositeOperation = "source-over";
      ctx!.clearRect(0, 0, canvas!.width, canvas!.height);
      ctx!.drawImage(litCanvas, 0, 0);
      drawGlows(scrollY, time);
    }

    function handleArrival(p: Pulse) {
      if (p.flashEndpoint) {
        p.flashEndpoint.flash = 1;
      }
      if (p.trace.to >= 0) {
        flash[p.trace.to] = 1;
      }
      if (p.replyLane) {
        emit({
          trace: p.replyLane,
          dist: 0,
          speed: p.speed * 0.85,
          dim: true,
          flashEndpoint: p.replyEndpoint,
        });
      }
      if (p.fanout) {
        for (const f of p.fanout) {
          emit({
            trace: f.trace,
            dist: 0,
            speed: p.speed,
            flashEndpoint: f.endpoint,
          });
        }
      }
      if (p.remaining && p.remaining.length > 0) {
        const [head, ...rest] = p.remaining;
        emit({
          trace: head.trace,
          dist: 0,
          speed: p.speed,
          flashEndpoint: head.endpoint,
          remaining: rest,
        });
      }
    }

    function stepPulses(dt: number) {
      const finished: Pulse[] = [];
      for (const pulse of pulses) {
        pulse.dist += pulse.speed * dt;
        if (
          pulse.retry &&
          !pulse.retried &&
          pulse.dist >= pulse.trace.len * 0.5
        ) {
          pulse.dist = 0;
          pulse.retried = true;
          if (pulse.originEndpoint) {
            pulse.originEndpoint.flash = 1;
          }
        }
        if (pulse.taps) {
          for (const t of pulse.taps) {
            if (!t.fired && pulse.dist >= t.at) {
              t.fired = true;
              emit({
                trace: t.trace,
                dist: 0,
                speed: 220,
                dim: true,
                flashEndpoint: t.endpoint,
              });
            }
          }
        }
        if (pulse.dist >= pulse.trace.len) {
          finished.push(pulse);
        }
      }
      pulses = pulses.filter((p) => p.dist < p.trace.len);
      for (const f of finished) {
        handleArrival(f);
      }
    }

    function driveMotif(m: Motif, dt: number) {
      if (m.level < 0.25) {
        return;
      }
      m.clock += dt * 1000;
      if (m.kind === "reqreply") {
        if (m.clock >= 2500) {
          m.clock = 0;
          emit({
            trace: m.outLanes[0],
            dist: 0,
            speed: 300,
            flashEndpoint: m.endpoints[1],
            replyLane: m.inLanes[0],
            replyEndpoint: m.endpoints[0],
          });
        }
      } else if (m.kind === "pubsub") {
        if (m.clock >= 2200) {
          m.clock = 0;
          emit({
            trace: m.inLanes[0],
            dist: 0,
            speed: 300,
            flashEndpoint: m.endpoints[0],
            fanout: m.outLanes.map((t, i) => ({
              trace: t,
              endpoint: m.endpoints[1 + i],
            })),
          });
        }
      } else if (m.kind === "outbox") {
        if (m.clock >= 2600) {
          m.clock = 0;
          m.beat++;
          emit({
            trace: m.segments[0],
            dist: 0,
            speed: 240,
            flashEndpoint: m.endpoints[1],
            retry: m.beat % 4 === 0,
            originEndpoint: m.endpoints[0],
          });
        }
      } else if (m.kind === "bus") {
        if (m.clock >= 2100) {
          m.clock = 0;
          m.beat++;
          const multi = m.beat % 5 === 0;
          if (multi) {
            m.segments.forEach((lane, i) =>
              emit({
                trace: lane,
                dist: 0,
                speed: 300,
                flashEndpoint: m.endpoints[i],
              }),
            );
          } else {
            const idx = m.beat % m.segments.length;
            m.activeIndex = idx;
            emit({
              trace: m.segments[idx],
              dist: 0,
              speed: 300,
              flashEndpoint: m.endpoints[idx],
            });
          }
        }
      } else if (m.kind === "patterns") {
        if (m.clock >= 1300) {
          m.clock = 0;
          m.beat = (m.beat + 1) % 3;
          m.activeIndex = m.beat;
          const cell = m.cells[m.beat];
          if (!cell) {
            return;
          }
          if (cell.kind === "send") {
            emit({
              trace: cell.outLanes[0],
              dist: 0,
              speed: 300,
              flashEndpoint: cell.endpoints[0],
            });
          } else if (cell.kind === "pubsub") {
            emit({
              trace: cell.inLane,
              dist: 0,
              speed: 300,
              flashEndpoint: cell.endpoints[0],
              fanout: cell.outLanes.map((t, i) => ({
                trace: t,
                endpoint: cell.endpoints[1 + i],
              })),
            });
          } else {
            emit({
              trace: cell.outLanes[0],
              dist: 0,
              speed: 300,
              flashEndpoint: cell.endpoints[1],
              replyLane: cell.replyLane,
              replyEndpoint: cell.endpoints[0],
            });
          }
        }
      } else if (m.kind === "saga") {
        if (m.clock >= 4600) {
          m.clock = 0;
          m.beat++;
          const compensate = m.beat % 3 === 0 && m.branches.length >= 2;
          if (compensate) {
            // Draft -> Checked, then divert down and roll back to Draft.
            emit({
              trace: m.segments[0],
              dist: 0,
              speed: 220,
              flashEndpoint: m.endpoints[1],
              remaining: [
                { trace: m.branches[0], endpoint: m.endpoints[3] },
                { trace: m.branches[1], endpoint: m.endpoints[0] },
              ],
            });
          } else {
            emit({
              trace: m.segments[0],
              dist: 0,
              speed: 220,
              flashEndpoint: m.endpoints[1],
              remaining: [{ trace: m.segments[1], endpoint: m.endpoints[2] }],
            });
          }
        }
      } else if (m.kind === "tracetap" && m.mainLane) {
        if (m.clock >= 1900) {
          m.clock = 0;
          emit({
            trace: m.mainLane,
            dist: 0,
            speed: 260,
            taps: m.taps.map((t) => ({ ...t, fired: false })),
          });
        }
      }
    }

    function stepMotifs(dt: number, scrollY: number) {
      if (!board) {
        return;
      }
      const center = scrollY + viewH * 0.5;
      for (const m of board.motifs) {
        const onScreen =
          m.bottom > scrollY + viewH * 0.08 && m.top < scrollY + viewH * 0.92;
        const target = onScreen ? 1 : 0;
        m.level += (target - m.level) * Math.min(1, dt / 0.5);
        for (const ep of m.endpoints) {
          ep.flash = Math.max(0, ep.flash - dt / 0.6);
        }
        if (!reducedMotion) {
          // Weight the drive toward the motif nearest the viewport center.
          const focal = Math.abs(m.y - center) < viewH * 0.55;
          if (focal) {
            driveMotif(m, dt);
          }
        }
      }
    }

    function stepNodes(dt: number, scrollY: number) {
      if (!board) {
        return;
      }
      const bottom = scrollY + viewH;
      for (let i = 0; i < board.nodes.length; i++) {
        const y = board.nodes[i].y;
        if (y > scrollY - 60 && y < bottom + 60) {
          power[i] = Math.min(1, power[i] + dt / 0.9);
        }
        flash[i] = Math.max(0, flash[i] - dt / 0.6);
        const hoverTarget = i === hoveredIndex ? 1 : 0;
        hover[i] += (hoverTarget - hover[i]) * Math.min(1, dt / 0.15);
      }
    }

    function driveHeroAndCta(dt: number, scrollY: number) {
      if (!board) {
        return;
      }
      // Hero: fan a pulse outward across the services near the top of the page.
      const heroOnScreen = board.nodes[0] && board.nodes[0].y < scrollY + viewH;
      if (!openingDone && elapsed > 600) {
        openingDone = true;
        heroFanout();
      }
      heroClock += dt * 1000;
      if (heroClock >= 4200) {
        heroClock = 0;
        if (heroOnScreen) {
          heroFanout();
        }
      }
      // CTA: pull inbound pulses toward the CTA node behind the button.
      ctaClock += dt * 1000;
      if (ctaClock >= 1500 && board.ctaIndex >= 0) {
        ctaClock = 0;
        const cta = board.ctaIndex;
        const ctaY = board.nodes[cta].y;
        if (ctaY > scrollY && ctaY < scrollY + viewH) {
          const inbound = (board.outgoing[cta] ?? [])
            .filter((t) => t.kind === "connector" && t.rev)
            .map((t) => t.rev as Trace);
          if (inbound.length > 0) {
            const trace = inbound[Math.floor(Math.random() * inbound.length)];
            emit({ trace, dist: 0, speed: 260 });
          }
        }
      }
    }

    function loop(time: number) {
      if (disposed) {
        return;
      }
      const dt = lastTime > 0 ? Math.min((time - lastTime) / 1000, 0.05) : 0;
      lastTime = time;
      elapsed += dt * 1000;
      const scrollY = window.scrollY;
      stepNodes(dt, scrollY);
      stepMotifs(dt, scrollY);
      driveHeroAndCta(dt, scrollY);
      nodeSpawnClock += dt * 1000;
      if (nodeSpawnClock >= NODE_SPAWN_MS) {
        nodeSpawnClock = 0;
        if (pulses.length < MAX_NODE_PULSES && Math.random() < 0.5 && board) {
          spawnFromNode(Math.floor(Math.random() * board.nodes.length));
        }
      }
      stepPulses(dt);
      renderFrame(time);
      rafId = requestAnimationFrame(loop);
    }

    function startLoop() {
      if (!reducedMotion && rafId === 0) {
        lastTime = 0;
        rafId = requestAnimationFrame(loop);
      }
    }

    function stopLoop() {
      if (rafId !== 0) {
        cancelAnimationFrame(rafId);
        rafId = 0;
      }
    }

    function staticRender() {
      if (!board) {
        return;
      }
      for (const m of board.motifs) {
        const onScreen =
          m.bottom > window.scrollY && m.top < window.scrollY + viewH;
        m.level = onScreen ? 1 : 0;
      }
      renderFrame(0);
    }

    measureAndBuild();
    if (reducedMotion) {
      staticRender();
    } else {
      startLoop();
    }

    const onScroll = () => {
      if (reducedMotion) {
        staticRender();
      }
    };
    const onVisibility = () => {
      if (document.hidden) {
        stopLoop();
      } else {
        startLoop();
      }
    };
    let resizeRaf = 0;
    const resizeObserver = new ResizeObserver(() => {
      cancelAnimationFrame(resizeRaf);
      resizeRaf = requestAnimationFrame(() => {
        measureAndBuild();
        if (reducedMotion) {
          staticRender();
        }
      });
    });
    resizeObserver.observe(root);
    window.addEventListener("scroll", onScroll, { passive: true });
    document.addEventListener("visibilitychange", onVisibility);

    return () => {
      disposed = true;
      stopLoop();
      cancelAnimationFrame(resizeRaf);
      resizeObserver.disconnect();
      window.removeEventListener("scroll", onScroll);
      document.removeEventListener("visibilitychange", onVisibility);
      for (const cleanup of chipCleanups) {
        cleanup();
      }
    };
  }, []);

  return (
    <div ref={rootRef} className="absolute inset-0" aria-hidden="true">
      <canvas
        ref={canvasRef}
        className="pointer-events-none fixed inset-0 h-full w-full"
      />
    </div>
  );
}

interface ServiceNodeProps {
  readonly label: string;
  readonly className?: string;
  readonly cta?: boolean;
}

/** A blue chip marking a service: a light source on the board. */
function ServiceNode({ label, className, cta }: ServiceNodeProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-2 ${className ?? ""}`}
    >
      <span
        data-mg12-node
        data-mg12-cta={cta ? "" : undefined}
        className="border-cc-accent/80 pointer-events-auto relative block h-4 w-4 cursor-default rounded-[4px] border bg-[#052430] shadow-[0_0_18px_4px_rgba(22,185,228,0.25)] transition-shadow duration-300 hover:shadow-[0_0_28px_8px_rgba(22,185,228,0.45)]"
        style={{
          borderColor: "rgba(22,185,228,0.85)",
        }}
      >
        <span
          className="absolute inset-[4px] rounded-[2px]"
          style={{
            backgroundColor: "#16b9e4",
            boxShadow: "0 0 7px 1px rgba(56,205,246,0.9)",
          }}
        />
      </span>
      {label ? (
        <span
          className="font-mono text-[0.6rem] tracking-[0.22em] uppercase"
          style={{
            color: "#8fd6ee",
            textShadow: "0 0 14px rgba(22,185,228,0.4)",
          }}
        >
          {label}
        </span>
      ) : null}
    </div>
  );
}

/** Centered stage that hosts a motif as the section's visual (desktop only). */
function MotifStage({ kind }: { readonly kind: MotifKind }) {
  return (
    <div className="relative mt-12 hidden h-72 lg:block" aria-hidden="true">
      <div
        data-mg12-motif={kind}
        className="absolute top-1/2 left-1/2 h-px w-px -translate-x-1/2 -translate-y-1/2"
      />
    </div>
  );
}

interface SectionProps {
  readonly eyebrow: string;
  readonly headline: string;
  readonly subhead?: string;
  readonly body: string;
  readonly children?: React.ReactNode;
}

/** Shared section shell: eyebrow, headline, body, then the motif stage. */
function Section({ eyebrow, headline, subhead, body, children }: SectionProps) {
  return (
    <div className="relative z-10 mx-auto max-w-3xl text-center">
      <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.22em] uppercase">
        {eyebrow}
      </p>
      <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold text-balance">
        {headline}
      </h2>
      {subhead ? (
        <p className="text-cc-accent mt-3 font-mono text-[0.72rem] tracking-[0.12em] uppercase">
          {subhead}
        </p>
      ) : null}
      <p className="text-cc-ink mt-5 text-base text-pretty sm:text-lg">
        {body}
      </p>
      {children}
    </div>
  );
}

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

export function MessagingGraphicV12() {
  return (
    <div className="relative">
      <CircuitCanvas />

      {/* Hero */}
      <section className="relative flex min-h-svh flex-col items-center justify-center px-5 py-24 text-center">
        <ServiceNode label="Ordering" className="top-[16%] left-[11%]" />
        <ServiceNode
          label="Billing"
          className="top-[24%] right-[9%] max-md:hidden"
        />
        <ServiceNode
          label="Payments"
          className="bottom-[21%] left-[17%] max-md:hidden"
        />
        <ServiceNode
          label="Shipping"
          className="right-[12%] bottom-[6%] md:right-[15%] md:bottom-[27%]"
        />

        <div className="relative z-10 max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Mocha · Messaging
          </p>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6 leading-[1.08] font-semibold text-balance">
            Every app runs on events.
          </h1>
          <p className="text-cc-accent mt-4 font-mono text-[0.72rem] tracking-[0.14em] uppercase">
            Messaging framework for .NET
          </p>
          <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
            A CreateReview command, a ReviewCreated event, a saga that moves a
            review from Draft to Published. Mocha turns backend behavior into
            commands, events, handlers, and sagas for .NET, in-process and
            across services. One handler-first model, with every hop visible in
            Nitro.
          </p>
          <div className="mt-9 flex flex-wrap justify-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
          </div>
        </div>
      </section>

      {/* Mediator */}
      <section className="relative mx-auto max-w-6xl px-5 py-32 sm:px-12">
        <ServiceNode
          label="Reviews"
          className="top-[8%] left-[5%] max-md:hidden"
        />
        <Section
          eyebrow="Mediator"
          headline="A command dispatches through the in-process mediator."
          subhead="Source-generated, zero-reflection"
          body="ISender.Send(CreateReview) lands on a [Handler] method. A Roslyn source generator discovers the handler at compile time and emits typed registration plus a pre-compiled pipeline. Dispatch is a direct call, zero-reflection and AOT-friendly, not a reflective lookup on the hot path."
        />
        <MotifStage kind="reqreply" />
      </section>

      {/* Bus */}
      <section className="relative mx-auto max-w-6xl px-5 py-32 sm:px-12">
        <ServiceNode
          label="Notifications"
          className="top-[10%] right-[5%] max-md:hidden"
        />
        <Section
          eyebrow="Bus"
          headline="The same model crosses service boundaries."
          subhead="Cross-service message bus"
          body="The handler publishes ReviewCreated. The handler shape that powers an in-process notification powers a cross-service consumer without changes. PublishAsync and SendAsync read identically whether the message stays in-process or rides a transport to another service, so other services subscribe without coupling."
        />
        <MotifStage kind="pubsub" />
      </section>

      {/* Patterns */}
      <section className="relative mx-auto max-w-6xl px-5 py-32 sm:px-12">
        <Section
          eyebrow="Patterns"
          headline="Three patterns, one handler-first model."
          subhead="pub/sub · fire-and-forget · request/reply"
          body="The same API covers pub/sub, fire-and-forget, and request/reply, used independently or together. PublishAsync asks who needs to know, SendAsync asks who should act, RequestAsync asks what the result is. The catalog is the Enterprise Integration Patterns, not a bespoke vocabulary."
        />
        <MotifStage kind="patterns" />
        <div className="relative z-10 mx-auto mt-4 grid max-w-4xl gap-4 md:grid-cols-3">
          {PATTERNS.map((pattern) => (
            <div
              key={pattern.name}
              className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 text-left backdrop-blur-sm"
            >
              <p className="text-cc-heading font-heading text-[1rem] font-semibold">
                {pattern.name}
              </p>
              <p className="text-cc-ink-dim mt-1 text-sm">{pattern.question}</p>
              <span className="border-cc-accent/60 text-cc-accent bg-cc-surface mt-3 inline-block rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem]">
                {pattern.method}
              </span>
            </div>
          ))}
        </div>
      </section>

      {/* Reliability */}
      <section className="relative mx-auto max-w-6xl px-5 py-32 sm:px-12">
        <ServiceNode
          label="Search"
          className="top-[10%] left-[5%] max-md:hidden"
        />
        <Section
          eyebrow="Reliability"
          headline="At-least-once delivery, exactly-once processing."
          subhead="transactional outbox + idempotent inbox"
          body="Brokers redeliver; pretending otherwise is the bug. A transactional outbox commits your Postgres domain row and the ReviewCreated message together, so a crash never loses a message. An idempotent inbox dedupes by message id, so the handler runs once even when the broker hands you the same message twice."
        />
        <MotifStage kind="outbox" />
        <p className="text-cc-ink relative z-10 mx-auto mt-4 max-w-2xl text-center text-[0.95rem] leading-relaxed">
          That is exactly-once{" "}
          <span className="text-cc-accent font-medium">processing</span>, not
          exactly-once delivery, with retry, dead-letter routing, and circuit
          breaker as pipeline middleware.
        </p>
      </section>

      {/* Sagas */}
      <section className="relative mx-auto max-w-6xl px-5 py-32 sm:px-12">
        <Section
          eyebrow="Saga"
          headline="A saga carries the workflow across services."
          subhead="Draft → Checked → Published"
          body="The review saga is a C# state machine. ReviewCreated moves it from Draft to Checked, ContentChecked moves it from Checked to Published. At startup Mocha validates the shape, that every state is reachable and every path reaches a final state, so a workflow cannot silently get stuck. Validated before traffic, not at compile time."
        />
        <MotifStage kind="saga" />
      </section>

      {/* Observability */}
      <section className="relative mx-auto max-w-6xl px-5 py-32 sm:px-12">
        <ServiceNode
          label="Catalog"
          className="right-[6%] bottom-[10%] max-md:hidden"
        />
        <Section
          eyebrow="Observability"
          headline="Every hop is a span in Nitro."
          subhead="OpenTelemetry-native"
          body="Decoupled work does not have to be a black box. Mocha is OpenTelemetry-native: every dispatch, transport hop, and handler execution emits spans, and correlation propagates across service boundaries. The same trace shows Send CreateReview, the mediator handler, PublishAsync, the transport hop, and the consumer handler, once telemetry is configured to flow into Nitro."
        />
        <MotifStage kind="tracetap" />
      </section>

      {/* Transports */}
      <section className="relative mx-auto max-w-6xl px-5 py-32 sm:px-12">
        <Section
          eyebrow="Transports"
          headline="Swap transports without touching handlers."
          subhead="RabbitMQ · Postgres · in-process"
          body="Transports are pluggable. Lead with RabbitMQ, Postgres, and in-process, and swap them without changing a handler. Kafka, Azure Service Bus, and Event Hub also exist in source."
        />
        <MotifStage kind="bus" />
        <div className="relative z-10 mt-6 flex flex-wrap items-center justify-center gap-2">
          {TRANSPORTS.map((transport) => (
            <span
              key={transport}
              className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap"
            >
              {transport}
            </span>
          ))}
          <Link
            href="/docs/mocha"
            className="text-cc-accent hover:text-cc-accent-hover ml-1 text-sm font-medium"
          >
            Learn more about Mocha →
          </Link>
        </div>
      </section>

      {/* CTA */}
      <section className="relative mx-auto flex max-w-6xl flex-col items-center px-5 py-40 pb-56 text-center sm:px-12">
        <ServiceNode
          label=""
          className="bottom-[22%] left-1/2 -translate-x-1/2"
          cta
        />
        <div className="relative z-10 mx-auto max-w-2xl">
          <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.22em] uppercase">
            Get started
          </p>
          <h2 className="font-heading text-cc-heading text-h3 mt-4 font-semibold text-balance">
            Keep the workflow moving without losing the thread.
          </h2>
          <p className="text-cc-ink mt-5 text-base text-pretty sm:text-lg">
            Write a handler, attribute it, dispatch it. The source generator
            handles registration and the pipeline; the bus, the outbox, the
            inbox, the sagas, and the traces are part of the framework, not
            packages you wire yourself. Follow every message from publish to
            consume in Nitro.
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
