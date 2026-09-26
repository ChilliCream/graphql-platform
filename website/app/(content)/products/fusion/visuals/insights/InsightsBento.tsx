"use client";

import { CountUp, HBarSeries, TraceWaterfall } from "@/src/nitro";
import type { Client } from "@/src/nitro/lib/data/types";
import { CheckGlyph } from "@/src/icons/CheckGlyph";

import { BentoCard, ChartFrame } from "./BentoCard";
import {
  COST_BUDGET,
  LATENCY_DRIVER,
  RATE_LIMIT_STATUS,
  REQUEST_COST,
  REQUEST_TRACE,
  USAGE_CLIENTS,
} from "./data";
import { useBentoProgress } from "./useBentoProgress";

const GRID_CLASS =
  "grid grid-cols-1 gap-3 @min-[560px]:grid-cols-6 @min-[560px]:gap-4";

function formatCost(n: number) {
  return `${Math.round(n)} of ${COST_BUDGET.toLocaleString("en-US")}`;
}

/** Hides the trace's built-in axis row, whose tick labels render under 11px; mirrors SignalsBento's slow-span card. */
const HIDE_TRACE_AXIS = `.nitro-insights-trace [role="img"] > div:first-child { display: none; }`;

export function InsightsBento() {
  const { ref, progress } = useBentoProgress();

  return (
    <div ref={ref} className="@container">
      <div className="p-4 @min-[500px]:p-5">
        <div className={GRID_CLASS}>
          <BentoCard
            title="Trace"
            hint="POST /graphql"
            className="@min-[560px]:col-span-6"
          >
            <ChartFrame className="nitro-insights-trace">
              <style>{HIDE_TRACE_AXIS}</style>
              <TraceWaterfall
                trace={REQUEST_TRACE}
                rowHeight={30}
                progress={progress}
                playWindow={[0, 1]}
              />
            </ChartFrame>
            <p className="text-cc-warning mt-3 font-mono text-[0.7rem]">
              {LATENCY_DRIVER}
            </p>
          </BentoCard>

          <BentoCard
            title="Cost"
            hint="Cost Spec"
            className="@min-[560px]:col-span-2"
          >
            <ChartFrame className="h-11">
              <CountUp
                value={REQUEST_COST}
                format={formatCost}
                style={{ justifyContent: "flex-start", fontSize: 26 }}
                progress={progress}
                playWindow={[0, 1]}
              />
            </ChartFrame>
            <div className="text-cc-success mt-3 flex items-center gap-2">
              <CheckGlyph className="h-3.5 w-3.5 shrink-0" />
              <span className="font-mono text-[0.7rem]">
                {RATE_LIMIT_STATUS}
              </span>
            </div>
          </BentoCard>

          <BentoCard
            title="Usage"
            hint="by client"
            className="@min-[560px]:col-span-4"
          >
            <ChartFrame>
              <HBarSeries
                clients={USAGE_CLIENTS as Client[]}
                maxBars={3}
                progress={progress}
                playWindow={[0, 1]}
              />
            </ChartFrame>
            <p className="text-cc-ink-dim mt-3 font-mono text-[0.7rem]">
              usage-based billing, recorded in Nitro
            </p>
          </BentoCard>
        </div>
      </div>
    </div>
  );
}
