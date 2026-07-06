import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { logScale, ms, compact } from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import type { LatencyDistribution } from "../lib/data";

export interface DistributionHistogramProps {
  distribution: LatencyDistribution;
  width?: number;
  height?: number;
  yDomain: [number, number];
  xDomain?: [number, number];
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  ariaLabel?: string;
  className?: string;
  style?: CSSProperties;
}

export function DistributionHistogram({
  distribution,
  width = 1000,
  height = 240,
  yDomain,
  xDomain = [distribution.min, distribution.max],
  progress,
  playWindow,
  durationMs,
  ariaLabel,
  className,
  style,
}: DistributionHistogramProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });
  const xScale = logScale(xDomain[0], xDomain[1], 0, width);
  const yScale = logScale(yDomain[0], yDomain[1], height, 0);
  const n = distribution.bins.length;

  const label =
    ariaLabel ??
    `Latency distribution of ${compact(distribution.total)} operations, p95 ${ms(distribution.p95)}`;

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
        {distribution.bins.map((b, i) => {
          const x0 = xScale(b.x0);
          const x1 = xScale(b.x1);
          const total = b.success + b.error;
          if (total <= 0) return null;
          return (
            <Bar
              key={i}
              x={x0 + 0.5}
              w={Math.max(0.6, x1 - x0 - 1)}
              baseY={height}
              successY={yScale(Math.max(1, b.success))}
              totalY={yScale(total)}
              hasError={b.error > 0}
              frac={i / Math.max(1, n - 1)}
              t={t}
            />
          );
        })}
      </svg>

      <Marker
        label="Current"
        leftPct={(xScale(distribution.current) / width) * 100}
        color={token.cP99}
        t={t}
        at={0.6}
      />
      <Marker
        label="p95"
        leftPct={(xScale(distribution.p95) / width) * 100}
        color={token.cP95}
        t={t}
        at={0.7}
      />
    </div>
  );
}

function Bar({
  x,
  w,
  baseY,
  successY,
  totalY,
  hasError,
  frac,
  t,
}: {
  x: number;
  w: number;
  baseY: number;
  successY: number;
  totalY: number;
  hasError: boolean;
  frac: number;
  t: MotionValue<number>;
}) {
  const s0 = frac * 0.7;
  const grow = useTransform(t, [s0, s0 + 0.22], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  return (
    <motion.g
      style={{
        scaleY: grow,
        transformBox: "fill-box",
        transformOrigin: "bottom",
      }}
    >
      <rect
        x={x}
        y={successY}
        width={w}
        height={Math.max(0, baseY - successY)}
        style={{ fill: token.cSuccess }}
      />
      {hasError && (
        <rect
          x={x}
          y={totalY}
          width={w}
          height={Math.max(0, successY - totalY)}
          style={{ fill: token.cError }}
        />
      )}
    </motion.g>
  );
}

function Marker({
  label,
  leftPct,
  color,
  t,
  at,
}: {
  label: string;
  leftPct: number;
  color: string;
  t: MotionValue<number>;
  at: number;
}) {
  const opacity = useTransform(t, [at, at + 0.1], [0, 1], { clamp: true });
  const scaleY = useTransform(t, [at, at + 0.14], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  return (
    <motion.div
      aria-hidden
      style={{
        position: "absolute",
        top: 0,
        bottom: 0,
        left: `${leftPct}%`,
        opacity,
        pointerEvents: "none",
      }}
    >
      <motion.div
        style={{
          position: "absolute",
          top: 18,
          bottom: 0,
          left: 0,
          width: 0,
          borderLeft: `1.5px solid ${color}`,
          transformOrigin: "top",
          scaleY,
        }}
      />
      <span
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          transform: "translateX(-50%)",
          fontSize: 10,
          fontWeight: 600,
          color: "#fff",
          background: color,
          borderRadius: 3,
          padding: "1px 6px",
          whiteSpace: "nowrap",
        }}
      >
        {label}
      </span>
    </motion.div>
  );
}
