"use client";

/**
 * Mocha messaging, version 11: "Aligned board".
 *
 * The same board-and-traffic system as v10 (service chips light a generated
 * circuit board, pulses travel the lanes, concept motifs are printed onto the
 * board, silkscreen keep-outs under the service names), restyled to sit
 * inside the site's normal visual language instead of a pure-black world:
 *
 * - The page keeps the regular site background; the canvas only adds the lit
 *   board on top of it, so unlit areas match every other page.
 * - The palette is the site palette: teal (cc-accent) lanes, pulses, chips,
 *   and highlights over steel traces, no yellow special world.
 * - Sections and cards use the standard tokens (cc-card-bg, cc-card-border,
 *   cc-surface) exactly like the production pages.
 */

import Link from "next/link";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Board geometry. Traces walk on this grid with 45 degree bends, PCB style.
const GRID = 24;
const SUBSTRATE = "#0c1322";
const TRACE_COLOR = "rgba(148, 163, 184, 0.42)";
const TRACE_ALT_COLOR = "rgba(94, 234, 212, 0.3)";
const LANE_COLOR = "rgba(94, 234, 212, 0.55)";
const VIA_COLOR = "rgba(148, 163, 184, 0.55)";
const PAD_COLOR = "rgba(148, 163, 184, 0.12)";
const SILK_COLOR = "rgba(148, 163, 184, 0.45)";
const SILK_EM_COLOR = "rgba(94, 234, 212, 0.75)";
const HATCH_COLOR = "rgba(148, 163, 184, 0.08)";
const MONO_FONT = "ui-monospace, SFMono-Regular, Menlo, monospace";

// Light sizes in CSS pixels.
const NODE_LIGHT_RADIUS = 170;
const MOTIF_LIGHT_RADIUS = 160;
const PULSE_LIGHT_RADIUS = 110;
const PULSE_TRAIL = 150;

const MAX_PULSES = 12;
const SPAWN_INTERVAL_MS = 420;
const SPAWN_CHANCE = 0.6;

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
  /** Node index the trace leaves from, or -1 for ambient traces. */
  readonly from: number;
  /** Node index a connector arrives at, or -1. */
  readonly to: number;
  readonly kind: TraceKind;
  /** The same connector in the opposite direction, for replies. */
  rev?: Trace;
}

interface Silkscreen {
  readonly x: number;
  readonly y: number;
  readonly text: string;
  /** Emphasized silkscreen: the concept labels printed larger and brighter. */
  readonly em?: boolean;
}

interface Footprint {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

interface HatchPatch {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

type MotifKind = "pubsub" | "send" | "reqreply" | "outbox" | "bus";

interface MotifAnchorPoint {
  readonly kind: MotifKind;
  readonly x: number;
  readonly y: number;
}

/** A faint fixed lamp that keeps a concept motif readable. */
interface MotifLight {
  readonly x: number;
  readonly y: number;
  readonly r: number;
}

interface Board {
  readonly traces: Trace[];
  /** Traces a pulse can leave node i on, including reversed connectors. */
  readonly outgoing: Trace[][];
  readonly vias: Point[];
  readonly nodes: Point[];
  readonly silks: Silkscreen[];
  readonly footprints: Footprint[];
  readonly hatches: HatchPatch[];
  readonly motifLights: MotifLight[];
  /** Trace indices bucketed by BIN_H rows of their bounding box. */
  readonly bins: number[][];
  readonly width: number;
  readonly height: number;
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
  /** Replies render dimmer. */
  dim?: boolean;
  /** Spawn a dim reply on the reversed lane when this pulse arrives. */
  wantReply?: boolean;
}

const BIN_H = 300;

// Eight compass directions as grid steps; odd indices are the diagonals.
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

/** Deterministic PRNG so the board is stable for a given page size. */
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

/** Sampled segment-rectangle test; 8px steps are enough at trace scale. */
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

/**
 * Walk a PCB-style trace: long axis-aligned runs, short 45 degree jogs, never
 * a reversal. Segments never enter a keep-out (label or motif area); the walk
 * stops at the boundary instead. Returns null when nothing could be drawn.
 */
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
      // Snap back to an axis so diagonals stay short corner cuts.
      d = (d + (rand() < 0.5 ? 1 : 7)) % 8;
    } else if (rand() > 0.45) {
      d = (d + (rand() < 0.5 ? 1 : 7)) % 8;
    }
  }
  return pts.length >= 2
    ? traceFromPoints(pts, from, -1, from >= 0 ? "radial" : "ambient")
    : null;
}

/**
 * Route between two nodes: one long axis run plus a 45 degree jog. Both
 * corner variants are tried so the lane avoids the service-name keep-outs
 * when it can.
 */
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

/** Bags the generator fills while laying down a concept motif. */
interface MotifOutput {
  traces: Trace[];
  vias: Point[];
  silks: Silkscreen[];
  footprints: Footprint[];
  lights: MotifLight[];
  keepOuts: Rect[];
}

/**
 * Print one page concept onto the board: a small engineering structure with
 * an emphasized silkscreen title and its own faint lamp.
 */
function buildMotif(anchor: MotifAnchorPoint, out: MotifOutput) {
  const { kind, x, y } = anchor;
  const lane = (pts: Point[]) => traceFromPoints(pts, -1, -1, "connector");
  const wire = (pts: Point[]) => traceFromPoints(pts, -1, -1, "ambient");

  if (kind === "pubsub") {
    out.traces.push(
      wire([
        { x: x - 120, y },
        { x: x - 4, y },
      ]),
    );
    out.traces.push(
      wire([
        { x, y },
        { x: x + 34, y },
        { x: x + 82, y: y - 48 },
      ]),
    );
    out.traces.push(
      wire([
        { x, y },
        { x: x + 110, y },
      ]),
    );
    out.traces.push(
      wire([
        { x, y },
        { x: x + 34, y },
        { x: x + 82, y: y + 48 },
      ]),
    );
    out.vias.push(
      { x, y },
      { x: x - 120, y },
      { x: x + 82, y: y - 48 },
      { x: x + 110, y },
      { x: x + 82, y: y + 48 },
    );
    out.silks.push({ x: x - 28, y: y - 22, text: "PUB / SUB", em: true });
    out.lights.push({ x, y, r: MOTIF_LIGHT_RADIUS });
    out.keepOuts.push({ x: x - 36, y: y - 36, w: 90, h: 22 });
  } else if (kind === "send") {
    out.traces.push(
      wire([
        { x: x - 110, y },
        { x: x + 60, y },
        { x: x + 96, y: y + 36 },
      ]),
    );
    out.vias.push({ x: x - 110, y }, { x: x + 96, y: y + 36 });
    out.silks.push({ x: x - 110, y: y - 14, text: "SEND", em: true });
    out.lights.push({ x, y, r: MOTIF_LIGHT_RADIUS * 0.9 });
    out.keepOuts.push({ x: x - 118, y: y - 28, w: 60, h: 22 });
  } else if (kind === "reqreply") {
    out.footprints.push({ x: x - 104, y: y - 8, w: 20, h: 16 });
    out.footprints.push({ x: x + 84, y: y - 8, w: 20, h: 16 });
    out.traces.push(
      wire([
        { x: x - 84, y: y - 4 },
        { x: x + 84, y: y - 4 },
      ]),
    );
    out.traces.push(
      wire([
        { x: x - 84, y: y + 4 },
        { x: x + 84, y: y + 4 },
      ]),
    );
    out.silks.push({ x: x - 36, y: y - 18, text: "REQ / REPLY", em: true });
    out.lights.push({ x, y, r: MOTIF_LIGHT_RADIUS });
    out.keepOuts.push({ x: x - 44, y: y - 32, w: 106, h: 22 });
  } else if (kind === "outbox") {
    out.footprints.push({ x: x - 140, y: y - 16, w: 52, h: 32 });
    out.footprints.push({ x: x + 88, y: y - 16, w: 52, h: 32 });
    out.traces.push(
      lane([
        { x: x - 88, y },
        { x: x + 88, y },
      ]),
    );
    out.silks.push({ x: x - 136, y: y + 34, text: "OUTBOX", em: true });
    out.silks.push({ x: x + 94, y: y + 34, text: "INBOX", em: true });
    out.lights.push({ x, y, r: MOTIF_LIGHT_RADIUS * 1.1 });
    out.keepOuts.push({ x: x - 148, y: y + 22, w: 74, h: 20 });
    out.keepOuts.push({ x: x + 86, y: y + 22, w: 64, h: 20 });
  } else {
    // bus: three parallel transport lanes, each with its silkscreen name.
    const names = ["RABBITMQ", "POSTGRES", "IN-PROCESS"];
    names.forEach((name, i) => {
      const ly = y + (i - 1) * 26;
      out.traces.push(
        lane([
          { x: x - 220, y: ly },
          { x: x + 220, y: ly },
        ]),
      );
      out.vias.push({ x: x - 220, y: ly }, { x: x + 220, y: ly });
      out.silks.push({ x: x - 212, y: ly - 7, text: name, em: true });
      out.keepOuts.push({ x: x - 220, y: ly - 20, w: 110, h: 15 });
    });
    out.lights.push({ x: x - 110, y, r: MOTIF_LIGHT_RADIUS * 1.2 });
    out.lights.push({ x: x + 110, y, r: MOTIF_LIGHT_RADIUS * 1.2 });
  }
}

function generateBoard(
  nodes: Point[],
  labelKeepOuts: Rect[],
  motifs: MotifAnchorPoint[],
  width: number,
  height: number,
): Board {
  const rand = mulberry32(1337 + width * 31 + Math.floor(height));
  const traces: Trace[] = [];
  const vias: Point[] = [];
  const silks: Silkscreen[] = [];
  const footprints: Footprint[] = [];
  const outgoing: Trace[][] = nodes.map(() => []);

  // Concept motifs first: their structures and keep-outs must exist before
  // anything random is routed around them.
  const motifOut: MotifOutput = {
    traces: [],
    vias: [],
    silks: [],
    footprints: [],
    lights: [],
    keepOuts: [],
  };
  for (const anchor of motifs) {
    buildMotif(anchor, motifOut);
  }
  const keepOuts = [...labelKeepOuts, ...motifOut.keepOuts];
  traces.push(...motifOut.traces);
  vias.push(...motifOut.vias);
  silks.push(...motifOut.silks);
  footprints.push(...motifOut.footprints);

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
        traces.push(trace);
        outgoing[n].push(trace);
        vias.push(trace.pts[trace.pts.length - 1]);
      }
    }
  }

  // A lane between each node and its two nearest neighbors, so pulses can
  // visibly travel service to service. The reversed twin carries replies.
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
      traces.push(laneTrace);
      outgoing[i].push(laneTrace);
      outgoing[o.j].push(back);
    }
  }

  // Ambient traces everywhere else, only ever seen when light passes by.
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
      traces.push(trace);
      vias.push(trace.pts[0]);
      vias.push(trace.pts[trace.pts.length - 1]);
    }
  }

  // PCB furniture, visible only when lit: hatched ground-plane patches, IC
  // footprints, and silkscreen part codes. All of it respects the keep-outs.
  const hatches: HatchPatch[] = [];
  const hatchCount = Math.min(24, Math.floor((width * height) / 150000));
  for (let i = 0; i < hatchCount; i++) {
    const patch = {
      x: GRID * Math.floor(rand() * (width / GRID - 8)),
      y: GRID * Math.floor(rand() * (height / GRID - 8)),
      w: GRID * (3 + Math.floor(rand() * 5)),
      h: GRID * (2 + Math.floor(rand() * 4)),
    };
    if (
      !keepOuts.some(
        (r) =>
          patch.x < r.x + r.w &&
          patch.x + patch.w > r.x &&
          patch.y < r.y + r.h &&
          patch.y + patch.h > r.y,
      )
    ) {
      hatches.push(patch);
    }
  }

  const footCount = Math.min(40, Math.floor((width * height) / 90000));
  for (let i = 0; i < footCount; i++) {
    const foot = {
      x: GRID * Math.floor(rand() * (width / GRID - 5)),
      y: GRID * Math.floor(rand() * (height / GRID - 4)),
      w: GRID * (2 + Math.floor(rand() * 3)),
      h: GRID * (1 + Math.floor(rand() * 2)),
    };
    if (
      !keepOuts.some(
        (r) =>
          foot.x < r.x + r.w &&
          foot.x + foot.w > r.x &&
          foot.y < r.y + r.h &&
          foot.y + foot.h > r.y,
      )
    ) {
      footprints.push(foot);
    }
  }

  const silkPool = ["U", "R", "C", "MSG-", "NET-", "TP"];
  const silkCount = Math.min(60, Math.floor((width * height) / 60000));
  for (let i = 0; i < silkCount; i++) {
    const prefix = silkPool[Math.floor(rand() * silkPool.length)];
    const silk = {
      x: GRID * (1 + Math.floor(rand() * (width / GRID - 3))),
      y: GRID * (1 + Math.floor(rand() * (height / GRID - 3))),
      text: `${prefix}${1 + Math.floor(rand() * 89)}`,
    };
    if (!keepOuts.some((r) => pointInRect(silk.x, silk.y, r))) {
      silks.push(silk);
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
    outgoing,
    vias,
    nodes,
    silks,
    footprints,
    hatches,
    motifLights: motifOut.lights,
    bins,
    width,
    height,
  };
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

/** Fade the pulse light in as it leaves the node and out as it ends. */
function pulseEnvelope(pulse: Pulse): number {
  return (
    Math.min(1, pulse.dist / 90) *
    Math.min(1, Math.max(0, (pulse.trace.len - pulse.dist) / 140))
  );
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
    // Per-node dynamics: scroll power-on, arrival flash, hover boost.
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
    let spawnClock = 0;
    let fanoutClock = 0;
    let nextFanoutAt = 4500;
    let replyClock = 0;
    let nextReplyAt = 6200;
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

    function spawnFrom(nodeIndex: number, opts?: { wantReply?: boolean }) {
      if (!board || pulses.length >= MAX_PULSES + 4) {
        return;
      }
      const lanes = board.outgoing[nodeIndex] ?? [];
      if (lanes.length === 0) {
        return;
      }
      const connectors = lanes.filter(
        (t) => t.kind === "connector" && t.len > 120,
      );
      const pool =
        connectors.length > 0 && Math.random() < 0.7
          ? connectors
          : lanes.filter((t) => t.len > 120);
      if (pool.length === 0) {
        return;
      }
      const trace = pool[Math.floor(Math.random() * pool.length)];
      pulses.push({
        trace,
        dist: 0,
        speed: 260 + Math.random() * 180,
        wantReply: opts?.wantReply && trace.kind === "connector",
      });
    }

    /** One publish, several subscribers: pulses on every lane at once. */
    function fanout(nodeIndex: number) {
      if (!board) {
        return;
      }
      const lanes = (board.outgoing[nodeIndex] ?? []).filter(
        (t) => t.kind === "connector" && t.len > 120,
      );
      const branches = lanes.slice(0, 3);
      for (const trace of branches) {
        pulses.push({ trace, dist: 0, speed: 280 + Math.random() * 120 });
      }
      if (branches.length === 0) {
        spawnFrom(nodeIndex);
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
            spawnFrom(i);
            spawnFrom(i);
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
      // The chips live in the sibling content sections, so search from the
      // shared parent, not from this background wrapper.
      const scope = root!.parentElement ?? root!;
      const chips = Array.from(
        scope.querySelectorAll<HTMLElement>("[data-mg11-node]"),
      );
      const scrollY = window.scrollY;
      const nodes: Point[] = [];
      const labelKeepOuts: Rect[] = [];
      for (const chip of chips) {
        const r = chip.getBoundingClientRect();
        nodes.push({
          x: r.left + r.width / 2,
          y: r.top + r.height / 2 + scrollY,
        });
        // The service name below the chip is silkscreen: keep the board
        // empty underneath it, like text on a real PCB.
        const label = chip.nextElementSibling;
        if (label) {
          const lr = label.getBoundingClientRect();
          labelKeepOuts.push({
            x: lr.left - 8,
            y: lr.top + scrollY - 6,
            w: lr.width + 16,
            h: lr.height + 12,
          });
        }
      }
      const motifs: MotifAnchorPoint[] = Array.from(
        scope.querySelectorAll<HTMLElement>("[data-mg11-motif]"),
      )
        .filter((el) => el.offsetParent !== null)
        .map((el) => {
          const r = el.getBoundingClientRect();
          return {
            kind: el.dataset.mg11Motif as MotifKind,
            x: r.left + r.width / 2,
            y: r.top + r.height / 2 + scrollY,
          };
        });
      board = generateBoard(
        nodes,
        labelKeepOuts,
        motifs,
        root!.clientWidth,
        root!.scrollHeight,
      );
      pulses = [];
      power = nodes.map((_, i) => (reducedMotion ? 1 : (power[i] ?? 0)));
      flash = nodes.map(() => 0);
      hover = nodes.map(() => 0);
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

      // Substrate and pad grid: the surface the light reveals.
      litCtx!.fillStyle = SUBSTRATE;
      litCtx!.fillRect(0, top, viewW, viewH);
      litCtx!.fillStyle = PAD_COLOR;
      const pitch = GRID * 2;
      const firstRow = Math.floor(top / pitch) * pitch;
      for (let y = firstRow; y <= bottom; y += pitch) {
        for (let x = 0; x <= viewW; x += pitch) {
          litCtx!.fillRect(x - 0.75, y - 0.75, 1.5, 1.5);
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
        for (let x = patch.x - patch.h; x <= patch.x + patch.w; x += 8) {
          litCtx!.moveTo(x, patch.y + patch.h);
          litCtx!.lineTo(x + patch.h, patch.y);
        }
        litCtx!.stroke();
        litCtx!.restore();
        litCtx!.strokeStyle = "rgba(148, 163, 184, 0.12)";
        litCtx!.strokeRect(patch.x, patch.y, patch.w, patch.h);
      }

      // Traces in the visible bins. Service lanes draw wider and brighter.
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

      // IC footprints with pin stubs along the long sides.
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

      // Vias: small annular rings at trace ends.
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

      // Silkscreen, on top like a real board. Concept labels draw larger.
      for (const silk of board.silks) {
        if (silk.y < top - 14 || silk.y > bottom + 14) {
          continue;
        }
        litCtx!.font = silk.em ? `10px ${MONO_FONT}` : `9px ${MONO_FONT}`;
        litCtx!.fillStyle = silk.em ? SILK_EM_COLOR : SILK_COLOR;
        litCtx!.fillText(silk.text, silk.x, silk.y);
      }
    }

    /** Combined per-node light level: flicker, power-on, hover, flash. */
    function nodeLevel(i: number, time: number): number {
      const flickerBase =
        0.86 +
        0.08 * Math.sin(time / 690 + i * 1.7) +
        0.06 * Math.sin(time / 251 + i * 3.1);
      return flickerBase * power[i] * (1 + 0.35 * hover[i] + 0.8 * flash[i]);
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
        // Three drifting blobs instead of one circle, so the pool of light
        // has an irregular, slowly moving edge.
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

      // Faint fixed lamps over the concept motifs.
      for (let i = 0; i < board.motifLights.length; i++) {
        const lamp = board.motifLights[i];
        if (lamp.y < top - lamp.r || lamp.y > bottom + lamp.r) {
          continue;
        }
        const level = 0.5 * (0.92 + 0.08 * Math.sin(time / 840 + i * 2.1));
        const g = maskCtx!.createRadialGradient(
          lamp.x,
          lamp.y,
          0,
          lamp.x,
          lamp.y,
          lamp.r,
        );
        g.addColorStop(0, `rgba(255,255,255,${level})`);
        g.addColorStop(0.6, `rgba(255,255,255,${level * 0.4})`);
        g.addColorStop(1, "rgba(255,255,255,0)");
        maskCtx!.fillStyle = g;
        maskCtx!.beginPath();
        maskCtx!.arc(lamp.x, lamp.y, lamp.r, 0, Math.PI * 2);
        maskCtx!.fill();
      }

      for (const pulse of pulses) {
        const alpha = pulseEnvelope(pulse) * (pulse.dim ? 0.55 : 1);
        if (alpha <= 0) {
          continue;
        }
        // Light at the head, plus a decaying afterglow along the trail.
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

      // A soft accent glow around each node, matching the site palette.
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
        halo.addColorStop(0, `rgba(94,234,212,${0.12 * level})`);
        halo.addColorStop(0.45, `rgba(94,234,212,${0.05 * level})`);
        halo.addColorStop(1, "rgba(94,234,212,0)");
        ctx!.fillStyle = halo;
        ctx!.beginPath();
        ctx!.arc(node.x, node.y, haloR, 0, Math.PI * 2);
        ctx!.fill();

        const core = ctx!.createRadialGradient(
          node.x,
          node.y,
          0,
          node.x,
          node.y,
          60,
        );
        core.addColorStop(0, `rgba(255,255,255,${0.06 * level})`);
        core.addColorStop(1, "rgba(255,255,255,0)");
        ctx!.fillStyle = core;
        ctx!.beginPath();
        ctx!.arc(node.x, node.y, 60, 0, Math.PI * 2);
        ctx!.fill();

        // Delivery blip: an expanding ring while the arrival flash decays.
        if (flash[i] > 0.02) {
          const ringR = 14 + (1 - flash[i]) * 46;
          ctx!.strokeStyle = `rgba(94,234,212,${0.5 * flash[i]})`;
          ctx!.lineWidth = 1.6;
          ctx!.beginPath();
          ctx!.arc(node.x, node.y, ringR, 0, Math.PI * 2);
          ctx!.stroke();
        }
      }

      // Pulse trails and heads.
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
          ctx!.lineWidth = 2.2;
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
            ctx!.strokeStyle = `rgba(94,234,212,${alpha * Math.pow(k / chunks, 2) * 0.8})`;
            ctx!.beginPath();
            ctx!.moveTo(prev.x, prev.y);
            ctx!.lineTo(p.x, p.y);
            ctx!.stroke();
            prev = p;
          }
          const headR = pulse.dim ? 18 : 26;
          const g = ctx!.createRadialGradient(
            head.x,
            head.y,
            0,
            head.x,
            head.y,
            headR,
          );
          g.addColorStop(0, `rgba(153,246,228,${alpha * 0.5})`);
          g.addColorStop(1, "rgba(153,246,228,0)");
          ctx!.fillStyle = g;
          ctx!.beginPath();
          ctx!.arc(head.x, head.y, headR, 0, Math.PI * 2);
          ctx!.fill();
          ctx!.fillStyle = `rgba(240,253,250,${alpha})`;
          ctx!.beginPath();
          ctx!.arc(head.x, head.y, 2.4, 0, Math.PI * 2);
          ctx!.fill();
        }
      }
      ctx!.restore();
    }

    function renderFrame(time: number) {
      const scrollY = window.scrollY;
      drawBoardSlice(scrollY);
      drawLightMask(scrollY, time);

      // Keep the board only where light falls.
      litCtx!.setTransform(1, 0, 0, 1, 0, 0);
      litCtx!.globalCompositeOperation = "destination-in";
      litCtx!.drawImage(maskCanvas, 0, 0);
      litCtx!.globalCompositeOperation = "source-over";

      // Unlit areas stay transparent so the regular page background shows.
      ctx!.setTransform(1, 0, 0, 1, 0, 0);
      ctx!.globalCompositeOperation = "source-over";
      ctx!.clearRect(0, 0, canvas!.width, canvas!.height);
      ctx!.drawImage(litCanvas, 0, 0);

      drawGlows(scrollY, time);
    }

    function stepNodes(dt: number, scrollY: number) {
      if (!board) {
        return;
      }
      const bottom = scrollY + viewH;
      for (let i = 0; i < board.nodes.length; i++) {
        const y = board.nodes[i].y;
        // Power on the first time the node enters the viewport, then stay on.
        if (y > scrollY - 60 && y < bottom + 60) {
          power[i] = Math.min(1, power[i] + dt / 0.9);
        }
        flash[i] = Math.max(0, flash[i] - dt / 0.6);
        const hoverTarget = i === hoveredIndex ? 1 : 0;
        hover[i] += (hoverTarget - hover[i]) * Math.min(1, dt / 0.15);
      }
    }

    function stepPulses(dt: number) {
      if (!board) {
        return;
      }
      const finished: Pulse[] = [];
      for (const pulse of pulses) {
        pulse.dist += pulse.speed * dt;
        if (pulse.dist >= pulse.trace.len) {
          finished.push(pulse);
        }
      }
      pulses = pulses.filter((p) => p.dist < p.trace.len);
      for (const pulse of finished) {
        // A delivery: flash the receiving service and send the reply.
        if (pulse.trace.to >= 0) {
          flash[pulse.trace.to] = 1;
        }
        if (pulse.wantReply && pulse.trace.rev) {
          pulses.push({
            trace: pulse.trace.rev,
            dist: 0,
            speed: pulse.speed * 0.85,
            dim: true,
          });
        }
      }

      // The scripted opening beat: one deliberate fan-out from the first
      // hero service before ambient traffic starts.
      if (!openingDone && elapsed > 600) {
        openingDone = true;
        fanout(0);
        return;
      }

      spawnClock += dt * 1000;
      if (spawnClock >= SPAWN_INTERVAL_MS) {
        spawnClock = 0;
        if (pulses.length < MAX_PULSES && Math.random() < SPAWN_CHANCE) {
          spawnFrom(Math.floor(Math.random() * board.nodes.length));
        }
      }

      fanoutClock += dt * 1000;
      if (fanoutClock >= nextFanoutAt) {
        fanoutClock = 0;
        nextFanoutAt = 4000 + Math.random() * 3000;
        fanout(Math.floor(Math.random() * board.nodes.length));
      }

      replyClock += dt * 1000;
      if (replyClock >= nextReplyAt) {
        replyClock = 0;
        nextReplyAt = 5000 + Math.random() * 3500;
        spawnFrom(Math.floor(Math.random() * board.nodes.length), {
          wantReply: true,
        });
      }
    }

    function loop(time: number) {
      if (disposed) {
        return;
      }
      const dt = lastTime > 0 ? Math.min((time - lastTime) / 1000, 0.05) : 0;
      lastTime = time;
      elapsed += dt * 1000;
      stepNodes(dt, window.scrollY);
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

    measureAndBuild();
    if (reducedMotion) {
      renderFrame(0);
    } else {
      startLoop();
    }

    const onScroll = () => {
      if (reducedMotion) {
        renderFrame(0);
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
          renderFrame(0);
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
}

/**
 * An accent-colored chip with its service name, one light source on the
 * board. Hovering the chip brightens its light and emits a burst of pulses.
 */
function ServiceNode({ label, className }: ServiceNodeProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-2 ${className ?? ""}`}
    >
      <span
        data-mg11-node
        className="border-cc-accent/80 pointer-events-auto relative block h-4 w-4 cursor-default rounded-[4px] border bg-[#06231f] shadow-[0_0_18px_4px_rgba(94,234,212,0.25)] transition-shadow duration-300 hover:shadow-[0_0_28px_8px_rgba(94,234,212,0.45)]"
      >
        <span className="bg-cc-accent absolute inset-[4px] rounded-[2px] shadow-[0_0_7px_1px_rgba(153,246,228,0.9)]" />
      </span>
      <span className="text-cc-accent/90 font-mono text-[0.6rem] tracking-[0.22em] uppercase [text-shadow:0_0_14px_rgba(94,234,212,0.4)]">
        {label}
      </span>
    </div>
  );
}

/** Invisible anchor: the canvas prints the named concept motif here. */
function MotifAnchor({
  kind,
  className,
}: {
  readonly kind: MotifKind;
  readonly className?: string;
}) {
  return (
    <div
      data-mg11-motif={kind}
      aria-hidden="true"
      className={`absolute h-px w-px ${className ?? ""}`}
    />
  );
}

const RANGES = [
  {
    scope: "In process",
    name: "Mediator",
    copy: "A request maps to one typed handler. Behaviors around it take care of validation, caching, and telemetry.",
  },
  {
    scope: "Across services",
    name: "Bus",
    copy: "Publish an event once. Every service that subscribes reacts on its own schedule. The transport underneath is pluggable.",
  },
  {
    scope: "Over time",
    name: "Sagas",
    copy: "Long-running work keeps its state, advances as events arrive, and compensates when a step fails.",
  },
] as const;

const STEPS = [
  {
    label: "command dispatched",
    title: "An action becomes a command.",
    copy: "A CreateReview command is dispatched through the in-process mediator. Commands, queries, and notifications are plain typed messages, so the request can return while the work it started keeps moving.",
  },
  {
    label: "handler runs",
    title: "A handler picks it up.",
    copy: "A source generator finds the matching handler at compile time and emits typed registration plus a pre-compiled pipeline. Dispatch is zero-reflection, with retry, circuit breaker, and concurrency limits as middleware.",
  },
  {
    label: "event published",
    title: "An event goes onto the bus.",
    copy: "The handler publishes ReviewCreated. The same model spans in-process notifications and the cross-service bus, so other services subscribe without coupling.",
  },
  {
    label: "saga advances",
    title: "A saga carries the workflow.",
    copy: "A saga is a C# state machine that advances across services as events arrive. Mocha validates that every state is reachable and every path reaches a final state before the service handles traffic.",
  },
] as const;

const PATTERNS = [
  {
    name: "Event",
    question: "Who needs to know?",
    method: "PublishAsync",
    copy: "One event, any number of handlers.",
  },
  {
    name: "Send",
    question: "Who should act?",
    method: "SendAsync",
    copy: "Hand work off without waiting.",
  },
  {
    name: "Request-Reply",
    question: "What is the result?",
    method: "RequestAsync",
    copy: "Send a message and wait for the answer.",
  },
] as const;

const OBSERVABILITY_POINTS = [
  "Sagas are validated before the service handles traffic, so a stuck workflow does not reach production.",
  "Pluggable transports: RabbitMQ, Postgres, and in-process, swappable without touching your handlers.",
  "Handler-first and explicit: you declare IEventHandler<T>, ICommand<T>, and IQuery<TResponse>; Mocha builds the endpoints, pipelines, and consumers around them.",
] as const;

const TRANSPORTS = ["RabbitMQ", "Postgres", "in-process"] as const;

export function MessagingGraphicV11() {
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
          <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
            Under the request and response, an app is a set of parts reacting to
            each other. Mocha is the messaging that carries those events: an
            in-process mediator, a bus across services, and sagas for the work
            that takes time.
          </p>
          <div className="mt-9 flex flex-wrap justify-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
          </div>
        </div>
      </section>

      {/* Three ranges */}
      <section className="relative mx-auto max-w-6xl px-5 py-36 sm:px-12">
        <ServiceNode
          label="Inventory"
          className="top-[8%] right-[4%] max-md:hidden"
        />
        <ServiceNode
          label="Notifications"
          className="bottom-[6%] left-[2%] max-md:hidden"
        />

        <div className="relative z-10">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold">
            One model, three ranges.
          </h2>
          <p className="text-cc-ink mt-4 max-w-2xl text-base sm:text-lg">
            The same message model covers a method call, a hop between services,
            and a process that runs for days.
          </p>
          <div className="mt-12 grid gap-5 md:grid-cols-3">
            {RANGES.map((range) => (
              <div
                key={range.name}
                className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm"
              >
                <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.2em] uppercase">
                  {range.scope}
                </p>
                <p className="text-cc-heading font-heading text-h5 mt-3 font-semibold">
                  {range.name}
                </p>
                <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
                  {range.copy}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How a workflow moves */}
      <section className="relative mx-auto max-w-6xl px-5 py-36 sm:px-12">
        <ServiceNode
          label="Reviews"
          className="top-[10%] right-[6%] max-md:hidden"
        />
        <ServiceNode
          label="Warehouse"
          className="bottom-[8%] left-[4%] max-md:hidden"
        />

        <div className="relative z-10">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold">
            How a workflow moves.
          </h2>
          <p className="text-cc-ink mt-4 max-w-2xl text-base sm:text-lg">
            From the first request to the last event, one flow: a command, a
            handler, an event, a saga.
          </p>
          <div className="mt-12 grid gap-5 md:grid-cols-2">
            {STEPS.map((step, i) => (
              <div
                key={step.label}
                className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm"
              >
                <div className="flex items-baseline justify-between gap-3">
                  <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.2em] uppercase">
                    {step.label}
                  </p>
                  <span className="text-cc-ink-faint font-mono text-sm tabular-nums">
                    0{i + 1}
                  </span>
                </div>
                <p className="text-cc-heading font-heading text-h5 mt-3 font-semibold">
                  {step.title}
                </p>
                <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
                  {step.copy}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Patterns */}
      <section className="relative mx-auto max-w-6xl px-5 py-36 sm:px-12">
        <ServiceNode
          label="Identity"
          className="top-[6%] right-[32%] max-md:hidden"
        />
        <ServiceNode label="Analytics" className="bottom-[2%] left-[42%]" />
        <MotifAnchor
          kind="pubsub"
          className="top-[42%] left-[9%] max-lg:hidden"
        />
        <MotifAnchor
          kind="send"
          className="top-[16%] right-[9%] max-lg:hidden"
        />
        <MotifAnchor
          kind="reqreply"
          className="right-[10%] bottom-[10%] max-lg:hidden"
        />

        <div className="relative z-10">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold">
            The patterns are built in.
          </h2>
          <p className="text-cc-ink mt-4 max-w-2xl text-base sm:text-lg">
            The same handler-first API covers pub/sub, fire-and-forget, and
            request/reply, used independently or together.
          </p>
          <div className="mt-12 grid gap-5 md:grid-cols-3">
            {PATTERNS.map((pattern) => (
              <div
                key={pattern.name}
                className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm"
              >
                <p className="text-cc-heading font-heading text-[1.02rem] font-semibold">
                  {pattern.name}
                </p>
                <p className="text-cc-ink-dim mt-1 text-sm">
                  {pattern.question}
                </p>
                <p className="text-cc-ink mt-3 text-[0.92rem] leading-relaxed">
                  {pattern.copy}
                </p>
                <span className="border-cc-accent/60 text-cc-accent bg-cc-surface mt-4 inline-block rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem]">
                  {pattern.method}
                </span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Reliability */}
      <section className="relative mx-auto max-w-6xl px-5 py-36 sm:px-12">
        <ServiceNode
          label="Search"
          className="top-[12%] left-[6%] max-md:hidden"
        />
        <MotifAnchor
          kind="outbox"
          className="right-[16%] bottom-[8%] max-lg:hidden"
        />

        <div className="relative z-10">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold">
            Reliable by default.
          </h2>
          <p className="text-cc-ink mt-4 max-w-2xl text-base sm:text-lg">
            Delivery is where messaging systems quietly fail. Mocha makes the
            failure modes explicit and handles them for you.
          </p>
          <div className="mt-12 grid gap-5 md:grid-cols-2">
            <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm">
              <p className="text-cc-heading font-heading text-h5 font-semibold">
                Transactional outbox
              </p>
              <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
                The database write and the message dispatch succeed or fail
                together.
              </p>
            </div>
            <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm">
              <p className="text-cc-heading font-heading text-h5 font-semibold">
                Idempotent inbox
              </p>
              <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
                Duplicate messages are deduplicated so each is processed once.
              </p>
            </div>
          </div>
          <p className="text-cc-ink mt-8 max-w-2xl text-[0.95rem] leading-relaxed">
            Together they give{" "}
            <span className="text-cc-accent font-medium">
              effectively exactly-once
            </span>{" "}
            processing. Per-exception retry and redelivery, dead-letter routing,
            circuit breaker, concurrency limiter, and scheduled or delayed
            delivery all ship as pipeline middleware.
          </p>
        </div>
      </section>

      {/* Observability */}
      <section className="relative mx-auto max-w-6xl px-5 py-36 sm:px-12">
        <ServiceNode
          label="Catalog"
          className="right-[6%] bottom-[10%] max-md:hidden"
        />

        <div className="relative z-10 max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold">
            Decoupled, but not a black box.
          </h2>
          <p className="text-cc-ink mt-4 text-base sm:text-lg">
            Most messaging frameworks hand you decoupling and then lose the
            thread: the request returns, work fans out, and you cannot say where
            it went. Mocha is OpenTelemetry-native. Every dispatch, receive, and
            handler execution emits structured traces and metrics, and you can
            follow a message from publish to consume as real spans in Nitro.
          </p>
          <ul className="mt-8 space-y-3">
            {OBSERVABILITY_POINTS.map((point) => (
              <li key={point} className="flex items-start gap-3">
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon />
                </span>
                <span className="text-cc-ink text-[0.95rem] leading-relaxed">
                  {point}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* Transports */}
      <section className="relative mx-auto max-w-6xl px-5 pt-24 pb-64 sm:px-12">
        <MotifAnchor
          kind="bus"
          className="bottom-[15%] left-1/2 max-lg:hidden"
        />

        <div className="border-cc-card-border bg-cc-card-bg/60 relative z-10 mx-auto max-w-3xl rounded-2xl border p-6 text-center backdrop-blur-sm sm:p-8">
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.15em] uppercase">
            transports and patterns
          </p>
          <div className="mt-4 flex flex-wrap items-center justify-center gap-2">
            {TRANSPORTS.map((transport) => (
              <span
                key={transport}
                className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap"
              >
                {transport}
              </span>
            ))}
          </div>
          <p className="text-cc-ink-dim mt-5 text-sm">
            Open and modern, built on the Enterprise Integration Patterns
            catalog. Kafka, Azure Service Bus, and Event Hub also exist in
            source.{" "}
            <Link
              href="/docs/mocha"
              className="text-cc-accent hover:text-cc-accent-hover font-medium"
            >
              Learn more about Mocha
            </Link>
            .
          </p>
        </div>
      </section>

      {/* CTA */}
      <section className="relative mx-auto max-w-6xl px-5 py-36 pb-52 text-center sm:px-12">
        <div className="relative z-10">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold text-balance">
            Keep the workflow moving without losing the thread.
          </h2>
          <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base sm:text-lg">
            Ship event-driven work that continues after the request and stays
            visible end to end.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
          </div>
        </div>
      </section>
    </div>
  );
}
