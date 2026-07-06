/**
 * DashboardFrame — responsive dashboard grid that reflows 3→2→1 WITHOUT media queries.
 *
 * The layout chrome every other primitive lives inside. A single CSS Grid track template
 * does the responsive work: `repeat(auto-fit, minmax(min(100%, <min>), 1fr))` packs as
 * many columns of at least `minColWidth` as fit, then drops to fewer (and finally one,
 * thanks to the `min(100%, …)` floor) as the container narrows — purely from intrinsic
 * sizing, so it adapts to its container, not the viewport.
 *
 * Children place themselves with `gridColumn`:
 *   - full-width banner → `gridColumn: '1 / -1'`   (or the `FULL_WIDTH` constant below)
 *   - wide (two tracks)  → `gridColumn: 'span 2'`   (or `SPAN_2`; auto-clamps to a single
 *     column when only one track fits, because Grid can't span past the last line)
 *
 * Animation is optional and deliberately minimal — a layout shell shouldn't draw the eye.
 * When mounted it fades/rises in once via the shared clock `t` (0→1), which also means it
 * slots into the Monitoring Overview's master cycle and freezes to its final frame under
 * reduced motion, for free.
 */
import type { CSSProperties, ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { ease } from "../lib/motion";
import { useChartClock } from "../lib/useInViewLoop";

/** Drop a child onto every column (full-bleed banner / hero row). */
export const FULL_WIDTH: CSSProperties = { gridColumn: "1 / -1" };
/** Let a child occupy two tracks (auto-clamps to one on a single-column layout). */
export const SPAN_2: CSSProperties = { gridColumn: "span 2" };

export interface DashboardFrameProps {
  children: ReactNode;
  /** gutter between tiles, px */
  gap?: number;
  /** minimum track width before the grid drops a column, px */
  minColWidth?: number;
  /** outer padding around the grid, px */
  padding?: number;
  /** opt out of the one-shot mount fade (e.g. when the parent owns motion) */
  animate?: boolean;
  /** shared master clock (overview); omit for a self-contained standalone fade */
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
  // A subtle settle: fade up over the play window, then hold. When `animate` is off we
  // pin to the final frame so the grid is fully visible and never re-derives on `t`.
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
        // 3→2→1 reflow with no media queries: the `min(100%, …)` floor lets a track
        // shrink to the full width so the last column can always collapse cleanly.
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
