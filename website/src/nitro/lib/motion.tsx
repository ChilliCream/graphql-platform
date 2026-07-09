import { MotionConfig, cubicBezier, useReducedMotion } from "motion/react";
import { createContext, useContext } from "react";
import type { ReactNode } from "react";

export type ReducedMotionMode = "user" | "always" | "never";

const ReducedMotionContext = createContext<ReducedMotionMode>("user");

export function useReducedMotionPreference(): boolean {
  const mode = useContext(ReducedMotionContext);
  const media = useReducedMotion() ?? false;
  if (mode === "always") return true;
  if (mode === "never") return false;
  return media;
}

export const ease = {
  out: cubicBezier(0.22, 1, 0.36, 1),
  inOut: cubicBezier(0.65, 0, 0.35, 1),
  glide: cubicBezier(0.37, 0, 0.63, 1),
  pop: cubicBezier(0.34, 1.56, 0.64, 1),
  linear: "linear" as const,
};

export const beat = {
  loop: 11,
  draw: 1.4,
  stagger: 0.08,
};

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
