import Link from "next/link";
import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Mocha messaging section, take v2: "In-process / across services".
 *
 * A split composition. One shared origin (`CreateReview` / handler) forks into
 * two dispatch paths drawn with identical node styling: a left lane where a
 * command reaches its handler in the same process, and a right lane where an
 * event reaches two consumers on other services across a dashed transport
 * boundary. The same handler model, two distances. Two full-width rows below
 * carry the remaining beats: a saga state strip and an exactly-once
 * outbox -> inbox mark. Every beat is on screen at once; no tabs or steppers.
 *
 * Server component, no hooks. Dark cc-* palette: teal is the only accent, amber
 * marks the single in-flight message, green marks the returned request. All svg
 * geometry is self-contained; there are no shared svg ids to collide.
 */
export function MochaSectionV2() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* Frame: the section heading block. */}
        <div className="max-w-3xl">
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Messaging
          </span>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 leading-[1.1] font-semibold text-balance">
            Messaging, mediator, and sagas.
          </h2>
          <p className="text-cc-ink mt-5 max-w-3xl text-base text-pretty sm:text-lg">
            Mocha is the messaging side of the platform: an in-process mediator
            for commands and queries, a bus for events across services, sagas
            for long-running work, and exactly-once delivery over RabbitMQ,
            Kafka, Postgres, or Azure.
          </p>
          <Link
            href="/platform/workflows"
            className="text-cc-accent hover:text-cc-accent-hover mt-5 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* Split: one shared origin forking into the two dispatch paths. */}
        <div className="mt-12 sm:mt-16">
          {/* Beat 1: the shared origin at the top of the fork. */}
          <div className="mx-auto max-w-xl text-center">
            <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.12em] uppercase">
              Background work
            </span>
            <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.1] font-semibold text-balance">
              Return now, process after.
            </h3>
            <p className="text-cc-ink-dim mx-auto mt-3 max-w-md text-sm/relaxed text-pretty">
              Hand slow or fan-out work to a handler and return the response.
              The request stays fast; the rest runs on its own.
            </p>
            <div className="mt-5 flex justify-center">
              <div className="border-cc-accent/40 bg-cc-surface inline-flex flex-col items-start gap-2 rounded-xl border px-4 py-3 text-left">
                <div className="flex w-full items-center justify-between gap-5">
                  <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.14em] uppercase">
                    handler
                  </span>
                  <span className="border-cc-status-healthy/40 text-cc-status-healthy rounded-full border px-2 py-0.5 font-mono text-[0.55rem]">
                    200 returns now
                  </span>
                </div>
                <span className="text-cc-accent font-mono text-sm">
                  CreateReview
                </span>
              </div>
            </div>
          </div>

          {/* Fork connector: left branch solid (in-process), right dashed
              (across services). Stretched to the lane region on large screens. */}
          <svg
            viewBox="0 0 1000 80"
            fill="none"
            aria-hidden="true"
            preserveAspectRatio="none"
            className="hidden h-10 w-full lg:block"
          >
            <circle cx={500} cy={5} r={3} fill={C.accent} />
            <path
              d="M500 5 C 500 46 250 38 250 80"
              stroke={C.line}
              strokeWidth={1.5}
              vectorEffect="non-scaling-stroke"
            />
            <path
              d="M500 5 C 500 46 750 38 750 80"
              stroke={C.line}
              strokeWidth={1.5}
              strokeDasharray="4 4"
              vectorEffect="non-scaling-stroke"
            />
          </svg>

          {/* The two lanes, identical node styling, two distances. */}
          <div className="mt-6 grid grid-cols-1 gap-6 lg:mt-0 lg:grid-cols-2">
            <LaneCard
              eyebrow="Mediator"
              headline="Commands and queries, in-process."
              blurb="Dispatch through the mediator and one handler interface. CQRS without the registration wiring."
              laneLabel="Mediator - in-process"
              caption="same process - synchronous"
            >
              <MediatorDiagram />
            </LaneCard>

            <LaneCard
              eyebrow="Bus"
              headline="Events, across services."
              blurb="Publish an event and the same handlers run on other services over a bus. The model does not change when work leaves the process."
              laneLabel="Bus - across services"
              caption="other services - async"
            >
              <BusDiagram />
            </LaneCard>
          </div>
        </div>

        {/* The remaining beats, two compact full-width rows. */}
        <div className="mt-6 space-y-6">
          <BeatRow
            eyebrow="Sagas"
            headline="Long-running processes."
            blurb="A saga is a state machine that drives a process across many messages and resumes after a restart."
          >
            <SagaStrip />
          </BeatRow>

          <BeatRow
            eyebrow="Delivery"
            headline="Exactly-once."
            blurb="A transactional outbox and idempotent inbox give exactly-once handling over RabbitMQ, Kafka, Postgres, or Azure."
          >
            <DeliveryBlock />
          </BeatRow>
        </div>
      </RevealOnScroll>
    </section>
  );
}

/** Monospace stack used for all svg labels, matching the cards' font-mono. */
const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette: dark surfaces, neutral ink, teal accent, amber in-flight. */
const C = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  faint: "rgba(245, 241, 234, 0.42)",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  line: "rgba(245, 241, 234, 0.22)",
} as const;

const SVG_STYLE = {
  display: "block",
  overflow: "visible",
  fontFamily: MONO,
} as const;

interface LaneNodeProps {
  readonly x: number;
  readonly y: number;
  readonly w?: number;
  readonly kind: string;
  readonly name: string;
  readonly service: string;
}

/**
 * Destination node, shared by both lanes (and the delivery mark) so the handler
 * on the left reads identically to the consumers on the right.
 */
function LaneNode({ x, y, w = 116, kind, name, service }: LaneNodeProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={42}
        rx={10}
        fill={C.surface}
        fillOpacity={0.55}
        stroke={C.cardBorder}
        strokeWidth={1}
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 12}
        y={y + 14}
        fontSize={6}
        letterSpacing={1.3}
        fill={C.navLabel}
      >
        {kind}
      </text>
      <text x={x + 12} y={y + 27} fontSize={9.5} fill={C.heading}>
        {name}
      </text>
      <text x={x + 12} y={y + 37} fontSize={6.5} fill={C.faint}>
        {service}
      </text>
    </g>
  );
}

interface MessagePillProps {
  readonly x: number;
  readonly y: number;
  readonly kind: string;
  readonly name: string;
}

/** Origin message (the command or the event), teal-edged and identical per lane. */
function MessagePill({ x, y, kind, name }: MessagePillProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={96}
        height={40}
        rx={10}
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity={0.5}
        strokeWidth={1.25}
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 12}
        y={y + 15}
        fontSize={6}
        letterSpacing={1.3}
        fill={C.navLabel}
      >
        {kind}
      </text>
      <text x={x + 12} y={y + 29} fontSize={9.5} fill={C.accent}>
        {name}
      </text>
    </g>
  );
}

/** Left lane: a command reaches its handler in the same process, synchronously. */
function MediatorDiagram() {
  return (
    <svg
      viewBox="0 0 300 132"
      width="100%"
      style={SVG_STYLE}
      aria-hidden="true"
    >
      <rect
        x={6}
        y={40}
        width={288}
        height={64}
        rx={14}
        fill="none"
        stroke={C.inkFaint}
        strokeWidth={1}
        strokeDasharray="3 5"
        vectorEffect="non-scaling-stroke"
      />
      <text x={18} y={34} fontSize={6} letterSpacing={1.4} fill={C.navLabel}>
        PROCESS
      </text>

      <line
        x1={108}
        y1={72}
        x2={176}
        y2={72}
        stroke={C.accent}
        strokeOpacity={0.5}
        strokeWidth={1.5}
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />
      <circle cx={176} cy={72} r={2.5} fill={C.accent} />

      <MessagePill x={12} y={52} kind="COMMAND" name="CreateReview" />
      <LaneNode
        x={176}
        y={51}
        kind="HANDLER"
        name="ReviewHandler"
        service="same process"
      />
    </svg>
  );
}

/** Right lane: an event reaches two consumers on other services over the bus. */
function BusDiagram() {
  return (
    <svg
      viewBox="0 0 300 132"
      width="100%"
      style={SVG_STYLE}
      aria-hidden="true"
    >
      <line
        x1={150}
        y1={12}
        x2={150}
        y2={122}
        stroke={C.inkFaint}
        strokeWidth={1}
        strokeDasharray="3 5"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={150}
        y={8}
        textAnchor="middle"
        fontSize={6}
        letterSpacing={1.4}
        fill={C.navLabel}
      >
        TRANSPORT
      </text>

      <path
        d="M104 72 C 140 72 150 37 178 37"
        fill="none"
        stroke={C.accent}
        strokeOpacity={0.3}
        strokeWidth={1}
        strokeDasharray="4 4"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />
      <path
        d="M104 72 C 140 72 150 107 178 107"
        fill="none"
        stroke={C.accent}
        strokeOpacity={0.3}
        strokeWidth={1}
        strokeDasharray="4 4"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />

      <MessagePill x={8} y={52} kind="EVENT" name="ReviewCreated" />
      <LaneNode
        x={178}
        y={16}
        kind="CONSUMER"
        name="SearchIndexer"
        service="search-svc"
      />
      <LaneNode
        x={178}
        y={86}
        kind="CONSUMER"
        name="NotifyAuthor"
        service="email-svc"
      />

      <circle cx={104} cy={72} r={2.5} fill={C.accent} />
      <circle cx={178} cy={37} r={2} fill={C.accent} />
      <circle cx={178} cy={107} r={2} fill={C.accent} />

      {/* One copy of the event in flight on the top hop. */}
      <rect
        x={108}
        y={44}
        width={64}
        height={16}
        rx={8}
        fill={C.surface}
        stroke={C.amber}
        strokeOpacity={0.9}
        strokeWidth={1}
        vectorEffect="non-scaling-stroke"
      />
      <text x={140} y={55} textAnchor="middle" fontSize={7} fill={C.amber}>
        ReviewCreated
      </text>
    </svg>
  );
}

/** Delivery mark: a transactional outbox feeds an idempotent inbox, applied once. */
function DeliveryDiagram() {
  return (
    <svg viewBox="0 0 280 96" width="100%" style={SVG_STYLE} aria-hidden="true">
      <line
        x1={116}
        y1={48}
        x2={164}
        y2={48}
        stroke={C.accent}
        strokeOpacity={0.5}
        strokeWidth={1.5}
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />
      <circle cx={164} cy={48} r={2.5} fill={C.accent} />

      <LaneNode
        x={8}
        y={27}
        w={108}
        kind="OUTBOX"
        name="transactional"
        service="commit + send"
      />
      <LaneNode
        x={164}
        y={27}
        w={108}
        kind="INBOX"
        name="idempotent"
        service="dedupe + apply"
      />

      <rect
        x={108}
        y={6}
        width={64}
        height={18}
        rx={9}
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity={0.5}
        strokeWidth={1}
        vectorEffect="non-scaling-stroke"
      />
      <text x={140} y={18} textAnchor="middle" fontSize={7} fill={C.accent}>
        exactly-once
      </text>
    </svg>
  );
}

/** Beat 5 body: the delivery mark plus the transports it runs over. */
function DeliveryBlock() {
  return (
    <div>
      <DeliveryDiagram />
      <p className="text-cc-ink-dim mt-4 font-mono text-[0.62rem] tracking-[0.06em]">
        RabbitMQ &middot; Kafka &middot; Postgres &middot; Azure
      </p>
    </div>
  );
}

const SAGA_STATES: readonly {
  readonly name: string;
  readonly state: "done" | "active" | "next";
}[] = [
  { name: "Draft", state: "done" },
  { name: "Checked", state: "active" },
  { name: "Published", state: "next" },
];

/** Beat 4 body: the saga state strip that resumes after a restart. */
function SagaStrip() {
  return (
    <div>
      <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.12em] uppercase">
        Saga state
      </p>
      <div className="mt-3 flex flex-wrap items-center gap-1.5">
        {SAGA_STATES.map((s, i) => (
          <span key={s.name} className="flex items-center">
            {i > 0 && (
              <span
                aria-hidden="true"
                className="text-cc-ink-faint px-1 text-[0.7rem]"
              >
                &rarr;
              </span>
            )}
            <span
              className={[
                "rounded-md border px-2.5 py-1 font-mono text-[0.7rem]",
                s.state === "active"
                  ? "border-cc-accent/60 text-cc-accent bg-cc-accent/5"
                  : s.state === "done"
                    ? "border-cc-card-border text-cc-ink-dim"
                    : "border-cc-ink-faint text-cc-ink-dim border-dashed",
              ].join(" ")}
            >
              {s.name}
            </span>
          </span>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-3 font-mono text-[0.62rem]">
        across many messages - resumes after a restart
      </p>
    </div>
  );
}

interface LaneCardProps {
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly laneLabel: string;
  readonly caption: string;
  readonly children: ReactNode;
}

/** One lane of the split: a beat's copy above its dispatch diagram. */
function LaneCard({
  eyebrow,
  headline,
  blurb,
  laneLabel,
  caption,
  children,
}: LaneCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover rounded-2xl border p-6 backdrop-blur-sm transition-colors sm:p-7">
      <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.12em] uppercase">
        {eyebrow}
      </span>
      <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.1] font-semibold text-balance">
        {headline}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm/relaxed text-pretty">
        {blurb}
      </p>

      <div className="border-cc-card-border bg-cc-surface/40 mt-5 rounded-xl border p-4">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.12em] uppercase">
          {laneLabel}
        </p>
        <div className="mt-3">{children}</div>
        <p className="text-cc-ink-dim mt-3 font-mono text-[0.62rem]">
          {caption}
        </p>
      </div>
    </div>
  );
}

interface BeatRowProps {
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly children: ReactNode;
}

/** A compact full-width beat: copy on one side, its diagram on the other. */
function BeatRow({ eyebrow, headline, blurb, children }: BeatRowProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover grid grid-cols-1 items-center gap-6 rounded-2xl border p-6 backdrop-blur-sm transition-colors sm:p-7 lg:grid-cols-2 lg:gap-10">
      <div>
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.12em] uppercase">
          {eyebrow}
        </span>
        <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.1] font-semibold text-balance">
          {headline}
        </h3>
        <p className="text-cc-ink-dim mt-3 text-sm/relaxed text-pretty">
          {blurb}
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-surface/40 rounded-xl border p-5">
        {children}
      </div>
    </div>
  );
}
