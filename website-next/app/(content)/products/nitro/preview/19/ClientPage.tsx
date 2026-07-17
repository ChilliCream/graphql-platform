"use client";

import { useReducedMotion } from "motion/react";
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

// Hero beam palette: electric indigo / blue-violet with a white-hot core.
const BEAM_INDIGO = "#818cf8"; // indigo-400 accent word + eyebrow line
const BEAM_INDIGO_SOFT = "#a5b4fc"; // indigo-300, gentler tint

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
   Hero: a single elegant vertical light shaft rising from the center-top of
   the reel. A white-hot core wrapped in electric indigo glow that widens
   gently upward and fades to black, flaring into a wide horizontal bloom at
   the reel's top edge, with fine sparks drifting slowly upward.
   ──────────────────────────────────────────────────────────────────────── */

// Deterministic PRNG so the spark field is identical every render (no
// per-render Math.random) and stable under prefers-reduced-motion.
function mulberry32(seed: number): () => number {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

interface Mote {
  readonly left: number;
  readonly bottom: number;
  readonly size: number;
  readonly dur: number;
  readonly delay: number;
  readonly dy: number;
  readonly peak: number;
  readonly hot: boolean;
}

// Sparks drifting up inside the shaft plus a wider low band fanning out of the
// base flare. Deterministic (seeded PRNG) so positions never shift per render.
const MOTES: readonly Mote[] = (() => {
  const rnd = mulberry32(0x5eead4);
  const column = Array.from({ length: 32 }, (): Mote => {
    return {
      left: 50 + (rnd() - 0.5) * 24, // tight around the axis
      bottom: 3 + rnd() * 94,
      size: 0.5 + rnd() * 1.7,
      dur: 7 + rnd() * 9,
      delay: -rnd() * 16,
      dy: -(150 + rnd() * 210),
      peak: 0.4 + rnd() * 0.55,
      hot: rnd() > 0.66,
    };
  });
  const flare = Array.from({ length: 14 }, (): Mote => {
    return {
      left: 50 + (rnd() - 0.5) * 72, // fan out across the base bloom
      bottom: rnd() * 20,
      size: 0.5 + rnd() * 1.5,
      dur: 6 + rnd() * 7,
      delay: -rnd() * 13,
      dy: -(90 + rnd() * 150),
      peak: 0.35 + rnd() * 0.5,
      hot: rnd() > 0.55,
    };
  });
  return [...column, ...flare];
})();

interface Speck {
  readonly left: number;
  readonly bottom: number;
  readonly size: number;
  readonly min: number;
  readonly max: number;
  readonly dur: number;
  readonly delay: number;
  readonly hot: boolean;
}

// Faint far-field star specks that give the upper void depth. Deterministic.
const FAR_SPECKS: readonly Speck[] = (() => {
  const rnd = mulberry32(0x7c4dff);
  return Array.from({ length: 18 }, (): Speck => {
    const min = 0.05 + rnd() * 0.12;
    return {
      left: 22 + rnd() * 76, // biased to centre-right
      bottom: 12 + rnd() * 84,
      size: 1 + rnd() * 1.5,
      min,
      max: min + 0.16 + rnd() * 0.22,
      dur: 3.5 + rnd() * 4.5,
      delay: -rnd() * 6,
      hot: rnd() > 0.82,
    };
  });
})();

function HeroBeam() {
  const reduced = useReducedMotion();
  const flarePulse = reduced
    ? undefined
    : "beamFlarePulse 6.5s ease-in-out infinite";
  const corePulse = reduced
    ? undefined
    : "beamCorePulse 5s ease-in-out infinite";

  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 z-0"
    >
      <style>{`
        @keyframes beamFlarePulse {
          0%, 100% { opacity: 0.9; }
          50% { opacity: 1; }
        }
        @keyframes beamCorePulse {
          0%, 100% { opacity: 0.85; }
          50% { opacity: 1; }
        }
        @keyframes beamMoteRise {
          0% { transform: translateY(0) scale(1); opacity: 0; }
          14% { opacity: var(--mote-peak); }
          50% { opacity: calc(var(--mote-peak) * 0.5); }
          85% { opacity: var(--mote-peak); }
          100% { transform: translateY(var(--mote-dy)) scale(0.5); opacity: 0; }
        }
        @keyframes beamSpeckTwinkle {
          0%, 100% { opacity: var(--speck-min); }
          50% { opacity: var(--speck-max); }
        }
      `}</style>

      {/* Ambient cool halo pooled just above the reel. */}
      <div
        className="absolute left-1/2 h-[620px] w-[920px] -translate-x-1/2 blur-[110px]"
        style={{
          bottom: "calc(100% - 140px)",
          background:
            "radial-gradient(50% 60% at 50% 100%, rgba(91,108,255,0.16), rgba(79,70,229,0.05) 46%, transparent 72%)",
        }}
      />

      {/* Outer cone glow: electric indigo widening gently upward, gone by ~60%. */}
      <div
        className="absolute left-1/2 -translate-x-1/2 blur-[52px]"
        style={{
          bottom: "100%",
          height: "720px",
          width: "440px",
          clipPath: "polygon(46% 100%, 54% 100%, 80% 0, 20% 0)",
          background:
            "linear-gradient(to top, rgba(124,90,255,0.5) 0%, rgba(91,108,255,0.4) 22%, rgba(79,70,229,0.18) 44%, transparent 60%)",
          mixBlendMode: "screen",
          animation: flarePulse,
        }}
      />

      {/* Inner glow beam: narrow, intense indigo / blue-violet. */}
      <div
        className="absolute left-1/2 -translate-x-1/2 blur-[15px]"
        style={{
          bottom: "100%",
          height: "660px",
          width: "46px",
          background:
            "linear-gradient(to top, rgba(224,228,255,0.95) 0%, rgba(124,110,255,0.85) 18%, rgba(91,108,255,0.55) 46%, rgba(79,70,229,0.2) 66%, transparent 84%)",
          mixBlendMode: "screen",
          animation: corePulse,
        }}
      />

      {/* White-hot core line: the center clips to white, fading up. */}
      <div
        className="absolute left-1/2 -translate-x-1/2 blur-[1.5px]"
        style={{
          bottom: "100%",
          height: "640px",
          width: "3px",
          background:
            "linear-gradient(to top, #ffffff 0%, rgba(255,255,255,0.95) 15%, rgba(210,216,255,0.68) 40%, rgba(150,160,255,0.24) 62%, transparent 82%)",
          boxShadow: "0 0 22px 5px rgba(120,110,255,0.55)",
          mixBlendMode: "screen",
          animation: corePulse,
        }}
      />

      {/* Base flare: wide horizontal bloom pouring out of the reel's top edge. */}
      <div
        className="absolute left-1/2 -translate-x-1/2 blur-[44px]"
        style={{
          bottom: "calc(100% - 105px)",
          height: "210px",
          width: "min(96%, 900px)",
          background:
            "radial-gradient(50% 58% at 50% 50%, rgba(150,165,255,0.6), rgba(91,108,255,0.4) 30%, rgba(79,70,229,0.14) 55%, transparent 76%)",
          mixBlendMode: "screen",
          animation: flarePulse,
        }}
      />

      {/* Hot white-blue core of the flare pooling right at the seam. */}
      <div
        className="absolute left-1/2 -translate-x-1/2 blur-[26px]"
        style={{
          bottom: "calc(100% - 60px)",
          height: "120px",
          width: "min(52%, 560px)",
          background:
            "radial-gradient(50% 60% at 50% 50%, rgba(255,255,255,0.95), rgba(200,208,255,0.5) 34%, transparent 70%)",
          mixBlendMode: "screen",
          animation: corePulse,
        }}
      />

      {/* Crisp bright light line exactly on the reel's top border. */}
      <div
        className="absolute left-1/2 -translate-x-1/2 blur-[8px]"
        style={{
          bottom: "calc(100% - 3px)",
          height: "6px",
          width: "min(84%, 940px)",
          background:
            "linear-gradient(90deg, transparent, rgba(200,210,255,0.65) 22%, #ffffff 50%, rgba(200,210,255,0.65) 78%, transparent)",
          mixBlendMode: "screen",
          animation: corePulse,
        }}
      />

      {/* Sparks drifting up through the shaft and out of the base flare. */}
      <div
        className="absolute left-1/2 -translate-x-1/2"
        style={{ bottom: "100%", height: "620px", width: "760px" }}
      >
        {MOTES.map((m, i) => (
          <span
            key={i}
            className="absolute rounded-full"
            style={
              {
                left: `${m.left}%`,
                bottom: `${m.bottom}%`,
                width: `${m.size}px`,
                height: `${m.size}px`,
                background: m.hot ? "#ffffff" : "rgba(180,190,255,0.95)",
                boxShadow: m.hot
                  ? "0 0 6px 1px rgba(206,214,255,0.85)"
                  : "0 0 4px 1px rgba(99,102,241,0.5)",
                mixBlendMode: "screen",
                opacity: reduced ? m.peak : 0,
                animation: reduced
                  ? undefined
                  : `beamMoteRise ${m.dur}s ease-in-out ${m.delay}s infinite`,
                "--mote-peak": String(m.peak),
                "--mote-dy": `${m.dy}px`,
              } as CSSProperties
            }
          />
        ))}
      </div>

      {/* Far-field star specks giving the upper void depth. */}
      <div
        className="absolute inset-x-0"
        style={{ bottom: "100%", height: "780px" }}
      >
        {FAR_SPECKS.map((s, i) => (
          <span
            key={i}
            className="absolute rounded-full"
            style={
              {
                left: `${s.left}%`,
                bottom: `${s.bottom}%`,
                width: `${s.size}px`,
                height: `${s.size}px`,
                background: s.hot ? "#ffffff" : "rgba(165,180,255,0.9)",
                boxShadow: s.hot
                  ? "0 0 5px 1px rgba(206,214,255,0.7)"
                  : "0 0 3px 1px rgba(99,102,241,0.45)",
                mixBlendMode: "screen",
                opacity: reduced ? (s.min + s.max) / 2 : s.min,
                animation: reduced
                  ? undefined
                  : `beamSpeckTwinkle ${s.dur}s ease-in-out ${s.delay}s infinite`,
                "--speck-min": String(s.min),
                "--speck-max": String(s.max),
              } as CSSProperties
            }
          />
        ))}
      </div>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Page
   ──────────────────────────────────────────────────────────────────────── */

export function ClientPage() {
  return (
    <>
      {/* HERO ─────────────────────────────────────────────────── */}
      <section className="relative isolate overflow-hidden pt-6 pb-16 sm:pt-12">
        <RevealOnScroll className="relative z-20 flex max-w-2xl flex-col items-start gap-6 text-left">
          <div className="flex items-center gap-3">
            <span
              aria-hidden="true"
              className="h-px w-12 rounded-full"
              style={{
                background: `linear-gradient(90deg, ${BEAM_INDIGO}, rgba(129,140,248,0))`,
              }}
            />
            <span
              className="text-caption font-medium tracking-[0.22em] uppercase"
              style={{ color: BEAM_INDIGO_SOFT }}
            >
              The Control Plane for GraphQL
            </span>
          </div>
          <h1 className="font-heading text-h1 text-balance">
            <span className="text-cc-heading">Your whole API, on one </span>
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage: `linear-gradient(180deg, ${BEAM_INDIGO_SOFT}, #6366f1)`,
              }}
            >
              control plane.
            </span>
          </h1>
          <p className="lead text-cc-ink max-w-xl !text-xl !leading-relaxed">
            Author operations, watch them run, trace every request, and evolve
            your schema without breaking the clients you ship to. Nitro is the
            control plane for GraphQL and .NET.
          </p>
          <div className="mt-2 flex flex-wrap items-center gap-4">
            <SolidButton
              href="https://nitro.chillicream.com"
              className="bg-[image:linear-gradient(180deg,#6d6cf6,#4f46e5)] !text-white shadow-[0_14px_20px_-12px_rgba(79,70,229,0.5)] ring-1 ring-white/15 ring-inset hover:opacity-90"
            >
              See it in action
            </SolidButton>
          </div>
        </RevealOnScroll>

        {/* 5-tab reel sitting bottom-center. Its own centered phase caption is
            hidden (the hero already leads), so nothing overlaps the beam; the
            beam is anchored to this wrapper so its flare pours out of the
            window's top edge while the product stays crisp above it. */}
        <RevealOnScroll
          className="relative z-10 mt-16 sm:mt-20"
          hiddenClassName="translate-y-10 scale-[0.98] opacity-0"
          shownClassName="translate-y-0 scale-100 opacity-100"
        >
          <div className="relative mx-auto w-full max-w-6xl">
            {/* Hide the reel's own centered phase caption (TabReel's first
                child) so nothing overlaps the beam. Anchored by direct-child
                combinators so only the top-level reel, not the nested product
                screens, is affected. */}
            <style>{`
              .v19-reel-host > .nt-root > [role="group"] > div:first-child {
                display: none;
              }
            `}</style>
            <HeroBeam />
            <div className="v19-reel-host relative z-10">
              <NitroReel tabsOverlay />
            </div>
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
