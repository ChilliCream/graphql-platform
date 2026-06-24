"use client";

import type { CSSProperties, ReactNode } from "react";
import { useRef } from "react";
import { motion, useInView, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Single brand-spectrum hairline, used exactly once on the closing CTA.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const ACCENT = "#5eead4"; // cc-accent teal

// -----------------------------------------------------------------------------
// Deterministic 64-sample waveform.
//
// Values in [0..1]. Built once at module load from a seeded LCG so the bars are
// stable across renders and SSR/CSR. Four bars are tagged as named events so
// the waveform reads as a literal transcript of topic traffic, with the highest
// bar reserved for the "process" tick (saga compensated, the climax).
// -----------------------------------------------------------------------------

const BAR_COUNT = 64;

function buildWave(): readonly number[] {
  // Mulberry32-style LCG, fixed seed.
  let s = 0x9e3779b9;
  const next = () => {
    s = (s + 0x6d2b79f5) | 0;
    let t = s;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
  const out: number[] = [];
  for (let i = 0; i < BAR_COUNT; i++) {
    // Two sinusoidal carriers + low noise, normalized to a min floor.
    const base =
      0.18 +
      0.42 * (0.5 + 0.5 * Math.sin((i / BAR_COUNT) * Math.PI * 3.1)) +
      0.22 * (0.5 + 0.5 * Math.sin((i / BAR_COUNT) * Math.PI * 7.3 + 1.2));
    const noise = (next() - 0.5) * 0.18;
    out.push(Math.max(0.08, Math.min(0.96, base + noise)));
  }
  // Engineered peaks for the labeled events. The "process" peak (index 47)
  // is set highest so the process tick line lands on it.
  out[9] = 0.62; //  CreateReview     (publish)
  out[23] = 0.71; // ReviewCreated    (deliver)
  out[47] = 0.98; // SagaCompensated  (process, anchor)
  out[55] = 0.58; // OrderPlaced
  return out;
}

const WAVE = buildWave();

interface LabeledEvent {
  readonly index: number;
  readonly name: string;
}

const LABELED_EVENTS: readonly LabeledEvent[] = [
  { index: 9, name: "CreateReview" },
  { index: 23, name: "ReviewCreated" },
  { index: 47, name: "SagaCompensated" },
  { index: 55, name: "OrderPlaced" },
];

const LABELED_INDEX_SET = new Set(LABELED_EVENTS.map((e) => e.index));

// Phase ticks: publish around the first labeled event, deliver around the
// second, process exactly on the highest bar.
const PHASE_TICKS: readonly { id: string; label: string; index: number }[] = [
  { id: "publish", label: "publish", index: 9 },
  { id: "deliver", label: "deliver", index: 23 },
  { id: "process", label: "process", index: 47 },
];

// -----------------------------------------------------------------------------
// Small primitives
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

interface SectionHeaderProps {
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly subtitle?: string;
}

function SectionHeader({
  index,
  eyebrow,
  title,
  subtitle,
}: SectionHeaderProps) {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <IndexTag value={index} />
        <Eyebrow>{eyebrow}</Eyebrow>
      </div>
      {/* Echo of the waveform: a thin 2px accent dash row under the eyebrow. */}
      <div aria-hidden className="flex h-[2px] items-end gap-[3px]">
        {[3, 6, 4, 8, 5, 3, 7, 4, 6, 3].map((h, i) => (
          <span
            key={i}
            className="bg-cc-accent block w-[2px]"
            style={{ height: `${h}px`, opacity: 0.55 }}
          />
        ))}
      </div>
      <h2 className="text-cc-heading font-heading mt-1 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
        {title}
      </h2>
      {subtitle ? (
        <p className="text-cc-prose mt-1 max-w-2xl text-base leading-relaxed sm:text-lg">
          {subtitle}
        </p>
      ) : null}
    </div>
  );
}

// Card with a 1px inner top highlight to read as a recording-strip lip.
const CARD_INSET_SHADOW: CSSProperties = {
  boxShadow: "inset 0 1px 0 rgba(245, 241, 234, 0.04)",
};

// -----------------------------------------------------------------------------
// 02 Signal on the wire: the waveform band itself
// -----------------------------------------------------------------------------

interface WaveformBandProps {
  readonly wave: readonly number[];
}

function WaveformBand({ wave }: WaveformBandProps) {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, margin: "-10% 0px" });

  // Layout constants for the band, mirrored in the label positioning logic.
  const barTrackHeightPx = 220; // visual height of the bar track

  return (
    <div ref={ref} className="relative">
      <div
        aria-hidden
        className="absolute inset-y-0 left-1/2 -z-10 w-screen -translate-x-1/2"
      >
        <div className="border-cc-card-border bg-cc-surface h-full w-full border-y" />
      </div>

      <div className="relative px-1 sm:px-2">
        {/* Phase ticks: thin vertical guides at publish / deliver / process */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 z-0"
          style={{ height: `${barTrackHeightPx}px` }}
        >
          {PHASE_TICKS.map((tick) => {
            const leftPct = ((tick.index + 0.5) / BAR_COUNT) * 100;
            const isProcess = tick.id === "process";
            return (
              <div
                key={tick.id}
                className="absolute top-0 bottom-0"
                style={{
                  left: `${leftPct}%`,
                  width: "1px",
                  background: isProcess
                    ? "rgba(94,234,212,0.45)"
                    : "rgba(245,241,234,0.10)",
                }}
              />
            );
          })}
        </div>

        {/* Bar track */}
        <div
          className="relative z-10 flex items-end justify-between gap-[3px]"
          style={{ height: `${barTrackHeightPx}px` }}
        >
          {wave.map((v, i) => {
            const isLabeled = LABELED_INDEX_SET.has(i);
            const targetPx = Math.round(v * (barTrackHeightPx - 10) + 6);
            const initialHeight = reduce ? targetPx : 0;
            return (
              <motion.span
                key={i}
                aria-hidden
                className="bg-cc-accent block w-[3px] flex-1 rounded-[1px] sm:w-[4px]"
                style={{
                  opacity: isLabeled ? 1 : 0.72,
                  height: initialHeight,
                  maxWidth: "10px",
                }}
                initial={{ height: initialHeight }}
                animate={
                  reduce
                    ? { height: targetPx }
                    : inView
                      ? { height: targetPx }
                      : { height: 0 }
                }
                transition={{
                  duration: 0.5,
                  delay: reduce ? 0 : (i * 12) / 1000,
                  ease: [0.2, 0.7, 0.2, 1],
                }}
              />
            );
          })}
        </div>

        {/* Playhead: time-driven left-to-right loop, dwell at each end */}
        {!reduce ? (
          <motion.div
            aria-hidden
            className="pointer-events-none absolute z-20"
            style={{
              top: 0,
              height: `${barTrackHeightPx}px`,
              width: "1px",
              background: ACCENT,
              boxShadow: "0 0 8px rgba(94,234,212,0.6)",
              left: 0,
            }}
            animate={{ left: ["0%", "100%", "100%", "0%", "0%"] }}
            transition={{
              duration: 6,
              ease: "linear",
              times: [0, 0.46, 0.5, 0.96, 1],
              repeat: Infinity,
              repeatType: "loop",
            }}
          />
        ) : null}

        {/* Phase tick labels (publish / deliver / process) above the bars */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 -top-6"
        >
          {PHASE_TICKS.map((tick) => {
            const leftPct = ((tick.index + 0.5) / BAR_COUNT) * 100;
            const isProcess = tick.id === "process";
            return (
              <span
                key={tick.id}
                className={`absolute -translate-x-1/2 font-mono text-[10px] tracking-widest uppercase ${
                  isProcess ? "text-cc-accent" : "text-cc-ink-dim"
                }`}
                style={{ left: `${leftPct}%` }}
              >
                {tick.label}
              </span>
            );
          })}
        </div>

        {/* Event labels under specific bars */}
        <div className="relative mt-3 h-10">
          {LABELED_EVENTS.map((evt) => {
            const leftPct = ((evt.index + 0.5) / BAR_COUNT) * 100;
            return (
              <div
                key={evt.name}
                className="absolute top-0 -translate-x-1/2 text-center"
                style={{ left: `${leftPct}%` }}
              >
                <div
                  aria-hidden
                  className="bg-cc-accent mx-auto h-2 w-px opacity-70"
                />
                <div className="text-cc-ink mt-1 font-mono text-[10.5px] tracking-tight whitespace-nowrap">
                  {evt.name}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// 03 Code panels: shared [Handler] attribute across mediator and bus.
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
  kw: { color: "#ff7b72" } as CSSProperties,
  type: { color: "#ffa657" } as CSSProperties,
  comment: { color: "#8b949e", fontStyle: "italic" } as CSSProperties,
  attr: { color: "#5eead4", fontWeight: 600 } as CSSProperties,
  fn: { color: "#d2a8ff" } as CSSProperties,
  param: { color: "#79c0ff" } as CSSProperties,
  punct: { color: "#c9d1d9" } as CSSProperties,
  plain: { color: "#c9d1d9" } as CSSProperties,
};

interface CodePanelProps {
  readonly file: string;
  readonly tag: string;
  readonly footer: ReactNode;
  readonly children: ReactNode;
}

function CodePanel({ file, tag, footer, children }: CodePanelProps) {
  return (
    <div
      className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border"
      style={CARD_INSET_SHADOW}
    >
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
          {file}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <div className="relative py-4">{children}</div>
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        {footer}
      </div>
    </div>
  );
}

function MediatorPanel() {
  return (
    <CodePanel
      file="Reviews/CreateReviewHandler.cs"
      tag="mediator"
      footer={
        <>
          <span>ISender.Send(CreateReview)</span>
          <span className="text-cc-accent">in-process</span>
        </>
      }
    >
      <CodeLine n={1}>
        <span style={C.kw}>using</span> <span style={C.plain}>Mocha;</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.kw}>public record</span>{" "}
        <span style={C.type}>CreateReview</span>
        <span style={C.punct}>(</span>
        <span style={C.type}>Guid</span> <span style={C.param}>ProductId</span>
        <span style={C.punct}>, </span>
        <span style={C.type}>string</span> <span style={C.param}>Text</span>
        <span style={C.punct}>)</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.plain}>{`    : `}</span>
        <span style={C.type}>ICommand</span>
        <span style={C.punct}>{`<`}</span>
        <span style={C.type}>Guid</span>
        <span style={C.punct}>{`>;`}</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.punct}>[</span>
        <span style={C.attr}>Handler</span>
        <span style={C.punct}>]</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.kw}>public static async</span>{" "}
        <span style={C.type}>Task</span>
        <span style={C.punct}>{`<`}</span>
        <span style={C.type}>Guid</span>
        <span style={C.punct}>{`>`}</span> <span style={C.fn}>HandleAsync</span>
        <span style={C.punct}>(</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.type}>CreateReview</span>{" "}
        <span style={C.param}>command</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.type}>ReviewsDbContext</span>{" "}
        <span style={C.param}>db</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.type}>IPublisher</span> <span style={C.param}>bus</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={11}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.type}>CancellationToken</span>{" "}
        <span style={C.param}>ct</span>
        <span style={C.punct}>{`)`}</span>
      </CodeLine>
      <CodeLine n={12}>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={13}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>var</span> <span style={C.param}>review</span>{" "}
        <span style={C.punct}>=</span> <span style={C.type}>Review</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>Draft</span>
        <span style={C.punct}>(</span>
        <span style={C.param}>command</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={14}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>await</span> <span style={C.param}>bus</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>PublishAsync</span>
        <span style={C.punct}>(</span>
        <span style={C.kw}>new</span> <span style={C.type}>ReviewCreated</span>
        <span style={C.punct}>(</span>
        <span style={C.param}>review</span>
        <span style={C.punct}>.</span>
        <span style={C.plain}>Id</span>
        <span style={C.punct}>), </span>
        <span style={C.param}>ct</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={15}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>return</span> <span style={C.param}>review</span>
        <span style={C.punct}>.</span>
        <span style={C.plain}>Id</span>
        <span style={C.punct}>;</span>
      </CodeLine>
      <CodeLine n={16}>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
    </CodePanel>
  );
}

function BusPanel() {
  return (
    <CodePanel
      file="Search/ReviewCreatedHandler.cs"
      tag="bus"
      footer={
        <>
          <span>IEventHandler over RabbitMQ / Azure SB</span>
          <span className="text-cc-accent">same shape</span>
        </>
      }
    >
      <CodeLine n={1}>
        <span style={C.kw}>using</span> <span style={C.plain}>Mocha;</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.comment}>
          {`// Received from the bus, processed exactly once via inbox.`}
        </span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.punct}>[</span>
        <span style={C.attr}>Handler</span>
        <span style={C.punct}>]</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.kw}>public static async</span>{" "}
        <span style={C.type}>Task</span> <span style={C.fn}>HandleAsync</span>
        <span style={C.punct}>(</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.type}>ReviewCreated</span>{" "}
        <span style={C.param}>evt</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.type}>ISearchIndex</span>{" "}
        <span style={C.param}>index</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.type}>CancellationToken</span>{" "}
        <span style={C.param}>ct</span>
        <span style={C.punct}>{`) =>`}</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>await</span> <span style={C.param}>index</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>UpsertAsync</span>
        <span style={C.punct}>(</span>
        <span style={C.param}>evt</span>
        <span style={C.punct}>.</span>
        <span style={C.plain}>ReviewId</span>
        <span style={C.punct}>, </span>
        <span style={C.param}>ct</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={11}>
        <span style={C.comment}>
          {`// Same [Handler] attribute, dispatched from a transport.`}
        </span>
      </CodeLine>
    </CodePanel>
  );
}

// -----------------------------------------------------------------------------
// 04 Saga step strip
// -----------------------------------------------------------------------------

const SAGA_STEPS: readonly {
  readonly id: string;
  readonly label: string;
  readonly body: string;
}[] = [
  {
    id: "define",
    label: "Define",
    body: "States, triggers, transitions, compensations, all in one type.",
  },
  {
    id: "analyze",
    label: "Analyze",
    body: "The source generator walks the saga graph at compile time.",
  },
  {
    id: "validate",
    label: "Validate",
    body: "Reachable states, handled triggers, terminal paths checked at startup.",
  },
  {
    id: "run",
    label: "Run",
    body: "A saga shape that is not consistent never gets past startup.",
  },
];

function SagaStrip() {
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
      {SAGA_STEPS.map((s, i) => (
        <div
          key={s.id}
          className="border-cc-card-border bg-cc-surface relative rounded-xl border p-5"
          style={CARD_INSET_SHADOW}
        >
          <div className="flex items-center justify-between">
            <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
              step {String(i + 1).padStart(2, "0")}
            </span>
            <span aria-hidden className="text-cc-accent">
              <CheckIcon size={14} />
            </span>
          </div>
          <div className="text-cc-heading font-heading mt-3 text-lg font-semibold">
            {s.label}
          </div>
          <p className="text-cc-prose mt-2 text-sm leading-relaxed">{s.body}</p>
        </div>
      ))}
    </div>
  );
}

// -----------------------------------------------------------------------------
// 05 Exactly-once processing pair
// -----------------------------------------------------------------------------

function ExactlyOncePair() {
  return (
    <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
      <div
        className="border-cc-card-border bg-cc-surface relative rounded-xl border p-6"
        style={CARD_INSET_SHADOW}
      >
        <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
          transport guarantee
        </span>
        <h3 className="text-cc-heading font-heading mt-3 text-xl font-semibold tracking-tight">
          At-least-once delivery
        </h3>
        <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">
          The broker can redeliver. Redelivery happens after retries, after
          consumer restarts, after acknowledgement failures. Treating delivery
          as exactly-once is the bug.
        </p>
        <ul className="mt-5 flex flex-col gap-2.5">
          {[
            "Redelivery on consumer crash mid-handle.",
            "Redelivery on ack loss after success.",
            "Redelivery on partition rebalance.",
          ].map((b) => (
            <li
              key={b}
              className="text-cc-ink-dim flex items-start gap-3 text-sm"
            >
              <span className="text-cc-ink-dim mt-1.5 inline-block h-1 w-1 shrink-0 rounded-full bg-current opacity-60" />
              <span>{b}</span>
            </li>
          ))}
        </ul>
      </div>
      <div
        className="border-cc-accent/40 bg-cc-surface relative rounded-xl border p-6"
        style={{
          ...CARD_INSET_SHADOW,
          borderColor: "rgba(94,234,212,0.45)",
        }}
      >
        <span className="text-cc-accent font-mono text-[10.5px] tracking-widest uppercase">
          mocha guarantee
        </span>
        <h3 className="text-cc-heading font-heading mt-3 text-xl font-semibold tracking-tight">
          Exactly-once PROCESSING
        </h3>
        <p className="text-cc-prose mt-3 text-sm leading-relaxed">
          Domain change and outbox row commit in one transaction. The inbox
          records the message id on the receive side, so a redelivered message
          is observed and skipped. The handler runs exactly once.
        </p>
        <ul className="mt-5 flex flex-col gap-2.5">
          {[
            "Transactional outbox commits with the domain row.",
            "Idempotent inbox dedupes redeliveries by id.",
            "Per-exception retry, dead-letter, and concurrency limiter.",
          ].map((b) => (
            <li key={b} className="text-cc-ink flex items-start gap-3 text-sm">
              <span className="text-cc-accent mt-1 shrink-0">
                <CheckIcon size={14} />
              </span>
              <span>{b}</span>
            </li>
          ))}
        </ul>
        {/* Tiny three-bar echo of the waveform inside the card */}
        <div
          aria-hidden
          className="border-cc-card-border mt-5 flex items-end gap-1.5 border-t pt-4"
        >
          {[14, 22, 36].map((h, i) => (
            <span
              key={i}
              className="bg-cc-accent block w-[3px] rounded-[1px]"
              style={{ height: `${h}px`, opacity: 0.85 }}
            />
          ))}
          <span className="text-cc-ink-dim ml-2 font-mono text-[10.5px] tracking-tight">
            commit / dedupe / process
          </span>
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// 06 Transports + storage grid
// -----------------------------------------------------------------------------

interface TileProps {
  readonly group: string;
  readonly name: string;
  readonly role: string;
}

function GridTile({ group, name, role }: TileProps) {
  return (
    <div
      className="border-cc-card-border bg-cc-surface flex flex-col gap-3 rounded-xl border p-5"
      style={CARD_INSET_SHADOW}
    >
      <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
        {group}
      </span>
      <span className="text-cc-heading font-mono text-base">{name}</span>
      <span className="text-cc-prose text-sm leading-relaxed">{role}</span>
    </div>
  );
}

const TRANSPORTS: readonly TileProps[] = [
  {
    group: "transport",
    name: "RabbitMQ",
    role: "Topic and queue routing for cross-service traffic.",
  },
  {
    group: "transport",
    name: "Azure Service Bus",
    role: "Managed broker on Azure with sessions and dead-letter queues.",
  },
  {
    group: "transport",
    name: "in-process",
    role: "Same handler shape in tests and local development.",
  },
];

const STORAGE: readonly TileProps[] = [
  {
    group: "storage",
    name: "Postgres outbox",
    role: "Transactional outbox alongside your domain rows.",
  },
  {
    group: "storage",
    name: "SQL Server outbox",
    role: "Outbox on SQL Server, same wiring through your DbContext.",
  },
  {
    group: "storage",
    name: "EF Core inbox",
    role: "Inbox table on the consumer side, indexed by message id.",
  },
];

// -----------------------------------------------------------------------------
// 07 Observability small trace illustration
// -----------------------------------------------------------------------------

interface TraceRowProps {
  readonly label: string;
  readonly leftPct: number;
  readonly widthPct: number;
  readonly tone: "primary" | "muted";
}

function TraceRow({ label, leftPct, widthPct, tone }: TraceRowProps) {
  return (
    <div className="flex items-center gap-3">
      <span className="text-cc-ink-dim w-36 shrink-0 truncate font-mono text-[10.5px] tracking-tight">
        {label}
      </span>
      <div className="bg-cc-bg/40 border-cc-card-border relative h-3 flex-1 rounded-full border">
        <div
          className="absolute inset-y-0 rounded-full"
          style={{
            left: `${leftPct}%`,
            width: `${widthPct}%`,
            background:
              tone === "primary"
                ? "rgba(94,234,212,0.75)"
                : "rgba(245,241,234,0.30)",
          }}
        />
      </div>
    </div>
  );
}

function TraceIllustration() {
  return (
    <div
      className="border-cc-card-border bg-cc-surface rounded-xl border p-5"
      style={CARD_INSET_SHADOW}
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
          trace
        </span>
        <span className="text-cc-ink-dim font-mono text-[10.5px] tabular-nums">
          78 ms
        </span>
      </div>
      <div className="mt-4 flex flex-col gap-3">
        <TraceRow
          label="ISender.Send"
          leftPct={0}
          widthPct={14}
          tone="primary"
        />
        <TraceRow
          label="CreateReviewHandler"
          leftPct={2}
          widthPct={28}
          tone="muted"
        />
        <TraceRow
          label="outbox.commit"
          leftPct={18}
          widthPct={10}
          tone="primary"
        />
        <TraceRow label="bus.publish" leftPct={30} widthPct={8} tone="muted" />
        <TraceRow
          label="transport.deliver"
          leftPct={38}
          widthPct={16}
          tone="primary"
        />
        <TraceRow label="inbox.dedupe" leftPct={54} widthPct={6} tone="muted" />
        <TraceRow
          label="ReviewCreatedHandler"
          leftPct={60}
          widthPct={34}
          tone="primary"
        />
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  const reduce = useReducedMotion();

  // Hero gridline backdrop, five 1px rules at 8% opacity.
  const HERO_GRID: CSSProperties = {
    backgroundImage:
      "repeating-linear-gradient(to bottom, rgba(245,241,234,0.08) 0 1px, transparent 1px 56px)",
    backgroundSize: "100% 280px",
    backgroundPosition: "center 40px",
    backgroundRepeat: "no-repeat",
  };

  return (
    <div className="mx-auto w-full max-w-6xl px-4 sm:px-6">
      {/* 01 HERO */}
      <section className="relative pt-16 pb-20 sm:pt-24 sm:pb-24">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10"
          style={HERO_GRID}
        />
        <div className="flex flex-col gap-6">
          <div className="flex items-center gap-3">
            <IndexTag value="01" />
            <Eyebrow>Mocha messaging .NET</Eyebrow>
          </div>
          <div aria-hidden className="flex h-[2px] items-end gap-[3px]">
            {[4, 8, 3, 6, 10, 5, 7, 3, 9, 4, 6, 8, 3, 5].map((h, i) => (
              <span
                key={i}
                className="bg-cc-accent block w-[2px]"
                style={{ height: `${h}px`, opacity: 0.6 }}
              />
            ))}
          </div>
          <motion.h1
            className="text-cc-heading font-heading mt-2 max-w-4xl text-5xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-6xl"
            initial={reduce ? false : { opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: [0.2, 0.7, 0.2, 1] }}
          >
            Messages you can read on a scope.
          </motion.h1>
          <motion.p
            className="text-cc-prose max-w-2xl text-lg leading-relaxed"
            initial={reduce ? false : { opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{
              duration: 0.5,
              delay: 0.08,
              ease: [0.2, 0.7, 0.2, 1],
            }}
          >
            Mocha is the open-source source-generated mediator and bus for .NET.
            Same handler shape in-process and across services. Validated sagas,
            transactional outbox, idempotent inbox, every hop a span.
          </motion.p>
          <motion.div
            className="mt-2 flex flex-wrap gap-3"
            initial={reduce ? false : { opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{
              duration: 0.5,
              delay: 0.16,
              ease: [0.2, 0.7, 0.2, 1],
            }}
          >
            <SolidButton href="/docs/mocha">Read the docs</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              See the API
            </OutlineButton>
          </motion.div>
          <span className="text-cc-ink-dim mt-3 font-mono text-[11px] tracking-widest uppercase">
            Mocha messaging .NET
          </span>
        </div>
      </section>

      {/* 02 SIGNAL ON THE WIRE */}
      <section className="relative py-24 sm:py-28">
        <div className="flex flex-col gap-8">
          <SectionHeader
            index="02"
            eyebrow="Signal on the wire"
            title="A topic slice, drawn as a waveform."
            subtitle="A deterministic 64-sample slice of an event topic, with four messages labeled. The publish, deliver, and process phase ticks anchor where each message hits the pipeline. Illustrative, not live."
          />
          <div className="pt-8">
            <WaveformBand wave={WAVE} />
          </div>
          <p className="text-cc-ink-dim font-mono text-[11px] tracking-tight">
            64 samples / one slice / process tick on the highest bar
          </p>
        </div>
      </section>

      {/* 03 ONE GENERATOR, TWO SURFACES */}
      <section className="relative py-20 sm:py-24">
        <div className="flex flex-col gap-10">
          <SectionHeader
            index="03"
            eyebrow="One generator, two surfaces"
            title="Same [Handler] attribute, two dispatch boundaries."
            subtitle="A Roslyn source generator discovers handlers across your assemblies and emits typed registration plus pre-compiled pipeline delegates. The same handler shape runs in-process through ISender and across services through the bus."
          />
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <MediatorPanel />
            <BusPanel />
          </div>
        </div>
      </section>

      {/* 04 SAGAS VALIDATED BEFORE TRAFFIC */}
      <section className="relative py-20 sm:py-24">
        <div className="flex flex-col gap-10">
          <SectionHeader
            index="04"
            eyebrow="Validated sagas"
            title="Sagas validated before traffic."
            subtitle="A saga is a state machine: states, triggers, transitions, compensations. Mocha walks the graph at compile time and checks it at startup. A shape that is not consistent never sees traffic."
          />
          <SagaStrip />
        </div>
      </section>

      {/* 05 EXACTLY-ONCE PROCESSING, HONESTLY */}
      <section className="relative py-20 sm:py-24">
        <div className="flex flex-col gap-10">
          <SectionHeader
            index="05"
            eyebrow="Exactly-once, honestly"
            title="At-least-once delivery, exactly-once PROCESSING."
            subtitle="Brokers redeliver. Pretending otherwise is the bug. Mocha pairs a transactional outbox with an idempotent inbox so the handler runs exactly once even when the wire repeats the message."
          />
          <ExactlyOncePair />
        </div>
      </section>

      {/* 06 TRANSPORTS AND STORAGE */}
      <section className="relative py-20 sm:py-24">
        <div className="flex flex-col gap-10">
          <SectionHeader
            index="06"
            eyebrow="Transports and storage"
            title="Pick your wire, pick your store."
            subtitle="Same handlers, different infrastructure. Route specific message types through a different transport, or run multiple side by side."
          />
          <div className="grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-3">
            {TRANSPORTS.map((t) => (
              <GridTile key={t.name} {...t} />
            ))}
          </div>
          <div className="grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-3">
            {STORAGE.map((t) => (
              <GridTile key={t.name} {...t} />
            ))}
          </div>
        </div>
      </section>

      {/* 07 OBSERVABILITY VIA NITRO */}
      <section className="relative py-20 sm:py-24">
        <div className="flex flex-col gap-10">
          <SectionHeader
            index="07"
            eyebrow="Observability via Nitro"
            title="Every hop a span, end to end."
          />
          <div className="grid grid-cols-1 gap-10 lg:grid-cols-12">
            <div className="lg:col-span-5">
              <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
                Mocha emits structured OpenTelemetry traces for every publish,
                transport hop, and handler invocation. Correlation ids propagate
                across services automatically. The GraphQL IDE serves from the
                endpoint, telemetry requires Nitro configuration in your
                application.
              </p>
              <ul className="mt-6 flex flex-col gap-2.5">
                {[
                  "Spans on PublishAsync, transport.deliver, handler.invoke.",
                  "Correlation ids carried across process boundaries.",
                  "Point one OTLP exporter at Nitro to read the flow.",
                ].map((b) => (
                  <li
                    key={b}
                    className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
                  >
                    <span className="text-cc-accent mt-1 shrink-0">
                      <CheckIcon size={14} />
                    </span>
                    <span>{b}</span>
                  </li>
                ))}
              </ul>
            </div>
            <div className="lg:col-span-7">
              <TraceIllustration />
            </div>
          </div>
          <div aria-hidden className="bg-cc-accent/40 h-px w-full" />
        </div>
      </section>

      {/* 08 CLOSING CTA */}
      <section className="relative py-24 text-center sm:py-28">
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Ship the messages, keep the receipts.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Write a handler, attribute it, dispatch it. Outbox, inbox, sagas,
            and traces are part of the framework, not bolt-on packages you wire
            yourself.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/mocha">Read the docs</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              See the API
            </OutlineButton>
          </div>
          {/* The single brand-spectrum hairline on the page. */}
          <div
            aria-hidden
            className="mx-auto mt-12 h-px w-full max-w-3xl"
            style={{ background: SPECTRUM }}
          />
        </div>
      </section>
    </div>
  );
}
