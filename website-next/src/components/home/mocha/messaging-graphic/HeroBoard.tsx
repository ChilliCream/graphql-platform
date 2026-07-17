"use client";

/**
 * Hero-board comparison for the Mocha messaging page. One full-viewport hero,
 * four candidate board behaviors behind the same headline and copy, so only
 * the board treatment varies:
 *
 *   cascade   services power on one by one, then a CreateReview pulse ripples
 *             service to service across the board and settles into traffic
 *   traced    one labelled CreateReview message sweeps a hero lane through
 *             Dispatch -> Handle -> Publish -> Consume, each stop lighting
 *   hub       a central MOCHA chip with lanes radiating to the services;
 *             messages flow in and out of the chip
 *   topology  a denser always-on constellation with constant coral traffic
 *
 * The board is drawn with the same reveal pipeline as the page background: an
 * offscreen "lit" canvas holds the board, a light mask keeps it only where
 * light falls, and blue node glows and coral message trails draw additively.
 * Unlit areas stay transparent so the site background shows through. Scoped to
 * the hero section (no scroll), the palette is the landing hero gradient:
 * blue #16b9e4 services, coral #f0786a messages.
 */

import { useEffect, useRef } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export type HeroMode = "cascade" | "traced" | "hub" | "topology";

const GRID = 24;
const SUBSTRATE = "#0c1322";
const TRACE_COLOR = "rgba(148, 163, 184, 0.42)";
const TRACE_ALT_COLOR = "rgba(124, 146, 198, 0.34)";
const LANE_COLOR = "rgba(150, 205, 240, 0.7)";
const VIA_COLOR = "rgba(148, 163, 184, 0.55)";
const PAD_COLOR = "rgba(148, 163, 184, 0.1)";
const MONO_FONT = "ui-monospace, SFMono-Regular, Menlo, monospace";
const BLUE = "22, 185, 228";
const CORAL = "240, 120, 106";
const CORAL_SOFT = "253, 178, 168";

const NODE_LIGHT_RADIUS = 172;
const PULSE_LIGHT_RADIUS = 116;
const PULSE_TRAIL = 150;

interface Point {
  readonly x: number;
  readonly y: number;
}

interface Trace {
  readonly pts: Point[];
  readonly cum: number[];
  readonly len: number;
  readonly from: number;
  readonly to: number;
  readonly connector: boolean;
  rev?: Trace;
}

interface Node {
  x: number;
  y: number;
  power: number;
  powerAt: number;
  flash: number;
  hub: boolean;
}

interface Stop {
  x: number;
  y: number;
  at: number;
  label: string;
  flash: number;
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
  dim?: boolean;
  label?: string;
  flashTo?: number;
  taps?: { at: number; stop: Stop; fired: boolean }[];
  remaining?: number[];
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

function traceFrom(
  pts: Point[],
  from: number,
  to: number,
  connector: boolean,
): Trace {
  const cum = [0];
  let len = 0;
  for (let i = 1; i < pts.length; i++) {
    len += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    cum.push(len);
  }
  return { pts, cum, len, from, to, connector };
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
  const segs = 3 + Math.floor(rand() * 5);
  for (let s = 0; s < segs; s++) {
    const diag = d % 2 === 1;
    const cells = diag
      ? 1 + Math.floor(rand() * 3)
      : 2 + Math.floor(rand() * 6);
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
    } else if (rand() > 0.45) {
      d = (d + (rand() < 0.5 ? 1 : 7)) % 8;
    }
  }
  return pts.length >= 2 ? traceFrom(pts, -1, -1, false) : null;
}

function connector(a: Point, b: Point, from: number, to: number): Trace {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const mid =
    Math.abs(dx) >= Math.abs(dy)
      ? { x: b.x - Math.sign(dx) * Math.abs(dy), y: a.y }
      : { x: a.x, y: b.y - Math.sign(dy) * Math.abs(dx) };
  return traceFrom([a, mid, b], from, to, true);
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
    Math.min(1, p.dist / 80) *
    Math.min(1, Math.max(0, (p.trace.len - p.dist) / 130))
  );
}

function HeroCanvas({ mode }: { readonly mode: HeroMode }) {
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

    let nodes: Node[] = [];
    let traces: Trace[] = [];
    let connectors: Trace[] = [];
    let vias: Point[] = [];
    let footprints: {
      x: number;
      y: number;
      w: number;
      h: number;
      label?: string;
    }[] = [];
    let heroLane: Trace | null = null;
    let stops: Stop[] = [];
    let hubIndex = -1;
    let pulses: Pulse[] = [];

    let w = 0;
    let h = 0;
    let dpr = 1;
    let raf = 0;
    let last = 0;
    let elapsed = 0;
    let spawnClock = 0;
    let driveClock = 0;
    let driveBeat = 0;
    let cascadePhase = 0;
    let cascadeFired = false;
    let disposed = false;

    function size() {
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      const r = root!.getBoundingClientRect();
      w = r.width;
      h = r.height;
      for (const c of [canvas!, lit, mask]) {
        c.width = Math.round(w * dpr);
        c.height = Math.round(h * dpr);
      }
    }

    function build() {
      size();
      const rand = mulberry32(97 + Math.floor(w) * 7 + Math.floor(h));
      const rootRect = root!.getBoundingClientRect();
      // The chips are siblings of this canvas wrapper, so search the section.
      const scope = root!.parentElement ?? root!;
      const chips = Array.from(
        scope.querySelectorAll<HTMLElement>("[data-hero-node]"),
      );
      nodes = chips.map((chip) => {
        const cr = chip.getBoundingClientRect();
        return {
          x: cr.left - rootRect.left + cr.width / 2,
          y: cr.top - rootRect.top + cr.height / 2,
          power: reduced ? 1 : 0,
          powerAt: 0,
          flash: 0,
          hub: chip.dataset.heroHub !== undefined,
        };
      });
      hubIndex = nodes.findIndex((n) => n.hub);

      traces = [];
      connectors = [];
      vias = [];
      footprints = [];
      heroLane = null;
      stops = [];
      pulses = [];

      // Radial traces from every node (fewer around the hub, it gets lanes).
      nodes.forEach((node) => {
        if (node.hub) {
          return;
        }
        const count = 5 + Math.floor(rand() * 3);
        for (let i = 0; i < count; i++) {
          const dir = Math.floor(rand() * 8);
          const start = {
            x: node.x + DIR_VECS[dir].x * GRID * 0.5,
            y: node.y + DIR_VECS[dir].y * GRID * 0.5,
          };
          const t = walk(rand, start, dir, w, h);
          if (t) {
            traces.push(t);
            vias.push(t.pts[t.pts.length - 1]);
          }
        }
      });

      if (mode === "hub" && hubIndex >= 0) {
        // Lanes radiating from the MOCHA chip to every service.
        footprints.push({
          x: nodes[hubIndex].x - 34,
          y: nodes[hubIndex].y - 22,
          w: 68,
          h: 44,
          label: "MOCHA",
        });
        nodes.forEach((node, i) => {
          if (i === hubIndex) {
            return;
          }
          const lane = connector(nodes[hubIndex], node, hubIndex, i);
          const back = traceFrom([...lane.pts].reverse(), i, hubIndex, true);
          lane.rev = back;
          back.rev = lane;
          traces.push(lane);
          connectors.push(lane, back);
        });
      } else {
        // Lanes between each node and its two nearest neighbors.
        for (let i = 0; i < nodes.length; i++) {
          if (nodes[i].hub) {
            continue;
          }
          const others = nodes
            .map((n, j) => ({
              j,
              d: Math.hypot(n.x - nodes[i].x, n.y - nodes[i].y),
            }))
            .filter((o) => o.j > i && !nodes[o.j].hub)
            .sort((a, b) => a.d - b.d)
            .slice(0, 2);
          for (const o of others) {
            const lane = connector(nodes[i], nodes[o.j], i, o.j);
            const back = traceFrom([...lane.pts].reverse(), o.j, i, true);
            lane.rev = back;
            back.rev = lane;
            traces.push(lane);
            connectors.push(lane, back);
          }
        }
      }

      if (mode === "traced") {
        // A hero lane in the lower third with four named stops.
        const y0 = h * 0.78;
        const x0 = w * 0.12;
        const x1 = w * 0.88;
        const jog = 26;
        const pts: Point[] = [
          { x: x0, y: y0 },
          { x: x0 + (x1 - x0) * 0.3, y: y0 },
          { x: x0 + (x1 - x0) * 0.36, y: y0 - jog },
          { x: x0 + (x1 - x0) * 0.62, y: y0 - jog },
          { x: x0 + (x1 - x0) * 0.68, y: y0 },
          { x: x1, y: y0 },
        ];
        heroLane = traceFrom(pts, -1, -1, true);
        const labels = ["DISPATCH", "HANDLE", "PUBLISH", "CONSUME"];
        [0.06, 0.4, 0.62, 0.97].forEach((f, i) => {
          const at = heroLane!.len * f;
          const p = pointAt(heroLane!, at);
          stops.push({ x: p.x, y: p.y, at, label: labels[i], flash: 0 });
        });
      }

      // Ambient traces, sparser than the full page.
      const ambient = Math.min(320, Math.floor((w * h) / 12000));
      for (let i = 0; i < ambient; i++) {
        const start = {
          x: GRID * (1 + Math.floor(rand() * (w / GRID - 2))),
          y: GRID * (1 + Math.floor(rand() * (h / GRID - 2))),
        };
        const t = walk(rand, start, Math.floor(rand() * 8), w, h);
        if (t) {
          traces.push(t);
          vias.push(t.pts[0], t.pts[t.pts.length - 1]);
        }
      }

      // Stagger node power-on for the cascade; others are lit immediately.
      if (mode === "cascade" && !reduced) {
        const order = nodes
          .map((n, i) => ({ i, x: n.x }))
          .sort((a, b) => a.x - b.x);
        order.forEach((o, k) => {
          nodes[o.i].power = 0;
          nodes[o.i].powerAt = 300 + k * 260;
        });
      } else if (!reduced) {
        nodes.forEach((n) => {
          n.power = 0;
          n.powerAt = 0;
        });
      }
      cascadePhase = 0;
      cascadeFired = false;
    }

    function emit(p: Pulse) {
      if (pulses.length < 36) {
        pulses.push(p);
      }
    }

    function spawnConnector(fromNode: number) {
      const lanes = connectors.filter((t) => t.from === fromNode && t.len > 80);
      if (lanes.length === 0) {
        return;
      }
      const t = lanes[Math.floor(Math.random() * lanes.length)];
      emit({
        trace: t,
        dist: 0,
        speed: 260 + Math.random() * 150,
        flashTo: t.to,
      });
    }

    function drawBoard() {
      litCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      litCtx!.globalCompositeOperation = "source-over";
      litCtx!.clearRect(0, 0, w, h);
      litCtx!.fillStyle = SUBSTRATE;
      litCtx!.fillRect(0, 0, w, h);

      litCtx!.fillStyle = PAD_COLOR;
      const pitch = GRID * 2;
      for (let y = 0; y <= h; y += pitch) {
        for (let x = 0; x <= w; x += pitch) {
          litCtx!.fillRect(x - 0.75, y - 0.75, 1.5, 1.5);
        }
      }

      litCtx!.lineCap = "round";
      litCtx!.lineJoin = "round";
      for (let i = 0; i < traces.length; i++) {
        const t = traces[i];
        if (t.connector) {
          litCtx!.lineWidth = 2.2;
          litCtx!.strokeStyle = LANE_COLOR;
        } else {
          litCtx!.lineWidth = 1.4;
          litCtx!.strokeStyle = i % 6 === 0 ? TRACE_ALT_COLOR : TRACE_COLOR;
        }
        litCtx!.beginPath();
        litCtx!.moveTo(t.pts[0].x, t.pts[0].y);
        for (let p = 1; p < t.pts.length; p++) {
          litCtx!.lineTo(t.pts[p].x, t.pts[p].y);
        }
        litCtx!.stroke();
      }
      if (heroLane) {
        litCtx!.lineWidth = 2.4;
        litCtx!.strokeStyle = LANE_COLOR;
        litCtx!.beginPath();
        litCtx!.moveTo(heroLane.pts[0].x, heroLane.pts[0].y);
        for (let p = 1; p < heroLane.pts.length; p++) {
          litCtx!.lineTo(heroLane.pts[p].x, heroLane.pts[p].y);
        }
        litCtx!.stroke();
      }

      for (const f of footprints) {
        litCtx!.fillStyle = "#0a1120";
        litCtx!.strokeStyle = "rgba(150, 205, 240, 0.55)";
        litCtx!.lineWidth = 1.3;
        litCtx!.beginPath();
        litCtx!.roundRect(f.x, f.y, f.w, f.h, 4);
        litCtx!.fill();
        litCtx!.stroke();
        litCtx!.fillStyle = "rgba(150, 205, 240, 0.4)";
        for (let px = f.x + 8; px <= f.x + f.w - 8; px += 10) {
          litCtx!.fillRect(px, f.y - 4, 3, 4);
          litCtx!.fillRect(px, f.y + f.h, 3, 4);
        }
      }

      litCtx!.strokeStyle = VIA_COLOR;
      litCtx!.lineWidth = 1.2;
      litCtx!.fillStyle = SUBSTRATE;
      for (const via of vias) {
        litCtx!.beginPath();
        litCtx!.arc(via.x, via.y, 2.4, 0, Math.PI * 2);
        litCtx!.fill();
        litCtx!.stroke();
      }
    }

    function nodeLevel(n: Node, time: number, i: number): number {
      const flicker =
        0.86 +
        0.08 * Math.sin(time / 690 + i * 1.7) +
        0.06 * Math.sin(time / 251 + i * 3.1);
      return flicker * n.power * (1 + 0.8 * n.flash) * (n.hub ? 1.15 : 1);
    }

    function drawMask(time: number) {
      maskCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      maskCtx!.clearRect(0, 0, w, h);

      // A soft central lamp so the board reads faintly behind the copy.
      const cl = maskCtx!.createRadialGradient(
        w / 2,
        h * 0.5,
        0,
        w / 2,
        h * 0.5,
        Math.max(w, h) * 0.55,
      );
      cl.addColorStop(0, "rgba(255,255,255,0.16)");
      cl.addColorStop(1, "rgba(255,255,255,0)");
      maskCtx!.fillStyle = cl;
      maskCtx!.fillRect(0, 0, w, h);

      nodes.forEach((n, i) => {
        const level = nodeLevel(n, time, i);
        if (level <= 0.01) {
          return;
        }
        const radius =
          NODE_LIGHT_RADIUS * (n.hub ? 1.15 : 1) * (1 + 0.18 * n.flash);
        const p1 = time * 0.00037 + i * 2.4;
        const p2 = time * 0.00029 + i * 1.3;
        const blobs = [
          { dx: 0, dy: 0, r: radius, a: 0.85 },
          {
            dx: Math.cos(p1) * 28,
            dy: Math.sin(p2) * 24,
            r: radius * 0.6,
            a: 0.5,
          },
        ];
        for (const b of blobs) {
          const bx = n.x + b.dx;
          const by = n.y + b.dy;
          const alpha = Math.min(1, b.a * level);
          const g = maskCtx!.createRadialGradient(bx, by, 0, bx, by, b.r);
          g.addColorStop(0, `rgba(255,255,255,${alpha})`);
          g.addColorStop(0.55, `rgba(255,255,255,${alpha * 0.45})`);
          g.addColorStop(1, "rgba(255,255,255,0)");
          maskCtx!.fillStyle = g;
          maskCtx!.beginPath();
          maskCtx!.arc(bx, by, b.r, 0, Math.PI * 2);
          maskCtx!.fill();
        }
      });

      for (const pulse of pulses) {
        const alpha = envelope(pulse) * (pulse.dim ? 0.55 : 1);
        if (alpha <= 0) {
          continue;
        }
        for (let k = 0; k < 4; k++) {
          const d = pulse.dist - (k * PULSE_TRAIL) / 3;
          if (d < 0) {
            break;
          }
          const p = pointAt(pulse.trace, d);
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

    function drawGlows(time: number) {
      ctx!.save();
      ctx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx!.globalCompositeOperation = "lighter";

      nodes.forEach((n, i) => {
        const level = nodeLevel(n, time, i);
        if (level <= 0.01) {
          return;
        }
        const haloR = NODE_LIGHT_RADIUS * (n.hub ? 1.05 : 0.9);
        const halo = ctx!.createRadialGradient(n.x, n.y, 0, n.x, n.y, haloR);
        halo.addColorStop(0, `rgba(${BLUE},${(n.hub ? 0.2 : 0.14) * level})`);
        halo.addColorStop(0.45, `rgba(${BLUE},${0.05 * level})`);
        halo.addColorStop(1, `rgba(${BLUE},0)`);
        ctx!.fillStyle = halo;
        ctx!.beginPath();
        ctx!.arc(n.x, n.y, haloR, 0, Math.PI * 2);
        ctx!.fill();
        if (n.flash > 0.02) {
          const ringR = 14 + (1 - n.flash) * 48;
          ctx!.strokeStyle = `rgba(${CORAL},${0.55 * n.flash})`;
          ctx!.lineWidth = 1.7;
          ctx!.beginPath();
          ctx!.arc(n.x, n.y, ringR, 0, Math.PI * 2);
          ctx!.stroke();
        }
      });

      // Hero-structure emphasis: hero lane, stops, and the MOCHA chip label.
      if (heroLane) {
        ctx!.lineCap = "round";
        ctx!.lineJoin = "round";
        ctx!.lineWidth = 2.4;
        ctx!.strokeStyle = "rgba(150, 205, 240, 0.8)";
        ctx!.beginPath();
        ctx!.moveTo(heroLane.pts[0].x, heroLane.pts[0].y);
        for (let p = 1; p < heroLane.pts.length; p++) {
          ctx!.lineTo(heroLane.pts[p].x, heroLane.pts[p].y);
        }
        ctx!.stroke();
        ctx!.font = `600 10px ${MONO_FONT}`;
        for (const s of stops) {
          ctx!.fillStyle = `rgba(150, 205, 240, ${0.5 + 0.5 * Math.min(1, s.flash + 0.001)})`;
          ctx!.beginPath();
          ctx!.arc(s.x, s.y, 3, 0, Math.PI * 2);
          ctx!.fill();
          ctx!.fillStyle = "rgba(178, 210, 242, 0.9)";
          ctx!.fillText(s.label, s.x - 20, s.y + 22);
          if (s.flash > 0.02) {
            const ringR = 8 + (1 - s.flash) * 26;
            ctx!.strokeStyle = `rgba(${CORAL},${0.65 * s.flash})`;
            ctx!.lineWidth = 1.8;
            ctx!.beginPath();
            ctx!.arc(s.x, s.y, ringR, 0, Math.PI * 2);
            ctx!.stroke();
          }
        }
      }
      // Hub emphasis: light the lanes radiating from the MOCHA chip so the
      // hub-and-spokes structure reads even between pulses.
      if (hubIndex >= 0) {
        ctx!.lineCap = "round";
        ctx!.lineJoin = "round";
        ctx!.lineWidth = 2.6;
        ctx!.strokeStyle = "rgba(150, 205, 240, 0.8)";
        for (const t of connectors) {
          if (t.from !== hubIndex) {
            continue;
          }
          ctx!.beginPath();
          ctx!.moveTo(t.pts[0].x, t.pts[0].y);
          for (let p = 1; p < t.pts.length; p++) {
            ctx!.lineTo(t.pts[p].x, t.pts[p].y);
          }
          ctx!.stroke();
        }
        // A soft blue pool so the chip and its spokes stay legible.
        const hn = nodes[hubIndex];
        const pool = ctx!.createRadialGradient(hn.x, hn.y, 0, hn.x, hn.y, 150);
        pool.addColorStop(0, `rgba(${BLUE},0.16)`);
        pool.addColorStop(1, `rgba(${BLUE},0)`);
        ctx!.fillStyle = pool;
        ctx!.beginPath();
        ctx!.arc(hn.x, hn.y, 150, 0, Math.PI * 2);
        ctx!.fill();
      }
      for (const f of footprints) {
        if (!f.label) {
          continue;
        }
        ctx!.font = `600 11px ${MONO_FONT}`;
        ctx!.fillStyle = "rgba(190, 220, 245, 0.95)";
        ctx!.textAlign = "center";
        ctx!.fillText(f.label, f.x + f.w / 2, f.y + f.h + 16);
        ctx!.textAlign = "start";
      }

      ctx!.lineCap = "round";
      for (const pulse of pulses) {
        const alpha = envelope(pulse) * (pulse.dim ? 0.5 : 1);
        if (alpha <= 0) {
          continue;
        }
        const head = pointAt(pulse.trace, pulse.dist);
        const chunks = 8;
        ctx!.lineWidth = 2.4;
        let prev = pointAt(pulse.trace, Math.max(0, pulse.dist - PULSE_TRAIL));
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
        ctx!.arc(head.x, head.y, 2.6, 0, Math.PI * 2);
        ctx!.fill();
        if (pulse.label) {
          ctx!.font = `600 11px ${MONO_FONT}`;
          ctx!.fillStyle = `rgba(255, 220, 210, ${alpha})`;
          ctx!.fillText(pulse.label, head.x + 12, head.y - 10);
        }
      }
      ctx!.restore();
    }

    function render(time: number) {
      drawBoard();
      drawMask(time);
      litCtx!.setTransform(1, 0, 0, 1, 0, 0);
      litCtx!.globalCompositeOperation = "destination-in";
      litCtx!.drawImage(mask, 0, 0);
      litCtx!.globalCompositeOperation = "source-over";
      ctx!.setTransform(1, 0, 0, 1, 0, 0);
      ctx!.globalCompositeOperation = "source-over";
      ctx!.clearRect(0, 0, canvas!.width, canvas!.height);
      ctx!.drawImage(lit, 0, 0);
      drawGlows(time);
    }

    function stepPulses(dt: number) {
      const finished: Pulse[] = [];
      for (const p of pulses) {
        p.dist += p.speed * dt;
        if (p.taps) {
          for (const tap of p.taps) {
            if (!tap.fired && p.dist >= tap.at) {
              tap.fired = true;
              tap.stop.flash = 1;
            }
          }
        }
        if (p.dist >= p.trace.len) {
          finished.push(p);
        }
      }
      pulses = pulses.filter((p) => p.dist < p.trace.len);
      for (const p of finished) {
        if (p.flashTo !== undefined && p.flashTo >= 0) {
          nodes[p.flashTo].flash = 1;
        }
        if (p.remaining && p.remaining.length >= 2) {
          const [a, b, ...rest] = p.remaining;
          const lane = connectors.find((t) => t.from === a && t.to === b);
          if (lane) {
            emit({
              trace: lane,
              dist: 0,
              speed: p.speed,
              flashTo: b,
              remaining: [b, ...rest],
            });
          }
        }
      }
    }

    function drive(dt: number) {
      if (reduced) {
        return;
      }
      driveClock += dt * 1000;
      if (mode === "topology") {
        if (driveClock >= 460) {
          driveClock = 0;
          if (Math.random() < 0.85) {
            spawnConnector(Math.floor(Math.random() * nodes.length));
          }
        }
      } else if (mode === "hub" && hubIndex >= 0) {
        if (driveClock >= 640) {
          driveClock = 0;
          driveBeat++;
          if (driveBeat % 4 === 0) {
            // Publish: hub to three services at once.
            connectors
              .filter((t) => t.from === hubIndex)
              .slice(0, 3)
              .forEach((t) =>
                emit({ trace: t, dist: 0, speed: 300, flashTo: t.to }),
              );
          } else if (driveBeat % 2 === 0) {
            spawnConnector(hubIndex);
          } else {
            // A service sends into the hub.
            const svc = connectors.filter((t) => t.to === hubIndex);
            if (svc.length) {
              const t = svc[Math.floor(Math.random() * svc.length)];
              emit({ trace: t, dist: 0, speed: 300, flashTo: hubIndex });
            }
          }
        }
      } else if (mode === "traced" && heroLane) {
        if (driveClock >= 3000 || (elapsed > 500 && driveBeat === 0)) {
          driveClock = 0;
          driveBeat++;
          for (const s of stops) {
            s.flash = 0;
          }
          emit({
            trace: heroLane,
            dist: 0,
            speed: 240,
            label: "CreateReview",
            taps: stops.map((s) => ({ at: s.at, stop: s, fired: false })),
          });
        }
      } else if (mode === "cascade") {
        if (cascadePhase === 0) {
          const allOn = nodes.every((n) => n.power > 0.85);
          if ((allOn && elapsed > 400) || elapsed > nodes.length * 260 + 900) {
            cascadePhase = 1;
          }
        } else if (cascadePhase === 1 && !cascadeFired) {
          cascadeFired = true;
          // Nearest-neighbor hop order across the services, as a lit chain.
          const order = [0];
          const used = new Set([0]);
          while (order.length < nodes.length) {
            const cur = order[order.length - 1];
            let best = -1;
            let bestD = Infinity;
            for (let j = 0; j < nodes.length; j++) {
              if (used.has(j)) {
                continue;
              }
              const d = Math.hypot(
                nodes[j].x - nodes[cur].x,
                nodes[j].y - nodes[cur].y,
              );
              if (d < bestD) {
                bestD = d;
                best = j;
              }
            }
            if (best < 0) {
              break;
            }
            used.add(best);
            order.push(best);
          }
          const chain: number[] = [];
          for (let i = 0; i < order.length - 1; i++) {
            chain.push(order[i], order[i + 1]);
          }
          const lane = connectors.find(
            (t) => t.from === chain[0] && t.to === chain[1],
          );
          if (lane) {
            nodes[0].flash = 1;
            emit({
              trace: lane,
              dist: 0,
              speed: 300,
              label: "CreateReview",
              flashTo: chain[1],
              remaining: chain,
            });
          }
          cascadePhase = 2;
          driveClock = 0;
        } else if (cascadePhase === 2) {
          if (driveClock >= 500) {
            driveClock = 0;
            if (Math.random() < 0.6) {
              spawnConnector(Math.floor(Math.random() * nodes.length));
            }
          }
          if (elapsed - 0 > 0 && pulses.length === 0 && driveBeat === 0) {
            driveBeat = 0;
          }
        }
      }

      // Ambient background traffic for the non-traced modes.
      if (mode !== "traced") {
        spawnClock += dt * 1000;
        if (spawnClock >= 900) {
          spawnClock = 0;
          if (pulses.length < 8 && Math.random() < 0.4) {
            spawnConnector(Math.floor(Math.random() * nodes.length));
          }
        }
      }
    }

    function stepNodes(dt: number) {
      nodes.forEach((n) => {
        if (n.power < 1 && elapsed >= n.powerAt) {
          n.power = Math.min(1, n.power + dt / 0.7);
        }
        n.flash = Math.max(0, n.flash - dt / 0.6);
      });
      for (const s of stops) {
        s.flash = Math.max(0, s.flash - dt / 0.7);
      }
    }

    function loop(time: number) {
      if (disposed) {
        return;
      }
      const dt = last > 0 ? Math.min((time - last) / 1000, 0.05) : 0;
      last = time;
      elapsed += dt * 1000;
      stepNodes(dt);
      drive(dt);
      stepPulses(dt);
      render(time);
      raf = requestAnimationFrame(loop);
    }

    build();
    if (reduced) {
      render(0);
    } else {
      raf = requestAnimationFrame(loop);
    }

    let resizeRaf = 0;
    const ro = new ResizeObserver(() => {
      cancelAnimationFrame(resizeRaf);
      resizeRaf = requestAnimationFrame(() => {
        build();
        if (reduced) {
          render(0);
        }
      });
    });
    ro.observe(root);

    return () => {
      disposed = true;
      if (raf) {
        cancelAnimationFrame(raf);
      }
      cancelAnimationFrame(resizeRaf);
      ro.disconnect();
    };
  }, [mode]);

  return (
    <div
      ref={rootRef}
      className="absolute inset-0 overflow-hidden"
      aria-hidden="true"
    >
      <canvas
        ref={canvasRef}
        className="pointer-events-none absolute inset-0 h-full w-full"
      />
    </div>
  );
}

interface ChipProps {
  readonly label: string;
  readonly className?: string;
  readonly hub?: boolean;
}

function ServiceChip({ label, className, hub }: ChipProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-2 ${className ?? ""}`}
    >
      <span
        data-hero-node
        data-hero-hub={hub ? "" : undefined}
        className="relative block rounded-[4px] border bg-[#052430]"
        style={{
          width: hub ? "1.4rem" : "1rem",
          height: hub ? "1.4rem" : "1rem",
          borderColor: "rgba(22,185,228,0.85)",
          boxShadow: hub
            ? "0 0 26px 7px rgba(22,185,228,0.4)"
            : "0 0 18px 4px rgba(22,185,228,0.25)",
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

const CHIPS: Record<HeroMode, ReadonlyArray<ChipProps>> = {
  cascade: [
    { label: "Ordering", className: "top-[15%] left-[10%]" },
    { label: "Billing", className: "top-[20%] right-[9%]" },
    { label: "Reviews", className: "top-[46%] left-[6%]" },
    { label: "Payments", className: "bottom-[16%] left-[16%]" },
    { label: "Shipping", className: "bottom-[20%] right-[12%]" },
    { label: "Inventory", className: "top-[52%] right-[6%]" },
  ],
  traced: [
    { label: "Ordering", className: "top-[16%] left-[9%]" },
    { label: "Billing", className: "top-[18%] right-[10%]" },
  ],
  hub: [
    { label: "", className: "top-[81%] left-1/2 -translate-x-1/2", hub: true },
    { label: "Ordering", className: "top-[15%] left-[15%]" },
    { label: "Billing", className: "top-[15%] right-[15%]" },
    { label: "Payments", className: "top-[44%] left-[7%]" },
    { label: "Shipping", className: "top-[44%] right-[7%]" },
  ],
  topology: [
    { label: "Ordering", className: "top-[13%] left-[9%]" },
    { label: "Reviews", className: "top-[12%] left-[38%]" },
    { label: "Billing", className: "top-[16%] right-[10%]" },
    { label: "Inventory", className: "top-[48%] left-[5%]" },
    { label: "Catalog", className: "top-[52%] right-[6%]" },
    { label: "Payments", className: "bottom-[14%] left-[18%]" },
    {
      label: "Notifications",
      className: "bottom-[11%] left-1/2 -translate-x-1/2",
    },
    { label: "Shipping", className: "bottom-[18%] right-[14%]" },
  ],
};

export function HeroBoard({ mode }: { readonly mode: HeroMode }) {
  return (
    <section className="relative flex min-h-svh flex-col items-center justify-center overflow-hidden px-5 py-24 text-center">
      <HeroCanvas mode={mode} />
      {CHIPS[mode].map((chip, i) => (
        <ServiceChip key={`${chip.label}-${i}`} {...chip} />
      ))}
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
  );
}
