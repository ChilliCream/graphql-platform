import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { SectionShell } from "@/src/components/SectionShell";
import { ConnectorArrowGlyph } from "@/src/icons/ConnectorArrowGlyph";
import { CrossGlyph } from "@/src/icons/CrossGlyph";
import { SpinnerGlyph } from "@/src/icons/SpinnerGlyph";

const GUARDRAIL = "#0a1426";
const GUARDRAIL_LINE = "rgba(124, 146, 198, 0.16)";

interface GateNodeProps {
  readonly label: string;
  readonly state: string;
  readonly stateClassName?: string;
  readonly icon?: ReactNode;
  readonly tone: "passed" | "future";
}

function GateNode({
  label,
  state,
  stateClassName = "",
  icon,
  tone,
}: GateNodeProps) {
  const tones = {
    passed: "border-cc-success/40 bg-cc-success/[0.06] text-cc-success",
    future: "border-cc-card-border bg-cc-hover/40 text-cc-ink-dim opacity-70",
  } as const;
  return (
    <div className={`flex-1 rounded-lg border ${tones[tone]} px-4 py-4`}>
      <div className="flex items-center gap-2 font-mono text-[0.85rem] font-semibold">
        {icon !== undefined && (
          <span className="flex h-4 w-4 items-center justify-center">
            {icon}
          </span>
        )}
        {label}
      </div>
      <div
        className={`text-cc-ink-dim mt-0.5 font-mono text-[0.64rem] ${stateClassName}`}
      >
        {state}
      </div>
    </div>
  );
}

interface ConnectorProps {
  readonly muted?: boolean;
}

function Connector({ muted = false }: ConnectorProps) {
  return (
    <div className="flex items-center justify-center" aria-hidden>
      <ConnectorArrowGlyph
        dashed={muted}
        width={40}
        height={24}
        className={`rotate-90 sm:rotate-0 ${muted ? "text-cc-nav-label/30" : "text-cc-nav-label/60"}`}
      />
    </div>
  );
}

function StagingGateNode() {
  return (
    <div className="relative flex-1">
      <div
        className="border-cc-warning/40 bg-cc-warning/[0.06] text-cc-warning rounded-lg border px-4 py-4 motion-safe:animate-[staging-validating_6s_ease-in-out_infinite] motion-reduce:hidden"
        aria-hidden="true"
      >
        <div className="flex items-center gap-2 font-mono text-[0.85rem] font-semibold">
          <span className="flex h-4 w-4 items-center justify-center">
            <SpinnerGlyph
              width={12}
              height={12}
              className="animate-spin motion-reduce:animate-none"
            />
          </span>
          Staging
        </div>
        <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.64rem]">
          Validating
        </div>
      </div>
      <div className="border-cc-danger/40 bg-cc-danger/[0.06] text-cc-danger absolute inset-0 rounded-lg border px-4 py-4 opacity-0 motion-safe:animate-[staging-failed_6s_ease-in-out_infinite] motion-reduce:opacity-100">
        <div className="flex items-center gap-2 font-mono text-[0.85rem] font-semibold">
          <span className="flex h-4 w-4 items-center justify-center">
            <CrossGlyph width={12} height={12} />
          </span>
          Staging
        </div>
        <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.64rem]">
          Failed
        </div>
      </div>
    </div>
  );
}

function GateSchematic() {
  return (
    <div
      className="border-cc-card-border relative overflow-hidden rounded-xl border p-6 sm:p-8"
      style={{
        backgroundColor: GUARDRAIL,
        backgroundImage: `linear-gradient(${GUARDRAIL_LINE} 1px, transparent 1px), linear-gradient(90deg, ${GUARDRAIL_LINE} 1px, transparent 1px)`,
        backgroundSize: "26px 26px",
      }}
    >
      <style href="release-safety-gate-schematic" precedence="medium">{`
        @keyframes staging-validating {
          0%, 45% { opacity: 1; }
          55%, 100% { opacity: 0; }
        }
        @keyframes staging-failed {
          0%, 45% { opacity: 0; }
          55%, 100% { opacity: 1; }
        }
      `}</style>
      <div className="flex flex-col items-stretch gap-3 sm:flex-row sm:items-center">
        <GateNode
          label="Development"
          state="Passed"
          icon={<CheckIcon size={14} />}
          tone="passed"
        />
        <Connector />
        <StagingGateNode />
        <Connector muted />
        <GateNode
          label="Production"
          state="Future"
          stateClassName="invisible"
          tone="future"
        />
      </div>
    </div>
  );
}

export function GateSection() {
  return (
    <SectionShell
      title="Every environment is its own gate."
      lead="Development, staging, and production each hold their own published operations, so the same change is validated against what actually runs in each of them. A change that passes staging can still fail production, because different client versions are published there."
      artifact={<GateSchematic />}
      flip
    />
  );
}
