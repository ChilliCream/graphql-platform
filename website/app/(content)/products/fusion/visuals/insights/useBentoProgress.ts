import { useEffect, useRef } from "react";
import {
  animate,
  useInView,
  useMotionValue,
  useReducedMotion,
} from "motion/react";

/** One shared reveal clock for the bento's cards: paused until in view, settled instantly for reduced motion. */
export function useBentoProgress() {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotion() ?? false;
  const inView = useInView(ref, { amount: 0.25 });
  const progress = useMotionValue(0);

  useEffect(() => {
    if (reduced) {
      progress.set(1);
      return;
    }
    if (!inView) {
      return;
    }
    const controls = animate(progress, 1, { duration: 2.4, ease: "linear" });
    return () => controls.stop();
  }, [reduced, inView, progress]);

  return { ref, progress };
}
