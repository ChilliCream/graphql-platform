// Minimal 3D projection, the same idea as the Fusion Tokamak hero's
// geometry.ts (camera, project, perspective divide) rebuilt for this
// folder: a point in world space, a camera with a pitch tilt, and a
// perspective projection to screen space.
export interface Vec3 {
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
  readonly baseScale: number;
}

export interface Projected {
  readonly x: number;
  readonly y: number;
  readonly scale: number;
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

/** A world point on the ring at `radius` around the vertical (Y) axis, rotated by `theta`. */
export function ringPoint(radius: number, theta: number, y: number): Vec3 {
  return { x: radius * Math.cos(theta), y, z: radius * Math.sin(theta) };
}
