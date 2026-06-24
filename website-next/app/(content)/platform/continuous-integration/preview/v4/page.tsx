import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroSchema } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Continuous Integration for GraphQL | ChilliCream",
  description:
    "A long-form dispatch on GraphQL schema registry CI: validate, upload, publish, and deploy every change through one Nitro CLI pipeline on any CI runner.",
  keywords: [
    "GraphQL schema registry CI",
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
      "A dispatch on safe GraphQL schema evolution. Validate, upload, publish, and deploy through the Nitro CLI on any CI runner.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Editorial primitives                                                */
/* ------------------------------------------------------------------ */

interface GutterMarginaliaProps {
  readonly children: ReactNode;
}

/* The "stage rule": a 1px cc-accent vertical hairline hanging from the   */
/* section divider, with a single line of mono marginalia next to it.    */
/* On lg+ this floats into the left editorial gutter. On smaller         */
/* viewports it sits inline above the headline so the spine stays        */
/* legible across breakpoints.                                           */
function GutterMarginalia({ children }: GutterMarginaliaProps) {
  return (
    <div className="relative mb-6 lg:absolute lg:top-0 lg:left-[-220px] lg:mb-0 lg:w-[180px]">
      <span
        aria-hidden
        className="bg-cc-accent absolute top-0 left-0 hidden h-16 w-px lg:block"
      />
      <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase lg:block lg:pl-3">
        {children}
      </span>
    </div>
  );
}

interface ChapterProps {
  readonly marginalia: ReactNode;
  readonly heading: string;
  readonly children: ReactNode;
}

function Chapter({ marginalia, heading, children }: ChapterProps) {
  return (
    <section className="relative">
      <div className="border-cc-card-border border-t pt-14" />
      <div className="relative">
        <GutterMarginalia>{marginalia}</GutterMarginalia>
        <h2 className="font-heading text-h2 text-cc-heading font-semibold tracking-tight">
          {heading}
        </h2>
        <div className="mt-8 flex flex-col gap-8">{children}</div>
      </div>
    </section>
  );
}

interface ProseProps {
  readonly children: ReactNode;
}

function Prose({ children }: ProseProps) {
  return <p className="text-body text-cc-prose leading-[1.75]">{children}</p>;
}

interface DefRowProps {
  readonly term: ReactNode;
  readonly children: ReactNode;
}

function DefRow({ term, children }: DefRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-[7.5rem_minmax(0,1fr)] gap-6 border-t py-4 first:border-t-0 sm:grid-cols-[9rem_minmax(0,1fr)]">
      <dt className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.18em] uppercase">
        {term}
      </dt>
      <dd className="text-body text-cc-prose leading-relaxed">{children}</dd>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Masthead                                                            */
/* ------------------------------------------------------------------ */

function Masthead() {
  return (
    <section>
      <div className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.22em] uppercase">
        Platform / Continuous Integration / Dispatch 04
      </div>
      <h1 className="font-heading text-hero text-cc-heading mt-8 font-semibold tracking-tight">
        A pipeline
        <br />
        for safe schema
        <br />
        evolution.
      </h1>
      <p className="text-lead text-cc-prose mt-10 leading-[1.25]">
        Validate, upload, publish, and deploy every GraphQL change through one
        Nitro CLI pipeline. Stage by stage, classification before promotion,
        gates before deploy.
      </p>
      <div className="mt-10 flex flex-wrap items-center gap-3">
        <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
      <div className="border-cc-card-border mt-16 border-t" />
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Opening essay with drop cap                                         */
/* ------------------------------------------------------------------ */

function OpeningEssay() {
  return (
    <section>
      <div className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
        Dispatch 04 / Run #482 / orders-api
      </div>
      <div className="mt-6">
        <p className="text-lead text-cc-prose leading-[1.35]">
          <span
            aria-hidden
            className="font-heading text-cc-accent float-left mr-3 text-[5rem] leading-[0.85] font-bold"
          >
            V
          </span>
          alidate, upload, publish, deploy. Four words that name the spine of a
          GraphQL schema registry CI pipeline, and the difference between a
          release that lands quietly and one that wakes up the on-call. The
          Nitro CLI walks the same four steps on every runner, classifies each
          field-level change against the operations published clients have
          registered, and refuses to promote what it cannot defend.
        </p>
      </div>
      <p className="text-body text-cc-prose mt-6 leading-[1.75]">
        What follows is a tour of the four stages, in the order the CLI runs
        them, with the artifacts each stage emits. The registry holds the
        schema. The pipeline holds the discipline. Your runner holds the
        rollout.
      </p>
      <dl className="mt-10">
        <DefRow term="Validate">
          Classify every change as safe, dangerous, or breaking against the
          client registry. Fail the build before anything is staged.
        </DefRow>
        <DefRow term="Upload">
          Stage the new schema in the registry, tagged with the commit SHA and
          pinned to an environment. Nothing is live yet.
        </DefRow>
        <DefRow term="Publish">
          Promote the staged schema through an approval gate so the new contract
          becomes the active one for the target environment.
        </DefRow>
        <DefRow term="Deploy">
          Hand control back to your pipeline. The same runner that built the
          code rolls out the API behind the new schema.
        </DefRow>
      </dl>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter 01 - Validate                                               */
/* ------------------------------------------------------------------ */

interface ClassificationRow {
  readonly label: "SAFE" | "DANGEROUS" | "BREAKING";
  readonly tone: "success" | "warning" | "danger";
  readonly path: string;
  readonly note: string;
}

const CLASSIFICATION_ROWS: readonly ClassificationRow[] = [
  {
    label: "SAFE",
    tone: "success",
    path: "Order.totalAmount: Money!",
    note: "field added",
  },
  {
    label: "DANGEROUS",
    tone: "warning",
    path: "Order.placedAt @deprecated",
    note: "deprecation reason recorded",
  },
  {
    label: "BREAKING",
    tone: "danger",
    path: "Order.total: Float!",
    note: "field removed, 3 published clients affected",
  },
];

function toneClass(tone: ClassificationRow["tone"]) {
  if (tone === "success") return "text-cc-success";
  if (tone === "warning") return "text-cc-warning";
  return "text-cc-danger";
}

function ClassificationBlock() {
  return (
    <div className="border-cc-card-border border-t border-b py-1">
      {CLASSIFICATION_ROWS.map((row) => (
        <div
          key={row.path}
          className="border-cc-card-border grid grid-cols-[6.5rem_minmax(0,1fr)] items-baseline gap-4 border-b py-4 last:border-b-0 sm:grid-cols-[8rem_minmax(0,1fr)_auto]"
        >
          <span
            className={`font-mono text-[0.72rem] font-semibold tracking-[0.18em] uppercase ${toneClass(row.tone)}`}
          >
            {row.label}
          </span>
          <div className="min-w-0">
            <div className="text-cc-heading font-mono text-[0.82rem]">
              {row.path}
            </div>
            <div className="text-cc-ink-dim mt-1 font-mono text-[0.72rem] leading-relaxed">
              {row.note}
            </div>
          </div>
          <span className="text-cc-nav-label hidden font-mono text-[0.66rem] tracking-[0.18em] uppercase sm:inline">
            registry
          </span>
        </div>
      ))}
    </div>
  );
}

function ValidateChapter() {
  return (
    <Chapter
      marginalia="01 / Validate / run #482"
      heading="Classify before you commit."
    >
      <Prose>
        Every push runs the CLI against the registry. Each schema change is
        stamped with one of three verdicts, and the build either earns a green
        light or stops where it stands. The classifier reads the operations your
        clients have published, so a field rename is not a rumor, it is a
        measurable break against named consumers.
      </Prose>
      <Prose>
        The exit code drives the rest of the pipeline. A breaking change fails
        the job before anything reaches the upload stage, which means the
        registry never holds a contract that was not vetted. The verdict below
        is the one the CLI returned for run #482.
      </Prose>
      <ClassificationBlock />
    </Chapter>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter 02 - Upload                                                 */
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
    <div className="flex items-start gap-3 font-mono text-[0.78rem] leading-relaxed">
      <span className="text-cc-nav-label/70 w-3 shrink-0 select-none">
        {prompt ? "$" : " "}
      </span>
      <span className={toneCls}>{children}</span>
    </div>
  );
}

function UploadTranscript() {
  return (
    <div className="border-cc-card-border border-t border-b py-5">
      <TerminalLine prompt>
        <span className="text-cc-accent">dotnet nitro</span> schema publish \
      </TerminalLine>
      <TerminalLine>{"  "}--stage staging \</TerminalLine>
      <TerminalLine>
        {"  "}--tag{" "}
        <span className="text-cc-accent">
          {"$"}
          {"{"}GITHUB_SHA{"}"}
        </span>
      </TerminalLine>
      <div className="my-3 h-px" />
      <TerminalLine tone="ok">
        [ok] schema validated against client registry
      </TerminalLine>
      <TerminalLine tone="ok">[ok] uploaded as schema v14</TerminalLine>
      <TerminalLine tone="warn">
        [warn] dangerous change: Order.placedAt @deprecated
      </TerminalLine>
      <TerminalLine>tag: a1f2e9c, env: staging, run: #482</TerminalLine>
    </div>
  );
}

function UploadChapter() {
  return (
    <Chapter
      marginalia="02 / Upload / run #482"
      heading="Stage the new contract."
    >
      <Prose>
        Once validation clears, the same CLI command stages the schema in the
        registry. The new version is tagged with the commit SHA and pinned to a
        target environment, which makes the upload reproducible from any later
        release. Dangerous changes ride along with the reason text the author
        left behind, not as a silent line of telemetry.
      </Prose>
      <Prose>
        Nothing is live yet. The active contract for the environment is the one
        from the previous publish. Upload is a staging act, the way a
        printer&apos;s plate is locked before the press runs.
      </Prose>
      <UploadTranscript />
    </Chapter>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter 03 - Publish                                                */
/* ------------------------------------------------------------------ */

function PublishChapter() {
  return (
    <Chapter
      marginalia="03 / Publish / approval gate"
      heading="Approval gate, then promote."
    >
      <Prose>
        Publish is the moment the staged schema becomes the contract clients
        will see. The approval gate sits between upload and publish, and can
        require a named reviewer when the verdict is dangerous or breaking.
        Promotion never runs before the gate clears, and every release lands in
        the registry timeline with its classification attached.
      </Prose>
      <Prose>
        Each environment keeps its own active version, so staging can race ahead
        while production stays on the last green release. The registry records
        every step, which means a rollback is a re-publish of a prior tag, not a
        hand-edit of a deployment.
      </Prose>
      <div className="border-cc-card-border border-t border-b">
        <NitroSchema />
      </div>
    </Chapter>
  );
}

/* ------------------------------------------------------------------ */
/* Chapter 04 - Deploy                                                 */
/* ------------------------------------------------------------------ */

interface EnvRow {
  readonly env: string;
  readonly version: string;
  readonly clients: string;
  readonly state: "passed" | "active" | "pending";
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

function envChip(state: EnvRow["state"]) {
  if (state === "passed") {
    return (
      <span className="text-cc-success font-mono text-[0.7rem] font-semibold tracking-[0.18em] uppercase">
        passed
      </span>
    );
  }
  if (state === "active") {
    return (
      <span className="text-cc-warning font-mono text-[0.7rem] font-semibold tracking-[0.18em] uppercase">
        running
      </span>
    );
  }
  return (
    <span className="text-cc-nav-label font-mono text-[0.7rem] font-semibold tracking-[0.18em] uppercase">
      pending
    </span>
  );
}

function EnvTable() {
  return (
    <div className="border-cc-card-border border-t border-b">
      <div className="text-cc-nav-label grid grid-cols-[1fr_4rem_4rem_5rem] gap-4 py-3 font-mono text-[0.62rem] tracking-[0.22em] uppercase sm:grid-cols-[1fr_5rem_5rem_6rem]">
        <span>env</span>
        <span>version</span>
        <span>clients</span>
        <span className="text-right">state</span>
      </div>
      {ENV_ROWS.map((row) => (
        <div
          key={row.env}
          className="border-cc-card-border grid grid-cols-[1fr_4rem_4rem_5rem] items-baseline gap-4 border-t py-4 sm:grid-cols-[1fr_5rem_5rem_6rem]"
        >
          <div className="min-w-0">
            <div className="text-cc-heading font-mono text-[0.82rem]">
              {row.env}
            </div>
            <div className="text-cc-nav-label mt-1 font-mono text-[0.66rem] leading-relaxed">
              {row.note}
            </div>
          </div>
          <span className="text-cc-prose font-mono text-[0.78rem]">
            {row.version}
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.76rem]">
            {row.clients}
          </span>
          <div className="flex justify-end">{envChip(row.state)}</div>
        </div>
      ))}
    </div>
  );
}

function DeployChapter() {
  return (
    <Chapter
      marginalia="04 / Deploy / environments"
      heading="Deploy on your runner."
    >
      <Prose>
        Deployment is yours. Once publish clears, the CLI hands control back to
        the pipeline that built the code, and the runner that compiled the API
        also rolls it out. The registry tracks what is active per environment,
        the runner moves the bits, and the two stay in step through the commit
        SHA they both already know.
      </Prose>
      <Prose>
        Per-environment workflows let dev, QA, staging, and production each
        advance at their own cadence. A rollback is never a forensic dig, it is
        a republish of a prior tag, and the environment row below flips to
        reflect it on the next run.
      </Prose>
      <EnvTable />
    </Chapter>
  );
}

/* ------------------------------------------------------------------ */
/* Sidebar essay: One CLI, every runner                                */
/* ------------------------------------------------------------------ */

interface RunnerExcerptProps {
  readonly runner: string;
  readonly role: string;
  readonly snippet: string;
}

function RunnerExcerpt({ runner, role, snippet }: RunnerExcerptProps) {
  return (
    <figure className="border-cc-card-border border-t pt-5">
      <figcaption className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.22em] uppercase">
        {runner} / {role}
      </figcaption>
      <pre className="text-cc-prose mt-3 overflow-x-auto font-mono text-[0.78rem] leading-relaxed">
        <code>{snippet}</code>
      </pre>
    </figure>
  );
}

function RunnerEssay() {
  return (
    <section className="relative">
      <div className="border-cc-card-border border-t pt-14" />
      <h2 className="font-heading text-h2 text-cc-heading font-semibold tracking-tight">
        One CLI, every runner.
      </h2>
      <div className="mt-8 flex flex-col gap-8">
        <Prose>
          The Nitro CLI is the only thing the pipeline asks for. Drop it into
          GitHub Actions, an Azure DevOps task, or a plain shell, and the same
          commands return the same exit codes against the same registry. The
          runner is a detail. The pipeline is the discipline.
        </Prose>
        <div className="flex flex-col gap-6">
          <RunnerExcerpt
            runner="GitHub Actions"
            role="workflow step"
            snippet={`- name: Publish schema
  run: |
    dotnet nitro schema publish \\
      --stage staging \\
      --tag \${{ github.sha }}`}
          />
          <RunnerExcerpt
            runner="Azure DevOps"
            role="pipeline task"
            snippet={`- script: |
    dotnet nitro schema publish \\
      --stage staging \\
      --tag $(Build.SourceVersion)
  displayName: Publish schema`}
          />
          <RunnerExcerpt
            runner="Any shell"
            role="bash, make, buildkite"
            snippet={`dotnet nitro schema publish --stage dev
dotnet nitro schema publish --stage qa
dotnet nitro schema publish --stage prod`}
          />
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* The honest version                                                  */
/* ------------------------------------------------------------------ */

interface PromiseRow {
  readonly tone: "success" | "warning";
  readonly term: string;
  readonly body: ReactNode;
}

const PROMISES: readonly PromiseRow[] = [
  {
    tone: "success",
    term: "Published clients affected",
    body: "Breaking-change classification runs against the operations published clients have registered. The gate reports the clients by name, not a vague global verdict.",
  },
  {
    tone: "warning",
    term: "Unregistered traffic",
    body: "A client is only guarded once its operations are uploaded to the registry. Untracked consumers will not appear in the impact list.",
  },
  {
    tone: "success",
    term: "Validate, then upload, then publish",
    body: "The CLI follows a strict lifecycle: classify and check, then upload, then publish. Promotion never runs before validation clears.",
  },
  {
    tone: "warning",
    term: "The IDE serves from your endpoint",
    body: "The GraphQL IDE is served by your API endpoint, not by the registry. The registry tracks the schema, the endpoint hosts the tooling.",
  },
];

function promiseToneClass(tone: PromiseRow["tone"]) {
  return tone === "success" ? "text-cc-success" : "text-cc-warning";
}

function HonestyEssay() {
  return (
    <section className="relative">
      <div className="border-cc-card-border border-t pt-14" />
      <h2 className="font-heading text-h2 text-cc-heading font-semibold tracking-tight">
        What the pipeline can and cannot promise.
      </h2>
      <div className="mt-8 flex flex-col gap-6">
        <Prose>
          The Nitro pipeline reports on what it has registered. It is honest
          about its edges, so a green check carries the weight it is meant to
          carry, no more and no less.
        </Prose>
        <Prose>
          The list below names the promises the pipeline keeps and the limits it
          will not pretend away. Each pair sits as a term and its definition,
          the way a glossary at the back of a long article declares its own
          scope.
        </Prose>
        <dl className="mt-2">
          {PROMISES.map((row) => (
            <div
              key={row.term}
              className="border-cc-card-border grid grid-cols-1 gap-3 border-t py-5 first:border-t-0 sm:grid-cols-[14rem_minmax(0,1fr)] sm:gap-8"
            >
              <dt
                className={`font-mono text-[0.72rem] font-semibold tracking-[0.18em] uppercase ${promiseToneClass(row.tone)}`}
              >
                {row.term}
              </dt>
              <dd className="text-body text-cc-prose leading-relaxed">
                {row.body}
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Colophon CTA                                                        */
/* ------------------------------------------------------------------ */

function Colophon() {
  return (
    <section className="relative">
      <div className="border-cc-card-border border-t pt-16" />
      <div className="text-center">
        <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
          Make every release a pipeline run.
        </h2>
        <p className="text-lead text-cc-prose mx-auto mt-6 max-w-[36rem] leading-[1.3]">
          Validate, upload, publish, and deploy on the runner you already use.
          Schema and client registry in one place, classification before
          promotion.
        </p>
        <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/docs/nitro">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
        <div className="text-cc-nav-label mt-14 font-mono text-[0.68rem] tracking-[0.22em] uppercase">
          End of dispatch / Nitro CLI / ChilliCream
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export default function ContinuousIntegrationPreviewV4() {
  return (
    <div className="py-20 sm:py-28">
      <div className="mx-auto w-full max-w-[920px] px-6 lg:pr-0 lg:pl-[240px]">
        <div className="max-w-[640px]">
          <div className="flex flex-col gap-20 sm:gap-24">
            <Masthead />
            <OpeningEssay />
            <ValidateChapter />
            <UploadChapter />
            <PublishChapter />
            <DeployChapter />
            <RunnerEssay />
            <HonestyEssay />
            <Colophon />
          </div>
        </div>
      </div>
    </div>
  );
}
