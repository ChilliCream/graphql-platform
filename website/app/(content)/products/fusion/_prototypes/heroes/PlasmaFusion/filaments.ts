export interface FilamentPoint {
  readonly x: number;
  readonly y: number;
}

export interface FilamentPath {
  readonly points: readonly FilamentPoint[];
  /** Centreline stroke width in CSS px: 1-1.5 for a main strand, 0.7-1 for a branch. */
  readonly width: number;
  readonly alpha: number;
}

/**
 * One strand of the shell web: a main polyline that hugs the sphere's
 * surface plus 1-2 short branches (and sometimes a sparser inner branch
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

interface Vec {
  readonly x: number;
  readonly y: number;
}

interface Sphere extends Vec {
  readonly radius: number;
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

/**
 * Random walk from `(x0, y0)` toward `core`, wandering more than crawling.
 * The step length scales with `sphere.radius` (about 3-7% of it per step)
 * and the walk stops early if it strays more than 1.25x the radius from the
 * sphere's centre, so no strand can wander off its own sphere regardless of
 * viewport size. Used only for the sparser inner branches that reach from
 * the shell toward the fusion core.
 */
function walkPolyline(
  rand: () => number,
  x0: number,
  y0: number,
  core: Vec,
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
 * Random walk that hugs the sphere's shell: `radiusFactor` is nudged by a
 * small jitter and clamped to `[0.9, 1.1]` of `sphere.radius`, so every
 * point stays within +/-10% of the surface (planner craft read, ticket
 * hc-0-wrc.2 comment 29 item 2). `angle` random-walks around a persistent
 * per-call `direction` -- a pure symmetric walk barely drifts from its start
 * over 20-40 steps and reads as a tangled clump instead of a strand that
 * "hugs and wraps the circle" (comment 29 item 2's own phrase); the
 * direction gives it somewhere to go while the jitter keeps it jagged
 * instead of a smooth arc. Each step's arc length is 2-5% of the radius,
 * converted to an angle at the strand's current radius.
 */
function walkShell(
  rand: () => number,
  sphere: Sphere,
  startAngle: number,
  startRadiusFactor: number,
  steps: number,
  direction: 1 | -1,
): FilamentPoint[] {
  let angle = startAngle;
  let radiusFactor = startRadiusFactor;
  const point = (): FilamentPoint => ({
    x: sphere.x + Math.cos(angle) * sphere.radius * radiusFactor,
    y: sphere.y + Math.sin(angle) * sphere.radius * radiusFactor,
  });
  const points: FilamentPoint[] = [point()];
  for (let i = 0; i < steps; i++) {
    const arcStep = sphere.radius * (0.02 + rand() * 0.03);
    const angleStep = arcStep / (sphere.radius * radiusFactor);
    angle += direction * angleStep + (rand() - 0.5) * angleStep * 1.5;
    radiusFactor = clamp(radiusFactor + (rand() - 0.5) * 0.02, 0.9, 1.1);
    points.push(point());
  }
  return points;
}

function buildPaths(
  rand: () => number,
  sphere: Sphere,
  core: Vec,
): FilamentPath[] {
  // Density is highest at the shell and at the fusion seam (the side facing
  // the other sphere/the core), lowest in the interior: bias the start angle
  // toward the seam most of the time, otherwise spread it around the whole
  // circumference so the web still wraps the sphere.
  const seamAngle = Math.atan2(core.y - sphere.y, core.x - sphere.x);
  const nearSeam = rand() < 0.55;
  const startAngle = nearSeam
    ? seamAngle + (rand() - 0.5) * 1.6
    : rand() * Math.PI * 2;
  const startRadiusFactor = 0.9 + rand() * 0.2;
  const mainSteps = 20 + Math.floor(rand() * 21);
  const mainDirection: 1 | -1 = rand() < 0.5 ? 1 : -1;
  const main = walkShell(
    rand,
    sphere,
    startAngle,
    startRadiusFactor,
    mainSteps,
    mainDirection,
  );
  const paths: FilamentPath[] = [
    { points: main, width: 1 + rand() * 0.5, alpha: 0.72 + rand() * 0.28 },
  ];

  // 1-2 shorter branches that peel off the main strand and keep crawling
  // the shell (their own direction, often against the main strand's, so the
  // web reads as woven rather than a single loose line).
  const branchCount = 1 + (rand() < 0.5 ? 1 : 0);
  for (let b = 0; b < branchCount && main.length > 6; b++) {
    const from = main[3 + Math.floor(rand() * (main.length - 4))];
    const fromAngle = Math.atan2(from.y - sphere.y, from.x - sphere.x);
    const fromRadiusFactor =
      Math.hypot(from.x - sphere.x, from.y - sphere.y) / sphere.radius;
    const branchSteps = 8 + Math.floor(rand() * 10);
    const branchDirection: 1 | -1 = rand() < 0.5 ? 1 : -1;
    const branch = walkShell(
      rand,
      sphere,
      fromAngle,
      fromRadiusFactor,
      branchSteps,
      branchDirection,
    );
    paths.push({
      points: branch,
      width: 0.7 + rand() * 0.3,
      alpha: 0.35 + rand() * 0.3,
    });
  }

  // A sparser subset (about 1 in 4 strands) also sends a thin branch inward
  // from the shell toward the core, so the web is not confined only to the
  // surface -- it thins out toward the interior instead of stopping dead.
  if (rand() < 0.25 && main.length > 4) {
    const from = main[Math.floor(rand() * main.length)];
    const innerSteps = 8 + Math.floor(rand() * 8);
    const inner = walkPolyline(rand, from.x, from.y, core, innerSteps, sphere);
    paths.push({
      points: inner,
      width: 0.7 + rand() * 0.3,
      alpha: 0.3 + rand() * 0.25,
    });
  }

  return paths;
}

/** Creates one filament strand (a shell-hugging main polyline plus branches). */
export function createStrand(
  sphere: Sphere,
  core: Vec,
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
  core: Vec,
  rand: () => number,
): void {
  strand.paths = buildPaths(rand, sphere, core);
}
