/**
 * Mocha messaging flow, version 7: "Layered stack".
 *
 * A cross-section of the system as a stack of full-width horizontal layers,
 * read top to bottom. A thin public-surface strip at the top holds the Fusion
 * gateway (a request comes in, 200 OK goes back out). Below a synchronous /
 * asynchronous divider the layers are: Your app (the orders-svc subgraph
 * responds, then raises the OrderPlaced event), Mocha (the mediator, bus and
 * sagas, with publish/subscribe, request/reply and send as tidy chips),
 * Pluggable transport (RabbitMQ, Apache Kafka, Azure Service Bus, Amazon SQS as
 * one inline group) and Other services (UpdateInventory, NotifyCustomer with a
 * batch tag, GetQuote and ShipOrder, each on its own service). A single teal
 * event path threads down from the orders-svc node, through the publish/subscribe
 * chip, onto the transport, and is delivered across the service boundary into
 * the subscriber cards. A slim OrderFulfillment saga band coordinates the
 * lifecycle, with Paid as the current state and a timeout compensation to
 * Cancelled. Unlike the node-and-arrow versions, this reads as a layered
 * architecture sandwich. Fully static, no motion. Three font sizes only. Every
 * svg id is prefixed "mg7-".
 */

// One small type scale for the whole diagram, so labels stay consistent.
const FS = { label: 8, name: 11, title: 13 } as const;

// The diagram is a fixed grid; every layer spans the same full width.
const BX = 40;
const BR = 960;
const BW = 920;
const CX = 500;
const R = 12;

// The stacked layers, top to bottom, each a full-width band.
const GW = { y: 36, h: 52 } as const;
const AP = { y: 118, h: 84 } as const;
const MO = { y: 232, h: 140 } as const;
const TR = { y: 402, h: 60 } as const;
const OS = { y: 500, h: 148 } as const;
const SA = { y: 678, h: 128 } as const;

// The orders-svc node sits centred in the Your app layer; the teal event path
// drops from its base down the centre of the stack.
const NODE = { x: CX - 110, y: AP.y + 18, w: 220, h: 48 } as const;

// The publish/subscribe chip is the centre of the three Mocha pattern chips and
// the point the event is published from.
const CHIP_Y = MO.y + 58;
const CHIP_H = 52;

// The four subscriber/handler cards, equal columns in the Other services layer.
const CARD_W = 212;
const CARD_Y = OS.y + 36;
const CARD_H = 96;

// The saga lifecycle row.
const STATE_Y = SA.y + 54;
const STATE_H = 36;

export function MessagingGraphicV7() {
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
        viewBox="0 0 1000 814"
        width="100%"
        role="img"
        aria-label="A layered architecture stack read top to bottom. A thin public surface strip holds the Fusion gateway: a request comes in and 200 OK goes back out. A synchronous and asynchronous divider separates the fast response above from the events below. The layers are: Your app, where the orders-svc subgraph responds and then raises the OrderPlaced event; Mocha, the mediator, message bus and sagas, which owns publish and subscribe, request and reply, and send; a pluggable transport that can be RabbitMQ, Apache Kafka, Azure Service Bus, or Amazon SQS; and Other services, where UpdateInventory and NotifyCustomer subscribe as one batch, GetQuote replies and ShipOrder is sent, each on its own service. A teal event path threads down from the orders-svc node through the publish and subscribe chip, onto the transport, and across a service boundary into the subscriber cards. A slim OrderFulfillment saga coordinates the lifecycle across Placed, AwaitingPayment, Paid and Shipped, with a payment timeout compensation path to Cancelled."
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

        {/* PUBLIC SURFACE: request in, 200 OK out, around the Fusion gateway */}
        <text
          x={CX - 72}
          y={22}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          REQUEST
        </text>
        <line
          x1={CX - 72}
          y1={28}
          x2={CX - 72}
          y2={GW.y}
          stroke={C.faint}
          strokeWidth="1.25"
          markerEnd="url(#mg7-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX + 72}
          y={22}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.healthy}
        >
          200 OK
        </text>
        <line
          x1={CX + 72}
          y1={GW.y}
          x2={CX + 72}
          y2={28}
          stroke={C.healthy}
          strokeWidth="1.25"
          markerEnd="url(#mg7-green)"
          vectorEffect="non-scaling-stroke"
        />

        <Layer y={GW.y} h={GW.h} fillOpacity={0.6} />
        <text
          x={BX + 22}
          y={GW.y + 22}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          PUBLIC SURFACE
        </text>
        <text
          x={BX + 22}
          y={GW.y + 40}
          fontSize={FS.name}
          fontWeight="600"
          fill={C.heading}
        >
          Fusion gateway
        </text>
        <text
          x={BR - 22}
          y={GW.y + 32}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          the public GraphQL API
        </text>

        {/* Gateway resolves down into the subgraph node */}
        <line
          x1={CX}
          y1={GW.y + GW.h}
          x2={CX}
          y2={NODE.y}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg7-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX + 12}
          y={GW.y + GW.h + 18}
          fontSize={FS.label}
          letterSpacing="1.2"
          fill={C.navLabel}
        >
          RESOLVES
        </text>

        {/* LAYER 1: YOUR APP, the orders-svc subgraph */}
        <Layer y={AP.y} h={AP.h} fillOpacity={0.5} />
        <text
          x={BX + 22}
          y={AP.y + 26}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          YOUR APP
        </text>
        <text x={BX + 22} y={AP.y + 42} fontSize={FS.label} fill={C.faint}>
          your service code
        </text>
        <rect
          x={NODE.x}
          y={NODE.y}
          width={NODE.w}
          height={NODE.h}
          rx={10}
          fill={C.surface}
          fillOpacity="0.7"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX}
          y={NODE.y + 20}
          textAnchor="middle"
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          SUBGRAPH
        </text>
        <text
          x={CX}
          y={NODE.y + 38}
          textAnchor="middle"
          fontSize={FS.name}
          fontWeight="600"
          fill={C.heading}
        >
          orders-svc
        </text>
        <text
          x={BR - 22}
          y={AP.y + 47}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          responds first, synchronously
        </text>

        {/* The synchronous / asynchronous divider, and the published event */}
        <line
          x1={BX}
          y1={216}
          x2={BR}
          y2={216}
          stroke="rgba(245, 241, 234, 0.16)"
          strokeWidth="1"
          strokeDasharray="4 5"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={BX}
          y={211}
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          ASYNC, AFTER THE 200 OK
        </text>
        <circle cx={CX} cy={NODE.y + NODE.h} r="2.5" fill={C.accent} />
        <line
          x1={CX}
          y1={NODE.y + NODE.h}
          x2={CX}
          y2={CHIP_Y}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg7-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX + 14}
          y={205}
          fontSize={FS.label}
          letterSpacing="0.4"
          fill={C.accent}
        >
          raises OrderPlaced
        </text>

        {/* LAYER 2: MOCHA, the mediator, bus and sagas */}
        <Layer y={MO.y} h={MO.h} fillOpacity={0.6} />
        <text
          x={BX + 22}
          y={MO.y + 28}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          MOCHA
        </text>
        <text x={BX + 22} y={MO.y + 44} fontSize={FS.label} fill={C.faint}>
          mediator, message bus, sagas
        </text>
        <text
          x={BR - 22}
          y={MO.y + 28}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          owns the messaging patterns
        </text>
        {PATTERNS.map((p) => (
          <PatternChip
            key={p.name}
            cx={p.cx}
            name={p.name}
            sub={p.sub}
            primary={p.primary}
          />
        ))}

        {/* The publish/subscribe chip puts the event onto the transport */}
        <line
          x1={CX}
          y1={CHIP_Y + CHIP_H}
          x2={CX}
          y2={TR.y}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg7-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={CX + 14}
          y={MO.y + MO.h + 19}
          fontSize={FS.label}
          letterSpacing="0.4"
          fill={C.accent}
        >
          onto the transport
        </text>

        {/* LAYER 3: PLUGGABLE TRANSPORT, four interchangeable options inline */}
        <Layer y={TR.y} h={TR.h} fillOpacity={0.5} />
        <text
          x={BX + 22}
          y={TR.y + 20}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          PLUGGABLE TRANSPORT
        </text>
        <text
          x={BR - 22}
          y={TR.y + 20}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          same code, swap the broker
        </text>
        {TRANSPORT_DIVS.map((dx) => (
          <line
            key={dx}
            x1={dx}
            y1={TR.y + 32}
            x2={dx}
            y2={TR.y + 50}
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
        ))}
        {TRANSPORTS.map((t) => (
          <text
            key={t.name}
            x={t.cx}
            y={TR.y + 46}
            textAnchor="middle"
            fontSize={FS.name}
            fill={C.inkDim}
          >
            {t.name}
          </text>
        ))}

        {/* DELIVERY across the service boundary into the handler services */}
        <line
          x1={BX}
          y1={481}
          x2={BR}
          y2={481}
          stroke="rgba(245, 241, 234, 0.16)"
          strokeWidth="1"
          strokeDasharray="4 5"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={BR}
          y={476}
          textAnchor="end"
          fontSize={FS.label}
          letterSpacing="1.4"
          fill={C.navLabel}
        >
          SERVICE BOUNDARY
        </text>
        {/* teal: the published event is delivered to the two subscribers */}
        <line
          x1={CX}
          y1={TR.y + TR.h}
          x2={CX}
          y2={472}
          stroke={C.accent}
          strokeWidth="1.5"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={158}
          y1={472}
          x2={CX}
          y2={472}
          stroke={C.accent}
          strokeWidth="1.5"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={158}
          y1={472}
          x2={158}
          y2={OS.y}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg7-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={386}
          y1={472}
          x2={386}
          y2={OS.y}
          stroke={C.accent}
          strokeWidth="1.5"
          markerEnd="url(#mg7-teal)"
          vectorEffect="non-scaling-stroke"
        />
        <rect
          x={244}
          y={483}
          width="56"
          height="16"
          rx="8"
          fill={C.surface}
          stroke={C.accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={272}
          y={494}
          textAnchor="middle"
          fontSize={FS.label}
          fill={C.accent}
        >
          1 batch
        </text>
        {/* neutral: request/reply and send reach their own services too */}
        <line
          x1={614}
          y1={TR.y + TR.h}
          x2={614}
          y2={OS.y}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg7-ink)"
          vectorEffect="non-scaling-stroke"
        />
        <line
          x1={842}
          y1={TR.y + TR.h}
          x2={842}
          y2={OS.y}
          stroke={C.inkDim}
          strokeWidth="1.25"
          markerEnd="url(#mg7-ink)"
          vectorEffect="non-scaling-stroke"
        />

        {/* LAYER 4: OTHER SERVICES, each handler on its own service */}
        <Layer y={OS.y} h={OS.h} fillOpacity={0.3} />
        <text
          x={BX + 22}
          y={OS.y + 22}
          fontSize={FS.label}
          letterSpacing="1.6"
          fill={C.navLabel}
        >
          OTHER SERVICES
        </text>
        <text
          x={BR - 22}
          y={OS.y + 22}
          textAnchor="end"
          fontSize={FS.label}
          fill={C.faint}
        >
          each handler on its own service
        </text>
        {SERVICES.map((s) => (
          <ServiceCard
            key={s.name}
            cx={s.cx}
            pattern={s.pattern}
            name={s.name}
            service={s.service}
            note={s.note}
            primary={s.primary}
            batch={s.batch}
          />
        ))}

        {/* SAGA: a slim coordinator band across the order lifecycle */}
        <Layer y={SA.y} h={SA.h} fillOpacity={0.4} />
        <text
          x={BX + 22}
          y={SA.y + 28}
          fontSize={FS.title}
          fontWeight="600"
          fill={C.heading}
        >
          OrderFulfillment saga
        </text>
        <text x={BX + 22} y={SA.y + 44} fontSize={FS.label} fill={C.navLabel}>
          Mocha coordinates the order lifecycle
        </text>
        <rect
          x={BR - 134}
          y={SA.y + 14}
          width="112"
          height="22"
          rx="8"
          fill="none"
          stroke={C.cardBorder}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={BR - 78}
          y={SA.y + 29}
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
              y1={STATE_Y + STATE_H / 2}
              x2={tr.x2}
              y2={STATE_Y + STATE_H / 2}
              stroke={C.faint}
              strokeWidth="1.25"
              markerEnd="url(#mg7-ink)"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x={(tr.x1 + tr.x2) / 2}
              y={STATE_Y + STATE_H / 2 - 7}
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
          x1={380}
          y1={STATE_Y + STATE_H}
          x2={380}
          y2={STATE_Y + STATE_H + 8}
          stroke={C.coral}
          strokeOpacity="0.8"
          strokeWidth="1.25"
          strokeDasharray="3 3"
          markerEnd="url(#mg7-coral)"
          vectorEffect="non-scaling-stroke"
        />
        <rect
          x={305}
          y={STATE_Y + STATE_H + 8}
          width="150"
          height="24"
          rx="8"
          fill="none"
          stroke={C.coral}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeDasharray="3 3"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x={380}
          y={STATE_Y + STATE_H + 24}
          textAnchor="middle"
          fontSize={FS.name}
          fill={C.coral}
        >
          Cancelled
        </text>
        <text
          x={465}
          y={STATE_Y + STATE_H + 24}
          fontSize={FS.label}
          fill={C.coral}
          fillOpacity="0.8"
        >
          payment timeout, runs compensation
        </text>
      </svg>
    </div>
  );
}

interface LayerProps {
  readonly y: number;
  readonly h: number;
  readonly fillOpacity: number;
}

/** One full-width layer slab in the stack. */
function Layer({ y, h, fillOpacity }: LayerProps) {
  return (
    <rect
      x={BX}
      y={y}
      width={BW}
      height={h}
      rx={R}
      fill={C.surface}
      fillOpacity={fillOpacity}
      stroke={C.cardBorder}
      strokeWidth="1"
      vectorEffect="non-scaling-stroke"
    />
  );
}

interface PatternChipProps {
  readonly cx: number;
  readonly name: string;
  readonly sub: string;
  readonly primary?: boolean;
}

/** One messaging primitive Mocha provides, as a chip in the Mocha layer. */
function PatternChip({ cx, name, sub, primary }: PatternChipProps) {
  const w = 236;
  return (
    <g>
      <rect
        x={cx - w / 2}
        y={CHIP_Y}
        width={w}
        height={CHIP_H}
        rx={10}
        fill={C.surface}
        fillOpacity="0.7"
        stroke={primary ? C.accent : C.cardBorder}
        strokeOpacity={primary ? 0.55 : 1}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={cx}
        y={CHIP_Y + 22}
        textAnchor="middle"
        fontSize={FS.name}
        fontWeight={primary ? 600 : 400}
        fill={primary ? C.accent : C.heading}
      >
        {name}
      </text>
      <text
        x={cx}
        y={CHIP_Y + 39}
        textAnchor="middle"
        fontSize={FS.label}
        fill={primary ? C.accent : C.faint}
        fillOpacity={primary ? 0.8 : 1}
      >
        {sub}
      </text>
    </g>
  );
}

interface ServiceCardProps {
  readonly cx: number;
  readonly pattern: string;
  readonly name: string;
  readonly service: string;
  readonly note: string;
  readonly primary?: boolean;
  readonly batch?: boolean;
}

/** One handler on its own service, inside the Other services layer. */
function ServiceCard({
  cx,
  pattern,
  name,
  service,
  note,
  primary,
  batch,
}: ServiceCardProps) {
  const x = cx - CARD_W / 2;
  return (
    <g>
      <rect
        x={x}
        y={CARD_Y}
        width={CARD_W}
        height={CARD_H}
        rx={10}
        fill={C.surface}
        fillOpacity="0.6"
        stroke={primary ? C.accent : C.cardBorder}
        strokeOpacity={primary ? 0.55 : 1}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x={x + 16}
        y={CARD_Y + 22}
        fontSize={FS.label}
        letterSpacing="1.2"
        fill={primary ? C.accent : C.navLabel}
      >
        {pattern}
      </text>
      <text
        x={x + 16}
        y={CARD_Y + 44}
        fontSize={FS.name}
        fontWeight={primary ? 600 : 400}
        fill={primary ? C.accent : C.heading}
      >
        {name}
      </text>
      {batch && (
        <>
          <rect
            x={x + CARD_W - 58}
            y={CARD_Y + 33}
            width="42"
            height="15"
            rx="7"
            fill="none"
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x={x + CARD_W - 37}
            y={CARD_Y + 44}
            textAnchor="middle"
            fontSize={FS.label}
            fill={C.navLabel}
          >
            batch
          </text>
        </>
      )}
      <text x={x + 16} y={CARD_Y + 62} fontSize={FS.label} fill={C.navLabel}>
        {service}
      </text>
      <text x={x + 16} y={CARD_Y + 82} fontSize={FS.label} fill={C.faint}>
        {note}
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
          y={STATE_Y - 8}
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
        y={STATE_Y}
        width="150"
        height={STATE_H}
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
        y={STATE_Y + 23}
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
  { id: "mg7-teal", fill: "#5eead4" },
  { id: "mg7-green", fill: "#34d399" },
  { id: "mg7-coral", fill: "#f0786a" },
  { id: "mg7-ink", fill: "rgba(245, 241, 234, 0.42)" },
];

interface Pattern {
  readonly cx: number;
  readonly name: string;
  readonly sub: string;
  readonly primary?: boolean;
}

/** The three messaging primitives Mocha owns; publish/subscribe is the path. */
const PATTERNS: readonly Pattern[] = [
  { cx: 240, name: "Request / Reply", sub: "one reply" },
  { cx: CX, name: "Publish / Subscribe", sub: "fan-out", primary: true },
  { cx: 760, name: "Send", sub: "one-way" },
];

interface Transport {
  readonly name: string;
  readonly cx: number;
}

/** The four interchangeable transports, laid out as one tidy inline group. */
const TRANSPORTS: readonly Transport[] = [
  { name: "RabbitMQ", cx: 230 },
  { name: "Apache Kafka", cx: 410 },
  { name: "Azure Service Bus", cx: 590 },
  { name: "Amazon SQS", cx: 770 },
];

const TRANSPORT_DIVS: readonly number[] = [320, 500, 680];

interface Service {
  readonly cx: number;
  readonly pattern: string;
  readonly name: string;
  readonly service: string;
  readonly note: string;
  readonly primary?: boolean;
  readonly batch?: boolean;
}

/** The four handler services, each on its own service, tagged by pattern. */
const SERVICES: readonly Service[] = [
  {
    cx: 158,
    pattern: "PUBLISH / SUBSCRIBE",
    name: "UpdateInventory",
    service: "inventory-svc",
    note: "subscribes",
    primary: true,
  },
  {
    cx: 386,
    pattern: "PUBLISH / SUBSCRIBE",
    name: "NotifyCustomer",
    service: "notify-svc",
    note: "subscribes",
    primary: true,
    batch: true,
  },
  {
    cx: 614,
    pattern: "REQUEST / REPLY",
    name: "GetQuote",
    service: "pricing-svc",
    note: "replies to the caller",
  },
  {
    cx: 842,
    pattern: "SEND",
    name: "ShipOrder",
    service: "shipping-svc",
    note: "no reply expected",
  },
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
}

/** The labelled transitions between saga states: events in, a command out. */
const SAGA_TRANSITIONS: readonly SagaTransition[] = [
  { label: "OrderPlaced", x1: 225, x2: 305 },
  { label: "PaymentReceived", x1: 455, x2: 535 },
  { label: "sends ShipOrder", x1: 685, x2: 765 },
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
