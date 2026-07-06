/**
 * LineAreaChart — animated multi-series line + area.
 *
 * Used for latency (mean/p95/p99), throughput (opm), and errors. Each series' line
 * draws left→right via `pathLength`; its area wipes in under a synced clip. Drawing is
 * driven by a normalized clock `t` (0→1) from `useChartClock`, so the chart animates
 * standalone in its story and also slots into the Monitoring Overview's shared cycle.
 *
 * Responsive contract (shared by every chart primitive):
 *   - inline SVG, `viewBox="0 0 W H"`, `preserveAspectRatio="none"`, width/height 100%
 *   - strokes get `vectorEffect="non-scaling-stroke"` so they stay crisp when stretched
 *   - text/labels live in the DOM (here: none baked into the SVG), not in pixels
 */
import { useEffect, useId, useState } from "react";
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import {
  areaFromLine,
  linScale,
  logScale,
  smoothLinePath,
  linePath,
  niceTicks,
  type Pt,
} from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export interface LineSeries {
  values: number[];
  /** stroke color — a `token.*` var string or hex */
  stroke: string;
  /** draw a filled area down to the baseline */
  fill?: boolean;
  /** area fill color (defaults to `stroke`) */
  fillColor?: string;
  fillOpacity?: number;
  /**
   * Render the area as a top-down gradient (color at `fillOpacity` fading to
   * transparent at the baseline) instead of a flat fill, matching the console
   * chart language. Off by default so existing flat fills stay unchanged.
   */
  fillGradient?: boolean;
  strokeWidth?: number;
  smooth?: boolean;
  dash?: string;
}

interface Insets {
  top: number;
  right: number;
  bottom: number;
  left: number;
}

export interface LineAreaChartProps {
  series: LineSeries[];
  width?: number;
  height?: number;
  padding?: Partial<Insets>;
  /** y-domain; computed from data (with headroom) when omitted */
  domain?: [number, number];
  log?: boolean;
  grid?: boolean;
  gridCount?: number;
  /** shared master clock (overview); omit for a self-contained standalone loop */
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  /** fraction of the local window each successive series is offset by */
  seriesStagger?: number;
  /** pulsing dot at the most recent point of the first series */
  showHead?: boolean;
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
  ariaLabel?: string;
}

const DEFAULT_PAD: Insets = { top: 8, right: 4, bottom: 8, left: 4 };

export function LineAreaChart({
  series,
  width = 600,
  height = 200,
  padding,
  domain,
  log = false,
  grid = true,
  gridCount = 4,
  progress,
  playWindow,
  seriesStagger = 0.12,
  showHead = false,
  durationMs,
  className,
  style,
  ariaLabel,
}: LineAreaChartProps) {
  const { ref, t, reduced, inView } = useChartClock({
    progress,
    playWindow,
    durationMs,
  });
  // Only let the head dot pulse when it would actually be seen and motion is allowed.
  const pulse = !reduced && inView;
  const label = ariaLabel ?? `Line chart with ${series.length} series`;
  const pad: Insets = { ...DEFAULT_PAD, ...padding };
  const plotLeft = pad.left;
  const plotRight = width - pad.right;
  const plotTop = pad.top;
  const plotBottom = height - pad.bottom;
  const plotW = plotRight - plotLeft;

  const allValues = series.flatMap((s) => s.values);
  const dMin = domain ? domain[0] : Math.min(...allValues);
  const dMaxRaw = domain ? domain[1] : Math.max(...allValues);
  // headroom so the peak isn't glued to the top
  const dMax = domain
    ? dMaxRaw
    : dMaxRaw + (dMaxRaw - dMin) * 0.12 || dMaxRaw + 1;
  const lo = log ? Math.max(dMin, 1) : dMin;
  const yScale = log
    ? logScale(lo, dMax, plotBottom, plotTop)
    : linScale(dMin, dMax, plotBottom, plotTop);

  const xOf = (i: number, n: number) =>
    n <= 1 ? plotLeft : plotLeft + (i / (n - 1)) * plotW;

  const ticks = grid ? niceTicks(dMin, dMax, gridCount) : [];

  const span = Math.max(0.001, 1 - (series.length - 1) * seriesStagger);

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
        {grid &&
          ticks.map((v) => {
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

        {series.map((s, i) => {
          const pts: Pt[] = s.values.map((v, j) => [
            xOf(j, s.values.length),
            yScale(v),
          ]);
          const s0 = Math.min(i * seriesStagger, 0.99);
          const s1 = Math.min(s0 + span, 1);
          return (
            <SeriesPath
              key={i}
              pts={pts}
              baselineY={plotBottom}
              plotLeft={plotLeft}
              plotW={plotW}
              series={s}
              t={t}
              draw={[s0, s1]}
              showHead={showHead && i === 0}
              pulse={pulse}
            />
          );
        })}
      </svg>
    </div>
  );
}

function SeriesPath({
  pts,
  baselineY,
  plotLeft,
  plotW,
  series,
  t,
  draw,
  showHead,
  pulse,
}: {
  pts: Pt[];
  baselineY: number;
  plotLeft: number;
  plotW: number;
  series: LineSeries;
  t: MotionValue<number>;
  draw: [number, number];
  showHead: boolean;
  /** animate the head halo (off under reduced motion / off-screen → static dot only) */
  pulse: boolean;
}) {
  const uid = useId().replace(/:/g, "");
  const clipId = `wipe-${uid}`;
  const gradId = `fill-${uid}`;
  const lineD = (series.smooth ?? true) ? smoothLinePath(pts) : linePath(pts);
  const areaD = areaFromLine(lineD, pts, baselineY);
  const fillColor = series.fillColor ?? series.stroke;
  const gradient = series.fillGradient ?? false;
  const progress = useTransform(t, [draw[0], draw[1]], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const wipeW = useTransform(progress, [0, 1], [0, plotW]);
  const fillOpacity = useTransform(
    progress,
    [0, 0.25, 1],
    [0, 0, series.fillOpacity ?? 0.18],
  );
  // Play-once head dot: ramp in as the line nears the end, then fade back out
  // right at the finish so it does not sit pulsing forever after the draw settles.
  const headOpacity = useTransform(progress, [0.85, 0.95, 1], [0, 1, 0]);
  // Motion draws the line by animating `pathLength`, which it implements with a
  // `pathLength="1"` + `stroke-dasharray="1 1"` pair. On longer, wigglier paths the
  // dash rounding clips the final segment once the draw settles, leaving a
  // stroke-less area wedge at the right edge. Once fully drawn, drop `pathLength`
  // and render the whole stroke solid so it caps the entire area. The reveal is
  // unchanged while drawing.
  const [drawn, setDrawn] = useState(() => progress.get() >= 0.99);
  useEffect(() => {
    return progress.on("change", (p) => setDrawn(p >= 0.99));
  }, [progress]);
  const last = pts[pts.length - 1];

  return (
    <g>
      {series.fill && (
        <>
          <clipPath id={clipId}>
            <motion.rect
              x={plotLeft}
              y={0}
              height={baselineY}
              style={{ width: wipeW }}
            />
          </clipPath>
          {gradient && (
            <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={fillColor} stopOpacity={1} />
              <stop offset="100%" stopColor={fillColor} stopOpacity={0} />
            </linearGradient>
          )}
          <motion.path
            d={areaD}
            clipPath={`url(#${clipId})`}
            style={{
              fill: gradient ? `url(#${gradId})` : fillColor,
              opacity: fillOpacity,
            }}
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
                stroke: series.stroke,
                strokeWidth: series.strokeWidth ?? 2,
                // Clear the dash Motion leaves from the pathLength draw so the
                // stroke renders solid end to end (style overrides the attribute).
                strokeDasharray: series.dash ?? "none",
                strokeDashoffset: 0,
              }
            : {
                stroke: series.stroke,
                strokeWidth: series.strokeWidth ?? 2,
                pathLength: progress,
              }
        }
      />
      {showHead && last && (
        <motion.g style={{ opacity: headOpacity }}>
          {/* Pulsing halo: a free-running loop, so render it ONLY when motion is allowed
              and the chart is on screen — otherwise it would keep flickering its opacity
              under reduced motion and never idle off-screen. The static dot always shows. */}
          {pulse && (
            <motion.circle
              cx={last[0]}
              cy={last[1]}
              r={5}
              style={{ fill: series.stroke }}
              animate={{ scale: [1, 2.4, 1], opacity: [0.5, 0, 0.5] }}
              transition={{
                repeat: Infinity,
                duration: 2.2,
                ease: "easeInOut",
              }}
            />
          )}
          <circle
            cx={last[0]}
            cy={last[1]}
            r={2.6}
            style={{ fill: series.stroke }}
          />
        </motion.g>
      )}
    </g>
  );
}
