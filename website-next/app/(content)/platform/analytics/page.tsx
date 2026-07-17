import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import Link from "next/link";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  ChartPanel,
  CodeBlock,
  CountUp,
  HBarSeries,
  InsightsTable,
  LineAreaChart,
  NitroDiagnose,
  NitroTheme,
  NitroTrace,
  token,
  TraceWaterfall,
} from "@/src/nitro";
import type { Client, InsightRow, Trace } from "@/src/nitro/lib/data/types";

export const metadata: Metadata = {
  title: "Analytics | See What the API Is Doing",
  description:
    "OpenTelemetry-native analytics for your whole backend: operation insights, impact score, per-client usage, distributed traces, and service monitoring across GraphQL, REST, gRPC, and background jobs.",
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
  openGraph: {
    title: "Analytics — See what the API is doing.",
    description:
      "Latency, errors, and throughput for every operation, and the distributed trace behind every slow request. OpenTelemetry-native across GraphQL, REST, gRPC, and background jobs.",
  },
};

/* ----------------------------------------------------------------------------
   Scene palette. teal #5eead4 is the signature; status semantics are rationed
   as data and carried by the charts, not the prose:
     green  #34d399  healthy
     amber  #fbbf24  investigating
     coral  #f0786a  firing
---------------------------------------------------------------------------- */
const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";

type Status = "healthy" | "investigating" | "firing";

const STATUS_COLOR: Record<Status, string> = {
  healthy: GREEN,
  investigating: AMBER,
  firing: CORAL,
};

/* ============================================================================
   Primitives
============================================================================ */

interface EyebrowProps {
  readonly tag: string;
  readonly children: ReactNode;
  readonly color?: string;
  readonly align?: "start" | "center";
}

function Eyebrow({
  tag,
  children,
  color = TEAL,
  align = "start",
}: EyebrowProps) {
  return (
    <p
      className={`text-cc-nav-label flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.18em] uppercase ${
        align === "center" ? "justify-center" : ""
      }`}
    >
      <span style={{ color }}>{tag}</span>
      <span className="bg-cc-card-border h-px w-6" aria-hidden />
      {children}
    </p>
  );
}

/**
 * Centered chapter opener with a soft teal halo, used to punctuate the page
 * between the graphic-heavy feature rows. Balanced whitespace + the glow give
 * it presence so it does not read as a slim, lopsided text block.
 */
interface ChapterIntroProps {
  readonly tag: string;
  readonly kicker: ReactNode;
  readonly title: ReactNode;
  readonly body: ReactNode;
  readonly className?: string;
}

function ChapterIntro({
  tag,
  kicker,
  title,
  body,
  className = "",
}: ChapterIntroProps) {
  return (
    <div
      className={`relative mx-auto max-w-2xl overflow-x-clip px-4 text-center ${className}`}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute top-1/2 left-1/2 -z-10 h-[150%] w-[140%] -translate-x-1/2 -translate-y-1/2"
        style={{
          background:
            "radial-gradient(50% 50% at 50% 50%, rgba(94,234,212,0.08), transparent 70%)",
        }}
      />
      <Eyebrow tag={tag} align="center">
        {kicker}
      </Eyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
        {title}
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl leading-relaxed">
        {body}
      </p>
    </div>
  );
}

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

/* ----------------------------------------------------------------------------
   N19 idiom: marketing Card shells + real product chart primitives inside a
   NitroCanvas (a NitroTheme wrapper so the `--t-*` chart tokens resolve).
---------------------------------------------------------------------------- */

interface CardProps {
  readonly className?: string;
  readonly children: ReactNode;
  readonly glow?: boolean;
}

/** Hairline-bordered, softly rounded, backdrop-blurred surface. */
function Card({ className, children, glow = false }: CardProps) {
  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur",
        className ?? "",
      ].join(" ")}
    >
      {glow && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-24 right-0 -z-0 h-56 w-56 opacity-40 blur-3xl"
          style={{
            background:
              "radial-gradient(50% 50% at 60% 40%, rgba(94,234,212,0.18), transparent 70%)",
          }}
        />
      )}
      <div className="relative z-10 flex h-full flex-col">{children}</div>
    </div>
  );
}

interface CardHeaderProps {
  readonly title: string;
  readonly hint?: string;
}

function CardHeader({ title, hint }: CardHeaderProps) {
  return (
    <div className="flex items-baseline justify-between gap-3 px-5 pt-5">
      <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
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

/* ============================================================================
   Shared kind chips — used by the full-OTel statement band.
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

const KIND_TINT: Record<ChipKind, string> = {
  graphql: TEAL,
  rest: VIOLET,
  grpc: CORAL,
  job: VIOLET,
  db: VIOLET,
};

/* ============================================================================
   Shared trace fixture — the hero waterfall and the bento's "Slow span" tile
   read from the same checkout incident.
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
          <StatusBadge status="investigating" label="Investigating" />
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

      {/* The stitch: a single trace-id hairline dropping from the spike into
          the slow span of the waterfall below. Anchored from the container's
          bottom so it tracks the waterfall card's height, not the op card's. */}
      <div
        className="pointer-events-none absolute bottom-[120px] left-[55%] z-10 hidden h-[240px] w-px sm:block"
        aria-hidden
      >
        <svg
          viewBox="0 0 2 150"
          width="2"
          height="100%"
          preserveAspectRatio="none"
        >
          <defs>
            <linearGradient id="hero-stitch" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={CORAL} stopOpacity="0" />
              <stop offset="45%" stopColor={CORAL} stopOpacity="0.6" />
              <stop offset="100%" stopColor={CORAL} stopOpacity="0.95" />
            </linearGradient>
          </defs>
          <line
            x1="1"
            y1="0"
            x2="1"
            y2="150"
            stroke="url(#hero-stitch)"
            strokeWidth="1.5"
            strokeDasharray="3 3"
          />
        </svg>
      </div>
      <div
        className="absolute bottom-[289px] left-[55%] z-20 hidden -translate-x-1/2 sm:block"
        aria-hidden
      >
        <span
          className="rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.08em]"
          style={{
            color: CORAL,
            borderColor: `${CORAL}55`,
            backgroundColor: "rgba(11,15,26,0.9)",
          }}
        >
          trace {TRACE_ID}
        </span>
      </div>

      {/* Distributed-trace waterfall the stitch lands on. */}
      <div className="border-cc-card-border bg-cc-card-bg relative z-0 mt-[-26px] rounded-2xl border pt-8 shadow-[0_30px_70px_-34px_rgba(0,0,0,0.8)] backdrop-blur">
        <div className="border-cc-card-border flex items-center justify-between border-b px-5 py-2.5">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
            distributed trace · checkout
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.6rem] tabular-nums">
            318ms · 5 spans
          </span>
        </div>
        <div className="px-5 pt-4 pb-2">
          <NitroCanvas>
            {/* Draw the trace in the first ~30% of the loop, then hold the
                complete waterfall — a hero artifact should read at a glance. */}
            <TraceWaterfall
              trace={CHECKOUT_TRACE}
              rowHeight={30}
              durationMs={4500}
              once
            />
          </NitroCanvas>
        </div>
        <div className="border-cc-card-border bg-cc-surface/40 flex items-center gap-2 border-t px-5 py-2.5">
          <StatusDot status="firing" />
          <span className="text-cc-ink-dim font-mono text-[0.6rem]">
            <span style={{ color: CORAL }}>204ms</span> of this 318ms request
            were spent in the billing service.
          </span>
        </div>
      </div>
    </div>
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

function Hero() {
  return (
    <header className="border-cc-card-border bg-cc-surface/40 relative overflow-hidden rounded-3xl border px-6 py-12 backdrop-blur sm:px-12 sm:py-16">
      {/* spotlight mesh in the scene accent, rationed to one gradient */}
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(70% 80% at 12% -10%, rgba(94,234,212,0.18), transparent 60%), radial-gradient(60% 70% at 105% 110%, rgba(240,120,106,0.12), transparent 55%)",
        }}
        aria-hidden
      />
      {/* faint grid floor for depth */}
      <div
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.05]"
        style={{
          backgroundImage:
            "linear-gradient(rgba(245,241,234,1) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,1) 1px, transparent 1px)",
          backgroundSize: "46px 46px",
          maskImage:
            "radial-gradient(75% 70% at 30% 30%, #000 30%, transparent 80%)",
        }}
        aria-hidden
      />

      <div className="grid items-center gap-12 lg:grid-cols-[0.95fr_1.05fr]">
        <div>
          <Eyebrow tag="analytics" color={AMBER}>
            <span className="inline-flex items-center gap-2">
              <StatusDot status="investigating" pulse />
              API observability for GraphQL &amp; .NET
            </span>
          </Eyebrow>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-5 tracking-tight">
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

          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
              Read the Docs
            </OutlineButton>
          </div>

          {/* the legend doubles as proof the color is data, not decoration */}
          <div className="mt-8 flex flex-wrap items-center gap-x-5 gap-y-2">
            <LegendDot status="healthy" label="healthy" />
            <LegendDot status="investigating" label="investigating" />
            <LegendDot status="firing" label="firing" />
          </div>
        </div>

        <IncidentArtifact />
      </div>
    </header>
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

const OTEL_SNIPPET = `{
  "Otlp": {
    "Endpoint": "https://otlp.chillicream.cloud",
    "Protocol": "grpc"
  }
}`;

const OTEL_CHECKS: readonly string[] = [
  "Vendor-neutral OTLP in, no proprietary agent",
  "Hot Chocolate is auto-instrumented",
  "Your data stays yours: fan the same export out to any OTel backend",
];

function FullOtelBand() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-12 grid items-center gap-10 rounded-3xl border p-6 backdrop-blur sm:p-10 lg:grid-cols-[1.05fr_0.95fr]">
      <div>
        <Eyebrow tag="otel" color={TEAL}>
          traces · metrics · logs
        </Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          OpenTelemetry-native,{" "}
          <span className="whitespace-nowrap" style={{ color: TEAL }}>
            end to end.
          </span>
        </h2>
        <p className="text-cc-ink-dim mt-4 leading-relaxed">
          Your services export traces, metrics, and logs over plain OTLP, and
          Nitro turns them into one picture. A single trace follows each request
          through your GraphQL API, REST endpoints, gRPC services, background
          jobs, and the database.
        </p>
        <div className="mt-6 flex flex-wrap items-center gap-2">
          {STATEMENT_KINDS.map((kind) => (
            <span
              key={kind}
              className="rounded-full px-2.5 py-1 font-mono text-[0.62rem] tracking-[0.08em] uppercase"
              style={{
                color: KIND_TINT[kind],
                backgroundColor: `${KIND_TINT[kind]}1f`,
              }}
            >
              {KIND_LABEL[kind]}
            </span>
          ))}
        </div>
      </div>

      <Card glow>
        <CardHeader title="One config, everything flows" hint="OTLP" />
        <div className="flex flex-col px-5 pt-3 pb-5">
          <p className="text-cc-nav-label mb-2 font-mono text-[0.56rem] tracking-[0.12em] uppercase">
            appsettings.json
          </p>
          <div className="border-cc-card-border/60 rounded-lg border bg-black/30 px-3">
            <NitroCanvas>
              <CodeBlock
                code={OTEL_SNIPPET}
                lang="json"
                gutter={false}
                caret={false}
              />
            </NitroCanvas>
          </div>
          <ul className="mt-4 space-y-2 text-[0.78rem]">
            {OTEL_CHECKS.map((check) => (
              <li key={check} className="text-cc-ink flex items-start gap-2">
                <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
                  <CheckIcon size={13} />
                </span>
                <span>{check}</span>
              </li>
            ))}
          </ul>
        </div>
      </Card>
    </section>
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

interface QuestionRowProps {
  readonly index: string;
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly body: ReactNode;
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

/** One question, one claim, one product visual — alternating split row. */
function QuestionRow({
  index,
  eyebrow,
  title,
  body,
  visual,
  reverse = false,
}: QuestionRowProps) {
  return (
    <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-16">
      <div
        className={["min-w-0 lg:col-span-5", reverse ? "lg:order-2" : ""].join(
          " ",
        )}
      >
        <Eyebrow tag={index}>{eyebrow}</Eyebrow>
        <h3 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
          {title}
        </h3>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">{body}</p>
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

function ThreeQuestions() {
  return (
    <section className="mt-20 sm:mt-28">
      <ChapterIntro
        className="mb-14 sm:mb-20"
        tag="dashboards"
        kicker="operations · services · clients"
        title={
          <>
            What&rsquo;s slow. How bad. <br className="hidden sm:block" />
            And for whom.
          </>
        }
        body="Three questions decide every production investigation. The dashboards answer each one directly — shown here with one example incident: a slow checkout request."
      />

      <div className="flex flex-col gap-16 sm:gap-20">
        <QuestionRow
          index="01"
          eyebrow="what's slow"
          title="Fix what hurts most, with the impact score."
          body={
            <>
              Every operation is ranked by an impact score that combines
              traffic, latency, and error rate — across GraphQL, REST, gRPC, and
              background jobs. The list reads top to bottom as your to-do list.
            </>
          }
          visual={
            <Card glow>
              <CardHeader
                title="Operations"
                hint="ranked by impact · last 1h"
              />
              <div className="px-5 pt-3 pb-4">
                <NitroCanvas>
                  <InsightsTable
                    once
                    rows={IMPACT_INSIGHTS as InsightRow[]}
                    nameHeader="Operation"
                    errorThreshold={0.01}
                  />
                </NitroCanvas>
              </div>
            </Card>
          }
        />

        <QuestionRow
          index="02"
          eyebrow="how bad"
          title="The whole latency picture, not an average."
          body={
            <>
              p95 and p99 per operation, with throughput and error rate beside
              them. In this incident the p99 climbed to 318ms while the p95
              stayed flat — a tail problem your average would never show.
            </>
          }
          reverse
          visual={
            <Card glow>
              <CardHeader title="Latency · checkout" hint="p95 / p99 · ms" />
              <div className="px-5 pt-3">
                <NitroCanvas>
                  <ChartPanel
                    title="Response time"
                    subtitle="last 60 minutes"
                    height={168}
                    yDomain={[0, 340]}
                    yTicks={[0, 100, 200, 300]}
                    legend={[
                      { label: "p95", color: token.cP95 },
                      { label: "p99", color: token.cP99 },
                    ]}
                  >
                    <LineAreaChart
                      once
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
                      domain={[0, 340]}
                      grid
                      showHead
                    />
                  </ChartPanel>
                </NitroCanvas>
              </div>
              <div className="border-cc-card-border/60 mt-4 grid grid-cols-2 gap-x-6 gap-y-3 border-t px-5 py-4 sm:grid-cols-4">
                <div>
                  <p className="text-cc-nav-label font-mono text-[0.56rem] tracking-[0.1em] uppercase">
                    p99
                  </p>
                  <div className="flex items-baseline gap-1">
                    <NitroCanvas className="h-8">
                      <CountUp
                        once
                        value={318}
                        style={{ justifyContent: "flex-start", fontSize: 24 }}
                      />
                    </NitroCanvas>
                    <span className="text-cc-ink-dim font-mono text-xs">
                      ms
                    </span>
                  </div>
                </div>
                <MiniMetric label="p95" value="42ms" />
                <MiniMetric label="throughput" value="1.2k/m" />
                <MiniMetric label="error rate" value="0.31%" tone={AMBER} />
              </div>
            </Card>
          }
        />

        <QuestionRow
          index="03"
          eyebrow="for whom"
          title={<>Know who&rsquo;s affected — before they report it.</>}
          body={
            <>
              Latency and errors are attributed to the client that felt them, by
              name and version. &ldquo;Is it everyone, or just the web
              app?&rdquo; becomes a lookup, not a debate.
            </>
          }
          visual={
            <Card glow>
              <CardHeader title="Clients · checkout" hint="share of impact" />
              <div className="px-5 pt-4 pb-5">
                <NitroCanvas>
                  <HBarSeries
                    once
                    clients={CLIENTS as Client[]}
                    maxBars={3}
                    barHeight={14}
                  />
                </NitroCanvas>
              </div>
              <p className="text-caption text-cc-ink-dim border-cc-card-border/60 border-t px-5 py-3">
                The web storefront drives the impact; both mobile apps are
                barely affected.
              </p>
            </Card>
          }
        />
      </div>
    </section>
  );
}

/* ============================================================================
   FIND THE CAUSE — chapter header for the two product screens: metrics link
   to traces, traces link to the failing line.
============================================================================ */

function CauseChapterHeader() {
  return (
    <ChapterIntro
      className="mt-24 sm:mt-32"
      tag="root cause"
      kicker="metrics → traces → code"
      title={
        <>
          From symptom to cause in{" "}
          <span className="whitespace-nowrap" style={{ color: TEAL }}>
            one click.
          </span>
        </>
      }
      body="Metrics and traces are linked. When a chart shows a spike, the slow requests behind it are one click away — and each trace shows which service, query, or job took the time. No second tool, no correlating timestamps by hand."
    />
  );
}

/* ============================================================================
   NITRO SCREEN SHOWCASES — two static split rows, framed product visuals.
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

function TraceShowcase() {
  return (
    <section className="mt-12 grid items-center gap-12 overflow-x-clip lg:grid-cols-12 lg:gap-16">
      <div className="lg:col-span-5">
        <Eyebrow tag="trace">span waterfall</Eyebrow>
        <h3 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
          Follow one request across your whole backend.
        </h3>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          Distributed tracing follows a single request across GraphQL, REST,
          gRPC, and background jobs. Open the waterfall and see how long every
          call took, and which one made the request slow.
        </p>
        <p className="mt-6 font-mono text-[0.66rem] tracking-wide">
          <span className="text-cc-ink-dim">dashboard</span>
          <span className="text-cc-nav-label"> → </span>
          <span className="text-cc-ink-dim">operation</span>
          <span className="text-cc-nav-label"> → </span>
          <span style={{ color: CORAL }}>slow span</span>
        </p>
      </div>
      <div className="lg:col-span-7">
        <FramedVisual>
          <NitroTrace className="w-full" />
        </FramedVisual>
      </div>
    </section>
  );
}

function DiagnoseShowcase() {
  return (
    <section className="mt-12 grid items-center gap-12 overflow-x-clip lg:grid-cols-12 lg:gap-16">
      <div className="lg:order-2 lg:col-span-5">
        <Eyebrow tag="diagnose" color={CORAL}>
          error spike
        </Eyebrow>
        <h3 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
          From an error spike to the line that threw it.
        </h3>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          When errors spike, click through to the failing operation and read the
          server-side stack trace behind it — without searching through logs.
        </p>
        <p className="mt-6 font-mono text-[0.66rem] tracking-wide">
          <span className="text-cc-ink-dim">error spike</span>
          <span className="text-cc-nav-label"> → </span>
          <span className="text-cc-ink-dim">failing operation</span>
          <span className="text-cc-nav-label"> → </span>
          <span style={{ color: CORAL }}>stack trace</span>
        </p>
      </div>
      <div className="lg:order-1 lg:col-span-7">
        <FramedVisual>
          <NitroDiagnose className="w-full" />
        </FramedVisual>
      </div>
    </section>
  );
}

/* ============================================================================
   HONESTY BAND — keep the real dashboards and the setup step two distinct
   facts.
============================================================================ */

function HonestySection() {
  const points: readonly string[] = [
    "Telemetry is plain OpenTelemetry: it flows to Nitro and to any OTel backend you already run.",
    "Dashboards do not light up until your services export OTLP to a Nitro project.",
    "Hot Chocolate ships with the instrumentation that powers these views; REST, gRPC, and job services instrument with the standard OTel SDK.",
    "Numbers on this page are illustrative dashboard values. Yours come from your own telemetry.",
  ];
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-12 grid gap-8 rounded-3xl border p-6 backdrop-blur sm:grid-cols-[0.8fr_1.2fr] sm:p-10">
      <div>
        <Eyebrow tag="setup" color={GREEN}>
          <span className="inline-flex items-center gap-2">
            <StatusDot status="healthy" />
            before you start
          </span>
        </Eyebrow>
        <h2 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
          Built on standards, honest about setup.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          These dashboards need your telemetry before they light up. No
          surprises: here is exactly what that takes.
        </p>
        <Link
          href="/docs/nitro/open-telemetry/operation-monitoring"
          className="text-cc-accent hover:text-cc-accent-hover mt-5 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
        >
          See the setup guide
          <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>
      <ul className="space-y-4">
        {points.map((point) => (
          <li
            key={point}
            className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
          >
            <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
              <CheckIcon size={15} />
            </span>
            <span>{point}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

/* ============================================================================
   CLOSING CTA
============================================================================ */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/40 relative mt-12 overflow-hidden rounded-3xl border px-6 py-14 text-center backdrop-blur sm:px-12 sm:py-20">
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(55% 90% at 50% -10%, rgba(94,234,212,0.16), transparent 60%)",
        }}
        aria-hidden
      />
      <Eyebrow tag="get started" color={GREEN}>
        <span className="inline-flex items-center gap-2">
          <StatusDot status="healthy" pulse />
          monitoring on
        </span>
      </Eyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h2 mx-auto mt-5 max-w-2xl">
        Know what the API is doing — before your users do.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-5 max-w-xl leading-relaxed">
        One trace, end to end — latency, errors, throughput, and impact for
        every operation, service, and client, with the trace behind every
        number.
      </p>
      <div className="mt-9 flex flex-wrap justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-xl text-sm leading-relaxed">
        Learn more about{" "}
        <Link
          href="/platform/continuous-integration"
          className="text-cc-accent hover:text-cc-accent-hover"
        >
          continuous integration
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
    </section>
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
      <CauseChapterHeader />
      <TraceShowcase />
      <DiagnoseShowcase />
      <HonestySection />
      <ClosingCta />
    </>
  );
}
