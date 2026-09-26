import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { CrossGlyph } from "@/src/icons/CrossGlyph";

import type { CheckStatus } from "./data";

/**
 * The small pieces the Security Checkpoint graphic draws with: a client or
 * service card, one row of the router's check stack, and the connector a
 * request pulse travels along between columns. Two DOM nodes per connector
 * (one per axis) rather than one repositioned node, the same dual-layout
 * idiom `LayeredDiagram`'s `Band` uses for its row/stacked connectors.
 */

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

/** One client or service pill, lit while a request is in flight through it. */
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
  /** Shown next to a failing check that denies rather than rejects the request. */
  readonly code?: string;
}

const CHECK_ROW_BASE =
  "flex items-center justify-between gap-2 rounded-md border px-2 py-1 font-mono text-[0.7rem] transition-colors duration-500";
const CHECK_ROW_TONE: Record<CheckStatus, string> = {
  idle: "border-cc-card-border text-cc-ink-dim",
  pass: "border-cc-success/50 bg-cc-success/[0.08] text-cc-success",
  fail: "border-cc-danger/50 bg-cc-danger/[0.08] text-cc-danger",
};

/** One row of the router's check stack: a label and its pass/fail glyph. */
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
  /** Remounts the pulse so its animation restarts for this phase. */
  readonly stepKey: string;
}

const DOT_BASE = "absolute h-2 w-2 rounded-full";
const DOT_TONE: Record<NodeTone, string> = {
  active: "bg-cc-accent shadow-[0_0_8px_var(--color-cc-accent)]",
  success: "bg-cc-success shadow-[0_0_8px_var(--color-cc-success)]",
  danger: "bg-cc-danger shadow-[0_0_8px_var(--color-cc-danger)]",
};

/** The line a request pulse travels between two columns, stacked or side by side. */
export function Track({ active, tone, stepKey }: TrackProps) {
  return (
    <div className="relative h-6 w-full @min-[480px]:h-full @min-[480px]:w-6">
      <span className="bg-cc-card-border absolute top-1/2 left-0 h-px w-full @min-[480px]:top-0 @min-[480px]:left-1/2 @min-[480px]:h-full @min-[480px]:w-px" />
      {active && (
        <span
          key={`col-${stepKey}`}
          className={`${DOT_BASE} ${DOT_TONE[tone]} left-0 motion-safe:animate-[sec-pulse-col_900ms_ease-in-out_both] motion-reduce:hidden @min-[480px]:hidden`}
          style={{ top: "-4px" }}
        />
      )}
      {active && (
        <span
          key={`row-${stepKey}`}
          className={`${DOT_BASE} ${DOT_TONE[tone]} top-1/2 hidden motion-safe:animate-[sec-pulse-row_900ms_ease-in-out_both] motion-reduce:hidden @min-[480px]:block`}
          style={{ marginTop: -4 }}
        />
      )}
    </div>
  );
}

interface AuditLineProps {
  readonly text: string;
  readonly tone: "success" | "danger";
  readonly dim?: boolean;
}

/** One line of the audit trail strip; dimmed for the line scrolling out. */
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
