import Link from "next/link";
import type { ReactNode } from "react";

import { ArrowLink } from "@/src/components/ArrowLink";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

const HEX = {
  accent: "#5eead4",
  coral: "#f0786a",
  navLabel: "#94a3b8",
  page: "#0b0f1a",
  slo: "rgba(245, 241, 234, 0.30)",
} as const;

const TRACK = "rgba(245, 241, 234, 0.1)";

const MONO = 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

type Status = "firing" | "investigating" | "healthy";

const STATUS_HEX: Record<Status, string> = {
  firing: "#f0786a",
  investigating: "#fbbf24",
  healthy: "#34d399",
};

const STATUS_TEXT: Record<Status, string> = {
  firing: "text-cc-status-firing",
  investigating: "text-cc-status-investigating",
  healthy: "text-cc-status-healthy",
};

export function CombinedObservability() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="mx-auto max-w-3xl text-center">
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.1] font-semibold text-balance">
            See what the API is doing.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Rank reported GraphQL operations by impact and inspect traces from services configured to export supported
            OpenTelemetry data. The documented .NET setup covers REST, gRPC, and background jobs.
          </p>
          <ArrowLink href="/platform/analytics" className="mt-6">
            Learn more
          </ArrowLink>
        </div>

        <p className="text-cc-ink-dim mt-10 text-center font-mono text-[0.65rem] tracking-[0.14em] uppercase sm:mt-12">
          Illustrative incident data
        </p>

        <div className="mt-3 grid gap-4 sm:gap-5 lg:grid-cols-3">
          <ImpactFacet />
          <TraceFacet />
          <SymptomCauseFacet />
        </div>
      </RevealOnScroll>
    </section>
  );
}

interface FacetCardProps {
  readonly title: string;
  readonly href: string;
  readonly children: ReactNode;
}

function FacetCard({ title, href, children }: FacetCardProps) {
  return (
    <Link
      href={href}
      className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-5 no-underline backdrop-blur-sm transition-colors"
    >
      <h3 className="font-heading text-cc-heading text-base font-semibold">{title}</h3>
      {children}
    </Link>
  );
}

interface ImpactRow {
  readonly rank: number;
  readonly name: string;
  readonly kind: string;
  readonly impact: number;
  readonly status: Status;
}

const OPERATIONS: readonly ImpactRow[] = [
  {
    rank: 1,
    name: "checkout",
    kind: "graphql",
    impact: 98,
    status: "firing",
  },
  {
    rank: 2,
    name: "Billing.Charge",
    kind: "grpc",
    impact: 71,
    status: "investigating",
  },
  {
    rank: 3,
    name: "POST /orders",
    kind: "rest",
    impact: 46,
    status: "healthy",
  },
];

function ImpactFacet() {
  return (
    <FacetCard href="/platform/analytics" title="Rank operations by impact.">
      <div className="mt-5 space-y-2.5">
        {OPERATIONS.map((row) => {
          const pinned = row.status === "firing";
          return (
            <div
              key={row.name}
              className={`rounded-xl border p-3 ${
                pinned ? "border-cc-status-firing/30 bg-cc-status-firing/5" : "border-cc-card-border bg-cc-surface/40"
              }`}
              style={pinned ? { boxShadow: `inset 2px 0 0 ${STATUS_HEX.firing}` } : undefined}
            >
              <div className="flex items-center gap-2">
                <span
                  className={`shrink-0 font-mono text-[0.62rem] tabular-nums ${
                    pinned ? "text-cc-heading font-semibold" : "text-cc-ink-dim"
                  }`}
                >
                  #{row.rank}
                </span>
                <StatusDot status={row.status} />
                <span
                  className={`min-w-0 flex-1 truncate font-mono text-xs ${
                    pinned ? "text-cc-heading font-semibold" : "text-cc-ink"
                  }`}
                >
                  {row.name}
                </span>
                <KindTag kind={row.kind} />
                <span
                  className={`${STATUS_TEXT[row.status]} w-6 shrink-0 text-right font-mono text-xs font-semibold tabular-nums`}
                >
                  {row.impact}
                </span>
              </div>
              <div className="mt-2">
                <ImpactBar status={row.status} impact={row.impact} />
              </div>
            </div>
          );
        })}
      </div>
    </FacetCard>
  );
}

interface WaterfallSpan {
  readonly name: string;
  readonly kind: string;
  readonly left: number;
  readonly width: number;
  readonly dur: number;
  readonly tone: "root" | "healthy" | "slow";
}

const SPANS: readonly WaterfallSpan[] = [
  {
    name: "checkout",
    kind: "GraphQL",
    left: 0,
    width: 100,
    dur: 318,
    tone: "root",
  },
  {
    name: "users-svc",
    kind: "REST",
    left: 2.5,
    width: 11,
    dur: 34,
    tone: "healthy",
  },
  {
    name: "cache",
    kind: "REST",
    left: 8,
    width: 6,
    dur: 19,
    tone: "healthy",
  },
  {
    name: "billing",
    kind: "gRPC",
    left: 14.5,
    width: 63,
    dur: 201,
    tone: "slow",
  },
  {
    name: "payments",
    kind: "gRPC",
    left: 24,
    width: 40,
    dur: 127,
    tone: "healthy",
  },
  {
    name: "orders-db",
    kind: "DB",
    left: 81,
    width: 14,
    dur: 44,
    tone: "healthy",
  },
];

const BAR_FILL: Record<WaterfallSpan["tone"], string> = {
  root: "bg-cc-accent/30",
  healthy: "bg-cc-status-healthy",
  slow: "bg-cc-status-firing",
};

const DUR_TEXT: Record<WaterfallSpan["tone"], string> = {
  root: "text-cc-heading font-semibold",
  healthy: "text-cc-ink-dim",
  slow: "text-cc-status-firing font-semibold",
};

const TIME_TICKS: readonly { readonly ms: number; readonly pct: number }[] = [
  { ms: 0, pct: 0 },
  { ms: 100, pct: 31.4 },
  { ms: 200, pct: 62.9 },
  { ms: 318, pct: 100 },
];

function TraceFacet() {
  return (
    <FacetCard href="/platform/analytics" title="See where time is lost.">
      <div className="relative mt-5">
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 flex gap-2">
          <span className="w-[4.5rem] shrink-0" />
          <span className="relative flex-1">
            {TIME_TICKS.map((tick) => (
              <span
                key={tick.ms}
                className="bg-cc-card-border absolute inset-y-0 w-px"
                style={{ left: `${tick.pct}%` }}
              />
            ))}
          </span>
          <span className="w-12 shrink-0" />
        </div>

        <div className="relative space-y-3.5">
          {SPANS.map((span) => (
            <div key={span.name} className="flex items-center gap-2">
              <span className="text-cc-ink-dim w-[4.5rem] shrink-0 truncate text-right font-mono text-[0.6rem]">
                {span.name}
              </span>
              <span className="bg-cc-surface/60 relative h-3 flex-1 overflow-hidden rounded-full">
                <span
                  className={`absolute top-0 h-full rounded-full ${BAR_FILL[span.tone]}`}
                  style={{ left: `${span.left}%`, width: `${span.width}%` }}
                />
              </span>
              <span className={`w-12 shrink-0 text-right font-mono text-[0.6rem] tabular-nums ${DUR_TEXT[span.tone]}`}>
                {span.dur} ms
              </span>
            </div>
          ))}
        </div>

        <div className="relative mt-2 flex gap-2">
          <span className="w-[4.5rem] shrink-0" />
          <span className="text-cc-ink-dim relative block h-3 flex-1 font-mono text-[0.5rem] tabular-nums">
            {TIME_TICKS.map((tick) => (
              <span
                key={tick.ms}
                className="absolute top-0"
                style={
                  tick.pct === 0
                    ? { left: 0 }
                    : tick.pct === 100
                      ? { right: 0 }
                      : { left: `${tick.pct}%`, transform: "translateX(-50%)" }
                }
              >
                {tick.ms === 318 ? "318 ms" : tick.ms}
              </span>
            ))}
          </span>
          <span className="w-12 shrink-0" />
        </div>
      </div>
    </FacetCard>
  );
}

const SPARK_ID = "cmb-obs-";

const SERIES: readonly number[] = [152, 158, 150, 161, 155, 163, 157, 168, 162, 178, 205, 248, 292, 318];

const DOMAIN_MIN = 120;
const DOMAIN_MAX = 340;
const SLO_VALUE = 250;
const PLOT = { left: 10, right: 270, top: 8, bottom: 74 } as const;

type Point = readonly [number, number];

function buildSloSparkline() {
  const n = SERIES.length;
  const round = (v: number) => Math.round(v * 10) / 10;
  const xOf = (i: number) => PLOT.left + (i / (n - 1)) * (PLOT.right - PLOT.left);
  const yOf = (v: number) => PLOT.bottom - ((v - DOMAIN_MIN) / (DOMAIN_MAX - DOMAIN_MIN)) * (PLOT.bottom - PLOT.top);

  const pts: Point[] = SERIES.map((v, i) => [xOf(i), yOf(v)]);
  const sloY = yOf(SLO_VALUE);

  const toLine = (p: readonly Point[]) =>
    p.map(([x, y], i) => `${i === 0 ? "M" : "L"}${round(x)} ${round(y)}`).join(" ");
  const toArea = (p: readonly Point[]) =>
    `${toLine(p)} L${round(p[p.length - 1][0])} ${PLOT.bottom} L${round(p[0][0])} ${PLOT.bottom} Z`;

  const crossIndex = SERIES.findIndex((v) => v > SLO_VALUE);
  if (crossIndex < 1) {
    return {
      belowLine: toLine(pts),
      aboveLine: "",
      belowArea: toArea(pts),
      aboveArea: "",
      sloY: round(sloY),
      cross: [round(pts[0][0]), round(sloY)] as const,
      last: [round(pts[n - 1][0]), round(pts[n - 1][1])] as const,
    };
  }

  const prev = crossIndex - 1;
  const t = (SLO_VALUE - SERIES[prev]) / (SERIES[crossIndex] - SERIES[prev]);
  const crossX = xOf(prev) + t * (xOf(crossIndex) - xOf(prev));
  const cross: Point = [crossX, sloY];

  const below: Point[] = [...pts.slice(0, crossIndex), cross];
  const above: Point[] = [cross, ...pts.slice(crossIndex)];

  return {
    belowLine: toLine(below),
    aboveLine: toLine(above),
    belowArea: toArea(below),
    aboveArea: toArea(above),
    sloY: round(sloY),
    cross: [round(cross[0]), round(cross[1])] as const,
    last: [round(pts[n - 1][0]), round(pts[n - 1][1])] as const,
  };
}

const SPARK = buildSloSparkline();

interface CauseSpan {
  readonly name: string;
  readonly left: number;
  readonly width: number;
  readonly root?: boolean;
  readonly cause?: boolean;
  readonly delta?: string;
}

const CAUSE_SPANS: readonly CauseSpan[] = [
  { name: "checkout", left: 0, width: 100, root: true },
  {
    name: "billing.Charge",
    left: 29,
    width: 57,
    cause: true,
    delta: "+180 ms",
  },
  { name: "orders", left: 88, width: 7 },
];

function SymptomCauseFacet() {
  return (
    <FacetCard href="/platform/analytics" title="Inspect the slow spans.">
      <div className="mt-5">
        <div className="flex items-baseline justify-between gap-3">
          <span className="text-cc-ink-dim font-mono text-[0.58rem] tracking-[0.12em] uppercase">checkout p99</span>
          <span className="text-cc-status-firing font-mono text-sm font-semibold tabular-nums">318 ms</span>
        </div>
        <div className="mt-2">
          <svg
            viewBox="0 0 280 84"
            width="100%"
            role="img"
            aria-label="p99 latency holds near 155 ms, then climbs sharply across the 250 ms SLO line to 318 ms in breach"
            style={{ display: "block", overflow: "visible" }}
          >
            <defs>
              <linearGradient
                id={`${SPARK_ID}calm-fill`}
                x1="0"
                y1={PLOT.top}
                x2="0"
                y2={PLOT.bottom}
                gradientUnits="userSpaceOnUse"
              >
                <stop offset="0" stopColor={HEX.accent} stopOpacity="0.14" />
                <stop offset="1" stopColor={HEX.accent} stopOpacity="0" />
              </linearGradient>
              <linearGradient
                id={`${SPARK_ID}breach-fill`}
                x1="0"
                y1={PLOT.top}
                x2="0"
                y2={PLOT.bottom}
                gradientUnits="userSpaceOnUse"
              >
                <stop offset="0" stopColor={HEX.coral} stopOpacity="0.26" />
                <stop offset="1" stopColor={HEX.coral} stopOpacity="0" />
              </linearGradient>
            </defs>

            <path d={SPARK.belowArea} fill={`url(#${SPARK_ID}calm-fill)`} />
            <path d={SPARK.aboveArea} fill={`url(#${SPARK_ID}breach-fill)`} />

            <line
              x1={PLOT.left - 2}
              y1={SPARK.sloY}
              x2={PLOT.right + 2}
              y2={SPARK.sloY}
              stroke={HEX.slo}
              strokeWidth="1"
              strokeDasharray="3 3"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={PLOT.left}
              y={SPARK.sloY - 5}
              fontFamily={MONO}
              fontSize="8"
              letterSpacing="0.06em"
              fill={HEX.navLabel}
            >
              SLO 250 ms
            </text>

            <path
              d={SPARK.belowLine}
              fill="none"
              stroke={HEX.accent}
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            />
            <path
              d={SPARK.aboveLine}
              fill="none"
              stroke={HEX.coral}
              strokeWidth="1.7"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            />

            <circle
              cx={SPARK.cross[0]}
              cy={SPARK.cross[1]}
              r="2.4"
              fill={HEX.page}
              stroke={HEX.coral}
              strokeWidth="1.4"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx={SPARK.last[0]} cy={SPARK.last[1]} r="6" fill={HEX.coral} fillOpacity="0.16" />
            <circle cx={SPARK.last[0]} cy={SPARK.last[1]} r="2.8" fill={HEX.coral} />
          </svg>
        </div>
      </div>

      <div className="mt-5 space-y-2">
        {CAUSE_SPANS.map((span) => (
          <div key={span.name} className="flex items-center gap-2">
            <span
              className={`w-[4.5rem] shrink-0 truncate text-right font-mono text-[0.58rem] ${
                span.cause ? "text-cc-status-firing" : span.root ? "text-cc-ink" : "text-cc-ink-dim"
              }`}
            >
              {span.name}
            </span>
            <span className="bg-cc-surface relative h-2 flex-1 overflow-hidden rounded-full">
              <span
                className={`absolute top-0 h-full rounded-full ${
                  span.cause ? "bg-cc-status-firing" : span.root ? "bg-cc-accent/25" : "bg-cc-accent/70"
                }`}
                style={{ left: `${span.left}%`, width: `${span.width}%` }}
              />
            </span>
            <span
              className={`w-12 shrink-0 text-right font-mono text-[0.58rem] tabular-nums ${
                span.cause ? "text-cc-status-firing" : "text-transparent"
              }`}
            >
              {span.delta ?? ""}
            </span>
          </div>
        ))}
      </div>
    </FacetCard>
  );
}

interface StatusDotProps {
  readonly status: Status;
}

function StatusDot({ status }: StatusDotProps) {
  const color = STATUS_HEX[status];
  return (
    <svg width={12} height={12} viewBox="0 0 14 14" aria-hidden="true" className="shrink-0">
      <circle cx={7} cy={7} r={5} fill={`${color}22`} stroke={color} strokeWidth={1} />
      <circle cx={7} cy={7} r={2.3} fill={color} />
    </svg>
  );
}

interface KindTagProps {
  readonly kind: string;
}

function KindTag({ kind }: KindTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim hidden shrink-0 rounded border px-1.5 py-0.5 font-mono text-[0.55rem] tracking-[0.04em] sm:inline">
      {kind}
    </span>
  );
}

interface ImpactBarProps {
  readonly status: Status;
  readonly impact: number;
}

function ImpactBar({ status, impact }: ImpactBarProps) {
  return (
    <svg viewBox="0 0 100 5" width="100%" height={5} preserveAspectRatio="none" aria-hidden="true" className="block">
      <rect x={0} y={0} width={100} height={5} rx={2.5} fill={TRACK} />
      <rect x={0} y={0} width={impact} height={5} rx={2.5} fill={STATUS_HEX[status]} />
    </svg>
  );
}
