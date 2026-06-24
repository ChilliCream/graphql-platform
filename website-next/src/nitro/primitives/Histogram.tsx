/**
 * Histogram — animated latency distribution with a p95 marker (PLAN.md §9).
 *
 * Each bin is a stacked vertical bar: a success rect at the bottom and an error rect
 * stacked above it. Both grow `scaleY` from the baseline, staggered left→right, with
 * heights derived from each bin's total frequency over the busiest bin. After the bars
 * settle, a dashed vertical marker draws/fades in at the x-position of `histogram.p95`
 * (interpolated across the bin edges) with an HTML label chip. Bin-edge labels sit under
 * the bars as DOM text. Everything is driven by a normalized clock `t` (0→1) from
 * `useChartClock`, so the chart animates standalone in its story and slots into the
 * Monitoring Overview's shared cycle.
 *
 * Responsive contract (shared by every chart primitive):
 *   - inline SVG, `viewBox="0 0 W H"`, `preserveAspectRatio="none"`, width/height 100%
 *   - strokes get `vectorEffect="non-scaling-stroke"` so they stay crisp when stretched
 *   - bars grow via `scaleY` with `transformBox:'fill-box'` + `transformOrigin:'bottom'`
 *   - labels live in the DOM (bin edges, p95 chip), not baked into the SVG
 */
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { clamp, lerp, norm, linScale, ms } from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import type { LatencyHistogram } from "../lib/data";

export interface HistogramProps {
  histogram: LatencyHistogram;
  /** fill for the success portion of each bar */
  successColor?: string;
  /** fill for the error portion stacked above success */
  errorColor?: string;
  width?: number;
  height?: number;
  /** shared master clock (overview); omit for a self-contained standalone loop */
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
  ariaLabel?: string;
}

interface Insets {
  top: number;
  right: number;
  bottom: number;
  left: number;
}

// Room at the bottom for the HTML bin-edge axis, at the top for the p95 chip.
const PAD: Insets = { top: 18, right: 6, bottom: 16, left: 6 };
/** fraction of a slot taken by the bar (rest is the gap between bars) */
const BAR_FILL = 0.72;

export function Histogram({
  histogram,
  successColor = token.cSuccess,
  errorColor = token.cError,
  width = 520,
  height = 200,
  progress,
  playWindow,
  durationMs,
  className,
  style,
  ariaLabel,
}: HistogramProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });
  const { bins, p95 } = histogram;

  const plotLeft = PAD.left;
  const plotRight = width - PAD.right;
  const plotTop = PAD.top;
  const plotBottom = height - PAD.bottom;
  const plotW = plotRight - plotLeft;

  const n = bins.length;
  const slot = n > 0 ? plotW / n : plotW;
  const barW = slot * BAR_FILL;
  // x-center of bar `i`
  const centerOf = (i: number) => plotLeft + slot * (i + 0.5);

  // Heights scale off the busiest bin's total (success + error).
  const totals = bins.map((b) => b.successFrequency + b.errorFrequency);
  const maxTotal = Math.max(1, ...totals);
  const yScale = linScale(0, maxTotal, plotBottom, plotTop);
  // pixel height of a frequency span (0 at baseline → up)
  const hOf = (freq: number) => plotBottom - yScale(freq);

  // p95 x: find the bin edge interval containing p95, then interpolate between the
  // two surrounding bar centers (bins are categorical edges, not linearly spaced).
  const p95X = p95MarkerX(
    bins.map((b) => b.bin),
    p95,
    centerOf,
    plotLeft,
    plotRight,
  );

  // Bars finish first; the p95 marker draws over a later slice of the cycle.
  const barsEnd = 0.74;
  const markerWin: [number, number] = [0.78, 1];

  const totalSuccess = bins.reduce((s, b) => s + b.successFrequency, 0);
  const totalError = bins.reduce((s, b) => s + b.errorFrequency, 0);
  const label =
    ariaLabel ??
    `Latency distribution histogram across ${n} bins: ` +
      `${totalSuccess.toLocaleString()} successful and ${totalError.toLocaleString()} failed ` +
      `operations, p95 at ${ms(p95)}.`;

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
        {/* baseline under the bars */}
        <line
          x1={plotLeft}
          x2={plotRight}
          y1={plotBottom}
          y2={plotBottom}
          stroke={token.grid}
          strokeWidth={1}
          vectorEffect="non-scaling-stroke"
        />

        {bins.map((b, i) => {
          // stagger: each bar grows over a sub-window sliding left→right
          const s0 = (i / Math.max(1, n)) * barsEnd * 0.55;
          const s1 = Math.min(s0 + barsEnd * 0.5, barsEnd);
          return (
            <Bar
              key={b.bin}
              x={centerOf(i) - barW / 2}
              barW={barW}
              baselineY={plotBottom}
              successH={hOf(b.successFrequency)}
              errorH={hOf(b.errorFrequency)}
              successColor={successColor}
              errorColor={errorColor}
              t={t}
              grow={[s0, s1]}
            />
          );
        })}

        <P95Marker
          x={p95X}
          top={plotTop}
          bottom={plotBottom}
          t={t}
          win={markerWin}
        />
      </svg>

      {/* p95 chip — HTML, anchored near the top of the marker line */}
      <motion.div
        style={{
          position: "absolute",
          top: 0,
          left: `${(p95X / width) * 100}%`,
          transform: "translateX(-50%)",
          display: "flex",
          alignItems: "center",
          gap: 4,
          padding: "1px 6px",
          borderRadius: 4,
          whiteSpace: "nowrap",
          fontFamily: token.mono,
          fontSize: 10,
          lineHeight: 1.4,
          color: token.textStrong,
          background: token.surface,
          border: `1px solid ${token.borderStrong}`,
          opacity: useTransform(
            t,
            [markerWin[0] + 0.04, markerWin[1]],
            [0, 1],
            {
              clamp: true,
            },
          ),
        }}
      >
        <span style={{ color: token.textSecondary }}>p95</span>
        <span>{ms(p95)}</span>
      </motion.div>

      {/* bin-edge axis labels — HTML, under the bars */}
      <div
        aria-hidden
        style={{
          position: "absolute",
          left: `${(plotLeft / width) * 100}%`,
          right: `${(PAD.right / width) * 100}%`,
          bottom: 0,
          height: PAD.bottom,
          display: "flex",
        }}
      >
        {bins.map((b) => (
          <span
            key={b.bin}
            style={{
              flex: "1 1 0",
              textAlign: "center",
              fontFamily: token.mono,
              fontSize: 9,
              lineHeight: `${PAD.bottom}px`,
              color: token.textSecondary,
              overflow: "hidden",
            }}
          >
            {b.bin}
          </span>
        ))}
      </div>
    </div>
  );
}

/**
 * One stacked bin bar: success (bottom) + error (above). Each rect scales from full
 * height down to 0 via `scaleY` so it grows out of the baseline; we offset the error
 * rect by the success rect's full height so the stack reads correctly while growing.
 */
function Bar({
  x,
  barW,
  baselineY,
  successH,
  errorH,
  successColor,
  errorColor,
  t,
  grow,
}: {
  x: number;
  barW: number;
  baselineY: number;
  successH: number;
  errorH: number;
  successColor: string;
  errorColor: string;
  t: MotionValue<number>;
  grow: [number, number];
}) {
  const p = useTransform(t, [grow[0], grow[1]], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  // success rises first, error tops off slightly later for a "stacking" read
  const scaleSuccess = useTransform(p, [0, 0.85], [0, 1], { clamp: true });
  const scaleError = useTransform(p, [0.3, 1], [0, 1], { clamp: true });

  const successTop = baselineY - successH;
  const errorTop = successTop - errorH;

  return (
    <g>
      {successH > 0.5 && (
        <motion.rect
          x={x}
          y={successTop}
          width={barW}
          height={successH}
          rx={1}
          style={{
            fill: successColor,
            transformBox: "fill-box",
            transformOrigin: "bottom",
            scaleY: scaleSuccess,
          }}
        />
      )}
      {errorH > 0.5 && (
        <motion.rect
          x={x}
          y={errorTop}
          width={barW}
          height={errorH}
          rx={1}
          style={{
            fill: errorColor,
            transformBox: "fill-box",
            transformOrigin: "bottom",
            scaleY: scaleError,
          }}
        />
      )}
    </g>
  );
}

/** Dashed vertical p95 line that draws top→bottom over its window. */
function P95Marker({
  x,
  top,
  bottom,
  t,
  win,
}: {
  x: number;
  top: number;
  bottom: number;
  t: MotionValue<number>;
  win: [number, number];
}) {
  const draw = useTransform(t, [win[0], win[1]], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const opacity = useTransform(t, [win[0], win[0] + 0.05], [0, 1], {
    clamp: true,
  });
  return (
    <motion.line
      x1={x}
      x2={x}
      y1={top}
      y2={bottom}
      stroke={token.textStrong}
      strokeWidth={1.5}
      strokeDasharray="4 3"
      vectorEffect="non-scaling-stroke"
      style={{ pathLength: draw, opacity }}
    />
  );
}

/**
 * Map a latency value to an x-coordinate across categorical bin edges. Bars sit at
 * fixed centers (one per edge); we find which edge interval `value` falls in and lerp
 * between the two neighbouring centers, clamped to the plot.
 */
function p95MarkerX(
  edges: number[],
  value: number,
  centerOf: (i: number) => number,
  lo: number,
  hi: number,
): number {
  const n = edges.length;
  if (n === 0) return lo;
  if (value <= edges[0]) return clamp(centerOf(0), lo, hi);
  if (value >= edges[n - 1]) return clamp(centerOf(n - 1), lo, hi);
  for (let i = 0; i < n - 1; i++) {
    if (value >= edges[i] && value <= edges[i + 1]) {
      const f = norm(value, edges[i], edges[i + 1]);
      return clamp(lerp(centerOf(i), centerOf(i + 1), f), lo, hi);
    }
  }
  return clamp(centerOf(n - 1), lo, hi);
}
