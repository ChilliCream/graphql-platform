import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Workflow — In-Process Mediator & Message Bus | Mocha",
  description:
    "Let work continue after the request returns. Mocha is a source-generated in-process mediator and cross-service message bus with pluggable transports, sagas, and exactly-once processing.",
  keywords: [
    "Mocha",
    "in-process mediator",
    "message bus",
    "CQRS",
    "sagas",
    "transactional outbox",
    "idempotent inbox",
    "exactly-once processing",
    "RabbitMQ",
    "Kafka",
    "Azure Service Bus",
    "Postgres transport",
    ".NET messaging",
    "OpenTelemetry",
  ],
  openGraph: {
    title: "Workflow — In-Process Mediator & Message Bus | Mocha",
    description:
      "Let work continue after the request returns. One source-generated framework for the in-process mediator and the cross-service bus, with sagas, reliable delivery, and a span per hop.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  ACCENT                                                                     */
/*  This scene's ink is amber #f59e0b warming toward coral #f0786a — the       */
/*  single running message. It is used as INK, sparingly, on a mostly          */
/*  monochrome editorial page. Teal is intentionally absent.                   */
/* -------------------------------------------------------------------------- */

const AMBER = "#f59e0b";
const CORAL = "#f0786a";

/* -------------------------------------------------------------------------- */
/*  BLUEPRINT — a faint dot-grid surface the line-diagrams are drawn on.       */
/* -------------------------------------------------------------------------- */

interface BlueprintProps {
  readonly children: React.ReactNode;
  readonly className?: string;
}

function Blueprint({ children, className }: BlueprintProps) {
  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border backdrop-blur-sm",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          backgroundImage:
            "radial-gradient(rgba(245,241,234,0.10) 1px, transparent 1px)",
          backgroundSize: "22px 22px",
          backgroundPosition: "-1px -1px",
          maskImage:
            "radial-gradient(120% 120% at 50% 0%, #000 55%, transparent 100%)",
        }}
      />
      <div className="relative">{children}</div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  SECTION NUMBER — the numbered narrative spine (01 / 02 / 03 ...).          */
/* -------------------------------------------------------------------------- */

interface ChapterProps {
  readonly index: string;
  readonly kicker: string;
}

function ChapterMark({ index, kicker }: ChapterProps) {
  return (
    <div className="flex items-baseline gap-4">
      <span
        className="font-heading text-h3 leading-none font-semibold tabular-nums"
        style={{ color: AMBER }}
      >
        {index}
      </span>
      <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.28em] uppercase">
        {kicker}
      </span>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  HERO LINE-ART — a minimal flat left-to-right trace.                        */
/*  The request returns early; the message keeps moving. ONE dashed amber      */
/*  segment marks the in-flight hop; settled hops are solid hairlines.         */
/* -------------------------------------------------------------------------- */

function HeroTrace() {
  const nodes = [
    { x: 64, label: "request", on: true },
    { x: 240, label: "outbox", on: true },
    { x: 416, label: "publish", on: true },
    { x: 592, label: "consume", flight: true },
    { x: 664, label: "saga", pending: true },
  ] as const;

  return (
    <svg
      viewBox="0 0 720 168"
      className="h-auto w-full"
      role="img"
      aria-label="A request returns at the first node while a single message continues right through three more hops; the in-flight hop is a dashed amber line, the settled hops are solid."
    >
      <defs>
        <linearGradient id="wf2-flight" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor={AMBER} />
          <stop offset="100%" stopColor={CORAL} />
        </linearGradient>
        <marker
          id="wf2-tip"
          viewBox="0 0 10 10"
          refX="7"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M0 0 L10 5 L0 10 z" fill={CORAL} />
        </marker>
      </defs>

      {/* the request that returns early */}
      <g>
        <line
          x1="64"
          y1="44"
          x2="64"
          y2="14"
          stroke="rgba(245,241,234,0.3)"
          strokeWidth="1.5"
        />
        <polyline
          points="48,24 64,12 80,24"
          fill="none"
          stroke="rgba(245,241,234,0.3)"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <text
          x="64"
          y="8"
          textAnchor="middle"
          className="font-mono"
          fontSize="9"
          fill="rgba(245,241,234,0.5)"
        >
          200 OK
        </text>
      </g>

      {/* the spine */}
      <line
        x1="32"
        y1="92"
        x2="688"
        y2="92"
        stroke="rgba(245,241,234,0.12)"
        strokeWidth="1"
      />

      {/* settled hops — solid hairlines between nodes */}
      <line
        x1="64"
        y1="92"
        x2="240"
        y2="92"
        stroke="rgba(245,241,234,0.42)"
        strokeWidth="2"
      />
      <line
        x1="240"
        y1="92"
        x2="416"
        y2="92"
        stroke="rgba(245,241,234,0.42)"
        strokeWidth="2"
      />

      {/* the single in-flight hop — dashed, accent, animated */}
      <line
        x1="416"
        y1="92"
        x2="592"
        y2="92"
        stroke="url(#wf2-flight)"
        strokeWidth="2.5"
        strokeDasharray="7 7"
        strokeLinecap="round"
        markerEnd="url(#wf2-tip)"
      >
        <animate
          attributeName="stroke-dashoffset"
          from="28"
          to="0"
          dur="1.1s"
          repeatCount="indefinite"
        />
      </line>

      {/* pending tail — not yet reached */}
      <line
        x1="592"
        y1="92"
        x2="664"
        y2="92"
        stroke="rgba(245,241,234,0.16)"
        strokeWidth="1.5"
        strokeDasharray="3 6"
      />

      {/* nodes */}
      {nodes.map((n) => {
        const flight = "flight" in n && n.flight;
        const pending = "pending" in n && n.pending;
        return (
          <g key={n.x}>
            <circle
              cx={n.x}
              cy="92"
              r={flight ? 6 : 4.5}
              fill={flight ? CORAL : pending ? "transparent" : "#0b0f1a"}
              stroke={
                flight
                  ? CORAL
                  : pending
                    ? "rgba(245,241,234,0.3)"
                    : "rgba(245,241,234,0.6)"
              }
              strokeWidth="1.5"
            />
            <text
              x={n.x}
              y="118"
              textAnchor="middle"
              className="font-mono"
              fontSize="9.5"
              fill={
                flight ? CORAL : pending ? "rgba(245,241,234,0.4)" : "#a1a3af"
              }
            >
              {n.label}
            </text>
          </g>
        );
      })}

      {/* legend */}
      <g>
        <line
          x1="32"
          y1="150"
          x2="58"
          y2="150"
          stroke="rgba(245,241,234,0.42)"
          strokeWidth="2"
        />
        <text
          x="66"
          y="153"
          className="font-mono"
          fontSize="9"
          fill="rgba(245,241,234,0.5)"
        >
          settled
        </text>
        <line
          x1="150"
          y1="150"
          x2="176"
          y2="150"
          stroke={AMBER}
          strokeWidth="2"
          strokeDasharray="6 6"
        />
        <text
          x="184"
          y="153"
          className="font-mono"
          fontSize="9"
          fill="rgba(245,241,234,0.5)"
        >
          in flight
        </text>
      </g>
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  CODE PANE — a small spec/code block in the page's code chrome.            */
/* -------------------------------------------------------------------------- */

interface CodePaneProps {
  readonly file: string;
  readonly lines: readonly React.ReactNode[];
}

function CodePane({ file, lines }: CodePaneProps) {
  return (
    <figure className="border-cc-card-border overflow-hidden rounded-xl border">
      <figcaption className="border-cc-card-border bg-cc-code-header flex items-center gap-2 border-b px-4 py-2.5">
        <span className="h-2.5 w-2.5 rounded-full bg-[#f0786a]/70" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#f59e0b]/70" />
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="text-cc-nav-label ml-2 font-mono text-[0.72rem]">
          {file}
        </span>
      </figcaption>
      <pre className="bg-cc-code-bg overflow-x-auto px-4 py-4 font-mono text-[0.78rem] leading-[1.7]">
        <code className="text-cc-ink">
          {lines.map((l, i) => (
            <div key={i}>{l}</div>
          ))}
        </code>
      </pre>
    </figure>
  );
}

const kw = (s: string) => <span style={{ color: "#7c92c6" }}>{s}</span>;
const at = (s: string) => <span style={{ color: AMBER }}>{s}</span>;
const cm = (s: string) => <span className="text-cc-ink-dim">{s}</span>;
const ty = (s: string) => <span className="text-cc-heading">{s}</span>;

/* -------------------------------------------------------------------------- */
/*  TWO-LANE DIAGRAM — mediator above, bus below, sharing one vertical seam.  */
/*  Flat horizontal lanes (NOT isometric). The bus lane carries the dashed    */
/*  in-flight segment; the mediator lane is settled.                          */
/* -------------------------------------------------------------------------- */

interface LaneProps {
  readonly title: string;
  readonly scope: string;
  readonly steps: readonly string[];
  readonly inFlight?: boolean;
}

function Lane({ title, scope, steps, inFlight }: LaneProps) {
  return (
    <div className="px-5 py-5 sm:px-7">
      <div className="mb-4 flex items-baseline justify-between">
        <span className="font-heading text-cc-heading text-h6 font-semibold">
          {title}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.68rem] tracking-[0.2em] uppercase">
          {scope}
        </span>
      </div>
      <div className="flex items-center">
        {steps.map((step, i) => {
          const last = i === steps.length - 1;
          const flight = inFlight && i === steps.length - 2;
          return (
            <div key={step} className="flex flex-1 items-center last:flex-none">
              <span
                className="rounded-md border px-3 py-1.5 font-mono text-[0.72rem] whitespace-nowrap"
                style={{
                  borderColor: flight ? CORAL : "rgba(245,241,234,0.18)",
                  color: flight ? CORAL : "#a1a3af",
                  background: flight ? "rgba(240,120,106,0.08)" : "transparent",
                }}
              >
                {step}
              </span>
              {!last && (
                <span
                  className="mx-1.5 h-px flex-1"
                  aria-hidden
                  style={{
                    background:
                      inFlight && i === steps.length - 2
                        ? `repeating-linear-gradient(90deg, ${AMBER} 0 6px, transparent 6px 12px)`
                        : "rgba(245,241,234,0.3)",
                  }}
                />
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function TwoLaneDiagram() {
  return (
    <Blueprint>
      <div className="divide-cc-card-border divide-y">
        <Lane
          title="Mediator"
          scope="in-process"
          steps={["request", "[Handler]", "response"]}
        />
        <Lane
          title="Message bus"
          scope="cross-service"
          steps={["PublishAsync", "transport", "consume"]}
          inFlight
        />
      </div>
      <div className="border-cc-card-border border-t px-5 py-3 sm:px-7">
        <p className="text-cc-ink-dim font-mono text-[0.72rem]">
          one handler shape · one spine · the message keeps moving after the
          response returns
        </p>
      </div>
    </Blueprint>
  );
}

/* -------------------------------------------------------------------------- */
/*  SAGA STRIP — Draft -> Checked -> Published. Settled hops solid; the        */
/*  active transition dashed amber. Validated before traffic.                  */
/* -------------------------------------------------------------------------- */

interface SagaStateProps {
  readonly label: string;
  readonly state: "done" | "active" | "pending";
}

function SagaState({ label, state }: SagaStateProps) {
  const done = state === "done";
  const active = state === "active";
  return (
    <div className="flex flex-col items-center gap-2">
      <span
        className="flex h-9 w-9 items-center justify-center rounded-full border text-[0.7rem]"
        style={{
          borderColor: done
            ? "rgba(245,241,234,0.6)"
            : active
              ? CORAL
              : "rgba(245,241,234,0.22)",
          background: active ? "rgba(240,120,106,0.1)" : "transparent",
          color: active ? CORAL : done ? "#a1a3af" : "rgba(245,241,234,0.4)",
        }}
      >
        {done ? <CheckIcon size={13} /> : active ? "●" : "○"}
      </span>
      <span
        className="font-mono text-[0.72rem]"
        style={{
          color: active ? CORAL : done ? "#a1a3af" : "rgba(245,241,234,0.4)",
        }}
      >
        {label}
      </span>
    </div>
  );
}

function SagaStrip() {
  return (
    <Blueprint className="px-6 py-7">
      <div className="flex items-start justify-between">
        <SagaState label="Draft" state="done" />
        <div
          className="mt-4 h-px flex-1"
          aria-hidden
          style={{ background: "rgba(245,241,234,0.42)" }}
        />
        <SagaState label="Checked" state="done" />
        <div
          className="mt-4 h-px flex-1"
          aria-hidden
          style={{
            background: `repeating-linear-gradient(90deg, ${AMBER} 0 6px, transparent 6px 12px)`,
          }}
        />
        <SagaState label="Published" state="active" />
      </div>
      <p className="border-cc-card-border text-cc-ink-dim mt-6 border-t pt-4 font-mono text-[0.72rem]">
        every state reachable · every path terminates · validated before the
        service handles traffic
      </p>
    </Blueprint>
  );
}

/* -------------------------------------------------------------------------- */
/*  TRANSPORTS — a quiet badge row. Pluggable; handlers never change.         */
/* -------------------------------------------------------------------------- */

const TRANSPORTS = [
  "RabbitMQ",
  "Postgres",
  "in-process",
  "Kafka",
  "Azure Service Bus",
] as const;

function TransportRow() {
  return (
    <div className="flex flex-wrap items-center gap-2.5">
      {TRANSPORTS.map((t) => (
        <span
          key={t}
          className="border-cc-card-border text-cc-ink rounded-full border px-3.5 py-1.5 font-mono text-[0.72rem]"
        >
          {t}
        </span>
      ))}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  COLLAPSE-THE-TANGLE — hand-rolled plumbing vs one Mocha definition.        */
/* -------------------------------------------------------------------------- */

const TANGLE = [
  "broker client + connection retry",
  "publish confirms + outbox table",
  "consumer dedup / idempotency keys",
  "dead-letter queue + replay",
  "saga state persistence",
  "retry / backoff / poison handling",
] as const;

function CollapseTangle() {
  return (
    <div className="grid gap-5 sm:grid-cols-[1fr_auto_1fr] sm:items-stretch">
      <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 backdrop-blur-sm">
        <span className="text-cc-nav-label font-mono text-[0.68rem] tracking-[0.2em] uppercase">
          hand-rolled
        </span>
        <ul className="mt-4 space-y-2.5">
          {TANGLE.map((t) => (
            <li
              key={t}
              className="text-cc-ink-dim decoration-cc-ink-faint flex items-start gap-2.5 font-mono text-[0.78rem] line-through"
            >
              <span
                aria-hidden
                className="bg-cc-ink-faint mt-1.5 h-1 w-1 shrink-0 rounded-full"
              />
              {t}
            </li>
          ))}
        </ul>
      </div>

      <div className="flex items-center justify-center sm:flex-col">
        <span
          className="font-heading text-h4 leading-none font-semibold"
          style={{ color: AMBER }}
          aria-hidden
        >
          →
        </span>
      </div>

      <div
        className="rounded-xl border p-5"
        style={{
          borderColor: "rgba(245,158,11,0.3)",
          background: "rgba(245,158,11,0.04)",
        }}
      >
        <span className="text-cc-nav-label font-mono text-[0.68rem] tracking-[0.2em] uppercase">
          one Mocha definition
        </span>
        <pre className="mt-4 overflow-x-auto font-mono text-[0.78rem] leading-[1.8]">
          <code className="text-cc-ink">
            <div>{at("[Handler]")}</div>
            <div>
              {kw("public")} {kw("async")} {ty("Task")} HandleAsync(
            </div>
            <div>{"  "}CreateReview cmd)</div>
            <div>{"{"}</div>
            <div>{"  "}await bus.PublishAsync(</div>
            <div>
              {"    "}
              {kw("new")} ReviewCreated(cmd.Id));
            </div>
            <div>{"}"}</div>
          </code>
        </pre>
        <p className="border-cc-card-border text-cc-ink-dim mt-4 border-t pt-3 font-mono text-[0.72rem]">
          outbox, inbox, retries and dead-lettering are the framework, not your
          code.
        </p>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  TRACE RIBBON — every hop a span in Nitro. A flat waterfall.               */
/* -------------------------------------------------------------------------- */

interface SpanRowProps {
  readonly label: string;
  readonly offset: number;
  readonly width: number;
  readonly accent?: boolean;
}

function SpanRow({ label, offset, width, accent }: SpanRowProps) {
  return (
    <div className="flex items-center gap-4 py-1.5">
      <span className="text-cc-ink-dim w-40 shrink-0 font-mono text-[0.72rem]">
        {label}
      </span>
      <div className="bg-cc-ink-faint/40 relative h-3 flex-1 rounded-sm">
        <span
          className="absolute top-0 h-3 rounded-sm"
          style={{
            left: `${offset}%`,
            width: `${width}%`,
            background: accent
              ? `linear-gradient(90deg, ${AMBER}, ${CORAL})`
              : "rgba(245,241,234,0.32)",
          }}
        />
      </div>
    </div>
  );
}

function TraceRibbon() {
  return (
    <Blueprint className="px-5 py-6 sm:px-7">
      <div className="mb-4 flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.2em] uppercase">
          trace · CreateReview
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">Nitro</span>
      </div>
      <SpanRow label="POST /reviews" offset={0} width={96} />
      <SpanRow label="mediator · [Handler]" offset={4} width={28} />
      <SpanRow label="outbox · commit" offset={28} width={14} />
      <SpanRow label="publish · ReviewCreated" offset={42} width={20} accent />
      <SpanRow label="transport · RabbitMQ" offset={58} width={16} />
      <SpanRow label="inbox · consume" offset={70} width={22} />
      <p className="border-cc-card-border text-cc-ink-dim mt-5 border-t pt-4 font-mono text-[0.72rem]">
        the response returns at ~32% — every later hop is still a span you can
        open.
      </p>
    </Blueprint>
  );
}

/* -------------------------------------------------------------------------- */
/*  PAGE                                                                        */
/* -------------------------------------------------------------------------- */

export default function WorkflowsPreviewV2() {
  return (
    <article className="pb-24">
      {/* ---------------------------------------------------------------- */}
      {/* HERO                                                             */}
      {/* ---------------------------------------------------------------- */}
      <header className="pt-6 sm:pt-10">
        <span className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.3em] uppercase">
          Mocha · Workflow
        </span>
        <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6 leading-[1.02] font-semibold text-balance">
          Let work continue
          <br />
          after the request.
        </h1>
        <p className="lead text-cc-ink-dim mt-7 max-w-2xl">
          A request should answer fast and leave. The actual work — notifying,
          projecting, publishing, compensating — keeps moving on its own. Mocha
          is the one framework that carries it: an in-process mediator and a
          cross-service message bus, sharing a single handler shape.
        </p>

        <div className="mt-9 flex flex-wrap gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
        </div>

        <div className="mt-14">
          <Blueprint className="px-5 py-7 sm:px-8">
            <HeroTrace />
          </Blueprint>
        </div>
      </header>

      {/* a thin rule introduces the numbered narrative */}
      <div className="bg-cc-card-border mt-20 mb-16 h-px w-full" />

      {/* ---------------------------------------------------------------- */}
      {/* 01 — TWO JOBS, ONE FRAMEWORK                                     */}
      {/* ---------------------------------------------------------------- */}
      <section className="grid gap-10 lg:grid-cols-2 lg:items-center lg:gap-16">
        <div>
          <ChapterMark index="01" kicker="mediator and bus, in one" />
          <h2 className="font-heading text-cc-heading text-h3 mt-5 leading-tight font-semibold text-balance">
            The same handler, in-process or across the wire.
          </h2>
          <p className="text-cc-ink mt-5 max-w-xl">
            In-process, Mocha is a CQRS mediator: a command meets a{" "}
            <span className="text-cc-heading font-mono">[Handler]</span> and
            returns a response. Across services, the same shape becomes a
            message bus —{" "}
            <span className="text-cc-heading font-mono">PublishAsync</span>{" "}
            hands the message to a transport and a consumer picks it up.
          </p>
          <p className="text-cc-ink-dim mt-4 max-w-xl">
            You learn one model and one set of handlers. Whether a call stays in
            the process or crosses the network becomes a wiring decision, not a
            rewrite. Both lanes share a single spine.
          </p>
        </div>
        <div className="lg:pl-2">
          <TwoLaneDiagram />
        </div>
      </section>

      <div className="bg-cc-card-border my-16 h-px w-full" />

      {/* ---------------------------------------------------------------- */}
      {/* 02 — RELIABILITY                                                 */}
      {/* ---------------------------------------------------------------- */}
      <section className="grid gap-10 lg:grid-cols-2 lg:items-center lg:gap-16">
        <div className="order-2 lg:order-1 lg:pr-2">
          <CodePane
            file="OutboxConfiguration.cs"
            lines={[
              <>{cm("// writes and dispatch commit together")}</>,
              <>
                services.{at("AddMocha")}(m {"=>"}
              </>,
              <>{"{"}</>,
              <>
                {"  "}m.{at("UseOutbox")}();{"   "}
                {cm("// transactional")}
              </>,
              <>
                {"  "}m.{at("UseInbox")}();{"    "}
                {cm("// idempotent")}
              </>,
              <>
                {"  "}m.{at("UseRabbitMq")}(rabbit);
              </>,
              <>{"});"}</>,
              <> </>,
              <>{cm("// exactly-once PROCESSING:")}</>,
              <>{cm("// a duplicate delivery is handled once.")}</>,
            ]}
          />
        </div>
        <div className="order-1 lg:order-2">
          <ChapterMark index="02" kicker="reliable by construction" />
          <h2 className="font-heading text-cc-heading text-h3 mt-5 leading-tight font-semibold text-balance">
            The write and the dispatch succeed together.
          </h2>
          <p className="text-cc-ink mt-5 max-w-xl">
            A transactional{" "}
            <span className="text-cc-heading font-mono">outbox</span> commits
            your state change and the outgoing message in the same transaction,
            so a message is never lost between a successful write and a failed
            broker call. An idempotent{" "}
            <span className="text-cc-heading font-mono">inbox</span> recognises
            a redelivered message and handles it once.
          </p>
          <p className="text-cc-ink-dim mt-4 max-w-xl">
            Brokers retry; networks duplicate. Mocha gives you exactly-once{" "}
            <span className="text-cc-heading">processing</span> — the effect
            lands once even when delivery does not. That is honest reliability,
            not a delivery guarantee no broker can make.
          </p>
        </div>
      </section>

      <div className="bg-cc-card-border my-16 h-px w-full" />

      {/* ---------------------------------------------------------------- */}
      {/* 03 — SAGAS                                                       */}
      {/* ---------------------------------------------------------------- */}
      <section className="grid gap-10 lg:grid-cols-2 lg:items-center lg:gap-16">
        <div>
          <ChapterMark index="03" kicker="sagas that can't strand" />
          <h2 className="font-heading text-cc-heading text-h3 mt-5 leading-tight font-semibold text-balance">
            A long-running flow that can&apos;t get stuck.
          </h2>
          <p className="text-cc-ink mt-5 max-w-xl">
            A review moves{" "}
            <span className="text-cc-heading font-mono">
              Draft → Checked → Published
            </span>{" "}
            across services and time. Mocha models that as a saga: a state
            machine with compensation, driven entirely by messages.
          </p>
          <p className="text-cc-ink-dim mt-4 max-w-xl">
            Before the service starts handling traffic, Mocha validates the saga
            — every state is reachable and every path reaches an end. You can
            still hit failures, but you can&apos;t deploy a flow with a dead end
            baked in.
          </p>
        </div>
        <div className="lg:pl-2">
          <SagaStrip />
        </div>
      </section>

      <div className="bg-cc-card-border my-16 h-px w-full" />

      {/* ---------------------------------------------------------------- */}
      {/* 04 — TRANSPORTS + COLLAPSE THE TANGLE                            */}
      {/* ---------------------------------------------------------------- */}
      <section>
        <ChapterMark index="04" kicker="swap the transport, keep the code" />
        <h2 className="font-heading text-cc-heading text-h3 mt-5 max-w-3xl leading-tight font-semibold text-balance">
          One definition replaces a drawer of plumbing.
        </h2>
        <p className="text-cc-ink mt-5 max-w-2xl">
          The transport is configuration. Start in-process, move to RabbitMQ or
          Postgres for durability, reach for Kafka or Azure Service Bus at scale
          — your handlers never change. Everything you would normally hand-roll
          around a broker is collapsed into the framework.
        </p>

        <div className="mt-8">
          <TransportRow />
        </div>

        <div className="mt-10">
          <CollapseTangle />
        </div>
      </section>

      <div className="bg-cc-card-border my-16 h-px w-full" />

      {/* ---------------------------------------------------------------- */}
      {/* 05 — OBSERVABILITY                                               */}
      {/* ---------------------------------------------------------------- */}
      <section className="grid gap-10 lg:grid-cols-2 lg:items-center lg:gap-16">
        <div className="order-2 lg:order-1 lg:pr-2">
          <TraceRibbon />
        </div>
        <div className="order-1 lg:order-2">
          <ChapterMark index="05" kicker="see where the message went" />
          <h2 className="font-heading text-cc-heading text-h3 mt-5 leading-tight font-semibold text-balance">
            Every hop is a span.
          </h2>
          <p className="text-cc-ink mt-5 max-w-xl">
            Asynchronous work is hard to debug because the thread of causality
            is invisible. Mocha makes it visible: each hop — the handler, the
            outbox commit, the publish, the transport, the inbox consume — emits
            an OpenTelemetry span, stitched into one trace in Nitro.
          </p>
          <p className="text-cc-ink-dim mt-4 max-w-xl">
            The request returned long ago, but you can still open the trace and
            watch the message finish its journey, hop by hop.
          </p>
        </div>
      </section>

      <div className="bg-cc-card-border my-16 h-px w-full" />

      {/* ---------------------------------------------------------------- */}
      {/* HONESTY BEAT                                                     */}
      {/* ---------------------------------------------------------------- */}
      <section className="mx-auto max-w-3xl text-center">
        <span className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.3em] uppercase">
          what we don&apos;t claim
        </span>
        <p className="font-heading text-cc-heading text-h4 mt-5 leading-snug font-semibold text-balance">
          Honest about the hard parts of distributed work.
        </p>
        <div className="mt-9 grid gap-4 text-left sm:grid-cols-2">
          {[
            [
              "Exactly-once processing, not delivery.",
              "Brokers can and will redeliver. We guarantee the effect happens once, not that a packet arrives exactly once.",
            ],
            [
              "Sagas are validated, not invincible.",
              "We check reachability and termination before traffic. Runtime failures still happen; you just can't ship a structurally stuck flow.",
            ],
            [
              "Published clients are what's affected.",
              "When a contract changes, we tell you which published clients are affected — not a prophecy about what breaks at runtime.",
            ],
            [
              "Strawberry Shake is MSBuild codegen.",
              "Typed .NET clients are generated by an MSBuild step. We're precise about which tool does which job.",
            ],
          ].map(([title, body]) => (
            <div
              key={title}
              className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 backdrop-blur-sm"
            >
              <div className="flex items-start gap-2.5">
                <span style={{ color: AMBER }} className="mt-0.5">
                  <CheckIcon size={14} />
                </span>
                <div>
                  <p className="font-heading text-cc-heading text-h6 font-semibold">
                    {title}
                  </p>
                  <p className="text-cc-ink-dim mt-2 text-[0.92rem]">{body}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      <div className="bg-cc-card-border my-16 h-px w-full" />

      {/* ---------------------------------------------------------------- */}
      {/* PROOF — source-generated dispatch                               */}
      {/* ---------------------------------------------------------------- */}
      <section className="grid gap-10 lg:grid-cols-2 lg:items-center lg:gap-16">
        <div>
          <span className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.3em] uppercase">
            under the hood
          </span>
          <h2 className="font-heading text-cc-heading text-h3 mt-5 leading-tight font-semibold text-balance">
            Source-generated dispatch on .NET.
          </h2>
          <p className="text-cc-ink mt-5 max-w-xl">
            Mocha discovers your handlers and sagas with a Roslyn source
            generator at build time, then wires dispatch with no runtime
            reflection. The generated registration is AOT-friendly and fast to
            cold-start.
          </p>
          <ul className="mt-6 space-y-3">
            {[
              "Zero-reflection handler and saga dispatch",
              "AOT-friendly, trims cleanly, fast cold starts",
              "The same model from a single process to many services",
            ].map((line) => (
              <li key={line} className="text-cc-ink flex items-start gap-3">
                <span style={{ color: AMBER }} className="mt-0.5 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{line}</span>
              </li>
            ))}
          </ul>
        </div>
        <div className="lg:pl-2">
          <CodePane
            file="Generated/MochaDispatch.g.cs"
            lines={[
              <>{cm("// generated at build — no reflection")}</>,
              <>
                {kw("internal")} {kw("static")} {kw("void")} Register(
              </>,
              <>{"  "}IMochaRegistry r)</>,
              <>{"{"}</>,
              <>
                {"  "}r.{at("Handler")}
                {"<"}
                {ty("CreateReview")},
              </>,
              <>
                {"    "}
                {ty("CreateReviewHandler")}
                {">"}();
              </>,
              <>
                {"  "}r.{at("Saga")}
                {"<"}
                {ty("ReviewSaga")}
                {">"}();
              </>,
              <>
                {"  "}
                {cm("// Draft -> Checked -> Published")}
              </>,
              <>{"}"}</>,
            ]}
          />
        </div>
      </section>

      {/* ---------------------------------------------------------------- */}
      {/* CLOSING CTA                                                      */}
      {/* ---------------------------------------------------------------- */}
      <section className="mt-24">
        <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border px-8 py-14 text-center backdrop-blur-sm sm:px-12">
          <div
            aria-hidden
            className="pointer-events-none absolute inset-x-0 top-0 h-px"
            style={{
              background: `linear-gradient(90deg, transparent, ${AMBER}, ${CORAL}, transparent)`,
            }}
          />
          <span className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.3em] uppercase">
            Mocha
          </span>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-tight font-semibold text-balance">
            Answer fast. Let the work keep moving.
          </h2>
          <p className="lead text-cc-ink-dim mx-auto mt-6 max-w-xl">
            One framework for the mediator and the bus, with reliable delivery,
            sagas, and a span for every hop.
          </p>
          <div className="mt-9 flex flex-wrap justify-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
          </div>
        </div>
      </section>
    </article>
  );
}
