import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Combined Mocha messaging section. Merges three landing takes into one compact
 * section: one shared header, then three tight facet cards. Each card keeps the
 * original take's sub-headline and a shrunk, adapted version of its illustration:
 *
 *  1. "Every app runs on events." A small horizon scene: the thin API surface
 *     above a 1px line, the published event fanning to a couple of handlers below.
 *  2. "Your app is mostly side effects." A small fan-out: one OrderPlaced event
 *     to three reactions, one copy in flight (amber).
 *  3. "Simple, and it scales." A trimmed GetUserById query plus DataLoader
 *     handler, with one compact batching cue.
 *
 * Server component. Static SVGs only. Every svg id is prefixed "cmocha-" so the
 * inlined diagrams cannot collide with other art on the page.
 */
export function CombinedMocha() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* one shared header for all three facets */}
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Messaging
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Messaging, mediator, and sagas.
          </h2>
          <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
            Mocha is the messaging side of the platform: an in-process mediator,
            a bus across services, sagas for long-running work, and exactly-once
            delivery.
          </p>
          <Link
            href="/platform/workflows"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* three facets, compact, side by side on large screens */}
        <div className="mt-10 grid grid-cols-1 gap-4 sm:mt-12 lg:grid-cols-3">
          {/* facet 1: the events substrate under the surface */}
          <FacetCard
            kicker="Surface and events"
            title="Every app runs on events."
            line="What users see is the thin surface. The events underneath are the larger part."
          >
            <HorizonMini />
          </FacetCard>

          {/* facet 2: one event, the reactions that follow */}
          <FacetCard
            kicker="From one order"
            title="Your app is mostly side effects."
            line="One order is placed; stock, payment, and confirmation react on their own."
          >
            <FanOutMini />
          </FacetCard>

          {/* facet 3: the read pattern, and how it batches */}
          <FacetCard
            kicker="One read"
            title="Simple, and it scales."
            line="Every read is the same small shape, and the DataLoader batches it."
          >
            <ReadPatternMini />
          </FacetCard>
        </div>
      </RevealOnScroll>
    </section>
  );
}

interface FacetCardProps {
  readonly kicker: string;
  readonly title: string;
  readonly line: string;
  readonly children: React.ReactNode;
}

/** One compact facet: a mono kicker, the take's sub-headline, a framed
 * illustration, and one short line pinned to the bottom of the card. */
function FacetCard({ kicker, title, line, children }: FacetCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-5 backdrop-blur-sm transition-colors">
      <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
        {kicker}
      </p>
      <h3 className="font-heading text-cc-heading text-h6 mt-1.5 leading-snug font-semibold text-balance">
        {title}
      </h3>
      <div className="border-cc-card-border bg-cc-surface/60 mt-4 rounded-xl border p-3">
        {children}
      </div>
      <p className="text-cc-ink-dim mt-auto pt-4 text-sm text-pretty">{line}</p>
    </div>
  );
}

/**
 * Facet 1, shrunk. A 1px horizon splits the thin API surface (a request and its
 * returned response) from the messaging substrate below, where the same action
 * is published as an event that fans to two handlers.
 */
function HorizonMini() {
  return (
    <svg
      viewBox="0 0 300 184"
      width="100%"
      role="img"
      aria-label="A thin API surface with a request and a returned response sits above a horizon line; below it the published OrderPlaced event fans to two handlers."
      className="h-auto w-full"
      style={{ display: "block", fontFamily: MONO }}
    >
      <defs>
        <linearGradient
          id="cmocha-h-flow"
          gradientUnits="userSpaceOnUse"
          x1="116"
          y1="127"
          x2="184"
          y2="116"
        >
          <stop offset="0" stopColor={C.accent} stopOpacity="0.5" />
          <stop offset="1" stopColor={C.accent} />
        </linearGradient>
      </defs>

      {/* request down, response up, over the API node */}
      <text x="126" y="12" textAnchor="end" fontSize="7" fill={C.navLabel}>
        request
      </text>
      <line
        x1="140"
        y1="16"
        x2="140"
        y2="30"
        stroke={C.accent}
        strokeOpacity="0.7"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <path d="M137 25 L140 31 L143 25" fill={C.accent} />
      <text x="174" y="12" fontSize="7" fill={C.healthy}>
        200 OK
      </text>
      <line
        x1="160"
        y1="30"
        x2="160"
        y2="16"
        stroke={C.healthy}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <path d="M157 21 L160 15 L163 21" fill={C.healthy} />

      {/* the API node, small and calm */}
      <rect
        x="106"
        y="32"
        width="88"
        height="28"
        rx="8"
        fill={C.surface}
        stroke={C.cardBorder}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="150"
        y="45"
        textAnchor="middle"
        fontSize="6"
        letterSpacing="1.3"
        fill={C.navLabel}
      >
        API
      </text>
      <text x="150" y="55" textAnchor="middle" fontSize="7.5" fill={C.inkDim}>
        POST /orders
      </text>

      {/* the horizon line and its plain labels */}
      <line
        x1="8"
        y1="78"
        x2="292"
        y2="78"
        stroke="rgba(245,241,234,0.28)"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      <text x="8" y="73" fontSize="6.5" letterSpacing="1.2" fill={C.navLabel}>
        API SURFACE
      </text>
      <text x="292" y="73" textAnchor="end" fontSize="6.5" fill={C.faint}>
        what users see
      </text>
      <text x="8" y="90" fontSize="6.5" letterSpacing="1.2" fill={C.navLabel}>
        EVENTS
      </text>
      <text x="292" y="90" textAnchor="end" fontSize="6.5" fill={C.faint}>
        what the app runs on
      </text>

      {/* the API publishes the action as an event, crossing the line downward */}
      <path
        d="M150 60 C 150 92 64 92 64 104"
        fill="none"
        stroke={C.accent}
        strokeOpacity="0.4"
        strokeWidth="1"
        strokeDasharray="4 4"
        vectorEffect="non-scaling-stroke"
      />

      {/* the published event node */}
      <rect
        x="12"
        y="104"
        width="104"
        height="46"
        rx="12"
        fill={C.surface}
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text x="26" y="120" fontSize="6.5" letterSpacing="1.3" fill={C.navLabel}>
        EVENT
      </text>
      <text x="26" y="137" fontSize="12" fontWeight="600" fill={C.accent}>
        OrderPlaced
      </text>

      {/* the two handlers the event fans to */}
      {[
        { name: "ChargePayment", cy: 116, traced: true },
        { name: "UpdateInventory", cy: 156, traced: false },
      ].map((h) => (
        <g key={h.name}>
          <path
            d={`M116 127 C 150 127 150 ${h.cy} 184 ${h.cy}`}
            fill="none"
            stroke={h.traced ? "url(#cmocha-h-flow)" : C.accent}
            strokeOpacity={h.traced ? 1 : 0.3}
            strokeWidth={h.traced ? 1.75 : 1}
            strokeDasharray={h.traced ? undefined : "4 4"}
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
          <rect
            x="184"
            y={h.cy - 15}
            width="104"
            height="30"
            rx="8"
            fill={C.surface}
            fillOpacity="0.55"
            stroke={h.traced ? C.accent : C.cardBorder}
            strokeOpacity={h.traced ? 0.5 : 1}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="196"
            y={h.cy - 2}
            fontSize="6"
            letterSpacing="1.1"
            fill={C.navLabel}
          >
            HANDLER
          </text>
          <text x="196" y={h.cy + 9} fontSize="8.5" fill={C.inkDim}>
            {h.name}
          </text>
          <circle cx="184" cy={h.cy} r="2" fill={C.accent} />
        </g>
      ))}
      <circle cx="116" cy="127" r="2.5" fill={C.accent} />
    </svg>
  );
}

/**
 * Facet 2, shrunk. One OrderPlaced event on the left, three reactions on the
 * right. The middle hop carries one in-flight copy of the event (amber); the
 * others are idle, waiting for their copy.
 */
function FanOutMini() {
  const reactions = [
    { name: "reduce stock", cy: 46, kind: "idle" as const },
    { name: "charge payment", cy: 92, kind: "flight" as const },
    { name: "send confirmation", cy: 138, kind: "idle" as const },
  ];

  return (
    <svg
      viewBox="0 0 300 184"
      width="100%"
      role="img"
      aria-label="One OrderPlaced event fans out to three reactions: reduce stock, charge payment, and send confirmation, with one copy of the event in flight."
      className="h-auto w-full"
      style={{ display: "block", fontFamily: MONO }}
    >
      <defs>
        <linearGradient
          id="cmocha-f-flow"
          gradientUnits="userSpaceOnUse"
          x1="112"
          y1="92"
          x2="176"
          y2="92"
        >
          <stop offset="0" stopColor={C.amber} />
          <stop offset="1" stopColor={C.accent} />
        </linearGradient>
      </defs>

      {/* the one event that happened */}
      <rect
        x="12"
        y="70"
        width="100"
        height="44"
        rx="12"
        fill={C.surface}
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text x="26" y="86" fontSize="6.5" letterSpacing="1.3" fill={C.navLabel}>
        EVENT
      </text>
      <text x="26" y="103" fontSize="11.5" fontWeight="600" fill={C.accent}>
        OrderPlaced
      </text>

      {/* the three reactions and the hops that reach them */}
      {reactions.map((r) => (
        <g key={r.name}>
          <path
            d={`M112 92 C 150 92 150 ${r.cy} 176 ${r.cy}`}
            fill="none"
            stroke={r.kind === "flight" ? "url(#cmocha-f-flow)" : C.accent}
            strokeOpacity={r.kind === "flight" ? 0.95 : 0.3}
            strokeWidth={r.kind === "flight" ? 1.75 : 1}
            strokeDasharray={r.kind === "flight" ? undefined : "4 5"}
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
          <rect
            x="176"
            y={r.cy - 16}
            width="112"
            height="32"
            rx="8"
            fill={C.surface}
            fillOpacity="0.6"
            stroke={r.kind === "flight" ? C.amber : C.cardBorder}
            strokeOpacity={r.kind === "flight" ? 0.55 : 1}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          {r.kind === "flight" && (
            <text
              x="190"
              y={r.cy - 4}
              fontSize="5.5"
              letterSpacing="1.2"
              fill={C.amber}
            >
              IN FLIGHT
            </text>
          )}
          <text
            x="190"
            y={r.cy + (r.kind === "flight" ? 9 : 3)}
            fontSize="9"
            fontWeight="500"
            fill={C.heading}
          >
            {r.name}
          </text>
          <circle
            cx="176"
            cy={r.cy}
            r="2"
            fill={r.kind === "flight" ? C.amber : C.accent}
          />
        </g>
      ))}

      {/* one in-flight copy of the event riding the active hop */}
      <rect
        x="116"
        y="83"
        width="56"
        height="18"
        rx="9"
        fill={C.surface}
        stroke={C.amber}
        strokeOpacity="0.9"
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text
        x="144"
        y="95"
        textAnchor="middle"
        fontSize="7"
        fontWeight="500"
        fill={C.amber}
      >
        OrderPlaced
      </text>
      <circle cx="112" cy="92" r="2.5" fill={C.accent} />
    </svg>
  );
}

/**
 * Facet 3, shrunk. The whole read pattern in a few lines: a resolver that
 * dispatches a query and a handler that loads through a DataLoader, with one
 * compact cue showing reads in a single tick collapsing into one batched query.
 */
function ReadPatternMini() {
  return (
    <div>
      <div className="overflow-x-auto">
        <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          resolver
        </p>
        <CodeBlock lines={RESOLVER} />
        <div className="border-cc-card-border my-2.5 border-t border-dashed" />
        <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          handler
        </p>
        <CodeBlock lines={HANDLER} />
      </div>

      {/* the one scaling cue: reads in one tick batch into one query */}
      <div className="border-cc-card-border mt-4 flex items-center gap-3 border-t pt-4">
        <div className="space-y-1">
          {["1", "2", "3"].map((id) => (
            <div
              key={id}
              className="border-cc-card-border bg-cc-surface text-cc-ink-dim rounded-md border px-2 py-1 font-mono text-[0.58rem]"
            >
              GetUserById(<span className="text-cc-accent">{id}</span>)
            </div>
          ))}
        </div>
        <span
          aria-hidden="true"
          className="text-cc-ink-faint font-mono text-sm"
        >
          &rarr;
        </span>
        <div className="border-cc-accent/40 bg-cc-accent/5 rounded-lg border px-3 py-2">
          <p className="text-cc-nav-label font-mono text-[0.5rem] tracking-[0.12em] uppercase">
            one query
          </p>
          <p className="text-cc-accent mt-0.5 font-mono text-[0.72rem]">
            id IN (1,2,3)
          </p>
        </div>
      </div>
    </div>
  );
}

interface CodeBlockProps {
  readonly lines: readonly CodeLine[];
}

/** One block of syntax-tinted code lines, sized for the compact card. */
function CodeBlock({ lines }: CodeBlockProps) {
  return (
    <pre className="mt-1 font-mono text-[0.62rem] leading-[1.7] sm:text-[0.66rem]">
      <code>
        {lines.map((line) => (
          <div key={line.map((token) => token[0]).join("")}>
            {line.map((token, index) => (
              <span
                key={`${token[0]}-${index}`}
                style={{ color: SYN[token[1]] }}
              >
                {token[0]}
              </span>
            ))}
          </div>
        ))}
      </code>
    </pre>
  );
}

type TokenKind = keyof typeof SYN;
type Token = readonly [text: string, kind: TokenKind];
type CodeLine = readonly Token[];

// The resolver: a query field that dispatches an IQuery through the mediator.
const RESOLVER: readonly CodeLine[] = [
  [
    ["GetUser", "mem"],
    ["(", "txt"],
    ["int ", "kw"],
    ["id", "txt"],
    [", ", "txt"],
    ["IMediator ", "type"],
    ["mediator", "txt"],
    [")", "txt"],
  ],
  [
    ["  => ", "txt"],
    ["mediator", "txt"],
    [".", "txt"],
    ["QueryAsync", "mem"],
    ["(", "txt"],
    ["new ", "kw"],
    ["GetUserById", "type"],
    ["(", "txt"],
    ["id", "txt"],
    ["));", "txt"],
  ],
];

// The handler: loads through the generated DataLoader, which batches the reads.
const HANDLER: readonly CodeLine[] = [
  [
    ["Handle", "mem"],
    ["(", "txt"],
    ["GetUserById ", "type"],
    ["q", "txt"],
    [", ", "txt"],
    ["IUserByIdDataLoader ", "type"],
    ["users", "txt"],
    [")", "txt"],
  ],
  [
    ["  => ", "txt"],
    ["users", "txt"],
    [".", "txt"],
    ["LoadAsync", "mem"],
    ["(", "txt"],
    ["q", "txt"],
    [".", "txt"],
    ["Id", "mem"],
    [");", "txt"],
  ],
];

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Restrained GitHub-dark syntax tokens that read on the navy code surface. */
const SYN = {
  kw: "#f97583",
  type: "#b392f0",
  mem: "#79b8ff",
  txt: "#c9d4e3",
} as const;

/** Locked cc-* palette for the inline diagrams: navy surfaces, neutral ink, teal
 * accent, amber for the one in-flight copy, green for the returned response. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  healthy: "#34d399",
} as const;
