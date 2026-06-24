import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Release Safety for GraphQL Schemas | ChilliCream",
  description:
    "Stamp every schema change safe, dangerous, or breaking. Pin a review thread on the line that breaks and let CI gates stop unsafe GraphQL releases before any published client ships against them.",
  keywords: [
    "GraphQL release safety",
    "schema diff review",
    "breaking change detection",
    "schema registry",
    "client registry",
    "validate publish gate",
    "CI schema checks",
    "published clients affected",
    "schema version timeline",
    "GraphQL contract review",
  ],
  openGraph: {
    title: "Change Contracts With a Safety Net",
    description:
      "A status-stamped schema diff, a pinned review thread on the breaking line, and a CI gate that stops unsafe GraphQL releases before consumers discover them.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Scene tokens                                                        */
/* This page is status-driven on a guardrail-blue surface:             */
/*   cc-success = safe, cc-warning = dangerous, cc-danger = breaking.   */
/* ------------------------------------------------------------------ */

const GUARDRAIL = "#0a1426";
const GUARDRAIL_RAISED = "rgba(13, 27, 48, 0.78)";
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

interface SafeWordProps {
  readonly status: ChangeStatus;
  readonly children: ReactNode;
}

function SafeWord({ status, children }: SafeWordProps) {
  return (
    <span className={`font-medium ${STATUS_META[status].text}`}>
      {children}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Window chrome wrapper                                               */
/* ------------------------------------------------------------------ */

interface AppWindowProps {
  readonly title: ReactNode;
  readonly tab?: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
  readonly className?: string;
}

function AppWindow({
  title,
  tab,
  children,
  footer,
  className = "",
}: AppWindowProps) {
  return (
    <div
      className={`border-cc-card-border overflow-hidden rounded-xl border shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)] backdrop-blur-md ${className}`}
      style={{ backgroundColor: GUARDRAIL }}
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
      <span className="border-cc-card-border text-cc-nav-label/70 w-9 shrink-0 border-r py-1 pr-2 text-right font-mono text-[0.66rem] select-none">
        {line.old ?? ""}
      </span>
      <span className="border-cc-card-border text-cc-nav-label/70 w-9 shrink-0 border-r py-1 pr-2 text-right font-mono text-[0.66rem] select-none">
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
      {line.status !== undefined && (
        <span className="flex shrink-0 items-center py-1 pr-3">
          <StatusChip status={line.status} />
        </span>
      )}
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
          <span className="text-cc-nav-label ml-auto font-mono text-[0.62rem]">
            line 43
          </span>
        </div>
        <p className="text-cc-ink-dim mt-2 text-[0.78rem] leading-relaxed">
          Removing{" "}
          <code className="bg-cc-hover text-cc-prose rounded px-1 font-mono text-[0.72rem]">
            Order.total
          </code>{" "}
          breaks queries that still select it.{" "}
          <span className="text-cc-prose">3 published clients affected.</span>{" "}
          Deprecate it for one release, then drop it once usage clears.
        </p>
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <span className="bg-cc-danger/15 text-cc-danger ring-cc-danger/30 rounded-md px-2.5 py-1 font-mono text-[0.64rem] font-semibold ring-1 ring-inset">
            Resolve
          </span>
          <span className="text-cc-ink-dim ring-cc-card-border rounded-md px-2.5 py-1 font-mono text-[0.64rem] ring-1 ring-inset">
            Reply
          </span>
          <span className="text-cc-ink-dim ring-cc-card-border rounded-md px-2.5 py-1 font-mono text-[0.64rem] ring-1 ring-inset">
            Suggest deprecation
          </span>
        </div>
      </div>
    </div>
  );
}

function HeroDiffMock() {
  return (
    <AppWindow
      title={
        <>
          <span className="text-cc-prose">schema.graphql</span>
          <span className="text-cc-nav-label">·</span>
          <span>orders-api</span>
        </>
      }
      tab="diff · main…release"
      footer={
        <div className="flex items-center justify-between">
          <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
            <span className="bg-cc-danger h-2 w-2 rounded-full" />
            registry check failed
          </span>
          <span className="text-cc-nav-label font-mono text-[0.66rem]">
            1 breaking · 1 dangerous · 1 safe
          </span>
        </div>
      }
    >
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
    </AppWindow>
  );
}

function HeroSection() {
  const facts = [
    { k: "classify", v: "validate → publish" },
    { k: "gate", v: "CI blocks unsafe" },
    { k: "feedback", v: "in your build" },
  ];
  return (
    <section className="grid items-center gap-12 lg:grid-cols-[minmax(0,0.92fr)_minmax(0,1.08fr)]">
      <div>
        <Eyebrow>Platform · Release Safety</Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-bold tracking-tight">
          Change contracts
          <br />
          with a safety net.
        </h1>
        <p className="lead text-cc-ink-dim mt-6 max-w-xl">
          Ship schema changes without breaking the apps that depend on them.
        </p>
        <p className="text-body text-cc-prose mt-5 max-w-xl leading-relaxed">
          Every edit is classified <SafeWord status="safe">safe</SafeWord>,{" "}
          <SafeWord status="dangerous">dangerous</SafeWord>, or{" "}
          <SafeWord status="breaking">breaking</SafeWord>, validated against the
          clients you have actually published, and only then promoted. Unsafe
          releases stop at the gate, before a consumer ever discovers them.
        </p>
        <div className="mt-9 flex flex-wrap items-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/continuous-integration">
            Read the Docs
          </OutlineButton>
        </div>
        <dl className="border-cc-card-border mt-10 grid max-w-md grid-cols-3 gap-px overflow-hidden rounded-lg border">
          {facts.map((s) => (
            <div
              key={s.k}
              className="px-3 py-3"
              style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
            >
              <dt className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.16em] uppercase">
                {s.k}
              </dt>
              <dd className="text-cc-prose mt-1 font-mono text-[0.72rem]">
                {s.v}
              </dd>
            </div>
          ))}
        </dl>
      </div>
      <div className="relative">
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
    <svg viewBox="0 0 16 16" width={12} height={12} aria-hidden>
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

interface CheckRowProps {
  readonly icon: "fail" | "pass" | "run";
  readonly name: string;
  readonly detail: string;
}

function CheckRow({ icon, name, detail }: CheckRowProps) {
  const map = {
    fail: { node: <CrossGlyph />, color: "text-cc-danger" },
    pass: { node: <CheckIcon size={12} />, color: "text-cc-success" },
    run: { node: <SpinnerGlyph />, color: "text-cc-warning" },
  } as const;
  const m = map[icon];
  return (
    <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3 last:border-b-0">
      <span className={`flex h-5 w-5 items-center justify-center ${m.color}`}>
        {m.node}
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
      title={
        <>
          <span>orders-api</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">#482 Add Money type</span>
        </>
      }
      tab="checks"
      footer={
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-cc-ink-dim font-mono text-[0.66rem]">
            Merging is blocked until checks pass.
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
      />
      <CheckRow
        icon="pass"
        name="Schema validation — additive"
        detail="Money, totalAmount added"
      />
      <CheckRow
        icon="run"
        name="Client compatibility — partner app"
        detail="validating…"
      />
    </AppWindow>
  );
}

function CheckCardSection() {
  return (
    <SectionShell
      eyebrow="The gate"
      title="A failing check is the whole point."
      lead="The registry check runs on every pull request. If a change would break a published client, the check fails and merge is blocked, so the conversation happens in review instead of in production."
      bullets={[
        "Breaking changes fail the check and block the merge.",
        "Additive, safe changes pass without ceremony.",
        "Re-run after a fix; the gate re-validates against published clients.",
      ]}
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
  readonly status: "ok" | "risk" | "queued";
}

const CLIENT_ROWS: readonly ClientRow[] = [
  { name: "web", env: "production", ok: 5, total: 5, status: "ok" },
  { name: "mobile", env: "production", ok: 3, total: 5, status: "risk" },
  { name: "partner", env: "sandbox", ok: 0, total: 4, status: "queued" },
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
        : "bg-cc-nav-label/50";
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
    queued: { text: "queued", cls: "text-cc-nav-label" },
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
      tab="published clients affected"
    >
      <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[1.3fr_1fr_0.8fr] gap-3 border-b px-4 py-2 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
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
              <div className="text-cc-nav-label font-mono text-[0.62rem]">
                {c.env}
              </div>
            </div>
            <div className="flex items-center gap-3">
              <ImpactBar ok={c.ok} total={c.total} status={c.status} />
              <span className="text-cc-ink-dim font-mono text-[0.68rem]">
                {c.ok}/{c.total}
              </span>
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
      eyebrow="Blast radius"
      title="Know who a change touches."
      lead="The client registry tracks the operations your published clients actually run. Before a change ships, you can see which clients are clear and which would have operations break, by name and by environment."
      bullets={[
        "Validation runs against the operations published clients send.",
        "Each client reports passing operations, not a vague global verdict.",
        "Queued clients are validated as soon as their operations are registered.",
      ]}
      artifact={<ClientImpactMatrix />}
    />
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: validate -> publish gate schematic on blueprint grid       */
/* ------------------------------------------------------------------ */

interface GateNodeProps {
  readonly kicker: string;
  readonly label: string;
  readonly sub: string;
  readonly tone: "neutral" | "warning" | "success" | "danger";
  readonly compact?: boolean;
}

function GateNode({
  kicker,
  label,
  sub,
  tone,
  compact = false,
}: GateNodeProps) {
  const tones = {
    neutral: "border-cc-card-border text-cc-heading",
    warning: "border-cc-warning/40 text-cc-warning",
    success: "border-cc-success/40 text-cc-success",
    danger: "border-cc-danger/40 text-cc-danger",
  } as const;
  return (
    <div
      className={`flex-1 rounded-lg border ${tones[tone]} px-4 ${compact ? "py-2.5" : "py-4"}`}
      style={{ backgroundColor: GUARDRAIL_RAISED }}
    >
      <div className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] uppercase">
        {kicker}
      </div>
      <div className="mt-1 font-mono text-[0.85rem] font-semibold">{label}</div>
      <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.64rem]">
        {sub}
      </div>
    </div>
  );
}

interface ConnectorProps {
  readonly branch?: boolean;
}

function Connector({ branch = false }: ConnectorProps) {
  return (
    <div className="flex items-center justify-center" aria-hidden>
      <svg
        viewBox="0 0 40 24"
        width={40}
        height={24}
        className="text-cc-nav-label/60 rotate-90 sm:rotate-0"
      >
        {branch ? (
          <>
            <path
              d="M2 12 H20"
              stroke="currentColor"
              strokeWidth="1.5"
              fill="none"
            />
            <path
              d="M20 12 C30 12 28 4 38 4"
              stroke="currentColor"
              strokeWidth="1.5"
              fill="none"
            />
            <path
              d="M20 12 C30 12 28 20 38 20"
              stroke="currentColor"
              strokeWidth="1.5"
              fill="none"
            />
          </>
        ) : (
          <>
            <path
              d="M2 12 H32"
              stroke="currentColor"
              strokeWidth="1.5"
              fill="none"
            />
            <path
              d="M30 7 L38 12 L30 17"
              stroke="currentColor"
              strokeWidth="1.5"
              fill="none"
            />
          </>
        )}
      </svg>
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
      <div className="flex flex-col items-stretch gap-3 sm:flex-row sm:items-center">
        <GateNode
          kicker="01"
          label="schema change"
          sub="pull request"
          tone="neutral"
        />
        <Connector />
        <GateNode
          kicker="02"
          label="validate"
          sub="classify + check clients"
          tone="warning"
        />
        <Connector branch />
        <div className="flex flex-1 flex-col gap-3">
          <GateNode
            kicker="03a"
            label="publish"
            sub="safe → promoted"
            tone="success"
            compact
          />
          <GateNode
            kicker="03b"
            label="blocked"
            sub="breaking → stop"
            tone="danger"
            compact
          />
        </div>
      </div>
      <p className="text-cc-ink-dim mt-6 max-w-2xl font-mono text-[0.72rem] leading-relaxed">
        Nothing reaches <span className="text-cc-prose">publish</span> until it
        clears <span className="text-cc-warning">validate</span>. A breaking
        change never advances; it stops at the gate with the reason attached.
      </p>
    </div>
  );
}

function GateSection() {
  return (
    <SectionShell
      eyebrow="The flow"
      title="Validate, then publish. Never the other way around."
      lead="Release safety is a two-stage gate. A change is classified and checked against published clients first; only changes that clear validation are promoted. The order is the guarantee."
      bullets={[
        "Validate classifies the change and tests it against real clients.",
        "Publish only ever runs on a change that already cleared validation.",
        "A blocked change carries its reason, not just a red X.",
      ]}
      artifact={<GateSchematic />}
      flip
    />
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
    <AppWindow
      title={
        <>
          <span>orders-api</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">schema history</span>
        </>
      }
      tab="registry"
    >
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
        <p className="text-cc-ink-dim mt-6 font-mono text-[0.7rem] leading-relaxed">
          The breaking removal at <span className="text-cc-danger">v14</span>{" "}
          never shipped. It was re-shaped as an additive change, deprecated, and
          only dropped at <span className="text-cc-warning">v15</span> once
          client usage cleared.
        </p>
      </div>
    </AppWindow>
  );
}

function TimelineSection() {
  return (
    <SectionShell
      eyebrow="The record"
      title="Every version, every classification, kept."
      lead="The registry keeps the full history of your schema with each change classified in place. You can see exactly when a contract shifted, why it was safe, and how a risky change was reshaped before it shipped."
      bullets={[
        "Each published version records its classification.",
        "Deprecations buy time; removals wait for usage to clear.",
        "Drift between code and the published contract shows up as build feedback.",
      ]}
      artifact={<VersionTimeline />}
    />
  );
}

/* ------------------------------------------------------------------ */
/* Reusable two-column section shell                                   */
/* ------------------------------------------------------------------ */

interface SectionShellProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly lead: string;
  readonly bullets: readonly string[];
  readonly artifact: ReactNode;
  readonly flip?: boolean;
}

function SectionShell({
  eyebrow,
  title,
  lead,
  bullets,
  artifact,
  flip = false,
}: SectionShellProps) {
  return (
    <section className="grid items-center gap-10 lg:grid-cols-2 lg:gap-14">
      <div className={flip ? "lg:order-2" : ""}>
        <Eyebrow>{eyebrow}</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
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
/* SECTION: big-number reliability band                                */
/* ------------------------------------------------------------------ */

function ReliabilityBand() {
  const stats = [
    { n: "0", l: "breaking changes shipped", c: "text-cc-success" },
    { n: "12", l: "published clients guarded", c: "text-cc-heading" },
    { n: "100%", l: "of releases checked", c: "text-cc-heading" },
  ];
  return (
    <section
      className="border-cc-card-border overflow-hidden rounded-2xl border"
      style={{
        backgroundColor: GUARDRAIL,
        backgroundImage:
          "radial-gradient(80% 120% at 50% -20%, rgba(124,146,198,0.12), transparent 60%)",
      }}
    >
      <div className="divide-cc-card-border grid divide-y sm:grid-cols-3 sm:divide-x sm:divide-y-0">
        {stats.map((s) => (
          <div key={s.l} className="px-6 py-9 text-center">
            <div
              className={`font-heading text-h2 font-bold tracking-tight ${s.c}`}
            >
              {s.n}
            </div>
            <div className="text-cc-nav-label mt-2 font-mono text-[0.66rem] tracking-[0.14em] uppercase">
              {s.l}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: honesty / credibility beat                                 */
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

function HonestySection() {
  return (
    <section
      className="border-cc-card-border rounded-2xl border px-6 py-9 sm:px-10 sm:py-11"
      style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
    >
      <div className="max-w-3xl">
        <Eyebrow>What this does and does not promise</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          A safety net, not a blindfold.
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">
          A check is only useful if you can trust what it claims. Release safety
          tells you which <em>published</em> clients are affected by a change,
          based on the operations they have registered. It is honest about its
          edges.
        </p>
        <ul className="mt-7 grid gap-4 sm:grid-cols-2">
          <HonestyItem
            tone="success"
            head="What it stops"
            body="Releases that would break a published client are blocked before merge, with the breaking line and reason in hand."
          />
          <HonestyItem
            tone="warning"
            head="What it needs"
            body="A client is only guarded once its operations are registered. Unregistered traffic is outside the net."
          />
          <HonestyItem
            tone="success"
            head="What it surfaces"
            body="Strawberry Shake regenerates clients via MSBuild codegen, so contract drift shows up as build feedback you cannot miss."
          />
          <HonestyItem
            tone="warning"
            head="What it will not pretend"
            body="It reports published clients affected. It does not claim certainty about consumers it has never seen."
          />
        </ul>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION: closing CTA pair                                           */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="border-cc-card-border relative overflow-hidden rounded-2xl border px-6 py-14 text-center sm:px-12">
      <div
        aria-hidden
        className="absolute inset-0 -z-10"
        style={{
          backgroundColor: GUARDRAIL,
          backgroundImage:
            "radial-gradient(70% 100% at 50% 0%, rgba(124,146,198,0.16), transparent 65%)",
        }}
      />
      <h2 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
        Put a safety net under every schema change.
      </h2>
      <p className="text-body text-cc-prose mx-auto mt-5 max-w-xl leading-relaxed">
        Classify, validate, and gate your releases so breaking changes stop at
        the door, not in your users&rsquo; hands.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/platform/continuous-integration">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export default function ReleaseSafetyPreviewV1() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <CheckCardSection />
      <ImpactSection />
      <GateSection />
      <TimelineSection />
      <ReliabilityBand />
      <HonestySection />
      <ClosingCta />
    </div>
  );
}
