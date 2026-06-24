import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

/**
 * Preview variant (v6) of the Agentic coding page. Barista / coffee family:
 * ChilliCream's house brand is coffee (Hot Chocolate, Strawberry Shake, Mocha,
 * Green Donut, Cookie Crumble, Nitro), so this variant leans on barista voice
 * in eyebrows and section headings, with the existing drink icons as small
 * decorative glyphs in section headers.
 *
 * The palette stays cc-* dark navy/teal. Violet (#7c92c6) is the page accent,
 * coral (#f0786a) is reserved strictly for the single destructive annotation,
 * and the brand spectrum gradient appears at most once, in the hero lead.
 *
 * No warm-beige paper, no chalkboard textures, no foreign palette. Coffee
 * lives only in the copy and four decorative icons.
 */

export const metadata: Metadata = {
  title: "Agentic Coding: A House Blend for Coding Agents",
  description:
    "GraphQL MCP for coding agents, pulled like a shot. Published operations become governed tools, authored in repo, validated in CI, staged with a gate, and traced.",
  keywords: [
    "GraphQL MCP for coding agents",
    "agentic coding feedback loop",
    "operations as MCP tools",
    "agent tool lifecycle governance",
    "MCP behavior annotations",
    "client registry grounding for agents",
    "skillz agent conventions",
    "validate MCP tools in CI",
    ".NET GraphQL agents",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "House Blend for Coding Agents",
    description:
      "GraphQL MCP for coding agents, pulled like a shot. Published operations become governed tools, authored, validated, staged, and traced before they touch production.",
  },
};

const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

/** One spectrum gradient is permitted per screen; used once, in the hero lead. */
const SPECTRUM = "linear-gradient(100deg,#16b9e4 0%,#7c92c6 50%,#f0786a 100%)";

/* ------------------------------------------------------------------ *
 * Shared small parts
 * ------------------------------------------------------------------ */

interface EyebrowProps {
  readonly children: ReactNode;
  readonly icon?: ReactNode;
}

function Eyebrow({ children, icon }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      {icon ? (
        <span
          aria-hidden="true"
          className="inline-flex h-3.5 w-3.5 items-center justify-center"
          style={{ color: VIOLET }}
        >
          {icon}
        </span>
      ) : null}
      <span>{children}</span>
    </p>
  );
}

interface SectionHeadingProps {
  readonly eyebrow: string;
  readonly eyebrowIcon?: ReactNode;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

/** Left-aligned section header: eyebrow + heading + optional intro prose. */
function SectionHeading({
  eyebrow,
  eyebrowIcon,
  title,
  children,
}: SectionHeadingProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow icon={eyebrowIcon}>{eyebrow}</Eyebrow>
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

/* ------------------------------------------------------------------ *
 * Hero artifact: the order ticket + pulled shot pair
 * ------------------------------------------------------------------ */

/**
 * Two stacked artifacts: an "order ticket" chit showing the agent's intended
 * call (operation, shot count, behavior notes), then the "pulled shot" card
 * confirming the approved pour. Both rendered as cc-surface cards with violet
 * accent; the only coral is the destructiveHint badge.
 */
function OrderTicket() {
  return (
    <div className="space-y-4">
      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-md">
        <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2.5">
          <span className="flex items-center gap-2">
            <span
              aria-hidden="true"
              className="inline-flex h-4 w-4 items-center justify-center"
              style={{ color: VIOLET }}
            >
              <Espresso className="h-4 w-4" />
            </span>
            <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-wide">
              order ticket · /graphql/mcp
            </span>
          </span>
          <span
            className="font-mono text-[0.55rem] tracking-[0.1em] uppercase"
            style={{ color: VIOLET }}
          >
            pending
          </span>
        </div>

        <div className="bg-cc-code-bg space-y-3 px-4 py-4 font-mono text-[0.72rem] leading-relaxed sm:px-5 sm:py-5">
          <div className="flex items-baseline justify-between gap-3">
            <span className="text-cc-ink-faint text-[0.6rem] tracking-[0.16em] uppercase">
              op
            </span>
            <span className="text-cc-ink">updateProductTags</span>
          </div>
          <div className="flex items-baseline justify-between gap-3">
            <span className="text-cc-ink-faint text-[0.6rem] tracking-[0.16em] uppercase">
              shots
            </span>
            <span className="text-cc-ink">1 · single product, id 42</span>
          </div>
          <div className="flex items-baseline justify-between gap-3">
            <span className="text-cc-ink-faint text-[0.6rem] tracking-[0.16em] uppercase">
              notes
            </span>
            <span className="text-cc-ink">add tag &ldquo;sale&rdquo;</span>
          </div>
          <div className="border-cc-card-border flex items-center justify-between gap-3 border-t pt-3">
            <span className="text-cc-ink-faint text-[0.6rem] tracking-[0.16em] uppercase">
              behavior
            </span>
            <span className="flex flex-wrap items-center justify-end gap-1.5">
              <HintBadge hint="destructive" />
            </span>
          </div>
        </div>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-md">
        <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2.5">
          <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-wide">
            pulled shot · ProductCard.graphql
          </span>
          <span
            className="font-mono text-[0.55rem] tracking-[0.1em] uppercase"
            style={{ color: VIOLET }}
          >
            approved
          </span>
        </div>
        <pre className="bg-cc-code-bg overflow-x-auto px-4 py-3.5 font-mono text-[0.68rem] leading-relaxed sm:px-5">
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
  );
}

/* ------------------------------------------------------------------ *
 * Menu card rail: published operations styled as a coffee menu
 * ------------------------------------------------------------------ */

interface MenuItemProps {
  readonly name: string;
  readonly summary: string;
  readonly note: string;
  readonly hint: Hint;
}

const MENU_ITEMS: readonly MenuItemProps[] = [
  {
    name: "getProduct",
    summary: "query · single product by id",
    note: "house pour",
    hint: "read-only",
  },
  {
    name: "searchOrders",
    summary: "query · filtered order list",
    note: "repeatable",
    hint: "idempotent",
  },
  {
    name: "tagProduct",
    summary: "mutation · upsert product tags",
    note: "repeatable",
    hint: "idempotent",
  },
  {
    name: "deleteReview",
    summary: "mutation · remove a review",
    note: "off the menu without a gate",
    hint: "destructive",
  },
  {
    name: "openTicket",
    summary: "mutation · calls an external desk",
    note: "reaches outside the bar",
    hint: "open-world",
  },
];

function MenuItem({ name, summary, note, hint }: MenuItemProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover flex items-start gap-3 rounded-xl border px-3.5 py-3 transition-colors">
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-ink truncate font-mono text-xs">{name}</p>
          <HintBadge hint={hint} />
        </div>
        <p className="text-cc-ink-faint mt-0.5 truncate text-[0.7rem]">
          {summary}
        </p>
        <p
          className="mt-1 font-mono text-[0.6rem] tracking-[0.08em]"
          style={{
            color: hint === "destructive" ? CORAL : VIOLET,
          }}
        >
          note · {note}
        </p>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Inward-converging MCP hub diagram (Behind the bar)
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
  const cx = 200;
  const cy = 168;
  const radius = 128;
  const coreR = 38;

  const points = HUB_SPOKES.map((spoke, i) => {
    const angle = (i / HUB_SPOKES.length) * Math.PI * 2 - Math.PI / 2;
    const x = cx + radius * Math.cos(angle);
    const y = cy + radius * Math.sin(angle);
    const tx = cx + (coreR + 6) * Math.cos(angle);
    const ty = cy + (coreR + 6) * Math.sin(angle);
    return { spoke, x, y, tx, ty };
  });

  return (
    <svg
      viewBox="0 0 400 336"
      className="h-auto w-full"
      role="img"
      aria-label="Every published operation flowing inward to one /graphql/mcp hub, like orders to one bar."
    >
      <defs>
        <marker
          id="acv6-hub-arrow"
          viewBox="0 0 10 10"
          refX="8"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M0 0 L10 5 L0 10 z" fill={VIOLET} />
        </marker>
        <radialGradient id="acv6-hub-core" cx="50%" cy="50%" r="60%">
          <stop offset="0%" stopColor="rgba(124,146,198,0.35)" />
          <stop offset="100%" stopColor="rgba(124,146,198,0.05)" />
        </radialGradient>
      </defs>

      {points.map(({ spoke, x, y, tx, ty }) => {
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
              strokeOpacity={0.55}
              strokeWidth={1.25}
              markerEnd="url(#acv6-hub-arrow)"
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

      {/* Small espresso glyph hovering over the core */}
      <g transform={`translate(${cx - 11}, ${cy - coreR - 22})`}>
        <Espresso style={{ width: 22, height: 22, color: VIOLET }} />
      </g>

      <circle cx={cx} cy={cy} r={coreR + 10} fill="url(#acv6-hub-core)" />
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
 * Brew lifecycle strip: Author / Validate / Stage / Trace
 * ------------------------------------------------------------------ */

interface LifecycleStep {
  readonly key: string;
  readonly title: string;
  readonly brew: string;
  readonly note: string;
  readonly icon: ReactNode;
}

const LIFECYCLE: readonly LifecycleStep[] = [
  {
    key: "author",
    title: "Author",
    brew: "grind",
    note: "in repo · .graphql + settings",
    icon: <DripBrewer className="h-5 w-5" />,
  },
  {
    key: "validate",
    title: "Validate",
    brew: "tamp",
    note: "in CI · nitro mcp validate",
    icon: <FrenchPress className="h-5 w-5" />,
  },
  {
    key: "stage",
    title: "Stage",
    brew: "pull",
    note: "promote with approval gate",
    icon: <PourOver className="h-5 w-5" />,
  },
  {
    key: "trace",
    title: "Trace",
    brew: "taste",
    note: "per-tool p95 in Nitro",
    icon: <Espresso className="h-5 w-5" />,
  },
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
            <span className="flex items-center gap-2">
              <span
                aria-hidden="true"
                className="inline-flex h-5 w-5 items-center justify-center"
                style={{ color: VIOLET }}
              >
                {item.icon}
              </span>
              <span
                className="font-mono text-[0.62rem] tracking-[0.14em] uppercase"
                style={{ color: VIOLET }}
              >
                0{index + 1} · {item.brew}
              </span>
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
 * skillz bento (House rules)
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
 * Honesty beat
 * ------------------------------------------------------------------ */

const HONESTY_POINTS: readonly string[] = [
  "Tools and prompts are authored in the repo as reviewed code, not minted at runtime.",
  "nitro mcp validate runs in CI, so a broken tool collection never reaches a stage.",
  "Behavior is declared with idempotentHint, destructiveHint, and openWorldHint.",
  "An edit is checked against published operations; risky changes read “published clients affected.”",
  "Every tool call is traced in Nitro with p95 latency, error rate, and impact.",
];

/* ------------------------------------------------------------------ *
 * Page
 * ------------------------------------------------------------------ */

export default function AgenticCodingPreviewV6() {
  return (
    <>
      {/* ---------------------------------------------------------- *
       * Hero: barista voice, order ticket + pulled shot artifact
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
            Today&rsquo;s pour · Agentic coding preview
          </span>

          <h1 className="font-heading text-cc-heading mt-6 text-4xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
            Pull every agent call like a shot.
          </h1>

          <p className="lead text-cc-ink-dim mt-6 max-w-xl text-pretty">
            GraphQL MCP for coding agents, served from one bar. Your published
            operations are the house blend, each tool a single-origin shot
            pulled to a known recipe, with a{" "}
            <span
              className="bg-clip-text font-medium text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              governed pour
            </span>{" "}
            from order ticket to served cup.
          </p>

          <p className="text-cc-ink mt-5 max-w-xl text-base/relaxed text-pretty">
            Your GraphQL server is already an MCP server. Published operations
            become tools an agent can call with real product context, each one
            authored, validated, and traced before it ever lands in production.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>
        </div>

        <div className="lg:pl-4">
          <OrderTicket />
          <p className="text-cc-ink-faint mt-3 text-center font-mono text-[0.62rem] tracking-wide">
            order ticket on the rail, shot pulled only after the gate
          </p>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * On the menu: grounding + menu card rail
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="grid items-start gap-10 lg:grid-cols-[1fr_1.05fr] lg:gap-14">
          <SectionHeading
            eyebrow="On the menu"
            eyebrowIcon={<CoffeeTray className="h-3.5 w-3.5" />}
            title="Agents order from a menu your clients already drink."
          >
            <p>
              A coding agent that does not know your graph invents fields and
              writes queries no client would ship. The schema and client
              registry change that: your published operations become the menu,
              each item a real, reviewed shape your product already depends on.
            </p>
            <p>
              MCP serves those operations as tools and prompts with behavior
              annotations, so the agent can tell a safe pour from a destructive
              one before it acts, and you keep authority over what goes out.
            </p>
          </SectionHeading>

          <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
            <div className="flex items-center justify-between">
              <Eyebrow icon={<CoffeeTray className="h-3.5 w-3.5" />}>
                The menu card
              </Eyebrow>
              <span className="text-cc-ink-faint font-mono text-[0.6rem]">
                38 published ops
              </span>
            </div>
            <div className="mt-4 space-y-2.5">
              {MENU_ITEMS.map((item) => (
                <MenuItem
                  key={item.name}
                  name={item.name}
                  summary={item.summary}
                  note={item.note}
                  hint={item.hint}
                />
              ))}
            </div>
            <p className="text-cc-ink-faint mt-4 font-mono text-[0.6rem] leading-relaxed">
              annotations: idempotentHint · readOnlyHint · openWorldHint ·
              <span style={{ color: CORAL }}> destructiveHint</span>
            </p>
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Behind the bar: one MCP hub
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="grid items-center gap-10 lg:grid-cols-[1fr_1fr] lg:gap-14">
          <div className="order-2 lg:order-1">
            <div className="border-cc-card-border bg-cc-card-bg rounded-3xl border p-6 backdrop-blur-sm sm:p-8">
              <McpHub />
            </div>
          </div>

          <div className="order-1 lg:order-2">
            <SectionHeading
              eyebrow="Behind the bar"
              eyebrowIcon={<Espresso className="h-3.5 w-3.5" />}
              title="Every order flows to one bar."
            >
              <p>
                All published operations flow inward to a single{" "}
                <code className="text-cc-info">/graphql/mcp</code> endpoint over
                Streamable HTTP. One endpoint, one schema, no parallel tool
                definitions to drift; the same registry that runs your API
                grounds the agent.
              </p>
              <p>
                Because the schema is typed and introspectable, each tool
                carries an accurate parameter contract, so agents make fewer
                malformed calls and destructive ones stay clearly marked.
              </p>
            </SectionHeading>
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * The brew lifecycle
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div>
          <SectionHeading
            eyebrow="The brew lifecycle"
            eyebrowIcon={<DripBrewer className="h-3.5 w-3.5" />}
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
       * House rules: skillz bento
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div>
          <SectionHeading
            eyebrow="House rules"
            eyebrowIcon={<FrenchPress className="h-3.5 w-3.5" />}
            title="The recipes every agent inherits."
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
       * Quality control: honesty beat
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="grid items-start gap-10 lg:grid-cols-[1fr_1fr] lg:gap-14">
          <SectionHeading
            eyebrow="Quality control on the bar"
            eyebrowIcon={<PourOver className="h-3.5 w-3.5" />}
            title="We only serve what the registry can prove."
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
        <Eyebrow>Open the bar</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h3 mx-auto mt-4 max-w-2xl leading-tight font-semibold text-balance">
          Put your agents on a recipe you control.
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
