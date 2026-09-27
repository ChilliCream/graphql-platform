import { token } from "@/src/nitro";

import { CORES, FETCHES_AFTER, FETCHES_BEFORE, PLAN_CACHE_HIT } from "./data";

interface Readout {
  readonly label: string;
  readonly value: string;
}

const READOUTS: readonly Readout[] = [
  { label: "cores", value: `${CORES}` },
  { label: "plan cache", value: `${PLAN_CACHE_HIT}%` },
  { label: "fetches", value: `${FETCHES_BEFORE} → ${FETCHES_AFTER}` },
];

export function SideReadouts() {
  return (
    <div
      className="flex flex-wrap items-center justify-center gap-x-7 gap-y-2"
      role="group"
      aria-label="Performance readouts"
    >
      {READOUTS.map((r) => (
        <div key={r.label} className="flex flex-col items-center">
          <span
            className="text-[11px] tracking-[0.08em] whitespace-nowrap uppercase"
            style={{ color: token.textSecondary, fontFamily: token.mono }}
          >
            {r.label}
          </span>
          <span
            className="text-[15px] whitespace-nowrap"
            style={{
              color: token.textStrong,
              fontFamily: token.mono,
              fontWeight: 800,
              fontStyle: "italic",
              fontVariantNumeric: "tabular-nums",
            }}
          >
            {r.value}
          </span>
        </div>
      ))}
    </div>
  );
}
