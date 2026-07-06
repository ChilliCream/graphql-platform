/**
 * Sparkline — tiny inline line chart for table rows.
 *
 * A label-free, axis-free micro line (optionally with a faint area) sized to drop into a
 * cell of the Insights table. The line draws left→right via `pathLength`, the area fades
 * in behind it — all derived from a normalized clock `t` (0→1) from `useChartClock`, so
 * it animates standalone in its story and slots into the Monitoring Overview's shared
 * cycle when handed a `progress` value.
 *
 * Responsive contract (shared by every chart primitive):
 *   - inline SVG, `viewBox="0 0 W H"`, `preserveAspectRatio="none"`, width/height 100%
 *   - strokes get `vectorEffect="non-scaling-stroke"` so they stay crisp when stretched
 *   - no `<text>` baked into the SVG — this primitive carries no labels at all
 */
import { useEffect, useId, useState } from "react";
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { areaFromLine, smoothLinePath, type Pt } from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export interface SparklineProps {
  values: number[];
  /** line stroke — a `token.*` var string or hex */
  stroke?: string;
  /** draw a faint filled area down to the baseline */
  fill?: boolean;
  /** viewBox width */
  width?: number;
  /** viewBox height */
  height?: number;
  strokeWidth?: number;
  /** shared master clock (overview); omit for a self-contained standalone loop */
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  ariaLabel?: string;
  className?: string;
  style?: CSSProperties;
}

/** Vertical breathing room so peaks/troughs aren't glued to the edges. */
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

  // y-domain straight from the data (flat series → a centered horizontal line).
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

  // Draw the line and fade the area in across the play window.
  const draw = useTransform(t, [0, 1], [0, 1], { ease: ease.out, clamp: true });
  const wipeW = useTransform(draw, [0, 1], [0, width]);
  const fillOpacity = useTransform(draw, [0, 0.25, 1], [0, 0, 0.08]);

  // Motion draws the line by animating `pathLength`, which it backs with a
  // `stroke-dasharray="1 1"` pair. Once the draw settles that dash stays on the
  // element and the rounding clips the final segment, leaving a broken/faint
  // line. Once fully drawn, drop `pathLength` and render the stroke solid.
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
    <div
      ref={ref}
      className={className}
      style={{ position: "relative", width: "100%", height: "100%", ...style }}
      role="img"
      aria-label={label}
    >
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
                  // Clear the dash Motion leaves from the pathLength draw so the
                  // stroke renders solid end to end (style overrides the attribute).
                  strokeDasharray: "none",
                  strokeDashoffset: 0,
                }
              : { stroke, strokeWidth, pathLength: draw }
          }
        />
      </svg>
    </div>
  );
}
