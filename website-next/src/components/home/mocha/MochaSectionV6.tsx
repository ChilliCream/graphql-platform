import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Mocha messaging section, take v6 "Every app runs on events".
 *
 * One horizon-split scene. A single 1px line runs across the full width. Above
 * it sits the thin visible surface: a request arriving, its response returning,
 * and an API node, kept small and calm. Below it, filling most of the frame,
 * is the messaging layer the app actually runs on: the same action published as
 * an event, fanning across a service boundary to a few handlers, and a saga
 * advancing after the response. The size difference is the point. What users see
 * is the thin top; the events are the larger part underneath. Teal marks the one
 * traced event, amber the single in-flight copy, green the returned response.
 * Every svg id is prefixed "v6-". All content is visible at once.
 */
export function MochaSectionV6() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* frame: the section heading block */}
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Messaging
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Every app runs on events.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Under the request and response, an app is a set of parts reacting to
            each other: an order placed, a review posted, a job finished. Mocha
            is the messaging that carries those events: an in-process mediator,
            a bus across services, and sagas for the work that takes time.
          </p>
          <Link
            href="/platform/workflows"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Open workflows
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* the horizon scene: thin surface above, messaging substrate below */}
        <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover mt-10 rounded-3xl border p-5 backdrop-blur-sm transition-colors sm:mt-12 sm:p-8">
          <header className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
              Surface and events
            </p>
            <div className="flex items-center gap-4">
              <LegendDot color={C.accent} label="traced event" />
              <LegendDot color={C.amber} label="in flight" />
            </div>
          </header>

          <HorizonScene />

          <p className="text-cc-ink-dim border-cc-card-border mt-5 border-t pt-4 text-sm text-pretty">
            Above the line is the request and its response. Below it, the same
            action is an event other services react to, with a saga that keeps
            running after the response returns.
          </p>
        </div>

        {/* the three parts named in the lead, plainly stated */}
        <div className="mt-8 grid grid-cols-1 gap-3 sm:grid-cols-3">
          {PARTS.map((part) => (
            <div
              key={part.label}
              className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-2xl border p-4 transition-colors sm:p-5"
            >
              <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                {part.label}
              </p>
              <h3 className="font-heading text-cc-heading text-h6 mt-1.5 leading-snug font-semibold text-balance">
                {part.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm text-pretty">
                {part.line}
              </p>
            </div>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}

interface Part {
  readonly label: string;
  readonly title: string;
  readonly line: string;
}

/** The three parts of Mocha named in the lead, in the same order. */
const PARTS: readonly Part[] = [
  {
    label: "Mediator",
    title: "Commands and queries, in-process.",
    line: "Dispatch through one handler interface. CQRS without the registration wiring.",
  },
  {
    label: "Bus",
    title: "Events, across services.",
    line: "Publish an event and the same handlers run on other services over a bus.",
  },
  {
    label: "Sagas",
    title: "Work that takes time.",
    line: "A saga advances as a state machine across many messages and resumes after a restart.",
  },
];

/**
 * The horizon diagram. One full-width line separates the thin API surface above
 * from the messaging substrate below. The substrate is given most of the height
 * so it reads as the larger part of the app.
 */
function HorizonScene() {
  return (
    <svg
      viewBox="0 0 1000 450"
      width="100%"
      role="img"
      aria-label="A thin API surface of one request and its response sits above a horizon line; beneath it the same action is published as an event that fans across a service boundary to three handlers, with a saga advancing after the response."
      className="mt-5"
      style={{ display: "block", overflow: "visible", fontFamily: MONO }}
    >
      <defs>
        <radialGradient id="v6-lit" cx="50%" cy="50%" r="62%">
          <stop offset="0" stopColor={C.accent} stopOpacity="0.16" />
          <stop offset="100%" stopColor={C.accent} stopOpacity="0" />
        </radialGradient>
        <linearGradient
          id="v6-flow"
          gradientUnits="userSpaceOnUse"
          x1="300"
          y1="240"
          x2="560"
          y2="240"
        >
          <stop offset="0" stopColor={C.amber} />
          <stop offset="1" stopColor={C.accent} />
        </linearGradient>
        <filter id="v6-glow" x="-40%" y="-40%" width="180%" height="180%">
          <feGaussianBlur stdDeviation="5" />
        </filter>
      </defs>

      {/* ABOVE THE LINE: the thin visible surface */}

      {/* request arriving, down into the API */}
      <text x="478" y="18" textAnchor="middle" fontSize="7.5" fill={C.navLabel}>
        request
      </text>
      <line
        x1="478"
        y1="26"
        x2="478"
        y2="50"
        stroke={C.accent}
        strokeOpacity="0.7"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <path d="M474 44 L478 51 L482 44" fill={C.accent} />

      {/* response returning, up out of the API */}
      <text x="524" y="18" textAnchor="middle" fontSize="7.5" fill={C.healthy}>
        200 OK
      </text>
      <line
        x1="524"
        y1="50"
        x2="524"
        y2="26"
        stroke={C.healthy}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <path d="M520 32 L524 25 L528 32" fill={C.healthy} />

      {/* the API node, small and calm */}
      <rect
        x="430"
        y="52"
        width="140"
        height="46"
        rx="10"
        fill={C.surface}
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="500"
        y="73"
        textAnchor="middle"
        fontSize="7"
        letterSpacing="1.5"
        fill={C.navLabel}
      >
        API
      </text>
      <text x="500" y="89" textAnchor="middle" fontSize="9" fill={C.inkDim}>
        POST /orders
      </text>

      {/* the horizon line and its plain labels */}
      <line
        x1="30"
        y1="134"
        x2="970"
        y2="134"
        stroke="rgba(245,241,234,0.28)"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="30" y="126" fontSize="8" letterSpacing="1.5" fill={C.navLabel}>
        API SURFACE
      </text>
      <text x="970" y="126" textAnchor="end" fontSize="8.5" fill={C.faint}>
        what users see
      </text>
      <text x="30" y="151" fontSize="8" letterSpacing="1.5" fill={C.navLabel}>
        EVENTS
      </text>
      <text x="970" y="151" textAnchor="end" fontSize="8.5" fill={C.faint}>
        what the app runs on
      </text>

      {/* the API publishes the action as an event, crossing the line downward */}
      <path
        d="M500 98 C 500 150 149 150 149 210"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.5"
        strokeWidth="1.5"
        vectorEffect="non-scaling-stroke"
      />
      <circle cx="500" cy="98" r="2" fill={C.accent} />
      <text x="325" y="145" textAnchor="middle" fontSize="8" fill={C.navLabel}>
        publishes OrderPlaced
      </text>

      {/* BELOW THE LINE: the messaging substrate, given most of the height */}

      {/* lit halo behind the published event, the one origin */}
      <rect x="40" y="188" width="220" height="104" fill="url(#v6-lit)" />

      {/* service boundary the events cross */}
      <line
        x1="548"
        y1="150"
        x2="548"
        y2="348"
        stroke={C.inkFaint}
        strokeWidth="1"
        strokeDasharray="3 5"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="548"
        y="146"
        textAnchor="middle"
        fontSize="7"
        letterSpacing="1.2"
        fill={C.navLabel}
      >
        SERVICE BOUNDARY
      </text>

      {/* two idle async hops: dashed, waiting for their copy of the event */}
      <path
        d="M228 240 C 430 240 430 172 600 172"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.3"
        strokeWidth="1"
        strokeDasharray="4 4"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />
      <path
        d="M228 240 C 430 240 430 308 600 308"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.3"
        strokeWidth="1"
        strokeDasharray="4 4"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />

      {/* the traced hop: solid, amber at the source warming to teal on arrival */}
      <line
        x1="228"
        y1="240"
        x2="596"
        y2="240"
        stroke="url(#v6-flow)"
        strokeWidth="1.75"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />

      {/* connection dots: amber at the lit source, teal where copies land */}
      <circle cx="228" cy="240" r="2.5" fill={C.amber} />
      <circle cx="600" cy="172" r="2" fill={C.accent} fillOpacity="0.5" />
      <circle cx="600" cy="240" r="2.5" fill={C.accent} />
      <circle cx="600" cy="308" r="2" fill={C.accent} fillOpacity="0.5" />

      {/* the published event node: the lit origin of the fan-out */}
      <rect
        x="70"
        y="210"
        width="158"
        height="60"
        rx="14"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.4"
        strokeWidth="2"
        filter="url(#v6-glow)"
      />
      <rect
        x="70"
        y="210"
        width="158"
        height="60"
        rx="14"
        fill={C.surface}
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="149"
        y="232"
        textAnchor="middle"
        fontSize="7"
        letterSpacing="1.5"
        fill={C.navLabel}
      >
        EVENT
      </text>
      <text
        x="149"
        y="250"
        textAnchor="middle"
        fontSize="13"
        fontWeight="600"
        fill={C.accent}
      >
        OrderPlaced
      </text>
      <text x="149" y="263" textAnchor="middle" fontSize="8" fill={C.faint}>
        order-svc
      </text>

      {/* in-flight token: one amber copy of the event riding the traced hop */}
      <rect
        x="362"
        y="229"
        width="80"
        height="22"
        rx="11"
        fill={C.surface}
        stroke={C.amber}
        strokeOpacity="0.9"
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="402"
        y="244"
        textAnchor="middle"
        fontSize="9"
        fontWeight="500"
        fill={C.amber}
      >
        OrderPlaced
      </text>
      <circle cx="446" cy="240" r="3" fill={C.amber} />

      {/* the three reactions, each an independent node on its own service */}
      {REACTIONS.map((r) => (
        <g key={r.name}>
          <rect
            x="600"
            y={r.cy - 26}
            width="180"
            height="52"
            rx="10"
            fill={C.surface}
            fillOpacity="0.55"
            stroke={r.traced ? C.accent : C.cardBorder}
            strokeOpacity={r.traced ? 0.5 : 1}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="618"
            y={r.cy - 12}
            fontSize="7"
            letterSpacing="1.2"
            fill={C.navLabel}
          >
            {r.kind}
          </text>
          <text x="618" y={r.cy + 2} fontSize="10" fill={C.inkDim}>
            {r.name}
          </text>
          <text x="618" y={r.cy + 15} fontSize="7" fill={C.faint}>
            {r.service}
          </text>
        </g>
      ))}

      {/* a saga keeps the multi-step work going after the response returned */}
      <text x="70" y="372" fontSize="8" letterSpacing="1.2" fill={C.navLabel}>
        SAGA &middot; ADVANCES AFTER THE RESPONSE
      </text>
      {SAGA.map((s, i) => (
        <g key={s.name}>
          {i > 0 && (
            <text
              x={s.cx - 95}
              y="414"
              textAnchor="middle"
              fontSize="10"
              fill={C.faint}
            >
              &rarr;
            </text>
          )}
          <rect
            x={s.cx - 60}
            y="395"
            width="120"
            height="30"
            rx="8"
            fill={s.state === "active" ? C.accent : "none"}
            fillOpacity={s.state === "active" ? 0.06 : 1}
            stroke={
              s.state === "active"
                ? C.accent
                : s.state === "next"
                  ? C.inkFaint
                  : C.cardBorder
            }
            strokeOpacity={s.state === "active" ? 0.6 : 1}
            strokeWidth="1"
            strokeDasharray={s.state === "next" ? "3 3" : undefined}
            vectorEffect="non-scaling-stroke"
          />
          <text
            x={s.cx}
            y="414"
            textAnchor="middle"
            fontSize="9"
            fill={s.state === "active" ? C.accent : C.inkDim}
          >
            {s.name}
          </text>
        </g>
      ))}
    </svg>
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

/** The three reactions the one event fans out to, top to bottom. The middle one
 * is the traced hop the in-flight copy is heading toward. */
const REACTIONS: readonly {
  readonly kind: "HANDLER" | "CONSUMER";
  readonly name: string;
  readonly service: string;
  readonly cy: number;
  readonly traced?: boolean;
}[] = [
  { kind: "HANDLER", name: "ChargePayment", service: "payments-svc", cy: 172 },
  {
    kind: "HANDLER",
    name: "UpdateInventory",
    service: "inventory-svc",
    cy: 240,
    traced: true,
  },
  { kind: "CONSUMER", name: "Analytics", service: "metrics-svc", cy: 308 },
];

/** The saga that keeps advancing after the response returned. */
const SAGA: readonly {
  readonly name: string;
  readonly cx: number;
  readonly state: "done" | "active" | "next";
}[] = [
  { name: "Placed", cx: 230, state: "done" },
  { name: "Paid", cx: 500, state: "active" },
  { name: "Shipped", cx: 770, state: "next" },
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette for the inline diagram: navy surfaces, neutral ink, teal,
 * amber for the one in-flight message, healthy green for the returned response. */
const C = {
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
