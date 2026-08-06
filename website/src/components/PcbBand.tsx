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

/** Inner shadows cast by the page surface onto the recessed board, so the PCB
 *  reads as a layer sitting a bit behind the page instead of fading into it. */
const SHADOW_TOP =
  "linear-gradient(to bottom, rgba(2,6,16,0.7), rgba(2,6,16,0.25) 55%, transparent)";
const SHADOW_BOTTOM =
  "linear-gradient(to top, rgba(2,6,16,0.7), rgba(2,6,16,0.25) 55%, transparent)";

/** Shadow vignette in the HeroBoard overlay idiom: soft dark gradients laid
 *  over the canvas (below the silkscreen chips) so the board falls off into
 *  shadow toward the band's edges. The top/bottom fades are wider and gentler
 *  than the inner shadows layered above them, so the two blend into one
 *  falloff instead of doubling into a hard band; the side fade is faint
 *  enough that the edge chips' lamp pools still glow through. */
const VIGNETTE_TOP =
  "linear-gradient(to bottom, rgba(2,6,16,0.35), rgba(2,6,16,0) 12%)";
const VIGNETTE_BOTTOM =
  "linear-gradient(to top, rgba(2,6,16,0.55), rgba(2,6,16,0) 18%)";
const VIGNETTE_SIDES =
  "linear-gradient(to right, rgba(2,6,16,0.3), rgba(2,6,16,0) 8%, rgba(2,6,16,0) 92%, rgba(2,6,16,0.3))";

/** Lighting model: the board sinks into shadow away from the lights. A faint
 *  BASE keeps copper texture perceptible everywhere, never fully black
 *  (extra-dimmed in the central text zone via a destination-out radial erase
 *  baked into the prerendered BASE canvas), while the light-source nodes
 *  carry the dominant read: wide, softly falling-off lamp pools of bright
 *  copper (the prerendered LIT canvas). CSS vignette overlays then shade the
 *  band's edges. Per frame the two canvases are simply composited, no
 *  gradients are built. */

/** Strength of the shadowed base board: faint but visible everywhere, so the
 *  board between the lamp pools reads as copper in shadow (flanks at about
 *  0.3 effective, the erased center at about 0.1). */
const BASE_ALPHA = 0.3;
/** Strength of the dominant lamp-pool reveal, breathing with the halos
 *  (±10%). */
const LIT_ALPHA = 0.85;
/** Radius of each light source's copper lamp pool, in CSS pixels. */
const LIT_RADIUS = 230;
/** Mid stop of the pool's three-stop falloff (core 1, LIT_MID_ALPHA at
 *  LIT_MID_STOP of the radius, 0 at the edge), so copper fades gradually
 *  from each light into the surrounding shadow like a lamp pool. */
const LIT_MID_STOP = 0.55;
const LIT_MID_ALPHA = 0.45;
/** Center/radius/strength of the central dim zone baked into the base:
 *  centered on the text column, erasing up to DIM_ALPHA of the board so the
 *  center keeps roughly a third of the base strength (never going fully
 *  invisible) while the flanks stay at full base strength. */
const DIM_CX = 0.5;
const DIM_CY = 0.3;
const DIM_RADIUS = 0.45;
const DIM_ALPHA = 0.65;

/** A board service node: its seed position as width/height fractions plus an
 *  optional silkscreen chip printed at the same spot in the DOM layer. Every
 *  seed is a light source (halo plus copper reveal pool); only seeds on the
 *  band's flanks carry a printed label, so no chip text ever sits behind the
 *  centered heading or the facet cards. */
interface BoardSeed {
  readonly fx: number;
  readonly fy: number;
  readonly label?: string;
  readonly designator?: string;
  /** The least-central chips step aside on small screens to avoid clutter. */
  readonly hideOnSmall?: boolean;
}

/** The band's light sources: exactly five nodes. Four labeled chips on the
 *  flanks (the canvas nodes and DOM silkscreen chips derive from the same
 *  fractions, so the labels sit exactly on the lit nodes) plus one unlabeled
 *  node behind the center visualization panel, whose glow deliberately bleeds
 *  through it. No light sits around the heading text itself. */
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
  // The single interior light: no printed label, it sits behind the
  // visualization panel and its glow shines through.
  { fx: 0.5, fy: 0.58 },
];

interface NodeChipProps {
  readonly seed: BoardSeed;
}

/** Silkscreen service marker printed over a canvas node: a ring-and-dot plus
 *  a mono label with its reference designator, purely decorative. */
function NodeChip({ seed }: NodeChipProps) {
  return (
    <div
      // Center the ring-dot on the light source (half the 10px dot height);
      // the label hangs below instead of pushing the dot off the node.
      className={`pointer-events-none absolute flex -translate-x-1/2 -translate-y-[5px] flex-col items-center gap-1.5 ${
        seed.hideOnSmall ? "max-md:hidden" : ""
      }`}
      style={{ left: `${seed.fx * 100}%`, top: `${seed.fy * 100}%` }}
    >
      <span className="relative block h-2.5 w-2.5 rounded-full border border-[rgba(205,216,232,0.55)]">
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
          {seed.designator}·
        </span>
        {seed.label}
      </span>
    </div>
  );
}

interface PcbBandProps {
  readonly children: ReactNode;
  /** Border width and vertical padding are the caller's concern, e.g.
   *  `"pb-16 sm:pb-24"` when wrapping a section that already carries its own
   *  top padding. */
  readonly className?: string;
  readonly id?: string;
}

/**
 * Full-bleed "chapter" band that breaks out of the centered content column to
 * carry a procedural PCB (circuit board) texture. The board sits mostly in
 * shadow: a faint copper texture everywhere (dimmed further behind the
 * central text column, never fully black) with the light-source nodes
 * carrying the read, each revealing a bright, softly falling-off lamp pool of
 * copper around itself. Silkscreen chips label the flank nodes in the DOM so
 * they stay crisp. While the band is on screen (and motion is allowed) the
 * board runs the messaging animation: teal halos breathe at the service
 * nodes and coral message pulses travel the copper lanes, flashing a ring on
 * arrival. The band meets the page on hard edges, with a shadow vignette
 * (HeroBoard-style gradient overlays) plus inner shadows at the top and
 * bottom so the board reads as recessed behind the page surface and sinks
 * into darkness toward the edges. The children supply their own centered
 * column.
 */
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

/** The procedural circuit board with its messaging animation. The static
 *  board is pre-rendered once (and on resize) into two offscreen canvases:
 *  BASE (the full board kept faint, with the central text zone erased down
 *  to about a third of that base strength) and LIT (the board kept only
 *  inside a soft lamp pool around each light source, the dominant read). The
 *  animation loop composites the two with plain drawImage calls and draws
 *  node halos, message pulses, and arrival rings on top. With
 *  prefers-reduced-motion the base+lit composite renders once, statically,
 *  and nothing moves. */
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
    // Deterministic runtime stream for spawn timing and lane choice.
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

      // BASE: the full board, with the central text zone erased down so the
      // center keeps roughly a third of the (already faint) base strength
      // while the flanks stay at full base strength.
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

      // LIT: the board kept only inside a soft lamp pool around each light
      // source. The pools are painted as a union of alpha gradients first,
      // then the board is drawn through them (equivalent to a destination-in
      // mask, but overlapping pools merge instead of intersecting).
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
      // Composite the prerendered lighting: the dimmed base everywhere, then
      // the light-pool reveal breathing in step with the halos.
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

      // Breathing teal halos: the service nodes lighting up on the board.
      // An arrival flash briefly lifts the node's light level.
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

      // Coral messages: a fading trail and a soft glowing head.
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

      // Arrival flash rings at nodes and at loose trace endpoints.
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
