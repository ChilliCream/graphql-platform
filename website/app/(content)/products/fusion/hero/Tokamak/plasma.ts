import { nearFactor, project, torusPoint, type Camera } from "./geometry";
import type { TorusParams } from "./sceneLayout";

interface Pt {
  readonly x: number;
  readonly y: number;
}

/** One projected point of a streak/filament/spiral, with its near/far factor baked in. */
export interface ShadedPoint extends Pt {
  readonly near: number;
  /**
   * The world orbit angle (`theta`) this point was projected at. Lets
   * `isFarSide` classify each point of a path individually instead of the
   * whole path by one `theta0` -- a short streak arc straddling the
   * far/near boundary needs its own points split into separate runs (see
   * `splitByPredicate`) rather than being drawn wholesale on one side,
   * which would let its head or tail cross the column instead of being
   * occluded by it.
   */
  readonly theta: number;
}

/**
 * Whether a torus orbit angle sits on the far side of the column, the same
 * convention `chamber.ts` back-face-culls the column by: `sin(theta) > 0`.
 * Used per point (each `ShadedPoint`'s own `theta`, via `splitByPredicate`)
 * to split both streaks and the helix into far runs drawn before the
 * column layer and near runs drawn after it -- real occlusion from actual
 * draw order, not a dimming factor.
 */
export function isFarSide(theta: number): boolean {
  return Math.sin(theta) > 0;
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
  /**
   * 0..1 envelope over `phi`, peaking at the tube's own OUTER mid-line
   * (`phi` 0, where the torus' `y` offset is 0 and the tube faces the
   * camera), falling to near zero at the top/bottom silhouette (`phi`
   * +-PI/2) and dimmed (not silenced) at the INNER mid-line (`phi` PI): the
   * inner limb sits at the tube's smallest radius, closest to the column's
   * own radius, and a full population there would fill in the exact screen
   * area the column's tiles are meant to occlude, regardless of `theta0`.
   * Folded into every projected point's `near` factor
   * (see `projectStreak`) so the band's own density -- not a hand-set 2D
   * fade or a column-width check -- gives it both a feathered edge and a
   * visible gap for the column, without hollowing out the ring's own
   * density.
   */
  readonly weight: number;
  /** Multiplies the torus' tube radius `a` for this streak only; 1 for the band body, 1.3-2.2 for the sparse "stray" population that reads as the reference's loose outer streaks. */
  readonly tubeScale: number;
}

interface CreateStreakOptions {
  /** A sparse outer streak, well off the tube's own radius, at a further-dampened weight -- the reference's loose streaks thinning out above/below the band. */
  readonly stray?: boolean;
}

/**
 * `theta0`/`phi` are passed in (usually from a stratified grid over the
 * torus' (theta, phi) domain, see `index.tsx`) so hundreds of streaks
 * spread evenly across the whole band instead of a purely random draw
 * letting some radius bands go empty while others accumulate enough
 * overlapping arcs to read as a complete, flat annulus.
 */
export function createStreak(
  rand: () => number,
  theta0: number,
  phi: number,
  opts: CreateStreakOptions = {},
): Streak {
  // Peaks at 1 at the outer mid-line (phi 0); the silhouette term alone
  // falls to 0.12 at top/bottom (+-PI/2) AND at the inner mid-line (phi
  // PI), the outer-limb term then dims the inner mid-line further (floor
  // 0.12, same as the silhouette) while leaving the outer mid-line at full
  // strength -- see the `weight` doc.
  const silhouetteEnvelope = 0.12 + 0.88 * Math.cos(phi) ** 2;
  const outerLimbEnvelope = 0.4 + 0.6 * (0.5 + 0.5 * Math.cos(phi));
  const envelope = silhouetteEnvelope * outerLimbEnvelope;
  return {
    theta0,
    // 0.35-0.7 rad (20-40deg): long enough, at this density and with
    // `lighter` compositing, that overlapping arcs stack into a continuous
    // band with a brighter middle line instead of a scattered cloud of
    // short dashes spread across too much of the column's height.
    arc: 0.35 + rand() * 0.35,
    phi,
    speed: (rand() < 0.5 ? -1 : 1) * (0.05 + rand() * 0.07),
    width: 0.6 + rand(),
    alpha: 0.4 + rand() * 0.45,
    flickerSpeed: 0.3 + rand() * 0.6,
    flickerPhase: rand() * Math.PI * 2,
    weight: opts.stray ? envelope * 0.25 : envelope,
    tubeScale: opts.stray ? 1.3 + rand() * 0.9 : 1,
  };
}

/** Projects a streak's short arc through the torus + camera, one shaded point per sample. */
export function projectStreak(
  streak: Streak,
  torus: TorusParams,
  camera: Camera,
  timeSec: number,
  animate: boolean,
  samples = 16,
): ShadedPoint[] {
  const theta = streak.theta0 + (animate ? streak.speed * timeSec : 0);
  const pts: ShadedPoint[] = [];
  for (let i = 0; i < samples; i++) {
    const tt = theta + (i / (samples - 1) - 0.5) * streak.arc;
    const world = torusPoint(
      torus.R,
      torus.a * streak.tubeScale,
      tt,
      streak.phi,
      torus.y,
      torus.z,
    );
    const proj = project(world, camera);
    pts.push({
      x: proj.x,
      y: proj.y,
      near: nearFactor(proj.scale, camera) * streak.weight,
      theta: tt,
    });
  }
  return pts;
}

/**
 * The helical filament twisting inside the band: one continuous loop around
 * the full ring, its tube angle `phi` winding `windCount` times as `theta`
 * sweeps 0..2*PI, so it reads as a helix threaded through the streak band
 * rather than a flat sine drawn in 2D. `twistPhase` advances over time for
 * the twisting motion. Runs at the torus' own major radius (`ampl = 1`) with
 * a tube amplitude under the streak band's own tube radius so it threads
 * through the band rather than swinging outside it as an unrelated loop.
 */
export function projectHelix(
  torus: TorusParams,
  camera: Camera,
  twistPhase: number,
  windCount = 3,
  ampl = 1,
  tubeAmpl = 0.5,
  samples = 128,
): ShadedPoint[] {
  const pts: ShadedPoint[] = [];
  for (let i = 0; i <= samples; i++) {
    const theta = (i / samples) * Math.PI * 2;
    const phi = twistPhase + theta * windCount;
    const world = torusPoint(
      torus.R * ampl,
      torus.a * tubeAmpl,
      theta,
      phi,
      torus.y,
      torus.z,
    );
    const proj = project(world, camera);
    pts.push({
      x: proj.x,
      y: proj.y,
      near: nearFactor(proj.scale, camera),
      theta,
    });
  }
  return pts;
}

/**
 * Per-point far-side dimming for the helix: the filament's own far/near
 * split (see `splitByPredicate` below) already draws its far run before the
 * column layer and its near run after, but the filament's far points
 * falling OUTSIDE the column's own projected width still need this extra
 * dimming (nothing there to occlude them), so each point is tested against
 * `isFarSide` and the column's projected half-width individually.
 */
export function occludeHelixBehindColumn(
  pts: readonly ShadedPoint[],
  camera: Camera,
  columnHalfWidthPx: number,
  factor = 0.3,
): ShadedPoint[] {
  return pts.map((p) =>
    isFarSide(p.theta) && Math.abs(p.x - camera.originX) < columnHalfWidthPx
      ? { ...p, near: p.near * factor }
      : p,
  );
}

/**
 * Splits a per-point-classified path into contiguous runs (e.g. the
 * helix's or a streak's far-side-behind-the-column points vs its near-side
 * points), each boundary point duplicated into both neighbouring runs so
 * the runs still meet with no visible gap. Used for the helix (drawing its
 * far half before the live front streaks and its near half after, so the
 * thread reads as partly hidden by the column and crossed by the front
 * streaks) and, per point rather than by a whole streak's `theta0`, for
 * every projected streak: an arc spanning 20-40 degrees can straddle the
 * far/near boundary, and drawing it wholesale on one side would let its
 * head or tail cross the column instead of being occluded by it.
 */
export function splitByPredicate<T>(
  pts: readonly T[],
  isFar: (p: T) => boolean,
): { readonly far: T[][]; readonly near: T[][] } {
  const far: T[][] = [];
  const near: T[][] = [];
  if (pts.length === 0) {
    return { far, near };
  }
  let current: T[] = [pts[0]];
  let currentFar = isFar(pts[0]);
  for (let i = 1; i < pts.length; i++) {
    const p = pts[i];
    const f = isFar(p);
    if (f !== currentFar) {
      current.push(p);
      (currentFar ? far : near).push(current);
      current = [p];
      currentFar = f;
    } else {
      current.push(p);
    }
  }
  (currentFar ? far : near).push(current);
  return { far, near };
}
