import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export interface MarkerLineProps {
  label: string;
  caption?: string;
  at?: number;
  color?: string;
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
