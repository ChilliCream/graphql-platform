import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Preview variant (v4) of the Agentic coding page. Stance: "The Operator's
 * Manifest", family dense-catalog. The page reads like a developer reference
 * manual: a fixed left ledger gutter with section indices and route slugs
 * runs the full page length; the content column is typeset data (definition
 * lists, tight tables, key/value spec rows, runbook lines, a directory tree).
 *
 * Accent is violet (#7c92c6); coral (#f0786a) is reserved strictly for the
 * single destructive hint in the catalog. No diagrams, no oversized display
 * type, no bento, no cards. Hairlines only.
 */

export const metadata: Metadata = {
  title: "Agentic Coding: GraphQL MCP for Coding Agents",
  description:
    "GraphQL MCP for coding agents. A dense reference manifest: schema and client registry ground the agent, published operations become governed MCP tools.",
  keywords: [
    "agentic coding feedback loop",
    "GraphQL MCP server",
    "operations as MCP tools",
    "agent tool lifecycle governance",
    "MCP behavior annotations",
    "idempotent destructive openWorld hints",
    "client registry grounding for agents",
    "skillz agent conventions",
    "validate MCP tools in CI",
    ".NET GraphQL agents",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "GraphQL MCP for Coding Agents",
    description:
      "GraphQL MCP for coding agents. The dense operator manifest: schema, registry, published ops as governed tools, with hints, lifecycle, and tracing.",
  },
};

const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

/**
 * Build-time stamp shown in the manifest header. Computed once at module load
 * so the header always reads internally consistent with the current build,
 * instead of carrying a stale literal date.
 */
const BUILD_STAMP = new Date().toISOString().slice(0, 10);

/* ------------------------------------------------------------------ *
 * Layout shell: ledger gutter + content column
 * ------------------------------------------------------------------ */

interface SectionRowProps {
  readonly index: string;
  readonly title: string;
  readonly slug: string;
  readonly children: ReactNode;
}

/**
 * A numbered section band. The gutter shows the section index plus the route
 * slug; the content column opens with the small mono index above a text-h5
 * subsection title, then the section body.
 */
function SectionRow({ index, title, slug, children }: SectionRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-[12rem_1fr] border-t">
      <div className="border-cc-card-border border-r px-4 py-8 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        <p style={{ color: VIOLET }}>{index}</p>
        <p className="text-cc-ink-faint mt-2">{slug}</p>
      </div>
      <div className="px-6 py-8">
        <p
          className="font-mono text-[0.65rem] tracking-[0.18em] uppercase"
          style={{ color: VIOLET }}
        >
          {index}
        </p>
        <h2 className="font-heading text-cc-heading text-h5 mt-2 font-semibold">
          {title}
        </h2>
        <div className="mt-6">{children}</div>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * §01 GROUNDING: definition list + stat row
 * ------------------------------------------------------------------ */

interface GroundingTerm {
  readonly term: string;
  readonly body: string;
}

const GROUNDING_TERMS: readonly GroundingTerm[] = [
  {
    term: "schema",
    body: "The typed, introspectable graph. Every tool argument and return shape is contract-checked, so agents make fewer malformed calls and the destructive ones stay marked.",
  },
  {
    term: "client registry",
    body: "Real published operations, the queries and mutations your clients already ship. The agent grounds in field demand that exists, not invented shapes no client would send.",
  },
  {
    term: "published ops",
    body: "Each operation becomes a callable MCP tool with an accurate parameter contract. The catalog is the surface area, you decide what is in it.",
  },
  {
    term: "behavior hints",
    body: "idempotentHint, readOnlyHint, openWorldHint, and destructiveHint travel with each tool. The agent reads them before it acts; gates trigger on the dangerous ones.",
  },
];

function GroundingBand() {
  return (
    <>
      <dl className="border-cc-card-border divide-cc-card-border divide-y border-t border-b">
        {GROUNDING_TERMS.map((row) => (
          <div
            key={row.term}
            className="grid grid-cols-[10rem_1fr] gap-6 px-1 py-3.5"
          >
            <dt
              className="font-mono text-xs tracking-wide"
              style={{ color: VIOLET }}
            >
              {row.term}
            </dt>
            <dd className="text-cc-ink text-body text-pretty">{row.body}</dd>
          </div>
        ))}
      </dl>

      <div className="border-cc-card-border divide-cc-card-border mt-6 grid grid-cols-3 divide-x border-t border-b">
        <div className="px-4 py-4">
          <p className="text-cc-heading font-heading text-h4 font-semibold tabular-nums">
            38
          </p>
          <p className="text-cc-ink-faint mt-1 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            ops
          </p>
        </div>
        <div className="px-4 py-4">
          <p className="text-cc-heading font-heading text-h4 font-semibold tabular-nums">
            4
          </p>
          <p className="text-cc-ink-faint mt-1 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            hint kinds
          </p>
        </div>
        <div className="px-4 py-4">
          <p className="text-cc-heading font-heading text-h4 font-semibold tabular-nums">
            1
          </p>
          <p className="text-cc-ink-faint mt-1 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            endpoint
          </p>
        </div>
      </div>
    </>
  );
}

/* ------------------------------------------------------------------ *
 * §02 CATALOG: tight table of published operations
 * ------------------------------------------------------------------ */

type Hint = "read-only" | "idempotent" | "open-world" | "destructive";

interface CatalogRow {
  readonly op: string;
  readonly kind: "query" | "mutation";
  readonly summary: string;
  readonly hint: Hint;
}

const CATALOG: readonly CatalogRow[] = [
  {
    op: "getProduct",
    kind: "query",
    summary: "single product by id",
    hint: "read-only",
  },
  {
    op: "searchOrders",
    kind: "query",
    summary: "filtered order list",
    hint: "idempotent",
  },
  {
    op: "listSkills",
    kind: "query",
    summary: "installed skills index",
    hint: "read-only",
  },
  {
    op: "tagProduct",
    kind: "mutation",
    summary: "upsert product tags",
    hint: "idempotent",
  },
  {
    op: "openTicket",
    kind: "mutation",
    summary: "calls an external desk",
    hint: "open-world",
  },
  {
    op: "deleteReview",
    kind: "mutation",
    summary: "remove a review",
    hint: "destructive",
  },
];

interface HintCellProps {
  readonly hint: Hint;
}

function HintCell({ hint }: HintCellProps) {
  const label =
    hint === "read-only"
      ? "readOnlyHint"
      : hint === "idempotent"
        ? "idempotentHint"
        : hint === "open-world"
          ? "openWorldHint"
          : "destructiveHint";

  if (hint === "destructive") {
    return (
      <span
        className="rounded border px-1.5 py-0.5 font-mono text-[0.62rem]"
        style={{
          color: CORAL,
          borderColor: "rgba(240,120,106,0.5)",
          backgroundColor: "rgba(240,120,106,0.08)",
        }}
      >
        {label}
      </span>
    );
  }

  return (
    <span className="border-cc-card-border bg-cc-surface text-cc-ink-dim rounded border px-1.5 py-0.5 font-mono text-[0.62rem]">
      {label}
    </span>
  );
}

function CatalogTable() {
  return (
    <div className="border-cc-card-border overflow-x-auto border">
      <table className="w-full border-collapse font-mono text-xs">
        <thead>
          <tr className="border-cc-card-border bg-cc-surface text-cc-ink-faint border-b">
            <th className="border-cc-card-border border-r px-3 py-1.5 text-left text-[0.62rem] tracking-[0.18em] uppercase">
              op
            </th>
            <th className="border-cc-card-border border-r px-3 py-1.5 text-left text-[0.62rem] tracking-[0.18em] uppercase">
              kind
            </th>
            <th className="border-cc-card-border border-r px-3 py-1.5 text-left text-[0.62rem] tracking-[0.18em] uppercase">
              summary
            </th>
            <th className="px-3 py-1.5 text-left text-[0.62rem] tracking-[0.18em] uppercase">
              hint
            </th>
          </tr>
        </thead>
        <tbody>
          {CATALOG.map((row, i) => (
            <tr
              key={row.op}
              className={`border-cc-card-border border-b last:border-b-0 ${
                i % 2 === 1 ? "bg-cc-surface" : ""
              }`}
            >
              <td className="border-cc-card-border text-cc-ink border-r px-3 py-1.5">
                {row.op}
              </td>
              <td className="border-cc-card-border text-cc-ink-dim border-r px-3 py-1.5">
                {row.kind}
              </td>
              <td className="border-cc-card-border text-cc-ink-dim border-r px-3 py-1.5">
                {row.summary}
              </td>
              <td className="px-3 py-1.5">
                <HintCell hint={row.hint} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * §03 ENDPOINT: key/value spec table
 * ------------------------------------------------------------------ */

interface SpecRow {
  readonly key: string;
  readonly value: string;
  readonly accent?: boolean;
}

const ENDPOINT_SPEC: readonly SpecRow[] = [
  { key: "route", value: "/graphql/mcp", accent: true },
  { key: "transport", value: "Streamable HTTP" },
  { key: "auth", value: "inherits API auth" },
  { key: "schema source", value: "the live GraphQL schema" },
  { key: "tool surface", value: "published operations + skills" },
  { key: "drift", value: "none, one schema grounds both" },
];

function EndpointBand() {
  return (
    <table className="border-cc-card-border w-full border-collapse border font-mono text-xs">
      <tbody>
        {ENDPOINT_SPEC.map((row, i) => (
          <tr
            key={row.key}
            className={`border-cc-card-border border-b last:border-b-0 ${
              i % 2 === 1 ? "bg-cc-surface" : ""
            }`}
          >
            <td className="border-cc-card-border text-cc-ink-faint w-[14rem] border-r px-3 py-1.5 text-[0.65rem] tracking-[0.18em] uppercase">
              {row.key}
            </td>
            <td className="px-3 py-1.5">
              {row.accent ? (
                <span style={{ color: VIOLET }}>{row.value}</span>
              ) : (
                <span className="text-cc-ink">{row.value}</span>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

/* ------------------------------------------------------------------ *
 * §04 LIFECYCLE: four flush runbook rows
 * ------------------------------------------------------------------ */

interface LifecycleStep {
  readonly index: string;
  readonly stage: string;
  readonly note: string;
  readonly command: string;
}

const LIFECYCLE: readonly LifecycleStep[] = [
  {
    index: "01",
    stage: "author",
    note: "in repo · .graphql + settings",
    command: "nitro mcp author",
  },
  {
    index: "02",
    stage: "validate",
    note: "in CI · before merge",
    command: "nitro mcp validate",
  },
  {
    index: "03",
    stage: "stage",
    note: "promote with approval gate",
    command: "nitro mcp promote",
  },
  {
    index: "04",
    stage: "trace",
    note: "per-tool p95 in Nitro",
    command: "nitro mcp trace",
  },
];

function LifecycleBand() {
  return (
    <ol className="border-cc-card-border divide-cc-card-border divide-y border-t border-b font-mono text-xs">
      {LIFECYCLE.map((step) => (
        <li
          key={step.index}
          className="grid grid-cols-[3rem_8rem_1fr_auto] items-center gap-4 px-1 py-2.5"
        >
          <span className="text-cc-ink-faint tabular-nums">{step.index}</span>
          <span className="text-cc-ink">{step.stage}</span>
          <span className="text-cc-ink-dim">{step.note}</span>
          <span className="text-right" style={{ color: VIOLET }}>
            {step.command}
          </span>
        </li>
      ))}
    </ol>
  );
}

/* ------------------------------------------------------------------ *
 * §05 SKILLZ: directory listing
 * ------------------------------------------------------------------ */

interface SkillFile {
  readonly name: string;
  readonly size: string;
  readonly body: string;
}

const SKILLZ: readonly SkillFile[] = [
  {
    name: "pagination.SKILL.md",
    size: "2.1kB",
    body: "Always page list fields with the registry connection contract.",
  },
  {
    name: "errors.SKILL.md",
    size: "1.7kB",
    body: "Model failures as typed union results, never thrown exceptions.",
  },
  {
    name: "naming.SKILL.md",
    size: "1.4kB",
    body: "Mutation inputs and payloads follow the team naming rules.",
  },
  {
    name: "auth.SKILL.md",
    size: "1.9kB",
    body: "Gate fields with the shared policy directives, not ad-hoc checks.",
  },
];

function SkillzBand() {
  return (
    <div className="font-mono text-xs">
      <p className="text-cc-ink-faint mb-2">skillz/</p>
      <ul className="border-cc-card-border divide-cc-card-border divide-y border-t border-b">
        {SKILLZ.map((file) => (
          <li
            key={file.name}
            className="grid grid-cols-[auto_5rem_1fr] items-baseline gap-4 px-1 py-2"
          >
            <span className="pl-4" style={{ color: VIOLET }}>
              {file.name}
            </span>
            <span className="text-cc-ink-faint tabular-nums">{file.size}</span>
            <span className="text-cc-ink-dim">{file.body}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * §06 GUARANTEES: numbered honesty points
 * ------------------------------------------------------------------ */

const HONESTY_POINTS: readonly string[] = [
  "Tools and prompts are authored in the repo as reviewed code, not minted at runtime.",
  "nitro mcp validate runs in CI, so a broken tool collection never reaches a stage.",
  "Behavior is declared with idempotentHint, destructiveHint, and openWorldHint.",
  "An edit is checked against published operations; risky changes read published clients affected.",
  "Every tool call is traced in Nitro with p95 latency, error rate, and impact.",
];

function GuaranteesBand() {
  return (
    <ol className="border-cc-card-border divide-cc-card-border divide-y border-t border-b">
      {HONESTY_POINTS.map((point, i) => (
        <li
          key={point}
          className="grid grid-cols-[3.5rem_1fr] items-baseline gap-4 px-1 py-3"
        >
          <span
            className="font-mono text-xs tabular-nums"
            style={{ color: VIOLET }}
          >
            [0{i + 1}]
          </span>
          <span className="text-cc-ink text-body text-pretty">{point}</span>
        </li>
      ))}
    </ol>
  );
}

/* ------------------------------------------------------------------ *
 * Page
 * ------------------------------------------------------------------ */

export default function AgenticCodingPreviewV4() {
  return (
    <article className="border-cc-card-border -mx-4 border-x sm:-mx-6 lg:-mx-8">
      {/* -------------------------------------------------------- *
       * Manifest header: pinned mono status bar
       * -------------------------------------------------------- */}
      <header className="border-cc-card-border border-b">
        <div className="grid grid-cols-[12rem_1fr] items-center font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          <div className="border-cc-card-border text-cc-ink-faint border-r px-4 py-2.5">
            /platform
          </div>
          <div className="flex flex-wrap items-center justify-between gap-3 px-6 py-2.5">
            <span style={{ color: VIOLET }}>/graphql/mcp</span>
            <span className="flex items-center gap-3">
              <span
                className="rounded border px-1.5 py-0.5"
                style={{
                  color: VIOLET,
                  borderColor: "rgba(124,146,198,0.4)",
                  backgroundColor: "rgba(124,146,198,0.08)",
                }}
              >
                governed
              </span>
              <span className="text-cc-ink-dim">v0.preview</span>
              <span className="text-cc-ink-faint">updated {BUILD_STAMP}</span>
            </span>
          </div>
        </div>
      </header>

      {/* -------------------------------------------------------- *
       * Hero band: deliberately quiet text-h2, single inline CTA prompt
       * -------------------------------------------------------- */}
      <section className="grid grid-cols-[12rem_1fr]">
        <div className="border-cc-card-border border-r px-4 py-10 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          <p style={{ color: VIOLET }}>§00</p>
          <p className="text-cc-ink-faint mt-2">manifest</p>
          <p className="text-cc-ink-faint mt-6">line 0001</p>
        </div>
        <div className="px-6 py-10">
          <p
            className="font-mono text-[0.65rem] tracking-[0.18em] uppercase"
            style={{ color: VIOLET }}
          >
            §00 · manifest
          </p>
          <h1 className="font-heading text-cc-heading text-h2 mt-3 font-semibold text-balance">
            GraphQL MCP for coding agents.
          </h1>
          <p className="text-cc-ink-dim text-lead mt-5 max-w-2xl text-pretty">
            A governed feedback loop. Schema and client registry ground the
            agent, published operations become tools you author in repo,
            validate in CI, stage, and trace.
          </p>

          <p className="text-cc-ink-dim mt-8 font-mono text-xs">
            <span className="text-cc-ink-faint">&rsaquo;</span>{" "}
            <span style={{ color: VIOLET }}>nitro mcp init</span>
            <span
              aria-hidden="true"
              className="ml-1 inline-block h-3 w-1.5 align-middle"
              style={{ backgroundColor: VIOLET }}
            />
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>
        </div>
      </section>

      {/* -------------------------------------------------------- *
       * §01 GROUNDING
       * -------------------------------------------------------- */}
      <SectionRow
        index="§01 · grounding"
        slug="line 0042"
        title="What the agent stands on."
      >
        <GroundingBand />
      </SectionRow>

      {/* -------------------------------------------------------- *
       * §02 CATALOG
       * -------------------------------------------------------- */}
      <SectionRow
        index="§02 · catalog"
        slug="line 0118"
        title="Published operations, as tools."
      >
        <p className="text-cc-ink-dim text-body mb-5 max-w-3xl text-pretty">
          Each row is a real, reviewed operation a client already ships. The
          hint column is the contract the agent reads before it acts; every call
          is traced in Nitro.
        </p>
        <CatalogTable />
        <p className="text-cc-ink-faint mt-3 font-mono text-[0.65rem]">
          legend: readOnlyHint · idempotentHint · openWorldHint ·{" "}
          <span style={{ color: CORAL }}>destructiveHint</span>
        </p>
      </SectionRow>

      {/* -------------------------------------------------------- *
       * §03 ENDPOINT
       * -------------------------------------------------------- */}
      <SectionRow
        index="§03 · endpoint"
        slug="line 0204"
        title="One MCP hub. No second surface."
      >
        <p className="text-cc-ink-dim text-body mb-5 max-w-3xl text-pretty">
          The same schema and registry that run the API ground the agent. There
          is no parallel tool definition to drift. The table below is the spec;
          it is also the diagram.
        </p>
        <EndpointBand />
      </SectionRow>

      {/* -------------------------------------------------------- *
       * §04 LIFECYCLE
       * -------------------------------------------------------- */}
      <SectionRow
        index="§04 · lifecycle"
        slug="line 0287"
        title="Author, validate, stage, trace."
      >
        <p className="text-cc-ink-dim text-body mb-5 max-w-3xl text-pretty">
          Every tool moves through a fixed sequence. Reviewed code, then a CI
          gate, then a staged promotion with approval, then production traces.
          You can read it as a runbook.
        </p>
        <LifecycleBand />
      </SectionRow>

      {/* -------------------------------------------------------- *
       * §05 SKILLZ
       * -------------------------------------------------------- */}
      <SectionRow
        index="§05 · skillz"
        slug="line 0341"
        title="Conventions, packaged."
      >
        <p className="text-cc-ink-dim text-body mb-5 max-w-3xl text-pretty">
          skillz files travel with the repo. Installed into the agents your team
          already uses, they make the next pull request look like your codebase,
          not a generic one.
        </p>
        <SkillzBand />
      </SectionRow>

      {/* -------------------------------------------------------- *
       * §06 GUARANTEES
       * -------------------------------------------------------- */}
      <SectionRow
        index="§06 · guarantees"
        slug="line 0408"
        title="What we actually claim."
      >
        <p className="text-cc-ink-dim text-body mb-5 max-w-3xl text-pretty">
          Honesty is the differentiator. We do not promise to mint safe tools at
          runtime or to name every client that breaks. We promise a governed,
          observed path. Five points, no more.
        </p>
        <GuaranteesBand />
      </SectionRow>

      {/* -------------------------------------------------------- *
       * §07 FOOTER MANIFEST: closing status bar
       * -------------------------------------------------------- */}
      <footer className="border-cc-card-border border-t">
        <div className="grid grid-cols-[12rem_1fr] items-center font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          <div className="border-cc-card-border text-cc-ink-faint border-r px-4 py-3">
            {"// end of manifest"}
          </div>
          <div className="flex flex-wrap items-center justify-between gap-4 px-6 py-3">
            <span className="flex flex-wrap items-center gap-5">
              <Link
                href="/get-started"
                className="hover:text-cc-heading transition-colors"
                style={{ color: VIOLET }}
              >
                start for free &rarr;
              </Link>
              <Link
                href="/docs/nitro/apis/client-registry"
                className="text-cc-ink-dim hover:text-cc-heading transition-colors"
              >
                read the docs &rarr;
              </Link>
            </span>
            <span className="text-cc-ink-faint">/platform/agentic-coding</span>
          </div>
        </div>
      </footer>
    </article>
  );
}
