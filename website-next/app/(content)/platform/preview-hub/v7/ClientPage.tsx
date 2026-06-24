"use client";

import Link from "next/link";
import {
  animate,
  motion,
  MotionConfig,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";
import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Brand palette                                                             */
/* -------------------------------------------------------------------------- */

const CYAN = "#16b9e4";
const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

/* -------------------------------------------------------------------------- */
/*  Arc + surface data                                                        */
/*  Eight platform surfaces routed to their real pages.                       */
/* -------------------------------------------------------------------------- */

type ArcKey = "build" | "run" | "evolve";

interface ArcTheme {
  readonly label: string;
  readonly intent: string;
  readonly color: string;
}

const ARCS: Record<ArcKey, ArcTheme> = {
  build: {
    label: "Build",
    intent: "Author the API and let agents help.",
    color: CYAN,
  },
  run: {
    label: "Run",
    intent: "Operate it in production with eyes on every call.",
    color: VIOLET,
  },
  evolve: {
    label: "Evolve",
    intent: "Ship change without breaking published clients.",
    color: CORAL,
  },
};

interface Surface {
  readonly id: string;
  readonly arc: ArcKey;
  readonly title: string;
  readonly outcome: string;
  readonly href: string;
  /** Short monospaced label rendered inside the surface node circle. */
  readonly shortLabel: string;
  /** Position on the centerpiece circuit, in SVG user units (viewBox 0..800 x 0..520). */
  readonly cx: number;
  readonly cy: number;
}

const SURFACES: readonly Surface[] = [
  {
    id: "build",
    arc: "build",
    title: "Build",
    outcome: "Ship from the code that runs it.",
    href: "/platform/build",
    shortLabel: "build",
    cx: 140,
    cy: 170,
  },
  {
    id: "agentic-coding",
    arc: "build",
    title: "Agentic Coding",
    outcome: "Give coding agents a feedback loop.",
    href: "/platform/agentic-coding",
    shortLabel: "agents",
    cx: 240,
    cy: 90,
  },
  {
    id: "observability",
    arc: "run",
    title: "Observability",
    outcome: "See what the API is doing, right now.",
    href: "/platform/observability",
    shortLabel: "observe",
    cx: 560,
    cy: 90,
  },
  {
    id: "workflows",
    arc: "run",
    title: "Workflows",
    outcome: "Let work continue after the request.",
    href: "/platform/workflows",
    shortLabel: "workflow",
    cx: 660,
    cy: 170,
  },
  {
    id: "release-safety",
    arc: "evolve",
    title: "Release Safety",
    outcome: "Change contracts with a safety net.",
    href: "/platform/release-safety",
    shortLabel: "release",
    cx: 660,
    cy: 350,
  },
  {
    id: "analytics",
    arc: "run",
    title: "Analytics",
    outcome: "Know which fields earn their keep.",
    href: "/platform/analytics",
    shortLabel: "analytic",
    cx: 560,
    cy: 430,
  },
  {
    id: "continuous-integration",
    arc: "evolve",
    title: "Continuous Integration",
    outcome: "Innovate with confidence at merge time.",
    href: "/platform/continuous-integration",
    shortLabel: "ci",
    cx: 240,
    cy: 430,
  },
  {
    id: "ecosystem",
    arc: "evolve",
    title: "Ecosystem",
    outcome: "An ecosystem you can trust and reuse.",
    href: "/platform/ecosystem",
    shortLabel: "ecosys",
    cx: 140,
    cy: 350,
  },
];

const NITRO = { cx: 400, cy: 260 };

/* -------------------------------------------------------------------------- */
/*  Pulse path                                                                */
/*  Build -> Agentic Coding -> Nitro -> Observability -> Workflows -> Nitro   */
/*  -> Release Safety -> Analytics -> CI -> Ecosystem -> back to Build.       */
/* -------------------------------------------------------------------------- */

function buildPulsePath(): string {
  const byId = (id: string) => SURFACES.find((s) => s.id === id)!;
  const seq = [
    byId("build"),
    byId("agentic-coding"),
    NITRO,
    byId("observability"),
    byId("workflows"),
    NITRO,
    byId("release-safety"),
    byId("analytics"),
    byId("continuous-integration"),
    byId("ecosystem"),
    byId("build"),
  ];
  return seq.map((p, i) => `${i === 0 ? "M" : "L"} ${p.cx} ${p.cy}`).join(" ");
}

/* -------------------------------------------------------------------------- */
/*  Chrome primitives                                                         */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

function Hero() {
  const heroLines: readonly ReactNode[] = [
    "One GraphQL platform,",
    <>
      one <span style={{ color: CYAN }}>live circuit</span>.
    </>,
  ];
  return (
    <header className="flex flex-col gap-7">
      <Eyebrow>The ChilliCream Platform</Eyebrow>
      <h1 className="font-heading text-hero text-cc-heading max-w-4xl font-semibold tracking-tight">
        {heroLines.map((line, i) => (
          <motion.span
            key={i}
            className="block"
            initial={{ opacity: 0, y: 16 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{ duration: 0.6, delay: i * 0.12, ease: "easeOut" }}
          >
            {line}
          </motion.span>
        ))}
      </h1>
      {/*
        The brand spectrum (cyan -> violet -> coral) is reserved for the single
        Nitro hub ring in the centerpiece below; one element, one event.
      */}
      <motion.p
        className="text-cc-ink lead max-w-2xl"
        initial={{ opacity: 0, y: 12 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, margin: "-10% 0px" }}
        transition={{ duration: 0.6, delay: 0.25, ease: "easeOut" }}
      >
        The GraphQL platform runs as a single circuit. A request never visits
        one tool, it travels Build, Run, and Evolve while Nitro keeps every
        surface in sync.
      </motion.p>
      <div className="flex flex-wrap items-center gap-3">
        <SolidButton href="/products/nitro">Explore Nitro</SolidButton>
        <OutlineButton href="#circuit">See the surfaces</OutlineButton>
      </div>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Centerpiece: Platform Circuit                                             */
/* -------------------------------------------------------------------------- */

interface PulseDotProps {
  readonly progress: ReturnType<typeof useMotionValue<number>>;
  readonly pathRef: RefObject<SVGPathElement | null>;
  readonly reduced: boolean;
}

function PulseDot({ progress, pathRef, reduced }: PulseDotProps) {
  const cx = useMotionValue(0);
  const cy = useMotionValue(0);

  useEffect(() => {
    const path = pathRef.current;
    if (!path) return;
    const total = path.getTotalLength();
    const update = (p: number) => {
      const point = path.getPointAtLength((((p % 1) + 1) % 1) * total);
      cx.set(point.x);
      cy.set(point.y);
    };
    update(progress.get());
    const unsub = progress.on("change", update);
    return () => unsub();
  }, [progress, cx, cy, pathRef]);

  if (reduced) return null;

  return (
    <>
      <motion.circle
        r={9}
        cx={cx}
        cy={cy}
        fill={CYAN}
        opacity={0.25}
        style={{ filter: "blur(6px)" }}
      />
      <motion.circle
        r={4}
        cx={cx}
        cy={cy}
        fill="var(--color-cc-heading)"
        style={{
          filter: `drop-shadow(0 0 6px ${CYAN}) drop-shadow(0 0 14px ${CYAN})`,
        }}
      />
    </>
  );
}

interface SurfaceNodeProps {
  readonly surface: Surface;
  readonly progress: ReturnType<typeof useMotionValue<number>>;
  readonly pulseTotal: number;
  readonly pathRef: RefObject<SVGPathElement | null>;
  readonly reduced: boolean;
  readonly onHover: (s: Surface | null) => void;
}

function SurfaceNode({
  surface,
  progress,
  pulseTotal,
  pathRef,
  reduced,
  onHover,
}: SurfaceNodeProps) {
  const arc = ARCS[surface.arc];
  const lit = useMotionValue(reduced ? 0.35 : 0.18);
  const outerStrokeOpacity = useTransform(lit, (v) => v * 0.35);

  // Compute the phase along the path where this node sits.
  const phaseRef = useRef<number>(0);
  useEffect(() => {
    const path = pathRef.current;
    if (!path || pulseTotal <= 0) return;
    // Sample to find closest length to the node.
    let best = 0;
    let bestDist = Infinity;
    const steps = 240;
    for (let i = 0; i <= steps; i++) {
      const len = (i / steps) * pulseTotal;
      const pt = path.getPointAtLength(len);
      const dx = pt.x - surface.cx;
      const dy = pt.y - surface.cy;
      const d = dx * dx + dy * dy;
      if (d < bestDist) {
        bestDist = d;
        best = len;
      }
    }
    phaseRef.current = best / pulseTotal;
  }, [pathRef, pulseTotal, surface.cx, surface.cy]);

  useEffect(() => {
    if (reduced) return;
    const update = (p: number) => {
      const phase = phaseRef.current;
      let d = Math.abs((((p % 1) + 1) % 1) - phase);
      if (d > 0.5) d = 1 - d;
      // window of ~0.06 of total path
      const v = Math.max(0, 1 - d / 0.06);
      lit.set(0.18 + v * 0.82);
    };
    update(progress.get());
    const unsub = progress.on("change", update);
    return () => unsub();
  }, [progress, lit, reduced]);

  return (
    <g
      onMouseEnter={() => onHover(surface)}
      onMouseLeave={() => onHover(null)}
      style={{ cursor: "pointer" }}
    >
      <a href={surface.href} aria-label={`Open ${surface.title}`}>
        <motion.circle
          cx={surface.cx}
          cy={surface.cy}
          r={22}
          fill="rgba(12,19,34,0.9)"
          stroke={arc.color}
          strokeOpacity={lit}
          strokeWidth={2}
        />
        <motion.circle
          cx={surface.cx}
          cy={surface.cy}
          r={32}
          fill="none"
          stroke={arc.color}
          strokeOpacity={outerStrokeOpacity}
          strokeWidth={1}
        />
        <text
          x={surface.cx}
          y={surface.cy + 4}
          textAnchor="middle"
          fontSize={10}
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fill="var(--color-cc-heading)"
          opacity={0.85}
          style={{ pointerEvents: "none" }}
        >
          {surface.shortLabel}
        </text>
      </a>
    </g>
  );
}

function PlatformCircuit() {
  const reduced = useReducedMotion() ?? false;
  const containerRef = useRef<HTMLDivElement>(null);
  const pathRef = useRef<SVGPathElement>(null);
  const inView = useInView(containerRef, { margin: "-10% 0px" });

  // Time-based pulse: one full loop every ~16s while the section is in view.
  // No scroll coupling — animation runs independently of scroll position.
  const pulse = useMotionValue(0);

  useEffect(() => {
    if (reduced || !inView) return;
    let raf = 0;
    let last = performance.now();
    let drift = pulse.get();
    const tick = (now: number) => {
      const dt = (now - last) / 1000;
      last = now;
      drift += dt / 16;
      pulse.set(drift);
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [inView, reduced, pulse]);

  const [hovered, setHovered] = useState<Surface | null>(null);
  const [pathLen, setPathLen] = useState(0);
  useEffect(() => {
    const path = pathRef.current;
    if (path) setPathLen(path.getTotalLength());
  }, []);

  const pulsePathD = useMemo(() => buildPulsePath(), []);

  return (
    <section
      id="circuit"
      ref={containerRef}
      className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-2xl border"
    >
      <div className="flex flex-col gap-4 px-6 pt-6 md:px-10 md:pt-10">
        <Eyebrow>Centerpiece · Platform Circuit</Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading max-w-3xl font-semibold tracking-tight">
          A request never visits one tool. It travels the platform.
        </h2>
      </div>
      <div className="relative aspect-[8/5.4] w-full">
        <svg
          viewBox="0 0 800 520"
          className="h-full w-full"
          role="img"
          aria-label="Animated platform circuit with Nitro at the center and eight platform surfaces on three orbital arcs."
        >
          <defs>
            <radialGradient id="v7-nitro-glow" cx="50%" cy="50%" r="50%">
              <stop offset="0%" stopColor={CYAN} stopOpacity="0.45" />
              <stop offset="60%" stopColor={CYAN} stopOpacity="0.1" />
              <stop offset="100%" stopColor={CYAN} stopOpacity="0" />
            </radialGradient>
            <linearGradient id="v7-spectrum" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor={CYAN} />
              <stop offset="50%" stopColor={VIOLET} />
              <stop offset="100%" stopColor={CORAL} />
            </linearGradient>
          </defs>

          {/* Orbit arcs */}
          <motion.ellipse
            cx={400}
            cy={260}
            rx={300}
            ry={140}
            fill="none"
            stroke={CYAN}
            strokeOpacity={0.35}
            strokeWidth={1}
            initial={reduced ? { pathLength: 1 } : { pathLength: 0 }}
            whileInView={{ pathLength: 1 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{ duration: reduced ? 0 : 1.2, ease: "easeOut" }}
          />
          <motion.ellipse
            cx={400}
            cy={260}
            rx={300}
            ry={195}
            fill="none"
            stroke={VIOLET}
            strokeOpacity={0.28}
            strokeWidth={1}
            initial={reduced ? { pathLength: 1 } : { pathLength: 0 }}
            whileInView={{ pathLength: 1 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{
              duration: reduced ? 0 : 1.2,
              delay: 0.15,
              ease: "easeOut",
            }}
          />
          <motion.ellipse
            cx={400}
            cy={260}
            rx={300}
            ry={250}
            fill="none"
            stroke={CORAL}
            strokeOpacity={0.22}
            strokeWidth={1}
            initial={reduced ? { pathLength: 1 } : { pathLength: 0 }}
            whileInView={{ pathLength: 1 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{
              duration: reduced ? 0 : 1.2,
              delay: 0.3,
              ease: "easeOut",
            }}
          />

          {/* Pulse path (used to sample points; rendered faintly as a guide). */}
          <path
            ref={pathRef}
            d={pulsePathD}
            fill="none"
            stroke="rgba(245,241,234,0.08)"
            strokeWidth={1}
            strokeDasharray="2 4"
          />

          {/* Nitro hub */}
          <circle
            cx={NITRO.cx}
            cy={NITRO.cy}
            r={90}
            fill="url(#v7-nitro-glow)"
          />
          <motion.circle
            cx={NITRO.cx}
            cy={NITRO.cy}
            r={44}
            fill="rgba(12,19,34,0.95)"
            stroke="url(#v7-spectrum)"
            strokeWidth={2}
            animate={
              reduced
                ? undefined
                : { scale: [1, 1.04, 1], opacity: [0.9, 1, 0.9] }
            }
            transition={{ duration: 4.5, repeat: Infinity, ease: "easeInOut" }}
            style={{ transformOrigin: `${NITRO.cx}px ${NITRO.cy}px` }}
          />
          <text
            x={NITRO.cx}
            y={NITRO.cy - 4}
            textAnchor="middle"
            fontSize={14}
            fontFamily="var(--font-heading), sans-serif"
            fontWeight={600}
            fill="#f5f0ea"
          >
            Nitro
          </text>
          <text
            x={NITRO.cx}
            y={NITRO.cy + 12}
            textAnchor="middle"
            fontSize={9}
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fill="#a1a3af"
          >
            control plane
          </text>

          {/* Surface nodes */}
          {SURFACES.map((s) => (
            <SurfaceNode
              key={s.id}
              surface={s}
              progress={pulse}
              pulseTotal={pathLen}
              pathRef={pathRef}
              reduced={reduced}
              onHover={setHovered}
            />
          ))}

          {/* Pulse dot */}
          <PulseDot progress={pulse} pathRef={pathRef} reduced={reduced} />
        </svg>

        {/* Hover label card */}
        {hovered ? (
          <div
            className="border-cc-card-border bg-cc-surface/95 pointer-events-none absolute right-6 bottom-6 max-w-xs rounded-lg border p-4 backdrop-blur"
            style={{ boxShadow: `0 0 0 1px ${ARCS[hovered.arc].color}33` }}
          >
            <div className="flex items-center gap-2">
              <span
                className="h-2 w-2 rounded-full"
                style={{ backgroundColor: ARCS[hovered.arc].color }}
                aria-hidden
              />
              <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
                {ARCS[hovered.arc].label}
              </span>
            </div>
            <p className="font-heading text-cc-heading mt-1 text-base font-semibold tracking-tight">
              {hovered.title}
            </p>
            <p className="text-cc-ink-dim mt-1 text-[0.82rem] leading-snug">
              {hovered.outcome}
            </p>
          </div>
        ) : null}
      </div>
      <p className="text-cc-ink-dim px-6 pt-2 pb-8 text-center text-[0.85rem] md:px-10 md:pb-10">
        Scroll to advance the pulse. Hover any node to inspect a surface.
      </p>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Arc rows                                                                  */
/* -------------------------------------------------------------------------- */

interface ArcCard {
  readonly id: string;
  readonly title: string;
  readonly outcome: string;
  readonly href: string;
  readonly proofs: readonly string[];
}

const BUILD_CARDS: readonly ArcCard[] = [
  {
    id: "build",
    title: "Build",
    outcome: "Ship from the code that runs it.",
    href: "/platform/build",
    proofs: [
      "Implementation-first GraphQL in C#",
      "Schema, resolvers, DataLoaders from one class",
      "Typed .NET clients out of the same source",
    ],
  },
  {
    id: "agentic-coding",
    title: "Agentic Coding",
    outcome: "Give coding agents a feedback loop.",
    href: "/platform/agentic-coding",
    proofs: [
      "Typed contracts agents can read",
      "Diff and lint signal on every change",
      "Same loop a senior reviewer would run",
    ],
  },
];

const RUN_CARDS: readonly ArcCard[] = [
  {
    id: "observability",
    title: "Observability",
    outcome: "See what the API is doing, right now.",
    href: "/platform/observability",
    proofs: [
      "Operation-level traces and timings",
      "Field hot paths and N+1 detection",
      "OpenTelemetry export to your stack",
    ],
  },
  {
    id: "workflows",
    title: "Workflows",
    outcome: "Let work continue after the request.",
    href: "/platform/workflows",
    proofs: [
      "Durable steps with retries",
      "Background jobs in the same model",
      "Resumable on cold start",
    ],
  },
  {
    id: "analytics",
    title: "Analytics",
    outcome: "Know which fields earn their keep.",
    href: "/platform/analytics",
    proofs: [
      "Field-level usage over time",
      "Per-client adoption per type",
      "Spot dead fields before you cut",
    ],
  },
];

const EVOLVE_CARDS: readonly ArcCard[] = [
  {
    id: "release-safety",
    title: "Release Safety",
    outcome: "Change contracts with a safety net.",
    href: "/platform/release-safety",
    proofs: [
      "Schema diff against published clients",
      "Breaking change flagged before merge",
      "Block, warn, or allow per rule",
    ],
  },
  {
    id: "continuous-integration",
    title: "Continuous Integration",
    outcome: "Innovate with confidence at merge time.",
    href: "/platform/continuous-integration",
    proofs: [
      "Schema check on every pull request",
      "Composition validation across services",
      "Annotated diffs in code review",
    ],
  },
  {
    id: "ecosystem",
    title: "Ecosystem",
    outcome: "An ecosystem you can trust and reuse.",
    href: "/platform/ecosystem",
    proofs: [
      "Banana Cake Pop IDE",
      "Strawberry Shake typed clients",
      "Green Donut DataLoaders",
    ],
  },
];

interface ArcRowProps {
  readonly arc: ArcKey;
  readonly cards: readonly ArcCard[];
  readonly index: number;
  readonly indicator: (color: string) => ReactNode;
}

function ArcRow({ arc, cards, index, indicator }: ArcRowProps) {
  const theme = ARCS[arc];
  return (
    <section className="flex flex-col gap-7">
      <div className="flex items-end justify-between gap-6">
        <div>
          <Eyebrow>
            Arc {String(index).padStart(2, "0")} · {theme.label}
          </Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading mt-2 font-semibold tracking-tight">
            {theme.intent}
          </h2>
        </div>
        <span
          className="hidden h-px w-32 shrink-0 md:block"
          style={{
            background: `linear-gradient(90deg, ${theme.color}, transparent)`,
          }}
          aria-hidden
        />
      </div>
      <div className="grid auto-rows-fr gap-5 md:grid-cols-3">
        {cards.map((card, i) => (
          <motion.div
            key={card.id}
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{ duration: 0.5, delay: i * 0.08, ease: "easeOut" }}
          >
            <Link
              href={card.href}
              className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col gap-4 rounded-xl border p-5 no-underline backdrop-blur-sm transition-colors"
            >
              <span
                className="absolute inset-x-5 top-0 h-px opacity-60"
                style={{
                  background: `linear-gradient(90deg, transparent, ${theme.color}, transparent)`,
                }}
                aria-hidden
              />
              <div className="flex items-center justify-between">
                <Eyebrow>{theme.label}</Eyebrow>
                <span
                  className="font-mono text-[0.6rem] tracking-tight"
                  style={{ color: theme.color }}
                >
                  {card.href.split("/").pop()}
                </span>
              </div>
              <h3 className="font-heading text-h5 text-cc-heading group-hover:text-cc-accent font-semibold tracking-tight transition-colors">
                {card.title}
              </h3>
              <p className="text-cc-ink text-[0.95rem] leading-relaxed">
                {card.outcome}
              </p>
              {indicator(theme.color)}
              <ul className="mt-1 flex flex-col gap-1.5">
                {card.proofs.map((proof) => (
                  <li
                    key={proof}
                    className="text-cc-ink-dim flex items-start gap-2 text-[0.82rem] leading-snug"
                  >
                    <span
                      className="mt-1 flex h-3 w-3 shrink-0 items-center justify-center"
                      style={{ color: theme.color }}
                    >
                      <CheckIcon size={12} />
                    </span>
                    <span>{proof}</span>
                  </li>
                ))}
              </ul>
              <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
                Open {card.title} →
              </span>
            </Link>
          </motion.div>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Per-arc indicator visuals                                                 */
/* -------------------------------------------------------------------------- */

function SparklineIndicator({ color }: { readonly color: string }) {
  return (
    <div
      className="border-cc-card-border bg-cc-surface/60 relative h-16 overflow-hidden rounded-lg border"
      style={{ boxShadow: `inset 0 0 0 1px ${color}14` }}
    >
      <svg viewBox="0 0 280 64" className="h-full w-full" aria-hidden>
        <motion.path
          d="M6 50 L40 38 L70 44 L100 22 L140 30 L180 14 L220 26 L274 10"
          fill="none"
          stroke={color}
          strokeWidth={2}
          strokeLinecap="round"
          initial={{ pathLength: 0, opacity: 0.3 }}
          whileInView={{ pathLength: 1, opacity: 1 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 1.1, ease: "easeOut" }}
        />
        <motion.path
          d="M6 56 L40 52 L70 48 L100 50 L140 44 L180 46 L220 38 L274 36"
          fill="none"
          stroke="var(--color-cc-ink)"
          strokeOpacity={0.35}
          strokeWidth={1.5}
          strokeLinecap="round"
          initial={{ pathLength: 0 }}
          whileInView={{ pathLength: 1 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 1.1, delay: 0.1, ease: "easeOut" }}
        />
      </svg>
    </div>
  );
}

function WaterfallIndicator({ color }: { readonly color: string }) {
  const bars = [
    { x: 4, w: 90, y: 8 },
    { x: 30, w: 140, y: 24 },
    { x: 70, w: 80, y: 40 },
    { x: 110, w: 160, y: 56 },
  ];
  return (
    <div
      className="border-cc-card-border bg-cc-surface/60 relative h-16 overflow-hidden rounded-lg border"
      style={{ boxShadow: `inset 0 0 0 1px ${color}14` }}
    >
      <svg viewBox="0 0 280 72" className="h-full w-full" aria-hidden>
        {bars.map((b, i) => (
          <motion.rect
            key={i}
            x={b.x}
            y={b.y}
            height={6}
            rx={3}
            fill={i === 1 ? color : "var(--color-cc-ink)"}
            opacity={i === 1 ? 1 : 0.4}
            initial={{ width: 0 }}
            whileInView={{ width: b.w }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{
              duration: 0.7,
              delay: i * 0.12,
              ease: "easeOut",
            }}
          />
        ))}
      </svg>
    </div>
  );
}

function SchemaDiffIndicator({ color }: { readonly color: string }) {
  return (
    <div
      className="border-cc-card-border bg-cc-surface/60 relative h-16 overflow-hidden rounded-lg border"
      style={{ boxShadow: `inset 0 0 0 1px ${color}14` }}
    >
      <svg viewBox="0 0 280 64" className="h-full w-full" aria-hidden>
        <motion.path
          d="M6 18 L274 18"
          stroke={color}
          strokeWidth={1.5}
          strokeLinecap="round"
          initial={{ pathLength: 0 }}
          whileInView={{ pathLength: 1 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 0.7, ease: "easeOut" }}
        />
        <motion.path
          d="M6 46 L274 46"
          stroke="var(--color-cc-ink)"
          strokeWidth={1.5}
          strokeOpacity={0.45}
          strokeLinecap="round"
          strokeDasharray="3 4"
          initial={{ pathLength: 0 }}
          whileInView={{ pathLength: 1 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 0.7, delay: 0.15, ease: "easeOut" }}
        />
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize={9}
        >
          <text x={12} y={14} fill={color}>
            + field discount: Money
          </text>
          <text x={12} y={42} fill="var(--color-cc-ink)" opacity={0.7}>
            - field legacyId: String
          </text>
        </g>
      </svg>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Nitro band                                                                */
/* -------------------------------------------------------------------------- */

function NitroMiniHub() {
  const reduced = useReducedMotion() ?? false;
  return (
    <div className="relative flex h-44 w-44 shrink-0 items-center justify-center md:h-56 md:w-56">
      <svg viewBox="0 0 200 200" className="h-full w-full" aria-hidden>
        <defs>
          <radialGradient id="v7-mini-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CYAN} stopOpacity="0.5" />
            <stop offset="60%" stopColor={CYAN} stopOpacity="0.1" />
            <stop offset="100%" stopColor={CYAN} stopOpacity="0" />
          </radialGradient>
        </defs>
        <circle cx={100} cy={100} r={80} fill="url(#v7-mini-glow)" />
        {[
          { r: 56, c: CYAN },
          { r: 72, c: VIOLET },
          { r: 88, c: CORAL },
        ].map((o) => (
          <circle
            key={o.r}
            cx={100}
            cy={100}
            r={o.r}
            fill="none"
            stroke={o.c}
            strokeOpacity={0.3}
            strokeDasharray="2 4"
          />
        ))}
        <motion.circle
          cx={100}
          cy={100}
          r={28}
          fill="rgba(12,19,34,0.95)"
          stroke={CYAN}
          strokeWidth={2}
          animate={
            reduced
              ? undefined
              : { scale: [1, 1.05, 1], opacity: [0.9, 1, 0.9] }
          }
          transition={{ duration: 4.5, repeat: Infinity, ease: "easeInOut" }}
          style={{ transformOrigin: "100px 100px" }}
        />
        <text
          x={100}
          y={104}
          textAnchor="middle"
          fontSize={14}
          fontFamily="var(--font-heading), sans-serif"
          fontWeight={600}
          fill="#f5f0ea"
        >
          Nitro
        </text>
      </svg>
    </div>
  );
}

function NitroBand() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 md:p-10">
      <span
        className="pointer-events-none absolute -top-32 -right-32 h-72 w-72 rounded-full opacity-30 blur-3xl"
        style={{ background: CYAN }}
        aria-hidden
      />
      <div className="relative grid gap-8 md:grid-cols-[1fr_auto] md:items-center">
        <div className="flex flex-col gap-4">
          <Eyebrow>Nitro · the control plane</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading max-w-2xl font-semibold tracking-tight">
            The hub the pulse keeps returning to.
          </h2>
          <p className="text-cc-ink max-w-2xl leading-relaxed">
            Nitro is where the eight surfaces meet. Schema registry, release
            checks, analytics, and traces share one home. Connect a service,
            ship a change, and Nitro keeps the rest of the platform in sync.
          </p>
          <ul className="text-cc-ink-dim mt-1 flex flex-col gap-1.5">
            {[
              "Schema registry for every environment",
              "Release checks against published clients",
              "Field usage and traces in one timeline",
            ].map((line) => (
              <li
                key={line}
                className="flex items-start gap-2 text-[0.88rem] leading-snug"
              >
                <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                  <CheckIcon size={12} />
                </span>
                <span>{line}</span>
              </li>
            ))}
          </ul>
          <div className="mt-2 flex flex-wrap items-center gap-3">
            <SolidButton href="/products/nitro">About Nitro</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Open Nitro
            </OutlineButton>
          </div>
        </div>
        <NitroMiniHub />
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Outcomes counters                                                         */
/* -------------------------------------------------------------------------- */

interface CounterProps {
  readonly value: number;
  readonly prefix?: string;
  readonly suffix?: string;
  readonly decimals?: number;
}

function Counter({
  value,
  prefix = "",
  suffix = "",
  decimals = 0,
}: CounterProps) {
  const reduced = useReducedMotion() ?? false;
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, margin: "-10% 0px" });
  const [display, setDisplay] = useState(reduced ? value : 0);

  useEffect(() => {
    if (!inView || reduced) return;
    const controls = animate(0, value, {
      duration: 1.6,
      ease: "easeOut",
      onUpdate: (v) => setDisplay(v),
    });
    return () => controls.stop();
  }, [inView, value, reduced]);

  const text = `${prefix}${display.toFixed(decimals)}${suffix}`;
  return (
    <span ref={ref} className="tabular-nums">
      {text}
    </span>
  );
}

function OutcomesStrip() {
  const cards = [
    {
      label: "Surfaces",
      value: 8,
      suffix: "",
      title: "Eight surfaces, one circuit.",
      caption:
        "Build, Agentic Coding, Observability, Workflows, Analytics, Release Safety, Continuous Integration, and Ecosystem all share one platform.",
      color: CYAN,
    },
    {
      label: "Arcs",
      value: 3,
      suffix: "",
      title: "Three arcs, one journey.",
      caption:
        "Build, Run, and Evolve are the three arcs the request travels, with Nitro at the hub of each turn.",
      color: VIOLET,
    },
    {
      label: "Control plane",
      value: 1,
      suffix: "",
      title: "One Nitro to tie it together.",
      caption:
        "Schema registry, release checks, analytics, and traces share one home, so every surface stays in sync.",
      color: CORAL,
    },
  ];
  return (
    <section className="flex flex-col gap-7">
      <div>
        <Eyebrow>Outcomes</Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading mt-2 max-w-3xl font-semibold tracking-tight">
          What the circuit moves.
        </h2>
      </div>
      <div className="grid gap-5 md:grid-cols-3">
        {cards.map((c) => (
          <div
            key={c.label}
            className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6"
          >
            <span
              className="absolute inset-x-5 top-0 h-px opacity-60"
              style={{
                background: `linear-gradient(90deg, transparent, ${c.color}, transparent)`,
              }}
              aria-hidden
            />
            <Eyebrow>{c.label}</Eyebrow>
            <p
              className="font-heading mt-3 text-[3rem] leading-none font-semibold"
              style={{ color: c.color }}
            >
              <Counter value={c.value} suffix={c.suffix} />
            </p>
            <p className="font-heading text-cc-heading mt-3 text-[1.05rem] font-semibold tracking-tight">
              {c.title}
            </p>
            <p className="text-cc-ink-dim mt-2 text-[0.88rem] leading-snug">
              {c.caption}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                               */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <motion.section
      className="flex flex-col items-center gap-6 py-6 text-center"
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10% 0px" }}
      transition={{ duration: 0.6, ease: "easeOut" }}
    >
      <Eyebrow>One circuit, eight surfaces</Eyebrow>
      <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
        Start with one surface, grow into the platform.
      </h2>
      <p className="text-cc-ink-dim max-w-2xl text-[0.95rem] leading-relaxed">
        Open the surface closest to today&apos;s problem. The rest of the
        platform folds in as you need it, with Nitro keeping the circuit live.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/products/nitro">Explore Nitro</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </motion.section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Client root                                                               */
/* -------------------------------------------------------------------------- */

export function PlatformLiveCircuitClient() {
  return (
    <MotionConfig reducedMotion="user">
      <div className="flex flex-col gap-20 py-6">
        <Hero />
        <PlatformCircuit />
        <ArcRow
          arc="build"
          cards={BUILD_CARDS}
          index={1}
          indicator={(color) => <SparklineIndicator color={color} />}
        />
        <ArcRow
          arc="run"
          cards={RUN_CARDS}
          index={2}
          indicator={(color) => <WaterfallIndicator color={color} />}
        />
        <ArcRow
          arc="evolve"
          cards={EVOLVE_CARDS}
          index={3}
          indicator={(color) => <SchemaDiffIndicator color={color} />}
        />
        <NitroBand />
        <OutcomesStrip />
        <ClosingCta />
      </div>
    </MotionConfig>
  );
}
