import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Hot Chocolate Reference, GraphQL Server for .NET",
  description:
    "Reference data sheet for Hot Chocolate, the open source GraphQL server for .NET. Source-generated schema, DataLoader batching, subscriptions, OpenTelemetry, Fusion.",
  keywords: [
    "Hot Chocolate",
    "GraphQL server for .NET",
    "C# GraphQL",
    "ASP.NET Core GraphQL",
    "DataLoader",
    "Green Donut",
    "GraphQL subscriptions",
    "OpenTelemetry",
    "Apollo Federation",
    "Fusion",
    "Roslyn source generator",
    "MIT",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Hot Chocolate Reference, GraphQL Server for .NET",
    description:
      "Printed-style reference sheet for Hot Chocolate. C# is the schema, source-generated resolvers, batched DataLoaders, OpenTelemetry, Fusion federation, MIT.",
    type: "website",
  },
};

// Brand spectrum allowed at most once per screen. Used on the closing CTA rule.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface SectionStripProps {
  readonly index: string;
  readonly slug: string;
  readonly eyebrow: string;
  readonly tag: string;
}

/**
 * A four-column header strip in font-mono uppercase. Sits at the top of every
 * section, evoking a printed API reference: index, slug, eyebrow, tag.
 */
function SectionStrip({ index, slug, eyebrow, tag }: SectionStripProps) {
  return (
    <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-12 items-center gap-3 border-t pt-3 font-mono text-[10.5px] tracking-[0.2em] uppercase">
      <span className="text-cc-accent col-span-2 tabular-nums sm:col-span-1">
        {index}
      </span>
      <span className="text-cc-heading col-span-5 truncate sm:col-span-3">
        {slug}
      </span>
      <span className="col-span-5 truncate sm:col-span-6">{eyebrow}</span>
      <span className="border-cc-card-border col-span-12 inline-flex h-5 w-fit items-center justify-self-start rounded-sm border px-1.5 tabular-nums sm:col-span-2 sm:justify-self-end">
        {tag}
      </span>
    </div>
  );
}

interface KvRowProps {
  readonly k: string;
  readonly v: ReactNode;
}

/** One row of a dense key/value definition list with a vertical hairline. */
function KvRow({ k, v }: KvRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-12 border-b last:border-b-0">
      <dt className="border-cc-card-border text-cc-ink-dim col-span-5 border-r px-3 py-2 font-mono text-[11.5px] tracking-tight uppercase">
        {k}
      </dt>
      <dd className="text-cc-ink col-span-7 px-3 py-2 font-mono text-[11.5px] tabular-nums">
        {v}
      </dd>
    </div>
  );
}

interface KvListProps {
  readonly caption?: string;
  readonly rows: readonly { readonly k: string; readonly v: ReactNode }[];
}

function KvList({ caption, rows }: KvListProps) {
  return (
    <div className="border-cc-card-border border">
      {caption ? (
        <div className="border-cc-card-border text-cc-ink-dim border-b px-3 py-1.5 font-mono text-[10.5px] tracking-[0.2em] uppercase">
          {caption}
        </div>
      ) : null}
      <dl>
        {rows.map((r) => (
          <KvRow key={r.k} k={r.k} v={r.v} />
        ))}
      </dl>
    </div>
  );
}

interface TocItem {
  readonly id: string;
  readonly index: string;
  readonly label: string;
}

const TOC: readonly TocItem[] = [
  { id: "manifest", index: "00", label: "manifest" },
  { id: "capabilities", index: "01", label: "capabilities" },
  { id: "composition", index: "02", label: "composition" },
  { id: "authoring", index: "03", label: "authoring" },
  { id: "dataloader", index: "04", label: "dataloader" },
  { id: "subscriptions", index: "05", label: "subscriptions" },
  { id: "observability", index: "06", label: "observability" },
  { id: "federation", index: "07", label: "federation" },
  { id: "index", index: "08", label: "index" },
  { id: "ide", index: "09", label: "ide" },
  { id: "license", index: "10", label: "license" },
  { id: "invoke", index: "11", label: "invoke" },
];

// -----------------------------------------------------------------------------
// Sections
// -----------------------------------------------------------------------------

function ManifestHero() {
  const spec: readonly { readonly k: string; readonly v: string }[] = [
    { k: "License", v: "MIT" },
    { k: "Runtime", v: "ASP.NET Core" },
    { k: "Spec", v: "GraphQL 2025" },
    { k: "Transports", v: "HTTP / WS / SSE" },
  ];

  return (
    <section id="manifest" className="scroll-mt-24 pt-10 pb-14 sm:pt-16">
      <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-12 items-center gap-3 border-t pt-3 font-mono text-[10.5px] tracking-[0.2em] uppercase">
        <span className="text-cc-accent col-span-2 tabular-nums sm:col-span-1">
          00
        </span>
        <span className="text-cc-heading col-span-5 truncate sm:col-span-3">
          /manifest
        </span>
        <span className="col-span-5 truncate sm:col-span-6">
          GraphQL server for .NET
        </span>
        <span className="border-cc-card-border col-span-12 inline-flex h-5 w-fit items-center justify-self-start rounded-sm border px-1.5 tabular-nums sm:col-span-2 sm:justify-self-end">
          rev. 2025.6
        </span>
      </div>

      <div className="mt-8 grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-7">
          <h1 className="text-cc-heading font-heading text-hero text-balance">
            Your C# is the schema.
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-sm leading-relaxed">
            Hot Chocolate is the open-source GraphQL server for .NET. Annotate a
            partial class, write idiomatic C# resolvers, and a Roslyn source
            generator emits the schema, the resolver pipeline, and DataLoader
            infrastructure at build time. One server speaks HTTP, WebSocket, and
            Server-Sent Events, and the same code can run standalone or as a
            Fusion subgraph later.
          </p>
          <div className="mt-7 flex flex-wrap gap-3">
            <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
        <div className="lg:col-span-5">
          <div className="border-cc-card-border grid grid-cols-2 border">
            {spec.map((s, i) => (
              <div
                key={s.k}
                className={[
                  "px-4 py-4",
                  i % 2 === 0 ? "border-cc-card-border border-r" : "",
                  i < 2 ? "border-cc-card-border border-b" : "",
                ].join(" ")}
              >
                <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.2em] uppercase">
                  {s.k}
                </div>
                <div className="text-cc-heading font-heading mt-2 text-xl font-semibold tabular-nums">
                  {s.v}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

interface CapabilityRow {
  readonly capability: string;
  readonly surface: string;
  readonly status: string;
}

function CapabilitiesTable() {
  const rows: readonly CapabilityRow[] = [
    {
      capability: "Schema generation",
      surface: "Roslyn source generator",
      status: "stable",
    },
    {
      capability: "DataLoader batching",
      surface: "Green Donut, [DataLoader]",
      status: "stable",
    },
    {
      capability: "Subscriptions",
      surface: "graphql-ws, graphql-sse",
      status: "stable",
    },
    {
      capability: "Observability",
      surface: "OpenTelemetry, OTLP",
      status: "stable",
    },
    {
      capability: "Federation",
      surface: "Fusion, Apollo Federation",
      status: "stable",
    },
    {
      capability: "Cost analysis",
      surface: "@cost, @listSize",
      status: "stable",
    },
  ];

  return (
    <section
      id="capabilities"
      className="scroll-mt-24 py-12 sm:py-14"
      aria-label="Capabilities"
    >
      <SectionStrip
        index="01"
        slug="/capabilities"
        eyebrow="Surface index"
        tag="6 rows"
      />
      <h2 className="text-cc-heading font-heading text-h4 mt-6 tracking-tight">
        Capabilities at a glance.
      </h2>
      <div className="border-cc-card-border mt-6 border">
        <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-12 border-b font-mono text-[10.5px] tracking-[0.2em] uppercase">
          <div className="border-cc-card-border col-span-5 border-r px-3 py-2">
            Capability
          </div>
          <div className="border-cc-card-border col-span-5 border-r px-3 py-2">
            Surface
          </div>
          <div className="col-span-2 px-3 py-2">Status</div>
        </div>
        {rows.map((r, i) => (
          <div
            key={r.capability}
            className={[
              "grid grid-cols-12",
              i === rows.length - 1 ? "" : "border-cc-card-border border-b",
            ].join(" ")}
          >
            <div className="border-cc-card-border text-cc-ink col-span-5 border-r px-3 py-2 text-sm">
              {r.capability}
            </div>
            <div className="border-cc-card-border text-cc-ink-dim col-span-5 border-r px-3 py-2 font-mono text-[11.5px]">
              {r.surface}
            </div>
            <div className="text-cc-accent col-span-2 inline-flex items-center gap-2 px-3 py-2 font-mono text-[11.5px] uppercase">
              <CheckIcon size={12} />
              <span>{r.status}</span>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

interface EntryProps {
  readonly id: string;
  readonly index: string;
  readonly slug: string;
  readonly eyebrow: string;
  readonly tag: string;
  readonly title: string;
  readonly children: ReactNode;
  readonly spec: ReactNode;
  readonly footer?: ReactNode;
}

function Entry({
  id,
  index,
  slug,
  eyebrow,
  tag,
  title,
  children,
  spec,
  footer,
}: EntryProps) {
  return (
    <section id={id} className="scroll-mt-24 py-12 sm:py-14">
      <SectionStrip index={index} slug={slug} eyebrow={eyebrow} tag={tag} />
      <div className="mt-6 grid gap-8 lg:grid-cols-12 lg:gap-10">
        <div className="lg:col-span-7">
          <h2 className="text-cc-heading font-heading text-h4 tracking-tight">
            {title}
          </h2>
          <div className="text-cc-prose mt-4 max-w-2xl text-sm leading-relaxed">
            {children}
          </div>
        </div>
        <div className="lg:col-span-5">{spec}</div>
      </div>
      {footer ? <div className="mt-6">{footer}</div> : null}
    </section>
  );
}

/** A compact SDL snippet rendered as a code line block. */
function SdlSnippet() {
  const lines: readonly string[] = [
    "# Catalog/Query.cs -> generated SDL",
    "type Query {",
    "  productById(id: ID!): Product",
    "}",
    "",
    'extend type Product @key(fields: "id") {',
    "  reviews: [Review!]!",
    "}",
  ];
  return (
    <div className="border-cc-card-border border">
      <div className="border-cc-card-border text-cc-ink-dim border-b px-3 py-1.5 font-mono text-[10.5px] tracking-[0.2em] uppercase">
        emitted.graphql
      </div>
      <pre className="text-cc-ink overflow-x-auto px-3 py-3 font-mono text-[11.5px] leading-5">
        {lines.join("\n")}
      </pre>
    </div>
  );
}

interface BatchRow {
  readonly seq: string;
  readonly call: string;
  readonly id: string;
  readonly resolved: string;
}

function BatchTable() {
  const rows: readonly BatchRow[] = [
    { seq: "01", call: "product(id: 1)", id: "0x01", resolved: "batched" },
    { seq: "02", call: "product(id: 2)", id: "0x02", resolved: "batched" },
    { seq: "03", call: "product(id: 3)", id: "0x03", resolved: "batched" },
    { seq: "04", call: "product(id: 4)", id: "0x04", resolved: "batched" },
    { seq: "05", call: "product(id: 5)", id: "0x05", resolved: "batched" },
  ];
  return (
    <div className="border-cc-card-border border">
      <div className="border-cc-card-border text-cc-ink-dim border-b px-3 py-1.5 font-mono text-[10.5px] tracking-[0.2em] uppercase">
        5 requests, 1 LoadAsync call
      </div>
      <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-12 border-b font-mono text-[10.5px] tracking-[0.2em] uppercase">
        <div className="border-cc-card-border col-span-2 border-r px-3 py-1.5">
          Seq
        </div>
        <div className="border-cc-card-border col-span-5 border-r px-3 py-1.5">
          Call
        </div>
        <div className="border-cc-card-border col-span-3 border-r px-3 py-1.5">
          Id
        </div>
        <div className="col-span-2 px-3 py-1.5">Resolved</div>
      </div>
      {rows.map((r, i) => (
        <div
          key={r.seq}
          className={[
            "grid grid-cols-12",
            i === rows.length - 1 ? "" : "border-cc-card-border border-b",
          ].join(" ")}
        >
          <div className="border-cc-card-border text-cc-accent col-span-2 border-r px-3 py-1.5 font-mono text-[11.5px] tabular-nums">
            {r.seq}
          </div>
          <div className="border-cc-card-border text-cc-ink col-span-5 border-r px-3 py-1.5 font-mono text-[11.5px]">
            {r.call}
          </div>
          <div className="border-cc-card-border text-cc-ink-dim col-span-3 border-r px-3 py-1.5 font-mono text-[11.5px] tabular-nums">
            {r.id}
          </div>
          <div className="text-cc-ink-dim col-span-2 px-3 py-1.5 font-mono text-[11.5px]">
            {r.resolved}
          </div>
        </div>
      ))}
    </div>
  );
}

interface SpanRow {
  readonly span: string;
  readonly depth: string;
  readonly bar: string;
  readonly ms: string;
}

function SpanTable() {
  const rows: readonly SpanRow[] = [
    { span: "graphql.request", depth: "0", bar: "##############", ms: "42" },
    { span: "graphql.execute", depth: "1", bar: " ############", ms: "38" },
    { span: "graphql.parse+validate", depth: "2", bar: "  ######", ms: "9" },
    { span: "resolve product", depth: "2", bar: "        ####", ms: "18" },
    { span: "dataloader.batch", depth: "3", bar: "         ###", ms: "12" },
    { span: "db", depth: "4", bar: "          #", ms: "6" },
  ];
  return (
    <div className="border-cc-card-border border">
      <div className="border-cc-card-border text-cc-ink-dim border-b px-3 py-1.5 font-mono text-[10.5px] tracking-[0.2em] uppercase">
        trace_id 7f8a, 6 spans
      </div>
      <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-12 border-b font-mono text-[10.5px] tracking-[0.2em] uppercase">
        <div className="border-cc-card-border col-span-5 border-r px-3 py-1.5">
          Span
        </div>
        <div className="border-cc-card-border col-span-1 border-r px-3 py-1.5">
          D
        </div>
        <div className="border-cc-card-border col-span-4 border-r px-3 py-1.5">
          Width
        </div>
        <div className="col-span-2 px-3 py-1.5">ms</div>
      </div>
      {rows.map((r, i) => (
        <div
          key={r.span}
          className={[
            "grid grid-cols-12",
            i === rows.length - 1 ? "" : "border-cc-card-border border-b",
          ].join(" ")}
        >
          <div className="border-cc-card-border text-cc-ink col-span-5 border-r px-3 py-1.5 font-mono text-[11.5px]">
            {r.span}
          </div>
          <div className="border-cc-card-border text-cc-ink-dim col-span-1 border-r px-3 py-1.5 font-mono text-[11.5px] tabular-nums">
            {r.depth}
          </div>
          <div className="border-cc-card-border text-cc-accent col-span-4 overflow-hidden border-r px-3 py-1.5 font-mono text-[11.5px] whitespace-pre">
            {r.bar}
          </div>
          <div className="text-cc-ink col-span-2 px-3 py-1.5 font-mono text-[11.5px] tabular-nums">
            {r.ms}
          </div>
        </div>
      ))}
    </div>
  );
}

interface FedRow {
  readonly mode: string;
  readonly plan: string;
  readonly output: string;
}

function FederationMatrix() {
  const rows: readonly FedRow[] = [
    { mode: "Standalone", plan: "single server", output: "schema.graphql" },
    { mode: "Fusion", plan: "build-time plan", output: "fusion plan" },
    { mode: "Apollo", plan: "spec subgraph", output: "_service SDL" },
    { mode: "Cost", plan: "@cost, @listSize", output: "policy" },
  ];
  return (
    <div className="border-cc-card-border border">
      <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-12 border-b font-mono text-[10.5px] tracking-[0.2em] uppercase">
        <div className="border-cc-card-border col-span-4 border-r px-3 py-1.5">
          Mode
        </div>
        <div className="border-cc-card-border col-span-4 border-r px-3 py-1.5">
          Plan
        </div>
        <div className="col-span-4 px-3 py-1.5">Output</div>
      </div>
      {rows.map((r, i) => (
        <div
          key={r.mode}
          className={[
            "grid grid-cols-12",
            i === rows.length - 1 ? "" : "border-cc-card-border border-b",
          ].join(" ")}
        >
          <div className="border-cc-card-border text-cc-ink col-span-4 border-r px-3 py-1.5 font-mono text-[11.5px]">
            {r.mode}
          </div>
          <div className="border-cc-card-border text-cc-ink-dim col-span-4 border-r px-3 py-1.5 font-mono text-[11.5px]">
            {r.plan}
          </div>
          <div className="text-cc-ink-dim col-span-4 px-3 py-1.5 font-mono text-[11.5px]">
            {r.output}
          </div>
        </div>
      ))}
    </div>
  );
}

interface IndexMetric {
  readonly value: string;
  readonly label: string;
}

function IndexMetrics() {
  const metrics: readonly IndexMetric[] = [
    { value: "3", label: "Operations" },
    { value: "2", label: "Generators" },
    { value: "3", label: "Transports" },
    { value: "3", label: "Federation modes" },
  ];
  return (
    <section id="index" className="scroll-mt-24 py-14 sm:py-16">
      <SectionStrip
        index="08"
        slug="/index"
        eyebrow="Surface totals"
        tag="metrics"
      />
      <div className="border-cc-card-border mt-6 grid grid-cols-2 border lg:grid-cols-4">
        {metrics.map((m, i) => (
          <div
            key={m.label}
            className={[
              "px-5 py-7",
              i % 2 === 0 ? "border-cc-card-border border-r" : "",
              i < 2 ? "border-cc-card-border border-b lg:border-b-0" : "",
              i === 2 ? "lg:border-cc-card-border lg:border-r" : "",
            ].join(" ")}
          >
            <div className="text-cc-heading font-heading text-h2 tabular-nums">
              {m.value}
            </div>
            <div className="text-cc-ink-dim mt-2 font-mono text-[10.5px] tracking-[0.2em] uppercase">
              {m.label}
            </div>
          </div>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-4 font-mono text-[10.5px] tracking-[0.2em] uppercase">
        Operations: query, mutation, subscription. Transports: http, ws, sse.
      </p>
    </section>
  );
}

function IdeSection() {
  return (
    <section id="ide" className="scroll-mt-24 py-12 sm:py-14">
      <SectionStrip
        index="09"
        slug="/ide"
        eyebrow="Embedded developer IDE"
        tag="nitro"
      />
      <div className="mt-6 grid items-end gap-6 lg:grid-cols-12">
        <div className="lg:col-span-8">
          <h2 className="text-cc-heading font-heading text-h4 tracking-tight">
            A GraphQL IDE ships with every server.
          </h2>
          <p className="text-cc-prose mt-4 max-w-2xl text-sm leading-relaxed">
            Run your server and the Nitro GraphQL IDE is served from the
            endpoint. Browse the schema, draft operations against your live
            resolvers, inspect responses, and share documents with the rest of
            the team.
          </p>
        </div>
        <div className="lg:col-span-4 lg:text-right">
          <p className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.2em] uppercase">
            live at /graphql
          </p>
        </div>
      </div>
      <div className="border-cc-card-border bg-cc-surface mt-6 overflow-hidden border">
        <NitroCompose />
      </div>
    </section>
  );
}

function LicenseSection() {
  const rows: readonly { readonly k: string; readonly v: string }[] = [
    { k: "License", v: "MIT" },
    { k: "Runtime", v: ".NET / ASP.NET Core" },
    { k: "Spec", v: "GraphQL 2025" },
    { k: "Transports", v: "HTTP / WS / SSE" },
    { k: "Federation", v: "Fusion + Apollo" },
    { k: "Client", v: "Strawberry Shake" },
  ];
  return (
    <section id="license" className="scroll-mt-24 py-12 sm:py-14">
      <SectionStrip index="10" slug="/license" eyebrow="MIT proof" tag="open" />
      <div className="mt-6 grid items-start gap-10 lg:grid-cols-12">
        <div className="lg:col-span-7">
          <h2 className="text-cc-heading font-heading text-h4 tracking-tight">
            Open source, in production, and free to use.
          </h2>
          <p className="text-cc-prose mt-4 max-w-2xl text-sm leading-relaxed">
            Hot Chocolate has been developed in the open for years and is
            released under the MIT license. Use it in commercial work, fork it,
            vendor it, audit it. The codebase, the issue tracker, the roadmap,
            and the release notes all live on GitHub.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <SolidButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </SolidButton>
            <OutlineButton href="/docs/hotchocolate">
              Read the docs
            </OutlineButton>
          </div>
        </div>
        <div className="lg:col-span-5">
          <div className="border-cc-card-border border">
            {rows.map((r, i) => (
              <div
                key={r.k}
                className={[
                  "grid grid-cols-12",
                  i === rows.length - 1 ? "" : "border-cc-card-border border-b",
                ].join(" ")}
              >
                <dt className="border-cc-card-border text-cc-ink-dim col-span-5 border-r px-3 py-3 font-mono text-[10.5px] tracking-[0.2em] uppercase">
                  {r.k}
                </dt>
                <dd className="text-cc-heading font-heading col-span-7 px-3 py-3 text-sm tabular-nums">
                  {r.v}
                </dd>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

function ClosingInvoke() {
  return (
    <section id="invoke" className="relative scroll-mt-24 py-14 sm:py-20">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px"
        style={{ background: SPECTRUM }}
      />
      <div className="text-cc-ink-dim grid grid-cols-12 items-center gap-3 pt-3 font-mono text-[10.5px] tracking-[0.2em] uppercase">
        <span className="text-cc-accent col-span-2 tabular-nums sm:col-span-1">
          11
        </span>
        <span className="text-cc-heading col-span-5 truncate sm:col-span-3">
          /invoke
        </span>
        <span className="col-span-5 truncate sm:col-span-6">
          Terminal entry point
        </span>
        <span className="border-cc-card-border col-span-12 inline-flex h-5 w-fit items-center justify-self-start rounded-sm border px-1.5 tabular-nums sm:col-span-2 sm:justify-self-end">
          cli
        </span>
      </div>
      <div className="border-cc-card-border bg-cc-surface mt-6 border">
        <div className="border-cc-card-border text-cc-ink-dim border-b px-3 py-1.5 font-mono text-[10.5px] tracking-[0.2em] uppercase">
          shell
        </div>
        <div className="text-cc-ink flex items-center gap-3 px-4 py-4 font-mono text-sm">
          <span className="text-cc-accent" aria-hidden>
            $
          </span>
          <span className="tabular-nums">dotnet new graphql</span>
        </div>
      </div>
      <div className="mt-8 flex flex-wrap items-center gap-3">
        <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function HotChocolatePreviewV4() {
  return (
    <div className="grid gap-10 lg:grid-cols-12 lg:gap-12">
      {/* Sticky table of contents on the left. */}
      <aside className="hidden lg:col-span-2 lg:block">
        <nav
          aria-label="Page contents"
          className="sticky top-24 max-h-[calc(100vh-6rem)] overflow-y-auto pt-12"
        >
          <div className="text-cc-ink-dim mb-3 font-mono text-[10.5px] tracking-[0.2em] uppercase">
            contents
          </div>
          <ul className="flex flex-col gap-1">
            {TOC.map((item) => (
              <li key={item.id}>
                <a
                  href={`#${item.id}`}
                  className="text-cc-ink-dim hover:text-cc-accent flex items-baseline gap-2 font-mono text-[11px] tracking-tight uppercase"
                >
                  <span className="text-cc-nav-label tabular-nums">
                    {item.index}
                  </span>
                  <span>/{item.label}</span>
                </a>
              </li>
            ))}
          </ul>
        </nav>
      </aside>

      <main className="lg:col-span-10">
        <ManifestHero />
        <CapabilitiesTable />

        <Entry
          id="composition"
          index="02"
          slug="/composition"
          eyebrow="Build-time planning"
          tag="entry 01"
          title="Compose subgraphs at build time, not at runtime."
          spec={
            <KvList
              caption="composition.spec"
              rows={[
                { k: "api", v: "fusion compose" },
                { k: "package", v: "HotChocolate.Fusion" },
                { k: "output", v: "fusion plan" },
                { k: "runtime cost", v: "0 ms (precomputed)" },
                { k: "transport", v: "HTTP" },
              ]}
            />
          }
          footer={<SdlSnippet />}
        >
          Fusion plans composition once, in CI, against the source SDLs. The
          gateway loads a finished query plan and stays cheap to run at the
          edge. Schema changes show up as planning errors before they show up as
          production incidents.
        </Entry>

        <Entry
          id="authoring"
          index="03"
          slug="/authoring"
          eyebrow="Two styles, one schema"
          tag="entry 02"
          title="Implementation-first, or code-first. Pick the style that fits."
          spec={
            <div className="flex flex-col gap-4">
              <KvList
                caption="implementation-first"
                rows={[
                  { k: "trigger", v: "[QueryType]" },
                  { k: "host", v: "partial class" },
                  { k: "emitter", v: "Roslyn generator" },
                  { k: "output", v: "schema + resolvers" },
                ]}
              />
              <KvList
                caption="code-first"
                rows={[
                  { k: "trigger", v: "ObjectType<T>" },
                  { k: "host", v: "fluent descriptor" },
                  { k: "emitter", v: "runtime builder" },
                  { k: "output", v: "schema + resolvers" },
                ]}
              />
            </div>
          }
        >
          Implementation-first is the default: annotate a partial class with
          [QueryType] and the Roslyn source generator emits the schema and
          resolver pipelines from your C#, similar to how Meta built its GraphQL
          server. When you need the schema to diverge from the model, drop into
          the fluent ObjectType&lt;T&gt; descriptor and mix both in the same
          project.
        </Entry>

        <Entry
          id="dataloader"
          index="04"
          slug="/dataloader"
          eyebrow="Green Donut"
          tag="entry 03"
          title="N+1 disappears at the field level."
          spec={
            <KvList
              caption="dataloader.spec"
              rows={[
                { k: "api", v: "[DataLoader]" },
                { k: "package", v: "GreenDonut" },
                { k: "output", v: "loader + interface + DI" },
                { k: "kinds", v: "batch, group" },
                { k: "scope", v: "per request" },
              ]}
            />
          }
          footer={<BatchTable />}
        >
          Green Donut is built into Hot Chocolate. Annotate a static method with
          [DataLoader] and the generator emits the loader class, the interface,
          and the DI registration. Per-request keys are deduplicated, the
          execution engine resolves fields in waves, and every batch dispatches
          together.
        </Entry>

        <Entry
          id="subscriptions"
          index="05"
          slug="/subscriptions"
          eyebrow="Realtime"
          tag="entry 04"
          title="Subscriptions over WebSocket and Server-Sent Events."
          spec={
            <KvList
              caption="subscriptions.spec"
              rows={[
                { k: "api", v: "[SubscriptionType]" },
                { k: "transports", v: "graphql-ws, graphql-sse" },
                { k: "providers", v: "Redis, NATS, Postgres, RabbitMQ" },
                { k: "publisher", v: "ITopicEventSender" },
                { k: "topics", v: "dynamic via [Topic]" },
              ]}
            />
          }
        >
          [SubscriptionType] with [Topic] placeholders gives you dynamic
          per-resource streams. Pick a transport: modern graphql-ws or
          graphql-sse for HTTP/2 and proxy-friendly delivery. Pick a pub/sub
          provider: in-memory for dev, Redis, NATS, Postgres LISTEN/NOTIFY, or
          RabbitMQ for production.
        </Entry>

        <Entry
          id="observability"
          index="06"
          slug="/observability"
          eyebrow="OpenTelemetry"
          tag="entry 05"
          title="OpenTelemetry, native and vendor-neutral."
          spec={
            <KvList
              caption="observability.spec"
              rows={[
                { k: "api", v: "AddHotChocolateInstrumentation()" },
                { k: "package", v: "HotChocolate.Diagnostics" },
                { k: "output", v: "spans + attributes" },
                { k: "layers", v: "server, execution, dataloader" },
                { k: "exporter", v: "OTLP" },
              ]}
            />
          }
          footer={<SpanTable />}
        >
          AddInstrumentation() plus AddHotChocolateInstrumentation() wires Hot
          Chocolate into the proposed GraphQL OTel semantic conventions. Spans
          carry operation type, document hash, trusted document id, per-field
          selection, and DataLoader batch size. Configure an OTLP exporter and
          the traces land in whatever backend you already run.
        </Entry>

        <Entry
          id="federation"
          index="07"
          slug="/federation"
          eyebrow="Federation matrix"
          tag="entry 06"
          title="Fusion-ready, and Apollo Federation spec compatible."
          spec={<FederationMatrix />}
        >
          The same Hot Chocolate server runs three ways. As a single API. As a
          Fusion subgraph composed at build time into a planned gateway schema.
          As an Apollo Federation subgraph for teams already in that ecosystem.
          The resolvers do not change. The choice is operational, not
          architectural.
        </Entry>

        <IndexMetrics />
        <IdeSection />
        <LicenseSection />
        <ClosingInvoke />
      </main>
    </div>
  );
}
