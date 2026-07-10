import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { compact } from "../lib/scale";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export interface CountUpProps {
  value: number;
  format?: (n: number) => string;
  from?: number;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
  ariaLabel?: string;
}

export function CountUp({
  value,
  format = (n) => compact(n),
  from = 0,
  progress,
  playWindow,
  durationMs,
  className,
  style,
  ariaLabel,
}: CountUpProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });

  const display = useTransform(t, (p) => format(from + (value - from) * p));

  return (
    <div
      ref={ref}
      className={className}
      role="img"
      aria-label={ariaLabel ?? format(value)}
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        width: "100%",
        height: "100%",
        ...style,
      }}
    >
      <motion.span
        aria-hidden="true"
        style={{
          fontFamily: token.mono,
          fontSize: 32,
          fontWeight: 600,
          lineHeight: 1,
          letterSpacing: "-0.02em",
          color: token.textStrong,
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {display}
      </motion.span>
    </div>
  );
}
