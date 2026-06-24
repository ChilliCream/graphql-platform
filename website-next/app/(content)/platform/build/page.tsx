import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { BuildLoopWalkthrough } from "@/src/components/platform/BuildLoopWalkthrough";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Implementation-First GraphQL in C#",
  description:
    "Implementation-first GraphQL in C#: define your contract once and generate the server pipeline, DataLoaders, and typed .NET clients in one connected lifecycle.",
  keywords: [
    "implementation-first GraphQL in C#",
    "generated server to client",
    "source-generated GraphQL server",
    "typed .NET GraphQL client",
    "Hot Chocolate build loop",
    "Strawberry Shake codegen",
    "Green Donut DataLoaders",
    "MSBuild code generation",
    "one connected GraphQL lifecycle",
    "C# is your schema",
  ],
  openGraph: {
    title: "Build GraphQL in C#, Generated Server to Client",
    description:
      "Define your GraphQL contract in C# and generate the server pipeline, DataLoaders, and typed .NET clients in one connected lifecycle.",
  },
};

/** One generated artifact in the closing surface strip. */
interface GeneratedArtifact {
  readonly source: string;
  readonly title: string;
  readonly how: string;
}

const GENERATED_SURFACE: readonly GeneratedArtifact[] = [
  {
    source: "Hot Chocolate",
    title: "Server pipeline",
    how: "Schema and resolver pipeline, source-generated at build time",
  },
  {
    source: "Green Donut",
    title: "DataLoaders",
    how: "Batched, deduplicated fetches source-generated from a method",
  },
  {
    source: "Strawberry Shake",
    title: "Typed .NET client",
    how: "Generated from your operations via MSBuild code generation",
  },
  {
    source: "Nitro",
    title: "Local tooling",
    how: "The GraphQL IDE served at /graphql by the running server",
  },
];

/** A single stitched-stack box used to contrast one source against many tools. */
interface DriftBox {
  readonly label: string;
  readonly dashed: boolean;
}

const ONE_SOURCE: readonly DriftBox[] = [
  { label: "C# implementation", dashed: false },
  { label: "schema", dashed: false },
  { label: "resolver pipeline", dashed: false },
  { label: "DataLoaders", dashed: false },
  { label: "typed client", dashed: false },
];

const STITCHED: readonly DriftBox[] = [
  { label: "server", dashed: true },
  { label: "codegen tool", dashed: true },
  { label: "registry", dashed: true },
  { label: "client cache", dashed: true },
];

/** Pre-ship checklist for the "generated where it matters" honesty beat. */
const SHIP_CHECKLIST: readonly string[] = [
  "Schema and resolver pipeline source-generated from the C# you run",
  "DataLoaders source-generated, batching and deduplicating by default",
  "Typed .NET clients generated from your operations via MSBuild",
  "A contract change surfaces as build feedback before the app ships",
  "The GraphQL IDE served from the same endpoint, not a side install",
];

/**
 * Side-by-side "fewer places for drift to hide" visual. One tidy generated
 * stack from a single source, against faint dashed disconnected tools. No
 * vendor names on the stitched side; the contrast is structural, not a callout.
 */
function DriftContrast() {
  return (
    <div className="grid gap-6 sm:grid-cols-2">
      <div className="border-cc-accent/40 bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.14em] uppercase">
          one source
        </p>
        <p className="text-cc-heading mt-2 text-sm font-semibold">
          Generated from the contract
        </p>
        <div className="mt-5 flex flex-col gap-2">
          {ONE_SOURCE.map((box, index) => (
            <div key={box.label} className="flex items-center gap-2">
              {index > 0 && (
                <span
                  aria-hidden="true"
                  className="text-cc-accent/60 w-3 shrink-0 text-center text-xs"
                >
                  &darr;
                </span>
              )}
              {index === 0 && <span aria-hidden="true" className="w-3 shrink-0" />}
              <span className="border-cc-accent/50 text-cc-ink bg-cc-surface flex-1 rounded-lg border px-3 py-2 font-mono text-[0.7rem]">
                {box.label}
              </span>
            </div>
          ))}
        </div>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg/40 rounded-2xl border p-6 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.14em] uppercase">
          stitched stack
        </p>
        <p className="text-cc-ink-dim mt-2 text-sm font-semibold">
          Separate tools, kept in sync by hand
        </p>
        <div className="mt-5 grid grid-cols-2 gap-2">
          {STITCHED.map((box) => (
            <span
              key={box.label}
              className="border-cc-ink-faint text-cc-ink-dim rounded-lg border border-dashed px-3 py-2 text-center font-mono text-[0.7rem]"
            >
              {box.label}
            </span>
          ))}
        </div>
        <p className="text-cc-ink-dim mt-5 text-center font-mono text-[0.6rem]">
          each seam kept in step on its own
        </p>
      </div>
    </div>
  );
}

/**
 * DataLoader batching mini-diagram: several incoming key chips collapse into one
 * batched fetch to the database. The "fast by default" proof visual.
 */
function BatchingDiagram() {
  const keys = ["id: 7", "id: 12", "id: 4", "id: 21"];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-md rounded-2xl border p-6 backdrop-blur-sm">
      <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.14em] uppercase">
        N+1 solved by default
      </p>
      <div className="mt-5 grid grid-cols-[1fr_auto_auto] items-center gap-4">
        <div className="flex flex-col gap-2">
          {keys.map((key) => (
            <span
              key={key}
              className="border-cc-card-border text-cc-ink bg-cc-surface rounded-lg border px-3 py-1.5 text-center font-mono text-[0.68rem]"
            >
              {key}
            </span>
          ))}
        </div>
        <span aria-hidden="true" className="text-cc-ink-faint text-lg">
          &rarr;
        </span>
        <div className="flex flex-col items-center gap-2">
          <span className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-3 py-2 text-center font-mono text-[0.68rem]">
            one batched
            <br />
            fetch
          </span>
          <span aria-hidden="true" className="text-cc-ink-faint text-sm">
            &darr;
          </span>
          <span className="border-cc-card-border text-cc-ink-dim bg-cc-surface rounded-lg border px-3 py-1.5 font-mono text-[0.68rem]">
            database
          </span>
        </div>
      </div>
      <p className="text-cc-ink-dim mt-5 text-center font-mono text-[0.6rem]">
        keys batched and deduplicated, not one query per row
      </p>
    </div>
  );
}

/** Reusable value section with copy on one side and an inline visual on the other. */
function ValueSection({
  eyebrow,
  heading,
  children,
  visual,
  flip = false,
}: {
  readonly eyebrow: string;
  readonly heading: string;
  readonly children: ReactNode;
  readonly visual: ReactNode;
  readonly flip?: boolean;
}) {
  return (
    <div className="grid items-center gap-8 lg:grid-cols-2 lg:gap-12">
      <div className={flip ? "lg:order-2" : "lg:order-1"}>
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
          {eyebrow}
        </span>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
          {heading}
        </h2>
        <div className="text-cc-ink mt-5 space-y-4 text-base/relaxed text-pretty">
          {children}
        </div>
      </div>
      <div className={flip ? "lg:order-1" : "lg:order-2"}>{visual}</div>
    </div>
  );
}

export default function BuildPage() {
  return (
    <>
      {/* Editorial hero: eyebrow + left-aligned headline + lead, single column. */}
      <section className="py-16 sm:py-24">
        <div className="max-w-3xl">
          <p className="text-cc-ink-dim font-mono text-xs font-semibold tracking-widest uppercase">
            Build loop
          </p>
          <h1 className="font-heading text-cc-heading mt-4 text-4xl leading-[1.05] font-semibold tracking-tight sm:text-5xl lg:text-6xl">
            Ship from the code that runs it.
          </h1>
          <p className="text-cc-ink-dim mt-6 text-base sm:text-lg">
            Implementation-first GraphQL in C#: define the contract where the
            implementation lives, then let the platform generate the server
            pipeline, DataLoaders, typed .NET clients, and local tooling around
            that same contract. One connected lifecycle, not a server, a codegen
            tool, and a registry stitched across ecosystems.
          </p>
          <p className="text-cc-nav-label mt-5 font-mono text-xs tracking-[0.12em] uppercase">
            one loop, not stitched tools
          </p>
          <div className="mt-8 flex flex-wrap gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
        </div>
      </section>

      {/* Centerpiece: sticky C# code rail + scrolling artifact steps. */}
      <section className="py-12">
        <div className="max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold">
            The generation is the build.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            Annotate a partial class and a Roslyn source generator emits the
            schema, resolver pipeline, and DataLoader infrastructure at build
            time. Scroll the steps and watch each artifact light up in the code
            it comes from. The implementation is the single source of truth, so
            there is no separate schema to sync by hand.
          </p>
        </div>
        <div className="mt-10">
          <BuildLoopWalkthrough />
        </div>
      </section>

      {/* Value section: fewer places for drift to hide. */}
      <section className="py-12">
        <ValueSection
          eyebrow="Integration, not just type safety"
          heading="Fewer places for drift to hide."
          visual={<DriftContrast />}
        >
          <p>
            The JS field reaches end-to-end type safety by assembling separate
            tools: a server, a codegen tool, a registry, a client cache, often
            from different vendors and ecosystems. Each is a seam where the
            contract has to be kept in step.
          </p>
          <p>
            Here the server, DataLoaders, clients, and tooling are generated from
            the one contract by one vendor. The generation is part of the build,
            so drift has fewer places to hide.
          </p>
        </ValueSection>
      </section>

      {/* Value section: N+1 solved by default. */}
      <section className="py-12">
        <ValueSection
          eyebrow="Fast by default"
          heading="N+1 is solved before you think about it."
          flip
          visual={<BatchingDiagram />}
        >
          <p>
            A <code className="text-cc-accent">[DataLoader]</code> method is
            source-generated into a Green Donut DataLoader that batches and
            deduplicates keys for you. The fan-out is fast by default, not a
            hand-wired add-on you remember to bolt on later.
          </p>
          <p>
            Resolvers stay idiomatic C#: plain methods, arguments as parameters,
            services injected from DI. There is no DSL to learn and keep in sync,
            so the code you read is the code that runs.
          </p>
        </ValueSection>
      </section>

      {/* Generated-surface strip: horizontal mono chips of what gets generated. */}
      <section className="py-12">
        <div className="max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold">
            One contract, a generated surface.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            From the same C# contract, the platform produces the pieces you would
            otherwise wire together by hand, each attributed to the tool that
            generates it.
          </p>
        </div>
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {GENERATED_SURFACE.map((artifact) => (
            <div
              key={artifact.title}
              className="border-cc-card-border bg-cc-card-bg/60 flex flex-col rounded-2xl border p-5 backdrop-blur-sm"
            >
              <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                {artifact.source}
              </span>
              <p className="text-cc-heading mt-3 text-sm font-semibold">
                {artifact.title}
              </p>
              <p className="text-cc-ink-dim mt-2 text-xs/relaxed">
                {artifact.how}
              </p>
            </div>
          ))}
        </div>
      </section>

      {/* Honesty / credibility beat: generated where it matters. */}
      <section className="py-12">
        <div className="border-cc-card-border bg-cc-card-bg/60 grid items-start gap-8 rounded-3xl border p-8 backdrop-blur-sm sm:p-10 lg:grid-cols-2 lg:gap-12">
          <div>
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
              Honest by default
            </span>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-tight font-semibold text-balance">
              Generated where it matters, validated before it ships.
            </h2>
            <p className="text-cc-ink mt-5 text-base/relaxed">
              We say generated where it is generated and feedback where it is
              feedback. Hot Chocolate and Green Donut source-generate the server
              and DataLoaders. Strawberry Shake generates clients through MSBuild,
              so a contract change shows up as build feedback before the app
              ships, a smaller and more trustworthy claim than &ldquo;one type
              system&rdquo; or &ldquo;compiled means correct.&rdquo;
            </p>
          </div>
          <ul className="space-y-4">
            {SHIP_CHECKLIST.map((item) => (
              <li key={item} className="flex items-start gap-3">
                <span className="text-cc-accent mt-0.5 shrink-0">
                  <CheckIcon />
                </span>
                <span className="text-cc-ink text-sm/relaxed">{item}</span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* Closer + CTA. */}
      <section className="py-12">
        <div className="flex flex-col items-start">
          <p className="text-cc-accent font-mono text-sm">
            One connected GraphQL lifecycle, not stitched tools.
          </p>
          <h2 className="font-heading text-cc-heading text-h3 mt-4 leading-tight font-semibold text-balance">
            Write the implementation. Get the loop.
          </h2>
          <p className="text-cc-ink mt-5 max-w-2xl text-base/relaxed">
            Start single and scale later: the same contract that runs your server
            generates the clients and tooling around it, so the graph and its
            consumers move together.
          </p>
          <div className="mt-8 flex flex-wrap gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
          <p className="text-cc-ink-dim mt-6 text-sm">
            Learn more about{" "}
            <Link
              href="/platform/analytics"
              className="text-cc-accent hover:text-cc-accent-hover transition-colors"
            >
              analytics
            </Link>
            ,{" "}
            <Link
              href="/platform/continuous-integration"
              className="text-cc-accent hover:text-cc-accent-hover transition-colors"
            >
              continuous integration
            </Link>
            , or the wider{" "}
            <Link
              href="/platform"
              className="text-cc-accent hover:text-cc-accent-hover transition-colors"
            >
              platform
            </Link>
            .
          </p>
        </div>
      </section>
    </>
  );
}
