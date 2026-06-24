"use client";

import { motion } from "framer-motion";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/*  The Workflow Quarterly. One accent: coral #f0786a. Brand spectrum  */
/*  (cyan, violet, coral) appears ONCE, on the in-flight trace span.   */
/* ------------------------------------------------------------------ */

const CORAL = "#f0786a";
const SPECTRUM_CYAN = "#16b9e4";
const SPECTRUM_VIOLET = "#7c92c6";

/* ===========================  Margin primitives  ==================== */

interface MarginKickerProps {
  readonly children: ReactNode;
}

function MarginKicker({ children }: MarginKickerProps) {
  return (
    <p
      className="font-mono text-[11px] tracking-[0.22em] uppercase"
      style={{ color: CORAL }}
    >
      {children}
    </p>
  );
}

interface PullQuoteProps {
  readonly children: ReactNode;
}

function PullQuote({ children }: PullQuoteProps) {
  return (
    <p className="font-heading text-cc-heading text-[22px] leading-snug italic sm:text-[26px]">
      &ldquo;{children}&rdquo;
    </p>
  );
}

interface MarginNoteProps {
  readonly marker: string;
  readonly children: ReactNode;
}

function MarginNote({ marker, children }: MarginNoteProps) {
  return (
    <p className="text-cc-ink-dim font-mono text-[11px] leading-relaxed">
      <span className="text-cc-heading mr-1.5" style={{ color: CORAL }}>
        {marker}
      </span>
      {children}
    </p>
  );
}

function DottedDivider() {
  return (
    <span
      aria-hidden
      className="block h-px w-full"
      style={{
        backgroundImage:
          "repeating-linear-gradient(90deg, rgba(245,241,234,0.28) 0 2px, transparent 2px 6px)",
      }}
    />
  );
}

function CoralTick() {
  return (
    <span
      aria-hidden
      className="block h-px w-6"
      style={{ backgroundColor: CORAL, height: 1 }}
    />
  );
}

interface SectionNumeralProps {
  readonly n: string;
}

function SectionNumeral({ n }: SectionNumeralProps) {
  return (
    <motion.span
      initial={{ opacity: 0, x: -12 }}
      whileInView={{ opacity: 1, x: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.5, ease: "easeOut" }}
      className="font-heading block text-[88px] leading-none tracking-tight sm:text-[120px]"
      style={{
        color: "transparent",
        WebkitTextStroke: `1px ${CORAL}`,
      }}
    >
      {n}
    </motion.span>
  );
}

/* ==============================  Topology  ========================== */

interface SpineNodeProps {
  readonly label: string;
  readonly sub: string;
  readonly state: "done" | "live" | "pending";
}

function SpineNode({ label, sub, state }: SpineNodeProps) {
  const ringClass =
    state === "live"
      ? "border-[color:var(--w-accent)]"
      : "border-cc-card-border";
  const bgClass =
    state === "live"
      ? "bg-[color:var(--w-accent-wash)]"
      : state === "done"
        ? "bg-cc-surface/80"
        : "bg-cc-surface/40";
  const dot =
    state === "live"
      ? "var(--w-accent)"
      : state === "done"
        ? "var(--color-cc-accent)"
        : "rgba(245,241,234,0.28)";
  return (
    <div
      className={`flex min-w-0 items-center gap-2.5 rounded-lg border px-3 py-2 ${ringClass} ${bgClass}`}
    >
      <span
        className={`size-2 shrink-0 rounded-full ${state === "live" ? "w-pulse" : ""}`}
        style={{ backgroundColor: dot }}
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

function HeroTopologyCard() {
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur-md"
      style={
        {
          "--w-accent": CORAL,
          "--w-accent-wash": "color-mix(in srgb, #f0786a 12%, transparent)",
        } as CSSProperties
      }
    >
      <div className="border-cc-card-border bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px]">
          mocha · topology · live
        </span>
        <span className="text-cc-ink-dim flex items-center gap-1.5 font-mono text-[11px]">
          <span
            className="w-pulse size-1.5 rounded-full"
            style={{ backgroundColor: CORAL }}
          />
          1 in flight
        </span>
      </div>

      <div className="p-4 sm:p-6">
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
            handler returns 201, work keeps moving
          </span>
        </div>

        <div className="mb-2 flex items-center gap-2">
          <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
            mediator · in-process
          </span>
          <span className="bg-cc-card-border h-px flex-1" />
        </div>
        <div className="grid grid-cols-[1.1fr_auto_1fr] items-center gap-1 sm:gap-2">
          <SpineNode label="CreateReview" sub="ICommand" state="done" />
          <div className="w-24 sm:w-40">
            <SpanConnector label="dispatch" ms="0.4ms" state="done" />
          </div>
          <SpineNode label="ReviewHandler" sub="[Handler]" state="done" />
        </div>

        <div className="bg-cc-card-border my-1 ml-[14px] h-5 w-px" />

        <div className="mb-2 flex items-center gap-2">
          <span
            className="font-mono text-[10px] tracking-[0.18em] uppercase"
            style={{ color: CORAL }}
          >
            message bus · cross-service
          </span>
          <span className="bg-cc-card-border h-px flex-1" />
        </div>
        <div className="grid grid-cols-[1.1fr_auto_1fr] items-center gap-1 sm:gap-2">
          <SpineNode label="ReviewCreated" sub="PublishAsync" state="live" />
          <div className="w-24 sm:w-40">
            <SpanConnector label="rabbitmq" ms="…" state="live" />
          </div>
          <SpineNode
            label="SearchIndexer"
            sub="IEventHandler"
            state="pending"
          />
        </div>

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

/* ============================  Dispatch lanes  ===================== */

interface DispatchLaneProps {
  readonly kind: "mediator" | "bus";
  readonly eyebrow: string;
  readonly title: string;
  readonly steps: readonly string[];
  readonly note: string;
}

function DispatchLane({
  kind,
  eyebrow,
  title,
  steps,
  note,
}: DispatchLaneProps) {
  const accent = kind === "bus";
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 backdrop-blur-sm"
      style={accent ? ({ "--w-accent": CORAL } as CSSProperties) : undefined}
    >
      <p
        className="mb-1 font-mono text-[11px] tracking-[0.18em] uppercase"
        style={{ color: accent ? CORAL : "var(--color-cc-accent)" }}
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
                  stroke={accent ? CORAL : "rgba(245,241,234,0.35)"}
                  strokeWidth="1.5"
                  strokeDasharray={accent ? "4 3" : "0"}
                />
                <polygon
                  points="17,1 24,5 17,9"
                  fill={accent ? CORAL : "rgba(245,241,234,0.5)"}
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
      ? CORAL
      : state === "done"
        ? "var(--color-cc-accent)"
        : "rgba(245,241,234,0.3)";
  const tone = state === "live" ? CORAL : "#5eead4";
  return (
    <div className="flex items-center gap-2 sm:gap-3">
      {!first && (
        <svg viewBox="0 0 40 12" className="h-3 w-8 sm:w-12" aria-hidden>
          <line
            x1="0"
            y1="6"
            x2="34"
            y2="6"
            stroke={state === "pending" ? "rgba(245,241,234,0.2)" : CORAL}
            strokeWidth="1.5"
            strokeDasharray={state === "pending" ? "4 3" : "0"}
          />
          <polygon
            points="33,2 40,6 33,10"
            fill={state === "pending" ? "rgba(245,241,234,0.3)" : CORAL}
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
          className={`size-2 rounded-full ${state === "live" ? "w-pulse" : ""}`}
          style={{ backgroundColor: fill }}
        />
        <span className="text-cc-heading font-mono text-[12px]">{label}</span>
      </div>
    </div>
  );
}

/* ===========================  Before / After  ====================== */

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
      style={{ borderColor: "color-mix(in srgb, #f0786a 38%, transparent)" }}
    >
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2">
        <span className="text-cc-nav-label font-mono text-[11px]">
          ReviewHandlers.cs
        </span>
        <span className="font-mono text-[10px]" style={{ color: CORAL }}>
          11 lines
        </span>
      </div>
      <pre className="overflow-x-auto px-4 py-3 font-mono text-[12px] leading-relaxed">
        <code>
          <span className="text-cc-nav-label">{"[Handler]"}</span>
          {"\n"}
          <span className="text-cc-ink-dim">public async </span>
          <span style={{ color: CORAL }}>Task</span>
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

/* ============================  Transports  ========================== */

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
          ? "color-mix(in srgb, #f0786a 45%, transparent)"
          : "var(--color-cc-card-border)",
      }}
    >
      <span className="text-cc-heading font-mono text-[13px]">{name}</span>
      <span
        className="font-mono text-[10px] tracking-wide uppercase"
        style={{ color: highlight ? CORAL : "var(--color-cc-nav-label)" }}
      >
        {tag}
      </span>
    </div>
  );
}

/* =============================  Trace ribbon  ======================= */

interface TraceSpanProps {
  readonly label: string;
  readonly widthPct: number;
  readonly offsetPct: number;
  readonly live?: boolean;
}

function TraceSpanRow({ label, widthPct, offsetPct, live }: TraceSpanProps) {
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
              ? `linear-gradient(90deg, ${SPECTRUM_CYAN}, ${SPECTRUM_VIOLET}, ${CORAL})`
              : "rgba(94,234,212,0.35)",
          }}
        />
      </div>
    </div>
  );
}

/* =============================  Page  =============================== */

export function ClientPage() {
  return (
    <div className="relative">
      {/* paper-rule baseline grid + motion keyframes */}
      <style>{`
        @keyframes w-dashflow { to { stroke-dashoffset: -18; } }
        .w-flow { animation: w-dashflow 0.9s linear infinite; }
        @keyframes w-pulse-key {
          0%, 100% { box-shadow: 0 0 0 0 color-mix(in srgb, ${CORAL} 0%, transparent); opacity: 1; }
          50% { box-shadow: 0 0 0 5px color-mix(in srgb, ${CORAL} 25%, transparent); opacity: 0.85; }
        }
        .w-pulse { animation: w-pulse-key 1.8s ease-in-out infinite; }
        @media (prefers-reduced-motion: reduce) {
          .w-flow, .w-pulse { animation: none; }
        }
      `}</style>

      {/* paper-rule background, masked at top/bottom */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 z-0"
        style={{
          backgroundImage:
            "repeating-linear-gradient(0deg, rgba(245,241,234,0.025) 0 1px, transparent 1px 32px)",
          WebkitMaskImage:
            "linear-gradient(to bottom, transparent, black 8%, black 92%, transparent)",
          maskImage:
            "linear-gradient(to bottom, transparent, black 8%, black 92%, transparent)",
        }}
      />

      <div className="relative z-10 flex flex-col gap-24 py-6 sm:gap-28">
        {/* ===================  MASTHEAD  =================== */}
        <div className="border-cc-card-border flex flex-wrap items-center justify-between gap-3 border-y py-2.5">
          <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.22em] uppercase">
            The Workflow Quarterly
          </span>
          <span className="text-cc-ink-dim flex items-center gap-3 font-mono text-[11px]">
            <span>ISSUE 04</span>
            <span className="text-cc-nav-label">·</span>
            <span>VOL. MOCHA</span>
            <span className="text-cc-nav-label">·</span>
            <span>Q2</span>
          </span>
          <span
            className="font-mono text-[11px] tracking-[0.22em] uppercase"
            style={{ color: CORAL }}
          >
            event-driven .NET
          </span>
        </div>

        {/* ===================  COVER SPREAD  =================== */}
        <section className="grid gap-10 lg:grid-cols-[1fr_2fr] lg:gap-12">
          {/* narrow-left: issue header */}
          <aside className="border-cc-card-border flex flex-col gap-6 lg:border-r lg:pr-10">
            <MarginKicker>Issue 04 / Workflows</MarginKicker>
            <PullQuote>work that survives the response</PullQuote>
            <DottedDivider />
            <div className="flex flex-col gap-3">
              <MarginNote marker="*">
                feature story. p. 03. mocha, the source-generated mediator and
                cross-service message bus for .NET.
              </MarginNote>
              <MarginNote marker="†">
                see also: validated sagas, outbox + inbox reliability, pluggable
                transports.
              </MarginNote>
              <MarginNote marker="‡">
                cover visual on the facing page: live topology with one message
                in flight.
              </MarginNote>
            </div>
            <CoralTick />
          </aside>

          {/* wide-right: title + lead + hero card */}
          <div className="flex flex-col gap-8">
            <div>
              <h1 className="font-heading text-h1 text-cc-heading">
                Let work continue,
                <br />
                after the request.
              </h1>
              <p className="lead text-cc-ink-dim mt-6 max-w-2xl">
                Return the response the instant the user needs it. Hand the
                slow, fan-out, cross-service work to a message and let it keep
                moving on its own.
              </p>
              <p className="text-cc-prose mt-5 max-w-2xl">
                Mocha is one source-generated framework for both the in-process
                command you dispatch and the event you publish across services.
                Same handler-first model, same traces, whichever way the work
                travels.
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
                    <span style={{ color: CORAL }}>
                      <CheckIcon />
                    </span>
                    {t}
                  </li>
                ))}
              </ul>
            </div>
            <HeroTopologyCard />
          </div>
        </section>

        {/* ===================  01 · TWO LANES  =================== */}
        <section className="grid gap-10 lg:grid-cols-[2fr_1fr] lg:gap-12">
          {/* wide-left: title + lanes */}
          <div className="border-cc-card-border lg:border-r lg:pr-10">
            <div className="flex items-baseline gap-4">
              <span
                className="font-mono text-[11px] tracking-[0.22em] uppercase"
                style={{ color: CORAL }}
              >
                Section 01
              </span>
              <span className="text-cc-nav-label font-mono text-[11px]">
                two ways to dispatch · one model
              </span>
            </div>
            <h2 className="font-heading text-h2 text-cc-heading mt-3 max-w-3xl">
              <span
                className="font-heading mr-1 inline-block align-bottom text-[64px] leading-none"
                style={{ color: CORAL }}
              >
                I
              </span>
              n-process when near. On the bus when far.
            </h2>
            <p className="text-cc-prose mt-5 max-w-2xl">
              Inside one process, the mediator dispatches commands and queries
              straight to a{" "}
              <span className="text-cc-ink font-mono">[Handler]</span> through a
              pre-compiled pipeline. When the work belongs to another service,
              the same publish crosses a transport and fans out to its
              consumers. You change the verb, not the mental model.
            </p>
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
                steps={[
                  "ReviewCreated",
                  "PublishAsync",
                  "transport",
                  "consumers",
                ]}
                note="One event reaches every interested service through a pluggable transport, with outbox and inbox guaranteeing each consumer processes it exactly once."
              />
            </div>
          </div>

          {/* narrow-right: marginalia */}
          <aside className="flex flex-col gap-6 lg:pl-2">
            <SectionNumeral n="01" />
            <DottedDivider />
            <div className="flex flex-col gap-2">
              <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
                diagram legend
              </p>
              <div className="flex items-center gap-2">
                <span
                  className="size-2 rounded-full"
                  style={{ backgroundColor: "var(--color-cc-accent)" }}
                />
                <span className="text-cc-ink-dim font-mono text-[11px]">
                  in-process hop, solid
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span
                  className="size-2 rounded-full"
                  style={{ backgroundColor: CORAL }}
                />
                <span className="text-cc-ink-dim font-mono text-[11px]">
                  cross-service hop, dashed
                </span>
              </div>
            </div>
            <DottedDivider />
            <MarginNote marker="¹">
              dispatch is source-generated at build time; the pipeline contains
              no reflection on the hot path.
            </MarginNote>
            <MarginNote marker="²">
              the publish verb is identical in both lanes. only the registration
              changes.
            </MarginNote>
            <CoralTick />
          </aside>
        </section>

        {/* ===================  02 · SAGA COLUMN  =================== */}
        <section className="grid gap-10 lg:grid-cols-[1fr_2fr] lg:gap-12">
          {/* narrow-left: margin */}
          <aside className="border-cc-card-border flex flex-col gap-6 lg:border-r lg:pr-10">
            <SectionNumeral n="02" />
            <PullQuote>a workflow that cannot dead-end</PullQuote>
            <DottedDivider />
            <MarginNote marker="*">
              the state-machine check runs before the service handles traffic,
              not at compile time.
            </MarginNote>
            <MarginNote marker="†">
              every state must be reachable. every path must reach a terminal
              state.
            </MarginNote>
            <CoralTick />
          </aside>

          {/* wide-right: title + saga */}
          <div className="flex flex-col gap-6">
            <div className="flex items-baseline gap-4">
              <span
                className="font-mono text-[11px] tracking-[0.22em] uppercase"
                style={{ color: CORAL }}
              >
                Section 02
              </span>
              <span className="text-cc-nav-label font-mono text-[11px]">
                sagas · stateful workflows
              </span>
            </div>
            <h2 className="font-heading text-h2 text-cc-heading max-w-3xl">
              <span
                className="font-heading mr-1 inline-block align-bottom text-[64px] leading-none"
                style={{ color: CORAL }}
              >
                D
              </span>
              raft. Checked. Published.
            </h2>
            <p className="text-cc-prose max-w-2xl">
              A review moves{" "}
              <span className="text-cc-ink font-mono">
                Draft → Checked → Published
              </span>{" "}
              across several messages and minutes. Define that state machine
              once; Mocha checks that every state is reachable and every path
              reaches a final state, validated before the service handles
              traffic, so a saga that can dead-end never makes it into
              production.
            </p>

            <div className="border-cc-card-border bg-cc-card-bg mt-2 rounded-2xl border p-6 backdrop-blur-sm sm:p-8">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
                  ReviewSaga
                </span>
                <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px]">
                  <CheckIcon />
                  <span style={{ color: CORAL }}>
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
                  ["running hop", "in flight now", CORAL],
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
                    <span className="text-cc-ink font-mono text-[11px]">
                      {k}
                    </span>
                    <span className="text-cc-nav-label font-mono text-[11px]">
                      , {v}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>

        {/* ===================  03 · BEFORE / AFTER  =================== */}
        <section className="grid gap-10 lg:grid-cols-[2fr_1fr] lg:gap-12">
          {/* wide-left */}
          <div className="border-cc-card-border lg:border-r lg:pr-10">
            <div className="flex items-baseline gap-4">
              <span
                className="font-mono text-[11px] tracking-[0.22em] uppercase"
                style={{ color: CORAL }}
              >
                Section 03
              </span>
              <span className="text-cc-nav-label font-mono text-[11px]">
                before / after
              </span>
            </div>
            <h2 className="font-heading text-h2 text-cc-heading mt-3 max-w-3xl">
              <span
                className="font-heading mr-1 inline-block align-bottom text-[64px] leading-none"
                style={{ color: CORAL }}
              >
                C
              </span>
              ollapse the plumbing.
            </h2>
            <p className="text-cc-prose mt-5 max-w-2xl">
              Reliable messaging usually means hand-rolling a broker connection,
              retry and backoff, a dead-letter path, a dedup table, and an
              outbox wired into your transaction. Mocha owns all of that. You
              write the handler and the publish.
            </p>
            <div className="mt-8 grid gap-5">
              <TangleBefore />
              <TangleAfter />
            </div>
          </div>

          {/* narrow-right */}
          <aside className="flex flex-col gap-6 lg:pl-2">
            <SectionNumeral n="03" />
            <DottedDivider />
            <div className="flex flex-col gap-1">
              <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
                line count
              </p>
              <p
                className="font-heading text-[48px] leading-none"
                style={{ color: CORAL }}
              >
                184
                <span className="text-cc-nav-label mx-2 font-mono text-[18px]">
                  /
                </span>
                <span className="text-cc-heading">11</span>
              </p>
              <p className="text-cc-ink-dim font-mono text-[11px]">
                hand-rolled / with mocha
              </p>
            </div>
            <DottedDivider />
            <MarginNote marker="*">
              mocha owns the broker connection, retry and backoff, dead-letter
              routing, dedup, and the outbox wired into your db write.
            </MarginNote>
            <span
              aria-hidden
              className="text-cc-ink-dim font-mono text-2xl"
              style={{ color: CORAL }}
            >
              ↓
            </span>
            <CoralTick />
          </aside>
        </section>

        {/* ===================  04 · TRANSPORTS  =================== */}
        <section className="grid gap-10 lg:grid-cols-[1fr_2fr] lg:gap-12">
          {/* narrow-left */}
          <aside className="border-cc-card-border flex flex-col gap-6 lg:border-r lg:pr-10">
            <SectionNumeral n="04" />
            <MarginKicker>Pluggable</MarginKicker>
            <DottedDivider />
            <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
              guarantees
            </p>
            <ul className="flex flex-col gap-2">
              {[
                "transactional outbox",
                "idempotent inbox",
                "dead-letter routing",
                "retry + redelivery",
                "delayed delivery",
              ].map((g) => (
                <li
                  key={g}
                  className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px]"
                >
                  <span style={{ color: CORAL }}>
                    <CheckIcon size={12} />
                  </span>
                  {g}
                </li>
              ))}
            </ul>
            <CoralTick />
          </aside>

          {/* wide-right */}
          <div className="flex flex-col gap-6">
            <div className="flex items-baseline gap-4">
              <span
                className="font-mono text-[11px] tracking-[0.22em] uppercase"
                style={{ color: CORAL }}
              >
                Section 04
              </span>
              <span className="text-cc-nav-label font-mono text-[11px]">
                pluggable transports
              </span>
            </div>
            <h2 className="font-heading text-h2 text-cc-heading max-w-3xl">
              <span
                className="font-heading mr-1 inline-block align-bottom text-[64px] leading-none"
                style={{ color: CORAL }}
              >
                S
              </span>
              wap the broker, keep the handlers.
            </h2>
            <p className="text-cc-prose max-w-2xl">
              The transport is a registration detail, not a rewrite. Start
              in-process, move to Postgres or RabbitMQ in production, route
              high-throughput streams through Kafka, and run several at once.
              Your handlers never know the difference.
            </p>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              <TransportChip name="RabbitMQ" tag="in use" highlight />
              <TransportChip name="Postgres" tag="durable" />
              <TransportChip name="Kafka" tag="streaming" />
              <TransportChip name="Azure Service Bus" tag="cloud" />
              <TransportChip name="In-process" tag="zero-infra" />
              <TransportChip name="Azure Event Hub" tag="ingest" />
            </div>
          </div>
        </section>

        {/* ===================  05 · OBSERVABILITY CENTERFOLD  =================== */}
        <section className="grid gap-10 lg:grid-cols-[2fr_1fr] lg:gap-12">
          {/* wide-left */}
          <div className="border-cc-card-border lg:border-r lg:pr-10">
            <div className="flex items-baseline gap-4">
              <span
                className="font-mono text-[11px] tracking-[0.22em] uppercase"
                style={{ color: CORAL }}
              >
                Section 05
              </span>
              <span className="text-cc-nav-label font-mono text-[11px]">
                observability · centerfold
              </span>
            </div>
            <h2 className="font-heading text-h2 text-cc-heading mt-3 max-w-3xl">
              <span
                className="font-heading mr-1 inline-block align-bottom text-[64px] leading-none"
                style={{ color: CORAL }}
              >
                F
              </span>
              ollow one message, hop by hop.
            </h2>
            <p className="text-cc-prose mt-5 max-w-2xl">
              Publish, dispatch, receive, and consume each emit a real
              OpenTelemetry span, with the correlation id propagating across
              every service boundary. The same trace opens in Nitro, so you can
              watch the in-flight message advance the way you reason about it.
            </p>
            <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
              <div className="border-cc-card-border mb-4 flex items-center justify-between border-b pb-3">
                <span className="text-cc-heading font-mono text-[11px]">
                  trace · review.created
                </span>
                <span className="text-cc-nav-label font-mono text-[10px]">
                  42.6ms · 5 spans
                </span>
              </div>
              <div className="space-y-2.5">
                <TraceSpanRow
                  label="POST /reviews"
                  widthPct={100}
                  offsetPct={0}
                />
                <TraceSpanRow label="CreateReview" widthPct={9} offsetPct={2} />
                <TraceSpanRow
                  label="outbox.commit"
                  widthPct={14}
                  offsetPct={11}
                />
                <TraceSpanRow
                  label="publish→rabbitmq"
                  widthPct={34}
                  offsetPct={26}
                  live
                />
                <TraceSpanRow
                  label="SearchIndexer"
                  widthPct={32}
                  offsetPct={62}
                />
              </div>
              <div className="border-cc-card-border mt-4 flex items-center gap-2 border-t pt-3">
                <span
                  className="inline-block h-2.5 w-6 rounded"
                  style={{
                    background: `linear-gradient(90deg, ${SPECTRUM_CYAN}, ${SPECTRUM_VIOLET}, ${CORAL})`,
                  }}
                />
                <span className="text-cc-nav-label font-mono text-[10px]">
                  running message, the hop in flight right now
                </span>
              </div>
            </div>
          </div>

          {/* narrow-right */}
          <aside className="flex flex-col gap-6 lg:pl-2">
            <SectionNumeral n="05" />
            <MarginKicker>Every hop a span</MarginKicker>
            <DottedDivider />
            <ul className="flex flex-col gap-2">
              {[
                "Correlation id carried across services",
                "Spans for dispatch, transport, and handler",
                "Zero overhead when the observer is off",
              ].map((t) => (
                <li
                  key={t}
                  className="text-cc-ink-dim flex items-start gap-2 font-mono text-[11px] leading-relaxed"
                >
                  <span style={{ color: CORAL }} className="mt-0.5 shrink-0">
                    <CheckIcon size={12} />
                  </span>
                  {t}
                </li>
              ))}
            </ul>
            <DottedDivider />
            <MarginNote marker="*">
              telemetry surfaces in Nitro once configured. the centerfold spans
              are real OTel data.
            </MarginNote>
            <CoralTick />
          </aside>
        </section>

        {/* ===================  06 · COLOPHON  =================== */}
        <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
          <div className="mb-6 flex items-center gap-4">
            <span
              className="font-mono text-[11px] tracking-[0.22em] uppercase"
              style={{ color: CORAL }}
            >
              Section 06
            </span>
            <span className="text-cc-nav-label font-mono text-[11px]">
              colophon · what we mean precisely
            </span>
          </div>
          <h2 className="font-heading text-h3 text-cc-heading max-w-3xl">
            Reliability claims, stated honestly.
          </h2>
          <div className="mt-8 grid gap-0 sm:grid-cols-3">
            {[
              {
                h: "Exactly-once processing",
                p: "The outbox commits the message with your database write, and the inbox deduplicates on receive. That gives exactly-once processing, not exactly-once delivery, which no transport can promise.",
                marker: "I",
              },
              {
                h: "Sagas validated before traffic",
                p: "The state-machine check runs before the service handles traffic, not at compile time. It proves your saga can always reach a final state.",
                marker: "II",
              },
              {
                h: "Published clients affected",
                p: "Because dispatch is source-generated and contracts are explicit, a changed message shows up at build time, so you can see which published clients are affected.",
                marker: "III",
              },
            ].map((c, i) => (
              <div
                key={c.h}
                className={
                  i > 0
                    ? "sm:border-cc-card-border pt-6 sm:border-l sm:pt-0 sm:pl-6"
                    : "sm:pr-6"
                }
              >
                <p
                  className="font-mono text-[10px] tracking-[0.22em] uppercase"
                  style={{ color: CORAL }}
                >
                  {c.marker}
                </p>
                <h3 className="font-heading text-h6 text-cc-heading mt-2 mb-2">
                  {c.h}
                </h3>
                <p className="text-cc-ink-dim text-sm">{c.p}</p>
              </div>
            ))}
          </div>
        </section>

        {/* ===================  07 · CLOSING MASTHEAD  =================== */}
        <section className="flex flex-col items-center gap-6 text-center">
          <CoralTick />
          <span
            className="font-mono text-[11px] tracking-[0.22em] uppercase"
            style={{ color: CORAL }}
          >
            Section 07 · sign-off
          </span>
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
          <div className="border-cc-card-border mt-6 w-full max-w-2xl border-t pt-4">
            <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.22em] uppercase">
              The Workflow Quarterly · Issue 04 · Vol. Mocha · Q2 · printed in
              .NET
            </p>
          </div>
        </section>
      </div>
    </div>
  );
}
