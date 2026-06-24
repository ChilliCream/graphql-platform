"use client";

import { useEffect, useRef, useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import {
  MotionConfig,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * "tail -f production". The page is staged as a terminal window
 * streaming live observability output. Scanlines drift behind each
 * pane to evoke a CRT monitoring console, kept honest to ops work
 * (reading p99 spikes, scrolling spans, watching a log tail).
 *
 * Same cc-* dark surfaces as the rest of the site. Teal is the single
 * accent; status colors (amber, coral) are rationed as data, not chrome.
 * The brand spectrum appears exactly once, as a hairline above the CTA.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";

// The single spectrum event for the page lives above the closing CTA.
const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

// The scanline overlay: 1px tinted teal at ~4% opacity, on a 4px stride.
const SCANLINES = `repeating-linear-gradient(to bottom, transparent 0 3px, rgba(94,234,212,0.04) 3px 4px)`;

// A vertical fade so scanline edges never feel hard.
const SCAN_MASK =
  "linear-gradient(to bottom, transparent 0%, black 14%, black 86%, transparent 100%)";

// Eased motion that matches the rest of the site.
const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1];

// The trace id is the thread that ties hero, incident, and trace panes.
const TRACE_ID = "4b1c8f2a9e07";

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user" transition={{ ease: EASE }}>
      <main className="text-cc-ink flex flex-col gap-24 pb-16">
        <Hero />
        <IncidentStrip />
        <LensesSection />
        <TraceSection />
        <TopologySection />
        <SignalsSection />
        <HonestyNote />
        <ClosingCta />
      </main>
    </MotionConfig>
  );
}

/* ================================================================== *
 * Scanline layer + terminal-pane chrome (the diegetic frame).
 * ================================================================== */

function Scanlines() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0 z-0"
      style={{
        backgroundImage: SCANLINES,
        WebkitMaskImage: SCAN_MASK,
        maskImage: SCAN_MASK,
      }}
    />
  );
}

interface TerminalPaneProps {
  readonly command: string;
  readonly meta: readonly string[];
  readonly children: ReactNode;
  readonly className?: string;
  readonly hairlineGrid?: boolean;
}

// Each major section is framed as a [ops@nitro:~]$ <command> pane: a thin
// top bar carrying a mono caption, then content, over drifting scanlines.
function TerminalPane({
  command,
  meta,
  children,
  className,
  hairlineGrid,
}: TerminalPaneProps) {
  return (
    <section
      className={`border-cc-card-border bg-cc-surface/80 relative isolate overflow-hidden rounded-2xl border ${className ?? ""}`}
    >
      <Scanlines />
      {hairlineGrid && (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 z-0"
          style={{
            backgroundImage: `repeating-linear-gradient(to right, transparent 0 calc(8.333% - 1px), rgba(245,241,234,0.04) calc(8.333% - 1px) 8.333%)`,
          }}
        />
      )}
      <div className="border-cc-card-border/60 bg-cc-code-header/60 relative z-10 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-2.5">
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="text-cc-ink-dim ml-1 font-mono text-[11px]">
          <span style={{ color: TEAL }}>[ops@nitro:~]$</span> {command}
        </span>
        {meta.map((m) => (
          <span
            key={m}
            className="text-cc-nav-label hidden font-mono text-[10px] tracking-wide uppercase last:ml-auto last:inline sm:inline"
          >
            {m}
          </span>
        ))}
      </div>
      <div className="relative z-10">{children}</div>
    </section>
  );
}

/* ================================================================== *
 * HERO
 * Left: outcome-led copy + dual CTA. Right: a live "tail -f" pane that
 * appends a new mono log row every ~1.4s, with a CRT refresh beam
 * sweeping the pane. The trace id stitches into the next sections.
 * ================================================================== */

function Hero() {
  return (
    <TerminalPane
      command="tail -f operations.live"
      meta={["TRACE 4b1c8f2a", "ENV prod-eu-west", "ts +00:00:00"]}
      hairlineGrid
    >
      <HeroBeam />
      <div className="relative grid items-center gap-12 px-6 py-10 sm:px-8 lg:grid-cols-[1.05fr_1fr]">
        <div>
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
            Prod view · trace <span className="text-cc-ink-dim">4b1c8f2a</span>
          </span>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6">
            See what the API
            <br />
            is doing.
          </h1>
          <p className="lead text-cc-prose mt-6 max-w-xl">
            The moment latency climbs, you already know which operation hurts,
            who it reaches, and exactly which hop is slow.
          </p>
          <p className="text-body text-cc-ink-dim mt-5 max-w-xl">
            Nitro is OpenTelemetry-native: operation, service, and client views
            with p95 / p99, throughput, error rate, and an impact score. Every
            request is a distributed trace that spans GraphQL, REST, gRPC, and
            background jobs, so debugging starts from evidence, not another
            dashboard project.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
              Read the Docs
            </OutlineButton>
          </div>
          <div className="text-cc-nav-label mt-8 flex items-center gap-3 font-mono text-[11px]">
            <StatusDot color={AMBER} pulse />
            <span className="tracking-wide uppercase">
              Live tail on this page
            </span>
            <span className="text-cc-ink-faint">·</span>
            <span>
              trace <span className="text-cc-ink-dim">{TRACE_ID}</span>
            </span>
          </div>
        </div>
        <LiveTail />
      </div>
    </TerminalPane>
  );
}

// A 1px teal beam that sweeps the hero pane top to bottom, like a CRT
// refresh. Time-driven, never coupled to scroll. Disabled when reduced.
function HeroBeam() {
  const reduced = useReducedMotion();
  if (reduced) {
    return null;
  }
  return (
    <motion.div
      aria-hidden
      className="pointer-events-none absolute inset-x-0 top-0 z-0 h-px"
      style={{ backgroundColor: TEAL, opacity: 0.08 }}
      animate={{ y: ["0%", "100%"] }}
      transition={{ duration: 6, ease: "linear", repeat: Infinity }}
    />
  );
}

interface TailRow {
  readonly id: number;
  readonly metric: string;
  readonly value: string;
  readonly tone: "ink" | "ok" | "warn" | "fire";
}

// The believable rolling tail. Rows are drawn from a fixed deck so the
// numbers stay honest to the rest of the page (p99 318ms, errors 0.3%).
const TAIL_DECK: readonly Omit<TailRow, "id">[] = [
  { metric: "checkout · p99", value: "318ms", tone: "fire" },
  { metric: "checkout · throughput", value: "9.4k rpm", tone: "ink" },
  { metric: "billing.Charge() · gRPC", value: "201ms", tone: "warn" },
  { metric: "checkout · error rate", value: "0.3%", tone: "warn" },
  { metric: "cartSummary · p95", value: "31ms", tone: "ok" },
  { metric: "users-svc · GET /me", value: "21ms", tone: "ink" },
  { metric: "checkout · p95", value: "42ms", tone: "ok" },
  { metric: "worker · enqueue receipt", value: "37ms", tone: "ink" },
];

const TAIL_TONE: Record<TailRow["tone"], string> = {
  ink: "var(--color-cc-ink-dim)",
  ok: GREEN,
  warn: AMBER,
  fire: CORAL,
};

function LiveTail() {
  const reduced = useReducedMotion();
  const seed: TailRow[] = TAIL_DECK.slice(0, 6).map((r, i) => ({
    ...r,
    id: i,
  }));
  const [rows, setRows] = useState<TailRow[]>(seed);
  const next = useRef(seed.length);

  useEffect(() => {
    if (reduced) {
      return;
    }
    const timer = setInterval(() => {
      setRows((prev) => {
        const pick = TAIL_DECK[next.current % TAIL_DECK.length];
        const appended: TailRow = { ...pick, id: next.current };
        next.current += 1;
        return [...prev.slice(1), appended];
      });
    }, 1400);
    return () => clearInterval(timer);
  }, [reduced]);

  return (
    <div className="border-cc-card-border/70 bg-cc-code-bg/80 overflow-hidden rounded-xl border">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center gap-2 border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px]">
          nitro · live tail
        </span>
        <span className="border-cc-card-border/70 text-cc-nav-label ml-auto inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wide uppercase">
          <StatusDot color={GREEN} />
          streaming
        </span>
      </div>
      <div className="space-y-1.5 px-4 py-4">
        {rows.map((row) => (
          <div
            key={row.id}
            className="flex items-baseline gap-3 font-mono text-[12px]"
          >
            <span className="text-cc-ink-faint shrink-0">{">"}</span>
            <span className="text-cc-ink-dim flex-1 truncate">
              {row.metric}
            </span>
            <span style={{ color: TAIL_TONE[row.tone] }}>{row.value}</span>
          </div>
        ))}
        <div className="flex items-center gap-2 pt-1 font-mono text-[12px]">
          <span className="text-cc-ink-faint">{">"}</span>
          <Caret reduced={reduced} />
        </div>
      </div>
    </div>
  );
}

interface CaretProps {
  readonly reduced: boolean | null;
}

function Caret({ reduced }: CaretProps) {
  if (reduced) {
    return (
      <span
        className="inline-block h-3.5 w-1.5"
        style={{ backgroundColor: TEAL, opacity: 0.7 }}
      />
    );
  }
  return (
    <motion.span
      className="inline-block h-3.5 w-1.5"
      style={{ backgroundColor: TEAL }}
      animate={{ opacity: [1, 1, 0, 0] }}
      transition={{ duration: 1.1, repeat: Infinity, times: [0, 0.5, 0.5, 1] }}
    />
  );
}

/* ================================================================== *
 * INCIDENT STRIP
 * A single wide p99 sparkline across 60s with an amber spike, framed as
 * `tail -f operations.p99`. The line draws once on enter-view, then sits.
 * ================================================================== */

function IncidentStrip() {
  return (
    <TerminalPane
      command="tail -f operations.p99"
      meta={["OP checkout.placeOrder", "WINDOW 60s", "IMPACT 0.81"]}
    >
      <div className="px-6 py-8 sm:px-8">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
              Incident · p99 spike
            </span>
            <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-3">
              Follow the slow span, not the dashboard.
            </h2>
          </div>
          <div className="flex items-center gap-2 font-mono text-[12px]">
            <StatusDot color={AMBER} pulse />
            <span style={{ color: CORAL }}>+312ms</span>
            <span className="text-cc-ink-faint">·</span>
            <span className="text-cc-ink-dim">impact 0.81</span>
          </div>
        </div>
        <P99Strip />
        <div className="text-cc-nav-label mt-4 flex flex-wrap items-center gap-x-6 gap-y-1 font-mono text-[11px]">
          <span>
            op <span className="text-cc-ink-dim">checkout.placeOrder</span>
          </span>
          <span>
            trace <span style={{ color: TEAL }}>{TRACE_ID}</span>
          </span>
          <span className="text-cc-ink-dim">
            one click from the spike to its trace
          </span>
        </div>
      </div>
    </TerminalPane>
  );
}

// A wide 60s p99 strip: baseline then a coral spike onset. The path draws
// once when it scrolls into view (enter-view-once), no scroll coupling.
function P99Strip() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15%" });
  const reduced = useReducedMotion();

  const points = [
    16, 18, 15, 17, 16, 19, 17, 16, 18, 20, 17, 16, 19, 18, 21, 30, 46, 62, 76,
    80, 78,
  ];
  const w = 960;
  const h = 120;
  const max = 96;
  const step = w / (points.length - 1);
  const coords = points.map((p, i) => {
    const x = i * step;
    const y = h - (p / max) * h;
    return [x, y] as const;
  });
  const line = coords.map(([x, y]) => `${x},${y}`).join(" ");
  const area = `${line} ${w},${h} 0,${h}`;
  const spikeIndex = points.findIndex((p) => p >= 46);
  const [sx] = coords[spikeIndex];
  const last = coords[coords.length - 1];

  return (
    <div ref={ref} className="mt-6">
      <div className="border-cc-card-border/60 bg-cc-code-bg/70 relative overflow-hidden rounded-xl border px-4 pt-3 pb-2">
        <div className="text-cc-nav-label mb-1 flex items-center justify-between font-mono text-[10px] tracking-wide uppercase">
          <span>p99 latency · 60s</span>
          <span style={{ color: CORAL }}>▲ 318 ms</span>
        </div>
        <svg
          viewBox={`0 0 ${w} ${h}`}
          className="h-[120px] w-full"
          preserveAspectRatio="none"
          aria-hidden
        >
          <defs>
            <linearGradient id="p99Fill-v8" x1="0" y1="0" x2="0" y2="1">
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
            strokeDasharray="4 5"
          />
          <line
            x1={sx}
            y1="0"
            x2={sx}
            y2={h}
            stroke={AMBER}
            strokeWidth="1"
            strokeOpacity="0.5"
          />
          <motion.polygon
            points={area}
            fill="url(#p99Fill-v8)"
            initial={reduced ? { opacity: 1 } : { opacity: 0 }}
            animate={inView ? { opacity: 1 } : undefined}
            transition={{ duration: 0.8, delay: 0.6 }}
          />
          <motion.polyline
            points={line}
            fill="none"
            stroke={CORAL}
            strokeWidth="2.5"
            strokeLinejoin="round"
            strokeLinecap="round"
            initial={reduced ? { pathLength: 1 } : { pathLength: 0 }}
            animate={inView ? { pathLength: 1 } : undefined}
            transition={{ duration: 1.4 }}
          />
          <motion.circle
            cx={last[0]}
            cy={last[1]}
            r="4"
            fill={CORAL}
            initial={reduced ? { opacity: 1 } : { opacity: 0 }}
            animate={inView ? { opacity: 1 } : undefined}
            transition={{ duration: 0.4, delay: 1.4 }}
          />
        </svg>
        <div className="text-cc-nav-label mt-1 flex items-center justify-between font-mono text-[10px]">
          <span>-60s</span>
          <span>-30s</span>
          <span>now</span>
        </div>
      </div>
    </div>
  );
}

/* ================================================================== *
 * THREE LENSES
 * Operation / Service / Client mini-panes, each a mono header, one chart
 * row, and two numeric KPIs. The same incident, sliced three ways.
 * ================================================================== */

function LensesSection() {
  return (
    <TerminalPane command="watch operations --by lens" meta={["3 LENSES"]}>
      <div className="px-6 py-8 sm:px-8">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          Many lenses, same incident
        </span>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-3 max-w-2xl">
          Operation, service, client. Pick the angle the question asks for.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5 max-w-2xl">
          Telemetry is the same stream, sliced three ways. Rank operations by
          impact to find what hurts most, drop into the service that is
          degraded, or check which published clients are affected before you
          ship a fix.
        </p>
        <div className="mt-8 grid gap-4 lg:grid-cols-3">
          <LensPane
            tab="operation"
            title="Ranked by impact"
            kpis={[
              { label: "p95", value: "42ms", tone: "ok" },
              { label: "error %", value: "0.3%", tone: "warn" },
            ]}
          >
            <BarRow label="checkout" pct={98} tone="fire" />
            <BarRow label="cartSummary" pct={61} tone="warn" />
            <BarRow label="productList" pct={32} tone="ok" />
          </LensPane>
          <LensPane
            tab="service"
            title="billing · degraded"
            kpis={[
              { label: "p95", value: "42ms", tone: "ok" },
              { label: "error %", value: "0.3%", tone: "warn" },
            ]}
          >
            <div className="text-cc-nav-label mb-1.5 font-mono text-[10px] tracking-wide uppercase">
              status codes · 5m
            </div>
            <div className="flex h-2 overflow-hidden rounded-full">
              <span style={{ width: "96.4%", backgroundColor: GREEN }} />
              <span style={{ width: "3.3%", backgroundColor: AMBER }} />
              <span style={{ width: "0.3%", backgroundColor: CORAL }} />
            </div>
            <div className="mt-2 flex items-center gap-3 font-mono text-[10px]">
              <span style={{ color: GREEN }}>2xx 96.4%</span>
              <span style={{ color: AMBER }}>4xx 3.3%</span>
              <span style={{ color: CORAL }}>5xx 0.3%</span>
            </div>
          </LensPane>
          <LensPane
            tab="client"
            title="Published clients affected"
            kpis={[
              { label: "p95", value: "57ms", tone: "warn" },
              { label: "error %", value: "0.4%", tone: "warn" },
            ]}
          >
            <BarRow label="web-storefront@4.2.0" pct={61} tone="fire" />
            <BarRow label="ios-app@3.8.1" pct={27} tone="warn" />
            <BarRow label="partner-api@1.0" pct={12} tone="ok" />
          </LensPane>
        </div>
      </div>
    </TerminalPane>
  );
}

interface Kpi {
  readonly label: string;
  readonly value: string;
  readonly tone: "ok" | "warn" | "fire";
}

interface LensPaneProps {
  readonly tab: string;
  readonly title: string;
  readonly kpis: readonly Kpi[];
  readonly children: ReactNode;
}

const KPI_TONE: Record<Kpi["tone"], string> = {
  ok: GREEN,
  warn: AMBER,
  fire: CORAL,
};

function LensPane({ tab, title, kpis, children }: LensPaneProps) {
  return (
    <div className="border-cc-card-border/70 bg-cc-code-bg/70 overflow-hidden rounded-xl border">
      <div className="border-cc-card-border/60 bg-cc-code-header/60 flex items-center justify-between border-b px-4 py-2.5 font-mono text-[10px] tracking-wide uppercase">
        <span className="text-cc-nav-label">{tab}</span>
        <span className="text-cc-ink-faint">nitro</span>
      </div>
      <div className="px-4 py-4">
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
        <div className="mt-3 space-y-1.5">{children}</div>
        <div className="border-cc-card-border/50 mt-4 grid grid-cols-2 gap-3 border-t pt-3">
          {kpis.map((k) => (
            <div key={k.label}>
              <div className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
                {k.label}
              </div>
              <div
                className="mt-0.5 font-mono text-sm"
                style={{ color: KPI_TONE[k.tone] }}
              >
                {k.value}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

interface BarRowProps {
  readonly label: string;
  readonly pct: number;
  readonly tone: "ok" | "warn" | "fire";
}

function BarRow({ label, pct, tone }: BarRowProps) {
  const color = KPI_TONE[tone];
  return (
    <div className="flex items-center gap-2.5">
      <StatusDot color={color} pulse={tone === "fire"} />
      <span className="text-cc-ink-dim flex-1 truncate font-mono text-[12px]">
        {label}
      </span>
      <span className="bg-cc-surface/70 h-1 w-12 overflow-hidden rounded-full">
        <span
          className="block h-full rounded-full"
          style={{ width: `${pct}%`, backgroundColor: color }}
        />
      </span>
    </div>
  );
}

/* ================================================================== *
 * DISTRIBUTED TRACE WATERFALL
 * Vertical span list, GraphQL > REST > gRPC > job, monospace timing
 * column, the same TRACE 4b1c8f2a threaded through. One coral slow span.
 * ================================================================== */

interface Span {
  readonly id: string;
  readonly label: string;
  readonly kind: "graphql" | "rest" | "grpc" | "job" | "db";
  readonly start: number;
  readonly width: number;
  readonly ms: string;
  readonly depth: number;
  readonly slow?: boolean;
}

const KIND_LABEL: Record<Span["kind"], string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  job: "Job",
  db: "DB",
};

const KIND_COLOR: Record<Span["kind"], string> = {
  graphql: TEAL,
  rest: VIOLET,
  grpc: CORAL,
  job: "#8b9bd4",
  db: "#7dd3fc",
};

const SPANS: readonly Span[] = [
  {
    id: "s0",
    label: "mutation checkout",
    kind: "graphql",
    start: 0,
    width: 100,
    ms: "318ms",
    depth: 0,
  },
  {
    id: "s1",
    label: "api → users-svc · GET /me",
    kind: "rest",
    start: 4,
    width: 11,
    ms: "21ms",
    depth: 1,
  },
  {
    id: "s2",
    label: "users-svc → billing · Charge()",
    kind: "grpc",
    start: 16,
    width: 64,
    ms: "201ms",
    depth: 1,
    slow: true,
  },
  {
    id: "s3",
    label: "billing → db · SELECT account",
    kind: "db",
    start: 20,
    width: 12,
    ms: "9ms",
    depth: 2,
  },
  {
    id: "s4",
    label: "billing → worker · enqueue receipt",
    kind: "job",
    start: 82,
    width: 13,
    ms: "37ms",
    depth: 2,
  },
];

function TraceSection() {
  return (
    <TerminalPane
      command={`trace open ${TRACE_ID}`}
      meta={["mutation checkout", "DURATION 318ms"]}
    >
      <div className="px-6 py-8 sm:px-8">
        <div className="grid gap-8 lg:grid-cols-[1fr_1.7fr] lg:items-start">
          <div>
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
              One incident, one trace
            </span>
            <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-3">
              A single request, every hop a span.
            </h2>
            <p className="text-body text-cc-ink-dim mt-5">
              The p99 spike on{" "}
              <code className="text-cc-ink font-mono">checkout</code> is one
              click from its trace. A single request fans out across your graph
              and the services behind it, every hop a real OpenTelemetry span.
              The same{" "}
              <code className="font-mono" style={{ color: TEAL }}>
                trace {TRACE_ID}
              </code>{" "}
              that surfaced on the tail is stitched straight to the span that is
              actually slow.
            </p>
            <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
              <LegendRow
                kind="graphql"
                text="The GraphQL operation, root of the trace"
              />
              <LegendRow kind="rest" text="A REST hop to users-svc" />
              <LegendRow
                kind="grpc"
                text="The slow gRPC charge to billing"
                highlight
              />
              <LegendRow kind="db" text="A fast database read" />
              <LegendRow
                kind="job"
                text="A background job enqueued for the receipt"
              />
            </ul>
          </div>
          <TraceWaterfall />
        </div>
      </div>
    </TerminalPane>
  );
}

interface LegendRowProps {
  readonly kind: Span["kind"];
  readonly text: string;
  readonly highlight?: boolean;
}

function LegendRow({ kind, text, highlight }: LegendRowProps) {
  return (
    <li className="flex items-center gap-3">
      <span
        className="h-2.5 w-2.5 shrink-0 rounded-[3px]"
        style={{ backgroundColor: KIND_COLOR[kind] }}
      />
      <span className={highlight ? "text-cc-prose" : undefined}>
        {text}
        {highlight && (
          <span
            className="ml-2 rounded px-1.5 py-0.5 font-mono text-[10px] tracking-wide uppercase"
            style={{ color: CORAL, backgroundColor: `${CORAL}1a` }}
          >
            201 ms · 63%
          </span>
        )}
      </span>
    </li>
  );
}

function TraceWaterfall() {
  return (
    <div className="border-cc-card-border/70 bg-cc-code-bg/70 overflow-hidden rounded-xl border">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
        <span className="font-mono text-[11px]" style={{ color: TEAL }}>
          {TRACE_ID}
        </span>
        <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          mutation checkout
        </span>
        <span className="text-cc-nav-label ml-auto inline-flex items-center gap-1.5 font-mono text-[11px]">
          duration <span className="text-cc-heading">318ms</span>
        </span>
      </div>
      <div className="space-y-2.5 px-5 py-5">
        {SPANS.map((span) => (
          <SpanRow key={span.id} span={span} />
        ))}
        <div className="border-cc-card-border/50 text-cc-nav-label mt-5 ml-[40%] flex items-center justify-between border-t pt-2 font-mono text-[10px]">
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
}

function SpanRow({ span }: SpanRowProps) {
  const color = KIND_COLOR[span.kind];
  const isRoot = span.kind === "graphql";
  const barStyle: CSSProperties = {
    left: `${span.start}%`,
    width: `${span.width}%`,
    backgroundColor: span.slow ? CORAL : color,
    opacity: span.slow ? 1 : 0.78,
    boxShadow: span.slow ? `0 0 16px ${CORAL}66` : undefined,
  };
  return (
    <div className="flex items-center gap-3">
      <div
        className="flex w-[40%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: span.depth * 14 }}
      >
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[9px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
        >
          {KIND_LABEL[span.kind]}
        </span>
        <span
          className={`truncate font-mono text-[12px] ${isRoot ? "text-cc-heading" : "text-cc-ink-dim"}`}
        >
          {span.label}
        </span>
      </div>
      <div className="bg-cc-surface/60 relative h-6 flex-1 rounded">
        <div
          className="absolute top-1/2 flex h-4 -translate-y-1/2 items-center rounded-[3px]"
          style={barStyle}
        >
          {span.slow && (
            <span className="text-cc-surface ml-2 font-mono text-[10px] font-semibold">
              billing.Charge() slow
            </span>
          )}
        </div>
        <span
          className="text-cc-nav-label absolute top-1/2 -translate-y-1/2 font-mono text-[10px]"
          style={{ left: `calc(${span.start + span.width}% + 8px)` }}
        >
          {span.ms}
        </span>
      </div>
    </div>
  );
}

/* ================================================================== *
 * TOPOLOGY SNAPSHOT
 * Compact node graph rendered ASCII-adjacent: boxes + mono connectors,
 * showing the services touched by the incident trace. No brand logos.
 * ================================================================== */

function TopologySection() {
  return (
    <TerminalPane
      command={`topo --trace ${TRACE_ID}`}
      meta={["6 SERVICES", "1 HOT HOP"]}
    >
      <div className="grid gap-10 px-6 py-8 sm:px-8 lg:grid-cols-[1fr_1.2fr] lg:items-center">
        <div>
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
            Every .NET service, one trace
          </span>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-3">
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
              The hot hop glows, so the eye lands on cause, not noise
            </CheckLine>
          </ul>
        </div>
        <TopologyGraph />
      </div>
    </TerminalPane>
  );
}

function TopologyGraph() {
  return (
    <div className="border-cc-card-border/70 bg-cc-code-bg/70 rounded-xl border p-5">
      <div className="text-cc-nav-label mb-3 font-mono text-[10px] tracking-wide uppercase">
        service topology · checkout
      </div>
      <svg
        viewBox="0 0 360 300"
        className="h-auto w-full"
        role="img"
        aria-label="Service topology: GraphQL fans out to REST, gRPC, job, and database hops, with the slow gRPC hop to billing highlighted."
      >
        <defs>
          <linearGradient id="hotEdge-v8" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={TEAL} stopOpacity="0.5" />
            <stop offset="100%" stopColor={CORAL} stopOpacity="0.9" />
          </linearGradient>
          <filter id="nodeGlow-v8" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="4" result="b" />
            <feMerge>
              <feMergeNode in="b" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>
        <Edge x1={180} y1={48} x2={84} y2={132} />
        <Edge x1={180} y1={48} x2={180} y2={132} hot />
        <Edge x1={180} y1={48} x2={276} y2={132} />
        <Edge x1={180} y1={132} x2={132} y2={228} />
        <Edge x1={180} y1={132} x2={228} y2={228} />
        <Node x={180} y={48} kind="graphql" label="api" sub="GraphQL" />
        <Node x={84} y={132} kind="rest" label="users-svc" sub="REST" />
        <Node x={180} y={132} kind="grpc" label="billing" sub="gRPC" hot />
        <Node x={276} y={132} kind="job" label="worker" sub="Job" />
        <Node x={132} y={228} kind="db" label="accounts" sub="DB" />
        <Node x={228} y={228} kind="db" label="ledger" sub="DB" />
      </svg>
      <div className="text-cc-nav-label mt-2 flex flex-wrap items-center justify-center gap-x-4 gap-y-1 font-mono text-[10px]">
        <LegendChip kind="graphql" />
        <LegendChip kind="rest" />
        <LegendChip kind="grpc" />
        <LegendChip kind="job" />
        <LegendChip kind="db" />
      </div>
    </div>
  );
}

interface EdgeProps {
  readonly x1: number;
  readonly y1: number;
  readonly x2: number;
  readonly y2: number;
  readonly hot?: boolean;
}

function Edge({ x1, y1, x2, y2, hot }: EdgeProps) {
  return (
    <line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={hot ? "url(#hotEdge-v8)" : "var(--color-cc-card-border)"}
      strokeWidth={hot ? 2 : 1.25}
      strokeDasharray={hot ? undefined : "3 3"}
    />
  );
}

interface NodeProps {
  readonly x: number;
  readonly y: number;
  readonly kind: Span["kind"];
  readonly label: string;
  readonly sub: string;
  readonly hot?: boolean;
}

function Node({ x, y, kind, label, sub, hot }: NodeProps) {
  const color = hot ? CORAL : KIND_COLOR[kind];
  return (
    <g filter={hot ? "url(#nodeGlow-v8)" : undefined}>
      <rect
        x={x - 46}
        y={y - 18}
        width={92}
        height={36}
        rx={8}
        fill="var(--color-cc-surface)"
        stroke={color}
        strokeWidth={hot ? 1.5 : 1}
        strokeOpacity={hot ? 1 : 0.6}
      />
      <circle cx={x - 34} cy={y} r={3} fill={color} />
      <text
        x={x - 24}
        y={y - 1}
        fill="var(--color-cc-heading)"
        fontSize="10"
        fontFamily="var(--font-mono, monospace)"
      >
        {label}
      </text>
      <text
        x={x - 24}
        y={y + 10}
        fill="var(--color-cc-nav-label)"
        fontSize="7.5"
        fontFamily="var(--font-mono, monospace)"
        letterSpacing="0.08em"
      >
        {sub.toUpperCase()}
      </text>
    </g>
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
 * SIGNALS YOU ACTUALLY GET. Three-column checklist, honest copy.
 * ================================================================== */

function SignalsSection() {
  const signals = [
    "p95 / p99 latency per operation",
    "Throughput in requests per minute",
    "Error rate, split by status code",
    "An impact score that ranks what hurts",
    "Client breakdown across published clients",
    "Operation cardinality across the graph",
  ];
  return (
    <TerminalPane command="cat signals.txt" meta={["6 SIGNALS"]}>
      <div className="px-6 py-8 sm:px-8">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          Signals you actually get
        </span>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-3 max-w-2xl">
          The numbers behind the incident, every one of them real.
        </h2>
        <ul className="border-cc-card-border bg-cc-card-border mt-8 grid gap-px overflow-hidden rounded-xl border sm:grid-cols-2 lg:grid-cols-3">
          {signals.map((s) => (
            <li
              key={s}
              className="bg-cc-surface/85 text-body text-cc-ink-dim flex items-start gap-3 px-5 py-5"
            >
              <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
                <CheckIcon size={15} />
              </span>
              <span className="font-mono text-[13px]">{s}</span>
            </li>
          ))}
        </ul>
        <p className="text-caption text-cc-nav-label mt-4">
          Telemetry is configured, not magic: these signals come from
          OpenTelemetry you point at Nitro, a deliberate, documented Nitro
          configuration step.
        </p>
      </div>
    </TerminalPane>
  );
}

/* ================================================================== *
 * HONESTY NOTE. Short mono-flavored panel. No overclaims.
 * ================================================================== */

function HonestyNote() {
  return (
    <TerminalPane command="cat HONESTY.md" meta={["NO OVERCLAIMS"]}>
      <div className="px-6 py-8 sm:px-8">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          Straight about what it is
        </span>
        <h2 className="font-heading text-h4 text-cc-heading mt-3 max-w-2xl">
          Honest about the setup, precise about the payoff.
        </h2>
        <div className="mt-7 grid gap-4 md:grid-cols-3">
          <HonestyCard title="OpenTelemetry-native">
            It is OpenTelemetry end to end. Vendor-neutral spans mean your data
            is yours, with no proprietary agent locking the trace in.
          </HonestyCard>
          <HonestyCard title="The IDE is a separate thing">
            The GraphQL IDE can be served from your Hot Chocolate endpoint. That
            is independent of the telemetry dashboards here. Two facts, kept
            apart.
          </HonestyCard>
          <HonestyCard title="Telemetry is configured">
            The views above come from telemetry you point at Nitro. It is a
            Nitro configuration step, deliberate and documented, not something
            that turns on by itself.
          </HonestyCard>
        </div>
      </div>
    </TerminalPane>
  );
}

interface HonestyCardProps {
  readonly title: string;
  readonly children: ReactNode;
}

function HonestyCard({ title, children }: HonestyCardProps) {
  return (
    <div className="border-cc-card-border/70 bg-cc-code-bg/60 rounded-xl border px-5 py-5">
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
 * CLOSING CTA. Breaks the terminal frame. The single spectrum event.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/80 relative overflow-hidden rounded-2xl border px-6 py-14 text-center sm:px-12">
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
        Ship with the lights on.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Wire your services to OpenTelemetry once and every request becomes
        evidence: ranked by impact, traced end to end, slow span already
        highlighted.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}

/* ================================================================== *
 * Shared primitives
 * ================================================================== */

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
  readonly children: ReactNode;
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
