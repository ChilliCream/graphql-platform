"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { motion, useInView, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// ---------------------------------------------------------------------------
// Data (verbatim from v1)
// ---------------------------------------------------------------------------

type Level = "Foundations" | "Core" | "Advanced" | "Production";

interface CurriculumTrack {
  readonly code: string;
  readonly title: string;
  readonly level: Level;
  readonly summary: string;
  readonly topics: readonly string[];
}

const CURRICULUM: readonly CurriculumTrack[] = [
  {
    code: "GQL-101",
    title: "GraphQL Fundamentals",
    level: "Foundations",
    summary:
      "The mental model: types, fields, resolvers, and how a query maps onto your data.",
    topics: [
      "Schema, types, and the request lifecycle",
      "Queries, mutations, subscriptions",
      "Fragments, variables, directives",
      "Error shapes, nullability, and pagination patterns",
    ],
  },
  {
    code: "HC-201",
    title: "Hot Chocolate Server",
    level: "Core",
    summary:
      "Build a production GraphQL server on ASP.NET Core with Hot Chocolate, end to end.",
    topics: [
      "Code-first types, resolvers, and DI",
      "Data loaders, projections, and EF Core integration",
      "Authorization, filtering, sorting, paging",
      "Testing, instrumentation, and configuration",
    ],
  },
  {
    code: "FUS-301",
    title: "Fusion Federation",
    level: "Advanced",
    summary:
      "Compose multiple Hot Chocolate services into one Fusion graph without leaking the seams.",
    topics: [
      "Subgraph design and ownership boundaries",
      "Composition, source schemas, and lookup keys",
      "Cross-subgraph entities and shared types",
      "Gateway configuration and rollout strategy",
    ],
  },
  {
    code: "NIT-401",
    title: "Production Observability with Nitro",
    level: "Production",
    summary:
      "Wire Hot Chocolate and Fusion into Nitro to see what production is actually doing.",
    topics: [
      "Schema registry and published client tracking",
      "Operation telemetry and slow-resolver triage",
      "Persisted operations and safe schema evolution",
      "Reading traces when an incident is live",
    ],
  },
  {
    code: "REL-301",
    title: "React + Relay Client",
    level: "Advanced",
    summary:
      "Drive a React UI with Relay so data flows match component boundaries by default.",
    topics: [
      "Fragments, connections, and colocation",
      "Mutations, optimistic updates, and store consistency",
      "Suspense, streaming, and refetch patterns",
      "Working with persisted queries from Nitro",
    ],
  },
  {
    code: "DSN-301",
    title: "Schema Design & Evolution",
    level: "Advanced",
    summary:
      "Design a schema your team can change safely once real clients depend on it.",
    topics: [
      "Naming, nullability, and shaping for change",
      "Errors as data, mutations, and result unions",
      "Versionless evolution and deprecation",
      "Reviewing diffs against published clients",
    ],
  },
];

interface DeliveryFormat {
  readonly name: "On-site" | "Remote" | "Hybrid";
  readonly tagline: string;
  readonly notes: readonly string[];
}

const FORMATS: readonly DeliveryFormat[] = [
  {
    name: "On-site",
    tagline: "We come to you.",
    notes: [
      "Instructor on location with your team",
      "Best for hands-on labs and whiteboard design",
      "Travel quoted with the engagement",
    ],
  },
  {
    name: "Remote",
    tagline: "Live, distributed cohorts.",
    notes: [
      "Live sessions across time zones",
      "Shared repo, recordings, and Q&A channel",
      "Easiest to schedule across multiple offices",
    ],
  },
  {
    name: "Hybrid",
    tagline: "Half in the room, half on Zoom.",
    notes: [
      "Anchor cohort in one location, others dial in",
      "Workshops can splice on-site labs with remote review",
      "Useful when seniors are co-located and juniors are not",
    ],
  },
];

interface OfferPerk {
  readonly text: string;
}

interface Offer {
  readonly name: "Corporate Training" | "Corporate Workshop";
  readonly tagline: string;
  readonly description: string;
  readonly perks: readonly OfferPerk[];
  readonly highlight?: boolean;
}

const OFFERS: readonly Offer[] = [
  {
    name: "Corporate Training",
    tagline: "Flexible curriculum, shaped to your team.",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      { text: "Level up their proficiency" },
      { text: "Catered to different skills" },
      { text: "Overcome challenges they have been wrestling with" },
      { text: "Get everybody on the same technical page" },
    ],
  },
  {
    name: "Corporate Workshop",
    tagline: "Focused, hands-on, project-shaped.",
    description:
      "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
    perks: [
      { text: "Core concepts and advanced" },
      { text: "Deepen knowledge of GraphQL API" },
      { text: "Work on a real project" },
      { text: "Scale and production quirks" },
      { text: "Level up your entire team at once" },
      { text: "Have Lots of Fun!" },
    ],
    highlight: true,
  },
];

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQ: readonly FaqItem[] = [
  {
    q: "How long is a typical engagement?",
    a: "Most workshops run two to five days. A Corporate Training that spans several tracks is usually split into multiple weeks so people keep shipping in between. We size duration to the topics you pick and the seniority of the room, then put it in writing before we start.",
  },
  {
    q: "How many people can attend?",
    a: "We have run sessions from a single team of five up to a few dozen engineers across offices. For workshops with live labs we keep cohorts small enough that everyone gets feedback. For larger groups we either split into cohorts or lean on the lecture-and-clinic format.",
  },
  {
    q: "What do attendees need to know beforehand?",
    a: "For the Hot Chocolate and Fusion tracks, comfort with C# and ASP.NET Core. For the Relay track, comfort with React and TypeScript. No prior GraphQL is required for the foundations track. We ask a few questions before we start and adjust the depth of each module to the room.",
  },
  {
    q: "How is pricing handled?",
    a: "Pricing is on request because the right number depends on tracks, duration, headcount, format, and travel. Tell us what you want covered and we send back a written quote.",
  },
  {
    q: "How soon can we book?",
    a: "Lead time is typically a few weeks so we can tailor the curriculum and line up the instructor. Urgent engagements are possible when we have a slot open. Get in touch early if your delivery date is fixed.",
  },
  {
    q: "Do you sign NDAs and work on our code?",
    a: "Yes. We routinely sign NDAs and tailor workshop projects against your own schema or service. If you would rather keep the workshop on a neutral codebase we bring one.",
  },
];

const TRAINING_MAILTO = "mailto:contact@chillicream.com?subject=Training";

// ---------------------------------------------------------------------------
// Graph geometry
// ---------------------------------------------------------------------------

interface GraphNode {
  readonly code: string;
  readonly title: string;
  readonly level: Level;
  readonly x: number;
  readonly y: number;
}

const VIEW_W = 880;
const VIEW_H = 440;

// Directed graph layout for the six tracks.
// Foundations to Core to Advanced (FUS, DSN) to Production (NIT). REL hangs off DSN.
const GRAPH_NODES: readonly GraphNode[] = [
  {
    code: "GQL-101",
    title: "GraphQL Fundamentals",
    level: "Foundations",
    x: 90,
    y: 220,
  },
  {
    code: "HC-201",
    title: "Hot Chocolate Server",
    level: "Core",
    x: 290,
    y: 220,
  },
  {
    code: "FUS-301",
    title: "Fusion Federation",
    level: "Advanced",
    x: 510,
    y: 110,
  },
  {
    code: "DSN-301",
    title: "Schema Design & Evolution",
    level: "Advanced",
    x: 510,
    y: 330,
  },
  {
    code: "NIT-401",
    title: "Production Observability with Nitro",
    level: "Production",
    x: 740,
    y: 220,
  },
  {
    code: "REL-301",
    title: "React + Relay Client",
    level: "Advanced",
    x: 740,
    y: 380,
  },
];

interface GraphEdge {
  readonly from: string;
  readonly to: string;
  // sequencing index controlling the draw order on enter view
  readonly order: number;
}

// Dependency-order draw sequence. Each edge animates in its own slot.
const GRAPH_EDGES: readonly GraphEdge[] = [
  { from: "GQL-101", to: "HC-201", order: 0 },
  { from: "HC-201", to: "FUS-301", order: 1 },
  { from: "HC-201", to: "DSN-301", order: 1 },
  { from: "FUS-301", to: "NIT-401", order: 2 },
  { from: "DSN-301", to: "NIT-401", order: 2 },
  { from: "DSN-301", to: "REL-301", order: 3 },
];

// Per-edge timings (seconds), derived from the dependency order.
const EDGE_STEP_DELAY = 0.18;
const EDGE_DRAW_DURATION = 0.55;
const NODE_STEP_DELAY = 0.08;
const NODE_DRAW_DURATION = 0.35;
const LEVEL_TICK_INTERVAL = 0.09;
// Pulse starts after the last edge finishes drawing.
const PULSE_START_DELAY =
  (Math.max(...GRAPH_EDGES.map((e) => e.order)) + 1) * EDGE_STEP_DELAY +
  EDGE_DRAW_DURATION * 0.5;
const PULSE_SEGMENT_DURATION = 0.6;

// The accent pulse glides along this representative path.
// Foundations to Core to Advanced (DSN) to Production (NIT).
const PULSE_PATH: readonly string[] = [
  "GQL-101",
  "HC-201",
  "DSN-301",
  "NIT-401",
];

// Single-accent visual encoding: levels distinguish by stroke weight and dash.
// All chips, edges, nodes, and pulses use var(--color-cc-accent).
interface LevelStyle {
  readonly strokeWidth: number;
  readonly strokeOpacity: number;
  readonly chipOpacity: number;
  readonly dashArray?: string;
}

const LEVEL_STYLE: Record<Level, LevelStyle> = {
  Foundations: { strokeWidth: 1, strokeOpacity: 0.55, chipOpacity: 0.55 },
  Core: { strokeWidth: 1.5, strokeOpacity: 0.75, chipOpacity: 0.75 },
  Advanced: { strokeWidth: 1.5, strokeOpacity: 1, chipOpacity: 1 },
  Production: {
    strokeWidth: 2,
    strokeOpacity: 1,
    chipOpacity: 1,
    dashArray: "4 3",
  },
};

function nodeByCode(code: string): GraphNode {
  const found = GRAPH_NODES.find((n) => n.code === code);
  if (!found) {
    throw new Error(`Unknown node ${code}`);
  }
  return found;
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function TrainingPreviewV7Client() {
  return (
    <>
      <CatalogHero />
      <LearningPathCenterpiece />
      <CurriculumDetail />
      <DeliveryFormats />
      <OffersSection />
      <InstructorsBand />
      <FaqSection />
      <ContactBand />
    </>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function CatalogHero() {
  return (
    <section className="py-20 sm:py-24">
      <div className="grid gap-10 lg:grid-cols-[1.3fr_1fr] lg:items-center">
        <div>
          <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-widest uppercase">
            Training catalogue
          </div>
          <h1 className="font-heading text-cc-heading text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
            Six tracks. One graph your team can walk.
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-base sm:text-lg">
            A catalogue of GraphQL training workshops built around Hot
            Chocolate, ASP.NET Core, React, and Relay. The tracks connect into a
            learning path your team walks from Foundations to Production,
            delivered as Corporate Training or a focused Corporate Workshop.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="#learning-path">See the path</SolidButton>
            <OutlineButton href={TRAINING_MAILTO}>Talk to us</OutlineButton>
          </div>
        </div>
        <HeroConstellation />
      </div>
    </section>
  );
}

function HeroConstellation() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });
  const reduce = useReducedMotion();
  const animate = inView && !reduce;

  // Compact mini layout reusing the main graph topology.
  const scale = 0.42;
  const offsetY = 18;
  const mini = (code: string) => {
    const n = nodeByCode(code);
    return { x: n.x * scale + 16, y: n.y * scale + offsetY };
  };

  return (
    <aside
      aria-label="Curriculum constellation preview"
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-5 sm:p-6"
    >
      <div className="text-cc-nav-label mb-3 font-mono text-[11px] font-semibold tracking-widest uppercase">
        Constellation
      </div>
      <div ref={ref} className="relative aspect-[16/10] w-full">
        <svg
          viewBox={`0 0 ${VIEW_W * scale + 32} ${VIEW_H * scale + offsetY + 16}`}
          className="text-cc-accent absolute inset-0 h-full w-full"
          aria-hidden
        >
          <FaintGrid
            w={VIEW_W * scale + 32}
            h={VIEW_H * scale + offsetY + 16}
            step={20}
          />
          {GRAPH_EDGES.map((e) => {
            const a = mini(e.from);
            const b = mini(e.to);
            return (
              <line
                key={`${e.from}-${e.to}`}
                x1={a.x}
                y1={a.y}
                x2={b.x}
                y2={b.y}
                stroke="rgba(148,163,184,0.35)"
                strokeWidth={1}
              />
            );
          })}
          {GRAPH_NODES.map((n) => {
            const ls = LEVEL_STYLE[n.level];
            const p = mini(n.code);
            return (
              <circle
                key={n.code}
                cx={p.x}
                cy={p.y}
                r={4}
                fill="currentColor"
                opacity={ls.chipOpacity}
              />
            );
          })}
          <HeroPulse animate={animate} mini={mini} />
        </svg>
      </div>
      <p className="text-cc-ink-dim mt-3 font-mono text-[11px] tracking-wider uppercase">
        Foundations to Production
      </p>
    </aside>
  );
}

function HeroPulse({
  animate,
  mini,
}: {
  readonly animate: boolean;
  readonly mini: (code: string) => { x: number; y: number };
}) {
  const points = PULSE_PATH.map((c) => mini(c));
  if (!animate) {
    const last = points[points.length - 1];
    return (
      <circle
        cx={last.x}
        cy={last.y}
        r={4.5}
        fill="currentColor"
        opacity={0.9}
      />
    );
  }
  const cx = points.map((p) => p.x);
  const cy = points.map((p) => p.y);
  return (
    <motion.circle
      r={4.5}
      fill="currentColor"
      initial={{ cx: cx[0], cy: cy[0], opacity: 0 }}
      animate={{ cx, cy, opacity: [0, 1, 1, 1, 0.6] }}
      transition={{
        duration: 2.6,
        ease: "easeInOut",
        times: [0, 0.15, 0.5, 0.85, 1],
      }}
    />
  );
}

// ---------------------------------------------------------------------------
// Centerpiece: animated learning path graph
// ---------------------------------------------------------------------------

function LearningPathCenterpiece() {
  const sectionRef = useRef<HTMLDivElement>(null);
  const reduce = useReducedMotion();
  const inView = useInView(sectionRef, { once: true, amount: 0.25 });
  const animate = inView && !reduce;
  const [hovered, setHovered] = useState<string | null>(null);

  return (
    <section id="learning-path" className="py-16">
      <div className="mx-auto mb-8 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          The learning path
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Six stars, one constellation.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Each track is a node. The edges are the way teams actually walk the
          curriculum, from Foundations through Core and Advanced into
          Production.
        </p>
      </div>

      <div
        ref={sectionRef}
        className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-4 sm:p-6"
      >
        <svg
          viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
          className="text-cc-accent block h-auto w-full"
          role="img"
          aria-label="Curriculum learning path graph"
        >
          <defs>
            <marker
              id="cc-training-v7-arrow"
              viewBox="0 0 10 10"
              refX="8"
              refY="5"
              markerWidth="6"
              markerHeight="6"
              orient="auto-start-reverse"
            >
              <path d="M0,0 L10,5 L0,10 Z" fill="rgba(148,163,184,0.55)" />
            </marker>
            <marker
              id="cc-training-v7-arrow-accent"
              viewBox="0 0 10 10"
              refX="8"
              refY="5"
              markerWidth="6"
              markerHeight="6"
              orient="auto-start-reverse"
            >
              <path d="M0,0 L10,5 L0,10 Z" fill="currentColor" />
            </marker>
          </defs>

          <FaintGrid w={VIEW_W} h={VIEW_H} step={32} />

          {/* Edges */}
          {GRAPH_EDGES.map((edge) => (
            <Edge
              key={`${edge.from}-${edge.to}`}
              edge={edge}
              animate={animate}
              reduce={!!reduce}
              hovered={hovered}
            />
          ))}

          {/* Nodes */}
          {GRAPH_NODES.map((node, index) => (
            <Node
              key={node.code}
              node={node}
              index={index}
              animate={animate}
              reduce={!!reduce}
              hovered={hovered}
              onHoverStart={() => setHovered(node.code)}
              onHoverEnd={() =>
                setHovered((cur) => (cur === node.code ? null : cur))
              }
            />
          ))}

          {/* You-are-here pulse */}
          <YouArePulse animate={animate} reduce={!!reduce} />
        </svg>

        <LegendStrip />
      </div>
    </section>
  );
}

function FaintGrid({
  w,
  h,
  step,
}: {
  readonly w: number;
  readonly h: number;
  readonly step: number;
}) {
  const cols = Math.ceil(w / step);
  const rows = Math.ceil(h / step);
  const lines: ReactNode[] = [];
  for (let i = 1; i < cols; i++) {
    lines.push(
      <line
        key={`v${i}`}
        x1={i * step}
        y1={0}
        x2={i * step}
        y2={h}
        stroke="rgba(148,163,184,0.06)"
        strokeWidth={1}
      />,
    );
  }
  for (let i = 1; i < rows; i++) {
    lines.push(
      <line
        key={`h${i}`}
        x1={0}
        y1={i * step}
        x2={w}
        y2={i * step}
        stroke="rgba(148,163,184,0.06)"
        strokeWidth={1}
      />,
    );
  }
  return <g aria-hidden>{lines}</g>;
}

function Edge({
  edge,
  animate,
  reduce,
  hovered,
}: {
  readonly edge: GraphEdge;
  readonly animate: boolean;
  readonly reduce: boolean;
  readonly hovered: string | null;
}) {
  const a = nodeByCode(edge.from);
  const b = nodeByCode(edge.to);
  // Trim endpoints so the line stops at the node circle radius (28).
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const len = Math.sqrt(dx * dx + dy * dy);
  const ux = dx / len;
  const uy = dy / len;
  const r = 30;
  const x1 = a.x + ux * r;
  const y1 = a.y + uy * r;
  const x2 = b.x - ux * (r + 4);
  const y2 = b.y - uy * (r + 4);

  const touched = hovered === edge.from || hovered === edge.to;
  const dim = hovered !== null && !touched;
  const opacity = dim ? 0.18 : 0.85;
  const stroke = touched ? "currentColor" : "rgba(148,163,184,0.55)";
  const marker = touched
    ? "url(#cc-training-v7-arrow-accent)"
    : "url(#cc-training-v7-arrow)";

  const drawDelay = edge.order * EDGE_STEP_DELAY;

  return (
    <motion.line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={stroke}
      strokeWidth={1.5}
      strokeLinecap="round"
      markerEnd={marker}
      initial={reduce ? false : { pathLength: 0, opacity: 0 }}
      animate={
        reduce
          ? { opacity }
          : animate
            ? { pathLength: 1, opacity }
            : { pathLength: 0, opacity: 0 }
      }
      transition={{
        pathLength: {
          duration: EDGE_DRAW_DURATION,
          ease: "easeInOut",
          delay: drawDelay,
        },
        opacity: { duration: 0.25, ease: "easeOut" },
      }}
    />
  );
}

function Node({
  node,
  index,
  animate,
  reduce,
  hovered,
  onHoverStart,
  onHoverEnd,
}: {
  readonly node: GraphNode;
  readonly index: number;
  readonly animate: boolean;
  readonly reduce: boolean;
  readonly hovered: string | null;
  readonly onHoverStart: () => void;
  readonly onHoverEnd: () => void;
}) {
  // Stagger node reveal slightly ahead of its outgoing edges.
  const appearDelay = index * NODE_STEP_DELAY;
  const chipStartDelay = appearDelay + NODE_DRAW_DURATION * 0.75;

  const isHovered = hovered === node.code;
  const dim = hovered !== null && !isHovered && !isNeighbor(hovered, node.code);
  const fadeOpacity = dim ? 0.35 : 1;
  const ls = LEVEL_STYLE[node.level];

  const initial = reduce ? false : { opacity: 0, scale: 0.7 };
  const target = reduce
    ? { opacity: fadeOpacity }
    : animate
      ? { opacity: fadeOpacity, scale: 1 }
      : { opacity: 0, scale: 0.7 };

  return (
    <motion.g
      style={{ transformOrigin: `${node.x}px ${node.y}px` }}
      initial={initial}
      animate={target}
      transition={{
        scale: {
          duration: NODE_DRAW_DURATION,
          ease: "easeOut",
          delay: appearDelay,
        },
        opacity: animate
          ? {
              duration: NODE_DRAW_DURATION,
              ease: "easeOut",
              delay: appearDelay,
            }
          : { duration: 0.2, ease: "easeOut" },
      }}
      onHoverStart={onHoverStart}
      onHoverEnd={onHoverEnd}
      whileHover={{ scale: 1.04 }}
      className="cursor-default"
    >
      <circle
        cx={node.x}
        cy={node.y}
        r={30}
        fill="var(--color-cc-surface)"
        fillOpacity={0.85}
        stroke="currentColor"
        strokeOpacity={ls.strokeOpacity}
        strokeWidth={isHovered ? ls.strokeWidth + 0.75 : ls.strokeWidth}
        strokeDasharray={ls.dashArray}
      />
      <g className="text-cc-heading">
        <text
          x={node.x}
          y={node.y - 4}
          textAnchor="middle"
          className="font-mono"
          fontSize={11}
          fontWeight={600}
          fill="currentColor"
        >
          {node.code}
        </text>
      </g>
      <LevelChipSvg
        x={node.x}
        y={node.y + 9}
        level={node.level}
        animate={animate}
        startDelay={chipStartDelay}
        reduce={reduce}
      />
      <g className="text-cc-ink-dim">
        <text
          x={node.x}
          y={node.y + 52}
          textAnchor="middle"
          fontSize={11}
          fill="currentColor"
        >
          {shortTitle(node.code)}
        </text>
      </g>
    </motion.g>
  );
}

function shortTitle(code: string): string {
  switch (code) {
    case "GQL-101":
      return "Fundamentals";
    case "HC-201":
      return "Hot Chocolate";
    case "FUS-301":
      return "Fusion";
    case "NIT-401":
      return "Nitro";
    case "REL-301":
      return "Relay";
    case "DSN-301":
      return "Schema Design";
    default:
      return code;
  }
}

function isNeighbor(a: string, b: string): boolean {
  return GRAPH_EDGES.some(
    (e) => (e.from === a && e.to === b) || (e.to === a && e.from === b),
  );
}

const LEVEL_LABELS: readonly Level[] = [
  "Foundations",
  "Core",
  "Advanced",
  "Production",
];

function LevelChipSvg({
  x,
  y,
  level,
  animate,
  startDelay,
  reduce,
}: {
  readonly x: number;
  readonly y: number;
  readonly level: Level;
  readonly animate: boolean;
  readonly startDelay: number;
  readonly reduce: boolean;
}) {
  // Tick through the four labels once on enter view, settle on the real level.
  const finalIndex = LEVEL_LABELS.indexOf(level);
  const [label, setLabel] = useState<Level>(reduce ? level : LEVEL_LABELS[0]);

  useEffect(() => {
    if (reduce || !animate) {
      return;
    }
    const sequence: Level[] = [
      LEVEL_LABELS[0],
      LEVEL_LABELS[1],
      LEVEL_LABELS[2],
      LEVEL_LABELS[3],
      LEVEL_LABELS[finalIndex],
    ];
    const timeouts: ReturnType<typeof setTimeout>[] = [];
    sequence.forEach((next, i) => {
      const t = setTimeout(
        () => setLabel((prev) => (prev === next ? prev : next)),
        (startDelay + i * LEVEL_TICK_INTERVAL) * 1000,
      );
      timeouts.push(t);
    });
    return () => {
      timeouts.forEach((t) => clearTimeout(t));
    };
  }, [animate, reduce, finalIndex, startDelay]);

  const final = reduce ? level : label;
  const ls = LEVEL_STYLE[final];

  return (
    <g aria-hidden>
      <rect
        x={x - 32}
        y={y - 1}
        width={64}
        height={12}
        rx={6}
        fill="rgba(148,163,184,0.08)"
        stroke="currentColor"
        strokeOpacity={ls.chipOpacity * 0.5}
        strokeWidth={0.75}
      />
      <text
        x={x}
        y={y + 8}
        textAnchor="middle"
        className="font-mono"
        fontSize={7.5}
        letterSpacing={1.2}
        fill="currentColor"
        fillOpacity={ls.chipOpacity}
      >
        {final.toUpperCase()}
      </text>
    </g>
  );
}

function YouArePulse({
  animate,
  reduce,
}: {
  readonly animate: boolean;
  readonly reduce: boolean;
}) {
  const points = PULSE_PATH.map((c) => nodeByCode(c));

  if (reduce) {
    const last = points[points.length - 1];
    return (
      <g aria-hidden>
        <circle
          cx={last.x}
          cy={last.y}
          r={9}
          fill="currentColor"
          opacity={0.18}
        />
        <circle cx={last.x} cy={last.y} r={4} fill="currentColor" />
      </g>
    );
  }

  const xs = points.map((p) => p.x);
  const ys = points.map((p) => p.y);
  const totalDuration = (points.length - 1) * PULSE_SEGMENT_DURATION;
  // Match keyframe length to positions so motion can interpolate per segment.
  const opacitySteps = xs.map((_, i) => {
    if (i === 0) return 0;
    if (i === xs.length - 1) return 0.6;
    return 1;
  });

  const startTarget = { cx: xs[0], cy: ys[0], opacity: 0 };
  const runTarget = animate
    ? { cx: xs, cy: ys, opacity: opacitySteps }
    : startTarget;

  return (
    <g aria-hidden>
      <motion.circle
        r={11}
        fill="currentColor"
        opacity={0.16}
        initial={startTarget}
        animate={runTarget}
        transition={{
          duration: totalDuration,
          ease: "easeInOut",
          delay: PULSE_START_DELAY,
        }}
      />
      <motion.circle
        r={4.5}
        fill="currentColor"
        initial={startTarget}
        animate={runTarget}
        transition={{
          duration: totalDuration,
          ease: "easeInOut",
          delay: PULSE_START_DELAY,
        }}
      />
    </g>
  );
}

function LegendStrip() {
  const levels: readonly Level[] = [
    "Foundations",
    "Core",
    "Advanced",
    "Production",
  ];
  return (
    <div className="border-cc-card-border mt-5 flex flex-col gap-4 border-t pt-5 sm:flex-row sm:items-center sm:justify-between">
      <ul className="text-cc-accent flex flex-wrap items-center gap-4">
        {levels.map((l) => {
          const ls = LEVEL_STYLE[l];
          return (
            <li
              key={l}
              className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px] tracking-wider uppercase"
            >
              <span
                className="bg-cc-accent inline-block size-2.5 rounded-full"
                style={{ opacity: ls.chipOpacity }}
                aria-hidden
              />
              {l}
            </li>
          );
        })}
      </ul>
      <div className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px] tracking-wider uppercase">
        <span
          className="bg-cc-accent inline-block size-2.5 rounded-full"
          style={{ boxShadow: "0 0 0 4px rgba(94,234,212,0.18)" }}
          aria-hidden
        />
        A team&apos;s journey
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Curriculum detail (cards mirroring the constellation)
// ---------------------------------------------------------------------------

function CurriculumDetail() {
  return (
    <section className="py-16">
      <div className="mb-10 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="max-w-2xl">
          <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
            What we teach
          </div>
          <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
            Six tracks, mix and match.
          </h2>
          <p className="text-cc-prose mt-3 text-base sm:text-lg">
            Each track is modular. A Corporate Training stitches several tracks
            together. A Corporate Workshop usually goes deep on one or two.
          </p>
        </div>
        <div className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
          Catalogue v7
        </div>
      </div>

      <ol className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
        {CURRICULUM.map((track, i) => (
          <TrackCard key={track.code} track={track} index={i} />
        ))}
      </ol>
    </section>
  );
}

function TrackCard({
  track,
  index,
}: {
  readonly track: CurriculumTrack;
  readonly index: number;
}) {
  const ls = LEVEL_STYLE[track.level];
  return (
    <motion.li
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{
        duration: 0.45,
        ease: "easeOut",
        delay: (index % 3) * 0.05,
      }}
      className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col rounded-2xl border p-6 transition-colors"
    >
      <span
        className="bg-cc-accent absolute top-0 left-6 h-px w-12"
        style={{ opacity: ls.chipOpacity }}
        aria-hidden
      />
      <header className="border-cc-card-border mb-5 flex items-baseline justify-between gap-3 border-b pb-4">
        <span
          className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ opacity: ls.chipOpacity }}
        >
          {track.code}
        </span>
        <LevelChip level={track.level} />
      </header>
      <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
        {track.title}
      </h3>
      <p className="text-cc-prose mt-3 text-sm leading-relaxed">
        {track.summary}
      </p>
      <ul className="text-cc-prose mt-5 space-y-2 text-sm">
        {track.topics.map((topic) => (
          <li key={topic} className="flex items-start gap-3">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{topic}</span>
          </li>
        ))}
      </ul>
    </motion.li>
  );
}

function LevelChip({ level }: { readonly level: Level }) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim rounded-full border px-2.5 py-0.5 font-mono text-[10px] font-semibold tracking-widest uppercase">
      {level}
    </span>
  );
}

// ---------------------------------------------------------------------------
// Delivery formats
// ---------------------------------------------------------------------------

function DeliveryFormats() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Delivery format
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          On-site, remote, or hybrid.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          The catalogue is the same. The room is up to you.
        </p>
      </div>
      <div className="grid gap-5 md:grid-cols-3">
        {FORMATS.map((format, i) => (
          <FormatCard key={format.name} format={format} index={i} />
        ))}
      </div>
    </section>
  );
}

function FormatCard({
  format,
  index,
}: {
  readonly format: DeliveryFormat;
  readonly index: number;
}) {
  return (
    <motion.article
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.45, ease: "easeOut", delay: index * 0.08 }}
      className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-2xl border p-6"
    >
      <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
        {format.name}
      </h3>
      <p className="text-cc-ink-dim mt-1 text-sm">{format.tagline}</p>
      <ul className="text-cc-prose mt-5 space-y-3 text-sm">
        {format.notes.map((note) => (
          <li key={note} className="flex items-start gap-3">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{note}</span>
          </li>
        ))}
      </ul>
    </motion.article>
  );
}

// ---------------------------------------------------------------------------
// Offers
// ---------------------------------------------------------------------------

function OffersSection() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          How it is delivered
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Two ways to run the catalogue.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Pick the shape that fits how your team learns. The curriculum carries
          across both.
        </p>
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        {OFFERS.map((offer, i) => (
          <OfferCard key={offer.name} offer={offer} index={i} />
        ))}
      </div>
    </section>
  );
}

function OfferCard({
  offer,
  index,
}: {
  readonly offer: Offer;
  readonly index: number;
}) {
  const cardSkin = offer.highlight
    ? "border-cc-accent/60 shadow-[0_0_0_1px_rgba(94,234,212,0.25)]"
    : "border-cc-card-border";
  const CtaButton = offer.highlight ? SolidButton : OutlineButton;

  return (
    <motion.article
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{ duration: 0.5, ease: "easeOut", delay: index * 0.08 }}
      className={`bg-cc-card-bg relative flex h-full flex-col rounded-2xl border p-7 ${cardSkin}`}
    >
      {offer.highlight && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
          Deep dive
        </span>
      )}
      <header>
        <h3 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight">
          {offer.name}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm">{offer.tagline}</p>
      </header>
      <p className="text-cc-prose mt-5 text-sm leading-relaxed sm:text-base">
        {offer.description}
      </p>
      <ul className="mt-6 flex-1 space-y-3">
        {offer.perks.map((perk) => (
          <li key={perk.text} className="flex items-start gap-3 text-sm">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span className="text-cc-prose">{perk.text}</span>
          </li>
        ))}
      </ul>
      <div className="mt-7">
        <CtaButton
          href={`mailto:contact@chillicream.com?subject=${offer.name}`}
          className="w-full"
        >
          Talk to us about {offer.name}
        </CtaButton>
      </div>
    </motion.article>
  );
}

// ---------------------------------------------------------------------------
// Instructors band with breathing radial
// ---------------------------------------------------------------------------

function InstructorsBand() {
  const reduce = useReducedMotion();
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <BreathingGlow reduce={!!reduce} />
        <div className="relative grid gap-8 lg:grid-cols-[1.2fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Who teaches
            </div>
            <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
              The team behind Hot Chocolate, Fusion, and Nitro.
            </h2>
            <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed sm:text-lg">
              Every session is led by ChilliCream engineers who write and
              maintain the products you are training on. When a question slides
              past the slide deck, you get an answer from the people who decided
              how it works.
            </p>
          </div>
          <ul className="grid gap-4 sm:grid-cols-2">
            <InstructorFact title="Product maintainers">
              Trainers ship on Hot Chocolate, Fusion, and Nitro.
            </InstructorFact>
            <InstructorFact title="Real engagements">
              Years of paid work shaping production GraphQL on .NET.
            </InstructorFact>
            <InstructorFact title="Public speakers">
              Regulars at GraphQL Conf and .NET community events.
            </InstructorFact>
            <InstructorFact title="Honest answers">
              We will tell you what the product does and what it does not.
            </InstructorFact>
          </ul>
        </div>
      </div>
    </section>
  );
}

function BreathingGlow({ reduce }: { readonly reduce: boolean }) {
  return (
    <motion.svg
      className="pointer-events-none absolute -top-24 -right-24 h-[420px] w-[420px]"
      viewBox="0 0 400 400"
      aria-hidden
      initial={{ opacity: reduce ? 0.6 : 0.45 }}
      animate={reduce ? { opacity: 0.6 } : { opacity: [0.45, 0.7, 0.45] }}
      transition={
        reduce
          ? undefined
          : { duration: 6, ease: "easeInOut", repeat: Infinity }
      }
    >
      <defs>
        <radialGradient id="cc-training-v7-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.35" />
          <stop offset="60%" stopColor="#5eead4" stopOpacity="0.05" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx="200" cy="200" r="200" fill="url(#cc-training-v7-glow)" />
    </motion.svg>
  );
}

function InstructorFact({
  title,
  children,
}: {
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <li className="border-cc-card-border rounded-xl border p-4">
      <div className="flex items-start gap-3">
        <span
          className="text-cc-accent mt-[3px] inline-flex shrink-0"
          aria-hidden
        >
          <CheckIcon size={16} />
        </span>
        <div>
          <div className="text-cc-heading font-medium">{title}</div>
          <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
            {children}
          </p>
        </div>
      </div>
    </li>
  );
}

// ---------------------------------------------------------------------------
// FAQ
// ---------------------------------------------------------------------------

function FaqSection() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          FAQ
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          The questions managers ask first.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Straight answers, no hedging.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item) => (
          <details
            key={item.q}
            className="group px-5 py-5 sm:px-6"
            name="training-faq"
          >
            <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
              <span className="text-cc-heading text-base font-medium sm:text-lg">
                {item.q}
              </span>
              <span
                className="text-cc-ink-dim mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                aria-hidden
              >
                <PlusGlyph />
              </span>
            </summary>
            <p className="text-cc-prose mt-3 pr-10 text-sm leading-relaxed sm:text-base">
              {item.a}
            </p>
          </details>
        ))}
      </div>
    </section>
  );
}

function PlusGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={16} height={16} aria-hidden>
      <path
        d="M8 3v10M3 8h10"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

// ---------------------------------------------------------------------------
// Contact band
// ---------------------------------------------------------------------------

function ContactBand() {
  return (
    <section className="py-20">
      <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 text-center sm:p-12">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Get in touch
        </div>
        <h2 className="font-heading text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
          Tell us which tracks, we will quote the rest.
        </h2>
        <p className="text-cc-prose mx-auto mt-4 max-w-xl text-base sm:text-lg">
          Send a short note with the tracks you are interested in, headcount,
          and your preferred format. We come back with a written proposal and a
          date.
        </p>
        <CounterStrip />
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
          <SolidButton href={TRAINING_MAILTO}>Email the team</SolidButton>
          <OutlineButton href="/services/advisory">
            Pair with Advisory
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

function CounterStrip() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const reduce = useReducedMotion();
  const animate = inView && !reduce;
  return (
    <div
      ref={ref}
      className="text-cc-ink-dim mt-8 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 font-mono text-xs tracking-widest uppercase"
    >
      <CounterChunk label="tracks" target={6} animate={animate} />
      <span aria-hidden>&middot;</span>
      <CounterChunk label="levels" target={4} animate={animate} />
      <span aria-hidden>&middot;</span>
      <CounterChunk label="formats" target={3} animate={animate} />
    </div>
  );
}

function CounterChunk({
  label,
  target,
  animate,
}: {
  readonly label: string;
  readonly target: number;
  readonly animate: boolean;
}) {
  const [value, setValue] = useState(0);

  useEffect(() => {
    if (!animate) {
      const t = window.setTimeout(() => setValue(target), 0);
      return () => window.clearTimeout(t);
    }
    const reset = window.setTimeout(() => setValue(0), 0);
    let current = 0;
    const id = window.setInterval(() => {
      current += 1;
      setValue(current);
      if (current >= target) {
        window.clearInterval(id);
      }
    }, 120);
    return () => {
      window.clearTimeout(reset);
      window.clearInterval(id);
    };
  }, [animate, target]);

  return (
    <span className="inline-flex items-baseline gap-1">
      <span className="text-cc-heading text-base">{value}</span>
      <span>{label}</span>
    </span>
  );
}
