/**
 * Mocha messaging flow, version 3: "Numbered stacked bands".
 *
 * The flow is read top to bottom as five full-width horizontal bands, each
 * marked by a numbered kicker on the left and a quiet caption on the right, and
 * separated by generous whitespace rather than boxes:
 *   01 API SURFACE  the Fusion gateway takes a request and returns 200 OK; a
 *                   horizon line divides this public surface from the services.
 *   02 PUBLISH      the gateway resolves against the orders-svc subgraph, which
 *                   after responding publishes the OrderPlaced event downward.
 *   03 TRANSPORT    one pluggable bus (RabbitMQ, Apache Kafka, Azure Service
 *                   Bus, Amazon SQS), the event entering at the left.
 *   04 PATTERNS     three equal pattern cards, each on its own service, reached
 *                   by straight orthogonal drops off the bus: publish/subscribe
 *                   (two subscribers, one batch), request/reply, and send.
 *   05 SAGA         a full-width OrderFulfillment coordinator holding state from
 *                   Placed to Shipped, with a timeout compensation to Cancelled.
 *
 * Unlike the centered spine of v1, the primary flow runs down a left-of-centre
 * rail and the transport delivers a full-width bus to evenly spaced cards.
 * Fully static, no motion. Three font sizes only. Every svg id is "mg3-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// Content spans this gutter; the primary flow rail sits left of centre.
const X0 = 40;
const X1 = 960;
const RAIL = 184;

// The three pattern cards sit on one row and share these dimensions.
const CARD_Y = 444;
const CARD_H = 126;
const CARD_W = 288;
const CARD_X = [40, 356, 672] as const;

export function MessagingGraphicV3() {
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
        viewBox="0 0 1000 808"
        width="100%"
        role="img"
        aria-label="Five stacked full-width bands read top to bottom. Band one, the API surface: the Fusion gateway takes a request and returns 200 OK, with a horizon line separating the public GraphQL API from the services below. Band two, publish: the gateway resolves against the orders-svc subgraph, which after responding publishes the OrderPlaced event downward. Band three, transport: one pluggable bus that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS, with the event entering at the left. Band four, patterns: three equal cards, each on its own service, reached by straight drops off the bus, showing publish and subscribe with two subscribers (one batch), request and reply, and send. Band five: a full-width OrderFulfillment saga holding state across Placed, AwaitingPayment, Paid, and Shipped, with a payment timeout compensation path to Cancelled."
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

        {/* BAND 01: the public API surface */}
        <BandKicker
          index="01"
          name="API SURFACE"
          caption="the public GraphQL API"
          y={20}
        />
        <text
          x={150}
          y={46}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          REQUEST
        </text>
        <line
          x1={150}
          y1={52}
          x2={150}
          y2={72}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg3-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={250}
          y={46}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1={250}
          y1={72}
          x2={250}
          y2={52}
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg3-green)"
          vectorEffect="non-scaling-stroke"
        />
        <NodeCard x={X0} y={74} w={380} label="GATEWAY" name="Fusion" />
        <text x={X1} y={98} textAnchor="end" fontSize={FS.label} fill={C.faint}>
          request and 200 OK
        </text>
        <text
          x={X1}
          y={114}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          stay on the public surface
        </text>

        {/* The horizon dividing the public surface from the services */}
        <text
          x={X1}
          y={144}
          textAnchor="end"
          fontSize={FS.label}
          letterSpacing="1"
          fill={C.navLabel}
        >
          public surface
        </text>
        <line
          x1={X0}
          y1={150}
          x2={X1}
          y2={150}
          stroke="rgba(245, 241, 234, 0.2)"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        {/* The gateway resolves down across the horizon into the subgraph */}
        <line
          x1={RAIL}
          y1={128}
          x2={RAIL}
          y2={196}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg3-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={RAIL + 12}
          y={148}
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          RESOLVES
        </text>

        {/* BAND 02: the orders-svc subgraph publishes OrderPlaced */}
        <BandKicker
          index="02"
          name="PUBLISH"
          caption="internal services, not exposed"
          y={180}
        />
        <NodeCard x={X0} y={196} w={380} label="SUBGRAPH" name="orders-svc" />
        <text
          x={X1}
          y={220}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          fire and forget,
        </text>
        <text
          x={X1}
          y={236}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          not in the response path
        </text>
        <circle cx={RAIL} cy={250} r="2.5" fill={C.accent} />
        <line
          x1={RAIL}
          y1={250}
          x2={RAIL}
          y2={312}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg3-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={RAIL + 12}
          y={276}
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.accent}
        >
          publishes OrderPlaced
        </text>
        <text x={RAIL + 12} y={290} fontSize={FS.label} fill={C.faint}>
          after 200 OK
        </text>

        {/* BAND 03: the single, pluggable transport bus */}
        <BandKicker
          index="03"
          name="TRANSPORT"
          caption="one bus, four brokers"
          y={300}
        />
        <rect
          x={X0}
          y={312}
          width={X1 - X0}
          height="64"
          rx="14"
          fill={C.surface}
          fillOpacity="0.6"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={500}
          y={332}
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
            y1={344}
            x2={dx}
            y2={368}
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
        ))}
        {TRANSPORTS.map((t) => (
          <text
            key={t.name}
            x={t.cx}
            y={362}
            textAnchor="middle"
            fontSize={FS.name}
            fill={C.inkDim}
          >
            {t.name}
          </text>
        ))}
        <circle cx={RAIL} cy={312} r="2.5" fill={C.accent} />

        {/* BAND 04: three patterns, each on its own service */}
        <BandKicker
          index="04"
          name="PATTERNS"
          caption="each handler on its own service"
          y={404}
        />
        {/* The service boundary the bus delivers across */}
        <line
          x1={X0}
          y1={420}
          x2={X1}
          y2={420}
          stroke={C.cardBorder}
          strokeWidth="1"
          strokeDasharray="4 4"
          vectorEffect="non-scaling-stroke"
        />
        {/* Straight orthogonal drops off the bus to each card centre */}
        <line
          x1={RAIL}
          y1={376}
          x2={RAIL}
          y2={442}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg3-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={500}
          y1={376}
          x2={500}
          y2={442}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg3-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={816}
          y1={376}
          x2={816}
          y2={442}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg3-ink)"
          vectorEffect="non-scaling-stroke"
        />

        {/* PUBLISH / SUBSCRIBE: the primary path, two subscribers */}
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

        {/* REQUEST / REPLY: one handler, synchronous reply */}
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

        {/* SEND: a one-way command */}
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

        {/* BAND 05: the stateful OrderFulfillment saga */}
        <BandKicker
          index="05"
          name="SAGA"
          caption="stateful, across the lifecycle"
          y={600}
        />
        <rect
          x={X0}
          y={614}
          width={X1 - X0}
          height="176"
          rx="18"
          fill={C.surface}
          fillOpacity="0.4"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={62}
          y={644}
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text x={62} y={660} fontSize={FS.label} fill={C.navLabel}>
          stateful coordinator across the order lifecycle
        </text>
        <rect
          x={824}
          y={628}
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
          y={643}
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
              y1={709}
              x2={tr.x2}
              y2={709}
              stroke={C.faint}
              strokeWidth="1.25"
              markerEnd="url(#mg3-ink)"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(tr.x1 + tr.x2) / 2}
              y={702}
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
                  y={684}
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
                y={690}
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
                y={713}
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
          y1={728}
          x2={385}
          y2={752}
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg3-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <text x={397} y={745} fontSize={FS.label} fill={C.coral}>
          payment timeout
        </text>
        <rect
          x={310}
          y={752}
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
          y={771}
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
        >
          Cancelled
        </text>
        <text
          x={476}
          y={771}
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

interface BandKickerProps {
  readonly index: string;
  readonly name: string;
  readonly caption: string;
  readonly y: number;
}

/** A numbered band header: an index plus name on the left, caption on the right. */
function BandKicker({ index, name, caption, y }: BandKickerProps) {
  return (
    <g>
      <text x={X0} y={y} fontSize={FS.label} letterSpacing="1.4">
        <tspan fill={C.inkDim}>{index}</tspan>
        <tspan dx="10" fill={C.navLabel}>
          {name}
        </tspan>
      </text>
      <text x={X1} y={y} textAnchor="end" fontSize={FS.label} fill={C.faint}>
        {caption}
      </text>
    </g>
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

/** The shared frame for one pattern card: a kicker over a quiet caption. */
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

/** One handler, on its own service, inside a pattern card. */
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
  { id: "mg3-teal", fill: "#5eead4" },
  { id: "mg3-green", fill: "#34d399" },
  { id: "mg3-coral", fill: "#f0786a" },
  { id: "mg3-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

interface Transport {
  readonly name: string;
  readonly cx: number;
}

/** The four interchangeable transports, laid out as one tidy inline group. */
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
