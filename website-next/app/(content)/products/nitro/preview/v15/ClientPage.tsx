"use client";

import { motion, useReducedMotion, useScroll, useSpring } from "motion/react";
import type { Variants } from "motion/react";
import type { CSSProperties, ReactNode } from "react";

import { ControlPlaneConsole } from "@/src/components/nitro/ControlPlaneConsole";
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

// Brand spectrum gradient. Used sparingly as the single "color event" per Linear.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// A soft teal radial, placed BEHIND a product frame as a glow (never a mask over it).
const FRAME_GLOW =
  "radial-gradient(58% 58% at 50% 40%, rgba(94,234,212,0.16), transparent 70%)";

// Band seam — a 1px gradient hairline that separates two full-width bands.
const SEAM =
  "linear-gradient(90deg, transparent, rgba(245,241,234,0.12) 50%, transparent)";

// Subtle neutral tint that lifts a band a shade above the near-black base.
const BAND_TINT =
  "linear-gradient(180deg, rgba(245,241,234,0.008), rgba(245,241,234,0.024) 50%, rgba(245,241,234,0.008))";

// Faint vertical hairline texture for the occasional band (kept barely visible).
const BAND_TEXTURE =
  "repeating-linear-gradient(90deg, rgba(245,241,234,0.03) 0 1px, transparent 1px 96px)";

// Shared ease for the fade + rise reveals.
const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1];

/* ────────────────────────────────────────────────────────────────────────
   Motion — fade + slight rise entrance only, honoring prefers-reduced-motion
   ──────────────────────────────────────────────────────────────────────── */

interface RiseVariants {
  readonly parent: Variants;
  readonly text: Variants;
  readonly visual: Variants;
  readonly hero: Variants;
}

/** Builds the reveal variants, collapsing to fade-only when motion is reduced. */
function useRiseVariants(): RiseVariants {
  const reduced = useReducedMotion();

  if (reduced) {
    const fade: Variants = {
      hidden: { opacity: 0 },
      show: { opacity: 1, transition: { duration: 0.3 } },
    };
    return {
      parent: { hidden: {}, show: { transition: { staggerChildren: 0.05 } } },
      text: fade,
      visual: fade,
      hero: fade,
    };
  }

  return {
    parent: { hidden: {}, show: { transition: { staggerChildren: 0.08 } } },
    text: {
      hidden: { opacity: 0, y: 24 },
      show: { opacity: 1, y: 0, transition: { duration: 0.6, ease: EASE } },
    },
    visual: {
      hidden: { opacity: 0, y: 28 },
      show: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.6, ease: EASE },
      },
    },
    hero: {
      hidden: { opacity: 0, y: 36 },
      show: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.7, ease: EASE },
      },
    },
  };
}

const VIEWPORT = {
  once: true,
  margin: "0px 0px -12% 0px",
  amount: 0.2,
} as const;

interface RevealProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly kind?: "text" | "visual" | "hero";
}

/** Single fade + rise reveal that fires once when it scrolls into view, then stays crisp. */
function Reveal({ children, className, kind = "text" }: RevealProps) {
  const v = useRiseVariants();
  return (
    <motion.div
      className={className}
      variants={v[kind]}
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT}
    >
      {children}
    </motion.div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Backdrop — graduated neutral near-black (counters the blue base)
   ──────────────────────────────────────────────────────────────────────── */

/** Fixed, page-local backdrop that overpaints the blue body base with neutral near-black. */
function Backdrop() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none fixed inset-0 -z-10 overflow-hidden"
    >
      {/* (a) neutral near-black vertical wash — overrides the body blue */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "linear-gradient(180deg, #0b0c0e 0%, #090a0c 38%, #08090a 72%, #060708 100%)",
        }}
      />
      {/* (b) hero bloom — soft teal, huge, anchored top-center behind the reel */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "radial-gradient(58% 40% at 50% 4%, rgba(94,234,212,0.12), transparent 66%)",
        }}
      />
      {/* (c) low-frequency depth glows lower on the page, off-axis, for parallax weight */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "radial-gradient(34% 26% at 84% 40%, rgba(94,234,212,0.05), transparent 70%), radial-gradient(40% 30% at 10% 72%, rgba(245,241,234,0.03), transparent 72%), radial-gradient(30% 22% at 62% 88%, rgba(94,234,212,0.04), transparent 72%)",
        }}
      />
      {/* (d) side vignette — darken the outer columns so the eye stays centre-stage */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "linear-gradient(90deg, rgba(6,7,8,0.7), transparent 18%, transparent 82%, rgba(6,7,8,0.7))",
        }}
      />
      {/* (e) vignette floor so the page ends in true black, not navy */}
      <div
        className="absolute inset-x-0 bottom-0 h-[48vh]"
        style={{
          background: "linear-gradient(180deg, transparent, #060708 86%)",
        }}
      />
    </div>
  );
}

/** Fixed left scroll rail — a thin index track that fills as the page advances. */
function ScrollRail() {
  const reduced = useReducedMotion();
  const { scrollYProgress } = useScroll();
  const fill = useSpring(scrollYProgress, {
    stiffness: 90,
    damping: 30,
    mass: 0.3,
  });
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none fixed top-1/2 left-5 z-30 hidden -translate-y-1/2 flex-col items-center gap-3 xl:flex"
    >
      <span className="text-cc-ink-dim font-mono text-[0.58rem] tracking-[0.24em] tabular-nums">
        01
      </span>
      <div
        className="relative h-44 w-px overflow-hidden"
        style={{
          background:
            "linear-gradient(180deg, transparent, rgba(245,241,234,0.14) 14%, rgba(245,241,234,0.14) 86%, transparent)",
        }}
      >
        <motion.div
          className="absolute inset-x-0 top-0 h-full w-px origin-top"
          style={{
            scaleY: reduced ? 1 : fill,
            background:
              "linear-gradient(180deg, #5eead4, rgba(94,234,212,0.25))",
          }}
        />
      </div>
      <span className="text-cc-ink-dim font-mono text-[0.58rem] tracking-[0.24em] tabular-nums">
        09
      </span>
    </div>
  );
}

interface BandProps {
  readonly children: ReactNode;
  readonly tint?: boolean;
  readonly texture?: boolean;
  readonly seam?: boolean;
}

/**
 * Full-width section band. Alternates the near-black base with a subtly tinted
 * sheet, adds an optional faint texture, and draws a 1px gradient seam at its
 * top edge. The base backdrop and glows read through on untinted bands.
 */
function Band({
  children,
  tint = false,
  texture = false,
  seam = true,
}: BandProps) {
  return (
    <div className="relative">
      {seam && (
        <div
          aria-hidden="true"
          className="absolute inset-x-0 top-0 z-10 h-px"
          style={{ background: SEAM }}
        />
      )}
      {tint && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0"
          style={{ background: BAND_TINT }}
        />
      )}
      {texture && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 opacity-40"
          style={{ background: BAND_TEXTURE }}
        />
      )}
      <div className="relative">{children}</div>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Type — mono microlabels + two-tone statement lines
   ──────────────────────────────────────────────────────────────────────── */

interface MicroLabelProps {
  readonly fig: string;
  readonly children: ReactNode;
}

function MicroLabel({ fig, children }: MicroLabelProps) {
  return (
    <div className="flex items-center gap-2.5">
      <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-[0.24em] tabular-nums">
        {fig}
      </span>
      <span
        aria-hidden="true"
        className="h-px w-6"
        style={{
          background:
            "linear-gradient(90deg, rgba(94,234,212,0.6), transparent)",
        }}
      />
      <span className="text-cc-accent font-mono text-[0.62rem] tracking-[0.24em] uppercase">
        {children}
      </span>
    </div>
  );
}

interface SectionIntroProps {
  readonly fig: string;
  readonly label: string;
  readonly titleBright: string;
  readonly titleDim?: string;
  readonly lead?: string;
  readonly align?: "center" | "start";
}

/** Mono microlabel + two-tone statement line (bright lead + muted continuation) + optional lead. */
function SectionIntro({
  fig,
  label,
  titleBright,
  titleDim,
  lead,
  align = "center",
}: SectionIntroProps) {
  return (
    <div
      className={[
        "flex flex-col gap-4",
        align === "center"
          ? "mx-auto max-w-2xl items-center text-center"
          : "max-w-md items-start",
      ].join(" ")}
    >
      <MicroLabel fig={fig}>{label}</MicroLabel>
      <h2 className="font-heading text-h3 leading-[1.12] text-balance">
        <span className="text-cc-heading">{titleBright}</span>
        {titleDim ? (
          <>
            {" "}
            <span className="text-cc-ink-dim">{titleDim}</span>
          </>
        ) : null}
      </h2>
      {lead && (
        <p className="text-cc-ink max-w-2xl text-base leading-relaxed text-pretty sm:text-lg">
          {lead}
        </p>
      )}
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Surfaces — crisp product frame + facet cards
   ──────────────────────────────────────────────────────────────────────── */

interface FramedVisualProps {
  readonly children: ReactNode;
}

/**
 * Crisp product frame. The screenshot stays fully readable: a thin hairline
 * border, a soft shadow, and a soft radial glow placed BEHIND the frame.
 * No blur or mask ever touches the product UI.
 */
function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -inset-x-8 -inset-y-6 -z-10 blur-3xl"
        style={{ background: FRAME_GLOW }}
      />
      <div className="border-cc-white/10 bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/50">
        {children}
      </div>
    </div>
  );
}

interface CardProps {
  readonly className?: string;
  readonly children: ReactNode;
}

/** Translucent facet surface — a lit sheet, not a bordered box. */
function Card({ className, children }: CardProps) {
  return (
    <div
      className={[
        "relative overflow-hidden rounded-2xl border border-white/[0.055] bg-white/[0.015] backdrop-blur-md",
        className ?? "",
      ].join(" ")}
      style={{ boxShadow: "inset 0 1px 0 rgba(245,241,234,0.06)" }}
    >
      {/* Top-left facet sheen so the card reads as a lit sheet, not a flat box. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            "radial-gradient(120% 80% at 0% 0%, rgba(245,241,234,0.045), transparent 46%)",
        }}
      />
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
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.16em] uppercase">
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

interface ShowcaseProps {
  readonly id: string;
  readonly fig: string;
  readonly label: string;
  readonly titleBright: string;
  readonly titleDim: string;
  readonly body: string;
  readonly visual: ReactNode;
  readonly aside?: ReactNode;
  readonly reverse?: boolean;
}

/** Alternating split: two-tone headline + crisply framed product graphic, staged reveal. */
function Showcase({
  id,
  fig,
  label,
  titleBright,
  titleDim,
  body,
  visual,
  aside,
  reverse = false,
}: ShowcaseProps) {
  const v = useRiseVariants();
  return (
    <motion.section
      id={id}
      className="scroll-mt-24 py-28 sm:py-40"
      variants={v.parent}
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT}
    >
      <div className="mx-auto grid max-w-6xl items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <motion.div
          variants={v.text}
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex flex-col gap-5">
            <SectionIntro
              fig={fig}
              label={label}
              titleBright={titleBright}
              titleDim={titleDim}
              align="start"
            />
            <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
              {body}
            </p>
            {aside}
          </div>
        </motion.div>

        <motion.div
          variants={v.visual}
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <FramedVisual>{visual}</FramedVisual>
        </motion.div>
      </div>
    </motion.section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Principles triplet — crisp monochrome isometric line-art (Linear FIG rows)
   ──────────────────────────────────────────────────────────────────────── */

interface GlyphProps {
  readonly className?: string;
  readonly strokeWidth?: number;
}

function RealGlyph({ className, strokeWidth = 1.1 }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 48 48"
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      {/* isometric stacked graph plates */}
      <path d="M24 6 40 15 24 24 8 15Z" />
      <path d="M8 15v9l16 9 16-9v-9" opacity="0.85" />
      <path d="M8 24v9l16 9 16-9v-9" opacity="0.55" />
      <path d="M24 24v18" opacity="0.4" />
    </svg>
  );
}

function ImpactGlyph({ className, strokeWidth = 1.1 }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 48 48"
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      {/* isometric ranked bars on a floor plane */}
      <path d="M6 33 24 42 42 33" opacity="0.5" />
      <path d="M13 30V22l7-4v8Z" />
      <path d="M22 33V19l7-4v14Z" opacity="0.85" />
      <path d="M31 36V15l7-4v21Z" opacity="0.7" />
    </svg>
  );
}

function SafeGlyph({ className, strokeWidth = 1.1 }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 48 48"
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      {/* isometric shield with a check */}
      <path d="M24 6 38 11v11c0 9-6.2 14.6-14 18-7.8-3.4-14-9-14-18V11Z" />
      <path d="M24 6v41" opacity="0.35" />
      <path d="M17 24l5 5 9-11" opacity="0.9" />
    </svg>
  );
}

const PRINCIPLES: readonly {
  readonly title: string;
  readonly body: string;
  readonly Glyph: (p: GlyphProps) => ReactNode;
}[] = [
  {
    title: "Real, not mocked",
    body: "Author against real data with a schema-aware editor on your composed graph, not a mock.",
    Glyph: RealGlyph,
  },
  {
    title: "Ranked by impact",
    body: "Observe by impact, with every signal ranked so you fix what actually hurts.",
    Glyph: ImpactGlyph,
  },
  {
    title: "Safe by default",
    body: "Evolve without breaking: changes are classified and checked against published clients before they ship.",
    Glyph: SafeGlyph,
  },
];

function PrinciplesBand() {
  const v = useRiseVariants();
  return (
    <motion.section
      className="py-24 sm:py-32"
      variants={v.parent}
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT}
    >
      <div className="mx-auto grid max-w-5xl grid-cols-1 gap-y-12 sm:grid-cols-3 sm:gap-y-0">
        {PRINCIPLES.map((p, i) => (
          <motion.div
            key={p.title}
            variants={v.text}
            className={["px-8", i > 0 ? "sm:border-l" : ""].join(" ")}
            style={
              i > 0
                ? {
                    borderImage:
                      "linear-gradient(180deg, transparent, rgba(245,241,234,0.12) 30%, rgba(245,241,234,0.12) 70%, transparent) 1",
                  }
                : undefined
            }
          >
            <p.Glyph className="text-cc-accent h-9 w-9" strokeWidth={1.1} />
            <h3 className="text-cc-heading font-heading text-h6 mt-5">
              {p.title}
            </h3>
            <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
              {p.body}
            </p>
          </motion.div>
        ))}
      </div>
    </motion.section>
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
    <div className="relative mx-auto max-w-5xl">
      {/* One shared soft glow behind the whole grid — tiles read as facets of one lit surface. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(50% 40% at 50% 30%, rgba(94,234,212,0.08), transparent 70%)",
        }}
      />
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
        {/* Latency p95/p99 */}
        <Card className="sm:col-span-4">
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
          orders-api · v15
        </span>
        <span className="text-cc-danger text-caption font-mono">
          publish blocked
        </span>
      </div>
      <div className="divide-y divide-white/[0.06] border-t border-white/[0.06]">
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
      <div className="text-cc-ink-dim border-t border-white/[0.06] px-4 py-2.5 font-mono text-[11px]">
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

interface CheckIconMarkProps {
  readonly state: "pass" | "fail";
}

function CheckIconMark({ state }: CheckIconMarkProps) {
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
  const v = useRiseVariants();
  return (
    <motion.section
      id="delivery"
      className="scroll-mt-24 py-28 sm:py-40"
      variants={v.parent}
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT}
    >
      <motion.div variants={v.text}>
        <SectionIntro
          fig="07"
          label="Delivery"
          titleBright="Ship on green,"
          titleDim="roll out with a safety net."
          lead="Persisted, trusted operations lock the queries clients can send. Schema and client checks run in CI, so a breaking change fails the build instead of the customer."
        />
      </motion.div>

      <div className="mx-auto mt-12 grid max-w-5xl gap-4 lg:grid-cols-3">
        <motion.div variants={v.visual} className="lg:col-span-2">
          <Card>
            <div className="flex items-center justify-between px-5 py-3.5">
              <span className="text-cc-heading font-heading text-h6">
                CI schema check
              </span>
              <span className="text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08] rounded border px-2 py-0.5 font-mono text-[10px] tracking-[0.12em]">
                FAILED
              </span>
            </div>
            <div className="divide-y divide-white/[0.06] border-t border-white/[0.06]">
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
            <div className="text-cc-ink-dim border-t border-white/[0.06] px-5 py-3 font-mono text-[11px]">
              merging is blocked until every check passes
            </div>
          </Card>
        </motion.div>

        <motion.div variants={v.visual}>
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
              <div className="rounded-lg border border-white/[0.08] bg-black/20 p-3 font-mono text-xs">
                <div className="text-cc-ink-dim">POST /graphql</div>
                <div className="text-cc-accent mt-1 truncate">
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
        </motion.div>
      </div>
    </motion.section>
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
  const v = useRiseVariants();
  return (
    <motion.section
      id="ecosystem"
      className="scroll-mt-24 py-28 sm:py-40"
      variants={v.parent}
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT}
    >
      <motion.div variants={v.text}>
        <SectionIntro
          fig="09"
          label="Platform"
          titleBright="One open-source stack,"
          titleDim="end to end."
          lead="Nitro sits on top of the same GraphQL platform you already build with, from the server that answers to the gateway that composes."
        />
      </motion.div>

      <div className="mx-auto mt-12 grid max-w-5xl gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {PLATFORM.map(({ name, role, Icon }, i) => (
          <motion.div key={name} variants={v.visual}>
            <Card className="h-full">
              <div className="flex flex-col gap-4 p-5">
                <div className="text-cc-accent flex h-10 w-10 items-center justify-center rounded-lg border border-white/[0.08] bg-black/20">
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
                        : "linear-gradient(90deg, rgba(94,234,212,0.5), transparent)",
                  }}
                />
              </div>
            </Card>
          </motion.div>
        ))}
      </div>

      <motion.div variants={v.text} className="mt-6">
        <div className="text-cc-ink-dim text-caption text-center font-mono tracking-[0.14em] uppercase">
          MIT licensed · built in the open · one graph for .NET
        </div>
      </motion.div>
    </motion.section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Centered Observe + Signals sections (staged reveal)
   ──────────────────────────────────────────────────────────────────────── */

function ObserveSection() {
  const v = useRiseVariants();
  return (
    <motion.section
      id="observe"
      className="scroll-mt-24 py-28 text-center sm:py-40"
      variants={v.parent}
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT}
    >
      <motion.div variants={v.text}>
        <SectionIntro
          fig="02"
          label="Observe"
          titleBright="See exactly how your API behaves"
          titleDim="in production."
          lead="OpenTelemetry-native monitoring: latency, throughput, and error rate per operation and per client, ranked by the impact score that tells you what to fix first."
        />
      </motion.div>
      <motion.div
        variants={v.visual}
        className="mx-auto mt-14 max-w-5xl sm:mt-16"
      >
        <FramedVisual>
          <ControlPlaneConsole className="w-full" />
        </FramedVisual>
      </motion.div>
    </motion.section>
  );
}

function SignalsSection() {
  const v = useRiseVariants();
  return (
    <motion.section
      id="signals"
      className="scroll-mt-24 py-28 sm:py-40"
      variants={v.parent}
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT}
    >
      <motion.div variants={v.text}>
        <SectionIntro
          fig="03"
          label="Every signal"
          titleBright="One console,"
          titleDim="every signal in view."
          lead="p95 and p99 latency, throughput, error budget, per-client usage, impact ranking, and the slow span, all reading from the same telemetry."
        />
      </motion.div>
      <motion.div variants={v.visual} className="mt-12">
        <SignalsBento />
      </motion.div>
    </motion.section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Page
   ──────────────────────────────────────────────────────────────────────── */

export function ClientPage() {
  return (
    <>
      <Backdrop />
      <ScrollRail />

      {/* HERO ─────────────────────────────────────────────────── */}
      <Band seam={false}>
        <section className="pt-10 pb-24 text-center">
          <Reveal className="mx-auto flex max-w-3xl flex-col items-center gap-6">
            <div className="flex flex-col items-center gap-4">
              <span
                aria-hidden="true"
                className="h-px w-24 rounded-full"
                style={{ background: SPECTRUM }}
              />
              <MicroLabel fig="1.0">The Control Plane for GraphQL</MicroLabel>
            </div>
            <h1 className="font-heading text-h1 text-balance">
              <span className="text-cc-heading">Your whole API,</span>{" "}
              <span className="text-cc-ink-dim">on one control plane.</span>
            </h1>
            <p className="lead text-cc-ink mx-auto max-w-2xl !text-xl !leading-relaxed">
              Author operations, watch them run, trace every request, and evolve
              your schema without breaking the clients you ship to.
            </p>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="https://nitro.chillicream.com">
                Launch Nitro
              </OutlineButton>
            </div>
          </Reveal>

          {/* 5-tab reel: its own app window, so it renders frameless. A soft radial
            glow sits BEHIND it; the UI itself stays fully crisp. */}
          <Reveal kind="hero" className="mt-16 sm:mt-20">
            <div className="relative mx-auto w-full max-w-6xl">
              <div
                aria-hidden="true"
                className="pointer-events-none absolute -inset-x-10 -inset-y-8 -z-10 blur-3xl"
                style={{
                  background:
                    "radial-gradient(50% 45% at 50% 30%, rgba(94,234,212,0.16), transparent 70%)",
                }}
              />
              <NitroReel tabsOverlay />
            </div>
          </Reveal>
        </section>
      </Band>

      {/* PRINCIPLES ───────────────────────────────────────────── */}
      <Band tint texture>
        <PrinciplesBand />
      </Band>

      {/* AUTHOR ───────────────────────────────────────────────── */}
      <Band>
        <Showcase
          id="author"
          fig="01"
          label="Author"
          titleBright="Compose GraphQL"
          titleDim="against real, federated data."
          body="A schema-aware editor with live validation and one-click operations, running against your composed graph, not a mock."
          visual={<NitroCompose className="w-full" />}
        />
      </Band>

      {/* OBSERVE ──────────────────────────────────────────────── */}
      <Band tint>
        <ObserveSection />
      </Band>

      {/* SIGNALS BENTO ────────────────────────────────────────── */}
      <Band>
        <SignalsSection />
      </Band>

      {/* TRACE ────────────────────────────────────────────────── */}
      <Band tint>
        <Showcase
          id="trace"
          fig="04"
          label="Trace"
          titleBright="Follow one request"
          titleDim="across your whole backend."
          body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
          visual={<NitroTrace className="w-full" />}
          reverse
        />
      </Band>

      {/* DIAGNOSE ─────────────────────────────────────────────── */}
      <Band>
        <Showcase
          id="diagnose"
          fig="05"
          label="Diagnose"
          titleBright="From an error spike"
          titleDim="to the line that threw it."
          body="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it. No log spelunking."
          visual={<NitroDiagnose className="w-full" />}
        />
      </Band>

      {/* EVOLVE / SCHEMA ──────────────────────────────────────── */}
      <Band tint>
        <Showcase
          id="schema"
          fig="06"
          label="Evolve"
          titleBright="Change your schema"
          titleDim="without breaking clients."
          body="The registry classifies every change as safe, dangerous, or breaking and checks it against published clients before it ships."
          visual={<NitroSchema className="w-full" />}
          aside={<ClassificationCard />}
          reverse
        />
      </Band>

      {/* DELIVERY / SAFETY ────────────────────────────────────── */}
      <Band>
        <DeliveryBand />
      </Band>

      {/* FUSION ───────────────────────────────────────────────── */}
      <Band tint>
        <Showcase
          id="fusion"
          fig="08"
          label="Compose"
          titleBright="One graph,"
          titleDim="executed across every subgraph."
          body="Fusion shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
          visual={<NitroFusion className="w-full" />}
        />
      </Band>

      {/* ECOSYSTEM ────────────────────────────────────────────── */}
      <Band texture>
        <EcosystemStrip />
      </Band>

      {/* CTA ──────────────────────────────────────────────────── */}
      <Band tint>
        <section className="py-28 text-center sm:py-40">
          <Reveal className="mx-auto flex max-w-2xl flex-col items-center gap-6">
            <span
              aria-hidden="true"
              className="h-px w-24 rounded-full"
              style={{ background: SPECTRUM }}
            />
            <span className="text-cc-accent font-mono text-[0.62rem] tracking-[0.24em] uppercase">
              Ready when you are
            </span>
            <h2 className="font-heading text-h2 text-balance">
              <span className="text-cc-heading">Put your API</span>{" "}
              <span className="text-cc-ink-dim">on the control plane.</span>
            </h2>
            <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
              Start in the GraphQL IDE in seconds, then grow into observability,
              tracing, and a registry that keeps your schema and clients in
              sync.
            </p>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="https://nitro.chillicream.com">
                Launch Nitro
              </OutlineButton>
            </div>
          </Reveal>
        </section>
      </Band>
    </>
  );
}
