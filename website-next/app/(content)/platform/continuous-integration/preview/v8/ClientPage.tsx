"use client";

import { useRef, type CSSProperties, type ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Scene tokens (cc-* dark, single cc-accent teal)                     */
/* The wall is a corkboard: every tile is a record the registry filed. */
/* ------------------------------------------------------------------ */

const SUNK = "#0b1525";
const DEEP = "#0d1b30";

/* 28px square dotted grid behind the mosaic, very low contrast. */
const WALL_BG: CSSProperties = {
  backgroundColor: "var(--color-cc-bg)",
  backgroundImage:
    "radial-gradient(rgba(124,146,198,0.05) 1px, transparent 1px)",
  backgroundSize: "28px 28px",
};

/* One soft accent wash at the hero top-left. */
const HERO_WASH: CSSProperties = {
  backgroundImage:
    "radial-gradient(60% 90% at 0% 0%, rgba(94,234,212,0.10), transparent 60%)",
};

/* ------------------------------------------------------------------ */
/* Tile primitive                                                      */
/* span encodes weight: 1x1 fact, 2x1 row, 1x2 column, 2x2 anchor.     */
/* ------------------------------------------------------------------ */

type TileSpan = "1x1" | "2x1" | "1x2" | "2x2";

const SPAN_CLASS: Record<TileSpan, string> = {
  "1x1": "sm:col-span-1 sm:row-span-1",
  "2x1": "sm:col-span-2 sm:row-span-1",
  "1x2": "sm:col-span-1 sm:row-span-2",
  "2x2": "sm:col-span-2 sm:row-span-2",
};

interface TileProps {
  readonly span: TileSpan;
  readonly index: number;
  readonly accentBar?: "success" | "warning";
  readonly children: ReactNode;
  readonly className?: string;
}

function Tile({ span, index, accentBar, children, className = "" }: TileProps) {
  const reduce = useReducedMotion();
  const bar =
    accentBar === "success"
      ? "bg-cc-success"
      : accentBar === "warning"
        ? "bg-cc-warning"
        : null;
  return (
    <motion.div
      className={`group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex flex-col overflow-hidden rounded-xl border p-5 transition-[transform,border-color] duration-200 hover:-translate-y-0.5 ${SPAN_CLASS[span]} ${className}`}
      initial={reduce ? false : { opacity: 0, y: 14 }}
      whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-40px" }}
      transition={{ duration: 0.45, delay: index * 0.04, ease: "easeOut" }}
    >
      {bar !== null && (
        <span
          className={`absolute top-4 bottom-4 left-0 w-[3px] rounded-full ${bar}`}
          aria-hidden
        />
      )}
      {children}
    </motion.div>
  );
}

interface EyebrowProps {
  readonly children: ReactNode;
}

function TileEyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Verdict pill (classification color language)                        */
/* ------------------------------------------------------------------ */

type Verdict = "safe" | "dangerous" | "breaking";

const VERDICT_META: Record<
  Verdict,
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

interface VerdictPillProps {
  readonly verdict: Verdict;
  readonly count?: string;
}

function VerdictPill({ verdict, count }: VerdictPillProps) {
  const m = VERDICT_META[verdict];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] ring-1 ring-inset ${m.bg} ${m.text} ${m.ring}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${m.dot}`} />
      {m.label}
      {count !== undefined && (
        <span className="text-cc-ink-dim font-normal">{count}</span>
      )}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Hero band                                                           */
/* ------------------------------------------------------------------ */

interface LegendDotProps {
  readonly tone: "success" | "warning" | "danger" | "pending";
  readonly label: string;
}

function LegendDot({ tone, label }: LegendDotProps) {
  const dot =
    tone === "success"
      ? "bg-cc-success"
      : tone === "warning"
        ? "bg-cc-warning"
        : tone === "danger"
          ? "bg-cc-danger"
          : "bg-cc-nav-label/60";
  return (
    <span className="text-cc-ink-dim inline-flex items-center gap-2 font-mono text-[0.66rem]">
      <span className={`h-2 w-2 rounded-full ${dot}`} />
      {label}
    </span>
  );
}

function HeroBand() {
  return (
    <section className="relative overflow-hidden">
      <div aria-hidden className="absolute inset-0 -z-10" style={HERO_WASH} />
      <div className="grid items-end gap-10 lg:grid-cols-[minmax(0,1fr)_auto]">
        <div>
          <TileEyebrow>Platform / Continuous Integration</TileEyebrow>
          <h1 className="font-heading text-hero text-cc-heading mt-5 font-bold tracking-tight">
            Every change, pinned
            <br />
            to the registry wall.
          </h1>
          <p className="lead text-cc-ink-dim mt-6 max-w-2xl">
            Validate, upload, publish, and deploy every GraphQL change through
            one Nitro CLI pipeline. The wall files a record for each schema
            change, client, environment, and runner step, so you can scan it and
            know exactly where a release stands.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-x-6 gap-y-2">
            <LegendDot tone="success" label="passed" />
            <LegendDot tone="warning" label="running" />
            <LegendDot tone="danger" label="blocked" />
            <LegendDot tone="pending" label="pending" />
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Tile bodies                                                         */
/* ------------------------------------------------------------------ */

interface FactTileProps {
  readonly eyebrow: string;
  readonly value: string;
  readonly sub: string;
  readonly index: number;
  readonly valueClass?: string;
}

function FactTile({
  eyebrow,
  value,
  sub,
  index,
  valueClass = "text-cc-heading",
}: FactTileProps) {
  return (
    <Tile span="1x1" index={index}>
      <TileEyebrow>{eyebrow}</TileEyebrow>
      <div
        className={`font-heading mt-auto pt-6 text-[1.55rem] leading-none font-bold tracking-tight ${valueClass}`}
      >
        {value}
      </div>
      <div className="text-cc-ink-dim mt-2 font-mono text-[0.62rem]">{sub}</div>
    </Tile>
  );
}

/* A diff line for the Validate anchor. */
interface DiffLineProps {
  readonly sign: "+" | "~" | "-";
  readonly verdict: Verdict;
  readonly text: string;
}

function DiffLine({ sign, verdict, text }: DiffLineProps) {
  const m = VERDICT_META[verdict];
  return (
    <div
      className="border-cc-card-border flex items-start gap-2 rounded-md border px-3 py-2 font-mono text-[0.72rem]"
      style={{ backgroundColor: SUNK }}
    >
      <span className={`shrink-0 font-semibold ${m.text}`}>{sign}</span>
      <span className="text-cc-prose min-w-0 break-words">{text}</span>
    </div>
  );
}

function ValidateAnchor({ index }: { readonly index: number }) {
  return (
    <Tile span="2x2" index={index}>
      <div className="flex items-center justify-between">
        <TileEyebrow>01 / validate</TileEyebrow>
        <VerdictPill verdict="breaking" />
      </div>
      <h2 className="font-heading text-cc-heading mt-3 text-[1.3rem] font-semibold tracking-tight">
        Classify before you commit.
      </h2>
      <p className="text-cc-ink-dim mt-2 text-[0.84rem] leading-relaxed">
        Every change is stamped safe, dangerous, or breaking, checked against
        the operations your published clients send.
      </p>
      <div className="mt-4 flex flex-col gap-2">
        <DiffLine sign="+" verdict="safe" text="Order.totalAmount: Money!" />
        <DiffLine
          sign="~"
          verdict="dangerous"
          text="Order.placedAt @deprecated"
        />
        <DiffLine
          sign="-"
          verdict="breaking"
          text="Order.total: Float! removed"
        />
      </div>
      <div className="border-cc-card-border text-cc-nav-label mt-auto flex items-center justify-between border-t pt-3 font-mono text-[0.62rem]">
        <span>classification complete</span>
        <span>1 safe / 1 dangerous / 1 breaking</span>
      </div>
    </Tile>
  );
}

function ClassificationTile({ index }: { readonly index: number }) {
  return (
    <Tile span="2x1" index={index}>
      <TileEyebrow>change / classification</TileEyebrow>
      <div className="mt-3 flex flex-wrap items-center gap-2">
        <VerdictPill verdict="safe" count="1" />
        <VerdictPill verdict="dangerous" count="1" />
        <VerdictPill verdict="breaking" count="1" />
      </div>
      <div
        className="border-cc-card-border mt-auto flex items-center justify-between gap-3 rounded-md border px-3 py-2.5 font-mono text-[0.72rem]"
        style={{ backgroundColor: SUNK }}
      >
        <span className="text-cc-danger truncate">Order.total: Float!</span>
        <span className="text-cc-ink-dim shrink-0">
          published clients affected: 3
        </span>
      </div>
    </Tile>
  );
}

function PublishCliTile({ index }: { readonly index: number }) {
  return (
    <Tile span="1x2" index={index} className="p-0">
      <div
        className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5"
        style={{ backgroundColor: DEEP }}
      >
        <span className="flex gap-1.5" aria-hidden>
          <span className="bg-cc-danger/60 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-warning/60 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-success/60 h-2.5 w-2.5 rounded-full" />
        </span>
        <span className="text-cc-nav-label ml-1 font-mono text-[0.6rem] tracking-[0.16em] uppercase">
          runner / cli
        </span>
      </div>
      <div className="flex flex-1 flex-col gap-1.5 px-4 py-4 font-mono text-[0.72rem] leading-relaxed">
        <div>
          <span className="text-cc-info">dotnet nitro</span>
          <span className="text-cc-prose"> schema publish \</span>
        </div>
        <div className="text-cc-prose">
          {"  "}--stage <span className="text-cc-tip">staging</span> \
        </div>
        <div className="text-cc-prose">
          {"  "}--tag{" "}
          <span className="text-cc-tip">
            {"$"}
            {"{"}GITHUB_SHA{"}"}
          </span>
        </div>
        <div className="bg-cc-card-border my-3 h-px" />
        <div className="text-cc-success">[ok] validated against clients</div>
        <div className="text-cc-success">[ok] uploaded as schema v14</div>
        <div className="text-cc-warning">[warn] dangerous: @deprecated</div>
        <div className="text-cc-nav-label mt-auto pt-2">
          tag a1f2e9c / env staging / run #482
        </div>
      </div>
    </Tile>
  );
}

function PublishAnchor({ index }: { readonly index: number }) {
  const reduce = useReducedMotion();
  return (
    <Tile span="2x2" index={index}>
      <div className="flex items-center justify-between">
        <TileEyebrow>03 / publish</TileEyebrow>
        <span className="text-cc-warning inline-flex items-center gap-1.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em]">
          {reduce ? (
            <span className="bg-cc-accent h-1.5 w-1.5 rounded-full" />
          ) : (
            <motion.span
              className="bg-cc-accent h-1.5 w-1.5 rounded-full"
              animate={{ opacity: [1, 0.25, 1], scale: [1, 0.8, 1] }}
              transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
            />
          )}
          ACTIVE
        </span>
      </div>
      <h2 className="font-heading text-cc-heading mt-3 text-[1.3rem] font-semibold tracking-tight">
        Approval gate, then promote.
      </h2>
      <p className="text-cc-ink-dim mt-2 text-[0.84rem] leading-relaxed">
        Publish promotes the staged schema for an environment. Gates can require
        a reviewer on dangerous or breaking changes.
      </p>
      <div
        className="border-cc-card-border mt-4 rounded-md border px-4 py-3"
        style={{ backgroundColor: SUNK }}
      >
        <div className="flex items-center gap-3">
          <span
            className="border-cc-card-border text-cc-nav-label flex h-8 w-8 shrink-0 items-center justify-center rounded-full border font-mono text-[0.6rem]"
            style={{ backgroundColor: DEEP }}
            aria-hidden
          >
            RW
          </span>
          <div className="min-w-0">
            <div className="text-cc-heading font-mono text-[0.72rem]">
              awaiting reviewer
            </div>
            <div className="text-cc-nav-label font-mono text-[0.62rem]">
              approval gate / dangerous change
            </div>
          </div>
        </div>
        <div className="mt-3 flex items-center gap-3 font-mono text-[0.74rem]">
          <span className="text-cc-ink-dim">schema v14</span>
          <span className="text-cc-accent" aria-hidden>
            &rarr;
          </span>
          <span className="text-cc-heading">v15</span>
          <span className="text-cc-nav-label ml-auto text-[0.62rem]">
            promote
          </span>
        </div>
      </div>
      <div className="border-cc-card-border text-cc-nav-label mt-auto flex items-center justify-between border-t pt-3 font-mono text-[0.62rem]">
        <span>each env keeps its own version</span>
        <span>full timeline recorded</span>
      </div>
    </Tile>
  );
}

/* ------------------------------------------------------------------ */
/* Environments tile (2x1) + rollback (1x2)                            */
/* ------------------------------------------------------------------ */

interface EnvPill {
  readonly env: string;
  readonly version: string;
  readonly tone: "success" | "warning" | "pending";
}

const ENV_PILLS: readonly EnvPill[] = [
  { env: "dev", version: "v14", tone: "success" },
  { env: "qa", version: "v14", tone: "success" },
  { env: "staging", version: "v14", tone: "warning" },
  { env: "production", version: "v13", tone: "pending" },
];

function EnvironmentsTile({ index }: { readonly index: number }) {
  return (
    <Tile span="2x1" index={index}>
      <TileEyebrow>env / per-environment workflow</TileEyebrow>
      <div className="mt-auto grid grid-cols-2 gap-2 pt-4 sm:grid-cols-4">
        {ENV_PILLS.map((p) => {
          const dot =
            p.tone === "success"
              ? "bg-cc-success"
              : p.tone === "warning"
                ? "bg-cc-warning"
                : "bg-cc-nav-label/60";
          return (
            <div
              key={p.env}
              className="border-cc-card-border rounded-md border px-2.5 py-2"
              style={{ backgroundColor: SUNK }}
            >
              <div className="flex items-center gap-1.5">
                <span className={`h-1.5 w-1.5 rounded-full ${dot}`} />
                <span className="text-cc-heading font-mono text-[0.66rem]">
                  {p.env}
                </span>
              </div>
              <div className="text-cc-nav-label mt-1.5 font-mono text-[0.6rem]">
                {p.version}
              </div>
            </div>
          );
        })}
      </div>
    </Tile>
  );
}

function RollbackTile({ index }: { readonly index: number }) {
  return (
    <Tile span="1x2" index={index}>
      <TileEyebrow>04 / rollback</TileEyebrow>
      <h3 className="font-heading text-cc-heading mt-3 text-[1.05rem] font-semibold">
        Roll back by re-publishing.
      </h3>
      <p className="text-cc-ink-dim mt-2 text-[0.82rem] leading-relaxed">
        Rollback is a re-publish of a prior tagged version. Deployment stays on
        the same runner that validated and published.
      </p>
      <div
        className="border-cc-card-border mt-auto flex flex-col gap-1.5 rounded-md border px-3 py-3 font-mono text-[0.7rem]"
        style={{ backgroundColor: SUNK }}
      >
        <span className="text-cc-prose">
          <span className="text-cc-info">dotnet nitro</span> schema publish
        </span>
        <span className="text-cc-prose">
          {"  "}--tag <span className="text-cc-tip">v13</span>
        </span>
        <span className="text-cc-nav-label">re-pins the prior contract</span>
      </div>
    </Tile>
  );
}

/* ------------------------------------------------------------------ */
/* Runner-logo tiles + YAML tile                                       */
/* ------------------------------------------------------------------ */

function GithubGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width={22}
      height={22}
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden>
      <path
        fill="#16b9e4"
        d="M9.5 3 3 18.5l4 .5L13.5 8 9.5 3Zm4.5 4 7 14H10l-2-3 4-1L14 7Z"
      />
    </svg>
  );
}

function ShellGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden>
      <path
        d="M5 7 L10 12 L5 17 M12 17 H19"
        stroke="currentColor"
        strokeWidth="1.7"
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="text-cc-accent"
      />
    </svg>
  );
}

interface RunnerTileProps {
  readonly label: string;
  readonly sub: string;
  readonly glyph: ReactNode;
  readonly index: number;
}

function RunnerTile({ label, sub, glyph, index }: RunnerTileProps) {
  return (
    <Tile span="1x1" index={index}>
      <span
        className="border-cc-card-border flex h-10 w-10 items-center justify-center rounded-md border"
        style={{ backgroundColor: DEEP }}
      >
        {glyph}
      </span>
      <div className="text-cc-heading font-heading mt-auto pt-5 text-[0.95rem] font-semibold">
        {label}
      </div>
      <div className="text-cc-nav-label mt-1 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
        {sub}
      </div>
    </Tile>
  );
}

function YamlTile({ index }: { readonly index: number }) {
  return (
    <Tile span="2x1" index={index} className="p-0">
      <div
        className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5"
        style={{ backgroundColor: DEEP }}
      >
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.16em] uppercase">
          runner / one cli, every runner
        </span>
        <span className="bg-cc-hover text-cc-ink-dim ml-auto rounded px-2 py-0.5 font-mono text-[0.6rem]">
          workflow.yml
        </span>
      </div>
      <pre
        className="text-cc-prose flex-1 overflow-x-auto px-4 py-3 font-mono text-[0.7rem] leading-relaxed"
        style={{ backgroundColor: SUNK }}
      >
        <code>{`- name: Publish schema
  run: |
    dotnet nitro schema publish \\
      --stage staging \\
      --tag \${{ github.sha }}`}</code>
      </pre>
    </Tile>
  );
}

/* ------------------------------------------------------------------ */
/* Honesty tiles (1x1, left accent bar)                                */
/* ------------------------------------------------------------------ */

interface HonestyTileProps {
  readonly tone: "success" | "warning";
  readonly head: string;
  readonly body: string;
  readonly index: number;
}

function HonestyTile({ tone, head, body, index }: HonestyTileProps) {
  return (
    <Tile span="1x1" index={index} accentBar={tone}>
      <div className="pl-3">
        <div className="flex items-center gap-1.5">
          <span
            className={
              tone === "success" ? "text-cc-success" : "text-cc-warning"
            }
          >
            <CheckIcon size={11} />
          </span>
          <div className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.14em] uppercase">
            {head}
          </div>
        </div>
        <p className="text-cc-ink-dim mt-2 text-[0.8rem] leading-relaxed">
          {body}
        </p>
      </div>
    </Tile>
  );
}

/* ------------------------------------------------------------------ */
/* Closing CTA tile (full row, brand-spectrum hairline once)           */
/* ------------------------------------------------------------------ */

function ClosingTile({ index }: { readonly index: number }) {
  const reduce = useReducedMotion();
  return (
    <motion.div
      className="border-cc-card-border bg-cc-card-bg relative col-span-1 overflow-hidden rounded-xl border px-6 py-12 text-center sm:col-span-2 lg:col-span-4"
      initial={reduce ? false : { opacity: 0, y: 14 }}
      whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-40px" }}
      transition={{ duration: 0.45, delay: index * 0.04, ease: "easeOut" }}
    >
      {/* Brand-spectrum hairline, the only place it appears on this page. */}
      <span
        aria-hidden
        className="absolute inset-x-0 top-0 h-px"
        style={{
          backgroundImage:
            "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      />
      <TileEyebrow>Pipe ready</TileEyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mx-auto mt-4 max-w-2xl font-bold tracking-tight">
        Make every release a pinned record.
      </h2>
      <p className="text-body text-cc-prose mx-auto mt-5 max-w-xl leading-relaxed">
        Validate, upload, publish, and deploy on the runner you already use.
        Schema and client registry in one place, classification before
        promotion.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/docs/nitro">Get Started</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
    </motion.div>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  const wallRef = useRef<HTMLDivElement>(null);
  return (
    <div className="flex flex-col gap-16 py-6">
      <HeroBand />

      {/* The mosaic: one continuous dense grid covering the page middle. */}
      <div
        ref={wallRef}
        className="rounded-2xl p-4 sm:p-6"
        style={WALL_BG}
        aria-label="Registry wall: pinned records of the release pipeline"
      >
        <div className="grid grid-flow-row-dense auto-rows-[148px] grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {/* Row A: Validate anchor + facts */}
          <ValidateAnchor index={0} />
          <FactTile
            eyebrow="changes"
            value="3"
            sub="classified this run"
            index={1}
          />
          <FactTile
            eyebrow="exit code"
            value="1"
            sub="breaking fails the job"
            index={2}
            valueClass="text-cc-danger"
          />
          <FactTile eyebrow="run id" value="#482" sub="3m 12s" index={3} />
          <FactTile
            eyebrow="registry"
            value="v14"
            sub="schema version"
            index={4}
            valueClass="text-cc-accent"
          />

          {/* Row B: classification + CLI column */}
          <ClassificationTile index={5} />
          <PublishCliTile index={6} />

          {/* Row C: Publish anchor + facts */}
          <PublishAnchor index={7} />
          <FactTile eyebrow="tag" value="a1f2e9c" sub="commit sha" index={8} />
          <FactTile
            eyebrow="approver"
            value="required"
            sub="on dangerous change"
            index={9}
            valueClass="text-cc-warning"
          />
          <FactTile
            eyebrow="env / target"
            value="staging"
            sub="promote on approval"
            index={10}
          />
          <FactTile
            eyebrow="promotion"
            value="v14 -> v15"
            sub="active contract"
            index={11}
            valueClass="text-cc-accent"
          />

          {/* Row D: environments + rollback */}
          <EnvironmentsTile index={12} />
          <RollbackTile index={13} />

          {/* Row E: runners + YAML */}
          <RunnerTile
            label="GitHub Actions"
            sub="workflow step"
            glyph={<GithubGlyph />}
            index={14}
          />
          <RunnerTile
            label="Azure DevOps"
            sub="pipeline task"
            glyph={<AzureGlyph />}
            index={15}
          />
          <RunnerTile
            label="Any shell runner"
            sub="bash, make"
            glyph={<ShellGlyph />}
            index={16}
          />
          <YamlTile index={17} />

          {/* Honesty row */}
          <HonestyTile
            tone="success"
            head="Published clients affected"
            body="Classification runs against operations published clients have registered. The gate names them, not a vague global verdict."
            index={18}
          />
          <HonestyTile
            tone="warning"
            head="Untracked clients are outside the net"
            body="A client is only guarded once its operations are uploaded to the registry. Untracked consumers will not appear in the impact list."
            index={19}
          />
          <HonestyTile
            tone="success"
            head="Validate before upload, publish after"
            body="The CLI follows a strict lifecycle: classify and check, then upload, then publish. Promotion never runs before validation clears."
            index={20}
          />
          <HonestyTile
            tone="warning"
            head="IDE serves from your endpoint"
            body="The GraphQL IDE is served by your API endpoint, not by the Nitro service. The registry tracks the schema, the endpoint hosts the tooling."
            index={21}
          />

          {/* Closing CTA tile, full row */}
          <ClosingTile index={22} />
        </div>
      </div>
    </div>
  );
}
