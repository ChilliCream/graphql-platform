"use client";

import type { CSSProperties, ReactNode } from "react";
import { useEffect, useRef } from "react";
import {
  animate,
  useInView,
  useMotionValue,
  useReducedMotion,
  type MotionValue,
} from "motion/react";

import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import {
  BarSeries,
  CountUp,
  LineAreaChart,
  NitroTheme,
  token,
} from "@/src/nitro";

import { Tachometer } from "./Tachometer";

const GAUGE_MAX = 20000;
const RED_ZONE_START = 19000;
const SETTLE_VALUE = 18400;
const IDLE_BAND: readonly [number, number] = [18100, 18500];

const LATENCY_P50 = [11, 10, 12, 11, 10, 11, 12, 10, 11, 12, 11, 10];
const LATENCY_P95 = [41, 43, 40, 42, 44, 41, 43, 42, 40, 43, 42, 41];
const THROUGHPUT_BARS = [
  18200, 18400, 18100, 18600, 18300, 18500, 18200, 18700, 18400, 18300, 18600,
  18200,
];

function useBentoReveal() {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotion() ?? false;
  const inView = useInView(ref, { amount: 0.25 });
  const progress = useMotionValue(reduced ? 1 : 0);

  useEffect(() => {
    if (reduced) {
      progress.set(1);
      return;
    }
    if (!inView) return;
    const controls = animate(progress, 1, { duration: 1.6, ease: "linear" });
    return () => controls.stop();
  }, [reduced, inView, progress]);

  return { ref, progress };
}

interface CardHeaderProps {
  readonly title: string;
  readonly hint?: ReactNode;
}

function CardHeader({ title, hint }: CardHeaderProps) {
  return (
    <div className="flex items-baseline justify-between gap-2 px-4 pt-4">
      <Eyebrow as="h3" color="ink-dim" className="min-w-0 truncate">
        {title}
      </Eyebrow>
      {hint && (
        <Eyebrow
          as="span"
          color="ink-dim"
          className="min-w-0 shrink-0 truncate"
        >
          {hint}
        </Eyebrow>
      )}
    </div>
  );
}

interface NitroCanvasProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: CSSProperties;
}

/** Wraps a chart primitive in the Nitro dark palette without painting a background of its own. */
function NitroCanvas({ children, className, style }: NitroCanvasProps) {
  return (
    <NitroTheme
      theme="dark"
      className={className}
      style={{ background: "transparent", ...style }}
    >
      {children}
    </NitroTheme>
  );
}

function GaugeCard() {
  return (
    <Card className="h-full" glow>
      <div className="relative z-10 flex h-full flex-col">
        <CardHeader title="Throughput" />
        <div className="flex flex-1 items-center justify-center px-4 pt-2 pb-4">
          <NitroCanvas>
            <Tachometer
              max={GAUGE_MAX}
              redZoneStart={RED_ZONE_START}
              settleValue={SETTLE_VALUE}
              idleBand={IDLE_BAND}
              unit="ops / min"
            />
          </NitroCanvas>
        </div>
      </div>
    </Card>
  );
}

interface RevealCardProps {
  readonly progress: MotionValue<number>;
}

function LatencyCard({ progress }: RevealCardProps) {
  return (
    <Card className="h-full">
      <div className="relative z-10 flex h-full flex-col">
        <CardHeader title="Latency" hint="p50 · p95" />
        <div className="px-4 pt-2 pb-4">
          <NitroCanvas>
            <LineAreaChart
              series={[
                {
                  values: LATENCY_P50,
                  stroke: token.cP95,
                  fill: true,
                  fillGradient: true,
                  fillOpacity: 0.24,
                  strokeWidth: 1.2,
                },
                {
                  values: LATENCY_P95,
                  stroke: token.cP99,
                  fill: true,
                  fillGradient: true,
                  fillOpacity: 0.18,
                  strokeWidth: 1.2,
                },
              ]}
              domain={[0, 60]}
              height={88}
              grid
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
        </div>
      </div>
    </Card>
  );
}

function ThroughputCard({ progress }: RevealCardProps) {
  return (
    <Card className="h-full">
      <div className="relative z-10 flex h-full flex-col">
        <CardHeader title="Sustained" hint="ops / min" />
        <div className="flex flex-1 flex-col justify-between px-4 pt-2 pb-4">
          <NitroCanvas className="h-9">
            <CountUp
              value={SETTLE_VALUE}
              format={(n) => Math.round(n).toLocaleString("en-US")}
              style={{ justifyContent: "flex-start" }}
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
          <NitroCanvas className="mt-2 h-12">
            <BarSeries
              values={THROUGHPUT_BARS}
              domain={[0, GAUGE_MAX]}
              color={token.cThroughput}
              progress={progress}
              playWindow={[0, 1]}
            />
          </NitroCanvas>
        </div>
      </div>
    </Card>
  );
}

export function PerformanceBento() {
  const { ref, progress } = useBentoReveal();

  return (
    <div ref={ref} className="@container p-3 sm:p-4">
      <div className="grid grid-cols-1 gap-3 @min-[480px]:grid-cols-5">
        <div className="@min-[480px]:col-span-2">
          <GaugeCard />
        </div>
        <div className="flex flex-col gap-3 @min-[480px]:col-span-3">
          <LatencyCard progress={progress} />
          <ThroughputCard progress={progress} />
        </div>
      </div>
    </div>
  );
}
