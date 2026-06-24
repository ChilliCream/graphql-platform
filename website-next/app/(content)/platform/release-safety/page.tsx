import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Safe GraphQL Schema Evolution",
  description:
    "Safe GraphQL schema evolution: classify every change safe, dangerous, or breaking, check the operations published clients use, and stop unsafe releases in CI.",
  keywords: [
    "safe GraphQL schema evolution",
    "GraphQL schema registry",
    "client registry",
    "breaking change detection",
    "schema checks in CI",
    "generated .NET GraphQL clients",
    "published clients affected",
    "validate publish gate",
    "persisted operations",
    "schema version history",
  ],
  openGraph: {
    title: "Safe GraphQL Schema Evolution: Catch Breaks in CI",
    description:
      "Classify every change safe, dangerous, or breaking, check the operations published clients use, and stop unsafe releases in CI before they reach a consumer.",
  },
};

/**
 * The three change classes the registry assigns to a proposed diff. The hue is
 * applied only as a ring/dot/label tint (cc-success / cc-warning / cc-danger),
 * never as a fill, so the cards stay calm.
 */
type Verdict = "safe" | "dangerous" | "breaking";

const VERDICT_META: Record<
  Verdict,
  { readonly label: string; readonly ring: string; readonly text: string }
> = {
  safe: {
    label: "Safe",
    ring: "ring-cc-success/40",
    text: "text-cc-success",
  },
  dangerous: {
    label: "Dangerous",
    ring: "ring-cc-warning/40",
    text: "text-cc-warning",
  },
  breaking: {
    label: "Breaking",
    ring: "ring-cc-danger/40",
    text: "text-cc-danger",
  },
};

/** One schema change and how it lands in each classification column. */
interface ChangeRow {
  readonly change: string;
  readonly detail: string;
  readonly verdict: Verdict;
  /** Shown only when the gate is keyed to published-client usage. */
  readonly affected?: string;
}

const CHANGE_ROWS: readonly ChangeRow[] = [
  {
    change: "Add a field",
    detail: "New optional field on an existing type.",
    verdict: "safe",
    affected: "0 published clients affected",
  },
  {
    change: "Add an enum member",
    detail: "A client not handling the new value may misbehave.",
    verdict: "dangerous",
    affected: "3 published clients affected",
  },
  {
    change: "Loosen output nullability",
    detail:
      "A non-null field becomes nullable; clients assuming a value can break.",
    verdict: "breaking",
    affected: "2 published clients affected",
  },
  {
    change: "Remove a field in use",
    detail: "Field is read by registered operations.",
    verdict: "breaking",
    affected: "5 published clients affected",
  },
];

/** Small status dot in the verdict tint, used in the matrix header and cards. */
function VerdictDot({ verdict }: { readonly verdict: Verdict }) {
  return (
    <span
      aria-hidden="true"
      className={[
        "inline-block size-2 rounded-full ring-2 ring-offset-0",
        VERDICT_META[verdict].ring,
        VERDICT_META[verdict].text,
        "bg-current",
      ].join(" ")}
    />
  );
}

/**
 * The "validate -> publish" gate badge shown in the hero. Reuses the
 * ChangePipeline "guarded" pill idiom so the safety stamp reads immediately.
 */
function GateBadge() {
  const steps = [
    { label: "validate", done: true },
    { label: "classify", done: true },
    { label: "publish", done: false },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-sm rounded-2xl border p-5 backdrop-blur-sm">
      <div className="flex items-center justify-between">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          contract review
        </p>
        <span className="border-cc-accent/50 text-cc-accent rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
          guarded
        </span>
      </div>

      <div className="mt-4 flex items-center justify-center gap-1.5">
        {steps.map((step, index) => (
          <span key={step.label} className="flex items-center">
            {index > 0 && (
              <span
                aria-hidden="true"
                className="text-cc-ink-faint px-0.5 text-sm"
              >
                &rarr;
              </span>
            )}
            <span
              className={[
                "rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem]",
                step.done
                  ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
                  : "border-cc-ink-faint text-cc-ink-dim border-dashed",
              ].join(" ")}
            >
              {step.label}
            </span>
          </span>
        ))}
      </div>

      <div className="border-cc-card-border mt-5 space-y-2 border-t pt-4">
        {[
          { label: "diff", value: "remove Product.legacySku" },
          { label: "verdict", value: "breaking" },
          { label: "scope", value: "5 published clients affected" },
        ].map((row) => (
          <div
            key={row.label}
            className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2"
          >
            <span className="text-cc-nav-label w-14 shrink-0 font-mono text-[0.55rem] tracking-[0.08em] uppercase">
              {row.label}
            </span>
            <span className="text-cc-ink font-mono text-xs">{row.value}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

/** Mobile fallback: one card per classification listing its example rows. */
function ClassificationCard({ verdict }: { readonly verdict: Verdict }) {
  const meta = VERDICT_META[verdict];
  const rows = CHANGE_ROWS.filter((row) => row.verdict === verdict);

  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 ring-1 backdrop-blur-sm",
        meta.ring,
      ].join(" ")}
    >
      <div className="flex items-center gap-2">
        <VerdictDot verdict={verdict} />
        <p className={["text-sm font-semibold", meta.text].join(" ")}>
          {meta.label}
        </p>
      </div>
      <ul className="mt-4 space-y-3">
        {rows.map((row) => (
          <li key={row.change} className="border-cc-card-border border-t pt-3">
            <p className="text-cc-heading text-sm font-medium">{row.change}</p>
            <p className="text-cc-ink-dim mt-1 text-xs">{row.detail}</p>
            {row.affected && (
              <p className="text-cc-ink mt-2 font-mono text-[0.65rem]">
                {row.affected}
              </p>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}

/**
 * The classification matrix. Wide screens get the three-column comparison
 * table; narrow screens collapse to stacked per-classification cards.
 */
function ClassificationMatrix() {
  const columns: readonly Verdict[] = ["safe", "dangerous", "breaking"];

  return (
    <>
      {/* Desktop / tablet: side-by-side comparison table. */}
      <div className="border-cc-card-border bg-cc-card-bg/40 hidden overflow-hidden rounded-3xl border backdrop-blur-sm md:block">
        <table className="w-full border-collapse text-left text-sm">
          <thead>
            <tr className="border-cc-card-border border-b">
              <th className="text-cc-nav-label px-5 py-4 font-mono text-[0.6rem] font-medium tracking-[0.12em] uppercase">
                Schema change
              </th>
              {columns.map((verdict) => (
                <th key={verdict} className="px-5 py-4">
                  <span className="flex items-center gap-2">
                    <VerdictDot verdict={verdict} />
                    <span
                      className={[
                        "text-xs font-semibold tracking-[0.1em] uppercase",
                        VERDICT_META[verdict].text,
                      ].join(" ")}
                    >
                      {VERDICT_META[verdict].label}
                    </span>
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {CHANGE_ROWS.map((row) => (
              <tr
                key={row.change}
                className="border-cc-card-border border-b last:border-b-0"
              >
                <td className="px-5 py-4 align-top">
                  <p className="text-cc-heading font-medium">{row.change}</p>
                  <p className="text-cc-ink-dim mt-1 text-xs">{row.detail}</p>
                </td>
                {columns.map((verdict) => {
                  const hit = row.verdict === verdict;
                  return (
                    <td key={verdict} className="px-5 py-4 align-top">
                      {hit ? (
                        <div
                          className={[
                            "rounded-xl px-3 py-2.5 ring-1",
                            VERDICT_META[verdict].ring,
                          ].join(" ")}
                        >
                          <p
                            className={[
                              "font-mono text-[0.7rem] font-medium",
                              VERDICT_META[verdict].text,
                            ].join(" ")}
                          >
                            {VERDICT_META[verdict].label}
                          </p>
                          {row.affected && (
                            <p className="text-cc-ink mt-1.5 font-mono text-[0.65rem]">
                              {row.affected}
                            </p>
                          )}
                        </div>
                      ) : (
                        <span
                          aria-hidden="true"
                          className="text-cc-ink-faint font-mono text-xs"
                        >
                          &middot;
                        </span>
                      )}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile: stacked classification cards. */}
      <div className="grid gap-4 md:hidden">
        {columns.map((verdict) => (
          <ClassificationCard key={verdict} verdict={verdict} />
        ))}
      </div>
    </>
  );
}

/** One step in the horizontal validate -> publish gate strip. */
interface PipelineStep {
  readonly stage: string;
  readonly title: string;
  readonly body: string;
  readonly checked: boolean;
}

const PIPELINE_STEPS: readonly PipelineStep[] = [
  {
    stage: "PR",
    title: "Validate",
    body: "Registry checks compare the proposed schema against the persisted operations your published clients depend on.",
    checked: true,
  },
  {
    stage: "Release",
    title: "CI gate",
    body: "A breaking change fails the build before merge. Approval gates hold dangerous changes for a human checkpoint.",
    checked: true,
  },
  {
    stage: "Deploy",
    title: "Publish",
    body: "The schema is published per stage. An earlier tagged version can be republished to roll a change back.",
    checked: false,
  },
  {
    stage: "Consumer",
    title: "Build feedback",
    body: "Generated .NET clients regenerate from the schema, so contract drift surfaces as a build error before the app ships.",
    checked: false,
  },
];

function PipelineStrip() {
  return (
    <ol className="grid gap-4 lg:grid-cols-4">
      {PIPELINE_STEPS.map((step, index) => (
        <li
          key={step.title}
          className="border-cc-card-border bg-cc-card-bg/60 relative rounded-2xl border p-5 backdrop-blur-sm"
        >
          <div className="flex items-center justify-between">
            <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
              {step.stage}
            </span>
            {step.checked ? (
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
            ) : (
              <span className="text-cc-nav-label font-mono text-[0.6rem]">
                {index + 1}
              </span>
            )}
          </div>
          <p className="text-cc-heading mt-3 text-sm font-semibold">
            {step.title}
          </p>
          <p className="text-cc-ink-dim mt-2 text-xs/relaxed">{step.body}</p>
          {index < PIPELINE_STEPS.length - 1 && (
            <span
              aria-hidden="true"
              className="text-cc-ink-faint absolute top-1/2 -right-3 hidden -translate-y-1/2 text-lg lg:block"
            >
              &rarr;
            </span>
          )}
        </li>
      ))}
    </ol>
  );
}

/** Reusable value section with an optional inline visual on the side. */
function ValueSection({
  eyebrow,
  heading,
  children,
  visual,
  flip = false,
}: {
  readonly eyebrow: string;
  readonly heading: string;
  readonly children: ReactNode;
  readonly visual: ReactNode;
  readonly flip?: boolean;
}) {
  return (
    <div className="grid items-center gap-8 lg:grid-cols-2 lg:gap-12">
      <div className={flip ? "lg:order-2" : "lg:order-1"}>
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
          {eyebrow}
        </span>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
          {heading}
        </h2>
        <div className="text-cc-ink mt-5 space-y-4 text-base/relaxed text-pretty">
          {children}
        </div>
      </div>
      <div className={flip ? "lg:order-1" : "lg:order-2"}>{visual}</div>
    </div>
  );
}

/** Visual for the two-loop section: registry loop over generated-client loop. */
function TwoLoopVisual() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-md rounded-2xl border p-5 backdrop-blur-sm">
      <div className="border-cc-card-border bg-cc-surface rounded-xl border p-4">
        <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          registry loop
        </p>
        <div className="mt-3 flex flex-wrap items-center gap-1.5">
          {["schema diff", "registry check", "published clients"].map(
            (label, index) => (
              <span key={label} className="flex items-center">
                {index > 0 && (
                  <span
                    aria-hidden="true"
                    className="text-cc-ink-faint px-0.5 text-sm"
                  >
                    &rarr;
                  </span>
                )}
                <span className="border-cc-card-border text-cc-ink rounded-lg border px-2.5 py-1.5 font-mono text-[0.62rem]">
                  {label}
                </span>
              </span>
            ),
          )}
        </div>
      </div>

      <div className="my-3 text-center">
        <span className="text-cc-ink-dim font-mono text-[0.6rem]">
          both converge before it ships
        </span>
      </div>

      <div className="border-cc-card-border bg-cc-surface rounded-xl border p-4">
        <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          generated-client loop
        </p>
        <div className="mt-3 flex flex-wrap items-center gap-1.5">
          {["schema change", "regenerate", "build feedback"].map(
            (label, index) => (
              <span key={label} className="flex items-center">
                {index > 0 && (
                  <span
                    aria-hidden="true"
                    className="text-cc-ink-faint px-0.5 text-sm"
                  >
                    &rarr;
                  </span>
                )}
                <span className="border-cc-card-border text-cc-ink rounded-lg border px-2.5 py-1.5 font-mono text-[0.62rem]">
                  {label}
                </span>
              </span>
            ),
          )}
        </div>
      </div>
    </div>
  );
}

/** Visual for the stages section: dev / QA / prod ladder with a held promotion. */
function StageLadderVisual() {
  const stages = [
    { name: "dev", note: "v14 active", state: "done" as const },
    { name: "QA", note: "v14 active", state: "active" as const },
    { name: "prod", note: "v13, awaiting approval", state: "held" as const },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-md rounded-2xl border p-5 backdrop-blur-sm">
      <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        promotion path
      </p>
      <div className="mt-4 space-y-2.5">
        {stages.map((stage) => (
          <div
            key={stage.name}
            className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2.5"
          >
            <span
              className={[
                "font-mono text-xs font-medium",
                stage.state === "held" ? "text-cc-warning" : "text-cc-accent",
              ].join(" ")}
            >
              {stage.name}
            </span>
            <span className="text-cc-ink-dim flex-1 font-mono text-[0.62rem]">
              {stage.note}
            </span>
            {stage.state === "done" && (
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
            )}
            {stage.state === "held" && (
              <span className="text-cc-warning font-mono text-[0.55rem] tracking-[0.08em] uppercase">
                hold
              </span>
            )}
          </div>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-4 text-center font-mono text-[0.58rem]">
        --wait-for-approval &middot; chronological audit log
      </p>
    </div>
  );
}

/** Final pre-publish CI checklist card. */
const CI_CHECKLIST: readonly string[] = [
  "Schema validates against the registered schema version",
  "Every diff is classified safe, dangerous, or breaking",
  "Proposed change is checked against published clients' operations",
  "Approval gate cleared for dangerous and breaking changes",
];

export default function ReleaseSafetyPage() {
  return (
    <>
      {/* Two-column hero: copy on the left, validate -> publish gate on the right. */}
      <section className="grid items-center gap-10 py-16 sm:py-24 lg:grid-cols-2 lg:gap-16">
        <div>
          <p className="text-cc-ink-dim font-mono text-xs font-semibold tracking-widest uppercase">
            Release safety
          </p>
          <h1 className="font-heading text-cc-heading mt-4 text-4xl leading-[1.05] font-semibold tracking-tight sm:text-5xl lg:text-6xl">
            Change contracts with a safety net.
          </h1>
          <p className="text-cc-ink-dim mt-6 max-w-xl text-base sm:text-lg">
            Safe GraphQL schema evolution from the registry side: classify every
            change, check the operations your published clients actually use,
            and stop unsafe releases in CI before they reach a consumer.
          </p>
        </div>
        <div>
          <GateBadge />
        </div>
      </section>

      {/* Classification matrix: SAFE / DANGEROUS / BREAKING. */}
      <section className="py-12">
        <div className="max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold">
            Classify the change, not just the diff.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            A schema registry tracks what the SDL says. A client registry tracks
            the persisted operations published clients depend on. Nitro keys the
            verdict to both, so a change is graded against real usage rather
            than whether the SDL moved.
          </p>
        </div>
        <div className="mt-8">
          <ClassificationMatrix />
        </div>
        <p className="text-cc-ink-dim mt-4 text-xs">
          Counts read &ldquo;published clients affected&rdquo; from the client
          registry of persisted operations. They scope impact; they are not a
          claim to name every client and version.
        </p>
      </section>

      {/* Horizontal validate -> publish -> CI gate -> build feedback strip. */}
      <section className="py-12">
        <div className="max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold">
            One gate, mapped to your pipeline.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            The lifecycle is CI-native: validate, then publish, mapped onto PR,
            release, and deploy. A breaking change is caught in the PR build,
            and generated clients add a second loop so drift can surface again
            in the consumer&rsquo;s build.
          </p>
        </div>
        <div className="mt-8">
          <PipelineStrip />
        </div>
      </section>

      {/* Value section: the second feedback loop. */}
      <section className="py-12">
        <ValueSection
          eyebrow="Two loops, one workflow"
          heading="A break can surface twice before it ships."
          visual={<TwoLoopVisual />}
        >
          <p>
            Most registries give you one check: the diff against historical or
            sampled traffic. Nitro pairs that with a declared, governed
            contract, the persisted operations your published clients
            registered, so the gate keys to what consumers actually call.
          </p>
          <p>
            Then a second loop runs in the consumer. Generated .NET clients
            (Strawberry Shake) regenerate from the schema through MSBuild, so a
            contract change shows up as build feedback in the app before it
            ships, not as a runtime error after deploy.
          </p>
        </ValueSection>
      </section>

      {/* Value section: stages, version history, rollback. */}
      <section className="py-12">
        <ValueSection
          eyebrow="Per environment"
          heading="Validate per stage, promote with a checkpoint."
          flip
          visual={<StageLadderVisual />}
        >
          <p>
            Stages (dev, QA, prod) each pin one active schema version and their
            own published client versions, so a change is validated for the
            environment it is about to enter, not just in the abstract.
          </p>
          <p>
            Approval gates (
            <code className="text-cc-accent">--wait-for-approval</code>) and a
            chronological deployment audit log keep a human in the loop on risky
            changes. Version history lets you republish an earlier tagged schema
            to back a change out.
          </p>
        </ValueSection>
      </section>

      {/* Honesty / credibility beat. */}
      <section className="py-12">
        <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-3xl rounded-3xl border p-8 backdrop-blur-sm sm:p-10">
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
            Honest scoping
          </span>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-tight font-semibold text-balance">
            &ldquo;Published clients affected,&rdquo; not &ldquo;exactly who
            breaks.&rdquo;
          </h2>
          <p className="text-cc-ink mt-5 text-base/relaxed">
            The category likes to promise certainty. We scope it to what the
            registries can actually prove: a change is checked against the
            operations published clients registered, and a breaking change is
            validated before production. That is a smaller claim, and a more
            trustworthy one when you are the engineer signing off the release.
          </p>
        </div>
      </section>

      {/* Pre-publish CI checklist + learn more + CTA. */}
      <section className="py-12">
        <div className="grid items-start gap-8 lg:grid-cols-2 lg:gap-12">
          <div className="border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-8 backdrop-blur-sm">
            <h2 className="font-heading text-cc-heading text-h4 leading-tight font-semibold">
              Pass before publish.
            </h2>
            <ul className="mt-6 space-y-4">
              {CI_CHECKLIST.map((item) => (
                <li key={item} className="flex items-start gap-3">
                  <span className="text-cc-accent mt-0.5 shrink-0">
                    <CheckIcon />
                  </span>
                  <span className="text-cc-ink text-sm/relaxed">{item}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="flex flex-col justify-center">
            <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold text-balance">
              Evolve the graph with confidence.
            </h2>
            <p className="text-cc-ink mt-5 text-base/relaxed">
              Wire the registry checks into your pipeline and let CI stop unsafe
              releases before a consumer ever sees them.
            </p>
            <div className="mt-8 flex flex-wrap gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/nitro/apis/client-registry">
                Read the Docs
              </OutlineButton>
            </div>
            <p className="text-cc-ink-dim mt-6 text-sm">
              Learn more about{" "}
              <Link
                href="/platform/continuous-integration"
                className="text-cc-accent hover:text-cc-accent-hover transition-colors"
              >
                continuous integration
              </Link>
              ,{" "}
              <Link
                href="/platform/analytics"
                className="text-cc-accent hover:text-cc-accent-hover transition-colors"
              >
                analytics
              </Link>
              , or the wider{" "}
              <Link
                href="/platform"
                className="text-cc-accent hover:text-cc-accent-hover transition-colors"
              >
                platform
              </Link>
              .
            </p>
          </div>
        </div>
      </section>
    </>
  );
}
