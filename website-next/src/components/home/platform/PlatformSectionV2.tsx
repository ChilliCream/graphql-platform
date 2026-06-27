import Link from "next/link";
import type { ComponentType } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { AgenticIllu } from "@/src/components/home/platform/illustrations/AgenticIllu";
import { BuildIllu } from "@/src/components/home/platform/illustrations/BuildIllu";
import { GuardrailsIllu } from "@/src/components/home/platform/illustrations/GuardrailsIllu";
import { ObserveIllu } from "@/src/components/home/platform/illustrations/ObserveIllu";
import { WorkflowsIllu } from "@/src/components/home/platform/illustrations/WorkflowsIllu";

type IlluComponent = ComponentType<{ readonly className?: string }>;

interface LoopTopic {
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly href: string;
  readonly Illu: IlluComponent;
}

// The five platform stations in flow order. The loop closes when the last one
// (release safety) feeds back into the first (build): build -> agentic ->
// observe -> workflow -> release -> build.
const TOPICS: readonly LoopTopic[] = [
  {
    eyebrow: "Build loop",
    headline: "Ship from the code that runs it.",
    blurb:
      "Schema, resolvers, batching, and typed clients all stay in step with your annotated C#.",
    href: "/platform/build",
    Illu: BuildIllu,
  },
  {
    eyebrow: "Agentic coding",
    headline: "Give coding agents a feedback loop.",
    blurb:
      "Ground agents in the operations clients already use, and gate the risky calls.",
    href: "/platform/agentic-coding",
    Illu: AgenticIllu,
  },
  {
    eyebrow: "Production view",
    headline: "See what the API is doing.",
    blurb:
      "Live traffic becomes operation metrics, traces, and impact scores you can act on.",
    href: "/platform/observability",
    Illu: ObserveIllu,
  },
  {
    eyebrow: "Workflow",
    headline: "Let work continue after the request.",
    blurb:
      "Hand slow, fan-out, cross-service work to Mocha so the user gets a response now.",
    href: "/platform/workflows",
    Illu: WorkflowsIllu,
  },
  {
    eyebrow: "Release safety",
    headline: "Change contracts with a safety net.",
    blurb:
      "Every change is classified and checked against the clients you have published.",
    href: "/platform/release-safety",
    Illu: GuardrailsIllu,
  },
];

const RETURN_GRADIENT_ID = "platform-loop-return";

/** Two-digit, zero-padded station number (for example "03"). */
function pad(value: number): string {
  return String(value).padStart(2, "0");
}

/** Horizontal connector with an arrowhead. `flip` points it leftward. */
function FlowArrow({ flip = false }: { readonly flip?: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={[
        "text-cc-ink-faint flex shrink-0 items-center self-center px-1.5",
        flip ? "rotate-180" : "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <svg width="34" height="10" viewBox="0 0 34 10" fill="none">
        <path
          d="M0 5h29"
          stroke="currentColor"
          strokeWidth="1.25"
          strokeLinecap="round"
        />
        <path
          d="M26 1.5 L30 5 L26 8.5"
          stroke="currentColor"
          strokeWidth="1.25"
          strokeLinecap="round"
          strokeLinejoin="round"
          fill="none"
        />
      </svg>
    </span>
  );
}

/** Vertical connector with a downward arrowhead. */
function DownArrow({ className }: { readonly className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={["text-cc-ink-faint flex justify-center", className ?? ""]
        .filter(Boolean)
        .join(" ")}
    >
      <svg width="10" height="28" viewBox="0 0 10 28" fill="none">
        <path
          d="M5 0v23"
          stroke="currentColor"
          strokeWidth="1.25"
          strokeLinecap="round"
        />
        <path
          d="M1.5 20 L5 24 L8.5 20"
          stroke="currentColor"
          strokeWidth="1.25"
          strokeLinecap="round"
          strokeLinejoin="round"
          fill="none"
        />
      </svg>
    </span>
  );
}

interface StationCardProps {
  readonly topic: LoopTopic;
  readonly index: number;
  readonly className?: string;
}

/**
 * One loop station: number, eyebrow, headline, illustration, blurb, jump-off.
 * The whole card is the link to the topic.
 */
function StationCard({ topic, index, className }: StationCardProps) {
  const Illu = topic.Illu;

  return (
    <Link
      href={topic.href}
      className={[
        "group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-5 transition-colors",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <div className="flex items-center gap-2.5">
        <span className="text-cc-accent font-mono text-xs tabular-nums">
          {pad(index)}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.12em] uppercase">
          {topic.eyebrow}
        </span>
      </div>

      <h3 className="font-heading text-cc-heading text-h6 sm:text-h5 mt-2 leading-[1.15] font-semibold text-balance">
        {topic.headline}
      </h3>

      <div className="mt-4">
        <Illu className="mx-auto w-full max-w-sm" />
      </div>

      <p className="text-cc-ink mt-4 text-sm text-pretty">{topic.blurb}</p>

      <span className="text-cc-accent group-hover:text-cc-accent-hover mt-auto inline-flex items-center gap-1.5 pt-4 text-sm font-medium transition-colors">
        Open
        <span
          aria-hidden="true"
          className="transition-transform group-hover:translate-x-0.5"
        >
          &rarr;
        </span>
      </span>
    </Link>
  );
}

/**
 * Platform section, take "The Loop". The five platform stations are all visible
 * at once as connected cards. On large screens they read as a closed loop:
 * build -> agentic -> observe across the top, a down turn into workflow,
 * release across the bottom, and a single brand-spectrum return arc sweeping
 * back into build. On small screens they stack vertically with a return hint.
 * Nothing is hidden behind interaction.
 */
export function PlatformSectionV2() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="mx-auto max-w-3xl text-center">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            The platform
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            One platform around the whole API loop.
          </h2>
          <p className="text-cc-ink mx-auto mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            From the code you write, to the agents that extend it, the telemetry
            that watches it, the workflows behind it, and the checks that keep
            releases safe, ChilliCream keeps every feedback loop close. Here is
            the whole loop, before you pick a plan.
          </p>
        </div>

        {/* Desktop loop: two serpentine rows joined by connectors, closed by the
            single brand-spectrum return arc on the left rail. */}
        <div className="relative mt-16 hidden px-12 lg:block">
          <svg
            aria-hidden="true"
            viewBox="0 0 100 100"
            preserveAspectRatio="none"
            fill="none"
            className="pointer-events-none absolute inset-0 h-full w-full"
          >
            <defs>
              <linearGradient
                id={RETURN_GRADIENT_ID}
                gradientUnits="userSpaceOnUse"
                x1="0"
                y1="100"
                x2="0"
                y2="0"
              >
                <stop offset="0" stopColor="#f0786a" />
                <stop offset="0.55" stopColor="#b681a9" />
                <stop offset="1" stopColor="#16b9e4" />
              </linearGradient>
            </defs>
            <path
              d="M50 95 C 22 100 3 88 3 60 C 3 32 3 14 13 6"
              stroke={`url(#${RETURN_GRADIENT_ID})`}
              strokeWidth="1.5"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
            />
          </svg>

          {/* return arc arrowhead, in the left rail, pointing back up into build */}
          <span
            aria-hidden="true"
            className="text-cc-accent absolute top-0 left-[1.5%] z-10"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M3 9 L8 4 L13 9"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </span>

          {/* row 1: build -> agentic -> observe */}
          <div className="flex items-stretch">
            <StationCard
              topic={TOPICS[0]}
              index={1}
              className="min-w-0 flex-1 basis-0"
            />
            <FlowArrow />
            <StationCard
              topic={TOPICS[1]}
              index={2}
              className="min-w-0 flex-1 basis-0"
            />
            <FlowArrow />
            <StationCard
              topic={TOPICS[2]}
              index={3}
              className="min-w-0 flex-1 basis-0"
            />
          </div>

          {/* down turn under the right column: observe -> workflow */}
          <div className="flex justify-end pr-[15%]">
            <DownArrow className="py-3" />
          </div>

          {/* row 2 (serpentine): release <- workflow, with the return caption
              filling the empty bottom-left cell */}
          <div className="flex items-stretch">
            <div className="flex min-w-0 flex-1 basis-0 items-end justify-center px-2 pb-6">
              <span className="text-cc-ink-dim text-center font-mono text-[0.6rem] tracking-[0.1em]">
                release feeds back into build
              </span>
            </div>
            <StationCard
              topic={TOPICS[4]}
              index={5}
              className="min-w-0 flex-1 basis-0"
            />
            <FlowArrow flip />
            <StationCard
              topic={TOPICS[3]}
              index={4}
              className="min-w-0 flex-1 basis-0"
            />
          </div>
        </div>

        {/* Mobile / tablet: vertical stack joined by a connecting line, with a
            return hint after the last station. */}
        <div className="mt-12 lg:hidden">
          <div className="mx-auto flex max-w-md flex-col">
            {TOPICS.map((topic, i) => (
              <div key={topic.eyebrow} className="flex flex-col">
                <StationCard topic={topic} index={i + 1} />
                {i < TOPICS.length - 1 && <DownArrow className="py-2" />}
              </div>
            ))}

            <div className="mt-2 flex flex-col items-center">
              <span aria-hidden="true" className="text-cc-accent">
                <svg width="140" height="64" viewBox="0 0 140 64" fill="none">
                  <path
                    d="M70 2 C 70 26 70 38 40 38 C 18 38 12 30 12 14"
                    stroke="currentColor"
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    strokeDasharray="3 4"
                    fill="none"
                  />
                  <path
                    d="M7 19 L12 13 L17 19"
                    stroke="currentColor"
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    fill="none"
                  />
                </svg>
              </span>
              <span className="text-cc-ink-dim mt-1 font-mono text-[0.6rem] tracking-[0.1em]">
                release feeds back into build
              </span>
            </div>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}
