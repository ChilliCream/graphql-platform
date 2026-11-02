/**
 * Minimal 3D projection for the chamber and the plasma torus: a point in
 * world space, a camera with a pitch tilt, and a perspective projection to
 * screen space. Every elliptical ring in this hero (chamber rows, the
 * plasma band, the helical filament, the service-colour spirals) is built
 * as real points in this 3D space and projected through the same `project`
 * function, so the ellipses, the foreshortening and the near/far density
 * fall out of the projection instead of being drawn as flat 2D shapes.
 *
 * World axes: `y` runs along the column (the chamber's and the torus'
 * shared axis), `x`/`z` are the horizontal plane perpendicular to it. A
 * ring around the column at height `y` and "radius" `radius` is
 * `ringPoint(radius, theta, y, z)`, sweeping `theta` around the column.
 */

interface Vec3 {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

export interface Camera {
  readonly originX: number;
  readonly originY: number;
  readonly focal: number;
  readonly dist: number;
  readonly cosTilt: number;
  readonly sinTilt: number;
  /** `focal / dist`: the projection scale of a point at the camera's aim, used to normalise near/far. */
  readonly baseScale: number;
}

interface Projected {
  readonly x: number;
  readonly y: number;
  /** Screen px per world unit at this point: larger = nearer the camera. */
  readonly scale: number;
  /** Camera-space depth along the view axis; always positive for a visible point. */
  readonly depth: number;
}

export function makeCamera(
  originX: number,
  originY: number,
  focal: number,
  dist: number,
  tiltDeg: number,
): Camera {
  const t = (tiltDeg * Math.PI) / 180;
  return {
    originX,
    originY,
    focal,
    dist,
    cosTilt: Math.cos(t),
    sinTilt: Math.sin(t),
    baseScale: focal / dist,
  };
}

/** Projects a world point through the camera's pitch tilt and perspective divide. */
export function project(p: Vec3, cam: Camera): Projected {
  const y1 = p.y * cam.cosTilt - p.z * cam.sinTilt;
  const z1 = p.y * cam.sinTilt + p.z * cam.cosTilt;
  const depth = z1 + cam.dist;
  const scale = cam.focal / Math.max(depth, 1);
  return {
    x: cam.originX + p.x * scale,
    y: cam.originY - y1 * scale,
    scale,
    depth,
  };
}

/** A point on a ring of the given radius around the column axis, at height `y` and depth offset `z`. */
export function ringPoint(
  radius: number,
  theta: number,
  y: number,
  z: number,
): Vec3 {
  return { x: radius * Math.cos(theta), y, z: z + radius * Math.sin(theta) };
}

/**
 * A point on the plasma torus: `theta` sweeps around the column (the main
 * ring), `phi` sweeps around the tube's cross-section. `R` is the major
 * radius (column to tube centre), `a` the tube radius.
 */
export function torusPoint(
  R: number,
  a: number,
  theta: number,
  phi: number,
  centerY: number,
  centerZ: number,
): Vec3 {
  const rho = R + a * Math.cos(phi);
  return {
    x: rho * Math.cos(theta),
    y: centerY + a * Math.sin(phi),
    z: centerZ + rho * Math.sin(theta),
  };
}

/** Clamps a near/far factor derived from `scale / baseScale` so streaks never vanish or blow out. */
export function nearFactor(
  scale: number,
  cam: Camera,
  min = 0.16,
  max = 1.7,
): number {
  const f = scale / cam.baseScale;
  return Math.max(min, Math.min(max, f));
}
