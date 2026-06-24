import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Preview variant (v5) of the Agentic Coding page. Stance: "Ledger of Two
 * Columns". Side-by-side comparison family: every body section is a strict
 * two-column grid split by a full-height vertical cc-card-border rule, with a
 * small violet "VS" node punched through the rule at each section break. Left
 * column is always "Hand-rolled MCP" (muted ink, cross chips); right column is
 * always "ChilliCream Platform" (violet accent, check chips). All canonical
 * facts (operations, lifecycle verbs, SKILL filenames, honesty points) match
 * v1 verbatim; only the layout grammar changes.
 */

export const metadata: Metadata = {
  title: "GraphQL MCP for Coding Agents | Side-by-Side Ledger",
  description:
    "GraphQL MCP for coding agents, column by column: hand-rolled tool wiring on the left, governed operations with Author, Validate, Stage, Trace on the right.",
  keywords: [
    "GraphQL MCP for coding agents",
    "agent tool lifecycle",
    "schema and client registry grounding",
    "MCP behavior annotations",
    "destructiveHint readOnlyHint idempotentHint openWorldHint",
    "hand-rolled MCP server vs platform",
    "skillz SKILL.md conventions",
    "Nitro tool tracing",
    "validate MCP tools in CI",
    "agent governance",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "GraphQL MCP for Coding Agents, Side by Side",
    description:
      "Pick the column you want to live in: hand-rolled tool wiring, or operations as governed tools with a real Author, Validate, Stage, Trace lifecycle.",
  },
};

const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

/** One spectrum gradient is permitted per screen; used once, on the hero word. */
const SPECTRUM = "linear-gradient(100deg,#16b9e4 0%,#7c92c6 50%,#f0786a 100%)";

/* ------------------------------------------------------------------ *
 * Small shared parts
 * ------------------------------------------------------------------ */

interface MonoEyebrowProps {
  readonly children: ReactNode;
}

function MonoEyebrow({ children }: MonoEyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

/* ------------------------------------------------------------------ *
 * Ledger primitives: the section header (with VS grommet) and the
 * two paired columns that snap to the central vertical rule.
 * ------------------------------------------------------------------ */

interface VsNodeProps {
  readonly label?: string;
}

/** Small circular violet "VS" grommet that punches through the centre rule. */
function VsNode({ label = "VS" }: VsNodeProps) {
  return (
    <span
      aria-hidden="true"
      className="border-cc-card-border bg-cc-bg text-cc-ink-dim hidden h-9 w-9 shrink-0 items-center justify-center rounded-full border font-mono text-[0.62rem] tracking-[0.16em] lg:inline-flex"
      style={{
        color: VIOLET,
        borderColor: "rgba(124,146,198,0.55)",
        boxShadow: "inset 0 0 0 1px rgba(124,146,198,0.12)",
      }}
    >
      {label}
    </span>
  );
}

interface LedgerHeaderProps {
  readonly eyebrow: string;
  readonly leftTitle: string;
  readonly rightTitle: string;
}

/**
 * Section header spanning both columns. Eyebrow centred above, two row
 * titles flanking the VS grommet.
 */
function LedgerHeader({ eyebrow, leftTitle, rightTitle }: LedgerHeaderProps) {
  return (
    <header className="mb-10">
      <div className="text-center">
        <MonoEyebrow>{eyebrow}</MonoEyebrow>
      </div>
      <h2 className="font-heading text-h4 sm:text-h3 mt-5 grid items-center gap-4 leading-[1.1] font-semibold text-balance lg:grid-cols-[1fr_auto_1fr] lg:gap-0">
        <span className="text-cc-ink-dim lg:pr-10 lg:text-right">
          {leftTitle}
        </span>
        <VsNode />
        <span className="text-cc-heading lg:pl-10">{rightTitle}</span>
      </h2>
    </header>
  );
}

interface ColumnHeaderProps {
  readonly side: "left" | "right";
}

/** Sub-header above each column body: the mono eyebrow + h6 column name. */
function ColumnHeader({ side }: ColumnHeaderProps) {
  if (side === "left") {
    return (
      <div className="mb-4">
        <p className="text-cc-ink-faint font-mono text-[0.62rem] tracking-[0.18em] uppercase">
          Column A
        </p>
        <p className="font-heading text-cc-ink text-h6 mt-1 font-semibold">
          Hand-rolled MCP
        </p>
      </div>
    );
  }
  return (
    <div className="mb-4">
      <p
        className="font-mono text-[0.62rem] tracking-[0.18em] uppercase"
        style={{ color: VIOLET }}
      >
        Column B
      </p>
      <p className="font-heading text-cc-heading text-h6 mt-1 font-semibold">
        ChilliCream Platform
      </p>
    </div>
  );
}

interface LedgerRowProps {
  readonly left: ReactNode;
  readonly right: ReactNode;
}

/**
 * The structural primitive every body section uses: two equal-weight columns
 * separated by a vertical cc-card-border rule that snaps to the page centre.
 */
function LedgerRow({ left, right }: LedgerRowProps) {
  return (
    <div className="relative grid gap-8 lg:grid-cols-2 lg:gap-0">
      {/* Continuous central rule, the visual motif of the page. */}
      <span
        aria-hidden="true"
        className="bg-cc-card-border pointer-events-none absolute inset-y-0 left-1/2 hidden w-px lg:block"
      />
      <div className="lg:pr-10">{left}</div>
      <div className="lg:pl-10">{right}</div>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Chips: the cross (left) and check (right) status markers.
 * ------------------------------------------------------------------ */

interface CrossGlyphProps {
  readonly size?: number;
}

function CrossGlyph({ size = 10 }: CrossGlyphProps) {
  return (
    <svg
      viewBox="0 0 10 10"
      width={size}
      height={size}
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
    >
      <path d="M2 2 L8 8" />
      <path d="M8 2 L2 8" />
    </svg>
  );
}

interface MutedChipProps {
  readonly children: ReactNode;
}

function MutedChip({ children }: MutedChipProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim bg-cc-surface inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.62rem] tracking-[0.04em]">
      <span className="text-cc-ink-faint">
        <CrossGlyph />
      </span>
      {children}
    </span>
  );
}

interface AccentChipProps {
  readonly children: ReactNode;
}

function AccentChip({ children }: AccentChipProps) {
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.62rem] tracking-[0.04em]"
      style={{
        color: VIOLET,
        borderColor: "rgba(124,146,198,0.45)",
        backgroundColor: "rgba(124,146,198,0.08)",
      }}
    >
      <span style={{ color: VIOLET }}>
        <CheckIcon size={9} />
      </span>
      {children}
    </span>
  );
}

/** The single coral-accented chip, reserved for the destructive annotation row. */
interface CoralChipProps {
  readonly children: ReactNode;
}

function CoralChip({ children }: CoralChipProps) {
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.62rem] tracking-[0.04em] whitespace-nowrap"
      style={{
        color: CORAL,
        borderColor: "rgba(240,120,106,0.45)",
        backgroundColor: "rgba(240,120,106,0.08)",
      }}
    >
      {children}
    </span>
  );
}

/* ------------------------------------------------------------------ *
 * Card wrappers used inside columns. Heights are encouraged to match
 * via h-full so the central rule never visually breaks.
 * ------------------------------------------------------------------ */

interface ColumnCardProps {
  readonly side: "left" | "right";
  readonly children: ReactNode;
}

function ColumnCard({ side, children }: ColumnCardProps) {
  const borderStyle =
    side === "right" ? { borderColor: "rgba(124,146,198,0.25)" } : undefined;
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg h-full rounded-2xl border p-6 backdrop-blur-sm sm:p-7"
      style={borderStyle}
    >
      {children}
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Hero status rails: the two stacked "Without / With GraphQL MCP" rows
 * that replace the product mock from v1.
 * ------------------------------------------------------------------ */

interface HeroRailItem {
  readonly text: string;
}

const WITHOUT_RAIL: readonly HeroRailItem[] = [
  { text: "Agents guess fields the API never published" },
  { text: "Tool wiring lives in a side server you maintain" },
  { text: "Writes and reads sit at the same risk level" },
  { text: "Every call lands blind in production" },
];

const WITH_RAIL: readonly HeroRailItem[] = [
  { text: "Grounded in the schema and client registry" },
  { text: "Operations are tools at /graphql/mcp" },
  { text: "idempotent, readOnly, openWorld, destructive" },
  { text: "Authored, validated, staged, traced" },
];

interface HeroRailProps {
  readonly variant: "without" | "with";
  readonly items: readonly HeroRailItem[];
}

function HeroRail({ variant, items }: HeroRailProps) {
  const isWith = variant === "with";
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6"
      style={isWith ? { borderColor: "rgba(124,146,198,0.35)" } : undefined}
    >
      <div className="flex items-center justify-between">
        <p
          className="font-mono text-[0.62rem] tracking-[0.18em] uppercase"
          style={{ color: isWith ? VIOLET : undefined }}
        >
          {isWith ? "With GraphQL MCP" : "Without GraphQL MCP"}
        </p>
        <p className="text-cc-ink-faint font-mono text-[0.58rem] tracking-[0.12em] uppercase">
          {isWith ? "ChilliCream Platform" : "Hand-rolled"}
        </p>
      </div>
      <ul className="mt-4 space-y-2.5">
        {items.map((item) => (
          <li key={item.text} className="flex items-start gap-2.5">
            <span
              aria-hidden="true"
              className={
                isWith ? "mt-1 shrink-0" : "text-cc-ink-faint mt-1 shrink-0"
              }
              style={isWith ? { color: VIOLET } : undefined}
            >
              {isWith ? <CheckIcon size={11} /> : <CrossGlyph size={11} />}
            </span>
            <span
              className={
                isWith
                  ? "text-cc-ink text-sm/relaxed"
                  : "text-cc-ink-dim text-sm/relaxed"
              }
            >
              {item.text}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Section 1 (Grounding): a 3-row chip ledger of operations, showing
 * "guess" on the left and "grounded" on the right for each.
 * ------------------------------------------------------------------ */

interface OpRow {
  readonly name: string;
  readonly leftLabel: string;
  readonly rightLabel: string;
}

const OP_ROWS: readonly OpRow[] = [
  {
    name: "getProduct",
    leftLabel: "guessed shape",
    rightLabel: "registry · readOnly",
  },
  {
    name: "tagProduct",
    leftLabel: "no annotation",
    rightLabel: "registry · idempotent",
  },
  {
    name: "deleteReview",
    leftLabel: "indistinguishable",
    rightLabel: "registry · destructive",
  },
];

/* ------------------------------------------------------------------ *
 * Section 2 (Surface): cost cards for "bespoke MCP server" vs
 * "your /graphql/mcp endpoint". Monospace endpoint chip on each side.
 * ------------------------------------------------------------------ */

interface CostLine {
  readonly label: string;
  readonly text: string;
}

const HANDROLL_COSTS: readonly CostLine[] = [
  { label: "Build", text: "Define every tool by hand and keep them in sync." },
  { label: "Run", text: "Operate a second server next to your GraphQL API." },
  { label: "Secure", text: "Re-issue tokens, scopes, and rate limits for it." },
];

const PLATFORM_COSTS: readonly CostLine[] = [
  {
    label: "Build",
    text: "Published operations become tools without new code.",
  },
  {
    label: "Run",
    text: "Streamable HTTP rides on the same GraphQL server.",
  },
  {
    label: "Secure",
    text: "Same auth, same registry, same observability.",
  },
];

/* ------------------------------------------------------------------ *
 * Section 3 (Behavior annotations): four mirrored rows. Destructive is
 * the only place coral appears: a left-side danger ghost and a right-side
 * destructiveHint badge.
 * ------------------------------------------------------------------ */

interface AnnotationRow {
  readonly leftLabel: string;
  readonly rightHint:
    | "readOnlyHint"
    | "idempotentHint"
    | "openWorldHint"
    | "destructiveHint";
  readonly meaning: string;
}

const ANNOTATIONS: readonly AnnotationRow[] = [
  {
    leftLabel: "unlabelled read",
    rightHint: "readOnlyHint",
    meaning: "Pure query; safe to retry, safe to cache.",
  },
  {
    leftLabel: "unlabelled retry",
    rightHint: "idempotentHint",
    meaning: "Same input, same effect; safe to retry.",
  },
  {
    leftLabel: "unlabelled outbound",
    rightHint: "openWorldHint",
    meaning: "Touches an external system the platform does not own.",
  },
  {
    leftLabel: "unlabelled write",
    rightHint: "destructiveHint",
    meaning: "Removes data; requires an approval gate.",
  },
];

/* ------------------------------------------------------------------ *
 * Section 4 (Lifecycle): four numbered ledger rows on each side. Left
 * is the manual cost; right is the platform verb.
 * ------------------------------------------------------------------ */

interface LifecycleRow {
  readonly index: string;
  readonly leftTitle: string;
  readonly leftNote: string;
  readonly rightTitle: string;
  readonly rightNote: string;
}

const LIFECYCLE_ROWS: readonly LifecycleRow[] = [
  {
    index: "01",
    leftTitle: "Re-explain conventions",
    leftNote: "in every PR",
    rightTitle: "Author",
    rightNote: "in repo · .graphql + settings",
  },
  {
    index: "02",
    leftTitle: "Discover breakage",
    leftNote: "after merge, at runtime",
    rightTitle: "Validate",
    rightNote: "in CI · nitro mcp validate",
  },
  {
    index: "03",
    leftTitle: "Hand-promote tools",
    leftNote: "between environments",
    rightTitle: "Stage",
    rightNote: "promote with approval gate",
  },
  {
    index: "04",
    leftTitle: "Read scattered logs",
    leftNote: "to guess at impact",
    rightTitle: "Trace",
    rightNote: "per-tool p95 in Nitro",
  },
];

/* ------------------------------------------------------------------ *
 * Section 5 (skillz): paired list of four SKILL.md filenames on the
 * right vs four "remind the agent..." bullets on the left.
 * ------------------------------------------------------------------ */

interface SkillPair {
  readonly remind: string;
  readonly file: string;
  readonly body: string;
}

const SKILL_PAIRS: readonly SkillPair[] = [
  {
    remind: "Remind the agent to page list fields the registry way.",
    file: "pagination.SKILL.md",
    body: "Always page list fields with the registry connection contract.",
  },
  {
    remind: "Remind the agent to use typed result unions, not throws.",
    file: "errors.SKILL.md",
    body: "Model failures as typed union results, never thrown exceptions.",
  },
  {
    remind: "Remind the agent how to name inputs and payloads.",
    file: "naming.SKILL.md",
    body: "Mutation inputs and payloads follow the team naming rules.",
  },
  {
    remind: "Remind the agent which policy directive to apply.",
    file: "auth.SKILL.md",
    body: "Gate fields with the shared policy directives, not ad-hoc checks.",
  },
];

/* ------------------------------------------------------------------ *
 * Section 6 (Honesty): the five HONESTY_POINTS, restated as a paired
 * five-row cross/check ledger.
 * ------------------------------------------------------------------ */

interface HonestyRow {
  readonly promise: string;
  readonly proof: string;
}

const HONESTY_ROWS: readonly HonestyRow[] = [
  {
    promise: "Mint safe agent tools at runtime.",
    proof:
      "Tools and prompts are authored in the repo as reviewed code, not minted at runtime.",
  },
  {
    promise: "Catch broken tools later, in staging.",
    proof:
      "nitro mcp validate runs in CI, so a broken tool collection never reaches a stage.",
  },
  {
    promise: "Trust the agent to know read from write.",
    proof:
      "Behavior is declared with idempotentHint, destructiveHint, and openWorldHint.",
  },
  {
    promise: "Name every customer that breaks.",
    proof:
      "An edit is checked against published operations; risky changes read “published clients affected.”",
  },
  {
    promise: "Show generic dashboards after the fact.",
    proof:
      "Every tool call is traced in Nitro with p95 latency, error rate, and impact.",
  },
];

/* ------------------------------------------------------------------ *
 * Page
 * ------------------------------------------------------------------ */

export default function AgenticCodingPreviewV5() {
  return (
    <div className="mx-auto max-w-6xl">
      {/* ---------------------------------------------------------- *
       * Hero: split headline + dual status rails. No product mock.
       * ---------------------------------------------------------- */}
      <section className="py-14 sm:py-20">
        <div className="text-center">
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
            Agentic coding · preview · side by side
          </span>

          <h1 className="font-heading text-cc-heading text-hero mx-auto mt-7 max-w-4xl leading-[1.04] font-semibold tracking-tight text-balance">
            Coding agents, on a{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              leash you wrote
            </span>
            .
          </h1>

          <p className="text-cc-ink-dim text-lead mx-auto mt-6 max-w-3xl text-pretty">
            Two columns, one decision. Hand-rolled tool wiring on the left,
            grounded operations and a governed lifecycle on the right.
          </p>

          <div className="mt-9 flex flex-wrap justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>
        </div>

        <div className="relative mt-14 grid gap-6 lg:grid-cols-2 lg:gap-0">
          <span
            aria-hidden="true"
            className="bg-cc-card-border pointer-events-none absolute inset-y-0 left-1/2 hidden w-px lg:block"
          />
          <div className="lg:pr-8">
            <HeroRail variant="without" items={WITHOUT_RAIL} />
          </div>
          <div className="lg:pl-8">
            <HeroRail variant="with" items={WITH_RAIL} />
          </div>
        </div>
      </section>

      {/* ---------------------------------------------------------- *
       * Grounding compare
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16 sm:py-20">
        <LedgerHeader
          eyebrow="Grounding"
          leftTitle="Hand-rolled tool wiring"
          rightTitle="Operations as governed tools"
        />

        <LedgerRow
          left={
            <ColumnCard side="left">
              <ColumnHeader side="left" />
              <p className="text-cc-ink-dim text-body text-pretty">
                A coding agent that does not know your graph invents fields and
                writes queries no client would ship. Without grounding, every
                tool is a guess wrapped in a hand-maintained schema you copied
                out of band.
              </p>
              <ul className="mt-6 space-y-2.5">
                {OP_ROWS.map((row) => (
                  <li
                    key={row.name}
                    className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-3 rounded-xl border px-3.5 py-2.5"
                  >
                    <span className="text-cc-ink-dim font-mono text-xs">
                      {row.name}
                    </span>
                    <MutedChip>{row.leftLabel}</MutedChip>
                  </li>
                ))}
              </ul>
            </ColumnCard>
          }
          right={
            <ColumnCard side="right">
              <ColumnHeader side="right" />
              <p className="text-cc-ink text-body text-pretty">
                The schema and client registry change that. Your published
                operations become a catalog of callable tools, each one a real,
                reviewed shape your product already depends on, with behavior
                declared up front.
              </p>
              <ul className="mt-6 space-y-2.5">
                {OP_ROWS.map((row) => (
                  <li
                    key={row.name}
                    className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-3 rounded-xl border px-3.5 py-2.5"
                    style={{ borderColor: "rgba(124,146,198,0.22)" }}
                  >
                    <span
                      className="font-mono text-xs"
                      style={{ color: VIOLET }}
                    >
                      {row.name}
                    </span>
                    <AccentChip>{row.rightLabel}</AccentChip>
                  </li>
                ))}
              </ul>
            </ColumnCard>
          }
        />
      </section>

      {/* ---------------------------------------------------------- *
       * Surface compare
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16 sm:py-20">
        <LedgerHeader
          eyebrow="Surface"
          leftTitle="Bespoke MCP server"
          rightTitle="Your /graphql/mcp endpoint"
        />

        <LedgerRow
          left={
            <ColumnCard side="left">
              <ColumnHeader side="left" />
              <p className="text-cc-ink-faint mb-4 inline-flex items-center gap-2 font-mono text-[0.7rem]">
                <CrossGlyph />
                <span>https://tools.internal/mcp</span>
              </p>
              <ul className="space-y-4">
                {HANDROLL_COSTS.map((line) => (
                  <li key={line.label}>
                    <p className="text-cc-ink-faint font-mono text-[0.6rem] tracking-[0.14em] uppercase">
                      {line.label}
                    </p>
                    <p className="text-cc-ink-dim mt-1 text-sm/relaxed">
                      {line.text}
                    </p>
                  </li>
                ))}
              </ul>
              <div className="mt-6 flex flex-wrap gap-2">
                <MutedChip>parallel definitions</MutedChip>
                <MutedChip>second auth surface</MutedChip>
                <MutedChip>own telemetry</MutedChip>
              </div>
            </ColumnCard>
          }
          right={
            <ColumnCard side="right">
              <ColumnHeader side="right" />
              <p
                className="mb-4 inline-flex items-center gap-2 font-mono text-[0.7rem]"
                style={{ color: VIOLET }}
              >
                <CheckIcon size={11} />
                <span>https://api.yours.com/graphql/mcp</span>
              </p>
              <ul className="space-y-4">
                {PLATFORM_COSTS.map((line) => (
                  <li key={line.label}>
                    <p
                      className="font-mono text-[0.6rem] tracking-[0.14em] uppercase"
                      style={{ color: VIOLET }}
                    >
                      {line.label}
                    </p>
                    <p className="text-cc-ink mt-1 text-sm/relaxed">
                      {line.text}
                    </p>
                  </li>
                ))}
              </ul>
              <div className="mt-6 flex flex-wrap gap-2">
                <AccentChip>Streamable HTTP</AccentChip>
                <AccentChip>same registry</AccentChip>
                <AccentChip>traced in Nitro</AccentChip>
              </div>
            </ColumnCard>
          }
        />
      </section>

      {/* ---------------------------------------------------------- *
       * Behavior annotations compare
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16 sm:py-20">
        <LedgerHeader
          eyebrow="Behavior annotations"
          leftTitle="Unlabelled calls"
          rightTitle="Annotated by hint"
        />

        <LedgerRow
          left={
            <ColumnCard side="left">
              <ColumnHeader side="left" />
              <p className="text-cc-ink-dim text-body text-pretty">
                Without behavior hints, a read and a delete look the same to the
                agent. The model is left to infer intent from the name, which is
                exactly the kind of guess governance is meant to remove.
              </p>
              <ul className="mt-6 space-y-3">
                {ANNOTATIONS.map((row, idx) => {
                  const isDestructive = row.rightHint === "destructiveHint";
                  return (
                    <li
                      key={row.leftLabel}
                      className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-3 rounded-xl border px-3.5 py-2.5"
                    >
                      <span className="text-cc-ink-faint font-mono text-[0.62rem] tracking-[0.12em] uppercase">
                        0{idx + 1}
                      </span>
                      {isDestructive ? (
                        <CoralChip>{row.leftLabel}</CoralChip>
                      ) : (
                        <MutedChip>{row.leftLabel}</MutedChip>
                      )}
                    </li>
                  );
                })}
              </ul>
            </ColumnCard>
          }
          right={
            <ColumnCard side="right">
              <ColumnHeader side="right" />
              <p className="text-cc-ink text-body text-pretty">
                MCP exposes each operation with its intent declared. Agents tell
                a safe read from a write before they act, and the destructive
                ones stay clearly marked all the way to the trace.
              </p>
              <ul className="mt-6 space-y-3">
                {ANNOTATIONS.map((row, idx) => {
                  const isDestructive = row.rightHint === "destructiveHint";
                  return (
                    <li
                      key={row.rightHint}
                      className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-3 rounded-xl border px-3.5 py-2.5"
                      style={{ borderColor: "rgba(124,146,198,0.22)" }}
                    >
                      <span
                        className="font-mono text-[0.62rem] tracking-[0.12em] uppercase"
                        style={{ color: VIOLET }}
                      >
                        0{idx + 1}
                      </span>
                      {isDestructive ? (
                        <CoralChip>{row.rightHint}</CoralChip>
                      ) : (
                        <AccentChip>{row.rightHint}</AccentChip>
                      )}
                    </li>
                  );
                })}
              </ul>
              <p className="text-cc-ink-faint mt-5 font-mono text-[0.6rem] leading-relaxed">
                {ANNOTATIONS.map((row) => row.meaning).join(" · ")}
              </p>
            </ColumnCard>
          }
        />
      </section>

      {/* ---------------------------------------------------------- *
       * Lifecycle compare
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16 sm:py-20">
        <LedgerHeader
          eyebrow="Lifecycle"
          leftTitle="Drift between dev and prod"
          rightTitle="Author, Validate, Stage, Trace"
        />

        <LedgerRow
          left={
            <ColumnCard side="left">
              <ColumnHeader side="left" />
              <ol className="space-y-4">
                {LIFECYCLE_ROWS.map((row) => (
                  <li key={row.index} className="flex items-start gap-4">
                    <span className="text-cc-ink-faint mt-1 shrink-0 font-mono text-[0.62rem] tracking-[0.14em]">
                      {row.index}
                    </span>
                    <div>
                      <p className="text-cc-ink-dim text-body font-medium">
                        {row.leftTitle}
                      </p>
                      <p className="text-cc-ink-faint mt-1 font-mono text-[0.68rem] leading-relaxed">
                        {row.leftNote}
                      </p>
                    </div>
                  </li>
                ))}
              </ol>
            </ColumnCard>
          }
          right={
            <ColumnCard side="right">
              <ColumnHeader side="right" />
              <ol className="space-y-4">
                {LIFECYCLE_ROWS.map((row) => (
                  <li key={row.index} className="flex items-start gap-4">
                    <span
                      className="mt-1 shrink-0 font-mono text-[0.62rem] tracking-[0.14em]"
                      style={{ color: VIOLET }}
                    >
                      {row.index}
                    </span>
                    <div>
                      <p className="text-cc-heading font-heading text-h6 font-semibold">
                        {row.rightTitle}
                      </p>
                      <p className="text-cc-ink-dim mt-1 font-mono text-[0.68rem] leading-relaxed">
                        {row.rightNote}
                      </p>
                    </div>
                  </li>
                ))}
              </ol>
            </ColumnCard>
          }
        />
      </section>

      {/* ---------------------------------------------------------- *
       * skillz compare
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16 sm:py-20">
        <LedgerHeader
          eyebrow="Conventions"
          leftTitle="Re-explain conventions every PR"
          rightTitle="SKILL.md, installed once"
        />

        <LedgerRow
          left={
            <ColumnCard side="left">
              <ColumnHeader side="left" />
              <ul className="space-y-4">
                {SKILL_PAIRS.map((pair) => (
                  <li
                    key={pair.remind}
                    className="border-cc-card-border bg-cc-surface flex items-start gap-3 rounded-xl border px-3.5 py-3"
                  >
                    <span className="text-cc-ink-faint mt-1 shrink-0">
                      <CrossGlyph />
                    </span>
                    <p className="text-cc-ink-dim text-sm/relaxed">
                      {pair.remind}
                    </p>
                  </li>
                ))}
              </ul>
            </ColumnCard>
          }
          right={
            <ColumnCard side="right">
              <ColumnHeader side="right" />
              <ul className="space-y-4">
                {SKILL_PAIRS.map((pair) => (
                  <li
                    key={pair.file}
                    className="border-cc-card-border bg-cc-surface rounded-xl border px-3.5 py-3"
                    style={{ borderColor: "rgba(124,146,198,0.22)" }}
                  >
                    <p
                      className="font-mono text-[0.72rem]"
                      style={{ color: VIOLET }}
                    >
                      {pair.file}
                    </p>
                    <p className="text-cc-ink-dim mt-1.5 text-sm/relaxed">
                      {pair.body}
                    </p>
                  </li>
                ))}
              </ul>
            </ColumnCard>
          }
        />
      </section>

      {/* ---------------------------------------------------------- *
       * Honesty ledger
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16 sm:py-20">
        <LedgerHeader
          eyebrow="Honesty"
          leftTitle="What others promise"
          rightTitle="What the registries can prove"
        />

        <LedgerRow
          left={
            <ColumnCard side="left">
              <ColumnHeader side="left" />
              <ul className="space-y-4">
                {HONESTY_ROWS.map((row) => (
                  <li key={row.promise} className="flex items-start gap-3">
                    <span className="text-cc-ink-faint mt-1.5 shrink-0">
                      <CrossGlyph />
                    </span>
                    <p className="text-cc-ink-dim text-sm/relaxed text-pretty">
                      {row.promise}
                    </p>
                  </li>
                ))}
              </ul>
            </ColumnCard>
          }
          right={
            <ColumnCard side="right">
              <ColumnHeader side="right" />
              <ul className="space-y-4">
                {HONESTY_ROWS.map((row) => (
                  <li key={row.proof} className="flex items-start gap-3">
                    <span className="mt-0.5 shrink-0" style={{ color: VIOLET }}>
                      <CheckIcon />
                    </span>
                    <p className="text-cc-ink text-sm/relaxed text-pretty">
                      {row.proof}
                    </p>
                  </li>
                ))}
              </ul>
            </ColumnCard>
          }
        />
      </section>

      {/* ---------------------------------------------------------- *
       * Closing CTA: single column, breaks the ledger grammar on purpose.
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16 text-center sm:py-20">
        <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
          Decide
        </p>
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto mt-5 max-w-3xl leading-tight font-semibold text-balance">
          Pick the column you want to live in.
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
    </div>
  );
}
