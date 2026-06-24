import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Release Safety for GraphQL APIs | ChilliCream",
  description:
    "Stamp every GraphQL schema change safe, dangerous, or breaking. See which published clients are affected and let CI block unsafe releases before consumers hit them.",
  keywords: [
    "GraphQL release safety",
    "schema registry",
    "client registry",
    "breaking change detection",
    "schema diff",
    "CI schema check",
    "schema validation",
    "API contract",
    "schema versioning",
    "ChilliCream Nitro",
  ],
  openGraph: {
    title: "Release Safety for GraphQL APIs | ChilliCream",
    description:
      "Stamp every schema change safe, dangerous, or breaking, surface the published clients affected, and gate unsafe releases in CI before consumers ever see them.",
  },
  robots: { index: false, follow: false },
};

/* --------------------------------------------------------------------------
   Scene accent: status-driven on a guardrail-blue surface.
     SAFE      -> cc-success green
     DANGEROUS -> cc-warning amber
     BREAKING  -> cc-danger coral
   Guardrail blue (#3f6fb0-ish) is the structural/chrome hue for rails, grids
   and gate plumbing so the three status colors stay meaningful.
   -------------------------------------------------------------------------- */

const GUARDRAIL = "#4d7fc4";

type Status = "safe" | "dangerous" | "breaking";

const STATUS: Record<
  Status,
  { label: string; color: string; soft: string; ring: string }
> = {
  safe: {
    label: "SAFE",
    color: "var(--color-cc-success)",
    soft: "color-mix(in srgb, var(--color-cc-success) 14%, transparent)",
    ring: "color-mix(in srgb, var(--color-cc-success) 42%, transparent)",
  },
  dangerous: {
    label: "DANGEROUS",
    color: "var(--color-cc-warning)",
    soft: "color-mix(in srgb, var(--color-cc-warning) 14%, transparent)",
    ring: "color-mix(in srgb, var(--color-cc-warning) 42%, transparent)",
  },
  breaking: {
    label: "BREAKING",
    color: "var(--color-cc-danger)",
    soft: "color-mix(in srgb, var(--color-cc-danger) 16%, transparent)",
    ring: "color-mix(in srgb, var(--color-cc-danger) 46%, transparent)",
  },
};

// Narrows a wider union (status + non-status sentinels like "ok"/"queued") to a
// Status entry, returning null for the sentinels.
function statusOf(value: string): (typeof STATUS)[Status] | null {
  return value in STATUS ? STATUS[value as Status] : null;
}

// --------------------------------------------------------------------------
// Small shared primitives
// --------------------------------------------------------------------------

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
  readonly status: Status;
  readonly className?: string;
}

function StatusChip({ status, className }: StatusChipProps) {
  const s = STATUS[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2 py-[3px] font-mono text-[0.6rem] leading-none font-semibold tracking-[0.12em] uppercase ${className ?? ""}`}
      style={{
        color: s.color,
        backgroundColor: s.soft,
        boxShadow: `inset 0 0 0 1px ${s.ring}`,
      }}
    >
      <span
        className="size-1.5 rounded-full"
        style={{ backgroundColor: s.color }}
      />
      {s.label}
    </span>
  );
}

interface TileProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly glow?: string;
}

// Layered bento tile: hairline card + an optional status glow bleeding from a
// corner. Depth comes from the inner highlight + soft outer shadow.
function Tile({ children, className, glow }: TileProps) {
  return (
    <div
      className={`group border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur-xl ${className ?? ""}`}
      style={{
        boxShadow:
          "inset 0 1px 0 0 rgba(245,241,234,0.06), 0 18px 50px -28px rgba(0,0,0,0.85)",
      }}
    >
      {glow ? (
        <div
          aria-hidden
          className="pointer-events-none absolute -top-20 -right-16 size-56 rounded-full opacity-50 blur-3xl transition-opacity duration-500 group-hover:opacity-80"
          style={{ backgroundColor: glow }}
        />
      ) : null}
      <div className="relative">{children}</div>
    </div>
  );
}

// Faint blueprint grid backdrop for schematic tiles.
function BlueprintGrid() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0"
      style={{
        backgroundImage: `linear-gradient(${GUARDRAIL}1f 1px, transparent 1px), linear-gradient(90deg, ${GUARDRAIL}1f 1px, transparent 1px)`,
        backgroundSize: "26px 26px",
        maskImage:
          "radial-gradient(120% 90% at 50% 10%, #000 35%, transparent 100%)",
        WebkitMaskImage:
          "radial-gradient(120% 90% at 50% 10%, #000 35%, transparent 100%)",
      }}
    />
  );
}

// --------------------------------------------------------------------------
// HERO
// --------------------------------------------------------------------------

function Hero() {
  return (
    <section className="border-cc-card-border relative isolate overflow-hidden rounded-3xl border px-6 py-14 sm:px-12 sm:py-20">
      {/* spotlight mesh in guardrail blue + status accents, motion-safe drift */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background: `radial-gradient(80% 60% at 26% 0%, ${GUARDRAIL}30, transparent 60%), radial-gradient(60% 50% at 92% 18%, color-mix(in srgb, var(--color-cc-danger) 22%, transparent), transparent 62%), radial-gradient(70% 60% at 60% 110%, color-mix(in srgb, var(--color-cc-success) 18%, transparent), transparent 60%)`,
        }}
      />
      <BlueprintGrid />

      <div className="relative grid items-center gap-12 lg:grid-cols-[1.05fr_0.95fr]">
        <div>
          <div className="flex items-center gap-3">
            <Eyebrow>Platform / Release Safety</Eyebrow>
            <span
              className="hidden h-px flex-1 sm:block"
              style={{
                background: `linear-gradient(90deg, ${GUARDRAIL}55, transparent)`,
              }}
            />
          </div>

          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-5 font-bold tracking-tight">
            Change contracts
            <br />
            with a{" "}
            <span
              style={{
                color: "transparent",
                backgroundImage:
                  "linear-gradient(100deg, #16b9e4, #7c92c6 52%, #f0786a)",
                backgroundClip: "text",
                WebkitBackgroundClip: "text",
              }}
            >
              safety net.
            </span>
          </h1>

          <p className="lead text-cc-ink-dim mt-6 max-w-xl">
            Your schema is a promise to every client that ships against it.
            ChilliCream classifies each change, shows the published clients it
            puts at risk, and stops an unsafe release before a single consumer
            discovers it the hard way.
          </p>

          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/platform/continuous-integration">
              Read the Docs
            </OutlineButton>
          </div>

          <ul className="text-caption text-cc-ink-dim mt-8 flex flex-wrap gap-x-7 gap-y-2">
            {[
              "Diff classified safe / dangerous / breaking",
              "Validated against the client registry",
              "CI gate blocks unsafe releases",
            ].map((t) => (
              <li key={t} className="flex items-center gap-2">
                <span style={{ color: "var(--color-cc-success)" }}>
                  <CheckIcon />
                </span>
                {t}
              </li>
            ))}
          </ul>
        </div>

        {/* Floating PR-check status card */}
        <div className="lg:pl-4">
          <PrCheckCard />
        </div>
      </div>
    </section>
  );
}

// PR check status card — the GitHub-style "checks failed" panel.
function PrCheckCard() {
  const checks: {
    name: string;
    status: Status | "ok";
    note: string;
  }[] = [
    { name: "lint / build", status: "ok", note: "passed" },
    { name: "schema / safe changes", status: "safe", note: "2 additions" },
    { name: "schema / dangerous", status: "dangerous", note: "1 default drop" },
    { name: "schema / breaking", status: "breaking", note: "1 field removed" },
  ];

  return (
    <div
      className="border-cc-card-border bg-cc-code-bg/90 relative rounded-2xl border backdrop-blur-xl"
      style={{
        boxShadow:
          "0 26px 70px -30px rgba(0,0,0,0.9), inset 0 1px 0 0 rgba(245,241,234,0.05)",
      }}
    >
      <div className="border-cc-card-border bg-cc-code-header/80 flex items-center justify-between border-b px-4 py-3">
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">
          PR #482 · evolve order schema
        </span>
        <span
          className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 font-mono text-[0.6rem] font-semibold tracking-wider uppercase"
          style={{
            color: "var(--color-cc-danger)",
            backgroundColor: STATUS.breaking.soft,
            boxShadow: `inset 0 0 0 1px ${STATUS.breaking.ring}`,
          }}
        >
          <span
            className="size-1.5 rounded-full"
            style={{ backgroundColor: "var(--color-cc-danger)" }}
          />
          Checks failed
        </span>
      </div>

      <div className="px-4 py-3">
        <p className="text-cc-ink-dim mb-3 font-mono text-[0.68rem]">
          Registry check — 1 breaking, 1 dangerous, 2 safe
        </p>
        <ul className="space-y-1.5">
          {checks.map((c) => {
            const s = statusOf(c.status);
            const isOk = s === null;
            const color = s?.color ?? "var(--color-cc-success)";
            return (
              <li
                key={c.name}
                className="border-cc-card-border/70 flex items-center justify-between rounded-lg border px-3 py-2"
                style={{
                  backgroundColor: s?.soft ?? "transparent",
                }}
              >
                <span className="flex items-center gap-2.5">
                  <span
                    className="grid size-4 place-items-center rounded-full"
                    style={{
                      color,
                      boxShadow: `inset 0 0 0 1.5px ${color}`,
                    }}
                  >
                    {isOk ? (
                      <CheckIcon size={9} />
                    ) : (
                      <span className="font-mono text-[0.6rem] leading-none font-bold">
                        !
                      </span>
                    )}
                  </span>
                  <span className="text-cc-heading font-mono text-[0.72rem]">
                    {c.name}
                  </span>
                </span>
                <span
                  className="font-mono text-[0.62rem]"
                  style={{ color: isOk ? "var(--color-cc-ink-dim)" : color }}
                >
                  {c.note}
                </span>
              </li>
            );
          })}
        </ul>

        <div className="border-cc-card-border mt-3 flex items-center justify-between border-t pt-3">
          <span className="text-cc-ink-dim font-mono text-[0.62rem]">
            Merging blocked until resolved
          </span>
          <span
            className="text-cc-ink-dim rounded-md px-2.5 py-1 font-mono text-[0.62rem]"
            style={{ boxShadow: "inset 0 0 0 1px var(--color-cc-card-border)" }}
          >
            Re-run checks
          </span>
        </div>
      </div>
    </div>
  );
}

// --------------------------------------------------------------------------
// SCHEMA DIFF (the centerpiece — owns the pinned-comment-thread device)
// --------------------------------------------------------------------------

interface DiffLineProps {
  readonly sign?: "+" | "-" | " ";
  readonly indent?: number;
  readonly children: ReactNode;
  readonly status?: Status;
}

function DiffLine({ sign = " ", indent = 0, children, status }: DiffLineProps) {
  const tint =
    sign === "+"
      ? "color-mix(in srgb, var(--color-cc-success) 9%, transparent)"
      : sign === "-"
        ? "color-mix(in srgb, var(--color-cc-danger) 9%, transparent)"
        : "transparent";
  const gutter =
    sign === "+"
      ? "var(--color-cc-success)"
      : sign === "-"
        ? "var(--color-cc-danger)"
        : "transparent";
  return (
    <div
      className="flex items-center gap-3 px-4"
      style={{ backgroundColor: tint }}
    >
      <span
        className="w-3 shrink-0 text-center font-mono text-[0.72rem] select-none"
        style={{
          color:
            gutter === "transparent" ? "var(--color-cc-ink-faint)" : gutter,
        }}
      >
        {sign === " " ? "" : sign}
      </span>
      <code
        className="text-cc-prose flex-1 py-[3px] font-mono text-[0.74rem] leading-relaxed"
        style={{ paddingLeft: `${indent * 1.1}rem` }}
      >
        {children}
      </code>
      {status ? <StatusChip status={status} className="my-1 shrink-0" /> : null}
    </div>
  );
}

function SchemaDiff() {
  return (
    <Tile className="lg:col-span-7" glow={STATUS.breaking.soft}>
      <div className="border-cc-card-border bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-3">
        <div className="flex items-center gap-2.5">
          <span className="flex gap-1.5">
            <span className="bg-cc-danger/70 size-2.5 rounded-full" />
            <span className="bg-cc-warning/70 size-2.5 rounded-full" />
            <span className="bg-cc-success/70 size-2.5 rounded-full" />
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.7rem]">
            schema.graphql · validate
          </span>
        </div>
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          v14 → v15
        </span>
      </div>

      <div className="bg-cc-code-bg py-2">
        <DiffLine>
          <span style={{ color: GUARDRAIL }}>type</span> Order {"{"}
        </DiffLine>
        <DiffLine indent={1}>id: ID!</DiffLine>
        <DiffLine sign="+" indent={1} status="safe">
          trackingUrl: String
        </DiffLine>
        <DiffLine sign="+" indent={1} status="safe">
          estimatedDelivery: DateTime
        </DiffLine>
        <DiffLine sign="-" indent={1} status="dangerous">
          status: OrderStatus = PENDING
        </DiffLine>
        <DiffLine sign="+" indent={1}>
          status: OrderStatus!
        </DiffLine>
        <DiffLine sign="-" indent={1} status="breaking">
          shippingAddress: Address!
        </DiffLine>
        <DiffLine indent={1}>total: Money!</DiffLine>
        <DiffLine>{"}"}</DiffLine>
      </div>

      {/* Pinned comment thread on the breaking line */}
      <div className="border-cc-card-border border-t px-4 py-4">
        <PinnedThread />
      </div>
    </Tile>
  );
}

function PinnedThread() {
  return (
    <div
      className="rounded-xl border p-3.5"
      style={{
        borderColor: STATUS.breaking.ring,
        backgroundColor: STATUS.breaking.soft,
      }}
    >
      <div className="mb-2.5 flex items-center gap-2">
        <span style={{ color: "var(--color-cc-danger)" }}>
          <PinIcon />
        </span>
        <span className="text-cc-heading font-mono text-[0.64rem] font-semibold tracking-wider uppercase">
          Breaking · line 7
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.6rem]">
          removing a non-null field
        </span>
      </div>

      <div className="space-y-2.5">
        <Comment
          author="registry-bot"
          tone="bot"
          body="shippingAddress is referenced by 1 published client (mobile-app v8.2). Removing it breaks live queries."
        />
        <Comment
          author="you"
          tone="human"
          body="Deprecate it for one release, ship the replacement, then remove once mobile rolls forward."
        />
      </div>

      <div className="border-cc-card-border/60 mt-3 flex items-center justify-between border-t pt-2.5">
        <span className="text-cc-ink-dim font-mono text-[0.6rem]">
          Status synced to the PR check
        </span>
        <span
          className="inline-flex items-center gap-1.5 rounded-md px-2 py-1 font-mono text-[0.6rem] font-semibold"
          style={{
            color: "var(--color-cc-warning)",
            boxShadow:
              "inset 0 0 0 1px color-mix(in srgb, var(--color-cc-warning) 42%, transparent)",
          }}
        >
          Resolve
        </span>
      </div>
    </div>
  );
}

interface CommentProps {
  readonly author: string;
  readonly tone: "bot" | "human";
  readonly body: string;
}

function Comment({ author, tone, body }: CommentProps) {
  const isBot = tone === "bot";
  return (
    <div className="flex gap-2.5">
      <span
        className="mt-[1px] grid size-6 shrink-0 place-items-center rounded-full font-mono text-[0.6rem] font-semibold"
        style={{
          color: isBot ? GUARDRAIL : "var(--color-cc-heading)",
          backgroundColor: isBot ? `${GUARDRAIL}26` : "rgba(245,241,234,0.08)",
        }}
      >
        {isBot ? "◆" : "you"}
      </span>
      <div className="min-w-0">
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          {author}
        </span>
        <p className="text-cc-prose text-[0.78rem] leading-snug">{body}</p>
      </div>
    </div>
  );
}

function PinIcon() {
  return (
    <svg viewBox="0 0 16 16" width={13} height={13} aria-hidden>
      <path
        d="M9.5 1.5 6 5 3 5.5 2.5 7 5 9.5 1.5 13l-.5 1.5L6.5 11 9 13.5l1.5-.5L10.5 10l3.5-3.5L13.5 5l-.5-3z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.3"
        strokeLinejoin="round"
      />
    </svg>
  );
}

// --------------------------------------------------------------------------
// CLIENT IMPACT MATRIX
// --------------------------------------------------------------------------

function ClientImpactMatrix() {
  const rows: {
    client: string;
    detail: string;
    ok: number;
    total: number;
    status: Status | "queued";
  }[] = [
    {
      client: "web",
      detail: "checkout · 5 ops",
      ok: 5,
      total: 5,
      status: "safe",
    },
    {
      client: "mobile",
      detail: "iOS / Android v8.2",
      ok: 3,
      total: 5,
      status: "breaking",
    },
    {
      client: "partner-api",
      detail: "webhook consumers",
      ok: 4,
      total: 5,
      status: "dangerous",
    },
    {
      client: "internal-bff",
      detail: "queued for review",
      ok: 0,
      total: 0,
      status: "queued",
    },
  ];

  return (
    <Tile className="lg:col-span-5" glow={STATUS.dangerous.soft}>
      <div className="p-5 sm:p-6">
        <Eyebrow>Client registry</Eyebrow>
        <h3 className="font-heading text-h5 text-cc-heading mt-2 font-semibold">
          Published clients affected
        </h3>
        <p className="text-caption text-cc-ink-dim mt-1.5">
          The diff is validated against every registered client and its
          persisted operations, so risk is named before merge.
        </p>

        <ul className="mt-5 space-y-2.5">
          {rows.map((r) => {
            const s = statusOf(r.status);
            const queued = s === null;
            const color = s?.color ?? "var(--color-cc-ink-dim)";
            const pct = r.total > 0 ? (r.ok / r.total) * 100 : 0;
            return (
              <li
                key={r.client}
                className="border-cc-card-border/70 bg-cc-surface/40 rounded-xl border px-3.5 py-3"
              >
                <div className="flex items-center justify-between">
                  <span className="text-cc-heading font-mono text-[0.78rem]">
                    {r.client}
                  </span>
                  {queued ? (
                    <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-wider uppercase">
                      Queued
                    </span>
                  ) : (
                    <span
                      className="font-mono text-[0.66rem] font-semibold"
                      style={{ color }}
                    >
                      {r.ok}/{r.total} {r.status === "safe" ? "OK" : "at risk"}
                    </span>
                  )}
                </div>
                <div className="mt-2 flex items-center gap-3">
                  <span className="text-cc-ink-dim font-mono text-[0.6rem]">
                    {r.detail}
                  </span>
                </div>
                <div
                  className="mt-2 h-1.5 w-full overflow-hidden rounded-full"
                  style={{ backgroundColor: "rgba(245,241,234,0.07)" }}
                >
                  <div
                    className="h-full rounded-full"
                    style={{
                      width: `${queued ? 12 : pct}%`,
                      backgroundColor: color,
                      opacity: queued ? 0.4 : 1,
                    }}
                  />
                </div>
              </li>
            );
          })}
        </ul>
      </div>
    </Tile>
  );
}

// --------------------------------------------------------------------------
// VALIDATE -> PUBLISH GATE SCHEMATIC
// --------------------------------------------------------------------------

function GateSchematic() {
  return (
    <Tile className="lg:col-span-5" glow={`${GUARDRAIL}33`}>
      <div className="relative p-5 sm:p-6">
        <BlueprintGrid />
        <div className="relative">
          <Eyebrow>The gate</Eyebrow>
          <h3 className="font-heading text-h5 text-cc-heading mt-2 font-semibold">
            Validate, then publish
          </h3>
          <p className="text-caption text-cc-ink-dim mt-1.5 max-w-sm">
            Validation runs in the PR build. Publish only promotes a schema the
            gate already cleared, so the active contract is never the
            experiment.
          </p>

          <div className="mt-6 flex items-stretch gap-3">
            <GateStage
              phase="PR build"
              cmd="nitro schema validate"
              label="Validate"
              color={GUARDRAIL}
              hint="classify + check clients"
            />
            <GateConnector />
            <GateStage
              phase="Release"
              cmd="nitro schema publish"
              label="Publish"
              color="var(--color-cc-success)"
              hint="promote active schema"
            />
          </div>

          <div
            className="mt-5 flex items-center gap-2.5 rounded-lg px-3 py-2.5"
            style={{
              backgroundColor: STATUS.breaking.soft,
              boxShadow: `inset 0 0 0 1px ${STATUS.breaking.ring}`,
            }}
          >
            <span style={{ color: "var(--color-cc-danger)" }}>
              <ShieldIcon />
            </span>
            <span className="text-cc-prose text-[0.74rem]">
              A breaking diff fails the gate and the release never reaches the
              publish step.
            </span>
          </div>
        </div>
      </div>
    </Tile>
  );
}

interface GateStageProps {
  readonly phase: string;
  readonly cmd: string;
  readonly label: string;
  readonly color: string;
  readonly hint: string;
}

function GateStage({ phase, cmd, label, color, hint }: GateStageProps) {
  return (
    <div
      className="bg-cc-code-bg/80 flex-1 rounded-xl border p-3.5"
      style={{
        borderColor: `color-mix(in srgb, ${color} 38%, transparent)`,
      }}
    >
      <span className="text-cc-ink-dim font-mono text-[0.58rem] tracking-[0.16em] uppercase">
        {phase}
      </span>
      <div
        className="font-heading text-h6 mt-2 font-semibold"
        style={{ color }}
      >
        {label}
      </div>
      <code className="text-cc-prose mt-2 block truncate font-mono text-[0.62rem]">
        $ {cmd}
      </code>
      <span className="text-cc-ink-dim mt-1.5 block font-mono text-[0.58rem]">
        {hint}
      </span>
    </div>
  );
}

function GateConnector() {
  return (
    <div className="flex flex-col items-center justify-center px-0.5">
      <svg viewBox="0 0 24 16" width={26} height={16} aria-hidden>
        <defs>
          <linearGradient id="rs-gate-arrow" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor={GUARDRAIL} />
            <stop offset="100%" stopColor="var(--color-cc-success)" />
          </linearGradient>
        </defs>
        <path
          d="M1 8 H19 M14 3 L20 8 L14 13"
          fill="none"
          stroke="url(#rs-gate-arrow)"
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
      <span
        className="mt-1 grid size-5 place-items-center rounded-full"
        style={{
          color: "var(--color-cc-success)",
          boxShadow: "inset 0 0 0 1.5px var(--color-cc-success)",
        }}
      >
        <CheckIcon size={9} />
      </span>
    </div>
  );
}

function ShieldIcon() {
  return (
    <svg viewBox="0 0 16 16" width={15} height={15} aria-hidden>
      <path
        d="M8 1.5 13 3.5V8c0 3.2-2.2 5.2-5 6.5C5.2 13.2 3 11.2 3 8V3.5L8 1.5Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.3"
        strokeLinejoin="round"
      />
      <path
        d="M5.6 8 7.3 9.7 10.6 6"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

// --------------------------------------------------------------------------
// VERSION TIMELINE
// --------------------------------------------------------------------------

function VersionTimeline() {
  const versions: { tag: string; status: Status | "active" }[] = [
    { tag: "v11", status: "safe" },
    { tag: "v12", status: "dangerous" },
    { tag: "v13", status: "breaking" },
    { tag: "v14", status: "safe" },
    { tag: "v15", status: "active" },
  ];
  return (
    <Tile className="lg:col-span-7" glow={`${GUARDRAIL}26`}>
      <div className="p-5 sm:p-6">
        <Eyebrow>Schema registry · version history</Eyebrow>
        <h3 className="font-heading text-h5 text-cc-heading mt-2 font-semibold">
          Every change is a tagged, reversible snapshot
        </h3>

        <div className="relative mt-8 pb-2">
          <div
            className="absolute top-[6px] right-0 left-0 h-px"
            style={{
              background: `linear-gradient(90deg, transparent, ${GUARDRAIL}66 12%, ${GUARDRAIL}66 88%, transparent)`,
            }}
          />
          <ol className="relative flex justify-between">
            {versions.map((v) => {
              const s = statusOf(v.status);
              const active = s === null;
              const color = s?.color ?? "var(--color-cc-accent)";
              return (
                <li
                  key={v.tag}
                  className="flex flex-col items-center gap-2 text-center"
                >
                  <span
                    className="size-3 rounded-full"
                    style={{
                      backgroundColor: color,
                      boxShadow: active
                        ? `0 0 0 4px color-mix(in srgb, ${color} 22%, transparent)`
                        : "none",
                    }}
                  />
                  <span className="text-cc-heading font-mono text-[0.68rem]">
                    {v.tag}
                  </span>
                  <span
                    className="font-mono text-[0.54rem] tracking-wider uppercase"
                    style={{ color }}
                  >
                    {s ? s.label.toLowerCase() : "active"}
                  </span>
                </li>
              );
            })}
          </ol>
        </div>

        <p className="text-caption text-cc-ink-dim mt-5">
          Breaking-change markers stay on the record. Rollback is republishing
          an earlier tag, so a bad release is a one-command reversal rather than
          an incident.
        </p>
      </div>
    </Tile>
  );
}

// --------------------------------------------------------------------------
// STAT BAND + PULL QUOTE
// --------------------------------------------------------------------------

interface StatProps {
  readonly value: string;
  readonly label: string;
  readonly color: string;
}

function StatTile({ value, label, color }: StatProps) {
  return (
    <Tile
      className="lg:col-span-4"
      glow={`color-mix(in srgb, ${color} 24%, transparent)`}
    >
      <div className="flex h-full flex-col justify-center p-6">
        <span
          className="font-heading text-h2 leading-none font-bold"
          style={{ color }}
        >
          {value}
        </span>
        <span className="text-caption text-cc-ink-dim mt-3 leading-snug">
          {label}
        </span>
      </div>
    </Tile>
  );
}

function PullQuote() {
  return (
    <Tile className="lg:col-span-8" glow={`${GUARDRAIL}26`}>
      <div className="relative flex h-full flex-col justify-center p-6 sm:p-8">
        <span
          aria-hidden
          className="font-heading text-cc-ink-faint absolute top-2 right-5 text-[5rem] leading-none"
        >
          &rdquo;
        </span>
        <p className="font-heading text-h5 text-cc-heading relative max-w-2xl leading-snug font-medium">
          The release that would have paged us at 2&nbsp;a.m. now fails as a red
          check on the pull request. The breaking line is annotated, the
          affected client is named, and merge stays blocked until it&apos;s
          resolved.
        </p>
        <span className="text-cc-nav-label mt-5 font-mono text-[0.66rem] tracking-[0.16em] uppercase">
          How teams describe shipping with the registry
        </span>
      </div>
    </Tile>
  );
}

// --------------------------------------------------------------------------
// HONESTY / CREDIBILITY BEAT
// --------------------------------------------------------------------------

function HonestyBeat() {
  const points: { title: string; body: string }[] = [
    {
      title: "Classification, not a crystal ball",
      body: "We classify a change as safe, dangerous, or breaking against your registered schema and clients. It is structural analysis of the contract, not a guess about runtime behaviour.",
    },
    {
      title: "Published clients, by operation",
      body: "Impact is scoped to clients whose persisted operations are in the registry. A client that never registered its operations can't be checked, and we don't pretend otherwise.",
    },
    {
      title: "The gate is only as strict as you wire it",
      body: "Validation surfaces risk; your CI decides what blocks a merge. Dangerous changes can be allowed through deliberately, with the annotation kept on the record.",
    },
  ];
  return (
    <section className="mt-6">
      <div className="mx-auto max-w-2xl text-center">
        <Eyebrow>What it does and doesn&apos;t do</Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold">
          A safety net, honestly described
        </h2>
        <p className="lead text-cc-ink-dim mt-4">
          Confidence comes from knowing the exact edges of the guarantee.
        </p>
      </div>

      <div className="mt-10 grid gap-4 md:grid-cols-3">
        {points.map((p) => (
          <div
            key={p.title}
            className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-xl"
            style={{
              boxShadow: "inset 0 1px 0 0 rgba(245,241,234,0.05)",
            }}
          >
            <span style={{ color: GUARDRAIL }}>
              <ShieldIcon />
            </span>
            <h3 className="font-heading text-h6 text-cc-heading mt-3 font-semibold">
              {p.title}
            </h3>
            <p className="text-caption text-cc-ink-dim mt-2 leading-relaxed">
              {p.body}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

// --------------------------------------------------------------------------
// CLOSING CTA
// --------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="border-cc-card-border relative mt-6 overflow-hidden rounded-3xl border px-6 py-14 text-center sm:px-12">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background: `radial-gradient(70% 80% at 50% 0%, ${GUARDRAIL}2e, transparent 65%), radial-gradient(50% 60% at 18% 100%, color-mix(in srgb, var(--color-cc-success) 14%, transparent), transparent 70%), radial-gradient(50% 60% at 84% 100%, color-mix(in srgb, var(--color-cc-danger) 14%, transparent), transparent 70%)`,
        }}
      />
      <h2 className="font-heading text-h3 text-cc-heading sm:text-h2 font-semibold">
        Stop unsafe releases before
        <br className="hidden sm:block" /> your consumers find them.
      </h2>
      <p className="lead text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Put the registry between your schema changes and production. Classify,
        review, gate, publish.
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

// --------------------------------------------------------------------------
// PAGE
// --------------------------------------------------------------------------

export default function ReleaseSafetyV3Page() {
  return (
    <div className="space-y-6 py-2">
      <Hero />

      {/* Asymmetric bento grid — the heart of the page */}
      <section className="grid grid-cols-1 gap-4 lg:grid-cols-12">
        <SchemaDiff />
        <ClientImpactMatrix />

        <StatTile
          value="0"
          label="breaking changes shipped to a consumer through the gate"
          color="var(--color-cc-success)"
        />
        <PullQuote />

        <GateSchematic />
        <VersionTimeline />

        <StatTile
          value="3"
          label="status classes: safe, dangerous, breaking — stamped on every diff line"
          color="var(--color-cc-warning)"
        />
        <StatTile
          value="100%"
          label="of releases pass through the validate gate before they go active"
          color={GUARDRAIL}
        />
        <StatTile
          value="1 cmd"
          label="rollback by republishing an earlier tagged schema snapshot"
          color="var(--color-cc-accent)"
        />
      </section>

      <HonestyBeat />
      <ClosingCta />
    </div>
  );
}
