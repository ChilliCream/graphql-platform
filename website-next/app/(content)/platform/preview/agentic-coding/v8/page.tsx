import type { Metadata } from "next";
import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Preview variant (v8) of the Agentic coding page. Concept: "Beamlines, tool
 * calls in flight." Five vertical thin gradient beams descend the full page
 * background at very low opacity, anchored at the top and fading out two
 * thirds down, so the eye reads a continuous channel of agent calls raining
 * through the platform. Sections sit `relative z-10` on top of the static
 * beam layer; eyebrows pick up the same vocabulary as small mono beam stubs.
 *
 * Single accent: violet (#7c92c6). Coral is reserved for one destructiveHint
 * chip, the cyan, violet, coral spectrum is used exactly once on the center
 * hero beam. No motion, no hooks, pure server component.
 */

export const metadata: Metadata = {
  title: "Agentic Coding: Tool Calls in Flight, Governed End to End",
  description:
    "Agentic coding on a GraphQL MCP server. Published operations become tools you author in repo, validate in CI, stage behind an approval gate, and trace.",
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
    title: "Tool Calls in Flight, Governed End to End",
    description:
      "Your GraphQL server is already an MCP server. Author agent tools in repo, validate them in CI, stage with an approval gate, and trace every call.",
  },
};

const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

/** Used exactly once: the wider center beam in the hero band. */
const SPECTRUM =
  "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.10) 6%, rgba(124,146,198,0.09) 38%, rgba(240,120,106,0.06) 60%, rgba(240,120,106,0) 78%)";

/* ------------------------------------------------------------------ *
 * Background beam layer
 * ------------------------------------------------------------------ */

interface BeamSpec {
  readonly left: string;
  readonly width: string;
  readonly background: string;
}

const BEAMS: readonly BeamSpec[] = [
  {
    left: "8%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(94,234,212,0) 0%, rgba(94,234,212,0.10) 8%, rgba(94,234,212,0.04) 40%, transparent 75%)",
  },
  {
    left: "24%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.10) 8%, rgba(22,185,228,0.05) 40%, transparent 75%)",
  },
  {
    left: "50%",
    width: "2.5px",
    background: SPECTRUM,
  },
  {
    left: "72%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.10) 8%, rgba(22,185,228,0.05) 40%, transparent 75%)",
  },
  {
    left: "92%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(94,234,212,0) 0%, rgba(94,234,212,0.10) 8%, rgba(94,234,212,0.04) 40%, transparent 75%)",
  },
];

function BeamLayer() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 -z-0 overflow-hidden"
    >
      {BEAMS.map((beam) => (
        <div
          key={beam.left}
          className="absolute top-0 h-full"
          style={{
            left: beam.left,
            width: beam.width,
            background: beam.background,
          }}
        />
      ))}
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Shared parts
 * ------------------------------------------------------------------ */

/** A 24px vertical violet line that sits above an eyebrow, echoing the bg beams. */
function BeamStub() {
  return (
    <span
      aria-hidden="true"
      className="mb-3 block h-6 w-px"
      style={{
        background:
          "linear-gradient(180deg, rgba(124,146,198,0) 0%, rgba(124,146,198,0.85) 100%)",
      }}
    />
  );
}

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface SectionHeadProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function SectionHead({ eyebrow, title, children }: SectionHeadProps) {
  return (
    <div className="max-w-2xl">
      <BeamStub />
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
        {title}
      </h2>
      {children ? (
        <div className="text-cc-ink mt-5 space-y-4 text-base/relaxed text-pretty">
          {children}
        </div>
      ) : null}
    </div>
  );
}

type Hint = "idempotent" | "read-only" | "open-world" | "destructive";

interface HintBadgeProps {
  readonly hint: Hint;
}

function HintBadge({ hint }: HintBadgeProps) {
  const label =
    hint === "idempotent"
      ? "idempotentHint"
      : hint === "read-only"
        ? "readOnlyHint"
        : hint === "open-world"
          ? "openWorldHint"
          : "destructiveHint";

  if (hint === "destructive") {
    return (
      <span
        className="rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.06em] whitespace-nowrap"
        style={{
          color: CORAL,
          borderColor: "rgba(240,120,106,0.45)",
          backgroundColor: "rgba(240,120,106,0.08)",
        }}
      >
        {label}
      </span>
    );
  }

  return (
    <span className="border-cc-card-border text-cc-ink-dim bg-cc-surface rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.06em] whitespace-nowrap">
      {label}
    </span>
  );
}

/* ------------------------------------------------------------------ *
 * Hero artifact: tool call descending through 4 lanes
 * ------------------------------------------------------------------ */

interface LaneRowProps {
  readonly label: string;
  readonly tag: string;
  readonly children: ReactNode;
  readonly last?: boolean;
}

function LaneRow({ label, tag, children, last }: LaneRowProps) {
  return (
    <div
      className={`relative flex items-center gap-4 px-5 py-4 sm:px-6 ${last ? "" : "border-cc-card-border border-b"}`}
    >
      {/* Beam stub anchored to the lane */}
      <span
        aria-hidden="true"
        className="absolute top-0 left-5 h-full w-px sm:left-6"
        style={{
          background:
            "linear-gradient(180deg, rgba(124,146,198,0.55) 0%, rgba(124,146,198,0.18) 100%)",
        }}
      />
      <div className="w-24 shrink-0 pl-3">
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.16em] uppercase">
          {label}
        </span>
      </div>
      <div className="text-cc-ink min-w-0 flex-1 font-mono text-[0.75rem]">
        {children}
      </div>
      <span
        className="text-cc-ink-dim hidden font-mono text-[0.6rem] tracking-[0.1em] uppercase sm:block"
        style={{ color: VIOLET }}
      >
        {tag}
      </span>
    </div>
  );
}

function AgentSessionCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-sm">
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-5 py-3">
        <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-wide">
          agent session · one tool call in flight
        </span>
        <span
          className="font-mono text-[0.55rem] tracking-[0.14em] uppercase"
          style={{ color: VIOLET }}
        >
          /graphql/mcp
        </span>
      </div>

      <LaneRow label="01 registry" tag="grounded">
        <span className="text-cc-ink-dim">match</span>{" "}
        <span className="text-cc-heading">tagProduct</span>{" "}
        <span className="text-cc-ink-dim">in 38 published ops</span>
      </LaneRow>
      <LaneRow label="02 mcp" tag="resolved">
        <span className="text-cc-ink-dim">call</span>{" "}
        <span className="text-cc-heading">
          mcp.call tagProduct {'{ id: 42, add: ["sale"] }'}
        </span>
      </LaneRow>
      <LaneRow label="03 approval" tag="granted">
        <span className="inline-flex items-center gap-2">
          <span
            className="inline-flex h-4 w-4 items-center justify-center rounded-full"
            style={{ border: `1px solid ${VIOLET}`, color: VIOLET }}
          >
            <CheckIcon size={9} />
          </span>
          <span className="text-cc-ink">
            approval gate · pending then granted
          </span>
        </span>
      </LaneRow>
      <LaneRow label="04 trace" tag="observed" last>
        <span className="text-cc-ink-dim">nitro · p95</span>{" "}
        <span className="text-cc-heading">42 ms</span>{" "}
        <span className="text-cc-ink-dim">· error rate 0%</span>
      </LaneRow>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Grounding rail: 5 operations as horizontal mono rows with left beam
 * ------------------------------------------------------------------ */

interface OpRowProps {
  readonly name: string;
  readonly summary: string;
  readonly hint: Hint;
}

function OpRow({ name, summary, hint }: OpRowProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover relative flex items-center gap-3 rounded-xl border px-4 py-3 pl-5 transition-colors hover:-translate-y-px">
      <span
        aria-hidden="true"
        className="absolute top-3 bottom-3 left-2 w-px"
        style={{
          background:
            "linear-gradient(180deg, rgba(124,146,198,0.6) 0%, rgba(124,146,198,0.15) 100%)",
        }}
      />
      <div className="min-w-0 flex-1">
        <p className="text-cc-ink truncate font-mono text-xs">{name}</p>
        <p className="text-cc-ink-faint mt-0.5 truncate text-[0.7rem]">
          {summary}
        </p>
      </div>
      <HintBadge hint={hint} />
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * MCP hub beam diagram
 * ------------------------------------------------------------------ */

const HUB_OPS: readonly string[] = [
  "getProduct",
  "searchOrders",
  "tagProduct",
  "deleteReview",
  "openTicket",
];

function McpHubBeams() {
  const width = 720;
  const height = 280;
  const top = 36;
  const barY = 226;
  const labelY = 22;
  const lanes = HUB_OPS.length;
  // Even spacing across the inner viewport with 10% margins
  const innerLeft = 0.1 * width;
  const innerRight = 0.9 * width;
  const step = (innerRight - innerLeft) / (lanes - 1);

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      className="h-auto w-full"
      role="img"
      aria-label="Five operations descending as vertical beams into one /graphql/mcp bar."
    >
      <defs>
        <linearGradient id="v8-beam" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={VIOLET} stopOpacity="0.05" />
          <stop offset="40%" stopColor={VIOLET} stopOpacity="0.55" />
          <stop offset="100%" stopColor={VIOLET} stopOpacity="0.9" />
        </linearGradient>
        <linearGradient id="v8-bar" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor={VIOLET} stopOpacity="0.35" />
          <stop offset="50%" stopColor={VIOLET} stopOpacity="0.85" />
          <stop offset="100%" stopColor={VIOLET} stopOpacity="0.35" />
        </linearGradient>
      </defs>

      {HUB_OPS.map((label, i) => {
        const x = innerLeft + i * step;
        return (
          <g key={label}>
            <text
              x={x}
              y={labelY}
              textAnchor="middle"
              className="font-mono"
              fontSize="11"
              fill="#a1a3af"
            >
              {label}
            </text>
            <rect
              x={x - 0.75}
              y={top}
              width={1.5}
              height={barY - top}
              fill="url(#v8-beam)"
            />
          </g>
        );
      })}

      {/* The /graphql/mcp bar */}
      <rect
        x={innerLeft - 24}
        y={barY}
        width={innerRight - innerLeft + 48}
        height={28}
        rx={6}
        fill="#0c1322"
        stroke={VIOLET}
        strokeOpacity={0.5}
      />
      <rect
        x={innerLeft - 24}
        y={barY}
        width={innerRight - innerLeft + 48}
        height={2}
        fill="url(#v8-bar)"
      />
      <text
        x={width / 2}
        y={barY + 18}
        textAnchor="middle"
        className="font-mono"
        fontSize="12"
        fill="#f5f0ea"
      >
        /graphql/mcp
      </text>
    </svg>
  );
}

/* ------------------------------------------------------------------ *
 * Behavior annotation cards
 * ------------------------------------------------------------------ */

interface BehaviorCardProps {
  readonly hint: Hint;
  readonly title: string;
  readonly body: string;
}

function BehaviorCard({ hint, title, body }: BehaviorCardProps) {
  const isDestructive = hint === "destructive";
  const borderColor = isDestructive ? "rgba(240,120,106,0.45)" : undefined;
  const style: CSSProperties | undefined = isDestructive
    ? { borderColor }
    : undefined;
  const beamColor = isDestructive ? CORAL : VIOLET;
  return (
    <div
      className={`bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm transition-all hover:-translate-y-0.5 ${isDestructive ? "" : "border-cc-card-border hover:border-cc-card-border-hover"}`}
      style={style}
    >
      <svg
        aria-hidden="true"
        viewBox="0 0 24 24"
        width="14"
        height="24"
        className="mb-3"
      >
        <defs>
          <linearGradient id={`v8-stub-${hint}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={beamColor} stopOpacity="0.1" />
            <stop offset="100%" stopColor={beamColor} stopOpacity="0.95" />
          </linearGradient>
        </defs>
        <rect
          x="11"
          y="0"
          width="2"
          height="24"
          fill={`url(#v8-stub-${hint})`}
        />
      </svg>
      <HintBadge hint={hint} />
      <p className="font-heading text-cc-heading text-h6 mt-3 font-semibold">
        {title}
      </p>
      <p className="text-cc-ink-dim mt-2 text-sm/relaxed">{body}</p>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Lifecycle cards: vertical violet beam down the left edge
 * ------------------------------------------------------------------ */

interface LifecycleStep {
  readonly key: string;
  readonly index: string;
  readonly title: string;
  readonly note: string;
}

const LIFECYCLE: readonly LifecycleStep[] = [
  {
    key: "author",
    index: "01",
    title: "Author",
    note: "in repo · .graphql + settings",
  },
  {
    key: "validate",
    index: "02",
    title: "Validate",
    note: "in CI · nitro mcp validate",
  },
  {
    key: "stage",
    index: "03",
    title: "Stage",
    note: "promote with approval gate",
  },
  { key: "trace", index: "04", title: "Trace", note: "per-tool p95 in Nitro" },
];

function LifecycleCard({ step }: { readonly step: LifecycleStep }) {
  return (
    <li className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative overflow-hidden rounded-2xl border px-5 py-5 pl-6 backdrop-blur-sm transition-colors">
      <span
        aria-hidden="true"
        className="absolute top-3 bottom-3 left-2 w-px"
        style={{
          background:
            "linear-gradient(180deg, rgba(124,146,198,0.85) 0%, rgba(124,146,198,0.18) 100%)",
        }}
      />
      <span
        className="font-mono text-[0.62rem] tracking-[0.14em] uppercase"
        style={{ color: VIOLET }}
      >
        {step.index}
      </span>
      <p className="font-heading text-cc-heading text-h6 mt-3 font-semibold">
        {step.title}
      </p>
      <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.68rem] leading-relaxed">
        {step.note}
      </p>
    </li>
  );
}

/* ------------------------------------------------------------------ *
 * skillz tiles
 * ------------------------------------------------------------------ */

interface SkillTileProps {
  readonly name: string;
  readonly body: string;
}

const SKILL_TILES: readonly SkillTileProps[] = [
  {
    name: "pagination.SKILL.md",
    body: "Always page list fields with the registry connection contract.",
  },
  {
    name: "errors.SKILL.md",
    body: "Model failures as typed union results, never thrown exceptions.",
  },
  {
    name: "naming.SKILL.md",
    body: "Mutation inputs and payloads follow the team naming rules.",
  },
  {
    name: "auth.SKILL.md",
    body: "Gate fields with the shared policy directives, not ad-hoc checks.",
  },
];

function SkillTile({ name, body }: SkillTileProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-2xl border p-5 backdrop-blur-sm transition-transform duration-200 hover:-translate-y-1">
      <p className="font-mono text-[0.72rem]" style={{ color: VIOLET }}>
        {name}
      </p>
      <p className="text-cc-ink-dim mt-2 text-sm/relaxed">{body}</p>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Page
 * ------------------------------------------------------------------ */

const HONESTY_POINTS: readonly string[] = [
  "Tools and prompts are authored in the repo as reviewed code, not minted at runtime.",
  "nitro mcp validate runs in CI, so a broken tool collection never reaches a stage.",
  "Behavior is declared with idempotentHint, destructiveHint, and openWorldHint.",
  "The registry tells you which published clients are affected by a change.",
  "Every tool call is traced in Nitro with p95 latency and error rate.",
];

export default function AgenticCodingPreviewV8() {
  return (
    <div className="relative">
      <BeamLayer />

      {/* ---------------------------------------------------------- *
       * Hero
       * ---------------------------------------------------------- */}
      <section className="relative z-10 py-20">
        <div className="mx-auto max-w-6xl">
          <div className="mx-auto max-w-3xl text-center">
            <BeamStub />
            <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
              Agentic Coding · GraphQL MCP
            </p>
            <h1 className="font-heading text-cc-heading mt-6 text-4xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
              Tool calls in flight, governed end to end.
            </h1>
            <p className="lead text-cc-ink-dim mt-6 text-pretty">
              Your GraphQL server is already an MCP server. Published operations
              become governed tools an agent can call with real product context,
              authored in repo, validated in CI, staged with an approval gate,
              and traced in production.
            </p>
            <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/nitro/apis/client-registry">
                Read the Docs
              </OutlineButton>
            </div>
          </div>

          <div className="mt-14">
            <AgentSessionCard />
            <p className="text-cc-ink-faint mt-3 text-center font-mono text-[0.62rem] tracking-wide">
              one call descending through registry, MCP, approval, and trace
            </p>
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Grounding rail
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative z-10 border-t py-20">
        <div className="mx-auto grid max-w-6xl items-start gap-10 lg:grid-cols-[1fr_1.05fr] lg:gap-14">
          <SectionHead
            eyebrow="Grounding"
            title="Agents call your schema, not their guesses."
          >
            <p>
              A coding agent that does not know your graph invents fields and
              writes queries no client would ship. The schema and client
              registry change that: your published operations become a catalog
              of callable tools, each one a real, reviewed shape your product
              already depends on.
            </p>
            <p>
              MCP exposes those operations as tools with behavior annotations,
              so the agent can tell a safe read from a write before it acts, and
              you keep authority over what it is allowed to do.
            </p>
          </SectionHead>

          <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
            <div className="flex items-center justify-between">
              <Eyebrow>Published operations</Eyebrow>
              <span className="text-cc-ink-faint font-mono text-[0.6rem]">
                38 in registry
              </span>
            </div>
            <div className="mt-4 space-y-2.5">
              <OpRow
                name="getProduct"
                summary="query · single product by id"
                hint="read-only"
              />
              <OpRow
                name="searchOrders"
                summary="query · filtered order list"
                hint="idempotent"
              />
              <OpRow
                name="tagProduct"
                summary="mutation · upsert product tags"
                hint="idempotent"
              />
              <OpRow
                name="deleteReview"
                summary="mutation · remove a review"
                hint="destructive"
              />
              <OpRow
                name="openTicket"
                summary="mutation · calls an external desk"
                hint="open-world"
              />
            </div>
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * MCP hub beam
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative z-10 border-t py-20">
        <div className="mx-auto max-w-6xl">
          <div className="mx-auto max-w-3xl text-center">
            <span
              className="mx-auto mb-3 block h-6 w-px"
              aria-hidden="true"
              style={{
                background:
                  "linear-gradient(180deg, rgba(124,146,198,0) 0%, rgba(124,146,198,0.85) 100%)",
              }}
            />
            <Eyebrow>One endpoint</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
              Every operation lands at{" "}
              <code className="text-cc-heading font-mono">/graphql/mcp</code>.
            </h2>
            <p className="text-cc-ink mt-5 text-base/relaxed text-pretty">
              No second surface to secure, no parallel tool definitions to
              drift. The same schema and registry that run your API ground the
              agent, over a single Streamable HTTP endpoint.
            </p>
          </div>

          <div className="border-cc-card-border bg-cc-card-bg mt-12 rounded-3xl border p-6 backdrop-blur-sm sm:p-10">
            <McpHubBeams />
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Behavior annotations
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative z-10 border-t py-20">
        <div className="mx-auto max-w-6xl">
          <SectionHead
            eyebrow="Behavior annotations"
            title="Each tool declares how it behaves."
          >
            <p>
              MCP carries behavior hints alongside each tool. Agents and humans
              both read them, and reviewers can tell a safe read from a
              destructive write at a glance.
            </p>
          </SectionHead>

          <div className="mt-10 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <BehaviorCard
              hint="idempotent"
              title="Safe to retry."
              body="Calling the same input twice produces the same effect, so the agent can retry on a transient error without doubling the work."
            />
            <BehaviorCard
              hint="destructive"
              title="Needs an approval gate."
              body="The tool removes or replaces state. Stage promotion requires a granted approval, and the trace tags the call as destructive."
            />
            <BehaviorCard
              hint="open-world"
              title="Touches systems beyond yours."
              body="The tool reaches an external service, so its outcome is not fully owned by your graph. The trace records the outbound hop."
            />
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Governed lifecycle
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative z-10 border-t py-20">
        <div className="mx-auto max-w-6xl">
          <SectionHead
            eyebrow="Governed lifecycle"
            title="Author, validate, stage, trace."
          >
            <p>
              The point is not that we have MCP. It is that every agent tool
              moves through a lifecycle you control. Tools start as reviewed
              code, get validated before they ship, and are promoted through
              stages with approval gates, then observed in production.
            </p>
          </SectionHead>

          <ol className="mt-10 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {LIFECYCLE.map((step) => (
              <LifecycleCard key={step.key} step={step} />
            ))}
          </ol>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * skillz
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative z-10 border-t py-20">
        <div className="mx-auto grid max-w-6xl items-start gap-10 lg:grid-cols-[1fr_1.05fr] lg:gap-14">
          <SectionHead
            eyebrow="Conventions"
            title="Teach every agent your conventions once."
          >
            <p>
              skillz packages your team&rsquo;s GraphQL conventions as portable{" "}
              <code className="text-cc-info">SKILL.md</code> files, installable
              across the agents your team already uses, so the next pull request
              looks like your codebase, not a generic one.
            </p>
          </SectionHead>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {SKILL_TILES.map((tile) => (
              <SkillTile key={tile.name} name={tile.name} body={tile.body} />
            ))}
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Honesty beat
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative z-10 border-t py-20">
        <div className="mx-auto max-w-3xl text-center">
          <span
            className="mx-auto mb-3 block h-6 w-px"
            aria-hidden="true"
            style={{
              background:
                "linear-gradient(180deg, rgba(124,146,198,0) 0%, rgba(124,146,198,0.85) 100%)",
            }}
          />
          <Eyebrow>What we actually claim</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
            Honest about every claim we make.
          </h2>
          <p className="text-cc-ink mt-5 text-base/relaxed text-pretty">
            We do not promise to name every client that breaks or to mint safe
            tools at runtime. We promise a governed, observed path.
          </p>

          <ul className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-2xl space-y-4 rounded-3xl border p-7 text-left backdrop-blur-sm">
            {HONESTY_POINTS.map((point) => (
              <li key={point} className="flex items-start gap-3">
                <span className="mt-0.5 shrink-0" style={{ color: VIOLET }}>
                  <CheckIcon />
                </span>
                <span className="text-cc-ink text-sm/relaxed text-pretty">
                  {point}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Closing CTA
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative z-10 border-t py-20 text-center">
        <div className="mx-auto max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 mx-auto leading-tight font-semibold text-balance">
            Put agents on a loop you can trace.
          </h2>
          <p className="text-cc-ink-dim mx-auto mt-5 text-base/relaxed">
            Expose your operations as governed tools, ground them in real field
            demand, and trace every call in the platform your team already runs.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>
          <p className="text-cc-ink-dim mt-6 font-mono text-[0.7rem] tracking-[0.14em]">
            <Link
              href="/platform"
              className="text-cc-info hover:text-cc-heading transition-colors"
            >
              /platform
            </Link>
          </p>
        </div>
      </section>
    </div>
  );
}
