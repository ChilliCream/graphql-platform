"use client";

/**
 * The federation walkthrough, in the homepage FusionFlow language. Five
 * service streams hold their own parallel columns while the story introduces
 * them one by one, then all five bend together through one shared convergence
 * band into the glowing composition node, with a single output line
 * continuing below. Before the hub, each chapter is a side-by-side row: the
 * schema card hangs directly on its service's line (the line runs behind the
 * card) with the explaining copy beside it, so ownership is readable at a
 * glance. After the hub there is only one line, so copy and cards center on
 * it. On phones the fixed-aspect canvas would collapse, so the same story
 * renders as a plain stacked column instead. Static; the page scrolls
 * normally.
 */

import type { ReactNode } from "react";

import { CANON, GlowNode, INK_DIM } from "./visuals/stage";

/* Design space, rendered 1:1 at the section's max width. Kept tight: no run
 * of bare line between chapters is longer than ~230 units, so every viewport
 * always holds a chapter. */
const W = 1024;
const H = 4100;

/* One column per service; markers join their column as the story needs them. */
const MARKERS = [
  { s: 0, x: 150, y: 120 },
  { s: 1, x: 320, y: 690 },
  { s: 2, x: 512, y: 1290 },
  { s: 3, x: 704, y: 1360 },
  { s: 4, x: 874, y: 1430 },
] as const;

/* Every stream falls straight, then bends through the same band into the hub. */
const BEND_START = 2300;
const HUB = { x: 512, y: 2760 } as const;

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
  { x: 470, w: 460, y: 1575, h: 230 },
  { x: 470, w: 460, y: 1935, h: 230 },
  { x: 220, w: 584, y: 2895, h: 230 },
  { x: 220, w: 584, y: 3475, h: 230 },
] as const;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

/* ── Copy blocks ─────────────────────────────────────────────────────── */

const SCRIM =
  "radial-gradient(ellipse 58% 54% at 50% 50%, rgba(11,15,26,0.93) 0%, rgba(11,15,26,0.85) 45%, rgba(11,15,26,0.45) 72%, rgba(11,15,26,0) 90%)";

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
        className="pointer-events-none absolute -inset-x-24 -inset-y-14"
        style={{ background: SCRIM }}
      />
      <CopyContent {...content} />
    </div>
  );
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
      className={`absolute z-10 -translate-x-1/2 -translate-y-1/2 ${
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
    { t: "  # billing fixed", c: TERM_DIM },
  ],
  [{ t: "✓ composed 5 source schemas · 0 errors", c: "#66be77" }],
  [
    { t: "→ ", c: TERM_CMD },
    { t: "gateway.far", c: "#a5d6ff" },
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
      top: 400,
      left: 68,
      side: true,
      title: "Every team runs its own service.",
      children: (
        <p>
          Catalog is an ordinary GraphQL service: its own code, its own
          database, its own deploys. Its schema is the part the rest of the
          company sees.
        </p>
      ),
    },
    boxes: [
      {
        top: 400,
        left: 26,
        label: "Catalog · schema.graphql",
        color: CANON[0].color,
        lines: [
          { text: "type Product {" },
          { text: "  id: ID!" },
          { text: "  name: String!" },
          { text: "  description: String" },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 1000,
      left: 68,
      side: true,
      title: "More teams, more schemas.",
      children: (
        <p>
          Billing ships separately, on its own schedule. That independence is
          the point: nobody waits on anybody. But every new service is another
          schema, and another API for clients to deal with.
        </p>
      ),
    },
    boxes: [
      {
        top: 1000,
        left: 34,
        label: "Billing · schema.graphql",
        color: CANON[1].color,
        lines: [
          { text: "type Product {" },
          { text: "  id: ID!" },
          { text: "  price: Float!" },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 1690,
      left: 68,
      side: true,
      title: "Left alone, nothing lines up.",
      children: (
        <p>
          Clients now call several APIs and join the results themselves, in
          every app, again and again. And nothing stops two teams from
          describing the same thing differently:
        </p>
      ),
    },
    boxes: [
      {
        top: 1690,
        left: 30,
        label: "The same field, twice",
        lines: [
          {
            text: "price: Money!",
            prefix: { label: "catalog", color: CANON[0].color },
          },
          {
            text: "price: Float!",
            tone: "error",
            prefix: { label: "billing", color: CANON[1].color },
          },
        ],
        footer: "Nothing catches this until someone's app breaks.",
      },
    ],
  },
  {
    copy: {
      top: 2050,
      left: 68,
      side: true,
      title: "Entities: one object, in many services.",
      children: (
        <p>
          Catalog knows this product&apos;s name. Billing knows its price.
          Shipping knows when it arrives. They are all describing the same
          product, so federation gives them one identity: types that share an id
          are one entity, assembled across services.
        </p>
      ),
    },
    boxes: [
      {
        top: 2050,
        left: 32,
        label: 'Product · "P-401" · one entity',
        lines: [
          { text: "{" },
          {
            text: '  "id": "P-401",',
            dots: [CANON[0].color, CANON[1].color, CANON[3].color],
          },
          { text: '  "name": "Aero Mug",', dots: [CANON[0].color] },
          { text: '  "price": "24.90 EUR",', dots: [CANON[1].color] },
          { text: '  "delivery": "2d"', dots: [CANON[3].color] },
          { text: "}" },
        ],
        footer:
          "Each service stores its slice. The shared id makes them one product.",
      },
    ],
  },
  {
    copy: {
      top: 3010,
      left: 50,
      title: "Composition merges them into one schema.",
      children: (
        <p>
          Fusion takes every schema and composes one graph out of them, at build
          time. Types that share an identity are merged; every field keeps
          exactly one owner; and conflicts, like that Float!, fail the build
          before anything ships.
        </p>
      ),
    },
    terminal: { top: 3290, left: 27 },
    boxes: [
      {
        top: 3290,
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
          { text: "  delivery: String!", dots: [CANON[3].color] },
          { text: "}" },
        ],
        footer: "✓ every field keeps exactly one owner",
      },
    ],
  },
  {
    copy: {
      top: 3590,
      left: 50,
      title: "Clients get one endpoint.",
      children: (
        <p>
          Apps send one query. The gateway splits it across the services that
          own each field, runs the calls, and returns one response. Nobody
          stitches anything by hand anymore.
        </p>
      ),
    },
    boxes: [
      {
        top: 3860,
        left: 27,
        paired: true,
        label: "One request",
        lines: [
          { text: "{" },
          { text: '  product(id: "P-401") {' },
          { text: "    name", dots: [CANON[0].color] },
          { text: "    price", dots: [CANON[1].color] },
          { text: "    delivery", dots: [CANON[3].color] },
          { text: "  }" },
          { text: "}" },
        ],
      },
      {
        top: 3860,
        left: 73,
        paired: true,
        label: "One response",
        lines: [
          { text: "{" },
          { text: '  "product": {' },
          { text: '    "name": "Aero Mug",', dots: [CANON[0].color] },
          { text: '    "price": "24.90 EUR",', dots: [CANON[1].color] },
          { text: '    "delivery": "2d"', dots: [CANON[3].color] },
          { text: "  }" },
          { text: "}" },
        ],
      },
    ],
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
              <stop offset="0.22" stopColor="#555" />
              <stop offset="0.78" stopColor="#555" />
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

            {/* Composition emits one graph: a pulse rides the output line down,
                fading under the copy bands just as the line does. */}
            <rect
              className="motion-reduce:hidden"
              x={HUB.x - 2}
              y={HUB.y + 12}
              width={4}
              height={20}
              rx={2}
              fill="url(#fw-emit)"
              opacity={0}
            >
              <animateTransform
                attributeName="transform"
                type="translate"
                dur="6s"
                repeatCount="indefinite"
                keyTimes="0;1"
                values="0 0;0 760"
              />
              <animate
                attributeName="opacity"
                dur="6s"
                repeatCount="indefinite"
                keyTimes="0;0.08;0.55;1"
                values="0;0.9;0.9;0"
              />
            </rect>
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

          {/* Ambient composition-node motion. Every layer here is additive and
              gated with motion-reduce:hidden, so reduced motion shows exactly the
              static GlowNode below. */}
          <defs>
            <radialGradient id="fw-sun-halo" cx="50%" cy="50%" r="50%">
              <stop offset="0" stopColor="#fff" stopOpacity="0.7" />
              <stop offset="0.4" stopColor="#fff" stopOpacity="0.14" />
              <stop offset="1" stopColor="#fff" stopOpacity="0" />
            </radialGradient>
            <linearGradient id="fw-emit" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#fff" />
              <stop offset="1" stopColor="#66be77" />
            </linearGradient>
            {/* Invisible duplicates of the streams, ridden by the intake beads. */}
            {MARKERS.map((m) => (
              <path
                key={m.s}
                id={`fw-bead-path-${m.s}`}
                d={streamPath(m.x, m.y + 12)}
                fill="none"
                stroke="none"
              />
            ))}
          </defs>
          <style>{`
            @keyframes fw-breathe {
              0%, 100% { transform: scale(1); opacity: 0.5; }
              50% { transform: scale(1.05); opacity: 0.82; }
            }
            @keyframes fw-orbit { to { transform: rotate(360deg); } }
            .fw-breathe {
              transform-box: fill-box;
              transform-origin: center;
              animation: fw-breathe 6s ease-in-out infinite;
            }
            .fw-orbit {
              transform-box: fill-box;
              transform-origin: center;
              animation: fw-orbit 48s linear infinite;
            }
          `}</style>

          {/* Halo breathing swell. */}
          <circle
            className="fw-breathe motion-reduce:hidden"
            cx={HUB.x}
            cy={HUB.y}
            r={54}
            fill="url(#fw-sun-halo)"
          />
          {/* Absorption heartbeat, flashing at the intake cadence. */}
          <circle
            className="motion-reduce:hidden"
            cx={HUB.x}
            cy={HUB.y}
            r={40}
            fill="url(#fw-sun-halo)"
            opacity={0}
          >
            <animate
              attributeName="opacity"
              dur="2.4s"
              begin="1.5s"
              repeatCount="indefinite"
              keyTimes="0;0.12;0.42;1"
              values="0;0.2;0;0"
            />
          </circle>
          {/* Faint dashed orbit, echoing the roundel language. */}
          <circle
            className="fw-orbit motion-reduce:hidden"
            cx={HUB.x}
            cy={HUB.y}
            r={34}
            fill="none"
            stroke="rgba(245,241,234,0.16)"
            strokeWidth={1}
            strokeDasharray="2 8"
          />

          {/* The composition node, straight from the homepage. */}
          <GlowNode x={HUB.x} y={HUB.y} id="fw-hub" r={10} />

          {/* Service-colored intake beads riding the last stretch into the hub. */}
          {MARKERS.map((m) => (
            <circle
              key={m.s}
              className="motion-reduce:hidden"
              r={3}
              fill={CANON[m.s].color}
              opacity={0}
            >
              <animateMotion
                dur="12s"
                begin={`${-m.s * 2.4}s`}
                repeatCount="indefinite"
                calcMode="linear"
                keyTimes="0;0.15;1"
                keyPoints="0.82;1;1"
              >
                <mpath href={`#fw-bead-path-${m.s}`} />
              </animateMotion>
              <animate
                attributeName="opacity"
                dur="12s"
                begin={`${-m.s * 2.4}s`}
                repeatCount="indefinite"
                keyTimes="0;0.03;0.12;0.15;1"
                values="0;0.95;0.95;0;0"
              />
            </circle>
          ))}
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
                className="absolute z-10 w-[min(43%,21rem)] -translate-x-1/2 -translate-y-1/2"
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
