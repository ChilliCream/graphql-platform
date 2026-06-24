import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroSchema } from "@/src/nitro";

export const metadata: Metadata = {
  title: "GraphQL CI/CD Safety Net for Schema Evolution",
  description:
    "Ship GraphQL changes safely. Nitro classifies every diff, names published clients affected, and stops breaking releases in your CI pipeline before deploy.",
  keywords: [
    "GraphQL CI/CD",
    "schema validation",
    "Nitro CLI",
    "schema registry",
    "client registry",
    "breaking change detection",
    "GitHub Actions GraphQL",
    "Azure DevOps GraphQL",
    "published clients affected",
    "schema rollback",
  ],
  openGraph: {
    title: "GraphQL CI/CD Safety Net for Schema Evolution",
    description:
      "Classify every diff, name published clients affected, and stop breaking GraphQL releases in CI before they reach a consumer.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Stance: confidence-led safety net. Teal accent for the "guarded"   */
/* affordance; status hues only as ink (text, hairline, dot).         */
/* ------------------------------------------------------------------ */

type Verdict = "safe" | "dangerous" | "breaking";

const VERDICT: Record<
  Verdict,
  { readonly label: string; readonly ink: string; readonly ring: string }
> = {
  safe: { label: "Safe", ink: "text-cc-success", ring: "ring-cc-success/40" },
  dangerous: {
    label: "Dangerous",
    ink: "text-cc-warning",
    ring: "ring-cc-warning/40",
  },
  breaking: {
    label: "Breaking",
    ink: "text-cc-danger",
    ring: "ring-cc-danger/40",
  },
};

/* ------------------------------------------------------------------ */
/* Atoms                                                              */
/* ------------------------------------------------------------------ */

function Eyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      {children}
    </span>
  );
}

function SectionTitle({ children }: { readonly children: ReactNode }) {
  return (
    <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
      {children}
    </h2>
  );
}

function VerdictDot({ verdict }: { readonly verdict: Verdict }) {
  return (
    <span
      aria-hidden="true"
      className={[
        "inline-block size-2 rounded-full ring-2",
        VERDICT[verdict].ring,
        VERDICT[verdict].ink,
        "bg-current",
      ].join(" ")}
    />
  );
}

/* ------------------------------------------------------------------ */
/* Hero. Confidence statement + dual CTA + a guarded-gate badge.       */
/* ------------------------------------------------------------------ */

function HeroGateCard() {
  const verdict: Verdict = "breaking";
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-md rounded-2xl border p-6 backdrop-blur-sm">
      <div className="flex items-center justify-between">
        <Eyebrow>pre-publish check</Eyebrow>
        <span className="border-cc-accent/50 text-cc-accent bg-cc-surface rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          blocked
        </span>
      </div>

      <div className="border-cc-card-border bg-cc-surface mt-5 rounded-xl border p-4">
        <p className="text-cc-ink-dim font-mono text-[0.6rem]">
          <span className="text-cc-nav-label">$</span> nitro schema validate
        </p>
        <div className="mt-3 space-y-1.5 font-mono text-[0.65rem] leading-relaxed">
          <p className="text-cc-ink">
            <span className="text-cc-accent">+</span> Order.tipAmount: Money
          </p>
          <p className="text-cc-ink">
            <span className={VERDICT.dangerous.ink}>~</span> ShipMethod adds
            value EXPRESS
          </p>
          <p className="text-cc-ink">
            <span className={VERDICT.breaking.ink}>-</span> Product.legacySku
          </p>
        </div>
      </div>

      <div className="border-cc-card-border mt-5 space-y-2 border-t pt-4">
        <div className="flex items-center gap-3">
          <Eyebrow>verdict</Eyebrow>
          <span className="flex items-center gap-2">
            <VerdictDot verdict={verdict} />
            <span
              className={[
                "font-mono text-xs font-medium",
                VERDICT[verdict].ink,
              ].join(" ")}
            >
              {VERDICT[verdict].label}
            </span>
          </span>
        </div>
        <div className="flex items-center gap-3">
          <Eyebrow>scope</Eyebrow>
          <span className="text-cc-ink font-mono text-xs">
            5 published clients affected
          </span>
        </div>
        <div className="flex items-center gap-3">
          <Eyebrow>action</Eyebrow>
          <span className="text-cc-ink font-mono text-xs">
            release held, rollback to v13 available
          </span>
        </div>
      </div>
    </div>
  );
}

function Hero() {
  return (
    <section className="grid items-center gap-10 py-16 sm:py-24 lg:grid-cols-2 lg:gap-16">
      <div>
        <Eyebrow>Continuous integration</Eyebrow>
        <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 font-semibold text-balance">
          We will not let you ship breaking changes that consumers care about.
        </h1>
        <p className="text-cc-ink-dim mt-6 max-w-xl text-base text-pretty sm:text-lg">
          Nitro&apos;s registry and CLI sit in your pipeline, classify every
          schema diff, name the published clients affected, and stop the release
          before it reaches a consumer. When something does slip, an earlier
          tagged version is one command away.
        </p>
        <div className="mt-8 flex flex-wrap gap-4">
          <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>
      </div>
      <div>
        <HeroGateCard />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Promise band: three confidence statements.                          */
/* ------------------------------------------------------------------ */

interface PromiseCard {
  readonly title: string;
  readonly body: string;
  readonly mono: string;
}

const PROMISES: readonly PromiseCard[] = [
  {
    title: "Catch breaking changes before deploy.",
    body: "The CLI runs in your PR build, validates the proposed schema against the registered version, and classifies every diff. A breaking change fails the build, not the deploy.",
    mono: "nitro schema validate",
  },
  {
    title: "Know which published clients are affected.",
    body: "The client registry tracks the operations your shipped clients depend on. The verdict reports published clients affected, so the reviewer sees scope, not just a verb.",
    mono: "published clients affected: 5",
  },
  {
    title: "Roll back to a tagged schema version.",
    body: "Every publish is tagged in registry history. Republish an earlier version per environment when something has to revert. The audit log keeps the trail.",
    mono: "nitro schema publish --tag v13",
  },
];

function PromiseBand() {
  return (
    <section className="py-12">
      <div className="max-w-3xl">
        <Eyebrow>Three promises</Eyebrow>
        <SectionTitle>The safety net, said plainly.</SectionTitle>
      </div>
      <div className="mt-10 grid gap-5 lg:grid-cols-3">
        {PROMISES.map((promise, index) => (
          <div
            key={promise.title}
            className="border-cc-card-border bg-cc-card-bg/60 group hover:border-cc-card-border-hover relative flex flex-col rounded-2xl border p-6 backdrop-blur-sm transition-colors"
          >
            <div className="flex items-center justify-between">
              <span className="border-cc-accent/40 text-cc-accent flex size-7 items-center justify-center rounded-full border font-mono text-xs">
                {index + 1}
              </span>
              <span className="text-cc-accent">
                <CheckIcon size={16} />
              </span>
            </div>
            <h3 className="font-heading text-cc-heading mt-5 text-lg leading-tight font-semibold text-balance">
              {promise.title}
            </h3>
            <p className="text-cc-ink mt-3 flex-1 text-sm/relaxed">
              {promise.body}
            </p>
            <div className="border-cc-card-border bg-cc-surface mt-5 rounded-lg border px-3 py-2">
              <p className="text-cc-ink-dim font-mono text-[0.65rem]">
                {promise.mono}
              </p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CI block: a real GitHub Actions step with nitro schema validate.    */
/* ------------------------------------------------------------------ */

interface CodeLine {
  readonly text: string;
  readonly tone?: "comment" | "key" | "string" | "value" | "plain";
}

/* GitHub Actions YAML, broken into tone-tagged lines. Indentation is preserved
   with non-breaking spaces in the rendered <pre>, so the snippet renders the
   same regardless of how the source is wrapped. */
const WORKFLOW_LINES: readonly CodeLine[] = [
  { text: "# .github/workflows/schema-check.yml", tone: "comment" },
  { text: "name: schema-check", tone: "key" },
  { text: "on:", tone: "key" },
  { text: "  pull_request:", tone: "key" },
  { text: "    branches: [main]", tone: "value" },
  { text: "", tone: "plain" },
  { text: "jobs:", tone: "key" },
  { text: "  validate:", tone: "key" },
  { text: "    runs-on: ubuntu-latest", tone: "value" },
  { text: "    steps:", tone: "key" },
  { text: "      - uses: actions/checkout@v4", tone: "value" },
  { text: "      - name: install nitro cli", tone: "key" },
  {
    text: "        run: dotnet tool install -g ChilliCream.Nitro.CommandLine",
    tone: "plain",
  },
  { text: "      - name: validate schema", tone: "key" },
  {
    text: "        run: nitro schema validate \\",
    tone: "plain",
  },
  {
    text: "          --api-id $NITRO_API_ID \\",
    tone: "plain",
  },
  {
    text: "          --stage prod \\",
    tone: "plain",
  },
  {
    text: "          --schema-file ./schema.graphqls",
    tone: "plain",
  },
  {
    text: "        env:",
    tone: "key",
  },
  {
    text: "          NITRO_API_KEY: ${{ secrets.NITRO_API_KEY }}",
    tone: "value",
  },
  {
    text: "          NITRO_API_ID: ${{ vars.NITRO_API_ID }}",
    tone: "value",
  },
];

function ToneClass(tone: CodeLine["tone"]): string {
  switch (tone) {
    case "comment":
      return "text-cc-ink-dim italic";
    case "key":
      return "text-cc-accent";
    case "string":
      return "text-cc-warning";
    case "value":
      return "text-cc-ink";
    default:
      return "text-cc-ink";
  }
}

function WorkflowSnippet() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-2xl border">
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2.5">
        <div className="flex items-center gap-2">
          <span
            aria-hidden="true"
            className="bg-cc-status-firing size-2.5 rounded-full"
          />
          <span
            aria-hidden="true"
            className="bg-cc-status-investigating size-2.5 rounded-full"
          />
          <span
            aria-hidden="true"
            className="bg-cc-status-healthy size-2.5 rounded-full"
          />
          <span className="text-cc-ink-dim ml-3 font-mono text-[0.65rem]">
            schema-check.yml
          </span>
        </div>
        <span className="border-cc-accent/40 text-cc-accent rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          GitHub Actions
        </span>
      </div>
      <pre className="overflow-x-auto p-5 font-mono text-[0.72rem] leading-[1.7]">
        <code>
          {WORKFLOW_LINES.map((line, index) => (
            <span key={index} className="block">
              <span className={ToneClass(line.tone)}>
                {line.text.length === 0 ? " " : line.text}
              </span>
            </span>
          ))}
        </code>
      </pre>
      <div className="border-cc-card-border bg-cc-code-header flex flex-wrap items-center gap-x-4 gap-y-1.5 border-t px-4 py-3 font-mono text-[0.62rem]">
        <span className="text-cc-status-healthy inline-flex items-center gap-1.5">
          <CheckIcon size={12} /> checkout
        </span>
        <span className="text-cc-status-healthy inline-flex items-center gap-1.5">
          <CheckIcon size={12} /> install nitro cli
        </span>
        <span className="text-cc-danger inline-flex items-center gap-1.5">
          <span
            aria-hidden="true"
            className="border-cc-danger inline-block size-3 rounded-full border"
          />
          validate schema (1 breaking, stage prod)
        </span>
        <span className="text-cc-ink-dim ml-auto">PR #482 build failed</span>
      </div>
    </div>
  );
}

function ValidateBlock() {
  return (
    <section className="py-12">
      <div className="grid items-start gap-10 lg:grid-cols-5 lg:gap-12">
        <div className="lg:col-span-2">
          <Eyebrow>Nitro CLI, in your pipeline</Eyebrow>
          <SectionTitle>One step. Any pipeline.</SectionTitle>
          <p className="text-cc-ink mt-5 text-base/relaxed">
            The Nitro CLI runs anywhere a shell does: GitHub Actions, Azure
            DevOps, GitLab CI, or your own runner. Drop one step in the PR
            build. The CLI validates the proposed schema against the registered
            version for the target environment and exits non-zero on a verdict
            you decided to gate on.
          </p>
          <ul className="mt-6 space-y-3">
            {[
              "validate locally or in CI before any publish",
              "upload the candidate schema, tagged per branch or stage",
              "publish on green; rollback by republishing an older tag",
            ].map((line) => (
              <li key={line} className="flex items-start gap-3">
                <span className="text-cc-accent mt-0.5 shrink-0">
                  <CheckIcon />
                </span>
                <span className="text-cc-ink text-sm/relaxed">{line}</span>
              </li>
            ))}
          </ul>
          <p className="text-cc-ink-dim mt-6 text-xs">
            Honest scope: the GraphQL IDE is served by your Hot Chocolate
            endpoint, not by Nitro. Nitro hosts the registry, the validation
            checks, and the schema and client history that back this gate.
          </p>
        </div>

        <div className="lg:col-span-3">
          <WorkflowSnippet />
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Stage promotion strip: dev -> QA -> prod with approval gates.       */
/* ------------------------------------------------------------------ */

interface Stage {
  readonly name: string;
  readonly version: string;
  readonly status: "promoted" | "promoted-recent" | "awaiting" | "held";
  readonly note: string;
}

const STAGES: readonly Stage[] = [
  {
    name: "dev",
    version: "v14",
    status: "promoted",
    note: "auto-published on PR merge",
  },
  {
    name: "QA",
    version: "v14",
    status: "promoted-recent",
    note: "promoted 12m ago, suite green",
  },
  {
    name: "prod",
    version: "v13",
    status: "held",
    note: "approval gate, breaking diff on v14",
  },
];

function StageBadge({ status }: { readonly status: Stage["status"] }) {
  if (status === "held") {
    return (
      <span className="border-cc-warning/40 text-cc-warning rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
        hold
      </span>
    );
  }
  if (status === "awaiting") {
    return (
      <span className="border-cc-card-border text-cc-ink-dim rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
        waiting
      </span>
    );
  }
  return (
    <span className="border-cc-accent/40 text-cc-accent rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
      promoted
    </span>
  );
}

function StagePromotionStrip() {
  return (
    <section className="py-12">
      <div className="max-w-3xl">
        <Eyebrow>Per-environment publish</Eyebrow>
        <SectionTitle>
          Promote stage by stage. Hold the risky ones.
        </SectionTitle>
        <p className="text-cc-ink mt-5 text-base/relaxed">
          Each environment pins its own active schema and its own set of
          published clients. A change validates per stage and promotes on
          approval. Dangerous and breaking diffs sit on a gate until a human
          signs off.
        </p>
      </div>

      <ol className="mt-10 grid gap-4 lg:grid-cols-3">
        {STAGES.map((stage, index) => (
          <li
            key={stage.name}
            className="border-cc-card-border bg-cc-card-bg/60 relative rounded-2xl border p-6 backdrop-blur-sm"
          >
            <div className="flex items-center justify-between">
              <span className="font-heading text-cc-heading text-lg font-semibold tracking-wide uppercase">
                {stage.name}
              </span>
              <StageBadge status={stage.status} />
            </div>
            <div className="border-cc-card-border bg-cc-surface mt-5 flex items-center justify-between rounded-lg border px-3 py-2">
              <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
                active
              </span>
              <span
                className={[
                  "font-mono text-xs font-medium",
                  stage.status === "held"
                    ? "text-cc-warning"
                    : "text-cc-accent",
                ].join(" ")}
              >
                {stage.version}
              </span>
            </div>
            <p className="text-cc-ink-dim mt-4 text-xs/relaxed">{stage.note}</p>
            {index < STAGES.length - 1 && (
              <span
                aria-hidden="true"
                className="text-cc-ink-faint absolute top-1/2 -right-3 hidden -translate-y-1/2 text-xl lg:block"
              >
                &rarr;
              </span>
            )}
          </li>
        ))}
      </ol>

      <div className="border-cc-card-border bg-cc-card-bg/40 mt-6 flex flex-wrap items-center gap-x-6 gap-y-2 rounded-2xl border px-5 py-4 font-mono text-[0.65rem] backdrop-blur-sm">
        <span className="text-cc-ink-dim">approval gate</span>
        <span className="text-cc-accent">--wait-for-approval</span>
        <span className="text-cc-ink-dim">rollback</span>
        <span className="text-cc-accent">
          nitro schema publish --tag v13 --stage prod
        </span>
        <span className="text-cc-ink-dim ml-auto">audit log retained</span>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Lifecycle + classification: validate -> upload -> publish with the  */
/* verdict pills the registry can return.                              */
/* ------------------------------------------------------------------ */

interface LifecycleStep {
  readonly label: string;
  readonly command: string;
  readonly body: string;
}

const LIFECYCLE: readonly LifecycleStep[] = [
  {
    label: "validate",
    command: "nitro schema validate",
    body: "Diff the candidate against the registered schema, classify every change, and check against persisted operations from published clients.",
  },
  {
    label: "upload",
    command: "nitro schema upload",
    body: "Stage the candidate against a tag (per branch, per environment). The schema lives in the registry; nothing is live yet.",
  },
  {
    label: "publish",
    command: "nitro schema publish",
    body: "On a green PR build and a cleared approval gate, the tag goes live for the target environment. The previous tag remains in history.",
  },
];

function LifecycleClassification() {
  const verdicts: readonly Verdict[] = ["safe", "dangerous", "breaking"];
  return (
    <section className="py-12">
      <div className="grid items-start gap-10 lg:grid-cols-2 lg:gap-12">
        <div>
          <Eyebrow>Validate. Upload. Publish.</Eyebrow>
          <SectionTitle>The lifecycle is the safety net.</SectionTitle>
          <p className="text-cc-ink mt-5 text-base/relaxed">
            The CLI splits the publish path into three commands so each gate is
            explicit. Validate is read-only and safe to run on every commit.
            Upload stages the candidate. Publish is the final, audited step.
          </p>
          <ol className="mt-8 space-y-4">
            {LIFECYCLE.map((step, index) => (
              <li
                key={step.label}
                className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm"
              >
                <div className="flex items-center gap-3">
                  <span className="border-cc-accent/40 text-cc-accent flex size-6 items-center justify-center rounded-full border font-mono text-[0.6rem]">
                    {index + 1}
                  </span>
                  <span className="font-heading text-cc-heading text-sm font-semibold tracking-wide uppercase">
                    {step.label}
                  </span>
                  <span className="text-cc-ink-dim font-mono text-[0.65rem]">
                    {step.command}
                  </span>
                </div>
                <p className="text-cc-ink-dim mt-3 text-sm/relaxed">
                  {step.body}
                </p>
              </li>
            ))}
          </ol>
        </div>

        <div>
          <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm">
            <Eyebrow>Verdict the registry returns</Eyebrow>
            <h3 className="font-heading text-cc-heading mt-3 text-xl font-semibold">
              Every diff lands in one of three buckets.
            </h3>
            <div className="mt-5 space-y-3">
              {verdicts.map((verdict) => (
                <div
                  key={verdict}
                  className={[
                    "border-cc-card-border bg-cc-surface rounded-xl border p-4 ring-1",
                    VERDICT[verdict].ring,
                  ].join(" ")}
                >
                  <div className="flex items-center gap-2">
                    <VerdictDot verdict={verdict} />
                    <p
                      className={[
                        "font-mono text-[0.7rem] font-semibold tracking-[0.1em] uppercase",
                        VERDICT[verdict].ink,
                      ].join(" ")}
                    >
                      {VERDICT[verdict].label}
                    </p>
                  </div>
                  <p className="text-cc-ink-dim mt-3 text-xs/relaxed">
                    {verdict === "safe" &&
                      "Additive and backwards compatible. Passes the gate without a human."}
                    {verdict === "dangerous" &&
                      "Could surprise a client even when the SDL still parses. Held for review by default."}
                    {verdict === "breaking" &&
                      "Removes or tightens part of the contract. Fails the build when published clients depend on it."}
                  </p>
                </div>
              ))}
            </div>
            <p className="text-cc-ink-dim mt-5 text-xs">
              Scope is reported as published clients affected, read from the
              client registry of persisted operations.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Nitro animation embed: a single product screen in a bordered card.  */
/* ------------------------------------------------------------------ */

function SchemaEmbed() {
  return (
    <section className="py-12">
      <div className="max-w-3xl">
        <Eyebrow>See the registry react</Eyebrow>
        <SectionTitle>
          Field usage is the evidence behind the verdict.
        </SectionTitle>
        <p className="text-cc-ink mt-5 text-base/relaxed">
          The schema view in Nitro shows deprecated and at-risk fields with the
          operations that still call them. The same signal feeds the validate
          gate, so a verdict you see in CI is the one a reviewer can drill into.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg/40 mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
        <NitroSchema />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Closing CTA.                                                       */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-4xl rounded-3xl border p-8 text-center backdrop-blur-sm sm:p-12">
        <Eyebrow>Wire it in</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mx-auto mt-4 max-w-2xl leading-tight font-semibold text-balance">
          Stop breaking changes in CI, not in production.
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base/relaxed">
          One CLI step in your existing pipeline turns the registry into a
          safety net. Validate every PR, publish on green, and keep an audited
          path back to any tagged version.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-4">
          <SolidButton href="/docs/nitro">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>
        <p className="text-cc-ink-dim mt-8 text-sm">
          Pairs with{" "}
          <Link
            href="/platform/release-safety"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            release safety
          </Link>
          ,{" "}
          <Link
            href="/platform/analytics"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            analytics
          </Link>
          , and the wider{" "}
          <Link
            href="/platform"
            className="text-cc-accent hover:text-cc-accent-hover transition-colors"
          >
            platform
          </Link>
          .
        </p>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export default function ContinuousIntegrationPreviewV2Page() {
  return (
    <>
      <Hero />
      <PromiseBand />
      <ValidateBlock />
      <StagePromotionStrip />
      <LifecycleClassification />
      <SchemaEmbed />
      <ClosingCta />
    </>
  );
}
