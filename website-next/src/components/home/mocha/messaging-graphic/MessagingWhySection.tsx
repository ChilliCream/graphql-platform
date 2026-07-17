"use client";

/**
 * "Why Mocha" section: copy on the left, a circuit-board messaging network
 * bleeding across the dark background on the right. Keeps the PCB idea, the
 * routed lanes, and the reveal-by-light, but in a restrained, premium palette:
 * a deep near-black board, muted slate hairline traces, soft light around the
 * service nodes, refined ring-and-dot markers instead of glowing chips, and a
 * single muted coral for the messages travelling the lanes. Unboxed, full
 * bleed, in the site's own dark navy and type.
 */

import { useEffect, useRef } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const GRID = 22;
const MONO_FONT = "ui-monospace, SFMono-Regular, Menlo, monospace";

const SUBSTRATE = "#090d15";
const PAD_COLOR = "rgba(150, 166, 194, 0.06)";
const TRACE_COLOR = "rgba(140, 154, 180, 0.26)";
const TRACE_ALT_COLOR = "rgba(140, 154, 180, 0.17)";
const LANE_COLOR = "rgba(174, 190, 216, 0.5)";
const LANE_ALWAYS = "rgba(162, 182, 210, 0.2)";
const VIA_COLOR = "rgba(150, 166, 194, 0.38)";
const NODE_HALO = "150, 176, 208";
const MSG = "224, 140, 122";
const MSG_SOFT = "246, 202, 190";
const RING_COLOR = "rgba(205, 216, 232, 0.42)";
const DOT_COLOR = "rgba(232, 238, 248, 0.9)";
const LABEL_COLOR = "rgba(150, 166, 194, 0.6)";

const NODE_LIGHT_RADIUS = 138;
const PULSE_LIGHT_RADIUS = 96;
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
}

interface NodeDef {
  readonly fx: number;
  readonly fy: number;
  readonly label: string;
}

interface Node {
  x: number;
  y: number;
  power: number;
  powerAt: number;
  flash: number;
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
  to: number;
}

// Fractions of the section, concentrated right so the copy stays clear.
const NODES: readonly NodeDef[] = [
  { fx: 0.61, fy: 0.31, label: "Ordering" },
  { fx: 0.84, fy: 0.24, label: "Billing" },
  { fx: 0.73, fy: 0.57, label: "Payments" },
  { fx: 0.93, fy: 0.52, label: "Shipping" },
  { fx: 0.81, fy: 0.8, label: "Reviews" },
  { fx: 0.53, fy: 0.72, label: "Inventory" },
];

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

function traceFrom(pts: Point[], to: number, connector: boolean): Trace {
  const cum = [0];
  let len = 0;
  for (let i = 1; i < pts.length; i++) {
    len += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    cum.push(len);
  }
  return { pts, cum, len, to, connector };
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

function CircuitBoard() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
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

    let w = 0;
    let h = 0;
    let dpr = 1;
    let nodes: Node[] = [];
    let traces: Trace[] = [];
    let connectors: Trace[] = [];
    let vias: Point[] = [];
    let pulses: Pulse[] = [];
    let raf = 0;
    let last = 0;
    let elapsed = 0;
    let spawnClock = 0;
    let disposed = false;

    function build() {
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      const r = canvas!.getBoundingClientRect();
      w = r.width;
      h = r.height;
      if (w < 2 || h < 2) {
        return;
      }
      for (const c of [canvas!, lit, mask]) {
        c.width = Math.round(w * dpr);
        c.height = Math.round(h * dpr);
      }
      const rand = mulberry32(31 + Math.floor(w) * 7 + Math.floor(h));
      nodes = NODES.map((n, i) => ({
        x: n.fx * w,
        y: n.fy * h,
        power: reduced ? 1 : 0,
        powerAt: 150 + i * 170,
        flash: 0,
      }));

      traces = [];
      connectors = [];
      vias = [];
      pulses = [];

      for (const node of nodes) {
        const count = 4 + Math.floor(rand() * 2);
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
      }

      for (let i = 0; i < nodes.length; i++) {
        const others = nodes
          .map((n, j) => ({
            j,
            d: Math.hypot(n.x - nodes[i].x, n.y - nodes[i].y),
          }))
          .filter((o) => o.j > i)
          .sort((a, b) => a.d - b.d)
          .slice(0, 2);
        for (const o of others) {
          // One lane geometry per pair; the reverse rides the same points so
          // every message travels the line that is actually drawn.
          const lane = connector(nodes[i], nodes[o.j], o.j);
          const back = traceFrom([...lane.pts].reverse(), i, true);
          connectors.push(lane, back);
          traces.push(lane);
        }
      }

      const ambient = Math.min(150, Math.floor((w * h) / 16000));
      for (let i = 0; i < ambient; i++) {
        const start = {
          x: GRID * (1 + Math.floor(rand() * (w / GRID - 2))),
          y: GRID * (1 + Math.floor(rand() * (h / GRID - 2))),
        };
        const t = walk(rand, start, Math.floor(rand() * 8), w, h);
        if (t) {
          traces.push(t);
          vias.push(t.pts[t.pts.length - 1]);
        }
      }
    }

    function spawn() {
      const lanes = connectors.filter((t) => t.len > 60);
      if (lanes.length === 0 || pulses.length >= 4) {
        return;
      }
      const t = lanes[Math.floor(Math.random() * lanes.length)];
      emit(t);
    }

    function emit(trace: Trace) {
      pulses.push({
        trace,
        dist: 0,
        speed: 170 + Math.random() * 90,
        to: trace.to,
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
          litCtx!.fillRect(x - 0.6, y - 0.6, 1.2, 1.2);
        }
      }
      litCtx!.lineCap = "round";
      litCtx!.lineJoin = "round";
      for (let i = 0; i < traces.length; i++) {
        const t = traces[i];
        if (t.connector) {
          litCtx!.lineWidth = 1.8;
          litCtx!.strokeStyle = LANE_COLOR;
        } else {
          litCtx!.lineWidth = 1.1;
          litCtx!.strokeStyle = i % 5 === 0 ? TRACE_ALT_COLOR : TRACE_COLOR;
        }
        litCtx!.beginPath();
        litCtx!.moveTo(t.pts[0].x, t.pts[0].y);
        for (let p = 1; p < t.pts.length; p++) {
          litCtx!.lineTo(t.pts[p].x, t.pts[p].y);
        }
        litCtx!.stroke();
      }
      litCtx!.strokeStyle = VIA_COLOR;
      litCtx!.lineWidth = 1;
      litCtx!.fillStyle = SUBSTRATE;
      for (const via of vias) {
        litCtx!.beginPath();
        litCtx!.arc(via.x, via.y, 2, 0, Math.PI * 2);
        litCtx!.fill();
        litCtx!.stroke();
      }
    }

    function nodeLevel(n: Node, time: number, i: number): number {
      const flicker = 0.9 + 0.06 * Math.sin(time / 720 + i * 1.7);
      return flicker * n.power * (1 + 0.5 * n.flash);
    }

    function drawMask(time: number) {
      maskCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      maskCtx!.clearRect(0, 0, w, h);
      nodes.forEach((n, i) => {
        const level = nodeLevel(n, time, i);
        if (level <= 0.01) {
          return;
        }
        const radius = NODE_LIGHT_RADIUS * (1 + 0.12 * n.flash);
        const g = maskCtx!.createRadialGradient(n.x, n.y, 0, n.x, n.y, radius);
        g.addColorStop(0, `rgba(255,255,255,${0.72 * level})`);
        g.addColorStop(0.5, `rgba(255,255,255,${0.28 * level})`);
        g.addColorStop(1, "rgba(255,255,255,0)");
        maskCtx!.fillStyle = g;
        maskCtx!.beginPath();
        maskCtx!.arc(n.x, n.y, radius, 0, Math.PI * 2);
        maskCtx!.fill();
      });
      for (const pulse of pulses) {
        const alpha = envelope(pulse);
        if (alpha <= 0) {
          continue;
        }
        for (let k = 0; k < 3; k++) {
          const d = pulse.dist - (k * PULSE_TRAIL) / 2.5;
          if (d < 0) {
            break;
          }
          const p = pointAt(pulse.trace, d);
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

    function drawGlows(time: number) {
      ctx!.save();
      ctx!.setTransform(dpr, 0, 0, dpr, 0, 0);

      // Faint always-on lanes between services, so the connections stay
      // readable between the lit pools (brighter where the light falls).
      ctx!.globalCompositeOperation = "source-over";
      ctx!.lineCap = "round";
      ctx!.lineJoin = "round";
      ctx!.lineWidth = 1;
      ctx!.strokeStyle = LANE_ALWAYS;
      for (const t of traces) {
        if (!t.connector) {
          continue;
        }
        ctx!.beginPath();
        ctx!.moveTo(t.pts[0].x, t.pts[0].y);
        for (let p = 1; p < t.pts.length; p++) {
          ctx!.lineTo(t.pts[p].x, t.pts[p].y);
        }
        ctx!.stroke();
      }

      // Soft muted-steel halos around the nodes (additive, low).
      ctx!.globalCompositeOperation = "lighter";
      nodes.forEach((n, i) => {
        const level = nodeLevel(n, time, i);
        if (level <= 0.01) {
          return;
        }
        const haloR = NODE_LIGHT_RADIUS * 0.85;
        const halo = ctx!.createRadialGradient(n.x, n.y, 0, n.x, n.y, haloR);
        halo.addColorStop(0, `rgba(${NODE_HALO},${0.09 * level})`);
        halo.addColorStop(1, `rgba(${NODE_HALO},0)`);
        ctx!.fillStyle = halo;
        ctx!.beginPath();
        ctx!.arc(n.x, n.y, haloR, 0, Math.PI * 2);
        ctx!.fill();
      });

      // Coral message heads and trails (additive, restrained).
      ctx!.lineCap = "round";
      for (const pulse of pulses) {
        const alpha = envelope(pulse);
        if (alpha <= 0) {
          continue;
        }
        const head = pointAt(pulse.trace, pulse.dist);
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
        const g = ctx!.createRadialGradient(
          head.x,
          head.y,
          0,
          head.x,
          head.y,
          13,
        );
        g.addColorStop(0, `rgba(${MSG_SOFT},${alpha * 0.42})`);
        g.addColorStop(1, `rgba(${MSG_SOFT},0)`);
        ctx!.fillStyle = g;
        ctx!.beginPath();
        ctx!.arc(head.x, head.y, 13, 0, Math.PI * 2);
        ctx!.fill();
        ctx!.fillStyle = `rgba(248,238,234,${alpha})`;
        ctx!.beginPath();
        ctx!.arc(head.x, head.y, 1.8, 0, Math.PI * 2);
        ctx!.fill();
      }

      // Refined node markers: a thin ring, a centre dot, a muted label.
      ctx!.globalCompositeOperation = "source-over";
      ctx!.textAlign = "center";
      ctx!.font = `600 9.5px ${MONO_FONT}`;
      nodes.forEach((n, i) => {
        const level = Math.min(1, n.power);
        if (level <= 0.02) {
          return;
        }
        ctx!.globalAlpha = level;
        ctx!.strokeStyle = RING_COLOR;
        ctx!.lineWidth = 1;
        ctx!.beginPath();
        ctx!.arc(n.x, n.y, 5, 0, Math.PI * 2);
        ctx!.stroke();
        if (n.flash > 0.02) {
          ctx!.strokeStyle = `rgba(${MSG},${0.45 * n.flash})`;
          ctx!.beginPath();
          ctx!.arc(n.x, n.y, 5 + (1 - n.flash) * 11, 0, Math.PI * 2);
          ctx!.stroke();
        }
        ctx!.fillStyle = DOT_COLOR;
        ctx!.beginPath();
        ctx!.arc(n.x, n.y, 1.7 + 0.5 * n.flash, 0, Math.PI * 2);
        ctx!.fill();
        ctx!.fillStyle = LABEL_COLOR;
        ctx!.fillText(NODES[i].label.toUpperCase(), n.x, n.y + 18);
        ctx!.globalAlpha = 1;
      });
      ctx!.textAlign = "start";
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

    function step(dt: number) {
      nodes.forEach((n) => {
        if (n.power < 1 && elapsed >= n.powerAt) {
          n.power = Math.min(1, n.power + dt / 0.8);
        }
        n.flash = Math.max(0, n.flash - dt / 0.7);
      });
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
          nodes[f.to].flash = 1;
        }
      }
      spawnClock += dt * 1000;
      if (spawnClock >= 1100) {
        spawnClock = 0;
        if (Math.random() < 0.8) {
          spawn();
        }
      }
    }

    function loop(time: number) {
      if (disposed) {
        return;
      }
      const dt = last > 0 ? Math.min((time - last) / 1000, 0.05) : 0;
      last = time;
      elapsed += dt * 1000;
      step(dt);
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
    ro.observe(canvas);

    return () => {
      disposed = true;
      if (raf) {
        cancelAnimationFrame(raf);
      }
      cancelAnimationFrame(resizeRaf);
      ro.disconnect();
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 h-full w-full"
    />
  );
}

export function MessagingWhySection() {
  return (
    <section className="relative flex min-h-svh items-center overflow-hidden">
      <CircuitBoard />
      <div className="relative z-10 mx-auto grid w-full max-w-6xl items-center gap-10 px-5 py-24 sm:px-12 lg:grid-cols-2">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Why Mocha
          </p>
          <h2 className="font-heading text-h3 sm:text-h2 mt-5 leading-[1.15] font-semibold text-balance">
            <span className="text-cc-ink-dim block">
              A request returns in milliseconds.
            </span>
            <span className="text-cc-heading block">
              The work it sets off runs for days.
            </span>
          </h2>
          <p className="text-cc-ink mt-6 max-w-md text-base text-pretty sm:text-lg">
            Without a messaging layer, that work stops the moment the request
            returns. Events scatter across services, and no one can follow where
            they went. Mocha carries them: commands, events, handlers, and
            sagas, with every hop traced in Nitro.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
          </div>
        </div>
        <div aria-hidden="true" />
      </div>
    </section>
  );
}
