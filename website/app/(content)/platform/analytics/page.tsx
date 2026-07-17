import type { CSSProperties, ReactNode } from "react";

import Link from "next/link";

import { ArrowLink } from "@/src/components/ArrowLink";
import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CheckList } from "@/src/components/CheckList";
import { PatternBand } from "@/src/components/PatternBand";
import { SectionHeading } from "@/src/components/SectionHeading";
import { StatStrip } from "@/src/components/StatStrip";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Tag } from "@/src/design-system/Tag";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import {
  HBarSeries,
  InsightsTable,
  LineAreaChart,
  NitroDiagnose,
  NitroTheme,
  NitroTrace,
  TraceWaterfall,
} from "@/src/nitro";
import type { Client, InsightRow, Trace } from "@/src/nitro/lib/data/types";

export const metadata = pageMetadata({
  title: "Analytics",
  description:
    "OpenTelemetry-native analytics for your whole backend: operation insights, impact score, per-client usage, distributed traces, and service monitoring across GraphQL, REST, gRPC, and background jobs.",
  path: "/platform/analytics",
  keywords: [
    "API analytics",
    "OpenTelemetry analytics",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "per-client usage",
    "operation monitoring",
    "REST gRPC monitoring",
    ".NET observability",
    "Nitro",
  ],
});

/* ----------------------------------------------------------------------------
   Scene palette. teal #5eead4 is the signature; status semantics are rationed
   as data and carried by the charts, not the prose:
     green  #34d399  healthy
     amber  #fbbf24  warning
     coral  #f0786a  error
---------------------------------------------------------------------------- */
const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";

type Status = "healthy" | "warning" | "error";

const STATUS_COLOR: Record<Status, string> = {
  healthy: GREEN,
  warning: AMBER,
  error: CORAL,
};

/* ============================================================================
   Status primitives — page-specific product signals (no house equivalent).
============================================================================ */

interface StatusDotProps {
  readonly status: Status;
  readonly pulse?: boolean;
}

function StatusDot({ status, pulse = false }: StatusDotProps) {
  const color = STATUS_COLOR[status];
  return (
    <span className="relative inline-flex h-2 w-2 shrink-0">
      {pulse && (
        <span
          className="absolute inline-flex h-full w-full rounded-full opacity-60 motion-safe:animate-ping"
          style={{ backgroundColor: color }}
          aria-hidden
        />
      )}
      <span
        className="relative inline-flex h-2 w-2 rounded-full"
        style={{ backgroundColor: color, boxShadow: `0 0 8px ${color}aa` }}
      />
    </span>
  );
}

interface StatusBadgeProps {
  readonly status: Status;
  readonly label: string;
}

function StatusBadge({ status, label }: StatusBadgeProps) {
  const color = STATUS_COLOR[status];
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.6rem] tracking-[0.1em] uppercase"
      style={{
        color,
        borderColor: `${color}55`,
        backgroundColor: `${color}14`,
      }}
    >
      <StatusDot status={status} pulse={status !== "healthy"} />
      {label}
    </span>
  );
}

interface LegendDotProps {
  readonly status: Status;
  readonly label: string;
}

function LegendDot({ status, label }: LegendDotProps) {
  return (
    <span className="flex items-center gap-2">
      <StatusDot status={status} pulse={status !== "healthy"} />
      <span className="text-cc-ink-dim font-mono text-[0.66rem] tracking-wide">
        {label}
      </span>
    </span>
  );
}

/* ----------------------------------------------------------------------------
   Local helpers built from house parts.
---------------------------------------------------------------------------- */

interface NitroFrameProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: CSSProperties;
  /** "always" renders the chart at its final static frame (no draw-in). */
  readonly reducedMotion?: "user" | "always" | "never";
}

/** Wraps chart primitives so their `--t-*` token vars resolve; stays transparent. */
function NitroFrame({
  children,
  className,
  style,
  reducedMotion = "never",
}: NitroFrameProps) {
  return (
    <NitroTheme
      theme="dark"
      reducedMotion={reducedMotion}
      className={className}
      style={{ background: "transparent", ...style }}
    >
      {children}
    </NitroTheme>
  );
}

interface ChartTileProps {
  readonly title: string;
  readonly hint?: string;
  readonly glow?: boolean;
  readonly children: ReactNode;
}

/** House Card tile with a title/hint header row, wrapping a product chart. */
function ChartTile({ title, hint, glow = false, children }: ChartTileProps) {
  return (
    <Card variant="tile" glow={glow}>
      <div className="flex items-baseline justify-between gap-3">
        <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
        {hint && <Eyebrow size="2xs">{hint}</Eyebrow>}
      </div>
      <div className="mt-4">{children}</div>
    </Card>
  );
}

interface FeatureRowProps {
  readonly title: ReactNode;
  readonly body: ReactNode;
  readonly visual: ReactNode;
  readonly reverse?: boolean;
  readonly children?: ReactNode;
}

/** One claim, one product visual — alternating split row. */
function FeatureRow({
  title,
  body,
  visual,
  reverse = false,
  children,
}: FeatureRowProps) {
  return (
    <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-16">
      <div
        className={["min-w-0 lg:col-span-5", reverse ? "lg:order-2" : ""].join(
          " ",
        )}
      >
        <SectionHeading title={title} description={body} />
        {children}
      </div>
      <div
        className={["min-w-0 lg:col-span-7", reverse ? "lg:order-1" : ""].join(
          " ",
        )}
      >
        {visual}
      </div>
    </div>
  );
}

interface ChapterBandProps {
  readonly title: ReactNode;
  readonly description: ReactNode;
  /** Outer top spacing, e.g. "mt-20 sm:mt-28". */
  readonly className?: string;
}

/**
 * Full-bleed "chapter" opener: the hero's faint grid backdrop plus a soft teal
 * glow, breaking out of the centered content column to punctuate the page
 * between the graphic-heavy feature rows. Wraps a centered SectionHeading.
 */
function ChapterBand({ title, description, className = "" }: ChapterBandProps) {
  return (
    <div
      className={`border-cc-card-border/50 bg-cc-surface/25 relative left-1/2 w-screen -translate-x-1/2 overflow-x-clip border-y py-16 text-center sm:py-24 ${className}`}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.07]"
        style={{
          backgroundImage:
            "linear-gradient(rgba(245,241,234,1) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,1) 1px, transparent 1px)",
          backgroundSize: "46px 46px",
          maskImage:
            "radial-gradient(80% 130% at 50% 45%, #000 35%, transparent 82%)",
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(60% 90% at 50% 40%, rgba(94,234,212,0.08), transparent 65%)",
        }}
      />
      <div className="mx-auto max-w-3xl px-5 sm:px-12">
        <SectionHeading
          align="center"
          size="lg"
          title={title}
          description={description}
        />
      </div>
    </div>
  );
}

interface StepPathProps {
  readonly steps: readonly string[];
  readonly className?: string;
}

/** A mono "a → b → c" path joined by the ArrowRight icon; the final step is the
 *  destination, tinted coral. */
function StepPath({ steps, className = "" }: StepPathProps) {
  return (
    <p
      className={`text-cc-ink-dim flex flex-wrap items-center gap-2 font-mono text-[0.66rem] tracking-wide ${className}`}
    >
      {steps.map((step, i) => (
        <span key={step} className="flex items-center gap-2">
          {i > 0 && <ArrowRightIcon className="text-cc-nav-label size-3" />}
          <span style={i === steps.length - 1 ? { color: CORAL } : undefined}>
            {step}
          </span>
        </span>
      ))}
    </p>
  );
}

/* ============================================================================
   Shared kind labels — used by the full-OTel statement band.
============================================================================ */

const TRACE_ID = "7f3a·9b2e·c1";

type ChipKind = "graphql" | "rest" | "grpc" | "job" | "db";

const KIND_LABEL: Record<ChipKind, string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  job: "job",
  db: "DB",
};

/* ============================================================================
   Shared trace fixture — the hero waterfall reads from a checkout incident.
============================================================================ */

const CHECKOUT_TRACE: Trace = {
  totalMs: 318,
  spans: [
    {
      id: "s1",
      name: "POST /graphql",
      kind: "server",
      startMs: 0,
      durationMs: 318,
      depth: 0,
    },
    {
      id: "s2",
      name: "mutation checkout",
      kind: "graphql",
      startMs: 4,
      durationMs: 306,
      depth: 1,
    },
    {
      id: "s3",
      name: "users-svc · GET /me",
      kind: "http",
      startMs: 16,
      durationMs: 44,
      depth: 2,
    },
    {
      id: "s4",
      name: "billing · Charge",
      kind: "http",
      startMs: 67,
      durationMs: 204,
      depth: 2,
    },
    {
      id: "s5",
      name: "worker · receipt.enqueue",
      kind: "internal",
      startMs: 159,
      durationMs: 58,
      depth: 2,
    },
    {
      id: "s6",
      name: "orders.db · INSERT",
      kind: "internal",
      startMs: 271,
      durationMs: 38,
      depth: 2,
    },
  ],
};

/* ============================================================================
   HERO — spotlight mesh, oversized headline, the signature incident artifact
   floating over a trace waterfall, stitched by one trace-id.
============================================================================ */

const SPIKE_POINTS = [
  18, 21, 19, 24, 22, 26, 23, 28, 31, 27, 34, 30, 41, 52, 71, 96, 102, 88,
];

interface AreaChartProps {
  readonly points: readonly number[];
  readonly stroke: string;
  readonly fill: string;
  readonly id: string;
  readonly height?: number;
}

function AreaChart({ points, stroke, fill, id, height = 64 }: AreaChartProps) {
  const width = 240;
  const max = Math.max(...points);
  const min = Math.min(...points);
  const span = max - min || 1;
  const step = width / (points.length - 1);
  const coords = points.map((p, i) => {
    const x = i * step;
    const y = height - ((p - min) / span) * (height - 8) - 4;
    return [x, y] as const;
  });
  const line = coords
    .map(([x, y], i) => `${i === 0 ? "M" : "L"}${x.toFixed(1)},${y.toFixed(1)}`)
    .join(" ");
  const area = `${line} L${width},${height} L0,${height} Z`;
  const last = coords[coords.length - 1];
  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      width="100%"
      height={height}
      preserveAspectRatio="none"
      aria-hidden
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={fill} stopOpacity="0.42" />
          <stop offset="100%" stopColor={fill} stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={area} fill={`url(#${id})`} />
      <path d={line} fill="none" stroke={stroke} strokeWidth="1.75" />
      <circle cx={last[0]} cy={last[1]} r="3" fill={stroke} />
    </svg>
  );
}

interface MiniMetricProps {
  readonly label: string;
  readonly value: string;
  readonly tone?: string;
}

function MiniMetric({ label, value, tone }: MiniMetricProps) {
  return (
    <div>
      <p className="text-cc-nav-label font-mono text-[0.56rem] tracking-[0.1em] uppercase">
        {label}
      </p>
      <p
        className="text-cc-heading mt-0.5 font-mono text-sm font-semibold tabular-nums"
        style={tone ? { color: tone } : undefined}
      >
        {value}
      </p>
    </div>
  );
}

function IncidentArtifact() {
  return (
    <div className="relative">
      {/* layered glow behind the floating tile */}
      <div
        className="pointer-events-none absolute -inset-6 -z-10 rounded-[2.5rem] opacity-70 blur-3xl"
        style={{
          background:
            "radial-gradient(55% 55% at 60% 25%, rgba(94,234,212,0.22), transparent 70%), radial-gradient(50% 50% at 30% 90%, rgba(240,120,106,0.16), transparent 70%)",
        }}
        aria-hidden
      />

      {/* Floating dashboard tile, mid-incident */}
      <div className="border-cc-card-border bg-cc-surface/95 relative z-20 mx-auto max-w-md rounded-2xl border shadow-[0_30px_70px_-30px_rgba(0,0,0,0.8)] backdrop-blur">
        <div className="border-cc-card-border flex items-center justify-between border-b px-5 py-2.5">
          <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.12em] uppercase">
            operation · checkout
          </span>
          <StatusBadge status="warning" label="Warning" />
        </div>
        <div className="p-5">
          <div className="flex items-start justify-between">
            <div>
              <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                operation
              </p>
              <p className="text-cc-heading mt-0.5 font-mono text-sm">
                mutation checkout
              </p>
            </div>
            <div className="text-right">
              <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                p99
              </p>
              <p
                className="mt-0.5 font-mono text-lg font-semibold tabular-nums"
                style={{ color: CORAL }}
              >
                318ms
                <span className="ml-1 align-middle text-[0.58rem] font-normal">
                  ▲ 7.6×
                </span>
              </p>
            </div>
          </div>
          <div className="relative mt-4">
            <AreaChart
              points={SPIKE_POINTS}
              stroke={CORAL}
              fill={CORAL}
              id="hero-spike"
            />
            <span className="text-cc-nav-label absolute top-0 left-0 font-mono text-[0.56rem]">
              latency / 5m
            </span>
          </div>
          <div className="border-cc-card-border mt-4 grid grid-cols-3 gap-3 border-t pt-3">
            <MiniMetric label="p95" value="42ms" />
            <MiniMetric label="throughput" value="1.2k/m" />
            <MiniMetric label="errors" value="0.3%" tone={AMBER} />
          </div>
        </div>
      </div>

      {/* Distributed-trace waterfall below the operation card. The trace id
          lives in this header — it identifies this trace, so it belongs here
          rather than floating between the two cards. */}
      <div className="border-cc-card-border bg-cc-card-bg relative z-0 mt-4 rounded-2xl border shadow-[0_30px_70px_-34px_rgba(0,0,0,0.8)] backdrop-blur">
        <div className="border-cc-card-border flex items-center justify-between border-b px-5 py-2.5">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
            distributed trace · checkout
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.6rem] tabular-nums">
            {TRACE_ID} · 318ms
          </span>
        </div>
        <div className="px-5 pt-4 pb-2">
          <NitroFrame>
            {/* Draw the trace in the first ~30% of the loop, then hold the
                complete waterfall — a hero artifact should read at a glance. */}
            <TraceWaterfall
              trace={CHECKOUT_TRACE}
              rowHeight={30}
              durationMs={4500}
              once
            />
          </NitroFrame>
        </div>
        <div className="border-cc-card-border bg-cc-surface/40 flex items-center gap-2 border-t px-5 py-2.5">
          <StatusDot status="error" />
          <span className="text-cc-ink-dim font-mono text-[0.6rem]">
            <span style={{ color: CORAL }}>204ms</span> of this 318ms request
            were spent in the billing service.
          </span>
        </div>
      </div>
    </div>
  );
}

function Hero() {
  return (
    <PatternBand pattern="grid" flush className="border-b py-16 sm:py-24">
      <div className="grid items-center gap-12 lg:grid-cols-[0.95fr_1.05fr]">
        <div>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 tracking-tight">
            See what the <span style={{ color: TEAL }}>API</span> is doing.
          </h1>
          <p className="lead text-cc-prose !font-body !text-lead mt-6 max-w-xl !font-normal">
            Latency, errors, and throughput for every operation in production.
            And when something gets slow, open the trace and see exactly which
            call took the time.
          </p>
          <p className="text-cc-ink-dim mt-4 max-w-xl text-sm leading-relaxed">
            Nitro is OpenTelemetry-native: it collects traces, metrics, and logs
            from every service you run, not just your GraphQL API.
          </p>

          <ButtonRow align="start" className="mt-9">
            <SolidButton href="https://nitro.chillicream.com">
              Start for Free
            </SolidButton>
            <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
              Read the Docs
            </OutlineButton>
          </ButtonRow>

          {/* the legend doubles as proof the color is data, not decoration */}
          <div className="mt-8 flex flex-wrap items-center gap-x-5 gap-y-2">
            <LegendDot status="healthy" label="healthy" />
            <LegendDot status="warning" label="warning" />
            <LegendDot status="error" label="error" />
          </div>
        </div>

        <IncidentArtifact />
      </div>
    </PatternBand>
  );
}

/* ============================================================================
   FULL-OTEL BAND — the standards claim, anchored by the actual OTLP config so
   it reads as a section, not a floating caption. Copy left, proof right.
============================================================================ */

const STATEMENT_KINDS: readonly ChipKind[] = [
  "graphql",
  "rest",
  "grpc",
  "job",
  "db",
];

const OTEL_CHECKS: readonly string[] = [
  "Vendor-neutral OTLP in, no proprietary agent",
  "Hot Chocolate is auto-instrumented",
  "Works with any OpenTelemetry backend, not just Nitro",
];

function FullOtelBand() {
  return (
    <Band
      className="mt-12"
      skin="card"
      layout="split"
      labelledBy="otel-title"
      main={
        <div>
          <SectionHeading
            titleId="otel-title"
            title={
              <>
                OpenTelemetry-native,{" "}
                <span className="text-cc-accent whitespace-nowrap">
                  end to end.
                </span>
              </>
            }
            description="Your services export traces, metrics, and logs over plain OTLP, and Nitro turns them into one picture — a single trace that follows each request from the GraphQL edge all the way down to the database."
          />
          <div className="mt-6 flex flex-wrap gap-2">
            {STATEMENT_KINDS.map((kind) => (
              <Tag key={kind}>{KIND_LABEL[kind]}</Tag>
            ))}
          </div>
        </div>
      }
      aside={
        <div>
          <Eyebrow size="2xs">Program.cs</Eyebrow>
          <pre className="border-cc-card-border/60 text-cc-ink mt-3 overflow-x-auto rounded-xl border bg-black/20 p-4 font-mono text-[0.72rem] leading-relaxed">
            <code>
              {"builder.Services\n    ."}
              <span className="text-cc-accent">AddNitro</span>
              {"()\n    ."}
              <span className="text-cc-accent">AddOpenTelemetry</span>
              {"();\n\nbuilder.Services\n    ."}
              <span className="text-cc-accent">AddGraphQLServer</span>
              {"()\n    ."}
              <span className="text-cc-accent">AddInstrumentation</span>
              {"();"}
            </code>
          </pre>
          <CheckList items={OTEL_CHECKS} className="mt-5" />
        </div>
      }
    />
  );
}

/* ============================================================================
   THREE QUESTIONS — what's slow, how bad, for whom. One question per row,
   one product visual per claim, alternating sides (vendor feature-row idiom).
============================================================================ */

const P95_SERIES = [
  40, 43, 41, 45, 42, 46, 44, 41, 43, 46, 42, 44, 47, 43, 45, 42, 44, 46, 43,
  45, 41, 44, 42, 45,
];
const P99_SERIES = [
  90, 94, 98, 102, 108, 115, 122, 130, 140, 152, 165, 180, 198, 218, 238, 258,
  278, 296, 310, 318, 312, 298, 270, 240,
];

const CLIENTS: readonly Client[] = [
  { name: "web-storefront", total: 184000, impact: 94 },
  { name: "mobile-ios", total: 121000, impact: 71 },
  { name: "android", total: 68000, impact: 58 },
];

const IMPACT_INSIGHTS: readonly InsightRow[] = [
  {
    id: "op-checkout",
    spanKind: "server",
    name: "mutation checkout",
    averageLatency: 62,
    opm: 1200,
    errorRate: 0.003,
    impact: 98,
    latencySeries: [42, 48, 55, 61, 58, 62, 60],
    throughputSeries: [980, 1040, 1100, 1150, 1190, 1200, 1220],
  },
  {
    id: "op-billing",
    spanKind: "client",
    name: "gRPC · Billing.Charge",
    averageLatency: 204,
    opm: 610,
    errorRate: 0.004,
    impact: 71,
    latencySeries: [180, 190, 198, 204, 201, 204, 204],
    throughputSeries: [560, 580, 600, 610, 605, 612, 610],
  },
  {
    id: "op-orders",
    spanKind: "server",
    name: "REST · POST /orders",
    averageLatency: 31,
    opm: 1400,
    errorRate: 0.0,
    impact: 54,
    latencySeries: [28, 30, 29, 31, 30, 31, 31],
    throughputSeries: [1300, 1340, 1360, 1380, 1390, 1400, 1410],
  },
  {
    id: "op-coupon",
    spanKind: "server",
    name: "mutation applyCoupon",
    averageLatency: 16,
    opm: 340,
    errorRate: 0.014,
    impact: 38,
    latencySeries: [14, 15, 16, 17, 15, 16, 16],
    throughputSeries: [300, 310, 320, 330, 335, 338, 340],
  },
  {
    id: "op-receipt",
    spanKind: "consumer",
    name: "job · receipt.worker",
    averageLatency: 58,
    opm: 240,
    errorRate: 0.002,
    impact: 33,
    latencySeries: [52, 54, 56, 58, 57, 58, 58],
    throughputSeries: [210, 220, 228, 235, 238, 240, 240],
  },
];

function ThreeQuestions() {
  return (
    <>
      <ChapterBand
        className="mt-12 sm:mt-16"
        title={
          <>
            What&rsquo;s slow. How bad. <br className="hidden sm:block" />
            And for whom.
          </>
        }
        description="Three questions decide every production investigation. The dashboards answer each one directly — shown here with one example incident: a slow checkout request."
      />

      <section className="py-12 sm:py-16">
        <div className="flex flex-col gap-16 sm:gap-20">
          <FeatureRow
            title="Fix what hurts most, with the impact score."
            body={
              <>
                Every operation is ranked by an impact score that combines
                traffic, latency, and error rate, so the list reads top to
                bottom as your to-do list.
              </>
            }
            visual={
              <ChartTile
                title="Operations"
                hint="ranked by impact · last 1h"
                glow
              >
                <NitroFrame>
                  {/* height:auto sizes to the rows; overflowY:hidden suppresses
                      the ~5px vertical scrollbar the grid/card sizing otherwise
                      forces (overflowX:auto stays for mobile horizontal scroll). */}
                  <InsightsTable
                    once
                    rows={IMPACT_INSIGHTS as InsightRow[]}
                    nameHeader="Operation"
                    errorThreshold={0.01}
                    style={{ height: "auto", overflowY: "hidden" }}
                  />
                </NitroFrame>
              </ChartTile>
            }
          />

          <FeatureRow
            reverse
            title="The whole latency picture, not an average."
            body={
              <>
                Averages hide your slowest requests. Here p95 held flat at 42ms
                while p99 spiked to 318ms — most users were fine, the slowest
                few were not. Only percentiles show it.
              </>
            }
            visual={
              <ChartTile title="Latency · checkout" hint="p95 / p99 · ms" glow>
                {/* reducedMotion="always" renders the lines static (no draw-in
                    animation — animated line reveals never look clean). Literal
                    hex, not --t-* var tokens: hc2's refactored LineAreaChart
                    drops CSS-var colors inside its motion styles, so p95/p99
                    pass as the page's teal (calm) and coral (hot). */}
                <NitroFrame reducedMotion="always">
                  <div className="mb-3 flex items-center gap-4 font-mono text-[0.62rem]">
                    <span className="text-cc-ink-dim flex items-center gap-1.5">
                      <span
                        className="h-1.5 w-3 rounded-full"
                        style={{ background: TEAL }}
                      />
                      p95
                    </span>
                    <span className="text-cc-ink-dim flex items-center gap-1.5">
                      <span
                        className="h-1.5 w-3 rounded-full"
                        style={{ background: CORAL }}
                      />
                      p99
                    </span>
                  </div>
                  <div className="h-44">
                    <LineAreaChart
                      series={[
                        {
                          values: P95_SERIES,
                          stroke: TEAL,
                          fill: true,
                          fillOpacity: 0.12,
                        },
                        {
                          values: P99_SERIES,
                          stroke: CORAL,
                          fill: true,
                          fillOpacity: 0.14,
                        },
                      ]}
                      domain={[0, 340]}
                      grid
                      showHead
                    />
                  </div>
                </NitroFrame>
                <StatStrip
                  className="mt-4"
                  items={[
                    { label: "p99", value: "318ms" },
                    { label: "p95", value: "42ms" },
                    { label: "throughput", value: "1.2k/m" },
                    { label: "error rate", value: "0.31%" },
                  ]}
                />
              </ChartTile>
            }
          />

          <FeatureRow
            title={<>Know who&rsquo;s affected — before they report it.</>}
            body={
              <>
                Latency and errors are attributed to the client that felt them,
                by name and version. &ldquo;Is it everyone, or just the web
                app?&rdquo; becomes a lookup, not a debate.
              </>
            }
            visual={
              <ChartTile title="Clients · checkout" hint="share of impact">
                <NitroFrame>
                  <HBarSeries
                    once
                    clients={CLIENTS as Client[]}
                    maxBars={3}
                    barHeight={14}
                  />
                </NitroFrame>
                <p className="text-caption text-cc-ink-dim mt-3">
                  The web storefront drives the impact; both mobile apps are
                  barely affected.
                </p>
              </ChartTile>
            }
          />
        </div>
      </section>
    </>
  );
}

/* ============================================================================
   FIND THE CAUSE — a centered opener, then the two product screens: metrics
   link to traces, traces link to the failing line.
============================================================================ */

interface FramedVisualProps {
  readonly children: ReactNode;
}

/** Frames a chrome-less Nitro product screen like an embedded screenshot. */
function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-x-6 -inset-y-4 -z-10 rounded-[2rem] opacity-40 blur-3xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 40%, rgba(94,234,212,0.16), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
        {children}
      </div>
    </div>
  );
}

function FindTheCause() {
  return (
    <>
      <ChapterBand
        className="mt-12 sm:mt-16"
        title={
          <>
            From symptom to cause in{" "}
            <span className="text-cc-accent whitespace-nowrap">one click.</span>
          </>
        }
        description="Metrics and traces are linked. When a chart shows a spike, the slow requests behind it are one click away — and each trace shows which service, query, or job took the time. No second tool, no correlating timestamps by hand."
      />

      <section className="py-12 sm:py-16">
        <div className="flex flex-col gap-16 sm:gap-20">
          <FeatureRow
            title="Follow one request across your whole backend."
            body="Distributed tracing follows a single request across every service it touches. Open the waterfall to see how long each call took, and which one made the request slow."
            visual={
              <FramedVisual>
                <NitroTrace className="w-full" />
              </FramedVisual>
            }
          >
            <StepPath
              className="mt-6"
              steps={["dashboard", "operation", "slow span"]}
            />
          </FeatureRow>

          <FeatureRow
            reverse
            title="From an error spike to the line that threw it."
            body="When errors spike, click through to the failing operation and read the server-side stack trace behind it — without searching through logs."
            visual={
              <FramedVisual>
                <NitroDiagnose className="w-full" />
              </FramedVisual>
            }
          />
        </div>
      </section>
    </>
  );
}

/* ============================================================================
   HONESTY BAND — keep the real dashboards and the setup step two distinct
   facts.
============================================================================ */

const HONESTY_POINTS: readonly string[] = [
  "Telemetry is plain OpenTelemetry: it flows to Nitro and to any OTel backend you already run.",
  "Dashboards do not light up until your services export OTLP to a Nitro project.",
  "Hot Chocolate ships with the instrumentation that powers these views; your other services instrument with the standard OTel SDK.",
  "It's the same OpenTelemetry you would set up anyway — Nitro is just where it lands.",
];

function HonestySection() {
  return (
    <Band
      className="mt-12"
      skin="card"
      layout="split"
      labelledBy="setup-title"
      main={
        <div>
          <SectionHeading
            titleId="setup-title"
            title="Built on standards, honest about setup."
            description="These dashboards need your telemetry before they light up. No surprises: here is exactly what that takes."
          />
          <ArrowLink
            href="/docs/nitro/open-telemetry/operation-monitoring"
            className="mt-6"
          >
            See the setup guide
          </ArrowLink>
        </div>
      }
      aside={<CheckList items={HONESTY_POINTS} />}
    />
  );
}

/* ============================================================================
   CLOSING CTA
============================================================================ */

function ClosingCta() {
  return (
    <Band className="mt-12" skin="accent" layout="centered">
      <SectionHeading
        align="center"
        title="Know what the API is doing — before your users do."
        description="One trace, end to end — latency, errors, throughput, and impact for every operation, service, and client, with the trace behind every number."
      />
      <ButtonRow align="center" className="mt-9">
        <SolidButton href="https://nitro.chillicream.com">
          Start for Free
        </SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </ButtonRow>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-xl text-sm leading-relaxed">
        Learn more about{" "}
        <Link
          href="/platform/release-safety"
          className="text-cc-accent hover:text-cc-accent-hover"
        >
          release safety
        </Link>
        , the{" "}
        <Link
          href="/platform/ecosystem"
          className="text-cc-accent hover:text-cc-accent-hover"
        >
          ecosystem
        </Link>
        , or the wider{" "}
        <Link
          href="/platform"
          className="text-cc-accent hover:text-cc-accent-hover"
        >
          platform
        </Link>
        .
      </p>
    </Band>
  );
}

/* ============================================================================
   PAGE
============================================================================ */

export default function AnalyticsPage() {
  return (
    <>
      <Hero />
      <FullOtelBand />
      <ThreeQuestions />
      <FindTheCause />
      <HonestySection />
      <ClosingCta />
    </>
  );
}
