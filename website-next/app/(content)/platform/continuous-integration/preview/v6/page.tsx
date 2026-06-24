import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { PourOver } from "@/src/icons/PourOver";
import { NitroSchema } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Continuous Integration for GraphQL | ChilliCream",
  description:
    "GraphQL schema registry CI: validate, upload, publish, and deploy every change through one Nitro CLI pipeline on GitHub Actions, Azure DevOps, or any shell runner.",
  keywords: [
    "GraphQL schema registry CI",
    "GraphQL continuous integration",
    "Nitro CLI",
    "schema registry",
    "client registry",
    "GitHub Actions GraphQL",
    "Azure DevOps GraphQL",
    "breaking change classification",
    "validate publish gate",
    "environment workflows",
  ],
  openGraph: {
    title: "Continuous Integration for GraphQL",
    description:
      "A GraphQL schema registry CI pipeline. Validate, upload, publish, and deploy through the Nitro CLI on any runner.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Scene tokens. The pipeline is reframed as a barista order ticket    */
/* flowing through four stations (Cup, Dose, Pull, Serve). The palette */
/* stays cc-* dark navy/teal. Coffee lives only in copy and a few      */
/* inline drink icons.                                                  */
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
/* Hero: "Today's order ticket" docket with four bar stations          */
/* ------------------------------------------------------------------ */

interface StationDef {
  readonly num: string;
  readonly key: string;
  readonly title: string;
  readonly real: string;
  readonly sub: string;
  readonly meta: string;
  readonly state: StageState;
}

const STATIONS: readonly StationDef[] = [
  {
    num: "01",
    key: "cup",
    title: "Cup",
    real: "Validate",
    sub: "score the change",
    meta: "schema + clients",
    state: "passed",
  },
  {
    num: "02",
    key: "dose",
    title: "Dose",
    real: "Upload",
    sub: "weigh and tag",
    meta: "version + sha",
    state: "passed",
  },
  {
    num: "03",
    key: "pull",
    title: "Pull",
    real: "Publish",
    sub: "approver at the bar",
    meta: "promote to env",
    state: "active",
  },
  {
    num: "04",
    key: "serve",
    title: "Serve",
    real: "Deploy",
    sub: "out to every table",
    meta: "your pipeline",
    state: "pending",
  },
];

interface CupDotProps {
  readonly state: StageState;
}

function CupDot({ state }: CupDotProps) {
  const t = STAGE_TONE[state];
  return (
    <span
      className={`inline-flex h-5 w-5 items-center justify-center rounded-full ring-1 ring-inset ${t.bg} ${t.ring}`}
      aria-hidden
    >
      <svg viewBox="0 0 16 16" width={11} height={11} className={t.text}>
        <path
          d="M3 6 H11 V10 a2 2 0 0 1 -2 2 H5 a2 2 0 0 1 -2 -2 Z M11 7 h1.5 a1.5 1.5 0 0 1 0 3 H11"
          stroke="currentColor"
          strokeWidth="1.2"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M5 3 Q5 4 6 4.5 M8 3 Q8 4 9 4.5"
          stroke="currentColor"
          strokeWidth="1.2"
          fill="none"
          strokeLinecap="round"
        />
      </svg>
    </span>
  );
}

interface StationNodeProps {
  readonly station: StationDef;
}

function StationNode({ station }: StationNodeProps) {
  const tone = STAGE_TONE[station.state];
  return (
    <div
      className="border-cc-card-border relative flex-1 rounded-lg border px-4 py-4"
      style={{ backgroundColor: TRACK_RAISED }}
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] uppercase">
          {station.num} · {station.real}
        </span>
        <StageChip state={station.state} />
      </div>
      <div className="mt-3 flex items-center gap-2">
        <CupDot state={station.state} />
        <span
          className={`font-heading text-[1.05rem] font-semibold ${tone.text}`}
        >
          {station.title}
        </span>
      </div>
      <div className="text-cc-prose mt-1 font-mono text-[0.72rem]">
        {station.sub}
      </div>
      <div className="text-cc-ink-dim mt-2 font-mono text-[0.62rem]">
        {station.meta}
      </div>
    </div>
  );
}

function StationConnector() {
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

function OrderTicket() {
  return (
    <div
      className="border-cc-card-border relative overflow-hidden rounded-xl border p-6 sm:p-8"
      style={{
        backgroundColor: TRACK_BG,
        backgroundImage: `linear-gradient(${TRACK_LINE} 1px, transparent 1px), linear-gradient(90deg, ${TRACK_LINE} 1px, transparent 1px)`,
        backgroundSize: "26px 26px",
      }}
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span
            className="border-cc-card-border text-cc-accent flex h-8 w-8 items-center justify-center rounded-md border"
            style={{ backgroundColor: "#0d1b30" }}
            aria-hidden
          >
            <CoffeeTray className="h-5 w-5" />
          </span>
          <div className="flex flex-col">
            <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
              today&apos;s order ticket
            </span>
            <span className="text-cc-ink-dim font-mono text-[0.66rem]">
              orders-api · release
            </span>
          </div>
        </div>
        <div className="text-cc-ink-dim hidden font-mono text-[0.62rem] sm:block">
          docket #482 · 3m 12s
        </div>
      </div>
      <div className="mt-6 flex flex-col items-stretch gap-3 sm:flex-row sm:items-stretch">
        {STATIONS.map((station, i) => (
          <div key={station.key} className="contents">
            <StationNode station={station} />
            {i < STATIONS.length - 1 && <StationConnector />}
          </div>
        ))}
      </div>
      <div className="border-cc-card-border mt-6 flex flex-wrap items-center gap-x-6 gap-y-2 border-t pt-4">
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
          <span className="bg-cc-success h-2 w-2 rounded-full" />
          cup, dose cleared
        </span>
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
          <span className="bg-cc-warning h-2 w-2 rounded-full" />
          pull waiting on approver
        </span>
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
          <span className="bg-cc-nav-label/60 h-2 w-2 rounded-full" />
          serve runs after pull
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
          <Eyebrow>
            On the bar today · Platform · Continuous Integration
          </Eyebrow>
          <h1 className="font-heading text-hero text-cc-heading mt-5 font-bold tracking-tight">
            Every schema change,
            <br />
            pulled like a shot.
          </h1>
          <p className="lead text-cc-ink-dim mt-6 max-w-2xl">
            Validate, upload, publish, and deploy every GraphQL change through
            one Nitro CLI pipeline. Four stations on a single docket: cup the
            change, dose the version, pull at the bar, serve to every table.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </div>
      <OrderTicket />
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Station 01: Cup (Validate), cupping scorecard                       */
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
      score: "9.0",
    };
  }
  if (k === "dangerous") {
    return {
      text: "text-cc-warning",
      bg: "bg-cc-warning/10",
      ring: "ring-cc-warning/30",
      label: "DANGEROUS",
      score: "6.5",
    };
  }
  return {
    text: "text-cc-danger",
    bg: "bg-cc-danger/10",
    ring: "ring-cc-danger/30",
    label: "BREAKING",
    score: "3.0",
  };
}

function CupVisual() {
  return (
    <Panel
      title={
        <>
          <span className="text-cc-prose">dotnet nitro schema publish</span>
          <span className="text-cc-nav-label">·</span>
          <span>cupping</span>
        </>
      }
      tab="3 changes"
      footer={
        <div className="flex items-center justify-between">
          <span className="text-cc-ink-dim font-mono text-[0.66rem]">
            cupping complete
          </span>
          <span className="text-cc-nav-label font-mono text-[0.66rem]">
            1 safe · 1 dangerous · 1 breaking
          </span>
        </div>
      }
    >
      <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[auto_minmax(0,1fr)_auto_auto] gap-3 border-b px-4 py-2 font-mono text-[0.58rem] tracking-[0.14em] uppercase">
        <span>verdict</span>
        <span>change</span>
        <span>score</span>
        <span>source</span>
      </div>
      {VALIDATE_ROWS.map((row) => {
        const t = kindTone(row.kind);
        return (
          <div
            key={row.path}
            className="border-cc-card-border grid grid-cols-[auto_minmax(0,1fr)_auto_auto] items-center gap-3 border-b px-4 py-3 last:border-b-0"
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
            <span className={`font-mono text-[0.72rem] ${t.text}`}>
              {t.score}
            </span>
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
/* Station 02: Dose (Upload), terminal block                           */
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

function DoseVisual() {
  return (
    <Panel
      title={
        <>
          <span>ci runner</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">github actions</span>
        </>
      }
      tab="dose"
      footer={
        <div className="text-cc-ink-dim flex items-center justify-between gap-2 font-mono text-[0.66rem]">
          <span className="flex items-center gap-2">
            <span className="bg-cc-success h-2 w-2 rounded-full" />
            schema v14 staged, awaiting pull
          </span>
          <span className="text-cc-nav-label">
            grams in: schema v14 · sha a1f2e9c
          </span>
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
/* Station 03: Pull (Publish), wraps NitroSchema                        */
/* ------------------------------------------------------------------ */

function PullVisual() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mx-auto overflow-hidden rounded-xl border">
      <NitroSchema />
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Station 04: Serve (Deploy), tables matrix                            */
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

function ServeVisual() {
  return (
    <Panel
      title={
        <>
          <span>orders-api</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">tables</span>
        </>
      }
      tab="per-environment workflow"
    >
      <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[1fr_0.7fr_0.8fr_0.9fr] gap-3 border-b px-4 py-2 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
        <span>table</span>
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
/* Station section shell. The station mark is a drink icon.            */
/* ------------------------------------------------------------------ */

interface StationSectionProps {
  readonly num: string;
  readonly station: string;
  readonly title: string;
  readonly lead: string;
  readonly bullets: readonly string[];
  readonly artifact: ReactNode;
  readonly icon: ReactNode;
  readonly flip?: boolean;
}

function StationSection({
  num,
  station,
  title,
  lead,
  bullets,
  artifact,
  icon,
  flip = false,
}: StationSectionProps) {
  return (
    <section className="grid items-center gap-10 lg:grid-cols-2 lg:gap-14">
      <div className={flip ? "lg:order-2" : ""}>
        <div className="flex items-center gap-3">
          <span
            className="border-cc-card-border text-cc-accent flex h-10 w-10 items-center justify-center rounded-md border"
            style={{ backgroundColor: "#0d1b30" }}
            aria-hidden
          >
            {icon}
          </span>
          <div className="flex flex-col">
            <Eyebrow>
              Station {num} · {station}
            </Eyebrow>
          </div>
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
/* Runner band: same recipe on every machine                           */
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
          <Eyebrow>House recipe</Eyebrow>
          <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
            Same recipe on every machine.
          </h2>
          <p className="text-body text-cc-prose mt-4 max-w-2xl leading-relaxed">
            The Nitro CLI is the only thing the pipeline needs. One recipe card,
            every machine on the bar: GitHub Actions, Azure DevOps, or any
            shell-capable runner. Same commands, same exit codes, same registry
            on the other side.
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
        <Eyebrow>The honest pour</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          What the barista will and will not promise.
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
        Make every release a clean pour.
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

export default function ContinuousIntegrationPreviewV6() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <StationSection
        num="01"
        station="Cup"
        title="Score every change before it touches the bar."
        lead="Run the Nitro CLI against your branch and every change is stamped safe, dangerous, or breaking, checked against the operations your published clients actually send. The build fails loudly, not silently."
        bullets={[
          "Schema diff classified per field: safe, dangerous, or breaking.",
          "Validated against the operations published clients have registered.",
          "Exit code drives your CI: a breaking change fails the job before upload.",
        ]}
        artifact={<CupVisual />}
        icon={<Espresso className="h-6 w-6" />}
      />
      <StationSection
        num="02"
        station="Dose"
        title="Weighed, tagged, and on the rail."
        lead="Once cupping clears, the same CLI uploads the schema to the registry, tagged with your commit SHA and pinned to a target environment. Nothing is live yet, but the version is reproducible."
        bullets={[
          "One command stages the schema against the chosen environment.",
          "Each upload is tagged with the commit SHA for traceability.",
          "Dangerous changes are recorded with reason, not silently dropped.",
        ]}
        artifact={<DoseVisual />}
        icon={<DripBrewer className="text-cc-accent h-6 w-6" />}
        flip
      />
      <StationSection
        num="03"
        station="Pull"
        title="The shot, with an approver at the bar."
        lead="Publish promotes the staged schema for an environment. Approval gates can require a reviewer, classification, or both. Only changes that pass clear the gate and become the new contract."
        bullets={[
          "Approval gates can require a reviewer on dangerous or breaking changes.",
          "Each environment keeps its own active version and history.",
          "The registry stores the full timeline, with classification per release.",
        ]}
        artifact={<PullVisual />}
        icon={<PourOver className="text-cc-accent h-6 w-6" />}
      />
      <StationSection
        num="04"
        station="Serve"
        title="Out to every table."
        lead="Deployment is yours. The Nitro CLI hands control back to your pipeline once publish clears, so the same runner that built your code rolls out the API behind the new schema."
        bullets={[
          "Per-environment workflows: dev, QA, staging, production.",
          "Deploy runs on the same pipeline that validated and published.",
          "Rollback is a re-publish of a prior tagged version.",
        ]}
        artifact={<ServeVisual />}
        icon={<CoffeeTray className="h-6 w-6" />}
        flip
      />
      <RunnerBand />
      <HonestyBand />
      <ClosingCta />
    </div>
  );
}
