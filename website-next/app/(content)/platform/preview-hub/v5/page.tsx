import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Metadata                                                                  */
/* -------------------------------------------------------------------------- */

export const metadata: Metadata = {
  title: "The Field Guide to the ChilliCream GraphQL Platform",
  description:
    "A long-form field guide to the ChilliCream GraphQL platform: eight numbered capabilities across Build, Run, and Evolve, plus the Nitro control plane that ties them.",
  keywords: [
    "GraphQL platform",
    "ChilliCream platform",
    "Nitro control plane",
    "GraphQL observability",
    "GraphQL release safety",
    "GraphQL analytics",
    "GraphQL workflows",
    "agentic coding",
    "continuous integration",
    "GraphQL ecosystem",
  ],
  openGraph: {
    title: "The Field Guide to the ChilliCream GraphQL Platform",
    description:
      "An editorial field guide: eight numbered capabilities for every GraphQL API, plus the Nitro control plane that ties Build, Run, and Evolve together.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Content model                                                             */
/*  Verbatim facts from v1: titles, outcomes, hrefs, proofs.                  */
/* -------------------------------------------------------------------------- */

type MovementKey = "build" | "run" | "evolve";

interface Movement {
  readonly key: MovementKey;
  readonly numeral: string;
  readonly label: string;
  readonly intent: string;
  readonly range: string;
}

const MOVEMENTS: readonly Movement[] = [
  {
    key: "build",
    numeral: "I",
    label: "Build",
    intent: "Author the API and let agents help.",
    range: "01-02",
  },
  {
    key: "run",
    numeral: "II",
    label: "Run",
    intent: "Operate it in production with eyes on every call.",
    range: "03-06",
  },
  {
    key: "evolve",
    numeral: "III",
    label: "Evolve",
    intent: "Ship change without breaking published clients.",
    range: "07-08",
  },
];

interface Chapter {
  readonly id: string;
  readonly number: string;
  readonly movement: MovementKey;
  readonly title: string;
  readonly outcome: string;
  readonly href: string;
  readonly body: string;
  readonly proofs: readonly string[];
}

const CHAPTERS: readonly Chapter[] = [
  {
    id: "build",
    number: "01",
    movement: "build",
    title: "Build",
    outcome: "Ship from the code that runs it.",
    href: "/platform/build",
    body: "Build is where a GraphQL API begins, in the same C# code that will serve it in production. Hot Chocolate is source-generated, so schema, resolvers, and DataLoaders come out of one class without a parallel definition file to keep in sync. Strawberry Shake closes the loop on the client side with MSBuild codegen that turns operations into typed .NET clients.",
    proofs: [
      "Implementation-first GraphQL in C#",
      "Schema, resolvers, DataLoaders from one class",
      "Typed .NET clients out of the same source",
    ],
  },
  {
    id: "agentic-coding",
    number: "02",
    movement: "build",
    title: "Agentic Coding",
    outcome: "Give coding agents a feedback loop.",
    href: "/platform/agentic-coding",
    body: "An agent is only as good as the feedback it gets. Typed contracts make the schema legible to an agent, diff and lint catch the shape of every change, and the same review loop a senior engineer would run is available on every iteration. The result is an agent that proposes changes you can actually merge.",
    proofs: [
      "Typed contracts agents can read",
      "Diff and lint signal on every change",
      "Same loop a senior reviewer would run",
    ],
  },
  {
    id: "observability",
    number: "03",
    movement: "run",
    title: "Observability",
    outcome: "See what the API is doing, right now.",
    href: "/platform/observability",
    body: "Once the API is live, the question shifts from what it does to what it is doing. Operation-level traces and timings sit alongside field hot paths and N+1 detection, and the whole feed exports to your existing OpenTelemetry stack. Telemetry needs Nitro configuration to come online, and once it does it reads like a live log of every call.",
    proofs: [
      "Operation-level traces and timings",
      "Field hot paths and N+1 detection",
      "OpenTelemetry export to your stack",
    ],
  },
  {
    id: "workflows",
    number: "04",
    movement: "run",
    title: "Workflows",
    outcome: "Let work continue after the request.",
    href: "/platform/workflows",
    body: "Not every job finishes inside a request. Mocha gives you durable steps with retries, background jobs in the same programming model, and resumption on cold start. Mocha sagas are validated before traffic, and processing semantics are exactly-once, so the same workflow can be reasoned about end to end.",
    proofs: [
      "Durable steps with retries",
      "Background jobs in the same model",
      "Resumable on cold start",
    ],
  },
  {
    id: "analytics",
    number: "05",
    movement: "run",
    title: "Analytics",
    outcome: "Know which fields earn their keep.",
    href: "/platform/analytics",
    body: "A GraphQL schema is a contract that grows by accretion. Analytics keeps a field-level record of usage over time, per-client adoption of every type, and a quiet way to spot dead fields before you cut them. The graph stops being a guess and starts being a measurement.",
    proofs: [
      "Field-level usage over time",
      "Per-client adoption per type",
      "Spot dead fields before you cut",
    ],
  },
  {
    id: "ecosystem",
    number: "06",
    movement: "run",
    title: "Ecosystem",
    outcome: "An ecosystem you can trust and reuse.",
    href: "/platform/ecosystem",
    body: "Around the server sits a small family of tools that share its conventions. Banana Cake Pop is the IDE that serves from the endpoint, Strawberry Shake produces typed clients through MSBuild codegen, and Green Donut is the DataLoader story that closes the N+1 gap. They are all written against the same assumptions, so they compose without ceremony.",
    proofs: [
      "Banana Cake Pop IDE",
      "Strawberry Shake typed clients",
      "Green Donut DataLoaders",
    ],
  },
  {
    id: "release-safety",
    number: "07",
    movement: "evolve",
    title: "Release Safety",
    outcome: "Change contracts with a safety net.",
    href: "/platform/release-safety",
    body: "A schema change is a contract change, and contract changes have witnesses. Release Safety diffs the next schema against the published clients that depend on it, flags breaking changes before merge, and lets you block, warn, or allow per rule. The output is a precise list of published clients affected by any proposed change.",
    proofs: [
      "Schema diff against published clients",
      "Breaking change flagged before merge",
      "Block, warn, or allow per rule",
    ],
  },
  {
    id: "continuous-integration",
    number: "08",
    movement: "evolve",
    title: "Continuous Integration",
    outcome: "Innovate with confidence at merge time.",
    href: "/platform/continuous-integration",
    body: "The pull request is where intent becomes commitment. Schema checks run on every pull request, Fusion composition is validated across services at planning time, and the diff arrives annotated in code review. The gateway is always self-run, so what you compose is exactly what you ship.",
    proofs: [
      "Schema check on every pull request",
      "Composition validation across services",
      "Annotated diffs in code review",
    ],
  },
];

/* -------------------------------------------------------------------------- */
/*  Inline primitives                                                         */
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

function Rule() {
  return <hr className="border-cc-card-border my-0 w-full border-t" />;
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <header className="flex flex-col gap-7">
      <Eyebrow>The ChilliCream Platform / A field guide</Eyebrow>
      <h1 className="font-heading text-hero text-cc-heading max-w-[18ch] font-semibold tracking-tight">
        Eight capabilities, one platform for every GraphQL API.
      </h1>
      <p className="text-cc-ink lead">
        A long-form field guide to the ChilliCream GraphQL platform. Read it top
        to bottom, or step into the chapter that maps to the work in front of
        you today.
      </p>
      <div className="flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Standfirst essay with drop cap                                            */
/* -------------------------------------------------------------------------- */

function Standfirst() {
  return (
    <section className="flex flex-col gap-10">
      <p className="text-cc-ink lead">
        <span
          aria-hidden
          className="text-cc-accent font-heading float-left mt-1 mr-3 text-[5rem] leading-none font-semibold"
        >
          E
        </span>
        very GraphQL API has three movements in its life. It is{" "}
        <span className="text-cc-heading">built</span>, often quickly and from
        the code that will run it. It is then{" "}
        <span className="text-cc-heading">run</span>, where the questions change
        from what it does to what it is doing right now. And, if it succeeds, it
        must be <span className="text-cc-heading">evolved</span>, carefully,
        without breaking the published clients that have come to depend on its
        shape. The eight chapters below follow that arc, in order, so that the
        platform reads less like a feature list and more like a map of an
        API&apos;s working life.
      </p>
      <Rule />
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Table of contents                                                         */
/* -------------------------------------------------------------------------- */

function Contents() {
  return (
    <section className="flex flex-col gap-6">
      <Eyebrow>Contents</Eyebrow>
      <ul className="flex flex-col gap-3">
        {MOVEMENTS.map((movement) => (
          <li
            key={movement.key}
            className="text-cc-ink text-body flex items-baseline gap-4"
          >
            <span className="text-cc-accent font-mono text-[0.72rem] tracking-[0.18em] tabular-nums">
              {movement.range}
            </span>
            <Link
              href={`#movement-${movement.key}`}
              className="text-cc-heading hover:text-cc-accent font-heading no-underline transition-colors"
            >
              Movement {movement.numeral}. {movement.label}
            </Link>
            <span className="text-cc-ink-dim hidden sm:inline">
              {movement.intent}
            </span>
          </li>
        ))}
      </ul>
      <Rule />
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Chapter                                                                   */
/* -------------------------------------------------------------------------- */

interface ChapterEntryProps {
  readonly chapter: Chapter;
}

function ChapterEntry({ chapter }: ChapterEntryProps) {
  return (
    <article
      id={chapter.id}
      className="grid grid-cols-1 gap-x-8 gap-y-6 lg:grid-cols-[6rem_minmax(0,1fr)]"
    >
      <div
        aria-hidden
        className="text-cc-accent font-heading text-h1 leading-none font-semibold tabular-nums lg:pt-1 lg:text-right"
      >
        {chapter.number}
      </div>
      <div className="flex flex-col gap-4">
        <h3 className="font-heading text-h2 text-cc-heading font-semibold tracking-tight">
          {chapter.title}
        </h3>
        <p className="text-cc-ink lead">{chapter.outcome}</p>
        <p className="text-cc-ink-dim text-body leading-relaxed">
          {chapter.body}
        </p>
        <ul className="flex flex-col gap-1">
          {chapter.proofs.map((proof) => (
            <li
              key={proof}
              className="text-cc-ink-dim text-body leading-relaxed"
            >
              {proof}
            </li>
          ))}
        </ul>
        <Link
          href={chapter.href}
          className="text-cc-accent hover:text-cc-accent text-body font-medium no-underline"
        >
          Open {chapter.title} &rarr;
        </Link>
      </div>
    </article>
  );
}

/* -------------------------------------------------------------------------- */
/*  Movement                                                                  */
/* -------------------------------------------------------------------------- */

interface MovementSectionProps {
  readonly movement: Movement;
}

function MovementSection({ movement }: MovementSectionProps) {
  const chapters = CHAPTERS.filter((c) => c.movement === movement.key);
  return (
    <section id={`movement-${movement.key}`} className="flex flex-col gap-12">
      <div className="flex flex-col gap-4">
        <Eyebrow>
          Movement {movement.numeral} / Chapters {movement.range}
        </Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
          {movement.label}. {movement.intent}
        </h2>
        <Rule />
      </div>
      <div className="flex flex-col gap-12">
        {chapters.map((chapter, index) => (
          <div key={chapter.id} className="flex flex-col gap-12">
            <ChapterEntry chapter={chapter} />
            {index < chapters.length - 1 ? <Rule /> : null}
          </div>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Nitro interlude                                                           */
/* -------------------------------------------------------------------------- */

function NitroInterlude() {
  return (
    <section className="flex flex-col gap-8">
      <Rule />
      <div className="border-cc-card-border flex flex-col gap-6 border p-8 md:p-10">
        <Eyebrow>Interlude / The Control Plane</Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
          Nitro is where the eight capabilities meet.
        </h2>
        <p className="text-cc-ink lead">
          Nitro is the hosted surface where the eight capabilities meet: schema
          registry, release checks, analytics, and traces share one home.
          Connect a service, ship a change, and Nitro keeps the rest of the
          platform in sync.
        </p>
        <ul className="text-cc-ink-dim flex flex-col gap-1">
          <li className="text-body leading-relaxed">
            Schema registry for every environment.
          </li>
          <li className="text-body leading-relaxed">
            Release checks against published clients.
          </li>
          <li className="text-body leading-relaxed">
            Field usage and traces in one timeline.
          </li>
        </ul>
        <div className="flex flex-wrap items-center gap-3 pt-2">
          <SolidButton href="https://nitro.chillicream.com">
            Open Nitro
          </SolidButton>
          <OutlineButton href="/products/nitro">About Nitro</OutlineButton>
        </div>
      </div>
      <Rule />
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Coda                                                                      */
/* -------------------------------------------------------------------------- */

function Coda() {
  return (
    <section className="flex flex-col items-center gap-6 text-center">
      <Eyebrow>Read on</Eyebrow>
      <h2 className="font-heading text-h2 text-cc-heading font-semibold tracking-tight">
        Start with the surface closest to today&apos;s problem.
      </h2>
      <p className="text-cc-ink-dim text-body leading-relaxed">
        Every chapter above links to a real page. Open the one that maps to the
        work in front of you, or start a project and let the rest of the
        platform fold in as you need it.
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

export default function PlatformFieldGuidePage() {
  return (
    <div className="mx-auto flex max-w-[68ch] flex-col gap-24 py-10 md:gap-32 md:py-16">
      <Hero />
      <Standfirst />
      <Contents />
      {MOVEMENTS.map((movement) => (
        <MovementSection key={movement.key} movement={movement} />
      ))}
      <NitroInterlude />
      <Coda />
    </div>
  );
}
