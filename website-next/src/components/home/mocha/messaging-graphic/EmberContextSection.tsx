"use client";

/**
 * Ember-context slide, first pass at the V7-style reference: a warm, near-black
 * stage lit by a soft ember glow with drifting sparks, a thin inset frame, a
 * serif two-line headline on the left, and on the right a stack of dashed
 * "year" rings (the firm's deal history) fading down into the dark. A coral
 * spark and a bracket mark the slice a context window can actually hold.
 *
 * Layout matches the reference at desktop widths; on small screens the graphic
 * is hidden and the text stacks. Copy and brand mark are placeholders to
 * iterate on.
 */

import { useEffect, useRef } from "react";

export const SERIF = "'Iowan Old Style', Georgia, 'Times New Roman', serif";

/** Dashed year rings, brightest at the top and fading into the dark. */
const RINGS = [
  { cy: 44, op: 0.85, label: null },
  { cy: 104, op: 1.0, label: "2026" },
  { cy: 172, op: 0.7, label: "2025" },
  { cy: 220, op: 0.5, label: "2024" },
  { cy: 256, op: 0.34, label: "2023" },
  { cy: 284, op: 0.23, label: "2022" },
  { cy: 308, op: 0.15, label: "2021" },
  { cy: 330, op: 0.09, label: "2020" },
] as const;

interface Ember {
  x: number;
  y: number;
  r: number;
  vx: number;
  vy: number;
  base: number;
  phase: number;
  speed: number;
  warm: number;
}

export function EmberField() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    const host = canvas.parentElement;
    if (!ctx || !host) {
      return;
    }
    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;

    let w = 0;
    let h = 0;
    let dpr = 1;
    let embers: Ember[] = [];
    let raf = 0;
    let last = 0;
    let disposed = false;

    function build() {
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      const r = host!.getBoundingClientRect();
      w = r.width;
      h = r.height;
      canvas!.width = Math.round(w * dpr);
      canvas!.height = Math.round(h * dpr);
      const count = Math.round(Math.min(70, (w * h) / 24000));
      embers = Array.from({ length: count }, () => {
        // Bias toward the bottom and top bands, like settling dust.
        const band = Math.random();
        const y =
          band < 0.6
            ? h * (0.62 + Math.random() * 0.36)
            : h * Math.random() * 0.3;
        return {
          x: Math.random() * w,
          y,
          r: 0.4 + Math.random() * 1.7,
          vx: (Math.random() - 0.5) * 6,
          vy: 4 + Math.random() * 16,
          base: 0.12 + Math.random() * 0.5,
          phase: Math.random() * Math.PI * 2,
          speed: 0.6 + Math.random() * 1.6,
          warm: Math.random(),
        };
      });
    }

    function draw(time: number) {
      ctx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx!.clearRect(0, 0, w, h);
      ctx!.globalCompositeOperation = "lighter";
      for (const e of embers) {
        const tw = 0.45 + 0.55 * Math.sin(time * 0.001 * e.speed + e.phase);
        const a = e.base * tw;
        const g = ctx!.createRadialGradient(e.x, e.y, 0, e.x, e.y, e.r * 6);
        const rC = 250;
        const gC = Math.round(150 + e.warm * 40);
        const bC = Math.round(70 + e.warm * 30);
        g.addColorStop(0, `rgba(${rC}, ${gC}, ${bC}, ${a})`);
        g.addColorStop(1, `rgba(${rC}, ${gC}, ${bC}, 0)`);
        ctx!.fillStyle = g;
        ctx!.beginPath();
        ctx!.arc(e.x, e.y, e.r * 6, 0, Math.PI * 2);
        ctx!.fill();
        ctx!.fillStyle = `rgba(255, 220, 180, ${a * 0.9})`;
        ctx!.beginPath();
        ctx!.arc(e.x, e.y, e.r * 0.7, 0, Math.PI * 2);
        ctx!.fill();
      }
    }

    function step(dt: number) {
      for (const e of embers) {
        e.y -= e.vy * dt;
        e.x += e.vx * dt;
        if (e.y < -12 || e.x < -12 || e.x > w + 12) {
          e.x = Math.random() * w;
          e.y = h + 8;
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
      draw(time);
      raf = requestAnimationFrame(loop);
    }

    build();
    if (reduced) {
      draw(0);
    } else {
      raf = requestAnimationFrame(loop);
    }

    let resizeRaf = 0;
    const ro = new ResizeObserver(() => {
      cancelAnimationFrame(resizeRaf);
      resizeRaf = requestAnimationFrame(() => {
        build();
        if (reduced) {
          draw(0);
        }
      });
    });
    ro.observe(host);

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

// Rounded at module load so server and client serialize identical attributes.
const SPARK_SPOKES = Array.from({ length: 12 }, (_, i) => {
  const a = (i * Math.PI) / 6;
  const round = (v: number) => Math.round(v * 1000) / 1000;
  return {
    x1: round(Math.cos(a) * 4),
    y1: round(Math.sin(a) * 4),
    x2: round(Math.cos(a) * 18),
    y2: round(Math.sin(a) * 18),
  };
});

/** The coral spark to the left of the ring stack. */
export function Spark({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="-24 -24 48 48"
      className={className}
      style={{
        filter: "drop-shadow(0 0 8px rgba(242,100,60,0.7))",
        animation: "spin 26s linear infinite",
      }}
      aria-hidden="true"
    >
      {SPARK_SPOKES.map((s, i) => (
        <line
          key={i}
          x1={s.x1}
          y1={s.y1}
          x2={s.x2}
          y2={s.y2}
          stroke="#f2643c"
          strokeWidth={2}
          strokeLinecap="round"
        />
      ))}
    </svg>
  );
}

/** The dashed cylinder of year rings, fading down into the dark. */
function YearStack({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 260 360"
      className={className}
      fill="none"
      aria-hidden="true"
    >
      {/* Bracket marking what a context window can hold (top ring only). */}
      <g stroke="rgba(255,255,255,0.16)" strokeWidth={1}>
        <path d="M22 26 H14 V138 H22" />
        <path d="M238 26 H246 V138 H238" />
      </g>

      {RINGS.map((ring) => (
        <g key={ring.cy}>
          <ellipse
            cx={130}
            cy={ring.cy}
            rx={94}
            ry={21}
            stroke="#ffffff"
            strokeOpacity={ring.op}
            strokeWidth={1}
            strokeDasharray="4 5"
          />
          {ring.label ? (
            <text
              x={130}
              y={ring.cy + 3.5}
              textAnchor="middle"
              fill="#ffffff"
              fillOpacity={Math.min(1, ring.op + 0.05)}
              style={{
                font: "600 10px ui-monospace, SFMono-Regular, Menlo, monospace",
                letterSpacing: "0.12em",
              }}
            >
              {ring.label}
            </text>
          ) : null}
        </g>
      ))}
    </svg>
  );
}

export function EmberContextSection() {
  return (
    <section className="relative min-h-svh overflow-hidden bg-[#0b0706]">
      {/* Warm ember glow, offset toward the ring stack. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            "radial-gradient(46rem 34rem at 60% 47%, rgba(214,104,44,0.16), rgba(150,66,26,0.07) 42%, rgba(0,0,0,0) 72%), radial-gradient(16rem 12rem at 57% 44%, rgba(255,168,96,0.12), rgba(0,0,0,0) 70%)",
        }}
      />
      <EmberField />

      {/* Inset frame. */}
      <div className="absolute inset-5 rounded-[3px] border border-white/[0.08] sm:inset-8 lg:inset-10">
        {/* Registration ticks, top and bottom center. */}
        <span className="absolute top-0 left-1/2 h-5 w-px -translate-x-1/2 bg-white/10" />
        <span className="absolute bottom-0 left-1/2 h-5 w-px -translate-x-1/2 bg-white/10" />

        <span className="text-cc-ink-faint absolute top-7 left-7 font-mono text-[0.6rem] tracking-[0.32em] text-white/30 uppercase sm:top-9 sm:left-11">
          Why
        </span>

        {/* Headline. */}
        <div className="absolute top-1/2 left-7 max-w-[20rem] -translate-y-1/2 sm:left-11 sm:max-w-[34rem]">
          <h2
            style={{ fontFamily: SERIF }}
            className="text-[1.7rem] leading-[1.22] tracking-[-0.01em] sm:text-[2.05rem] lg:text-[2.4rem]"
          >
            <span className="block text-[#b7a89a]">
              Top LLMs cap at a 1M token context window.
            </span>
            <span className="mt-1 block text-[#f3ede4]">
              Your firm has 15 years of deal history.
            </span>
          </h2>
        </div>

        {/* Body. */}
        <p className="absolute bottom-8 left-7 max-w-md text-[0.82rem] leading-relaxed text-white/45 sm:bottom-11 sm:left-11">
          Without a context layer, your model sees a fraction of what it needs.
          <br />
          It forgets everything the moment the session ends.
        </p>

        {/* Ring stack + spark (desktop). */}
        <div className="absolute top-1/2 right-[15%] hidden -translate-y-1/2 md:block">
          <Spark className="absolute -top-2 -left-12 h-11 w-11" />
          <YearStack className="h-[22rem] w-[16rem]" />
        </div>

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
