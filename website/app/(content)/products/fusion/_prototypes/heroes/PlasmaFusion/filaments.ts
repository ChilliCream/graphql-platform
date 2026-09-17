export interface FilamentPoint {
  readonly x: number;
  readonly y: number;
}

export interface FilamentPath {
  readonly points: readonly FilamentPoint[];
  /** Centreline stroke width in CSS px: 1-1.5 for a main strand, 0.7-1 for a branch. */
  readonly width: number;
  readonly alpha: number;
  /**
   * True when this run sits mostly on the sphere's back hemisphere (pz < 0,
   * away from the viewer). `width`/`alpha` already have the back-hemisphere
   * dimming/thinning baked in (ticket hc-0-wrc.5: "about 0.3-0.4 of the
   * front alpha and slightly thinner"); this flag is exposed only so paint
   * code can tell front from back if it ever needs to (it currently does
   * not -- both layers just draw whatever `width`/`alpha` say).
   */
  readonly back: boolean;
  /**
   * 0..1: how much this run faces the fusion seam/core. Drives the warm
   * tint bleed from the halo onto the hemisphere facing the core (ticket
   * hc-0-wrc.5 fix direction); 0 is pure shell cyan, 1 is fully warm.
   */
  readonly warm: number;
}

/**
 * One strand of the shell web: a main path that walks the sphere's surface
 * in 3D plus 1-2 shorter branches (and sometimes a sparser inner branch
 * toward the core). `paths` is regenerated in place by `reseedStrand` every
 * `reseedInterval` ms so the strand "crawls" without every strand
 * regenerating at once; `flickerPhase`/`flickerSpeed` drive its brightness
 * independent of its shape.
 */
export interface Strand {
  paths: FilamentPath[];
  readonly flickerPhase: number;
  readonly flickerSpeed: number;
  readonly reseedInterval: number;
  nextReseedAt: number;
}

interface Vec2 {
  readonly x: number;
  readonly y: number;
}

interface Vec3 {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

interface Sphere extends Vec2 {
  readonly radius: number;
}

function clamp01(value: number): number {
  return value < 0 ? 0 : value > 1 ? 1 : value;
}

function normalize2(v: Vec2): Vec2 {
  const len = Math.hypot(v.x, v.y) || 1;
  return { x: v.x / len, y: v.y / len };
}

function normalize3(v: Vec3): Vec3 {
  const len = Math.hypot(v.x, v.y, v.z) || 1;
  return { x: v.x / len, y: v.y / len, z: v.z / len };
}

function cross3(a: Vec3, b: Vec3): Vec3 {
  return {
    x: a.y * b.z - a.z * b.y,
    y: a.z * b.x - a.x * b.z,
    z: a.x * b.y - a.y * b.x,
  };
}

/**
 * Uniform-random point on the unit sphere (equal-area sampling: `z` uniform
 * in [-1, 1], angle uniform around it -- Archimedes' hat-box theorem). This
 * is the seed for a strand's shell walk, and it is also what makes the
 * limb-brightening fall out of the projection for free: orthographically
 * projecting a uniform-on-the-sphere point cloud onto the disc concentrates
 * points toward the rim (the area element per unit disc radius grows as
 * `1/sqrt(1 - r^2)`), with no explicit density curve to hand-tune and no
 * radius the interior is excluded from (ticket hc-0-wrc.5 fix direction).
 */
function randomOnSphere(rand: () => number): Vec3 {
  const z = rand() * 2 - 1;
  const phi = rand() * Math.PI * 2;
  const s = Math.sqrt(Math.max(0, 1 - z * z));
  return { x: Math.cos(phi) * s, y: Math.sin(phi) * s, z };
}

/** Any unit vector orthogonal to `p`, used as the walk's starting tangent. */
function anyTangent(p: Vec3): Vec3 {
  const reference: Vec3 =
    Math.abs(p.z) < 0.9 ? { x: 0, y: 0, z: 1 } : { x: 1, y: 0, z: 0 };
  return normalize3(cross3(reference, p));
}

interface Walk3State {
  readonly p: Vec3;
  readonly t: Vec3;
}

/**
 * One geodesic step on the unit sphere: the heading `t` first wanders by
 * `turn` radians within the tangent plane at `p` (rotating around `p`
 * itself, using the frame's second tangent `b = p x t`), then the walker
 * moves `step` radians along that new heading, which -- because `p` and the
 * heading are always kept orthonormal -- both advances the position and
 * updates the heading to stay tangent at the new position (a great-circle
 * rotation of the `(p, t)` frame by `step` about `b`). This is the 3D
 * analogue of the old 2D shell walk's `angle += direction * angleStep +
 * jitter`: the same wander-with-persistent-direction shape, just carried
 * out as a rotation on the sphere instead of an angle around a circle.
 */
function stepWalk3(state: Walk3State, step: number, turn: number): Walk3State {
  const b = cross3(state.p, state.t);
  const cosTurn = Math.cos(turn);
  const sinTurn = Math.sin(turn);
  const heading = normalize3({
    x: state.t.x * cosTurn + b.x * sinTurn,
    y: state.t.y * cosTurn + b.y * sinTurn,
    z: state.t.z * cosTurn + b.z * sinTurn,
  });
  const cosStep = Math.cos(step);
  const sinStep = Math.sin(step);
  const p = normalize3({
    x: state.p.x * cosStep + heading.x * sinStep,
    y: state.p.y * cosStep + heading.y * sinStep,
    z: state.p.z * cosStep + heading.z * sinStep,
  });
  const t = normalize3({
    x: heading.x * cosStep - state.p.x * sinStep,
    y: heading.y * cosStep - state.p.y * sinStep,
    z: heading.z * cosStep - state.p.z * sinStep,
  });
  return { p, t };
}

const STEP_ANGLE_MIN = 0.02;
const STEP_ANGLE_RANGE = 0.03;

/**
 * Random walk across the sphere's surface starting at `start`, wandering
 * more than crawling: each step's arc length is 2-5% of a radian (same
 * scale as the old shell walk's 2-5%-of-radius arc length) and the heading
 * wanders around `direction` with jitter, so a strand hugs and wraps the
 * sphere instead of drifting in a smooth great circle or a tight clump.
 */
function walkSurface3D(
  rand: () => number,
  start: Vec3,
  startTangent: Vec3,
  steps: number,
  direction: 1 | -1,
): Vec3[] {
  let state: Walk3State = { p: start, t: startTangent };
  const points: Vec3[] = [state.p];
  for (let i = 0; i < steps; i++) {
    const step = STEP_ANGLE_MIN + rand() * STEP_ANGLE_RANGE;
    const turn = direction * step + (rand() - 0.5) * step * 1.5;
    state = stepWalk3(state, step, turn);
    points.push(state.p);
  }
  return points;
}

/** Orthographic projection (ticket hc-0-wrc.5: `x = cx + r*px, y = cy + r*py`). */
function project(sphere: Sphere, p: Vec3): FilamentPoint {
  return {
    x: sphere.x + p.x * sphere.radius,
    y: sphere.y + p.y * sphere.radius,
  };
}

/**
 * Builds one drawable run: projects the 3D points to screen space and bakes
 * in the depth treatment -- back-hemisphere runs (`back`, decided by the
 * run's own `pz` sign) get `width * 0.85` and `alpha * backFactor` (about
 * 0.3-0.4 of the front alpha, per the fix direction) -- plus the average
 * `warm` (how much the run faces the fusion seam), so paint code can just
 * draw `width`/`alpha` as given and only needs `warm` for colour.
 */
function makeRunPath(
  run: readonly Vec3[],
  back: boolean,
  sphere: Sphere,
  coreDir: Vec2,
  width: number,
  alpha: number,
  backFactor: number,
): FilamentPath {
  let warmSum = 0;
  for (const p of run) {
    warmSum += Math.max(0, p.x * coreDir.x + p.y * coreDir.y);
  }
  return {
    points: run.map((p) => project(sphere, p)),
    width: back ? width * 0.85 : width,
    alpha: back ? alpha * backFactor : alpha,
    back,
    warm: clamp01(warmSum / run.length),
  };
}

/**
 * Splits a 3D walk into one or more `FilamentPath`s at every front/back
 * hemisphere crossing (a single `ctx.stroke()` call can only have one
 * colour/width, so a walk that crosses the limb needs a fresh run on each
 * side); each split shares its boundary point with its neighbour so the
 * strand has no visible gap where it crosses.
 */
function toPaths(
  points: readonly Vec3[],
  sphere: Sphere,
  coreDir: Vec2,
  width: number,
  alpha: number,
  backFactor: number,
): FilamentPath[] {
  const out: FilamentPath[] = [];
  if (points.length < 2) {
    return out;
  }
  let runStart = 0;
  let runBack = points[0].z < 0;
  for (let i = 1; i < points.length; i++) {
    const back = points[i].z < 0;
    if (back !== runBack) {
      out.push(
        makeRunPath(
          points.slice(runStart, i + 1),
          runBack,
          sphere,
          coreDir,
          width,
          alpha,
          backFactor,
        ),
      );
      runStart = i;
      runBack = back;
    }
  }
  if (points.length - runStart >= 2) {
    out.push(
      makeRunPath(
        points.slice(runStart),
        runBack,
        sphere,
        coreDir,
        width,
        alpha,
        backFactor,
      ),
    );
  }
  return out;
}

/**
 * Random walk from `(x0, y0)` toward `core`, wandering more than crawling,
 * in screen space (not constrained to the sphere's surface). The step
 * length scales with `sphere.radius` (about 3-7% of it per step) and the
 * walk stops early if it strays more than 1.25x the radius from the
 * sphere's centre. Used only for the sparser inner branches that reach from
 * the shell toward the fusion core -- a plasma stream leaving the shell, not
 * a filament crawling on it, so it is not part of the 3D surface walk.
 */
function walkPolyline(
  rand: () => number,
  x0: number,
  y0: number,
  core: Vec2,
  steps: number,
  sphere: Sphere,
): FilamentPoint[] {
  let x = x0;
  let y = y0;
  let angle = Math.atan2(core.y - y0, core.x - x0) + (rand() - 0.5) * 2.4;
  const stepLen = sphere.radius * (0.03 + rand() * 0.04);
  const maxDist = sphere.radius * 1.25;
  const points: FilamentPoint[] = [{ x, y }];
  for (let i = 0; i < steps; i++) {
    const toCore = Math.atan2(core.y - y, core.x - x);
    angle += (rand() - 0.5) * 1.0;
    // Mostly wander, nudged toward the core so strands thin out and
    // converge near the seam instead of drifting away from the sphere.
    angle = angle * 0.82 + toCore * 0.18;
    const nx = x + Math.cos(angle) * stepLen;
    const ny = y + Math.sin(angle) * stepLen;
    const dx = nx - sphere.x;
    const dy = ny - sphere.y;
    if (dx * dx + dy * dy > maxDist * maxDist) {
      break;
    }
    x = nx;
    y = ny;
    points.push({ x, y });
  }
  return points;
}

function buildPaths(
  rand: () => number,
  sphere: Sphere,
  core: Vec2,
): FilamentPath[] {
  const coreDir = normalize2({ x: core.x - sphere.x, y: core.y - sphere.y });
  const paths: FilamentPath[] = [];

  // Main strand: seeded uniformly on the whole sphere (not just the visible
  // face -- the back hemisphere still shows through, dimmer) and walked in
  // 3D, then projected. See `randomOnSphere` for why this alone gives a
  // disc that is never empty and gets smoothly denser toward the limb.
  const seed = randomOnSphere(rand);
  const mainSteps = 20 + Math.floor(rand() * 21);
  const mainDirection: 1 | -1 = rand() < 0.5 ? 1 : -1;
  const main3 = walkSurface3D(
    rand,
    seed,
    anyTangent(seed),
    mainSteps,
    mainDirection,
  );
  const mainWidth = 1 + rand() * 0.5;
  const mainAlpha = 0.72 + rand() * 0.28;
  const mainBackFactor = 0.3 + rand() * 0.1;
  paths.push(
    ...toPaths(main3, sphere, coreDir, mainWidth, mainAlpha, mainBackFactor),
  );

  // 1-2 shorter branches that peel off the main strand at one of its points
  // and keep crawling the surface in their own direction (often against the
  // main strand's), so the web reads as woven rather than a single line.
  const branchCount = 1 + (rand() < 0.5 ? 1 : 0);
  for (let b = 0; b < branchCount && main3.length > 6; b++) {
    const from = main3[3 + Math.floor(rand() * (main3.length - 4))];
    const branchSteps = 8 + Math.floor(rand() * 10);
    const branchDirection: 1 | -1 = rand() < 0.5 ? 1 : -1;
    const branch3 = walkSurface3D(
      rand,
      from,
      anyTangent(from),
      branchSteps,
      branchDirection,
    );
    const branchWidth = 0.7 + rand() * 0.3;
    const branchAlpha = 0.35 + rand() * 0.3;
    const branchBackFactor = 0.3 + rand() * 0.1;
    paths.push(
      ...toPaths(
        branch3,
        sphere,
        coreDir,
        branchWidth,
        branchAlpha,
        branchBackFactor,
      ),
    );
  }

  // A sparser subset (about 1 in 4 strands) also sends a thin branch inward
  // from the shell toward the core, so the web is not confined only to the
  // surface -- it thins out toward the interior instead of stopping dead.
  if (rand() < 0.25 && main3.length > 4) {
    const from3 = main3[Math.floor(rand() * main3.length)];
    const fromPoint = project(sphere, from3);
    const innerSteps = 8 + Math.floor(rand() * 8);
    const innerPoints = walkPolyline(
      rand,
      fromPoint.x,
      fromPoint.y,
      core,
      innerSteps,
      sphere,
    );
    const innerBack = from3.z < 0;
    const innerWidth = 0.7 + rand() * 0.3;
    const innerAlpha = 0.3 + rand() * 0.25;
    const innerBackFactor = 0.3 + rand() * 0.1;
    let warmSum = 0;
    for (const p of innerPoints) {
      warmSum += Math.max(
        0,
        ((p.x - sphere.x) * coreDir.x + (p.y - sphere.y) * coreDir.y) /
          sphere.radius,
      );
    }
    paths.push({
      points: innerPoints,
      width: innerBack ? innerWidth * 0.85 : innerWidth,
      alpha: innerBack ? innerAlpha * innerBackFactor : innerAlpha,
      back: innerBack,
      warm: clamp01(warmSum / innerPoints.length),
    });
  }

  return paths;
}

/** Creates one filament strand (a shell-hugging main polyline plus branches). */
export function createStrand(
  sphere: Sphere,
  core: Vec2,
  rand: () => number,
): Strand {
  return {
    paths: buildPaths(rand, sphere, core),
    flickerPhase: rand() * Math.PI * 2,
    flickerSpeed: 0.5 + rand() * 1.1,
    reseedInterval: 80 + rand() * 120,
    nextReseedAt: rand() * 200,
  };
}

/** Regenerates a strand's shape in place; its flicker identity is unchanged. */
export function reseedStrand(
  strand: Strand,
  sphere: Sphere,
  core: Vec2,
  rand: () => number,
): void {
  strand.paths = buildPaths(rand, sphere, core);
}
