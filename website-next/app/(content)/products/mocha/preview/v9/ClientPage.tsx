"use client";

import type { CSSProperties, ReactNode } from "react";
import { MotionConfig, motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * Tilt: a pinball table for messages.
 *
 * Mocha routes messages, so the page becomes a pinball table that
 * routes the reader. One representative command (CreateReview) is
 * plunged in the hero and ricochets down the page along static SVG
 * angled arrows that bounce the eye between left- and right-anchored
 * content slabs. Each bumper it strikes is a stage of Mocha's
 * pipeline: the source-generated mediator, the bus handoff, the
 * outbox flush, the saga validated before traffic, the exactly-once
 * processing latch, and finally the telemetry drain.
 *
 * Mono labels next to each arrow read like flipper-board callouts
 * (PLUNGE, MEDIATE, BUS, OUTBOX, SAGA, ONCE, TRACE). The whole page
 * uses ONE accent (coral), overridden locally so every cc-accent
 * utility resolves to coral. The brand spectrum appears exactly once,
 * on the closing CTA rule.
 * ------------------------------------------------------------------ */

const CORAL = "#f0786a";
const CYAN = "#16b9e4";
const VIOLET = "#7c92c6";

const SPECTRUM = `linear-gradient(90deg, ${CYAN} 0%, ${VIOLET} 50%, ${CORAL} 100%)`;

const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1];

// Scope the page accent to coral. Every cc-accent utility inside the
// wrapper resolves to coral without touching the global teal token.
const ACCENT_SCOPE = {
  "--color-cc-accent": CORAL,
  "--color-cc-accent-hover": "#f59389",
} as CSSProperties;

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user" transition={{ ease: EASE }}>
      <div className="relative isolate" style={ACCENT_SCOPE}>
        <DotGrid />
        {/* The center rail the arrows hang off, full page height. */}
        <div
          aria-hidden
          className="border-cc-card-border pointer-events-none absolute inset-y-0 left-1/2 hidden -translate-x-1/2 border-l border-dashed lg:block"
        />
        <main className="relative mx-auto max-w-6xl">
          <Hero />
          <Bumper
            index="01"
            label="MEDIATE"
            entry="right"
            eyebrow="Source-generated mediator"
            title="The ball strikes a mediator the compiler already wired."
            body="ISender.Send(CreateReview) lands on a [Handler] method. A Roslyn source generator discovered it at compile time and emitted typed registration plus a pre-compiled pipeline delegate, so dispatch is a direct call, not a reflective lookup on the hot path."
            bullets={[
              "Zero-reflection dispatch, no MakeGenericType at runtime.",
              "AOT-friendly: no runtime emit, no dynamic code.",
              "No marker interfaces to implement, just attribute the method.",
            ]}
            visual={<MediateVisual />}
          />
          <Bumper
            index="02"
            label="BUS"
            entry="left"
            eyebrow="Cross-service message bus"
            title="It ricochets onto the bus and crosses a transport."
            body="The same handler shape that powers an in-process call powers a cross-service consumer. PublishAsync and SendAsync read identically whether the message stays in-process or rides RabbitMQ, Azure Service Bus, or Kafka to another service."
            bullets={[
              "Send and Publish ergonomics are identical in-process and over the wire.",
              "Register a default transport, route specific message types elsewhere.",
              "Run more than one transport side by side without changing handlers.",
            ]}
            visual={<BusVisual />}
          />
          <Bumper
            index="03"
            label="OUTBOX"
            entry="right"
            eyebrow="Transactional outbox"
            title="It drops into the outbox, committed with the row."
            body="The transactional outbox writes your domain change and the message to dispatch in one database transaction, so a crash never loses messages. A background dispatcher flushes outbox rows into the bus, giving at-least-once shipping into the transport."
            bullets={[
              "Domain row and outbox row commit together, or not at all.",
              "Dispatcher flushes durable rows into the bus after commit.",
              "At-least-once shipping into the transport, paired with the inbox downstream.",
            ]}
            visual={<OutboxVisual />}
          />
          <Bumper
            index="04"
            label="SAGA"
            entry="left"
            eyebrow="Validated sagas"
            title="It rolls through a saga checked before any traffic."
            body="Define a state machine: states, transitions, compensations. At startup Mocha validates the state shape, checks transitions are exhaustive, and confirms compensations are declared. A saga that would get stuck or drop a message never gets past startup. Validated before traffic, not at compile time."
            bullets={[
              "State shape validated at startup.",
              "Transitions checked exhaustive across the machine.",
              "Compensations declared for the failure paths.",
            ]}
            visual={<SagaVisual />}
          />
          <Bumper
            index="05"
            label="ONCE"
            entry="right"
            eyebrow="Exactly-once processing"
            title="It hits the latch: each message processed once."
            body="On the receive side an idempotent inbox records the message id and skips duplicates, so each message is processed exactly once even when the broker redelivers. This is exactly-once processing, not exactly-once delivery: the broker may hand you the same message twice, the inbox makes the handler run once."
            bullets={[
              "Inbox records the message id and skips a redelivered duplicate.",
              "Exactly-once processing on the consumer, no extra application code.",
              "Delivery can repeat, the handler effect happens once.",
            ]}
            visual={<OnceVisual />}
          />
          <Bumper
            index="06"
            label="TRACE"
            entry="left"
            eyebrow="OpenTelemetry drain"
            title="It drains into telemetry, the whole path on one trace."
            body="Mocha emits OpenTelemetry spans for every dispatch, transport hop, and handler execution, with correlation propagated across service boundaries. The same trace shows the publisher, the transport hop, and the consumer. Telemetry needs Nitro configuration to land in a backend you can read."
            bullets={[
              "Every publish, transport hop, and handler invocation is a span.",
              "Correlation ids propagate across services automatically.",
              "Telemetry needs Nitro configuration, then the flow reads end to end.",
            ]}
            visual={<TraceVisual />}
          />
          <Drain />
        </main>
      </div>
    </MotionConfig>
  );
}

/* ================================================================== *
 * Background: faint static dot grid over the cc-bg surface.
 * ================================================================== */

function DotGrid() {
  return (
    <div
      aria-hidden
      className="pointer-events-none fixed inset-0 -z-10"
      style={{
        backgroundImage:
          "radial-gradient(rgba(245,241,234,0.06) 1px, transparent 1px)",
        backgroundSize: "8px 8px",
      }}
    />
  );
}

/* ================================================================== *
 * Playfield arrow. A static angled connector that lives in the center
 * rail gutter. On first enter-view it draws its polyline (pathLength
 * 0 -> 1) and fades the mono label, then stays static. Reduced motion
 * snaps to the final state with no transition.
 * ================================================================== */

interface ArrowProps {
  readonly id: string;
  readonly label: string;
  // "left" means the ball exits down-left into a left-entry bumper;
  // "right" is the mirror.
  readonly direction: "left" | "right";
}

function PlayfieldArrow({ id, label, direction }: ArrowProps) {
  const reduced = useReducedMotion();
  const headId = `${id}-head`;

  // Diagonal from the previous bumper exit (top) to this bumper entry (bottom).
  const startX = direction === "left" ? 132 : 28;
  const endX = direction === "left" ? 28 : 132;
  const d = `M ${startX} 6 L ${startX} 40 L ${endX} 92 L ${endX} 128`;

  return (
    <div className="relative hidden h-[150px] w-full justify-center lg:flex">
      <svg
        viewBox="0 0 160 150"
        className="h-full w-[160px] overflow-visible"
        aria-hidden
      >
        <defs>
          <marker
            id={headId}
            viewBox="0 0 10 10"
            refX="6"
            refY="5"
            markerWidth="7"
            markerHeight="7"
            orient="auto-start-reverse"
          >
            <path d="M 0 0 L 10 5 L 0 10 z" fill={CORAL} />
          </marker>
        </defs>
        <motion.path
          d={d}
          fill="none"
          stroke={CORAL}
          strokeWidth="1.6"
          strokeLinejoin="round"
          markerEnd={`url(#${headId})`}
          initial={reduced ? { pathLength: 1 } : { pathLength: 0 }}
          whileInView={{ pathLength: 1 }}
          viewport={{ once: true, margin: "-40px" }}
          transition={reduced ? { duration: 0 } : { duration: 0.6, ease: EASE }}
        />
      </svg>
      <motion.span
        className="text-cc-accent absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 font-mono text-[10px] tracking-[0.28em] whitespace-nowrap uppercase"
        initial={reduced ? { opacity: 1 } : { opacity: 0 }}
        whileInView={{ opacity: 1 }}
        viewport={{ once: true, margin: "-40px" }}
        transition={
          reduced ? { duration: 0 } : { duration: 0.6, ease: EASE, delay: 0.2 }
        }
      >
        [ {label} ]
      </motion.span>
    </div>
  );
}

// Mobile fallback: a short vertical zigzag with the label stacked above the
// next bumper. Keeps the flipper-board callout without horizontal travel.
function MobileArrow({ label }: { readonly label: string }) {
  return (
    <div className="flex flex-col items-center gap-2 py-8 lg:hidden">
      <svg viewBox="0 0 24 48" className="h-12 w-6" aria-hidden>
        <path
          d="M 12 2 L 6 16 L 18 30 L 12 44"
          fill="none"
          stroke={CORAL}
          strokeWidth="1.6"
          strokeLinejoin="round"
        />
        <path
          d="M 8 38 L 12 46 L 16 38"
          fill="none"
          stroke={CORAL}
          strokeWidth="1.6"
        />
      </svg>
      <span className="text-cc-accent font-mono text-[10px] tracking-[0.28em] uppercase">
        [ {label} ]
      </span>
    </div>
  );
}

/* ================================================================== *
 * HERO / PLUNGER
 * H1, lead, dual CTA on the left. A static SVG plunger on the right
 * with a coral ball labeled CreateReview waiting at the bottom rail,
 * nudging on a slow loop to suggest a loaded plunger.
 * ================================================================== */

function Hero() {
  const reduced = useReducedMotion();

  return (
    <section className="px-6 pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-7">
          <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
            Messaging framework for .NET
          </span>
          <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
            Mocha messaging .NET, a table that routes every message.
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
            Plunge one command and watch it ricochet: the source-generated
            mediator, the bus, the transactional outbox, a saga validated before
            traffic, the exactly-once inbox, and the telemetry drain. One
            handler-first model, in-process and across services.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/docs/mocha">Read the docs</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <dl className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 border-t pt-6">
            <HeroFact term="License" value="MIT" />
            <HeroFact term="Runtime" value=".NET / ASP.NET Core" />
            <HeroFact term="Dispatch" value="Source-generated" />
          </dl>
        </div>
        <div className="lg:col-span-5">
          <Plunger reduced={Boolean(reduced)} />
        </div>
      </div>
    </section>
  );
}

interface HeroFactProps {
  readonly term: string;
  readonly value: string;
}

function HeroFact({ term, value }: HeroFactProps) {
  return (
    <div>
      <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
        {term}
      </dt>
      <dd className="text-cc-ink mt-1 text-sm">{value}</dd>
    </div>
  );
}

// Static plunger graphic: a vertical spring with the ball loaded at the
// bottom rail. The ball nudges 6px down and back on a 2.4s loop.
function Plunger({ reduced }: { readonly reduced: boolean }) {
  return (
    <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-xl border p-6">
      <div className="text-cc-ink-dim flex items-center justify-between font-mono text-[11px]">
        <span>playfield.launch</span>
        <span className="text-cc-accent tracking-[0.2em] uppercase">
          PLUNGE
        </span>
      </div>
      <svg
        viewBox="0 0 120 320"
        className="mx-auto mt-4 h-[300px] w-auto"
        role="img"
        aria-label="A loaded pinball plunger with the CreateReview command waiting at the bottom rail"
      >
        {/* Lane walls */}
        <line
          x1="40"
          y1="12"
          x2="40"
          y2="300"
          stroke="rgba(245,241,234,0.16)"
          strokeWidth="1"
        />
        <line
          x1="80"
          y1="12"
          x2="80"
          y2="300"
          stroke="rgba(245,241,234,0.16)"
          strokeWidth="1"
        />
        {/* Top rail the ball launches toward */}
        <line
          x1="36"
          y1="16"
          x2="84"
          y2="16"
          stroke="rgba(245,241,234,0.28)"
          strokeWidth="1"
        />
        {/* Spring coil under the ball */}
        <path
          d="M 48 300 L 72 290 L 48 280 L 72 270 L 48 260 L 72 250 L 48 240 L 72 230"
          fill="none"
          stroke={CORAL}
          strokeWidth="1.6"
          strokeLinejoin="round"
          opacity="0.85"
        />
        {/* Plunger base */}
        <rect
          x="44"
          y="300"
          width="32"
          height="8"
          rx="2"
          fill="rgba(245,241,234,0.2)"
        />
        {/* The ball: the CreateReview message, loaded and ready */}
        <motion.g
          animate={reduced ? undefined : { y: [0, 6, 0] }}
          transition={
            reduced
              ? undefined
              : { duration: 2.4, ease: "easeInOut", repeat: Infinity }
          }
        >
          <circle
            cx="60"
            cy="214"
            r="16"
            fill={CORAL}
            fillOpacity="0.18"
            stroke={CORAL}
            strokeWidth="1.6"
          />
          <circle cx="60" cy="214" r="5" fill={CORAL} />
        </motion.g>
        {/* Ball label */}
        <text
          x="60"
          y="250"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.7)"
        >
          CreateReview
        </text>
      </svg>
      <p className="text-cc-ink-dim mt-2 text-center font-mono text-[11px]">
        one command, loaded at the rail
      </p>
    </div>
  );
}

/* ================================================================== *
 * BUMPER
 * A content slab that alternates left / right across the center rail.
 * Carries an index chip, a coral corner notch on the entry side, the
 * copy + CheckIcon bullets, and an inline diagram. Each bumper draws
 * its incoming arrow on first enter-view.
 * ================================================================== */

interface BumperProps {
  readonly index: string;
  readonly label: string;
  readonly entry: "left" | "right";
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
}

function Bumper({
  index,
  label,
  entry,
  eyebrow,
  title,
  body,
  bullets,
  visual,
}: BumperProps) {
  const onLeft = entry === "left";

  return (
    <section className="px-6">
      <MobileArrow label={label} />
      <PlayfieldArrow id={`arrow-${index}`} label={label} direction={entry} />
      <div className="grid items-stretch gap-6 py-4 lg:grid-cols-12 lg:gap-10 lg:py-8">
        <div
          className={[
            "lg:col-span-6",
            onLeft ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <Slab notch={entry} index={index}>
            <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
              {eyebrow}
            </span>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
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
          </Slab>
        </div>
        <div
          className={[
            "lg:col-span-6",
            onLeft ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="border-cc-card-border bg-cc-card-bg flex h-full items-center justify-center rounded-xl border p-5 sm:p-6">
            {visual}
          </div>
        </div>
      </div>
    </section>
  );
}

interface SlabProps {
  readonly notch: "left" | "right";
  readonly index: string;
  readonly children: ReactNode;
}

function Slab({ notch, index, children }: SlabProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface relative h-full rounded-xl border p-6 sm:p-7">
      {/* Coral L-bracket notch in the corner the ball enters from. */}
      <div className="pointer-events-none absolute top-0 left-0 h-6 w-6">
        <NotchCorner side={notch} />
      </div>
      <span className="border-cc-card-border text-cc-ink-dim absolute top-4 right-4 inline-flex h-6 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
        {index}
      </span>
      {children}
    </div>
  );
}

// A 2px coral L-bracket drawn in the entry corner of the slab.
function NotchCorner({ side }: { readonly side: "left" | "right" }) {
  // The notch points to the side the ball enters from. Both anchor at the
  // top-left of the card; the long arm runs along the entry side.
  const d = side === "left" ? "M 2 22 L 2 2 L 22 2" : "M 2 2 L 22 2 L 22 22";
  return (
    <svg viewBox="0 0 24 24" className="h-6 w-6" aria-hidden>
      <path
        d={d}
        fill="none"
        stroke={CORAL}
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

/* ================================================================== *
 * Inline diagrams. Hand-built SVG aligned to cc-* tones, coral accent.
 * ================================================================== */

const MONO = "ui-monospace, monospace";
const INK = "#f5f0ea";
const INK_DIM = "rgba(245,241,234,0.6)";
const HAIR = "rgba(245,241,234,0.16)";
const CORAL_FILL = "rgba(240,120,106,0.1)";
const CORAL_STROKE = "rgba(240,120,106,0.6)";

function MediateVisual() {
  return (
    <svg
      viewBox="0 0 360 200"
      className="h-auto w-full"
      role="img"
      aria-label="ISender hits a source-generated [Handler] for CreateReview, no reflection on the path"
    >
      <rect
        x="16"
        y="80"
        width="78"
        height="40"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text
        x="55"
        y="104"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="11"
        fill="#a1a3af"
      >
        ISender
      </text>
      <path
        d="M 94 100 L 130 100"
        stroke={CORAL_STROKE}
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="130,96 140,100 130,104" fill={CORAL} />
      <rect
        x="142"
        y="78"
        width="92"
        height="44"
        rx="6"
        fill={CORAL_FILL}
        stroke={CORAL_STROKE}
      />
      <text
        x="188"
        y="98"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10.5"
        fill={CORAL}
      >
        [Handler]
      </text>
      <text
        x="188"
        y="112"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="9.5"
        fill={INK_DIM}
      >
        CreateReview
      </text>
      <path
        d="M 234 100 L 270 100"
        stroke={CORAL_STROKE}
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="270,96 280,100 270,104" fill={CORAL} />
      <rect
        x="282"
        y="80"
        width="62"
        height="40"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text
        x="313"
        y="104"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10"
        fill={INK}
      >
        return
      </text>
      <text x="16" y="160" fontFamily={MONO} fontSize="10" fill={INK_DIM}>
        Roslyn emits typed registration at compile time
      </text>
      <text x="16" y="178" fontFamily={MONO} fontSize="10" fill={INK_DIM}>
        zero reflection on the dispatch path
      </text>
    </svg>
  );
}

function BusVisual() {
  const chips = ["RabbitMQ", "Azure Service Bus", "Kafka"];
  return (
    <svg
      viewBox="0 0 360 200"
      className="h-auto w-full"
      role="img"
      aria-label="PublishAsync and SendAsync ride RabbitMQ, Azure Service Bus, or Kafka with the same handler shape"
    >
      <rect
        x="16"
        y="84"
        width="96"
        height="44"
        rx="6"
        fill={CORAL_FILL}
        stroke={CORAL_STROKE}
      />
      <text
        x="64"
        y="104"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10.5"
        fill={CORAL}
      >
        IPublisher
      </text>
      <text
        x="64"
        y="118"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="9"
        fill={INK_DIM}
      >
        PublishAsync
      </text>
      {chips.map((c, i) => {
        const y = 30 + i * 50;
        return (
          <g key={c}>
            <path
              d={`M 112 106 C 170 106, 180 ${y + 13}, 222 ${y + 13}`}
              stroke={CORAL_STROKE}
              strokeWidth="1.1"
              fill="none"
            />
            <rect
              x="222"
              y={y}
              width="124"
              height="26"
              rx="13"
              fill="rgba(245,241,234,0.04)"
              stroke={HAIR}
            />
            <text
              x="284"
              y={y + 17}
              textAnchor="middle"
              fontFamily={MONO}
              fontSize="10.5"
              fill={INK}
            >
              {c}
            </text>
          </g>
        );
      })}
      <text x="16" y="170" fontFamily={MONO} fontSize="10" fill={INK_DIM}>
        same Send / Publish in-process and over the wire
      </text>
    </svg>
  );
}

function OutboxVisual() {
  return (
    <svg
      viewBox="0 0 360 200"
      className="h-auto w-full"
      role="img"
      aria-label="A Postgres row and an outbox row commit together, then a dispatcher flushes the outbox into the broker"
    >
      <rect
        x="14"
        y="40"
        width="150"
        height="120"
        rx="10"
        fill="rgba(245,241,234,0.03)"
        stroke={HAIR}
        strokeDasharray="4 4"
      />
      <text
        x="24"
        y="58"
        fontFamily={MONO}
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        one transaction
      </text>
      <rect
        x="28"
        y="70"
        width="122"
        height="32"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text x="40" y="90" fontFamily={MONO} fontSize="10.5" fill={INK}>
        Postgres row
      </text>
      <rect
        x="28"
        y="112"
        width="122"
        height="32"
        rx="6"
        fill={CORAL_FILL}
        stroke={CORAL_STROKE}
      />
      <text x="40" y="132" fontFamily={MONO} fontSize="10.5" fill={CORAL}>
        outbox: ReviewCreated
      </text>
      <path
        d="M 164 100 L 200 100"
        stroke={CORAL_STROKE}
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="200,96 210,100 200,104" fill={CORAL} />
      <rect
        x="212"
        y="82"
        width="64"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text
        x="244"
        y="104"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10"
        fill={INK}
      >
        dispatcher
      </text>
      <path
        d="M 276 100 L 312 100"
        stroke={CORAL_STROKE}
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="312,96 322,100 312,104" fill={CORAL} />
      <rect
        x="300"
        y="40"
        width="48"
        height="120"
        rx="8"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text
        x="324"
        y="104"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10"
        fill={INK_DIM}
      >
        bus
      </text>
      <text x="14" y="184" fontFamily={MONO} fontSize="10" fill={INK_DIM}>
        commit together, then at-least-once into the bus
      </text>
    </svg>
  );
}

function SagaVisual() {
  const nodes = [
    { x: 18, label: "Draft" },
    { x: 134, label: "Checked" },
    { x: 250, label: "Published" },
  ];
  return (
    <svg
      viewBox="0 0 360 200"
      className="h-auto w-full"
      role="img"
      aria-label="A three-state saga Draft, Checked, Published with two transitions, validated before traffic"
    >
      {nodes.map((n, i) => (
        <g key={n.label}>
          <rect
            x={n.x}
            y="80"
            width="92"
            height="40"
            rx="10"
            fill={i === 1 ? CORAL_FILL : "rgba(245,241,234,0.04)"}
            stroke={i === 1 ? CORAL_STROKE : "rgba(245,241,234,0.22)"}
          />
          <text
            x={n.x + 46}
            y="105"
            textAnchor="middle"
            fontFamily="var(--font-body)"
            fontSize="13"
            fill={INK}
          >
            {n.label}
          </text>
          {i < 2 && (
            <g>
              <path
                d={`M ${n.x + 92} 100 L ${n.x + 130} 100`}
                stroke={CORAL_STROKE}
                strokeWidth="1.4"
                fill="none"
              />
              <polygon
                points={`${n.x + 130},96 ${n.x + 138},100 ${n.x + 130},104`}
                fill={CORAL}
              />
            </g>
          )}
        </g>
      ))}
      <text
        x="74"
        y="70"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="9.5"
        fill={INK_DIM}
      >
        ReviewCreated
      </text>
      <text
        x="190"
        y="70"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="9.5"
        fill={INK_DIM}
      >
        ContentChecked
      </text>
      <text x="18" y="164" fontFamily={MONO} fontSize="10" fill={CORAL}>
        validated before traffic, not at compile time
      </text>
    </svg>
  );
}

function OnceVisual() {
  return (
    <svg
      viewBox="0 0 360 200"
      className="h-auto w-full"
      role="img"
      aria-label="The broker redelivers a message, the inbox latch records its id and lets the handler run exactly once"
    >
      {/* Two delivery attempts arrive */}
      <rect
        x="16"
        y="56"
        width="96"
        height="28"
        rx="14"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text
        x="64"
        y="74"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10"
        fill={INK_DIM}
      >
        deliver #1
      </text>
      <rect
        x="16"
        y="116"
        width="96"
        height="28"
        rx="14"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text
        x="64"
        y="134"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10"
        fill={INK_DIM}
      >
        deliver #1 again
      </text>
      <path
        d="M 112 70 L 158 96"
        stroke={CORAL_STROKE}
        strokeWidth="1.2"
        fill="none"
      />
      <path
        d="M 112 130 L 158 104"
        stroke={CORAL_STROKE}
        strokeWidth="1.2"
        fill="none"
      />
      {/* The latch */}
      <rect
        x="160"
        y="78"
        width="80"
        height="44"
        rx="6"
        fill={CORAL_FILL}
        stroke={CORAL_STROKE}
      />
      <text
        x="200"
        y="98"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10.5"
        fill={CORAL}
      >
        inbox latch
      </text>
      <text
        x="200"
        y="112"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="9"
        fill={INK_DIM}
      >
        dedupe by id
      </text>
      <path
        d="M 240 100 L 276 100"
        stroke={CORAL_STROKE}
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="276,96 286,100 276,104" fill={CORAL} />
      <rect
        x="288"
        y="82"
        width="58"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke={HAIR}
      />
      <text
        x="317"
        y="104"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10"
        fill={INK}
      >
        once
      </text>
      <text x="16" y="178" fontFamily={MONO} fontSize="10" fill={INK_DIM}>
        delivery can repeat, processing happens once
      </text>
    </svg>
  );
}

function TraceVisual() {
  const spans = [
    { y: 28, x: 70, w: 240, label: "Send CreateReview" },
    { y: 52, x: 92, w: 150, label: "mediator [Handler]" },
    { y: 76, x: 110, w: 180, label: "PublishAsync (bus)" },
    { y: 100, x: 140, w: 130, label: "transport hop" },
    { y: 124, x: 160, w: 150, label: "consumer [Handler]" },
  ];
  return (
    <svg
      viewBox="0 0 360 200"
      className="h-auto w-full"
      role="img"
      aria-label="A span tree of the ball's full path: send, mediate, publish, transport, consume"
    >
      {spans.map((s, i) => (
        <g key={s.label}>
          <rect
            x={s.x}
            y={s.y}
            width={s.w}
            height="16"
            rx="3"
            fill={i === 0 ? CORAL_FILL : "rgba(245,241,234,0.05)"}
            stroke={i === 0 ? CORAL_STROKE : HAIR}
          />
          <text
            x={s.x + 8}
            y={s.y + 12}
            fontFamily={MONO}
            fontSize="9.5"
            fill={i === 0 ? CORAL : INK_DIM}
          >
            {s.label}
          </text>
        </g>
      ))}
      {/* time axis */}
      <line x1="60" y1="150" x2="320" y2="150" stroke={HAIR} strokeWidth="1" />
      <text x="60" y="166" fontFamily={MONO} fontSize="9" fill={INK_DIM}>
        t0
      </text>
      <text
        x="312"
        y="166"
        textAnchor="end"
        fontFamily={MONO}
        fontSize="9"
        fill={INK_DIM}
      >
        drain
      </text>
      <text x="60" y="188" fontFamily={MONO} fontSize="10" fill={INK_DIM}>
        telemetry needs Nitro configuration
      </text>
    </svg>
  );
}

/* ================================================================== *
 * DRAIN / CTA
 * Closing slab where the ball lands. The single allowed brand-spectrum
 * element is the 1px rule above the heading.
 * ================================================================== */

function Drain() {
  return (
    <section className="relative px-6 pt-20 pb-24 sm:pt-28">
      <MobileArrow label="DRAIN" />
      <div className="border-cc-card-border bg-cc-surface relative mx-auto max-w-3xl overflow-hidden rounded-2xl border p-10 text-center sm:p-12">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
          Drain
        </span>
        <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-2xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
          The ball lands. The whole table is one framework.
        </h2>
        <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
          Write a handler, attribute it, dispatch it. The source generator
          handles registration and the pipeline. The transport, the outbox, the
          inbox, the sagas, and the traces are part of the framework, not
          bolt-on packages you wire yourself.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <SolidButton href="/docs/mocha">Get started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            Talk to us
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}
