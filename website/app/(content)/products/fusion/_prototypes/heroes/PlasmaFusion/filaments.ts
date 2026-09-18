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
   * 0..1: how much this run faces the fusion seam/core. Drives the warm
   * tint bleed from the halo onto the hemisphere facing the core (ticket
   * hc-0-wrc.5 fix direction); 0 is pure shell cyan, 1 is fully warm.
   */
  readonly warm: number;
}

/**
 * One strand of the shell web: a main path that walks the sphere's surface
 * in 3D plus 1-2 shorter branches (sometimes with an acute sub-branch of
 * their own), and sometimes a sparser inner branch toward the core. `paths`
 * is regenerated in place by `reseedStrand` every `reseedInterval` ms so the
 * strand "crawls" without every strand regenerating at once;
 * `flickerPhase`/`flickerSpeed` drive its brightness independent of its
 * shape.
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
 * rotation of the `(p, t)` frame by `step` about `b`). FROZEN by ticket
 * hc-0-wrc.6's fix direction ("keep walkSurface3D's tangent-plane stepping
 * ... exactly"): only the `step`/`turn` values a caller passes in change,
 * never this rotation itself.
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

// Shorter per-step arc length than hc-0-wrc.5's 0.02-0.05 rad: the heading
// jitter below (not the step length) is now what makes a strand jagged, so
// each step only needs to carry the walk forward a little before the next
// kink (ticket hc-0-wrc.6 fix direction: "shorter steps with per-step
// heading jitter").
const STEP_ANGLE_MIN = 0.01;
const STEP_ANGLE_RANGE = 0.02;

// Heading jitter in degrees (converted to radians below), independent of
// step length -- this is what turns a smooth great-circle arc into a
// crackle: the walker's heading swings by roughly a third of a turn every
// step instead of drifting smoothly. `KINK_PROBABILITY` occasionally widens
// that swing further for the sharp single-step kinks the reference shows.
const HEADING_JITTER_MIN_DEG = 25;
const HEADING_JITTER_RANGE_DEG = 15;
const KINK_PROBABILITY = 0.12;
const KINK_MULTIPLIER_MIN = 1.8;
const KINK_MULTIPLIER_RANGE = 1.4;

/**
 * One step's heading jitter in radians, signed: +/-25-40 degrees, widened to
 * +/-45-100 degrees on the ~1-in-8 steps that land a larger kink (ticket
 * hc-0-wrc.6 fix direction).
 */
function headingJitter(rand: () => number): number {
  const deg = HEADING_JITTER_MIN_DEG + rand() * HEADING_JITTER_RANGE_DEG;
  let rad = (deg * Math.PI) / 180;
  if (rand() < KINK_PROBABILITY) {
    rad *= KINK_MULTIPLIER_MIN + rand() * KINK_MULTIPLIER_RANGE;
  }
  return rand() < 0.5 ? -rad : rad;
}

/**
 * Random walk across the sphere's surface starting at `start`: each step's
 * arc length is short (see `STEP_ANGLE_MIN`/`RANGE`) and the heading kinks
 * by `headingJitter` around a small persistent drift toward `direction`, so
 * a strand reads as jagged electric crackle -- random kinks with an
 * occasional sharper one -- while still wrapping the sphere over its full
 * length rather than wandering in a tight clump (ticket hc-0-wrc.6). Step
 * count is roughly double hc-0-wrc.5's for the same `direction`/`steps`
 * scale so a strand's total arc length -- and so the disc's density profile,
 * which is FROZEN -- stays put even though each individual step is shorter.
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
    const turn = direction * step * 1.5 + headingJitter(rand);
    state = stepWalk3(state, step, turn);
    points.push(state.p);
  }
  return points;
}

/**
 * Approximates the walk's tangent direction at `points[i]` from its
 * neighbours (a central difference, projected back into the tangent plane
 * at `points[i]` so it stays a valid heading on the sphere). Used only to
 * spawn a branch at an acute angle to its parent's own heading, at the
 * branch point -- `walkSurface3D` itself does not track a per-point tangent,
 * so this is reconstructed from the points it already returned rather than
 * changing what `walkSurface3D` returns.
 */
function approxTangentAt(points: readonly Vec3[], i: number): Vec3 {
  const a = points[Math.max(0, i - 1)];
  const b = points[Math.min(points.length - 1, i + 1)];
  const p = points[i];
  const d: Vec3 = { x: b.x - a.x, y: b.y - a.y, z: b.z - a.z };
  const radial = d.x * p.x + d.y * p.y + d.z * p.z;
  return normalize3({
    x: d.x - radial * p.x,
    y: d.y - radial * p.y,
    z: d.z - radial * p.z,
  });
}

/**
 * Rotates the unit tangent `t` (at point `p`) by `angle` radians within the
 * tangent plane at `p`, the same rotation `stepWalk3`'s heading update uses.
 * Used to aim a branch's starting tangent at an acute angle off its
 * parent's own heading, instead of the unrelated `anyTangent` direction a
 * fresh walk starts from (ticket hc-0-wrc.6: "child strands at acute
 * angles").
 */
function rotateTangent(p: Vec3, t: Vec3, angle: number): Vec3 {
  const b = cross3(p, t);
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);
  return normalize3({
    x: t.x * cos + b.x * sin,
    y: t.y * cos + b.y * sin,
    z: t.z * cos + b.z * sin,
  });
}

/** Orthographic projection (ticket hc-0-wrc.5: `x = cx + r*px, y = cy + r*py`). */
function project(sphere: Sphere, p: Vec3): FilamentPoint {
  return {
    x: sphere.x + p.x * sphere.radius,
    y: sphere.y + p.y * sphere.radius,
  };
}

// A run of points is drawn as several short overlapping chunks instead of
// one long stroke, so brightness can vary along its length (ticket
// hc-0-wrc.6: "noise-driven brightness modulation along each strand"); each
// chunk shares its first point with the previous chunk's last so the
// strand has no visible gap between them.
const BRIGHTNESS_CHUNK_MIN = 4;
const BRIGHTNESS_CHUNK_RANGE = 3;

function chunkForBrightness(points: readonly Vec3[]): Vec3[][] {
  const chunks: Vec3[][] = [];
  let i = 0;
  // Chunk size is derived from the position (not `rand()`) so it is stable
  // between calls with the same points -- callers may recompute this list.
  let size = BRIGHTNESS_CHUNK_MIN;
  while (i < points.length - 1) {
    const end = Math.min(points.length - 1, i + size);
    chunks.push(points.slice(i, end + 1));
    // Vary the next chunk's length with the walk's own position so chunk
    // boundaries (and so hot spots) do not fall in lockstep across strands.
    const p = points[end];
    size =
      BRIGHTNESS_CHUNK_MIN +
      Math.floor(((p.x + p.y + p.z + 2) / 4) * BRIGHTNESS_CHUNK_RANGE);
    i = end;
  }
  return chunks;
}

interface ChunkStats {
  /** 0..1 average "faces the core" measure, reused as the path's colour tint. */
  readonly warm: number;
  /** Noise-driven brightness multiplier, hot near the core-facing side and the limb. */
  readonly brightnessMul: number;
}

/**
 * Per-chunk stats for one short run of points: the same dot-with-`coreDir`
 * "warm" measure `FilamentPath.warm` always carried, plus a brightness
 * multiplier combining a noise-driven base with two geometric hot-spot terms
 * (ticket hc-0-wrc.6) -- `warm` again for the core-facing hemisphere and
 * `limb` for grazing-angle points near the silhouette (`pz` close to 0, i.e.
 * close to the disc's rim once projected). `rand` is the strand's own
 * build-time RNG, so hot spots are baked into the strand's shape each time
 * it (re)seeds rather than animated.
 */
function chunkStats(
  chunk: readonly Vec3[],
  coreDir: Vec2,
  rand: () => number,
): ChunkStats {
  let warmSum = 0;
  let limbSum = 0;
  for (const p of chunk) {
    warmSum += Math.max(0, p.x * coreDir.x + p.y * coreDir.y);
    limbSum += 1 - Math.min(1, Math.abs(p.z) / 0.6);
  }
  const warmAvg = warmSum / chunk.length;
  const limbAvg = Math.max(0, limbSum / chunk.length);
  const noise = 0.4 + rand() * 1.35;
  return {
    warm: clamp01(warmAvg),
    brightnessMul: (0.5 + warmAvg * 0.55 + limbAvg * 0.65) * noise,
  };
}

/**
 * Builds the drawable runs for one 3D walk: splits at every front/back
 * hemisphere crossing (a single `ctx.stroke()` call can only have one
 * colour/width, so a walk that crosses the limb needs a fresh run on each
 * side, sharing its boundary point with its neighbour so the strand has no
 * visible gap), then further splits each hemisphere-consistent run into
 * brightness chunks. Back-hemisphere runs (`pz < 0`) get `width * 0.85` and
 * `alpha * backFactor` (about 0.3-0.4 of the front alpha, per hc-0-wrc.5's
 * fix direction) before the chunk's own brightness multiplier is applied.
 */
function toPaths(
  points: readonly Vec3[],
  sphere: Sphere,
  coreDir: Vec2,
  width: number,
  alpha: number,
  backFactor: number,
  rand: () => number,
): FilamentPath[] {
  const out: FilamentPath[] = [];
  if (points.length < 2) {
    return out;
  }
  let runStart = 0;
  let runBack = points[0].z < 0;
  const emitRun = (runPoints: readonly Vec3[], back: boolean) => {
    const baseWidth = back ? width * 0.85 : width;
    const baseAlpha = back ? alpha * backFactor : alpha;
    for (const chunk of chunkForBrightness(runPoints)) {
      if (chunk.length < 2) {
        continue;
      }
      const stats = chunkStats(chunk, coreDir, rand);
      out.push({
        points: chunk.map((p) => project(sphere, p)),
        width: baseWidth * clamp01(0.65 + stats.brightnessMul * 0.3),
        alpha: clamp01(baseAlpha * stats.brightnessMul),
        warm: stats.warm,
      });
    }
  };
  for (let i = 1; i < points.length; i++) {
    const back = points[i].z < 0;
    if (back !== runBack) {
      emitRun(points.slice(runStart, i + 1), runBack);
      runStart = i;
      runBack = back;
    }
  }
  if (points.length - runStart >= 2) {
    emitRun(points.slice(runStart), runBack);
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

/**
 * Walks the sphere's surface from `start` and returns both the raw 3D points
 * (for a caller that wants to spawn an acute-angle branch off a specific
 * point) and its `FilamentPath`s.
 */
function walkAndBuild(
  rand: () => number,
  start: Vec3,
  startTangent: Vec3,
  steps: number,
  direction: 1 | -1,
  sphere: Sphere,
  coreDir: Vec2,
  width: number,
  alpha: number,
  backFactor: number,
): { points: Vec3[]; paths: FilamentPath[] } {
  const points = walkSurface3D(rand, start, startTangent, steps, direction);
  const paths = toPaths(
    points,
    sphere,
    coreDir,
    width,
    alpha,
    backFactor,
    rand,
  );
  return { points, paths };
}

/**
 * Spawns a child strand off `parent` at an acute angle, starting somewhere
 * between 40-70% of the way along `parent` (ticket hc-0-wrc.6: "a branch
 * probability spawning child strands at acute angles at 40-70% of the
 * parent's remaining length, thinner"). The child's own length is a
 * fraction of what is left of the parent past that point, so it never
 * outruns the strand it split from.
 */
function spawnBranch(
  rand: () => number,
  parent: readonly Vec3[],
  sphere: Sphere,
  coreDir: Vec2,
  baseWidth: number,
  baseAlpha: number,
): { points: Vec3[]; paths: FilamentPath[] } | null {
  if (parent.length < 6) {
    return null;
  }
  const originFrac = 0.4 + rand() * 0.3;
  const originIdx = Math.min(
    parent.length - 3,
    Math.max(2, Math.floor(originFrac * (parent.length - 1))),
  );
  const remaining = parent.length - originIdx;
  const steps = Math.max(4, Math.floor(remaining * (0.5 + rand() * 0.5)));
  const parentTangent = approxTangentAt(parent, originIdx);
  const acuteDeg = 20 + rand() * 25;
  const acuteRad = (acuteDeg * Math.PI) / 180;
  const signedAngle = rand() < 0.5 ? -acuteRad : acuteRad;
  const branchTangent = rotateTangent(
    parent[originIdx],
    parentTangent,
    signedAngle,
  );
  const direction: 1 | -1 = rand() < 0.5 ? 1 : -1;
  const backFactor = 0.3 + rand() * 0.1;
  return walkAndBuild(
    rand,
    parent[originIdx],
    branchTangent,
    steps,
    direction,
    sphere,
    coreDir,
    baseWidth,
    baseAlpha,
    backFactor,
  );
}

function buildPaths(
  rand: () => number,
  sphere: Sphere,
  core: Vec2,
): FilamentPath[] {
  const coreDir = normalize2({ x: core.x - sphere.x, y: core.y - sphere.y });
  const paths: FilamentPath[] = [];

  // A small set of strands read as brighter main bolts: thicker, closer to
  // full alpha, so their near-white centreline sits over the wide cyan glow
  // (ticket hc-0-wrc.6: "a small set of main bolts drawn thicker with
  // near-white centres"). This changes only the main strand's own
  // width/alpha inputs -- the two-pass glow-then-white-centre draw in
  // paint.ts/index.tsx already scales both from `path.width`/`path.alpha`.
  const isMainBolt = rand() < 0.06;

  // Main strand: seeded uniformly on the whole sphere (not just the visible
  // face -- the back hemisphere still shows through, dimmer) and walked in
  // 3D, then projected. See `randomOnSphere` for why this alone gives a
  // disc that is never empty and gets smoothly denser toward the limb.
  const seed = randomOnSphere(rand);
  const mainSteps = 34 + Math.floor(rand() * 37);
  const mainDirection: 1 | -1 = rand() < 0.5 ? 1 : -1;
  const mainWidth = (1 + rand() * 0.5) * (isMainBolt ? 2 + rand() * 0.6 : 1);
  const mainAlpha = Math.min(
    1,
    (0.72 + rand() * 0.28) * (isMainBolt ? 1.25 : 1),
  );
  const mainBackFactor = 0.3 + rand() * 0.1;
  const main = walkAndBuild(
    rand,
    seed,
    anyTangent(seed),
    mainSteps,
    mainDirection,
    sphere,
    coreDir,
    mainWidth,
    mainAlpha,
    mainBackFactor,
  );
  paths.push(...main.paths);

  // 1-2 acute-angle branches peeling off the main strand at 40-70% of its
  // remaining length, thinner than the main strand, and sometimes one
  // further acute sub-branch off a branch -- the reference's "acute
  // sub-branches splitting off" at more than one order (ticket hc-0-wrc.6).
  const branchCount = 1 + (rand() < 0.5 ? 1 : 0);
  for (let b = 0; b < branchCount; b++) {
    const branchWidth = 0.7 + rand() * 0.3;
    const branchAlpha = 0.35 + rand() * 0.3;
    const branch = spawnBranch(
      rand,
      main.points,
      sphere,
      coreDir,
      branchWidth,
      branchAlpha,
    );
    if (!branch) {
      continue;
    }
    paths.push(...branch.paths);
    if (rand() < 0.3) {
      const subBranch = spawnBranch(
        rand,
        branch.points,
        sphere,
        coreDir,
        branchWidth * 0.75,
        branchAlpha * 0.85,
      );
      if (subBranch) {
        paths.push(...subBranch.paths);
      }
    }
  }

  // A sparser subset (about 1 in 4 strands) also sends a thin branch inward
  // from the shell toward the core, so the web is not confined only to the
  // surface -- it thins out toward the interior instead of stopping dead.
  if (rand() < 0.25 && main.points.length > 4) {
    const from3 = main.points[Math.floor(rand() * main.points.length)];
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
    // A little build-time brightness noise along the inner branch too, for
    // the same "varying along the strand" read (ticket hc-0-wrc.6) -- it
    // walks in screen space rather than on the sphere, so it has no `pz` to
    // drive a limb term from, just the noise term.
    const innerAlpha = (0.3 + rand() * 0.25) * (0.7 + rand() * 0.6);
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
      alpha: clamp01(innerBack ? innerAlpha * innerBackFactor : innerAlpha),
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
