import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";

export const metadata: Metadata = {
  title: "Release Safety for GraphQL Schemas | ChilliCream",
  description:
    "Safe GraphQL schema evolution as quality control on the bar. Every change is tasted, stamped safe, dangerous, or breaking; CI blocks unsafe pours.",
  keywords: [
    "safe GraphQL schema evolution",
    "GraphQL release safety",
    "schema diff review",
    "breaking change detection",
    "schema registry",
    "client registry",
    "validate publish gate",
    "CI schema checks",
    "published clients affected",
    "schema version timeline",
  ],
  openGraph: {
    title: "Every Change Passes the Bar",
    description:
      "Safe GraphQL schema evolution as quality control on the bar: every change is tasted, stamped, and only then served. Breaking pours go back at the gate.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Scene tokens                                                        */
/* The page reads as a sibling of v1: same guardrail-blue surfaces,    */
/* same status triplet (success/warning/danger). Coffee lives in the   */
/* copy and three drink icons, not in the palette.                     */
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

interface StatusChipProps {
  readonly status: ChangeStatus;
  readonly label?: string;
  readonly className?: string;
}

function StatusChip({ status, label, className = "" }: StatusChipProps) {
  const meta = STATUS_META[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${meta.bg} ${meta.text} ${meta.ring} ${className}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
      {label ?? meta.label}
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
/* HERO: status-stamped diff + Tasting Notes side card                 */
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

interface TastingTicketProps {
  readonly field: string;
  readonly status: ChangeStatus;
  readonly stamp: string;
  readonly note: string;
}

function TastingTicket({ field, status, stamp, note }: TastingTicketProps) {
  const meta = STATUS_META[status];
  return (
    <li
      className={`border-cc-card-border rounded-md border px-3 py-2.5 ${meta.bg}`}
    >
      <div className="flex items-center gap-2">
        <code className="text-cc-heading bg-cc-hover rounded px-1.5 py-0.5 font-mono text-[0.7rem]">
          {field}
        </code>
        <span
          className={`ml-auto font-mono text-[0.6rem] font-semibold tracking-[0.16em] uppercase ${meta.text}`}
        >
          {stamp}
        </span>
      </div>
      <p className="text-cc-ink-dim mt-1.5 text-[0.72rem] leading-snug">
        {note}
      </p>
    </li>
  );
}

function TastingNotesCard() {
  return (
    <div
      className="border-cc-card-border rounded-xl border p-3.5"
      style={{ backgroundColor: GUARDRAIL_RAISED }}
    >
      <div className="flex items-center gap-2.5">
        <span className="text-cc-accent flex h-7 w-7 items-center justify-center">
          <Espresso className="h-7 w-7" />
        </span>
        <div>
          <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            Tasting notes
          </div>
          <div className="text-cc-heading font-mono text-[0.78rem]">
            three tickets at the bar
          </div>
        </div>
      </div>
      <ul className="mt-3 space-y-2">
        <TastingTicket
          field="Order.totalAmount"
          status="safe"
          stamp="Safe to serve"
          note="Additive. Pour it."
        />
        <TastingTicket
          field="Order.total"
          status="breaking"
          stamp="Send it back"
          note="Three regulars still order this. Deprecate first, drop once usage clears."
        />
        <TastingTicket
          field="Order.placedAt"
          status="dangerous"
          stamp="Hold the pour"
          note="Deprecation set. Watch the bar before the next round."
        />
      </ul>
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
          <DiffRow key={i} line={line} />
        ))}
      </div>
    </AppWindow>
  );
}

function HeroSection() {
  const legend: readonly {
    status: ChangeStatus;
    stamp: string;
    note: string;
  }[] = [
    { status: "safe", stamp: "Safe to serve", note: "additive, pour it" },
    { status: "dangerous", stamp: "Hold the pour", note: "deprecate first" },
    { status: "breaking", stamp: "Send it back", note: "breaks a regular" },
  ];
  return (
    <section className="grid items-start gap-12 lg:grid-cols-[minmax(0,0.92fr)_minmax(0,1.08fr)]">
      <div>
        <Eyebrow>Platform · Release Safety</Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-bold tracking-tight">
          Every change
          <br />
          passes the bar.
        </h1>
        <p className="lead text-cc-ink-dim mt-6 max-w-xl">
          Safe GraphQL schema evolution as quality control on the bar. Nothing
          leaves the counter unstamped.
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
        <ul className="border-cc-card-border mt-10 max-w-md overflow-hidden rounded-lg border">
          {legend.map((l, i) => {
            const meta = STATUS_META[l.status];
            return (
              <li
                key={l.stamp}
                className={`flex items-center gap-3 px-3 py-2.5 ${i > 0 ? "border-cc-card-border border-t" : ""}`}
                style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
              >
                <span
                  className={`flex h-5 w-5 items-center justify-center rounded-full ${meta.bg} ring-1 ring-inset ${meta.ring}`}
                >
                  <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
                </span>
                <span
                  className={`w-32 font-mono text-[0.68rem] font-semibold tracking-[0.12em] uppercase ${meta.text}`}
                >
                  {l.stamp}
                </span>
                <span className="text-cc-ink-dim font-mono text-[0.7rem]">
                  {l.note}
                </span>
              </li>
            );
          })}
        </ul>
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
        <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.62fr)]">
          <HeroDiffMock />
          <TastingNotesCard />
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* HOUSE RULES: the validate -> publish gate as bar quality control    */
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
        name="Schema validation, breaking change"
        detail="Order.total removed"
      />
      <CheckRow
        icon="pass"
        name="Schema validation, additive"
        detail="Money, totalAmount added"
      />
      <CheckRow
        icon="run"
        name="Client compatibility, partner app"
        detail="validating…"
      />
    </AppWindow>
  );
}

function HouseRulesSection() {
  return (
    <SectionShell
      eyebrow="Behind the bar"
      title="Nothing leaves the counter unstamped."
      lead="The order is simple. Taste first, then serve. The registry check runs on every pull request, classifies the change against published clients, and refuses to wave a breaking pour through. Merging is blocked until the ticket is clean."
      bullets={[
        "Taste: every change is classified against the published clients.",
        "Serve: only changes that clear the taste check are promoted.",
        "A failing ticket carries its reason, not just a red stamp.",
      ]}
      artifact={<CheckCard />}
      flip
    />
  );
}

/* ------------------------------------------------------------------ */
/* ON THE MENU: published-clients impact matrix as regulars table       */
/* ------------------------------------------------------------------ */

interface ClientRow {
  readonly name: string;
  readonly env: string;
  readonly usual: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "queued";
}

const CLIENT_ROWS: readonly ClientRow[] = [
  {
    name: "web",
    env: "production",
    usual: "the usual",
    ok: 5,
    total: 5,
    status: "ok",
  },
  {
    name: "mobile",
    env: "production",
    usual: "the usual",
    ok: 3,
    total: 5,
    status: "risk",
  },
  {
    name: "partner",
    env: "sandbox",
    usual: "trying a new pour",
    ok: 0,
    total: 4,
    status: "queued",
  },
  {
    name: "internal-admin",
    env: "staging",
    usual: "the usual",
    ok: 6,
    total: 6,
    status: "ok",
  },
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
    <span className="flex gap-1" aria-label="operations passing">
      {cells.map((_, i) => (
        <span
          key={i}
          className={`h-2 w-5 rounded-[2px] ${i < ok ? color : "bg-cc-ink-faint"}`}
        />
      ))}
    </span>
  );
}

function PerforatedEdge({ side }: { readonly side: "top" | "bottom" }) {
  return (
    <div
      aria-hidden
      className={`pointer-events-none absolute right-0 left-0 ${side === "top" ? "top-0" : "bottom-0"} h-2`}
      style={{
        backgroundImage:
          "radial-gradient(circle at 6px 50%, var(--color-cc-bg, #0b0f1a) 2.5px, transparent 3px)",
        backgroundSize: "12px 100%",
        backgroundRepeat: "repeat-x",
        backgroundPosition: side === "top" ? "0 0" : "0 100%",
      }}
    />
  );
}

function RegularsTable() {
  const statusLabel: Record<
    ClientRow["status"],
    { text: string; cls: string }
  > = {
    ok: { text: "OK", cls: "text-cc-success" },
    risk: { text: "at risk", cls: "text-cc-warning" },
    queued: { text: "queued", cls: "text-cc-nav-label" },
  };
  return (
    <div
      className="border-cc-card-border relative overflow-hidden rounded-md border shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)]"
      style={{ backgroundColor: GUARDRAIL }}
    >
      <PerforatedEdge side="top" />
      {/* order ticket header */}
      <div
        className="border-cc-card-border border-b px-4 pt-5 pb-3"
        style={{ backgroundColor: "#0d1b30" }}
      >
        <div className="flex items-baseline justify-between font-mono text-[0.62rem] tracking-[0.18em] uppercase">
          <span className="text-cc-accent">order ticket</span>
          <span className="text-cc-nav-label">no. 0482</span>
        </div>
        <div className="text-cc-heading mt-2 font-mono text-[0.84rem] font-semibold">
          client registry, impact of #482
        </div>
        <div className="text-cc-ink-dim mt-1 font-mono text-[0.66rem]">
          published clients affected, 4 regulars on file
        </div>
      </div>
      <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[1.4fr_1.1fr_0.7fr] gap-3 border-b px-4 py-2 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
        <span>regular</span>
        <span>operations passing</span>
        <span className="text-right">status</span>
      </div>
      {CLIENT_ROWS.map((c, i) => {
        const s = statusLabel[c.status];
        return (
          <div
            key={c.name}
            className="border-cc-card-border grid grid-cols-[1.4fr_1.1fr_0.7fr] items-center gap-3 border-b px-4 py-3 last:border-b-0"
          >
            <div className="flex min-w-0 items-center gap-3">
              <span className="text-cc-nav-label/70 w-6 shrink-0 font-mono text-[0.62rem] tabular-nums">
                {String(i + 1).padStart(2, "0")}
              </span>
              <div className="min-w-0">
                <div className="text-cc-heading truncate font-mono text-[0.78rem]">
                  {c.name}
                </div>
                <div className="text-cc-nav-label font-mono text-[0.62rem]">
                  {c.env} · {c.usual}
                </div>
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
      <div
        className="border-cc-card-border flex items-center justify-between border-t px-4 py-3 font-mono text-[0.64rem]"
        style={{ backgroundColor: "#0d1b30" }}
      >
        <span className="text-cc-ink-dim">stamp before serving</span>
        <span className="text-cc-accent tracking-[0.18em] uppercase">
          bar copy
        </span>
      </div>
      <PerforatedEdge side="bottom" />
    </div>
  );
}

function MenuSection() {
  return (
    <SectionShell
      eyebrow="Who orders this"
      title="On the menu: the regulars table."
      lead="The client registry tracks the operations your published clients actually run. Before a change ships, you can see which regulars are clear and which would have an order break, by name and by environment."
      bullets={[
        "Validation runs against the operations published clients send.",
        "Each client reports passing operations, not a vague global verdict.",
        "Queued clients are validated as soon as their operations are registered.",
      ]}
      artifact={<RegularsTable />}
    />
  );
}

/* ------------------------------------------------------------------ */
/* THE POUR ORDER: validate -> publish schematic on blueprint grid      */
/* ------------------------------------------------------------------ */

interface GateNodeProps {
  readonly kicker: string;
  readonly label: string;
  readonly sub: string;
  readonly tone: "neutral" | "warning" | "success" | "danger";
  readonly compact?: boolean;
  readonly icon?: ReactNode;
}

function GateNode({
  kicker,
  label,
  sub,
  tone,
  compact = false,
  icon,
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
      <div className="flex items-center justify-between gap-2">
        <div className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] uppercase">
          {kicker}
        </div>
        {icon !== undefined && (
          <span className="h-5 w-5 opacity-80">{icon}</span>
        )}
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

function PourOrderSchematic() {
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
          label="order in"
          sub="pull request"
          tone="neutral"
        />
        <Connector />
        <GateNode
          kicker="02"
          label="taste check"
          sub="classify + check clients"
          tone="warning"
          icon={<DripBrewer className="text-cc-warning h-5 w-5" />}
        />
        <Connector branch />
        <div className="flex flex-1 flex-col gap-3">
          <GateNode
            kicker="03a"
            label="served"
            sub="safe → promoted"
            tone="success"
            compact
          />
          <GateNode
            kicker="03b"
            label="sent back"
            sub="breaking → stop"
            tone="danger"
            compact
          />
        </div>
      </div>
      <p className="text-cc-ink-dim mt-6 max-w-2xl font-mono text-[0.72rem] leading-relaxed">
        The order is the guarantee. Nothing reaches{" "}
        <span className="text-cc-prose">served</span> until it clears{" "}
        <span className="text-cc-warning">taste check</span>. A breaking pour
        never advances; it goes back with the reason attached.
      </p>
    </div>
  );
}

function PourOrderSection() {
  return (
    <SectionShell
      eyebrow="The pour order"
      title="Validate, then publish. Never the other way around."
      lead="Release safety is a two-stage bar. A change is classified and checked against published clients first; only changes that clear validation are served. The order is the guarantee."
      bullets={[
        "Validate classifies the change and tests it against real clients.",
        "Publish only ever runs on a change that already cleared validation.",
        "A sent-back change carries its reason, not just a red stamp.",
      ]}
      artifact={<PourOrderSchematic />}
      flip
    />
  );
}

/* ------------------------------------------------------------------ */
/* THE LOGBOOK: schema version timeline                                */
/* ------------------------------------------------------------------ */

interface VersionPoint {
  readonly v: string;
  readonly note: string;
  readonly status: ChangeStatus;
  readonly tail?: string;
}

const VERSIONS: readonly VersionPoint[] = [
  { v: "v12", note: "add Cart.discount", status: "safe" },
  { v: "v13", note: "deprecate Order.placedAt", status: "dangerous" },
  {
    v: "v14",
    note: "remove Order.total",
    status: "breaking",
    tail: "returned to kitchen",
  },
  { v: "v14", note: "add Order.totalAmount", status: "safe" },
  { v: "v15", note: "drop Order.total, usage cleared", status: "dangerous" },
];

function LogbookTimeline() {
  return (
    <div
      className="border-cc-card-border relative overflow-hidden rounded-xl border shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)]"
      style={{ backgroundColor: GUARDRAIL }}
    >
      {/* logbook header strip */}
      <div
        className="border-cc-card-border flex items-baseline justify-between border-b px-5 py-3 font-mono text-[0.62rem] tracking-[0.18em] uppercase"
        style={{ backgroundColor: "#0d1b30" }}
      >
        <span className="text-cc-accent">behind the bar log</span>
        <span className="text-cc-nav-label">orders-api · book 03</span>
      </div>
      {/* ruled binder margin on the left + horizontal rules */}
      <div className="relative px-5 pt-2 pb-6">
        <span
          aria-hidden
          className="bg-cc-danger/30 absolute top-0 bottom-0 left-12 w-px"
        />
        <ol className="font-mono">
          {VERSIONS.map((p, i) => {
            const meta = STATUS_META[p.status];
            return (
              <li
                key={i}
                className="border-cc-card-border/60 grid grid-cols-[2rem_2.75rem_minmax(0,1fr)_auto] items-center gap-3 border-b py-3 last:border-b-0"
              >
                <span className="text-cc-nav-label/70 text-right text-[0.62rem] tabular-nums">
                  {String(i + 1).padStart(2, "0")}
                </span>
                <span
                  className={`text-[0.74rem] font-semibold ${meta.text} flex items-center gap-2`}
                >
                  <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
                  {p.v}
                </span>
                <span className="text-cc-prose min-w-0 truncate text-[0.74rem]">
                  {p.note}
                  {p.tail !== undefined && (
                    <span className="text-cc-danger ml-2 text-[0.66rem]">
                      · {p.tail}
                    </span>
                  )}
                </span>
                <StatusChip status={p.status} />
              </li>
            );
          })}
        </ol>
        <p className="text-cc-ink-dim mt-5 font-mono text-[0.7rem] leading-relaxed">
          The breaking removal at <span className="text-cc-danger">v14</span>{" "}
          never shipped. It was reshaped as an additive change, deprecated, and
          only dropped at <span className="text-cc-warning">v15</span> once
          client usage cleared.
        </p>
      </div>
    </div>
  );
}

function LogbookSection() {
  return (
    <SectionShell
      eyebrow="Behind the bar log"
      title="Every pour, recorded."
      lead="The registry keeps the full history of your schema with each change classified in place. You can see exactly when a contract shifted, why it was safe, and how a risky change was reshaped before it shipped."
      bullets={[
        "Each published version records its classification.",
        "Deprecations buy time; removals wait for usage to clear.",
        "Drift between code and the published contract shows up as build feedback.",
      ]}
      artifact={<LogbookTimeline />}
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
/* Reliability band: big numbers, no metaphor                          */
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
/* HOUSE PROMISE: honesty section                                      */
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

function HousePromiseSection() {
  return (
    <section
      className="border-cc-card-border rounded-2xl border px-6 py-9 sm:px-10 sm:py-11"
      style={{ backgroundColor: "rgba(13, 27, 48, 0.6)" }}
    >
      <div className="max-w-3xl">
        <Eyebrow>House promise</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          What we pour, and what we will not pretend.
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
/* Closing CTA: CoffeeTray flourish above the headline                 */
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
      <CoffeeTray className="mx-auto mb-5 h-12 w-auto opacity-90" />
      <h2 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
        Put a safety net under every pour.
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

export default function ReleaseSafetyPreviewV6() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <HouseRulesSection />
      <MenuSection />
      <PourOrderSection />
      <LogbookSection />
      <ReliabilityBand />
      <HousePromiseSection />
      <ClosingCta />
    </div>
  );
}
