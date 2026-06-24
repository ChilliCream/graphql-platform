import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Release Safety, Guardrail Constellation | ChilliCream",
  description:
    "Safe GraphQL schema evolution with a registry, breaking-change classification (safe, dangerous, breaking), and CI gates that stop unsafe releases before any published client ships against them.",
  keywords: [
    "safe GraphQL schema evolution",
    "GraphQL release safety",
    "schema registry",
    "breaking change classification",
    "CI gates",
    "published clients affected",
    "schema diff",
    "deprecated field",
    "additive change",
    "schema version history",
  ],
  openGraph: {
    title: "Guardrail Constellation, Release Safety",
    description:
      "A constellation of SAFE, DANGEROUS, and BREAKING wired into one CI gate. Safe GraphQL schema evolution that catches breaking changes before they ship.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/* Local tokens                                                        */
/* Single accent reserved for the breaking motif.                       */
/* ------------------------------------------------------------------ */

const CORAL = "#f0786a";
const CORAL_SOFT = "rgba(240, 120, 106, 0.14)";
const CORAL_LINE = "rgba(240, 120, 106, 0.55)";

type ChangeStatus = "safe" | "dangerous" | "breaking";

interface StatusMeta {
  readonly label: string;
  readonly text: string;
  readonly bg: string;
  readonly ring: string;
  readonly dot: string;
}

const STATUS_META: Record<ChangeStatus, StatusMeta> = {
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
  readonly size?: "sm" | "md";
}

function StatusChip({ status, size = "sm" }: StatusChipProps) {
  const meta = STATUS_META[status];
  const sizing =
    size === "md" ? "px-2.5 py-1 text-[0.7rem]" : "px-1.5 py-0.5 text-[0.6rem]";
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] font-mono font-semibold tracking-[0.14em] ring-1 ring-inset ${meta.bg} ${meta.text} ${meta.ring} ${sizing}`}
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

interface CoralUnderlineProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function CoralUnderline({ children, className = "" }: CoralUnderlineProps) {
  return (
    <span
      className={`relative inline-block ${className}`}
      style={
        {
          backgroundImage: `linear-gradient(${CORAL}, ${CORAL})`,
          backgroundRepeat: "no-repeat",
          backgroundPosition: "0 100%",
          backgroundSize: "100% 1px",
          paddingBottom: "0.18em",
        } as CSSProperties
      }
    >
      {children}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* HERO: giant constellation SVG + sparse left copy                    */
/* ------------------------------------------------------------------ */

function ConstellationSvg() {
  return (
    <svg
      viewBox="0 0 720 720"
      className="h-full w-full"
      role="img"
      aria-label="A schema change orbit showing safe, dangerous and breaking satellites wired into a central validate-then-publish hub"
    >
      <defs>
        <radialGradient id="cc-hub-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="rgba(124, 146, 198, 0.35)" />
          <stop offset="100%" stopColor="rgba(124, 146, 198, 0)" />
        </radialGradient>
        <radialGradient id="cc-coral-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="rgba(240, 120, 106, 0.45)" />
          <stop offset="100%" stopColor="rgba(240, 120, 106, 0)" />
        </radialGradient>
      </defs>

      {/* Orbit rings */}
      <circle
        cx="360"
        cy="360"
        r="260"
        fill="none"
        stroke="rgba(245, 241, 234, 0.08)"
        strokeWidth="1"
      />
      <circle
        cx="360"
        cy="360"
        r="180"
        fill="none"
        stroke="rgba(245, 241, 234, 0.06)"
        strokeWidth="1"
      />
      <circle
        cx="360"
        cy="360"
        r="100"
        fill="none"
        stroke="rgba(245, 241, 234, 0.05)"
        strokeWidth="1"
      />

      {/* Hub glow */}
      <circle cx="360" cy="360" r="160" fill="url(#cc-hub-glow)" />

      {/* Connector: hub -> SAFE (top) */}
      <path
        d="M360 280 C 360 220, 420 180, 500 140"
        stroke="rgba(245, 241, 234, 0.18)"
        strokeWidth="1.5"
        fill="none"
      />
      <circle
        cx="500"
        cy="140"
        r="6"
        fill="oklch(76.5% 0.177 163.223)"
        opacity="0.9"
      />

      {/* Connector: hub -> DANGEROUS (right) */}
      <path
        d="M420 380 C 500 400, 560 420, 620 440"
        stroke="rgba(245, 241, 234, 0.18)"
        strokeWidth="1.5"
        fill="none"
      />
      <circle
        cx="620"
        cy="440"
        r="6"
        fill="oklch(82.8% 0.189 84.429)"
        opacity="0.9"
      />

      {/* Connector: hub -> BREAKING (bottom-left). Severed by coral mark. */}
      <path
        d="M320 410 C 260 470, 220 540, 180 600"
        stroke={CORAL_LINE}
        strokeWidth="1.6"
        fill="none"
        strokeDasharray="6 6"
      />
      {/* Severed-link mark */}
      <g transform="translate(245 510) rotate(135)">
        <line
          x1="-14"
          y1="0"
          x2="14"
          y2="0"
          stroke={CORAL}
          strokeWidth="2.5"
          strokeLinecap="round"
        />
        <line
          x1="0"
          y1="-14"
          x2="0"
          y2="14"
          stroke={CORAL}
          strokeWidth="2.5"
          strokeLinecap="round"
        />
      </g>

      {/* Hub */}
      <circle
        cx="360"
        cy="360"
        r="78"
        fill="#0c1322"
        stroke="rgba(245, 241, 234, 0.18)"
        strokeWidth="1.2"
      />
      <text
        x="360"
        y="350"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        letterSpacing="2"
        fill="#62748e"
      >
        CI GATE
      </text>
      <text
        x="360"
        y="372"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="13"
        fill="#f5f0ea"
        fontWeight="600"
      >
        validate
      </text>
      <text
        x="360"
        y="390"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="13"
        fill="#f5f0ea"
        fontWeight="600"
      >
        publish
      </text>

      {/* SAFE satellite */}
      <circle
        cx="500"
        cy="140"
        r="58"
        fill="#0c1322"
        stroke="oklch(76.5% 0.177 163.223 / 0.45)"
        strokeWidth="1.4"
      />
      <text
        x="500"
        y="132"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        letterSpacing="2"
        fill="oklch(76.5% 0.177 163.223)"
        fontWeight="600"
      >
        SAFE
      </text>
      <text
        x="500"
        y="152"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="rgba(245, 241, 234, 0.62)"
      >
        additive
      </text>

      {/* DANGEROUS satellite */}
      <circle
        cx="620"
        cy="440"
        r="58"
        fill="#0c1322"
        stroke="oklch(82.8% 0.189 84.429 / 0.45)"
        strokeWidth="1.4"
      />
      <text
        x="620"
        y="432"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        letterSpacing="2"
        fill="oklch(82.8% 0.189 84.429)"
        fontWeight="600"
      >
        DANGEROUS
      </text>
      <text
        x="620"
        y="452"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="rgba(245, 241, 234, 0.62)"
      >
        deprecate
      </text>

      {/* BREAKING satellite */}
      <circle
        cx="180"
        cy="600"
        r="62"
        fill="#0c1322"
        stroke={CORAL_LINE}
        strokeWidth="1.6"
      />
      <circle cx="180" cy="600" r="90" fill="url(#cc-coral-glow)" />
      <text
        x="180"
        y="592"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        letterSpacing="2"
        fill={CORAL}
        fontWeight="700"
      >
        BREAKING
      </text>
      <text
        x="180"
        y="612"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="rgba(245, 241, 234, 0.62)"
      >
        stopped
      </text>

      {/* A few quiet star sparks */}
      <circle cx="120" cy="220" r="2" fill="rgba(245, 241, 234, 0.4)" />
      <circle cx="640" cy="260" r="1.5" fill="rgba(245, 241, 234, 0.3)" />
      <circle cx="280" cy="120" r="1.5" fill="rgba(245, 241, 234, 0.3)" />
      <circle cx="560" cy="600" r="1.5" fill="rgba(245, 241, 234, 0.3)" />
      <circle cx="80" cy="420" r="2" fill="rgba(245, 241, 234, 0.4)" />
    </svg>
  );
}

function HeroSection() {
  return (
    <section className="relative grid items-center gap-10 lg:grid-cols-[minmax(0,0.78fr)_minmax(0,1.22fr)] lg:gap-8">
      <div className="lg:pr-2">
        <Eyebrow>Platform / Release Safety</Eyebrow>
        <h1 className="font-heading text-hero text-cc-heading mt-6 font-bold tracking-tight">
          Catch <CoralUnderline>breaking&nbsp;changes</CoralUnderline>
          <br />
          before they ship.
        </h1>
        <p className="lead text-cc-ink-dim mt-7 max-w-md">
          Safe GraphQL schema evolution, wired into your CI.
        </p>
        <p className="text-body text-cc-prose mt-5 max-w-md leading-relaxed">
          Every edit is classified <SafeWord status="safe">safe</SafeWord>,{" "}
          <SafeWord status="dangerous">dangerous</SafeWord>, or{" "}
          <SafeWord status="breaking">breaking</SafeWord>, then checked against
          the published clients that depend on the contract.
        </p>
        <div className="mt-9 flex flex-wrap items-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/continuous-integration">
            Read the Docs
          </OutlineButton>
        </div>
      </div>
      <div className="relative aspect-square w-full lg:aspect-auto lg:h-[36rem]">
        <ConstellationSvg />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* BAND: oversized classification stamps                               */
/* ------------------------------------------------------------------ */

interface StampDef {
  readonly status: ChangeStatus;
  readonly title: string;
  readonly definition: string;
}

const STAMPS: readonly StampDef[] = [
  {
    status: "safe",
    title: "Additive only.",
    definition:
      "New fields, new types, new arguments with defaults. Existing operations keep returning what they returned yesterday.",
  },
  {
    status: "dangerous",
    title: "Deprecation in flight.",
    definition:
      "The contract still answers, but a field is marked for removal. Buys a release for clients to move off.",
  },
  {
    status: "breaking",
    title: "Stops at the gate.",
    definition:
      "A change that would make a published client query fail. The check blocks merge until it is reshaped.",
  },
];

function ClassificationBand() {
  return (
    <section className="bg-cc-surface border-cc-card-border rounded-2xl border px-6 py-10 sm:px-10 sm:py-12">
      <Eyebrow>The vocabulary</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 max-w-2xl font-semibold tracking-tight">
        Three stamps, one shared language for schema change.
      </h2>
      <div className="border-cc-card-border mt-9 grid gap-0 border-t border-b sm:grid-cols-3">
        {STAMPS.map((s, i) => (
          <div
            key={s.status}
            className={`border-cc-card-border px-5 py-6 ${
              i < STAMPS.length - 1 ? "sm:border-r" : ""
            } ${i > 0 ? "border-t sm:border-t-0" : ""}`}
          >
            <StatusChip status={s.status} size="md" />
            <div className="text-cc-heading mt-5 font-mono text-[0.95rem] font-semibold">
              {s.title}
            </div>
            <p className="text-cc-ink-dim mt-2 text-[0.88rem] leading-relaxed">
              {s.definition}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* BAND: full-width CI verdict card with coral FAIL ribbon              */
/* ------------------------------------------------------------------ */

function FailRibbon() {
  return (
    <div
      aria-hidden
      className="absolute top-0 bottom-0 left-0 flex w-8 items-center justify-center"
      style={{ backgroundColor: CORAL_SOFT }}
    >
      <span
        className="font-mono text-[0.62rem] font-bold tracking-[0.32em]"
        style={{
          color: CORAL,
          writingMode: "vertical-rl",
          transform: "rotate(180deg)",
        }}
      >
        FAIL
      </span>
    </div>
  );
}

interface RuleRowProps {
  readonly verdict: "fail" | "pass";
  readonly name: string;
  readonly detail: string;
}

function RuleRow({ verdict, name, detail }: RuleRowProps) {
  const isFail = verdict === "fail";
  return (
    <div className="border-cc-card-border flex items-center gap-3 border-t px-5 py-4 first:border-t-0">
      {isFail ? (
        <span
          className="flex h-5 w-5 items-center justify-center rounded-full"
          style={{ backgroundColor: CORAL_SOFT, color: CORAL }}
          aria-hidden
        >
          <svg viewBox="0 0 16 16" width={11} height={11}>
            <path
              d="M4 4 L12 12 M12 4 L4 12"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
            />
          </svg>
        </span>
      ) : (
        <span className="bg-cc-success/15 text-cc-success ring-cc-success/25 flex h-5 w-5 items-center justify-center rounded-full ring-1 ring-inset">
          <CheckIcon size={11} />
        </span>
      )}
      <span
        className={`text-[0.86rem] font-medium ${
          isFail ? "text-cc-heading" : "text-cc-prose"
        }`}
      >
        {name}
      </span>
      <span className="text-cc-ink-dim ml-auto font-mono text-[0.7rem]">
        {detail}
      </span>
    </div>
  );
}

function VerdictBand() {
  return (
    <section>
      <Eyebrow>The verdict</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 max-w-2xl font-semibold tracking-tight">
        A failing check is the whole point.
      </h2>
      <div className="bg-cc-surface border-cc-card-border relative mt-8 overflow-hidden rounded-2xl border">
        <FailRibbon />
        <div className="pl-10">
          <div className="border-cc-card-border flex flex-wrap items-center gap-3 border-b px-5 py-4">
            <span
              className="rounded-md px-2.5 py-1 font-mono text-[0.66rem] font-semibold tracking-wide ring-1 ring-inset"
              style={{
                backgroundColor: CORAL_SOFT,
                color: CORAL,
                boxShadow: `inset 0 0 0 1px ${CORAL_LINE}`,
              }}
            >
              REGISTRY CHECK FAILED
            </span>
            <span className="text-cc-heading text-[0.86rem] font-medium">
              orders-api
            </span>
            <span className="text-cc-nav-label">/</span>
            <span className="text-cc-prose font-mono text-[0.78rem]">
              PR #482 Add Money type
            </span>
            <span className="text-cc-ink-dim ml-auto font-mono text-[0.68rem]">
              1 breaking · 2 safe
            </span>
          </div>
          <RuleRow
            verdict="fail"
            name="Schema validation, breaking change"
            detail="Order.total removed"
          />
          <RuleRow
            verdict="pass"
            name="Schema validation, additive"
            detail="Money, totalAmount added"
          />
          <RuleRow
            verdict="pass"
            name="Client compatibility, web + internal-admin"
            detail="operations unaffected"
          />
        </div>
        <div className="border-cc-card-border bg-cc-bg/40 border-t px-5 py-3 pl-10">
          <span className="text-cc-ink-dim font-mono text-[0.72rem]">
            Merging is blocked until checks pass.
          </span>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* STRIP: 3-up published-clients impact grid                           */
/* ------------------------------------------------------------------ */

interface ClientCardData {
  readonly name: string;
  readonly env: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "queued";
}

const CLIENT_CARDS: readonly ClientCardData[] = [
  { name: "web", env: "production", ok: 5, total: 5, status: "ok" },
  { name: "mobile", env: "production", ok: 3, total: 5, status: "risk" },
  { name: "partner", env: "sandbox", ok: 0, total: 4, status: "queued" },
];

interface SegmentBarProps {
  readonly ok: number;
  readonly total: number;
  readonly status: ClientCardData["status"];
}

function SegmentBar({ ok, total, status }: SegmentBarProps) {
  const color =
    status === "ok"
      ? "bg-cc-success"
      : status === "risk"
        ? "bg-cc-warning"
        : "bg-cc-nav-label/50";
  return (
    <span className="flex gap-1">
      {Array.from({ length: total }).map((_, i) => (
        <span
          key={i}
          className={`h-1.5 w-6 rounded-[2px] ${
            i < ok ? color : "bg-cc-ink-faint"
          }`}
        />
      ))}
    </span>
  );
}

interface ClientCardProps {
  readonly data: ClientCardData;
}

function ClientCard({ data }: ClientCardProps) {
  const isRisk = data.status === "risk";
  const statusLabel: Record<ClientCardData["status"], string> = {
    ok: "OK",
    risk: "at risk",
    queued: "queued",
  };
  const statusColor: Record<ClientCardData["status"], string> = {
    ok: "text-cc-success",
    risk: "text-cc-warning",
    queued: "text-cc-nav-label",
  };
  return (
    <div className="bg-cc-surface border-cc-card-border rounded-xl border p-5">
      <div className="flex items-center gap-2">
        <span className="text-cc-heading font-mono text-[0.82rem]">
          {data.name}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.62rem]">
          {data.env}
        </span>
      </div>
      {isRisk ? (
        <CoralUnderline className="mt-1">
          <span className="text-cc-ink-dim text-[0.74rem]">
            published client at risk
          </span>
        </CoralUnderline>
      ) : (
        <span className="text-cc-ink-dim mt-1 block text-[0.74rem]">
          {data.status === "ok" ? "operations clear" : "awaiting registration"}
        </span>
      )}
      <div className="mt-5 flex items-center gap-3">
        <SegmentBar ok={data.ok} total={data.total} status={data.status} />
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">
          {data.ok}/{data.total}
        </span>
      </div>
      <div
        className={`mt-4 font-mono text-[0.72rem] font-semibold ${statusColor[data.status]}`}
      >
        {statusLabel[data.status]}
      </div>
    </div>
  );
}

function ImpactStrip() {
  return (
    <section>
      <Eyebrow>Published clients affected</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 max-w-2xl font-semibold tracking-tight">
        See who a change touches, by name.
      </h2>
      <p className="text-body text-cc-prose mt-4 max-w-2xl leading-relaxed">
        The client registry tracks the operations your published clients
        actually run. Before a change ships, each client reports passing
        operations, not a global verdict.
      </p>
      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        {CLIENT_CARDS.map((c) => (
          <ClientCard key={c.name} data={c} />
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* RAIL: validate -> publish with coral branch into blocked            */
/* ------------------------------------------------------------------ */

function FlowRailSvg() {
  return (
    <svg
      viewBox="0 0 880 140"
      className="h-auto w-full"
      role="img"
      aria-label="A horizontal flow rail from change to validate to publish, with a coral branch peeling off into blocked"
    >
      {/* Backbone */}
      <line
        x1="60"
        y1="70"
        x2="820"
        y2="70"
        stroke="rgba(245, 241, 234, 0.18)"
        strokeWidth="1.5"
      />

      {/* Coral branch off validate -> blocked */}
      <path
        d="M450 70 C 540 70, 560 120, 660 120"
        stroke={CORAL}
        strokeWidth="1.6"
        fill="none"
        strokeDasharray="5 5"
      />

      {/* Nodes */}
      {/* change */}
      <circle
        cx="120"
        cy="70"
        r="22"
        fill="#0c1322"
        stroke="rgba(245, 241, 234, 0.22)"
        strokeWidth="1.3"
      />
      <text
        x="120"
        y="115"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        fill="rgba(245, 241, 234, 0.62)"
      >
        change
      </text>
      <text
        x="120"
        y="40"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        letterSpacing="1.5"
        fill="#62748e"
      >
        01
      </text>

      {/* validate */}
      <circle
        cx="450"
        cy="70"
        r="26"
        fill="#0c1322"
        stroke="oklch(82.8% 0.189 84.429 / 0.55)"
        strokeWidth="1.4"
      />
      <text
        x="450"
        y="115"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        fill="rgba(245, 241, 234, 0.78)"
      >
        validate
      </text>
      <text
        x="450"
        y="40"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        letterSpacing="1.5"
        fill="#62748e"
      >
        02
      </text>

      {/* publish */}
      <circle
        cx="780"
        cy="70"
        r="22"
        fill="#0c1322"
        stroke="oklch(76.5% 0.177 163.223 / 0.55)"
        strokeWidth="1.4"
      />
      <text
        x="780"
        y="115"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        fill="rgba(245, 241, 234, 0.78)"
      >
        publish
      </text>
      <text
        x="780"
        y="40"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        letterSpacing="1.5"
        fill="#62748e"
      >
        03a
      </text>

      {/* blocked */}
      <circle
        cx="680"
        cy="120"
        r="14"
        fill="#0c1322"
        stroke={CORAL_LINE}
        strokeWidth="1.6"
      />
      <text
        x="710"
        y="124"
        textAnchor="start"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        fill={CORAL}
        fontWeight="600"
      >
        blocked
      </text>
      <text
        x="680"
        y="98"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        letterSpacing="1.5"
        fill="#62748e"
      >
        03b
      </text>
    </svg>
  );
}

function FlowRailBand() {
  return (
    <section className="bg-cc-surface border-cc-card-border rounded-2xl border px-6 py-10 sm:px-10 sm:py-12">
      <Eyebrow>The order is the guarantee</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 max-w-2xl font-semibold tracking-tight">
        Validate, then publish. Never the other way around.
      </h2>
      <div className="mt-9">
        <FlowRailSvg />
      </div>
      <p className="text-body text-cc-prose mt-6 max-w-3xl leading-relaxed">
        Nothing reaches publish until it clears validate. A breaking change
        never advances, it peels off into blocked with its reason attached.
      </p>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* TIMELINE: schema history with coral marker on v14                   */
/* ------------------------------------------------------------------ */

interface VersionPoint {
  readonly v: string;
  readonly note: string;
  readonly status: ChangeStatus;
  readonly motif?: boolean;
}

const VERSIONS: readonly VersionPoint[] = [
  { v: "v12", note: "add Cart.discount", status: "safe" },
  { v: "v13", note: "deprecate Order.placedAt", status: "dangerous" },
  {
    v: "v14",
    note: "remove Order.total, blocked",
    status: "breaking",
    motif: true,
  },
  { v: "v14", note: "add Order.totalAmount", status: "safe" },
  { v: "v15", note: "drop Order.total, usage cleared", status: "dangerous" },
];

function VersionTimeline() {
  return (
    <section className="bg-cc-surface border-cc-card-border rounded-2xl border px-6 py-10 sm:px-10 sm:py-12">
      <Eyebrow>The record</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 max-w-2xl font-semibold tracking-tight">
        Every version, every classification, kept.
      </h2>
      <p className="text-body text-cc-prose mt-4 max-w-2xl leading-relaxed">
        The registry keeps the full history of your schema with each change
        classified in place. You can see exactly when a contract shifted, why it
        was safe, and how a risky change was reshaped before it shipped.
      </p>
      <ol className="mt-9 space-y-0">
        {VERSIONS.map((p, i) => {
          const meta = STATUS_META[p.status];
          const isMotif = p.motif === true;
          return (
            <li
              key={i}
              className="border-cc-card-border relative flex flex-wrap items-center gap-4 border-t py-4 first:border-t-0"
            >
              <span
                className={`flex h-5 w-5 items-center justify-center rounded-full ${meta.bg} ring-1 ring-inset ${meta.ring}`}
              >
                <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
              </span>
              <span className="text-cc-heading w-12 shrink-0 font-mono text-[0.78rem] font-semibold">
                {p.v}
              </span>
              <span className="text-cc-prose min-w-0 flex-1 font-mono text-[0.8rem]">
                {isMotif ? <CoralUnderline>{p.note}</CoralUnderline> : p.note}
              </span>
              <StatusChip status={p.status} />
              {isMotif && (
                <span
                  className="font-mono text-[0.62rem] font-semibold tracking-[0.18em]"
                  style={{ color: CORAL }}
                >
                  CALLBACK
                </span>
              )}
            </li>
          );
        })}
      </ol>
      <p className="text-cc-ink-dim mt-7 max-w-3xl font-mono text-[0.74rem] leading-relaxed">
        The breaking removal at <span className="text-cc-danger">v14</span>{" "}
        never shipped. It was reshaped as an additive change, deprecated, and
        only dropped at <span className="text-cc-warning">v15</span> once client
        usage cleared.
      </p>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* HONESTY: 2-up calm cards                                            */
/* ------------------------------------------------------------------ */

interface HonestyCardProps {
  readonly tone: "success" | "warning";
  readonly head: string;
  readonly body: string;
}

function HonestyCard({ tone, head, body }: HonestyCardProps) {
  const topBar = tone === "success" ? "bg-cc-success" : "bg-cc-warning";
  return (
    <div className="bg-cc-surface border-cc-card-border relative overflow-hidden rounded-xl border px-6 py-7">
      <span
        aria-hidden
        className={`absolute top-0 right-0 left-0 h-[2px] ${topBar}`}
      />
      <Eyebrow>{head}</Eyebrow>
      <p className="text-cc-ink-dim mt-4 text-[0.92rem] leading-relaxed">
        {body}
      </p>
    </div>
  );
}

function HonestySection() {
  return (
    <section>
      <Eyebrow>Honest about its edges</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 max-w-2xl font-semibold tracking-tight">
        A safety net, not a blindfold.
      </h2>
      <div className="mt-8 grid gap-4 sm:grid-cols-2">
        <HonestyCard
          tone="success"
          head="What this stops"
          body="Releases that would break a published client are blocked before merge, with the breaking line and reason in hand. Strawberry Shake regenerates clients via MSBuild codegen, so contract drift shows up as build feedback you cannot miss."
        />
        <HonestyCard
          tone="warning"
          head="What it needs"
          body="A client is only guarded once its operations are registered. Unregistered traffic is outside the net. The gate reports published clients affected, not a guess about consumers it has never seen."
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CLOSING CTA: quiet centered strip with coral underline              */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="py-6 text-center">
      <h3 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
        <CoralUnderline>
          Put a safety net under every schema change.
        </CoralUnderline>
      </h3>
      <p className="lead text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Classify, validate, and gate your releases so breaking changes stop at
        the door, not in your users&rsquo; hands.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
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

export default function ReleaseSafetyPreviewV4() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <ClassificationBand />
      <VerdictBand />
      <ImpactStrip />
      <FlowRailBand />
      <VersionTimeline />
      <HonestySection />
      <ClosingCta />
    </div>
  );
}
