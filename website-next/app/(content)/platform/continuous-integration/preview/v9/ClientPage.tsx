"use client";

import type { ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Registry Codex                                                      */
/* A chapter book about GraphQL schema evolution. Single narrow        */
/* column of vellum on cc-bg, Josefin display headings, cc-accent      */
/* dropcaps, fleuron-plus-dots ornaments between chapters. The single  */
/* brand spectrum (cyan to violet to coral) appears exactly once, as   */
/* the underline beneath the version chip row in Chapter IV.           */
/* ------------------------------------------------------------------ */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* ------------------------------------------------------------------ */
/* Motion helpers (enter-view-once only, never scroll-coupled)         */
/* ------------------------------------------------------------------ */

const FADE_VIEWPORT = { once: true, amount: 0.4 } as const;

function useFadeUp() {
  const reduce = useReducedMotion();
  if (reduce) {
    return {
      initial: { opacity: 1, y: 0 },
      whileInView: { opacity: 1, y: 0 },
      viewport: FADE_VIEWPORT,
    } as const;
  }
  return {
    initial: { opacity: 0, y: 12 },
    whileInView: { opacity: 1, y: 0 },
    viewport: FADE_VIEWPORT,
    transition: { duration: 0.6, ease: "easeOut" },
  } as const;
}

/* ------------------------------------------------------------------ */
/* Ornament: three dots, fleuron trefoil, three dots                   */
/* ------------------------------------------------------------------ */

function Fleuron() {
  return (
    <svg
      viewBox="0 0 48 24"
      width={48}
      height={24}
      className="text-cc-accent"
      aria-hidden
    >
      <path
        d="M24 4 C20 4 17 7 17 11 C17 15 20 18 24 20 C28 18 31 15 31 11 C31 7 28 4 24 4 Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
      />
      <path
        d="M24 20 C24 16 21 13 16 12 M24 20 C24 16 27 13 32 12"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <circle cx="24" cy="11" r="1.6" fill="currentColor" />
    </svg>
  );
}

function Dot() {
  return <span className="bg-cc-accent h-1 w-1 rounded-full" aria-hidden />;
}

function Ornament() {
  const fade = useFadeUp();
  return (
    <motion.div
      {...fade}
      className="flex items-center justify-center gap-3 py-2 opacity-45"
      aria-hidden
    >
      <Dot />
      <Dot />
      <Dot />
      <Fleuron />
      <Dot />
      <Dot />
      <Dot />
    </motion.div>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter scaffold                                                    */
/* ------------------------------------------------------------------ */

interface ChapterHeadProps {
  readonly numeral: string;
  readonly title: string;
}

function ChapterHead({ numeral, title }: ChapterHeadProps) {
  const reduce = useReducedMotion();
  return (
    <motion.div
      initial={reduce ? { opacity: 1, y: 0 } : { opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={FADE_VIEWPORT}
      transition={reduce ? undefined : { duration: 0.6, ease: "easeOut" }}
      className="text-center"
    >
      <p className="text-cc-accent font-mono text-[0.72rem] tracking-[0.32em] uppercase">
        Chapter {numeral}
      </p>
      <h2 className="font-heading text-h2 text-cc-heading mt-4 font-semibold tracking-tight">
        {title}
      </h2>
      <div className="bg-cc-card-border mx-auto mt-6 h-px w-16" aria-hidden />
    </motion.div>
  );
}

interface DropcapParagraphProps {
  readonly cap: string;
  readonly children: ReactNode;
}

function DropcapParagraph({ cap, children }: DropcapParagraphProps) {
  const reduce = useReducedMotion();
  return (
    <p className="text-body text-cc-ink mt-8 leading-relaxed">
      <motion.span
        initial={
          reduce ? { opacity: 1, scale: 1 } : { opacity: 0, scale: 0.92 }
        }
        whileInView={{ opacity: 1, scale: 1 }}
        viewport={FADE_VIEWPORT}
        transition={reduce ? undefined : { duration: 0.6, ease: "easeOut" }}
        className="text-cc-accent font-heading float-left mr-3 font-bold"
        style={{ fontSize: "5rem", lineHeight: 0.9 }}
        aria-hidden
      >
        {cap}
      </motion.span>
      {children}
    </p>
  );
}

interface MarginNoteProps {
  readonly children: ReactNode;
}

/**
 * Hangs in the right gutter on desktop (absolute), folds inline as an
 * italic mono callout on mobile.
 */
function MarginNote({ children }: MarginNoteProps) {
  return (
    <aside
      className="text-cc-nav-label border-cc-card-border mt-6 border-l pl-4 font-mono text-[0.7rem] leading-relaxed italic lg:absolute lg:top-0 lg:left-full lg:mt-0 lg:ml-10 lg:w-52 lg:border-l lg:pl-4"
      role="note"
    >
      {children}
    </aside>
  );
}

interface ChapterProps {
  readonly numeral: string;
  readonly title: string;
  readonly cap: string;
  readonly body: ReactNode;
  readonly marginNote?: ReactNode;
  readonly figure?: ReactNode;
}

function Chapter({
  numeral,
  title,
  cap,
  body,
  marginNote,
  figure,
}: ChapterProps) {
  return (
    <section className="relative py-24">
      <ChapterHead numeral={numeral} title={title} />
      <DropcapParagraph cap={cap}>{body}</DropcapParagraph>
      {marginNote}
      {figure}
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter II figure: classified schema diff (three staggered rows)   */
/* ------------------------------------------------------------------ */

interface DiffRow {
  readonly tag: string;
  readonly path: string;
  readonly note: string;
  readonly text: string;
  readonly bg: string;
  readonly ring: string;
}

const DIFF_ROWS: readonly DiffRow[] = [
  {
    tag: "SAFE",
    path: "Order.totalAmount: Money!",
    note: "field added",
    text: "text-cc-success",
    bg: "bg-cc-success/10",
    ring: "ring-cc-success/30",
  },
  {
    tag: "DANGEROUS",
    path: "Order.status: Status!",
    note: "enum value narrowed",
    text: "text-cc-warning",
    bg: "bg-cc-warning/10",
    ring: "ring-cc-warning/30",
  },
  {
    tag: "BREAKING",
    path: "Order.total: Float!",
    note: "field removed, 3 published clients affected",
    text: "text-cc-danger",
    bg: "bg-cc-danger/10",
    ring: "ring-cc-danger/30",
  },
];

function DiffFigure() {
  const reduce = useReducedMotion();
  return (
    <motion.figure
      initial="hidden"
      whileInView="show"
      viewport={FADE_VIEWPORT}
      variants={{
        hidden: {},
        show: { transition: { staggerChildren: reduce ? 0 : 0.08 } },
      }}
      className="border-cc-card-border bg-cc-surface mt-10 overflow-hidden rounded-xl border"
    >
      {DIFF_ROWS.map((row) => (
        <motion.div
          key={row.path}
          variants={{
            hidden: reduce ? { opacity: 1, y: 0 } : { opacity: 0, y: 8 },
            show: { opacity: 1, y: 0 },
          }}
          transition={reduce ? undefined : { duration: 0.4, ease: "easeOut" }}
          className="border-cc-card-border grid grid-cols-[auto_minmax(0,1fr)] items-center gap-3 border-b px-4 py-3 last:border-b-0"
        >
          <span
            className={`rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${row.bg} ${row.text} ${row.ring}`}
          >
            {row.tag}
          </span>
          <div className="min-w-0">
            <div className="text-cc-heading truncate font-mono text-[0.78rem]">
              {row.path}
            </div>
            <div className="text-cc-ink-dim font-mono text-[0.66rem]">
              {row.note}
            </div>
          </div>
        </motion.div>
      ))}
      <figcaption className="text-cc-nav-label border-cc-card-border border-t px-4 py-2.5 font-mono text-[0.62rem] tracking-[0.14em] uppercase">
        nitro schema validate, 1 safe, 1 dangerous, 1 breaking
      </figcaption>
    </motion.figure>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter III figure: two-line CLI block                              */
/* ------------------------------------------------------------------ */

function CliFigure() {
  const fade = useFadeUp();
  return (
    <motion.figure
      {...fade}
      className="border-cc-card-border bg-cc-surface mt-10 overflow-hidden rounded-xl border"
    >
      <div className="border-cc-card-border text-cc-ink-dim border-b px-4 py-2.5 font-mono text-[0.66rem]">
        ci runner, upload candidate
      </div>
      <pre className="text-cc-ink overflow-x-auto px-4 py-4 font-mono text-[0.74rem] leading-relaxed">
        <code>
          <span className="text-cc-nav-label/70 select-none">$ </span>
          dotnet nitro schema upload --stage staging{"\n"}
          <span className="text-cc-nav-label/70 select-none">$ </span>
          dotnet nitro client upload --stage staging
        </code>
      </pre>
    </motion.figure>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter IV figure: version chip row with the single spectrum line  */
/* ------------------------------------------------------------------ */

interface VersionChip {
  readonly version: string;
  readonly status: string;
}

const VERSION_CHIPS: readonly VersionChip[] = [
  { version: "v13", status: "published" },
  { version: "v14", status: "candidate" },
  { version: "v15", status: "draft" },
];

function VersionFigure() {
  const fade = useFadeUp();
  return (
    <motion.figure {...fade} className="mt-10">
      <div className="flex flex-wrap items-center gap-3">
        {VERSION_CHIPS.map((chip) => (
          <span
            key={chip.version}
            className="border-cc-card-border bg-cc-surface text-cc-ink-dim inline-flex items-center gap-2 rounded-full border px-3 py-1.5 font-mono text-[0.72rem]"
          >
            <span className="text-cc-heading font-semibold">
              {chip.version}
            </span>
            <span className="text-cc-nav-label">{chip.status}</span>
          </span>
        ))}
      </div>
      {/* The single brand-spectrum event on this page. */}
      <div
        className="mt-4 h-px w-full rounded-full"
        style={{ backgroundImage: SPECTRUM }}
        aria-hidden
      />
      <figcaption className="text-cc-nav-label mt-3 font-mono text-[0.62rem] tracking-[0.14em] uppercase">
        publish promotes the candidate to published
      </figcaption>
    </motion.figure>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter V figure: dev, QA, prod ladder                              */
/* ------------------------------------------------------------------ */

interface LadderStep {
  readonly env: string;
  readonly status: string;
}

const LADDER: readonly LadderStep[] = [
  { env: "dev", status: "auto-publish from main, v14 active" },
  { env: "qa", status: "promoted after dev, v14 active" },
  { env: "prod", status: "publish after approval, v13 active" },
];

function LadderFigure() {
  const fade = useFadeUp();
  return (
    <motion.figure
      {...fade}
      className="border-cc-card-border bg-cc-surface mt-10 overflow-hidden rounded-xl border"
    >
      {LADDER.map((step, i) => (
        <div
          key={step.env}
          className="border-cc-card-border flex items-baseline justify-between gap-4 px-5 py-4 last:border-b-0"
          style={{
            borderBottomWidth: i < LADDER.length - 1 ? 1 : 0,
          }}
        >
          <span className="text-cc-heading font-mono text-[0.82rem] tracking-[0.12em] uppercase">
            {step.env}
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.7rem]">
            {step.status}
          </span>
        </div>
      ))}
    </motion.figure>
  );
}

/* ------------------------------------------------------------------ */
/* Frontispiece                                                        */
/* ------------------------------------------------------------------ */

function Frontispiece() {
  const fade = useFadeUp();
  return (
    <motion.section {...fade} className="pt-16 pb-8 text-center">
      <p className="text-cc-accent font-mono text-[0.72rem] tracking-[0.32em] uppercase">
        A Field Guide
      </p>
      <h1 className="font-heading text-hero text-cc-heading mt-6 font-bold tracking-tight">
        The Registry Codex
      </h1>
      <p className="lead text-cc-ink-dim mx-auto mt-6 max-w-2xl">
        A chapter book on GraphQL schema registry CI with Nitro: how a change
        travels from a developer laptop to production without breaking the
        clients you already published.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="#chapter-i">Read the chapters</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Open Nitro docs
        </OutlineButton>
      </div>
    </motion.section>
  );
}

/* ------------------------------------------------------------------ */
/* Colophon                                                            */
/* ------------------------------------------------------------------ */

function Wordmark({ children }: { readonly children: ReactNode }) {
  return (
    <span className="text-cc-ink-dim font-mono text-[0.78rem] tracking-[0.18em] [font-variant:small-caps]">
      {children}
    </span>
  );
}

function Colophon() {
  const fade = useFadeUp();
  return (
    <motion.section {...fade} className="py-24 text-center">
      <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.24em] [font-variant:small-caps]">
        Colophon
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-x-8 gap-y-3">
        <Wordmark>GitHub Actions</Wordmark>
        <span className="text-cc-accent/50" aria-hidden>
          ·
        </span>
        <Wordmark>Azure DevOps</Wordmark>
        <span className="text-cc-accent/50" aria-hidden>
          ·
        </span>
        <Wordmark>GitLab CI</Wordmark>
      </div>
      <p className="text-cc-ink-dim mx-auto mt-8 max-w-xl text-[0.92rem] leading-relaxed">
        One CLI, the same commands and exit codes, the same registry on the
        other side. Set it as a step in the runner you already use.
      </p>
      <div className="mt-10 flex justify-center">
        <SolidButton href="/docs/nitro">Start a Nitro pipeline</SolidButton>
      </div>
    </motion.section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  return (
    <div className="relative">
      {/* Gutter hairline behind the column, like the spine of an open book. */}
      <div
        aria-hidden
        className="bg-cc-card-border pointer-events-none absolute inset-y-0 left-1/2 w-px -translate-x-1/2 opacity-30"
      />

      <article className="relative mx-auto max-w-3xl px-4">
        <Frontispiece />
        <Ornament />

        <div id="chapter-i" className="scroll-mt-24">
          <Chapter
            numeral="I"
            title="The Premise"
            cap="A"
            body={
              <>
                {" "}
                GraphQL schema is a contract. Remove a field or narrow a type
                and a client that still asks for it breaks, quietly, in
                production, long after the pull request merged. The registry
                exists to watch that contract. It records every schema version
                and every operation that published clients have registered, so a
                change can be judged against what consumers actually send. A CI
                pipeline turns that judgement into a gate: classify the diff,
                refuse the dangerous ones, and let the safe ones through on
                their own.
              </>
            }
            marginNote={
              <MarginNote>
                published clients affected · field removed · type narrowed
              </MarginNote>
            }
          />
        </div>

        <Ornament />

        <Chapter
          numeral="II"
          title="Validate"
          cap="R"
          body={
            <>
              un{" "}
              <span className="text-cc-heading font-mono">
                nitro schema validate
              </span>{" "}
              against the branch and every change is stamped safe, dangerous, or
              breaking, checked against the operations your published clients
              have registered. A safe addition passes. A narrowing is flagged
              dangerous. A removal that published clients still depend on is
              breaking, and its exit code fails the job before anything reaches
              the registry. The build fails loudly, not silently.
            </>
          }
          marginNote={
            <MarginNote>
              breaking-change classes: safe, dangerous, breaking
            </MarginNote>
          }
          figure={<DiffFigure />}
        />

        <Ornament />

        <Chapter
          numeral="III"
          title="Upload"
          cap="O"
          body={
            <>
              nce validation clears, the same CLI uploads the candidate schema
              to the registry and uploads the client operation documents
              alongside it, both pinned to a target environment. Nothing is live
              yet. The version is staged, reproducible, and ready for a reviewer
              to promote.
            </>
          }
          figure={<CliFigure />}
        />

        <Ornament />

        <Chapter
          numeral="IV"
          title="Publish"
          cap="P"
          body={
            <>
              ublish promotes a candidate to the published version for an
              environment once validation has cleared. Each environment keeps
              its own active version and its own history, so v13 can serve
              production while v14 waits as the candidate and v15 is still a
              draft. Approval gates can require a reviewer on dangerous or
              breaking changes before the candidate becomes the new contract.
            </>
          }
          figure={<VersionFigure />}
        />

        <Ornament />

        <Chapter
          numeral="V"
          title="Promote"
          cap="T"
          body={
            <>
              he last chapter is the ladder: dev, then QA, then production. A
              change auto-publishes to dev from main, gets promoted to QA after
              it settles, and reaches production only behind an approval gate.
              Each rung carries its own active version and status line, and a
              rollback is simply a re-publish of a prior tagged version.
            </>
          }
          marginNote={<MarginNote>environments: dev · qa · prod</MarginNote>}
          figure={<LadderFigure />}
        />

        <Ornament />

        <Colophon />
      </article>
    </div>
  );
}
