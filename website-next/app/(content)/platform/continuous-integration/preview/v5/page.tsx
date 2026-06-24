import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroSchema } from "@/src/nitro";

export const metadata: Metadata = {
  title: "GraphQL Schema Registry CI | ChilliCream",
  description:
    "GraphQL schema registry CI field manual. Validate, upload, publish, deploy through the Nitro CLI with breaking-change classification on every runner.",
  keywords: [
    "GraphQL schema registry CI",
    "Nitro CLI",
    "schema registry",
    "client registry",
    "breaking change detection",
    "validate publish gate",
    "environment workflows",
    "GitHub Actions GraphQL",
    "Azure DevOps GraphQL",
    "published clients affected",
  ],
  openGraph: {
    title: "GraphQL Schema Registry CI",
    description:
      "A field manual for safe schema evolution. Validate, upload, publish, deploy through the Nitro CLI on any runner.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Accent and surface tokens                                           */
/* Cyan is the single accent. The rail and ordinal numerals carry it.  */
/* ------------------------------------------------------------------ */

const CYAN = "#16b9e4";
const SLAB_BG_DEEP = "#0a1426";

/* ------------------------------------------------------------------ */
/* Shared primitives                                                   */
/* ------------------------------------------------------------------ */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      {children}
    </span>
  );
}

interface TickLabelProps {
  readonly children: ReactNode;
}

function TickLabel({ children }: TickLabelProps) {
  return (
    <span
      className="font-mono text-[0.7rem] tracking-[0.22em] uppercase"
      style={{ color: CYAN }}
    >
      {children}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Hero                                                                */
/* ------------------------------------------------------------------ */

interface LegendItem {
  readonly num: string;
  readonly label: string;
}

const LEGEND: readonly LegendItem[] = [
  { num: "01", label: "VALIDATE" },
  { num: "02", label: "UPLOAD" },
  { num: "03", label: "PUBLISH" },
  { num: "04", label: "DEPLOY" },
  { num: "05", label: "RUNNERS" },
  { num: "06", label: "LIMITS" },
];

function HeroLegend() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex flex-wrap items-center gap-x-6 gap-y-3 rounded-xl border px-5 py-4">
      {LEGEND.map((item, i) => (
        <div key={item.num} className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <span
              className="font-heading text-[0.95rem] font-semibold tabular-nums"
              style={{ color: CYAN }}
            >
              {item.num}
            </span>
            <span className="text-cc-ink-dim font-mono text-[0.66rem] tracking-[0.18em] uppercase">
              {item.label}
            </span>
          </div>
          {i < LEGEND.length - 1 && (
            <span
              className="hidden h-px w-6 sm:inline-block"
              style={{ backgroundColor: "rgba(22, 185, 228, 0.35)" }}
              aria-hidden
            />
          )}
        </div>
      ))}
    </div>
  );
}

function HeroSection() {
  return (
    <section className="mx-auto w-full max-w-[760px] pt-2">
      <Eyebrow>Platform / Continuous Integration</Eyebrow>
      <h1 className="font-heading text-hero text-cc-heading mt-6 leading-[1.02] font-bold tracking-tight">
        A field manual for safe schema evolution.
      </h1>
      <p className="lead text-cc-ink-dim mt-7">
        One Nitro CLI walks every change through validate, upload, publish, and
        deploy. Six steps, one rail, every runner. Classification happens before
        promotion, the registry tracks published clients affected.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-3">
        <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
      <div className="mt-12">
        <HeroLegend />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Step shell: rail + colossal numeral + content                       */
/* ------------------------------------------------------------------ */

interface StepProps {
  readonly num: string;
  readonly tick: string;
  readonly title: string;
  readonly lead: string;
  readonly children?: ReactNode;
}

function Step({ num, tick, title, lead, children }: StepProps) {
  return (
    <section className="relative mx-auto w-full max-w-[760px]">
      {/* Pin on the rail */}
      <span
        aria-hidden
        className="absolute top-3 left-[-4px] hidden h-2 w-2 sm:block"
        style={{ backgroundColor: CYAN, marginLeft: "-1px" }}
      />
      <div className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-8 sm:gap-x-10">
        <div className="flex flex-col items-start">
          <span
            className="font-heading text-h1 sm:text-hero leading-none font-semibold tabular-nums"
            style={{ color: CYAN }}
          >
            {num}
          </span>
          <span className="mt-3 hidden sm:block">
            <TickLabel>{tick}</TickLabel>
          </span>
        </div>
        <div>
          <span className="sm:hidden">
            <TickLabel>{tick}</TickLabel>
          </span>
          <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight sm:mt-0">
            {title}
          </h2>
          <p className="text-lead text-cc-prose mt-5">{lead}</p>
          {children !== undefined && <div className="mt-8">{children}</div>}
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Step 01: VALIDATE                                                   */
/* ------------------------------------------------------------------ */

interface ChangeRow {
  readonly path: string;
  readonly note: string;
  readonly kind: "safe" | "dangerous" | "breaking";
}

const VALIDATE_ROWS: readonly ChangeRow[] = [
  {
    path: "Order.totalAmount: Money!",
    note: "field added",
    kind: "safe",
  },
  {
    path: "Order.placedAt @deprecated",
    note: "deprecation reason recorded",
    kind: "dangerous",
  },
  {
    path: "Order.total: Float!",
    note: "field removed, 3 published clients affected",
    kind: "breaking",
  },
];

function kindTone(k: ChangeRow["kind"]) {
  if (k === "safe") {
    return {
      text: "text-cc-success",
      bg: "bg-cc-success/10",
      ring: "ring-cc-success/30",
      label: "SAFE",
    };
  }
  if (k === "dangerous") {
    return {
      text: "text-cc-warning",
      bg: "bg-cc-warning/10",
      ring: "ring-cc-warning/30",
      label: "DANGEROUS",
    };
  }
  return {
    text: "text-cc-danger",
    bg: "bg-cc-danger/10",
    ring: "ring-cc-danger/30",
    label: "BREAKING",
  };
}

function ValidateSlab() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">
          nitro schema diff
        </span>
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.14em] uppercase">
          3 changes
        </span>
      </div>
      {VALIDATE_ROWS.map((row) => {
        const t = kindTone(row.kind);
        return (
          <div
            key={row.path}
            className="border-cc-card-border grid grid-cols-[auto_minmax(0,1fr)] items-center gap-3 border-b px-4 py-3 last:border-b-0"
          >
            <span
              className={`rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${t.bg} ${t.text} ${t.ring}`}
            >
              {t.label}
            </span>
            <div className="min-w-0">
              <div className="text-cc-heading truncate font-mono text-[0.78rem]">
                {row.path}
              </div>
              <div className="text-cc-ink-dim font-mono text-[0.66rem]">
                {row.note}
              </div>
            </div>
          </div>
        );
      })}
      <div className="border-cc-card-border flex items-center justify-between border-t px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[0.66rem]">
          classification complete
        </span>
        <span className="text-cc-nav-label font-mono text-[0.66rem]">
          1 safe, 1 dangerous, 1 breaking
        </span>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Step 02: UPLOAD, terminal block                                     */
/* ------------------------------------------------------------------ */

interface TerminalLineProps {
  readonly prompt?: boolean;
  readonly tone?: "default" | "ok" | "warn";
  readonly children: ReactNode;
}

function TerminalLine({
  prompt = false,
  tone = "default",
  children,
}: TerminalLineProps) {
  const toneCls =
    tone === "ok"
      ? "text-cc-success"
      : tone === "warn"
        ? "text-cc-warning"
        : "text-cc-prose";
  return (
    <div className="flex items-start gap-2 font-mono text-[0.74rem] leading-relaxed">
      <span className="text-cc-nav-label/70 shrink-0 select-none">
        {prompt ? "$" : " "}
      </span>
      <span className={toneCls}>{children}</span>
    </div>
  );
}

function UploadSlab() {
  return (
    <div
      className="border-cc-card-border overflow-hidden rounded-xl border"
      style={{ backgroundColor: SLAB_BG_DEEP }}
    >
      <div
        className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5"
        style={{ backgroundColor: "#0d1b30" }}
      >
        <span className="flex gap-1.5" aria-hidden>
          <span className="bg-cc-danger/60 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-warning/60 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-success/60 h-2.5 w-2.5 rounded-full" />
        </span>
        <span className="text-cc-ink-dim ml-2 font-mono text-[0.72rem]">
          ci runner
        </span>
        <span
          className="bg-cc-hover text-cc-ink-dim ml-auto rounded-md px-2 py-1 font-mono text-[0.66rem]"
          style={{ color: CYAN }}
        >
          upload
        </span>
      </div>
      <div className="px-5 py-5">
        <TerminalLine prompt>
          <span style={{ color: CYAN }}>dotnet nitro</span> schema publish \
        </TerminalLine>
        <TerminalLine>
          {"  "}--stage <span className="text-cc-tip">staging</span> \
        </TerminalLine>
        <TerminalLine>
          {"  "}--tag{" "}
          <span className="text-cc-tip">
            {"$"}
            {"{"}GITHUB_SHA{"}"}
          </span>
        </TerminalLine>
        <div className="bg-cc-card-border my-3 h-px" />
        <TerminalLine tone="ok">
          [ok] schema validated against client registry
        </TerminalLine>
        <TerminalLine tone="ok">[ok] uploaded as schema v14</TerminalLine>
        <TerminalLine tone="warn">
          [warn] dangerous change: Order.placedAt @deprecated
        </TerminalLine>
        <TerminalLine>tag: a1f2e9c, env: staging, run: #482</TerminalLine>
      </div>
      <div
        className="border-cc-card-border flex items-center gap-2 border-t px-4 py-2.5"
        style={{ backgroundColor: "#0d1b30" }}
      >
        <span
          className="h-2 w-2 rounded-full"
          style={{ backgroundColor: CYAN }}
        />
        <span className="text-cc-ink-dim font-mono text-[0.66rem]">
          schema v14 staged, awaiting publish
        </span>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Step 03: PUBLISH, NitroSchema embed                                 */
/* ------------------------------------------------------------------ */

function PublishSlab() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border">
      <NitroSchema />
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Step 04: DEPLOY, environment table                                  */
/* ------------------------------------------------------------------ */

type ChipState = "passed" | "active" | "pending";

const CHIP_TONE: Record<
  ChipState,
  { label: string; text: string; ring: string; bg: string; dot: string }
> = {
  passed: {
    label: "PASSED",
    text: "text-cc-success",
    ring: "ring-cc-success/35",
    bg: "bg-cc-success/10",
    dot: "bg-cc-success",
  },
  active: {
    label: "RUNNING",
    text: "text-cc-warning",
    ring: "ring-cc-warning/35",
    bg: "bg-cc-warning/10",
    dot: "bg-cc-warning",
  },
  pending: {
    label: "PENDING",
    text: "text-cc-nav-label",
    ring: "ring-cc-card-border",
    bg: "bg-cc-hover",
    dot: "bg-cc-nav-label/60",
  },
};

interface EnvRow {
  readonly env: string;
  readonly version: string;
  readonly clients: string;
  readonly state: ChipState;
  readonly note: string;
}

const ENV_ROWS: readonly EnvRow[] = [
  {
    env: "dev",
    version: "v14",
    clients: "5/5",
    state: "passed",
    note: "auto-publish from main",
  },
  {
    env: "qa",
    version: "v14",
    clients: "5/5",
    state: "passed",
    note: "promoted after dev",
  },
  {
    env: "staging",
    version: "v14",
    clients: "4/5",
    state: "active",
    note: "waiting on approver",
  },
  {
    env: "production",
    version: "v13",
    clients: "12/12",
    state: "pending",
    note: "deploy after publish",
  },
];

interface StateChipProps {
  readonly state: ChipState;
}

function StateChip({ state }: StateChipProps) {
  const t = CHIP_TONE[state];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${t.bg} ${t.text} ${t.ring}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${t.dot}`} />
      {t.label}
    </span>
  );
}

function DeploySlab() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">
          orders-api, environments
        </span>
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.14em] uppercase">
          per-env workflow
        </span>
      </div>
      <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[1fr_0.7fr_0.8fr_0.9fr] gap-3 border-b px-4 py-2 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
        <span>env</span>
        <span>version</span>
        <span>clients</span>
        <span className="text-right">state</span>
      </div>
      {ENV_ROWS.map((row) => (
        <div
          key={row.env}
          className="border-cc-card-border grid grid-cols-[1fr_0.7fr_0.8fr_0.9fr] items-center gap-3 border-b px-4 py-3 last:border-b-0"
        >
          <div className="min-w-0">
            <div className="text-cc-heading font-mono text-[0.78rem]">
              {row.env}
            </div>
            <div className="text-cc-nav-label font-mono text-[0.62rem]">
              {row.note}
            </div>
          </div>
          <span className="text-cc-prose font-mono text-[0.74rem]">
            {row.version}
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.72rem]">
            {row.clients}
          </span>
          <div className="flex justify-end">
            <StateChip state={row.state} />
          </div>
        </div>
      ))}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Step 05: RUNNERS, vertically stacked snippets                       */
/* ------------------------------------------------------------------ */

interface RunnerSlabProps {
  readonly title: string;
  readonly sub: string;
  readonly snippet: string;
}

function RunnerSlab({ title, sub, snippet }: RunnerSlabProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
        <div className="flex items-center gap-2">
          <span
            className="h-1.5 w-1.5"
            style={{ backgroundColor: CYAN }}
            aria-hidden
          />
          <span className="text-cc-heading font-heading text-[0.92rem] font-semibold">
            {title}
          </span>
        </div>
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.14em] uppercase">
          {sub}
        </span>
      </div>
      <pre
        className="text-cc-prose overflow-x-auto px-5 py-4 font-mono text-[0.72rem] leading-relaxed"
        style={{ backgroundColor: SLAB_BG_DEEP }}
      >
        <code>{snippet}</code>
      </pre>
    </div>
  );
}

function RunnersStack() {
  return (
    <div className="flex flex-col gap-5">
      <RunnerSlab
        title="GitHub Actions"
        sub="workflow step"
        snippet={`- name: Publish schema
  run: |
    dotnet nitro schema publish \\
      --stage staging \\
      --tag \${{ github.sha }}`}
      />
      <RunnerSlab
        title="Azure DevOps"
        sub="pipeline task"
        snippet={`- script: |
    dotnet nitro schema publish \\
      --stage staging \\
      --tag $(Build.SourceVersion)
  displayName: Publish schema`}
      />
      <RunnerSlab
        title="Any shell runner"
        sub="bash, make, buildkite"
        snippet={`dotnet nitro schema publish --stage dev
dotnet nitro schema publish --stage qa
dotnet nitro schema publish --stage prod`}
      />
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Step 06: LIMITS, four stacked annotated rows                        */
/* ------------------------------------------------------------------ */

interface LimitRowProps {
  readonly tone: "success" | "warning";
  readonly head: string;
  readonly body: ReactNode;
}

function LimitRow({ tone, head, body }: LimitRowProps) {
  const bar = tone === "success" ? "bg-cc-success" : "bg-cc-warning";
  return (
    <li className="border-cc-card-border bg-cc-card-bg relative rounded-lg border px-5 py-4">
      <span
        className={`absolute top-4 bottom-4 left-0 w-[3px] rounded-full ${bar}`}
      />
      <div className="pl-3">
        <div className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
          {head}
        </div>
        <p className="text-body text-cc-ink-dim mt-2 leading-relaxed">{body}</p>
      </div>
    </li>
  );
}

function LimitsStack() {
  return (
    <ul className="flex flex-col gap-4">
      <LimitRow
        tone="success"
        head="Published clients affected"
        body="Breaking-change classification runs against the operations your published clients have registered. The gate reports the clients by name, not a vague global verdict."
      />
      <LimitRow
        tone="warning"
        head="Unregistered traffic is outside the net"
        body="A client is only guarded once its operations are uploaded to the registry. Untracked consumers will not appear in the impact list."
      />
      <LimitRow
        tone="success"
        head="Validate before upload, publish after"
        body="The CLI follows a strict lifecycle: classify and check, then upload, then publish. Promotion never runs before validation clears."
      />
      <LimitRow
        tone="warning"
        head="The IDE serves from your endpoint"
        body="The GraphQL IDE is served by your API endpoint, not by the Nitro service. The registry tracks the schema, the endpoint hosts the tooling."
      />
    </ul>
  );
}

/* ------------------------------------------------------------------ */
/* Closing CTA                                                         */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="relative mx-auto w-full max-w-[760px]">
      {/* Square cap on the rail */}
      <span
        aria-hidden
        className="absolute top-0 left-[-5px] hidden h-2.5 w-2.5 sm:block"
        style={{ backgroundColor: CYAN }}
      />
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border px-6 py-14 text-center sm:px-12">
        <div
          aria-hidden
          className="absolute inset-0 -z-10"
          style={{
            backgroundImage:
              "radial-gradient(70% 100% at 50% 0%, rgba(22, 185, 228, 0.16), transparent 65%)",
          }}
        />
        <h2 className="font-heading text-h2 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
          Make every release a pipeline run.
        </h2>
        <p className="text-body text-cc-prose mx-auto mt-5 max-w-xl leading-relaxed">
          Validate, upload, publish, and deploy on the runner you already use.
          Schema and client registry in one place, classification before
          promotion.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/docs/nitro">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export default function ContinuousIntegrationPreviewV5() {
  return (
    <div className="relative py-6">
      <div className="flex flex-col gap-[120px] sm:gap-[160px]">
        <HeroSection />
        {/* Rail anchored to the first step, runs through every step to the closing CTA cap. */}
        <div className="relative mx-auto w-full max-w-[760px]">
          <span
            aria-hidden
            className="bg-cc-card-border absolute inset-y-0 left-0 hidden w-px sm:block"
          />
          <div className="flex flex-col gap-[120px] sm:gap-[160px]">
            <Step
              num="01"
              tick="STEP / VALIDATE"
              title="Classify before you commit."
              lead="Run the Nitro CLI against your branch and every change is stamped safe, dangerous, or breaking, checked against the operations your published clients actually send. The build fails loudly, not silently."
            >
              <ValidateSlab />
            </Step>
            <Step
              num="02"
              tick="STEP / UPLOAD"
              title="Stage the new contract."
              lead="Once validation clears, the same CLI uploads the schema to the registry, tagged with your commit SHA and pinned to a target environment. Nothing is live yet, but the version is reproducible."
            >
              <UploadSlab />
            </Step>
            <Step
              num="03"
              tick="STEP / PUBLISH"
              title="Approval gate, then promote."
              lead="Publish promotes the staged schema for an environment. Approval gates can require a reviewer, classification, or both. Only changes that pass clear the gate and become the new contract."
            >
              <PublishSlab />
            </Step>
            <Step
              num="04"
              tick="STEP / DEPLOY"
              title="Deploy on your runner."
              lead="Deployment is yours. The Nitro CLI hands control back to your pipeline once publish clears, so the same runner that built your code rolls out the API behind the new schema."
            >
              <DeploySlab />
            </Step>
            <Step
              num="05"
              tick="STEP / RUNNERS"
              title="One CLI, every runner."
              lead="The Nitro CLI is the only thing the pipeline needs. Drop it into GitHub Actions, Azure DevOps, or any shell-capable runner. Same commands, same exit codes, same registry on the other side."
            >
              <RunnersStack />
            </Step>
            <Step
              num="06"
              tick="STEP / LIMITS"
              title="What the pipeline can and cannot promise."
              lead="The Nitro pipeline reports on what it has registered. It is honest about its edges, so you can trust the green check when you see it."
            >
              <LimitsStack />
            </Step>
            <ClosingCta />
          </div>
        </div>
      </div>
    </div>
  );
}
