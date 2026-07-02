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

// Brand spectrum gradient, used exactly once on this screen (the hero eyebrow rule).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

/* -------------------------------------------------------------------------- */
/*  Ambient dot-matrix background                                              */
/*                                                                            */
/*  One repeating texture for the whole page: an evenly spaced ivory dot      */
/*  lattice. A large, soft teal glow drifts across it on its own, and a       */
/*  second copy of the lattice in teal is masked to that same drifting glow,  */
/*  so the dots it passes over visibly brighten to teal. Nothing tracks the   */
/*  pointer; the motion is a single, slow, looping CSS keyframe animation.    */
/* -------------------------------------------------------------------------- */

const FIELD_STYLE = `
.cc-v11-dots,
.cc-v11-glow,
.cc-v11-teal {
  position: absolute;
  inset: 0;
}
.cc-v11-dots {
  background-image: radial-gradient(
    circle at center,
    rgba(245, 241, 234, 0.14) 1.4px,
    transparent 2.2px
  );
  background-size: 20px 20px;
}
.cc-v11-glow {
  background-repeat: no-repeat;
  background-size: 1000px 1000px;
  background-position: 50% 38%;
  background-image: radial-gradient(
    circle at center,
    rgba(94, 234, 212, 0.14) 0%,
    rgba(94, 234, 212, 0.06) 32%,
    transparent 66%
  );
}
.cc-v11-teal {
  background-size: 20px 20px;
  background-image: radial-gradient(
    circle at center,
    rgba(94, 234, 212, 0.4) 1.4px,
    transparent 2.2px
  );
  -webkit-mask-image: radial-gradient(
    circle at center,
    #000 0%,
    rgba(0, 0, 0, 0.65) 42%,
    transparent 74%
  );
  mask-image: radial-gradient(
    circle at center,
    #000 0%,
    rgba(0, 0, 0, 0.65) 42%,
    transparent 74%
  );
  -webkit-mask-repeat: no-repeat;
  mask-repeat: no-repeat;
  -webkit-mask-size: 720px 720px;
  mask-size: 720px 720px;
  -webkit-mask-position: 50% 38%;
  mask-position: 50% 38%;
}
.cc-v11-animate.cc-v11-glow {
  animation: cc-v11-glowDrift 40s ease-in-out infinite;
}
.cc-v11-animate.cc-v11-teal {
  animation: cc-v11-maskDrift 40s ease-in-out infinite;
}
@keyframes cc-v11-glowDrift {
  0% {
    background-position: 12% 22%;
  }
  18% {
    background-position: 72% 14%;
  }
  38% {
    background-position: 90% 60%;
  }
  58% {
    background-position: 54% 88%;
  }
  78% {
    background-position: 18% 60%;
  }
  100% {
    background-position: 12% 22%;
  }
}
@keyframes cc-v11-maskDrift {
  0% {
    -webkit-mask-position: 12% 22%;
    mask-position: 12% 22%;
  }
  18% {
    -webkit-mask-position: 72% 14%;
    mask-position: 72% 14%;
  }
  38% {
    -webkit-mask-position: 90% 60%;
    mask-position: 90% 60%;
  }
  58% {
    -webkit-mask-position: 54% 88%;
    mask-position: 54% 88%;
  }
  78% {
    -webkit-mask-position: 18% 60%;
    mask-position: 18% 60%;
  }
  100% {
    -webkit-mask-position: 12% 22%;
    mask-position: 12% 22%;
  }
}
@media (prefers-reduced-motion: reduce) {
  .cc-v11-glow,
  .cc-v11-teal {
    animation: none;
  }
}
`;

interface DotMatrixFieldProps {
  readonly reduced: boolean;
}

function DotMatrixField({ reduced }: DotMatrixFieldProps) {
  const animate = reduced ? "" : " cc-v11-animate";
  return (
    <div aria-hidden="true" className="pointer-events-none fixed inset-0 -z-10">
      <style dangerouslySetInnerHTML={{ __html: FIELD_STYLE }} />
      <div className="cc-v11-dots" />
      <div className={`cc-v11-glow${animate}`} />
      <div className={`cc-v11-teal${animate}`} />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section primitives                                                         */
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
      <DotMatrixField reduced={reduced} />

      <div className="relative z-0">
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
              error rate per operation, with p95 and p99, per-client usage, and
              an impact score that ranks what hurts the system most.
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
              tracing, and a registry that keeps your schema and clients in
              sync.
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
    </div>
  );
}
