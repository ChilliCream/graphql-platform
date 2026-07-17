"use client";

/**
 * Ember-context slide with the messaging PCB network integrated. Same warm
 * stage, frame, serif headline, and body as the reference, but the right-hand
 * graphic is now a live circuit: warm service nodes light a generated board,
 * and coral messages travel the lanes between them. The board is warm-themed
 * to sit inside the ember scene rather than the cool site-blue treatment, and
 * only shows where light falls, so the left side stays dark under the copy.
 *
 * Reuses the ember glow, drifting sparks, and coral spark from the v5 slide.
 */

import { useEffect, useRef } from "react";

import {
  EmberField,
  SERIF,
} from "@/src/components/home/mocha/messaging-graphic/EmberContextSection";

const GRID = 22;
const SUBSTRATE = "#160d09";
const TRACE_COLOR = "rgba(196, 158, 128, 0.34)";
const TRACE_ALT_COLOR = "rgba(242, 150, 96, 0.28)";
const LANE_COLOR = "rgba(238, 186, 146, 0.62)";
const VIA_COLOR = "rgba(200, 165, 135, 0.5)";
const PAD_COLOR = "rgba(200, 165, 135, 0.09)";
const AMBER = "236, 150, 66";
const CORAL = "242, 100, 60";
const CORAL_SOFT = "255, 172, 128";

const NODE_LIGHT_RADIUS = 150;
const PULSE_LIGHT_RADIUS = 100;
const PULSE_TRAIL = 140;

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
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
  flashTo: number;
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
  const segs = 3 + Math.floor(rand() * 4);
  for (let s = 0; s < segs; s++) {
    const diag = d % 2 === 1;
    const cells = diag
      ? 1 + Math.floor(rand() * 3)
      : 2 + Math.floor(rand() * 5);
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

function NetworkCanvas() {
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
    let pulses: Pulse[] = [];
    let w = 0;
    let h = 0;
    let dpr = 1;
    let raf = 0;
    let last = 0;
    let elapsed = 0;
    let spawnClock = 0;
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
      const rand = mulberry32(53 + Math.floor(w) * 7 + Math.floor(h));
      const rootRect = root!.getBoundingClientRect();
      const scope = root!.parentElement ?? root!;
      const chips = Array.from(
        scope.querySelectorAll<HTMLElement>("[data-ember-node]"),
      );
      nodes = chips.map((chip, i) => {
        const cr = chip.getBoundingClientRect();
        return {
          x: cr.left - rootRect.left + cr.width / 2,
          y: cr.top - rootRect.top + cr.height / 2,
          power: reduced ? 1 : 0,
          powerAt: 200 + i * 220,
          flash: 0,
        };
      });

      traces = [];
      connectors = [];
      vias = [];
      pulses = [];

      nodes.forEach((node, n) => {
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
        void n;
      });

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
          const lane = connector(nodes[i], nodes[o.j], i, o.j);
          const back = traceFrom([...lane.pts].reverse(), o.j, i, true);
          lane.rev = back;
          back.rev = lane;
          traces.push(lane);
          connectors.push(lane, back);
        }
      }

      // Sparse ambient traces, only revealed near the lit nodes.
      const ambient = Math.min(260, Math.floor((w * h) / 14000));
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
    }

    function emit(p: Pulse) {
      if (pulses.length < 14) {
        pulses.push(p);
      }
    }

    function spawn() {
      const lanes = connectors.filter((t) => t.len > 70);
      if (lanes.length === 0) {
        return;
      }
      const t = lanes[Math.floor(Math.random() * lanes.length)];
      emit({
        trace: t,
        dist: 0,
        speed: 220 + Math.random() * 140,
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
          litCtx!.fillRect(x - 0.7, y - 0.7, 1.4, 1.4);
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
          litCtx!.lineWidth = 1.3;
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
      litCtx!.lineWidth = 1.1;
      litCtx!.fillStyle = SUBSTRATE;
      for (const via of vias) {
        litCtx!.beginPath();
        litCtx!.arc(via.x, via.y, 2.3, 0, Math.PI * 2);
        litCtx!.fill();
        litCtx!.stroke();
      }
    }

    function nodeLevel(n: Node, time: number, i: number): number {
      const flicker =
        0.85 +
        0.09 * Math.sin(time / 640 + i * 1.7) +
        0.06 * Math.sin(time / 233 + i * 3.1);
      return flicker * n.power * (1 + 0.8 * n.flash);
    }

    function drawMask(time: number) {
      maskCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      maskCtx!.clearRect(0, 0, w, h);
      nodes.forEach((n, i) => {
        const level = nodeLevel(n, time, i);
        if (level <= 0.01) {
          return;
        }
        const radius = NODE_LIGHT_RADIUS * (1 + 0.16 * n.flash);
        const p1 = time * 0.00037 + i * 2.4;
        const p2 = time * 0.00029 + i * 1.3;
        const blobs = [
          { dx: 0, dy: 0, r: radius, a: 0.85 },
          {
            dx: Math.cos(p1) * 26,
            dy: Math.sin(p2) * 22,
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
          const r = PULSE_LIGHT_RADIUS * (1 - k * 0.2);
          const a = alpha * (1 - k * 0.3) * 0.9;
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
        const haloR = NODE_LIGHT_RADIUS * 0.9;
        const halo = ctx!.createRadialGradient(n.x, n.y, 0, n.x, n.y, haloR);
        halo.addColorStop(0, `rgba(${AMBER},${0.17 * level})`);
        halo.addColorStop(0.45, `rgba(${AMBER},${0.06 * level})`);
        halo.addColorStop(1, `rgba(${AMBER},0)`);
        ctx!.fillStyle = halo;
        ctx!.beginPath();
        ctx!.arc(n.x, n.y, haloR, 0, Math.PI * 2);
        ctx!.fill();
        if (n.flash > 0.02) {
          const ringR = 12 + (1 - n.flash) * 42;
          ctx!.strokeStyle = `rgba(${CORAL},${0.55 * n.flash})`;
          ctx!.lineWidth = 1.6;
          ctx!.beginPath();
          ctx!.arc(n.x, n.y, ringR, 0, Math.PI * 2);
          ctx!.stroke();
        }
      });

      // Brighten the lanes between nodes so the network structure reads.
      ctx!.lineCap = "round";
      ctx!.lineJoin = "round";
      ctx!.lineWidth = 2;
      ctx!.strokeStyle = `rgba(${CORAL_SOFT}, 0.28)`;
      for (const t of connectors) {
        if (t.from > t.to) {
          continue;
        }
        ctx!.beginPath();
        ctx!.moveTo(t.pts[0].x, t.pts[0].y);
        for (let p = 1; p < t.pts.length; p++) {
          ctx!.lineTo(t.pts[p].x, t.pts[p].y);
        }
        ctx!.stroke();
      }

      for (const pulse of pulses) {
        const alpha = envelope(pulse);
        if (alpha <= 0) {
          continue;
        }
        const head = pointAt(pulse.trace, pulse.dist);
        const chunks = 8;
        ctx!.lineWidth = 2.3;
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
        const g = ctx!.createRadialGradient(
          head.x,
          head.y,
          0,
          head.x,
          head.y,
          26,
        );
        g.addColorStop(0, `rgba(${CORAL_SOFT},${alpha * 0.6})`);
        g.addColorStop(1, `rgba(${CORAL_SOFT},0)`);
        ctx!.fillStyle = g;
        ctx!.beginPath();
        ctx!.arc(head.x, head.y, 26, 0, Math.PI * 2);
        ctx!.fill();
        ctx!.fillStyle = `rgba(255,236,224,${alpha})`;
        ctx!.beginPath();
        ctx!.arc(head.x, head.y, 2.5, 0, Math.PI * 2);
        ctx!.fill();
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

    function step(dt: number) {
      nodes.forEach((n) => {
        if (n.power < 1 && elapsed >= n.powerAt) {
          n.power = Math.min(1, n.power + dt / 0.8);
        }
        n.flash = Math.max(0, n.flash - dt / 0.6);
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
        if (f.flashTo >= 0) {
          nodes[f.flashTo].flash = 1;
        }
      }
      spawnClock += dt * 1000;
      if (spawnClock >= 440) {
        spawnClock = 0;
        if (Math.random() < 0.85) {
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
    ro.observe(root);

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
    <div ref={rootRef} className="absolute inset-0" aria-hidden="true">
      <canvas
        ref={canvasRef}
        className="pointer-events-none absolute inset-0 h-full w-full"
      />
    </div>
  );
}

interface NodeChipProps {
  readonly label: string;
  readonly className?: string;
}

/** A warm amber service node marking a light source on the board. */
function NodeChip({ label, className }: NodeChipProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-1.5 ${className ?? ""}`}
    >
      <span
        data-ember-node
        className="relative block h-3.5 w-3.5 rounded-[3px] border"
        style={{
          borderColor: "rgba(236,150,66,0.85)",
          backgroundColor: "#2a1608",
          boxShadow: "0 0 16px 4px rgba(236,150,66,0.28)",
        }}
      >
        <span
          className="absolute inset-[3px] rounded-[2px]"
          style={{
            backgroundColor: "#f0a24a",
            boxShadow: "0 0 6px 1px rgba(255,190,120,0.9)",
          }}
        />
      </span>
      <span
        className="font-mono text-[0.56rem] tracking-[0.2em] uppercase"
        style={{
          color: "#e7b489",
          textShadow: "0 0 12px rgba(236,150,66,0.4)",
        }}
      >
        {label}
      </span>
    </div>
  );
}

const NODES: ReadonlyArray<NodeChipProps> = [
  { label: "Ordering", className: "top-[28%] right-[27%]" },
  { label: "Billing", className: "top-[42%] right-[13%]" },
  { label: "Shipping", className: "top-[33%] right-[6%]" },
  { label: "Payments", className: "top-[58%] right-[23%]" },
  { label: "Reviews", className: "top-[64%] right-[9%]" },
  { label: "Inventory", className: "top-[48%] left-[43%]" },
];

export function EmberNetworkSection() {
  return (
    <section className="relative min-h-svh overflow-hidden bg-[#0b0706]">
      {/* Warm ember glow plus a coral hue where the network sits, and a cool
          counter-hue on the far side for depth. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            "radial-gradient(44rem 32rem at 60% 48%, rgba(214,104,44,0.14), rgba(150,66,26,0.05) 44%, rgba(0,0,0,0) 72%), radial-gradient(22rem 18rem at 68% 30%, rgba(240,96,84,0.13), rgba(0,0,0,0) 68%), radial-gradient(30rem 26rem at 14% 82%, rgba(60,84,150,0.09), rgba(0,0,0,0) 70%)",
        }}
      />

      {/* Milk-glass card: a frosted panel that only frosts the ambient glow
          behind it. The network and copy sit on top so they stay crisp. */}
      <div
        aria-hidden="true"
        className="absolute inset-5 rounded-xl border border-white/12 shadow-[inset_0_1px_0_rgba(255,255,255,0.09),0_24px_70px_rgba(0,0,0,0.45)] backdrop-blur-md sm:inset-8 lg:inset-10"
        style={{
          background:
            "linear-gradient(160deg, rgba(255,250,244,0.09), rgba(255,244,236,0.035) 48%, rgba(255,240,232,0.07))",
        }}
      />

      {/* The messaging PCB network, crisp above the glass. */}
      <NetworkCanvas />
      <EmberField />

      {/* Content, positioned within the same frame (transparent). */}
      <div className="absolute inset-5 sm:inset-8 lg:inset-10">
        <span className="absolute top-0 left-1/2 h-5 w-px -translate-x-1/2 bg-white/10" />
        <span className="absolute bottom-0 left-1/2 h-5 w-px -translate-x-1/2 bg-white/10" />

        <span className="absolute top-7 left-7 font-mono text-[0.6rem] tracking-[0.32em] text-white/30 uppercase sm:top-9 sm:left-11">
          Why Mocha
        </span>

        {/* Service nodes, positioned within the frame. */}
        {NODES.map((node) => (
          <NodeChip key={node.label} {...node} />
        ))}

        {/* Headline. */}
        <div className="absolute top-1/2 left-7 max-w-[20rem] -translate-y-1/2 sm:left-11 sm:max-w-[34rem]">
          <h2
            style={{ fontFamily: SERIF }}
            className="text-[1.7rem] leading-[1.22] tracking-[-0.01em] sm:text-[2.05rem] lg:text-[2.4rem]"
          >
            <span className="block text-[#b7a89a]">
              A request returns in milliseconds.
            </span>
            <span className="mt-1 block text-[#f3ede4]">
              The work it sets off runs for days.
            </span>
          </h2>
        </div>

        {/* Body. */}
        <p className="absolute bottom-8 left-7 max-w-md text-[0.82rem] leading-relaxed text-white/45 sm:bottom-11 sm:left-11">
          Without a messaging layer, that work stops the moment the request
          returns.
          <br />
          Events scatter across services, and no one can follow where they went.
        </p>

        {/* Brand mark. */}
        <span
          className="absolute top-1/2 right-[5%] hidden -translate-y-1/2 text-2xl font-bold tracking-tight text-white/90 md:block"
          style={{ fontFamily: SERIF }}
        >
          Mocha
        </span>
      </div>
    </section>
  );
}
