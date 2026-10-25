import type { CSSProperties, ReactNode } from "react";
import { motion, useMotionValue, type MotionValue } from "motion/react";
import { useElementSize } from "../../lib/useElementSize";
import { token } from "../../lib/tokens";
import { ChartCanvas } from "../ChartCanvas";

export interface StageCamera {
  x: MotionValue<number>;
  y: MotionValue<number>;
  scale: MotionValue<number>;
}

export interface StageProps {
  width?: number;
  height?: number;
  camera?: StageCamera;
  children: ReactNode;
  overlay?: ReactNode;
  fit?: "aspect" | "fill";
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
    <ChartCanvas
      ref={ref}
      sizing="none"
      className={className}
      label={ariaLabel}
      style={{
        ...fillStyle,
        overflow: "hidden",
        borderRadius: chrome ? 10 : 0,
        border: chrome ? `1px solid ${token.borderStrong}` : "none",
        background: token.bg,
        ...style,
      }}
    >
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
    </ChartCanvas>
  );
}
