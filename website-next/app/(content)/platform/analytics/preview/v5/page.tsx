import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * The Constellation. Visual-hero family.
 * Single giant inline-SVG diagram opposite sparse copy on cc-bg, then a
 * vertical stack of full-width cc-surface tiles with cc-card-border.
 * cc-accent (teal) is the only accent. Coral is rationed to two places:
 * the one slow gRPC span and the #1 impact row.
 * ------------------------------------------------------------------ */

const ACCENT = "#5eead4";
const ACCENT_SOFT = "#5eead422";
const ACCENT_LINE = "#5eead455";
const CORAL = "#f0786a";
const INK_DIM = "rgba(245, 241, 234, 0.62)";
const INK_FAINT = "rgba(245, 241, 234, 0.16)";

export const metadata: Metadata = {
  title: "Nitro Analytics: The Operation Constellation",
  description:
    "GraphQL observability for .NET: rank operations by impact, follow distributed traces span by span, see per-client usage, all from OpenTelemetry data you own.",
  keywords: [
    "GraphQL observability for .NET",
    "GraphQL analytics",
    "OpenTelemetry .NET",
    "Nitro dashboard",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "Hot Chocolate telemetry",
  ],
  openGraph: {
    title: "Nitro Analytics: The Operation Constellation",
    description:
      "GraphQL observability for .NET: ranked impact, traced spans, per-client usage, all on OpenTelemetry.",
  },
  robots: { index: false, follow: false },
};

export default function AnalyticsPreviewV5Page() {
  return (
    <main className="flex flex-col gap-24 pb-20">
      <Hero />
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-24 px-4">
        <ImpactTile />
        <TraceTile />
        <ClientTile />
        <CrossServiceTile />
        <HonestyTile />
      </div>
      <ClosingCta />
    </main>
  );
}

/* ================================================================== *
 * HERO
 * 40/60 split. Left: eyebrow, two-line text-hero, lede, dual CTA, stat
 * strip. Right: the constellation diagram, the page signature.
 * ================================================================== */

function Hero() {
  return (
    <section className="relative isolate pt-8">
      <div className="relative mx-auto grid max-w-7xl gap-12 px-4 lg:grid-cols-[2fr_3fr] lg:items-center lg:gap-16">
        <div>
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
            Nitro Observability
          </span>
          <h1 className="font-heading text-hero text-cc-heading mt-6 leading-[0.95]">
            See every span.
            <br />
            Rank what hurts.
          </h1>
          <p className="lead text-cc-prose mt-7 max-w-md">
            GraphQL observability for .NET. Operation, service, and per-client
            views on OpenTelemetry data you own, with p95, p99, and an impact
            score that surfaces the request worth opening first.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-4">
            <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
              Get Started
            </SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch
            </OutlineButton>
          </div>
          <HeroStatStrip />
        </div>
        <div className="relative">
          <ConstellationDiagram />
        </div>
      </div>
    </section>
  );
}

function HeroStatStrip() {
  return (
    <div className="border-cc-card-border mt-10 flex flex-wrap items-center gap-x-6 gap-y-2 border-t pt-5">
      <span className="text-cc-nav-label flex items-center gap-2 font-mono text-[11px]">
        <span
          className="h-1.5 w-1.5 rounded-full"
          style={{ backgroundColor: ACCENT }}
        />
        <span className="tracking-wide uppercase">p95 / p99</span>
        <span className="text-cc-ink-dim">live</span>
      </span>
      <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
      <span className="text-cc-nav-label flex items-center gap-2 font-mono text-[11px]">
        <span
          className="h-1.5 w-1.5 rounded-full"
          style={{ backgroundColor: ACCENT }}
        />
        <span className="tracking-wide uppercase">impact</span>
        <span className="text-cc-ink-dim">ranked</span>
      </span>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Constellation diagram. One inline SVG, ~46vw wide / 520px tall.
 * Center: GraphQL operation node. Five satellites on an orbit, with
 * gRPC billing enlarged and pulse-ringed as the slow hop. Curved
 * cc-accent strokes, dashed orbit paths, span-duration micro-labels.
 * ------------------------------------------------------------------ */

interface Satellite {
  readonly id: string;
  readonly kind: "GraphQL" | "REST" | "gRPC" | "DB" | "Job";
  readonly label: string;
  readonly ms: string;
  readonly cx: number;
  readonly cy: number;
  readonly r: number;
  readonly slow?: boolean;
}

const SATELLITES: readonly Satellite[] = [
  {
    id: "s1",
    kind: "GraphQL",
    label: "schema.execute",
    ms: "12ms",
    cx: 560,
    cy: 90,
    r: 22,
  },
  {
    id: "s2",
    kind: "REST",
    label: "users-svc /me",
    ms: "21ms",
    cx: 700,
    cy: 250,
    r: 24,
  },
  {
    id: "s3",
    kind: "gRPC",
    label: "billing.Charge()",
    ms: "201ms",
    cx: 590,
    cy: 430,
    r: 40,
    slow: true,
  },
  {
    id: "s4",
    kind: "DB",
    label: "SELECT account",
    ms: "9ms",
    cx: 240,
    cy: 430,
    r: 22,
  },
  {
    id: "s5",
    kind: "Job",
    label: "enqueue receipt",
    ms: "37ms",
    cx: 130,
    cy: 250,
    r: 24,
  },
];

const CENTER_X = 420;
const CENTER_Y = 260;

function ConstellationDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          trace 4b1c8f2a · mutation checkout
        </span>
        <span className="font-mono text-[11px]" style={{ color: ACCENT }}>
          318ms
        </span>
      </div>
      <svg
        viewBox="0 0 840 520"
        role="img"
        aria-label="Operation constellation: central GraphQL node connected to five satellite services. The gRPC billing satellite is enlarged as the slow hop."
        className="block h-auto w-full"
      >
        <defs>
          <radialGradient id="cc-v5-core-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={ACCENT} stopOpacity="0.35" />
            <stop offset="60%" stopColor={ACCENT} stopOpacity="0.08" />
            <stop offset="100%" stopColor={ACCENT} stopOpacity="0" />
          </radialGradient>
          <radialGradient id="cc-v5-slow-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CORAL} stopOpacity="0.4" />
            <stop offset="100%" stopColor={CORAL} stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* dashed orbit paths */}
        <ellipse
          cx={CENTER_X}
          cy={CENTER_Y}
          rx={210}
          ry={150}
          fill="none"
          stroke={INK_FAINT}
          strokeWidth={1}
          strokeDasharray="3 6"
        />
        <ellipse
          cx={CENTER_X}
          cy={CENTER_Y}
          rx={300}
          ry={205}
          fill="none"
          stroke={INK_FAINT}
          strokeWidth={1}
          strokeDasharray="3 6"
        />

        {/* core glow */}
        <circle
          cx={CENTER_X}
          cy={CENTER_Y}
          r={130}
          fill="url(#cc-v5-core-glow)"
        />

        {/* slow-hop pulse halo behind the gRPC satellite */}
        <circle cx={590} cy={430} r={90} fill="url(#cc-v5-slow-glow)" />

        {/* curved spokes from center to each satellite */}
        {SATELLITES.map((s) => (
          <CurvedSpoke key={s.id} sat={s} />
        ))}

        {/* core node */}
        <g>
          <circle
            cx={CENTER_X}
            cy={CENTER_Y}
            r={44}
            fill="#0c1322"
            stroke={ACCENT}
            strokeWidth={2}
          />
          <circle
            cx={CENTER_X}
            cy={CENTER_Y}
            r={32}
            fill={ACCENT_SOFT}
            stroke={ACCENT_LINE}
            strokeWidth={1}
          />
          <text
            x={CENTER_X}
            y={CENTER_Y - 4}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="10"
            fontWeight="600"
            fill={ACCENT}
            letterSpacing="1.5"
          >
            GRAPHQL
          </text>
          <text
            x={CENTER_X}
            y={CENTER_Y + 12}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            checkout
          </text>
        </g>

        {/* satellite nodes */}
        {SATELLITES.map((s) => (
          <SatelliteNode key={s.id} sat={s} />
        ))}
      </svg>
    </div>
  );
}

interface CurvedSpokeProps {
  readonly sat: Satellite;
}

function CurvedSpoke({ sat }: CurvedSpokeProps) {
  const midX = (CENTER_X + sat.cx) / 2;
  const midY = (CENTER_Y + sat.cy) / 2;
  const dx = sat.cx - CENTER_X;
  const dy = sat.cy - CENTER_Y;
  const len = Math.hypot(dx, dy) || 1;
  const nx = -dy / len;
  const ny = dx / len;
  const bow = 40;
  const ctrlX = midX + nx * bow;
  const ctrlY = midY + ny * bow;
  const stroke = sat.slow ? CORAL : ACCENT_LINE;
  const width = sat.slow ? 2 : 1.2;
  return (
    <path
      d={`M ${CENTER_X} ${CENTER_Y} Q ${ctrlX} ${ctrlY} ${sat.cx} ${sat.cy}`}
      fill="none"
      stroke={stroke}
      strokeWidth={width}
      strokeLinecap="round"
      opacity={sat.slow ? 0.95 : 0.7}
    />
  );
}

interface SatelliteNodeProps {
  readonly sat: Satellite;
}

function SatelliteNode({ sat }: SatelliteNodeProps) {
  const stroke = sat.slow ? CORAL : ACCENT;
  const fillRing = sat.slow ? "#1a0e0d" : "#0c1322";
  const labelY = sat.cy + sat.r + 18;
  const msY = labelY + 14;
  return (
    <g>
      {sat.slow && (
        <circle
          cx={sat.cx}
          cy={sat.cy}
          r={sat.r + 10}
          fill="none"
          stroke={CORAL}
          strokeWidth={1}
          opacity={0.55}
        />
      )}
      <circle
        cx={sat.cx}
        cy={sat.cy}
        r={sat.r}
        fill={fillRing}
        stroke={stroke}
        strokeWidth={sat.slow ? 2 : 1.5}
      />
      <text
        x={sat.cx}
        y={sat.cy + 4}
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fontWeight="600"
        fill={stroke}
        letterSpacing="1"
      >
        {sat.kind.toUpperCase()}
      </text>
      <text
        x={sat.cx}
        y={labelY}
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="#f5f0ea"
      >
        {sat.label}
      </text>
      <text
        x={sat.cx}
        y={msY}
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill={sat.slow ? CORAL : INK_DIM}
      >
        {sat.ms}
      </text>
    </g>
  );
}

/* ================================================================== *
 * SHARED TILE
 * ================================================================== */

interface TileProps {
  readonly eyebrow: string;
  readonly heading: string;
  readonly headingLevel?: "h3" | "h4";
  readonly lede: React.ReactNode;
  readonly children: React.ReactNode;
}

function Tile({
  eyebrow,
  heading,
  headingLevel = "h3",
  lede,
  children,
}: TileProps) {
  const headingClass =
    headingLevel === "h4"
      ? "font-heading text-h4 text-cc-heading mt-4"
      : "font-heading text-h3 text-cc-heading mt-4";
  return (
    <section className="border-cc-card-border bg-cc-surface/80 rounded-2xl border px-6 py-10 backdrop-blur-md sm:px-10">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        {eyebrow}
      </span>
      <h2 className={headingClass}>{heading}</h2>
      <p className="text-body text-cc-ink-dim mt-5 max-w-3xl">{lede}</p>
      <div className="mt-8">{children}</div>
    </section>
  );
}

/* ================================================================== *
 * OPERATION IMPACT TILE
 * Compact 4-row impact table. #1 row outlined in cc-accent.
 * ================================================================== */

interface OpRow {
  readonly rank: number;
  readonly name: string;
  readonly p95: string;
  readonly p99: string;
  readonly rpm: string;
  readonly err: string;
  readonly impact: number;
}

const OP_ROWS: readonly OpRow[] = [
  {
    rank: 1,
    name: "checkout",
    p95: "42ms",
    p99: "318ms",
    rpm: "9.4k",
    err: "0.3%",
    impact: 94,
  },
  {
    rank: 2,
    name: "cartSummary",
    p95: "31ms",
    p99: "88ms",
    rpm: "12.1k",
    err: "0.1%",
    impact: 71,
  },
  {
    rank: 3,
    name: "productList",
    p95: "12ms",
    p99: "27ms",
    rpm: "18.6k",
    err: "0.0%",
    impact: 38,
  },
  {
    rank: 4,
    name: "userProfile",
    p95: "8ms",
    p99: "19ms",
    rpm: "5.2k",
    err: "0.0%",
    impact: 22,
  },
];

function ImpactTile() {
  return (
    <Tile
      eyebrow="Operation insights"
      heading="Ranked by what hurts, not what is loud."
      lede="The Nitro impact score combines p95, p99, throughput, and error rate into one number, so the operation at the top owes you attention. Drill into latency distributions, throughput, and 5xx share from the same row."
    >
      <ImpactTable />
    </Tile>
  );
}

function ImpactTable() {
  return (
    <div className="border-cc-card-border overflow-hidden rounded-xl border">
      <div className="bg-cc-code-header/70 text-cc-nav-label border-cc-card-border/60 grid grid-cols-[28px_1fr_56px_56px_56px_56px_72px] gap-3 border-b px-4 py-2.5 font-mono text-[10px] tracking-wide uppercase">
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
          <ImpactRow key={row.name} row={row} />
        ))}
      </div>
    </div>
  );
}

interface ImpactRowProps {
  readonly row: OpRow;
}

function ImpactRow({ row }: ImpactRowProps) {
  const isTop = row.rank === 1;
  return (
    <div
      className="grid grid-cols-[28px_1fr_56px_56px_56px_56px_72px] items-center gap-3 px-4 py-3 font-mono text-[12px]"
      style={
        isTop
          ? {
              boxShadow: `inset 0 0 0 1px ${ACCENT}`,
              backgroundColor: "rgba(94, 234, 212, 0.05)",
            }
          : undefined
      }
    >
      <span className="text-cc-nav-label text-[11px]">#{row.rank}</span>
      <span style={isTop ? { color: ACCENT } : { color: INK_DIM }}>
        {row.name}
      </span>
      <span className="text-cc-ink-dim text-right">{row.p95}</span>
      <span className="text-right" style={{ color: isTop ? CORAL : INK_DIM }}>
        {row.p99}
      </span>
      <span className="text-cc-ink-dim text-right">{row.rpm}</span>
      <span className="text-cc-ink-dim text-right">{row.err}</span>
      <span className="flex items-center justify-end gap-1.5">
        <ImpactBar value={row.impact} top={isTop} />
        <span className="text-cc-ink-dim w-6 text-right text-[11px]">
          {row.impact}
        </span>
      </span>
    </div>
  );
}

interface ImpactBarProps {
  readonly value: number;
  readonly top: boolean;
}

function ImpactBar({ value, top }: ImpactBarProps) {
  return (
    <span className="bg-cc-surface/80 relative inline-block h-1.5 w-12 overflow-hidden rounded-full">
      <span
        className="absolute inset-y-0 left-0 rounded-full"
        style={{
          width: `${value}%`,
          backgroundColor: top ? ACCENT : ACCENT_LINE,
        }}
      />
    </span>
  );
}

/* ================================================================== *
 * DISTRIBUTED TRACE TILE
 * Trimmed 5-span waterfall. Slow gRPC accented in coral, others accent.
 * ================================================================== */

interface Span {
  readonly id: string;
  readonly label: string;
  readonly kind: "GraphQL" | "REST" | "gRPC" | "DB" | "Job";
  readonly start: number;
  readonly width: number;
  readonly ms: string;
  readonly slow?: boolean;
}

const SPANS: readonly Span[] = [
  {
    id: "s0",
    label: "mutation checkout",
    kind: "GraphQL",
    start: 0,
    width: 100,
    ms: "318ms",
  },
  {
    id: "s1",
    label: "api → users-svc /me",
    kind: "REST",
    start: 4,
    width: 11,
    ms: "21ms",
  },
  {
    id: "s2",
    label: "users-svc → billing.Charge()",
    kind: "gRPC",
    start: 16,
    width: 64,
    ms: "201ms",
    slow: true,
  },
  {
    id: "s3",
    label: "billing → SELECT account",
    kind: "DB",
    start: 20,
    width: 12,
    ms: "9ms",
  },
  {
    id: "s4",
    label: "billing → enqueue receipt",
    kind: "Job",
    start: 82,
    width: 13,
    ms: "37ms",
  },
];

const SPAN_DEPTH: readonly number[] = [0, 1, 1, 2, 2];

function TraceTile() {
  return (
    <Tile
      eyebrow="Distributed tracing"
      heading="From the spike to the slow span in one click."
      lede="From the impact row, Nitro opens the trace. Every hop is a real OpenTelemetry span: GraphQL at the root, REST and gRPC across services, the database read, the job enqueued at the end. The hop that is actually slow is highlighted."
    >
      <TraceWaterfall />
    </Tile>
  );
}

function TraceWaterfall() {
  return (
    <div className="border-cc-card-border overflow-hidden rounded-xl border">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
        <span className="font-mono text-[11px]" style={{ color: ACCENT }}>
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
  const isRoot = span.kind === "GraphQL";
  const barColor = span.slow ? CORAL : ACCENT;
  return (
    <div className="flex items-center gap-3">
      <div
        className="flex w-[38%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: depth * 14 }}
      >
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[9px] font-semibold tracking-wide uppercase"
          style={{
            color: span.slow ? CORAL : ACCENT,
            backgroundColor: span.slow ? `${CORAL}1a` : ACCENT_SOFT,
          }}
        >
          {span.kind}
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
        <div
          className="absolute top-1/2 flex h-4 -translate-y-1/2 items-center rounded-[3px]"
          style={{
            left: `${span.start}%`,
            width: `${span.width}%`,
            backgroundColor: barColor,
            opacity: span.slow ? 1 : 0.7,
            boxShadow: span.slow ? `0 0 16px ${CORAL}55` : undefined,
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
 * PER-CLIENT TILE
 * One horizontal share bar plus 4 rows with name@version, share %, rpm.
 * ================================================================== */

interface ClientRow {
  readonly name: string;
  readonly share: number;
  readonly rpm: string;
  readonly highlight?: boolean;
}

const CLIENT_ROWS: readonly ClientRow[] = [
  { name: "web-storefront@4.2.0", share: 61, rpm: "5.7k", highlight: true },
  { name: "ios-app@3.8.1", share: 27, rpm: "2.5k" },
  { name: "android-app@3.5.0", share: 9, rpm: "0.8k" },
  { name: "partner-api@1.0", share: 3, rpm: "0.4k" },
];

function ClientTile() {
  return (
    <Tile
      eyebrow="Per-client usage"
      heading="Which published clients call this, and how often."
      lede="Nitro registers your clients by name and version. The same telemetry that fuels the impact table breaks down by caller, so you can see published clients affected before you ship a fix, and which versions still hit a deprecated field."
    >
      <ClientShareView />
    </Tile>
  );
}

function ClientShareView() {
  return (
    <div className="border-cc-card-border overflow-hidden rounded-xl border">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          clients · checkout · 1h
        </span>
        <span className="text-cc-ink-faint font-mono text-[10px]">nitro</span>
      </div>
      <div className="px-5 py-5">
        <div className="flex h-2 overflow-hidden rounded-full">
          {CLIENT_ROWS.map((c, i) => (
            <span
              key={c.name}
              style={{
                width: `${c.share}%`,
                backgroundColor: ACCENT,
                opacity: 1 - i * 0.18,
              }}
            />
          ))}
        </div>
        <div className="mt-5 space-y-2">
          {CLIENT_ROWS.map((c) => (
            <div
              key={c.name}
              className="flex items-center gap-3 rounded-lg px-2.5 py-2"
              style={
                c.highlight
                  ? { backgroundColor: "rgba(94, 234, 212, 0.06)" }
                  : { backgroundColor: "rgba(12, 19, 34, 0.4)" }
              }
            >
              <span
                className="h-1.5 w-1.5 shrink-0 rounded-full"
                style={{
                  backgroundColor: ACCENT,
                  opacity: c.highlight ? 1 : 0.6,
                }}
              />
              <span
                className={`flex-1 truncate font-mono text-[12px] ${
                  c.highlight ? "text-cc-heading" : "text-cc-ink-dim"
                }`}
              >
                {c.name}
              </span>
              <span className="bg-cc-surface/80 relative inline-block h-1.5 w-20 overflow-hidden rounded-full">
                <span
                  className="absolute inset-y-0 left-0 rounded-full"
                  style={{
                    width: `${c.share}%`,
                    backgroundColor: ACCENT,
                    opacity: c.highlight ? 1 : 0.55,
                  }}
                />
              </span>
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
          Drill into any client to see which operations and which schema
          versions it touches.
        </p>
      </div>
    </div>
  );
}

/* ================================================================== *
 * CROSS-SERVICE TILE
 * Inline 4-up mini-legend (GraphQL / REST / gRPC / Jobs) styled as tags
 * inside the same tile. No subcards.
 * ================================================================== */

interface ServiceTag {
  readonly label: string;
  readonly note: string;
  readonly count: string;
}

const SERVICE_TAGS: readonly ServiceTag[] = [
  {
    label: "GraphQL",
    note: "Hot Chocolate, source-generated",
    count: "1 gateway",
  },
  { label: "REST", note: "ASP.NET Core endpoints", count: "6 services" },
  { label: "gRPC", note: "service-to-service calls", count: "4 services" },
  { label: "Jobs", note: "queued work, scheduled tasks", count: "3 workers" },
];

function CrossServiceTile() {
  return (
    <Tile
      eyebrow="Cross-service .NET monitoring"
      heading="One pane for every .NET service in the trace."
      lede={
        <>
          The OpenTelemetry pipeline is the same across GraphQL, REST, gRPC, and
          your background workers, so a single trace can carry the whole call.
          Wire your services through{" "}
          <code className="text-cc-ink font-mono">
            ChilliCream.Nitro.OpenTelemetry
          </code>
          , no proprietary agent in the middle.
        </>
      }
    >
      <div className="flex flex-wrap items-center gap-3">
        {SERVICE_TAGS.map((t) => (
          <span
            key={t.label}
            className="border-cc-card-border bg-cc-surface/60 inline-flex items-center gap-2.5 rounded-full border px-3.5 py-1.5"
          >
            <span
              className="rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold tracking-wide uppercase"
              style={{ color: ACCENT, backgroundColor: ACCENT_SOFT }}
            >
              {t.label}
            </span>
            <span className="text-cc-ink-dim text-[12px]">{t.note}</span>
            <span className="text-cc-nav-label font-mono text-[10px]">
              {t.count}
            </span>
          </span>
        ))}
      </div>
    </Tile>
  );
}

/* ================================================================== *
 * HONESTY TILE
 * Three short paragraphs separated by hairline cc-card-border dividers.
 * text-h4 to signal lower visual weight.
 * ================================================================== */

function HonestyTile() {
  return (
    <Tile
      eyebrow="Straight about what it is"
      heading="Honest about setup, precise about payoff."
      headingLevel="h4"
      lede="Three facts kept apart, so the picture above stays accurate."
    >
      <div className="divide-cc-card-border divide-y">
        <HonestyRow title="Telemetry needs configuration">
          The views above come from telemetry you point at Nitro. It is a
          configuration step in your services, deliberate and documented, not
          something that turns on by itself.
        </HonestyRow>
        <HonestyRow title="OpenTelemetry underneath">
          Vendor-neutral spans end to end. Your data stays yours, there is no
          proprietary agent in the trace, and you can keep exporting the same
          spans anywhere else you already send them.
        </HonestyRow>
        <HonestyRow title="The IDE is a separate thing">
          The GraphQL IDE can be served from your Hot Chocolate endpoint. That
          is independent of the telemetry views here. Two capabilities, kept
          apart.
        </HonestyRow>
      </div>
    </Tile>
  );
}

interface HonestyRowProps {
  readonly title: string;
  readonly children: React.ReactNode;
}

function HonestyRow({ title, children }: HonestyRowProps) {
  return (
    <div className="flex flex-col gap-2 py-5 first:pt-0 last:pb-0 sm:flex-row sm:gap-8">
      <div className="flex shrink-0 items-center gap-2 sm:w-64 sm:pt-0.5">
        <span style={{ color: ACCENT }}>
          <CheckIcon size={14} />
        </span>
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
      </div>
      <p className="text-caption text-cc-ink-dim flex-1">{children}</p>
    </div>
  );
}

/* ================================================================== *
 * CLOSING CTA
 * Wide cc-surface strip. A flattened constellation-arc SVG runs as a
 * horizon divider at the top, then text-h2 + body + dual CTA.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/80 relative mx-auto w-full max-w-6xl overflow-hidden rounded-2xl border px-6 pt-24 pb-14 text-center backdrop-blur-md sm:px-12">
      <HorizonArc />
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Nitro Observability
      </span>
      <h2 className="font-heading text-h2 text-cc-heading mt-5">
        Point telemetry at Nitro once.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Every request becomes evidence: ranked by impact, traced end to end,
        sliced by client and by .NET service.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Get Started
        </SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
    </section>
  );
}

function HorizonArc() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-x-0 top-0 h-24"
    >
      <svg
        viewBox="0 0 1200 120"
        preserveAspectRatio="none"
        className="block h-full w-full"
      >
        <defs>
          <linearGradient id="cc-v5-arc" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor={ACCENT} stopOpacity="0" />
            <stop offset="50%" stopColor={ACCENT} stopOpacity="0.7" />
            <stop offset="100%" stopColor={ACCENT} stopOpacity="0" />
          </linearGradient>
        </defs>
        {/* horizon arc echoing the constellation orbit, flattened */}
        <path
          d="M 0 90 Q 600 -10 1200 90"
          fill="none"
          stroke="url(#cc-v5-arc)"
          strokeWidth={1.4}
        />
        <path
          d="M 0 100 Q 600 30 1200 100"
          fill="none"
          stroke={INK_FAINT}
          strokeWidth={1}
          strokeDasharray="3 6"
        />
        {/* satellite markers flattened onto the horizon */}
        <circle cx={180} cy={78} r={4} fill={ACCENT} opacity={0.6} />
        <circle cx={360} cy={62} r={4} fill={ACCENT} opacity={0.7} />
        <circle cx={600} cy={48} r={6} fill={ACCENT} />
        <circle cx={840} cy={62} r={5} fill={CORAL} />
        <circle cx={1020} cy={78} r={4} fill={ACCENT} opacity={0.6} />
      </svg>
    </div>
  );
}
