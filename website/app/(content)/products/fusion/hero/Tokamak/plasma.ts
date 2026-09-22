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
  /**
   * hc-0-540: per-streak multiplier (0.6-1.6) on the shared orbit angular
   * rate, so the live band doesn't rotate as one rigid ring (fix direction
   * 2's "per-streak variance"). Fixed at 1 (a no-op) unless
   * `CreateStreakOptions.energetic` is set -- see that option's own doc for
   * why: drawing it unconditionally would shift the shared `rand` stream's
   * cursor and change every later streak's `theta0`/`phi` even in the
   * pre-ticket (reduced-motion) build, breaking that build's own pixel
   * parity with the starting commit.
   */
  readonly orbitVariance: number;
  /** hc-0-540: micro-flicker rate in Hz (3-8, fix direction 3), 0 (unused/no-op) unless `energetic`. */
  readonly microFlickerRate: number;
  /** hc-0-540: micro-flicker phase offset, 0 (unused/no-op) unless `energetic`. */
  readonly microFlickerPhase: number;
}

interface CreateStreakOptions {
  /** A sparse outer streak, well off the tube's own radius, at a further-dampened weight -- the reference's loose streaks thinning out above/below the band. */
  readonly stray?: boolean;
  /**
   * hc-0-540: draws the three extra `rand()` samples the energetic-only
   * fields (`orbitVariance`, `microFlickerRate`, `microFlickerPhase`) need,
   * AFTER every field this function drew before this ticket, in the same
   * order, unconditionally. Left `false`/omitted, `createStreak` draws
   * exactly the same `rand()` calls in the same order as the starting
   * commit, byte for byte -- required so the reduced-motion build (see
   * `index.tsx`'s `energetic`) reseeds an IDENTICAL static cache and live
   * pool to the starting commit's, not merely a visually-similar one: this
   * shared `rand` stream is consumed sequentially across every streak
   * `buildScene` creates, so one function drawing a different number of
   * randoms shifts every later streak's own theta0/phi/other fields too.
   */
  readonly energetic?: boolean;
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
  const streak: Streak = {
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
    orbitVariance: 1,
    microFlickerRate: 0,
    microFlickerPhase: 0,
  };
  if (!opts.energetic) {
    return streak;
  }
  return {
    ...streak,
    orbitVariance: 0.6 + rand() * 1.0,
    microFlickerRate: 3 + rand() * 5,
    microFlickerPhase: rand() * Math.PI * 2,
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
/**
 * hc-0-540: cheap deterministic pseudo-random in [0, 1) from an integer
 * seed -- a sine-hash, not a stored RNG stream, because surge/reseed/spark
 * timing has to be a pure function of `timeSec` re-derived fresh every
 * frame (so the schedule is identical however many frames were skipped,
 * and identical between the implementer's and the reviewer's own runs),
 * not advanced from a generator that would need its own persisted state.
 */
export function hashUnit(seed: number): number {
  const x = Math.sin(seed * 12.9898) * 43758.5453;
  return x - Math.floor(x);
}

/** Shortest angular distance between two `theta`s, folded into `[0, PI]`. */
export function angularDelta(a: number, b: number): number {
  let d = (a - b) % (Math.PI * 2);
  if (d > Math.PI) d -= Math.PI * 2;
  if (d < -Math.PI) d += Math.PI * 2;
  return Math.abs(d);
}

/** hc-0-540 fix direction 3: a moving, soft-edged brightened neighbourhood of streaks, never a single point. */
export interface SurgeState {
  readonly active: boolean;
  /** The brightened zone's current centre, in torus `theta`. */
  readonly sweepTheta: number;
  /** Half-width of the brightened zone, in radians. */
  readonly halfWidth: number;
  /** 0..1 attack/sustain/fade envelope at this instant (0 = no surge right now). */
  readonly envelope: number;
}

const SURGE_PERIOD_S = 2.4;
const SURGE_ATTACK_S = 0.15;
const SURGE_SWEEP_S = 1;
const SURGE_FADE_S = 0.4;
const SURGE_QUARTER_TURN = Math.PI / 2;

/**
 * Every `SURGE_PERIOD_S` (within the ticket's 2-4s cadence), a group of
 * neighbouring streaks brightens 1.5-2x and sweeps a quarter turn over
 * about a second, then fades (fix direction 3) -- the reference's brighter
 * bands, never a single dot on a path. One surge "cycle" fires per
 * `SURGE_PERIOD_S` window; its own centre angle and sweep direction are
 * picked by `hashUnit` from the cycle's own index, so the schedule needs no
 * persisted RNG state and is reproducible from `timeSec` alone.
 * `targetStreakCount` (about 10-30, the ticket's own range) sets
 * `halfWidth` as a FRACTION of the full ring (via `totalLive`) so the
 * average number of streaks the sweep actually covers stays in range
 * whether a viewport runs the mobile or desktop live-streak count.
 */
export function computeSurge(
  timeSec: number,
  totalLive: number,
  targetStreakCount = 20,
): SurgeState {
  const activeWindow = SURGE_SWEEP_S + SURGE_FADE_S;
  const fullWidth =
    totalLive > 0 ? (targetStreakCount / totalLive) * Math.PI * 2 : 0;
  const halfWidth = Math.max(0.05, Math.min(Math.PI * 0.5, fullWidth / 2));
  if (totalLive <= 0) {
    return { active: false, sweepTheta: 0, halfWidth, envelope: 0 };
  }
  const cycle = Math.floor(timeSec / SURGE_PERIOD_S);
  const tInCycle = timeSec - cycle * SURGE_PERIOD_S;
  if (tInCycle >= activeWindow) {
    return { active: false, sweepTheta: 0, halfWidth, envelope: 0 };
  }
  const centerTheta = hashUnit(cycle) * Math.PI * 2;
  const direction = hashUnit(cycle + 0.5) < 0.5 ? -1 : 1;
  const sweepP = Math.max(0, Math.min(1, tInCycle / SURGE_SWEEP_S));
  const eased = sweepP * sweepP * (3 - 2 * sweepP); // smoothstep, no hard stop at the sweep's end
  const sweepTheta = centerTheta + direction * SURGE_QUARTER_TURN * eased;
  let envelope: number;
  if (tInCycle < SURGE_ATTACK_S) {
    envelope = tInCycle / SURGE_ATTACK_S;
  } else if (tInCycle < SURGE_SWEEP_S) {
    envelope = 1;
  } else {
    envelope = Math.max(0, 1 - (tInCycle - SURGE_SWEEP_S) / SURGE_FADE_S);
  }
  return { active: true, sweepTheta, halfWidth, envelope };
}

/** 0 outside the surge's brightened zone, up to `surge.envelope` at its centre, quadratic falloff to the edge -- a soft-edged neighbourhood, not a hand-set spike at one theta. */
export function surgeWeight(theta: number, surge: SurgeState): number {
  if (!surge.active || surge.envelope <= 0) {
    return 0;
  }
  const d = angularDelta(theta, surge.sweepTheta);
  if (d >= surge.halfWidth) {
    return 0;
  }
  const t = 1 - d / surge.halfWidth;
  return surge.envelope * t * t;
}

/** hc-0-540 fix direction 4: the twisting filament's current wind and, right at a reseed, its brief forking second thread. */
export interface FilamentState {
  readonly twistPhase: number;
  readonly windCount: number;
  readonly fork: {
    readonly twistPhase: number;
    readonly windCount: number;
    /** 0..1, fading out over the fork window. */
    readonly alpha: number;
  } | null;
}

const RESEED_PERIOD_S = 4.5;
const RESEED_FORK_S = 0.6;

/**
 * The filament's twist keeps advancing continuously (`twistPhase`), but
 * every `RESEED_PERIOD_S` its own wind count reseeds to a new value (fix
 * direction 4's "faster twist and reseed"). Right at that moment, a second
 * thread -- the PREVIOUS wind count, phase-offset -- fades out over
 * `RESEED_FORK_S` (`fork`, non-null only in that brief window and only when
 * the reseed actually changed the wind count) so the jump reads as the
 * filament briefly forking and one branch dying out, not a hard cut.
 */
export function computeFilamentState(
  timeSec: number,
  twistPeriodS: number,
): FilamentState {
  const twistPhase = (timeSec / twistPeriodS) * Math.PI * 2;
  const reseedIndex = Math.floor(timeSec / RESEED_PERIOD_S);
  const tSinceReseed = timeSec - reseedIndex * RESEED_PERIOD_S;
  const windCountFor = (i: number) => 2 + Math.floor(hashUnit(i) * 3); // 2, 3 or 4 winds
  const windCount = windCountFor(reseedIndex);
  let fork: FilamentState["fork"] = null;
  if (tSinceReseed < RESEED_FORK_S && reseedIndex > 0) {
    const prevWindCount = windCountFor(reseedIndex - 1);
    if (prevWindCount !== windCount) {
      fork = {
        twistPhase: twistPhase + Math.PI * 0.2,
        windCount: prevWindCount,
        alpha: 1 - tSinceReseed / RESEED_FORK_S,
      };
    }
  }
  return { twistPhase, windCount, fork };
}

/** hc-0-540 fix direction 6: one short-lived spark's own schedule (never re-drawn from `rand` at runtime, only its fixed def). */
export interface SparkDef {
  readonly periodS: number;
  readonly activeS: number;
  readonly theta0: number;
  readonly phi0: number;
  readonly direction: number;
  readonly phaseOffset: number;
}

/**
 * A small fixed pool of sparks -- short-lived motes flung tangentially off
 * the band, fading over 0.5-1s (fix direction 6) -- each on its own cycle
 * (`periodS`) so only a few are ever visible at once, staggered by
 * `phaseOffset`. Built once per `buildScene` from the same shared `rand`
 * stream as the streaks (only when `index.tsx`'s `energetic` is true), so
 * it reseeds together with everything else on a real layout change and
 * never runs when the reduced-motion build has no use for it.
 */
export function createSparkDefs(rand: () => number, count: number): SparkDef[] {
  const defs: SparkDef[] = [];
  for (let i = 0; i < count; i++) {
    defs.push({
      periodS: 2.5 + rand() * 2.5,
      activeS: 0.5 + rand() * 0.5,
      theta0: rand() * Math.PI * 2,
      phi0: rand() * Math.PI * 2,
      direction: rand() < 0.5 ? -1 : 1,
      phaseOffset: rand() * 10,
    });
  }
  return defs;
}

/**
 * One spark's projected short tail at `timeSec`, or `null` outside its
 * active window. A trailing run of points (never a single dot) whose tube
 * radius grows past the band's own tube radius as it flings outward and
 * whose brightness rises then fades smoothly over its `activeS` window --
 * never a marker riding the ring's own orbit path: it moves tangentially a
 * short distance and radially outward, on its own short-lived schedule,
 * unrelated to the band's shared orbit phase.
 */
export function projectSpark(
  spark: SparkDef,
  torus: TorusParams,
  camera: Camera,
  timeSec: number,
): { readonly pts: ShadedPoint[]; readonly envelope: number } | null {
  const t = timeSec + spark.phaseOffset;
  const tc = t - Math.floor(t / spark.periodS) * spark.periodS;
  if (tc >= spark.activeS) {
    return null;
  }
  const p = tc / spark.activeS;
  const envelope = Math.sin(p * Math.PI);
  if (envelope <= 0.02) {
    return null;
  }
  const samples = 4;
  const pts: ShadedPoint[] = [];
  for (let i = 0; i < samples; i++) {
    const pp = Math.max(0, p - (i / (samples - 1)) * 0.12);
    const theta = spark.theta0 + spark.direction * pp * 0.5;
    const tubeScale = 1 + pp * 0.6;
    const world = torusPoint(
      torus.R,
      torus.a * tubeScale,
      theta,
      spark.phi0,
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
  return { pts, envelope };
}

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
