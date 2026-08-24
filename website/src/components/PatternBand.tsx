"use client";

import { useReducedMotion } from "motion/react";
import type { CSSProperties, PointerEvent, ReactNode } from "react";
import { useCallback, useRef, useState } from "react";

const DOT_BG = "radial-gradient(circle, rgba(245,241,234,0.12) 1px, transparent 1.2px)";

const HALO_BG = "radial-gradient(circle, rgba(94,234,212,0.7) 1px, transparent 1.2px)";

const GRID_BG =
  "linear-gradient(rgba(245,241,234,1) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,1) 1px, transparent 1px)";

const GRID_MASK = "radial-gradient(80% 130% at 50% 45%, #000 35%, transparent 82%)";

const HALO_MASK = "radial-gradient(circle 180px at var(--x) var(--y), #000 0%, rgba(0,0,0,0.6) 40%, transparent 75%)";

const EDGE_FADE = "linear-gradient(to bottom, transparent 0%, #000 14%, #000 86%, transparent 100%)";

const EDGE_FADE_TOP = "linear-gradient(to bottom, transparent 0%, #000 14%, #000 100%)";

const SHADOW_TOP = "linear-gradient(to bottom, rgba(2,6,16,0.7), rgba(2,6,16,0.25) 55%, transparent)";
const SHADOW_BOTTOM = "linear-gradient(to top, rgba(2,6,16,0.7), rgba(2,6,16,0.25) 55%, transparent)";

const TEAL_GLOW = "radial-gradient(60% 90% at 50% 40%, rgba(94,234,212,0.08), transparent 65%)";

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

type BandPattern = "dots" | "grid" | "lines";

interface PatternBandProps {
  readonly pattern: BandPattern;
  readonly children: ReactNode;
  readonly className?: string;
  readonly id?: string;
  readonly flush?: boolean;
  readonly contain?: boolean;
  readonly blend?: boolean;
  readonly recessed?: boolean;
  readonly recessedBottom?: boolean;
}

export function PatternBand({
  pattern,
  children,
  className = "",
  id,
  flush = false,
  contain = true,
  blend = false,
  recessed = false,
  recessedBottom = false,
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
    recessed ? "border-y" : "",
    recessedBottom ? "border-b shadow-[0_20px_30px_-18px_rgba(2,6,16,0.9)]" : "",
    flush ? "-mt-8" : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  const fade = recessedBottom ? EDGE_FADE_TOP : EDGE_FADE;
  const bgMask = blend ? { WebkitMaskImage: fade, maskImage: fade } : undefined;

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
      <div aria-hidden className="pointer-events-none absolute inset-0" style={bgMask}>
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
            <div
              className="absolute inset-0 transition-opacity duration-300 motion-reduce:hidden"
              style={{
                backgroundImage: HALO_BG,
                backgroundSize: "24px 24px",
                backgroundPosition: "0 0",
                opacity: active ? 1 : 0,
                WebkitMaskImage: HALO_MASK,
                maskImage: HALO_MASK,
              }}
            />
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
            <div className="absolute inset-0" style={{ background: TEAL_GLOW }} />
          </>
        )}

        {pattern === "lines" && (
          <>
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
                background: "linear-gradient(180deg, transparent 0%, rgba(245,241,234,0.9) 100%)",
              }}
            />
          </>
        )}

        {recessed && (
          <>
            <div className="absolute inset-x-0 top-0 h-10" style={{ background: SHADOW_TOP }} />
            <div className="absolute inset-x-0 bottom-0 h-10" style={{ background: SHADOW_BOTTOM }} />
          </>
        )}
      </div>

      <div className={contain ? "relative z-10 mx-auto max-w-7xl px-5 sm:px-12" : "relative z-10"}>{children}</div>
    </div>
  );
}
