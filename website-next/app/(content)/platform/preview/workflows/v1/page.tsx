import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Workflows: Mocha Mediator & Message Bus for .NET",
  description:
    "Let work continue after the request. Mocha is a source-generated in-process mediator and cross-service message bus for .NET, with pluggable transports, validated sagas, and outbox reliability.",
  keywords: [
    "mocha",
    "dotnet message bus",
    "in-process mediator",
    "CQRS",
    "sagas",
    "transactional outbox",
    "idempotent inbox",
    "exactly-once processing",
    "RabbitMQ",
    "Kafka",
    "Postgres transport",
    "Azure Service Bus",
    "event-driven architecture",
    "OpenTelemetry tracing",
  ],
  openGraph: {
    title: "Mocha Workflows: mediator + message bus for .NET",
    description:
      "Hand the slow work to a message and let it keep moving after the response goes out. One source-generated framework for in-process CQRS and cross-service messaging.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/*  Scene accent: amber #f59e0b warming toward coral #f0786a.          */
/*  The accent tracks the single running message across every visual.  */
/* ------------------------------------------------------------------ */

const AMBER = "#f59e0b";
const CORAL = "#f0786a";

/* ============================  Hero switchboard  ===================== */

interface SpineNodeProps {
  readonly label: string;
  readonly sub: string;
  readonly state: "done" | "live" | "pending";
}

function SpineNode({ label, sub, state }: SpineNodeProps) {
  const ring =
    state === "live"
      ? "border-[color:var(--w-accent)] bg-[color:var(--w-accent-wash)]"
      : state === "done"
        ? "border-cc-card-border bg-cc-surface/80"
        : "border-cc-card-border bg-cc-surface/40";
  const dot =
    state === "live"
      ? "var(--w-accent)"
      : state === "done"
        ? "var(--color-cc-accent)"
        : "rgba(245,241,234,0.28)";
  return (
    <div
      className={`flex min-w-0 items-center gap-2.5 rounded-lg border px-3 py-2 ${ring}`}
    >
      <span
        className="size-2 shrink-0 rounded-full"
        style={{
          backgroundColor: dot,
          boxShadow:
            state === "live"
              ? "0 0 0 4px color-mix(in srgb, var(--w-accent) 22%, transparent)"
              : "none",
        }}
      />
      <span className="min-w-0">
        <span className="text-cc-heading block truncate font-mono text-[12px] leading-tight">
          {label}
        </span>
        <span className="text-cc-nav-label block truncate font-mono text-[10px] leading-tight">
          {sub}
        </span>
      </span>
    </div>
  );
}

interface SpanConnectorProps {
  readonly label: string;
  readonly ms: string;
  readonly state: "done" | "live";
}

function SpanConnector({ label, ms, state }: SpanConnectorProps) {
  return (
    <div className="flex items-center gap-2 px-1">
      <svg
        viewBox="0 0 120 12"
        className="h-3 w-full"
        preserveAspectRatio="none"
        aria-hidden
      >
        <line
          x1="0"
          y1="6"
          x2="120"
          y2="6"
          stroke={
            state === "live" ? "var(--w-accent)" : "rgba(245,241,234,0.18)"
          }
          strokeWidth="1.5"
          strokeDasharray={state === "live" ? "5 4" : "0"}
          className={state === "live" ? "w-flow" : ""}
        />
        <polygon
          points="114,2 120,6 114,10"
          fill={state === "live" ? "var(--w-accent)" : "rgba(245,241,234,0.32)"}
        />
      </svg>
      <span className="text-cc-nav-label shrink-0 font-mono text-[10px] whitespace-nowrap">
        {label} · <span className="text-cc-ink">{ms}</span>
      </span>
    </div>
  );
}

function HeroSwitchboard() {
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur-md"
      style={
        {
          "--w-accent": AMBER,
          "--w-accent-wash": "color-mix(in srgb, #f59e0b 12%, transparent)",
        } as React.CSSProperties
      }
    >
      {/* window chrome */}
      <div className="border-cc-card-border bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <div className="flex items-center gap-2">
          <span className="size-2.5 rounded-full bg-[#f0786a]/60" />
          <span className="size-2.5 rounded-full bg-[#f59e0b]/60" />
          <span className="bg-cc-accent/60 size-2.5 rounded-full" />
        </div>
        <span className="text-cc-nav-label font-mono text-[11px]">
          mocha · topology · live
        </span>
        <span className="text-cc-ink-dim flex items-center gap-1.5 font-mono text-[11px]">
          <span
            className="size-1.5 rounded-full"
            style={{ backgroundColor: AMBER }}
          />
          1 in flight
        </span>
      </div>

      <div className="p-4 sm:p-6">
        {/* origin pill */}
        <div className="mb-4 flex flex-wrap items-center gap-3">
          <div className="border-cc-card-border bg-cc-surface/80 flex items-center gap-2 rounded-lg border px-3 py-2">
            <svg
              viewBox="0 0 16 16"
              className="text-cc-ink size-3.5"
              aria-hidden
            >
              <path
                d="M3 8h10M8 3v10"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeLinecap="round"
              />
            </svg>
            <span className="text-cc-heading font-mono text-[12px]">
              POST /reviews
            </span>
          </div>
          <span className="text-cc-nav-label font-mono text-[11px]">
            handler returns 201 — work keeps moving
          </span>
        </div>

        {/* Lane A: in-process mediator */}
        <div className="mb-2 flex items-center gap-2">
          <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
            mediator · in-process
          </span>
          <span className="bg-cc-card-border h-px flex-1" />
        </div>
        <div className="grid grid-cols-[1.1fr_auto_1fr] items-center gap-1 sm:gap-2">
          <SpineNode label="CreateReview" sub="ICommand" state="done" />
          <div className="w-28 sm:w-40">
            <SpanConnector label="dispatch" ms="0.4ms" state="done" />
          </div>
          <SpineNode label="ReviewHandler" sub="[Handler]" state="done" />
        </div>

        {/* shared spine drop */}
        <div className="bg-cc-card-border my-1 ml-[14px] h-5 w-px" />

        {/* Lane B: cross-service bus, the live hop */}
        <div className="mb-2 flex items-center gap-2">
          <span
            className="font-mono text-[10px] tracking-[0.18em] uppercase"
            style={{ color: AMBER }}
          >
            message bus · cross-service
          </span>
          <span className="bg-cc-card-border h-px flex-1" />
        </div>
        <div className="grid grid-cols-[1.1fr_auto_1fr] items-center gap-1 sm:gap-2">
          <SpineNode label="ReviewCreated" sub="PublishAsync" state="live" />
          <div className="w-28 sm:w-40">
            <SpanConnector label="rabbitmq" ms="…" state="live" />
          </div>
          <SpineNode
            label="SearchIndexer"
            sub="IEventHandler"
            state="pending"
          />
        </div>

        {/* fan-out subscribers */}
        <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-3">
          {[
            ["NotifyAuthor", "queued"],
            ["UpdateScore", "queued"],
            ["WarmCache", "queued"],
          ].map(([n, s]) => (
            <div
              key={n}
              className="border-cc-card-border bg-cc-surface/30 flex items-center justify-between rounded-md border border-dashed px-2.5 py-1.5"
            >
              <span className="text-cc-ink font-mono text-[11px]">{n}</span>
              <span className="text-cc-nav-label font-mono text-[10px]">
                {s}
              </span>
            </div>
          ))}
        </div>

        {/* outbox / inbox reliability footer */}
        <div className="border-cc-card-border mt-4 flex flex-wrap items-center gap-2 border-t pt-3">
          <span className="text-cc-nav-label font-mono text-[10px]">
            guaranteed by
          </span>
          <span className="border-cc-card-border text-cc-ink rounded border px-2 py-0.5 font-mono text-[10px]">
            transactional outbox
          </span>
          <span className="border-cc-card-border text-cc-ink rounded border px-2 py-0.5 font-mono text-[10px]">
            idempotent inbox
          </span>
          <span
            className="ml-auto font-mono text-[10px]"
            style={{ color: CORAL }}
          >
            exactly-once processing
          </span>
        </div>
      </div>
    </div>
  );
}

/* ============================  Mediator vs Bus lanes  ================= */

interface LaneProps {
  readonly kind: "mediator" | "bus";
  readonly eyebrow: string;
  readonly title: string;
  readonly steps: readonly string[];
  readonly note: string;
}

function DispatchLane({ kind, eyebrow, title, steps, note }: LaneProps) {
  const accent = kind === "bus";
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 backdrop-blur-sm"
      style={
        accent ? ({ "--w-accent": AMBER } as React.CSSProperties) : undefined
      }
    >
      <p
        className="mb-1 font-mono text-[11px] tracking-[0.18em] uppercase"
        style={{ color: accent ? AMBER : "var(--color-cc-accent)" }}
      >
        {eyebrow}
      </p>
      <h3 className="font-heading text-h6 text-cc-heading mb-4">{title}</h3>
      <div className="flex items-center gap-1.5 overflow-x-auto pb-1">
        {steps.map((s, i) => (
          <div key={s} className="flex shrink-0 items-center gap-1.5">
            <span className="border-cc-card-border bg-cc-surface/70 text-cc-heading rounded-md border px-2.5 py-1.5 font-mono text-[11px]">
              {s}
            </span>
            {i < steps.length - 1 && (
              <svg viewBox="0 0 24 10" className="h-2.5 w-6" aria-hidden>
                <line
                  x1="0"
                  y1="5"
                  x2="18"
                  y2="5"
                  stroke={accent ? AMBER : "rgba(245,241,234,0.35)"}
                  strokeWidth="1.5"
                  strokeDasharray={accent ? "4 3" : "0"}
                />
                <polygon
                  points="17,1 24,5 17,9"
                  fill={accent ? AMBER : "rgba(245,241,234,0.5)"}
                />
              </svg>
            )}
          </div>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-4 text-sm">{note}</p>
    </div>
  );
}

/* ============================  Saga strip  ========================== */

interface SagaStateProps {
  readonly label: string;
  readonly state: "done" | "live" | "pending";
  readonly first?: boolean;
}

function SagaState({ label, state, first }: SagaStateProps) {
  const fill =
    state === "live"
      ? AMBER
      : state === "done"
        ? "var(--color-cc-accent)"
        : "rgba(245,241,234,0.3)";
  const tone = state === "live" ? AMBER : "#5eead4";
  return (
    <div className="flex items-center gap-2 sm:gap-3">
      {!first && (
        <svg viewBox="0 0 40 12" className="h-3 w-8 sm:w-12" aria-hidden>
          <line
            x1="0"
            y1="6"
            x2="34"
            y2="6"
            stroke={state === "pending" ? "rgba(245,241,234,0.2)" : AMBER}
            strokeWidth="1.5"
            strokeDasharray={state === "pending" ? "4 3" : "0"}
          />
          <polygon
            points="33,2 40,6 33,10"
            fill={state === "pending" ? "rgba(245,241,234,0.3)" : AMBER}
          />
        </svg>
      )}
      <div
        className="flex items-center gap-2 rounded-full border px-3.5 py-1.5"
        style={{
          borderColor:
            state === "pending"
              ? "var(--color-cc-card-border)"
              : `color-mix(in srgb, ${tone} 50%, transparent)`,
          backgroundColor:
            state === "pending"
              ? "transparent"
              : `color-mix(in srgb, ${tone} 10%, transparent)`,
        }}
      >
        <span
          className="size-2 rounded-full"
          style={{ backgroundColor: fill }}
        />
        <span className="text-cc-heading font-mono text-[12px]">{label}</span>
      </div>
    </div>
  );
}

/* ============================  Tangle before/after  ================ */

function TangleBefore() {
  const lines = [
    "var conn = factory.CreateConnection();",
    "var channel = conn.CreateModel();",
    'channel.QueueDeclare("reviews", durable: true);',
    "// dedup: have we seen this message id?",
    "if (await _seen.ContainsAsync(msg.Id)) return;",
    "// retry with backoff, then dead-letter…",
    "for (var i = 0; i < maxRetries; i++) { … }",
    'channel.BasicPublish("", "reviews.dlq", body);',
    "// outbox: now wire the DB transaction by hand",
    "await _tx.SaveChangesAsync(); await _bus.Flush();",
  ];
  return (
    <div className="bg-cc-code-bg overflow-hidden rounded-xl border border-[color:rgba(240,120,106,0.3)]">
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2">
        <span className="text-cc-nav-label font-mono text-[11px]">
          MessagingPlumbing.cs
        </span>
        <span className="font-mono text-[10px]" style={{ color: CORAL }}>
          hand-rolled · 184 lines
        </span>
      </div>
      <pre className="text-cc-ink-dim overflow-x-auto px-4 py-3 font-mono text-[11px] leading-relaxed">
        {lines.map((l, i) => (
          <div key={i} className="whitespace-pre">
            <span className="text-cc-nav-label mr-3 select-none">
              {String(i + 1).padStart(2, "0")}
            </span>
            {l}
          </div>
        ))}
      </pre>
    </div>
  );
}

function TangleAfter() {
  return (
    <div
      className="bg-cc-code-bg overflow-hidden rounded-xl border"
      style={{ borderColor: "color-mix(in srgb, #f59e0b 38%, transparent)" }}
    >
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2">
        <span className="text-cc-nav-label font-mono text-[11px]">
          ReviewHandlers.cs
        </span>
        <span className="font-mono text-[10px]" style={{ color: AMBER }}>
          one definition
        </span>
      </div>
      <pre className="overflow-x-auto px-4 py-3 font-mono text-[12px] leading-relaxed">
        <code>
          <span className="text-cc-nav-label">{"[Handler]"}</span>
          {"\n"}
          <span className="text-cc-ink-dim">public async </span>
          <span style={{ color: AMBER }}>Task</span>
          <span className="text-cc-ink-dim"> Handle(</span>
          {"\n"}
          {"  "}
          <span className="text-cc-heading">CreateReview</span>
          <span className="text-cc-ink-dim"> command,</span>
          {"\n"}
          {"  "}
          <span className="text-cc-heading">IMessageBus</span>
          <span className="text-cc-ink-dim"> bus)</span>
          {"\n"}
          <span className="text-cc-ink-dim">{"{"}</span>
          {"\n"}
          {"  "}
          <span className="text-cc-ink-dim">var review = </span>
          <span className="text-cc-heading">Review</span>
          <span className="text-cc-ink-dim">.Draft(command);</span>
          {"\n\n"}
          {"  "}
          <span className="text-cc-nav-label">
            {"// committed with the DB write, sent once"}
          </span>
          {"\n"}
          {"  "}
          <span className="text-cc-ink-dim">await bus.</span>
          <span style={{ color: CORAL }}>PublishAsync</span>
          <span className="text-cc-ink-dim">(</span>
          {"\n"}
          {"    "}
          <span className="text-cc-ink-dim">new </span>
          <span className="text-cc-heading">ReviewCreated</span>
          <span className="text-cc-ink-dim">(review.Id));</span>
          {"\n"}
          <span className="text-cc-ink-dim">{"}"}</span>
        </code>
      </pre>
    </div>
  );
}

/* ============================  Transports row  ===================== */

interface TransportChipProps {
  readonly name: string;
  readonly tag: string;
  readonly highlight?: boolean;
}

function TransportChip({ name, tag, highlight }: TransportChipProps) {
  return (
    <div
      className="bg-cc-surface/60 flex items-center justify-between rounded-lg border px-3.5 py-2.5"
      style={{
        borderColor: highlight
          ? "color-mix(in srgb, #f59e0b 45%, transparent)"
          : "var(--color-cc-card-border)",
      }}
    >
      <span className="text-cc-heading font-mono text-[13px]">{name}</span>
      <span
        className="font-mono text-[10px] tracking-wide uppercase"
        style={{ color: highlight ? AMBER : "var(--color-cc-nav-label)" }}
      >
        {tag}
      </span>
    </div>
  );
}

/* ============================  Trace ribbon  ======================= */

interface TraceSpanProps {
  readonly label: string;
  readonly widthPct: number;
  readonly offsetPct: number;
  readonly live?: boolean;
}

function TraceSpan({ label, widthPct, offsetPct, live }: TraceSpanProps) {
  return (
    <div className="flex items-center gap-3">
      <span className="text-cc-ink w-32 shrink-0 truncate font-mono text-[11px]">
        {label}
      </span>
      <div className="bg-cc-surface/50 relative h-5 flex-1 rounded">
        <div
          className="absolute top-0 h-5 rounded"
          style={{
            left: `${offsetPct}%`,
            width: `${widthPct}%`,
            background: live
              ? `linear-gradient(90deg, ${AMBER}, ${CORAL})`
              : "rgba(94,234,212,0.35)",
            boxShadow: live
              ? "0 0 12px color-mix(in srgb, #f59e0b 40%, transparent)"
              : "none",
          }}
        />
      </div>
    </div>
  );
}

/* ============================  Shared bits  ======================== */

interface SectionLabelProps {
  readonly children: React.ReactNode;
}

function SectionLabel({ children }: SectionLabelProps) {
  return (
    <p
      className="mb-3 font-mono text-[11px] tracking-[0.2em] uppercase"
      style={{ color: AMBER }}
    >
      {children}
    </p>
  );
}

/* ==============================  Page  ============================= */

export default function WorkflowsPreviewV1() {
  return (
    <div className="flex flex-col gap-28 py-6">
      {/* keyframes for the single running message; reduced-motion safe */}
      <style>{`
        @keyframes w-dashflow { to { stroke-dashoffset: -18; } }
        .w-flow { animation: w-dashflow 0.9s linear infinite; }
        @media (prefers-reduced-motion: reduce) {
          .w-flow { animation: none; }
        }
      `}</style>

      {/* ---------------------------- HERO ---------------------------- */}
      <section className="grid items-center gap-12 lg:grid-cols-[1fr_1.05fr]">
        <div>
          <p className="border-cc-card-border bg-cc-card-bg text-cc-ink-dim mb-5 inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[11px]">
            <span
              className="size-1.5 rounded-full"
              style={{ backgroundColor: AMBER }}
            />
            Mocha · mediator + message bus
          </p>
          <h1 className="font-heading text-h2 text-cc-heading">
            Let work continue
            <br />
            after the request.
          </h1>
          <p className="lead text-cc-ink-dim mt-6 max-w-xl">
            Return the response the instant the user needs it. Hand the slow,
            fan-out, cross-service work to a message and let it keep moving on
            its own.
          </p>
          <p className="text-cc-prose mt-5 max-w-xl">
            Mocha is one source-generated framework for both the in-process
            command you dispatch and the event you publish across services. Same
            handler-first model, same traces, whichever way the work travels.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
          </div>
          <ul className="mt-8 flex flex-wrap gap-x-6 gap-y-2">
            {[
              "Source-generated dispatch",
              "Validated sagas",
              "Outbox + inbox reliability",
            ].map((t) => (
              <li
                key={t}
                className="text-cc-ink-dim flex items-center gap-2 text-sm"
              >
                <span style={{ color: AMBER }}>
                  <CheckIcon />
                </span>
                {t}
              </li>
            ))}
          </ul>
        </div>
        <HeroSwitchboard />
      </section>

      {/* ---------------------- TWO DISPATCH LANES -------------------- */}
      <section>
        <div className="max-w-2xl">
          <SectionLabel>two ways to dispatch · one model</SectionLabel>
          <h2 className="font-heading text-h3 text-cc-heading">
            In-process when it&apos;s near. On the bus when it&apos;s far.
          </h2>
          <p className="text-cc-prose mt-4">
            Inside one process, the mediator dispatches commands and queries
            straight to a{" "}
            <span className="text-cc-ink font-mono">[Handler]</span> through a
            pre-compiled pipeline. When the work belongs to another service, the
            same publish crosses a transport and fans out to its consumers. You
            change the verb, not the mental model.
          </p>
        </div>
        <div className="mt-8 grid gap-5 lg:grid-cols-2">
          <DispatchLane
            kind="mediator"
            eyebrow="mediator · in-process CQRS"
            title="Dispatch and reply, no hops"
            steps={["CreateReview", "ISender", "[Handler]", "Result"]}
            note="Commands, queries, and notifications resolve through a source-generated pipeline. No reflection, no service-locator lookup on the hot path."
          />
          <DispatchLane
            kind="bus"
            eyebrow="message bus · cross-service"
            title="Publish and fan out, durably"
            steps={["ReviewCreated", "PublishAsync", "transport", "consumers"]}
            note="One event reaches every interested service through a pluggable transport, with outbox and inbox guaranteeing each consumer processes it exactly once."
          />
        </div>
      </section>

      {/* -------------------------- SAGA STRIP ------------------------ */}
      <section>
        <div className="max-w-2xl">
          <SectionLabel>sagas · stateful workflows</SectionLabel>
          <h2 className="font-heading text-h3 text-cc-heading">
            A workflow that can&apos;t get stuck.
          </h2>
          <p className="text-cc-prose mt-4">
            A review moves{" "}
            <span className="text-cc-ink font-mono">
              Draft → Checked → Published
            </span>{" "}
            across several messages and minutes. Define that state machine once;
            Mocha checks that every state is reachable and every path reaches a
            final state, validated before the service handles traffic, so a saga
            that can dead-end never makes it into production.
          </p>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-2xl border p-6 backdrop-blur-sm sm:p-8">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
              ReviewSaga
            </span>
            <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px]">
              <CheckIcon />
              <span style={{ color: AMBER }}>
                validated · all paths terminal
              </span>
            </span>
          </div>
          <div className="mt-6 flex flex-wrap items-center gap-y-4">
            <SagaState label="Draft" state="done" first />
            <SagaState label="Checked" state="live" />
            <SagaState label="Published" state="pending" />
          </div>
          <div className="border-cc-card-border mt-6 grid gap-3 border-t pt-5 sm:grid-cols-3">
            {[
              ["solid hop", "done · committed", "var(--color-cc-accent)"],
              ["running hop", "in flight now", AMBER],
              [
                "dashed hop",
                "pending · not yet reached",
                "rgba(245,241,234,0.4)",
              ],
            ].map(([k, v, c]) => (
              <div key={k} className="flex items-center gap-2">
                <span
                  className="size-2 rounded-full"
                  style={{ backgroundColor: c }}
                />
                <span className="text-cc-ink font-mono text-[11px]">{k}</span>
                <span className="text-cc-nav-label font-mono text-[11px]">
                  — {v}
                </span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* --------------------- COLLAPSE THE TANGLE -------------------- */}
      <section>
        <div className="max-w-2xl">
          <SectionLabel>before / after</SectionLabel>
          <h2 className="font-heading text-h3 text-cc-heading">
            Collapse the plumbing.
          </h2>
          <p className="text-cc-prose mt-4">
            Reliable messaging usually means hand-rolling a broker connection,
            retry and backoff, a dead-letter path, a dedup table, and an outbox
            wired into your transaction. Mocha owns all of that. You write the
            handler and the publish.
          </p>
        </div>
        <div className="mt-8 grid items-stretch gap-4 lg:grid-cols-[1fr_auto_1fr]">
          <TangleBefore />
          <div className="flex items-center justify-center">
            <div
              className="rounded-full border px-3 py-2 font-mono text-[11px]"
              style={{
                borderColor: "color-mix(in srgb, #f59e0b 45%, transparent)",
                color: AMBER,
              }}
            >
              →
            </div>
          </div>
          <TangleAfter />
        </div>
      </section>

      {/* ------------------------- TRANSPORTS ------------------------ */}
      <section>
        <div className="max-w-2xl">
          <SectionLabel>pluggable transports</SectionLabel>
          <h2 className="font-heading text-h3 text-cc-heading">
            Swap the broker, keep the handlers.
          </h2>
          <p className="text-cc-prose mt-4">
            The transport is a registration detail, not a rewrite. Start
            in-process, move to Postgres or RabbitMQ in production, route
            high-throughput streams through Kafka, and run several at once. Your
            handlers never know the difference.
          </p>
        </div>
        <div className="mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <TransportChip name="RabbitMQ" tag="broker" highlight />
          <TransportChip name="Postgres" tag="durable" />
          <TransportChip name="Kafka" tag="streaming" />
          <TransportChip name="Azure Service Bus" tag="cloud" />
          <TransportChip name="In-process" tag="zero-infra" />
          <TransportChip name="Azure Event Hub" tag="ingest" />
        </div>
        <div className="mt-4 flex flex-wrap gap-3">
          {[
            "transactional outbox",
            "idempotent inbox",
            "dead-letter routing",
            "retry + redelivery",
            "delayed delivery",
          ].map((b) => (
            <span
              key={b}
              className="border-cc-card-border bg-cc-surface/50 text-cc-ink-dim flex items-center gap-1.5 rounded-full border px-3 py-1.5 font-mono text-[11px]"
            >
              <span style={{ color: AMBER }}>
                <CheckIcon size={12} />
              </span>
              {b}
            </span>
          ))}
        </div>
      </section>

      {/* ------------------------- TRACE RIBBON ---------------------- */}
      <section className="grid items-center gap-12 lg:grid-cols-[0.9fr_1.1fr]">
        <div>
          <SectionLabel>observability · every hop a span</SectionLabel>
          <h2 className="font-heading text-h3 text-cc-heading">
            Follow one message, hop by hop.
          </h2>
          <p className="text-cc-prose mt-4">
            Publish, dispatch, receive, and consume each emit a real
            OpenTelemetry span, with the correlation id propagating across every
            service boundary. The same trace opens in Nitro, so you can watch
            the in-flight message advance the way you reason about it.
          </p>
          <ul className="mt-6 space-y-2">
            {[
              "Correlation id carried across services",
              "Spans for dispatch, transport, and handler",
              "Zero overhead when the observer is off",
            ].map((t) => (
              <li
                key={t}
                className="text-cc-ink-dim flex items-center gap-2 text-sm"
              >
                <span style={{ color: AMBER }}>
                  <CheckIcon />
                </span>
                {t}
              </li>
            ))}
          </ul>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
          <div className="border-cc-card-border mb-4 flex items-center justify-between border-b pb-3">
            <span className="text-cc-heading font-mono text-[11px]">
              trace · review.created
            </span>
            <span className="text-cc-nav-label font-mono text-[10px]">
              42.6ms · 5 spans
            </span>
          </div>
          <div className="space-y-2.5">
            <TraceSpan label="POST /reviews" widthPct={100} offsetPct={0} />
            <TraceSpan label="CreateReview" widthPct={9} offsetPct={2} />
            <TraceSpan label="outbox.commit" widthPct={14} offsetPct={11} />
            <TraceSpan
              label="publish→rabbitmq"
              widthPct={34}
              offsetPct={26}
              live
            />
            <TraceSpan label="SearchIndexer" widthPct={32} offsetPct={62} />
          </div>
          <div className="border-cc-card-border mt-4 flex items-center gap-2 border-t pt-3">
            <span
              className="inline-block h-2.5 w-6 rounded"
              style={{
                background: `linear-gradient(90deg, ${AMBER}, ${CORAL})`,
              }}
            />
            <span className="text-cc-nav-label font-mono text-[10px]">
              running message — the hop in flight right now
            </span>
          </div>
        </div>
      </section>

      {/* ------------------------ HONESTY BEAT ----------------------- */}
      <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
        <SectionLabel>what we mean precisely</SectionLabel>
        <h2 className="font-heading text-h4 text-cc-heading max-w-3xl">
          Reliability claims, stated honestly.
        </h2>
        <div className="mt-6 grid gap-6 sm:grid-cols-3">
          {[
            {
              h: "Exactly-once processing",
              p: "The outbox commits the message with your database write, and the inbox deduplicates on receive. That gives exactly-once processing, not exactly-once delivery, which no transport can promise.",
            },
            {
              h: "Sagas validated before traffic",
              p: "The state-machine check runs before the service handles traffic, not at compile time. It proves your saga can always reach a final state.",
            },
            {
              h: "Published clients aren't surprised",
              p: "Because dispatch is source-generated and contracts are explicit, a changed message shows up at build time, so you can see which published clients are affected.",
            },
          ].map((c) => (
            <div key={c.h}>
              <h3 className="font-heading text-h6 text-cc-heading mb-2">
                {c.h}
              </h3>
              <p className="text-cc-ink-dim text-sm">{c.p}</p>
            </div>
          ))}
        </div>
      </section>

      {/* --------------------------- CLOSE --------------------------- */}
      <section className="flex flex-col items-center gap-6 text-center">
        <div
          className="mx-auto h-px w-40"
          style={{
            background: `linear-gradient(90deg, transparent, ${AMBER}, ${CORAL}, transparent)`,
          }}
        />
        <h2 className="font-heading text-h3 text-cc-heading max-w-2xl">
          Ship the response. Keep the work moving.
        </h2>
        <p className="text-cc-prose max-w-xl">
          One framework for the command you dispatch in-process and the event
          you publish across services, with reliability and traces built in.
        </p>
        <div className="mt-2 flex flex-wrap justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
