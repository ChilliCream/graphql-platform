import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

const ID = "combined-governance-";

type LintSeverity = "error" | "warning";

interface LintFinding {
  readonly severity: LintSeverity;
  readonly rule: string;
  readonly message: ReactNode;
  readonly fix?: string;
}

// Two errors and one style warning a lint run surfaces on a single change.
const FINDINGS: readonly LintFinding[] = [
  { severity: "error", rule: "naming", message: "get_user", fix: "getUser" },
  {
    severity: "error",
    rule: "deprecation",
    message: "User.email removed",
  },
  { severity: "warning", rule: "style", message: "product", fix: "Product" },
];

type ChangeKind = "added" | "deprecated";

interface ChangeEntry {
  readonly version: string;
  readonly kind: ChangeKind;
  readonly field: string;
  readonly author: string;
  /** True when the author is a coding agent, traced like any human author. */
  readonly agent?: boolean;
  readonly time: string;
}

// Three registry versions, including an agent author traced like a human one.
const CHANGES: readonly ChangeEntry[] = [
  {
    version: "v14",
    kind: "added",
    field: "Product.rating",
    author: "alice",
    time: "2d",
  },
  {
    version: "v13",
    kind: "deprecated",
    field: "User.email",
    author: "agent/codex",
    agent: true,
    time: "5d",
  },
  {
    version: "v12",
    kind: "added",
    field: "Order.totalAmount",
    author: "bob",
    time: "1w",
  },
];

/** Status colors used as data: green additive change, amber deprecation. */
const KIND: Record<ChangeKind, string> = {
  added: "bg-cc-status-healthy",
  deprecated: "bg-cc-status-investigating",
};

/** Coral x-in-circle marking the blocked breaking result. */
function BlockMark() {
  return (
    <svg
      id={`${ID}block-mark`}
      aria-hidden="true"
      viewBox="0 0 12 12"
      width="13"
      height="13"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      className="text-cc-status-firing"
    >
      <circle cx="6" cy="6" r="4.7" />
      <path d="M4.4 4.4 7.6 7.6M7.6 4.4 4.4 7.6" />
    </svg>
  );
}

/** Small checklist mark for the lint window title bar. */
function LintMark() {
  return (
    <svg
      id={`${ID}lint-mark`}
      viewBox="0 0 16 16"
      width="13"
      height="13"
      aria-hidden="true"
      className="text-cc-nav-label shrink-0"
    >
      <rect
        x="2.5"
        y="2"
        width="11"
        height="12"
        rx="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.1"
      />
      <path
        d="M5 6h6M5 8.5h6M5 11h3.5"
        stroke="currentColor"
        strokeWidth="1.1"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** Registry / schema-version node glyph. */
function RegistryNodeIcon() {
  return (
    <svg
      id={`${ID}registry-node`}
      viewBox="0 0 16 16"
      width="13"
      height="13"
      aria-hidden="true"
      className="text-cc-accent shrink-0"
    >
      <path
        fill="currentColor"
        d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
      />
      <circle cx="8" cy="8" r="1.7" fill="currentColor" />
    </svg>
  );
}

/** Shared compact window frame: title bar slot above the illustration body. */
function FacetWindow({
  bar,
  children,
}: {
  readonly bar: ReactNode;
  readonly children: ReactNode;
}) {
  return (
    <div className="border-cc-card-border bg-cc-surface mt-4 flex flex-1 flex-col overflow-hidden rounded-xl border shadow-[0_1px_3px_rgba(2,6,16,0.6)]">
      <div className="border-cc-card-border flex shrink-0 items-center gap-2 border-b px-3 py-2">
        {bar}
      </div>
      {children}
    </div>
  );
}

/** Facet 1: a blocked publish in the CLI. */
function BlockedPublishCard() {
  return (
    <FacetWindow
      bar={
        <>
          <span className="flex gap-1.5" aria-hidden="true">
            <span className="bg-cc-ink-faint size-2 rounded-full" />
            <span className="bg-cc-ink-faint size-2 rounded-full" />
            <span className="bg-cc-ink-faint size-2 rounded-full" />
          </span>
          <span className="text-cc-nav-label ml-auto font-mono text-[0.6rem]">
            schema publish
          </span>
        </>
      }
    >
      <div className="flex flex-1 flex-col justify-center space-y-2 p-3 font-mono text-[0.7rem] leading-relaxed">
        <div className="flex items-center gap-1.5">
          <span aria-hidden="true" className="text-cc-nav-label select-none">
            $
          </span>
          <span className="text-cc-ink">nitro schema publish</span>
        </div>

        <div className="border-cc-status-firing/25 bg-cc-status-firing/[0.06] rounded-lg border p-2.5">
          <div className="flex items-start gap-2">
            <span className="mt-px shrink-0">
              <BlockMark />
            </span>
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-x-1.5 gap-y-1">
                <span className="border-cc-status-firing/50 bg-cc-status-firing/10 text-cc-status-firing inline-flex items-center rounded border px-1.5 py-0.5 text-[0.55rem] font-semibold tracking-[0.08em] uppercase">
                  Breaking
                </span>
                <span className="text-cc-ink">
                  <span className="text-cc-status-firing">Product.rating</span>{" "}
                  removed
                </span>
              </div>
              <p className="text-cc-ink-dim mt-1.5">
                <span className="text-cc-heading">4,213 req</span> / 7d,{" "}
                <span className="text-cc-heading">web@2.4.0</span>
              </p>
              <p className="text-cc-status-firing mt-1.5 font-semibold">
                publish blocked
              </p>
            </div>
          </div>
        </div>
      </div>
    </FacetWindow>
  );
}

/** Facet 2: a schema-lint result with naming, deprecation, and style checks. */
function SchemaLintCard() {
  return (
    <FacetWindow
      bar={
        <>
          <LintMark />
          <span className="text-cc-heading font-mono text-[0.7rem] font-semibold">
            schema-lint
          </span>
          <span className="border-cc-status-firing/40 bg-cc-status-firing/10 text-cc-status-firing ml-auto rounded-full border px-2 py-0.5 font-mono text-[0.55rem] font-semibold tracking-[0.08em] uppercase">
            failed
          </span>
        </>
      }
    >
      <div className="flex flex-1 flex-col justify-center py-1.5">
        {FINDINGS.map((finding) => {
          const isError = finding.severity === "error";
          return (
            <div
              key={finding.rule}
              className={[
                "relative flex items-start gap-2 py-1.5 pr-2 pl-3",
                isError
                  ? "bg-cc-status-firing/5"
                  : "bg-cc-status-investigating/5",
              ].join(" ")}
            >
              <span
                aria-hidden="true"
                className={[
                  "absolute top-0 left-0 h-full w-[3px]",
                  isError
                    ? "bg-cc-status-firing"
                    : "bg-cc-status-investigating",
                ].join(" ")}
              />
              <span
                aria-hidden="true"
                className={[
                  "shrink-0 font-mono text-[0.7rem] leading-[1.5] font-bold",
                  isError
                    ? "text-cc-status-firing"
                    : "text-cc-status-investigating",
                ].join(" ")}
              >
                {isError ? "x" : "!"}
              </span>
              <code className="text-cc-ink min-w-0 flex-1 font-mono text-[0.7rem] leading-[1.5] break-words">
                <span className="text-cc-ink-dim">{finding.rule}</span>{" "}
                {finding.message}
                {finding.fix ? (
                  <>
                    {" "}
                    <span className="text-cc-ink-faint">-&gt;</span>{" "}
                    <span className="text-cc-accent">{finding.fix}</span>
                  </>
                ) : null}
              </code>
            </div>
          );
        })}
      </div>
    </FacetWindow>
  );
}

/** Facet 3: a registry changelog tracing each version and its author. */
function RegistryTraceCard() {
  return (
    <FacetWindow
      bar={
        <>
          <RegistryNodeIcon />
          <span className="text-cc-heading font-mono text-[0.7rem] font-semibold">
            schema.graphql
          </span>
          <span className="text-cc-nav-label font-mono text-[0.7rem]">
            history
          </span>
          <span className="border-cc-accent/40 text-cc-accent ml-auto inline-flex shrink-0 items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.06em] uppercase">
            <span
              aria-hidden="true"
              className="bg-cc-accent size-1.5 rounded-full"
            />
            v14
          </span>
        </>
      }
    >
      <ol className="flex flex-1 flex-col justify-center p-2">
        {CHANGES.map((entry) => (
          <li
            key={entry.version}
            className="flex items-baseline gap-2 px-1 py-1.5 font-mono text-[0.7rem]"
          >
            <span className="text-cc-nav-label w-6 shrink-0">
              {entry.version}
            </span>
            <span
              aria-hidden="true"
              className={`${KIND[entry.kind]} size-1.5 shrink-0 self-center rounded-full`}
            />
            <span className="text-cc-heading min-w-0 flex-1 truncate">
              {entry.field}
            </span>
            <span
              className={`shrink-0 ${entry.agent ? "text-cc-accent" : "text-cc-ink-dim"}`}
            >
              {entry.author}
            </span>
            <span className="text-cc-nav-label w-6 shrink-0 text-right">
              {entry.time}
            </span>
          </li>
        ))}
      </ol>
    </FacetWindow>
  );
}

interface Facet {
  readonly title: string;
  readonly line: string;
  readonly card: ReactNode;
}

const FACETS: readonly Facet[] = [
  {
    title: "Break the build, not the client.",
    line: "Checked against every published client.",
    card: <BlockedPublishCard />,
  },
  {
    title: "One style, whoever's typing.",
    line: "Naming, structure, and deprecation on every change.",
    card: <SchemaLintCard />,
  },
  {
    title: "Nothing changes without a trace.",
    line: "Every version, who changed it, and when.",
    card: <RegistryTraceCard />,
  },
];

/**
 * CombinedGovernance: the compacted Governance section for the homepage landing
 * (after the protocol cards, above pricing, on the dark navy canvas). One shared
 * header sits above a tight three-up grid that folds the registry, lint, and
 * release-safety takes into compact facet cards: a blocked publish, a schema
 * lint result, and a registry changelog. The whole section is about the height
 * of a single take.
 */
export function CombinedGovernance() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* Shared header. */}
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Governance
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 leading-[1.1] font-semibold text-balance">
            Change contracts with a safety net.
          </h2>
          <p className="text-cc-ink mt-5 max-w-3xl text-base text-pretty sm:text-lg">
            Schema registry, client registry, validation, linting, and rules:
            stop bad changes, enforce one style, and trace every change.
          </p>
          <a
            href="/platform/release-safety"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </a>
        </div>

        {/* Three compact facets. */}
        <div className="mt-12 grid grid-cols-1 gap-8 lg:grid-cols-3 lg:gap-6">
          {FACETS.map((facet) => (
            <div key={facet.title} className="flex h-full flex-col">
              <h3 className="font-heading text-cc-heading text-base font-semibold sm:text-lg">
                {facet.title}
              </h3>
              <p className="text-cc-ink-dim mt-1.5 text-sm lg:min-h-[2.5rem]">
                {facet.line}
              </p>
              {facet.card}
            </div>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
