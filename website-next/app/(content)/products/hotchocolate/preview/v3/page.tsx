import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "@/src/design-system/Table";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Hot Chocolate: The .NET-native GraphQL server",
  description:
    "Hot Chocolate is the .NET-native GraphQL server: Roslyn source-generated schema, idiomatic C# resolvers, HTTP/WS/SSE, REST and MCP surfaces, Fusion-ready.",
  keywords: [
    "Hot Chocolate",
    ".NET GraphQL server",
    "C# GraphQL",
    "Roslyn source generator",
    "DataLoader",
    "GraphQL subscriptions",
    "OpenTelemetry",
    "Apollo Federation spec",
    "Fusion",
    "MCP server",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Hot Chocolate: The .NET-native GraphQL server",
    description:
      "Why .NET teams choose Hot Chocolate: source-generated schema, idiomatic C# resolvers, one schema across HTTP/WS/SSE, REST and MCP, Fusion-ready.",
    type: "website",
  },
};

// Brand spectrum gradient. Used exactly once on this screen, on the wedge word.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-[0.25em] uppercase">
      {children}
    </div>
  );
}

interface SectionHeaderProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly lead?: ReactNode;
  readonly align?: "left" | "center";
}

function SectionHeader({
  eyebrow,
  title,
  lead,
  align = "center",
}: SectionHeaderProps) {
  const alignment = align === "center" ? "text-center mx-auto" : "text-left";
  return (
    <div className={`max-w-3xl ${alignment}`}>
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-3 text-3xl tracking-tight sm:text-4xl">
        {title}
      </h2>
      {lead ? <p className="text-cc-ink-dim lead mt-4">{lead}</p> : null}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="mx-auto max-w-4xl text-center">
        <Eyebrow>GraphQL Server for .NET</Eyebrow>
        <h1 className="text-cc-heading font-heading text-hero mt-5 tracking-tight">
          The{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            .NET-native
          </span>{" "}
          GraphQL server.
        </h1>
        <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl">
          Hot Chocolate is the open-source GraphQL server built for C#. Roslyn
          source generation turns your types and resolvers into a spec-compliant
          schema at build time, and one schema serves HTTP, WebSocket, SSE,
          REST, and MCP from the same ASP.NET Core app.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-4">
          <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
        <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
          <span>MIT licensed</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>ASP.NET Core</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>GraphQL 2025 spec</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>Fusion-ready</span>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// "What makes Hot Chocolate the .NET choice" (4 wedge cards)
// ---------------------------------------------------------------------------

interface PillarCardProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

function PillarCard({ index, title, body, bullets }: PillarCardProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-2xl border p-7 backdrop-blur-sm transition-colors">
      <div className="text-cc-accent font-mono text-xs tracking-[0.3em] uppercase">
        {index}
      </div>
      <h3 className="text-cc-heading font-heading mt-3 text-xl tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">{body}</p>
      <ul className="mt-5 space-y-2">
        {bullets.map((b) => (
          <li
            key={b}
            className="text-cc-ink flex items-start gap-2 text-sm leading-snug"
          >
            <span className="text-cc-accent mt-0.5 inline-flex shrink-0">
              <CheckIcon />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

const PILLARS: readonly PillarCardProps[] = [
  {
    index: "01",
    title: "Implementation-first via Roslyn",
    body: "Annotate partial classes with [QueryType], [MutationType], and [DataLoader]. A Roslyn source generator emits the schema, resolver pipeline, and DataLoader infrastructure at build time. No reflection-heavy startup, refactor-safe with nameof.",
    bullets: [
      "Compile-time feedback in your editor",
      "Code-first ObjectType<T> still available",
      "Mix both styles in the same project",
    ],
  },
  {
    index: "02",
    title: "Idiomatic C# resolvers",
    body: "Resolvers are just methods. Arguments are parameters, services are DI-injected without ceremony, and CancellationToken, [Parent], and keyed services flow through naturally. XML doc comments become schema descriptions.",
    bullets: [
      "No DSL, no codegen layer between you and the graph",
      "EF Core, Marten, MongoDB, RavenDB integrations",
      "IQueryable middleware pushes paging, filter, and sort to the database",
    ],
  },
  {
    index: "03",
    title: "One schema, many surfaces",
    body: "The same Hot Chocolate server exposes GraphQL over HTTP, WebSocket, and Server-Sent Events. Annotate operations with @http to generate REST endpoints. Call AddMcp() and MapGraphQLMcp() to expose tools and prompts to AI agents at /graphql/mcp.",
    bullets: [
      "HTTP, WebSocket, SSE transports out of the box",
      "REST via the @http directive (OpenAPI adapter)",
      "MCP server via AddMcp() at /graphql/mcp",
    ],
  },
  {
    index: "04",
    title: "Built for Fusion and the Federation spec",
    body: "A standalone Hot Chocolate server can act as a Fusion subgraph without changing resolvers. Compose at planning time with Fusion, or interop with Apollo subgraphs via the Apollo Federation spec. Start single, scale later.",
    bullets: [
      "Fusion composes subgraph schemas at plan time",
      "Apollo Federation spec for cross-vendor subgraphs",
      "Trusted documents and cost analysis travel with the graph",
    ],
  },
];

function PillarsSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The wedge"
        title="What makes Hot Chocolate the .NET choice"
        lead="Other servers exist for .NET. Hot Chocolate is the one that treats C# as the schema and ASP.NET Core as the runtime, not as a host for a generic GraphQL engine."
      />
      <div className="mt-12 grid gap-5 md:grid-cols-2">
        {PILLARS.map((p) => (
          <PillarCard key={p.index} {...p} />
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Comparison table (factual deltas only)
// ---------------------------------------------------------------------------

type Cell =
  | { kind: "yes"; note?: string }
  | { kind: "no"; note?: string }
  | { kind: "partial"; note?: string }
  | { kind: "text"; note: string };

interface ComparisonRow {
  readonly capability: string;
  readonly hot: Cell;
  readonly graphqlNet: Cell;
  readonly apolloServer: Cell;
}

function CellMark({ cell }: { cell: Cell }) {
  if (cell.kind === "yes") {
    return (
      <span className="text-cc-success inline-flex items-center gap-2 text-sm">
        <CheckIcon />
        <span className="text-cc-ink">{cell.note ?? "Yes"}</span>
      </span>
    );
  }
  if (cell.kind === "partial") {
    return (
      <span className="text-cc-warning inline-flex items-center gap-2 text-sm">
        <span aria-hidden className="font-mono text-xs">
          ~
        </span>
        <span className="text-cc-ink">{cell.note ?? "Partial"}</span>
      </span>
    );
  }
  if (cell.kind === "no") {
    return (
      <span className="text-cc-ink-dim inline-flex items-center gap-2 text-sm">
        <span aria-hidden className="font-mono text-xs">
          /
        </span>
        <span>{cell.note ?? "No"}</span>
      </span>
    );
  }
  return <span className="text-cc-ink text-sm">{cell.note}</span>;
}

const ROWS: readonly ComparisonRow[] = [
  {
    capability: "Schema authoring",
    hot: {
      kind: "text",
      note: "Implementation-first via Roslyn source generator, plus code-first and schema-first",
    },
    graphqlNet: {
      kind: "text",
      note: "Code-first via type registrations and ResolveFieldContext",
    },
    apolloServer: {
      kind: "text",
      note: "Schema-first SDL plus resolver maps (Node.js runtime)",
    },
  },
  {
    capability: "DataLoader / N+1",
    hot: {
      kind: "yes",
      note: "Green Donut, source-generated from [DataLoader]",
    },
    graphqlNet: { kind: "yes", note: "Manual DataLoader registration" },
    apolloServer: { kind: "yes", note: "dataloader package, hand-wired" },
  },
  {
    capability: "Subscriptions",
    hot: {
      kind: "yes",
      note: "WebSocket (graphql-ws) and SSE (graphql-sse); Redis, NATS, Postgres, RabbitMQ",
    },
    graphqlNet: {
      kind: "partial",
      note: "WebSocket subscriptions; transport package separate",
    },
    apolloServer: {
      kind: "partial",
      note: "Subscriptions via separate graphql-ws server",
    },
  },
  {
    capability: "REST surface from the same schema",
    hot: { kind: "yes", note: "@http directive, OpenAPI adapter" },
    graphqlNet: { kind: "no", note: "Not provided" },
    apolloServer: { kind: "no", note: "Not provided" },
  },
  {
    capability: "MCP server surface",
    hot: { kind: "yes", note: "AddMcp() + MapGraphQLMcp() at /graphql/mcp" },
    graphqlNet: { kind: "no", note: "Not provided" },
    apolloServer: { kind: "no", note: "Not provided" },
  },
  {
    capability: "OpenTelemetry",
    hot: {
      kind: "yes",
      note: "Native, aligned with proposed GraphQL OTel conventions",
    },
    graphqlNet: { kind: "partial", note: "Community OTel integrations" },
    apolloServer: {
      kind: "partial",
      note: "Apollo-specific tracing plus OTel via plugin",
    },
  },
  {
    capability: "Federation",
    hot: {
      kind: "yes",
      note: "Fusion (plan-time composition) and Apollo Federation spec",
    },
    graphqlNet: {
      kind: "partial",
      note: "Community Apollo Federation libraries",
    },
    apolloServer: { kind: "yes", note: "Apollo Federation spec (native)" },
  },
  {
    capability: "Runtime",
    hot: { kind: "text", note: "ASP.NET Core (.NET 8+, Azure Functions)" },
    graphqlNet: { kind: "text", note: "ASP.NET Core (.NET)" },
    apolloServer: { kind: "text", note: "Node.js" },
  },
];

function ComparisonSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Honest deltas"
        title="How it lines up against common alternatives"
        lead="Factual side-by-side, not a takedown. Pick the column that matches your stack and read across."
      />
      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl rounded-2xl border p-2 backdrop-blur-sm sm:p-4">
        <Table alternating className="min-w-[640px]">
          <TableHead>
            <TableRow>
              <TableHeaderCell className="w-[22%]">Capability</TableHeaderCell>
              <TableHeaderCell className="w-[30%]">
                Hot Chocolate
              </TableHeaderCell>
              <TableHeaderCell className="w-[24%]">GraphQL.NET</TableHeaderCell>
              <TableHeaderCell className="w-[24%]">
                Apollo Server (JS)
              </TableHeaderCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {ROWS.map((row) => (
              <TableRow key={row.capability}>
                <TableCell className="text-cc-heading font-medium">
                  {row.capability}
                </TableCell>
                <TableCell>
                  <CellMark cell={row.hot} />
                </TableCell>
                <TableCell>
                  <CellMark cell={row.graphqlNet} />
                </TableCell>
                <TableCell>
                  <CellMark cell={row.apolloServer} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-3xl text-center text-sm">
        On the client side, Strawberry Shake gives .NET callers a typed client
        with MSBuild code generation, alongside any spec-compliant client
        (Relay, urql, Apollo Client) that talks GraphQL over HTTP.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Real-feel C# code example with token coloring
// ---------------------------------------------------------------------------

// Color tokens. Kept inline so the snippet ships as static HTML with no client JS.
const TOK = {
  kw: "text-[#7c92c6]", // keyword (violet)
  type: "text-[#16b9e4]", // type / class (cyan)
  attr: "text-cc-accent", // [QueryType], [DataLoader]
  str: "text-[#f0786a]", // string (coral)
  com: "text-cc-ink-dim", // comment
  ident: "text-cc-heading",
  punct: "text-cc-ink",
};

function CodeExample() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The shape of code"
        title="Your C# is the schema"
        lead="Annotate partials with [QueryType] and [DataLoader]. The Roslyn generator wires the schema, the resolver pipeline, and the per-request batching for you."
      />
      <div className="mx-auto mt-10 max-w-3xl">
        <div className="border-cc-card-border bg-cc-surface/80 overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-sm">
          <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
            <div className="flex items-center gap-2">
              <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
            </div>
            <span className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
              ProductQueries.cs
            </span>
          </div>
          <pre className="overflow-x-auto p-5 font-mono text-[13px] leading-relaxed">
            <code>
              <span className={TOK.com}>
                {"// Implementation-first. The schema is generated from this."}
              </span>
              {"\n"}
              <span className={TOK.kw}>using </span>
              <span className={TOK.type}>HotChocolate.Types</span>
              <span className={TOK.punct}>;</span>
              {"\n"}
              <span className={TOK.kw}>using </span>
              <span className={TOK.type}>GreenDonut</span>
              <span className={TOK.punct}>;</span>
              {"\n\n"}
              <span className={TOK.attr}>{"[QueryType]"}</span>
              {"\n"}
              <span className={TOK.kw}>public partial class </span>
              <span className={TOK.type}>ProductQueries</span>
              {"\n"}
              <span className={TOK.punct}>{"{"}</span>
              {"\n  "}
              <span className={TOK.com}>
                {"// Plain method. Services are injected; no [Service] needed."}
              </span>
              {"\n  "}
              <span className={TOK.kw}>public static async </span>
              <span className={TOK.type}>Task</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>Product</span>
              <span className={TOK.punct}>?{">"} </span>
              <span className={TOK.ident}>GetProductAsync</span>
              <span className={TOK.punct}>{"("}</span>
              {"\n      "}
              <span className={TOK.type}>int</span>{" "}
              <span className={TOK.ident}>id</span>
              <span className={TOK.punct}>{","}</span>
              {"\n      "}
              <span className={TOK.type}>ProductByIdDataLoader</span>{" "}
              <span className={TOK.ident}>products</span>
              <span className={TOK.punct}>{","}</span>
              {"\n      "}
              <span className={TOK.type}>CancellationToken</span>{" "}
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{") =>"}</span>
              {"\n      "}
              <span className={TOK.kw}>await </span>
              <span className={TOK.ident}>products</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>LoadAsync</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>id</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n"}
              <span className={TOK.punct}>{"}"}</span>
              {"\n\n"}
              <span className={TOK.kw}>internal static class </span>
              <span className={TOK.type}>ProductDataLoaders</span>
              {"\n"}
              <span className={TOK.punct}>{"{"}</span>
              {"\n  "}
              <span className={TOK.attr}>{"[DataLoader]"}</span>
              {"\n  "}
              <span className={TOK.kw}>public static async </span>
              <span className={TOK.type}>Task</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>IReadOnlyDictionary</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>int</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.type}>Product</span>
              <span className={TOK.punct}>{">> "}</span>
              {"\n      "}
              <span className={TOK.ident}>GetProductByIdAsync</span>
              <span className={TOK.punct}>{"("}</span>
              {"\n          "}
              <span className={TOK.type}>IReadOnlyList</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>int</span>
              <span className={TOK.punct}>{">"} </span>
              <span className={TOK.ident}>ids</span>
              <span className={TOK.punct}>{","}</span>
              {"\n          "}
              <span className={TOK.type}>CatalogDbContext</span>{" "}
              <span className={TOK.ident}>db</span>
              <span className={TOK.punct}>{","}</span>
              {"\n          "}
              <span className={TOK.type}>CancellationToken</span>{" "}
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{") =>"}</span>
              {"\n      "}
              <span className={TOK.kw}>await </span>
              <span className={TOK.ident}>db</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Products</span>
              {"\n          "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Where</span>
              <span className={TOK.punct}>{"(p => "}</span>
              <span className={TOK.ident}>ids</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Contains</span>
              <span className={TOK.punct}>{"(p.Id))"}</span>
              {"\n          "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>ToDictionaryAsync</span>
              <span className={TOK.punct}>{"(p => p.Id, "}</span>
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n"}
              <span className={TOK.punct}>{"}"}</span>
            </code>
          </pre>
        </div>
        <p className="text-cc-ink-dim mt-4 text-center text-sm">
          The generator emits <code className="text-cc-ink">Query</code>,{" "}
          <code className="text-cc-ink">ProductByIdDataLoader</code>, and the DI
          registrations. You write the domain; the platform writes the wiring.
        </p>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Nitro embed (single product card on this page)
// ---------------------------------------------------------------------------

function NitroEmbed() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Runs against your server"
        title="The GraphQL IDE ships with the endpoint"
        lead="Open the running Hot Chocolate server in your browser and the embedded Nitro IDE is right there: schema browser, SDL, run pane. Same UX shown below, talking to your local server."
      />
      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
        <NitroCompose />
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Compact feature catalogue (the 6 existing bullets, denser layout)
// ---------------------------------------------------------------------------

interface FeatureProps {
  readonly title: string;
  readonly body: string;
  readonly icon: ReactNode;
}

function FeatureIcon({ children }: { children: ReactNode }) {
  return (
    <span
      aria-hidden
      className="border-cc-card-border bg-cc-bg/60 text-cc-accent inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border"
    >
      {children}
    </span>
  );
}

function IconCompose() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <rect x="3" y="3" width="7" height="7" rx="1.5" />
      <rect x="14" y="3" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <path d="M14 17.5h7" />
      <path d="M17.5 14v7" />
    </svg>
  );
}

function IconCode() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <path d="M8 6 2.5 12 8 18" />
      <path d="M16 6 21.5 12 16 18" />
    </svg>
  );
}

function IconBatch() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="6" cy="6" r="2" />
      <circle cx="6" cy="18" r="2" />
      <circle cx="18" cy="12" r="2" />
      <path d="M8 6h2a4 4 0 0 1 4 4v0a2 2 0 0 0 2 2" />
      <path d="M8 18h2a4 4 0 0 0 4-4v0a2 2 0 0 1 2-2" />
    </svg>
  );
}

function IconLive() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="12" cy="12" r="2" />
      <path d="M7.5 7.5a6 6 0 0 0 0 9" />
      <path d="M16.5 7.5a6 6 0 0 1 0 9" />
      <path d="M4.5 4.5a10 10 0 0 0 0 15" />
      <path d="M19.5 4.5a10 10 0 0 1 0 15" />
    </svg>
  );
}

function IconTelemetry() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <path d="M3 17l5-6 4 4 5-7 4 5" />
      <circle cx="8" cy="11" r="1.2" />
      <circle cx="12" cy="15" r="1.2" />
      <circle cx="17" cy="8" r="1.2" />
    </svg>
  );
}

function IconFederation() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="12" cy="12" r="3" />
      <circle cx="4" cy="6" r="1.6" />
      <circle cx="20" cy="6" r="1.6" />
      <circle cx="4" cy="18" r="1.6" />
      <circle cx="20" cy="18" r="1.6" />
      <path d="M9.5 10.5 5.2 7.2" />
      <path d="M14.5 10.5 18.8 7.2" />
      <path d="M9.5 13.5 5.2 16.8" />
      <path d="M14.5 13.5 18.8 16.8" />
    </svg>
  );
}

const FEATURES: readonly FeatureProps[] = [
  {
    title: "Compile-time composition",
    body: "Fusion plans subgraph composition at build time, not at runtime. The gateway stays fast and queries stay typed end to end.",
    icon: <IconCompose />,
  },
  {
    title: "Code-first or schema-first",
    body: "Author your GraphQL schema the way your team prefers. Hot Chocolate supports both styles, with full type safety, and lets you mix them.",
    icon: <IconCode />,
  },
  {
    title: "DataLoader batching",
    body: "Green Donut batches loads, dedupes keys, and caches per request. Cross-service N+1 disappears, including across Fusion subgraphs.",
    icon: <IconBatch />,
  },
  {
    title: "Realtime subscriptions",
    body: "Server-Sent Events and WebSocket subscriptions are first class. Pub/sub providers cover Redis, NATS, Postgres LISTEN/NOTIFY, and RabbitMQ.",
    icon: <IconLive />,
  },
  {
    title: "OpenTelemetry built in",
    body: "Native OTel instrumentation aligned with the proposed GraphQL semantic conventions. Configure your exporter for Jaeger, Tempo, Datadog, or Honeycomb.",
    icon: <IconTelemetry />,
  },
  {
    title: "Federation-ready",
    body: "Compose with other Hot Chocolate services via Fusion, or interop with Apollo subgraphs via the Apollo Federation spec.",
    icon: <IconFederation />,
  },
];

function CatalogueSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The catalogue"
        title="Everything you already expect from a serious GraphQL server"
        lead="The shortlist below ships in the box. Cost analysis, trusted documents, file uploads, projections, cursor pagination, and Azure Functions hosting come with it."
      />
      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {FEATURES.map((f) => (
          <div
            key={f.title}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full gap-4 rounded-xl border p-5 backdrop-blur-sm transition-colors"
          >
            <FeatureIcon>{f.icon}</FeatureIcon>
            <div>
              <h3 className="text-cc-heading font-heading text-base tracking-tight">
                {f.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
                {f.body}
              </p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// MIT band
// ---------------------------------------------------------------------------

function MitBand() {
  return (
    <section className="py-12 sm:py-16">
      <div className="border-cc-card-border bg-cc-surface/70 relative overflow-hidden rounded-2xl border p-8 sm:p-10">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px opacity-70"
          style={{ background: SPECTRUM }}
        />
        <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="max-w-2xl">
            <Eyebrow>Open source</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              MIT licensed. Use it anywhere.
            </h2>
            <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">
              Hot Chocolate is released under the MIT license. Drop it into a
              commercial product, an internal API, or a side project. Read the
              source, file an issue, send a PR.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE">
              Read the license
            </OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA
// ---------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="pt-12 pb-20 text-center sm:pt-16">
      <Eyebrow>Five minutes from zero</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-4 text-3xl tracking-tight sm:text-4xl">
        Build the .NET-native GraphQL server you actually want.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-5 max-w-2xl">
        Install the templates, scaffold a project, and open the embedded IDE
        against your own schema. Add Fusion when one service stops being enough.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-6 font-mono text-xs tracking-widest uppercase">
        dotnet new install HotChocolate.Templates
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function HotChocolateWhyDotNetPage() {
  return (
    <>
      <Hero />
      <PillarsSection />
      <ComparisonSection />
      <CodeExample />
      <NitroEmbed />
      <CatalogueSection />
      <MitBand />
      <ClosingCta />
    </>
  );
}
