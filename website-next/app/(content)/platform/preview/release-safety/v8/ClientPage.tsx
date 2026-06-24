"use client";

import { motion, useInView, useReducedMotion } from "motion/react";
import { useRef, type ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Concept: every schema change is a validation ticket. Each ticket    */
/* is a perforated stub punched at a gate before it can board. The     */
/* single accent (cc-accent teal) is the conductor's ink. Status hues  */
/* (success / warning / danger) classify each punch column.            */
/* ------------------------------------------------------------------ */

const EASE_OUT: [number, number, number, number] = [0.22, 1, 0.36, 1];

/* A receipt-roll of dashed horizontal rules that runs behind the whole
   ticket stack, suggesting paper coming off a conductor's roll. */
const ROLL_PATTERN =
  "repeating-linear-gradient(to bottom, transparent 0, transparent 22px, rgba(245,241,234,0.12) 22px, rgba(245,241,234,0.12) 23px, transparent 23px, transparent 24px)";

/* Perforated ticket edge: semicircle "bites" notched out of the corners so
   a card reads as a torn stub off a roll. */
const PERF_MASK =
  "radial-gradient(circle 7px at 0 0, transparent 7px, #000 7.5px), radial-gradient(circle 7px at 100% 0, transparent 7px, #000 7.5px), radial-gradient(circle 7px at 0 100%, transparent 7px, #000 7.5px), radial-gradient(circle 7px at 100% 100%, transparent 7px, #000 7.5px)";

const PERF_STYLE = {
  WebkitMaskImage: PERF_MASK,
  maskImage: PERF_MASK,
  WebkitMaskComposite: "source-in",
  maskComposite: "intersect",
} as const;

type ChangeStatus = "safe" | "dangerous" | "breaking";

const STATUS_META: Record<
  ChangeStatus,
  { label: string; text: string; ring: string; bg: string; dot: string }
> = {
  safe: {
    label: "SAFE",
    text: "text-cc-success",
    ring: "ring-cc-success/40",
    bg: "bg-cc-success/10",
    dot: "bg-cc-success",
  },
  dangerous: {
    label: "DANGEROUS",
    text: "text-cc-warning",
    ring: "ring-cc-warning/40",
    bg: "bg-cc-warning/10",
    dot: "bg-cc-warning",
  },
  breaking: {
    label: "BREAKING",
    text: "text-cc-danger",
    ring: "ring-cc-danger/40",
    bg: "bg-cc-danger/10",
    dot: "bg-cc-danger",
  },
};

/* ------------------------------------------------------------------ */
/* Ticket shell: a perforated card with notched corners. Children lay   */
/* out a main stub and an optional carbon stub with a dashed tear.      */
/* ------------------------------------------------------------------ */

interface TicketProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly hover?: boolean;
}

function Ticket({ children, className = "", hover = false }: TicketProps) {
  return (
    <motion.div
      whileHover={hover ? { y: -2 } : undefined}
      transition={{ duration: 0.25, ease: EASE_OUT }}
      className={`bg-cc-surface border-cc-card-border relative rounded-md border shadow-[0_24px_70px_-40px_rgba(0,0,0,0.85)] ${className}`}
      style={PERF_STYLE}
    >
      {children}
    </motion.div>
  );
}

/* The dashed "STUB NO." chip used as a section eyebrow. */
interface StubChipProps {
  readonly children: ReactNode;
}

function StubChip({ children }: StubChipProps) {
  return (
    <span className="border-cc-accent/40 text-cc-accent inline-flex items-center gap-1.5 rounded-[4px] border border-dashed px-2 py-1 font-mono text-[0.62rem] tracking-[0.2em] uppercase">
      <span className="bg-cc-accent/70 h-1.5 w-1.5 rounded-full" />
      {children}
    </span>
  );
}

/* A punched hole: an inactive column shows an unpunched dashed ring, an
   active column shows a punched hole inked in its status hue. */
interface PunchProps {
  readonly active: boolean;
  readonly tone: ChangeStatus;
}

function Punch({ active, tone }: PunchProps) {
  if (!active) {
    return (
      <span
        aria-hidden
        className="border-cc-card-border block h-5 w-5 rounded-full border border-dashed"
      />
    );
  }
  return (
    <span
      aria-hidden
      className={`bg-cc-bg block h-5 w-5 rounded-full ring-2 ring-inset ${STATUS_META[tone].ring}`}
      style={{ boxShadow: "inset 0 2px 5px rgba(0,0,0,0.6)" }}
    />
  );
}

/* The three-column classification strip stamped onto a ticket. Exactly
   one column is punched; the punched verdict is inked. */
interface PunchStripProps {
  readonly verdict: ChangeStatus;
}

function PunchStrip({ verdict }: PunchStripProps) {
  const cols: ChangeStatus[] = ["safe", "dangerous", "breaking"];
  return (
    <div className="grid grid-cols-3 gap-px overflow-hidden rounded-[4px]">
      {cols.map((c) => {
        const active = c === verdict;
        return (
          <div
            key={c}
            className={`flex flex-col items-center gap-2 px-2 py-3 ${
              active ? STATUS_META[c].bg : "bg-cc-hover"
            }`}
          >
            <span
              className={`font-mono text-[0.54rem] tracking-[0.14em] ${
                active ? STATUS_META[c].text : "text-cc-nav-label"
              }`}
            >
              {STATUS_META[c].label}
            </span>
            <Punch active={active} tone={c} />
          </div>
        );
      })}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* HERO - the oversized validation ticket for CHG-2026-0482            */
/* ------------------------------------------------------------------ */

interface DiffLine {
  readonly sign: "+" | "-" | " ";
  readonly text: string;
  readonly status?: ChangeStatus;
}

const HERO_DIFF: readonly DiffLine[] = [
  { sign: " ", text: "type Order {" },
  { sign: " ", text: "  id: ID!" },
  { sign: "+", text: "  totalAmount: Money!", status: "safe" },
  { sign: "-", text: "  total: Float!", status: "breaking" },
  {
    sign: "+",
    text: '  placedAt: DateTime @deprecated(reason: "use createdAt")',
    status: "dangerous",
  },
  { sign: " ", text: "}" },
];

function diffRowTint(sign: DiffLine["sign"]): string {
  if (sign === "+") {
    return "bg-cc-success/[0.05]";
  }
  if (sign === "-") {
    return "bg-cc-danger/[0.06]";
  }
  return "";
}

function diffSignInk(sign: DiffLine["sign"]): string {
  if (sign === "+") {
    return "text-cc-success";
  }
  if (sign === "-") {
    return "text-cc-danger";
  }
  return "text-cc-ink-faint";
}

/* The VOID overstamp that rotates in over the breaking ticket once on
   first paint. */
interface VoidStampProps {
  readonly play: boolean;
}

function VoidStamp({ play }: VoidStampProps) {
  return (
    <motion.span
      aria-hidden
      initial={play ? { opacity: 0, scale: 1.4, rotate: -24 } : false}
      animate={play ? { opacity: 1, scale: 1, rotate: -14 } : undefined}
      transition={{ duration: 0.5, ease: EASE_OUT, delay: 0.7 }}
      className="text-cc-danger border-cc-danger pointer-events-none absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 rounded-md border-[3px] px-4 py-1.5 font-mono text-lg font-bold tracking-[0.3em] select-none"
      style={play ? undefined : { rotate: "-14deg" }}
    >
      VOID
    </motion.span>
  );
}

function HeroTicket() {
  const reduce = useReducedMotion();
  const play = !reduce;
  return (
    <Ticket hover className="overflow-hidden">
      <div className="grid sm:grid-cols-[1fr_15rem]">
        {/* MAIN STUB: the artifact (schema diff) */}
        <div className="border-cc-card-border border-b border-dashed sm:border-r sm:border-b-0">
          <div className="border-cc-card-border flex items-center justify-between border-b border-dashed px-4 py-3">
            <span className="text-cc-ink-dim font-mono text-[0.7rem]">
              schema.graphql
            </span>
            <span className="text-cc-accent font-mono text-[0.62rem] tracking-[0.16em] uppercase">
              orders-api
            </span>
          </div>
          <div className="font-mono text-[0.76rem] leading-relaxed">
            {HERO_DIFF.map((line, i) => (
              <div
                key={i}
                className={`flex items-center gap-3 px-4 py-1.5 ${diffRowTint(line.sign)}`}
              >
                <span className={`w-3 shrink-0 ${diffSignInk(line.sign)}`}>
                  {line.sign}
                </span>
                <span
                  className={`min-w-0 flex-1 truncate ${
                    line.sign === " " ? "text-cc-ink-dim" : "text-cc-prose"
                  }`}
                >
                  {line.text}
                </span>
                {line.status !== undefined && (
                  <span
                    className={`shrink-0 font-mono text-[0.54rem] tracking-[0.12em] ${STATUS_META[line.status].text}`}
                  >
                    {STATUS_META[line.status].label}
                  </span>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* CARBON STUB: serial, date, destination, classification punch */}
        <div className="relative flex flex-col gap-4 px-5 py-5">
          <div>
            <div className="text-cc-nav-label font-mono text-[0.56rem] tracking-[0.2em] uppercase">
              serial
            </div>
            <div className="text-cc-heading mt-1 font-mono text-[0.92rem] tracking-[0.08em]">
              CHG-2026-0482
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <div className="text-cc-nav-label font-mono text-[0.56rem] tracking-[0.2em] uppercase">
                stamped
              </div>
              <div className="text-cc-prose mt-1 font-mono text-[0.7rem]">
                2026-06-23
              </div>
            </div>
            <div>
              <div className="text-cc-nav-label font-mono text-[0.56rem] tracking-[0.2em] uppercase">
                destination
              </div>
              <div className="text-cc-prose mt-1 font-mono text-[0.7rem]">
                publish
              </div>
            </div>
          </div>
          <div>
            <div className="text-cc-nav-label mb-2 font-mono text-[0.56rem] tracking-[0.2em] uppercase">
              classification
            </div>
            <motion.div
              initial={play ? { opacity: 0 } : false}
              animate={play ? { opacity: 1 } : undefined}
              transition={{ duration: 0.4, ease: EASE_OUT }}
            >
              <PunchStrip verdict="breaking" />
            </motion.div>
          </div>
          <VoidStamp play={play} />
        </div>
      </div>
      <div className="border-cc-card-border flex items-center justify-between border-t border-dashed px-4 py-2.5">
        <span className="text-cc-danger flex items-center gap-2 font-mono text-[0.64rem]">
          <span className="bg-cc-danger h-2 w-2 rounded-full" />
          gate failed
        </span>
        <span className="text-cc-nav-label font-mono text-[0.64rem]">
          1 breaking · 1 dangerous · 1 safe
        </span>
      </div>
    </Ticket>
  );
}

function HeroSection() {
  return (
    <section className="pt-6 pb-4">
      <div className="mb-7 flex justify-center">
        <StubChip>Stub No. 00 · Release Safety</StubChip>
      </div>
      <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto max-w-3xl text-center leading-[1.08] font-bold tracking-tight text-balance">
        Stamp every schema change before it boards.
      </h1>
      <p className="lead text-cc-ink-dim mx-auto mt-6 max-w-2xl text-center text-pretty">
        Every edit is a ticket. It gets punched safe, dangerous, or breaking,
        validated against the clients you have actually published, and only then
        promoted. Breaking tickets are voided at the gate.
      </p>
      <div className="mt-9 flex flex-wrap justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/platform/continuous-integration">
          Read the Docs
        </OutlineButton>
      </div>
      <div className="relative mx-auto mt-12 max-w-3xl">
        <div
          aria-hidden
          className="absolute -inset-8 -z-10 rounded-full opacity-70 blur-3xl"
          style={{
            background:
              "radial-gradient(50% 50% at 50% 0%, rgba(94,234,212,0.16), transparent 70%)",
          }}
        />
        <HeroTicket />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* PUNCH LEGEND - three mini stubs explaining the classifications      */
/* ------------------------------------------------------------------ */

interface LegendItem {
  readonly status: ChangeStatus;
  readonly title: string;
  readonly body: string;
}

const LEGEND: readonly LegendItem[] = [
  {
    status: "safe",
    title: "Punches clean",
    body: "An additive change, like a new optional field, boards without ceremony. The ticket clears the gate.",
  },
  {
    status: "dangerous",
    title: "Punched for review",
    body: "A deprecation or a widened input still boards, but the stub is flagged so a reviewer reads it before it ships.",
  },
  {
    status: "breaking",
    title: "Stamped VOID",
    body: "A removal a published client still reads cannot board. The ticket is voided and merge is held.",
  },
];

function PunchLegendSection() {
  return (
    <section>
      <div className="mb-7 flex flex-col items-center gap-3 text-center">
        <StubChip>Stub No. 01 · The Punch</StubChip>
        <h2 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
          Three columns. One punch.
        </h2>
        <p className="text-body text-cc-ink-dim max-w-2xl">
          The registry diffs the proposed schema against the version in use and
          punches exactly one column on the ticket.
        </p>
      </div>
      <Ticket className="px-5 py-5 sm:px-7 sm:py-7">
        <div className="grid gap-px sm:grid-cols-3">
          {LEGEND.map((item, i) => (
            <div
              key={item.status}
              className={`flex flex-col gap-4 px-4 py-2 ${
                i > 0
                  ? "border-cc-card-border border-t border-dashed pt-6 sm:border-t-0 sm:border-l sm:pt-2 sm:pl-7"
                  : ""
              }`}
            >
              <div className="flex items-center gap-3">
                <Punch active tone={item.status} />
                <span
                  className={`font-mono text-[0.66rem] tracking-[0.16em] ${STATUS_META[item.status].text}`}
                >
                  {STATUS_META[item.status].label}
                </span>
              </div>
              <div>
                <div className="text-cc-heading text-[0.92rem] font-medium">
                  {item.title}
                </div>
                <p className="text-cc-ink-dim mt-2 text-[0.84rem] leading-relaxed">
                  {item.body}
                </p>
              </div>
            </div>
          ))}
        </div>
      </Ticket>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* REGISTRY ROLL - a vertical stack of change tickets off the roll      */
/* ------------------------------------------------------------------ */

interface RollTicket {
  readonly serial: string;
  readonly date: string;
  readonly change: string;
  readonly verdict: ChangeStatus;
  readonly note: string;
  readonly voided?: boolean;
}

const ROLL: readonly RollTicket[] = [
  {
    serial: "CHG-2026-0479",
    date: "2026-06-18",
    change: "add Order.totalAmount: Money!",
    verdict: "safe",
    note: "additive · boarded",
  },
  {
    serial: "CHG-2026-0480",
    date: "2026-06-20",
    change: "deprecate Order.placedAt",
    verdict: "dangerous",
    note: "deprecation · flagged for review",
  },
  {
    serial: "CHG-2026-0482",
    date: "2026-06-23",
    change: "remove Order.total: Float!",
    verdict: "breaking",
    note: "removal in use · held",
    voided: true,
  },
];

interface RollTicketCardProps {
  readonly ticket: RollTicket;
  readonly index: number;
}

function RollTicketCard({ ticket, index }: RollTicketCardProps) {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  return (
    <motion.div
      ref={ref}
      initial={reduce ? false : { opacity: 0, y: 8 }}
      animate={inView && !reduce ? { opacity: 1, y: 0 } : undefined}
      transition={{ duration: 0.4, ease: EASE_OUT, delay: index * 0.08 }}
    >
      <Ticket hover>
        <div className="grid items-center gap-4 px-5 py-4 sm:grid-cols-[1fr_auto]">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-5">
            <div className="sm:w-36 sm:shrink-0">
              <div className="text-cc-heading font-mono text-[0.78rem] tracking-[0.06em]">
                {ticket.serial}
              </div>
              <div className="text-cc-nav-label mt-0.5 font-mono text-[0.62rem]">
                {ticket.date}
              </div>
            </div>
            <div className="border-cc-card-border min-w-0 sm:border-l sm:border-dashed sm:pl-5">
              <div className="text-cc-prose truncate font-mono text-[0.78rem]">
                {ticket.change}
              </div>
              <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.64rem]">
                {ticket.note}
              </div>
            </div>
          </div>
          <div className="relative flex items-center justify-end gap-3">
            <span
              className={`font-mono text-[0.6rem] tracking-[0.14em] ${STATUS_META[ticket.verdict].text}`}
            >
              {STATUS_META[ticket.verdict].label}
            </span>
            <Punch active tone={ticket.verdict} />
            {ticket.voided === true && (
              <span
                aria-hidden
                className="text-cc-danger border-cc-danger absolute -top-1 right-0 rotate-[-12deg] rounded border-2 px-1.5 py-0.5 font-mono text-[0.6rem] font-bold tracking-[0.2em] select-none"
              >
                VOID
              </span>
            )}
          </div>
        </div>
      </Ticket>
    </motion.div>
  );
}

function RegistryRollSection() {
  return (
    <section>
      <div className="mb-7 flex flex-col items-center gap-3 text-center">
        <StubChip>Stub No. 02 · The Roll</StubChip>
        <h2 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
          The roll keeps every ticket.
        </h2>
        <p className="text-body text-cc-ink-dim max-w-2xl">
          The registry keeps the full history of your schema, each change
          classified in place. You can see exactly when a contract shifted and
          how a risky change was reshaped before it shipped.
        </p>
      </div>
      <div className="relative mx-auto max-w-3xl">
        {/* The perforated connector running down the roll. */}
        <span
          aria-hidden
          className="border-cc-card-border absolute top-4 bottom-4 left-1/2 -z-10 -translate-x-1/2 border-l border-dashed"
        />
        <div className="flex flex-col gap-5">
          {ROLL.map((t, i) => (
            <RollTicketCard key={t.serial} ticket={t} index={i} />
          ))}
        </div>
        <p className="text-cc-ink-dim mt-6 text-center font-mono text-[0.68rem] leading-relaxed">
          The breaking removal never boarded. It was reshaped as an additive
          change, deprecated, and only dropped once client usage cleared.
        </p>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CI GATE - the conductor's punch: validate then publish               */
/* ------------------------------------------------------------------ */

interface GateStationProps {
  readonly kicker: string;
  readonly label: string;
  readonly sub: string;
  readonly tone: "validate" | "publish";
}

function GateStation({ kicker, label, sub, tone }: GateStationProps) {
  return (
    <div
      className={`bg-cc-hover flex-1 rounded-[4px] border border-dashed px-4 py-4 ${
        tone === "validate" ? "border-cc-accent/40" : "border-cc-card-border"
      }`}
    >
      <div className="text-cc-nav-label font-mono text-[0.56rem] tracking-[0.2em] uppercase">
        {kicker}
      </div>
      <div
        className={`mt-1 flex items-center gap-2 font-mono text-[0.88rem] font-semibold ${
          tone === "validate" ? "text-cc-accent" : "text-cc-heading"
        }`}
      >
        {label}
        {tone === "validate" && (
          <span className="text-cc-accent">
            <CheckIcon size={12} />
          </span>
        )}
      </div>
      <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.62rem]">
        {sub}
      </div>
    </div>
  );
}

function GateConnector() {
  return (
    <div className="flex items-center justify-center" aria-hidden>
      <span className="border-cc-accent/50 h-px w-8 border-t border-dashed sm:w-12" />
      <svg
        viewBox="0 0 12 12"
        width={11}
        height={11}
        className="text-cc-accent/60"
      >
        <path
          d="M2 2 L9 6 L2 10"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </div>
  );
}

function CiGateSection() {
  return (
    <section>
      <div className="mb-7 flex flex-col items-center gap-3 text-center">
        <StubChip>Stub No. 03 · The Gate</StubChip>
        <h2 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
          Validate, then publish. Never the other way around.
        </h2>
        <p className="text-body text-cc-ink-dim max-w-2xl">
          Release safety is a two-station gate. A ticket is punched and checked
          against published clients first; only tickets that clear validation
          board for publish. A breaking ticket is overstamped VOID at the door.
        </p>
      </div>
      <Ticket className="px-5 py-6 sm:px-8 sm:py-8">
        <div className="flex flex-col items-stretch gap-4 sm:flex-row sm:items-center">
          <GateStation
            kicker="station 01"
            label="validate"
            sub="punch + check clients"
            tone="validate"
          />
          <div className="rotate-90 sm:rotate-0">
            <GateConnector />
          </div>
          <GateStation
            kicker="station 02"
            label="publish"
            sub="boards only when green"
            tone="publish"
          />
        </div>
        <div className="border-cc-card-border relative mt-6 flex items-center justify-between gap-4 rounded-[4px] border border-dashed px-4 py-3">
          <div className="min-w-0">
            <div className="text-cc-heading font-mono text-[0.76rem]">
              CHG-2026-0482 · remove Order.total
            </div>
            <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.62rem]">
              merge held until the contract is resolved
            </div>
          </div>
          <span
            aria-hidden
            className="text-cc-danger border-cc-danger shrink-0 rotate-[-10deg] rounded-md border-2 px-3 py-1 font-mono text-sm font-bold tracking-[0.25em] select-none"
          >
            VOID
          </span>
        </div>
        <p className="text-cc-ink-dim mt-5 font-mono text-[0.7rem] leading-relaxed">
          Nothing reaches <span className="text-cc-accent">publish</span> until
          it clears <span className="text-cc-accent">validate</span>. The same
          punch that stamps the diff posts back as a required check on the pull
          request, so unsafe releases stop before a consumer discovers them.
        </p>
      </Ticket>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* PUBLISHED CLIENTS - carbon copies torn off the main ticket           */
/* ------------------------------------------------------------------ */

interface ClientCarbon {
  readonly name: string;
  readonly env: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "queued";
}

const CLIENTS: readonly ClientCarbon[] = [
  { name: "web", env: "production", ok: 5, total: 5, status: "ok" },
  { name: "mobile", env: "production", ok: 3, total: 5, status: "risk" },
  { name: "partner", env: "sandbox", ok: 0, total: 4, status: "queued" },
  { name: "internal-admin", env: "staging", ok: 6, total: 6, status: "ok" },
];

interface PunchMarksProps {
  readonly ok: number;
  readonly total: number;
  readonly status: ClientCarbon["status"];
}

function PunchMarks({ ok, total, status }: PunchMarksProps) {
  const color =
    status === "ok"
      ? "bg-cc-success"
      : status === "risk"
        ? "bg-cc-warning"
        : "bg-cc-nav-label/50";
  return (
    <span className="flex gap-1.5" aria-hidden>
      {Array.from({ length: total }).map((_, i) => (
        <span
          key={i}
          className={`h-2.5 w-2.5 rounded-full ${
            i < ok ? color : "border-cc-card-border border border-dashed"
          }`}
        />
      ))}
    </span>
  );
}

function ClientsSection() {
  const statusLabel: Record<
    ClientCarbon["status"],
    { text: string; cls: string }
  > = {
    ok: { text: "clear", cls: "text-cc-success" },
    risk: { text: "at risk", cls: "text-cc-warning" },
    queued: { text: "queued", cls: "text-cc-nav-label" },
  };
  return (
    <section>
      <div className="mb-7 flex flex-col items-center gap-3 text-center">
        <StubChip>Stub No. 04 · The Carbon Copy</StubChip>
        <h2 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
          A carbon copy for every published client.
        </h2>
        <p className="text-body text-cc-ink-dim max-w-2xl">
          The client registry tracks the operations your published clients
          actually run. A carbon stub tears off for each one, showing passing
          operations as small punch marks. This is the published clients
          affected, by name and by environment.
        </p>
      </div>
      <Ticket className="px-1 py-1">
        <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[1.3fr_1fr_0.7fr] gap-3 border-b border-dashed px-5 py-3 font-mono text-[0.56rem] tracking-[0.16em] uppercase">
          <span>client</span>
          <span>operations passing</span>
          <span className="text-right">status</span>
        </div>
        {CLIENTS.map((c) => {
          const s = statusLabel[c.status];
          return (
            <div
              key={c.name}
              className="border-cc-card-border grid grid-cols-[1.3fr_1fr_0.7fr] items-center gap-3 border-b border-dashed px-5 py-3.5 last:border-b-0"
            >
              <div className="min-w-0">
                <div className="text-cc-heading truncate font-mono text-[0.76rem]">
                  {c.name}
                </div>
                <div className="text-cc-nav-label font-mono text-[0.6rem]">
                  {c.env}
                </div>
              </div>
              <div className="flex items-center gap-3">
                <PunchMarks ok={c.ok} total={c.total} status={c.status} />
                <span className="text-cc-ink-dim font-mono text-[0.66rem]">
                  {c.ok}/{c.total}
                </span>
              </div>
              <div
                className={`text-right font-mono text-[0.68rem] font-semibold ${s.cls}`}
              >
                {s.text}
              </div>
            </div>
          );
        })}
      </Ticket>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* HONESTY STUB - a smaller half-ticket on scope                        */
/* ------------------------------------------------------------------ */

interface HonestyItem {
  readonly tone: "safe" | "dangerous";
  readonly head: string;
  readonly body: ReactNode;
}

const HONESTY: readonly HonestyItem[] = [
  {
    tone: "safe",
    head: "What it stops",
    body: "Releases that would break a published client are held before merge, with the breaking line and reason on the stub.",
  },
  {
    tone: "dangerous",
    head: "What it needs",
    body: "A client is only guarded once its operations are registered. Unregistered traffic is outside the net.",
  },
  {
    tone: "safe",
    head: "What it surfaces",
    body: "Strawberry Shake regenerates clients via MSBuild codegen, so contract drift shows up in your build.",
  },
  {
    tone: "dangerous",
    head: "What it will not pretend",
    body: "It reports published clients affected. It does not claim certainty about consumers it has never seen.",
  },
];

function HonestySection() {
  return (
    <section className="mx-auto max-w-3xl">
      <Ticket className="px-6 py-7 sm:px-9 sm:py-9">
        <div className="flex flex-col items-start gap-3">
          <StubChip>Stub No. 05 · The Fine Print</StubChip>
          <h2 className="font-heading text-cc-heading text-h5 font-semibold tracking-tight">
            A safety net, not a blindfold.
          </h2>
          <p className="text-body text-cc-ink-dim text-pretty">
            A punch is only useful if you can trust what it claims. Release
            safety tells you which published clients are affected by a change,
            based on the operations they have registered. It is honest about its
            edges.
          </p>
        </div>
        <ul className="mt-6 grid gap-3 sm:grid-cols-2">
          {HONESTY.map((item) => (
            <li
              key={item.head}
              className="border-cc-card-border bg-cc-hover relative rounded-[4px] border border-dashed px-4 py-3.5"
            >
              <div className="flex items-center gap-2">
                <span
                  className={`h-1.5 w-1.5 rounded-full ${STATUS_META[item.tone].dot}`}
                />
                <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.16em] uppercase">
                  {item.head}
                </span>
              </div>
              <p className="text-cc-ink-dim mt-2 text-[0.82rem] leading-relaxed">
                {item.body}
              </p>
            </li>
          ))}
        </ul>
      </Ticket>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* CLOSING CTA - a blank ticket waiting to be stamped                   */
/* ------------------------------------------------------------------ */

function ClosingSection() {
  return (
    <section className="mx-auto max-w-3xl">
      <Ticket className="overflow-hidden">
        <div
          aria-hidden
          className="absolute inset-0 -z-10"
          style={{
            background:
              "radial-gradient(60% 100% at 50% 0%, rgba(94,234,212,0.12), transparent 65%)",
          }}
        />
        <div className="grid sm:grid-cols-[1fr_13rem]">
          <div className="px-7 py-10 text-center sm:py-12">
            <h2 className="font-heading text-cc-heading text-h4 mx-auto max-w-xl font-bold tracking-tight text-balance">
              Punch the next ticket clean.
            </h2>
            <p className="text-body text-cc-ink-dim mx-auto mt-4 max-w-md text-pretty">
              Classify, validate, and gate your releases so breaking changes
              stop at the door, not in your users&rsquo; hands.
            </p>
            <div className="mt-8 flex flex-wrap justify-center gap-3">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/platform/continuous-integration">
                Read the Docs
              </OutlineButton>
            </div>
          </div>
          <div className="border-cc-card-border flex flex-col justify-center gap-4 border-t border-dashed px-5 py-6 sm:border-t-0 sm:border-l">
            <div>
              <div className="text-cc-nav-label font-mono text-[0.56rem] tracking-[0.2em] uppercase">
                serial
              </div>
              <div className="text-cc-heading mt-1 font-mono text-[0.92rem] tracking-[0.08em]">
                CHG-NEXT
              </div>
            </div>
            <div>
              <div className="text-cc-nav-label mb-2 font-mono text-[0.56rem] tracking-[0.2em] uppercase">
                awaiting punch
              </div>
              <div className="grid grid-cols-3 gap-2">
                {(["safe", "dangerous", "breaking"] as ChangeStatus[]).map(
                  (c) => (
                    <div key={c} className="flex flex-col items-center gap-1.5">
                      <span
                        aria-hidden
                        className="border-cc-card-border h-5 w-5 rounded-full border border-dashed"
                      />
                      <span className="text-cc-nav-label font-mono text-[0.5rem] tracking-[0.1em]">
                        {STATUS_META[c].label}
                      </span>
                    </div>
                  ),
                )}
              </div>
            </div>
          </div>
        </div>
      </Ticket>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* PAGE                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  return (
    <div className="relative">
      {/* Receipt-roll paper running down behind the ticket stack. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-full opacity-60"
        style={{ backgroundImage: ROLL_PATTERN }}
      />
      <div className="mx-auto flex max-w-5xl flex-col gap-24 py-6 sm:gap-28">
        <HeroSection />
        <PunchLegendSection />
        <RegistryRollSection />
        <CiGateSection />
        <ClientsSection />
        <HonestySection />
        <ClosingSection />
      </div>
    </div>
  );
}
