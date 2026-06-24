import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroSchema } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro CLI: Ship GraphQL Schemas From the Terminal",
  description:
    "Validate, upload, and publish GraphQL schemas with the Nitro CLI. Classify safe, dangerous, and breaking changes, and gate deploys on affected clients.",
  keywords: [
    "Nitro CLI",
    "GraphQL schema registry",
    "schema validation CI",
    "breaking change detection",
    "GitHub Actions GraphQL",
    "Azure DevOps GraphQL",
    "schema publish",
    "GraphQL CI/CD",
    "client registry",
    "per environment schema",
  ],
  openGraph: {
    title: "Nitro CLI: Ship GraphQL Schemas From the Terminal",
    description:
      "A terminal-first registry workflow. Validate, upload, and publish GraphQL schemas, classify breaking changes, and gate deploys on published clients affected.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Scene accent (one color event allowed)                                    */
/* -------------------------------------------------------------------------- */

const SCENE_FROM = "#16b9e4";
const SCENE_MID = "#7c92c6";
const SCENE_TO = "#f0786a";
const ACCENT = "#5eead4";

/* -------------------------------------------------------------------------- */
/*  Terminal palette and primitives                                           */
/* -------------------------------------------------------------------------- */

const TERM = {
  prompt: "#5eead4",
  flag: "#7c92c6",
  arg: "#a5d6ff",
  cmd: "#f5f0ea",
  dim: "#62748e",
  ok: "#34d399",
  warn: "#fbbf24",
  danger: "#f0786a",
  rule: "rgba(245,241,234,0.08)",
};

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface TerminalChromeProps {
  readonly tab: string;
  readonly cwd: string;
  readonly children: ReactNode;
}

function TerminalChrome({ tab, cwd, children }: TerminalChromeProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40 backdrop-blur-md">
      <div className="border-cc-card-border bg-cc-code-header flex items-center gap-2 border-b px-3.5 py-2.5">
        <span className="flex gap-1.5" aria-hidden>
          <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]/80" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]/80" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]/80" />
        </span>
        <span className="text-cc-ink-dim ml-1.5 font-mono text-[0.7rem] tracking-tight">
          {tab}
        </span>
        <span className="text-cc-nav-label ml-auto font-mono text-[0.65rem] tracking-tight">
          {cwd}
        </span>
      </div>
      <div className="font-mono text-[0.78rem] leading-[1.55rem]">
        {children}
      </div>
    </div>
  );
}

interface PromptLineProps {
  readonly host?: string;
  readonly children: ReactNode;
}

function PromptLine({ host = "ci", children }: PromptLineProps) {
  return (
    <div className="flex items-start gap-2 px-4 py-0.5">
      <span style={{ color: TERM.prompt }} className="select-none">
        {host} $
      </span>
      <span className="text-cc-heading">{children}</span>
    </div>
  );
}

interface OutLineProps {
  readonly tone?: "default" | "ok" | "warn" | "danger" | "dim" | "heading";
  readonly indent?: number;
  readonly children: ReactNode;
}

function OutLine({ tone = "default", indent = 0, children }: OutLineProps) {
  const color =
    tone === "ok"
      ? TERM.ok
      : tone === "warn"
        ? TERM.warn
        : tone === "danger"
          ? TERM.danger
          : tone === "dim"
            ? TERM.dim
            : tone === "heading"
              ? TERM.cmd
              : "rgba(245,241,234,0.78)";
  return (
    <div
      className="px-4"
      style={{
        color,
        paddingLeft: `${1 + indent * 1.2}rem`,
        fontWeight: tone === "heading" ? 600 : 400,
      }}
    >
      {children}
    </div>
  );
}

function RuleLine() {
  return (
    <div
      className="mx-4 my-1.5 border-t"
      style={{ borderColor: TERM.rule }}
      aria-hidden
    />
  );
}

/* -------------------------------------------------------------------------- */
/*  The centerpiece: a believable Nitro CLI session                           */
/* -------------------------------------------------------------------------- */

function CliCenterpiece() {
  return (
    <TerminalChrome
      tab="zsh  ·  nitro@latest  ·  3 stages"
      cwd="~/svc/checkout-api"
    >
      <div className="py-3">
        {/* --- stage 1: validate ----------------------------------------- */}
        <PromptLine>
          <span style={{ color: TERM.cmd }}>nitro </span>
          <span style={{ color: TERM.flag }}>schema validate</span>
          <span style={{ color: TERM.flag }}> --api</span>
          <span style={{ color: TERM.arg }}> checkout</span>
          <span style={{ color: TERM.flag }}> --schema</span>
          <span style={{ color: TERM.arg }}> ./schema.graphql</span>
          <span style={{ color: TERM.flag }}> --stage</span>
          <span style={{ color: TERM.arg }}> prod</span>
        </PromptLine>
        <OutLine tone="dim">
          comparing ./schema.graphql against registry · checkout @ prod
        </OutLine>
        <OutLine tone="heading">
          <span style={{ color: TERM.ok }}>safe</span>{" "}
          <span className="text-cc-ink-dim">·</span>{" "}
          <span style={{ color: TERM.ok }}>4</span>
          <span className="text-cc-ink-dim">
            {"   "}field additions, nullable args, new enum value
          </span>
        </OutLine>
        <OutLine indent={1} tone="ok">
          + Query.cartById(id: ID!): Cart
        </OutLine>
        <OutLine indent={1} tone="ok">
          + Cart.estimatedDelivery: DateTime
        </OutLine>
        <OutLine indent={1} tone="ok">
          + Mutation.applyPromo.code: String -&gt; ID
        </OutLine>
        <OutLine indent={1} tone="ok">
          + ShippingMethod.OVERNIGHT
        </OutLine>
        <OutLine tone="heading">
          <span style={{ color: TERM.warn }}>dangerous</span>{" "}
          <span className="text-cc-ink-dim">·</span>{" "}
          <span style={{ color: TERM.warn }}>1</span>
          <span className="text-cc-ink-dim">
            {"   "}semantic change, requires reviewer approval
          </span>
        </OutLine>
        <OutLine indent={1} tone="warn">
          ~ Cart.total now excludes tax (default arg flipped)
        </OutLine>
        <OutLine tone="heading">
          <span style={{ color: TERM.danger }}>breaking</span>{" "}
          <span className="text-cc-ink-dim">·</span>{" "}
          <span style={{ color: TERM.danger }}>0</span>
        </OutLine>
        <RuleLine />
        <OutLine tone="dim">
          published clients affected (last 7d, prod traffic):
        </OutLine>
        <OutLine indent={1}>
          <span style={{ color: TERM.cmd }}>web@2.14.0</span>
          <span style={{ color: TERM.dim }}>{"      "}142 ops</span>
          <span style={{ color: TERM.ok }}>{"   5/5 OK"}</span>
        </OutLine>
        <OutLine indent={1}>
          <span style={{ color: TERM.cmd }}>mobile@1.8.3</span>
          <span style={{ color: TERM.dim }}>{"    91 ops"}</span>
          <span style={{ color: TERM.warn }}>{"   3/5 at risk"}</span>
          <span style={{ color: TERM.dim }}>{"  (Cart.total)"}</span>
        </OutLine>
        <OutLine indent={1}>
          <span style={{ color: TERM.cmd }}>partner-bff@0.3.1</span>
          <span style={{ color: TERM.dim }}>{"  18 ops"}</span>
          <span style={{ color: TERM.ok }}>{"   2/2 OK"}</span>
        </OutLine>
        <OutLine tone="ok">exit 0 · validation passed with warnings</OutLine>

        <RuleLine />

        {/* --- stage 2: upload ------------------------------------------- */}
        <PromptLine>
          <span style={{ color: TERM.cmd }}>nitro </span>
          <span style={{ color: TERM.flag }}>schema upload</span>
          <span style={{ color: TERM.flag }}> --api</span>
          <span style={{ color: TERM.arg }}> checkout</span>
          <span style={{ color: TERM.flag }}> --tag</span>
          <span style={{ color: TERM.arg }}> $GIT_SHA</span>
          <span style={{ color: TERM.flag }}> --schema</span>
          <span style={{ color: TERM.arg }}> ./schema.graphql</span>
        </PromptLine>
        <OutLine tone="dim">
          tag <span style={{ color: TERM.cmd }}>9f3a1c2</span> · uploaded by
          ci-runner@github · 14.2 kb
        </OutLine>
        <OutLine tone="dim">
          stored · awaiting promotion · visible in registry as checkout@9f3a1c2
        </OutLine>
        <OutLine tone="ok">exit 0 · schema candidate accepted</OutLine>

        <RuleLine />

        {/* --- stage 3: publish ------------------------------------------ */}
        <PromptLine>
          <span style={{ color: TERM.cmd }}>nitro </span>
          <span style={{ color: TERM.flag }}>schema publish</span>
          <span style={{ color: TERM.flag }}> --api</span>
          <span style={{ color: TERM.arg }}> checkout</span>
          <span style={{ color: TERM.flag }}> --tag</span>
          <span style={{ color: TERM.arg }}> 9f3a1c2</span>
          <span style={{ color: TERM.flag }}> --stage</span>
          <span style={{ color: TERM.arg }}> prod</span>
          <span style={{ color: TERM.flag }}> --require-approval</span>
        </PromptLine>
        <OutLine tone="dim">
          waiting for approval gate · stage{" "}
          <span style={{ color: TERM.cmd }}>prod</span>
        </OutLine>
        <OutLine indent={1} tone="ok">
          approved · reviewer-1
        </OutLine>
        <OutLine tone="dim">
          promoting checkout@9f3a1c2 from staging to prod
        </OutLine>
        <OutLine tone="ok">
          live · prod · checkout@9f3a1c2 · rollout window 60s
        </OutLine>
        <OutLine tone="dim">
          previous: checkout@7a01b88 retained · rollback via nitro schema
          publish --tag 7a01b88
        </OutLine>
        <OutLine tone="ok">exit 0 · publish complete</OutLine>
        <div className="h-1" />
        <PromptLine>
          <span
            style={{ color: TERM.prompt }}
            className="bg-cc-accent/70 inline-block w-2 align-middle"
            aria-hidden
          >
            &nbsp;
          </span>
        </PromptLine>
      </div>
    </TerminalChrome>
  );
}

/* -------------------------------------------------------------------------- */
/*  CLI surface cards                                                          */
/* -------------------------------------------------------------------------- */

interface CliCardProps {
  readonly title: string;
  readonly purpose: string;
  readonly invocation: string;
  readonly bullets: readonly string[];
}

function CliCard({ title, purpose, invocation, bullets }: CliCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex flex-col overflow-hidden rounded-xl border backdrop-blur-sm">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-3">
        <span className="text-cc-heading font-mono text-[0.78rem]">
          {title}
        </span>
        <span
          className="rounded-full px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase"
          style={{
            color: ACCENT,
            backgroundColor: "rgba(94, 234, 212, 0.08)",
            border: "1px solid rgba(94, 234, 212, 0.22)",
          }}
        >
          cli
        </span>
      </div>
      <div className="flex grow flex-col gap-3 p-4">
        <p className="text-cc-prose text-[0.92rem] leading-snug">{purpose}</p>
        <pre className="bg-cc-code-bg/80 border-cc-card-border text-cc-heading rounded-md border px-3 py-2 font-mono text-[0.72rem] leading-5">
          <code>{invocation}</code>
        </pre>
        <ul className="mt-auto flex flex-col gap-1.5">
          {bullets.map((b) => (
            <li
              key={b}
              className="text-cc-ink-dim flex items-start gap-2 text-[0.82rem]"
            >
              <span
                className="mt-1.5 inline-block h-1 w-1 shrink-0 rounded-full"
                style={{ background: ACCENT }}
              />
              {b}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Pipeline workflow file mock                                                */
/* -------------------------------------------------------------------------- */

interface YamlLineProps {
  readonly n: number;
  readonly indent?: number;
  readonly children: ReactNode;
}

function YamlLine({ n, indent = 0, children }: YamlLineProps) {
  return (
    <div className="flex items-start">
      <span
        className="text-cc-nav-label w-8 shrink-0 pr-3 text-right font-mono text-[0.68rem] leading-6 select-none"
        aria-hidden
      >
        {n}
      </span>
      <code
        className="font-mono text-[0.74rem] leading-6"
        style={{
          paddingLeft: `${indent * 1.2}ch`,
          color: "rgba(245,241,234,0.86)",
        }}
      >
        {children}
      </code>
    </div>
  );
}

interface KeyProps {
  readonly children: ReactNode;
}

function Key({ children }: KeyProps) {
  return <span style={{ color: "#7ee787" }}>{children}</span>;
}

interface StrProps {
  readonly children: ReactNode;
}

function Str({ children }: StrProps) {
  return <span style={{ color: "#a5d6ff" }}>{children}</span>;
}

interface CmtProps {
  readonly children: ReactNode;
}

function Cmt({ children }: CmtProps) {
  return <span style={{ color: "#8b949e" }}>{children}</span>;
}

function PipelineMock() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border backdrop-blur-md">
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-3.5 py-2.5">
        <div className="flex items-center gap-2">
          <span className="flex gap-1.5" aria-hidden>
            <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]/80" />
            <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]/80" />
            <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]/80" />
          </span>
          <span className="text-cc-ink-dim ml-1.5 font-mono text-[0.7rem]">
            .github/workflows/schema.yml
          </span>
        </div>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
          GitHub Actions
        </span>
      </div>
      <div className="py-3">
        <YamlLine n={1}>
          <Key>name</Key>: <Str>checkout-api · schema</Str>
        </YamlLine>
        <YamlLine n={2}>
          <Key>on</Key>:
        </YamlLine>
        <YamlLine n={3} indent={1}>
          <Key>pull_request</Key>:
        </YamlLine>
        <YamlLine n={4} indent={2}>
          <Key>paths</Key>: [<Str>{`"schema.graphql"`}</Str>]
        </YamlLine>
        <YamlLine n={5} indent={1}>
          <Key>push</Key>:
        </YamlLine>
        <YamlLine n={6} indent={2}>
          <Key>branches</Key>: [<Str>{`"main"`}</Str>]
        </YamlLine>
        <YamlLine n={7}>{""}</YamlLine>
        <YamlLine n={8}>
          <Key>jobs</Key>:
        </YamlLine>
        <YamlLine n={9} indent={1}>
          <Key>schema</Key>:
        </YamlLine>
        <YamlLine n={10} indent={2}>
          <Key>runs-on</Key>: <Str>ubuntu-latest</Str>
        </YamlLine>
        <YamlLine n={11} indent={2}>
          <Key>steps</Key>:
        </YamlLine>
        <YamlLine n={12} indent={3}>
          - <Key>uses</Key>: <Str>actions/checkout@v4</Str>
        </YamlLine>
        <YamlLine n={13} indent={3}>
          - <Key>name</Key>: <Str>install nitro</Str>
        </YamlLine>
        <YamlLine n={14} indent={4}>
          <Key>run</Key>:{" "}
          <Str>dotnet tool install -g ChilliCream.Nitro.CommandLine</Str>
        </YamlLine>
        <YamlLine n={15} indent={3}>
          - <Key>name</Key>: <Str>validate</Str>
        </YamlLine>
        <YamlLine n={16} indent={4}>
          <Key>run</Key>:{" "}
          <Str>nitro schema validate --api checkout --stage prod</Str>
        </YamlLine>
        <YamlLine n={17} indent={3}>
          - <Key>name</Key>: <Str>upload</Str>
        </YamlLine>
        <YamlLine n={18} indent={4}>
          <Key>if</Key>: <Str>{`github.event_name == 'push'`}</Str>
        </YamlLine>
        <YamlLine n={19} indent={4}>
          <Key>run</Key>:{" "}
          <Str>nitro schema upload --api checkout --tag ${"$"}GITHUB_SHA</Str>
        </YamlLine>
        <YamlLine n={20} indent={3}>
          - <Key>name</Key>: <Str>publish</Str>
        </YamlLine>
        <YamlLine n={21} indent={4}>
          <Key>if</Key>: <Str>{`github.ref == 'refs/heads/main'`}</Str>
        </YamlLine>
        <YamlLine n={22} indent={4}>
          <Key>env</Key>:
        </YamlLine>
        <YamlLine n={23} indent={5}>
          <Key>NITRO_TOKEN</Key>: <Str>${"${{ secrets.NITRO_TOKEN }}"}</Str>
        </YamlLine>
        <YamlLine n={24} indent={4}>
          <Key>run</Key>:{" "}
          <Str>
            nitro schema publish --api checkout --tag ${"$"}GITHUB_SHA --stage
            prod --require-approval
          </Str>
        </YamlLine>
        <YamlLine n={25}>{""}</YamlLine>
        <YamlLine n={26}>
          <Cmt>
            # same three commands run on Azure DevOps, GitLab, Buildkite,
            Jenkins.
          </Cmt>
        </YamlLine>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Environment ladder: dev / qa / prod                                        */
/* -------------------------------------------------------------------------- */

interface EnvRowProps {
  readonly stage: string;
  readonly tag: string;
  readonly clients: string;
  readonly gate: string;
  readonly latest?: boolean;
}

function EnvRow({ stage, tag, clients, gate, latest = false }: EnvRowProps) {
  return (
    <div
      className="border-cc-card-border grid items-center border-t first:border-t-0"
      style={{ gridTemplateColumns: "0.7fr 1fr 1.2fr 1.1fr" }}
    >
      <div className="px-4 py-3">
        <span
          className="rounded-md border px-2 py-0.5 font-mono text-[0.7rem] tracking-tight"
          style={{
            color: latest ? ACCENT : "rgba(245,241,234,0.78)",
            borderColor: latest
              ? "rgba(94,234,212,0.32)"
              : "var(--color-cc-card-border)",
            backgroundColor: latest ? "rgba(94,234,212,0.06)" : "transparent",
          }}
        >
          {stage}
        </span>
      </div>
      <div className="border-cc-card-border text-cc-heading border-l px-4 py-3 font-mono text-[0.78rem]">
        {tag}
      </div>
      <div className="border-cc-card-border text-cc-ink-dim border-l px-4 py-3 text-[0.85rem]">
        {clients}
      </div>
      <div className="border-cc-card-border text-cc-ink-dim border-l px-4 py-3 text-[0.85rem]">
        {gate}
      </div>
    </div>
  );
}

function EnvLadder() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <div
        className="border-cc-card-border bg-cc-surface/40 grid border-b"
        style={{ gridTemplateColumns: "0.7fr 1fr 1.2fr 1.1fr" }}
      >
        <div className="px-4 py-3">
          <Eyebrow>stage</Eyebrow>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3">
          <Eyebrow>active tag</Eyebrow>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3">
          <Eyebrow>active clients</Eyebrow>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3">
          <Eyebrow>promotion gate</Eyebrow>
        </div>
      </div>
      <EnvRow
        stage="dev"
        tag="checkout@9f3a1c2"
        clients="web-dev, mobile-canary"
        gate="auto on push to main"
      />
      <EnvRow
        stage="qa"
        tag="checkout@7a01b88"
        clients="web@2.14.0, partner-bff@0.3.1"
        gate="optional reviewer approval"
      />
      <EnvRow
        stage="prod"
        tag="checkout@7a01b88"
        clients="web@2.14.0, mobile@1.8.3"
        gate="reviewer approval · breaking blocks"
        latest
      />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Classification legend                                                      */
/* -------------------------------------------------------------------------- */

interface ChangeKindProps {
  readonly kind: "safe" | "dangerous" | "breaking";
  readonly title: string;
  readonly description: string;
  readonly examples: readonly string[];
  readonly behaviour: string;
}

function ChangeKind({
  kind,
  title,
  description,
  examples,
  behaviour,
}: ChangeKindProps) {
  const color =
    kind === "safe" ? TERM.ok : kind === "dangerous" ? TERM.warn : TERM.danger;
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex flex-col gap-3 rounded-xl border p-5 backdrop-blur-sm">
      <div className="flex items-center justify-between">
        <span
          className="font-mono text-[0.7rem] tracking-[0.18em] uppercase"
          style={{ color }}
        >
          {kind}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-tight">
          {title}
        </span>
      </div>
      <p className="text-cc-prose text-[0.95rem] leading-snug">{description}</p>
      <ul className="border-cc-card-border flex flex-col gap-1 border-t pt-3">
        {examples.map((e) => (
          <li
            key={e}
            className="text-cc-ink-dim font-mono text-[0.72rem] leading-5"
          >
            <span style={{ color }}>
              {kind === "safe" ? "+" : kind === "dangerous" ? "~" : "-"}
            </span>{" "}
            {e}
          </li>
        ))}
      </ul>
      <p
        className="mt-auto font-mono text-[0.68rem] tracking-tight"
        style={{ color }}
      >
        {behaviour}
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Audit log                                                                  */
/* -------------------------------------------------------------------------- */

interface AuditEntryProps {
  readonly time: string;
  readonly actor: string;
  readonly verb: string;
  readonly tag: string;
  readonly stage: string;
  readonly result: "ok" | "warn" | "danger";
}

function AuditEntry({
  time,
  actor,
  verb,
  tag,
  stage,
  result,
}: AuditEntryProps) {
  const color =
    result === "ok" ? TERM.ok : result === "warn" ? TERM.warn : TERM.danger;
  const glyph = result === "ok" ? "OK" : result === "warn" ? "!!" : "XX";
  return (
    <div className="border-cc-card-border grid items-center gap-3 border-t px-4 py-2.5 font-mono text-[0.74rem] first:border-t-0 sm:grid-cols-[7rem_8rem_1fr_5rem_3rem]">
      <span className="text-cc-nav-label">{time}</span>
      <span className="text-cc-ink">{actor}</span>
      <span className="text-cc-heading">
        {verb} <span className="text-cc-ink-dim">{tag}</span>
      </span>
      <span className="text-cc-ink-dim">{stage}</span>
      <span style={{ color }} className="text-[0.7rem] tracking-[0.18em]">
        {glyph}
      </span>
    </div>
  );
}

function AuditLog() {
  const entries: readonly AuditEntryProps[] = [
    {
      time: "14:02:11Z",
      actor: "ci-runner@gh",
      verb: "publish",
      tag: "checkout@9f3a1c2",
      stage: "prod",
      result: "ok",
    },
    {
      time: "14:01:48Z",
      actor: "reviewer-2",
      verb: "approve",
      tag: "checkout@9f3a1c2",
      stage: "prod",
      result: "ok",
    },
    {
      time: "14:01:20Z",
      actor: "reviewer-1",
      verb: "approve",
      tag: "checkout@9f3a1c2",
      stage: "prod",
      result: "ok",
    },
    {
      time: "13:58:02Z",
      actor: "ci-runner@gh",
      verb: "upload",
      tag: "checkout@9f3a1c2",
      stage: "(none)",
      result: "ok",
    },
    {
      time: "13:57:31Z",
      actor: "ci-runner@gh",
      verb: "validate",
      tag: "checkout@9f3a1c2",
      stage: "prod",
      result: "warn",
    },
    {
      time: "11:42:17Z",
      actor: "service-owner",
      verb: "block",
      tag: "checkout@e21ffe0",
      stage: "prod",
      result: "danger",
    },
    {
      time: "11:41:55Z",
      actor: "ci-runner@gh",
      verb: "validate",
      tag: "checkout@e21ffe0",
      stage: "prod",
      result: "danger",
    },
  ];
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <div className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between border-b px-4 py-3">
        <Eyebrow>registry · audit log</Eyebrow>
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-tight">
          checkout · last 24h
        </span>
      </div>
      {entries.map((e) => (
        <AuditEntry key={`${e.time}-${e.verb}`} {...e} />
      ))}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section heading helper                                                     */
/* -------------------------------------------------------------------------- */

interface SectionHeadProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function SectionHead({ eyebrow, title, children }: SectionHeadProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight">
        {title}
      </h2>
      {children ? (
        <p className="text-cc-prose mt-4 text-[1.05rem] leading-relaxed">
          {children}
        </p>
      ) : null}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export default function ContinuousIntegrationV3Page() {
  return (
    <div className="flex flex-col gap-28 py-6 sm:gap-36">
      {/* ----------------------------- HERO ----------------------------- */}
      <section className="grid items-start gap-12 lg:grid-cols-[0.9fr_1.1fr]">
        <div>
          <Eyebrow>Continuous integration · CLI first</Eyebrow>
          <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
            Ship schemas from the{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage: `linear-gradient(100deg, ${SCENE_FROM}, ${SCENE_MID}, ${SCENE_TO})`,
              }}
            >
              terminal you already trust.
            </span>
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-[1.15rem] leading-relaxed">
            The Nitro CLI is the single surface for schema evolution. Validate
            against the registry, upload a candidate tagged with your commit,
            and publish behind an approval gate. Three commands, one binary, the
            same pipeline whether you run GitHub Actions, Azure DevOps,
            Buildkite, or a shell on a build box.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/docs/nitro">Get Started</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch
            </OutlineButton>
          </div>
          <ul className="text-cc-ink-dim mt-9 flex flex-col gap-2 text-[0.9rem]">
            {[
              "Breaking changes classified before they reach prod",
              "Published clients affected, named in the diff",
              "Per-environment workflows: dev, QA, prod",
              "Approval gates and full audit trail in the registry",
            ].map((item) => (
              <li key={item} className="flex items-center gap-2.5">
                <span style={{ color: ACCENT }}>
                  <CheckIcon size={13} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        <div className="relative">
          <div
            className="pointer-events-none absolute -inset-6 -z-10 rounded-3xl opacity-40 blur-2xl"
            style={{
              background: `radial-gradient(60% 60% at 30% 20%, ${SCENE_FROM}33, transparent 70%)`,
            }}
            aria-hidden
          />
          <CliCenterpiece />
          <p className="text-cc-nav-label mt-3 font-mono text-[0.66rem] tracking-tight">
            illustrative session ·{" "}
            <code className="text-cc-ink">nitro --version</code> available on
            install
          </p>
        </div>
      </section>

      {/* --------------------- CLI surfaces grid ------------------------ */}
      <section>
        <SectionHead
          eyebrow="CLI surfaces · validate · upload · publish"
          title="Three commands, one lifecycle, every pipeline."
        >
          Each command does one thing, exits with a real status code, and writes
          machine-readable JSON when you ask for it. Compose them how your
          pipeline already composes steps.
        </SectionHead>

        <div className="mt-10 grid gap-5 md:grid-cols-3">
          <CliCard
            title="nitro schema validate"
            purpose="Compare a local schema against the registry tag for a stage. Classifies every change and lists published clients affected."
            invocation={`nitro schema validate \\
  --api checkout \\
  --schema ./schema.graphql \\
  --stage prod \\
  --format json`}
            bullets={[
              "Distinct exit codes per classification, scriptable in CI",
              "JSON output drops straight into PR checks",
              "Local pre-commit or CI, identical behaviour",
            ]}
          />
          <CliCard
            title="nitro schema upload"
            purpose="Push a candidate to the central registry, tagged with your commit SHA. Nothing is promoted yet. Nothing serves traffic."
            invocation={`nitro schema upload \\
  --api checkout \\
  --tag $GIT_SHA \\
  --schema ./schema.graphql`}
            bullets={[
              "Candidates are immutable, addressable by tag",
              "Linked to the actor and the pipeline run",
              "Discoverable in the registry web UI",
            ]}
          />
          <CliCard
            title="nitro schema publish"
            purpose="Promote an uploaded candidate into a stage. Approval gates block breaking changes and require named reviewers."
            invocation={`nitro schema publish \\
  --api checkout \\
  --tag $GIT_SHA \\
  --stage prod \\
  --require-approval`}
            bullets={[
              "Optional reviewer approval per stage",
              "Previous tag retained, rollback by republishing it",
              "Blocked publishes surface published clients affected",
            ]}
          />
        </div>
      </section>

      {/* ------------------- pipeline + environments ------------------- */}
      <section className="grid gap-12 lg:grid-cols-[1.1fr_0.9fr]">
        <div>
          <SectionHead
            eyebrow="Connect your ecosystem"
            title="The same three commands, in whatever pipeline runs your code."
          >
            Drop the Nitro CLI into a GitHub Actions job, an Azure DevOps task,
            a GitLab stage, or a Buildkite step. There is no per-vendor plugin
            tree to chase, just a binary and the commands above.
          </SectionHead>
          <ul className="mt-6 flex flex-col gap-2.5 text-[0.92rem]">
            {[
              "Install Nitro via your existing tooling, no per-vendor plugin needed",
              "Auth via short-lived NITRO_TOKEN from your secret store",
              "Tag with $GITHUB_SHA in Actions, $BUILD_SOURCEVERSION in Azure DevOps, $CI_COMMIT_SHA in GitLab",
              "Outputs structured JSON for downstream PR comments",
              "Runs offline against a self-hosted Nitro service too",
            ].map((item) => (
              <li
                key={item}
                className="text-cc-ink-dim flex items-start gap-2.5"
              >
                <span style={{ color: ACCENT }} className="mt-1">
                  <CheckIcon size={13} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>
        <PipelineMock />
      </section>

      <section>
        <SectionHead
          eyebrow="Per-environment workflows"
          title="Dev, QA, prod each carry their own active tag and active clients."
        >
          A stage in the registry is a named promotion target with its own
          policy. Candidates flow upward by promotion, never by re-upload, so
          what you tested in QA is bit-for-bit what you publish to prod.
        </SectionHead>
        <div className="mt-10">
          <EnvLadder />
        </div>
        <p className="text-cc-nav-label mt-4 font-mono text-[0.66rem] tracking-tight">
          stages are configured per API. Add more (canary, eu-prod, edge) when
          you need them.
        </p>
      </section>

      {/* ---------------- breaking change classification ---------------- */}
      <section>
        <SectionHead
          eyebrow="Change classification"
          title="Every diff lands in one of three buckets, before anyone hits deploy."
        >
          The CLI walks the schema diff and projects it against operations
          recorded by published clients. The bucket determines the exit code,
          the approval gate, and the PR check colour.
        </SectionHead>
        <div className="mt-10 grid gap-5 md:grid-cols-3">
          <ChangeKind
            kind="safe"
            title="additive only"
            description="The new schema is a strict superset of the old one. No recorded operation can fail because of this change."
            examples={[
              "new Query field",
              "new optional argument",
              "new enum value on an input enum",
            ]}
            behaviour="passes the gate · auto-promote where allowed"
          />
          <ChangeKind
            kind="dangerous"
            title="behaviour shift"
            description="The shape is compatible, but the meaning is not. Existing operations keep typechecking; their results may change."
            examples={[
              "default argument value flipped",
              "new enum value on a response enum",
              "nullability widened on a return type",
            ]}
            behaviour="warning exit code · requires reviewer approval"
          />
          <ChangeKind
            kind="breaking"
            title="contract removed"
            description="At least one recorded operation no longer typechecks. The diff names every published client affected and the offending field."
            examples={[
              "field removed from a response type",
              "required argument added",
              "type kind changed (object to interface)",
            ]}
            behaviour="failing exit code · publish blocked at the gate"
          />
        </div>
      </section>

      {/* ------------------- schema deep dive (Nitro) ------------------- */}
      <section>
        <SectionHead
          eyebrow="Inside the registry"
          title="The same classification, with usage attached, in the Nitro UI."
        >
          The CLI is the contract; the Nitro UI is the audit and drilldown. Pick
          a field, see every published client that depends on it, and decide on
          the rollout before you cut the tag.
        </SectionHead>
        <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border">
          <NitroSchema />
        </div>
      </section>

      {/* --------------------- audit log + honesty --------------------- */}
      <section className="grid gap-12 lg:grid-cols-[1fr_1fr]">
        <div>
          <SectionHead
            eyebrow="Audit · who, what, when, which tag"
            title="Every CLI call is recorded against the actor and the candidate tag."
          >
            Validates, uploads, approvals, publishes, and blocks all land in the
            same append-only log. The registry is the canonical history of your
            schema across environments, queryable by API, stage, tag, or actor.
          </SectionHead>
          <ul className="mt-6 flex flex-col gap-2.5 text-[0.92rem]">
            {[
              "Filter by actor, stage, tag, or verb",
              "Visible in the registry UI and via the Nitro API",
              "Append-only history per API across environments",
            ].map((item) => (
              <li
                key={item}
                className="text-cc-ink-dim flex items-start gap-2.5"
              >
                <span style={{ color: ACCENT }} className="mt-1">
                  <CheckIcon size={13} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>
        <AuditLog />
      </section>

      {/* --------------------------- HONESTY ---------------------------- */}
      <section className="border-cc-card-border bg-cc-surface/50 rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
        <Eyebrow>Where the line is</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-3 max-w-3xl font-semibold tracking-tight">
          The CLI tells you what the registry knows, no more, no less.
        </h2>
        <div className="mt-7 grid gap-6 sm:grid-cols-2">
          <p className="text-cc-prose text-[1rem] leading-relaxed">
            Published clients affected is grounded in the operations your
            clients have uploaded to the registry. A client that never published
            its operations cannot be flagged, so part of adopting the workflow
            is making sure your clients publish too.
          </p>
          <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
            The GraphQL IDE you embed is served from your GraphQL endpoint, not
            from Nitro. Telemetry needs Nitro configured in your service. Once
            it is, the CLI and the registry have the same view of what ships and
            what depends on it.
          </p>
        </div>
      </section>

      {/* ---------------------------- CTA ------------------------------- */}
      <section className="flex flex-col items-center gap-7 py-6 text-center">
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Install the CLI. Wire the three commands. Sleep through deploys.
        </h2>
        <p className="text-cc-prose max-w-xl text-[1.1rem] leading-relaxed">
          Validate against the registry, upload a candidate per commit, publish
          behind a gate. The rest of your pipeline stays exactly the way you
          built it.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/docs/nitro">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </section>
    </div>
  );
}
