import Link from "next/link";
import type { ComponentType } from "react";

import { AgenticIllu } from "@/src/components/home/platform/illustrations/AgenticIllu";
import { BuildIllu } from "@/src/components/home/platform/illustrations/BuildIllu";
import { GuardrailsIllu } from "@/src/components/home/platform/illustrations/GuardrailsIllu";
import { ObserveIllu } from "@/src/components/home/platform/illustrations/ObserveIllu";
import { WorkflowsIllu } from "@/src/components/home/platform/illustrations/WorkflowsIllu";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

interface IlluProps {
  readonly className?: string;
}

interface Topic {
  readonly id: string;
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly href: string;
  readonly Illu: ComponentType<IlluProps>;
}

// The five platform topics, read top to bottom as one continuous loop: the code
// you write, the agents that extend it, the telemetry that watches it, the
// workflows behind it, and the checks that keep releases safe.
const TOPICS: readonly Topic[] = [
  {
    id: "build",
    eyebrow: "Build loop",
    headline: "Ship from the code that runs it.",
    blurb:
      "Write your API as annotated C#, and the schema, resolvers, batching, and typed clients all stay in step with it, so the contract you publish is the code that answers the request.",
    href: "/platform/build",
    Illu: BuildIllu,
  },
  {
    id: "agentic",
    eyebrow: "Agentic coding",
    headline: "Give coding agents a feedback loop.",
    blurb:
      "Ground agents in the operations your clients already use, gate the risky calls, and turn every fast edit into feedback you can trust before it touches production.",
    href: "/platform/agentic-coding",
    Illu: AgenticIllu,
  },
  {
    id: "production",
    eyebrow: "Production view",
    headline: "See what the API is doing.",
    blurb:
      "Nitro turns live traffic into operation metrics, distributed traces, and impact scores, so when latency climbs you debug from evidence instead of starting another dashboard project.",
    href: "/platform/observability",
    Illu: ObserveIllu,
  },
  {
    id: "workflow",
    eyebrow: "Workflow",
    headline: "Let work continue after the request.",
    blurb:
      "Hand the slow, fan-out, cross-service work to Mocha as commands and events, so the user gets a response now while the rest keeps moving on its own.",
    href: "/platform/workflows",
    Illu: WorkflowsIllu,
  },
  {
    id: "release",
    eyebrow: "Release safety",
    headline: "Change contracts with a safety net.",
    blurb:
      "Every schema change is classified safe, dangerous, or breaking and checked against the clients you have actually published, so unsafe releases stop at the gate, not in production.",
    href: "/platform/release-safety",
    Illu: GuardrailsIllu,
  },
];

/**
 * One full-width topic row in the alternating-rows layout. The illustration sits
 * in a framed figure panel and the copy fills the other column; `flip` swaps the
 * two columns on large screens so the figure side alternates row to row. On
 * mobile both stack with the figure above the copy.
 */
function TopicRow({
  topic,
  flip,
}: {
  readonly topic: Topic;
  readonly flip: boolean;
}) {
  const { Illu } = topic;

  return (
    <RevealOnScroll className="grid grid-cols-1 items-center gap-10 lg:grid-cols-2 lg:gap-16">
      <div
        className={[
          "border-cc-card-border bg-cc-card-bg/40 hover:border-cc-card-border-hover rounded-3xl border p-5 transition-colors",
          flip ? "lg:order-2" : "lg:order-1",
        ].join(" ")}
      >
        <Illu className="w-full max-w-md" />
      </div>

      <div className={flip ? "lg:order-1" : "lg:order-2"}>
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          {topic.eyebrow}
        </span>
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
          {topic.headline}
        </h3>
        <p className="text-cc-ink mt-5 max-w-xl text-base text-pretty sm:text-lg">
          {topic.blurb}
        </p>
        <Link
          href={topic.href}
          className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
        >
          Learn more
          <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>
    </RevealOnScroll>
  );
}

/**
 * Platform section, "Alternating Rows" take: an overarching frame above five
 * full-width topic rows. Every topic's eyebrow, headline, blurb, illustration,
 * and jump link is visible at once; the figure column alternates sides per row
 * for an editorial rhythm. Static server component, nothing hidden behind
 * interaction.
 */
export function PlatformSectionV1() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          The platform
        </p>
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
          One platform around the whole API loop.
        </h2>
        <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
          From the code you write, to the agents that extend it, the telemetry
          that watches it, the workflows behind it, and the checks that keep
          releases safe, ChilliCream keeps every feedback loop close. Here is
          the whole loop, before you pick a plan.
        </p>
      </RevealOnScroll>

      <div className="mt-14 space-y-16 sm:mt-20 sm:space-y-24">
        {TOPICS.map((topic, index) => (
          <TopicRow key={topic.id} topic={topic} flip={index % 2 === 1} />
        ))}
      </div>
    </section>
  );
}
