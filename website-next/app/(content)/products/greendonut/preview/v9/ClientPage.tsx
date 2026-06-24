"use client";

import type { CSSProperties, ReactNode } from "react";
import { useState } from "react";
import { motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * The Standup.
 *
 * The page reads as a recorded standup thread: Dev raises the N+1,
 * Agent (Green Donut speaking back) answers with batching and code,
 * and Ops reports the production effects. Each section is a chat
 * bubble anchored to one speaker, with an avatar tile, a role label,
 * a timestamp, and a permalink-style index. One vertical rail runs
 * down the left as the conversation timeline. Teal is the single page
 * accent; the brand spectrum appears exactly once, on the closing CTA
 * bubble border. Motion is enter-view-once only, no scroll coupling.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Mono C# token palette, scoped to the code card only (GitHub-dark-ish), so
// the rest of the page stays on cc-* tokens.
const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  plain: { color: "#c9d1d9" },
};

const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1];

type Role = "dev" | "agent" | "ops";

const ROLE_META: Record<
  Role,
  { readonly label: string; readonly handle: string; readonly initials: string }
> = {
  dev: { label: "Dev", handle: "@dev", initials: "DV" },
  agent: { label: "Agent", handle: "@green-donut", initials: "GD" },
  ops: { label: "Ops", handle: "@ops", initials: "OP" },
};

// -----------------------------------------------------------------------------
// Avatar tile: 56px square, mono initials, accent left bar.
// -----------------------------------------------------------------------------

interface AvatarProps {
  readonly role: Role;
}

function Avatar({ role }: AvatarProps) {
  const { initials } = ROLE_META[role];
  return (
    <div className="relative shrink-0">
      <div className="border-cc-card-border bg-cc-surface relative flex h-14 w-14 items-center justify-center overflow-hidden rounded-2xl border">
        <span
          className="absolute top-0 bottom-0 left-0 w-[2px]"
          style={{ backgroundColor: TEAL }}
          aria-hidden
        />
        <span className="text-cc-heading font-mono text-[15px] font-semibold tracking-[0.08em]">
          {initials}
        </span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// One message row: rail dot + avatar on the left, bubble on the right.
// Enter-view-once fade up, with a per-message stagger delay.
// -----------------------------------------------------------------------------

interface MessageProps {
  readonly role: Role;
  readonly index: string;
  readonly time: string;
  readonly order: number;
  readonly children: ReactNode;
  readonly accentBubble?: boolean;
  readonly spectrum?: boolean;
}

function Message({
  role,
  index,
  time,
  order,
  children,
  accentBubble = false,
  spectrum = false,
}: MessageProps) {
  const reduce = useReducedMotion();
  const { label, handle } = ROLE_META[role];

  const bubbleInner = (
    <div className="border-cc-card-border bg-cc-surface relative flex-1 rounded-2xl rounded-tl-sm border p-5 sm:p-6">
      {accentBubble && !spectrum ? (
        <span
          className="ring-cc-accent pointer-events-none absolute inset-0 rounded-2xl rounded-tl-sm ring-1 ring-inset"
          aria-hidden
        />
      ) : null}

      {/* Header strip: role label, handle, timestamp, permalink index. */}
      <div className="border-cc-card-border mb-4 flex flex-wrap items-center gap-x-3 gap-y-1 border-b pb-3">
        <span className="text-cc-heading font-mono text-[11px] font-semibold tracking-[0.18em] uppercase">
          {label}
        </span>
        <span className="text-cc-ink-dim font-mono text-[11px]">{handle}</span>
        <span className="text-cc-ink-faint" aria-hidden>
          &middot;
        </span>
        <span className="text-cc-ink-dim font-mono text-[11px] tabular-nums">
          {time}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex h-6 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
          {index}
        </span>
      </div>

      {children}
    </div>
  );

  return (
    <motion.div
      className="relative flex items-start gap-4 sm:gap-6"
      initial={reduce ? false : { opacity: 0, y: 8 }}
      whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.32, ease: EASE, delay: order * 0.06 }}
    >
      {/* Rail dot, sits on the vertical timeline at the avatar center. */}
      <span
        className="bg-cc-bg absolute top-6 left-[27px] hidden h-2 w-2 -translate-x-1/2 rounded-full sm:block"
        style={{ boxShadow: `0 0 0 1px ${TEAL}` }}
        aria-hidden
      />
      <Avatar role={role} />

      {spectrum ? (
        <div
          className="flex-1 rounded-2xl rounded-tl-sm p-[1px]"
          style={{ background: SPECTRUM }}
        >
          {bubbleInner}
        </div>
      ) : (
        bubbleInner
      )}
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// Inset sub-card: the artifact embedded inside a bubble.
// -----------------------------------------------------------------------------

interface ArtifactProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function Artifact({ children, className = "" }: ArtifactProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-bg/60 mt-4 rounded-xl border p-4 ${className}`}
    >
      {children}
    </div>
  );
}

interface ArtifactCaptionProps {
  readonly children: ReactNode;
}

function ArtifactCaption({ children }: ArtifactCaptionProps) {
  return (
    <p className="text-cc-ink-dim mt-3 font-mono text-[11px] tracking-[0.04em]">
      {children}
    </p>
  );
}

// -----------------------------------------------------------------------------
// One-shot typing indicator: three dots animate once on view, then resolve
// into the bubble content. Time-driven keyframes, no scroll coupling.
// -----------------------------------------------------------------------------

interface TypingThenProps {
  readonly children: ReactNode;
}

function TypingThen({ children }: TypingThenProps) {
  const reduce = useReducedMotion();
  const [typed, setTyped] = useState(reduce ? true : false);

  if (typed) {
    return <>{children}</>;
  }

  const dotStyle: CSSProperties = {
    backgroundColor: TEAL,
  };

  return (
    <motion.div
      className="flex h-8 items-center gap-1.5"
      aria-label="Green Donut is typing"
      initial={{ opacity: 1 }}
      whileInView="run"
      viewport={{ once: true }}
      onAnimationComplete={() => setTyped(true)}
    >
      {[0, 1, 2].map((i) => (
        <motion.span
          key={i}
          className="h-2 w-2 rounded-full"
          style={dotStyle}
          variants={{
            run: {
              opacity: [0.3, 1, 0.3],
              transition: {
                duration: 0.6,
                ease: "easeInOut",
                delay: i * 0.12,
              },
            },
          }}
        />
      ))}
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// Code card: the [DataLoader] attribute, mono, scoped token colors.
// -----------------------------------------------------------------------------

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-4 px-4">
      <span
        className="w-5 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[12px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

function DataLoaderCode() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span
          className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-success/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span className="ml-3 font-mono text-[11px] text-[#8b949e]">
          UserDataLoader.cs
        </span>
      </div>
      <div className="overflow-x-auto py-3">
        <CodeLine n={1}>
          <span style={C.kw}>public static partial class</span>{" "}
          <span style={C.type}>UserDataLoader</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{"{"}</span>
        </CodeLine>
        <CodeLine n={3}>
          {"    "}
          <span style={C.attr}>[DataLoader]</span>
        </CodeLine>
        <CodeLine n={4}>
          {"    "}
          <span style={C.kw}>public static async</span>{" "}
          <span style={C.type}>{"Task<IReadOnlyDictionary<int, User>>"}</span>
        </CodeLine>
        <CodeLine n={5}>
          {"    "}
          <span style={C.fn}>GetUsersAsync</span>
          <span style={C.plain}>(</span>
          <span style={C.type}>{"IReadOnlyList<int>"}</span>{" "}
          <span style={C.param}>ids</span>
          <span style={C.plain}>,</span>
        </CodeLine>
        <CodeLine n={6}>
          {"        "}
          <span style={C.type}>AppDbContext</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>,</span>{" "}
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.plain}>)</span>
        </CodeLine>
        <CodeLine n={7}>
          {"        "}
          <span style={C.plain}>{"=> await"}</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>.Users.</span>
          <span style={C.fn}>Where</span>
          <span style={C.plain}>(u {"=>"} </span>
          <span style={C.param}>ids</span>
          <span style={C.plain}>.</span>
          <span style={C.fn}>Contains</span>
          <span style={C.plain}>(u.Id))</span>
        </CodeLine>
        <CodeLine n={8}>
          {"            "}
          <span style={C.plain}>.</span>
          <span style={C.fn}>ToDictionaryAsync</span>
          <span style={C.plain}>(u {"=>"} u.Id, </span>
          <span style={C.param}>ct</span>
          <span style={C.plain}>);</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.plain}>{"}"}</span>
        </CodeLine>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Before/after micro-diagram: six keys collapse into one batched call.
// -----------------------------------------------------------------------------

function BatchDiagram() {
  return (
    <svg
      viewBox="0 0 240 96"
      className="w-full"
      role="img"
      aria-label="Six keys collected on one tick collapse into one batched call"
    >
      <defs>
        <linearGradient id="gd9-batch-line" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor={TEAL} stopOpacity="0.25" />
          <stop offset="100%" stopColor={TEAL} stopOpacity="0.9" />
        </linearGradient>
      </defs>
      {[12, 26, 40, 54, 68, 82].map((y, i) => (
        <g key={y}>
          <circle cx="14" cy={y} r="3" fill={TEAL} fillOpacity="0.85" />
          <path
            d={`M18,${y} C 100,${y} 140,47 196,47`}
            fill="none"
            stroke="url(#gd9-batch-line)"
            strokeWidth="1.25"
            strokeLinecap="round"
            opacity={0.5 + i * 0.07}
          />
        </g>
      ))}
      <rect
        x="192"
        y="38"
        width="40"
        height="18"
        rx="4"
        fill={TEAL}
        fillOpacity="0.18"
        stroke={TEAL}
        strokeOpacity="0.65"
      />
      <text
        x="212"
        y="50"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="9"
        fill={TEAL}
      >
        IN (...)
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// KPI strip: three illustrative tiles inside the Ops bubble.
// -----------------------------------------------------------------------------

const KPIS: ReadonlyArray<{ readonly label: string; readonly value: string }> =
  [
    { label: "queries", value: "-83%" },
    { label: "P95", value: "-41%" },
    { label: "allocations", value: "-62%" },
  ];

function KpiStrip() {
  return (
    <div className="border-cc-card-border grid grid-cols-3 divide-x divide-[var(--color-cc-card-border)] overflow-hidden rounded-lg border text-center">
      {KPIS.map((k) => (
        <div key={k.label} className="px-3 py-3">
          <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
            {k.label}
          </div>
          <div
            className="mt-1 font-mono text-[16px] font-semibold tabular-nums"
            style={{ color: TEAL }}
          >
            {k.value}
          </div>
        </div>
      ))}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Participant chip in the thread header.
// -----------------------------------------------------------------------------

interface ChipProps {
  readonly role: Role;
}

function ParticipantChip({ role }: ChipProps) {
  const { label, handle } = ROLE_META[role];
  return (
    <span className="border-cc-card-border bg-cc-surface inline-flex items-center gap-2 rounded-full border py-1.5 pr-3 pl-1.5">
      <span
        className="flex h-6 w-6 items-center justify-center rounded-full font-mono text-[10px] font-semibold"
        style={{ color: TEAL, boxShadow: `inset 0 0 0 1px ${TEAL}` }}
      >
        {ROLE_META[role].initials}
      </span>
      <span className="text-cc-heading font-mono text-[11px] font-semibold tracking-[0.12em] uppercase">
        {label}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px]">{handle}</span>
    </span>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <main className="bg-cc-bg w-full">
      <div className="mx-auto w-full max-w-3xl px-6 pt-16 pb-24 sm:px-8">
        {/* THREAD HEADER */}
        <header className="relative">
          <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
            Green Donut // Standup Thread
          </span>
          <h1 className="font-heading text-cc-heading text-h1 mt-5">
            DataLoader for .NET, in conversation.
          </h1>
          <p className="lead text-cc-ink mt-5 max-w-2xl">
            One recorded standup. Dev raises the N+1, the Green Donut agent
            answers with batching and generated wiring, and Ops reports what
            changed on the cluster. Read it top to bottom.
          </p>
          <div className="mt-7 flex flex-wrap items-center gap-3">
            <ParticipantChip role="dev" />
            <ParticipantChip role="agent" />
            <ParticipantChip role="ops" />
            <span className="text-cc-ink-dim ml-auto font-mono text-[11px] tracking-[0.16em] uppercase">
              started 09:14
            </span>
          </div>
        </header>

        {/* THREAD: vertical rail + dotted timeline behind the messages. */}
        <div className="relative mt-14">
          {/* Conversation rail and faint dotted timeline, desktop only. */}
          <div
            className="pointer-events-none absolute top-0 bottom-0 left-[27px] hidden w-24 sm:block"
            aria-hidden
            style={{
              backgroundImage: `radial-gradient(rgba(245,241,234,0.2) 1px, transparent 1px)`,
              backgroundSize: "96px 24px",
              backgroundPosition: "0 0",
            }}
          />
          <span
            className="pointer-events-none absolute top-0 bottom-0 left-[27px] hidden w-px sm:block"
            aria-hidden
            style={{ backgroundColor: "rgba(245,241,234,0.05)" }}
          />

          <div className="relative flex flex-col gap-16">
            {/* #01 Dev: the opening question + attachments. */}
            <Message role="dev" index="#01" time="09:14" order={0}>
              <p className="text-cc-ink text-body">
                Resolvers are firing{" "}
                <span className="text-cc-heading font-mono text-[13px]">
                  GetUser
                </span>{" "}
                per row. P95 is climbing on the orders list and the connection
                pool is starting to choke. What do I reach for?
              </p>
              <Artifact>
                <div className="text-cc-ink-dim mb-2 font-mono text-[10.5px] tracking-[0.18em] uppercase">
                  resolver trace, one request
                </div>
                <ul className="space-y-1.5">
                  {["7", "12", "3", "9", "21", "4"].map((id) => (
                    <li
                      key={id}
                      className="flex items-center justify-between font-mono text-[12px]"
                    >
                      <span className="text-cc-ink">GetUser(id: {id})</span>
                      <span className="text-cc-danger/90 font-mono text-[10.5px] tracking-[0.16em] uppercase">
                        SELECT
                      </span>
                    </li>
                  ))}
                </ul>
                <div className="border-cc-card-border mt-3 flex items-center justify-between border-t pt-3">
                  <span className="text-cc-ink-dim font-mono text-[11px]">
                    round trips
                  </span>
                  <span className="text-cc-danger font-mono text-[13px] font-semibold tabular-nums">
                    6
                  </span>
                </div>
              </Artifact>
              <div className="mt-5 flex flex-wrap items-center gap-3">
                <SolidButton href="/docs/greendonut">See the fix</SolidButton>
                <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                  GitHub
                </OutlineButton>
              </div>
            </Message>

            {/* #02 Agent: the answer, with typing indicator + diagram. */}
            <Message
              role="agent"
              index="#02"
              time="09:16"
              order={1}
              accentBubble
            >
              <TypingThen>
                <p className="text-cc-ink text-body">
                  I am Green Donut, the DataLoader for .NET. I collect the keys
                  your resolvers ask for on one tick, then send a single batched
                  fetch and hand each resolver its result.
                </p>
                <Artifact>
                  <div className="text-cc-ink-dim mb-3 flex items-center justify-between font-mono text-[10.5px] tracking-[0.16em] uppercase">
                    <span>six keys</span>
                    <span>one fetch</span>
                  </div>
                  <BatchDiagram />
                  <ArtifactCaption>
                    keys collected on one tick, IN (...) goes out once
                  </ArtifactCaption>
                </Artifact>
              </TypingThen>
            </Message>

            {/* #03 Agent: the [DataLoader] attribute generates the wiring. */}
            <Message
              role="agent"
              index="#03"
              time="09:17"
              order={2}
              accentBubble
            >
              <p className="text-cc-ink text-body">
                You write one static method.{" "}
                <span className="text-cc-heading font-mono text-[13px]">
                  [DataLoader]
                </span>{" "}
                generates the interface, the registration, and the typed
                accessor.
              </p>
              <Artifact className="px-0 pt-0 pb-0">
                <DataLoaderCode />
              </Artifact>
              <ArtifactCaption>source-generated at build</ArtifactCaption>
            </Message>

            {/* #04 Ops: cache + dedup, with KPI strip. */}
            <Message role="ops" index="#04" time="09:23" order={3}>
              <p className="text-cc-ink text-body">
                Per-request cache means repeat lookups inside one request are
                free. Same key in the same request returns the same task, so
                dedup is automatic. Here is what we saw after the rollout.
              </p>
              <Artifact>
                <KpiStrip />
                <ArtifactCaption>
                  illustrative figures, your numbers depend on the workload
                </ArtifactCaption>
              </Artifact>
            </Message>

            {/* #05 Agent: works with HC or standalone, feature checklist. */}
            <Message
              role="agent"
              index="#05"
              time="09:26"
              order={4}
              accentBubble
            >
              <p className="text-cc-ink text-body">
                I run inside Hot Chocolate, where loaders are auto discovered,
                or standalone in any .NET service. AOT-friendly, zero
                reflection. MIT licensed.
              </p>
              <Artifact>
                <ul className="space-y-2.5">
                  {[
                    "Batching: many keys on one tick, one fetch",
                    "Per-request cache: scoped, never shared across requests",
                    "Dedup: same key in one request, served once",
                    "AOT-friendly: generated wiring, no reflection",
                  ].map((item) => (
                    <li
                      key={item}
                      className="text-cc-ink flex items-start gap-2.5 text-sm"
                    >
                      <span className="text-cc-accent mt-[3px]">
                        <CheckIcon />
                      </span>
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </Artifact>
            </Message>

            {/* #06 Dev: how do I drop it in. */}
            <Message role="dev" index="#06" time="09:31" order={5}>
              <p className="text-cc-ink text-body">How do I drop it in?</p>
              <Artifact>
                <ol className="space-y-2.5">
                  {[
                    "dotnet add package GreenDonut",
                    "add [DataLoader] to a static method",
                    "inject the generated loader, call LoadAsync",
                  ].map((step, i) => (
                    <li
                      key={step}
                      className="flex items-start gap-3 font-mono text-[12.5px]"
                    >
                      <span
                        className="text-cc-bg mt-px flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[11px] font-semibold tabular-nums"
                        style={{ backgroundColor: TEAL }}
                      >
                        {i + 1}
                      </span>
                      <span className="text-cc-ink leading-5">{step}</span>
                    </li>
                  ))}
                </ol>
              </Artifact>
            </Message>

            {/* #07 Ops: telemetry, honest about configuration. */}
            <Message role="ops" index="#07" time="09:38" order={6}>
              <p className="text-cc-ink text-body">
                Once you batch, the per-loader behavior is worth watching.
              </p>
              <Artifact>
                <p className="text-cc-ink-dim text-sm leading-relaxed">
                  Telemetry surfaces in Nitro once you configure the exporter.
                  It is not on by default, you wire up OpenTelemetry and point
                  it at Nitro, then the batch and cache signals show up there.
                </p>
              </Artifact>
            </Message>

            {/* #08 Closing CTA: the one spectrum-bordered bubble. */}
            <Message role="agent" index="#08" time="09:42" order={7} spectrum>
              <div className="text-cc-ink-dim mb-2 font-mono text-[11px] tracking-[0.14em] uppercase">
                signed dev / agent / ops
              </div>
              <h2 className="font-heading text-cc-heading text-h3">
                Ship the batch.
              </h2>
              <p className="text-cc-ink text-body mt-3">
                Add Green Donut, mark a method with{" "}
                <span className="text-cc-heading font-mono text-[13px]">
                  [DataLoader]
                </span>
                , and inject the generated loader. The N+1 disappears on the
                next request.
              </p>
              <div className="mt-6 flex flex-wrap items-center gap-3">
                <SolidButton href="/docs/greendonut">
                  Install Green Donut
                </SolidButton>
                <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                  Read the docs
                </OutlineButton>
              </div>
              <div className="border-cc-card-border mt-6 border-t pt-4">
                <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
                  thread closed 09:42
                </span>
              </div>
            </Message>
          </div>
        </div>
      </div>
    </main>
  );
}
