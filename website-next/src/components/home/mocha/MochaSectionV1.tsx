import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/** Locked cc-* palette for the inline diagram: dark surface, neutral ink, teal,
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

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "mv1-";

interface Beat {
  readonly n: string;
  readonly eyebrow: string;
  readonly headline: string;
  readonly line: string;
}

/** The five beats, in timeline order, rendered as a labeled row beneath the
 * sequence. Each one names a part of the diagram above it. */
const BEATS: readonly Beat[] = [
  {
    n: "01",
    eyebrow: "Background work",
    headline: "Return now, process after.",
    line: "Hand slow or fan-out work to a handler and return the response. The request stays fast; the rest runs on its own.",
  },
  {
    n: "02",
    eyebrow: "Mediator",
    headline: "Commands and queries, in-process.",
    line: "Dispatch through the mediator and one handler interface. CQRS without the registration wiring.",
  },
  {
    n: "03",
    eyebrow: "Bus",
    headline: "Events, across services.",
    line: "Publish an event and the same handlers run on other services over a bus. The model does not change when work leaves the process.",
  },
  {
    n: "04",
    eyebrow: "Sagas",
    headline: "Long-running processes.",
    line: "A saga is a state machine that drives a process across many messages and resumes after a restart.",
  },
  {
    n: "05",
    eyebrow: "Delivery",
    headline: "Exactly-once.",
    line: "A transactional outbox and idempotent inbox give exactly-once handling over RabbitMQ, Kafka, Postgres, or Azure.",
  },
];

/**
 * Mocha messaging section, take v1 "Sequence".
 *
 * One left-to-right sequence diagram on a thin time axis. A request enters at
 * the left and the response returns early, back up to the caller. Everything to
 * the right of that point runs afterward: the handler, an event published to a
 * bus that fans out to two consumers (one copy in flight, in amber), and a saga
 * advancing one state. The timeline rail is the time axis; thin 1px strokes and
 * mono labels throughout. Below the diagram the five beats sit in a labeled row,
 * each one naming a part of the sequence. All content is visible at once.
 */
export function MochaSectionV1() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* frame */}
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
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* sequence diagram */}
        <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover mt-10 rounded-3xl border p-5 backdrop-blur-sm transition-colors sm:p-8">
          <div className="flex items-center justify-between gap-3">
            <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.15em] uppercase">
              request lifecycle
            </span>
            <span className="border-cc-accent/35 text-cc-accent bg-cc-accent/10 inline-flex shrink-0 items-center rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] font-medium">
              exactly-once
            </span>
          </div>

          <svg
            viewBox="0 0 1000 350"
            width="100%"
            aria-hidden="true"
            className="mt-5"
            style={{ display: "block", overflow: "visible", fontFamily: MONO }}
          >
            <defs>
              <radialGradient id={`${ID}lit`} cx="50%" cy="50%" r="62%">
                <stop offset="0" stopColor={C.accent} stopOpacity="0.16" />
                <stop offset="100%" stopColor={C.accent} stopOpacity="0" />
              </radialGradient>
              <linearGradient
                id={`${ID}flow`}
                gradientUnits="userSpaceOnUse"
                x1="648"
                y1="196"
                x2="728"
                y2="267"
              >
                <stop offset="0" stopColor={C.amber} />
                <stop offset="1" stopColor={C.accent} />
              </linearGradient>
              <filter
                id={`${ID}glow`}
                x="-40%"
                y="-40%"
                width="180%"
                height="180%"
              >
                <feGaussianBlur stdDeviation="5" />
              </filter>
            </defs>

            {/* time axis / process rail. The request portion (entry to the early
                response) is teal; the continuation portion is neutral. */}
            <line
              x1="92"
              y1="176"
              x2="322"
              y2="176"
              stroke={C.accent}
              strokeOpacity="0.45"
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <line
              x1="322"
              y1="176"
              x2="936"
              y2="176"
              stroke={C.cardBorder}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <path
              d="M930 172 L938 176 L930 180"
              fill="none"
              stroke={C.faint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="934"
              y="166"
              textAnchor="end"
              fontSize="7"
              fill={C.navLabel}
            >
              time
            </text>

            {/* divider: everything to the right runs after the response */}
            <line
              x1="372"
              y1="72"
              x2="372"
              y2="316"
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeDasharray="2 6"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="380"
              y="80"
              fontSize="7"
              letterSpacing="1.2"
              fill={C.navLabel}
            >
              AFTER THE RESPONSE
            </text>

            {/* caller lane: request drops in, response returns early */}
            <rect
              x="44"
              y="82"
              width="86"
              height="36"
              rx="8"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="87"
              y="98"
              textAnchor="middle"
              fontSize="5.5"
              letterSpacing="1.2"
              fill={C.navLabel}
            >
              ORIGIN
            </text>
            <text
              x="87"
              y="109"
              textAnchor="middle"
              fontSize="9"
              fill={C.inkDim}
            >
              caller
            </text>
            <line
              x1="130"
              y1="100"
              x2="322"
              y2="100"
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeDasharray="3 5"
              vectorEffect="non-scaling-stroke"
            />

            {/* request: caller -> rail */}
            <line
              x1="150"
              y1="100"
              x2="150"
              y2="155"
              stroke={C.accent}
              strokeOpacity="0.7"
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <path d="M146 149 L150 156 L154 149" fill={C.accent} />

            {/* response: rail -> caller, early and in green */}
            <line
              x1="322"
              y1="176"
              x2="322"
              y2="101"
              stroke={C.healthy}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <path d="M318 107 L322 100 L326 107" fill={C.healthy} />
            <circle cx="322" cy="176" r="2.5" fill={C.healthy} />
            <text x="330" y="121" fontSize="8" fill={C.healthy}>
              response returned
            </text>
            <text x="330" y="132" fontSize="7" fill={C.inkDim}>
              200 &middot; early
            </text>

            {/* request node */}
            <rect
              x="98"
              y="156"
              width="104"
              height="40"
              rx="9"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="150"
              y="172"
              textAnchor="middle"
              fontSize="5.5"
              letterSpacing="1.3"
              fill={C.navLabel}
            >
              REQUEST
            </text>
            <text
              x="150"
              y="186"
              textAnchor="middle"
              fontSize="9"
              fill={C.inkDim}
            >
              POST /reviews
            </text>

            {/* handler node */}
            <rect
              x="430"
              y="156"
              width="112"
              height="40"
              rx="9"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="486"
              y="172"
              textAnchor="middle"
              fontSize="5.5"
              letterSpacing="1.3"
              fill={C.navLabel}
            >
              MEDIATOR
            </text>
            <text
              x="486"
              y="186"
              textAnchor="middle"
              fontSize="9"
              fill={C.inkDim}
            >
              ReviewHandler
            </text>

            {/* event node: the lit origin of the fan-out */}
            <rect
              x="592"
              y="156"
              width="112"
              height="40"
              fill={`url(#${ID}lit)`}
            />
            <rect
              x="592"
              y="156"
              width="112"
              height="40"
              rx="9"
              fill="none"
              stroke={C.accent}
              strokeOpacity="0.4"
              strokeWidth="2"
              filter={`url(#${ID}glow)`}
            />
            <rect
              x="592"
              y="156"
              width="112"
              height="40"
              rx="9"
              fill={C.surface}
              stroke={C.accent}
              strokeWidth="1.25"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="648"
              y="172"
              textAnchor="middle"
              fontSize="5.5"
              letterSpacing="1.3"
              fill={C.navLabel}
            >
              BUS &middot; EVENT
            </text>
            <text
              x="648"
              y="186"
              textAnchor="middle"
              fontSize="9"
              fontWeight="600"
              fill={C.accent}
            >
              ReviewCreated
            </text>

            {/* saga node */}
            <rect
              x="804"
              y="156"
              width="108"
              height="40"
              rx="9"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="858"
              y="172"
              textAnchor="middle"
              fontSize="5.5"
              letterSpacing="1.3"
              fill={C.navLabel}
            >
              SAGA
            </text>
            <text
              x="858"
              y="186"
              textAnchor="middle"
              fontSize="9"
              fill={C.inkDim}
            >
              OrderSaga
            </text>

            {/* idle fan-out hop: delivered copy, dashed */}
            <path
              d="M648 196 C 624 226 600 250 584 267"
              fill="none"
              stroke={C.accent}
              strokeOpacity="0.32"
              strokeWidth="1"
              strokeDasharray="4 4"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx="584" cy="267" r="2" fill={C.accent} />

            {/* active fan-out hop: amber at the source, teal on arrival */}
            <path
              d="M648 196 C 672 226 700 248 728 267"
              fill="none"
              stroke={`url(#${ID}flow)`}
              strokeWidth="1.75"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx="728" cy="267" r="2" fill={C.accent} />

            {/* in-flight message: one amber copy of the event, exactly-once */}
            <text
              x="694"
              y="212"
              textAnchor="middle"
              fontSize="6"
              letterSpacing="1.2"
              fill={C.navLabel}
            >
              EXACTLY-ONCE
            </text>
            <rect
              x="652"
              y="218"
              width="84"
              height="20"
              rx="10"
              fill={C.surface}
              stroke={C.amber}
              strokeOpacity="0.9"
              strokeWidth="1.25"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="694"
              y="231"
              textAnchor="middle"
              fontSize="8"
              fontWeight="500"
              fill={C.amber}
            >
              ReviewCreated
            </text>
            <circle cx="712" cy="248" r="2.5" fill={C.amber} />

            {/* two consumers, each on its own service */}
            {[
              { cx: 584, name: "SearchIndexer", svc: "search-svc" },
              { cx: 728, name: "NotifyAuthor", svc: "email-svc" },
            ].map((r) => (
              <g key={r.name}>
                <rect
                  x={r.cx - 60}
                  y="267"
                  width="120"
                  height="40"
                  rx="9"
                  fill={C.surface}
                  fillOpacity="0.6"
                  stroke={C.cardBorder}
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                />
                <text
                  x={r.cx}
                  y="281"
                  textAnchor="middle"
                  fontSize="5.5"
                  letterSpacing="1.2"
                  fill={C.navLabel}
                >
                  CONSUMER
                </text>
                <text
                  x={r.cx}
                  y="293"
                  textAnchor="middle"
                  fontSize="8.5"
                  fill={C.inkDim}
                >
                  {r.name}
                </text>
                <text
                  x={r.cx}
                  y="303"
                  textAnchor="middle"
                  fontSize="6"
                  fill={C.faint}
                >
                  {r.svc}
                </text>
              </g>
            ))}

            {/* saga state strip: advancing one state */}
            <line
              x1="858"
              y1="196"
              x2="858"
              y2="232"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="858"
              y="227"
              textAnchor="middle"
              fontSize="6"
              letterSpacing="1.1"
              fill={C.navLabel}
            >
              ADVANCES ONE STATE
            </text>
            {[
              {
                x: 772,
                w: 42,
                label: "Draft",
                state: "done" as const,
              },
              {
                x: 826,
                w: 52,
                label: "Checked",
                state: "active" as const,
              },
              {
                x: 890,
                w: 58,
                label: "Published",
                state: "next" as const,
              },
            ].map((s, i) => (
              <g key={s.label}>
                {i > 0 && (
                  <text
                    x={s.x - 7}
                    y="251"
                    textAnchor="middle"
                    fontSize="8"
                    fill={C.faint}
                  >
                    &rarr;
                  </text>
                )}
                <rect
                  x={s.x}
                  y="238"
                  width={s.w}
                  height="18"
                  rx="5"
                  fill={s.state === "active" ? C.accent : "none"}
                  fillOpacity={s.state === "active" ? 0.06 : 1}
                  stroke={s.state === "active" ? C.accent : C.inkFaint}
                  strokeOpacity={s.state === "active" ? 0.6 : 1}
                  strokeWidth="1"
                  strokeDasharray={s.state === "next" ? "3 3" : undefined}
                  vectorEffect="non-scaling-stroke"
                />
                <text
                  x={s.x + s.w / 2}
                  y="250"
                  textAnchor="middle"
                  fontSize="7.5"
                  fill={s.state === "active" ? C.accent : C.inkDim}
                >
                  {s.label}
                </text>
              </g>
            ))}
          </svg>

          <p className="text-cc-ink-dim border-cc-card-border mt-5 border-t pt-4 text-sm text-pretty">
            The response is returned early. The handler, the published event and
            its two consumers, and the saga all run after it.
          </p>
        </div>

        {/* the five beats, tied to the sequence above */}
        <div className="mt-8 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
          {BEATS.map((beat) => (
            <div
              key={beat.n}
              className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-2xl border p-4 transition-colors sm:p-5"
            >
              <div className="flex items-center gap-2">
                <span className="text-cc-accent font-mono text-[0.6rem]">
                  {beat.n}
                </span>
                <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                  {beat.eyebrow}
                </span>
              </div>
              <h3 className="font-heading text-cc-heading text-h6 mt-2 leading-snug font-semibold text-balance">
                {beat.headline}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm text-pretty">
                {beat.line}
              </p>
            </div>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
