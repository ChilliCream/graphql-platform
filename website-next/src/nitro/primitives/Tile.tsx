/**
 * Tile — card shell with an animated skeleton→content entrance (PLAN.md §10 step 0).
 *
 * The dashboard's first beat is every tile "booting up": the card fades + rises in, a
 * skeleton overlay shimmers over the body, then crossfades out as the real children fade
 * in. Like every primitive the timing is derived from a normalized clock `t` (0→1) via
 * `useChartClock`, so it animates standalone in its story and also slots into the
 * Monitoring Overview's shared cycle (pass `progress`/`playWindow`).
 *
 * This is chrome, not a chart — no SVG/viewBox here. It still honors the shared contract:
 * fills its container (width/height 100%), takes `className`/`style`, and collapses to a
 * static final frame (content shown, skeleton hidden) under reduced motion via `t = 1`.
 */
import type { CSSProperties, ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";

/** How the tile spans a parent dashboard grid. */
export type TileSpan = "full" | "wide" | "default";

export interface TileProps {
  /** Header title (strong text). */
  title: string;
  /** Optional secondary line under the title. */
  subheader?: string;
  /** Optional right-aligned control / badge in the header. */
  action?: ReactNode;
  /** Tile body. */
  children: ReactNode;
  /** Grid placement: 'full' → `1 / -1`, 'wide' → `span 2`, 'default' → unset. */
  span?: TileSpan;
  /** Inner padding (px). */
  padding?: number;
  /** Shared master clock (overview); omit for a self-contained standalone loop. */
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

/** Skeleton bar widths (% of body) — a title-ish bar then a couple of body lines. */
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

  // Whole card: fade + rise in early, then hold.
  const cardOpacity = useTransform(t, [0, 0.18], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const cardY = useTransform(t, [0, 0.18], [8, 0], {
    ease: ease.out,
    clamp: true,
  });

  // Skeleton overlay crossfades out; real body fades in just after.
  const skeletonOpacity = useTransform(t, [0, 0.25], [1, 0], {
    ease: ease.inOut,
    clamp: true,
  });
  const bodyOpacity = useTransform(t, [0.18, 0.4], [0, 1], {
    ease: ease.out,
    clamp: true,
  });

  // Skip the overlay entirely once it has faded so it never traps pointer events.
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

      {/* Body: real content fades in over the skeleton overlay. */}
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

/**
 * A single shimmer bar. A highlight sweeps left→right (driven by `t`, looped via the
 * standalone clock) only when motion is allowed; under reduced motion it's a flat block.
 * Rendered as a child so its `useTransform` hooks aren't called in a parent loop.
 */
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
  // Stagger each bar's sweep slightly so the shimmer ripples down the stack.
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
