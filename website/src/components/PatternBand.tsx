"use client";

import { useReducedMotion } from "motion/react";
import type { CSSProperties, PointerEvent, ReactNode } from "react";
import { useCallback, useRef, useState } from "react";

const DOT_BG =
  "radial-gradient(circle, rgba(245,241,234,0.12) 1px, transparent 1.2px)";

const HALO_BG =
  "radial-gradient(circle, rgba(94,234,212,0.7) 1px, transparent 1.2px)";

const GRID_BG =
  "linear-gradient(rgba(245,241,234,1) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,1) 1px, transparent 1px)";

const GRID_MASK =
  "radial-gradient(80% 130% at 50% 45%, #000 35%, transparent 82%)";

const HALO_MASK =
  "radial-gradient(circle 180px at var(--x) var(--y), #000 0%, rgba(0,0,0,0.6) 40%, transparent 75%)";

/** Vertical feather so a band's texture and tint fade into the page at the top
 *  and bottom edges instead of ending on a hard line (see the `blend` prop). */
const EDGE_FADE =
  "linear-gradient(to bottom, transparent 0%, #000 14%, #000 86%, transparent 100%)";

/** Soft teal wash under the grid texture. */
const TEAL_GLOW =
  "radial-gradient(60% 90% at 50% 40%, rgba(94,234,212,0.08), transparent 65%)";

/** The agentic-coding beamlines: one spectrum beam through the center with
 *  teal/cyan beams either side. This is the "lines" texture, kept identical to
 *  the agentic-coding page's own background. The gradients are percentage-based
 *  so they scale to the band's height. */
const SPECTRUM_BEAM =
  "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.1) 6%, rgba(124,146,198,0.09) 38%, rgba(240,120,106,0.06) 60%, rgba(240,120,106,0) 78%)";

const CYAN_BEAM =
  "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.1) 8%, rgba(22,185,228,0.05) 40%, transparent 75%)";

const TEAL_BEAM =
  "linear-gradient(180deg, rgba(94,234,212,0) 0%, rgba(94,234,212,0.1) 8%, rgba(94,234,212,0.04) 40%, transparent 75%)";

interface BeamSpec {
  readonly left: string;
  readonly width: string;
  readonly background: string;
}

const BEAMS: readonly BeamSpec[] = [
  { left: "8%", width: "1.5px", background: TEAL_BEAM },
  { left: "24%", width: "1.5px", background: CYAN_BEAM },
  { left: "50%", width: "2.5px", background: SPECTRUM_BEAM },
  { left: "72%", width: "1.5px", background: CYAN_BEAM },
  { left: "92%", width: "1.5px", background: TEAL_BEAM },
];

/** A single white streak falls down one beam at a time: the animation walks the
 *  drop through three legs (the 24%, 50%, and 92% beams), so at most one drop is
 *  ever visible. The drop stays invisible (opacity 0) unless the animation runs,
 *  so with prefers-reduced-motion the beams simply stay still. */
const DROP_CSS = `
@media (prefers-reduced-motion: no-preference) {
  .beam-drop { animation: beam-drop-fall 15s linear infinite; }
}
@keyframes beam-drop-fall {
  0% { left: 24%; transform: translateY(-80px); opacity: 0; }
  2% { opacity: 0.45; }
  24% { opacity: 0.45; }
  28% { left: 24%; transform: translateY(1900px); opacity: 0; }
  33% { left: 50%; transform: translateY(-80px); opacity: 0; }
  35% { opacity: 0.45; }
  57% { opacity: 0.45; }
  61% { left: 50%; transform: translateY(1900px); opacity: 0; }
  66% { left: 92%; transform: translateY(-80px); opacity: 0; }
  68% { opacity: 0.45; }
  90% { opacity: 0.45; }
  94% { left: 92%; transform: translateY(1900px); opacity: 0; }
  100% { left: 92%; transform: translateY(1900px); opacity: 0; }
}
`;

type BandPattern = "dots" | "grid" | "lines";

interface PatternBandProps {
  /** Which signature texture to paint behind the band. */
  readonly pattern: BandPattern;
  readonly children: ReactNode;
  /** Border width and vertical padding are the caller's concern, e.g.
   *  `"border-b py-16 sm:py-24"` for a hero or `"pb-16 sm:pb-24"` when wrapping
   *  a section that already carries its own top padding. */
  readonly className?: string;
  readonly id?: string;
  /**
   * Pull the band flush under the sticky header by cancelling the (content)
   * layout's `py-8` top padding. Use on the first section of a page so the
   * texture meets the header with no plain background between them.
   */
  readonly flush?: boolean;
  /**
   * Re-establish the centered `max-w-7xl` content column for the children
   * (default). Set `false` when wrapping a section that already renders its
   * own centered column, so the band only supplies the full-bleed texture.
   */
  readonly contain?: boolean;
  /**
   * Feather the texture and tint into the page at the top and bottom edges so
   * stacked bands blend instead of meeting on hard lines. Pair with a
   * borderless className.
   */
  readonly blend?: boolean;
}

/**
 * Full-bleed "chapter" band that breaks out of the centered content column to
 * carry a page's signature background texture. `dots` adds a pointer-following
 * teal halo (gated on reduced motion), `lines` carries the agentic beamlines
 * and their falling drop, `grid` is static. With `blend` the whole background
 * feathers into the page at its edges so stacked bands read as one soft flow.
 */
export function PatternBand({
  pattern,
  children,
  className = "",
  id,
  flush = false,
  contain = true,
  blend = false,
}: PatternBandProps) {
  const reduced = useReducedMotion();
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const [active, setActive] = useState(false);
  const tracks = pattern === "dots" && !reduced;

  const onMove = useCallback(
    (event: PointerEvent<HTMLDivElement>) => {
      if (!tracks) {
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
    [tracks, active],
  );

  const onLeave = useCallback(() => setActive(false), []);

  const bandClass = [
    "border-cc-card-border/50 relative left-1/2 isolate w-screen -translate-x-1/2 overflow-hidden",
    flush ? "-mt-8" : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  const bgMask = blend
    ? { WebkitMaskImage: EDGE_FADE, maskImage: EDGE_FADE }
    : undefined;

  return (
    <div
      id={id}
      ref={wrapperRef}
      onPointerMove={tracks ? onMove : undefined}
      onPointerLeave={tracks ? onLeave : undefined}
      className={bandClass}
      style={
        {
          "--x": "50%",
          "--y": "50%",
        } as CSSProperties
      }
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={bgMask}
      >
        <div className="bg-cc-surface/25 absolute inset-0" />

        {pattern === "dots" && (
          <>
            <div
              className="absolute inset-0"
              style={{
                backgroundImage: DOT_BG,
                backgroundSize: "24px 24px",
                backgroundPosition: "0 0",
              }}
            />
            {!reduced ? (
              <div
                className="absolute inset-0 transition-opacity duration-300"
                style={{
                  backgroundImage: HALO_BG,
                  backgroundSize: "24px 24px",
                  backgroundPosition: "0 0",
                  opacity: active ? 1 : 0,
                  WebkitMaskImage: HALO_MASK,
                  maskImage: HALO_MASK,
                }}
              />
            ) : null}
          </>
        )}

        {pattern === "grid" && (
          <>
            <div
              className="absolute inset-0 opacity-[0.06]"
              style={{
                backgroundImage: GRID_BG,
                backgroundSize: "46px 46px",
                WebkitMaskImage: GRID_MASK,
                maskImage: GRID_MASK,
              }}
            />
            <div
              className="absolute inset-0"
              style={{ background: TEAL_GLOW }}
            />
          </>
        )}

        {pattern === "lines" && (
          <>
            <style>{DROP_CSS}</style>
            {BEAMS.map((beam) => (
              <div
                key={beam.left}
                className="absolute top-0 h-full"
                style={{
                  left: beam.left,
                  width: beam.width,
                  background: beam.background,
                }}
              />
            ))}
            <div
              className="beam-drop absolute top-0 rounded-full opacity-0"
              style={{
                width: "1.5px",
                height: "60px",
                background:
                  "linear-gradient(180deg, transparent 0%, rgba(245,241,234,0.9) 100%)",
              }}
            />
          </>
        )}
      </div>

      <div
        className={
          contain
            ? "relative z-10 mx-auto max-w-7xl px-5 sm:px-12"
            : "relative z-10"
        }
      >
        {children}
      </div>
    </div>
  );
}
