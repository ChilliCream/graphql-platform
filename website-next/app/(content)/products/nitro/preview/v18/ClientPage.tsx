"use client";

import { useReducedMotion } from "motion/react";
import { useEffect, useRef } from "react";
import type { CSSProperties, ReactNode } from "react";

import { ControlPlaneConsole } from "@/src/components/nitro/ControlPlaneConsole";
import { RisingParticles } from "@/src/components/nitro/RisingParticles";
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
    <span
      className="text-caption font-medium tracking-[0.22em] uppercase"
      style={{ color: "#f27765" }}
    >
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

/** Glassy, hairline-bordered surface: transparent enough for the page
    atmosphere to glow through, still legible for text and data. */
function Card({ className, children, glow = false }: CardProps) {
  return (
    <div
      className={[
        "relative overflow-hidden rounded-2xl border border-white/10 bg-white/[0.03] backdrop-blur-sm",
        className ?? "",
      ].join(" ")}
    >
      {glow && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-24 right-0 -z-0 h-56 w-56 opacity-40 blur-3xl"
          style={{
            background:
              "radial-gradient(50% 50% at 60% 40%, rgba(242,119,101,0.18), transparent 70%)",
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
            "radial-gradient(55% 60% at 34% 40%, rgba(242,119,101,0.14), transparent 70%), radial-gradient(55% 60% at 68% 45%, rgba(0,188,229,0.14), transparent 70%)",
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
                <div className="mt-1 truncate" style={{ color: "#16b9e4" }}>
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

// Per-card divider tints so the platform strip walks across the spectrum;
// the last card gets the full SPECTRUM sweep.
const PLATFORM_TINTS = [
  "rgba(242,119,101,0.5)",
  "rgba(102,190,119,0.5)",
  "rgba(0,188,229,0.5)",
] as const;

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
                <div
                  className="border-cc-card-border flex h-10 w-10 items-center justify-center rounded-lg border bg-black/20"
                  style={{ color: "#f27765" }}
                >
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
                        : `linear-gradient(90deg, ${PLATFORM_TINTS[i]}, transparent)`,
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
   Hero: a real sunrise cresting a dark planet's horizon.
   The tapering rim reads as a HORIZON line (thin at the ends), with a warm
   white-to-gold SUN blooming right on the crest at top-center as the focal
   point. The surrounding sky carries only a restrained spectrum tint: a faint
   cool cyan on one flank, a faint warm coral on the other, low saturation, so
   it feels natural rather than a garish rainbow. The whole render fills its
   container edge to edge and fades to brand navy at the left/right/bottom so
   the arc dissolves into the page with no hard box edge. Soft motes rise from
   behind the reel's center up toward the sun.
   ──────────────────────────────────────────────────────────────────────── */

// Volumetric god-rays fanning up from the sun. Warm and neutral near the
// crest, the outer rays pick up only the faintest cool/warm atmospheric tint.
const RAYS = [
  { angle: -26, fill: "url(#v18RayWarm)" },
  { angle: -13, fill: "url(#v18RayGold)" },
  { angle: 0, fill: "url(#v18RayGold)" },
  { angle: 13, fill: "url(#v18RayGold)" },
  { angle: 26, fill: "url(#v18RayCool)" },
] as const;

/**
 * The sunrise: a warm white-to-gold sun blooming on the horizon crest, faint
 * volumetric god-rays fanning up from it, and a crisp tapering rim-light that
 * reads as the planet's horizon line. The brightest point sits on the crest at
 * top-center and dissipates into the sky with no hard disc. The surrounding sky
 * carries only a subtle, low-saturation spectrum tint. The whole render fills
 * its container edge to edge and fades to navy at the left/right so the arc
 * dissolves into the page rather than sitting in a box.
 */
function SunriseArc() {
  const reduceMotion = useReducedMotion();
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 z-0 overflow-hidden"
    >
      <svg
        viewBox="0 0 1200 560"
        preserveAspectRatio="xMidYMin slice"
        className="absolute top-0 left-0 h-[46rem] w-full"
      >
        <defs>
          {/* Broad atmospheric halo: a soft warm dawn sky with only a faint,
              low-saturation cool cyan on the right flank and warm coral on the
              left, edgeless. Restrained, not a rainbow. */}
          <radialGradient
            id="v18Halo"
            gradientUnits="userSpaceOnUse"
            cx="600"
            cy="150"
            r="640"
          >
            <stop offset="0" stopColor="#fbe6c8" stopOpacity="0.16" />
            <stop offset="30%" stopColor="#f6d2b0" stopOpacity="0.08" />
            <stop offset="62%" stopColor="#2a3350" stopOpacity="0.05" />
            <stop offset="100%" stopColor="#0b0f1a" stopOpacity="0" />
          </radialGradient>
          {/* Faint horizontal atmospheric tint laid over the sky: coral haze on
              the left, cool cyan haze on the right, kept low so it reads as a
              subtle dawn spectrum, never a garish rainbow. */}
          <linearGradient id="v18SkyTint" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#f27765" stopOpacity="0" />
            <stop offset="18%" stopColor="#f27765" stopOpacity="0.06" />
            <stop offset="42%" stopColor="#f27765" stopOpacity="0" />
            <stop offset="58%" stopColor="#16b9e4" stopOpacity="0" />
            <stop offset="82%" stopColor="#16b9e4" stopOpacity="0.06" />
            <stop offset="100%" stopColor="#16b9e4" stopOpacity="0" />
          </linearGradient>
          {/* The sun: a warm white-to-gold bloom cresting on the horizon line,
              no hard disc. Bright white core, gold falloff, dissolving into the
              dawn sky. This is the focal point. */}
          <radialGradient
            id="v18Sun"
            gradientUnits="userSpaceOnUse"
            cx="600"
            cy="150"
            r="300"
          >
            <stop offset="0" stopColor="#ffffff" stopOpacity="0.98" />
            <stop offset="7%" stopColor="#fff6e6" stopOpacity="0.82" />
            <stop offset="16%" stopColor="#ffe6b0" stopOpacity="0.42" />
            <stop offset="30%" stopColor="#f4b45e" stopOpacity="0.2" />
            <stop offset="52%" stopColor="#e08a4a" stopOpacity="0.07" />
            <stop offset="74%" stopColor="#0b0f1a" stopOpacity="0" />
            <stop offset="100%" stopColor="#0b0f1a" stopOpacity="0" />
          </radialGradient>
          {/* Horizon rim-light: warm white-gold at the crest where the sun sits,
              tapering to a faint coral on the far left and faint cyan on the far
              right. Mostly neutral, only a restrained tint at the flanks. */}
          <linearGradient id="v18Arc" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#f0a07a" stopOpacity="0" />
            <stop offset="14%" stopColor="#f0a07a" stopOpacity="0.34" />
            <stop offset="30%" stopColor="#ffdca6" stopOpacity="0.72" />
            <stop offset="44%" stopColor="#fff3df" stopOpacity="0.95" />
            <stop offset="50%" stopColor="#ffffff" stopOpacity="1" />
            <stop offset="56%" stopColor="#fdf3e6" stopOpacity="0.95" />
            <stop offset="70%" stopColor="#d9ecf3" stopOpacity="0.72" />
            <stop offset="86%" stopColor="#8fc9dd" stopOpacity="0.34" />
            <stop offset="100%" stopColor="#8fc9dd" stopOpacity="0" />
          </linearGradient>
          {/* God-rays fanning up from the sun: warm and gold near the crest, a
              barely-there cool tint on the outer ray. */}
          <linearGradient id="v18RayGold" x1="0.5" y1="1" x2="0.5" y2="0">
            <stop offset="0" stopColor="#ffe1ab" stopOpacity="0.36" />
            <stop offset="55%" stopColor="#ffe1ab" stopOpacity="0.11" />
            <stop offset="1" stopColor="#ffe1ab" stopOpacity="0" />
          </linearGradient>
          <linearGradient id="v18RayWarm" x1="0.5" y1="1" x2="0.5" y2="0">
            <stop offset="0" stopColor="#f6c9a0" stopOpacity="0.3" />
            <stop offset="55%" stopColor="#f6c9a0" stopOpacity="0.09" />
            <stop offset="1" stopColor="#f6c9a0" stopOpacity="0" />
          </linearGradient>
          <linearGradient id="v18RayCool" x1="0.5" y1="1" x2="0.5" y2="0">
            <stop offset="0" stopColor="#bfe3ef" stopOpacity="0.28" />
            <stop offset="55%" stopColor="#bfe3ef" stopOpacity="0.09" />
            <stop offset="1" stopColor="#bfe3ef" stopOpacity="0" />
          </linearGradient>
          {/* Left/right edge masks: fade the whole render to navy at the sides
              so the arc + glow have no hard box edge. */}
          <linearGradient id="v18SideFade" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#0b0f1a" stopOpacity="1" />
            <stop offset="16%" stopColor="#0b0f1a" stopOpacity="0" />
            <stop offset="84%" stopColor="#0b0f1a" stopOpacity="0" />
            <stop offset="100%" stopColor="#0b0f1a" stopOpacity="1" />
          </linearGradient>
          {/* Bottom mask: fade the whole render down into brand navy so the
              sunrise sky/arc/glow dissolves into the page with no hard edge
              where the hero meets the next section. */}
          <linearGradient id="v18BottomFade" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#0b0f1a" stopOpacity="0" />
            <stop offset="55%" stopColor="#0b0f1a" stopOpacity="0" />
            <stop offset="82%" stopColor="#0b0f1a" stopOpacity="0.7" />
            <stop offset="100%" stopColor="#0b0f1a" stopOpacity="1" />
          </linearGradient>
          <filter id="v18ArcBlur" x="-20%" y="-60%" width="140%" height="260%">
            <feGaussianBlur stdDeviation="9" />
          </filter>
          <filter id="v18RayBlur" x="-70%" y="-70%" width="240%" height="240%">
            <feGaussianBlur stdDeviation="20" />
          </filter>
          <filter
            id="v18BloomBlur"
            x="-60%"
            y="-60%"
            width="220%"
            height="220%"
          >
            <feGaussianBlur stdDeviation="11" />
          </filter>
        </defs>

        {/* Broad atmospheric halo, plus a faint horizontal dawn tint */}
        <rect x="0" y="0" width="1200" height="560" fill="url(#v18Halo)" />
        <rect x="0" y="0" width="1200" height="560" fill="url(#v18SkyTint)" />

        {/* The sun: a warm white-to-gold bloom on the crest, softly blurred so
            the core has no hard boundary. The hero's focal point. */}
        <rect
          x="0"
          y="0"
          width="1200"
          height="560"
          fill="url(#v18Sun)"
          filter="url(#v18BloomBlur)"
          className={reduceMotion ? "" : "v18-bloom"}
        />

        {/* Faint volumetric god-rays fanning up from the sun */}
        <g
          filter="url(#v18RayBlur)"
          className={reduceMotion ? "" : "v18-rays"}
          opacity="0.6"
        >
          {RAYS.map((r) => (
            <rect
              key={r.angle}
              x="576"
              y="-220"
              width="48"
              height="372"
              rx="24"
              fill={r.fill}
              transform={`rotate(${r.angle} 600 150)`}
            />
          ))}
        </g>

        {/* Blurred bloom of the arc */}
        <path
          d="M -80 470 Q 600 -170 1280 470"
          fill="none"
          stroke="url(#v18Arc)"
          strokeWidth="16"
          strokeLinecap="round"
          opacity="0.6"
          filter="url(#v18ArcBlur)"
        />
        {/* Crisp bright rim-light arc */}
        <path
          d="M -80 470 Q 600 -170 1280 470"
          fill="none"
          stroke="url(#v18Arc)"
          strokeWidth="2"
          strokeLinecap="round"
        />

        {/* Fade the sides into brand navy so the effect has no box edge. */}
        <rect x="0" y="0" width="1200" height="560" fill="url(#v18SideFade)" />
        {/* Fade the bottom into brand navy so the sky dissolves into the page. */}
        <rect
          x="0"
          y="0"
          width="1200"
          height="560"
          fill="url(#v18BottomFade)"
        />
      </svg>
      {/* Reliable CSS screen-space fades over the SVG. preserveAspectRatio="slice"
          crops the in-SVG side/bottom masks on wide screens, which can leave a
          hard box edge; these dissolve the render into brand navy (the exact page
          background, var(--color-cc-bg)) in screen space so there is no seam at
          any width. Left/right flanks first, then the full bottom. */}
      <div
        aria-hidden="true"
        className="absolute inset-y-0 left-0 w-[22%] bg-gradient-to-r from-[var(--color-cc-bg)] to-transparent"
      />
      <div
        aria-hidden="true"
        className="absolute inset-y-0 right-0 w-[22%] bg-gradient-to-l from-[var(--color-cc-bg)] to-transparent"
      />
      <div
        aria-hidden="true"
        className="absolute inset-x-0 top-[26rem] h-[28rem] bg-gradient-to-b from-transparent via-[var(--color-cc-bg)] to-[var(--color-cc-bg)]"
      />
    </div>
  );
}

// Restrained warm tints for the rising motes: mostly warm white and gold like
// embers lifting toward the sun, with only a faint cool mote for atmosphere.
const MOTE_TINTS = [
  { fill: "255,246,232", glow: "255,224,168" }, // warm white
  { fill: "255,236,196", glow: "244,180,94" }, // gold
  { fill: "255,246,232", glow: "255,224,168" }, // warm white (weighted)
  { fill: "255,224,196", glow: "242,140,90" }, // warm coral
  { fill: "222,240,246", glow: "143,201,221" }, // faint cool
] as const;

/**
 * Soft warm motes rising from behind the reel's center up toward the sun. They
 * originate from a tight band at the bottom-center (where the hue-glow behind
 * the window sits) and fan gently upward and outward as they climb, so the
 * light clearly emits from the center rather than scattering across the hero.
 */
function RisingMotes() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const reduceMotion = useReducedMotion();

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    if (!ctx) {
      return;
    }

    // Deterministic PRNG (mulberry32); never Math.random on a render path.
    let seed = 0x1a2b3c4d;
    const rand = () => {
      seed = (seed + 0x6d2b79f5) | 0;
      let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };

    // A plume of motes emitting from behind the reel's center (bottom-center)
    // and rising toward the sun. `life` runs 0 at the origin to 1 at the top;
    // horizontal position is derived from it so the motes start tightly at the
    // center and fan gently outward as they climb, never scattering.
    const ORIGIN_X = 0.5; // bottom-center, behind the window
    const RISE_TOP = 0.12; // near the sun crest
    const motes = Array.from({ length: 54 }, () => ({
      life: rand(),
      fan: (rand() - 0.5) * 2, // -1..1, how far this mote drifts off-center
      wobPhase: rand() * Math.PI * 2,
      wobSpeed: 0.4 + rand() * 0.9,
      r: 0.5 + rand() * 2.1,
      speed: 0.04 + rand() * 0.07, // life units per second
      base: 0.32 + rand() * 0.52,
      phase: rand() * Math.PI * 2,
      twSpeed: 0.6 + rand() * 1.4,
      tint: MOTE_TINTS[Math.floor(rand() * MOTE_TINTS.length)],
    }));

    // Smooth 0→1 ramp used to fade motes in/out along their rising life.
    const smoothstep = (a: number, b: number, x: number) => {
      const t = Math.min(1, Math.max(0, (x - a) / (b - a)));
      return t * t * (3 - 2 * t);
    };

    // Position a mote from its life: y interpolates origin→crest; x stays near
    // center at the base and fans outward with height, plus a gentle wobble.
    const positionOf = (
      m: (typeof motes)[number],
      tSec: number,
    ): { readonly nx: number; readonly ny: number } => {
      const ny = 1 - m.life * (1 - RISE_TOP);
      const wobble = Math.sin(m.wobPhase + tSec * m.wobSpeed) * 0.01;
      // Fan width grows with life (eased), so the plume starts as a tight point
      // at the hue center and widens into a plume only as it rises.
      const spread = 0.008 + 0.22 * (m.life * m.life);
      const nx = ORIGIN_X + m.fan * spread + wobble;
      return { nx, ny };
    };

    let w = 0;
    let h = 0;
    const resize = () => {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = canvas.clientWidth;
      h = canvas.clientHeight;
      canvas.width = Math.max(1, Math.floor(w * dpr));
      canvas.height = Math.max(1, Math.floor(h * dpr));
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const render = (tSec: number) => {
      ctx.clearRect(0, 0, w, h);
      ctx.shadowBlur = 7;
      for (const m of motes) {
        const { nx, ny } = positionOf(m, tSec);
        const px = nx * w;
        const py = ny * h;
        // Fade in as it lifts off the origin, ease out as it nears the sun.
        const edge =
          smoothstep(0, 0.16, m.life) * (1 - smoothstep(0.78, 1, m.life));
        const twinkle = 0.55 + 0.45 * Math.sin(m.phase + tSec * m.twSpeed);
        const alpha = m.base * twinkle * edge;
        if (alpha <= 0.003) {
          continue;
        }
        ctx.fillStyle = `rgba(${m.tint.fill},1)`;
        ctx.shadowColor = `rgba(${m.tint.glow},0.9)`;
        ctx.globalAlpha = alpha;
        ctx.beginPath();
        ctx.arc(px, py, m.r, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.globalAlpha = 1;
      ctx.shadowBlur = 0;
    };

    resize();

    if (reduceMotion) {
      render(0);
      const ro = new ResizeObserver(() => {
        resize();
        render(0);
      });
      ro.observe(canvas);
      return () => ro.disconnect();
    }

    let raf = 0;
    let start = 0;
    let last = 0;
    const loop = (ts: number) => {
      if (!start) {
        start = ts;
        last = ts;
      }
      const tSec = (ts - start) / 1000;
      const dt = Math.min(0.05, (ts - last) / 1000);
      last = ts;
      for (const m of motes) {
        m.life += m.speed * dt;
        if (m.life > 1) {
          // Respawn at the origin with a fresh fan offset and tint.
          m.life -= 1;
          m.fan = (rand() - 0.5) * 2;
          m.tint = MOTE_TINTS[Math.floor(rand() * MOTE_TINTS.length)];
        }
      }
      render(tSec);
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);

    const ro = new ResizeObserver(() => resize());
    ro.observe(canvas);

    return () => {
      cancelAnimationFrame(raf);
      ro.disconnect();
    };
  }, [reduceMotion]);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 z-0 h-full w-full"
    />
  );
}

function Hero() {
  return (
    <section className="relative left-1/2 isolate w-screen -translate-x-1/2 overflow-hidden">
      {/* Effect layer: the sunrise (horizon rim + sun on the crest) and the
          motes rising from behind the reel's center, spanning the full viewport
          width and fading to brand navy at the sides and bottom, so it blends
          seamlessly into the page atmosphere below. */}
      <SunriseArc />
      <RisingMotes />
      {/* Bottom fade so the copy and reel stay readable and the hero dissolves
          into the page below without a seam. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 bottom-0 z-0 h-64 bg-gradient-to-b from-transparent to-[var(--color-cc-bg)]"
      />

      <div className="relative z-10 mx-auto max-w-7xl px-5 pt-52 pb-16 text-center sm:px-12 sm:pt-56">
        <RevealOnScroll className="mx-auto flex max-w-3xl flex-col items-center gap-6">
          <h1 className="font-heading text-h1 text-balance">
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage:
                  "linear-gradient(180deg, #f5f0ea 34%, #f6c9a0 62%, #7cd6ea 100%)",
              }}
            >
              Your whole API, on one control plane.
            </span>
          </h1>

          <p className="lead text-cc-ink mx-auto max-w-2xl !text-xl !leading-relaxed">
            Nitro is the control plane for GraphQL and .NET. Author operations,
            watch them run, trace every request, and evolve your schema without
            breaking the clients you ship to.
          </p>

          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton
              href="/get-started"
              className="!text-cc-heading border !border-[rgba(242,119,101,0.5)] !bg-[rgba(242,119,101,0.16)] shadow-[inset_0_1px_0_rgba(255,255,255,0.22)] backdrop-blur-md transition-all duration-200 hover:!bg-[rgba(242,119,101,0.28)] hover:shadow-[inset_0_1px_0_rgba(255,255,255,0.28),0_0_30px_rgba(242,119,101,0.42)]"
            >
              Start for Free
            </SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>

        {/* Reel crests up right below the CTAs, rising out of a soft teal
            underglow that bridges the arc light to the product. It is its own
            app window, so no outer frame; keep it crisp. */}
        <RevealOnScroll
          className="mt-2 sm:mt-4"
          hiddenClassName="translate-y-10 scale-[0.98] opacity-0"
          shownClassName="translate-y-0 scale-100 opacity-100"
        >
          <div className="relative mx-auto w-full max-w-6xl">
            {/* A strong warm hue backlighting the window from UNDERNEATH: the
                reel appears lit from behind and below, and this is where the
                rising motes emit from. The glow is centred on the window and
                biased toward its lower half / bottom edge, so the light reads as
                coming from behind-and-below the console (glowing UP from under
                it), not from the sides. Warm gold core with a restrained coral
                and cyan tint, all fading to transparent so there is no hard
                edge. This is the mote origin. */}
            <div
              aria-hidden="true"
              className="absolute top-[150px] left-1/2 z-0 h-[520px] w-[1700px] max-w-[170vw] -translate-x-1/2 -translate-y-1/2 blur-[70px]"
              style={{
                background:
                  "radial-gradient(44% 52% at 50% 66%, rgba(255,206,140,0.85), transparent 72%), radial-gradient(60% 46% at 50% 72%, rgba(255,216,152,0.5), transparent 76%), radial-gradient(34% 40% at 44% 74%, rgba(242,119,101,0.34), transparent 76%), radial-gradient(34% 40% at 56% 74%, rgba(0,188,229,0.26), transparent 76%)",
              }}
            />
            {/* Softer tall halo bridging the gap up toward the CTAs, warm and
                centered so it reads as the same central glow rising. */}
            <div
              aria-hidden="true"
              className="absolute -inset-x-16 -top-40 -bottom-6 z-0 rounded-[3rem] opacity-70 blur-3xl"
              style={{
                background:
                  "radial-gradient(50% 60% at 50% 14%, rgba(255,200,140,0.14), transparent 64%)",
              }}
            />
            <div className="relative z-10">
              <NitroReel tabsOverlay />
            </div>
          </div>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Full-page atmosphere: drifting teal particles + soft teal glows that scroll
   with the document, so the brand light recurs from hero to footer.
   ──────────────────────────────────────────────────────────────────────── */

function PageAtmosphere() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 left-1/2 -z-10 w-screen -translate-x-1/2 overflow-hidden"
    >
      <RisingParticles
        color="255,244,235"
        count={64}
        className="absolute inset-0"
      />
      {/* Large, very soft spectrum glows placed down the page so the brand
          light recurs and ties the whole surface together: a warm coral bloom
          upper-left, a green bloom mid, and a cool cyan bloom lower. */}
      <div
        className="absolute top-[16%] left-0 h-[46rem] w-[46rem] -translate-x-1/3 rounded-full opacity-70 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, rgba(242,119,101,0.11), transparent 68%)",
        }}
      />
      <div
        className="absolute top-[44%] left-1/2 h-[44rem] w-[44rem] -translate-x-1/2 rounded-full opacity-55 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, rgba(102,190,119,0.09), transparent 68%)",
        }}
      />
      <div
        className="absolute top-[52%] right-0 h-[42rem] w-[42rem] translate-x-1/3 rounded-full opacity-60 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, rgba(124,146,198,0.09), transparent 68%)",
        }}
      />
      <div
        className="absolute bottom-[6%] left-1/2 h-[40rem] w-[40rem] -translate-x-1/2 rounded-full opacity-60 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, rgba(0,188,229,0.09), transparent 68%)",
        }}
      />
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Page
   ──────────────────────────────────────────────────────────────────────── */

export function ClientPage() {
  return (
    // The shared content layout wraps children in a div with `py-8` (32px top
    // padding). Cancel it for this page only with `-mt-8` so the full-bleed hero
    // sits flush right under the site header, with no gap under the nav.
    <div className="relative isolate -mt-8">
      <style>{`
        html, body { overflow-x: hidden; }
        @keyframes v18Bloom {
          0%, 100% { opacity: 0.9; }
          50% { opacity: 1; }
        }
        .v18-bloom { animation: v18Bloom 8s ease-in-out infinite; }
        @keyframes v18Rays {
          0%, 100% { opacity: 0.5; }
          50% { opacity: 0.82; }
        }
        .v18-rays { animation: v18Rays 7s ease-in-out infinite; }
        @media (prefers-reduced-motion: reduce) {
          .v18-bloom, .v18-rays { animation: none; }
        }
      `}</style>

      {/* FULL-PAGE ATMOSPHERE ─────────────────────────────────── */}
      <PageAtmosphere />

      {/* HERO ─────────────────────────────────────────────────── */}
      <Hero />

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
    </div>
  );
}
