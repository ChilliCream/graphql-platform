"use client";

import type { ReactNode } from "react";
import { useEffect, useRef } from "react";

import type {
  Board,
  Point,
  Pulse,
  RingFlash,
} from "@/src/components/mocha/board";
import {
  GLOW_RGB,
  MSG,
  MSG_SOFT,
  PULSE_TRAIL,
  envelope,
  generateBoard,
  mulberry32,
  paintBoard,
  pointAt,
} from "@/src/components/mocha/board";
import { MONO_FONT } from "@/src/components/mocha/palette";

const SHADOW_TOP =
  "linear-gradient(to bottom, rgba(2,6,16,0.7), rgba(2,6,16,0.25) 55%, transparent)";
const SHADOW_BOTTOM =
  "linear-gradient(to top, rgba(2,6,16,0.7), rgba(2,6,16,0.25) 55%, transparent)";

const VIGNETTE_TOP =
  "linear-gradient(to bottom, rgba(2,6,16,0.35), rgba(2,6,16,0) 12%)";
const VIGNETTE_BOTTOM =
  "linear-gradient(to top, rgba(2,6,16,0.55), rgba(2,6,16,0) 18%)";
const VIGNETTE_SIDES =
  "linear-gradient(to right, rgba(2,6,16,0.3), rgba(2,6,16,0) 8%, rgba(2,6,16,0) 92%, rgba(2,6,16,0.3))";

const BASE_ALPHA = 0.3;
const LIT_ALPHA = 0.85;
const LIT_RADIUS = 230;
const LIT_MID_STOP = 0.55;
const LIT_MID_ALPHA = 0.45;
const DIM_CX = 0.5;
const DIM_CY = 0.3;
const DIM_RADIUS = 0.45;
const DIM_ALPHA = 0.65;

interface BoardSeed {
  readonly fx: number;
  readonly fy: number;
  readonly label?: string;
  readonly designator?: string;
  readonly hideOnSmall?: boolean;
}

const SEEDS: readonly BoardSeed[] = [
  {
    fx: 0.12,
    fy: 0.1,
    label: "Shipping",
    designator: "U44",
    hideOnSmall: true,
  },
  { fx: 0.07, fy: 0.28, label: "Orders", designator: "U7", hideOnSmall: true },
  {
    fx: 0.88,
    fy: 0.12,
    label: "Payments",
    designator: "U18",
    hideOnSmall: true,
  },
  {
    fx: 0.9,
    fy: 0.38,
    label: "Inventory",
    designator: "U9",
    hideOnSmall: true,
  },
  { fx: 0.5, fy: 0.58 },
];

interface NodeChipProps {
  readonly seed: BoardSeed;
}

function NodeChip({ seed }: NodeChipProps) {
  return (
    <div
      className={`pointer-events-none absolute flex -translate-x-1/2 -translate-y-[5px] flex-col items-center gap-1.5 ${
        seed.hideOnSmall ? "max-md:hidden" : ""
      }`}
      style={{ left: `${seed.fx * 100}%`, top: `${seed.fy * 100}%` }}
    >
      <span className="relative block h-2.5 w-2.5 rounded-full border border-[rgba(205,216,232,0.55)]">
        <span className="absolute inset-[2.5px] rounded-full bg-[rgba(232,238,248,0.9)]" />
      </span>
      <span
        className="font-mono text-[0.68rem] font-semibold tracking-[0.26em] whitespace-nowrap uppercase"
        style={{ color: "rgba(170,188,214,0.8)", fontFamily: MONO_FONT }}
      >
        <span
          className="font-normal"
          style={{ color: "rgba(154,172,200,0.45)" }}
        >
          {seed.designator}·
        </span>
        {seed.label}
      </span>
    </div>
  );
}

interface PcbBandProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly id?: string;
}

export function PcbBand({ children, className = "", id }: PcbBandProps) {
  const bandClass = [
    "border-cc-card-border/50 relative left-1/2 isolate w-screen -translate-x-1/2 overflow-hidden border-y",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div id={id} className={bandClass}>
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <div className="bg-cc-surface/25 absolute inset-0" />
        <PcbBoard />
        <div
          className="absolute inset-0"
          style={{ background: VIGNETTE_BOTTOM }}
        />
        <div
          className="absolute inset-0"
          style={{ background: VIGNETTE_TOP }}
        />
        <div
          className="absolute inset-0"
          style={{ background: VIGNETTE_SIDES }}
        />
        <div aria-hidden className="pointer-events-none absolute inset-0">
          {SEEDS.filter((seed) => seed.label).map((seed) => (
            <NodeChip key={seed.designator} seed={seed} />
          ))}
        </div>
        <div
          className="absolute inset-x-0 top-0 h-10"
          style={{ background: SHADOW_TOP }}
        />
        <div
          className="absolute inset-x-0 bottom-0 h-10"
          style={{ background: SHADOW_BOTTOM }}
        />
      </div>

      <div className="relative z-10">{children}</div>
    </div>
  );
}

function PcbBoard() {
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const wrapper = wrapperRef.current;
    const canvas = canvasRef.current;
    if (!wrapper || !canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    const off = document.createElement("canvas");
    const offCtx = off.getContext("2d");
    const base = document.createElement("canvas");
    const baseCtx = base.getContext("2d");
    const lit = document.createElement("canvas");
    const litCtx = lit.getContext("2d");
    if (!ctx || !offCtx || !baseCtx || !litCtx) {
      return;
    }

    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    const runtime = mulberry32(0x9c2b4d1);

    let board: Board | null = null;
    let nodes: Point[] = [];
    let pulses: Pulse[] = [];
    let flash: number[] = [];
    let rings: RingFlash[] = [];
    let dpr = 1;
    let w = 0;
    let h = 0;
    let raf = 0;
    let last = 0;
    let spawnClock = 0;
    let inView = false;
    let disposed = false;

    function prerender() {
      if (!board) {
        return;
      }
      const dw = Math.round(w * dpr);
      const dh = Math.round(h * dpr);
      off.width = dw;
      off.height = dh;
      const g = offCtx!;
      g.setTransform(dpr, 0, 0, dpr, 0, 0);
      g.clearRect(0, 0, w, h);
      paintBoard(g, board, w, h);

      base.width = dw;
      base.height = dh;
      const b = baseCtx!;
      b.setTransform(1, 0, 0, 1, 0, 0);
      b.clearRect(0, 0, dw, dh);
      b.drawImage(off, 0, 0);
      b.setTransform(dpr, 0, 0, dpr, 0, 0);
      b.globalCompositeOperation = "destination-out";
      const dim = b.createRadialGradient(
        w * DIM_CX,
        h * DIM_CY,
        0,
        w * DIM_CX,
        h * DIM_CY,
        w * DIM_RADIUS,
      );
      dim.addColorStop(0, `rgba(0,0,0,${DIM_ALPHA})`);
      dim.addColorStop(1, "rgba(0,0,0,0)");
      b.fillStyle = dim;
      b.fillRect(0, 0, w, h);
      b.globalCompositeOperation = "source-over";

      lit.width = dw;
      lit.height = dh;
      const l = litCtx!;
      l.setTransform(dpr, 0, 0, dpr, 0, 0);
      l.clearRect(0, 0, w, h);
      for (const n of nodes) {
        const pool = l.createRadialGradient(n.x, n.y, 0, n.x, n.y, LIT_RADIUS);
        pool.addColorStop(0, "rgba(255,255,255,1)");
        pool.addColorStop(LIT_MID_STOP, `rgba(255,255,255,${LIT_MID_ALPHA})`);
        pool.addColorStop(1, "rgba(255,255,255,0)");
        l.fillStyle = pool;
        l.beginPath();
        l.arc(n.x, n.y, LIT_RADIUS, 0, Math.PI * 2);
        l.fill();
      }
      l.globalCompositeOperation = "source-in";
      l.setTransform(1, 0, 0, 1, 0, 0);
      l.drawImage(off, 0, 0);
      l.globalCompositeOperation = "source-over";
    }

    function emitNode(nodeIndex: number) {
      if (!board || pulses.length >= 10) {
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
      if (!board || pulses.length >= 10) {
        return;
      }
      const lanes = board.connectors.filter((t) => t.len > 80);
      if (lanes.length === 0) {
        return;
      }
      const t = lanes[Math.floor(runtime() * lanes.length)];
      pulses.push({ trace: t, dist: 0, speed: 120 + runtime() * 80, to: t.to });
    }

    function render(time: number) {
      const c = ctx!;
      c.setTransform(1, 0, 0, 1, 0, 0);
      c.globalCompositeOperation = "source-over";
      c.clearRect(0, 0, canvas!.width, canvas!.height);
      c.globalAlpha = BASE_ALPHA;
      c.drawImage(base, 0, 0);
      c.globalAlpha = LIT_ALPHA * (1 + 0.1 * Math.sin(time / 177));
      c.drawImage(lit, 0, 0);
      c.globalAlpha = 1;
      if (!board) {
        return;
      }
      c.setTransform(dpr, 0, 0, dpr, 0, 0);
      c.globalCompositeOperation = "lighter";

      for (let i = 0; i < nodes.length; i++) {
        const n = nodes[i];
        const breathe = 1 + 0.08 * Math.sin(time / 177 + i * 2.1);
        const level = breathe * (1 + 0.8 * flash[i]);
        const halo = c.createRadialGradient(n.x, n.y, 0, n.x, n.y, 90);
        halo.addColorStop(0, `rgba(${GLOW_RGB},${0.13 * level})`);
        halo.addColorStop(1, `rgba(${GLOW_RGB},0)`);
        c.fillStyle = halo;
        c.beginPath();
        c.arc(n.x, n.y, 90, 0, Math.PI * 2);
        c.fill();
        const core = c.createRadialGradient(n.x, n.y, 0, n.x, n.y, 16);
        core.addColorStop(0, `rgba(${GLOW_RGB},${0.3 * level})`);
        core.addColorStop(1, `rgba(${GLOW_RGB},0)`);
        c.fillStyle = core;
        c.beginPath();
        c.arc(n.x, n.y, 16, 0, Math.PI * 2);
        c.fill();
      }

      c.lineCap = "round";
      for (const pulse of pulses) {
        const alpha = envelope(pulse) * 0.8;
        if (alpha <= 0) {
          continue;
        }
        const head = pointAt(pulse.trace, pulse.dist);
        const chunks = 7;
        c.lineWidth = 1.6;
        let prev = pointAt(pulse.trace, Math.max(0, pulse.dist - PULSE_TRAIL));
        for (let k = 1; k <= chunks; k++) {
          const d = Math.max(
            0,
            pulse.dist - PULSE_TRAIL + (k * PULSE_TRAIL) / chunks,
          );
          const p = pointAt(pulse.trace, d);
          c.strokeStyle = `rgba(${MSG},${alpha * Math.pow(k / chunks, 2) * 0.9})`;
          c.beginPath();
          c.moveTo(prev.x, prev.y);
          c.lineTo(p.x, p.y);
          c.stroke();
          prev = p;
        }
        const g = c.createRadialGradient(head.x, head.y, 0, head.x, head.y, 8);
        g.addColorStop(0, `rgba(${MSG_SOFT},${alpha * 0.4})`);
        g.addColorStop(1, `rgba(${MSG_SOFT},0)`);
        c.fillStyle = g;
        c.beginPath();
        c.arc(head.x, head.y, 8, 0, Math.PI * 2);
        c.fill();
        c.fillStyle = `rgba(${MSG_SOFT},${alpha})`;
        c.beginPath();
        c.arc(head.x, head.y, 1.8, 0, Math.PI * 2);
        c.fill();
      }

      c.lineWidth = 1.4;
      for (let i = 0; i < nodes.length; i++) {
        if (flash[i] <= 0.02) {
          continue;
        }
        const n = nodes[i];
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
      for (let i = 0; i < flash.length; i++) {
        flash[i] = Math.max(0, flash[i] - dt / 0.7);
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
        if (runtime() < 0.6) {
          emitNode(Math.floor(runtime() * nodes.length));
        } else {
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
      w = wrapper!.clientWidth;
      h = wrapper!.clientHeight;
      if (w === 0 || h === 0) {
        return;
      }
      canvas!.width = Math.round(w * dpr);
      canvas!.height = Math.round(h * dpr);
      nodes = SEEDS.map((s) => ({ x: s.fx * w, y: s.fy * h }));
      board = generateBoard(nodes, w, h);
      pulses = [];
      rings = [];
      flash = nodes.map(() => 0);
      prerender();
    }

    measureAndBuild();
    render(0);

    const onVisibility = () => {
      sync();
    };
    let io: IntersectionObserver | null = null;
    if (!reduced) {
      io = new IntersectionObserver(
        (entries) => {
          inView = entries[entries.length - 1]?.isIntersecting ?? false;
          sync();
        },
        { rootMargin: "60px" },
      );
      io.observe(wrapper);
      document.addEventListener("visibilitychange", onVisibility);
    }

    let resizeRaf = 0;
    const ro = new ResizeObserver(() => {
      cancelAnimationFrame(resizeRaf);
      resizeRaf = requestAnimationFrame(() => {
        if (
          disposed ||
          (wrapper.clientWidth === w && wrapper.clientHeight === h && board)
        ) {
          return;
        }
        measureAndBuild();
        render(0);
      });
    });
    ro.observe(wrapper);

    return () => {
      disposed = true;
      stop();
      cancelAnimationFrame(resizeRaf);
      ro.disconnect();
      io?.disconnect();
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, []);

  return (
    <div ref={wrapperRef} className="absolute inset-0">
      <canvas ref={canvasRef} className="absolute inset-0 h-full w-full" />
    </div>
  );
}
