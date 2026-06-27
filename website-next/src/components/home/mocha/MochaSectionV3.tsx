import Link from "next/link";
import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Mocha messaging section, take "One message, end to end".
 *
 * One message is followed along a single horizontal path through the
 * reliability mechanics: publish -> outbox (data write and message committed in
 * the same DB transaction) -> transport (RabbitMQ) -> inbox (dedupe, a duplicate
 * is skipped) -> handler. That exactly-once delivery path is the spine. Beneath
 * it, four short annotations attach the other beats to points on the same path:
 * background work returns at publish, the mediator dispatches it in-process, the
 * bus carries it across services at the transport hop, and a saga may advance on
 * it at the handler.
 *
 * All-visible: every beat is on screen at once, no tabs or steppers. Dark cc-*
 * palette, teal accent, amber reserved for the single in-flight message, green
 * for the returned request. Diagrams are inline svg with no shared ids.
 */
export function MochaSectionV3() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <header className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Messaging
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Messaging, mediator, and sagas.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Mocha is the messaging side of the platform: an in-process mediator
            for commands and queries, a bus for events across services, sagas
            for long-running work, and exactly-once delivery over RabbitMQ,
            Kafka, Postgres, or Azure.
          </p>
          <Link
            href="/platform/workflows"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </header>
      </RevealOnScroll>

      <RevealOnScroll className="mt-12 sm:mt-16">
        {/* The spine: one message followed from publish to handler. */}
        <div className="relative">
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0 overflow-hidden rounded-3xl"
          >
            <div
              className="absolute top-1/2 left-0 h-64 w-2/3 -translate-y-1/2"
              style={{
                background:
                  "radial-gradient(ellipse 60% 70% at 14% 50%, rgba(94,234,212,0.07), transparent 72%)",
              }}
            />
          </div>

          <div className="relative">
            <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
              <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
                Delivery
              </p>
              <p className="text-cc-ink-dim font-mono text-[0.65rem] tracking-[0.12em] uppercase">
                one message, end to end
              </p>
            </div>
            <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.1] font-semibold">
              Exactly-once.
            </h3>
            <p className="text-cc-ink mt-3 max-w-2xl text-base text-pretty">
              A transactional outbox and idempotent inbox give exactly-once
              handling over RabbitMQ, Kafka, Postgres, or Azure.
            </p>

            <ol className="mt-7 flex flex-col lg:flex-row lg:items-stretch">
              <li className="flex lg:flex-1">
                <StopCard step="01" label="publish">
                  <PublishBody />
                </StopCard>
              </li>
              <Connector />
              <li className="flex lg:flex-1">
                <StopCard step="02" label="outbox">
                  <OutboxBody />
                </StopCard>
              </li>
              <Connector inFlight />
              <li className="flex lg:flex-1">
                <StopCard step="03" label="transport" kicker={<InFlightPill />}>
                  <TransportBody />
                </StopCard>
              </li>
              <Connector />
              <li className="flex lg:flex-1">
                <StopCard step="04" label="inbox">
                  <InboxBody />
                </StopCard>
              </li>
              <Connector />
              <li className="flex lg:flex-1">
                <StopCard step="05" label="handler">
                  <HandlerBody />
                </StopCard>
              </li>
            </ol>

            <p className="text-cc-ink-dim mt-5 text-center font-mono text-[0.65rem] tracking-[0.12em]">
              publish &rarr; outbox &rarr; RabbitMQ &rarr; inbox &rarr; handler
            </p>
          </div>
        </div>

        {/* The other four beats, each attached to a point on the path above. */}
        <div className="mt-12 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {BEATS.map((beat) => (
            <article
              key={beat.key}
              className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-2xl border p-5 transition-colors"
            >
              <div className="flex items-center justify-between gap-2">
                <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                  {beat.eyebrow}
                </span>
                <span className="border-cc-card-border text-cc-ink-dim inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[0.55rem]">
                  <span
                    aria-hidden="true"
                    className="bg-cc-accent size-1 rounded-full"
                  />
                  {beat.anchor}
                </span>
              </div>
              <beat.Glyph className="text-cc-accent mt-4 h-7 w-10" />
              <h4 className="font-heading text-cc-heading text-h6 mt-4 leading-[1.2] font-semibold text-balance">
                {beat.headline}
              </h4>
              <p className="text-cc-ink-dim mt-2 text-sm text-pretty">
                {beat.blurb}
              </p>
            </article>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}

/** One node on the delivery path. Cards equalize height across the lg row. */
function StopCard({
  step,
  label,
  kicker,
  children,
}: {
  readonly step: string;
  readonly label: string;
  readonly kicker?: ReactNode;
  readonly children: ReactNode;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex w-full flex-col rounded-2xl border p-4 transition-colors">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-1.5">
          <span className="text-cc-nav-label/60 font-mono text-[0.6rem]">
            {step}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
            {label}
          </span>
        </div>
        {kicker}
      </div>
      <div className="mt-3 flex-1">{children}</div>
    </div>
  );
}

/** Connector between two stops: a thin line that turns with the layout. */
function Connector({ inFlight = false }: { readonly inFlight?: boolean }) {
  return (
    <li
      aria-hidden="true"
      className="flex shrink-0 items-center justify-center py-1.5 lg:px-1.5 lg:py-0"
    >
      {/* vertical, mobile */}
      <svg
        className="lg:hidden"
        width="16"
        height="30"
        viewBox="0 0 16 30"
        fill="none"
      >
        <line
          x1="8"
          y1="0"
          x2="8"
          y2="24"
          stroke={inFlight ? "rgba(251,191,36,0.5)" : "rgba(245,241,234,0.18)"}
          strokeWidth="1"
        />
        <path
          d="M4.5 20 L8 25 L11.5 20"
          stroke="rgba(245,241,234,0.32)"
          strokeWidth="1"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
      {/* horizontal, desktop */}
      <svg
        className="hidden lg:block"
        width="40"
        height="16"
        viewBox="0 0 40 16"
        fill="none"
      >
        <line
          x1="0"
          y1="8"
          x2="34"
          y2="8"
          stroke={inFlight ? "rgba(251,191,36,0.5)" : "rgba(245,241,234,0.18)"}
          strokeWidth="1"
        />
        <path
          d="M30 4 L35 8 L30 12"
          stroke="rgba(245,241,234,0.32)"
          strokeWidth="1"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </li>
  );
}

/** The single in-flight message marker, amber, on the transport stop. */
function InFlightPill() {
  return (
    <span className="border-cc-status-investigating/40 bg-cc-status-investigating/10 text-cc-status-investigating inline-flex items-center gap-1 rounded-full border px-1.5 py-0.5 font-mono text-[0.55rem]">
      <span
        aria-hidden="true"
        className="bg-cc-status-investigating size-1 rounded-full"
      />
      in flight
    </span>
  );
}

/** Stop 1: the message is published and the request returns. */
function PublishBody() {
  return (
    <div className="space-y-2.5">
      <span className="border-cc-accent/35 bg-cc-accent/[0.06] text-cc-accent inline-block rounded-md border px-2 py-1 font-mono text-[0.7rem]">
        OrderPlaced
      </span>
      <div className="flex items-center gap-1.5">
        <span className="border-cc-status-healthy/40 bg-cc-status-healthy/10 text-cc-status-healthy inline-flex items-center rounded-md border px-1.5 py-0.5 font-mono text-[0.6rem]">
          200
        </span>
        <span className="text-cc-ink-dim text-[0.7rem]">request returns</span>
      </div>
    </div>
  );
}

/** Stop 2: data write and message committed in one DB transaction. */
function OutboxBody() {
  return (
    <div className="flex gap-2">
      <div className="border-cc-accent/40 w-1.5 shrink-0 rounded-l-md border-y border-l" />
      <div className="min-w-0 flex-1">
        <p className="text-cc-accent font-mono text-[0.5rem] tracking-[0.1em] uppercase">
          same transaction
        </p>
        <div className="border-cc-card-border divide-cc-card-border mt-1.5 divide-y rounded-md border">
          <OutboxRow tag="data" value="INSERT order" />
          <OutboxRow tag="msg" value="OrderPlaced" />
        </div>
      </div>
    </div>
  );
}

function OutboxRow({
  tag,
  value,
}: {
  readonly tag: string;
  readonly value: string;
}) {
  return (
    <div className="flex items-center gap-2 px-2.5 py-1.5">
      <span className="text-cc-nav-label w-8 shrink-0 font-mono text-[0.5rem] tracking-[0.08em] uppercase">
        {tag}
      </span>
      <span className="text-cc-ink truncate font-mono text-[0.7rem]">
        {value}
      </span>
    </div>
  );
}

/** Stop 3: the transport hop. One concrete broker, the others available. */
function TransportBody() {
  return (
    <div className="space-y-2">
      <span className="border-cc-card-border bg-cc-surface text-cc-ink inline-block rounded-md border px-2 py-1 font-mono text-[0.7rem]">
        RabbitMQ
      </span>
      <p className="text-cc-nav-label font-mono text-[0.55rem]">
        Kafka &middot; Postgres &middot; Azure
      </p>
      <div className="text-cc-ink-dim flex items-center gap-1.5 text-[0.6rem]">
        <span
          aria-hidden="true"
          className="border-cc-ink-faint inline-block h-px w-4 border-t border-dashed"
        />
        crosses services
      </div>
    </div>
  );
}

/** Stop 4: the inbox handles once and skips the duplicate copy. */
function InboxBody() {
  return (
    <div className="space-y-2">
      <div className="flex items-center gap-1.5">
        <CheckGlyph className="text-cc-accent size-3 shrink-0" />
        <span className="text-cc-ink font-mono text-[0.7rem]">OrderPlaced</span>
        <span className="text-cc-ink-dim text-[0.6rem]">handled</span>
      </div>
      <div className="flex items-center gap-1.5 opacity-55">
        <CrossGlyph className="text-cc-ink-faint size-3 shrink-0" />
        <span className="text-cc-ink-dim font-mono text-[0.7rem] line-through">
          OrderPlaced
        </span>
        <span className="text-cc-ink-dim text-[0.6rem]">duplicate skipped</span>
      </div>
    </div>
  );
}

/** Stop 5: the handler runs once and the saga may advance. */
function HandlerBody() {
  return (
    <div className="space-y-2">
      <span className="border-cc-card-border bg-cc-surface text-cc-ink inline-block rounded-md border px-2 py-1 font-mono text-[0.7rem]">
        OrderPlacedHandler
      </span>
      <p className="text-cc-ink-dim text-[0.65rem]">runs exactly once</p>
      <div className="text-cc-ink-dim flex items-center gap-1.5 text-[0.6rem]">
        <SagaGlyph className="text-cc-accent/80 h-2 w-8 shrink-0" />
        saga may advance
      </div>
    </div>
  );
}

interface GlyphProps {
  readonly className?: string;
}

function CheckGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 12 12"
      fill="none"
      className={className}
      aria-hidden="true"
    >
      <path
        d="M2.5 6.5 L5 9 L9.5 3.5"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function CrossGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 12 12"
      fill="none"
      className={className}
      aria-hidden="true"
    >
      <path
        d="M3.5 3.5 L8.5 8.5 M8.5 3.5 L3.5 8.5"
        stroke="currentColor"
        strokeWidth="1.2"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** Three chained states: a compact state-machine mark for the saga beat. */
function SagaGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 40 10"
      fill="none"
      className={className}
      aria-hidden="true"
    >
      <rect
        x="1"
        y="1.5"
        width="9"
        height="7"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.55"
      />
      <path
        d="M10 5 H15"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.55"
      />
      <rect
        x="15"
        y="1.5"
        width="9"
        height="7"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1"
      />
      <path
        d="M24 5 H29"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.55"
      />
      <rect
        x="29"
        y="1.5"
        width="9"
        height="7"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.55"
      />
    </svg>
  );
}

/** Beat 1: a returned response, the rest of the work continuing after it. */
function BackgroundGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 40 28"
      fill="none"
      className={className}
      aria-hidden="true"
    >
      <circle cx="11" cy="14" r="2.5" fill="currentColor" />
      <path d="M11 14 H4" stroke="currentColor" strokeWidth="1" />
      <path
        d="M6.5 11.5 L4 14 L6.5 16.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M11 14 H36"
        stroke="currentColor"
        strokeWidth="1"
        strokeDasharray="3 3"
        strokeOpacity="0.7"
      />
      <path
        d="M33.5 11.5 L36 14 L33.5 16.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Beat 2: sender -> mediator -> handler, all inside one process. */
function MediatorGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 40 28"
      fill="none"
      className={className}
      aria-hidden="true"
    >
      <rect
        x="2"
        y="9"
        width="8"
        height="10"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.5"
      />
      <circle cx="20" cy="14" r="3" stroke="currentColor" strokeWidth="1" />
      <rect
        x="30"
        y="9"
        width="8"
        height="10"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.5"
      />
      <path d="M10 14 H16.5" stroke="currentColor" strokeWidth="1" />
      <path
        d="M14.5 12 L16.5 14 L14.5 16"
        stroke="currentColor"
        strokeWidth="1"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path d="M23.5 14 H30" stroke="currentColor" strokeWidth="1" />
      <path
        d="M28 12 L30 14 L28 16"
        stroke="currentColor"
        strokeWidth="1"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Beat 3: one event crossing a service boundary to handlers on both sides. */
function BusGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 40 28"
      fill="none"
      className={className}
      aria-hidden="true"
    >
      <circle cx="6" cy="14" r="2.5" fill="currentColor" />
      <line
        x1="20"
        y1="3"
        x2="20"
        y2="25"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.3"
        strokeDasharray="2 3"
      />
      <path
        d="M6 14 C 16 14 14 8 33 8"
        stroke="currentColor"
        strokeWidth="1"
        strokeDasharray="3 3"
        strokeOpacity="0.7"
      />
      <path
        d="M6 14 C 16 14 14 20 33 20"
        stroke="currentColor"
        strokeWidth="1"
        strokeDasharray="3 3"
        strokeOpacity="0.7"
      />
      <rect
        x="33"
        y="5"
        width="6"
        height="6"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.6"
      />
      <rect
        x="33"
        y="17"
        width="6"
        height="6"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1"
        strokeOpacity="0.6"
      />
    </svg>
  );
}

interface Beat {
  readonly key: string;
  readonly eyebrow: string;
  readonly anchor: string;
  readonly headline: string;
  readonly blurb: string;
  readonly Glyph: (props: GlyphProps) => ReactNode;
}

/** The four non-spine beats, ordered to follow the path left to right. */
const BEATS: readonly Beat[] = [
  {
    key: "background",
    eyebrow: "Background work",
    anchor: "publish",
    headline: "Return now, process after.",
    blurb:
      "Hand slow or fan-out work to a handler and return the response. The request stays fast; the rest runs on its own.",
    Glyph: BackgroundGlyph,
  },
  {
    key: "mediator",
    eyebrow: "Mediator",
    anchor: "in-process",
    headline: "Commands and queries, in-process.",
    blurb:
      "Dispatch through the mediator and one handler interface. CQRS without the registration wiring.",
    Glyph: MediatorGlyph,
  },
  {
    key: "bus",
    eyebrow: "Bus",
    anchor: "transport",
    headline: "Events, across services.",
    blurb:
      "Publish an event and the same handlers run on other services over a bus. The model does not change when work leaves the process.",
    Glyph: BusGlyph,
  },
  {
    key: "sagas",
    eyebrow: "Sagas",
    anchor: "handler",
    headline: "Long-running processes.",
    blurb:
      "A saga is a state machine that drives a process across many messages and resumes after a restart.",
    Glyph: SagaGlyph,
  },
];
