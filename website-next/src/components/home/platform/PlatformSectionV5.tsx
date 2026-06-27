import Link from "next/link";
import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { AgenticIllu } from "@/src/components/home/platform/illustrations/AgenticIllu";
import { BuildIllu } from "@/src/components/home/platform/illustrations/BuildIllu";
import { GuardrailsIllu } from "@/src/components/home/platform/illustrations/GuardrailsIllu";
import { ObserveIllu } from "@/src/components/home/platform/illustrations/ObserveIllu";
import { WorkflowsIllu } from "@/src/components/home/platform/illustrations/WorkflowsIllu";

interface ArrowRightProps {
  readonly className?: string;
}

function ArrowRight({ className }: ArrowRightProps) {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className={["h-3.5 w-3.5", className ?? ""].filter(Boolean).join(" ")}
    >
      <path
        d="M2.75 8h9.5M8.75 4.25 12.5 8l-3.75 3.75"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

interface CalloutProps {
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly href: string;
  readonly illustration: ReactNode;
  readonly className?: string;
  readonly wide?: boolean;
}

/** Body of a callout node: eyebrow, headline, one-line blurb, and an Open link. */
function CalloutText({
  eyebrow,
  headline,
  blurb,
}: {
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
}) {
  return (
    <div className="flex flex-1 flex-col">
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.12em] uppercase">
        {eyebrow}
      </p>
      <h3 className="font-heading text-cc-heading text-h6 sm:text-h5 group-hover:text-cc-accent mt-2 leading-[1.15] font-semibold text-balance transition-colors duration-300">
        {headline}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm text-pretty">{blurb}</p>
      <span className="text-cc-accent mt-4 inline-flex items-center gap-1.5 text-sm font-medium">
        Open {eyebrow}
        <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
      </span>
    </div>
  );
}

/**
 * One callout node on the map: a link card carrying its topic's eyebrow,
 * headline, blurb, jump-off, and shared illustration. The `wide` variant lays
 * the illustration beside the text for the full-width foundation node.
 */
function Callout({
  eyebrow,
  headline,
  blurb,
  href,
  illustration,
  className,
  wide = false,
}: CalloutProps) {
  return (
    <Link
      href={href}
      className={[
        "group bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover focus-visible:outline-cc-accent/60 relative z-10 flex flex-col rounded-2xl border p-5 backdrop-blur-sm transition-colors duration-300 focus-visible:outline-2 focus-visible:outline-offset-2",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {wide ? (
        <div className="grid items-center gap-6 lg:grid-cols-2">
          <CalloutText eyebrow={eyebrow} headline={headline} blurb={blurb} />
          <div className="order-first lg:order-last">{illustration}</div>
        </div>
      ) : (
        <>
          <div className="mb-5">{illustration}</div>
          <CalloutText eyebrow={eyebrow} headline={headline} blurb={blurb} />
        </>
      )}
    </Link>
  );
}

interface CoreRingProps {
  readonly className?: string;
}

// Five evenly spaced nodes on the loop ring, one per platform stage. The top
// node is the primary (where the loop is read to begin) and is teal-filled.
const RING_NODES: readonly {
  readonly x: number;
  readonly y: number;
  readonly primary?: boolean;
}[] = [
  { x: 60, y: 18, primary: true },
  { x: 100, y: 47 },
  { x: 84.7, y: 94 },
  { x: 35.3, y: 94 },
  { x: 20, y: 47 },
];

/**
 * The core glyph: a continuous teal flow arc threading five nodes on a faint
 * ring, conveying the closed API loop the five callouts each belong to.
 */
function CoreRing({ className }: CoreRingProps) {
  return (
    <svg
      viewBox="0 0 120 120"
      fill="none"
      aria-hidden="true"
      className={["mx-auto", className ?? ""].filter(Boolean).join(" ")}
    >
      <defs>
        <radialGradient id="v5-core-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0" stopColor="#5eead4" stopOpacity="0.16" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
        <marker
          id="v5-core-arrow"
          markerWidth="6"
          markerHeight="6"
          refX="3"
          refY="3"
          orient="auto"
          markerUnits="userSpaceOnUse"
        >
          <path
            d="M0 0.8 L4 3 L0 5.2"
            fill="none"
            stroke="#5eead4"
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </marker>
      </defs>

      {/* soft teal aura behind the ring */}
      <circle cx="60" cy="60" r="48" fill="url(#v5-core-glow)" />

      {/* the faint structural ring */}
      <circle
        cx="60"
        cy="60"
        r="42"
        stroke="rgba(245,241,234,0.16)"
        strokeWidth="1"
        strokeDasharray="2 4"
      />

      {/* the teal flow: a near-complete clockwise arc that closes the loop */}
      <path
        d="M60 18 A 42 42 0 1 1 20 47"
        stroke="#5eead4"
        strokeOpacity="0.7"
        strokeWidth="1.5"
        strokeLinecap="round"
        markerEnd="url(#v5-core-arrow)"
      />

      {/* the five stage nodes */}
      {RING_NODES.map((node) => (
        <g key={`${node.x}-${node.y}`}>
          {node.primary && (
            <circle
              cx={node.x}
              cy={node.y}
              r="8"
              fill="#5eead4"
              opacity="0.18"
            />
          )}
          <circle
            cx={node.x}
            cy={node.y}
            r="4.5"
            fill="#0c1322"
            stroke="#5eead4"
            strokeWidth="1.4"
          />
          {node.primary && (
            <circle cx={node.x} cy={node.y} r="2" fill="#5eead4" />
          )}
        </g>
      ))}
    </svg>
  );
}

// Connector endpoints in the 100x100 overlay, one per callout, ordered to match
// the desktop grid: top-left, top-right, mid-left, mid-right, bottom-center.
// The hub sits at the vertical center of the two stacked side rows. The first
// spoke (Build, where the loop begins) is the single teal primary path.
const HUB = { x: 50, y: 38 } as const;
const SPOKES: readonly { readonly x: number; readonly y: number }[] = [
  { x: 17, y: 16 },
  { x: 83, y: 16 },
  { x: 17, y: 60 },
  { x: 83, y: 60 },
  { x: 50, y: 88 },
];

/**
 * Platform jump-off, take v5 ("Architecture Map"): the whole API loop drawn as
 * one connected diagram. A central loop core sits in the middle of a desktop
 * grid with the five platform stages arranged around it as link callouts, each
 * tied back to the core by a thin connector. Every callout's eyebrow, headline,
 * blurb, illustration, and jump-off is visible at once, nothing hidden behind
 * interaction. Sits between the protocol section and pricing on the shared dark
 * canvas and leads the eye down into the plans.
 */
export function PlatformSectionV5() {
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
            releases safe, ChilliCream keeps every feedback loop close. Here is
            the whole loop, before you pick a plan.
          </p>
        </div>

        {/* The map: a connector overlay behind a grid that places the loop core
            in the middle with the five stage callouts around it. */}
        <div className="relative mt-12 sm:mt-16">
          <svg
            viewBox="0 0 100 100"
            preserveAspectRatio="none"
            fill="none"
            aria-hidden="true"
            className="pointer-events-none absolute inset-0 hidden h-full w-full lg:block"
          >
            {SPOKES.map((spoke, index) => (
              <line
                key={`${spoke.x}-${spoke.y}`}
                x1={HUB.x}
                y1={HUB.y}
                x2={spoke.x}
                y2={spoke.y}
                stroke={index === 0 ? "#5eead4" : "rgba(245,241,234,0.16)"}
                strokeOpacity={index === 0 ? 0.5 : 1}
                strokeWidth={index === 0 ? 1.25 : 1}
                strokeLinecap="round"
                vectorEffect="non-scaling-stroke"
              />
            ))}
          </svg>

          <div className="relative grid grid-cols-1 gap-5 lg:grid-cols-3 lg:gap-6">
            {/* Core, first in the DOM so it leads on mobile, centered on desktop. */}
            <div className="bg-cc-card-bg border-cc-card-border relative z-10 flex flex-col items-center justify-center rounded-3xl border p-6 text-center backdrop-blur-sm lg:col-start-2 lg:row-span-2 lg:row-start-1">
              <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.2em] uppercase">
                The API loop
              </p>
              <CoreRing className="mt-5 w-full max-w-[200px]" />
              <h3 className="font-heading text-cc-heading text-h5 sm:text-h4 mt-6 leading-[1.1] font-semibold text-balance">
                Five stages, one loop.
              </h3>
              <p className="text-cc-ink-dim mt-3 max-w-xs text-sm text-pretty">
                Each callout is one stage of the same loop. A change in one
                shows up in the others, so the feedback never has far to travel.
              </p>
            </div>

            <Callout
              href="/platform/build"
              eyebrow="Build loop"
              headline="Ship from the code that runs it."
              blurb="Write your API as annotated C#, and the schema, resolvers, batching, and typed clients all stay in step with it."
              illustration={<BuildIllu className="w-full" />}
              className="lg:col-start-1 lg:row-start-1"
            />

            <Callout
              href="/platform/agentic-coding"
              eyebrow="Agentic coding"
              headline="Give coding agents a feedback loop."
              blurb="Ground agents in the operations your clients already use, and turn every fast edit into feedback you can trust."
              illustration={<AgenticIllu className="w-full" />}
              className="lg:col-start-3 lg:row-start-1"
            />

            <Callout
              href="/platform/observability"
              eyebrow="Production view"
              headline="See what the API is doing."
              blurb="Nitro turns live traffic into operation metrics, distributed traces, and impact scores, so you debug from evidence."
              illustration={<ObserveIllu className="w-full" />}
              className="lg:col-start-1 lg:row-start-2"
            />

            <Callout
              href="/platform/workflows"
              eyebrow="Workflow"
              headline="Let work continue after the request."
              blurb="Hand the slow, fan-out, cross-service work to Mocha, so the user gets a response now while the rest keeps moving."
              illustration={<WorkflowsIllu className="w-full" />}
              className="lg:col-start-3 lg:row-start-2"
            />

            <Callout
              wide
              href="/platform/release-safety"
              eyebrow="Release safety"
              headline="Change contracts with a safety net."
              blurb="Every schema change is classified safe, dangerous, or breaking and checked against the clients you have actually published, so unsafe releases stop at the gate."
              illustration={<GuardrailsIllu className="w-full max-w-md" />}
              className="lg:col-span-3 lg:col-start-1 lg:row-start-3"
            />
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}
