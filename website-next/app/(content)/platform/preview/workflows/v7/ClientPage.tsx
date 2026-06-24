"use client";

import { useEffect, useRef } from "react";
import type { MotionValue } from "motion/react";
import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/*  V7 "Live Wire": one coral message travels the Mocha topology as the
    reader scrolls. Coral is the running color, cc-accent (teal) is the
    committed/done color, so motion encodes meaning rather than decoration. */

const CORAL = "#f0786a";
const TEAL = "var(--color-cc-accent)";

/* ============================== Section label ============================ */

interface SectionLabelProps {
  readonly children: React.ReactNode;
}

function SectionLabel({ children }: SectionLabelProps) {
  return (
    <p
      className="mb-3 font-mono text-[11px] tracking-[0.2em] uppercase"
      style={{ color: CORAL }}
    >
      {children}
    </p>
  );
}

/* =============================== Hero badge dot ========================= */

function HeroBadgeDot() {
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { amount: 0.4 });
  return (
    <motion.span
      ref={ref}
      className="size-1.5 rounded-full"
      style={{ backgroundColor: CORAL }}
      animate={inView ? { opacity: [0.5, 1, 0.5] } : { opacity: 0.8 }}
      transition={{
        duration: 2,
        repeat: inView ? Infinity : 0,
        ease: "easeInOut",
      }}
    />
  );
}

/* =============================== Hero preview ============================ */
/*  A compact static preview of the centerpiece that draws its first dashed
    wire whileInView, so the hero already hints at the motion language.      */

function HeroPreview() {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.5, ease: "easeOut" }}
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur-md"
    >
      <div className="border-cc-card-border bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <div className="flex items-center gap-2">
          <span className="size-2.5 rounded-full bg-[#f0786a]/60" />
          <span className="size-2.5 rounded-full bg-[#f59e0b]/60" />
          <span className="bg-cc-accent/60 size-2.5 rounded-full" />
        </div>
        <span className="text-cc-nav-label font-mono text-[11px]">
          mocha · topology · preview
        </span>
        <span className="text-cc-ink-dim flex items-center gap-1.5 font-mono text-[11px]">
          <span
            className="size-1.5 rounded-full"
            style={{ backgroundColor: CORAL }}
          />
          in flight
        </span>
      </div>

      <div className="p-5 sm:p-6">
        <svg viewBox="0 0 320 160" className="block h-auto w-full" aria-hidden>
          {/* horizontal spine */}
          <motion.path
            d="M 24 80 L 296 80"
            stroke="rgba(245,241,234,0.18)"
            strokeWidth="1.5"
            fill="none"
            strokeDasharray="5 4"
            initial={{ pathLength: 0 }}
            whileInView={{ pathLength: 1 }}
            viewport={{ once: true, amount: 0.5 }}
            transition={{ duration: 1.2, ease: "easeInOut" }}
          />
          {/* nodes */}
          {[
            ["Producer", 24],
            ["Mediator", 96],
            ["Bus", 168],
            ["Inbox", 240],
            ["Saga", 296],
          ].map(([label, x], i) => (
            <g key={String(label)} transform={`translate(${x}, 80)`}>
              <motion.circle
                r="6"
                fill="var(--color-cc-surface)"
                stroke={i === 2 ? CORAL : "var(--color-cc-card-border-hover)"}
                strokeWidth="1.5"
                initial={{ scale: 0.4, opacity: 0 }}
                whileInView={{ scale: 1, opacity: 1 }}
                viewport={{ once: true, amount: 0.5 }}
                transition={{ delay: 0.2 + i * 0.12, duration: 0.35 }}
              />
              <text
                y={26}
                textAnchor="middle"
                className="fill-cc-ink-dim font-mono"
                style={{ fontSize: 9 }}
              >
                {String(label).toLowerCase()}
              </text>
            </g>
          ))}
          {/* running token */}
          <motion.circle
            r="4"
            fill={CORAL}
            initial={{ cx: 24, cy: 80, opacity: 0 }}
            whileInView={{
              cx: [24, 96, 168, 240, 296],
              opacity: [0, 1, 1, 1, 1],
            }}
            viewport={{ once: true, amount: 0.5 }}
            transition={{ duration: 2.4, ease: "easeInOut", delay: 0.4 }}
            style={{ filter: `drop-shadow(0 0 6px ${CORAL})` }}
          />
        </svg>

        <div className="border-cc-card-border mt-4 flex flex-wrap items-center gap-2 border-t pt-3">
          <span className="text-cc-nav-label font-mono text-[10px]">
            guaranteed by
          </span>
          <span className="border-cc-card-border text-cc-ink rounded border px-2 py-0.5 font-mono text-[10px]">
            transactional outbox
          </span>
          <span className="border-cc-card-border text-cc-ink rounded border px-2 py-0.5 font-mono text-[10px]">
            idempotent inbox
          </span>
          <span
            className="ml-auto font-mono text-[10px]"
            style={{ color: CORAL }}
          >
            exactly-once processing
          </span>
        </div>
      </div>
    </motion.div>
  );
}

/* =========================== Centerpiece topology ========================= */
/*  Scroll-bound SVG: a coral token rides Producer -> Mediator -> Outbox ->
    Bus -> Inbox -> Saga -> Subscribers. Nodes activate via useTransform
    thresholds; a synchronized OTel-style span strip draws in below as each
    hop completes. A replay control (useAnimate) re-runs in place.            */

interface TopologyNode {
  readonly id: string;
  readonly label: string;
  readonly sub: string;
  readonly x: number;
  readonly y: number;
}

const NODES: readonly TopologyNode[] = [
  { id: "producer", label: "Producer", sub: "POST /reviews", x: 60, y: 110 },
  { id: "mediator", label: "Mediator", sub: "[Handler]", x: 200, y: 110 },
  { id: "outbox", label: "Outbox", sub: "tx commit", x: 340, y: 110 },
  { id: "bus", label: "Bus", sub: "rabbitmq", x: 480, y: 110 },
  { id: "inbox", label: "Inbox", sub: "dedupe", x: 620, y: 110 },
  { id: "saga", label: "Saga", sub: "ReviewSaga", x: 760, y: 110 },
];

const SUBSCRIBERS = [
  { id: "search", label: "SearchIndexer", x: 700, y: 220 },
  { id: "notify", label: "NotifyAuthor", x: 820, y: 220 },
  { id: "cache", label: "WarmCache", x: 940, y: 220 },
];

/* Stops along the path that align to centers of nodes 0..5 plus the fan-out. */
const STOPS = [0, 0.16, 0.32, 0.48, 0.64, 0.78, 1];

function pathPoint(t: number): { x: number; y: number } {
  // Approximate the polyline so the token sits visually on the path at progress t.
  // The polyline crosses 6 horizontal hops, then optionally drops to subscribers.
  const segments: Array<[number, number, number, number]> = [
    [NODES[0].x, NODES[0].y, NODES[1].x, NODES[1].y],
    [NODES[1].x, NODES[1].y, NODES[2].x, NODES[2].y],
    [NODES[2].x, NODES[2].y, NODES[3].x, NODES[3].y],
    [NODES[3].x, NODES[3].y, NODES[4].x, NODES[4].y],
    [NODES[4].x, NODES[4].y, NODES[5].x, NODES[5].y],
    [NODES[5].x, NODES[5].y, SUBSCRIBERS[1].x, SUBSCRIBERS[1].y],
  ];
  const clamped = Math.max(0, Math.min(1, t));
  const segCount = segments.length;
  const exact = clamped * segCount;
  const i = Math.min(segCount - 1, Math.floor(exact));
  const local = exact - i;
  const [x1, y1, x2, y2] = segments[i];
  return { x: x1 + (x2 - x1) * local, y: y1 + (y2 - y1) * local };
}

interface TopologyNodeViewProps {
  readonly node: TopologyNode;
  readonly progress: MotionValue<number>;
  readonly index: number;
}

function TopologyNodeView({ node, progress, index }: TopologyNodeViewProps) {
  // Glow rises while the token is in this node's span, then fades.
  const glowOpacity = useTransform(
    progress,
    [
      Math.max(0, STOPS[index] - 0.02),
      STOPS[index],
      STOPS[index + 1],
      Math.min(1, STOPS[index + 1] + 0.02),
    ],
    [0, 0.7, 0.7, 0],
  );
  // Done state turns the border teal once the token has passed.
  const doneOpacity = useTransform(
    progress,
    [STOPS[index + 1], Math.min(1, STOPS[index + 1] + 0.01)],
    [0, 1],
  );
  return (
    <g transform={`translate(${node.x}, ${node.y})`}>
      {/* base border: dim until done */}
      <rect
        x={-52}
        y={-22}
        width={104}
        height={44}
        rx={10}
        ry={10}
        fill="var(--color-cc-surface)"
        stroke="rgba(245,241,234,0.28)"
        strokeWidth={1.5}
      />
      {/* done border: teal, fades in when the token has passed */}
      <motion.rect
        x={-52}
        y={-22}
        width={104}
        height={44}
        rx={10}
        ry={10}
        fill="none"
        stroke={TEAL}
        strokeWidth={1.5}
        style={{ opacity: doneOpacity }}
      />
      {/* active glow ring */}
      <motion.circle
        cx={0}
        cy={0}
        r={28}
        fill="none"
        stroke={CORAL}
        strokeWidth={1.25}
        style={{ opacity: glowOpacity }}
      />
      <text
        y={-3}
        textAnchor="middle"
        className="fill-cc-heading font-mono"
        style={{ fontSize: 11 }}
      >
        {node.label}
      </text>
      <text
        y={13}
        textAnchor="middle"
        className="fill-cc-nav-label font-mono"
        style={{ fontSize: 9 }}
      >
        {node.sub}
      </text>
    </g>
  );
}

interface SubscriberViewProps {
  readonly node: { id: string; label: string; x: number; y: number };
  readonly progress: MotionValue<number>;
  readonly index: number;
}

function SubscriberView({ node, progress, index }: SubscriberViewProps) {
  const onOpacity = useTransform(
    progress,
    [0.82 + index * 0.03, 0.9 + index * 0.03, 1],
    [0, 1, 1],
  );
  return (
    <g transform={`translate(${node.x}, ${node.y})`}>
      <rect
        x={-58}
        y={-14}
        width={116}
        height={28}
        rx={8}
        ry={8}
        fill="var(--color-cc-surface)"
        stroke="rgba(245,241,234,0.22)"
        strokeWidth={1.25}
        strokeDasharray="4 3"
      />
      <motion.rect
        x={-58}
        y={-14}
        width={116}
        height={28}
        rx={8}
        ry={8}
        fill="none"
        stroke={CORAL}
        strokeWidth={1.5}
        style={{ opacity: onOpacity }}
      />
      <text
        textAnchor="middle"
        y={4}
        className="fill-cc-ink font-mono"
        style={{ fontSize: 10 }}
      >
        {node.label}
      </text>
    </g>
  );
}

interface TraceRowProps {
  readonly label: string;
  readonly index: number;
  readonly progress: MotionValue<number>;
}

function TraceRow({ label, index, progress }: TraceRowProps) {
  const scaleX = useTransform(
    progress,
    [STOPS[index], STOPS[index + 1]],
    [0, 1],
  );
  return (
    <div className="flex items-center gap-3">
      <span className="text-cc-ink w-28 shrink-0 truncate font-mono text-[11px]">
        {label}
      </span>
      <div className="bg-cc-surface/60 relative h-3 flex-1 rounded">
        <motion.div
          className="absolute top-0 left-0 h-3 rounded"
          style={{
            background: `linear-gradient(90deg, ${CORAL}, color-mix(in srgb, ${CORAL} 50%, transparent))`,
            scaleX,
            transformOrigin: "left center",
            width: `${(STOPS[index + 1] - STOPS[index]) * 100}%`,
            left: `${STOPS[index] * 100}%`,
          }}
        />
      </div>
    </div>
  );
}

function Centerpiece() {
  const reduced = useReducedMotion();
  const containerRef = useRef<HTMLDivElement>(null);
  const tokenRef = useRef<SVGCircleElement>(null);
  const inView = useInView(containerRef, { once: true, amount: 0.35 });

  // Shared MotionValue that drives every animated piece in the centerpiece:
  // node glow, done borders, the running token, the travelled coral line,
  // and the trace strip bars. Decoupled from scroll: a one-shot animation
  // from 0 -> 1 runs when the centerpiece first enters view. The replay
  // button re-runs the same animation in place.
  const journey = useMotionValue(reduced ? 1 : 0);

  // Track the running x/y of the token so the travelled-line endpoint follows.
  const tokenX = useTransform(journey, (v) => pathPoint(reduced ? 1 : v).x);
  const tokenY = useTransform(journey, (v) => pathPoint(reduced ? 1 : v).y);

  // Move the token along the precomputed polyline whenever journey changes.
  useEffect(() => {
    const apply = (v: number) => {
      if (!tokenRef.current) return;
      const { x, y } = pathPoint(reduced ? 1 : v);
      tokenRef.current.setAttribute("cx", String(x));
      tokenRef.current.setAttribute("cy", String(y));
    };
    apply(journey.get());
    const unsubscribe = journey.on("change", apply);
    return () => {
      unsubscribe();
    };
  }, [journey, reduced]);

  // One-shot: when the centerpiece enters view, run the journey 0 -> 1.
  useEffect(() => {
    if (reduced || !inView) return;
    const controls = animate(journey, 1, {
      duration: 2.4,
      ease: "easeInOut",
    });
    return () => {
      controls.stop();
    };
  }, [inView, journey, reduced]);

  const replay = () => {
    if (reduced) return;
    journey.set(0);
    animate(journey, 1, { duration: 2.4, ease: "easeInOut" });
  };

  return (
    <div ref={containerRef}>
      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md">
        <div className="border-cc-card-border bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
          <span className="text-cc-nav-label font-mono text-[11px]">
            mocha · topology · scroll to follow the message
          </span>
          <button
            type="button"
            onClick={replay}
            className="border-cc-card-border text-cc-ink hover:border-cc-card-border-hover hover:text-cc-heading rounded-md border px-2.5 py-1 font-mono text-[10px] transition-colors"
            aria-label="Replay the message journey"
          >
            replay
          </button>
        </div>

        <div className="p-4 sm:p-6">
          <svg
            viewBox="0 0 1000 280"
            className="block h-auto w-full"
            aria-label="Animated Mocha topology: a message travels from Producer through Mediator, Outbox, Bus, Inbox and Saga, then fans out to subscribers."
            role="img"
          >
            {/* main spine */}
            <motion.path
              d={`M ${NODES[0].x} ${NODES[0].y} L ${NODES[5].x} ${NODES[5].y}`}
              stroke="rgba(245,241,234,0.18)"
              strokeWidth="1.5"
              fill="none"
              strokeDasharray="6 5"
            />
            {/* fan-out wires from saga to each subscriber */}
            {SUBSCRIBERS.map((s) => (
              <path
                key={`wire-${s.id}`}
                d={`M ${NODES[5].x} ${NODES[5].y} L ${s.x} ${s.y}`}
                stroke="rgba(245,241,234,0.14)"
                strokeWidth="1.25"
                strokeDasharray="4 4"
                fill="none"
              />
            ))}

            {/* coral travelled portion (drawn from origin to current token x) */}
            <motion.line
              x1={NODES[0].x}
              y1={NODES[0].y}
              x2={tokenX}
              y2={tokenY}
              stroke={CORAL}
              strokeWidth="2"
              strokeLinecap="round"
              style={{ filter: `drop-shadow(0 0 6px ${CORAL})` }}
            />

            {/* nodes (each owns its own active/done transforms) */}
            {NODES.map((n, i) => (
              <TopologyNodeView
                key={n.id}
                node={n}
                progress={journey}
                index={i}
              />
            ))}

            {/* subscriber boxes */}
            {SUBSCRIBERS.map((s, i) => (
              <SubscriberView
                key={s.id}
                node={s}
                progress={journey}
                index={i}
              />
            ))}

            {/* the running token */}
            <circle
              ref={tokenRef}
              r="6"
              fill={CORAL}
              style={{ filter: `drop-shadow(0 0 8px ${CORAL})` }}
            />
          </svg>

          {/* synchronized OTel-style span strip */}
          <div className="border-cc-card-border mt-5 border-t pt-4">
            <div className="mb-2 flex items-center justify-between">
              <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
                trace · review.created
              </span>
              <span className="text-cc-nav-label font-mono text-[10px]">
                42.6ms · 6 spans
              </span>
            </div>
            <div className="space-y-1.5">
              {NODES.map((n, i) => (
                <TraceRow
                  key={n.id}
                  label={n.label.toLowerCase()}
                  index={i}
                  progress={journey}
                />
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ============================ Mediator vs Bus ============================ */

interface LaneProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly caption: string;
  readonly steps: readonly string[];
  readonly accent: boolean;
}

function Lane({ eyebrow, title, caption, steps, accent }: LaneProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 14 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.5, ease: "easeOut" }}
      className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 backdrop-blur-sm"
    >
      <p
        className="mb-1 font-mono text-[11px] tracking-[0.18em] uppercase"
        style={{ color: accent ? CORAL : TEAL }}
      >
        {eyebrow}
      </p>
      <h3 className="font-heading text-h6 text-cc-heading mb-4">{title}</h3>
      <svg viewBox="0 0 320 60" className="block h-auto w-full" aria-hidden>
        <motion.path
          d="M 20 30 L 300 30"
          stroke={accent ? CORAL : "rgba(245,241,234,0.3)"}
          strokeWidth="1.5"
          strokeDasharray={accent ? "5 4" : "0"}
          fill="none"
          initial={{ pathLength: 0 }}
          whileInView={{ pathLength: 1 }}
          viewport={{ once: true, amount: 0.5 }}
          transition={{ duration: 1.0, ease: "easeInOut" }}
        />
        <motion.circle
          r="4"
          fill={accent ? CORAL : TEAL}
          initial={{ cx: 20, cy: 30, opacity: 0 }}
          whileInView={{ cx: [20, 160, 300], opacity: [0, 1, 1] }}
          viewport={{ once: true, amount: 0.5 }}
          transition={{ duration: 1.4, ease: "easeInOut", delay: 0.2 }}
          style={{
            filter: accent ? `drop-shadow(0 0 6px ${CORAL})` : undefined,
          }}
        />
      </svg>
      <div className="mt-2 flex flex-wrap items-center gap-1.5">
        {steps.map((s, i) => (
          <div key={s} className="flex items-center gap-1.5">
            <span className="border-cc-card-border bg-cc-surface/70 text-cc-heading rounded-md border px-2.5 py-1 font-mono text-[11px]">
              {s}
            </span>
            {i < steps.length - 1 && (
              <span
                className="font-mono text-[11px]"
                style={{ color: accent ? CORAL : "rgba(245,241,234,0.4)" }}
              >
                →
              </span>
            )}
          </div>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-4 text-sm">{caption}</p>
    </motion.div>
  );
}

/* ============================== Saga strip =============================== */

interface SagaStepProps {
  readonly label: string;
  readonly index: number;
  readonly total: number;
}

function SagaStep({ label, index, total }: SagaStepProps) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const isTerminal = index === total - 1;
  return (
    <div ref={ref} className="flex items-center gap-2 sm:gap-3">
      {index > 0 && (
        <svg viewBox="0 0 40 12" className="h-3 w-8 sm:w-12" aria-hidden>
          <motion.line
            x1="0"
            y1="6"
            x2="34"
            y2="6"
            stroke={CORAL}
            strokeWidth="1.5"
            strokeDasharray="0"
            initial={{ pathLength: 0 }}
            animate={inView ? { pathLength: 1 } : { pathLength: 0 }}
            transition={{ duration: 0.4, delay: 0.15 + index * 0.15 }}
          />
          <polygon points="33,2 40,6 33,10" fill={CORAL} />
        </svg>
      )}
      <motion.div
        initial={{ scale: 0.92, opacity: 0 }}
        animate={
          inView
            ? { scale: [0.92, 1.03, 1], opacity: 1 }
            : { scale: 0.92, opacity: 0 }
        }
        transition={{ duration: 0.45, delay: index * 0.15 }}
        className="flex items-center gap-2 rounded-full border px-3.5 py-1.5"
        style={{
          borderColor: `color-mix(in srgb, ${isTerminal ? "#5eead4" : "#f0786a"} 55%, transparent)`,
          backgroundColor: `color-mix(in srgb, ${isTerminal ? "#5eead4" : "#f0786a"} 12%, transparent)`,
        }}
      >
        <span
          className="size-2 rounded-full"
          style={{ backgroundColor: isTerminal ? "#5eead4" : CORAL }}
        />
        <span className="text-cc-heading font-mono text-[12px]">{label}</span>
      </motion.div>
    </div>
  );
}

interface CountUpProps {
  readonly to: number;
  readonly suffix?: string;
}

function CountUp({ to, suffix = "" }: CountUpProps) {
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.6 });
  const reduced = useReducedMotion();
  useEffect(() => {
    if (!ref.current) return;
    if (reduced || !inView) {
      ref.current.textContent = `${to}${suffix}`;
      return;
    }
    const start = performance.now();
    const dur = 700;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / dur);
      const v = Math.round(to * (1 - Math.pow(1 - t, 3)));
      if (ref.current) ref.current.textContent = `${v}${suffix}`;
      if (t < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }, [inView, to, suffix, reduced]);
  return <span ref={ref}>0{suffix}</span>;
}

/* ============================ Reliability rail =========================== */

function ReliabilityRail() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });
  return (
    <div ref={ref} className="grid gap-4 sm:grid-cols-2">
      <motion.div
        initial={{ opacity: 0, x: -16 }}
        animate={inView ? { opacity: 1, x: 0 } : { opacity: 0, x: -16 }}
        transition={{ duration: 0.5 }}
        className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 backdrop-blur-sm"
      >
        <p
          className="mb-2 font-mono text-[11px] tracking-[0.18em] uppercase"
          style={{ color: CORAL }}
        >
          transactional outbox
        </p>
        <h3 className="font-heading text-h6 text-cc-heading mb-3">
          Commit with the DB write.
        </h3>
        <div className="flex items-center gap-2">
          <span className="border-cc-card-border text-cc-ink rounded-md border px-2 py-1 font-mono text-[11px]">
            db.SaveChanges
          </span>
          <motion.svg
            viewBox="0 0 30 12"
            className="h-3 w-8"
            aria-hidden
            initial={{ opacity: 0 }}
            animate={inView ? { opacity: 1 } : { opacity: 0 }}
            transition={{ delay: 0.3 }}
          >
            <motion.line
              x1="0"
              y1="6"
              x2="24"
              y2="6"
              stroke={CORAL}
              strokeWidth="1.5"
              initial={{ pathLength: 0 }}
              animate={inView ? { pathLength: 1 } : { pathLength: 0 }}
              transition={{ duration: 0.5, delay: 0.35 }}
            />
            <polygon points="23,2 30,6 23,10" fill={CORAL} />
          </motion.svg>
          <span
            className="rounded-md border px-2 py-1 font-mono text-[11px]"
            style={{
              borderColor: "color-mix(in srgb, #f0786a 50%, transparent)",
              color: "var(--color-cc-heading)",
              backgroundColor: "color-mix(in srgb, #f0786a 10%, transparent)",
            }}
          >
            outbox.row
          </span>
        </div>
        <p className="text-cc-ink-dim mt-4 text-sm">
          The message is written inside the same transaction as your state
          change, then dispatched by a relay. Crash anywhere and the row is
          either both there or both absent.
        </p>
      </motion.div>

      <motion.div
        initial={{ opacity: 0, x: 16 }}
        animate={inView ? { opacity: 1, x: 0 } : { opacity: 0, x: 16 }}
        transition={{ duration: 0.5, delay: 0.1 }}
        className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 backdrop-blur-sm"
      >
        <p
          className="mb-2 font-mono text-[11px] tracking-[0.18em] uppercase"
          style={{ color: CORAL }}
        >
          idempotent inbox
        </p>
        <h3 className="font-heading text-h6 text-cc-heading mb-3">
          Receive once, even on retry.
        </h3>
        <div className="flex items-center gap-2">
          <span className="border-cc-card-border text-cc-ink rounded-md border px-2 py-1 font-mono text-[11px]">
            receive(msg)
          </span>
          <motion.svg
            viewBox="0 0 30 12"
            className="h-3 w-8"
            aria-hidden
            initial={{ opacity: 0 }}
            animate={inView ? { opacity: 1 } : { opacity: 0 }}
            transition={{ delay: 0.45 }}
          >
            <motion.line
              x1="0"
              y1="6"
              x2="24"
              y2="6"
              stroke={CORAL}
              strokeWidth="1.5"
              initial={{ pathLength: 0 }}
              animate={inView ? { pathLength: 1 } : { pathLength: 0 }}
              transition={{ duration: 0.5, delay: 0.5 }}
            />
            <polygon points="23,2 30,6 23,10" fill={CORAL} />
          </motion.svg>
          <span
            className="rounded-md border px-2 py-1 font-mono text-[11px]"
            style={{
              borderColor: "color-mix(in srgb, #f0786a 50%, transparent)",
              color: "var(--color-cc-heading)",
              backgroundColor: "color-mix(in srgb, #f0786a 10%, transparent)",
            }}
          >
            inbox.dedupe
          </span>
        </div>
        <p className="text-cc-ink-dim mt-4 text-sm">
          A redelivery hits the inbox before your handler. If the message id is
          already there, the handler does not run a second time. Result:
          exactly-once processing, not exactly-once delivery.
        </p>
      </motion.div>
    </div>
  );
}

/* =============================== Transports ============================== */

interface TransportChipProps {
  readonly name: string;
  readonly tag: string;
  readonly highlight?: boolean;
  readonly index: number;
}

function TransportChip({ name, tag, highlight, index }: TransportChipProps) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { amount: 0.4 });
  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 10 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.5 }}
      transition={{ duration: 0.4, delay: index * 0.06 }}
      className="bg-cc-surface/60 relative flex items-center justify-between rounded-lg border px-3.5 py-2.5"
      style={{
        borderColor: highlight
          ? "color-mix(in srgb, #f0786a 50%, transparent)"
          : "var(--color-cc-card-border)",
      }}
    >
      <span className="text-cc-heading font-mono text-[13px]">{name}</span>
      <span
        className="font-mono text-[10px] tracking-wide uppercase"
        style={{ color: highlight ? CORAL : "var(--color-cc-nav-label)" }}
      >
        {tag}
      </span>
      {highlight && (
        <motion.span
          aria-hidden
          className="pointer-events-none absolute inset-0 rounded-lg"
          style={{
            boxShadow: `inset 0 0 0 1px color-mix(in srgb, ${CORAL} 60%, transparent)`,
          }}
          animate={inView ? { opacity: [0.4, 1, 0.4] } : { opacity: 0.7 }}
          transition={{
            duration: 2.4,
            repeat: inView ? Infinity : 0,
            ease: "easeInOut",
          }}
        />
      )}
    </motion.div>
  );
}

/* ============================== Honesty beat ============================= */

interface HonestyCardProps {
  readonly heading: string;
  readonly body: string;
  readonly index: number;
}

function HonestyCard({ heading, body, index }: HonestyCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.45, delay: index * 0.1 }}
    >
      <h3 className="font-heading text-h6 text-cc-heading mb-2">{heading}</h3>
      <p className="text-cc-ink-dim text-sm">{body}</p>
    </motion.div>
  );
}

/* ================================== Page ================================= */

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <div className="flex flex-col gap-28 py-6">
        {/* ---------------------------- HERO ---------------------------- */}
        <section className="grid items-center gap-12 lg:grid-cols-[1fr_1.05fr]">
          <div>
            <p className="border-cc-card-border bg-cc-card-bg text-cc-ink-dim mb-5 inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[11px]">
              <HeroBadgeDot />
              Live Wire · Mocha · mediator + message bus
            </p>
            <h1 className="font-heading text-h2 text-cc-heading">
              Let work continue
              <br />
              after the request.
            </h1>
            <p className="lead text-cc-ink-dim mt-6 max-w-xl">
              Return the response the instant the user needs it. Hand the slow,
              fan-out, cross-service work to a message and let it keep moving on
              its own.
            </p>
            <p className="text-cc-prose mt-5 max-w-xl">
              Mocha is one source-generated framework for both the in-process
              command you dispatch and the event you publish across services.
              Same handler-first model, same traces, whichever way the work
              travels.
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-3">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
            </div>
            <ul className="mt-8 flex flex-wrap gap-x-6 gap-y-2">
              {[
                "Source-generated dispatch",
                "Validated sagas",
                "Outbox + inbox reliability",
              ].map((t) => (
                <li
                  key={t}
                  className="text-cc-ink-dim flex items-center gap-2 text-sm"
                >
                  <span style={{ color: CORAL }}>
                    <CheckIcon />
                  </span>
                  {t}
                </li>
              ))}
            </ul>
          </div>
          <HeroPreview />
        </section>

        {/* -------------------------- CENTERPIECE ----------------------- */}
        <section>
          <div className="max-w-2xl">
            <SectionLabel>follow one message through the topology</SectionLabel>
            <h2 className="font-heading text-h3 text-cc-heading">
              Scroll, and the message moves.
            </h2>
            <p className="text-cc-prose mt-4">
              Producer to Mediator to Outbox to Bus to Inbox to Saga, then
              fan-out to subscribers. As you scroll, a coral token travels the
              wire and the trace strip below fills in span by span. Coral is the
              hop in flight; teal is the hop already committed.
            </p>
          </div>
          <div className="mt-8">
            <Centerpiece />
          </div>
        </section>

        {/* ---------------------- TWO DISPATCH LANES -------------------- */}
        <section>
          <div className="max-w-2xl">
            <SectionLabel>two ways to dispatch · one model</SectionLabel>
            <h2 className="font-heading text-h3 text-cc-heading">
              In-process when it&apos;s near. On the bus when it&apos;s far.
            </h2>
            <p className="text-cc-prose mt-4">
              Inside one process, the mediator dispatches commands and queries
              straight to a{" "}
              <span className="text-cc-ink font-mono">[Handler]</span> through a
              pre-compiled pipeline. When the work belongs to another service,
              the same publish crosses a transport and fans out to its
              consumers. You change the verb, not the mental model.
            </p>
          </div>
          <div className="mt-8 grid gap-5 lg:grid-cols-2">
            <Lane
              eyebrow="mediator · in-process CQRS"
              title="Dispatch and reply, no hops"
              caption="Commands, queries, and notifications resolve through a source-generated pipeline. No reflection, no service-locator lookup on the hot path."
              steps={["CreateReview", "ISender", "[Handler]", "Result"]}
              accent={false}
            />
            <Lane
              eyebrow="message bus · cross-service"
              title="Publish and fan out, durably"
              caption="One event reaches every interested service through a pluggable transport, with outbox and inbox guaranteeing each consumer processes it exactly once."
              steps={[
                "ReviewCreated",
                "PublishAsync",
                "transport",
                "consumers",
              ]}
              accent
            />
          </div>
        </section>

        {/* -------------------------- SAGA STRIP ------------------------ */}
        <section>
          <div className="max-w-2xl">
            <SectionLabel>sagas · stateful workflows</SectionLabel>
            <h2 className="font-heading text-h3 text-cc-heading">
              A workflow that can&apos;t get stuck.
            </h2>
            <p className="text-cc-prose mt-4">
              A review moves{" "}
              <span className="text-cc-ink font-mono">
                Draft, Checked, Published
              </span>{" "}
              across several messages and minutes. Define that state machine
              once; Mocha checks that every state is reachable and every path
              reaches a final state, validated before the service handles
              traffic, so a saga that can dead-end never makes it into
              production.
            </p>
          </div>

          <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-2xl border p-6 backdrop-blur-sm sm:p-8">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
                ReviewSaga
              </span>
              <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px]">
                <span style={{ color: TEAL }}>
                  <CheckIcon />
                </span>
                <span>
                  validated · <CountUp to={3} />
                  {" of "}
                  <CountUp to={3} suffix=" paths terminal" />
                </span>
              </span>
            </div>
            <div className="mt-6 flex flex-wrap items-center gap-y-4">
              {["Draft", "Checked", "Published"].map((label, i) => (
                <SagaStep key={label} label={label} index={i} total={3} />
              ))}
            </div>
            <div className="border-cc-card-border mt-6 grid gap-3 border-t pt-5 sm:grid-cols-3">
              {[
                ["running hop", "in flight right now", CORAL],
                ["solid hop", "committed", "var(--color-cc-accent)"],
                ["dashed hop", "not yet reached", "rgba(245,241,234,0.4)"],
              ].map(([k, v, c]) => (
                <div key={k} className="flex items-center gap-2">
                  <span
                    className="size-2 rounded-full"
                    style={{ backgroundColor: c }}
                  />
                  <span className="text-cc-ink font-mono text-[11px]">{k}</span>
                  <span className="text-cc-nav-label font-mono text-[11px]">
                    , {v}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* ----------------------- RELIABILITY RAIL --------------------- */}
        <section>
          <div className="max-w-2xl">
            <SectionLabel>outbox + inbox</SectionLabel>
            <h2 className="font-heading text-h3 text-cc-heading">
              The two boxes that make it exactly-once.
            </h2>
            <p className="text-cc-prose mt-4">
              Reliability lives in two adjacent stores. The outbox commits the
              message with your database write so a send cannot be lost. The
              inbox deduplicates on receive so a redelivery cannot run your
              handler twice.
            </p>
          </div>
          <div className="mt-8">
            <ReliabilityRail />
          </div>
        </section>

        {/* ------------------------- TRANSPORTS ------------------------ */}
        <section>
          <div className="max-w-2xl">
            <SectionLabel>pluggable transports</SectionLabel>
            <h2 className="font-heading text-h3 text-cc-heading">
              Swap the broker, keep the handlers.
            </h2>
            <p className="text-cc-prose mt-4">
              The transport is a registration detail, not a rewrite. Start
              in-process, move to Postgres or RabbitMQ in production, route
              high-throughput streams through Kafka, and run several at once.
              Your handlers never know the difference.
            </p>
          </div>
          <div className="mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {[
              { name: "RabbitMQ", tag: "broker", highlight: true },
              { name: "Postgres", tag: "durable", highlight: false },
              { name: "Kafka", tag: "streaming", highlight: false },
              { name: "Azure Service Bus", tag: "cloud", highlight: false },
              { name: "In-process", tag: "zero-infra", highlight: false },
              { name: "Azure Event Hub", tag: "ingest", highlight: false },
            ].map((t, i) => (
              <TransportChip
                key={t.name}
                name={t.name}
                tag={t.tag}
                highlight={t.highlight}
                index={i}
              />
            ))}
          </div>
        </section>

        {/* ------------------------ HONESTY BEAT ----------------------- */}
        <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
          <SectionLabel>what we mean precisely</SectionLabel>
          <h2 className="font-heading text-h4 text-cc-heading max-w-3xl">
            Reliability claims, stated honestly.
          </h2>
          <div className="mt-6 grid gap-6 sm:grid-cols-3">
            {[
              {
                h: "Exactly-once processing",
                p: "The outbox commits the message with your database write, and the inbox deduplicates on receive. That gives exactly-once processing, not exactly-once delivery, which no transport can promise.",
              },
              {
                h: "Sagas validated before traffic",
                p: "The state-machine check runs before the service handles traffic, not at compile time. It proves your saga can always reach a final state.",
              },
              {
                h: "Published clients aren't surprised",
                p: "Because dispatch is source-generated and contracts are explicit, a changed message shows up at build time, so you can see which published clients are affected.",
              },
            ].map((c, i) => (
              <HonestyCard key={c.h} heading={c.h} body={c.p} index={i} />
            ))}
          </div>
        </section>

        {/* --------------------------- CLOSE --------------------------- */}
        <section className="flex flex-col items-center gap-6 text-center">
          <svg
            viewBox="0 0 240 4"
            className="h-1 w-60"
            preserveAspectRatio="none"
            aria-hidden
          >
            <motion.path
              d="M 0 2 L 240 2"
              stroke="url(#cc-spectrum-v7)"
              strokeWidth="2"
              fill="none"
              initial={{ pathLength: 0 }}
              whileInView={{ pathLength: 1 }}
              viewport={{ once: true, amount: 0.6 }}
              transition={{ duration: 1.0, ease: "easeInOut" }}
            />
            <defs>
              <linearGradient id="cc-spectrum-v7" x1="0" y1="0" x2="1" y2="0">
                <stop offset="0%" stopColor="#16b9e4" />
                <stop offset="50%" stopColor="#7c92c6" />
                <stop offset="100%" stopColor="#f0786a" />
              </linearGradient>
            </defs>
          </svg>
          <h2 className="font-heading text-h3 text-cc-heading max-w-2xl">
            Ship the response. Keep the work moving.
          </h2>
          <p className="text-cc-prose max-w-xl">
            One framework for the command you dispatch in-process and the event
            you publish across services, with reliability and traces built in.
          </p>
          <div className="mt-2 flex flex-wrap justify-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
          </div>
        </section>
      </div>
    </MotionConfig>
  );
}
