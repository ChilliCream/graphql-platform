/**
 * Cursor — a simulated pointer for the reel. Position is driven by x/y MotionValues (the
 * orchestrator scripts the path); a ripple fires near each click time, and the pointer dips
 * slightly on press. It renders as an ARROW while travelling and switches to the POINTING-HAND
 * over a clickable element (around each click beat, plus any explicit `pointerWindows`) — like a
 * real browser. Purely decorative chrome — aria-hidden.
 */
import { motion, useTransform, type MotionValue } from "motion/react";
import { clamp } from "../../lib/scale";

export interface CursorProps {
  x: MotionValue<number>;
  y: MotionValue<number>;
  /** master progress 0..1 */
  progress: MotionValue<number>;
  /** times (0..1) at which a click ripple fires */
  clickTimes?: number[];
  /** extra [start,end] windows where the cursor hovers a clickable element (shows the hand) */
  pointerWindows?: [number, number][];
  /** how long before a click the hand appears (hover lead) */
  hoverLead?: number;
  /** optional overall opacity (fade the pointer out, e.g. across a page transition so it never
   *  appears to instantly warp between two distant targets) */
  opacity?: MotionValue<number>;
}

// arrow pointer — tip at (1,1)
const ARROW = "M1 1 L1 19 L5.6 14.6 L8.5 21 L11.4 19.7 L8.6 13.6 L14.5 13.6 Z";
// pointing hand (index finger up) — authored with the fingertip near the top, then translated so
// the fingertip sits at the (1,1) hotspot to match the arrow's click point.
const HAND =
  "M9 11.5V4a1.6 1.6 0 0 1 3.2 0v6.6h.7a1.35 1.35 0 0 1 1.35-1.2 1.35 1.35 0 0 1 1.35 1.2 1.35 1.35 0 0 1 1.35-1.0 1.35 1.35 0 0 1 1.35 1.25v4.4a5 5 0 0 1-5 5h-2.2a4.3 4.3 0 0 1-3.2-1.43l-3.15-3.5a1.5 1.5 0 0 1 2.2-2.02l1.5 1.45V11.5z";

export function Cursor({
  x,
  y,
  progress,
  clickTimes = [],
  pointerWindows = [],
  hoverLead = 0.06,
  opacity,
}: CursorProps) {
  // tiny press dip just as each click lands
  const press = useTransform(progress, (p) => {
    let s = 1;
    for (const t of clickTimes) {
      const d = Math.abs(p - t);
      if (d < 0.012) s = Math.min(s, 0.82 + (d / 0.012) * 0.18);
    }
    return s;
  });

  // hand when hovering a clickable element: just before/at each click, or inside a pointer window
  const hand = useTransform(progress, (p): number => {
    for (const t of clickTimes)
      if (p >= t - hoverLead && p <= t + 0.02) return 1;
    for (const [a, b] of pointerWindows) if (p >= a && p <= b) return 1;
    return 0;
  });
  const arrowOpacity = useTransform(hand, [0, 1], [1, 0]);

  return (
    <motion.div
      aria-hidden
      data-testid="reel-cursor"
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        x,
        y,
        opacity,
        zIndex: 50,
        willChange: "transform",
      }}
    >
      {clickTimes.map((t, i) => (
        <Ripple key={i} progress={progress} at={t} />
      ))}
      <motion.div
        style={{
          position: "relative",
          width: 22,
          height: 24,
          scale: press,
          transformOrigin: "2px 2px",
        }}
      >
        {/* arrow */}
        <motion.svg
          width={22}
          height={24}
          viewBox="0 0 22 24"
          style={{
            position: "absolute",
            inset: 0,
            display: "block",
            opacity: arrowOpacity,
          }}
        >
          <path
            d={ARROW}
            fill="#fff"
            stroke="#10151c"
            strokeWidth={1.4}
            strokeLinejoin="round"
          />
        </motion.svg>
        {/* hand (fingertip translated to the (1,1) hotspot) */}
        <motion.svg
          width={26}
          height={28}
          viewBox="0 0 26 28"
          style={{
            position: "absolute",
            left: -9,
            top: -2,
            display: "block",
            opacity: hand,
          }}
        >
          <path
            d={HAND}
            fill="#fff"
            stroke="#10151c"
            strokeWidth={1.3}
            strokeLinejoin="round"
            strokeLinecap="round"
          />
        </motion.svg>
      </motion.div>
    </motion.div>
  );
}

function Ripple({
  progress,
  at,
}: {
  progress: MotionValue<number>;
  at: number;
}) {
  const scale = useTransform(progress, [at, at + 0.05], [0, 2.6], {
    clamp: true,
  });
  const opacity = useTransform(progress, (p) => {
    const d = p - at;
    if (d < 0 || d > 0.05) return 0;
    return clamp(0.5 * (1 - d / 0.05), 0, 0.5);
  });
  return (
    <motion.span
      style={{
        position: "absolute",
        left: 2,
        top: 2,
        width: 22,
        height: 22,
        marginLeft: -11,
        marginTop: -11,
        borderRadius: "50%",
        border: "2px solid #9fe8ff",
        scale,
        opacity,
      }}
    />
  );
}
