import Link from "next/link";
import type { ReactNode } from "react";

import { ArrowLink } from "@/src/components/ArrowLink";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { BlockMark } from "@/src/icons/BlockMark";
import { LintMark } from "@/src/icons/LintMark";
import { RegistryNodeIcon } from "@/src/icons/RegistryNodeIcon";

type LintSeverity = "error" | "warning";

interface LintFinding {
  readonly severity: LintSeverity;
  readonly rule: string;
  readonly message: ReactNode;
  readonly fix?: string;
}

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
  readonly agent?: boolean;
  readonly time: string;
}

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

const KIND: Record<ChangeKind, string> = {
  added: "bg-cc-success",
  deprecated: "bg-cc-warning",
};

interface FacetWindowProps {
  readonly bar: ReactNode;
  readonly children: ReactNode;
}

function FacetWindow({ bar, children }: FacetWindowProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface mt-4 flex flex-1 flex-col overflow-hidden rounded-xl border shadow-[0_1px_3px_rgba(2,6,16,0.6)]">
      <div className="border-cc-card-border flex shrink-0 items-center gap-2 border-b px-3 py-2">
        {bar}
      </div>
      {children}
    </div>
  );
}

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
          <span className="text-cc-ink-dim ml-auto font-mono text-[0.6rem]">
            schema publish
          </span>
        </>
      }
    >
      <div className="flex flex-1 flex-col justify-center space-y-2 p-3 font-mono text-[0.7rem] leading-relaxed">
        <p className="text-cc-ink-dim text-[0.58rem] tracking-[0.12em] uppercase">
          Illustrative configured check
        </p>
        <div className="flex items-center gap-1.5">
          <span aria-hidden="true" className="text-cc-ink-dim select-none">
            $
          </span>
          <span className="text-cc-ink">nitro schema publish</span>
        </div>

        <div className="border-cc-danger/25 bg-cc-danger/[0.06] rounded-lg border p-2.5">
          <div className="flex items-start gap-2">
            <span className="mt-px shrink-0">
              <BlockMark className="text-cc-danger" />
            </span>
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-x-1.5 gap-y-1">
                <span className="border-cc-danger/50 bg-cc-danger/10 text-cc-danger inline-flex items-center rounded border px-1.5 py-0.5 text-[0.55rem] font-semibold tracking-[0.08em] uppercase">
                  Breaking
                </span>
                <span className="text-cc-ink">
                  <span className="text-cc-danger">Product.rating</span> removed
                </span>
              </div>
              <p className="text-cc-ink-dim mt-1.5">
                <span className="text-cc-heading">4,213 req</span> / 7d,{" "}
                <span className="text-cc-heading">web@2.4.0</span>
              </p>
              <p className="text-cc-danger mt-1.5 font-semibold">
                publish blocked
              </p>
            </div>
          </div>
        </div>
      </div>
    </FacetWindow>
  );
}

function SchemaLintCard() {
  return (
    <FacetWindow
      bar={
        <>
          <LintMark className="text-cc-ink-dim shrink-0" />
          <span className="text-cc-heading font-mono text-[0.7rem] font-semibold">
            schema-lint
          </span>
          <span className="border-cc-danger/40 bg-cc-danger/10 text-cc-danger ml-auto rounded-full border px-2 py-0.5 font-mono text-[0.55rem] font-semibold tracking-[0.08em] uppercase">
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
                isError ? "bg-cc-danger/5" : "bg-cc-warning/5",
              ].join(" ")}
            >
              <span
                aria-hidden="true"
                className={[
                  "absolute top-0 left-0 h-full w-[3px]",
                  isError ? "bg-cc-danger" : "bg-cc-warning",
                ].join(" ")}
              />
              <span
                aria-hidden="true"
                className={[
                  "shrink-0 font-mono text-[0.7rem] leading-[1.5] font-bold",
                  isError ? "text-cc-danger" : "text-cc-warning",
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

function RegistryTraceCard() {
  return (
    <FacetWindow
      bar={
        <>
          <RegistryNodeIcon className="text-cc-accent shrink-0" />
          <span className="text-cc-heading font-mono text-[0.7rem] font-semibold">
            schema.graphql
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.7rem]">
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
            <span className="text-cc-ink-dim w-6 shrink-0">
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
            <span className="text-cc-ink-dim w-6 shrink-0 text-right">
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
    title: "Use validation as a required CI check.",
    line: "Check operations registered by published client versions.",
    card: <BlockedPublishCard />,
  },
  {
    title: "One style, whoever's typing.",
    line: "Naming, structure, and deprecation on every change.",
    card: <SchemaLintCard />,
  },
  {
    title: "Keep published version history.",
    line: "See what changed, who changed it, and when.",
    card: <RegistryTraceCard />,
  },
];

export function CombinedGovernance() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="mx-auto max-w-3xl text-center">
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.1] font-semibold text-balance">
            Change contracts with a safety net.
          </h2>
          <p className="text-cc-ink mt-5 max-w-3xl text-base text-pretty sm:text-lg">
            Classify schema changes, validate them against operations registered
            by published client versions, apply lint rules, and keep version
            history.
          </p>
          <ArrowLink href="/platform/release-safety" className="mt-6">
            Explore schema checks
          </ArrowLink>
        </div>

        <div className="mt-12 grid grid-cols-1 gap-8 lg:grid-cols-3 lg:gap-6">
          {FACETS.map((facet) => (
            <Link
              key={facet.title}
              href="/platform/release-safety"
              className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-2xl border p-5 no-underline backdrop-blur-sm transition-colors"
            >
              <h3 className="font-heading text-cc-heading text-base font-semibold sm:text-lg">
                {facet.title}
              </h3>
              <p className="text-cc-ink-dim mt-1.5 text-sm lg:min-h-[2.5rem]">
                {facet.line}
              </p>
              {facet.card}
            </Link>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
