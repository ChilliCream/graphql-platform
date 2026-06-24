import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Strawberry Shake: Typed GraphQL Client for .NET (Reference)",
  description:
    "Reference sheet for Strawberry Shake, the open-source typed GraphQL client for .NET. MSBuild codegen, normalized store, fetch strategies, subscriptions, Blazor.",
  openGraph: {
    title: "Strawberry Shake: Typed GraphQL Client for .NET (Reference)",
    description:
      "Reference sheet for Strawberry Shake, the open-source typed GraphQL client for .NET. MSBuild codegen, normalized store, fetch strategies, subscriptions, Blazor.",
    type: "website",
  },
  keywords: [
    "Strawberry Shake",
    ".NET GraphQL client",
    "strongly-typed GraphQL",
    "MSBuild codegen",
    "Blazor GraphQL",
    "Razor GraphQL",
    "MAUI GraphQL",
    "GraphQL subscriptions",
    "persisted operations",
    "reactive store",
    "dotnet graphql",
    "Hot Chocolate",
  ],
  robots: { index: false, follow: false },
};

// -----------------------------------------------------------------------------
// Primitives
// -----------------------------------------------------------------------------

interface GutterIndexProps {
  readonly value: string;
}

function GutterIndex({ value }: GutterIndexProps) {
  return (
    <span
      className="text-cc-ink-dim w-10 shrink-0 font-mono text-[10.5px] tracking-widest tabular-nums select-none sm:w-14"
      aria-hidden
    >
      {value}
    </span>
  );
}

interface CardProps {
  readonly id?: string;
  readonly section: string;
  readonly title: string;
  readonly children: ReactNode;
}

/** Titled card with the section index in the gutter and a mono uppercase title. */
function Card({ id, section, title, children }: CardProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t first:border-t-0"
    >
      <div className="flex items-start gap-0 py-5 sm:py-6">
        <GutterIndex value={section} />
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline gap-3">
            <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
              {section}
            </span>
            <h2 className="text-cc-heading text-h6 font-mono font-semibold tracking-[0.18em] uppercase">
              {title}
            </h2>
          </div>
          <div className="mt-4">{children}</div>
        </div>
      </div>
    </section>
  );
}

interface RowProps {
  readonly idx: string;
  readonly label: string;
  readonly children: ReactNode;
}

/** A single hairline ledger row: gutter index, mono label, value column. */
function Row({ idx, label, children }: RowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-1 gap-2 border-t py-2.5 first:border-t-0 lg:grid-cols-12 lg:gap-6 lg:py-2">
      <div className="lg:col-span-4 lg:flex lg:items-baseline lg:gap-3">
        <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase tabular-nums">
          {idx}
        </span>
        <span className="text-cc-heading text-caption font-mono tracking-tight uppercase">
          {label}
        </span>
      </div>
      <div className="text-cc-ink lg:col-span-8">{children}</div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Token color helpers for inline code samples.
// -----------------------------------------------------------------------------

const T: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  field: { color: "#7ee787" },
};

interface CodeProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function Code({ children, className }: CodeProps) {
  return (
    <code
      className={`font-mono text-[12px] whitespace-pre-wrap ${className ?? "text-cc-ink"}`}
    >
      {children}
    </code>
  );
}

// -----------------------------------------------------------------------------
// 00 Masthead (spec sheet)
// -----------------------------------------------------------------------------

function Masthead() {
  return (
    <section className="border-cc-card-border border-b pt-10 pb-8 sm:pt-14">
      <div className="flex items-start gap-0">
        <GutterIndex value="R-00" />
        <div className="min-w-0 flex-1">
          <p className="text-cc-accent text-caption font-mono tracking-[0.22em] uppercase">
            Strawberry-Shake / Typed GraphQL Client for .NET
          </p>
          <h1 className="text-cc-heading font-heading text-h2 mt-3 font-semibold tracking-tight text-balance">
            The reference card for a typed GraphQL client your .NET team can
            own.
          </h1>
          <p className="text-cc-ink-dim text-body mt-4 max-w-prose">
            One MIT-licensed package. Operations live in .graphql files, MSBuild
            runs codegen at build time, the runtime is the .NET you already
            ship. This page is the spec sheet, scanned top to bottom.
          </p>

          <div className="mt-7 lg:max-w-3xl">
            <Row idx="00.1" label="License">
              MIT, open source, free for commercial use.
            </Row>
            <Row idx="00.2" label="Codegen">
              MSBuild task, runs during{" "}
              <Code className="text-cc-accent">dotnet build</Code>. Not source
              generators, not reflection at runtime.
            </Row>
            <Row idx="00.3" label="Runtimes">
              .NET 8 and .NET 9, with Blazor (Server, WebAssembly, hybrid) and
              .NET MAUI.
            </Row>
            <Row idx="00.4" label="Store">
              Normalized entity store keyed by type and id, reactive, optional
              persistence (SQLite, LiteDB).
            </Row>
            <Row idx="00.5" label="Transports">
              HTTP for queries and mutations, WebSocket for subscriptions, with
              reconnect and auth payloads.
            </Row>
            <Row idx="00.6" label="Server">
              Any spec-compliant GraphQL server. Pairs naturally with Hot
              Chocolate, not coupled to it.
            </Row>
            <Row idx="00.7" label="Get started">
              <div className="flex flex-wrap items-center gap-3">
                <SolidButton href="/docs/strawberryshake">
                  Get Started
                </SolidButton>
                <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                  View on GitHub
                </OutlineButton>
              </div>
            </Row>
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// 01 Capability index (table of contents)
// -----------------------------------------------------------------------------

interface IndexRowProps {
  readonly idx: string;
  readonly capability: string;
  readonly notes: string;
  readonly refTag: string;
  readonly anchor: string;
}

function IndexRow({ idx, capability, notes, refTag, anchor }: IndexRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-1 gap-2 border-t py-2.5 first:border-t-0 lg:grid-cols-12 lg:items-baseline lg:gap-4">
      <div className="flex items-baseline gap-3 lg:col-span-5 lg:gap-4">
        <span className="text-cc-ink-dim font-mono text-[11px] tabular-nums lg:w-10">
          {idx}
        </span>
        <span className="text-cc-heading text-caption font-mono tracking-tight uppercase">
          {capability}
        </span>
      </div>
      <div className="flex items-baseline justify-between gap-4 lg:col-span-7 lg:contents">
        <span className="text-cc-ink text-caption lg:col-span-5">{notes}</span>
        <a
          href={anchor}
          className="text-cc-accent text-caption text-right font-mono tracking-tight uppercase hover:underline lg:col-span-2"
        >
          {refTag}
        </a>
      </div>
    </div>
  );
}

function CapabilityIndex() {
  return (
    <Card id="capabilities" section="R-01" title="Capability index">
      <div className="border-cc-card-border text-cc-ink-dim hidden grid-cols-12 gap-4 border-b pb-2 font-mono text-[10px] tracking-widest uppercase lg:grid">
        <span className="col-span-1">idx</span>
        <span className="col-span-4">capability</span>
        <span className="col-span-5">notes</span>
        <span className="col-span-2 text-right">ref</span>
      </div>
      <IndexRow
        idx="01.a"
        capability="MSBuild codegen"
        notes="Build-time generation of typed client, records, DI registration."
        refTag="R-02"
        anchor="#pipeline"
      />
      <IndexRow
        idx="01.b"
        capability="Typed client API"
        notes="ExecuteAsync, Watch, IObservable<Result>, IsErrorResult."
        refTag="R-03"
        anchor="#api"
      />
      <IndexRow
        idx="01.c"
        capability="Fetch strategies"
        notes="CacheFirst, NetworkOnly, CacheAndNetwork. Per-call override."
        refTag="R-04"
        anchor="#strategies"
      />
      <IndexRow
        idx="01.d"
        capability="Normalized store"
        notes="Entity rows keyed by type and id, dedup, optional persistence."
        refTag="R-05"
        anchor="#store"
      />
      <IndexRow
        idx="01.e"
        capability="WebSocket subscriptions"
        notes="connection_init, reconnect, store write-through on push."
        refTag="R-05"
        anchor="#store"
      />
      <IndexRow
        idx="01.f"
        capability="Razor / Blazor components"
        notes="UseQuery, UseSubscription, UseFragment, DataComponent<T>."
        refTag="R-06"
        anchor="#razor"
      />
      <IndexRow
        idx="01.g"
        capability="Nitro IDE pairing"
        notes="Draft operations in Nitro, save as .graphql, codegen picks up."
        refTag="R-07"
        anchor="#nitro"
      />
      <IndexRow
        idx="01.h"
        capability="Open source"
        notes="MIT license, repository, package, CLI, platform."
        refTag="R-08"
        anchor="#footer"
      />
    </Card>
  );
}

// -----------------------------------------------------------------------------
// 02 Pipeline ledger
// -----------------------------------------------------------------------------

function PipelineLedger() {
  return (
    <Card
      id="pipeline"
      section="R-02"
      title="Build pipeline: dotnet build runs codegen"
    >
      <Row idx="02.a" label=".graphqlrc.json">
        <div className="flex flex-col gap-2 lg:flex-row lg:items-baseline lg:justify-between lg:gap-6">
          <span className="text-cc-ink-dim text-caption">
            Names the client, namespace, and schema URL.
          </span>
          <Code>
            {`{ "schema": "schema.graphql", "documents": "**/*.graphql" }`}
          </Code>
        </div>
      </Row>
      <Row idx="02.b" label="schema.graphql">
        <div className="flex flex-col gap-2 lg:flex-row lg:items-baseline lg:justify-between lg:gap-6">
          <span className="text-cc-ink-dim text-caption">
            Pulled from any spec-compliant server by the CLI.
          </span>
          <Code>dotnet graphql init https://api.example.com/graphql</Code>
        </div>
      </Row>
      <Row idx="02.c" label="*.graphql">
        <div className="flex flex-col gap-2 lg:flex-row lg:items-baseline lg:justify-between lg:gap-6">
          <span className="text-cc-ink-dim text-caption">
            Your operations live next to the code that uses them.
          </span>
          <Code>
            <span style={T.field}>query</span>{" "}
            <span style={T.type}>GetProduct</span>($id: ID!) {`{ ... }`}
          </Code>
        </div>
      </Row>
      <Row idx="02.d" label="MSBuild task">
        <div className="flex flex-col gap-2 lg:flex-row lg:items-baseline lg:justify-between lg:gap-6">
          <span className="text-cc-ink-dim text-caption">
            StrawberryShake.Tools NuGet wires the codegen target. Runs in CI.
          </span>
          <Code>
            &lt;PackageReference Include=&quot;StrawberryShake.Tools&quot;
            PrivateAssets=&quot;all&quot; /&gt;
          </Code>
        </div>
      </Row>
      <Row idx="02.e" label="Emitted .cs">
        <div className="flex flex-col gap-2 lg:flex-row lg:items-baseline lg:justify-between lg:gap-6">
          <span className="text-cc-ink-dim text-caption">
            Typed client, immutable records, fragments, DI extension method.
          </span>
          <Code>
            <span style={T.type}>ICatalogClient</span>,{" "}
            <span style={T.type}>GetProductResult</span>,{" "}
            <span style={T.fn}>AddCatalogClient</span>()
          </Code>
        </div>
      </Row>
    </Card>
  );
}

// -----------------------------------------------------------------------------
// 03 Typed client API surface
// -----------------------------------------------------------------------------

interface ApiRowProps {
  readonly idx: string;
  readonly signature: ReactNode;
  readonly description: string;
}

function ApiRow({ idx, signature, description }: ApiRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-12 items-baseline gap-4 border-t py-2.5 first:border-t-0">
      <span className="text-cc-ink-dim col-span-2 font-mono text-[11px] tabular-nums lg:col-span-1">
        {idx}
      </span>
      <span className="text-cc-heading col-span-10 font-mono text-[12px] break-all lg:col-span-6">
        {signature}
      </span>
      <span className="text-cc-ink text-caption col-span-12 lg:col-span-5">
        {description}
      </span>
    </div>
  );
}

function ApiSurface() {
  return (
    <Card id="api" section="R-03" title="Typed client API surface">
      <div className="border-cc-card-border text-cc-ink-dim hidden grid-cols-12 gap-4 border-b pb-2 font-mono text-[10px] tracking-widest uppercase lg:grid">
        <span className="col-span-1">idx</span>
        <span className="col-span-6">signature</span>
        <span className="col-span-5">description</span>
      </div>
      <ApiRow
        idx="03.a"
        signature={<span style={T.type}>ICatalogClient</span>}
        description="Generated client interface, one per .graphqlrc.json client."
      />
      <ApiRow
        idx="03.b"
        signature={
          <>
            client.GetProduct.<span style={T.fn}>ExecuteAsync</span>(id)
          </>
        }
        description="One-shot async call. Returns Task<IOperationResult<TResult>>."
      />
      <ApiRow
        idx="03.c"
        signature={
          <>
            client.GetProduct.<span style={T.fn}>Watch</span>(id, strategy)
          </>
        }
        description="Reactive subscription on the store. Emits cache and network values."
      />
      <ApiRow
        idx="03.d"
        signature={
          <>
            <span style={T.type}>IObservable</span>&lt;
            <span style={T.type}>IOperationResult</span>&lt;TResult&gt;&gt;
          </>
        }
        description="Result stream from Watch. Re-emits when the store row changes."
      />
      <ApiRow
        idx="03.e"
        signature={<>result.Data</>}
        description="Immutable nullable-aware record. Decomposable with deconstruction."
      />
      <ApiRow
        idx="03.f"
        signature={<>result.Errors</>}
        description="Aggregated GraphQL errors with path, message, and extensions."
      />
      <ApiRow
        idx="03.g"
        signature={
          <>
            result.<span style={T.fn}>IsErrorResult</span>()
          </>
        }
        description="True if the response carries errors or transport failed."
      />
    </Card>
  );
}

// -----------------------------------------------------------------------------
// 04 Fetch strategies matrix
// -----------------------------------------------------------------------------

interface StrategyRowProps {
  readonly idx: string;
  readonly name: string;
  readonly behavior: string;
  readonly whenToUse: string;
  readonly emits: string;
}

function StrategyRow({
  idx,
  name,
  behavior,
  whenToUse,
  emits,
}: StrategyRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-12 items-baseline gap-4 border-t py-2.5 first:border-t-0">
      <span className="text-cc-ink-dim col-span-2 font-mono text-[11px] tabular-nums lg:col-span-1">
        {idx}
      </span>
      <span className="text-cc-accent text-caption col-span-10 font-mono tracking-tight lg:col-span-3">
        {name}
      </span>
      <span className="text-cc-ink text-caption col-span-12 lg:col-span-3">
        {behavior}
      </span>
      <span className="text-cc-ink-dim text-caption col-span-12 lg:col-span-3">
        {whenToUse}
      </span>
      <span className="text-cc-ink text-caption col-span-12 font-mono tabular-nums lg:col-span-2">
        {emits}
      </span>
    </div>
  );
}

function FetchStrategies() {
  return (
    <Card id="strategies" section="R-04" title="Fetch strategies matrix">
      <div className="border-cc-card-border text-cc-ink-dim hidden grid-cols-12 gap-4 border-b pb-2 font-mono text-[10px] tracking-widest uppercase lg:grid">
        <span className="col-span-1">idx</span>
        <span className="col-span-3">strategy</span>
        <span className="col-span-3">behavior</span>
        <span className="col-span-3">when to use</span>
        <span className="col-span-2">emits</span>
      </div>
      <StrategyRow
        idx="04.a"
        name="CacheFirst"
        behavior="Store hit returns first, no request issued."
        whenToUse="Detail pages already warmed by a list query."
        emits="1 value"
      />
      <StrategyRow
        idx="04.b"
        name="NetworkOnly"
        behavior="Always fetch, then write the result through the store."
        whenToUse="Mutation follow-up or strict freshness reads."
        emits="1 value"
      />
      <StrategyRow
        idx="04.c"
        name="CacheAndNetwork"
        behavior="Yield cache immediately, refresh in the background."
        whenToUse="Snappy launches and detail pages on cold start."
        emits="1 or 2 values"
      />
      <div className="text-cc-ink-dim border-cc-card-border text-caption mt-3 border-t pt-3">
        Strategy is a per-Watch override on top of a per-client default. The
        result stream emits cache and network values in order.
      </div>
    </Card>
  );
}

// -----------------------------------------------------------------------------
// 05 Store + subscriptions
// -----------------------------------------------------------------------------

function StoreAndSubscriptions() {
  return (
    <Card id="store" section="R-05" title="Normalized store and subscriptions">
      <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
        05.1 / Normalized entity store
      </div>
      <Row idx="05.1a" label="Keying">
        Each result is denormalized into rows keyed by GraphQL type and id, the
        same model Relay and Apollo use for client caches.
      </Row>
      <Row idx="05.1b" label="Deduplication">
        A query that returns the same product as a list item and as a detail
        shares one row. Watch a query, re-render when that row changes from any
        operation.
      </Row>
      <Row idx="05.1c" label="Persistence">
        Optional SQLite or LiteDB persistence rehydrates the store on next
        launch. Combine with CacheAndNetwork for instant cold-start reads.
      </Row>

      <div className="text-cc-ink-dim mt-8 font-mono text-[10.5px] tracking-widest uppercase">
        05.2 / WebSocket subscriptions
      </div>
      <Row idx="05.2a" label="Transport">
        WebSocket carries the connection_init payload for auth, plus reconnect
        handling, on top of the GraphQL-over-WebSocket protocol.
      </Row>
      <Row idx="05.2b" label="Watch surface">
        Subscriptions look like queries on the typed client. Same Watch surface,
        same IObservable&lt;Result&gt; output.
      </Row>
      <Row idx="05.2c" label="Store write-through">
        Pushed values flow into the entity store, so any open query, fragment,
        or Razor component reflects the update without a separate event-handler
        pipeline.
      </Row>
    </Card>
  );
}

// -----------------------------------------------------------------------------
// 06 Razor / Blazor component reference
// -----------------------------------------------------------------------------

interface RazorRowProps {
  readonly idx: string;
  readonly component: string;
  readonly props: string;
  readonly slots: string;
}

function RazorRow({ idx, component, props, slots }: RazorRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-12 items-baseline gap-4 border-t py-2.5 first:border-t-0">
      <span className="text-cc-ink-dim col-span-2 font-mono text-[11px] tabular-nums lg:col-span-1">
        {idx}
      </span>
      <span className="text-cc-heading text-caption col-span-10 font-mono lg:col-span-3">
        {component}
      </span>
      <span className="text-cc-ink text-caption col-span-12 font-mono lg:col-span-4">
        {props}
      </span>
      <span className="text-cc-ink-dim text-caption col-span-12 font-mono lg:col-span-4">
        {slots}
      </span>
    </div>
  );
}

function RazorReference() {
  return (
    <Card id="razor" section="R-06" title="Razor / Blazor component reference">
      <div className="border-cc-card-border text-cc-ink-dim hidden grid-cols-12 gap-4 border-b pb-2 font-mono text-[10px] tracking-widest uppercase lg:grid">
        <span className="col-span-1">idx</span>
        <span className="col-span-3">component</span>
        <span className="col-span-4">props</span>
        <span className="col-span-4">slots</span>
      </div>
      <RazorRow
        idx="06.a"
        component="<UseQuery>"
        props="TResult, Operation"
        slots="Pending, Error, ChildContent"
      />
      <RazorRow
        idx="06.b"
        component="<UseSubscription>"
        props="TResult, Subscribe"
        slots="Pending, Error, ChildContent"
      />
      <RazorRow
        idx="06.c"
        component="<UseFragment>"
        props="TResult, Fragment"
        slots="ChildContent"
      />
      <RazorRow
        idx="06.d"
        component="DataComponent<T>"
        props="OverrideOnParametersSetAsync"
        slots="Pending, Error, ChildContent (inherited)"
      />
      <div className="text-cc-ink-dim border-cc-card-border text-caption mt-3 border-t pt-3">
        Server, WebAssembly, and hybrid Blazor projects all use the same client.
        Works inside .NET MAUI for typed GraphQL on iOS, Android, and desktop.
      </div>
    </Card>
  );
}

// -----------------------------------------------------------------------------
// 07 Nitro exhibit
// -----------------------------------------------------------------------------

function NitroExhibit() {
  return (
    <Card
      id="nitro"
      section="R-07"
      title="Exhibit A / draft operations in Nitro"
    >
      <p className="text-cc-ink-dim text-caption max-w-prose">
        Nitro is the GraphQL IDE that ships with the Hot Chocolate server, and
        the surface most teams use to draft operations before saving them as
        .graphql files. Browse the schema, run an operation against the live
        endpoint, copy the document into the codegen pipeline.
      </p>
      <div className="border-cc-card-border bg-cc-surface mt-5 overflow-hidden border">
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[10.5px] tracking-widest uppercase">
          <span>Exhibit A / Nitro IDE</span>
          <span className="text-cc-accent">live at /graphql</span>
        </div>
        <NitroCompose />
      </div>
    </Card>
  );
}

// -----------------------------------------------------------------------------
// 08 Footer + CTA
// -----------------------------------------------------------------------------

function FooterSpec() {
  return (
    <section
      id="footer"
      className="border-cc-card-border scroll-mt-24 border-t"
    >
      <div className="flex items-start gap-0 py-5 sm:py-6">
        <GutterIndex value="R-08" />
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline gap-3">
            <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
              R-08
            </span>
            <span className="text-cc-heading text-h6 font-mono font-semibold tracking-[0.18em] uppercase">
              Footer spec / get started
            </span>
          </div>

          <div className="mt-4 lg:max-w-3xl">
            <Row idx="08.a" label="Repository">
              <a
                href="https://github.com/ChilliCream/graphql-platform"
                className="text-cc-accent font-mono hover:underline"
              >
                github.com/ChilliCream/graphql-platform
              </a>
            </Row>
            <Row idx="08.b" label="License">
              MIT. Use in commercial work, fork, vendor, audit.
            </Row>
            <Row idx="08.c" label="Package">
              <Code className="text-cc-ink">StrawberryShake.Server</Code> on
              NuGet, plus the StrawberryShake.Razor and StrawberryShake.Blazor
              integrations.
            </Row>
            <Row idx="08.d" label="CLI">
              <Code className="text-cc-ink">
                dotnet tool install StrawberryShake.Tools
              </Code>
            </Row>
            <Row idx="08.e" label="Platform">
              Part of the ChilliCream GraphQL platform alongside Hot Chocolate.
            </Row>
            <Row idx="08.f" label="Capabilities">
              <ul className="flex flex-col gap-1.5">
                {[
                  "MSBuild codegen at build time, not at runtime.",
                  "Normalized reactive entity store with optional persistence.",
                  "Three fetch strategies, per-call override.",
                  "WebSocket subscriptions, store write-through.",
                  "Razor and Blazor components, MAUI compatible.",
                  "Persisted operations: ship hashes, lock production.",
                ].map((c) => (
                  <li
                    key={c}
                    className="text-cc-ink text-caption flex items-start gap-2"
                  >
                    <span className="text-cc-accent mt-1 shrink-0" aria-hidden>
                      <CheckIcon size={12} />
                    </span>
                    <span>{c}</span>
                  </li>
                ))}
              </ul>
            </Row>
          </div>

          {/* Closing CTA row, capped by a single cc-accent hairline */}
          <div className="relative mt-6 pt-6">
            <div
              aria-hidden
              className="bg-cc-accent pointer-events-none absolute inset-x-0 top-0 h-px"
            />
            <div className="lg:max-w-3xl">
              <Row idx="08.g" label="Get started">
                <div className="flex flex-wrap items-center gap-3 lg:justify-end">
                  <SolidButton href="/docs/strawberryshake">
                    Get Started
                  </SolidButton>
                  <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                    View on GitHub
                  </OutlineButton>
                </div>
              </Row>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function StrawberryShakePreviewV5() {
  return (
    <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
      <Masthead />
      <CapabilityIndex />
      <PipelineLedger />
      <ApiSurface />
      <FetchStrategies />
      <StoreAndSubscriptions />
      <RazorReference />
      <NitroExhibit />
      <FooterSpec />
    </div>
  );
}
