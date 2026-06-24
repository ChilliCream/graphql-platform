"use client";

import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Brand spectrum, used exactly once on the page (cycle continues hairline).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// The seven planner phases that label each calendar column.
const PHASES = [
  "COMPOSE",
  "VALIDATE",
  "PLAN",
  "ROUTE",
  "EXECUTE",
  "OBSERVE",
  "SHIP",
] as const;

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface MonoLabelProps {
  readonly children: ReactNode;
  readonly tone?: "accent" | "dim" | "ink";
}

function MonoLabel({ children, tone = "dim" }: MonoLabelProps) {
  const color =
    tone === "accent"
      ? "text-cc-accent"
      : tone === "ink"
        ? "text-cc-ink"
        : "text-cc-nav-label";
  return (
    <span
      className={`${color} font-mono text-[11px] font-medium tracking-[0.22em] uppercase`}
    >
      {children}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Ledger background pattern. Single inline SVG with a 7-column grid of
// vertical hairlines plus horizontal week separators, masked top/bottom.
// -----------------------------------------------------------------------------

function LedgerPattern() {
  // 7 columns over a 700x500 viewBox so vertical lines land at 100px steps.
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0"
      style={{
        maskImage:
          "linear-gradient(to bottom, transparent 0%, black 14%, black 86%, transparent 100%)",
        WebkitMaskImage:
          "linear-gradient(to bottom, transparent 0%, black 14%, black 86%, transparent 100%)",
      }}
    >
      <svg
        viewBox="0 0 700 500"
        preserveAspectRatio="none"
        className="h-full w-full"
      >
        <g stroke="rgba(245,241,234,0.12)" strokeOpacity="0.35" strokeWidth="1">
          {[0, 100, 200, 300, 400, 500, 600, 700].map((x) => (
            <line key={`v-${x}`} x1={x} x2={x} y1="0" y2="500" />
          ))}
          {[0, 100, 200, 300, 400, 500].map((y) => (
            <line key={`h-${y}`} x1="0" x2="700" y1={y} y2={y} />
          ))}
        </g>
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Calendar cell, animated in once on first view.
// -----------------------------------------------------------------------------

interface CellProps {
  readonly day: string;
  readonly title: string;
  readonly body: string;
  readonly index: number;
  readonly span?: 1 | 2;
  readonly today?: boolean;
}

function CalendarCell({
  day,
  title,
  body,
  index,
  span = 1,
  today = false,
}: CellProps) {
  const reduce = useReducedMotion();

  const colSpan =
    span === 2
      ? "col-span-2 sm:col-span-2 lg:col-span-2"
      : "col-span-1 sm:col-span-1 lg:col-span-1";

  return (
    <motion.div
      initial={reduce ? false : { opacity: 0, y: 8 }}
      whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "0px 0px -10% 0px" }}
      transition={{
        duration: 0.45,
        delay: Math.min(index * 0.035, 0.9),
        ease: "easeOut",
      }}
      className={[
        colSpan,
        "relative flex min-h-[140px] flex-col justify-between rounded-md border p-4",
        today
          ? "border-cc-accent/60 bg-cc-accent/[0.06]"
          : "border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover",
        "transition-colors",
      ].join(" ")}
    >
      {today && (
        <motion.span
          aria-hidden
          className="pointer-events-none absolute inset-0 rounded-md"
          initial={false}
          animate={
            reduce
              ? undefined
              : {
                  boxShadow: [
                    "0 0 0 0 rgba(94,234,212,0.0)",
                    "0 0 0 6px rgba(94,234,212,0.12)",
                    "0 0 0 0 rgba(94,234,212,0.0)",
                  ],
                  opacity: [0.7, 1, 0.7],
                }
          }
          transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
        />
      )}
      <div className="relative flex items-start justify-between gap-2">
        <span
          className={`font-mono text-[11px] tabular-nums ${
            today ? "text-cc-accent" : "text-cc-nav-label"
          }`}
        >
          {day}
        </span>
        {today && (
          <span className="border-cc-accent/50 bg-cc-accent/10 text-cc-accent inline-flex items-center rounded-full border px-1.5 py-0.5 font-mono text-[9px] tracking-widest uppercase">
            today
          </span>
        )}
      </div>
      <div className="relative">
        <h3
          className={`font-heading text-[15px] leading-snug font-semibold tracking-tight ${
            today ? "text-cc-heading" : "text-cc-heading"
          }`}
        >
          {title}
        </h3>
        <p className="text-cc-ink-dim mt-1.5 text-[12.5px] leading-snug">
          {body}
        </p>
      </div>
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// The 7x5 calendar definition. Indexes drive the staggered entrance.
// -----------------------------------------------------------------------------

interface CellSpec {
  readonly day: string;
  readonly title: string;
  readonly body: string;
  readonly span?: 1 | 2;
  readonly today?: boolean;
}

// 35 cells, some spanning 2 columns. We track running columns to keep day
// numbers in order and the calendar visually full.
const CALENDAR: readonly CellSpec[] = [
  // Week 1
  {
    day: "01",
    title: "Compose in CI",
    body: "Read subgraph SDLs, merge, fail the build on conflict.",
  },
  {
    day: "02",
    title: "Stable diagnostics",
    body: "Composition errors carry codes you can match in scripts.",
  },
  {
    day: "03",
    title: "Satisfiability proof",
    body: "Every reachable field is provably resolvable across subgraphs.",
  },
  {
    day: "04",
    title: "Query plan",
    body: "Each operation compiles into a planned distributed fetch.",
  },
  {
    day: "05",
    title: "Parallel fan-out",
    body: "Independent fetches run in parallel, dependent ones sequence.",
  },
  {
    day: "06",
    title: "OpenTelemetry",
    body: "Spans for ExecuteRequest, PlanOperation, ExecutePlanNode.",
  },
  {
    day: "07",
    title: "Versioned .far",
    body: "Inspectable archive, diffable between releases.",
  },
  // Week 2 (cells 8..14). Day 09 is "today", a 2-col cell, replacing 09 and 10.
  {
    day: "08",
    title: "Federation v2 interop",
    body: "Apollo @key, @requires, @provides flow through unchanged.",
  },
  {
    day: "09",
    title: "Planning-time composition.",
    body: "If it composes, it answers. Subgraphs are merged, validated, and proven before a client sends a query.",
    span: 2,
    today: true,
  },
  // skip day 10 - covered by today span
  {
    day: "11",
    title: "Composite Schemas spec",
    body: "Vendor-neutral, under the GraphQL Foundation.",
  },
  {
    day: "12",
    title: "Hot Chocolate native",
    body: "Existing Hot Chocolate servers are valid subgraphs.",
  },
  {
    day: "13",
    title: "Entity batching",
    body: "Shared keys collected, fetched in one HTTP/2 call.",
  },
  {
    day: "14",
    title: "No N+1 across graph",
    body: "The plan removes round-trips the runtime would otherwise add.",
  },
  // Week 3 (cells 15..21). Day 18 spans 2 cols.
  {
    day: "15",
    title: "ASP.NET Core",
    body: "AddGraphQLGateway() in your DI, your middleware, your auth.",
  },
  {
    day: "16",
    title: "No standalone binary",
    body: "No Rust process, no Node runtime, no YAML config.",
  },
  {
    day: "17",
    title: "JWT, cookie, OIDC, mTLS",
    body: "Standard ASP.NET Core auth, headers forwarded to subgraphs.",
  },
  {
    day: "18",
    title: "Self-run gateway, always.",
    body: "Fusion runs inside your network boundary. No hosted hop in the request path, ever.",
    span: 2,
  },
  // skip day 19 - covered by span
  {
    day: "20",
    title: "Cache control",
    body: "Conservative merge of subgraph cache policies.",
  },
  {
    day: "21",
    title: "Persisted operations",
    body: "Pin client queries, lock down the surface for production.",
  },
  // Week 4 (cells 22..28)
  {
    day: "22",
    title: "Schema registry",
    body: "Nitro can deliver composed archives, never in the request path.",
  },
  {
    day: "23",
    title: "Error contracts",
    body: "Errors keep stable extension codes across subgraphs.",
  },
  {
    day: "24",
    title: "Header propagation",
    body: "Forward request headers to subgraphs per policy.",
  },
  {
    day: "25",
    title: "Subgraph health",
    body: "OpenTelemetry per fetch, traced into Nitro.",
  },
  {
    day: "26",
    title: "Plan caching",
    body: "Compiled plans cached by operation, recomputed on schema change.",
  },
  {
    day: "27",
    title: "MIT licensed",
    body: "ChilliCream GraphQL Platform, developed in the open on GitHub.",
  },
  {
    day: "28",
    title: "Inspectable artifact",
    body: "Open the .far, see exactly what the gateway will serve.",
  },
  // Week 5 (cells 29..35)
  {
    day: "29",
    title: "Schema diff",
    body: "Diff composed archives between releases, before deploy.",
  },
  {
    day: "30",
    title: "Published clients tracked",
    body: "See which published clients are affected on a breaking change.",
  },
  {
    day: "31",
    title: "Migration path",
    body: "Documented directive-by-directive map from Federation v2.",
  },
  {
    day: "32",
    title: "GraphQL IDE",
    body: "Served from the endpoint, scoped by gateway configuration.",
  },
  {
    day: "33",
    title: "Telemetry via Nitro",
    body: "OpenTelemetry pipeline configured through Nitro for the gateway.",
  },
  {
    day: "34",
    title: "Connectors",
    body: "Fusion.Connectors.ApolloFederation reads existing subgraphs.",
  },
  {
    day: "35",
    title: "Roadmap on GitHub",
    body: "Issues, releases, and proposals live in the open repo.",
  },
];

// -----------------------------------------------------------------------------
// Code sample, syntax-highlighted with inline spans.
// -----------------------------------------------------------------------------

function ComposeSample() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-md border">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
        <MonoLabel>ci/compose.sh</MonoLabel>
        <span className="border-cc-card-border text-cc-nav-label rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-widest uppercase">
          shell
        </span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[12.5px] leading-6">
        <span style={{ color: "#8b949e" }}>
          {"# Compose subgraphs, fail the build on any conflict.\n"}
        </span>
        <span style={{ color: "#ff7b72" }}>nitro</span>
        <span style={{ color: "#c9d1d9" }}> fusion compose \</span>
        {"\n"}
        <span style={{ color: "#c9d1d9" }}>
          {"  --subgraph catalog=./catalog.graphql \\"}
        </span>
        {"\n"}
        <span style={{ color: "#c9d1d9" }}>
          {"  --subgraph checkout=./checkout.graphql \\"}
        </span>
        {"\n"}
        <span style={{ color: "#c9d1d9" }}>
          {"  --subgraph reviews=./reviews.graphql \\"}
        </span>
        {"\n"}
        <span style={{ color: "#c9d1d9" }}>{"  --output ./gateway.far"}</span>
        {"\n\n"}
        <span style={{ color: "#5eead4" }}>OK</span>
        <span style={{ color: "#c9d1d9" }}>
          {" composed 3 subgraphs, 0 errors, "}
        </span>
        <span style={{ color: "#a5d6ff" }}>gateway.far</span>
        <span style={{ color: "#c9d1d9" }}> written</span>
      </pre>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Subgraph monogram tile.
// -----------------------------------------------------------------------------

interface MonogramProps {
  readonly letter: string;
  readonly label: string;
}

function Monogram({ letter, label }: MonogramProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col items-center gap-2 rounded-md border p-4 transition-colors">
      <div className="border-cc-card-border text-cc-heading font-heading flex h-12 w-12 items-center justify-center rounded-md border bg-black/20 text-xl font-semibold">
        {letter}
      </div>
      <span className="text-cc-nav-label font-mono text-[10.5px] tracking-widest uppercase">
        {label}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Phase callout cell on the week-detail row.
// -----------------------------------------------------------------------------

interface PhaseCalloutProps {
  readonly phase: string;
  readonly title: string;
  readonly body: string;
}

function PhaseCallout({ phase, title, body }: PhaseCalloutProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-md border p-4">
      <MonoLabel>{phase}</MonoLabel>
      <h4 className="text-cc-heading font-heading mt-2 text-[15px] font-semibold tracking-tight">
        {title}
      </h4>
      <p className="text-cc-ink-dim mt-1 text-[12.5px] leading-snug">{body}</p>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <>
      {/* HERO: month-header card with eyebrow, headline, lead, CTAs, then a
          7-col strip of phase headers below it. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-14">
        <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-xl border p-6 sm:p-10">
          <div className="flex flex-col gap-2">
            <MonoLabel tone="accent">JUN 2026 / FUSION</MonoLabel>
            <h1 className="text-cc-heading font-heading mt-3 max-w-4xl text-4xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
              A distributed GraphQL gateway, planned like a calendar.
            </h1>
            <p className="text-cc-prose mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
              Fusion lays out subgraphs on a grid, validates the whole month,
              then dispatches each query. Composition runs at planning time on
              ASP.NET Core, so the schema your clients see has already been
              proven answerable across every subgraph.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/fusion">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
          </div>

          {/* Phase header strip. */}
          <div className="border-cc-card-border mt-10 grid grid-cols-7 gap-px overflow-hidden rounded-md border bg-black/10">
            {PHASES.map((p) => (
              <div
                key={p}
                className="bg-cc-card-bg flex items-center justify-center px-1 py-3 sm:py-4"
              >
                <span className="text-cc-nav-label font-mono text-[9px] tracking-[0.18em] uppercase sm:text-[10.5px] sm:tracking-[0.22em]">
                  {p}
                </span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* COMPOSITION CALENDAR: 7-col, 5-row grid of capability cells. */}
      <section
        aria-label="Composition calendar"
        className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-xl border py-12 sm:py-16"
      >
        <LedgerPattern />
        <div className="relative px-4 sm:px-8">
          <div className="mb-8 flex items-end justify-between gap-4">
            <div>
              <MonoLabel>The month / 35 cells</MonoLabel>
              <h2 className="text-cc-heading font-heading mt-3 max-w-3xl text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
                Each cell, a capability. One cell, today.
              </h2>
            </div>
            <div className="hidden sm:block">
              <MonoLabel tone="accent">WK 1 / 5</MonoLabel>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-2 sm:grid-cols-7 sm:gap-2.5">
            {CALENDAR.map((cell, i) => (
              <CalendarCell
                key={cell.day}
                day={cell.day}
                title={cell.title}
                body={cell.body}
                span={cell.span}
                today={cell.today}
                index={i}
              />
            ))}
          </div>
        </div>
      </section>

      {/* The one allowed spectrum moment: hairline rule with a mono caption. */}
      <section
        aria-label="The cycle continues"
        className="relative py-12 sm:py-16"
      >
        <div className="flex items-center gap-4">
          <div className="flex-1">
            <div
              aria-hidden
              className="h-px w-full"
              style={{ background: SPECTRUM }}
            />
          </div>
          <MonoLabel>The cycle continues</MonoLabel>
          <div className="flex-1">
            <div
              aria-hidden
              className="h-px w-full"
              style={{ background: SPECTRUM }}
            />
          </div>
        </div>
      </section>

      {/* WEEK DETAIL: code sample on left 4 cols, phase callouts on right 3. */}
      <section
        aria-label="One week, expanded"
        className="border-cc-card-border border-t py-16 sm:py-20"
      >
        <div className="mb-10 flex flex-col gap-3">
          <MonoLabel>Week detail / compose, validate, plan</MonoLabel>
          <h2 className="text-cc-heading font-heading max-w-3xl text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            One command, three planner phases.
          </h2>
          <p className="text-cc-prose max-w-2xl text-base leading-relaxed sm:text-lg">
            The compose CLI reads each subgraph, merges them, runs
            satisfiability over the result, and emits a versioned archive. The
            same artifact powers planning at runtime, so every served query is
            one the gateway already proved it could answer.
          </p>
        </div>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-7">
          <div className="lg:col-span-4">
            <ComposeSample />
          </div>
          <div className="flex flex-col gap-4 lg:col-span-3">
            <PhaseCallout
              phase="COMPOSE"
              title="Merge sources"
              body="Subgraph SDLs are parsed, enriched, and merged with stable diagnostics on conflict."
            />
            <PhaseCallout
              phase="VALIDATE"
              title="Prove satisfiability"
              body="Every reachable field gets a resolver path. Unreachable shapes fail composition."
            />
            <PhaseCallout
              phase="PLAN"
              title="Emit a plan"
              body="A versioned, inspectable archive (gateway.far) feeds the runtime planner."
            />
          </div>
        </div>
      </section>

      {/* SUBGRAPH ROSTER: a 7-tile strip. */}
      <section
        aria-label="Subgraph roster"
        className="border-cc-card-border border-t py-16 sm:py-20"
      >
        <div className="mb-8 flex flex-col gap-2">
          <MonoLabel>Inputs / seven subgraphs</MonoLabel>
          <h2 className="text-cc-heading font-heading max-w-3xl text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Independent services, one composed graph.
          </h2>
        </div>
        <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-4 lg:grid-cols-7">
          {[
            { letter: "I", label: "Inventory" },
            { letter: "P", label: "Pricing" },
            { letter: "R", label: "Reviews" },
            { letter: "Id", label: "Identity" },
            { letter: "O", label: "Orders" },
            { letter: "S", label: "Search" },
            { letter: "C", label: "Catalog" },
          ].map((m) => (
            <Monogram key={m.label} letter={m.letter} label={m.label} />
          ))}
        </div>
      </section>

      {/* APOLLO FEDERATION: one wide full-bleed featured cell. */}
      <section
        aria-label="Apollo Federation compatibility"
        className="border-cc-card-border border-t py-16 sm:py-20"
      >
        <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6 sm:p-10">
          <div className="grid items-start gap-10 lg:grid-cols-12">
            <div className="lg:col-span-5">
              <MonoLabel tone="accent">FEATURED EVENT</MonoLabel>
              <h2 className="text-cc-heading font-heading mt-3 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
                Apollo Federation, on an open spec.
              </h2>
              <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed">
                Fusion implements the GraphQL Composite Schemas specification
                under the GraphQL Foundation, and reads Apollo Federation v2
                subgraphs through a dedicated connector. Bring existing
                directives into a Fusion composition without rewriting
                resolvers. Fusion runs the gateway itself, always in your own
                infrastructure.
              </p>
            </div>
            <div className="lg:col-span-7">
              <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                {[
                  "@key for entity identification",
                  "@requires for field dependencies",
                  "@provides for selection hints",
                  "@external markers on borrowed fields",
                  "Federation v2 subgraph SDLs read as-is",
                  "Hot Chocolate servers are valid subgraphs",
                  "Composite Schemas spec, vendor-neutral",
                  "Documented directive-by-directive migration",
                ].map((item) => (
                  <li
                    key={item}
                    className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
                  >
                    <span className="text-cc-accent mt-1 shrink-0">
                      <CheckIcon size={14} />
                    </span>
                    <span>{item}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      </section>

      {/* HONEST CONSTRAINTS: 7-col footer grid. */}
      <section
        aria-label="Honest constraints"
        className="border-cc-card-border border-t py-16 sm:py-20"
      >
        <div className="mb-8 flex flex-col gap-2">
          <MonoLabel>Footer / what fusion is and isn&apos;t</MonoLabel>
          <h2 className="text-cc-heading font-heading max-w-3xl text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            The honest constraints.
          </h2>
        </div>
        <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2 lg:grid-cols-4">
          {[
            {
              tag: "GATEWAY",
              title: "Always self-run",
              body: "Fusion runs in your infrastructure. Nitro cloud is never in the request path.",
            },
            {
              tag: "IDE",
              title: "Served from endpoint",
              body: "The GraphQL IDE is served from the gateway endpoint, scoped by configuration.",
            },
            {
              tag: "TELEMETRY",
              title: "Configured via Nitro",
              body: "OpenTelemetry pipeline is configured through Nitro for the gateway.",
            },
            {
              tag: "BREAKING CHANGE",
              title: "Published clients affected",
              body: "On a breaking change, you can see which published clients are affected.",
            },
          ].map((c) => (
            <div
              key={c.tag}
              className="border-cc-card-border bg-cc-card-bg rounded-md border p-4"
            >
              <MonoLabel>{c.tag}</MonoLabel>
              <h3 className="text-cc-heading font-heading mt-2 text-[15px] font-semibold tracking-tight">
                {c.title}
              </h3>
              <p className="text-cc-ink-dim mt-1 text-[12.5px] leading-snug">
                {c.body}
              </p>
            </div>
          ))}
        </div>
      </section>

      {/* CTA FOOTER: calendar-style footer row. */}
      <section
        aria-label="Get started"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="flex flex-col items-center gap-5 text-center">
          <MonoLabel tone="accent">Next cycle -&gt;</MonoLabel>
          <h2 className="text-cc-heading font-heading mx-auto max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Plan the month, compose the graph, ship the gateway.
          </h2>
          <p className="text-cc-prose mx-auto max-w-2xl text-base leading-relaxed sm:text-lg">
            Point Fusion at your subgraphs, compose in CI, and serve from a
            single .NET endpoint you operate yourself. The plan is built, the
            satisfiability is proven, and the runtime is the ASP.NET Core you
            already run.
          </p>
          <div className="mt-4 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/fusion">Start with Fusion</SolidButton>
            <OutlineButton href="/docs/fusion">Read the docs</OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
