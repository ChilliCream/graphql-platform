"use client";

import type { CSSProperties, ReactNode } from "react";

import { ControlPlaneConsole } from "@/src/components/nitro/ControlPlaneConsole";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  BarSeries,
  ChartPanel,
  CountUp,
  HBarSeries,
  InsightsTable,
  LineAreaChart,
  NitroCompose,
  NitroDiagnose,
  NitroFusion,
  NitroReel,
  NitroSchema,
  NitroTheme,
  NitroTrace,
  Sparkline,
  token,
  TraceWaterfall,
} from "@/src/nitro";
import type { Client, InsightRow, Trace } from "@/src/nitro/lib/data/types";

// Brand spectrum gradient. Used sparingly as the single "color event" per Linear.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

/* ────────────────────────────────────────────────────────────────────────
   Shared shells
   ──────────────────────────────────────────────────────────────────────── */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-medium tracking-[0.22em] uppercase">
      {children}
    </span>
  );
}

interface SectionIntroProps {
  readonly index?: string;
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly lead?: string;
  readonly align?: "center" | "start";
}

function SectionIntro({
  index,
  eyebrow,
  title,
  lead,
  align = "center",
}: SectionIntroProps) {
  return (
    <div
      className={[
        "flex flex-col gap-4",
        align === "center" ? "mx-auto max-w-2xl text-center" : "max-w-md",
      ].join(" ")}
    >
      <div
        className={[
          "flex items-center gap-3",
          align === "center" ? "justify-center" : "",
        ].join(" ")}
      >
        {index && (
          <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
            {index}
          </span>
        )}
        <Eyebrow>{eyebrow}</Eyebrow>
      </div>
      <h2 className="text-cc-heading font-heading text-h3 text-balance">
        {title}
      </h2>
      {lead && (
        <p className="text-cc-ink text-base leading-relaxed text-pretty sm:text-lg">
          {lead}
        </p>
      )}
    </div>
  );
}

interface CardProps {
  readonly className?: string;
  readonly children: ReactNode;
  readonly glow?: boolean;
}

/** Hairline-bordered, softly rounded, backdrop-blurred surface (Linear card). */
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
  readonly index: string;
  readonly title: string;
  readonly hint?: string;
}

function CardHeader({ index, title, hint }: CardHeaderProps) {
  return (
    <div className="flex items-baseline justify-between gap-3 px-5 pt-5">
      <div className="flex items-center gap-2.5">
        <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
          {index}
        </span>
        <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
      </div>
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

/** Frames a chrome-less Nitro product screen like an embedded screenshot. */
interface FramedVisualProps {
  readonly children: ReactNode;
}

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

interface ShowcaseProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly visual: ReactNode;
  readonly aside?: ReactNode;
  readonly reverse?: boolean;
}

/** Alternating split: short headline + graphic, Linear feature-row rhythm. */
function Showcase({
  id,
  index,
  eyebrow,
  title,
  body,
  visual,
  aside,
  reverse = false,
}: ShowcaseProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <RevealOnScroll
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex flex-col gap-5">
            <SectionIntro
              index={index}
              eyebrow={eyebrow}
              title={title}
              align="start"
            />
            <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
              {body}
            </p>
            {aside}
          </div>
        </RevealOnScroll>

        <RevealOnScroll
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
          hiddenClassName="translate-y-8 opacity-0"
        >
          <FramedVisual>{visual}</FramedVisual>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Fixtures for the bento chart primitives
   ──────────────────────────────────────────────────────────────────────── */

const P95_SERIES = [
  38, 41, 39, 44, 42, 47, 45, 43, 40, 42, 46, 49, 44, 41, 43, 42, 45, 48, 44,
  40, 39, 42, 44, 41,
];
const P99_SERIES = [
  92, 96, 89, 104, 98, 118, 132, 121, 108, 99, 112, 141, 168, 128, 110, 104,
  118, 152, 137, 112, 101, 108, 116, 106,
];
const THROUGHPUT_BARS = [
  32, 41, 38, 47, 52, 58, 61, 66, 72, 78, 74, 81, 88, 92, 86, 94,
];
const ERROR_SERIES = [
  0.2, 0.3, 0.2, 0.4, 0.3, 0.6, 1.1, 0.9, 0.5, 0.3, 0.4, 0.8, 1.6, 0.7, 0.4,
  0.3, 0.5, 0.9, 0.6, 0.3,
];

const CLIENTS: readonly Client[] = [
  { name: "web-storefront", total: 184000, impact: 94 },
  { name: "mobile-ios", total: 121000, impact: 71 },
  { name: "partner-api", total: 68000, impact: 58 },
  { name: "admin-console", total: 24000, impact: 33 },
  { name: "analytics-etl", total: 12000, impact: 18 },
];

const INSIGHTS: readonly InsightRow[] = [
  {
    id: "op-checkout",
    spanKind: "server",
    name: "mutation checkout",
    averageLatency: 168,
    opm: 1240,
    errorRate: 0.031,
    impact: 98,
    latencySeries: [42, 48, 61, 88, 132, 168, 141],
    throughputSeries: [820, 910, 1040, 1180, 1210, 1240, 1190],
  },
  {
    id: "op-cart",
    spanKind: "server",
    name: "query cart",
    averageLatency: 44,
    opm: 4820,
    errorRate: 0.004,
    impact: 61,
    latencySeries: [38, 41, 44, 42, 46, 44, 41],
    throughputSeries: [4200, 4500, 4700, 4820, 4780, 4820, 4900],
  },
  {
    id: "op-search",
    spanKind: "internal",
    name: "query search",
    averageLatency: 72,
    opm: 2140,
    errorRate: 0.012,
    impact: 47,
    latencySeries: [58, 64, 71, 69, 74, 72, 70],
    throughputSeries: [1900, 2010, 2100, 2140, 2080, 2140, 2200],
  },
];

const TRACE: Trace = {
  totalMs: 168,
  spans: [
    {
      id: "s1",
      name: "POST /graphql",
      kind: "server",
      startMs: 0,
      durationMs: 168,
      depth: 0,
    },
    {
      id: "s2",
      name: "mutation checkout",
      kind: "graphql",
      startMs: 4,
      durationMs: 158,
      depth: 1,
    },
    {
      id: "s3",
      name: "PricingService.quote",
      kind: "http",
      startMs: 12,
      durationMs: 34,
      depth: 2,
    },
    {
      id: "s4",
      name: "InventoryDb.reserve",
      kind: "internal",
      startMs: 48,
      durationMs: 96,
      depth: 2,
    },
    {
      id: "s5",
      name: "PaymentGateway.charge",
      kind: "http",
      startMs: 146,
      durationMs: 18,
      depth: 2,
    },
  ],
};

/* ────────────────────────────────────────────────────────────────────────
   Bento of telemetry signals (Observe, built from chart primitives)
   ──────────────────────────────────────────────────────────────────────── */

function SignalsBento() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
      {/* Latency p95/p99 */}
      <Card className="sm:col-span-4" glow>
        <CardHeader index="a" title="Latency" hint="p95 / p99 · ms" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <ChartPanel
              title="Response time"
              subtitle="last 60 minutes"
              height={168}
              yDomain={[0, 180]}
              yTicks={[0, 60, 120, 180]}
              yFormat={(n) => `${n}`}
              legend={[
                { label: "p95", color: token.cP95 },
                { label: "p99", color: token.cP99 },
              ]}
            >
              <LineAreaChart
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
                domain={[0, 180]}
                grid
                showHead
              />
            </ChartPanel>
          </NitroCanvas>
        </div>
      </Card>

      {/* Throughput big stat */}
      <Card className="sm:col-span-2">
        <CardHeader index="b" title="Throughput" hint="ops / min" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <NitroCanvas className="h-11">
            <CountUp
              value={94200}
              format={(n) => Math.round(n).toLocaleString("en-US")}
              style={{ justifyContent: "flex-start", fontSize: 34 }}
            />
          </NitroCanvas>
          <NitroCanvas className="mt-3 h-16">
            <BarSeries values={THROUGHPUT_BARS} color={token.cThroughput} />
          </NitroCanvas>
        </div>
      </Card>

      {/* Per-client usage & impact */}
      <Card className="sm:col-span-3">
        <CardHeader index="c" title="Top clients" hint="by impact" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <HBarSeries clients={CLIENTS as Client[]} maxBars={5} />
          </NitroCanvas>
        </div>
      </Card>

      {/* Error rate */}
      <Card className="sm:col-span-3">
        <CardHeader index="d" title="Error rate" hint="% of requests" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <div className="flex items-baseline gap-2">
            <span
              className="font-heading text-h4 tabular-nums"
              style={{ color: token.cError }}
            >
              0.31%
            </span>
            <span className="text-cc-ink-dim text-caption">
              within budget · 1.6% peak
            </span>
          </div>
          <NitroCanvas className="mt-3 h-16">
            <Sparkline values={ERROR_SERIES} stroke={token.cError} fill />
          </NitroCanvas>
        </div>
      </Card>

      {/* Impact-ranked operations */}
      <Card className="sm:col-span-4">
        <CardHeader index="e" title="Impact score" hint="what hurts most" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <InsightsTable
              rows={INSIGHTS as InsightRow[]}
              errorThreshold={0.02}
            />
          </NitroCanvas>
        </div>
      </Card>

      {/* Trace preview */}
      <Card className="sm:col-span-2">
        <CardHeader index="f" title="Slow span" hint="checkout" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <TraceWaterfall trace={TRACE} rowHeight={22} />
          </NitroCanvas>
        </div>
      </Card>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Schema change classification (Evolve aside graphic)
   ──────────────────────────────────────────────────────────────────────── */

type ChangeKind = "safe" | "dangerous" | "breaking";

const KIND_STYLE: Record<
  ChangeKind,
  { readonly label: string; readonly className: string }
> = {
  safe: {
    label: "SAFE",
    className: "text-cc-success border-cc-success/40 bg-cc-success/[0.08]",
  },
  dangerous: {
    label: "DANGEROUS",
    className: "text-cc-warning border-cc-warning/40 bg-cc-warning/[0.08]",
  },
  breaking: {
    label: "BREAKING",
    className: "text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08]",
  },
};

interface KindPillProps {
  readonly kind: ChangeKind;
}

function KindPill({ kind }: KindPillProps) {
  const s = KIND_STYLE[kind];
  return (
    <span
      className={[
        "rounded border px-1.5 py-0.5 font-mono text-[10px] tracking-[0.12em]",
        s.className,
      ].join(" ")}
    >
      {s.label}
    </span>
  );
}

const SCHEMA_CHANGES: readonly {
  readonly field: string;
  readonly kind: ChangeKind;
}[] = [
  { field: "+ Order.deliveryEstimate: DateTime", kind: "safe" },
  { field: "~ Product.price: Float → Money", kind: "dangerous" },
  { field: "- Order.total: Float", kind: "breaking" },
];

function ClassificationCard() {
  return (
    <Card className="mt-1">
      <div className="flex items-center justify-between px-4 py-3">
        <span className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
          orders-api · v14
        </span>
        <span className="text-cc-danger text-caption font-mono">
          publish blocked
        </span>
      </div>
      <div className="border-cc-card-border divide-cc-card-border/60 divide-y border-t">
        {SCHEMA_CHANGES.map((c) => (
          <div
            key={c.field}
            className="flex items-center justify-between gap-3 px-4 py-2.5"
          >
            <code className="text-cc-ink truncate font-mono text-xs">
              {c.field}
            </code>
            <KindPill kind={c.kind} />
          </div>
        ))}
      </div>
      <div className="text-cc-ink-dim border-cc-card-border border-t px-4 py-2.5 font-mono text-[11px]">
        1 safe · 1 dangerous · 1 breaking
      </div>
    </Card>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Delivery / safety band (persisted ops · CI checks · safe rollout)
   ──────────────────────────────────────────────────────────────────────── */

interface CheckRow {
  readonly label: string;
  readonly detail: string;
  readonly state: "pass" | "fail";
}

const CI_CHECKS: readonly CheckRow[] = [
  { label: "schema validate", detail: "127 fields", state: "pass" },
  { label: "client checks", detail: "3 published clients", state: "pass" },
  { label: "breaking change", detail: "Order.total removed", state: "fail" },
  { label: "trusted operations", detail: "482 hashes signed", state: "pass" },
];

function CheckIconMark({ state }: { readonly state: "pass" | "fail" }) {
  if (state === "pass") {
    return (
      <svg
        viewBox="0 0 16 16"
        aria-hidden="true"
        className="text-cc-success h-4 w-4 fill-current"
      >
        <path d="M6.5 11.2 3.3 8l1.1-1.1 2.1 2.1 5-5L12.6 5z" />
      </svg>
    );
  }
  return (
    <svg
      viewBox="0 0 16 16"
      aria-hidden="true"
      className="text-cc-danger h-4 w-4 fill-current"
    >
      <path d="M11.5 5.6 9.1 8l2.4 2.4-1.1 1.1L8 9.1l-2.4 2.4-1.1-1.1L6.9 8 4.5 5.6l1.1-1.1L8 6.9l2.4-2.4z" />
    </svg>
  );
}

function DeliveryBand() {
  return (
    <section
      id="delivery"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <RevealOnScroll>
        <SectionIntro
          index="07"
          eyebrow="Delivery"
          title="Ship on green, roll out with a safety net."
          lead="Persisted, trusted operations lock the queries clients can send. Schema and client checks run in CI, so a breaking change fails the build instead of the customer."
        />
      </RevealOnScroll>

      <div className="mt-12 grid gap-4 lg:grid-cols-3">
        <RevealOnScroll className="lg:col-span-2">
          <Card>
            <div className="flex items-center justify-between px-5 py-3.5">
              <span className="text-cc-heading font-heading text-h6">
                CI schema check
              </span>
              <span className="text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08] rounded border px-2 py-0.5 font-mono text-[10px] tracking-[0.12em]">
                FAILED
              </span>
            </div>
            <div className="border-cc-card-border divide-cc-card-border/60 divide-y border-t">
              {CI_CHECKS.map((c) => (
                <div
                  key={c.label}
                  className="flex items-center gap-3 px-5 py-3"
                >
                  <CheckIconMark state={c.state} />
                  <span className="text-cc-ink font-mono text-sm">
                    {c.label}
                  </span>
                  <span className="text-cc-ink-dim ml-auto font-mono text-xs">
                    {c.detail}
                  </span>
                </div>
              ))}
            </div>
            <div className="text-cc-ink-dim border-cc-card-border border-t px-5 py-3 font-mono text-[11px]">
              merging is blocked until every check passes
            </div>
          </Card>
        </RevealOnScroll>

        <RevealOnScroll>
          <Card className="h-full">
            <div className="flex flex-col gap-5 p-6">
              <div>
                <div className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
                  Persisted operations
                </div>
                <p className="text-cc-ink mt-2 text-sm leading-relaxed">
                  Only registered query hashes execute. Ad-hoc queries and
                  injection never reach a resolver.
                </p>
              </div>
              <div className="border-cc-card-border rounded-lg border bg-black/20 p-3 font-mono text-xs">
                <div className="text-cc-ink-dim">POST /graphql</div>
                <div className="text-cc-accent mt-1 truncate">
                  documentId: sha256:7f3a9b2e…
                </div>
                <div className="text-cc-success mt-1">
                  200 · trusted · 12 ms
                </div>
              </div>
              <div className="mt-auto flex items-center gap-2">
                <span className="text-cc-success text-caption font-mono">
                  ● safe rollout
                </span>
                <span className="text-cc-ink-dim text-caption">
                  stage → canary → prod
                </span>
              </div>
            </div>
          </Card>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Ecosystem / platform strip
   ──────────────────────────────────────────────────────────────────────── */

interface PlatformIconProps {
  readonly className?: string;
}

function ServerGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <rect x="3" y="4" width="18" height="6" rx="1.5" />
      <rect x="3" y="14" width="18" height="6" rx="1.5" />
      <circle cx="7" cy="7" r="1" className="fill-cc-bg" />
      <circle cx="7" cy="17" r="1" className="fill-cc-bg" />
    </svg>
  );
}

function ClientGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <rect x="3" y="4" width="18" height="13" rx="1.5" />
      <rect x="9" y="19" width="6" height="1.6" rx="0.8" />
      <rect
        x="6"
        y="7"
        width="8"
        height="1.4"
        rx="0.7"
        className="fill-cc-bg"
      />
    </svg>
  );
}

function GatewayGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <circle cx="12" cy="12" r="3" />
      <circle cx="4" cy="5" r="2" />
      <circle cx="4" cy="19" r="2" />
      <circle cx="20" cy="12" r="2" />
      <path
        d="M6 6l4 5M6 18l4-5M15 12h3"
        stroke="currentColor"
        strokeWidth="1.4"
        fill="none"
      />
    </svg>
  );
}

function ControlGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <circle
        cx="12"
        cy="12"
        r="9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
      />
      <circle cx="12" cy="12" r="2.5" />
      <path
        d="M12 3v3M12 18v3M3 12h3M18 12h3"
        stroke="currentColor"
        strokeWidth="1.6"
      />
    </svg>
  );
}

const PLATFORM: readonly {
  readonly name: string;
  readonly role: string;
  readonly Icon: (p: PlatformIconProps) => ReactNode;
}[] = [
  { name: "Hot Chocolate", role: "GraphQL server", Icon: ServerGlyph },
  { name: "Strawberry Shake", role: "GraphQL client", Icon: ClientGlyph },
  { name: "Fusion", role: "Federation gateway", Icon: GatewayGlyph },
  { name: "Nitro", role: "Control plane", Icon: ControlGlyph },
];

function EcosystemStrip() {
  return (
    <section
      id="ecosystem"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <RevealOnScroll>
        <SectionIntro
          index="09"
          eyebrow="Platform"
          title="One open-source stack, end to end."
          lead="Nitro sits on top of the same GraphQL platform you already build with, from the server that answers to the gateway that composes."
        />
      </RevealOnScroll>

      <div className="mt-12 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {PLATFORM.map(({ name, role, Icon }, i) => (
          <RevealOnScroll key={name} hiddenClassName="translate-y-6 opacity-0">
            <Card className="h-full">
              <div className="flex flex-col gap-4 p-5">
                <div className="text-cc-accent border-cc-card-border flex h-10 w-10 items-center justify-center rounded-lg border bg-black/20">
                  <Icon className="h-5 w-5 fill-current" />
                </div>
                <div>
                  <div className="text-cc-heading font-heading text-h6">
                    {name}
                  </div>
                  <div className="text-cc-ink-dim text-caption font-mono tracking-[0.14em] uppercase">
                    {role}
                  </div>
                </div>
                <span
                  aria-hidden="true"
                  className="h-px w-full rounded-full opacity-60"
                  style={{
                    background:
                      i === PLATFORM.length - 1
                        ? SPECTRUM
                        : "linear-gradient(90deg, rgba(94,234,212,0.5), transparent)",
                  }}
                />
              </div>
            </Card>
          </RevealOnScroll>
        ))}
      </div>

      <RevealOnScroll className="mt-6">
        <div className="text-cc-ink-dim text-caption text-center font-mono tracking-[0.14em] uppercase">
          MIT licensed · built in the open · one graph for .NET
        </div>
      </RevealOnScroll>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Page
   ──────────────────────────────────────────────────────────────────────── */

export function ClientPage() {
  return (
    <>
      {/* HERO ─────────────────────────────────────────────────── */}
      <section className="pt-6 pb-16 text-center sm:pt-12">
        <RevealOnScroll className="mx-auto flex max-w-3xl flex-col items-center gap-6">
          <div className="flex flex-col items-center gap-4">
            <span
              aria-hidden="true"
              className="h-px w-24 rounded-full"
              style={{ background: SPECTRUM }}
            />
            <Eyebrow>The Control Plane for GraphQL</Eyebrow>
          </div>
          <h1 className="text-cc-heading font-heading text-h1 text-balance">
            Your whole API, on one control plane.
          </h1>
          <p className="lead text-cc-ink mx-auto max-w-2xl !text-xl !leading-relaxed">
            Author operations, watch them run, trace every request, and evolve
            your schema without breaking the clients you ship to.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>

        {/* 5-tab reel: it is its own app window, so no outer frame; the phase
            nav floats over the bottom edge of the stage. */}
        <RevealOnScroll
          className="mt-16 sm:mt-20"
          hiddenClassName="translate-y-10 scale-[0.98] opacity-0"
          shownClassName="translate-y-0 scale-100 opacity-100"
        >
          <div className="relative mx-auto w-full max-w-6xl">
            <div
              aria-hidden="true"
              className="absolute -inset-x-10 -inset-y-8 -z-10 rounded-[2.5rem] opacity-50 blur-3xl"
              style={{
                background:
                  "radial-gradient(50% 50% at 50% 30%, rgba(94,234,212,0.16), transparent 70%)",
              }}
            />
            <NitroReel tabsOverlay />
          </div>
        </RevealOnScroll>
      </section>

      {/* AUTHOR ───────────────────────────────────────────────── */}
      <Showcase
        id="author"
        index="01"
        eyebrow="Author"
        title="Compose GraphQL against real, federated data."
        body="A schema-aware editor with live validation and one-click operations, running against your composed graph, not a mock."
        visual={<NitroCompose className="w-full" />}
      />

      {/* OBSERVE ──────────────────────────────────────────────── */}
      <section
        id="observe"
        className="border-cc-card-border scroll-mt-24 border-t py-20 text-center sm:py-28"
      >
        <RevealOnScroll>
          <SectionIntro
            index="02"
            eyebrow="Observe"
            title="See exactly how your API behaves in production."
            lead="OpenTelemetry-native monitoring: latency, throughput, and error rate per operation and per client, ranked by the impact score that tells you what to fix first."
          />
        </RevealOnScroll>
        <RevealOnScroll
          className="mt-14 sm:mt-16"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <ControlPlaneConsole className="mx-auto max-w-5xl" />
        </RevealOnScroll>
      </section>

      {/* SIGNALS BENTO ────────────────────────────────────────── */}
      <section
        id="signals"
        className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
      >
        <RevealOnScroll>
          <SectionIntro
            index="03"
            eyebrow="Every signal"
            title="One console, every signal in view."
            lead="p95 and p99 latency, throughput, error budget, per-client usage, impact ranking, and the slow span, all reading from the same telemetry."
          />
        </RevealOnScroll>
        <RevealOnScroll
          className="mt-12"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <SignalsBento />
        </RevealOnScroll>
      </section>

      {/* TRACE ────────────────────────────────────────────────── */}
      <Showcase
        id="trace"
        index="04"
        eyebrow="Trace"
        title="Follow one request across your whole backend."
        body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
        visual={<NitroTrace className="w-full" />}
        reverse
      />

      {/* DIAGNOSE ─────────────────────────────────────────────── */}
      <Showcase
        id="diagnose"
        index="05"
        eyebrow="Diagnose"
        title="From an error spike to the line that threw it."
        body="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it. No log spelunking."
        visual={<NitroDiagnose className="w-full" />}
      />

      {/* EVOLVE / SCHEMA ──────────────────────────────────────── */}
      <Showcase
        id="schema"
        index="06"
        eyebrow="Evolve"
        title="Change your schema without breaking clients."
        body="The registry classifies every change as safe, dangerous, or breaking and checks it against published clients before it ships."
        visual={<NitroSchema className="w-full" />}
        aside={<ClassificationCard />}
        reverse
      />

      {/* DELIVERY / SAFETY ────────────────────────────────────── */}
      <DeliveryBand />

      {/* FUSION ───────────────────────────────────────────────── */}
      <Showcase
        id="fusion"
        index="08"
        eyebrow="Compose"
        title="One graph, executed across every subgraph."
        body="Fusion shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
        visual={<NitroFusion className="w-full" />}
      />

      {/* ECOSYSTEM ────────────────────────────────────────────── */}
      <EcosystemStrip />

      {/* CTA ──────────────────────────────────────────────────── */}
      <section className="border-cc-card-border border-t py-24 text-center sm:py-32">
        <RevealOnScroll className="mx-auto flex max-w-2xl flex-col items-center gap-6">
          <Eyebrow>Ready when you are</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 text-balance">
            Put your API on the control plane.
          </h2>
          <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
            Start in the GraphQL IDE in seconds, then grow into observability,
            tracing, and a registry that keeps your schema and clients in sync.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>
      </section>
    </>
  );
}
