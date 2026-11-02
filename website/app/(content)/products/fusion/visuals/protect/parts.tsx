import type { CSSProperties, ReactNode } from "react";

import { BlockMark } from "@/src/icons/BlockMark";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { RegistryNodeIcon } from "@/src/icons/RegistryNodeIcon";

interface RevealProps {
  readonly keyframeName: string;
  readonly delay: number;
  readonly className?: string;
  readonly children?: ReactNode;
}

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

export function DockLabel() {
  return (
    <div className="border-cc-card-border text-cc-ink-dim rounded-lg border border-dashed px-2.5 py-2">
      <span className="font-mono text-[0.7rem] whitespace-nowrap">
        incoming
      </span>
    </div>
  );
}

const DIFF_TONE_CLASS = {
  danger: "text-cc-danger",
  warning: "text-cc-warning",
} as const;

const VERDICT_TONE_CLASS = {
  danger: "border-cc-danger/40 bg-cc-danger/[0.08] text-cc-danger",
  success: "border-cc-success/40 bg-cc-success/[0.08] text-cc-success",
} as const;

const CARD_CLASS =
  "border-cc-card-border bg-cc-card-bg z-10 block w-[9.5rem] rounded-lg border px-2.5 py-2 shadow-lg @min-[420px]:w-[11.5rem]";

interface VerdictContentProps {
  readonly tone: "danger" | "success";
  readonly text: string;
  readonly size?: number;
  readonly iconClassName?: string;
  /** The stamp's longer verdict text must wrap; short row outcomes stay on one line. */
  readonly wrap?: boolean;
}

function VerdictContent({
  tone,
  text,
  size = 11,
  iconClassName = "shrink-0",
  wrap = false,
}: VerdictContentProps) {
  const Icon = tone === "danger" ? BlockMark : CheckGlyph;
  return (
    <>
      <Icon width={size} height={size} className={iconClassName} />
      <span
        className={`font-mono text-[0.7rem] ${wrap ? "" : "whitespace-nowrap"}`}
      >
        {text}
      </span>
    </>
  );
}

interface CardFaceProps {
  readonly version: string;
  readonly diff: string;
  readonly diffTone: "danger" | "warning";
  readonly schemaFile: string;
  readonly verdict: ReactNode;
}

function CardFace({
  version,
  diff,
  diffTone,
  schemaFile,
  verdict,
}: CardFaceProps) {
  return (
    <div className={CARD_CLASS}>
      <div className="flex items-center justify-between gap-1">
        <span className="text-cc-heading font-mono text-[0.7rem] font-semibold">
          {version}
        </span>
        <span className="text-cc-ink-dim hidden font-mono text-[0.7rem] @min-[420px]:inline">
          schema
        </span>
      </div>
      <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.7rem]">
        {schemaFile}
      </div>
      <code
        className={`mt-1 block font-mono text-[0.7rem] ${DIFF_TONE_CLASS[diffTone]}`}
      >
        {diff}
      </code>
      {verdict}
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
  readonly delay: number;
  readonly axis: "x" | "y";
}

/** Static counterpart to the animated x-axis wrapper below (kept off the animated element itself, per the CLS fix). */
const Y_WRAPPER_CLASS = "absolute top-0 left-1/2 -translate-x-1/2";

export function TravelCard({
  version,
  diff,
  diffTone,
  verdict,
  verdictTone,
  schemaFile,
  delay,
  axis,
}: TravelCardProps) {
  const stamp = (
    <Reveal
      keyframeName="pw-stamp"
      delay={delay}
      className={`mt-1.5 flex items-center gap-1 rounded border px-1.5 py-0.5 ${VERDICT_TONE_CLASS[verdictTone]}`}
    >
      <VerdictContent
        tone={verdictTone}
        text={verdict}
        iconClassName="mt-0.5 shrink-0 self-start"
        wrap
      />
    </Reveal>
  );
  const face = (
    <CardFace
      version={version}
      diff={diff}
      diffTone={diffTone}
      schemaFile={schemaFile}
      verdict={stamp}
    />
  );

  if (axis === "y") {
    return (
      <span className={Y_WRAPPER_CLASS}>
        <Reveal
          keyframeName="pw-doc-travel-y"
          delay={delay}
          className="z-10 block opacity-0"
        >
          {face}
        </Reveal>
      </span>
    );
  }

  return (
    <Reveal
      keyframeName="pw-doc-travel-x"
      delay={delay}
      className="absolute inset-0 flex items-center opacity-0"
    >
      <Reveal keyframeName="pw-doc-travel-x-counter" delay={delay}>
        {face}
      </Reveal>
    </Reveal>
  );
}

interface StaticVerdictCardProps {
  readonly version: string;
  readonly diff: string;
  readonly diffTone: "danger" | "warning";
  readonly verdict: string;
  readonly verdictTone: "danger" | "success";
  readonly schemaFile: string;
}

export function StaticVerdictCard({
  version,
  diff,
  diffTone,
  verdict,
  verdictTone,
  schemaFile,
}: StaticVerdictCardProps) {
  return (
    <CardFace
      version={version}
      diff={diff}
      diffTone={diffTone}
      schemaFile={schemaFile}
      verdict={
        <span
          className={`mt-1.5 flex items-center gap-1 rounded border px-1.5 py-0.5 ${VERDICT_TONE_CLASS[verdictTone]}`}
        >
          <VerdictContent tone={verdictTone} text={verdict} wrap />
        </span>
      }
    />
  );
}

interface RowOutcomeProps {
  readonly tone: "danger" | "success";
  readonly text: string;
  readonly delay: number;
  readonly keyframeName: string;
}

export function RowOutcome({
  tone,
  text,
  delay,
  keyframeName,
}: RowOutcomeProps) {
  const toneClass = tone === "danger" ? "text-cc-danger" : "text-cc-success";
  return (
    <Reveal
      keyframeName={keyframeName}
      delay={delay}
      className={`absolute inset-0 flex items-center justify-end gap-1.5 ${toneClass}`}
    >
      <VerdictContent tone={tone} text={text} size={12} />
    </Reveal>
  );
}

interface ClientRowProps {
  readonly label: string;
  readonly first: RowOutcomeProps;
  readonly second: RowOutcomeProps;
}

export function ClientRow({ label, first, second }: ClientRowProps) {
  return (
    <div className="border-cc-card-border flex h-9 items-center justify-between gap-3 border-b px-3 last:border-b-0">
      <span className="text-cc-heading font-mono text-[0.7rem]">{label}</span>
      <span className="relative h-4 min-w-0 flex-1">
        <RowOutcome {...first} />
        <RowOutcome {...second} />
      </span>
    </div>
  );
}

interface StaticClientRowProps {
  readonly label: string;
  readonly tone: "danger" | "success";
  readonly text: string;
}

export function StaticClientRow({ label, tone, text }: StaticClientRowProps) {
  return (
    <div className="border-cc-card-border flex h-9 items-center justify-between gap-3 border-b px-3 last:border-b-0">
      <span className="text-cc-heading font-mono text-[0.7rem]">{label}</span>
      <span
        className={`flex items-center gap-1.5 ${tone === "danger" ? "text-cc-danger" : "text-cc-success"}`}
      >
        <VerdictContent tone={tone} text={text} size={12} />
      </span>
    </div>
  );
}

/** Linear so `checkDelay` in ProtectWindow.tsx can key to an exact crossing time. */
export function ScanBar() {
  return (
    <Reveal
      keyframeName="pw-scan"
      delay={0}
      className="pw-scan-el bg-cc-accent/50 pointer-events-none absolute inset-x-0 top-0 h-0.5 rounded-full"
    />
  );
}
