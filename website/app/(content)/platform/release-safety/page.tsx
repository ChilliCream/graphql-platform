import type { ReactNode } from "react";

import NextLink from "next/link";

import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CheckIcon } from "@/src/components/CheckIcon";
import { DotGridSurface } from "@/src/components/DotGridSurface";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { PageSection } from "@/src/components/PageSection";
import { SectionHeading } from "@/src/components/SectionHeading";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

export const metadata = pageMetadata({
  title: "GraphQL Schema Checks and Release Safety",
  description:
    "Catch breaking GraphQL schema changes before they ship. Nitro's schema checks validate proposed schemas against the operations your clients use in each environment.",
  path: "/platform/release-safety",
  keywords: [
    "GraphQL schema release safety",
    "breaking change detection",
    "schema registry",
    "client registry",
    "schema validation CI",
    "safe schema evolution",
    "Nitro schema checks",
    "published operation impact",
    "schema linting",
    "GraphQL governance",
  ],
});

/* ------------------------------------------------------------------ */
/* Scene tokens                                                        */
/* This page is status-driven on a guardrail-blue surface:             */
/*   cc-success = safe, cc-warning = dangerous, cc-danger = breaking.   */
/* ------------------------------------------------------------------ */

const GUARDRAIL = "#0a1426";
const GUARDRAIL_LINE = "rgba(124, 146, 198, 0.16)";

type ChangeStatus = "safe" | "dangerous" | "breaking";

const STATUS_META: Record<
  ChangeStatus,
  { label: string; text: string; bg: string; ring: string; dot: string }
> = {
  safe: {
    label: "SAFE",
    text: "text-cc-success",
    bg: "bg-cc-success/10",
    ring: "ring-cc-success/30",
    dot: "bg-cc-success",
  },
  dangerous: {
    label: "DANGEROUS",
    text: "text-cc-warning",
    bg: "bg-cc-warning/10",
    ring: "ring-cc-warning/30",
    dot: "bg-cc-warning",
  },
  breaking: {
    label: "BREAKING",
    text: "text-cc-danger",
    bg: "bg-cc-danger/10",
    ring: "ring-cc-danger/30",
    dot: "bg-cc-danger",
  },
};

/* ------------------------------------------------------------------ */
/* Small shared primitives                                             */
/* ------------------------------------------------------------------ */

interface StatusChipProps {
  readonly status: ChangeStatus;
  readonly className?: string;
}

function StatusChip({ status, className = "" }: StatusChipProps) {
  const meta = STATUS_META[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${meta.bg} ${meta.text} ${meta.ring} ${className}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
      {meta.label}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Window chrome wrapper                                               */
/* ------------------------------------------------------------------ */

interface AppWindowProps {
  readonly title: ReactNode;
  readonly disclosure?: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
  readonly className?: string;
}

/*
 * Thin adapter over the shared MockWindowChrome: this page's windows sit on
 * the guardrail-blue scene, use status-colored traffic lights, and carry an
 * optional disclosure eyebrow above the frame.
 */
function AppWindow({
  title,
  disclosure,
  children,
  footer,
  className = "",
}: AppWindowProps) {
  return (
    <div className="min-w-0">
      {disclosure !== undefined && (
        <Eyebrow color="ink-dim" size="2xs" className="mb-3">
          {disclosure}
        </Eyebrow>
      )}
      <MockWindowChrome
        header={{
          variant: "custom",
          content: (
            <>
              <span className="flex gap-1.5" aria-hidden>
                <span className="bg-cc-danger/60 h-2.5 w-2.5 rounded-full" />
                <span className="bg-cc-warning/60 h-2.5 w-2.5 rounded-full" />
                <span className="bg-cc-success/60 h-2.5 w-2.5 rounded-full" />
              </span>
              <div className="text-cc-ink-dim ml-2 flex items-center gap-2 font-mono text-[0.72rem]">
                {title}
              </div>
            </>
          ),
        }}
        headerClassName="flex items-center gap-2 bg-[#0d1b30] px-4 py-2.5"
        footer={footer}
        footerClassName="bg-[#0d1b30] px-4 py-2.5"
        shadow="none"
        rounded="rounded-xl"
        surfaceClassName={`bg-[#0a1426] shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)] backdrop-blur-md ${className}`}
      >
        {children}
      </MockWindowChrome>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* HERO: status-stamped unified schema diff + pinned review thread     */
/* ------------------------------------------------------------------ */

type DiffSign = "+" | "-" | " ";

interface DiffLine {
  readonly old: number | null;
  readonly nw: number | null;
  readonly sign: DiffSign;
  readonly code: ReactNode;
  readonly status?: ChangeStatus;
  readonly pinned?: boolean;
}

function diffRowColor(sign: DiffSign): string {
  if (sign === "+") {
    return "bg-cc-success/[0.06]";
  }
  if (sign === "-") {
    return "bg-cc-danger/[0.07]";
  }
  return "";
}

function gutterColor(sign: DiffSign): string {
  if (sign === "+") {
    return "text-cc-success/70";
  }
  if (sign === "-") {
    return "text-cc-danger/70";
  }
  return "text-cc-nav-label";
}

const tk = {
  kw: (s: string) => <span className="text-cc-info">{s}</span>,
  ty: (s: string) => <span className="text-cc-tip">{s}</span>,
  fld: (s: string) => <span className="text-cc-heading">{s}</span>,
  dir: (s: string) => <span className="text-cc-note">{s}</span>,
  punc: (s: string) => <span className="text-cc-ink-dim">{s}</span>,
};

interface DiffRowProps {
  readonly line: DiffLine;
}

function DiffRow({ line }: DiffRowProps) {
  return (
    <div className={`flex items-stretch ${diffRowColor(line.sign)}`}>
      <span className="border-cc-card-border text-cc-ink-dim w-9 shrink-0 border-r py-1 pr-2 text-right font-mono text-[0.66rem] select-none">
        {line.old ?? ""}
      </span>
      <span className="border-cc-card-border text-cc-ink-dim w-9 shrink-0 border-r py-1 pr-2 text-right font-mono text-[0.66rem] select-none">
        {line.nw ?? ""}
      </span>
      <span
        className={`w-5 shrink-0 py-1 pl-2 font-mono text-[0.78rem] select-none ${gutterColor(line.sign)}`}
      >
        {line.sign}
      </span>
      <span className="text-cc-prose min-w-0 flex-1 py-1 pr-3 font-mono text-[0.78rem] leading-relaxed whitespace-pre">
        {line.code}
      </span>
    </div>
  );
}

const HERO_DIFF: readonly DiffLine[] = [
  {
    old: 41,
    nw: 41,
    sign: " ",
    code: (
      <>
        {tk.kw("type")} {tk.ty("Order")} {tk.punc("{")}
      </>
    ),
  },
  {
    old: 42,
    nw: 42,
    sign: " ",
    code: (
      <>
        {"  "}
        {tk.fld("id")}
        {tk.punc(": ID!")}
      </>
    ),
  },
  {
    old: null,
    nw: 43,
    sign: "+",
    code: (
      <>
        {"  "}
        {tk.fld("totalAmount")}
        {tk.punc(": ")}
        {tk.ty("Money!")}
      </>
    ),
    status: "safe",
  },
  {
    old: 43,
    nw: null,
    sign: "-",
    code: (
      <>
        {"  "}
        {tk.fld("total")}
        {tk.punc(": ")}
        {tk.ty("Float!")}
      </>
    ),
    status: "breaking",
    pinned: true,
  },
  {
    old: 44,
    nw: 44,
    sign: " ",
    code: (
      <>
        {"  "}
        {tk.fld("status")}
        {tk.punc(": ")}
        {tk.ty("OrderStatus!")}
      </>
    ),
  },
  {
    old: null,
    nw: 45,
    sign: "+",
    code: (
      <>
        {"  "}
        {tk.fld("placedAt")}
        {tk.punc(": ")}
        {tk.ty("DateTime")} {tk.dir("@deprecated")}
        {tk.punc('(reason: "use createdAt")')}
      </>
    ),
    status: "dangerous",
  },
  {
    old: 45,
    nw: 46,
    sign: " ",
    code: <>{tk.punc("}")}</>,
  },
];

function PinnedThread() {
  return (
    <div className="border-cc-danger/50 bg-cc-danger/[0.05] ml-[3.55rem] border-l-2">
      <div className="px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="bg-cc-danger/15 text-cc-danger flex h-5 w-5 items-center justify-center rounded-full font-mono text-[0.6rem] font-semibold">
            R
          </span>
          <span className="text-cc-heading text-[0.78rem] font-medium">
            Registry
          </span>
          <StatusChip status="breaking" />
          <span className="text-cc-ink-dim ml-auto font-mono text-[0.62rem]">
            line 43
          </span>
        </div>
        <p className="text-cc-ink-dim mt-2 text-[0.78rem] leading-relaxed">
          Removing{" "}
          <code className="bg-cc-hover text-cc-prose rounded px-1 font-mono text-[0.72rem]">
            Order.total
          </code>{" "}
          breaks queries that still select it.{" "}
          <span className="text-cc-prose">
            Queries and mutations from 3 client versions published to this stage
            are affected.
          </span>{" "}
          Deprecate it, then remove it after those versions are retired or
          unpublished from the stage.
        </p>
      </div>
    </div>
  );
}

function HeroDiffMock() {
  return (
    <AppWindow
      title={<span className="text-cc-prose">schema.graphql</span>}
      footer={
        <div className="flex items-center justify-between">
          <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
            <span className="bg-cc-danger h-2 w-2 rounded-full" />
            registry check failed
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.66rem]">
            1 breaking · 1 dangerous · 1 safe
          </span>
        </div>
      }
    >
      <div
        role="region"
        aria-label="Illustrative schema diff"
        tabIndex={0}
        className="focus-visible:ring-cc-accent/40 overflow-x-auto focus-visible:ring-2 focus-visible:outline-none"
      >
        <div className="min-w-[30rem]">
          <div className="bg-cc-success/[0.04] text-cc-ink-dim px-4 py-1.5 font-mono text-[0.64rem]">
            @@ type Order @@
          </div>
          <div>
            {HERO_DIFF.map((line, i) => (
              <div key={i}>
                <DiffRow line={line} />
                {line.pinned === true && <PinnedThread />}
              </div>
            ))}
          </div>
        </div>
      </div>
    </AppWindow>
  );
}

function HeroSection() {
  return (
    <DotGridSurface className="border-cc-card-border/50 bg-cc-surface/25 relative left-1/2 -mt-14 w-screen -translate-x-1/2 border-b py-16 sm:py-24">
      <PageSection className="grid items-center gap-12 lg:grid-cols-[minmax(0,0.92fr)_minmax(0,1.08fr)]">
        <div className="min-w-0">
          <h1 className="font-heading text-h2 text-cc-heading font-bold tracking-tight">
            Ship GraphQL schema changes
            <br />
            with confidence.
          </h1>
          <p className="text-body text-cc-prose mt-5 max-w-xl leading-relaxed">
            Nitro&apos;s schema checks test a proposed change against the
            operations your clients rely on in production. Breaking change
            detection shows what would break, so you can fix it before it
            reaches users.
          </p>
          <ButtonRow align="start" className="mt-9">
            <SolidButton href="https://nitro.chillicream.com">
              Start for free
            </SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read client registry docs
            </OutlineButton>
          </ButtonRow>
        </div>
        <div className="relative min-w-0">
          <div
            aria-hidden
            className="absolute -inset-6 -z-10 rounded-3xl opacity-60 blur-2xl"
            style={{
              background:
                "radial-gradient(60% 60% at 70% 20%, rgba(124,146,198,0.18), transparent 70%)",
            }}
          />
          <HeroDiffMock />
        </div>
      </PageSection>
    </DotGridSurface>
  );
}

/* ------------------------------------------------------------------ */
/* Reusable two-column section shell (house SectionHeading)            */
/* ------------------------------------------------------------------ */

interface SectionShellProps {
  readonly title: string;
  readonly lead: string;
  readonly artifact: ReactNode;
  readonly flip?: boolean;
}

function SectionShell({
  title,
  lead,
  artifact,
  flip = false,
}: SectionShellProps) {
  return (
    <section className="grid items-center gap-10 lg:grid-cols-2 lg:gap-14">
      <div className={`min-w-0 ${flip ? "lg:order-2" : ""}`}>
        <SectionHeading title={title} description={lead} />
      </div>
      <div className={`min-w-0 ${flip ? "lg:order-1" : ""}`}>{artifact}</div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: PR registry check status card                              */
/* ------------------------------------------------------------------ */

function CrossGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={12} height={12} aria-hidden>
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

function SpinnerGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width={12}
      height={12}
      aria-hidden
      className="animate-spin motion-reduce:animate-none"
    >
      <circle
        cx="8"
        cy="8"
        r="6"
        fill="none"
        stroke="currentColor"
        strokeOpacity="0.25"
        strokeWidth="2"
      />
      <path
        d="M8 2 a6 6 0 0 1 6 6"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

function ValidatingDots() {
  return (
    <span>
      validating
      {[0, 1, 2].map((i) => (
        <span
          key={i}
          className="animate-[ellipsis-blink_1.2s_ease-in-out_infinite] motion-reduce:animate-none"
          style={{ animationDelay: `${i * 0.2}s` }}
        >
          .
        </span>
      ))}
    </span>
  );
}

interface CheckRowProps {
  readonly icon: "fail" | "pass" | "run";
  readonly name: string;
  readonly detail: ReactNode;
  readonly delayClassName: string;
}

function CheckRow({ icon, name, detail, delayClassName }: CheckRowProps) {
  const map = {
    fail: { node: <CrossGlyph />, color: "text-cc-danger" },
    pass: { node: <CheckIcon size={12} />, color: "text-cc-success" },
    run: { node: <SpinnerGlyph />, color: "text-cc-warning" },
  } as const;
  const m = map[icon];
  return (
    <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3 last:border-b-0">
      <span className={`flex h-5 w-5 items-center justify-center ${m.color}`}>
        <RevealOnScroll
          className={`flex ${delayClassName}`}
          hiddenClassName="scale-50 opacity-0 motion-reduce:scale-100"
          shownClassName="scale-100 opacity-100"
        >
          {m.node}
        </RevealOnScroll>
      </span>
      <span className="text-cc-heading text-[0.82rem] font-medium">{name}</span>
      <span className="text-cc-ink-dim ml-auto font-mono text-[0.7rem]">
        {detail}
      </span>
    </div>
  );
}

function CheckCard() {
  return (
    <AppWindow
      title={<span className="text-cc-prose">#482 Add Money type</span>}
      footer={
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-cc-ink-dim font-mono text-[0.66rem]">
            Required CI policy blocks merge until checks pass.
          </span>
          <span className="bg-cc-hover text-cc-prose ring-cc-card-border rounded-md px-2.5 py-1 font-mono text-[0.64rem] ring-1 ring-inset">
            Re-run check
          </span>
        </div>
      }
    >
      <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3.5">
        <span className="bg-cc-danger/15 text-cc-danger ring-cc-danger/30 flex items-center gap-2 rounded-md px-2.5 py-1 font-mono text-[0.66rem] font-semibold tracking-wide ring-1 ring-inset">
          <CrossGlyph /> FAIL
        </span>
        <span className="text-cc-heading text-[0.84rem] font-medium">
          Registry check
        </span>
        <span className="text-cc-ink-dim ml-auto font-mono text-[0.68rem]">
          1 breaking · 2 safe
        </span>
      </div>
      <CheckRow
        icon="fail"
        name="Schema validation — breaking change"
        detail="Order.total removed"
        delayClassName="delay-0"
      />
      <CheckRow
        icon="pass"
        name="Schema validation — additive"
        detail="Money, totalAmount added"
        delayClassName="delay-150"
      />
      <CheckRow
        icon="run"
        name="Client compatibility — partner app"
        detail={<ValidatingDots />}
        delayClassName="delay-300"
      />
    </AppWindow>
  );
}

function CheckCardSection() {
  return (
    <SectionShell
      title="Breaking changes fail the pull request."
      lead="Nitro fails the pull request when a proposed change would break an operation your clients have published. The problem shows up as a red status on the PR, not as an incident after release. Mark it as required and the merge button stays locked until the schema is safe."
      artifact={<CheckCard />}
      flip
    />
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: published-clients impact matrix                            */
/* ------------------------------------------------------------------ */

interface ClientRow {
  readonly name: string;
  readonly env: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "outside";
}

const CLIENT_ROWS: readonly ClientRow[] = [
  { name: "web", env: "production", ok: 5, total: 5, status: "ok" },
  { name: "mobile", env: "production", ok: 3, total: 5, status: "risk" },
  { name: "partner", env: "sandbox", ok: 0, total: 0, status: "outside" },
  { name: "internal-admin", env: "staging", ok: 6, total: 6, status: "ok" },
];

interface ImpactBarProps {
  readonly ok: number;
  readonly total: number;
  readonly status: ClientRow["status"];
}

function ImpactBar({ ok, total, status }: ImpactBarProps) {
  const cells = Array.from({ length: total });
  const color =
    status === "ok"
      ? "bg-cc-success"
      : status === "risk"
        ? "bg-cc-warning"
        : "bg-cc-ink-dim/50";
  return (
    <span className="flex gap-1">
      {cells.map((_, i) => (
        <span
          key={i}
          className={`h-2 w-5 rounded-[2px] ${i < ok ? color : "bg-cc-ink-faint"}`}
        />
      ))}
    </span>
  );
}

function ClientImpactMatrix() {
  const statusLabel: Record<
    ClientRow["status"],
    { text: string; cls: string }
  > = {
    ok: { text: "OK", cls: "text-cc-success" },
    risk: { text: "at risk", cls: "text-cc-warning" },
    outside: { text: "outside result", cls: "text-cc-ink-dim" },
  };
  return (
    <AppWindow
      title={
        <>
          <span>client registry</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">impact of #482</span>
        </>
      }
    >
      <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-[1.3fr_1fr_0.8fr] gap-3 border-b px-4 py-2 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
        <span>client</span>
        <span>operations passing</span>
        <span className="text-right">status</span>
      </div>
      {CLIENT_ROWS.map((c) => {
        const s = statusLabel[c.status];
        return (
          <div
            key={c.name}
            className="border-cc-card-border grid grid-cols-[1.3fr_1fr_0.8fr] items-center gap-3 border-b px-4 py-3 last:border-b-0"
          >
            <div className="min-w-0">
              <div className="text-cc-heading truncate font-mono text-[0.78rem]">
                {c.name}
              </div>
              <div className="text-cc-ink-dim font-mono text-[0.62rem]">
                {c.env}
              </div>
            </div>
            <div className="flex items-center gap-3">
              {c.total === 0 ? (
                <span className="text-cc-heading font-mono text-[0.68rem]">
                  none published
                </span>
              ) : (
                <>
                  <ImpactBar ok={c.ok} total={c.total} status={c.status} />
                  <span className="text-cc-ink-dim font-mono text-[0.68rem]">
                    {c.ok}/{c.total}
                  </span>
                </>
              )}
            </div>
            <div
              className={`text-right font-mono text-[0.72rem] font-semibold ${s.cls}`}
            >
              {s.text}
            </div>
          </div>
        );
      })}
    </AppWindow>
  );
}

function ImpactSection() {
  return (
    <SectionShell
      title="See which clients a change would break."
      lead="Validation runs against the set of operations your client versions have published to that environment. Each client gets its own result: a change that is safe for web can still break mobile, and you see that before you merge."
      artifact={<ClientImpactMatrix />}
    />
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: environment validation workflow                            */
/* ------------------------------------------------------------------ */

interface GateNodeProps {
  readonly label: string;
  readonly state: string;
  readonly stateClassName?: string;
  readonly icon?: ReactNode;
  readonly tone: "passed" | "future";
}

function GateNode({
  label,
  state,
  stateClassName = "",
  icon,
  tone,
}: GateNodeProps) {
  const tones = {
    passed: "border-cc-success/40 bg-cc-success/[0.06] text-cc-success",
    future: "border-cc-card-border bg-cc-hover/40 text-cc-ink-dim opacity-70",
  } as const;
  return (
    <div className={`flex-1 rounded-lg border ${tones[tone]} px-4 py-4`}>
      <div className="flex items-center gap-2 font-mono text-[0.85rem] font-semibold">
        {icon !== undefined && (
          <span className="flex h-4 w-4 items-center justify-center">
            {icon}
          </span>
        )}
        {label}
      </div>
      <div
        className={`text-cc-ink-dim mt-0.5 font-mono text-[0.64rem] ${stateClassName}`}
      >
        {state}
      </div>
    </div>
  );
}

interface ConnectorProps {
  readonly muted?: boolean;
}

function Connector({ muted = false }: ConnectorProps) {
  return (
    <div className="flex items-center justify-center" aria-hidden>
      <svg
        viewBox="0 0 40 24"
        width={40}
        height={24}
        className={`rotate-90 sm:rotate-0 ${muted ? "text-cc-nav-label/30" : "text-cc-nav-label/60"}`}
      >
        <path
          d="M2 12 H32"
          stroke="currentColor"
          strokeWidth="1.5"
          fill="none"
          strokeDasharray={muted ? "3 3" : undefined}
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

function StagingGateNode() {
  return (
    <div className="relative flex-1">
      <div
        className="border-cc-warning/40 bg-cc-warning/[0.06] text-cc-warning rounded-lg border px-4 py-4 motion-safe:animate-[staging-validating_6s_ease-in-out_infinite] motion-reduce:hidden"
        aria-hidden="true"
      >
        <div className="flex items-center gap-2 font-mono text-[0.85rem] font-semibold">
          <span className="flex h-4 w-4 items-center justify-center">
            <SpinnerGlyph />
          </span>
          Staging
        </div>
        <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.64rem]">
          Validating
        </div>
      </div>
      <div className="border-cc-danger/40 bg-cc-danger/[0.06] text-cc-danger absolute inset-0 rounded-lg border px-4 py-4 opacity-0 motion-safe:animate-[staging-failed_6s_ease-in-out_infinite] motion-reduce:opacity-100">
        <div className="flex items-center gap-2 font-mono text-[0.85rem] font-semibold">
          <span className="flex h-4 w-4 items-center justify-center">
            <CrossGlyph />
          </span>
          Staging
        </div>
        <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.64rem]">
          Failed
        </div>
      </div>
    </div>
  );
}

function GateSchematic() {
  return (
    <div
      className="border-cc-card-border relative overflow-hidden rounded-xl border p-6 sm:p-8"
      style={{
        backgroundColor: GUARDRAIL,
        backgroundImage: `linear-gradient(${GUARDRAIL_LINE} 1px, transparent 1px), linear-gradient(90deg, ${GUARDRAIL_LINE} 1px, transparent 1px)`,
        backgroundSize: "26px 26px",
      }}
    >
      <style>{`
        @keyframes staging-validating {
          0%, 45% { opacity: 1; }
          55%, 100% { opacity: 0; }
        }
        @keyframes staging-failed {
          0%, 45% { opacity: 0; }
          55%, 100% { opacity: 1; }
        }
      `}</style>
      <div className="flex flex-col items-stretch gap-3 sm:flex-row sm:items-center">
        <GateNode
          label="Development"
          state="Passed"
          icon={<CheckIcon size={14} />}
          tone="passed"
        />
        <Connector />
        <StagingGateNode />
        <Connector muted />
        <GateNode
          label="Production"
          state="Future"
          stateClassName="invisible"
          tone="future"
        />
      </div>
    </div>
  );
}

function GateSection() {
  return (
    <SectionShell
      title="Every environment is its own gate."
      lead="Development, staging, and production each hold their own published operations, so the same change is validated against what actually runs in each of them. A change that passes staging can still fail production, because different client versions are published there."
      artifact={<GateSchematic />}
      flip
    />
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: CI marketplace integrations                                */
/* ------------------------------------------------------------------ */

/** House focus ring (design-system Dropdown idiom). */
const CARD_FOCUS_CLASSES =
  "focus-visible:ring-cc-accent/30 focus-visible:ring-2 focus-visible:outline-hidden";

interface PipelineCardSpec {
  readonly kicker: string;
  readonly file: string;
  readonly code: ReactNode;
  readonly status: string;
  readonly href: string;
  readonly action: string;
}

const PIPELINE_CARDS: readonly PipelineCardSpec[] = [
  {
    kicker: "GitHub Actions",
    file: ".github/workflows/ci.yml",
    code: (
      <>
        {tk.punc("- ")}
        {tk.kw("uses")}
        {tk.punc(": ")}ChilliCream/nitro-schema-validate{tk.ty("@v16")}
        {"\n"}
        {"  "}
        {tk.kw("with")}
        {tk.punc(":")}
        {"\n"}
        {"    "}
        {tk.kw("api-id")}
        {tk.punc(": ")}
        {tk.dir("${{ vars.NITRO_API_ID }}")}
        {"\n"}
        {"    "}
        {tk.kw("api-key")}
        {tk.punc(": ")}
        {tk.dir("${{ secrets.NITRO_API_KEY }}")}
        {"\n"}
        {"    "}
        {tk.kw("schema-file")}
        {tk.punc(": ")}./schema.graphql
        {"\n"}
        {"    "}
        {tk.kw("stage")}
        {tk.punc(": ")}
        {tk.ty("production")}
        {"\n"}
        {"    "}
        {tk.kw("comment-mode")}
        {tk.punc(": ")}review
      </>
    ),
    status: "✓ check passed",
    href: "https://github.com/marketplace?query=nitro",
    action: "GitHub Marketplace",
  },
  {
    kicker: "Azure Pipelines",
    file: "azure-pipelines.yml",
    code: (
      <>
        {tk.punc("- ")}
        {tk.kw("task")}
        {tk.punc(": ")}NitroSchemaValidate{tk.ty("@16")}
        {"\n"}
        {"  "}
        {tk.kw("inputs")}
        {tk.punc(":")}
        {"\n"}
        {"    "}
        {tk.kw("authenticationType")}
        {tk.punc(": ")}serviceConnection
        {"\n"}
        {"    "}
        {tk.kw("nitroServiceConnection")}
        {tk.punc(": ")}nitro-prod
        {"\n"}
        {"    "}
        {tk.kw("apiId")}
        {tk.punc(": ")}
        {tk.dir("$(NITRO_API_ID)")}
        {"\n"}
        {"    "}
        {tk.kw("schemaFile")}
        {tk.punc(": ")}./schema.graphql
        {"\n"}
        {"    "}
        {tk.kw("stage")}
        {tk.punc(": ")}
        {tk.ty("production")}
      </>
    ),
    status: "✓ task succeeded",
    href: "https://marketplace.visualstudio.com/items?itemName=ChilliCream.nitro-azure-pipelines-tasks",
    action: "Visual Studio Marketplace",
  },
  {
    kicker: "Any other CI",
    file: "shell",
    code: (
      <>
        {tk.punc("$ ")}
        {tk.fld("nitro schema validate")} {tk.punc("\\")}
        {"\n"}
        {"    "}
        {tk.kw("--api-id")} {tk.dir("$NITRO_API_ID")} {tk.punc("\\")}
        {"\n"}
        {"    "}
        {tk.kw("--schema-file")} schema.graphql {tk.punc("\\")}
        {"\n"}
        {"    "}
        {tk.kw("--stage")} {tk.ty("production")}
        {"\n"}
        {"\n"}
        {tk.punc("validating against production…")}
        {"\n"}
        <span className="text-cc-success">✓ no breaking changes</span>
      </>
    ),
    status: "✓ exit 0",
    href: "/docs/nitro/cli/schema",
    action: "CLI reference",
  },
];

function PipelineCard({
  kicker,
  file,
  code,
  status,
  href,
  action,
}: PipelineCardSpec) {
  const external = !href.startsWith("/");
  const linkClassName = `group block min-w-0 rounded-xl no-underline transition-transform duration-200 hover:-translate-y-1 ${CARD_FOCUS_CLASSES}`;
  const card = (
    <AppWindow
      disclosure={kicker}
      title={<span className="text-cc-prose">{file}</span>}
      footer={
        <div className="flex items-center justify-between gap-2">
          <span className="text-cc-success font-mono text-[0.66rem]">
            {status}
          </span>
          <span className="text-cc-accent group-hover:text-cc-accent-hover inline-flex items-center gap-1.5 text-sm font-medium transition-colors">
            {action}
            <ArrowRightIcon className="size-3.5 transition-transform group-hover:translate-x-0.5" />
          </span>
        </div>
      }
    >
      <div className="text-cc-prose overflow-x-auto px-4 py-4 font-mono text-[0.7rem] leading-relaxed whitespace-pre">
        {code}
      </div>
    </AppWindow>
  );
  return external ? (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={linkClassName}
    >
      {card}
    </a>
  ) : (
    <NextLink href={href} className={linkClassName}>
      {card}
    </NextLink>
  );
}

function PipelineSection() {
  return (
    <section aria-labelledby="pipelines-title">
      <SectionHeading
        titleId="pipelines-title"
        title="Run the checks in the CI you already have."
        description="The validate, upload, and publish steps ship as ready-made GitHub Actions and Azure Pipelines tasks, both wrapping the Nitro CLI."
      />
      <div className="mt-10 grid items-start gap-5 lg:grid-cols-3">
        {PIPELINE_CARDS.map((card) => (
          <PipelineCard key={card.kicker} {...card} />
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: schema version timeline with breaking-change markers       */
/* ------------------------------------------------------------------ */

interface VersionPoint {
  readonly v: string;
  readonly note: string;
  readonly status: ChangeStatus;
}

const VERSIONS: readonly VersionPoint[] = [
  { v: "v12", note: "add Cart.discount", status: "safe" },
  { v: "v13", note: "deprecate Order.placedAt", status: "dangerous" },
  { v: "v14", note: "remove Order.total — blocked", status: "breaking" },
  { v: "v14", note: "add Order.totalAmount", status: "safe" },
  { v: "v15", note: "drop Order.total — usage cleared", status: "dangerous" },
];

function VersionTimeline() {
  return (
    <AppWindow title={<span className="text-cc-prose">schema history</span>}>
      <div className="px-5 py-6">
        <div className="relative">
          <span
            aria-hidden
            className="bg-cc-card-border absolute top-2.5 left-2 h-[calc(100%-1.25rem)] w-px"
          />
          <ol className="space-y-5">
            {VERSIONS.map((p, i) => {
              const meta = STATUS_META[p.status];
              return (
                <li key={i} className="relative flex items-center gap-4 pl-7">
                  <span
                    className={`absolute left-0 flex h-4 w-4 items-center justify-center rounded-full ${meta.bg} ring-1 ring-inset ${meta.ring}`}
                  >
                    <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
                  </span>
                  <span className="text-cc-heading w-10 shrink-0 font-mono text-[0.74rem] font-semibold">
                    {p.v}
                  </span>
                  <span className="text-cc-prose min-w-0 flex-1 truncate font-mono text-[0.74rem]">
                    {p.note}
                  </span>
                  <StatusChip status={p.status} />
                </li>
              );
            })}
          </ol>
        </div>
      </div>
    </AppWindow>
  );
}

function TimelineSection() {
  return (
    <SectionShell
      title="Keep the full history of your schema."
      lead="Every upload and publish adds a version to the registry, giving you a browsable record of how the API evolved: what changed in each version, how severe it was, and when a blocked removal finally cleared. Answering 'when did this field change and why' no longer means digging through merge commits."
      artifact={<VersionTimeline />}
    />
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: closing CTA pair                                           */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <DotGridSurface className="rounded-3xl">
      <NextStepsSection
        skin="accent"
        className=""
        title="Know what breaks before your users do."
        text="Publish the operations each client uses, validate proposed schemas against the environment you plan to update, and merge with the answer in hand."
        primaryLink="https://nitro.chillicream.com"
        primaryLinkText="Start for free"
        secondaryLink="/docs/nitro/apis/client-registry"
        secondaryLinkText="Read client registry docs"
      />
    </DotGridSurface>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

const BREADCRUMB_DATA = {
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  itemListElement: [
    {
      "@type": "ListItem",
      position: 1,
      name: "Home",
      item: `${SITE_URL}/`,
    },
    {
      "@type": "ListItem",
      position: 2,
      name: "Platform",
      item: `${SITE_URL}/platform`,
    },
    {
      "@type": "ListItem",
      position: 3,
      name: "Release Safety",
    },
  ],
};

export default function ReleaseSafetyPage() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(BREADCRUMB_DATA) }}
      />
      <HeroSection />
      <CheckCardSection />
      <ImpactSection />
      <GateSection />
      <PipelineSection />
      <TimelineSection />
      <ClosingCta />
    </div>
  );
}
