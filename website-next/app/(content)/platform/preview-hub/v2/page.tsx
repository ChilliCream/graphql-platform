import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroMonitoring } from "@/src/nitro";

export const metadata: Metadata = {
  title: "The ChilliCream Platform: The Developer Loop",
  description:
    "Walk the ChilliCream GraphQL platform as a developer loop: build, run, observe, evolve, and open to agents. Five beats, eight surfaces, one control plane.",
  keywords: [
    "ChilliCream platform",
    "GraphQL developer loop",
    "GraphQL build pipeline",
    "GraphQL observability",
    "GraphQL workflows",
    "GraphQL release safety",
    "agentic coding",
    "Nitro control plane",
    "GraphQL platform overview",
    "GraphQL ecosystem",
  ],
  openGraph: {
    title: "The ChilliCream Platform: The Developer Loop",
    description:
      "Build, observe, evolve, then let agents help. Walk the ChilliCream GraphQL platform as a developer loop across five beats and eight surfaces.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Brand spectrum (single use, on the hero headline).                        */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Beat manifest. Five ordered beats trace the developer loop. Each beat     */
/*  links to its sub-page and lists the concrete moves it enables.            */
/* -------------------------------------------------------------------------- */

interface Beat {
  readonly number: string;
  readonly verb: string;
  readonly title: string;
  readonly href: string;
  readonly lede: string;
  readonly moves: readonly string[];
  readonly tint: string;
}

const BEATS: readonly Beat[] = [
  {
    number: "01",
    verb: "Build",
    title: "Author the schema once, in real code.",
    href: "/platform/build",
    lede: "Define types, resolvers, and operations in your language of choice. Schema-first or code-first, both compose into a single graph that ships to production.",
    moves: [
      "Code-first or schema-first authoring in .NET, TypeScript, and more",
      "Compose subgraphs into a single graph with Fusion",
      "Local previews of the composed schema before you commit",
    ],
    tint: "#16b9e4",
  },
  {
    number: "02",
    verb: "Run",
    title: "Promote through workflows you can audit.",
    href: "/platform/workflows",
    lede: "Move a schema from feature branch to production through workflows that capture who changed what, why, and what was checked before it shipped.",
    moves: [
      "Branch, stage, and production environments per graph",
      "Required checks on composition, breaking changes, and policy",
      "Promotion history with the diff and the approver attached",
    ],
    tint: "#5fa6d3",
  },
  {
    number: "03",
    verb: "Observe",
    title: "Watch every operation, not just every request.",
    href: "/platform/observability",
    lede: "Nitro indexes every operation by name and shape, so latency, errors, and field usage are GraphQL-native, not log lines stitched together after the fact.",
    moves: [
      "Per-operation latency, error rate, and throughput",
      "Field-level usage across every published client",
      "Trace waterfalls that follow a query through every subgraph",
    ],
    tint: "#7c92c6",
  },
  {
    number: "04",
    verb: "Evolve",
    title: "Ship change without surprising the callers.",
    href: "/platform/release-safety",
    lede: "Before a deprecated field disappears, see who is still calling it. Before a breaking change merges, see which published clients are affected and gate on it.",
    moves: [
      "Breaking-change detection against the live schema",
      "Field usage attribution per published client",
      "Deprecation timelines with traffic, not guesswork",
    ],
    tint: "#b6856e",
  },
  {
    number: "05",
    verb: "Open to agents",
    title: "Give coding agents a graph they can reason about.",
    href: "/platform/agentic-coding",
    lede: "Agents that read the schema, the operation catalog, and recent traces ship correct code faster, because they query the same control plane your team does.",
    moves: [
      "MCP endpoints exposing the schema and operation catalog",
      "Scoped credentials and audit trails for agent activity",
      "Patterns for review-gated, agent-authored changes",
    ],
    tint: "#f0786a",
  },
];

/* -------------------------------------------------------------------------- */
/*  "More to explore" strip. The three established platform surfaces that     */
/*  sit alongside the loop but are not beats of it.                           */
/* -------------------------------------------------------------------------- */

interface ExploreLink {
  readonly title: string;
  readonly href: string;
  readonly blurb: string;
}

const EXPLORE: readonly ExploreLink[] = [
  {
    title: "Analytics",
    href: "/platform/analytics",
    blurb:
      "Dashboards on every operation, every client, every field. Read the graph the way it is actually used.",
  },
  {
    title: "Continuous Integration",
    href: "/platform/continuous-integration",
    blurb:
      "Schema checks, composition, and policy enforcement wired into the pull request before code lands.",
  },
  {
    title: "Ecosystem",
    href: "/platform/ecosystem",
    blurb:
      "Hot Chocolate, Strawberry Shake, Banana Cake Pop, and Nitro. The libraries and tools the platform is built on.",
  },
];

/* -------------------------------------------------------------------------- */
/*  Small inline section components.                                          */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-[0.2em] uppercase">
      {children}
    </div>
  );
}

function Hero() {
  return (
    <section className="py-16 sm:py-24">
      <Eyebrow>The Platform</Eyebrow>
      <h1 className="font-heading text-cc-heading mt-5 max-w-4xl text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        Build, observe, evolve.{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM }}
        >
          Then let agents help.
        </span>
      </h1>
      <p className="text-cc-ink mt-7 max-w-2xl text-lg leading-relaxed sm:text-xl">
        The ChilliCream platform is the developer loop for a GraphQL API. Author
        the schema, promote it through workflows, watch every operation in
        production, evolve without surprising the callers, then open the same
        control plane to coding agents.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-3">
        <SolidButton href="#loop">Walk the loop</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Open Nitro
        </OutlineButton>
      </div>
      <div className="text-cc-ink-dim mt-8 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
        <SignalRow label="Eight platform surfaces" />
        <SignalRow label="One Nitro control plane" />
        <SignalRow label="Open to your coding agents" />
      </div>
    </section>
  );
}

interface SignalRowProps {
  readonly label: string;
}

function SignalRow({ label }: SignalRowProps) {
  return (
    <span className="inline-flex items-center gap-2">
      <span className="text-cc-accent">
        <CheckIcon size={14} />
      </span>
      {label}
    </span>
  );
}

interface LoopRailProps {
  readonly beats: readonly Beat[];
}

function LoopRail({ beats }: LoopRailProps) {
  return (
    <ol className="border-cc-card-border flex flex-wrap items-center gap-x-3 gap-y-2 rounded-full border px-4 py-3 text-xs">
      {beats.map((beat, index) => (
        <li key={beat.number} className="flex items-center gap-3">
          <a
            href={`#beat-${beat.number}`}
            className="text-cc-ink hover:text-cc-accent inline-flex items-center gap-2 font-mono tracking-wider uppercase no-underline transition-colors"
          >
            <span
              className="inline-block h-2 w-2 rounded-full"
              style={{ backgroundColor: beat.tint }}
              aria-hidden
            />
            <span className="text-cc-nav-label">{beat.number}</span>
            <span>{beat.verb}</span>
          </a>
          {index < beats.length - 1 && (
            <span className="text-cc-ink-faint" aria-hidden>
              ›
            </span>
          )}
        </li>
      ))}
    </ol>
  );
}

interface BeatBlockProps {
  readonly beat: Beat;
  readonly index: number;
  readonly children?: ReactNode;
}

function BeatBlock({ beat, index, children }: BeatBlockProps) {
  const flipped = index % 2 === 1;
  return (
    <section
      id={`beat-${beat.number}`}
      className="relative scroll-mt-24 py-14 sm:py-20"
    >
      <div
        className={`grid items-start gap-8 lg:grid-cols-12 lg:gap-12 ${
          flipped ? "lg:[&>div:first-child]:order-2" : ""
        }`}
      >
        {/* Left: number + verb + lede */}
        <div className="lg:col-span-5">
          <div className="flex items-baseline gap-4">
            <span
              className="font-heading text-5xl font-semibold tracking-tight sm:text-6xl"
              style={{ color: beat.tint }}
              aria-hidden
            >
              {beat.number}
            </span>
            <Eyebrow>{beat.verb}</Eyebrow>
          </div>
          <h2 className="font-heading text-cc-heading mt-5 text-3xl leading-tight font-semibold tracking-tight sm:text-4xl">
            {beat.title}
          </h2>
          <p className="text-cc-ink mt-5 text-base leading-relaxed sm:text-lg">
            {beat.lede}
          </p>
          <div className="mt-6">
            <Link
              href={beat.href}
              className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1 text-sm font-medium no-underline"
            >
              Go to {beat.verb.toLowerCase()}
              <span aria-hidden>→</span>
            </Link>
          </div>
        </div>

        {/* Right: moves list + optional embed */}
        <div className="lg:col-span-7">
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6 backdrop-blur-sm sm:p-8">
            <Eyebrow>What it gives you</Eyebrow>
            <ul className="mt-5 space-y-3">
              {beat.moves.map((move) => (
                <li key={move} className="flex items-start gap-3">
                  <span
                    className="mt-1.5 inline-block h-1.5 w-1.5 shrink-0 rounded-full"
                    style={{ backgroundColor: beat.tint }}
                    aria-hidden
                  />
                  <span className="text-cc-ink text-sm leading-relaxed sm:text-base">
                    {move}
                  </span>
                </li>
              ))}
            </ul>
          </div>
          {children}
        </div>
      </div>
    </section>
  );
}

function ObserveEmbed() {
  return (
    <div className="mx-auto mt-6 max-w-5xl">
      <div className="border-cc-card-border bg-cc-surface/60 overflow-hidden rounded-xl border">
        <NitroMonitoring />
      </div>
      <p className="text-cc-ink-dim mt-3 text-xs">
        Nitro monitoring, the control plane every beat of the loop reports into.
        Live preview, not a screenshot.
      </p>
    </div>
  );
}

function NitroCallout() {
  return (
    <section className="py-16 sm:py-20">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 backdrop-blur-sm sm:p-12">
        <div className="grid items-center gap-8 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-7">
            <Eyebrow>Nitro, the control plane</Eyebrow>
            <h2 className="font-heading text-cc-heading mt-4 text-3xl leading-tight font-semibold tracking-tight sm:text-4xl">
              Every beat of the loop reports into one place.
            </h2>
            <p className="text-cc-ink mt-5 text-base leading-relaxed sm:text-lg">
              Schemas, operations, traces, deprecations, and agent activity land
              in Nitro. It is the surface your team opens in the morning and the
              API your tooling talks to.
            </p>
            <ul className="text-cc-ink mt-6 space-y-3 text-sm sm:text-base">
              <NitroBullet>
                Operation catalog, indexed by name and shape
              </NitroBullet>
              <NitroBullet>
                Field-usage history per published client
              </NitroBullet>
              <NitroBullet>
                MCP endpoints for coding agents and CI bots
              </NitroBullet>
            </ul>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://nitro.chillicream.com">
                Open Nitro
              </SolidButton>
              <OutlineButton href="/products/nitro">
                Learn about Nitro
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <NitroTokenBlock />
          </div>
        </div>
      </div>
    </section>
  );
}

interface NitroBulletProps {
  readonly children: ReactNode;
}

function NitroBullet({ children }: NitroBulletProps) {
  return (
    <li className="flex items-start gap-3">
      <span className="text-cc-accent mt-0.5">
        <CheckIcon size={16} />
      </span>
      <span>{children}</span>
    </li>
  );
}

/**
 * Decorative token block: stacks the five beat tints as a vertical bar with
 * each beat's verb. Static SVG, no animation, cc-* tokens only.
 */
function NitroTokenBlock() {
  return (
    <div className="border-cc-card-border bg-cc-bg/60 rounded-xl border p-6">
      <Eyebrow>What lands in Nitro</Eyebrow>
      <ul className="mt-5 space-y-2">
        {BEATS.map((beat) => (
          <li
            key={beat.number}
            className="flex items-center justify-between gap-4 py-1"
          >
            <div className="flex items-center gap-3">
              <span
                className="inline-block h-2.5 w-2.5 rounded-full"
                style={{ backgroundColor: beat.tint }}
                aria-hidden
              />
              <span className="text-cc-nav-label font-mono text-xs tracking-widest uppercase">
                {beat.number}
              </span>
              <span className="text-cc-ink text-sm">{beat.verb}</span>
            </div>
            <span className="text-cc-ink-dim font-mono text-[11px] tracking-wider uppercase">
              streamed
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function MoreToExplore() {
  return (
    <section className="py-16 sm:py-20">
      <div className="flex items-baseline justify-between gap-6">
        <h2 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight sm:text-3xl">
          More to explore
        </h2>
        <Eyebrow>Adjacent surfaces</Eyebrow>
      </div>
      <p className="text-cc-ink-dim mt-3 max-w-2xl text-sm sm:text-base">
        Three surfaces that sit next to the loop. Analytics reads it, Continuous
        Integration gates it, and the Ecosystem is what it is built on.
      </p>
      <ul className="mt-8 grid gap-4 sm:grid-cols-3">
        {EXPLORE.map((item) => (
          <li key={item.href}>
            <Link
              href={item.href}
              className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
            >
              <h3 className="font-heading text-cc-heading group-hover:text-cc-accent text-lg font-semibold tracking-tight transition-colors">
                {item.title}
              </h3>
              <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">
                {item.blurb}
              </p>
              <span className="text-cc-accent mt-6 inline-flex items-center gap-1 text-sm font-medium">
                Open {item.title.toLowerCase()}
                <span aria-hidden>→</span>
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="py-20 text-center sm:py-28">
      <Eyebrow>Start the loop</Eyebrow>
      <h2 className="font-heading text-cc-heading mx-auto mt-5 max-w-3xl text-4xl leading-tight font-semibold tracking-tight sm:text-5xl">
        One platform for the API that everything depends on.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-base sm:text-lg">
        Start by building the schema, or open Nitro and watch the loop run on a
        graph that already exists.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/platform/build">Start with Build</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Open Nitro
        </OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformLoopPage() {
  return (
    <>
      <Hero />

      <div id="loop" className="scroll-mt-24">
        <div className="sticky top-4 z-10 mb-4 flex justify-center">
          <LoopRail beats={BEATS} />
        </div>

        <div className="border-cc-card-border divide-cc-card-border divide-y border-y">
          {BEATS.map((beat, index) => (
            <BeatBlock key={beat.number} beat={beat} index={index}>
              {beat.verb === "Observe" ? <ObserveEmbed /> : null}
            </BeatBlock>
          ))}
        </div>
      </div>

      <NitroCallout />
      <MoreToExplore />
      <ClosingCta />
    </>
  );
}
