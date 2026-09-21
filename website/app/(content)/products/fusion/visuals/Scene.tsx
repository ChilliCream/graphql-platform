"use client";

import { createContext, useContext, useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

/**
 * The scene primitive the Fusion page's console visuals and hero render into:
 * a fixed-ratio stage that gates their motion by viewport visibility and tab
 * focus.
 */

export { useReducedMotionPreference };

const SceneActiveContext = createContext(false);

/**
 * `true` while the enclosing `Scene` is in the viewport and the tab is
 * visible. For client components rendered inside a `Scene`; outside one it is
 * always `false`.
 */
export function useSceneActive(): boolean {
  return useContext(SceneActiveContext);
}

const SceneRatioContext = createContext<
  ((ratio: string | null) => void) | null
>(null);

/**
 * Lets a visual rendered inside a `Scene` replace the stage's `aspect-ratio`
 * at runtime, e.g. a narrower, taller ratio for a mobile-only re-flowed
 * layout. Call it on every render with the desired ratio, or `null` to
 * release the override; a `null` override leaves the `Scene`'s own `ratio`
 * prop in effect, so a visual that never overrides it (or a `Scene` with no
 * such child) renders exactly as before. Outside a `Scene` this is a no-op.
 */
export function useSceneRatio(override: string | null): void {
  const setOverride = useContext(SceneRatioContext);

  useEffect(() => {
    setOverride?.(override);
    return () => setOverride?.(null);
  }, [setOverride, override]);
}

interface SceneProps {
  /** CSS `aspect-ratio` value for the box, e.g. "16 / 9" or "4 / 3". */
  readonly ratio?: string;
  /** Accessible name; the scene is decorative (`aria-hidden`) without one. */
  readonly label?: string;
  readonly className?: string;
  /**
   * Plain nodes, or a render function called with `active`, true only while
   * the scene is in the viewport and the tab is visible. `active` stays
   * `false` on the server and on the first client frame.
   */
  readonly children: ReactNode | ((active: boolean) => ReactNode);
}

/**
 * Fixed-ratio, overflow-hidden stage for an animated graphic. It reserves its
 * own height, so the page never shifts while the visual loads, and gates
 * motion with an IntersectionObserver plus the page visibility state. Reduced
 * motion stays the visual's own call: read `useReducedMotionPreference` and
 * render a still frame.
 */
export function Scene({
  ratio = "16 / 9",
  label,
  className,
  children,
}: SceneProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [inView, setInView] = useState(false);
  const [visible, setVisible] = useState(true);
  const [ratioOverride, setRatioOverride] = useState<string | null>(null);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          setInView(entry.isIntersecting);
        }
      },
      { rootMargin: "10% 0px", threshold: 0.15 },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const onChange = () => setVisible(document.visibilityState === "visible");
    onChange();
    document.addEventListener("visibilitychange", onChange);
    return () => document.removeEventListener("visibilitychange", onChange);
  }, []);

  const active = inView && visible;

  return (
    <div
      ref={ref}
      style={{ aspectRatio: ratioOverride ?? ratio }}
      aria-hidden={label ? undefined : true}
      aria-label={label}
      role={label ? "img" : undefined}
      className={`relative w-full overflow-hidden ${className ?? ""}`.trim()}
    >
      <SceneActiveContext.Provider value={active}>
        <SceneRatioContext.Provider value={setRatioOverride}>
          {typeof children === "function" ? children(active) : children}
        </SceneRatioContext.Provider>
      </SceneActiveContext.Provider>
    </div>
  );
}
