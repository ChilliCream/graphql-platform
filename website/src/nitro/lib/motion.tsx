/**
 * Shared Motion config: easings, durations, and the reduced-motion-aware wrapper.
 * Import these so every demo shares one rhythm.
 */
import { MotionConfig, cubicBezier, useReducedMotion } from "motion/react";
import { createContext, useContext } from "react";
import type { ReactNode } from "react";

export type ReducedMotionMode = "user" | "always" | "never";

/**
 * Our own reduced-motion signal. Motion's public `useReducedMotion()` only reads the
 * `prefers-reduced-motion` media query — it ignores `MotionConfig reducedMotion`. We
 * provide this context so an explicit override (Storybook toolbar / forced stories)
 * is authoritative, while `'user'` still defers to the OS setting.
 */
const ReducedMotionContext = createContext<ReducedMotionMode>("user");

/** Boolean reduced-motion preference, honoring the override mode then the media query. */
export function useReducedMotionPreference(): boolean {
  const mode = useContext(ReducedMotionContext);
  const media = useReducedMotion() ?? false;
  if (mode === "always") return true;
  if (mode === "never") return false;
  return media;
}

/**
 * Cubic-bezier easings reused across charts, as `EasingFunction`s so they work in BOTH
 * `useTransform(..., { ease })` (which requires a function) and `transition.ease`.
 */
export const ease = {
  /** gentle settle (ease-out-expo-ish) — line/area draws, entrances */
  out: cubicBezier(0.22, 1, 0.36, 1),
  /** symmetric ease-in-out — counts, sweeps */
  inOut: cubicBezier(0.65, 0, 0.35, 1),
  /** smooth ease-in-out-sine — natural cursor travel: gentle accel + decel, no harsh mid-whip */
  glide: cubicBezier(0.37, 0, 0.63, 1),
  /** soft overshoot — bars growing, markers landing */
  pop: cubicBezier(0.34, 1.56, 0.64, 1),
  /** linear string for `animate()`/`transition` (clocks, sweeps) */
  linear: "linear" as const,
};

/** Beat lengths (seconds) for the master loop choreography. */
export const beat = {
  /** full loop length */
  loop: 11,
  /** an individual chart "draw in" */
  draw: 1.4,
  /** a stagger step */
  stagger: 0.08,
};

/**
 * Root wrapper. `reducedMotion="user"` (default) makes Motion respect the OS setting
 * for its declarative animations; primitives additionally branch on `useReducedMotion()`
 * — which reads this context — to jump custom `pathLength`/loop animations to their
 * final frame (Motion does not auto-cancel those).
 *
 * Pass `reducedMotion="always"` to force the reduced path (used by the Storybook motion
 * toolbar / ReducedMotion stories so the static final frame is demonstrable in-app).
 */
export function AppMotionConfig({
  children,
  reducedMotion = "user",
}: {
  children: ReactNode;
  reducedMotion?: ReducedMotionMode;
}) {
  return (
    <ReducedMotionContext.Provider value={reducedMotion}>
      <MotionConfig reducedMotion={reducedMotion}>{children}</MotionConfig>
    </ReducedMotionContext.Provider>
  );
}
