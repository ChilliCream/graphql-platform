/**
 * CountUp — animated metric number.
 *
 * A single big stat that tweens from `from` to `value` as the normalized clock `t`
 * (0→1) sweeps its play window, then holds. The displayed string is derived straight
 * off `t` via `useTransform`, so the count animates standalone in its story and also
 * rides the Monitoring Overview's shared cycle when given a `progress` value.
 *
 * Accessibility: the live, ticking span is `aria-hidden` and the root carries a
 * stable `aria-label` (the *final* formatted value), so screen readers announce the
 * destination number once instead of every intermediate frame. Under reduced motion
 * `useChartClock` pins `t = 1`, so the final value is shown immediately.
 */
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { compact } from "../lib/scale";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export interface CountUpProps {
  /** Destination value the number counts up to. */
  value: number;
  /** Formats the live number (defaults to `compact`, e.g. 1234 → "1.2k"). */
  format?: (n: number) => string;
  /** Value to count up from. */
  from?: number;
  /** Shared master progress; omit to run a self-contained, in-view-gated clock. */
  progress?: MotionValue<number>;
  /** When this primitive counts within the cycle, as [start,end] fractions of 0..1. */
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
  /** Override the screen-reader label (defaults to the final formatted value). */
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

  // Drive the displayed text directly off the clock: from → value across t.
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
          // tabular numerals keep width steady so the value doesn't jitter as it ticks
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {display}
      </motion.span>
    </div>
  );
}
