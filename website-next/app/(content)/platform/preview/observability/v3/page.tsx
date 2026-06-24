import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Production View | See What the API Is Doing",
  description:
    "Nitro is OpenTelemetry-native: watch p95/p99, throughput, error rate, and impact score per operation, then follow one trace-id from the spike down to the slow span.",
  keywords: [
    "GraphQL observability",
    "OpenTelemetry",
    "distributed tracing",
    "Nitro",
    "Hot Chocolate",
    "operation monitoring",
    "p95 p99 latency",
    "impact score",
    "trace waterfall",
    ".NET APM",
  ],
  openGraph: {
    title: "Production View — See what the API is doing.",
    description:
      "One checkout incident, every lens. Stitched by a single trace-id from the p99 spike straight into the slow gRPC span. OpenTelemetry-native observability for GraphQL.",
  },
  robots: { index: false, follow: false },
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
}

function Eyebrow({ tag, children, color = TEAL }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
      <span style={{ color }}>{tag}</span>
      <span className="bg-cc-card-border h-px w-6" aria-hidden />
      {children}
    </p>
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

interface TileProps {
  readonly children: ReactNode;
  readonly className?: string;
  /** Optional accent used for the corner glow that gives the tile depth. */
  readonly glow?: string;
}

/**
 * The bento cell. A single hairline card with backdrop blur and an optional
 * rationed corner glow, so the mosaic reads as layered depth rather than a row
 * of flat boxes.
 */
function Tile({ children, className = "", glow }: TileProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur ${className}`}
    >
      {glow && (
        <div
          className="pointer-events-none absolute -top-16 -right-16 h-40 w-40 rounded-full opacity-40 blur-3xl"
          style={{ backgroundColor: `${glow}55` }}
          aria-hidden
        />
      )}
      {children}
    </div>
  );
}

interface ChromeBarProps {
  readonly route: string;
  readonly right?: ReactNode;
}

function ChromeBar({ route, right }: ChromeBarProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-header flex items-center gap-3 border-b px-4 py-2.5">
      <div className="flex gap-1.5" aria-hidden>
        <span className="h-2.5 w-2.5 rounded-full bg-[#f0786a]/70" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#fbbf24]/70" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#34d399]/70" />
      </div>
      <span className="text-cc-ink-dim truncate font-mono text-[0.66rem]">
        {route}
      </span>
      {right && <div className="ml-auto flex items-center gap-2">{right}</div>}
    </div>
  );
}

/* ============================================================================
   Shared trace fixtures — every lens reads from the same incident.
============================================================================ */

const TRACE_ID = "7f3a·9b2e·c1";

interface SpanRow {
  readonly service: string;
  readonly kind: "graphql" | "rest" | "grpc" | "job" | "db";
  readonly offset: number; // 0..1 start
  readonly width: number; // 0..1 duration
  readonly ms: string;
  readonly hot?: boolean;
}

const KIND_LABEL: Record<SpanRow["kind"], string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  job: "job",
  db: "DB",
};

const KIND_TINT: Record<SpanRow["kind"], string> = {
  graphql: TEAL,
  rest: VIOLET,
  grpc: CORAL,
  job: VIOLET,
  db: VIOLET,
};

const TRACE_SPANS: readonly SpanRow[] = [
  {
    service: "api · checkout",
    kind: "graphql",
    offset: 0,
    width: 1,
    ms: "318ms",
  },
  {
    service: "users-svc · GET /me",
    kind: "rest",
    offset: 0.05,
    width: 0.13,
    ms: "44ms",
  },
  {
    service: "billing · Charge",
    kind: "grpc",
    offset: 0.21,
    width: 0.58,
    ms: "204ms",
    hot: true,
  },
  {
    service: "worker · receipt.enqueue",
    kind: "job",
    offset: 0.5,
    width: 0.18,
    ms: "58ms",
  },
  {
    service: "orders.db · INSERT",
    kind: "db",
    offset: 0.84,
    width: 0.12,
    ms: "38ms",
  },
];

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
        <ChromeBar
          route="nitro › operations › checkout"
          right={<StatusBadge status="investigating" label="Investigating" />}
        />
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
          the slow span of the waterfall below. */}
      <div
        className="pointer-events-none absolute top-[150px] left-[58%] z-10 hidden h-[150px] w-px sm:block"
        aria-hidden
      >
        <svg
          viewBox="0 0 2 150"
          width="2"
          height="150"
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
        className="absolute top-[214px] left-[58%] z-20 hidden -translate-x-1/2 sm:block"
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
      <div className="border-cc-card-border bg-cc-card-bg relative z-0 mt-[-26px] rounded-2xl border pt-12 shadow-[0_30px_70px_-34px_rgba(0,0,0,0.8)] backdrop-blur">
        <div className="border-cc-card-border flex items-center justify-between border-b px-5 pb-3">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
            distributed trace · checkout
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.6rem] tabular-nums">
            318ms · 5 spans
          </span>
        </div>
        <div className="space-y-2 p-5">
          {TRACE_SPANS.map((span) => (
            <SpanBar key={span.service} span={span} compact />
          ))}
        </div>
        <div className="border-cc-card-border bg-cc-surface/40 flex items-center gap-2 border-t px-5 py-2.5">
          <StatusDot status="firing" />
          <span className="text-cc-ink-dim font-mono text-[0.6rem]">
            billing · Charge held the request{" "}
            <span style={{ color: CORAL }}>204ms</span> — 64% of the trace.
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

interface SpanBarProps {
  readonly span: SpanRow;
  readonly compact?: boolean;
}

function SpanBar({ span, compact = false }: SpanBarProps) {
  const tint = span.hot ? CORAL : KIND_TINT[span.kind];
  return (
    <div
      className={`grid items-center gap-3 ${compact ? "grid-cols-[8rem_1fr]" : "grid-cols-[9.5rem_1fr]"}`}
    >
      <div className="flex items-center gap-2 overflow-hidden">
        <span
          className="shrink-0 rounded px-1 py-0.5 font-mono text-[0.5rem] tracking-[0.04em] uppercase"
          style={{ color: tint, backgroundColor: `${tint}1f` }}
        >
          {KIND_LABEL[span.kind]}
        </span>
        <span className="text-cc-ink-dim truncate font-mono text-[0.6rem]">
          {span.service}
        </span>
      </div>
      <div className="relative h-5">
        <div className="bg-cc-card-border/40 absolute inset-y-0 left-0 w-full rounded" />
        <div
          className="absolute inset-y-0 flex items-center justify-end rounded pr-1.5"
          style={{
            left: `${span.offset * 100}%`,
            width: `${span.width * 100}%`,
            backgroundColor: span.hot ? `${tint}33` : `${tint}22`,
            boxShadow: span.hot
              ? `inset 0 0 0 1px ${tint}aa, 0 0 16px ${tint}55`
              : `inset 0 0 0 1px ${tint}44`,
          }}
        >
          <span
            className="font-mono text-[0.55rem] tabular-nums"
            style={{ color: span.hot ? tint : "#c8d2e6" }}
          >
            {span.ms}
          </span>
        </div>
      </div>
    </div>
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
          <Eyebrow tag="production" color={AMBER}>
            <span className="inline-flex items-center gap-2">
              <StatusDot status="investigating" pulse />
              live telemetry
            </span>
          </Eyebrow>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-5 tracking-tight">
            See what the <span style={{ color: TEAL }}>API</span> is doing.
          </h1>
          <p className="lead text-cc-prose !font-body !text-lead mt-6 max-w-xl !font-normal">
            When checkout gets slow, you don&rsquo;t start another dashboard
            project. You open the operation, watch the p99 climb, and follow one
            trace-id straight to the span that&rsquo;s actually to blame.
          </p>
          <p className="text-cc-ink-dim mt-4 max-w-xl text-sm leading-relaxed">
            Nitro is OpenTelemetry-native. Debugging starts from evidence, not
            from a second source of truth.
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

/* ============================================================================
   BENTO MOSAIC — one incident, many lenses, in mismatched tiles.
============================================================================ */

function BentoMosaic() {
  return (
    <section className="mt-8">
      <div className="mb-7 max-w-3xl">
        <Eyebrow tag={`trace ${TRACE_ID}`}>one incident · many lenses</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          Operation, service, client. <br className="hidden sm:block" />
          Same trace, different question.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          Every tile below reads from the same checkout trace. The impact score
          tells you where to look, the lenses tell you who felt it, and the
          waterfall tells you exactly where the time went.
        </p>
      </div>

      {/* Asymmetric 6-column mosaic with mismatched row spans. */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
        {/* Oversized: operation table ranked by impact */}
        <div className="sm:col-span-4">
          <OperationTableTile />
        </div>

        {/* Oversized stat: p99 */}
        <div className="sm:col-span-2">
          <BigStatTile />
        </div>

        {/* Service topology, converging downward */}
        <div className="sm:col-span-2">
          <TopologyTile />
        </div>

        {/* Client lens + response-class lens stacked */}
        <div className="sm:col-span-2">
          <LensStackTile />
        </div>

        {/* Signal sparkline grid */}
        <div className="sm:col-span-2">
          <SparkGridTile />
        </div>

        {/* Full-width pull quote / honesty-adjacent statement */}
        <div className="sm:col-span-6">
          <PullQuoteTile />
        </div>
      </div>
    </section>
  );
}

/* ---- Tile: operation table ranked by impact ------------------------------ */

interface OpRow {
  readonly op: string;
  readonly type: "query" | "mutation";
  readonly p95: string;
  readonly errors: string;
  readonly impact: number;
  readonly status: Status;
}

const OP_ROWS: readonly OpRow[] = [
  {
    op: "checkout",
    type: "mutation",
    p95: "42ms",
    errors: "0.3%",
    impact: 98,
    status: "investigating",
  },
  {
    op: "productPage",
    type: "query",
    p95: "11ms",
    errors: "0.0%",
    impact: 71,
    status: "healthy",
  },
  {
    op: "cartSummary",
    type: "query",
    p95: "9ms",
    errors: "0.1%",
    impact: 54,
    status: "healthy",
  },
  {
    op: "applyCoupon",
    type: "mutation",
    p95: "16ms",
    errors: "1.4%",
    impact: 38,
    status: "firing",
  },
  {
    op: "search",
    type: "query",
    p95: "28ms",
    errors: "0.2%",
    impact: 33,
    status: "healthy",
  },
];

function OperationTableTile() {
  return (
    <Tile className="flex h-full flex-col" glow={TEAL}>
      <ChromeBar
        route="nitro › operations"
        right={
          <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.1em] uppercase">
            last 1h
          </span>
        }
      />
      <div className="text-cc-nav-label grid grid-cols-[1.2rem_1fr_3rem_3.4rem_4.6rem] items-center gap-3 px-5 py-2 font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        <span />
        <span>operation</span>
        <span className="text-right">p95</span>
        <span className="text-right">errors</span>
        <span className="text-right">impact</span>
      </div>
      <div className="divide-cc-card-border/60 flex-1 divide-y">
        {OP_ROWS.map((row) => (
          <div
            key={row.op}
            className="hover:bg-cc-hover grid grid-cols-[1.2rem_1fr_3rem_3.4rem_4.6rem] items-center gap-3 px-5 py-2.5 transition-colors"
          >
            <StatusDot status={row.status} pulse={row.status !== "healthy"} />
            <span className="flex items-baseline gap-2 truncate">
              <span className="text-cc-nav-label font-mono text-[0.58rem]">
                {row.type}
              </span>
              <span className="text-cc-heading truncate font-mono text-[0.82rem]">
                {row.op}
              </span>
            </span>
            <span className="text-cc-ink-dim text-right font-mono text-[0.72rem] tabular-nums">
              {row.p95}
            </span>
            <span
              className="text-right font-mono text-[0.72rem] tabular-nums"
              style={{ color: row.status === "firing" ? CORAL : undefined }}
            >
              {row.errors}
            </span>
            <span className="flex items-center justify-end gap-2">
              <span className="bg-cc-card-border/40 hidden h-1 w-9 overflow-hidden rounded-full sm:block">
                <span
                  className="block h-full rounded-full"
                  style={{
                    width: `${row.impact}%`,
                    backgroundColor: STATUS_COLOR[row.status],
                  }}
                />
              </span>
              <span
                className="w-6 text-right font-mono text-[0.74rem] font-semibold tabular-nums"
                style={{ color: STATUS_COLOR[row.status] }}
              >
                {row.impact}
              </span>
            </span>
          </div>
        ))}
      </div>
      <p className="text-caption text-cc-ink-dim border-cc-card-border/60 border-t px-5 py-3">
        Ranked by impact score, so the operation that&rsquo;s slow for everyone
        wins, not the one that&rsquo;s merely loud.
      </p>
    </Tile>
  );
}

/* ---- Tile: oversized p99 stat -------------------------------------------- */

function BigStatTile() {
  return (
    <Tile className="flex h-full flex-col justify-between p-6" glow={CORAL}>
      <div>
        <Eyebrow tag="checkout" color={CORAL}>
          p99
        </Eyebrow>
        <div className="font-heading text-cc-heading mt-5 text-[4rem] leading-none tabular-nums">
          318
          <span className="text-h6 text-cc-ink-dim ml-1 font-mono">ms</span>
        </div>
        <div
          className="mt-2 inline-flex items-center gap-1.5 font-mono text-[0.72rem]"
          style={{ color: CORAL }}
        >
          ▲ 7.6× over the p95 budget
        </div>
      </div>
      <div className="border-cc-card-border/60 mt-6 space-y-1.5 border-t pt-4 font-mono text-[0.7rem]">
        <div className="text-cc-ink-dim flex justify-between">
          <span>p95</span>
          <span className="text-cc-ink">42ms</span>
        </div>
        <div className="text-cc-ink-dim flex justify-between">
          <span>throughput</span>
          <span className="text-cc-ink">1.2k/m</span>
        </div>
        <div className="text-cc-ink-dim flex justify-between">
          <span>error rate</span>
          <span style={{ color: AMBER }}>0.3%</span>
        </div>
      </div>
    </Tile>
  );
}

/* ---- Tile: service topology, converging downward ------------------------- */

interface TopoNode {
  readonly id: string;
  readonly label: string;
  readonly kind: SpanRow["kind"];
  readonly x: number;
  readonly y: number;
  readonly hot?: boolean;
}

const TOPO_NODES: readonly TopoNode[] = [
  { id: "api", label: "api", kind: "graphql", x: 110, y: 26 },
  { id: "users", label: "users-svc", kind: "rest", x: 40, y: 92 },
  { id: "billing", label: "billing", kind: "grpc", x: 180, y: 92, hot: true },
  { id: "worker", label: "worker", kind: "job", x: 110, y: 154 },
  { id: "db", label: "orders.db", kind: "db", x: 110, y: 212 },
];

const TOPO_EDGES: readonly (readonly [string, string, boolean])[] = [
  ["api", "users", false],
  ["api", "billing", true],
  ["billing", "worker", true],
  ["users", "worker", false],
  ["worker", "db", false],
];

function nodeById(id: string): TopoNode {
  const n = TOPO_NODES.find((node) => node.id === id);
  if (!n) {
    throw new Error(`unknown node ${id}`);
  }
  return n;
}

function TopologyTile() {
  return (
    <Tile className="flex h-full flex-col">
      <ChromeBar route="nitro › topology" />
      <div className="flex flex-1 items-center justify-center px-4 pt-3">
        <svg
          viewBox="0 0 220 238"
          className="h-auto w-full max-w-[240px]"
          role="img"
          aria-label="Service topology for the checkout operation, with the billing gRPC hop highlighted as the hot path."
        >
          {TOPO_EDGES.map(([from, to, hot]) => {
            const a = nodeById(from);
            const b = nodeById(to);
            return (
              <line
                key={`${from}-${to}`}
                x1={a.x}
                y1={a.y}
                x2={b.x}
                y2={b.y}
                stroke={hot ? CORAL : "rgba(124,146,198,0.3)"}
                strokeWidth={hot ? 1.8 : 1}
                strokeDasharray={hot ? undefined : "3 4"}
              />
            );
          })}
          {TOPO_NODES.map((node) => (
            <g key={node.id}>
              {node.hot && (
                <circle cx={node.x} cy={node.y} r="17" fill={`${CORAL}22`}>
                  <animate
                    attributeName="r"
                    values="13;19;13"
                    dur="2.4s"
                    repeatCount="indefinite"
                  />
                </circle>
              )}
              <circle
                cx={node.x}
                cy={node.y}
                r="10"
                fill="rgba(12,19,34,0.95)"
                stroke={node.hot ? CORAL : KIND_TINT[node.kind]}
                strokeWidth={node.hot ? 1.8 : 1.3}
                style={
                  node.hot
                    ? { filter: `drop-shadow(0 0 8px ${CORAL}88)` }
                    : undefined
                }
              />
              <text
                x={node.x}
                y={node.y + 24}
                textAnchor="middle"
                fontSize="8"
                fontFamily="ui-monospace, monospace"
                fill={node.hot ? CORAL : "rgba(245,241,234,0.62)"}
              >
                {node.label}
              </text>
            </g>
          ))}
        </svg>
      </div>
      <div className="border-cc-card-border bg-cc-surface/40 flex items-center gap-2 border-t px-4 py-2.5">
        <StatusDot status="firing" />
        <span className="text-cc-ink-dim font-mono text-[0.6rem]">
          hot path: api → billing (gRPC)
        </span>
      </div>
    </Tile>
  );
}

/* ---- Tile: client lens + response-class lens stacked --------------------- */

function LensStackTile() {
  const clients = [
    { name: "web", share: 62, status: "investigating" as Status },
    { name: "ios", share: 24, status: "healthy" as Status },
    { name: "android", share: 14, status: "healthy" as Status },
  ];
  const classes = [
    { code: "2xx", pct: 99.7, color: GREEN },
    { code: "4xx", pct: 0.2, color: AMBER },
    { code: "5xx", pct: 0.1, color: CORAL },
  ];
  return (
    <Tile className="flex h-full flex-col p-5">
      <span className="text-cc-nav-label mb-4 font-mono text-[0.6rem] tracking-[0.12em] uppercase">
        by client · checkout
      </span>
      <div className="space-y-2.5">
        {clients.map((c) => (
          <div key={c.name} className="flex items-center gap-3">
            <span className="text-cc-ink-dim w-14 shrink-0 font-mono text-[0.64rem]">
              {c.name}
            </span>
            <span className="bg-cc-card-border/40 h-1.5 flex-1 overflow-hidden rounded-full">
              <span
                className="block h-full rounded-full"
                style={{
                  width: `${c.share}%`,
                  backgroundColor: STATUS_COLOR[c.status],
                }}
              />
            </span>
            <span className="text-cc-ink-dim w-8 shrink-0 text-right font-mono text-[0.62rem] tabular-nums">
              {c.share}%
            </span>
          </div>
        ))}
      </div>

      <span className="text-cc-nav-label mt-6 mb-3 font-mono text-[0.6rem] tracking-[0.12em] uppercase">
        response class
      </span>
      <div className="flex h-2.5 w-full overflow-hidden rounded-full">
        {classes.map((c) => (
          <span
            key={c.code}
            style={{
              width: `${Math.max(c.pct, 0.8)}%`,
              backgroundColor: c.color,
            }}
          />
        ))}
      </div>
      <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1">
        {classes.map((c) => (
          <span key={c.code} className="flex items-center gap-1.5">
            <span
              className="h-2 w-2 rounded-full"
              style={{ backgroundColor: c.color }}
            />
            <span className="text-cc-ink-dim font-mono text-[0.6rem] tabular-nums">
              {c.code} · {c.pct}%
            </span>
          </span>
        ))}
      </div>
      <p className="text-caption text-cc-ink-dim mt-auto pt-4">
        Web felt it, mobile stayed green. 2xx still dominates: slow, not broken.
      </p>
    </Tile>
  );
}

/* ---- Tile: signal sparkline grid ----------------------------------------- */

interface SparkProps {
  readonly label: string;
  readonly value: string;
  readonly tone: string;
  readonly data: readonly number[];
}

const SPARKS: readonly SparkProps[] = [
  {
    label: "throughput",
    value: "1.2k/m",
    tone: GREEN,
    data: [40, 44, 42, 46, 48, 45, 50, 49],
  },
  {
    label: "error rate",
    value: "0.3%",
    tone: AMBER,
    data: [4, 5, 4, 6, 8, 12, 10, 11],
  },
  {
    label: "p95",
    value: "42ms",
    tone: TEAL,
    data: [20, 22, 21, 23, 22, 24, 23, 24],
  },
  {
    label: "p99",
    value: "318ms",
    tone: CORAL,
    data: [18, 20, 19, 26, 40, 58, 72, 70],
  },
];

interface SparklineProps {
  readonly data: readonly number[];
  readonly tone: string;
}

function Sparkline({ data, tone }: SparklineProps) {
  const w = 100;
  const h = 26;
  const max = Math.max(...data);
  const min = Math.min(...data);
  const range = max - min || 1;
  const step = w / (data.length - 1);
  const line = data
    .map((d, i) => `${i * step},${h - ((d - min) / range) * (h - 4) - 2}`)
    .join(" ");
  return (
    <svg
      viewBox={`0 0 ${w} ${h}`}
      className="mt-2 h-6 w-full"
      preserveAspectRatio="none"
      aria-hidden
    >
      <polyline
        points={line}
        fill="none"
        stroke={tone}
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function SparkGridTile() {
  return (
    <Tile className="flex h-full flex-col p-5">
      <span className="text-cc-nav-label mb-3 font-mono text-[0.6rem] tracking-[0.12em] uppercase">
        signals · checkout
      </span>
      <div className="grid flex-1 grid-cols-2 gap-3">
        {SPARKS.map((s) => (
          <div
            key={s.label}
            className="border-cc-card-border/60 rounded-lg border bg-black/20 p-3"
          >
            <div className="flex items-baseline justify-between">
              <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-wide uppercase">
                {s.label}
              </span>
              <span
                className="font-mono text-[0.74rem] tabular-nums"
                style={{ color: s.tone }}
              >
                {s.value}
              </span>
            </div>
            <Sparkline data={s.data} tone={s.tone} />
          </div>
        ))}
      </div>
    </Tile>
  );
}

/* ---- Tile: full-width pull quote ----------------------------------------- */

function PullQuoteTile() {
  return (
    <Tile className="p-7 sm:p-10" glow={TEAL}>
      <p className="font-heading text-h5 text-cc-heading sm:text-h4 max-w-3xl leading-snug">
        Debugging starts from <span style={{ color: TEAL }}>evidence</span>, not
        from another dashboard project.
      </p>
      <p className="text-cc-ink-dim mt-4 max-w-2xl leading-relaxed">
        Because the telemetry is OpenTelemetry-native, the trace that proves the
        problem is the same trace your team already trusts. The dashboard spike,
        the topology, and the slow span all point at one trace-id, so you cross
        from symptom to cause without switching tools.
      </p>
    </Tile>
  );
}

/* ============================================================================
   PROOF — how the evidence gets here (C#/.NET + OTel as proof, not headline).
============================================================================ */

interface ProofItem {
  readonly k: string;
  readonly title: string;
  readonly body: string;
  readonly color: string;
}

const PROOF: readonly ProofItem[] = [
  {
    k: "01",
    title: "OpenTelemetry in, no proprietary agent",
    body: "Nitro ingests OTel traces, metrics, and logs over OTLP — the standard your services already emit, not a collector you have to adopt to be seen.",
    color: TEAL,
  },
  {
    k: "02",
    title: "One trace past the GraphQL edge",
    body: "GraphQL is the entry point, but the same trace follows your REST APIs, gRPC services, and background jobs, so the slow span shows up wherever it lives.",
    color: VIOLET,
  },
  {
    k: "03",
    title: "Logs correlated inside the trace",
    body: "Logs are stitched into the trace they belong to, so the line that explains the slow span sits right next to the span, not in a separate search.",
    color: CORAL,
  },
];

function ProofSection() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-8 rounded-3xl border p-6 backdrop-blur sm:p-10">
      <div className="max-w-2xl">
        <Eyebrow tag="evidence">how it gets here</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          The proof is your own telemetry.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          Hot Chocolate and the rest of your .NET services emit OpenTelemetry.
          Nitro reads it. Nothing here is synthetic.
        </p>
      </div>
      <div className="mt-8 grid gap-5 sm:grid-cols-3">
        {PROOF.map((p) => (
          <div
            key={p.k}
            className="border-cc-card-border relative overflow-hidden rounded-2xl border bg-black/20 p-6"
          >
            <span
              className="font-heading text-h5 tabular-nums"
              style={{ color: p.color }}
            >
              {p.k}
            </span>
            <h3 className="font-heading text-h6 text-cc-heading mt-3">
              {p.title}
            </h3>
            <p className="text-caption text-cc-ink-dim mt-2 leading-relaxed">
              {p.body}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ============================================================================
   HONESTY BEAT — keep IDE and dashboards as two distinct facts.
============================================================================ */

function HonestySection() {
  const points: readonly string[] = [
    "Telemetry is plain OpenTelemetry: it flows to Nitro and to any OTel backend you already run.",
    "The GraphQL IDE can be served straight from your Hot Chocolate endpoint.",
    "The telemetry dashboards are a separate step: they require Nitro configuration before charts light up.",
    "p95 42ms, 0.3% errors, #1 impact on checkout — the kind of numbers you read here, measured, not promised.",
  ];
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-8 grid gap-8 rounded-3xl border p-6 backdrop-blur sm:grid-cols-[0.8fr_1.2fr] sm:p-10">
      <div>
        <Eyebrow tag="scope" color={GREEN}>
          <span className="inline-flex items-center gap-2">
            <StatusDot status="healthy" />
            what is true
          </span>
        </Eyebrow>
        <h2 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
          Built on standards, honest about setup.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          Honesty is the differentiator, so here&rsquo;s the line we won&rsquo;t
          blur: serving the IDE is one switch, wiring telemetry into Nitro is a
          deliberate configuration step. Two facts, kept apart on purpose.
        </p>
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
    <section className="border-cc-card-border bg-cc-surface/40 relative mt-8 overflow-hidden rounded-3xl border px-6 py-14 text-center backdrop-blur sm:px-12 sm:py-20">
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(55% 90% at 50% -10%, rgba(94,234,212,0.16), transparent 60%)",
        }}
        aria-hidden
      />
      <Eyebrow tag="ship it" color={GREEN}>
        <span className="inline-flex items-center gap-2">
          <StatusDot status="healthy" pulse />
          eyes open
        </span>
      </Eyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h2 mx-auto mt-5 max-w-2xl">
        Know what the API is doing before the incident finds you.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-5 max-w-xl leading-relaxed">
        Operation, service, and client views with p95/p99, throughput, error
        rate, and impact score — stitched by the trace that proves the cause.
      </p>
      <div className="mt-9 flex flex-wrap justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}

/* ============================================================================
   PAGE
============================================================================ */

export default function ObservabilityPreviewV3Page() {
  return (
    <main>
      <Hero />
      <BentoMosaic />
      <ProofSection />
      <HonestySection />
      <ClosingCta />
    </main>
  );
}
