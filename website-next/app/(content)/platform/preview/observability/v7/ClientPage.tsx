"use client";

import { useEffect, useRef, useState } from "react";
import {
  MotionConfig,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * Palette and shared constants. Same cc-* dark surfaces as the rest
 * of the site. Teal is the accent for this page, coral is reserved
 * as the data-driven status for the failing span.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";

// The single spectrum event for the page lives in the closing CTA.
const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

// Eased motion that matches the rest of the site.
const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1];

// The trace id is the thread that ties hero, centerpiece, and CTA.
const TRACE_ID = "4b1c8f2a9e07";

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user" transition={{ ease: EASE }}>
      <main className="flex flex-col gap-28 pb-16">
        <Hero />
        <CenterpieceSection />
        <LensesSection />
        <CriticalPathSection />
        <TopologySection />
        <HonestySection />
        <ClosingCta />
      </main>
    </MotionConfig>
  );
}

/* ================================================================== *
 * HERO
 * Outcome headline + dual CTA on the left. Right side shows a static
 * final-frame preview of the trace waterfall so the hero is instant.
 * ================================================================== */

function Hero() {
  return (
    <section className="relative isolate pt-8">
      <HeroGlow />
      <div className="relative grid items-center gap-12 lg:grid-cols-[1.05fr_1fr]">
        <div>
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
            Production view
          </span>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6">
            GraphQL observability
            <br />
            for .NET, OTel-native.
          </h1>
          <p className="lead text-cc-prose mt-6 max-w-xl">
            A live distributed trace, drawn in front of you. The slow gRPC hop
            paints itself coral while the rest of the spans stay teal.
          </p>
          <p className="text-body text-cc-ink-dim mt-5 max-w-xl">
            Nitro reads operation, service, and client lenses off the same
            OpenTelemetry stream: p95, p99, throughput, error rate, and an
            impact score that ranks what hurts most. Every request is one
            distributed trace across GraphQL, REST, gRPC, and background jobs.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-4">
            <SolidButton href="/get-started">Install Nitro</SolidButton>
            <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
              Read the observability guide
            </OutlineButton>
          </div>
          <div className="text-cc-nav-label mt-8 flex items-center gap-3 font-mono text-[11px]">
            <StatusDot color={AMBER} pulse />
            <span className="tracking-wide uppercase">
              Live trace on this page
            </span>
            <span className="text-cc-ink-faint">·</span>
            <span>
              trace <span className="text-cc-ink-dim">{TRACE_ID}</span>
            </span>
          </div>
        </div>
        <HeroPreview />
      </div>
    </section>
  );
}

function HeroGlow() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute -top-24 right-0 -z-10 h-[460px] w-[620px] opacity-60 blur-3xl"
      style={{
        background: `radial-gradient(60% 60% at 70% 30%, ${TEAL}1c 0%, transparent 70%), radial-gradient(50% 50% at 80% 70%, ${AMBER}18 0%, transparent 72%)`,
      }}
    />
  );
}

// Static preview tile: the final frame of the centerpiece, so the hero
// loads instantly and signals what the scroll will reveal.
function HeroPreview() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border p-1 shadow-2xl backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-surface/95 overflow-hidden rounded-xl border">
        <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center gap-2 border-b px-4 py-2.5">
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="text-cc-nav-label ml-2 font-mono text-[11px]">
            nitro · trace {TRACE_ID}
          </span>
          <span className="border-cc-card-border/70 text-cc-nav-label ml-auto inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wide uppercase">
            <StatusDot color={AMBER} />
            spike
          </span>
        </div>
        <div className="px-5 py-5">
          <div className="flex items-baseline justify-between">
            <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
              mutation checkout
            </span>
            <span className="font-mono text-[11px]" style={{ color: CORAL }}>
              318 ms
            </span>
          </div>
          <div className="mt-4 space-y-2">
            {SPANS.slice(0, 7).map((span) => (
              <div key={span.id} className="flex items-center gap-2">
                <span
                  className="text-cc-nav-label w-[42%] truncate font-mono text-[10px]"
                  style={{ paddingLeft: (SPAN_DEPTH[span.idx] ?? 0) * 8 }}
                >
                  {span.label}
                </span>
                <div className="bg-cc-surface/60 relative h-2.5 flex-1 rounded">
                  <div
                    className="absolute top-0 h-full rounded-[2px]"
                    style={{
                      left: `${span.start}%`,
                      width: `${span.width}%`,
                      backgroundColor: span.slow
                        ? CORAL
                        : KIND_COLOR[span.kind],
                      opacity: span.slow ? 1 : 0.72,
                    }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

/* ================================================================== *
 * CENTERPIECE
 * Scroll-anchored, in-view-gated trace waterfall. Spans animate their
 * widths left to right in time-proportional order. A vertical "now"
 * cursor sweeps across, a p99 sparkline draws in sync, and numbers
 * tick up. Reduced motion: static final frame, no sweep.
 * ================================================================== */

interface Span {
  readonly id: string;
  readonly idx: number;
  readonly label: string;
  readonly kind: "graphql" | "rest" | "grpc" | "db" | "cache" | "job";
  readonly start: number;
  readonly width: number;
  readonly ms: number;
  readonly slow?: boolean;
}

const KIND_LABEL: Record<Span["kind"], string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  db: "DB",
  cache: "Cache",
  job: "Job",
};

const KIND_COLOR: Record<Span["kind"], string> = {
  graphql: TEAL,
  rest: VIOLET,
  grpc: CORAL,
  db: "#7dd3fc",
  cache: "#a7f3d0",
  job: "#8b9bd4",
};

// 12 spans, time-proportional. The slow gRPC payments hop is the
// critical-path. Tree depth lives in SPAN_DEPTH below.
const SPANS: readonly Span[] = [
  {
    id: "s0",
    idx: 0,
    label: "mutation checkout",
    kind: "graphql",
    start: 0,
    width: 100,
    ms: 318,
  },
  {
    id: "s1",
    idx: 1,
    label: "resolver: cart",
    kind: "graphql",
    start: 3,
    width: 12,
    ms: 38,
  },
  {
    id: "s2",
    idx: 2,
    label: "cache: cart:get",
    kind: "cache",
    start: 4,
    width: 4,
    ms: 11,
  },
  {
    id: "s3",
    idx: 3,
    label: "db: SELECT cart_items",
    kind: "db",
    start: 8,
    width: 6,
    ms: 17,
  },
  {
    id: "s4",
    idx: 4,
    label: "resolver: charge",
    kind: "graphql",
    start: 16,
    width: 70,
    ms: 222,
    slow: true,
  },
  {
    id: "s5",
    idx: 5,
    label: "rest: /checkout",
    kind: "rest",
    start: 17,
    width: 9,
    ms: 28,
  },
  {
    id: "s6",
    idx: 6,
    label: "grpc: payments.Charge()",
    kind: "grpc",
    start: 26,
    width: 56,
    ms: 178,
    slow: true,
  },
  {
    id: "s7",
    idx: 7,
    label: "db: SELECT account",
    kind: "db",
    start: 28,
    width: 5,
    ms: 14,
  },
  {
    id: "s8",
    idx: 8,
    label: "cache: rate_limit",
    kind: "cache",
    start: 34,
    width: 3,
    ms: 8,
  },
  {
    id: "s9",
    idx: 9,
    label: "db: UPDATE ledger",
    kind: "db",
    start: 76,
    width: 6,
    ms: 19,
  },
  {
    id: "s10",
    idx: 10,
    label: "resolver: receipt",
    kind: "graphql",
    start: 86,
    width: 12,
    ms: 38,
  },
  {
    id: "s11",
    idx: 11,
    label: "job: enqueue receipt",
    kind: "job",
    start: 88,
    width: 9,
    ms: 28,
  },
];

const SPAN_DEPTH: readonly number[] = [0, 1, 2, 2, 1, 2, 3, 4, 4, 3, 1, 2];

function CenterpieceSection() {
  return (
    <section className="relative">
      <SectionEyebrow>One trace, drawn live</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5 max-w-3xl">
        The slow hop draws itself coral. Everything else stays teal.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5 max-w-2xl">
        Scroll into the trace below and the waterfall plays once: spans expand
        in time order, a sweep cursor crosses the request, and the numbers catch
        up to where the incident actually lands.
      </p>
      <div className="mt-10">
        <TraceWaterfall />
      </div>
    </section>
  );
}

function TraceWaterfall() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-20%" });
  const reduced = useReducedMotion() ?? false;
  const [playKey, setPlayKey] = useState(0);
  const playing = inView || playKey > 0;

  // Total runtime of the animation (ms). Each span animates over a
  // window proportional to its real duration. Reduced motion: 0.
  const totalMs = reduced ? 0 : 2400;

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md"
    >
      {/* header */}
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
        <span className="font-mono text-[11px]" style={{ color: TEAL }}>
          {TRACE_ID}
        </span>
        <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          mutation checkout
        </span>
        <div className="ml-auto flex items-center gap-3">
          <TickingDuration
            key={`ticking-${playKey}`}
            target={318}
            durationMs={totalMs}
            play={playing}
            reduced={reduced}
            suffix="ms"
            replayKey={playKey}
          />
          <button
            type="button"
            onClick={() => setPlayKey((k) => k + 1)}
            className="border-cc-card-border/70 text-cc-nav-label hover:text-cc-heading hover:border-cc-card-border-hover inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10px] tracking-wide uppercase transition-colors"
            aria-label="Replay trace animation"
          >
            <ReplayIcon />
            Replay trace
          </button>
        </div>
      </div>

      {/* p99 sparkline drawing in sync */}
      <div className="border-cc-card-border/40 border-b px-5 pt-4 pb-3">
        <div className="flex items-baseline justify-between">
          <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
            p99 latency · 30m
          </span>
          <span className="text-cc-nav-label font-mono text-[10px]">
            spike at +24m
          </span>
        </div>
        <SparklineDraw play={playing} reduced={reduced} replayKey={playKey} />
      </div>

      {/* waterfall body */}
      <div className="relative px-5 py-5">
        <div className="space-y-2.5">
          {SPANS.map((span) => (
            <SpanRow
              key={`${span.id}-${playKey}`}
              span={span}
              depth={SPAN_DEPTH[span.idx] ?? 0}
              play={playing}
              reduced={reduced}
              totalMs={totalMs}
            />
          ))}
        </div>

        {/* sweep cursor */}
        <SweepCursor play={playing} reduced={reduced} replayKey={playKey} />

        {/* slow-span tooltip */}
        <SlowSpanTooltip play={playing} reduced={reduced} />

        {/* time axis */}
        <div className="border-cc-card-border/50 text-cc-nav-label mt-5 ml-[38%] flex items-center justify-between border-t pt-2 font-mono text-[10px]">
          <span>0ms</span>
          <span>100ms</span>
          <span>200ms</span>
          <span>318ms</span>
        </div>
      </div>
    </div>
  );
}

interface SpanRowProps {
  readonly span: Span;
  readonly depth: number;
  readonly play: boolean;
  readonly reduced: boolean;
  readonly totalMs: number;
}

function SpanRow({ span, depth, play, reduced, totalMs }: SpanRowProps) {
  const color = KIND_COLOR[span.kind];
  const isRoot = span.kind === "graphql" && span.idx === 0;
  // Each span's animation window is proportional to where it sits in
  // the trace, so spans appear in real time order.
  const delaySec = reduced ? 0 : (span.start / 100) * (totalMs / 1000);
  const durationSec = reduced
    ? 0
    : Math.max(0.18, (span.width / 100) * (totalMs / 1000));

  return (
    <div className="flex items-center gap-3">
      <div
        className="flex w-[38%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: depth * 12 }}
      >
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[9px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
        >
          {KIND_LABEL[span.kind]}
        </span>
        <span
          className={`truncate font-mono text-[12px] ${
            isRoot ? "text-cc-heading" : "text-cc-ink-dim"
          }`}
        >
          {span.label}
        </span>
      </div>

      <div className="bg-cc-surface/60 relative h-6 flex-1 rounded">
        <motion.div
          className="absolute top-1/2 flex h-4 -translate-y-1/2 items-center rounded-[3px]"
          style={{
            left: `${span.start}%`,
            width: `${span.width}%`,
            backgroundColor: span.slow ? CORAL : color,
            boxShadow: span.slow ? `0 0 16px ${CORAL}55` : undefined,
            transformOrigin: "left center",
            opacity: span.slow ? 1 : 0.78,
          }}
          initial={reduced ? false : { scaleX: 0 }}
          animate={
            play ? { scaleX: 1 } : reduced ? { scaleX: 1 } : { scaleX: 0 }
          }
          transition={{ delay: delaySec, duration: durationSec, ease: EASE }}
        >
          {span.slow && span.kind === "grpc" && (
            <span className="text-cc-surface ml-2 font-mono text-[10px] font-semibold">
              payments.Charge()
            </span>
          )}
        </motion.div>
        <motion.span
          className="text-cc-nav-label absolute top-1/2 -translate-y-1/2 font-mono text-[10px]"
          style={{ left: `calc(${span.start + span.width}% + 8px)` }}
          initial={reduced ? false : { opacity: 0 }}
          animate={
            play ? { opacity: 1 } : reduced ? { opacity: 1 } : { opacity: 0 }
          }
          transition={{ delay: delaySec + durationSec, duration: 0.25 }}
        >
          {span.ms}ms
        </motion.span>
      </div>
    </div>
  );
}

interface SweepCursorProps {
  readonly play: boolean;
  readonly reduced: boolean;
  readonly replayKey: number;
}

function SweepCursor({ play, reduced, replayKey }: SweepCursorProps) {
  if (reduced) {
    return null;
  }
  // The waterfall tracks fill the area to the right of the 38% label
  // column with some padding. We position the cursor over that area
  // and animate left from 0% to 100% so the line sweeps across.
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute top-5 bottom-12"
      style={{ left: "calc(38% + 12px)", right: 20 }}
    >
      <motion.div
        key={replayKey}
        className="absolute top-0 bottom-0"
        style={{
          width: 1,
          backgroundColor: AMBER,
          boxShadow: `0 0 8px ${AMBER}88`,
        }}
        initial={{ left: "0%" }}
        animate={play ? { left: "100%" } : { left: "0%" }}
        transition={{ duration: 2.4, ease: EASE }}
      />
    </div>
  );
}

interface SlowSpanTooltipProps {
  readonly play: boolean;
  readonly reduced: boolean;
}

function SlowSpanTooltip({ play, reduced }: SlowSpanTooltipProps) {
  // Pops in late on the slow span row (row idx 6, ~7th row after header).
  // Anchored in pixels to the gRPC row rather than a percentage of the
  // parent: each row is h-6 (24px) with space-y-2.5 (10px) between, and
  // the body has py-5 (20px) top padding. Row 6 center sits at
  // 20 + 6 * (24 + 10) + 12 = 236px; the tooltip drops just below that.
  const SLOW_ROW_TOP_PX = 20 + 6 * 34 + 26;
  return (
    <motion.div
      aria-hidden
      className="border-cc-card-border/70 bg-cc-surface/95 pointer-events-none absolute rounded-md border px-3 py-2 shadow-lg"
      style={{
        top: `${SLOW_ROW_TOP_PX}px`,
        left: "62%",
      }}
      initial={reduced ? false : { opacity: 0, y: 6 }}
      animate={
        play
          ? { opacity: 1, y: 0 }
          : reduced
            ? { opacity: 1, y: 0 }
            : { opacity: 0, y: 6 }
      }
      transition={{ delay: reduced ? 0 : 2.05, duration: 0.35 }}
    >
      <div className="text-cc-nav-label font-mono text-[9px] tracking-wide uppercase">
        slow span
      </div>
      <div className="text-cc-heading mt-0.5 font-mono text-[11px]">
        payments.Charge()
      </div>
      <div className="mt-1 flex items-center gap-2 font-mono text-[10px]">
        <span style={{ color: CORAL }}>178 ms</span>
        <span className="text-cc-ink-faint">·</span>
        <span className="text-cc-ink-dim">56% of request</span>
      </div>
      <div className="text-cc-nav-label mt-0.5 font-mono text-[9px]">
        trace {TRACE_ID}
      </div>
    </motion.div>
  );
}

interface SparklineDrawProps {
  readonly play: boolean;
  readonly reduced: boolean;
  readonly replayKey: number;
}

function SparklineDraw({ play, reduced, replayKey }: SparklineDrawProps) {
  const points = [
    18, 16, 19, 17, 20, 18, 16, 19, 21, 18, 17, 20, 19, 22, 28, 41, 58, 72, 80,
    78,
  ];
  const w = 640;
  const h = 56;
  const max = 96;
  const step = w / (points.length - 1);
  const coords = points.map((p, i) => {
    const x = i * step;
    const y = h - (p / max) * h;
    return [x, y] as const;
  });
  const line = coords.map(([x, y]) => `${x},${y}`).join(" ");
  const spikeStart = coords.findIndex((_, i) => points[i] >= 41);
  const [sx] = coords[spikeStart];

  return (
    <svg
      viewBox={`0 0 ${w} ${h}`}
      className="mt-2 h-[56px] w-full"
      preserveAspectRatio="none"
      aria-hidden
    >
      <defs>
        <linearGradient id="sparkFillV7" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={CORAL} stopOpacity="0.28" />
          <stop offset="100%" stopColor={CORAL} stopOpacity="0" />
        </linearGradient>
      </defs>
      <line
        x1="0"
        y1={h - (32 / max) * h}
        x2={w}
        y2={h - (32 / max) * h}
        stroke="var(--color-cc-ink-faint)"
        strokeWidth="1"
        strokeDasharray="3 4"
      />
      <line
        x1={sx}
        y1="0"
        x2={sx}
        y2={h}
        stroke={AMBER}
        strokeWidth="1"
        strokeOpacity="0.4"
      />
      <motion.polyline
        key={`spark-${replayKey}`}
        points={line}
        fill="none"
        stroke={CORAL}
        strokeWidth="2"
        strokeLinejoin="round"
        strokeLinecap="round"
        initial={reduced ? false : { pathLength: 0 }}
        animate={
          play
            ? { pathLength: 1 }
            : reduced
              ? { pathLength: 1 }
              : { pathLength: 0 }
        }
        transition={{ duration: reduced ? 0 : 2.4, ease: EASE }}
      />
    </svg>
  );
}

interface TickingDurationProps {
  readonly target: number;
  readonly durationMs: number;
  readonly play: boolean;
  readonly reduced: boolean;
  readonly suffix: string;
  readonly replayKey: number;
}

function TickingDuration({
  target,
  durationMs,
  play,
  reduced,
  suffix,
  replayKey,
}: TickingDurationProps) {
  // Always initialize to 0 so SSR and the first client paint agree
  // (useReducedMotion returns null on first render). The animation
  // effect below settles on the right value once reduced motion
  // resolves. For reduced motion we short-circuit display to the
  // final value during render rather than via setState in an effect.
  const [value, setValue] = useState(0);

  useEffect(() => {
    if (reduced || !play) {
      return;
    }
    const start = performance.now();
    let raf = 0;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / durationMs);
      // ease-out cubic
      const eased = 1 - Math.pow(1 - t, 3);
      setValue(Math.round(target * eased));
      if (t < 1) {
        raf = requestAnimationFrame(tick);
      }
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [play, reduced, target, durationMs, replayKey]);

  // When the user prefers reduced motion, render the final number
  // directly. This avoids both the initial-zero flash and the
  // forbidden setState-in-effect pattern.
  const display = reduced ? target : value;

  return (
    <span className="text-cc-nav-label inline-flex items-center gap-1.5 font-mono text-[11px]">
      duration{" "}
      <span className="text-cc-heading">
        {display}
        {suffix}
      </span>
    </span>
  );
}

function ReplayIcon() {
  return (
    <svg width="10" height="10" viewBox="0 0 16 16" fill="none" aria-hidden>
      <path
        d="M3 8a5 5 0 1 1 1.46 3.54"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <path
        d="M3 4v3h3"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/* ================================================================== *
 * LENSES SECTION
 * Three cards (Ops, Service, Client). Each has a tiny sparkline that
 * draws on whileInView and a number that counts up.
 * ================================================================== */

function LensesSection() {
  return (
    <section>
      <SectionEyebrow>Three lenses, one stream</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5 max-w-2xl">
        Ops, service, client. Pick the angle the question asks for.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5 max-w-2xl">
        The same OpenTelemetry stream slices three ways. Rank operations by
        impact to find what hurts most, drop into the service that is degraded,
        or check which published clients are affected before you ship the fix.
      </p>
      <div className="mt-10 grid gap-5 lg:grid-cols-3">
        <LensCard
          tab="operations"
          title="Ranked by impact"
          metricLabel="checkout · p99"
          metricTarget={318}
          metricSuffix="ms"
          metricTone={CORAL}
          spark="up"
          delay={0}
          note="Impact ranks by what hurts the system, not raw call count."
        />
        <LensCard
          tab="services"
          title="payments · degraded"
          metricLabel="error rate"
          metricTarget={0.3}
          metricSuffix="%"
          metricFractional
          metricTone={AMBER}
          spark="flat"
          delay={0.1}
          note="5xx concentrated on payments.Charge() between 14:02 and 14:08."
        />
        <LensCard
          tab="clients"
          title="Published clients affected"
          metricLabel="web-storefront@4.2.0"
          metricTarget={61}
          metricSuffix="%"
          metricTone={TEAL}
          spark="rising"
          delay={0.2}
          note="See which published clients are affected before you ship the fix."
        />
      </div>
    </section>
  );
}

interface LensCardProps {
  readonly tab: string;
  readonly title: string;
  readonly metricLabel: string;
  readonly metricTarget: number;
  readonly metricSuffix: string;
  readonly metricTone: string;
  readonly metricFractional?: boolean;
  readonly spark: "up" | "flat" | "rising";
  readonly delay: number;
  readonly note: string;
}

function LensCard({
  tab,
  title,
  metricLabel,
  metricTarget,
  metricSuffix,
  metricTone,
  metricFractional,
  spark,
  delay,
  note,
}: LensCardProps) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15%" });
  const reduced = useReducedMotion() ?? false;
  return (
    <motion.div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-md"
      initial={reduced ? false : { opacity: 0, y: 12 }}
      animate={
        inView
          ? { opacity: 1, y: 0 }
          : reduced
            ? { opacity: 1, y: 0 }
            : { opacity: 0, y: 12 }
      }
      transition={{ delay, duration: 0.5, ease: EASE }}
    >
      <div className="border-cc-card-border/60 bg-cc-code-header/60 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          {tab}
        </span>
        <span className="text-cc-ink-faint font-mono text-[10px]">nitro</span>
      </div>
      <div className="px-4 py-4">
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
        <div className="mt-3 flex items-end justify-between gap-4">
          <div>
            <div className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
              {metricLabel}
            </div>
            <div className="mt-1 flex items-baseline gap-1">
              <CountUp
                target={metricTarget}
                fractional={metricFractional}
                tone={metricTone}
                play={inView}
                reduced={reduced}
              />
              <span className="text-cc-ink-dim font-mono text-xs">
                {metricSuffix}
              </span>
            </div>
          </div>
          <MiniSparkline shape={spark} play={inView} reduced={reduced} />
        </div>
        <p className="text-cc-nav-label mt-4 text-[11px]">{note}</p>
      </div>
    </motion.div>
  );
}

interface CountUpProps {
  readonly target: number;
  readonly fractional?: boolean;
  readonly tone: string;
  readonly play: boolean;
  readonly reduced: boolean;
}

function CountUp({ target, fractional, tone, play, reduced }: CountUpProps) {
  // Initialize at 0 to keep SSR and the first client paint in sync
  // (useReducedMotion returns null on first render). When the user
  // prefers reduced motion, render the final number directly during
  // render to avoid the forbidden setState-in-effect pattern.
  const [value, setValue] = useState(0);
  useEffect(() => {
    if (reduced || !play) {
      return;
    }
    const start = performance.now();
    const dur = 1100;
    let raf = 0;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / dur);
      const eased = 1 - Math.pow(1 - t, 3);
      setValue(target * eased);
      if (t < 1) {
        raf = requestAnimationFrame(tick);
      }
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [play, reduced, target]);
  const settled = reduced ? target : value;
  const display = fractional
    ? settled.toFixed(1)
    : Math.round(settled).toString();
  return (
    <span className="font-heading text-h3 leading-none" style={{ color: tone }}>
      {display}
    </span>
  );
}

interface MiniSparklineProps {
  readonly shape: "up" | "flat" | "rising";
  readonly play: boolean;
  readonly reduced: boolean;
}

function MiniSparkline({ shape, play, reduced }: MiniSparklineProps) {
  // Three tiny waveforms, each 80x28.
  const series: Record<MiniSparklineProps["shape"], readonly number[]> = {
    up: [14, 12, 15, 13, 14, 12, 13, 18, 24, 22],
    flat: [10, 12, 11, 13, 12, 11, 12, 11, 13, 12],
    rising: [8, 10, 11, 13, 14, 14, 16, 18, 19, 21],
  };
  const color: Record<MiniSparklineProps["shape"], string> = {
    up: CORAL,
    flat: AMBER,
    rising: TEAL,
  };
  const pts = series[shape];
  const w = 80;
  const h = 28;
  const max = 28;
  const step = w / (pts.length - 1);
  const path = pts
    .map((p, i) => `${i === 0 ? "M" : "L"} ${i * step} ${h - (p / max) * h}`)
    .join(" ");
  return (
    <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`} aria-hidden>
      <motion.path
        d={path}
        fill="none"
        stroke={color[shape]}
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
        initial={reduced ? false : { pathLength: 0 }}
        animate={
          play
            ? { pathLength: 1 }
            : reduced
              ? { pathLength: 1 }
              : { pathLength: 0 }
        }
        transition={{ duration: reduced ? 0 : 1.1, ease: EASE }}
      />
    </svg>
  );
}

/* ================================================================== *
 * CRITICAL PATH
 * Zoom in on the slow gRPC span, with annotation lines animating in.
 * ================================================================== */

function CriticalPathSection() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15%" });
  const reduced = useReducedMotion() ?? false;
  return (
    <section className="grid gap-10 lg:grid-cols-[1fr_1.15fr] lg:items-center">
      <div>
        <SectionEyebrow>Critical path</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          The worst offender, named and ranked.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          Impact score surfaces the hop that owns the request. In this trace it
          is{" "}
          <code className="font-mono" style={{ color: CORAL }}>
            payments.Charge()
          </code>{" "}
          at 178 ms, 56% of the 318 ms request. The other ten spans together
          account for the remaining 140 ms.
        </p>
        <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
          <CheckLine>
            One span, one OpenTelemetry attribute set, one fix to ship
          </CheckLine>
          <CheckLine>
            Ranked by impact across operations, not just by raw count
          </CheckLine>
          <CheckLine>
            Same <code className="text-cc-ink font-mono">trace {TRACE_ID}</code>{" "}
            you saw above
          </CheckLine>
        </ul>
      </div>
      <div
        ref={ref}
        className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-6 backdrop-blur-md"
      >
        <svg viewBox="0 0 360 220" className="h-auto w-full" aria-hidden>
          {/* faint context spans behind */}
          <rect
            x="0"
            y="40"
            width="60"
            height="8"
            rx="2"
            fill={TEAL}
            opacity="0.3"
          />
          <rect
            x="60"
            y="60"
            width="40"
            height="8"
            rx="2"
            fill={VIOLET}
            opacity="0.3"
          />
          {/* the focused slow span */}
          <motion.rect
            x="100"
            y="92"
            height="22"
            rx="3"
            fill={CORAL}
            initial={reduced ? false : { width: 0 }}
            animate={
              inView ? { width: 220 } : reduced ? { width: 220 } : { width: 0 }
            }
            transition={{ duration: reduced ? 0 : 0.9, ease: EASE }}
            style={{ filter: `drop-shadow(0 0 12px ${CORAL}55)` }}
          />
          <text
            x="108"
            y="107"
            fill="var(--color-cc-surface)"
            fontSize="10"
            fontFamily="monospace"
            fontWeight="600"
          >
            payments.Charge()
          </text>
          <rect
            x="320"
            y="130"
            width="30"
            height="8"
            rx="2"
            fill={TEAL}
            opacity="0.3"
          />

          {/* annotation lines */}
          <motion.g
            initial={reduced ? false : { opacity: 0 }}
            animate={
              inView
                ? { opacity: 1 }
                : reduced
                  ? { opacity: 1 }
                  : { opacity: 0 }
            }
            transition={{ delay: reduced ? 0 : 0.9, duration: 0.5 }}
          >
            <line
              x1="320"
              y1="92"
              x2="340"
              y2="60"
              stroke={CORAL}
              strokeWidth="1"
              strokeDasharray="2 3"
            />
            <text
              x="340"
              y="52"
              fill="var(--color-cc-heading)"
              fontSize="10"
              fontFamily="monospace"
              textAnchor="end"
            >
              178 ms
            </text>
            <text
              x="340"
              y="40"
              fill="var(--color-cc-nav-label)"
              fontSize="8"
              fontFamily="monospace"
              textAnchor="end"
              letterSpacing="0.08em"
            >
              56% OF REQUEST
            </text>
          </motion.g>

          <motion.g
            initial={reduced ? false : { opacity: 0 }}
            animate={
              inView
                ? { opacity: 1 }
                : reduced
                  ? { opacity: 1 }
                  : { opacity: 0 }
            }
            transition={{ delay: reduced ? 0 : 1.15, duration: 0.5 }}
          >
            <line
              x1="100"
              y1="114"
              x2="80"
              y2="170"
              stroke={AMBER}
              strokeWidth="1"
              strokeDasharray="2 3"
            />
            <text
              x="80"
              y="186"
              fill="var(--color-cc-heading)"
              fontSize="10"
              fontFamily="monospace"
              textAnchor="start"
            >
              ranked #1 by impact
            </text>
            <text
              x="80"
              y="200"
              fill="var(--color-cc-nav-label)"
              fontSize="8"
              fontFamily="monospace"
              textAnchor="start"
              letterSpacing="0.08em"
            >
              CHECKOUT, LAST 30M
            </text>
          </motion.g>
        </svg>
      </div>
    </section>
  );
}

/* ================================================================== *
 * TOPOLOGY MINI-GRAPH
 * Nodes fade in, edges draw between them with staggered motion, the
 * failing edge pulses coral.
 * ================================================================== */

interface TopoNode {
  readonly id: string;
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly kind: Span["kind"];
}

const TOPO_NODES: readonly TopoNode[] = [
  { id: "gw", x: 180, y: 48, label: "gateway", kind: "graphql" },
  { id: "co", x: 90, y: 140, label: "checkout", kind: "rest" },
  { id: "pm", x: 270, y: 140, label: "payments", kind: "grpc" },
  { id: "db", x: 180, y: 232, label: "ledger-db", kind: "db" },
];

interface TopoEdge {
  readonly from: string;
  readonly to: string;
  readonly hot?: boolean;
}

const TOPO_EDGES: readonly TopoEdge[] = [
  { from: "gw", to: "co" },
  { from: "gw", to: "pm", hot: true },
  { from: "co", to: "db" },
  { from: "pm", to: "db" },
];

function TopologySection() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15%" });
  const reduced = useReducedMotion() ?? false;
  const nodeById = (id: string) => TOPO_NODES.find((n) => n.id === id)!;

  return (
    <section className="grid gap-10 lg:grid-cols-[1fr_1.15fr] lg:items-center">
      <div>
        <SectionEyebrow>Every .NET service, one trace</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          The graph is the entry. The trace goes all the way down.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          A distributed trace does not stop at the GraphQL boundary. Nitro
          monitors REST APIs, gRPC services, and background jobs through{" "}
          <code className="text-cc-ink font-mono">
            ChilliCream.Nitro.OpenTelemetry
          </code>
          , so the same trace that opens on{" "}
          <code className="font-mono" style={{ color: TEAL }}>
            checkout
          </code>{" "}
          follows the call down to the hop that is actually slow.
        </p>
        <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
          <CheckLine>
            Operation, service, and client views over one OpenTelemetry stream
          </CheckLine>
          <CheckLine>
            Vendor-neutral OTel: no proprietary agent to wire up
          </CheckLine>
          <CheckLine>
            The hot hop pulses, so the eye lands on cause, not noise
          </CheckLine>
        </ul>
      </div>
      <div
        ref={ref}
        className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 backdrop-blur-md"
      >
        <svg
          viewBox="0 0 360 290"
          className="h-auto w-full"
          role="img"
          aria-label="Service topology: gateway fans out to checkout and payments; both connect to the ledger database. The hot edge to payments pulses."
        >
          <defs>
            <linearGradient id="hotEdgeV7" x1="0" y1="0" x2="1" y2="1">
              <stop offset="0%" stopColor={TEAL} stopOpacity="0.5" />
              <stop offset="100%" stopColor={CORAL} stopOpacity="0.95" />
            </linearGradient>
            <filter
              id="nodeGlowV7"
              x="-50%"
              y="-50%"
              width="200%"
              height="200%"
            >
              <feGaussianBlur stdDeviation="4" result="b" />
              <feMerge>
                <feMergeNode in="b" />
                <feMergeNode in="SourceGraphic" />
              </feMerge>
            </filter>
          </defs>

          {/* edges (drawn first, behind nodes) */}
          {TOPO_EDGES.map((edge, i) => {
            const from = nodeById(edge.from);
            const to = nodeById(edge.to);
            const length = Math.hypot(to.x - from.x, to.y - from.y);
            return (
              <motion.line
                key={`${edge.from}-${edge.to}`}
                x1={from.x}
                y1={from.y}
                x2={to.x}
                y2={to.y}
                stroke={
                  edge.hot ? "url(#hotEdgeV7)" : "var(--color-cc-card-border)"
                }
                strokeWidth={edge.hot ? 2 : 1.25}
                strokeDasharray={length}
                initial={
                  reduced ? false : { strokeDashoffset: length, opacity: 0 }
                }
                animate={
                  inView
                    ? edge.hot
                      ? {
                          strokeDashoffset: 0,
                          opacity: [0.6, 1, 0.6],
                        }
                      : { strokeDashoffset: 0, opacity: 1 }
                    : reduced
                      ? { strokeDashoffset: 0, opacity: 1 }
                      : { strokeDashoffset: length, opacity: 0 }
                }
                transition={
                  edge.hot
                    ? {
                        strokeDashoffset: {
                          delay: reduced ? 0 : 0.55 + i * 0.1,
                          duration: reduced ? 0 : 0.8,
                          ease: EASE,
                        },
                        opacity: {
                          delay: reduced ? 0 : 1.35,
                          duration: 1.8,
                          repeat: Infinity,
                          repeatType: "loop",
                          ease: "easeInOut",
                        },
                      }
                    : {
                        delay: reduced ? 0 : 0.55 + i * 0.1,
                        duration: reduced ? 0 : 0.6,
                        ease: EASE,
                      }
                }
              />
            );
          })}

          {/* nodes */}
          {TOPO_NODES.map((n, i) => {
            const isHot = n.id === "pm";
            const color = isHot ? CORAL : KIND_COLOR[n.kind];
            return (
              <motion.g
                key={n.id}
                filter={isHot ? "url(#nodeGlowV7)" : undefined}
                initial={reduced ? false : { opacity: 0, y: 6 }}
                animate={
                  inView
                    ? { opacity: 1, y: 0 }
                    : reduced
                      ? { opacity: 1, y: 0 }
                      : { opacity: 0, y: 6 }
                }
                transition={{
                  delay: reduced ? 0 : 0.1 + i * 0.1,
                  duration: 0.45,
                  ease: EASE,
                }}
              >
                <rect
                  x={n.x - 50}
                  y={n.y - 18}
                  width={100}
                  height={36}
                  rx={8}
                  fill="var(--color-cc-surface)"
                  stroke={color}
                  strokeWidth={isHot ? 1.5 : 1}
                  strokeOpacity={isHot ? 1 : 0.6}
                />
                <circle cx={n.x - 38} cy={n.y} r={3} fill={color} />
                <text
                  x={n.x - 28}
                  y={n.y - 1}
                  fill="var(--color-cc-heading)"
                  fontSize="10"
                  fontFamily="monospace"
                >
                  {n.label}
                </text>
                <text
                  x={n.x - 28}
                  y={n.y + 10}
                  fill="var(--color-cc-nav-label)"
                  fontSize="7.5"
                  fontFamily="monospace"
                  letterSpacing="0.08em"
                >
                  {KIND_LABEL[n.kind].toUpperCase()}
                </text>
              </motion.g>
            );
          })}
        </svg>
        <div className="text-cc-nav-label mt-3 flex flex-wrap items-center justify-center gap-x-4 gap-y-1 font-mono text-[10px]">
          <LegendChip kind="graphql" />
          <LegendChip kind="rest" />
          <LegendChip kind="grpc" />
          <LegendChip kind="db" />
        </div>
      </div>
    </section>
  );
}

interface LegendChipProps {
  readonly kind: Span["kind"];
}

function LegendChip({ kind }: LegendChipProps) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span
        className="h-2 w-2 rounded-[2px]"
        style={{ backgroundColor: KIND_COLOR[kind] }}
      />
      {KIND_LABEL[kind]}
    </span>
  );
}

/* ================================================================== *
 * HONESTY / WHAT IT IS NOT
 * Plain copy, no motion.
 * ================================================================== */

function HonestySection() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border px-6 py-9 backdrop-blur-md sm:px-10">
      <SectionEyebrow>What it is, what it is not</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading mt-5 max-w-2xl">
        Honest about the setup, precise about the payoff.
      </h2>
      <div className="mt-8 grid gap-6 md:grid-cols-3">
        <HonestyCard title="OpenTelemetry end to end">
          Spans are OTel spans, attributes are OTel attributes. There is no
          proprietary agent in front of your services, and your existing OTel
          collector keeps working.
        </HonestyCard>
        <HonestyCard title="Your dashboards still work">
          Nitro is one consumer of the same telemetry. Send the stream to Nitro
          for the operation, service, and client views; keep sending it to your
          existing tools too.
        </HonestyCard>
        <HonestyCard title="Telemetry is configured, not magic">
          The dashboards above come from telemetry you point at Nitro. It is a
          configuration step, deliberate and documented, not something that
          turns on by itself.
        </HonestyCard>
      </div>
    </section>
  );
}

interface HonestyCardProps {
  readonly title: string;
  readonly children: React.ReactNode;
}

function HonestyCard({ title, children }: HonestyCardProps) {
  return (
    <div className="border-cc-card-border/70 bg-cc-surface/50 rounded-xl border px-5 py-5">
      <div className="flex items-center gap-2">
        <span style={{ color: TEAL }}>
          <CheckIcon size={15} />
        </span>
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
      </div>
      <p className="text-caption text-cc-ink-dim mt-3">{children}</p>
    </div>
  );
}

/* ================================================================== *
 * CLOSING CTA, the single spectrum event for the page. The trace id
 * repeats here as a quiet thread back to the centerpiece.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/80 relative overflow-hidden rounded-2xl border px-6 py-14 text-center backdrop-blur-md sm:px-12">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px"
        style={{ background: SPECTRUM }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -bottom-24 left-1/2 h-64 w-[680px] -translate-x-1/2 opacity-25 blur-3xl"
        style={{ background: SPECTRUM }}
      />
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Production view
      </span>
      <h2 className="font-heading text-h3 text-cc-heading sm:text-h2 mt-5">
        Stop guessing. Watch the trace.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Wire your services to OpenTelemetry once and every request becomes
        evidence: ranked by impact, traced end to end, slow span already coral.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Install Nitro</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the observability guide
        </OutlineButton>
      </div>
      <div className="text-cc-nav-label mt-8 font-mono text-[11px]">
        trace <span className="text-cc-ink-dim">{TRACE_ID}</span>
      </div>
    </section>
  );
}

/* ================================================================== *
 * Shared primitives
 * ================================================================== */

interface SectionEyebrowProps {
  readonly children: React.ReactNode;
}

function SectionEyebrow({ children }: SectionEyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
      {children}
    </span>
  );
}

interface StatusDotProps {
  readonly color: string;
  readonly pulse?: boolean;
}

function StatusDot({ color, pulse }: StatusDotProps) {
  return (
    <span className="relative inline-flex h-2 w-2 shrink-0">
      {pulse && (
        <span
          className="absolute inline-flex h-full w-full rounded-full opacity-60 motion-safe:animate-ping"
          style={{ backgroundColor: color }}
        />
      )}
      <span
        className="relative inline-flex h-2 w-2 rounded-full"
        style={{ backgroundColor: color }}
      />
    </span>
  );
}

interface CheckLineProps {
  readonly children: React.ReactNode;
}

function CheckLine({ children }: CheckLineProps) {
  return (
    <li className="flex items-start gap-2.5">
      <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
        <CheckIcon size={14} />
      </span>
      <span>{children}</span>
    </li>
  );
}
