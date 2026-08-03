import type { CSSProperties, ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

export type TileSpan = "full" | "wide" | "default";

export interface TileProps {
  title: string;
  subheader?: string;
  action?: ReactNode;
  children: ReactNode;
  span?: TileSpan;
  padding?: number;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
}

const SPAN_COLUMN: Record<TileSpan, CSSProperties["gridColumn"]> = {
  full: "1 / -1",
  wide: "span 2",
  default: undefined,
};

const SKELETON_BARS = ["62%", "92%", "78%"];

export function Tile({
  title,
  subheader,
  action,
  children,
  span = "default",
  padding = 14,
  progress,
  playWindow,
  durationMs,
  className,
  style,
}: TileProps) {
  const { ref, t, reduced } = useChartClock({
    progress,
    playWindow,
    durationMs,
  });

  const cardOpacity = useTransform(t, [0, 0.18], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const cardY = useTransform(t, [0, 0.18], [8, 0], {
    ease: ease.out,
    clamp: true,
  });

  const skeletonOpacity = useTransform(t, [0, 0.25], [1, 0], {
    ease: ease.inOut,
    clamp: true,
  });
  const bodyOpacity = useTransform(t, [0.18, 0.4], [0, 1], {
    ease: ease.out,
    clamp: true,
  });

  const skeletonVisible = useTransform(skeletonOpacity, (o) =>
    o <= 0.001 ? "none" : "block",
  );

  const ariaLabel = subheader ? `${title} — ${subheader}` : title;

  return (
    <motion.section
      ref={ref}
      className={className}
      role="group"
      aria-label={ariaLabel}
      style={{
        position: "relative",
        width: "100%",
        height: "100%",
        boxSizing: "border-box",
        display: "flex",
        flexDirection: "column",
        gap: 10,
        padding,
        background: token.card,
        border: `1px solid ${token.border}`,
        borderRadius: 6,
        boxShadow: token.shadow,
        gridColumn: SPAN_COLUMN[span],
        opacity: cardOpacity,
        y: cardY,
        ...style,
      }}
    >
      <header
        style={{
          display: "flex",
          alignItems: "flex-start",
          justifyContent: "space-between",
          gap: 8,
        }}
      >
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            gap: 2,
            minWidth: 0,
          }}
        >
          <span
            style={{
              fontFamily: token.fontHeading,
              fontSize: 14,
              fontWeight: 600,
              letterSpacing: "0.01em",
              lineHeight: 1.3,
              color: token.textStrong,
            }}
          >
            {title}
          </span>
          {subheader && (
            <span
              style={{
                fontSize: 11,
                lineHeight: 1.3,
                color: token.textSecondary,
              }}
            >
              {subheader}
            </span>
          )}
        </div>
        {action != null && (
          <div style={{ flexShrink: 0, display: "flex", alignItems: "center" }}>
            {action}
          </div>
        )}
      </header>

      <div style={{ position: "relative", flex: 1, minHeight: 0 }}>
        <motion.div style={{ height: "100%", opacity: bodyOpacity }}>
          {children}
        </motion.div>

        <motion.div
          aria-hidden
          style={{
            position: "absolute",
            inset: 0,
            display: skeletonVisible,
            flexDirection: "column",
            justifyContent: "center",
            gap: 10,
            opacity: skeletonOpacity,
            pointerEvents: "none",
          }}
        >
          {SKELETON_BARS.map((w, i) => (
            <SkeletonBar key={i} width={w} index={i} t={t} reduced={reduced} />
          ))}
        </motion.div>
      </div>
    </motion.section>
  );
}

function SkeletonBar({
  width,
  index,
  t,
  reduced,
}: {
  width: string;
  index: number;
  t: MotionValue<number>;
  reduced: boolean;
}) {
  const start = index * 0.06;
  const shimmer = useTransform(t, [start, start + 0.22], [-120, 220], {
    clamp: false,
  });
  const shimmerX = useTransform(shimmer, (v: number) => `${v}%`);

  return (
    <div
      style={{
        position: "relative",
        height: 9,
        width,
        borderRadius: 4,
        background: token.skeleton,
        overflow: "hidden",
      }}
    >
      {!reduced && (
        <motion.div
          style={{
            position: "absolute",
            top: 0,
            bottom: 0,
            width: "40%",
            x: shimmerX,
            background: `linear-gradient(90deg, transparent, ${token.shimmer}, transparent)`,
          }}
        />
      )}
    </div>
  );
}
