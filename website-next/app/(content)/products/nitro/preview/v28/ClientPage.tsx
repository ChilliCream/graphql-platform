"use client";

import { useReducedMotion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";
import { useEffect, useRef } from "react";

import { ControlPlaneConsole } from "@/src/components/nitro/ControlPlaneConsole";
import { RisingParticles } from "@/src/components/nitro/RisingParticles";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  BarSeries,
  ChartPanel,
  CountUp,
  HBarSeries,
  InsightsTable,
  LineAreaChart,
  NitroCompose,
  NitroDiagnose,
  NitroFusion,
  NitroReel,
  NitroSchema,
  NitroTheme,
  NitroTrace,
  Sparkline,
  token,
  TraceWaterfall,
} from "@/src/nitro";
import type { Client, InsightRow, Trace } from "@/src/nitro/lib/data/types";

// Brand spectrum gradient, the landing-hero grade: cyan -> periwinkle -> mauve
// -> coral. Used sparingly as the single "color event" per Linear.
const SPECTRUM =
  "linear-gradient(100deg, #16b9e4 0%, #7c92c6 40%, #b681a9 68%, #f0786a 100%)";

/* ────────────────────────────────────────────────────────────────────────
   Shared shells
   ──────────────────────────────────────────────────────────────────────── */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span
      className="text-caption font-medium tracking-[0.22em] uppercase"
      style={{ color: "#16b9e4" }}
    >
      {children}
    </span>
  );
}

interface SectionIntroProps {
  readonly index?: string;
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly lead?: string;
  readonly align?: "center" | "start";
}

function SectionIntro({
  index,
  eyebrow,
  title,
  lead,
  align = "center",
}: SectionIntroProps) {
  return (
    <div
      className={[
        "flex flex-col gap-4",
        align === "center" ? "mx-auto max-w-2xl text-center" : "max-w-md",
      ].join(" ")}
    >
      <div
        className={[
          "flex items-center gap-3",
          align === "center" ? "justify-center" : "",
        ].join(" ")}
      >
        {index && (
          <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
            {index}
          </span>
        )}
        <Eyebrow>{eyebrow}</Eyebrow>
      </div>
      <h2 className="text-cc-heading font-heading text-h3 text-balance">
        {title}
      </h2>
      {lead && (
        <p className="text-cc-ink text-base leading-relaxed text-pretty sm:text-lg">
          {lead}
        </p>
      )}
    </div>
  );
}

interface CardProps {
  readonly className?: string;
  readonly children: ReactNode;
  readonly glow?: boolean;
}

/** Glassy, hairline-bordered surface: transparent enough for the page
    atmosphere to glow through, still legible for text and data. */
function Card({ className, children, glow = false }: CardProps) {
  return (
    <div
      className={[
        "relative overflow-hidden rounded-2xl border border-white/10 bg-white/[0.03] backdrop-blur-sm",
        className ?? "",
      ].join(" ")}
    >
      {glow && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-24 right-0 -z-0 h-56 w-56 opacity-40 blur-3xl"
          style={{
            background:
              "radial-gradient(50% 50% at 60% 40%, rgba(22,185,228,0.18), transparent 70%)",
          }}
        />
      )}
      <div className="relative z-10 flex h-full flex-col">{children}</div>
    </div>
  );
}

interface CardHeaderProps {
  readonly index: string;
  readonly title: string;
  readonly hint?: string;
}

function CardHeader({ index, title, hint }: CardHeaderProps) {
  return (
    <div className="flex items-baseline justify-between gap-3 px-5 pt-5">
      <div className="flex items-center gap-2.5">
        <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
          {index}
        </span>
        <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
      </div>
      {hint && (
        <span className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
          {hint}
        </span>
      )}
    </div>
  );
}

interface NitroCanvasProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: CSSProperties;
}

/** Wraps chart primitives so their `--t-*` token vars resolve; stays transparent. */
function NitroCanvas({ children, className, style }: NitroCanvasProps) {
  return (
    <NitroTheme
      theme="dark"
      reducedMotion="never"
      className={className}
      style={{ background: "transparent", ...style }}
    >
      {children}
    </NitroTheme>
  );
}

/** Frames a chrome-less Nitro product screen like an embedded screenshot. */
interface FramedVisualProps {
  readonly children: ReactNode;
}

function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-x-6 -inset-y-4 -z-10 rounded-[2rem] opacity-40 blur-3xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 40%, rgba(22,185,228,0.16), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
        {children}
      </div>
    </div>
  );
}

interface ShowcaseProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly visual: ReactNode;
  readonly aside?: ReactNode;
  readonly reverse?: boolean;
}

/** Alternating split: short headline + graphic, Linear feature-row rhythm. */
function Showcase({
  id,
  index,
  eyebrow,
  title,
  body,
  visual,
  aside,
  reverse = false,
}: ShowcaseProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <RevealOnScroll
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex flex-col gap-5">
            <SectionIntro
              index={index}
              eyebrow={eyebrow}
              title={title}
              align="start"
            />
            <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
              {body}
            </p>
            {aside}
          </div>
        </RevealOnScroll>

        <RevealOnScroll
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
          hiddenClassName="translate-y-8 opacity-0"
        >
          <FramedVisual>{visual}</FramedVisual>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Hero aurora beam (signature visual): a real-aurora beam descending from the
   top that flares into a wide aurora burst at the bottom-center, pouring
   light into and behind the crisp product reel. Graded on the landing-hero
   spectrum: white-hot core -> cyan #16b9e4 as the dominant curtain, with
   periwinkle #7c92c6, mauve #b681a9, and coral #f0786a accents at the fringes.
   Spans the full viewport width and fades to navy #0b0f1a at every edge so it
   dissolves into the page atmosphere. Everything stills under
   prefers-reduced-motion.
   ──────────────────────────────────────────────────────────────────────── */

// Stacked conic fans: the defining triangular burst. Each widens downward from
// the apex; screen-blended and graded coral fringe -> mauve -> periwinkle ->
// cyan inner -> cool-white core, so the light reads as a saturated multi-color
// cone pouring over the reel, not a flat wash.
const CONES: readonly {
  readonly halfAngle: number; // degrees off vertical to each edge
  readonly color: string; // "r,g,b"
  readonly peak: number; // opacity at the bright axis
  readonly mid: number; // opacity mid-way to the edge
  readonly blur: number; // px
}[] = [
  { halfAngle: 37, color: "240,120,106", peak: 0.4, mid: 0.18, blur: 50 },
  { halfAngle: 30, color: "182,129,169", peak: 0.52, mid: 0.24, blur: 44 },
  { halfAngle: 22, color: "124,146,198", peak: 0.58, mid: 0.28, blur: 36 },
  { halfAngle: 15, color: "22,185,228", peak: 0.66, mid: 0.32, blur: 28 },
  { halfAngle: 9, color: "196,236,247", peak: 0.9, mid: 0.46, blur: 20 },
];

// Aurora curtains: vertical striations splaying wide from the apex across the
// fan, giving the shimmering ribbed texture (px widths, degrees of splay).
const CURTAINS: readonly {
  readonly rot: number;
  readonly width: number;
  readonly height: number;
  readonly opacity: number;
  readonly hue: string;
}[] = [
  {
    rot: -27,
    width: 3,
    height: 470,
    opacity: 0.24,
    hue: "rgba(240,120,106,0.6)",
  },
  {
    rot: -18,
    width: 3,
    height: 510,
    opacity: 0.34,
    hue: "rgba(182,129,169,0.68)",
  },
  {
    rot: -10,
    width: 2.5,
    height: 545,
    opacity: 0.44,
    hue: "rgba(22,185,228,0.8)",
  },
  {
    rot: -4,
    width: 2,
    height: 565,
    opacity: 0.5,
    hue: "rgba(206,240,248,0.9)",
  },
  {
    rot: 4,
    width: 2,
    height: 565,
    opacity: 0.5,
    hue: "rgba(200,236,247,0.88)",
  },
  {
    rot: 10,
    width: 2.5,
    height: 545,
    opacity: 0.44,
    hue: "rgba(22,185,228,0.78)",
  },
  {
    rot: 18,
    width: 3,
    height: 510,
    opacity: 0.34,
    hue: "rgba(124,146,198,0.68)",
  },
  {
    rot: 27,
    width: 3,
    height: 470,
    opacity: 0.24,
    hue: "rgba(240,120,106,0.6)",
  },
];

// Beam-impact anchor, shared by the aurora beam, the reel glare, and the hero
// sparks so all three stay locked together at every viewport width. The strike
// sits a fixed 288px right of viewport center (the reel's 3/4 point at
// max-w-6xl: 1152px * 0.75 from the left edge == center + 288) and ~505px down
// from the hero top, right on the reel's top edge.
const STRIKE_X_OFFSET = 288;
const STRIKE_Y = 505;

// Mote tints, taken verbatim from the v18 RisingMotes palette so the sparks read
// exactly like the v18 motes: warm white (weighted), gold, warm coral, faint cool.
const MOTE_TINTS = [
  { fill: "255,246,232", glow: "255,224,168" }, // warm white
  { fill: "255,236,196", glow: "244,180,94" }, // gold
  { fill: "255,246,232", glow: "255,224,168" }, // warm white (weighted)
  { fill: "255,224,196", glow: "242,140,90" }, // warm coral
  { fill: "222,240,246", glow: "143,201,221" }, // faint cool
] as const;

interface HeroSparksProps {
  readonly reduced: boolean;
  readonly className?: string;
}

/** Canvas-2D motes rising from the beam-impact point on the reel's top edge,
    a gentle warm ember spread in the feel of the v18 sunrise motes: a fairly
    dense, well-spaced field that lifts slowly and fans out in a tight-ish cone
    as it climbs, fading in off the strike and easing out near the top. The
    origin tracks the shared strike anchor (center + STRIKE_X_OFFSET, STRIKE_Y)
    so it stays aligned with the beam and the glare at every viewport width. */
function HeroSparks({ reduced, className }: HeroSparksProps) {
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

    // How far a mote climbs above the strike (px), and how wide the fan opens at
    // full height (px, half-width to each side). Kept tight so the plume reads as
    // a spaced rising fan from the impact point, matching the v18 mote feel.
    const RISE_PX = 340;
    const FAN_PX = 150;

    // A plume of motes in the v18 model: `life` runs 0 at the strike to 1 near
    // the top; density, gentle rise speed, tight eased fan, and alpha fades all
    // mirror the v18 RisingMotes so the spread reads the same, just emitted from
    // the beam-impact point instead of the reel's bottom-center.
    const motes = Array.from({ length: 54 }, (_, i) => ({
      life: rand(i * 9 + 1),
      fan: (rand(i * 9 + 2) - 0.5) * 2, // -1..1, how far it drifts off the axis
      wobPhase: rand(i * 9 + 3) * Math.PI * 2,
      wobSpeed: 0.4 + rand(i * 9 + 4) * 0.9,
      r: 0.5 + rand(i * 9 + 5) * 2.1,
      speed: 0.04 + rand(i * 9 + 6) * 0.07, // life units per second
      base: 0.32 + rand(i * 9 + 7) * 0.52,
      phase: rand(i * 9 + 8) * Math.PI * 2,
      twSpeed: 0.6 + rand(i * 9 + 9) * 1.4,
      tint: MOTE_TINTS[Math.floor(rand(i * 9 + 10) * MOTE_TINTS.length)],
    }));

    // Smooth 0→1 ramp used to fade motes in/out along their rising life.
    const smoothstep = (a: number, b: number, x: number) => {
      const s = Math.min(1, Math.max(0, (x - a) / (b - a)));
      return s * s * (3 - 2 * s);
    };

    let cw = 0;
    let ch = 0;
    const resize = () => {
      cw = canvas.clientWidth;
      ch = canvas.clientHeight;
      const base = Math.min(window.devicePixelRatio || 1, 2);
      const cap = Math.sqrt(4_000_000 / Math.max(1, cw * ch));
      dpr = Math.max(0.75, Math.min(base, cap));
      canvas.width = Math.max(1, Math.round(cw * dpr));
      canvas.height = Math.max(1, Math.round(ch * dpr));
      // Draw in CSS pixels; the transform handles dpr (same as v18 RisingMotes).
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };
    const observer = new ResizeObserver(resize);
    observer.observe(canvas);
    resize();

    let last = performance.now();
    const start = last;
    const draw = () => {
      const now = performance.now();
      const t = (now - start) / 1000;
      const dt = reduced ? 0 : Math.min(0.05, (now - last) / 1000);
      last = now;
      // Origin in CSS pixels: shared strike anchor. Horizontal offset is a fixed
      // px value from center, matching the beam/glare exactly.
      const ox = cw / 2 + STRIKE_X_OFFSET;
      const oy = STRIKE_Y;
      ctx.clearRect(0, 0, cw, ch);
      ctx.shadowBlur = 7;
      for (const m of motes) {
        if (!reduced) {
          m.life += m.speed * dt;
          if (m.life > 1) {
            // Respawn at the strike with a fresh fan offset.
            m.life -= 1;
            m.fan = (rand(m.phase * 1000 + t) - 0.5) * 2;
          }
        }
        // Rise from the strike; the fan opens from a tight point (eased by
        // life²) and a gentle wobble keeps the plume alive.
        const wobble = Math.sin(m.wobPhase + t * m.wobSpeed) * 6;
        const spread = 0.05 + 0.95 * (m.life * m.life);
        const px = ox + m.fan * FAN_PX * spread + wobble;
        const py = oy - m.life * RISE_PX;
        // Fade in as it lifts off the strike, ease out as it nears the top.
        const edge =
          smoothstep(0, 0.16, m.life) * (1 - smoothstep(0.78, 1, m.life));
        const twinkle = 0.55 + 0.45 * Math.sin(m.phase + t * m.twSpeed);
        const a = m.base * twinkle * edge;
        if (a <= 0.01) {
          continue;
        }
        // Exactly the v18 RisingMotes draw: a solid tinted dot with a soft shadow
        // glow, no per-particle gradient (that per-frame cost was the slowdown).
        ctx.fillStyle = `rgba(${m.tint.fill},1)`;
        ctx.shadowColor = `rgba(${m.tint.glow},0.9)`;
        ctx.globalAlpha = a;
        ctx.beginPath();
        ctx.arc(px, py, m.r, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.globalAlpha = 1;
      ctx.shadowBlur = 0;
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
  }, [reduced]);

  return (
    <canvas
      ref={ref}
      aria-hidden="true"
      className={className}
      style={{ display: "block", width: "100%", height: "100%" }}
    />
  );
}

interface HeroAuroraProps {
  readonly reduced: boolean;
}

function HeroAurora({ reduced }: HeroAuroraProps) {
  // y-offset (px from hero top) of the reel's top edge, where the beam strikes
  // and blooms white-hot. Every base-flare layer keys off this.
  const baseY = 530;
  const screen = { mixBlendMode: "screen" as const };
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 z-0 overflow-hidden"
    >
      <style>{`
        @keyframes v22-breathe {
          0%, 100% { opacity: 0.9; }
          50% { opacity: 1; }
        }
        /* Suppress the reel's per-tab benefit headline in the hero: it landed
           inside the beam, center-aligned and washed out. The hero has its
           own H1 + lead, so this floating block is dropped here only. */
        .v22-hero-reel [role="group"] > div:first-child { display: none !important; }
      `}</style>

      {/* Ambient atmosphere: an aurora sky spread across the whole width,
          coral bleeding in from the right, cyan filling the crown, periwinkle
          cooling the left, so no quadrant is dead space. */}
      <div
        className="absolute inset-0"
        style={{
          ...screen,
          background:
            "radial-gradient(130% 80% at 50% -12%, rgba(22,185,228,0.24), transparent 58%), radial-gradient(58% 50% at 86% 6%, rgba(240,120,106,0.24), transparent 64%), radial-gradient(52% 44% at 8% 6%, rgba(124,146,198,0.22), transparent 62%)",
        }}
      />
      {/* Soft aurora cloud haze for atmospheric depth: two offset blooms so the
          sky reads as layered cloud, not a flat blob. */}
      <div
        className="absolute"
        style={{
          ...screen,
          top: 10,
          right: -60,
          width: 820,
          height: 580,
          filter: "blur(88px)",
          background:
            "radial-gradient(48% 48% at 62% 32%, rgba(22,185,228,0.13), transparent 72%), radial-gradient(40% 40% at 88% 60%, rgba(240,120,106,0.11), transparent 74%)",
        }}
      />

      {/* Breathing wrapper around the beam + burst. */}
      <div
        className="absolute inset-0"
        style={{
          // Fixed px offset from viewport center (== the glare's anchor), so the
          // beam and the glare stay aligned at every screen width. 288px is the
          // reel's 3/4 point at max-w-6xl (1152px / 4).
          transform: "translateX(288px)",
          ...(reduced
            ? {}
            : { animation: "v22-breathe 9s ease-in-out infinite" }),
        }}
      >
        {/* THE FAN: stacked conic cones widening from the apex to a wide base
            over the reel, graded teal -> cyan -> white-hot core. This is the
            dramatic triangular burst, not a soft oval. */}
        {CONES.map((c, i) => (
          <div
            key={i}
            className="absolute left-1/2 -translate-x-1/2"
            style={{
              ...screen,
              top: 24,
              height: baseY - 24,
              width: 1180,
              filter: `blur(${c.blur}px)`,
              maskImage:
                "linear-gradient(to bottom, transparent 1%, rgba(0,0,0,0.55) 15%, #000 42%, #000 100%)",
              WebkitMaskImage:
                "linear-gradient(to bottom, transparent 1%, rgba(0,0,0,0.55) 15%, #000 42%, #000 100%)",
              background: `conic-gradient(from 0deg at 50% 0%, transparent 0deg, transparent ${
                180 - c.halfAngle
              }deg, rgba(${c.color},${c.mid}) ${
                180 - c.halfAngle * 0.5
              }deg, rgba(${c.color},${c.peak}) 180deg, rgba(${c.color},${
                c.mid
              }) ${180 + c.halfAngle * 0.5}deg, transparent ${
                180 + c.halfAngle
              }deg, transparent 360deg)`,
            }}
          />
        ))}

        {/* Repeating vertical striations (aurora grain), masked to the fan. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: 40,
            height: baseY - 10,
            width: 760,
            filter: "blur(1px)",
            background:
              "repeating-linear-gradient(90deg, transparent 0 10px, rgba(206,240,248,0.07) 10px 12px)",
            maskImage:
              "radial-gradient(50% 62% at 50% 60%, #000 8%, transparent 76%)",
            WebkitMaskImage:
              "radial-gradient(50% 62% at 50% 60%, #000 8%, transparent 76%)",
          }}
        />

        {/* Curtain streaks splaying wide from the apex across the fan. */}
        {CURTAINS.map((c, i) => (
          <div
            key={i}
            className="absolute left-1/2 blur-[10px]"
            style={{
              ...screen,
              top: 26,
              width: c.width,
              height: c.height,
              opacity: c.opacity,
              transform: `translateX(-50%) rotate(${c.rot}deg)`,
              transformOrigin: "top center",
              background: `linear-gradient(to bottom, ${c.hue} 0%, transparent 100%)`,
            }}
          />
        ))}

        {/* Signature bright filament from the very top down into the burst. */}
        <div
          className="absolute top-0 left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            height: baseY,
            width: 6,
            filter: "blur(4px)",
            background:
              "linear-gradient(to bottom, rgba(255,255,255,0.9) 0%, rgba(196,236,247,0.7) 46%, rgba(22,185,228,0.55) 100%)",
          }}
        />
        <div
          className="absolute top-0 left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            height: baseY - 20,
            width: 2,
            filter: "blur(0.5px)",
            background:
              "linear-gradient(to bottom, rgba(255,255,255,1) 0%, rgba(226,244,250,0.9) 55%, rgba(226,244,250,0.4) 100%)",
          }}
        />

        {/* Base flare: the money shot where the beam strikes the reel. Every
            layer focuses at 50% 100% and bottoms out at the reel edge, so the
            light blooms UP from the strike point and pours into the window. */}
        {/* Wide cyan fan spreading across the top of the reel, with a
            coral fringe bleeding into the outer edge. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 300,
            height: 300,
            width: 1120,
            filter: "blur(90px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(22,185,228,0.42), rgba(240,120,106,0.16) 52%, transparent 80%)",
          }}
        />
        {/* Coral/mauve lower fringe on the left flank, the warm hue an aurora
            picks up beneath the cyan curtain. */}
        <div
          className="absolute left-[24%]"
          style={{
            ...screen,
            top: baseY - 280,
            height: 280,
            width: 600,
            filter: "blur(78px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(240,120,106,0.5), rgba(182,129,169,0.24) 46%, transparent 78%)",
          }}
        />
        {/* Periwinkle lower fringe on the right flank, cooling the opposite edge. */}
        <div
          className="absolute right-[22%]"
          style={{
            ...screen,
            top: baseY - 260,
            height: 260,
            width: 560,
            filter: "blur(78px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(124,146,198,0.44), transparent 78%)",
          }}
        />
        {/* Cyan fan (#16b9e4), the dominant curtain color. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 280,
            height: 280,
            width: 840,
            filter: "blur(70px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(22,185,228,0.58), transparent 74%)",
          }}
        />
        {/* Cyan burst (#16b9e4), the saturated heart, warmed with a periwinkle
            fringe. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 250,
            height: 250,
            width: 620,
            filter: "blur(50px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(22,185,228,0.7), rgba(124,146,198,0.34) 44%, transparent 74%)",
          }}
        />
        {/* White-hot elliptical bloom (~620x220) sitting just above the reel. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 220,
            height: 220,
            width: 620,
            filter: "blur(64px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(255,255,255,0.9), rgba(22,185,228,0.5) 40%, transparent 72%)",
          }}
        />
        {/* Impact hue: a wider spectrum bloom centered on the strike point so the
            beam clearly lands on the window, spilling a little onto its top edge. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 150,
            height: 300,
            width: 740,
            filter: "blur(38px)",
            background:
              "radial-gradient(ellipse 50% 46% at 50% 50%, rgba(255,255,255,0.5), rgba(22,185,228,0.42) 30%, rgba(124,146,198,0.26) 52%, rgba(240,120,106,0.14) 70%, transparent 84%)",
          }}
        />
        {/* Bright vertical flare-up spike where the filament strikes the edge. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 300,
            height: 300,
            width: 74,
            filter: "blur(32px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(255,255,255,0.85), rgba(196,236,247,0.4) 42%, transparent 72%)",
          }}
        />
        {/* Broad white band spreading the burst across the reel's top edge. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 120,
            height: 120,
            width: 720,
            filter: "blur(44px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(230,244,250,0.42), transparent 72%)",
          }}
        />
        {/* Tight white core right at the foot of the beam. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 140,
            height: 140,
            width: 220,
            filter: "blur(24px)",
            background:
              "radial-gradient(ellipse 100% 100% at 50% 100%, rgba(255,255,255,0.92), rgba(206,240,248,0.42) 46%, transparent 72%)",
          }}
        />
        {/* Hot horizontal spread lying right on the reel's top edge, so the
            dashboard reads as lit from the descending beam. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 60,
            height: 120,
            width: 560,
            filter: "blur(20px)",
            background:
              "radial-gradient(ellipse 100% 50% at 50% 0%, rgba(255,255,255,0.9), rgba(22,185,228,0.5) 42%, transparent 72%)",
          }}
        />
        {/* Rim light along the reel's top edge. */}
        <div
          className="absolute left-1/2 -translate-x-1/2"
          style={{
            ...screen,
            top: baseY - 9,
            height: 18,
            width: 900,
            filter: "blur(6px)",
            background:
              "linear-gradient(90deg, transparent 0%, rgba(126,206,236,0.55) 26%, rgba(255,255,255,0.98) 50%, rgba(126,206,236,0.55) 74%, transparent 100%)",
          }}
        />
      </div>

      {/* Sparks erupting from the beam-impact point on the reel's top edge,
          soft warm-white embers thrown off where the beam strikes. Their origin
          tracks the shared strike anchor, so they stay locked to the beam and
          the glare at every viewport width. */}
      <HeroSparks reduced={reduced} className="absolute inset-0" />

      {/* Edge + bottom fade to brand navy #0b0f1a so the full-bleed aurora
          dissolves into the page with no seam or hard cutoff. */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "radial-gradient(120% 90% at 50% 8%, transparent 46%, rgba(11,15,26,0.55) 78%, #0b0f1a 100%)",
        }}
      />
      <div className="absolute inset-x-0 bottom-0 h-72 bg-gradient-to-b from-transparent to-[#0b0f1a]" />
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Fixtures for the bento chart primitives
   ──────────────────────────────────────────────────────────────────────── */

const P95_SERIES = [
  38, 41, 39, 44, 42, 47, 45, 43, 40, 42, 46, 49, 44, 41, 43, 42, 45, 48, 44,
  40, 39, 42, 44, 41,
];
const P99_SERIES = [
  92, 96, 89, 104, 98, 118, 132, 121, 108, 99, 112, 141, 168, 128, 110, 104,
  118, 152, 137, 112, 101, 108, 116, 106,
];
const THROUGHPUT_BARS = [
  32, 41, 38, 47, 52, 58, 61, 66, 72, 78, 74, 81, 88, 92, 86, 94,
];
const ERROR_SERIES = [
  0.2, 0.3, 0.2, 0.4, 0.3, 0.6, 1.1, 0.9, 0.5, 0.3, 0.4, 0.8, 1.6, 0.7, 0.4,
  0.3, 0.5, 0.9, 0.6, 0.3,
];

const CLIENTS: readonly Client[] = [
  { name: "web-storefront", total: 184000, impact: 94 },
  { name: "mobile-ios", total: 121000, impact: 71 },
  { name: "partner-api", total: 68000, impact: 58 },
  { name: "admin-console", total: 24000, impact: 33 },
  { name: "analytics-etl", total: 12000, impact: 18 },
];

const INSIGHTS: readonly InsightRow[] = [
  {
    id: "op-checkout",
    spanKind: "server",
    name: "mutation checkout",
    averageLatency: 168,
    opm: 1240,
    errorRate: 0.031,
    impact: 98,
    latencySeries: [42, 48, 61, 88, 132, 168, 141],
    throughputSeries: [820, 910, 1040, 1180, 1210, 1240, 1190],
  },
  {
    id: "op-cart",
    spanKind: "server",
    name: "query cart",
    averageLatency: 44,
    opm: 4820,
    errorRate: 0.004,
    impact: 61,
    latencySeries: [38, 41, 44, 42, 46, 44, 41],
    throughputSeries: [4200, 4500, 4700, 4820, 4780, 4820, 4900],
  },
  {
    id: "op-search",
    spanKind: "internal",
    name: "query search",
    averageLatency: 72,
    opm: 2140,
    errorRate: 0.012,
    impact: 47,
    latencySeries: [58, 64, 71, 69, 74, 72, 70],
    throughputSeries: [1900, 2010, 2100, 2140, 2080, 2140, 2200],
  },
];

const TRACE: Trace = {
  totalMs: 168,
  spans: [
    {
      id: "s1",
      name: "POST /graphql",
      kind: "server",
      startMs: 0,
      durationMs: 168,
      depth: 0,
    },
    {
      id: "s2",
      name: "mutation checkout",
      kind: "graphql",
      startMs: 4,
      durationMs: 158,
      depth: 1,
    },
    {
      id: "s3",
      name: "PricingService.quote",
      kind: "http",
      startMs: 12,
      durationMs: 34,
      depth: 2,
    },
    {
      id: "s4",
      name: "InventoryDb.reserve",
      kind: "internal",
      startMs: 48,
      durationMs: 96,
      depth: 2,
    },
    {
      id: "s5",
      name: "PaymentGateway.charge",
      kind: "http",
      startMs: 146,
      durationMs: 18,
      depth: 2,
    },
  ],
};

/* ────────────────────────────────────────────────────────────────────────
   Bento of telemetry signals (Observe, built from chart primitives)
   ──────────────────────────────────────────────────────────────────────── */

function SignalsBento() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
      {/* Latency p95/p99 */}
      <Card className="sm:col-span-4" glow>
        <CardHeader index="a" title="Latency" hint="p95 / p99 · ms" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <ChartPanel
              title="Response time"
              subtitle="last 60 minutes"
              height={168}
              yDomain={[0, 180]}
              yTicks={[0, 60, 120, 180]}
              yFormat={(n) => `${n}`}
              legend={[
                { label: "p95", color: token.cP95 },
                { label: "p99", color: token.cP99 },
              ]}
            >
              <LineAreaChart
                series={[
                  {
                    values: P95_SERIES,
                    stroke: token.cP95,
                    fill: true,
                    fillOpacity: 0.12,
                  },
                  {
                    values: P99_SERIES,
                    stroke: token.cP99,
                    fill: true,
                    fillOpacity: 0.1,
                  },
                ]}
                domain={[0, 180]}
                grid
                showHead
              />
            </ChartPanel>
          </NitroCanvas>
        </div>
      </Card>

      {/* Throughput big stat */}
      <Card className="sm:col-span-2">
        <CardHeader index="b" title="Throughput" hint="ops / min" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <NitroCanvas className="h-11">
            <CountUp
              value={94200}
              format={(n) => Math.round(n).toLocaleString("en-US")}
              style={{ justifyContent: "flex-start", fontSize: 34 }}
            />
          </NitroCanvas>
          <NitroCanvas className="mt-3 h-16">
            <BarSeries values={THROUGHPUT_BARS} color={token.cThroughput} />
          </NitroCanvas>
        </div>
      </Card>

      {/* Per-client usage & impact */}
      <Card className="sm:col-span-3">
        <CardHeader index="c" title="Top clients" hint="by impact" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <HBarSeries clients={CLIENTS as Client[]} maxBars={5} />
          </NitroCanvas>
        </div>
      </Card>

      {/* Error rate */}
      <Card className="sm:col-span-3">
        <CardHeader index="d" title="Error rate" hint="% of requests" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <div className="flex items-baseline gap-2">
            <span
              className="font-heading text-h4 tabular-nums"
              style={{ color: token.cError }}
            >
              0.31%
            </span>
            <span className="text-cc-ink-dim text-caption">
              within budget · 1.6% peak
            </span>
          </div>
          <NitroCanvas className="mt-3 h-16">
            <Sparkline values={ERROR_SERIES} stroke={token.cError} fill />
          </NitroCanvas>
        </div>
      </Card>

      {/* Impact-ranked operations */}
      <Card className="sm:col-span-4">
        <CardHeader index="e" title="Impact score" hint="what hurts most" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <InsightsTable
              rows={INSIGHTS as InsightRow[]}
              errorThreshold={0.02}
            />
          </NitroCanvas>
        </div>
      </Card>

      {/* Trace preview */}
      <Card className="sm:col-span-2">
        <CardHeader index="f" title="Slow span" hint="checkout" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <TraceWaterfall trace={TRACE} rowHeight={22} />
          </NitroCanvas>
        </div>
      </Card>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Schema change classification (Evolve aside graphic)
   ──────────────────────────────────────────────────────────────────────── */

type ChangeKind = "safe" | "dangerous" | "breaking";

const KIND_STYLE: Record<
  ChangeKind,
  { readonly label: string; readonly className: string }
> = {
  safe: {
    label: "SAFE",
    className: "text-cc-success border-cc-success/40 bg-cc-success/[0.08]",
  },
  dangerous: {
    label: "DANGEROUS",
    className: "text-cc-warning border-cc-warning/40 bg-cc-warning/[0.08]",
  },
  breaking: {
    label: "BREAKING",
    className: "text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08]",
  },
};

interface KindPillProps {
  readonly kind: ChangeKind;
}

function KindPill({ kind }: KindPillProps) {
  const s = KIND_STYLE[kind];
  return (
    <span
      className={[
        "rounded border px-1.5 py-0.5 font-mono text-[10px] tracking-[0.12em]",
        s.className,
      ].join(" ")}
    >
      {s.label}
    </span>
  );
}

const SCHEMA_CHANGES: readonly {
  readonly field: string;
  readonly kind: ChangeKind;
}[] = [
  { field: "+ Order.deliveryEstimate: DateTime", kind: "safe" },
  { field: "~ Product.price: Float → Money", kind: "dangerous" },
  { field: "- Order.total: Float", kind: "breaking" },
];

function ClassificationCard() {
  return (
    <Card className="mt-1">
      <div className="flex items-center justify-between px-4 py-3">
        <span className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
          orders-api · v14
        </span>
        <span className="text-cc-danger text-caption font-mono">
          publish blocked
        </span>
      </div>
      <div className="border-cc-card-border divide-cc-card-border/60 divide-y border-t">
        {SCHEMA_CHANGES.map((c) => (
          <div
            key={c.field}
            className="flex items-center justify-between gap-3 px-4 py-2.5"
          >
            <code className="text-cc-ink truncate font-mono text-xs">
              {c.field}
            </code>
            <KindPill kind={c.kind} />
          </div>
        ))}
      </div>
      <div className="text-cc-ink-dim border-cc-card-border border-t px-4 py-2.5 font-mono text-[11px]">
        1 safe · 1 dangerous · 1 breaking
      </div>
    </Card>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Delivery / safety band (persisted ops · CI checks · safe rollout)
   ──────────────────────────────────────────────────────────────────────── */

interface CheckRow {
  readonly label: string;
  readonly detail: string;
  readonly state: "pass" | "fail";
}

const CI_CHECKS: readonly CheckRow[] = [
  { label: "schema validate", detail: "127 fields", state: "pass" },
  { label: "client checks", detail: "3 published clients", state: "pass" },
  { label: "breaking change", detail: "Order.total removed", state: "fail" },
  { label: "trusted operations", detail: "482 hashes signed", state: "pass" },
];

function CheckIconMark({ state }: { readonly state: "pass" | "fail" }) {
  if (state === "pass") {
    return (
      <svg
        viewBox="0 0 16 16"
        aria-hidden="true"
        className="text-cc-success h-4 w-4 fill-current"
      >
        <path d="M6.5 11.2 3.3 8l1.1-1.1 2.1 2.1 5-5L12.6 5z" />
      </svg>
    );
  }
  return (
    <svg
      viewBox="0 0 16 16"
      aria-hidden="true"
      className="text-cc-danger h-4 w-4 fill-current"
    >
      <path d="M11.5 5.6 9.1 8l2.4 2.4-1.1 1.1L8 9.1l-2.4 2.4-1.1-1.1L6.9 8 4.5 5.6l1.1-1.1L8 6.9l2.4-2.4z" />
    </svg>
  );
}

function DeliveryBand() {
  return (
    <section
      id="delivery"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <RevealOnScroll>
        <SectionIntro
          index="07"
          eyebrow="Delivery"
          title="Ship on green, roll out with a safety net."
          lead="Persisted, trusted operations lock the queries clients can send. Schema and client checks run in CI, so a breaking change fails the build instead of the customer."
        />
      </RevealOnScroll>

      <div className="mt-12 grid gap-4 lg:grid-cols-3">
        <RevealOnScroll className="lg:col-span-2">
          <Card>
            <div className="flex items-center justify-between px-5 py-3.5">
              <span className="text-cc-heading font-heading text-h6">
                CI schema check
              </span>
              <span className="text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08] rounded border px-2 py-0.5 font-mono text-[10px] tracking-[0.12em]">
                FAILED
              </span>
            </div>
            <div className="border-cc-card-border divide-cc-card-border/60 divide-y border-t">
              {CI_CHECKS.map((c) => (
                <div
                  key={c.label}
                  className="flex items-center gap-3 px-5 py-3"
                >
                  <CheckIconMark state={c.state} />
                  <span className="text-cc-ink font-mono text-sm">
                    {c.label}
                  </span>
                  <span className="text-cc-ink-dim ml-auto font-mono text-xs">
                    {c.detail}
                  </span>
                </div>
              ))}
            </div>
            <div className="text-cc-ink-dim border-cc-card-border border-t px-5 py-3 font-mono text-[11px]">
              merging is blocked until every check passes
            </div>
          </Card>
        </RevealOnScroll>

        <RevealOnScroll>
          <Card className="h-full">
            <div className="flex flex-col gap-5 p-6">
              <div>
                <div className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
                  Persisted operations
                </div>
                <p className="text-cc-ink mt-2 text-sm leading-relaxed">
                  Only registered query hashes execute. Ad-hoc queries and
                  injection never reach a resolver.
                </p>
              </div>
              <div className="border-cc-card-border rounded-lg border bg-black/20 p-3 font-mono text-xs">
                <div className="text-cc-ink-dim">POST /graphql</div>
                <div className="mt-1 truncate" style={{ color: "#16b9e4" }}>
                  documentId: sha256:7f3a9b2e…
                </div>
                <div className="text-cc-success mt-1">
                  200 · trusted · 12 ms
                </div>
              </div>
              <div className="mt-auto flex items-center gap-2">
                <span className="text-cc-success text-caption font-mono">
                  ● safe rollout
                </span>
                <span className="text-cc-ink-dim text-caption">
                  stage → canary → prod
                </span>
              </div>
            </div>
          </Card>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Ecosystem / platform strip
   ──────────────────────────────────────────────────────────────────────── */

interface PlatformIconProps {
  readonly className?: string;
}

function ServerGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <rect x="3" y="4" width="18" height="6" rx="1.5" />
      <rect x="3" y="14" width="18" height="6" rx="1.5" />
      <circle cx="7" cy="7" r="1" className="fill-cc-bg" />
      <circle cx="7" cy="17" r="1" className="fill-cc-bg" />
    </svg>
  );
}

function ClientGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <rect x="3" y="4" width="18" height="13" rx="1.5" />
      <rect x="9" y="19" width="6" height="1.6" rx="0.8" />
      <rect
        x="6"
        y="7"
        width="8"
        height="1.4"
        rx="0.7"
        className="fill-cc-bg"
      />
    </svg>
  );
}

function GatewayGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <circle cx="12" cy="12" r="3" />
      <circle cx="4" cy="5" r="2" />
      <circle cx="4" cy="19" r="2" />
      <circle cx="20" cy="12" r="2" />
      <path
        d="M6 6l4 5M6 18l4-5M15 12h3"
        stroke="currentColor"
        strokeWidth="1.4"
        fill="none"
      />
    </svg>
  );
}

function ControlGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <circle
        cx="12"
        cy="12"
        r="9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
      />
      <circle cx="12" cy="12" r="2.5" />
      <path
        d="M12 3v3M12 18v3M3 12h3M18 12h3"
        stroke="currentColor"
        strokeWidth="1.6"
      />
    </svg>
  );
}

const PLATFORM: readonly {
  readonly name: string;
  readonly role: string;
  readonly Icon: (p: PlatformIconProps) => ReactNode;
}[] = [
  { name: "Hot Chocolate", role: "GraphQL server", Icon: ServerGlyph },
  { name: "Strawberry Shake", role: "GraphQL client", Icon: ClientGlyph },
  { name: "Fusion", role: "Federation gateway", Icon: GatewayGlyph },
  { name: "Nitro", role: "Control plane", Icon: ControlGlyph },
];

function EcosystemStrip() {
  return (
    <section
      id="ecosystem"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <RevealOnScroll>
        <SectionIntro
          index="09"
          eyebrow="Platform"
          title="One open-source stack, end to end."
          lead="Nitro sits on top of the same GraphQL platform you already build with, from the server that answers to the gateway that composes."
        />
      </RevealOnScroll>

      <div className="mt-12 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {PLATFORM.map(({ name, role, Icon }, i) => (
          <RevealOnScroll key={name} hiddenClassName="translate-y-6 opacity-0">
            <Card className="h-full">
              <div className="flex flex-col gap-4 p-5">
                <div
                  className="border-cc-card-border flex h-10 w-10 items-center justify-center rounded-lg border bg-black/20"
                  style={{ color: "#16b9e4" }}
                >
                  <Icon className="h-5 w-5 fill-current" />
                </div>
                <div>
                  <div className="text-cc-heading font-heading text-h6">
                    {name}
                  </div>
                  <div className="text-cc-ink-dim text-caption font-mono tracking-[0.14em] uppercase">
                    {role}
                  </div>
                </div>
                <span
                  aria-hidden="true"
                  className="h-px w-full rounded-full opacity-60"
                  style={{
                    background:
                      i === PLATFORM.length - 1
                        ? SPECTRUM
                        : "linear-gradient(90deg, rgba(22,185,228,0.5), transparent)",
                  }}
                />
              </div>
            </Card>
          </RevealOnScroll>
        ))}
      </div>

      <RevealOnScroll className="mt-6">
        <div className="text-cc-ink-dim text-caption text-center font-mono tracking-[0.14em] uppercase">
          MIT licensed · built in the open · one graph for .NET
        </div>
      </RevealOnScroll>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Hero (full-bleed cyan aurora, left-aligned copy, crisp reel)
   ──────────────────────────────────────────────────────────────────────── */

interface HeroProps {
  readonly reduced: boolean;
}

function Hero({ reduced }: HeroProps) {
  return (
    <section className="relative left-1/2 isolate w-screen -translate-x-1/2 overflow-hidden">
      {/* Full-viewport-width aurora that fades to brand navy at every edge. */}
      <HeroAurora reduced={reduced} />

      <div className="relative z-10 mx-auto max-w-7xl px-5 pt-6 pb-16 sm:px-12 sm:pt-12">
        <RevealOnScroll className="flex max-w-2xl flex-col gap-6">
          <div className="flex items-center gap-3">
            <span
              aria-hidden="true"
              className="h-px w-16 rounded-full"
              style={{ background: "#16b9e4" }}
            />
            <Eyebrow>The Control Plane for GraphQL</Eyebrow>
          </div>
          <h1
            className="font-heading text-h1 text-balance"
            style={{
              backgroundImage:
                "linear-gradient(105deg, #f5f0ea 24%, #8fd3ec 58%, #16b9e4 82%, #7c92c6 100%)",
              backgroundClip: "text",
              WebkitBackgroundClip: "text",
              color: "transparent",
              WebkitTextFillColor: "transparent",
            }}
          >
            Your whole API, on one control plane.
          </h1>
          <p className="lead text-cc-ink max-w-xl !text-xl !leading-relaxed">
            Author operations, watch them run, trace every request, and evolve
            your schema without breaking the clients you ship to.
          </p>
          <div className="mt-2 flex flex-wrap items-center gap-4">
            <span
              className="relative inline-flex rounded-full"
              style={{
                boxShadow:
                  "0 0 24px rgba(22,185,228,0.5), 0 0 52px rgba(240,120,106,0.28)",
              }}
            >
              <SolidButton href="/get-started">See it in action</SolidButton>
            </span>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>

        {/* 5-tab reel: it is its own app window, so no outer frame. It rises
            out of the aurora and stays crisp on top of the glow. */}
        <RevealOnScroll
          className="relative z-10 mt-16 sm:mt-20"
          hiddenClassName="translate-y-10 scale-[0.98] opacity-0"
          shownClassName="translate-y-0 scale-100 opacity-100"
        >
          <div className="v22-hero-reel relative mx-auto w-full max-w-6xl">
            <NitroReel tabsOverlay />
            {/* Beam glare: the aurora beam strikes the reel's top edge here, so
                light glares off the top border and spills onto the window. In
                front of the reel (z-30), blended as screen so it reads as an
                actual hit, not light passing behind the window. */}
            <div
              aria-hidden="true"
              className="pointer-events-none absolute inset-x-0 top-0 z-30"
            >
              {/* Soft halo bloom around the strike. */}
              <div
                className="absolute"
                style={{
                  left: "calc(50% + 288px)",
                  top: 3,
                  transform: "translate(-50%, -50%)",
                  width: 300,
                  height: 120,
                  filter: "blur(16px)",
                  mixBlendMode: "screen",
                  background:
                    "radial-gradient(ellipse 50% 50% at 50% 50%, rgba(140,214,240,0.55), rgba(22,185,228,0.26) 52%, transparent 80%)",
                }}
              />
              {/* Wide soft anamorphic glow lying along the top border. */}
              <div
                className="absolute"
                style={{
                  left: "calc(50% + 288px)",
                  top: 3,
                  transform: "translate(-50%, -50%)",
                  width: 460,
                  height: 18,
                  filter: "blur(7px)",
                  mixBlendMode: "screen",
                  background:
                    "linear-gradient(90deg, transparent, rgba(170,222,244,0.75) 40%, rgba(255,255,255,0.95) 50%, rgba(170,222,244,0.75) 60%, transparent)",
                }}
              />
              {/* Crisp specular streak, the glare line ON the border. */}
              <div
                className="absolute"
                style={{
                  left: "calc(50% + 288px)",
                  top: 3,
                  transform: "translate(-50%, -50%)",
                  width: 640,
                  height: 3,
                  filter: "blur(1px)",
                  mixBlendMode: "screen",
                  background:
                    "linear-gradient(90deg, transparent, rgba(140,214,240,0.5) 24%, rgba(255,255,255,1) 50%, rgba(140,214,240,0.5) 76%, transparent)",
                }}
              />
              {/* Bright compact flare point at the exact hit. */}
              <div
                className="absolute"
                style={{
                  left: "calc(50% + 288px)",
                  top: 3,
                  transform: "translate(-50%, -50%)",
                  width: 64,
                  height: 64,
                  filter: "blur(5px)",
                  mixBlendMode: "screen",
                  background:
                    "radial-gradient(circle, rgba(255,255,255,1), rgba(205,240,255,0.7) 30%, transparent 68%)",
                }}
              />
              {/* Light spilling down onto the window surface below the hit. */}
              <div
                className="absolute"
                style={{
                  left: "calc(50% + 288px)",
                  top: 5,
                  transform: "translateX(-50%)",
                  width: 300,
                  height: 130,
                  filter: "blur(16px)",
                  mixBlendMode: "screen",
                  background:
                    "linear-gradient(to bottom, rgba(205,240,255,0.42), rgba(22,185,228,0.16) 45%, transparent 82%)",
                }}
              />
            </div>
          </div>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Full-page atmosphere: two drifting particle layers on different spectrum
   hues + soft multi-color glows that scroll with the document, so the brand
   spectrum recurs from hero to footer instead of a single flat teal.
   ──────────────────────────────────────────────────────────────────────── */

function PageAtmosphere() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 left-1/2 -z-10 w-screen -translate-x-1/2 overflow-hidden"
    >
      {/* Two low-count mote layers, cyan and coral, so the drifting field
          itself reads multi-color. */}
      <RisingParticles
        color="22,185,228"
        count={16}
        className="absolute inset-0"
      />
      <RisingParticles
        color="240,120,106"
        count={12}
        className="absolute inset-0"
      />
      {/* Large, very soft spectrum glows placed down the page so the brand
          light recurs and ties the whole surface together: coral upper-left,
          cyan mid, mauve lower. */}
      <div
        className="absolute top-[20%] left-0 h-[48rem] w-[48rem] -translate-x-1/4 rounded-full opacity-90 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, rgba(240,120,106,0.18), transparent 68%)",
        }}
      />
      <div
        className="absolute top-[44%] right-0 h-[44rem] w-[44rem] translate-x-1/4 rounded-full opacity-90 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, rgba(22,185,228,0.18), transparent 68%)",
        }}
      />
      <div
        className="absolute bottom-[8%] left-1/2 h-[42rem] w-[42rem] -translate-x-1/2 rounded-full opacity-85 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, rgba(182,129,169,0.16), transparent 68%)",
        }}
      />
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Page
   ──────────────────────────────────────────────────────────────────────── */

export function ClientPage() {
  const reduced = useReducedMotion() ?? false;

  return (
    <div className="relative isolate">
      {/* FULL-PAGE ATMOSPHERE ─────────────────────────────────── */}
      <PageAtmosphere />

      {/* HERO ─────────────────────────────────────────────────── */}
      <Hero reduced={reduced} />

      {/* AUTHOR ───────────────────────────────────────────────── */}
      <Showcase
        id="author"
        index="01"
        eyebrow="Author"
        title="Compose GraphQL against real, federated data."
        body="A schema-aware editor with live validation and one-click operations, running against your composed graph, not a mock."
        visual={<NitroCompose className="w-full" />}
      />

      {/* OBSERVE ──────────────────────────────────────────────── */}
      <section
        id="observe"
        className="border-cc-card-border scroll-mt-24 border-t py-20 text-center sm:py-28"
      >
        <RevealOnScroll>
          <SectionIntro
            index="02"
            eyebrow="Observe"
            title="See exactly how your API behaves in production."
            lead="OpenTelemetry-native monitoring: latency, throughput, and error rate per operation and per client, ranked by the impact score that tells you what to fix first."
          />
        </RevealOnScroll>
        <RevealOnScroll
          className="mt-14 sm:mt-16"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <ControlPlaneConsole className="mx-auto max-w-5xl" />
        </RevealOnScroll>
      </section>

      {/* SIGNALS BENTO ────────────────────────────────────────── */}
      <section
        id="signals"
        className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
      >
        <RevealOnScroll>
          <SectionIntro
            index="03"
            eyebrow="Every signal"
            title="One console, every signal in view."
            lead="p95 and p99 latency, throughput, error budget, per-client usage, impact ranking, and the slow span, all reading from the same telemetry."
          />
        </RevealOnScroll>
        <RevealOnScroll
          className="mt-12"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <SignalsBento />
        </RevealOnScroll>
      </section>

      {/* TRACE ────────────────────────────────────────────────── */}
      <Showcase
        id="trace"
        index="04"
        eyebrow="Trace"
        title="Follow one request across your whole backend."
        body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
        visual={<NitroTrace className="w-full" />}
        reverse
      />

      {/* DIAGNOSE ─────────────────────────────────────────────── */}
      <Showcase
        id="diagnose"
        index="05"
        eyebrow="Diagnose"
        title="From an error spike to the line that threw it."
        body="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it. No log spelunking."
        visual={<NitroDiagnose className="w-full" />}
      />

      {/* EVOLVE / SCHEMA ──────────────────────────────────────── */}
      <Showcase
        id="schema"
        index="06"
        eyebrow="Evolve"
        title="Change your schema without breaking clients."
        body="The registry classifies every change as safe, dangerous, or breaking and checks it against published clients before it ships."
        visual={<NitroSchema className="w-full" />}
        aside={<ClassificationCard />}
        reverse
      />

      {/* DELIVERY / SAFETY ────────────────────────────────────── */}
      <DeliveryBand />

      {/* FUSION ───────────────────────────────────────────────── */}
      <Showcase
        id="fusion"
        index="08"
        eyebrow="Compose"
        title="One graph, executed across every subgraph."
        body="Fusion shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
        visual={<NitroFusion className="w-full" />}
      />

      {/* ECOSYSTEM ────────────────────────────────────────────── */}
      <EcosystemStrip />

      {/* CTA ──────────────────────────────────────────────────── */}
      <section className="border-cc-card-border border-t py-24 text-center sm:py-32">
        <RevealOnScroll className="mx-auto flex max-w-2xl flex-col items-center gap-6">
          <Eyebrow>Ready when you are</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 text-balance">
            Put your API on the control plane.
          </h2>
          <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
            Start in the GraphQL IDE in seconds, then grow into observability,
            tracing, and a registry that keeps your schema and clients in sync.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>
      </section>
    </div>
  );
}
