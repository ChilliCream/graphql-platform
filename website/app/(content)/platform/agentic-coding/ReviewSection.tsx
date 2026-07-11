"use client";

import { useEffect, useRef, useState } from "react";

import { SectionHeading } from "@/src/components/SectionHeading";

interface HunkRun {
  readonly t: string;
  readonly accent?: boolean;
}

interface Hunk {
  readonly file: string;
  readonly added: number;
  readonly removed: number;
  readonly lines: readonly (readonly HunkRun[])[];
}

// Three files in one PR, and the feature spans the platform: the GraphQL
// mutation, the event handler reacting to it, and the query handler serving
// the summary. Different subsystems, same two-line shape (contract, then
// member). That is what makes the review a glance instead of a decode.
const HUNKS: readonly Hunk[] = [
  {
    file: "Reviews/AddReview.cs",
    added: 34,
    removed: 2,
    lines: [
      [{ t: "[" }, { t: "Mutation", accent: true }, { t: "]" }],
      [{ t: "static Task<Review> AddReviewAsync(...)" }],
    ],
  },
  {
    file: "Reviews/ReviewAddedHandler.cs",
    added: 21,
    removed: 0,
    lines: [
      [
        { t: "class ReviewAddedHandler : " },
        { t: "IEventHandler", accent: true },
        { t: "<ReviewAdded>" },
      ],
      [{ t: "ValueTask HandleAsync(ReviewAdded e, ...)" }],
    ],
  },
  {
    file: "Reviews/GetSummaryHandler.cs",
    added: 18,
    removed: 0,
    lines: [
      [
        { t: "class GetSummaryHandler : " },
        { t: "IQueryHandler", accent: true },
        { t: "<GetSummary, Summary>" },
      ],
      [{ t: "ValueTask<Summary> HandleAsync(...)" }],
    ],
  },
];

const TOTAL_ADDED = HUNKS.reduce((sum, hunk) => sum + hunk.added, 0);
const TOTAL_REMOVED = HUNKS.reduce((sum, hunk) => sum + hunk.removed, 0);

interface TimeBar {
  readonly label: string;
  readonly width: number;
  readonly fill: string;
}

// Write is short because the agent did it. Review is short because the change
// has one shape. The ghosted bar is the review you used to pay for.
const TIME_BARS: readonly TimeBar[] = [
  { label: "write", width: 22, fill: "rgba(245, 241, 234, 0.42)" },
  { label: "review", width: 18, fill: "var(--color-cc-accent)" },
  { label: "usual review", width: 84, fill: "rgba(245, 241, 234, 0.13)" },
];

// ---------------------------------------------------------------------------
// The review demo script. The window replays a real GitHub review: the PR
// opens "In Review" with the checks in progress, a pointer marks each file
// as viewed, CI completes along the way (build after the second file, tests
// and the schema check after the approval), the reviewer hits Approve, and
// the PR flips to Merged. The script plays once; the window stays merged.
// ---------------------------------------------------------------------------

type DemoStatus = "review" | "approved" | "merged";

type CursorTarget = "file-0" | "file-1" | "file-2" | "approve";

interface DemoState {
  readonly viewed: readonly [boolean, boolean, boolean];
  readonly buildDone: boolean;
  readonly testsDone: boolean;
  readonly schemaDone: boolean;
  readonly status: DemoStatus;
  readonly cursor: CursorTarget | null;
  readonly clicking: boolean;
}

const INITIAL_DEMO: DemoState = {
  viewed: [false, false, false],
  buildDone: false,
  testsDone: false,
  schemaDone: false,
  status: "review",
  cursor: null,
  clicking: false,
};

/** The end state, shown statically when the user prefers reduced motion. */
const FINISHED_DEMO: DemoState = {
  viewed: [true, true, true],
  buildDone: true,
  testsDone: true,
  schemaDone: true,
  status: "merged",
  cursor: null,
  clicking: false,
};

const STEPS: readonly {
  readonly at: number;
  readonly apply: (prev: DemoState) => DemoState;
}[] = [
  { at: 400, apply: (s) => ({ ...s, cursor: "file-0" }) },
  { at: 1400, apply: (s) => ({ ...s, clicking: true }) },
  {
    at: 1550,
    apply: (s) => ({ ...s, clicking: false, viewed: [true, false, false] }),
  },
  { at: 2200, apply: (s) => ({ ...s, cursor: "file-1" }) },
  { at: 3100, apply: (s) => ({ ...s, clicking: true }) },
  {
    at: 3250,
    apply: (s) => ({ ...s, clicking: false, viewed: [true, true, false] }),
  },
  // Build finishes while the reviewer is still reading.
  { at: 3900, apply: (s) => ({ ...s, buildDone: true }) },
  { at: 4200, apply: (s) => ({ ...s, cursor: "file-2" }) },
  { at: 5100, apply: (s) => ({ ...s, clicking: true }) },
  {
    at: 5250,
    apply: (s) => ({ ...s, clicking: false, viewed: [true, true, true] }),
  },
  // All files viewed; down to the Approve button.
  { at: 6000, apply: (s) => ({ ...s, cursor: "approve" }) },
  { at: 7000, apply: (s) => ({ ...s, clicking: true }) },
  { at: 7150, apply: (s) => ({ ...s, clicking: false, status: "approved" }) },
  // The reviewer's pointer leaves once the approval is in.
  { at: 7700, apply: (s) => ({ ...s, cursor: null }) },
  // The remaining checks land right after the approval.
  { at: 8000, apply: (s) => ({ ...s, testsDone: true }) },
  { at: 8800, apply: (s) => ({ ...s, schemaDone: true }) },
  { at: 9500, apply: (s) => ({ ...s, status: "merged" }) },
];

function FileGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="text-cc-nav-label size-3.5 shrink-0"
    >
      <path
        d="M4 2.5h5l3 3v8H4z"
        stroke="currentColor"
        strokeWidth={1.1}
        strokeLinejoin="round"
      />
      <path
        d="M9 2.5v3h3"
        stroke="currentColor"
        strokeWidth={1.1}
        strokeLinejoin="round"
      />
    </svg>
  );
}

function BranchGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="text-cc-ink-dim size-3.5 shrink-0"
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

function CheckGlyph({
  className = "text-cc-success size-3.5",
}: {
  readonly className?: string;
}) {
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

/** Small git-merge glyph for the merged status badge. */
function MergeGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className={className}
    >
      <circle
        cx="4.5"
        cy="3.5"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <circle
        cx="4.5"
        cy="12.5"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <circle
        cx="11.5"
        cy="8"
        r="1.5"
        stroke="currentColor"
        strokeWidth={1.1}
      />
      <path d="M4.5 5v6" stroke="currentColor" strokeWidth={1.1} />
      <path
        d="M4.5 6.5a3.5 3.5 0 0 0 3.5 3.5H10"
        stroke="currentColor"
        strokeWidth={1.1}
      />
    </svg>
  );
}

/** The animated pointer that plays the reviewer. */
function CursorGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
      className={className}
    >
      <path
        d="M5.5 3.2 17.6 11l-5.3 1.1 2.9 5.9-2.5 1.2-2.9-5.9-4.3 3.4z"
        fill="#f5f1ea"
        stroke="#0a0f1e"
        strokeWidth={1.2}
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Amber spinner, GitHub-style, for a check that is still running: a
 * three-quarter circle arc rotating in place. */
function PendingSpinner() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="size-3.5 shrink-0 animate-spin"
    >
      <circle
        cx="8"
        cy="8"
        r="5"
        stroke="#d9a441"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeDasharray="23.5"
      />
    </svg>
  );
}

/** The PR status badge in the title bar: In Review -> Approved -> Merged. */
function StatusBadge({ status }: { readonly status: DemoStatus }) {
  if (status === "approved") {
    return (
      <span className="border-cc-success/40 bg-cc-success/10 text-cc-success ml-auto inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.65rem] tracking-[0.1em] uppercase">
        <CheckGlyph />
        Approved
      </span>
    );
  }
  if (status === "merged") {
    return (
      <span
        className="ml-auto inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.65rem] tracking-[0.1em] uppercase"
        style={{
          color: "#7c92c6",
          borderColor: "rgba(124, 146, 198, 0.4)",
          background: "rgba(124, 146, 198, 0.1)",
        }}
      >
        <MergeGlyph className="size-3.5 shrink-0" />
        Merged
      </span>
    );
  }
  return (
    <span
      className="ml-auto inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.65rem] tracking-[0.1em] uppercase"
      style={{
        color: "#d9a441",
        borderColor: "rgba(217, 164, 65, 0.4)",
        background: "rgba(217, 164, 65, 0.1)",
      }}
    >
      <span className="size-1.5 animate-pulse rounded-full bg-current" />
      In Review
    </span>
  );
}

/** One file in the PR: GitHub-dark file box with header strip, added-line
 * green wash, and the Viewed checkbox the pointer ticks. */
function FileCard({
  hunk,
  viewed,
  pressed,
  checkboxRef,
}: {
  readonly hunk: Hunk;
  readonly viewed: boolean;
  readonly pressed: boolean;
  readonly checkboxRef: (el: HTMLElement | null) => void;
}) {
  return (
    <div className="border-cc-card-border overflow-hidden rounded-lg border select-none">
      <div className="border-cc-card-border flex items-center gap-2 border-b bg-white/[0.03] px-3 py-2">
        <FileGlyph />
        <span className="text-cc-nav-label min-w-0 truncate font-mono text-xs">
          {hunk.file}
        </span>
        <span className="flex shrink-0 items-center gap-1.5 font-mono text-[0.6rem]">
          <span className="text-cc-success">+{hunk.added}</span>
          <span className="text-cc-danger">-{hunk.removed}</span>
        </span>
        <span className="text-cc-ink-dim ml-auto flex shrink-0 items-center gap-1.5 font-mono text-[0.6rem]">
          <span
            ref={checkboxRef}
            className={`flex size-3.5 items-center justify-center rounded-[4px] border transition-all duration-150 ${
              viewed
                ? "border-cc-accent/60 bg-cc-accent/20"
                : "border-cc-ink-faint"
            } ${pressed ? "scale-90" : ""}`}
          >
            {viewed && <CheckGlyph className="text-cc-accent size-2.5" />}
          </span>
          Viewed
        </span>
      </div>
      <div
        className={`py-1 font-mono text-xs transition-opacity duration-300 ${
          viewed ? "opacity-45" : ""
        }`}
      >
        {hunk.lines.map((runs, lineIndex) => (
          <div
            key={lineIndex}
            className="flex min-w-0 items-start gap-2.5 px-3 py-0.5"
            style={{ background: "rgba(63, 185, 80, 0.1)" }}
          >
            <span className="text-cc-success shrink-0 select-none">+</span>
            <span className="min-w-0 break-words">
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
    </div>
  );
}

/** The CI gates on the PR; the schema check is the platform's own. */
const CHECKS = [
  { name: "Build", key: "buildDone" },
  { name: "Tests", key: "testsDone" },
  { name: "Schema Checks", key: "schemaDone" },
] as const;

/**
 * REVIEW SECTION: the payoff of uniform output, played out. A GitHub-dark PR
 * window replays the whole review (view each file, checks land, approve,
 * merge) while the time strip beside it makes the claim explicit: review is
 * a glance here.
 */
export function ReviewSection() {
  const [demo, setDemo] = useState<DemoState>(INITIAL_DEMO);
  const [started, setStarted] = useState(false);
  const windowRef = useRef<HTMLDivElement | null>(null);
  const cursorRef = useRef<HTMLDivElement | null>(null);
  const targetRefs = useRef<Partial<Record<CursorTarget, HTMLElement | null>>>(
    {},
  );

  // Start the script once the window scrolls into view.
  useEffect(() => {
    const el = windowRef.current;
    if (!el) {
      return;
    }
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry?.isIntersecting) {
          setStarted(true);
          observer.disconnect();
        }
      },
      { threshold: 0.3 },
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  // Play the review script once; with reduced motion, show the end state.
  useEffect(() => {
    if (!started) {
      return;
    }
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      const id = window.setTimeout(() => setDemo(FINISHED_DEMO), 0);
      return () => window.clearTimeout(id);
    }
    const timeouts = STEPS.map((step) =>
      window.setTimeout(() => setDemo(step.apply), step.at),
    );
    return () => {
      for (const id of timeouts) {
        window.clearTimeout(id);
      }
    };
  }, [started]);

  // The pointer glides between targets via a transform transition; positions
  // are written to the DOM directly so movement causes no re-renders.
  useEffect(() => {
    const cursorEl = cursorRef.current;
    const container = windowRef.current;
    if (!cursorEl || !container) {
      return;
    }
    if (!demo.cursor) {
      // Keep the last position; the pointer just fades out in place.
      return;
    }
    const target = targetRefs.current[demo.cursor];
    if (!target) {
      return;
    }
    const containerRect = container.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();
    const x = targetRect.left - containerRect.left + targetRect.width / 2 - 5;
    const y = targetRect.top - containerRect.top + targetRect.height / 2 - 3;
    cursorEl.style.transform = `translate(${x}px, ${y}px)`;
  }, [demo.cursor]);

  const approved = demo.status !== "review";

  return (
    <section className="py-12">
      <SectionHeading
        align="center"
        eyebrow="Review"
        title="Review stays fast because changes stay uniform."
        description="Every change an agent produces on this platform is the same small, uniform shape, so review stays a glance instead of an investigation. That matters because writing code is cheap now; reviewing it is where your time actually goes."
      />

      {/* The figure: the PR window replaying the review on the left, the
          time-to-ship payoff beside it. */}
      <div className="mt-10 grid grid-cols-1 gap-8 lg:grid-cols-3 lg:gap-10">
        <div
          ref={windowRef}
          className="border-cc-card-border relative flex flex-col overflow-hidden rounded-xl border shadow-[0_28px_70px_-28px_rgba(0,0,0,0.7)] lg:col-span-2"
        >
          {/* Title bar */}
          <div className="border-cc-card-border flex flex-wrap items-center gap-x-3 gap-y-2 border-b bg-white/[0.03] px-4 py-2.5">
            <BranchGlyph />
            <span className="text-cc-ink font-mono text-sm">
              feat: add product reviews
            </span>
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.1em] uppercase">
              3 files changed
            </span>
            <span className="font-mono text-[0.65rem]">
              <span className="text-cc-success">+{TOTAL_ADDED}</span>{" "}
              <span className="text-cc-danger">-{TOTAL_REMOVED}</span>
            </span>
            <StatusBadge status={demo.status} />
          </div>

          <div className="bg-cc-surface/60 grow space-y-3 p-5">
            {HUNKS.map((hunk, index) => (
              <FileCard
                key={hunk.file}
                hunk={hunk}
                viewed={demo.viewed[index] ?? false}
                pressed={demo.clicking && demo.cursor === `file-${index}`}
                checkboxRef={(el) => {
                  targetRefs.current[`file-${index}` as CursorTarget] = el;
                }}
              />
            ))}

            {/* CI results under the diff: the platform's schema check runs
                beside the usual build and tests. */}
            <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-4 select-none">
              <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.1em] uppercase">
                Checks
              </p>
              <ul className="mt-2.5 space-y-1.5">
                {CHECKS.map((check) => {
                  const done = demo[check.key];
                  return (
                    <li
                      key={check.name}
                      className="flex items-center gap-2.5 font-mono text-xs"
                    >
                      {done ? <CheckGlyph /> : <PendingSpinner />}
                      <span className="text-cc-ink">{check.name}</span>
                      <span className="text-cc-ink-dim ml-auto text-[0.6rem]">
                        {done ? "Succeeded" : "In progress"}
                      </span>
                    </li>
                  );
                })}
              </ul>
            </div>

            {/* The review verdict; the pointer ends the pass here. */}
            <div className="flex items-center justify-end pt-1 select-none">
              <span
                ref={(el) => {
                  targetRefs.current.approve = el;
                }}
                className={`inline-flex items-center gap-1.5 rounded-lg border px-4 py-1.5 font-mono text-xs transition-all duration-150 ${
                  demo.clicking && demo.cursor === "approve" ? "scale-95" : ""
                } ${
                  approved
                    ? "border-cc-success/40 bg-cc-success/10 text-cc-success"
                    : "border-cc-success/50 bg-cc-success/20 text-cc-success"
                }`}
              >
                {approved && <CheckGlyph />}
                {approved ? "Approved" : "Approve"}
              </span>
            </div>
          </div>

          {/* The reviewer's pointer. */}
          <div
            ref={cursorRef}
            aria-hidden="true"
            className={`pointer-events-none absolute top-0 left-0 z-10 transition-[transform,opacity] duration-700 ease-in-out ${
              demo.cursor ? "opacity-100" : "opacity-0"
            }`}
            style={{ transform: "translate(320px, 90px)" }}
          >
            <CursorGlyph
              className={`size-5 drop-shadow-md transition-transform duration-150 ${
                demo.clicking ? "scale-90" : ""
              }`}
            />
          </div>
        </div>

        {/* The payoff, plain beside the window. */}
        <div className="flex flex-col justify-center lg:px-2">
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.14em] uppercase">
            time to ship
          </p>
          <p className="font-heading text-cc-heading text-h5 mt-2 leading-tight font-semibold">
            reviewed in seconds
          </p>
          <div className="mt-5 space-y-3" aria-hidden="true">
            {TIME_BARS.map((bar) => (
              <div key={bar.label} className="flex items-center gap-3">
                <span className="text-cc-nav-label w-24 shrink-0 font-mono text-[0.6rem] tracking-[0.08em] uppercase">
                  {bar.label}
                </span>
                <span
                  className="h-2 flex-1 overflow-hidden rounded-full"
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
      </div>
    </section>
  );
}
