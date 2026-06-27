import Link from "next/link";
import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { AgenticIllu } from "@/src/components/home/platform/illustrations/AgenticIllu";
import { BuildIllu } from "@/src/components/home/platform/illustrations/BuildIllu";
import { GuardrailsIllu } from "@/src/components/home/platform/illustrations/GuardrailsIllu";
import { ObserveIllu } from "@/src/components/home/platform/illustrations/ObserveIllu";
import { WorkflowsIllu } from "@/src/components/home/platform/illustrations/WorkflowsIllu";

// All five tiles share one base. The whole tile is a next/link, the only
// interactive flourish is a hover lift plus the arrow nudge on its Open
// affordance. Everything (eyebrow, headline, one line, illustration, jump-off)
// is on screen at once, nothing hidden behind interaction.
const TILE_BASE =
  "group relative flex h-full flex-col overflow-hidden rounded-3xl border border-cc-card-border bg-cc-card-bg p-6 no-underline transition duration-300 hover:-translate-y-1 hover:border-cc-card-border-hover";

// One shared, capped illustration size keeps the bento tight: the wide tiles get
// a centered diagram rather than a stretched one, so the whole section stays
// roughly the height of a single take.
const ILLU_CLASS = "w-full max-w-sm";

interface Topic {
  readonly eyebrow: string;
  readonly headline: string;
  readonly line: string;
  readonly href: string;
  readonly span: string;
  readonly accent: boolean;
  readonly illustration: ReactNode;
}

// The five stages of the one API loop, shown once. Footprints make the rhythm:
// three equal tiles up top, two wide tiles below. Build is the primary, where
// the loop begins, and carries the section's single teal-forward moment.
const TOPICS: readonly Topic[] = [
  {
    eyebrow: "Build loop",
    headline: "Ship from the code that runs it.",
    line: "Annotated C# keeps the schema, resolvers, and typed clients in step.",
    href: "/platform/build",
    span: "lg:col-span-2",
    accent: true,
    illustration: <BuildIllu className={ILLU_CLASS} />,
  },
  {
    eyebrow: "Agentic coding",
    headline: "Give coding agents a feedback loop.",
    line: "Ground agents in published operations and gate the risky calls.",
    href: "/platform/agentic-coding",
    span: "lg:col-span-2",
    accent: false,
    illustration: <AgenticIllu className={ILLU_CLASS} />,
  },
  {
    eyebrow: "Production view",
    headline: "See what the API is doing.",
    line: "Live traffic becomes operation metrics, traces, and impact scores.",
    href: "/platform/observability",
    span: "lg:col-span-2",
    accent: false,
    illustration: <ObserveIllu className={ILLU_CLASS} />,
  },
  {
    eyebrow: "Workflow",
    headline: "Let work continue after the request.",
    line: "Hand slow, fan-out work to Mocha so the response returns now.",
    href: "/platform/workflows",
    span: "lg:col-span-3",
    accent: false,
    illustration: <WorkflowsIllu className={ILLU_CLASS} />,
  },
  {
    eyebrow: "Release safety",
    headline: "Change contracts with a safety net.",
    line: "Every change is classified and checked against the clients you published.",
    href: "/platform/release-safety",
    span: "sm:col-span-2 lg:col-span-3",
    accent: false,
    illustration: <GuardrailsIllu className={ILLU_CLASS} />,
  },
];

interface OpenLinkProps {
  readonly accent: boolean;
}

/** Jump-off affordance shown in every tile. The parent tile is the real link, so
 *  this is a styled span: teal at rest on the primary tile, teal on hover
 *  elsewhere. */
function OpenLink({ accent }: OpenLinkProps) {
  return (
    <span
      className={[
        "relative mt-auto inline-flex items-center gap-1.5 pt-5 text-sm font-medium transition-colors",
        accent
          ? "text-cc-accent group-hover:text-cc-accent-hover"
          : "text-cc-ink-dim group-hover:text-cc-accent",
      ].join(" ")}
    >
      Open
      <span
        aria-hidden="true"
        className="transition-transform group-hover:translate-x-0.5"
      >
        &rarr;
      </span>
    </span>
  );
}

interface TopicTileProps {
  readonly topic: Topic;
}

/** One stage of the loop: eyebrow, kept sub-headline, one line, a shrunk shared
 *  illustration, and a jump-off into that stage's page. */
function TopicTile({ topic }: TopicTileProps) {
  return (
    <Link href={topic.href} className={`${TILE_BASE} ${topic.span}`}>
      {topic.accent && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-20 -right-16 h-56 w-56 rounded-full"
          style={{
            background:
              "radial-gradient(circle, rgba(94,234,212,0.14) 0%, rgba(94,234,212,0) 70%)",
          }}
        />
      )}

      <div className="relative">
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.12em] uppercase">
          {topic.eyebrow}
        </span>
        <h3 className="font-heading text-cc-heading text-h6 group-hover:text-cc-accent mt-2.5 leading-[1.15] font-semibold text-balance transition-colors duration-300">
          {topic.headline}
        </h3>
        <p className="text-cc-ink-dim mt-2 text-sm text-pretty">{topic.line}</p>
      </div>

      <div className="relative mt-5 flex flex-1 items-center justify-center">
        {topic.illustration}
      </div>

      <OpenLink accent={topic.accent} />
    </Link>
  );
}

/**
 * Combined Platform section: one compact take on the whole API loop. A single
 * shared header introduces the loop, then the five stages (Build, Agentic
 * coding, Production view, Workflow, Release safety) are each shown once in a
 * 3-plus-2 bento. Every tile carries its stage's eyebrow, kept sub-headline, one
 * line, a shrunk version of that stage's shared illustration, and an Open link
 * into the stage page. Sits on the shared dark canvas between the protocol
 * section and pricing.
 */
export function CombinedPlatform() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            The platform
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            One platform around the whole API loop.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            From the code you write, to the agents that extend it, the telemetry
            that watches it, the workflows behind it, and the checks that keep
            releases safe.
          </p>
          <Link
            href="/platform"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        <div className="mt-12 grid grid-cols-1 gap-4 sm:mt-14 sm:grid-cols-2 sm:gap-5 lg:grid-cols-6">
          {TOPICS.map((topic) => (
            <TopicTile key={topic.eyebrow} topic={topic} />
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
