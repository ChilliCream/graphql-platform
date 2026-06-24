"use client";

import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

// The brand spectrum, used exactly ONCE on this page as the very last sketched
// underline on the back cover (closing CTA).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// The single ink colour used for every sketched mark on this journal. Matches
// cc-accent (#5eead4).
const INK = "#5eead4";

// -----------------------------------------------------------------------------
// Roughened stroke filter. A small turbulence + displacement pass that gives
// every <path> a subtle hand-drawn wobble. Defined once at page scope and
// referenced by id from each sketched glyph.
// -----------------------------------------------------------------------------

function RoughDefs() {
  return (
    <svg
      aria-hidden
      width="0"
      height="0"
      style={{ position: "absolute" }}
      focusable="false"
    >
      <defs>
        <filter
          id="hc-fj-rough"
          x="-5%"
          y="-5%"
          width="110%"
          height="110%"
          colorInterpolationFilters="sRGB"
        >
          <feTurbulence
            type="fractalNoise"
            baseFrequency="0.02"
            numOctaves="2"
            seed="7"
            result="noise"
          />
          <feDisplacementMap in="SourceGraphic" in2="noise" scale="1.2" />
        </filter>
        <filter
          id="hc-fj-rough-strong"
          x="-5%"
          y="-5%"
          width="110%"
          height="110%"
          colorInterpolationFilters="sRGB"
        >
          <feTurbulence
            type="fractalNoise"
            baseFrequency="0.03"
            numOctaves="2"
            seed="3"
            result="noise"
          />
          <feDisplacementMap in="SourceGraphic" in2="noise" scale="2.2" />
        </filter>
      </defs>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Sketched primitives. Each is an inline SVG path that draws itself in once
// when scrolled into view (whileInView + viewport once: true). NO scroll
// coupling. Respects prefers-reduced-motion via useReducedMotion.
// -----------------------------------------------------------------------------

interface DrawProps {
  readonly className?: string;
  readonly style?: React.CSSProperties;
}

/** A wobbly horizontal rule used as a section divider. */
function SketchedRule({ className }: DrawProps) {
  const reduce = useReducedMotion();
  const path =
    "M 2 6 C 60 2, 140 10, 220 5 S 360 9, 460 4 S 620 8, 720 5 S 880 9, 998 6";
  return (
    <svg
      viewBox="0 0 1000 12"
      preserveAspectRatio="none"
      className={className}
      role="presentation"
      aria-hidden
    >
      <motion.path
        d={path}
        stroke={INK}
        strokeWidth="1.4"
        strokeOpacity="0.55"
        strokeLinecap="round"
        fill="none"
        filter="url(#hc-fj-rough)"
        initial={reduce ? false : { pathLength: 0, opacity: 0 }}
        whileInView={reduce ? undefined : { pathLength: 1, opacity: 1 }}
        viewport={{ once: true, margin: "0px 0px -10% 0px" }}
        transition={{ duration: 0.7, ease: "easeOut" }}
      />
    </svg>
  );
}

/** A wobbly dotted rule used for the signature line on the back cover. */
function DottedSignatureRule({ className }: DrawProps) {
  const reduce = useReducedMotion();
  return (
    <svg
      viewBox="0 0 1000 12"
      preserveAspectRatio="none"
      className={className}
      role="presentation"
      aria-hidden
    >
      <motion.path
        d="M 2 6 C 60 4, 140 8, 220 6 S 360 8, 460 5 S 620 7, 720 6 S 880 8, 998 6"
        stroke={INK}
        strokeWidth="1.5"
        strokeOpacity="0.75"
        strokeLinecap="round"
        strokeDasharray="2 6"
        fill="none"
        filter="url(#hc-fj-rough)"
        initial={reduce ? false : { pathLength: 0, opacity: 0 }}
        whileInView={reduce ? undefined : { pathLength: 1, opacity: 1 }}
        viewport={{ once: true, margin: "0px 0px -10% 0px" }}
        transition={{ duration: 0.7, ease: "easeOut" }}
      />
    </svg>
  );
}

/** A hand-drawn checkmark glyph for field-note observations. */
function SketchedCheck() {
  const reduce = useReducedMotion();
  return (
    <svg
      viewBox="0 0 20 20"
      width="18"
      height="18"
      role="presentation"
      aria-hidden
    >
      <motion.path
        d="M 3 11 C 5 12, 6 13, 8 16 C 11 11, 14 6, 18 3"
        stroke={INK}
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
        fill="none"
        filter="url(#hc-fj-rough)"
        initial={reduce ? false : { pathLength: 0 }}
        whileInView={reduce ? undefined : { pathLength: 1 }}
        viewport={{ once: true }}
        transition={{ duration: 0.5, ease: "easeOut" }}
      />
    </svg>
  );
}

/** A sketched curving arrow that connects a margin label to a target. */
interface SketchArrowProps {
  readonly d: string;
  readonly headD: string;
  readonly className?: string;
  readonly viewBox?: string;
  readonly delay?: number;
  readonly opacity?: number;
}

function SketchArrow({
  d,
  headD,
  className,
  viewBox = "0 0 200 80",
  delay = 0,
  opacity = 0.75,
}: SketchArrowProps) {
  const reduce = useReducedMotion();
  return (
    <svg
      viewBox={viewBox}
      className={className}
      role="presentation"
      aria-hidden
    >
      <motion.path
        d={d}
        stroke={INK}
        strokeWidth="1.4"
        strokeOpacity={opacity}
        strokeLinecap="round"
        fill="none"
        filter="url(#hc-fj-rough)"
        initial={reduce ? false : { pathLength: 0, opacity: 0 }}
        whileInView={reduce ? undefined : { pathLength: 1, opacity }}
        viewport={{ once: true, margin: "0px 0px -10% 0px" }}
        transition={{ duration: 0.6, ease: "easeOut", delay }}
      />
      <motion.path
        d={headD}
        stroke={INK}
        strokeWidth="1.4"
        strokeOpacity={opacity}
        strokeLinecap="round"
        strokeLinejoin="round"
        fill="none"
        filter="url(#hc-fj-rough)"
        initial={reduce ? false : { pathLength: 0, opacity: 0 }}
        whileInView={reduce ? undefined : { pathLength: 1, opacity }}
        viewport={{ once: true, margin: "0px 0px -10% 0px" }}
        transition={{
          duration: 0.3,
          ease: "easeOut",
          delay: delay + 0.55,
        }}
      />
    </svg>
  );
}

/** A wobbly circle, used to ink-around a key phrase. Optional breathing loop. */
interface SketchedCircleProps {
  readonly className?: string;
  readonly breathing?: boolean;
}

function SketchedCircle({ className, breathing = false }: SketchedCircleProps) {
  const reduce = useReducedMotion();
  // A slightly imperfect oval, drawn as a single closed bezier so it looks
  // hand-traced rather than geometric.
  const d =
    "M 14 36 C 8 18, 36 5, 80 5 C 138 4, 196 12, 198 36 C 200 60, 156 71, 96 71 C 38 72, 18 58, 14 36 Z";
  return (
    <svg
      viewBox="0 0 212 78"
      preserveAspectRatio="none"
      className={className}
      role="presentation"
      aria-hidden
    >
      <motion.path
        d={d}
        stroke={INK}
        strokeOpacity="0.9"
        strokeLinecap="round"
        fill="none"
        filter="url(#hc-fj-rough-strong)"
        initial={reduce ? { strokeWidth: 1.6 } : { pathLength: 0, opacity: 0 }}
        whileInView={
          reduce ? undefined : { pathLength: 1, opacity: 1, strokeWidth: 1.6 }
        }
        viewport={{ once: true, margin: "0px 0px -10% 0px" }}
        animate={
          breathing && !reduce ? { strokeWidth: [1.5, 1.7, 1.5] } : undefined
        }
        transition={
          breathing && !reduce
            ? {
                strokeWidth: {
                  duration: 4,
                  repeat: Infinity,
                  ease: "easeInOut",
                },
                pathLength: { duration: 0.8, ease: "easeOut" },
              }
            : { duration: 0.8, ease: "easeOut" }
        }
      />
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Small text primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface DateStampProps {
  readonly date: string;
  readonly entry: string;
}

function DateStamp({ date, entry }: DateStampProps) {
  return (
    <div className="text-cc-ink-dim flex flex-col items-end gap-1 font-mono text-[11px] tracking-wider uppercase">
      <span className="border-cc-card-border rounded-sm border px-2 py-0.5 italic">
        {entry}
      </span>
      <span className="not-italic tabular-nums">{date}</span>
    </div>
  );
}

interface MarginNoteProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function MarginNote({ children, className }: MarginNoteProps) {
  return (
    <p
      className={[
        "text-cc-ink-dim font-mono text-[12px] leading-snug italic",
        className ?? "",
      ].join(" ")}
    >
      {children}
    </p>
  );
}

// -----------------------------------------------------------------------------
// Hero code card. A real-feel GitHub-dark C# snippet with line numbers, plus
// three sketched margin arrows that point at [QueryType], the partial class
// line, and the [DataLoader] attribute.
// -----------------------------------------------------------------------------

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-4 px-5">
      <span
        className="w-6 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
};

function HeroCodeCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span
          className="bg-cc-status-firing h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-investigating h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-healthy h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span className="text-cc-ink-dim ml-3 font-mono text-[11px]">
          Catalog/Query.cs
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          C#
        </span>
      </div>
      <div className="relative py-4">
        <CodeLine n={1}>
          <span style={C.kw}>using</span>{" "}
          <span style={C.plain}>HotChocolate.Types;</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.kw}>namespace</span>{" "}
          <span style={C.plain}>Catalog;</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.comment}>{`// The C# is the schema.`}</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.punct}>[</span>
          <span style={C.attr}>QueryType</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.kw}>public partial class</span>{" "}
          <span style={C.type}>Query</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>public static async</span>{" "}
          <span style={C.type}>Task</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>Product</span>
          <span style={C.punct}>{`?>`}</span>{" "}
          <span style={C.fn}>GetProductByIdAsync</span>
          <span style={C.punct}>(</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>Guid</span> <span style={C.param}>id</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>IProductByIdDataLoader</span>{" "}
          <span style={C.param}>productById</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.punct}>{`) =>`}</span>
        </CodeLine>
        <CodeLine n={13}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.kw}>await</span>{" "}
          <span style={C.param}>productById</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>LoadAsync</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>id</span>
          <span style={C.punct}>, </span>
          <span style={C.param}>ct</span>
          <span style={C.punct}>);</span>
        </CodeLine>
        <CodeLine n={14}>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={15}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={16}>
          <span
            style={C.comment}
          >{`// Source-generated: batches Guids per request, no N+1.`}</span>
        </CodeLine>
        <CodeLine n={17}>
          <span style={C.punct}>[</span>
          <span style={C.attr}>DataLoader</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={18}>
          <span style={C.kw}>internal static async</span>{" "}
          <span style={C.type}>Task</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>IReadOnlyDictionary</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>Guid</span>
          <span style={C.punct}>, </span>
          <span style={C.type}>Product</span>
          <span style={C.punct}>{`>>`}</span>
        </CodeLine>
        <CodeLine n={19}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.fn}>GetProductsByIdAsync</span>
          <span style={C.punct}>(</span>
        </CodeLine>
        <CodeLine n={20}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>IReadOnlyList</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>Guid</span>
          <span style={C.punct}>{`>`}</span> <span style={C.param}>ids</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={21}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>CatalogDbContext</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={22}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.punct}>{`) =>`}</span>
        </CodeLine>
        <CodeLine n={23}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.kw}>await</span> <span style={C.param}>db</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Products</span>
        </CodeLine>
        <CodeLine n={24}>
          <span style={C.plain}>{`            `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Where</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>{` => `}</span>
          <span style={C.param}>ids</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Contains</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Id</span>
          <span style={C.punct}>))</span>
        </CodeLine>
        <CodeLine n={25}>
          <span style={C.plain}>{`            `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>ToDictionaryAsync</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>{` => `}</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Id</span>
          <span style={C.punct}>, </span>
          <span style={C.param}>ct</span>
          <span style={C.punct}>);</span>
        </CodeLine>
      </div>
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        <span>build: schema + resolvers + DataLoader emitted</span>
        <span className="text-cc-accent">Roslyn source generator</span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Inline diagrams. Each is drawn with a wobbly stroke and roughened by the
// page-scoped turbulence filter, so they read as pencil sketches.
// -----------------------------------------------------------------------------

interface SketchedDiagramProps {
  readonly children: ReactNode;
  readonly viewBox?: string;
  readonly ariaLabel: string;
}

function SketchedDiagram({
  children,
  viewBox = "0 0 480 220",
  ariaLabel,
}: SketchedDiagramProps) {
  return (
    <svg
      viewBox={viewBox}
      className="h-auto w-full"
      role="img"
      aria-label={ariaLabel}
    >
      <g filter="url(#hc-fj-rough)">{children}</g>
    </svg>
  );
}

/** A small reusable sketched rect with optional label. */
interface SketchRectProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly label?: string;
  readonly sub?: string;
  readonly accent?: boolean;
}

function SketchRect({ x, y, w, h, label, sub, accent }: SketchRectProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        rx="4"
        fill={accent ? "rgba(94,234,212,0.06)" : "rgba(245,241,234,0.03)"}
        stroke={accent ? "rgba(94,234,212,0.7)" : "rgba(245,241,234,0.42)"}
        strokeWidth="1.3"
      />
      {label ? (
        <text
          x={x + w / 2}
          y={sub ? y + h / 2 - 2 : y + h / 2 + 4}
          textAnchor="middle"
          fontFamily="var(--font-mono), ui-monospace, monospace"
          fontStyle="italic"
          fontSize="11"
          fill={accent ? "#5eead4" : "#f5f0ea"}
        >
          {label}
        </text>
      ) : null}
      {sub ? (
        <text
          x={x + w / 2}
          y={y + h / 2 + 12}
          textAnchor="middle"
          fontFamily="var(--font-mono), ui-monospace, monospace"
          fontStyle="italic"
          fontSize="10"
          fill="rgba(245,241,234,0.62)"
        >
          {sub}
        </text>
      ) : null}
    </g>
  );
}

function CompositionDiagram() {
  return (
    <SketchedDiagram ariaLabel="Subgraph SDLs composed at build time into a fusion plan">
      {[
        { y: 24, label: "catalog.graphql" },
        { y: 92, label: "checkout.graphql" },
        { y: 160, label: "reviews.graphql" },
      ].map((n) => (
        <g key={n.label}>
          <SketchRect x={12} y={n.y} w={148} h={36} label={n.label} />
          <path
            d={`M 162 ${n.y + 18} C 210 ${n.y + 18}, 230 110, 278 110`}
            stroke={INK}
            strokeOpacity="0.6"
            strokeWidth="1.3"
            strokeLinecap="round"
            fill="none"
          />
        </g>
      ))}
      <SketchRect
        x={280}
        y={86}
        w={100}
        h={48}
        label="fusion compose"
        sub="build time"
        accent
      />
      <path
        d="M 380 110 C 400 110, 412 110, 432 110"
        stroke={INK}
        strokeOpacity="0.75"
        strokeWidth="1.4"
        strokeLinecap="round"
        fill="none"
      />
      <path
        d="M 432 106 L 444 110 L 432 114"
        stroke={INK}
        strokeOpacity="0.75"
        strokeWidth="1.4"
        strokeLinecap="round"
        fill="none"
      />
      <text
        x="438"
        y="98"
        textAnchor="end"
        fontFamily="var(--font-mono), ui-monospace, monospace"
        fontStyle="italic"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        plan
      </text>
    </SketchedDiagram>
  );
}

function AuthoringDiagram() {
  return (
    <SketchedDiagram ariaLabel="Implementation-first and code-first both compile to one schema">
      <SketchRect
        x={12}
        y={24}
        w={180}
        h={64}
        label="Implementation-first"
        sub="[QueryType] partial class"
      />
      <SketchRect
        x={12}
        y={132}
        w={180}
        h={64}
        label="Code-first"
        sub="ObjectType<T> + descriptor"
      />
      <path
        d="M 192 56 C 240 56, 260 110, 300 110"
        stroke={INK}
        strokeOpacity="0.55"
        strokeWidth="1.3"
        strokeLinecap="round"
        fill="none"
      />
      <path
        d="M 192 164 C 240 164, 260 110, 300 110"
        stroke={INK}
        strokeOpacity="0.55"
        strokeWidth="1.3"
        strokeLinecap="round"
        fill="none"
      />
      <SketchRect
        x={300}
        y={78}
        w={160}
        h={64}
        label="One GraphQL schema"
        sub="spec-compliant SDL"
        accent
      />
    </SketchedDiagram>
  );
}

function DataLoaderDiagram() {
  const requests = [16, 38, 60, 82, 104];
  return (
    <SketchedDiagram ariaLabel="Per-field requests collapsed into one batched LoadAsync call">
      {requests.map((y, i) => (
        <g key={y}>
          <SketchRect
            x={12}
            y={y}
            w={120}
            h={22}
            label={`product(id: ${i + 1})`}
          />
          <path
            d={`M 132 ${y + 11} C 200 ${y + 11}, 200 110, 268 110`}
            stroke={INK}
            strokeOpacity="0.5"
            strokeWidth="1.2"
            strokeLinecap="round"
            fill="none"
          />
        </g>
      ))}
      <SketchRect
        x={268}
        y={92}
        w={120}
        h={40}
        label="LoadAsync(ids)"
        sub="one batched call"
        accent
      />
      <path
        d="M 388 112 C 412 112, 420 112, 432 112"
        stroke={INK}
        strokeOpacity="0.7"
        strokeWidth="1.3"
        strokeLinecap="round"
        fill="none"
      />
      <path
        d="M 432 108 L 444 112 L 432 116"
        stroke={INK}
        strokeOpacity="0.75"
        strokeWidth="1.3"
        strokeLinecap="round"
        fill="none"
      />
      <text
        x="438"
        y="100"
        textAnchor="end"
        fontFamily="var(--font-mono), ui-monospace, monospace"
        fontStyle="italic"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        db
      </text>
    </SketchedDiagram>
  );
}

function SubscriptionsDiagram() {
  return (
    <SketchedDiagram ariaLabel="Pub/sub fan-out via [SubscriptionType] to WebSocket and SSE clients">
      <SketchRect
        x={12}
        y={88}
        w={130}
        h={48}
        label="ITopicEventSender"
        sub="Redis / NATS / PG"
      />
      <path
        d="M 142 112 C 170 112, 196 112, 220 112"
        stroke={INK}
        strokeOpacity="0.65"
        strokeWidth="1.3"
        strokeLinecap="round"
        fill="none"
      />
      <SketchRect
        x={220}
        y={88}
        w={120}
        h={48}
        label="[SubscriptionType]"
        sub="dynamic topics"
        accent
      />
      {[64, 112, 160].map((y, i) => (
        <g key={y}>
          <path
            d={`M 340 112 C 380 112, 380 ${y}, 420 ${y}`}
            stroke={INK}
            strokeOpacity="0.55"
            strokeWidth="1.3"
            strokeLinecap="round"
            fill="none"
          />
          <SketchRect
            x={420}
            y={y - 12}
            w={48}
            h={24}
            label={i === 0 ? "ws" : i === 1 ? "sse" : "ws"}
          />
        </g>
      ))}
    </SketchedDiagram>
  );
}

function OtelDiagram() {
  const spans = [
    { y: 28, x: 12, w: 380, label: "graphql.request" },
    { y: 56, x: 24, w: 320, label: "graphql.execute" },
    { y: 84, x: 40, w: 200, label: "graphql.parse + validate" },
    { y: 112, x: 250, w: 90, label: "resolve product" },
    { y: 140, x: 270, w: 60, label: "dataloader.batch" },
    { y: 168, x: 340, w: 28, label: "db" },
  ];
  return (
    <SketchedDiagram ariaLabel="Trace waterfall with server, execution and dataloader spans">
      <text
        x="12"
        y="18"
        fontFamily="var(--font-mono), ui-monospace, monospace"
        fontStyle="italic"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        trace_id 7f8a...
      </text>
      {spans.map((s, i) => (
        <g key={s.label}>
          <rect
            x={s.x}
            y={s.y}
            width={s.w}
            height="14"
            rx="3"
            fill={i < 2 ? "rgba(94,234,212,0.18)" : "rgba(94,234,212,0.1)"}
            stroke="rgba(94,234,212,0.55)"
            strokeWidth="1.1"
          />
          <text
            x={s.x + s.w + 6}
            y={s.y + 11}
            fontFamily="var(--font-mono), ui-monospace, monospace"
            fontStyle="italic"
            fontSize="10"
            fill="rgba(245,241,234,0.7)"
          >
            {s.label}
          </text>
        </g>
      ))}
      {/* Bracket around the dataloader.batch span. */}
      <path
        d="M 264 138 L 260 138 L 260 156 L 264 156"
        stroke={INK}
        strokeOpacity="0.8"
        strokeWidth="1.3"
        strokeLinecap="round"
        fill="none"
      />
      <path
        d="M 336 138 L 340 138 L 340 156 L 336 156"
        stroke={INK}
        strokeOpacity="0.8"
        strokeWidth="1.3"
        strokeLinecap="round"
        fill="none"
      />
      <text
        x="300"
        y="200"
        textAnchor="middle"
        fontFamily="var(--font-mono), ui-monospace, monospace"
        fontStyle="italic"
        fontSize="10"
        fill={INK}
        fillOpacity="0.85"
      >
        three diagnostic layers
      </text>
    </SketchedDiagram>
  );
}

function FederationDiagram() {
  return (
    <SketchedDiagram ariaLabel="Same Hot Chocolate server runs standalone, as a Fusion subgraph, or as an Apollo Federation subgraph">
      <SketchRect
        x={12}
        y={86}
        w={160}
        h={48}
        label="Hot Chocolate server"
        sub="same resolvers"
        accent
      />
      {[
        { y: 18, label: "single API", sub: "standalone" },
        { y: 86, label: "Fusion gateway", sub: "compile-time plan" },
        { y: 154, label: "Apollo Federation", sub: "spec subgraph" },
      ].map((g) => (
        <g key={g.label}>
          <SketchRect
            x={300}
            y={g.y}
            w={170}
            h={48}
            label={g.label}
            sub={g.sub}
          />
          <path
            d={`M 172 110 C 220 110, 250 ${g.y + 24}, 300 ${g.y + 24}`}
            stroke={INK}
            strokeOpacity="0.5"
            strokeWidth="1.3"
            strokeLinecap="round"
            fill="none"
          />
        </g>
      ))}
    </SketchedDiagram>
  );
}

// -----------------------------------------------------------------------------
// Page wrapper. Provides the ruled-paper background as a CSS background-image
// (inline data URI SVG), the left vertical "margin rule", and a max-w-5xl
// content column.
// -----------------------------------------------------------------------------

// Pale horizontal hairlines every 28px, alpha ~0.04. Plain inline SVG via data URI.
const RULED_PAPER_BG =
  "url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='28' height='28'><line x1='0' y1='27.5' x2='28' y2='27.5' stroke='%23f5f1ea' stroke-opacity='0.04' stroke-width='1'/></svg>\")";

interface PageShellProps {
  readonly children: ReactNode;
}

function PageShell({ children }: PageShellProps) {
  const reduce = useReducedMotion();
  return (
    <div className="relative isolate">
      {/* Ruled paper. Fixed so it follows the viewport but stays subtle. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: RULED_PAPER_BG,
          backgroundSize: "28px 28px",
        }}
      />
      {/* The vertical wobbly margin rule, drawn down the left edge of the
          content column. Sized to the document height via 100% on the parent. */}
      <div className="pointer-events-none absolute inset-y-0 left-0 -z-10 flex w-full justify-center">
        <div className="mx-auto h-full w-full max-w-5xl">
          <svg
            preserveAspectRatio="none"
            viewBox="0 0 4 1000"
            className="ml-1 h-full w-[3px]"
            aria-hidden
          >
            <motion.path
              d="M 2 0 C 1 200, 3 400, 2 600 S 1 900, 2 1000"
              stroke={INK}
              strokeOpacity="0.18"
              strokeWidth="1"
              strokeLinecap="round"
              fill="none"
              filter="url(#hc-fj-rough)"
              initial={reduce ? false : { pathLength: 0 }}
              whileInView={reduce ? undefined : { pathLength: 1 }}
              viewport={{ once: true }}
              transition={{ duration: 1.2, ease: "easeOut" }}
            />
          </svg>
        </div>
      </div>
      <div className="mx-auto max-w-5xl">{children}</div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Entry shell. Three-column grid: margin note (left), prose + visual (center),
// date stamp (right). Reused by every numbered entry.
// -----------------------------------------------------------------------------

interface EntryProps {
  readonly id: string;
  readonly entry: string;
  readonly date: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly children: ReactNode;
  readonly marginNote: ReactNode;
}

function Entry({
  id,
  entry,
  date,
  eyebrow,
  title,
  children,
  marginNote,
}: EntryProps) {
  return (
    <section
      id={id}
      className="scroll-mt-24 py-16 sm:py-20"
      aria-labelledby={`${id}-title`}
    >
      <div className="grid gap-6 lg:grid-cols-12 lg:gap-8">
        <aside className="lg:col-span-3">
          <div className="lg:sticky lg:top-24">{marginNote}</div>
        </aside>
        <div className="lg:col-span-7">
          <div className="flex items-center gap-3">
            <Eyebrow>{eyebrow}</Eyebrow>
          </div>
          <h2
            id={`${id}-title`}
            className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl"
          >
            {title}
          </h2>
          <div className="mt-6">{children}</div>
        </div>
        <aside className="flex justify-end lg:col-span-2">
          <DateStamp date={date} entry={entry} />
        </aside>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// The page itself.
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <>
      <RoughDefs />
      <PageShell>
        {/* ================================================================
            PAGE 01 (HERO): full-width entry, page number "01", title with a
            sketched circle around "C# is the schema", margin arrow pointing
            into the circle from a "note to self" annotation.
            ================================================================ */}
        <section className="pt-12 pb-10 sm:pt-20 sm:pb-14" aria-label="Hero">
          <div className="grid gap-6 lg:grid-cols-12 lg:gap-8">
            <aside className="lg:col-span-3">
              <div className="relative">
                <MarginNote>note to self</MarginNote>
                <p className="text-cc-ink-dim mt-1 font-mono text-[12px] italic">
                  the headline claim
                </p>
                {/* Arrow from this margin label into the circled phrase. */}
                <SketchArrow
                  className="absolute top-6 right-0 hidden h-16 w-40 lg:block"
                  viewBox="0 0 200 80"
                  d="M 10 20 C 60 24, 110 50, 180 64"
                  headD="M 170 56 L 184 66 L 168 72"
                  delay={0.4}
                />
              </div>
            </aside>
            <div className="lg:col-span-7">
              <Eyebrow>GraphQL server for .NET</Eyebrow>
              <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
                Your{" "}
                <span className="relative inline-block px-2">
                  <span className="relative z-10">C# is the schema</span>
                  <SketchedCircle
                    className="absolute inset-x-0 -inset-y-2 z-0 h-[calc(100%+1rem)] w-full"
                    breathing
                  />
                </span>
                .
              </h1>
              <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
                Hot Chocolate is the open-source GraphQL server for .NET.
                Annotate a partial class, write idiomatic C# resolvers, and a
                Roslyn source generator emits the schema, the resolver pipeline,
                and DataLoader infrastructure at build time. One server speaks
                HTTP, WebSocket, and Server-Sent Events, and the same code can
                run standalone or as a Fusion subgraph later.
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
                <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                  View on GitHub
                </OutlineButton>
              </div>
            </div>
            <aside className="flex justify-end lg:col-span-2">
              <DateStamp entry="Page 01" date="2026-06-23" />
            </aside>
          </div>

          {/* Hero code card with three sketched margin arrows pointing at
              [QueryType], the partial class line, and [DataLoader]. */}
          <div className="mt-12 grid gap-4 lg:grid-cols-12 lg:gap-6">
            <aside className="relative lg:col-span-3">
              {/* Three captions, vertically stacked, each with a small arrow
                  pointing into the corresponding code line. */}
              <div className="flex h-full flex-col justify-between gap-6 pt-4 pb-4 text-right lg:pr-0">
                <div className="relative">
                  <MarginNote className="text-right">
                    source-generated
                  </MarginNote>
                  <SketchArrow
                    className="absolute top-3 right-[-16px] hidden h-10 w-28 lg:block"
                    viewBox="0 0 140 60"
                    d="M 4 10 C 50 20, 90 38, 134 46"
                    headD="M 122 38 L 138 48 L 122 54"
                    delay={0.1}
                  />
                </div>
                <div className="relative">
                  <MarginNote className="text-right">
                    one type, one schema
                  </MarginNote>
                  <SketchArrow
                    className="absolute top-3 right-[-16px] hidden h-10 w-28 lg:block"
                    viewBox="0 0 140 60"
                    d="M 4 16 C 50 26, 90 30, 134 30"
                    headD="M 122 22 L 138 30 L 122 38"
                    delay={0.2}
                  />
                </div>
                <div className="relative">
                  <MarginNote className="text-right">
                    no N+1, built-in
                  </MarginNote>
                  <SketchArrow
                    className="absolute top-3 right-[-16px] hidden h-10 w-28 lg:block"
                    viewBox="0 0 140 60"
                    d="M 4 26 C 50 30, 90 26, 134 14"
                    headD="M 122 8 L 138 12 L 122 22"
                    delay={0.3}
                  />
                </div>
              </div>
            </aside>
            <div className="lg:col-span-9">
              <HeroCodeCard />
            </div>
          </div>

          <dl className="mt-10 grid grid-cols-3 gap-6 pt-6">
            <SketchedRule className="col-span-3 -mt-6 mb-2 h-3 w-full" />
            <div>
              <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                License
              </dt>
              <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
            </div>
            <div>
              <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                Runtime
              </dt>
              <dd className="text-cc-ink mt-1 text-sm">ASP.NET Core</dd>
            </div>
            <div>
              <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                Spec
              </dt>
              <dd className="text-cc-ink mt-1 text-sm">GraphQL 2025</dd>
            </div>
          </dl>
        </section>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            PAGE 02 (Field Notes): a fieldwork inventory of six observations
            in two columns. Each is prefixed with a sketched check + a
            pencilled roman numeral.
            ================================================================ */}
        <section
          id="field-notes"
          aria-labelledby="field-notes-title"
          className="scroll-mt-24 py-16 sm:py-20"
        >
          <div className="grid gap-6 lg:grid-cols-12 lg:gap-8">
            <aside className="lg:col-span-3">
              <MarginNote>field notes</MarginNote>
              <p className="text-cc-ink-dim mt-1 font-mono text-[12px] italic">
                what you actually get on day one
              </p>
            </aside>
            <div className="lg:col-span-7">
              <Eyebrow>Field notes</Eyebrow>
              <h2
                id="field-notes-title"
                className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl"
              >
                Six observations from the working notebook.
              </h2>
              <ul className="mt-8 grid grid-cols-1 gap-x-8 gap-y-5 sm:grid-cols-2">
                {[
                  ["i.", "Compile-time composition with Fusion"],
                  ["ii.", "Code-first or implementation-first authoring"],
                  ["iii.", "DataLoader batching, source-generated"],
                  ["iv.", "Subscriptions over WebSocket and SSE"],
                  ["v.", "OpenTelemetry, vendor-neutral"],
                  ["vi.", "Fusion and Apollo Federation ready"],
                ].map(([numeral, label]) => (
                  <li
                    key={label}
                    className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
                  >
                    <span
                      className="text-cc-ink-dim shrink-0 pt-0.5 font-mono text-[11px] italic tabular-nums"
                      aria-hidden
                    >
                      {numeral}
                    </span>
                    <span className="shrink-0 pt-0.5">
                      <SketchedCheck />
                    </span>
                    <span>{label}</span>
                  </li>
                ))}
              </ul>
            </div>
            <aside className="flex justify-end lg:col-span-2">
              <DateStamp entry="Page 02" date="2026-06-23" />
            </aside>
          </div>
        </section>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            ENTRY 03: Composition
            ================================================================ */}
        <Entry
          id="composition"
          entry="Entry 03"
          date="2026-06-23"
          eyebrow="Composition"
          title="When subgraphs talk in CI, prod stops surprising you."
          marginNote={
            <div className="relative">
              <MarginNote>planned at build time, not at runtime</MarginNote>
              <SketchArrow
                className="absolute top-6 right-[-12px] hidden h-12 w-32 lg:block"
                viewBox="0 0 160 80"
                d="M 10 14 C 60 30, 100 50, 154 64"
                headD="M 142 56 L 158 66 L 142 72"
              />
            </div>
          }
        >
          <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
            Fusion plans composition once, in CI, against the source SDLs. The
            gateway loads a finished query plan and stays cheap to run at the
            edge. Schema changes show up as planning errors before they show up
            as production incidents.
          </p>
          <div className="border-cc-card-border bg-cc-card-bg mt-6 rounded-xl border p-5 sm:p-6">
            <CompositionDiagram />
          </div>
          <ul className="mt-6 flex flex-col gap-2.5">
            {[
              "Compose any mix of Hot Chocolate subgraphs into a single planned gateway schema.",
              "Resolver paths stay typed end-to-end across the gateway.",
              "Standalone today, Fusion subgraph tomorrow, no resolver rewrites.",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="shrink-0 pt-0.5">
                  <SketchedCheck />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </Entry>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            ENTRY 04: Authoring
            ================================================================ */}
        <Entry
          id="authoring"
          entry="Entry 04"
          date="2026-06-23"
          eyebrow="Authoring"
          title="Two paths, one schema."
          marginNote={
            <div className="relative">
              <MarginNote>
                I usually start with [QueryType] and only drop to
                ObjectType&lt;T&gt; when the model and schema disagree.
              </MarginNote>
            </div>
          }
        >
          <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
            Implementation-first is the default: annotate a partial class with{" "}
            [QueryType] and the Roslyn source generator emits the schema and
            resolver pipelines from your C#. When the model and the schema need
            to diverge, drop into the fluent ObjectType&lt;T&gt; descriptor and
            mix both in the same project.
          </p>
          <div className="border-cc-card-border bg-cc-card-bg mt-6 rounded-xl border p-5 sm:p-6">
            <AuthoringDiagram />
          </div>
          <ul className="mt-6 flex flex-col gap-2.5">
            {[
              "Resolvers are plain C# methods with DI-injected services and CancellationToken.",
              "XML doc comments become GraphQL descriptions, refactors stay safe with nameof.",
              "dotnet new graphql gets a running server in minutes.",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="shrink-0 pt-0.5">
                  <SketchedCheck />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </Entry>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            ENTRY 05: DataLoader
            ================================================================ */}
        <Entry
          id="dataloader"
          entry="Entry 05"
          date="2026-06-23"
          eyebrow="DataLoader"
          title="N+1 disappears at the field level."
          marginNote={
            <div className="relative">
              <MarginNote>this is the win</MarginNote>
              <SketchArrow
                className="absolute top-6 right-[-12px] hidden h-12 w-32 lg:block"
                viewBox="0 0 160 80"
                d="M 10 18 C 60 26, 100 44, 154 60"
                headD="M 142 52 L 158 62 L 142 68"
              />
            </div>
          }
        >
          <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
            Green Donut is built into Hot Chocolate. Annotate a static method
            with [DataLoader] and the generator emits the loader class, the
            interface, and the DI registration. Per-request keys are
            deduplicated, the execution engine resolves fields in waves, and
            every batch dispatches together.
          </p>
          <div className="border-cc-card-border bg-cc-card-bg relative mt-6 rounded-xl border p-5 sm:p-6">
            <DataLoaderDiagram />
            {/* A hand-drawn circle around the "one batched call" label,
                positioned over the LoadAsync box in the diagram. */}
            <div className="pointer-events-none absolute top-[55%] left-[55%] hidden h-16 w-36 -translate-x-1/2 -translate-y-1/2 sm:block">
              <SketchedCircle className="h-full w-full" />
            </div>
          </div>
          <ul className="mt-6 flex flex-col gap-2.5">
            {[
              "Batch (one-to-one) and group (one-to-many) loaders, per-request caching.",
              "Works against Entity Framework Core, MongoDB, Marten, Raven, or any IQueryable.",
              "Projections push the selection set down to native database queries.",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="shrink-0 pt-0.5">
                  <SketchedCheck />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </Entry>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            ENTRY 06: Realtime
            ================================================================ */}
        <Entry
          id="subscriptions"
          entry="Entry 06"
          date="2026-06-23"
          eyebrow="Realtime"
          title="Subscriptions over WebSocket and Server-Sent Events."
          marginNote={
            <div className="relative">
              <MarginNote>
                pub/sub: Redis, NATS, Postgres LISTEN/NOTIFY, or RabbitMQ.
              </MarginNote>
            </div>
          }
        >
          <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
            [SubscriptionType] with [Topic] placeholders gives you dynamic
            per-resource streams. Pick a transport: modern graphql-ws or
            graphql-sse for HTTP/2 and proxy-friendly delivery. Pick a pub/sub
            provider: in-memory for dev, Redis, NATS, Postgres LISTEN/NOTIFY, or
            RabbitMQ for production.
          </p>
          <div className="border-cc-card-border bg-cc-card-bg mt-6 rounded-xl border p-5 sm:p-6">
            <SubscriptionsDiagram />
          </div>
          <ul className="mt-6 flex flex-col gap-2.5">
            {[
              "Dynamic topics derived from arguments via [Topic], or your own subscribe resolver.",
              "Provider-agnostic publishing via ITopicEventSender from any service.",
              "@defer and @stream stream partial responses on the same connection.",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="shrink-0 pt-0.5">
                  <SketchedCheck />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </Entry>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            ENTRY 07: Observability
            ================================================================ */}
        <Entry
          id="otel"
          entry="Entry 07"
          date="2026-06-23"
          eyebrow="Observability"
          title="OpenTelemetry, configured once."
          marginNote={
            <div className="relative">
              <MarginNote>
                wire AddInstrumentation() + AddHotChocolateInstrumentation(),
                point at any OTLP endpoint.
              </MarginNote>
            </div>
          }
        >
          <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
            Hot Chocolate plugs into the proposed GraphQL OTel semantic
            conventions. Spans carry operation type, document hash, trusted
            document id, per-field selection, and DataLoader batch size.
            Configure an OTLP exporter and the traces land in whatever backend
            you already run.
          </p>
          <div className="border-cc-card-border bg-cc-card-bg mt-6 rounded-xl border p-5 sm:p-6">
            <OtelDiagram />
          </div>
          <ul className="mt-6 flex flex-col gap-2.5">
            {[
              "Three diagnostic layers: server transport, execution pipeline, DataLoader.",
              "Low-cardinality root span names by design, ActivityEnricher for custom data.",
              "Works with Jaeger, Tempo, Datadog, Honeycomb, or any OTLP endpoint.",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="shrink-0 pt-0.5">
                  <SketchedCheck />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </Entry>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            ENTRY 08: Federation
            ================================================================ */}
        <Entry
          id="federation"
          entry="Entry 08"
          date="2026-06-23"
          eyebrow="Federation"
          title="Same server, three ways."
          marginNote={
            <div className="relative">
              <MarginNote>
                the gateway is always self-run, your infra, your call.
              </MarginNote>
            </div>
          }
        >
          <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
            The same Hot Chocolate server runs three ways. As a single API. As a
            Fusion subgraph composed at build time into a planned gateway
            schema. As an Apollo Federation subgraph for teams already in that
            ecosystem. The resolvers do not change. The choice is operational,
            not architectural.
          </p>
          <div className="border-cc-card-border bg-cc-card-bg mt-6 rounded-xl border p-5 sm:p-6">
            <FederationDiagram />
          </div>
          <ul className="mt-6 flex flex-col gap-2.5">
            {[
              "Start with one server, add Fusion only when you actually need to.",
              "Apollo Federation spec implemented via the ApolloFederation package.",
              "Cost analysis (@cost, @listSize) and trusted operations apply at every tier.",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="shrink-0 pt-0.5">
                  <SketchedCheck />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </Entry>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            ENTRY 09: IDE (Nitro embed)
            ================================================================ */}
        <section
          id="nitro"
          aria-labelledby="nitro-title"
          className="scroll-mt-24 py-16 sm:py-20"
        >
          <div className="grid gap-6 lg:grid-cols-12 lg:gap-8">
            <aside className="lg:col-span-3">
              <MarginNote>served from the endpoint at /graphql</MarginNote>
            </aside>
            <div className="lg:col-span-7">
              <Eyebrow>IDE</Eyebrow>
              <h2
                id="nitro-title"
                className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl"
              >
                A GraphQL IDE ships with every server.
              </h2>
              <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
                Run your server and the Nitro GraphQL IDE is served from the
                endpoint. Browse the schema, draft operations against your live
                resolvers, inspect responses, and share documents with the rest
                of the team.
              </p>
            </div>
            <aside className="flex justify-end lg:col-span-2">
              <DateStamp entry="Page 09" date="2026-06-23" />
            </aside>
          </div>

          {/* Nitro embed inside a sketched frame. The frame is a wobbly rect
              outline behind the actual card. */}
          <div className="relative mt-10">
            <svg
              aria-hidden
              viewBox="0 0 1000 600"
              preserveAspectRatio="none"
              className="pointer-events-none absolute -inset-2"
            >
              <rect
                x="6"
                y="6"
                width="988"
                height="588"
                rx="12"
                fill="none"
                stroke={INK}
                strokeOpacity="0.35"
                strokeWidth="1.4"
                filter="url(#hc-fj-rough-strong)"
              />
            </svg>
            <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-xl border">
              <NitroCompose />
            </div>
          </div>
        </section>

        <SketchedRule className="h-3 w-full" />

        {/* ================================================================
            BACK COVER (Closing CTA): centred, sketched dotted signature line,
            and the lone brand-spectrum band as the very last sketched
            underline of the entire journal.
            ================================================================ */}
        <section
          aria-label="Get started"
          className="relative scroll-mt-24 py-20 sm:py-28"
        >
          <div className="text-center">
            <Eyebrow>Get started</Eyebrow>
            <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
              Ship a GraphQL API your .NET team can actually own.
            </h2>
            <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
              A C# project, a partial class, a few attributes. The schema, the
              DataLoaders, and the resolver pipeline are generated for you at
              build time, and the runtime is the ASP.NET Core you already run.
            </p>
            <div className="mt-8 flex flex-wrap justify-center gap-3">
              <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
            <DottedSignatureRule className="mx-auto mt-10 h-3 w-full max-w-md" />
            <p className="text-cc-ink-dim mt-2 font-mono text-[11px] tracking-widest uppercase">
              signed, the field journal
            </p>
          </div>

          {/* The single brand-spectrum band of the entire page, rendered as
              the very last sketched underline at the bottom of the journal. */}
          <div className="relative mt-16">
            <div
              aria-hidden
              className="absolute inset-x-0 top-1/2 h-[2px] -translate-y-1/2 rounded-full"
              style={{ background: SPECTRUM, opacity: 0.85 }}
            />
            <svg
              aria-hidden
              viewBox="0 0 1000 14"
              preserveAspectRatio="none"
              className="relative h-3 w-full"
            >
              <path
                d="M 2 7 C 60 3, 140 11, 220 5 S 360 11, 460 6 S 620 10, 720 5 S 880 11, 998 7"
                stroke="rgba(11,15,26,0.85)"
                strokeWidth="6"
                strokeLinecap="round"
                fill="none"
                filter="url(#hc-fj-rough)"
              />
            </svg>
          </div>
        </section>
      </PageShell>
    </>
  );
}
