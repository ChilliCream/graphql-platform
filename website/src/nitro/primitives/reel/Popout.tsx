import type { MotionValue } from "motion/react";
import { motion, useTransform } from "motion/react";
import { token } from "../../lib/tokens";

export interface PopoutRow {
  label: string;
  value: string;
  color?: string;
}

export interface PopoutProps {
  x: number;
  y: number;
  progress: MotionValue<number>;
  show: number;
  hide: number;
  title?: string;
  rows: PopoutRow[];
  anchor?: "top-left" | "top-right" | "bottom-left" | "bottom-right";
}

export function Popout({
  x,
  y,
  progress,
  show,
  hide,
  title,
  rows,
  anchor = "top-left",
}: PopoutProps) {
  const FADE = 0.02;
  const opacity = useTransform(
    progress,
    [show, show + FADE, hide - FADE, hide],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const ty = useTransform(progress, [show, show + FADE], [6, 0], {
    clamp: true,
  });

  const right = anchor.endsWith("right");
  const bottom = anchor.startsWith("bottom");

  return (
    <motion.div
      aria-hidden
      data-testid="reel-popout"
      data-popout-title={title}
      style={{
        position: "absolute",
        left: x,
        top: y,
        opacity,
        y: ty,
        transform: `translate(${right ? "-100%" : "0"}, ${bottom ? "-100%" : "0"})`,
        background: token.surface,
        border: `1px solid ${token.borderStrong}`,
        borderRadius: 6,
        boxShadow: "0 6px 20px rgba(1,4,9,0.5)",
        padding: "8px 10px",
        minWidth: 132,
        pointerEvents: "none",
        zIndex: 40,
      }}
    >
      {title && (
        <div
          style={{ fontSize: 11, color: token.textSecondary, marginBottom: 6 }}
        >
          {title}
        </div>
      )}
      <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
        {rows.map((r) => (
          <div
            key={r.label}
            style={{ display: "flex", alignItems: "center", gap: 8 }}
          >
            <span
              style={{
                width: 8,
                height: 8,
                borderRadius: 2,
                background: r.color ?? token.textSecondary,
                flex: "0 0 auto",
              }}
            />
            <span style={{ fontSize: 11, color: token.textSecondary }}>
              {r.label}
            </span>
            <span
              style={{
                marginLeft: "auto",
                fontSize: 12,
                fontFamily: token.mono,
                color: token.textStrong,
              }}
            >
              {r.value}
            </span>
          </div>
        ))}
      </div>
    </motion.div>
  );
}
