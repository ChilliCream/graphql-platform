"use client";

import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * Switchback Telemetry.
 *
 * The page is staged as the actual diagnosis path through Nitro: a
 * signal lights up on one side, you cut across to the trace on the
 * other, then back to the client breakdown, across to the service,
 * and down to the proof. Sections alternate hard left and hard right,
 * a single thin teal hairline draws the Z down the page, and each
 * pivot carries a mono milestone tag.
 *
 * Teal is the single accent. Status is rationed as data: green
 * healthy, amber investigating, coral firing. The brand spectrum is
 * spent exactly once, as a 1px top hairline on the closing CTA.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";

const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

const STATUS_COLOR: Record<"ok" | "warn" | "fire", string> = {
  ok: GREEN,
  warn: AMBER,
  fire: CORAL,
};

const SWITCHBACKS: readonly { readonly ord: string; readonly label: string }[] =
  [
    { ord: "01", label: "RANK" },
    { ord: "02", label: "TRACE" },
    { ord: "03", label: "CLIENT" },
    { ord: "04", label: "SERVICE" },
    { ord: "05", label: "PROOF" },
  ];

export function ClientPage() {
  return (
    <main className="relative isolate flex flex-col pb-16">
      <ZigzagSpine />
      <Hero />
      <RankSwitchback />
      <TraceSwitchback />
      <ClientSwitchback />
      <ServiceSwitchback />
      <ProofSwitchback />
      <ClosingCta />
    </main>
  );
}

/* ================================================================== *
 * ZIGZAG SPINE
 * A single full-height SVG behind the main column. A thin 1px polyline
 * in cc-card-border with a faint teal glow draws the Z, with five
 * circular nodes carrying the ordinals. The draw runs once on mount;
 * it is not coupled to scroll.
 * ================================================================== */

function ZigzagSpine() {
  const reduceMotion = useReducedMotion();

  // A normalized Z that kinks across the center guideline. The viewBox
  // is stretched to the full column height with preserveAspectRatio
  // "none" so the path tracks the alternating sections.
  const points = "78,8 22,26 78,44 22,62 78,80 22,98";
  const nodes: readonly { readonly x: number; readonly y: number }[] = [
    { x: 78, y: 8 },
    { x: 22, y: 26 },
    { x: 78, y: 44 },
    { x: 22, y: 62 },
    { x: 78, y: 80 },
  ];

  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0 -z-10 hidden lg:block"
    >
      {/* Lit endpoints: teal at the first switchback, violet at the last. */}
      <div
        className="absolute top-[6%] right-[8%] h-72 w-72 rounded-full opacity-70 blur-3xl"
        style={{
          background: `radial-gradient(50% 50% at 50% 50%, ${TEAL}14 0%, transparent 70%)`,
        }}
      />
      <div
        className="absolute bottom-[14%] left-[8%] h-72 w-72 rounded-full opacity-60 blur-3xl"
        style={{
          background: `radial-gradient(50% 50% at 50% 50%, ${VIOLET}10 0%, transparent 70%)`,
        }}
      />
      <svg
        className="absolute inset-0 h-full w-full"
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
      >
        <defs>
          <filter
            id="v8-spine-glow"
            x="-20%"
            y="-20%"
            width="140%"
            height="140%"
          >
            <feDropShadow
              dx="0"
              dy="0"
              stdDeviation="0.6"
              floodColor={TEAL}
              floodOpacity="0.5"
            />
          </filter>
        </defs>
        <motion.polyline
          points={points}
          fill="none"
          stroke="rgba(245, 241, 234, 0.12)"
          strokeWidth="0.18"
          strokeLinejoin="round"
          vectorEffect="non-scaling-stroke"
          filter="url(#v8-spine-glow)"
          initial={reduceMotion ? false : { pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ duration: 1.2, ease: "easeOut" }}
        />
      </svg>
      {/* Nodes carry the ordinals at each kink. Positioned in percentages so
          they sit on the polyline regardless of column height. */}
      {nodes.map((n, i) => (
        <span
          key={SWITCHBACKS[i].ord}
          className="absolute flex h-7 w-7 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full font-mono text-[10px]"
          style={{
            left: `${n.x}%`,
            top: `${n.y}%`,
            backgroundColor: "var(--color-cc-surface)",
            border: `1px solid ${TEAL}66`,
            color: TEAL,
            boxShadow: `0 0 12px ${TEAL}26`,
          }}
        >
          {SWITCHBACKS[i].ord}
        </span>
      ))}
    </div>
  );
}

/* ================================================================== *
 * HERO
 * Centered, full width. Eyebrow, the switchback headline, the lead,
 * dual CTA, and a thin compass strip that previews the five labels of
 * the path the page is about to walk.
 * ================================================================== */

function Hero() {
  return (
    <section className="relative isolate px-6 pt-10 pb-20">
      <div className="relative mx-auto max-w-3xl text-center">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          Nitro observability
        </span>
        <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6">
          Follow the signal,
          <br />
          switchback by switchback.
        </h1>
        <p className="lead text-cc-prose mx-auto mt-6 max-w-2xl">
          OpenTelemetry-native GraphQL observability for .NET. A signal lights
          up, you cut across to the trace, back to the client breakdown, then
          across to the service it touches. Nitro stages the whole diagnosis as
          one path.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
          <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
            Get Started
          </SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
        <CompassStrip />
      </div>
    </section>
  );
}

function CompassStrip() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 inline-flex flex-wrap items-center justify-center gap-x-4 gap-y-2 rounded-full border px-5 py-2.5 backdrop-blur-md">
      {SWITCHBACKS.map((s, i) => (
        <span
          key={s.label}
          className="text-cc-nav-label flex items-center gap-2 font-mono text-[11px]"
        >
          <span className="text-cc-ink-faint">{s.ord}</span>
          <span className="tracking-wide uppercase" style={{ color: TEAL }}>
            {s.label}
          </span>
          {i < SWITCHBACKS.length - 1 && (
            <span className="text-cc-ink-faint pl-2">·</span>
          )}
        </span>
      ))}
    </div>
  );
}

/* ================================================================== *
 * SWITCHBACK FRAME
 * Every section is a 12-col grid at lg. Odd switchbacks place the
 * narrative in cols 1-5 and the artifact in cols 7-12; even ones flip
 * it. The narrative fades and slides 12px from its own side on first
 * viewport entry, once. On mobile the grid collapses to one column.
 * ================================================================== */

interface SwitchbackProps {
  readonly ord: string;
  readonly label: string;
  readonly side: "left" | "right";
  readonly narrative: ReactNode;
  readonly artifact: ReactNode;
}

function Switchback({
  ord,
  label,
  side,
  narrative,
  artifact,
}: SwitchbackProps) {
  const reduceMotion = useReducedMotion();
  const fromX = side === "left" ? -12 : 12;

  const narrativeBlock = (
    <motion.div
      className={
        side === "left"
          ? "lg:col-span-5 lg:col-start-1"
          : "lg:col-span-5 lg:col-start-8"
      }
      initial={reduceMotion ? false : { opacity: 0, x: fromX }}
      whileInView={{ opacity: 1, x: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.5, ease: "easeOut" }}
    >
      <Milestone ord={ord} label={label} />
      {narrative}
    </motion.div>
  );

  const artifactBlock = (
    <div
      className={
        side === "left"
          ? "lg:col-span-6 lg:col-start-7"
          : "lg:col-span-6 lg:col-start-1 lg:row-start-1"
      }
    >
      {artifact}
    </div>
  );

  return (
    <section className="px-6 py-14 lg:py-20">
      <div className="mx-auto grid max-w-5xl items-center gap-10 lg:grid-cols-12 lg:gap-x-12">
        {narrativeBlock}
        {artifactBlock}
      </div>
    </section>
  );
}

interface MilestoneProps {
  readonly ord: string;
  readonly label: string;
}

function Milestone({ ord, label }: MilestoneProps) {
  return (
    <div className="mb-5 flex items-center gap-2.5">
      <span
        className="font-mono text-[11px]"
        style={{ color: TEAL, letterSpacing: "0.12em" }}
      >
        {ord}
      </span>
      <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.28em] uppercase">
        {label}
      </span>
      <span className="bg-cc-card-border h-px flex-1" />
    </div>
  );
}

/* ================================================================== *
 * 01 RANK
 * Narrative left, artifact right. Impact-ranked operations: p95/p99,
 * throughput, error rate, with the top row flagged coral.
 * ================================================================== */

interface OpRow {
  readonly rank: number;
  readonly name: string;
  readonly p95: string;
  readonly p99: string;
  readonly rpm: string;
  readonly errRate: string;
  readonly impact: number;
  readonly status: "ok" | "warn" | "fire";
}

const OP_ROWS: readonly OpRow[] = [
  {
    rank: 1,
    name: "checkout",
    p95: "42ms",
    p99: "318ms",
    rpm: "9.4k",
    errRate: "0.3%",
    impact: 94,
    status: "fire",
  },
  {
    rank: 2,
    name: "cartSummary",
    p95: "31ms",
    p99: "88ms",
    rpm: "12.1k",
    errRate: "0.1%",
    impact: 71,
    status: "warn",
  },
  {
    rank: 3,
    name: "productList",
    p95: "12ms",
    p99: "27ms",
    rpm: "18.6k",
    errRate: "0.0%",
    impact: 38,
    status: "ok",
  },
  {
    rank: 4,
    name: "userProfile",
    p95: "8ms",
    p99: "19ms",
    rpm: "5.2k",
    errRate: "0.0%",
    impact: 22,
    status: "ok",
  },
];

function RankSwitchback() {
  return (
    <Switchback
      ord="01"
      label="Rank"
      side="left"
      narrative={
        <div>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3">
            Ranked by what hurts, not what is loud.
          </h2>
          <p className="text-body text-cc-ink-dim mt-5">
            The Nitro impact score folds p95, p99, throughput, and error rate
            into one number, so the operation at the top of the table is the one
            worth opening first. That top row is where the switchback begins.
          </p>
          <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
            <CheckLine>
              p95 and p99 latency with real distributions, not averages
            </CheckLine>
            <CheckLine>
              Throughput and error rate as siblings, never separated
            </CheckLine>
            <CheckLine>
              Impact score surfaces the operation that owes you attention
            </CheckLine>
          </ul>
        </div>
      }
      artifact={<OperationsTable />}
    />
  );
}

function OperationsTable() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md">
      <CardHeader label="nitro · operations" right="sorted by impact" dots />
      <div className="text-cc-nav-label border-cc-card-border/50 grid grid-cols-[28px_1fr_56px_56px_56px_56px_60px] gap-3 border-b px-4 py-2.5 font-mono text-[10px] tracking-wide uppercase">
        <span />
        <span>operation</span>
        <span className="text-right">p95</span>
        <span className="text-right">p99</span>
        <span className="text-right">rpm</span>
        <span className="text-right">err</span>
        <span className="text-right">impact</span>
      </div>
      <div className="divide-cc-card-border/40 divide-y">
        {OP_ROWS.map((row) => (
          <OperationRow key={row.name} row={row} />
        ))}
      </div>
    </div>
  );
}

interface OperationRowProps {
  readonly row: OpRow;
}

function OperationRow({ row }: OperationRowProps) {
  const isHot = row.status === "fire";
  return (
    <div
      className={`grid grid-cols-[28px_1fr_56px_56px_56px_56px_60px] items-center gap-3 px-4 py-3 font-mono text-[12px] ${
        isHot ? "bg-cc-surface/80" : "bg-cc-surface/40"
      }`}
      style={isHot ? { boxShadow: `inset 0 0 0 1px ${CORAL}33` } : undefined}
    >
      <span className="text-cc-nav-label text-[11px]">#{row.rank}</span>
      <span className="flex items-center gap-2">
        <StatusDot
          color={STATUS_COLOR[row.status]}
          pulse={row.status === "fire"}
        />
        <span className={isHot ? "text-cc-heading" : "text-cc-ink-dim"}>
          {row.name}
        </span>
      </span>
      <span className="text-cc-ink-dim text-right">{row.p95}</span>
      <span
        className="text-right"
        style={{ color: isHot ? CORAL : "var(--color-cc-ink-dim)" }}
      >
        {row.p99}
      </span>
      <span className="text-cc-ink-dim text-right">{row.rpm}</span>
      <span
        className="text-right"
        style={{
          color:
            row.status === "fire"
              ? CORAL
              : row.status === "warn"
                ? AMBER
                : "var(--color-cc-ink-dim)",
        }}
      >
        {row.errRate}
      </span>
      <span className="flex items-center justify-end gap-1.5">
        <ImpactBar value={row.impact} status={row.status} />
        <span className="text-cc-ink-dim w-6 text-right text-[11px]">
          {row.impact}
        </span>
      </span>
    </div>
  );
}

interface ImpactBarProps {
  readonly value: number;
  readonly status: OpRow["status"];
}

function ImpactBar({ value, status }: ImpactBarProps) {
  return (
    <span className="bg-cc-surface/80 relative inline-block h-1.5 w-12 overflow-hidden rounded-full">
      <span
        className="absolute inset-y-0 left-0 rounded-full"
        style={{ width: `${value}%`, backgroundColor: STATUS_COLOR[status] }}
      />
    </span>
  );
}

/* ================================================================== *
 * 02 TRACE
 * Artifact left, narrative right. A trimmed distributed-trace
 * waterfall for the #1 operation, with the slow gRPC span highlighted.
 * ================================================================== */

interface Span {
  readonly id: string;
  readonly label: string;
  readonly kind: "graphql" | "rest" | "grpc" | "job" | "db";
  readonly start: number;
  readonly width: number;
  readonly ms: string;
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
  },
  {
    id: "s1",
    label: "api → users-svc · GET /me",
    kind: "rest",
    start: 4,
    width: 11,
    ms: "21ms",
  },
  {
    id: "s2",
    label: "users-svc → billing · Charge()",
    kind: "grpc",
    start: 16,
    width: 64,
    ms: "201ms",
    slow: true,
  },
  {
    id: "s3",
    label: "billing → db · SELECT account",
    kind: "db",
    start: 20,
    width: 12,
    ms: "9ms",
  },
  {
    id: "s4",
    label: "billing → worker · enqueue receipt",
    kind: "job",
    start: 82,
    width: 13,
    ms: "37ms",
  },
];

const SPAN_DEPTH: readonly number[] = [0, 1, 1, 2, 2];

function TraceSwitchback() {
  return (
    <Switchback
      ord="02"
      label="Trace"
      side="right"
      narrative={
        <div>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3">
            One click from the impact row to the slow span.
          </h2>
          <p className="text-body text-cc-ink-dim mt-5">
            Cut across from the top of the leaderboard and Nitro opens the
            trace. Every hop is a real OpenTelemetry span: GraphQL at the root,
            REST and gRPC across your services, the database read, the
            background job enqueued at the end. The hop that is actually slow is
            highlighted.
          </p>
          <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
            <CheckLine>
              Trace waterfall with span attributes, properties, and stack frames
            </CheckLine>
            <CheckLine>
              The slow span is marked, not buried below averages
            </CheckLine>
            <CheckLine>
              Open standard underneath, the spans are your data
            </CheckLine>
          </ul>
        </div>
      }
      artifact={<TraceWaterfall />}
    />
  );
}

function TraceWaterfall() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
        <span className="font-mono text-[11px]" style={{ color: TEAL }}>
          4b1c8f2a9e07
        </span>
        <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          mutation checkout
        </span>
        <span className="text-cc-nav-label ml-auto inline-flex items-center gap-1.5 font-mono text-[11px]">
          duration <span className="text-cc-heading">318ms</span>
        </span>
      </div>
      <div className="px-5 py-5">
        <div className="space-y-2.5">
          {SPANS.map((span, i) => (
            <SpanRow key={span.id} span={span} depth={SPAN_DEPTH[i] ?? 0} />
          ))}
        </div>
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
}

function SpanRow({ span, depth }: SpanRowProps) {
  const color = KIND_COLOR[span.kind];
  const isRoot = span.kind === "graphql";
  return (
    <div className="flex items-center gap-3">
      <div
        className="flex w-[38%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: depth * 14 }}
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
          style={{
            left: `${span.start}%`,
            width: `${span.width}%`,
            backgroundColor: span.slow ? CORAL : color,
            opacity: span.slow ? 1 : 0.78,
            boxShadow: span.slow ? `0 0 16px ${CORAL}66` : undefined,
          }}
        >
          {span.slow && (
            <span className="text-cc-surface ml-2 font-mono text-[10px] font-semibold">
              billing.Charge()
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
 * 03 CLIENT
 * Narrative left, artifact right. Per-client / per-version visibility
 * for the published clients affected.
 * ================================================================== */

interface ClientRow {
  readonly name: string;
  readonly share: number;
  readonly rpm: string;
  readonly status: "ok" | "warn" | "fire";
}

const CLIENT_ROWS: readonly ClientRow[] = [
  { name: "web-storefront@4.2.0", share: 61, rpm: "5.7k", status: "fire" },
  { name: "ios-app@3.8.1", share: 27, rpm: "2.5k", status: "warn" },
  { name: "android-app@3.5.0", share: 9, rpm: "0.8k", status: "ok" },
];

function ClientSwitchback() {
  return (
    <Switchback
      ord="03"
      label="Client"
      side="left"
      narrative={
        <div>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3">
            Which published clients call this, and how often.
          </h2>
          <p className="text-body text-cc-ink-dim mt-5">
            The same telemetry that fuels the leaderboard breaks down by caller.
            Nitro registers your clients by name and version, so the published
            clients affected are on one tile, with the versions still out there
            hitting the deprecated field.
          </p>
          <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
            <CheckLine>
              Per-client share of traffic, latency, and errors
            </CheckLine>
            <CheckLine>
              Version-aware: storefront@4.2.0 vs storefront@4.1.x, side by side
            </CheckLine>
            <CheckLine>
              Ties cleanly into the schema registry for deprecation work
            </CheckLine>
          </ul>
        </div>
      }
      artifact={<ClientShareTile />}
    />
  );
}

function ClientShareTile() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md">
      <CardHeader label="clients · checkout · 1h" right="nitro" />
      <div className="px-5 py-5">
        <div className="flex h-2 overflow-hidden rounded-full">
          {CLIENT_ROWS.map((c) => (
            <span
              key={c.name}
              style={{
                width: `${c.share}%`,
                backgroundColor: STATUS_COLOR[c.status],
                opacity: c.status === "ok" ? 0.55 : 1,
              }}
            />
          ))}
        </div>
        <div className="mt-5 space-y-2">
          {CLIENT_ROWS.map((c) => (
            <div
              key={c.name}
              className="flex items-center gap-3 rounded-lg px-2.5 py-2"
              style={{
                backgroundColor:
                  c.status === "fire"
                    ? "rgba(240, 120, 106, 0.07)"
                    : "rgba(12, 19, 34, 0.4)",
              }}
            >
              <StatusDot color={STATUS_COLOR[c.status]} />
              <span
                className={`flex-1 truncate font-mono text-[12px] ${
                  c.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
                }`}
              >
                {c.name}
              </span>
              <ShareBar share={c.share} status={c.status} />
              <span className="text-cc-nav-label w-12 text-right font-mono text-[11px]">
                {c.share}%
              </span>
              <span className="text-cc-ink-dim w-14 text-right font-mono text-[11px]">
                {c.rpm}
              </span>
            </div>
          ))}
        </div>
        <p className="text-cc-nav-label mt-4 font-mono text-[11px]">
          Drill into any client to see which operations and which versions of
          the schema it touches.
        </p>
      </div>
    </div>
  );
}

interface ShareBarProps {
  readonly share: number;
  readonly status: ClientRow["status"];
}

function ShareBar({ share, status }: ShareBarProps) {
  return (
    <span className="bg-cc-surface/80 relative inline-block h-1.5 w-20 overflow-hidden rounded-full">
      <span
        className="absolute inset-y-0 left-0 rounded-full"
        style={{ width: `${share}%`, backgroundColor: STATUS_COLOR[status] }}
      />
    </span>
  );
}

/* ================================================================== *
 * 04 SERVICE
 * Artifact left, narrative right. A 2x2 service-kind tile grid for the
 * .NET services the trace touches, plus the one-pipeline narrative.
 * ================================================================== */

interface ServiceKind {
  readonly key: Span["kind"];
  readonly label: string;
  readonly note: string;
  readonly count: string;
}

const SERVICE_KINDS: readonly ServiceKind[] = [
  {
    key: "graphql",
    label: "GraphQL",
    note: "Hot Chocolate, source-generated",
    count: "1 gateway",
  },
  {
    key: "rest",
    label: "REST APIs",
    note: "ASP.NET Core endpoints",
    count: "6 services",
  },
  {
    key: "grpc",
    label: "gRPC",
    note: "service-to-service calls",
    count: "4 services",
  },
  {
    key: "job",
    label: "Background jobs",
    note: "queued work, scheduled tasks",
    count: "3 workers",
  },
];

function ServiceSwitchback() {
  return (
    <Switchback
      ord="04"
      label="Service"
      side="right"
      narrative={
        <div>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3">
            One pane for every .NET service the trace touches.
          </h2>
          <p className="text-body text-cc-ink-dim mt-5">
            The OpenTelemetry pipeline is the same across GraphQL, REST, gRPC,
            and your background workers, so a single trace can carry the whole
            call. Nitro ingests it, ranks it, and lets you ask the operation and
            the service the same questions.
          </p>
          <p className="text-body text-cc-ink-dim mt-4">
            Wire your services through{" "}
            <code className="text-cc-ink font-mono">
              ChilliCream.Nitro.OpenTelemetry
            </code>
            . The tiles show every kind side by side, no proprietary agent in
            the middle.
          </p>
        </div>
      }
      artifact={
        <div className="grid gap-3 sm:grid-cols-2">
          {SERVICE_KINDS.map((s) => (
            <ServiceKindTile key={s.key} kind={s} />
          ))}
        </div>
      }
    />
  );
}

interface ServiceKindTileProps {
  readonly kind: ServiceKind;
}

function ServiceKindTile({ kind }: ServiceKindTileProps) {
  const color = KIND_COLOR[kind.key];
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border px-4 py-4 backdrop-blur-md">
      <div className="flex items-center justify-between">
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
        >
          {kind.label}
        </span>
        <span className="text-cc-nav-label font-mono text-[10px]">
          {kind.count}
        </span>
      </div>
      <p className="text-cc-ink-dim mt-3 text-[13px]">{kind.note}</p>
      <div className="bg-cc-surface/60 mt-3 h-1 overflow-hidden rounded-full">
        <span
          className="block h-full"
          style={{ width: "100%", backgroundColor: color, opacity: 0.5 }}
        />
      </div>
    </div>
  );
}

/* ================================================================== *
 * 05 PROOF
 * Narrative left (an honesty band of three check-led cards), artifact
 * right (a compact wire-up card with the OTel exporter snippet).
 * ================================================================== */

function ProofSwitchback() {
  return (
    <Switchback
      ord="05"
      label="Proof"
      side="left"
      narrative={
        <div>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3">
            Honest about the setup, precise about the payoff.
          </h2>
          <div className="mt-7 space-y-4">
            <ProofCard title="Telemetry needs Nitro configuration">
              The dashboards above come from telemetry you point at Nitro. It is
              a configuration step in your services, deliberate and documented,
              not something that turns on by itself.
            </ProofCard>
            <ProofCard title="OpenTelemetry, end to end">
              Vendor-neutral spans mean your data is yours, and there is no
              proprietary agent locking the trace in.
            </ProofCard>
            <ProofCard title="The GraphQL IDE is a separate thing">
              The GraphQL IDE serves from your Hot Chocolate endpoint. That is
              independent of the telemetry dashboards here. Two facts, kept
              apart.
            </ProofCard>
          </div>
        </div>
      }
      artifact={<WireUpCard />}
    />
  );
}

interface ProofCardProps {
  readonly title: string;
  readonly children: ReactNode;
}

function ProofCard({ title, children }: ProofCardProps) {
  return (
    <div className="border-cc-card-border/70 bg-cc-surface/50 rounded-xl border px-5 py-4">
      <div className="flex items-center gap-2">
        <span style={{ color: TEAL }}>
          <CheckIcon size={15} />
        </span>
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
      </div>
      <p className="text-caption text-cc-ink-dim mt-2.5">{children}</p>
    </div>
  );
}

function WireUpCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md">
      <CardHeader label="Program.cs · wire-up" right="otel" dots />
      <pre className="overflow-x-auto px-5 py-5 font-mono text-[12px] leading-relaxed">
        <code>
          <CodeLine>
            <Tok c={VIOLET}>builder</Tok>.<Tok c={TEAL}>Services</Tok>
          </CodeLine>
          <CodeLine indent={1}>
            .<Tok c={TEAL}>AddOpenTelemetry</Tok>()
          </CodeLine>
          <CodeLine indent={2}>
            .<Tok c={TEAL}>WithTracing</Tok>(<Tok c={VIOLET}>t</Tok> =&gt;{" "}
            <Tok c={VIOLET}>t</Tok>
          </CodeLine>
          <CodeLine indent={3}>
            .<Tok c={TEAL}>AddHotChocolateInstrumentation</Tok>()
          </CodeLine>
          <CodeLine indent={3}>
            .<Tok c={TEAL}>AddAspNetCoreInstrumentation</Tok>()
          </CodeLine>
          <CodeLine indent={3}>
            .<Tok c={TEAL}>AddGrpcClientInstrumentation</Tok>()
          </CodeLine>
          <CodeLine indent={3}>
            .<Tok c={TEAL}>AddNitroExporter</Tok>());
          </CodeLine>
        </code>
      </pre>
      <div className="border-cc-card-border/60 border-t px-5 py-3">
        <p className="text-cc-nav-label font-mono text-[11px]">
          One pipeline, every kind. The exporter ships spans to Nitro.
        </p>
      </div>
    </div>
  );
}

interface CodeLineProps {
  readonly indent?: number;
  readonly children: ReactNode;
}

function CodeLine({ indent = 0, children }: CodeLineProps) {
  return (
    <div className="text-cc-ink-dim" style={{ paddingLeft: indent * 14 }}>
      {children}
    </div>
  );
}

interface TokProps {
  readonly c: string;
  readonly children: ReactNode;
}

function Tok({ c, children }: TokProps) {
  return <span style={{ color: c }}>{children}</span>;
}

/* ================================================================== *
 * CLOSING CTA
 * Breaks the zigzag, centered. The single brand-spectrum event for the
 * page is the 1px top hairline. Summary line ties the five switchbacks
 * into one sentence about turning signals into evidence.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="px-6 pt-10">
      <div className="border-cc-card-border bg-cc-surface/80 relative mx-auto max-w-5xl overflow-hidden rounded-2xl border px-6 py-14 text-center backdrop-blur-md sm:px-12">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          Nitro observability
        </span>
        <h2 className="font-heading text-h3 text-cc-heading sm:text-h2 mt-5">
          Signals become evidence.
        </h2>
        <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-xl">
          Rank, trace, client, service, proof: five switchbacks, one path. Point
          OpenTelemetry at Nitro once and every request turns into evidence,
          ranked by impact and traced end to end.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
          <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
            Get Started
          </SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* ================================================================== *
 * Shared primitives
 * ================================================================== */

interface CardHeaderProps {
  readonly label: string;
  readonly right?: string;
  readonly dots?: boolean;
}

function CardHeader({ label, right, dots }: CardHeaderProps) {
  return (
    <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center gap-2 border-b px-4 py-2.5">
      {dots && (
        <>
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        </>
      )}
      <span
        className={`text-cc-nav-label font-mono text-[11px] tracking-wide uppercase ${dots ? "ml-2" : ""}`}
      >
        {label}
      </span>
      {right && (
        <span className="text-cc-nav-label ml-auto font-mono text-[10px] tracking-wide uppercase">
          {right}
        </span>
      )}
    </div>
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
