import Link from "next/link";
import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Mocha messaging section, take v5 "At a glance".
 *
 * One composed system panel that shows the whole Mocha system at once, with no
 * tabs, stepper, or reveal. The frame heading block sits above; below it a
 * single framed diagram lays out the five parts as labeled regions that read
 * top to bottom: an in-process band (mediator + background work), the bus
 * fanning one event across a service boundary to handlers on two other
 * services, and a delivery band (a saga advancing + an outbox/inbox tagged
 * exactly-once over four transports). Every svg id is prefixed "v5-".
 */
export function MochaSectionV5() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* frame: the section heading block */}
        <div className="max-w-3xl">
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
            Open workflows
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* one composed system panel: all five parts visible at once */}
        <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover mt-10 rounded-3xl border p-5 backdrop-blur-sm transition-colors sm:mt-12 sm:p-8">
          <header className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
              Mocha &middot; one system
            </p>
            <div className="flex items-center gap-4">
              <LegendDot color={SVG.accent} label="in-process" />
              <LegendDot color={SVG.amber} label="in-flight" />
            </div>
          </header>

          <div className="mt-7 space-y-5 sm:mt-8 sm:space-y-6">
            {/* in-process band: mediator + background work */}
            <div className="grid gap-4 sm:grid-cols-2 sm:gap-5">
              <Region
                label="Mediator"
                title="Commands and queries, in-process."
              >
                <div className="flex flex-wrap items-center gap-1">
                  <Chip tone="accent">command</Chip>
                  <Arrow />
                  <Chip>mediator</Chip>
                  <Arrow />
                  <Chip>handler</Chip>
                </div>
                <Caption>
                  Dispatch through the mediator and one handler interface. CQRS
                  without the registration wiring.
                </Caption>
              </Region>

              <Region
                label="Background work"
                title="Return now, process after."
              >
                <div className="flex flex-col gap-2">
                  <div className="flex flex-wrap items-center gap-1">
                    <Chip>request</Chip>
                    <Arrow />
                    <Chip tone="good">200 returns now</Chip>
                  </div>
                  <div className="flex flex-wrap items-center gap-1">
                    <Chip tone="accent">handler</Chip>
                    <Arrow />
                    <Chip tone="pending">runs after</Chip>
                  </div>
                </div>
                <Caption>
                  Hand slow or fan-out work to a handler and return the
                  response. The request stays fast; the rest runs on its own.
                </Caption>
              </Region>
            </div>

            <FlowSeam label="publishes an event" />

            {/* bus band: one event, across a service boundary, to two services */}
            <Region label="Bus" title="Events, across services.">
              <div className="mx-auto max-w-2xl">
                <BusFanOut />
              </div>
              <Caption className="mx-auto max-w-2xl">
                Publish an event and the same handlers run on other services
                over a bus. The model does not change when work leaves the
                process.
              </Caption>
            </Region>

            <FlowSeam label="exactly-once delivery" />

            {/* delivery band: saga + outbox/inbox over four transports */}
            <div className="grid gap-4 sm:grid-cols-2 sm:gap-5">
              <Region label="Sagas" title="Long-running processes.">
                <div className="flex flex-wrap items-center gap-1">
                  <Chip tone="muted">Draft</Chip>
                  <Arrow />
                  <Chip tone="accent">Checked</Chip>
                  <Arrow />
                  <Chip tone="pending">Published</Chip>
                </div>
                <Caption>
                  A saga is a state machine that drives a process across many
                  messages and resumes after a restart.
                </Caption>
              </Region>

              <Region label="Delivery" title="Exactly-once.">
                <OutboxInbox />
                <div className="mt-3 flex flex-wrap gap-1.5">
                  {TRANSPORTS.map((transport) => (
                    <span
                      key={transport}
                      className="border-cc-card-border text-cc-ink-dim bg-cc-surface rounded-full border px-2.5 py-1 font-mono text-[0.6rem] tracking-[0.06em]"
                    >
                      {transport}
                    </span>
                  ))}
                </div>
                <Caption>
                  A transactional outbox and idempotent inbox give exactly-once
                  handling over RabbitMQ, Kafka, Postgres, or Azure.
                </Caption>
              </Region>
            </div>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}

const TRANSPORTS = ["RabbitMQ", "Kafka", "Postgres", "Azure"] as const;

/** A labeled sub-region inside the system panel. */
function Region({
  label,
  title,
  children,
}: {
  readonly label: string;
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <div className="border-cc-card-border bg-cc-surface/40 rounded-2xl border p-4 sm:p-5">
      <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
        {label}
      </p>
      <h3 className="font-heading text-cc-heading text-h6 mt-1.5 leading-[1.15] font-semibold text-balance">
        {title}
      </h3>
      <div className="mt-3.5">{children}</div>
    </div>
  );
}

/** Secondary one-line description under a region's diagram. */
function Caption({
  children,
  className,
}: {
  readonly children: ReactNode;
  readonly className?: string;
}) {
  return (
    <p
      className={["text-cc-ink-dim mt-3 text-sm text-pretty", className ?? ""]
        .filter(Boolean)
        .join(" ")}
    >
      {children}
    </p>
  );
}

/** Mono flow chip. Tones map to the diagram's calm teal / status palette. */
function Chip({
  children,
  tone = "default",
}: {
  readonly children: ReactNode;
  readonly tone?: "default" | "accent" | "good" | "pending" | "muted";
}) {
  const tones = {
    default: "border-cc-card-border text-cc-ink bg-cc-surface",
    accent: "border-cc-accent/60 text-cc-accent bg-cc-surface",
    good: "border-[#34d399]/60 text-[#34d399] bg-cc-surface",
    pending: "border-cc-ink-faint text-cc-ink-dim bg-cc-surface border-dashed",
    muted: "border-cc-card-border text-cc-ink-dim bg-cc-surface",
  } as const;

  return (
    <span
      className={[
        "rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        tones[tone],
      ].join(" ")}
    >
      {children}
    </span>
  );
}

function Arrow() {
  return (
    <span aria-hidden="true" className="text-cc-ink-faint px-0.5 text-sm">
      &rarr;
    </span>
  );
}

/** A thin labeled seam between bands, signalling downward flow. */
function FlowSeam({ label }: { readonly label: string }) {
  return (
    <div className="flex items-center gap-3" aria-hidden="true">
      <span className="bg-cc-card-border h-px flex-1" />
      <span className="text-cc-nav-label inline-flex items-center gap-1.5 font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        {label}
        <span className="text-cc-ink-faint not-italic">&darr;</span>
      </span>
      <span className="bg-cc-card-border h-px flex-1" />
    </div>
  );
}

function LegendDot({
  color,
  label,
}: {
  readonly color: string;
  readonly label: string;
}) {
  return (
    <span className="text-cc-ink-dim inline-flex items-center gap-1.5 font-mono text-[0.55rem] tracking-[0.08em] uppercase">
      <span
        aria-hidden="true"
        className="inline-block size-2 rounded-full"
        style={{ backgroundColor: color }}
      />
      {label}
    </span>
  );
}

/**
 * Bus fan-out: one event published in-process crosses a service boundary over
 * the bus and lands on a handler in each of two other services. The active hop
 * carries one amber in-flight copy; the second hop is idle and dashed.
 */
function BusFanOut() {
  return (
    <svg
      viewBox="0 0 472 188"
      width="100%"
      role="img"
      aria-label="An event published in-process crosses a service boundary over the bus and runs on a handler in two other services."
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      <defs>
        <linearGradient
          id="v5-bus-flow"
          gradientUnits="userSpaceOnUse"
          x1="252"
          y1="86"
          x2="326"
          y2="47"
        >
          <stop offset="0" stopColor={SVG.amber} />
          <stop offset="1" stopColor={SVG.accent} />
        </linearGradient>
        <radialGradient id="v5-bus-lit" cx="16%" cy="50%" r="70%">
          <stop offset="0" stopColor={SVG.accent} stopOpacity="0.14" />
          <stop offset="0.7" stopColor={SVG.accent} stopOpacity="0" />
        </radialGradient>
      </defs>

      {/* lit origin behind the producer, the one in-process node */}
      <rect x="0" y="46" width="180" height="96" fill="url(#v5-bus-lit)" />

      {/* service boundary the event crosses */}
      <line
        x1="300"
        y1="14"
        x2="300"
        y2="172"
        stroke={SVG.inkFaint}
        strokeWidth="1"
        strokeDasharray="3 5"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="300"
        y="9"
        textAnchor="middle"
        fontSize="6"
        letterSpacing="1.4"
        fill={SVG.navLabel}
      >
        SERVICE BOUNDARY
      </text>

      {/* event -> bus, in-process */}
      <line
        x1="122"
        y1="94"
        x2="196"
        y2="94"
        stroke={SVG.accent}
        strokeOpacity="0.55"
        strokeWidth="1.5"
        vectorEffect="non-scaling-stroke"
      />

      {/* idle hop to the second service: dashed, waiting for its copy */}
      <path
        d="M260 102 C 288 114 300 141 326 141"
        fill="none"
        stroke={SVG.accent}
        strokeOpacity="0.3"
        strokeWidth="1"
        strokeDasharray="4 4"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />

      {/* active hop to the first service: amber at the source, teal on arrival */}
      <path
        d="M260 86 C 288 74 300 47 326 47"
        fill="none"
        stroke="url(#v5-bus-flow)"
        strokeWidth="1.75"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />

      {/* connection dots */}
      <circle cx="196" cy="94" r="2" fill={SVG.accent} />
      <circle cx="326" cy="47" r="2.5" fill={SVG.accent} />
      <circle cx="326" cy="141" r="2" fill={SVG.accent} fillOpacity="0.5" />

      {/* producer: the single event, published the moment 200 returns */}
      <rect
        x="10"
        y="66"
        width="112"
        height="56"
        rx="12"
        fill={SVG.surface}
        stroke={SVG.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="66"
        y="84"
        textAnchor="middle"
        fontSize="6"
        letterSpacing="1.5"
        fill={SVG.navLabel}
      >
        EVENT
      </text>
      <text
        x="66"
        y="99"
        textAnchor="middle"
        fontSize="10.5"
        fontWeight="600"
        fill={SVG.accent}
      >
        ReviewCreated
      </text>
      <text x="66" y="112" textAnchor="middle" fontSize="7" fill={SVG.inkDim}>
        POST /reviews
      </text>

      {/* returned-200 flag: the request is done while the fan-out runs */}
      <rect
        x="82"
        y="50"
        width="42"
        height="16"
        rx="8"
        fill={SVG.surface}
        stroke={SVG.healthy}
        strokeOpacity="0.7"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="103"
        y="61"
        textAnchor="middle"
        fontSize="7.5"
        fontWeight="500"
        fill={SVG.healthy}
      >
        200 OK
      </text>

      {/* bus node */}
      <rect
        x="196"
        y="78"
        width="64"
        height="32"
        rx="16"
        fill={SVG.surface}
        stroke={SVG.accent}
        strokeOpacity="0.5"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="228"
        y="98"
        textAnchor="middle"
        fontSize="8"
        letterSpacing="1.5"
        fill={SVG.accent}
      >
        BUS
      </text>

      {/* the two services, each running the same handler on its own */}
      {SERVICES.map((service) => (
        <g key={service.name}>
          <rect
            x="326"
            y={service.cy - 25}
            width="138"
            height="50"
            rx="10"
            fill={SVG.surface}
            fillOpacity="0.55"
            stroke={SVG.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="340"
            y={service.cy - 11}
            fontSize="6"
            letterSpacing="1.4"
            fill={SVG.navLabel}
          >
            HANDLER
          </text>
          <text x="340" y={service.cy + 1} fontSize="9.5" fill={SVG.inkDim}>
            {service.name}
          </text>
          <text x="340" y={service.cy + 12} fontSize="6.5" fill={SVG.faint}>
            {service.host}
          </text>
        </g>
      ))}

      {/* in-flight copy: one amber message crossing the boundary */}
      <rect
        x="248"
        y="40"
        width="74"
        height="20"
        rx="10"
        fill={SVG.surface}
        stroke={SVG.amber}
        strokeOpacity="0.9"
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="285"
        y="54"
        textAnchor="middle"
        fontSize="8.5"
        fontWeight="500"
        fill={SVG.amber}
      >
        ReviewCreated
      </text>
    </svg>
  );
}

/** Outbox -> inbox, the pair that makes handling exactly-once across a hop. */
function OutboxInbox() {
  return (
    <svg
      viewBox="0 0 264 92"
      width="100%"
      role="img"
      aria-label="A transactional outbox hands a message to an idempotent inbox for exactly-once handling."
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      {/* exactly-once tag, tied down to the hop */}
      <line
        x1="132"
        y1="28"
        x2="132"
        y2="56"
        stroke={SVG.accent}
        strokeOpacity="0.45"
        strokeWidth="1"
        strokeDasharray="3 3"
        vectorEffect="non-scaling-stroke"
      />
      <rect
        x="90"
        y="6"
        width="84"
        height="22"
        rx="11"
        fill={SVG.surface}
        stroke={SVG.accent}
        strokeOpacity="0.6"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="132"
        y="21"
        textAnchor="middle"
        fontSize="8.5"
        fontWeight="500"
        fill={SVG.accent}
      >
        exactly-once
      </text>

      {/* the hop between the two stores */}
      <line
        x1="104"
        y1="56"
        x2="160"
        y2="56"
        stroke={SVG.accent}
        strokeOpacity="0.55"
        strokeWidth="1.5"
        vectorEffect="non-scaling-stroke"
      />
      <circle cx="132" cy="56" r="2" fill={SVG.accent} />

      {/* outbox */}
      <rect
        x="6"
        y="34"
        width="98"
        height="44"
        rx="10"
        fill={SVG.surface}
        stroke={SVG.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="20" y="52" fontSize="6" letterSpacing="1.4" fill={SVG.navLabel}>
        OUTBOX
      </text>
      <text x="20" y="66" fontSize="8.5" fill={SVG.inkDim}>
        transactional
      </text>

      {/* inbox */}
      <rect
        x="160"
        y="34"
        width="98"
        height="44"
        rx="10"
        fill={SVG.surface}
        stroke={SVG.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="174" y="52" fontSize="6" letterSpacing="1.4" fill={SVG.navLabel}>
        INBOX
      </text>
      <text x="174" y="66" fontSize="8.5" fill={SVG.inkDim}>
        idempotent
      </text>
    </svg>
  );
}

/** The two services the one event fans out to over the bus. */
const SERVICES: readonly {
  readonly name: string;
  readonly host: string;
  readonly cy: number;
}[] = [
  { name: "OrderProjector", host: "orders-svc", cy: 47 },
  { name: "ChargeCustomer", host: "billing-svc", cy: 141 },
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette for the inline diagrams: navy surfaces, teal, amber. */
const SVG = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  healthy: "#34d399",
} as const;
