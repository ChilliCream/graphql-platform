export interface FilamentPoint {
  readonly x: number;
  readonly y: number;
}

export interface FilamentPath {
  readonly points: readonly FilamentPoint[];
  /** Centreline stroke width in CSS px, 1.5-2.5 for a main strand. */
  readonly width: number;
  readonly alpha: number;
}

/**
 * One branching lightning-like strand: a main polyline plus an optional
 * short branch. `paths` is regenerated in place by `reseedStrand` every
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

/**
 * Random walk from `(x0, y0)` toward `core`, wandering more than crawling.
 * The step length scales with `sphere.radius` (about 3-7% of it per step)
 * and the walk stops early if it strays more than 1.25x the radius from the
 * sphere's centre, so no strand can wander off its own sphere regardless of
 * viewport size.
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

function buildPaths(
  rand: () => number,
  sphere: Sphere,
  core: Vec,
): FilamentPath[] {
  // Dense near the shell and sparse inside: start points cluster around
  // 0.62-1.04x the radius rather than filling the disc evenly.
  const startAngle = rand() * Math.PI * 2;
  const startFactor = 0.62 + rand() * 0.42;
  const x0 = sphere.x + Math.cos(startAngle) * sphere.radius * startFactor;
  const y0 = sphere.y + Math.sin(startAngle) * sphere.radius * startFactor;
  const mainSteps = 12 + Math.floor(rand() * 10);
  const main = walkPolyline(rand, x0, y0, core, mainSteps, sphere);
  const paths: FilamentPath[] = [
    { points: main, width: 1.5 + rand(), alpha: 0.72 + rand() * 0.28 },
  ];

  if (rand() < 0.4 && main.length > 6) {
    const branchFrom = main[3 + Math.floor(rand() * (main.length - 6))];
    const branchSteps = 5 + Math.floor(rand() * 6);
    const branch = walkPolyline(
      rand,
      branchFrom.x,
      branchFrom.y,
      core,
      branchSteps,
      sphere,
    );
    paths.push({
      points: branch,
      width: 1 + rand() * 0.7,
      alpha: 0.35 + rand() * 0.3,
    });
  }
  return paths;
}

/** Creates one filament strand (a main polyline plus an optional branch). */
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
