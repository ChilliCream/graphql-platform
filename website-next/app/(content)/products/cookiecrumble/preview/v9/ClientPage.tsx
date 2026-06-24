"use client";

import type { CSSProperties, ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// -----------------------------------------------------------------------------
// v9 "Heartbeat of the Suite". The page reads like a calm test runner left
// running on a second monitor. Every snapshot has a status (committed,
// mismatched, pending review, passing) and a tiny pulsing dot communicates
// which state is "live" right now. All motion is time-driven via motion's
// animate={{}} with transition repeat: Infinity. Nothing is coupled to scroll.
// Reduced motion falls back to static dots.
// -----------------------------------------------------------------------------

// Brand spectrum hairline, used at most once per screen, on the closing CTA.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Status palette. These map to the page's runner narrative:
//   running   -> teal cc-accent, the assertion currently executing
//   passing   -> green cc-status-healthy, snapshot matched
//   mismatch  -> coral cc-status-firing, snapshot differs
//   settled   -> dim ink, committed and quiet
const STATUS = {
  running: "#5eead4",
  passing: "#34d399",
  mismatch: "#f0786a",
  settled: "rgba(245, 241, 234, 0.45)",
} as const;

type StatusKey = keyof typeof STATUS;

// GitHub-dark token colors scoped to the snapshot/code blocks. The rest of the
// page stays on cc-* tokens.
const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
};

// -----------------------------------------------------------------------------
// Pulse marker. The page's signature motion: a status dot that gently breathes
// on a 1.4s ease-in-out opacity + scale loop. Reduced motion renders a static
// dot at full opacity. Used on the hero live indicator and on running test-tree
// rows and active cards.
// -----------------------------------------------------------------------------

interface PulseDotProps {
  readonly status: StatusKey;
  readonly size?: number;
  readonly active?: boolean;
  readonly className?: string;
}

function PulseDot({
  status,
  size = 10,
  active = true,
  className,
}: PulseDotProps) {
  const reduceMotion = useReducedMotion();
  const color = STATUS[status];
  const dimensions: CSSProperties = {
    width: size,
    height: size,
    backgroundColor: color,
  };

  // Settled markers and reduced-motion never pulse; they sit static.
  const shouldPulse = active && !reduceMotion;

  return (
    <span
      className={["relative inline-flex", className ?? ""]
        .filter(Boolean)
        .join(" ")}
      aria-hidden
    >
      {shouldPulse ? (
        <motion.span
          className="absolute inset-0 rounded-full"
          style={{ backgroundColor: color }}
          animate={{ opacity: [0.35, 0, 0.35], scale: [1, 2.1, 1] }}
          transition={{ duration: 1.4, repeat: Infinity, ease: "easeInOut" }}
        />
      ) : null}
      {shouldPulse ? (
        <motion.span
          className="relative rounded-full"
          style={dimensions}
          animate={{ opacity: [0.55, 1, 0.55], scale: [1, 1.25, 1] }}
          transition={{ duration: 1.4, repeat: Infinity, ease: "easeInOut" }}
        />
      ) : (
        <span
          className="relative rounded-full"
          style={{ ...dimensions, opacity: active ? 1 : 0.55 }}
        />
      )}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Small primitives shared across the page.
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface CodeLineProps {
  readonly n?: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-4 px-5">
      {n !== undefined ? (
        <span
          className="w-6 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
          aria-hidden
        >
          {n}
        </span>
      ) : null}
      <span className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Left rail: a compact runner-style test tree. Status dots pulse to show which
// suite is currently running. Counts are static; only the markers pulse.
// -----------------------------------------------------------------------------

interface TreeNode {
  readonly name: string;
  readonly status: StatusKey;
  readonly count: string;
  readonly note: string;
}

const TEST_TREE: readonly TreeNode[] = [
  {
    name: "Catalog.Tests",
    status: "running",
    count: "48",
    note: "executing",
  },
  {
    name: "Schema.Tests",
    status: "passing",
    count: "61",
    note: "passed",
  },
  {
    name: "Fusion.Tests",
    status: "mismatch",
    count: "33",
    note: "1 differs",
  },
];

function TestTree() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-4">
      <div className="border-cc-card-border flex items-center justify-between border-b pb-3">
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-wider uppercase">
          Test runner
        </span>
        <span className="text-cc-accent inline-flex items-center gap-2 font-mono text-[10px] tracking-wider uppercase">
          <PulseDot status="running" size={7} />
          Live
        </span>
      </div>
      <ul className="mt-3 flex flex-col gap-1">
        {TEST_TREE.map((node) => (
          <li
            key={node.name}
            className="hover:bg-cc-surface/40 flex items-center gap-3 rounded-md px-2 py-2 transition-colors"
          >
            <PulseDot
              status={node.status}
              size={8}
              active={node.status === "running"}
            />
            <span className="text-cc-ink font-mono text-[12.5px]">
              {node.name}
            </span>
            <span className="text-cc-ink-dim ml-auto font-mono text-[10.5px] tabular-nums">
              {node.count}
            </span>
            <span
              className="font-mono text-[10px] tracking-wider uppercase"
              style={{ color: STATUS[node.status] }}
            >
              {node.note}
            </span>
          </li>
        ))}
      </ul>
      <p className="border-cc-card-border text-cc-ink-dim mt-3 border-t pt-3 font-mono text-[11px]">
        142 snapshots, all GraphQL-aware
      </p>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Hero snapshot card. A single oversized snapshot whose header carries the
// filename, a pulsing live dot, and a green PASS pill. The body is the
// IExecutionResult snapshot output.
// -----------------------------------------------------------------------------

function HeroSnapshotCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-3 border-b px-4 py-3">
        <PulseDot status="running" size={9} />
        <span className="text-cc-ink-dim font-mono text-[11px]">
          __snapshots__/ProductQueryTests.Product_By_Id_Returns_Catalog_Shape.snap
        </span>
        <span
          className="ml-auto inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 font-mono text-[10px] font-semibold tracking-wider uppercase"
          style={{
            color: STATUS.passing,
            backgroundColor: "rgba(52, 211, 153, 0.12)",
          }}
        >
          <span
            className="h-1.5 w-1.5 rounded-full"
            style={{ backgroundColor: STATUS.passing }}
            aria-hidden
          />
          Pass
        </span>
      </div>
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 180px at 14% 18%, rgba(94, 234, 212, 0.16), transparent 70%)",
        }}
      />
      <div className="relative py-4">
        <CodeLine n={1}>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`  `}</span>
          <span style={C.str}>{`"data"`}</span>
          <span style={C.punct}>: {`{`}</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`"productById"`}</span>
          <span style={C.punct}>: {`{`}</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`      `}</span>
          <span style={C.str}>{`"id"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.str}>{`"p_42"`}</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.plain}>{`      `}</span>
          <span style={C.str}>{`"name"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.str}>{`"Cookie Crumble Tee"`}</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.plain}>{`      `}</span>
          <span style={C.str}>{`"price"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.plain}>{`24.0`}</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.plain}>{`  `}</span>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={C.comment}>{`# Committed alongside the test.`}</span>
        </CodeLine>
        <CodeLine n={12}>
          <span
            style={C.comment}
          >{`# Diffs in PRs read like the API contract.`}</span>
        </CodeLine>
      </div>
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        <span>GraphQL-aware formatter</span>
        <span className="text-cc-accent">IExecutionResult</span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Capabilities strip. Five mono-cased chips with a static CheckIcon.
// -----------------------------------------------------------------------------

const CAPABILITIES: readonly string[] = [
  "GraphQL-aware formatters",
  "Inline + file + Markdown",
  "__mismatch__ workflow",
  "xUnit, NUnit, TUnit, MSTest",
  "Dogfooded by the platform",
];

function CapabilitiesStrip() {
  return (
    <section
      aria-label="Capabilities at a glance"
      className="border-cc-card-border border-t py-6"
    >
      <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-5">
        {CAPABILITIES.map((label) => (
          <li
            key={label}
            className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
          >
            <span className="text-cc-accent" aria-hidden>
              <CheckIcon size={12} />
            </span>
            {label}
          </li>
        ))}
      </ul>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Snapshot anatomy. One oversized labelled snapshot with callouts pointing at
// the data, errors, and extensions blocks. A pulsing formatter dot sits beside
// the IExecutionResult label to mark the live GraphQL-aware path.
// -----------------------------------------------------------------------------

function SnapshotAnatomy() {
  return (
    <section
      id="formatters"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-start gap-12 lg:grid-cols-12 lg:gap-16">
        <div className="lg:col-span-5">
          <Eyebrow>GraphQL-aware formatters</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            The snapshot reads like the GraphQL response, not a dump.
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            Cookie Crumble ships first-class formatters for Hot Chocolate&apos;s
            IExecutionResult and for GraphQLHttpResponse. Pass either type to
            MatchSnapshot and the snapshot file comes out as the request and the
            response, in a shape your reviewers can read. No custom serializers,
            no opt-in attributes.
          </p>
          <ul className="mt-6 flex flex-col gap-2.5">
            {[
              "Native formatter for IExecutionResult covers data, errors, and extensions.",
              "Native formatter for GraphQLHttpResponse keeps status, headers, and body together.",
              "Falls back to a structural formatter for any other .NET object you assert on.",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
        <div className="lg:col-span-7">
          <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-xl border">
            <div className="bg-cc-code-header border-cc-card-border flex items-center gap-3 border-b px-4 py-3">
              <PulseDot status="running" size={8} />
              <span className="text-cc-ink-dim font-mono text-[11px]">
                IExecutionResult
              </span>
              <span className="text-cc-accent ml-auto font-mono text-[10px] tracking-wider uppercase">
                live formatter
              </span>
            </div>
            <div className="py-4">
              <CodeLine>
                <span style={C.punct}>{`{`}</span>
              </CodeLine>
              <CodeLine>
                <span style={C.plain}>{`  `}</span>
                <span style={C.str}>{`"data"`}</span>
                <span style={C.punct}>: {} </span>
                <span style={C.comment}>{`// shape of the response`}</span>
              </CodeLine>
              <CodeLine>
                <span style={C.plain}>{`  `}</span>
                <span style={C.str}>{`"errors"`}</span>
                <span style={C.punct}>: [ ] </span>
                <span style={C.comment}>{`// surfaced, never swallowed`}</span>
              </CodeLine>
              <CodeLine>
                <span style={C.plain}>{`  `}</span>
                <span style={C.str}>{`"extensions"`}</span>
                <span style={C.punct}>: {} </span>
                <span style={C.comment}>{`// tracing, request metadata`}</span>
              </CodeLine>
              <CodeLine>
                <span style={C.punct}>{`}`}</span>
              </CodeLine>
            </div>
          </div>
          <p className="text-cc-ink-dim mt-3 text-[12px] leading-relaxed">
            The formatter walks the data, errors, and extensions blocks in turn,
            so the snapshot reads like the GraphQL response itself, not like a
            serialized object graph.
          </p>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Three flavors row. Three stacked code cards. The currently "active" card
// carries a pulsing teal status dot in its header. The active index cycles on a
// time-driven loop (3.5s per card), never on scroll.
// -----------------------------------------------------------------------------

interface FlavorCard {
  readonly label: string;
  readonly api: string;
  readonly lines: ReactNode;
}

const FLAVORS: readonly FlavorCard[] = [
  {
    label: "Inline",
    api: "MatchInlineSnapshot",
    lines: (
      <>
        <CodeLine n={1}>
          <span style={C.plain}>result</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchInlineSnapshot</span>
          <span style={C.punct}>(</span>
          <span style={C.str}>{`"""`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`{ "data": { "ping": "pong" } }`}</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`"""`}</span>
          <span style={C.punct}>);</span>
        </CodeLine>
      </>
    ),
  },
  {
    label: "File",
    api: "MatchSnapshot",
    lines: (
      <CodeLine n={1}>
        <span style={C.plain}>result</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchSnapshot</span>
        <span style={C.punct}>();</span>{" "}
        <span style={C.comment}>{`// __snapshots__/<test>.snap`}</span>
      </CodeLine>
    ),
  },
  {
    label: "Markdown",
    api: "MatchMarkdownSnapshot",
    lines: (
      <>
        <CodeLine n={1}>
          <span style={C.type}>Snapshot</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Create</span>
          <span style={C.punct}>()</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(request, </span>
          <span style={C.str}>{`"Request"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(result, </span>
          <span style={C.str}>{`"Result"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchMarkdownSnapshot</span>
          <span style={C.punct}>();</span>
        </CodeLine>
      </>
    ),
  },
];

function FlavorRow() {
  const reduceMotion = useReducedMotion();
  const cycle = 3.5;
  const total = FLAVORS.length * cycle;

  return (
    <section
      id="flavors"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="max-w-2xl">
        <Eyebrow>Inline, file, Markdown</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          Three snapshot shapes, one assertion API.
        </h2>
        <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
          Small assertions go inline so the expected output sits beside the
          test. Larger payloads land in a snapshot file next to the test. When a
          single test exercises several layers (request, response, projected
          events, audit log), MatchMarkdownSnapshot composes them into one
          readable document instead of a bag of unrelated assertions.
        </p>
      </div>
      <div className="mt-8 grid gap-4 lg:grid-cols-3">
        {FLAVORS.map((flavor, index) => {
          // Each card lights up for one slot of the loop. We animate the
          // header dot's opacity on a staggered window so exactly one card
          // reads as "active" at a time, all on the same infinite timeline.
          const start = (index * cycle) / total;
          const peak = start + cycle / total / 2;
          const end = start + cycle / total;
          const keyframes = [0, start, peak, end, 1];
          return (
            <div
              key={flavor.label}
              className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-xl border"
            >
              <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-3">
                <span className="text-cc-ink-dim inline-flex items-center gap-2.5 font-mono text-[11px]">
                  {reduceMotion ? (
                    <PulseDot status="running" size={8} active={false} />
                  ) : (
                    <span className="relative inline-flex" aria-hidden>
                      <motion.span
                        className="rounded-full"
                        style={{
                          width: 8,
                          height: 8,
                          backgroundColor: STATUS.running,
                        }}
                        animate={{
                          opacity: [0.3, 0.3, 1, 0.3, 0.3],
                          scale: [1, 1, 1.3, 1, 1],
                        }}
                        transition={{
                          duration: total,
                          times: keyframes,
                          repeat: Infinity,
                          ease: "easeInOut",
                        }}
                      />
                    </span>
                  )}
                  {flavor.label}
                </span>
                <span className="text-cc-accent font-mono text-[11px]">
                  {flavor.api}
                </span>
              </div>
              <div className="py-3">{flavor.lines}</div>
            </div>
          );
        })}
      </div>
      <ul className="mt-6 flex flex-col gap-2.5">
        {[
          "MatchInlineSnapshot keeps tiny assertions self-contained.",
          "MatchSnapshot writes to a snapshot file next to your test.",
          "MatchMarkdownSnapshot captures several shapes of state in one document.",
        ].map((b) => (
          <li
            key={b}
            className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
          >
            <span className="text-cc-accent mt-1 shrink-0">
              <CheckIcon size={14} />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Mismatch workflow stepper. A horizontal 3-step strip. A single pulsing ring
// travels from step to step on an 8s infinite loop, narrating the review story
// without any scrolling. Each step's ring opacity is animated on a staggered
// window of the same timeline.
// -----------------------------------------------------------------------------

interface WorkflowStep {
  readonly index: string;
  readonly title: string;
  readonly note: string;
  readonly status: StatusKey;
}

const WORKFLOW_STEPS: readonly WorkflowStep[] = [
  {
    index: "01",
    title: "Test run differs",
    note: "snapshot mismatch",
    status: "mismatch",
  },
  {
    index: "02",
    title: "__mismatch__/ folder",
    note: "gitignored, never committed",
    status: "settled",
  },
  {
    index: "03",
    title: "Move into __snapshots__/",
    note: "review the diff, then accept",
    status: "passing",
  },
];

function MismatchStepper() {
  const reduceMotion = useReducedMotion();
  const total = 8;
  const slot = 1 / WORKFLOW_STEPS.length;

  return (
    <section
      id="mismatch"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="max-w-2xl">
        <Eyebrow>Update workflow</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          A __mismatch__ folder turns failing snapshots into a code review.
        </h2>
        <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
          When a snapshot test fails, Cookie Crumble writes the actual output
          into a __mismatch__/ folder next to the test. The folder is
          gitignored, so the failing artefact never sneaks into a commit by
          accident. Diff it against the committed snapshot, decide whether the
          change is intentional, and move it into place when you accept it.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg mt-8 grid gap-3 rounded-xl border p-5 sm:grid-cols-3 sm:gap-4 sm:p-6">
        {WORKFLOW_STEPS.map((step, index) => {
          const start = index * slot;
          const peak = start + slot / 2;
          const end = start + slot;
          const times = [0, start, peak, end, 1];
          return (
            <div
              key={step.index}
              className="relative rounded-lg p-4"
              style={{ backgroundColor: "rgba(12, 19, 34, 0.5)" }}
            >
              {/* The traveling ring: one motion.div per step, all sharing the
                  same 8s timeline, each lit in its own window. */}
              {reduceMotion ? (
                index === 0 ? (
                  <span
                    className="pointer-events-none absolute inset-0 rounded-lg border"
                    style={{ borderColor: STATUS[step.status] }}
                    aria-hidden
                  />
                ) : null
              ) : (
                <motion.span
                  className="pointer-events-none absolute inset-0 rounded-lg border"
                  style={{ borderColor: STATUS[step.status] }}
                  animate={{ opacity: [0, 0, 1, 0, 0] }}
                  transition={{
                    duration: total,
                    times,
                    repeat: Infinity,
                    ease: "easeInOut",
                  }}
                  aria-hidden
                />
              )}
              <div className="relative flex items-center gap-2.5">
                <PulseDot
                  status={step.status}
                  size={8}
                  active={step.status !== "settled"}
                />
                <span className="text-cc-ink-dim font-mono text-[11px] tabular-nums">
                  {step.index}
                </span>
              </div>
              <p className="text-cc-heading relative mt-3 font-mono text-[13px]">
                {step.title}
              </p>
              <p className="text-cc-ink-dim relative mt-1 font-mono text-[10.5px]">
                {step.note}
              </p>
            </div>
          );
        })}
      </div>
      <ul className="mt-6 flex flex-col gap-2.5">
        {[
          "Failing snapshots land in __mismatch__/, never on top of the committed file.",
          "The folder is meant to be gitignored, so nothing accidental gets checked in.",
          "Updates become a deliberate review step, not a silent overwrite.",
        ].map((b) => (
          <li
            key={b}
            className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
          >
            <span className="text-cc-accent mt-1 shrink-0">
              <CheckIcon size={14} />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Framework matrix + dogfood combo. A 2x2 framework grid followed by a Hot
// Chocolate / Fusion / Mocha pill row, each pill carrying a green pulsing
// passing dot to convey live use across the platform.
// -----------------------------------------------------------------------------

const FRAMEWORKS: readonly { name: string; note: string }[] = [
  { name: "xUnit", note: "[Fact] / [Theory]" },
  { name: "NUnit", note: "[Test]" },
  { name: "TUnit", note: "[Test]" },
  { name: "MSTest", note: "[TestMethod]" },
];

const DOGFOOD: readonly { name: string; role: string }[] = [
  { name: "Hot Chocolate", role: "GraphQL server" },
  { name: "Fusion", role: "Federation gateway" },
  { name: "Mocha", role: "Distributed messaging" },
];

function FrameworkAndDogfood() {
  return (
    <section
      id="frameworks"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-start gap-12 lg:grid-cols-12 lg:gap-16">
        <div className="lg:col-span-5">
          <Eyebrow>Test framework</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Drops into the .NET test runner you already use.
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            The same MatchSnapshot, MatchInlineSnapshot, and
            MatchMarkdownSnapshot APIs work on top of xUnit, NUnit, TUnit, and
            MSTest. Cookie Crumble figures out the current test&apos;s name and
            namespace from the runner, names the snapshot file accordingly, and
            surfaces failures through the runner&apos;s normal channel.
          </p>
          <p className="text-cc-ink-dim mt-6 text-[13px] leading-relaxed">
            Every product on the ChilliCream platform writes its assertions with
            Cookie Crumble, so each commit through those products re-exercises
            the library under real production pressure.
          </p>
        </div>
        <div className="lg:col-span-7">
          <div className="grid grid-cols-2 gap-3">
            {FRAMEWORKS.map((f) => (
              <div
                key={f.name}
                className="border-cc-card-border bg-cc-surface/40 flex flex-col gap-1 rounded-lg border px-4 py-4"
              >
                <div className="flex items-center justify-between">
                  <span className="text-cc-heading font-heading text-base font-semibold">
                    {f.name}
                  </span>
                  <span className="text-cc-accent" aria-hidden>
                    <CheckIcon size={14} />
                  </span>
                </div>
                <span className="text-cc-ink-dim font-mono text-[11px]">
                  {f.note}
                </span>
              </div>
            ))}
          </div>
          <ul className="mt-3 flex flex-col gap-2.5">
            {DOGFOOD.map((p) => (
              <li
                key={p.name}
                className="border-cc-card-border bg-cc-surface/40 flex items-center gap-3 rounded-lg border px-4 py-3"
              >
                <PulseDot status="passing" size={8} />
                <span className="text-cc-heading font-heading text-base font-semibold">
                  {p.name}
                </span>
                <span className="text-cc-ink-dim ml-auto font-mono text-[11px] tracking-wider uppercase">
                  {p.role}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// MIT band. Open-source proof tile alongside the heading.
// -----------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: string;
}

function ProofItem({ label, value }: ProofItemProps) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        {value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </div>
  );
}

function MitBand() {
  return (
    <section
      aria-label="Open source"
      className="border-cc-card-border border-t py-20 sm:py-24"
    >
      <div className="grid items-center gap-10 lg:grid-cols-12">
        <div className="lg:col-span-7">
          <Eyebrow>MIT licensed</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Open source, dogfooded, free to use.
          </h2>
          <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
            Cookie Crumble is released under the MIT license and developed in
            the open alongside the rest of the ChilliCream platform. Use it in
            commercial work, fork it, vendor it, audit it. The package, the
            issue tracker, and the release notes all live on GitHub.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </SolidButton>
            <OutlineButton href="/docs/cookiecrumble">
              Read the docs
            </OutlineButton>
          </div>
        </div>
        <div className="lg:col-span-5">
          <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
            <ProofItem label="License" value="MIT" />
            <ProofItem label="Package" value="CookieCrumble" />
            <ProofItem label="Runtimes" value=".NET 8 and later" />
            <ProofItem
              label="Frameworks"
              value="xUnit + NUnit + TUnit + MSTest"
            />
            <ProofItem label="Formatters" value="GraphQL-aware" />
            <ProofItem label="Workflow" value="__mismatch__/" />
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Page. Asymmetric two-pane layout mimicking a test runner. The left rail
// (cols 1-3 on lg+) carries the test tree; the right main column carries the
// hero and the rest of the sections.
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <>
      {/* Runner grid: a fixed, masked radial-dot layer behind the page. */}
      <div
        aria-hidden
        className="pointer-events-none fixed inset-0 -z-10"
        style={{
          backgroundImage:
            "radial-gradient(circle at 0 0, rgba(94,234,212,0.04) 1px, transparent 1px)",
          backgroundSize: "32px 32px",
          maskImage: "linear-gradient(to bottom, black 30%, transparent 95%)",
          WebkitMaskImage:
            "linear-gradient(to bottom, black 30%, transparent 95%)",
        }}
      />

      <div className="grid gap-10 lg:grid-cols-12 lg:gap-12">
        {/* Left rail: the test tree. Horizontal-feel anchor on desktop, stacks
            above the content on mobile. */}
        <aside className="lg:col-span-3 lg:pt-20">
          <TestTree />
        </aside>

        {/* Right main column. */}
        <div className="lg:col-span-9">
          {/* HERO */}
          <section className="pt-4 pb-10 sm:pb-16 lg:pt-16">
            <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-10">
              <div>
                <Eyebrow>Snapshot testing for .NET</Eyebrow>
                <h1 className="text-cc-heading font-heading mt-5 text-4xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-5xl">
                  GraphQL snapshot testing for .NET, with a heartbeat you can
                  read.
                </h1>
                <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
                  Cookie Crumble is the open-source snapshot library the
                  ChilliCream team writes its own tests with. It ships native
                  formatters for Hot Chocolate IExecutionResult and
                  GraphQLHttpResponse, so the snapshot file reads like the
                  GraphQL response itself. Inline, file, or Markdown. xUnit,
                  NUnit, TUnit, or MSTest. MIT-licensed.
                </p>
                <div className="mt-8 flex flex-wrap gap-3">
                  <SolidButton href="/docs/cookiecrumble">
                    Get Started
                  </SolidButton>
                  <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                    View on GitHub
                  </OutlineButton>
                </div>
              </div>
              <div>
                <HeroSnapshotCard />
              </div>
            </div>
          </section>

          <CapabilitiesStrip />
          <SnapshotAnatomy />
          <FlavorRow />
          <MismatchStepper />
          <FrameworkAndDogfood />
          <MitBand />

          {/* Closing CTA. The single brand-spectrum hairline lives here. */}
          <section className="border-cc-card-border relative border-t py-20 sm:py-28">
            <div
              aria-hidden
              className="pointer-events-none absolute inset-x-0 top-0 h-px"
              style={{ background: SPECTRUM }}
            />
            <div className="text-center">
              <Eyebrow>Get started</Eyebrow>
              <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
                Write the assertion. Read the GraphQL.
              </h2>
              <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
                Add the Cookie Crumble package to your test project, call
                MatchSnapshot on an IExecutionResult or a GraphQLHttpResponse,
                and the next pull request diff reads like the API contract
                instead of a wall of property assertions.
              </p>
              <div className="mt-8 flex flex-wrap justify-center gap-3">
                <SolidButton href="/docs/cookiecrumble">
                  Get Started
                </SolidButton>
                <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                  View on GitHub
                </OutlineButton>
              </div>
            </div>
          </section>
        </div>
      </div>
    </>
  );
}
