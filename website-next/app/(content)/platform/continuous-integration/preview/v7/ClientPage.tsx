"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import {
  AnimatePresence,
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Metadata                                                            */
/* ------------------------------------------------------------------ */

/* ------------------------------------------------------------------ */
/* Scene tokens (cc-* dark, cc-accent teal)                            */
/* ------------------------------------------------------------------ */

const TRACK_BG = "#0a1426";
const TRACK_RAISED = "rgba(13, 27, 48, 0.78)";
const TRACK_DEEP = "#0d1b30";
const TRACK_LINE = "rgba(124, 146, 198, 0.16)";
const SUNK = "#0b1525";

type StageState = "safe" | "dangerous" | "breaking";

const VERDICT_META: Record<
  StageState,
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

interface VerdictChipProps {
  readonly status: StageState;
  readonly className?: string;
}

function VerdictChip({ status, className = "" }: VerdictChipProps) {
  const meta = VERDICT_META[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${meta.bg} ${meta.text} ${meta.ring} ${className}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
      {meta.label}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Hero                                                                */
/* ------------------------------------------------------------------ */

function HeroRailUnderline() {
  return (
    <svg
      viewBox="0 0 600 24"
      width="100%"
      height={24}
      className="text-cc-accent mt-6 block max-w-2xl"
      aria-hidden
    >
      <motion.path
        d="M2 12 H560"
        stroke="currentColor"
        strokeWidth="1.5"
        fill="none"
        strokeLinecap="round"
        initial={{ pathLength: 0, opacity: 0.4 }}
        whileInView={{ pathLength: 1, opacity: 1 }}
        viewport={{ once: true, amount: 0.7 }}
        transition={{ duration: 1.4, ease: [0.22, 1, 0.36, 1] }}
      />
      <motion.circle
        cx={560}
        cy={12}
        r={4}
        fill="currentColor"
        initial={{ scale: 0, opacity: 0 }}
        whileInView={{ scale: 1, opacity: 1 }}
        viewport={{ once: true, amount: 0.7 }}
        transition={{ duration: 0.4, delay: 1.2 }}
      />
      <motion.rect
        x={540}
        y={4}
        width={16}
        height={16}
        rx={3}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.2"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 0.6 }}
        viewport={{ once: true, amount: 0.7 }}
        transition={{ duration: 0.4, delay: 1.3 }}
      />
    </svg>
  );
}

function HeroSection() {
  return (
    <section className="flex flex-col gap-8">
      <div className="grid items-end gap-10 lg:grid-cols-[minmax(0,1fr)_auto]">
        <div>
          <Eyebrow>Platform · Continuous Integration</Eyebrow>
          <h1 className="font-heading text-hero text-cc-heading mt-5 font-bold tracking-tight">
            GraphQL schema registry CI
            <br />
            that moves at the speed
            <br />
            of your pipeline.
          </h1>
          <p className="lead text-cc-ink-dim mt-6 max-w-2xl">
            Scroll the page and watch one schema change travel the rail.
            Validate, upload, publish, deploy. Breaking-change classification
            gates every promotion across dev, QA, staging, and prod.
          </p>
          <HeroRailUnderline />
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Centerpiece: Schema Travel Lane                                     */
/* ------------------------------------------------------------------ */

interface LaneStation {
  readonly key: string;
  readonly num: string;
  readonly title: string;
  readonly sub: string;
  readonly meta: string;
  /** progress threshold at which the station "lights up" */
  readonly threshold: number;
}

const LANE_STATIONS: readonly LaneStation[] = [
  {
    key: "validate",
    num: "01",
    title: "Validate",
    sub: "classify changes",
    meta: "schema + clients",
    threshold: 0.15,
  },
  {
    key: "upload",
    num: "02",
    title: "Upload",
    sub: "stage schema",
    meta: "version + tag",
    threshold: 0.4,
  },
  {
    key: "publish",
    num: "03",
    title: "Publish",
    sub: "approval gate",
    meta: "promote to env",
    threshold: 0.65,
  },
  {
    key: "deploy",
    num: "04",
    title: "Deploy",
    sub: "release the API",
    meta: "dev · QA · staging · prod",
    threshold: 0.9,
  },
];

interface StationCardProps {
  readonly station: LaneStation;
  readonly lit: boolean;
  readonly index: number;
}

function StationCard({ station, lit, index }: StationCardProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const checks = ["lex", "types", "ops", "clients"];

  return (
    <div
      ref={ref}
      className={`border-cc-card-border relative flex-1 rounded-lg border px-4 py-4 transition-colors duration-500 ${
        lit ? "ring-cc-accent/40 ring-1 ring-inset" : ""
      }`}
      style={{
        backgroundColor: lit ? "rgba(94, 234, 212, 0.06)" : TRACK_RAISED,
      }}
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] uppercase">
          {station.num}
        </span>
        <span
          className={`font-mono text-[0.6rem] font-semibold tracking-[0.14em] uppercase transition-colors ${
            lit ? "text-cc-accent" : "text-cc-nav-label"
          }`}
        >
          {lit ? "active" : "pending"}
        </span>
      </div>
      <div
        className={`font-heading mt-3 text-[1.05rem] font-semibold transition-colors ${
          lit ? "text-cc-heading" : "text-cc-ink-dim"
        }`}
      >
        {station.title}
      </div>
      <div className="text-cc-prose mt-1 font-mono text-[0.72rem]">
        {station.sub}
      </div>
      <div className="text-cc-ink-dim mt-2 font-mono text-[0.62rem]">
        {station.meta}
      </div>
      <div className="mt-3 flex gap-1.5">
        {checks.map((c, i) => (
          <motion.span
            key={c}
            className="bg-cc-card-border h-1.5 flex-1 rounded-full"
            initial={false}
            animate={{
              backgroundColor:
                inView && lit
                  ? "var(--color-cc-success)"
                  : "var(--color-cc-card-border)",
              scale: inView && lit ? 1 : 0.96,
            }}
            transition={{
              duration: 0.4,
              delay: inView && lit ? i * 0.12 + index * 0.05 : 0,
              ease: [0.22, 1, 0.36, 1],
            }}
          />
        ))}
      </div>
    </div>
  );
}

interface ClassifierBadgeProps {
  readonly verdict: StageState;
  readonly forced: boolean;
}

function ClassifierBadge({ verdict, forced }: ClassifierBadgeProps) {
  return (
    <div
      className="border-cc-card-border w-full max-w-xs rounded-lg border p-4"
      style={{ backgroundColor: TRACK_DEEP }}
    >
      <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
        Classifier
      </div>
      <div className="mt-2 flex items-baseline justify-between gap-3">
        <span className="text-cc-prose font-mono text-[0.7rem]">
          Order.total: Float!
        </span>
      </div>
      <div className="text-cc-ink-dim mt-1 font-mono text-[0.62rem]">
        field removed
      </div>
      <div className="border-cc-card-border mt-3 border-t pt-3">
        <AnimatePresence mode="wait" initial={false}>
          <motion.div
            key={verdict + (forced ? "-forced" : "")}
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -6 }}
            transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
            className="flex items-center gap-2"
          >
            <VerdictChip status={verdict} />
            {forced && (
              <span className="bg-cc-warning/10 text-cc-warning ring-cc-warning/30 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] uppercase ring-1 ring-inset">
                force --reason
              </span>
            )}
          </motion.div>
        </AnimatePresence>
        <div className="text-cc-ink-dim mt-2 font-mono text-[0.6rem]">
          {verdict === "safe" && "no published clients affected"}
          {verdict === "dangerous" && "@deprecated, reason recorded"}
          {verdict === "breaking" &&
            !forced &&
            "3 published clients affected · gate closed"}
          {verdict === "breaking" &&
            forced &&
            "override accepted, audit row written"}
        </div>
      </div>
    </div>
  );
}

interface ScrollGatedLane {
  readonly reduced: boolean | null;
}

function SchemaTravelLane({ reduced }: ScrollGatedLane) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.35 });

  // One-shot, time-driven progress (0 -> 1) once the lane enters view.
  // Pauses briefly at the publish gate, then resumes.
  const progressMV = useMotionValue(reduced ? 1 : 0);
  const [progress, setProgress] = useState(reduced ? 1 : 0);

  useEffect(() => {
    if (reduced) {
      progressMV.set(1);
      return;
    }
    if (!inView) return;
    const unsub = progressMV.on("change", (v) => setProgress(v));
    const controls = animate(progressMV, [0, 0.6, 0.6, 1], {
      duration: 5,
      times: [0, 0.55, 0.7, 1],
      ease: [0.22, 1, 0.36, 1],
    });
    return () => {
      controls.stop();
      unsub();
    };
  }, [inView, reduced, progressMV]);

  // Derive token x and pulses from the time-driven progress.
  const effectiveProgress = reduced ? 1 : progress;
  const tokenLeft = `${4 + effectiveProgress * 92}%`;
  const tokenScale =
    effectiveProgress < 0.5
      ? 0.95 + effectiveProgress * 0.2
      : 1.05 - (effectiveProgress - 0.5) * 0.1;
  const gateRingVisible =
    !reduced && effectiveProgress >= 0.58 && effectiveProgress <= 0.8;

  // Derive verdict and forced state from progress.
  let verdict: StageState;
  let forced: boolean;
  if (reduced) {
    verdict = "breaking";
    forced = true;
  } else if (effectiveProgress < 0.45) {
    verdict = "safe";
    forced = false;
  } else if (effectiveProgress < 0.6) {
    verdict = "dangerous";
    forced = false;
  } else if (effectiveProgress < 0.74) {
    verdict = "breaking";
    forced = false;
  } else {
    verdict = "breaking";
    forced = true;
  }

  return (
    <section className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <Eyebrow>Centerpiece · Schema Travel Lane</Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
          One schema change, four stations, one rail.
        </h2>
        <p className="text-body text-cc-prose max-w-2xl leading-relaxed">
          Scroll the page. A schema token leaves Validate, gets tagged at
          Upload, hits the Publish gate where the classifier reads the diff, and
          lands at Deploy with the environment it earned. The rail does not lie
          about what it has registered.
        </p>
      </div>
      <div
        ref={ref}
        className="border-cc-card-border relative overflow-hidden rounded-2xl border p-6 sm:p-8"
        style={{
          backgroundColor: TRACK_BG,
          backgroundImage: `linear-gradient(${TRACK_LINE} 1px, transparent 1px), linear-gradient(90deg, ${TRACK_LINE} 1px, transparent 1px)`,
          backgroundSize: "26px 26px",
          minHeight: "60vh",
        }}
      >
        <div className="flex items-center justify-between">
          <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
            nitro pipeline · orders-api · release
          </div>
          <div className="text-cc-ink-dim hidden font-mono text-[0.62rem] sm:block">
            run #482 · token: schema@build.42
          </div>
        </div>

        {/* Rail with token */}
        <div className="relative mt-8 h-12">
          <div
            className="absolute top-1/2 right-0 left-0 h-[2px] -translate-y-1/2 rounded-full"
            style={{ backgroundColor: "rgba(245,241,234,0.14)" }}
          />
          <motion.div
            className="bg-cc-accent absolute top-1/2 left-0 h-[2px] origin-left -translate-y-1/2 rounded-full"
            style={{
              scaleX: reduced ? 1 : progressMV,
              width: "100%",
            }}
          />
          {/* Station ticks */}
          {LANE_STATIONS.map((s) => (
            <div
              key={s.key}
              className="absolute top-1/2 -translate-x-1/2 -translate-y-1/2"
              style={{ left: `${s.threshold * 100}%` }}
              aria-hidden
            >
              <span
                className={`block h-3 w-3 rounded-full ring-2 transition-all duration-500 ${
                  effectiveProgress >= s.threshold
                    ? "bg-cc-accent ring-cc-accent/30"
                    : "ring-cc-card-border bg-cc-bg"
                }`}
              />
            </div>
          ))}
          {/* Token */}
          <div
            className="absolute top-1/2 -translate-x-1/2 -translate-y-1/2"
            style={{ left: reduced ? "96%" : tokenLeft }}
          >
            <div
              style={{ transform: `scale(${reduced ? 1 : tokenScale})` }}
              className="border-cc-accent relative flex items-center gap-1.5 rounded-full border bg-[#0c1322] px-3 py-1.5 shadow-[0_0_24px_rgba(94,234,212,0.35)]"
            >
              <span className="bg-cc-accent h-1.5 w-1.5 rounded-full" />
              <span className="text-cc-accent font-mono text-[0.62rem] tracking-[0.12em]">
                schema@build.42
              </span>
              {/* Gate-blocked ring pulse */}
              {gateRingVisible && (
                <motion.span
                  aria-hidden
                  className="ring-cc-danger absolute inset-[-4px] rounded-full ring-2"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: [0, 1, 1, 0] }}
                  transition={{ duration: 1.1, ease: "easeInOut" }}
                />
              )}
            </div>
          </div>
        </div>

        {/* Stations */}
        <div className="mt-10 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {LANE_STATIONS.map((station, i) => (
            <StationCard
              key={station.key}
              station={station}
              index={i}
              lit={effectiveProgress >= station.threshold}
            />
          ))}
        </div>

        {/* Classifier card */}
        <div className="mt-8 flex flex-col items-stretch gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="max-w-md">
            <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
              gate status
            </div>
            <div className="text-cc-prose mt-2 font-mono text-[0.74rem] leading-relaxed">
              {!forced && verdict !== "breaking" && (
                <>
                  classifier reports <VerdictChip status={verdict} /> · gate
                  open, token continues
                </>
              )}
              {!forced && verdict === "breaking" && (
                <>
                  classifier reports <VerdictChip status={verdict} /> · gate
                  closed, token stalls on the rail
                </>
              )}
              {forced && (
                <>
                  override flag accepted on <VerdictChip status={verdict} /> ·
                  token continues with audit row
                </>
              )}
            </div>
          </div>
          <ClassifierBadge verdict={verdict} forced={forced} />
        </div>

        <div className="text-cc-ink-dim mt-6 font-mono text-[0.62rem]">
          {reduced
            ? "reduced motion · static final frame"
            : "scroll to advance · scroll back to rewind"}
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Stage anatomy: 4-column grid                                        */
/* ------------------------------------------------------------------ */

interface AnatomyCardProps {
  readonly num: string;
  readonly title: string;
  readonly lead: string;
  readonly meta: string;
}

function MiniSpark() {
  const ref = useRef<SVGSVGElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.6 });
  // 12 random-but-stable ticks
  const ticks = [42, 38, 44, 35, 40, 32, 36, 30, 33, 28, 26, 24];
  const points = ticks
    .map((t, i) => `${(i / (ticks.length - 1)) * 100},${t}`)
    .join(" ");
  return (
    <svg
      ref={ref}
      viewBox="0 0 100 50"
      width="100%"
      height={36}
      className="text-cc-accent"
      aria-hidden
      preserveAspectRatio="none"
    >
      <motion.polyline
        points={points}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
        strokeLinejoin="round"
        initial={{ pathLength: 0, opacity: 0.4 }}
        animate={
          inView
            ? { pathLength: 1, opacity: 1 }
            : { pathLength: 0, opacity: 0.4 }
        }
        transition={{ duration: 1.0, ease: [0.22, 1, 0.36, 1] }}
      />
    </svg>
  );
}

function AnatomyCard({ num, title, lead, meta }: AnatomyCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 18 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="border-cc-card-border flex h-full flex-col gap-3 rounded-xl border p-5"
      style={{ backgroundColor: TRACK_RAISED }}
    >
      <div className="flex items-center justify-between">
        <span className="border-cc-card-border text-cc-accent flex h-8 w-8 items-center justify-center rounded-md border font-mono text-[0.74rem] font-semibold">
          {num}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.14em] uppercase">
          {meta}
        </span>
      </div>
      <div className="text-cc-heading font-heading text-[1rem] font-semibold">
        {title}
      </div>
      <p className="text-cc-ink-dim text-[0.86rem] leading-relaxed">{lead}</p>
      <div className="mt-auto">
        <div className="text-cc-nav-label mb-1 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
          command latency · last 12
        </div>
        <MiniSpark />
      </div>
    </motion.div>
  );
}

function StageAnatomySection() {
  return (
    <section className="flex flex-col gap-8">
      <div className="grid items-end gap-6 lg:grid-cols-[minmax(0,1fr)_auto]">
        <div>
          <Eyebrow>Anatomy</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading mt-4 font-semibold tracking-tight">
            Four stations, one CLI.
          </h2>
          <p className="text-body text-cc-prose mt-4 max-w-2xl leading-relaxed">
            Every station is one Nitro CLI command. The same exit codes drive
            your runner, the same registry stores the result, and the same
            classification decides whether the token clears the gate.
          </p>
        </div>
        <div className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.14em] uppercase">
          per station
        </div>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <AnatomyCard
          num="01"
          title="Validate"
          meta="schema + clients"
          lead="Classify each change safe, dangerous, or breaking against the operations published clients have registered. Build fails loud, not silent."
        />
        <AnatomyCard
          num="02"
          title="Upload"
          meta="version + tag"
          lead="Stage the schema, tagged with the commit SHA, pinned to a target environment. Reproducible, not yet live."
        />
        <AnatomyCard
          num="03"
          title="Publish"
          meta="approval gate"
          lead="Promote the staged version. Reviewer gates can require approval on dangerous or breaking changes before the contract flips."
        />
        <AnatomyCard
          num="04"
          title="Deploy"
          meta="dev · QA · prod"
          lead="Each environment keeps its own active version and history. Deploy runs on the runner that validated and published."
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Breaking-change classifier section                                  */
/* ------------------------------------------------------------------ */

interface ClassifierExample {
  readonly diff: ReactNode;
  readonly verdict: StageState;
  readonly note: string;
}

const CLASSIFIER_CYCLE: readonly ClassifierExample[] = [
  {
    verdict: "safe",
    note: "no published clients affected",
    diff: (
      <>
        <div className="text-cc-success">+ totalAmount: Money!</div>
        <div className="text-cc-ink-dim"> type Order {`{`}</div>
        <div className="text-cc-ink-dim"> id: ID!</div>
        <div className="text-cc-ink-dim"> {`}`}</div>
      </>
    ),
  },
  {
    verdict: "dangerous",
    note: "deprecation reason recorded",
    diff: (
      <>
        <div className="text-cc-warning">
          ~ placedAt: DateTime! @deprecated(reason: &quot;use placedOn&quot;)
        </div>
        <div className="text-cc-ink-dim"> type Order {`{`}</div>
        <div className="text-cc-ink-dim"> id: ID!</div>
        <div className="text-cc-ink-dim"> {`}`}</div>
      </>
    ),
  },
  {
    verdict: "breaking",
    note: "3 published clients affected",
    diff: (
      <>
        <div className="text-cc-danger">- total: Float!</div>
        <div className="text-cc-ink-dim"> type Order {`{`}</div>
        <div className="text-cc-ink-dim"> id: ID!</div>
        <div className="text-cc-ink-dim"> {`}`}</div>
      </>
    ),
  },
];

function TickUpNumber({
  to,
  duration = 1.2,
  suffix = "",
}: {
  readonly to: number;
  readonly duration?: number;
  readonly suffix?: string;
}) {
  const ref = useRef<HTMLSpanElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.6 });
  const reduced = useReducedMotion();
  const mv = useMotionValue(0);
  const [display, setDisplay] = useState(0);

  useEffect(() => {
    if (!inView) return;
    if (reduced) {
      mv.set(to);
      const unsub = mv.on("change", (v) => setDisplay(Math.round(v)));
      return () => unsub();
    }
    const controls = animate(mv, to, {
      duration,
      ease: [0.22, 1, 0.36, 1],
    });
    const unsub = mv.on("change", (v) => setDisplay(Math.round(v)));
    return () => {
      controls.stop();
      unsub();
    };
  }, [inView, mv, to, duration, reduced]);

  return (
    <span ref={ref}>
      {display.toLocaleString()}
      {suffix}
    </span>
  );
}

function ClassifierSection() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { amount: 0.4 });
  const reduced = useReducedMotion();
  const [i, setI] = useState(0);

  useEffect(() => {
    if (!inView || reduced) return;
    const id = setInterval(() => {
      setI((prev) => (prev + 1) % CLASSIFIER_CYCLE.length);
    }, 4000);
    return () => clearInterval(id);
  }, [inView, reduced]);

  const example = CLASSIFIER_CYCLE[i];

  return (
    <section
      ref={ref}
      className="border-cc-card-border rounded-2xl border px-6 py-9 sm:px-10 sm:py-11"
      style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
    >
      <div className="grid items-start gap-10 lg:grid-cols-[1.1fr_0.9fr]">
        <div>
          <Eyebrow>Breaking-change classifier</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading mt-4 font-semibold tracking-tight">
            Every line, classified before it ships.
          </h2>
          <p className="text-body text-cc-prose mt-4 leading-relaxed">
            The classifier diffs the staged schema against the active version,
            then checks each change against the operations your published
            clients have registered. The verdict drives the gate.
          </p>
          <div className="mt-7 flex flex-wrap items-end gap-x-10 gap-y-4">
            <div>
              <div className="text-cc-heading font-heading text-[2.4rem] leading-none font-bold">
                <TickUpNumber to={47} />
              </div>
              <div className="text-cc-nav-label mt-2 font-mono text-[0.66rem] tracking-[0.14em] uppercase">
                breaking changes caught
                <br />
                in a sample week (illustrative)
              </div>
            </div>
            <div className="text-cc-ink-dim max-w-xs text-[0.86rem] leading-relaxed">
              Figure is illustrative. Classification runs against the operations
              published clients have registered. Untracked consumers will not
              appear in the impact list.
            </div>
          </div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div
            className="border-cc-card-border rounded-xl border p-4"
            style={{ backgroundColor: TRACK_DEEP }}
          >
            <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
              schema diff
            </div>
            <div
              className="mt-3 rounded-md p-3 font-mono text-[0.74rem] leading-relaxed"
              style={{ backgroundColor: SUNK }}
            >
              <AnimatePresence mode="wait" initial={false}>
                <motion.div
                  key={example.verdict}
                  initial={{ opacity: 0, y: 6 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -6 }}
                  transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
                >
                  {example.diff}
                </motion.div>
              </AnimatePresence>
            </div>
          </div>
          <div
            className="border-cc-card-border rounded-xl border p-4"
            style={{ backgroundColor: TRACK_DEEP }}
          >
            <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
              verdict
            </div>
            <div className="mt-3 flex flex-col items-start gap-3">
              <AnimatePresence mode="wait" initial={false}>
                <motion.div
                  key={example.verdict + "-chip"}
                  initial={{ opacity: 0, scale: 0.92 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.96 }}
                  transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
                >
                  <VerdictChip status={example.verdict} />
                </motion.div>
              </AnimatePresence>
              <AnimatePresence mode="wait" initial={false}>
                <motion.div
                  key={example.verdict + "-note"}
                  initial={{ opacity: 0, y: 4 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -4 }}
                  transition={{ duration: 0.35 }}
                  className="text-cc-prose font-mono text-[0.72rem]"
                >
                  {example.note}
                </motion.div>
              </AnimatePresence>
              <div className="text-cc-ink-dim mt-2 font-mono text-[0.62rem]">
                gate {example.verdict === "breaking" ? "closed" : "open"} · exit
                code {example.verdict === "breaking" ? "1" : "0"}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Environment promotion ladder                                        */
/* ------------------------------------------------------------------ */

const ENV_RUNGS = [
  {
    key: "dev",
    label: "dev",
    note: "auto-publish from main",
  },
  {
    key: "qa",
    label: "qa",
    note: "promoted after dev",
  },
  {
    key: "staging",
    label: "staging",
    note: "approver review",
  },
  {
    key: "prod",
    label: "prod",
    note: "approval gate, tagged release",
  },
] as const;

function PromotionLadder() {
  const [active, setActive] = useState<string>("qa");
  const layoutId = useId();

  return (
    <section className="grid items-start gap-10 lg:grid-cols-[1fr_1fr]">
      <div>
        <Eyebrow>Environment promotion</Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading mt-4 font-semibold tracking-tight">
          Same tag, every rung.
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">
          Promotion is tag-based. The same uploaded version moves from dev
          through QA and staging to prod without a rebuild. Click a rung to move
          the marker, the way a release manager moves the schema.
        </p>
        <ul className="mt-6 space-y-3">
          {[
            "Per-environment workflows: dev, QA, staging, production.",
            "Promotion is a tag flip, not a re-upload.",
            "Rollback is a re-publish of a prior tagged version.",
          ].map((b) => (
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
      <div
        className="border-cc-card-border relative rounded-2xl border p-6"
        style={{ backgroundColor: TRACK_RAISED }}
      >
        <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
          orders-api · schema@build.42
        </div>
        <ol className="relative mt-5 flex flex-col">
          {ENV_RUNGS.map((rung, idx) => {
            const selected = active === rung.key;
            return (
              <li key={rung.key} className="relative">
                <button
                  type="button"
                  onClick={() => setActive(rung.key)}
                  className={`group border-cc-card-border relative flex w-full items-center gap-4 rounded-lg border px-4 py-4 text-left transition-colors ${
                    selected
                      ? "bg-cc-accent/[0.06]"
                      : "hover:border-cc-card-border-hover bg-transparent"
                  }`}
                >
                  <span className="relative flex h-7 w-7 shrink-0 items-center justify-center">
                    <span className="bg-cc-card-border absolute h-7 w-7 rounded-full" />
                    {selected && (
                      <motion.span
                        layoutId={`promo-dot-${layoutId}`}
                        className="bg-cc-accent absolute h-7 w-7 rounded-full shadow-[0_0_18px_rgba(94,234,212,0.4)]"
                        transition={{
                          type: "spring",
                          stiffness: 380,
                          damping: 30,
                        }}
                      />
                    )}
                    <span
                      className={`relative font-mono text-[0.66rem] font-semibold ${
                        selected ? "text-cc-bg" : "text-cc-ink-dim"
                      }`}
                    >
                      {String(idx + 1).padStart(2, "0")}
                    </span>
                  </span>
                  <div className="flex-1">
                    <div className="text-cc-heading font-mono text-[0.82rem] tracking-[0.06em] uppercase">
                      {rung.label}
                    </div>
                    <div className="text-cc-ink-dim font-mono text-[0.66rem]">
                      {rung.note}
                    </div>
                  </div>
                  <span
                    className={`font-mono text-[0.62rem] tracking-[0.14em] uppercase ${
                      selected ? "text-cc-accent" : "text-cc-nav-label"
                    }`}
                  >
                    {selected ? "active" : "tag flip"}
                  </span>
                </button>
                {idx < ENV_RUNGS.length - 1 && (
                  <span
                    className="bg-cc-card-border absolute top-[3.5rem] left-[1.65rem] h-3 w-px"
                    aria-hidden
                  />
                )}
              </li>
            );
          })}
        </ol>
        <div className="text-cc-ink-dim mt-4 font-mono text-[0.62rem]">
          click a rung to move the marker · no rebuild required
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Runner integrations strip                                           */
/* ------------------------------------------------------------------ */

function GithubGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width={22}
      height={22}
      className="text-cc-heading"
      aria-hidden
    >
      <path
        fill="currentColor"
        d="M12 .5C5.65.5.5 5.65.5 12c0 5.08 3.29 9.39 7.86 10.92.58.11.79-.25.79-.55 0-.27-.01-.99-.01-1.95-3.2.69-3.88-1.54-3.88-1.54-.52-1.34-1.28-1.69-1.28-1.69-1.05-.72.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.03 1.77 2.71 1.26 3.37.96.1-.75.4-1.26.73-1.55-2.55-.29-5.24-1.27-5.24-5.66 0-1.25.45-2.27 1.18-3.07-.12-.29-.51-1.46.11-3.04 0 0 .97-.31 3.18 1.17a11.04 11.04 0 0 1 5.79 0c2.21-1.48 3.18-1.17 3.18-1.17.62 1.58.23 2.75.11 3.04.74.8 1.18 1.82 1.18 3.07 0 4.4-2.69 5.36-5.25 5.65.41.36.78 1.06.78 2.13 0 1.54-.01 2.78-.01 3.16 0 .31.21.67.8.55A11.51 11.51 0 0 0 23.5 12C23.5 5.65 18.35.5 12 .5Z"
      />
    </svg>
  );
}

function AzureGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden>
      <path
        fill="#16b9e4"
        d="M9.5 3 3 18.5l4 .5L13.5 8 9.5 3Zm4.5 4 7 14H10l-2-3 4-1L14 7Z"
      />
    </svg>
  );
}

function GitlabGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden>
      <path d="M12 21 4 10l2-7 2 6h8l2-6 2 7Z" fill="#f0786a" opacity="0.9" />
    </svg>
  );
}

function ShellGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width={22}
      height={22}
      className="text-cc-accent"
      aria-hidden
    >
      <path
        d="M4 5h16v14H4z"
        stroke="currentColor"
        strokeWidth="1.4"
        fill="none"
      />
      <path
        d="M7 10l3 2-3 2M11 14h6"
        stroke="currentColor"
        strokeWidth="1.5"
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

const RUNNER_LIST = [
  { key: "gh", title: "GitHub Actions", glyph: <GithubGlyph /> },
  { key: "azdo", title: "Azure DevOps", glyph: <AzureGlyph /> },
  { key: "gitlab", title: "GitLab", glyph: <GitlabGlyph /> },
  { key: "shell", title: "Any shell runner", glyph: <ShellGlyph /> },
];

const YAML_LINES: readonly string[] = [
  "- name: Publish schema",
  "  run: |",
  "    dotnet nitro schema publish \\",
  "      --stage staging \\",
  "      --tag ${{ github.sha }}",
  "    dotnet nitro schema publish --stage prod",
];

function RunnerIntegrations() {
  return (
    <section>
      <div className="grid items-end gap-6 lg:grid-cols-[minmax(0,1fr)_auto]">
        <div>
          <Eyebrow>Pipe ready</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading mt-4 font-semibold tracking-tight">
            Drops into the runner you already use.
          </h2>
          <p className="text-body text-cc-prose mt-4 max-w-2xl leading-relaxed">
            The Nitro CLI is the only thing the pipeline needs. Same commands,
            same exit codes, same registry on the other side.
          </p>
        </div>
        <div className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.14em] uppercase">
          one CLI, every runner
        </div>
      </div>
      <motion.ul
        className="mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-4"
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.3 }}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.06 } },
        }}
      >
        {RUNNER_LIST.map((r) => (
          <motion.li
            key={r.key}
            variants={{
              hidden: { opacity: 0, y: 14 },
              show: { opacity: 1, y: 0 },
            }}
            transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1] }}
            className="border-cc-card-border flex items-center gap-3 rounded-xl border px-4 py-4"
            style={{ backgroundColor: TRACK_RAISED }}
          >
            <span
              className="border-cc-card-border flex h-10 w-10 items-center justify-center rounded-md border"
              style={{ backgroundColor: TRACK_DEEP }}
            >
              {r.glyph}
            </span>
            <div>
              <div className="text-cc-heading font-heading text-[0.95rem] font-semibold">
                {r.title}
              </div>
              <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.14em] uppercase">
                workflow step
              </div>
            </div>
          </motion.li>
        ))}
      </motion.ul>
      <motion.pre
        className="text-cc-prose border-cc-card-border mt-6 overflow-x-auto rounded-xl border px-4 py-4 font-mono text-[0.74rem] leading-relaxed"
        style={{ backgroundColor: SUNK }}
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.4 }}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.08, delayChildren: 0.1 } },
        }}
      >
        <code>
          {YAML_LINES.map((line, idx) => (
            <motion.div
              key={idx}
              variants={{
                hidden: { opacity: 0, y: 6 },
                show: { opacity: 1, y: 0 },
              }}
              transition={{ duration: 0.4 }}
            >
              {line}
            </motion.div>
          ))}
        </code>
      </motion.pre>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Proof tiles                                                         */
/* ------------------------------------------------------------------ */

interface ProofTileProps {
  readonly value: number;
  readonly suffix?: string;
  readonly label: string;
  readonly sub: string;
}

function ProofTile({ value, suffix = "", label, sub }: ProofTileProps) {
  return (
    <div
      className="border-cc-card-border rounded-xl border p-6"
      style={{ backgroundColor: TRACK_RAISED }}
    >
      <div className="text-cc-heading font-heading text-[2.6rem] leading-none font-bold tracking-tight">
        <TickUpNumber to={value} suffix={suffix} />
      </div>
      <div className="text-cc-prose mt-3 font-mono text-[0.78rem]">{label}</div>
      <div className="text-cc-ink-dim mt-1 font-mono text-[0.66rem]">{sub}</div>
    </div>
  );
}

function ProofRow() {
  return (
    <section>
      <Eyebrow>Pipeline pulse · sample</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 font-semibold tracking-tight">
        What the rail registers in a typical sprint.
      </h2>
      <p className="text-cc-ink-dim mt-3 max-w-2xl text-[0.86rem] leading-relaxed">
        Illustrative figures, drawn to show the shape of the signal the registry
        surfaces once your pipeline is wired up.
      </p>
      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        <ProofTile
          value={1284}
          label="schemas published"
          sub="across services in a sample sprint"
        />
        <ProofTile
          value={47}
          label="breaking changes blocked"
          sub="gate refused promotion"
        />
        <ProofTile
          value={612}
          label="deploys gated"
          sub="approval cleared, then released"
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* FAQ-lite                                                            */
/* ------------------------------------------------------------------ */

interface FaqRow {
  readonly q: string;
  readonly a: ReactNode;
}

const FAQS: readonly FaqRow[] = [
  {
    q: "Does the registry handle monorepos?",
    a: (
      <>
        Yes. One registry can host many schemas, each with its own tag stream.
        The Nitro CLI takes a service name on every command, so a monorepo can
        publish many schemas in one run without crossing wires.
      </>
    ),
  },
  {
    q: "How do multiple teams share a schema?",
    a: (
      <>
        Each team owns its schema in the registry. When teams compose, the
        composition runs at planning time and the result is registered like any
        other schema. The gateway is always self-run.
      </>
    ),
  },
  {
    q: "What about offline or self-hosted runners?",
    a: (
      <>
        The Nitro CLI talks to the registry over HTTPS, so any runner with
        outbound access works the same. Air-gapped setups can point the CLI at a
        self-hosted registry instance.
      </>
    ),
  },
];

interface FaqItemProps {
  readonly row: FaqRow;
  readonly open: boolean;
  readonly onToggle: () => void;
}

function FaqItem({ row, open, onToggle }: FaqItemProps) {
  return (
    <li
      className="border-cc-card-border overflow-hidden rounded-xl border"
      style={{ backgroundColor: TRACK_RAISED }}
    >
      <button
        type="button"
        onClick={onToggle}
        className="flex w-full items-center justify-between gap-4 px-5 py-4 text-left"
      >
        <span className="text-cc-heading font-heading text-[0.98rem] font-semibold">
          {row.q}
        </span>
        <motion.span
          className="text-cc-accent font-mono text-[0.9rem]"
          animate={{ rotate: open ? 45 : 0 }}
          transition={{ duration: 0.3, ease: [0.22, 1, 0.36, 1] }}
          aria-hidden
        >
          +
        </motion.span>
      </button>
      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            key="content"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
          >
            <div className="text-cc-ink-dim border-cc-card-border border-t px-5 py-4 text-[0.9rem] leading-relaxed">
              {row.a}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </li>
  );
}

function FaqSection() {
  const [open, setOpen] = useState<number | null>(0);
  return (
    <section>
      <Eyebrow>Common questions</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 font-semibold tracking-tight">
        Three things teams ask first.
      </h2>
      <ul className="mt-7 flex flex-col gap-3">
        {FAQS.map((row, idx) => (
          <FaqItem
            key={row.q}
            row={row}
            open={open === idx}
            onToggle={() => setOpen(open === idx ? null : idx)}
          />
        ))}
      </ul>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Closing CTA with a tiny token-on-rail motif                         */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  const reduced = useReducedMotion();
  return (
    <section className="border-cc-card-border relative overflow-hidden rounded-2xl border px-6 py-14 text-center sm:px-12">
      <div
        aria-hidden
        className="absolute inset-0 -z-10"
        style={{
          backgroundColor: TRACK_BG,
          backgroundImage:
            "radial-gradient(70% 100% at 50% 0%, rgba(94,234,212,0.14), transparent 65%)",
        }}
      />
      <h2 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
        Wire Nitro CLI into your pipeline.
      </h2>
      <p className="text-body text-cc-prose mx-auto mt-5 max-w-xl leading-relaxed">
        Validate, upload, publish, deploy. One CLI on the runner you already
        use, one registry on the other side.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/docs/nitro">Get Started</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
      <div
        className="relative mx-auto mt-12 h-10 max-w-md overflow-hidden"
        aria-hidden
      >
        <div
          className="absolute top-1/2 right-0 left-0 h-[2px] -translate-y-1/2 rounded-full"
          style={{ backgroundColor: "rgba(245,241,234,0.14)" }}
        />
        {reduced ? (
          <span className="bg-cc-accent absolute top-1/2 left-[80%] h-2 w-2 -translate-y-1/2 rounded-full" />
        ) : (
          <motion.span
            className="border-cc-accent absolute top-1/2 flex h-5 -translate-y-1/2 items-center gap-1.5 rounded-full border bg-[#0c1322] px-2 shadow-[0_0_16px_rgba(94,234,212,0.35)]"
            initial={{ x: "-10%" }}
            animate={{ x: "110%" }}
            transition={{
              duration: 5,
              ease: "linear",
              repeat: Infinity,
            }}
          >
            <span className="bg-cc-accent h-1.5 w-1.5 rounded-full" />
            <span className="text-cc-accent font-mono text-[0.58rem] tracking-[0.12em]">
              schema@build.42
            </span>
          </motion.span>
        )}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

function PageInner() {
  const reduced = useReducedMotion();
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <SchemaTravelLane reduced={reduced} />
      <StageAnatomySection />
      <ClassifierSection />
      <PromotionLadder />
      <RunnerIntegrations />
      <ProofRow />
      <FaqSection />
      <ClosingCta />
    </div>
  );
}

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <PageInner />
    </MotionConfig>
  );
}
