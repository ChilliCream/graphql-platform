"use client";

import type { CSSProperties, ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import {
  animate,
  useInView,
  useMotionValue,
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
import { Legend } from "@/src/nitro/primitives/Legend";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { Tachometer } from "./Tachometer";

const GAUGE_MAX = 7000;
const RED_ZONE_START = 6000;
const SETTLE_VALUE = 5500;
const IDLE_BAND: readonly [number, number] = [5300, 5650];
const CORES = 8;

const LATENCY_P50: readonly number[] = [7, 8, 7, 8, 7, 8, 7, 8, 7, 8, 7, 8];
const LATENCY_P95: readonly number[] = [
  12, 13, 11, 12, 13, 12, 11, 13, 12, 11, 13, 12,
];
const LATENCY_DOMAIN: [number, number] = [0, 20];

const THROUGHPUT_BARS: readonly number[] = [
  5400, 5500, 5350, 5600, 5450, 5550, 5400, 5650, 5500, 5400, 5600, 5450,
];

const STEP_MS = 900;
const REFRESH_MS = 80;

function nextLatencyP50(t: number): number {
  return Math.round(7 + 0.8 * Math.sin(t * 0.5 + 1) + 0.3 * Math.sin(t * 2.1));
}

function nextLatencyP95(t: number): number {
  return Math.round(
    12 + 1.2 * Math.sin(t * 0.7) + 0.4 * Math.sin(t * 2.3 + 0.6),
  );
}

function nextThroughput(t: number): number {
  return Math.round(
    5500 + 220 * Math.sin(t * 0.6 + 0.5) + 80 * Math.sin(t * 1.9),
  );
}

function lerpSeries(
  from: readonly number[],
  to: readonly number[],
  t: number,
): number[] {
  return from.map((v, i) => v + (to[i] - v) * t);
}

// Blends toward the next keyframe continuously instead of snapping on an interval.
function useLiveSeries(
  initial: readonly number[],
  next: (t: number) => number,
  active: boolean,
): number[] {
  const [series, setSeries] = useState<number[]>(() => [...initial]);
  const counter = useRef(initial.length);
  const from = useRef<number[]>([...initial]);
  const to = useRef<number[]>([...initial]);
  const stepStart = useRef(0);
  const lastPaint = useRef(0);

  useEffect(() => {
    if (!active) return;
    let frameId: number;
    stepStart.current = performance.now();

    const step = (now: number) => {
      const t = Math.min(1, (now - stepStart.current) / STEP_MS);
      if (now - lastPaint.current >= REFRESH_MS || t >= 1) {
        lastPaint.current = now;
        setSeries(lerpSeries(from.current, to.current, t));
      }
      if (t >= 1) {
        counter.current += 1;
        const value = next(counter.current);
        from.current = to.current;
        to.current = [...from.current.slice(1), value];
        stepStart.current = now;
      }
      frameId = requestAnimationFrame(step);
    };

    frameId = requestAnimationFrame(step);
    return () => cancelAnimationFrame(frameId);
  }, [active, next]);

  return series;
}

function useBentoReveal() {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotionPreference();
  const inView = useInView(ref, { amount: 0.25 });
  const progress = useMotionValue(0);

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
          <NitroCanvas className="w-full">
            <Tachometer
              max={GAUGE_MAX}
              redZoneStart={RED_ZONE_START}
              settleValue={SETTLE_VALUE}
              idleBand={IDLE_BAND}
              unit="ops/s"
              cores={CORES}
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
  const ref = useRef<HTMLDivElement>(null);
  const active = useElementMotion(ref);
  const p50 = useLiveSeries(LATENCY_P50, nextLatencyP50, active);
  const p95 = useLiveSeries(LATENCY_P95, nextLatencyP95, active);
  const p95Latest = p95[p95.length - 1];

  return (
    <div ref={ref} className="flex min-h-0 flex-1 flex-col">
      <Card className="flex h-full min-h-0 flex-1 flex-col">
        <div className="relative z-10 flex h-full min-h-0 flex-col">
          <CardHeader
            title="Latency"
            hint={`p95 ${Math.round(p95Latest)} ms`}
          />
          <NitroCanvas className="flex items-center gap-2 px-4">
            <Legend
              items={[
                { label: "p50", color: token.cLatency },
                { label: "p95", color: token.cP95 },
              ]}
            />
          </NitroCanvas>
          <div className="flex min-h-0 flex-1 flex-col px-4 pt-2 pb-4">
            <NitroCanvas className="min-h-0 flex-1">
              <LineAreaChart
                series={[
                  {
                    values: p50,
                    stroke: token.cLatency,
                    fill: true,
                    fillGradient: true,
                    fillOpacity: 0.24,
                    strokeWidth: 1.2,
                  },
                  {
                    values: p95,
                    stroke: token.cP95,
                    fill: true,
                    fillGradient: true,
                    fillOpacity: 0.18,
                    strokeWidth: 1.2,
                  },
                ]}
                domain={LATENCY_DOMAIN}
                height={88}
                grid
                progress={progress}
                playWindow={[0, 1]}
              />
            </NitroCanvas>
          </div>
        </div>
      </Card>
    </div>
  );
}

function ThroughputCard({ progress }: RevealCardProps) {
  const ref = useRef<HTMLDivElement>(null);
  const active = useElementMotion(ref);
  const bars = useLiveSeries(THROUGHPUT_BARS, nextThroughput, active);

  return (
    <div ref={ref} className="flex min-h-0 flex-1 flex-col">
      <Card className="flex h-full min-h-0 flex-1 flex-col">
        <div className="relative z-10 flex h-full min-h-0 flex-col">
          <CardHeader title="Sustained" hint="ops/s" />
          <div className="flex min-h-0 flex-1 flex-col justify-between px-4 pt-2 pb-4">
            <NitroCanvas className="h-9 shrink-0">
              <CountUp
                value={SETTLE_VALUE}
                format={(n) => `${Math.round(n).toLocaleString("en-US")} ops/s`}
                style={{ justifyContent: "flex-start" }}
                progress={progress}
                playWindow={[0, 1]}
              />
            </NitroCanvas>
            <NitroCanvas className="mt-2 min-h-0 flex-1">
              <BarSeries
                values={bars}
                domain={[0, GAUGE_MAX]}
                color={token.cThroughput}
                progress={progress}
                playWindow={[0, 1]}
              />
            </NitroCanvas>
          </div>
        </div>
      </Card>
    </div>
  );
}

export function PerformanceBento() {
  const { ref, progress } = useBentoReveal();

  return (
    <div ref={ref} className="@container">
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
