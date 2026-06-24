"use client";

import { motion, useReducedMotion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Survey accent                                                             */
/*  Page accent: cc-accent teal (#5eead4). The brand spectrum (cyan ->        */
/*  violet -> coral) appears EXACTLY ONCE, on the hero summit ring stroke.    */
/* -------------------------------------------------------------------------- */

const ACCENT = "#5eead4";
const SPECTRUM_CYAN = "#16b9e4";
const SPECTRUM_VIOLET = "#7c92c6";
const SPECTRUM_CORAL = "#f0786a";

/* GitHub-dark inspired syntax palette so the editor mocks read as real code. */
const SYN = {
  attr: "#7ee787",
  keyword: "#ff7b72",
  type: "#79c0ff",
  member: "#d2a8ff",
  string: "#a5d6ff",
  comment: "#8b949e",
  arg: "#ffa657",
  punct: "#c9d1d9",
};

/* -------------------------------------------------------------------------- */
/*  Topographic contour backdrop                                              */
/*  Concentric closed-loop "blob" rings drawn as one inline SVG. Lines        */
/*  thicken and brighten toward the center (the source) and fade with         */
/*  distance, so the eye always traces back to the implementation. Map-clean: */
/*  no filters, no blur, no gradients on the contours themselves.             */
/* -------------------------------------------------------------------------- */

/**
 * Builds a closed blob path on a 600x600 box centered at (300, 300).
 * `r` is the base radius; `wobble` perturbs each of eight anchor points so
 * the rings read as irregular survey contours rather than perfect circles.
 */
function blobPath(r: number, wobble: number): string {
  const cx = 300;
  const cy = 300;
  const steps = 8;
  const pts: Array<readonly [number, number]> = [];
  for (let i = 0; i < steps; i++) {
    const angle = (i / steps) * Math.PI * 2;
    // Deterministic per-angle perturbation, stable across renders.
    const wob = Math.sin(angle * 3 + r * 0.04) * wobble;
    const rr = r + wob;
    pts.push([cx + Math.cos(angle) * rr, cy + Math.sin(angle) * rr * 0.92]);
  }
  // Smooth closed curve through the points using midpoint quadratics.
  let d = "";
  for (let i = 0; i < steps; i++) {
    const [x0, y0] = pts[i];
    const [x1, y1] = pts[(i + 1) % steps];
    const mx = (x0 + x1) / 2;
    const my = (y0 + y1) / 2;
    if (i === 0) {
      d += `M ${mx.toFixed(1)} ${my.toFixed(1)} `;
    }
    const [nx, ny] = pts[(i + 1) % steps];
    const cmx = (x1 + nx) / 2;
    const cmy = (y1 + ny) / 2;
    d += `Q ${x1.toFixed(1)} ${y1.toFixed(1)} ${cmx.toFixed(1)} ${cmy.toFixed(1)} `;
  }
  return `${d}Z`;
}

interface ContourFieldProps {
  /** Number of nested rings; the spec calls for ~14. */
  readonly rings?: number;
  /** Extra class for positioning the fixed/absolute container. */
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * One contour cluster: nested rings plus a faint radial glow at the same
 * center to suggest "depth toward the source".
 */
function ContourField({ rings = 14, className, style }: ContourFieldProps) {
  const items = Array.from({ length: rings }, (_, i) => i);
  return (
    <div
      className={["pointer-events-none absolute", className ?? ""].join(" ")}
      style={style}
      aria-hidden
    >
      {/* depth glow at the survey center */}
      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(38% 38% at 50% 50%, ${ACCENT}0a, transparent 72%)`,
        }}
      />
      <svg
        viewBox="0 0 600 600"
        className="h-full w-full"
        fill="none"
        preserveAspectRatio="xMidYMid meet"
      >
        {items.map((i) => {
          const r = 22 + i * 19;
          // Thicker + brighter near the center, fading outward (0.10 -> 0.04).
          const t = i / (rings - 1);
          const opacity = 0.1 - t * 0.06;
          return (
            <path
              key={i}
              d={blobPath(r, 5 + i * 0.9)}
              stroke={ACCENT}
              strokeWidth={1}
              opacity={opacity}
            />
          );
        })}
      </svg>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Elevation eyebrow (left-gutter section index, E-01 .. E-08)               */
/* -------------------------------------------------------------------------- */

interface ElevationProps {
  readonly mark: string;
  readonly children: ReactNode;
}

/** A mono eyebrow tagged with its elevation band, doubling as a tick mark. */
function Elevation({ mark, children }: ElevationProps) {
  return (
    <div className="flex items-center gap-3">
      <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] tabular-nums">
        {mark}
      </span>
      <span className="bg-cc-card-border h-px w-6" aria-hidden />
      <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
        {children}
      </span>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Survey-plate panel (map-tile framed surface)                              */
/* -------------------------------------------------------------------------- */

interface PlateProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function Plate({ children, className }: PlateProps) {
  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg rounded-2xl border backdrop-blur-sm",
        className ?? "",
      ].join(" ")}
    >
      {children}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Editor chrome                                                             */
/* -------------------------------------------------------------------------- */

interface WindowDotsProps {
  readonly title: string;
  readonly meta?: string;
}

function WindowDots({ title, meta }: WindowDotsProps) {
  return (
    <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-3.5 py-2.5">
      <span className="flex gap-1.5" aria-hidden>
        <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]/80" />
      </span>
      <span className="text-cc-ink-dim ml-1.5 font-mono text-[0.7rem] tracking-tight">
        {title}
      </span>
      {meta ? (
        <span className="text-cc-nav-label ml-auto font-mono text-[0.65rem] tracking-tight">
          {meta}
        </span>
      ) : null}
    </div>
  );
}

interface CodeTokenProps {
  readonly c?: string;
  readonly children: ReactNode;
}

function T({ c, children }: CodeTokenProps) {
  return <span style={c ? { color: c } : undefined}>{children}</span>;
}

interface CodeLineProps {
  readonly n: number;
  readonly indent?: number;
  readonly highlight?: boolean;
  readonly children: ReactNode;
}

function CodeLine({ n, indent = 0, highlight = false, children }: CodeLineProps) {
  return (
    <div className={["flex items-start", highlight ? "bg-[#5eead4]/8" : ""].join(" ")}>
      <span
        className="text-cc-nav-label w-9 shrink-0 select-none pr-3 text-right font-mono text-[0.7rem] leading-6"
        aria-hidden
      >
        {n}
      </span>
      <code
        className="font-mono text-[0.78rem] leading-6"
        style={{ paddingLeft: `${indent * 0.9}rem`, color: SYN.punct }}
      >
        {children}
      </code>
    </div>
  );
}

/**
 * The summit source: the annotated Hot Chocolate `[QueryType]` partial class.
 * Everything on the page reads off this one file.
 */
function HeroEditor() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40 backdrop-blur-md">
      <WindowDots title="ProductApi.cs" meta="C#  ·  summit" />
      <div className="overflow-x-auto py-3">
        <CodeLine n={1}>
          <T c={SYN.attr}>[QueryType]</T>
        </CodeLine>
        <CodeLine n={2}>
          <T c={SYN.keyword}>public partial class</T> <T c={SYN.type}>ProductApi</T>
        </CodeLine>
        <CodeLine n={3}>{"{"}</CodeLine>
        <CodeLine n={4} indent={1}>
          <T c={SYN.keyword}>public static</T> <T c={SYN.type}>Product</T>{" "}
          <T c={SYN.member}>GetProduct</T>(
        </CodeLine>
        <CodeLine n={5} indent={2}>
          <T c={SYN.type}>int</T> id,
        </CodeLine>
        <CodeLine n={6} indent={2}>
          <T c={SYN.type}>ProductService</T> service)
        </CodeLine>
        <CodeLine n={7} indent={3}>
          {"=> service."}
          <T c={SYN.member}>ById</T>
          {"(id);"}
        </CodeLine>
        <CodeLine n={8}>{""}</CodeLine>
        <CodeLine n={9} indent={1} highlight>
          <T c={SYN.attr}>[DataLoader]</T>
        </CodeLine>
        <CodeLine n={10} indent={1} highlight>
          <T c={SYN.keyword}>internal static async</T> <T c={SYN.type}>Task</T>
          {"<"}
          <T c={SYN.type}>IReadOnlyDictionary</T>
          {"<"}
          <T c={SYN.type}>int</T>, <T c={SYN.type}>Product</T>
          {">>"}
        </CodeLine>
        <CodeLine n={11} indent={2} highlight>
          <T c={SYN.member}>GetProductsAsync</T>(
        </CodeLine>
        <CodeLine n={12} indent={3} highlight>
          <T c={SYN.type}>IReadOnlyList</T>
          {"<"}
          <T c={SYN.type}>int</T>
          {">"} keys,
        </CodeLine>
        <CodeLine n={13} indent={3} highlight>
          <T c={SYN.type}>ProductService</T> service)
        </CodeLine>
        <CodeLine n={14} indent={3} highlight>
          {"=> service."}
          <T c={SYN.member}>ByIds</T>
          {"(keys);"}
          <T c={SYN.comment}>{" // batches N keys, 1 fetch"}</T>
        </CodeLine>
        <CodeLine n={15}>{"}"}</CodeLine>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero summit ring: a closed contour wrapping the class card, stroked with  */
/*  the brand spectrum (the page's single spectrum event), animated as a slow */
/*  survey sweep via stroke-dashoffset. Honors reduced motion.                */
/* -------------------------------------------------------------------------- */

function SummitRing({ reduced }: { readonly reduced: boolean }) {
  return (
    <svg
      viewBox="0 0 600 600"
      className="pointer-events-none absolute -inset-8 -z-10 h-[calc(100%+4rem)] w-[calc(100%+4rem)]"
      fill="none"
      aria-hidden
      preserveAspectRatio="none"
    >
      <defs>
        <linearGradient id="summit-spectrum" x1="0" y1="0" x2="600" y2="600">
          <stop offset="0" stopColor={SPECTRUM_CYAN} />
          <stop offset="0.5" stopColor={SPECTRUM_VIOLET} />
          <stop offset="1" stopColor={SPECTRUM_CORAL} />
        </linearGradient>
      </defs>
      <motion.path
        d={blobPath(250, 16)}
        stroke="url(#summit-spectrum)"
        strokeWidth={1}
        strokeDasharray="10 14"
        opacity={0.55}
        initial={{ strokeDashoffset: 0 }}
        animate={reduced ? undefined : { strokeDashoffset: -240 }}
        transition={
          reduced
            ? undefined
            : { duration: 12, ease: "linear", repeat: Infinity }
        }
      />
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hover-outline card: a 1px contour outline fades in on hover via CSS only. */
/* -------------------------------------------------------------------------- */

interface SurveyCardProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function SurveyCard({ children, className }: SurveyCardProps) {
  return (
    <div
      className={[
        "group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative rounded-xl border backdrop-blur-sm transition-colors",
        className ?? "",
      ].join(" ")}
    >
      {/* contour outline that fades in on hover */}
      <span
        aria-hidden
        className="pointer-events-none absolute inset-1 rounded-lg opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        style={{ border: `1px solid ${ACCENT}40` }}
      />
      {children}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Artifact cards (used in the radial lineage)                               */
/* -------------------------------------------------------------------------- */

interface ArtifactHeaderProps {
  readonly title: string;
  readonly tag: string;
}

function ArtifactHeader({ title, tag }: ArtifactHeaderProps) {
  return (
    <div className="border-cc-card-border flex items-center justify-between border-b px-3.5 py-2.5">
      <span className="text-cc-heading font-mono text-[0.7rem]">{title}</span>
      <span
        className="rounded-full px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase"
        style={{
          color: ACCENT,
          backgroundColor: "rgba(94, 234, 212, 0.08)",
          border: "1px solid rgba(94, 234, 212, 0.22)",
        }}
      >
        {tag}
      </span>
    </div>
  );
}

function SdlArtifact() {
  return (
    <SurveyCard className="flex flex-col overflow-hidden">
      <ArtifactHeader title="schema.graphql" tag="generated" />
      <div className="p-3.5">
        <pre className="font-mono text-[0.72rem] leading-5">
          <code>
            <span style={{ color: SYN.keyword }}>type</span>{" "}
            <span style={{ color: SYN.type }}>Query</span> {"{"}
            {"\n  "}
            <span style={{ color: SYN.member }}>product</span>(
            <span style={{ color: SYN.arg }}>id</span>:{" "}
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
      </div>
    </SurveyCard>
  );
}

function BatchArtifact() {
  const keys = [41, 17, 88, 17, 41];
  return (
    <SurveyCard className="flex flex-col overflow-hidden">
      <ArtifactHeader title="DataLoader" tag="runtime" />
      <div className="flex grow flex-col p-3.5">
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

          <svg viewBox="0 0 64 120" className="h-28 w-16 shrink-0" aria-hidden fill="none">
            {[12, 33, 54, 75, 96].map((y, i) => (
              <path
                key={i}
                d={`M0 ${y} C 26 ${y}, 26 60, 60 60`}
                stroke={ACCENT}
                strokeWidth="1.5"
                opacity={0.85}
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
      </div>
    </SurveyCard>
  );
}

function ClientArtifact() {
  return (
    <SurveyCard className="flex flex-col overflow-hidden">
      <ArtifactHeader title="ProductClient.cs" tag="MSBuild" />
      <div className="p-3.5">
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
      </div>
    </SurveyCard>
  );
}

/* -------------------------------------------------------------------------- */
/*  Comparison plate                                                          */
/* -------------------------------------------------------------------------- */

type Cell = "good" | "warn" | "bad";

interface RowProps {
  readonly label: string;
  readonly handWired: Cell;
  readonly schemaFirst: Cell;
  readonly generated: Cell;
  readonly handWiredText: string;
  readonly schemaFirstText: string;
  readonly generatedText: string;
}

function CellMark({ kind, text }: { readonly kind: Cell; readonly text: string }) {
  const styles: Record<Cell, string> = {
    good: "text-cc-success",
    warn: "text-cc-warning",
    bad: "text-cc-danger",
  };
  const glyph: Record<Cell, string> = { good: "●", warn: "◐", bad: "○" };
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
    schemaFirstText: "DSL ≠ code",
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

function ComparisonPlate() {
  return (
    <Plate className="overflow-hidden">
      <div className="border-cc-card-border grid grid-cols-[1.1fr_1fr_1fr_1.1fr] border-b">
        <div className="px-4 py-3.5">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            approach
          </span>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3.5">
          <span className="text-cc-ink font-mono text-[0.72rem]">Hand-wired</span>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3.5">
          <span className="text-cc-ink font-mono text-[0.72rem]">Schema-first DSL</span>
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
    </Plate>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section heading helper                                                     */
/* -------------------------------------------------------------------------- */

interface SectionHeadProps {
  readonly title: ReactNode;
  readonly children?: ReactNode;
  readonly centered?: boolean;
}

function SectionHead({ title, children, centered = false }: SectionHeadProps) {
  return (
    <div className={centered ? "mx-auto max-w-2xl text-center" : "max-w-2xl"}>
      <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight">
        {title}
      </h2>
      {children ? (
        <p className="text-cc-prose mt-4 text-[1.05rem] leading-relaxed">{children}</p>
      ) : null}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  const reduced = useReducedMotion() ?? false;

  return (
    <div className="relative mx-auto flex max-w-[1100px] flex-col gap-28 py-6 sm:gap-36">
      {/* Page-level contour backdrop, centered behind the hero summit. */}
      <ContourField
        className="left-1/2 top-0 -z-10 h-[1100px] w-[1100px] -translate-x-1/2"
        style={{ transform: "translateX(-50%)" }}
      />
      {/* Second, smaller cluster behind the comparison section, offset right. */}
      <ContourField
        rings={10}
        className="-z-10 hidden h-[700px] w-[700px] lg:block"
        style={{ top: "62%", right: "-180px" }}
      />

      {/* ----------------------------- E-01 HERO ----------------------------- */}
      <section className="relative grid items-center gap-12 lg:grid-cols-[0.9fr_1.1fr]">
        <div>
          <Elevation mark="E-01">implementation-first GraphQL .NET</Elevation>
          <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
            Ship from the{" "}
            <span style={{ color: ACCENT }}>code that runs it.</span>
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-[1.15rem] leading-relaxed">
            Write your GraphQL API as annotated C#. Hot Chocolate generates the
            schema, the resolver pipeline, and DataLoaders straight from those
            classes, so the contract you publish is the implementation that
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

        {/* the summit: class card wrapped by the brand-spectrum survey ring */}
        <div className="relative">
          <SummitRing reduced={reduced} />
          <HeroEditor />
        </div>
      </section>

      {/* ------------------------- E-02 ELEVATION LEGEND ------------------------- */}
      <section className="relative">
        <Elevation mark="E-02">legend · loop as terrain</Elevation>
        <Plate className="mt-5 px-5 py-5 sm:px-8">
          <div className="grid grid-cols-2 gap-x-4 gap-y-6 sm:grid-cols-4 sm:gap-0">
            {[
              { alt: "1200 m", label: "annotate", note: "[QueryType] / [DataLoader]" },
              { alt: "1400 m", label: "generate", note: "schema + resolver pipeline" },
              { alt: "1600 m", label: "codegen", note: "typed .NET client" },
              { alt: "1800 m", label: "ship", note: "the contract you compiled" },
            ].map((step, i) => (
              <div
                key={step.label}
                className={[
                  "flex flex-col gap-1.5",
                  i > 0 ? "sm:border-cc-card-border sm:border-l sm:pl-6" : "",
                ].join(" ")}
              >
                <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] tabular-nums">
                  {step.alt}
                </span>
                <span className="flex items-center gap-2">
                  <span
                    className="h-2.5 w-px"
                    style={{ backgroundColor: ACCENT }}
                    aria-hidden
                  />
                  <span
                    className="font-mono text-[0.8rem] tracking-tight"
                    style={{ color: ACCENT }}
                  >
                    {step.label}
                  </span>
                </span>
                <span className="text-cc-ink-dim text-[0.78rem] leading-snug">
                  {step.note}
                </span>
              </div>
            ))}
          </div>
        </Plate>
      </section>

      {/* --------------------------- E-03 RADIAL LINEAGE --------------------------- */}
      <section className="relative">
        <Elevation mark="E-03">one source · three artifacts</Elevation>
        <SectionHead title="One class. Three derived artifacts.">
          The <code className="text-cc-ink font-mono">ProductApi</code> class at
          the summit is the only thing you maintain. At build time it fans out
          into the schema you serve, the batching that protects your database,
          and a typed client your callers consume.
        </SectionHead>

        <div className="mt-12">
          {/* central hub */}
          <div className="mb-6 flex justify-center">
            <span
              className="rounded-lg border px-4 py-2 font-mono text-[0.78rem]"
              style={{
                color: ACCENT,
                borderColor: "rgba(94,234,212,0.3)",
                backgroundColor: "rgba(94,234,212,0.05)",
              }}
            >
              [QueryType] ProductApi
            </span>
          </div>

          {/* contour-following tracers, drawn once when in view */}
          <motion.svg
            viewBox="0 0 600 56"
            className="mx-auto block h-14 w-full max-w-3xl"
            preserveAspectRatio="none"
            aria-hidden
            fill="none"
            initial="hidden"
            whileInView="shown"
            viewport={{ once: true, amount: 0.6 }}
          >
            {[
              "M300 0 C 300 34, 100 18, 100 56",
              "M300 0 L300 56",
              "M300 0 C 300 34, 500 18, 500 56",
            ].map((d, i) => (
              <motion.path
                key={i}
                d={d}
                stroke={ACCENT}
                strokeWidth="1.5"
                opacity={0.7}
                variants={{
                  hidden: { pathLength: reduced ? 1 : 0 },
                  shown: { pathLength: 1 },
                }}
                transition={{ duration: 0.7, ease: "easeOut" }}
              />
            ))}
          </motion.svg>

          <div className="mt-3 grid gap-5 md:grid-cols-3">
            <SdlArtifact />
            <BatchArtifact />
            <ClientArtifact />
          </div>
        </div>
      </section>

      {/* ------------------------- E-04 BUILD-TIME SURVEY ------------------------- */}
      <section className="relative">
        <Elevation mark="E-04">build-time survey · derived, not hand-edited</Elevation>
        <SectionHead title="Add a field once. The map redraws at build.">
          A save triggers the same generation your CI runs. The schema is
          re-derived and Strawberry Shake regenerates the typed client from your
          operations, all before anything hits a runtime path.
        </SectionHead>

        <div className="mt-10 grid items-stretch gap-3 lg:grid-cols-[1fr_auto_1fr]">
          <Plate className="overflow-hidden">
            <WindowDots title="ProductApi.cs" meta="edit" />
            <div className="py-3">
              <CodeLine n={4} indent={1}>
                <T c={SYN.keyword}>public static</T> <T c={SYN.type}>Product</T>{" "}
                <T c={SYN.member}>GetProduct</T>(...)
              </CodeLine>
              <CodeLine n={5} indent={1} highlight>
                <T c={SYN.keyword}>public static</T> <T c={SYN.type}>string</T>{" "}
                <T c={SYN.member}>Sku</T>(
                <T c={SYN.type}>Product</T> p)
              </CodeLine>
              <CodeLine n={6} indent={2} highlight>
                {"=> p."}
                <T c={SYN.member}>Sku</T>;
                <T c={SYN.comment}>{" // new field"}</T>
              </CodeLine>
            </div>
          </Plate>

          {/* single hairline marking "derived at build" */}
          <div className="flex items-center justify-center">
            <svg viewBox="0 0 56 24" className="h-6 w-14" aria-hidden fill="none">
              <path d="M0 12 L48 12" stroke={ACCENT} strokeWidth="1" opacity={0.6} />
              <path
                d="M44 7 L52 12 L44 17"
                stroke={ACCENT}
                strokeWidth="1"
                opacity={0.6}
              />
            </svg>
          </div>

          <Plate className="overflow-hidden">
            <WindowDots title="schema.graphql + client" meta="derived" />
            <div className="py-3">
              <CodeLine n={1} indent={1}>
                <span style={{ color: SYN.member }}>id</span>:{" "}
                <span style={{ color: SYN.type }}>Int!</span>
              </CodeLine>
              <CodeLine n={2} indent={1} highlight>
                <span style={{ color: SYN.member }}>sku</span>:{" "}
                <span style={{ color: SYN.type }}>String!</span>
                <T c={SYN.comment}>{" # added"}</T>
              </CodeLine>
              <CodeLine n={3} indent={1}>
                <span style={{ color: SYN.member }}>name</span>:{" "}
                <span style={{ color: SYN.type }}>String!</span>
              </CodeLine>
              <CodeLine n={4} indent={1} highlight>
                result.Data.Product.<T c={SYN.member}>Sku</T>
                <T c={SYN.comment}>{" // typed"}</T>
              </CodeLine>
            </div>
          </Plate>
        </div>
      </section>

      {/* --------------------------- E-05 COLLAPSE --------------------------- */}
      <section className="relative grid items-center gap-12 lg:grid-cols-[1fr_1fr]">
        <div>
          <Elevation mark="E-05">fewer glue layers</Elevation>
          <SectionHead title="Fewer files between you and the contract.">
            Schema-first stacks ask you to keep a DSL, a resolver map, and a
            client schema in step by hand. Implementation-first removes those
            seams: the annotation is the binding, so there is nothing in between
            to fall out of date.
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

        <Plate className="overflow-hidden">
          <WindowDots title="before  →  after" />
          <div className="grid grid-cols-[1fr_auto_0.8fr] items-center gap-3 p-5">
            <div className="flex flex-col gap-2">
              {["schema.graphql", "Resolvers.cs", "client.schema", "mappings"].map(
                (l) => (
                  <span
                    key={l}
                    className="text-cc-ink-dim border-cc-card-border bg-cc-surface/60 rounded-md border border-dashed px-2.5 py-1.5 text-center font-mono text-[0.66rem]"
                  >
                    {l}
                  </span>
                ),
              )}
            </div>

            {/* contour curves converging to one tile */}
            <svg viewBox="0 0 56 120" className="h-32 w-14" aria-hidden fill="none">
              {[18, 46, 74, 102].map((y, i) => (
                <path
                  key={i}
                  d={`M0 ${y} C 28 ${y}, 28 60, 54 60`}
                  stroke={ACCENT}
                  strokeWidth="1.5"
                  opacity={0.8}
                />
              ))}
            </svg>

            <span
              className="rounded-lg border px-2.5 py-3 text-center font-mono text-[0.66rem] leading-tight"
              style={{
                color: ACCENT,
                borderColor: "rgba(94,234,212,0.32)",
                backgroundColor: "rgba(94,234,212,0.06)",
              }}
            >
              ProductApi
              <br />
              .cs
            </span>
          </div>
        </Plate>
      </section>

      {/* ------------------------- E-06 COMPARISON PLATE ------------------------- */}
      <section className="relative">
        <Elevation mark="E-06">the difference, line by line</Elevation>
        <SectionHead title="Three ways to build a GraphQL API. One keeps the contract honest." />
        <div className="mt-10">
          <ComparisonPlate />
        </div>
        <p className="text-cc-nav-label mt-4 font-mono text-[0.66rem] tracking-tight">
          ● strong · ◐ partial · ○ weak, based on what each approach maintains by
          hand.
        </p>
      </section>

      {/* --------------------------- E-07 HONESTY BAND --------------------------- */}
      <section className="relative">
        <Elevation mark="E-07">where the survey ends</Elevation>
        <div className="border-cc-card-border bg-cc-surface/50 mt-5 rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
          <h2 className="font-heading text-h4 text-cc-heading mt-1 max-w-3xl font-semibold tracking-tight">
            Generation removes drift inside your service. It does not freeze the
            world outside it.
          </h2>
          <div className="mt-7 grid gap-6 sm:grid-cols-2">
            <p className="text-cc-prose text-[1rem] leading-relaxed">
              One annotated class means the schema, resolvers, and DataLoaders
              cannot disagree, because they are derived from the same code. That
              is a real, narrow guarantee, and it is the one we make.
            </p>
            <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
              It is not a promise about consumers you do not control. When a type
              changes, the schema diff identifies the published clients affected,
              so you can coordinate the rollout rather than discovering it in
              production.
            </p>
          </div>
        </div>
      </section>

      {/* ---------------------------- E-08 CTA SUMMIT ---------------------------- */}
      <section className="relative flex flex-col items-center gap-7 py-6 text-center">
        {/* small mirror of the hero contour cluster to close the loop */}
        <ContourField
          rings={9}
          className="left-1/2 top-1/2 -z-10 h-[520px] w-[520px] -translate-x-1/2 -translate-y-1/2"
          style={{ transform: "translate(-50%, -50%)" }}
        />
        <Elevation mark="E-08">close the loop</Elevation>
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Start at the class. Ship the contract.
        </h2>
        <p className="text-cc-prose max-w-xl text-[1.1rem] leading-relaxed">
          Build your first implementation-first GraphQL API in C# and watch the
          schema, batching, and a typed client appear from the code you already
          wrote.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
