import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Agentic Coding | A Feedback Loop for Coding Agents",
  description:
    "Your GraphQL server is already an MCP server. Ground coding agents in published operations and your schema registry, then govern every agent tool from repo to CI to stage to trace.",
  keywords: [
    "agentic coding",
    "coding agents",
    "MCP server",
    "Model Context Protocol",
    "GraphQL",
    "agent tools",
    "behavior annotations",
    "schema registry",
    "client registry",
    "published operations",
    "ChilliCream",
    "Nitro",
  ],
  openGraph: {
    title: "Give coding agents a feedback loop",
    description:
      "Ground coding agents in your schema and published operations, then govern every agent tool through a lifecycle: author in repo, validate in CI, stage, trace.",
  },
  robots: { index: false, follow: false },
};

/* Scene accent for this take: violet (cc-info) means agency + governance.
   Coral is reserved exclusively for the "destructive" tool annotation. */
const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

export default function AgenticCodingPreviewV2() {
  return (
    <article className="pb-28">
      <Hero />
      <Thesis />
      <ChapterOne />
      <ChapterTwo />
      <ChapterThree />
      <HonestyBeat />
      <ClosingCta />
    </article>
  );
}

/* -------------------------------------------------------------------------- */
/* Hero                                                                       */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <header className="relative pt-10 sm:pt-16">
      <Eyebrow>Agentic coding</Eyebrow>
      <h1 className="hero text-cc-heading mt-6 max-w-4xl text-[clamp(2.75rem,8vw,6.5rem)]">
        Give coding agents a feedback loop.
      </h1>
      <p className="lead font-body text-cc-prose mt-8 max-w-2xl text-[1.35rem] leading-snug font-normal">
        Coding agents do not need more freedom. They need product context and a
        way to be told <em className="text-cc-heading not-italic">no</em>. Your
        GraphQL server already carries both.
      </p>
      <div className="mt-10 flex flex-wrap items-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/apis/client-registry">
          Read the Docs
        </OutlineButton>
      </div>

      <div className="mt-16">
        <ApprovalTranscript />
      </div>
    </header>
  );
}

/* The signature visual: a terminal transcript with a PENDING -> GRANTED
   approval gate in violet, and a safe-patch diff shown only after the grant.
   Rendered statically (CSS-only emphasis) so it is a server component and
   reads as an inline editorial figure, not a live screenshot. */
function ApprovalTranscript() {
  return (
    <figure className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md">
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="bg-cc-ink-faint size-3 rounded-full" />
          <span className="bg-cc-ink-faint size-3 rounded-full" />
          <span className="bg-cc-ink-faint size-3 rounded-full" />
        </div>
        <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-wide">
          agent · session 4f2a · /graphql/mcp
        </span>
      </div>

      <div className="bg-cc-code-bg space-y-3 px-5 py-6 font-mono text-[0.82rem] leading-relaxed sm:px-7">
        <Line prompt>refactor checkout to read tax from the API</Line>
        <Line muted>
          resolving context → schema registry · 1,204 published operations
        </Line>
        <Line>
          <span className="text-cc-ink-dim">tool</span>{" "}
          <span className="text-cc-heading">getCheckoutTaxBreakdown</span>
          <Tag color={VIOLET} bg="rgba(124,146,198,0.14)">
            read
          </Tag>
        </Line>
        <Line muted>
          → grounded in real fields · no guessing at the shape of the response
        </Line>

        <ApprovalGate />

        <PatchDiff />
      </div>
    </figure>
  );
}

interface LineProps {
  readonly children: React.ReactNode;
  readonly prompt?: boolean;
  readonly muted?: boolean;
}

function Line({ children, prompt, muted }: LineProps) {
  return (
    <p
      className={
        muted
          ? "text-cc-ink-dim pl-4"
          : prompt
            ? "text-cc-heading"
            : "text-cc-ink"
      }
    >
      {prompt ? <span className="text-cc-accent mr-2">$</span> : null}
      {children}
    </p>
  );
}

interface TagProps {
  readonly children: React.ReactNode;
  readonly color: string;
  readonly bg: string;
}

function Tag({ children, color, bg }: TagProps) {
  return (
    <span
      className="ml-2 inline-block rounded-full px-2 py-0.5 align-middle text-[0.62rem] tracking-wider uppercase"
      style={{ color, backgroundColor: bg }}
    >
      {children}
    </span>
  );
}

/* The pressed/sunken approval row. The GRANTED state is rendered to communicate
   the after-state of the gate; violet carries the "granted" meaning. */
function ApprovalGate() {
  return (
    <div className="my-4 rounded-lg border border-[rgba(124,146,198,0.35)] bg-[rgba(124,146,198,0.06)] p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span
            className="flex size-7 shrink-0 items-center justify-center rounded-full"
            style={{ backgroundColor: "rgba(124,146,198,0.18)", color: VIOLET }}
          >
            <CheckIcon size={13} />
          </span>
          <div>
            <p className="text-cc-heading">
              applyCheckoutPatch
              <Tag color={VIOLET} bg="rgba(124,146,198,0.14)">
                idempotent
              </Tag>
            </p>
            <p className="text-cc-ink-dim mt-0.5 text-[0.72rem]">
              approval gate · pending → granted by maintainer
            </p>
          </div>
        </div>
        <span
          className="rounded-full px-3 py-1 text-[0.66rem] font-semibold tracking-wider uppercase"
          style={{ backgroundColor: "rgba(124,146,198,0.16)", color: VIOLET }}
        >
          Granted
        </span>
      </div>
    </div>
  );
}

/* The safe patch diff appears only AFTER the gate grants. */
function PatchDiff() {
  return (
    <div className="border-cc-card-border mt-2 rounded-lg border bg-[rgba(12,19,34,0.6)] p-4 text-[0.78rem]">
      <p className="text-cc-nav-label mb-2 text-[0.66rem] tracking-wider uppercase">
        checkout/tax.ts
      </p>
      <p className="text-cc-danger/70">
        <span className="mr-2 select-none">-</span>const tax = subtotal *
        0.0825;
      </p>
      <p style={{ color: "rgba(94,234,212,0.85)" }}>
        <span className="mr-2 select-none">+</span>const {"{ tax }"} = await
        getCheckoutTaxBreakdown({"{ cartId }"});
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/* Thesis                                                                      */
/* -------------------------------------------------------------------------- */

function Thesis() {
  return (
    <section className="border-cc-card-border mt-28 border-t pt-12">
      <div className="grid gap-8 md:grid-cols-[1fr_1.4fr]">
        <p className="text-cc-nav-label font-mono text-[0.72rem] tracking-wider uppercase">
          The premise
        </p>
        <p className="font-body text-cc-prose text-[1.5rem] leading-snug">
          An agent that edits your code is only as safe as what it knows and
          what it is allowed to do.{" "}
          <span className="text-cc-heading">
            Your GraphQL server is already an MCP server.
          </span>{" "}
          Its published operations and schema registry become the ground truth
          an agent reads from, and a governed tool lifecycle becomes the rail it
          rides on, so risky edits turn into feedback instead of surprises.
        </p>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* Chapter 01 — Ground                                                         */
/* -------------------------------------------------------------------------- */

function ChapterOne() {
  return (
    <Chapter
      number="01"
      kicker="Ground"
      title="Context the agent did not have to invent."
      lede="Published operations and the schema and client registry are exposed as MCP tools and prompts. The agent edits against the real shape of your product instead of a hallucinated one."
      diagram={<HubAndSpoke />}
      diagramSide="right"
    >
      <SpecBlock
        title="What gets exposed"
        rows={[
          ["Published operations", "as callable tools"],
          ["Schema & client registry", "as grounding prompts"],
          ["Behavior annotations", "on every tool"],
        ]}
      />
      <ul className="font-body text-cc-ink-dim mt-8 space-y-3 text-[0.95rem]">
        <Bullet>
          Operations carry their inputs, outputs, and meaning, so the agent
          stops guessing field names.
        </Bullet>
        <Bullet>
          The registry tells the agent which clients ship which operations
          today.
        </Bullet>
        <Bullet>
          One endpoint, <Mono>/graphql/mcp</Mono>, that every agent converges
          on.
        </Bullet>
      </ul>
    </Chapter>
  );
}

/* Hub-and-spoke convergence: operations-as-tools pulse INWARD to one
   /graphql/mcp core. Inverted from the usual outward fan-out. */
function HubAndSpoke() {
  const spokes = [
    { label: "checkout", angle: -150 },
    { label: "catalog", angle: -90 },
    { label: "orders", angle: -30 },
    { label: "users", angle: 30 },
    { label: "billing", angle: 90 },
    { label: "shipping", angle: 150 },
  ];
  const cx = 160;
  const cy = 160;
  const rOuter = 128;

  return (
    <BlueprintFrame label="MCP convergence">
      <style>{`@media (prefers-reduced-motion: reduce){.ac2-pulse{display:none}}`}</style>
      <svg
        viewBox="0 0 320 320"
        className="h-auto w-full"
        role="img"
        aria-label="Six published operations converging inward to one /graphql/mcp hub node."
      >
        <defs>
          <radialGradient id="ac2-core" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={VIOLET} stopOpacity="0.55" />
            <stop offset="100%" stopColor={VIOLET} stopOpacity="0" />
          </radialGradient>
        </defs>

        {spokes.map((s) => {
          const rad = (s.angle * Math.PI) / 180;
          const x = cx + rOuter * Math.cos(rad);
          const y = cy + rOuter * Math.sin(rad);
          return (
            <g key={s.label}>
              <line
                x1={x}
                y1={y}
                x2={cx}
                y2={cy}
                stroke={VIOLET}
                strokeOpacity="0.32"
                strokeWidth="1"
              />
              {/* particle pulsing inward toward the core */}
              <circle r="2.4" fill={VIOLET} className="ac2-pulse">
                <animateMotion
                  dur="2.6s"
                  repeatCount="indefinite"
                  path={`M ${x} ${y} L ${cx} ${cy}`}
                />
              </circle>
              <g transform={`translate(${x} ${y})`}>
                <rect
                  x="-30"
                  y="-12"
                  width="60"
                  height="24"
                  rx="6"
                  fill="rgba(12,19,34,0.85)"
                  stroke="rgba(245,241,234,0.16)"
                />
                <text
                  x="0"
                  y="4"
                  textAnchor="middle"
                  fontSize="10"
                  fontFamily="var(--font-mono, monospace)"
                  fill="#a1a3af"
                >
                  {s.label}
                </text>
              </g>
            </g>
          );
        })}

        <circle cx={cx} cy={cy} r="58" fill="url(#ac2-core)" />
        <circle
          cx={cx}
          cy={cy}
          r="34"
          fill="rgba(12,19,34,0.92)"
          stroke={VIOLET}
          strokeWidth="1.5"
        />
        <text
          x={cx}
          y={cy + 4}
          textAnchor="middle"
          fontSize="10"
          fontFamily="var(--font-mono, monospace)"
          fill="#f5f0ea"
        >
          /graphql/mcp
        </text>
      </svg>
    </BlueprintFrame>
  );
}

/* -------------------------------------------------------------------------- */
/* Chapter 02 — Annotate                                                       */
/* -------------------------------------------------------------------------- */

function ChapterTwo() {
  return (
    <Chapter
      number="02"
      kicker="Annotate"
      title="Every tool declares how it behaves."
      lede="MCP exposes each operation with behavior annotations. The agent and its guardrails read them before acting, so a read is never mistaken for a write, and a destructive call never slips through unflagged."
      diagram={<ToolCatalog />}
      diagramSide="left"
    >
      <div className="space-y-5">
        <AnnotationRow
          name="idempotentHint"
          color={VIOLET}
          text="Re-running the call yields the same result. Safe to retry."
        />
        <AnnotationRow
          name="openWorldHint"
          color={VIOLET}
          text="The tool reaches an external system whose state it does not own."
        />
        <AnnotationRow
          name="destructiveHint"
          color={CORAL}
          text="The call removes or overwrites state. Treated as a stop-and-ask edge."
        />
      </div>
      <p className="font-body text-cc-ink-dim mt-8 text-[0.9rem]">
        These three are the real MCP annotations. Nothing invented, nothing
        implied.
      </p>
    </Chapter>
  );
}

interface AnnotationRowProps {
  readonly name: string;
  readonly color: string;
  readonly text: string;
}

function AnnotationRow({ name, color, text }: AnnotationRowProps) {
  return (
    <div
      className="flex items-start gap-4 border-l-2 pl-4"
      style={{ borderColor: color }}
    >
      <div>
        <p className="font-mono text-[0.85rem]" style={{ color }}>
          {name}
        </p>
        <p className="font-body text-cc-ink-dim mt-1 text-[0.92rem]">{text}</p>
      </div>
    </div>
  );
}

/* Tool-catalog rail: published operations as tool cards, each with a behavior
   badge. Coral marks the single destructive tool. */
function ToolCatalog() {
  const tools: ReadonlyArray<{
    name: string;
    badge: string;
    color: string;
    destructive?: boolean;
  }> = [
    { name: "getCheckoutTaxBreakdown", badge: "read", color: VIOLET },
    { name: "applyCheckoutPatch", badge: "idempotent", color: VIOLET },
    { name: "fetchShippingQuote", badge: "open-world", color: VIOLET },
    {
      name: "deleteAbandonedCart",
      badge: "destructive",
      color: CORAL,
      destructive: true,
    },
  ];

  return (
    <BlueprintFrame label="Tool catalog">
      <ul className="space-y-3">
        {tools.map((t) => (
          <li
            key={t.name}
            className="flex items-center justify-between rounded-lg border bg-[rgba(12,19,34,0.7)] px-4 py-3"
            style={{
              borderColor: t.destructive
                ? "rgba(240,120,106,0.4)"
                : "rgba(245,241,234,0.12)",
            }}
          >
            <span className="text-cc-heading font-mono text-[0.82rem]">
              {t.name}
            </span>
            <span
              className="ml-3 shrink-0 rounded-full px-2.5 py-0.5 font-mono text-[0.6rem] tracking-wider uppercase"
              style={{
                color: t.color,
                backgroundColor: t.destructive
                  ? "rgba(240,120,106,0.14)"
                  : "rgba(124,146,198,0.14)",
              }}
            >
              {t.badge}
            </span>
          </li>
        ))}
      </ul>
      <p className="text-cc-nav-label mt-4 text-center font-mono text-[0.66rem] tracking-wider">
        ▼ feeds /graphql/mcp
      </p>
    </BlueprintFrame>
  );
}

/* -------------------------------------------------------------------------- */
/* Chapter 03 — Govern                                                         */
/* -------------------------------------------------------------------------- */

function ChapterThree() {
  return (
    <Chapter
      number="03"
      kicker="Govern"
      title="A lifecycle, not a permission prompt."
      lede="Agent tools are authored in the repo, validated in CI, staged, and traced in production. The same review you trust for code now governs what your agents are allowed to do."
      diagram={<LifecycleStrip />}
      diagramSide="right"
    >
      <SpecBlock
        title="The agent-tool lifecycle"
        rows={[
          ["author", "tools declared in the repo, reviewed as code"],
          ["validate", "checked in CI before the service handles traffic"],
          ["stage", "rolled out behind approval gates"],
          ["trace", "every call observable, every edit accountable"],
        ]}
      />
      <p className="font-body text-cc-ink-dim mt-8 text-[0.95rem]">
        And <Mono>skillz</Mono> teaches your conventions across agents, so a new
        agent inherits the same rules as the last one instead of relearning them
        by trial and error.
      </p>
    </Chapter>
  );
}

/* Flat author -> validate -> stage -> trace 4-chip lifecycle strip. */
function LifecycleStrip() {
  const steps = [
    { n: "01", label: "author", sub: "in repo" },
    { n: "02", label: "validate", sub: "in CI" },
    { n: "03", label: "stage", sub: "behind gates" },
    { n: "04", label: "trace", sub: "in prod" },
  ];
  return (
    <BlueprintFrame label="Agent-tool lifecycle">
      <ol className="flex flex-col gap-3 sm:flex-row sm:items-stretch">
        {steps.map((s, i) => (
          <li key={s.label} className="flex flex-1 items-stretch gap-3">
            <div className="flex flex-1 flex-col rounded-lg border border-[rgba(124,146,198,0.3)] bg-[rgba(124,146,198,0.05)] px-4 py-4">
              <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-wider">
                {s.n}
              </span>
              <span className="font-heading text-cc-heading mt-2 text-[1.05rem] font-semibold">
                {s.label}
              </span>
              <span
                className="mt-0.5 font-mono text-[0.66rem]"
                style={{ color: VIOLET }}
              >
                {s.sub}
              </span>
            </div>
            {i < steps.length - 1 ? (
              <span
                className="hidden self-center font-mono text-sm sm:inline"
                style={{ color: VIOLET }}
                aria-hidden
              >
                →
              </span>
            ) : null}
          </li>
        ))}
      </ol>
    </BlueprintFrame>
  );
}

/* -------------------------------------------------------------------------- */
/* Honesty beat                                                                */
/* -------------------------------------------------------------------------- */

function HonestyBeat() {
  return (
    <section className="border-cc-card-border mt-28 border-y py-14">
      <Eyebrow>What this is, and is not</Eyebrow>
      <div className="mt-8 grid gap-10 md:grid-cols-2">
        <div>
          <h3 className="font-heading text-cc-heading text-[1.6rem] font-semibold">
            We give agents a loop.
          </h3>
          <ul className="font-body text-cc-ink-dim mt-5 space-y-3 text-[0.95rem]">
            <Bullet>
              Tools are validated in CI before the service handles traffic.
            </Bullet>
            <Bullet>
              Destructive operations are annotated and gated, never silent.
            </Bullet>
            <Bullet>
              When a schema change lands, you can see which published clients
              are affected.
            </Bullet>
          </ul>
        </div>
        <div>
          <h3 className="font-heading text-cc-ink text-[1.6rem] font-semibold">
            We do not pretend it is magic.
          </h3>
          <ul className="font-body text-cc-ink-dim mt-5 space-y-3 text-[0.95rem]">
            <Bullet muted>
              An agent can still propose a bad edit. The gate is what catches
              it.
            </Bullet>
            <Bullet muted>
              Grounding reduces guessing. It does not remove the need for
              review.
            </Bullet>
            <Bullet muted>
              Governance is a process you run, not a switch you flip.
            </Bullet>
          </ul>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* Closing CTA                                                                 */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="mt-24 text-center">
      <Eyebrow center>Ready when your agents are</Eyebrow>
      <h2 className="font-heading text-cc-heading mx-auto mt-6 max-w-3xl text-[clamp(2.25rem,6vw,3.625rem)] leading-tight font-bold">
        Stop hoping your agents guess right.
      </h2>
      <p className="font-body text-cc-prose mx-auto mt-6 max-w-xl text-[1.05rem]">
        Ground them in your schema, govern their tools, and watch risky edits
        become reviewable feedback.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/apis/client-registry">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/* Shared editorial primitives                                                 */
/* -------------------------------------------------------------------------- */

interface ChapterProps {
  readonly number: string;
  readonly kicker: string;
  readonly title: string;
  readonly lede: string;
  readonly diagram: React.ReactNode;
  readonly diagramSide: "left" | "right";
  readonly children: React.ReactNode;
}

function Chapter({
  number,
  kicker,
  title,
  lede,
  diagram,
  diagramSide,
  children,
}: ChapterProps) {
  return (
    <section className="mt-28">
      <div className="border-cc-card-border flex items-baseline gap-5 border-b pb-6">
        <span
          className="font-heading text-[2.75rem] leading-none font-bold"
          style={{ color: "rgba(124,146,198,0.55)" }}
        >
          {number}
        </span>
        <div>
          <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.18em] uppercase">
            {kicker}
          </p>
          <h2 className="font-heading text-cc-heading mt-1 max-w-2xl text-[clamp(1.75rem,4vw,2.75rem)] leading-tight font-bold">
            {title}
          </h2>
        </div>
      </div>

      <div
        className={`mt-12 grid items-start gap-12 lg:grid-cols-2 ${
          diagramSide === "left" ? "" : ""
        }`}
      >
        <div className={diagramSide === "left" ? "lg:order-2" : "lg:order-1"}>
          <p className="font-body text-cc-prose text-[1.15rem] leading-relaxed">
            {lede}
          </p>
          <div className="mt-8">{children}</div>
        </div>
        <div className={diagramSide === "left" ? "lg:order-1" : "lg:order-2"}>
          {diagram}
        </div>
      </div>
    </section>
  );
}

interface BlueprintFrameProps {
  readonly label: string;
  readonly children: React.ReactNode;
}

/* Faint blueprint dot-grid frame for line diagrams. */
function BlueprintFrame({ label, children }: BlueprintFrameProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border p-6 backdrop-blur-md">
      <div
        className="pointer-events-none absolute inset-0 rounded-2xl opacity-[0.5]"
        style={{
          backgroundImage:
            "radial-gradient(rgba(245,241,234,0.10) 1px, transparent 1px)",
          backgroundSize: "18px 18px",
        }}
        aria-hidden
      />
      <p className="text-cc-nav-label relative mb-4 font-mono text-[0.62rem] tracking-[0.2em] uppercase">
        {label}
      </p>
      <div className="relative">{children}</div>
    </div>
  );
}

interface SpecBlockProps {
  readonly title: string;
  readonly rows: ReadonlyArray<readonly [string, string]>;
}

function SpecBlock({ title, rows }: SpecBlockProps) {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[rgba(12,19,34,0.4)] p-5">
      <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.2em] uppercase">
        {title}
      </p>
      <dl className="divide-cc-card-border mt-4 divide-y">
        {rows.map(([k, v]) => (
          <div
            key={k}
            className="flex flex-col gap-1 py-2.5 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4"
          >
            <dt className="text-cc-heading font-mono text-[0.8rem]">{k}</dt>
            <dd className="font-body text-cc-ink-dim text-[0.85rem] sm:text-right">
              {v}
            </dd>
          </div>
        ))}
      </dl>
    </div>
  );
}

interface BulletProps {
  readonly children: React.ReactNode;
  readonly muted?: boolean;
}

function Bullet({ children, muted }: BulletProps) {
  return (
    <li className="flex items-start gap-3">
      <span
        className="mt-1.5 size-1.5 shrink-0 rounded-full"
        style={{ backgroundColor: muted ? "rgba(161,163,175,0.5)" : VIOLET }}
        aria-hidden
      />
      <span>{children}</span>
    </li>
  );
}

interface EyebrowProps {
  readonly children: React.ReactNode;
  readonly center?: boolean;
}

function Eyebrow({ children, center }: EyebrowProps) {
  return (
    <p
      className={`text-cc-nav-label flex items-center gap-3 font-mono text-[0.72rem] tracking-[0.22em] uppercase ${
        center ? "justify-center" : ""
      }`}
    >
      <span
        className="inline-block h-px w-8"
        style={{ backgroundColor: VIOLET, opacity: 0.6 }}
        aria-hidden
      />
      {children}
    </p>
  );
}

function Mono({ children }: { readonly children: React.ReactNode }) {
  return (
    <code className="text-cc-heading rounded bg-[rgba(124,146,198,0.12)] px-1.5 py-0.5 font-mono text-[0.85em]">
      {children}
    </code>
  );
}
