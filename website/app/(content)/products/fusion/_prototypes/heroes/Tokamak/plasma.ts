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

/** One short dash of a service stream's inward spiral, plus its progress `t` (0 = outer wall, 1 = the ring join) so the caller can fade and thin it out. */
export interface SpiralDash {
  readonly pts: readonly ShadedPoint[];
  readonly t: number;
}

/**
 * One of the five service-colour streams spiralling in from the outer
 * chamber and dissolving into the ring: radius eases from `outerRadius`
 * down to the torus radius over `loops` turns (several real turns, a
 * visible inward pitch, not a single near-flat loop), drawn as a run of
 * short, separated dashes -- never one continuous path, so it reads as a
 * trail of accents feeding into the ring rather than a drawn orbit line
 * (README section 4, planner ruling 187 item 4). Each dash's `t` lets the
 * caller fade its alpha and taper its width toward 0 as it nears the ring.
 */
export function projectSpiralDashes(
  torus: TorusParams,
  camera: Camera,
  theta0: number,
  outerRadius: number,
  loops: number,
  dashCount = 22,
  samplesPerDash = 4,
): SpiralDash[] {
  const dashes: SpiralDash[] = [];
  const dashSpan = 0.5; // fraction of each dash's slot the stroke fills; the rest is gap
  for (let d = 0; d < dashCount; d++) {
    const slot = 1 / dashCount;
    const t0 = d * slot;
    const t1 = t0 + slot * dashSpan;
    const pts: ShadedPoint[] = [];
    for (let s = 0; s < samplesPerDash; s++) {
      const t = t0 + (t1 - t0) * (s / (samplesPerDash - 1));
      const theta = theta0 + t * loops * Math.PI * 2;
      // Linear radius decay (an even Archimedean pitch), not eased: a
      // visible inward spiral over several turns, not a loop that only
      // collapses at the very end.
      const radius = outerRadius + (torus.R - outerRadius) * t;
      const y =
        torus.y + (1 - t) * torus.a * 2.2 * Math.sin(theta0 * 3 + t * 6);
      const world = {
        x: radius * Math.cos(theta),
        y,
        z: torus.z + radius * Math.sin(theta),
      };
      const proj = project(world, camera);
      pts.push({ x: proj.x, y: proj.y, near: nearFactor(proj.scale, camera) });
    }
    dashes.push({ pts, t: (t0 + t1) / 2 });
  }
  return dashes;
}
