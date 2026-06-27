/**
 * Mocha messaging flow, version 5: "Broker hub".
 *
 * A publish/subscribe hub-and-spoke picture. The transport is one central
 * BROKER module that lists the four interchangeable options (RabbitMQ, Apache
 * Kafka, Azure Service Bus, Amazon SQS). On the left the Fusion gateway is the
 * public API surface (a request comes in, 200 OK goes back out) and sits above a
 * small horizon line; below it the orders-svc subgraph, which the gateway
 * resolves against, publishes the OrderPlaced event with one arrow into the
 * broker. From the broker, clean orthogonal spokes fan out to the right to the
 * handler services, a tidy vertical column of equal cards, each on its own
 * service across a service boundary: publish/subscribe (two subscribers, one
 * batch) on the emphasised centre spoke, request/reply (GetQuote, whose reply
 * travels back to the broker) on top, and send (ShipOrder, one-way) below. A
 * full-width OrderFulfillment saga holds state across the order lifecycle with a
 * timeout compensation path. Unlike the centred spine of v1, the left-to-right
 * pipeline of v2, or the numbered bands of v3, here a single broker in the
 * middle is the hub and the services hang off its spokes. Fully static, no
 * motion. Three font sizes only. Every svg id is prefixed "mg5-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// The shared vertical axis: subgraph, broker and the centre spoke all sit here.
const MID = 248;

// The left column: the gateway above a small horizon, the subgraph below it.
const LEFT_X = 40;
const LEFT_W = 216;
const GW_Y = 70;
const GW_H = 64;
const HORIZON_Y = 175;
const SUB_Y = 216;
const SUB_H = 64;

// The central broker hub, the tall module that lists the four transports.
const BROKER_X = 378;
const BROKER_W = 216;
const BROKER_Y = 70;
const BROKER_H = 356;
const BROKER_R = BROKER_X + BROKER_W;

// The service boundary the broker delivers across to the handler services.
const BOUNDARY_X = 655;

// The handler services: a tidy vertical column of equal cards on the right.
const CARD_X = 716;
const CARD_W = 244;
const CARD_H = 100;
const CARD_TOPS = [70, 198, 326] as const;

// The full-width OrderFulfillment saga panel along the bottom.
const SAGA_X = 40;
const SAGA_Y = 452;
const SAGA_W = 920;
const SAGA_H = 176;

const RADIUS = 12;

export function MessagingGraphicV5() {
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
        viewBox="0 0 1000 656"
        width="100%"
        role="img"
        aria-label="A hub-and-spoke messaging diagram. On the left the Fusion gateway is the public API surface: a request comes in and 200 OK goes back out, above a small horizon line. Below it the gateway resolves against the orders-svc subgraph, which after responding publishes the OrderPlaced event with one arrow into a central broker. The broker is one pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS. From the broker, orthogonal spokes fan out to the right across a service boundary to three handler services, each on its own service: request and reply (GetQuote, whose reply returns to the broker), publish and subscribe with two subscribers (one batch) on the emphasised centre spoke, and send (ShipOrder, one-way). A full-width OrderFulfillment saga holds state across Placed, AwaitingPayment, Paid, and Shipped, with a payment timeout compensation path to Cancelled."
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

        {/* LEFT, ABOVE THE HORIZON: the gateway, the public API surface */}
        <text
          x={100}
          y={40}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          REQUEST
        </text>
        <line
          x1={100}
          y1={46}
          x2={100}
          y2={GW_Y}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg5-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={180}
          y={40}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1={180}
          y1={GW_Y}
          x2={180}
          y2={46}
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg5-green)"
          vectorEffect="non-scaling-stroke"
        />
        <NodeCard
          x={LEFT_X}
          y={GW_Y}
          w={LEFT_W}
          h={GW_H}
          label="GATEWAY"
          name="Fusion"
        />

        {/* The gateway resolves down across a small horizon into the subgraph */}
        <line
          x1={148}
          y1={GW_Y + GW_H + 6}
          x2={148}
          y2={SUB_Y - 6}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg5-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={162}
          y={180}
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          RESOLVES
        </text>
        <line
          x1={LEFT_X}
          y1={HORIZON_Y}
          x2={LEFT_X + LEFT_W}
          y2={HORIZON_Y}
          stroke="rgba(245, 241, 234, 0.2)"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={LEFT_X}
          y={167}
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          API SURFACE
        </text>
        <text
          x={LEFT_X}
          y={192}
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          SERVICES
        </text>

        {/* LEFT, BELOW THE HORIZON: the orders-svc subgraph */}
        <NodeCard
          x={LEFT_X}
          y={SUB_Y}
          w={LEFT_W}
          h={SUB_H}
          label="SUBGRAPH"
          name="orders-svc"
        />
        <text x={LEFT_X} y={302} fontSize={FS.label} fill={C.faint}>
          after 200 OK the subgraph
        </text>
        <text x={LEFT_X} y={316} fontSize={FS.label} fill={C.faint}>
          publishes a domain event
        </text>

        {/* PRIMARY EVENT: the subgraph publishes into the broker */}
        <circle cx={LEFT_X + LEFT_W} cy={MID} r="2.5" fill={C.accent} />
        <line
          x1={LEFT_X + LEFT_W}
          y1={MID}
          x2={BROKER_X}
          y2={MID}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg5-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={(LEFT_X + LEFT_W + BROKER_X) / 2}
          y={MID - 10}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="0.6"
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>

        {/* THE BROKER HUB: one pluggable transport, four interchangeable options */}
        <rect
          x={BROKER_X}
          y={BROKER_Y}
          width={BROKER_W}
          height={BROKER_H}
          rx={RADIUS}
          fill={C.surface}
          fillOpacity="0.6"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={BROKER_X + 18}
          y={BROKER_Y + 26}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          BROKER
        </text>
        <text
          x={BROKER_X + 18}
          y={BROKER_Y + 42}
          fontSize={FS.label}
          fill={C.faint}
        >
          one pluggable transport
        </text>
        {TRANSPORTS.map((t) => (
          <BrokerChip key={t.name} cy={t.cy} name={t.name} />
        ))}

        {/* SERVICE BOUNDARY: the broker delivers across it to the services */}
        <text
          x={BOUNDARY_X}
          y={62}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SERVICE BOUNDARY
        </text>
        <line
          x1={BOUNDARY_X}
          y1={CARD_TOPS[0]}
          x2={BOUNDARY_X}
          y2={CARD_TOPS[2] + CARD_H}
          stroke={C.cardBorder}
          strokeWidth="1"
          strokeDasharray="4 4"
          vectorEffect="non-scaling-stroke"
        />

        {/* SPOKE 1, top: request / reply, with a reply back to the broker */}
        <line
          x1={BROKER_R}
          y1={114}
          x2={CARD_X}
          y2={114}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg5-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={CARD_X}
          y1={126}
          x2={BROKER_R}
          y2={126}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg5-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text x={642} y={140} fontSize={FS.label} fill={C.faint}>
          reply
        </text>

        {/* SPOKE 2, centre: publish / subscribe, the emphasised primary path */}
        <line
          x1={BROKER_R}
          y1={MID}
          x2={CARD_X}
          y2={MID}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg5-teal)"
          vectorEffect="non-scaling-stroke"
        />

        {/* SPOKE 3, bottom: send, a one-way command */}
        <line
          x1={BROKER_R}
          y1={376}
          x2={CARD_X}
          y2={376}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg5-ink)"
          vectorEffect="non-scaling-stroke"
        />

        {/* The three handler services, each on its own service */}
        <PatternCard top={CARD_TOPS[0]} kicker="REQUEST / REPLY" />
        <HandlerRow
          top={CARD_TOPS[0]}
          row={0}
          name="GetQuote"
          service="pricing-svc"
        />
        <text
          x={CARD_X + 18}
          y={CARD_TOPS[0] + 82}
          fontSize={FS.label}
          fill={C.faint}
        >
          one handler, synchronous reply
        </text>

        <PatternCard top={CARD_TOPS[1]} kicker="PUBLISH / SUBSCRIBE" />
        <HandlerRow
          top={CARD_TOPS[1]}
          row={0}
          name="UpdateInventory"
          service="inventory-svc"
          primary
        />
        <HandlerRow
          top={CARD_TOPS[1]}
          row={1}
          name="NotifyCustomer"
          service="notify-svc"
          batch
        />

        <PatternCard top={CARD_TOPS[2]} kicker="SEND" />
        <HandlerRow
          top={CARD_TOPS[2]}
          row={0}
          name="ShipOrder"
          service="shipping-svc"
        />
        <text
          x={CARD_X + 18}
          y={CARD_TOPS[2] + 82}
          fontSize={FS.label}
          fill={C.faint}
        >
          one-way command, no reply
        </text>

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
          y={SAGA_Y + 30}
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text
          x={SAGA_X + 22}
          y={SAGA_Y + 46}
          fontSize={FS.label}
          fill={C.navLabel}
        >
          stateful coordinator across the order lifecycle
        </text>
        <rect
          x={SAGA_X + SAGA_W - 134}
          y={SAGA_Y + 14}
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
          y={SAGA_Y + 29}
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
              y1={SAGA_Y + 95}
              x2={tr.x2}
              y2={SAGA_Y + 95}
              stroke={C.faint}
              strokeWidth="1.25"
              markerEnd="url(#mg5-ink)"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(tr.x1 + tr.x2) / 2}
              y={SAGA_Y + 88}
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
          x1={385}
          y1={SAGA_Y + 114}
          x2={385}
          y2={SAGA_Y + 138}
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg5-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x={397} y={SAGA_Y + 131} fontSize={FS.label} fill={C.coral}>
          payment timeout
        </text>
        <rect
          x={310}
          y={SAGA_Y + 138}
          width="150"
          height="30"
          rx="8"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={385}
          y={SAGA_Y + 157}
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
        >
          Cancelled
        </text>
        <text
          x={476}
          y={SAGA_Y + 157}
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

interface NodeCardProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly label: string;
  readonly name: string;
}

/** A left-anchored service node (gateway, subgraph): a type label over a name. */
function NodeCard({ x, y, w, h, label, name }: NodeCardProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        rx={RADIUS}
        fill={C.surface}
        fillOpacity="0.6"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 18}
        y={y + 24}
        fontSize={FS.label}
        letterSpacing="1.6"
        fill={C.navLabel}
      >
        {label}
      </text>
      <text
        x={x + 18}
        y={y + 44}
        fontSize={FS.name}
        fontWeight="600"
        fill={C.heading}
      >
        {name}
      </text>
    </g>
  );
}

interface BrokerChipProps {
  readonly cy: number;
  readonly name: string;
}

/** One interchangeable transport, listed as a chip inside the broker hub. */
function BrokerChip({ cy, name }: BrokerChipProps) {
  return (
    <g>
      <rect
        x={BROKER_X + 16}
        y={cy - 17}
        width={BROKER_W - 32}
        height="34"
        rx="9"
        fill={C.surface}
        fillOpacity="0.7"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={BROKER_X + BROKER_W / 2}
        y={cy + 4}
        textAnchor="middle"
        fontSize={FS.name}
        fill={C.inkDim}
      >
        {name}
      </text>
    </g>
  );
}

interface PatternCardProps {
  readonly top: number;
  readonly kicker: string;
}

/** The shared frame for one handler service card: a kicker over its handlers. */
function PatternCard({ top, kicker }: PatternCardProps) {
  return (
    <g>
      <rect
        x={CARD_X}
        y={top}
        width={CARD_W}
        height={CARD_H}
        rx={RADIUS}
        fill={C.surface}
        fillOpacity="0.5"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={CARD_X + 18}
        y={top + 22}
        fontSize={FS.label}
        letterSpacing="1.4"
        fill={C.navLabel}
      >
        {kicker}
      </text>
    </g>
  );
}

interface HandlerRowProps {
  readonly top: number;
  readonly row: number;
  readonly name: string;
  readonly service: string;
  readonly primary?: boolean;
  readonly batch?: boolean;
}

/** One handler, on its own service, inside a handler service card. */
function HandlerRow({
  top,
  row,
  name,
  service,
  primary,
  batch,
}: HandlerRowProps) {
  const x = CARD_X + 16;
  const y = top + 36 + row * 30;
  const w = CARD_W - 32;
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height="24"
        rx="8"
        fill={C.surface}
        fillOpacity="0.7"
        stroke={primary ? C.accent : C.cardBorder}
        strokeOpacity={primary ? 0.55 : 1}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 12}
        y={y + 16}
        fontSize={FS.name}
        fill={primary ? C.accent : C.heading}
      >
        {name}
      </text>
      {batch && (
        <>
          <rect
            x={x + 104}
            y={y + 5}
            width="38"
            height="15"
            rx="7"
            fill="none"
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x={x + 123}
            y={y + 15}
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
        y={y + 16}
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
          y={SAGA_Y + 70}
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
        y={SAGA_Y + 76}
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
        y={SAGA_Y + 100}
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
  { id: "mg5-teal", fill: "#5eead4" },
  { id: "mg5-green", fill: "#34d399" },
  { id: "mg5-coral", fill: "#f0786a" },
  { id: "mg5-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

interface Transport {
  readonly name: string;
  readonly cy: number;
}

/** The four interchangeable transports, listed as chips inside the broker. */
const TRANSPORTS: readonly Transport[] = [
  { name: "RabbitMQ", cy: 143 },
  { name: "Apache Kafka", cy: 213 },
  { name: "Azure Service Bus", cy: 283 },
  { name: "Amazon SQS", cy: 353 },
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
