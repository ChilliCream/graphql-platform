"use client";

import { useEffect, useRef } from "react";

import {
  BOARD_ALPHA,
  Board,
  GLOW_RGB,
  MSG,
  MSG_SOFT,
  PULSE_TRAIL,
  Point,
  Pulse,
  RingFlash,
  envelope,
  generateBoard,
  mulberry32,
  paintBoard,
  pointAt,
} from "./board";
import { MONO_FONT } from "./palette";

interface NodeChipProps {
  readonly label: string;
  readonly designator: string;
  readonly className?: string;
}

function NodeChip({ label, designator, className }: NodeChipProps) {
  return (
    <div
      className={`pointer-events-none absolute z-10 flex flex-col items-center gap-1.5 ${className ?? ""}`}
    >
      <span
        data-mocha-node
        className="pointer-events-auto relative block h-2.5 w-2.5 rounded-full border border-[rgba(205,216,232,0.55)] transition-colors duration-300 hover:border-[rgba(246,202,190,0.9)]"
      >
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
    const reducedMql = window.matchMedia("(prefers-reduced-motion: reduce)");
    let reduced = reducedMql.matches;
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

    function prerender() {
      if (!board) {
        return;
      }
      off.width = Math.round(w * dpr);
      off.height = Math.round(h * dpr);
      const g = offCtx!;
      g.setTransform(dpr, 0, 0, dpr, 0, 0);
      g.clearRect(0, 0, w, h);
      paintBoard(g, board, w, h);
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
        root!.querySelectorAll<HTMLElement>("[data-mocha-node]"),
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

    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1]?.isIntersecting ?? false;
        sync();
      },
      { rootMargin: "60px" },
    );
    io.observe(root);
    const onVisibility = () => {
      sync();
    };
    document.addEventListener("visibilitychange", onVisibility);

    const onReducedChange = () => {
      reduced = reducedMql.matches;
      sync();
    };
    reducedMql.addEventListener("change", onReducedChange);

    let dprMql: MediaQueryList | null = null;
    const onDprChange = () => {
      if (disposed) {
        return;
      }
      measureAndBuild();
      render(0);
      watchDpr();
    };
    function watchDpr() {
      dprMql?.removeEventListener("change", onDprChange);
      dprMql = window.matchMedia(
        `(resolution: ${window.devicePixelRatio}dppx)`,
      );
      dprMql.addEventListener("change", onDprChange);
    }
    watchDpr();

    if (document.fonts?.ready) {
      document.fonts.ready.then(() => {
        if (!disposed) {
          measureAndBuild();
          render(0);
        }
      });
    }

    const RESIZE_DEBOUNCE_MS = 150;
    const RESIZE_THRESHOLD_PX = 4;
    let resizeTimer: ReturnType<typeof setTimeout> | null = null;
    const ro = new ResizeObserver(() => {
      if (resizeTimer !== null) {
        clearTimeout(resizeTimer);
      }
      resizeTimer = setTimeout(() => {
        resizeTimer = null;
        if (disposed) {
          return;
        }
        const dw = Math.abs(root.clientWidth - w);
        const dh = Math.abs(root.clientHeight - h);
        if (dw < RESIZE_THRESHOLD_PX && dh < RESIZE_THRESHOLD_PX && board) {
          return;
        }
        measureAndBuild();
        render(0);
      }, RESIZE_DEBOUNCE_MS);
    });
    ro.observe(root);

    return () => {
      disposed = true;
      stop();
      if (resizeTimer !== null) {
        clearTimeout(resizeTimer);
      }
      ro.disconnect();
      io.disconnect();
      document.removeEventListener("visibilitychange", onVisibility);
      reducedMql.removeEventListener("change", onReducedChange);
      dprMql?.removeEventListener("change", onDprChange);
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
