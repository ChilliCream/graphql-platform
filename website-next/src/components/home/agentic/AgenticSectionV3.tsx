"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { FeedbackVariant5 } from "@/src/components/home/act2/versions/v6/feedback/FeedbackVariant5";

const FLIP_INTERVAL_MS = 2500;

interface Agent {
  readonly name: string;
  readonly slug: string;
  readonly logoSrc?: string;
}

// Drop official SVGs into public/agent-logos/<slug>.svg and set logoSrc to use
// real marks. While logoSrc is unset, each agent falls back to a simple original
// monochrome placeholder glyph below (an abstract shape, never a brand logo).
const AGENTS: readonly Agent[] = [
  { name: "Claude", slug: "claude", logoSrc: "/agent-logos/claude.svg" },
  { name: "Codex", slug: "codex" },
  { name: "Copilot", slug: "copilot", logoSrc: "/agent-logos/copilot.svg" },
  { name: "Cursor", slug: "cursor", logoSrc: "/agent-logos/cursor.svg" },
  { name: "Windsurf", slug: "windsurf", logoSrc: "/agent-logos/windsurf.svg" },
  { name: "Gemini", slug: "gemini", logoSrc: "/agent-logos/gemini.svg" },
  { name: "Aider", slug: "aider" },
  { name: "Cline", slug: "cline", logoSrc: "/agent-logos/cline.svg" },
];

// Original, abstract, monochrome placeholder glyphs keyed by slug. These are
// intentionally generic geometric shapes, NOT copies of any official brand mark.
// They render in currentColor, so they pick up the accent tint in the headline
// and the dimmed ink color in the wall.
const PLACEHOLDER_GLYPHS: Record<string, ReactNode> = {
  claude: (
    <>
      <circle cx="9" cy="12" r="5.5" />
      <circle cx="15" cy="12" r="5.5" />
    </>
  ),
  codex: <path d="M12 3 19.5 7.5 19.5 16.5 12 21 4.5 16.5 4.5 7.5Z" />,
  copilot: <path d="M12 4 20 19 4 19Z" />,
  cursor: <path d="M12 3 21 12 12 21 3 12Z" />,
  windsurf: (
    <>
      <path d="M4 12 12 6 20 12" />
      <path d="M4 18 12 12 20 18" />
    </>
  ),
  gemini: (
    <>
      <path d="M9 4V20" />
      <path d="M15 4V20" />
    </>
  ),
  aider: (
    <>
      <path d="M12 4V20" />
      <path d="M4 12H20" />
    </>
  ),
  cline: (
    <>
      <circle cx="12" cy="12" r="8" />
      <path d="M4 12H20" />
    </>
  ),
};

interface PlaceholderGlyphProps {
  readonly slug: string;
  readonly className?: string;
}

/** Renders the abstract placeholder shape for an agent that has no logoSrc yet. */
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

/**
 * One agent mark. With logoSrc set, the official SVG from
 * /public/agent-logos/<slug>.svg renders as a plain img; otherwise the abstract
 * placeholder glyph stands in. Used by both the flipping headline and the wall.
 */
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

/**
 * "Start now" panel: a single command that installs the checked-in skills into
 * whichever agent you reached for above.
 */
function StartNowPanel() {
  return (
    <div>
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        Start now
      </p>
      <p className="text-cc-heading font-heading mt-3 text-lg font-semibold">
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
    </div>
  );
}

/**
 * "Built for any agent" take of the agentic-coding section. The headline names
 * one agent at a time, flipping on a timer, while every supported agent stays
 * visible together in the wall below. One command checks the skills in, and the
 * resulting SKILL.md teaches every agent the same conventions.
 */
export function AgenticSectionV3() {
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
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Agentic coding
        </p>
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
          Built for <FlipSlot activeIndex={activeIndex} />
        </h2>
        <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
          Use Claude, Codex, Copilot, Cursor, Windsurf, or whatever you reach
          for. They all produce the same structured, best-practice code here, so
          the tool is your choice and the quality is not.
        </p>

        <ul className="mt-12 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {AGENTS.map((agent) => (
            <li key={agent.slug}>
              <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex items-center gap-3 rounded-2xl border px-4 py-4 transition-colors">
                <AgentLogo
                  agent={agent}
                  className="text-cc-ink-dim size-7 shrink-0"
                />
                <span className="text-cc-ink font-mono text-sm">
                  {agent.name}
                </span>
              </div>
            </li>
          ))}
        </ul>

        <div className="mt-16 grid grid-cols-1 items-center gap-8 lg:grid-cols-2">
          <StartNowPanel />
          <div>
            <FeedbackVariant5 />
          </div>
        </div>
        <p className="text-cc-ink-dim mx-auto mt-8 max-w-2xl text-center text-sm text-pretty">
          Run it once; one checked-in SKILL.md then teaches every agent the same
          conventions.
        </p>

        <Link
          href="/platform/agentic-coding"
          className="text-cc-accent hover:text-cc-accent-hover mt-12 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
        >
          Open agentic coding
          <span aria-hidden="true">-&gt;</span>
        </Link>
      </RevealOnScroll>
    </section>
  );
}
