import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Hot Chocolate: GraphQL Server for .NET",
  description:
    "Hot Chocolate, the open-source GraphQL server for .NET, side-by-side against hand-rolled .NET GraphQL: source generators, DataLoaders, OpenTelemetry, Fusion.",
  keywords: [
    "Hot Chocolate",
    "GraphQL server for .NET",
    "GraphQL server",
    ".NET GraphQL",
    "C# GraphQL",
    "ASP.NET Core",
    "DataLoader",
    "Green Donut",
    "GraphQL subscriptions",
    "OpenTelemetry",
    "Apollo Federation",
    "Fusion",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Hot Chocolate: GraphQL Server for .NET",
    description:
      "The honest diff between hand-rolled .NET GraphQL and Hot Chocolate: source-generated schema, DataLoaders, OpenTelemetry, Fusion-ready, MIT licensed on ASP.NET Core.",
    type: "website",
  },
};

// Brand spectrum, allowed at most once per screen. Used on the closing CTA rule.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const LEFT_LABEL = "Hand-rolled .NET GraphQL";
const RIGHT_LABEL = "Hot Chocolate";

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface IndexTagProps {
  readonly value: string;
}

function IndexTag({ value }: IndexTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex h-6 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
      {value}
    </span>
  );
}

// Cross icon for the "without" column. Uses cc-status-firing token, dimmed.
interface CrossIconProps {
  readonly size?: number;
}

function CrossIcon({ size = 14 }: CrossIconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M4 4 L12 12 M12 4 L4 12"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Sticky column header used at the top of every compare grid. Status chip in
// front of a mono-caps label. Left column carries the cross chip, right column
// carries the check chip.
// -----------------------------------------------------------------------------

type Side = "left" | "right";

interface ColumnHeaderProps {
  readonly side: Side;
  readonly label: string;
}

function ColumnHeader({ side, label }: ColumnHeaderProps) {
  const isRight = side === "right";
  return (
    <div className="flex items-center gap-2.5 px-5 py-3.5">
      <span
        className={[
          "inline-flex h-5 w-5 items-center justify-center rounded-full border",
          isRight
            ? "border-cc-accent/40 text-cc-accent bg-cc-accent/10"
            : "border-cc-status-firing/30 text-cc-status-firing/70 bg-cc-status-firing/5",
        ].join(" ")}
      >
        {isRight ? <CheckIcon size={11} /> : <CrossIcon size={11} />}
      </span>
      <span
        className={[
          "font-mono text-[11px] tracking-[0.18em] uppercase",
          isRight ? "text-cc-heading" : "text-cc-ink-dim",
        ].join(" ")}
      >
        {label}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// CompareRow: one horizontally aligned pair across the vertical rule. Left cell
// gets the cross chip, right cell gets the check chip. The two cells share the
// same row height by being siblings of the same grid row.
// -----------------------------------------------------------------------------

interface CompareRowProps {
  readonly left: ReactNode;
  readonly right: ReactNode;
  readonly mono?: boolean;
  readonly last?: boolean;
}

function CompareRow({
  left,
  right,
  mono = false,
  last = false,
}: CompareRowProps) {
  const cellBase = "flex items-start gap-3 px-5 py-4";
  const border = last ? "" : "border-cc-card-border border-b";
  const textCls = mono
    ? "font-mono text-[12px] leading-snug"
    : "text-body leading-snug";
  return (
    <>
      <div className={[cellBase, border].join(" ")}>
        <span className="text-cc-status-firing/70 mt-0.5 shrink-0" aria-hidden>
          <CrossIcon size={13} />
        </span>
        <span className={`text-cc-ink-dim ${textCls}`}>{left}</span>
      </div>
      <div className={[cellBase, border].join(" ")}>
        <span className="text-cc-accent mt-0.5 shrink-0" aria-hidden>
          <CheckIcon size={13} />
        </span>
        <span className={`text-cc-ink ${textCls}`}>{right}</span>
      </div>
    </>
  );
}

// -----------------------------------------------------------------------------
// CompareGrid: the framed two-column card with the signature vertical rule
// running edge-to-edge down its exact center. Holds a sticky-feeling header
// row and a stack of CompareRow children.
// -----------------------------------------------------------------------------

interface CompareGridProps {
  readonly children: ReactNode;
  readonly topSlot?: ReactNode;
}

function CompareGrid({ children, topSlot }: CompareGridProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border">
      {/* The signature vertical rule, edge to edge, exact center. */}
      <div
        aria-hidden
        className="bg-cc-card-border pointer-events-none absolute inset-y-0 left-1/2 w-px"
      />
      <div className="grid grid-cols-2">
        {/* Header row */}
        <div className="border-cc-card-border bg-cc-surface/60 border-b">
          <ColumnHeader side="left" label={LEFT_LABEL} />
        </div>
        <div className="border-cc-card-border bg-cc-surface/60 border-b">
          <ColumnHeader side="right" label={RIGHT_LABEL} />
        </div>

        {/* Optional top slot for paired mini code blocks. Rendered as a single
            grid row that spans both columns but visually keeps the divider. */}
        {topSlot}

        {children}
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// SectionHeader: the small left-rail block (col-span-3) with IndexTag, eyebrow,
// h2 title, lead copy. The compare grid lives in col-span-9 on the right.
// -----------------------------------------------------------------------------

interface SectionHeaderProps {
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly lead: string;
}

function SectionHeader({ index, eyebrow, title, lead }: SectionHeaderProps) {
  return (
    <div className="lg:col-span-3">
      <div className="flex items-center gap-3">
        <IndexTag value={index} />
        <Eyebrow>{eyebrow}</Eyebrow>
      </div>
      <h2 className="text-cc-heading font-heading text-h2 mt-5 font-semibold tracking-tight text-balance">
        {title}
      </h2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">{lead}</p>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Inline code-pair top slot for sections that lead with a mini code diff.
// Renders two MiniCode blocks in their respective grid cells, separated by the
// signature rule via the parent CompareGrid.
// -----------------------------------------------------------------------------

interface MiniCodeProps {
  readonly side: Side;
  readonly title: string;
  readonly lines: readonly ReactNode[];
}

function MiniCode({ side, title, lines }: MiniCodeProps) {
  const isRight = side === "right";
  return (
    <div className="border-cc-card-border border-b px-5 py-4">
      <div
        className={[
          "mb-2 font-mono text-[10.5px] tracking-[0.18em] uppercase",
          isRight ? "text-cc-accent" : "text-cc-ink-dim",
        ].join(" ")}
      >
        {title}
      </div>
      <pre
        className={[
          "bg-cc-code-bg border-cc-card-border overflow-x-auto rounded-md border px-3 py-3 font-mono text-[11.5px] leading-5 whitespace-pre",
          isRight ? "text-cc-ink" : "text-cc-ink-dim",
        ].join(" ")}
      >
        {lines.map((line, i) => (
          <div key={i}>{line}</div>
        ))}
      </pre>
    </div>
  );
}

// -----------------------------------------------------------------------------
// HERO code diff: a single full-width card with the signature vertical rule
// down the center, hand-rolled SDL + resolver + N+1 loop on the left, partial
// class with [QueryType] + [DataLoader] on the right.
// -----------------------------------------------------------------------------

const TOKEN = {
  kw: "#ff7b72",
  type: "#ffa657",
  str: "#a5d6ff",
  comment: "#8b949e",
  attr: "#d2a8ff",
  fn: "#d2a8ff",
  param: "#79c0ff",
  punct: "#c9d1d9",
};

interface TProps {
  readonly c: string;
  readonly children: ReactNode;
}

function T({ c, children }: TProps) {
  return <span style={{ color: c }}>{children}</span>;
}

function HeroDiffCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border shadow-2xl">
      {/* Window chrome with the two file labels straddling the rule. */}
      <div className="border-cc-card-border bg-cc-surface/70 grid grid-cols-2 border-b">
        <div className="flex items-center gap-2.5 px-5 py-3">
          <span
            className="bg-cc-status-firing/70 inline-flex h-4 w-4 items-center justify-center rounded-full"
            aria-hidden
          >
            <CrossIcon size={10} />
          </span>
          <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
            {LEFT_LABEL}
          </span>
          <span className="text-cc-ink-dim/60 ml-auto font-mono text-[10.5px]">
            Schema.cs + Resolvers.cs
          </span>
        </div>
        <div className="border-cc-card-border flex items-center gap-2.5 border-l px-5 py-3">
          <span
            className="bg-cc-accent/15 text-cc-accent inline-flex h-4 w-4 items-center justify-center rounded-full"
            aria-hidden
          >
            <CheckIcon size={10} />
          </span>
          <span className="text-cc-heading font-mono text-[11px] tracking-[0.18em] uppercase">
            {RIGHT_LABEL}
          </span>
          <span className="text-cc-ink-dim/60 ml-auto font-mono text-[10.5px]">
            Catalog/Query.cs
          </span>
        </div>
      </div>

      {/* The signature center rule. */}
      <div
        aria-hidden
        className="bg-cc-card-border pointer-events-none absolute inset-y-0 left-1/2 w-px"
      />

      <div className="bg-cc-code-bg grid grid-cols-2">
        {/* LEFT: hand-rolled. SDL string + resolver class + N+1 loop. */}
        <pre className="text-cc-ink-dim overflow-x-auto px-5 py-5 font-mono text-[12px] leading-[1.55] whitespace-pre">
          {`// schema.graphql, kept in sync by hand
const string Sdl = @"
  type Query {
    productById(id: ID!): Product
  }
  type Product { id: ID! name: String! }
";

services.AddGraphQL()
  .AddSchema(Schema.For(Sdl, c => c.Types
    .Include(typeof(Resolvers))));

public class Resolvers
{
  public Product? GetProductById(string id, MyDb db)
    => db.Products.FirstOrDefault(p => p.Id == id);
    // one DB roundtrip per id, classic N+1
}

// wiring: register IDataLoader<T> by hand,
// hope per-request scope is correct...
`}
        </pre>

        {/* RIGHT: Hot Chocolate. Source-generated partial + DataLoader. */}
        <pre className="overflow-x-auto px-5 py-5 font-mono text-[12px] leading-[1.55] whitespace-pre">
          <T
            c={TOKEN.comment}
          >{`// C# is the schema. Roslyn emits the rest.\n`}</T>
          <T c={TOKEN.punct}>[</T>
          <T c={TOKEN.attr}>QueryType</T>
          <T c={TOKEN.punct}>]</T>
          {"\n"}
          <T c={TOKEN.kw}>public partial class</T> <T c={TOKEN.type}>Query</T>
          {"\n"}
          <T c={TOKEN.punct}>{`{`}</T>
          {"\n"}
          {`  `}
          <T c={TOKEN.kw}>public static async</T> <T c={TOKEN.type}>Task</T>
          <T c={TOKEN.punct}>{`<`}</T>
          <T c={TOKEN.type}>Product</T>
          <T c={TOKEN.punct}>{`?>`}</T> <T c={TOKEN.fn}>GetProductByIdAsync</T>
          <T c={TOKEN.punct}>(</T>
          {"\n"}
          {`      `}
          <T c={TOKEN.type}>Guid</T> <T c={TOKEN.param}>id</T>
          <T c={TOKEN.punct}>,</T>
          {"\n"}
          {`      `}
          <T c={TOKEN.type}>IProductByIdDataLoader</T>{" "}
          <T c={TOKEN.param}>byId</T>
          <T c={TOKEN.punct}>,</T>
          {"\n"}
          {`      `}
          <T c={TOKEN.type}>CancellationToken</T> <T c={TOKEN.param}>ct</T>
          <T c={TOKEN.punct}>{`) =>`}</T>
          {"\n"}
          {`      `}
          <T c={TOKEN.kw}>await</T> <T c={TOKEN.param}>byId</T>
          <T c={TOKEN.punct}>.</T>
          <T c={TOKEN.fn}>LoadAsync</T>
          <T c={TOKEN.punct}>(</T>
          <T c={TOKEN.param}>id</T>
          <T c={TOKEN.punct}>, </T>
          <T c={TOKEN.param}>ct</T>
          <T c={TOKEN.punct}>);</T>
          {"\n"}
          <T c={TOKEN.punct}>{`}`}</T>
          {"\n"}
          {"\n"}
          <T
            c={TOKEN.comment}
          >{`// Generated batch: collapses N ids into one query.\n`}</T>
          <T c={TOKEN.punct}>[</T>
          <T c={TOKEN.attr}>DataLoader</T>
          <T c={TOKEN.punct}>]</T>
          {"\n"}
          <T c={TOKEN.kw}>internal static async</T> <T c={TOKEN.type}>Task</T>
          <T c={TOKEN.punct}>{`<`}</T>
          <T c={TOKEN.type}>IReadOnlyDictionary</T>
          <T c={TOKEN.punct}>{`<`}</T>
          <T c={TOKEN.type}>Guid</T>
          <T c={TOKEN.punct}>, </T>
          <T c={TOKEN.type}>Product</T>
          <T c={TOKEN.punct}>{`>>`}</T>
          {"\n"}
          {`  `}
          <T c={TOKEN.fn}>GetProductsByIdAsync</T>
          <T c={TOKEN.punct}>(</T>
          <T c={TOKEN.type}>IReadOnlyList</T>
          <T c={TOKEN.punct}>{`<`}</T>
          <T c={TOKEN.type}>Guid</T>
          <T c={TOKEN.punct}>{`>`}</T> <T c={TOKEN.param}>ids</T>
          <T c={TOKEN.punct}>, </T>
          <T c={TOKEN.type}>CatalogDbContext</T> <T c={TOKEN.param}>db</T>
          <T c={TOKEN.punct}>{`) =>`}</T>
          {"\n"}
          {`  `}
          <T c={TOKEN.kw}>await</T> <T c={TOKEN.param}>db</T>
          <T c={TOKEN.punct}>.</T>Products<T c={TOKEN.punct}>.</T>
          <T c={TOKEN.fn}>Where</T>
          <T c={TOKEN.punct}>(</T>
          <T c={TOKEN.param}>p</T> <T c={TOKEN.punct}>{`=> `}</T>
          <T c={TOKEN.param}>ids</T>
          <T c={TOKEN.punct}>.</T>
          <T c={TOKEN.fn}>Contains</T>
          <T c={TOKEN.punct}>(</T>
          <T c={TOKEN.param}>p</T>
          <T c={TOKEN.punct}>.</T>Id<T c={TOKEN.punct}>));</T>
          {"\n"}
        </pre>
      </div>

      <div className="border-cc-card-border bg-cc-surface/70 grid grid-cols-2 border-t">
        <div className="text-cc-ink-dim px-5 py-2.5 font-mono text-[11px]">
          hand-written SDL, hand-wired resolvers
        </div>
        <div className="border-cc-card-border text-cc-accent border-l px-5 py-2.5 font-mono text-[11px]">
          schema + resolvers + DataLoader emitted at build
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// CompareSection: the standard 12-col layout used by sections 01..05.
// -----------------------------------------------------------------------------

interface CompareSectionProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly lead: string;
  readonly topSlot?: ReactNode;
  readonly rows: readonly {
    readonly left: ReactNode;
    readonly right: ReactNode;
  }[];
}

function CompareSection({
  id,
  index,
  eyebrow,
  title,
  lead,
  topSlot,
  rows,
}: CompareSectionProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
        <SectionHeader
          index={index}
          eyebrow={eyebrow}
          title={title}
          lead={lead}
        />
        <div className="lg:col-span-9">
          <CompareGrid topSlot={topSlot}>
            {rows.map((row, i) => (
              <CompareRow
                key={i}
                left={row.left}
                right={row.right}
                last={i === rows.length - 1}
              />
            ))}
          </CompareGrid>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// ProofPairRow: one row in the licensing/open-source paired stat band. Renders
// label on both sides, dimmed value on the left, concrete value on the right.
// -----------------------------------------------------------------------------

interface ProofPairRowProps {
  readonly label: string;
  readonly left: string;
  readonly right: string;
  readonly last?: boolean;
}

function ProofPairRow({ label, left, right, last = false }: ProofPairRowProps) {
  const border = last ? "" : "border-cc-card-border border-b";
  return (
    <>
      <div className={["px-5 py-4", border].join(" ")}>
        <div className="text-cc-ink-dim/80 font-mono text-[10.5px] tracking-[0.18em] uppercase">
          {label}
        </div>
        <div className="text-cc-ink-dim mt-1.5 font-mono text-[13px]">
          {left}
        </div>
      </div>
      <div className={["px-5 py-4", border].join(" ")}>
        <div className="text-cc-accent font-mono text-[10.5px] tracking-[0.18em] uppercase">
          {label}
        </div>
        <div className="text-cc-heading font-heading mt-1.5 text-xl font-semibold tracking-tight">
          {right}
        </div>
      </div>
    </>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function HotChocolatePreviewV5() {
  return (
    <>
      {/* HERO: small left rail, full-width diff card, dual CTAs centered. */}
      <section className="pt-12 pb-12 sm:pt-20 sm:pb-16">
        <div className="grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-3">
            <Eyebrow>GraphQL server for .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading text-hero mt-5 font-semibold tracking-tight text-balance">
              See the diff.
            </h1>
            <p className="text-cc-prose text-lead mt-6 leading-relaxed">
              The same .NET GraphQL feature, written two ways. On the left, what
              you write by hand. On the right, what Hot Chocolate generates from
              a partial class. Same outcome, different surface area.
            </p>
            <dl className="border-cc-card-border mt-8 grid grid-cols-3 gap-6 border-t pt-6">
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  License
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Runtime
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">ASP.NET Core</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Spec
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">GraphQL 2025</dd>
              </div>
            </dl>
          </div>

          <div className="lg:col-span-9">
            <HeroDiffCard />
            <div className="mt-8 flex flex-wrap justify-center gap-3">
              <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
          </div>
        </div>
      </section>

      {/* Compact capabilities band: six chips on a thin border-y separator. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-6">
          {[
            "Source-generated schema",
            "Code-first or schema-first",
            "DataLoader batching",
            "Realtime subscriptions",
            "OpenTelemetry built in",
            "Federation-ready",
          ].map((label) => (
            <li
              key={label}
              className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
            >
              <span className="text-cc-accent" aria-hidden>
                <CheckIcon size={12} />
              </span>
              {label}
            </li>
          ))}
        </ul>
      </section>

      {/* 01 Schema authoring */}
      <CompareSection
        id="schema"
        index="01"
        eyebrow="Schema authoring"
        title="Where does the schema actually live?"
        lead="A GraphQL schema is a contract. The question is whether your C# types are the contract, or a second artefact you keep in sync with one. Hot Chocolate makes C# the source of truth and emits the schema from it."
        rows={[
          {
            left: "SDL files in a string or a .graphql resource, kept in sync with C# by hand.",
            right:
              "C# is the schema. A Roslyn source generator emits SDL from your partial class at build time.",
          },
          {
            left: "Renaming a field means editing two places. The compiler will not catch the drift.",
            right:
              "Rename a property in C# and the schema follows. nameof keeps resolver paths typed.",
          },
          {
            left: "Descriptions live in markdown files or are missing from the schema entirely.",
            right:
              "XML doc comments on your C# types become GraphQL field descriptions.",
          },
          {
            left: "Nullability is whatever you typed into the SDL string. T? in C# is not enforced.",
            right:
              "Nullable reference types in C# project straight into the GraphQL schema.",
          },
          {
            left: "Resolver wiring lives in DI configuration, separate from the type.",
            right:
              "Resolvers are plain C# methods with DI-injected services and CancellationToken.",
          },
        ]}
      />

      {/* 02 N+1 / DataLoader */}
      <CompareSection
        id="dataloader"
        index="02"
        eyebrow="N+1"
        title="Batched data access, generated for you."
        lead="Every GraphQL server eventually hits N+1. The honest version of solving it is per-request batching with deduplication. Green Donut is built into Hot Chocolate and the loader code is source-generated from a single attribute."
        topSlot={
          <>
            <MiniCode
              side="left"
              title="By hand"
              lines={[
                "public class ProductByIdLoader",
                "  : BatchDataLoader<Guid, Product>",
                "{",
                "  // implement LoadBatchAsync,",
                "  // wire IDataLoader<T> in DI,",
                "  // remember per-request scope...",
                "}",
              ]}
            />
            <MiniCode
              side="right"
              title="Generated"
              lines={[
                "[DataLoader]",
                "internal static Task<",
                "  IReadOnlyDictionary<Guid, Product>>",
                "  GetProductsByIdAsync(",
                "    IReadOnlyList<Guid> ids,",
                "    CatalogDbContext db,",
                "    CancellationToken ct) => ...",
              ]}
            />
          </>
        }
        rows={[
          {
            left: "Hand-roll one BatchDataLoader<TKey, TValue> class per relationship.",
            right:
              "Annotate a static method with [DataLoader] and the loader, interface, and DI registration are emitted.",
          },
          {
            left: "Entity Framework Core selections are written twice: once in C#, once in SQL.",
            right:
              "Projections push the selection set into native EF Core, MongoDB, Marten, and Raven queries.",
          },
          {
            left: "Per-request caching is a manual concern, easy to misconfigure.",
            right:
              "Per-request deduplication and caching ship by default with every DataLoader.",
          },
          {
            left: "One-to-many relationships require yet another bespoke loader class.",
            right:
              "Group loaders cover one-to-many fan-out, batch loaders cover one-to-one.",
          },
          {
            left: "Any IQueryable<T> needs a custom shim to participate in batching.",
            right:
              "Works against Entity Framework Core, MongoDB, Marten, Raven, or any IQueryable.",
          },
          {
            left: "Field execution order has to be tuned by hand to avoid waterfalls.",
            right:
              "The execution engine resolves fields in waves so batches dispatch together.",
          },
        ]}
      />

      {/* 03 Realtime */}
      <CompareSection
        id="subscriptions"
        index="03"
        eyebrow="Realtime"
        title="Subscriptions without the plumbing."
        lead="Realtime GraphQL is two problems: a transport, and a pub/sub. Hot Chocolate gives you both via attributes. Your code stays domain code."
        rows={[
          {
            left: "Pick a WebSocket library, implement the protocol, decide about SSE later.",
            right:
              "graphql-ws and graphql-sse ship in the box. Both transports run side by side.",
          },
          {
            left: "Topics are static strings sprinkled through services, easy to typo.",
            right:
              "[SubscriptionType] with [Topic] derives topics from arguments at compile time.",
          },
          {
            left: "Switching pub/sub providers means rewriting publisher and consumer code.",
            right:
              "Swap Redis, NATS, Postgres LISTEN/NOTIFY, RabbitMQ, or in-memory behind ITopicEventSender.",
          },
          {
            left: "Partial responses need a second API surface bolted on.",
            right:
              "@defer and @stream stream partial responses on the same connection without a second API surface.",
          },
          {
            left: "Choosing between WebSocket and Server-Sent Events forces a one-way commitment.",
            right:
              "graphql-ws and graphql-sse run side by side, so clients can pick the transport that fits.",
          },
        ]}
      />

      {/* 04 Observability */}
      <CompareSection
        id="otel"
        index="04"
        eyebrow="Observability"
        title="OpenTelemetry, on by configuration."
        lead="GraphQL traces are only useful when the spans share a vocabulary your backend already speaks. Hot Chocolate follows the proposed OpenTelemetry semantic conventions for GraphQL so traces, metrics, and logs line up with everything else."
        rows={[
          {
            left: "Stand up your own ActivitySource and pick span names you hope to keep stable.",
            right:
              "AddInstrumentation() + AddHotChocolateInstrumentation() wires the conventions for you.",
          },
          {
            left: "One layer of spans, either at the transport or at the resolver, never both.",
            right:
              "Three diagnostic layers: server transport, execution pipeline, and DataLoader batches.",
          },
          {
            left: "Span attributes leak high-cardinality fields and blow up your trace store.",
            right:
              "Low-cardinality root span names by design. ActivityEnricher attaches custom data on top.",
          },
          {
            left: "Trusted document ids and operation hashes have to be plumbed into spans manually.",
            right:
              "Operation type, document hash, trusted document id, and selection set arrive on the span.",
          },
          {
            left: "Tied to one vendor SDK and one collector.",
            right:
              "Works with Jaeger, Tempo, Datadog, Honeycomb, or any OTLP endpoint via the standard exporter.",
          },
        ]}
      />

      {/* 05 Federation */}
      <CompareSection
        id="federation"
        index="05"
        eyebrow="Federation"
        title="Same server, three deployment shapes."
        lead="Federation should be an operational decision, not an architectural one. The Hot Chocolate server runs standalone, as a Fusion subgraph composed at planning time, or as an Apollo Federation subgraph. Resolvers do not change."
        rows={[
          {
            left: "Composition happens inside the gateway at startup or at runtime.",
            right:
              "Fusion composes subgraphs at planning time in CI, against the source SDLs.",
          },
          {
            left: "The query plan is built per request, every time, on the gateway.",
            right:
              "The planned query lives next to the gateway, so the runtime stays cheap at the edge.",
          },
          {
            left: "Moving to a different gateway means rewriting resolvers for a new runtime.",
            right:
              "Same server runs standalone, as a Fusion subgraph, or as an Apollo Federation subgraph.",
          },
          {
            left: "Cost analysis is its own product, sold separately.",
            right:
              "@cost and @listSize directives apply at every tier, gateway and subgraph alike.",
          },
        ]}
      />

      {/* 06 Open source / licensing: ProofItem-style paired stats. */}
      <section
        id="open-source"
        className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
      >
        <div className="grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
          <SectionHeader
            index="06"
            eyebrow="Open source"
            title="MIT licensed, GitHub-hosted, IDE included."
            lead="The whole platform is open source. The codebase, the issue tracker, the roadmap, and the release notes all live on GitHub. The Nitro GraphQL IDE is served from the server endpoint by default."
          />
          <div className="lg:col-span-9">
            <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border">
              <div
                aria-hidden
                className="bg-cc-card-border pointer-events-none absolute inset-y-0 left-1/2 w-px"
              />
              <div className="grid grid-cols-2">
                <div className="border-cc-card-border bg-cc-surface/60 border-b">
                  <ColumnHeader side="left" label={LEFT_LABEL} />
                </div>
                <div className="border-cc-card-border bg-cc-surface/60 border-b">
                  <ColumnHeader side="right" label={RIGHT_LABEL} />
                </div>

                <ProofPairRow
                  label="License"
                  left="whatever you wrote"
                  right="MIT"
                />
                <ProofPairRow
                  label="Runtime"
                  left="ASP.NET Core, hand-wired"
                  right=".NET / ASP.NET Core"
                />
                <ProofPairRow
                  label="Spec"
                  left="partial, hand-tracked"
                  right="GraphQL 2025"
                />
                <ProofPairRow
                  label="Transports"
                  left="one you implemented"
                  right="HTTP / WS / SSE"
                />
                <ProofPairRow
                  label="Federation"
                  left="glue code per gateway"
                  right="Fusion + Apollo"
                />
                <ProofPairRow
                  label="Client"
                  left="write your own"
                  right="Strawberry Shake"
                  last
                />
              </div>
            </div>

            {/* Embedded Nitro IDE teaser, the only non-compare visual. */}
            <div className="mt-10">
              <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
                <div>
                  <Eyebrow>Bundled IDE</Eyebrow>
                  <h3 className="text-cc-heading font-heading text-h3 mt-3 font-semibold tracking-tight">
                    Nitro at /graphql, on every server.
                  </h3>
                </div>
                <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
                  live at /graphql
                </p>
              </div>
              <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border">
                <NitroCompose />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA. Single SPECTRUM hairline above. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Pick your side</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 mx-auto mt-5 max-w-3xl font-semibold tracking-tight text-balance">
            Ship the right-hand column.
          </h2>
          <p className="text-cc-prose text-lead mx-auto mt-5 max-w-2xl leading-relaxed">
            A partial class, a Roslyn source generator, the ASP.NET Core you
            already run. The schema, the DataLoaders, and the resolver pipeline
            are emitted at build time.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
