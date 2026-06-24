/**
 * MarkerLine — service-version vertical marker + label overlay (PLAN.md §10.6).
 *
 * A presentational overlay you drop on top of a time-series chart to flag a deploy:
 * a dashed vertical rule "drops" in from the top (scaleY 0→1, origin top) and a small
 * label chip fades + slides down just after the rule starts. Everything is derived from
 * the normalized clock `t` (0→1) of `useChartClock`, so it animates standalone in its
 * story and also slots into the Monitoring Overview's shared cycle.
 *
 * Responsive contract: root div is `position:relative; width:100%; height:100%` so the
 * marker tracks its parent chart; the rule is positioned at `left: at*100%`. No SVG here
 * — the line is a thin bordered div and the chip is semantic HTML, legible in both themes.
 */
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export interface MarkerLineProps {
  /** Marker text, e.g. "v2.14.0". */
  label: string;
  /** Secondary line under the label, e.g. "deployed". */
  caption?: string;
  /** Horizontal position as a 0..1 fraction of the width. */
  at?: number;
  /** Accent color for the rule + chip border. */
  color?: string;
  /** shared master clock (overview); omit for a self-contained standalone loop */
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
}

export function MarkerLine({
  label,
  caption,
  at = 0.74,
  color = token.active,
  progress,
  playWindow,
  durationMs,
  className,
  style,
}: MarkerLineProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });

  // The rule drops first (0→0.55 of the window); the chip lands just after (0.3→1).
  const ruleScaleY = useTransform(t, [0, 0.55], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const chipOpacity = useTransform(t, [0.3, 0.7], [0, 1], { clamp: true });
  const chipY = useTransform(t, [0.3, 0.85], [-6, 0], {
    ease: ease.pop,
    clamp: true,
  });

  const side = at > 0.5 ? "right" : "left";

  return (
    <div
      ref={ref}
      className={className}
      style={{ position: "relative", width: "100%", height: "100%", ...style }}
      role="img"
      aria-label={`Marker ${label}${caption ? ` (${caption})` : ""}`}
    >
      {/* Vertical dashed rule — grows top→bottom. */}
      <motion.div
        aria-hidden
        style={{
          position: "absolute",
          top: 0,
          bottom: 0,
          left: `${at * 100}%`,
          width: 0,
          borderLeft: `1.5px dashed ${color}`,
          transformOrigin: "top",
          scaleY: ruleScaleY,
        }}
      />

      {/* Label chip near the top, anchored to whichever side keeps it on-screen. */}
      <motion.div
        style={{
          position: "absolute",
          top: 8,
          [side]: `calc(${(side === "left" ? at : 1 - at) * 100}% + 6px)`,
          display: "flex",
          flexDirection: "column",
          gap: 1,
          padding: "3px 7px",
          borderLeft: `2px solid ${color}`,
          background: token.surface,
          borderRadius: 4,
          boxShadow: token.shadow,
          whiteSpace: "nowrap",
          pointerEvents: "none",
          opacity: chipOpacity,
          y: chipY,
        }}
      >
        <span
          style={{
            fontFamily: token.mono,
            fontSize: 11,
            fontWeight: 600,
            color: token.textStrong,
            lineHeight: 1.2,
          }}
        >
          {label}
        </span>
        {caption && (
          <span
            style={{
              fontSize: 10,
              color: token.textSecondary,
              lineHeight: 1.2,
            }}
          >
            {caption}
          </span>
        )}
      </motion.div>
    </div>
  );
}
