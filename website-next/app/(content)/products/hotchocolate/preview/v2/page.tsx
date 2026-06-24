import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Hot Chocolate, From a C# class to a GraphQL server",
  description:
    "Hot Chocolate is the open source GraphQL server for .NET. Annotate a C# class, build, and ship a typed schema with batching, subscriptions, and OpenTelemetry.",
  keywords: [
    "Hot Chocolate",
    "GraphQL server .NET",
    "C# GraphQL",
    "ASP.NET Core GraphQL",
    "DataLoader",
    "GraphQL subscriptions",
    "OpenTelemetry GraphQL",
    "Apollo Federation",
    "Fusion",
    "ChilliCream",
  ],
  openGraph: {
    title: "Hot Chocolate, From a C# class to a GraphQL server",
    description:
      "The open source GraphQL server for .NET. Annotate a C# class, build, and ship a typed schema with batching, subscriptions, and OpenTelemetry.",
  },
  robots: { index: false, follow: false },
};

// Brand spectrum used exactly once on the page, as the headline accent.
const SPECTRUM_GRADIENT =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

export default function HotChocolateStoryPage() {
  return (
    <>
      <Hero />
      <BuildLoop />
      <OutOfTheBox />
      <MitBand />
      <ClosingCta />
    </>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                       */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="py-16 text-center sm:py-24">
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
        Open source GraphQL server for .NET
      </div>

      <h1 className="text-cc-heading font-heading mx-auto max-w-4xl text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        From a C# class to a{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM_GRADIENT }}
        >
          running GraphQL server
        </span>{" "}
        in minutes.
      </h1>

      <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base sm:text-lg">
        Annotate a class. Run the build. Get a spec compliant schema, typed
        resolvers, and DataLoader infrastructure emitted at compile time by a
        Roslyn source generator. Then serve it over your existing ASP.NET Core
        endpoint.
      </p>

      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>

      <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 font-mono text-xs tracking-wide">
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          MIT licensed
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          GraphQL 2025 spec
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          ASP.NET Core
        </span>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  The build loop, 4 steps                                                    */
/* -------------------------------------------------------------------------- */

function BuildLoop() {
  return (
    <section className="py-16">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          The build loop
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Four steps, no GraphQL boilerplate.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Your implementation is your schema. The Roslyn source generator reads
          your C# at build time and emits the schema, resolver pipelines, and
          DataLoader infrastructure. You write domain code, the rest is
          generated.
        </p>
      </div>

      <ol className="space-y-10">
        <Step
          index="01"
          title="Attribute a class"
          blurb="Mark a partial class as your query root. Plain C# methods become resolvers. DI parameters are injected; non keyed services need no marker attribute."
        >
          <CodeFrame language="C# / Program.cs and Query.cs">
            <CsLine>
              <Kw>using</Kw> HotChocolate.Types;
            </CsLine>
            <CsLine />
            <CsLine>
              <Attr>[QueryType]</Attr>
            </CsLine>
            <CsLine>
              <Kw>public partial class</Kw> <Type>Query</Type>
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Kw>public</Kw> <Type>Book</Type> GetBook(
              <Type>BookService</Type> books, <Type>Guid</Type> id)
            </CsLine>
            <CsLine indent={2}>{`=> books.GetById(id);`}</CsLine>
            <CsLine>{`}`}</CsLine>
            <CsLine />
            <CsLine>
              <Cmt>{`// Program.cs`}</Cmt>
            </CsLine>
            <CsLine>builder.Services</CsLine>
            <CsLine indent={1}>.AddGraphQLServer()</CsLine>
            <CsLine indent={1}>.AddTypes();</CsLine>
          </CodeFrame>
        </Step>

        <Step
          index="02"
          title="Build"
          blurb="On dotnet build, the HotChocolate.Types.Analyzers source generator scans your annotated types and emits the schema, resolver pipelines, DataLoaders, and DI module."
        >
          <CodeFrame language="terminal">
            <TermLine prompt>dotnet build</TermLine>
            <TermLine>{`  Restored Sample.csproj`}</TermLine>
            <TermLine>{`  Sample -> bin/Debug/net9.0/Sample.dll`}</TermLine>
            <TermLine muted>
              {`    + generated: schema, resolvers, DataLoaders, DI module`}
            </TermLine>
            <TermLine ok>Build succeeded</TermLine>
          </CodeFrame>
        </Step>

        <Step
          index="03"
          title="Schema and resolvers emitted"
          blurb="The generator produces a spec compliant schema and the wiring that backs it. DataLoaders are first class: a static method with [DataLoader] becomes a batched, deduplicated, per request cached loader. No N+1, no manual plumbing."
        >
          <div className="grid gap-4 lg:grid-cols-2">
            <CodeFrame language="C# / BookLoaders.cs">
              <CsLine>
                <Kw>internal static class</Kw> <Type>BookLoaders</Type>
              </CsLine>
              <CsLine>{`{`}</CsLine>
              <CsLine indent={1}>
                <Attr>[DataLoader]</Attr>
              </CsLine>
              <CsLine indent={1}>
                <Kw>public static async</Kw>{" "}
                <Type>{`Task<IDictionary<Guid, Book>>`}</Type>
              </CsLine>
              <CsLine indent={2}>
                LoadBooksAsync(<Type>{`IReadOnlyList<Guid>`}</Type> ids,
              </CsLine>
              <CsLine indent={2}>
                <Type>BookRepository</Type> repo, <Type>CancellationToken</Type>{" "}
                ct)
              </CsLine>
              <CsLine indent={2}>
                {`=> await repo.GetManyAsync(ids, ct);`}
              </CsLine>
              <CsLine>{`}`}</CsLine>
            </CodeFrame>

            <CodeFrame language="GraphQL / schema.graphql (generated)">
              <CsLine>
                <Kw>type</Kw> <Type>Query</Type> {`{`}
              </CsLine>
              <CsLine indent={1}>
                book(id: <Type>UUID!</Type>): <Type>Book</Type>
              </CsLine>
              <CsLine>{`}`}</CsLine>
              <CsLine />
              <CsLine>
                <Kw>type</Kw> <Type>Book</Type> {`{`}
              </CsLine>
              <CsLine indent={1}>
                id: <Type>UUID!</Type>
              </CsLine>
              <CsLine indent={1}>
                title: <Type>String!</Type>
              </CsLine>
              <CsLine indent={1}>
                author: <Type>Author!</Type>
              </CsLine>
              <CsLine>{`}`}</CsLine>
            </CodeFrame>
          </div>
        </Step>

        <Step
          index="04"
          title="Run it"
          blurb="dotnet run, then open the endpoint. The GraphQL IDE serves directly from the server: browse the schema, run operations, and inspect responses. Same surface, no extra deploy."
        >
          <div className="border-cc-card-border bg-cc-card-bg mx-auto max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
            <CodeFrame language="terminal" flush>
              <TermLine prompt>dotnet run</TermLine>
              <TermLine>
                {`  Now listening on: https://localhost:5001`}
              </TermLine>
              <TermLine muted>{`  GraphQL endpoint    /graphql`}</TermLine>
              <TermLine muted>{`  IDE                 /graphql`}</TermLine>
            </CodeFrame>
            <div className="border-cc-card-border border-t">
              <NitroCompose />
            </div>
          </div>
          <p className="text-cc-ink-dim mt-3 text-center font-mono text-xs tracking-wide">
            Built in GraphQL IDE, served from the running server.
          </p>
        </Step>
      </ol>
    </section>
  );
}

interface StepProps {
  readonly index: string;
  readonly title: string;
  readonly blurb: string;
  readonly children: ReactNode;
}

function Step({ index, title, blurb, children }: StepProps) {
  return (
    <li className="grid gap-6 lg:grid-cols-[14rem_1fr] lg:gap-10">
      <div className="lg:pt-2">
        <div className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
          Step {index}
        </div>
        <h3 className="text-cc-heading mt-2 text-2xl font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">{blurb}</p>
      </div>
      <div>{children}</div>
    </li>
  );
}

/* -------------------------------------------------------------------------- */
/*  Code framing helpers                                                       */
/* -------------------------------------------------------------------------- */

interface CodeFrameProps {
  readonly language: string;
  readonly children: ReactNode;
  /** Drop the outer rounded border when the frame is embedded in another card. */
  readonly flush?: boolean;
}

function CodeFrame({ language, children, flush = false }: CodeFrameProps) {
  const outer = flush
    ? "overflow-hidden"
    : "border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm";
  return (
    <div className={outer}>
      <div className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px] tracking-wide">
        <div className="flex items-center gap-2">
          <span
            aria-hidden
            className="bg-cc-status-firing inline-block size-2 rounded-full opacity-70"
          />
          <span
            aria-hidden
            className="bg-cc-status-investigating inline-block size-2 rounded-full opacity-70"
          />
          <span
            aria-hidden
            className="bg-cc-status-healthy inline-block size-2 rounded-full opacity-70"
          />
        </div>
        <span className="uppercase">{language}</span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[13px] leading-[1.65]">
        <code>{children}</code>
      </pre>
    </div>
  );
}

interface CsLineProps {
  readonly children?: ReactNode;
  readonly indent?: number;
}

function CsLine({ children, indent = 0 }: CsLineProps) {
  const pad = "  ".repeat(indent);
  return (
    <div>
      {pad}
      {children}
      {"\n"}
    </div>
  );
}

function Kw({ children }: { readonly children: ReactNode }) {
  // Keyword tone, teal accent. Avoids the reserved brand spectrum.
  return <span className="text-cc-accent">{children}</span>;
}

function Type({ children }: { readonly children: ReactNode }) {
  // Type/identifier tone, soft warning gold. Avoids the reserved brand spectrum.
  return <span className="text-cc-warning">{children}</span>;
}

function Attr({ children }: { readonly children: ReactNode }) {
  // Attribute tone, cta fuchsia. Avoids the reserved brand spectrum.
  return <span className="text-cc-cta">{children}</span>;
}

function Cmt({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-ink-dim italic">{children}</span>;
}

interface TermLineProps {
  readonly children: ReactNode;
  readonly prompt?: boolean;
  readonly muted?: boolean;
  readonly ok?: boolean;
}

function TermLine({ children, prompt, muted, ok }: TermLineProps) {
  let cls = "text-cc-ink";
  if (muted) cls = "text-cc-ink-dim";
  if (ok) cls = "text-cc-success";
  return (
    <div className={cls}>
      {prompt ? <span className="text-cc-accent">$ </span> : null}
      {children}
      {"\n"}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Out of the box, 6 capability cards                                         */
/* -------------------------------------------------------------------------- */

interface Capability {
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const CAPABILITIES: readonly Capability[] = [
  {
    title: "Compile time Composition",
    body: "Fusion composes subgraph schemas at planning time, not at runtime. The gateway stays fast and queries stay typed end to end.",
    bullets: [
      "Plan once at build, execute many",
      "Run a single server now, add Fusion later",
    ],
  },
  {
    title: "Code first or Schema first",
    body: "Author your schema however your team prefers. Implementation first via the source generator, fluent code first via descriptors, or schema first interop. Mix freely in one project.",
    bullets: ["Roslyn source generator", "ObjectType<T> descriptors"],
  },
  {
    title: "DataLoader Batching",
    body: "Green Donut batches loads, deduplicates keys, and caches per request. N plus 1 disappears without manual plumbing.",
    bullets: ["[DataLoader] attribute", "Batch, group, and cache"],
  },
  {
    title: "Realtime Subscriptions",
    body: "WebSocket and Server Sent Events are first class transports. Pub sub providers are pluggable, from in memory to Redis, NATS, Postgres LISTEN NOTIFY, and RabbitMQ.",
    bullets: ["graphql-ws and graphql-sse", "Dynamic per resource topics"],
  },
  {
    title: "OpenTelemetry Built In",
    body: "Native OTel instrumentation aligned with the proposed GraphQL semantic conventions. Configure an exporter and traces flow to Jaeger, Tempo, Datadog, or Honeycomb.",
    bullets: ["Per resolver and DataLoader spans", "Document hash and id tags"],
  },
  {
    title: "Federation ready",
    body: "Compose with other Hot Chocolate services via Fusion, or interoperate with subgraphs that follow the Apollo Federation spec.",
    bullets: ["Fusion subgraph, no code changes", "Apollo Federation spec"],
  },
];

function OutOfTheBox() {
  return (
    <section className="py-16">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          What you get out of the box
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Production grade, day one.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          The build loop is the easy part. Everything below ships with the
          server, ready to wire into the platform you already run.
        </p>
      </div>

      <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {CAPABILITIES.map((capability) => (
          <article
            key={capability.title}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-xl border p-6 backdrop-blur-sm transition-colors"
          >
            <h3 className="text-cc-heading text-lg font-semibold">
              {capability.title}
            </h3>
            <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
              {capability.body}
            </p>
            <ul className="mt-4 space-y-1.5">
              {capability.bullets.map((bullet) => (
                <li
                  key={bullet}
                  className="text-cc-ink flex items-start gap-2 text-sm"
                >
                  <span className="text-cc-accent mt-1 shrink-0">
                    <CheckIcon />
                  </span>
                  <span>{bullet}</span>
                </li>
              ))}
            </ul>
          </article>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  MIT / open source band                                                     */
/* -------------------------------------------------------------------------- */

function MitBand() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 text-center backdrop-blur-sm sm:p-12">
        <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          MIT licensed, open source
        </div>
        <h2 className="text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
          Free to use, commercial or otherwise. No strings attached.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Hot Chocolate is open source under the MIT license and developed in
          the open on GitHub. Read the source, file issues, send patches, or
          fork it for your own platform.
        </p>
        <div className="mt-7 flex flex-wrap justify-center gap-4">
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE">
            Read the license
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                                */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="py-20 text-center">
      <h2 className="text-cc-heading mx-auto max-w-3xl text-3xl font-semibold tracking-tight sm:text-4xl">
        Your next GraphQL server is a{" "}
        <span className="text-cc-accent">dotnet new</span> away.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base sm:text-lg">
        Install the templates, scaffold a project, and run it. The build loop
        you just saw is waiting.
      </p>

      <div className="mx-auto mt-8 max-w-xl">
        <CodeFrame language="terminal">
          <TermLine prompt>dotnet new install HotChocolate.Templates</TermLine>
          <TermLine prompt>dotnet new graphql -n MyApi</TermLine>
          <TermLine prompt>cd MyApi &amp;&amp; dotnet run</TermLine>
        </CodeFrame>
      </div>

      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
    </section>
  );
}
