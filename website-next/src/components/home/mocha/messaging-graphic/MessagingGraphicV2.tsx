/**
 * Messaging-flow graphic, version 2: the "centered message bus" layout.
 *
 * The diagram flows top to bottom off one horizontal bus. Across the top, the
 * pluggable transport sits as a full-width row of four interchangeable chips
 * (RabbitMQ is the active one). Just below, the message bus is a glowing teal
 * spine that spans nearly the full width; orders-svc sits at the far left at bus
 * height, answers a request with 200 OK, and only then publishes OrderPlaced
 * onto the bus. Three evenly spaced pattern columns drop off the bus:
 * publish/subscribe fans to two handlers, request/reply replies back up to the
 * bus, and send is a one-way command. A full-width OrderFulfillment saga panel
 * holds the order's state across the bottom. Static diagram, no motion. Every
 * svg id is prefixed "mg2-".
 */
export function MessagingGraphicV2() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-3xl border p-5 backdrop-blur-sm sm:p-8">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <h3 className="font-heading text-cc-heading text-base font-semibold sm:text-lg">
          One event, any transport
        </h3>
        <div className="flex items-center gap-3.5">
          <LegendDot color={C.accent} label="main path" />
          <LegendDot color={C.healthy} label="200 OK" />
          <LegendDot color={C.violet} label="saga" />
        </div>
      </header>

      <svg
        viewBox="0 0 1000 500"
        width="100%"
        role="img"
        aria-label="A pluggable transport row (RabbitMQ active, plus Apache Kafka, Azure Service Bus, Amazon SQS) sits above a horizontal message bus. orders-svc at the far left answers a request with 200 OK and then publishes OrderPlaced onto the bus. Three pattern columns drop off the bus: publish and subscribe fanning to UpdateInventory and NotifyCustomer, a request and reply GetQuote node that replies back up to the bus, and a one-way send command ShipOrder. A full-width OrderFulfillment saga coordinates Placed, AwaitingPayment, Paid and Shipped with a payment-timeout compensation to Cancelled."
        className="mt-5"
        style={{ display: "block", overflow: "visible", fontFamily: MONO }}
      >
        <defs>
          <filter id="mg2-glow" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="4" />
          </filter>
          <marker
            id="mg2-arrowTeal"
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
            id="mg2-arrowGreen"
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
            id="mg2-arrowAmber"
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
            id="mg2-arrowCoral"
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
            id="mg2-arrowInk"
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

        {/* TRANSPORT ROW: pluggable transport, full width, above the bus */}
        <text x="32" y="16" fontSize="8" letterSpacing="1.4" fill={C.navLabel}>
          PLUGGABLE TRANSPORT
        </text>
        <text x="968" y="16" textAnchor="end" fontSize="8" fill={C.faint}>
          the bus runs on any of these
        </text>
        {TRANSPORTS.map((t, i) => {
          const cx = 32 + 234 * i + 117;
          const x = cx - 98;
          return (
            <g key={t.name}>
              <rect
                x={x}
                y="28"
                width="196"
                height="28"
                rx="14"
                fill={t.active ? C.accent : C.surface}
                fillOpacity={t.active ? 0.06 : 1}
                stroke={t.active ? C.accent : C.cardBorder}
                strokeOpacity={t.active ? 0.55 : 1}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              {t.active && <circle cx={x + 16} cy="42" r="3" fill={C.accent} />}
              <text
                x={t.active ? x + 26 : cx}
                y="46"
                textAnchor={t.active ? "start" : "middle"}
                fontSize="9"
                fill={t.active ? C.accent : C.inkDim}
              >
                {t.name}
              </text>
              {t.active && (
                <text
                  x={cx}
                  y="69"
                  textAnchor="middle"
                  fontSize="7"
                  letterSpacing="0.8"
                  fill={C.accent}
                >
                  active
                </text>
              )}
            </g>
          );
        })}

        {/* ORDER SERVICE: synchronous request in, 200 OK out, on the node */}
        <text x="16" y="121" fontSize="7.5" fill={C.navLabel}>
          request
        </text>
        <line
          x1="16"
          y1="128"
          x2="54"
          y2="128"
          stroke={C.accent}
          strokeOpacity="0.75"
          strokeWidth="1.25"
          markerEnd="url(#mg2-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="54"
          y1="152"
          x2="16"
          y2="152"
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg2-arrowGreen)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="16" y="166" fontSize="7.5" fill={C.healthy}>
          200 OK
        </text>
        <rect
          x="58"
          y="108"
          width="150"
          height="64"
          rx="12"
          fill={C.surface}
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="70"
          y="128"
          fontSize="6.5"
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          ORDER SERVICE
        </text>
        <text x="70" y="146" fontSize="12" fontWeight="600" fill={C.heading}>
          orders-svc
        </text>
        <text x="70" y="160" fontSize="7.5" fill={C.faint}>
          request -&gt; 200 OK, synchronous
        </text>

        {/* THE MESSAGE BUS: glowing teal spine across nearly the full width */}
        <line
          x1="210"
          y1="140"
          x2="952"
          y2="140"
          stroke={C.accent}
          strokeOpacity="0.4"
          strokeWidth="6"
          strokeLinecap="round"
          filter="url(#mg2-glow)"
        />
        <line
          x1="210"
          y1="140"
          x2="952"
          y2="140"
          stroke={C.accent}
          strokeWidth="2.5"
          strokeLinecap="round"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="950"
          y="131"
          textAnchor="end"
          fontSize="8"
          letterSpacing="1.6"
          fill={C.accent}
        >
          MESSAGE BUS
        </text>

        {/* the join: after the 200 OK, orders-svc publishes onto the bus */}
        <circle cx="210" cy="140" r="3" fill={C.accent} />
        <text x="218" y="129" fontSize="8" fill={C.accent}>
          publishes OrderPlaced (after 200 OK)
        </text>

        {/* bus taps: each pattern column drops straight off the spine */}
        <circle cx="300" cy="140" r="2.5" fill={C.accent} />
        <circle cx="500" cy="140" r="2.5" fill={C.accent} />
        <circle cx="830" cy="140" r="2.5" fill={C.accent} />

        {/* COLUMN 1: publish / subscribe, the main path fanning to two handlers */}
        <text x="80" y="190" fontSize="7.5" letterSpacing="1.2" fill={C.accent}>
          PUBLISH / SUBSCRIBE
        </text>
        <path
          d="M300 140 L300 172"
          fill="none"
          stroke={C.accent}
          strokeWidth="1.5"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="300"
          y1="172"
          x2="262"
          y2="224"
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg2-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="300"
          y1="172"
          x2="262"
          y2="296"
          stroke={C.accent}
          strokeOpacity="0.7"
          strokeWidth="1.5"
          markerEnd="url(#mg2-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <NodeBox
          x={80}
          y={196}
          role="HANDLER"
          name="UpdateInventory"
          service="inventory-svc"
          accent
          batch
        />
        <NodeBox
          x={80}
          y={268}
          role="HANDLER"
          name="NotifyCustomer"
          service="notify-svc"
        />

        {/* COLUMN 2: request / reply, replies back up to the bus */}
        <text
          x="415"
          y="190"
          fontSize="7.5"
          letterSpacing="1.2"
          fill={C.accent}
        >
          REQUEST / REPLY
        </text>
        <line
          x1="492"
          y1="140"
          x2="492"
          y2="196"
          stroke={C.accent}
          strokeOpacity="0.85"
          strokeWidth="1.25"
          markerEnd="url(#mg2-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="484" y="170" textAnchor="end" fontSize="6.5" fill={C.accent}>
          request
        </text>
        <line
          x1="508"
          y1="196"
          x2="508"
          y2="140"
          stroke={C.accent}
          strokeOpacity="0.6"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg2-arrowTeal)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="516" y="170" fontSize="6.5" fill={C.accent} fillOpacity="0.7">
          reply
        </text>
        <NodeBox
          x={415}
          y={196}
          role="REPLIES"
          name="GetQuote"
          service="pricing-svc"
        />

        {/* COLUMN 3: send, a one-way point-to-point command */}
        <text x="745" y="190" fontSize="7.5" letterSpacing="1.2" fill={C.amber}>
          SEND
        </text>
        <line
          x1="830"
          y1="140"
          x2="830"
          y2="196"
          stroke={C.amber}
          strokeOpacity="0.9"
          strokeWidth="1.25"
          markerEnd="url(#mg2-arrowAmber)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="838" y="170" fontSize="6.5" fill={C.amber}>
          command
        </text>
        <NodeBox
          x={745}
          y={196}
          role="HANDLES"
          name="ShipOrder"
          service="shipping-svc"
          tint={C.amber}
        />

        {/* SAGA: full-width stateful coordinator across the order's lifecycle */}
        <rect
          x="32"
          y="344"
          width="936"
          height="148"
          rx="18"
          fill={C.surface}
          fillOpacity="0.5"
          stroke={C.violet}
          strokeOpacity="0.35"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text x="52" y="374" fontSize="13" fontWeight="600" fill={C.violet}>
          OrderFulfillment saga
        </text>
        <text x="52" y="390" fontSize="8" fill={C.navLabel}>
          stateful coordinator: reacts to events, sends commands
        </text>
        <rect
          x="842"
          y="358"
          width="110"
          height="22"
          rx="11"
          fill="none"
          stroke={C.violet}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="897"
          y="373"
          textAnchor="middle"
          fontSize="7.5"
          letterSpacing="0.6"
          fill={C.violet}
        >
          holds state
        </text>

        {SAGA_TRANSITIONS.map((t) => (
          <g key={t.label}>
            <line
              x1={t.x1}
              y1="432"
              x2={t.x2}
              y2="432"
              stroke={t.command ? C.amber : C.faint}
              strokeOpacity={t.command ? 0.9 : 1}
              strokeWidth="1.25"
              markerEnd={
                t.command ? "url(#mg2-arrowAmber)" : "url(#mg2-arrowInk)"
              }
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(t.x1 + t.x2) / 2}
              y="424"
              textAnchor="middle"
              fontSize="7"
              fill={t.command ? C.amber : C.navLabel}
            >
              {t.label}
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
                  y="408"
                  textAnchor="middle"
                  fontSize="6.5"
                  letterSpacing="1"
                  fill={C.violet}
                >
                  CURRENT
                </text>
              )}
              <rect
                x={s.cx - 75}
                y="414"
                width="150"
                height="36"
                rx="10"
                fill={current ? C.violet : "none"}
                fillOpacity={current ? 0.14 : 1}
                stroke={current ? C.violet : next ? C.inkFaint : C.violet}
                strokeOpacity={current ? 0.7 : next ? 1 : 0.4}
                strokeWidth="1"
                strokeDasharray={next ? "3 3" : undefined}
                vectorEffect="non-scaling-stroke"
              />
              <text
                x={s.cx}
                y="437"
                textAnchor="middle"
                fontSize="10"
                fontWeight={current ? 600 : 400}
                fill={current ? C.violet : next ? C.faint : C.inkDim}
              >
                {s.name}
              </text>
            </g>
          );
        })}

        {/* compensation path: a payment timeout cancels and compensates */}
        <line
          x1="376"
          y1="450"
          x2="376"
          y2="464"
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg2-arrowCoral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="386" y="461" fontSize="6.5" fill={C.coral}>
          payment timeout
        </text>
        <rect
          x="301"
          y="464"
          width="150"
          height="26"
          rx="9"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="376"
          y="481"
          textAnchor="middle"
          fontSize="8.5"
          fill={C.coral}
          fillOpacity="0.9"
        >
          Cancelled, compensate
        </text>
      </svg>

      <p className="text-cc-ink-dim border-cc-card-border mt-5 border-t pt-4 text-sm text-pretty">
        orders-svc returns 200 OK, then publishes OrderPlaced onto the message
        bus. The bus runs on any transport, and off it drop the
        publish/subscribe handlers, the request/reply node, and the send
        command, while the OrderFulfillment saga coordinates the order&rsquo;s
        state.
      </p>
    </div>
  );
}

interface NodeBoxProps {
  readonly x: number;
  readonly y: number;
  readonly role: string;
  readonly name: string;
  readonly service: string;
  readonly accent?: boolean;
  readonly tint?: string;
  readonly batch?: boolean;
}

/** One pattern destination box dropping off the bus. Fixed width and height. */
function NodeBox({
  x,
  y,
  role,
  name,
  service,
  accent,
  tint,
  batch,
}: NodeBoxProps) {
  const stroke = accent ? C.accent : (tint ?? C.cardBorder);
  return (
    <g>
      <rect
        x={x}
        y={y}
        width="180"
        height="56"
        rx="12"
        fill={C.surface}
        fillOpacity="0.55"
        stroke={stroke}
        strokeOpacity={accent || tint ? 0.5 : 1}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 16}
        y={y + 18}
        fontSize="6.5"
        letterSpacing="1.2"
        fill={C.navLabel}
      >
        {role}
      </text>
      <text
        x={x + 16}
        y={y + 35}
        fontSize="11"
        fontWeight="600"
        fill={accent ? C.accent : C.inkDim}
      >
        {name}
      </text>
      <text x={x + 16} y={y + 49} fontSize="7.5" fill={C.faint}>
        {service}
      </text>
      {batch && (
        <>
          <rect
            x={x + 132}
            y={y + 6}
            width="42"
            height="15"
            rx="7.5"
            fill={C.amber}
            fillOpacity="0.12"
            stroke={C.amber}
            strokeOpacity="0.7"
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x={x + 153}
            y={y + 16.5}
            textAnchor="middle"
            fontSize="7.5"
            fill={C.amber}
          >
            batch
          </text>
        </>
      )}
    </g>
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

/** The interchangeable transports the bus can run on. RabbitMQ is active. */
const TRANSPORTS: readonly {
  readonly name: string;
  readonly active?: boolean;
}[] = [
  { name: "RabbitMQ", active: true },
  { name: "Apache Kafka" },
  { name: "Azure Service Bus" },
  { name: "Amazon SQS" },
];

interface SagaState {
  readonly name: string;
  readonly cx: number;
  readonly state: "done" | "current" | "next";
}

/** The saga's state machine, left to right, with Paid as the current state. */
const SAGA_STATES: readonly SagaState[] = [
  { name: "Placed", cx: 127, state: "done" },
  { name: "AwaitingPayment", cx: 376, state: "done" },
  { name: "Paid", cx: 624, state: "current" },
  { name: "Shipped", cx: 873, state: "next" },
];

interface SagaTransition {
  readonly label: string;
  readonly x1: number;
  readonly x2: number;
  readonly command?: boolean;
}

/** The labelled transitions between saga states: events in, a command out. */
const SAGA_TRANSITIONS: readonly SagaTransition[] = [
  { label: "OrderPlaced", x1: 202, x2: 301 },
  { label: "PaymentReceived", x1: 451, x2: 549 },
  { label: "sends ShipOrder", x1: 699, x2: 798, command: true },
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette: navy surfaces, neutral ink, teal accent, plus status
 * colors used as data only (green for the sync reply, violet for the saga,
 * coral for the compensation path, amber for the send command and batch tag). */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
  healthy: "#34d399",
  amber: "#fbbf24",
  violet: "#8b8ff0",
  coral: "#f0786a",
} as const;
