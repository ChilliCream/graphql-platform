import { useEffect, useId, useState } from "react";
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { areaFromLine, smoothLinePath, type Pt } from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import { ChartCanvas } from "./ChartCanvas";

export interface SparklineProps {
  values: number[];
  stroke?: string;
  fill?: boolean;
  width?: number;
  height?: number;
  strokeWidth?: number;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  ariaLabel?: string;
  className?: string;
  style?: CSSProperties;
}

const PAD_Y = 3;

export function Sparkline({
  values,
  stroke = token.cLatency,
  fill = true,
  width = 96,
  height = 28,
  strokeWidth = 1.5,
  progress,
  playWindow,
  durationMs,
  ariaLabel,
  className,
  style,
}: SparklineProps): React.JSX.Element {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });
  const clipId = useId().replace(/:/g, "");

  const plotTop = PAD_Y;
  const plotBottom = height - PAD_Y;
  const n = values.length;

  const dMin = n ? Math.min(...values) : 0;
  const dMax = n ? Math.max(...values) : 1;
  const yOf = (v: number) =>
    dMax === dMin
      ? (plotTop + plotBottom) / 2
      : plotBottom - ((v - dMin) / (dMax - dMin)) * (plotBottom - plotTop);
  const xOf = (i: number) => (n <= 1 ? 0 : (i / (n - 1)) * width);

  const pts: Pt[] = values.map((v, i) => [xOf(i), yOf(v)]);
  const lineD = smoothLinePath(pts);
  const areaD = areaFromLine(lineD, pts, plotBottom);

  const draw = useTransform(t, [0, 1], [0, 1], { ease: ease.out, clamp: true });
  const wipeW = useTransform(draw, [0, 1], [0, width]);
  const fillOpacity = useTransform(draw, [0, 0.25, 1], [0, 0, 0.08]);

  const [drawn, setDrawn] = useState(() => draw.get() >= 0.99);
  useEffect(() => {
    return draw.on("change", (p) => setDrawn(p >= 0.99));
  }, [draw]);

  const label =
    ariaLabel ??
    (n
      ? `Sparkline trend, ${n} points, from ${Math.round(values[0])} to ${Math.round(
          values[n - 1],
        )}`
      : "Sparkline");

  return (
    <ChartCanvas ref={ref} className={className} style={style} label={label}>
      <svg
        viewBox={`0 0 ${width} ${height}`}
        preserveAspectRatio="none"
        width="100%"
        height="100%"
        style={{ display: "block", overflow: "visible" }}
      >
        {fill && n > 1 && (
          <>
            <clipPath id={`spark-${clipId}`}>
              <motion.rect
                x={0}
                y={0}
                height={height}
                style={{ width: wipeW }}
              />
            </clipPath>
            <motion.path
              d={areaD}
              clipPath={`url(#spark-${clipId})`}
              style={{ fill: stroke, opacity: fillOpacity }}
            />
          </>
        )}
        <motion.path
          d={lineD}
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          vectorEffect="non-scaling-stroke"
          style={
            drawn
              ? {
                  stroke,
                  strokeWidth,
                  strokeDasharray: "none",
                  strokeDashoffset: 0,
                }
              : { stroke, strokeWidth, pathLength: draw }
          }
        />
      </svg>
    </ChartCanvas>
  );
}
