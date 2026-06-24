"use client";

import { motion } from "framer-motion";
import { useEffect, useRef, useState } from "react";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/*  Stance: Message on the Rail. Single accent: coral #f0786a.         */
/*  The brand spectrum (cyan -> violet -> coral) appears once, on the  */
/*  closing hairline. The rail dramatizes the message's journey.       */
/* ------------------------------------------------------------------ */

const CORAL = "#f0786a";
const SPECTRUM_CYAN = "#16b9e4";
const SPECTRUM_VIOLET = "#7c92c6";

/* ============================  Milestones  ========================== */

interface Milestone {
  readonly id: string;
  readonly num: string;
  readonly eyebrow: string;
  readonly label: string;
}

const MILESTONES: readonly Milestone[] = [
  { id: "origin", num: "01", eyebrow: "dispatch", label: "Origin" },
  { id: "mediator", num: "02", eyebrow: "mediator", label: "In-process" },
  { id: "publish", num: "03", eyebrow: "publish", label: "Cross-service" },
  {
    id: "reliability",
    num: "04",
    eyebrow: "outbox / inbox",
    label: "Reliability",
  },
  { id: "saga", num: "05", eyebrow: "saga", label: "Validated workflow" },
  {
    id: "transports",
    num: "06",
    eyebrow: "transports",
    label: "Pluggable broker",
  },
  { id: "trace", num: "07", eyebrow: "trace", label: "Observability" },
];

/* ============================  Mono helpers  ======================== */

interface MonoEyebrowProps {
  readonly num: string;
  readonly text: string;
}

function MonoEyebrow({ num, text }: MonoEyebrowProps) {
  return (
    <p
      className="font-mono text-[11px] tracking-[0.22em] uppercase"
      style={{ color: CORAL }}
    >
      <span className="text-cc-nav-label mr-1.5">{num}</span> / {text}
    </p>
  );
}

interface InlineHopHeaderProps {
  readonly num: string;
  readonly eyebrow: string;
}

/* Below-lg inline stub: dot, line, eyebrow. Visible only on small. */
function InlineHopHeader({ num, eyebrow }: InlineHopHeaderProps) {
  return (
    <div className="mb-5 flex items-center gap-3 lg:hidden">
      <span
        className="size-2.5 shrink-0 rounded-full"
        style={{
          backgroundColor: CORAL,
          boxShadow: "0 0 0 4px color-mix(in srgb, #f0786a 22%, transparent)",
        }}
      />
      <span
        className="h-px flex-1"
        style={{
          backgroundColor: "color-mix(in srgb, #f0786a 45%, transparent)",
        }}
      />
      <span
        className="font-mono text-[11px] tracking-[0.22em] uppercase"
        style={{ color: CORAL }}
      >
        <span className="text-cc-nav-label mr-1.5">{num}</span> / {eyebrow}
      </span>
    </div>
  );
}

/* ============================  Rail (lg+)  ========================== */

interface RailProps {
  readonly activeIndex: number;
}

function Rail({ activeIndex }: RailProps) {
  return (
    <aside className="sticky top-24 hidden h-fit lg:block">
      <div className="relative h-[520px] w-[220px]">
        {/* faint vertical wash behind rail */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-y-0 left-3 w-12 rounded-full"
          style={{
            background:
              "linear-gradient(180deg, transparent 0%, color-mix(in srgb, #f0786a 6%, transparent) 50%, transparent 100%)",
          }}
        />
        {/* full-height hairline */}
        <span
          aria-hidden
          className="bg-cc-card-border absolute top-0 bottom-0 left-[18px] w-px"
        />
        {/* coral fill between origin and active */}
        <span
          aria-hidden
          className="absolute top-0 left-[18px] w-px transition-[height] duration-500 ease-out"
          style={{
            height:
              activeIndex <= 0
                ? "0%"
                : `${(activeIndex / (MILESTONES.length - 1)) * 100}%`,
            background:
              "linear-gradient(180deg, color-mix(in srgb, #f0786a 80%, transparent), color-mix(in srgb, #f0786a 35%, transparent))",
          }}
        />

        <ol className="relative flex h-full flex-col justify-between">
          {MILESTONES.map((m, i) => {
            const isActive = i === activeIndex;
            const isVisited = i < activeIndex;
            return (
              <li key={m.id} className="group flex items-start gap-3">
                <a
                  href={`#${m.id}`}
                  className="flex items-start gap-3"
                  aria-label={`${m.num} ${m.label}`}
                >
                  <span className="relative mt-0.5 flex size-[18px] shrink-0 items-center justify-center">
                    {isActive && (
                      <motion.span
                        aria-hidden
                        className="absolute inset-0 rounded-full"
                        style={{
                          backgroundColor:
                            "color-mix(in srgb, #f0786a 25%, transparent)",
                        }}
                        animate={{
                          scale: [1, 1.6, 1],
                          opacity: [0.7, 0, 0.7],
                        }}
                        transition={{
                          duration: 2.2,
                          repeat: Infinity,
                          ease: "easeInOut",
                        }}
                      />
                    )}
                    <span
                      className="relative size-2.5 rounded-full transition-transform duration-200 group-hover:scale-125"
                      style={{
                        backgroundColor: isActive
                          ? CORAL
                          : isVisited
                            ? "color-mix(in srgb, #f0786a 55%, transparent)"
                            : "rgba(245,241,234,0.22)",
                        boxShadow: isActive
                          ? "0 0 0 4px color-mix(in srgb, #f0786a 22%, transparent)"
                          : "none",
                      }}
                    />
                  </span>
                  <span className="min-w-0">
                    <span
                      className="block font-mono text-[10px] tracking-[0.22em] uppercase transition-colors"
                      style={{
                        color: isActive ? CORAL : "var(--color-cc-nav-label)",
                      }}
                    >
                      {m.num} / {m.eyebrow}
                    </span>
                    <span
                      className="text-cc-ink group-hover:text-cc-heading block font-mono text-[12px] transition-colors"
                      style={{
                        color: isActive ? "var(--color-cc-heading)" : undefined,
                      }}
                    >
                      {m.label}
                    </span>
                  </span>
                </a>
              </li>
            );
          })}
        </ol>
      </div>
    </aside>
  );
}

/* ============================  Milestone card  ===================== */

interface MilestoneCardProps {
  readonly id: string;
  readonly num: string;
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly alt?: boolean;
  readonly children: ReactNode;
}

function MilestoneCard({
  id,
  num,
  eyebrow,
  title,
  alt,
  children,
}: MilestoneCardProps) {
  return (
    <section
      id={id}
      className={`relative scroll-mt-28 rounded-2xl border p-6 transition-colors sm:p-8 ${
        alt ? "bg-cc-card-bg" : "bg-cc-surface"
      } border-cc-card-border hover:border-cc-card-border-hover`}
    >
      {/* clamp dot on the left edge at lg+; placed so it sits on the rail */}
      <motion.span
        aria-hidden
        initial={{ scale: 0.4, opacity: 0 }}
        whileInView={{ scale: 1, opacity: 1 }}
        viewport={{ once: true, margin: "-15% 0px -15% 0px" }}
        transition={{ duration: 0.5, ease: "easeOut" }}
        className="absolute top-8 -left-[10px] hidden size-[18px] rounded-full lg:block"
        style={{
          backgroundColor: CORAL,
          boxShadow:
            "0 0 0 4px var(--color-cc-bg), 0 0 18px color-mix(in srgb, #f0786a 55%, transparent)",
        }}
      />
      <InlineHopHeader num={num} eyebrow={eyebrow} />
      <div className="hidden lg:block">
        <MonoEyebrow num={num} text={eyebrow} />
      </div>
      <h2 className="font-heading text-h3 text-cc-heading mt-3">{title}</h2>
      <div className="mt-5">{children}</div>
    </section>
  );
}

/* ============================  Diagrams  =========================== */

interface HopNodeProps {
  readonly label: string;
  readonly sub?: string;
  readonly state: "done" | "live" | "pending";
}

function HopNode({ label, sub, state }: HopNodeProps) {
  const dot =
    state === "live"
      ? CORAL
      : state === "done"
        ? "var(--color-cc-accent)"
        : "rgba(245,241,234,0.28)";
  const border =
    state === "live"
      ? "color-mix(in srgb, #f0786a 50%, transparent)"
      : "var(--color-cc-card-border)";
  const bg =
    state === "live"
      ? "color-mix(in srgb, #f0786a 8%, transparent)"
      : "color-mix(in srgb, var(--color-cc-surface) 80%, transparent)";
  return (
    <div
      className="flex min-w-0 items-center gap-2.5 rounded-lg border px-3 py-2"
      style={{ borderColor: border, backgroundColor: bg }}
    >
      <span
        className="size-2 shrink-0 rounded-full"
        style={{
          backgroundColor: dot,
          boxShadow:
            state === "live"
              ? "0 0 0 4px color-mix(in srgb, #f0786a 22%, transparent)"
              : "none",
        }}
      />
      <span className="min-w-0">
        <span className="text-cc-heading block truncate font-mono text-[12px] leading-tight">
          {label}
        </span>
        {sub && (
          <span className="text-cc-nav-label block truncate font-mono text-[10px] leading-tight">
            {sub}
          </span>
        )}
      </span>
    </div>
  );
}

interface ArrowProps {
  readonly label: string;
  readonly live?: boolean;
}

function Arrow({ label, live }: ArrowProps) {
  return (
    <div className="flex flex-col items-center gap-1 px-1">
      <svg
        viewBox="0 0 120 12"
        className="h-3 w-full min-w-[60px]"
        preserveAspectRatio="none"
        aria-hidden
      >
        <line
          x1="0"
          y1="6"
          x2="114"
          y2="6"
          stroke={live ? CORAL : "rgba(245,241,234,0.25)"}
          strokeWidth="1.5"
          strokeDasharray={live ? "5 4" : "0"}
          className={live ? "w-flow" : ""}
        />
        <polygon
          points="114,2 120,6 114,10"
          fill={live ? CORAL : "rgba(245,241,234,0.4)"}
        />
      </svg>
      <span className="text-cc-nav-label font-mono text-[10px] whitespace-nowrap">
        {label}
      </span>
    </div>
  );
}

/* ----- Origin diagram (hero) ---------------------------------------- */

function OriginDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border">
      <div className="border-cc-card-border bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <div className="flex items-center gap-2">
          <span className="size-2.5 rounded-full bg-[#f0786a]/60" />
          <span className="bg-cc-accent/60 size-2.5 rounded-full" />
          <span className="size-2.5 rounded-full bg-[#7c92c6]/60" />
        </div>
        <span className="text-cc-nav-label font-mono text-[11px]">
          mocha . the rail . live
        </span>
        <span className="text-cc-ink-dim flex items-center gap-1.5 font-mono text-[11px]">
          <span
            className="size-1.5 rounded-full"
            style={{ backgroundColor: CORAL }}
          />
          1 in flight
        </span>
      </div>
      <div className="p-5 sm:p-6">
        <div className="mb-5 flex flex-wrap items-center gap-3">
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
            handler returns 201 . work keeps moving
          </span>
        </div>

        {/* vertical rail visualization that mirrors the page rail */}
        <div className="relative">
          <span
            aria-hidden
            className="absolute top-1 bottom-1 left-[9px] w-px"
            style={{
              background:
                "linear-gradient(180deg, color-mix(in srgb, #f0786a 70%, transparent), color-mix(in srgb, #f0786a 20%, transparent))",
            }}
          />
          <ul className="space-y-3 pl-7">
            {[
              { l: "dispatch", t: "ISender . [Handler]", live: false },
              { l: "publish", t: "PublishAsync", live: true },
              { l: "outbox", t: "committed with DB tx", live: false },
              { l: "inbox", t: "dedup on receive", live: false },
            ].map((row, i) => (
              <li key={row.l} className="relative flex items-center gap-3">
                <span
                  className="absolute top-1/2 -left-7 size-3 -translate-y-1/2 rounded-full"
                  style={{
                    backgroundColor: row.live
                      ? CORAL
                      : "color-mix(in srgb, #f0786a 35%, transparent)",
                    boxShadow: row.live
                      ? "0 0 0 4px color-mix(in srgb, #f0786a 22%, transparent)"
                      : "none",
                  }}
                />
                <span className="text-cc-heading font-mono text-[12px]">
                  {String(i + 1).padStart(2, "0")} . {row.l}
                </span>
                <span className="text-cc-nav-label font-mono text-[11px]">
                  {row.t}
                </span>
              </li>
            ))}
          </ul>
        </div>

        <div className="border-cc-card-border mt-5 flex flex-wrap items-center gap-2 border-t pt-3">
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

/* ----- Mediator diagram --------------------------------------------- */

function MediatorDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-surface/40 rounded-xl border p-4">
      <div className="flex flex-wrap items-center gap-2 sm:flex-nowrap">
        <HopNode label="CreateReview" sub="ICommand" state="done" />
        <Arrow label="dispatch" />
        <HopNode label="ISender" sub="pipeline" state="done" />
        <Arrow label="invoke" live />
        <HopNode label="ReviewHandler" sub="[Handler]" state="live" />
        <Arrow label="result" />
        <HopNode label="Result" sub="returned" state="pending" />
      </div>
      <p className="text-cc-ink-dim mt-4 text-sm">
        Inside one process, commands and queries resolve through a
        source-generated pipeline. No reflection, no service-locator lookup on
        the hot path.
      </p>
    </div>
  );
}

/* ----- Publish diagram ---------------------------------------------- */

function PublishDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-surface/40 rounded-xl border p-4">
      <div className="flex flex-wrap items-center gap-2 sm:flex-nowrap">
        <HopNode label="ReviewCreated" sub="event" state="done" />
        <Arrow label="PublishAsync" />
        <HopNode label="transport" sub="rabbitmq" state="live" />
        <Arrow label="fan-out" live />
        <HopNode label="consumers" sub="3 subscribers" state="pending" />
      </div>
      <div className="mt-4 grid grid-cols-1 gap-2 sm:grid-cols-3">
        {[
          ["NotifyAuthor", "queued"],
          ["UpdateScore", "queued"],
          ["WarmCache", "queued"],
        ].map(([n, s]) => (
          <div
            key={n}
            className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between rounded-md border border-dashed px-2.5 py-1.5"
          >
            <span className="text-cc-ink font-mono text-[11px]">{n}</span>
            <span className="text-cc-nav-label font-mono text-[10px]">{s}</span>
          </div>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-4 text-sm">
        Same handler-first model, different verb. One event reaches every
        interested service through the configured transport.
      </p>
    </div>
  );
}

/* ----- Reliability pair --------------------------------------------- */

function ReliabilityPair() {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <div className="border-cc-card-border bg-cc-surface/40 rounded-xl border p-4">
        <p className="text-cc-nav-label mb-2 font-mono text-[11px] tracking-[0.18em] uppercase">
          on send . outbox
        </p>
        <div className="space-y-2">
          {[
            "BEGIN TRANSACTION",
            "INSERT review",
            "INSERT outbox.message",
            "COMMIT",
            "dispatcher publishes . once",
          ].map((l, i) => (
            <div
              key={l}
              className="font-mono text-[11.5px] leading-tight"
              style={{
                color:
                  i === 4
                    ? CORAL
                    : "color-mix(in srgb, var(--color-cc-ink) 90%, transparent)",
              }}
            >
              <span className="text-cc-nav-label mr-3 select-none">
                {String(i + 1).padStart(2, "0")}
              </span>
              {l}
            </div>
          ))}
        </div>
      </div>
      <div className="border-cc-card-border bg-cc-surface/40 rounded-xl border p-4">
        <p className="text-cc-nav-label mb-2 font-mono text-[11px] tracking-[0.18em] uppercase">
          on receive . inbox
        </p>
        <div className="space-y-2">
          {[
            "message arrives",
            "lookup inbox by message-id",
            "seen before . ack and skip",
            "fresh . run the handler",
            "INSERT inbox.message",
          ].map((l, i) => (
            <div
              key={l}
              className="font-mono text-[11.5px] leading-tight"
              style={{
                color:
                  i === 2 || i === 3
                    ? CORAL
                    : "color-mix(in srgb, var(--color-cc-ink) 90%, transparent)",
              }}
            >
              <span className="text-cc-nav-label mr-3 select-none">
                {String(i + 1).padStart(2, "0")}
              </span>
              {l}
            </div>
          ))}
        </div>
      </div>
      <div className="lg:col-span-2">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className="rounded-full border px-3 py-1 font-mono text-[11px]"
            style={{
              borderColor: "color-mix(in srgb, #f0786a 45%, transparent)",
              color: CORAL,
            }}
          >
            exactly-once processing
          </span>
          <span className="text-cc-ink-dim text-sm">
            outbox commits with the DB write, inbox deduplicates on receive.
            Delivery itself is not exactly-once, no transport can promise that.
          </span>
        </div>
      </div>
    </div>
  );
}

/* ----- Saga strip --------------------------------------------------- */

interface SagaChipProps {
  readonly label: string;
  readonly state: "done" | "live" | "pending";
  readonly first?: boolean;
}

function SagaChip({ label, state, first }: SagaChipProps) {
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
          className="size-2 rounded-full"
          style={{ backgroundColor: fill }}
        />
        <span className="text-cc-heading font-mono text-[12px]">{label}</span>
      </div>
    </div>
  );
}

function SagaStrip() {
  return (
    <div className="border-cc-card-border bg-cc-surface/40 rounded-xl border p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
          ReviewSaga
        </span>
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px]">
          <span style={{ color: CORAL }}>
            <CheckIcon />
          </span>
          <span style={{ color: CORAL }}>validated . all paths terminal</span>
        </span>
      </div>
      <div className="mt-5 flex flex-wrap items-center gap-y-3">
        <SagaChip label="Draft" state="done" first />
        <SagaChip label="Checked" state="live" />
        <SagaChip label="Published" state="pending" />
      </div>
      <p className="text-cc-ink-dim mt-5 text-sm">
        Define the state machine once. Mocha checks that every state is
        reachable and every path reaches a final state, validated before the
        service handles traffic.
      </p>
    </div>
  );
}

/* ----- Transports --------------------------------------------------- */

interface TransportChipProps {
  readonly name: string;
  readonly tag: string;
  readonly highlight?: boolean;
}

function TransportChip({ name, tag, highlight }: TransportChipProps) {
  return (
    <div
      className="bg-cc-surface/60 flex items-center justify-between rounded-lg border px-3.5 py-2.5 transition-colors"
      style={{
        borderColor: highlight
          ? "color-mix(in srgb, #f0786a 45%, transparent)"
          : "var(--color-cc-card-border)",
      }}
    >
      <span className="text-cc-heading font-mono text-[13px]">{name}</span>
      <span
        className="font-mono text-[10px] tracking-wide uppercase"
        style={{
          color: highlight ? CORAL : "var(--color-cc-nav-label)",
        }}
      >
        {tag}
      </span>
    </div>
  );
}

function Transports() {
  return (
    <div>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <TransportChip name="RabbitMQ" tag="broker" highlight />
        <TransportChip name="Postgres" tag="durable" />
        <TransportChip name="Kafka" tag="streaming" />
        <TransportChip name="Azure Service Bus" tag="cloud" />
        <TransportChip name="In-process" tag="zero-infra" />
        <TransportChip name="Azure Event Hub" tag="ingest" />
      </div>
      <div className="mt-4 flex flex-wrap gap-2">
        {["dead-letter routing", "retry + redelivery", "delayed delivery"].map(
          (b) => (
            <span
              key={b}
              className="border-cc-card-border bg-cc-surface/50 text-cc-ink-dim flex items-center gap-1.5 rounded-full border px-3 py-1.5 font-mono text-[11px]"
            >
              <span style={{ color: CORAL }}>
                <CheckIcon size={12} />
              </span>
              {b}
            </span>
          ),
        )}
      </div>
      <p className="text-cc-ink-dim mt-5 text-sm">
        The transport is a registration detail, not a rewrite. Start in-process,
        move to RabbitMQ or Postgres in production, route high-throughput
        streams through Kafka, run several at once.
      </p>
    </div>
  );
}

/* ----- Trace ribbon -------------------------------------------------- */

interface TraceSpanRowProps {
  readonly label: string;
  readonly widthPct: number;
  readonly offsetPct: number;
  readonly live?: boolean;
}

function TraceSpanRow({ label, widthPct, offsetPct, live }: TraceSpanRowProps) {
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
            background: live ? CORAL : "rgba(94,234,212,0.35)",
            boxShadow: live
              ? "0 0 12px color-mix(in srgb, #f0786a 40%, transparent)"
              : "none",
          }}
        />
      </div>
    </div>
  );
}

function TraceRibbon() {
  return (
    <div className="border-cc-card-border bg-cc-surface/40 rounded-xl border p-5">
      <div className="border-cc-card-border mb-4 flex items-center justify-between border-b pb-3">
        <span className="text-cc-heading font-mono text-[11px]">
          trace . review.created
        </span>
        <span className="text-cc-nav-label font-mono text-[10px]">
          42.6ms . 5 spans
        </span>
      </div>
      <div className="space-y-2.5">
        <TraceSpanRow label="POST /reviews" widthPct={100} offsetPct={0} />
        <TraceSpanRow label="CreateReview" widthPct={9} offsetPct={2} />
        <TraceSpanRow label="outbox.commit" widthPct={14} offsetPct={11} />
        <TraceSpanRow
          label="publish.rabbitmq"
          widthPct={34}
          offsetPct={26}
          live
        />
        <TraceSpanRow label="SearchIndexer" widthPct={32} offsetPct={62} />
      </div>
      <div className="border-cc-card-border mt-4 flex items-center gap-2 border-t pt-3">
        <span
          className="inline-block h-2.5 w-6 rounded"
          style={{ backgroundColor: CORAL }}
        />
        <span className="text-cc-nav-label font-mono text-[10px]">
          running hop . the span in flight right now
        </span>
      </div>
      <p className="text-cc-ink-dim mt-4 text-sm">
        Publish, dispatch, receive, and consume each emit a real OpenTelemetry
        span, with the correlation id propagating across every service boundary.
        The same trace opens in Nitro once telemetry is configured for the
        service.
      </p>
    </div>
  );
}

/* ============================  Active milestone hook  =============== */

function useActiveMilestone(ids: readonly string[]): number {
  const [active, setActive] = useState(0);
  const ref = useRef(0);

  useEffect(() => {
    if (typeof window === "undefined") return;
    if (!("IntersectionObserver" in window)) return;

    const nodes = ids
      .map((id) => document.getElementById(id))
      .filter((n): n is HTMLElement => n !== null);

    const observer = new IntersectionObserver(
      (entries) => {
        // pick the entry whose top is closest to the viewport's upper third
        const visible = entries
          .filter((e) => e.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
        if (visible.length > 0) {
          const top = visible[0].target.id;
          const idx = ids.indexOf(top);
          if (idx !== -1 && idx !== ref.current) {
            ref.current = idx;
            setActive(idx);
          }
        }
      },
      {
        rootMargin: "-30% 0px -55% 0px",
        threshold: [0, 0.25, 0.5],
      },
    );

    nodes.forEach((n) => observer.observe(n));
    return () => observer.disconnect();
  }, [ids]);

  return active;
}

/* ==============================  Page  ============================== */

export function ClientPage() {
  const ids = MILESTONES.map((m) => m.id);
  const activeIndex = useActiveMilestone(ids);

  return (
    <div className="flex flex-col gap-24 py-6">
      {/* time-driven dashed flow + reduced-motion guard */}
      <style>{`
        @keyframes w-dashflow { to { stroke-dashoffset: -18; } }
        .w-flow { animation: w-dashflow 0.9s linear infinite; }
        @media (prefers-reduced-motion: reduce) {
          .w-flow { animation: none; }
        }
      `}</style>

      {/* ---------------------------- HERO ---------------------------- */}
      <section id="origin" className="relative scroll-mt-28">
        {/* soft coral radial wash, single layer */}
        <div
          aria-hidden
          className="pointer-events-none absolute -top-12 left-1/2 -z-10 h-[420px] w-[820px] max-w-[120%] -translate-x-1/2"
          style={
            {
              background:
                "radial-gradient(closest-side, color-mix(in srgb, #f0786a 8%, transparent), transparent 70%)",
            } as CSSProperties
          }
        />
        <div className="grid items-center gap-12 lg:grid-cols-[1fr_1.05fr]">
          <div>
            <p className="border-cc-card-border bg-cc-card-bg text-cc-ink-dim mb-5 inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[11px]">
              <span
                className="size-1.5 rounded-full"
                style={{ backgroundColor: CORAL }}
              />
              mocha . message on the rail
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
          <OriginDiagram />
        </div>
      </section>

      {/* -------------------- RAIL + MILESTONES ---------------------- */}
      <div className="grid gap-10 lg:grid-cols-[220px_minmax(0,1fr)]">
        <Rail activeIndex={activeIndex} />

        <div className="flex flex-col gap-10">
          {/* 02 mediator */}
          <MilestoneCard
            id="mediator"
            num="02"
            eyebrow="dispatch"
            title={<>In-process, no hops.</>}
          >
            <p className="text-cc-prose mb-5 max-w-2xl">
              Inside one process, the mediator dispatches commands and queries
              straight to a{" "}
              <span className="text-cc-ink font-mono">[Handler]</span> through a
              pre-compiled pipeline. You change the verb, not the mental model.
            </p>
            <MediatorDiagram />
          </MilestoneCard>

          {/* 03 publish */}
          <MilestoneCard
            id="publish"
            num="03"
            eyebrow="publish"
            title={<>Cross-service, fan out.</>}
            alt
          >
            <p className="text-cc-prose mb-5 max-w-2xl">
              When the work belongs to another service, the same publish crosses
              a transport and fans out to its consumers. Same handler-first
              model on the other side.
            </p>
            <PublishDiagram />
          </MilestoneCard>

          {/* 04 outbox + inbox */}
          <MilestoneCard
            id="reliability"
            num="04"
            eyebrow="outbox / inbox"
            title={<>The message stays committed.</>}
          >
            <p className="text-cc-prose mb-5 max-w-2xl">
              On the way out, the outbox commits the message inside the same
              database transaction as your write. On the way in, the inbox
              deduplicates by message id. Together they give exactly-once
              processing.
            </p>
            <ReliabilityPair />
          </MilestoneCard>

          {/* 05 saga */}
          <MilestoneCard
            id="saga"
            num="05"
            eyebrow="saga"
            title={<>A workflow that can&apos;t get stuck.</>}
            alt
          >
            <p className="text-cc-prose mb-5 max-w-2xl">
              A review moves{" "}
              <span className="text-cc-ink font-mono">
                Draft . Checked . Published
              </span>{" "}
              across several messages and minutes. The state-machine check runs
              before the service handles traffic, so a saga that can dead-end
              never makes it into production.
            </p>
            <SagaStrip />
          </MilestoneCard>

          {/* 06 transports */}
          <MilestoneCard
            id="transports"
            num="06"
            eyebrow="transports"
            title={<>Swap the broker, keep the handlers.</>}
          >
            <Transports />
          </MilestoneCard>

          {/* 07 trace */}
          <MilestoneCard
            id="trace"
            num="07"
            eyebrow="trace"
            title={<>Follow one message, hop by hop.</>}
            alt
          >
            <TraceRibbon />
          </MilestoneCard>
        </div>
      </div>

      {/* -------------------- HONESTY BEAT (off-rail) ---------------- */}
      <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
        <p
          className="mb-3 font-mono text-[11px] tracking-[0.2em] uppercase"
          style={{ color: CORAL }}
        >
          what we mean precisely
        </p>
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
              h: "Published clients affected at build time",
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
          className="mx-auto h-px w-56"
          style={{
            background: `linear-gradient(90deg, transparent, ${SPECTRUM_CYAN}, ${SPECTRUM_VIOLET}, ${CORAL}, transparent)`,
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
