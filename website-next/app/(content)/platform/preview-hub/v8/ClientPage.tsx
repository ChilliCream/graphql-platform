"use client";

import Link from "next/link";
import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Brand spectrum                                                            */
/*  The page accent is violet. The full brand spectrum appears exactly once,  */
/*  as a backdrop mesh in the Nitro Spine band.                               */
/* -------------------------------------------------------------------------- */

const CYAN = "#16b9e4";
const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

const SPECTRUM_MESH =
  "radial-gradient(60% 80% at 15% 30%, rgba(22, 185, 228, 0.28), transparent 70%), " +
  "radial-gradient(55% 75% at 50% 60%, rgba(124, 146, 198, 0.32), transparent 72%), " +
  "radial-gradient(60% 80% at 85% 40%, rgba(240, 120, 106, 0.28), transparent 70%)";

/* -------------------------------------------------------------------------- */
/*  Capability ledger                                                         */
/*  Eight numbered surfaces, in narrative order, each routed to its real      */
/*  page. No per-tile art on this variant.                                    */
/* -------------------------------------------------------------------------- */

type LifecycleKey = "build" | "run" | "evolve";

interface Capability {
  readonly id: string;
  readonly title: string;
  readonly outcome: string;
  readonly href: string;
  readonly lifecycle: LifecycleKey;
}

const CAPABILITIES: readonly Capability[] = [
  {
    id: "build",
    title: "Build",
    outcome: "Ship from the code that runs it.",
    href: "/platform/build",
    lifecycle: "build",
  },
  {
    id: "agentic-coding",
    title: "Agentic Coding",
    outcome: "Give coding agents a feedback loop.",
    href: "/platform/agentic-coding",
    lifecycle: "build",
  },
  {
    id: "observability",
    title: "Observability",
    outcome: "See what the API is doing, right now.",
    href: "/platform/observability",
    lifecycle: "run",
  },
  {
    id: "workflows",
    title: "Workflows",
    outcome: "Let work continue after the request.",
    href: "/platform/workflows",
    lifecycle: "run",
  },
  {
    id: "analytics",
    title: "Analytics",
    outcome: "Know which fields earn their keep.",
    href: "/platform/analytics",
    lifecycle: "run",
  },
  {
    id: "ecosystem",
    title: "Ecosystem",
    outcome: "An ecosystem you can trust and reuse.",
    href: "/platform/ecosystem",
    lifecycle: "run",
  },
  {
    id: "release-safety",
    title: "Release Safety",
    outcome: "Change contracts with a safety net.",
    href: "/platform/release-safety",
    lifecycle: "evolve",
  },
  {
    id: "continuous-integration",
    title: "Continuous Integration",
    outcome: "Innovate with confidence at merge time.",
    href: "/platform/continuous-integration",
    lifecycle: "evolve",
  },
];

interface Lifecycle {
  readonly key: LifecycleKey;
  readonly label: string;
  readonly intent: string;
}

const LIFECYCLES: readonly Lifecycle[] = [
  {
    key: "build",
    label: "Build",
    intent: "Author the API and let agents help.",
  },
  {
    key: "run",
    label: "Run",
    intent: "Operate it in production with eyes on every call.",
  },
  {
    key: "evolve",
    label: "Evolve",
    intent: "Ship change without breaking published clients.",
  },
];

/* -------------------------------------------------------------------------- */
/*  Chrome primitives                                                         */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/*  Left-aligned. Violet accent bar replaces the spectrum on this section.    */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <header className="flex flex-col gap-7">
      <div className="flex items-center gap-3">
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em]">
          00
        </span>
        <span
          className="h-px w-12"
          style={{ backgroundColor: VIOLET }}
          aria-hidden
        />
        <Eyebrow>The ChilliCream Platform</Eyebrow>
      </div>
      <div className="flex items-start gap-5">
        <span
          className="mt-3 hidden h-24 w-[3px] shrink-0 md:block"
          style={{ backgroundColor: VIOLET }}
          aria-hidden
        />
        <h1 className="font-heading text-hero text-cc-heading max-w-4xl font-semibold tracking-tight">
          Eight capabilities, one platform for every GraphQL API.
        </h1>
      </div>
      <p className="text-cc-ink lead max-w-2xl">
        The ChilliCream platform covers the full life of a GraphQL API, from the
        first resolver to the next breaking change. Read the ledger below, walk
        the lifecycle ladder, and meet the control plane that holds it all
        together.
      </p>
      <div className="flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
      <dl className="mt-2 flex flex-wrap gap-x-10 gap-y-3">
        <div className="flex items-baseline gap-2">
          <dt className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            Capabilities
          </dt>
          <dd className="text-cc-heading font-mono text-[0.95rem] tabular-nums">
            08
          </dd>
        </div>
        <div className="flex items-baseline gap-2">
          <dt className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            Lifecycle stages
          </dt>
          <dd className="text-cc-heading font-mono text-[0.95rem] tabular-nums">
            03
          </dd>
        </div>
        <div className="flex items-baseline gap-2">
          <dt className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            Control plane
          </dt>
          <dd className="text-cc-heading font-mono text-[0.95rem] tabular-nums">
            01
          </dd>
        </div>
      </dl>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Capability Index                                                          */
/*  8 numbered rows. Enter-view-once fade + 8px rise, staggered 60ms.         */
/* -------------------------------------------------------------------------- */

interface CapabilityRowProps {
  readonly capability: Capability;
  readonly index: number;
  readonly reduced: boolean;
}

function CapabilityRow({ capability, index, reduced }: CapabilityRowProps) {
  const initial = reduced ? { opacity: 1, y: 0 } : { opacity: 0, y: 8 };
  const inView = { opacity: 1, y: 0 };
  return (
    <motion.div
      initial={initial}
      whileInView={inView}
      viewport={{ once: true, margin: "-10% 0px" }}
      transition={{
        duration: 0.45,
        ease: "easeOut",
        delay: reduced ? 0 : index * 0.06,
      }}
    >
      <Link
        href={capability.href}
        className="group border-cc-card-border hover:border-cc-card-border-hover grid grid-cols-[3rem_1fr_auto] items-center gap-4 border-b py-5 no-underline transition-colors md:grid-cols-[3.5rem_18rem_1fr_auto] md:gap-8"
      >
        <span className="text-cc-nav-label font-mono text-[0.78rem] tabular-nums">
          {String(index + 1).padStart(2, "0")}
        </span>
        <h3 className="font-heading text-cc-heading group-hover:text-cc-accent text-[1.15rem] font-semibold tracking-tight transition-colors md:text-[1.25rem]">
          {capability.title}
        </h3>
        <p className="text-cc-ink-dim col-span-3 text-[0.92rem] leading-relaxed md:col-span-1 md:text-[0.95rem]">
          {capability.outcome}
        </p>
        <span
          className="hidden font-mono text-[0.82rem] font-medium md:inline-flex md:items-center md:gap-2"
          style={{ color: VIOLET }}
        >
          Open
          <span
            aria-hidden
            className="inline-block transition-transform group-hover:translate-x-1"
          >
            →
          </span>
        </span>
      </Link>
    </motion.div>
  );
}

function CapabilityIndex() {
  const reduced = useReducedMotion() ?? false;
  return (
    <section className="flex flex-col gap-7">
      <div className="flex items-end justify-between gap-6">
        <div>
          <Eyebrow>01 · Capability Index</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading mt-2 font-semibold tracking-tight">
            Eight surfaces. Open the one closest to today.
          </h2>
        </div>
        <span
          className="hidden h-px w-32 shrink-0 md:block"
          style={{
            background: `linear-gradient(90deg, ${VIOLET}, transparent)`,
          }}
          aria-hidden
        />
      </div>
      <div className="border-cc-card-border border-t">
        {CAPABILITIES.map((capability, index) => (
          <CapabilityRow
            key={capability.id}
            capability={capability}
            index={index}
            reduced={reduced}
          />
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Lifecycle Ladder                                                          */
/*  Three vertical hairline spines with capability dots and labels.           */
/* -------------------------------------------------------------------------- */

function LifecycleLadder() {
  return (
    <section className="flex flex-col gap-7">
      <div className="flex items-end justify-between gap-6">
        <div>
          <Eyebrow>02 · Lifecycle Ladder</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading mt-2 font-semibold tracking-tight">
            Build it. Run it. Evolve it.
          </h2>
        </div>
        <span
          className="hidden h-px w-32 shrink-0 md:block"
          style={{
            background: `linear-gradient(90deg, ${VIOLET}, transparent)`,
          }}
          aria-hidden
        />
      </div>
      <div className="grid gap-8 md:grid-cols-3 md:gap-10">
        {LIFECYCLES.map((stage) => {
          const stageCaps = CAPABILITIES.filter(
            (c) => c.lifecycle === stage.key,
          );
          const globalIndex = (cap: Capability) =>
            CAPABILITIES.findIndex((c) => c.id === cap.id);
          return (
            <div
              key={stage.key}
              className="border-cc-card-border bg-cc-card-bg flex flex-col gap-5 rounded-xl border p-6"
            >
              <div className="flex items-center gap-3">
                <span
                  className="h-2 w-2 rounded-full"
                  style={{ backgroundColor: VIOLET }}
                  aria-hidden
                />
                <Eyebrow>{stage.label}</Eyebrow>
              </div>
              <p className="text-cc-ink text-[0.95rem] leading-relaxed">
                {stage.intent}
              </p>
              <ol className="relative mt-2 flex flex-col gap-4 pl-5">
                <span
                  className="bg-cc-card-border absolute top-1 bottom-1 left-[3px] w-px"
                  aria-hidden
                />
                {stageCaps.map((cap) => (
                  <li key={cap.id} className="relative">
                    <span
                      className="absolute top-[7px] left-[-19px] h-[7px] w-[7px] rounded-full"
                      style={{ backgroundColor: VIOLET }}
                      aria-hidden
                    />
                    <Link href={cap.href} className="group block no-underline">
                      <div className="flex items-baseline justify-between gap-3">
                        <span className="text-cc-heading group-hover:text-cc-accent font-heading text-[1rem] font-semibold tracking-tight transition-colors">
                          {cap.title}
                        </span>
                        <span className="text-cc-nav-label font-mono text-[0.65rem] tabular-nums">
                          {String(globalIndex(cap) + 1).padStart(2, "0")}
                        </span>
                      </div>
                      <p className="text-cc-ink-dim mt-0.5 text-[0.82rem] leading-snug">
                        {cap.outcome}
                      </p>
                    </Link>
                  </li>
                ))}
              </ol>
            </div>
          );
        })}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Nitro Spine band                                                          */
/*  The single spectrum event on this page. Full-bleed mesh backdrop,         */
/*  a horizontal pulsing spine line, hairline top and bottom edges.           */
/* -------------------------------------------------------------------------- */

function NitroSpineBand() {
  const reduced = useReducedMotion() ?? false;
  const pulse = reduced ? { opacity: 0.7 } : { opacity: [0.5, 0.9, 0.5] };
  return (
    <section
      className="border-cc-card-border relative left-1/2 w-screen -translate-x-1/2 border-y"
      aria-labelledby="nitro-spine-heading"
    >
      <span
        className="bg-cc-surface pointer-events-none absolute inset-0"
        aria-hidden
      />
      <span
        className="pointer-events-none absolute inset-0 opacity-[0.55]"
        style={{ background: SPECTRUM_MESH }}
        aria-hidden
      />
      <span
        className="pointer-events-none absolute inset-x-0 top-1/2 -translate-y-1/2"
        aria-hidden
      >
        <motion.span
          className="block h-px w-full"
          style={{
            background: `linear-gradient(90deg, transparent, ${CYAN}, ${VIOLET}, ${CORAL}, transparent)`,
          }}
          animate={pulse}
          transition={
            reduced
              ? undefined
              : {
                  duration: 4,
                  repeat: Infinity,
                  ease: "easeInOut",
                }
          }
        />
      </span>
      <div className="relative mx-auto w-full max-w-6xl px-6 py-16 md:px-10 md:py-20">
        <div className="grid gap-10 md:grid-cols-[1.4fr_1fr] md:items-center">
          <div className="flex flex-col gap-5">
            <Eyebrow>03 · The Spectrum Hinge · Nitro</Eyebrow>
            <h2
              id="nitro-spine-heading"
              className="font-heading text-h2 text-cc-heading max-w-2xl font-semibold tracking-tight"
            >
              The control plane that powers the platform.
            </h2>
            <p className="text-cc-ink max-w-xl leading-relaxed">
              Nitro is the hosted surface where the eight capabilities meet:
              schema registry, release checks, analytics, and traces all share
              one home. Connect a service, ship a change, and Nitro keeps the
              rest of the platform in sync.
            </p>
            <ul className="text-cc-ink-dim mt-1 flex flex-col gap-1.5">
              {[
                "Schema registry for every environment",
                "Release checks against published clients",
                "Field usage and traces in one timeline",
              ].map((line) => (
                <li
                  key={line}
                  className="flex items-start gap-2 text-[0.9rem] leading-snug"
                >
                  <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                    <CheckIcon size={12} />
                  </span>
                  <span>{line}</span>
                </li>
              ))}
            </ul>
          </div>
          <div className="flex flex-col gap-3 md:items-end">
            <SolidButton href="https://nitro.chillicream.com">
              Open Nitro
            </SolidButton>
            <OutlineButton href="/products/nitro">About Nitro</OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Proof Grid                                                                */
/*  3 by 2 cards on cc-card-bg. Honest framing throughout.                    */
/* -------------------------------------------------------------------------- */

interface ProofCardData {
  readonly title: string;
  readonly body: string;
  readonly mono: string;
}

const PROOFS: readonly ProofCardData[] = [
  {
    title: "Schema registry",
    body: "One registry per environment. Every service publishes the schema it actually serves so every consumer can plan against the same source.",
    mono: "registry",
  },
  {
    title: "Release checks",
    body: "Diff a proposed schema against published clients. Block on breaking change, warn on risky, allow the rest.",
    mono: "release",
  },
  {
    title: "Field analytics",
    body: "Field-level usage over time, per client. Find the fields you can deprecate and the ones you cannot.",
    mono: "analytics",
  },
  {
    title: "Operation traces",
    body: "Operation-level traces with field timings and N+1 hot paths. Export via OpenTelemetry into the stack you already run.",
    mono: "traces",
  },
  {
    title: "First-party ecosystem",
    body: "Banana Cake Pop IDE, Strawberry Shake typed clients via MSBuild codegen, Green Donut DataLoaders. One vendor, one support path.",
    mono: "ecosystem",
  },
  {
    title: "Telemetry honesty",
    body: "Traces and analytics need Nitro configuration on each service. No silent sampling, no hidden defaults, no surprise bills.",
    mono: "telemetry",
  },
];

function ProofGrid() {
  return (
    <section className="flex flex-col gap-7">
      <div className="flex items-end justify-between gap-6">
        <div>
          <Eyebrow>04 · Proof</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading mt-2 font-semibold tracking-tight">
            What the control plane actually does.
          </h2>
        </div>
        <span
          className="hidden h-px w-32 shrink-0 md:block"
          style={{
            background: `linear-gradient(90deg, ${VIOLET}, transparent)`,
          }}
          aria-hidden
        />
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {PROOFS.map((proof) => (
          <article
            key={proof.title}
            className="border-cc-card-border bg-cc-card-bg flex flex-col gap-3 rounded-xl border p-6"
          >
            <div className="flex items-center justify-between">
              <span
                className="h-2 w-2 rounded-full"
                style={{ backgroundColor: VIOLET }}
                aria-hidden
              />
              <span
                className="font-mono text-[0.6rem] tracking-[0.18em] uppercase"
                style={{ color: VIOLET }}
              >
                {proof.mono}
              </span>
            </div>
            <h3 className="font-heading text-cc-heading text-[1.05rem] font-semibold tracking-tight">
              {proof.title}
            </h3>
            <p className="text-cc-ink-dim text-[0.9rem] leading-relaxed">
              {proof.body}
            </p>
          </article>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Numbers strip                                                             */
/*  4 mono stats divided by cc-card-border hairlines.                         */
/* -------------------------------------------------------------------------- */

interface Stat {
  readonly value: string;
  readonly label: string;
}

const STATS: readonly Stat[] = [
  { value: "08", label: "Capabilities" },
  { value: "03", label: "Lifecycle stages" },
  { value: "01", label: "Control plane" },
  { value: "06", label: "First-party products" },
];

function NumbersStrip() {
  return (
    <section
      aria-label="Platform at a glance"
      className="border-cc-card-border grid grid-cols-2 overflow-hidden rounded-xl border md:grid-cols-4"
    >
      {STATS.map((stat, i) => (
        <div
          key={stat.label}
          className={[
            "bg-cc-card-bg flex flex-col gap-1 px-5 py-5",
            i > 0 ? "md:border-cc-card-border md:border-l" : "",
            i === 1 ? "border-cc-card-border border-l md:border-l-0" : "",
            i >= 2 ? "border-cc-card-border border-t md:border-t-0" : "",
            i === 3 ? "border-cc-card-border border-l" : "",
          ]
            .filter(Boolean)
            .join(" ")}
        >
          <span className="text-cc-heading font-mono text-[1.5rem] tabular-nums">
            {stat.value}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            {stat.label}
          </span>
        </div>
      ))}
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                               */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="flex flex-col items-center gap-6 py-6 text-center">
      <Eyebrow>05 · Pick a capability</Eyebrow>
      <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
        Start with the surface closest to today&apos;s problem.
      </h2>
      <p className="text-cc-ink-dim max-w-2xl text-[0.95rem] leading-relaxed">
        Every row above is a real page. Open the one that maps to the work in
        front of you, or start a project and let the platform fold in as you
        need it.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  return (
    <div className="flex flex-col gap-20 py-6">
      <Hero />
      <CapabilityIndex />
      <LifecycleLadder />
      <NitroSpineBand />
      <ProofGrid />
      <NumbersStrip />
      <ClosingCta />
    </div>
  );
}
