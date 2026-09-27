"use client";

import type { CSSProperties, ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import {
  animate,
  motion,
  useInView,
  useMotionValue,
  useTransform,
  type MotionValue,
} from "motion/react";

import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { CountUp, NitroTheme, token } from "@/src/nitro";
import { ChartCanvas } from "@/src/nitro/primitives/ChartCanvas";
import { Legend } from "@/src/nitro/primitives/Legend";
import { ease, useReducedMotionPreference } from "@/src/nitro/lib/motion";
import {
  areaFromLine,
  linScale,
  niceTicks,
  smoothLinePath,
  type Pt,
} from "@/src/nitro/lib/scale";

import { useElementMotion } from "../hooks";
import { Tachometer } from "./Tachometer";

const GAUGE_MAX = 7000;
const RED_ZONE_START = 6000;
const SETTLE_VALUE = 5500;
const IDLE_BAND: readonly [number, number] = [5300, 5650];
const CORES = 8;

const STEP_MS = 900;

function nextLatencyP50(t: number): number {
  return (
    Math.round(
      (7 + 0.8 * Math.sin(t * 0.5 + 1) + 0.3 * Math.sin(t * 2.1)) * 10,
    ) / 10
  );
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

const SEED_LEN = 12;
const LATENCY_P50: readonly number[] = Array.from(
  { length: SEED_LEN },
  (_, i) => nextLatencyP50(i),
);
const LATENCY_P95: readonly number[] = Array.from(
  { length: SEED_LEN },
  (_, i) => nextLatencyP95(i),
);
const LATENCY_DOMAIN: readonly [number, number] = [0, 20];

const THROUGHPUT_BARS: readonly number[] = Array.from(
  { length: SEED_LEN },
  (_, i) => nextThroughput(i),
);

const CHART_W = 600;
const CHART_H = 88;
const CHART_PAD = { top: 8, right: 4, bottom: 8, left: 4 };
const BAR_H = 200;

interface LiveCharts {
  readonly p50: readonly number[];
  readonly p95: readonly number[];
  readonly bars: readonly number[];
  readonly previewP50: number;
  readonly previewP95: number;
  readonly previewBar: number;
  readonly scrollT: MotionValue<number>;
}

function useLiveCharts(active: boolean): LiveCharts {
  const [p50, setP50] = useState<number[]>(() => [...LATENCY_P50]);
  const [p95, setP95] = useState<number[]>(() => [...LATENCY_P95]);
  const [bars, setBars] = useState<number[]>(() => [...THROUGHPUT_BARS]);
  const [preview, setPreview] = useState(() => ({
    p50: nextLatencyP50(SEED_LEN),
    p95: nextLatencyP95(SEED_LEN),
    bar: nextThroughput(SEED_LEN),
  }));
  const counter = useRef(SEED_LEN - 1);
  const scrollT = useMotionValue(0);

  useEffect(() => {
    if (!active) return;
    let cancelled = false;

    const step = () => {
      animate(scrollT, 1, {
        duration: STEP_MS / 1000,
        ease: ease.linear,
      }).then(() => {
        if (cancelled) return;
        const t = counter.current + 1;
        counter.current = t;
        setP50((prev) => [...prev.slice(1), nextLatencyP50(t)]);
        setP95((prev) => [...prev.slice(1), nextLatencyP95(t)]);
        setBars((prev) => [...prev.slice(1), nextThroughput(t)]);
        setPreview({
          p50: nextLatencyP50(t + 1),
          p95: nextLatencyP95(t + 1),
          bar: nextThroughput(t + 1),
        });
        scrollT.set(0);
        step();
      });
    };
    step();

    return () => {
      cancelled = true;
    };
  }, [active, scrollT]);

  return {
    p50,
    p95,
    bars,
    previewP50: preview.p50,
    previewP95: preview.p95,
    previewBar: preview.bar,
    scrollT,
  };
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

interface ScrollLineSeries {
  readonly values: readonly number[];
  readonly preview: number;
  readonly stroke: string;
  readonly fillOpacity: number;
}

interface ScrollingLatencyChartProps {
  readonly series: readonly ScrollLineSeries[];
  readonly domain: readonly [number, number];
  readonly scrollT: MotionValue<number>;
  readonly reveal: MotionValue<number>;
}

function ScrollingLatencyChart({
  series,
  domain,
  scrollT,
  reveal,
}: ScrollingLatencyChartProps) {
  const n = series[0].values.length;
  const plotLeft = CHART_PAD.left;
  const plotRight = CHART_W - CHART_PAD.right;
  const plotTop = CHART_PAD.top;
  const plotBottom = CHART_H - CHART_PAD.bottom;
  const plotW = plotRight - plotLeft;
  const slot = plotW / (n - 1);
  const yScale = linScale(domain[0], domain[1], plotBottom, plotTop);
  const ticks = niceTicks(domain[0], domain[1], 4);
  const translateX = useTransform(scrollT, (t) => -t * slot);
  const opacity = useTransform(reveal, [0, 1], [0, 1], {
    ease: ease.out,
    clamp: true,
  });

  return (
    <ChartCanvas label={`Latency, ${series.length} series scrolling over time`}>
      <motion.svg
        viewBox={`0 0 ${CHART_W} ${CHART_H}`}
        preserveAspectRatio="none"
        width="100%"
        height="100%"
        style={{ display: "block", overflow: "hidden", opacity }}
      >
        {ticks.map((v) => {
          const y = yScale(v);
          return (
            <line
              key={v}
              x1={plotLeft}
              x2={plotRight}
              y1={y}
              y2={y}
              stroke={token.grid}
              strokeWidth={1}
              vectorEffect="non-scaling-stroke"
            />
          );
        })}
        <motion.g style={{ x: translateX }}>
          {series.map((s, i) => {
            const pts: Pt[] = [...s.values, s.preview].map((v, j) => [
              plotLeft + j * slot,
              yScale(v),
            ]);
            const lineD = smoothLinePath(pts);
            const areaD = areaFromLine(lineD, pts, plotBottom);
            return (
              <g key={i}>
                <path d={areaD} fill={s.stroke} opacity={s.fillOpacity} />
                <path
                  d={lineD}
                  fill="none"
                  stroke={s.stroke}
                  strokeWidth={1.2}
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  vectorEffect="non-scaling-stroke"
                />
              </g>
            );
          })}
        </motion.g>
      </motion.svg>
    </ChartCanvas>
  );
}

interface ScrollingBarChartProps {
  readonly values: readonly number[];
  readonly preview: number;
  readonly domain: readonly [number, number];
  readonly color: string;
  readonly scrollT: MotionValue<number>;
  readonly reveal: MotionValue<number>;
}

function ScrollingBarChart({
  values,
  preview,
  domain,
  color,
  scrollT,
  reveal,
}: ScrollingBarChartProps) {
  const n = values.length;
  const gap = 2;
  const barRadius = 1;
  const slot = CHART_W / n;
  const barW = Math.max(0.5, slot - gap);
  const yScale = linScale(domain[0], domain[1], BAR_H, 0);
  const translateX = useTransform(scrollT, (t) => -t * slot);
  const scaleY = useTransform(reveal, [0, 1], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const all = [...values, preview];

  return (
    <ChartCanvas label={`Sustained throughput, ${n} bars scrolling over time`}>
      <svg
        viewBox={`0 0 ${CHART_W} ${BAR_H}`}
        preserveAspectRatio="none"
        width="100%"
        height="100%"
        style={{ display: "block", overflow: "hidden" }}
      >
        <motion.g style={{ x: translateX }}>
          {all.map((v, i) => {
            const x = i * slot + (slot - barW) / 2;
            const yTop = yScale(v);
            const barH = Math.max(0, BAR_H - yTop);
            return (
              <motion.rect
                key={i}
                x={x}
                y={yTop}
                width={barW}
                height={barH}
                rx={barRadius}
                ry={barRadius}
                vectorEffect="non-scaling-stroke"
                style={{
                  fill: color,
                  transformBox: "fill-box",
                  transformOrigin: "bottom",
                  scaleY,
                }}
              />
            );
          })}
        </motion.g>
      </svg>
    </ChartCanvas>
  );
}

interface RevealCardProps {
  readonly progress: MotionValue<number>;
  readonly live: LiveCharts;
}

function LatencyCard({ progress, live }: RevealCardProps) {
  const p95Latest = live.p95[live.p95.length - 1];

  return (
    <div className="flex min-h-0 flex-1 flex-col">
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
              <ScrollingLatencyChart
                series={[
                  {
                    values: live.p50,
                    preview: live.previewP50,
                    stroke: token.cLatency,
                    fillOpacity: 0.24,
                  },
                  {
                    values: live.p95,
                    preview: live.previewP95,
                    stroke: token.cP95,
                    fillOpacity: 0.18,
                  },
                ]}
                domain={LATENCY_DOMAIN}
                scrollT={live.scrollT}
                reveal={progress}
              />
            </NitroCanvas>
          </div>
        </div>
      </Card>
    </div>
  );
}

function ThroughputCard({ progress, live }: RevealCardProps) {
  return (
    <div className="flex min-h-0 flex-1 flex-col">
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
              <ScrollingBarChart
                values={live.bars}
                preview={live.previewBar}
                domain={[0, GAUGE_MAX]}
                color={token.cThroughput}
                scrollT={live.scrollT}
                reveal={progress}
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
  const active = useElementMotion(ref);
  const live = useLiveCharts(active);

  return (
    <div ref={ref} className="@container">
      <div className="grid grid-cols-1 gap-3 @min-[480px]:grid-cols-5">
        <div className="@min-[480px]:col-span-2">
          <GaugeCard />
        </div>
        <div className="flex flex-col gap-3 @min-[480px]:col-span-3">
          <LatencyCard progress={progress} live={live} />
          <ThroughputCard progress={progress} live={live} />
        </div>
      </div>
    </div>
  );
}
