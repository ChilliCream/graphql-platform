import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Agentic Coding Feedback Loop | GraphQL MCP",
  description:
    "Give coding agents a governed feedback loop. Your .NET GraphQL server is already an MCP server: published operations become annotated tools, validated in CI and traced.",
  keywords: [
    "agentic coding feedback loop",
    "GraphQL MCP server for coding agents",
    "agent tool lifecycle governance",
    "behavior annotation idempotentHint destructiveHint",
    "published operations as MCP tools",
    "client registry grounding for agents",
    "validate agent tools in CI",
    "Model Context Protocol .NET",
    "skillz agent conventions",
    "approval gate for agent edits",
  ],
  openGraph: {
    title: "Give Coding Agents a Feedback Loop",
    description:
      "A governed agent-tool lifecycle: author in repo, validate in CI, stage, and trace. Your GraphQL server is already an MCP server that grounds coding agents.",
  },
  robots: { index: false, follow: false },
};

/* Scene accent for this page is violet (cc-info / #7c92c6): agency + governance.
   Coral (#f0786a) is reserved exclusively for the destructive tool annotation. */
const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

export default function AgenticCodingPreviewV3() {
  return (
    <>
      <SceneStyles />
      <Hero />
      <BentoGrid />
      <LifecycleStrip />
      <HubConverge />
      <SkillzBento />
      <HonestyBeat />
      <ClosingCta />
    </>
  );
}

/* -------------------------------------------------------------------------- */
/* Keyframes + reduced-motion guard, scoped to this page.                     */
/* -------------------------------------------------------------------------- */

function SceneStyles() {
  return (
    <style>{`
      @keyframes v3-caret {
        0%, 100% { opacity: 1 }
        50% { opacity: 0 }
      }
      @keyframes v3-line-in {
        from { opacity: 0; transform: translateY(6px) }
        to { opacity: 1; transform: translateY(0) }
      }
      @keyframes v3-gate-grant {
        0%, 55% { transform: translateY(0); opacity: 1 }
        70% { transform: translateY(7px) scale(0.985); opacity: 0.85 }
        100% { transform: translateY(0); opacity: 1 }
      }
      @keyframes v3-diff-reveal {
        0%, 70% { opacity: 0; transform: translateY(8px); max-height: 0 }
        100% { opacity: 1; transform: translateY(0); max-height: 240px }
      }
      @keyframes v3-pulse-in {
        0% { stroke-dashoffset: 0; opacity: 0.15 }
        50% { opacity: 0.9 }
        100% { stroke-dashoffset: -22; opacity: 0.15 }
      }
      @keyframes v3-core-glow {
        0%, 100% { opacity: 0.55 }
        50% { opacity: 1 }
      }
      .v3-line { opacity: 0; animation: v3-line-in 0.5s ease forwards }
      .v3-d1 { animation-delay: 0.15s }
      .v3-d2 { animation-delay: 0.55s }
      .v3-d3 { animation-delay: 1.0s }
      .v3-d4 { animation-delay: 1.5s }
      .v3-gate {
        animation: v3-line-in 0.5s ease 1.5s forwards, v3-gate-grant 2.4s ease 2.4s infinite;
        opacity: 0;
      }
      .v3-diff { animation: v3-diff-reveal 2.4s ease 2.4s infinite; overflow: hidden }
      .v3-caret {
        display: inline-block; width: 8px; height: 1.05em;
        background: ${VIOLET}; vertical-align: text-bottom; margin-left: 2px;
        animation: v3-caret 1s step-end infinite;
      }
      .v3-pulse { animation: v3-pulse-in 2.6s linear infinite }
      .v3-core { animation: v3-core-glow 2.6s ease-in-out infinite }
      @media (prefers-reduced-motion: reduce) {
        .v3-line, .v3-gate, .v3-diff, .v3-caret, .v3-pulse, .v3-core {
          animation: none !important;
          opacity: 1 !important;
          max-height: none !important;
          transform: none !important;
        }
      }
    `}</style>
  );
}

/* -------------------------------------------------------------------------- */
/* Hero: violet spotlight mesh + identity-first headline + dual CTA.          */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="relative overflow-hidden rounded-[2rem]">
      {/* Layered violet spotlight glow (scene accent), no full-page paint. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background: `radial-gradient(120% 90% at 78% 8%, ${VIOLET}33 0%, transparent 55%), radial-gradient(90% 70% at 12% 100%, ${VIOLET}1f 0%, transparent 60%)`,
        }}
      />
      <div
        aria-hidden
        className="border-cc-card-border pointer-events-none absolute inset-0 -z-10 rounded-[2rem] border opacity-60"
        style={{
          maskImage:
            "linear-gradient(to bottom, black, transparent 75%), linear-gradient(to right, transparent, black 10%, black 90%, transparent)",
          maskComposite: "intersect",
          WebkitMaskComposite: "source-in",
          backgroundImage:
            "linear-gradient(rgba(245,241,234,0.05) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,0.05) 1px, transparent 1px)",
          backgroundSize: "44px 44px",
        }}
      />

      <div className="grid items-center gap-12 px-6 py-16 sm:px-10 lg:grid-cols-[1.05fr_0.95fr] lg:py-24">
        <div>
          <span
            className="inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[0.68rem] tracking-[0.18em] uppercase"
            style={{
              color: VIOLET,
              borderColor: `${VIOLET}55`,
              background: `${VIOLET}14`,
            }}
          >
            <span
              className="v3-core inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: VIOLET }}
            />
            Agentic coding
          </span>

          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6">
            Give coding agents
            <br />a feedback loop.
          </h1>

          <p className="lead text-cc-prose mt-6 max-w-xl">
            Stop agents from guessing at your API. Ground them in the operations
            your clients already ship, gate the risky edits behind an approval
            they have to earn, and trace every tool call they make.
          </p>

          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>

          <ul className="text-cc-ink-dim text-caption mt-8 flex flex-wrap gap-x-6 gap-y-2">
            {[
              "Your server is already an MCP server",
              "Tools authored in your repo",
              "Validated in CI before traffic",
            ].map((t) => (
              <li key={t} className="flex items-center gap-2">
                <span style={{ color: VIOLET }}>
                  <CheckIcon size={13} />
                </span>
                {t}
              </li>
            ))}
          </ul>
        </div>

        {/* Hero centerpiece: the agent terminal with the approval gate. */}
        <AgentTerminal />
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* AgentTerminal: transcript types tool-call lines, then a PENDING -> GRANTED */
/* approval row, then the safe patch diff reveals only after the gate grants. */
/* -------------------------------------------------------------------------- */

function AgentTerminal() {
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border shadow-2xl backdrop-blur-md"
      style={{ boxShadow: `0 30px 80px -40px ${VIOLET}66` }}
    >
      {/* window chrome */}
      <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span className="h-2.5 w-2.5 rounded-full bg-[#f0786a]/70" />
        <span className="bg-cc-warning/60 h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-success/60 h-2.5 w-2.5 rounded-full" />
        <span className="text-cc-nav-label ml-2 font-mono text-[0.7rem] tracking-wide">
          agent · session
        </span>
        <span
          className="ml-auto rounded-md px-2 py-0.5 font-mono text-[0.6rem] tracking-[0.12em] uppercase"
          style={{ color: VIOLET, background: `${VIOLET}1f` }}
        >
          mcp connected
        </span>
      </div>

      <div className="space-y-1.5 px-4 py-4 font-mono text-[0.74rem] leading-relaxed sm:text-[0.78rem]">
        <Line className="v3-d1">
          <span className="text-cc-ink-dim">$</span>{" "}
          <span className="text-cc-ink">
            agent edit{" "}
            <span className="text-cc-prose">&quot;add loyalty tier&quot;</span>
          </span>
        </Line>
        <Line className="v3-d2">
          <span className="text-cc-nav-label">→ resolving context from</span>{" "}
          <span style={{ color: VIOLET }}>/graphql/mcp</span>
        </Line>
        <Line className="v3-d3">
          <span className="text-cc-nav-label">→ call</span>{" "}
          <span className="text-cc-heading">getCustomerProfile</span>{" "}
          <Badge tone="read">read</Badge>
        </Line>
        <Line className="v3-d4">
          <span className="text-cc-nav-label">→ call</span>{" "}
          <span className="text-cc-heading">updateLoyaltyTier</span>{" "}
          <Badge tone="destructive">destructive</Badge>
        </Line>

        {/* approval gate: PENDING -> GRANTED, sinks on grant */}
        <div className="v3-gate pt-2">
          <div
            className="flex items-center gap-3 rounded-lg border px-3 py-2.5"
            style={{
              borderColor: `${VIOLET}66`,
              background: `${VIOLET}14`,
            }}
          >
            <span
              className="grid h-7 w-7 place-items-center rounded-md"
              style={{ background: `${VIOLET}29`, color: VIOLET }}
            >
              <LockIcon />
            </span>
            <div className="min-w-0">
              <p className="text-cc-heading text-[0.72rem]">
                Approval gate · updateLoyaltyTier
              </p>
              <p className="text-cc-ink-dim text-[0.66rem]">
                destructive tool requires sign-off
              </p>
            </div>
            <span className="ml-auto flex items-center gap-2">
              <span
                className="rounded px-2 py-0.5 text-[0.6rem] tracking-[0.12em] uppercase line-through opacity-50"
                style={{ color: VIOLET }}
              >
                pending
              </span>
              <span
                className="inline-flex items-center gap-1 rounded px-2 py-0.5 text-[0.62rem] font-semibold tracking-[0.12em] uppercase"
                style={{ color: VIOLET, background: `${VIOLET}29` }}
              >
                <CheckIcon size={11} />
                granted
              </span>
            </span>
          </div>
        </div>

        {/* safe-patch diff appears only after the gate grants */}
        <div className="v3-diff pt-2">
          <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-lg border">
            <div className="border-cc-card-border text-cc-nav-label flex items-center gap-2 border-b px-3 py-1.5 text-[0.62rem]">
              <span>LoyaltyService.cs</span>
              <span className="ml-auto" style={{ color: VIOLET }}>
                safe patch
              </span>
            </div>
            <pre className="overflow-x-auto px-3 py-2 text-[0.68rem] leading-5">
              <code>
                <DiffLine kind="ctx">{"  public async Task Promote("}</DiffLine>
                <DiffLine kind="add">
                  {"+   var tier = await client.UpdateLoyaltyTier(id, next);"}
                </DiffLine>
                <DiffLine kind="add">
                  {"+   return tier.Affected; // typed result"}
                </DiffLine>
                <DiffLine kind="ctx">{"  )"}</DiffLine>
              </code>
            </pre>
          </div>
        </div>

        <div className="text-cc-nav-label flex items-center pt-1 text-[0.7rem]">
          <span className="v3-caret" />
        </div>
      </div>
    </div>
  );
}

interface LineProps {
  readonly className?: string;
  readonly children: ReactNode;
}

function Line({ className, children }: LineProps) {
  return <div className={`v3-line ${className ?? ""}`}>{children}</div>;
}

interface DiffLineProps {
  readonly kind: "add" | "ctx";
  readonly children: ReactNode;
}

function DiffLine({ kind, children }: DiffLineProps) {
  if (kind === "add") {
    return (
      <span className="block bg-[#7c92c6]/10 text-[#9db0d6]">{children}</span>
    );
  }
  return <span className="text-cc-ink-dim block">{children}</span>;
}

/* -------------------------------------------------------------------------- */
/* Behavior-annotation badge: the three REAL MCP hints.                       */
/* -------------------------------------------------------------------------- */

interface BadgeProps {
  readonly tone: "read" | "idempotent" | "destructive";
  readonly children: ReactNode;
}

function Badge({ tone, children }: BadgeProps) {
  const styles: Record<BadgeProps["tone"], { color: string; bg: string }> = {
    read: { color: "#5eead4", bg: "rgba(94,234,212,0.14)" },
    idempotent: { color: VIOLET, bg: `${VIOLET}1f` },
    destructive: { color: CORAL, bg: "rgba(240,120,106,0.16)" },
  };
  const s = styles[tone];
  return (
    <span
      className="inline-flex items-center rounded px-1.5 py-0.5 font-mono text-[0.58rem] tracking-[0.08em] uppercase"
      style={{ color: s.color, background: s.bg }}
    >
      {children}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/* BentoGrid: asymmetric tiles — tool catalog rail, oversized stat, quote.    */
/* -------------------------------------------------------------------------- */

function BentoGrid() {
  return (
    <section className="mt-24">
      <SectionLabel>The shape of a grounded agent</SectionLabel>
      <h2 className="font-heading text-cc-heading text-h3 mt-3 max-w-2xl">
        Operations become tools. Tools carry their own behavior.
      </h2>
      <p className="text-cc-prose text-body mt-4 max-w-2xl">
        Every published operation in your registry is exposed over MCP as a tool
        or prompt, annotated with how it behaves. The agent reads that
        annotation before it acts, so the editor knows a read from a write
        before a single line changes.
      </p>

      <div className="mt-10 grid auto-rows-[minmax(0,1fr)] grid-cols-1 gap-4 sm:grid-cols-6">
        {/* Tool-catalog rail (tall, spans 3 cols / 2 rows) */}
        <BentoTile className="sm:col-span-3 sm:row-span-2">
          <TileEyebrow>Tool catalog</TileEyebrow>
          <h3 className="font-heading text-cc-heading text-h5 mt-2">
            Published operations, as annotated tools
          </h3>
          <div className="mt-5 space-y-2.5">
            <ToolCard
              name="getCustomerProfile"
              hint="read"
              desc="openWorldHint · resolves identity"
            />
            <ToolCard
              name="setProductTags"
              hint="idempotent"
              desc="idempotentHint · safe to retry"
            />
            <ToolCard
              name="deleteSubscription"
              hint="destructive"
              desc="destructiveHint · requires approval"
            />
          </div>
          <p className="text-cc-ink-dim text-caption mt-5">
            The badge is the contract.{" "}
            <code className="font-mono text-[#9db0d6]">idempotentHint</code>,{" "}
            <code className="font-mono text-[#9db0d6]">destructiveHint</code>{" "}
            and <code className="font-mono text-[#9db0d6]">openWorldHint</code>{" "}
            ride with the tool so the agent plans around behavior instead of
            guessing.
          </p>
        </BentoTile>

        {/* Oversized stat */}
        <BentoTile className="sm:col-span-3" accent>
          <TileEyebrow>Grounding</TileEyebrow>
          <p
            className="font-heading mt-3 text-[3.4rem] leading-none"
            style={{ color: VIOLET }}
          >
            0
          </p>
          <p className="text-cc-heading text-h6 mt-2">invented field names</p>
          <p className="text-cc-ink-dim text-caption mt-2">
            The agent edits against the schema and the operations your clients
            actually publish, not a hallucinated guess at your API surface.
          </p>
        </BentoTile>

        {/* Pull quote */}
        <BentoTile className="sm:col-span-3">
          <span
            className="font-heading text-[2.5rem] leading-none"
            style={{ color: `${VIOLET}88` }}
          >
            &ldquo;
          </span>
          <p className="text-cc-prose text-h6 -mt-3 leading-snug">
            A coding agent is only as safe as the feedback you give it. Make the
            risky move impossible to take silently.
          </p>
          <p className="text-cc-nav-label mt-4 font-mono text-[0.68rem] tracking-[0.12em] uppercase">
            Design principle
          </p>
        </BentoTile>
      </div>
    </section>
  );
}

interface ToolCardProps {
  readonly name: string;
  readonly hint: "read" | "idempotent" | "destructive";
  readonly desc: string;
}

function ToolCard({ name, hint, desc }: ToolCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface/60 flex items-center gap-3 rounded-lg border px-3 py-2.5">
      <span className="text-cc-nav-label font-mono text-[0.8rem]">{"{}"}</span>
      <div className="min-w-0">
        <p className="text-cc-heading font-mono text-[0.78rem]">{name}</p>
        <p className="text-cc-ink-dim text-[0.66rem]">{desc}</p>
      </div>
      <span className="ml-auto">
        <Badge tone={hint}>{hint}</Badge>
      </span>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/* LifecycleStrip: flat author -> validate -> stage -> trace 4-chip strip.    */
/* -------------------------------------------------------------------------- */

function LifecycleStrip() {
  const steps: ReadonlyArray<{
    readonly n: string;
    readonly title: string;
    readonly desc: string;
  }> = [
    {
      n: "01",
      title: "Author in repo",
      desc: "Tools and prompts live next to your code, reviewed like any change.",
    },
    {
      n: "02",
      title: "Validate in CI",
      desc: "Tools are validated before the service handles traffic.",
    },
    {
      n: "03",
      title: "Stage",
      desc: "Roll the catalog to an environment and see published clients affected.",
    },
    {
      n: "04",
      title: "Trace",
      desc: "Every agent tool call shows up in Nitro with its real impact.",
    },
  ];

  return (
    <section className="mt-24">
      <div className="text-center">
        <SectionLabel>Governed agent-tool lifecycle</SectionLabel>
        <h2 className="font-heading text-cc-heading text-h3 mx-auto mt-3 max-w-2xl">
          The loop that turns a risky edit into actionable feedback
        </h2>
      </div>

      <div className="relative mt-12">
        {/* connecting line */}
        <div
          aria-hidden
          className="absolute top-[1.85rem] right-[8%] left-[8%] hidden h-px lg:block"
          style={{
            background: `linear-gradient(90deg, transparent, ${VIOLET}55 20%, ${VIOLET}55 80%, transparent)`,
          }}
        />
        <ol className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {steps.map((s) => (
            <li
              key={s.n}
              className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border p-5 backdrop-blur-sm"
            >
              <span
                className="bg-cc-surface relative z-10 grid h-9 w-9 place-items-center rounded-full border font-mono text-[0.72rem]"
                style={{ borderColor: `${VIOLET}66`, color: VIOLET }}
              >
                {s.n}
              </span>
              <h3 className="text-cc-heading text-h6 mt-4">{s.title}</h3>
              <p className="text-cc-ink-dim text-caption mt-1.5">{s.desc}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* HubConverge: operations-as-tools pulse INWARD to one /graphql/mcp core.    */
/* -------------------------------------------------------------------------- */

function HubConverge() {
  const spokes: ReadonlyArray<{
    readonly label: string;
    readonly angle: number;
  }> = [
    { label: "getCustomerProfile", angle: -150 },
    { label: "setProductTags", angle: -95 },
    { label: "searchCatalog", angle: -35 },
    { label: "deleteSubscription", angle: 35 },
    { label: "updateLoyaltyTier", angle: 95 },
    { label: "listOrders", angle: 150 },
  ];

  const cx = 300;
  const cy = 170;
  const r = 142;

  return (
    <section className="mt-24">
      <div className="grid items-center gap-10 lg:grid-cols-[0.85fr_1.15fr]">
        <div>
          <SectionLabel>One hub, every operation</SectionLabel>
          <h2 className="font-heading text-cc-heading text-h3 mt-3">
            Everything converges on{" "}
            <span style={{ color: VIOLET }}>/graphql/mcp</span>
          </h2>
          <p className="text-cc-prose text-body mt-4">
            Each published operation flows inward to a single MCP hub. Agents
            connect to one endpoint and discover the whole governed catalog,
            schema, registry, tools, and prompts, in one handshake.
          </p>
          <ul className="mt-6 space-y-3">
            {[
              "Schema and client registry as context, not a copy of your docs",
              "Tools and prompts surfaced with behavior annotations",
              "skillz teaches shared conventions across every agent",
            ].map((t) => (
              <li key={t} className="text-cc-ink-dim text-body flex gap-3">
                <span className="mt-1 shrink-0" style={{ color: VIOLET }}>
                  <CheckIcon size={14} />
                </span>
                {t}
              </li>
            ))}
          </ul>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-4 backdrop-blur-sm">
          <svg
            viewBox="0 0 600 340"
            className="h-auto w-full"
            role="img"
            aria-label="Published operations converging inward to a single /graphql/mcp hub"
          >
            <defs>
              <radialGradient id="v3-core-grad" cx="50%" cy="50%" r="50%">
                <stop offset="0%" stopColor={VIOLET} stopOpacity="0.55" />
                <stop offset="100%" stopColor={VIOLET} stopOpacity="0" />
              </radialGradient>
            </defs>

            {/* converging spokes */}
            {spokes.map((s) => {
              const rad = (s.angle * Math.PI) / 180;
              const x = cx + r * Math.cos(rad);
              const y = cy + r * Math.sin(rad);
              return (
                <g key={s.label}>
                  <line
                    x1={x}
                    y1={y}
                    x2={cx}
                    y2={cy}
                    stroke={VIOLET}
                    strokeOpacity="0.22"
                    strokeWidth="1"
                  />
                  {/* inward pulse */}
                  <line
                    x1={x}
                    y1={y}
                    x2={cx}
                    y2={cy}
                    stroke={VIOLET}
                    strokeWidth="1.5"
                    strokeDasharray="3 19"
                    className="v3-pulse"
                  />
                </g>
              );
            })}

            {/* core glow */}
            <circle
              cx={cx}
              cy={cy}
              r="78"
              fill="url(#v3-core-grad)"
              className="v3-core"
            />

            {/* spoke nodes (operations as tools) */}
            {spokes.map((s) => {
              const rad = (s.angle * Math.PI) / 180;
              const x = cx + r * Math.cos(rad);
              const y = cy + r * Math.sin(rad);
              const anchor =
                x < cx - 20 ? "end" : x > cx + 20 ? "start" : "middle";
              const tx =
                anchor === "end" ? x - 9 : anchor === "start" ? x + 9 : x;
              return (
                <g key={`n-${s.label}`}>
                  <circle
                    cx={x}
                    cy={y}
                    r="4.5"
                    fill="#0c1322"
                    stroke={VIOLET}
                    strokeWidth="1.3"
                  />
                  <text
                    x={tx}
                    y={y + 3.5}
                    textAnchor={anchor}
                    fontSize="10.5"
                    fontFamily="ui-monospace, monospace"
                    fill="rgba(245,241,234,0.62)"
                  >
                    {s.label}
                  </text>
                </g>
              );
            })}

            {/* core node */}
            <circle
              cx={cx}
              cy={cy}
              r="34"
              fill="#0c1322"
              stroke={VIOLET}
              strokeWidth="1.6"
            />
            <text
              x={cx}
              y={cy - 2}
              textAnchor="middle"
              fontSize="12"
              fontFamily="ui-monospace, monospace"
              fontWeight="600"
              fill="#f5f0ea"
            >
              /graphql
            </text>
            <text
              x={cx}
              y={cy + 13}
              textAnchor="middle"
              fontSize="12"
              fontFamily="ui-monospace, monospace"
              fontWeight="600"
              fill={VIOLET}
            >
              /mcp
            </text>
          </svg>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* SkillzBento: prompts + conventions tiles with hover-lift.                  */
/* -------------------------------------------------------------------------- */

function SkillzBento() {
  const tiles: ReadonlyArray<{
    readonly title: string;
    readonly desc: string;
    readonly tag: string;
  }> = [
    {
      tag: "skillz",
      title: "Teach conventions once",
      desc: "Encode how your team writes operations and reuses fragments, then every agent inherits it.",
    },
    {
      tag: "prompts",
      title: "Ship reusable prompts",
      desc: "Curated MCP prompts steer agents toward the operations you trust for a given task.",
    },
    {
      tag: "registry",
      title: "Stay in sync",
      desc: "When the registry changes, the catalog the agent sees changes with it. No drift.",
    },
    {
      tag: "context",
      title: "Edit with product context",
      desc: "The agent knows which fields clients consume, so refactors respect real usage.",
    },
  ];

  return (
    <section className="mt-24">
      <SectionLabel>Conventions that travel</SectionLabel>
      <h2 className="font-heading text-cc-heading text-h3 mt-3 max-w-2xl">
        skillz teaches every agent how your team works
      </h2>

      <div className="mt-9 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {tiles.map((t) => (
          <div
            key={t.tag}
            className="group border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm transition-transform duration-300 hover:-translate-y-1"
          >
            <span
              className="font-mono text-[0.66rem] tracking-[0.14em] uppercase"
              style={{ color: VIOLET }}
            >
              {t.tag}
            </span>
            <h3 className="text-cc-heading text-h6 mt-3">{t.title}</h3>
            <p className="text-cc-ink-dim text-caption mt-2">{t.desc}</p>
            <div
              className="mt-4 h-px w-full origin-left scale-x-0 transition-transform duration-300 group-hover:scale-x-100"
              style={{ background: `${VIOLET}88` }}
            />
          </div>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* HonestyBeat: credibility / what we actually claim.                         */
/* -------------------------------------------------------------------------- */

function HonestyBeat() {
  const facts: ReadonlyArray<{ readonly k: string; readonly v: string }> = [
    {
      k: "Validation is real, not magic",
      v: "Tools are validated in CI before the service handles traffic. We tell you which published clients are affected, so you stage with eyes open.",
    },
    {
      k: "The annotations are the MCP ones",
      v: "idempotentHint, destructiveHint and openWorldHint come straight from the protocol. We do not invent behavior the agent cannot rely on.",
    },
    {
      k: "Approval is a gate, not a suggestion",
      v: "A destructive tool stays pending until it is granted. The agent cannot route around the gate to apply the edit anyway.",
    },
  ];

  return (
    <section className="mt-24">
      <div
        className="relative overflow-hidden rounded-[1.75rem] border p-8 sm:p-12"
        style={{
          borderColor: `${VIOLET}33`,
          background: `linear-gradient(180deg, ${VIOLET}10, transparent 60%)`,
        }}
      >
        <SectionLabel>Where we draw the line</SectionLabel>
        <h2 className="font-heading text-cc-heading text-h3 mt-3 max-w-2xl">
          Honest about what the loop does
        </h2>
        <p className="text-cc-prose text-body mt-4 max-w-2xl">
          A feedback loop is only useful if you can trust it. Here is exactly
          what this one guarantees, and what it does not.
        </p>

        <div className="mt-9 grid grid-cols-1 gap-5 md:grid-cols-3">
          {facts.map((f) => (
            <div key={f.k}>
              <div className="flex items-start gap-2.5">
                <span className="mt-0.5 shrink-0" style={{ color: VIOLET }}>
                  <CheckIcon size={15} />
                </span>
                <h3 className="text-cc-heading text-h6">{f.k}</h3>
              </div>
              <p className="text-cc-ink-dim text-caption mt-2 pl-[1.6rem]">
                {f.v}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* Closing CTA pair.                                                          */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="mt-24 mb-8 text-center">
      <h2 className="font-heading text-cc-heading text-h2 mx-auto max-w-2xl">
        Give your agents a loop worth trusting.
      </h2>
      <p className="text-cc-prose text-body mx-auto mt-5 max-w-xl">
        Connect a coding agent to the operations you already ship, gate the
        dangerous edits, and watch every tool call land in your traces.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/apis/client-registry">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* Shared small pieces.                                                       */
/* -------------------------------------------------------------------------- */

function SectionLabel({ children }: { readonly children: ReactNode }) {
  return (
    <span
      className="font-mono text-[0.7rem] tracking-[0.2em] uppercase"
      style={{ color: VIOLET }}
    >
      {children}
    </span>
  );
}

interface BentoTileProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly accent?: boolean;
}

function BentoTile({ children, className, accent }: BentoTileProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg flex flex-col rounded-2xl border p-6 backdrop-blur-sm ${
        className ?? ""
      }`}
      style={
        accent
          ? {
              background: `linear-gradient(160deg, ${VIOLET}14, transparent 70%)`,
              borderColor: `${VIOLET}33`,
            }
          : undefined
      }
    >
      {children}
    </div>
  );
}

function TileEyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <span
      className="font-mono text-[0.66rem] tracking-[0.14em] uppercase"
      style={{ color: VIOLET }}
    >
      {children}
    </span>
  );
}

function LockIcon() {
  return (
    <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden>
      <rect
        x="3.5"
        y="7"
        width="9"
        height="6.5"
        rx="1.2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
      />
      <path
        d="M5.5 7 V5 a2.5 2.5 0 0 1 5 0 V7"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
    </svg>
  );
}
