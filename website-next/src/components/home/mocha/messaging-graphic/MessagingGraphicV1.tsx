/**
 * Mocha messaging flow, version 1: "Calm centered flow".
 *
 * A centered, symmetric top-to-bottom composition. The Fusion gateway sits
 * centered above a full-width horizon line (a request comes in, 200 OK goes
 * back out). The horizon separates the public API surface above from the
 * services below. Below it a centered vertical spine: the orders-svc subgraph
 * publishes the OrderPlaced event down onto one pluggable transport (the four
 * interchangeable options shown inline). From the transport the event fans out
 * symmetrically across a service boundary to three messaging patterns, each on
 * its own service: publish/subscribe (two subscribers, one batch), request/
 * reply, and send. A clean full-width OrderFulfillment saga holds state across
 * the order lifecycle with a timeout/compensation path. Fully static, no
 * motion. Three font sizes only. Every svg id is prefixed "mg1-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// The centered vertical spine that the gateway, subgraph and transport share.
const CX = 500;

// The three pattern cards sit on one row and share these dimensions.
const CARD_Y = 390;
const CARD_H = 116;
const CARD_W = 280;
const LEFT_X = 40;
const MID_X = 360;
const RIGHT_X = 680;

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
        aria-label="A horizon line separates the public API surface from the services below. Above the line the Fusion gateway takes a request and returns 200 OK. Below the line the gateway resolves against the orders-svc subgraph, which then publishes the OrderPlaced event onto one pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS. Across a service boundary the transport delivers to three messaging patterns, each on its own service: publish and subscribe with two subscribers (one batch), request and reply, and send. A full-width OrderFulfillment saga holds state across Placed, AwaitingPayment, Paid, and Shipped, with a payment timeout compensation path to Cancelled."
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

        {/* ABOVE THE LINE: request in, 200 OK out, around the gateway */}
        <text
          x={CX - 40}
          y="18"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          REQUEST
        </text>
        <line
          x1={CX - 40}
          y1="24"
          x2={CX - 40}
          y2="42"
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg1-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX + 40}
          y="18"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1={CX + 40}
          y1="42"
          x2={CX + 40}
          y2="24"
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg1-green)"
          vectorEffect="non-scaling-stroke"
        />
        <SpineNode x={CX - 100} y={44} w={200} label="GATEWAY" name="Fusion" />

        {/* The horizon line dividing the public surface from the services */}
        <line
          x1="32"
          y1="138"
          x2="968"
          y2="138"
          stroke="rgba(245, 241, 234, 0.2)"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="32"
          y="132"
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          API SURFACE
        </text>
        <text
          x="968"
          y="132"
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          the public GraphQL API
        </text>
        <text
          x="32"
          y="152"
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SERVICES
        </text>
        <text
          x="968"
          y="152"
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          internal, not exposed
        </text>

        {/* Gateway resolves against the subgraph, crossing the line */}
        <line
          x1={CX}
          y1="102"
          x2={CX}
          y2="166"
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg1-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX + 12}
          y="160"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          RESOLVES
        </text>

        {/* BELOW THE LINE: the orders-svc subgraph */}
        <SpineNode
          x={CX - 100}
          y={168}
          w={200}
          label="SUBGRAPH"
          name="orders-svc"
        />

        {/* PRIMARY EVENT: after responding, the subgraph publishes downward */}
        <circle cx={CX} cy="226" r="2.5" fill={C.accent} />
        <line
          x1={CX}
          y1="226"
          x2={CX}
          y2="270"
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg1-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX + 12}
          y="242"
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>
        <text x={CX + 12} y="256" fontSize={FS.label} fill={C.faint}>
          after 200 OK
        </text>

        {/* The single, pluggable transport: four interchangeable options inline */}
        <rect
          x="180"
          y="272"
          width="640"
          height="58"
          rx="14"
          fill={C.surface}
          fillOpacity="0.6"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX}
          y="290"
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.8"
          fill={C.navLabel}
        >
          PLUGGABLE TRANSPORT, SAME CODE
        </text>
        {[340, 500, 660].map((dx) => (
          <line
            key={dx}
            x1={dx}
            y1="302"
            x2={dx}
            y2="324"
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
        ))}
        {TRANSPORTS.map((t) => (
          <text
            key={t.name}
            x={t.cx}
            y="316"
            textAnchor="middle"
            fontSize={FS.name}
            fill={C.inkDim}
          >
            {t.name}
          </text>
        ))}

        {/* FAN-OUT across the service boundary to the three patterns */}
        <text
          x="32"
          y="372"
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SERVICE BOUNDARY
        </text>
        {/* primary spine continues straight down to the main subscriber */}
        <line
          x1={CX}
          y1="330"
          x2={CX}
          y2="388"
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg1-teal)"
          vectorEffect="non-scaling-stroke"
        />
        {/* quiet distribution rail and the two side drops */}
        <line
          x1="180"
          y1="360"
          x2="820"
          y2="360"
          stroke={C.faint}
          strokeWidth="1.25"
          vectorEffect="non-scaling-stroke"
        />
        <circle cx={CX} cy="360" r="2.5" fill={C.accent} />
        <line
          x1="180"
          y1="360"
          x2="180"
          y2="388"
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg1-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1="820"
          y1="360"
          x2="820"
          y2="388"
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg1-ink)"
          vectorEffect="non-scaling-stroke"
        />

        {/* LEFT: request / reply */}
        <PatternFrame
          x={LEFT_X}
          kicker="REQUEST / REPLY"
          caption="one handler, synchronous reply"
        />
        <HandlerRow
          x={LEFT_X + 16}
          y={CARD_Y + 50}
          name="GetQuote"
          service="pricing-svc"
        />
        <text
          x={LEFT_X + 18}
          y={CARD_Y + 94}
          fontSize={FS.label}
          fill={C.faint}
        >
          replies to the caller
        </text>

        {/* CENTER: publish / subscribe, the primary path, two subscribers */}
        <PatternFrame
          x={MID_X}
          kicker="PUBLISH / SUBSCRIBE"
          caption="fan-out to every subscriber"
        />
        <HandlerRow
          x={MID_X + 16}
          y={CARD_Y + 50}
          name="UpdateInventory"
          service="inventory-svc"
          primary
        />
        <HandlerRow
          x={MID_X + 16}
          y={CARD_Y + 82}
          name="NotifyCustomer"
          service="notify-svc"
          batch
        />

        {/* RIGHT: send, a one-way command */}
        <PatternFrame x={RIGHT_X} kicker="SEND" caption="one-way command" />
        <HandlerRow
          x={RIGHT_X + 16}
          y={CARD_Y + 50}
          name="ShipOrder"
          service="shipping-svc"
        />
        <text
          x={RIGHT_X + 18}
          y={CARD_Y + 94}
          fontSize={FS.label}
          fill={C.faint}
        >
          no reply expected
        </text>

        {/* SAGA: a stateful coordinator across the order lifecycle */}
        <rect
          x="40"
          y="540"
          width="920"
          height="192"
          rx="18"
          fill={C.surface}
          fillOpacity="0.4"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="62"
          y="572"
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text x="62" y="588" fontSize={FS.label} fill={C.navLabel}>
          stateful coordinator across the order lifecycle
        </text>
        <rect
          x="824"
          y="556"
          width="112"
          height="22"
          rx="11"
          fill="none"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="880"
          y="571"
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
              y1="643"
              x2={tr.x2}
              y2="643"
              stroke={C.faint}
              strokeWidth="1.25"
              markerEnd="url(#mg1-ink)"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(tr.x1 + tr.x2) / 2}
              y="636"
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
                  y="618"
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
                y="624"
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
                y="648"
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

        {/* Compensation path: a payment timeout cancels and compensates */}
        <line
          x1="385"
          y1="662"
          x2="385"
          y2="690"
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg1-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x="397" y="680" fontSize={FS.label} fill={C.coral}>
          payment timeout
        </text>
        <rect
          x="310"
          y="692"
          width="150"
          height="32"
          rx="10"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="385"
          y="712"
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
        >
          Cancelled
        </text>
        <text
          x="476"
          y="712"
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

interface SpineNodeProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly label: string;
  readonly name: string;
}

/** A centered spine node (gateway, subgraph): a type label over a name. */
function SpineNode({ x, y, w, label, name }: SpineNodeProps) {
  const cx = x + w / 2;
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height="58"
        rx="14"
        fill={C.surface}
        fillOpacity="0.6"
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={cx}
        y={y + 23}
        textAnchor="middle"
        fontSize={FS.label}
        letterSpacing="1.6"
        fill={C.navLabel}
      >
        {label}
      </text>
      <text
        x={cx}
        y={y + 42}
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

interface PatternFrameProps {
  readonly x: number;
  readonly kicker: string;
  readonly caption: string;
}

/** The shared frame for one pattern card: a kicker over a quiet caption. */
function PatternFrame({ x, kicker, caption }: PatternFrameProps) {
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

/** One handler, on its own service, inside a pattern card. */
function HandlerRow({ x, y, name, service, primary, batch }: HandlerRowProps) {
  const w = 248;
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
            x={x + 112}
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
            x={x + 131}
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
  { id: "mg1-teal", fill: "#5eead4" },
  { id: "mg1-green", fill: "#34d399" },
  { id: "mg1-coral", fill: "#f0786a" },
  { id: "mg1-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

interface Transport {
  readonly name: string;
  readonly cx: number;
}

/** The four interchangeable transports, laid out as one tidy inline group. */
const TRANSPORTS: readonly Transport[] = [
  { name: "RabbitMQ", cx: 260 },
  { name: "Apache Kafka", cx: 420 },
  { name: "Azure Service Bus", cx: 580 },
  { name: "Amazon SQS", cx: 740 },
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
