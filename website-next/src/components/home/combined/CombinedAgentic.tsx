"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

const FLIP_INTERVAL_MS = 2500;

// --- Agents: flipping headline + the logo row ------------------------------

interface Agent {
  readonly name: string;
  readonly slug: string;
  readonly logoSrc?: string;
}

// Every listed agent ships a checked-in mark under /agent-logos and renders the
// real SVG. The placeholder glyph below now only sizes the flipping headline.
const AGENTS: readonly Agent[] = [
  { name: "Claude", slug: "claude", logoSrc: "/agent-logos/claude.svg" },
  { name: "Codex", slug: "codex", logoSrc: "/agent-logos/codex.svg" },
  { name: "Copilot", slug: "copilot", logoSrc: "/agent-logos/copilot.svg" },
  { name: "Cursor", slug: "cursor", logoSrc: "/agent-logos/cursor.svg" },
  { name: "Windsurf", slug: "windsurf", logoSrc: "/agent-logos/windsurf.svg" },
  { name: "Gemini", slug: "gemini", logoSrc: "/agent-logos/gemini.svg" },
  { name: "Cline", slug: "cline", logoSrc: "/agent-logos/cline.svg" },
];

// The flipping headline reserves width with an invisible sizer; this abstract
// monochrome shape (not a brand mark) stands in for that sizer only.
const PLACEHOLDER_GLYPHS: Record<string, ReactNode> = {
  windsurf: (
    <>
      <path d="M4 12 12 6 20 12" />
      <path d="M4 18 12 12 20 18" />
    </>
  ),
};

interface PlaceholderGlyphProps {
  readonly slug: string;
  readonly className?: string;
}

/** Renders the abstract placeholder shape for an agent that has no logoSrc. */
function PlaceholderGlyph({ slug, className }: PlaceholderGlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      {PLACEHOLDER_GLYPHS[slug]}
    </svg>
  );
}

interface AgentLogoProps {
  readonly agent: Agent;
  readonly className?: string;
}

/** One agent mark: the real SVG when present, otherwise a placeholder glyph. */
function AgentLogo({ agent, className }: AgentLogoProps) {
  if (agent.logoSrc !== undefined) {
    return (
      // eslint-disable-next-line @next/next/no-img-element
      <img
        src={agent.logoSrc}
        alt={agent.name}
        className={["object-contain", className].filter(Boolean).join(" ")}
      />
    );
  }

  return <PlaceholderGlyph slug={agent.slug} className={className} />;
}

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
        <PlaceholderGlyph slug="windsurf" className="size-7 sm:size-8" />
        Windsurf.
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

/** The supported agents, grouped beside the start-now command. */
function AgentGroup() {
  return (
    <div>
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        Works with
      </p>
      <ul className="mt-5 grid grid-cols-2 gap-x-6 gap-y-3.5 sm:grid-cols-3">
        {AGENTS.map((agent) => (
          <li key={agent.slug} className="flex items-center gap-2.5">
            <AgentLogo
              agent={agent}
              className="text-cc-ink-dim size-6 shrink-0"
            />
            <span className="text-cc-ink font-mono text-sm">{agent.name}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

// --- Facet 1: keep the time your agent saves you ---------------------------

interface Hunk {
  readonly file: string;
  readonly query: string;
  readonly handler: string;
}

// Two hunks are enough to show the shape: every change is a query field and the
// handler behind it. The structure repeats, so the review is a glance.
const HUNKS: readonly Hunk[] = [
  {
    file: "AddReview.cs",
    query: "addReview: Review",
    handler: "AddReviewHandler",
  },
  {
    file: "ProductReviews.cs",
    query: "product.reviews",
    handler: "ProductReviewsHandler",
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

function BranchGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="text-cc-ink-dim size-3 shrink-0"
    >
      <circle
        cx="4.5"
        cy="3.6"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <circle
        cx="4.5"
        cy="12.4"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <circle
        cx="11.5"
        cy="3.6"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <path d="M4.5 5.1v5.8" stroke="currentColor" strokeWidth={1.1} />
      <path
        d="M11.5 5.1v1.3a3 3 0 0 1-3 3H6"
        stroke="currentColor"
        strokeWidth={1.1}
      />
    </svg>
  );
}

function CheckGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className={className}
    >
      <path
        d="M3.5 8.5 6.5 11.5 12.5 4.5"
        stroke="currentColor"
        strokeWidth={1.6}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Compact PR figure: a uniform diff above the time it costs to review. */
function ReviewFacet() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-4 select-none">
      <div className="flex items-center gap-2">
        <BranchGlyph />
        <span className="text-cc-ink min-w-0 truncate font-mono text-[0.7rem]">
          feat: add reviews
        </span>
        <span className="border-cc-status-healthy/40 bg-cc-status-healthy/10 text-cc-status-healthy ml-auto inline-flex shrink-0 items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
          <CheckGlyph className="text-cc-status-healthy size-3" />
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
            <div className="flex gap-1.5">
              <span className="text-cc-ink-dim shrink-0 select-none">+</span>
              <span className="text-cc-nav-label shrink-0">query</span>
              <span className="text-cc-ink min-w-0 truncate">{hunk.query}</span>
            </div>
            <div className="flex gap-1.5">
              <span className="text-cc-ink-dim shrink-0 select-none">+</span>
              <span className="text-cc-nav-label shrink-0">handler</span>
              <span className="text-cc-ink min-w-0 truncate">
                {hunk.handler}
              </span>
            </div>
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
    </div>
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
const PATTERNS: readonly Pattern[] = [
  { label: "Query", line: [t("["), key("Query"), t("]")] },
  { label: "DataLoader", line: [t("["), key("DataLoader"), t("]")] },
  { label: "Pagination", line: [t("["), key("UsePaging"), t("]")] },
  { label: "Authorization", line: [t("["), key("Authorize"), t("]")] },
  { label: "Message handler", line: [t("["), key("MessageHandler"), t("]")] },
  { label: "Saga", line: [t("["), key("Saga"), t("]")] },
  { label: "Batch handler", line: [t("["), key("BatchHandler"), t("]")] },
  { label: "Request handler", line: [t("["), key("RequestHandler"), t("]")] },
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
        <div
          key={pattern.label}
          className="border-cc-card-border bg-cc-card-bg flex flex-col gap-2 rounded-xl border p-3"
        >
          <div className="flex items-center justify-between gap-1.5">
            <span className="text-cc-nav-label min-w-0 truncate font-mono text-[0.55rem] tracking-[0.12em] uppercase">
              {pattern.label}
            </span>
            <FollowsMark />
          </div>
          <code className="block truncate font-mono text-[0.6rem]">
            {pattern.line.map((token, index) => (
              <span
                key={index}
                className={
                  token.color ? TOKEN_CLASS[token.color] : "text-cc-ink"
                }
              >
                {token.text}
              </span>
            ))}
          </code>
        </div>
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
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border select-none">
      {/* window chrome: MD badge + file name, checked-in path on the right */}
      <div className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between gap-2.5 border-b px-3 py-2.5">
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
        <span className="text-cc-nav-label font-mono text-[0.6rem] whitespace-nowrap">
          .claude/skills/
        </span>
      </div>

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

      {/* status bar: checked-in branch + reviewed-like-code status */}
      <div className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between gap-2.5 border-t px-3 py-1.5">
        <span className="inline-flex items-center gap-3">
          <span className="text-cc-nav-label inline-flex items-center gap-1.5 font-mono text-[0.6rem] whitespace-nowrap">
            <BranchGlyph />
            main
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem]">
            markdown
          </span>
        </span>
        <span className="text-cc-ink-dim inline-flex items-center gap-1.5 font-mono text-[0.6rem] whitespace-nowrap">
          <CheckGlyph className="text-cc-status-healthy size-3" />
          reviewed
        </span>
      </div>
    </div>
  );
}

// --- Footer: start now ------------------------------------------------------

/** A plain two-rectangle copy affordance; decorative, inherits currentColor. */
function CopyGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <rect x="9" y="9" width="11" height="11" rx="2" />
      <path d="M5 15V5a2 2 0 0 1 2-2h8" />
    </svg>
  );
}

/** "Start now": the single command that checks the skills into your agent. */
function StartNowPanel() {
  return (
    <div>
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        Start now
      </p>
      <p className="text-cc-heading font-heading mt-3 text-lg font-semibold sm:text-xl">
        Add the skills to your agent.
      </p>
      <div className="bg-cc-surface border-cc-card-border relative mt-5 rounded-xl border p-4 font-mono">
        <CopyGlyph className="text-cc-ink-faint absolute top-3 right-3 size-4" />
        <code className="block pr-7 text-sm leading-relaxed break-words">
          <span className="text-cc-ink-faint select-none">$ </span>
          <span className="text-cc-accent">dnx</span>
          <span className="text-cc-ink"> skillz chillicream/agent-skills</span>
        </code>
      </div>
      <Link
        href="/platform/agentic-coding"
        className="text-cc-accent hover:text-cc-accent-hover mt-5 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
      >
        Learn more
        <span aria-hidden="true">-&gt;</span>
      </Link>
    </div>
  );
}

// --- Combined section ------------------------------------------------------

interface Facet {
  readonly heading: string;
  readonly illustration: ReactNode;
}

const FACETS: readonly Facet[] = [
  {
    heading: "Keep the time your agent saves you.",
    illustration: <ReviewFacet />,
  },
  {
    heading: "Best practices your agent actually follows.",
    illustration: <PatternsFacet />,
  },
  {
    heading: "One reviewed skill teaches every agent.",
    illustration: <SkillFacet />,
  },
];

/**
 * Combined agentic-coding section. The headline names one agent at a time,
 * flipping on a timer. Below it the start-now command sits beside the group of
 * supported agents, then three compact facets follow: the uniform PR diff that
 * keeps review fast, the catalog of platform patterns an agent follows, and the
 * reviewed SKILL.md that teaches them.
 */
export function CombinedAgentic() {
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    const id = setInterval(() => {
      setActiveIndex((index) => (index + 1) % AGENTS.length);
    }, FLIP_INTERVAL_MS);
    return () => clearInterval(id);
  }, []);

  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Agentic coding
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
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
            <div
              key={facet.heading}
              className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover flex flex-col gap-4 rounded-3xl border p-5 transition-colors sm:p-6"
            >
              <h3 className="font-heading text-cc-heading text-h6 leading-snug font-semibold">
                {facet.heading}
              </h3>
              <div className="min-w-0">{facet.illustration}</div>
            </div>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
