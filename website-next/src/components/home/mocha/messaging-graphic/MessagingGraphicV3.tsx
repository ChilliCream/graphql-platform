/**
 * Mocha messaging flow graphic, version 3, layout angle "Clean stacked lanes".
 *
 * The corrected message flow arranged as full-width horizontal lanes, read top
 * to bottom, with the message travelling straight down through them:
 *   SYNC EDGE   a request hits orders-svc, which returns 200 OK on the same
 *               node (a normal synchronous request and response);
 *   EMIT        after responding, orders-svc publishes the OrderPlaced domain
 *               event, labelled on the down edge into the transport;
 *   TRANSPORT   a full-width pluggable band holding RabbitMQ (the active one),
 *               Apache Kafka, Azure Service Bus and Amazon SQS as interchangeable
 *               options;
 *   HANDLERS    the three messaging patterns as a full-width row, each reached
 *               by a fan-out comb that branches down from the transport: Publish
 *               and Subscribe (UpdateInventory plus a second, batch subscriber
 *               NotifyCustomer), Request and Reply (GetShippingRate, with the
 *               reply shown), and Send (the one-way ShipOrder command).
 * The OrderFulfillment saga is a tall coordinator rail on the right that spans
 * the lanes and is wired to the handlers lane: events flow up into it and it
 * sends the ShipOrder command back down. It is a stateful coordinator with its
 * own state machine (Placed -> AwaitingPayment -> Paid, the current state, ->
 * Shipped) plus a timeout compensation path to Cancelled.
 *
 * Static diagram, no motion. Every svg id is prefixed "mg3-".
 */
export function MessagingGraphicV3() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-3xl border p-5 backdrop-blur-sm sm:p-8">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <h3 className="font-heading text-cc-heading text-h6 font-semibold">
          One event, any transport
        </h3>
        <div className="flex items-center gap-4">
          <LegendDot color={C.accent} label="active" />
          <LegendDot color={C.coral} label="compensation" />
        </div>
      </header>

      <svg
        viewBox="0 0 1000 520"
        width="100%"
        role="img"
        aria-label="Stacked horizontal lanes read top to bottom. Sync edge: a request hits orders-svc, which returns 200 OK synchronously on the same node. Emit: after responding, orders-svc publishes the OrderPlaced event down into the transport. Transport: a full-width pluggable band holding RabbitMQ (active), Apache Kafka, Azure Service Bus and Amazon SQS. Handlers: a fan-out comb branches the event down to three patterns, Publish and Subscribe fanning to UpdateInventory and a batch NotifyCustomer, Request and Reply with GetShippingRate, and Send with ShipOrder. A tall OrderFulfillment saga rail on the right is wired to the handlers lane, receiving events and sending the ShipOrder command, coordinating the states Placed, AwaitingPayment, Paid (current) and Shipped, with a timeout path to Cancelled."
        className="mt-5"
        style={{ display: "block", overflow: "visible", fontFamily: MONO }}
      >
        <defs>
          <linearGradient
            id="mg3-band"
            gradientUnits="userSpaceOnUse"
            x1="24"
            y1="257"
            x2="728"
            y2="257"
          >
            <stop offset="0" stopColor={C.accent} stopOpacity="0.06" />
            <stop offset="1" stopColor={C.violet} stopOpacity="0.06" />
          </linearGradient>
          <marker
            id="mg3-arrowTeal"
            viewBox="0 0 10 10"
            refX="8"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M0 0 L10 5 L0 10 z" fill={C.accent} />
          </marker>
          <marker
            id="mg3-arrowGreen"
            viewBox="0 0 10 10"
            refX="8"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M0 0 L10 5 L0 10 z" fill={C.healthy} />
          </marker>
          <marker
            id="mg3-arrowViolet"
            viewBox="0 0 10 10"
            refX="8"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M0 0 L10 5 L0 10 z" fill={C.violet} />
          </marker>
          <marker
            id="mg3-arrowAmber"
            viewBox="0 0 10 10"
            refX="8"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M0 0 L10 5 L0 10 z" fill={C.amber} />
          </marker>
          <marker
            id="mg3-arrowCoral"
            viewBox="0 0 10 10"
            refX="8"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M0 0 L10 5 L0 10 z" fill={C.coral} />
          </marker>
          <marker
            id="mg3-arrowInk"
            viewBox="0 0 10 10"
            refX="8"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M0 0 L10 5 L0 10 z" fill={C.faint} />
          </marker>
        </defs>

        {/* ---- lane dividers and short lane tags (no numbers) ---- */}
        <line
          x1="24"
          y1="130"
          x2="728"
          y2="130"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text x="28" y="36" fontSize="8" letterSpacing="1.6" fill={C.navLabel}>
          SYNC EDGE
        </text>
        <text x="28" y="152" fontSize="8" letterSpacing="1.6" fill={C.navLabel}>
          EMIT
        </text>
        <text x="28" y="332" fontSize="8" letterSpacing="1.6" fill={C.navLabel}>
          HANDLERS
        </text>

        {/* ---- LANE: SYNC EDGE, request in and 200 OK out on orders-svc ---- */}
        <text x="236" y="72" textAnchor="middle" fontSize="7.5" fill={C.accent}>
          request
        </text>
        <line
          x1="170"
          y1="80"
          x2="284"
          y2="80"
          stroke={C.accent}
          strokeOpacity="0.85"
          strokeWidth="1.25"
          markerEnd="url(#mg3-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="284"
          y1="98"
          x2="170"
          y2="98"
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg3-arrowGreen)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="236"
          y="112"
          textAnchor="middle"
          fontSize="7.5"
          fill={C.healthy}
        >
          200 OK
        </text>

        <rect
          x="286"
          y="50"
          width="180"
          height="62"
          rx="12"
          fill={C.surface}
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="376"
          y="72"
          textAnchor="middle"
          fontSize="7"
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          SERVICE
        </text>
        <text
          x="376"
          y="90"
          textAnchor="middle"
          fontSize="12.5"
          fontWeight="600"
          fill={C.heading}
        >
          orders-svc
        </text>
        <text x="376" y="103" textAnchor="middle" fontSize="7.5" fill={C.faint}>
          POST /orders
        </text>

        {/* note that balances the right of the top lane ahead of the saga */}
        <text
          x="720"
          y="72"
          textAnchor="end"
          fontSize="7"
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          SYNCHRONOUS
        </text>
        <text x="720" y="86" textAnchor="end" fontSize="7.5" fill={C.inkDim}>
          the caller already has 200 OK
        </text>
        <text x="720" y="99" textAnchor="end" fontSize="7.5" fill={C.inkDim}>
          before anything is published
        </text>

        {/* ---- LANE: EMIT, publish OrderPlaced on the down edge ---- */}
        <circle cx="376" cy="112" r="2.5" fill={C.accent} />
        <line
          x1="376"
          y1="112"
          x2="376"
          y2="150"
          stroke={C.accent}
          strokeOpacity="0.7"
          strokeWidth="1.5"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="376"
          y="144"
          textAnchor="middle"
          fontSize="7"
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          PUBLISHES
        </text>
        <rect
          x="320"
          y="150"
          width="112"
          height="24"
          rx="8"
          fill={C.surface}
          stroke={C.accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="376"
          y="166"
          textAnchor="middle"
          fontSize="11"
          fontWeight="600"
          fill={C.accent}
        >
          OrderPlaced
        </text>
        <line
          x1="376"
          y1="174"
          x2="376"
          y2="208"
          stroke={C.accent}
          strokeOpacity="0.7"
          strokeWidth="1.5"
          markerEnd="url(#mg3-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="376" y="192" textAnchor="middle" fontSize="7.5" fill={C.faint}>
          after it responds
        </text>

        {/* ---- LANE: TRANSPORT, the pluggable band, full width ---- */}
        <rect
          x="24"
          y="210"
          width="704"
          height="94"
          rx="14"
          fill="url(#mg3-band)"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text x="40" y="234" fontSize="8" letterSpacing="1.6" fill={C.navLabel}>
          TRANSPORT
        </text>
        <text x="40" y="248" fontSize="7.5" fill={C.accent}>
          pluggable
        </text>
        <text x="712" y="234" textAnchor="end" fontSize="7.5" fill={C.faint}>
          swap without touching your code
        </text>
        <text
          x="148"
          y="246"
          textAnchor="middle"
          fontSize="6.5"
          letterSpacing="0.8"
          fill={C.accent}
        >
          active
        </text>

        {TRANSPORTS.map((t) => (
          <g key={t.label}>
            <rect
              x={t.cx - t.w / 2}
              y="252"
              width={t.w}
              height="30"
              rx="15"
              fill={t.active ? C.accent : C.surface}
              fillOpacity={t.active ? 0.1 : 0.85}
              stroke={t.active ? C.accent : C.cardBorder}
              strokeOpacity={t.active ? 0.6 : 1}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={t.cx}
              y="271"
              textAnchor="middle"
              fontSize="9.5"
              fill={t.active ? C.accent : C.inkDim}
            >
              {t.label}
            </text>
          </g>
        ))}

        {/* ---- fan-out comb: transport branches down to all three patterns ---- */}
        <line
          x1="376"
          y1="304"
          x2="376"
          y2="316"
          stroke={C.accent}
          strokeWidth="2"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="132"
          y1="316"
          x2="620"
          y2="316"
          stroke={C.accent}
          strokeOpacity="0.55"
          strokeWidth="1.5"
          vectorEffect="non-scaling-stroke"
        />
        <circle cx="132" cy="316" r="2.5" fill={C.accent} />
        <circle cx="376" cy="316" r="2.5" fill={C.accent} />
        <circle cx="620" cy="316" r="2.5" fill={C.accent} />
        <line
          x1="132"
          y1="316"
          x2="132"
          y2="348"
          stroke={C.accent}
          strokeOpacity="0.85"
          strokeWidth="1.5"
          markerEnd="url(#mg3-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="620"
          y1="316"
          x2="620"
          y2="348"
          stroke={C.accent}
          strokeOpacity="0.85"
          strokeWidth="1.5"
          markerEnd="url(#mg3-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />

        {/* ---- HANDLERS: Publish / Subscribe, fanning to two subscribers ---- */}
        {/* main delivery into the first subscriber, on the spine */}
        <line
          x1="376"
          y1="316"
          x2="376"
          y2="348"
          stroke={C.accent}
          strokeOpacity="0.85"
          strokeWidth="1.5"
          markerEnd="url(#mg3-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        {/* branch around the first box to the second subscriber */}
        <path
          d="M132 340 H250 V455 H242"
          fill="none"
          stroke={C.accent}
          strokeOpacity="0.45"
          strokeWidth="1.25"
          markerEnd="url(#mg3-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />

        <rect
          x="24"
          y="348"
          width="216"
          height="66"
          rx="11"
          fill={C.surface}
          fillOpacity="0.6"
          stroke={C.accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="38"
          y="367"
          fontSize="7"
          letterSpacing="1.1"
          fill={C.accent}
          fillOpacity="0.9"
        >
          PUBLISH / SUBSCRIBE
        </text>
        <text x="38" y="387" fontSize="11.5" fontWeight="600" fill={C.accent}>
          UpdateInventory
        </text>
        <text x="38" y="401" fontSize="7.5" fill={C.faint}>
          inventory-svc
        </text>

        <rect
          x="24"
          y="426"
          width="216"
          height="58"
          rx="11"
          fill={C.surface}
          fillOpacity="0.55"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text x="38" y="444" fontSize="6.5" letterSpacing="1" fill={C.navLabel}>
          HANDLER &middot; SUBSCRIBES
        </text>
        <text x="38" y="461" fontSize="11" fontWeight="600" fill={C.inkDim}>
          NotifyCustomer
        </text>
        <text x="38" y="473" fontSize="7.5" fill={C.faint}>
          email-svc
        </text>
        <rect
          x="176"
          y="432"
          width="46"
          height="16"
          rx="8"
          fill={C.accent}
          fillOpacity="0.1"
          stroke={C.accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="199"
          y="443"
          textAnchor="middle"
          fontSize="7.5"
          fill={C.accent}
        >
          batch
        </text>

        {/* ---- HANDLERS: Request / Reply ---- */}
        <line
          x1="392"
          y1="348"
          x2="392"
          y2="320"
          stroke={C.violet}
          strokeOpacity="0.7"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg3-arrowViolet)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="402" y="334" fontSize="6.5" fill={C.violet}>
          reply
        </text>
        <rect
          x="286"
          y="348"
          width="180"
          height="66"
          rx="11"
          fill={C.surface}
          fillOpacity="0.55"
          stroke={C.violet}
          strokeOpacity="0.45"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text x="300" y="367" fontSize="7" letterSpacing="1.1" fill={C.violet}>
          REQUEST / REPLY
        </text>
        <text x="300" y="387" fontSize="11" fontWeight="600" fill={C.inkDim}>
          GetShippingRate
        </text>
        <text x="300" y="401" fontSize="7.5" fill={C.faint}>
          pricing-svc
        </text>
        <text x="300" y="432" fontSize="7" fill={C.navLabel}>
          a request that expects a response
        </text>

        {/* ---- HANDLERS: Send, a one-way command ---- */}
        <text x="556" y="334" textAnchor="middle" fontSize="6.5" fill={C.amber}>
          command
        </text>
        <rect
          x="530"
          y="348"
          width="180"
          height="66"
          rx="11"
          fill={C.surface}
          fillOpacity="0.55"
          stroke={C.amber}
          strokeOpacity="0.4"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text x="544" y="367" fontSize="7" letterSpacing="1.1" fill={C.amber}>
          SEND &middot; POINT TO POINT
        </text>
        <text x="544" y="387" fontSize="11" fontWeight="600" fill={C.inkDim}>
          ShipOrder
        </text>
        <text x="544" y="401" fontSize="7.5" fill={C.faint}>
          shipping-svc
        </text>
        <text x="544" y="432" fontSize="7" fill={C.navLabel}>
          one-way, to exactly one handler
        </text>

        {/* ---- SAGA: tall coordinator rail, wired to the handlers lane ---- */}
        <rect
          x="746"
          y="44"
          width="230"
          height="452"
          rx="16"
          fill={C.surface}
          fillOpacity="0.55"
          stroke={C.accent}
          strokeOpacity="0.35"
          strokeWidth="1.25"
          vectorEffect="non-scaling-stroke"
        />
        <text x="766" y="72" fontSize="7" letterSpacing="1.6" fill={C.navLabel}>
          SAGA
        </text>
        <text x="766" y="92" fontSize="13" fontWeight="600" fill={C.heading}>
          OrderFulfillment
        </text>
        <text x="766" y="107" fontSize="8" fill={C.inkDim}>
          stateful coordinator
        </text>
        <rect
          x="864"
          y="58"
          width="92"
          height="20"
          rx="10"
          fill="none"
          stroke={C.accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="910"
          y="71"
          textAnchor="middle"
          fontSize="7.5"
          letterSpacing="0.6"
          fill={C.accent}
        >
          holds state
        </text>

        {/* events in, the saga reacts to the messages on the transport */}
        <line
          x1="730"
          y1="250"
          x2="764"
          y2="250"
          stroke={C.faint}
          strokeWidth="1"
          strokeDasharray="3 3"
          markerEnd="url(#mg3-arrowInk)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="748" y="244" textAnchor="middle" fontSize="6.5" fill={C.faint}>
          events
        </text>
        <circle cx="766" cy="250" r="2" fill={C.faint} />

        {/* the saga sends the ShipOrder command back down to the Send handler */}
        <line
          x1="766"
          y1="398"
          x2="712"
          y2="398"
          stroke={C.amber}
          strokeOpacity="0.85"
          strokeWidth="1.25"
          markerEnd="url(#mg3-arrowAmber)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="739" y="392" textAnchor="middle" fontSize="6.5" fill={C.amber}>
          sends ShipOrder
        </text>
        <circle cx="766" cy="398" r="2" fill={C.amber} />

        {/* the saga state machine, top to bottom */}
        {SAGA_STATES.map((s, i) => {
          const next = SAGA_STATES[i + 1];
          const active = s.state === "active";
          return (
            <g key={s.label}>
              {next !== undefined && (
                <>
                  <path
                    d={`M861 ${s.y + s.h + 6} l-4 -5 m4 5 l4 -5`}
                    fill="none"
                    stroke={C.faint}
                    strokeWidth="1"
                    vectorEffect="non-scaling-stroke"
                  />
                  {s.on !== undefined && (
                    <text
                      x="861"
                      y={s.y + s.h + 18}
                      textAnchor="middle"
                      fontSize="7"
                      fill={C.faint}
                    >
                      on {s.on}
                    </text>
                  )}
                </>
              )}
              <rect
                x="766"
                y={s.y}
                width="190"
                height={s.h}
                rx="8"
                fill={active ? C.accent : "none"}
                fillOpacity={active ? 0.1 : 1}
                stroke={active ? C.accent : C.cardBorder}
                strokeOpacity={active ? 0.6 : 1}
                strokeWidth="1"
                strokeDasharray={s.state === "next" ? "3 3" : undefined}
                vectorEffect="non-scaling-stroke"
              />
              {active && (
                <circle cx="778" cy={s.y + s.h / 2} r="2.5" fill={C.accent} />
              )}
              <text
                x="861"
                y={s.y + s.h / 2 + 3.5}
                textAnchor="middle"
                fontSize="9.5"
                fontWeight={active ? 600 : 400}
                fill={active ? C.accent : C.inkDim}
              >
                {s.label}
              </text>
            </g>
          );
        })}

        {/* timeout compensation path, routed along the rail margin */}
        <path
          d="M956 219 H966 V425 H958"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          markerEnd="url(#mg3-arrowCoral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="861" y="406" textAnchor="middle" fontSize="7" fill={C.coral}>
          on timeout
        </text>
        <rect
          x="766"
          y="412"
          width="190"
          height="26"
          rx="8"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="861"
          y="428.5"
          textAnchor="middle"
          fontSize="9.5"
          fill={C.coral}
        >
          Cancelled
        </text>
        <text x="861" y="462" textAnchor="middle" fontSize="7" fill={C.faint}>
          compensating actions run
        </text>
      </svg>

      <p className="text-cc-ink-dim border-cc-card-border mt-5 border-t pt-4 text-sm text-pretty">
        orders-svc answers the request with 200 OK, then publishes OrderPlaced
        onto the pluggable transport (RabbitMQ shown). A fan-out routes it to
        the publish/subscribe handlers, the request/reply node and the send
        command, while the OrderFulfillment saga coordinates the order across
        its own states.
      </p>
    </div>
  );
}

interface LegendDotProps {
  readonly color: string;
  readonly label: string;
}

function LegendDot({ color, label }: LegendDotProps) {
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

interface Transport {
  readonly label: string;
  readonly cx: number;
  readonly w: number;
  readonly active?: boolean;
}

/** The interchangeable transport options inside the band, left to right. The
 * active one carries the event in this snapshot. */
const TRANSPORTS: readonly Transport[] = [
  { label: "RabbitMQ", cx: 148, w: 92, active: true },
  { label: "Apache Kafka", cx: 312, w: 118 },
  { label: "Azure Service Bus", cx: 506, w: 150 },
  { label: "Amazon SQS", cx: 662, w: 116 },
];

interface SagaState {
  readonly label: string;
  readonly y: number;
  readonly h: number;
  readonly state: "done" | "active" | "next";
  readonly on?: string;
}

/** The saga state machine, top to bottom. Paid is the current state; `on` is
 * the event that advances each state to the next one below it. */
const SAGA_STATES: readonly SagaState[] = [
  { label: "Placed", y: 150, h: 26, state: "done", on: "OrderPlaced" },
  {
    label: "AwaitingPayment",
    y: 204,
    h: 30,
    state: "done",
    on: "PaymentReceived",
  },
  { label: "Paid", y: 262, h: 30, state: "active", on: "OrderShipped" },
  { label: "Shipped", y: 320, h: 26, state: "next" },
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette: navy surfaces, neutral ink, teal accent, green for the
 * synchronous response, violet for the request/reply pattern, amber for the
 * send command, coral for the compensation path. Status hues are data only. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  healthy: "#34d399",
  violet: "#8b8ff0",
  coral: "#f0786a",
} as const;
