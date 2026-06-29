import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { AgenticIllu } from "@/src/components/home/platform/illustrations/AgenticIllu";
import { BuildIllu } from "@/src/components/home/platform/illustrations/BuildIllu";
import { GuardrailsIllu } from "@/src/components/home/platform/illustrations/GuardrailsIllu";
import { ObserveIllu } from "@/src/components/home/platform/illustrations/ObserveIllu";
import { WorkflowsIllu } from "@/src/components/home/platform/illustrations/WorkflowsIllu";

// Every tile is a whole-card next/link. Nothing is hidden behind interaction:
// each topic's eyebrow, headline, blurb, illustration, and jump link is on
// screen at once. The only interactive flourish is a hover lift plus the arrow
// and accent reveal on the "Open" link.
const TILE_BASE =
  "group relative flex h-full flex-col overflow-hidden rounded-3xl border border-cc-card-border bg-cc-card-bg no-underline transition duration-300 hover:-translate-y-1 hover:border-cc-card-border-hover";

interface TileEyebrowProps {
  readonly children: string;
}

/** Shrunk mono eyebrow tuned for the cards. */
function TileEyebrow({ children }: TileEyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.12em] uppercase">
      {children}
    </span>
  );
}

interface OpenLinkProps {
  readonly label: string;
  readonly accent?: boolean;
  readonly className?: string;
}

/** Jump-off affordance shown in every tile; teal at rest on the hero, teal on
 *  hover elsewhere. The parent tile is the actual link. */
function OpenLink({ label, accent = false, className }: OpenLinkProps) {
  return (
    <span
      className={[
        "inline-flex items-center gap-1.5 text-sm font-medium transition-colors",
        accent
          ? "text-cc-accent group-hover:text-cc-accent-hover"
          : "text-cc-ink-dim group-hover:text-cc-accent",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {label}
      <span
        aria-hidden="true"
        className="transition-transform group-hover:translate-x-0.5"
      >
        &rarr;
      </span>
    </span>
  );
}

/** Build loop: the 2x2 hero anchoring the mosaic, carrying the section's single
 *  teal-forward moment and the largest illustration. */
function BuildHeroTile() {
  return (
    <Link
      href="/platform/build"
      className={`${TILE_BASE} p-7 sm:col-span-2 sm:p-8 lg:col-span-2 lg:col-start-1 lg:row-span-2 lg:row-start-1`}
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-24 -right-20 h-64 w-64 rounded-full"
        style={{
          background:
            "radial-gradient(circle, rgba(94,234,212,0.14) 0%, rgba(94,234,212,0) 70%)",
        }}
      />

      <div className="relative">
        <TileEyebrow>Build loop</TileEyebrow>
        <h3 className="font-heading text-cc-heading text-h4 mt-3 leading-[1.1] font-semibold text-balance">
          Ship from the code that runs it.
        </h3>
        <p className="text-cc-ink mt-4 max-w-md text-base text-pretty">
          Write your API as annotated C#, and the schema, resolvers, batching,
          and typed clients all stay in step with it, so the contract you
          publish is the code that answers the request.
        </p>
      </div>

      <div className="relative mt-6 flex flex-1 items-center justify-center">
        <BuildIllu className="w-full max-w-lg" />
      </div>

      <OpenLink label="Open Build loop" accent className="relative pt-6" />
    </Link>
  );
}

/** Agentic coding: the 2x1 wide tile, text beside the approval-gate transcript. */
function AgenticTile() {
  return (
    <Link
      href="/platform/agentic-coding"
      className={`${TILE_BASE} p-5 sm:col-span-2 sm:p-6 lg:col-span-2 lg:col-start-3 lg:row-start-1`}
    >
      <div className="flex flex-col gap-5 sm:flex-row sm:items-center sm:gap-6">
        <div className="flex flex-col sm:flex-1">
          <TileEyebrow>Agentic coding</TileEyebrow>
          <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.15] font-semibold text-balance">
            Give coding agents a feedback loop.
          </h3>
          <p className="text-cc-ink-dim mt-3 text-sm text-pretty">
            Ground agents in the operations your clients already use, and gate
            the risky calls before they reach production.
          </p>
          <OpenLink label="Open Agentic coding" className="mt-5 pt-1 sm:mt-6" />
        </div>
        <AgenticIllu className="w-full sm:w-[15rem] sm:shrink-0 lg:w-[16rem]" />
      </div>
    </Link>
  );
}

/** Production view: the smallest 1x1 tile, carrying the trace waterfall. */
function ObserveTile() {
  return (
    <Link
      href="/platform/observability"
      className={`${TILE_BASE} p-5 sm:p-6 lg:col-start-3 lg:row-start-2`}
    >
      <TileEyebrow>Production view</TileEyebrow>
      <h3 className="font-heading text-cc-heading text-h6 mt-3 leading-[1.15] font-semibold text-balance">
        See what the API is doing.
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm text-pretty">
        Live traffic becomes operation metrics, distributed traces, and impact
        scores, so you debug from evidence.
      </p>
      <div className="mt-5 flex flex-1 items-center justify-center">
        <ObserveIllu className="w-full" />
      </div>
      <OpenLink label="Open Production view" className="pt-6" />
    </Link>
  );
}

/** Workflow: the 1x2 tall tile, giving the richest event-driven fan-out room. */
function WorkflowTile() {
  return (
    <Link
      href="/platform/workflows"
      className={`${TILE_BASE} p-5 sm:p-6 lg:col-start-4 lg:row-span-2 lg:row-start-2`}
    >
      <TileEyebrow>Workflow</TileEyebrow>
      <h3 className="font-heading text-cc-heading text-h6 mt-3 leading-[1.15] font-semibold text-balance">
        Let work continue after the request.
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm text-pretty">
        Hand the slow, fan-out, cross-service work to Mocha, so the user gets a
        response now while the rest keeps moving.
      </p>
      <div className="mt-6 flex flex-1 items-center justify-center">
        <WorkflowsIllu className="w-full" />
      </div>
      <OpenLink label="Open Workflow" className="pt-6" />
    </Link>
  );
}

/** Release safety: the 3x1 wide strip, text beside the schema-evolution rail. */
function ReleaseSafetyTile() {
  return (
    <Link
      href="/platform/release-safety"
      className={`${TILE_BASE} p-5 sm:col-span-2 sm:p-6 lg:col-span-3 lg:col-start-1 lg:row-start-3`}
    >
      <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:gap-10">
        <div className="flex flex-col lg:max-w-md lg:flex-1">
          <TileEyebrow>Release safety</TileEyebrow>
          <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.15] font-semibold text-balance">
            Change contracts with a safety net.
          </h3>
          <p className="text-cc-ink-dim mt-3 text-sm text-pretty">
            Every schema change is classified safe, dangerous, or breaking, then
            checked against the clients you have published, so unsafe releases
            stop at the gate, not in production.
          </p>
          <OpenLink label="Open Release safety" className="mt-5 pt-1 lg:mt-6" />
        </div>
        <GuardrailsIllu className="w-full lg:max-w-md lg:shrink-0" />
      </div>
    </Link>
  );
}

/**
 * Platform section, "Bento" take: an asymmetric, illustration-forward mosaic
 * that breaks the page's equal-grid habit. Five tiles, five distinct
 * footprints, every topic fully on screen at once: a 2x2 Build hero, a 2x1
 * Agentic strip, a 1x1 Production tile, a 1x2 tall Workflow column, and a 3x1
 * Release safety strip. Each tile carries its own topic illustration and a jump
 * link, so the whole API loop reads before the visitor reaches pricing.
 */
export function PlatformSectionV3() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
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

        <div className="mt-12 grid grid-cols-1 gap-4 sm:mt-14 sm:grid-cols-2 sm:gap-5 lg:auto-rows-[minmax(13rem,auto)] lg:grid-cols-4">
          <BuildHeroTile />
          <AgenticTile />
          <ObserveTile />
          <WorkflowTile />
          <ReleaseSafetyTile />
        </div>
      </RevealOnScroll>
    </section>
  );
}
