"use client";

import {
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
  type MotionValue,
} from "motion/react";
import type { CSSProperties, ReactNode } from "react";
import { useEffect, useId, useRef } from "react";

import { ControlPlaneConsole } from "@/src/components/nitro/ControlPlaneConsole";
import { RisingParticles } from "@/src/components/nitro/RisingParticles";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  BarSeries,
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
  TraceWaterfall,
} from "@/src/nitro";
import { areaFromLine, smoothLinePath, type Pt } from "@/src/nitro/lib/scale";
import type { Client, InsightRow, Trace } from "@/src/nitro/lib/data/types";

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

function Card({ className, children, glow = false }: CardProps) {
  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border",
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
  readonly title: string;
  readonly hint?: ReactNode;
}

function CardHeader({ title, hint }: CardHeaderProps) {
  return (
    <div className="flex items-baseline justify-between gap-3 px-5 pt-5">
      <h3 className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
        {title}
      </h3>
      {hint && (
        <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
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

function NitroCanvas({ children, className, style }: NitroCanvasProps) {
  return (
    <NitroTheme
      theme="dark"
      reducedMotion="never"
      className={className}
      style={
        {
          background: "transparent",
          "--t-font": "var(--font-body)",
          "--t-font-heading": "var(--font-heading)",
          ...style,
        } as CSSProperties
      }
    >
      {children}
    </NitroTheme>
  );
}

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
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border shadow-2xl shadow-black/50">
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

const CONES: readonly {
  readonly halfAngle: number;
  readonly color: string;
  readonly peak: number;
  readonly mid: number;
  readonly blur: number;
}[] = [
  { halfAngle: 37, color: "240,120,106", peak: 0.4, mid: 0.18, blur: 50 },
  { halfAngle: 30, color: "182,129,169", peak: 0.52, mid: 0.24, blur: 44 },
  { halfAngle: 22, color: "124,146,198", peak: 0.58, mid: 0.28, blur: 36 },
  { halfAngle: 15, color: "22,185,228", peak: 0.66, mid: 0.32, blur: 28 },
  { halfAngle: 9, color: "196,236,247", peak: 0.9, mid: 0.46, blur: 20 },
];

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

const STRIKE_X_OFFSET = 288;
const STRIKE_Y = 577;

const MOTE_TINTS = [
  { fill: "255,246,232", glow: "255,224,168" },
  { fill: "255,236,196", glow: "244,180,94" },
  { fill: "255,246,232", glow: "255,224,168" },
  { fill: "255,224,196", glow: "242,140,90" },
  { fill: "222,240,246", glow: "143,201,221" },
] as const;

interface HeroSparksProps {
  readonly reduced: boolean;
  readonly className?: string;
}

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

    const RISE_PX = 340;
    const FAN_PX = 150;

    const motes = Array.from({ length: 54 }, (_, i) => ({
      life: rand(i * 9 + 1),
      fan: (rand(i * 9 + 2) - 0.5) * 2,
      wobPhase: rand(i * 9 + 3) * Math.PI * 2,
      wobSpeed: 0.4 + rand(i * 9 + 4) * 0.9,
      r: 0.5 + rand(i * 9 + 5) * 2.1,
      speed: 0.04 + rand(i * 9 + 6) * 0.07,
      base: 0.32 + rand(i * 9 + 7) * 0.52,
      phase: rand(i * 9 + 8) * Math.PI * 2,
      twSpeed: 0.6 + rand(i * 9 + 9) * 1.4,
      tint: MOTE_TINTS[Math.floor(rand(i * 9 + 10) * MOTE_TINTS.length)],
    }));

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
      const ox = cw / 2 + STRIKE_X_OFFSET;
      const oy = STRIKE_Y;
      ctx.clearRect(0, 0, cw, ch);
      ctx.shadowBlur = 7;
      for (const m of motes) {
        if (!reduced) {
          m.life += m.speed * dt;
          if (m.life > 1) {
            m.life -= 1;
            m.fan = (rand(m.phase * 1000 + t) - 0.5) * 2;
          }
        }
        const wobble = Math.sin(m.wobPhase + t * m.wobSpeed) * 6;
        const spread = 0.05 + 0.95 * (m.life * m.life);
        const px = ox + m.fan * FAN_PX * spread + wobble;
        const py = oy - m.life * RISE_PX;
        const edge =
          smoothstep(0, 0.16, m.life) * (1 - smoothstep(0.78, 1, m.life));
        const twinkle = 0.55 + 0.45 * Math.sin(m.phase + t * m.twSpeed);
        const a = m.base * twinkle * edge;
        if (a <= 0.01) {
          continue;
        }
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
  const baseY = 602;
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
        .v22-hero-reel [role="group"] > div:first-child { display: none !important; }
        @media (max-width: 639px) {
          .v22-hero-reel [role="group"] > div:last-child > div:last-child > div {
            display: none !important;
          }
          .v22-hero-flare {
            opacity: 0.4 !important;
          }
        }
      `}</style>

      <div
        className="absolute inset-0"
        style={{
          ...screen,
          background:
            "radial-gradient(130% 80% at 50% -12%, rgba(22,185,228,0.24), transparent 58%), radial-gradient(58% 50% at 86% 6%, rgba(240,120,106,0.24), transparent 64%), radial-gradient(52% 44% at 8% 6%, rgba(124,146,198,0.22), transparent 62%)",
        }}
      />
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

      <div
        className="v22-hero-flare absolute inset-0"
        style={{
          transform: "translateX(288px)",
          ...(reduced
            ? {}
            : { animation: "v22-breathe 9s ease-in-out infinite" }),
        }}
      >
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

      <HeroSparks reduced={reduced} className="absolute inset-0" />

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

const BENTO_THROUGHPUT = "#16b9e4";
const BENTO_P95 = "#d6488f";
const BENTO_P99 = "#9b7dd0";

function LatencyLegend() {
  return (
    <span className="flex items-center gap-3">
      <span className="flex items-center gap-1.5">
        <span
          aria-hidden="true"
          className="inline-block h-1.5 w-1.5 rounded-full"
          style={{ background: BENTO_P95 }}
        />
        p95
      </span>
      <span className="flex items-center gap-1.5">
        <span
          aria-hidden="true"
          className="inline-block h-1.5 w-1.5 rounded-full"
          style={{ background: BENTO_P99 }}
        />
        p99
      </span>
    </span>
  );
}

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

function useBentoProgress() {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotion() ?? false;
  const inView = useInView(ref, { amount: 0.25, once: true });
  const progress = useMotionValue(reduced ? 1 : 0);

  useEffect(() => {
    if (reduced) {
      progress.set(1);
      return;
    }
    if (!inView) {
      return;
    }
    const controls = animate(progress, 1, { duration: 5.5, ease: "linear" });
    return () => controls.stop();
  }, [reduced, inView, progress]);

  return { ref, progress };
}

const ERROR_CURVE = [
  0.18, 0.22, 0.2, 0.28, 0.26, 0.34, 0.44, 0.52, 0.66, 0.9, 1.2, 1.6, 1.24,
  0.82, 0.58, 0.48, 0.5, 0.44, 0.38, 0.31,
];

interface ErrorRateSparkProps {
  readonly progress: MotionValue<number>;
}

function ErrorRateSpark({ progress }: ErrorRateSparkProps) {
  const W = 240;
  const H = 64;
  const PAD = 6;
  const stroke = "#f0786a";
  const gid = useId().replace(/:/g, "");
  const reduced = useReducedMotion() ?? false;

  const n = ERROR_CURVE.length;
  const dMin = Math.min(...ERROR_CURVE);
  const dMax = Math.max(...ERROR_CURVE);
  const xOf = (i: number) => (i / (n - 1)) * W;
  const yOf = (v: number) =>
    H - PAD - ((v - dMin) / (dMax - dMin)) * (H - PAD * 2);
  const pts: Pt[] = ERROR_CURVE.map((v, i) => [xOf(i), yOf(v)]);
  const lineD = smoothLinePath(pts);
  const areaD = areaFromLine(lineD, pts, H - PAD);
  const peakIndex = ERROR_CURVE.indexOf(dMax);
  const peakLeft = `${(xOf(peakIndex) / W) * 100}%`;
  const peakTop = `${(yOf(dMax) / H) * 100}%`;

  const draw = useTransform(progress, [0, 1], [0, 1], { clamp: true });
  const wipeW = useTransform(draw, [0, 1], [0, W]);
  const peakFrac = n > 1 ? peakIndex / (n - 1) : 0.5;
  const dotOpacity = useTransform(
    draw,
    [peakFrac, Math.min(1, peakFrac + 0.05)],
    [0, 1],
    { clamp: true },
  );
  const dotScale = useTransform(
    draw,
    [peakFrac, Math.min(1, peakFrac + 0.1)],
    [0.3, 1],
    { clamp: true },
  );
  const glowOpacity = useTransform(
    draw,
    [peakFrac, Math.min(1, peakFrac + 0.14)],
    [0, 1],
    { clamp: true },
  );

  return (
    <div style={{ position: "relative", width: "100%", height: "100%" }}>
      <svg
        viewBox={`0 0 ${W} ${H}`}
        preserveAspectRatio="none"
        width="100%"
        height="100%"
        style={{ display: "block", overflow: "visible" }}
        aria-hidden="true"
      >
        <defs>
          <linearGradient id={`err-${gid}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={stroke} stopOpacity="0.12" />
            <stop offset="100%" stopColor={stroke} stopOpacity="0" />
          </linearGradient>
          <clipPath id={`wipe-${gid}`}>
            <motion.rect x={0} y={0} height={H} width={wipeW} />
          </clipPath>
        </defs>
        <g clipPath={`url(#wipe-${gid})`}>
          <path d={areaD} fill={`url(#err-${gid})`} />
          <path
            d={lineD}
            fill="none"
            stroke={stroke}
            strokeWidth={1.5}
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />
        </g>
      </svg>
      <motion.span
        aria-hidden="true"
        style={{
          position: "absolute",
          left: peakLeft,
          top: peakTop,
          width: 22,
          height: 22,
          marginLeft: -11,
          marginTop: -11,
          opacity: glowOpacity,
        }}
      >
        <motion.span
          aria-hidden="true"
          style={{
            display: "block",
            width: "100%",
            height: "100%",
            borderRadius: "9999px",
            border: `1px solid ${stroke}`,
          }}
          animate={
            reduced
              ? undefined
              : { scale: [0.7, 1.6, 0.7], opacity: [0.6, 0, 0.6] }
          }
          transition={
            reduced
              ? undefined
              : { repeat: Infinity, duration: 1.8, ease: "easeInOut" }
          }
        />
      </motion.span>
      <motion.span
        aria-hidden="true"
        style={{
          position: "absolute",
          left: peakLeft,
          top: peakTop,
          width: 7,
          height: 7,
          marginLeft: -3.5,
          marginTop: -3.5,
          borderRadius: "9999px",
          background: stroke,
          boxShadow: `0 0 8px ${stroke}`,
          opacity: dotOpacity,
          scale: dotScale,
        }}
      />
    </div>
  );
}

function SignalsBento() {
  const { ref, progress } = useBentoProgress();

  return (
    <div ref={ref} className="grid grid-cols-1 gap-4 sm:grid-cols-6">
      <Card className="sm:col-span-4" glow>
        <CardHeader title="Latency" hint={<LatencyLegend />} />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <LineAreaChart
              series={[
                {
                  values: P95_SERIES,
                  stroke: BENTO_P95,
                  fill: true,
                  fillGradient: true,
                  fillOpacity: 0.28,
                  strokeWidth: 1.2,
                },
                {
                  values: P99_SERIES,
                  stroke: BENTO_P99,
                  fill: true,
                  fillGradient: true,
                  fillOpacity: 0.2,
                  strokeWidth: 1.2,
                },
              ]}
              domain={[0, 180]}
              height={168}
              grid
              showHead
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
        </div>
      </Card>

      <Card className="sm:col-span-2">
        <CardHeader title="Throughput" hint="ops / min" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <NitroCanvas className="h-11">
            <CountUp
              value={94200}
              format={(n) => Math.round(n).toLocaleString("en-US")}
              style={{ justifyContent: "flex-start", fontSize: 34 }}
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
          <NitroCanvas className="mt-3 h-16">
            <BarSeries
              values={THROUGHPUT_BARS}
              color={BENTO_THROUGHPUT}
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
        </div>
      </Card>

      <Card className="sm:col-span-3">
        <CardHeader title="Top clients" hint="by impact" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <HBarSeries
              clients={CLIENTS as Client[]}
              maxBars={5}
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
        </div>
      </Card>

      <Card className="sm:col-span-3">
        <CardHeader title="Error rate" hint="% of requests" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <div className="flex items-baseline gap-2">
            <span
              className="font-heading text-h4 tabular-nums"
              style={{ color: "#f0786a" }}
            >
              0.31%
            </span>
            <span className="text-cc-ink-dim text-caption">
              within budget · 1.6% peak
            </span>
          </div>
          <div className="mt-3 h-16">
            <ErrorRateSpark progress={progress} />
          </div>
        </div>
      </Card>

      <Card className="sm:col-span-4">
        <CardHeader title="Impact score" hint="what hurts most" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <InsightsTable
              rows={INSIGHTS as InsightRow[]}
              errorThreshold={0.02}
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
        </div>
      </Card>

      <Card className="sm:col-span-2">
        <CardHeader title="Slow span" hint="checkout" />
        <div className="nitro-trace px-5 pt-4 pb-5">
          <style>{`
            .nitro-trace [role="img"] > div:first-child { display: none; }
            .nitro-trace [style*="dashed"] { display: none; }
            .nitro-trace [role="img"] > div:nth-child(2) > div:last-child > div:last-child {
              left: auto !important;
              right: 0;
              transform: none;
            }
          `}</style>
          <NitroCanvas>
            <TraceWaterfall
              trace={TRACE}
              rowHeight={34}
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
        </div>
      </Card>
    </div>
  );
}

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
        <span className="text-cc-ink-dim text-caption font-mono tracking-[0.16em] uppercase">
          orders-api · v14
        </span>
        <span className="text-cc-danger text-caption font-mono">
          publish blocked
        </span>
      </div>
      <div className="divide-cc-card-border border-cc-card-border divide-y border-t">
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
          title="Ship with confidence."
          lead="Nitro turns release readiness into visible checks: schema validation, client compatibility, trusted operations, and rollout status."
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
            <div className="divide-cc-card-border border-cc-card-border divide-y border-t">
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
                <div className="text-cc-ink-dim text-caption font-mono tracking-[0.16em] uppercase">
                  Persisted operations
                </div>
                <p className="text-cc-ink mt-2 text-sm leading-relaxed">
                  Only registered query hashes execute. Ad-hoc queries and
                  injection never reach a resolver.
                </p>
              </div>
              <div className="border-cc-card-border bg-cc-card-bg rounded-lg border p-3 font-mono text-xs">
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

interface HeroProps {
  readonly reduced: boolean;
}

function Hero({ reduced }: HeroProps) {
  return (
    <section className="relative left-1/2 isolate -mt-8 w-screen -translate-x-1/2 overflow-hidden">
      <HeroAurora reduced={reduced} />

      <div className="relative z-10 mx-auto max-w-7xl px-5 pt-24 pb-16 sm:px-12 sm:pt-30">
        <RevealOnScroll className="flex max-w-2xl flex-col gap-6">
          <div className="flex items-center gap-3">
            <span
              aria-hidden="true"
              className="h-px w-16 rounded-full"
              style={{ background: "#16b9e4" }}
            />
            <Eyebrow>API Operations Platform</Eyebrow>
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
            Nitro brings observability, tracing, schema governance, client
            safety, and rollout checks together, so teams can understand what is
            running and ship changes with confidence.
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

        <div className="relative z-10 mt-16 sm:mt-20">
          <div className="v22-hero-reel relative mx-auto w-full max-w-6xl">
            <NitroReel tabsOverlay />
            <div
              aria-hidden="true"
              className="pointer-events-none absolute inset-x-0 top-0 z-30 hidden sm:block"
            >
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
        </div>
      </div>
    </section>
  );
}

function PageAtmosphere() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 left-1/2 -z-10 w-screen -translate-x-1/2 overflow-hidden"
    >
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

export function ClientPage() {
  const reduced = useReducedMotion() ?? false;

  return (
    <div className="relative isolate">
      <PageAtmosphere />

      <Hero reduced={reduced} />

      <section
        id="observe"
        className="border-cc-card-border scroll-mt-24 border-t py-20 text-center sm:py-28"
      >
        <RevealOnScroll>
          <SectionIntro
            index="01"
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

      <section
        id="signals"
        className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
      >
        <RevealOnScroll>
          <SectionIntro
            index="02"
            eyebrow="Operations Console"
            title="See the health of your API at a glance."
            lead="Track p95 and p99 latency, throughput, error rate, top clients, and the slowest spans from the same telemetry source."
          />
        </RevealOnScroll>
        <RevealOnScroll
          className="mt-12"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <SignalsBento />
        </RevealOnScroll>
      </section>

      <Showcase
        id="trace"
        index="03"
        eyebrow="Trace"
        title="Follow one request across your whole backend."
        body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
        visual={<NitroTrace className="w-full" />}
        reverse
      />

      <Showcase
        id="diagnose"
        index="04"
        eyebrow="Diagnose"
        title="Move from symptoms to cause."
        body="Start with an error spike, open the affected operation, inspect the trace, and find the failing stack frame without digging through disconnected logs."
        visual={<NitroDiagnose className="w-full" />}
      />

      <Showcase
        id="fusion"
        index="05"
        eyebrow="Compose"
        title="Understand how your operations are executed."
        body="Inspect distributed execution plans, see how work is split across services, and understand how one request becomes one response."
        visual={<NitroFusion className="w-full" />}
        reverse
      />

      <Showcase
        id="schema"
        index="06"
        eyebrow="Schema Governance"
        title="Know the impact before you merge."
        body="Nitro compares schema changes with published clients and gives teams a clear signal on what is safe, risky, or breaking."
        visual={<NitroSchema className="w-full" />}
        aside={<ClassificationCard />}
      />

      <DeliveryBand />

      <Showcase
        id="author"
        index="08"
        eyebrow="Workspace"
        title="Give teams a shared workspace."
        body="Explore schemas, run operations, validate documents, and keep important API work connected to the rest of Nitro."
        visual={<NitroCompose className="w-full" />}
      />

      <section className="border-cc-card-border border-t py-24 text-center sm:py-32">
        <RevealOnScroll className="mx-auto flex max-w-2xl flex-col items-center gap-6">
          <Eyebrow>Ready when you are</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 text-balance">
            Put your API on one control plane.
          </h2>
          <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
            Start with production visibility, then add tracing, schema
            governance, client safety, and release checks as your team grows.
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
