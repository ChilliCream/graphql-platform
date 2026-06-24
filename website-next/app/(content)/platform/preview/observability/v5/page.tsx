import type { Metadata } from "next";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Telemetry Reference Sheet: GraphQL observability for .NET",
  description:
    "GraphQL observability for .NET as a developer reference sheet: OpenTelemetry-native operation, service, and client lenses with p95/p99, error rate, and traces.",
  keywords: [
    "GraphQL observability for .NET",
    "OpenTelemetry .NET",
    "Nitro telemetry",
    "distributed tracing",
    "p95 p99 latency",
    "operation monitoring",
    "Hot Chocolate observability",
    "trace span ledger",
  ],
  openGraph: {
    title: "Telemetry Reference Sheet",
    description:
      "GraphQL observability for .NET laid out as a dense catalog: span ledger, lenses, signals, topology. OpenTelemetry end to end.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ *
 * Single brand accent: cc-accent (teal). Status colors appear strictly
 * as data dots and a single coral row tint, matching site rationing.
 * The brand-spectrum gradient is used at most once, as a hairline above
 * the footer CTA.
 * ------------------------------------------------------------------ */

const ACCENT = "#5eead4"; // cc-accent
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const CYAN = "#16b9e4";
const VIOLET = "#7c92c6";

const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

const TRACE_ID = "4b1c8f2a9e07";

export default function ObservabilityPreviewV5Page() {
  return (
    <main className="relative pb-12">
      <div className="lg:grid lg:grid-cols-[7rem_minmax(0,1fr)] lg:gap-x-8">
        <SectionIndex />
        <div className="flex flex-col gap-14">
          <HeaderRecord />
          <Section01Production />
          <Section02SpanLedger />
          <Section03Lenses />
          <Section04Signals />
          <Section05Topology />
          <Section06Notes />
          <Section07Footer />
        </div>
      </div>
    </main>
  );
}

/* ================================================================== *
 * LEFT RAIL: §01..§07 mono section index
 * Hidden below lg. Sticky so it tracks the page like a man-page TOC.
 * ================================================================== */

const SECTION_INDEX: ReadonlyArray<{ id: string; label: string }> = [
  { id: "s01", label: "§01 PRODUCTION" },
  { id: "s02", label: "§02 SPANS" },
  { id: "s03", label: "§03 LENSES" },
  { id: "s04", label: "§04 SIGNALS" },
  { id: "s05", label: "§05 TOPOLOGY" },
  { id: "s06", label: "§06 NOTES" },
  { id: "s07", label: "§07 INDEX" },
];

function SectionIndex() {
  return (
    <aside className="hidden lg:block">
      <div className="text-cc-nav-label sticky top-24 flex flex-col gap-3 pt-1 font-mono text-[11px] tracking-[0.16em] uppercase">
        <span className="text-cc-ink-faint">index</span>
        {SECTION_INDEX.map((s) => (
          <a
            key={s.id}
            href={`#${s.id}`}
            className="hover:text-cc-accent transition-colors"
          >
            {s.label}
          </a>
        ))}
      </div>
    </aside>
  );
}

/* ================================================================== *
 * HEADER RECORD
 * Two columns: eyebrow + h1 + mono deck + dl of facts. Below, a full
 * content-width tabular SLO/metric strip with p95 / p99 / errors / rpm.
 * ================================================================== */

function HeaderRecord() {
  return (
    <header className="border-cc-card-border border-t pt-8">
      <div className="grid gap-10 md:grid-cols-[1.15fr_1fr]">
        <div>
          <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.28em] uppercase">
            platform/observability
          </span>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-5">
            See what the API is doing.
          </h1>
          <p className="text-cc-prose mt-5 max-w-xl font-mono text-[13px] leading-[1.65]">
            GraphQL observability for .NET, laid out as a reference sheet. The
            moment latency climbs, you already know which operation hurts, who
            it reaches, and exactly which hop is slow.
          </p>
        </div>
        <FactList
          rows={[
            ["runtime", ".NET"],
            ["transport", "OpenTelemetry"],
            ["surfaces", "ops / services / clients"],
            ["trace-id", TRACE_ID],
            ["incident", "checkout · investigating"],
          ]}
        />
      </div>

      <SloStrip />
    </header>
  );
}

interface FactListProps {
  readonly rows: ReadonlyArray<readonly [label: string, value: string]>;
}

function FactList({ rows }: FactListProps) {
  return (
    <dl className="border-cc-card-border divide-cc-card-border divide-y border-t border-b">
      {rows.map(([k, v]) => (
        <div
          key={k}
          className="grid grid-cols-[8rem_minmax(0,1fr)] items-baseline gap-3 py-2"
        >
          <dt className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
            {k}
          </dt>
          <dd className="text-cc-heading truncate font-mono text-[13px]">
            {v}
          </dd>
        </div>
      ))}
    </dl>
  );
}

function SloStrip() {
  const cells = [
    {
      label: "p95 latency",
      value: "42",
      unit: "ms",
      dot: GREEN,
      note: "within budget",
    },
    {
      label: "p99 latency",
      value: "318",
      unit: "ms",
      dot: CORAL,
      note: "spiking",
    },
    {
      label: "error rate",
      value: "0.3",
      unit: "%",
      dot: AMBER,
      note: "5xx on billing",
    },
    {
      label: "throughput",
      value: "9.4",
      unit: "k rpm",
      dot: ACCENT,
      note: "steady",
    },
  ];
  return (
    <div className="border-cc-card-border mt-10 grid grid-cols-2 border-t border-b sm:grid-cols-4">
      {cells.map((c, i) => (
        <div
          key={c.label}
          className={`px-4 py-4 ${
            i > 0 ? "sm:border-cc-card-border sm:border-l" : ""
          } ${
            i % 2 === 1 ? "border-cc-card-border border-l" : ""
          } ${i >= 2 ? "border-cc-card-border border-t sm:border-t-0" : ""}`}
        >
          <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
            {c.label}
          </div>
          <div className="mt-2 flex items-baseline gap-1.5">
            <span className="font-heading text-h2 text-cc-heading leading-none tabular-nums">
              {c.value}
            </span>
            <span className="text-cc-ink-dim font-mono text-[12px]">
              {c.unit}
            </span>
          </div>
          <div className="mt-3 flex items-center gap-2">
            <Dot color={c.dot} />
            <span className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
              {c.note}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}

/* ================================================================== *
 * §01 PRODUCTION
 * Four-column key/value row describing the active checkout incident,
 * plus a thin inline SVG hairline sparkline aligned with the p99 cell.
 * ================================================================== */

function Section01Production() {
  return (
    <Section id="s01" marker="§01" title="Production">
      <CatalogRow
        cells={[
          { label: "operation", value: "checkout", tone: "heading" },
          { label: "status", value: "investigating", dot: AMBER },
          { label: "p99", value: "318ms", tone: "coral" },
          { label: "impact", value: "#1", tone: "heading" },
        ]}
      />
      <div className="border-cc-card-border grid grid-cols-[8rem_minmax(0,1fr)] items-center border-t">
        <div className="text-cc-nav-label px-4 py-3 font-mono text-[10px] tracking-[0.18em] uppercase">
          p99 trend · 30m
        </div>
        <div className="border-cc-card-border border-l px-4 py-2">
          <Sparkline />
        </div>
      </div>
      <FootnoteRow text="Impact score ranks by what hurts the system, not raw call count." />
    </Section>
  );
}

function Sparkline() {
  const points = [
    18, 16, 19, 17, 20, 18, 16, 19, 21, 18, 17, 20, 19, 22, 28, 41, 58, 72, 80,
    78,
  ];
  const w = 640;
  const h = 28;
  const max = 96;
  const step = w / (points.length - 1);
  const coords = points.map((p, i) => {
    const x = i * step;
    const y = h - (p / max) * h;
    return [x, y] as const;
  });
  const line = coords.map(([x, y]) => `${x},${y}`).join(" ");
  const last = coords[coords.length - 1];
  return (
    <svg
      viewBox={`0 0 ${w} ${h}`}
      className="h-[28px] w-full"
      preserveAspectRatio="none"
      aria-hidden
    >
      <line
        x1="0"
        y1={h - (32 / max) * h}
        x2={w}
        y2={h - (32 / max) * h}
        stroke="var(--color-cc-ink-faint)"
        strokeWidth="1"
        strokeDasharray="2 4"
      />
      <polyline
        points={line}
        fill="none"
        stroke={CORAL}
        strokeWidth="1.25"
        strokeLinejoin="round"
        strokeLinecap="round"
      />
      <circle cx={last[0]} cy={last[1]} r="2" fill={CORAL} />
    </svg>
  );
}

/* ================================================================== *
 * §02 SPAN LEDGER
 * Numbered trace table: idx | kind | route | start | dur | %share | st
 * The slow billing.Charge() row is tinted coral with an accent hairline.
 * ================================================================== */

interface SpanRecord {
  readonly idx: string;
  readonly kind: "graphql" | "rest" | "grpc" | "job" | "db";
  readonly route: string;
  readonly depth: number;
  readonly start: string;
  readonly dur: string;
  readonly share: string;
  readonly status: "ok" | "warn" | "fire";
  readonly slow?: boolean;
}

const KIND_LABEL: Record<SpanRecord["kind"], string> = {
  graphql: "gql",
  rest: "rest",
  grpc: "grpc",
  job: "job",
  db: "db",
};

const KIND_COLOR: Record<SpanRecord["kind"], string> = {
  graphql: ACCENT,
  rest: VIOLET,
  grpc: CORAL,
  job: "#8b9bd4",
  db: "#7dd3fc",
};

const SPAN_LEDGER: readonly SpanRecord[] = [
  {
    idx: "#01",
    kind: "graphql",
    route: "mutation checkout",
    depth: 0,
    start: "0ms",
    dur: "318ms",
    share: "100%",
    status: "fire",
  },
  {
    idx: "#02",
    kind: "rest",
    route: "api → users-svc · GET /me",
    depth: 1,
    start: "13ms",
    dur: "21ms",
    share: "7%",
    status: "ok",
  },
  {
    idx: "#03",
    kind: "grpc",
    route: "users-svc → billing · Charge()",
    depth: 1,
    start: "51ms",
    dur: "201ms",
    share: "63%",
    status: "fire",
    slow: true,
  },
  {
    idx: "#04",
    kind: "db",
    route: "billing → db · SELECT account",
    depth: 2,
    start: "64ms",
    dur: "9ms",
    share: "3%",
    status: "ok",
  },
  {
    idx: "#05",
    kind: "job",
    route: "billing → worker · enqueue receipt",
    depth: 2,
    start: "261ms",
    dur: "37ms",
    share: "12%",
    status: "ok",
  },
];

const STATUS_COLOR: Record<SpanRecord["status"], string> = {
  ok: GREEN,
  warn: AMBER,
  fire: CORAL,
};

function Section02SpanLedger() {
  return (
    <Section id="s02" marker="§02" title="Span ledger">
      <div className="border-cc-card-border border-t border-b">
        <div className="text-cc-nav-label grid grid-cols-[3rem_3rem_minmax(0,1fr)_4rem_4.5rem_4rem_2.25rem] items-center gap-3 px-4 py-2 font-mono text-[10px] tracking-[0.18em] uppercase">
          <span>idx</span>
          <span>kind</span>
          <span>route</span>
          <span className="text-right">start</span>
          <span className="text-right">dur</span>
          <span className="text-right">%</span>
          <span className="text-right">st</span>
        </div>
        <div className="border-cc-card-border border-t">
          {SPAN_LEDGER.map((s) => (
            <SpanLedgerRow key={s.idx} span={s} />
          ))}
        </div>
        <div className="border-cc-card-border text-cc-nav-label flex items-center justify-between border-t px-4 py-2 font-mono text-[10px] tracking-[0.18em] uppercase">
          <span>
            trace <span style={{ color: ACCENT }}>{TRACE_ID}</span>
          </span>
          <span>
            total <span className="text-cc-heading">318ms</span>
          </span>
        </div>
      </div>
      <FootnoteRow text="One request, every hop a real OpenTelemetry span. The load-bearing row is the slow gRPC call to billing." />
    </Section>
  );
}

interface SpanLedgerRowProps {
  readonly span: SpanRecord;
}

function SpanLedgerRow({ span }: SpanLedgerRowProps) {
  const kindColor = KIND_COLOR[span.kind];
  const dotColor = STATUS_COLOR[span.status];
  const rowStyle = span.slow
    ? {
        backgroundColor: `${CORAL}0d`,
        boxShadow: `inset 2px 0 0 ${ACCENT}`,
      }
    : undefined;
  return (
    <div
      className="border-cc-card-border/60 grid grid-cols-[3rem_3rem_minmax(0,1fr)_4rem_4.5rem_4rem_2.25rem] items-center gap-3 border-t px-4 py-2 font-mono text-[12px]"
      style={rowStyle}
    >
      <span className="text-cc-nav-label tabular-nums">{span.idx}</span>
      <span
        className="inline-flex items-center justify-center rounded-[3px] px-1.5 py-0.5 text-[10px] font-semibold tracking-wide uppercase"
        style={{ color: kindColor, backgroundColor: `${kindColor}1a` }}
      >
        {KIND_LABEL[span.kind]}
      </span>
      <span
        className={`truncate ${
          span.slow ? "text-cc-heading" : "text-cc-ink-dim"
        }`}
        style={{ paddingLeft: span.depth * 14 }}
      >
        <span className="text-cc-ink-faint">{span.depth > 0 ? "└ " : ""}</span>
        {span.route}
      </span>
      <span className="text-cc-ink-dim text-right tabular-nums">
        {span.start}
      </span>
      <span
        className="text-right tabular-nums"
        style={{ color: span.slow ? CORAL : "var(--color-cc-heading)" }}
      >
        {span.dur}
      </span>
      <span className="text-cc-ink-dim text-right tabular-nums">
        {span.share}
      </span>
      <span className="flex justify-end">
        <Dot color={dotColor} pulse={span.status === "fire"} />
      </span>
    </div>
  );
}

/* ================================================================== *
 * §03 LENSES CATALOG
 * Three stacked tight tables: operations / services / clients.
 * No card wrappers, only mono headers and hairline rows.
 * ================================================================== */

function Section03Lenses() {
  return (
    <Section id="s03" marker="§03" title="Lenses">
      <OperationsTable />
      <ServicesTable />
      <ClientsTable />
      <FootnoteRow text="Same OpenTelemetry stream, sliced three ways. Rank operations by impact, then drop into the degraded service, then check published clients affected before you ship the fix." />
    </Section>
  );
}

interface OpLensRow {
  readonly num: string;
  readonly name: string;
  readonly p95: string;
  readonly status: SpanRecord["status"];
}

const OP_LENS: readonly OpLensRow[] = [
  { num: "#1", name: "checkout", p95: "42ms", status: "fire" },
  { num: "#2", name: "cartSummary", p95: "31ms", status: "warn" },
  { num: "#3", name: "productList", p95: "12ms", status: "ok" },
  { num: "#4", name: "userProfile", p95: "8ms", status: "ok" },
];

function OperationsTable() {
  return (
    <div className="border-cc-card-border border-t">
      <div className="text-cc-nav-label flex items-center justify-between px-4 py-2 font-mono text-[10px] tracking-[0.22em] uppercase">
        <span>operations · ranked by impact</span>
        <span className="text-cc-ink-faint">nitro</span>
      </div>
      <div className="text-cc-nav-label border-cc-card-border grid grid-cols-[3rem_minmax(0,1fr)_4.5rem_2.25rem] items-center gap-3 border-t px-4 py-2 font-mono text-[10px] tracking-[0.18em] uppercase">
        <span>#</span>
        <span>name</span>
        <span className="text-right">p95</span>
        <span className="text-right">st</span>
      </div>
      {OP_LENS.map((row) => (
        <div
          key={row.name}
          className="border-cc-card-border/60 grid grid-cols-[3rem_minmax(0,1fr)_4.5rem_2.25rem] items-center gap-3 border-t px-4 py-2 font-mono text-[12px]"
        >
          <span className="text-cc-nav-label tabular-nums">{row.num}</span>
          <span
            className={
              row.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
            }
          >
            {row.name}
          </span>
          <span
            className="text-right tabular-nums"
            style={{
              color: row.status === "fire" ? CORAL : "var(--color-cc-ink-dim)",
            }}
          >
            {row.p95}
          </span>
          <span className="flex justify-end">
            <Dot
              color={STATUS_COLOR[row.status]}
              pulse={row.status === "fire"}
            />
          </span>
        </div>
      ))}
    </div>
  );
}

interface ServiceRow {
  readonly name: string;
  readonly p95: string;
  readonly p99: string;
  readonly errors: string;
  readonly rpm: string;
  readonly codes: readonly [ok: number, warn: number, fire: number];
  readonly status: SpanRecord["status"];
}

const SERVICES: readonly ServiceRow[] = [
  {
    name: "billing",
    p95: "42ms",
    p99: "318ms",
    errors: "0.3%",
    rpm: "9.4k",
    codes: [96.4, 3.3, 0.3],
    status: "fire",
  },
  {
    name: "users-svc",
    p95: "18ms",
    p99: "44ms",
    errors: "0.0%",
    rpm: "12.1k",
    codes: [99.9, 0.1, 0],
    status: "ok",
  },
  {
    name: "worker",
    p95: "37ms",
    p99: "92ms",
    errors: "0.1%",
    rpm: "2.6k",
    codes: [99.5, 0.4, 0.1],
    status: "warn",
  },
];

function ServicesTable() {
  return (
    <div className="border-cc-card-border border-t">
      <div className="text-cc-nav-label flex items-center justify-between px-4 py-2 font-mono text-[10px] tracking-[0.22em] uppercase">
        <span>services</span>
        <span className="text-cc-ink-faint">nitro</span>
      </div>
      <div className="text-cc-nav-label border-cc-card-border grid grid-cols-[minmax(0,1fr)_4rem_4.5rem_3.5rem_3.5rem_6rem_2.25rem] items-center gap-3 border-t px-4 py-2 font-mono text-[10px] tracking-[0.18em] uppercase">
        <span>name</span>
        <span className="text-right">p95</span>
        <span className="text-right">p99</span>
        <span className="text-right">err</span>
        <span className="text-right">rpm</span>
        <span>codes</span>
        <span className="text-right">st</span>
      </div>
      {SERVICES.map((s) => (
        <div
          key={s.name}
          className="border-cc-card-border/60 grid grid-cols-[minmax(0,1fr)_4rem_4.5rem_3.5rem_3.5rem_6rem_2.25rem] items-center gap-3 border-t px-4 py-2 font-mono text-[12px]"
        >
          <span
            className={
              s.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
            }
          >
            {s.name}
          </span>
          <span className="text-cc-ink-dim text-right tabular-nums">
            {s.p95}
          </span>
          <span
            className="text-right tabular-nums"
            style={{
              color: s.status === "fire" ? CORAL : "var(--color-cc-heading)",
            }}
          >
            {s.p99}
          </span>
          <span className="text-cc-ink-dim text-right tabular-nums">
            {s.errors}
          </span>
          <span className="text-cc-ink-dim text-right tabular-nums">
            {s.rpm}
          </span>
          <CodeBar codes={s.codes} />
          <span className="flex justify-end">
            <Dot color={STATUS_COLOR[s.status]} pulse={s.status === "fire"} />
          </span>
        </div>
      ))}
    </div>
  );
}

interface CodeBarProps {
  readonly codes: readonly [ok: number, warn: number, fire: number];
}

function CodeBar({ codes }: CodeBarProps) {
  const [ok, warn, fire] = codes;
  return (
    <span className="flex h-1.5 w-full overflow-hidden rounded-[1px]">
      {ok > 0 && <span style={{ width: `${ok}%`, backgroundColor: GREEN }} />}
      {warn > 0 && (
        <span style={{ width: `${warn}%`, backgroundColor: AMBER }} />
      )}
      {fire > 0 && (
        <span style={{ width: `${fire}%`, backgroundColor: CORAL }} />
      )}
    </span>
  );
}

interface ClientRow {
  readonly name: string;
  readonly version: string;
  readonly share: string;
  readonly status: SpanRecord["status"];
}

const CLIENTS: readonly ClientRow[] = [
  { name: "web-storefront", version: "4.2.0", share: "61%", status: "fire" },
  { name: "ios-app", version: "3.8.1", share: "27%", status: "warn" },
  { name: "partner-api", version: "1.0", share: "12%", status: "ok" },
];

function ClientsTable() {
  return (
    <div className="border-cc-card-border border-t">
      <div className="text-cc-nav-label flex items-center justify-between px-4 py-2 font-mono text-[10px] tracking-[0.22em] uppercase">
        <span>clients · published, affected</span>
        <span className="text-cc-ink-faint">nitro</span>
      </div>
      <div className="text-cc-nav-label border-cc-card-border grid grid-cols-[minmax(0,1fr)_5rem_4rem_2.25rem] items-center gap-3 border-t px-4 py-2 font-mono text-[10px] tracking-[0.18em] uppercase">
        <span>name</span>
        <span className="text-right">version</span>
        <span className="text-right">share</span>
        <span className="text-right">st</span>
      </div>
      {CLIENTS.map((c) => (
        <div
          key={c.name}
          className="border-cc-card-border/60 grid grid-cols-[minmax(0,1fr)_5rem_4rem_2.25rem] items-center gap-3 border-t px-4 py-2 font-mono text-[12px]"
        >
          <span
            className={
              c.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
            }
          >
            {c.name}
          </span>
          <span className="text-cc-ink-dim text-right tabular-nums">
            {c.version}
          </span>
          <span className="text-cc-ink-dim text-right tabular-nums">
            {c.share}
          </span>
          <span className="flex justify-end">
            <Dot color={STATUS_COLOR[c.status]} pulse={c.status === "fire"} />
          </span>
        </div>
      ))}
    </div>
  );
}

/* ================================================================== *
 * §04 SIGNALS
 * Flush grid of four big-number cells separated by hairlines.
 * label · tabular-nums value · unit · one-line note · status dot.
 * ================================================================== */

function Section04Signals() {
  const signals = [
    {
      label: "p95 latency",
      value: "42",
      unit: "ms",
      note: "within budget",
      dot: GREEN,
    },
    {
      label: "p99 latency",
      value: "318",
      unit: "ms",
      note: "spiking",
      dot: CORAL,
    },
    {
      label: "error rate",
      value: "0.3",
      unit: "%",
      note: "5xx on billing",
      dot: AMBER,
    },
    {
      label: "throughput",
      value: "9.4",
      unit: "k rpm",
      note: "steady",
      dot: ACCENT,
    },
  ];
  return (
    <Section id="s04" marker="§04" title="Signals">
      <div className="border-cc-card-border grid grid-cols-2 border-t border-b lg:grid-cols-4">
        {signals.map((s, i) => (
          <div
            key={s.label}
            className={`px-4 py-5 ${
              i > 0 ? "lg:border-cc-card-border lg:border-l" : ""
            } ${
              i % 2 === 1 ? "border-cc-card-border border-l" : ""
            } ${i >= 2 ? "border-cc-card-border border-t lg:border-t-0" : ""}`}
          >
            <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
              {s.label}
            </div>
            <div className="mt-2 flex items-baseline gap-1.5">
              <span className="font-heading text-h3 text-cc-heading leading-none tabular-nums">
                {s.value}
              </span>
              <span className="text-cc-ink-dim font-mono text-[12px]">
                {s.unit}
              </span>
            </div>
            <div className="mt-3 flex items-center gap-2">
              <Dot color={s.dot} />
              <span className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
                {s.note}
              </span>
            </div>
          </div>
        ))}
      </div>
      <FootnoteRow text="The four signals that matter, held side by side. Status is rationed as a colored dot only." />
    </Section>
  );
}

/* ================================================================== *
 * §05 TOPOLOGY
 * Compact inline SVG node diagram captioned by a parallel dl of edges.
 * ================================================================== */

function Section05Topology() {
  return (
    <Section id="s05" marker="§05" title="Topology">
      <div className="border-cc-card-border grid border-t border-b md:grid-cols-[1fr_1fr]">
        <div className="px-4 py-5">
          <TopologyDiagram />
        </div>
        <div className="border-cc-card-border border-t md:border-t-0 md:border-l">
          <div className="text-cc-nav-label flex items-center justify-between px-4 py-2 font-mono text-[10px] tracking-[0.22em] uppercase">
            <span>edges</span>
            <span className="text-cc-ink-faint">nitro</span>
          </div>
          <dl className="border-cc-card-border divide-cc-card-border/60 divide-y border-t">
            <EdgeRow from="api" to="users-svc" kind="rest" note="healthy" />
            <EdgeRow
              from="api"
              to="billing"
              kind="grpc"
              note="hot · 201ms"
              hot
            />
            <EdgeRow from="billing" to="accounts" kind="db" note="9ms" />
            <EdgeRow
              from="billing"
              to="worker"
              kind="job"
              note="enqueue receipt"
            />
            <EdgeRow from="worker" to="ledger" kind="db" note="async write" />
          </dl>
        </div>
      </div>
      <FootnoteRow text="A distributed trace does not stop at the GraphQL boundary. Nitro monitors REST APIs, gRPC services, and background jobs through ChilliCream.Nitro.OpenTelemetry." />
    </Section>
  );
}

function TopologyDiagram() {
  return (
    <svg
      viewBox="0 0 360 240"
      className="h-auto w-full"
      role="img"
      aria-label="Service topology: api fans out to users-svc and billing, billing fans to accounts and worker, worker writes to ledger. The api to billing gRPC edge is hot."
    >
      <defs>
        <linearGradient id="hotEdgeV5" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={ACCENT} stopOpacity="0.5" />
          <stop offset="100%" stopColor={CORAL} stopOpacity="0.95" />
        </linearGradient>
      </defs>

      <TopologyEdge x1={180} y1={36} x2={92} y2={112} />
      <TopologyEdge x1={180} y1={36} x2={268} y2={112} hot />
      <TopologyEdge x1={268} y1={112} x2={68} y2={196} />
      <TopologyEdge x1={268} y1={112} x2={180} y2={196} />
      <TopologyEdge x1={180} y1={196} x2={292} y2={196} />

      <TopologyNode x={180} y={36} label="api" sub="gql" kind="graphql" />
      <TopologyNode x={92} y={112} label="users-svc" sub="rest" kind="rest" />
      <TopologyNode
        x={268}
        y={112}
        label="billing"
        sub="grpc"
        kind="grpc"
        hot
      />
      <TopologyNode x={68} y={196} label="accounts" sub="db" kind="db" />
      <TopologyNode x={180} y={196} label="worker" sub="job" kind="job" />
      <TopologyNode x={292} y={196} label="ledger" sub="db" kind="db" />
    </svg>
  );
}

interface TopologyEdgeProps {
  readonly x1: number;
  readonly y1: number;
  readonly x2: number;
  readonly y2: number;
  readonly hot?: boolean;
}

function TopologyEdge({ x1, y1, x2, y2, hot }: TopologyEdgeProps) {
  return (
    <line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={hot ? "url(#hotEdgeV5)" : "var(--color-cc-card-border)"}
      strokeWidth={hot ? 1.75 : 1}
    />
  );
}

interface TopologyNodeProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly sub: string;
  readonly kind: SpanRecord["kind"];
  readonly hot?: boolean;
}

function TopologyNode({ x, y, label, sub, kind, hot }: TopologyNodeProps) {
  const color = hot ? CORAL : KIND_COLOR[kind];
  return (
    <g>
      <rect
        x={x - 44}
        y={y - 16}
        width={88}
        height={32}
        rx={3}
        fill="var(--color-cc-surface)"
        stroke={color}
        strokeWidth={hot ? 1.25 : 1}
        strokeOpacity={hot ? 1 : 0.55}
      />
      <text
        x={x}
        y={y - 2}
        textAnchor="middle"
        fill="var(--color-cc-heading)"
        fontSize="10"
        fontFamily="monospace"
      >
        {label}
      </text>
      <text
        x={x}
        y={y + 9}
        textAnchor="middle"
        fill="var(--color-cc-nav-label)"
        fontSize="7.5"
        fontFamily="monospace"
        letterSpacing="0.16em"
      >
        {sub.toUpperCase()}
      </text>
    </g>
  );
}

interface EdgeRowProps {
  readonly from: string;
  readonly to: string;
  readonly kind: SpanRecord["kind"];
  readonly note: string;
  readonly hot?: boolean;
}

function EdgeRow({ from, to, kind, note, hot }: EdgeRowProps) {
  const color = KIND_COLOR[kind];
  return (
    <div className="grid grid-cols-[minmax(0,1fr)_4.5rem_minmax(0,1fr)] items-baseline gap-3 px-4 py-2 font-mono text-[12px]">
      <dt className="text-cc-ink-dim truncate text-right">{from}</dt>
      <dd
        className="text-center text-[10px] font-semibold tracking-wide uppercase"
        style={{ color }}
      >
        → {KIND_LABEL[kind]}
      </dd>
      <dd className={`truncate ${hot ? "text-cc-heading" : "text-cc-ink-dim"}`}>
        {to}{" "}
        <span
          className="text-cc-nav-label"
          style={hot ? { color: CORAL } : undefined}
        >
          · {note}
        </span>
      </dd>
    </div>
  );
}

/* ================================================================== *
 * §06 NOTES & HONESTY
 * Numbered note list 01..03 as a dl with terms in cc-accent.
 * ================================================================== */

function Section06Notes() {
  const notes: ReadonlyArray<{
    readonly num: string;
    readonly term: string;
    readonly def: string;
  }> = [
    {
      num: "01",
      term: "telemetry is configured, not magic",
      def: "The dashboards above come from telemetry you point at Nitro. It is a deliberate, documented configuration step, not something that turns on by itself.",
    },
    {
      num: "02",
      term: "the IDE is a separate thing",
      def: "The GraphQL IDE can be served from your Hot Chocolate endpoint. That is independent of the telemetry surfaced on this page. Two facts, kept apart.",
    },
    {
      num: "03",
      term: "OpenTelemetry end to end",
      def: "Vendor-neutral spans across GraphQL, REST, gRPC, and background jobs. Your data is yours; there is no proprietary agent locking the trace in.",
    },
  ];
  return (
    <Section id="s06" marker="§06" title="Notes">
      <dl className="border-cc-card-border divide-cc-card-border divide-y border-t border-b">
        {notes.map((n) => (
          <div
            key={n.num}
            className="grid grid-cols-[3.5rem_minmax(0,1fr)] gap-4 px-4 py-3"
          >
            <dt className="font-mono text-[12px]">
              <span className="text-cc-nav-label tabular-nums">{n.num}</span>
              <span
                className="mt-1 block text-[10px] tracking-[0.18em] uppercase"
                style={{ color: ACCENT }}
              >
                note
              </span>
            </dt>
            <dd>
              <div className="text-cc-heading font-mono text-[13px]">
                {n.term}
              </div>
              <div className="text-cc-ink-dim mt-1 font-mono text-[12px] leading-[1.65]">
                {n.def}
              </div>
            </dd>
          </div>
        ))}
      </dl>
    </Section>
  );
}

/* ================================================================== *
 * §07 INDEX FOOTER / CTA
 * A single thin row laid out like a man-page footer with one spectrum
 * hairline above it (the single brand-spectrum event on the page).
 * ================================================================== */

function Section07Footer() {
  return (
    <Section id="s07" marker="§07" title="Footer">
      <div className="relative">
        <div
          aria-hidden
          className="absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="grid items-center gap-4 px-1 pt-6 pb-2 md:grid-cols-[auto_1fr_auto]">
          <div className="flex items-center gap-3 font-mono text-[12px]">
            <SolidButton href="/get-started">Start for Free</SolidButton>
          </div>
          <span className="text-cc-ink-faint hidden text-center font-mono text-[10px] tracking-[0.22em] uppercase md:inline">
            · cc/observability · graphql observability for .net ·
          </span>
          <div className="flex items-center justify-start gap-3 font-mono text-[12px] md:justify-end">
            <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
              Read the Docs
            </OutlineButton>
          </div>
        </div>
        <div className="text-cc-nav-label border-cc-card-border mt-4 flex flex-wrap items-center justify-between gap-2 border-t pt-2 font-mono text-[10px] tracking-[0.22em] uppercase">
          <span>chillicream · nitro</span>
          <span className="text-cc-ink-dim hidden sm:inline">
            docs/nitro/open-telemetry/operation-monitoring
          </span>
          <span>trace {TRACE_ID}</span>
        </div>
      </div>
    </Section>
  );
}

/* ================================================================== *
 * Shared section frame: §0x marker + heading + body slot.
 * ================================================================== */

interface SectionProps {
  readonly id: string;
  readonly marker: string;
  readonly title: string;
  readonly children: React.ReactNode;
}

function Section({ id, marker, title, children }: SectionProps) {
  return (
    <section id={id} className="scroll-mt-28">
      <div className="border-cc-card-border flex items-baseline justify-between border-t pt-5 pb-3">
        <div className="flex items-baseline gap-4">
          <span
            className="font-mono text-[11px] tracking-[0.22em] uppercase"
            style={{ color: ACCENT }}
          >
            {marker}
          </span>
          <h2 className="font-heading text-h4 text-cc-heading">{title}</h2>
        </div>
        <span className="text-cc-ink-faint font-mono text-[10px] tracking-[0.22em] uppercase">
          ref
        </span>
      </div>
      <div className="flex flex-col">{children}</div>
    </section>
  );
}

/* ================================================================== *
 * §01 catalog row: 4-up key/value cells with optional dot/tone.
 * ================================================================== */

interface CatalogCell {
  readonly label: string;
  readonly value: string;
  readonly dot?: string;
  readonly tone?: "heading" | "coral";
}

interface CatalogRowProps {
  readonly cells: ReadonlyArray<CatalogCell>;
}

function CatalogRow({ cells }: CatalogRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-2 border-t md:grid-cols-4">
      {cells.map((c, i) => (
        <div
          key={c.label}
          className={`px-4 py-3 ${
            i > 0 ? "md:border-cc-card-border md:border-l" : ""
          } ${
            i % 2 === 1 ? "border-cc-card-border border-l" : ""
          } ${i >= 2 ? "border-cc-card-border border-t md:border-t-0" : ""}`}
        >
          <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
            {c.label}
          </div>
          <div className="mt-1.5 flex items-center gap-2">
            {c.dot && <Dot color={c.dot} />}
            <span
              className="font-mono text-[13px] tabular-nums"
              style={{
                color:
                  c.tone === "coral"
                    ? CORAL
                    : c.tone === "heading"
                      ? "var(--color-cc-heading)"
                      : "var(--color-cc-ink-dim)",
              }}
            >
              {c.value}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}

/* ================================================================== *
 * Tiny shared atoms
 * ================================================================== */

interface FootnoteRowProps {
  readonly text: string;
}

function FootnoteRow({ text }: FootnoteRowProps) {
  return (
    <p className="text-cc-nav-label mt-3 px-4 font-mono text-[11px] leading-[1.6]">
      <span className="text-cc-ink-faint">{"// "}</span>
      {text}
    </p>
  );
}

interface DotProps {
  readonly color: string;
  readonly pulse?: boolean;
}

function Dot({ color, pulse }: DotProps) {
  return (
    <span className="relative inline-flex h-1.5 w-1.5 shrink-0">
      {pulse && (
        <span
          className="absolute inline-flex h-full w-full rounded-full opacity-60 motion-safe:animate-ping"
          style={{ backgroundColor: color }}
        />
      )}
      <span
        className="relative inline-flex h-1.5 w-1.5 rounded-full"
        style={{ backgroundColor: color }}
      />
    </span>
  );
}
