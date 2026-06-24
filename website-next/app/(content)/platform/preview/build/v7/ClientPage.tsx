"use client";

import type { ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import {
  AnimatePresence,
  MotionConfig,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Scene accent                                                              */
/*  Single brand color event: cc-accent (#5eead4), paired with cyan deep      */
/*  (#16b9e4) only on rail gradients (still inside the brand family).         */
/* -------------------------------------------------------------------------- */

const ACCENT = "#5eead4";
const ACCENT_DEEP = "#16b9e4";

/* -------------------------------------------------------------------------- */
/*  Shared chrome                                                             */
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

/* -------------------------------------------------------------------------- */
/*  Editor tokens                                                             */
/* -------------------------------------------------------------------------- */

const SYN = {
  attr: "#7ee787",
  keyword: "#ff7b72",
  type: "#79c0ff",
  member: "#d2a8ff",
  string: "#a5d6ff",
  comment: "#8b949e",
  punct: "#c9d1d9",
};

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
    <div
      className={[
        "flex items-start",
        highlight ? "bg-[#5eead4]/[0.06]" : "",
      ].join(" ")}
    >
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

function HeroEditor() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40 backdrop-blur-md">
      <WindowDots title="ProductApi.cs" meta="C#  ·  saved" />
      <div className="overflow-x-auto py-3">
        <CodeLine n={1}>
          <T c={SYN.attr}>[QueryType]</T>
        </CodeLine>
        <CodeLine n={2}>
          <T c={SYN.keyword}>public partial class</T>{" "}
          <T c={SYN.type}>ProductApi</T>
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
          <T c={SYN.keyword}>internal static async</T>{" "}
          <T c={SYN.type}>Task</T>
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
/*  Section heading helper                                                    */
/* -------------------------------------------------------------------------- */

interface SectionHeadProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function SectionHead({ eyebrow, title, children }: SectionHeadProps) {
  return (
    <div className="max-w-2xl">
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
/*  THE FORGE: centerpiece animated codegen pipeline                          */
/* -------------------------------------------------------------------------- */

const SDL_TOKENS: readonly string[] = [
  "type Query {",
  "  product(id: Int!): Product",
  "}",
  "",
  "type Product {",
  "  id: Int!",
  "  name: String!",
  "}",
];

const CLIENT_TOKENS: readonly string[] = [
  "var result =",
  "  await client",
  "    .GetProduct",
  "    .ExecuteAsync(id);",
  "",
  "string name =",
  "  result.Data.Product.Name;",
];

const BATCH_KEYS: readonly number[] = [41, 17, 88, 17, 41];

interface ForgePartProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

function Forge() {
  const reduced = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-80px" });
  const active = reduced ? true : inView;

  return (
    <div ref={ref} className="relative">
      <div
        className="pointer-events-none absolute -inset-8 -z-10 rounded-3xl opacity-30 blur-3xl"
        style={{
          background: `radial-gradient(50% 50% at 30% 50%, ${ACCENT}33, transparent 70%)`,
        }}
        aria-hidden
      />
      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm">
        <WindowDots title="forge · ProductApi to 3 artifacts" meta="motion" />
        <div className="relative grid items-stretch gap-6 p-6 lg:grid-cols-[1fr_auto_1fr]">
          <ForgeSource active={active} reduced={reduced} />
          <ForgeRails active={active} reduced={reduced} />
          <div className="flex flex-col gap-4">
            <SdlPanel active={active} reduced={reduced} />
            <BatchPanel active={active} reduced={reduced} />
            <ClientPanel active={active} reduced={reduced} />
          </div>
        </div>
        <ForgeCounter active={active} reduced={reduced} />
      </div>
    </div>
  );
}

function ForgeSource({ active }: ForgePartProps) {
  return (
    <motion.div
      initial={{ opacity: 0, x: -8 }}
      animate={active ? { opacity: 1, x: 0 } : { opacity: 0, x: -8 }}
      transition={{ duration: 0.4 }}
      className="border-cc-card-border bg-cc-code-bg/95 flex flex-col overflow-hidden rounded-xl border"
    >
      <div className="border-cc-card-border flex items-center justify-between border-b px-3.5 py-2.5">
        <span className="text-cc-heading font-mono text-[0.7rem]">
          ProductApi.cs
        </span>
        <span
          className="rounded-full px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase"
          style={{
            color: ACCENT,
            backgroundColor: "rgba(94, 234, 212, 0.08)",
            border: "1px solid rgba(94, 234, 212, 0.22)",
          }}
        >
          source
        </span>
      </div>
      <div className="grow py-2">
        <CodeLine n={1}>
          <T c={SYN.attr}>[QueryType]</T>
        </CodeLine>
        <CodeLine n={2}>
          <T c={SYN.keyword}>public partial class</T>{" "}
          <T c={SYN.type}>ProductApi</T>
        </CodeLine>
        <CodeLine n={3}>{"{"}</CodeLine>
        <CodeLine n={4} indent={1}>
          <T c={SYN.keyword}>public static</T> <T c={SYN.type}>Product</T>{" "}
          <T c={SYN.member}>GetProduct</T>(<T c={SYN.type}>int</T> id)
        </CodeLine>
        <CodeLine n={5} indent={2}>
          {"=> svc."}
          <T c={SYN.member}>ById</T>
          {"(id);"}
        </CodeLine>
        <CodeLine n={6} indent={1}>
          <T c={SYN.attr}>[DataLoader]</T>
        </CodeLine>
        <CodeLine n={7} indent={1}>
          <T c={SYN.keyword}>static async</T>{" "}
          <T c={SYN.member}>GetProductsAsync</T>
          {"(keys)"}
        </CodeLine>
        <CodeLine n={8} indent={2}>
          {"=> svc."}
          <T c={SYN.member}>ByIds</T>
          {"(keys);"}
        </CodeLine>
        <CodeLine n={9}>{"}"}</CodeLine>
      </div>
      <div className="border-cc-card-border border-t px-3.5 py-2">
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-tight">
          1 source · annotated
        </span>
      </div>
    </motion.div>
  );
}

function ForgeRails({ active, reduced }: ForgePartProps) {
  const paths = [
    "M0 28 C 60 28, 60 60, 120 60",
    "M0 28 C 60 28, 60 160, 120 160",
    "M0 28 C 60 28, 60 260, 120 260",
  ];
  const endY = [60, 160, 260];
  return (
    <div className="hidden lg:flex lg:items-stretch lg:justify-center">
      <svg
        viewBox="0 0 120 320"
        preserveAspectRatio="none"
        className="h-full w-24"
        aria-hidden
        fill="none"
      >
        <defs>
          <linearGradient id="forge-rail" x1="0" y1="0" x2="120" y2="0">
            <stop offset="0" stopColor={ACCENT_DEEP} />
            <stop offset="1" stopColor={ACCENT} />
          </linearGradient>
        </defs>
        {paths.map((d, i) => (
          <motion.path
            key={d}
            d={d}
            stroke="url(#forge-rail)"
            strokeWidth="1.6"
            strokeLinecap="round"
            initial={{ pathLength: reduced ? 1 : 0, opacity: 0.85 }}
            animate={
              active ? { pathLength: 1, opacity: 0.95 } : { pathLength: 0 }
            }
            transition={{
              duration: reduced ? 0 : 1.1,
              delay: reduced ? 0 : 0.2 + i * 0.18,
              ease: "easeInOut",
            }}
          />
        ))}
        {endY.map((cy, i) => (
          <motion.circle
            key={`dot-${i}`}
            cx="119"
            cy={cy}
            r="3"
            fill={ACCENT}
            initial={{ opacity: 0 }}
            animate={active ? { opacity: 1 } : { opacity: 0 }}
            transition={{
              duration: 0.3,
              delay: reduced ? 0 : 1.2 + i * 0.18,
            }}
          />
        ))}
      </svg>
    </div>
  );
}

/* ---------- SDL output (typing tokens) ---------- */

function SdlPanel({ active, reduced }: ForgePartProps) {
  const [idx, setIdx] = useState(reduced ? SDL_TOKENS.length : 0);

  useEffect(() => {
    if (!active || reduced) {
      return;
    }
    if (idx >= SDL_TOKENS.length) {
      return;
    }
    const t = setTimeout(() => setIdx((v) => v + 1), 180 + idx * 30);
    return () => clearTimeout(t);
  }, [active, reduced, idx]);

  return (
    <ArtifactPanel tag="generated" title="schema.graphql" delay={0.4}>
      <pre className="font-mono text-[0.7rem] leading-5">
        <code>
          {SDL_TOKENS.slice(0, idx).map((line, i) => (
            <span key={i}>
              {colorizeSdl(line)}
              {"\n"}
            </span>
          ))}
          {!reduced && idx < SDL_TOKENS.length ? (
            <motion.span
              animate={{ opacity: [1, 0.1, 1] }}
              transition={{ repeat: Infinity, duration: 0.9 }}
              style={{ color: ACCENT }}
            >
              ▍
            </motion.span>
          ) : null}
        </code>
      </pre>
    </ArtifactPanel>
  );
}

function colorizeSdl(line: string) {
  if (line.startsWith("type ")) {
    const rest = line.slice(5);
    const head = rest.split(" ")[0];
    const tail = rest.slice(head.length);
    return (
      <>
        <span style={{ color: SYN.keyword }}>type</span>{" "}
        <span style={{ color: SYN.type }}>{head}</span>
        <span>{tail}</span>
      </>
    );
  }
  if (line.trim() === "}" || line.trim() === "") {
    return <span>{line}</span>;
  }
  const m = line.match(/^(\s*)(\w+)(\(.+?\))?(:\s*)(.+)$/);
  if (m) {
    return (
      <>
        <span>{m[1]}</span>
        <span style={{ color: SYN.member }}>{m[2]}</span>
        {m[3] ? <span style={{ color: SYN.punct }}>{m[3]}</span> : null}
        <span>{m[4]}</span>
        <span style={{ color: SYN.type }}>{m[5]}</span>
      </>
    );
  }
  return <span>{line}</span>;
}

/* ---------- DataLoader batch panel ---------- */

function BatchPanel({ active, reduced }: ForgePartProps) {
  return (
    <ArtifactPanel tag="runtime" title="DataLoader" delay={0.7}>
      <div className="flex items-center justify-between gap-3">
        <div className="flex flex-col gap-1.5">
          {BATCH_KEYS.map((k, i) => (
            <motion.span
              key={`${k}-${i}`}
              initial={{ opacity: 0, x: -8 }}
              animate={active ? { opacity: 1, x: 0 } : { opacity: 0, x: -8 }}
              transition={{
                duration: reduced ? 0 : 0.32,
                delay: reduced ? 0 : 0.9 + i * 0.08,
              }}
              className="text-cc-ink-dim border-cc-card-border bg-cc-surface rounded-md border px-2 py-0.5 text-center font-mono text-[0.62rem] tabular-nums"
            >
              {k}
            </motion.span>
          ))}
        </div>

        <svg
          viewBox="0 0 64 110"
          className="h-24 w-14 shrink-0"
          aria-hidden
          fill="none"
        >
          <defs>
            <linearGradient id="batch-wire-v7" x1="0" y1="0" x2="64" y2="0">
              <stop offset="0" stopColor={ACCENT_DEEP} />
              <stop offset="1" stopColor={ACCENT} />
            </linearGradient>
          </defs>
          {[10, 30, 50, 70, 90].map((y, i) => (
            <motion.path
              key={i}
              d={`M0 ${y} C 26 ${y}, 26 55, 60 55`}
              stroke="url(#batch-wire-v7)"
              strokeWidth="1.4"
              initial={{ pathLength: reduced ? 1 : 0, opacity: 0.85 }}
              animate={
                active
                  ? { pathLength: 1, opacity: 0.9 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{
                duration: reduced ? 0 : 0.55,
                delay: reduced ? 0 : 1.25 + i * 0.07,
                ease: "easeInOut",
              }}
            />
          ))}
          <motion.circle
            cx="61"
            cy="55"
            r="3"
            fill={ACCENT}
            initial={{ opacity: 0 }}
            animate={active ? { opacity: 1 } : { opacity: 0 }}
            transition={{ duration: 0.3, delay: reduced ? 0 : 1.85 }}
          />
        </svg>

        <motion.span
          initial={{ opacity: 0, scale: 0.92 }}
          animate={
            active ? { opacity: 1, scale: 1 } : { opacity: 0, scale: 0.92 }
          }
          transition={{
            duration: reduced ? 0 : 0.4,
            delay: reduced ? 0 : 1.95,
          }}
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
        </motion.span>
      </div>
      <p className="text-cc-nav-label border-cc-card-border mt-2.5 border-t pt-2 font-mono text-[0.58rem] tracking-tight">
        5 keys, deduped to 3 ids, 1 fetch
      </p>
    </ArtifactPanel>
  );
}

/* ---------- Client output (line-by-line) ---------- */

function ClientPanel({ active, reduced }: ForgePartProps) {
  const [idx, setIdx] = useState(reduced ? CLIENT_TOKENS.length : 0);

  useEffect(() => {
    if (!active || reduced) {
      return;
    }
    if (idx >= CLIENT_TOKENS.length) {
      return;
    }
    const t = setTimeout(() => setIdx((v) => v + 1), 220 + idx * 30);
    return () => clearTimeout(t);
  }, [active, reduced, idx]);

  return (
    <ArtifactPanel tag="MSBuild" title="ProductClient.cs" delay={1.0}>
      <pre className="font-mono text-[0.7rem] leading-5">
        <code>
          <AnimatePresence initial={false}>
            {CLIENT_TOKENS.slice(0, idx).map((line, i) => (
              <motion.span
                key={`${i}-${line}`}
                initial={{ opacity: 0, y: 4 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.25 }}
                style={{ display: "block" }}
              >
                {colorizeClient(line)}
              </motion.span>
            ))}
          </AnimatePresence>
        </code>
      </pre>
    </ArtifactPanel>
  );
}

function colorizeClient(line: string) {
  if (line.trim() === "") {
    return <span>{line || " "}</span>;
  }
  if (line.startsWith("var ")) {
    return (
      <>
        <span style={{ color: SYN.keyword }}>var</span>
        <span>{line.slice(3)}</span>
      </>
    );
  }
  if (line.startsWith("string ")) {
    return (
      <>
        <span style={{ color: SYN.keyword }}>string</span>
        <span>{line.slice(6)}</span>
      </>
    );
  }
  if (line.includes("await ")) {
    const [pre, post] = line.split("await ");
    return (
      <>
        <span>{pre}</span>
        <span style={{ color: SYN.keyword }}>await</span>
        <span> {post}</span>
      </>
    );
  }
  if (line.includes(".GetProduct") || line.includes(".ExecuteAsync")) {
    const m = line.match(/^(\s*\.)(\w+)(.*)$/);
    if (m) {
      return (
        <>
          <span>{m[1]}</span>
          <span style={{ color: SYN.member }}>{m[2]}</span>
          <span>{m[3]}</span>
        </>
      );
    }
  }
  if (line.includes("result.Data.Product.Name")) {
    return (
      <>
        <span>{"  result.Data."}</span>
        <span style={{ color: SYN.member }}>Product</span>
        <span>.</span>
        <span style={{ color: SYN.member }}>Name</span>
        <span>;</span>
        <span style={{ color: SYN.comment }}>{" // typed"}</span>
      </>
    );
  }
  return <span>{line}</span>;
}

/* ---------- Artifact wrapper used by the three forge outputs ---------- */

interface ArtifactPanelProps {
  readonly tag: string;
  readonly title: string;
  readonly delay: number;
  readonly children: ReactNode;
}

function ArtifactPanel({ tag, title, delay, children }: ArtifactPanelProps) {
  return (
    <motion.div
      initial={{ opacity: 0, x: 8 }}
      whileInView={{ opacity: 1, x: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.45, delay }}
      className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border"
    >
      <div className="border-cc-card-border flex items-center justify-between border-b px-3.5 py-2">
        <span className="text-cc-heading font-mono text-[0.68rem]">{title}</span>
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
      <div className="p-3">{children}</div>
    </motion.div>
  );
}

/* ---------- Forge counter ---------- */

function ForgeCounter({ active, reduced }: ForgePartProps) {
  const [artifacts, setArtifacts] = useState(reduced ? 3 : 0);

  useEffect(() => {
    if (!active || reduced) {
      return;
    }
    if (artifacts >= 3) {
      return;
    }
    const t = setTimeout(() => setArtifacts((v) => v + 1), 700 + artifacts * 600);
    return () => clearTimeout(t);
  }, [active, reduced, artifacts]);

  return (
    <div className="border-cc-card-border flex items-center justify-between border-t px-5 py-3">
      <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.2em] uppercase">
        forge · counter
      </span>
      <span className="text-cc-heading font-mono text-[0.78rem] tabular-nums">
        <span style={{ color: ACCENT }}>{artifacts}</span> artifacts · 1 source
      </span>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Build ribbon: staggered timeline                                          */
/* -------------------------------------------------------------------------- */

interface StageItem {
  readonly index: number;
  readonly label: string;
  readonly note: string;
}

const STAGES: readonly StageItem[] = [
  {
    index: 1,
    label: "annotate",
    note: "Add [QueryType] / [DataLoader] to a partial class.",
  },
  {
    index: 2,
    label: "generate",
    note: "Source generators emit the schema and resolver pipeline.",
  },
  {
    index: 3,
    label: "codegen",
    note: "Strawberry Shake (MSBuild) builds a typed .NET client from your operations.",
  },
  {
    index: 4,
    label: "ship",
    note: "The contract you publish is the code that just compiled.",
  },
];

function BuildRibbon() {
  return (
    <motion.ol
      initial="hidden"
      whileInView="show"
      viewport={{ once: true, margin: "-80px" }}
      variants={{
        hidden: {},
        show: { transition: { staggerChildren: 0.18 } },
      }}
      className="flex flex-col gap-8 sm:flex-row sm:gap-0"
    >
      {STAGES.map((s, i) => (
        <motion.li
          key={s.index}
          variants={{
            hidden: { opacity: 0, y: 8 },
            show: { opacity: 1, y: 0 },
          }}
          transition={{ duration: 0.45 }}
          className="relative flex flex-1 flex-col"
        >
          <div className="flex items-center">
            <motion.span
              variants={{
                hidden: { scale: 0.6, opacity: 0 },
                show: { scale: 1, opacity: 1 },
              }}
              transition={{ duration: 0.4, ease: "easeOut" }}
              className="relative z-10 flex h-7 w-7 shrink-0 items-center justify-center rounded-full font-mono text-[0.7rem] font-semibold"
              style={{
                color: "#0b0f1a",
                background: `linear-gradient(135deg, ${ACCENT_DEEP}, ${ACCENT})`,
              }}
            >
              {s.index}
            </motion.span>
            {i < STAGES.length - 1 ? (
              <motion.span
                variants={{
                  hidden: { scaleX: 0 },
                  show: { scaleX: 1 },
                }}
                transition={{ duration: 0.55, ease: "easeOut" }}
                style={{
                  transformOrigin: "0% 50%",
                  background:
                    "linear-gradient(90deg, rgba(94,234,212,0.5), rgba(94,234,212,0.12))",
                }}
                className="h-px flex-1"
                aria-hidden
              />
            ) : null}
          </div>
          <p className="text-cc-heading mt-3 font-mono text-[0.72rem] tracking-tight">
            {s.label}
          </p>
          <p className="text-cc-ink-dim mt-1 pr-4 text-[0.78rem] leading-snug">
            {s.note}
          </p>
        </motion.li>
      ))}
    </motion.ol>
  );
}

/* -------------------------------------------------------------------------- */
/*  Glue collapse                                                             */
/* -------------------------------------------------------------------------- */

function GlueCollapse() {
  const labels = ["schema.graphql", "Resolvers.cs", "client.schema", "mappings"];
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm">
      <WindowDots title="before  to  after" />
      <div className="grid grid-cols-[1fr_auto_0.8fr] items-center gap-3 p-5">
        <motion.div
          initial="hidden"
          whileInView="show"
          viewport={{ once: true, margin: "-80px" }}
          variants={{ hidden: {}, show: { transition: { staggerChildren: 0.1 } } }}
          className="flex flex-col gap-2"
        >
          {labels.map((l) => (
            <motion.span
              key={l}
              variants={{
                hidden: { opacity: 0, x: 0 },
                show: { opacity: 1, x: 0 },
              }}
              animate={{ x: [0, 28, 28] }}
              transition={{
                duration: 1.6,
                times: [0, 0.55, 1],
                repeat: 0,
              }}
              className="text-cc-ink-dim border-cc-card-border bg-cc-surface/60 rounded-md border border-dashed px-2.5 py-1.5 text-center font-mono text-[0.66rem]"
            >
              {l}
            </motion.span>
          ))}
        </motion.div>

        <svg viewBox="0 0 56 120" className="h-32 w-14" aria-hidden fill="none">
          <defs>
            <linearGradient id="collapse-v7" x1="0" y1="0" x2="56" y2="0">
              <stop offset="0" stopColor={ACCENT_DEEP} />
              <stop offset="1" stopColor={ACCENT} />
            </linearGradient>
          </defs>
          {[18, 46, 74, 102].map((y, i) => (
            <motion.path
              key={i}
              d={`M0 ${y} C 28 ${y}, 28 60, 54 60`}
              stroke="url(#collapse-v7)"
              strokeWidth="1.5"
              opacity={0.8}
              initial={{ pathLength: 0 }}
              whileInView={{ pathLength: 1 }}
              viewport={{ once: true, margin: "-80px" }}
              transition={{ duration: 0.7, delay: 0.1 + i * 0.08 }}
            />
          ))}
        </svg>

        <motion.span
          initial={{ opacity: 0, scale: 0.9 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true, margin: "-80px" }}
          transition={{ duration: 0.5, delay: 0.6 }}
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
        </motion.span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Stat tile with count-up                                                   */
/* -------------------------------------------------------------------------- */

interface StatProps {
  readonly value: string;
  readonly label: string;
  readonly count?: number;
}

function Stat({ value, label, count }: StatProps) {
  const reduced = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-80px" });
  const [n, setN] = useState(reduced || count === undefined ? (count ?? 0) : 0);

  useEffect(() => {
    if (count === undefined) {
      return;
    }
    if (!inView || reduced) {
      return;
    }
    let raf = 0;
    const start = performance.now();
    const duration = 900;
    const tick = (t: number) => {
      const p = Math.min(1, (t - start) / duration);
      setN(Math.round(count * (1 - Math.pow(1 - p, 3))));
      if (p < 1) {
        raf = requestAnimationFrame(tick);
      }
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [inView, reduced, count]);

  const display = count === undefined ? value : value.replace("{n}", String(n));

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 6 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.4 }}
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 backdrop-blur-sm"
    >
      <p
        className="font-heading text-h3 tabular-nums"
        style={{ color: ACCENT }}
      >
        {display}
      </p>
      <p className="text-cc-ink-dim mt-2 text-[0.92rem] leading-snug">{label}</p>
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Comparison table: static, soft stagger only                               */
/* -------------------------------------------------------------------------- */

type Cell = "good" | "warn" | "bad";

interface RowDef {
  readonly label: string;
  readonly handWired: Cell;
  readonly schemaFirst: Cell;
  readonly generated: Cell;
  readonly handWiredText: string;
  readonly schemaFirstText: string;
  readonly generatedText: string;
}

const COMPARISON: readonly RowDef[] = [
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

interface CellMarkProps {
  readonly kind: Cell;
  readonly text: string;
}

function CellMark({ kind, text }: CellMarkProps) {
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

function ComparisonTable() {
  return (
    <motion.div
      initial="hidden"
      whileInView="show"
      viewport={{ once: true, margin: "-80px" }}
      variants={{
        hidden: {},
        show: { transition: { staggerChildren: 0.06 } },
      }}
      className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm"
    >
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
        <motion.div
          key={row.label}
          variants={{
            hidden: { opacity: 0, y: 4 },
            show: { opacity: 1, y: 0 },
          }}
          transition={{ duration: 0.35 }}
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
        </motion.div>
      ))}
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page body                                                                 */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <div className="flex flex-col gap-28 py-6 sm:gap-36">
        {/* ----------------------------- HERO ----------------------------- */}
        <motion.section
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="grid items-center gap-12 lg:grid-cols-[0.9fr_1.1fr]"
        >
          <div>
            <Eyebrow>Build loop · codegen forge</Eyebrow>
            <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
              The class is the spark.{" "}
              <span
                className="bg-clip-text text-transparent"
                style={{
                  backgroundImage: `linear-gradient(100deg, ${ACCENT_DEEP}, ${ACCENT})`,
                }}
              >
                The forge does the rest.
              </span>
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-[1.15rem] leading-relaxed">
              Implementation-first GraphQL .NET. One annotated C# class lights
              up Hot Chocolate source generation, which forges the SDL, the
              DataLoader batching, and a Strawberry Shake typed .NET client
              from the code that actually answers the request.
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

          <div className="relative">
            <div
              className="pointer-events-none absolute -inset-6 -z-10 rounded-3xl opacity-40 blur-2xl"
              style={{
                background: `radial-gradient(60% 60% at 30% 20%, ${ACCENT_DEEP}33, transparent 70%)`,
              }}
              aria-hidden
            />
            <HeroEditor />
          </div>
        </motion.section>

        {/* --------------------------- THE FORGE --------------------------- */}
        <section>
          <SectionHead
            eyebrow="The forge · one source, three artifacts"
            title="Watch the codegen pipeline light up."
          >
            Scroll the source into view and the rails draw. SDL types itself
            into a schema, five keys flow into a DataLoader and collapse into a
            single batched call, and a typed Strawberry Shake client appears
            line by line. One class. Three artifacts. No glue file in between.
          </SectionHead>
          <div className="mt-10">
            <Forge />
          </div>
        </section>

        {/* ------------------------- BUILD RIBBON ------------------------- */}
        <section>
          <SectionHead
            eyebrow="MSBuild codegen · .graphql to typed .NET"
            title="The loop closes at build, not at the first failing request."
          >
            A save triggers the same generation your CI runs. The schema is
            re-derived, DataLoaders are rebuilt, and Strawberry Shake MSBuild
            codegen regenerates the typed client from your operations, all
            before anything hits a runtime path.
          </SectionHead>

          <div className="border-cc-card-border bg-cc-card-bg mt-10 rounded-2xl border p-6 backdrop-blur-sm sm:p-8">
            <BuildRibbon />
          </div>
        </section>

        {/* -------------------------- COLLAPSE ---------------------------- */}
        <section className="grid items-center gap-12 lg:grid-cols-[1fr_1fr]">
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

          <GlueCollapse />
        </section>

        {/* ----------------------------- STATS ----------------------------- */}
        <section>
          <SectionHead
            eyebrow="What the forge produces"
            title="One annotated class. The artifacts that fall out of it."
          />
          <div className="mt-10 grid gap-5 md:grid-cols-3">
            <Stat value="1" label="source of truth, the annotated C# class." />
            <Stat
              value="0"
              label="hand-written DSL files to keep in step with code."
            />
            <Stat
              value="{n} keys, 1 fetch"
              count={5}
              label="DataLoader batching, generated from [DataLoader]."
            />
          </div>
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
            ● strong, ◐ partial, ○ weak. Based on what each approach maintains
            by hand.
          </p>
        </section>

        {/* -------------------------- HONESTY ----------------------------- */}
        <section className="border-cc-card-border bg-cc-surface/50 rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
          <Eyebrow>Where the line is</Eyebrow>
          <h2 className="font-heading text-h4 text-cc-heading mt-3 max-w-3xl font-semibold tracking-tight">
            Generation removes drift inside your service. It does not freeze
            the world outside it.
          </h2>
          <div className="mt-7 grid gap-6 sm:grid-cols-2">
            <p className="text-cc-prose text-[1rem] leading-relaxed">
              One annotated class means the schema, resolvers, and DataLoaders
              cannot disagree, because they are derived from the same code.
              That is a real, narrow guarantee, and it is the one we make.
            </p>
            <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
              It is not a promise about consumers you do not control. When a
              type changes, the schema diff tells you which published clients
              are affected so you can coordinate the rollout, rather than
              discovering it in production.
            </p>
          </div>
        </section>

        {/* ---------------------------- CTA ------------------------------- */}
        <section className="relative flex flex-col items-center gap-7 py-6 text-center">
          <motion.div
            initial={{ scaleX: 0, opacity: 0 }}
            whileInView={{ scaleX: 1, opacity: 1 }}
            viewport={{ once: true, margin: "-80px" }}
            transition={{ duration: 0.8, ease: "easeOut" }}
            style={{
              transformOrigin: "50% 50%",
              background: `linear-gradient(90deg, transparent, ${ACCENT_DEEP}, ${ACCENT}, transparent)`,
            }}
            className="absolute inset-x-12 top-0 h-px"
            aria-hidden
          />
          <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
            Start with the class. Ship the contract.
          </h2>
          <p className="text-cc-prose max-w-xl text-[1.1rem] leading-relaxed">
            Build your first implementation-first GraphQL .NET API and watch
            the schema, batching, and a typed client appear from the code you
            already wrote.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
        </section>
      </div>
    </MotionConfig>
  );
}
