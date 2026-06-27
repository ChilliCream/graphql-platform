/**
 * Mocha messaging flow, version 4: "Message bus spine".
 *
 * The classic message-bus architecture picture: one full-width horizontal bus
 * runs across the middle and every service taps off it. Above a horizon line
 * the Fusion gateway is the public API surface (a request comes in, 200 OK goes
 * back out). Below the horizon the orders-svc subgraph (which the gateway
 * resolves against) publishes the OrderPlaced event straight DOWN onto the bus.
 * The bus itself is the single pluggable transport and carries the four
 * interchangeable options inline (RabbitMQ, Apache Kafka, Azure Service Bus,
 * Amazon SQS). Across a service boundary the subscriber and handler services
 * hang below the bus as a row of equal cards, each reached by a clean vertical
 * drop, each on its own service: publish/subscribe (two subscribers, one
 * batch), request/reply (the reply goes back up to the bus), and send. A
 * full-width OrderFulfillment saga holds state across the order lifecycle with a
 * timeout compensation path. Unlike the centered vertical spine of v1, the hero
 * here is the horizontal bus; services tap off it. Fully static, no motion.
 * Three font sizes only. Every svg id is prefixed "mg4-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// Content gutters; the publisher column and its primary subscriber share an x.
const X0 = 40;
const X1 = 960;
const PUB_CX = 184;

// The horizontal message bus: the full-width hero spine.
const BUS_X = 40;
const BUS_Y = 296;
const BUS_W = 920;
const BUS_H = 64;

// The three handler cards hang below the bus on one equal-height row.
const CARD_Y = 440;
const CARD_H = 126;
const CARD_W = 288;
const CARD_X = [40, 356, 672] as const;

// The full-width OrderFulfillment saga panel along the bottom.
const SAGA_X = 40;
const SAGA_Y = 620;
const SAGA_W = 920;
const SAGA_H = 176;

export function MessagingGraphicV4() {
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
        viewBox="0 0 1000 812"
        width="100%"
        role="img"
        aria-label="A message-bus architecture diagram. Above a horizon line the Fusion gateway is the public API surface: a request comes in and 200 OK goes back out. Below the horizon the gateway resolves against the orders-svc subgraph, which then publishes the OrderPlaced event straight down onto a full-width message bus. The bus is one pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS. Across a service boundary the bus delivers down to a row of handler services, each on its own service: publish and subscribe with two subscribers (one batch), request and reply where the reply goes back up to the bus, and send. A full-width OrderFulfillment saga holds state across Placed, AwaitingPayment, Paid, and Shipped, with a payment timeout compensation path to Cancelled."
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

        {/* ABOVE THE HORIZON: the gateway, the public API surface */}
        <text
          x={140}
          y={30}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          REQUEST
        </text>
        <line
          x1={140}
          y1={36}
          x2={140}
          y2={58}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg4-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={228}
          y={30}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1={228}
          y1={58}
          x2={228}
          y2={36}
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg4-green)"
          vectorEffect="non-scaling-stroke"
        />
        <NodeCard x={X0} y={60} w={CARD_W} label="GATEWAY" name="Fusion" />
        <text x={X1} y={86} textAnchor="end" fontSize={FS.label} fill={C.faint}>
          the public GraphQL API
        </text>
        <text
          x={X1}
          y={102}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          request in, 200 OK out
        </text>

        {/* The horizon dividing the public surface from the services */}
        <text
          x={X0}
          y={132}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          API SURFACE
        </text>
        <line
          x1={X0}
          y1={138}
          x2={X1}
          y2={138}
          stroke="rgba(245, 241, 234, 0.2)"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={X0}
          y={158}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SERVICES
        </text>
        <text
          x={X1}
          y={158}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          internal, not exposed
        </text>

        {/* The gateway resolves down across the horizon into the subgraph */}
        <line
          x1={PUB_CX}
          y1={120}
          x2={PUB_CX}
          y2={164}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg4-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={PUB_CX + 12}
          y={158}
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          RESOLVES
        </text>

        {/* BELOW THE HORIZON: the orders-svc subgraph publishes OrderPlaced */}
        <NodeCard
          x={X0}
          y={168}
          w={CARD_W}
          label="SUBGRAPH"
          name="orders-svc"
        />
        <text
          x={X1}
          y={192}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          fire and forget,
        </text>
        <text
          x={X1}
          y={208}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          not in the response path
        </text>
        <circle cx={PUB_CX} cy={222} r="2.5" fill={C.accent} />
        <line
          x1={PUB_CX}
          y1={222}
          x2={PUB_CX}
          y2={294}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg4-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={PUB_CX + 12}
          y={252}
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>
        <text x={PUB_CX + 12} y={266} fontSize={FS.label} fill={C.faint}>
          after 200 OK
        </text>

        {/* THE MESSAGE BUS: the full-width hero spine, one pluggable transport */}
        <text
          x={X0}
          y={286}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          MESSAGE BUS
        </text>
        <text
          x={X1}
          y={286}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          one bus, four brokers
        </text>
        <rect
          x={BUS_X}
          y={BUS_Y}
          width={BUS_W}
          height={BUS_H}
          rx="14"
          fill={C.surface}
          fillOpacity="0.6"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={500}
          y={320}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.8"
          fill={C.navLabel}
        >
          PLUGGABLE TRANSPORT, SAME CODE
        </text>
        {[270, 500, 730].map((dx) => (
          <line
            key={dx}
            x1={dx}
            y1={330}
            x2={dx}
            y2={352}
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
        ))}
        {TRANSPORTS.map((t) => (
          <text
            key={t.name}
            x={t.cx}
            y={347}
            textAnchor="middle"
            fontSize={FS.name}
            fill={C.inkDim}
          >
            {t.name}
          </text>
        ))}
        {/* The event enters the bus where the subgraph publishes */}
        <circle cx={PUB_CX} cy={BUS_Y} r="2.5" fill={C.accent} />

        {/* SERVICE BOUNDARY: the bus delivers across it to the handlers */}
        <text
          x={X0}
          y={402}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SERVICE BOUNDARY
        </text>
        <line
          x1={X0}
          y1={408}
          x2={X1}
          y2={408}
          stroke={C.cardBorder}
          strokeWidth="1"
          strokeDasharray="4 4"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={X1}
          y={402}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          each handler on its own service
        </text>

        {/* Vertical drops off the bus to each handler card */}
        {/* PUBLISH / SUBSCRIBE: the primary path, drops straight down */}
        <circle
          cx={CARD_X[0] + CARD_W / 2}
          cy={BUS_Y + BUS_H}
          r="2.5"
          fill={C.accent}
        />
        <line
          x1={CARD_X[0] + CARD_W / 2}
          y1={BUS_Y + BUS_H}
          x2={CARD_X[0] + CARD_W / 2}
          y2={CARD_Y - 2}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg4-teal)"
          vectorEffect="non-scaling-stroke"
        />
        {/* REQUEST / REPLY: delivered down, the reply goes back up to the bus */}
        <line
          x1={CARD_X[1] + CARD_W / 2 - 14}
          y1={BUS_Y + BUS_H}
          x2={CARD_X[1] + CARD_W / 2 - 14}
          y2={CARD_Y - 2}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg4-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={CARD_X[1] + CARD_W / 2 + 14}
          y1={CARD_Y - 2}
          x2={CARD_X[1] + CARD_W / 2 + 14}
          y2={BUS_Y + BUS_H + 2}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg4-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CARD_X[1] + CARD_W / 2 + 22}
          y={398}
          fontSize={FS.label}
          fill={C.faint}
        >
          reply
        </text>
        {/* SEND: a one-way command, drops straight down */}
        <line
          x1={CARD_X[2] + CARD_W / 2}
          y1={BUS_Y + BUS_H}
          x2={CARD_X[2] + CARD_W / 2}
          y2={CARD_Y - 2}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg4-ink)"
          vectorEffect="non-scaling-stroke"
        />

        {/* The handler cards, each on its own service */}
        <PatternCard
          x={CARD_X[0]}
          kicker="PUBLISH / SUBSCRIBE"
          caption="fan-out to every subscriber"
        />
        <HandlerRow
          x={CARD_X[0] + 16}
          y={CARD_Y + 50}
          name="UpdateInventory"
          service="inventory-svc"
          primary
        />
        <HandlerRow
          x={CARD_X[0] + 16}
          y={CARD_Y + 82}
          name="NotifyCustomer"
          service="notify-svc"
          batch
        />

        <PatternCard
          x={CARD_X[1]}
          kicker="REQUEST / REPLY"
          caption="one handler, replies"
        />
        <HandlerRow
          x={CARD_X[1] + 16}
          y={CARD_Y + 50}
          name="GetQuote"
          service="pricing-svc"
        />
        <text
          x={CARD_X[1] + 18}
          y={CARD_Y + 94}
          fontSize={FS.label}
          fill={C.faint}
        >
          replies to the caller
        </text>

        <PatternCard x={CARD_X[2]} kicker="SEND" caption="one-way command" />
        <HandlerRow
          x={CARD_X[2] + 16}
          y={CARD_Y + 50}
          name="ShipOrder"
          service="shipping-svc"
        />
        <text
          x={CARD_X[2] + 18}
          y={CARD_Y + 94}
          fontSize={FS.label}
          fill={C.faint}
        >
          no reply expected
        </text>

        {/* SAGA: a stateful coordinator across the order lifecycle */}
        <rect
          x={SAGA_X}
          y={SAGA_Y}
          width={SAGA_W}
          height={SAGA_H}
          rx="18"
          fill={C.surface}
          fillOpacity="0.4"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={62}
          y={SAGA_Y + 30}
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text x={62} y={SAGA_Y + 46} fontSize={FS.label} fill={C.navLabel}>
          stateful coordinator across the order lifecycle
        </text>
        <rect
          x={824}
          y={SAGA_Y + 14}
          width="112"
          height="22"
          rx="11"
          fill="none"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={880}
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
              markerEnd="url(#mg4-ink)"
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
        {SAGA_STATES.map((s) => {
          const current = s.state === "current";
          const next = s.state === "next";
          return (
            <g key={s.name}>
              {current && (
                <text
                  x={s.cx}
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
                x={s.cx - 75}
                y={SAGA_Y + 76}
                width="150"
                height="38"
                rx="10"
                fill={current ? C.accent : "none"}
                fillOpacity={current ? 0.1 : 1}
                stroke={current ? C.accent : next ? C.inkFaint : C.cardBorder}
                strokeOpacity={current ? 0.7 : 1}
                strokeWidth="1"
                strokeDasharray={next ? "3 3" : undefined}
                vectorEffect="non-scaling-stroke"
              />
              <text
                x={s.cx}
                y={SAGA_Y + 99}
                textAnchor="middle"
                fontSize={FS.name}
                fontWeight={current ? 600 : 400}
                fill={current ? C.accent : next ? C.faint : C.inkDim}
              >
                {s.name}
              </text>
            </g>
          );
        })}

        {/* Compensation: a payment timeout cancels and compensates */}
        <line
          x1={385}
          y1={SAGA_Y + 114}
          x2={385}
          y2={SAGA_Y + 138}
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg4-coral)"
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
          rx="10"
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
  readonly label: string;
  readonly name: string;
}

/** A left-anchored service node (gateway, subgraph): a type label over a name. */
function NodeCard({ x, y, w, label, name }: NodeCardProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height="54"
        rx="14"
        fill={C.surface}
        fillOpacity="0.6"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 22}
        y={y + 22}
        fontSize={FS.label}
        letterSpacing="1.6"
        fill={C.navLabel}
      >
        {label}
      </text>
      <text
        x={x + 22}
        y={y + 40}
        fontSize={FS.name}
        fontWeight="600"
        fill={C.heading}
      >
        {name}
      </text>
    </g>
  );
}

interface PatternCardProps {
  readonly x: number;
  readonly kicker: string;
  readonly caption: string;
}

/** The shared frame for one handler card: a kicker over a quiet caption. */
function PatternCard({ x, kicker, caption }: PatternCardProps) {
  return (
    <g>
      <rect
        x={x}
        y={CARD_Y}
        width={CARD_W}
        height={CARD_H}
        rx="14"
        fill={C.surface}
        fillOpacity="0.5"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 18}
        y={CARD_Y + 24}
        fontSize={FS.label}
        letterSpacing="1.4"
        fill={C.navLabel}
      >
        {kicker}
      </text>
      <text x={x + 18} y={CARD_Y + 40} fontSize={FS.label} fill={C.faint}>
        {caption}
      </text>
    </g>
  );
}

interface HandlerRowProps {
  readonly x: number;
  readonly y: number;
  readonly name: string;
  readonly service: string;
  readonly primary?: boolean;
  readonly batch?: boolean;
}

/** One handler, on its own service, inside a handler card. */
function HandlerRow({ x, y, name, service, primary, batch }: HandlerRowProps) {
  const w = 256;
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height="26"
        rx="9"
        fill={C.surface}
        fillOpacity="0.7"
        stroke={primary ? C.accent : C.cardBorder}
        strokeOpacity={primary ? 0.55 : 1}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 12}
        y={y + 17}
        fontSize={FS.name}
        fill={primary ? C.accent : C.heading}
      >
        {name}
      </text>
      {batch && (
        <>
          <rect
            x={x + 110}
            y={y + 6}
            width="38"
            height="15"
            rx="7"
            fill="none"
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x={x + 129}
            y={y + 16}
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
        y={y + 17}
        textAnchor="end"
        fontSize={FS.label}
        fill={C.navLabel}
      >
        {service}
      </text>
    </g>
  );
}

interface Arrow {
  readonly id: string;
  readonly fill: string;
}

const ARROWS: readonly Arrow[] = [
  { id: "mg4-teal", fill: "#5eead4" },
  { id: "mg4-green", fill: "#34d399" },
  { id: "mg4-coral", fill: "#f0786a" },
  { id: "mg4-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

interface Transport {
  readonly name: string;
  readonly cx: number;
}

/** The four interchangeable transports, laid out inline along the bus. */
const TRANSPORTS: readonly Transport[] = [
  { name: "RabbitMQ", cx: 155 },
  { name: "Apache Kafka", cx: 385 },
  { name: "Azure Service Bus", cx: 615 },
  { name: "Amazon SQS", cx: 845 },
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
