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
 * Extra dimming for streak points on the torus' far side, behind the
 * column, on top of the existing near/far `nearFactor` (planner ruling 187
 * item 3: "back streaks pass behind [the column] dimmer or occluded").
 * `theta0` is the streak's own orbit angle (short arcs barely move `theta`,
 * so one value stands in for the whole streak); far-side is `sin(theta0) >
 * 0`, the same convention `chamber.ts` back-face-culls the column by.
 * `columnHalfWidthPx` is the column's own projected half-width at the
 * plasma's height (see `index.tsx`) -- a far-side point within that span of
 * the column's screen-space centre (`camera.originX`) reads as behind it.
 */
export function occludeBehindColumn(
  pts: readonly ShadedPoint[],
  theta0: number,
  camera: Camera,
  columnHalfWidthPx: number,
  factor = 0.35,
): ShadedPoint[] {
  if (Math.sin(theta0) <= 0) {
    return pts as ShadedPoint[];
  }
  return pts.map((p) =>
    Math.abs(p.x - camera.originX) < columnHalfWidthPx
      ? { ...p, near: p.near * factor }
      : p,
  );
}
