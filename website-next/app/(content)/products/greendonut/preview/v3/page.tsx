import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GreenDonut as GreenDonutIcon } from "@/src/icons/GreenDonut";

export const metadata: Metadata = {
  title: "Green Donut: The DataLoader for .NET behind Hot Chocolate",
  description:
    "Green Donut is the high-performance DataLoader for .NET. Source-generated, AOT-friendly batching, caching, and dedup. Powers Hot Chocolate, runs anywhere.",
  keywords: [
    "Green Donut",
    "DataLoader",
    ".NET DataLoader",
    "GraphQL N+1",
    "batching",
    "deduplication",
    "AOT",
    "Hot Chocolate",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Green Donut: The DataLoader for .NET behind Hot Chocolate",
    description:
      "The high-performance .NET DataLoader behind Hot Chocolate. Source-generated [DataLoader] methods, batching, caching, deduplication. Use it in any .NET service.",
    type: "website",
  },
};

// Brand spectrum gradient. Used exactly once on this screen, on the hero wedge.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// ---------------------------------------------------------------------------
// Small shared pieces
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
// Hero
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="mx-auto grid max-w-6xl items-center gap-10 lg:grid-cols-[1.4fr_1fr]">
        <div>
          <Eyebrow>The DataLoader for .NET</Eyebrow>
          <h1 className="text-cc-heading font-heading text-hero mt-5 tracking-tight">
            Powering{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              Hot Chocolate
            </span>
            . Use it anywhere.
          </h1>
          <p className="text-cc-ink-dim lead mt-6 max-w-2xl">
            Green Donut is the open-source DataLoader for .NET. It collapses
            many key requests in a single tick into one batched fetch, dedupes
            keys, and caches per request. Hot Chocolate uses it under the hood
            to eliminate N+1. Any .NET service can use it the same way.
          </p>
          <div className="mt-9 flex flex-wrap gap-4">
            <SolidButton href="/docs/greendonut">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
            <span>MIT licensed</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>.NET 8+</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>AOT-friendly</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>Auto-discovered by Hot Chocolate</span>
          </div>
        </div>
        <div className="relative mx-auto hidden h-72 w-72 lg:block">
          <div
            aria-hidden
            className="absolute inset-0 rounded-full opacity-30 blur-3xl"
            style={{
              background:
                "radial-gradient(circle, rgba(94,234,212,0.35), transparent 70%)",
            }}
          />
          <GreenDonutIcon className="relative h-full w-full" />
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Two-panel positioning: Inside Hot Chocolate / Standalone
// ---------------------------------------------------------------------------

interface PanelProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly intro: string;
  readonly bullets: readonly string[];
  readonly footnote: string;
  readonly accent: "teal" | "spectrum";
}

function PositioningPanel({
  eyebrow,
  title,
  intro,
  bullets,
  footnote,
  accent,
}: PanelProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col overflow-hidden rounded-2xl border p-7 backdrop-blur-sm transition-colors">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px opacity-70"
        style={
          accent === "spectrum"
            ? { background: SPECTRUM }
            : { background: "var(--color-cc-accent)" }
        }
      />
      <Eyebrow>{eyebrow}</Eyebrow>
      <h3 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">{intro}</p>
      <ul className="mt-6 space-y-2.5">
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
      <p className="text-cc-ink-dim mt-6 border-t border-[var(--color-cc-card-border)] pt-4 font-mono text-xs tracking-wider uppercase">
        {footnote}
      </p>
    </article>
  );
}

function PositioningSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="One library, two homes"
        title="Built into Hot Chocolate. Useful far beyond it."
        lead="Green Donut started as the batching layer inside Hot Chocolate. The same library is published standalone for any .NET service that needs DataLoader semantics."
      />
      <div className="mt-12 grid gap-5 md:grid-cols-2">
        <PositioningPanel
          accent="spectrum"
          eyebrow="Used inside Hot Chocolate to"
          title="Disappear N+1 across the graph"
          intro="Hot Chocolate auto-discovers loaders defined with the [DataLoader] attribute and wires them into the resolver pipeline. Per-request scoping is automatic, and Fusion lets that batching cross service boundaries."
          bullets={[
            "Auto-discovers [DataLoader] methods in your assembly",
            "Per-request cache and lifetime are owned by the server",
            "Federated, cross-subgraph batching when used with Fusion",
            "Instrumentation flows into the same OpenTelemetry pipeline",
          ]}
          footnote="HotChocolate.Types.GreenDonut"
        />
        <PositioningPanel
          accent="teal"
          eyebrow="Use it standalone"
          title="In any .NET service, GraphQL or not"
          intro="Green Donut is just a NuGet package. Drop it into a Minimal API, a gRPC service, a background worker, or a console app. You get the same batching, caching, and dedup, with no GraphQL dependency."
          bullets={[
            "No GraphQL, no Hot Chocolate, no ASP.NET Core required",
            "AOT-friendly, suitable for trimmed and native-AOT builds",
            "Plug in your own cache, scheduler, or instrumentation",
            "Works with HTTP clients, EF Core, Dapper, gRPC, you name it",
          ]}
          footnote="dotnet add package GreenDonut"
        />
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Soft comparison band: factual deltas vs Facebook/Node DataLoader and
// graphql-dotnet's loader.
// ---------------------------------------------------------------------------

interface DeltaCardProps {
  readonly name: string;
  readonly runtime: string;
  readonly authoring: string;
  readonly aot: string;
  readonly position: string;
}

function DeltaCard({
  name,
  runtime,
  authoring,
  aot,
  position,
}: DeltaCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-6 backdrop-blur-sm">
      <div className="text-cc-heading font-heading text-lg tracking-tight">
        {name}
      </div>
      <dl className="mt-5 space-y-3 text-sm">
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-[0.25em] uppercase">
            Runtime
          </dt>
          <dd className="text-cc-ink mt-1">{runtime}</dd>
        </div>
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-[0.25em] uppercase">
            Authoring
          </dt>
          <dd className="text-cc-ink mt-1">{authoring}</dd>
        </div>
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-[0.25em] uppercase">
            AOT and trimming
          </dt>
          <dd className="text-cc-ink mt-1">{aot}</dd>
        </div>
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-[0.25em] uppercase">
            Position
          </dt>
          <dd className="text-cc-ink-dim mt-1 text-xs leading-relaxed">
            {position}
          </dd>
        </div>
      </dl>
    </div>
  );
}

function ComparisonSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Soft comparison"
        title="Where Green Donut sits in the DataLoader family"
        lead="The DataLoader pattern came from Facebook's Node.js library. Several .NET ports exist. Here are the factual deltas, no takedowns."
      />
      <div className="mt-12 grid gap-5 md:grid-cols-3">
        <DeltaCard
          name="Green Donut"
          runtime=".NET 8+ (also .NET Standard targets in older versions)"
          authoring="[DataLoader] attribute on a static method; wiring is source-generated. Keyed, grouped, and pagination loaders out of the box."
          aot="AOT-friendly; no reflection on the hot path; works with trimmed and native-AOT builds."
          position=".NET-first, attribute-driven, auto-discovered by Hot Chocolate, and reusable in any .NET service."
        />
        <DeltaCard
          name="Facebook DataLoader (Node)"
          runtime="JavaScript and TypeScript on Node.js."
          authoring="Construct a new DataLoader instance and pass a batch function. Wiring is done by hand per request."
          aot="N/A. Targets the Node.js runtime."
          position="The original reference implementation. Defines the pattern; not a .NET option."
        />
        <DeltaCard
          name="graphql-dotnet DataLoader"
          runtime=".NET, coupled to the graphql-dotnet server."
          authoring="Imperative API; loaders are registered manually against the DataLoader context. No source-generation."
          aot="Targets the classic .NET runtime model; no first-class AOT or trimming story documented for the DataLoader."
          position="A DataLoader implementation bundled with the graphql-dotnet server, used inside that server."
        />
      </div>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-3xl text-center text-sm">
        Same pattern, three implementations. Green Donut is the one written for
        modern .NET first, with the attribute model and AOT story to match.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// N+1 explainer: before / after
// ---------------------------------------------------------------------------

interface FlowProps {
  readonly label: string;
  readonly tone: "danger" | "success";
  readonly title: string;
  readonly description: string;
  readonly rows: readonly { readonly tick: string; readonly call: string }[];
}

function NPlusOneCard({ label, tone, title, description, rows }: FlowProps) {
  const accentClass = tone === "danger" ? "text-cc-danger" : "text-cc-success";
  const dotClass = tone === "danger" ? "bg-cc-danger/80" : "bg-cc-success/80";
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-2xl border p-6 backdrop-blur-sm">
      <div className="flex items-center gap-3">
        <span className={`h-2.5 w-2.5 rounded-full ${dotClass}`} aria-hidden />
        <span
          className={`${accentClass} font-mono text-xs tracking-[0.25em] uppercase`}
        >
          {label}
        </span>
      </div>
      <h3 className="text-cc-heading font-heading mt-3 text-xl tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
        {description}
      </p>
      <ol className="mt-5 space-y-1.5 font-mono text-xs">
        {rows.map((r) => (
          <li
            key={r.tick + r.call}
            className="text-cc-ink-dim flex gap-3 leading-relaxed"
          >
            <span className="text-cc-nav-label w-12 shrink-0 tracking-wider">
              {r.tick}
            </span>
            <span className="text-cc-ink">{r.call}</span>
          </li>
        ))}
      </ol>
    </div>
  );
}

function NPlusOneSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The problem"
        title="The N+1 pattern, before and after"
        lead="A single resolver tick can trigger one database call per key. Green Donut collapses those keys into a single batched fetch and dedupes repeats."
      />
      <div className="mx-auto mt-10 grid max-w-5xl gap-5 md:grid-cols-2">
        <NPlusOneCard
          label="Without DataLoader"
          tone="danger"
          title="One round-trip per key"
          description="Resolving 50 users in one query fires 50 separate SELECT statements. The hot path is dominated by latency, not work."
          rows={[
            { tick: "t0", call: "SELECT * FROM users WHERE id = 1" },
            { tick: "t0", call: "SELECT * FROM users WHERE id = 2" },
            { tick: "t0", call: "SELECT * FROM users WHERE id = 1" },
            { tick: "t0", call: "SELECT * FROM users WHERE id = 3" },
            { tick: "...", call: "(46 more, including duplicates)" },
          ]}
        />
        <NPlusOneCard
          label="With Green Donut"
          tone="success"
          title="One batched fetch per tick"
          description="Keys requested in the same tick are coalesced. Duplicates collapse via dedup. The per-request cache short-circuits repeats inside the same request."
          rows={[
            { tick: "t0", call: "loader.LoadAsync(1, 2, 3, ...)" },
            { tick: "t0", call: "dedup -> { 1, 2, 3, ... 50 }" },
            {
              tick: "t0",
              call: "SELECT * FROM users WHERE id IN (1, 2, 3, ...)",
            },
            { tick: "t1", call: "cache hit on repeated keys" },
          ]}
        />
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Capabilities grid (batching/caching/dedup + loader flavors)
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

function IconCache() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <ellipse cx="12" cy="6" rx="7" ry="2.5" />
      <path d="M5 6v6c0 1.4 3.1 2.5 7 2.5s7-1.1 7-2.5V6" />
      <path d="M5 12v6c0 1.4 3.1 2.5 7 2.5s7-1.1 7-2.5v-6" />
    </svg>
  );
}

function IconDedup() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="9" cy="9" r="4.5" />
      <circle cx="15" cy="15" r="4.5" />
    </svg>
  );
}

function IconKeyed() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="8" cy="14" r="3.5" />
      <path d="M11.5 12.5 20 4" />
      <path d="M17 7l2.5 2.5" />
      <path d="M14.5 9.5 17 12" />
    </svg>
  );
}

function IconGrouped() {
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
      <circle cx="12" cy="6" r="2" />
      <circle cx="18" cy="6" r="2" />
      <path d="M6 8v3a3 3 0 0 0 3 3h6a3 3 0 0 0 3-3V8" />
      <circle cx="12" cy="18" r="2" />
    </svg>
  );
}

function IconPaging() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <rect x="3" y="4" width="14" height="16" rx="1.5" />
      <path d="M7 4v16" />
      <path d="M20 8v12" />
    </svg>
  );
}

const FEATURES: readonly FeatureProps[] = [
  {
    title: "Batching",
    body: "Keys requested in the same tick are collected, then handed to your batch function as one list. One round-trip instead of N.",
    icon: <IconBatch />,
  },
  {
    title: "Per-request cache",
    body: "A request-scoped cache short-circuits repeats inside the same request. Pluggable: swap in your own cache when you need different semantics.",
    icon: <IconCache />,
  },
  {
    title: "Deduplication",
    body: "If the same key is asked for twice in the same tick, the batch function still only sees it once. No extra work, same answer.",
    icon: <IconDedup />,
  },
  {
    title: "Keyed loads",
    body: "The bread-and-butter pattern: load entities by primary key. Returns a dictionary your method then projects back to per-key answers.",
    icon: <IconKeyed />,
  },
  {
    title: "Grouped loads",
    body: "One-to-many lookups. Load all posts for a list of author ids, get back a lookup grouped by key. No manual stitching.",
    icon: <IconGrouped />,
  },
  {
    title: "Pagination loads",
    body: "GreenDonut.Data provides cursor and offset paging primitives, with EF Core integration that pushes selection and paging into SQL.",
    icon: <IconPaging />,
  },
];

function CapabilitiesSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="What you get"
        title="Batching, caching, dedup, and the loader flavors you actually need"
        lead="The three core DataLoader behaviors come baked in. On top of that, Green Donut ships the three loader shapes most resolvers reach for."
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
// [DataLoader] code snippet
// ---------------------------------------------------------------------------

// Color tokens. Kept inline so the snippet ships as static HTML with no client JS.
const TOK = {
  kw: "text-[#7c92c6]",
  type: "text-[#16b9e4]",
  attr: "text-cc-accent",
  str: "text-[#f0786a]",
  com: "text-cc-ink-dim",
  ident: "text-cc-heading",
  punct: "text-cc-ink",
};

function CodeExample() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The shape of code"
        title="Write a method. Get a DataLoader."
        lead="Annotate a static method with [DataLoader]. The source generator emits the loader class, the DI registration, and the wiring. Hot Chocolate auto-discovers it; standalone apps resolve it from DI."
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
              UserDataLoaders.cs
            </span>
          </div>
          <pre className="overflow-x-auto p-5 font-mono text-[13px] leading-relaxed">
            <code>
              <span className={TOK.kw}>using </span>
              <span className={TOK.type}>GreenDonut</span>
              <span className={TOK.punct}>;</span>
              {"\n\n"}
              <span className={TOK.kw}>internal static class </span>
              <span className={TOK.type}>UserDataLoaders</span>
              {"\n"}
              <span className={TOK.punct}>{"{"}</span>
              {"\n  "}
              <span className={TOK.com}>
                {
                  "// One method. The generator emits UserByIdDataLoader and DI."
                }
              </span>
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
              <span className={TOK.type}>User</span>
              <span className={TOK.punct}>{">> "}</span>
              {"\n      "}
              <span className={TOK.ident}>GetUserByIdAsync</span>
              <span className={TOK.punct}>{"("}</span>
              {"\n          "}
              <span className={TOK.type}>IReadOnlyList</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>int</span>
              <span className={TOK.punct}>{">"} </span>
              <span className={TOK.ident}>ids</span>
              <span className={TOK.punct}>{","}</span>
              {"\n          "}
              <span className={TOK.type}>AppDbContext</span>{" "}
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
              <span className={TOK.ident}>Users</span>
              {"\n          "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Where</span>
              <span className={TOK.punct}>{"(u => "}</span>
              <span className={TOK.ident}>ids</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Contains</span>
              <span className={TOK.punct}>{"(u.Id))"}</span>
              {"\n          "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>ToDictionaryAsync</span>
              <span className={TOK.punct}>{"(u => u.Id, "}</span>
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n\n  "}
              <span className={TOK.com}>
                {"// One-to-many lookup, same idea, grouped result."}
              </span>
              {"\n  "}
              <span className={TOK.attr}>{"[DataLoader]"}</span>
              {"\n  "}
              <span className={TOK.kw}>public static async </span>
              <span className={TOK.type}>Task</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>ILookup</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>int</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.type}>Post</span>
              <span className={TOK.punct}>{">> "}</span>
              {"\n      "}
              <span className={TOK.ident}>GetPostsByAuthorAsync</span>
              <span className={TOK.punct}>{"("}</span>
              {"\n          "}
              <span className={TOK.type}>IReadOnlyList</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>int</span>
              <span className={TOK.punct}>{">"} </span>
              <span className={TOK.ident}>authorIds</span>
              <span className={TOK.punct}>{","}</span>
              {"\n          "}
              <span className={TOK.type}>AppDbContext</span>{" "}
              <span className={TOK.ident}>db</span>
              <span className={TOK.punct}>{","}</span>
              {"\n          "}
              <span className={TOK.type}>CancellationToken</span>{" "}
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{") =>"}</span>
              {"\n      "}
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.kw}>await </span>
              <span className={TOK.ident}>db</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Posts</span>
              {"\n          "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Where</span>
              <span className={TOK.punct}>{"(p => "}</span>
              <span className={TOK.ident}>authorIds</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Contains</span>
              <span className={TOK.punct}>{"(p.AuthorId))"}</span>
              {"\n          "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>ToListAsync</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{"))"}</span>
              {"\n          "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>ToLookup</span>
              <span className={TOK.punct}>{"(p => p.AuthorId);"}</span>
              {"\n"}
              <span className={TOK.punct}>{"}"}</span>
            </code>
          </pre>
        </div>
        <p className="text-cc-ink-dim mt-4 text-center text-sm">
          The first method becomes{" "}
          <code className="text-cc-ink">UserByIdDataLoader</code>; the second
          becomes <code className="text-cc-ink">PostsByAuthorDataLoader</code>.
          Inject them by type into your resolver, controller, or service.
        </p>
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
        <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="max-w-2xl">
            <Eyebrow>Open source</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              MIT licensed. Same engine we ship with Hot Chocolate.
            </h2>
            <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">
              Green Donut is released under the MIT license and developed in the
              open inside the ChilliCream GraphQL platform repository. Read the
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
      <Eyebrow>One package, no N+1</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-4 text-3xl tracking-tight sm:text-4xl">
        Add Green Donut. Stop paying for round trips.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-5 max-w-2xl">
        If your .NET service has an N+1 problem, you already know the fix. Green
        Donut gives you the fix that powers Hot Chocolate, usable anywhere.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/greendonut">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-6 font-mono text-xs tracking-widest uppercase">
        dotnet add package GreenDonut
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function GreenDonutInsideHotChocolatePage() {
  return (
    <>
      <Hero />
      <PositioningSection />
      <ComparisonSection />
      <NPlusOneSection />
      <CapabilitiesSection />
      <CodeExample />
      <MitBand />
      <ClosingCta />
    </>
  );
}
