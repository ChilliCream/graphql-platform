import type { CSSProperties, ReactNode } from "react";

import { BlockMark } from "@/src/icons/BlockMark";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { RegistryNodeIcon } from "@/src/icons/RegistryNodeIcon";

interface RevealProps {
  /** Name of one of the `<style>` block's `@keyframes`. */
  readonly keyframeName: string;
  /** Seconds into the shared cycle this instance's local timeline starts. */
  readonly delay: number;
  readonly className?: string;
  readonly children?: ReactNode;
}

/** One CSS-keyframe-driven element: duration, easing and the play/pause gate live on the shared `.pw-anim` class; only the shape and its phase differ per instance. */
export function Reveal({
  keyframeName,
  delay,
  className = "",
  children,
}: RevealProps) {
  const style: CSSProperties = {
    animationName: keyframeName,
    animationDelay: `${delay}s`,
  };
  return (
    <span className={`pw-anim ${className}`} style={style}>
      {children}
    </span>
  );
}

/** The registry destination node: a hexagon mark and its label. */
export function RegistryNode() {
  return (
    <div className="border-cc-accent/40 bg-cc-accent/[0.06] text-cc-accent flex shrink-0 items-center gap-1.5 rounded-lg border px-2.5 py-2">
      <RegistryNodeIcon className="shrink-0" />
      <span className="font-mono text-[0.7rem] whitespace-nowrap">
        Nitro registry
      </span>
    </div>
  );
}

/** The dock a document departs from before it travels to the registry. */
export function DockLabel() {
  return (
    <div className="border-cc-card-border text-cc-ink-dim rounded-lg border border-dashed px-2.5 py-2">
      <span className="font-mono text-[0.7rem] whitespace-nowrap">
        incoming
      </span>
    </div>
  );
}

interface TravelCardProps {
  readonly version: string;
  readonly diff: string;
  readonly diffTone: "danger" | "warning";
  readonly verdict: string;
  readonly verdictTone: "danger" | "success";
  readonly schemaFile: string;
  /** Seconds into the shared 11s cycle this card's own travel starts. */
  readonly delay: number;
}

const DIFF_TONE_CLASS = {
  danger: "text-cc-danger",
  warning: "text-cc-warning",
} as const;

const VERDICT_TONE_CLASS = {
  danger: "border-cc-danger/40 bg-cc-danger/[0.08] text-cc-danger",
  success: "border-cc-success/40 bg-cc-success/[0.08] text-cc-success",
} as const;

/**
 * A schema document travelling from the dock to the registry: it fades in,
 * moves across the lane, holds while it is analyzed, is stamped with a
 * verdict, then fades out before the next document arrives.
 */
export function TravelCard({
  version,
  diff,
  diffTone,
  verdict,
  verdictTone,
  schemaFile,
  delay,
}: TravelCardProps) {
  const VerdictIcon = verdictTone === "danger" ? BlockMark : CheckGlyph;
  return (
    <Reveal
      keyframeName="pw-doc-travel"
      delay={delay}
      className="border-cc-card-border bg-cc-card-bg absolute top-1/2 left-[4%] z-10 w-[9.5rem] -translate-y-1/2 rounded-lg border px-2.5 py-2 opacity-0 shadow-[0_8px_20px_-12px_rgba(0,0,0,0.6)] @min-[420px]:w-[11.5rem]"
    >
      <div className="flex items-center justify-between gap-1">
        <span className="text-cc-heading font-mono text-[0.7rem] font-semibold">
          {version}
        </span>
        <span className="text-cc-ink-faint hidden font-mono text-[0.7rem] @min-[420px]:inline">
          schema
        </span>
      </div>
      <div className="text-cc-ink-dim mt-0.5 truncate font-mono text-[0.7rem]">
        {schemaFile}
      </div>
      <code
        className={`mt-1 block font-mono text-[0.7rem] ${DIFF_TONE_CLASS[diffTone]}`}
      >
        {diff}
      </code>
      <Reveal
        keyframeName="pw-stamp"
        delay={delay}
        className={`mt-1.5 flex items-center gap-1 rounded border px-1.5 py-0.5 ${VERDICT_TONE_CLASS[verdictTone]}`}
      >
        <VerdictIcon width={11} height={11} className="shrink-0" />
        <span className="font-mono text-[0.7rem] whitespace-nowrap">
          {verdict}
        </span>
      </Reveal>
    </Reveal>
  );
}

interface RowOutcomeProps {
  readonly tone: "danger" | "success";
  readonly text: string;
  readonly delay: number;
}

/** One row's outcome for a single pass: a tick or a flag, staggered in as the scan reaches it. */
export function RowOutcome({ tone, text, delay }: RowOutcomeProps) {
  const Icon = tone === "danger" ? BlockMark : CheckGlyph;
  const toneClass = tone === "danger" ? "text-cc-danger" : "text-cc-success";
  return (
    <Reveal
      keyframeName="pw-check"
      delay={delay}
      className={`absolute inset-0 flex items-center justify-end gap-1.5 ${toneClass}`}
    >
      <Icon width={12} height={12} className="shrink-0" />
      <span className="font-mono text-[0.7rem] whitespace-nowrap">{text}</span>
    </Reveal>
  );
}

interface ClientRowProps {
  readonly label: string;
  /** Outcome shown while the first (breaking) document is analyzed. */
  readonly first: RowOutcomeProps;
  /** Outcome shown while the second (safe) document is analyzed. */
  readonly second: RowOutcomeProps;
}

/** One registered client operation, with its outcome for each pass of the loop. */
export function ClientRow({ label, first, second }: ClientRowProps) {
  return (
    <div className="border-cc-card-border flex items-center justify-between gap-3 border-b px-3 py-2 last:border-b-0">
      <span className="text-cc-heading font-mono text-[0.7rem]">{label}</span>
      <span className="relative h-4 min-w-0 flex-1">
        <RowOutcome {...first} />
        <RowOutcome {...second} />
      </span>
    </div>
  );
}

/** The highlight bar that sweeps the client list once per document analyzed. */
export function ScanBar() {
  return (
    <Reveal
      keyframeName="pw-scan"
      delay={0}
      className="bg-cc-accent/50 pointer-events-none absolute inset-x-0 top-0 h-0.5 rounded-full"
    />
  );
}
