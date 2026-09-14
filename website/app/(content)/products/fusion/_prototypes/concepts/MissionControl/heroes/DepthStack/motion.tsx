"use client";

import { useEffect, useState } from "react";
import type { CSSProperties } from "react";

import { anim } from "../../hooks";
import { MC } from "../../palette";
import type { P3 } from "./scene";
import { lerp3 } from "./scene";

/**
 * Traffic for the Depth Stack hero: the beams that run between the three
 * planes and the request dots that travel along them into depth and back.
 *
 * A beam is a row of dots placed at interpolated points between two panel
 * centres, each with its own `translateZ`, so the perspective of the scene
 * makes the line recede on its own - no rotation maths, no raster.
 *
 * A travelling dot is a full-size layer whose `translate3d` percentages
 * therefore resolve against the stage box; the layer carries the two
 * waypoints as custom properties, and one shared set of keyframes moves it
 * from the first to the second inside its own window of the round trip.
 */

interface Leg {
  readonly name: string;
  /** Window of the round trip this leg occupies, in percent. */
  readonly from: number;
  readonly to: number;
}

/** The four legs of one round trip, in order. */
export const LEGS: Readonly<Record<string, Leg>> = {
  request: { name: "mc-depth-request", from: 2, to: 26 },
  fanOut: { name: "mc-depth-fan-out", from: 30, to: 54 },
  collect: { name: "mc-depth-collect", from: 58, to: 82 },
  answer: { name: "mc-depth-answer", from: 86, to: 98 },
};

const FROM = "translate3d(var(--mc-ax), var(--mc-ay), var(--mc-az))";
const TO = "translate3d(var(--mc-bx), var(--mc-by), var(--mc-bz))";

export const KEYFRAMES = Object.values(LEGS)
  .map(
    (leg) => `
@keyframes ${leg.name} {
  0%, ${leg.from}% { transform: ${FROM}; opacity: 0; }
  ${leg.from + 4}% { opacity: 1; }
  ${leg.to - 4}% { opacity: 1; }
  ${leg.to}%, 100% { transform: ${TO}; opacity: 0; }
}`,
  )
  .join("\n");

/** Where the beam dots sit between the two panels. */
const BEAM = [0.2, 0.38, 0.56, 0.74, 0.9] as const;

interface BeamProps {
  readonly from: P3;
  readonly to: P3;
  readonly color: string;
  readonly lit: boolean;
}

/** The link between two panels, as dots receding into depth. */
export function Beam({ from, to, color, lit }: BeamProps) {
  return (
    <>
      {BEAM.map((t) => {
        const point = lerp3(from, to, t);
        return (
          <span
            key={t}
            style={{
              position: "absolute",
              left: `${point.x}%`,
              top: `${point.y}%`,
              width: 4,
              height: 4,
              marginLeft: -2,
              marginTop: -2,
              borderRadius: "50%",
              background: lit ? color : MC.line,
              opacity: lit ? 0.9 : 0.4,
              transform: `translateZ(${point.z}px)`,
              transition: "background 420ms, opacity 420ms",
            }}
          />
        );
      })}
    </>
  );
}

interface TravellerProps {
  readonly from: P3;
  readonly to: P3;
  readonly color: string;
  readonly leg: Leg;
  /** Position along the leg the rest frame parks on. */
  readonly rest: number;
  readonly running: boolean;
  readonly periodMs: number;
}

/** One request dot, travelling from one plane to the next. */
export function Traveller({
  from,
  to,
  color,
  leg,
  rest,
  running,
  periodMs,
}: TravellerProps) {
  const parked = lerp3(from, to, rest);

  return (
    <div
      style={
        {
          position: "absolute",
          inset: 0,
          "--mc-ax": `${from.x}%`,
          "--mc-ay": `${from.y}%`,
          "--mc-az": `${from.z}px`,
          "--mc-bx": `${to.x}%`,
          "--mc-by": `${to.y}%`,
          "--mc-bz": `${to.z}px`,
          animation: anim(running, `${leg.name} ${periodMs}ms linear infinite`),
          transform: running
            ? undefined
            : `translate3d(${parked.x}%, ${parked.y}%, ${parked.z}px)`,
          opacity: running ? undefined : 0.9,
        } as CSSProperties
      }
    >
      <span
        style={{
          position: "absolute",
          left: 0,
          top: 0,
          width: 9,
          height: 9,
          marginLeft: -4.5,
          marginTop: -4.5,
          borderRadius: "50%",
          background: color,
          boxShadow: `0 0 12px 2px color-mix(in srgb, ${color} 45%, transparent)`,
        }}
      />
    </div>
  );
}

export interface Tilt {
  readonly x: number;
  readonly y: number;
}

const LEVEL: Tilt = { x: 0, y: 0 };

/**
 * Gentle parallax: the pointer swings the whole world by a few degrees on a
 * desktop pointer. It is off whenever the motion gate is closed - so under
 * reduced motion, off screen or on a hidden tab - and off on touch, where
 * there is no pointer to follow.
 */
export function usePointerTilt(enabled: boolean, swing: number): Tilt {
  const [tilt, setTilt] = useState<Tilt>(LEVEL);

  useEffect(() => {
    if (!enabled || swing === 0) return;
    if (!window.matchMedia("(pointer: fine)").matches) return;

    let frame = 0;
    const onMove = (event: PointerEvent) => {
      if (frame !== 0) return;
      frame = window.requestAnimationFrame(() => {
        frame = 0;
        const dx = event.clientX / window.innerWidth - 0.5;
        const dy = event.clientY / window.innerHeight - 0.5;
        setTilt({ x: -dy * swing, y: dx * swing });
      });
    };

    window.addEventListener("pointermove", onMove);
    return () => {
      window.removeEventListener("pointermove", onMove);
      if (frame !== 0) window.cancelAnimationFrame(frame);
    };
  }, [enabled, swing]);

  // Derived, not stored: a closed gate levels the world out without a state
  // write, and the last pointer position is forgotten while it stays closed.
  return enabled && swing !== 0 ? tilt : LEVEL;
}
