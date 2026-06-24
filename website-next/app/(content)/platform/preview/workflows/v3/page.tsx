import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Workflows: .NET Mediator + Message Bus in One",
  description:
    "Let work continue after the request returns. Mocha is a source-generated in-process mediator and cross-service message bus with pluggable transports, sagas, and outbox/inbox reliability.",
  keywords: [
    "dotnet message bus",
    "dotnet mediator",
    "CQRS source generator",
    "transactional outbox",
    "idempotent inbox",
    "exactly-once processing",
    "saga state machine",
    "RabbitMQ Kafka Postgres transport",
    "event-driven architecture .NET",
    "Mocha messaging framework",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Workflows: Let Work Continue After the Request",
    description:
      "A source-generated in-process mediator and cross-service message bus in one. Pluggable transports, sagas validated before traffic, and outbox/inbox for exactly-once processing.",
  },
};

/* -------------------------------------------------------------------------- */
/*  Scene accent: amber (#f59e0b) warming toward coral (#f0786a).             */
/*  The accent tracks the single in-flight message across every visual:       */
/*  a dashed amber connector = pending, a solid hairline = done.              */
/* -------------------------------------------------------------------------- */

/** Section eyebrow in the mono nav-label voice. */
function Eyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <span className="font-mono text-[0.7rem] tracking-[0.22em] text-amber-400/90 uppercase">
      {children}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  HERO SWITCHBOARD                                                           */
/*  A left-to-right spine: a central bus/mediator rail with handler nodes      */
/*  branching right. One dashed amber connector marks the in-flight message;   */
/*  the rest are solid (done). Pure inline SVG, themed from tokens.            */
/* -------------------------------------------------------------------------- */

interface HandlerNode {
  readonly y: number;
  readonly label: string;
  readonly span: string;
  /** "live" = the single in-flight hop (dashed amber); others are done. */
  readonly state: "done" | "live" | "queued";
}

const SWITCHBOARD_NODES: readonly HandlerNode[] = [
  {
    y: 46,
    label: "[Handler] OnReviewCreated",
    span: "span: notify",
    state: "done",
  },
  {
    y: 104,
    label: "[Handler] IndexReview",
    span: "span: search.index",
    state: "live",
  },
  {
    y: 162,
    label: "[Handler] UpdateScore",
    span: "span: score.recalc",
    state: "queued",
  },
];

function HeroSwitchboard() {
  const spineX = 116;
  const nodeX = 300;

  return (
    <div className="border-cc-card-border bg-cc-card-bg/70 relative overflow-hidden rounded-[1.75rem] border p-5 shadow-[0_30px_80px_-40px_rgba(245,158,11,0.45)] backdrop-blur-md sm:p-7">
      {/* amber spotlight wash behind the diagram */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-24 -left-16 h-64 w-64 rounded-full bg-amber-500/20 blur-3xl"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -right-20 -bottom-24 h-64 w-64 rounded-full bg-[#f0786a]/15 blur-3xl"
      />

      <div className="relative flex items-center justify-between">
        <p className="font-mono text-[0.62rem] tracking-[0.18em] text-amber-300/80 uppercase">
          PublishAsync(new ReviewCreated())
        </p>
        <span className="rounded-full border border-amber-400/40 px-2.5 py-0.5 font-mono text-[0.55rem] tracking-[0.12em] text-amber-300 uppercase">
          1 in flight
        </span>
      </div>

      <svg
        viewBox="0 0 520 210"
        className="relative mt-4 w-full"
        role="img"
        aria-label="A message bus spine on the left fans out to three handler nodes on the right. The middle handler is the single in-flight message, drawn as a dashed amber connector; the others are solid."
      >
        <defs>
          <linearGradient id="wf-spine" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#f59e0b" />
            <stop offset="100%" stopColor="#f0786a" />
          </linearGradient>
          <linearGradient id="wf-live" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor="#f59e0b" />
            <stop offset="100%" stopColor="#f0786a" />
          </linearGradient>
        </defs>

        {/* the bus / mediator spine */}
        <rect
          x={spineX - 6}
          y={26}
          width={12}
          height={158}
          rx={6}
          fill="url(#wf-spine)"
          opacity={0.9}
        />
        <text
          x={spineX}
          y={16}
          textAnchor="middle"
          className="fill-amber-200 font-mono"
          style={{ fontSize: 10, letterSpacing: "0.08em" }}
        >
          BUS
        </text>

        {/* the publish entry connector into the spine */}
        <line
          x1={20}
          y1={104}
          x2={spineX - 6}
          y2={104}
          stroke="url(#wf-live)"
          strokeWidth={2}
        />
        <circle cx={20} cy={104} r={4} fill="#f59e0b" />

        {SWITCHBOARD_NODES.map((node) => {
          const isLive = node.state === "live";
          const isQueued = node.state === "queued";
          const connectorColor = isLive
            ? "url(#wf-live)"
            : isQueued
              ? "rgba(245,241,234,0.18)"
              : "rgba(94,234,212,0.55)";
          return (
            <g key={node.label}>
              {/* connector = a labeled OTel span */}
              <path
                d={`M ${spineX + 6} 104 C ${spineX + 70} 104, ${nodeX - 90} ${node.y + 16}, ${nodeX - 8} ${node.y + 16}`}
                fill="none"
                stroke={connectorColor}
                strokeWidth={isLive ? 2.4 : 1.6}
                strokeDasharray={isLive ? "5 5" : isQueued ? "2 5" : undefined}
                strokeLinecap="round"
              >
                {isLive && (
                  <animate
                    attributeName="stroke-dashoffset"
                    from="20"
                    to="0"
                    dur="1.1s"
                    repeatCount="indefinite"
                  />
                )}
              </path>
              <text
                x={(spineX + nodeX) / 2 - 6}
                y={node.y + 4}
                textAnchor="middle"
                className="fill-cc-ink-dim font-mono"
                style={{ fontSize: 8, letterSpacing: "0.04em" }}
              >
                {node.span}
              </text>

              {/* handler node */}
              <rect
                x={nodeX}
                y={node.y}
                width={200}
                height={32}
                rx={9}
                fill="rgba(12,19,34,0.92)"
                stroke={
                  isLive
                    ? "#f59e0b"
                    : isQueued
                      ? "rgba(245,241,234,0.16)"
                      : "rgba(94,234,212,0.4)"
                }
                strokeWidth={isLive ? 1.6 : 1}
              />
              <circle
                cx={nodeX + 14}
                cy={node.y + 16}
                r={4}
                fill={
                  isLive
                    ? "#f59e0b"
                    : isQueued
                      ? "rgba(245,241,234,0.3)"
                      : "#5eead4"
                }
              >
                {isLive && (
                  <animate
                    attributeName="opacity"
                    values="1;0.35;1"
                    dur="1.1s"
                    repeatCount="indefinite"
                  />
                )}
              </circle>
              <text
                x={nodeX + 26}
                y={node.y + 20}
                className="fill-cc-heading font-mono"
                style={{ fontSize: 9.5 }}
              >
                {node.label}
              </text>
            </g>
          );
        })}
      </svg>

      <div className="border-cc-card-border relative mt-3 flex items-center justify-between border-t pt-3">
        <span className="text-cc-ink-dim font-mono text-[0.6rem]">
          <span className="text-[#5eead4]">solid</span> = done &middot;{" "}
          <span className="text-amber-300">dashed</span> = in flight
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.6rem]">
          every hop &rarr; a span in Nitro
        </span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  TWO-LANE DISPATCH (mediator above, bus below, sharing the spine)          */
/* -------------------------------------------------------------------------- */

interface LaneProps {
  readonly title: string;
  readonly scope: string;
  readonly steps: readonly string[];
  readonly accent: "teal" | "amber";
}

function DispatchLane({ title, scope, steps, accent }: LaneProps) {
  const ring =
    accent === "amber" ? "border-amber-400/35" : "border-[#5eead4]/35";
  const dot = accent === "amber" ? "bg-amber-400" : "bg-[#5eead4]";
  const dotText = accent === "amber" ? "text-amber-300" : "text-[#5eead4]";

  return (
    <div
      className={`bg-cc-surface/70 rounded-2xl border ${ring} p-5 backdrop-blur-sm`}
    >
      <div className="flex items-baseline justify-between gap-3">
        <p className={`font-mono text-sm font-medium ${dotText}`}>{title}</p>
        <span className="text-cc-ink-dim font-mono text-[0.58rem] tracking-[0.1em] uppercase">
          {scope}
        </span>
      </div>
      <div className="mt-4 flex items-center">
        {steps.map((step, index) => (
          <div key={step} className="flex flex-1 items-center">
            <div className="flex items-center gap-2">
              <span
                aria-hidden="true"
                className={`size-1.5 shrink-0 rounded-full ${dot}`}
              />
              <span className="text-cc-ink font-mono text-[0.68rem]">
                {step}
              </span>
            </div>
            {index < steps.length - 1 && (
              <span
                aria-hidden="true"
                className="mx-2 h-px flex-1 bg-gradient-to-r from-amber-400/50 to-[#f0786a]/30"
              />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  TRANSPORT TABS (a faux-tabbed control; first tab presented as selected)   */
/* -------------------------------------------------------------------------- */

interface TransportTab {
  readonly name: string;
  readonly selected: boolean;
}

const TRANSPORTS: readonly TransportTab[] = [
  { name: "RabbitMQ", selected: true },
  { name: "Postgres", selected: false },
  { name: "Kafka", selected: false },
  { name: "Azure Service Bus", selected: false },
  { name: "in-process", selected: false },
];

function TransportSwitch() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
      <Eyebrow>Pluggable transports</Eyebrow>
      <p className="text-cc-heading mt-2 text-sm/relaxed">
        Route a message through a different broker without touching a handler.
      </p>
      <div className="mt-4 flex flex-wrap gap-2">
        {TRANSPORTS.map((t) => (
          <span
            key={t.name}
            className={[
              "rounded-lg border px-3 py-1.5 font-mono text-[0.68rem] transition-colors",
              t.selected
                ? "border-amber-400/60 bg-amber-400/10 text-amber-200"
                : "border-cc-card-border text-cc-ink-dim",
            ].join(" ")}
          >
            {t.name}
          </span>
        ))}
      </div>
      <div className="border-cc-card-border bg-cc-code-bg mt-4 rounded-xl border p-3 font-mono text-[0.7rem] leading-relaxed">
        <span className="text-cc-ink-dim">services.AddMocha(m =&gt; </span>
        <br />
        <span className="text-cc-ink-dim">{"  "}m.</span>
        <span className="text-amber-300">UseRabbitMq</span>
        <span className="text-cc-ink-dim">(cfg));</span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  SAGA STRIP  (Draft -> Checked -> Published, validated before traffic)     */
/* -------------------------------------------------------------------------- */

interface SagaState {
  readonly name: string;
  readonly state: "done" | "active" | "pending";
}

const SAGA_STATES: readonly SagaState[] = [
  { name: "Draft", state: "done" },
  { name: "Checked", state: "active" },
  { name: "Published", state: "pending" },
];

function SagaStrip() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
      <div className="flex items-center justify-between">
        <Eyebrow>Saga state machine</Eyebrow>
        <span className="rounded-full border border-[#5eead4]/40 px-2.5 py-0.5 font-mono text-[0.5rem] tracking-[0.1em] text-[#5eead4] uppercase">
          validated before traffic
        </span>
      </div>
      <div className="mt-5 flex items-center">
        {SAGA_STATES.map((s, index) => {
          const isDone = s.state === "done";
          const isActive = s.state === "active";
          return (
            <div
              key={s.name}
              className="flex flex-1 items-center last:flex-none"
            >
              <div className="flex flex-col items-center gap-2">
                <span
                  className={[
                    "flex size-9 items-center justify-center rounded-full border font-mono text-[0.6rem]",
                    isDone
                      ? "border-[#5eead4]/60 bg-[#5eead4]/10 text-[#5eead4]"
                      : isActive
                        ? "border-amber-400/70 bg-amber-400/10 text-amber-300"
                        : "border-cc-card-border text-cc-ink-dim border-dashed",
                  ].join(" ")}
                >
                  {isDone ? <CheckIcon /> : index + 1}
                </span>
                <span
                  className={[
                    "font-mono text-[0.66rem]",
                    isDone
                      ? "text-[#5eead4]"
                      : isActive
                        ? "text-amber-300"
                        : "text-cc-ink-dim",
                  ].join(" ")}
                >
                  {s.name}
                </span>
              </div>
              {index < SAGA_STATES.length - 1 && (
                <span
                  aria-hidden="true"
                  className={[
                    "mx-1 mt-[-1.4rem] h-px flex-1",
                    index === 0
                      ? "bg-[#5eead4]/50"
                      : "bg-[repeating-linear-gradient(90deg,rgba(245,158,11,0.6)_0_5px,transparent_5px_10px)]",
                  ].join(" ")}
                />
              )}
            </div>
          );
        })}
      </div>
      <p className="text-cc-ink-dim mt-5 text-xs/relaxed">
        Mocha checks that every state is reachable and every path lands on a
        final state, so a stuck saga is caught before the service handles
        traffic.
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  RELIABILITY (outbox/inbox) MINI-MOCK                                       */
/* -------------------------------------------------------------------------- */

function ReliabilityCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
      <Eyebrow>Outbox + inbox</Eyebrow>
      <p className="text-cc-heading mt-2 text-sm/relaxed">
        The DB write and the publish commit together; the consumer dedupes on
        receive.
      </p>
      <div className="mt-4 space-y-2.5">
        {[
          {
            stage: "outbox",
            note: "write + enqueue in one transaction",
            done: true,
          },
          {
            stage: "transport",
            note: "at-least-once delivery on the wire",
            done: true,
          },
          {
            stage: "inbox",
            note: "dedupe by message id, run once",
            done: false,
          },
        ].map((row) => (
          <div
            key={row.stage}
            className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2.5"
          >
            <span className="text-cc-ink w-20 shrink-0 font-mono text-[0.58rem] tracking-[0.08em] uppercase">
              {row.stage}
            </span>
            <span className="text-cc-ink-dim flex-1 font-mono text-[0.66rem]">
              {row.note}
            </span>
            {row.done ? (
              <span className="text-[#5eead4]">
                <CheckIcon />
              </span>
            ) : (
              <span className="size-1.5 rounded-full bg-amber-400" />
            )}
          </div>
        ))}
      </div>
      <p className="mt-4 font-mono text-[0.62rem] text-amber-300/90">
        = effectively exactly-once{" "}
        <span className="text-cc-ink-dim">processing</span>
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  STAT TILE                                                                  */
/* -------------------------------------------------------------------------- */

interface StatProps {
  readonly value: string;
  readonly label: string;
  readonly hint: string;
}

function StatTile({ value, label, hint }: StatProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 flex flex-col justify-center rounded-2xl border p-6 backdrop-blur-sm">
      <p className="font-heading text-h3 bg-gradient-to-br from-amber-300 to-[#f0786a] bg-clip-text leading-none font-semibold text-transparent">
        {value}
      </p>
      <p className="text-cc-heading mt-3 text-sm font-medium">{label}</p>
      <p className="text-cc-ink-dim mt-1 text-xs/relaxed">{hint}</p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  COLLAPSE-THE-TANGLE  (before / after)                                      */
/* -------------------------------------------------------------------------- */

const TANGLE_PARTS: readonly string[] = [
  "broker client",
  "retry policy",
  "dedup table",
  "dead-letter queue",
  "outbox publisher",
  "consumer wiring",
  "correlation plumbing",
];

function CollapseTangle() {
  return (
    <div className="grid items-stretch gap-4 sm:grid-cols-[1fr_auto_1fr]">
      {/* before */}
      <div className="border-cc-card-border bg-cc-card-bg/40 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.14em] uppercase">
          Before &middot; hand-rolled
        </p>
        <div className="mt-4 flex flex-wrap gap-2">
          {TANGLE_PARTS.map((part) => (
            <span
              key={part}
              className="border-cc-card-border text-cc-ink-dim rounded-md border border-dashed px-2 py-1 font-mono text-[0.62rem]"
            >
              {part}
            </span>
          ))}
        </div>
        <p className="text-cc-ink-dim mt-4 text-xs/relaxed">
          Seven moving parts to keep one message reliable, each one a place to
          get the edge cases wrong.
        </p>
      </div>

      {/* arrow */}
      <div className="flex items-center justify-center">
        <span
          aria-hidden="true"
          className="font-heading bg-gradient-to-r from-amber-400 to-[#f0786a] bg-clip-text text-2xl text-transparent sm:rotate-0"
        >
          &rarr;
        </span>
      </div>

      {/* after */}
      <div className="rounded-2xl border border-amber-400/40 bg-amber-400/5 p-5 backdrop-blur-sm">
        <p className="font-mono text-[0.6rem] tracking-[0.14em] text-amber-300 uppercase">
          After &middot; one definition
        </p>
        <div className="border-cc-card-border bg-cc-code-bg mt-4 rounded-xl border p-4 font-mono text-[0.72rem] leading-relaxed">
          <span className="text-amber-300">[Handler]</span>
          <br />
          <span className="text-cc-heading">Task</span>{" "}
          <span className="text-cc-ink">Handle(</span>
          <span className="text-cc-heading">ReviewCreated</span>{" "}
          <span className="text-cc-ink">e)</span>
          <br />
          <span className="text-cc-ink-dim">
            {"  // your code, nothing else"}
          </span>
        </div>
        <p className="text-cc-ink mt-4 text-xs/relaxed">
          Retries, dead-letter, dedup, correlation, and the outbox are pipeline
          middleware Mocha wires around your handler.
        </p>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  PAGE                                                                       */
/* -------------------------------------------------------------------------- */

export default function WorkflowsPreviewPage() {
  return (
    <>
      {/* ---------------------------------------------------------------- */}
      {/*  HERO: spotlight + switchboard                                    */}
      {/* ---------------------------------------------------------------- */}
      <section className="relative py-16 sm:py-24">
        {/* gradient-mesh spotlight behind the hero (scene accent) */}
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 -top-10 -z-10 mx-auto h-[420px] max-w-3xl rounded-full bg-[radial-gradient(60%_60%_at_50%_30%,rgba(245,158,11,0.18),transparent_70%)] blur-2xl"
        />
        <div className="grid items-center gap-12 lg:grid-cols-[1fr_1.05fr]">
          <div>
            <Eyebrow>Workflows &middot; Mocha</Eyebrow>
            <h1 className="font-heading text-cc-heading mt-5 text-4xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
              Let work continue after the request.
            </h1>
            <p className="text-cc-ink-dim mt-6 max-w-xl text-base sm:text-lg">
              Return the response now, keep the work moving in the background. A
              review is created, the response is sent, and indexing, scoring,
              and notifications carry on as messages, each one a span you can
              follow.
            </p>
            <div className="mt-9 flex flex-wrap gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
            </div>
            <p className="text-cc-ink-dim mt-6 font-mono text-xs">
              CreateReview &rarr; ReviewCreated &rarr; 3 handlers, in flight
            </p>
          </div>
          <HeroSwitchboard />
        </div>
      </section>

      {/* ---------------------------------------------------------------- */}
      {/*  TWO LANES: mediator above, bus below                             */}
      {/* ---------------------------------------------------------------- */}
      <section className="py-12">
        <div className="max-w-3xl">
          <Eyebrow>One model, two reaches</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-tight font-semibold text-balance">
            A mediator in-process and a bus across services, from one handler.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            Mocha is a source-generated in-process mediator (CQRS) and a
            cross-service message bus in one. The same handler-first model
            dispatches a command to its handler inside the process, or publishes
            an event onto a transport for another service to consume.
          </p>
        </div>
        <div className="mt-8 space-y-4">
          <DispatchLane
            title="mediator"
            scope="in-process"
            accent="teal"
            steps={["command", "[Handler]", "response"]}
          />
          <DispatchLane
            title="message bus"
            scope="cross-service"
            accent="amber"
            steps={["PublishAsync", "transport", "consume"]}
          />
        </div>
      </section>

      {/* ---------------------------------------------------------------- */}
      {/*  BENTO GRID                                                        */}
      {/* ---------------------------------------------------------------- */}
      <section className="py-12">
        <div className="max-w-3xl">
          <Eyebrow>The moving parts</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-tight font-semibold text-balance">
            Reliability, topology, and state, all in the pipeline.
          </h2>
        </div>

        <div className="mt-8 grid gap-4 lg:grid-cols-3">
          {/* oversized: saga strip spans two columns */}
          <div className="lg:col-span-2">
            <SagaStrip />
          </div>
          {/* stat tile */}
          <StatTile
            value="5"
            label="Transports, one API"
            hint="RabbitMQ, Postgres, Kafka, Azure Service Bus, in-process."
          />

          {/* reliability */}
          <ReliabilityCard />
          {/* transports */}
          <TransportSwitch />
          {/* pull quote */}
          <div className="flex flex-col justify-center rounded-2xl border border-[#f0786a]/30 bg-gradient-to-br from-amber-500/10 to-[#f0786a]/5 p-6 backdrop-blur-sm">
            <p className="font-heading text-cc-heading text-lg leading-snug font-medium text-balance">
              &ldquo;Every dispatch, receive, and handler run is a span. You
              read the topology, not the logs.&rdquo;
            </p>
            <p className="text-cc-ink-dim mt-4 font-mono text-[0.62rem] tracking-[0.1em] uppercase">
              OpenTelemetry-native &middot; Nitro
            </p>
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------------- */}
      {/*  COLLAPSE THE TANGLE                                               */}
      {/* ---------------------------------------------------------------- */}
      <section className="py-12">
        <div className="max-w-3xl">
          <Eyebrow>Collapse the tangle</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-tight font-semibold text-balance">
            From a hand-rolled broker to one handler.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            The reliability machinery you would otherwise assemble by hand,
            retries, dead-letter routing, deduplication, the outbox, correlation
            propagation, is middleware in Mocha&rsquo;s receive and dispatch
            pipeline. You write the handler; the platform wires the rest.
          </p>
        </div>
        <div className="mt-8">
          <CollapseTangle />
        </div>
      </section>

      {/* ---------------------------------------------------------------- */}
      {/*  HONESTY BEAT                                                      */}
      {/* ---------------------------------------------------------------- */}
      <section className="py-12">
        <div className="bg-cc-card-bg/60 relative mx-auto max-w-3xl overflow-hidden rounded-3xl border border-amber-400/25 p-8 backdrop-blur-sm sm:p-10">
          <div
            aria-hidden="true"
            className="pointer-events-none absolute -top-16 -right-16 h-48 w-48 rounded-full bg-amber-500/10 blur-3xl"
          />
          <Eyebrow>Honest by design</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-tight font-semibold text-balance">
            Exactly-once <span className="text-amber-300">processing</span>, not
            exactly-once delivery.
          </h2>
          <p className="text-cc-ink mt-5 text-base/relaxed">
            The wire delivers at least once; networks retry. The transactional
            outbox keeps your write and your publish atomic, and the idempotent
            inbox dedupes on receive, so a handler runs once per message even
            when the transport delivers it twice. Sagas are validated before the
            service handles traffic, not at compile time. That is the claim we
            can stand behind, and it is the one that matters when you are on
            call.
          </p>
        </div>
      </section>

      {/* ---------------------------------------------------------------- */}
      {/*  CLOSING CTA                                                       */}
      {/* ---------------------------------------------------------------- */}
      <section className="py-12">
        <div className="grid items-start gap-8 lg:grid-cols-2 lg:gap-12">
          <div className="border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-8 backdrop-blur-sm">
            <h2 className="font-heading text-cc-heading text-h4 leading-tight font-semibold">
              What you get out of the box.
            </h2>
            <ul className="mt-6 space-y-4">
              {[
                "Source-generated mediator and bus from one handler-first model",
                "Pluggable transports: RabbitMQ, Postgres, Kafka, Azure Service Bus, in-process",
                "Transactional outbox and idempotent inbox for exactly-once processing",
                "Sagas validated before the service handles traffic",
                "Every hop a span in Nitro, correlation propagated across services",
              ].map((item) => (
                <li key={item} className="flex items-start gap-3">
                  <span className="mt-0.5 shrink-0 text-amber-300">
                    <CheckIcon />
                  </span>
                  <span className="text-cc-ink text-sm/relaxed">{item}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="flex flex-col justify-center">
            <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold text-balance">
              Keep the work moving.
            </h2>
            <p className="text-cc-ink mt-5 text-base/relaxed">
              Send the response, then let indexing, scoring, and notifications
              continue as messages you can trace end to end.
            </p>
            <div className="mt-8 flex flex-wrap gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
            </div>
            <p className="text-cc-ink-dim mt-6 text-sm">
              Explore the wider{" "}
              <Link
                href="/platform"
                className="text-amber-300 transition-colors hover:text-amber-200"
              >
                platform
              </Link>
              .
            </p>
          </div>
        </div>
      </section>
    </>
  );
}
