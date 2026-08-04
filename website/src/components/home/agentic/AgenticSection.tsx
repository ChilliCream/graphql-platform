"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";

import { AGENTS, AgentLogo } from "@/src/components/AgentLogo";
import { ArrowLink } from "@/src/components/ArrowLink";
import { CopyCommand } from "@/src/components/CopyCommand";
import { KeyValueChipCard } from "@/src/components/KeyValueChipCard";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { Card } from "@/src/design-system/Card";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { BranchGlyph } from "@/src/icons/BranchGlyph";
import { CheckGlyph } from "@/src/icons/CheckGlyph";

const FLIP_INTERVAL_MS = 2500;

// --- Agents: flipping headline + the logo row ------------------------------

// "Windsurf" is the widest agent name; the invisible sizer below reserves its
// width so the flipping headline never reflows.
const WIDEST_AGENT = AGENTS.find((agent) => agent.slug === "windsurf");

/**
 * The flipping headline slot. An invisible sizer reserves the widest name so the
 * line never reflows; the agent layers cross-fade in place over it.
 */
function FlipSlot({ activeIndex }: { readonly activeIndex: number }) {
  return (
    <span className="relative inline-flex align-baseline">
      <span
        aria-hidden="true"
        className="invisible inline-flex items-center gap-2 whitespace-nowrap"
      >
        {WIDEST_AGENT && (
          <AgentLogo agent={WIDEST_AGENT} className="size-7 sm:size-8" />
        )}
        {WIDEST_AGENT?.name}.
      </span>
      {AGENTS.map((agent, index) => {
        const active = index === activeIndex;
        return (
          <span
            key={agent.slug}
            aria-hidden={!active}
            className={[
              "text-cc-accent absolute inset-0 inline-flex items-center gap-2 whitespace-nowrap transition-all duration-500 ease-out",
              active
                ? "translate-y-0 opacity-100"
                : "pointer-events-none translate-y-2 opacity-0",
            ].join(" ")}
          >
            <AgentLogo agent={agent} className="size-7 sm:size-8" />
            {agent.name}.
          </span>
        );
      })}
    </span>
  );
}

/** The supported agents, grouped beside the start-now command. The seven
 * marks plus the "and many more" link fill the 3x3 grid completely. */
function AgentGroup() {
  return (
    <div>
      <ul className="grid grid-cols-2 gap-x-6 gap-y-3.5 sm:grid-cols-3">
        {AGENTS.map((agent) => (
          <li key={agent.slug} className="flex items-center gap-2.5">
            <AgentLogo agent={agent} className="size-6 shrink-0" />
            <span className="text-cc-ink font-mono text-sm">{agent.name}</span>
          </li>
        ))}
        <li className="sm:col-span-2">
          <Link
            href="/platform/agentic-coding"
            className="border-cc-ink-faint text-cc-ink-dim hover:border-cc-accent/60 hover:text-cc-accent flex h-full items-center justify-center gap-1.5 rounded-xl border border-dashed px-3 py-1.5 font-mono text-sm transition-colors"
          >
            and many more
            <ArrowRightIcon className="size-3.5" />
          </Link>
        </li>
      </ul>
    </div>
  );
}

// --- Facet 1: keep the time your agent saves you ---------------------------

interface HunkRun {
  readonly t: string;
  readonly accent?: boolean;
}

interface Hunk {
  readonly file: string;
  readonly lines: readonly (readonly HunkRun[])[];
}

// Two hunks are enough to show the shape: the GraphQL mutation and the event
// handler behind it, each a contract line plus the member. The structure
// repeats, so the review is a glance.
const HUNKS: readonly Hunk[] = [
  {
    file: "AddReview.cs",
    lines: [
      [{ t: "[" }, { t: "Mutation", accent: true }, { t: "]" }],
      [{ t: "static Task<Review> AddReviewAsync(...)" }],
    ],
  },
  {
    file: "ReviewAddedHandler.cs",
    lines: [
      [{ t: "class ReviewAddedHandler :" }],
      [
        { t: "  " },
        { t: "IEventHandler", accent: true },
        { t: "<ReviewAdded>" },
      ],
    ],
  },
];

interface TimeBar {
  readonly label: string;
  readonly width: number;
  readonly fill: string;
}

const TIME_BARS: readonly TimeBar[] = [
  { label: "write", width: 22, fill: "rgba(245, 241, 234, 0.42)" },
  { label: "review", width: 18, fill: "var(--color-cc-accent)" },
  { label: "usual review", width: 84, fill: "rgba(245, 241, 234, 0.13)" },
];

/** Compact PR figure: a uniform diff above the time it costs to review. */
function ReviewFacet() {
  return (
    <Card className="p-4 select-none">
      <div className="flex items-center gap-2">
        <BranchGlyph className="text-cc-ink-dim size-3 shrink-0" />
        <span className="text-cc-ink min-w-0 truncate font-mono text-[0.7rem]">
          feat: add reviews
        </span>
        <span className="border-cc-success/40 bg-cc-success/10 text-cc-success ml-auto inline-flex shrink-0 items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
          <CheckGlyph className="text-cc-success size-3" />
          Approved
        </span>
      </div>

      <div className="mt-3 space-y-2">
        {HUNKS.map((hunk) => (
          <div
            key={hunk.file}
            className="border-cc-ink-faint bg-cc-surface/40 rounded-lg border px-3 py-2 font-mono text-[0.6rem] leading-[1.6]"
          >
            <div className="text-cc-nav-label truncate">{hunk.file}</div>
            {hunk.lines.map((runs, lineIndex) => (
              <div key={lineIndex} className="flex gap-1.5">
                <span className="text-cc-ink-dim shrink-0 select-none">+</span>
                <span className="min-w-0 truncate whitespace-pre">
                  {runs.map((run, runIndex) => (
                    <span
                      key={runIndex}
                      className={run.accent ? "text-cc-accent" : "text-cc-ink"}
                    >
                      {run.t}
                    </span>
                  ))}
                </span>
              </div>
            ))}
          </div>
        ))}
      </div>

      <div
        className="border-cc-card-border mt-3 space-y-2 border-t pt-3"
        aria-hidden="true"
      >
        {TIME_BARS.map((bar) => (
          <div key={bar.label} className="flex items-center gap-2">
            <span className="text-cc-nav-label w-20 shrink-0 font-mono text-[0.55rem] tracking-[0.06em] uppercase">
              {bar.label}
            </span>
            <span
              className="h-1.5 flex-1 overflow-hidden rounded-full"
              style={{ background: "rgba(245, 241, 234, 0.06)" }}
            >
              <span
                className="block h-full rounded-full"
                style={{ width: `${bar.width}%`, background: bar.fill }}
              />
            </span>
          </div>
        ))}
      </div>
    </Card>
  );
}

// --- Facet 2: best practices your agent actually follows -------------------

type TokenColor = "kw" | "key";

interface CodeToken {
  readonly text: string;
  readonly color?: TokenColor;
}

interface Pattern {
  readonly label: string;
  readonly line: readonly CodeToken[];
}

const TOKEN_CLASS: Record<TokenColor, string> = {
  kw: "text-cc-ink-dim",
  key: "text-cc-accent",
};

const key = (text: string): CodeToken => ({ text, color: "key" });
const t = (text: string): CodeToken => ({ text });

// Eight representative platform patterns, four schema and four messaging. Each
// kind is its own uniform shape, and an agent here produces it the right way.
// Schema patterns are Hot Chocolate attributes; messaging patterns are the
// Mocha contracts (interfaces and the Saga base class), shown as they exist.
const PATTERNS: readonly Pattern[] = [
  { label: "Query", line: [t("["), key("Query"), t("]")] },
  { label: "DataLoader", line: [t("["), key("DataLoader"), t("]")] },
  { label: "Pagination", line: [t("["), key("UseConnection"), t("]")] },
  { label: "Authorization", line: [t("["), key("Authorize"), t("]")] },
  { label: "Message handler", line: [key("IEventHandler"), t("<T>")] },
  { label: "Saga", line: [key("Saga"), t("<TState>")] },
  { label: "Batch handler", line: [key("IBatchEventHandler"), t("<T>")] },
  { label: "Request handler", line: [key("IEventRequestHandler")] },
];

/** Subtle teal "follows the pattern" mark shown on each catalog tile. */
function FollowsMark() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="text-cc-accent/70 size-3 shrink-0"
    >
      <path
        d="M3 8.5 6.5 12 13 4.5"
        stroke="currentColor"
        strokeWidth={1.6}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Compact pattern catalog: eight tiles, each a label and its key attribute. */
function PatternsFacet() {
  return (
    <div className="grid grid-cols-2 gap-2.5">
      {PATTERNS.map((pattern) => (
        <KeyValueChipCard
          key={pattern.label}
          label={pattern.label}
          icon={<FollowsMark />}
          value={pattern.line.map((token, index) => (
            <span
              key={index}
              className={token.color ? TOKEN_CLASS[token.color] : "text-cc-ink"}
            >
              {token.text}
            </span>
          ))}
        />
      ))}
    </div>
  );
}

// --- Facet 3: the reviewed SKILL.md ----------------------------------------

const SKILL_VIOLET = "#8b8ff0";
const SKILL_VIOLET_SOFT = "#7c92c6";
const VAR_ACCENT = "var(--color-cc-accent)";
const VAR_HEADING = "var(--color-cc-heading)";
const VAR_INK = "var(--color-cc-ink)";
const VAR_INK_DIM = "var(--color-cc-ink-dim)";

/** One syntax-tinted run of text on a SKILL.md line. */
interface SkillToken {
  readonly text: string;
  readonly color: string;
}

// The authored SKILL.md, line by line. Violet frontmatter fences and keys, the
// teal skill name, cream markdown headings, and the bullet lines in ink.
const SKILL_LINES: readonly (readonly SkillToken[])[] = [
  [{ text: "---", color: SKILL_VIOLET_SOFT }],
  [
    { text: "name", color: SKILL_VIOLET },
    { text: ": ", color: VAR_INK_DIM },
    { text: "graphql-schema-design", color: VAR_ACCENT },
  ],
  [
    { text: "description", color: SKILL_VIOLET },
    { text: ": ", color: VAR_INK_DIM },
    { text: "design + review", color: VAR_INK },
  ],
  [{ text: "  schema changes", color: VAR_INK }],
  [{ text: "---", color: SKILL_VIOLET_SOFT }],
  [],
  [{ text: "# GraphQL Schema Design", color: VAR_HEADING }],
  [],
  [{ text: "## Mutations", color: VAR_HEADING }],
  [{ text: "- Return a payload type.", color: VAR_INK }],
  [{ text: "- Model errors as a union.", color: VAR_INK }],
];

/** A reviewed SKILL.md rendered as a compact code-editor card. */
function SkillFacet() {
  return (
    <MockWindowChrome
      shadow="none"
      surfaceClassName="bg-cc-card-bg select-none"
      headerClassName="bg-cc-surface/40 flex items-center justify-between gap-2.5 px-3 py-2.5"
      header={{
        variant: "custom",
        content: (
          <span className="inline-flex items-center gap-2">
            <span
              className="inline-flex size-[18px] items-center justify-center rounded-[5px] font-mono text-[0.5rem] font-bold"
              style={{
                background: "rgba(139, 143, 240, 0.14)",
                border: "1px solid rgba(139, 143, 240, 0.4)",
                color: SKILL_VIOLET,
              }}
            >
              MD
            </span>
            <span className="font-mono text-xs">
              <span className="text-cc-heading">SKILL</span>
              <span className="text-cc-ink-dim">.md</span>
            </span>
          </span>
        ),
      }}
      headerRight={
        <span className="text-cc-nav-label font-mono text-[0.6rem] whitespace-nowrap">
          skills/
        </span>
      }
      footerClassName="bg-cc-surface/40 flex items-center justify-between gap-2.5 px-3 py-1.5"
      footer={
        <>
          <span className="inline-flex items-center gap-3">
            <span className="text-cc-nav-label inline-flex items-center gap-1.5 font-mono text-[0.6rem] whitespace-nowrap">
              <BranchGlyph className="text-cc-ink-dim size-3 shrink-0" />
              main
            </span>
            <span className="text-cc-nav-label font-mono text-[0.6rem]">
              markdown
            </span>
          </span>
          <span className="text-cc-ink-dim inline-flex items-center gap-1.5 font-mono text-[0.6rem] whitespace-nowrap">
            <CheckGlyph className="text-cc-success size-3" />
            reviewed
          </span>
        </>
      }
    >
      {/* file body: gutter numbers + syntax-tinted lines */}
      <div className="py-2">
        {SKILL_LINES.map((tokens, i) => (
          <div key={`skill-line-${i}`} className="flex items-stretch">
            <span className="text-cc-nav-label border-cc-ink-faint w-7 shrink-0 border-r pr-2 text-right font-mono text-[0.6rem] leading-[19px]">
              {i + 1}
            </span>
            <span className="pl-3 font-mono text-[0.7rem] leading-[19px] whitespace-pre">
              {tokens.map((token, j) => (
                <span
                  key={`skill-tok-${i}-${j}`}
                  style={{ color: token.color }}
                >
                  {token.text}
                </span>
              ))}
            </span>
          </div>
        ))}
      </div>
    </MockWindowChrome>
  );
}

// --- Footer: start now ------------------------------------------------------

/** "Start now": the single command that checks the skills into your agent. */
function StartNowPanel() {
  return (
    <div>
      <p className="text-cc-heading font-heading text-lg font-semibold sm:text-xl">
        Add the skills to your agent.
      </p>
      <CopyCommand
        command="dnx skillz add chillicream/agent-skills"
        className="bg-cc-surface mt-5"
      />
      <ArrowLink href="/platform/agentic-coding" className="mt-5">
        Learn more
      </ArrowLink>
    </div>
  );
}

// --- Agentic section -------------------------------------------------------

interface Facet {
  readonly heading: string;
  /** Anchor of the related section on the agentic-coding page. */
  readonly href: string;
  readonly illustration: ReactNode;
}

const FACETS: readonly Facet[] = [
  {
    heading: "Keep the time your agent saves you.",
    href: "/platform/agentic-coding#review",
    illustration: <ReviewFacet />,
  },
  {
    heading: "Best practices your agent follows.",
    href: "/platform/agentic-coding#patterns",
    illustration: <PatternsFacet />,
  },
  {
    heading: "One reviewed skill teaches every agent.",
    href: "/platform/agentic-coding#skills",
    illustration: <SkillFacet />,
  },
];

/**
 * Agentic-coding landing section. The headline names one agent at a time,
 * flipping on a timer. Below it the start-now command sits beside the group of
 * supported agents, then three compact facets follow: the uniform PR diff that
 * keeps review fast, the catalog of platform patterns an agent follows, and the
 * reviewed SKILL.md that teaches them.
 */
export function AgenticSection() {
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    // Keep the headline static for users who prefer reduced motion.
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }
    const id = setInterval(() => {
      setActiveIndex((index) => (index + 1) % AGENTS.length);
    }, FLIP_INTERVAL_MS);
    return () => clearInterval(id);
  }, []);

  return (
    <PageSection className="pt-16 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.1] font-semibold text-balance">
            Built for <FlipSlot activeIndex={activeIndex} />
          </h2>
          <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
            Use Claude, Codex, Copilot, Cursor, Windsurf, or whatever you reach
            for. They all produce the same structured, best-practice code here,
            so the tool is your choice and the quality is not.
          </p>
        </div>

        <div className="mt-12 grid items-start gap-8 sm:mt-14 lg:grid-cols-2 lg:gap-12">
          <StartNowPanel />
          <AgentGroup />
        </div>

        <div className="mt-12 grid grid-cols-1 gap-5 sm:mt-14 lg:grid-cols-3">
          {FACETS.map((facet) => (
            <Link
              key={facet.heading}
              href={facet.href}
              className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover flex flex-col gap-4 rounded-3xl border p-5 transition-colors sm:p-6"
            >
              <h3 className="font-heading text-cc-heading text-h6 leading-snug font-semibold">
                {facet.heading}
              </h3>
              <div className="min-w-0">{facet.illustration}</div>
            </Link>
          ))}
        </div>
      </RevealOnScroll>
    </PageSection>
  );
}
