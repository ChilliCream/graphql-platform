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
import { StrawberryShake } from "@/src/icons/StrawberryShake";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Strawberry Shake: The GraphQL client for .NET UI",
  description:
    "Strawberry Shake is the strongly-typed GraphQL client for Blazor, MAUI, and .NET UI: typed C# from your operations, reactive store, MSBuild build-time codegen.",
  keywords: [
    "Strawberry Shake",
    ".NET GraphQL client",
    "Blazor GraphQL client",
    "MAUI GraphQL client",
    "Razor GraphQL",
    "MSBuild code generation",
    "dotnet graphql",
    "reactive store",
    "GraphQL subscriptions",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Strawberry Shake: The GraphQL client for .NET UI",
    description:
      "Why .NET UI teams choose Strawberry Shake: typed C# from your .graphql operations, normalized reactive store, real-time subscriptions, MSBuild codegen at build time.",
    type: "website",
  },
};

// Brand spectrum gradient. Used exactly once on this screen, on the wedge phrase.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// ---------------------------------------------------------------------------
// Small inline helpers
// ---------------------------------------------------------------------------

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
// Hero (wedge headline: "for Blazor, MAUI, and .NET UI")
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="mx-auto grid max-w-6xl items-center gap-10 px-2 sm:gap-12 lg:grid-cols-[1fr_auto]">
        <div className="text-center lg:text-left">
          <Eyebrow>GraphQL Client for .NET UI</Eyebrow>
          <h1 className="text-cc-heading font-heading text-hero mt-5 tracking-tight">
            The GraphQL client for{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              Blazor, MAUI, and .NET UI
            </span>
            .
          </h1>
          <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl lg:mx-0">
            Strawberry Shake is the open-source, strongly-typed GraphQL client
            for .NET. The <code className="text-cc-ink">dotnet graphql</code>{" "}
            CLI generates typed C# from your{" "}
            <code className="text-cc-ink">.graphql</code> files at build time,
            and a normalized reactive store keeps your UI in sync with queries,
            mutations, and live subscriptions.
          </p>
          <div className="mt-9 flex flex-wrap justify-center gap-4 lg:justify-start">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase lg:justify-start">
            <span>MIT licensed</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>Blazor &amp; MAUI</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>Spec-compliant</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>MSBuild codegen</span>
          </div>
        </div>
        <div className="hidden lg:block">
          <StrawberryShake
            className="h-64 w-auto opacity-90"
            style={{ filter: "drop-shadow(0 12px 40px rgba(0,0,0,0.5))" }}
          />
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// "What sets it apart" 4 wedge cards
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
    title: "Typed C# from your operations",
    body: "Write GraphQL queries, mutations, and fragments in .graphql files alongside your code. Strawberry Shake generates a typed client, request types, result records, and fragment models so calls into the graph look like ordinary async C# methods.",
    bullets: [
      "Fragments become reusable result interfaces",
      "Enums and input types come through as real C# types",
      "Refactor-safe across schema changes",
    ],
  },
  {
    index: "02",
    title: "Normalized reactive store",
    body: "Results are normalized into a typed entity store. Updates from any query, mutation, or subscription flow through the same store, so every Blazor or Razor component that watches an entity stays in sync without bespoke wiring.",
    bullets: [
      "Three fetch strategies: cache-first, network-only, cache-and-network",
      "Persisted state via SQLite, LiteDB, or IndexedDB",
      "Operation store de-dupes in-flight requests",
    ],
  },
  {
    index: "03",
    title: "Real-time subscriptions",
    body: "GraphQL subscriptions over WebSockets are first class. The same Watch() API drives live queries; auth headers travel through the connection_init payload so token-based auth just works.",
    bullets: [
      "WebSocket transport via StrawberryShake.Transport.WebSockets",
      "Auth via connection_init payload",
      "Watch() returns the same reactive sequence as queries",
    ],
  },
  {
    index: "04",
    title: "MSBuild codegen at build time",
    body: "The dotnet graphql CLI and the Strawberry Shake MSBuild target generate the typed client during the build, not at runtime. No reflection-heavy startup, no IL weaving, no surprise behaviour the first time a query runs in production.",
    bullets: [
      "dotnet graphql init scaffolds .graphqlrc.json and pulls the schema",
      "Codegen runs as a build step, output is just C# you can read",
      "No runtime code generation at request time",
    ],
  },
];

function PillarsSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="What sets it apart"
        title="Built for the .NET UI loop, not retro-fitted to it"
        lead="Four things that decide whether a GraphQL client actually fits a Blazor, MAUI, or WPF app. Strawberry Shake is designed around all four."
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
// Soft, factual comparison band (no disparagement, JS clients vs .NET client)
// ---------------------------------------------------------------------------

type Cell =
  | { kind: "yes"; note?: string }
  | { kind: "no"; note?: string }
  | { kind: "partial"; note?: string }
  | { kind: "text"; note: string };

interface ComparisonRow {
  readonly capability: string;
  readonly ss: Cell;
  readonly apollo: Cell;
  readonly relay: Cell;
  readonly urql: Cell;
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
        <span>{cell.note ?? "Not applicable"}</span>
      </span>
    );
  }
  return <span className="text-cc-ink text-sm">{cell.note}</span>;
}

const ROWS: readonly ComparisonRow[] = [
  {
    capability: "Primary target UI",
    ss: {
      kind: "text",
      note: "Blazor, MAUI, WPF, Avalonia, Razor, console .NET apps",
    },
    apollo: { kind: "text", note: "React, Angular, Vue, iOS, Android, Web" },
    relay: { kind: "text", note: "React (web and React Native)" },
    urql: { kind: "text", note: "React, Preact, Vue, Svelte" },
  },
  {
    capability: "Language of the generated client",
    ss: { kind: "text", note: "Strongly-typed C#" },
    apollo: { kind: "text", note: "TypeScript or JavaScript" },
    relay: { kind: "text", note: "TypeScript or Flow" },
    urql: { kind: "text", note: "TypeScript or JavaScript" },
  },
  {
    capability: "Codegen step",
    ss: {
      kind: "text",
      note: "MSBuild codegen via dotnet graphql, runs at build",
    },
    apollo: { kind: "text", note: "GraphQL Code Generator, runs at build" },
    relay: { kind: "text", note: "relay-compiler, runs at build" },
    urql: { kind: "text", note: "GraphQL Code Generator, optional" },
  },
  {
    capability: "Normalized reactive store",
    ss: { kind: "yes", note: "Entity store + operation store" },
    apollo: { kind: "yes", note: "InMemoryCache (normalized)" },
    relay: { kind: "yes", note: "Relay Store (normalized)" },
    urql: {
      kind: "partial",
      note: "Document cache by default, graphcache opt-in",
    },
  },
  {
    capability: "Subscriptions",
    ss: { kind: "yes", note: "WebSocket (subscriptions-transport-ws)" },
    apollo: { kind: "yes", note: "WebSocket (graphql-ws)" },
    relay: { kind: "yes", note: "WebSocket via network layer" },
    urql: { kind: "yes", note: "WebSocket exchange" },
  },
  {
    capability: "Persisted operations",
    ss: { kind: "yes", note: "Built in, pairs with Hot Chocolate server" },
    apollo: { kind: "yes", note: "APQ link" },
    relay: { kind: "yes", note: "Persisted queries via compiler" },
    urql: { kind: "partial", note: "Via separate exchange or plugin" },
  },
  {
    capability: "Offline / persisted state",
    ss: { kind: "yes", note: "SQLite, LiteDB, IndexedDB store" },
    apollo: { kind: "partial", note: "Cache persistors via community libs" },
    relay: { kind: "partial", note: "Custom store implementation" },
    urql: { kind: "partial", note: "Via offline exchange" },
  },
];

function ComparisonSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The factual delta"
        title="JS clients for JS UI. Strawberry Shake for .NET UI."
        lead="Apollo Client, Relay, and urql are excellent GraphQL clients. They target JavaScript UI runtimes. Strawberry Shake targets the .NET UI runtimes (Blazor, MAUI, WPF, Avalonia, Razor). Same protocol, different home."
      />
      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-6xl rounded-2xl border p-2 backdrop-blur-sm sm:p-4">
        <Table alternating className="min-w-[760px]">
          <TableHead>
            <TableRow>
              <TableHeaderCell className="w-[20%]">Capability</TableHeaderCell>
              <TableHeaderCell className="w-[26%]">
                Strawberry Shake
              </TableHeaderCell>
              <TableHeaderCell className="w-[18%]">
                Apollo Client
              </TableHeaderCell>
              <TableHeaderCell className="w-[18%]">Relay</TableHeaderCell>
              <TableHeaderCell className="w-[18%]">urql</TableHeaderCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {ROWS.map((row) => (
              <TableRow key={row.capability}>
                <TableCell className="text-cc-heading font-medium">
                  {row.capability}
                </TableCell>
                <TableCell>
                  <CellMark cell={row.ss} />
                </TableCell>
                <TableCell>
                  <CellMark cell={row.apollo} />
                </TableCell>
                <TableCell>
                  <CellMark cell={row.relay} />
                </TableCell>
                <TableCell>
                  <CellMark cell={row.urql} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-3xl text-center text-sm">
        Strawberry Shake is spec-compliant, so it talks to any GraphQL server
        (Hot Chocolate, Apollo Server, GraphQL Java, Hasura, and the rest). The
        wedge is the UI runtime: typed C# all the way to your XAML, Razor, or
        Blazor component.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Real-feel code example (the .graphql file + the typed Razor call)
// ---------------------------------------------------------------------------

// Color tokens. Kept inline so the snippet ships as static HTML with no client JS.
const TOK = {
  kw: "text-[#7c92c6]", // keyword (violet)
  type: "text-[#16b9e4]", // type / class (cyan)
  attr: "text-cc-accent", // attribute / directive
  str: "text-[#f0786a]", // string (coral)
  com: "text-cc-ink-dim", // comment
  ident: "text-cc-heading",
  punct: "text-cc-ink",
};

interface CodePaneProps {
  readonly filename: string;
  readonly children: ReactNode;
}

function CodePane({ filename, children }: CodePaneProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface/80 overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-sm">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
        <div className="flex items-center gap-2">
          <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
        </div>
        <span className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
          {filename}
        </span>
      </div>
      <pre className="overflow-x-auto p-5 font-mono text-[13px] leading-relaxed">
        <code>{children}</code>
      </pre>
    </div>
  );
}

function CodeExample() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The shape of code"
        title="A .graphql file becomes a typed client call"
        lead="Write the operation once. The CLI generates the request, the result records, and a Watch() that pushes updates into your component."
      />
      <div className="mx-auto mt-10 grid max-w-5xl gap-5 lg:grid-cols-2">
        <CodePane filename="Queries/GetSessions.graphql">
          <span className={TOK.com}>
            {"# Queries live next to your code, in .graphql files."}
          </span>
          {"\n"}
          <span className={TOK.kw}>query </span>
          <span className={TOK.ident}>GetSessions</span>
          <span className={TOK.punct}>{" {"}</span>
          {"\n  "}
          <span className={TOK.ident}>sessions</span>
          <span className={TOK.punct}>{"("}</span>
          <span className={TOK.ident}>first</span>
          <span className={TOK.punct}>{": "}</span>
          <span className={TOK.str}>20</span>
          <span className={TOK.punct}>{") {"}</span>
          {"\n    "}
          <span className={TOK.ident}>nodes</span>
          <span className={TOK.punct}>{" {"}</span>
          {"\n      "}
          <span className={TOK.ident}>id</span>
          {"\n      "}
          <span className={TOK.ident}>title</span>
          {"\n      "}
          <span className={TOK.ident}>startsAt</span>
          {"\n      "}
          <span className={TOK.ident}>speaker</span>
          <span className={TOK.punct}>{" {"}</span>
          {"\n        "}
          <span className={TOK.ident}>name</span>
          {"\n        "}
          <span className={TOK.ident}>avatarUrl</span>
          {"\n      "}
          <span className={TOK.punct}>{"}"}</span>
          {"\n    "}
          <span className={TOK.punct}>{"}"}</span>
          {"\n  "}
          <span className={TOK.punct}>{"}"}</span>
          {"\n"}
          <span className={TOK.punct}>{"}"}</span>
        </CodePane>
        <CodePane filename="Components/Sessions.razor.cs">
          <span className={TOK.com}>
            {"// MSBuild codegen wired the typed client at build time."}
          </span>
          {"\n"}
          <span className={TOK.kw}>using </span>
          <span className={TOK.type}>StrawberryShake</span>
          <span className={TOK.punct}>;</span>
          {"\n\n"}
          <span className={TOK.kw}>public partial class </span>
          <span className={TOK.type}>Sessions</span>{" "}
          <span className={TOK.punct}>{": "}</span>
          <span className={TOK.type}>ComponentBase</span>
          {"\n"}
          <span className={TOK.punct}>{"{"}</span>
          {"\n  "}
          <span className={TOK.punct}>{"["}</span>
          <span className={TOK.attr}>Inject</span>
          <span className={TOK.punct}>{"]"} </span>
          <span className={TOK.kw}>public required </span>
          <span className={TOK.type}>IConferenceClient</span>{" "}
          <span className={TOK.ident}>Client</span>
          <span className={TOK.punct}>{" { get; init; }"}</span>
          {"\n\n  "}
          <span className={TOK.kw}>protected override async </span>
          <span className={TOK.type}>Task</span>{" "}
          <span className={TOK.ident}>OnInitializedAsync</span>
          <span className={TOK.punct}>{"() {"}</span>
          {"\n    "}
          <span className={TOK.com}>
            {"// Watch() returns an IObservable of results."}
          </span>
          {"\n    "}
          <span className={TOK.ident}>Client</span>
          <span className={TOK.punct}>.</span>
          <span className={TOK.ident}>GetSessions</span>
          <span className={TOK.punct}>{"."}</span>
          <span className={TOK.ident}>Watch</span>
          <span className={TOK.punct}>{"("}</span>
          <span className={TOK.type}>ExecutionStrategy</span>
          <span className={TOK.punct}>.</span>
          <span className={TOK.ident}>CacheAndNetwork</span>
          <span className={TOK.punct}>{")"}</span>
          {"\n      "}
          <span className={TOK.punct}>.</span>
          <span className={TOK.ident}>Subscribe</span>
          <span className={TOK.punct}>{"(result => {"}</span>
          {"\n        "}
          <span className={TOK.ident}>_sessions</span>{" "}
          <span className={TOK.punct}>{"= "}</span>
          <span className={TOK.ident}>result</span>
          <span className={TOK.punct}>.</span>
          <span className={TOK.ident}>Data</span>
          <span className={TOK.punct}>{"?."}</span>
          <span className={TOK.ident}>Sessions</span>
          <span className={TOK.punct}>{"?."}</span>
          <span className={TOK.ident}>Nodes</span>
          <span className={TOK.punct}>{";"}</span>
          {"\n        "}
          <span className={TOK.ident}>StateHasChanged</span>
          <span className={TOK.punct}>{"();"}</span>
          {"\n      "}
          <span className={TOK.punct}>{"});"}</span>
          {"\n  "}
          <span className={TOK.punct}>{"}"}</span>
          {"\n"}
          <span className={TOK.punct}>{"}"}</span>
        </CodePane>
      </div>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-3xl text-center text-sm">
        <code className="text-cc-ink">IConferenceClient</code>,{" "}
        <code className="text-cc-ink">GetSessions</code>, and every result
        record are generated by the build. Add a field to the operation, save,
        rebuild, and the C# surface updates with it.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Nitro embed: the GraphQL IDE paired with the server side of the story
// ---------------------------------------------------------------------------

function NitroEmbed() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Paired with the server"
        title="Author the operation, ship the typed client"
        lead="Sketch the query in the IDE, drop it into a .graphql file, let MSBuild generate the client. The same operation hash can be persisted on both ends if you lock down production."
      />
      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
        <NitroCompose />
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Compact feature catalogue (the four required features, denser layout)
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

function IconTyped() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <path d="M4 6h7" />
      <path d="M7.5 6v12" />
      <path d="M13 12h7" />
      <path d="M16.5 12v6" />
    </svg>
  );
}

function IconStore() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="12" cy="6" r="3" />
      <circle cx="6" cy="17" r="2.5" />
      <circle cx="18" cy="17" r="2.5" />
      <path d="M12 9v3" />
      <path d="M12 12 6.8 14.8" />
      <path d="M12 12l5.2 2.8" />
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

function IconBuild() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <path d="M4 7h16" />
      <path d="M6 7v13h12V7" />
      <path d="M9 11h6" />
      <path d="M9 15h4" />
      <path d="M9 3h6v4H9z" />
    </svg>
  );
}

const FEATURES: readonly FeatureProps[] = [
  {
    title: "Strongly-typed client",
    body: "Typed C# request types, result records, and fragment interfaces are generated from your .graphql operations and the server schema. No hand-written DTOs to keep in sync.",
    icon: <IconTyped />,
  },
  {
    title: "Reactive store",
    body: "Normalized entity store keeps every component watching the same data in sync. Three execution strategies (cache-first, network-only, cache-and-network) cover the common UI patterns.",
    icon: <IconStore />,
  },
  {
    title: "Subscriptions",
    body: "GraphQL subscriptions over WebSockets. The same Watch() API handles queries and live updates, with auth carried in the connection_init payload.",
    icon: <IconLive />,
  },
  {
    title: "MSBuild code generation",
    body: "The dotnet graphql CLI and the Strawberry Shake MSBuild target generate the typed client at build time. No runtime reflection, no IL weaving, no codegen surprise at startup.",
    icon: <IconBuild />,
  },
];

function CatalogueSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The shortlist"
        title="What ships in the box"
        lead="Persisted state, persisted operations, the dotnet graphql CLI, Blazor and Razor helpers (UseQuery, UseSubscription, UseFragment) all come with it."
      />
      <div className="mt-12 grid gap-4 sm:grid-cols-2">
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
          className="bg-cc-accent/40 pointer-events-none absolute inset-x-0 top-0 h-px"
        />
        <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="max-w-2xl">
            <Eyebrow>Open source</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              MIT licensed. Use it anywhere.
            </h2>
            <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">
              Strawberry Shake is released under the MIT license. Drop it into a
              commercial Blazor app, an internal MAUI tool, or a weekend
              project. Read the source, file an issue, send a PR.
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
        Give your .NET UI a GraphQL client that feels native.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-5 max-w-2xl">
        Install the tool, point it at your schema, drop the first .graphql file
        in, and rebuild. The typed client is waiting for your Blazor or MAUI
        component on the next compile.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-6 font-mono text-xs tracking-widest uppercase">
        dotnet tool install --global StrawberryShake.Tools
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function StrawberryShakeForDotNetUiPage() {
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
