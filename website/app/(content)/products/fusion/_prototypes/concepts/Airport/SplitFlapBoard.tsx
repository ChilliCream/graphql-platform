"use client";

import { useRef } from "react";

import { anim, useCycle, useElementMotion } from "./hooks";
import { AP, GATES, specMark } from "./palette";

/**
 * Hero board: the split-flap departures display. Each row is one source
 * schema checking in at its gate, and the status column flips through
 * SCHEDULED, COMPOSING and IN COMPOSITE SCHEMA as the board fills, so the
 * whole roster - two specifications, five languages - ends up on the same
 * board. At rest every row already reads IN COMPOSITE SCHEMA.
 */

const STATUS = ["SCHEDULED", "COMPOSING", "IN COMPOSITE SCHEMA"] as const;
const STEPS = GATES.length + 3;
const REST = STEPS - 1;
const BEAT = 1100;

const DEPARTS = ["06:10", "06:25", "06:40", "07:05", "07:20"] as const;

const KEYFRAMES = `
@keyframes ap-flap {
  0% { transform: rotateX(-88deg); opacity: 0.25; }
  55% { transform: rotateX(12deg); opacity: 1; }
  100% { transform: rotateX(0deg); opacity: 1; }
}
@keyframes ap-board-live { 0%, 100% { opacity: 1; } 50% { opacity: 0.25; } }
`;

interface FlapProps {
  readonly value: string;
  readonly running: boolean;
  readonly className?: string;
  readonly color?: string;
}

/** One split-flap cell: re-keyed on its value so a change flips the flap. */
function Flap({ value, running, className, color }: FlapProps) {
  return (
    <span className="block overflow-hidden" style={{ perspective: "220px" }}>
      <span
        key={value}
        className={`block truncate ${className ?? ""}`.trim()}
        style={{
          color: color ?? AP.amber,
          transformOrigin: "top center",
          animation: anim(running, "ap-flap 420ms cubic-bezier(.3,.9,.3,1)"),
        }}
      >
        {value}
      </span>
    </span>
  );
}

export function SplitFlapBoard() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const phase = useCycle(running, STEPS, BEAT, REST);

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg w-full rounded-xl border p-3 font-mono sm:p-4"
    >
      <style>{KEYFRAMES}</style>

      <div className="border-cc-card-border text-cc-ink-dim mb-3 flex items-center justify-between border-b pb-2 text-[10px] tracking-[0.24em]">
        <span>DEPARTURES</span>
        <span className="flex items-center gap-2">
          <span
            className="inline-block h-1.5 w-1.5 rounded-full"
            style={{
              background: AP.taxi,
              animation: anim(
                running,
                "ap-board-live 2000ms ease-in-out infinite",
              ),
            }}
          />
          COMPOSITE SCHEMA
        </span>
      </div>

      <div className="text-cc-ink-dim mb-1 grid grid-cols-[3.2rem_1fr_5.4rem] gap-2 text-[10px] tracking-[0.18em] sm:grid-cols-[3.6rem_1fr_6rem_8.5rem]">
        <span>DEP</span>
        <span>SOURCE SCHEMA</span>
        <span className="hidden sm:block">SPEC</span>
        <span>STATUS</span>
      </div>

      <div className="space-y-1">
        {GATES.map((gate, i) => {
          const step = Math.min(2, Math.max(0, phase - i));
          const status = STATUS[step];
          const boarded = step === 2;

          return (
            <div
              key={gate.name}
              className="grid grid-cols-[3.2rem_1fr_5.4rem] items-center gap-2 rounded-md px-1.5 py-1.5 text-[11px] sm:grid-cols-[3.6rem_1fr_6rem_8.5rem] sm:text-xs"
              style={{ background: AP.wash }}
            >
              <Flap value={DEPARTS[i]} running={running} color={AP.dim} />
              <span className="flex min-w-0 items-baseline gap-2">
                <span className="truncate" style={{ color: AP.amber }}>
                  {gate.name}
                </span>
                <span className="text-cc-ink-dim text-[10px]">
                  {gate.stand} · {gate.language}
                </span>
              </span>
              <Flap
                value={specMark(gate.spec)}
                running={running}
                className="hidden text-[10px] sm:block"
                color={AP.ink}
              />
              <Flap
                value={status}
                running={running}
                className="text-[10px] sm:text-[11px]"
                color={boarded ? AP.taxi : AP.amber}
              />
            </div>
          );
        })}

        <div
          className="grid grid-cols-[3.2rem_1fr_5.4rem] items-center gap-2 rounded-md px-1.5 py-1.5 text-[11px] sm:grid-cols-[3.6rem_1fr_6rem_8.5rem] sm:text-xs"
          style={{ background: AP.wash }}
        >
          <span className="text-cc-ink-dim text-[10px]">GND</span>
          <span className="text-cc-heading truncate text-[11px]">
            Payments · Inventory
          </span>
          <span className="text-cc-ink-dim hidden text-[10px] sm:block">
            OPENAPI · GRPC
          </span>
          <span className="text-[10px]" style={{ color: AP.taxi }}>
            SAME BOARD
          </span>
        </div>
      </div>
    </div>
  );
}
