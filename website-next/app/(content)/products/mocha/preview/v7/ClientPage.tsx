"use client";

import type { ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroTrace } from "@/src/nitro";

// Brand spectrum gradient, used exactly once on the page (closing CTA hairline).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const ACCENT = "#5eead4"; // cc-accent teal

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

// -----------------------------------------------------------------------------
// CENTERPIECE: Flight of a Message
//
// A single packet travels through the Mocha pipeline:
//   1. publisher emits PublishAsync
//   2. domain row + outbox row commit together (one tx)
//   3. dispatcher carries packet across transport
//   4. inbox dedupes on arrival
//   5. saga consumes, state advances Draft -> Checked -> Published
//
// Implementation:
//   - one master timeline driven by a stage index, incrementing on a setInterval
//   - the packet is a motion.circle whose cx/cy keyframes are scoped to the
//     active stage; transition timings match the strokeDashoffset on the path
//   - sub-pulses on origin / outbox / inbox / saga nodes use scale + opacity
//     keyframes keyed off the same stage
//   - useInView gates the looping interval so off-screen motion does not run
//   - useReducedMotion renders the static final frame and skips the timeline
// -----------------------------------------------------------------------------

const STAGES = [
  { id: "publish", label: "publish" },
  { id: "commit", label: "commit" },
  { id: "dispatch", label: "dispatch" },
  { id: "dedupe", label: "dedupe" },
  { id: "consume", label: "consume" },
] as const;

type Stage = (typeof STAGES)[number]["id"];

// Path segment endpoints in viewBox coords. The packet animates between these.
const POINTS: Record<Stage, { x: number; y: number }> = {
  publish: { x: 60, y: 90 },
  commit: { x: 170, y: 90 },
  dispatch: { x: 320, y: 90 },
  dedupe: { x: 470, y: 90 },
  consume: { x: 560, y: 90 },
};

function FlightOfAMessage() {
  const reduced = useReducedMotion();
  const containerRef = useRef<HTMLDivElement>(null);
  const inView = useInView(containerRef, { margin: "-15%" });
  const [stageIndex, setStageIndex] = useState(0);
  const [running, setRunning] = useState(true);

  useEffect(() => {
    if (reduced) {
      const t = window.setTimeout(() => setStageIndex(STAGES.length - 1), 0);
      return () => window.clearTimeout(t);
    }
    if (!running || !inView) {
      return;
    }
    const id = window.setInterval(() => {
      setStageIndex((i) => (i + 1) % STAGES.length);
    }, 1400);
    return () => window.clearInterval(id);
  }, [running, inView, reduced]);

  const stage = STAGES[stageIndex].id;
  const point = POINTS[stage];

  // Pulse state helpers.
  const isPublish = stage === "publish";
  const isCommit = stage === "commit";
  const isDispatch = stage === "dispatch";
  const isDedupe = stage === "dedupe";
  const isConsume = stage === "consume";

  const sagaState =
    stage === "consume"
      ? "Published"
      : stage === "dedupe"
        ? "Checked"
        : "Draft";

  const handleReplay = () => {
    if (reduced) return;
    setStageIndex(0);
    setRunning(true);
  };

  return (
    <div
      ref={containerRef}
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border"
    >
      {/* Soft accent wash behind the diagram. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-80"
        style={{
          background:
            "radial-gradient(520px 220px at 18% 30%, rgba(94, 234, 212, 0.16), transparent 70%), radial-gradient(420px 220px at 82% 70%, rgba(22, 185, 228, 0.12), transparent 70%)",
        }}
      />

      <div className="relative px-5 pt-4 pb-3 sm:px-6 sm:pt-5">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
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
            <span className="text-cc-ink-dim ml-2 font-mono text-[11px]">
              flight-of-a-message.mocha
            </span>
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={handleReplay}
              disabled={Boolean(reduced)}
              className="border-cc-card-border hover:border-cc-card-border-hover text-cc-ink-dim hover:text-cc-ink inline-flex h-7 items-center gap-1.5 rounded-full border px-2.5 font-mono text-[10.5px] tracking-wider uppercase transition-colors disabled:cursor-not-allowed disabled:opacity-50"
              aria-label="Replay flight animation"
            >
              <span aria-hidden>{"▶"}</span>
              replay
            </button>
          </div>
        </div>
      </div>

      <div className="relative px-3 pb-4 sm:px-5">
        <svg
          viewBox="0 0 620 200"
          className="h-auto w-full"
          role="img"
          aria-label="A single message packet travels from publisher through outbox, transport, and inbox into a saga, with each stage highlighted in turn."
        >
          {/* Process boundary A */}
          <rect
            x="12"
            y="36"
            width="240"
            height="124"
            rx="10"
            fill="rgba(245,241,234,0.03)"
            stroke="rgba(245,241,234,0.16)"
            strokeDasharray="4 4"
          />
          <text
            x="22"
            y="54"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.5)"
          >
            producer service
          </text>

          {/* Publisher node */}
          <motion.rect
            x="22"
            y="70"
            width="78"
            height="40"
            rx="8"
            fill="rgba(94,234,212,0.08)"
            stroke="rgba(94,234,212,0.55)"
            animate={
              reduced
                ? { opacity: 1, scale: 1 }
                : {
                    opacity: isPublish ? 1 : 0.75,
                    scale: isPublish ? 1.04 : 1,
                  }
            }
            transition={{ duration: 0.4, ease: "easeOut" }}
            style={{ transformOrigin: "61px 90px", transformBox: "fill-box" }}
          />
          <text
            x="61"
            y="86"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="#5eead4"
          >
            [Handler]
          </text>
          <text
            x="61"
            y="100"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.62)"
          >
            PublishAsync
          </text>

          {/* Outbox group (DB row + outbox row) */}
          <motion.g
            animate={
              reduced ? { opacity: 1 } : { opacity: isCommit ? 1 : 0.78 }
            }
            transition={{ duration: 0.4 }}
          >
            <motion.rect
              x="130"
              y="62"
              width="80"
              height="22"
              rx="5"
              fill="rgba(245,241,234,0.04)"
              stroke={isCommit ? ACCENT : "rgba(245,241,234,0.22)"}
              animate={
                reduced ? { opacity: 1 } : { opacity: isCommit ? 1 : 0.85 }
              }
              transition={{ duration: 0.35 }}
            />
            <text
              x="170"
              y="76"
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="10"
              fill="#f5f0ea"
            >
              Reviews row
            </text>

            <motion.rect
              x="130"
              y="96"
              width="80"
              height="22"
              rx="5"
              fill="rgba(94,234,212,0.08)"
              stroke={ACCENT}
              animate={reduced ? { scale: 1 } : { scale: isCommit ? 1.05 : 1 }}
              transition={{ duration: 0.4, ease: "easeOut" }}
              style={{
                transformOrigin: "170px 107px",
                transformBox: "fill-box",
              }}
            />
            <text
              x="170"
              y="110"
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="10"
              fill="#5eead4"
            >
              outbox
            </text>

            {/* one-tx bracket */}
            <path
              d="M 120 60 L 116 60 L 116 120 L 120 120"
              stroke="rgba(94,234,212,0.55)"
              strokeWidth="1.2"
              fill="none"
            />
            <text
              x="112"
              y="93"
              textAnchor="end"
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill="rgba(94,234,212,0.85)"
            >
              tx
            </text>
          </motion.g>

          {/* Transport / dispatcher node */}
          <motion.rect
            x="280"
            y="70"
            width="80"
            height="40"
            rx="8"
            fill="rgba(22,185,228,0.08)"
            stroke="rgba(22,185,228,0.55)"
            animate={
              reduced
                ? { opacity: 1, scale: 1 }
                : {
                    opacity: isDispatch ? 1 : 0.7,
                    scale: isDispatch ? 1.04 : 1,
                  }
            }
            transition={{ duration: 0.4, ease: "easeOut" }}
            style={{ transformOrigin: "320px 90px", transformBox: "fill-box" }}
          />
          <text
            x="320"
            y="86"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="#16b9e4"
          >
            transport
          </text>
          <text
            x="320"
            y="100"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.62)"
          >
            rabbit / pg
          </text>

          {/* Process boundary B */}
          <rect
            x="388"
            y="36"
            width="220"
            height="124"
            rx="10"
            fill="rgba(245,241,234,0.03)"
            stroke="rgba(245,241,234,0.16)"
            strokeDasharray="4 4"
          />
          <text
            x="398"
            y="54"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.5)"
          >
            consumer service
          </text>

          {/* Inbox node */}
          <motion.rect
            x="430"
            y="70"
            width="80"
            height="40"
            rx="8"
            fill="rgba(94,234,212,0.08)"
            stroke="rgba(94,234,212,0.55)"
            animate={
              reduced
                ? { opacity: 1, scale: 1 }
                : {
                    opacity: isDedupe ? 1 : 0.75,
                    scale: isDedupe ? 1.04 : 1,
                  }
            }
            transition={{ duration: 0.4, ease: "easeOut" }}
            style={{ transformOrigin: "470px 90px", transformBox: "fill-box" }}
          />
          <text
            x="470"
            y="86"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="#5eead4"
          >
            inbox
          </text>
          <text
            x="470"
            y="100"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.62)"
          >
            dedupe
          </text>

          {/* Saga node */}
          <motion.rect
            x="528"
            y="70"
            width="68"
            height="40"
            rx="8"
            fill="rgba(94,234,212,0.08)"
            stroke="rgba(94,234,212,0.55)"
            animate={
              reduced
                ? { opacity: 1, scale: 1 }
                : {
                    opacity: isConsume ? 1 : 0.7,
                    scale: isConsume ? 1.05 : 1,
                  }
            }
            transition={{ duration: 0.4, ease: "easeOut" }}
            style={{ transformOrigin: "562px 90px", transformBox: "fill-box" }}
          />
          <text
            x="562"
            y="86"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="#5eead4"
          >
            saga
          </text>
          <motion.text
            x="562"
            y="100"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.75)"
            animate={reduced ? { opacity: 1 } : { opacity: 1 }}
            key={sagaState}
          >
            {sagaState}
          </motion.text>

          {/* Connecting path */}
          <path
            d="M 100 90 L 130 90 M 210 90 L 280 90 M 360 90 L 430 90 M 510 90 L 528 90"
            stroke="rgba(245,241,234,0.22)"
            strokeWidth="1.4"
            fill="none"
            strokeDasharray="3 4"
          />

          {/* The packet itself */}
          <motion.g
            animate={
              reduced
                ? { x: point.x - 60, y: point.y - 90 }
                : { x: point.x - 60, y: point.y - 90 }
            }
            transition={{
              duration: 0.9,
              ease: [0.4, 0, 0.2, 1],
            }}
          >
            <motion.circle
              cx="60"
              cy="90"
              r="7"
              fill="#5eead4"
              animate={
                reduced
                  ? { opacity: 1, scale: 1 }
                  : { opacity: [0.9, 1, 0.9], scale: [1, 1.1, 1] }
              }
              transition={
                reduced
                  ? { duration: 0 }
                  : { duration: 1.2, repeat: Infinity, ease: "easeInOut" }
              }
            />
            <circle
              cx="60"
              cy="90"
              r="13"
              fill="none"
              stroke="rgba(94,234,212,0.4)"
              strokeWidth="1.2"
            />
          </motion.g>

          {/* Stage label under packet */}
          <text
            x="310"
            y="186"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.7)"
          >
            stage: <tspan fill={ACCENT}>{STAGES[stageIndex].label}</tspan>
            {"   "}
            <tspan fill="rgba(245,241,234,0.45)">
              {`(${stageIndex + 1}/${STAGES.length})`}
            </tspan>
          </text>
        </svg>

        {/* Legend strip */}
        <ul className="border-cc-card-border mt-3 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 border-t pt-3 font-mono text-[10.5px] tracking-wider uppercase">
          {STAGES.map((s, i) => (
            <li
              key={s.id}
              className="flex items-center gap-1.5"
              style={{
                color: i === stageIndex ? "#5eead4" : "rgba(245,241,234,0.45)",
              }}
            >
              <span
                aria-hidden
                className="inline-block h-1.5 w-1.5 rounded-full"
                style={{
                  background:
                    i === stageIndex ? "#5eead4" : "rgba(245,241,234,0.35)",
                }}
              />
              {s.label}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Source-generated dispatch animation: [Handler] tokens fly into Roslyn,
// pre-compiled delegate emits out. Runs once on whileInView.
// -----------------------------------------------------------------------------

function SourceGenAnimation() {
  const reduced = useReducedMotion();
  const handlers = [
    { y: 32, label: "[Handler] CreateReview" },
    { y: 76, label: "[Handler] ReviewCreated" },
    { y: 120, label: "[Saga] PublishFlow" },
  ];
  return (
    <motion.svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="A Roslyn source generator collects handlers at build time and emits a pre-compiled pipeline."
      initial={reduced ? false : "hidden"}
      whileInView={reduced ? undefined : "visible"}
      viewport={{ once: true, margin: "-15%" }}
    >
      {handlers.map((h, i) => (
        <motion.g
          key={h.label}
          variants={{
            hidden: { opacity: 0, x: -20 },
            visible: { opacity: 1, x: 0 },
          }}
          transition={{ duration: 0.5, delay: 0.1 + i * 0.15 }}
        >
          <rect
            x="12"
            y={h.y}
            width="190"
            height="34"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.4)"
          />
          <text
            x="24"
            y={h.y + 21}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            {h.label}
          </text>
        </motion.g>
      ))}
      {handlers.map((h, i) => (
        <motion.path
          key={`p-${h.label}`}
          d={`M 202 ${h.y + 17} Q 244 ${h.y + 17}, 280 110`}
          stroke="rgba(94,234,212,0.4)"
          strokeWidth="1.2"
          fill="none"
          variants={{
            hidden: { pathLength: 0, opacity: 0 },
            visible: { pathLength: 1, opacity: 1 },
          }}
          transition={{ duration: 0.6, delay: 0.25 + i * 0.15 }}
        />
      ))}
      <motion.g
        variants={{
          hidden: { opacity: 0, scale: 0.92 },
          visible: { opacity: 1, scale: 1 },
        }}
        transition={{ duration: 0.5, delay: 0.85 }}
        style={{ transformOrigin: "330px 110px", transformBox: "fill-box" }}
      >
        <rect
          x="280"
          y="86"
          width="100"
          height="48"
          rx="8"
          fill="rgba(94,234,212,0.08)"
          stroke="rgba(94,234,212,0.55)"
        />
        <text
          x="330"
          y="106"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill="#5eead4"
        >
          Roslyn
        </text>
        <text
          x="330"
          y="122"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.62)"
        >
          compile time
        </text>
      </motion.g>
      <motion.path
        d="M 380 110 L 412 110"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.4"
        fill="none"
        variants={{
          hidden: { pathLength: 0 },
          visible: { pathLength: 1 },
        }}
        transition={{ duration: 0.35, delay: 1.15 }}
      />
      <motion.polygon
        points="412,106 424,110 412,114"
        fill="rgba(94,234,212,0.7)"
        variants={{
          hidden: { opacity: 0 },
          visible: { opacity: 1 },
        }}
        transition={{ duration: 0.25, delay: 1.4 }}
      />
      <motion.g
        variants={{
          hidden: { opacity: 0, y: 10 },
          visible: { opacity: 1, y: 0 },
        }}
        transition={{ duration: 0.4, delay: 1.45 }}
      >
        <rect
          x="380"
          y="158"
          width="88"
          height="40"
          rx="6"
          fill="rgba(245,241,234,0.04)"
          stroke="rgba(245,241,234,0.22)"
        />
        <text
          x="424"
          y="174"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.7)"
        >
          AddReviews()
        </text>
        <text
          x="424"
          y="188"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="9.5"
          fill="rgba(245,241,234,0.55)"
        >
          pre-compiled delegate
        </text>
      </motion.g>
      <text
        x="12"
        y="200"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        zero reflection, zero MakeGenericType at runtime
      </text>
    </motion.svg>
  );
}

// -----------------------------------------------------------------------------
// Pluggable transports: packet moves from IEventBus center to the active row.
// -----------------------------------------------------------------------------

const TRANSPORTS = [
  { y: 26, name: "RabbitMQ", note: "topic + queue routing" },
  { y: 64, name: "PostgreSQL", note: "durable + outbox" },
  { y: 102, name: "in-process", note: "same handler shape" },
  { y: 140, name: "Kafka", note: "partitioned log" },
  { y: 178, name: "Azure SB / EH", note: "managed Azure" },
] as const;

function TransportsAnimation() {
  const reduced = useReducedMotion();
  const containerRef = useRef<SVGSVGElement>(null);
  const inView = useInView(containerRef, { margin: "-15%" });
  const [active, setActive] = useState(reduced ? 0 : 0);

  useEffect(() => {
    if (reduced || !inView) return;
    const id = window.setInterval(() => {
      setActive((i) => (i + 1) % TRANSPORTS.length);
    }, 1500);
    return () => window.clearInterval(id);
  }, [reduced, inView]);

  const target = TRANSPORTS[active];

  return (
    <svg
      ref={containerRef}
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="One IEventBus dispatches to whichever transport you register, swap brokers without changing handlers."
    >
      <rect
        x="320"
        y="84"
        width="144"
        height="52"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="392"
        y="104"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        IEventBus
      </text>
      <text
        x="392"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        PublishAsync, SendAsync
      </text>

      {TRANSPORTS.map((t, i) => {
        const isActive = i === active;
        return (
          <g key={t.name}>
            <motion.rect
              x="12"
              y={t.y}
              width="200"
              height="26"
              rx="5"
              fill="rgba(245,241,234,0.04)"
              stroke={isActive ? ACCENT : "rgba(245,241,234,0.18)"}
              animate={
                reduced ? { opacity: 1 } : { opacity: isActive ? 1 : 0.7 }
              }
              transition={{ duration: 0.35 }}
            />
            <text
              x="24"
              y={t.y + 16}
              fontFamily="ui-monospace, monospace"
              fontSize="10.5"
              fill={isActive ? "#5eead4" : "#f5f0ea"}
            >
              {t.name}
            </text>
            <text
              x="208"
              y={t.y + 16}
              textAnchor="end"
              fontFamily="ui-monospace, monospace"
              fontSize="9.5"
              fill="rgba(245,241,234,0.55)"
            >
              {t.note}
            </text>
            <path
              d={`M 212 ${t.y + 13} C 270 ${t.y + 13}, 270 110, 320 110`}
              stroke={
                isActive ? "rgba(94,234,212,0.55)" : "rgba(94,234,212,0.18)"
              }
              strokeWidth={isActive ? 1.4 : 1}
              fill="none"
            />
          </g>
        );
      })}

      {/* Moving packet: starts at IEventBus, settles on active row endpoint. */}
      <motion.circle
        r="5"
        fill="#5eead4"
        initial={false}
        animate={
          reduced
            ? { cx: 220, cy: target.y + 13, opacity: 1 }
            : { cx: 220, cy: target.y + 13, opacity: [0, 1, 1, 0] }
        }
        transition={
          reduced
            ? { duration: 0 }
            : { duration: 1.4, ease: "easeInOut", times: [0, 0.3, 0.85, 1] }
        }
      />
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Validated saga state machine animation: Draft -> Checked -> Published.
// -----------------------------------------------------------------------------

const SAGA_STATES = [
  { x: 28, label: "Draft" },
  { x: 192, label: "Checked" },
  { x: 356, label: "Published" },
] as const;

function SagaAnimation() {
  const reduced = useReducedMotion();
  const ref = useRef<SVGSVGElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15%" });

  const [active, setActive] = useState(0);

  useEffect(() => {
    if (reduced) {
      const t = window.setTimeout(() => setActive(SAGA_STATES.length - 1), 0);
      return () => window.clearTimeout(t);
    }
    if (!inView) return;
    let i = 0;
    const id = window.setInterval(() => {
      i += 1;
      if (i >= SAGA_STATES.length) {
        window.clearInterval(id);
        return;
      }
      setActive(i);
    }, 900);
    return () => window.clearInterval(id);
  }, [inView, reduced]);

  return (
    <svg
      ref={ref}
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Saga states Draft, Checked, Published animate forward, with a validated-at-startup badge revealing on completion."
    >
      {SAGA_STATES.map((s, i) => {
        const isActive = i === active;
        return (
          <g key={s.label}>
            <motion.rect
              x={s.x}
              y="86"
              width="100"
              height="48"
              rx="10"
              fill={
                isActive ? "rgba(94,234,212,0.10)" : "rgba(245,241,234,0.04)"
              }
              stroke={isActive ? ACCENT : "rgba(245,241,234,0.22)"}
              animate={reduced ? { scale: 1 } : { scale: isActive ? 1.04 : 1 }}
              transition={{ duration: 0.4, ease: "easeOut" }}
              style={{
                transformOrigin: `${s.x + 50}px 110px`,
                transformBox: "fill-box",
              }}
            />
            {isActive && (
              <motion.rect
                x={s.x - 4}
                y="82"
                width="108"
                height="56"
                rx="12"
                fill="none"
                stroke="rgba(94,234,212,0.55)"
                strokeWidth="1.2"
                initial={reduced ? false : { opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 0.3 }}
              />
            )}
            <text
              x={s.x + 50}
              y="116"
              textAnchor="middle"
              fontFamily="var(--font-body)"
              fontSize="13"
              fill="#f5f0ea"
            >
              {s.label}
            </text>
            {i < SAGA_STATES.length - 1 && (
              <g>
                <path
                  d={`M ${s.x + 100} 110 L ${s.x + 188} 110`}
                  stroke="rgba(94,234,212,0.45)"
                  strokeWidth="1.4"
                  fill="none"
                />
                <polygon
                  points={`${s.x + 188},106 ${s.x + 196},110 ${s.x + 188},114`}
                  fill="rgba(94,234,212,0.65)"
                />
              </g>
            )}
          </g>
        );
      })}
      <text
        x="78"
        y="78"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        ReviewCreated
      </text>
      <text
        x="242"
        y="78"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        ContentChecked
      </text>

      <motion.g
        initial={reduced ? false : { opacity: 0, y: 8 }}
        animate={
          reduced
            ? { opacity: 1, y: 0 }
            : active === SAGA_STATES.length - 1
              ? { opacity: 1, y: 0 }
              : { opacity: 0, y: 8 }
        }
        transition={{ duration: 0.4, delay: 0.1 }}
      >
        <rect
          x="28"
          y="172"
          width="428"
          height="32"
          rx="6"
          fill="rgba(245,241,234,0.04)"
          stroke="rgba(94,234,212,0.4)"
        />
        <text
          x="44"
          y="192"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill="#5eead4"
        >
          validated at startup, before the service handles traffic
        </text>
      </motion.g>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Outbox + inbox two-frame animation: tx pulse, then dispatch, then dedupe.
// -----------------------------------------------------------------------------

function OutboxInboxAnimation() {
  const reduced = useReducedMotion();
  const ref = useRef<SVGSVGElement>(null);
  const inView = useInView(ref, { margin: "-15%" });
  const [frame, setFrame] = useState(reduced ? 2 : 0);

  useEffect(() => {
    if (reduced || !inView) return;
    const id = window.setInterval(() => {
      setFrame((f) => (f + 1) % 3);
    }, 1500);
    return () => window.clearInterval(id);
  }, [inView, reduced]);

  const txPulse = frame === 0;
  const inTransit = frame === 1;
  const dedupe = frame === 2;

  return (
    <svg
      ref={ref}
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="A domain row and outbox row commit together in one transaction, the dispatcher carries the message, the inbox flashes dedupe on the second arrival."
    >
      {/* DB tx */}
      <motion.rect
        x="12"
        y="24"
        width="180"
        height="172"
        rx="10"
        fill="rgba(245,241,234,0.03)"
        stroke={txPulse ? ACCENT : "rgba(245,241,234,0.18)"}
        strokeDasharray="4 4"
        animate={reduced ? { opacity: 1 } : { opacity: txPulse ? 1 : 0.85 }}
        transition={{ duration: 0.4 }}
      />
      <text
        x="22"
        y="42"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        one DB transaction
      </text>
      <motion.rect
        x="28"
        y="58"
        width="148"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.22)"
        animate={reduced ? { scale: 1 } : { scale: txPulse ? 1.02 : 1 }}
        transition={{ duration: 0.4 }}
        style={{ transformOrigin: "102px 76px", transformBox: "fill-box" }}
      />
      <text
        x="40"
        y="80"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        Reviews row
      </text>
      <motion.rect
        x="28"
        y="108"
        width="148"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke={ACCENT}
        animate={reduced ? { scale: 1 } : { scale: txPulse ? 1.05 : 1 }}
        transition={{ duration: 0.4 }}
        style={{ transformOrigin: "102px 126px", transformBox: "fill-box" }}
      />
      <text
        x="40"
        y="130"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        outbox: ReviewCreated
      </text>
      <text
        x="28"
        y="170"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        commit together, or not at all
      </text>

      <path
        d="M 192 124 L 240 124"
        stroke="rgba(94,234,212,0.45)"
        strokeWidth="1.4"
        fill="none"
      />
      <motion.rect
        x="240"
        y="106"
        width="68"
        height="36"
        rx="6"
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.55)"
        animate={reduced ? { opacity: 1 } : { opacity: inTransit ? 1 : 0.7 }}
        transition={{ duration: 0.3 }}
      />
      <text
        x="274"
        y="128"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#16b9e4"
      >
        dispatcher
      </text>
      <path
        d="M 308 124 L 348 124"
        stroke="rgba(22,185,228,0.45)"
        strokeWidth="1.4"
        fill="none"
      />

      {/* Moving packet */}
      <motion.circle
        r="5"
        fill="#5eead4"
        animate={
          reduced
            ? { cx: 408, cy: 76, opacity: 1 }
            : inTransit
              ? { cx: [192, 348], cy: [124, 124], opacity: [0, 1, 1] }
              : { cx: 348, cy: 124, opacity: 0 }
        }
        transition={{ duration: 0.9, ease: "easeInOut" }}
      />

      {/* Inbox */}
      <rect
        x="348"
        y="24"
        width="120"
        height="172"
        rx="10"
        fill="rgba(245,241,234,0.03)"
        stroke="rgba(245,241,234,0.16)"
        strokeDasharray="4 4"
      />
      <text
        x="358"
        y="42"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        consumer
      </text>
      <motion.rect
        x="360"
        y="58"
        width="96"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke={dedupe ? ACCENT : "rgba(94,234,212,0.4)"}
        animate={reduced ? { scale: 1 } : { scale: dedupe ? 1.05 : 1 }}
        transition={{ duration: 0.4 }}
        style={{ transformOrigin: "408px 76px", transformBox: "fill-box" }}
      />
      <text
        x="408"
        y="80"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        inbox dedupe
      </text>
      <rect
        x="360"
        y="108"
        width="96"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.18)"
      />
      <text
        x="408"
        y="130"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        handler
      </text>
      <motion.text
        x="358"
        y="170"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#5eead4"
        animate={reduced ? { opacity: 1 } : { opacity: dedupe ? 1 : 0.55 }}
        transition={{ duration: 0.3 }}
      >
        exactly-once processing
      </motion.text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Number tick-up, used in the observability strip and the proof band.
// -----------------------------------------------------------------------------

interface CountUpProps {
  readonly to: number;
  readonly suffix?: string;
  readonly format?: (n: number) => string;
  readonly duration?: number;
}

function CountUp({ to, suffix = "", format, duration = 1.4 }: CountUpProps) {
  const reduced = useReducedMotion();
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, margin: "-20%" });
  const [value, setValue] = useState(reduced ? to : 0);

  useEffect(() => {
    if (reduced || !inView) return;
    const controls = animate(0, to, {
      duration,
      ease: "easeOut",
      onUpdate: (v) => setValue(v),
    });
    return () => controls.stop();
  }, [to, inView, reduced, duration]);

  const text = format ? format(value) : Math.round(value).toLocaleString();
  return (
    <span ref={ref}>
      {text}
      {suffix}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Section reveal wrapper. Section copy fades + slides up once in view.
// -----------------------------------------------------------------------------

interface RevealProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly delay?: number;
}

function Reveal({ children, className, delay = 0 }: RevealProps) {
  return (
    <motion.div
      className={className}
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-15%" }}
      transition={{ duration: 0.55, ease: "easeOut", delay }}
    >
      {children}
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// Feature row
// -----------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
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
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <Reveal
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
          <ul className="mt-6 flex flex-col gap-2.5">
            {bullets.map((b) => (
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
        </Reveal>
        <Reveal
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
          delay={0.05}
        >
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
            {visual}
          </div>
        </Reveal>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Proof tile with quiet number tick-up.
// -----------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: ReactNode;
}

function ProofItem({ label, value }: ProofItemProps) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        {value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Capability strip with staggered reveal.
// -----------------------------------------------------------------------------

const CAPABILITIES = [
  "Mediator + bus",
  "Source-generated",
  "Pluggable transports",
  "Validated sagas",
  "Outbox + inbox",
  "Every hop a span",
] as const;

function CapabilityStrip() {
  return (
    <section
      aria-label="Capabilities at a glance"
      className="border-cc-card-border border-y py-6"
    >
      <motion.ul
        className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-6"
        initial="hidden"
        whileInView="visible"
        viewport={{ once: true, margin: "-15%" }}
        variants={{
          hidden: {},
          visible: { transition: { staggerChildren: 0.06 } },
        }}
      >
        {CAPABILITIES.map((label) => (
          <motion.li
            key={label}
            className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
            variants={{
              hidden: { opacity: 0, y: 6 },
              visible: { opacity: 1, y: 0 },
            }}
            transition={{ duration: 0.35, ease: "easeOut" }}
          >
            <span className="text-cc-accent" aria-hidden>
              <CheckIcon size={12} />
            </span>
            {label}
          </motion.li>
        ))}
      </motion.ul>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      {/* HERO: copy left, FlightOfAMessage centerpiece right. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>Mocha messaging .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Watch one message fly the whole pipeline.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Mocha is the open-source .NET messaging framework: a
              source-generated mediator and message bus with validated sagas, a
              transactional outbox, an idempotent inbox, and a span on every
              hop. The animation on the right follows a single packet from
              publish to consume, the same flight your services run in
              production.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/mocha">Get Started</SolidButton>
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
                  Runtime
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">
                  .NET / ASP.NET Core
                </dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Dispatch
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">Source-generated</dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-7">
            <FlightOfAMessage />
          </div>
        </div>
      </section>

      <CapabilityStrip />

      <FeatureRow
        id="source-generated-dispatch"
        index="01"
        eyebrow="Source-generated dispatch"
        title="A Roslyn source generator wires every handler at compile time."
        body="The Mocha analyzer discovers handlers, consumers, and sagas across your assemblies and emits typed registration plus pre-compiled pipeline delegates. No MakeGenericType, no service-provider lookups on the hot path, no reflection at runtime. The pipeline you ship is the pipeline the compiler built."
        bullets={[
          "Typed AddReviews()-style registration emitted per assembly, no manual wiring.",
          "Middleware composed at build time into a delegate per handler.",
          "AOT-friendly: no runtime emit, no dynamic code, no MakeGenericType.",
        ]}
        visual={<SourceGenAnimation />}
      />

      <FeatureRow
        id="transports"
        index="02"
        eyebrow="Pluggable transports"
        title="Pick the broker, swap brokers, run more than one at once."
        body="RabbitMQ, PostgreSQL, and in-process ship as first-class transports. Kafka, Azure Service Bus, and Azure Event Hub ship in source. Register a default transport, route specific message types through a different one, or run multiple transports side by side. The handlers do not change when the transport does."
        bullets={[
          "RabbitMQ for topic and queue routing, Postgres for durable + outbox in one store.",
          "In-process transport for local development and tests, same code as production.",
          "Per-message routing rules let high-volume topics use a different broker.",
        ]}
        visual={<TransportsAnimation />}
        reverse
      />

      <FeatureRow
        id="sagas"
        index="03"
        eyebrow="Validated sagas"
        title="Sagas are checked before the service handles its first request."
        body="Define a state machine: states, triggers, transitions, compensations. At startup Mocha validates that every state is reachable, every path leads to a final state, and every trigger you handle is one the saga can receive. A saga that would get stuck or drop a message never gets past startup."
        bullets={[
          "Draft, Checked, Published, with compensation paths on failure.",
          "Persisted state across hops, scoped to a correlation key.",
          "Validated before the service handles traffic, never silently broken in prod.",
        ]}
        visual={<SagaAnimation />}
      />

      <FeatureRow
        id="reliability"
        index="04"
        eyebrow="Outbox + inbox"
        title="Exactly-once processing, the boring way: outbox plus idempotent inbox."
        body="The transactional outbox commits your domain change and the message to dispatch in the same database transaction, so a crash never loses messages. On the receive side, an idempotent inbox records the message id and skips duplicates so each message is processed exactly once, even when the broker redelivers."
        bullets={[
          "Transactional outbox on Postgres (and EF Core), wired through your DbContext.",
          "Idempotent inbox dedupes on the consumer side without extra application code.",
          "Per-exception retry, redelivery, dead-letter, circuit breaker, concurrency limiter.",
        ]}
        visual={<OutboxInboxAnimation />}
        reverse
      />

      <FeatureRow
        id="observability"
        index="05"
        eyebrow="Every hop a span"
        title="OpenTelemetry-native, every publish and consume is a real span."
        body="Mocha emits structured OpenTelemetry traces and metrics for every dispatch, send, and handler execution, with correlation propagated across service boundaries. The same trace shows the publisher, the transport hop, and the consumer. Configure Nitro to land those spans in your existing backend."
        bullets={[
          "Every PublishAsync, transport hop, and handler invocation is a span.",
          "Correlation ids propagate across services automatically.",
          "Configure one OTLP exporter, see the flow end to end in your existing backend.",
        ]}
        visual={
          <div className="flex flex-col gap-4">
            <div className="bg-cc-surface relative overflow-hidden rounded-lg">
              <NitroTrace />
            </div>
            <div className="border-cc-card-border flex flex-wrap items-baseline justify-between gap-3 border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
                spans / second observed
              </span>
              <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight tabular-nums">
                <CountUp to={1284} />
              </span>
            </div>
          </div>
        }
      />

      {/* MIT / open source proof band with quiet tick-up. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <Reveal className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source. Free to use. Built in the open.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Mocha is released under the MIT license. Use it in commercial
              work, fork it, vendor it, audit it. The codebase, the issue
              tracker, the roadmap, and the release notes all live on GitHub
              alongside the rest of the ChilliCream platform.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/mocha">Read the docs</OutlineButton>
            </div>
          </Reveal>
          <Reveal className="lg:col-span-5" delay={0.05}>
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Runtime" value=".NET / ASP.NET Core" />
              <ProofItem label="Dispatch" value="Source-generated" />
              <ProofItem label="Transports" value="Rabbit / PG / mem" />
              <ProofItem label="Reliability" value="Outbox + inbox" />
              <ProofItem label="Tracing" value="OpenTelemetry" />
            </div>
          </Reveal>
        </div>
      </section>

      {/* Closing CTA. The single brand-spectrum hairline lives here. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <Reveal className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Stop choosing between a mediator and a bus.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Write a handler, attribute it, dispatch it. The source generator
            handles registration and the pipeline. The transport, the outbox,
            the inbox, the sagas, and the traces are part of the framework, not
            bolt-on packages you wire yourself.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/mocha">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </Reveal>
      </section>
    </MotionConfig>
  );
}
