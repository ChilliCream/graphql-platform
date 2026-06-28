/**
 * Mocha messaging flow, version 6: "Sequence diagram".
 *
 * A classic UML sequence diagram. Six participants run across the top, each with
 * a dotted lifeline dropping down and time flowing downward: Client, the Fusion
 * gateway, the orders-svc subgraph, one pluggable Transport, the subscriber
 * handlers, and the OrderFulfillment saga. Above a faint divider the synchronous
 * part plays out: the Client requests, the gateway resolves against orders-svc,
 * the data returns, and a green 200 OK goes back fast. The Client and gateway
 * lifelines then terminate, because their work is done. Below the divider, after
 * the response, the asynchronous part runs in teal as the primary path: orders-
 * svc publishes OrderPlaced onto the Transport (RabbitMQ, Apache Kafka, Azure
 * Service Bus, or Amazon SQS), which delivers it across a service boundary to the
 * handlers in three patterns (publish/subscribe, request/reply, send). On the
 * right the saga advances its state machine, Placed -> AwaitingPayment -> Paid
 * (current) -> Shipped, with a coral payment-timeout branch to Cancelled. Fully
 * static, no motion. Three font sizes only. Every svg id is prefixed "mg6-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// The six participant lifelines, evenly spaced across the top.
const LIFE = {
  client: 86,
  gateway: 240,
  orders: 394,
  transport: 548,
  handlers: 702,
  saga: 870,
} as const;

const LIFE_TOP = 70;
const BOX_W = 138;

// The faint divider: synchronous above, asynchronous below.
const DIVIDER_Y = 290;

// The saga state machine lives in a framed panel on its lifeline.
const PANEL_X = 796;
const PANEL_W = 160;
const PANEL_Y = 418;
const PANEL_H = 278;
const PILL_CX = 870;
const PILL_W = 128;

const RADIUS = 12;

export function MessagingGraphicV6() {
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
        viewBox="0 0 1000 720"
        width="100%"
        role="img"
        aria-label="A UML sequence diagram with six participants across the top, each with a lifeline and time flowing downward: Client, the Fusion gateway, the orders-svc subgraph, a pluggable Transport, the subscriber handlers, and the OrderFulfillment saga. Above a divider the synchronous exchange runs: the Client sends a request to the gateway, the gateway resolves against orders-svc, the data returns, and a green 200 OK is returned to the Client. The Client and gateway lifelines then end. Below the divider, after the response, the asynchronous part runs as the primary teal path: orders-svc publishes the OrderPlaced event onto the Transport, which can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS, and the Transport delivers it across a service boundary to the handlers in three patterns: publish and subscribe with UpdateInventory and NotifyCustomer in one batch, request and reply with GetQuote, and send with ShipOrder. On the right the OrderFulfillment saga advances its state machine from Placed to AwaitingPayment to Paid, the current state, to Shipped, with a coral payment-timeout branch to the Cancelled compensation state."
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

        {/* LIFELINES: a dotted vertical guide per participant, time goes down */}
        {LIFELINES.map((l) => (
          <g key={l.key}>
            <line
              x1={l.x}
              y1={LIFE_TOP}
              x2={l.x}
              y2={l.end}
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeDasharray="1 5"
              vectorEffect="non-scaling-stroke"
            />
            {l.terminate && (
              <g
                stroke={C.faint}
                strokeWidth="1.25"
                vectorEffect="non-scaling-stroke"
              >
                <line x1={l.x - 5} y1={l.end - 5} x2={l.x + 5} y2={l.end + 5} />
                <line x1={l.x - 5} y1={l.end + 5} x2={l.x + 5} y2={l.end - 5} />
              </g>
            )}
          </g>
        ))}

        {/* ACTIVATION BARS: the thin execution occurrences on each lifeline */}
        {ACTIVATIONS.map((a, i) => (
          <rect
            key={i}
            x={a.x - 4.5}
            y={a.y1}
            width="9"
            height={a.y2 - a.y1}
            rx="2"
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
        ))}

        {/* PARTICIPANT HEADERS: a role label over an entity name */}
        {LIFELINES.map((l) => (
          <Participant key={l.key} x={l.x} label={l.label} name={l.name} />
        ))}

        {/* TRANSPORT NOTE: one pluggable bus, four interchangeable options */}
        <line
          x1={LIFE.transport}
          y1={LIFE_TOP - 2}
          x2={LIFE.transport}
          y2={94}
          stroke={C.cardBorder}
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <rect
          x={512}
          y={94}
          width={320}
          height={96}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.92"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={530}
          y={120}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          PLUGGABLE TRANSPORT
        </text>
        <text x={530} y={136} fontSize={FS.label} fill={C.faint}>
          same code on any broker
        </text>
        {TRANSPORTS.map((t) => (
          <text key={t.name} x={t.x} y={t.y} fontSize={FS.name} fill={C.inkDim}>
            {t.name}
          </text>
        ))}

        {/* THE SYNCHRONOUS / ASYNCHRONOUS DIVIDER */}
        <text
          x={40}
          y={278}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SYNCHRONOUS
        </text>
        <line
          x1={40}
          y1={DIVIDER_Y}
          x2={386}
          y2={DIVIDER_Y}
          stroke={C.inkFaint}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={614}
          y1={DIVIDER_Y}
          x2={964}
          y2={DIVIDER_Y}
          stroke={C.inkFaint}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={500}
          y={294}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.8"
          fill={C.navLabel}
        >
          AFTER THE RESPONSE, ASYNCHRONOUS
        </text>
        <text x={40} y={312} fontSize={FS.label} fill={C.faint}>
          events and the saga run after the 200 OK
        </text>

        {/* SAGA PANEL: the stateful coordinator's state machine */}
        <rect
          x={PANEL_X}
          y={PANEL_Y}
          width={PANEL_W}
          height={PANEL_H}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.3"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={PILL_CX}
          y={442}
          textAnchor="middle"
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          Order lifecycle
        </text>

        {/* SYNCHRONOUS MESSAGES, above the divider */}
        <Message
          x1={91}
          x2={235}
          y={120}
          label="request"
          stroke={C.inkDim}
          labelFill={C.navLabel}
          marker="mg6-ink"
        />
        <Message
          x1={245}
          x2={389}
          y={168}
          label="resolve order"
          stroke={C.inkDim}
          labelFill={C.navLabel}
          marker="mg6-ink"
        />
        <Message
          x1={389}
          x2={245}
          y={200}
          label="data"
          stroke={C.faint}
          labelFill={C.faint}
          marker="mg6-ink"
          dashed
        />
        <Message
          x1={235}
          x2={91}
          y={232}
          label="200 OK"
          stroke={C.healthy}
          labelFill={C.healthy}
          marker="mg6-green"
          dashed
        />

        {/* ASYNCHRONOUS MESSAGES, below the divider */}
        <Message
          x1={399}
          x2={543}
          y={340}
          label="publish OrderPlaced"
          stroke={C.accent}
          labelFill={C.accent}
          marker="mg6-teal"
        />
        <Message
          x1={553}
          x2={697}
          y={388}
          label="deliver OrderPlaced"
          stroke={C.accent}
          labelFill={C.accent}
          marker="mg6-teal"
        />
        <Message
          x1={697}
          x2={553}
          y={424}
          label="reply: quote"
          stroke={C.faint}
          labelFill={C.faint}
          marker="mg6-ink"
          dashed
        />
        <Message
          x1={553}
          x2={PANEL_X + 10}
          y={485}
          label="OrderPlaced"
          stroke={C.inkDim}
          labelFill={C.navLabel}
          marker="mg6-ink"
        />
        <Message
          x1={553}
          x2={PANEL_X + 10}
          y={581}
          label="PaymentReceived"
          stroke={C.inkDim}
          labelFill={C.navLabel}
          marker="mg6-ink"
        />
        <Message
          x1={PANEL_X + 10}
          x2={707}
          y={627}
          label="send ShipOrder"
          stroke={C.inkDim}
          labelFill={C.navLabel}
          marker="mg6-ink"
        />

        {/* SAGA STATE MACHINE: pills on the lifeline, Paid is current */}
        {SAGA_CONNECTORS.map((y, i) => (
          <line
            key={i}
            x1={PILL_CX}
            y1={y}
            x2={PILL_CX}
            y2={y + 16}
            stroke={C.inkFaint}
            strokeWidth="1.25"
            markerEnd="url(#mg6-ink)"
            vectorEffect="non-scaling-stroke"
          />
        ))}
        {SAGA_STATES.map((s) => (
          <StatePill key={s.name} top={s.top} name={s.name} kind={s.kind} />
        ))}

        {/* COMPENSATION: a payment timeout branches to Cancelled */}
        <path
          d="M934 531 H948 V673 H934"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.85"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg6-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={PILL_CX}
          y={652}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.coral}
        >
          on payment timeout
        </text>

        {/* DELIVER FAN-OUT NOTE: each handler on its own service */}
        <rect
          x={40}
          y={452}
          width={330}
          height={206}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.5"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={62}
          y={478}
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          DELIVER FANS OUT
        </text>
        <text x={62} y={496} fontSize={FS.label} fill={C.faint}>
          each handler on its own service
        </text>
        {FANOUT.map((f) => (
          <g key={f.kicker}>
            <text
              x={62}
              y={f.y}
              fontSize={FS.label}
              letterSpacing="1.3"
              fill={C.navLabel}
            >
              {f.kicker}
            </text>
            <text x={62} y={f.y + 18} fontSize={FS.name} fill={C.inkDim}>
              {f.names}
            </text>
            <text x={62} y={f.y + 33} fontSize={FS.label} fill={C.faint}>
              {f.note}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}

interface ParticipantProps {
  readonly x: number;
  readonly label: string;
  readonly name: string;
}

/** One participant header at the top of a lifeline: a role label over a name. */
function Participant({ x, label, name }: ParticipantProps) {
  return (
    <g>
      <rect
        x={x - BOX_W / 2}
        y={12}
        width={BOX_W}
        height={56}
        rx={RADIUS}
        fill={C.surface}
        fillOpacity="0.6"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x}
        y={36}
        textAnchor="middle"
        fontSize={FS.label}
        letterSpacing="1.5"
        fill={C.navLabel}
      >
        {label}
      </text>
      <text
        x={x}
        y={54}
        textAnchor="middle"
        fontSize={FS.name}
        fontWeight="600"
        fill={C.heading}
      >
        {name}
      </text>
    </g>
  );
}

interface MessageProps {
  readonly x1: number;
  readonly x2: number;
  readonly y: number;
  readonly label: string;
  readonly stroke: string;
  readonly labelFill: string;
  readonly marker: string;
  readonly dashed?: boolean;
}

/** One horizontal message arrow with its label centred above the line. */
function Message({
  x1,
  x2,
  y,
  label,
  stroke,
  labelFill,
  marker,
  dashed,
}: MessageProps) {
  return (
    <g>
      <line
        x1={x1}
        y1={y}
        x2={x2}
        y2={y}
        stroke={stroke}
        strokeWidth={dashed ? 1.25 : 1.5}
        strokeDasharray={dashed ? "4 3" : undefined}
        markerEnd={`url(#${marker})`}
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={(x1 + x2) / 2}
        y={y - 9}
        textAnchor="middle"
        fontSize={FS.label}
        letterSpacing="0.3"
        fill={labelFill}
      >
        {label}
      </text>
    </g>
  );
}

interface StatePillProps {
  readonly top: number;
  readonly name: string;
  readonly kind: "done" | "current" | "next" | "comp";
}

/** One saga state pill on the saga lifeline; Paid (current) gets the accent. */
function StatePill({ top, name, kind }: StatePillProps) {
  const current = kind === "current";
  const next = kind === "next";
  const comp = kind === "comp";
  const border = current
    ? C.accent
    : comp
      ? C.coral
      : next
        ? C.inkFaint
        : C.cardBorder;
  const text = current ? C.accent : comp ? C.coral : next ? C.faint : C.inkDim;
  return (
    <g>
      {current && (
        <text
          x={PILL_CX}
          y={top - 6}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.accent}
        >
          CURRENT
        </text>
      )}
      <rect
        x={PILL_CX - PILL_W / 2}
        y={top}
        width={PILL_W}
        height={30}
        rx="9"
        fill={current ? C.accent : "none"}
        fillOpacity={current ? 0.1 : 1}
        stroke={border}
        strokeOpacity={current ? 0.7 : 1}
        strokeWidth="1"
        strokeDasharray={next || comp ? "3 3" : undefined}
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={PILL_CX}
        y={top + 20}
        textAnchor="middle"
        fontSize={FS.name}
        fontWeight={current ? 600 : 400}
        fill={text}
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
  { id: "mg6-teal", fill: "#5eead4" },
  { id: "mg6-green", fill: "#34d399" },
  { id: "mg6-coral", fill: "#f0786a" },
  { id: "mg6-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

interface Lifeline {
  readonly key: string;
  readonly x: number;
  readonly label: string;
  readonly name: string;
  readonly end: number;
  readonly terminate: boolean;
}

/** The six participants. Client and gateway terminate once the 200 OK returns;
 * orders-svc terminates after it has published; the rest run to the bottom. */
const LIFELINES: readonly Lifeline[] = [
  {
    key: "client",
    x: LIFE.client,
    label: "CALLER",
    name: "Client",
    end: 256,
    terminate: true,
  },
  {
    key: "gateway",
    x: LIFE.gateway,
    label: "GATEWAY",
    name: "Fusion",
    end: 256,
    terminate: true,
  },
  {
    key: "orders",
    x: LIFE.orders,
    label: "SUBGRAPH",
    name: "orders-svc",
    end: 356,
    terminate: true,
  },
  {
    key: "transport",
    x: LIFE.transport,
    label: "TRANSPORT",
    name: "message bus",
    end: 690,
    terminate: false,
  },
  {
    key: "handlers",
    x: LIFE.handlers,
    label: "SUBSCRIBERS",
    name: "handlers",
    end: 690,
    terminate: false,
  },
  {
    key: "saga",
    x: LIFE.saga,
    label: "SAGA",
    name: "OrderFulfillment",
    end: PANEL_Y,
    terminate: false,
  },
];

interface Activation {
  readonly x: number;
  readonly y1: number;
  readonly y2: number;
}

/** The execution occurrences: orders-svc shows two, the sync resolve then the
 * later async publish, to make the after-the-response timing explicit. */
const ACTIVATIONS: readonly Activation[] = [
  { x: LIFE.client, y1: 120, y2: 236 },
  { x: LIFE.gateway, y1: 120, y2: 240 },
  { x: LIFE.orders, y1: 168, y2: 210 },
  { x: LIFE.orders, y1: 330, y2: 356 },
  { x: LIFE.transport, y1: 336, y2: 588 },
  { x: LIFE.handlers, y1: 384, y2: 436 },
  { x: LIFE.handlers, y1: 619, y2: 635 },
];

interface TransportLabel {
  readonly name: string;
  readonly x: number;
  readonly y: number;
}

/** The four interchangeable transports, listed two-up inside the note. */
const TRANSPORTS: readonly TransportLabel[] = [
  { name: "RabbitMQ", x: 530, y: 162 },
  { name: "Azure Service Bus", x: 690, y: 162 },
  { name: "Apache Kafka", x: 530, y: 182 },
  { name: "Amazon SQS", x: 690, y: 182 },
];

interface SagaStatePill {
  readonly name: string;
  readonly top: number;
  readonly kind: "done" | "current" | "next" | "comp";
}

/** The saga's lifecycle, top to bottom, with Paid as the current state. */
const SAGA_STATES: readonly SagaStatePill[] = [
  { name: "Placed", top: 470, kind: "done" },
  { name: "AwaitingPayment", top: 516, kind: "done" },
  { name: "Paid", top: 566, kind: "current" },
  { name: "Shipped", top: 612, kind: "next" },
  { name: "Cancelled", top: 658, kind: "comp" },
];

/** The vertical transitions between consecutive happy-path saga states. */
const SAGA_CONNECTORS: readonly number[] = [500, 546, 596];

interface FanoutRow {
  readonly kicker: string;
  readonly names: string;
  readonly note: string;
  readonly y: number;
}

/** The three delivery patterns the OrderPlaced event drives. */
const FANOUT: readonly FanoutRow[] = [
  {
    kicker: "PUBLISH / SUBSCRIBE",
    names: "UpdateInventory, NotifyCustomer",
    note: "fan-out to every subscriber, one batch",
    y: 520,
  },
  {
    kicker: "REQUEST / REPLY",
    names: "GetQuote",
    note: "one handler, a reply travels back",
    y: 566,
  },
  {
    kicker: "SEND",
    names: "ShipOrder",
    note: "one-way command, issued by the saga",
    y: 612,
  },
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
