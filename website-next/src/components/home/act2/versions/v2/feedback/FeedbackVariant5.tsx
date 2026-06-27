import type { ReactNode } from "react";

interface FeedbackVariant5Props {
  readonly className?: string;
}

/**
 * "Agentic coding" scene illustration, concept 5 "SKILL.md as source of truth",
 * v2 "Flow Diagrams".
 *
 * Two stacked mini-flows separated by a divider (the BEFORE/AFTER motif of the
 * locked v2 flow system). The top row is the ungrounded path: a coding agent
 * guessing, its connector dashed into a not-yet-reached "grounded behavior"
 * node. The bottom row is the grounded path and carries the single teal accent:
 * a reviewed, checked-in SKILL.md (frontmatter, a /graphql/mcp example, and the
 * createReview destructive hint) loads into the agent and grounds its behavior.
 * The teal traces only that one route: SKILL.md -> agent -> grounded.
 *
 * Built from the ScrollScenes Chip + Arrow vocabulary on cc-surface nodes. No
 * animation, no client hooks, settled final frame. Every svg id is prefixed
 * "v2-feedback-5-".
 */

/** Inline chip: the ScrollScenes pill extended for the v2 flow system. */
function Chip({
  children,
  active = false,
  derived = false,
  notReached = false,
}: {
  readonly children: ReactNode;
  readonly active?: boolean;
  readonly derived?: boolean;
  readonly notReached?: boolean;
}) {
  return (
    <span
      className={[
        "border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        derived ? "rounded-md px-2 py-1" : "rounded-lg",
        active
          ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
          : notReached
            ? "border-cc-ink-faint text-cc-ink-dim bg-cc-surface border-dashed"
            : "border-cc-card-border text-cc-ink bg-cc-surface",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

/** The grey (or teal) text-arrow connector, matching the ScrollScenes Arrow. */
function Arrow({ accent = false }: { readonly accent?: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={[
        "px-0.5 text-sm",
        accent ? "text-cc-accent" : "text-cc-ink-faint",
      ].join(" ")}
    >
      &rarr;
    </span>
  );
}

/** A dashed deferred connector (the not-reached hop) drawn as a thin 1px path. */
function DashedArrow() {
  return (
    <span aria-hidden="true" className="text-cc-ink-faint px-1">
      <svg
        width="26"
        height="8"
        viewBox="0 0 26 8"
        fill="none"
        className="inline-block align-middle"
      >
        <line
          x1="0"
          y1="4"
          x2="18"
          y2="4"
          stroke="currentColor"
          strokeWidth="1"
          strokeDasharray="3 2"
        />
        <path
          d="M18 1.5 L22 4 L18 6.5"
          stroke="currentColor"
          strokeWidth="1"
          fill="none"
        />
      </svg>
    </span>
  );
}

export function FeedbackVariant5({ className }: FeedbackVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          source of truth
        </p>

        {/* BEFORE: ungrounded agent guesses; grounded behavior never reached */}
        <div className="mt-4">
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
            without a skill
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-1">
            <Chip>coding agent</Chip>
            <Arrow />
            <Chip>guesses</Chip>
            <DashedArrow />
            <Chip notReached>grounded behavior</Chip>
          </div>
        </div>

        {/* divider between the two mini-flows */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="text-cc-accent font-mono text-[0.55rem] tracking-[0.12em] uppercase">
            with SKILL.md
          </p>

          {/* the reviewed, checked-in artifact: the single teal source node */}
          <div className="mt-2 flex justify-center">
            <span className="border-cc-accent/60 bg-cc-surface flex w-full max-w-[15rem] flex-col gap-1.5 rounded-lg border px-3 py-2">
              <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.08em] uppercase">
                reviewed &middot; checked in
              </span>
              <span className="text-cc-accent font-mono text-xs">SKILL.md</span>
              <span className="border-cc-card-border mt-0.5 flex flex-wrap gap-1 border-t pt-2">
                <Chip derived>frontmatter</Chip>
                <Chip derived>/graphql/mcp</Chip>
              </span>
              <span className="text-cc-ink-dim font-mono text-[0.6rem]">
                createReview{" "}
                <span className="text-cc-status-firing">@destructive</span>
              </span>
            </span>
          </div>

          {/* AFTER: the grounded path, teal-traced source -> agent -> grounded */}
          <div className="mt-3 flex flex-wrap items-center justify-center gap-1">
            <Arrow accent />
            <Chip>coding agent</Chip>
            <Arrow accent />
            <Chip active>grounded behavior</Chip>
          </div>
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            a checked-in artifact grounds the agent
          </p>
        </div>
      </div>
    </div>
  );
}
