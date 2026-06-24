"use client";

import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
} from "motion/react";
import { useEffect, useRef, useState } from "react";
import type { CSSProperties } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Station colours pulled from the brand spectrum (cyan, violet, coral) and the
// page accent (teal). Each station gets one swatch so the diagram reads as the
// same site, with one topical animation in the hero.
const STATIONS = [
  {
    id: "client",
    label: "Strawberry Shake",
    role: "Client",
    cx: 90,
    cy: 200,
    color: "#16b9e4",
    detail: "Typed .NET client. MSBuild generated.",
  },
  {
    id: "gateway",
    label: "Fusion",
    role: "Gateway",
    cx: 340,
    cy: 200,
    color: "#7c92c6",
    detail: "Compose subgraphs at planning time.",
  },
  {
    id: "server",
    label: "Hot Chocolate",
    role: "Server",
    cx: 590,
    cy: 200,
    color: "#5eead4",
    detail: "Source-generated GraphQL server.",
  },
  {
    id: "nitro",
    label: "Nitro",
    role: "Control Plane",
    cx: 820,
    cy: 200,
    color: "#f0786a",
    detail: "Registry, CI, observability, IDE.",
  },
] as const;

type StationId = (typeof STATIONS)[number]["id"];

interface StationModel {
  readonly id: StationId;
  readonly label: string;
  readonly role: string;
  readonly cx: number;
  readonly cy: number;
  readonly color: string;
  readonly detail: string;
}

const PULSE_CYCLE_MS = 6000;
const PULSE_CYCLE_S = PULSE_CYCLE_MS / 1000;

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <PulseLegend />
      <LogoBand />
      <PlatformPillars />
      <Outcomes />
      <BuildObserveEvolve />
      <ProofQuote />
      <ClosingCta />
    </MotionConfig>
  );
}

function Hero() {
  return (
    <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="grid gap-12 lg:grid-cols-[1fr_1.15fr] lg:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
            Platform Pulse
          </p>
          <h1 className="font-heading text-cc-heading sm:text-h2 lg:text-h1 mt-6 max-w-2xl text-4xl leading-[1.05] font-semibold tracking-[-0.02em] text-balance">
            The GraphQL platform for .NET.
          </h1>
          <p className="text-cc-ink mt-7 max-w-xl text-base text-pretty sm:text-lg">
            One request, one platform. Strawberry Shake sends, Fusion plans, Hot
            Chocolate answers, Nitro watches. End to end on .NET, open source,
            MIT licensed.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
          <p className="text-cc-nav-label mt-5 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
            Self-host or run on Nitro Cloud.
          </p>
        </div>
        <PlatformPulse />
      </div>
    </section>
  );
}

function PlatformPulse() {
  const containerRef = useRef<HTMLDivElement>(null);
  const inView = useInView(containerRef, { margin: "-10%", once: false });
  const reduced = useReducedMotion();
  const [tick, setTick] = useState(0);

  // Drive the pulse loop only while in view and motion is allowed.
  useEffect(() => {
    if (reduced || !inView) {
      return;
    }
    const id = window.setInterval(() => {
      setTick((t) => t + 1);
    }, PULSE_CYCLE_MS);
    return () => window.clearInterval(id);
  }, [inView, reduced]);

  const animating = inView && !reduced;
  const cycleKey = animating ? tick : "static";

  return (
    <div
      ref={containerRef}
      className="border-cc-card-border bg-cc-card-bg/60 relative overflow-hidden rounded-2xl border p-5 shadow-[0_30px_80px_-40px_rgba(94,234,212,0.35)] sm:p-7"
      aria-label="Platform Pulse diagram"
    >
      <div className="text-cc-nav-label mb-4 flex items-center justify-between font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        <span>request loop</span>
        <span className="text-cc-accent">live</span>
      </div>
      <div className="relative aspect-[9/6] w-full">
        <svg
          viewBox="0 0 920 400"
          className="absolute inset-0 h-full w-full"
          role="img"
          aria-hidden="true"
        >
          {/* Wires: client to gateway, gateway fans into two subgraph paths then merges at server, server to nitro */}
          <Wire d="M 90 200 L 340 200" />
          <Wire d="M 340 200 C 420 200, 460 130, 540 130 L 590 200" />
          <Wire d="M 340 200 C 420 200, 460 270, 540 270 L 590 200" />
          <Wire d="M 590 200 L 820 200" />
          {/* Telemetry side-branch from server down to Nitro */}
          <Wire d="M 590 200 C 660 200, 700 320, 820 320" faint />

          {STATIONS.map((s) => (
            <Station
              key={s.id}
              station={s}
              activeAt={stationActivationFor(s.id)}
              animating={animating}
              cycleKey={cycleKey}
            />
          ))}

          {animating && (
            <PulseDot
              key={`req-${cycleKey}`}
              path="M 90 200 L 340 200 C 420 200, 460 130, 540 130 L 590 200"
              color="#5eead4"
              duration={2.4}
              delay={0}
            />
          )}
          {animating && (
            <PulseDot
              key={`res-${cycleKey}`}
              path="M 590 200 C 540 130, 460 130, 340 200 L 90 200"
              color="#16b9e4"
              duration={2.4}
              delay={2.6}
            />
          )}
          {animating && (
            <PulseDot
              key={`tel-${cycleKey}`}
              path="M 590 200 C 660 200, 700 320, 820 320"
              color="#f0786a"
              duration={1.8}
              delay={1.6}
            />
          )}
        </svg>
      </div>
      <TraceWaterfall cycleKey={cycleKey} animating={animating} />
    </div>
  );
}

function Wire({
  d,
  faint = false,
}: {
  readonly d: string;
  readonly faint?: boolean;
}) {
  return (
    <path
      d={d}
      fill="none"
      stroke={faint ? "rgba(245,241,234,0.10)" : "rgba(245,241,234,0.18)"}
      strokeWidth={faint ? 1.2 : 1.6}
      strokeDasharray={faint ? "4 6" : undefined}
    />
  );
}

function Station({
  station,
  activeAt,
  animating,
  cycleKey,
}: {
  readonly station: StationModel;
  readonly activeAt: { readonly start: number; readonly end: number };
  readonly animating: boolean;
  readonly cycleKey: number | string;
}) {
  const { cx, cy, color, label, role } = station;
  const startT = clamp01(activeAt.start / PULSE_CYCLE_S);
  const peakT = clamp01((activeAt.start + 0.2) / PULSE_CYCLE_S);
  const endT = clamp01(activeAt.end / PULSE_CYCLE_S);
  const times = [0, startT, peakT, endT, 1];

  return (
    <g>
      {animating && (
        <motion.circle
          key={`glow-${cycleKey}-${station.id}`}
          cx={cx}
          cy={cy}
          r={42}
          fill={color}
          initial={{ fillOpacity: 0 }}
          animate={{ fillOpacity: [0, 0, 0.35, 0, 0] }}
          transition={{
            duration: PULSE_CYCLE_S,
            times,
            ease: "easeInOut",
          }}
          style={{ filter: "blur(8px)" }}
        />
      )}
      {/* Fill mirrors --color-cc-bg from app/globals.css so the node sits flat on the page background. */}
      <circle
        cx={cx}
        cy={cy}
        r={26}
        fill="var(--color-cc-bg)"
        stroke={color}
        strokeWidth={1.6}
      />
      <circle cx={cx} cy={cy} r={6} fill={color} />
      <text
        x={cx}
        y={cy + 56}
        textAnchor="middle"
        fill="#f5f0ea"
        fontFamily="var(--font-heading)"
        fontSize="15"
        fontWeight={600}
      >
        {label}
      </text>
      <text
        x={cx}
        y={cy + 74}
        textAnchor="middle"
        fill="rgba(245,241,234,0.55)"
        fontFamily="var(--font-mono)"
        fontSize="10"
        letterSpacing="1.5"
      >
        {role.toUpperCase()}
      </text>
    </g>
  );
}

function clamp01(v: number) {
  if (v < 0) return 0;
  if (v > 1) return 1;
  return v;
}

// Seconds into a cycle when each station lights up.
function stationActivationFor(id: StationId) {
  switch (id) {
    case "client":
      return { start: 0.0, end: 0.6 };
    case "gateway":
      return { start: 0.8, end: 1.6 };
    case "server":
      return { start: 1.8, end: 2.6 };
    case "nitro":
      return { start: 3.0, end: 4.0 };
  }
}

function PulseDot({
  path,
  color,
  duration,
  delay,
}: {
  readonly path: string;
  readonly color: string;
  readonly duration: number;
  readonly delay: number;
}) {
  const style: CSSProperties = {
    offsetPath: `path('${path}')`,
    offsetRotate: "0deg",
    filter: `drop-shadow(0 0 8px ${color})`,
  };
  return (
    <motion.circle
      r={5}
      fill={color}
      initial={{ offsetDistance: "0%", opacity: 0 }}
      animate={{ offsetDistance: ["0%", "100%"], opacity: [0, 1, 1, 0] }}
      transition={{
        duration,
        delay,
        ease: "easeInOut",
        times: [0, 0.1, 0.9, 1],
      }}
      style={style}
    />
  );
}

function TraceWaterfall({
  cycleKey,
  animating,
}: {
  readonly cycleKey: number | string;
  readonly animating: boolean;
}) {
  const rows = [
    { label: "query checkout", color: "#5eead4", offset: 0, width: 92 },
    { label: "fusion.plan", color: "#7c92c6", offset: 6, width: 18 },
    { label: "cart.resolve", color: "#16b9e4", offset: 24, width: 28 },
    { label: "pricing.resolve", color: "#16b9e4", offset: 30, width: 34 },
    { label: "inventory.resolve", color: "#f0786a", offset: 52, width: 26 },
    { label: "merge", color: "#7c92c6", offset: 78, width: 12 },
  ];
  return (
    <div className="border-cc-card-border bg-cc-bg/40 mt-5 rounded-xl border p-4">
      <div className="text-cc-nav-label mb-3 flex items-center justify-between font-mono text-[0.62rem] tracking-[0.18em] uppercase">
        <span>nitro trace / checkout</span>
        <P95Number animating={animating} cycleKey={cycleKey} />
      </div>
      <div className="flex flex-col gap-1.5">
        {rows.map((row, i) => (
          <div
            key={row.label}
            className="grid grid-cols-[110px_1fr] items-center gap-3"
          >
            <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-[0.04em]">
              {row.label}
            </span>
            <div className="bg-cc-bg/60 relative h-2 rounded-full">
              {animating ? (
                <motion.div
                  key={`row-${cycleKey}-${i}`}
                  className="absolute top-0 h-2 rounded-full"
                  style={{
                    left: `${row.offset}%`,
                    backgroundColor: row.color,
                    boxShadow: `0 0 8px ${row.color}55`,
                  }}
                  initial={{ width: 0, opacity: 0 }}
                  animate={{ width: `${row.width}%`, opacity: 1 }}
                  transition={{
                    duration: 0.35,
                    delay: 1.6 + i * 0.12,
                    ease: "easeOut",
                  }}
                />
              ) : (
                <span
                  className="absolute top-0 h-2 rounded-full"
                  style={{
                    left: `${row.offset}%`,
                    width: `${row.width}%`,
                    backgroundColor: row.color,
                    boxShadow: `0 0 8px ${row.color}55`,
                  }}
                />
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function P95Number({
  animating,
  cycleKey,
}: {
  readonly animating: boolean;
  readonly cycleKey: number | string;
}) {
  const value = useMotionValue(0);
  const [display, setDisplay] = useState(0);

  useEffect(() => {
    if (!animating) {
      return;
    }
    value.set(0);
    const unsub = value.on("change", (v) => setDisplay(Math.round(v)));
    const controls = animate(value, 142, {
      duration: 1.8,
      delay: 2.0,
      ease: "easeOut",
    });
    return () => {
      unsub();
      controls.stop();
    };
    // Re-run on every cycle tick (cycleKey) so the count restarts on each loop.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [animating, cycleKey]);

  const shown = animating ? display : 142;

  return (
    <span className="text-cc-accent font-mono text-[0.65rem] tracking-[0.16em]">
      p95 {shown}ms
    </span>
  );
}

function PulseLegend() {
  return (
    <section aria-label="Platform Pulse legend" className="pb-12">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {STATIONS.map((s) => (
          <div
            key={s.id}
            className="border-cc-card-border bg-cc-card-bg/50 flex items-start gap-3 rounded-xl border p-4"
          >
            <span
              aria-hidden
              className="mt-1.5 inline-block h-2.5 w-2.5 flex-none rounded-full"
              style={{
                backgroundColor: s.color,
                boxShadow: `0 0 10px ${s.color}88`,
              }}
            />
            <div>
              <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                {s.role}
              </p>
              <p className="font-heading text-cc-heading mt-1 text-sm font-semibold">
                {s.label}
              </p>
              <p className="text-cc-ink-dim mt-1 text-xs">{s.detail}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function LogoBand() {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-15%" }}
      transition={{ duration: 0.5, ease: "easeOut" }}
      className="border-cc-card-border border-y"
    >
      <LogoCloud />
    </motion.div>
  );
}

interface Pillar {
  readonly name: string;
  readonly tagline: string;
  readonly description: string;
  readonly color: string;
  readonly href: string;
  readonly external?: boolean;
}

const PILLARS: readonly Pillar[] = [
  {
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The source-generated GraphQL server. Schema-first or code-first, built on ASP.NET Core, with the modern GraphQL spec.",
    color: "#5eead4",
    href: "/products/hotchocolate",
  },
  {
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "MSBuild code generation turns each query into a fully typed C# API, so apps stay in sync with the schema.",
    color: "#16b9e4",
    href: "/products/strawberryshake",
  },
  {
    name: "Fusion",
    tagline: "Composition for many subgraphs",
    description:
      "Compose multiple subgraphs at planning time, then run the gateway in your own ASP.NET Core process.",
    color: "#7c92c6",
    href: "/products/fusion",
  },
  {
    name: "Nitro",
    tagline: "Control plane and IDE",
    description:
      "Schema registry, client registry, CI checks, observability, and the GraphQL IDE your team already uses.",
    color: "#f0786a",
    href: "https://nitro.chillicream.com",
    external: true,
  },
];

function PlatformPillars() {
  return (
    <section aria-labelledby="pillars-heading" className="py-20 sm:py-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          The four stations
        </p>
        <h2
          id="pillars-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Each station, on its own.
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
          The pulse only works because every product was designed to talk to the
          next. Pull any one out and it still stands.
        </p>
      </div>
      <div className="mt-12 grid gap-5 md:grid-cols-2">
        {PILLARS.map((p, i) => (
          <motion.article
            key={p.name}
            initial={{ opacity: 0, y: 18 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-15%" }}
            transition={{ duration: 0.45, delay: i * 0.08, ease: "easeOut" }}
            className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover flex h-full flex-col rounded-2xl border p-6 transition-colors"
          >
            <div className="flex items-center gap-3">
              <span
                aria-hidden
                className="inline-block h-3 w-3 flex-none rounded-full"
                style={{
                  backgroundColor: p.color,
                  boxShadow: `0 0 12px ${p.color}88`,
                }}
              />
              <h3 className="font-heading text-cc-heading text-lg font-semibold">
                {p.name}
              </h3>
            </div>
            <p className="text-cc-nav-label mt-1 font-mono text-[0.7rem] tracking-[0.16em] uppercase">
              {p.tagline}
            </p>
            <p className="text-cc-ink mt-4 flex-1 text-sm leading-relaxed">
              {p.description}
            </p>
            <a
              href={p.href}
              {...(p.external
                ? { target: "_blank", rel: "noopener" }
                : undefined)}
              className="text-cc-accent hover:text-cc-heading mt-5 inline-flex items-center gap-1 font-mono text-xs tracking-[0.12em] uppercase transition-colors"
            >
              {p.name} -&gt;
            </a>
          </motion.article>
        ))}
      </div>
    </section>
  );
}

interface NumericKpi {
  readonly kind: "numeric";
  readonly label: string;
  readonly target: number;
  readonly suffix: string;
  readonly note: string;
}

interface CapabilityKpi {
  readonly kind: "capability";
  readonly label: string;
  readonly value: string;
  readonly note: string;
}

type Kpi = NumericKpi | CapabilityKpi;

const KPIS: readonly Kpi[] = [
  {
    kind: "numeric",
    label: "p95 latency",
    target: 142,
    suffix: "ms",
    note: "operation, observed",
  },
  {
    kind: "capability",
    label: "breaking-change diff",
    value: "on every PR",
    note: "from the registry",
  },
  {
    kind: "capability",
    label: "first query",
    value: "AddGraphQLServer()",
    note: "from new project",
  },
];

function Outcomes() {
  return (
    <section aria-labelledby="outcomes-heading" className="py-12">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Outcomes
        </p>
        <h2
          id="outcomes-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          What the loop earns you.
        </h2>
      </div>
      <div className="mt-10 grid gap-4 sm:grid-cols-3">
        {KPIS.map((k) => (
          <KpiTile key={k.label} kpi={k} />
        ))}
      </div>
    </section>
  );
}

function KpiTile({ kpi }: { readonly kpi: Kpi }) {
  if (kpi.kind === "numeric") {
    return <NumericKpiTile kpi={kpi} />;
  }
  return <CapabilityKpiTile kpi={kpi} />;
}

function NumericKpiTile({ kpi }: { readonly kpi: NumericKpi }) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-20%" });
  const reduced = useReducedMotion();
  const value = useMotionValue(0);
  const [display, setDisplay] = useState(0);

  useEffect(() => {
    if (reduced || !inView) {
      return;
    }
    const unsub = value.on("change", (v) => setDisplay(Math.round(v)));
    const controls = animate(value, kpi.target, {
      duration: 1.4,
      ease: "easeOut",
    });
    return () => {
      unsub();
      controls.stop();
    };
  }, [inView, reduced, value, kpi.target]);

  const shown = reduced ? kpi.target : display;

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6"
    >
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {kpi.label}
      </p>
      <p className="font-heading text-cc-heading mt-3 text-3xl font-semibold">
        {shown}
        <span className="text-cc-accent">{kpi.suffix}</span>
      </p>
      <p className="text-cc-ink-dim mt-2 font-mono text-[0.7rem]">{kpi.note}</p>
    </div>
  );
}

function CapabilityKpiTile({ kpi }: { readonly kpi: CapabilityKpi }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-20%" }}
      transition={{ duration: 0.45, ease: "easeOut" }}
      className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6"
    >
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {kpi.label}
      </p>
      <p className="font-heading text-cc-heading mt-3 text-xl font-semibold break-words sm:text-2xl">
        {kpi.value}
      </p>
      <p className="text-cc-ink-dim mt-2 font-mono text-[0.7rem]">{kpi.note}</p>
    </motion.div>
  );
}

interface LoopStep {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const LOOP_STEPS: readonly LoopStep[] = [
  {
    eyebrow: "Build",
    title: "Hot Chocolate plus Strawberry Shake",
    body: "Source-generated server, MSBuild-generated typed client. The schema you ship is the schema both sides agree on.",
    bullets: [
      "Schema-first or code-first authoring",
      "MSBuild code generation for the client",
    ],
  },
  {
    eyebrow: "Observe",
    title: "Nitro telemetry",
    body: "OpenTelemetry traces, per-operation p95, per-client tracking. Configured once in the server, surfaced in Nitro.",
    bullets: [
      "Per-operation p95, p99, throughput",
      "Per-client tracking and version drift",
    ],
  },
  {
    eyebrow: "Evolve",
    title: "Schema registry and CI",
    body: "Registry knows every published schema and operation. Nitro CI tells you which published clients a change affects.",
    bullets: [
      "Breaking change classification, safe to breaking",
      "Stage promotion with approval gates",
    ],
  },
];

function BuildObserveEvolve() {
  return (
    <section aria-labelledby="loop-heading" className="py-20 sm:py-24">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          The loop
        </p>
        <h2
          id="loop-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Build. Observe. Evolve.
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
          The platform is a loop, not a stack. The same telemetry that powers
          the dashboard powers the schema checks.
        </p>
      </div>
      <div className="relative mt-14">
        <div className="absolute top-7 right-8 left-8 hidden md:block">
          <svg
            viewBox="0 0 800 4"
            preserveAspectRatio="none"
            className="h-1 w-full"
            aria-hidden="true"
          >
            <motion.line
              x1={0}
              y1={2}
              x2={800}
              y2={2}
              stroke="#5eead4"
              strokeOpacity={0.55}
              strokeWidth={1.6}
              initial={{ pathLength: 0 }}
              whileInView={{ pathLength: 1 }}
              viewport={{ once: true, margin: "-20%" }}
              transition={{ duration: 1.2, ease: "easeOut" }}
            />
          </svg>
        </div>
        <div className="relative grid gap-6 md:grid-cols-3">
          {LOOP_STEPS.map((step, i) => (
            <motion.div
              key={step.eyebrow}
              initial={{ opacity: 0, y: 18 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, margin: "-15%" }}
              transition={{
                duration: 0.45,
                delay: 0.2 + i * 0.15,
                ease: "easeOut",
              }}
              className="border-cc-card-border bg-cc-card-bg/70 relative rounded-2xl border p-6"
            >
              <span className="bg-cc-bg border-cc-accent/40 text-cc-accent relative z-10 mb-4 inline-flex h-14 w-14 items-center justify-center rounded-full border font-mono text-sm">
                0{i + 1}
              </span>
              <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                {step.eyebrow}
              </p>
              <h3 className="font-heading text-cc-heading mt-2 text-lg font-semibold">
                {step.title}
              </h3>
              <p className="text-cc-ink mt-3 text-sm leading-relaxed">
                {step.body}
              </p>
              <ul className="mt-4 flex flex-col gap-2">
                {step.bullets.map((b) => (
                  <li key={b} className="flex items-start gap-2">
                    <span className="text-cc-accent mt-[3px] flex-none">
                      <CheckIcon />
                    </span>
                    <span className="text-cc-ink text-xs">{b}</span>
                  </li>
                ))}
              </ul>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}

function ProofQuote() {
  return (
    <section aria-labelledby="proof-heading" className="py-12">
      <h2 id="proof-heading" className="sr-only">
        Open source
      </h2>
      <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-3xl rounded-3xl border p-8 text-center sm:p-12">
        <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.2em] uppercase">
          Open source
        </p>
        <p className="text-cc-heading font-heading mt-4 text-xl leading-snug sm:text-2xl">
          MIT licensed across every package. Built in the open on GitHub.
        </p>
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="mt-20 mb-10 text-center sm:mt-28">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        Ready when you are
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        Ship your GraphQL platform on .NET.
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Start with Hot Chocolate, wire up Strawberry Shake, plug in Nitro for
        the registry and telemetry. Free to start, MIT to keep.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}
