"use client";

import { useReducedMotion } from "motion/react";
import { useEffect, useRef } from "react";

interface RisingParticlesProps {
  readonly className?: string;
  readonly count?: number;
  /** Comma-separated rgb triple, e.g. "180,205,255". */
  readonly color?: string;
}

/** A light canvas-2D field of soft motes drifting upward (additive blend). */
export function RisingParticles({
  className,
  count = 42,
  color = "180,205,255",
}: RisingParticlesProps) {
  const reduced = useReducedMotion();
  const ref = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    if (!ctx) {
      return;
    }

    let dpr = 1;
    const rand = (i: number) => {
      const x = Math.sin(i * 127.1 + 11.3) * 43758.5453;
      return x - Math.floor(x);
    };
    const parts = Array.from({ length: count }, (_, i) => ({
      x: rand(i * 3 + 1),
      y: rand(i * 3 + 2),
      r: 0.6 + rand(i * 3 + 3) * 1.4,
      sp: 0.004 + rand(i + 9) * 0.012,
      tw: rand(i + 21),
    }));

    const resize = () => {
      const cw = canvas.clientWidth;
      const ch = canvas.clientHeight;
      const base = Math.min(window.devicePixelRatio || 1, 2);
      // Cap total pixels (~4M) so a full-page-tall canvas stays cheap.
      const cap = Math.sqrt(4_000_000 / Math.max(1, cw * ch));
      dpr = Math.max(0.75, Math.min(base, cap));
      canvas.width = Math.max(1, Math.round(cw * dpr));
      canvas.height = Math.max(1, Math.round(ch * dpr));
    };
    const observer = new ResizeObserver(resize);
    observer.observe(canvas);
    resize();

    const start = performance.now();
    const draw = () => {
      const t = (performance.now() - start) / 1000;
      const w = canvas.width;
      const h = canvas.height;
      ctx.clearRect(0, 0, w, h);
      ctx.globalCompositeOperation = "lighter";
      for (const p of parts) {
        const y = (((p.y - t * p.sp) % 1) + 1) % 1;
        const px = p.x * w;
        const py = y * h;
        const tw =
          0.4 + 0.6 * Math.abs(Math.sin(t * (1 + p.tw * 2) + p.tw * 6));
        const a = tw * (0.3 + 0.4 * (1 - y));
        const rad = p.r * dpr * 3;
        const g = ctx.createRadialGradient(px, py, 0, px, py, rad);
        g.addColorStop(0, `rgba(${color},${a})`);
        g.addColorStop(1, `rgba(${color},0)`);
        ctx.fillStyle = g;
        ctx.beginPath();
        ctx.arc(px, py, rad, 0, Math.PI * 2);
        ctx.fill();
      }
    };

    let raf = 0;
    const loop = () => {
      draw();
      raf = requestAnimationFrame(loop);
    };
    if (reduced) {
      draw();
    } else {
      raf = requestAnimationFrame(loop);
    }

    return () => {
      cancelAnimationFrame(raf);
      observer.disconnect();
    };
  }, [count, color, reduced]);

  return (
    <canvas
      ref={ref}
      aria-hidden="true"
      className={className}
      style={{ display: "block", width: "100%", height: "100%" }}
    />
  );
}
