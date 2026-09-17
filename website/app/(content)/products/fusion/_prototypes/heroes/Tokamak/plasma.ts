import { nearFactor, project, torusPoint, type Camera } from "./geometry";
import type { TorusParams } from "./layout";

interface Pt {
  readonly x: number;
  readonly y: number;
}

/** One projected point of a streak/filament/spiral, with its near/far factor baked in. */
export interface ShadedPoint extends Pt {
  readonly near: number;
}

export interface Streak {
  readonly theta0: number;
  readonly arc: number;
  /** Tube cross-section angle: 0 = outer limb, PI = inner limb, +-PI/2 = top/bottom. */
  readonly phi: number;
  /** rad/s; only applied when a streak orbits live. */
  readonly speed: number;
  readonly width: number;
  readonly alpha: number;
  readonly flickerSpeed: number;
  readonly flickerPhase: number;
}

/**
 * `theta0`/`phi` are passed in (usually from a stratified grid over the
 * torus' (theta, phi) domain, see `index.tsx`) so hundreds of streaks
 * spread evenly across the whole band instead of a purely random draw
 * letting some radius bands go empty while others accumulate enough
 * overlapping arcs to read as a complete, flat ring -- the same "annulus"
 * failure the ticket warns against, just arrived at in 3D instead of 2D.
 */
export function createStreak(
  rand: () => number,
  theta0: number,
  phi: number,
): Streak {
  return {
    theta0,
    arc: 0.045 + rand() * 0.11,
    phi,
    speed: (rand() < 0.5 ? -1 : 1) * (0.05 + rand() * 0.07),
    width: 0.6 + rand() * 1,
    alpha: 0.4 + rand() * 0.45,
    flickerSpeed: 0.3 + rand() * 0.6,
    flickerPhase: rand() * Math.PI * 2,
  };
}

/** Projects a streak's short arc through the torus + camera, one shaded point per sample. */
export function projectStreak(
  streak: Streak,
  torus: TorusParams,
  camera: Camera,
  timeSec: number,
  animate: boolean,
  samples = 6,
): ShadedPoint[] {
  const theta = streak.theta0 + (animate ? streak.speed * timeSec : 0);
  const pts: ShadedPoint[] = [];
  for (let i = 0; i < samples; i++) {
    const tt = theta + (i / (samples - 1) - 0.5) * streak.arc;
    const world = torusPoint(
      torus.R,
      torus.a,
      tt,
      streak.phi,
      torus.y,
      torus.z,
    );
    const proj = project(world, camera);
    pts.push({ x: proj.x, y: proj.y, near: nearFactor(proj.scale, camera) });
  }
  return pts;
}

/**
 * The helical filament twisting inside the band: one continuous loop around
 * the full ring, its tube angle `phi` winding `windCount` times as `theta`
 * sweeps 0..2*PI, so it reads as a helix threaded through the streak band
 * rather than a flat sine drawn in 2D. `twistPhase` advances over time for
 * the twisting motion.
 */
export function projectHelix(
  torus: TorusParams,
  camera: Camera,
  twistPhase: number,
  windCount = 2.4,
  ampl = 0.62,
  samples = 96,
): ShadedPoint[] {
  const pts: ShadedPoint[] = [];
  for (let i = 0; i <= samples; i++) {
    const theta = (i / samples) * Math.PI * 2;
    const phi = twistPhase + theta * windCount;
    const world = torusPoint(
      torus.R * ampl + torus.a * 0.15,
      torus.a * 0.55,
      theta,
      phi,
      torus.y,
      torus.z,
    );
    const proj = project(world, camera);
    pts.push({ x: proj.x, y: proj.y, near: nearFactor(proj.scale, camera) });
  }
  return pts;
}

/**
 * One of the five service-colour streams spiralling in from the outer
 * chamber and fusing into the ring: radius eases from `outerRadius` down to
 * the torus radius over `loops` turns, low alpha and thin per the palette
 * rule (kept as a small identifying accent, never a saturated fill).
 */
export function projectSpiral(
  torus: TorusParams,
  camera: Camera,
  theta0: number,
  outerRadius: number,
  loops: number,
  samples = 80,
): ShadedPoint[] {
  const pts: ShadedPoint[] = [];
  for (let i = 0; i <= samples; i++) {
    const t = i / samples;
    const theta = theta0 + t * loops * Math.PI * 2;
    // Linear radius decay (an even Archimedean pitch), not eased: the ticket
    // calls for a visible inward spiral, not a large loop that only
    // collapses at the very end.
    const radius = outerRadius + (torus.R - outerRadius) * t;
    const y = torus.y + (1 - t) * torus.a * 2.4 * Math.sin(theta0 * 3 + t * 5);
    const world = {
      x: radius * Math.cos(theta),
      y,
      z: torus.z + radius * Math.sin(theta),
    };
    const proj = project(world, camera);
    pts.push({ x: proj.x, y: proj.y, near: nearFactor(proj.scale, camera) });
  }
  return pts;
}
