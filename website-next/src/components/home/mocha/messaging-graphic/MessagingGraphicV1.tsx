/**
 * Mocha messaging flow, version 1: "Linear pipeline".
 *
 * A horizon line splits the public API surface from the services below it. Above
 * the line sits the Fusion gateway: a request comes in and 200 OK goes back out.
 * Below the line, the gateway resolves against the orders-svc subgraph, which
 * after responding publishes the OrderPlaced event onto a pluggable transport
 * (RabbitMQ, Apache Kafka, Azure Service Bus, Amazon SQS). Across a service
 * boundary the transport delivers to three messaging patterns, each on its own
 * service: publish/subscribe (two handlers), request/reply, and send. A
 * full-width OrderFulfillment saga holds state across the order lifecycle with a
 * compensation path. Static diagram, no motion. Three font sizes only. Every svg
 * id is prefixed "mg1-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// The shared horizontal flow line that the subgraph, transport, and the main
// publish/subscribe handler all sit on.
const MAIN_Y = 200;

export function MessagingGraphicV1() {
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
        <div className="flex items-center gap-4">
          <LegendDot color={C.accent} label="current saga state" ring />
        </div>
      </header>

      <svg
        viewBox="0 0 1000 710"
        width="100%"
        role="img"
        aria-label="A horizon line splits the public API surface from the services below. Above the line, the Fusion gateway takes a request and returns 200 OK. Below the line, the gateway resolves against the orders-svc subgraph, which then publishes the OrderPlaced event onto a pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS. Across a service boundary the transport delivers to three messaging patterns, each on its own service: publish and subscribe with two handlers, request and reply, and send. Below, an OrderFulfillment saga holds state across Placed, AwaitingPayment, Paid, and Shipped, with a compensation path on payment timeout."
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

        {/* ABOVE THE LINE: the public API surface, the Fusion gateway */}
        <text
          x="86"
          y="22"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.navLabel}
        >
          request
        </text>
        <line
          x1="86"
          y1="26"
          x2="86"
          y2="40"
          stroke={C.accent}
          strokeWidth="1.25"
          markerEnd="url(#mg1-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="160"
          y="22"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1="160"
          y1="40"
          x2="160"
          y2="26"
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg1-green)"
          vectorEffect="non-scaling-stroke"
        />
        <Node x={40} y={40} w={180} label="GATEWAY" name="Fusion" />

        {/* the horizon line dividing the surface from the services below */}
        <line
          x1="24"
          y1="122"
          x2="976"
          y2="122"
          stroke="rgba(245, 241, 234, 0.28)"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="24"
          y="116"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          API SURFACE
        </text>
        <text
          x="976"
          y="116"
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          what the client calls
        </text>
        <text
          x="24"
          y="138"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          SERVICES
        </text>
        <text
          x="976"
          y="138"
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          what runs underneath
        </text>

        {/* gateway resolves against the subgraph, crossing the line */}
        <line
          x1="130"
          y1="106"
          x2="130"
          y2="167"
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg1-teal)"
          vectorEffect="non-scaling-stroke"
        />

        {/* BELOW THE LINE: the orders-svc subgraph */}
        <Node x={40} y={167} w={180} label="SUBGRAPH" name="orders-svc" />

        {/* EMIT: after responding, the subgraph publishes onto the transport */}
        <line
          x1="220"
          y1={MAIN_Y}
          x2="438"
          y2={MAIN_Y}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg1-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <circle cx="220" cy={MAIN_Y} r="2.5" fill={C.accent} />
        <text
          x="329"
          y={MAIN_Y - 8}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>
        <text
          x="329"
          y={MAIN_Y + 17}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.faint}
        >
          after 200 OK
        </text>

        {/* TRANSPORT band: pluggable, the four interchangeable options */}
        <rect
          x="440"
          y="152"
          width="172"
          height="262"
          rx="16"
          fill={C.surface}
          fillOpacity="0.7"
          stroke={C.accent}
          strokeOpacity="0.3"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="526"
          y="174"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          TRANSPORT
        </text>
        <text
          x="526"
          y="188"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.accent}
        >
          pluggable
        </text>
        <line
          x1="440"
          y1={MAIN_Y}
          x2="612"
          y2={MAIN_Y}
          stroke={C.accent}
          strokeOpacity="0.3"
          strokeWidth="1"
          strokeDasharray="3 4"
          vectorEffect="non-scaling-stroke"
        />
        {TRANSPORTS.map((name, i) => {
          const active = i === 0;
          const top = 218 + i * 38;
          return (
            <g key={name}>
              <rect
                x="456"
                y={top}
                width="140"
                height="30"
                rx="9"
                fill={C.surface}
                stroke={active ? C.accent : C.cardBorder}
                strokeOpacity={active ? 0.6 : 1}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="526"
                y={top + 19}
                textAnchor="middle"
                fontSize={FS.name}
                fill={active ? C.heading : C.inkDim}
              >
                {name}
              </text>
            </g>
          );
        })}
        <text
          x="526"
          y="394"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.navLabel}
        >
          runs on any of these
        </text>

        {/* SERVICE BOUNDARY: the handlers each live on their own service */}
        <line
          x1="620"
          y1="160"
          x2="620"
          y2="458"
          stroke={C.inkFaint}
          strokeWidth="1"
          strokeDasharray="3 5"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="628"
          y="150"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          OTHER SERVICES
        </text>

        {/* PUBLISH / SUBSCRIBE: the main path, fanning to two handlers */}
        <text
          x="628"
          y="170"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.accent}
        >
          PUBLISH / SUBSCRIBE
        </text>
        <line
          x1="612"
          y1={MAIN_Y}
          x2="628"
          y2={MAIN_Y}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg1-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <path
          d="M618 200 V259 H628"
          fill="none"
          stroke={C.accent}
          strokeOpacity="0.7"
          strokeWidth="1.25"
          markerEnd="url(#mg1-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <PatternNode
          y={176}
          role="HANDLER"
          name="UpdateInventory"
          service="inventory-svc"
          tint={C.accent}
        />
        <PatternNode
          y={234}
          role="HANDLER"
          name="NotifyCustomer"
          service="notify-svc"
          tint={C.accent}
          batch
        />

        {/* REQUEST / REPLY */}
        <text
          x="628"
          y="308"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.violet}
        >
          REQUEST / REPLY
        </text>
        <line
          x1="612"
          y1="328"
          x2="628"
          y2="328"
          stroke={C.violet}
          strokeWidth="1.25"
          markerEnd="url(#mg1-violet)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="628"
          y1="342"
          x2="612"
          y2="342"
          stroke={C.violet}
          strokeOpacity="0.7"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg1-violet)"
          vectorEffect="non-scaling-stroke"
        />
        <PatternNode
          y={316}
          role="REPLIES"
          name="GetQuote"
          service="pricing-svc"
          tint={C.violet}
        />

        {/* SEND: point to point command */}
        <text
          x="628"
          y="390"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.amber}
        >
          SEND
        </text>
        <line
          x1="612"
          y1="410"
          x2="628"
          y2="410"
          stroke={C.amber}
          strokeWidth="1.25"
          markerEnd="url(#mg1-amber)"
          vectorEffect="non-scaling-stroke"
        />
        <PatternNode
          y={398}
          role="HANDLES"
          name="ShipOrder"
          service="shipping-svc"
          tint={C.amber}
        />

        {/* the transport feeds every pattern: a faint delivery rail */}
        <line
          x1="612"
          y1={MAIN_Y}
          x2="612"
          y2="410"
          stroke={C.accent}
          strokeOpacity="0.18"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />

        {/* SAGA: a stateful coordinator across the order lifecycle */}
        <rect
          x="40"
          y="476"
          width="920"
          height="214"
          rx="20"
          fill={C.surface}
          fillOpacity="0.5"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="62"
          y="508"
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text x="62" y="524" fontSize={FS.label} fill={C.navLabel}>
          stateful coordinator: reacts to events, sends commands
        </text>
        <rect
          x="836"
          y="492"
          width="100"
          height="22"
          rx="11"
          fill="none"
          stroke={C.accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="886"
          y="507"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.accent}
        >
          holds state
        </text>

        {SAGA_TRANSITIONS.map((tr) => (
          <g key={tr.label}>
            <line
              x1={tr.x1}
              y1="580"
              x2={tr.x2}
              y2="580"
              stroke={tr.command ? C.amber : C.faint}
              strokeOpacity={tr.command ? 0.9 : 1}
              strokeWidth="1.25"
              markerEnd={tr.command ? "url(#mg1-amber)" : "url(#mg1-ink)"}
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(tr.x1 + tr.x2) / 2}
              y="572"
              textAnchor="middle"
              fontSize={FS.label}
              fill={tr.command ? C.amber : C.navLabel}
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
                  y="554"
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
                y="561"
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
                y="585"
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

        {/* compensation path: a payment timeout cancels and compensates */}
        <line
          x1="380"
          y1="599"
          x2="380"
          y2="632"
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg1-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="392" y="620" fontSize={FS.label} fill={C.coral}>
          payment timeout
        </text>
        <rect
          x="305"
          y="635"
          width="150"
          height="34"
          rx="10"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="380"
          y="656"
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
          fillOpacity="0.9"
        >
          Cancelled, compensate
        </text>
      </svg>
    </div>
  );
}

interface NodeProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly label: string;
  readonly name: string;
}

/** A pipeline entry node (gateway, subgraph): a type label over a name. */
function Node({ x, y, w, label, name }: NodeProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={66}
        rx="14"
        fill={C.surface}
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 16}
        y={y + 27}
        fontSize={FS.label}
        letterSpacing="1.4"
        fill={C.navLabel}
      >
        {label}
      </text>
      <text
        x={x + 16}
        y={y + 46}
        fontSize={FS.name}
        fontWeight="600"
        fill={C.heading}
      >
        {name}
      </text>
    </g>
  );
}

interface PatternNodeProps {
  readonly y: number;
  readonly role: string;
  readonly name: string;
  readonly service: string;
  readonly tint: string;
  readonly batch?: boolean;
}

/** One destination node, on its own service, across the boundary. */
function PatternNode({
  y,
  role,
  name,
  service,
  tint,
  batch,
}: PatternNodeProps) {
  return (
    <g>
      <rect
        x="628"
        y={y}
        width="340"
        height="50"
        rx="11"
        fill={C.surface}
        fillOpacity="0.55"
        stroke={tint}
        strokeOpacity="0.5"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="646"
        y={y + 18}
        fontSize={FS.label}
        letterSpacing="1.2"
        fill={C.navLabel}
      >
        {role}
      </text>
      <text x="646" y={y + 34} fontSize={FS.name} fill={C.heading}>
        {name}
      </text>
      <circle cx="649" cy={y + 43} r="1.6" fill={C.navLabel} />
      <text x="656" y={y + 46} fontSize={FS.label} fill={C.inkDim}>
        {service}
      </text>
      {batch && (
        <>
          <rect
            x="902"
            y={y + 9}
            width="50"
            height="18"
            rx="9"
            fill="none"
            stroke={C.accent}
            strokeOpacity="0.5"
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="927"
            y={y + 21}
            textAnchor="middle"
            fontSize={FS.label}
            fill={C.accent}
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
  readonly ring?: boolean;
}

function LegendDot({ color, label, ring }: LegendDotProps) {
  return (
    <span className="text-cc-ink-dim inline-flex items-center gap-1.5 font-mono text-[0.55rem] tracking-[0.08em] uppercase">
      <span
        aria-hidden="true"
        className="inline-block size-2 rounded-full"
        style={
          ring ? { border: `1.5px solid ${color}` } : { backgroundColor: color }
        }
      />
      {label}
    </span>
  );
}

interface Arrow {
  readonly id: string;
  readonly fill: string;
}

const ARROWS: readonly Arrow[] = [
  { id: "mg1-teal", fill: "#5eead4" },
  { id: "mg1-violet", fill: "#8b8ff0" },
  { id: "mg1-amber", fill: "#fbbf24" },
  { id: "mg1-green", fill: "#34d399" },
  { id: "mg1-coral", fill: "#f0786a" },
  { id: "mg1-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

const TRANSPORTS: readonly string[] = [
  "RabbitMQ",
  "Apache Kafka",
  "Azure Service Bus",
  "Amazon SQS",
];

interface SagaState {
  readonly name: string;
  readonly cx: number;
  readonly state: "done" | "current" | "next";
}

/** The saga's state machine, left to right, with Paid as the current state. */
const SAGA_STATES: readonly SagaState[] = [
  { name: "Placed", cx: 150, state: "done" },
  { name: "AwaitingPayment", cx: 380, state: "done" },
  { name: "Paid", cx: 610, state: "current" },
  { name: "Shipped", cx: 840, state: "next" },
];

interface SagaTransition {
  readonly label: string;
  readonly x1: number;
  readonly x2: number;
  readonly command?: boolean;
}

/** The labelled transitions between saga states: events in, a command out. */
const SAGA_TRANSITIONS: readonly SagaTransition[] = [
  { label: "OrderPlaced", x1: 225, x2: 303 },
  { label: "PaymentReceived", x1: 455, x2: 533 },
  { label: "sends ShipOrder", x1: 685, x2: 763, command: true },
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette: navy surfaces, neutral ink, teal accent, status colours
 * used as categorical data markers for the patterns and the saga. */
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
  coral: "#f0786a",
  violet: "#8b8ff0",
  healthy: "#34d399",
} as const;
