/**
 * Mocha messaging flow, candidate C: "Surface and substrate".
 *
 * A crisp horizon line is the hero. Above it sits the public API surface: the
 * Fusion gateway takes a request and returns 200 OK. Below it sits the messaging
 * substrate. The gateway resolves against the orders-svc subgraph, which after
 * responding publishes the OrderPlaced event onto a pluggable transport
 * (RabbitMQ, Apache Kafka, Azure Service Bus, Amazon SQS, all interchangeable).
 * Across a service boundary the transport delivers to three patterns, each on its
 * own service: publish/subscribe (two subscribers, one batch), request/reply, and
 * send. A stateful OrderFulfillment saga panel holds the order lifecycle with a
 * compensation path. Fully static, no motion. Three font sizes only. Every svg id
 * is prefixed "candc-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// The primary event path sits on a single horizontal line: the subgraph
// publishes, the transport delivers, and the main subscriber receives.
const SPINE_Y = 244;

export function CandidateC() {
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
          primary event path
        </span>
      </header>

      <svg
        viewBox="0 0 1000 748"
        width="100%"
        role="img"
        aria-label="A crisp horizon line splits the public API surface from the messaging substrate. Above the line, the Fusion gateway takes a request and returns 200 OK. Below the line, the gateway resolves against the orders-svc subgraph, which then publishes the OrderPlaced event onto a pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS. Across a service boundary the transport delivers to three messaging patterns, each on its own service: publish and subscribe with two subscribers (one tagged batch), request and reply, and send. Below, an OrderFulfillment saga holds state across Placed, AwaitingPayment, Paid, and Shipped, with a compensation path on payment timeout."
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

        {/* ABOVE THE LINE: the public API surface, calm and prominent */}
        <text
          x="98"
          y="16"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.navLabel}
        >
          request
        </text>
        <line
          x1="98"
          y1="24"
          x2="98"
          y2="44"
          stroke={C.line}
          strokeWidth="1.25"
          markerEnd="url(#candc-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="178"
          y="16"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1="178"
          y1="44"
          x2="178"
          y2="24"
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#candc-green)"
          vectorEffect="non-scaling-stroke"
        />
        <EntryNode x={48} y={44} label="GATEWAY" name="Fusion" />
        <text
          x="138"
          y="130"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.faint}
        >
          public GraphQL API
        </text>

        {/* the horizon line: the hero divide between surface and substrate */}
        <line
          x1="48"
          y1="150"
          x2="952"
          y2="150"
          stroke="rgba(245, 241, 234, 0.22)"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="952"
          y="142"
          textAnchor="end"
          fontSize={FS.label}
          letterSpacing="1.8"
          fill={C.navLabel}
        >
          PUBLIC API SURFACE
        </text>
        <text
          x="952"
          y="168"
          textAnchor="end"
          fontSize={FS.label}
          letterSpacing="1.8"
          fill={C.navLabel}
        >
          MESSAGING SUBSTRATE
        </text>

        {/* gateway resolves against the subgraph, crossing the horizon */}
        <line
          x1="138"
          y1="110"
          x2="138"
          y2="211"
          stroke={C.line}
          strokeWidth="1.25"
          markerEnd="url(#candc-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="150" y="184" fontSize={FS.label} fill={C.navLabel}>
          resolves
        </text>

        {/* BELOW THE LINE: the orders-svc subgraph, origin of the event */}
        <EntryNode x={48} y={211} label="SUBGRAPH" name="orders-svc" />

        {/* PRIMARY EVENT PATH: the subgraph publishes onto the transport */}
        <circle cx="228" cy={SPINE_Y} r="2.5" fill={C.accent} />
        <line
          x1="228"
          y1={SPINE_Y}
          x2="388"
          y2={SPINE_Y}
          stroke={C.accent}
          strokeWidth="1.25"
          markerEnd="url(#candc-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="308"
          y={SPINE_Y - 8}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>
        <text
          x="308"
          y={SPINE_Y + 14}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.faint}
        >
          after 200 OK
        </text>

        {/* TRANSPORT: a tidy, pluggable group of interchangeable options */}
        <rect
          x="388"
          y="200"
          width="192"
          height="288"
          rx="14"
          fill={C.surface}
          fillOpacity="0.55"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="484"
          y="227"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.8"
          fill={C.navLabel}
        >
          TRANSPORT
        </text>
        <text
          x="484"
          y="241"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.accent}
        >
          pluggable
        </text>
        {TRANSPORTS.map((name, i) => (
          <g key={name}>
            <rect
              x="404"
              y={270 + i * 48}
              width="160"
              height="36"
              rx="9"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="484"
              y={270 + i * 48 + 23}
              textAnchor="middle"
              fontSize={FS.name}
              fill={C.inkDim}
            >
              {name}
            </text>
          </g>
        ))}

        {/* SERVICE BOUNDARY: each handler lives on its own service */}
        <line
          x1="614"
          y1="200"
          x2="614"
          y2="500"
          stroke={C.inkFaint}
          strokeWidth="1"
          strokeDasharray="3 5"
          vectorEffect="non-scaling-stroke"
        />
        <text
          transform="rotate(-90 600 350)"
          x="600"
          y="350"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="2"
          fill={C.navLabel}
        >
          SERVICE BOUNDARY
        </text>

        {/* DELIVERY: primary path to the main subscriber, plus a quiet fan */}
        <line
          x1="580"
          y1={SPINE_Y}
          x2="636"
          y2={SPINE_Y}
          stroke={C.accent}
          strokeWidth="1.25"
          markerEnd="url(#candc-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="614"
          y1={SPINE_Y}
          x2="614"
          y2="470"
          stroke={C.line}
          strokeWidth="1.25"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="614"
          y1="306"
          x2="636"
          y2="306"
          stroke={C.line}
          strokeWidth="1.25"
          markerEnd="url(#candc-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="614"
          y1="388"
          x2="636"
          y2="388"
          stroke={C.line}
          strokeWidth="1.25"
          markerStart="url(#candc-ink)"
          markerEnd="url(#candc-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="614"
          y1="470"
          x2="636"
          y2="470"
          stroke={C.line}
          strokeWidth="1.25"
          markerEnd="url(#candc-ink)"
          vectorEffect="non-scaling-stroke"
        />

        {/* PATTERN GROUPS: section labels stay neutral, no rainbow */}
        <text
          x="636"
          y="210"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          PUBLISH / SUBSCRIBE
        </text>
        <DestNode
          y={218}
          role="SUBSCRIBER"
          name="UpdateInventory"
          service="inventory-svc"
          accent
        />
        <DestNode
          y={280}
          role="SUBSCRIBER"
          name="NotifyCustomer"
          service="notify-svc"
          batch
        />
        <text
          x="636"
          y="354"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          REQUEST / REPLY
        </text>
        <DestNode
          y={362}
          role="REPLIER"
          name="GetQuote"
          service="pricing-svc"
        />
        <text
          x="636"
          y="436"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          SEND
        </text>
        <DestNode
          y={444}
          role="HANDLER"
          name="ShipOrder"
          service="shipping-svc"
        />

        {/* SAGA: an elegant panel holding the order lifecycle */}
        <rect
          x="48"
          y="524"
          width="904"
          height="204"
          rx="14"
          fill={C.surface}
          fillOpacity="0.5"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="72"
          y="553"
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text x="72" y="569" fontSize={FS.label} fill={C.navLabel}>
          stateful coordinator: reacts to events, sends commands
        </text>
        <rect
          x="826"
          y="540"
          width="102"
          height="22"
          rx="11"
          fill="none"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="877"
          y="555"
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.navLabel}
        >
          holds state
        </text>

        {SAGA_TX.map((tr) => (
          <g key={tr.label}>
            <line
              x1={tr.x1}
              y1="622"
              x2={tr.x2}
              y2="622"
              stroke={C.line}
              strokeWidth="1.25"
              markerEnd="url(#candc-ink)"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(tr.x1 + tr.x2) / 2}
              y="614"
              textAnchor="middle"
              fontSize={FS.label}
              fill={C.navLabel}
            >
              {tr.label}
            </text>
          </g>
        ))}
        {SAGA_STATES.map((s) => {
          const current = s.kind === "current";
          const next = s.kind === "next";
          return (
            <g key={s.name}>
              {current && (
                <text
                  x={s.cx}
                  y="592"
                  textAnchor="middle"
                  fontSize={FS.label}
                  letterSpacing="1"
                  fill={C.accent}
                >
                  CURRENT
                </text>
              )}
              <rect
                x={s.cx - 70}
                y="602"
                width="140"
                height="40"
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
                y="627"
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

        {/* compensation: a payment timeout cancels and compensates */}
        <line
          x1="395"
          y1="642"
          x2="395"
          y2="674"
          stroke={C.coral}
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#candc-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="407" y="664" fontSize={FS.label} fill={C.coral}>
          payment timeout
        </text>
        <rect
          x="325"
          y="676"
          width="140"
          height="36"
          rx="10"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="395"
          y="699"
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
        >
          Cancelled, compensate
        </text>
      </svg>
    </div>
  );
}

interface EntryNodeProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly name: string;
}

/** A pipeline entry node (gateway, subgraph): a type label over a name. */
function EntryNode({ x, y, label, name }: EntryNodeProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={180}
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

interface DestNodeProps {
  readonly y: number;
  readonly role: string;
  readonly name: string;
  readonly service: string;
  readonly accent?: boolean;
  readonly batch?: boolean;
}

/** One destination handler, on its own service, across the boundary. */
function DestNode({ y, role, name, service, accent, batch }: DestNodeProps) {
  return (
    <g>
      <rect
        x="636"
        y={y}
        width="316"
        height="52"
        rx="14"
        fill={C.surface}
        fillOpacity="0.55"
        stroke={accent ? C.accent : C.cardBorder}
        strokeOpacity={accent ? 0.5 : 1}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="654"
        y={y + 17}
        fontSize={FS.label}
        letterSpacing="1.2"
        fill={accent ? C.accent : C.navLabel}
      >
        {role}
      </text>
      <text
        x="654"
        y={y + 34}
        fontSize={FS.name}
        fontWeight="600"
        fill={C.heading}
      >
        {name}
      </text>
      <circle cx="657" cy={y + 44} r="1.6" fill={C.navLabel} />
      <text x="664" y={y + 47} fontSize={FS.label} fill={C.inkDim}>
        {service}
      </text>
      {batch && (
        <>
          <rect
            x="886"
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
            x="911"
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

interface Arrow {
  readonly id: string;
  readonly fill: string;
}

const ARROWS: readonly Arrow[] = [
  { id: "candc-teal", fill: "#5eead4" },
  { id: "candc-green", fill: "#34d399" },
  { id: "candc-coral", fill: "#f0786a" },
  { id: "candc-ink", fill: "rgba(245, 241, 234, 0.5)" },
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
  readonly kind: "done" | "current" | "next";
}

/** The saga's state machine, left to right, with Paid as the current state. */
const SAGA_STATES: readonly SagaState[] = [
  { name: "Placed", cx: 185, kind: "done" },
  { name: "AwaitingPayment", cx: 395, kind: "done" },
  { name: "Paid", cx: 605, kind: "current" },
  { name: "Shipped", cx: 815, kind: "next" },
];

interface SagaTransition {
  readonly label: string;
  readonly x1: number;
  readonly x2: number;
}

/** The labelled transitions between saga states: events in, a command out. */
const SAGA_TX: readonly SagaTransition[] = [
  { label: "OrderPlaced", x1: 255, x2: 325 },
  { label: "PaymentReceived", x1: 465, x2: 535 },
  { label: "sends ShipOrder", x1: 675, x2: 745 },
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked cc-* palette: navy surfaces, neutral ink, teal accent, status colours
 * rationed to the 200 OK and the compensation path. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  line: "rgba(245, 241, 234, 0.32)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  healthy: "#34d399",
} as const;
