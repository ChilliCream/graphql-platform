"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
  type MotionValue,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * V7 client. Motion-showcase stance for the Agentic coding page. The
 * centerpiece is a sticky scroll-pinned Lifecycle Conveyor: a single tool
 * token glides along an SVG path through four stations while a paired agent
 * terminal panel animates the matching transcript line, the approval gate,
 * and a p95 sparkline in lockstep. Everything respects prefers-reduced-motion
 * via MotionConfig and a useReducedMotion fallback that lands the centerpiece
 * on its resolved final frame.
 */

const VIOLET = "#7c92c6";
const CORAL = "#f0786a";
/** One spectrum gradient is permitted per screen; used once, in the hero lead. */
const SPECTRUM = "linear-gradient(100deg,#16b9e4 0%,#7c92c6 50%,#f0786a 100%)";

/* ------------------------------------------------------------------ *
 * Shared small parts
 * ------------------------------------------------------------------ */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface SectionHeadingProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function SectionHeading({ eyebrow, title, children }: SectionHeadingProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
        {title}
      </h2>
      {children ? (
        <div className="text-cc-ink mt-5 space-y-4 text-base/relaxed text-pretty">
          {children}
        </div>
      ) : null}
    </div>
  );
}

type Hint = "idempotent" | "read-only" | "open-world" | "destructive";

interface HintBadgeProps {
  readonly hint: Hint;
  readonly className?: string;
}

function HintBadge({ hint, className }: HintBadgeProps) {
  const label =
    hint === "idempotent"
      ? "idempotentHint"
      : hint === "read-only"
        ? "readOnlyHint"
        : hint === "open-world"
          ? "openWorldHint"
          : "destructiveHint";

  if (hint === "destructive") {
    return (
      <span
        className={`rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.06em] whitespace-nowrap ${className ?? ""}`}
        style={{
          color: CORAL,
          borderColor: "rgba(240,120,106,0.45)",
          backgroundColor: "rgba(240,120,106,0.08)",
        }}
      >
        {label}
      </span>
    );
  }

  return (
    <span
      className={`border-cc-card-border text-cc-ink-dim bg-cc-surface rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.06em] whitespace-nowrap ${className ?? ""}`}
    >
      {label}
    </span>
  );
}

function WindowDots() {
  return (
    <div className="flex items-center gap-1.5">
      <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
      <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
      <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Hero: identity-first headline + small animated MCP-converge diagram
 * ------------------------------------------------------------------ */

interface HeroChip {
  readonly label: string;
  readonly hint: Hint;
}

const HERO_CHIPS: readonly HeroChip[] = [
  { label: "getProduct", hint: "read-only" },
  { label: "tagProduct", hint: "idempotent" },
  { label: "searchOrders", hint: "idempotent" },
  { label: "deleteReview", hint: "destructive" },
];

function HeroConvergeDiagram() {
  // Four chips converging into a single /graphql/mcp core. Lines draw in
  // when the diagram scrolls into view via motion.line pathLength.
  const cx = 200;
  const cy = 150;
  const coreR = 40;

  const seats = [
    { x: 28, y: 36 },
    { x: 372, y: 36 },
    { x: 28, y: 264 },
    { x: 372, y: 264 },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
      <div className="flex items-center justify-between">
        <Eyebrow>GraphQL MCP for coding agents</Eyebrow>
        <span className="text-cc-ink-faint font-mono text-[0.6rem]">
          /graphql/mcp
        </span>
      </div>

      <svg
        viewBox="0 0 400 300"
        className="mt-3 h-auto w-full"
        role="img"
        aria-label="Four published operations converge into one /graphql/mcp core."
      >
        <defs>
          <radialGradient id="acv7-hero-core" cx="50%" cy="50%" r="60%">
            <stop offset="0%" stopColor="rgba(124,146,198,0.35)" />
            <stop offset="100%" stopColor="rgba(124,146,198,0.05)" />
          </radialGradient>
        </defs>

        {HERO_CHIPS.map((chip, i) => {
          const seat = seats[i];
          const isDestructive = chip.hint === "destructive";
          const stroke = isDestructive ? CORAL : VIOLET;
          // Where the line should end (just outside the core circle).
          const dx = cx - seat.x;
          const dy = cy - seat.y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          const tx = seat.x + (dx / dist) * (dist - coreR - 6);
          const ty = seat.y + (dy / dist) * (dist - coreR - 6);

          return (
            <g key={chip.label}>
              <motion.line
                x1={seat.x}
                y1={seat.y}
                x2={tx}
                y2={ty}
                stroke={stroke}
                strokeOpacity={0.55}
                strokeWidth={1.4}
                initial={{ pathLength: 0, opacity: 0 }}
                whileInView={{ pathLength: 1, opacity: 1 }}
                viewport={{ once: true, margin: "-40px" }}
                transition={{
                  duration: 0.9,
                  ease: "easeOut",
                  delay: 0.2 + i * 0.18,
                }}
              />
              <motion.g
                initial={{ opacity: 0, y: -4 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-40px" }}
                transition={{ duration: 0.4, delay: i * 0.12 }}
              >
                <rect
                  x={seat.x - 44}
                  y={seat.y - 12}
                  width={88}
                  height={24}
                  rx={6}
                  fill="#0c1322"
                  stroke={
                    isDestructive
                      ? "rgba(240,120,106,0.45)"
                      : "rgba(245,241,234,0.14)"
                  }
                />
                <text
                  x={seat.x}
                  y={seat.y + 3.5}
                  textAnchor="middle"
                  className="font-mono"
                  fontSize="9"
                  fill={isDestructive ? CORAL : "#a1a3af"}
                >
                  {chip.label}
                </text>
              </motion.g>
            </g>
          );
        })}

        {/* Core hub */}
        <motion.circle
          cx={cx}
          cy={cy}
          r={coreR + 12}
          fill="url(#acv7-hero-core)"
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true, margin: "-40px" }}
          transition={{ duration: 0.6 }}
        />
        <circle
          cx={cx}
          cy={cy}
          r={coreR}
          fill="#0c1322"
          stroke={VIOLET}
          strokeWidth={1.5}
        />
        <text
          x={cx}
          y={cy - 2}
          textAnchor="middle"
          className="font-mono"
          fontSize="10"
          fill="#f5f0ea"
        >
          /graphql
        </text>
        <text
          x={cx}
          y={cy + 11}
          textAnchor="middle"
          className="font-mono"
          fontSize="10"
          fill={VIOLET}
        >
          /mcp
        </text>
      </svg>

      <p className="text-cc-ink-faint mt-3 font-mono text-[0.6rem] leading-relaxed">
        one hub · streamable http · grounded in the client registry
      </p>
    </div>
  );
}

function Hero() {
  return (
    <section className="grid items-center gap-10 py-12 sm:py-16 lg:grid-cols-[1.05fr_1fr] lg:gap-14">
      <div>
        <span
          className="inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[0.62rem] tracking-[0.16em] uppercase"
          style={{
            color: VIOLET,
            borderColor: "rgba(124,146,198,0.4)",
            backgroundColor: "rgba(124,146,198,0.07)",
          }}
        >
          <span
            className="h-1.5 w-1.5 rounded-full"
            style={{ backgroundColor: VIOLET }}
          />
          Agentic coding · preview
        </span>

        <h1 className="font-heading text-cc-heading mt-6 text-4xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
          Give coding agents a feedback loop, not a guessing game.
        </h1>

        <p className="lead text-cc-ink-dim mt-6 max-w-xl text-pretty">
          Stop letting agents invent fields. Ground them in the operations your
          clients already use, gate the risky calls, and turn every fast edit
          into a{" "}
          <span
            className="bg-clip-text font-medium text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            governed feedback loop
          </span>
          .
        </p>

        <p className="text-cc-ink mt-5 max-w-xl text-base/relaxed text-pretty">
          Your GraphQL server is already an MCP server. Published operations
          become tools an agent can call with real product context, each one
          authored, validated, and traced before it ever touches production.
        </p>

        <div className="mt-8 flex flex-wrap items-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs/nitro/apis/client-registry">
            Read the Docs
          </OutlineButton>
        </div>
      </div>

      <div className="lg:pl-4">
        <HeroConvergeDiagram />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Grounding band: tool catalog rail with staggered reveal + counter
 * ------------------------------------------------------------------ */

interface ToolRowProps {
  readonly name: string;
  readonly summary: string;
  readonly hint: Hint;
}

const TOOL_ROWS: readonly ToolRowProps[] = [
  {
    name: "getProduct",
    summary: "query · single product by id",
    hint: "read-only",
  },
  {
    name: "searchOrders",
    summary: "query · filtered order list",
    hint: "idempotent",
  },
  {
    name: "tagProduct",
    summary: "mutation · upsert product tags",
    hint: "idempotent",
  },
  {
    name: "deleteReview",
    summary: "mutation · remove a review",
    hint: "destructive",
  },
  {
    name: "openTicket",
    summary: "mutation · calls an external desk",
    hint: "open-world",
  },
];

function ToolRow({ name, summary, hint }: ToolRowProps) {
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: 8 },
        shown: { opacity: 1, y: 0 },
      }}
      transition={{ duration: 0.4, ease: "easeOut" }}
      className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover flex items-center gap-3 rounded-xl border px-3.5 py-3 transition-colors"
    >
      <div className="min-w-0 flex-1">
        <p className="text-cc-ink truncate font-mono text-xs">{name}</p>
        <p className="text-cc-ink-faint mt-0.5 truncate text-[0.7rem]">
          {summary}
        </p>
      </div>
      <HintBadge hint={hint} />
    </motion.div>
  );
}

interface PublishedCounterProps {
  readonly target: number;
}

function PublishedCounter({ target }: PublishedCounterProps) {
  // Tick a number from 0 -> target when the section scrolls into view.
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, margin: "-80px" });
  const reduce = useReducedMotion();
  const [value, setValue] = useState(0);
  const mv = useMotionValue(0);

  useEffect(() => {
    return mv.on("change", (v) => setValue(Math.round(v)));
  }, [mv]);

  useEffect(() => {
    if (!inView) {
      return;
    }
    if (reduce) {
      mv.set(target);
      return;
    }
    const controls = animate(mv, target, { duration: 1.4, ease: "easeOut" });
    return () => controls.stop();
  }, [inView, reduce, target, mv]);

  return (
    <span
      ref={ref}
      className="text-cc-heading font-heading text-h2 font-semibold tabular-nums"
      aria-label={`${target} published operations`}
    >
      {value}
    </span>
  );
}

function GroundingBand() {
  return (
    <section className="border-cc-card-border border-t py-16">
      <div className="grid items-start gap-10 lg:grid-cols-[1fr_1.05fr] lg:gap-14">
        <SectionHeading
          eyebrow="Grounding"
          title="Agents edit with product context, not guesses."
        >
          <p>
            A coding agent that does not know your graph invents fields and
            writes queries no client would ship. The schema and client registry
            change that: your published operations become a catalog of callable
            tools, each one a real, reviewed shape your product already depends
            on.
          </p>
          <p>
            MCP exposes those operations as tools and prompts with behavior
            annotations, so the agent can tell a safe read from a write before
            it acts, and you keep authority over what it is allowed to do.
          </p>
          <div className="border-cc-card-border bg-cc-surface mt-2 flex items-baseline gap-4 rounded-xl border px-4 py-3">
            <PublishedCounter target={38} />
            <span className="text-cc-ink-dim font-mono text-[0.7rem]">
              published operations · grounded by the client registry
            </span>
          </div>
        </SectionHeading>

        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
          <div className="flex items-center justify-between">
            <Eyebrow>Tool catalog</Eyebrow>
            <span className="text-cc-ink-faint font-mono text-[0.6rem]">
              38 published ops
            </span>
          </div>
          <motion.div
            className="mt-4 space-y-2.5"
            variants={{
              hidden: {},
              shown: { transition: { staggerChildren: 0.08 } },
            }}
            initial="hidden"
            whileInView="shown"
            viewport={{ once: true, margin: "-60px" }}
          >
            {TOOL_ROWS.map((tool) => (
              <ToolRow key={tool.name} {...tool} />
            ))}
          </motion.div>
          <p className="text-cc-ink-faint mt-4 font-mono text-[0.6rem] leading-relaxed">
            annotations: idempotentHint · readOnlyHint · openWorldHint ·
            <span style={{ color: CORAL }}> destructiveHint</span>
          </p>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * CENTERPIECE: Lifecycle Conveyor
 * ------------------------------------------------------------------ */

interface StationCardProps {
  readonly index: number;
  readonly title: string;
  readonly note: string;
  readonly progress: MotionValue<number>;
  readonly activeAt: number;
}

function StationCard({
  index,
  title,
  note,
  progress,
  activeAt,
}: StationCardProps) {
  // Each station card lights up as the token enters its zone.
  const opacity = useTransform(
    progress,
    [activeAt - 0.02, activeAt + 0.02],
    [0.45, 1],
  );
  const borderOpacity = useTransform(
    progress,
    [activeAt - 0.02, activeAt + 0.02],
    [0.18, 0.7],
  );
  const dotScale = useTransform(
    progress,
    [activeAt - 0.02, activeAt + 0.02],
    [0.6, 1.2],
  );

  return (
    <motion.div
      style={{ opacity }}
      className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border px-4 py-4 backdrop-blur-sm"
    >
      <motion.span
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 rounded-2xl"
        style={{
          borderColor: VIOLET,
          borderWidth: 1,
          borderStyle: "solid",
          opacity: borderOpacity,
        }}
      />
      <div className="flex items-center justify-between">
        <span
          className="font-mono text-[0.62rem] tracking-[0.14em] uppercase"
          style={{ color: VIOLET }}
        >
          0{index + 1}
        </span>
        <motion.span
          style={{
            scale: dotScale,
            backgroundColor: VIOLET,
          }}
          className="h-1.5 w-1.5 rounded-full"
        />
      </div>
      <p className="font-heading text-cc-heading text-h6 mt-3 font-semibold">
        {title}
      </p>
      <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.68rem] leading-relaxed">
        {note}
      </p>
    </motion.div>
  );
}

interface TranscriptLineProps {
  readonly progress: MotionValue<number>;
  readonly visibleFrom: number;
  readonly children: ReactNode;
}

function TranscriptLine({
  progress,
  visibleFrom,
  children,
}: TranscriptLineProps) {
  const opacity = useTransform(
    progress,
    [visibleFrom - 0.02, visibleFrom + 0.04],
    [0, 1],
  );
  const y = useTransform(
    progress,
    [visibleFrom - 0.02, visibleFrom + 0.04],
    [6, 0],
  );

  return (
    <motion.p style={{ opacity, y }} className="font-mono leading-relaxed">
      {children}
    </motion.p>
  );
}

function Sparkline({ progress }: { readonly progress: MotionValue<number> }) {
  // Draw a small p95 sparkline once the token reaches the Trace station.
  const pathLength = useTransform(progress, [0.78, 0.98], [0, 1]);
  const opacity = useTransform(progress, [0.76, 0.82], [0, 1]);

  return (
    <motion.svg
      style={{ opacity }}
      viewBox="0 0 200 50"
      className="h-12 w-full"
      role="img"
      aria-label="Per-tool p95 latency sparkline drawing in."
    >
      <motion.path
        d="M 4 36 L 24 30 L 44 33 L 64 22 L 84 26 L 104 18 L 124 24 L 144 14 L 164 19 L 184 11 L 196 16"
        fill="none"
        stroke={VIOLET}
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        style={{ pathLength }}
      />
      <text x={4} y={48} className="font-mono" fontSize="8" fill="#a1a3af">
        tagProduct · p95
      </text>
    </motion.svg>
  );
}

interface ConveyorStation {
  readonly title: string;
  readonly note: string;
  readonly activeAt: number;
}

const CONVEYOR_STATIONS: readonly ConveyorStation[] = [
  { title: "Author", note: ".graphql in repo", activeAt: 0.12 },
  { title: "Validate", note: "nitro mcp validate · CI", activeAt: 0.37 },
  { title: "Stage", note: "approval gate", activeAt: 0.62 },
  { title: "Trace", note: "p95 in Nitro", activeAt: 0.87 },
];

function LifecycleConveyor() {
  const sectionRef = useRef<HTMLDivElement>(null);
  const inView = useInView(sectionRef, { once: true, margin: "-15% 0px" });
  const reduce = useReducedMotion();

  // Single time-driven progress MotionValue. It animates from 0 -> 1 once,
  // the first time the section enters the viewport. Every downstream
  // useTransform consumer (token offset, station highlight, transcript
  // reveal, approval gate, sparkline) reads this value, so the lifecycle
  // narrative plays the same beats it used to, just on a timeline instead
  // of on scroll position.
  const progress = useMotionValue(0);

  useEffect(() => {
    if (!inView) {
      return;
    }
    if (reduce) {
      progress.set(1);
      return;
    }
    const controls = animate(progress, 1, {
      duration: 9,
      ease: "easeInOut",
    });
    return () => controls.stop();
  }, [inView, reduce, progress]);

  const tokenOffset = useTransform(progress, [0, 1], ["0%", "100%"]);
  const tokenOpacity = useTransform(progress, [0, 0.04], [0, 1]);

  // Approval-gate state values.
  const gatePending = useTransform(progress, [0.5, 0.6, 0.7], [1, 1, 0]);
  const gateGranted = useTransform(progress, [0.6, 0.7, 0.85], [0, 1, 1]);
  const gateOpacity = useTransform(progress, [0.46, 0.52], [0, 1]);

  // Path used by the token via offsetPath. Long horizontal arc through the
  // four station x-coordinates with a gentle wave.
  const conveyorPath =
    "M 4 80 C 80 50 140 110 220 80 C 300 50 360 110 440 80 C 520 50 580 110 660 80 C 740 50 800 110 876 80";

  return (
    <section
      ref={sectionRef}
      className="border-cc-card-border relative border-t"
      aria-label="Lifecycle conveyor: one operation moves from Author to Trace."
    >
      <div className="py-16">
        <div className="w-full">
          <SectionHeading
            eyebrow="Centerpiece"
            title="Watch one operation ride the lifecycle."
          >
            <p>
              A single tool token (
              <code className="text-cc-info">mutation tagProduct</code>) glides
              from the repo to a traced production tool. The agent terminal
              moves in lockstep: a call, an annotation check, an approval gate,
              and a p95 sparkline that draws in only after the trace lands.
              Scroll to control the pace.
            </p>
          </SectionHeading>

          <div className="mt-10 grid items-start gap-8 lg:grid-cols-[1.25fr_1fr] lg:gap-10">
            {/* Conveyor + stations */}
            <div className="border-cc-card-border bg-cc-card-bg rounded-3xl border p-5 backdrop-blur-sm sm:p-6">
              <div className="relative">
                <svg
                  viewBox="0 0 880 160"
                  className="h-auto w-full"
                  role="img"
                  aria-label="Conveyor path connecting Author, Validate, Stage, and Trace stations."
                >
                  <defs>
                    <linearGradient
                      id="acv7-conveyor"
                      x1="0"
                      y1="0"
                      x2="1"
                      y2="0"
                    >
                      <stop offset="0%" stopColor="rgba(124,146,198,0.15)" />
                      <stop offset="50%" stopColor="rgba(124,146,198,0.55)" />
                      <stop offset="100%" stopColor="rgba(124,146,198,0.15)" />
                    </linearGradient>
                  </defs>

                  {/* Conveyor track */}
                  <path
                    d={conveyorPath}
                    fill="none"
                    stroke="url(#acv7-conveyor)"
                    strokeWidth={2}
                    strokeLinecap="round"
                  />
                  {/* Dashed underlay to suggest travel */}
                  <path
                    d={conveyorPath}
                    fill="none"
                    stroke={VIOLET}
                    strokeOpacity={0.25}
                    strokeWidth={1}
                    strokeDasharray="4 8"
                    strokeLinecap="round"
                  />

                  {/* Station markers along the path */}
                  {[40, 305, 570, 836].map((x, i) => (
                    <g key={i}>
                      <circle
                        cx={x}
                        cy={80}
                        r={6}
                        fill="#0c1322"
                        stroke={VIOLET}
                        strokeOpacity={0.6}
                        strokeWidth={1.4}
                      />
                      <text
                        x={x}
                        y={120}
                        textAnchor="middle"
                        className="font-mono"
                        fontSize="9"
                        fill="#a1a3af"
                      >
                        0{i + 1}
                      </text>
                    </g>
                  ))}
                </svg>

                {/* Token riding the path */}
                {reduce ? (
                  <div
                    aria-hidden="true"
                    className="pointer-events-none absolute"
                    style={{
                      left: "95%",
                      top: "50%",
                      transform: "translate(-50%,-50%)",
                    }}
                  >
                    <span
                      className="block rounded-full border px-2.5 py-1 font-mono text-[0.6rem] whitespace-nowrap shadow-lg"
                      style={{
                        color: "#f5f0ea",
                        backgroundColor: "rgba(124,146,198,0.18)",
                        borderColor: VIOLET,
                      }}
                    >
                      tagProduct
                    </span>
                  </div>
                ) : (
                  <motion.div
                    aria-hidden="true"
                    style={{
                      position: "absolute",
                      top: 0,
                      left: 0,
                      offsetPath: `path("${conveyorPath}")`,
                      offsetDistance: tokenOffset,
                      opacity: tokenOpacity,
                    }}
                    className="pointer-events-none"
                  >
                    <span
                      className="block rounded-full border px-2.5 py-1 font-mono text-[0.6rem] whitespace-nowrap shadow-lg"
                      style={{
                        color: "#f5f0ea",
                        backgroundColor: "rgba(124,146,198,0.18)",
                        borderColor: VIOLET,
                      }}
                    >
                      tagProduct
                    </span>
                  </motion.div>
                )}
              </div>

              <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
                {CONVEYOR_STATIONS.map((station, i) => (
                  <StationCard
                    key={station.title}
                    index={i}
                    title={station.title}
                    note={station.note}
                    progress={progress}
                    activeAt={station.activeAt}
                  />
                ))}
              </div>
            </div>

            {/* Paired agent terminal */}
            <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-3xl border shadow-2xl backdrop-blur-md">
              <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2.5">
                <WindowDots />
                <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-wide">
                  agent session · /graphql/mcp
                </span>
                <span
                  className="font-mono text-[0.55rem] tracking-[0.1em] uppercase"
                  style={{ color: VIOLET }}
                >
                  governed
                </span>
              </div>

              <div className="bg-cc-code-bg space-y-3 px-4 py-4 text-[0.72rem] sm:px-5 sm:py-5">
                <TranscriptLine progress={progress} visibleFrom={0.04}>
                  <span style={{ color: VIOLET }}>agent</span>{" "}
                  <span className="text-cc-ink-dim">
                    grounded in client registry · 38 published operations
                  </span>
                </TranscriptLine>

                <TranscriptLine progress={progress} visibleFrom={0.18}>
                  <span className="text-cc-ink-faint">&rsaquo;</span>{" "}
                  <span className="text-cc-ink">
                    {'mcp.call tagProduct { id: 42, add: ["sale"] }'}
                  </span>
                </TranscriptLine>

                <TranscriptLine progress={progress} visibleFrom={0.42}>
                  <span className="text-cc-ink-dim">
                    resolves to{" "}
                    <span className="text-cc-ink">mutation TagProduct</span> ·
                    annotated{" "}
                  </span>
                  <span style={{ color: VIOLET }}>idempotentHint</span>
                </TranscriptLine>

                {/* Approval gate: pending -> granted flips around 0.62. */}
                <motion.div
                  style={{
                    opacity: gateOpacity,
                    borderColor: "rgba(124,146,198,0.45)",
                    backgroundColor: "rgba(124,146,198,0.09)",
                  }}
                  className="flex items-center justify-between gap-3 rounded-lg border px-3 py-2.5"
                >
                  <span className="flex items-center gap-2">
                    <span
                      className="inline-flex h-4 w-4 items-center justify-center rounded-full"
                      style={{ border: `1px solid ${VIOLET}`, color: VIOLET }}
                    >
                      <CheckIcon size={9} />
                    </span>
                    <span className="text-cc-ink font-mono">approval gate</span>
                  </span>
                  <span className="font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                    <motion.span
                      style={{ opacity: gatePending }}
                      className="text-cc-ink-dim"
                    >
                      pending
                    </motion.span>
                    <motion.span
                      style={{ opacity: gateGranted, color: VIOLET }}
                    >
                      granted
                    </motion.span>
                  </span>
                </motion.div>

                <TranscriptLine progress={progress} visibleFrom={0.72}>
                  <span className="text-cc-ink-dim">
                    traced · per-tool latency landing in Nitro
                  </span>
                </TranscriptLine>

                <Sparkline progress={progress} />
              </div>
            </div>
          </div>

          <p className="text-cc-ink-faint mt-6 text-center font-mono text-[0.62rem] tracking-wide">
            scroll to advance · respects prefers-reduced-motion
          </p>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Behavior annotations strip: four pulsing badges in sequence
 * ------------------------------------------------------------------ */

interface AnnotationStripBadgeProps {
  readonly hint: Hint;
  readonly summary: string;
  readonly delay: number;
}

function AnnotationStripBadge({
  hint,
  summary,
  delay,
}: AnnotationStripBadgeProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 6 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-60px" }}
      transition={{ duration: 0.4, delay }}
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm"
    >
      <motion.div
        initial={{ scale: 1 }}
        whileInView={{ scale: [1, 1.06, 1] }}
        viewport={{ once: true, margin: "-60px" }}
        transition={{ duration: 0.7, delay: delay + 0.2 }}
        className="inline-block"
      >
        <HintBadge hint={hint} />
      </motion.div>
      <p className="text-cc-ink-dim mt-3 text-sm/relaxed">{summary}</p>
    </motion.div>
  );
}

const ANNOTATION_BADGES: readonly {
  readonly hint: Hint;
  readonly summary: string;
}[] = [
  {
    hint: "idempotent",
    summary:
      "Safe to call again with the same input. Agents can retry without compounding effects.",
  },
  {
    hint: "read-only",
    summary:
      "No state changes. Free to call broadly to gather product context for a reasoning step.",
  },
  {
    hint: "open-world",
    summary:
      "Touches systems outside the graph (a help desk, an email service). Side effects live beyond your boundary.",
  },
  {
    hint: "destructive",
    summary:
      "Removes or replaces data. Always gated, always traced, always reviewed before promotion.",
  },
];

function BehaviorAnnotationsStrip() {
  return (
    <section className="border-cc-card-border border-t py-16">
      <SectionHeading
        eyebrow="Behavior vocabulary"
        title="Four annotations the agent reads before it acts."
      >
        <p>
          The annotations are part of the tool definition, not a runtime
          afterthought. The agent reads them, the gate enforces them, and the
          trace records them.
        </p>
      </SectionHeading>
      <div className="mt-10 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {ANNOTATION_BADGES.map((entry, i) => (
          <AnnotationStripBadge
            key={entry.hint}
            hint={entry.hint}
            summary={entry.summary}
            delay={i * 0.15}
          />
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Skillz bento
 * ------------------------------------------------------------------ */

interface SkillTileProps {
  readonly name: string;
  readonly body: string;
}

const SKILL_TILES: readonly SkillTileProps[] = [
  {
    name: "pagination.SKILL.md",
    body: "Always page list fields with the registry connection contract.",
  },
  {
    name: "errors.SKILL.md",
    body: "Model failures as typed union results, never thrown exceptions.",
  },
  {
    name: "naming.SKILL.md",
    body: "Mutation inputs and payloads follow the team naming rules.",
  },
  {
    name: "auth.SKILL.md",
    body: "Gate fields with the shared policy directives, not ad-hoc checks.",
  },
];

function SkillTile({ name, body }: SkillTileProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-60px" }}
      transition={{ duration: 0.4 }}
      whileHover={{ y: -4 }}
      className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-2xl border p-5 backdrop-blur-sm"
    >
      <p className="font-mono text-[0.72rem]" style={{ color: VIOLET }}>
        {name}
      </p>
      <p className="text-cc-ink-dim mt-2 text-sm/relaxed">{body}</p>
    </motion.div>
  );
}

function SkillzBento() {
  return (
    <section className="border-cc-card-border border-t py-16">
      <SectionHeading
        eyebrow="Conventions"
        title="Teach every agent your conventions once."
      >
        <p>
          skillz packages your team&rsquo;s GraphQL conventions as portable{" "}
          <code className="text-cc-info">SKILL.md</code> files, installable
          across the agents your team already uses, so the next pull request
          looks like your codebase, not a generic one.
        </p>
      </SectionHeading>
      <div className="mt-10 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {SKILL_TILES.map((tile) => (
          <SkillTile key={tile.name} name={tile.name} body={tile.body} />
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Honesty beat
 * ------------------------------------------------------------------ */

const HONESTY_POINTS: readonly string[] = [
  "Tools and prompts are authored in the repo as reviewed code, not minted at runtime.",
  "nitro mcp validate runs in CI, so a broken tool collection never reaches a stage.",
  "Behavior is declared with idempotentHint, destructiveHint, and openWorldHint.",
  "An edit is checked against published operations; risky changes read “published clients affected.”",
  "Every tool call is traced in Nitro with p95 latency, error rate, and impact.",
];

function HonestyBeat() {
  return (
    <section className="border-cc-card-border border-t py-16">
      <div className="grid items-start gap-10 lg:grid-cols-[1fr_1fr] lg:gap-14">
        <SectionHeading
          eyebrow="What we actually claim"
          title="We say what the registries can prove."
        >
          <p>
            Honesty is the differentiator. We do not promise to name every
            published client affected or to mint safe tools at runtime. We
            promise a governed, observed path: authored in repo, validated in
            CI, staged with a gate, and traced in production.
          </p>
        </SectionHeading>

        <motion.ul
          variants={{
            hidden: {},
            shown: { transition: { staggerChildren: 0.1 } },
          }}
          initial="hidden"
          whileInView="shown"
          viewport={{ once: true, margin: "-80px" }}
          className="border-cc-card-border bg-cc-card-bg space-y-4 rounded-3xl border p-7 backdrop-blur-sm"
        >
          {HONESTY_POINTS.map((point) => (
            <motion.li
              key={point}
              variants={{
                hidden: { opacity: 0, x: -6 },
                shown: { opacity: 1, x: 0 },
              }}
              transition={{ duration: 0.35, ease: "easeOut" }}
              className="flex items-start gap-3"
            >
              <span className="mt-0.5 shrink-0" style={{ color: VIOLET }}>
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm/relaxed text-pretty">
                {point}
              </span>
            </motion.li>
          ))}
        </motion.ul>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Closing CTA with motion underline
 * ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="border-cc-card-border border-t py-16 text-center">
      <h2 className="font-heading text-cc-heading text-h3 relative mx-auto inline-block leading-tight font-semibold text-balance">
        <span className="relative inline-block">
          Put your agents on a loop you control.
          <motion.svg
            aria-hidden="true"
            viewBox="0 0 400 8"
            preserveAspectRatio="none"
            className="pointer-events-none absolute -bottom-2 left-0 h-2 w-full"
          >
            <motion.path
              d="M 4 4 C 100 1 300 7 396 4"
              fill="none"
              stroke={VIOLET}
              strokeWidth={1.5}
              strokeLinecap="round"
              initial={{ pathLength: 0 }}
              whileInView={{ pathLength: 1 }}
              viewport={{ once: true, margin: "-80px" }}
              transition={{ duration: 1, ease: "easeOut" }}
            />
          </motion.svg>
        </span>
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-7 max-w-2xl text-base/relaxed">
        Expose your operations as governed tools, ground them in real field
        demand, and trace every call in the platform your team already runs.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/apis/client-registry">
          Read the Docs
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-6 text-sm">
        Or explore the{" "}
        <Link
          href="/docs/nitro/apis/client-registry"
          className="text-cc-info hover:text-cc-heading transition-colors"
        >
          client registry
        </Link>{" "}
        and the wider{" "}
        <Link
          href="/platform"
          className="text-cc-info hover:text-cc-heading transition-colors"
        >
          platform
        </Link>
        .
      </p>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Page
 * ------------------------------------------------------------------ */

export function AgenticCodingV7Client() {
  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <GroundingBand />
      <LifecycleConveyor />
      <BehaviorAnnotationsStrip />
      <SkillzBento />
      <HonestyBeat />
      <ClosingCta />
    </MotionConfig>
  );
}
