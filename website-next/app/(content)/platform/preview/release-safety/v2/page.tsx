import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Change Contracts With a Safety Net",
  description:
    "Classify every GraphQL schema change safe, dangerous, or breaking, validate it against the operations published clients use, and stop unsafe releases in CI.",
  keywords: [
    "GraphQL release safety",
    "schema registry",
    "client registry",
    "breaking change detection",
    "schema diff review",
    "validate publish gate",
    "CI schema checks",
    "published clients affected",
    "persisted operations",
    "safe schema evolution",
  ],
  openGraph: {
    title: "Change Contracts With a Safety Net",
    description:
      "Classify every schema change safe, dangerous, or breaking, validate it against the operations published clients use, and stop unsafe releases in CI.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Status vocabulary. The accent is guardrail blue; status hues are    */
/* used as INK only (text / hairline / dot), never as a fill.          */
/* ------------------------------------------------------------------ */

type Status = "safe" | "dangerous" | "breaking";

const STATUS: Record<
  Status,
  { readonly label: string; readonly ink: string; readonly line: string }
> = {
  safe: { label: "Safe", ink: "text-cc-success", line: "bg-cc-success" },
  dangerous: {
    label: "Dangerous",
    ink: "text-cc-warning",
    line: "bg-cc-warning",
  },
  breaking: { label: "Breaking", ink: "text-cc-danger", line: "bg-cc-danger" },
};

/* Guardrail-blue accent used as the scene's structural ink. Each variant is a
   static literal so Tailwind's JIT emits the class. */
const RULE = "border-[#7c92c6]/20";
const RULE_BG = "bg-[#7c92c6]/20";
const RULE_DIVIDE = "divide-[#7c92c6]/20";
const BLUEPRINT = "text-[#7c92c6]";
const BLUEPRINT_BG = "bg-[#7c92c6]/30";

/* ------------------------------------------------------------------ */
/* Shared layout atoms                                                 */
/* ------------------------------------------------------------------ */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.25em] uppercase">
      {children}
    </span>
  );
}

interface NumberedHeadingProps {
  readonly index: string;
  readonly kicker: string;
  readonly children: ReactNode;
}

/**
 * The narrative spine: a big mono ordinal in guardrail blue sitting beside an
 * editorial Josefin heading, separated by a hairline rule.
 */
function NumberedHeading({ index, kicker, children }: NumberedHeadingProps) {
  return (
    <div className="grid gap-6 sm:grid-cols-[6rem_1fr] sm:gap-10">
      <div className="flex flex-col">
        <span
          className={`text-h3 font-mono leading-none font-light tabular-nums ${BLUEPRINT}`}
        >
          {index}
        </span>
        <span className={`mt-3 h-px w-12 ${RULE_BG}`} />
      </div>
      <div>
        <Eyebrow>{kicker}</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 leading-[1.1] font-semibold text-balance">
          {children}
        </h2>
      </div>
    </div>
  );
}

/**
 * Faint blueprint dot-grid surface. Every technical line-diagram on this page
 * sits on this, so the illustrations read as one consistent schematic family.
 */
interface BlueprintProps {
  readonly children: ReactNode;
  readonly label?: string;
  readonly className?: string;
}

function Blueprint({ children, label, className = "" }: BlueprintProps) {
  return (
    <figure
      className={`border-cc-card-border bg-cc-surface/40 relative overflow-hidden rounded-2xl border backdrop-blur-sm ${className}`}
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 opacity-[0.5]"
        style={{
          backgroundImage:
            "radial-gradient(rgba(124,146,198,0.18) 1px, transparent 1px)",
          backgroundSize: "18px 18px",
        }}
      />
      <div className="relative p-6 sm:p-8">{children}</div>
      {label && (
        <figcaption
          className={`relative border-t px-6 py-3 font-mono text-[0.62rem] tracking-[0.18em] uppercase ${RULE} ${BLUEPRINT}`}
        >
          {label}
        </figcaption>
      )}
    </figure>
  );
}

/* ------------------------------------------------------------------ */
/* HERO — the validate -> publish gate as a quiet inline schematic      */
/* ------------------------------------------------------------------ */

function GateSchematic() {
  return (
    <Blueprint label="lifecycle · validate then publish">
      <div className="flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.2em] uppercase">
          contract review
        </span>
        <span className="text-cc-success font-mono text-[0.62rem] tracking-[0.18em] uppercase">
          gate armed
        </span>
      </div>

      {/* Two-stage gate on the dot-grid: validate node -> publish node. */}
      <div className="mt-8 flex items-center gap-3 sm:gap-5">
        <GateNode title="validate" sub="classify diff" tone="active" />
        <Connector blocked />
        <GateNode title="publish" sub="held" tone="held" />
      </div>

      <div className={`mt-8 space-y-2 border-t pt-5 ${RULE}`}>
        {[
          { k: "diff", v: "− Product.legacySku: String" },
          { k: "verdict", v: "breaking", ink: STATUS.breaking.ink },
          { k: "scope", v: "5 published clients affected" },
        ].map((row) => (
          <div key={row.k} className="flex items-baseline gap-4">
            <span className="text-cc-nav-label w-16 shrink-0 font-mono text-[0.6rem] tracking-[0.12em] uppercase">
              {row.k}
            </span>
            <span className={`font-mono text-xs ${row.ink ?? "text-cc-ink"}`}>
              {row.v}
            </span>
          </div>
        ))}
      </div>
    </Blueprint>
  );
}

interface GateNodeProps {
  readonly title: string;
  readonly sub: string;
  readonly tone: "active" | "held";
}

function GateNode({ title, sub, tone }: GateNodeProps) {
  const armed = tone === "active";
  return (
    <div
      className={`flex-1 rounded-xl border px-4 py-3 ${
        armed
          ? "border-cc-success/40 bg-cc-success/[0.04]"
          : `${RULE} border-dashed`
      }`}
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-heading font-mono text-sm">{title}</span>
        {armed ? (
          <span className="text-cc-success">
            <CheckIcon />
          </span>
        ) : (
          <span className="text-cc-warning font-mono text-[0.55rem] tracking-[0.12em] uppercase">
            hold
          </span>
        )}
      </div>
      <span className="text-cc-ink-dim mt-1 block font-mono text-[0.62rem]">
        {sub}
      </span>
    </div>
  );
}

interface ConnectorProps {
  readonly blocked?: boolean;
}

/** A short rule between gate nodes; a blocked one is dashed and coral-inked. */
function Connector({ blocked = false }: ConnectorProps) {
  return (
    <div className="flex flex-col items-center">
      <span
        aria-hidden="true"
        className={`block h-px w-8 sm:w-12 ${
          blocked ? "bg-cc-danger/50" : "bg-[#7c92c6]/40"
        }`}
        style={blocked ? { backgroundImage: "none" } : undefined}
      />
      {blocked && (
        <span className="text-cc-danger mt-1 font-mono text-[0.5rem] tracking-[0.1em] uppercase">
          blocked
        </span>
      )}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* SIGNATURE — stamped schema diff with a pinned comment thread         */
/* ------------------------------------------------------------------ */

interface DiffLine {
  readonly sign: "+" | "-" | " ";
  readonly text: string;
  readonly status?: Status;
  readonly pinned?: boolean;
}

const DIFF_LINES: readonly DiffLine[] = [
  { sign: " ", text: "type Product {" },
  { sign: " ", text: "  id: ID!" },
  { sign: "+", text: "  slug: String", status: "safe" },
  {
    sign: "+",
    text: "  shippingClass: ShippingClass",
    status: "dangerous",
  },
  {
    sign: "-",
    text: "  legacySku: String",
    status: "breaking",
    pinned: true,
  },
  { sign: " ", text: "  price: Money!" },
  { sign: " ", text: "}" },
];

function StampedDiff() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-2xl border">
      {/* Code chrome header. */}
      <div className="bg-cc-code-header border-cc-card-border flex items-center justify-between border-b px-5 py-3">
        <span className="text-cc-ink-dim font-mono text-[0.72rem]">
          schema.graphql
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-[0.12em] uppercase">
          registry diff · v14 → v15
        </span>
      </div>

      <div className="font-mono text-[0.78rem] leading-relaxed">
        {DIFF_LINES.map((line, i) => {
          const status = line.status ? STATUS[line.status] : undefined;
          const signInk =
            line.sign === "+"
              ? "text-cc-success"
              : line.sign === "-"
                ? "text-cc-danger"
                : "text-cc-ink-faint";
          return (
            <div key={i}>
              <div
                className={`flex items-center gap-3 px-5 py-1.5 ${
                  line.sign === "+"
                    ? "bg-cc-success/[0.05]"
                    : line.sign === "-"
                      ? "bg-cc-danger/[0.06]"
                      : ""
                }`}
              >
                <span className={`w-3 shrink-0 ${signInk}`}>{line.sign}</span>
                <span
                  className={`flex-1 ${
                    line.sign === " " ? "text-cc-ink-dim" : "text-cc-ink"
                  }`}
                >
                  {line.text}
                </span>
                {status && (
                  <span
                    className={`shrink-0 rounded-full border border-current/40 px-2 py-0.5 text-[0.58rem] tracking-[0.12em] uppercase ${status.ink}`}
                  >
                    {status.label}
                  </span>
                )}
              </div>

              {/* Pinned resolve thread, anchored under the breaking line. */}
              {line.pinned && <PinnedThread />}
            </div>
          );
        })}
      </div>
    </div>
  );
}

/**
 * The page's signature device: a Resolve / comment thread pinned to the
 * breaking diff line, with its state synced to the registry verdict.
 */
function PinnedThread() {
  return (
    <div className="bg-cc-surface/70 border-cc-danger/30 mx-4 my-3 rounded-xl border-l-2 px-4 py-3">
      <div className="flex items-center justify-between">
        <span className="text-cc-danger font-mono text-[0.62rem] tracking-[0.14em] uppercase">
          breaking · removal in use
        </span>
        <span className="text-cc-warning border-cc-warning/40 rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
          unresolved
        </span>
      </div>
      <p className="text-cc-ink mt-2 text-[0.78rem] leading-relaxed">
        <span className="text-cc-heading font-medium">registry</span> · This
        field is read by 23 persisted operations across{" "}
        <span className="text-cc-heading">5 published clients</span>. Removing
        it now would break responses in production.
      </p>
      <div className={`mt-3 flex items-center gap-3 border-t pt-3 ${RULE}`}>
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          resolve →
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          deprecate first, then remove next cycle
        </span>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* PR CHECK CARD                                                       */
/* ------------------------------------------------------------------ */

interface CheckRow {
  readonly name: string;
  readonly result: string;
  readonly status: Status | "pass";
}

const CHECK_ROWS: readonly CheckRow[] = [
  { name: "slug added", result: "no consumer impact", status: "safe" },
  {
    name: "shippingClass added",
    result: "new enum input · review",
    status: "dangerous",
  },
  {
    name: "legacySku removed",
    result: "5 published clients affected",
    status: "breaking",
  },
];

function PrCheckCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm">
      <div className="border-cc-card-border flex items-center justify-between border-b px-5 py-4">
        <div className="flex items-center gap-3">
          <span className="text-cc-danger border-cc-danger/40 rounded-md border px-2 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.12em] uppercase">
            Fail
          </span>
          <span className="text-cc-heading text-sm font-medium">
            Registry check
          </span>
        </div>
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          1 breaking · 1 dangerous · 1 safe
        </span>
      </div>

      <ul>
        {CHECK_ROWS.map((row) => {
          const ink =
            row.status === "pass" ? "text-cc-success" : STATUS[row.status].ink;
          return (
            <li
              key={row.name}
              className="border-cc-card-border flex items-center gap-3 border-b px-5 py-3 last:border-b-0"
            >
              <span className={`shrink-0 ${ink}`}>
                {row.status === "breaking" ? (
                  <CrossGlyph />
                ) : row.status === "dangerous" ? (
                  <DotGlyph />
                ) : (
                  <CheckIcon />
                )}
              </span>
              <span className="text-cc-ink flex-1 font-mono text-xs">
                {row.name}
              </span>
              <span className="text-cc-ink-dim font-mono text-[0.68rem]">
                {row.result}
              </span>
            </li>
          );
        })}
      </ul>

      <div className="border-cc-card-border flex items-center justify-between border-t px-5 py-3">
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          merge blocked until the contract is resolved
        </span>
        <span className="text-cc-nav-label hover:text-cc-ink border-cc-card-border hover:border-cc-card-border-hover rounded-md border px-2.5 py-1 font-mono text-[0.6rem] tracking-[0.1em] uppercase transition-colors">
          re-run
        </span>
      </div>
    </div>
  );
}

function CrossGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={14} height={14} aria-hidden="true">
      <path
        d="M4 4 L12 12 M12 4 L4 12"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

function DotGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={14} height={14} aria-hidden="true">
      <circle cx="8" cy="8" r="3.5" fill="currentColor" />
    </svg>
  );
}

/* ------------------------------------------------------------------ */
/* CLIENT-IMPACT ROWS                                                  */
/* ------------------------------------------------------------------ */

interface ImpactRow {
  readonly client: string;
  readonly meta: string;
  readonly ok: number;
  readonly total: number;
  readonly note: string;
  readonly status: Status | "queued";
}

const IMPACT_ROWS: readonly ImpactRow[] = [
  {
    client: "web",
    meta: "react · 12 ops",
    ok: 5,
    total: 5,
    note: "no affected operations",
    status: "safe",
  },
  {
    client: "mobile",
    meta: ".NET · 9 ops",
    ok: 3,
    total: 5,
    note: "2 operations read legacySku",
    status: "breaking",
  },
  {
    client: "partner",
    meta: "external · 4 ops",
    ok: 0,
    total: 0,
    note: "manifest not yet published",
    status: "queued",
  },
];

function ImpactMatrix() {
  return (
    <div className={`divide-y ${RULE_DIVIDE}`}>
      {IMPACT_ROWS.map((row) => {
        const queued = row.status === "queued";
        const ink = queued
          ? "text-cc-nav-label"
          : STATUS[row.status as Status].ink;
        return (
          <div
            key={row.client}
            className="grid grid-cols-[1fr_auto] items-center gap-4 py-5 sm:grid-cols-[1fr_8rem_auto]"
          >
            <div>
              <div className="flex items-baseline gap-3">
                <span className="text-cc-heading font-mono text-sm">
                  {row.client}
                </span>
                <span className="text-cc-ink-dim font-mono text-[0.62rem]">
                  {row.meta}
                </span>
              </div>
              <p className="text-cc-ink-dim mt-1 text-[0.78rem]">{row.note}</p>
            </div>

            {/* Coverage track: filled segments = ops that stay green. */}
            <div className="hidden items-center gap-1 sm:flex">
              {queued ? (
                <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.1em] uppercase">
                  queued
                </span>
              ) : (
                Array.from({ length: row.total }).map((_, i) => (
                  <span
                    key={i}
                    aria-hidden="true"
                    className={`h-2 w-4 rounded-sm ${
                      i < row.ok
                        ? STATUS.safe.line
                        : `${STATUS[row.status as Status].line} opacity-90`
                    }`}
                  />
                ))
              )}
            </div>

            <span
              className={`justify-self-end font-mono text-[0.7rem] tracking-[0.06em] ${ink}`}
            >
              {queued ? "—" : `${row.ok}/${row.total} ok`}
            </span>
          </div>
        );
      })}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* VERSION TIMELINE                                                    */
/* ------------------------------------------------------------------ */

interface VersionPoint {
  readonly v: string;
  readonly label: string;
  readonly status: Status;
}

const VERSIONS: readonly VersionPoint[] = [
  { v: "v11", label: "field added", status: "safe" },
  { v: "v12", label: "enum widened", status: "dangerous" },
  { v: "v13", label: "removal blocked", status: "breaking" },
  { v: "v14", label: "deprecated", status: "safe" },
  { v: "v15", label: "pending", status: "dangerous" },
];

function VersionTimeline() {
  return (
    <div className="relative">
      <div className={`absolute top-2 right-0 left-0 h-px ${BLUEPRINT_BG}`} />
      <ol className="relative grid grid-cols-5 gap-2">
        {VERSIONS.map((p) => (
          <li key={p.v} className="flex flex-col items-center text-center">
            <span
              aria-hidden="true"
              className={`border-cc-bg size-4 rounded-full border-2 ${STATUS[p.status].line}`}
            />
            <span className="text-cc-heading mt-3 font-mono text-[0.72rem]">
              {p.v}
            </span>
            <span className="text-cc-ink-dim mt-1 font-mono text-[0.6rem] leading-snug">
              {p.label}
            </span>
          </li>
        ))}
      </ol>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* BIG-NUMBER RELIABILITY BAND                                         */
/* ------------------------------------------------------------------ */

interface StatProps {
  readonly value: string;
  readonly label: string;
  readonly ink?: string;
}

const STATS: readonly StatProps[] = [
  { value: "0", label: "breaking changes shipped", ink: "text-cc-success" },
  { value: "12", label: "published clients guarded" },
  { value: "100%", label: "releases gated in CI" },
];

function Stat({ value, label, ink }: StatProps) {
  return (
    <div>
      <p
        className={`font-heading text-h2 leading-none font-light tabular-nums ${ink ?? "text-cc-heading"}`}
      >
        {value}
      </p>
      <p className="text-cc-ink-dim mt-3 text-sm text-pretty">{label}</p>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* PAGE                                                                */
/* ------------------------------------------------------------------ */

export default function ReleaseSafetyPreviewPage() {
  return (
    <article>
      {/* ---------------- HERO ---------------- */}
      <header className="grid items-center gap-12 py-16 sm:py-24 lg:grid-cols-[1.1fr_0.9fr] lg:gap-16">
        <div>
          <Eyebrow>Release safety</Eyebrow>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 leading-[1.02] font-semibold text-balance">
            Change contracts with a safety net.
          </h1>
          <p className="lead text-cc-ink-dim mt-7 max-w-xl text-pretty">
            Every schema change is classified safe, dangerous, or breaking,
            checked against the operations your published clients actually use,
            and stopped in CI before a consumer ever notices.
          </p>
          <div className="mt-9 flex flex-wrap gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/platform/continuous-integration">
              Read the Docs
            </OutlineButton>
          </div>
          <p className="text-cc-ink-dim mt-6 font-mono text-[0.7rem] tracking-[0.06em]">
            validate → publish · breaking changes never reach prod silently
          </p>
        </div>
        <GateSchematic />
      </header>

      <hr className={RULE} />

      {/* ---------------- 01 — CLASSIFY ---------------- */}
      <section className="py-20 sm:py-24">
        <NumberedHeading index="01" kicker="Read the diff like a reviewer">
          A verdict on every line, in its own color.
        </NumberedHeading>

        <div className="mt-12 grid gap-12 lg:grid-cols-[0.85fr_1.15fr] lg:gap-16">
          <div className="space-y-5">
            <p className="text-cc-ink text-base/relaxed text-pretty">
              The registry diffs the proposed schema against the version in use
              and stamps each change. An added optional field reads{" "}
              <span className={STATUS.safe.ink}>safe</span>. A widened enum a
              consumer might not handle reads{" "}
              <span className={STATUS.dangerous.ink}>dangerous</span>. A removal
              that registered operations still depend on reads{" "}
              <span className={STATUS.breaking.ink}>breaking</span>.
            </p>
            <p className="text-cc-ink-dim text-base/relaxed text-pretty">
              The breaking line carries a pinned thread you resolve before the
              change can move. Its state is the registry verdict, not a comment
              someone forgot to close, so the review and the gate can never
              disagree.
            </p>
            <p className="text-cc-ink-dim text-sm/relaxed text-pretty">
              Counts read “published clients affected” from the registry of
              persisted operations. They scope impact; they do not claim to name
              every client and version.
            </p>
          </div>

          <StampedDiff />
        </div>
      </section>

      <hr className={RULE} />

      {/* ---------------- 02 — GATE ---------------- */}
      <section className="py-20 sm:py-24">
        <NumberedHeading index="02" kicker="The gate lives in your pipeline">
          A breaking change fails the build, not production.
        </NumberedHeading>

        <div className="mt-12 grid gap-12 lg:grid-cols-2 lg:gap-16">
          <PrCheckCard />

          <div className="space-y-5 lg:pt-2">
            <p className="text-cc-ink text-base/relaxed text-pretty">
              The same verdict that stamps the diff posts back as a required
              check on the pull request. A breaking change fails it, and the
              merge is held until the contract is resolved or the change is
              walked back to something safe.
            </p>
            <p className="text-cc-ink-dim text-base/relaxed text-pretty">
              There is no separate dashboard to remember to open. The check is
              where the work already is, so unsafe releases are stopped before a
              consumer discovers them, not after.
            </p>
            <div className={`mt-6 flex flex-wrap gap-6 border-t pt-6 ${RULE}`}>
              {[
                ["validate", "diff vs. live schema + ops"],
                ["classify", "safe / dangerous / breaking"],
                ["publish", "only when the gate is green"],
              ].map(([k, v]) => (
                <div key={k} className="min-w-[8rem]">
                  <p className={`font-mono text-xs ${BLUEPRINT}`}>{k}</p>
                  <p className="text-cc-ink-dim mt-1 text-[0.78rem]">{v}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <hr className={RULE} />

      {/* ---------------- 03 — IMPACT ---------------- */}
      <section className="py-20 sm:py-24">
        <NumberedHeading index="03" kicker="Scoped to who actually calls it">
          See which published clients the change touches.
        </NumberedHeading>

        <div className="mt-12 grid gap-12 lg:grid-cols-[0.8fr_1.2fr] lg:gap-16">
          <div className="space-y-5">
            <p className="text-cc-ink text-base/relaxed text-pretty">
              Published clients register the operations they run. When a change
              touches a field those operations read, the registry knows which
              clients are affected and how much of their surface stays green.
            </p>
            <p className="text-cc-ink-dim text-base/relaxed text-pretty">
              Generated .NET clients (Strawberry Shake, via MSBuild codegen)
              close the loop a second time: contract drift regenerates into the
              consumer’s build as feedback, before the app ships.
            </p>
          </div>

          <Blueprint label="impact · published clients affected">
            <ImpactMatrix />
          </Blueprint>
        </div>

        <div className="mt-16">
          <Eyebrow>Schema version history</Eyebrow>
          <div className="mt-8 max-w-3xl">
            <VersionTimeline />
          </div>
          <p className="text-cc-ink-dim mt-6 max-w-2xl text-sm/relaxed text-pretty">
            Every published version is tagged and kept. A change that turned out
            badly can be backed out by republishing an earlier version, with a
            chronological audit log of who shipped what.
          </p>
        </div>
      </section>

      <hr className={RULE} />

      {/* ---------------- RELIABILITY BAND ---------------- */}
      <section className="py-20 sm:py-24">
        <div className="grid gap-12 sm:grid-cols-3 sm:gap-8">
          {STATS.map((s) => (
            <Stat key={s.label} {...s} />
          ))}
        </div>
      </section>

      <hr className={RULE} />

      {/* ---------------- HONESTY BEAT ---------------- */}
      <section className="py-20 sm:py-24">
        <div className="grid gap-10 lg:grid-cols-[0.4fr_0.6fr] lg:gap-16">
          <div>
            <Eyebrow>Honest scoping</Eyebrow>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-[1.2] font-semibold text-balance">
              “Published clients affected,” not “exactly who breaks.”
            </p>
            <p className="text-cc-ink-dim mt-6 text-base/relaxed text-pretty">
              The category likes to promise certainty. We scope it to what the
              registries can prove: a change checked against the operations
              published clients registered, and a breaking change validated
              before production. That is a smaller claim, and a more trustworthy
              one when you are the engineer signing off the release.
            </p>
          </div>
        </div>
      </section>

      <hr className={RULE} />

      {/* ---------------- CLOSING CTA ---------------- */}
      <section className="py-20 text-center sm:py-28">
        <h2 className="font-heading text-cc-heading text-h3 mx-auto max-w-2xl leading-[1.1] font-semibold text-balance">
          Evolve the graph with confidence.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-6 max-w-xl text-base/relaxed text-pretty">
          Wire the registry checks into your pipeline and let CI stop unsafe
          releases before a consumer ever sees them.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/continuous-integration">
            Read the Docs
          </OutlineButton>
        </div>
        <p className="text-cc-ink-dim mt-8 text-sm">
          Or explore{" "}
          <Link
            href="/platform/continuous-integration"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            continuous integration
          </Link>{" "}
          and the wider{" "}
          <Link
            href="/platform"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            platform
          </Link>
          .
        </p>
      </section>
    </article>
  );
}
