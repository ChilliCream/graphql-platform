/**
 * OperationDetail — redrawn by eye from the Nitro operation reference (operation.svg).
 *
 * Dashboard-only: a row of three mini panels (Throughput / Latency / Errors), the fine
 * Latency Distribution histogram (log axes + Current/p95 markers), and a Trace Sample
 * waterfall. Prop-less, autoplay, infinite-loop, responsive; one master clock sequences
 * the beats with an envelope seam; reduced motion → static final frame.
 */
import { useMemo } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { DashboardFrame } from "../../primitives/DashboardFrame";
import { ChartPanel } from "../../primitives/ChartPanel";
import { LineAreaChart } from "../../primitives/LineAreaChart";
import { DistributionHistogram } from "../../primitives/DistributionHistogram";
import { TraceWaterfall } from "../../primitives/TraceWaterfall";
import { token } from "../../lib/tokens";
import { niceTicks, compact, ms } from "../../lib/scale";
import {
  makeMonitoringData,
  makeLatencyDistribution,
  makeTrace,
} from "../../lib/data";
import { useMasterClock } from "../../lib/useInViewLoop";
import type { LegendItem } from "../../primitives/Legend";

export interface OperationDetailProps {
  seed?: number;
  durationMs?: number;
}

const domainOf = (max: number, count = 4) => {
  const ticks = niceTicks(0, max * 1.08, count);
  return { domain: [0, ticks[ticks.length - 1]] as [number, number], ticks };
};
const pow10Ceil = (n: number) =>
  Math.pow(10, Math.ceil(Math.log10(Math.max(10, n))));

export function OperationDetail({
  seed = 7,
  durationMs = 11000,
}: OperationDetailProps = {}) {
  const data = useMemo(() => makeMonitoringData(seed), [seed]);
  const dist = useMemo(() => makeLatencyDistribution(seed), [seed]);
  const trace = useMemo(() => makeTrace(seed), [seed]);
  const { ref, progress, reduced } = useMasterClock({ durationMs });
  const envelope = useTransform(progress, [0, 0.025, 0.975, 1], [0, 1, 1, 0]);

  const opName = data.insights[1].name; // createOrder (the reel drills into this)
  const meanVals = data.latency.map((p) => p.mean);
  const opmVals = data.throughput.map((p) => p.opm);
  const errRate = data.throughput.map(
    (p) => p.totalCountWithError / Math.max(1, p.totalCount),
  );
  const lat = domainOf(Math.max(...meanVals));
  const thr = domainOf(Math.max(...opmVals));

  const maxCount = Math.max(...dist.bins.map((b) => b.success + b.error));
  const yTop = pow10Ceil(maxCount);
  const distDomain: [number, number] = [1, yTop];
  const distYTicks: number[] = [];
  for (let v = 1; v <= yTop; v *= 10) distYTicks.push(v);
  const distXTicks = ["1ms", "10ms", "100ms", "1s", "10s"];

  return (
    <motion.div
      ref={ref}
      style={{ width: "100%", opacity: reduced ? 1 : envelope }}
      role="region"
      aria-label={`Operation detail for ${opName}`}
    >
      <DashboardFrame animate={false} minColWidth={260} gap={10} padding={0}>
        <div
          style={{
            gridColumn: "1 / -1",
            display: "grid",
            gridTemplateColumns:
              "repeat(auto-fit, minmax(min(100%, 220px), 1fr))",
            gap: 10,
          }}
        >
          <MiniPanel
            title="Throughput"
            legend={[{ label: "OPM", color: token.cThroughput }]}
            values={opmVals}
            color={token.cThroughput}
            domain={thr.domain}
            ticks={thr.ticks}
            fmt={compact}
            progress={progress}
            entrance={[0, 0.1]}
            draw={[0.12, 0.3]}
          />
          <MiniPanel
            title="Latency"
            legend={[{ label: "mean", color: token.cLatency }]}
            values={meanVals}
            color={token.cLatency}
            domain={lat.domain}
            ticks={lat.ticks}
            fmt={(n) => (n === 0 ? "0" : ms(n))}
            progress={progress}
            entrance={[0.03, 0.13]}
            draw={[0.16, 0.34]}
          />
          <MiniPanel
            title="Errors"
            legend={[{ label: "GraphQL", color: token.cError }]}
            values={errRate}
            color={token.cError}
            domain={[0, 1]}
            ticks={[0, 0.5, 1]}
            fmt={(n) => `${n}`}
            fill={false}
            progress={progress}
            entrance={[0.06, 0.16]}
            draw={[0.2, 0.38]}
          />
        </div>

        <div style={{ gridColumn: "1 / -1" }}>
          <ChartPanel
            title="Latency distribution"
            subtitle={`Total operations: ${compact(dist.total)}`}
            legend={[
              { label: "success", color: token.cSuccess, shape: "square" },
              { label: "error", color: token.cError, shape: "square" },
              { label: "p95", color: token.cP95 },
              { label: "current", color: token.cP99 },
            ]}
            height={210}
            yDomain={distDomain}
            yTicks={distYTicks}
            yFormat={compact}
            yLog
            xTicks={distXTicks}
            progress={progress}
            playWindow={[0.1, 0.2]}
          >
            <DistributionHistogram
              distribution={dist}
              yDomain={distDomain}
              ariaLabel={`Latency distribution of ${compact(dist.total)} operations`}
              progress={progress}
              playWindow={[0.3, 0.55]}
            />
          </ChartPanel>
        </div>

        <div style={{ gridColumn: "1 / -1" }}>
          <ChartPanel
            title="Trace sample"
            subtitle={`${ms(trace.totalMs)} · server`}
            progress={progress}
            playWindow={[0.16, 0.26]}
          >
            <TraceWaterfall
              trace={trace}
              ariaLabel="Trace waterfall for a sampled request"
              progress={progress}
              playWindow={[0.55, 0.9]}
            />
          </ChartPanel>
        </div>
      </DashboardFrame>
    </motion.div>
  );
}

function MiniPanel({
  title,
  legend,
  values,
  color,
  domain,
  ticks,
  fmt,
  fill = true,
  progress,
  entrance,
  draw,
}: {
  title: string;
  legend: LegendItem[];
  values: number[];
  color: string;
  domain: [number, number];
  ticks: number[];
  fmt: (n: number) => string;
  fill?: boolean;
  progress: MotionValue<number>;
  entrance: [number, number];
  draw: [number, number];
}) {
  return (
    <ChartPanel
      title={title}
      legend={legend}
      height={96}
      yDomain={domain}
      yTicks={ticks}
      yFormat={fmt}
      yAxisWidth={40}
      progress={progress}
      playWindow={entrance}
    >
      <LineAreaChart
        grid={false}
        domain={domain}
        padding={{ top: 0, right: 0, bottom: 0, left: 0 }}
        ariaLabel={title}
        progress={progress}
        playWindow={draw}
        series={[
          { values, stroke: color, fill, fillOpacity: 0.2, strokeWidth: 1.5 },
        ]}
      />
    </ChartPanel>
  );
}
