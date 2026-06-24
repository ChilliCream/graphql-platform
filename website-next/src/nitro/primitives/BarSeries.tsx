/**
 * BarSeries — vertical bars that grow from the baseline, staggered (PLAN.md §10.2).
 *
 * The throughput card's "errors per minute" mini-bars: one rect per value, each scaling
 * up from the bottom edge on a slightly offset sub-window of the normalized clock `t`
 * (0→1) so the row sweeps left→right. Driven by `useChartClock`, it animates standalone
 * in its story and also slots into the Monitoring Overview's shared cycle.
 *
 * Responsive contract (shared by every chart primitive):
 *   - inline SVG, `viewBox="0 0 W H"`, `preserveAspectRatio="none"`, width/height 100%
 *   - strokes get `vectorEffect="non-scaling-stroke"` so they stay crisp when stretched
 *   - bars scale via `scaleY` with `transformBox: 'fill-box'` + `transformOrigin: 'bottom'`
 *     so they grow from their own bottom edge regardless of the stretch
 *   - labels (none baked in here) live in the DOM, never in the SVG
 */
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { clamp, linScale } from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export interface BarSeriesProps {
  /** one bar per value, left→right */
  values: number[];
  /** bar color — a `token.*` var string or hex */
  color?: string;
  width?: number;
  height?: number;
  /** y-domain; `[0, max]` from data when omitted */
  domain?: [number, number];
  /** gap between bars, in viewBox units */
  gap?: number;
  /** corner radius of each bar, in viewBox units */
  barRadius?: number;
  /** fraction of the local window each successive bar is offset by */
  stagger?: number;
  /** shared master clock (overview); omit for a self-contained standalone loop */
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
  ariaLabel?: string;
}

export function BarSeries({
  values,
  color = token.cError,
  width = 600,
  height = 200,
  domain,
  gap = 2,
  barRadius = 1,
  stagger = 0.6,
  progress,
  playWindow,
  durationMs,
  className,
  style,
  ariaLabel,
}: BarSeriesProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });

  const n = values.length;
  const dMin = domain ? domain[0] : 0;
  const dMax = domain ? domain[1] : Math.max(1, ...values);
  const yScale = linScale(dMin, dMax, height, 0);

  // Bars share the full width; gap is split into the slot so the row is flush-fit.
  const slot = n > 0 ? width / n : width;
  const barW = Math.max(0.5, slot - gap);

  // Per-bar draw window: each bar's sub-window is `span` wide, sliding across [0,1] by
  // index so the row reveals left→right while overlapping (controlled by `stagger`).
  const span = Math.max(0.001, 1 - (n - 1) * (stagger / Math.max(1, n)));
  const step = n > 1 ? (1 - span) / (n - 1) : 0;

  const peak = n ? Math.max(...values) : 0;
  const label =
    ariaLabel ?? `Bar series of ${n} values, peak ${Math.round(peak)}`;

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
        {values.map((v, i) => {
          const x = i * slot + (slot - barW) / 2;
          const yTop = yScale(v);
          const barH = Math.max(0, height - yTop);
          const s0 = clamp(i * step, 0, 0.999);
          const s1 = clamp(s0 + span, s0 + 0.001, 1);
          return (
            <Bar
              key={i}
              x={x}
              y={yTop}
              width={barW}
              height={barH}
              radius={barRadius}
              color={color}
              t={t}
              grow={[s0, s1]}
            />
          );
        })}
      </svg>
    </div>
  );
}

function Bar({
  x,
  y,
  width,
  height,
  radius,
  color,
  t,
  grow,
}: {
  x: number;
  y: number;
  width: number;
  height: number;
  radius: number;
  color: string;
  t: MotionValue<number>;
  grow: [number, number];
}) {
  const scaleY = useTransform(t, [grow[0], grow[1]], [0, 1], {
    ease: ease.out,
    clamp: true,
  });

  return (
    <motion.rect
      x={x}
      y={y}
      width={width}
      height={height}
      rx={radius}
      ry={radius}
      vectorEffect="non-scaling-stroke"
      style={{
        fill: color,
        transformBox: "fill-box",
        transformOrigin: "bottom",
        scaleY,
      }}
    />
  );
}
