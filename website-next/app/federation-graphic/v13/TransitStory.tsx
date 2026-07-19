"use client";

/**
 * The federation walkthrough, in the homepage FusionFlow language. Five
 * service streams hold their own parallel columns while ten short chapters
 * explain the concept in general terms; the canvas carries the concrete
 * example (Catalog, Billing, the Float/Money drift). All five columns bend
 * together through one shared convergence band into the glowing composition
 * node, and a single output line continues below, crossing the build/runtime
 * horizon into the runtime gateway chip. Copy never depends on the example:
 * the diagram illustrates, the text explains. On phones the fixed-aspect
 * canvas would collapse, so the same story renders as a plain stacked column
 * instead. Static; the page scrolls normally.
 */

import type { ReactNode } from "react";

import { CANON, GatewayChip, GlowNode, INK_DIM } from "./visuals/stage";

/* Design space, rendered 1:1 at the section's max width. */
const W = 1024;
const H = 7520;

/* One column per service; markers join their column as the story needs them. */
const MARKERS = [
  { s: 0, x: 150, y: 120 },
  { s: 1, x: 320, y: 1190 },
  { s: 3, x: 704, y: 1950 },
  { s: 4, x: 874, y: 2750 },
  { s: 2, x: 512, y: 3550 },
] as const;

/* Every stream falls straight, then bends through the same band into the hub. */
const BEND_START = 5250;
const HUB = { x: 512, y: 5710 } as const;

/* The build/runtime horizon and the runtime gateway on the output line. */
const HORIZON_Y = 6520;
const CHIP = { x: 512, y: 6600 } as const;

function streamPath(x: number, y0: number): string {
  const c1y = BEND_START + (HUB.y - BEND_START) * 0.5;
  const c2y = HUB.y - (HUB.y - BEND_START) * 0.35;
  return `M${x} ${y0} L${x} ${BEND_START} C ${x} ${c1y}, ${HUB.x} ${c2y}, ${HUB.x} ${HUB.y}`;
}

/**
 * Bands where streams recede behind copy. The mask dims them to a low
 * opacity (never fully out) so every line stays continuous; the copy scrim
 * on top does the rest of the quieting behind the actual words. Cards need
 * no bands: their solid background hides the line, which is the point (the
 * card hangs on its line).
 */
const GAPS = [
  { x: 220, w: 584, y: 2200, h: 300 },
  { x: 470, w: 460, y: 2980, h: 340 },
  { x: 100, w: 460, y: 3800, h: 300 },
  { x: 470, w: 460, y: 4400, h: 300 },
  { x: 220, w: 584, y: 4900, h: 300 },
  { x: 220, w: 584, y: 5790, h: 360 },
  { x: 220, w: 584, y: 6670, h: 300 },
  { x: 220, w: 584, y: 7130, h: 300 },
] as const;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

/* ── Copy blocks ─────────────────────────────────────────────────────── */

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface CopyContentProps {
  readonly title: string;
  readonly children: ReactNode;
  /** Side-column copy is left-aligned; the centered spine copy is centered. */
  readonly side?: boolean;
}

function CopyContent({ title, children, side }: CopyContentProps) {
  return (
    <div className={`relative ${side ? "text-left" : "text-center"}`}>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
        {title}
      </h2>
      <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
        {children}
      </div>
    </div>
  );
}

interface CopyBlockProps extends CopyContentProps {
  readonly top: number;
  readonly left: number;
}

function CopyBlock({ top, left, ...content }: CopyBlockProps) {
  return (
    <div
      className={`absolute z-20 -translate-x-1/2 -translate-y-1/2 ${
        content.side ? "w-[min(44%,26rem)]" : "w-[min(92%,34rem)]"
      }`}
      style={{ top: pct(top, H), left: `${left}%` }}
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -inset-x-32 -inset-y-20"
        style={{ background: SCRIM }}
      />
      <CopyContent {...content} />
    </div>
  );
}

/** The dim example pointer under a chapter's concept text. */
function DiagramNote({ children }: { readonly children: ReactNode }) {
  return <p className="text-cc-ink-dim text-xs sm:text-sm">{children}</p>;
}

/* ── Code boxes ──────────────────────────────────────────────────────── */

interface CodeLine {
  readonly text: string;
  /** Owner dots, error tint, or a dim service prefix. */
  readonly dots?: readonly string[];
  readonly tone?: "error" | "ok";
  readonly prefix?: { readonly label: string; readonly color: string };
}

interface CodeContentProps {
  readonly label: string;
  readonly color?: string;
  readonly lines: readonly CodeLine[];
  readonly footer?: ReactNode;
}

function CodeContent({ label, color, lines, footer }: CodeContentProps) {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <div className="flex items-center gap-2">
        {color && (
          <span
            className="inline-block h-2.5 w-2.5 rounded-[3px]"
            style={{ background: color }}
          />
        )}
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
          {label}
        </span>
      </div>
      <div className="border-cc-card-border mt-2 border-t pt-2 font-mono text-[12px] leading-6">
        {lines.map((l, i) => (
          <div key={i} className="flex items-center gap-2">
            {l.prefix && (
              <span
                className="w-16 shrink-0 text-[10px]"
                style={{ color: l.prefix.color }}
              >
                {l.prefix.label}
              </span>
            )}
            <span
              className={
                "whitespace-pre " +
                (l.tone === "error"
                  ? "text-[#f27765]"
                  : l.tone === "ok"
                    ? "text-[#8fd6a0]"
                    : "text-[#c9d4e8]")
              }
            >
              {l.text}
            </span>
            {l.dots && l.dots.length > 0 && (
              <span className="ml-auto flex items-center gap-1">
                {l.dots.map((d, k) => (
                  <span
                    key={k}
                    className="inline-block h-2 w-2 rounded-full"
                    style={{ background: d }}
                  />
                ))}
              </span>
            )}
          </div>
        ))}
      </div>
      {footer && (
        <div className="border-cc-card-border text-cc-ink-dim mt-2 border-t pt-2 font-mono text-[10.5px]">
          {footer}
        </div>
      )}
    </div>
  );
}

interface PlacedBox extends CodeContentProps {
  readonly top: number;
  readonly left: number;
  /** Side-by-side pair: narrower so the two never collide as the canvas shrinks. */
  readonly paired?: boolean;
}

function CodeBox({ top, left, paired, ...content }: PlacedBox) {
  return (
    <div
      className={`absolute z-30 -translate-x-1/2 -translate-y-1/2 ${
        paired ? "w-[min(43%,21rem)]" : "w-[min(88%,21rem)]"
      }`}
      style={{ top: pct(top, H), left: `${left}%` }}
    >
      <CodeContent {...content} />
    </div>
  );
}

/* ── The compose terminal, straight from the product page's CLI ──────── */

interface TermSeg {
  readonly t: string;
  readonly c: string;
  /** Render a small arrow icon before this segment. */
  readonly arrow?: boolean;
}

function ArrowIcon({ color }: { readonly color: string }) {
  return (
    <svg
      viewBox="0 0 12 12"
      aria-hidden="true"
      className="mr-1 inline-block h-3 w-3 align-[-1px]"
      fill="none"
      stroke={color}
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M1.5 6h8M6.5 3l3 3-3 3" />
    </svg>
  );
}

const TERM_CMD = "#c9d1d9";
const TERM_DIM = "#8b949e";

const COMPOSE_TERMINAL: readonly (readonly TermSeg[])[] = [
  [
    { t: "$ ", c: TERM_DIM },
    { t: "nitro fusion compose", c: TERM_CMD },
  ],
  [{ t: "✕ OUTPUT_FIELD_TYPES_NOT_MERGEABLE", c: "#f27765" }],
  [{ t: "  Product.price: Money! ≠ Float!", c: TERM_DIM }],
  [{ t: "✕ exit 1 · nothing deployed", c: "rgba(242,119,101,0.75)" }],
  [{ t: " ", c: TERM_DIM }],
  [
    { t: "$ ", c: TERM_DIM },
    { t: "nitro fusion compose", c: TERM_CMD },
    { t: "  # catalog aligned", c: TERM_DIM },
  ],
  [{ t: "✓ composed 5 source schemas · 0 errors", c: "#66be77" }],
  [
    { t: "gateway.far", c: "#a5d6ff", arrow: true },
    { t: " · loaded by the gateway", c: TERM_CMD },
  ],
];

function TerminalCard() {
  return (
    <div className="border-cc-card-border overflow-hidden rounded-xl border bg-[#0b101c]">
      <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span aria-hidden="true" className="flex items-center gap-1.5">
          <span className="h-2 w-2 rounded-full bg-[#f27765]" />
          <span className="h-2 w-2 rounded-full bg-[#eabd21]" />
          <span className="h-2 w-2 rounded-full bg-[#66be77]" />
        </span>
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
          ci · fusion compose
        </span>
      </div>
      <div className="px-4 py-3 font-mono text-[11.5px] leading-6">
        {COMPOSE_TERMINAL.map((line, i) => (
          <div key={i} className="whitespace-pre">
            {line.map((seg, j) => (
              <span key={j} style={{ color: seg.c }}>
                {seg.arrow && <ArrowIcon color={seg.c} />}
                {seg.t}
              </span>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

/* ── The story, shared by the desktop canvas and the mobile column ───── */

interface Chapter {
  readonly copy: {
    readonly top: number;
    readonly left: number;
    readonly side?: boolean;
  } & CopyContentProps;
  readonly boxes: readonly PlacedBox[];
  /** Where the compose terminal sits beside this chapter's card, if it has one. */
  readonly terminal?: { readonly top: number; readonly left: number };
}

const STORY: readonly Chapter[] = [
  {
    copy: {
      top: 380,
      left: 50,
      title: "A service owns its data.",
      children: (
        <>
          <p>
            A service is a program that owns one area of data. It stores that
            data in its own database. Other programs do not read that database
            directly. They go through the service&apos;s API.
          </p>
          <p>The database stays private. The API is the only way in.</p>
        </>
      ),
    },
    boxes: [],
  },
  {
    copy: {
      top: 900,
      left: 68,
      side: true,
      title: "The schema states what the service provides.",
      children: (
        <>
          <p>
            Every API needs a contract. The schema is that contract, written
            down. It lists each piece of data the service provides. It gives
            each piece a name and a type.
          </p>
          <p>
            Anyone can read the schema and know what the service offers. People
            read it to build against the API. Tools read it to check every
            request.
          </p>
        </>
      ),
    },
    boxes: [
      {
        top: 900,
        left: 26,
        label: "Catalog · schema.graphql",
        color: CANON[0].color,
        lines: [
          { text: "type Product {" },
          { text: "  id: ID!" },
          { text: "  name: String!" },
          { text: "  price: Float!" },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 1500,
      left: 68,
      side: true,
      title: "Most systems grow into many services.",
      children: (
        <>
          <p>
            Companies grow, and new use cases emerge. Payments, logistics,
            customer accounts: each becomes its own area. Often, a different
            team owns each area. Eventually, no single team owns the whole.
          </p>
          <p>
            Each team builds a service for its area. Every service brings its
            own database, its own API, and its own schema. A large company runs
            many services, owned by many teams.
          </p>
        </>
      ),
    },
    boxes: [
      {
        top: 1500,
        left: 34,
        label: "Billing · schema.graphql",
        color: CANON[1].color,
        lines: [
          { text: "type Product {" },
          { text: "  id: ID!" },
          { text: "  price: Money!" },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 2350,
      left: 50,
      title: "Each service belongs to one team.",
      children: (
        <>
          <p>
            This split is on purpose. One team owns each service. The team
            writes the code, runs the database, and handles the incidents. It
            can change its service without asking another team.
          </p>
          <p>
            It deploys on its own schedule. Nobody waits for a shared release
            date. This independence is why companies accept the extra
            complexity.
          </p>
        </>
      ),
    },
    boxes: [],
  },
  {
    copy: {
      top: 3150,
      left: 68,
      side: true,
      title: "Different services store the same data.",
      children: (
        <>
          <p>
            Services split a system by area. The data does not split as cleanly.
            The same thing shows up in several areas. So each service stores its
            own view of it, shaped for its own job. That is normal. But the
            single source of truth is gone.
          </p>
          <p>
            No service holds the whole truth, and the views do not have to
            agree. Different teams wrote them at different times, for different
            needs. They use different names, different types, different rules.
            Nothing compares the definitions. The mismatch stays hidden until
            something breaks.
          </p>
          <DiagramNote>
            In the diagram: two services store a price, one as Float, one as
            Money.
          </DiagramNote>
        </>
      ),
    },
    boxes: [
      {
        top: 3150,
        left: 30,
        label: "The same field, twice",
        lines: [
          {
            text: "price: Float!",
            tone: "error",
            prefix: { label: "catalog", color: CANON[0].color },
          },
          {
            text: "price: Money!",
            tone: "error",
            prefix: { label: "billing", color: CANON[1].color },
          },
        ],
      },
    ],
  },
  {
    copy: {
      top: 3950,
      left: 32,
      side: true,
      title: "Every app merges data from many services.",
      children: (
        <>
          <p>
            One screen rarely shows data from a single service. It shows several
            areas of data at once. So the app calls each service, one request
            per service. Then it merges the answers into one view.
          </p>
          <p>
            This merging code is not small. It handles slow services, failed
            calls, and mismatched formats. Every app writes its own version of
            it. And when a service renames a field, every copy of that code
            breaks.
          </p>
        </>
      ),
    },
    boxes: [
      {
        top: 3950,
        left: 70,
        label: "One screen · five calls",
        lines: [
          { text: "GET /products/P-42", dots: [CANON[0].color] },
          { text: "GET /prices/P-42", dots: [CANON[1].color] },
          { text: "GET /orders?product=P-42", dots: [CANON[2].color] },
          { text: "GET /shipping/P-42", dots: [CANON[3].color] },
          { text: "GET /account", dots: [CANON[4].color] },
        ],
      },
    ],
  },
  {
    copy: {
      top: 4550,
      left: 68,
      side: true,
      title: "GraphQL lets clients ask for exact fields.",
      children: (
        <>
          <p>
            GraphQL is a query language for APIs. A GraphQL API publishes one
            schema. The schema is a typed list of every field you can request.
          </p>
          <p>
            A client sends one query. The query names the exact fields the
            client needs. The API returns one response with exactly those
            fields. The response contains nothing else. One request describes
            the whole screen.
          </p>
        </>
      ),
    },
    boxes: [
      {
        top: 4550,
        left: 30,
        label: "One query",
        lines: [
          { text: "{" },
          { text: '  product(id: "P-42") {' },
          { text: "    name", dots: [CANON[0].color] },
          { text: "    price", dots: [CANON[1].color] },
          { text: "    delivery", dots: [CANON[3].color] },
          { text: "  }" },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 5050,
      left: 50,
      title: "One GraphQL API needs one schema.",
      children: (
        <>
          <p>
            One schema for everything is the power of GraphQL. Clients see all
            the data in one place, with one set of types. They do not care which
            service owns what. That is exactly what you want.
          </p>
          <p>
            But your data now spans many services. Each service can describe
            only its own part. No single service can write the whole schema. You
            could build one big server in front of everything. Then one team
            owns that server, and every change queues behind it. That is the
            bottleneck you split services to avoid.
          </p>
        </>
      ),
    },
    boxes: [],
  },
  {
    copy: {
      top: 5970,
      left: 50,
      title: "Federation merges the schemas, not the services.",
      children: (
        <>
          <p>
            This is federation. Each service keeps its own schema and publishes
            it. A build step called composition collects the schemas. It merges
            them into one combined schema. Types that describe the same thing
            match by a shared id. Each field in the result keeps exactly one
            owning service.
          </p>
          <p>
            When two definitions conflict, composition fails the build. The
            error names the exact conflict. Nothing broken reaches production.
            The services themselves stay separate: separate code, separate
            databases, separate deploys.
          </p>
          <DiagramNote>
            In the diagram: Float versus Money stops the build.
          </DiagramNote>
        </>
      ),
    },
    terminal: { top: 6320, left: 27 },
    boxes: [
      {
        top: 6320,
        left: 73,
        paired: true,
        label: "Composite schema",
        lines: [
          { text: "type Product {" },
          {
            text: "  id: ID!",
            dots: [CANON[0].color, CANON[1].color, CANON[3].color],
          },
          { text: "  name: String!", dots: [CANON[0].color] },
          { text: "  description: String", dots: [CANON[0].color] },
          { text: "  price: Money!", dots: [CANON[1].color] },
          { text: "  delivery: Date!", dots: [CANON[3].color] },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 6820,
      left: 50,
      title: "A gateway serves the merged schema.",
      children: (
        <>
          <p>
            The merged schema needs a server. That server is the gateway. It
            offers one endpoint and one schema to all clients. When a query
            arrives, the gateway reads which fields it asks for. It calls only
            the services that own those fields. It combines their answers into
            one response.
          </p>
          <p>
            The merging code from every app now runs in one place. Apps stop
            writing it. The gateway is infrastructure, not another integration
            to maintain.
          </p>
        </>
      ),
    },
    boxes: [],
  },
  {
    copy: {
      top: 7280,
      left: 50,
      title: "Clients get one API, and teams stay independent.",
      children: (
        <>
          <p>
            Clients see one endpoint and one schema. They write queries, not
            merging code. Teams keep their own services, databases, and release
            schedules. A team can change its part of the schema alone.
            Composition checks every change against the whole.
          </p>
          <p>
            Conflicts fail the build, not production. That is the trade
            federation offers: one API for clients, independence for teams, and
            a build step that guards both. The next section shows how one
            request runs.
          </p>
        </>
      ),
    },
    boxes: [],
  },
];

/* ── The walkthrough ─────────────────────────────────────────────────── */

export function TransitStory() {
  return (
    <section className="border-cc-card-border overflow-hidden border-t">
      {/* Phones: the same story as a plain stacked column. */}
      <div className="space-y-10 px-5 py-16 sm:hidden">
        {STORY.map((chapter, i) => (
          <div key={i} className="space-y-10">
            <CopyContent title={chapter.copy.title}>
              {chapter.copy.children}
            </CopyContent>
            {chapter.terminal && (
              <div className="mx-auto w-[min(100%,21rem)]">
                <TerminalCard />
              </div>
            )}
            {chapter.boxes.map((box, j) => (
              <div key={j} className="mx-auto w-[min(100%,21rem)]">
                <CodeContent {...box} />
              </div>
            ))}
          </div>
        ))}
      </div>

      {/* Larger screens: the stream canvas. */}
      <div
        className="relative mx-auto hidden w-full max-w-5xl sm:block"
        style={{ aspectRatio: `${W} / ${H}` }}
      >
        {/* The streams: parallel service columns composing at the end. */}
        <svg
          viewBox={`0 0 ${W} ${H}`}
          aria-hidden="true"
          className="absolute inset-0 z-0 h-full w-full"
        >
          <defs>
            <linearGradient
              id="fw-out"
              x1="0"
              y1={HUB.y}
              x2="0"
              y2={H}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor="#f27765" />
              <stop offset="0.55" stopColor="#eabd21" />
              <stop offset="0.8" stopColor="#66be77" />
              <stop offset="1" stopColor="#66be77" stopOpacity="0" />
            </linearGradient>
            {/* Vertical fade for the copy bands: recede mid-band, full at edges. */}
            <linearGradient id="fw-gap" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#fff" />
              <stop offset="0.18" stopColor="#333" />
              <stop offset="0.82" stopColor="#333" />
              <stop offset="1" stopColor="#fff" />
            </linearGradient>
            <mask
              id="fw-mask"
              maskUnits="userSpaceOnUse"
              x="0"
              y="0"
              width={W}
              height={H}
            >
              <rect x="0" y="0" width={W} height={H} fill="#fff" />
              {GAPS.map((g, i) => (
                <rect
                  key={i}
                  x={g.x}
                  y={g.y}
                  width={g.w}
                  height={g.h}
                  fill="url(#fw-gap)"
                />
              ))}
            </mask>
          </defs>

          <g mask="url(#fw-mask)">
            {MARKERS.map((m) => (
              <path
                key={m.s}
                d={streamPath(m.x, m.y + 12)}
                fill="none"
                stroke={CANON[m.s].color}
                strokeWidth={2.5}
                strokeOpacity={0.9}
                strokeLinecap="round"
              />
            ))}

            {/* One output line leaving the composition, homepage gradient. */}
            <rect
              x={HUB.x - 1.25}
              y={HUB.y + 12}
              width={2.5}
              height={H - HUB.y - 12}
              fill="url(#fw-out)"
            />
          </g>

          {MARKERS.map((m) => (
            <g key={m.s}>
              <rect
                x={m.x - 8}
                y={m.y - 8}
                width={16}
                height={16}
                rx={4}
                fill={CANON[m.s].color}
              />
              <text
                x={m.x + 20}
                y={m.y + 5}
                textAnchor="start"
                fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
                fontSize={13}
                letterSpacing="0.18em"
                fill={INK_DIM}
              >
                {CANON[m.s].name.toUpperCase()}
              </text>
            </g>
          ))}

          {/* The composition node, straight from the homepage. */}
          <GlowNode x={HUB.x} y={HUB.y} id="fw-hub" r={10} />
          <text
            x={HUB.x - 122}
            y={HUB.y + 4}
            textAnchor="end"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize={11}
            letterSpacing="0.2em"
            fill={INK_DIM}
          >
            FUSION COMPOSITION
          </text>
          <line
            x1={HUB.x - 112}
            x2={HUB.x - 38}
            y1={HUB.y}
            y2={HUB.y}
            stroke="rgba(245,241,234,0.3)"
            strokeDasharray="4 5"
          />

          {/* The horizon: everything above happened before deploy. */}
          <line
            x1={120}
            x2={904}
            y1={HORIZON_Y}
            y2={HORIZON_Y}
            stroke="rgba(245,241,234,0.22)"
            strokeDasharray="5 7"
          />
          <text
            x={140}
            y={HORIZON_Y - 16}
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize={11}
            letterSpacing="0.2em"
            fill={INK_DIM}
          >
            BUILD TIME
          </text>
          <text
            x={140}
            y={HORIZON_Y + 26}
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize={11}
            letterSpacing="0.2em"
            fill={INK_DIM}
            opacity={0.7}
          >
            RUNTIME
          </text>

          {/* The runtime gateway on the output line. */}
          <GatewayChip x={CHIP.x} y={CHIP.y} />
        </svg>

        {/* The explanation. */}
        {STORY.map((chapter, i) => (
          <div key={i} className="contents">
            <CopyBlock
              top={chapter.copy.top}
              left={chapter.copy.left}
              side={chapter.copy.side}
              title={chapter.copy.title}
            >
              {chapter.copy.children}
            </CopyBlock>
            {chapter.terminal && (
              <div
                className="absolute z-30 w-[min(43%,21rem)] -translate-x-1/2 -translate-y-1/2"
                style={{
                  top: pct(chapter.terminal.top, H),
                  left: `${chapter.terminal.left}%`,
                }}
              >
                <TerminalCard />
              </div>
            )}
            {chapter.boxes.map((box, j) => (
              <CodeBox key={j} {...box} />
            ))}
          </div>
        ))}
      </div>
    </section>
  );
}
