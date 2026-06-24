import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroSchema } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Continuous Integration for GraphQL | ChilliCream",
  description:
    "Validate, upload, publish, and deploy GraphQL schema changes through one Nitro CLI pipeline. Pipe-ready for GitHub Actions, Azure DevOps, and any CI runner.",
  keywords: [
    "GraphQL continuous integration",
    "Nitro CLI",
    "schema registry",
    "client registry",
    "GitHub Actions GraphQL",
    "Azure DevOps GraphQL",
    "breaking change detection",
    "validate publish gate",
    "environment workflows",
    "schema evolution pipeline",
  ],
  openGraph: {
    title: "Continuous Integration for GraphQL",
    description:
      "A pipeline for safe schema evolution. Validate, upload, publish, and deploy through the Nitro CLI on any CI runner.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Scene tokens                                                        */
/* The pipeline lives on a track. Stage state drives color:            */
/*   cc-success = passed, cc-warning = active/awaiting,                */
/*   cc-danger = blocked, cc-nav-label = pending.                      */
/* ------------------------------------------------------------------ */

const TRACK_BG = "#0a1426";
const TRACK_RAISED = "rgba(13, 27, 48, 0.78)";
const TRACK_LINE = "rgba(124, 146, 198, 0.18)";

type StageState = "passed" | "active" | "blocked" | "pending";

const STAGE_TONE: Record<
  StageState,
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
  blocked: {
    label: "BLOCKED",
    text: "text-cc-danger",
    ring: "ring-cc-danger/35",
    bg: "bg-cc-danger/10",
    dot: "bg-cc-danger",
  },
  pending: {
    label: "PENDING",
    text: "text-cc-nav-label",
    ring: "ring-cc-card-border",
    bg: "bg-cc-hover",
    dot: "bg-cc-nav-label/60",
  },
};

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

interface StageChipProps {
  readonly state: StageState;
  readonly className?: string;
}

function StageChip({ state, className = "" }: StageChipProps) {
  const t = STAGE_TONE[state];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${t.bg} ${t.text} ${t.ring} ${className}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${t.dot}`} />
      {t.label}
    </span>
  );
}

interface PanelProps {
  readonly title: ReactNode;
  readonly tab?: string;
  readonly footer?: ReactNode;
  readonly children: ReactNode;
  readonly className?: string;
}

function Panel({ title, tab, footer, children, className = "" }: PanelProps) {
  return (
    <div
      className={`border-cc-card-border overflow-hidden rounded-xl border shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)] backdrop-blur-md ${className}`}
      style={{ backgroundColor: TRACK_BG }}
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
        <div className="text-cc-ink-dim ml-2 flex items-center gap-2 font-mono text-[0.72rem]">
          {title}
        </div>
        {tab !== undefined && (
          <span className="bg-cc-hover text-cc-ink-dim ml-auto rounded-md px-2 py-1 font-mono text-[0.66rem]">
            {tab}
          </span>
        )}
      </div>
      <div>{children}</div>
      {footer !== undefined && (
        <div
          className="border-cc-card-border border-t px-4 py-2.5"
          style={{ backgroundColor: "#0d1b30" }}
        >
          {footer}
        </div>
      )}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Hero pipeline diagram (4 stages on a track)                          */
/* ------------------------------------------------------------------ */

interface StageDef {
  readonly num: string;
  readonly key: string;
  readonly title: string;
  readonly sub: string;
  readonly meta: string;
  readonly state: StageState;
}

const PIPELINE: readonly StageDef[] = [
  {
    num: "01",
    key: "validate",
    title: "Validate",
    sub: "classify changes",
    meta: "schema + clients",
    state: "passed",
  },
  {
    num: "02",
    key: "upload",
    title: "Upload",
    sub: "stage schema",
    meta: "version + tag",
    state: "passed",
  },
  {
    num: "03",
    key: "publish",
    title: "Publish",
    sub: "approval gate",
    meta: "promote to env",
    state: "active",
  },
  {
    num: "04",
    key: "deploy",
    title: "Deploy",
    sub: "release the API",
    meta: "your pipeline",
    state: "pending",
  },
];

interface PipelineNodeProps {
  readonly stage: StageDef;
}

function PipelineNode({ stage }: PipelineNodeProps) {
  const tone = STAGE_TONE[stage.state];
  return (
    <div
      className={`border-cc-card-border relative flex-1 rounded-lg border px-4 py-4`}
      style={{ backgroundColor: TRACK_RAISED }}
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] uppercase">
          {stage.num}
        </span>
        <StageChip state={stage.state} />
      </div>
      <div
        className={`font-heading mt-3 text-[1.05rem] font-semibold ${tone.text}`}
      >
        {stage.title}
      </div>
      <div className="text-cc-prose mt-1 font-mono text-[0.72rem]">
        {stage.sub}
      </div>
      <div className="text-cc-ink-dim mt-2 font-mono text-[0.62rem]">
        {stage.meta}
      </div>
    </div>
  );
}

function PipelineConnector() {
  return (
    <div
      className="flex shrink-0 items-center justify-center px-1 sm:px-2"
      aria-hidden
    >
      <svg
        viewBox="0 0 40 24"
        width={40}
        height={24}
        className="text-cc-nav-label/55 rotate-90 sm:rotate-0"
      >
        <path
          d="M2 12 H32"
          stroke="currentColor"
          strokeWidth="1.5"
          fill="none"
          strokeDasharray="2 3"
        />
        <path
          d="M30 7 L38 12 L30 17"
          stroke="currentColor"
          strokeWidth="1.5"
          fill="none"
        />
      </svg>
    </div>
  );
}

function PipelineTrack() {
  return (
    <div
      className="border-cc-card-border relative overflow-hidden rounded-xl border p-6 sm:p-8"
      style={{
        backgroundColor: TRACK_BG,
        backgroundImage: `linear-gradient(${TRACK_LINE} 1px, transparent 1px), linear-gradient(90deg, ${TRACK_LINE} 1px, transparent 1px)`,
        backgroundSize: "26px 26px",
      }}
    >
      <div className="flex items-center justify-between">
        <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
          nitro pipeline · orders-api · release
        </div>
        <div className="text-cc-ink-dim hidden font-mono text-[0.62rem] sm:block">
          run #482 · 3m 12s
        </div>
      </div>
      <div className="mt-6 flex flex-col items-stretch gap-3 sm:flex-row sm:items-stretch">
        {PIPELINE.map((stage, i) => (
          <div key={stage.key} className="contents">
            <PipelineNode stage={stage} />
            {i < PIPELINE.length - 1 && <PipelineConnector />}
          </div>
        ))}
      </div>
      <div className="border-cc-card-border mt-6 flex flex-wrap items-center gap-x-6 gap-y-2 border-t pt-4">
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
          <span className="bg-cc-success h-2 w-2 rounded-full" />
          validate · upload cleared
        </span>
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
          <span className="bg-cc-warning h-2 w-2 rounded-full" />
          publish waiting on approver
        </span>
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
          <span className="bg-cc-nav-label/60 h-2 w-2 rounded-full" />
          deploy will run after publish
        </span>
      </div>
    </div>
  );
}

function HeroSection() {
  return (
    <section className="flex flex-col gap-12">
      <div className="grid items-end gap-10 lg:grid-cols-[minmax(0,1fr)_auto]">
        <div>
          <Eyebrow>Platform · Continuous Integration</Eyebrow>
          <h1 className="font-heading text-hero text-cc-heading mt-5 font-bold tracking-tight">
            A pipeline for
            <br />
            safe schema evolution.
          </h1>
          <p className="lead text-cc-ink-dim mt-6 max-w-2xl">
            Validate, upload, publish, and deploy every GraphQL change through
            one Nitro CLI pipeline. Stage-by-stage status, classification before
            promotion, gates before deploy.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </div>
      <PipelineTrack />
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Stage 01: Validate, classification visual                           */
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
    note: "field removed · 3 published clients affected",
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

function ValidateVisual() {
  return (
    <Panel
      title={
        <>
          <span className="text-cc-prose">dotnet nitro schema publish</span>
          <span className="text-cc-nav-label">·</span>
          <span>validate</span>
        </>
      }
      tab="3 changes"
      footer={
        <div className="flex items-center justify-between">
          <span className="text-cc-ink-dim font-mono text-[0.66rem]">
            classification complete
          </span>
          <span className="text-cc-nav-label font-mono text-[0.66rem]">
            1 safe · 1 dangerous · 1 breaking
          </span>
        </div>
      }
    >
      {VALIDATE_ROWS.map((row) => {
        const t = kindTone(row.kind);
        return (
          <div
            key={row.path}
            className="border-cc-card-border grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3 border-b px-4 py-3 last:border-b-0"
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
            <span className="text-cc-nav-label font-mono text-[0.62rem]">
              registry
            </span>
          </div>
        );
      })}
    </Panel>
  );
}

/* ------------------------------------------------------------------ */
/* Stage 02: Upload, terminal block showing CLI lifecycle              */
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

function UploadVisual() {
  return (
    <Panel
      title={
        <>
          <span>ci runner</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">github actions</span>
        </>
      }
      tab="upload"
      footer={
        <div className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
          <span className="bg-cc-success h-2 w-2 rounded-full" />
          schema v14 staged · awaiting publish
        </div>
      }
    >
      <div className="px-5 py-5">
        <TerminalLine prompt>
          <span className="text-cc-info">dotnet nitro</span> schema publish \
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
        <TerminalLine>tag: a1f2e9c · env: staging · run: #482</TerminalLine>
      </div>
    </Panel>
  );
}

/* ------------------------------------------------------------------ */
/* Stage 03: Publish, wraps NitroSchema in the bordered frame           */
/* ------------------------------------------------------------------ */

function PublishVisual() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mx-auto overflow-hidden rounded-xl border">
      <NitroSchema />
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Stage 04: Deploy, environment matrix                                */
/* ------------------------------------------------------------------ */

interface EnvRow {
  readonly env: string;
  readonly version: string;
  readonly clients: string;
  readonly state: StageState;
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

function DeployVisual() {
  return (
    <Panel
      title={
        <>
          <span>orders-api</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">environments</span>
        </>
      }
      tab="per-environment workflow"
    >
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
            <StageChip state={row.state} />
          </div>
        </div>
      ))}
    </Panel>
  );
}

/* ------------------------------------------------------------------ */
/* Stage section shell                                                 */
/* ------------------------------------------------------------------ */

interface StageSectionProps {
  readonly num: string;
  readonly stage: string;
  readonly title: string;
  readonly lead: string;
  readonly bullets: readonly string[];
  readonly artifact: ReactNode;
  readonly flip?: boolean;
}

function StageSection({
  num,
  stage,
  title,
  lead,
  bullets,
  artifact,
  flip = false,
}: StageSectionProps) {
  return (
    <section className="grid items-center gap-10 lg:grid-cols-2 lg:gap-14">
      <div className={flip ? "lg:order-2" : ""}>
        <div className="flex items-center gap-3">
          <span className="border-cc-card-border text-cc-accent flex h-9 w-9 items-center justify-center rounded-md border font-mono text-[0.78rem] font-semibold">
            {num}
          </span>
          <Eyebrow>Stage · {stage}</Eyebrow>
        </div>
        <h2 className="font-heading text-h4 text-cc-heading mt-5 font-semibold tracking-tight">
          {title}
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">{lead}</p>
        <ul className="mt-6 space-y-3">
          {bullets.map((b) => (
            <li key={b} className="flex items-start gap-3">
              <span className="bg-cc-success/12 text-cc-success ring-cc-success/25 mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full ring-1 ring-inset">
                <CheckIcon size={11} />
              </span>
              <span className="text-cc-ink-dim text-[0.92rem] leading-relaxed">
                {b}
              </span>
            </li>
          ))}
        </ul>
      </div>
      <div className={flip ? "lg:order-1" : ""}>{artifact}</div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Runner band: GitHub Actions, Azure DevOps, any pipeline             */
/* ------------------------------------------------------------------ */

interface RunnerCardProps {
  readonly title: string;
  readonly sub: string;
  readonly snippet: string;
  readonly logo: ReactNode;
}

function GithubGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width={20}
      height={20}
      className="text-cc-heading"
      aria-hidden
    >
      <path
        fill="currentColor"
        d="M12 .5C5.65.5.5 5.65.5 12c0 5.08 3.29 9.39 7.86 10.92.58.11.79-.25.79-.55 0-.27-.01-.99-.01-1.95-3.2.69-3.88-1.54-3.88-1.54-.52-1.34-1.28-1.69-1.28-1.69-1.05-.72.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.03 1.77 2.71 1.26 3.37.96.1-.75.4-1.26.73-1.55-2.55-.29-5.24-1.27-5.24-5.66 0-1.25.45-2.27 1.18-3.07-.12-.29-.51-1.46.11-3.04 0 0 .97-.31 3.18 1.17a11.04 11.04 0 0 1 5.79 0c2.21-1.48 3.18-1.17 3.18-1.17.62 1.58.23 2.75.11 3.04.74.8 1.18 1.82 1.18 3.07 0 4.4-2.69 5.36-5.25 5.65.41.36.78 1.06.78 2.13 0 1.54-.01 2.78-.01 3.16 0 .31.21.67.8.55A11.51 11.51 0 0 0 23.5 12C23.5 5.65 18.35.5 12 .5Z"
      />
    </svg>
  );
}

function AzureGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden>
      <path
        fill="#16b9e4"
        d="M9.5 3 3 18.5l4 .5L13.5 8 9.5 3Zm4.5 4 7 14H10l-2-3 4-1L14 7Z"
      />
    </svg>
  );
}

function PipeGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden>
      <path
        d="M4 12 H20 M9 7 L4 12 L9 17 M15 7 L20 12 L15 17"
        stroke="currentColor"
        strokeWidth="1.6"
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="text-cc-accent"
      />
    </svg>
  );
}

function RunnerCard({ title, sub, snippet, logo }: RunnerCardProps) {
  return (
    <div
      className="border-cc-card-border flex h-full flex-col gap-4 rounded-xl border p-5"
      style={{ backgroundColor: TRACK_RAISED }}
    >
      <div className="flex items-center gap-3">
        <span
          className="border-cc-card-border flex h-9 w-9 items-center justify-center rounded-md border"
          style={{ backgroundColor: "#0d1b30" }}
        >
          {logo}
        </span>
        <div>
          <div className="text-cc-heading font-heading text-[0.95rem] font-semibold">
            {title}
          </div>
          <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.14em] uppercase">
            {sub}
          </div>
        </div>
      </div>
      <pre
        className="text-cc-prose border-cc-card-border overflow-x-auto rounded-md border px-3 py-3 font-mono text-[0.7rem] leading-relaxed"
        style={{ backgroundColor: "#0b1525" }}
      >
        <code>{snippet}</code>
      </pre>
    </div>
  );
}

function RunnerBand() {
  return (
    <section>
      <div className="grid items-end gap-6 lg:grid-cols-[minmax(0,1fr)_auto]">
        <div>
          <Eyebrow>Pipe ready</Eyebrow>
          <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
            One CLI, every runner.
          </h2>
          <p className="text-body text-cc-prose mt-4 max-w-2xl leading-relaxed">
            The Nitro CLI is the only thing the pipeline needs. Drop it into
            GitHub Actions, Azure DevOps, or any shell-capable runner. Same
            commands, same exit codes, same registry on the other side.
          </p>
        </div>
        <div className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.14em] uppercase">
          examples below
        </div>
      </div>
      <div className="mt-8 grid gap-5 lg:grid-cols-3">
        <RunnerCard
          title="GitHub Actions"
          sub="workflow step"
          logo={<GithubGlyph />}
          snippet={`- name: Publish schema
  run: |
    dotnet nitro schema publish \\
      --stage staging \\
      --tag \${{ github.sha }}`}
        />
        <RunnerCard
          title="Azure DevOps"
          sub="pipeline task"
          logo={<AzureGlyph />}
          snippet={`- script: |
    dotnet nitro schema publish \\
      --stage staging \\
      --tag $(Build.SourceVersion)
  displayName: Publish schema`}
        />
        <RunnerCard
          title="Any shell runner"
          sub="bash, make, buildkite"
          logo={<PipeGlyph />}
          snippet={`dotnet nitro schema publish --stage dev
dotnet nitro schema publish --stage qa
dotnet nitro schema publish --stage prod`}
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Honesty band                                                        */
/* ------------------------------------------------------------------ */

interface HonestyItemProps {
  readonly tone: "success" | "warning";
  readonly head: string;
  readonly body: ReactNode;
}

function HonestyItem({ tone, head, body }: HonestyItemProps) {
  const bar = tone === "success" ? "bg-cc-success" : "bg-cc-warning";
  return (
    <li className="border-cc-card-border bg-cc-card-bg relative rounded-lg border px-4 py-4">
      <span
        className={`absolute top-4 bottom-4 left-0 w-[3px] rounded-full ${bar}`}
      />
      <div className="pl-3">
        <div className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.14em] uppercase">
          {head}
        </div>
        <p className="text-cc-ink-dim mt-2 text-[0.86rem] leading-relaxed">
          {body}
        </p>
      </div>
    </li>
  );
}

function HonestyBand() {
  return (
    <section
      className="border-cc-card-border rounded-2xl border px-6 py-9 sm:px-10 sm:py-11"
      style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
    >
      <div className="max-w-3xl">
        <Eyebrow>The honest version</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          What the pipeline can and cannot promise.
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">
          The Nitro pipeline reports on what it has registered. It is honest
          about its edges, so you can trust the green check when you see it.
        </p>
        <ul className="mt-7 grid gap-4 sm:grid-cols-2">
          <HonestyItem
            tone="success"
            head="Published clients affected"
            body="Breaking-change classification runs against the operations published clients have registered. The gate reports the clients by name, not a vague global verdict."
          />
          <HonestyItem
            tone="warning"
            head="Unregistered traffic is outside the net"
            body="A client is only guarded once its operations are uploaded to the registry. Untracked consumers will not appear in the impact list."
          />
          <HonestyItem
            tone="success"
            head="Validate before upload, publish after"
            body="The CLI follows a strict lifecycle: classify and check, then upload, then publish. Promotion never runs before validation clears."
          />
          <HonestyItem
            tone="warning"
            head="The IDE serves from your endpoint"
            body="The GraphQL IDE is served by your API endpoint, not by the Nitro service. The registry tracks the schema, the endpoint hosts the tooling."
          />
        </ul>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Closing CTA                                                         */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="border-cc-card-border relative overflow-hidden rounded-2xl border px-6 py-14 text-center sm:px-12">
      <div
        aria-hidden
        className="absolute inset-0 -z-10"
        style={{
          backgroundColor: TRACK_BG,
          backgroundImage:
            "radial-gradient(70% 100% at 50% 0%, rgba(94,234,212,0.14), transparent 65%)",
        }}
      />
      <h2 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
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
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export default function ContinuousIntegrationPreviewV1() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <StageSection
        num="01"
        stage="Validate"
        title="Classify before you commit."
        lead="Run the Nitro CLI against your branch and every change is stamped safe, dangerous, or breaking, checked against the operations your published clients actually send. The build fails loudly, not silently."
        bullets={[
          "Schema diff classified per field: safe, dangerous, or breaking.",
          "Validated against the operations published clients have registered.",
          "Exit code drives your CI: a breaking change fails the job before upload.",
        ]}
        artifact={<ValidateVisual />}
      />
      <StageSection
        num="02"
        stage="Upload"
        title="Stage the new contract."
        lead="Once validation clears, the same CLI uploads the schema to the registry, tagged with your commit SHA and pinned to a target environment. Nothing is live yet, but the version is reproducible."
        bullets={[
          "One command stages the schema against the chosen environment.",
          "Each upload is tagged with the commit SHA for traceability.",
          "Dangerous changes are recorded with reason, not silently dropped.",
        ]}
        artifact={<UploadVisual />}
        flip
      />
      <StageSection
        num="03"
        stage="Publish"
        title="Approval gate, then promote."
        lead="Publish promotes the staged schema for an environment. Approval gates can require a reviewer, classification, or both. Only changes that pass clear the gate and become the new contract."
        bullets={[
          "Approval gates can require a reviewer on dangerous or breaking changes.",
          "Each environment keeps its own active version and history.",
          "The registry stores the full timeline, with classification per release.",
        ]}
        artifact={<PublishVisual />}
      />
      <StageSection
        num="04"
        stage="Deploy"
        title="Deploy on your runner."
        lead="Deployment is yours. The Nitro CLI hands control back to your pipeline once publish clears, so the same runner that built your code rolls out the API behind the new schema."
        bullets={[
          "Per-environment workflows: dev, QA, staging, production.",
          "Deploy runs on the same pipeline that validated and published.",
          "Rollback is a re-publish of a prior tagged version.",
        ]}
        artifact={<DeployVisual />}
        flip
      />
      <RunnerBand />
      <HonestyBand />
      <ClosingCta />
    </div>
  );
}
