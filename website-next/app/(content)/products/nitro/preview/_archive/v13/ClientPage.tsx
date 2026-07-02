"use client";

import { useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { ControlPlaneConsole } from "@/src/components/nitro/ControlPlaneConsole";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  NitroDiagnose,
  NitroFusion,
  NitroReel,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

/* -------------------------------------------------------------------------- */
/*  Living-graph background                                                    */
/* -------------------------------------------------------------------------- */

// Brand spectrum gradient, used exactly once on this screen (the hero rule).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

type GraphNode = {
  readonly x: number;
  readonly y: number;
  readonly r: number;
  readonly teal?: boolean;
};

// A small, deliberately composed graph laid out across a 1440x900 field. Nodes
// hug the margins and leave the central reading column open, so the pattern is
// legible as a graph without ever competing with the copy. Positions are
// static; nothing here is simulated or driven by per-frame React state.
const NODES: readonly GraphNode[] = [
  { x: 110, y: 170, r: 4, teal: true }, // 0  left, top
  { x: 230, y: 380, r: 3.5 }, //           1  left, upper-mid
  { x: 150, y: 600, r: 4.5, teal: true }, // 2  left, lower
  { x: 320, y: 780, r: 3.5 }, //           3  bottom-left
  { x: 520, y: 130, r: 3.5 }, //           4  top
  { x: 780, y: 90, r: 4, teal: true }, //  5  top-center
  { x: 1030, y: 150, r: 3.5 }, //          6  top-right
  { x: 1310, y: 260, r: 4.5, teal: true }, // 7  right, upper
  { x: 1210, y: 470, r: 3.5 }, //          8  right, mid
  { x: 1330, y: 680, r: 4 }, //            9  right, lower
  { x: 1050, y: 790, r: 3.5, teal: true }, // 10 bottom-right
  { x: 640, y: 810, r: 4 }, //             11 bottom-center
  { x: 880, y: 440, r: 5, teal: true }, // 12 central hub
];

// Edges form one connected graph: a ring around the margins plus a central hub
// (node 12) that spokes into it, so the shape reads as a purposeful graph.
const EDGES: readonly (readonly [number, number])[] = [
  [0, 1],
  [1, 2],
  [2, 3],
  [0, 4],
  [4, 5],
  [5, 6],
  [6, 7],
  [7, 8],
  [8, 9],
  [9, 10],
  [10, 11],
  [11, 3],
  [1, 12],
  [4, 12],
  [8, 12],
  [11, 12],
];

// A few edges carry a soft blip of light that travels end to end, reading as
// data flowing through the graph.
const PULSES: readonly (readonly [number, number])[] = [
  [0, 4],
  [4, 5],
  [5, 6],
  [8, 12],
];
const PULSE_DUR = [7, 8, 6.5, 7.5];
const PULSE_DELAY = [0, -3.4, -1.6, -4.2];

const DRIFTS = ["ccDriftA", "ccDriftB", "ccDriftC", "ccDriftD"];

// Slow, staggered node drift plus a traveling blip along the pulse edges. Motion
// is entirely self-driven; there is no pointer input anywhere on this page.
const KEYFRAMES = `
@keyframes ccDriftA {0%,100%{transform:translate(0px,0px)}50%{transform:translate(4px,-3px)}}
@keyframes ccDriftB {0%,100%{transform:translate(0px,0px)}50%{transform:translate(-3px,4px)}}
@keyframes ccDriftC {0%,100%{transform:translate(0px,0px)}33%{transform:translate(3px,3px)}66%{transform:translate(-3px,2px)}}
@keyframes ccDriftD {0%,100%{transform:translate(0px,0px)}50%{transform:translate(-4px,-3px)}}
@keyframes ccPulse {0%{stroke-dashoffset:0}100%{stroke-dashoffset:-200}}
`;

interface GraphFieldProps {
  readonly reduced: boolean;
}

/**
 * The one background pattern for this screen: a calm, living graph of nodes and
 * edges spanning the viewport. On-brand for GraphQL, animated on its own.
 */
function GraphField({ reduced }: GraphFieldProps) {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none fixed inset-0 -z-10 overflow-hidden"
    >
      {!reduced ? <style>{KEYFRAMES}</style> : null}
      <svg
        className="h-full w-full"
        viewBox="0 0 1440 900"
        preserveAspectRatio="xMidYMid slice"
        fill="none"
      >
        {EDGES.map(([a, b], i) => (
          <line
            key={`edge-${i}`}
            x1={NODES[a].x}
            y1={NODES[a].y}
            x2={NODES[b].x}
            y2={NODES[b].y}
            stroke="rgba(94,234,212,0.1)"
            strokeWidth={1}
          />
        ))}

        {!reduced
          ? PULSES.map(([a, b], i) => (
              <line
                key={`pulse-${i}`}
                x1={NODES[a].x}
                y1={NODES[a].y}
                x2={NODES[b].x}
                y2={NODES[b].y}
                stroke="rgba(94,234,212,0.55)"
                strokeWidth={1.4}
                strokeLinecap="round"
                pathLength={200}
                strokeDasharray="5 195"
                style={{
                  animation: `ccPulse ${PULSE_DUR[i]}s linear ${PULSE_DELAY[i]}s infinite`,
                  filter: "drop-shadow(0 0 3px rgba(94,234,212,0.4))",
                }}
              />
            ))
          : null}

        {NODES.map((n, i) => {
          const core = n.teal
            ? "rgba(94,234,212,0.55)"
            : "rgba(245,241,234,0.35)";
          const halo = n.teal
            ? "rgba(94,234,212,0.1)"
            : "rgba(245,241,234,0.05)";
          const dur = 14 + (i % 5) * 2.2;
          const delay = -(i * 1.3);
          const style = reduced
            ? undefined
            : {
                animation: `${DRIFTS[i % DRIFTS.length]} ${dur}s ease-in-out ${delay}s infinite`,
              };
          return (
            <g key={`node-${i}`} style={style}>
              <circle cx={n.x} cy={n.y} r={n.r * 2.4} fill={halo} />
              <circle cx={n.x} cy={n.y} r={n.r} fill={core} />
            </g>
          );
        })}
      </svg>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Chrome primitives (plain, matching v10)                                    */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface FeatureSectionProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly visual: ReactNode;
  /** When true, the copy sits on the right and the visual on the left (lg+). */
  readonly reverse?: boolean;
}

/**
 * One feature beat: a short benefit headline + a sentence or two of body paired
 * with a framed, animated Nitro product screen (the real Nitro UI).
 */
function FeatureSection({
  id,
  index,
  eyebrow,
  title,
  body,
  visual,
  reverse = false,
}: FeatureSectionProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <RevealOnScroll
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex flex-col gap-5">
            <div className="flex items-center gap-3">
              <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                {index}
              </span>
              <Eyebrow>{eyebrow}</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading text-h3 text-balance">
              {title}
            </h2>
            <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
              {body}
            </p>
          </div>
        </RevealOnScroll>

        <RevealOnScroll
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
          hiddenClassName="translate-y-8 opacity-0"
        >
          <FramedVisual>{visual}</FramedVisual>
        </RevealOnScroll>
      </div>
    </section>
  );
}

interface FramedVisualProps {
  readonly children: ReactNode;
}

/** Frames a raw (chrome-less) Nitro screen like an embedded product screenshot. */
function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-x-6 -inset-y-4 -z-10 rounded-[2rem] opacity-40 blur-3xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 40%, rgba(94,234,212,0.18), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
        {children}
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  const reduced = useReducedMotion() ?? false;

  return (
    <div className="relative z-0">
      <GraphField reduced={reduced} />

      {/* HERO ───────────────────────────────────────────────── */}
      <section className="pt-6 pb-16 text-center sm:pt-12">
        <RevealOnScroll className="mx-auto flex max-w-3xl flex-col items-center gap-6">
          <div className="flex flex-col items-center gap-4">
            <span
              aria-hidden="true"
              className="h-px w-24 rounded-full"
              style={{ background: SPECTRUM }}
            />
            <Eyebrow>The Control Plane for GraphQL</Eyebrow>
          </div>
          <h1 className="text-cc-heading font-heading text-h1 text-balance">
            Your API, in motion.
          </h1>
          <p className="lead text-cc-ink mx-auto max-w-2xl !text-xl !leading-relaxed">
            Nitro is the cockpit for your GraphQL and .NET backend. Author
            operations, watch them run, trace every request, and evolve your
            schema without breaking the clients you ship to.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>

        {/* The 5-tab product reel: it is already an app window, so no outer
            frame, and the phase nav floats over the bottom edge of the stage. */}
        <RevealOnScroll
          className="mt-16 sm:mt-20"
          hiddenClassName="translate-y-10 opacity-0 scale-[0.98]"
          shownClassName="translate-y-0 opacity-100 scale-100"
        >
          <div className="relative mx-auto w-full max-w-6xl">
            <div
              aria-hidden="true"
              className="absolute -inset-x-10 -inset-y-8 -z-10 rounded-[2.5rem] opacity-50 blur-3xl"
              style={{
                background:
                  "radial-gradient(50% 50% at 50% 30%, rgba(94,234,212,0.16), transparent 70%)",
              }}
            />
            <NitroReel tabsOverlay />
          </div>
        </RevealOnScroll>
      </section>

      {/* OBSERVE ─ v7 control-plane console, centered like the v7 hero ─ */}
      <section
        id="observe"
        className="border-cc-card-border scroll-mt-24 border-t py-20 text-center sm:py-28"
      >
        <RevealOnScroll className="mx-auto flex max-w-2xl flex-col items-center gap-5">
          <div className="flex items-center gap-3">
            <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
              01
            </span>
            <Eyebrow>Observe</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading text-h3 text-balance">
            See exactly how your API behaves in production.
          </h2>
          <p className="text-cc-ink max-w-xl text-base leading-relaxed text-pretty sm:text-lg">
            Wire up Nitro and OpenTelemetry to watch latency, throughput, and
            error rate per operation, with p95 and p99, per-client usage, and an
            impact score that ranks what hurts the system most.
          </p>
        </RevealOnScroll>

        <RevealOnScroll
          className="mt-14 sm:mt-16"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <ControlPlaneConsole className="mx-auto max-w-5xl" />
        </RevealOnScroll>
      </section>

      {/* FEATURE SECTIONS ─ the real Nitro UI screens ─ */}
      <FeatureSection
        id="trace"
        index="02"
        eyebrow="Trace"
        title="Follow one request across your whole backend."
        body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
        visual={<NitroTrace className="w-full" />}
        reverse
      />

      <FeatureSection
        id="diagnose"
        index="03"
        eyebrow="Diagnose"
        title="From an error spike to the line that threw it."
        body="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it, with no log spelunking required."
        visual={<NitroDiagnose className="w-full" />}
      />

      <FeatureSection
        id="schema"
        index="04"
        eyebrow="Evolve"
        title="Change your schema without breaking your clients."
        body="The schema registry classifies every change as safe, dangerous, or breaking and checks it against published clients in CI, so you validate on a PR and publish only when it is safe to ship."
        visual={<NitroSchema className="w-full" />}
        reverse
      />

      <FeatureSection
        id="fusion"
        index="05"
        eyebrow="Compose"
        title="One graph, executed across every subgraph."
        body="With Fusion, Nitro shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
        visual={<NitroFusion className="w-full" />}
      />

      {/* CTA ────────────────────────────────────────────────── */}
      <section className="border-cc-card-border border-t py-24 text-center sm:py-32">
        <RevealOnScroll className="mx-auto flex max-w-2xl flex-col items-center gap-6">
          <Eyebrow>Ready when you are</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 text-balance">
            Put your API on the control plane.
          </h2>
          <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
            Start in the GraphQL IDE in seconds, then grow into observability,
            tracing, and a registry that keeps your schema and clients in sync.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>
      </section>
    </div>
  );
}
