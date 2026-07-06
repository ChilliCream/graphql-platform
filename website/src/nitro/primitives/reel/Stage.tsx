/**
 * Stage — the reel "viewport". A fixed design canvas (where every camera/cursor coordinate
 * is authored) scaled responsively to the container, clipping a taller screen that the
 * camera pans/zooms over. The cursor + popouts live in a separate overlay that is NOT
 * camera-transformed, so they stay in viewport space on top of the moving screen.
 *
 * Camera math: to frame a focus point (fx,fy) of the canvas at viewport centre at scale k,
 * the orchestrator sets x = W/2 - fx*k, y = H/2 - fy*k (transform-origin 0 0).
 */
import type { CSSProperties, ReactNode } from "react";
import { motion, useMotionValue, type MotionValue } from "motion/react";
import { useElementSize } from "../../lib/useElementSize";
import { token } from "../../lib/tokens";

export interface StageCamera {
  x: MotionValue<number>;
  y: MotionValue<number>;
  scale: MotionValue<number>;
}

export interface StageProps {
  /** design viewport size — the coordinate space the reel is authored in */
  width?: number;
  height?: number;
  /** optional pan/zoom camera; omit for a static (identity) camera */
  camera?: StageCamera;
  /** the screen(s) being toured (rendered at canvas width) */
  children: ReactNode;
  /** viewport-space overlay (cursor, popouts) — not camera-transformed */
  overlay?: ReactNode;
  /**
   * Sizing mode. `'aspect'` (default) makes the stage an aspect-ratio box at width:100%
   * (standalone stories). `'fill'` makes it fill an absolutely-positioned parent — used by
   * TabReel, whose content area already owns the aspect ratio and stacks crossfading screens.
   */
  fit?: "aspect" | "fill";
  /** chrome — disable the rounded border/background when embedded (e.g. inside TabReel). */
  chrome?: boolean;
  className?: string;
  style?: CSSProperties;
  ariaLabel?: string;
}

export function Stage({
  width = 1240,
  height = 780,
  camera,
  children,
  overlay,
  fit = "aspect",
  chrome = true,
  className,
  style,
  ariaLabel,
}: StageProps) {
  const { ref, width: cw } = useElementSize<HTMLDivElement>();
  const s = cw > 0 ? cw / width : 1;
  // identity-camera fallbacks (stable MotionValues) when no camera is provided
  const ix = useMotionValue(0);
  const iy = useMotionValue(0);
  const ik = useMotionValue(1);
  const cam = camera ?? { x: ix, y: iy, scale: ik };
  const fillStyle: CSSProperties =
    fit === "fill"
      ? { position: "absolute", inset: 0, width: "100%", height: "100%" }
      : {
          position: "relative",
          width: "100%",
          aspectRatio: `${width} / ${height}`,
        };

  return (
    <div
      ref={ref}
      className={className}
      role="img"
      aria-label={ariaLabel}
      style={{
        ...fillStyle,
        overflow: "hidden",
        borderRadius: chrome ? 10 : 0,
        border: chrome ? `1px solid ${token.borderStrong}` : "none",
        background: token.bg,
        ...style,
      }}
    >
      {/* fixed design canvas, scaled to fit the container width */}
      <div
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          width,
          height,
          transform: `scale(${s})`,
          transformOrigin: "0 0",
        }}
      >
        {/* camera-transformed screen layer */}
        <motion.div
          style={{
            position: "absolute",
            top: 0,
            left: 0,
            width,
            x: cam.x,
            y: cam.y,
            scale: cam.scale,
            transformOrigin: "0 0",
          }}
        >
          {children}
        </motion.div>

        {/* viewport-space overlay (cursor + popouts) — sits ABOVE all in-canvas UI (dropdown
            menus, flyouts) so the simulated cursor is never occluded. */}
        <div
          style={{
            position: "absolute",
            inset: 0,
            pointerEvents: "none",
            overflow: "hidden",
            zIndex: 100,
          }}
        >
          {overlay}
        </div>
      </div>
    </div>
  );
}
