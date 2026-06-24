import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Safe GraphQL Schema Evolution | ChilliCream Platform",
  description:
    "A registry that classifies every GraphQL schema change, validates it against your published clients, and gates unsafe releases before they ship.",
  keywords: [
    "safe GraphQL schema evolution",
    "GraphQL schema registry",
    "breaking change classification",
    "schema diff review",
    "CI schema checks",
    "published clients affected",
    "schema version timeline",
    "validate publish gate",
    "GraphQL contract review",
  ],
  openGraph: {
    title: "Safe GraphQL Schema Evolution",
    description:
      "A registry that classifies each schema change, validates it against the clients you have actually published, and gates unsafe GraphQL releases.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Scene tokens                                                        */
/* The Confession: a single centered column running down the page,    */
/* with one continuous 1px rail anchored to the narrow column edge    */
/* and status dots pinned to the rail at every beat. Coral (#f0786a)  */
/* is the page's one brand accent.                                    */
/* ------------------------------------------------------------------ */

const CORAL = "#f0786a";

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
  readonly className?: string;
}

function Eyebrow({ children, className = "" }: EyebrowProps) {
  return (
    <span
      className={`text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase ${className}`}
    >
      {children}
    </span>
  );
}

interface CoralRuleProps {
  readonly className?: string;
}

function CoralRule({ className = "" }: CoralRuleProps) {
  return (
    <span
      aria-hidden
      className={`block h-px w-10 ${className}`}
      style={{ backgroundColor: CORAL }}
    />
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
/* Beat: one story beat. Every beat is the same width (max-w-2xl).    */
/* Wide artifacts use the InColumnWide wrapper to overflow the prose  */
/* column without widening the beat itself, so the rail stays put.   */
/* The rail and dots are rendered ONCE at page level.                 */
/* ------------------------------------------------------------------ */

type DotKind = ChangeStatus | "coral";

interface BeatProps {
  readonly children: ReactNode;
}

function Beat({ children }: BeatProps) {
  return (
    <div className="relative mx-auto w-full max-w-2xl pt-32 first:pt-0">
      <div className="pl-8 sm:pl-10">{children}</div>
    </div>
  );
}

/* A wide inline artifact that overflows the prose column on both sides
   without changing the beat's left edge (so the rail stays still). */
interface InColumnWideProps {
  readonly children: ReactNode;
}

function InColumnWide({ children }: InColumnWideProps) {
  return (
    <div className="relative -mr-[5rem] -ml-[2rem] sm:-mr-[6rem] sm:-ml-[2rem]">
      {children}
    </div>
  );
}

/* The single continuous rail and the dots. Rendered once at page level,
   positioned absolutely against the page wrapper. Dots are placed by
   percent down the wrapper at a single fixed x (left of the narrow
   column's content area). */
interface RailDot {
  readonly top: string;
  readonly kind: DotKind;
}

interface PageRailProps {
  readonly dots: readonly RailDot[];
  readonly railTop: string;
  readonly railBottom: string;
}

function PageRail({ dots, railTop, railBottom }: PageRailProps) {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0 mx-auto w-full max-w-2xl"
    >
      <div className="relative h-full pl-8 sm:pl-10">
        <span
          className="bg-cc-card-border absolute w-px"
          style={{ left: 0, top: railTop, bottom: railBottom }}
        />
        {dots.map((d, i) => {
          const meta = d.kind === "coral" ? null : STATUS_META[d.kind];
          return (
            <span
              key={i}
              className="absolute -translate-x-1/2"
              style={{ left: 0, top: d.top }}
            >
              {d.kind === "coral" ? (
                <span
                  className="block h-2.5 w-2.5 rounded-full"
                  style={{
                    backgroundColor: CORAL,
                    boxShadow: "0 0 0 4px rgba(11,15,26,1)",
                  }}
                />
              ) : (
                <span
                  className={`block h-2.5 w-2.5 rounded-full ${meta!.dot}`}
                  style={{ boxShadow: "0 0 0 4px rgba(11,15,26,1)" }}
                />
              )}
            </span>
          );
        })}
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Window chrome for inline artifacts                                  */
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
      className={`border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)] ${className}`}
    >
      <div className="border-cc-card-border bg-cc-card-bg flex items-center gap-2 border-b px-4 py-2.5">
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
        <div className="border-cc-card-border bg-cc-card-bg border-t px-4 py-2.5">
          {footer}
        </div>
      )}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Diff tokens and rows                                                */
/* ------------------------------------------------------------------ */

type DiffSign = "+" | "-" | " ";

interface DiffLine {
  readonly old: number | null;
  readonly nw: number | null;
  readonly sign: DiffSign;
  readonly code: ReactNode;
  readonly status?: ChangeStatus;
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
  punc: (s: string) => <span className="text-cc-ink-dim">{s}</span>,
  attr: (s: string) => <span className="text-cc-warning">{s}</span>,
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

/* The diff shows three classified lines so the gate tally and the
   artifact agree: one breaking removal, one dangerous deprecation,
   one safe addition. */
const BROKEN_LINE_DIFF: readonly DiffLine[] = [
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
  },
  {
    old: null,
    nw: 43,
    sign: "+",
    code: (
      <>
        {"  "}
        {tk.fld("placedAt")}
        {tk.punc(": ")}
        {tk.ty("DateTime!")} {tk.attr("@deprecated")}
      </>
    ),
    status: "dangerous",
  },
  {
    old: null,
    nw: 44,
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
    old: 44,
    nw: 45,
    sign: " ",
    code: <>{tk.punc("}")}</>,
  },
];

function BrokenLineDiff() {
  return (
    <AppWindow
      title={
        <>
          <span className="text-cc-prose">schema.graphql</span>
          <span className="text-cc-nav-label">·</span>
          <span>orders-api</span>
        </>
      }
      tab="the line that broke"
      footer={
        <div className="flex items-center justify-between">
          <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
            <span className="bg-cc-danger h-2 w-2 rounded-full" />
            removed without a deprecation
          </span>
          <span className="text-cc-nav-label font-mono text-[0.66rem]">
            1 breaking, 1 dangerous, 1 safe
          </span>
        </div>
      }
    >
      <div className="bg-cc-danger/[0.04] text-cc-ink-dim px-4 py-1.5 font-mono text-[0.64rem]">
        @@ type Order @@
      </div>
      <div>
        {BROKEN_LINE_DIFF.map((line, i) => (
          <DiffRow key={i} line={line} />
        ))}
      </div>
    </AppWindow>
  );
}

/* ------------------------------------------------------------------ */
/* PR check artifact                                                   */
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

interface CheckSubRowProps {
  readonly icon: "fail" | "pass" | "run";
  readonly name: string;
  readonly detail: string;
}

function CheckSubRow({ icon, name, detail }: CheckSubRowProps) {
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
          1 breaking, 1 dangerous, 1 safe
        </span>
      </div>
      <CheckSubRow
        icon="fail"
        name="Schema validation, breaking change"
        detail="Order.total removed"
      />
      <CheckSubRow
        icon="run"
        name="Client compatibility, partner app"
        detail="validating"
      />
    </AppWindow>
  );
}

/* ------------------------------------------------------------------ */
/* Inline client list (flowing rows, not a card)                       */
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
          className={`h-1.5 w-4 rounded-[2px] ${i < ok ? color : "bg-cc-ink-faint"}`}
        />
      ))}
    </span>
  );
}

function InlineClientList() {
  const statusLabel: Record<
    ClientRow["status"],
    { text: string; cls: string }
  > = {
    ok: { text: "ok", cls: "text-cc-success" },
    risk: { text: "at risk", cls: "text-cc-warning" },
    queued: { text: "queued", cls: "text-cc-nav-label" },
  };
  return (
    <div className="border-cc-card-border divide-cc-card-border divide-y rounded-lg border">
      {CLIENT_ROWS.map((c) => {
        const s = statusLabel[c.status];
        return (
          <div
            key={c.name}
            className="bg-cc-card-bg flex items-center gap-4 px-4 py-3"
          >
            <div className="min-w-0 flex-1">
              <div className="text-cc-heading truncate font-mono text-[0.78rem]">
                {c.name}
              </div>
              <div className="text-cc-nav-label font-mono text-[0.62rem]">
                {c.env}
              </div>
            </div>
            <ImpactBar ok={c.ok} total={c.total} status={c.status} />
            <span className="text-cc-ink-dim w-14 text-right font-mono text-[0.68rem]">
              {c.ok}/{c.total}
            </span>
            <span
              className={`w-20 text-right font-mono text-[0.7rem] font-semibold ${s.cls}`}
            >
              {s.text}
            </span>
          </div>
        );
      })}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Inline version log                                                  */
/* ------------------------------------------------------------------ */

interface VersionPoint {
  readonly v: string;
  readonly note: string;
  readonly status: ChangeStatus;
}

const VERSIONS: readonly VersionPoint[] = [
  { v: "v12", note: "add Cart.discount", status: "safe" },
  { v: "v13", note: "deprecate Order.placedAt", status: "dangerous" },
  { v: "v14", note: "remove Order.total, blocked", status: "breaking" },
  { v: "v14", note: "add Order.totalAmount", status: "safe" },
  { v: "v15", note: "drop Order.total, usage cleared", status: "dangerous" },
];

function InlineVersionLog() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-lg border">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2">
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
          orders-api · schema history
        </span>
        <span className="text-cc-nav-label font-mono text-[0.62rem]">
          the registry remembers
        </span>
      </div>
      <div className="relative px-5 py-5">
        <span
          aria-hidden
          className="bg-cc-card-border absolute top-6 left-7 h-[calc(100%-3rem)] w-px"
        />
        <ol className="space-y-4">
          {VERSIONS.map((p, i) => {
            const meta = STATUS_META[p.status];
            return (
              <li key={i} className="relative flex items-center gap-4 pl-6">
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
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

/* Dot positions are tuned to land near the top of each beat's content.
   They share a single x-coordinate (left: 0 of the max-w-2xl column),
   so the rail is one straight vertical line. */
const RAIL_DOTS: readonly RailDot[] = [
  { top: "0.5rem", kind: "breaking" }, // 1 opening confession
  { top: "calc(0.5rem + 30rem)", kind: "breaking" }, // 2 the line that broke
  { top: "calc(0.5rem + 60rem)", kind: "dangerous" }, // 3 classify
  { top: "calc(0.5rem + 96rem)", kind: "breaking" }, // 4 the gate
  { top: "calc(0.5rem + 132rem)", kind: "dangerous" }, // 5 published clients
  { top: "calc(0.5rem + 160rem)", kind: "safe" }, // 6 the record
  { top: "calc(0.5rem + 192rem)", kind: "coral" }, // 7 honest edges (editorial)
  { top: "calc(0.5rem + 222rem)", kind: "safe" }, // 8 reliability line
  { top: "calc(0.5rem + 252rem)", kind: "coral" }, // 9 closing CTA (rail end)
];

export default function ReleaseSafetyPreviewV5() {
  return (
    <div className="relative py-10">
      {/* One continuous rail and one set of dots, anchored to the narrow
          column edge so they sit at a single x-position the whole way down.
          The rail starts at the first dot and terminates exactly at the
          closing coral dot. */}
      <PageRail
        dots={RAIL_DOTS}
        railTop="0.5rem"
        railBottom="calc(100% - 252rem - 1.25rem)"
      />

      {/* 1. Opening confession (hero) */}
      <Beat>
        <Eyebrow>Platform / Release Safety</Eyebrow>
        <CoralRule className="mt-5" />
        <h1 className="font-heading text-cc-heading text-h1 mt-6 font-bold tracking-tight">
          One breaking change
          <br />
          is one
          <br />
          <span style={{ color: CORAL }}>too many.</span>
        </h1>
        <p className="text-cc-prose text-lead mt-8 max-w-xl">
          Before we built the registry, one Friday push removed a field three
          published clients still called. Now a registry classifies every schema
          edit, checks it against the clients we actually publish, and refuses
          to promote anything that would break them.
        </p>
        <div className="mt-9 flex flex-wrap items-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/continuous-integration">
            Read the Docs
          </OutlineButton>
        </div>
      </Beat>

      {/* 2. The line that broke (inline diff) */}
      <Beat>
        <Eyebrow>The line that broke</Eyebrow>
        <p className="text-cc-prose text-body mt-4 leading-relaxed">
          It was four characters short of being safe. A field renamed in code,
          removed from the schema, merged on a Friday. By Monday, three
          published clients could not place an order.
        </p>
        <div className="mt-10">
          <InColumnWide>
            <BrokenLineDiff />
          </InColumnWide>
        </div>
        <p className="text-cc-ink-dim mt-4 font-mono text-[0.72rem] leading-relaxed">
          Order.total, removed without a deprecation, alongside the safer shape
          that should have replaced it. Three published clients affected.
        </p>
      </Beat>

      {/* 3. Classify (chapter break) */}
      <Beat>
        <Eyebrow>Classify</Eyebrow>
        <p className="font-heading text-cc-heading text-h2 mt-6 text-center leading-tight font-semibold tracking-tight">
          Every change earns one of three words.
        </p>
        <div className="mt-12 space-y-7">
          <p className="text-cc-prose text-body leading-relaxed">
            <SafeWord status="safe">Safe.</SafeWord> Additive. A new field, a
            new type, a new optional argument. Existing operations keep working
            exactly as they did. The check passes without ceremony.
          </p>
          <p className="text-cc-prose text-body leading-relaxed">
            <SafeWord status="dangerous">Dangerous.</SafeWord> A deprecation, a
            relaxed nullability, a default that moved. Today it works, tomorrow
            it might not. The change ships with a chip, not a stop sign.
          </p>
          <p className="text-cc-prose text-body leading-relaxed">
            <SafeWord status="breaking">Breaking.</SafeWord> A removed field, a
            tightened type, a renamed argument. Some published client,
            somewhere, will get an error. The check fails. The merge stops.
          </p>
        </div>
      </Beat>

      {/* 4. The gate (inline check card) */}
      <Beat>
        <Eyebrow>The gate</Eyebrow>
        <p className="text-cc-prose text-body mt-4 leading-relaxed">
          The registry check runs on every pull request. If a change would break
          a published client, the check fails and the merge is blocked, so the
          conversation happens in review instead of in production.
        </p>
        <div className="mt-10">
          <InColumnWide>
            <CheckCard />
          </InColumnWide>
        </div>
        <p className="font-heading text-cc-heading text-h3 mt-12 text-center font-semibold tracking-tight">
          Merging is blocked until checks pass.
        </p>
      </Beat>

      {/* 5. Published clients affected */}
      <Beat>
        <Eyebrow>Published clients affected</Eyebrow>
        <p className="text-cc-prose text-body mt-4 leading-relaxed">
          The client registry tracks the operations your published clients
          actually run. Before a change ships, you see which clients are clear
          and which would have operations break, by name and by environment.
        </p>
        <div className="mt-10">
          <InlineClientList />
        </div>
      </Beat>

      {/* 6. The record (inline version log) */}
      <Beat>
        <Eyebrow>The record</Eyebrow>
        <p className="text-cc-prose text-body mt-4 leading-relaxed">
          Every published version is kept with its classification in place. You
          can see exactly when a contract shifted, why it was safe, and how a
          risky change was reshaped before it ever shipped.
        </p>
        <div className="mt-10">
          <InlineVersionLog />
        </div>
        <p className="text-cc-ink-dim mt-5 font-mono text-[0.72rem] leading-relaxed">
          The breaking removal at v14 never shipped. It was re-shaped as an
          additive change, deprecated, and only dropped at v15 once client usage
          cleared.
        </p>
      </Beat>

      {/* 7. Honest edges. The dot for this beat uses coral as an editorial
          aside, so the rail's legend (safe / dangerous / breaking) stays
          intact. */}
      <Beat>
        <Eyebrow>Honest edges</Eyebrow>
        <p className="font-heading text-cc-heading text-h3 mt-6 text-center font-semibold tracking-tight">
          A safety net, not a blindfold.
        </p>
        <div className="mt-12 space-y-9">
          <div>
            <Eyebrow>What it stops</Eyebrow>
            <p className="text-cc-prose text-body mt-3 leading-relaxed">
              Releases that would break a published client are blocked before
              merge, with the breaking line and the reason in hand. Strawberry
              Shake regenerates clients via MSBuild codegen, so contract drift
              shows up as build feedback you cannot miss.
            </p>
          </div>
          <div>
            <Eyebrow>What it needs</Eyebrow>
            <p className="text-cc-prose text-body mt-3 leading-relaxed">
              A client is only guarded once its operations are registered.
              Unregistered traffic is outside the net. The registry reports
              published clients affected. It does not claim certainty about
              consumers it has never seen.
            </p>
          </div>
        </div>
      </Beat>

      {/* 8. Reliability line. Promoted to a definition list so screen readers
          announce the stat with its caption, not as a run-on sentence. */}
      <Beat>
        <Eyebrow>Since</Eyebrow>
        <dl
          className="mt-8 text-center"
          aria-label="0 breaking changes shipped across 12 published clients"
        >
          <dd
            className="font-heading text-cc-heading text-hero font-bold tracking-tight"
            style={{ color: CORAL }}
          >
            0
          </dd>
          <dt className="font-heading text-cc-heading text-h3 mt-4 font-semibold tracking-tight">
            breaking changes shipped
            <br />
            across 12 published clients.
          </dt>
        </dl>
      </Beat>

      {/* 9. Closing CTA. The rail terminates at this beat's coral dot. */}
      <Beat>
        <Eyebrow>Begin</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h3 mt-6 text-center font-semibold tracking-tight">
          Put a safety net under every schema change.
        </h2>
        <p className="text-cc-prose text-body mx-auto mt-6 max-w-xl text-center leading-relaxed">
          Classify, validate, and gate your releases so breaking changes stop at
          the door, not in your users&rsquo; hands.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/continuous-integration">
            Read the Docs
          </OutlineButton>
        </div>
      </Beat>
    </div>
  );
}
