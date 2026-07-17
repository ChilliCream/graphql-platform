"use client";

/**
 * Mocha messaging, version 9: "Dark board".
 *
 * A full-page take. The page is near black. Behind the copy sits a generated
 * circuit board (traces, vias, a faint pad grid) that is invisible on its own:
 * it only shows where light falls on it. The light sources are the service
 * nodes (Ordering, Billing, ...), small yellow chips spread around the hero
 * and the rest of the page. Each node illuminates the board around itself, and
 * occasionally emits a pulse that travels along one of the traces. The pulse
 * carries its own light, so the board becomes briefly visible along its path.
 *
 * Rendering: three canvases. An offscreen "lit" canvas gets the visible slice
 * of the board, then a light mask (radial gradients at nodes and pulse heads)
 * is applied with destination-in, so the board only survives where light is.
 * The result is composited onto the visible canvas over solid black, and the
 * yellow glows and pulse trails are drawn additively on top. The visible
 * canvas is viewport-sized and fixed; drawing is translated by scrollY, so the
 * board stays anchored to the page while only the viewport slice is painted.
 *
 * Node positions are DOM-measured (the chips are absolutely positioned inside
 * their sections), so the traces always start at the labels regardless of
 * layout. The board is regenerated when the page resizes. With reduced motion
 * there are no pulses and the scene only repaints on scroll and resize.
 */

import Link from "next/link";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Board geometry. Traces walk on this grid with 45 degree bends, PCB style.
const GRID = 24;
const PAGE_BG = "#04060b";
const SUBSTRATE = "#0d1524";
const TRACE_COLOR = "rgba(137, 160, 188, 0.55)";
const TRACE_ALT_COLOR = "rgba(94, 234, 212, 0.34)";
const VIA_COLOR = "rgba(160, 180, 205, 0.7)";
const PAD_COLOR = "rgba(148, 163, 184, 0.16)";

// Light sizes in CSS pixels.
const NODE_LIGHT_RADIUS = 190;
const PULSE_LIGHT_RADIUS = 120;
const PULSE_TRAIL = 150;

const MAX_PULSES = 7;
const SPAWN_INTERVAL_MS = 380;
const SPAWN_CHANCE = 0.7;

interface Point {
  readonly x: number;
  readonly y: number;
}

interface Trace {
  readonly pts: Point[];
  /** Cumulative length at each point, cum[0] = 0. */
  readonly cum: number[];
  readonly len: number;
  /** True when the trace starts at a service node and can carry a pulse. */
  readonly fromNode: boolean;
}

interface Board {
  readonly traces: Trace[];
  readonly vias: Point[];
  readonly nodes: Point[];
  /** Trace indices bucketed by BIN_H rows of their bounding box. */
  readonly bins: number[][];
  readonly width: number;
  readonly height: number;
}

interface Pulse {
  trace: Trace;
  dist: number;
  speed: number;
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

function traceFromPoints(pts: Point[], fromNode: boolean): Trace {
  const cum: number[] = [0];
  let len = 0;
  for (let i = 1; i < pts.length; i++) {
    len += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    cum.push(len);
  }
  return { pts, cum, len, fromNode };
}

/**
 * Walk a PCB-style trace: long axis-aligned runs, short 45 degree jogs, never
 * a reversal. Returns null when the walk leaves the board immediately.
 */
function walkTrace(
  rand: () => number,
  start: Point,
  dir: number,
  width: number,
  height: number,
  fromNode: boolean,
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
  return pts.length >= 2 ? traceFromPoints(pts, fromNode) : null;
}

/** Route between two nodes: one long axis run, then a 45 degree approach. */
function connectorTrace(a: Point, b: Point): Trace {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const mid: Point =
    Math.abs(dx) >= Math.abs(dy)
      ? { x: b.x - Math.sign(dx) * Math.abs(dy), y: a.y }
      : { x: a.x, y: b.y - Math.sign(dy) * Math.abs(dx) };
  return traceFromPoints([a, mid, b], true);
}

function generateBoard(nodes: Point[], width: number, height: number): Board {
  const rand = mulberry32(1337 + width * 31 + Math.floor(height));
  const traces: Trace[] = [];
  const vias: Point[] = [];

  // Traces radiating out of every service node.
  for (const node of nodes) {
    const count = 6 + Math.floor(rand() * 3);
    for (let i = 0; i < count; i++) {
      const dir = Math.floor(rand() * 8);
      const start = {
        x: node.x + DIR_VECS[dir].x * GRID * 0.5,
        y: node.y + DIR_VECS[dir].y * GRID * 0.5,
      };
      const trace = walkTrace(rand, start, dir, width, height, true);
      if (trace) {
        traces.push(trace);
        vias.push(trace.pts[trace.pts.length - 1]);
      }
    }
  }

  // A lane between each node and its two nearest neighbors, so pulses can
  // visibly travel service to service.
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
      traces.push(connectorTrace(nodes[i], o.n));
    }
  }

  // Ambient traces everywhere else, only ever seen when light passes by.
  const ambientCount = Math.min(900, Math.floor((width * height) / 9500));
  for (let i = 0; i < ambientCount; i++) {
    const start = {
      x: GRID * (1 + Math.floor(rand() * (width / GRID - 2))),
      y: GRID * (1 + Math.floor(rand() * (height / GRID - 2))),
    };
    const trace = walkTrace(
      rand,
      start,
      Math.floor(rand() * 8),
      width,
      height,
      false,
    );
    if (trace) {
      traces.push(trace);
      vias.push(trace.pts[0]);
      vias.push(trace.pts[trace.pts.length - 1]);
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

  return { traces, vias, nodes, bins, width, height };
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
    let dpr = 1;
    let viewW = 0;
    let viewH = 0;
    let rafId = 0;
    let lastTime = 0;
    let spawnClock = 0;
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

    function measureAndBuild() {
      sizeCanvases();
      // The chips live in the sibling content sections, so search from the
      // shared parent, not from this background wrapper.
      const scope = root!.parentElement ?? root!;
      const chips = scope.querySelectorAll<HTMLElement>("[data-mg9-node]");
      const scrollY = window.scrollY;
      const nodes: Point[] = [];
      chips.forEach((chip) => {
        const r = chip.getBoundingClientRect();
        nodes.push({
          x: r.left + r.width / 2,
          y: r.top + r.height / 2 + scrollY,
        });
      });
      const width = root!.clientWidth;
      const height = root!.scrollHeight;
      board = generateBoard(nodes, width, height);
      pulses = [];
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

      // Traces in the visible bins.
      litCtx!.lineCap = "round";
      litCtx!.lineJoin = "round";
      litCtx!.lineWidth = 1.4;
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
          litCtx!.strokeStyle = i % 6 === 0 ? TRACE_ALT_COLOR : TRACE_COLOR;
          litCtx!.beginPath();
          litCtx!.moveTo(trace.pts[0].x, trace.pts[0].y);
          for (let p = 1; p < trace.pts.length; p++) {
            litCtx!.lineTo(trace.pts[p].x, trace.pts[p].y);
          }
          litCtx!.stroke();
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

      for (let i = 0; i < board.nodes.length; i++) {
        const node = board.nodes[i];
        if (
          node.y < top - NODE_LIGHT_RADIUS ||
          node.y > bottom + NODE_LIGHT_RADIUS
        ) {
          continue;
        }
        const flicker =
          0.86 +
          0.08 * Math.sin(time / 690 + i * 1.7) +
          0.06 * Math.sin(time / 251 + i * 3.1);
        const g = maskCtx!.createRadialGradient(
          node.x,
          node.y,
          0,
          node.x,
          node.y,
          NODE_LIGHT_RADIUS,
        );
        g.addColorStop(0, `rgba(255,255,255,${0.95 * flicker})`);
        g.addColorStop(0.55, `rgba(255,255,255,${0.4 * flicker})`);
        g.addColorStop(1, "rgba(255,255,255,0)");
        maskCtx!.fillStyle = g;
        maskCtx!.beginPath();
        maskCtx!.arc(node.x, node.y, NODE_LIGHT_RADIUS, 0, Math.PI * 2);
        maskCtx!.fill();
      }

      for (const pulse of pulses) {
        const alpha = pulseEnvelope(pulse);
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

      // Warm halo around each node, on top of the revealed board.
      for (let i = 0; i < board.nodes.length; i++) {
        const node = board.nodes[i];
        if (
          node.y < top - NODE_LIGHT_RADIUS ||
          node.y > bottom + NODE_LIGHT_RADIUS
        ) {
          continue;
        }
        const flicker = 0.9 + 0.1 * Math.sin(time / 570 + i * 2.3);
        const g = ctx!.createRadialGradient(
          node.x,
          node.y,
          0,
          node.x,
          node.y,
          NODE_LIGHT_RADIUS * 0.85,
        );
        g.addColorStop(0, `rgba(251,191,36,${0.14 * flicker})`);
        g.addColorStop(0.5, `rgba(251,191,36,${0.05 * flicker})`);
        g.addColorStop(1, "rgba(251,191,36,0)");
        ctx!.fillStyle = g;
        ctx!.beginPath();
        ctx!.arc(node.x, node.y, NODE_LIGHT_RADIUS * 0.85, 0, Math.PI * 2);
        ctx!.fill();
      }

      // Pulse trails and heads.
      ctx!.lineCap = "round";
      for (const pulse of pulses) {
        const alpha = pulseEnvelope(pulse);
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
            ctx!.strokeStyle = `rgba(253,224,71,${alpha * Math.pow(k / chunks, 2) * 0.85})`;
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
          g.addColorStop(0, `rgba(254,240,138,${alpha * 0.6})`);
          g.addColorStop(1, "rgba(254,240,138,0)");
          ctx!.fillStyle = g;
          ctx!.beginPath();
          ctx!.arc(head.x, head.y, 26, 0, Math.PI * 2);
          ctx!.fill();
          ctx!.fillStyle = `rgba(255,251,235,${alpha})`;
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

      ctx!.setTransform(1, 0, 0, 1, 0, 0);
      ctx!.globalCompositeOperation = "source-over";
      ctx!.fillStyle = PAGE_BG;
      ctx!.fillRect(0, 0, canvas!.width, canvas!.height);
      ctx!.drawImage(litCanvas, 0, 0);

      drawGlows(scrollY, time);
    }

    function stepPulses(dt: number) {
      if (!board) {
        return;
      }
      for (const pulse of pulses) {
        pulse.dist += pulse.speed * dt;
      }
      pulses = pulses.filter((p) => p.dist < p.trace.len);

      spawnClock += dt * 1000;
      if (spawnClock >= SPAWN_INTERVAL_MS) {
        spawnClock = 0;
        if (pulses.length < MAX_PULSES && Math.random() < SPAWN_CHANCE) {
          const candidates = board.traces.filter(
            (t) => t.fromNode && t.len > 120,
          );
          if (candidates.length > 0) {
            const trace =
              candidates[Math.floor(Math.random() * candidates.length)];
            pulses.push({ trace, dist: 0, speed: 260 + Math.random() * 180 });
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

/** A yellow chip with its service name, one light source on the board. */
function ServiceNode({ label, className }: ServiceNodeProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-2 ${className ?? ""}`}
    >
      <span
        data-mg9-node
        className="relative block h-4 w-4 rounded-[4px] border border-amber-300/90 bg-[#221a06] shadow-[0_0_18px_4px_rgba(251,191,36,0.3)]"
      >
        <span className="absolute inset-[4px] rounded-[2px] bg-amber-300 shadow-[0_0_7px_1px_rgba(253,224,71,0.9)]" />
      </span>
      <span className="font-mono text-[0.6rem] tracking-[0.22em] text-amber-200/95 uppercase [text-shadow:0_0_14px_rgba(251,191,36,0.5)]">
        {label}
      </span>
    </div>
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

export function MessagingGraphicV9() {
  return (
    <div className="relative bg-[#04060b]">
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
                className="rounded-2xl border border-white/10 bg-[#070b14]/70 p-6 backdrop-blur-sm"
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
                className="rounded-2xl border border-white/10 bg-[#070b14]/70 p-6 backdrop-blur-sm"
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
                className="rounded-2xl border border-white/10 bg-[#070b14]/70 p-6 backdrop-blur-sm"
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
                <span className="border-cc-accent/60 text-cc-accent mt-4 inline-block rounded-lg border bg-[#070b14]/80 px-2.5 py-1.5 font-mono text-[0.65rem]">
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

        <div className="relative z-10">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold">
            Reliable by default.
          </h2>
          <p className="text-cc-ink mt-4 max-w-2xl text-base sm:text-lg">
            Delivery is where messaging systems quietly fail. Mocha makes the
            failure modes explicit and handles them for you.
          </p>
          <div className="mt-12 grid gap-5 md:grid-cols-2">
            <div className="rounded-2xl border border-white/10 bg-[#070b14]/70 p-6 backdrop-blur-sm">
              <p className="text-cc-heading font-heading text-h5 font-semibold">
                Transactional outbox
              </p>
              <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
                The database write and the message dispatch succeed or fail
                together.
              </p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-[#070b14]/70 p-6 backdrop-blur-sm">
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
      <section className="relative mx-auto max-w-6xl px-5 py-24 sm:px-12">
        <div className="relative z-10 mx-auto max-w-3xl rounded-2xl border border-white/10 bg-[#070b14]/70 p-6 text-center backdrop-blur-sm sm:p-8">
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.15em] uppercase">
            transports and patterns
          </p>
          <div className="mt-4 flex flex-wrap items-center justify-center gap-2">
            {TRANSPORTS.map((transport) => (
              <span
                key={transport}
                className="border-cc-accent/60 text-cc-accent rounded-lg border bg-[#070b14]/80 px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap"
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
