// A minimal perspective camera, the same idea as
// products/fusion/hero/Tokamak/geometry.ts (never imported from here):
// a world point with a real depth, a focal length and a perspective
// divide. The slab has no rotation, so this only needs the divide, not
// Tokamak's pitch tilt.
export interface Vec3 {
  readonly x: number;
  readonly y: number;
  /** Distance from the camera's near plane; 0 = nearest the camera. */
  readonly z: number;
}

export interface Camera {
  readonly originX: number;
  readonly originY: number;
  readonly focal: number;
  /** Distance from the camera to the z = 0 plane. */
  readonly dist: number;
}

export interface Projected {
  readonly x: number;
  readonly y: number;
  /** Screen px per world unit at this depth; larger = nearer the camera. */
  readonly scale: number;
  readonly depth: number;
}

export function makeCamera(
  originX: number,
  originY: number,
  focal: number,
  dist: number,
): Camera {
  return { originX, originY, focal, dist };
}

export function project(p: Vec3, cam: Camera): Projected {
  const depth = cam.dist + p.z;
  const scale = cam.focal / depth;
  return {
    x: cam.originX + p.x * scale,
    y: cam.originY + p.y * scale,
    scale,
    depth,
  };
}

/**
 * The inverse of `project`: the world point that projects to exactly
 * (sx, sy) at depth z. Used to place a node at a chosen screen position
 * and a chosen depth -- the node's (x, y, z) is then a real point in the
 * 3D slab, and re-projecting it reproduces the same screen position.
 */
export function placeAt(sx: number, sy: number, z: number, cam: Camera): Vec3 {
  const depth = cam.dist + z;
  const scale = cam.focal / depth;
  return {
    x: (sx - cam.originX) / scale,
    y: (sy - cam.originY) / scale,
    z,
  };
}
