"use client";

import { motion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Scene tokens                                                        */
/* The stacked-deck variant uses cc-* surfaces with a single accent    */
/* (cc-accent teal). Classification colors (safe/dangerous/breaking)   */
/* are the only other allowed semantic hues.                           */
/* ------------------------------------------------------------------ */

const DECK_SHADOW = "shadow-[0_18px_50px_-25px_rgba(0,0,0,0.7)]";

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

/* ------------------------------------------------------------------ */
/* SDL token helpers (kept identical in spirit to v1)                  */
/* ------------------------------------------------------------------ */

const tk = {
  kw: (s: string) => <span className="text-cc-info">{s}</span>,
  ty: (s: string) => <span className="text-cc-tip">{s}</span>,
  fld: (s: string) => <span className="text-cc-heading">{s}</span>,
  dir: (s: string) => <span className="text-cc-note">{s}</span>,
  punc: (s: string) => <span className="text-cc-ink-dim">{s}</span>,
};

/* ------------------------------------------------------------------ */
/* Card frame: the single visual atom of the deck                      */
/* ------------------------------------------------------------------ */

interface DeckCardProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: React.CSSProperties;
  readonly status?: ChangeStatus;
}

function DeckCard({ children, className = "", style, status }: DeckCardProps) {
  const ring =
    status === undefined
      ? "ring-cc-card-border"
      : status === "safe"
        ? "ring-cc-success/35"
        : status === "dangerous"
          ? "ring-cc-warning/35"
          : "ring-cc-danger/40";
  return (
    <div
      className={`bg-cc-card-bg ring-1 ring-inset ${ring} rounded-xl ${DECK_SHADOW} ${className}`}
      style={style}
    >
      {children}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* HERO: fanned stack of three classification cards                    */
/* ------------------------------------------------------------------ */

interface VerdictCardContent {
  readonly status: ChangeStatus;
  readonly title: string;
  readonly subtitle: string;
  readonly sdl: ReactNode;
  readonly note: string;
}

const HERO_VERDICTS: readonly VerdictCardContent[] = [
  {
    status: "safe",
    title: "Additive field",
    subtitle: "Order.totalAmount: Money!",
    sdl: (
      <>
        <span className="text-cc-success/80">+ </span>
        {tk.fld("totalAmount")}
        {tk.punc(": ")}
        {tk.ty("Money!")}
      </>
    ),
    note: "New non-null field added. No existing query rejected.",
  },
  {
    status: "dangerous",
    title: "Deprecation",
    subtitle: "Order.placedAt @deprecated",
    sdl: (
      <>
        {tk.fld("placedAt")}
        {tk.punc(": ")}
        {tk.ty("DateTime")} {tk.dir("@deprecated")}
      </>
    ),
    note: "Still resolves. Clients warned to migrate before removal.",
  },
  {
    status: "breaking",
    title: "Field removed",
    subtitle: "Order.total dropped",
    sdl: (
      <>
        <span className="text-cc-danger/80">- </span>
        {tk.fld("total")}
        {tk.punc(": ")}
        {tk.ty("Float!")}
      </>
    ),
    note: "3 published clients still select Order.total. Blocked at the gate.",
  },
];

interface FanCardProps {
  readonly content: VerdictCardContent;
  readonly rotation: number;
  readonly translateY: number;
  readonly translateX: number;
  readonly z: number;
}

function FanCard({
  content,
  rotation,
  translateY,
  translateX,
  z,
}: FanCardProps) {
  const meta = STATUS_META[content.status];
  return (
    <div
      className="absolute inset-x-0 mx-auto w-[88%] transition-transform duration-300 ease-out hover:!translate-y-[var(--lift)] hover:!rotate-0"
      style={{
        transform: `translate(${translateX}px, ${translateY}px) rotate(${rotation}deg)`,
        zIndex: z,
        ["--lift" as string]: `${translateY - 4}px`,
      }}
    >
      <DeckCard status={content.status} className="overflow-hidden">
        <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
          <span className="flex gap-1.5" aria-hidden>
            <span className="bg-cc-danger/60 h-2 w-2 rounded-full" />
            <span className="bg-cc-warning/60 h-2 w-2 rounded-full" />
            <span className="bg-cc-success/60 h-2 w-2 rounded-full" />
          </span>
          <span className="text-cc-ink-dim ml-2 font-mono text-[0.66rem]">
            schema.graphql
          </span>
          <span className={`ml-auto ${meta.text} font-mono text-[0.6rem]`}>
            verdict
          </span>
        </div>
        <div className="px-5 py-5">
          <div className="flex items-center justify-between">
            <div className="min-w-0">
              <div className="text-cc-heading font-heading text-[1rem] font-semibold tracking-tight">
                {content.title}
              </div>
              <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.7rem]">
                {content.subtitle}
              </div>
            </div>
            <StatusChip status={content.status} />
          </div>
          <div className="border-cc-card-border bg-cc-bg/60 mt-4 rounded-md border px-3 py-2 font-mono text-[0.78rem] leading-relaxed">
            {content.sdl}
          </div>
          <p className="text-cc-ink-dim mt-3 text-[0.78rem] leading-relaxed">
            {content.note}
          </p>
        </div>
      </DeckCard>
    </div>
  );
}

function HeroDeck() {
  // Fanned stack: safe at back, dangerous middle, breaking on top.
  const safe = HERO_VERDICTS[0];
  const dangerous = HERO_VERDICTS[1];
  const breaking = HERO_VERDICTS[2];
  return (
    <div className="relative mx-auto h-[440px] w-full max-w-[460px] sm:h-[460px]">
      <FanCard
        content={safe}
        rotation={-2}
        translateY={0}
        translateX={-18}
        z={1}
      />
      <FanCard
        content={dangerous}
        rotation={0}
        translateY={28}
        translateX={0}
        z={2}
      />
      <FanCard
        content={breaking}
        rotation={2}
        translateY={56}
        translateX={18}
        z={3}
      />
    </div>
  );
}

function HeroSection() {
  const facts = [
    { k: "classify", v: "validate → publish" },
    { k: "gate", v: "CI blocks unsafe" },
    { k: "feedback", v: "in your build" },
  ];
  return (
    <section className="relative">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: [
            "radial-gradient(70% 50% at 50% 20%, rgba(94,234,212,0.06), transparent 70%)",
            "radial-gradient(circle at center, rgba(124,146,198,0.07) 1px, transparent 1px)",
          ].join(", "),
          backgroundSize: "auto, 28px 28px",
        }}
      />
      <div className="grid items-center gap-14 lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
        <div>
          <Eyebrow>Platform · Release Safety</Eyebrow>
          <h1 className="font-heading text-h2 text-cc-heading mt-5 font-bold tracking-tight">
            Every change,
            <br />
            dealt as a verdict.
          </h1>
          <p className="lead text-cc-ink-dim mt-6 max-w-xl">
            Ship schema changes without breaking the apps that depend on them.
          </p>
          <p className="text-body text-cc-prose mt-5 max-w-xl leading-relaxed">
            The registry deals every edit one of three cards, safe, dangerous,
            or breaking, validates the play against the clients you have
            actually published, and only then promotes it. Unsafe releases never
            make it onto the table.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/platform/continuous-integration">
              Read the Docs
            </OutlineButton>
          </div>
          <dl className="border-cc-card-border bg-cc-surface mt-10 grid max-w-md grid-cols-3 gap-px overflow-hidden rounded-lg border">
            {facts.map((s) => (
              <div key={s.k} className="bg-cc-card-bg px-3 py-3">
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
          <HeroDeck />
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION 2: classification hand (5 cards, full-width row)            */
/* ------------------------------------------------------------------ */

interface HandCardContent {
  readonly status: ChangeStatus;
  readonly rule: string;
  readonly description: string;
  readonly sdl: ReactNode;
}

const HAND_CARDS: readonly HandCardContent[] = [
  {
    status: "safe",
    rule: "Add field",
    description: "New nullable or non-null field on an existing type.",
    sdl: (
      <>
        {tk.kw("type")} {tk.ty("Order")} {tk.punc("{")}
        {"\n  "}
        <span className="text-cc-success/80">+ </span>
        {tk.fld("totalAmount")}
        {tk.punc(": ")}
        {tk.ty("Money!")}
        {"\n"}
        {tk.punc("}")}
      </>
    ),
  },
  {
    status: "safe",
    rule: "Add enum value",
    description: "Producer-side widening; consumers handle unknown values.",
    sdl: (
      <>
        {tk.kw("enum")} {tk.ty("OrderStatus")} {tk.punc("{")}
        {"\n  PENDING\n  "}
        <span className="text-cc-success/80">+ </span>SHIPPED
        {"\n"}
        {tk.punc("}")}
      </>
    ),
  },
  {
    status: "dangerous",
    rule: "Deprecate field",
    description: "Still resolves; clients warned to migrate.",
    sdl: (
      <>
        {tk.fld("placedAt")}
        {tk.punc(": ")}
        {tk.ty("DateTime")} {tk.dir("@deprecated")}
        {"\n  "}
        {tk.punc('(reason: "use createdAt")')}
      </>
    ),
  },
  {
    status: "breaking",
    rule: "Remove field",
    description: "Existing selections fail; clients break on next request.",
    sdl: (
      <>
        {tk.kw("type")} {tk.ty("Order")} {tk.punc("{")}
        {"\n  "}
        <span className="text-cc-danger/80">- </span>
        {tk.fld("total")}
        {tk.punc(": ")}
        {tk.ty("Float!")}
        {"\n"}
        {tk.punc("}")}
      </>
    ),
  },
  {
    status: "breaking",
    rule: "Narrow argument",
    description: "Tighter input type rejects previously valid calls.",
    sdl: (
      <>
        {tk.fld("orders")}
        {tk.punc("(")}
        {tk.fld("since")}
        {tk.punc(": ")}
        <span className="text-cc-danger/80">DateTime!</span>
        {tk.punc(")")}
      </>
    ),
  },
];

interface HandCardProps {
  readonly card: HandCardContent;
  readonly index: number;
}

function HandCard({ card, index }: HandCardProps) {
  // Slight alternating rotation, light horizontal overlap via negative margin
  // is applied at the row level via gap negatives.
  const rotations = [-3, -1, 0, 1, 3];
  const rotation = rotations[index] ?? 0;
  const meta = STATUS_META[card.status];
  return (
    <div
      className="w-full transition-transform duration-300 ease-out hover:-translate-y-1 hover:!rotate-0"
      style={{ transform: `rotate(${rotation}deg)` }}
    >
      <DeckCard status={card.status} className="h-full overflow-hidden">
        <div className="flex items-center justify-between px-4 pt-4">
          <span
            className={`font-mono text-[0.62rem] font-semibold tracking-[0.18em] uppercase ${meta.text}`}
          >
            {meta.label}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem]">
            {String(index + 1).padStart(2, "0")} / 05
          </span>
        </div>
        <div className="px-4 pt-2 pb-4">
          <div className="text-cc-heading font-heading text-[0.98rem] font-semibold tracking-tight">
            {card.rule}
          </div>
          <p className="text-cc-ink-dim mt-1.5 text-[0.78rem] leading-relaxed">
            {card.description}
          </p>
          <pre className="border-cc-card-border bg-cc-bg/60 text-cc-prose mt-3 overflow-x-auto rounded-md border px-3 py-2 font-mono text-[0.7rem] leading-relaxed whitespace-pre">
            {card.sdl}
          </pre>
        </div>
      </DeckCard>
    </div>
  );
}

function ClassificationHand() {
  return (
    <section>
      <div className="mx-auto max-w-3xl text-center">
        <Eyebrow>The hand</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          Five rules, three verdicts.
        </h2>
        <p className="text-body text-cc-prose mx-auto mt-4 max-w-2xl leading-relaxed">
          Every edit lands as one of three cards. The registry knows the rules,
          so the conversation in review is about intent, not about whether
          something is safe.
        </p>
      </div>
      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-5 lg:gap-3">
        {HAND_CARDS.map((c, i) => (
          <HandCard key={c.rule} card={c} index={i} />
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION 3: dealt pile of client cards                               */
/* ------------------------------------------------------------------ */

interface ClientCardContent {
  readonly name: string;
  readonly env: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "queued";
}

const CLIENT_CARDS: readonly ClientCardContent[] = [
  { name: "web", env: "production", ok: 5, total: 5, status: "ok" },
  { name: "mobile", env: "production", ok: 3, total: 5, status: "risk" },
  { name: "partner", env: "sandbox", ok: 0, total: 4, status: "queued" },
];

interface ImpactBarProps {
  readonly ok: number;
  readonly total: number;
  readonly status: ClientCardContent["status"];
}

function ImpactBar({ ok, total, status }: ImpactBarProps) {
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
          className={`h-2 w-5 rounded-[2px] ${i < ok ? color : "bg-cc-ink-faint"}`}
        />
      ))}
    </span>
  );
}

interface DealtClientCardProps {
  readonly card: ClientCardContent;
  readonly rotation: number;
  readonly offsetY: number;
}

function DealtClientCard({ card, rotation, offsetY }: DealtClientCardProps) {
  const statusLabel = {
    ok: { text: "OK", cls: "text-cc-success", ring: "ring-cc-success/30" },
    risk: {
      text: "at risk",
      cls: "text-cc-warning",
      ring: "ring-cc-warning/30",
    },
    queued: {
      text: "queued",
      cls: "text-cc-nav-label",
      ring: "ring-cc-nav-label/30",
    },
  } as const;
  const s = statusLabel[card.status];
  return (
    <div
      className="transition-transform duration-300 ease-out hover:!rotate-0"
      style={{ transform: `translateY(${offsetY}px) rotate(${rotation}deg)` }}
    >
      <DeckCard className="overflow-hidden">
        <div className="flex items-center gap-3 px-4 py-4">
          <div className="bg-cc-hover ring-cc-card-border flex h-9 w-9 items-center justify-center rounded-md ring-1 ring-inset">
            <span className="text-cc-accent font-mono text-[0.72rem] font-semibold">
              {card.name.charAt(0).toUpperCase()}
            </span>
          </div>
          <div className="min-w-0 flex-1">
            <div className="text-cc-heading font-mono text-[0.82rem] font-semibold">
              {card.name}
            </div>
            <div className="text-cc-nav-label font-mono text-[0.62rem]">
              {card.env}
            </div>
          </div>
          <span
            className={`rounded-md px-2 py-0.5 font-mono text-[0.62rem] font-semibold ring-1 ring-inset ${s.cls} ${s.ring}`}
          >
            {s.text}
          </span>
        </div>
        <div className="border-cc-card-border flex items-center gap-3 border-t px-4 py-3">
          <ImpactBar ok={card.ok} total={card.total} status={card.status} />
          <span className="text-cc-ink-dim ml-auto font-mono text-[0.7rem]">
            {card.ok}/{card.total} operations passing
          </span>
        </div>
      </DeckCard>
    </div>
  );
}

function ClientPileSection() {
  const rotations = [-2, 0, 2];
  const offsets = [0, -6, -12];
  return (
    <section className="grid items-center gap-12 lg:grid-cols-2 lg:gap-16">
      <div>
        <Eyebrow>Blast radius</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          A dealt pile of clients, face up.
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">
          The client registry tracks the operations your published clients
          actually run. Before a release ships, the registry deals one card per
          client, face up, showing which clients are clear and which would have
          operations break.
        </p>
        <ul className="mt-6 space-y-3">
          {[
            "Validation runs against the operations published clients send.",
            "Each client reports passing operations, not a vague global verdict.",
            "Queued clients are validated as soon as their operations are registered.",
          ].map((b) => (
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
      <div className="relative">
        <div className="flex flex-col gap-4">
          {CLIENT_CARDS.map((c, i) => (
            <DealtClientCard
              key={c.name}
              card={c}
              rotation={rotations[i] ?? 0}
              offsetY={offsets[i] ?? 0}
            />
          ))}
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION 4: validate then publish gate (dealer's cut)                */
/* ------------------------------------------------------------------ */

interface InnerGateCardProps {
  readonly kicker: string;
  readonly label: string;
  readonly sub: string;
  readonly status?: ChangeStatus;
  readonly rotation: number;
  readonly translateX: number;
  readonly slid?: boolean;
}

function InnerGateCard({
  kicker,
  label,
  sub,
  status,
  rotation,
  translateX,
  slid = false,
}: InnerGateCardProps) {
  return (
    <div
      className={`flex-1 transition-transform duration-300 ease-out ${slid ? "opacity-90" : ""}`}
      style={{
        transform: `translateX(${translateX}px) rotate(${rotation}deg)`,
      }}
    >
      <DeckCard status={status} className="px-4 py-4">
        <div className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.18em] uppercase">
          {kicker}
        </div>
        <div
          className={`mt-1 font-mono text-[0.88rem] font-semibold ${
            status === "safe"
              ? "text-cc-success"
              : status === "breaking"
                ? "text-cc-danger"
                : status === "dangerous"
                  ? "text-cc-warning"
                  : "text-cc-heading"
          }`}
        >
          {label}
        </div>
        <div className="text-cc-ink-dim mt-1 font-mono text-[0.66rem] leading-relaxed">
          {sub}
        </div>
      </DeckCard>
    </div>
  );
}

function GateSection() {
  return (
    <section className="grid items-center gap-12 lg:grid-cols-2 lg:gap-16">
      <div className="lg:order-2">
        <Eyebrow>The dealer&rsquo;s cut</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          Validate, then publish. Never the other way around.
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">
          Release safety is a two-stage gate. A change is classified and checked
          against published clients first; only changes that clear validation
          are promoted. Breaking cards are slid out before the deal.
        </p>
        <ul className="mt-6 space-y-3">
          {[
            "Validate classifies the change and tests it against real clients.",
            "Publish only ever runs on a change that already cleared validation.",
            "A blocked change carries its reason, not just a red X.",
          ].map((b) => (
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
      <div className="lg:order-1">
        <DeckCard className="bg-cc-surface overflow-hidden">
          <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
            <span className="flex gap-1.5" aria-hidden>
              <span className="bg-cc-danger/60 h-2 w-2 rounded-full" />
              <span className="bg-cc-warning/60 h-2 w-2 rounded-full" />
              <span className="bg-cc-success/60 h-2 w-2 rounded-full" />
            </span>
            <span className="text-cc-ink-dim ml-2 font-mono text-[0.66rem]">
              registry · gate
            </span>
            <span className="bg-cc-hover text-cc-ink-dim ml-auto rounded-md px-2 py-0.5 font-mono text-[0.6rem]">
              validate → publish
            </span>
          </div>
          <div className="space-y-3 px-5 py-6">
            <InnerGateCard
              kicker="01"
              label="validate"
              sub="classify and check published clients"
              rotation={-1}
              translateX={0}
              status="dangerous"
            />
            <InnerGateCard
              kicker="02"
              label="publish"
              sub="safe and dangerous promoted to the registry"
              rotation={1}
              translateX={0}
              status="safe"
            />
            <InnerGateCard
              kicker="03"
              label="blocked"
              sub="breaking removal, slid out of the deck"
              rotation={-6}
              translateX={-36}
              status="breaking"
              slid
            />
          </div>
          <div className="border-cc-card-border border-t px-5 py-3">
            <p className="text-cc-ink-dim font-mono text-[0.7rem] leading-relaxed">
              Nothing reaches <span className="text-cc-success">publish</span>{" "}
              until it clears <span className="text-cc-warning">validate</span>.
              The breaking card never makes it onto the table.
            </p>
          </div>
        </DeckCard>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION 5: schema history pile (played pile)                        */
/* ------------------------------------------------------------------ */

interface VersionCardContent {
  readonly v: string;
  readonly note: string;
  readonly status: ChangeStatus;
  readonly signature: string;
}

const VERSIONS: readonly VersionCardContent[] = [
  {
    v: "v12",
    note: "add Cart.discount",
    status: "safe",
    signature: "registry/published",
  },
  {
    v: "v13",
    note: "deprecate Order.placedAt",
    status: "dangerous",
    signature: "registry/published",
  },
  {
    v: "v14",
    note: "remove Order.total, blocked at the gate",
    status: "breaking",
    signature: "registry/blocked",
  },
  {
    v: "v14",
    note: "add Order.totalAmount, reshaped",
    status: "safe",
    signature: "registry/published",
  },
  {
    v: "v15",
    note: "drop Order.total, usage cleared",
    status: "dangerous",
    signature: "registry/published",
  },
];

interface VersionCardProps {
  readonly version: VersionCardContent;
  readonly rotation: number;
  readonly isLatest: boolean;
}

function VersionCard({ version, rotation, isLatest }: VersionCardProps) {
  const meta = STATUS_META[version.status];
  const blocked = version.signature === "registry/blocked";
  return (
    <div
      className="transition-transform duration-300 ease-out hover:!rotate-0"
      style={{ transform: `rotate(${rotation}deg)` }}
    >
      <DeckCard className={`overflow-hidden ${blocked ? "opacity-80" : ""}`}>
        <div className="flex items-center gap-4 px-5 py-4">
          <div className="text-cc-heading w-12 shrink-0 font-mono text-[0.86rem] font-semibold">
            {version.v}
          </div>
          <div className="text-cc-prose min-w-0 flex-1 truncate font-mono text-[0.78rem]">
            {version.note}
          </div>
          <StatusChip status={version.status} />
          {isLatest ? (
            <span className="flex items-center gap-1.5">
              <motion.span
                aria-hidden
                className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
                animate={{ opacity: [0.6, 1, 0.6] }}
                transition={{
                  duration: 2.6,
                  repeat: Infinity,
                  ease: "easeInOut",
                }}
              />
              <span className="text-cc-accent font-mono text-[0.6rem] tracking-[0.14em] uppercase">
                latest
              </span>
            </span>
          ) : (
            <span className="text-cc-nav-label font-mono text-[0.62rem]">
              {version.signature}
            </span>
          )}
        </div>
        <div className="border-cc-card-border bg-cc-bg/50 flex items-center justify-between border-t px-5 py-2">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.14em] uppercase">
            classification
          </span>
          <span
            className={`font-mono text-[0.62rem] font-semibold ${meta.text}`}
          >
            {meta.label.toLowerCase()}
          </span>
        </div>
      </DeckCard>
    </div>
  );
}

function HistoryPileSection() {
  // Top of pile = latest = last entry (v15). Render reversed so the latest
  // sits on top of the played pile with a slow pulse on its status dot.
  const ordered = [...VERSIONS].reverse();
  return (
    <section>
      <div className="mx-auto max-w-3xl text-center">
        <Eyebrow>The record</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          A played pile of every version.
        </h2>
        <p className="text-body text-cc-prose mx-auto mt-4 max-w-2xl leading-relaxed">
          The registry keeps the full history of your schema as a stack of
          played cards. The breaking removal at v14 never shipped; it was
          reshaped as an additive change, deprecated, and only dropped at v15
          once client usage cleared.
        </p>
      </div>
      <div className="mx-auto mt-10 max-w-2xl space-y-3">
        {ordered.map((v, i) => {
          const rotation = i % 2 === 0 ? -1 : 1;
          return (
            <VersionCard
              key={`${v.v}-${i}`}
              version={v}
              rotation={rotation}
              isLatest={i === 0}
            />
          );
        })}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION 6: reliability stat band (deck laid flat)                   */
/* ------------------------------------------------------------------ */

function ReliabilityBand() {
  const stats = [
    { n: "0", l: "breaking changes shipped", c: "text-cc-success" },
    { n: "12", l: "published clients guarded", c: "text-cc-heading" },
    { n: "100%", l: "of releases checked", c: "text-cc-heading" },
  ];
  return (
    <section
      className={`bg-cc-surface border-cc-card-border overflow-hidden rounded-2xl border ${DECK_SHADOW}`}
    >
      <div className="divide-cc-card-border grid divide-y sm:grid-cols-3 sm:divide-x sm:divide-y-0">
        {stats.map((s) => (
          <div key={s.l} className="px-6 py-10 text-center">
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
/* SECTION 7: honesty pair (upright cards, no rotation)                */
/* ------------------------------------------------------------------ */

interface HonestyCardProps {
  readonly tone: "success" | "warning";
  readonly head: string;
  readonly body: ReactNode;
}

function HonestyCard({ tone, head, body }: HonestyCardProps) {
  const bar = tone === "success" ? "bg-cc-success" : "bg-cc-warning";
  return (
    <DeckCard className="relative px-5 py-5">
      <span
        className={`absolute top-5 bottom-5 left-0 w-[3px] rounded-full ${bar}`}
        aria-hidden
      />
      <div className="pl-4">
        <div className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.14em] uppercase">
          {head}
        </div>
        <p className="text-cc-ink-dim mt-2 text-[0.86rem] leading-relaxed">
          {body}
        </p>
      </div>
    </DeckCard>
  );
}

function HonestySection() {
  return (
    <section>
      <div className="max-w-3xl">
        <Eyebrow>What this does and does not promise</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-4 font-semibold tracking-tight">
          A safety net, not a blindfold.
        </h2>
        <p className="text-body text-cc-prose mt-4 leading-relaxed">
          A check is only useful if you can trust what it claims. Release safety
          reports the published clients affected by a change, based on the
          operations they have registered. Strawberry Shake regenerates clients
          via MSBuild codegen, so contract drift shows up as build feedback you
          cannot miss.
        </p>
      </div>
      <div className="mt-8 grid gap-4 sm:grid-cols-2">
        <HonestyCard
          tone="success"
          head="What it stops"
          body="Releases that would break a published client are blocked before merge, with the breaking line and reason in hand."
        />
        <HonestyCard
          tone="warning"
          head="What it needs"
          body="A client is only guarded once its operations are registered. Unregistered traffic is outside the net."
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* SECTION 8: closing CTA (single centered card, laid flat)            */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section>
      <DeckCard className="bg-cc-surface relative overflow-hidden px-6 py-14 text-center sm:px-12">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10"
          style={{
            backgroundImage:
              "radial-gradient(70% 100% at 50% 0%, rgba(94,234,212,0.10), transparent 65%)",
          }}
        />
        <h2 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl font-bold tracking-tight">
          Deal every release a verdict before it ships.
        </h2>
        <p className="text-body text-cc-prose mx-auto mt-5 max-w-xl leading-relaxed">
          Classify, validate, and gate your releases so breaking changes never
          make it onto the table.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/continuous-integration">
            Read the Docs
          </OutlineButton>
        </div>
      </DeckCard>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-24 py-6 sm:gap-28">
      <HeroSection />
      <ClassificationHand />
      <ClientPileSection />
      <GateSection />
      <HistoryPileSection />
      <ReliabilityBand />
      <HonestySection />
      <ClosingCta />
    </div>
  );
}
