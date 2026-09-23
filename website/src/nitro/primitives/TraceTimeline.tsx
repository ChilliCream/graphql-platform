import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { logScale, linScale, ms } from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import type { TraceSample } from "../lib/data";
import { ChartCanvas } from "./ChartCanvas";

export interface TraceTimelineProps {
  samples: TraceSample[];
  width?: number;
  height?: number;
  threshold?: number;
  showScan?: boolean;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  ariaLabel?: string;
  className?: string;
  style?: CSSProperties;
}

const PAD = { top: 8, right: 8, bottom: 8, left: 8 };
const LOG_TICKS = [10, 100, 1000];

export function TraceTimeline({
  samples,
  width = 600,
  height = 240,
  threshold,
  showScan = true,
  progress,
  playWindow,
  durationMs,
  ariaLabel,
  className,
  style,
}: TraceTimelineProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });

  const plotL = PAD.left;
  const plotR = width - PAD.right;
  const plotT = PAD.top;
  const plotB = height - PAD.bottom;

  const epochs = samples.map((s) => s.epoch);
  const tMin = Math.min(...epochs);
  const tMax = Math.max(...epochs);
  const durs = samples.map((s) => s.durationMs);
  const dMin = Math.max(4, Math.min(...durs));
  const dMax = Math.max(...durs);

  const xOf = linScale(tMin, tMax, plotL, plotR);
  const yOf = logScale(dMin, dMax, plotB, plotT);

  const errors = samples.filter((s) => s.status === "error").length;
  const label =
    ariaLabel ??
    `Trace sample timeline: ${samples.length} sampled requests, ${errors} errored, durations ${ms(dMin)} to ${ms(dMax)}`;

  return (
    <ChartCanvas ref={ref} className={className} style={style} label={label}>
      <svg
        viewBox={`0 0 ${width} ${height}`}
        preserveAspectRatio="none"
        width="100%"
        height="100%"
        style={{ display: "block", overflow: "visible" }}
      >
        {LOG_TICKS.filter((v) => v >= dMin && v <= dMax).map((v) => (
          <line
            key={v}
            x1={plotL}
            x2={plotR}
            y1={yOf(v)}
            y2={yOf(v)}
            stroke={token.grid}
            strokeWidth={1}
            vectorEffect="non-scaling-stroke"
          />
        ))}

        {threshold && threshold >= dMin && threshold <= dMax && (
          <line
            x1={plotL}
            x2={plotR}
            y1={yOf(threshold)}
            y2={yOf(threshold)}
            stroke={token.warning}
            strokeWidth={1}
            strokeDasharray="4 4"
            vectorEffect="non-scaling-stroke"
          />
        )}

        {samples.map((s, i) => (
          <Dot
            key={s.id}
            cx={xOf(s.epoch)}
            cy={yOf(s.durationMs)}
            color={s.status === "error" ? token.cError : token.cSuccess}
            frac={i / Math.max(1, samples.length - 1)}
            t={t}
          />
        ))}

        {showScan && (
          <Scan t={t} plotL={plotL} plotR={plotR} plotT={plotT} plotB={plotB} />
        )}
      </svg>

      {LOG_TICKS.filter((v) => v >= dMin && v <= dMax).map((v) => (
        <span
          key={v}
          style={{
            position: "absolute",
            left: 0,
            top: `${(yOf(v) / height) * 100}%`,
            transform: "translateY(-50%)",
            fontSize: 9,
            color: token.textSecondary,
            background: token.card,
            padding: "0 3px",
            pointerEvents: "none",
          }}
        >
          {ms(v)}
        </span>
      ))}
    </ChartCanvas>
  );
}

function Dot({
  cx,
  cy,
  color,
  frac,
  t,
}: {
  cx: number;
  cy: number;
  color: string;
  frac: number;
  t: MotionValue<number>;
}) {
  const s0 = frac * 0.7;
  const opacity = useTransform(t, [s0, s0 + 0.12], [0, 1], { clamp: true });
  const scale = useTransform(t, [s0, s0 + 0.18], [0.2, 1], {
    ease: ease.pop,
    clamp: true,
  });
  return (
    <motion.circle
      cx={cx}
      cy={cy}
      r={3}
      style={{
        fill: color,
        opacity,
        scale,
        transformBox: "fill-box",
        transformOrigin: "center",
      }}
    />
  );
}

function Scan({
  t,
  plotL,
  plotR,
  plotT,
  plotB,
}: {
  t: MotionValue<number>;
  plotL: number;
  plotR: number;
  plotT: number;
  plotB: number;
}) {
  const tx = useTransform(t, [0.74, 0.96], [plotL, plotR], { clamp: true });
  const opacity = useTransform(t, [0.74, 0.78, 0.92, 0.96], [0, 0.5, 0.5, 0], {
    clamp: true,
  });
  return (
    <motion.line
      x1={0}
      x2={0}
      y1={plotT}
      y2={plotB}
      stroke={token.accent}
      strokeWidth={1}
      vectorEffect="non-scaling-stroke"
      style={{ x: tx, opacity }}
      aria-hidden
    />
  );
}
