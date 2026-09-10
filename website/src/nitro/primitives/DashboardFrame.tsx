import type { CSSProperties, ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { ease } from "../lib/motion";
import { useChartClock } from "../lib/useInViewLoop";

export const FULL_WIDTH: CSSProperties = { gridColumn: "1 / -1" };
export const SPAN_2: CSSProperties = { gridColumn: "span 2" };

export interface DashboardFrameProps {
  children: ReactNode;
  gap?: number;
  minColWidth?: number;
  padding?: number;
  animate?: boolean;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
  ariaLabel?: string;
}

export function DashboardFrame({
  children,
  gap = 16,
  minColWidth = 320,
  padding = 16,
  animate = true,
  progress,
  playWindow = [0, 0.5],
  durationMs,
  className,
  style,
  ariaLabel = "Dashboard layout",
}: DashboardFrameProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });
  const opacity = useTransform(t, [0, 1], animate ? [0, 1] : [1, 1], {
    ease: ease.out,
  });
  const y = useTransform(t, [0, 1], animate ? [8, 0] : [0, 0], {
    ease: ease.out,
  });

  return (
    <motion.div
      ref={ref}
      className={className}
      role="group"
      aria-label={ariaLabel}
      style={{
        display: "grid",
        gridTemplateColumns: `repeat(auto-fit, minmax(min(100%, ${minColWidth}px), 1fr))`,
        gap,
        padding,
        width: "100%",
        height: "100%",
        alignContent: "start",
        opacity,
        y,
        ...style,
      }}
    >
      {children}
    </motion.div>
  );
}
