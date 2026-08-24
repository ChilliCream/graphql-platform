import dynamic from "next/dynamic";

import { ChapterBand } from "@/src/components/ChapterBand";
import { ChartTile } from "@/src/components/ChartTile";
import { FeatureRow } from "@/src/components/FeatureRow";
import { NitroFrame } from "@/src/components/NitroFrame";
import { StatStrip } from "@/src/components/StatStrip";
import type { Client, InsightRow } from "@/src/nitro/lib/data/types";

import { CORAL, TEAL } from "./palette";

const HBarSeries = dynamic(() => import("@/src/nitro").then((m) => m.HBarSeries));
const InsightsTable = dynamic(() => import("@/src/nitro").then((m) => m.InsightsTable));
const LineAreaChart = dynamic(() => import("@/src/nitro").then((m) => m.LineAreaChart));

const P95_SERIES = [40, 43, 41, 45, 42, 46, 44, 41, 43, 46, 42, 44, 47, 43, 45, 42, 44, 46, 43, 45, 41, 44, 42, 45];
const P99_SERIES = [
  90, 94, 98, 102, 108, 115, 122, 130, 140, 152, 165, 180, 198, 218, 238, 258, 278, 296, 310, 318, 312, 298, 270, 240,
];

const CLIENTS: Client[] = [
  { name: "web-storefront", total: 184000, impact: 94 },
  { name: "mobile-ios", total: 121000, impact: 71 },
  { name: "android", total: 68000, impact: 58 },
];

const IMPACT_INSIGHTS: InsightRow[] = [
  {
    id: "op-checkout",
    spanKind: "server",
    name: "mutation checkout",
    averageLatency: 62,
    opm: 1200,
    errorRate: 0.003,
    impact: 98,
    latencySeries: [42, 48, 55, 61, 58, 62, 60],
    throughputSeries: [980, 1040, 1100, 1150, 1190, 1200, 1220],
  },
  {
    id: "op-billing",
    spanKind: "client",
    name: "gRPC · Billing.Charge",
    averageLatency: 204,
    opm: 610,
    errorRate: 0.004,
    impact: 71,
    latencySeries: [180, 190, 198, 204, 201, 204, 204],
    throughputSeries: [560, 580, 600, 610, 605, 612, 610],
  },
  {
    id: "op-orders",
    spanKind: "server",
    name: "REST · POST /orders",
    averageLatency: 31,
    opm: 1400,
    errorRate: 0.0,
    impact: 54,
    latencySeries: [28, 30, 29, 31, 30, 31, 31],
    throughputSeries: [1300, 1340, 1360, 1380, 1390, 1400, 1410],
  },
  {
    id: "op-coupon",
    spanKind: "server",
    name: "mutation applyCoupon",
    averageLatency: 16,
    opm: 340,
    errorRate: 0.014,
    impact: 38,
    latencySeries: [14, 15, 16, 17, 15, 16, 16],
    throughputSeries: [300, 310, 320, 330, 335, 338, 340],
  },
  {
    id: "op-receipt",
    spanKind: "consumer",
    name: "job · receipt.worker",
    averageLatency: 58,
    opm: 240,
    errorRate: 0.002,
    impact: 33,
    latencySeries: [52, 54, 56, 58, 57, 58, 58],
    throughputSeries: [210, 220, 228, 235, 238, 240, 240],
  },
];

export function ThreeQuestions() {
  return (
    <>
      <ChapterBand
        className="mt-12 sm:mt-16"
        title={
          <>
            What&rsquo;s slow. How bad. <br className="hidden sm:block" />
            And for whom.
          </>
        }
      />

      <section className="py-12 sm:py-16">
        <div className="flex flex-col gap-16 sm:gap-20">
          <FeatureRow
            title="Rank operations to investigate with the impact score."
            body={
              <>
                The impact score combines traffic, latency, and error rate to help you decide which reported operations
                to investigate first.
              </>
            }
            visual={
              <ChartTile title="Operations" hint="ranked by impact · last 1h" glow>
                <NitroFrame>
                  <InsightsTable
                    once
                    rows={IMPACT_INSIGHTS}
                    nameHeader="Operation"
                    errorThreshold={0.01}
                    style={{ height: "auto", overflowY: "hidden" }}
                  />
                </NitroFrame>
              </ChartTile>
            }
          />

          <FeatureRow
            reverse
            title="The whole latency picture, not an average."
            body={
              <>
                An average can look healthy while a small number of requests are much slower. Percentiles show you where
                the tail starts, so you can find and fix those slow paths before they affect more users.
              </>
            }
            visual={
              <ChartTile title="Latency · checkout" hint="p95 / p99 · ms" glow>
                <NitroFrame reducedMotion="always">
                  <div className="mb-3 flex items-center gap-4 font-mono text-[0.62rem]">
                    <span className="text-cc-ink-dim flex items-center gap-1.5">
                      <span className="h-1.5 w-3 rounded-full" style={{ background: TEAL }} />
                      p95
                    </span>
                    <span className="text-cc-ink-dim flex items-center gap-1.5">
                      <span className="h-1.5 w-3 rounded-full" style={{ background: CORAL }} />
                      p99
                    </span>
                  </div>
                  <div className="h-44">
                    <LineAreaChart
                      series={[
                        {
                          values: P95_SERIES,
                          stroke: TEAL,
                          fill: true,
                          fillOpacity: 0.12,
                        },
                        {
                          values: P99_SERIES,
                          stroke: CORAL,
                          fill: true,
                          fillOpacity: 0.14,
                        },
                      ]}
                      domain={[0, 340]}
                      grid
                      showHead
                    />
                  </div>
                </NitroFrame>
                <StatStrip
                  className="mt-4"
                  items={[
                    { label: "p99", value: "318ms" },
                    { label: "p95", value: "42ms" },
                    { label: "throughput", value: "1.2k/m" },
                    { label: "error rate", value: "0.31%" },
                  ]}
                />
              </ChartTile>
            }
          />

          <FeatureRow
            title="Know which identified clients are affected."
            body={
              <>
                See which client apps and versions are behind an operation. When one starts causing trouble, you can
                tell whether it affects everyone or only a particular client.
              </>
            }
            visual={
              <ChartTile title="Clients · checkout" hint="share of impact">
                <NitroFrame>
                  <HBarSeries once clients={CLIENTS} maxBars={3} barHeight={14} />
                </NitroFrame>
              </ChartTile>
            }
          />
        </div>
      </section>
    </>
  );
}
