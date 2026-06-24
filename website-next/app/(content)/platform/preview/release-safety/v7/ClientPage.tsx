"use client";

import {
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
  animate,
  type MotionValue,
} from "motion/react";
import { useEffect, useRef, useState, type ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Scene tokens (mirror v1 so the page reads as the same family)       */
/* ------------------------------------------------------------------ */

const GUARDRAIL = "#0a1426";
const GUARDRAIL_RAISED = "rgba(13, 27, 48, 0.78)";
const GUARDRAIL_LINE = "rgba(124, 146, 198, 0.16)";
const CORAL = "#f0786a";

type ChangeStatus = "safe" | "dangerous" | "breaking";

const STATUS_META: Record<
  ChangeStatus,
  { label: string; text: string; bg: string; ring: string; dot: string }
> = {
  safe: {
    label: "SAFE",
    text: "text-cc-success",
    bg: "bg-cc-success/10",
    ring: "ring-cc-success/30",
    dot: "bg-cc-success",
  },
  dangerous: {
    label: "DANGEROUS",
    text: "text-cc-warning",
    bg: "bg-cc-warning/10",
    ring: "ring-cc-warning/30",
    dot: "bg-cc-warning",
  },
  breaking: {
    label: "BREAKING",
    text: "text-cc-danger",
    bg: "bg-cc-danger/10",
    ring: "ring-cc-danger/30",
    dot: "bg-cc-danger",
  },
};

const EASE_OUT: [number, number, number, number] = [0.22, 1, 0.36, 1];

/* ------------------------------------------------------------------ */
/* Small shared primitives                                             */
/* ------------------------------------------------------------------ */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      {children}
    </span>
  );
}

interface StatusChipProps {
  readonly status: ChangeStatus;
  readonly className?: string;
}

function StatusChip({ status, className = "" }: StatusChipProps) {
  const meta = STATUS_META[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${meta.bg} ${meta.text} ${meta.ring} ${className}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
      {meta.label}
    </span>
  );
}

interface SafeWordProps {
  readonly status: ChangeStatus;
  readonly children: ReactNode;
}

function SafeWord({ status, children }: SafeWordProps) {
  return (
    <span className={`font-medium ${STATUS_META[status].text}`}>
      {children}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Window chrome wrapper                                               */
/* ------------------------------------------------------------------ */

interface AppWindowProps {
  readonly title: ReactNode;
  readonly tab?: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
  readonly className?: string;
}

function AppWindow({
  title,
  tab,
  children,
  footer,
  className = "",
}: AppWindowProps) {
  return (
    <div
      className={`border-cc-card-border overflow-hidden rounded-xl border shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)] backdrop-blur-md ${className}`}
      style={{ backgroundColor: GUARDRAIL }}
    >
      <div
        className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5"
        style={{ backgroundColor: "#0d1b30" }}
      >
        <span className="flex gap-1.5" aria-hidden>
          <span className="bg-cc-danger/60 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-warning/60 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-success/60 h-2.5 w-2.5 rounded-full" />
        </span>
        <div className="text-cc-ink-dim ml-2 flex items-center gap-2 font-mono text-[0.72rem]">
          {title}
        </div>
        {tab !== undefined && (
          <span className="bg-cc-hover text-cc-ink-dim ml-auto rounded-md px-2 py-1 font-mono text-[0.66rem]">
            {tab}
          </span>
        )}
      </div>
      <div>{children}</div>
      {footer !== undefined && (
        <div
          className="border-cc-card-border border-t px-4 py-2.5"
          style={{ backgroundColor: "#0d1b30" }}
        >
          {footer}
        </div>
      )}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Syntax tokens                                                       */
/* ------------------------------------------------------------------ */

const tk = {
  kw: (s: string) => <span className="text-cc-info">{s}</span>,
  ty: (s: string) => <span className="text-cc-tip">{s}</span>,
  fld: (s: string) => <span className="text-cc-heading">{s}</span>,
  dir: (s: string) => <span className="text-cc-note">{s}</span>,
  punc: (s: string) => <span className="text-cc-ink-dim">{s}</span>,
};

/* ------------------------------------------------------------------ */
/* HERO                                                                */
/* Small preview chip cycles SAFE -> DANGEROUS -> BREAKING in view.    */
/* ------------------------------------------------------------------ */

function HeroPreviewChip() {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: false, margin: "-10%" });
  const order: readonly ChangeStatus[] = ["safe", "dangerous", "breaking"];
  const [i, setI] = useState(0);

  useEffect(() => {
    if (reduce === true || !inView) {
      return;
    }
    const id = window.setInterval(() => {
      setI((prev) => (prev + 1) % order.length);
    }, 1400);
    return () => window.clearInterval(id);
  }, [inView, reduce, order.length]);

  const status = order[i];

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg inline-flex items-center gap-2 rounded-full border px-3 py-1.5"
    >
      <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
        classifier
      </span>
      <motion.span
        key={status}
        initial={{ opacity: 0, scale: 0.85 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.32, ease: EASE_OUT }}
      >
        <StatusChip status={status} />
      </motion.span>
    </div>
  );
}

function HeroStillPreview() {
  return (
    <AppWindow
      title={
        <>
          <span className="text-cc-prose">schema.graphql</span>
          <span className="text-cc-nav-label">·</span>
          <span>orders-api</span>
        </>
      }
      tab="diff · main…release"
      footer={
        <div className="flex items-center justify-between">
          <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
            <span className="bg-cc-danger h-2 w-2 rounded-full" />
            registry check failed
          </span>
          <span className="text-cc-nav-label font-mono text-[0.66rem]">
            1 breaking · 1 dangerous · 1 safe
          </span>
        </div>
      }
    >
      <div className="bg-cc-success/[0.04] text-cc-ink-dim px-4 py-1.5 font-mono text-[0.64rem]">
        @@ type Order @@
      </div>
      <div className="px-4 py-3 font-mono text-[0.78rem] leading-relaxed">
        <div className="text-cc-prose">
          {tk.kw("type")} {tk.ty("Order")} {tk.punc("{")}
        </div>
        <div className="text-cc-prose">
          {"  "}
          {tk.fld("id")}
          {tk.punc(": ID!")}
        </div>
        <div className="bg-cc-success/[0.06] -mx-4 flex items-center justify-between px-4 py-0.5">
          <span className="text-cc-prose">
            <span className="text-cc-success/70">+ </span>
            {tk.fld("totalAmount")}
            {tk.punc(": ")}
            {tk.ty("Money!")}
          </span>
          <StatusChip status="safe" />
        </div>
        <div className="bg-cc-danger/[0.07] -mx-4 flex items-center justify-between px-4 py-0.5">
          <span className="text-cc-prose">
            <span className="text-cc-danger/70">- </span>
            {tk.fld("total")}
            {tk.punc(": ")}
            {tk.ty("Float!")}
          </span>
          <StatusChip status="breaking" />
        </div>
        <div className="text-cc-prose">
          {"  "}
          {tk.fld("status")}
          {tk.punc(": ")}
          {tk.ty("OrderStatus!")}
        </div>
        <div className="bg-cc-success/[0.06] -mx-4 flex items-center justify-between px-4 py-0.5">
          <span className="text-cc-prose">
            <span className="text-cc-success/70">+ </span>
            {tk.fld("placedAt")}
            {tk.punc(": ")}
            {tk.ty("DateTime")} {tk.dir("@deprecated")}
          </span>
          <StatusChip status="dangerous" />
        </div>
        <div className="text-cc-prose">{tk.punc("}")}</div>
      </div>
    </AppWindow>
  );
}

function HeroSection() {
  const facts = [
    { k: "classify", v: "scrub the diff" },
    { k: "gate", v: "CI blocks unsafe" },
    { k: "feedback", v: "in your build" },
  ];
  return (
    <section className="grid items-center gap-12 lg:grid-cols-[minmax(0,0.92fr)_minmax(0,1.08fr)]">
      <div>
        <Eyebrow>Platform · Release Safety</Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-bold tracking-tight">
          Safe GraphQL schema
          <br />
          evolution, scrubbable.
        </h1>
        <p className="lead text-cc-ink-dim mt-6 max-w-xl">
          Scroll the diff. Watch each change land. Stop the breaking ones at the
          gate.
        </p>
        <p className="text-body text-cc-prose mt-5 max-w-xl leading-relaxed">
          Every edit is classified <SafeWord status="safe">safe</SafeWord>,{" "}
          <SafeWord status="dangerous">dangerous</SafeWord>, or{" "}
          <SafeWord status="breaking">breaking</SafeWord>, validated against the
          clients you have actually published, and only then promoted. Unsafe
          releases stop at the gate, before a consumer ever discovers them.
        </p>
        <div className="mt-7">
          <HeroPreviewChip />
        </div>
        <div className="mt-9 flex flex-wrap items-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/continuous-integration">
            Read the Docs
          </OutlineButton>
        </div>
        <dl className="border-cc-card-border mt-10 grid max-w-md grid-cols-3 gap-px overflow-hidden rounded-lg border">
          {facts.map((s) => (
            <div
              key={s.k}
              className="px-3 py-3"
              style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
            >
              <dt className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.16em] uppercase">
                {s.k}
              </dt>
              <dd className="text-cc-prose mt-1 font-mono text-[0.72rem]">
                {s.v}
              </dd>
            </div>
          ))}
        </dl>
      </div>
      <div className="relative">
        <div
          aria-hidden
          className="absolute -inset-6 -z-10 rounded-3xl opacity-60 blur-2xl"
          style={{
            background:
              "radial-gradient(60% 60% at 70% 20%, rgba(124,146,198,0.18), transparent 70%)",
          }}
        />
        <HeroStillPreview />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CENTERPIECE: Classifier Reel                                        */
/* One-shot enter animation drives line reveals + chip stamps,         */
/* BLOCKED stamp slam, and connector draw to affected client avatars.  */
/* ------------------------------------------------------------------ */

interface ReelLine {
  readonly code: ReactNode;
  readonly sign: "+" | "-" | " ";
  readonly status?: ChangeStatus;
}

const REEL_LINES: readonly ReelLine[] = [
  {
    sign: " ",
    code: (
      <>
        {tk.kw("type")} {tk.ty("Order")} {tk.punc("{")}
      </>
    ),
  },
  {
    sign: " ",
    code: (
      <>
        {"  "}
        {tk.fld("id")}
        {tk.punc(": ID!")}
      </>
    ),
  },
  {
    sign: "+",
    status: "safe",
    code: (
      <>
        {"  "}
        {tk.fld("totalAmount")}
        {tk.punc(": ")}
        {tk.ty("Money!")}
      </>
    ),
  },
  {
    sign: "+",
    status: "dangerous",
    code: (
      <>
        {"  "}
        {tk.fld("placedAt")}
        {tk.punc(": ")}
        {tk.ty("DateTime")} {tk.dir("@deprecated")}
      </>
    ),
  },
  {
    sign: "-",
    status: "breaking",
    code: (
      <>
        {"  "}
        {tk.fld("total")}
        {tk.punc(": ")}
        {tk.ty("Float!")}
      </>
    ),
  },
  {
    sign: " ",
    code: <>{tk.punc("}")}</>,
  },
];

const BREAKING_INDEX = REEL_LINES.findIndex((l) => l.status === "breaking");
const LINE_PHASE_END = 0.62;

interface ReelLineRowProps {
  readonly line: ReelLine;
  readonly index: number;
  readonly progress: MotionValue<number>;
  readonly total: number;
  readonly reduce: boolean;
}

function ReelLineRow({
  line,
  index,
  progress,
  total,
  reduce,
}: ReelLineRowProps) {
  const slice = LINE_PHASE_END / total;
  const start = index * slice;
  const end = start + slice * 0.9;

  const opacity = useTransform(progress, [start, end], [0, 1], {
    clamp: true,
  });
  const x = useTransform(progress, [start, end], [-12, 0], { clamp: true });
  const chipScale = useTransform(progress, [end - slice * 0.4, end], [0, 1], {
    clamp: true,
  });
  const chipRotate = useTransform(
    progress,
    [end - slice * 0.4, end],
    [-12, 0],
    { clamp: true },
  );

  const rowBg =
    line.sign === "+"
      ? "bg-cc-success/[0.06]"
      : line.sign === "-"
        ? "bg-cc-danger/[0.07]"
        : "";

  const gutterColor =
    line.sign === "+"
      ? "text-cc-success/70"
      : line.sign === "-"
        ? "text-cc-danger/70"
        : "text-cc-nav-label";

  return (
    <div className={`relative flex items-stretch ${rowBg}`}>
      <span className="border-cc-card-border text-cc-nav-label/70 w-9 shrink-0 border-r py-1.5 pr-2 text-right font-mono text-[0.66rem] select-none">
        {index + 1}
      </span>
      <span
        className={`w-5 shrink-0 py-1.5 pl-2 font-mono text-[0.78rem] select-none ${gutterColor}`}
      >
        {line.sign}
      </span>
      <motion.span
        style={reduce ? { opacity: 1, x: 0 } : { opacity, x }}
        className="text-cc-prose min-w-0 flex-1 py-1.5 pr-3 font-mono text-[0.78rem] leading-relaxed whitespace-pre"
      >
        {line.code}
      </motion.span>
      {line.status !== undefined && (
        <motion.span
          style={
            reduce
              ? { scale: 1, rotate: 0 }
              : { scale: chipScale, rotate: chipRotate }
          }
          className="flex shrink-0 items-center py-1.5 pr-3"
        >
          <StatusChip status={line.status} />
        </motion.span>
      )}
    </div>
  );
}

interface ClientAvatarProps {
  readonly initial: string;
  readonly label: string;
  readonly active: MotionValue<number>;
  readonly reduce: boolean;
}

function ClientAvatar({ initial, label, active, reduce }: ClientAvatarProps) {
  const opacity = useTransform(active, [0, 1], [0.35, 1], { clamp: true });
  const scale = useTransform(active, [0, 1], [0.94, 1], { clamp: true });
  return (
    <motion.div
      style={reduce ? { opacity: 1, scale: 1 } : { opacity, scale }}
      className="border-cc-danger/40 bg-cc-danger/[0.06] flex items-center gap-2 rounded-lg border px-3 py-2"
    >
      <span className="bg-cc-danger/15 text-cc-danger flex h-7 w-7 items-center justify-center rounded-full font-mono text-[0.7rem] font-semibold">
        {initial}
      </span>
      <div className="min-w-0">
        <div className="text-cc-heading font-mono text-[0.72rem]">{label}</div>
        <div className="text-cc-nav-label font-mono text-[0.6rem]">
          published client
        </div>
      </div>
    </motion.div>
  );
}

interface ConnectorSvgProps {
  readonly draw: MotionValue<number>;
  readonly reduce: boolean;
}

function ConnectorSvg({ draw, reduce }: ConnectorSvgProps) {
  const length = 240;
  const offset = useTransform(draw, (v) => v * length);

  // viewBox spans the full grid: x=0 is the left edge of the diff window,
  // x=100 is the right edge of the avatar column. The breaking line sits
  // a little past the middle of the diff window; the avatars stack on the
  // lower right. Paths originate at the right edge of the diff window
  // (x=55) at the breaking line's y (~58) and fan out to the three avatars.
  const paths = [
    "M 55 58 C 70 62, 84 76, 96 78",
    "M 55 58 C 72 70, 86 96, 96 100",
    "M 55 58 C 72 80, 86 116, 96 122",
  ];

  return (
    <svg
      aria-hidden
      viewBox="0 0 100 130"
      preserveAspectRatio="none"
      className="pointer-events-none absolute inset-0 h-full w-full"
    >
      {paths.map((d, i) => (
        <motion.path
          key={i}
          d={d}
          stroke={CORAL}
          strokeWidth={0.5}
          fill="none"
          strokeLinecap="round"
          strokeDasharray={length}
          style={{ strokeDashoffset: reduce ? 0 : offset }}
          vectorEffect="non-scaling-stroke"
        />
      ))}
    </svg>
  );
}

function ClassifierReel() {
  const containerRef = useRef<HTMLDivElement>(null);
  const reduceMotion = useReducedMotion();
  const reduce = reduceMotion === true;
  const inView = useInView(containerRef, { once: true, margin: "-15%" });

  // Local 0 -> 1 motion value, animated once on enter view. The downstream
  // useTransform ranges below (breakingEnd, connectorStart, etc.) keep the
  // same staging the scrub used to express, so the visual sequence reads
  // the same without coupling to scroll position.
  const progress = useMotionValue(reduce ? 1 : 0);

  useEffect(() => {
    if (reduce) {
      progress.set(1);
      return;
    }
    if (!inView) {
      return;
    }
    const controls = animate(progress, 1, {
      duration: 2.6,
      ease: EASE_OUT,
    });
    return () => controls.stop();
  }, [inView, reduce, progress]);

  const breakingEnd =
    (BREAKING_INDEX + 0.9) * (LINE_PHASE_END / REEL_LINES.length);

  const gateScale = useTransform(
    progress,
    [breakingEnd, breakingEnd + 0.12],
    [0, 1],
    { clamp: true },
  );
  const gateRotate = useTransform(
    progress,
    [breakingEnd, breakingEnd + 0.12],
    [-18, -6],
    { clamp: true },
  );
  const mergeOpacity = useTransform(
    progress,
    [breakingEnd, breakingEnd + 0.08],
    [1, 0.32],
    { clamp: true },
  );

  const connectorStart = breakingEnd + 0.12;
  const connectorEnd = connectorStart + 0.18;
  const connectorDraw = useTransform(
    progress,
    [connectorStart, connectorEnd],
    [1, 0],
    { clamp: true },
  );
  const clientActive = useTransform(
    progress,
    [connectorStart + 0.04, connectorEnd],
    [0, 1],
    { clamp: true },
  );

  return (
    <section ref={containerRef} className="relative">
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <Eyebrow>The reel · scroll to scrub</Eyebrow>
          <span className="text-cc-nav-label hidden font-mono text-[0.66rem] sm:inline">
            classifier · CI gate · published clients affected
          </span>
        </div>
        <div className="relative grid gap-4 lg:grid-cols-[minmax(0,1.15fr)_minmax(0,1fr)]">
          <ConnectorSvg draw={connectorDraw} reduce={reduce} />
          <AppWindow
            title={
              <>
                <span className="text-cc-prose">schema.graphql</span>
                <span className="text-cc-nav-label">·</span>
                <span>orders-api · #482</span>
              </>
            }
            tab="diff · main…release"
          >
            <div className="bg-cc-success/[0.04] text-cc-ink-dim px-4 py-1.5 font-mono text-[0.64rem]">
              @@ type Order @@
            </div>
            <div>
              {REEL_LINES.map((line, i) => (
                <ReelLineRow
                  key={i}
                  line={line}
                  index={i}
                  progress={progress}
                  total={REEL_LINES.length}
                  reduce={reduce}
                />
              ))}
            </div>
          </AppWindow>

          <div className="relative flex flex-col gap-4">
            <AppWindow
              title={
                <>
                  <span>CI</span>
                  <span className="text-cc-nav-label">·</span>
                  <span className="text-cc-prose">registry check</span>
                </>
              }
              tab="pull request"
            >
              <div className="relative px-4 py-5">
                <div className="flex items-center gap-3">
                  <span className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.16em] uppercase">
                    merge
                  </span>
                  <motion.span
                    style={{ opacity: reduce ? 0.32 : mergeOpacity }}
                    className="border-cc-card-border text-cc-ink-dim rounded-md border px-3 py-1 font-mono text-[0.7rem]"
                  >
                    Squash and merge
                  </motion.span>
                </div>
                <p className="text-cc-ink-dim mt-3 font-mono text-[0.7rem] leading-relaxed">
                  Removing{" "}
                  <code className="bg-cc-hover text-cc-prose rounded px-1">
                    Order.total
                  </code>{" "}
                  is a breaking change. The gate holds the merge until the
                  change is reshaped or its usage clears.
                </p>
                <motion.div
                  style={{
                    scale: reduce ? 1 : gateScale,
                    rotate: reduce ? -6 : gateRotate,
                    color: CORAL,
                    borderColor: CORAL,
                  }}
                  className="pointer-events-none absolute top-4 right-4 rounded-md border-2 px-3 py-1.5 font-mono text-[0.82rem] font-bold tracking-[0.18em] select-none"
                >
                  BLOCKED
                </motion.div>
              </div>
            </AppWindow>

            <div className="relative">
              <div className="bg-cc-card-bg border-cc-card-border relative grid gap-2 rounded-xl border p-3">
                <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.16em] uppercase">
                  3 published clients affected
                </div>
                <ClientAvatar
                  initial="W"
                  label="web · production"
                  active={clientActive}
                  reduce={reduce}
                />
                <ClientAvatar
                  initial="M"
                  label="mobile · production"
                  active={clientActive}
                  reduce={reduce}
                />
                <ClientAvatar
                  initial="P"
                  label="partner · sandbox"
                  active={clientActive}
                  reduce={reduce}
                />
              </div>
            </div>
          </div>
        </div>
        <p className="text-cc-ink-dim mt-2 max-w-2xl font-mono text-[0.7rem] leading-relaxed">
          Scrub forward and back. Each new or removed line stamps a status as it
          lands; the breaking line slams the CI gate and exposes the published
          clients it would have hit.
        </p>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: Validate -> Publish gate with a gliding token              */
/* ------------------------------------------------------------------ */

interface GateNodeProps {
  readonly kicker: string;
  readonly label: string;
  readonly sub: string;
  readonly tone: "neutral" | "warning" | "success" | "danger";
  readonly compact?: boolean;
}

function GateNode({
  kicker,
  label,
  sub,
  tone,
  compact = false,
}: GateNodeProps) {
  const tones = {
    neutral: "border-cc-card-border text-cc-heading",
    warning: "border-cc-warning/40 text-cc-warning",
    success: "border-cc-success/40 text-cc-success",
    danger: "border-cc-danger/40 text-cc-danger",
  } as const;
  return (
    <div
      className={`flex-1 rounded-lg border ${tones[tone]} px-4 ${compact ? "py-2.5" : "py-4"}`}
      style={{ backgroundColor: GUARDRAIL_RAISED }}
    >
      <div className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] uppercase">
        {kicker}
      </div>
      <div className="mt-1 font-mono text-[0.85rem] font-semibold">{label}</div>
      <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.64rem]">
        {sub}
      </div>
    </div>
  );
}

function GateFlow() {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15%" });

  const tokenAnim =
    reduce === true
      ? { left: "92%", top: "26px" }
      : inView
        ? {
            left: ["0%", "44%", "44%", "92%"],
            top: ["0px", "0px", "26px", "26px"],
          }
        : { left: "0%", top: "0px" };

  return (
    <div
      ref={ref}
      className="border-cc-card-border relative overflow-hidden rounded-xl border p-6 sm:p-8"
      style={{
        backgroundColor: GUARDRAIL,
        backgroundImage: `linear-gradient(${GUARDRAIL_LINE} 1px, transparent 1px), linear-gradient(90deg, ${GUARDRAIL_LINE} 1px, transparent 1px)`,
        backgroundSize: "26px 26px",
      }}
    >
      <div className="flex flex-col items-stretch gap-3 sm:flex-row sm:items-center">
        <GateNode
          kicker="01"
          label="schema change"
          sub="pull request"
          tone="neutral"
        />
        <span className="text-cc-nav-label/60 flex items-center justify-center font-mono text-sm">
          →
        </span>
        <GateNode
          kicker="02"
          label="validate"
          sub="classify + check clients"
          tone="warning"
        />
        <span className="text-cc-nav-label/60 flex items-center justify-center font-mono text-sm">
          →
        </span>
        <div className="flex flex-1 flex-col gap-3">
          <GateNode
            kicker="03a"
            label="publish"
            sub="safe → promoted"
            tone="success"
            compact
          />
          <GateNode
            kicker="03b"
            label="blocked"
            sub="breaking → stop"
            tone="danger"
            compact
          />
        </div>
      </div>
      <div className="relative mt-4 hidden h-8 sm:block" aria-hidden>
        <motion.span
          className="bg-cc-heading absolute block h-2 w-2 rounded-full"
          initial={false}
          animate={tokenAnim}
          transition={{
            duration: reduce === true ? 0 : 2.6,
            ease: [0.65, 0, 0.35, 1],
            times: reduce === true ? undefined : [0, 0.35, 0.55, 1],
          }}
        />
      </div>
      <p className="text-cc-ink-dim mt-4 max-w-2xl font-mono text-[0.72rem] leading-relaxed">
        Nothing reaches <span className="text-cc-prose">publish</span> until it
        clears <span className="text-cc-warning">validate</span>. A breaking
        change never advances; it stops at the gate with the reason attached.
      </p>
    </div>
  );
}

function GateSection() {
  return (
    <SectionShell
      eyebrow="The flow"
      title="Validate, then publish. Never the other way around."
      lead="Release safety is a two-stage gate. A change is classified and checked against published clients first; only changes that clear validation are promoted. The order is the guarantee."
      bullets={[
        "Validate classifies the change and tests it against real clients.",
        "Publish only ever runs on a change that already cleared validation.",
        "A blocked change carries its reason, not just a red X.",
      ]}
      artifact={<GateFlow />}
      flip
    />
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: Blast radius matrix with cell-fill animation               */
/* ------------------------------------------------------------------ */

interface ClientRow {
  readonly name: string;
  readonly env: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "queued";
}

const CLIENT_ROWS: readonly ClientRow[] = [
  { name: "web", env: "production", ok: 5, total: 5, status: "ok" },
  { name: "mobile", env: "production", ok: 3, total: 5, status: "risk" },
  { name: "partner", env: "sandbox", ok: 0, total: 4, status: "queued" },
  { name: "internal-admin", env: "staging", ok: 6, total: 6, status: "ok" },
];

interface ImpactBarProps {
  readonly ok: number;
  readonly total: number;
  readonly status: ClientRow["status"];
  readonly rowIndex: number;
}

function ImpactBar({ ok, total, status, rowIndex }: ImpactBarProps) {
  const reduce = useReducedMotion();
  const cells = Array.from({ length: total });
  const color =
    status === "ok"
      ? "bg-cc-success"
      : status === "risk"
        ? "bg-cc-warning"
        : "bg-cc-nav-label/50";
  return (
    <span className="flex gap-1">
      {cells.map((_, i) => {
        const filled = i < ok;
        const cls = `block h-2 w-5 rounded-[2px] ${filled ? color : "bg-cc-ink-faint"}`;
        if (reduce === true) {
          return <span key={i} className={cls} />;
        }
        return (
          <motion.span
            key={i}
            initial={{ opacity: 0.2, scaleX: 0 }}
            whileInView={{ opacity: 1, scaleX: 1 }}
            viewport={{ once: true, margin: "-10%" }}
            transition={{
              duration: 0.32,
              ease: EASE_OUT,
              delay: 0.08 * rowIndex + 0.05 * i,
            }}
            style={{ originX: 0 }}
            className={cls}
          />
        );
      })}
    </span>
  );
}

interface StatusPillProps {
  readonly status: ClientRow["status"];
  readonly rowIndex: number;
}

function StatusPill({ status, rowIndex }: StatusPillProps) {
  const reduce = useReducedMotion();
  const cls = {
    ok: "text-cc-success",
    risk: "text-cc-warning",
    queued: "text-cc-nav-label",
  }[status];
  const text = { ok: "OK", risk: "at risk", queued: "queued" }[status];
  if (reduce === true) {
    return (
      <span
        className={`text-right font-mono text-[0.72rem] font-semibold ${cls}`}
      >
        {text}
      </span>
    );
  }
  return (
    <motion.span
      initial={{ opacity: 0 }}
      whileInView={{ opacity: 1 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ delay: 0.08 * rowIndex + 0.35, duration: 0.32 }}
      className={`text-right font-mono text-[0.72rem] font-semibold ${cls}`}
    >
      {text}
    </motion.span>
  );
}

function ClientImpactMatrix() {
  return (
    <AppWindow
      title={
        <>
          <span>client registry</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">impact of #482</span>
        </>
      }
      tab="published clients affected"
    >
      <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[1.3fr_1fr_0.8fr] gap-3 border-b px-4 py-2 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
        <span>client</span>
        <span>operations passing</span>
        <span className="text-right">status</span>
      </div>
      {CLIENT_ROWS.map((c, idx) => (
        <div
          key={c.name}
          className="border-cc-card-border grid grid-cols-[1.3fr_1fr_0.8fr] items-center gap-3 border-b px-4 py-3 last:border-b-0"
        >
          <div className="min-w-0">
            <div className="text-cc-heading truncate font-mono text-[0.78rem]">
              {c.name}
            </div>
            <div className="text-cc-nav-label font-mono text-[0.62rem]">
              {c.env}
            </div>
          </div>
          <div className="flex items-center gap-3">
            <ImpactBar
              ok={c.ok}
              total={c.total}
              status={c.status}
              rowIndex={idx}
            />
            <span className="text-cc-ink-dim font-mono text-[0.68rem]">
              {c.ok}/{c.total}
            </span>
          </div>
          <div className="text-right">
            <StatusPill status={c.status} rowIndex={idx} />
          </div>
        </div>
      ))}
    </AppWindow>
  );
}

function ImpactSection() {
  return (
    <SectionShell
      eyebrow="Blast radius"
      title="Know who a change touches."
      lead="The client registry tracks the operations your published clients actually run. Before a change ships, you can see which clients are clear and which would have operations break, by name and by environment."
      bullets={[
        "Validation runs against the operations published clients send.",
        "Each client reports passing operations, not a vague global verdict.",
        "Queued clients are validated as soon as their operations are registered.",
      ]}
      artifact={<ClientImpactMatrix />}
    />
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: Schema history timeline with drawn trunk + popping dots    */
/* ------------------------------------------------------------------ */

interface VersionPoint {
  readonly v: string;
  readonly note: string;
  readonly status: ChangeStatus;
  readonly wobble?: boolean;
}

const VERSIONS: readonly VersionPoint[] = [
  { v: "v12", note: "add Cart.discount", status: "safe" },
  { v: "v13", note: "deprecate Order.placedAt", status: "dangerous" },
  {
    v: "v14",
    note: "remove Order.total, blocked",
    status: "breaking",
    wobble: true,
  },
  { v: "v14", note: "add Order.totalAmount", status: "safe" },
  { v: "v15", note: "drop Order.total, usage cleared", status: "dangerous" },
];

function TrunkLine({
  trackRef,
}: {
  readonly trackRef: React.RefObject<HTMLDivElement | null>;
}) {
  const reduce = useReducedMotion();
  const inView = useInView(trackRef, { once: true, margin: "-10%" });
  return (
    <svg
      aria-hidden
      className="absolute top-2.5 left-2 h-[calc(100%-1.25rem)] w-px overflow-visible"
      viewBox="0 0 1 100"
      preserveAspectRatio="none"
    >
      <motion.line
        x1="0.5"
        y1="0"
        x2="0.5"
        y2="100"
        stroke="var(--color-cc-card-border)"
        strokeWidth={1}
        strokeDasharray={100}
        initial={{ strokeDashoffset: reduce === true ? 0 : 100 }}
        animate={{ strokeDashoffset: reduce === true || inView ? 0 : 100 }}
        transition={{ duration: 1.1, ease: EASE_OUT }}
      />
    </svg>
  );
}

function VersionTimeline() {
  const trackRef = useRef<HTMLDivElement>(null);
  const reduce = useReducedMotion();

  return (
    <AppWindow
      title={
        <>
          <span>orders-api</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">schema history</span>
        </>
      }
      tab="registry"
    >
      <div className="px-5 py-6">
        <div ref={trackRef} className="relative">
          <TrunkLine trackRef={trackRef} />
          <ol className="space-y-5">
            {VERSIONS.map((p, i) => {
              const meta = STATUS_META[p.status];
              return (
                <motion.li
                  key={i}
                  initial={reduce === true ? false : { opacity: 0, y: 6 }}
                  whileInView={
                    reduce === true ? undefined : { opacity: 1, y: 0 }
                  }
                  viewport={{ once: true, margin: "-10%" }}
                  transition={{
                    duration: 0.4,
                    delay: i * 0.08,
                    ease: EASE_OUT,
                  }}
                  className="relative flex items-center gap-4 pl-7"
                >
                  <motion.span
                    initial={reduce === true ? false : { scale: 0 }}
                    whileInView={reduce === true ? undefined : { scale: 1 }}
                    viewport={{ once: true, margin: "-10%" }}
                    transition={{
                      delay: 0.12 + i * 0.08,
                      type: "spring",
                      stiffness: 320,
                      damping: 14,
                    }}
                    className={`absolute left-0 flex h-4 w-4 items-center justify-center rounded-full ${meta.bg} ring-1 ring-inset ${meta.ring}`}
                  >
                    <motion.span
                      animate={
                        reduce === true || p.wobble !== true
                          ? undefined
                          : { x: [0, -2, 2, -1.5, 1.5, 0] }
                      }
                      transition={{
                        delay: 0.45 + i * 0.08,
                        duration: 0.7,
                        ease: EASE_OUT,
                      }}
                      className={`h-1.5 w-1.5 rounded-full ${meta.dot}`}
                    />
                  </motion.span>
                  <span className="text-cc-heading w-10 shrink-0 font-mono text-[0.74rem] font-semibold">
                    {p.v}
                  </span>
                  <span className="text-cc-prose min-w-0 flex-1 truncate font-mono text-[0.74rem]">
                    {p.note}
                  </span>
                  <StatusChip status={p.status} />
                </motion.li>
              );
            })}
          </ol>
        </div>
        <p className="text-cc-ink-dim mt-6 font-mono text-[0.7rem] leading-relaxed">
          The breaking removal at <span className="text-cc-danger">v14</span>{" "}
          never shipped. It was re-shaped as an additive change, deprecated, and
          only dropped at <span className="text-cc-warning">v15</span> once
          client usage cleared.
        </p>
      </div>
    </AppWindow>
  );
}

function TimelineSection() {
  return (
    <SectionShell
      eyebrow="The record"
      title="Every version, every classification, kept."
      lead="The registry keeps the full history of your schema with each change classified in place. You can see exactly when a contract shifted, why it was safe, and how a risky change was reshaped before it shipped."
      bullets={[
        "Each published version records its classification.",
        "Deprecations buy time; removals wait for usage to clear.",
        "Drift between code and the published contract shows up as build feedback.",
      ]}
      artifact={<VersionTimeline />}
    />
  );
}

/* ------------------------------------------------------------------ */
/* Reusable two-column section shell                                   */
/* ------------------------------------------------------------------ */

interface SectionShellProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly lead: string;
  readonly bullets: readonly string[];
  readonly artifact: ReactNode;
  readonly flip?: boolean;
}

function SectionShell({
  eyebrow,
  title,
  lead,
  bullets,
  artifact,
  flip = false,
}: SectionShellProps) {
  return (
    <motion.section
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-15%" }}
      transition={{ duration: 0.5, ease: EASE_OUT }}
      className="grid items-center gap-10 lg:grid-cols-2 lg:gap-14"
    >
      <div className={flip ? "lg:order-2" : ""}>
        <Eyebrow>{eyebrow}</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          {title}
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">{lead}</p>
        <ul className="mt-6 space-y-3">
          {bullets.map((b) => (
            <li key={b} className="flex items-start gap-3">
              <span className="bg-cc-success/12 text-cc-success ring-cc-success/25 mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full ring-1 ring-inset">
                <CheckIcon size={11} />
              </span>
              <span className="text-cc-ink-dim text-[0.92rem] leading-relaxed">
                {b}
              </span>
            </li>
          ))}
        </ul>
      </div>
      <div className={flip ? "lg:order-1" : ""}>{artifact}</div>
    </motion.section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: Reliability band with count-up motion                      */
/* ------------------------------------------------------------------ */

interface CountUpProps {
  readonly to: number;
  readonly suffix?: string;
  readonly className?: string;
  readonly active: boolean;
}

function CountUp({ to, suffix = "", className = "", active }: CountUpProps) {
  const reduce = useReducedMotion();
  const value = useMotionValue(0);
  const [display, setDisplay] = useState(0);

  useEffect(() => {
    if (!active || reduce === true) {
      return;
    }
    const controls = animate(value, to, {
      duration: 1.2,
      ease: EASE_OUT,
      onUpdate: (latest) => setDisplay(Math.round(latest)),
    });
    return () => controls.stop();
  }, [active, reduce, to, value]);

  const shown = reduce === true && active ? to : display;

  return (
    <span className={className}>
      {shown}
      {suffix}
    </span>
  );
}

function ReliabilityBand() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-20%" });

  const stats = [
    { n: 0, suffix: "", l: "breaking changes shipped", c: "text-cc-success" },
    {
      n: 12,
      suffix: "",
      l: "published clients guarded",
      c: "text-cc-heading",
    },
    { n: 100, suffix: "%", l: "of releases checked", c: "text-cc-heading" },
  ];
  return (
    <section
      ref={ref}
      className="border-cc-card-border overflow-hidden rounded-2xl border"
      style={{
        backgroundColor: GUARDRAIL,
        backgroundImage:
          "radial-gradient(80% 120% at 50% -20%, rgba(124,146,198,0.12), transparent 60%)",
      }}
    >
      <div className="divide-cc-card-border grid divide-y sm:grid-cols-3 sm:divide-x sm:divide-y-0">
        {stats.map((s) => (
          <div key={s.l} className="px-6 py-9 text-center">
            <CountUp
              to={s.n}
              suffix={s.suffix}
              active={inView}
              className={`font-heading text-h2 font-bold tracking-tight ${s.c}`}
            />
            <div className="text-cc-nav-label mt-2 font-mono text-[0.66rem] tracking-[0.14em] uppercase">
              {s.l}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: Honesty beat with staggered reveal                         */
/* ------------------------------------------------------------------ */

interface HonestyItemProps {
  readonly tone: "success" | "warning";
  readonly head: string;
  readonly body: ReactNode;
  readonly delay: number;
}

function HonestyItem({ tone, head, body, delay }: HonestyItemProps) {
  const bar = tone === "success" ? "bg-cc-success" : "bg-cc-warning";
  return (
    <motion.li
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ duration: 0.45, delay, ease: EASE_OUT }}
      className="border-cc-card-border bg-cc-card-bg relative rounded-lg border px-4 py-4"
    >
      <span
        className={`absolute top-4 bottom-4 left-0 w-[3px] rounded-full ${bar}`}
      />
      <div className="pl-3">
        <div className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.14em] uppercase">
          {head}
        </div>
        <p className="text-cc-ink-dim mt-2 text-[0.86rem] leading-relaxed">
          {body}
        </p>
      </div>
    </motion.li>
  );
}

function HonestySection() {
  return (
    <section
      className="border-cc-card-border rounded-2xl border px-6 py-9 sm:px-10 sm:py-11"
      style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
    >
      <div className="max-w-3xl">
        <Eyebrow>What this does and does not promise</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          A safety net, not a blindfold.
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">
          A check is only useful if you can trust what it claims. Release safety
          tells you which <em>published</em> clients are affected by a change,
          based on the operations they have registered. It is honest about its
          edges.
        </p>
        <ul className="mt-7 grid gap-4 sm:grid-cols-2">
          <HonestyItem
            tone="success"
            head="What it stops"
            body="Releases that would break a published client are blocked before merge, with the breaking line and reason in hand."
            delay={0}
          />
          <HonestyItem
            tone="warning"
            head="What it needs"
            body="A client is only guarded once its operations are registered. Unregistered traffic is outside the net."
            delay={0.08}
          />
          <HonestyItem
            tone="success"
            head="What it surfaces"
            body="Strawberry Shake regenerates clients via MSBuild codegen, so contract drift shows up as build feedback you cannot miss."
            delay={0.16}
          />
          <HonestyItem
            tone="warning"
            head="What it will not pretend"
            body="It reports published clients affected. It does not claim certainty about consumers it has never seen."
            delay={0.24}
          />
        </ul>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: Closing CTA with a single coral underline draw             */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  const reduce = useReducedMotion();
  return (
    <section className="border-cc-card-border relative overflow-hidden rounded-2xl border px-6 py-14 text-center sm:px-12">
      <div
        aria-hidden
        className="absolute inset-0 -z-10"
        style={{
          backgroundColor: GUARDRAIL,
          backgroundImage:
            "radial-gradient(70% 100% at 50% 0%, rgba(124,146,198,0.16), transparent 65%)",
        }}
      />
      <h2 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
        Put a safety net under every schema change.
      </h2>
      <div className="mx-auto mt-3 h-3 w-44">
        <svg
          viewBox="0 0 200 8"
          className="h-full w-full"
          preserveAspectRatio="none"
          aria-hidden
        >
          <motion.path
            d="M 4 4 Q 100 0 196 4"
            stroke={CORAL}
            strokeWidth={2}
            strokeLinecap="round"
            fill="none"
            strokeDasharray={220}
            initial={
              reduce === true
                ? { strokeDashoffset: 0 }
                : { strokeDashoffset: 220 }
            }
            whileInView={{ strokeDashoffset: 0 }}
            viewport={{ once: true, margin: "-20%" }}
            transition={{
              duration: reduce === true ? 0 : 1.1,
              ease: EASE_OUT,
            }}
          />
        </svg>
      </div>
      <p className="text-body text-cc-prose mx-auto mt-5 max-w-xl leading-relaxed">
        Classify, validate, and gate your releases so breaking changes stop at
        the door, not in your users&rsquo; hands.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/platform/continuous-integration">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <ClassifierReel />
      <GateSection />
      <ImpactSection />
      <TimelineSection />
      <ReliabilityBand />
      <HonestySection />
      <ClosingCta />
    </div>
  );
}
