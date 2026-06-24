import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL MCP for Coding Agents | Feedback Loop",
  description:
    "Your .NET GraphQL server is already an MCP server. Ground coding agents in the fields clients use, validate tools in CI, and trace every call in Nitro.",
  keywords: [
    "GraphQL MCP server for coding agents",
    "agent tool lifecycle governance",
    "published operations client registry",
    "MCP tools and prompts in CI",
    "per-tool telemetry p95 impact",
    "persisted operations safelist for agents",
    ".NET GraphQL agentic coding",
    "Model Context Protocol",
    "GraphQL operations as MCP tools",
    "Streamable HTTP",
  ],
  openGraph: {
    title: "Give Coding Agents a Feedback Loop",
    description:
      "Your GraphQL server is already an MCP server. Ground coding agents in the fields clients use, validate tools in CI, and trace every call in Nitro.",
  },
};

/**
 * Brand spectrum (cyan -> violet -> coral), used at most once on the page to
 * tint a single phrase in the lead. Everything else stays in the calm
 * cream / grey / teal palette, matching the ScrollScenes treatment.
 */
const SPECTRUM =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 33%,#b681a9 63%,#f0786a 100%)";

/** Small reusable chip for the bento mini-illustrations. */
function Chip({
  children,
  active = false,
  dashed = false,
}: {
  readonly children: ReactNode;
  readonly active?: boolean;
  readonly dashed?: boolean;
}) {
  return (
    <span
      className={[
        "rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        active
          ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
          : dashed
            ? "border-cc-ink-faint text-cc-ink-dim border-dashed"
            : "border-cc-card-border text-cc-ink bg-cc-surface",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

/** Inline right-arrow connector in the calm faint-ink tone. */
function Arrow() {
  return (
    <span aria-hidden="true" className="text-cc-ink-faint px-0.5 text-sm">
      &rarr;
    </span>
  );
}

/** One headline figure, mirroring the ScrollScenes Stat idiom. */
function Stat({
  figure,
  label,
}: {
  readonly figure: string;
  readonly label: string;
}) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h3 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-2 text-xs/relaxed">{label}</p>
    </div>
  );
}

/** Shared eyebrow label used in every bento tile header. */
function TileLabel({ children }: { readonly children: ReactNode }) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
      {children}
    </p>
  );
}

/**
 * DOMINANT tile (4x2): the live agent feedback loop. Reuses the
 * AgentFeedbackLoop idiom: coding agent -> registry -> safe patch, with the
 * "published ops show the fields clients actually use" grounding rows.
 */
function FeedbackLoopTile() {
  const rows = [
    { label: "agent patch", value: "remove Product.price" },
    { label: "published ops", value: "ProductCard { id name price }" },
    { label: "feedback", value: "breaking: published clients affected" },
  ];

  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-3xl border p-6 backdrop-blur-sm transition-colors sm:p-8 md:col-span-4 md:row-span-2">
      <div className="flex items-center justify-between">
        <TileLabel>Agent feedback loop</TileLabel>
        <span className="border-cc-accent/50 text-cc-accent rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
          grounded
        </span>
      </div>

      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
        Risky edits come back as feedback.
      </h2>
      <p className="text-cc-ink mt-4 max-w-xl text-base/relaxed text-pretty">
        Agents move fast but guess at your graph. The client registry shows the
        fields published clients actually depend on, so an edit is checked
        against real field demand, not just whether the SDL parses.
      </p>

      <div className="mt-6 flex flex-wrap items-center gap-1.5">
        <Chip active>coding agent</Chip>
        <Arrow />
        <Chip>client registry</Chip>
        <Arrow />
        <Chip>safe patch</Chip>
      </div>

      <div className="border-cc-card-border mt-5 space-y-2 border-t pt-5">
        {rows.map((row) => (
          <div
            key={row.label}
            className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2.5"
          >
            <span className="text-cc-nav-label w-28 shrink-0 font-mono text-[0.55rem] tracking-[0.08em] uppercase">
              {row.label}
            </span>
            <span className="text-cc-ink font-mono text-xs">{row.value}</span>
          </div>
        ))}
      </div>

      <div className="border-cc-ink-faint mt-5 border-t border-dashed pt-4">
        <p className="text-cc-ink-dim text-xs">
          Declared field demand from published operations, not raw endpoint
          hits.
        </p>
      </div>
    </article>
  );
}

/**
 * TALL tile (2x2): "Your GraphQL server is already an MCP server." Tool
 * exposure shown as a small list of callable operations with behavior hints.
 */
function McpServerTile() {
  const tools = [
    { name: "getProduct", hint: "idempotent" },
    { name: "searchOrders", hint: "idempotent" },
    { name: "createReview", hint: "destructive" },
  ];

  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-3xl border p-6 backdrop-blur-sm transition-colors md:col-span-2 md:row-span-2">
      <TileLabel>Tool exposure</TileLabel>
      <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 mt-3 leading-tight font-semibold text-balance">
        Your GraphQL server is already an MCP server.
      </h2>
      <p className="text-cc-ink-dim mt-3 text-sm/relaxed">
        <code className="text-cc-accent">AddMcp()</code> and{" "}
        <code className="text-cc-accent">MapGraphQLMcp()</code> expose your
        operations as tools at{" "}
        <code className="text-cc-accent">/graphql/mcp</code> over Streamable
        HTTP, so existing operations become agent tools without rewriting the
        API.
      </p>

      <div className="mt-5 space-y-2">
        {tools.map((tool) => (
          <div
            key={tool.name}
            className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-2 rounded-lg border px-3 py-2"
          >
            <span className="text-cc-ink font-mono text-xs">{tool.name}</span>
            <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.08em] uppercase">
              {tool.hint}
            </span>
          </div>
        ))}
      </div>

      <p className="text-cc-ink-faint mt-auto pt-5 text-xs/relaxed">
        A typed, introspectable schema maps cleanly to the JSON Schema MCP uses
        for parameters, so tool defs are accurate and agents make fewer
        malformed calls.
      </p>
    </article>
  );
}

/**
 * WIDE tile (3x1): the governed tool-collection pipeline. Author in repo ->
 * validate in CI -> versioned -> stage-promote, in the ChangePipeline idiom.
 */
function PipelineTile() {
  const steps = [
    { label: "author in repo", note: ".graphql + settings", checked: true },
    { label: "nitro mcp validate", note: "CI + publish", checked: true },
    { label: "versioned", note: "feature collection", checked: true },
    { label: "stage-promote", note: "approval gate", checked: false },
  ];

  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-3xl border p-6 backdrop-blur-sm transition-colors md:col-span-3">
      <div className="flex items-center justify-between">
        <TileLabel>Governed tool lifecycle</TileLabel>
        <span className="border-cc-accent/50 text-cc-accent rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
          guarded
        </span>
      </div>
      <h3 className="text-cc-heading mt-3 text-base font-semibold">
        Authored as reviewed code, gated in CI.
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm/relaxed">
        Tools and prompts live in your repo and bundle into versioned feature
        collections. <code className="text-cc-accent">nitro mcp validate</code>{" "}
        checks GraphQL docs against your schema, so a broken collection never
        reaches a stage.
      </p>

      <ol className="mt-5 grid gap-2 sm:grid-cols-2">
        {steps.map((step, index) => (
          <li
            key={step.label}
            className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2.5"
          >
            <span className="text-cc-nav-label w-4 shrink-0 text-center font-mono text-[0.6rem]">
              {index + 1}
            </span>
            <span className="flex-1">
              <span className="text-cc-ink block font-mono text-xs">
                {step.label}
              </span>
              <span className="text-cc-ink-faint block font-mono text-[0.58rem]">
                {step.note}
              </span>
              <span className="sr-only">
                {step.checked ? "done" : "pending"}
              </span>
            </span>
            {step.checked && (
              <span className="text-cc-accent shrink-0">
                <CheckIcon />
              </span>
            )}
          </li>
        ))}
      </ol>
    </article>
  );
}

/**
 * 3x1 tile: skillz / approved tools. One SKILL.md fanning out to agents from a
 * single canonical store.
 */
function SkillzTile() {
  const agents = ["Claude Code", "Cursor", "Copilot", "50+ agents"];

  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-3xl border p-6 backdrop-blur-sm transition-colors md:col-span-3">
      <TileLabel>Enablement</TileLabel>
      <h3 className="text-cc-heading mt-3 text-base font-semibold">
        Teach your conventions once.
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm/relaxed">
        skillz packages your conventions as portable{" "}
        <code className="text-cc-accent">SKILL.md</code> files, installable
        across your team&rsquo;s agents from one canonical store, so agents
        write GraphQL the way your team does.
      </p>

      <div className="mt-5 flex flex-wrap items-center gap-2">
        <Chip active>SKILL.md</Chip>
        <Arrow />
        {agents.map((agent) => (
          <Chip key={agent}>{agent}</Chip>
        ))}
      </div>

      <p className="text-cc-ink-faint mt-auto pt-5 font-mono text-[0.6rem]">
        skills-lock.json &middot; reproducible team sets
      </p>
    </article>
  );
}

/**
 * Small tile (2x1): the safelist gate. Persisted operations act as a safelist
 * so agents run approved operations, not free-form queries.
 */
function SafelistTile() {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-3xl border p-6 backdrop-blur-sm transition-colors md:col-span-2">
      <TileLabel>Safelist gate</TileLabel>
      <h3 className="text-cc-heading mt-3 text-base font-semibold">
        Approved operations, not free-form queries.
      </h3>
      <div className="mt-4 space-y-2">
        <div className="border-cc-accent/40 bg-cc-surface flex items-center justify-between gap-2 rounded-lg border px-3 py-2">
          <span className="text-cc-ink font-mono text-[0.65rem]">
            persisted op
          </span>
          <span className="sr-only">allowed</span>
          <span className="text-cc-accent shrink-0">
            <CheckIcon />
          </span>
        </div>
        <div className="border-cc-ink-faint flex items-center justify-between gap-2 rounded-lg border border-dashed px-3 py-2">
          <span className="text-cc-ink-dim font-mono text-[0.65rem]">
            arbitrary query
          </span>
          <span className="text-cc-ink-faint font-mono text-[0.65rem]">
            blocked
          </span>
        </div>
      </div>
      <p className="text-cc-ink-faint mt-auto pt-4 font-mono text-[0.58rem]">
        OnlyAllowPersistedDocuments &middot; ASP.NET Core auth
      </p>
    </article>
  );
}

/** Small tile (2x1): one-stat proof drawn from per-tool telemetry. */
function TelemetryStatTile() {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-3xl border p-6 backdrop-blur-sm transition-colors md:col-span-1">
      <TileLabel>Per-tool telemetry</TileLabel>
      <div className="mt-4">
        <Stat figure="p95" label="latency, error rate, and impact per tool" />
      </div>
      <p className="text-cc-ink-faint mt-auto pt-4 text-xs/relaxed">
        Every call traced in Nitro, so you can see which agent tool hurts
        production most.
      </p>
    </article>
  );
}

/** Final pre-merge checklist mirroring the release-safety CI checklist idiom. */
const TOOL_CHECKLIST: readonly string[] = [
  "Tools and prompts live in the repo as reviewed code",
  "nitro mcp validate runs in CI and inside publish",
  "Collections are versioned and stage-promoted with approval gates",
  "Persisted operations safelist what agents can run",
  "Every tool call is traced with p95, error rate, and impact in Nitro",
];

export default function AgenticCodingPage() {
  return (
    <>
      {/* Compact centered hero: kept short so the bento grid starts high. */}
      <section className="py-14 text-center sm:py-20">
        <p className="text-cc-ink-dim font-mono text-xs font-semibold tracking-widest uppercase">
          Agentic coding
        </p>
        <h1 className="font-heading text-cc-heading mx-auto mt-4 max-w-3xl text-4xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
          Give coding agents a feedback loop.
        </h1>
        <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
          Your GraphQL server is already an MCP server, so agents call your
          operations as tools. Nitro grounds them in the fields clients use,
          validates tools in CI, and traces every call, so fast edits come back
          as a{" "}
          <span
            className="bg-clip-text font-medium text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            feedback loop
          </span>
          , not a surprise.
        </p>
      </section>

      {/* Asymmetric bento grid: one dominant lifecycle tile + heterogeneous
          satellites, each a self-contained mini-illustration. */}
      <section className="pb-6">
        <h2 className="sr-only">The governed, observed agent-tool lifecycle</h2>
        <div className="grid auto-rows-min grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-6">
          <FeedbackLoopTile />
          <McpServerTile />
          <PipelineTile />
          <SkillzTile />
          <SafelistTile />
          <TelemetryStatTile />
        </div>

        {/* One-line lifecycle caption tying the tiles together. */}
        <p className="text-cc-ink-dim mt-8 text-center font-mono text-xs tracking-[0.12em] uppercase">
          Governed &middot; observed &middot; grounded
        </p>
      </section>

      {/* Differentiation + honesty beat: why the lifecycle, not the adapter. */}
      <section className="py-12">
        <div className="grid items-start gap-8 lg:grid-cols-2 lg:gap-12">
          <div>
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
              Why this, not just MCP
            </span>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
              Exposure is parity. The lifecycle is the point.
            </h2>
            <div className="text-cc-ink mt-5 space-y-4 text-base/relaxed text-pretty">
              <p>
                Everyone has an MCP server now. Exposing operations as tools is
                table stakes. What an agent actually needs is a signal: which
                fields are in use, which tools are approved, and what each call
                costs in production.
              </p>
              <p>
                ChilliCream ships exposure, enablement, and observability as one
                .NET-native family. The Hot Chocolate adapter exposes the tools,
                skillz teaches your conventions, and Nitro, the same registry
                and OpenTelemetry engine you already run for the API, governs
                and observes them. The lifecycle is not stitched across separate
                point tools.
              </p>
            </div>
          </div>

          <div className="border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-8 backdrop-blur-sm">
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
              Honest scoping
            </span>
            <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-tight font-semibold text-balance">
              We say what the registries can prove.
            </h3>
            <p className="text-cc-ink mt-4 text-sm/relaxed">
              An edit is checked against the operations published clients
              registered, and a risky change reads &ldquo;published clients
              affected.&rdquo; That is grounding in declared field demand, not a
              claim to name every client and version. Token efficiency comes
              from typed tool defs, introspection-driven discovery, and field
              selection, not borrowed benchmark numbers.
            </p>
            <ul className="mt-6 space-y-3">
              {TOOL_CHECKLIST.map((item) => (
                <li key={item} className="flex items-start gap-3">
                  <span className="text-cc-accent mt-0.5 shrink-0">
                    <CheckIcon />
                  </span>
                  <span className="text-cc-ink text-sm/relaxed">{item}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      {/* Learn more + CTA row. */}
      <section className="py-12 text-center">
        <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold text-balance">
          Put your agents on a loop you control.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-base/relaxed">
          Expose your operations as tools, ground them in real field demand, and
          govern and observe every call in the platform your team already runs.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs/nitro/apis/client-registry">
            Read the Docs
          </OutlineButton>
        </div>
        <p className="text-cc-ink-dim mt-6 text-sm">
          Learn more about the{" "}
          <Link
            href="/docs/nitro/apis/client-registry"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            client registry
          </Link>
          , dig into{" "}
          <Link
            href="/platform/analytics"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            analytics
          </Link>
          ,{" "}
          <Link
            href="/platform/release-safety"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            release safety
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
      </section>
    </>
  );
}
