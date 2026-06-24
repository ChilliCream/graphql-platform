"use client";

import {
  motion,
  useInView,
  useReducedMotion,
  type Variants,
} from "motion/react";
import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
} from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

// Brand spectrum, used at most ONCE per page (closing CTA hairline).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// ---------------------------------------------------------------------------
// Token color helpers, scoped to inline code panels.
// ---------------------------------------------------------------------------

const T: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  num: { color: "#79c0ff" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
  gqlKw: { color: "#ff7b72" },
  gqlType: { color: "#ffa657" },
  gqlField: { color: "#7ee787" },
  gqlVar: { color: "#79c0ff" },
};

// ---------------------------------------------------------------------------
// Small primitives
// ---------------------------------------------------------------------------

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

interface IndexTagProps {
  readonly value: string;
}

function IndexTag({ value }: IndexTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex h-6 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
      {value}
    </span>
  );
}

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

interface InlineCodePanelProps {
  readonly file: string;
  readonly tag: string;
  readonly lines: ReactNode;
  readonly footer?: ReactNode;
}

function InlineCodePanel({ file, tag, lines, footer }: InlineCodePanelProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[11px]">{file}</span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <div className="py-3">{lines}</div>
      {footer ? (
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-3 border-t px-4 py-2 font-mono text-[10.5px]">
          {footer}
        </div>
      ) : null}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Hero static call-site card (no motion, keeps the hero calm).
// ---------------------------------------------------------------------------

function HeroCallSiteCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 180px at 14% 18%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 140px at 8% 12%, rgba(22, 185, 228, 0.18), transparent 70%)",
        }}
      />
      <div className="bg-cc-code-header border-cc-card-border relative flex items-center gap-2 border-b px-4 py-3">
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
          Catalog/ProductService.cs
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          C#
        </span>
      </div>
      <div className="relative py-3">
        <CodeLine n={1}>
          <span style={T.kw}>using</span>{" "}
          <span style={T.plain}>Catalog.Client;</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={T.comment}>
            {`// Generated by MSBuild from your .graphql files.`}
          </span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={T.kw}>var</span> <span style={T.param}>client</span>{" "}
          <span style={T.punct}>=</span> <span style={T.param}>services</span>
          <span style={T.punct}>.</span>
          <span style={T.fn}>GetRequiredService</span>
          <span style={T.punct}>{`<`}</span>
          <span style={T.type}>ICatalogClient</span>
          <span style={T.punct}>{`>`}</span>
          <span style={T.punct}>();</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={T.kw}>var</span> <span style={T.param}>result</span>{" "}
          <span style={T.punct}>=</span> <span style={T.kw}>await</span>{" "}
          <span style={T.param}>client</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>GetProduct</span>
          <span style={T.punct}>.</span>
          <span style={T.fn}>ExecuteAsync</span>
          <span style={T.punct}>(</span>
          <span style={T.param}>id</span>
          <span style={T.punct}>);</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={T.type}>Product</span>{" "}
          <span style={T.param}>product</span> <span style={T.punct}>=</span>{" "}
          <span style={T.param}>result</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>Data</span>
          <span style={T.punct}>!.</span>
          <span style={T.plain}>ProductById</span>
          <span style={T.punct}>;</span>
        </CodeLine>
      </div>
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        <span>typed records, DI registration, normalized store</span>
        <span className="text-cc-accent">emitted at build time</span>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// CENTERPIECE: the animated MSBuild Forge.
// ---------------------------------------------------------------------------

const FORGE_PHASES = {
  idle: 0,
  ingest: 1,
  forge: 2,
  emit: 3,
  store: 4,
  subscribe: 5,
  watchers: 6,
  done: 7,
} as const;

type ForgePhase = (typeof FORGE_PHASES)[keyof typeof FORGE_PHASES];

interface ForgePanelProps {
  readonly replayKey: number;
  readonly reducedMotion: boolean;
}

function ForgePanel({ replayKey, reducedMotion }: ForgePanelProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { amount: 0.35 });
  const [phase, setPhase] = useState<ForgePhase>(() =>
    reducedMotion ? FORGE_PHASES.done : FORGE_PHASES.idle,
  );

  useEffect(() => {
    if (reducedMotion) {
      return;
    }

    if (!inView) {
      return;
    }

    let cancelled = false;
    const timers: ReturnType<typeof setTimeout>[] = [];
    const schedule = (next: ForgePhase, delay: number) => {
      timers.push(
        setTimeout(() => {
          if (!cancelled) {
            setPhase(next);
          }
        }, delay),
      );
    };

    schedule(FORGE_PHASES.idle, 0);
    schedule(FORGE_PHASES.ingest, 220);
    schedule(FORGE_PHASES.forge, 1500);
    schedule(FORGE_PHASES.emit, 2400);
    schedule(FORGE_PHASES.store, 3800);
    schedule(FORGE_PHASES.subscribe, 4700);
    schedule(FORGE_PHASES.watchers, 5500);
    schedule(FORGE_PHASES.done, 6400);

    return () => {
      cancelled = true;
      timers.forEach(clearTimeout);
    };
  }, [inView, replayKey, reducedMotion]);

  const reached = (p: ForgePhase) => phase >= p;

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border"
    >
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between gap-3 border-b px-5 py-3 font-mono text-[11px] tracking-wider uppercase">
        <span className="text-cc-ink-dim">Inputs</span>
        <span className="text-cc-ink-dim">MSBuild codegen</span>
        <span className="text-cc-ink-dim">Output and Store</span>
      </div>

      <div className="grid gap-6 px-5 py-7 sm:px-7 lg:grid-cols-12 lg:gap-4 lg:py-9">
        {/* LEFT: input tokens */}
        <div className="flex flex-col gap-3 lg:col-span-4">
          {[
            { name: "schema.graphql", sub: "downloaded by CLI", delay: 0 },
            {
              name: "GetProduct.graphql",
              sub: "your operations",
              delay: 0.18,
            },
            { name: ".graphqlrc.json", sub: "name + namespace", delay: 0.36 },
          ].map((token) => (
            <ForgeInputToken
              key={token.name}
              name={token.name}
              sub={token.sub}
              delay={token.delay}
              active={reached(FORGE_PHASES.ingest)}
              reducedMotion={reducedMotion}
            />
          ))}
          <div className="border-cc-card-border text-cc-ink-dim mt-2 inline-flex w-fit items-center gap-2 rounded-full border px-3 py-1 font-mono text-[10.5px] tracking-widest uppercase">
            <span
              className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
              aria-hidden
            />
            dotnet build
          </div>
        </div>

        {/* CENTER: gear and ingot */}
        <div className="relative flex flex-col items-center justify-center gap-5 lg:col-span-4">
          <ForgeGear
            pulsing={reached(FORGE_PHASES.forge)}
            reducedMotion={reducedMotion}
          />
          <ForgeIngot
            visible={reached(FORGE_PHASES.emit)}
            reducedMotion={reducedMotion}
          />
        </div>

        {/* RIGHT: entity store + watchers */}
        <div className="flex flex-col gap-4 lg:col-span-4">
          <ForgeStore
            filled={reached(FORGE_PHASES.store)}
            pulse={reached(FORGE_PHASES.subscribe)}
            reducedMotion={reducedMotion}
          />
          <ForgeWatchers
            visible={reached(FORGE_PHASES.watchers)}
            reducedMotion={reducedMotion}
          />
          <div className="border-cc-card-border text-cc-ink-dim mt-1 inline-flex w-fit items-center gap-2 rounded-full border px-3 py-1 font-mono text-[10.5px] tracking-widest uppercase">
            <span
              className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
              aria-hidden
            />
            EntityStore
          </div>
        </div>
      </div>

      {/* Caption legend */}
      <div className="border-cc-card-border bg-cc-code-header flex flex-wrap items-center justify-between gap-3 border-t px-5 py-3 font-mono text-[11px]">
        <span className="text-cc-ink-dim">
          <span className="text-cc-accent">dotnet build</span> &rarr; typed
          client + records + DI + normalized store
        </span>
        <span className="text-cc-ink-dim">
          subscription pulse on{" "}
          <span className="text-cc-accent">Product#42</span>
        </span>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Forge sub-pieces
// ---------------------------------------------------------------------------

interface ForgeInputTokenProps {
  readonly name: string;
  readonly sub: string;
  readonly delay: number;
  readonly active: boolean;
  readonly reducedMotion: boolean;
}

function ForgeInputToken({
  name,
  sub,
  delay,
  active,
  reducedMotion,
}: ForgeInputTokenProps) {
  const initial = reducedMotion ? { x: 0, opacity: 1 } : { x: -28, opacity: 0 };
  const animate =
    active || reducedMotion ? { x: 0, opacity: 1 } : { x: -28, opacity: 0 };
  return (
    <motion.div
      initial={initial}
      animate={animate}
      transition={{
        duration: reducedMotion ? 0 : 0.5,
        delay: reducedMotion ? 0 : delay,
        ease: "easeOut",
      }}
      className="border-cc-card-border bg-cc-surface relative flex items-center justify-between gap-3 rounded-md border px-3 py-2"
    >
      <div className="flex flex-col">
        <span className="text-cc-ink font-mono text-[12px] leading-tight">
          {name}
        </span>
        <span className="text-cc-ink-dim font-mono text-[10.5px] leading-tight">
          {sub}
        </span>
      </div>
      <motion.span
        aria-hidden
        className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
        animate={
          reducedMotion || !active
            ? { opacity: active ? 1 : 0.3 }
            : { opacity: [0.3, 1, 0.5] }
        }
        transition={reducedMotion ? { duration: 0 } : { duration: 1.4, delay }}
      />
    </motion.div>
  );
}

interface ForgeGearProps {
  readonly pulsing: boolean;
  readonly reducedMotion: boolean;
}

function ForgeGear({ pulsing, reducedMotion }: ForgeGearProps) {
  return (
    <div className="relative h-28 w-28">
      <motion.div
        aria-hidden
        className="absolute inset-0 rounded-full"
        style={{
          background:
            "radial-gradient(circle at center, rgba(94,234,212,0.32), rgba(94,234,212,0) 70%)",
        }}
        animate={
          reducedMotion
            ? { opacity: pulsing ? 1 : 0 }
            : pulsing
              ? { opacity: [0.2, 0.8, 0.35], scale: [0.95, 1.1, 1] }
              : { opacity: 0.1, scale: 0.95 }
        }
        transition={
          reducedMotion
            ? { duration: 0 }
            : { duration: 1.4, repeat: pulsing ? Infinity : 0 }
        }
      />
      <motion.svg
        viewBox="0 0 100 100"
        className="absolute inset-0 h-full w-full"
        animate={
          reducedMotion
            ? { rotate: 0 }
            : pulsing
              ? { rotate: 360 }
              : { rotate: 0 }
        }
        transition={
          reducedMotion
            ? { duration: 0 }
            : { duration: 8, ease: "linear", repeat: pulsing ? Infinity : 0 }
        }
      >
        <defs>
          <linearGradient id="ss-forge-gear" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stopColor="#5eead4" stopOpacity="0.9" />
            <stop offset="100%" stopColor="#16b9e4" stopOpacity="0.6" />
          </linearGradient>
        </defs>
        <g transform="translate(50 50)">
          {Array.from({ length: 10 }).map((_, i) => (
            <rect
              key={i}
              x="-4"
              y="-44"
              width="8"
              height="12"
              rx="1.5"
              fill="url(#ss-forge-gear)"
              transform={`rotate(${(360 / 10) * i})`}
            />
          ))}
          <circle
            r="28"
            fill="rgba(12,19,34,0.85)"
            stroke="rgba(94,234,212,0.55)"
            strokeWidth="1.2"
          />
          <circle
            r="8"
            fill="rgba(94,234,212,0.18)"
            stroke="rgba(94,234,212,0.7)"
            strokeWidth="1"
          />
        </g>
      </motion.svg>
      <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
        <span className="text-cc-accent font-mono text-[10px] tracking-widest uppercase">
          MSBuild
        </span>
      </div>
    </div>
  );
}

interface ForgeIngotProps {
  readonly visible: boolean;
  readonly reducedMotion: boolean;
}

const INGOT_LINES: ReadonlyArray<ReactNode> = [
  <>
    <span style={T.kw}>public sealed record</span>{" "}
    <span style={T.type}>Product</span>
    <span style={T.punct}>(</span>
    <span style={T.kw}>string</span> <span style={T.param}>Id</span>
    <span style={T.punct}>,</span> <span style={T.kw}>string</span>{" "}
    <span style={T.param}>Name</span>
    <span style={T.punct}>,</span> <span style={T.kw}>int</span>{" "}
    <span style={T.param}>PriceCents</span>
    <span style={T.punct}>);</span>
  </>,
  <>
    <span style={T.kw}>public partial interface</span>{" "}
    <span style={T.type}>ICatalogClient</span>
  </>,
  <>
    <span style={T.plain}>{`  `}</span>
    <span style={T.type}>IGetProductOperation</span>{" "}
    <span style={T.param}>GetProduct</span>
    <span style={T.punct}> {`{ get; }`}</span>
  </>,
];

function ForgeIngot({ visible, reducedMotion }: ForgeIngotProps) {
  return (
    <motion.div
      initial={reducedMotion ? { opacity: 1, y: 0 } : { opacity: 0, y: 12 }}
      animate={
        visible || reducedMotion ? { opacity: 1, y: 0 } : { opacity: 0, y: 12 }
      }
      transition={{ duration: reducedMotion ? 0 : 0.55, ease: "easeOut" }}
      className="bg-cc-code-bg border-cc-card-border w-full max-w-[260px] overflow-hidden rounded-md border"
    >
      <div className="bg-cc-code-header border-cc-card-border flex items-center justify-between gap-2 border-b px-3 py-1.5 font-mono text-[10px] tracking-widest uppercase">
        <span className="text-cc-ink-dim">CatalogClient.cs</span>
        <span className="text-cc-accent">emit</span>
      </div>
      <div className="px-3 py-2">
        {INGOT_LINES.map((line, idx) => (
          <IngotLine
            key={idx}
            visible={visible || reducedMotion}
            delay={idx * 0.25}
            reducedMotion={reducedMotion}
          >
            {line}
          </IngotLine>
        ))}
      </div>
    </motion.div>
  );
}

interface IngotLineProps {
  readonly visible: boolean;
  readonly delay: number;
  readonly reducedMotion: boolean;
  readonly children: ReactNode;
}

function IngotLine({
  visible,
  delay,
  reducedMotion,
  children,
}: IngotLineProps) {
  return (
    <motion.div
      initial={reducedMotion ? { opacity: 1 } : { opacity: 0, x: -6 }}
      animate={
        visible || reducedMotion ? { opacity: 1, x: 0 } : { opacity: 0, x: -6 }
      }
      transition={{
        duration: reducedMotion ? 0 : 0.32,
        delay: reducedMotion ? 0 : delay,
      }}
      className="font-mono text-[11px] leading-5 whitespace-pre"
    >
      {children}
    </motion.div>
  );
}

interface ForgeStoreProps {
  readonly filled: boolean;
  readonly pulse: boolean;
  readonly reducedMotion: boolean;
}

function ForgeStore({ filled, pulse, reducedMotion }: ForgeStoreProps) {
  const cells: ReadonlyArray<{
    readonly key: string;
    readonly target: boolean;
  }> = [
    { key: "a1", target: false },
    { key: "a2", target: false },
    { key: "a3", target: false },
    { key: "a4", target: false },
    { key: "b1", target: false },
    { key: "b2", target: true },
    { key: "b3", target: false },
    { key: "b4", target: false },
    { key: "c1", target: false },
    { key: "c2", target: false },
    { key: "c3", target: false },
    { key: "c4", target: false },
  ];
  return (
    <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-md border p-3">
      <div className="mb-2 flex items-center justify-between">
        <span className="text-cc-ink font-mono text-[11px]">
          Normalized store
        </span>
        <span className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
          Product#42
        </span>
      </div>
      <div className="grid grid-cols-4 gap-1.5">
        {cells.map((cell) => {
          const isTarget = cell.target;
          const fill =
            isTarget && (filled || reducedMotion)
              ? "rgba(94,234,212,0.55)"
              : "rgba(245,241,234,0.05)";
          const border =
            isTarget && (filled || reducedMotion)
              ? "rgba(94,234,212,0.85)"
              : "rgba(245,241,234,0.14)";
          return (
            <motion.div
              key={cell.key}
              className="h-7 rounded-[3px]"
              style={{
                backgroundColor: fill,
                border: `1px solid ${border}`,
              }}
              animate={
                isTarget && pulse && !reducedMotion
                  ? {
                      boxShadow: [
                        "0 0 0 0 rgba(94,234,212,0.0)",
                        "0 0 0 4px rgba(94,234,212,0.45)",
                        "0 0 0 0 rgba(94,234,212,0.0)",
                      ],
                    }
                  : { boxShadow: "0 0 0 0 rgba(94,234,212,0.0)" }
              }
              transition={{
                duration: reducedMotion ? 0 : 1.1,
                repeat: pulse && !reducedMotion ? 1 : 0,
              }}
            />
          );
        })}
      </div>
      <div className="mt-3 flex items-center justify-between font-mono text-[10px]">
        <span className="text-cc-ink-dim">ws://api/graphql</span>
        <span className="text-cc-accent">OnPriceChanged</span>
      </div>
    </div>
  );
}

interface ForgeWatchersProps {
  readonly visible: boolean;
  readonly reducedMotion: boolean;
}

function ForgeWatchers({ visible, reducedMotion }: ForgeWatchersProps) {
  const watchers = ["ProductCard.razor", "PriceTicker.razor"];
  return (
    <div className="flex flex-col gap-2">
      {watchers.map((w, i) => (
        <motion.div
          key={w}
          initial={reducedMotion ? { opacity: 1, x: 0 } : { opacity: 0, x: 10 }}
          animate={
            visible || reducedMotion
              ? { opacity: 1, x: 0 }
              : { opacity: 0, x: 10 }
          }
          transition={{
            duration: reducedMotion ? 0 : 0.4,
            delay: reducedMotion ? 0 : i * 0.18,
          }}
          className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-2 rounded-md border px-3 py-1.5 font-mono text-[11px]"
        >
          <span className="text-cc-ink">{w}</span>
          <span className="text-cc-accent text-[10px] tracking-widest uppercase">
            re-render
          </span>
        </motion.div>
      ))}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Capability strip with whileInView stagger.
// ---------------------------------------------------------------------------

const CAPABILITIES: readonly string[] = [
  "MSBuild code generation",
  "Normalized entity store",
  "Three fetch strategies",
  "WebSocket subscriptions",
  "Persisted operations",
  "Blazor and Razor ready",
];

function CapabilityStrip() {
  const containerVariants: Variants = {
    hidden: {},
    visible: { transition: { staggerChildren: 0.06 } },
  };
  const itemVariants: Variants = {
    hidden: { opacity: 0, y: 6 },
    visible: { opacity: 1, y: 0, transition: { duration: 0.35 } },
  };
  return (
    <motion.ul
      variants={containerVariants}
      initial="hidden"
      whileInView="visible"
      viewport={{ once: true, amount: 0.3 }}
      className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-6"
    >
      {CAPABILITIES.map((label) => (
        <motion.li
          key={label}
          variants={itemVariants}
          className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
        >
          <span className="text-cc-accent" aria-hidden>
            <CheckIcon size={12} />
          </span>
          {label}
        </motion.li>
      ))}
    </motion.ul>
  );
}

// ---------------------------------------------------------------------------
// FEATURE ROW 01: .graphqlrc.json with type-on lines (single pass, whileInView).
// ---------------------------------------------------------------------------

function GraphqlrcSnippetAnimated() {
  const lines: ReadonlyArray<ReactNode> = [
    <>
      <span style={T.punct}>{`{`}</span>
    </>,
    <>
      <span style={T.plain}>{`  `}</span>
      <span style={T.str}>&quot;schema&quot;</span>
      <span style={T.punct}>: </span>
      <span style={T.str}>&quot;schema.graphql&quot;</span>
      <span style={T.punct}>,</span>
    </>,
    <>
      <span style={T.plain}>{`  `}</span>
      <span style={T.str}>&quot;documents&quot;</span>
      <span style={T.punct}>: </span>
      <span style={T.str}>&quot;**/*.graphql&quot;</span>
      <span style={T.punct}>,</span>
    </>,
    <>
      <span style={T.plain}>{`  `}</span>
      <span style={T.str}>&quot;extensions&quot;</span>
      <span style={T.punct}>: {`{`}</span>
    </>,
    <>
      <span style={T.plain}>{`    `}</span>
      <span style={T.str}>&quot;strawberryShake&quot;</span>
      <span style={T.punct}>: {`{`}</span>
    </>,
    <>
      <span style={T.plain}>{`      `}</span>
      <span style={T.str}>&quot;name&quot;</span>
      <span style={T.punct}>: </span>
      <span style={T.str}>&quot;CatalogClient&quot;</span>
      <span style={T.punct}>,</span>
    </>,
    <>
      <span style={T.plain}>{`      `}</span>
      <span style={T.str}>&quot;namespace&quot;</span>
      <span style={T.punct}>: </span>
      <span style={T.str}>&quot;Catalog.Client&quot;</span>
      <span style={T.punct}>,</span>
    </>,
    <>
      <span style={T.plain}>{`      `}</span>
      <span style={T.str}>&quot;url&quot;</span>
      <span style={T.punct}>: </span>
      <span style={T.str}>&quot;https://api.example.com/graphql&quot;</span>
    </>,
    <>
      <span style={T.plain}>{`    `}</span>
      <span style={T.punct}>{`}`}</span>
    </>,
    <>
      <span style={T.plain}>{`  `}</span>
      <span style={T.punct}>{`}`}</span>
    </>,
    <>
      <span style={T.punct}>{`}`}</span>
    </>,
  ];

  const container: Variants = {
    hidden: {},
    visible: { transition: { staggerChildren: 0.05 } },
  };
  const child: Variants = {
    hidden: { opacity: 0, x: -6 },
    visible: { opacity: 1, x: 0, transition: { duration: 0.28 } },
  };

  return (
    <InlineCodePanel
      file=".graphqlrc.json"
      tag="JSON"
      lines={
        <motion.div
          variants={container}
          initial="hidden"
          whileInView="visible"
          viewport={{ once: true, amount: 0.4 }}
        >
          {lines.map((line, idx) => (
            <motion.div key={idx} variants={child}>
              <CodeLine n={idx + 1}>{line}</CodeLine>
            </motion.div>
          ))}
        </motion.div>
      }
      footer={
        <>
          <span>dotnet graphql init &amp; dotnet graphql update</span>
          <span className="text-cc-accent">CLI</span>
        </>
      }
    />
  );
}

// ---------------------------------------------------------------------------
// FEATURE ROW 02: reactive store mini-diagram with pathLength draw.
// ---------------------------------------------------------------------------

function ReactiveStoreDiagramAnimated() {
  const ref = useRef<SVGSVGElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });
  const reduced = useReducedMotion() ?? false;
  const target = inView || reduced ? 1 : 0;

  return (
    <svg
      ref={ref}
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Two queries denormalized into one entity row, two watchers subscribe to changes"
    >
      <defs>
        <linearGradient id="ss-v7-store-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
        </linearGradient>
      </defs>

      {[
        { y: 24, label: "GetProduct(id: 42)" },
        { y: 84, label: "ListProducts(first: 10)" },
      ].map((q, i) => (
        <g key={q.label}>
          <rect
            x="12"
            y={q.y}
            width="170"
            height="34"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={q.y + 21}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#a1a3af"
          >
            {q.label}
          </text>
          <motion.path
            d={`M 182 ${q.y + 17} C 230 ${q.y + 17}, 230 110, 270 110`}
            stroke="url(#ss-v7-store-line)"
            strokeWidth="1.5"
            fill="none"
            initial={{ pathLength: reduced ? 1 : 0 }}
            animate={{ pathLength: target }}
            transition={{
              duration: reduced ? 0 : 0.9,
              delay: reduced ? 0 : 0.15 + i * 0.18,
              ease: "easeInOut",
            }}
          />
        </g>
      ))}

      <motion.rect
        x="270"
        y="86"
        width="130"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
        initial={{ opacity: reduced ? 1 : 0 }}
        animate={{ opacity: target }}
        transition={{ duration: reduced ? 0 : 0.4, delay: reduced ? 0 : 0.9 }}
      />
      <text
        x="335"
        y="106"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        EntityStore
      </text>
      <text
        x="335"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        Product#42 (one row)
      </text>

      {[156, 188].map((y, i) => (
        <g key={y}>
          <motion.path
            d={`M 400 110 C 432 110, 432 ${y}, 462 ${y}`}
            stroke="rgba(94,234,212,0.55)"
            strokeWidth="1.2"
            fill="none"
            initial={{ pathLength: reduced ? 1 : 0 }}
            animate={{ pathLength: target }}
            transition={{
              duration: reduced ? 0 : 0.6,
              delay: reduced ? 0 : 1.2 + i * 0.2,
              ease: "easeInOut",
            }}
          />
          <motion.text
            x="462"
            y={y + 3}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.7)"
            initial={{ opacity: reduced ? 1 : 0 }}
            animate={{ opacity: target }}
            transition={{
              duration: reduced ? 0 : 0.35,
              delay: reduced ? 0 : 1.5 + i * 0.2,
            }}
          >
            {i === 0 ? "ProductCard.razor" : "PriceTicker.razor"}
          </motion.text>
        </g>
      ))}
      <text
        x="12"
        y="180"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        normalized, deduplicated, reactive
      </text>
    </svg>
  );
}

// ---------------------------------------------------------------------------
// FEATURE ROW 03: fetch strategies, dots travel chips to a target.
// ---------------------------------------------------------------------------

function FetchStrategiesDiagramAnimated() {
  const ref = useRef<SVGSVGElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });
  const reduced = useReducedMotion() ?? false;
  const play = inView && !reduced;

  const strategies: ReadonlyArray<{
    readonly y: number;
    readonly name: string;
    readonly desc: string;
    readonly dots: ReadonlyArray<{
      readonly delay: number;
      readonly dur: number;
    }>;
  }> = [
    {
      y: 16,
      name: "CacheFirst",
      desc: "store hit returns first, no request",
      dots: [{ delay: 0.1, dur: 0.8 }],
    },
    {
      y: 84,
      name: "NetworkOnly",
      desc: "always fetch, then update the store",
      dots: [{ delay: 0.2, dur: 1.6 }],
    },
    {
      y: 152,
      name: "CacheAndNetwork",
      desc: "yield cache, refresh in background",
      dots: [
        { delay: 0.15, dur: 0.7 },
        { delay: 0.8, dur: 1.4 },
      ],
    },
  ];

  return (
    <svg
      ref={ref}
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Three fetch strategies: cache-first, network-only, cache-and-network"
    >
      {strategies.map((s) => (
        <g key={s.name}>
          <rect
            x="12"
            y={s.y}
            width="180"
            height="48"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.5)"
          />
          <text
            x="24"
            y={s.y + 20}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {s.name}
          </text>
          <text
            x="24"
            y={s.y + 38}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {s.desc}
          </text>
          <path
            d={`M 192 ${s.y + 24} L 290 ${s.y + 24}`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
          <polygon
            points={`286,${s.y + 20} 298,${s.y + 24} 286,${s.y + 28}`}
            fill="rgba(94,234,212,0.55)"
          />
          {s.dots.map((d, idx) => (
            <motion.circle
              key={`${s.name}-${idx}`}
              cx="192"
              cy={s.y + 24}
              r="3.5"
              fill="#5eead4"
              initial={{ cx: 192, opacity: 0 }}
              animate={
                play
                  ? { cx: [192, 290], opacity: [0, 1, 1, 0] }
                  : { cx: 290, opacity: reduced ? 1 : 0 }
              }
              transition={{
                duration: d.dur,
                delay: d.delay,
                repeat: play ? Infinity : 0,
                repeatDelay: 2.2,
                ease: "easeInOut",
                times: [0, 0.15, 0.85, 1],
              }}
            />
          ))}
        </g>
      ))}
      <rect
        x="298"
        y="50"
        width="160"
        height="120"
        rx="10"
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="378"
        y="76"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        client.GetProduct
      </text>
      <text
        x="378"
        y="96"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        .Watch(strategy)
      </text>
      <text
        x="378"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        per call,
      </text>
      <text
        x="378"
        y="134"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        or set globally
      </text>
      <text
        x="378"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#5eead4"
      >
        IObservable&lt;Result&gt;
      </text>
    </svg>
  );
}

// ---------------------------------------------------------------------------
// FEATURE ROW 04: Razor + sparkline (drawn on view-enter).
// ---------------------------------------------------------------------------

function SubscriptionRazorPanel() {
  return (
    <InlineCodePanel
      file="PriceTicker.razor"
      tag="Razor"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>UseSubscription</span>{" "}
            <span style={T.param}>TResult</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;OnPriceChangedResult&quot;</span>{" "}
            <span style={T.param}>Subscribe</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>
              &quot;@(c =&gt; c.OnPriceChanged.Watch(sku))&quot;
            </span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.param}> Context</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;d&quot;</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;@d.PriceChanged.PriceCents&lt;/</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>UseSubscription</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>WebSocket transport, store-backed re-render</span>
          <span className="text-cc-accent">@OnPriceChanged</span>
        </>
      }
    />
  );
}

function SubscriptionSparkline() {
  const ref = useRef<SVGSVGElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });
  const reduced = useReducedMotion() ?? false;
  const target = inView || reduced ? 1 : 0;

  // A sparkline that drifts upward and ends at the badge.
  const path =
    "M 12 110 L 50 96 L 80 104 L 112 74 L 148 88 L 180 60 L 216 72 L 252 48 L 290 64 L 326 40 L 360 52";

  return (
    <svg
      ref={ref}
      viewBox="0 0 420 160"
      className="h-auto w-full"
      role="img"
      aria-label="Subscription price ticks drawn into a sparkline that ends at the OnPriceChanged badge"
    >
      <defs>
        <linearGradient id="ss-v7-spark" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.25" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.95" />
        </linearGradient>
      </defs>
      <rect
        x="6"
        y="6"
        width="408"
        height="148"
        rx="10"
        fill="rgba(12,19,34,0.45)"
        stroke="rgba(245,241,234,0.1)"
      />
      <text
        x="20"
        y="30"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="rgba(245,241,234,0.6)"
      >
        ws://api/graphql
      </text>
      <motion.path
        d={path}
        stroke="url(#ss-v7-spark)"
        strokeWidth="1.8"
        fill="none"
        initial={{ pathLength: reduced ? 1 : 0 }}
        animate={{ pathLength: target }}
        transition={{ duration: reduced ? 0 : 1.4, ease: "easeInOut" }}
      />
      <motion.circle
        cx="360"
        cy="52"
        r="4"
        fill="#5eead4"
        initial={{ opacity: reduced ? 1 : 0, scale: reduced ? 1 : 0.5 }}
        animate={{ opacity: target, scale: target ? 1 : 0.5 }}
        transition={{ duration: reduced ? 0 : 0.3, delay: reduced ? 0 : 1.3 }}
      />
      <motion.g
        initial={{ opacity: reduced ? 1 : 0 }}
        animate={{ opacity: target }}
        transition={{ duration: reduced ? 0 : 0.4, delay: reduced ? 0 : 1.4 }}
      >
        <rect
          x="280"
          y="100"
          width="124"
          height="32"
          rx="6"
          fill="rgba(94,234,212,0.1)"
          stroke="rgba(94,234,212,0.55)"
        />
        <text
          x="342"
          y="120"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill="#5eead4"
        >
          @OnPriceChanged
        </text>
      </motion.g>
    </svg>
  );
}

// ---------------------------------------------------------------------------
// Feature row wrapper.
// ---------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: ReactNode;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function FeatureRow({
  id,
  index,
  eyebrow,
  title,
  body,
  bullets,
  visual,
  reverse = false,
}: FeatureRowProps) {
  const bulletContainer: Variants = {
    hidden: {},
    visible: { transition: { staggerChildren: 0.06 } },
  };
  const bulletItem: Variants = {
    hidden: { opacity: 0, y: 4 },
    visible: { opacity: 1, y: 0, transition: { duration: 0.3 } },
  };

  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <div
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex items-center gap-3">
            <IndexTag value={index} />
            <Eyebrow>{eyebrow}</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            {title}
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            {body}
          </p>
          <motion.ul
            variants={bulletContainer}
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true, amount: 0.3 }}
            className="mt-6 flex flex-col gap-2.5"
          >
            {bullets.map((b) => (
              <motion.li
                key={b}
                variants={bulletItem}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{b}</span>
              </motion.li>
            ))}
          </motion.ul>
        </div>
        <div
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
            {visual}
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Proof item.
// ---------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: string;
}

function ProofItem({ label, value }: ProofItemProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 6 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.5 }}
      transition={{ duration: 0.35 }}
      className="flex flex-col gap-1"
    >
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        {value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </motion.div>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export function StrawberryShakeV7Client() {
  const reduced = useReducedMotion() ?? false;
  const [replayKey, setReplayKey] = useState(0);

  return (
    <>
      {/* HERO */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>GraphQL client for .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Your .graphql files are the contract.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Strawberry Shake is the open-source, strongly-typed GraphQL client
              for .NET. Drop your operations into .graphql files, run
              <code className="text-cc-accent mx-1 font-mono text-base">
                dotnet build
              </code>
              , and MSBuild codegen emits a typed client, immutable records, a
              normalized reactive store, and the DI registration. Call sites
              read like ordinary async C# against IDE-completed methods.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/strawberryshake">
                Get Started
              </SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
            <dl className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 border-t pt-6">
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  License
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Runtimes
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">.NET, Blazor, MAUI</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Codegen
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">
                  MSBuild, build time
                </dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-7">
            <HeroCallSiteCard />
          </div>
        </div>
      </section>

      {/* CENTERPIECE: The Forge */}
      <section
        id="forge"
        aria-label="MSBuild Forge"
        className="border-cc-card-border border-t py-16 sm:py-20"
      >
        <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <Eyebrow>Build-time forge</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              From .graphql files to a typed client, in one build.
            </h2>
            <p className="text-cc-prose mt-3 max-w-2xl text-base leading-relaxed">
              Watch your sources land in the MSBuild codegen task, emerge as
              immutable records, settle into the normalized store, and propagate
              a subscription pulse to the components that watch them.
            </p>
          </div>
          <button
            type="button"
            onClick={() => setReplayKey((k) => k + 1)}
            className="border-cc-card-border text-cc-ink-dim hover:text-cc-accent hover:border-cc-card-border-hover inline-flex items-center gap-2 rounded-full border px-3 py-1.5 font-mono text-[11px] tracking-widest uppercase transition-colors"
            aria-label="Replay the forge animation"
          >
            <span
              className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
              aria-hidden
            />
            Replay
          </button>
        </div>
        <ForgePanel replayKey={replayKey} reducedMotion={reduced} />
      </section>

      {/* Capability strip */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <CapabilityStrip />
      </section>

      {/* FEATURE 01 */}
      <FeatureRow
        id="strongly-typed"
        index="01"
        eyebrow="Strongly-typed Client"
        title="Operations in .graphql, typed C# at the call site."
        body="Queries, mutations, and subscriptions live in plain .graphql files next to the code that uses them. The schema lives in a schema.graphql file pulled from any spec-compliant server. The CLI reads the .graphqlrc.json config, MSBuild emits the typed client class, the result records, the fragments, and the DI registration, and the call sites are ordinary async C# with IntelliSense and refactor support."
        bullets={[
          "Operations are valid GraphQL documents you can hand to any tool.",
          "Generated records are nullable-aware, immutable, and deconstructible.",
          "Compatible with any GraphQL spec server, not only Hot Chocolate.",
        ]}
        visual={<GraphqlrcSnippetAnimated />}
      />

      {/* FEATURE 02 */}
      <FeatureRow
        id="reactive-store"
        index="02"
        eyebrow="Reactive Store"
        title="A normalized entity store, with Relay and Apollo vocabulary."
        body="Strawberry Shake denormalizes every GraphQL result into an entity store keyed by type and id, the same model Relay and Apollo made the standard for client caches. A query that returns the same product as a list and as a detail shares one row. Watch a query and your component re-renders when that row changes, no matter which operation produced the update."
        bullets={[
          "IObservable<Result> on every Watch(), Razor and Blazor wrappers wire it for you.",
          "Mutations write back into the store, related queries refresh automatically.",
          "Persist the store to SQLite or LiteDB and rehydrate it on next launch.",
        ]}
        visual={<ReactiveStoreDiagramAnimated />}
        reverse
      />

      {/* FEATURE 03 */}
      <FeatureRow
        id="fetch-strategies"
        index="03"
        eyebrow="Fetch Strategies"
        title="CacheFirst, NetworkOnly, CacheAndNetwork. Set globally, override per call."
        body="Every operation supports three execution strategies. CacheFirst returns the store entry without a request when it has one. NetworkOnly always fetches and writes the result through the store. CacheAndNetwork yields the cached entry immediately and refreshes in the background, which is the strategy that powers fast launches and snappy detail pages."
        bullets={[
          "Strategy is a per-Watch override on top of a per-client default.",
          "Combine with persisted state for instant cache hits on cold start.",
          "Result stream emits both cache and network values, in order.",
        ]}
        visual={<FetchStrategiesDiagramAnimated />}
      />

      {/* FEATURE 04: Subscriptions + Blazor combined */}
      <FeatureRow
        id="subscriptions"
        index="04"
        eyebrow="Subscriptions and Blazor"
        title="Realtime over WebSocket, into the same store and into Razor."
        body="Subscription operations look like queries: declare them in a .graphql file, get a typed Watch on the generated client. The WebSocket transport carries a connection_init payload for auth and reconnect handling. Pushed values write through the same entity store, so any open query, fragment, or Razor component sees the update."
        bullets={[
          "Token refresh and reconnect are part of the transport, not your code.",
          "UseSubscription Razor component lifts updates straight into Blazor markup.",
          "Same Watch surface as queries, no separate event-handler pipeline.",
        ]}
        visual={
          <div className="flex flex-col gap-4">
            <SubscriptionRazorPanel />
            <SubscriptionSparkline />
          </div>
        }
        reverse
      />

      {/* Nitro embed */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <IndexTag value="05" />
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Draft the operation, then check it in.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Nitro is the GraphQL IDE that ships with the Hot Chocolate server,
              and it is the same surface most teams use to draft operations
              before saving them as .graphql files in the client project. Browse
              the schema, run a query against the live server, and copy the
              document into the codegen pipeline.
            </p>
          </div>
          <div className="lg:col-span-5 lg:text-right">
            <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              live at /graphql
            </p>
          </div>
        </div>
        <div className="border-cc-card-border bg-cc-surface mx-auto max-w-5xl overflow-hidden rounded-xl border">
          <NitroCompose />
        </div>
      </section>

      {/* Open source proof grid */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source, in production, and free to use.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Strawberry Shake is part of the ChilliCream GraphQL platform and
              is released under the MIT license. Use it in commercial work, fork
              it, vendor it, audit it. The codebase, issues, and release notes
              all live on GitHub next to Hot Chocolate.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/strawberryshake">
                Read the docs
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Codegen" value="MSBuild" />
              <ProofItem label="Store" value="Normalized" />
              <ProofItem label="Transports" value="HTTP / WebSocket" />
              <ProofItem label="UI" value="Blazor / Razor" />
              <ProofItem label="Server" value="Hot Chocolate" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA, hairline draws in */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <motion.div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px origin-left"
          style={{ background: SPECTRUM }}
          initial={{ scaleX: reduced ? 1 : 0 }}
          whileInView={{ scaleX: 1 }}
          viewport={{ once: true, amount: 0.6 }}
          transition={{ duration: reduced ? 0 : 1.1, ease: "easeOut" }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            A typed GraphQL client your .NET team can actually own.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            A few .graphql files, a .graphqlrc.json, and a NuGet reference. The
            client, the records, the store, and the DI wiring are emitted for
            you at build time, and the runtime is the .NET you already ship.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
