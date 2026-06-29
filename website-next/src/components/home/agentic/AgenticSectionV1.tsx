import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

interface Hunk {
  readonly file: string;
  readonly query: string;
  readonly handler: string;
}

// Three files in one PR, every hunk the same small shape: a query field and the
// handler behind it. The names differ; the structure does not. That is what
// makes the review a glance instead of a decode.
const HUNKS: readonly Hunk[] = [
  {
    file: "Reviews/AddReview.cs",
    query: "addReview: Review",
    handler: "AddReviewHandler",
  },
  {
    file: "Reviews/ProductReviews.cs",
    query: "product.reviews",
    handler: "ProductReviewsHandler",
  },
  {
    file: "Reviews/ReviewSummary.cs",
    query: "reviewSummary",
    handler: "ReviewSummaryHandler",
  },
];

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

function CheckGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="text-cc-status-healthy size-3.5"
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

/** One diff hunk: a query line and its handler line, both added, same shape. */
function HunkCard({ hunk }: { readonly hunk: Hunk }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-4 select-none">
      <div className="flex items-center gap-2">
        <FileGlyph />
        <span className="text-cc-nav-label min-w-0 truncate font-mono text-xs">
          {hunk.file}
        </span>
        <span className="text-cc-ink-dim ml-auto shrink-0 font-mono text-[0.6rem]">
          +2
        </span>
      </div>

      <div className="mt-3 space-y-1 font-mono text-xs">
        <div className="flex min-w-0 items-start gap-2.5">
          <span className="text-cc-ink-dim shrink-0 select-none">+</span>
          <span className="text-cc-nav-label w-16 shrink-0">query</span>
          <span className="text-cc-ink min-w-0 break-words">{hunk.query}</span>
        </div>
        <div className="flex min-w-0 items-start gap-2.5">
          <span className="text-cc-ink-dim shrink-0 select-none">+</span>
          <span className="text-cc-nav-label w-16 shrink-0">handler</span>
          <span className="text-cc-ink min-w-0 break-words">
            {hunk.handler}
          </span>
        </div>
      </div>
    </div>
  );
}

/**
 * Agentic coding (take 1): the review is fast because the code is uniform. A
 * pull request where every hunk is the same small shape (a query and its
 * handler), an approved check, and a time strip showing that review stays short
 * here while a usual review runs long.
 */
export function AgenticSectionV1() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Agentic coding
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Keep the time your agent saves you.
          </h2>
          <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
            Agents made writing code cheap, so the expensive part is reviewing
            it, and a slow review gives the time right back. On ChilliCream
            every change is the same small, uniform shape, so a review is a
            glance, and the time your agent saved you stays saved.
          </p>
        </div>

        {/* The PR figure: uniform diff on the left, the time it costs on the
            right. The shape repeating is the reason review stays short. */}
        <div className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover mt-10 rounded-3xl border p-5 transition-colors sm:mt-14 sm:p-8">
          <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
            <BranchGlyph />
            <span className="text-cc-ink font-mono text-sm">
              feat: add product reviews
            </span>
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.1em] uppercase">
              3 files changed
            </span>
            <span className="border-cc-status-healthy/40 bg-cc-status-healthy/10 text-cc-status-healthy ml-auto inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.65rem] tracking-[0.1em] uppercase">
              <CheckGlyph />
              Approved
            </span>
          </div>

          <div className="border-cc-card-border mt-5 grid gap-6 border-t pt-5 lg:grid-cols-3 lg:gap-8">
            <div className="space-y-3 lg:col-span-2">
              {HUNKS.map((hunk) => (
                <HunkCard key={hunk.file} hunk={hunk} />
              ))}
            </div>

            <div className="flex flex-col gap-4">
              <div>
                <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.14em] uppercase">
                  time to ship
                </p>
                <p className="font-heading text-cc-heading text-h5 mt-2 leading-tight font-semibold">
                  reviewed in seconds
                </p>
              </div>

              <div className="space-y-3" aria-hidden="true">
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

              <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.06em]">
                teal is this PR &middot; faint is a usual review
              </p>
            </div>
          </div>
        </div>

        <div className="mt-10">
          <Link
            href="/platform/agentic-coding"
            className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Open agentic coding
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>
      </RevealOnScroll>
    </section>
  );
}
