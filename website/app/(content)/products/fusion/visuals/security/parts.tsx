import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { CrossGlyph } from "@/src/icons/CrossGlyph";

import { EVENTS } from "./data";
import type { CheckStatus } from "./data";

const VERDICT_TEXTS = ["denied · 403", "rejected · not in safelist"] as const;

export type NodeTone = "active" | "success" | "danger";

interface NodeCardProps {
  readonly label: string;
  readonly lit: boolean;
  readonly tone: NodeTone;
}

const CARD_BASE =
  "rounded-md border px-2 py-1.5 text-center font-mono text-[0.7rem] transition-colors duration-500";
const CARD_IDLE = "border-cc-card-border text-cc-ink-dim";
const CARD_TONE: Record<NodeTone, string> = {
  active: "border-cc-accent/50 bg-cc-accent/[0.08] text-cc-accent",
  success: "border-cc-success/50 bg-cc-success/[0.08] text-cc-success",
  danger: "border-cc-danger/50 bg-cc-danger/[0.08] text-cc-danger",
};

export function NodeCard({ label, lit, tone }: NodeCardProps) {
  return (
    <div className={`${CARD_BASE} ${lit ? CARD_TONE[tone] : CARD_IDLE}`}>
      {label}
    </div>
  );
}

interface CheckRowProps {
  readonly label: string;
  readonly status: CheckStatus;
  readonly code?: string;
}

const CHECK_ROW_BASE =
  "flex items-center justify-between gap-2 rounded-md border px-2 py-1 font-mono text-[0.7rem] transition-colors duration-500";
const CHECK_ROW_TONE: Record<CheckStatus, string> = {
  idle: "border-cc-card-border text-cc-ink-dim",
  pass: "border-cc-success/50 bg-cc-success/[0.08] text-cc-success",
  fail: "border-cc-danger/50 bg-cc-danger/[0.08] text-cc-danger",
};

export function CheckRow({ label, status, code }: CheckRowProps) {
  return (
    <div className={`${CHECK_ROW_BASE} ${CHECK_ROW_TONE[status]}`}>
      <span className="min-w-0 truncate">{label}</span>
      <span className="flex shrink-0 items-center gap-1">
        {code !== undefined && status === "fail" && (
          <span className="text-[0.7rem]">{code}</span>
        )}
        <span className="flex h-3 w-3 items-center justify-center">
          {status === "pass" && <CheckGlyph className="h-3 w-3" />}
          {status === "fail" && <CrossGlyph className="h-3 w-3" />}
        </span>
      </span>
    </div>
  );
}

interface TrackProps {
  readonly active: boolean;
  readonly tone: NodeTone;
  readonly stepKey: string;
}

const DOT_BASE = "absolute h-2 w-2 rounded-full";
const DOT_TONE: Record<NodeTone, string> = {
  active: "bg-cc-accent shadow-[0_0_8px_var(--color-cc-accent)]",
  success: "bg-cc-success shadow-[0_0_8px_var(--color-cc-success)]",
  danger: "bg-cc-danger shadow-[0_0_8px_var(--color-cc-danger)]",
};

export function Track({ active, tone, stepKey }: TrackProps) {
  return (
    <div className="relative h-10 w-full @min-[480px]:h-auto @min-[480px]:w-12 @min-[480px]:self-stretch">
      <span className="bg-cc-card-border absolute top-0 left-1/2 h-full w-px @min-[480px]:top-1/2 @min-[480px]:left-0 @min-[480px]:h-px @min-[480px]:w-full" />
      {active && (
        <span
          key={`col-${stepKey}`}
          className="absolute top-0 left-1/2 -ml-1 h-full w-2 motion-safe:animate-[sec-pulse-col_900ms_ease-in-out_both] motion-reduce:hidden @min-[480px]:hidden"
        >
          <span className={`${DOT_BASE} ${DOT_TONE[tone]} top-0 left-0`} />
        </span>
      )}
      {active && (
        <span
          key={`row-${stepKey}`}
          className="absolute top-1/2 left-0 -mt-1 hidden h-2 w-full motion-safe:animate-[sec-pulse-row_900ms_ease-in-out_both] motion-reduce:hidden @min-[480px]:block"
        >
          <span className={`${DOT_BASE} ${DOT_TONE[tone]} top-0 left-0`} />
        </span>
      )}
    </div>
  );
}

interface AuditLineProps {
  readonly text: string;
  readonly tone: "success" | "danger";
  readonly dim?: boolean;
}

export function AuditLine({ text, tone, dim = false }: AuditLineProps) {
  const toneClass = tone === "success" ? "text-cc-success" : "text-cc-danger";
  return (
    <div
      className={`font-mono text-[0.7rem] break-words ${toneClass} ${dim ? "opacity-45" : "opacity-100"}`}
    >
      {text}
    </div>
  );
}

interface VerdictSlotProps {
  readonly text: string | null;
}

/** Reserves the tallest of both verdict strings' height so swapping between them never shifts layout. */
export function VerdictSlot({ text }: VerdictSlotProps) {
  return (
    <div className="mt-1 grid text-center font-mono text-[0.7rem]">
      {VERDICT_TEXTS.map((verdict) => (
        <span
          key={verdict}
          aria-hidden="true"
          className="invisible col-start-1 row-start-1"
        >
          {verdict}
        </span>
      ))}
      <div
        className={`text-cc-danger col-start-1 row-start-1 ${text === null ? "invisible" : ""}`}
      >
        {text ?? VERDICT_TEXTS[0]}
      </div>
    </div>
  );
}

/** Reserves the tallest of every possible audit line's height so swapping text never shifts layout. */
export function AuditSlot(props: AuditLineProps) {
  return (
    <div className="grid">
      {EVENTS.map((event) => (
        <span
          key={event.auditLine}
          aria-hidden="true"
          className="invisible col-start-1 row-start-1 font-mono text-[0.7rem] break-words"
        >
          {event.auditLine}
        </span>
      ))}
      <div className="col-start-1 row-start-1">
        <AuditLine {...props} />
      </div>
    </div>
  );
}
