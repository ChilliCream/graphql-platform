import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Preview variant (v1) of the Agentic coding page. Product-UI craft stance
 * (Linear / Vercel Observability): an identity-first hero centered on a
 * believable agent-terminal mock with a scripted PENDING -> GRANTED approval
 * gate, then prose alternating with one focused artifact each (tool catalog,
 * inward-converging MCP hub, the author -> validate -> stage -> trace lifecycle
 * strip, and a skillz bento).
 *
 * Scene accent is violet (#7c92c6 / cc-info) for agency + governance; coral
 * (#f0786a) is reserved strictly for the single "destructive" tool annotation.
 *
 * All motion is pure CSS keyframes gated behind prefers-reduced-motion, so the
 * file stays a Server Component and can export `metadata` (required for the
 * static SEO + robots no-index export below).
 */

export const metadata: Metadata = {
  title: "Agentic Coding: A Governed Feedback Loop for Agents",
  description:
    "Give coding agents a feedback loop. Your GraphQL server is already an MCP server: published operations become governed tools you author in repo, validate in CI, stage, and trace.",
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
    title: "Give Coding Agents a Feedback Loop",
    description:
      "Your GraphQL server is already an MCP server. Author agent tools in repo, validate them in CI, stage, and trace every call, so risky edits come back as feedback.",
  },
};

const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

/** One spectrum gradient is permitted per screen; used once, in the hero lead. */
const SPECTRUM = "linear-gradient(100deg,#16b9e4 0%,#7c92c6 50%,#f0786a 100%)";

/* ------------------------------------------------------------------ *
 * Scoped animation styles (server-rendered, reduced-motion aware)
 * ------------------------------------------------------------------ */

/**
 * All keyframes live in one inline stylesheet so the page is a pure server
 * component. Every animation is disabled under prefers-reduced-motion, which
 * lands every element in its resolved final frame.
 */
const ANIMATION_CSS = `
@keyframes acv1-rise {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
@keyframes acv1-type {
  from { width: 0; }
  to { width: 100%; }
}
@keyframes acv1-caret {
  0%, 49% { opacity: 1; }
  50%, 100% { opacity: 0; }
}
@keyframes acv1-sink {
  from { transform: translateY(-2px); }
  to { transform: translateY(0); }
}
@keyframes acv1-grant-color {
  to { color: ${VIOLET}; }
}
@keyframes acv1-spoke {
  0%, 100% { stroke-opacity: 0.2; }
  50% { stroke-opacity: 0.75; }
}
.acv1-line { opacity: 0; animation: acv1-rise 0.5s ease-out forwards; }
.acv1-d1 { animation-delay: 0.2s; }
.acv1-d2 { animation-delay: 1.1s; }
.acv1-d3 { animation-delay: 2s; }
.acv1-typewrap {
  display: inline-block;
  overflow: hidden;
  white-space: nowrap;
  vertical-align: bottom;
  width: 100%;
  animation: acv1-type 0.9s steps(38) 1.1s both;
}
.acv1-caret {
  display: inline-block;
  width: 6px;
  height: 12px;
  margin-left: 2px;
  background: ${VIOLET};
  vertical-align: middle;
  animation: acv1-caret 0.9s steps(1) infinite;
}
.acv1-gate {
  opacity: 0;
  animation:
    acv1-rise 0.5s ease-out 2s forwards,
    acv1-sink 0.5s ease-out 3.4s both;
}
.acv1-gate-status { animation: acv1-grant-color 0.1s linear 3.4s forwards; }
.acv1-diff { opacity: 0; animation: acv1-rise 0.5s ease-out 3.7s forwards; }
.acv1-reveal { opacity: 0; animation: acv1-rise 0.7s ease-out forwards; }
.acv1-spoke { animation: acv1-spoke 2.8s ease-in-out infinite; }
@media (prefers-reduced-motion: reduce) {
  .acv1-line,
  .acv1-gate,
  .acv1-diff,
  .acv1-reveal {
    opacity: 1 !important;
    animation: none !important;
    transform: none !important;
  }
  .acv1-typewrap {
    animation: none !important;
    width: auto !important;
  }
  .acv1-caret {
    display: none !important;
  }
  .acv1-gate-status {
    color: ${VIOLET} !important;
    animation: none !important;
  }
  .acv1-spoke {
    animation: none !important;
    stroke-opacity: 0.55 !important;
  }
}
`;

/* ------------------------------------------------------------------ *
 * Shared small parts
 * ------------------------------------------------------------------ */

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

interface SectionHeadingProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

/** Left-aligned section header: eyebrow + heading + optional intro prose. */
function SectionHeading({ eyebrow, title, children }: SectionHeadingProps) {
  return (
    <div className="max-w-2xl">
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

/** Behavior-annotation badge. The destructive variant is the only coral. */
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

/** Faux window chrome dots for the product mocks. */
function WindowDots() {
  return (
    <div className="flex items-center gap-1.5">
      <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
      <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
      <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Hero artifact: the agent terminal with a scripted approval gate
 * ------------------------------------------------------------------ */

function AgentTerminal() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg w-full overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-md">
      {/* Window header */}
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2.5">
        <WindowDots />
        <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-wide">
          agent session · /graphql/mcp
        </span>
        <span
          className="font-mono text-[0.55rem] tracking-[0.1em] uppercase"
          style={{ color: VIOLET }}
        >
          governed
        </span>
      </div>

      {/* Transcript body */}
      <div className="bg-cc-code-bg space-y-2.5 px-4 py-4 font-mono text-[0.72rem] leading-relaxed sm:px-5 sm:py-5">
        <p className="acv1-line acv1-d1 text-cc-ink-dim">
          <span style={{ color: VIOLET }}>agent</span> grounded in client
          registry · 38 published operations
        </p>

        <p className="acv1-line acv1-d2 text-cc-ink">
          <span className="text-cc-ink-faint">&rsaquo;</span>{" "}
          <span className="acv1-typewrap">
            {'mcp.call updateProductTags { id: 42, add: ["sale"] }'}
          </span>
          <span className="acv1-caret" aria-hidden="true" />
        </p>

        <p className="acv1-line acv1-d3 text-cc-ink-dim">
          resolves to{" "}
          <span className="text-cc-ink">mutation UpdateProductTags</span> ·
          annotated <span style={{ color: CORAL }}>destructiveHint</span>
        </p>

        {/* Approval gate row: sinks once granted, status flips to violet. */}
        <div
          className="acv1-gate flex items-center justify-between gap-3 rounded-lg border px-3 py-2.5"
          style={{
            borderColor: "rgba(124,146,198,0.45)",
            backgroundColor: "rgba(124,146,198,0.09)",
          }}
        >
          <span className="flex items-center gap-2">
            <span
              className="inline-flex h-4 w-4 items-center justify-center rounded-full"
              style={{ border: `1px solid ${VIOLET}`, color: VIOLET }}
            >
              <CheckIcon size={9} />
            </span>
            <span className="text-cc-ink">approval gate</span>
          </span>
          <span className="acv1-gate-status text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.12em] uppercase">
            pending&nbsp;&rarr;&nbsp;granted
          </span>
        </div>

        {/* Safe-patch diff: only fades in after the gate grants. */}
        <div className="acv1-diff border-cc-card-border bg-cc-surface rounded-lg border">
          <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-3 py-1.5 text-[0.6rem]">
            <span>ProductCard.graphql</span>
            <span style={{ color: VIOLET }}>safe patch</span>
          </div>
          <pre className="overflow-x-auto px-3 py-2.5 text-[0.68rem] leading-relaxed">
            <span className="text-cc-ink-dim">{"  query ProductCard {"}</span>
            {"\n"}
            <span className="text-cc-ink-dim">{"    product(id: 42) {"}</span>
            {"\n"}
            <span className="text-cc-ink-dim">{"      id name"}</span>
            {"\n"}
            <span style={{ color: "#5eead4" }}>{"+     tags"}</span>
            {"\n"}
            <span className="text-cc-ink-dim">{"    }"}</span>
            {"\n"}
            <span className="text-cc-ink-dim">{"  }"}</span>
          </pre>
        </div>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Tool catalog rail
 * ------------------------------------------------------------------ */

interface ToolCardProps {
  readonly name: string;
  readonly summary: string;
  readonly hint: Hint;
}

function ToolCard({ name, summary, hint }: ToolCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover flex items-center gap-3 rounded-xl border px-3.5 py-3 transition-colors">
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
 * Inward-converging MCP hub diagram
 * ------------------------------------------------------------------ */

interface HubSpoke {
  readonly label: string;
  readonly hint: Hint;
}

const HUB_SPOKES: readonly HubSpoke[] = [
  { label: "getProduct", hint: "read-only" },
  { label: "searchOrders", hint: "idempotent" },
  { label: "deleteReview", hint: "destructive" },
  { label: "listSkills", hint: "read-only" },
  { label: "tagProduct", hint: "idempotent" },
  { label: "openTicket", hint: "open-world" },
];

function McpHub() {
  // Lay spokes evenly around a circle; arrows point INWARD to the core.
  const cx = 200;
  const cy = 160;
  const radius = 128;
  const coreR = 38;

  const points = HUB_SPOKES.map((spoke, i) => {
    const angle = (i / HUB_SPOKES.length) * Math.PI * 2 - Math.PI / 2;
    const x = cx + radius * Math.cos(angle);
    const y = cy + radius * Math.sin(angle);
    // Line stops short of the core so the arrowhead reads cleanly.
    const tx = cx + (coreR + 6) * Math.cos(angle);
    const ty = cy + (coreR + 6) * Math.sin(angle);
    return { spoke, x, y, tx, ty, i };
  });

  return (
    <svg
      viewBox="0 0 400 320"
      className="h-auto w-full"
      role="img"
      aria-label="Published operations converging inward as tools into one /graphql/mcp hub."
    >
      <defs>
        <marker
          id="acv1-hub-arrow"
          viewBox="0 0 10 10"
          refX="8"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M0 0 L10 5 L0 10 z" fill={VIOLET} />
        </marker>
        <radialGradient id="acv1-hub-core" cx="50%" cy="50%" r="60%">
          <stop offset="0%" stopColor="rgba(124,146,198,0.35)" />
          <stop offset="100%" stopColor="rgba(124,146,198,0.05)" />
        </radialGradient>
      </defs>

      {/* Inward spokes + node chips */}
      {points.map(({ spoke, x, y, tx, ty, i }) => {
        const isDestructive = spoke.hint === "destructive";
        const stroke = isDestructive ? CORAL : VIOLET;
        return (
          <g key={spoke.label}>
            <line
              x1={x}
              y1={y}
              x2={tx}
              y2={ty}
              stroke={stroke}
              strokeOpacity={0.5}
              strokeWidth={1.25}
              markerEnd="url(#acv1-hub-arrow)"
              className="acv1-spoke"
              style={{ animationDelay: `${i * 0.35}s` }}
            />
            <rect
              x={x - 44}
              y={y - 11}
              width={88}
              height={22}
              rx={6}
              fill="#0c1322"
              stroke={
                isDestructive
                  ? "rgba(240,120,106,0.4)"
                  : "rgba(245,241,234,0.12)"
              }
            />
            <text
              x={x}
              y={y + 3.5}
              textAnchor="middle"
              className="font-mono"
              fontSize="9"
              fill={isDestructive ? CORAL : "#a1a3af"}
            >
              {spoke.label}
            </text>
          </g>
        );
      })}

      {/* Core hub node */}
      <circle cx={cx} cy={cy} r={coreR + 10} fill="url(#acv1-hub-core)" />
      <circle
        cx={cx}
        cy={cy}
        r={coreR}
        fill="#0c1322"
        stroke={VIOLET}
        strokeWidth={1.5}
      />
      <text
        x={cx}
        y={cy - 2}
        textAnchor="middle"
        className="font-mono"
        fontSize="10"
        fill="#f5f0ea"
      >
        /graphql
      </text>
      <text
        x={cx}
        y={cy + 11}
        textAnchor="middle"
        className="font-mono"
        fontSize="10"
        fill={VIOLET}
      >
        /mcp
      </text>
    </svg>
  );
}

/* ------------------------------------------------------------------ *
 * Lifecycle strip: author -> validate -> stage -> trace
 * ------------------------------------------------------------------ */

interface LifecycleStep {
  readonly key: string;
  readonly title: string;
  readonly note: string;
}

const LIFECYCLE: readonly LifecycleStep[] = [
  { key: "author", title: "Author", note: "in repo · .graphql + settings" },
  { key: "validate", title: "Validate", note: "in CI · nitro mcp validate" },
  { key: "stage", title: "Stage", note: "promote with approval gate" },
  { key: "trace", title: "Trace", note: "per-tool p95 in Nitro" },
];

function LifecycleStrip() {
  return (
    <ol className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {LIFECYCLE.map((item, index) => (
        <li
          key={item.key}
          className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border px-5 py-5 backdrop-blur-sm"
        >
          <div className="flex items-center justify-between">
            <span
              className="font-mono text-[0.62rem] tracking-[0.14em] uppercase"
              style={{ color: VIOLET }}
            >
              0{index + 1}
            </span>
            {index < LIFECYCLE.length - 1 && (
              <span
                aria-hidden="true"
                className="text-cc-ink-faint hidden text-sm lg:block"
              >
                &rarr;
              </span>
            )}
          </div>
          <p className="font-heading text-cc-heading text-h6 mt-3 font-semibold">
            {item.title}
          </p>
          <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.68rem] leading-relaxed">
            {item.note}
          </p>
        </li>
      ))}
    </ol>
  );
}

/* ------------------------------------------------------------------ *
 * skillz bento
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
  "An edit is checked against published operations; risky changes read “published clients affected.”",
  "Every tool call is traced in Nitro with p95 latency, error rate, and impact.",
];

export default function AgenticCodingPreviewV1() {
  return (
    <>
      <style>{ANIMATION_CSS}</style>

      {/* ---------------------------------------------------------- *
       * Hero: identity-first, left text + dual CTA / right product mock
       * ---------------------------------------------------------- */}
      <section className="grid items-center gap-10 py-12 sm:py-16 lg:grid-cols-[1.05fr_1fr] lg:gap-14">
        <div>
          <span
            className="inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[0.62rem] tracking-[0.16em] uppercase"
            style={{
              color: VIOLET,
              borderColor: "rgba(124,146,198,0.4)",
              backgroundColor: "rgba(124,146,198,0.07)",
            }}
          >
            <span
              className="h-1.5 w-1.5 rounded-full"
              style={{ backgroundColor: VIOLET }}
            />
            Agentic coding · preview
          </span>

          <h1 className="font-heading text-cc-heading mt-6 text-4xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
            Give coding agents a feedback loop.
          </h1>

          <p className="lead text-cc-ink-dim mt-6 max-w-xl text-pretty">
            Stop letting agents guess at your graph. Ground them in the
            operations your clients already use, gate the risky calls, and turn
            every fast edit into a{" "}
            <span
              className="bg-clip-text font-medium text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              governed feedback loop
            </span>
            .
          </p>

          <p className="text-cc-ink mt-5 max-w-xl text-base/relaxed text-pretty">
            Your GraphQL server is already an MCP server. Published operations
            become tools an agent can call with real product context, each one
            authored, validated, and traced before it ever touches production.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>
        </div>

        <div className="lg:pl-4">
          <AgentTerminal />
          <p className="text-cc-ink-faint mt-3 text-center font-mono text-[0.62rem] tracking-wide">
            live approval gate · safe patch lands only after the grant
          </p>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Grounding: prose + tool catalog rail
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="acv1-reveal grid items-start gap-10 lg:grid-cols-[1fr_1.05fr] lg:gap-14">
          <SectionHeading
            eyebrow="Grounding"
            title="Agents edit with product context, not guesses."
          >
            <p>
              A coding agent that does not know your graph invents fields and
              writes queries no client would ship. The schema and client
              registry change that: your published operations become a catalog
              of callable tools, each one a real, reviewed shape your product
              already depends on.
            </p>
            <p>
              MCP exposes those operations as tools and prompts with behavior
              annotations, so the agent can tell a safe read from a write before
              it acts, and you keep authority over what it is allowed to do.
            </p>
          </SectionHeading>

          <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
            <div className="flex items-center justify-between">
              <Eyebrow>Tool catalog</Eyebrow>
              <span className="text-cc-ink-faint font-mono text-[0.6rem]">
                38 published ops
              </span>
            </div>
            <div className="mt-4 space-y-2.5">
              <ToolCard
                name="getProduct"
                summary="query · single product by id"
                hint="read-only"
              />
              <ToolCard
                name="searchOrders"
                summary="query · filtered order list"
                hint="idempotent"
              />
              <ToolCard
                name="tagProduct"
                summary="mutation · upsert product tags"
                hint="idempotent"
              />
              <ToolCard
                name="deleteReview"
                summary="mutation · remove a review"
                hint="destructive"
              />
              <ToolCard
                name="openTicket"
                summary="mutation · calls an external desk"
                hint="open-world"
              />
            </div>
            <p className="text-cc-ink-faint mt-4 font-mono text-[0.6rem] leading-relaxed">
              annotations: idempotentHint · readOnlyHint · openWorldHint ·
              <span style={{ color: CORAL }}> destructiveHint</span>
            </p>
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Convergence: inward MCP hub
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="acv1-reveal grid items-center gap-10 lg:grid-cols-[1fr_1fr] lg:gap-14">
          <div className="order-2 lg:order-1">
            <div className="border-cc-card-border bg-cc-card-bg rounded-3xl border p-6 backdrop-blur-sm sm:p-8">
              <McpHub />
            </div>
          </div>

          <div className="order-1 lg:order-2">
            <SectionHeading
              eyebrow="One hub"
              title="Operations converge into one MCP endpoint."
            >
              <p>
                Every published operation flows inward to a single{" "}
                <code className="text-cc-info">/graphql/mcp</code> hub over
                Streamable HTTP. There is no second surface to secure and no
                parallel tool definitions to drift, the same schema and registry
                that run your API ground the agent.
              </p>
              <p>
                Because the schema is typed and introspectable, each tool
                carries an accurate parameter contract, so agents make fewer
                malformed calls and the destructive ones stay clearly marked.
              </p>
            </SectionHeading>
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Lifecycle strip
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="acv1-reveal">
          <SectionHeading
            eyebrow="Governed lifecycle"
            title="Author, validate, stage, trace."
          >
            <p>
              The point is not that we have MCP, it is that every agent tool
              moves through a lifecycle you control. Tools start as reviewed
              code, get validated before they ship, and are promoted through
              stages with approval gates, then observed in production.
            </p>
          </SectionHeading>
          <div className="mt-10">
            <LifecycleStrip />
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * skillz bento
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="acv1-reveal">
          <SectionHeading
            eyebrow="Conventions"
            title="Teach every agent your conventions once."
          >
            <p>
              skillz packages your team&rsquo;s GraphQL conventions as portable{" "}
              <code className="text-cc-info">SKILL.md</code> files, installable
              across the agents your team already uses, so the next pull request
              looks like your codebase, not a generic one.
            </p>
          </SectionHeading>
          <div className="mt-10 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {SKILL_TILES.map((tile) => (
              <SkillTile key={tile.name} name={tile.name} body={tile.body} />
            ))}
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Honesty beat
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="acv1-reveal grid items-start gap-10 lg:grid-cols-[1fr_1fr] lg:gap-14">
          <SectionHeading
            eyebrow="What we actually claim"
            title="We say what the registries can prove."
          >
            <p>
              Honesty is the differentiator. We do not promise to name every
              client that breaks or to mint safe tools at runtime. We promise a
              governed, observed path: authored in repo, validated in CI, staged
              with a gate, and traced in production.
            </p>
          </SectionHeading>

          <ul className="border-cc-card-border bg-cc-card-bg space-y-4 rounded-3xl border p-7 backdrop-blur-sm">
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
      <section className="border-cc-card-border border-t py-16 text-center">
        <h2 className="font-heading text-cc-heading text-h3 mx-auto max-w-2xl leading-tight font-semibold text-balance">
          Put your agents on a loop you control.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-base/relaxed">
          Expose your operations as governed tools, ground them in real field
          demand, and trace every call in the platform your team already runs.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs/nitro/apis/client-registry">
            Read the Docs
          </OutlineButton>
        </div>
        <p className="text-cc-ink-dim mt-6 text-sm">
          Or explore the{" "}
          <Link
            href="/docs/nitro/apis/client-registry"
            className="text-cc-info hover:text-cc-heading transition-colors"
          >
            client registry
          </Link>{" "}
          and the wider{" "}
          <Link
            href="/platform"
            className="text-cc-info hover:text-cc-heading transition-colors"
          >
            platform
          </Link>
          .
        </p>
      </section>
    </>
  );
}
