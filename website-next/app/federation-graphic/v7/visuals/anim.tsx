"use client";

/**
 * Shared animation runtime for the federation visuals, following the
 * messaging-page idiom: a requestAnimationFrame master loop over a fixed
 * period, keyframed with ramps and eases, driving SVG attributes directly
 * through a ref map (no React re-renders). The loop only runs while the
 * visual is in view and the tab is visible; under prefers-reduced-motion the
 * initial render is kept as a meaningful static frame.
 */

import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";

export type Pt = readonly [number, number];

export interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

export function measure(pts: readonly Pt[]): Polyline {
  const lens: number[] = [];
  let total = 0;
  for (let i = 0; i < pts.length - 1; i++) {
    const len = Math.hypot(
      pts[i + 1][0] - pts[i][0],
      pts[i + 1][1] - pts[i][1],
    );
    lens.push(len);
    total += len;
  }
  return { pts, lens, total };
}

export function pointAt(p: Polyline, u: number): Pt {
  const target = clamp01(u) * p.total;
  let acc = 0;
  for (let i = 0; i < p.lens.length; i++) {
    if (target <= acc + p.lens[i] || i === p.lens.length - 1) {
      const t = p.lens[i] === 0 ? 0 : (target - acc) / p.lens[i];
      const [ax, ay] = p.pts[i];
      const [bx, by] = p.pts[i + 1];
      return [ax + (bx - ax) * t, ay + (by - ay) * t];
    }
    acc += p.lens[i];
  }
  return p.pts[p.pts.length - 1];
}

export function clamp01(v: number): number {
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

export function ramp(t: number, a: number, b: number): number {
  return clamp01((t - a) / (b - a));
}

export function easeOutCubic(u: number): number {
  return 1 - Math.pow(1 - u, 3);
}

export function easeInOutCubic(u: number): number {
  return u < 0.5 ? 4 * u * u * u : 1 - Math.pow(-2 * u + 2, 3) / 2;
}

export interface AnimHelpers {
  readonly setO: (k: string, v: number) => void;
  readonly setPop: (k: string, o: number, rise: number) => void;
  readonly setDot: (k: string, x: number, y: number, r?: number) => void;
  readonly setRing: (k: string, s: number, r0: number, dr: number) => void;
  readonly setX: (k: string, x: number, y?: number) => void;
  readonly placePulse: (
    p: string,
    poly: Polyline,
    u: number,
    op: number,
    coreR: number,
  ) => void;
  readonly hidePulse: (p: string) => void;
}

export interface Visual {
  readonly rootRef: React.RefObject<HTMLDivElement | null>;
  readonly set: (k: string) => (node: SVGElement | null) => void;
}

export function useVisual(
  T: number,
  apply: (t: number, h: AnimHelpers) => void,
): Visual {
  const rootRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
  const applyRef = useRef(apply);
  useEffect(() => {
    applyRef.current = apply;
  }, [apply]);

  useEffect(() => {
    const root = rootRef.current;
    if (!root) {
      return;
    }
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    const E = els;
    const setO = (k: string, v: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("opacity", v.toFixed(3));
      }
    };
    const setPop = (k: string, o: number, rise: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("opacity", o.toFixed(3));
        el.setAttribute(
          "transform",
          `translate(0 ${((1 - rise) * 5).toFixed(2)})`,
        );
      }
    };
    const setDot = (k: string, x: number, y: number, r?: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("cx", x.toFixed(2));
        el.setAttribute("cy", y.toFixed(2));
        if (r !== undefined) {
          el.setAttribute("r", Math.max(0, r).toFixed(2));
        }
      }
    };
    const setRing = (k: string, s: number, r0: number, dr: number) => {
      const el = E.get(k);
      if (!el) {
        return;
      }
      if (s < 0 || s >= 1) {
        el.setAttribute("opacity", "0");
        return;
      }
      el.setAttribute("r", (r0 + dr * easeOutCubic(s)).toFixed(2));
      el.setAttribute("opacity", (0.5 * (1 - s)).toFixed(3));
    };
    const setX = (k: string, x: number, y = 0) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute(
          "transform",
          `translate(${x.toFixed(2)} ${y.toFixed(2)})`,
        );
      }
    };
    const placePulse = (
      p: string,
      poly: Polyline,
      u: number,
      op: number,
      coreR: number,
    ) => {
      const g = E.get(p);
      if (!g) {
        return;
      }
      if (op <= 0.01 || coreR <= 0.05) {
        g.setAttribute("opacity", "0");
        return;
      }
      g.setAttribute("opacity", op.toFixed(3));
      const d = clamp01(u) * poly.total;
      const [x, y] = pointAt(poly, u);
      setDot(p + "core", x, y, coreR);
      setDot(p + "in", x, y, coreR * 0.45);
      setDot(p + "glow", x, y, Math.max(0.6, coreR * 2.4));
      for (let k = 1; k <= 3; k++) {
        const dk = d - 7 * k;
        const el = E.get(p + "t" + k);
        if (el) {
          if (dk <= 0) {
            el.setAttribute("opacity", "0");
          } else {
            const [tx, ty] = pointAt(poly, dk / poly.total);
            el.setAttribute("cx", tx.toFixed(2));
            el.setAttribute("cy", ty.toFixed(2));
            el.setAttribute("opacity", (0.45 - 0.12 * k).toFixed(2));
          }
        }
      }
    };
    const hidePulse = (p: string) => setO(p, 0);

    const h: AnimHelpers = {
      setO,
      setPop,
      setDot,
      setRing,
      setX,
      placePulse,
      hidePulse,
    };

    let raf = 0;
    let running = false;
    let inView = false;
    let t = 0;
    let last = 0;

    const step = (now: number) => {
      const dt = Math.min(now - last, 50);
      last = now;
      t = (t + dt) % T;
      applyRef.current(t, h);
      raf = requestAnimationFrame(step);
    };
    const sync = () => {
      const should = inView && !document.hidden;
      if (should && !running) {
        running = true;
        last = performance.now();
        raf = requestAnimationFrame(step);
      } else if (!should && running) {
        running = false;
        cancelAnimationFrame(raf);
      }
    };
    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1].isIntersecting;
        sync();
      },
      { threshold: 0.2 },
    );
    io.observe(root);
    document.addEventListener("visibilitychange", sync);
    return () => {
      io.disconnect();
      document.removeEventListener("visibilitychange", sync);
      cancelAnimationFrame(raf);
    };
  }, [els, T]);

  const set = (k: string) => (node: SVGElement | null) => {
    els.set(k, node);
  };

  return { rootRef, set };
}

interface PulseDefProps {
  readonly set: Visual["set"];
  readonly id: string;
  readonly main: string;
  readonly soft: string;
  readonly filter: string;
}

/** One traveling pulse: glow + core + inner + three fading trail dots. */
export function PulseGlyph({ set, id, main, soft, filter }: PulseDefProps) {
  return (
    <g ref={set(id)} opacity={0}>
      <circle ref={set(id + "t3")} r={1.4} fill={main} opacity={0} />
      <circle ref={set(id + "t2")} r={1.7} fill={main} opacity={0} />
      <circle ref={set(id + "t1")} r={2} fill={main} opacity={0} />
      <circle
        ref={set(id + "glow")}
        r={6}
        fill={main}
        opacity={0.22}
        filter={`url(#${filter})`}
      />
      <circle ref={set(id + "core")} r={2.5} fill={main} />
      <circle ref={set(id + "in")} r={1.1} fill={soft} />
    </g>
  );
}

interface VisualCardProps {
  readonly rootRef: React.RefObject<HTMLDivElement | null>;
  readonly children: ReactNode;
}

/** House card container for a looping visual. */
export function VisualCard({ rootRef, children }: VisualCardProps) {
  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative w-full overflow-hidden rounded-2xl border p-5 backdrop-blur"
    >
      {children}
    </div>
  );
}

export const HAIR = "rgba(139,160,188,0.22)";
export const LANE = "rgba(139,160,188,0.45)";
export const PANEL_STROKE = "rgba(139,160,188,0.28)";
export const SURFACE = "#0c1322";
export const DIM = "#62748e";
