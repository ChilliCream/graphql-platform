"use client";

import { useReducedMotion } from "motion/react";
import {
  useCallback,
  useRef,
  useState,
  type CSSProperties,
  type PointerEvent,
  type ReactNode,
} from "react";

/** Faint ivory dot lattice painted across the surface. */
const DOT_BG =
  "radial-gradient(circle, rgba(245,241,234,0.12) 1px, transparent 1.2px)";
/** Brighter teal lattice, revealed only inside the cursor-following halo. */
const HALO_BG =
  "radial-gradient(circle, rgba(94,234,212,0.7) 1px, transparent 1.2px)";

interface DotGridSurfaceProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly id?: string;
}

/**
 * A section surface with a subtle dot-grid pattern and a soft teal "spotlight"
 * that follows the pointer, brightening the dots beneath it. The halo is a
 * masked layer positioned from the `--x`/`--y` custom properties updated on
 * pointer move, so nothing re-renders per frame. Disabled under reduced motion.
 */
export function DotGridSurface({
  children,
  className = "",
  id,
}: DotGridSurfaceProps) {
  const reduced = useReducedMotion();
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const [active, setActive] = useState(false);

  const onMove = useCallback(
    (event: PointerEvent<HTMLDivElement>) => {
      if (reduced) {
        return;
      }
      const el = wrapperRef.current;
      if (!el) {
        return;
      }
      const rect = el.getBoundingClientRect();
      el.style.setProperty("--x", `${event.clientX - rect.left}px`);
      el.style.setProperty("--y", `${event.clientY - rect.top}px`);
      if (!active) {
        setActive(true);
      }
    },
    [active, reduced],
  );

  const onLeave = useCallback(() => setActive(false), []);

  return (
    <div
      id={id}
      ref={wrapperRef}
      onPointerMove={onMove}
      onPointerLeave={onLeave}
      className={`relative isolate overflow-hidden ${className}`}
      style={{ "--x": "50%", "--y": "50%" } as CSSProperties}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          backgroundImage: DOT_BG,
          backgroundSize: "24px 24px",
          backgroundPosition: "0 0",
        }}
      />
      {!reduced ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 transition-opacity duration-300"
          style={{
            backgroundImage: HALO_BG,
            backgroundSize: "24px 24px",
            backgroundPosition: "0 0",
            opacity: active ? 1 : 0,
            WebkitMaskImage:
              "radial-gradient(circle 180px at var(--x) var(--y), #000 0%, rgba(0,0,0,0.6) 40%, transparent 75%)",
            maskImage:
              "radial-gradient(circle 180px at var(--x) var(--y), #000 0%, rgba(0,0,0,0.6) 40%, transparent 75%)",
          }}
        />
      ) : null}
      <div className="relative z-10">{children}</div>
    </div>
  );
}
