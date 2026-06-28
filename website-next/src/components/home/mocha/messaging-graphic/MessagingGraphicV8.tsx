/**
 * Mocha messaging flow, version 8: "Timeline waterfall".
 *
 * A horizontal timeline. Time runs left to right under a faint axis with ticks.
 * Lanes are stacked down the left. The synchronous request/response is a short
 * green bar at the very start (request -> 200 OK), and a vertical marker shows
 * where the response is sent. Everything to the right of that marker is
 * asynchronous: the orders-svc subgraph publishes OrderPlaced onto one pluggable
 * transport, the event crosses a service boundary, and the handlers run in
 * parallel lanes (UpdateInventory + NotifyCustomer as one publish/subscribe
 * batch, GetQuote as request/reply, ShipOrder as a one-way send). Along the
 * bottom the OrderFulfillment saga advances through Placed, AwaitingPayment,
 * Paid (current), and Shipped, with a coral payment-timeout branch to Cancelled.
 *
 * The whole point the timeline makes obvious: the 200 OK returns fast and
 * synchronously, while the events and the saga continue afterward. Fully static,
 * no motion. Three font sizes only. Every svg id is prefixed "mg8-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// Lane labels are right-aligned to this gutter; the plot starts after it.
const LABEL_X = 150;

// The time axis: x = AX_LEFT is t0, x = AX_RIGHT is the end of the window.
const AX_LEFT = 170;
const AX_RIGHT = 966;
const AX_Y = 48;

// Where the synchronous response is sent; the async work begins after this.
const X_OK = 300;
const X_BOUND = 312;
const X_DELIVER = 470;

// Bar height, shared by every span so the lanes read as one grid.
const BH = 22;

// Lane vertical centers, top to bottom.
const LANE = {
  gateway: 92,
  subgraph: 130,
  transport: 168,
  updateInventory: 230,
  notifyCustomer: 268,
  getQuote: 308,
  shipOrder: 350,
} as const;

const TICK_XS = Array.from(
  { length: 7 },
  (_, i) => AX_LEFT + (i * (AX_RIGHT - AX_LEFT)) / 6,
);

export function MessagingGraphicV8() {
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
        viewBox="0 0 1000 494"
        width="100%"
        role="img"
        aria-label="A horizontal timeline of the messaging flow, time running left to right under a faint axis. At the very start a short green bar shows the Fusion gateway handling the request and returning 200 OK synchronously, with a marker where the response is sent. After that marker the work is asynchronous: the orders-svc subgraph publishes the OrderPlaced event onto one pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS, the event crosses a service boundary, and handlers run in parallel lanes, UpdateInventory and NotifyCustomer as one publish and subscribe batch, GetQuote as request and reply, and ShipOrder as a one-way send, each on its own service. Along the bottom the OrderFulfillment saga advances through Placed, AwaitingPayment, Paid which is current, and Shipped, with a coral payment-timeout branch to Cancelled that runs compensation."
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

        {/* TIME AXIS: faint baseline, ticks, and a few quiet gridlines */}
        {TICK_XS.map((x) => (
          <line
            key={`grid-${x}`}
            x1={x}
            y1="64"
            x2={x}
            y2="362"
            stroke={C.heading}
            strokeOpacity="0.05"
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
        ))}
        <line
          x1={AX_LEFT}
          y1={AX_Y}
          x2={AX_RIGHT}
          y2={AX_Y}
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        {TICK_XS.map((x) => (
          <line
            key={`tick-${x}`}
            x1={x}
            y1={AX_Y}
            x2={x}
            y2={AX_Y + 4}
            stroke={C.faint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
        ))}
        <text
          x={AX_RIGHT}
          y={AX_Y - 8}
          textAnchor="end"
          fontSize={FS.label}
          letterSpacing="1"
          fill={C.navLabel}
        >
          time -&gt;
        </text>

        {/* Top framing: synchronous half, then the async half after the marker */}
        <text
          x={AX_LEFT}
          y={AX_Y - 8}
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          SYNCHRONOUS
        </text>
        <text
          x={X_BOUND + 10}
          y={AX_Y - 8}
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.inkDim}
        >
          response sent, then asynchronously
        </text>

        {/* The response-sent marker dividing the synchronous start from the rest */}
        <line
          x1={X_BOUND}
          y1={AX_Y + 6}
          x2={X_BOUND}
          y2="362"
          stroke={C.faint}
          strokeWidth="1"
          strokeDasharray="3 4"
          vectorEffect="non-scaling-stroke"
        />

        {/* GATEWAY lane: the short synchronous request -> 200 OK bar */}
        <LaneLabel y={LANE.gateway} role="GATEWAY" name="Fusion" />
        <line
          x1={AX_LEFT - 12}
          y1={LANE.gateway}
          x2={AX_LEFT}
          y2={LANE.gateway}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg8-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <Span
          x1={AX_LEFT}
          x2={X_OK}
          y={LANE.gateway}
          fill={C.healthy}
          fillOpacity={0.12}
          stroke={C.healthy}
          strokeOpacity={0.7}
        />
        <line
          x1={X_OK}
          y1={LANE.gateway - BH / 2}
          x2={X_OK}
          y2={LANE.gateway - BH / 2 - 12}
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg8-green)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={X_OK}
          y={LANE.gateway - BH / 2 - 16}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1"
          fill={C.healthy}
        >
          200 OK
        </text>

        {/* Gateway resolves the subgraph, synchronously, inside the request */}
        <line
          x1="234"
          y1={LANE.gateway + BH / 2}
          x2="234"
          y2={LANE.subgraph - BH / 2}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg8-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="240" y="116" fontSize={FS.label} fill={C.navLabel}>
          resolves
        </text>

        {/* SUBGRAPH lane: resolves, responds, then publishes OrderPlaced */}
        <LaneLabel y={LANE.subgraph} role="SUBGRAPH" name="orders-svc" />
        <Span x1={206} x2={X_OK} y={LANE.subgraph} />
        <line
          x1="326"
          y1={LANE.subgraph + BH / 2}
          x2="348"
          y2={LANE.transport - BH / 2}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg8-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <path d="M326 124 l5 6 l-5 6 l-5 -6 z" fill={C.accent} stroke="none" />
        <text
          x="338"
          y="127"
          fontSize={FS.label}
          letterSpacing="0.6"
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>

        {/* TRANSPORT lane: one pluggable transport carries the event */}
        <LaneLabel y={LANE.transport} role="TRANSPORT" name="pluggable" />
        <Span x1={348} x2={X_DELIVER} y={LANE.transport} />
        <circle cx="350" cy={LANE.transport} r="2.5" fill={C.accent} />
        <text x="360" y="192" fontSize={FS.label} fill={C.faint}>
          RabbitMQ &middot; Apache Kafka &middot; Azure Service Bus &middot;
          Amazon SQS
        </text>

        {/* Service boundary: the event leaves orders-svc and reaches handlers */}
        <line
          x1="100"
          y1="206"
          x2="784"
          y2="206"
          stroke={C.inkFaint}
          strokeWidth="1"
          strokeDasharray="2 5"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="8"
          y="203"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          SERVICE BOUNDARY
        </text>
        <line
          x1={X_DELIVER}
          y1={LANE.transport + BH / 2}
          x2={X_DELIVER}
          y2={LANE.updateInventory - BH / 2}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg8-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text x={X_DELIVER + 8} y="214" fontSize={FS.label} fill={C.accent}>
          delivered
        </text>

        {/* HANDLERS: publish/subscribe (one batch), request/reply, send */}
        <LaneLabel
          y={LANE.updateInventory}
          role="INVENTORY-SVC"
          name="UpdateInventory"
        />
        <Span x1={X_DELIVER} x2={700} y={LANE.updateInventory} />
        <LaneLabel
          y={LANE.notifyCustomer}
          role="NOTIFY-SVC"
          name="NotifyCustomer"
        />
        <Span x1={X_DELIVER} x2={700} y={LANE.notifyCustomer} />
        {/* a bracket joins the two subscribers: one publish/subscribe batch */}
        <path
          d="M712 220 h6 v58 h-6"
          fill="none"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="724"
          y="246"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          PUBLISH / SUBSCRIBE
        </text>
        <text x="724" y="260" fontSize={FS.label} fill={C.faint}>
          one batch
        </text>

        <LaneLabel y={LANE.getQuote} role="PRICING-SVC" name="GetQuote" />
        <Span x1={X_DELIVER} x2={648} y={LANE.getQuote} />
        {/* the synchronous reply comes back to the caller */}
        <line
          x1="648"
          y1={LANE.getQuote + BH / 2 + 6}
          x2="500"
          y2={LANE.getQuote + BH / 2 + 6}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg8-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="660"
          y={LANE.getQuote + 4}
          fontSize={FS.label}
          fill={C.navLabel}
        >
          REQUEST / REPLY
        </text>
        <text
          x="574"
          y={LANE.getQuote + BH / 2 + 18}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.faint}
        >
          reply
        </text>

        <LaneLabel y={LANE.shipOrder} role="SHIPPING-SVC" name="ShipOrder" />
        <Span x1={X_DELIVER} x2={688} y={LANE.shipOrder} />
        <line
          x1="688"
          y1={LANE.shipOrder}
          x2="704"
          y2={LANE.shipOrder}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg8-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="716"
          y={LANE.shipOrder - 1}
          fontSize={FS.label}
          fill={C.navLabel}
        >
          SEND
        </text>
        <text
          x="716"
          y={LANE.shipOrder + 11}
          fontSize={FS.label}
          fill={C.faint}
        >
          one-way
        </text>

        {/* SAGA: the stateful coordinator advancing along the same timeline */}
        <text
          x={AX_LEFT}
          y="382"
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text
          x={AX_RIGHT}
          y="382"
          textAnchor="end"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          STATEFUL COORDINATOR
        </text>
        <rect
          x={X_BOUND}
          y="396"
          width={AX_RIGHT - X_BOUND}
          height="44"
          rx="14"
          fill={C.surface}
          fillOpacity="0.4"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        {SAGA.map((s) => {
          const current = s.state === "current";
          const next = s.state === "next";
          const cx = (s.x1 + s.x2) / 2;
          return (
            <g key={s.name}>
              {current && (
                <rect
                  x={s.x1 + 4}
                  y="400"
                  width={s.x2 - s.x1 - 8}
                  height="36"
                  rx="10"
                  fill={C.accent}
                  fillOpacity="0.1"
                  stroke={C.accent}
                  strokeOpacity="0.6"
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                />
              )}
              {s.x1 > X_BOUND && (
                <line
                  x1={s.x1}
                  y1="396"
                  x2={s.x1}
                  y2="440"
                  stroke={C.cardBorder}
                  strokeWidth="1"
                  strokeDasharray={next ? "3 3" : undefined}
                  vectorEffect="non-scaling-stroke"
                />
              )}
              {current && (
                <text
                  x={cx}
                  y="392"
                  textAnchor="middle"
                  fontSize={FS.label}
                  letterSpacing="1"
                  fill={C.accent}
                >
                  CURRENT
                </text>
              )}
              <text
                x={cx}
                y="422"
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
        {SAGA_TRANSITIONS.map((t) => (
          <g key={t.label}>
            <line
              x1={t.x}
              y1="385"
              x2={t.x}
              y2="396"
              stroke={C.faint}
              strokeWidth="1"
              markerEnd="url(#mg8-ink)"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={t.x}
              y="380"
              textAnchor="middle"
              fontSize={FS.label}
              fill={C.navLabel}
            >
              {t.label}
            </text>
          </g>
        ))}

        {/* Compensation: a payment timeout cancels and runs compensation */}
        <line
          x1="560"
          y1="440"
          x2="560"
          y2="458"
          stroke={C.coral}
          strokeOpacity="0.85"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg8-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="572" y="452" fontSize={FS.label} fill={C.coral}>
          payment timeout
        </text>
        <rect
          x="486"
          y="458"
          width="150"
          height="28"
          rx="14"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="561"
          y="476"
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
        >
          Cancelled
        </text>
        <text
          x="648"
          y="476"
          fontSize={FS.label}
          fill={C.coral}
          fillOpacity="0.85"
        >
          runs compensation
        </text>
      </svg>
    </div>
  );
}

interface SpanProps {
  readonly x1: number;
  readonly x2: number;
  readonly y: number;
  readonly fill?: string;
  readonly fillOpacity?: number;
  readonly stroke?: string;
  readonly strokeOpacity?: number;
}

/** One timeline span: a pill-ended bar of the shared height, centered on y. */
function Span({
  x1,
  x2,
  y,
  fill = C.surface,
  fillOpacity = 0.6,
  stroke = C.cardBorder,
  strokeOpacity = 1,
}: SpanProps) {
  return (
    <rect
      x={x1}
      y={y - BH / 2}
      width={x2 - x1}
      height={BH}
      rx={BH / 2}
      fill={fill}
      fillOpacity={fillOpacity}
      stroke={stroke}
      strokeOpacity={strokeOpacity}
      strokeWidth="1"
      vectorEffect="non-scaling-stroke"
    />
  );
}

interface LaneLabelProps {
  readonly y: number;
  readonly role: string;
  readonly name: string;
}

/** A lane's left label: a tracked role over the entity name, right-aligned. */
function LaneLabel({ y, role, name }: LaneLabelProps) {
  return (
    <g>
      <text
        x={LABEL_X}
        y={y - 3}
        textAnchor="end"
        fontSize={FS.label}
        letterSpacing="1"
        fill={C.navLabel}
      >
        {role}
      </text>
      <text
        x={LABEL_X}
        y={y + 10}
        textAnchor="end"
        fontSize={FS.name}
        fill={C.heading}
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
  { id: "mg8-teal", fill: "#5eead4" },
  { id: "mg8-green", fill: "#34d399" },
  { id: "mg8-coral", fill: "#f0786a" },
  { id: "mg8-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

interface SagaState {
  readonly name: string;
  readonly x1: number;
  readonly x2: number;
  readonly state: "done" | "current" | "next";
}

/** The saga states as consecutive segments, with Paid as the current state. */
const SAGA: readonly SagaState[] = [
  { name: "Placed", x1: 312, x2: 470, state: "done" },
  { name: "AwaitingPayment", x1: 470, x2: 620, state: "done" },
  { name: "Paid", x1: 620, x2: 790, state: "current" },
  { name: "Shipped", x1: 790, x2: 966, state: "next" },
];

interface SagaTransition {
  readonly label: string;
  readonly x: number;
}

/** The labelled transitions between saga states, placed at the boundaries. */
const SAGA_TRANSITIONS: readonly SagaTransition[] = [
  { label: "OrderPlaced", x: 470 },
  { label: "PaymentReceived", x: 620 },
  { label: "sends ShipOrder", x: 790 },
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
