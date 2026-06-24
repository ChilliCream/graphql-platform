/**
 * MonitoringOverview — redrawn by eye from the Nitro monitoring reference (monitoring.svg).
 *
 * Dashboard-only (no app shell): a Latency panel (single teal area), a row of Throughput
 * (blue area) / Clients (impact-gradient bars) / Errors (flat red line), then a Subgraphs
 * table. Prop-less, autoplay, infinite-loop, responsive; one master clock sequences the
 * beats and a whole-frame envelope cross-fades the seam; reduced motion → static frame.
 */
import { useMemo } from "react";
import { motion, useTransform } from "motion/react";
import { DashboardFrame } from "../../primitives/DashboardFrame";
import { ChartPanel } from "../../primitives/ChartPanel";
import { LineAreaChart } from "../../primitives/LineAreaChart";
import { HBarSeries } from "../../primitives/HBarSeries";
import { InsightsTable } from "../../primitives/InsightsTable";
import { token, IMPACT_STOPS } from "../../lib/tokens";
import { niceTicks, compact, ms, colorAt } from "../../lib/scale";
import { makeMonitoringData, type MonitoringData } from "../../lib/data";
import { useMasterClock } from "../../lib/useInViewLoop";

export interface MonitoringOverviewProps {
  seed?: number;
  durationMs?: number;
}

const fmtTime = (epoch: number) => {
  const d = new Date(epoch);
  const h = String(d.getUTCHours()).padStart(2, "0");
  const m = String(d.getUTCMinutes()).padStart(2, "0");
  return `${h}:${m}`;
};
const xTicksFrom = (epochs: number[], n = 6) =>
  Array.from({ length: n }, (_, i) =>
    fmtTime(epochs[Math.round((i / (n - 1)) * (epochs.length - 1))]),
  );

const domainOf = (
  max: number,
  count = 5,
): { domain: [number, number]; ticks: number[] } => {
  const ticks = niceTicks(0, max * 1.08, count);
  const top = ticks[ticks.length - 1];
  return { domain: [0, top], ticks };
};

export function MonitoringOverview({
  seed = 7,
  durationMs = 11000,
}: MonitoringOverviewProps = {}) {
  const data = useMemo<MonitoringData>(() => makeMonitoringData(seed), [seed]);
  const { ref, progress, reduced } = useMasterClock({ durationMs });
  const envelope = useTransform(progress, [0, 0.025, 0.975, 1], [0, 1, 1, 0]);

  const epochs = data.throughput.map((p) => p.epoch);
  const xticks = xTicksFrom(epochs);

  const meanVals = data.latency.map((p) => p.mean);
  const lat = domainOf(Math.max(...meanVals));
  const opmVals = data.throughput.map((p) => p.opm);
  const thr = domainOf(Math.max(...opmVals));
  const errRate = data.throughput.map(
    (p) => p.totalCountWithError / Math.max(1, p.totalCount),
  );

  return (
    <motion.div
      ref={ref}
      style={{ width: "100%", opacity: reduced ? 1 : envelope }}
      role="region"
      aria-label="Operations monitoring overview"
    >
      <DashboardFrame animate={false} minColWidth={300} gap={10} padding={0}>
        {/* Latency (full width) — single teal area */}
        <div style={{ gridColumn: "1 / -1" }}>
          <ChartPanel
            title="Latency"
            legend={[
              { label: "mean", color: token.cLatency },
              { label: "p95", color: token.cP95, shape: "ring", muted: true },
              { label: "p99", color: token.cP99, shape: "ring", muted: true },
            ]}
            height={210}
            yDomain={lat.domain}
            yTicks={lat.ticks}
            yFormat={(n) => (n === 0 ? "0" : ms(n))}
            xTicks={xticks}
            progress={progress}
            playWindow={[0, 0.1]}
          >
            <LineAreaChart
              grid={false}
              domain={lat.domain}
              padding={{ top: 0, right: 0, bottom: 0, left: 0 }}
              ariaLabel="Mean request latency over the last hour"
              progress={progress}
              playWindow={[0.1, 0.3]}
              series={[
                {
                  values: meanVals,
                  stroke: token.cLatency,
                  fill: true,
                  fillOpacity: 0.22,
                },
              ]}
            />
          </ChartPanel>
        </div>

        {/* Throughput — blue area */}
        <ChartPanel
          title="Throughput"
          legend={[
            { label: "OPM", color: token.cThroughput },
            {
              label: "Success",
              color: token.cSuccess,
              shape: "ring",
              muted: true,
            },
            { label: "Error", color: token.cError, shape: "square" },
          ]}
          height={150}
          yDomain={thr.domain}
          yTicks={thr.ticks}
          yFormat={(n) => compact(n)}
          xTicks={xticks}
          progress={progress}
          playWindow={[0.03, 0.13]}
        >
          <LineAreaChart
            grid={false}
            domain={thr.domain}
            padding={{ top: 0, right: 0, bottom: 0, left: 0 }}
            ariaLabel="Throughput, operations per minute"
            progress={progress}
            playWindow={[0.26, 0.46]}
            series={[
              {
                values: opmVals,
                stroke: token.cThroughput,
                fill: true,
                fillOpacity: 0.22,
              },
            ]}
          />
        </ChartPanel>

        {/* Clients — impact-gradient horizontal bars */}
        <ChartPanel
          title="Clients"
          action={<GradientKey />}
          height={150}
          progress={progress}
          playWindow={[0.06, 0.16]}
        >
          <div
            style={{ height: "100%", display: "flex", alignItems: "center" }}
          >
            <HBarSeries
              clients={data.clients}
              barHeight={14}
              ariaLabel="Top clients by traffic and impact"
              progress={progress}
              playWindow={[0.42, 0.6]}
              style={{ width: "100%" }}
            />
          </div>
        </ChartPanel>

        {/* Errors — flat red line on a 0..1 axis */}
        <ChartPanel
          title="Errors"
          legend={[{ label: "GraphQL", color: token.cError }]}
          height={150}
          yDomain={[0, 1]}
          yTicks={[0, 0.2, 0.4, 0.6, 0.8, 1]}
          yFormat={(n) => (n === 0 || n === 1 ? `${n}` : n.toFixed(1))}
          xTicks={xticks}
          progress={progress}
          playWindow={[0.09, 0.19]}
        >
          <LineAreaChart
            grid={false}
            domain={[0, 1]}
            padding={{ top: 0, right: 0, bottom: 0, left: 0 }}
            ariaLabel="GraphQL error rate over time"
            progress={progress}
            playWindow={[0.5, 0.7]}
            series={[
              {
                values: errRate,
                stroke: token.cError,
                fill: false,
                strokeWidth: 1.5,
              },
            ]}
          />
        </ChartPanel>

        {/* Subgraphs (full width) */}
        <div style={{ gridColumn: "1 / -1" }}>
          <ChartPanel
            title="Subgraphs"
            progress={progress}
            playWindow={[0.12, 0.22]}
          >
            <InsightsTable
              rows={data.insights}
              progress={progress}
              playWindow={[0.62, 0.88]}
              rowStagger={0.08}
            />
          </ChartPanel>
        </div>
      </DashboardFrame>
    </motion.div>
  );
}

/** "Low ▬ High" gradient key for the Clients panel. */
function GradientKey() {
  const grad = `linear-gradient(90deg, ${colorAt(IMPACT_STOPS, 0)}, ${colorAt(IMPACT_STOPS, 0.5)}, ${colorAt(IMPACT_STOPS, 1)})`;
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
      <span style={{ fontSize: 10, color: token.textSecondary }}>Low</span>
      <span
        style={{ width: 80, height: 8, borderRadius: 4, background: grad }}
      />
      <span style={{ fontSize: 10, color: token.textSecondary }}>High</span>
    </div>
  );
}
