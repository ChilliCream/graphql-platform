/**
 * Mocha messaging flow, version 2: "Left-to-right pipeline".
 *
 * A horizontal pipeline framed by two full-width bars. The top bar is the
 * Fusion gateway, the public API surface (a request comes in, 200 OK goes back
 * out). A horizon line below it separates that public surface from the services.
 * On one shared baseline the services read left to right: the orders-svc
 * subgraph (which the gateway resolves against) publishes the OrderPlaced event
 * onto a single pluggable transport (the four interchangeable options listed in
 * one tidy module), and across a service boundary the transport delivers to the
 * three messaging patterns, stacked as equal cards on the right: publish/
 * subscribe (two subscribers, one batch), request/reply, and send. A full-width
 * OrderFulfillment saga holds state across the order lifecycle with a timeout
 * compensation path. Fully static, no motion. Three font sizes only. Every svg
 * id is prefixed "mg2-". This is deliberately not the centered vertical spine
 * of version 1: the flow here runs horizontally, bracketed by a header bar and a
 * footer bar.
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// Page margins and the horizon that splits the API surface from the services.
const MARGIN_L = 40;
const MARGIN_R = 960;
const HORIZON_Y = 124;

// The full-width gateway bar (the public API surface) above the horizon.
const GW_Y = 44;
const GW_H = 58;
const RESOLVE_X = 140;

// The services baseline: subgraph and transport share one tall card height,
// centred on the same midline as the stack of pattern cards on the right.
const Y_MID = 326;
const GA_Y = 226;
const GA_H = 200;
const SUB_X = 40;
const TRN_X = 360;
const COL_W = 200;

// The three pattern cards on the right share a height and sit on a clean stack.
const PAT_X = 680;
const PAT_W = 280;
const PAT_H = 84;
const PAT_TOPS = [180, 284, 388] as const;
const BOUNDARY_X = 620;

// The full-width OrderFulfillment saga panel along the bottom.
const SAGA_X = 40;
const SAGA_Y = 512;
const SAGA_W = 920;
const SAGA_H = 200;

const RADIUS = 12;

export function MessagingGraphicV2() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-3xl border p-5 backdrop-blur-sm sm:p-8">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            Messaging
          </p>
          <h3 className="font-heading text-cc-heading text-h6 mt-1 leading-snug font-semibold">
            How a message flows
          </h3>
        </div>
        <span className="text-cc-ink-dim inline-flex items-center gap-1.5 font-mono text-[0.55rem] tracking-[0.08em] uppercase">
          <span
            aria-hidden="true"
            className="inline-block size-2 rounded-full"
            style={{ backgroundColor: C.accent }}
          />
          primary event flow
        </span>
      </header>

      <svg
        viewBox="0 0 1000 744"
        width="100%"
        role="img"
        aria-label="A full-width Fusion gateway bar is the public API surface: a request comes in and 200 OK goes back out. A horizon line separates it from the services below. Reading left to right, the gateway resolves against the orders-svc subgraph, which publishes the OrderPlaced event onto one pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS. Across a service boundary the transport delivers to three messaging patterns, each on its own service: publish and subscribe with two subscribers (one batch), request and reply, and send. A full-width OrderFulfillment saga holds state across Placed, AwaitingPayment, Paid, and Shipped, with a payment timeout compensation path to Cancelled."
        className="mt-5"
        style={{ display: "block", overflow: "visible", fontFamily: MONO }}
      >
        <defs>
          {ARROWS.map((a) => (
            <marker
              key={a.id}
              id={a.id}
              viewBox="0 0 10 10"
              refX="8"
              refY="5"
              markerWidth="6"
              markerHeight="6"
              orient="auto-start-reverse"
            >
              <path d="M0 0 L10 5 L0 10 z" fill={a.fill} />
            </marker>
          ))}
        </defs>

        {/* ABOVE THE HORIZON: the gateway bar, the public API surface */}
        <text
          x="120"
          y="28"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          REQUEST
        </text>
        <line
          x1="120"
          y1="33"
          x2="120"
          y2={GW_Y}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg2-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="200"
          y="28"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1="200"
          y1={GW_Y}
          x2="200"
          y2="33"
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg2-green)"
          vectorEffect="non-scaling-stroke"
        />

        <rect
          x={MARGIN_L}
          y={GW_Y}
          width={SAGA_W}
          height={GW_H}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.6"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="58"
          y={GW_Y + 24}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          GATEWAY
        </text>
        <text
          x="58"
          y={GW_Y + 43}
          fontSize={FS.name}
          fontWeight="600"
          fill={C.heading}
        >
          Fusion
        </text>
        <text
          x={MARGIN_R - 18}
          y={GW_Y + 35}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          the public GraphQL API
        </text>

        {/* The horizon line dividing the public surface from the services */}
        <line
          x1="32"
          y1={HORIZON_Y}
          x2="968"
          y2={HORIZON_Y}
          stroke="rgba(245, 241, 234, 0.2)"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={MARGIN_L}
          y={HORIZON_Y - 6}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          API SURFACE
        </text>
        <text
          x={MARGIN_R}
          y={HORIZON_Y - 6}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          what clients can call
        </text>
        <text
          x={MARGIN_L}
          y={HORIZON_Y + 16}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SERVICES
        </text>
        <text
          x={MARGIN_R}
          y={HORIZON_Y + 16}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          internal, not exposed
        </text>

        {/* Gateway resolves against the subgraph, crossing the horizon */}
        <line
          x1={RESOLVE_X}
          y1={GW_Y + GW_H}
          x2={RESOLVE_X}
          y2={GA_Y}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg2-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={RESOLVE_X + 12}
          y="178"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          RESOLVES
        </text>

        {/* SUBGRAPH: where the event originates */}
        <rect
          x={SUB_X}
          y={GA_Y}
          width={COL_W}
          height={GA_H}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.5"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={SUB_X + 18}
          y={GA_Y + 30}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SUBGRAPH
        </text>
        <text
          x={SUB_X + 18}
          y={GA_Y + 50}
          fontSize={FS.name}
          fontWeight="600"
          fill={C.heading}
        >
          orders-svc
        </text>
        <line
          x1={SUB_X + 18}
          y1={Y_MID}
          x2={SUB_X + COL_W - 18}
          y2={Y_MID}
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text x={SUB_X + 18} y={GA_Y + 142} fontSize={FS.label} fill={C.faint}>
          resolves and responds 200,
        </text>
        <text x={SUB_X + 18} y={GA_Y + 158} fontSize={FS.label} fill={C.faint}>
          then publishes a domain event
        </text>

        {/* PRIMARY EVENT: the subgraph publishes onto the transport */}
        <circle cx={SUB_X + COL_W} cy={Y_MID} r="2.5" fill={C.accent} />
        <line
          x1={SUB_X + COL_W}
          y1={Y_MID}
          x2={TRN_X}
          y2={Y_MID}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg2-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={(SUB_X + COL_W + TRN_X) / 2}
          y={Y_MID - 10}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="0.6"
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>
        <text
          x={(SUB_X + COL_W + TRN_X) / 2}
          y={Y_MID + 18}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.faint}
        >
          after 200 OK
        </text>

        {/* TRANSPORT: one pluggable transport, four interchangeable options */}
        <rect
          x={TRN_X}
          y={GA_Y}
          width={COL_W}
          height={GA_H}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.6"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={TRN_X + 18}
          y={GA_Y + 26}
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          PLUGGABLE TRANSPORT
        </text>
        <text x={TRN_X + 18} y={GA_Y + 44} fontSize={FS.label} fill={C.faint}>
          same code, any one of
        </text>
        {TRANSPORTS.map((t, i) => (
          <g key={t}>
            {i > 0 && (
              <line
                x1={TRN_X + 18}
                y1={GA_Y + 60 + i * 35}
                x2={TRN_X + COL_W - 18}
                y2={GA_Y + 60 + i * 35}
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
            )}
            <text
              x={TRN_X + 18}
              y={GA_Y + 81 + i * 35}
              fontSize={FS.name}
              fill={C.inkDim}
            >
              {t}
            </text>
          </g>
        ))}

        {/* SERVICE BOUNDARY: the transport delivers across it to the patterns */}
        <text
          x={BOUNDARY_X}
          y="168"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SERVICE BOUNDARY
        </text>
        <line
          x1={TRN_X + COL_W}
          y1={Y_MID}
          x2={BOUNDARY_X}
          y2={Y_MID}
          stroke={C.inkDim}
          strokeWidth="1.25"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={BOUNDARY_X}
          y1={patCenter(0)}
          x2={BOUNDARY_X}
          y2={patCenter(2)}
          stroke={C.inkDim}
          strokeWidth="1.25"
          vectorEffect="non-scaling-stroke"
        />
        {PAT_TOPS.map((top, i) => (
          <line
            key={top}
            x1={BOUNDARY_X}
            y1={patCenter(i)}
            x2={PAT_X}
            y2={patCenter(i)}
            stroke={C.inkDim}
            strokeWidth="1.25"
            markerEnd="url(#mg2-ink)"
            vectorEffect="non-scaling-stroke"
          />
        ))}

        {/* The three messaging patterns, each on its own service */}
        {PATTERNS.map((p, i) => (
          <PatternCard
            key={p.kicker}
            top={PAT_TOPS[i]}
            kicker={p.kicker}
            handlers={p.handlers}
            note={p.note}
          />
        ))}

        {/* SAGA: a stateful coordinator across the order lifecycle */}
        <rect
          x={SAGA_X}
          y={SAGA_Y}
          width={SAGA_W}
          height={SAGA_H}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.4"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={SAGA_X + 22}
          y={SAGA_Y + 32}
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text
          x={SAGA_X + 22}
          y={SAGA_Y + 48}
          fontSize={FS.label}
          fill={C.navLabel}
        >
          stateful coordinator across the order lifecycle
        </text>
        <rect
          x={SAGA_X + SAGA_W - 134}
          y={SAGA_Y + 16}
          width="112"
          height="22"
          rx="8"
          fill="none"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={SAGA_X + SAGA_W - 78}
          y={SAGA_Y + 31}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          holds state
        </text>

        {SAGA_TRANSITIONS.map((tr) => (
          <g key={tr.label}>
            <line
              x1={tr.x1}
              y1={SAGA_Y + 103}
              x2={tr.x2}
              y2={SAGA_Y + 103}
              stroke={C.faint}
              strokeWidth="1.25"
              markerEnd="url(#mg2-ink)"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(tr.x1 + tr.x2) / 2}
              y={SAGA_Y + 96}
              textAnchor="middle"
              fontSize={FS.label}
              fill={C.navLabel}
            >
              {tr.label}
            </text>
          </g>
        ))}
        {SAGA_STATES.map((s) => (
          <SagaStateNode key={s.name} cx={s.cx} name={s.name} state={s.state} />
        ))}

        {/* Compensation path: a payment timeout cancels and compensates */}
        <line
          x1="385"
          y1={SAGA_Y + 122}
          x2="385"
          y2={SAGA_Y + 150}
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg2-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="397" y={SAGA_Y + 140} fontSize={FS.label} fill={C.coral}>
          payment timeout
        </text>
        <rect
          x="310"
          y={SAGA_Y + 152}
          width="150"
          height="32"
          rx="8"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="385"
          y={SAGA_Y + 172}
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
        >
          Cancelled
        </text>
        <text
          x="476"
          y={SAGA_Y + 172}
          fontSize={FS.label}
          fill={C.coral}
          fillOpacity="0.8"
        >
          runs compensation
        </text>
      </svg>
    </div>
  );
}

/** Vertical centre of the i-th pattern card. */
function patCenter(i: number) {
  return PAT_TOPS[i] + PAT_H / 2;
}

interface HandlerData {
  readonly name: string;
  readonly service: string;
  readonly batch?: boolean;
}

interface PatternCardProps {
  readonly top: number;
  readonly kicker: string;
  readonly handlers: readonly HandlerData[];
  readonly note?: string;
}

/** One messaging pattern: a kicker over one or two handler rows. */
function PatternCard({ top, kicker, handlers, note }: PatternCardProps) {
  return (
    <g>
      <rect
        x={PAT_X}
        y={top}
        width={PAT_W}
        height={PAT_H}
        rx={RADIUS}
        fill={C.surface}
        fillOpacity="0.5"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={PAT_X + 18}
        y={top + 22}
        fontSize={FS.label}
        letterSpacing="1.4"
        fill={C.navLabel}
      >
        {kicker}
      </text>
      {handlers.map((h, i) => (
        <HandlerRow
          key={h.name}
          y={top + 30 + i * 26}
          name={h.name}
          service={h.service}
          batch={h.batch}
        />
      ))}
      {note && (
        <text x={PAT_X + 20} y={top + 68} fontSize={FS.label} fill={C.faint}>
          {note}
        </text>
      )}
    </g>
  );
}

interface HandlerRowProps {
  readonly y: number;
  readonly name: string;
  readonly service: string;
  readonly batch?: boolean;
}

/** One handler, on its own service, inside a pattern card. */
function HandlerRow({ y, name, service, batch }: HandlerRowProps) {
  const x = PAT_X + 18;
  const w = PAT_W - 36;
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height="20"
        rx="6"
        fill={C.surface}
        fillOpacity="0.7"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x={x + 12} y={y + 14} fontSize={FS.name} fill={C.heading}>
        {name}
      </text>
      {batch && (
        <>
          <rect
            x={x + 122}
            y={y + 4}
            width="34"
            height="13"
            rx="6"
            fill="none"
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x={x + 139}
            y={y + 13}
            textAnchor="middle"
            fontSize={FS.label}
            fill={C.navLabel}
          >
            batch
          </text>
        </>
      )}
      <text
        x={x + w - 12}
        y={y + 14}
        textAnchor="end"
        fontSize={FS.label}
        fill={C.navLabel}
      >
        {service}
      </text>
    </g>
  );
}

interface SagaStateNodeProps {
  readonly cx: number;
  readonly name: string;
  readonly state: "done" | "current" | "next";
}

/** One saga state chip; Paid is the current state and gets the teal accent. */
function SagaStateNode({ cx, name, state }: SagaStateNodeProps) {
  const current = state === "current";
  const next = state === "next";
  return (
    <g>
      {current && (
        <text
          x={cx}
          y={SAGA_Y + 78}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1"
          fill={C.accent}
        >
          CURRENT
        </text>
      )}
      <rect
        x={cx - 75}
        y={SAGA_Y + 84}
        width="150"
        height="38"
        rx="8"
        fill={current ? C.accent : "none"}
        fillOpacity={current ? 0.1 : 1}
        stroke={current ? C.accent : next ? C.inkFaint : C.cardBorder}
        strokeOpacity={current ? 0.7 : 1}
        strokeWidth="1"
        strokeDasharray={next ? "3 3" : undefined}
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={cx}
        y={SAGA_Y + 108}
        textAnchor="middle"
        fontSize={FS.name}
        fontWeight={current ? 600 : 400}
        fill={current ? C.accent : next ? C.faint : C.inkDim}
      >
        {name}
      </text>
    </g>
  );
}

interface Arrow {
  readonly id: string;
  readonly fill: string;
}

const ARROWS: readonly Arrow[] = [
  { id: "mg2-teal", fill: "#5eead4" },
  { id: "mg2-green", fill: "#34d399" },
  { id: "mg2-coral", fill: "#f0786a" },
  { id: "mg2-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

/** The four interchangeable transports, listed as one tidy module. */
const TRANSPORTS: readonly string[] = [
  "RabbitMQ",
  "Apache Kafka",
  "Azure Service Bus",
  "Amazon SQS",
];

interface Pattern {
  readonly kicker: string;
  readonly handlers: readonly HandlerData[];
  readonly note?: string;
}

/** The three messaging patterns the transport delivers to, top to bottom. */
const PATTERNS: readonly Pattern[] = [
  {
    kicker: "PUBLISH / SUBSCRIBE",
    handlers: [
      { name: "UpdateInventory", service: "inventory-svc" },
      { name: "NotifyCustomer", service: "notify-svc", batch: true },
    ],
  },
  {
    kicker: "REQUEST / REPLY",
    handlers: [{ name: "GetQuote", service: "pricing-svc" }],
    note: "one handler, synchronous reply",
  },
  {
    kicker: "SEND",
    handlers: [{ name: "ShipOrder", service: "shipping-svc" }],
    note: "one-way command, no reply",
  },
];

interface SagaState {
  readonly name: string;
  readonly cx: number;
  readonly state: "done" | "current" | "next";
}

/** The saga's state machine, left to right, with Paid as the current state. */
const SAGA_STATES: readonly SagaState[] = [
  { name: "Placed", cx: 155, state: "done" },
  { name: "AwaitingPayment", cx: 385, state: "done" },
  { name: "Paid", cx: 615, state: "current" },
  { name: "Shipped", cx: 845, state: "next" },
];

interface SagaTransition {
  readonly label: string;
  readonly x1: number;
  readonly x2: number;
}

/** The labelled transitions between saga states: events in, a command out. */
const SAGA_TRANSITIONS: readonly SagaTransition[] = [
  { label: "OrderPlaced", x1: 230, x2: 310 },
  { label: "PaymentReceived", x1: 460, x2: 540 },
  { label: "sends ShipOrder", x1: 690, x2: 770 },
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette: navy surfaces, neutral ink, a single teal accent, and
 * the rationed status colours (green for 200 OK, coral for compensation). */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  healthy: "#34d399",
} as const;
