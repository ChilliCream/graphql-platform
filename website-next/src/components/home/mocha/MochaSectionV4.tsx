import Link from "next/link";
import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Mocha messaging section, take v4 "Capabilities".
 *
 * An all-visible capability layout: a frame heading block, one wide intro card
 * for "Return now, process after." with a request -> response -> continuation
 * diagram, then a 2x2 grid of Mediator / Bus / Sagas / Delivery cards, each with
 * a small bespoke inline diagram. The whole section ends with a single
 * "Open workflows" link. No tabs, steppers, or click-to-reveal: every beat is on
 * screen at once. All svg ids and colors are local to this file (prefix "mv4-").
 */
export function MochaSectionV4() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll className="mx-auto max-w-3xl text-center">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Messaging
        </span>
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
          Messaging, mediator, and sagas.
        </h2>
        <p className="text-cc-ink mx-auto mt-6 max-w-3xl text-base text-pretty sm:text-lg">
          Mocha is the messaging side of the platform: an in-process mediator
          for commands and queries, a bus for events across services, sagas for
          long-running work, and exactly-once delivery over RabbitMQ, Kafka,
          Postgres, or Azure.
        </p>
      </RevealOnScroll>

      {/* wide intro card: the request returns immediately, the handler runs after */}
      <RevealOnScroll className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover mt-12 grid items-center gap-6 rounded-3xl border p-6 backdrop-blur-sm transition-colors sm:mt-14 sm:p-8 lg:grid-cols-2 lg:gap-10">
        <div>
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.12em] uppercase">
            Background work
          </span>
          <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.15] font-semibold text-balance">
            Return now, process after.
          </h3>
          <p className="text-cc-ink mt-3 max-w-md text-base text-pretty">
            Hand slow or fan-out work to a handler and return the response. The
            request stays fast; the rest runs on its own.
          </p>
        </div>
        <div className="border-cc-card-border bg-cc-surface rounded-2xl border p-4 sm:p-5">
          <ReturnNowDiagram />
        </div>
      </RevealOnScroll>

      {/* 2x2 capability grid: mediator, bus, sagas, delivery */}
      <div className="mt-5 grid gap-5 sm:mt-6 sm:gap-6 lg:grid-cols-2">
        {CAPABILITIES.map(({ key, eyebrow, headline, blurb, Diagram }) => (
          <CapabilityTile
            key={key}
            eyebrow={eyebrow}
            headline={headline}
            blurb={blurb}
          >
            <Diagram />
          </CapabilityTile>
        ))}
      </div>

      <div className="mt-10 flex justify-center sm:mt-12">
        <Link
          href="/platform/workflows"
          className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
        >
          Open workflows
          <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>
    </section>
  );
}

interface CapabilityTileProps {
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly children: ReactNode;
}

/** One capability tile: eyebrow + headline + one-line blurb + a small diagram. */
function CapabilityTile({
  eyebrow,
  headline,
  blurb,
  children,
}: CapabilityTileProps) {
  return (
    <RevealOnScroll className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-6 backdrop-blur-sm transition-colors sm:p-7">
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.12em] uppercase">
        {eyebrow}
      </span>
      <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.2] font-semibold text-balance">
        {headline}
      </h3>
      <p className="text-cc-ink mt-3 mb-6 text-sm text-pretty sm:text-base">
        {blurb}
      </p>
      <div className="border-cc-card-border bg-cc-surface mt-auto rounded-xl border p-4">
        {children}
      </div>
    </RevealOnScroll>
  );
}

/**
 * Intro diagram: a single request returns immediately (left of NOW) while the
 * handler keeps running after the response already went back (right of NOW).
 */
function ReturnNowDiagram() {
  return (
    <svg
      viewBox="0 0 600 180"
      width="100%"
      aria-hidden="true"
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      {/* NOW divider: response is to its left, processing continues to its right */}
      <line
        x1="362"
        y1="24"
        x2="362"
        y2="160"
        stroke={C.inkFaint}
        strokeWidth="1"
        strokeDasharray="3 6"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="362"
        y="16"
        textAnchor="middle"
        fontSize="8"
        letterSpacing="1.6"
        fill={C.navLabel}
      >
        NOW
      </text>

      {/* request: enters once, returns fast and hands off the slow work */}
      <rect
        x="16"
        y="70"
        width="120"
        height="44"
        rx="11"
        fill={C.surface}
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="30" y="89" fontSize="7" letterSpacing="1.2" fill={C.navLabel}>
        REQUEST
      </text>
      <text x="30" y="104" fontSize="10.5" fill={C.heading}>
        POST /reviews
      </text>

      {/* return-now branch: the response goes back fast, left of NOW */}
      <path
        d="M136 92 C 178 92 188 58 214 58"
        fill="none"
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <circle cx="136" cy="92" r="2.5" fill={C.accent} />
      <rect
        x="214"
        y="40"
        width="128"
        height="36"
        rx="9"
        fill={C.surface}
        stroke={C.healthy}
        strokeOpacity="0.55"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="226" y="55" fontSize="6.5" letterSpacing="1.2" fill={C.navLabel}>
        RESPONSE
      </text>
      <text x="226" y="68" fontSize="10" fill={C.healthy}>
        200 OK
      </text>

      {/* process-after branch: the handler runs on its own, crossing NOW */}
      <path
        d="M136 92 C 178 92 188 128 214 128"
        fill="none"
        stroke={C.amber}
        strokeOpacity="0.8"
        strokeWidth="1.25"
        strokeDasharray="4 4"
        vectorEffect="non-scaling-stroke"
      />
      <rect
        x="214"
        y="110"
        width="128"
        height="36"
        rx="9"
        fill={C.surface}
        stroke={C.amber}
        strokeOpacity="0.5"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="226"
        y="125"
        fontSize="6.5"
        letterSpacing="1.2"
        fill={C.navLabel}
      >
        BACKGROUND
      </text>
      <text x="226" y="138" fontSize="10" fill={C.heading}>
        handler
      </text>

      {/* continuation: keeps running after the response already returned */}
      <line
        x1="342"
        y1="128"
        x2="584"
        y2="128"
        stroke={C.amber}
        strokeOpacity="0.7"
        strokeWidth="1.25"
        strokeDasharray="4 4"
        vectorEffect="non-scaling-stroke"
      />
      <rect
        x="408"
        y="118"
        width="86"
        height="20"
        rx="10"
        fill={C.surface}
        stroke={C.amber}
        strokeOpacity="0.85"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="451" y="132" textAnchor="middle" fontSize="8.5" fill={C.amber}>
        running
      </text>
      <polygon
        points="584,128 576,124 576,132"
        fill={C.amber}
        fillOpacity="0.8"
      />
      <text x="512" y="112" fontSize="7" letterSpacing="0.6" fill={C.navLabel}>
        runs on its own
      </text>
    </svg>
  );
}

/** Mediator diagram: a command and a query dispatch through one mediator. */
function MediatorDiagram() {
  return (
    <svg
      viewBox="0 0 300 150"
      width="100%"
      aria-hidden="true"
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      {/* command */}
      <rect
        x="8"
        y="24"
        width="104"
        height="36"
        rx="8"
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity="0.5"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="20" y="40" fontSize="6.5" letterSpacing="1.1" fill={C.navLabel}>
        COMMAND
      </text>
      <text x="20" y="52" fontSize="9" fill={C.heading}>
        CreateReview
      </text>

      {/* query */}
      <rect
        x="8"
        y="90"
        width="104"
        height="36"
        rx="8"
        fill={C.surface}
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="20" y="106" fontSize="6.5" letterSpacing="1.1" fill={C.navLabel}>
        QUERY
      </text>
      <text x="20" y="118" fontSize="9" fill={C.heading}>
        GetReview
      </text>

      {/* both dispatch into one mediator */}
      <path
        d="M112 42 C 124 42 122 75 132 75"
        fill="none"
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <path
        d="M112 108 C 124 108 122 75 132 75"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.7"
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <rect
        x="132"
        y="58"
        width="58"
        height="34"
        rx="9"
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity="0.6"
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text x="161" y="79" textAnchor="middle" fontSize="8" fill={C.accent}>
        mediator
      </text>

      {/* to one handler interface */}
      <line
        x1="190"
        y1="75"
        x2="206"
        y2="75"
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <polygon points="210,75 202,71 202,79" fill={C.accent} />
      <rect
        x="210"
        y="57"
        width="82"
        height="36"
        rx="9"
        fill={C.surface}
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="222" y="73" fontSize="6.5" letterSpacing="1.1" fill={C.navLabel}>
        HANDLER
      </text>
      <text x="222" y="85" fontSize="9" fill={C.heading}>
        Handle()
      </text>
    </svg>
  );
}

/** Bus diagram: one event fans out to handlers running in other services. */
function BusDiagram() {
  return (
    <svg
      viewBox="0 0 300 160"
      width="100%"
      aria-hidden="true"
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      {/* the published event */}
      <rect
        x="6"
        y="58"
        width="106"
        height="42"
        rx="9"
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity="0.55"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="18" y="75" fontSize="6.5" letterSpacing="1.1" fill={C.navLabel}>
        EVENT
      </text>
      <text x="18" y="89" fontSize="9" fill={C.heading}>
        ReviewCreated
      </text>

      {/* service boundary the event crosses */}
      <line
        x1="156"
        y1="20"
        x2="156"
        y2="150"
        stroke={C.inkFaint}
        strokeWidth="1"
        strokeDasharray="3 5"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="240"
        y="14"
        textAnchor="middle"
        fontSize="6"
        letterSpacing="1.2"
        fill={C.navLabel}
      >
        OTHER SERVICES
      </text>

      {/* fan-out to three handlers; one copy is in flight on the bus */}
      <path
        d="M112 79 C 150 79 150 35 188 35"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.35"
        strokeWidth="1"
        strokeDasharray="4 4"
        vectorEffect="non-scaling-stroke"
      />
      <line
        x1="112"
        y1="79"
        x2="188"
        y2="79"
        stroke={C.amber}
        strokeOpacity="0.85"
        strokeWidth="1.25"
        strokeDasharray="4 4"
        vectorEffect="non-scaling-stroke"
      />
      <path
        d="M112 79 C 150 79 150 123 188 123"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.35"
        strokeWidth="1"
        strokeDasharray="4 4"
        vectorEffect="non-scaling-stroke"
      />
      <circle cx="150" cy="79" r="3" fill={C.amber} />

      {BUS_HANDLERS.map((h) => (
        <g key={h.name}>
          <circle cx="188" cy={h.cy} r="2" fill={C.accent} />
          <rect
            x="190"
            y={h.cy - 15}
            width="104"
            height="30"
            rx="8"
            fill={C.surface}
            stroke={C.border}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text x="204" y={h.cy + 3.5} fontSize="9" fill={C.inkDim}>
            {h.name}
          </text>
        </g>
      ))}
    </svg>
  );
}

/** Saga diagram: a state machine that resumes from persisted state on restart. */
function SagaDiagram() {
  return (
    <svg
      viewBox="0 0 300 150"
      width="100%"
      aria-hidden="true"
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      <text x="8" y="20" fontSize="6.5" letterSpacing="1.2" fill={C.navLabel}>
        STATE MACHINE
      </text>

      {/* Draft (done) */}
      <rect
        x="8"
        y="30"
        width="80"
        height="34"
        rx="8"
        fill={C.surface}
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="48" y="51" textAnchor="middle" fontSize="9.5" fill={C.inkDim}>
        Draft
      </text>
      <line
        x1="88"
        y1="47"
        x2="106"
        y2="47"
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <polygon points="110,47 102,43 102,51" fill={C.accent} />

      {/* Checked (active) */}
      <rect
        x="110"
        y="30"
        width="80"
        height="34"
        rx="8"
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity="0.6"
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text x="150" y="51" textAnchor="middle" fontSize="9.5" fill={C.accent}>
        Checked
      </text>
      <line
        x1="190"
        y1="47"
        x2="206"
        y2="47"
        stroke={C.inkFaint}
        strokeWidth="1.25"
        strokeDasharray="3 3"
        vectorEffect="non-scaling-stroke"
      />
      <polygon points="210,47 202,43 202,51" fill={C.inkFaint} />

      {/* Published (next) */}
      <rect
        x="212"
        y="30"
        width="80"
        height="34"
        rx="8"
        fill={C.surface}
        stroke={C.inkFaint}
        strokeWidth="1"
        strokeDasharray="3 3"
        vectorEffect="non-scaling-stroke"
      />
      <text x="252" y="51" textAnchor="middle" fontSize="9.5" fill={C.inkDim}>
        Published
      </text>

      {/* persisted state the saga reloads from after a restart */}
      <ellipse
        cx="150"
        cy="92"
        rx="26"
        ry="6"
        fill={C.surface}
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <line
        x1="124"
        y1="92"
        x2="124"
        y2="114"
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <line
        x1="176"
        y1="92"
        x2="176"
        y2="114"
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <ellipse
        cx="150"
        cy="114"
        rx="26"
        ry="6"
        fill="none"
        stroke={C.border}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="150" y="106" textAnchor="middle" fontSize="7.5" fill={C.inkDim}>
        state
      </text>

      {/* resume: state reloads into the active step */}
      <line
        x1="150"
        y1="86"
        x2="150"
        y2="66"
        stroke={C.accent}
        strokeOpacity="0.6"
        strokeWidth="1.25"
        strokeDasharray="3 3"
        vectorEffect="non-scaling-stroke"
      />
      <polygon
        points="150,62 146,70 154,70"
        fill={C.accent}
        fillOpacity="0.7"
      />
      <text x="166" y="80" fontSize="6.5" letterSpacing="0.5" fill={C.navLabel}>
        resume after restart
      </text>
    </svg>
  );
}

/** Delivery diagram: outbox to inbox, where a duplicate is dropped on dedupe. */
function DeliveryDiagram() {
  return (
    <svg
      viewBox="0 0 300 160"
      width="100%"
      aria-hidden="true"
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      {/* outbox */}
      <rect
        x="6"
        y="56"
        width="84"
        height="44"
        rx="9"
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity="0.5"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="48"
        y="74"
        textAnchor="middle"
        fontSize="6.5"
        letterSpacing="1.1"
        fill={C.navLabel}
      >
        OUTBOX
      </text>
      <text x="48" y="88" textAnchor="middle" fontSize="8" fill={C.inkDim}>
        committed
      </text>

      {/* inbox */}
      <rect
        x="210"
        y="56"
        width="84"
        height="44"
        rx="9"
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity="0.5"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="252"
        y="74"
        textAnchor="middle"
        fontSize="6.5"
        letterSpacing="1.1"
        fill={C.navLabel}
      >
        INBOX
      </text>
      <text x="252" y="88" textAnchor="middle" fontSize="8" fill={C.inkDim}>
        dedupe
      </text>

      {/* first delivery: accepted and handled once */}
      <line
        x1="90"
        y1="70"
        x2="206"
        y2="70"
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <polygon points="210,70 202,66 202,74" fill={C.accent} />
      <rect
        x="130"
        y="62"
        width="34"
        height="16"
        rx="8"
        fill={C.surface}
        stroke={C.accent}
        strokeOpacity="0.7"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="147" y="73.5" textAnchor="middle" fontSize="8" fill={C.accent}>
        #42
      </text>

      {/* duplicate retry: dropped at the inbox */}
      <line
        x1="90"
        y1="96"
        x2="188"
        y2="96"
        stroke={C.amber}
        strokeOpacity="0.8"
        strokeWidth="1.25"
        strokeDasharray="4 4"
        vectorEffect="non-scaling-stroke"
      />
      <rect
        x="120"
        y="88"
        width="34"
        height="16"
        rx="8"
        fill={C.surface}
        stroke={C.amber}
        strokeOpacity="0.75"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="137" y="99.5" textAnchor="middle" fontSize="8" fill={C.amber}>
        #42
      </text>
      <circle
        cx="196"
        cy="96"
        r="8"
        fill={C.surface}
        stroke={C.amber}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <path
        d="M192 92 L 200 100 M200 92 L 192 100"
        stroke={C.amber}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="196"
        y="118"
        textAnchor="middle"
        fontSize="6.5"
        fill={C.navLabel}
      >
        dropped
      </text>

      {/* result */}
      <text
        x="150"
        y="140"
        textAnchor="middle"
        fontSize="7"
        letterSpacing="0.8"
        fill={C.inkDim}
      >
        handled exactly once
      </text>
    </svg>
  );
}

interface Capability {
  readonly key: string;
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly Diagram: () => ReactNode;
}

const CAPABILITIES: readonly Capability[] = [
  {
    key: "mediator",
    eyebrow: "Mediator",
    headline: "Commands and queries, in-process.",
    blurb:
      "Dispatch through the mediator and one handler interface. CQRS without the registration wiring.",
    Diagram: MediatorDiagram,
  },
  {
    key: "bus",
    eyebrow: "Bus",
    headline: "Events, across services.",
    blurb:
      "Publish an event and the same handlers run on other services over a bus. The model does not change when work leaves the process.",
    Diagram: BusDiagram,
  },
  {
    key: "sagas",
    eyebrow: "Sagas",
    headline: "Long-running processes.",
    blurb:
      "A saga is a state machine that drives a process across many messages and resumes after a restart.",
    Diagram: SagaDiagram,
  },
  {
    key: "delivery",
    eyebrow: "Delivery",
    headline: "Exactly-once.",
    blurb:
      "A transactional outbox and idempotent inbox give exactly-once handling over RabbitMQ, Kafka, Postgres, or Azure.",
    Diagram: DeliveryDiagram,
  },
];

/** The three handlers the bus event fans out to, top to bottom. */
const BUS_HANDLERS: readonly { readonly name: string; readonly cy: number }[] =
  [
    { name: "search-svc", cy: 35 },
    { name: "email-svc", cy: 79 },
    { name: "metrics-svc", cy: 123 },
  ];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Local palette: dark surfaces, neutral ink, teal accent, status amber/green. */
const C = {
  heading: "#f5f0ea",
  inkDim: "rgba(245, 241, 234, 0.62)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  border: "rgba(245, 241, 234, 0.12)",
  surface: "#0c1322",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  healthy: "#34d399",
} as const;
