"use client";

import type { CSSProperties, ReactNode } from "react";
import { useMemo, useRef } from "react";
import { motion, useInView, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Scene accent                                                              */
/*  Page accent: cc-accent teal (#5eead4). The brand spectrum appears ONCE,   */
/*  on four nodes of the hero constellation, to mark inflection points.       */
/* -------------------------------------------------------------------------- */

const ACCENT = "#5eead4";
const BRAND_TEAL = "#5eead4";
const BRAND_CYAN = "#16b9e4";
const BRAND_VIOLET = "#7c92c6";
const BRAND_CORAL = "#f0786a";

/* -------------------------------------------------------------------------- */
/*  Constellation data                                                        */
/*  Deterministic node positions and a curated edge list. All edges fan out   */
/*  from the central source node (id 0) directly or via short hops, so the    */
/*  graph reads as "one star, many derivatives".                              */
/* -------------------------------------------------------------------------- */

interface Star {
  readonly id: number;
  readonly x: number;
  readonly y: number;
  readonly r: number;
  readonly fill?: string;
  readonly label?: string;
}

const STARS: readonly Star[] = [
  // central source node (teal, branded)
  { id: 0, x: 600, y: 300, r: 5, fill: BRAND_TEAL, label: "source" },
  // branded satellites: each maps to an artifact the page actually delivers
  { id: 1, x: 280, y: 180, r: 4, fill: BRAND_CYAN, label: "client" },
  { id: 2, x: 920, y: 220, r: 4, fill: BRAND_VIOLET, label: "dataloader" },
  { id: 3, x: 760, y: 460, r: 4, fill: BRAND_CORAL, label: "schema" },
  // ambient stars
  { id: 4, x: 120, y: 90, r: 1.5 },
  { id: 5, x: 220, y: 360, r: 2 },
  { id: 6, x: 350, y: 80, r: 1.5 },
  { id: 7, x: 440, y: 230, r: 2 },
  { id: 8, x: 470, y: 430, r: 1.5 },
  { id: 9, x: 540, y: 140, r: 2 },
  { id: 10, x: 560, y: 510, r: 1.5 },
  { id: 11, x: 680, y: 120, r: 2 },
  { id: 12, x: 720, y: 340, r: 2.5 },
  { id: 13, x: 820, y: 380, r: 1.5 },
  { id: 14, x: 860, y: 100, r: 2 },
  { id: 15, x: 980, y: 460, r: 1.5 },
  { id: 16, x: 1080, y: 270, r: 2 },
  { id: 17, x: 1130, y: 150, r: 1.5 },
  { id: 18, x: 80, y: 480, r: 1.5 },
  { id: 19, x: 180, y: 530, r: 2 },
  { id: 20, x: 380, y: 540, r: 1.5 },
  { id: 21, x: 50, y: 260, r: 2 },
  { id: 22, x: 1020, y: 70, r: 1.5 },
  { id: 23, x: 640, y: 60, r: 1.5 },
  { id: 24, x: 410, y: 380, r: 2 },
  { id: 25, x: 880, y: 540, r: 2 },
  { id: 26, x: 1170, y: 400, r: 1.5 },
  { id: 27, x: 300, y: 280, r: 2 },
];

/** Edges fan from the source node (0) outward through hops. */
const EDGES: readonly (readonly [number, number])[] = [
  [0, 1],
  [0, 2],
  [0, 3],
  [0, 7],
  [0, 9],
  [0, 12],
  [0, 11],
  [0, 27],
  [1, 4],
  [1, 21],
  [1, 27],
  [2, 14],
  [2, 16],
  [2, 17],
  [3, 10],
  [3, 13],
  [3, 25],
  [7, 5],
  [9, 23],
  [11, 14],
  [12, 8],
  [12, 24],
  [16, 22],
  [16, 26],
  [5, 18],
  [18, 19],
  [24, 20],
];

/* -------------------------------------------------------------------------- */
/*  Hero constellation                                                        */
/* -------------------------------------------------------------------------- */

function HeroConstellation() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.3 });
  const reduce = useReducedMotion();

  // Precompute line lengths so we can animate strokeDashoffset cleanly.
  const lines = useMemo(
    () =>
      EDGES.map(([a, b], i) => {
        const s = STARS[a];
        const t = STARS[b];
        const dx = t.x - s.x;
        const dy = t.y - s.y;
        const len = Math.sqrt(dx * dx + dy * dy);
        return { i, s, t, len, key: `${a}-${b}` };
      }),
    [],
  );

  const totalLines = lines.length;

  return (
    <div
      ref={ref}
      className="pointer-events-none absolute inset-0 -z-10 overflow-hidden"
      aria-hidden
    >
      {/* radial glow under the source star */}
      <div
        className="absolute inset-0 opacity-50"
        style={{
          background:
            "radial-gradient(45% 55% at 50% 50%, rgba(94,234,212,0.16), transparent 70%)",
        }}
      />
      <svg
        viewBox="0 0 1200 600"
        className="h-full w-full"
        preserveAspectRatio="xMidYMid slice"
        fill="none"
      >
        <g>
          {lines.map(({ i, s, t, len, key }) => {
            const delay = (i / Math.max(totalLines - 1, 1)) * 0.6;
            const animate =
              !reduce && inView
                ? { strokeDashoffset: 0 }
                : reduce
                  ? { strokeDashoffset: 0 }
                  : { strokeDashoffset: len };
            return (
              <motion.line
                key={key}
                x1={s.x}
                y1={s.y}
                x2={t.x}
                y2={t.y}
                stroke="rgba(148,163,184,0.45)"
                strokeWidth={1}
                strokeLinecap="round"
                style={{
                  strokeDasharray: len,
                  strokeDashoffset: reduce ? 0 : len,
                  opacity: 0.4,
                }}
                animate={animate}
                transition={{
                  duration: reduce ? 0 : 1.2,
                  delay: reduce ? 0 : delay,
                  ease: "easeOut",
                }}
              />
            );
          })}
        </g>
        <g>
          {STARS.map((star) => {
            const isBranded = star.id <= 3;
            const fill = star.fill ?? "rgba(203,213,225,0.55)";
            const delay = isBranded
              ? 0.8 + star.id * 0.12
              : 0.05 * (star.id % 10);
            return (
              <motion.circle
                key={star.id}
                cx={star.x}
                cy={star.y}
                r={star.r}
                fill={fill}
                initial={reduce ? { opacity: 1 } : { opacity: 0 }}
                animate={
                  reduce || inView ? { opacity: isBranded ? 1 : 0.85 } : { opacity: 0 }
                }
                transition={{
                  duration: 0.2,
                  delay: reduce ? 0 : delay,
                  ease: "easeOut",
                }}
              />
            );
          })}
          {/* faint halo around the source star */}
          <motion.circle
            cx={STARS[0].x}
            cy={STARS[0].y}
            r={14}
            fill="none"
            stroke={BRAND_TEAL}
            strokeWidth={1}
            initial={reduce ? { opacity: 0.35 } : { opacity: 0 }}
            animate={
              reduce || inView ? { opacity: 0.35 } : { opacity: 0 }
            }
            transition={{ duration: 0.4, delay: reduce ? 0 : 1.4 }}
          />
        </g>
      </svg>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Shared chrome primitives                                                  */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface SectionHeadProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
  readonly align?: "left" | "center";
}

function SectionHead({
  eyebrow,
  title,
  children,
  align = "left",
}: SectionHeadProps) {
  const alignClass = align === "center" ? "mx-auto text-center" : "";
  return (
    <div className={`max-w-2xl ${alignClass}`}>
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight">
        {title}
      </h2>
      {children ? (
        <p className="text-cc-prose mt-4 text-[1.05rem] leading-relaxed">
          {children}
        </p>
      ) : null}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Constellation glyph: a tiny "this is your star" icon for artifact cards   */
/* -------------------------------------------------------------------------- */

interface ConstellationGlyphProps {
  readonly highlightId: number;
}

function ConstellationGlyph({ highlightId }: ConstellationGlyphProps) {
  // A simplified 5-node glyph echoing the hero: center + 4 satellites.
  const nodes: ReadonlyArray<{
    readonly id: number;
    readonly x: number;
    readonly y: number;
  }> = [
    { id: 0, x: 30, y: 22 }, // center
    { id: 1, x: 8, y: 8 },
    { id: 2, x: 52, y: 10 },
    { id: 3, x: 48, y: 36 },
    { id: 4, x: 12, y: 36 },
  ];
  return (
    <svg
      viewBox="0 0 60 44"
      className="h-7 w-10 shrink-0"
      aria-hidden
      fill="none"
    >
      {nodes.slice(1).map((n) => (
        <line
          key={n.id}
          x1={nodes[0].x}
          y1={nodes[0].y}
          x2={n.x}
          y2={n.y}
          stroke="rgba(148,163,184,0.5)"
          strokeWidth={0.8}
        />
      ))}
      {nodes.map((n) => {
        const isCenter = n.id === 0;
        const isHighlight = n.id === highlightId;
        const fill = isCenter
          ? BRAND_TEAL
          : isHighlight
            ? ACCENT
            : "rgba(203,213,225,0.65)";
        return (
          <circle
            key={n.id}
            cx={n.x}
            cy={n.y}
            r={isCenter || isHighlight ? 2.6 : 1.5}
            fill={fill}
          />
        );
      })}
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  Artifact cards (3-up lineage legend)                                      */
/* -------------------------------------------------------------------------- */

interface ArtifactCardProps {
  readonly highlightId: number;
  readonly tag: string;
  readonly title: string;
  readonly children: ReactNode;
}

function ArtifactCard({
  highlightId,
  tag,
  title,
  children,
}: ArtifactCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex flex-col overflow-hidden rounded-xl border">
      <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3">
        <ConstellationGlyph highlightId={highlightId} />
        <div className="flex flex-col">
          <span className="text-cc-heading font-mono text-[0.72rem]">
            {title}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.14em] uppercase">
            {tag}
          </span>
        </div>
      </div>
      <div className="flex grow flex-col p-4">{children}</div>
    </div>
  );
}

const SYN = {
  attr: "#7ee787",
  keyword: "#ff7b72",
  type: "#79c0ff",
  member: "#d2a8ff",
  string: "#a5d6ff",
  comment: "#8b949e",
  punct: "#c9d1d9",
};

function SdlArtifact() {
  return (
    <ArtifactCard
      highlightId={3}
      tag="generated SDL"
      title="schema.graphql"
    >
      <pre className="font-mono text-[0.72rem] leading-5">
        <code>
          <span style={{ color: SYN.keyword }}>type</span>{" "}
          <span style={{ color: SYN.type }}>Query</span> {"{"}
          {"\n  "}
          <span style={{ color: SYN.member }}>product</span>(
          <span style={{ color: "#ffa657" }}>id</span>:{" "}
          <span style={{ color: SYN.type }}>Int!</span>):{" "}
          <span style={{ color: SYN.type }}>Product</span>
          {"\n"}
          {"}"}
          {"\n\n"}
          <span style={{ color: SYN.keyword }}>type</span>{" "}
          <span style={{ color: SYN.type }}>Product</span> {"{"}
          {"\n  "}
          <span style={{ color: SYN.member }}>id</span>:{" "}
          <span style={{ color: SYN.type }}>Int!</span>
          {"\n  "}
          <span style={{ color: SYN.member }}>name</span>:{" "}
          <span style={{ color: SYN.type }}>String!</span>
          {"\n"}
          {"}"}
        </code>
      </pre>
      <p className="text-cc-nav-label mt-3 border-t border-cc-card-border pt-2.5 font-mono text-[0.6rem] tracking-tight">
        derived at build, never edited by hand
      </p>
    </ArtifactCard>
  );
}

function BatchArtifact() {
  const keys = [41, 17, 88, 17, 41];
  return (
    <ArtifactCard highlightId={4} tag="runtime" title="DataLoader batch">
      <div className="flex grow items-center justify-between gap-2">
        <div className="flex flex-col gap-1.5">
          {keys.map((k, i) => (
            <span
              key={`${k}-${i}`}
              className="text-cc-ink-dim border-cc-card-border bg-cc-surface rounded-md border px-2 py-1 text-center font-mono text-[0.65rem] tabular-nums"
            >
              {k}
            </span>
          ))}
        </div>

        <svg
          viewBox="0 0 64 120"
          className="h-28 w-16 shrink-0"
          aria-hidden
          fill="none"
        >
          {[12, 33, 54, 75, 96].map((y, i) => (
            <path
              key={i}
              d={`M0 ${y} C 26 ${y}, 26 60, 60 60`}
              stroke={ACCENT}
              strokeWidth="1.2"
              opacity={0.7}
            />
          ))}
          <circle cx="61" cy="60" r="3" fill={ACCENT} />
        </svg>

        <div className="flex flex-col items-center gap-1">
          <span
            className="rounded-md px-2.5 py-1.5 text-center font-mono text-[0.62rem] leading-tight"
            style={{
              color: ACCENT,
              border: "1px solid rgba(94, 234, 212, 0.3)",
              backgroundColor: "rgba(94, 234, 212, 0.06)",
            }}
          >
            ByIds(
            <br />
            [41,17,88])
          </span>
          <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-tight">
            1 query
          </span>
        </div>
      </div>
      <p className="text-cc-nav-label border-cc-card-border mt-3 border-t pt-2.5 font-mono text-[0.6rem] tracking-tight">
        5 keys · deduped to 3 ids · 1 fetch
      </p>
    </ArtifactCard>
  );
}

function ClientArtifact() {
  return (
    <ArtifactCard
      highlightId={1}
      tag="MSBuild codegen"
      title="ProductClient.cs"
    >
      <pre className="font-mono text-[0.72rem] leading-5">
        <code>
          <span style={{ color: SYN.keyword }}>var</span> result ={"\n  "}
          <span style={{ color: SYN.keyword }}>await</span> client
          {"\n    ."}
          <span style={{ color: SYN.member }}>GetProduct</span>
          {"\n    ."}
          <span style={{ color: SYN.member }}>ExecuteAsync</span>(
          <span style={{ color: SYN.string }}>id</span>);
          {"\n\n"}
          <span style={{ color: SYN.keyword }}>string</span> name ={"\n  "}
          result.Data
          {"\n    ."}
          <span style={{ color: SYN.member }}>Product</span>
          {"\n    ."}
          <span style={{ color: SYN.member }}>Name</span>;
          <span style={{ color: SYN.comment }}>{" // typed"}</span>
        </code>
      </pre>
      <p className="text-cc-nav-label border-cc-card-border mt-3 border-t pt-2.5 font-mono text-[0.6rem] tracking-tight">
        Strawberry Shake regenerates on save
      </p>
    </ArtifactCard>
  );
}

/* -------------------------------------------------------------------------- */
/*  MSBuild codegen ribbon (horizontal stages)                                */
/* -------------------------------------------------------------------------- */

interface StageProps {
  readonly index: number;
  readonly label: string;
  readonly note: string;
  readonly last?: boolean;
}

function Stage({ index, label, note, last = false }: StageProps) {
  return (
    <li className="relative flex flex-1 flex-col">
      <div className="flex items-center">
        <span
          className="relative z-10 flex h-8 w-8 shrink-0 items-center justify-center rounded-full font-mono text-[0.72rem] font-semibold"
          style={{
            color: "#0b0f1a",
            backgroundColor: ACCENT,
          }}
        >
          {index}
        </span>
        {!last ? (
          <span
            className="h-px flex-1"
            style={{
              background:
                "linear-gradient(90deg, rgba(94,234,212,0.55), rgba(94,234,212,0.12))",
            }}
            aria-hidden
          />
        ) : null}
      </div>
      <p className="text-cc-heading mt-3 font-mono text-[0.74rem] tracking-tight">
        {label}
      </p>
      <p className="text-cc-ink-dim mt-1 pr-5 text-[0.82rem] leading-snug">
        {note}
      </p>
    </li>
  );
}

/* -------------------------------------------------------------------------- */
/*  Mini-constellation: glue collapse (decorative, no brand spectrum)         */
/* -------------------------------------------------------------------------- */

function MiniConstellation() {
  const nodes: ReadonlyArray<{
    readonly id: number;
    readonly x: number;
    readonly y: number;
    readonly label: string;
  }> = [
    { id: 0, x: 130, y: 95, label: "ProductApi.cs" },
    { id: 1, x: 30, y: 30, label: "schema.graphql" },
    { id: 2, x: 230, y: 28, label: "Resolvers.cs" },
    { id: 3, x: 240, y: 160, label: "client.schema" },
    { id: 4, x: 30, y: 160, label: "mappings" },
  ];
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[0.68rem]">
          glue collapse
        </span>
        <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.14em] uppercase">
          four files into one star
        </span>
      </div>
      <div className="relative px-4 py-6">
        <svg
          viewBox="0 0 270 200"
          className="block h-44 w-full"
          aria-hidden
          fill="none"
        >
          {nodes.slice(1).map((n) => (
            <line
              key={n.id}
              x1={nodes[0].x}
              y1={nodes[0].y}
              x2={n.x}
              y2={n.y}
              stroke="rgba(148,163,184,0.45)"
              strokeWidth={1}
              strokeDasharray="3 3"
            />
          ))}
          {nodes.map((n) => {
            const isCenter = n.id === 0;
            return (
              <g key={n.id}>
                <circle
                  cx={n.x}
                  cy={n.y}
                  r={isCenter ? 5 : 3}
                  fill={isCenter ? ACCENT : "rgba(203,213,225,0.65)"}
                />
                <text
                  x={n.x}
                  y={isCenter ? n.y + 22 : n.y < 80 ? n.y - 10 : n.y + 18}
                  textAnchor="middle"
                  className="font-mono"
                  fontSize={9}
                  fill={isCenter ? ACCENT : "rgba(203,213,225,0.7)"}
                >
                  {n.label}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Comparison table                                                          */
/* -------------------------------------------------------------------------- */

type Cell = "good" | "warn" | "bad";

interface RowProps {
  readonly label: string;
  readonly handWired: Cell;
  readonly handWiredText: string;
  readonly schemaFirst: Cell;
  readonly schemaFirstText: string;
  readonly generated: Cell;
  readonly generatedText: string;
}

function CellMark({
  kind,
  text,
}: {
  readonly kind: Cell;
  readonly text: string;
}) {
  const styles: Record<Cell, string> = {
    good: "text-cc-success",
    warn: "text-cc-warning",
    bad: "text-cc-danger",
  };
  const glyph: Record<Cell, string> = {
    good: "●",
    warn: "◐",
    bad: "○",
  };
  return (
    <span className="flex items-center gap-2 text-[0.82rem]">
      <span className={`${styles[kind]} text-[0.7rem]`} aria-hidden>
        {glyph[kind]}
      </span>
      <span className="text-cc-ink-dim">{text}</span>
    </span>
  );
}

const COMPARISON: readonly RowProps[] = [
  {
    label: "Schema drift",
    handWired: "warn",
    handWiredText: "Manual sync",
    schemaFirst: "bad",
    schemaFirstText: "DSL not code",
    generated: "good",
    generatedText: "One source",
  },
  {
    label: "Type safety",
    handWired: "warn",
    handWiredText: "Mostly typed",
    schemaFirst: "warn",
    schemaFirstText: "Re-mapped",
    generated: "good",
    generatedText: "End to end",
  },
  {
    label: "N+1 fetches",
    handWired: "bad",
    handWiredText: "Easy to miss",
    schemaFirst: "warn",
    schemaFirstText: "Wired by hand",
    generated: "good",
    generatedText: "[DataLoader]",
  },
  {
    label: "Client sync",
    handWired: "bad",
    handWiredText: "Hand-rolled",
    schemaFirst: "warn",
    schemaFirstText: "Separate gen",
    generated: "good",
    generatedText: "MSBuild gen",
  },
  {
    label: "Build feedback",
    handWired: "warn",
    handWiredText: "At runtime",
    schemaFirst: "warn",
    schemaFirstText: "Codegen step",
    generated: "good",
    generatedText: "At build",
  },
];

function ComparisonTable() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border">
      <div className="border-cc-card-border grid grid-cols-[1.1fr_1fr_1fr_1.1fr] border-b">
        <div className="px-4 py-3.5">
          <Eyebrow>approach</Eyebrow>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3.5">
          <span className="text-cc-ink font-mono text-[0.72rem]">
            Hand-wired
          </span>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3.5">
          <span className="text-cc-ink font-mono text-[0.72rem]">
            Schema-first DSL
          </span>
        </div>
        <div
          className="border-l px-4 py-3.5"
          style={{ borderColor: "rgba(94,234,212,0.3)" }}
        >
          <span className="font-mono text-[0.72rem]" style={{ color: ACCENT }}>
            Source-generated
          </span>
        </div>
      </div>

      {COMPARISON.map((row, i) => (
        <div
          key={row.label}
          className={[
            "grid grid-cols-[1.1fr_1fr_1fr_1.1fr] items-center",
            i % 2 === 1 ? "bg-cc-surface/40" : "",
          ].join(" ")}
        >
          <div className="px-4 py-3.5">
            <span className="text-cc-heading text-[0.85rem] font-medium">
              {row.label}
            </span>
          </div>
          <div className="border-cc-card-border border-l px-4 py-3.5">
            <CellMark kind={row.handWired} text={row.handWiredText} />
          </div>
          <div className="border-cc-card-border border-l px-4 py-3.5">
            <CellMark kind={row.schemaFirst} text={row.schemaFirstText} />
          </div>
          <div
            className="border-l px-4 py-3.5"
            style={{
              borderColor: "rgba(94,234,212,0.3)",
              backgroundColor: "rgba(94,234,212,0.04)",
            }}
          >
            <CellMark kind={row.generated} text={row.generatedText} />
          </div>
        </div>
      ))}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Anchor card overlay for hero                                              */
/* -------------------------------------------------------------------------- */

function AnchorCard() {
  return (
    <div
      className="border-cc-card-border bg-cc-surface/90 max-w-[19rem] rounded-xl border p-4 shadow-2xl shadow-black/40 backdrop-blur-md"
      style={{
        boxShadow:
          "0 30px 60px -20px rgba(0,0,0,0.6), 0 0 0 1px rgba(94,234,212,0.08)",
      }}
    >
      <div className="flex items-center gap-2">
        <span
          className="inline-block h-2 w-2 rounded-full"
          style={{ backgroundColor: BRAND_TEAL }}
          aria-hidden
        />
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
          source star
        </span>
      </div>
      <p className="text-cc-heading mt-2.5 font-mono text-[0.85rem]">
        [QueryType] ProductApi
      </p>
      <p className="text-cc-ink-dim mt-2 text-[0.8rem] leading-snug">
        The one class you maintain. The schema, DataLoaders, and the typed
        client all descend from it.
      </p>
      <div className="border-cc-card-border mt-3 flex items-center justify-between border-t pt-2.5">
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-tight">
          ProductApi.cs
        </span>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-tight">
          C# · partial
        </span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

const containerStyle: CSSProperties = {
  maxWidth: "72rem",
};

export function ClientPage() {
  return (
    <div
      className="mx-auto flex flex-col gap-28 py-6 sm:gap-32"
      style={containerStyle}
    >
      {/* ----------------------------- HERO ----------------------------- */}
      <section className="relative isolate min-h-[36rem] overflow-visible py-12 sm:py-16">
        <HeroConstellation />
        <div className="relative max-w-3xl">
          <Eyebrow>Build loop · constellation of truth</Eyebrow>
          <h1 className="font-heading text-hero text-cc-heading mt-5 font-semibold tracking-tight">
            Ship from the{" "}
            <span style={{ color: ACCENT }}>code that runs it.</span>
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-[1.15rem] leading-relaxed">
            Implementation-first GraphQL in C#. One annotated class is the
            source star. Hot Chocolate generates the schema, the resolver
            pipeline, and the DataLoaders from it. Strawberry Shake codegens a
            typed .NET client. Every artifact traces back to the same code that
            answers the request.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
          <ul className="text-cc-ink-dim mt-9 flex flex-wrap gap-x-6 gap-y-2 text-[0.85rem]">
            {[
              "No separate schema file to drift",
              "Typed end to end in one language",
              "DataLoaders generated, not hand-wired",
            ].map((item) => (
              <li key={item} className="flex items-center gap-2">
                <span style={{ color: ACCENT }}>
                  <CheckIcon size={13} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        <div className="relative mt-12 flex justify-end lg:absolute lg:right-0 lg:bottom-6 lg:mt-0">
          <AnchorCard />
        </div>
      </section>

      {/* ------------------------ LINEAGE LEGEND ------------------------ */}
      <section>
        <SectionHead
          eyebrow="One source · four stars"
          title="Each generated artifact is a star derived from the central class."
          align="center"
        >
          The <code className="text-cc-ink font-mono">ProductApi</code> class is
          the only thing you maintain. At build time it fans out into the
          schema you serve, the batching that protects your database, and a
          typed client your callers consume. Each card below carries a glyph for
          its place in the lineage.
        </SectionHead>

        <div className="mt-12 grid gap-5 md:grid-cols-3">
          <SdlArtifact />
          <BatchArtifact />
          <ClientArtifact />
        </div>
      </section>

      {/* ------------------------- BUILD RIBBON ------------------------- */}
      <section>
        <SectionHead
          eyebrow="MSBuild codegen · .graphql to typed .NET"
          title="The loop closes at build, not at the first failing request."
        >
          A save triggers the same generation your CI runs. The schema is
          re-derived, DataLoaders are rebuilt, and Strawberry Shake regenerates
          the typed client from your operations, all before anything hits a
          runtime path.
        </SectionHead>

        <div className="border-cc-card-border bg-cc-card-bg mt-10 rounded-2xl border p-6 sm:p-8">
          <ol className="flex flex-col gap-8 sm:flex-row sm:gap-0">
            <Stage
              index={1}
              label="annotate"
              note="Add [QueryType] / [DataLoader] to a partial class."
            />
            <Stage
              index={2}
              label="generate"
              note="Source generators emit the schema and resolver pipeline."
            />
            <Stage
              index={3}
              label="codegen"
              note="Strawberry Shake builds a typed .NET client via MSBuild."
            />
            <Stage
              index={4}
              label="ship"
              note="The contract you publish is the code that just compiled."
              last
            />
          </ol>
        </div>
      </section>

      {/* --------------------- GLUE COLLAPSE (60/40) -------------------- */}
      <section className="grid items-center gap-12 lg:grid-cols-[3fr_2fr]">
        <div>
          <SectionHead
            eyebrow="Fewer glue layers"
            title="Collapse the glue tangle into the class itself."
          >
            Schema-first stacks ask you to keep a DSL, a resolver map, and a
            client schema in step by hand. Implementation-first removes those
            seams: the annotation is the binding, so there is nothing in
            between to fall out of date.
          </SectionHead>
          <ul className="mt-6 flex flex-col gap-3">
            {[
              "No DSL file mirroring your types",
              "No resolver-to-field wiring table",
              "No separately maintained client schema",
            ].map((item) => (
              <li
                key={item}
                className="text-cc-ink-dim flex items-center gap-3 text-[0.95rem]"
              >
                <span style={{ color: ACCENT }}>
                  <CheckIcon size={14} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        <MiniConstellation />
      </section>

      {/* ------------------------- COMPARISON --------------------------- */}
      <section>
        <SectionHead
          eyebrow="The difference, line by line"
          title="Three ways to build a GraphQL API. One keeps the contract honest."
        />
        <div className="mt-10">
          <ComparisonTable />
        </div>
        <p className="text-cc-nav-label mt-4 font-mono text-[0.66rem] tracking-tight">
          ● strong · ◐ partial · ○ weak. Based on what each approach maintains
          by hand.
        </p>
      </section>

      {/* -------------------------- HONESTY ----------------------------- */}
      <section className="border-cc-card-border bg-cc-surface/60 rounded-2xl border p-8 sm:p-10">
        <Eyebrow>Where the line is</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-3 max-w-3xl font-semibold tracking-tight">
          Generation removes drift inside your service. It does not freeze the
          world outside it.
        </h2>
        <div className="mt-7 grid gap-6 sm:grid-cols-2">
          <p className="text-cc-prose text-[1rem] leading-relaxed">
            One annotated class means the schema, resolvers, and DataLoaders
            cannot disagree, because they are derived from the same code. That
            is a narrow, real guarantee, and it is the one we make.
          </p>
          <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
            It is not a promise about consumers you do not control. When a type
            changes, the schema diff names which published clients are affected
            so you can coordinate the rollout instead of discovering it in
            production.
          </p>
        </div>
      </section>

      {/* ---------------------------- CTA ------------------------------- */}
      <section className="flex flex-col items-center gap-7 py-6 text-center">
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Start with the class. Ship the contract.
        </h2>
        <p className="text-cc-prose max-w-xl text-[1.1rem] leading-relaxed">
          Build your first implementation-first GraphQL API in C# and watch the
          schema, the batching, and a typed client appear from the code you
          already wrote.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
